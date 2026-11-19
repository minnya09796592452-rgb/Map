using System;
using S_SnapTools;
using UnityEngine;

namespace LE_LevelEditor.Core;

[Serializable]
public class LE_ObjectSnapPoint
{
	private const string BUTTON_VISUALS_NAME = "LE_ObjectSnapPoint.Button.Visuals";

	[SerializeField]
	private Transform m_point;

	[SerializeField]
	private S_SnapToObjectPrefab[] m_prefabs = new S_SnapToObjectPrefab[1];

	[SerializeField]
	private Vector3 m_snapButtonLocalOffset = Vector3.zero;

	[SerializeField]
	private float m_snapButtonSize = 1f;

	private S_SnapToObject m_snapSysInstance;

	public Transform Point
	{
		get
		{
			return m_point;
		}
		set
		{
			m_point = value;
		}
	}

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

	public Vector3 SnapButtonLocalOffset
	{
		get
		{
			return m_snapButtonLocalOffset;
		}
		set
		{
			m_snapButtonLocalOffset = value;
		}
	}

	public float SnapButtonSize
	{
		get
		{
			return m_snapButtonSize;
		}
		set
		{
			m_snapButtonSize = value;
		}
	}

	public S_SnapToObject SnapSystemInstance
	{
		get
		{
			return m_snapSysInstance;
		}
		set
		{
			m_snapSysInstance = value;
		}
	}

	public LE_ObjectSnapPoint()
	{
		if (m_prefabs != null && m_prefabs.Length == 1)
		{
			m_prefabs[0] = new S_SnapToObjectPrefab();
		}
	}

	public S_SnapToObject InstatiateSnapSystem(GameObject p_buttonVisuals, bool p_isDrawUI, Material p_materialLine, Material p_materialFill)
	{
		if (m_point == null)
		{
			Debug.LogError("LE_ObjectSnapPoint: InstatiateSnapSystem: the Transform property 'Point' is null! Set it in the inspector!");
			return null;
		}
		if (m_prefabs == null || m_prefabs.Length == 0)
		{
			Debug.LogError("LE_ObjectSnapPoint: InstatiateSnapSystem: the Prefabs property array is null or has no entries!");
			return null;
		}
		if (m_snapSysInstance != null)
		{
			Debug.LogError("LE_ObjectSnapPoint: InstatiateSnapSystem: the system was already initialized!");
			UnityEngine.Object.Destroy(m_snapSysInstance);
		}
		m_snapSysInstance = Point.gameObject.AddComponent<S_SnapToObject>();
		m_snapSysInstance.Prefabs = Prefabs;
		m_snapSysInstance.IsDeactivatedAfterSnap = true;
		m_snapSysInstance.IsDestroyedAfterSnap = false;
		m_snapSysInstance.IsDrawUI = p_isDrawUI;
		m_snapSysInstance.UImaterialLine = p_materialLine;
		m_snapSysInstance.UImaterialFill = p_materialFill;
		SphereCollider sphereCollider = Point.gameObject.AddComponent<SphereCollider>();
		sphereCollider.radius = SnapButtonSize * 0.5f;
		sphereCollider.center = SnapButtonLocalOffset;
		p_buttonVisuals.name = "LE_ObjectSnapPoint.Button.Visuals";
		if (p_buttonVisuals != null)
		{
			p_buttonVisuals.transform.parent = Point;
			p_buttonVisuals.transform.localPosition = m_snapButtonLocalOffset;
			p_buttonVisuals.transform.localScale = Vector3.one * m_snapButtonSize;
			p_buttonVisuals.transform.localRotation = Quaternion.identity;
		}
		else
		{
			Debug.LogWarning("LE_ObjectSnapPoint: InstatiateSnapSystem: p_buttonVisuals is null! The snap button will be invisible for the user!");
		}
		return m_snapSysInstance;
	}

	public static void DestroySnapSystem(S_SnapToObject p_system)
	{
		if (!(p_system != null))
		{
			return;
		}
		UnityEngine.Object.Destroy(p_system);
		UnityEngine.Object.Destroy(p_system.GetComponent<SphereCollider>());
		Transform[] componentsInChildren = p_system.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].name == "LE_ObjectSnapPoint.Button.Visuals")
			{
				UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
			}
		}
	}
}
