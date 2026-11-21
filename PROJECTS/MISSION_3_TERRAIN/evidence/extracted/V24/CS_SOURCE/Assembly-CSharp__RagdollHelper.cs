using System.Collections.Generic;
using Beam;
using Ceto;
using UnityEngine;

public class RagdollHelper : MonoBehaviour
{
	private enum RagdollState
	{
		animated,
		ragdolled,
		blendToAnim
	}

	public class BodyPart
	{
		public Transform transform;

		public Vector3 storedPosition;

		public Quaternion storedRotation;
	}

	private RagdollState state;

	public Transform _renderers;

	public Transform _rig;

	public IPlayer player;

	public float ragdollToMecanimBlendTime = 0.5f;

	private float mecanimToGetUpTransitionTime = 0.05f;

	private float ragdollingEndTime = -100f;

	private Vector3 ragdolledHipPosition;

	private Vector3 ragdolledHeadPosition;

	private Vector3 ragdolledFeetPosition;

	private List<BodyPart> bodyParts = new List<BodyPart>();

	public Animator anim;

	public bool ragdolled
	{
		get
		{
			return state != RagdollState.animated;
		}
		set
		{
			if (value)
			{
				if (state == RagdollState.animated)
				{
					setKinematic(isKinematic: false);
					anim.enabled = false;
					state = RagdollState.ragdolled;
				}
			}
			else
			{
				if (state != RagdollState.ragdolled)
				{
					return;
				}
				setKinematic(isKinematic: true);
				ragdollingEndTime = Time.time;
				anim.enabled = true;
				state = RagdollState.blendToAnim;
				Quaternion rotation = _renderers.rotation;
				Quaternion rotation2 = _rig.rotation;
				transform.rotation = Quaternion.Euler(Vector3.zero);
				_renderers.rotation = rotation;
				_rig.rotation = rotation2;
				foreach (BodyPart bodyPart in bodyParts)
				{
					bodyPart.storedRotation = bodyPart.transform.rotation;
					bodyPart.storedPosition = bodyPart.transform.position;
				}
				ragdolledFeetPosition = 0.5f * (anim.GetBoneTransform(HumanBodyBones.LeftToes).position + anim.GetBoneTransform(HumanBodyBones.RightToes).position);
				ragdolledHeadPosition = anim.GetBoneTransform(HumanBodyBones.Head).position;
				ragdolledHipPosition = anim.GetBoneTransform(HumanBodyBones.Hips).position;
				float num = Ocean.Instance.QueryWaves(ragdolledHipPosition.x, ragdolledHipPosition.z);
				if ((ragdolledHipPosition.y < num) ? true : false)
				{
					anim.SetTrigger("ReviveFromWater");
				}
				else if (anim.GetBoneTransform(HumanBodyBones.Hips).forward.y > 0f)
				{
					anim.SetTrigger("ReviveFromBack");
				}
				else
				{
					anim.SetTrigger("ReviveFromBelly");
				}
			}
		}
	}

	private void setKinematic(bool isKinematic)
	{
		Rigidbody[] componentsInChildren = GetComponentsInChildren<Rigidbody>();
		foreach (Rigidbody obj in componentsInChildren)
		{
			bool flag = isKinematic;
			if (obj.gameObject == gameObject)
			{
				flag |= !player.IsOwner;
			}
			obj.isKinematic = flag;
		}
	}

	private void Start()
	{
		Component[] componentsInChildren = GetComponentsInChildren(typeof(Transform));
		foreach (Component component in componentsInChildren)
		{
			BodyPart bodyPart = new BodyPart();
			bodyPart.transform = component as Transform;
			bodyParts.Add(bodyPart);
		}
	}

	private void LateUpdate()
	{
		if (state != RagdollState.blendToAnim)
		{
			return;
		}
		if (Time.time <= ragdollingEndTime + mecanimToGetUpTransitionTime)
		{
			Vector3 vector = ragdolledHipPosition - anim.GetBoneTransform(HumanBodyBones.Hips).position;
			Vector3 pos = transform.position + vector;
			transform.position = new Vector3(pos.x, player.Movement.GetGroundPosition(pos), pos.z);
			Vector3 vector2 = ragdolledHeadPosition - ragdolledFeetPosition;
			vector2.y = 0f;
			Vector3 vector3 = 0.5f * (anim.GetBoneTransform(HumanBodyBones.LeftFoot).position + anim.GetBoneTransform(HumanBodyBones.RightFoot).position);
			Vector3 vector4 = anim.GetBoneTransform(HumanBodyBones.Head).position - vector3;
			vector4.y = 0f;
			transform.rotation *= Quaternion.FromToRotation(vector4.normalized, vector2.normalized);
		}
		float value = 1f - (Time.time - ragdollingEndTime - mecanimToGetUpTransitionTime) / ragdollToMecanimBlendTime;
		value = Mathf.Clamp01(value);
		foreach (BodyPart bodyPart in bodyParts)
		{
			if (bodyPart.transform != transform)
			{
				if (bodyPart.transform == anim.GetBoneTransform(HumanBodyBones.Hips))
				{
					bodyPart.transform.position = Vector3.Lerp(bodyPart.transform.position, bodyPart.storedPosition, value);
				}
				bodyPart.transform.rotation = Quaternion.Slerp(bodyPart.transform.rotation, bodyPart.storedRotation, value);
			}
		}
		if (value == 0f)
		{
			state = RagdollState.animated;
		}
	}
}
