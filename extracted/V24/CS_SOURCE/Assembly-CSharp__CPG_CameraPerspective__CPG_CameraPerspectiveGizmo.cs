using System;
using UnityEngine;

namespace CPG_CameraPerspective;

public class CPG_CameraPerspectiveGizmo : MonoBehaviour
{
	public enum EButtonTypes
	{
		TOGGLE_ORTHO,
		X_POS,
		X_NEG,
		Y_POS,
		Y_NEG,
		Z_POS,
		Z_NEG
	}

	[SerializeField]
	private Vector2 m_relativeScreenPos = Vector2.one * 0.85f;

	[SerializeField]
	private float m_relativeScreenSize = 0.1f;

	[SerializeField]
	private int m_layerToUse = 31;

	[SerializeField]
	private Vector3 m_pivot = Vector3.zero;

	[SerializeField]
	private float m_minDistToPivot = 15f;

	[SerializeField]
	private float m_orthoOffset;

	private Camera m_targetCamera;

	private Camera m_ownCamera;

	public EventHandler m_onBeforeSwitchToOrthographic;

	public EventHandler m_onAfterSwitchToOrthographic;

	public EventHandler m_onBeforeSwitchToPerspective;

	public EventHandler m_onAfterSwitchToPerspective;

	public Vector2 RelativeScreenPos
	{
		get
		{
			return m_relativeScreenPos;
		}
		set
		{
			m_relativeScreenPos = value;
		}
	}

	public float RelativeScreenSize
	{
		get
		{
			return m_relativeScreenSize;
		}
		set
		{
			m_relativeScreenSize = value;
		}
	}

	public int LayerToUse
	{
		get
		{
			return m_layerToUse;
		}
		set
		{
			m_layerToUse = value;
		}
	}

	public Vector3 Pivot
	{
		get
		{
			return m_pivot;
		}
		set
		{
			m_pivot = value;
		}
	}

	public float MinDistToPivot
	{
		get
		{
			return m_minDistToPivot;
		}
		set
		{
			m_minDistToPivot = value;
		}
	}

	public float OrthoOffset
	{
		get
		{
			return m_orthoOffset;
		}
		set
		{
			m_orthoOffset = value;
		}
	}

	public Camera TargetCamera
	{
		get
		{
			if (m_targetCamera == null)
			{
				m_targetCamera = Camera.main;
			}
			return m_targetCamera;
		}
		set
		{
			m_targetCamera = value;
		}
	}

	public Camera OwnCamera
	{
		get
		{
			if (m_ownCamera == null)
			{
				m_ownCamera = GetComponentInChildren<Camera>();
			}
			return m_ownCamera;
		}
	}

	public static CPG_CameraPerspectiveGizmo Create(Camera p_targetCamera, int p_layerToUse)
	{
		if (p_targetCamera != null)
		{
			UnityEngine.Object obj = Resources.Load("CPG_CameraPerspectiveGizmo");
			if (obj != null)
			{
				CPG_CameraPerspectiveGizmo component = ((GameObject)UnityEngine.Object.Instantiate(obj)).GetComponent<CPG_CameraPerspectiveGizmo>();
				component.TargetCamera = p_targetCamera;
				component.LayerToUse = p_layerToUse;
				return component;
			}
			Debug.LogError("CPG_CameraPerspectiveGizmo: Create: prefab 'CPG_CameraPerspectiveGizmo' not found!");
		}
		else
		{
			Debug.LogError("CPG_CameraPerspectiveGizmo: Create: p_targetCamera is null!");
		}
		return null;
	}

	public void ReportClick(EButtonTypes p_type)
	{
		switch (p_type)
		{
		case EButtonTypes.TOGGLE_ORTHO:
			ToggleOrtho();
			break;
		case EButtonTypes.X_POS:
			ToogleAxis(Vector3.right);
			break;
		case EButtonTypes.X_NEG:
			ToogleAxis(Vector3.left);
			break;
		case EButtonTypes.Y_POS:
			ToogleAxis(Vector3.up);
			break;
		case EButtonTypes.Y_NEG:
			ToogleAxis(Vector3.down);
			break;
		case EButtonTypes.Z_POS:
			ToogleAxis(Vector3.forward);
			break;
		case EButtonTypes.Z_NEG:
			ToogleAxis(Vector3.back);
			break;
		}
	}

	private void ToggleOrtho()
	{
		Camera targetCamera = TargetCamera;
		if (targetCamera != null)
		{
			if (targetCamera.orthographic && m_onBeforeSwitchToPerspective != null)
			{
				m_onBeforeSwitchToPerspective(this, EventArgs.Empty);
			}
			if (!targetCamera.orthographic && m_onBeforeSwitchToOrthographic != null)
			{
				m_onBeforeSwitchToOrthographic(this, EventArgs.Empty);
			}
			if (targetCamera.orthographic)
			{
				targetCamera.transform.position += targetCamera.transform.forward * m_orthoOffset;
			}
			else
			{
				targetCamera.transform.position -= targetCamera.transform.forward * m_orthoOffset;
			}
			targetCamera.orthographic = !targetCamera.orthographic;
			if (targetCamera.orthographic && m_onAfterSwitchToOrthographic != null)
			{
				m_onAfterSwitchToOrthographic(this, EventArgs.Empty);
			}
			if (!targetCamera.orthographic && m_onAfterSwitchToPerspective != null)
			{
				m_onAfterSwitchToPerspective(this, EventArgs.Empty);
			}
		}
	}

	private void ToogleAxis(Vector3 p_axis)
	{
		Camera targetCamera = TargetCamera;
		if (targetCamera != null)
		{
			float num = (targetCamera.transform.position - Pivot).magnitude;
			if (num == 0f)
			{
				num = 1f;
			}
			float num2 = m_minDistToPivot;
			if (targetCamera.orthographic)
			{
				num2 += m_orthoOffset;
			}
			targetCamera.transform.position = Pivot + Mathf.Max(num2, num) * p_axis;
			targetCamera.transform.LookAt(m_pivot);
		}
	}

	private void Start()
	{
		MoveToLayer(transform, m_layerToUse);
		m_ownCamera = GetComponentInChildren<Camera>();
		if (m_ownCamera != null)
		{
			m_ownCamera.aspect = 1f;
			Camera targetCamera = TargetCamera;
			if (targetCamera != null && targetCamera.pixelRect.xMax - targetCamera.pixelRect.xMin > 1f && targetCamera.pixelRect.yMax - targetCamera.pixelRect.yMin > 1f)
			{
				Rect rect = targetCamera.rect;
				float num = m_relativeScreenSize / targetCamera.aspect * rect.width;
				float num2 = m_relativeScreenSize * rect.height;
				Vector2 vector = new Vector2(rect.x, rect.y) + m_relativeScreenPos - new Vector2(1f - rect.xMax, 1f - rect.yMax) - 0.5f * new Vector2(num, num2);
				m_ownCamera.rect = new Rect(vector.x, vector.y, num, num2);
			}
			else
			{
				Debug.LogError("CPG_CameraPerspectiveGizmo: Start: could not find any camera with a non empty view rect! Will destroy self now!");
				UnityEngine.Object.Destroy(gameObject);
			}
			m_ownCamera.cullingMask = 1 << m_layerToUse;
		}
		else
		{
			Debug.LogError("CPG_CameraPerspectiveGizmo: Start: CPG_CameraPerspectiveGizmo prefab seems to be broken it needs a camera in its children!");
		}
	}

	private void Update()
	{
		Camera targetCamera = TargetCamera;
		if (targetCamera != null && m_ownCamera != null)
		{
			m_ownCamera.orthographic = targetCamera.orthographic;
			m_ownCamera.transform.localPosition = -5.2f * targetCamera.transform.forward;
			m_ownCamera.transform.rotation = targetCamera.transform.rotation;
		}
		else
		{
			Debug.LogWarning("CPG_CameraPerspectiveGizmo: Update: lost reference to camera ! Will destroy self now!");
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		m_onBeforeSwitchToOrthographic = null;
		m_onBeforeSwitchToPerspective = null;
		m_onAfterSwitchToOrthographic = null;
		m_onAfterSwitchToPerspective = null;
	}

	private void MoveToLayer(Transform root, int layer)
	{
		root.gameObject.layer = layer;
		foreach (Transform item in root)
		{
			MoveToLayer(item, layer);
		}
	}
}
