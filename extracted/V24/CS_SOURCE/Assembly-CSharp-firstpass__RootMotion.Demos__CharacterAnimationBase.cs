using UnityEngine;

namespace RootMotion.Demos;

public abstract class CharacterAnimationBase : MonoBehaviour
{
	public bool smoothFollow = true;

	public float smoothFollowSpeed = 20f;

	protected bool animatePhysics;

	private Vector3 lastPosition;

	private Vector3 localPosition;

	private Quaternion localRotation;

	private Quaternion lastRotation;

	public virtual bool animationGrounded => true;

	public virtual Vector3 GetPivotPoint()
	{
		return transform.position;
	}

	public float GetAngleFromForward(Vector3 worldDirection)
	{
		Vector3 vector = transform.InverseTransformDirection(worldDirection);
		return Mathf.Atan2(vector.x, vector.z) * 57.29578f;
	}

	protected virtual void Start()
	{
		if (transform.parent.GetComponent<CharacterBase>() == null)
		{
			Debug.LogWarning("Animation controllers should be parented to character controllers!", transform);
		}
		lastPosition = transform.position;
		localPosition = transform.localPosition;
		lastRotation = transform.rotation;
		localRotation = transform.localRotation;
	}

	protected virtual void LateUpdate()
	{
		if (!animatePhysics)
		{
			SmoothFollow();
		}
	}

	protected virtual void FixedUpdate()
	{
		if (animatePhysics)
		{
			SmoothFollow();
		}
	}

	private void SmoothFollow()
	{
		if (smoothFollow)
		{
			transform.position = Vector3.Lerp(lastPosition, transform.parent.TransformPoint(localPosition), Time.deltaTime * smoothFollowSpeed);
			transform.rotation = Quaternion.Lerp(lastRotation, transform.parent.rotation * localRotation, Time.deltaTime * smoothFollowSpeed);
		}
		else
		{
			transform.localPosition = localPosition;
			transform.localRotation = localRotation;
		}
		lastPosition = transform.position;
		lastRotation = transform.rotation;
	}
}
