using System;
using System.Collections.Generic;
using System.Linq;
using Beam;
using Beam.Utilities.Reflection;
using CPG_CameraPerspective;
using LE_LevelEditor.Core;
using LE_LevelEditor.Events;
using LE_LevelEditor.LEInput;
using LE_LevelEditor.Logic;
using LE_LevelEditor.UI;
using UnityEngine;

namespace LE_LevelEditor;

public class LE_LevelEditorMain : Singleton<LE_LevelEditorMain>, LE_IInputHandler
{
	private enum EEstimateDistanceMode
	{
		AVERAGE,
		NEAREST
	}

	public static int MAP_EDITOR_SEED;

	[SerializeField]
	private GameObject _screenShotCamera;

	public List<Texture2D> MapScreenshots = new List<Texture2D>();

	public List<LE_Object> MapObjectReferences = new List<LE_Object>();

	public GameObject Boundary;

	public Transform ObjectDumpContainer;

	public Transform ObjectInstanceContainer;

	[SerializeField]
	[Tooltip("Show/hide camera perspective gizmo. Works like the built-in Unity Editor scene view handle: click on the axes to change camera's position and direction; click in the middle to toggle between perspective and orthographic camera view.")]
	private bool IS_WITH_CAMERA_PERSPECTIVE_GIZMO = true;

	[SerializeField]
	[Tooltip("Camera perspective gizmo's render layer index. This layer should be excluded from the main camera.")]
	private int CAMERA_PERSPECTIVE_GIZMO_LAYER = 14;

	[SerializeField]
	[Tooltip("Root object map shown in the object editor's tree browser.")]
	private LE_ObjectMap ROOT_OBJECT_MAP;

	[SerializeField]
	[Tooltip("Enable to bring focused objects to the center of the visible screen (not hidden by right menu). On low resolution devices the right menu could use half of the screen. Without the oblique projection only the left half of a big object would be visible, because the right half would be behind the menu.")]
	private bool IS_OBLIQUE_FOCUS_CENTERING = true;

	[SerializeField]
	[Tooltip("If enabled, then the camera will be controlled by the level editor as described in the help popup.")]
	private bool IS_CAMERA_MOVEMENT = true;

	[SerializeField]
	[Tooltip("If enabled, then the terrain editor logic will be initialized and terrain related events handled.")]
	private bool IS_TERRAIN_EDITOR = true;

	[SerializeField]
	[Tooltip("If enabled, then the object editor logic will be initialized and object related events handled.")]
	private bool IS_OBJECT_EDITOR = true;

	[SerializeField]
	[LE_EnumFlags("Flag enum containing activated key combinations. For example if DUPLICATE is picked then objects will be cloned when Ctrl+D is typed.")]
	private LE_EKeyCombo ACTIVE_KEY_COMBOS = LE_EKeyCombo.FOCUS | LE_EKeyCombo.DUPLICATE;

	private LE_EEditMode m_editMode;

	private List<LE_GUI3dBase> m_GUI3d = new List<LE_GUI3dBase>();

	private LE_GUI3dTerrain m_GUI3dTerrain;

	private LE_GUI3dObject m_GUI3dObject;

	private List<LE_LogicBase> m_logic = new List<LE_LogicBase>();

	private LE_LogicTerrain m_logicTerrain;

	private LE_LogicLevel m_logicLevel;

	private LE_LogicObjects m_LogicObjects;

	private LE_Input m_input;

	private CPG_CameraPerspectiveGizmo m_cameraPerspectiveGizmo;

	private Vector3 m_camPivot = Vector3.zero;

	private Camera m_cam;

	private Rect m_perspectiveGizmoRect;

	private bool m_isFirstUpdateInitialized;

	private bool m_isSecondUpdateInitialized;

	private Action m_executeWhenReady;

	private int m_lastChangeFrame = -1;

	public LE_EEditMode EditMode => m_editMode;

	public LE_GUI3dTerrain GUI3dTerrain => m_GUI3dTerrain;

	public Vector3 CamPivot
	{
		get
		{
			return m_camPivot;
		}
		set
		{
			m_camPivot = value;
		}
	}

	private Camera Cam
	{
		get
		{
			if (m_cam == null)
			{
				m_cam = Camera.main;
			}
			return m_cam;
		}
	}

	public bool IsReady => m_isFirstUpdateInitialized;

	public int LastChangeFrame => m_lastChangeFrame;

	public void ExecuteWhenReady(Action p_action)
	{
		m_executeWhenReady = (Action)Delegate.Combine(m_executeWhenReady, p_action);
	}

	public static bool SetObliqueFocusProjectionMatrix(bool p_isObliqueProjectionEnabled)
	{
		if (LE_GUIInterface.Instance.delegates.GetObliqueCameraPerspectiveRightPixelOffset != null)
		{
			if (p_isObliqueProjectionEnabled)
			{
				Camera.main.ResetProjectionMatrix();
				Matrix4x4 projectionMatrix = Camera.main.projectionMatrix;
				projectionMatrix[0, 2] = LE_GUIInterface.Instance.delegates.GetObliqueCameraPerspectiveRightPixelOffset() / (float)Screen.width;
				Camera.main.projectionMatrix = projectionMatrix;
			}
			else
			{
				Camera.main.ResetProjectionMatrix();
			}
			return true;
		}
		return false;
	}

	void LE_IInputHandler.SetCursorPosition(Vector3 p_cursorScreenCoords)
	{
		for (int i = 0; i < m_GUI3d.Count; i++)
		{
			if (m_GUI3d[i].ActiveEditMode == m_editMode && m_GUI3d[i].IsInteractable)
			{
				m_GUI3d[i].SetCursorPosition(p_cursorScreenCoords);
			}
		}
	}

	void LE_IInputHandler.SetIsCursorAction(bool p_isCursorAction)
	{
		for (int i = 0; i < m_GUI3d.Count; i++)
		{
			if (m_GUI3d[i].ActiveEditMode == m_editMode)
			{
				m_GUI3d[i].SetIsCursorAction(p_isCursorAction);
			}
		}
	}

	void LE_IInputHandler.MoveCamera(Vector3 p_fromScreenCoords, Vector3 p_toScreenCoords, bool scrolling = false)
	{
		if (!IS_CAMERA_MOVEMENT)
		{
			return;
		}
		Camera cam = Cam;
		if ((!scrolling || !IsMouseOverGUI()) && cam != null)
		{
			float num = EstimateDistanceToLevel(EEstimateDistanceMode.AVERAGE);
			Vector3 direction = p_toScreenCoords - p_fromScreenCoords;
			direction = cam.transform.TransformDirection(direction);
			Vector3 vector;
			if (cam.orthographic)
			{
				float num2 = Vector3.Dot(direction, cam.transform.forward);
				direction -= cam.transform.forward * num2;
				cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - num2 * (cam.orthographicSize / (float)Screen.width), 5f, 1000f);
				vector = direction * (cam.orthographicSize / (float)Screen.width);
			}
			else
			{
				vector = direction * (num / (float)Screen.width);
			}
			cam.transform.position += vector;
			cam.transform.position = new Vector3(Mathf.Clamp(cam.transform.position.x, -300f, 300f), Mathf.Clamp(cam.transform.position.y, -100f, 150f), Mathf.Clamp(cam.transform.position.z, -300f, 300f));
		}
	}

	void LE_IInputHandler.RotateCamera(Vector3 p_fromScreenCoords, Vector3 p_toScreenCoords)
	{
		if (!IS_CAMERA_MOVEMENT)
		{
			return;
		}
		Camera cam = Cam;
		if (cam != null)
		{
			Ray ray = cam.ScreenPointToRay(p_fromScreenCoords);
			Ray ray2 = cam.ScreenPointToRay(p_toScreenCoords);
			if (cam.orthographic)
			{
				ray.direction = (ray.origin + ray.direction * 100f - cam.transform.position).normalized;
				ray2.direction = (ray2.origin + ray2.direction * 100f - cam.transform.position).normalized;
			}
			float num = Vector3.Angle(ray.direction, ray2.direction);
			Vector3 normalized = Vector3.Cross(ray.direction, ray2.direction).normalized;
			float num2 = Mathf.Abs(cam.transform.forward.y);
			if (num2 > 0.9f)
			{
				num2 = (1f - num2) / 0.100000024f;
				num2 *= num2;
				float num3 = Vector3.Dot(normalized, cam.transform.right);
				normalized -= (1f - num2) * (normalized - num3 * cam.transform.right);
			}
			cam.transform.Rotate(normalized, 0f - num, Space.World);
			if (cam.transform.up.y > 0f)
			{
				cam.transform.LookAt(cam.transform.position + cam.transform.forward, Vector3.up);
			}
			else
			{
				cam.transform.LookAt(cam.transform.position + cam.transform.forward, Vector3.down);
			}
		}
	}

	void LE_IInputHandler.RotateCameraAroundPivot(Vector3 p_fromScreenCoords, Vector3 p_toScreenCoords)
	{
		if (!IS_CAMERA_MOVEMENT)
		{
			return;
		}
		Camera cam = Cam;
		if (cam != null)
		{
			Vector2 vector = new Vector2(p_toScreenCoords.x, p_toScreenCoords.y) - new Vector2(p_fromScreenCoords.x, p_fromScreenCoords.y);
			vector.x /= Screen.width;
			vector.y /= Screen.height;
			cam.transform.RotateAround(m_camPivot, cam.transform.right, vector.y * 300f);
			cam.transform.RotateAround(m_camPivot, cam.transform.up, (0f - vector.x) * 300f);
			if (cam.transform.up.y > 0f)
			{
				cam.transform.LookAt(m_camPivot, Vector3.up);
			}
			else
			{
				cam.transform.LookAt(m_camPivot, Vector3.down);
			}
		}
	}

	private void Start()
	{
		if (LE_GUIInterface.Instance == null)
		{
			Debug.LogError("LE_LevelEditorMain: a LE_GUIInterface object must be added to the scene!");
		}
		LE_ConfigTerrain lE_ConfigTerrain = GetComponentInChildren<LE_ConfigTerrain>();
		if (lE_ConfigTerrain == null)
		{
			Debug.LogError("LE_LevelEditorMain: a LE_ConfigTerrain component must be added to the game object with the LE_LevelEditorMain script!");
			lE_ConfigTerrain = gameObject.AddComponent<LE_ConfigTerrain>();
		}
		if (!IS_TERRAIN_EDITOR)
		{
			if (lE_ConfigTerrain.Brushes.Length != 0)
			{
				Debug.LogWarning("LE_LevelEditorMain: IS_TERRAIN_EDITOR is set to 'false', but you have provided a non empty array for LE_ConfigTerrain.Brushes! This is performance waist and will increase loading time! Remove brushes or reenable terrain editing.");
			}
			if (lE_ConfigTerrain.BrushProjector != null)
			{
				Debug.LogWarning("LE_LevelEditorMain: IS_TERRAIN_EDITOR is set to 'false', but you have provided a value for LE_ConfigTerrain.BrushProjector! This is performance waist and will increase loading time! Remove brush projector from scene or reenable terrain editing.");
			}
		}
		Camera cam = Cam;
		if (cam == null)
		{
			Debug.LogError("LE_LevelEditorMain: Start: could not find main camera!");
		}
		if (IS_OBJECT_EDITOR)
		{
			m_GUI3dObject = gameObject.AddComponent<LE_GUI3dObject>();
			m_GUI3dObject.TERRAIN_LAYER = 14;
			LE_GUI3dObject gUI3dObject = m_GUI3dObject;
			gUI3dObject.OnFocused = (Action<Vector3, float>)Delegate.Combine(gUI3dObject.OnFocused, new Action<Vector3, float>(OnGUI3dObjectFocused));
			m_GUI3dObject.IsKeyComboFocus = (ACTIVE_KEY_COMBOS & LE_EKeyCombo.FOCUS) != 0;
			m_GUI3dObject.IsKeyComboDuplicate = (ACTIVE_KEY_COMBOS & LE_EKeyCombo.DUPLICATE) != 0;
			m_GUI3d.Add(m_GUI3dObject);
		}
		m_input = new LE_Input(this);
		m_camPivot = cam.transform.position + cam.transform.forward * 100f;
		LE_EventInterface.OnChangeLevelData = (EventHandler)Delegate.Combine(LE_EventInterface.OnChangeLevelData, new EventHandler(OnChangeLevelData));
		LE_GUIInterface.EventHandlers events = LE_GUIInterface.Instance.events;
		events.OnEditModeBtn = (EventHandler<LE_GUIInterface.EventHandlers.EditModeEventArgs>)Delegate.Combine(events.OnEditModeBtn, new EventHandler<LE_GUIInterface.EventHandlers.EditModeEventArgs>(OnEditModeBtn));
		MAP_EDITOR_SEED = StrandedWorld.GetRandowWorldSeed();
	}

	private void Initialize_InFirstUpdate()
	{
		Camera cam = Cam;
		LE_ConfigTerrain componentInChildren = GetComponentInChildren<LE_ConfigTerrain>();
		if (LE_GUIInterface.Instance.delegates.IsCursorOverUI == null)
		{
			Debug.LogError("LE_LevelEditorMain: you have not setup LE_GUIInterface.delegates.IsCursorOverUI. Terrain might be edited behind UI while the user interacts with the UI, same is true for object placement!");
		}
		if (IS_TERRAIN_EDITOR)
		{
			m_GUI3dTerrain = gameObject.AddComponent<LE_GUI3dTerrain>();
			m_GUI3d.Add(m_GUI3dTerrain);
			InitializeDefaultTerrain(componentInChildren);
			m_logicTerrain = new LE_LogicTerrain(componentInChildren, m_GUI3dTerrain);
			m_logic.Add(m_logicTerrain);
		}
		else if (componentInChildren.CustomDefaultTerrain != null)
		{
			Debug.LogWarning("LE_LevelEditorMain: IS_TERRAIN_EDITOR is set to 'false', but you have provided a value for LE_ConfigTerrain.CustomDefaultTerrain! The value will be ignored!");
		}
		m_logicLevel = new LE_LogicLevel(m_GUI3dTerrain, m_GUI3dObject, IS_OBLIQUE_FOCUS_CENTERING);
		m_logic.Add(m_logicLevel);
		if (IS_OBJECT_EDITOR)
		{
			if (ROOT_OBJECT_MAP == null)
			{
				InitializeObjects();
			}
			m_LogicObjects = new LE_LogicObjects(m_GUI3dObject, ROOT_OBJECT_MAP);
			m_logic.Add(m_LogicObjects);
		}
		if (cam != null && IS_WITH_CAMERA_PERSPECTIVE_GIZMO)
		{
			if (LE_GUIInterface.Instance.delegates.GetCameraPerspectiveGizmoRightPixelOffset != null)
			{
				float num = 0.2f * (float)Screen.width / cam.aspect;
				float height = 0.2f * (float)Screen.height;
				m_perspectiveGizmoRect = new Rect((float)Screen.width - LE_GUIInterface.Instance.delegates.GetCameraPerspectiveGizmoRightPixelOffset() - num, 0f, num, height);
				if (string.IsNullOrEmpty(LayerMask.LayerToName(CAMERA_PERSPECTIVE_GIZMO_LAYER)))
				{
					Debug.LogWarning("LE_LevelEditorMain: Inspector property 'CAMERA_PERSPECTIVE_GIZMO_LAYER' is set to '" + CAMERA_PERSPECTIVE_GIZMO_LAYER + "', but this layer has no name in 'Tags & Layers' set. Please set the name of this layer to 'CameraPerspectiveGizmo' or change the value of the 'CAMERA_PERSPECTIVE_GIZMO_LAYER' property! To open the 'Tags & Layers' manager select any game object, in the inspector click on the layer drop down at top right then click on 'Add Layer...'.");
				}
				else if (LayerMask.LayerToName(CAMERA_PERSPECTIVE_GIZMO_LAYER) != "CameraPerspectiveGizmo")
				{
					Debug.LogWarning("LE_LevelEditorMain: Inspector property 'CAMERA_PERSPECTIVE_GIZMO_LAYER' is set to '" + CAMERA_PERSPECTIVE_GIZMO_LAYER + "', but the name of this layer is '" + LayerMask.LayerToName(CAMERA_PERSPECTIVE_GIZMO_LAYER) + "'. Is this intended? If not please set the name of this layer to 'CameraPerspectiveGizmo' or change the value of the 'CAMERA_PERSPECTIVE_GIZMO_LAYER' property! To open the 'Tags & Layers' manager select any game object, in the inspector click on the layer drop down at top right then click on 'Add Layer...'.");
				}
				m_cameraPerspectiveGizmo = CPG_CameraPerspectiveGizmo.Create(cam, CAMERA_PERSPECTIVE_GIZMO_LAYER);
				if (m_cameraPerspectiveGizmo != null)
				{
					m_cameraPerspectiveGizmo.transform.position = Vector3.down * -10000f;
					m_cameraPerspectiveGizmo.OrthoOffset = cam.farClipPlane * 0.2f;
					cam.cullingMask &= ~(1 << CAMERA_PERSPECTIVE_GIZMO_LAYER);
					m_cameraPerspectiveGizmo.RelativeScreenSize = 0.2f;
					float num2 = LE_GUIInterface.Instance.delegates.GetCameraPerspectiveGizmoRightPixelOffset() / (float)Screen.width;
					m_cameraPerspectiveGizmo.RelativeScreenPos = new Vector2(1f - 0.1f / cam.aspect - num2, 0.9f);
					CPG_CameraPerspectiveGizmo cameraPerspectiveGizmo = m_cameraPerspectiveGizmo;
					cameraPerspectiveGizmo.m_onBeforeSwitchToOrthographic = (EventHandler)Delegate.Combine(cameraPerspectiveGizmo.m_onBeforeSwitchToOrthographic, new EventHandler(OnCameraPerspectiveSwitchToOrthographic));
					CPG_CameraPerspectiveGizmo cameraPerspectiveGizmo2 = m_cameraPerspectiveGizmo;
					cameraPerspectiveGizmo2.m_onAfterSwitchToPerspective = (EventHandler)Delegate.Combine(cameraPerspectiveGizmo2.m_onAfterSwitchToPerspective, new EventHandler(OnCameraPerspectiveSwitchToPerspective));
				}
			}
			else
			{
				Debug.LogError("LE_LevelEditorMain: LE_GUIInterface.delegates.GetCameraPerspectiveGizmoRightPixelOffset was not set from the UI scripts! The camera perspective gizmo cannot be placed to the right screen position! To set this delegate use 'LE_GUIInterface.Instance.delegates.GetCameraPerspectiveGizmoRightPixelOffset = ...' in one of the Start functions of your scripts!");
			}
		}
		if (IS_OBLIQUE_FOCUS_CENTERING && !SetObliqueFocusProjectionMatrix(p_isObliqueProjectionEnabled: true))
		{
			Debug.LogError("LE_LevelEditorMain: IS_OBLIQUE_FOCUS_CENTERING is true, but you have not provided the LE_GUIInterface.delegates.GetObliqueCameraPerspectiveRightPixelOffset delegate! Provide it to bring focused objects in the center of the visible (not covered by UI) screen area!");
		}
	}

	public void OnObjectEditModeKey(LE_EObjectEditMode editMode)
	{
		m_GUI3dObject.ObjectEditMode = editMode;
	}

	public void OnSelectedObjectDeleteKey()
	{
		if (m_GUI3dObject.SelectedObject != null)
		{
			if (LE_GUIInterface.Instance.delegates.ShowPopupConfirmDeleteObject != null)
			{
				m_LogicObjects.ConfirmDelete();
			}
			else
			{
				Debug.LogError("LE_LogicObject: OnSelectedObjectDeleteBtn: you have triggered the delete button behaviour, but the LE_GUIInterface.delegates.ShowPopupConfirmDeleteObject delegate is not set! Set this delegate to make the delete button work!");
			}
		}
	}

	public void OnSelectedObjectDuplicateKey()
	{
		m_GUI3dObject.CloneObject();
	}

	public void InitializeObjects()
	{
		Debug.Log("LevelEditorMain:: Initializing Editor Objects");
		List<LE_ObjectMap> list = new List<LE_ObjectMap>();
		foreach (LE_Object.Categories value in Enum.GetValues(typeof(LE_Object.Categories)))
		{
			if (value != LE_Object.Categories.NONE)
			{
				LE_ObjectMap lE_ObjectMap = new LE_ObjectMap();
				lE_ObjectMap.CategoryName = value.GetAttribute<DescriptionAttribute>().Text;
				lE_ObjectMap.ObjectPrefabs = new List<LE_Object>();
				list.Add(lE_ObjectMap);
			}
		}
		ROOT_OBJECT_MAP = new LE_ObjectMap();
		ROOT_OBJECT_MAP.ObjectPrefabs = new List<LE_Object>();
		Shader.SetGlobalColor("_HighlightRimColor", Color.black);
		Shader.SetGlobalFloat("_HighlightRimPower", 1f);
		LE_Object[] componentsInChildren = ObjectInstanceContainer.GetComponentsInChildren<LE_Object>();
		foreach (LE_Object item in componentsInChildren)
		{
			MapObjectReferences.Add(item);
		}
		ObjectInstanceContainer.gameObject.SetActive(value: false);
		foreach (LE_Object mapObjectReference in MapObjectReferences)
		{
			if (mapObjectReference.Category == LE_Object.Categories.NONE)
			{
				ROOT_OBJECT_MAP.ObjectPrefabs.Add(mapObjectReference);
			}
			else
			{
				list[(int)(mapObjectReference.Category - 1)].ObjectPrefabs.Add(mapObjectReference);
			}
		}
		foreach (LE_ObjectMap item2 in list)
		{
			item2.ObjectPrefabs = item2.ObjectPrefabs.OrderBy((LE_Object go) => go.gameObject.name).ToList();
			ROOT_OBJECT_MAP.SubObjectMaps.Add(item2);
		}
	}

	private void Validate_References()
	{
	}

	private void Initialize_InSecondUpdate()
	{
		if (IS_OBLIQUE_FOCUS_CENTERING)
		{
			Camera cam = Cam;
			if (cam != null)
			{
				SetObliqueFocusProjectionMatrix(!cam.orthographic);
			}
		}
	}

	private void InitializeDefaultTerrain(LE_ConfigTerrain p_LEConfTerrain)
	{
		if (p_LEConfTerrain.CustomDefaultTerrain != null && p_LEConfTerrain.CustomDefaultTerrain.terrainData != null)
		{
			m_GUI3dTerrain.SetTerrain(p_LEConfTerrain.CustomDefaultTerrain);
			LE_EventInterface.LevelDataChanged(p_LEConfTerrain.CustomDefaultTerrain.gameObject);
			if (LE_GUIInterface.Instance.delegates.SetTerrainUIMode != null)
			{
				LE_GUIInterface.Instance.delegates.SetTerrainUIMode(LE_GUIInterface.Delegates.ETerrainUIMode.EDIT);
			}
			else
			{
				Debug.LogWarning("LE_LevelEditorMain: you have not set the LE_GUIInterface.delegates.SetTerrainUIMode delegate. You need to set it for example if you want to disable the create UI if the default Unity terrain is set!");
			}
		}
		else if (LE_GUIInterface.Instance.delegates.SetTerrainUIMode != null)
		{
			LE_GUIInterface.Instance.delegates.SetTerrainUIMode(LE_GUIInterface.Delegates.ETerrainUIMode.CREATE);
		}
		else
		{
			Debug.LogWarning("LE_LevelEditorMain: you have not set the LE_GUIInterface.delegates.SetTerrainUIMode delegate. You need to set it for example if you want to show the create UI if there is no default Unity terrain set!");
		}
	}

	private void Update()
	{
		if (m_isFirstUpdateInitialized && !m_isSecondUpdateInitialized)
		{
			m_isSecondUpdateInitialized = true;
			Initialize_InSecondUpdate();
		}
		if (!m_isFirstUpdateInitialized)
		{
			m_isFirstUpdateInitialized = true;
			Initialize_InFirstUpdate();
		}
		bool flag = IsMouseOverGUI();
		for (int i = 0; i < m_GUI3d.Count; i++)
		{
			m_GUI3d[i].IsInteractable = !flag && m_GUI3d[i].ActiveEditMode == m_editMode;
		}
		for (int j = 0; j < m_logic.Count; j++)
		{
			m_logic[j].Update();
		}
		m_input.Update();
		UpdateCameraPivotPoint();
		if (m_cameraPerspectiveGizmo != null)
		{
			m_cameraPerspectiveGizmo.Pivot = m_camPivot;
		}
		if (m_executeWhenReady != null && IsReady)
		{
			m_executeWhenReady();
			m_executeWhenReady = null;
		}
	}

	public bool IsMouseOverGUI()
	{
		bool flag = LE_GUIInterface.Instance.delegates.IsCursorOverUI != null && LE_GUIInterface.Instance.delegates.IsCursorOverUI();
		bool flag2 = false;
		if (IS_WITH_CAMERA_PERSPECTIVE_GIZMO && !flag)
		{
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.y = Mathf.Clamp((float)Screen.height - mousePosition.y, 0f, Screen.height);
			mousePosition.x = Mathf.Clamp(mousePosition.x, 0f, Screen.width);
			flag2 = m_perspectiveGizmoRect.Contains(mousePosition);
		}
		return flag | flag2;
	}

	private void OnGUI3dObjectFocused(Vector3 p_focusOn, float p_distance)
	{
		m_camPivot = p_focusOn;
		Camera cam = Cam;
		if (cam != null && cam.orthographic && m_cameraPerspectiveGizmo != null)
		{
			cam.transform.position = m_camPivot - cam.transform.forward * Mathf.Abs(m_cameraPerspectiveGizmo.OrthoOffset + p_distance);
			cam.orthographicSize = p_distance;
		}
	}

	protected override void OnDestroy()
	{
		LE_EventInterface.OnChangeLevelData = (EventHandler)Delegate.Remove(LE_EventInterface.OnChangeLevelData, new EventHandler(OnChangeLevelData));
		if (LE_GUIInterface.Instance != null)
		{
			LE_GUIInterface.EventHandlers events = LE_GUIInterface.Instance.events;
			events.OnEditModeBtn = (EventHandler<LE_GUIInterface.EventHandlers.EditModeEventArgs>)Delegate.Remove(events.OnEditModeBtn, new EventHandler<LE_GUIInterface.EventHandlers.EditModeEventArgs>(OnEditModeBtn));
		}
		for (int i = 0; i < m_GUI3d.Count; i++)
		{
			if (m_GUI3d[i] is LE_GUI3dObject)
			{
				((LE_GUI3dObject)m_GUI3d[i]).OnFocused = null;
			}
		}
		m_executeWhenReady = null;
		if (m_input != null)
		{
			m_input.Destroy();
		}
		for (int j = 0; j < m_logic.Count; j++)
		{
			m_logic[j].Destroy();
		}
		base.OnDestroy();
	}

	private void OnChangeLevelData(object p_sender, EventArgs p_args)
	{
		m_lastChangeFrame = Time.frameCount;
	}

	private void OnEditModeBtn(object p_sender, LE_GUIInterface.EventHandlers.EditModeEventArgs p_args)
	{
		m_editMode = p_args.EditMode;
		if (m_editMode != LE_EEditMode.TERRAIN && m_GUI3dTerrain != null)
		{
			m_GUI3dTerrain.HideCursor();
		}
		if (m_editMode != LE_EEditMode.OBJECT && m_GUI3dObject != null)
		{
			m_GUI3dObject.RemoveSelection();
		}
		if (m_editMode != LE_EEditMode.NONE)
		{
			_screenShotCamera.SetActive(value: false);
		}
		else
		{
			_screenShotCamera.SetActive(value: true);
		}
	}

	private void OnCameraPerspectiveSwitchToOrthographic(object p_sender, EventArgs p_args)
	{
		if (IS_OBLIQUE_FOCUS_CENTERING)
		{
			SetObliqueFocusProjectionMatrix(p_isObliqueProjectionEnabled: false);
		}
		if (m_cameraPerspectiveGizmo != null)
		{
			float value = EstimateDistanceToLevel(EEstimateDistanceMode.NEAREST);
			Camera cam = Cam;
			if (cam != null)
			{
				cam.orthographicSize = Mathf.Clamp(value, 5f, 60f);
			}
		}
	}

	private void OnCameraPerspectiveSwitchToPerspective(object p_sender, EventArgs p_args)
	{
		if (IS_OBLIQUE_FOCUS_CENTERING)
		{
			SetObliqueFocusProjectionMatrix(p_isObliqueProjectionEnabled: true);
		}
	}

	private void UpdateCameraPivotPoint()
	{
		Camera cam = Cam;
		if (cam != null)
		{
			Vector3 rhs = m_camPivot - cam.transform.position;
			float num = Vector3.Dot(cam.transform.forward, rhs);
			if (num < 1f)
			{
				float num2 = num - 1.5f;
				m_camPivot = cam.transform.position - cam.transform.forward * num2;
				rhs = m_camPivot - cam.transform.position;
				num = Vector3.Dot(cam.transform.forward, rhs);
			}
			if (Vector3.Cross(cam.transform.forward, rhs).magnitude > 1f)
			{
				m_camPivot = cam.transform.position + cam.transform.forward * num;
			}
		}
	}

	private float EstimateDistanceToLevel(EEstimateDistanceMode p_mode)
	{
		float result = 300f;
		Camera cam = Cam;
		if (cam != null)
		{
			float num = 0f;
			Vector3 vector = Vector3.zero;
			float num2 = float.MaxValue;
			for (float num3 = 0f; num3 < 2f; num3++)
			{
				for (float num4 = 0f; num4 < 2f; num4++)
				{
					if (!Physics.Raycast(cam.ScreenPointToRay(new Vector3(cam.rect.width * (float)Screen.width * (0.25f + 0.5f * num3), cam.rect.height * (float)Screen.height * (0.25f + 0.5f * num4), 0f)), out var hitInfo))
					{
						continue;
					}
					num++;
					if (p_mode == EEstimateDistanceMode.AVERAGE)
					{
						vector += hitInfo.point;
						continue;
					}
					float magnitude = (hitInfo.point - cam.transform.position).magnitude;
					if (num2 > magnitude)
					{
						num2 = magnitude;
						vector = hitInfo.point;
					}
				}
			}
			if (num > 0f)
			{
				if (p_mode == EEstimateDistanceMode.AVERAGE)
				{
					vector /= num;
				}
				result = (vector - cam.transform.position).magnitude;
			}
		}
		return result;
	}

	public void ClearLevel()
	{
		foreach (Transform item in ObjectDumpContainer)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}
}
