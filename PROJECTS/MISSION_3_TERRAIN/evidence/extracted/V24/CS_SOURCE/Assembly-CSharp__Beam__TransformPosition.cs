using System;
using UnityEngine;

namespace Beam;

[Serializable]
public struct TransformPosition : IHavePosition
{
	[SerializeField]
	private Transform _transform;

	public Vector3 Position => _transform.position;

	public TransformPosition(Transform transform)
	{
		_transform = transform;
	}
}
