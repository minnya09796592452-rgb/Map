using UnityEngine;

namespace Beam.UI;

public class NullTransformViewAdapter : NullObject<NullTransformViewAdapter>, ITransformViewAdapter
{
	public Transform Transform => NullTransform.Instance;

	public Vector3 Position { get; set; }

	public Quaternion Rotation { get; set; }

	public Vector3 LocalPosition { get; set; }

	public Quaternion LocalRotation { get; set; }

	public void SetParent(ITransformViewAdapter transform, bool worldPositionStays)
	{
	}
}
