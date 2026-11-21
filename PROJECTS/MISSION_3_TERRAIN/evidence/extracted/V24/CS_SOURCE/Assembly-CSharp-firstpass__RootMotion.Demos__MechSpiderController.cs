using UnityEngine;

namespace RootMotion.Demos;

public class MechSpiderController : MonoBehaviour
{
	public MechSpider mechSpider;

	public Transform cameraTransform;

	public float speed = 6f;

	public float turnSpeed = 30f;

	public Vector3 inputVector => new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

	private void Update()
	{
		Vector3 tangent = cameraTransform.forward;
		Vector3 normal = transform.up;
		Vector3.OrthoNormalize(ref normal, ref tangent);
		Quaternion quaternion = Quaternion.LookRotation(tangent, transform.up);
		transform.Translate(quaternion * inputVector.normalized * Time.deltaTime * speed * mechSpider.scale, Space.World);
		transform.rotation = Quaternion.RotateTowards(transform.rotation, quaternion, Time.deltaTime * turnSpeed);
	}
}
