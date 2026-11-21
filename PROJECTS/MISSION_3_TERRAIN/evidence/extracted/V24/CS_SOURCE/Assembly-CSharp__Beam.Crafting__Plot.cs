using System;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using Photon.Bolt;
using UnityEngine;

namespace Beam.Crafting;

[RequireComponent(typeof(Farming_PLOT))]
public class Plot : MultiplayerBehaviour, IMultiplayerState<IPlotState>
{
	public class ReplicateKillCrop : MultiplayerMessage
	{
		[Replicate]
		public SaveablePrefab Prefab;

		public override void OnPeer()
		{
			Prefab.GetComponent<Plot>().KillCrop();
		}
	}

	public class ReplicatePoll : MultiplayerMessage
	{
		[Replicate]
		public SaveablePrefab Prefab;

		public override void OnPeer()
		{
			Prefab.GetComponent<Plot>().PollPlant_InvokeRepeating();
		}
	}

	private const float DEPLETE_INTERVAL_HOURS = 24f;

	[SerializeField]
	private int _maxWater = 4;

	[SerializeField]
	private Transform _plantPosition;

	private IPlantFactory _plantFactory;

	private Plant _plant;

	private bool _growing;

	private bool _isRaining;

	private int _water = 1;

	public bool Growing => _growing;

	public IPlotState MultiplayerState => GetMultiplayerState<IPlotState>();

	public event Action<PlantType> CropCreated;

	public bool HasPlants()
	{
		return _plant != null;
	}

	public float GetNormalizedWater()
	{
		return (float)_water / (float)_maxWater;
	}

	protected virtual void OnCropCreated(PlantType type)
	{
		CropCreated?.Invoke(type);
	}

	private void Awake()
	{
		FarmingManager plantFactory = UnityEngine.Object.FindObjectOfType<FarmingManager>();
		_plantFactory = plantFactory;
	}

	private void Start()
	{
		if (!Game.Mode.IsClient() && Singleton<AtmosphereStorm>.Instance != null)
		{
			Singleton<AtmosphereStorm>.Instance.WeatherStarted += AtmosphereStorm_WeatherStarted;
			Singleton<AtmosphereStorm>.Instance.WeatherStopped += AtmosphereStorm_WeatherStopped;
			if (Singleton<AtmosphereStorm>.Instance.CurrentWeatherEvent != null)
			{
				AtmosphereStorm_WeatherStarted(Singleton<AtmosphereStorm>.Instance.CurrentWeatherEvent);
			}
		}
	}

	private void OnDestroy()
	{
		CropCreated = null;
		if (!Game.Mode.IsClient() && Singleton<AtmosphereStorm>.Instance != null)
		{
			Singleton<AtmosphereStorm>.Instance.WeatherStarted -= AtmosphereStorm_WeatherStarted;
			Singleton<AtmosphereStorm>.Instance.WeatherStopped -= AtmosphereStorm_WeatherStopped;
		}
	}

	private void AtmosphereStorm_WeatherStarted(WeatherEvent weather)
	{
		if (Mathf.Approximately(weather.Humidity, 100f))
		{
			_isRaining = true;
			int num = 1;
			InvokeRepeating("PollRain_InvokeRepeating", GameTime.OneGameHour, (float)num * GameTime.OneGameHour);
		}
	}

	private void AtmosphereStorm_WeatherStopped()
	{
		_isRaining = false;
		CancelInvoke("PollRain_InvokeRepeating");
	}

	public void Place()
	{
	}

	public bool Plant(Plantable plantable)
	{
		if (_growing)
		{
			return false;
		}
		ClearCrop();
		if (_water < 1)
		{
			_water = 1;
		}
		CreateCrop(plantable.PlantType);
		Initialize();
		return true;
	}

	private void CreateCrop(PlantType plantType)
	{
		_growing = true;
		_plant = _plantFactory.CreatePlant(plantType);
		_plant.transform.parent = gameObject.transform;
		_plant.transform.position = _plantPosition.position;
		OnCropCreated(plantType);
	}

	private void Initialize()
	{
		if (Game.Mode.IsMaster())
		{
			CancelInvoke("PollWater_InvokeRepeating");
			CancelInvoke("PollPlant_InvokeRepeating");
			InvokeRepeating("PollWater_InvokeRepeating", 24f * GameTime.OneGameHour, 24f * GameTime.OneGameHour);
			InvokeRepeating("PollPlant_InvokeRepeating", 3f, GameTime.OneGameHour);
		}
	}

	private void PollRain_InvokeRepeating()
	{
		if (!LevelLoader.IsLoading(gameObject) && !LevelLoader.IsServerJoinInProgress && _isRaining)
		{
			_water++;
			_water = Mathf.Clamp(_water, 0, _maxWater);
		}
	}

	private void PollPlant_InvokeRepeating()
	{
		if (!LevelLoader.IsLoading() && !LevelLoader.IsServerJoinInProgress && _growing && !(_plant == null))
		{
			if (Game.Mode.IsServer())
			{
				ReplicatePoll replicatePoll = new ReplicatePoll();
				replicatePoll.Prefab = GetComponent<SaveablePrefab>();
				replicatePoll.Post();
			}
			_plant.Poll();
		}
	}

	private void PollWater_InvokeRepeating()
	{
		if (!LevelLoader.IsLoading() && !LevelLoader.IsServerJoinInProgress && !_isRaining)
		{
			_water--;
			_water = Mathf.Clamp(_water, 0, _maxWater);
			if (_water <= 0)
			{
				ReplicatedKillCrop();
			}
		}
	}

	public bool CanWaterCrop()
	{
		if (_growing)
		{
			return _water < _maxWater;
		}
		return false;
	}

	public bool WaterCrop(int amount)
	{
		if (_growing && _water < _maxWater)
		{
			_water += amount;
			_water = Mathf.Clamp(_water, 0, _maxWater);
			return true;
		}
		return false;
	}

	public bool ClearCrop()
	{
		if (_plant == null)
		{
			return false;
		}
		_plant.ClearFruit();
		_plantFactory.DestroyPlant(_plant);
		_plant = null;
		_growing = false;
		if (Game.Mode.IsMaster())
		{
			CancelInvoke("PollWater_InvokeRepeating");
		}
		return true;
	}

	private void KillCrop()
	{
		if (_growing)
		{
			_plant.Kill();
			_water = 0;
			_growing = false;
			if (Game.Mode.IsMaster())
			{
				CancelInvoke("PollWater_InvokeRepeating");
			}
		}
	}

	protected void FixedUpdate()
	{
		if (Game.Mode.IsMultiplayer())
		{
			if (!entity.IsOwner)
			{
				_water = MultiplayerState.Water;
				_growing = MultiplayerState.Growing;
			}
			else
			{
				MultiplayerState.Water = _water;
				MultiplayerState.Growing = _growing;
			}
		}
	}

	private void ReplicatedKillCrop()
	{
		SaveablePrefab component = GetComponent<SaveablePrefab>();
		if (component != null && component.IsOwner)
		{
			ReplicateKillCrop replicateKillCrop = new ReplicateKillCrop();
			replicateKillCrop.Prefab = GetComponent<SaveablePrefab>();
			replicateKillCrop.Post();
		}
		KillCrop();
	}

	public JObject Save()
	{
		JObject jObject = new JObject();
		jObject.AddField("Water", _water);
		PlantType plantType = ((_plant != null) ? _plant.PlantType : PlantType.NONE);
		jObject.AddField("PlantType", JSerializer.Serialize(plantType));
		if (_plant == null)
		{
			return jObject;
		}
		JObject jObject2 = new JObject();
		jObject2.Add(SavingUtilities.Save(_plant));
		jObject.AddField("Plants", jObject2);
		jObject.AddField("LastPollTime", JSerializer.Serialize(GameTime.Now));
		return jObject;
	}

	public void Load(JObject data)
	{
		JObject field = data.GetField("Water");
		if ((bool)field)
		{
			_water = field.GetValue<int>();
		}
		PlantType plantType = JSerializer.Deserialize<PlantType>(data.GetField("PlantType"));
		if (plantType == PlantType.NONE)
		{
			return;
		}
		CreateCrop(plantType);
		if (_water <= 0)
		{
			ReplicatedKillCrop();
			return;
		}
		JObject data2 = data.GetField("Plants")?.Children.FirstOrDefault_NonAlloc() ?? data.GetField("[0]Plant");
		if (SavingUtilities.Load(_plant, data2))
		{
			JObject field2 = data.GetField("LastPollTime");
			if (field2 != null && !field2.IsNull())
			{
				int num = Mathf.RoundToInt((float)(int)GameTime.GetHoursSince(JSerializer.Deserialize<DateTime>(field2)) / 24f);
				_water -= num;
				if (_water <= 0)
				{
					ReplicatedKillCrop();
					return;
				}
			}
			Initialize();
		}
		else
		{
			ClearCrop();
		}
	}
}
