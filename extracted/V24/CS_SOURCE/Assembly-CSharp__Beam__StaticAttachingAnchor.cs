using System;
using UnityEngine;

namespace Beam;

[Serializable]
public struct StaticAttachingAnchor : IAttachingAnchor
{
	[SerializeField]
	private Vector3 _position;

	[SerializeField]
	[EulerAngles]
	private Quaternion _rotation;

	public StaticAttachingAnchor(Vector3 position, Quaternion rotation)
	{
		_position = position;
		_rotation = rotation;
	}

	public StaticAttachingAnchor(Transform transform)
	{
		_position = transform.position;
		_rotation = transform.rotation;
	}

	public Vector3 GetPosition()
	{
		return _position;
	}

	public Quaternion GetRotation()
	{
		return _rotation;
	}

	public bool IsKinematic()
	{
		return true;
	}
}
