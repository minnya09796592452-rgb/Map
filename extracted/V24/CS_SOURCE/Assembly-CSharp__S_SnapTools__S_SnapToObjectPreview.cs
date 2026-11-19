using System;
using System.Collections.Generic;
using MyUtility;
using UnityEngine;

namespace S_SnapTools;

public class S_SnapToObjectPreview : MonoBehaviour
{
	private Camera m_cam;

	private SphereCollider m_collider;

	private bool m_isDrawUI;

	private float m_relativeScreenSize = 0.2f;

	private Action m_onClick;

	private Material m_uiMaterialLine;

	private Material m_uiMaterialFill;

	private Mesh m_uiMesh;

	private Vector3[] m_uiVerts = new Vector3[30];

	private bool m_isUiMeshIndicesSet;

	private float m_worldRadius = 1f;

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

	public float WorldRadius => m_worldRadius;

	public void Init(bool p_isDrawUI, float p_relativeScreenSize, Action p_onClick, Material p_uiMaterialLine, Material p_uiMaterialFill)
	{
		m_isDrawUI = p_isDrawUI;
		m_relativeScreenSize = p_relativeScreenSize;
		m_onClick = p_onClick;
		m_uiMaterialLine = p_uiMaterialLine;
		m_uiMaterialFill = p_uiMaterialFill;
		if (m_isDrawUI && (m_uiMaterialLine == null || m_uiMaterialFill == null))
		{
			Debug.LogError("S_SnapToObjectPreview: Init: IsDrawUI is set to 'true', but no UImaterial was passed!");
		}
	}

	private void Start()
	{
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		Vector3 zero = Vector3.zero;
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			zero += renderer.bounds.center;
		}
		zero *= 1f / (float)componentsInChildren.Length;
		m_collider = gameObject.AddComponent<SphereCollider>();
		m_collider.center = transform.InverseTransformPoint(zero);
		if (m_isDrawUI)
		{
			CreateUIObject();
		}
		if (m_onClick != null)
		{
			UtilityClickTouchDetector utilityClickTouchDetector = gameObject.AddComponent<UtilityClickTouchDetector>();
			utilityClickTouchDetector.ColliderInstance = m_collider;
			utilityClickTouchDetector.m_onClick = (Action)Delegate.Combine(utilityClickTouchDetector.m_onClick, m_onClick);
		}
		else
		{
			Debug.LogError("S_SnapToObjectPreview: Start: you have to call 'Init' before 'Start' and pass a valid p_onClick parameter!");
		}
	}

	private void Update()
	{
		if (Cam != null)
		{
			Vector3 vector = Cam.ScreenToWorldPoint(Cam.WorldToScreenPoint(transform.position) + Screen.height * Vector3.left * m_relativeScreenSize);
			m_worldRadius = (transform.position - vector).magnitude;
			m_collider.radius = 1f / transform.lossyScale.magnitude * m_worldRadius;
			UpdateUIVertices();
		}
	}

	private void OnDestroy()
	{
		if (m_uiMesh != null)
		{
			UnityEngine.Object.Destroy(m_uiMesh);
		}
	}

	private void CreateUIObject()
	{
		GameObject gameObject = new GameObject("S_SnapToObjectPreview UI");
		gameObject.transform.parent = transform;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		m_uiMesh = new Mesh();
		gameObject.AddComponent<MeshFilter>().mesh = m_uiMesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterials = new Material[2] { m_uiMaterialFill, m_uiMaterialLine };
	}

	private void UpdateUIVertices()
	{
		if (!(m_uiMesh != null) || !(m_collider != null) || !(Cam != null))
		{
			return;
		}
		float magnitude = transform.lossyScale.magnitude;
		float num = 1.5f * magnitude;
		Vector3 vector = transform.TransformPoint(m_collider.center);
		Vector3 vector2 = vector + (vector - Cam.transform.position).normalized * magnitude;
		float num2 = MathF.PI / 180f * (360f / (float)m_uiVerts.Length);
		for (int i = 0; i < m_uiVerts.Length; i++)
		{
			float f = (float)i * num2;
			m_uiVerts[i] = transform.InverseTransformPoint(vector2 + Cam.transform.TransformDirection(new Vector3(Mathf.Sin(f), Mathf.Cos(f), 0f)) * num);
		}
		m_uiMesh.vertices = m_uiVerts;
		if (!m_isUiMeshIndicesSet)
		{
			m_isUiMeshIndicesSet = true;
			m_uiMesh.subMeshCount = 2;
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			for (int j = 0; j < m_uiVerts.Length; j++)
			{
				if (j + 2 < m_uiVerts.Length)
				{
					list.Add(0);
					list.Add(j + 1);
					list.Add(j + 2);
				}
				list2.Add(j);
			}
			list2.Add(0);
			m_uiMesh.SetIndices(list.ToArray(), MeshTopology.Triangles, 0);
			m_uiMesh.SetIndices(list2.ToArray(), MeshTopology.LineStrip, 1);
		}
		m_uiMesh.RecalculateBounds();
	}
}
