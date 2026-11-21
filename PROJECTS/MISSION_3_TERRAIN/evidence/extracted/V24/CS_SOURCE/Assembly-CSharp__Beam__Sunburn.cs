using Beam.Serialization.Json;
using UnityEngine;

namespace Beam;

public class Sunburn : ScenePlayerEffect, IIdleUntilInitialized
{
	private const float SUNSTROKE_REMOVE_TIME_HOURS = 0.5f;

	[SerializeField]
	private Player _player;

	[SerializeField]
	private LayerMask _shelterLayerMask;

	[SerializeField]
	private FloatUnityEvent _onUpdateBurn;

	private float _sunExposureTime;

	private float _lastNotification;

	private bool _burnt;

	private bool _initialized;

	public override string Name { get; } = "Sunburn";

	public bool Protected { get; set; }

	public float SunExposureTime
	{
		get
		{
			if (PlayerUtilities.GetServerGameDifficulty() != EGameDifficultyMode.Creative)
			{
				return _sunExposureTime;
			}
			return 0f;
		}
	}

	public bool IsInitialized => _initialized;

	public void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			ResetExposure();
			if (Game.State != GameState.INTRO && PlayerUtilities.GetServerGameDifficulty() != EGameDifficultyMode.Creative)
			{
				_player.Statistics.PlayerEffectApplied += PlayerStatistics_PlayerEffectApplied;
				_player.Statistics.PlayerEffectRemoved += PlayerStatistics_PlayerEffectRemoved;
				Singleton<GameTime>.Instance.HourChanged += GameTime_HourChanged;
				InvokeRepeating("DoProtectionRaycast", 0f, 1f);
			}
		}
	}

	private void OnDestroy()
	{
		if (_player != null && _player.Statistics != null)
		{
			_player.Statistics.PlayerEffectApplied -= PlayerStatistics_PlayerEffectApplied;
			_player.Statistics.PlayerEffectRemoved -= PlayerStatistics_PlayerEffectRemoved;
		}
		if (Singleton<GameTime>.Instance != null)
		{
			Singleton<GameTime>.Instance.HourChanged -= GameTime_HourChanged;
		}
	}

	private void PlayerStatistics_PlayerEffectApplied(PlayerEffect effect)
	{
		if (effect.Name == "SUNBLOCK")
		{
			ResetExposure();
		}
		else if (effect.Name == "SUNSTROKE")
		{
			_burnt = true;
			if (Time.time - _lastNotification > 30f)
			{
				IPlayerSpeech playerSpeech = _player.PlayerSpeech;
				(playerSpeech ?? NullObject<NullPlayerSpeech>.Instance).Play("STATUS_SUNBURN", _player.Gender, _player.Movement.IsUnderwater);
			}
			_lastNotification = Time.time;
		}
	}

	private void PlayerStatistics_PlayerEffectRemoved(PlayerEffect effect)
	{
		if (effect.Name == "SUNSTROKE")
		{
			_burnt = false;
		}
	}

	private void GameTime_HourChanged(float hours)
	{
		BurnManual(hours);
	}

	public void ResetExposure()
	{
		_sunExposureTime = 0f;
		_onUpdateBurn?.Invoke(0f);
	}

	public bool IsDayTimeUV()
	{
		if (Singleton<GameTime>.Instance.MilitaryTime >= 10f)
		{
			return Singleton<GameTime>.Instance.MilitaryTime <= 15f;
		}
		return false;
	}

	private void Update()
	{
		if (PlayerUtilities.GetServerGameDifficulty() != EGameDifficultyMode.Creative && _initialized && !LevelLoader.IsLoadingScene() && !_player.Statistics.Invincible && !_player.Statistics.HasStatusEffect(StatusEffect.SUNBLOCK))
		{
			BurnManual(GameTime.DeltaTimeHour);
			if (!_burnt && _sunExposureTime >= 1f)
			{
				_player.Statistics.ApplyStatusEffect(StatusEffect.SUNSTROKE);
			}
			else if (_burnt && _sunExposureTime < 1f)
			{
				_player.Statistics.RemoveStatusEffect(StatusEffect.SUNSTROKE);
			}
			_onUpdateBurn?.Invoke(_sunExposureTime);
		}
	}

	private void BurnManual(float hours)
	{
		bool flag = IsDayTimeUV();
		if (flag && !Protected)
		{
			float num = Mathf.Clamp01(1f / UVIndexUtility.GetBurnTime(Singleton<AtmosphereTemperature>.Instance.GetUVIndex()));
			_sunExposureTime += num * hours;
		}
		else if (!flag || Protected)
		{
			_sunExposureTime -= 0.5f * hours;
		}
		_sunExposureTime = Mathf.Clamp01(_sunExposureTime);
	}

	private void DoProtectionRaycast()
	{
		Vector3 direction = Singleton<Atmosphere>.Instance.SunIlluminary.Transform.position - _player.Movement.transform.position;
		Protected = Physics.Raycast(_player.Movement.transform.position, direction, 100f, _shelterLayerMask.value, QueryTriggerInteraction.Collide);
		Protected |= _player.Movement.IsUnderwater;
		bool flag = (bool)Singleton<AtmosphereStorm>.Instance && (bool)Singleton<AtmosphereStorm>.Instance.CurrentWeatherEvent && Singleton<AtmosphereStorm>.Instance.CurrentWeatherEvent.Humidity > 90f && Singleton<AtmosphereStorm>.Instance.CurrentWeatherEvent.IsActive;
		Protected |= flag;
	}

	public override JObject Save()
	{
		JObject jObject = new JObject();
		jObject.AddField("BurnTimer", _sunExposureTime);
		return jObject;
	}

	public override void Load(JObject data)
	{
		JObject field = data.GetField("BurnTimer");
		if (field != null)
		{
			float num = field.GetValue<float>();
			if (num > 1f)
			{
				num = 0f;
			}
			_sunExposureTime = num;
		}
	}
}
