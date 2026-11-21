using UnityEngine;

namespace RootMotion.Demos;

public class UserControlThirdPerson : MonoBehaviour
{
	public struct State
	{
		public Vector3 move;

		public Vector3 lookPos;

		public bool crouch;

		public bool jump;

		public int actionIndex;
	}

	public bool walkByDefault;

	public bool canCrouch = true;

	public bool canJump = true;

	public State state;

	protected Transform cam;

	protected virtual void Start()
	{
		cam = Camera.main.transform;
	}

	protected virtual void Update()
	{
		state.crouch = canCrouch && Input.GetKey(KeyCode.C);
		state.jump = canJump && Input.GetButton("Jump");
		float axisRaw = Input.GetAxisRaw("Horizontal");
		float axisRaw2 = Input.GetAxisRaw("Vertical");
		Vector3 tangent = cam.rotation * new Vector3(axisRaw, 0f, axisRaw2).normalized;
		if (tangent != Vector3.zero)
		{
			Vector3 normal = transform.up;
			Vector3.OrthoNormalize(ref normal, ref tangent);
			state.move = tangent;
		}
		else
		{
			state.move = Vector3.zero;
		}
		bool key = Input.GetKey(KeyCode.LeftShift);
		float num;
		if (walkByDefault)
		{
			num = (key ? 1f : 0.5f);
		}
		else
		{
			num = (key ? 0.5f : 1f);
		}
		state.move *= num;
		state.lookPos = transform.position + cam.forward * 100f;
	}
}
