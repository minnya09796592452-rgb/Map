using System;
using UnityEngine;

namespace Beam.UI;

[Serializable]
public class GameObjectViewAdapter : IGameObjectViewAdapter
{
	[SerializeField]
	private GameObject _gameObject;

	public Transform Transform => _gameObject.transform;

	public bool Active
	{
		get
		{
			if (_gameObject != null)
			{
				return _gameObject.activeSelf;
			}
			return false;
		}
		set
		{
			if (_gameObject != null)
			{
				_gameObject.SetActive(value);
			}
		}
	}
}
