using System;
using Funlabs;
using UnityEngine;

namespace LE_LevelEditor.Core;

public class LE_ObjectEditHandle : MonoBehaviour
{
	private const int SKIP_FRAMES = 0;

	private const float MIN_SCALE_VALUE = 0.05f;

	[SerializeField]
	private LE_EObjectEditMode m_editMode = LE_EObjectEditMode.MOVE;

	private Vector3 m_activeEditAxis = Vector3.zero;

	private int m_lastDragFrame = -1;

	private Vector3 m_lastCursorPos = -1f * Vector3.one;

	private int m_dragSkipCounter;

	private Camera m_cam;

	public EventHandler m_onTransformed;

	public LE_EObjectEditMode EditMode => m_editMode;

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

	public bool IsDrag => m_lastDragFrame == Time.frameCount;

	public void OnMyMouseDrag(Vector3 p_axis)
	{
		m_activeEditAxis = p_axis;
		m_lastDragFrame = Time.frameCount;
	}

	public void DisableAxisX()
	{
		DisableAxis(Vector3.right);
	}

	public void DisableAxisY()
	{
		DisableAxis(Vector3.up);
	}

	public void DisableAxisZ()
	{
		DisableAxis(Vector3.forward);
	}

	private void DisableAxis(Vector3 p_axis)
	{
		LE_ObjectEditHandleCollider[] componentsInChildren = GetComponentsInChildren<LE_ObjectEditHandleCollider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].Axis == p_axis)
			{
				UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
			}
			else if (Vector3.Dot(componentsInChildren[i].Axis, p_axis) > 0.0001f)
			{
				Vector3 axis = componentsInChildren[i].Axis;
				axis.Scale(Vector3.one - p_axis);
				componentsInChildren[i].Axis = axis;
			}
		}
	}

	private void LateUpdate()
	{
		if (m_lastDragFrame < Time.frameCount)
		{
			m_activeEditAxis = Vector3.zero;
			m_dragSkipCounter = 0;
		}
		if ((double)m_activeEditAxis.sqrMagnitude > 0.01)
		{
			switch (m_editMode)
			{
			case LE_EObjectEditMode.MOVE:
				Move();
				break;
			case LE_EObjectEditMode.ROTATE:
				Rotate();
				break;
			case LE_EObjectEditMode.SCALE:
				Scale();
				break;
			}
		}
		m_lastCursorPos = GetCursorPosition();
	}

	private void OnDestroy()
	{
		m_onTransformed = null;
	}

	private void Move()
	{
		float editDelta = GetEditDelta();
		if (m_dragSkipCounter == 0)
		{
			float num = 15625f;
			Vector3 vector = transform.TransformDirection(m_activeEditAxis);
			if ((vector * editDelta + transform.position).XZSqr() < num)
			{
				transform.parent.position += vector * editDelta;
			}
			Vector3 position = transform.parent.position;
			position.y = Mathf.Clamp(transform.parent.position.y, -25f, 300f);
			transform.parent.position = position;
			if (m_onTransformed != null)
			{
				m_onTransformed(this, EventArgs.Empty);
			}
		}
		else if (Mathf.Abs(editDelta) > 0.0005f)
		{
			m_dragSkipCounter++;
		}
	}

	private void Scale()
	{
		float editDelta = GetEditDelta();
		if (m_dragSkipCounter == 0)
		{
			transform.parent.localScale += m_activeEditAxis * editDelta;
			transform.parent.localScale = Vector3.Max(Vector3.one * 0.05f, transform.parent.localScale);
			if (m_onTransformed != null)
			{
				m_onTransformed(this, EventArgs.Empty);
			}
		}
		else if (Mathf.Abs(editDelta) > 0.0005f)
		{
			m_dragSkipCounter++;
		}
	}

	private float GetEditDelta()
	{
		Camera cam = Cam;
		if (cam != null)
		{
			Vector3 p_screenAxis = cam.WorldToScreenPoint(transform.position + transform.TransformDirection(m_activeEditAxis)) - cam.WorldToScreenPoint(transform.position);
			p_screenAxis.z *= p_screenAxis.z;
			p_screenAxis.z *= p_screenAxis.z;
			float num = 1f / (1f - Mathf.Clamp(p_screenAxis.z, 0f, 0.8f));
			p_screenAxis.z = 0f;
			p_screenAxis.Normalize();
			if (cam.orthographic)
			{
				return GetEditDeltaOrthogonal(p_screenAxis, num, cam);
			}
			float magnitude = (cam.transform.position - transform.position).magnitude;
			return GetEditDeltaPerspective(p_screenAxis, num * magnitude, cam);
		}
		return 0f;
	}

	private void Rotate()
	{
		float editDeltaRotation = GetEditDeltaRotation();
		if (m_dragSkipCounter == 0)
		{
			transform.parent.Rotate(m_activeEditAxis * editDeltaRotation);
			if (m_onTransformed != null)
			{
				m_onTransformed(this, EventArgs.Empty);
			}
		}
		else if (Mathf.Abs(editDeltaRotation) > 0.0005f)
		{
			m_dragSkipCounter++;
		}
	}

	private float GetEditDeltaRotation()
	{
		Camera cam = Cam;
		if (cam != null)
		{
			Vector3 vector = transform.TransformDirection(m_activeEditAxis);
			Vector3 vector2 = cam.WorldToScreenPoint(transform.position);
			vector2.z = 0f;
			Vector3 p_screenAxis;
			if (Mathf.Abs(Vector3.Dot(vector, cam.transform.forward)) > 0.5f)
			{
				float num = Mathf.Sign(Vector3.Dot(cam.transform.forward, vector));
				if (num == 0f)
				{
					num = 1f;
				}
				Vector3 normalized = (GetCursorPosition() - vector2).normalized;
				p_screenAxis = new Vector3(0f - Mathf.Sign(normalized.y), Mathf.Sign(normalized.x), 0f);
				p_screenAxis *= num;
			}
			else
			{
				p_screenAxis = Vector3.Cross(cam.transform.forward, vector);
				p_screenAxis = cam.transform.InverseTransformDirection(p_screenAxis);
				p_screenAxis.z = 0f;
			}
			p_screenAxis.Normalize();
			if (cam.orthographic)
			{
				return GetEditDeltaOrthogonal(p_screenAxis, 360f / cam.orthographicSize, cam);
			}
			return GetEditDeltaPerspective(p_screenAxis, 360f, cam);
		}
		return 0f;
	}

	private float GetEditDeltaPerspective(Vector3 p_screenAxis, float p_multiplier, Camera p_cam)
	{
		float num = Vector3.Dot(p_screenAxis, GetCursorPosition() - m_lastCursorPos);
		Ray ray = p_cam.ScreenPointToRay(m_lastCursorPos + num * p_screenAxis);
		Ray ray2 = p_cam.ScreenPointToRay(m_lastCursorPos);
		Vector3 direction = ray.direction;
		Vector3 direction2 = ray2.direction;
		return (direction - direction2).magnitude * Mathf.Sign(num) * p_multiplier;
	}

	private float GetEditDeltaOrthogonal(Vector3 p_screenAxis, float p_multiplier, Camera p_cam)
	{
		float num = Vector3.Dot(p_screenAxis, GetCursorPosition() - m_lastCursorPos);
		Vector3 vector = p_cam.ScreenToWorldPoint(m_lastCursorPos + num * p_screenAxis);
		Vector3 vector2 = p_cam.ScreenToWorldPoint(m_lastCursorPos);
		return (vector - vector2).magnitude * Mathf.Sign(num) * p_multiplier;
	}

	private Vector3 GetCursorPosition()
	{
		if (Input.touchCount > 0)
		{
			return Input.GetTouch(0).position;
		}
		return Input.mousePosition;
	}
}
