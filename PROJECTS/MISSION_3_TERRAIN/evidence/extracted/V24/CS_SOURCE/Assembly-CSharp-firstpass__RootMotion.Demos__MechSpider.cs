using System;
using UnityEngine;

namespace RootMotion.Demos;

public class MechSpider : MonoBehaviour
{
	public LayerMask raycastLayers;

	public float scale = 1f;

	public Transform body;

	public MechSpiderLeg[] legs;

	public float legRotationWeight = 1f;

	public float rootPositionSpeed = 5f;

	public float rootRotationSpeed = 30f;

	public float breatheSpeed = 2f;

	public float breatheMagnitude = 0.2f;

	public float height = 3.5f;

	public float minHeight = 2f;

	public float raycastHeight = 10f;

	public float raycastDistance = 5f;

	private Vector3 lastPosition;

	private Vector3 defaultBodyLocalPosition;

	private float sine;

	private RaycastHit rootHit;

	public Vector3 velocity { get; private set; }

	private void Start()
	{
		lastPosition = transform.position;
	}

	private void Update()
	{
		velocity = (transform.position - lastPosition) / Time.deltaTime;
		lastPosition = transform.position;
		Vector3 legsPlaneNormal = GetLegsPlaneNormal();
		Quaternion quaternion = Quaternion.FromToRotation(transform.up, legsPlaneNormal);
		transform.rotation = Quaternion.Slerp(transform.rotation, quaternion * transform.rotation, Time.deltaTime * rootRotationSpeed);
		Vector3 vector = Vector3.Project(GetLegCentroid() + transform.up * height * scale - transform.position, transform.up);
		transform.position += vector * Time.deltaTime * (rootPositionSpeed * scale);
		if (Physics.Raycast(transform.position + transform.up * raycastHeight * scale, -transform.up, out rootHit, raycastHeight * scale + raycastDistance * scale, raycastLayers))
		{
			rootHit.distance -= raycastHeight * scale + minHeight * scale;
			if (rootHit.distance < 0f)
			{
				Vector3 b = transform.position - transform.up * rootHit.distance;
				transform.position = Vector3.Lerp(transform.position, b, Time.deltaTime * rootPositionSpeed * scale);
			}
		}
		sine += Time.deltaTime * breatheSpeed;
		if (sine >= MathF.PI * 2f)
		{
			sine -= MathF.PI * 2f;
		}
		float num = Mathf.Sin(sine) * breatheMagnitude * scale;
		Vector3 vector2 = transform.up * num;
		body.transform.position = transform.position + vector2;
	}

	private Vector3 GetLegCentroid()
	{
		Vector3 zero = Vector3.zero;
		float num = 1f / (float)legs.Length;
		for (int i = 0; i < legs.Length; i++)
		{
			zero += legs[i].position * num;
		}
		return zero;
	}

	private Vector3 GetLegsPlaneNormal()
	{
		Vector3 vector = transform.up;
		if (legRotationWeight <= 0f)
		{
			return vector;
		}
		float t = 1f / Mathf.Lerp(legs.Length, 1f, legRotationWeight);
		for (int i = 0; i < legs.Length; i++)
		{
			Vector3 vector2 = legs[i].position - (transform.position - transform.up * height * scale);
			Vector3 normal = transform.up;
			Vector3 tangent = vector2;
			Vector3.OrthoNormalize(ref normal, ref tangent);
			Quaternion b = Quaternion.FromToRotation(tangent, vector2);
			b = Quaternion.Lerp(Quaternion.identity, b, t);
			vector = b * vector;
		}
		return vector;
	}
}
