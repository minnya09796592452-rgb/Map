using Beam;
using Beam.Serialization.Json;
using Funlabs;
using UnityEngine;

public class PiscusManager : SaveablePrefab
{
	[SerializeField]
	private GameObject _piscusCreature;

	private Piscus_Creature _activeCreature;

	[SerializeField]
	private float _spawnDistance = 60f;

	private float _territoryRadius;

	[SerializeField]
	private bool _canRespawn;

	[SerializeField]
	private int _respawnHours;

	private int _respawningTimer;

	private float _lastPollTime;

	public float SpawnDistance
	{
		get
		{
			return _spawnDistance;
		}
		set
		{
			_spawnDistance = value;
		}
	}

	private void Start()
	{
		transform.position = new Vector3(transform.position.x, -1f, transform.position.z);
		if (!LevelLoader.IsLoading())
		{
			Initialize();
		}
	}

	public override void OnCustomMapLoaded()
	{
		base.OnCustomMapLoaded();
		Initialize();
	}

	private void Initialize()
	{
		if (!Game.Mode.IsClient())
		{
			if (Singleton<GameTime>.Instance != null)
			{
				Singleton<GameTime>.Instance.HourTick += GameTime_HourTick;
			}
			CheckInsideTerrain();
			_territoryRadius = _spawnDistance - 15f;
			float repeatRate = 5f;
			InvokeRepeating("PollActivation_InvokeRepeating", 0f, repeatRate);
			PollSpawn();
		}
	}

	private void OnDestroy()
	{
		if (Singleton<GameTime>.Instance != null)
		{
			Singleton<GameTime>.Instance.HourTick -= GameTime_HourTick;
		}
		if (_activeCreature != null)
		{
			MultiplayerMng.DestroyMultiplayerEntity(_activeCreature.gameObject);
		}
	}

	private void GameTime_HourTick()
	{
		if (_activeCreature == null)
		{
			_respawningTimer++;
			PollSpawn();
		}
	}

	private void PollActivation_InvokeRepeating()
	{
		if (!LevelLoader.IsServerJoinInProgress)
		{
			IPlayer player = FindClosestPlayer(transform.position, includeDeadPlayers: true);
			if (player != null && _activeCreature != null)
			{
				bool activate = Vector3Tools.DistanceOnAxis(transform.position, player.transform.position, Vector3Tools.xz) < _spawnDistance;
				Activate_Creature(activate);
			}
		}
	}

	private void PollSpawn()
	{
		IPlayer player = FindClosestPlayer(transform.position, includeDeadPlayers: true);
		if (player != null)
		{
			bool flag = Vector3Tools.DistanceOnAxis(transform.position, player.transform.position, Vector3Tools.xz) < _spawnDistance;
			if ((_canRespawn && !flag && _respawningTimer > _respawnHours) || _respawningTimer == 0)
			{
				_respawningTimer = 0;
				GameObject gameObject = MultiplayerMng.Instantiate<SaveablePrefab>(_piscusCreature.GetComponent<SaveablePrefab>().PrefabId, transform.position, Quaternion.identity, MiniGuid.New()).gameObject;
				gameObject.transform.parent = Singleton<PiscusInspectionManager>.Instance.transform;
				_activeCreature = gameObject.GetComponent<Piscus_Creature>();
				_activeCreature.BiomeSize = _territoryRadius;
				_activeCreature.EngagePlayerDistance = _activeCreature.BiomeSize * 0.5f;
				_activeCreature.Manager = this;
			}
		}
	}

	private void Activate_Creature(bool activate)
	{
		_lastPollTime = 0f;
		if (_activeCreature.gameObject.activeSelf != activate)
		{
			_activeCreature.gameObject.GetComponent<SaveablePrefab>().Active = activate;
			if (activate)
			{
				_activeCreature.CurrentState = Piscus_Creature.PiscusBehaviour.Explore;
			}
		}
	}

	private void CheckInsideTerrain()
	{
		if (Physics.CheckSphere(transform.position, 2f, 1 << Layers.TERRAIN) || Physics.Raycast(transform.position + Vector3.up * 5f, -Vector3.up, 7f, 1 << Layers.TERRAIN))
		{
			Debug.Log("Manager :: Piscus Manager Destroyed :: Collision with terrain");
			MultiplayerMng.DestroyMultiplayerEntity(gameObject);
		}
	}

	private IPlayer FindClosestPlayer(Vector3 position, bool includeDeadPlayers)
	{
		return PlayerUtilities.FindClosestPlayer(position, includeDeadPlayers);
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		JObject data = JSerializer.Serialize(transform);
		jObject.AddField("Transform", data);
		jObject.AddField("respawnCounter", JSerializer.Serialize(_respawningTimer));
		jObject.AddField("spawnDistance", JSerializer.Serialize(_spawnDistance));
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		if (data != null && !data.IsNull())
		{
			JSerializer.DeserializeInto(data.GetField("Transform"), transform);
			CompleteTransformSerialization();
			int value = data.GetField("respawnCounter").GetValue<int>();
			_respawningTimer = value;
			int value2 = data.GetField("spawnDistance").GetValue<int>();
			_spawnDistance = value2;
			float x = transform.localPosition.x;
			float z = transform.localPosition.z;
			float y = transform.localPosition.y;
			if (Mathf.Abs(x) == 100f && Mathf.Abs(z) == 100f)
			{
				x = ((!(x > 0f)) ? (-88f) : 88f);
				z = ((!(z > 0f)) ? (-88f) : 88f);
				transform.localPosition = new Vector3(x, y, z);
			}
		}
		Initialize();
	}
}
