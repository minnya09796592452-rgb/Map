using UnityEngine;

namespace Beam;

public class TransformProxy
{
	public Vector3 position;

	public Quaternion rotation;

	public Vector3 up => rotation * Vector3.up;

	public Vector3 down => rotation * Vector3.down;

	public Vector3 left => rotation * Vector3.left;

	public Vector3 right => rotation * Vector3.right;

	public Vector3 forward => rotation * Vector3.forward;

	public Vector3 back => rotation * Vector3.back;

	public TransformProxy(Transform transform)
	{
		position = transform.position;
		rotation = transform.rotation;
	}
}
