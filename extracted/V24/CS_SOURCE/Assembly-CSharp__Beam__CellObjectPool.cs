using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beam;

[Serializable]
public class CellObjectPool<TCellObject> : ICreateCellObjects<TCellObject>, IFreeCellObjects<TCellObject> where TCellObject : MonoBehaviour, ICellObject
{
	[SerializeField]
	private Transform _pooledContainer;

	[SerializeField]
	[HideInInspector]
	private Dictionary<int, Stack<TCellObject>> _pools = new Dictionary<int, Stack<TCellObject>>();

	TCellObject ICreateCellObjects<TCellObject>.CreateCellObject(TCellObject prefab)
	{
		return PopCellObject(prefab);
	}

	void IFreeCellObjects<TCellObject>.FreeCellObject(TCellObject obj)
	{
		PoolCellObject(obj);
	}

	private TCellObject PopCellObject(TCellObject prefab)
	{
		_pools.TryGetValue(prefab.PrefabId, out var value);
		TCellObject val;
		if (value != null && value.Count > 0)
		{
			val = value.Pop();
			val.transform.SetParent(null);
		}
		else
		{
			val = UnityEngine.Object.Instantiate(prefab);
		}
		return val;
	}

	private void PoolCellObject(TCellObject obj)
	{
		if (!_pools.TryGetValue(obj.PrefabId, out var value))
		{
			value = new Stack<TCellObject>();
			_pools.Add(obj.PrefabId, value);
		}
		obj.transform.SetParent(_pooledContainer, worldPositionStays: false);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		value.Push(obj);
	}
}
