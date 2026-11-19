using System.Collections.Generic;
using Beam.Language;
using Beam.Obfuscated.Users;
using Beam.Utilities;
using Funlabs;
using UnityEngine;
using UnityEngine.UI;

namespace Beam.UI;

public class TMPBeepiWatchViewAdapter : WatchViewAdapterBase
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private GameObjectViewAdapter[] _watchModes;

	[SerializeField]
	private TransformViewAdapter _statusLabelsContainer;

	[SerializeField]
	private TMPLabelViewAdapter _statusLabelPrefab;

	[SerializeField]
	private TransformViewAdapter _statisticsNamesLabelsContainer;

	[SerializeField]
	private TransformViewAdapter _statisticsValuesLabelsContainer;

	[SerializeField]
	private TMPLabelViewAdapter _statisticsValuesLabelPrefab;

	[SerializeField]
	private TMPLabelViewAdapter _statisticsNamesLabelPrefab;

	private List<string> statisticsNames = new List<string>();

	private List<TMPLabelViewAdapter> statisticsLabels = new List<TMPLabelViewAdapter>();

	[SerializeField]
	private UImageViewAdapter _healthIcon;

	[SerializeField]
	private UImageViewAdapter _hungerIcon;

	[SerializeField]
	private UImageViewAdapter _thirstIcon;

	[SerializeField]
	private UImageViewAdapter _sleepIcon;

	[SerializeField]
	private USliderViewAdapter _healthBar;

	[SerializeField]
	private USliderViewAdapter _hungerBar;

	[SerializeField]
	private USliderViewAdapter _thirstBar;

	[SerializeField]
	private USliderViewAdapter _sleepBar;

	[SerializeField]
	private USliderViewAdapter _huntingBar;

	[SerializeField]
	private USliderViewAdapter _cookingBar;

	[SerializeField]
	private USliderViewAdapter _harvestingBar;

	[SerializeField]
	private USliderViewAdapter _physicalBar;

	[SerializeField]
	private USliderViewAdapter _craftsmanshipBar;

	[Space]
	[SerializeField]
	private TMPLabelViewAdapter _huntingLabel;

	[SerializeField]
	private TMPLabelViewAdapter _cookingLabel;

	[SerializeField]
	private TMPLabelViewAdapter _harvestingLabel;

	[SerializeField]
	private TMPLabelViewAdapter _physicalLabel;

	[SerializeField]
	private TMPLabelViewAdapter _craftsmanshipLabel;

	[Space]
	[SerializeField]
	private TMPLabelViewAdapter _clockLabel;

	[SerializeField]
	private TMPLabelViewAdapter _dateLabel;

	[SerializeField]
	private TMPLabelViewAdapter _temperatureLabel;

	[SerializeField]
	private TMPLabelViewAdapter _daysLabel;

	[SerializeField]
	private TMPLabelViewAdapter _uvIndexLabel;

	[SerializeField]
	private UImageViewAdapter _temperatureImage;

	[SerializeField]
	private Sprite _temperatureSprite;

	[SerializeField]
	private Sprite _heatwaveSprite;

	private bool _celsius;

	private int _currentMode = -1;

	private WatchController _controller;

	private int[] _lastStatsValues = new int[10];

	public override Camera Camera => _camera;

	private void Start()
	{
		statisticsNames.Add("STATS_DAYS_SURVIVED");
		statisticsNames.Add("STATS_ITEMS_CRAFTED");
		statisticsNames.Add("STATS_ANIMALS_KILLED");
		statisticsNames.Add("STATS_ISLANDS_EXPLORED");
		statisticsNames.Add("STATS_SHIPS_SCAVENGED");
		foreach (string statisticsName in statisticsNames)
		{
			_ = statisticsName;
			TMPLabelViewAdapter tMPLabelViewAdapter = Object.Instantiate(_statisticsValuesLabelPrefab);
			statisticsLabels.Add(tMPLabelViewAdapter);
			tMPLabelViewAdapter.transform.SetParent(_statisticsValuesLabelsContainer.Transform, worldPositionStays: false);
			tMPLabelViewAdapter.Text = "0";
		}
		if ((Localization.GetLanguageHandler() as LocalizationHandler).GetCurrentLanguage() == SystemLanguage.Russian)
		{
			_statisticsValuesLabelsContainer.Transform.GetComponent<VerticalLayoutGroup>().spacing = 11f;
		}
		RenderTexture renderTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.anisoLevel = 7;
		renderTexture.useMipMap = false;
		renderTexture.wrapMode = TextureWrapMode.Clamp;
		_camera.targetTexture = renderTexture;
	}

	public override void SetController(WatchController controller)
	{
		_controller = controller;
	}

	private void OnDestroy()
	{
		BeamTeam.Destroy(_camera.targetTexture);
		_camera.targetTexture = null;
	}

	public override void Show()
	{
		_camera.enabled = true;
		SetWatchMode(_currentMode);
		base.Show();
	}

	public override void Hide()
	{
		_camera.enabled = false;
		base.Hide();
	}

	public override void SetTemperature(float temperature)
	{
		float num = (_celsius ? temperature.ToCelsius() : temperature);
		_temperatureLabel.Text = num.ToString("F1").PadLeft(5, ' ') + (_celsius ? "C" : "F");
	}

	public override void SetTemperatureUnits(Temperature temperature)
	{
		_celsius = temperature == Temperature.CELSIUS;
	}

	public override void SetHeatwave(bool heatwave)
	{
		_temperatureImage.Sprite = (heatwave ? _heatwaveSprite : _temperatureSprite);
	}

	public override void SetUVIndex(string uvIndex)
	{
		_uvIndexLabel.Text = uvIndex;
	}

	public override void SetClock(string clock)
	{
		_clockLabel.Text = clock;
	}

	public override void SetDate(string date)
	{
		_dateLabel.Text = date;
	}

	public override void SetDays(string days)
	{
		_daysLabel.Text = days + " DAYS SURVIVED";
	}

	public override void SetStatistics(Stats stats)
	{
		_lastStatsValues[0] = stats.DaysSurvived;
		_lastStatsValues[1] = stats.ItemsCrafted;
		_lastStatsValues[2] = stats.AnimalsKilled;
		_lastStatsValues[3] = stats.IslandsExplored;
		_lastStatsValues[4] = stats.ShipsScavenged;
		for (int i = 0; i < statisticsLabels.Count; i++)
		{
			statisticsLabels[i].Text = _lastStatsValues[i].ToString().ToUpper();
		}
	}

	public override void SetHealth(float health)
	{
		_healthBar.NormalizedValue = health;
	}

	public override void SetHunger(float hunger)
	{
		_hungerBar.NormalizedValue = hunger;
	}

	public override void SetThirst(float thirst)
	{
		_thirstBar.NormalizedValue = thirst;
	}

	public override void SetSleep(float sleep)
	{
		_sleepBar.NormalizedValue = sleep;
	}

	public override void SetHunting(float hunting)
	{
		_huntingLabel.Enabled = hunting <= 0f;
		_huntingBar.NormalizedValue = hunting;
	}

	public override void SetCooking(float cooking)
	{
		_cookingLabel.Enabled = cooking <= 0f;
		_cookingBar.NormalizedValue = cooking;
	}

	public override void SetHarvesting(float harvesting)
	{
		_harvestingLabel.Enabled = harvesting <= 0f;
		_harvestingBar.NormalizedValue = harvesting;
	}

	public override void SetPhysical(float physical)
	{
		_physicalLabel.Enabled = physical <= 0f;
		_physicalBar.NormalizedValue = physical;
	}

	public override void SetCraftsmanship(float craftsmanship)
	{
		_craftsmanshipLabel.Enabled = craftsmanship <= 0f;
		_craftsmanshipBar.NormalizedValue = craftsmanship;
	}

	public override void SetWatchMode(int mode)
	{
		if (_currentMode == mode && _currentMode != 3 && _currentMode != 2)
		{
			return;
		}
		_currentMode = mode;
		for (int i = 0; i < _watchModes.Length; i++)
		{
			bool active = i == _currentMode;
			_watchModes[i].Active = active;
		}
		if (_currentMode == 3)
		{
			int num = 0;
			Zone[] zones = Singleton<StrandedWorld>.Instance.Zones;
			foreach (Zone zone in zones)
			{
				if ((Game.Mode.IsMaster() && zone.HasVisited) || (Game.Mode.IsClient() && zone.HasVisited))
				{
					num++;
				}
			}
			CreateStatusLabels(statisticsNames, _statisticsNamesLabelsContainer, _statisticsNamesLabelPrefab, tryUpdate: true);
		}
		else if (_currentMode == 2)
		{
			IList<string> statusEffectsWatch = _controller.Player.Statistics.GetStatusEffectsWatch();
			if (statusEffectsWatch != null)
			{
				CreateStatusLabels(statusEffectsWatch, _statusLabelsContainer, _statusLabelPrefab, tryUpdate: true);
			}
		}
	}

	public override void CycleWatchModeForward()
	{
		int num = _currentMode + 1;
		if ((Game.Mode.IsClient() || (Game.Mode.IsLocalCoop() && _controller.Player.Id > 0)) && num == 3)
		{
			num++;
		}
		if (num > _watchModes.Length - 1)
		{
			num = 0;
		}
		SetWatchMode(num);
	}

	public override void CycleWatchModeBackward()
	{
		int num = _currentMode - 1;
		if (num < 0)
		{
			num = _watchModes.Length - 1;
		}
		if ((Game.Mode.IsClient() || (Game.Mode.IsLocalCoop() && _controller.Player.Id > 0)) && num == 3)
		{
			num--;
		}
		SetWatchMode(num);
	}

	public void CreateStatusLabels<T>(IList<T> labels, TransformViewAdapter container, TMPLabelViewAdapter prefab, bool tryUpdate = false)
	{
		if (tryUpdate && labels.Count == container.Transform.childCount)
		{
			int num = 0;
			for (num = 0; num < container.Transform.childCount; num++)
			{
				TMPLabelViewAdapter component = container.Transform.GetChild(num).gameObject.GetComponent<TMPLabelViewAdapter>();
				if (component == null)
				{
					break;
				}
				component.Text = labels[num].ToString().ToUpper();
			}
			if (num == container.Transform.childCount)
			{
				return;
			}
		}
		BeamTeam.DestroyAllChildren(container);
		foreach (T label in labels)
		{
			TMPLabelViewAdapter tMPLabelViewAdapter = Object.Instantiate(prefab);
			tMPLabelViewAdapter.transform.SetParent(container.Transform, worldPositionStays: false);
			tMPLabelViewAdapter.Text = label.ToString().ToUpper();
			FormatLabelAutoSizing(tMPLabelViewAdapter, 17f, 8f);
		}
	}

	private void FormatLabelAutoSizing(TMPLabelViewAdapter label, float maxSize, float minSize)
	{
		label.EnableAutoSizing = true;
		label.FontSizeMax = maxSize;
		label.FontSizeMin = minSize;
		label.EnableWordWrapping = false;
	}
}
