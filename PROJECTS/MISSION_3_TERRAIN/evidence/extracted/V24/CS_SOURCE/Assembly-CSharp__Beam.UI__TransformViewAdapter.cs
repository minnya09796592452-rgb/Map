using System;
using UnityEngine;

namespace Beam.UI;

[Serializable]
public class TransformViewAdapter : ITransformViewAdapter
{
	[SerializeField]
	private Transform _transform;

	public Transform Transform => _transform;

	public Vector3 Position
	{
		get
		{
			if (_transform != null)
			{
				return _transform.position;
			}
			return default;
		}
		set
		{
			if (_transform != null)
			{
				_transform.position = value;
			}
		}
	}

	public Quaternion Rotation
	{
		get
		{
			if (_transform != null)
			{
				return _transform.rotation;
			}
			return default;
		}
		set
		{
			if (_transform != null)
			{
				_transform.rotation = value;
			}
		}
	}

	public Vector3 LocalPosition
	{
		get
		{
			if (_transform != null)
			{
				return _transform.localPosition;
			}
			return default;
		}
		set
		{
			if (_transform != null)
			{
				_transform.localPosition = value;
			}
		}
	}

	public Quaternion LocalRotation
	{
		get
		{
			if (_transform != null)
			{
				return _transform.localRotation;
			}
			return default;
		}
		set
		{
			if (_transform != null)
			{
				_transform.localRotation = value;
			}
		}
	}

	public TransformViewAdapter(Transform t)
	{
		_transform = t;
	}

	public void SetParent(ITransformViewAdapter transform, bool worldPositionStays)
	{
		_transform.SetParent(transform.Transform, worldPositionStays);
	}
}
