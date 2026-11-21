using RootMotion.FinalIK;
using UnityEngine;

namespace RootMotion.Demos;

public class HoldingHands : MonoBehaviour
{
	public FullBodyBipedIK rightHandChar;

	public FullBodyBipedIK leftHandChar;

	public Transform rightHandTarget;

	public Transform leftHandTarget;

	public float crossFade;

	public float speed = 10f;

	private Quaternion rightHandRotation;

	private Quaternion leftHandRotation;

	private void Start()
	{
		rightHandRotation = Quaternion.Inverse(rightHandChar.solver.rightHandEffector.bone.rotation) * transform.rotation;
		leftHandRotation = Quaternion.Inverse(leftHandChar.solver.leftHandEffector.bone.rotation) * transform.rotation;
	}

	private void LateUpdate()
	{
		Vector3 b = Vector3.Lerp(rightHandChar.solver.rightHandEffector.bone.position, leftHandChar.solver.leftHandEffector.bone.position, crossFade);
		transform.position = Vector3.Lerp(transform.position, b, Time.deltaTime * speed);
		transform.rotation = Quaternion.Slerp(rightHandChar.solver.rightHandEffector.bone.rotation * rightHandRotation, leftHandChar.solver.leftHandEffector.bone.rotation * leftHandRotation, crossFade);
		rightHandChar.solver.rightHandEffector.position = rightHandTarget.position;
		rightHandChar.solver.rightHandEffector.rotation = rightHandTarget.rotation;
		leftHandChar.solver.leftHandEffector.position = leftHandTarget.position;
		leftHandChar.solver.leftHandEffector.rotation = leftHandTarget.rotation;
	}
}
