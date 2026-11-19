using UnityEngine;

namespace LE_LevelEditor.Core;

public class LE_ObjectEditHandleCollider : MonoBehaviour
{
	[SerializeField]
	private LE_ObjectEditHandle m_parent;

	[SerializeField]
	private Vector3 m_axis;

	[SerializeField]
	private bool m_isRotationHandle;

	[SerializeField]
	private Vector3 m_rotationHandleDir;

	private Renderer m_renderer;

	private Collider[] m_colliders;

	private Color m_originalColor;

	private bool m_isDrag;

	private float m_lastCamDist = -1f;

	private Vector3 m_lastParentScale = Vector3.one;

	private Vector3 m_lastParentParentScale = Vector3.one;

	private bool m_isMouseDown;

	private bool m_isMouseDownOnMe;

	private Camera m_cam;

	public Vector3 Axis
	{
		get
		{
			return m_axis;
		}
		set
		{
			m_axis = value;
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

	private void Start()
	{
		m_colliders = GetComponentsInChildren<Collider>();
		m_renderer = GetComponent<Renderer>();
		m_originalColor = m_renderer.material.color;
	}

	private void Update()
	{
		bool flag = (Input.GetMouseButton(0) && Input.touchCount < 2) || Input.touchCount == 1;
		if (!m_isMouseDown && !flag)
		{
			return;
		}
		if (m_isMouseDownOnMe && !flag)
		{
			OnMyMouseUp();
		}
		else if (m_isMouseDownOnMe & flag)
		{
			OnMyMouseDrag();
		}
		else
		{
			Camera cam = Cam;
			if (m_colliders != null && cam != null)
			{
				for (int i = 0; i < m_colliders.Length; i++)
				{
					Collider collider = m_colliders[i];
					if (((collider != null && !m_isMouseDown) & flag) && !m_parent.IsDrag && (collider.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hitInfo, float.PositiveInfinity) || (Input.touchCount == 1 && collider.Raycast(cam.ScreenPointToRay(Input.GetTouch(0).position), out hitInfo, float.PositiveInfinity))))
					{
						m_isMouseDownOnMe = true;
						OnMyMouseDown();
						break;
					}
				}
			}
		}
		m_isMouseDown = flag;
		if (!m_isMouseDown)
		{
			m_isMouseDownOnMe = false;
		}
	}

	private void LateUpdate()
	{
		Camera cam = Cam;
		if (!(cam != null))
		{
			return;
		}
		if (!m_isDrag && m_isRotationHandle)
		{
			Vector3 vector = transform.parent.InverseTransformPoint(cam.transform.position);
			Vector3 rhs = vector - Vector3.Dot(m_axis, vector) * m_axis;
			rhs.Normalize();
			float num = Vector3.Dot(m_rotationHandleDir, rhs);
			Vector3 euler = Vector3.Cross(m_rotationHandleDir, rhs);
			euler.Scale(m_axis);
			float num2 = 57.29578f * Mathf.Asin(euler.magnitude);
			euler.Normalize();
			if (num < 0f)
			{
				num2 = Mathf.Abs(-180f + num2);
			}
			euler *= num2;
			transform.localRotation = Quaternion.Euler(euler);
		}
		float magnitude = (cam.transform.position - transform.position).magnitude;
		if (!m_isDrag && (cam.orthographic || Mathf.Abs(magnitude - m_lastCamDist) > 0.0001f || m_lastParentScale != m_parent.transform.localScale || m_lastParentParentScale != m_parent.transform.parent.localScale))
		{
			float num3 = ((!cam.orthographic) ? (magnitude / 75f) : (cam.orthographicSize / 50f));
			transform.localScale = new Vector3(num3 / (m_parent.transform.parent.localScale.x * m_parent.transform.localScale.x), num3 / (m_parent.transform.parent.localScale.y * m_parent.transform.localScale.y), num3 / (m_parent.transform.parent.localScale.z * m_parent.transform.localScale.z));
			m_lastCamDist = magnitude;
			m_lastParentScale = m_parent.transform.localScale;
			m_lastParentParentScale = m_parent.transform.parent.localScale;
		}
	}

	private void OnMyMouseDown()
	{
		m_parent.OnMyMouseDrag(m_axis);
		m_renderer.material.color = Color.yellow;
		m_isDrag = true;
	}

	private void OnMyMouseDrag()
	{
		m_parent.OnMyMouseDrag(m_axis);
		m_isDrag = true;
	}

	private void OnMyMouseUp()
	{
		m_renderer.material.color = m_originalColor;
		m_parent.transform.localScale = new Vector3(1f / m_parent.transform.parent.localScale.x, 1f / m_parent.transform.parent.localScale.y, 1f / m_parent.transform.parent.localScale.z);
		m_isDrag = false;
	}
}
