using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beam;

public class Cell<TCellObject> : ICell where TCellObject : class, ICellObject
{
	private readonly Transform _transform;

	private readonly Vector3 _size;

	private int _lod = -1;

	private List<TCellObject> _objects = new List<TCellObject>();

	public int Id
	{
		get
		{
			float num = Mathf.Atan2(Position.z, Position.x) * 180f / MathF.PI;
			return (int)(Position.x + Position.z + num);
		}
	}

	public Vector3 Position
	{
		get
		{
			return _transform.position;
		}
		set
		{
			_transform.position = value;
		}
	}

	public Vector3 Size => _size;

	public int Lod
	{
		get
		{
			return _lod;
		}
		set
		{
			if (_lod != value)
			{
				_lod = value;
				LodChanged?.Invoke(_lod);
			}
		}
	}

	public IList<TCellObject> Objects => _objects;

	public Transform ObjectContainer { get; set; }

	public bool Generated { get; set; }

	public bool Loaded { get; set; }

	public event Action<int> LodChanged;

	public event Action<TCellObject> CellObjectFreed;

	public event Action<TCellObject> CellObjectDestroyed;

	public Cell(Transform transform, Vector3 size)
	{
		_transform = transform;
		_size = size;
	}

	public void ResetLod()
	{
		_lod = -1;
	}

	public void AddCellObject(TCellObject cellObject)
	{
		_objects.Add(cellObject);
		cellObject.Subscribe(this);
		cellObject.Freed += CellObject_Freed;
		cellObject.Destroyed += CellObject_Destroyed;
	}

	public void RemoveCellObject(TCellObject cellObject)
	{
		if (_objects.Remove(cellObject))
		{
			cellObject.Unsubscribe(this);
			cellObject.Freed -= CellObject_Freed;
			cellObject.Destroyed -= CellObject_Destroyed;
		}
	}

	private void CellObject_Freed(ICellObject obj)
	{
		TCellObject val = (TCellObject)obj;
		RemoveCellObject(val);
		CellObjectFreed?.Invoke(val);
	}

	private void CellObject_Destroyed(ICellObject obj)
	{
		TCellObject val = (TCellObject)obj;
		RemoveCellObject(val);
		CellObjectDestroyed?.Invoke(val);
	}
}
