using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace RootMotion.Demos;

public abstract class PickUp2Handed : MonoBehaviour
{
	public int GUIspace;

	public InteractionSystem interactionSystem;

	public InteractionObject obj;

	public Transform pivot;

	public Transform holdPoint;

	public float pickUpTime = 0.3f;

	private float holdWeight;

	private float holdWeightVel;

	private Vector3 pickUpPosition;

	private Quaternion pickUpRotation;

	private bool holding
	{
		get
		{
			if (!holdingLeft)
			{
				return holdingRight;
			}
			return true;
		}
	}

	private bool holdingLeft
	{
		get
		{
			if (interactionSystem.IsPaused(FullBodyBipedEffector.LeftHand))
			{
				return interactionSystem.GetInteractionObject(FullBodyBipedEffector.LeftHand) == obj;
			}
			return false;
		}
	}

	private bool holdingRight
	{
		get
		{
			if (interactionSystem.IsPaused(FullBodyBipedEffector.RightHand))
			{
				return interactionSystem.GetInteractionObject(FullBodyBipedEffector.RightHand) == obj;
			}
			return false;
		}
	}

	private void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(GUIspace);
		if (!holding)
		{
			if (GUILayout.Button("Pick Up " + obj.name))
			{
				interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, obj, interrupt: false);
				interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, obj, interrupt: false);
			}
		}
		else
		{
			GUILayout.BeginVertical();
			if (holdingRight && GUILayout.Button("Release Right"))
			{
				interactionSystem.ResumeInteraction(FullBodyBipedEffector.RightHand);
			}
			if (holdingLeft && GUILayout.Button("Release Left"))
			{
				interactionSystem.ResumeInteraction(FullBodyBipedEffector.LeftHand);
			}
			if (GUILayout.Button("Drop " + obj.name))
			{
				interactionSystem.ResumeAll();
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndHorizontal();
	}

	protected abstract void RotatePivot();

	private void Start()
	{
		InteractionSystem interactionSystem = this.interactionSystem;
		interactionSystem.OnInteractionStart = (InteractionSystem.InteractionDelegate)Delegate.Combine(interactionSystem.OnInteractionStart, new InteractionSystem.InteractionDelegate(OnStart));
		InteractionSystem interactionSystem2 = this.interactionSystem;
		interactionSystem2.OnInteractionPause = (InteractionSystem.InteractionDelegate)Delegate.Combine(interactionSystem2.OnInteractionPause, new InteractionSystem.InteractionDelegate(OnPause));
		InteractionSystem interactionSystem3 = this.interactionSystem;
		interactionSystem3.OnInteractionResume = (InteractionSystem.InteractionDelegate)Delegate.Combine(interactionSystem3.OnInteractionResume, new InteractionSystem.InteractionDelegate(OnDrop));
	}

	private void OnPause(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
	{
		if (effectorType == FullBodyBipedEffector.LeftHand && !(interactionObject != obj))
		{
			obj.transform.parent = interactionSystem.transform;
			Rigidbody component = obj.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.isKinematic = true;
			}
			pickUpPosition = obj.transform.position;
			pickUpRotation = obj.transform.rotation;
			holdWeight = 0f;
			holdWeightVel = 0f;
		}
	}

	private void OnStart(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
	{
		if (effectorType == FullBodyBipedEffector.LeftHand && !(interactionObject != obj))
		{
			RotatePivot();
			holdPoint.rotation = obj.transform.rotation;
		}
	}

	private void OnDrop(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
	{
		if (!holding && !(interactionObject != obj))
		{
			obj.transform.parent = null;
			if (obj.GetComponent<Rigidbody>() != null)
			{
				obj.GetComponent<Rigidbody>().isKinematic = false;
			}
		}
	}

	private void LateUpdate()
	{
		if (holding)
		{
			holdWeight = Mathf.SmoothDamp(holdWeight, 1f, ref holdWeightVel, pickUpTime);
			obj.transform.position = Vector3.Lerp(pickUpPosition, holdPoint.position, holdWeight);
			obj.transform.rotation = Quaternion.Lerp(pickUpRotation, holdPoint.rotation, holdWeight);
		}
	}

	private void OnDestroy()
	{
		if (!(this.interactionSystem == null))
		{
			InteractionSystem interactionSystem = this.interactionSystem;
			interactionSystem.OnInteractionStart = (InteractionSystem.InteractionDelegate)Delegate.Remove(interactionSystem.OnInteractionStart, new InteractionSystem.InteractionDelegate(OnStart));
			InteractionSystem interactionSystem2 = this.interactionSystem;
			interactionSystem2.OnInteractionPause = (InteractionSystem.InteractionDelegate)Delegate.Remove(interactionSystem2.OnInteractionPause, new InteractionSystem.InteractionDelegate(OnPause));
			InteractionSystem interactionSystem3 = this.interactionSystem;
			interactionSystem3.OnInteractionResume = (InteractionSystem.InteractionDelegate)Delegate.Remove(interactionSystem3.OnInteractionResume, new InteractionSystem.InteractionDelegate(OnDrop));
		}
	}
}
