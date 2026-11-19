using System;
using System.Collections.Generic;
using Beam.UI;
using Ceto;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beam;

public class Buoyancy : MonoBehaviour, IQueryWaves
{
	[Serializable]
	private class SlicesPerAxis
	{
		public int X = 2;

		public int Y = 2;

		public int Z = 2;
	}

	private const float WATER_DENSITY = 1000f;

	private List<WaveQuery> _waveQueries = new List<WaveQuery>();

	private int _outOfRangeCount;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private Rigidbody _rigidbody;

	[SerializeField]
	private Collider _bouyancyCollider;

	[FormerlySerializedAs("density")]
	[SerializeField]
	private float _density = 500f;

	[SerializeField]
	private SlicesPerAxis _slicesPerAxis = new SlicesPerAxis();

	[FormerlySerializedAs("voxelsLimit")]
	[SerializeField]
	private int _voxelsLimit = 8;

	[FormerlySerializedAs("damper")]
	[SerializeField]
	private float _damper = 0.1f;

	private float _originalDamper;

	private float _voxelHalfHeight;

	private Vector3 _localArchimedesForce;

	private List<Vector3> _voxels;

	private bool _wind = true;

	private bool _waveQueriesInitialized;

	private bool _floating;

	private bool _isInWater;

	bool IQueryWaves.Active
	{
		get
		{
			if (gameObject != null)
			{
				return gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	IEnumerable<WaveQuery> IQueryWaves.WaveQueries => _waveQueries;

	public Rigidbody Rigidbody => _rigidbody;

	public bool Floating
	{
		get
		{
			if (_isInWater)
			{
				return _floating;
			}
			return false;
		}
	}

	public bool IsInWater => _isInWater;

	void IQueryWaves.InRange()
	{
		_outOfRangeCount = 0;
	}

	void IQueryWaves.OutOfRange()
	{
		if (_outOfRangeCount >= 3)
		{
			_outOfRangeCount++;
			BaseObject component = GetComponent<BaseObject>();
			if (!(component == null))
			{
				LocalizedNotification localizedNotification = new LocalizedNotification(new Notification());
				localizedNotification.Priority = NotificationPriority.Immediate;
				localizedNotification.Duration = 8f;
				localizedNotification.MessageText.SetTerm("ITEM_NTF_LOST_AT_SEA_DESC");
				localizedNotification.MessageText.SetArgs(component.DisplayName);
				localizedNotification.Raise();
			}
		}
	}

	private void Awake()
	{
		Quaternion rotation = transform.rotation;
		Vector3 position = transform.position;
		transform.rotation = Quaternion.identity;
		transform.position = Vector3.zero;
		if (!_bouyancyCollider)
		{
			_bouyancyCollider = GetComponent<Collider>();
		}
		Bounds bounds = _bouyancyCollider.bounds;
		if (bounds.size.x < bounds.size.y)
		{
			_voxelHalfHeight = bounds.size.x;
			_voxelHalfHeight /= 2 * _slicesPerAxis.X;
		}
		else
		{
			_voxelHalfHeight = bounds.size.y;
			_voxelHalfHeight /= 2 * _slicesPerAxis.Y;
		}
		float num = bounds.size.z / (float)(2 * _slicesPerAxis.Z);
		if (num < _voxelHalfHeight)
		{
			_voxelHalfHeight = num;
		}
		_rigidbody.centerOfMass = new Vector3(0f, (0f - bounds.extents.y) * 0f, 0f) + transform.InverseTransformPoint(bounds.center);
		_voxels = SliceIntoVoxels();
		transform.rotation = rotation;
		transform.position = position;
		float num2 = _rigidbody.mass / _density;
		WeldPoints(_voxels, _voxelsLimit);
		CreateWaveQueries();
		float y = 1000f * Mathf.Abs(Physics.gravity.y) * num2;
		_localArchimedesForce = new Vector3(0f, y, 0f) / _voxels.Count;
		_originalDamper = _damper;
	}

	private void CreateWaveQueries()
	{
		_waveQueries.Clear();
		for (int i = 0; i < _voxels.Count; i++)
		{
			Vector3 vector = transform.TransformPoint(_voxels[i]);
			_waveQueries.Add(new WaveQuery(vector.x, vector.z));
		}
	}

	private void Start()
	{
		if (LevelLoader.IsLoading(gameObject))
		{
			LevelLoader.CurrentLoader.PhysicalLoading.Add(() =>
			{
				AddWaveQueries();
			});
		}
		else
		{
			AddWaveQueries();
		}
	}

	private void AddWaveQueries()
	{
		if (!_waveQueriesInitialized)
		{
			if ((bool)Singleton<BuoyancyManager>.Instance)
			{
				Singleton<BuoyancyManager>.Instance.Add(this);
			}
			_waveQueriesInitialized = true;
		}
	}

	private void RemoveWaveQueries()
	{
		if (_waveQueriesInitialized)
		{
			if ((bool)Singleton<BuoyancyManager>.Instance)
			{
				Singleton<BuoyancyManager>.Instance.Remove(this);
			}
			_waveQueriesInitialized = false;
		}
	}

	private void OnDestroy()
	{
		RemoveWaveQueries();
	}

	public void EnableWind(bool enable)
	{
		_wind = enable;
	}

	public void SetDampening(float dampening)
	{
		_damper = dampening;
	}

	public void ResetDampening()
	{
		_damper = _originalDamper;
	}

	private List<Vector3> SliceIntoVoxels()
	{
		List<Vector3> list = new List<Vector3>(_slicesPerAxis.X * _slicesPerAxis.Y * _slicesPerAxis.Z);
		Bounds bounds = _bouyancyCollider.bounds;
		for (int i = 0; i < _slicesPerAxis.X; i++)
		{
			for (int j = 0; j < _slicesPerAxis.Y; j++)
			{
				for (int k = 0; k < _slicesPerAxis.Z; k++)
				{
					float x = bounds.min.x + bounds.size.x / (float)_slicesPerAxis.X * (0.5f + (float)i);
					float y = bounds.min.y + bounds.size.y / (float)_slicesPerAxis.Y * (0.5f + (float)j);
					float z = bounds.min.z + bounds.size.z / (float)_slicesPerAxis.Z * (0.5f + (float)k);
					Vector3 item = transform.InverseTransformPoint(new Vector3(x, y, z));
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static void FindClosestPoints(IList<Vector3> list, out int firstIndex, out int secondIndex)
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		firstIndex = 0;
		secondIndex = 1;
		for (int i = 0; i < list.Count - 1; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				float num3 = Vector3.Distance(list[i], list[j]);
				if (num3 < num)
				{
					num = num3;
					firstIndex = i;
					secondIndex = j;
				}
				if (num3 > num2)
				{
					num2 = num3;
				}
			}
		}
	}

	private void OnTransformParentChanged()
	{
		if (!_rigidbody)
		{
			_rigidbody = GetComponent<Rigidbody>();
		}
	}

	private static void WeldPoints(IList<Vector3> list, int targetCount)
	{
		if (list.Count > 2 && targetCount >= 2)
		{
			while (list.Count > targetCount)
			{
				FindClosestPoints(list, out var firstIndex, out var secondIndex);
				Vector3 item = (list[firstIndex] + list[secondIndex]) * 0.5f;
				list.RemoveAt(secondIndex);
				list.RemoveAt(firstIndex);
				list.Add(item);
			}
		}
	}

	private void UpdateWaterAndQueries()
	{
		CheckIfInWater();
		if (_isInWater)
		{
			AddWaveQueries();
		}
		else
		{
			RemoveWaveQueries();
		}
	}

	private void FixedUpdate()
	{
		if (!Physics.autoSimulation)
		{
			return;
		}
		for (int i = 0; i < _voxels.Count; i++)
		{
			Vector3 position = _voxels[i];
			Vector3 vector = transform.TransformPoint(position);
			WaveQuery waveQuery = _waveQueries[i];
			waveQuery.posX = vector.x;
			waveQuery.posZ = vector.z;
		}
		if (!_rigidbody || !_rigidbody.useGravity || !_bouyancyCollider.enabled)
		{
			return;
		}
		UpdateWaterAndQueries();
		if (!_isInWater)
		{
			_floating = false;
			return;
		}
		int num = 0;
		for (int j = 0; j < _voxels.Count; j++)
		{
			Vector3 position2 = _voxels[j];
			Vector3 vector2 = transform.TransformPoint(position2);
			float height = _waveQueries[j].result.height;
			if (vector2.y - _voxelHalfHeight < height)
			{
				float value = (height - vector2.y) / (2f * _voxelHalfHeight) + 0.5f;
				value = Mathf.Clamp01(value);
				Vector3 force = -_rigidbody.GetPointVelocity(vector2) * (_damper * _rigidbody.mass) + Mathf.Sqrt(value) * _localArchimedesForce;
				_rigidbody.AddForceAtPosition(force, vector2);
				num++;
			}
		}
		if (!LevelLoader.IsLoadingScene() && !LevelLoader.IsLoading(gameObject))
		{
			_floating = num > 0;
			if (_floating && _wind)
			{
				_rigidbody.AddForce(Current.GetVelocity(), ForceMode.VelocityChange);
			}
		}
	}

	public void CheckIfInWater()
	{
		float num = Ceto.Ocean.Instance.QueryWaves(transform.position.x, transform.position.z) + 0.1f;
		_isInWater = num >= transform.position.y;
	}
}
