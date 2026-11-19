using System;
using System.Collections.Generic;
using Beam;
using Funlabs;
using LE_LevelEditor.Core;
using LE_LevelEditor.Events;
using MyUtility;
using Rewired;
using S_SnapTools;
using UnityEngine;

namespace LE_LevelEditor.UI;

public class LE_GUI3dObject : LE_GUI3dBase
{
	private const float MIN_FOCUS_DISTANCE = 3f;

	private const float SELECTION_MAX_OVERSIZE = 10f;

	private LE_Object m_object;

	private bool m_isObjectPlaceable;

	private string m_dragMessage = "";

	private LE_Object m_previewInstance;

	private LE_Object m_selectedObject;

	private LE_Object m_cursorActionOnObject;

	private bool m_isSelectionPossible;

	private bool m_isCursorAction;

	private bool m_isCursorActionInThisFrame;

	private Vector3 m_lastCursorActionStartPos = -100f * Vector3.one;

	private bool m_isSnapToObjectActive = true;

	private Dictionary<string, S_SnapToObject> m_snapPointUIDsToSnapPoints = new Dictionary<string, S_SnapToObject>();

	private Dictionary<string, int> m_snapPointUIDsToObjUIDs = new Dictionary<string, int>();

	public int TERRAIN_LAYER = 14;

	public Action<Vector3, float> OnFocused;

	private LE_EObjectEditMode m_objectEditMode;

	private bool m_isKeyComboDuplicate = true;

	private bool m_isKeyComboFocus = true;

	public LE_Object SelectedObject => m_selectedObject;

	public Dictionary<string, int> SnapPointUIDsToObjUIDs => m_snapPointUIDsToObjUIDs;

	public override LE_EEditMode ActiveEditMode => LE_EEditMode.OBJECT;

	public LE_EObjectEditMode ObjectEditMode
	{
		get
		{
			return m_objectEditMode;
		}
		set
		{
			m_objectEditMode = value;
		}
	}

	public bool IsKeyComboDuplicate
	{
		get
		{
			return m_isKeyComboDuplicate;
		}
		set
		{
			m_isKeyComboDuplicate = value;
		}
	}

	public bool IsKeyComboFocus
	{
		get
		{
			return m_isKeyComboFocus;
		}
		set
		{
			m_isKeyComboFocus = value;
		}
	}

	public override void SetCursorPosition(Vector3 p_cursorScreenCoords)
	{
		if (m_object != null && (m_object.SnapType == LE_Object.ESnapType.SNAP_TO_TERRAIN || m_object.SnapType == LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN))
		{
			m_cursorScreenCoords = p_cursorScreenCoords;
			m_cursorRay = Camera.main.ScreenPointToRay(p_cursorScreenCoords);
			SetIsCursorOverSomething(Physics.Raycast(m_cursorRay, out m_cursorHitInfo, float.MaxValue, 1 << TERRAIN_LAYER, QueryTriggerInteraction.Ignore));
		}
		else
		{
			base.SetCursorPosition(p_cursorScreenCoords);
		}
	}

	public override void SetIsCursorAction(bool p_isCursorAction)
	{
		if (IsInteractable)
		{
			m_isCursorActionInThisFrame |= p_isCursorAction;
		}
	}

	public void SetDraggableObject(LE_Object p_object)
	{
		if (m_object != p_object)
		{
			m_dragMessage = "";
			SetDragMessageInUI();
			m_object = p_object;
			m_isObjectPlaceable = IsObjectPlaceable();
		}
	}

	public void Focus()
	{
		if (!(m_selectedObject != null))
		{
			return;
		}
		Renderer[] componentsInChildren = m_selectedObject.GetComponentsInChildren<Renderer>();
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		float num = 0f;
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			if (renderer.GetComponentInParent<LE_ObjectEditHandle>() == null)
			{
				vector += renderer.bounds.center;
				vector2 += renderer.bounds.size;
				num++;
			}
		}
		if (num != 0f)
		{
			vector *= 1f / num;
			vector2 *= 1f / num;
		}
		else
		{
			vector = m_selectedObject.transform.position;
			vector2 = Vector3.one * 3f;
		}
		Vector3 vector3 = Camera.main.transform.position - vector;
		float num2 = Mathf.Max(vector2.x, vector2.y, vector2.z) * 3f;
		Camera.main.transform.position = vector + vector3.normalized * num2;
		if (Camera.main.transform.position.y < vector.y)
		{
			Vector3 position = Camera.main.transform.position;
			position.y = vector.y;
			Camera.main.transform.position = position;
		}
		Camera.main.transform.LookAt(vector, Vector3.up);
		if (OnFocused != null)
		{
			OnFocused(vector, num2);
		}
	}

	public void Delete()
	{
		if (m_selectedObject != null)
		{
			ReactivateSnapPoints(m_selectedObject.UID);
			UnityEngine.Object.Destroy(m_selectedObject.gameObject);
			m_selectedObject.name = "deleted";
			m_isObjectPlaceable = IsObjectPlaceable();
			LE_EventInterface.LevelDataChanged(m_selectedObject.gameObject);
		}
	}

	public void RemoveSelection()
	{
		SelectObject(null);
	}

	public void CloneObject()
	{
		if (m_selectedObject != null && IsObjectPlaceable(m_selectedObject))
		{
			m_selectedObject.EditMode = LE_EObjectEditMode.NO_EDIT;
			m_selectedObject.IsSelected = false;
			_ = m_selectedObject.name;
			PlaceObject(m_selectedObject, m_selectedObject.transform, p_isDestroyClonedScripts: true, clonedOffset: true);
		}
	}

	public bool IsObjectPlaceable(LE_Object p_object)
	{
		if (p_object.MaxInstancesInLevel != 0)
		{
			int num = 0;
			LE_Object[] array = UnityEngine.Object.FindObjectsOfType<LE_Object>();
			for (int i = 0; i < array.Length; i++)
			{
				if ((!(m_previewInstance != null) || !(m_previewInstance == array[i])) && array[i].Id == p_object.Id)
				{
					num++;
					if (p_object.MaxInstancesInLevel <= num)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public void AddSnappingScripts(LE_Object p_newObject)
	{
		if (p_newObject != null)
		{
			if (p_newObject.SnapType == LE_Object.ESnapType.SNAP_TO_OBJECT)
			{
				AddObjectSnapping(p_newObject);
			}
			else if (p_newObject.SnapType == LE_Object.ESnapType.SNAP_TO_TERRAIN)
			{
				AddTerrainSnapping(p_newObject);
			}
			else if (p_newObject.SnapType == LE_Object.ESnapType.SNAP_TO_3D_GRID || p_newObject.SnapType == LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN)
			{
				AddGridSnapping(p_newObject, p_isPreview: false);
			}
		}
		else
		{
			Debug.LogError("LE_GUI3dObject: AddSnappingScripts: passed object is null!");
		}
	}

	public void SetSnapPointUIDsToObjUIDs(Dictionary<string, int> p_snapPointUIDsToObjUIDs)
	{
		foreach (KeyValuePair<string, int> p_snapPointUIDsToObjUID in p_snapPointUIDsToObjUIDs)
		{
			m_snapPointUIDsToObjUIDs.Add(p_snapPointUIDsToObjUID.Key, p_snapPointUIDsToObjUID.Value);
		}
	}

	public void ClearLevelData()
	{
		m_snapPointUIDsToSnapPoints.Clear();
		m_snapPointUIDsToObjUIDs.Clear();
		m_isSnapToObjectActive = true;
	}

	private void Start()
	{
		if (LE_GUIInterface.Instance.delegates.IsObjectDragged == null)
		{
			Debug.LogError("LE_GUI3dObject: you must provide the LE_GUIInterface.delegates.IsObjectDragged to make the object drag&drop working!");
		}
		if (LE_GUIInterface.Instance.delegates.SetDraggableObjectMessage == null)
		{
			Debug.LogWarning("LE_GUI3dObject: LE_GUIInterface.delegates.SetDraggableObjectMessage is not set. Set it if you want to visualize the 'max. # reached!' message or messages generated via LE_ObjectDragEvent.");
		}
		if (LE_GUIInterface.Instance.delegates.SetDraggableObjectState == null)
		{
			Debug.LogWarning("LE_GUI3dObject: LE_GUIInterface.delegates.SetDraggableObjectState is not set. Set it if you want to hide your UI when a 3d preview is shown or show different UI if the object is not placeable.");
		}
		S_SnapToObject.OnNewObjectSnapped = (EventHandler<S_SnapToObjectEventArgs>)Delegate.Combine(S_SnapToObject.OnNewObjectSnapped, new EventHandler<S_SnapToObjectEventArgs>(OnNewObjectSnapped));
		S_SnapToObject.OnPreviewObjectInstantiated = (EventHandler<S_SnapToObjectEventArgs>)Delegate.Combine(S_SnapToObject.OnPreviewObjectInstantiated, new EventHandler<S_SnapToObjectEventArgs>(OnPreviewObjectInstantiated));
	}

	private void OnDestroy()
	{
		S_SnapToObject.OnNewObjectSnapped = (EventHandler<S_SnapToObjectEventArgs>)Delegate.Remove(S_SnapToObject.OnNewObjectSnapped, new EventHandler<S_SnapToObjectEventArgs>(OnNewObjectSnapped));
		S_SnapToObject.OnPreviewObjectInstantiated = (EventHandler<S_SnapToObjectEventArgs>)Delegate.Remove(S_SnapToObject.OnPreviewObjectInstantiated, new EventHandler<S_SnapToObjectEventArgs>(OnPreviewObjectInstantiated));
	}

	private void OnNewObjectPlaced(LE_Object p_newInstance)
	{
		AddSnappingScripts(p_newInstance);
		SelectObject(p_newInstance);
		m_isObjectPlaceable = IsObjectPlaceable();
		LE_EventInterface.LevelDataChanged(p_newInstance);
		if (LE_EventInterface.OnObjectPlaced != null)
		{
			LE_EventInterface.OnObjectPlaced(this, new LE_ObjectPlacedEvent(p_newInstance));
		}
	}

	private void OnPreviewObjectInstantiated(object p_sender, S_SnapToObjectEventArgs p_args)
	{
		LE_Object component = p_args.NewInstance.GetComponent<LE_Object>();
		if (component != null)
		{
			UnityEngine.Object.Destroy(component);
		}
	}

	private void OnNewObjectSnapped(object p_sender, S_SnapToObjectEventArgs p_args)
	{
		LE_Object component = p_args.NewInstance.GetComponent<LE_Object>();
		if (component != null)
		{
			LE_Object componentInParent = p_args.Source.GetComponentInParent<LE_Object>();
			LE_Object component2 = p_args.NewInstance.GetComponent<LE_Object>();
			MarkSnapPointAsUsed(componentInParent, component2, p_args.Source);
			if (component2.RootSnapPointIndex != -1)
			{
				MarkSnapPointAsUsed(component2, componentInParent, component2.RootSnapPointIndex);
			}
			OnNewObjectPlaced(component);
		}
		else
		{
			Debug.LogError("LE_GUI3dObject: OnNewObjectSnapped: LE_Object is not attached to the root of the new object! This object will not be saved if it has no LE_Object attached!");
		}
	}

	private void MarkSnapPointAsUsed(LE_Object p_sourceObj, LE_Object p_destinationObj, S_SnapToObject p_snapScript)
	{
		if (p_sourceObj != null)
		{
			int snapPointIndex = -1;
			for (int i = 0; i < p_sourceObj.ObjectSnapPoints.Length; i++)
			{
				if (p_sourceObj.ObjectSnapPoints[i].SnapSystemInstance == p_snapScript)
				{
					snapPointIndex = i;
					break;
				}
			}
			MarkSnapPointAsUsed(p_sourceObj, p_destinationObj, snapPointIndex);
		}
		else
		{
			Debug.LogError("LE_GUI3dObject: MarkSnapPointAsUsed: could not find LE_Object of snap source!");
		}
	}

	private void MarkSnapPointAsUsed(LE_Object p_sourceObj, LE_Object p_destinationObj, int snapPointIndex)
	{
		if (p_sourceObj != null)
		{
			if (snapPointIndex != -1)
			{
				string text = p_sourceObj.UID + "_" + snapPointIndex;
				if (!m_snapPointUIDsToObjUIDs.ContainsKey(text))
				{
					m_snapPointUIDsToObjUIDs.Add(text, p_destinationObj.UID);
					return;
				}
				Debug.LogError("LE_GUI3dObject: MarkSnapPointAsUsed: duplicate snapping on snapPointUID(" + text + ") new (ignored) objID(" + p_destinationObj.UID + ") and old objID(" + m_snapPointUIDsToObjUIDs[text] + ")");
			}
			else
			{
				Debug.LogError("LE_GUI3dObject: MarkSnapPointAsUsed: could not find LE_ObjectSnapPoint of snap source!");
			}
		}
		else
		{
			Debug.LogError("LE_GUI3dObject: MarkSnapPointAsUsed: could not find LE_Object of snap source!");
		}
	}

	private bool IsObjectPlaceable()
	{
		if (m_object != null)
		{
			if (!IsObjectPlaceable(m_object))
			{
				m_dragMessage = "MAP_EDITOR_MAX_NUM_OBJECTS";
				SetDragMessageInUI();
				return false;
			}
			m_dragMessage = "";
			SetDragMessageInUI();
			return true;
		}
		return false;
	}

	private void AddObjectSnapping(LE_Object p_newObject)
	{
		for (int i = 0; i < p_newObject.ObjectSnapPoints.Length; i++)
		{
			if (p_newObject.ObjectSnapPoints[i] != null)
			{
				Material p_materialLine = null;
				Material p_materialFill = null;
				if (p_newObject.IsDrawSnapToObjectUI)
				{
					p_materialLine = (Material)Resources.Load("SnapToObjectUIMaterial_Line");
					p_materialFill = (Material)Resources.Load("SnapToObjectUIMaterial_Fill");
				}
				S_SnapToObject s_SnapToObject = p_newObject.ObjectSnapPoints[i].InstatiateSnapSystem((GameObject)UnityEngine.Object.Instantiate(Resources.Load("ObjectSnapButtonVisuals")), p_newObject.IsDrawSnapToObjectUI, p_materialLine, p_materialFill);
				if (s_SnapToObject != null)
				{
					string text = p_newObject.UID + "_" + i;
					if (!m_snapPointUIDsToSnapPoints.ContainsKey(text))
					{
						m_snapPointUIDsToSnapPoints.Add(text, s_SnapToObject);
					}
					else
					{
						Debug.LogError("LE_GUI3dObject: AddObjectSnapping: a snap point instance of with the UID '" + text + "' already exists!");
					}
					if (!m_isSnapToObjectActive)
					{
						s_SnapToObject.gameObject.SetActive(value: false);
					}
					if (m_snapPointUIDsToObjUIDs.ContainsKey(text))
					{
						s_SnapToObject.IncSnapCounter();
					}
				}
			}
			else
			{
				Debug.LogError("LE_GUI3dObject: AddObjectSnapping: object '" + p_newObject.name + "' has a nullpointer in the ObjectSnapPoints array at index '" + i + "'!");
			}
		}
	}

	private void AddTerrainSnapping(LE_Object p_newObject)
	{
		S_SnapToWorld worldSnap = p_newObject.gameObject.AddComponent<S_SnapToWorld>();
		worldSnap.SnapToLayers = 1 << TERRAIN_LAYER;
		worldSnap.SnapFrameRate = -1;
		worldSnap.IsRotationSnap = p_newObject.IsPlacementRotationByNormal;
		worldSnap.DoSnap();
		EventHandler snapUpdateFunct = (object p_object, EventArgs p_args) =>
		{
			if (p_object is LE_TerrainManager || (p_object is LE_ObjectEditHandle && ((LE_ObjectEditHandle)p_object).transform.IsChildOf(p_newObject.transform)))
			{
				worldSnap.DoSnap();
			}
		};
		LE_EventInterface.OnChangeLevelData = (EventHandler)Delegate.Combine(LE_EventInterface.OnChangeLevelData, snapUpdateFunct);
		p_newObject.gameObject.AddComponent<UtilityOnDestroyHandler>().m_onDestroy = () =>
		{
			LE_EventInterface.OnChangeLevelData = (EventHandler)Delegate.Remove(LE_EventInterface.OnChangeLevelData, snapUpdateFunct);
		};
	}

	private void AddGridSnapping(LE_Object p_newObject, bool p_isPreview)
	{
		S_SnapToGrid s_SnapToGrid = p_newObject.gameObject.AddComponent<S_SnapToGrid>();
		s_SnapToGrid.GridOffset = p_newObject.SnapGridOffset;
		s_SnapToGrid.GridCellSize = p_newObject.SnapGridCellSize;
		s_SnapToGrid.SnapCondition = ((!p_isPreview) ? S_SnapToGrid.ESnapCondition.WHEN_STILL : S_SnapToGrid.ESnapCondition.ON_UPDATE);
		s_SnapToGrid.IsInstantSnap = p_isPreview;
		if (p_newObject.SnapType == LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN)
		{
			s_SnapToGrid.IsSnapAxisY = false;
			if (!p_isPreview)
			{
				AddTerrainSnapping(p_newObject);
			}
		}
	}

	private void Update()
	{
		UpdateObjectSelection();
		UpdateNewObjectDragAndDrop();
		UpdateSnapToObjectInstances();
		Rewired.Player player = ReInput.players.GetPlayer(0);
		if (m_isKeyComboFocus && player.GetButtonDown(53))
		{
			Focus();
		}
		m_isCursorActionInThisFrame = false;
	}

	private void UpdateObjectSelection()
	{
		bool isCursorOverSomething = IsCursorOverSomething;
		if (m_previewInstance == null && !IsObjectDraggedInUI())
		{
			if (!IsInteractable)
			{
				m_isCursorAction = isCursorOverSomething;
				m_isSelectionPossible = false;
			}
			else if (!m_isCursorAction && m_isCursorActionInThisFrame)
			{
				if (isCursorOverSomething)
				{
					m_cursorActionOnObject = m_cursorHitInfo.collider.GetComponentInParent<LE_Object>();
				}
				else
				{
					m_cursorActionOnObject = null;
				}
				m_lastCursorActionStartPos = m_cursorScreenCoords;
				m_isSelectionPossible = true;
				m_isCursorAction = true;
			}
			else if (m_isCursorAction && m_isCursorActionInThisFrame)
			{
				if ((!isCursorOverSomething && m_cursorActionOnObject != null) || (isCursorOverSomething && m_cursorActionOnObject != m_cursorHitInfo.collider.GetComponentInParent<LE_Object>()) || (double)(m_lastCursorActionStartPos - m_cursorScreenCoords).magnitude > (double)Screen.height * 0.025)
				{
					m_isSelectionPossible = false;
					m_cursorActionOnObject = null;
				}
			}
			else if (m_isCursorAction && !m_isCursorActionInThisFrame)
			{
				if (m_isSelectionPossible)
				{
					if (m_cursorActionOnObject == null)
					{
						m_cursorActionOnObject = TrySelectByOversizedBoundingVolume();
					}
					SelectObject(m_cursorActionOnObject);
				}
				m_isSelectionPossible = false;
				m_cursorActionOnObject = null;
				m_isCursorAction = false;
			}
			else
			{
				m_cursorActionOnObject = null;
				m_isCursorAction = false;
			}
		}
		if (m_selectedObject != null)
		{
			m_selectedObject.EditMode = m_objectEditMode;
		}
	}

	private void UpdateNewObjectDragAndDrop()
	{
		if (!(m_object != null))
		{
			return;
		}
		SetDragMessageInUI();
		if (!IsInteractable)
		{
			if (m_previewInstance != null)
			{
				UnityEngine.Object.Destroy(m_previewInstance.gameObject);
			}
		}
		else if (IsCursorOverSomething && m_cursorHitInfo.point.x < 150f && m_cursorHitInfo.point.x > -150f && m_cursorHitInfo.point.z < 150f && m_cursorHitInfo.point.z > -150f)
		{
			if (IsObjectDraggedInUI())
			{
				if (OnObjectDrag() && ((m_object.SnapType != LE_Object.ESnapType.SNAP_TO_TERRAIN && m_object.SnapType != LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN) || (m_cursorHitInfo.collider != null && m_cursorHitInfo.collider.gameObject.layer == TERRAIN_LAYER)))
				{
					if (m_previewInstance == null)
					{
						m_previewInstance = UnityEngine.Object.Instantiate(m_object);
						m_previewInstance.gameObject.GetComponent<Transform>().parent = Singleton<LE_LevelEditorMain>.Instance.ObjectDumpContainer;
						MoveToLayer(m_previewInstance.transform, LayerMask.NameToLayer("Ignore Raycast"));
						Rigidbody[] componentsInChildren = m_previewInstance.GetComponentsInChildren<Rigidbody>();
						for (int i = 0; i < componentsInChildren.Length; i++)
						{
							UnityEngine.Object.Destroy(componentsInChildren[i]);
						}
						if (m_previewInstance.SnapType == LE_Object.ESnapType.SNAP_TO_3D_GRID || m_previewInstance.SnapType == LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN)
						{
							AddGridSnapping(m_previewInstance, p_isPreview: true);
						}
					}
					m_previewInstance.transform.position = m_cursorHitInfo.point;
					if (m_previewInstance.SnapType == LE_Object.ESnapType.SNAP_TO_3D_GRID || m_previewInstance.SnapType == LE_Object.ESnapType.SNAP_TO_2D_GRID_AND_TERRAIN)
					{
						m_previewInstance.transform.position += m_cursorHitInfo.normal * 0.005f;
					}
					if (m_previewInstance.IsPlacementRotationByNormal)
					{
						m_previewInstance.transform.up = m_cursorHitInfo.normal;
					}
				}
			}
			else if (m_isObjectPlaceable)
			{
				PlaceObject();
			}
			else if (m_previewInstance != null)
			{
				UnityEngine.Object.Destroy(m_previewInstance.gameObject);
			}
		}
		else if (m_previewInstance != null)
		{
			UnityEngine.Object.Destroy(m_previewInstance.gameObject);
		}
		if (LE_GUIInterface.Instance.delegates.SetDraggableObjectState != null)
		{
			if (!IsInteractable || !IsObjectDraggedInUI())
			{
				LE_GUIInterface.Instance.delegates.SetDraggableObjectState(LE_GUIInterface.Delegates.EDraggedObjectState.NONE);
			}
			else if (m_previewInstance != null)
			{
				LE_GUIInterface.Instance.delegates.SetDraggableObjectState(LE_GUIInterface.Delegates.EDraggedObjectState.IN_3D_PREVIEW);
			}
			else
			{
				LE_GUIInterface.Instance.delegates.SetDraggableObjectState(LE_GUIInterface.Delegates.EDraggedObjectState.NOT_PLACEABLE);
			}
		}
	}

	private void UpdateSnapToObjectInstances()
	{
		bool flag = !m_isSnapToObjectActive && IsInteractable;
		if (!flag && (!m_isSnapToObjectActive || IsInteractable))
		{
			return;
		}
		m_isSnapToObjectActive = flag;
		List<string> list = new List<string>();
		foreach (string key in m_snapPointUIDsToSnapPoints.Keys)
		{
			if (m_snapPointUIDsToSnapPoints[key] != null)
			{
				if (!flag || m_snapPointUIDsToSnapPoints[key].SnapCounter == 0)
				{
					m_snapPointUIDsToSnapPoints[key].gameObject.SetActive(flag);
				}
			}
			else
			{
				list.Add(key);
			}
		}
		foreach (string item in list)
		{
			m_snapPointUIDsToSnapPoints.Remove(item);
		}
	}

	private void ReactivateSnapPoints(int p_deletedObjectUID)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> snapPointUIDsToObjUID in m_snapPointUIDsToObjUIDs)
		{
			if (snapPointUIDsToObjUID.Value == p_deletedObjectUID)
			{
				list.Add(snapPointUIDsToObjUID.Key);
				if (m_snapPointUIDsToSnapPoints.ContainsKey(snapPointUIDsToObjUID.Key))
				{
					m_snapPointUIDsToSnapPoints[snapPointUIDsToObjUID.Key].DecSnapCounter();
				}
			}
		}
		foreach (string item in list)
		{
			m_snapPointUIDsToObjUIDs.Remove(item);
		}
	}

	private LE_Object TrySelectByOversizedBoundingVolume()
	{
		List<LE_Object> list = new List<LE_Object>();
		List<float> list2 = new List<float>();
		LE_Object[] array = UnityEngine.Object.FindObjectsOfType<LE_Object>();
		for (int i = 0; i < array.Length; i++)
		{
			if (GetMinMaxBounds(array[i], out var o_min, out var o_max))
			{
				float num = 1f;
				float magnitude = (array[i].transform.position - Camera.main.transform.position).magnitude;
				Vector3 vector = new Vector3
				{
					x = Mathf.Abs(o_max.x - o_min.x),
					y = Mathf.Abs(o_max.y - o_min.y),
					z = Mathf.Abs(o_max.z - o_min.z)
				};
				float magnitude2 = vector.magnitude;
				Vector3 rhs = Vector3.one * 10f + vector;
				if (magnitude > 50f)
				{
					num = 2.25f;
					vector = Vector3.Max(vector, Vector3.one * 1.5f);
				}
				else if (magnitude > 25f)
				{
					num = 1.85f;
					vector = Vector3.Max(vector, Vector3.one);
				}
				else if (magnitude > 15f)
				{
					num = 1.5f;
					vector = Vector3.Max(vector, Vector3.one * 0.5f);
				}
				vector = Vector3.Min(vector * num, rhs);
				if (new Bounds((o_max + o_min) * 0.5f, vector).IntersectRay(m_cursorRay))
				{
					list.Add(array[i]);
					list2.Add(magnitude2);
				}
			}
		}
		if (list.Count > 0)
		{
			LE_Object result = list[0];
			float num2 = list2[0];
			for (int j = 1; j < list.Count; j++)
			{
				if (list2[j] < num2)
				{
					result = list[j];
					num2 = list2[j];
				}
			}
			float num3 = 9999999f;
			for (int k = 0; k < list.Count; k++)
			{
				if (list2[k] <= num2 * 1.25f)
				{
					float magnitude3 = Vector3.Cross(m_cursorRay.direction, list[k].transform.position - m_cursorRay.origin).magnitude;
					if (num3 > magnitude3)
					{
						num3 = magnitude3;
						result = list[k];
					}
				}
			}
			return result;
		}
		return null;
	}

	private bool GetMinMaxBounds(LE_Object p_object, out Vector3 o_min, out Vector3 o_max)
	{
		o_min = Vector3.one * 9999999f;
		o_max = -Vector3.one * 9999999f;
		Collider[] componentsInChildren = p_object.GetComponentsInChildren<Collider>();
		if (componentsInChildren.Length != 0)
		{
			bool result = false;
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].GetComponent<LE_ObjectEditHandleCollider>() == null)
				{
					result = true;
					Bounds bounds = componentsInChildren[i].bounds;
					o_min = Vector3.Min(o_min, bounds.min);
					o_max = Vector3.Max(o_max, bounds.max);
				}
			}
			return result;
		}
		return false;
	}

	private void PlaceObject()
	{
		if (m_previewInstance != null)
		{
			UnityEngine.Object.Destroy(m_previewInstance.gameObject);
			PlaceObject(m_object, m_previewInstance.transform, p_isDestroyClonedScripts: false);
		}
	}

	private LE_Object PlaceObject(LE_Object p_prefab, Transform p_copyTransform, bool p_isDestroyClonedScripts, bool clonedOffset = false)
	{
		Vector3 position = p_copyTransform.position;
		position.x++;
		if (position.XZSqr() > 15625f)
		{
			return null;
		}
		LE_Object lE_Object = UnityEngine.Object.Instantiate(p_prefab);
		lE_Object.gameObject.name = p_prefab.name;
		lE_Object.gameObject.GetComponent<Transform>().parent = Singleton<LE_LevelEditorMain>.Instance.ObjectDumpContainer;
		if (clonedOffset)
		{
			lE_Object.transform.position = p_copyTransform.position + new Vector3(1f, 0f, 0f);
		}
		else
		{
			lE_Object.transform.position = p_copyTransform.position;
		}
		lE_Object.transform.rotation = p_copyTransform.rotation;
		lE_Object.transform.localScale = p_copyTransform.localScale;
		if (p_isDestroyClonedScripts)
		{
			LE_ObjectEditHandle componentInChildren = lE_Object.GetComponentInChildren<LE_ObjectEditHandle>();
			if (componentInChildren != null)
			{
				UnityEngine.Object.Destroy(componentInChildren.gameObject);
			}
			S_SnapToWorld component = lE_Object.GetComponent<S_SnapToWorld>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			S_SnapToGrid component2 = lE_Object.GetComponent<S_SnapToGrid>();
			if (component2 != null)
			{
				UnityEngine.Object.Destroy(component2);
			}
			S_SnapToObject[] componentsInChildren = lE_Object.GetComponentsInChildren<S_SnapToObject>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				LE_ObjectSnapPoint.DestroySnapSystem(componentsInChildren[i]);
			}
			UtilityOnDestroyHandler component3 = lE_Object.GetComponent<UtilityOnDestroyHandler>();
			if (component3 != null)
			{
				component3.DestroyWithoutHandling();
			}
		}
		OnNewObjectPlaced(lE_Object);
		return lE_Object;
	}

	private void MoveToLayer(Transform root, int layer)
	{
		root.gameObject.layer = layer;
		foreach (Transform item in root)
		{
			MoveToLayer(item, layer);
		}
	}

	private void SelectObject(LE_Object p_object)
	{
		if (m_selectedObject != null && m_selectedObject != p_object)
		{
			m_selectedObject.EditMode = LE_EObjectEditMode.NO_EDIT;
			m_selectedObject.IsSelected = false;
		}
		m_selectedObject = p_object;
		if (m_selectedObject != null)
		{
			m_selectedObject.IsSelected = true;
		}
	}

	private bool OnObjectDrag()
	{
		bool isObjectPlaceable = m_isObjectPlaceable;
		if (LE_EventInterface.OnObjectDragged != null)
		{
			LE_ObjectDragEvent lE_ObjectDragEvent = new LE_ObjectDragEvent(m_object, m_previewInstance, m_isObjectPlaceable, m_dragMessage, m_cursorHitInfo);
			LE_EventInterface.OnObjectDragged(this, lE_ObjectDragEvent);
			isObjectPlaceable = lE_ObjectDragEvent.IsObjectPlaceable;
			SetDragMessageInUI(lE_ObjectDragEvent.Message);
		}
		if (!isObjectPlaceable && m_previewInstance != null)
		{
			UnityEngine.Object.Destroy(m_previewInstance.gameObject);
		}
		return isObjectPlaceable;
	}

	private void SetDragMessageInUI()
	{
		SetDragMessageInUI(m_dragMessage);
	}

	private void SetDragMessageInUI(string p_message)
	{
		if (LE_GUIInterface.Instance.delegates.SetDraggableObjectMessage != null)
		{
			LE_GUIInterface.Instance.delegates.SetDraggableObjectMessage(m_dragMessage);
		}
	}

	private bool IsObjectDraggedInUI()
	{
		if (LE_GUIInterface.Instance.delegates.IsObjectDragged != null)
		{
			return LE_GUIInterface.Instance.delegates.IsObjectDragged();
		}
		return false;
	}
}
