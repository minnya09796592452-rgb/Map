using System;
using Beam.Serialization;
using MyUtility;
using UnityEngine;

namespace S_SnapTools;

public class S_SnapToObject : MonoBehaviour
{
	private enum EAnimationState
	{
		ZOOM_IN,
		ZOOM_OUT,
		IDLE
	}

	public static EventHandler<S_SnapToObjectEventArgs> OnNewObjectSnapped;

	public static EventHandler<S_SnapToObjectEventArgs> OnPreviewObjectInstantiated;

	private static S_SnapToObject s_openSelection;

	private const float ZOOM_OUT_TIME = 0.4f;

	private const float ZOOM_IN_TIME = 0.2f;

	private readonly Vector3 MIN_SCALE = Vector3.one * 0.0001f;

	[SerializeField]
	private float PREVIEW_RELATIVE_SCREEN_DISTANCE = 0.3f;

	[SerializeField]
	private float PREVIEW_RELATIVE_SCREEN_CLICK_AREA = 0.2f;

	[SerializeField]
	private float PREVIEW_RELATIVE_MOVE_TO_CAMERA_DISTANCE = 0.5f;

	[SerializeField]
	private S_SnapToObjectPrefab[] m_prefabs = new S_SnapToObjectPrefab[1];

	[SerializeField]
	private bool m_isDeactivatedAfterSnap;

	[SerializeField]
	private bool m_isDestroyedAfterSnap = true;

	[SerializeField]
	private bool m_isSetNameToResourcePath = true;

	[SerializeField]
	private bool m_isDrawUI = true;

	[SerializeField]
	private Material m_uiMaterialLine;

	[SerializeField]
	private Material m_uiMaterialFill;

	private int m_snapCounter;

	private bool m_isSelectionOpen;

	private EAnimationState m_animState = EAnimationState.IDLE;

	private float m_zoomAnimationTime = -1f;

	private Transform m_previewRoot;

	private Camera m_cam;

	public S_SnapToObjectPrefab[] Prefabs
	{
		get
		{
			return m_prefabs;
		}
		set
		{
			m_prefabs = value;
		}
	}

	public bool IsDeactivatedAfterSnap
	{
		get
		{
			return m_isDeactivatedAfterSnap;
		}
		set
		{
			m_isDeactivatedAfterSnap = value;
		}
	}

	public bool IsDestroyedAfterSnap
	{
		get
		{
			return m_isDestroyedAfterSnap;
		}
		set
		{
			m_isDestroyedAfterSnap = value;
		}
	}

	public bool IsSetNameToResourcePath
	{
		get
		{
			return m_isSetNameToResourcePath;
		}
		set
		{
			m_isSetNameToResourcePath = value;
		}
	}

	public bool IsDrawUI
	{
		get
		{
			return m_isDrawUI;
		}
		set
		{
			m_isDrawUI = value;
		}
	}

	public Material UImaterialLine
	{
		get
		{
			return m_uiMaterialLine;
		}
		set
		{
			m_uiMaterialLine = value;
		}
	}

	public Material UImaterialFill
	{
		get
		{
			return m_uiMaterialFill;
		}
		set
		{
			m_uiMaterialFill = value;
		}
	}

	public int SnapCounter => m_snapCounter;

	public bool IsPrefabSelectionOpen => m_isSelectionOpen;

	public Transform PreviewRoot
	{
		get
		{
			if (m_previewRoot == null)
			{
				m_previewRoot = new GameObject("S_SnapToObject Preview Root").transform;
				m_previewRoot.position = transform.position;
			}
			return m_previewRoot;
		}
	}

	private Camera Cam
	{
		get
		{
			if (m_cam == null)
			{
				m_cam = Camera.main;
				if (m_cam == null)
				{
					Debug.LogError("S_SnapToObject: could not find main camera!");
				}
			}
			return m_cam;
		}
	}

	public S_SnapToObject()
	{
		if (m_prefabs != null && m_prefabs.Length == 1)
		{
			m_prefabs[0] = new S_SnapToObjectPrefab();
		}
	}

	public void IncSnapCounter()
	{
		if (m_isDeactivatedAfterSnap)
		{
			gameObject.SetActive(value: false);
		}
		else if (m_isDestroyedAfterSnap)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		m_snapCounter++;
	}

	public void DecSnapCounter()
	{
		if (m_snapCounter > 0)
		{
			m_snapCounter--;
			if (m_isDeactivatedAfterSnap && m_snapCounter == 0)
			{
				gameObject.SetActive(value: true);
			}
		}
	}

	public void OpenSelection()
	{
		if (s_openSelection != null)
		{
			s_openSelection.CloseSelection();
		}
		s_openSelection = this;
		m_isSelectionOpen = true;
		m_animState = EAnimationState.ZOOM_OUT;
		m_zoomAnimationTime = 0f;
		for (int i = 0; i < m_prefabs.Length; i++)
		{
			CreatePrefabPreview(m_prefabs[i]);
		}
	}

	public void CloseSelection()
	{
		if (s_openSelection == this)
		{
			s_openSelection = null;
		}
		m_isSelectionOpen = false;
		m_animState = EAnimationState.ZOOM_IN;
		m_zoomAnimationTime = 0f;
	}

	private void Start()
	{
		UtilityClickTouchDetector utilityClickTouchDetector = gameObject.AddComponent<UtilityClickTouchDetector>();
		utilityClickTouchDetector.m_onClick = (Action)Delegate.Combine(utilityClickTouchDetector.m_onClick, new Action(OnClicked));
	}

	private void Update()
	{
		if (IsPrefabSelectionOpen)
		{
			PreviewRoot.position = transform.position;
		}
		else if (m_previewRoot != null && m_animState == EAnimationState.IDLE)
		{
			UnityEngine.Object.Destroy(m_previewRoot.gameObject);
			m_previewRoot = null;
		}
		if (m_animState == EAnimationState.ZOOM_OUT)
		{
			float p_fade = UpdateAnimationTimeFadeState(0.4f);
			UpdateZoomAnimation(p_fade);
		}
		else if (m_animState == EAnimationState.ZOOM_IN)
		{
			float num = UpdateAnimationTimeFadeState(0.2f);
			if (num < 1f)
			{
				UpdateZoomAnimation(1f - num);
			}
			else
			{
				DestroyAllPreviews();
			}
		}
		else if (IsPrefabSelectionOpen)
		{
			UpdateZoomAnimation(1f);
		}
	}

	private void DestroyAllPreviews()
	{
		for (int i = 0; i < m_prefabs.Length; i++)
		{
			UnityEngine.Object.Destroy(m_prefabs[i].m_currentInstance);
		}
	}

	private float UpdateAnimationTimeFadeState(float p_animTime)
	{
		m_zoomAnimationTime += Time.deltaTime;
		if (m_zoomAnimationTime >= p_animTime)
		{
			m_animState = EAnimationState.IDLE;
			m_zoomAnimationTime = -1f;
			return 1f;
		}
		return m_zoomAnimationTime / p_animTime;
	}

	private void UpdateZoomAnimation(float p_fade)
	{
		if (!(Cam != null))
		{
			return;
		}
		Vector3 vector = Cam.WorldToScreenPoint(transform.position);
		float num = 360f / (float)m_prefabs.Length;
		for (int i = 0; i < m_prefabs.Length; i++)
		{
			GameObject currentInstance = m_prefabs[i].m_currentInstance;
			if (currentInstance != null && m_prefabs[i].m_currentInstancePreviewScript != null)
			{
				Vector3 vector2 = (float)Screen.height * PREVIEW_RELATIVE_SCREEN_DISTANCE * new Vector3(Mathf.Cos(MathF.PI / 180f * num * (float)i * p_fade), Mathf.Sin(MathF.PI / 180f * num * (float)i * p_fade), 0f);
				vector2 = Cam.ScreenToWorldPoint(vector + vector2);
				vector2 = vector2 * (1f - PREVIEW_RELATIVE_MOVE_TO_CAMERA_DISTANCE) + Cam.transform.position * PREVIEW_RELATIVE_MOVE_TO_CAMERA_DISTANCE;
				currentInstance.transform.localScale = Vector3.Lerp(MIN_SCALE, m_prefabs[i].PreviewScale * m_prefabs[i].m_currentInstancePreviewScript.WorldRadius, p_fade);
				currentInstance.transform.position = Vector3.Lerp(transform.position, vector2, p_fade);
				Quaternion rotation = Quaternion.LookRotation(Cam.transform.position - currentInstance.transform.position, Vector3.up);
				rotation *= Quaternion.Euler(m_prefabs[i].PreviewEulerRotation);
				currentInstance.transform.rotation = rotation;
			}
		}
	}

	private void OnClicked()
	{
		if (!m_isSelectionOpen)
		{
			OpenSelection();
		}
		else
		{
			CloseSelection();
		}
	}

	private void OnDisable()
	{
		if (m_previewRoot != null)
		{
			UnityEngine.Object.Destroy(m_previewRoot.gameObject);
			m_previewRoot = null;
		}
		DestroyAllPreviews();
		if (s_openSelection == this)
		{
			s_openSelection = null;
		}
		m_isSelectionOpen = false;
		m_animState = EAnimationState.IDLE;
		m_zoomAnimationTime = -1f;
	}

	private void CreatePrefabPreview(S_SnapToObjectPrefab p_prefab)
	{
		if (p_prefab.m_currentInstance != null)
		{
			Debug.LogWarning("S_SnapToObject: CreatePrefabPreview: there was already an instance of this prefab '" + p_prefab.m_currentInstance.name + "'! Old instance will be destroyed...");
			UnityEngine.Object.Destroy(p_prefab.m_currentInstance);
		}
		if (p_prefab.PrefabResourcePath != null)
		{
			p_prefab.m_currentInstance = UnityEngine.Object.Instantiate(Beam.Serialization.Prefabs.CreatePrefabFromId(p_prefab.PrefabResourcePath.PrefabId).gameObject);
			p_prefab.m_currentInstance.transform.parent = PreviewRoot;
			p_prefab.m_currentInstance.transform.localPosition = Vector3.zero;
			p_prefab.m_currentInstance.transform.localScale = Vector3.one;
			if (p_prefab.IsCollidersDisabledInPreview)
			{
				Collider[] componentsInChildren = p_prefab.m_currentInstance.GetComponentsInChildren<Collider>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = false;
				}
			}
			p_prefab.m_currentInstance.transform.localScale = MIN_SCALE;
			p_prefab.m_currentInstancePreviewScript = p_prefab.m_currentInstance.AddComponent<S_SnapToObjectPreview>();
			p_prefab.m_currentInstancePreviewScript.Init(m_isDrawUI, PREVIEW_RELATIVE_SCREEN_CLICK_AREA, () =>
			{
				PlacePrefab(p_prefab);
			}, m_uiMaterialLine, m_uiMaterialFill);
			if (OnPreviewObjectInstantiated != null)
			{
				OnPreviewObjectInstantiated(this, new S_SnapToObjectEventArgs(this, p_prefab.m_currentInstance));
			}
		}
		else
		{
			Debug.LogError("S_SnapToObject: CreatePrefabPreview: no prefab assigned!");
		}
	}

	private void PlacePrefab(S_SnapToObjectPrefab p_prefab)
	{
		if (m_isSelectionOpen)
		{
			DestroyAllPreviews();
			if (s_openSelection == this)
			{
				s_openSelection = null;
			}
			m_isSelectionOpen = false;
			m_animState = EAnimationState.IDLE;
			m_zoomAnimationTime = -1f;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(Beam.Serialization.Prefabs.CreatePrefabFromId(p_prefab.PrefabResourcePath.PrefabId).gameObject);
		gameObject.transform.localRotation = transform.rotation;
		gameObject.transform.Rotate(p_prefab.LocalEulerRotation);
		gameObject.transform.localScale = Vector3.Scale(p_prefab.LocalScale, transform.lossyScale);
		gameObject.transform.position = transform.position + gameObject.transform.TransformPoint(p_prefab.LocalPosition);
		IncSnapCounter();
		if (OnNewObjectSnapped != null)
		{
			OnNewObjectSnapped(this, new S_SnapToObjectEventArgs(this, gameObject));
		}
	}
}
