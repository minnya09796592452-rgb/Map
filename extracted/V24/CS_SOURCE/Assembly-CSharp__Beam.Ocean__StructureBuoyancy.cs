using System.Collections.Generic;
using Beam.UI;
using Ceto;
using Funlabs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beam.Ocean;

public class StructureBuoyancy : MultiplayerBehaviour, IQueryWaves
{
	private const float WATER_DENSITY = 1000f;

	private List<WaveQuery> _waveQueries = new List<WaveQuery>();

	private bool _outOfRange;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private Rigidbody _rigidbody;

	[SerializeField]
	private BoxCollider _bouyancyCollider;

	[FormerlySerializedAs("density")]
	[SerializeField]
	private float _density = 500f;

	[FormerlySerializedAs("slicesPerAxis")]
	[SerializeField]
	private int _slicesPerAxis = 3;

	[FormerlySerializedAs("voxelsLimit")]
	[SerializeField]
	private int _voxelsLimit = 16;

	[FormerlySerializedAs("damper")]
	[SerializeField]
	private float _damper = 0.1f;

	private float _originalDamper;

	private float _voxelHalfHeight;

	private Vector3 _localArchimedesForce;

	private List<Vector3> _voxels = new List<Vector3>();

	private bool _wind = true;

	private bool _floating;

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

	public BoxCollider BuoyancyCollider => _bouyancyCollider;

	public List<RaftBuoyancy> RaftBuoyancys { get; set; } = new List<RaftBuoyancy>();

	void IQueryWaves.InRange()
	{
		_outOfRange = false;
	}

	void IQueryWaves.OutOfRange()
	{
		if (!_outOfRange)
		{
			_outOfRange = true;
			LocalizedNotification localizedNotification = new LocalizedNotification(new Notification());
			localizedNotification.Priority = NotificationPriority.Immediate;
			localizedNotification.Duration = 8f;
			localizedNotification.MessageText.SetTerm("ITEM_NTF_LOST_AT_SEA_DESC");
			localizedNotification.MessageText.SetArgs("INTERACTIVE_TYPE_STRUCTURE_RAFT");
			localizedNotification.Raise();
		}
	}

	private void Awake()
	{
		_originalDamper = _damper;
	}

	public override void Attached()
	{
		base.Attached();
		Resize();
	}

	public void Resize()
	{
		Vector3 position = transform.position;
		Quaternion rotation = transform.rotation;
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		_rigidbody.velocity = Vector3.zero;
		_rigidbody.angularVelocity = Vector3.zero;
		_rigidbody.useGravity = false;
		_rigidbody.isKinematic = true;
		_bouyancyCollider.center = Vector3.zero;
		_bouyancyCollider.size = Vector3.zero;
		Bounds bounds = _bouyancyCollider.bounds;
		for (int i = 0; i < RaftBuoyancys.Count; i++)
		{
			RaftBuoyancy raftBuoyancy = RaftBuoyancys[i];
			bounds.Encapsulate(raftBuoyancy.Collider.bounds);
		}
		_bouyancyCollider.center = bounds.center;
		_bouyancyCollider.size = bounds.size;
		_voxelHalfHeight = _bouyancyCollider.bounds.size.y / 2f;
		_voxels = SliceIntoVoxels();
		CreateWaveQueries();
		float num = _rigidbody.mass / _density;
		float y = 1000f * Mathf.Abs(Physics.gravity.y) * num;
		_localArchimedesForce = new Vector3(0f, y, 0f) / _voxels.Count;
		transform.rotation = rotation;
		transform.position = position;
		gameObject.SetActive(value: false);
		gameObject.SetActive(value: true);
		_rigidbody.useGravity = true;
		bool isOwner = entity.IsOwner;
		_rigidbody.isKinematic = !isOwner;
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
				Singleton<BuoyancyManager>.Instance?.Add(this);
			});
		}
		else
		{
			Singleton<BuoyancyManager>.Instance?.Add(this);
		}
	}

	private void OnDestroy()
	{
		Singleton<BuoyancyManager>.Instance?.Remove(this);
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
		List<Vector3> list = new List<Vector3>(_slicesPerAxis * _slicesPerAxis);
		Bounds bounds = _bouyancyCollider.bounds;
		for (int i = 0; i < _slicesPerAxis; i++)
		{
			for (int j = 0; j < _slicesPerAxis; j++)
			{
				float x = bounds.min.x + bounds.size.x / (float)_slicesPerAxis * (0.5f + (float)i);
				float z = bounds.min.z + bounds.size.z / (float)_slicesPerAxis * (0.5f + (float)j);
				float y = bounds.min.y + bounds.size.y * 0.5f;
				Vector3 item = transform.InverseTransformPoint(new Vector3(x, y, z));
				list.Add(item);
			}
		}
		return list;
	}

	private void FixedUpdate()
	{
		if (!Physics.autoSimulation)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < _voxels.Count; i++)
		{
			Vector3 position = _voxels[i];
			Vector3 vector = transform.TransformPoint(position);
			WaveQuery waveQuery = _waveQueries[i];
			waveQuery.posX = vector.x;
			waveQuery.posZ = vector.z;
			float height = waveQuery.result.height;
			if (vector.y - _voxelHalfHeight < height)
			{
				float num2 = (height - vector.y) / (2f * _voxelHalfHeight) + 0.5f;
				if (num2 > 7f)
				{
					num2 = 7f / (_rigidbody.mass / 40f);
				}
				else if (num2 < 0f)
				{
					num2 = 0f;
				}
				Vector3 vector2 = -_rigidbody.GetPointVelocity(vector) * _damper * _rigidbody.mass;
				vector2.x /= _voxels.Count;
				vector2.z /= _voxels.Count;
				Vector3 force = vector2 + Mathf.Sqrt(num2) * _localArchimedesForce;
				_rigidbody.AddForceAtPosition(force, vector);
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
}
