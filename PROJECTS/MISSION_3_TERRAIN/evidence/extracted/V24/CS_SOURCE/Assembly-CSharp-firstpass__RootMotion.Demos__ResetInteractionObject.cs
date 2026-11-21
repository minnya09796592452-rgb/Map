using System.Collections;
using RootMotion.FinalIK;
using UnityEngine;

namespace RootMotion.Demos;

public class ResetInteractionObject : MonoBehaviour
{
	public float resetDelay = 1f;

	private Vector3 defaultPosition;

	private Quaternion defaultRotation;

	private Transform defaultParent;

	private Rigidbody r;

	private void Start()
	{
		defaultPosition = transform.position;
		defaultRotation = transform.rotation;
		defaultParent = transform.parent;
		r = GetComponent<Rigidbody>();
	}

	private void OnPickUp(Transform t)
	{
		if (enabled)
		{
			StopAllCoroutines();
			StartCoroutine(ResetObject(Time.time + resetDelay));
		}
	}

	private IEnumerator ResetObject(float resetTime)
	{
		while (Time.time < resetTime)
		{
			yield return null;
		}
		Poser component = transform.parent.GetComponent<Poser>();
		if (component != null)
		{
			component.poseRoot = null;
			component.weight = 0f;
		}
		transform.parent = defaultParent;
		transform.position = defaultPosition;
		transform.rotation = defaultRotation;
		if (r != null)
		{
			r.isKinematic = false;
		}
	}
}
