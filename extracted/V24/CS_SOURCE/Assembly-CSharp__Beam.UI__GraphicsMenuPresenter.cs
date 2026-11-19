using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beam.Utilities;
using Beam.Utilities.Reflection;
using Funlabs;
using UnityEngine;

namespace Beam.UI;

public class GraphicsMenuPresenter : MonoBehaviour, IOptionPanel
{
	[SerializeField]
	private OptionsMenuPresenter _optionsMenuPresenter;

	private IGraphicsMenuViewAdapter _view;

	private GraphicsSettings _settings;

	private bool _presetChange;

	public Transform GetCategoriesContainer()
	{
		return _view.CategoriesContainer;
	}

	public void Initialize(IGraphicsMenuViewAdapter view)
	{
		_view = view;
		PopulateControls();
	}

	public void Show()
	{
		if (!_view.Visible)
		{
			_view.Show();
			_optionsMenuPresenter.SetApplyButtonEvent(GraphicsMenuApplyButton_Click);
			_settings = Options.GraphicsSettings.ShallowCopy();
			SetPresetControlSelection();
			SetResolutionControlSelection();
			SetControlsSelections(_settings);
			WireControlEvents();
		}
	}

	public void Hide()
	{
		if (_view.Visible)
		{
			_view.Hide();
			UnwireControlEvents();
		}
	}

	public void Apply()
	{
		GraphicsMenuApplyButton_Click(null);
	}

	private void WireControlEvents()
	{
		_view.PresetButton.Click += PresetButton_Click;
		_view.PresetButton.SelectionChanged += PresetButton_SelectionChanged;
		WireWidgetEvents(_view.ResolutionButton);
		WireWidgetEvents(_view.FixedFrameRateButton);
		WireWidgetEvents(_view.VerticalSyncButton);
		WireWidgetEvents(_view.FullScreenButton);
		WireWidgetEvents(_view.DrawDistanceButton);
		WireWidgetEvents(_view.TextureQualityButton);
		WireWidgetEvents(_view.ShadowQualityButton);
		WireWidgetEvents(_view.BillboardDensityButton);
		_view.ResolutionButton.SelectionChanged += ResolutionButton_Changed;
		_view.FixedFrameRateButton.SelectionChanged += FixedFrameRateButton_Changed;
		_view.VerticalSyncButton.SelectionChanged += VerticalSyncButton_Changed;
		_view.FullScreenButton.SelectionChanged += FullScreenButton_Changed;
		_view.ColorModeButton.SelectionChanged += OptionButton_Changed;
		_view.FieldOfViewSlider.ValueChanged += FieldOfViewSlider_Changed;
		_view.DrawDistanceButton.SelectionChanged += OptionButton_Changed;
		_view.TextureQualityButton.SelectionChanged += OptionButton_Changed;
		_view.ShadowQualityButton.SelectionChanged += OptionButton_Changed;
		_view.BillboardDensityButton.SelectionChanged += OptionButton_Changed;
		WireWidgetEvents(_view.GrassButton);
		WireWidgetEvents(_view.CloudsButton);
		_view.GrassButton.SelectionChanged += OptionButton_Changed;
		_view.CloudsButton.SelectionChanged += OptionButton_Changed;
		WireWidgetEvents(_view.WaterQualityButton);
		WireWidgetEvents(_view.CausticsButton);
		_view.WaterQualityButton.SelectionChanged += OptionButton_Changed;
		_view.CausticsButton.SelectionChanged += OptionButton_Changed;
		WireWidgetEvents(_view.AmbientOcclusionButton);
		WireWidgetEvents(_view.AntialiasingButton);
		WireWidgetEvents(_view.BloomButton);
		WireWidgetEvents(_view.LensEffectsButton);
		WireWidgetEvents(_view.MotionBlurButton);
		_view.AmbientOcclusionButton.SelectionChanged += OptionButton_Changed;
		_view.AntialiasingButton.SelectionChanged += OptionButton_Changed;
		_view.BloomButton.SelectionChanged += OptionButton_Changed;
		_view.LensEffectsButton.SelectionChanged += OptionButton_Changed;
		_view.MotionBlurButton.SelectionChanged += OptionButton_Changed;
	}

	private void UnwireControlEvents()
	{
		_view.PresetButton.Click -= PresetButton_Click;
		_view.PresetButton.SelectionChanged -= PresetButton_SelectionChanged;
		UnwireWidgetEvents(_view.ResolutionButton);
		UnwireWidgetEvents(_view.FixedFrameRateButton);
		UnwireWidgetEvents(_view.VerticalSyncButton);
		UnwireWidgetEvents(_view.FullScreenButton);
		UnwireWidgetEvents(_view.DrawDistanceButton);
		UnwireWidgetEvents(_view.TextureQualityButton);
		UnwireWidgetEvents(_view.ShadowQualityButton);
		UnwireWidgetEvents(_view.BillboardDensityButton);
		_view.ResolutionButton.SelectionChanged -= ResolutionButton_Changed;
		_view.FixedFrameRateButton.SelectionChanged -= FixedFrameRateButton_Changed;
		_view.VerticalSyncButton.SelectionChanged -= VerticalSyncButton_Changed;
		_view.FullScreenButton.SelectionChanged -= FullScreenButton_Changed;
		_view.ColorModeButton.SelectionChanged -= OptionButton_Changed;
		_view.FieldOfViewSlider.ValueChanged -= FieldOfViewSlider_Changed;
		_view.DrawDistanceButton.SelectionChanged -= OptionButton_Changed;
		_view.TextureQualityButton.SelectionChanged -= OptionButton_Changed;
		_view.ShadowQualityButton.SelectionChanged -= OptionButton_Changed;
		_view.BillboardDensityButton.SelectionChanged -= OptionButton_Changed;
		UnwireWidgetEvents(_view.GrassButton);
		UnwireWidgetEvents(_view.CloudsButton);
		_view.GrassButton.SelectionChanged -= OptionButton_Changed;
		_view.CloudsButton.SelectionChanged -= OptionButton_Changed;
		UnwireWidgetEvents(_view.WaterQualityButton);
		UnwireWidgetEvents(_view.CausticsButton);
		_view.WaterQualityButton.SelectionChanged -= OptionButton_Changed;
		_view.CausticsButton.SelectionChanged -= OptionButton_Changed;
		UnwireWidgetEvents(_view.AmbientOcclusionButton);
		UnwireWidgetEvents(_view.AntialiasingButton);
		UnwireWidgetEvents(_view.BloomButton);
		UnwireWidgetEvents(_view.LensEffectsButton);
		UnwireWidgetEvents(_view.MotionBlurButton);
		_view.AmbientOcclusionButton.SelectionChanged -= OptionButton_Changed;
		_view.AntialiasingButton.SelectionChanged -= OptionButton_Changed;
		_view.BloomButton.SelectionChanged -= OptionButton_Changed;
		_view.LensEffectsButton.SelectionChanged -= OptionButton_Changed;
		_view.MotionBlurButton.SelectionChanged -= OptionButton_Changed;
	}

	private void PopulateControls()
	{
		IEnumerable<DescriptionAttribute> attributes = typeof(Quality_Preset).GetAttributes<DescriptionAttribute>();
		_view.PresetButton.Initialize(attributes);
		IEnumerable<IOptionElementData> items = (from res in new[]
			{
				new
				{
					Width = Screen.width,
					Height = Screen.height
				}
			}.Union(Screen.resolutions.Select((Resolution r) => new
			{
				Width = r.width,
				Height = r.height
			})).Distinct()
			orderby res.Width, res.Height
			select new OptionElementData($"{res.Width} x {res.Height}")).ToList();
		_view.ResolutionButton.Initialize(items);
		_view.FixedFrameRateButton.Initialize(typeof(Quality_FixedFrameRate).GetAttributes<DescriptionAttribute>());
		_view.VerticalSyncButton.Initialize(typeof(Quality_VerticalSync).GetAttributes<DescriptionAttribute>());
		_view.FullScreenButton.Initialize(typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_view.ColorModeButton.Initialize(typeof(Quality_ColorMode).GetAttributes<DescriptionAttribute>());
		_view.DrawDistanceButton.Initialize(typeof(Quality_DrawDistance).GetAttributes<DescriptionAttribute>());
		_view.TextureQualityButton.Initialize(typeof(Quality_Textures).GetAttributes<DescriptionAttribute>());
		_view.ShadowQualityButton.Initialize(typeof(Quality_Shadows).GetAttributes<DescriptionAttribute>());
		_view.BillboardDensityButton.Initialize(typeof(Quality_BillboardDensity).GetAttributes<DescriptionAttribute>());
		_view.GrassButton.Initialize(typeof(Quality_Grass).GetAttributes<DescriptionAttribute>());
		_view.CloudsButton.Initialize(typeof(Quality_VolumetricClouds).GetAttributes<DescriptionAttribute>());
		_view.WaterQualityButton.Initialize(typeof(Quality_LAPTOP_ULTRA).GetAttributes<DescriptionAttribute>());
		_view.WaterReflectionsButton.Initialize(typeof(Quality_OFF_HIGH).GetAttributes<DescriptionAttribute>());
		_view.CausticsButton.Initialize(typeof(Quality_Caustics).GetAttributes<DescriptionAttribute>());
		_view.AmbientOcclusionButton.Initialize(typeof(Quality_AmbientOcclusion).GetAttributes<DescriptionAttribute>());
		_view.AntialiasingButton.Initialize(typeof(Quality_Antialiasing).GetAttributes<DescriptionAttribute>());
		_view.BloomButton.Initialize(typeof(Quality_Bloom).GetAttributes<DescriptionAttribute>());
		_view.LensEffectsButton.Initialize(typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_view.MotionBlurButton.Initialize(typeof(Quality_MotionBlur).GetAttributes<DescriptionAttribute>());
	}

	private void SetPresetControlSelection()
	{
		_view.PresetButton.SelectedIndex = _settings.Preset.GetIndex();
	}

	private void SetResolutionControlSelection()
	{
		string resolution = _settings.Resolution;
		if (string.IsNullOrEmpty(resolution))
		{
			resolution = Screen.width + " x " + Screen.height;
		}
		int num = _view.ResolutionButton.Elements.FindIndex((IOptionElementData r) => r.Text.Equals(resolution, StringComparison.InvariantCultureIgnoreCase));
		if (num != -1)
		{
			_view.ResolutionButton.SelectedIndex = num;
		}
	}

	private void SetControlsSelections(GraphicsSettings settings)
	{
		_view.FixedFrameRateButton.SelectedIndex = settings.FixedFrameRate.GetIndex();
		_view.VerticalSyncButton.SelectedIndex = settings.VerticalSync.GetIndex();
		_view.FullScreenButton.SelectedIndex = ((!Screen.fullScreen) ? Quality_OFF_ON.OFF : Quality_OFF_ON.ON).GetIndex();
		_view.ColorModeButton.SelectedIndex = settings.ColorMode.GetIndex();
		_view.DrawDistanceButton.SelectedIndex = settings.DrawDistance.GetIndex();
		_view.TextureQualityButton.SelectedIndex = settings.TextureQuality.GetIndex();
		_view.ShadowQualityButton.SelectedIndex = settings.ShadowQuality.GetIndex();
		_view.BillboardDensityButton.SelectedIndex = settings.BillboardDensity.GetIndex();
		_view.FieldOfViewSlider.Value = settings.FieldOfView;
		_view.GrassButton.SelectedIndex = settings.Grass.GetIndex();
		_view.CloudsButton.SelectedIndex = settings.Clouds.GetIndex();
		_view.WaterQualityButton.SelectedIndex = settings.WaterQuality.GetIndex();
		_view.WaterReflectionsButton.SelectedIndex = settings.WaterReflections.GetIndex();
		_view.CausticsButton.SelectedIndex = settings.Caustics.GetIndex();
		_view.AntialiasingButton.SelectedIndex = settings.Antialiasing.GetIndex();
		_view.AmbientOcclusionButton.SelectedIndex = settings.AmbientOcclusion.GetIndex();
		_view.BloomButton.SelectedIndex = settings.Bloom.GetIndex();
		_view.LensEffectsButton.SelectedIndex = settings.LensEffects.GetIndex();
		_view.MotionBlurButton.SelectedIndex = settings.MotionBlur.GetIndex();
		CheckFixedFrameRate();
		CheckVerticalSync();
		CheckFullScreen();
		CheckFieldOfView(_view.FieldOfViewSlider.Value);
		CheckMotionBlur();
	}

	private void GraphicsMenuApplyButton_Click(object sender)
	{
		_settings.Preset = (Quality_Preset)_view.PresetButton.SelectedIndex;
		_settings.FixedFrameRate = (Quality_FixedFrameRate)_view.FixedFrameRateButton.SelectedIndex;
		_settings.VerticalSync = (Quality_VerticalSync)_view.VerticalSyncButton.SelectedIndex;
		_settings.FullScreen = (Quality_OFF_ON)_view.FullScreenButton.SelectedIndex;
		_settings.ColorMode = (Quality_ColorMode)_view.ColorModeButton.SelectedIndex;
		_settings.DrawDistance = (Quality_DrawDistance)_view.DrawDistanceButton.SelectedIndex;
		_settings.TextureQuality = (Quality_Textures)_view.TextureQualityButton.SelectedIndex;
		_settings.ShadowQuality = (Quality_Shadows)_view.ShadowQualityButton.SelectedIndex;
		_settings.BillboardDensity = (Quality_BillboardDensity)_view.BillboardDensityButton.SelectedIndex;
		_settings.FieldOfView = _view.FieldOfViewSlider.Value;
		_settings.Grass = (Quality_Grass)_view.GrassButton.SelectedIndex;
		_settings.Clouds = (Quality_VolumetricClouds)_view.CloudsButton.SelectedIndex;
		_settings.WaterQuality = (Quality_LAPTOP_ULTRA)_view.WaterQualityButton.SelectedIndex;
		_settings.WaterReflections = (Quality_OFF_HIGH)_view.WaterReflectionsButton.SelectedIndex;
		_settings.Caustics = (Quality_Caustics)_view.CausticsButton.SelectedIndex;
		_settings.AmbientOcclusion = (Quality_AmbientOcclusion)_view.AmbientOcclusionButton.SelectedIndex;
		_settings.Antialiasing = (Quality_Antialiasing)_view.AntialiasingButton.SelectedIndex;
		_settings.Bloom = (Quality_Bloom)_view.BloomButton.SelectedIndex;
		_settings.LensEffects = (Quality_OFF_ON)_view.LensEffectsButton.SelectedIndex;
		_settings.Resolution = _view.ResolutionButton.SelectedElement.Text;
		if (!Game.Mode.IsLocalCoop())
		{
			_settings.MotionBlur = (Quality_MotionBlur)_view.MotionBlurButton.SelectedIndex;
		}
		GraphicsUtilities.SetFixedFrameRate(_settings.FixedFrameRate);
		GraphicsUtilities.SetVSync(_settings.VerticalSync);
		SetResolution();
		Options.GraphicsSettings = _settings.ShallowCopy();
		_optionsMenuPresenter.Save();
	}

	private void PresetButton_Click(object sender)
	{
		if (_view.PresetButton.SelectedIndex == 5)
		{
			_view.PresetButton.SelectedIndex = Quality_Preset.LAPTOP.GetIndex();
		}
	}

	private void PresetButton_SelectionChanged(IButtonViewAdapter sender)
	{
		if (!_presetChange)
		{
			switch ((Quality_Preset)_view.PresetButton.SelectedIndex)
			{
			case Quality_Preset.LAPTOP:
				_settings = GraphicsPreset.LAPTOP;
				break;
			case Quality_Preset.LOW:
				_settings = GraphicsPreset.LOW;
				break;
			default:
				_settings = GraphicsPreset.MEDIUM;
				break;
			case Quality_Preset.HIGH:
				_settings = GraphicsPreset.HIGH;
				break;
			case Quality_Preset.ULTRA:
				_settings = GraphicsPreset.ULTRA;
				break;
			case Quality_Preset.CUSTOM:
				_settings.Preset = Quality_Preset.CUSTOM;
				_view.PresetButton.SelectedIndex = Quality_Preset.CUSTOM.GetIndex();
				return;
			}
			_presetChange = true;
			SetControlsSelections(_settings);
			_optionsMenuPresenter.SetDirtySettings();
			_presetChange = false;
		}
	}

	private void ResolutionButton_Changed(IOptionWidgetViewAdapter sender)
	{
		_optionsMenuPresenter.SetDirtySettings();
	}

	private void FixedFrameRateButton_Changed(IOptionButtonViewAdapter sender)
	{
		CheckFixedFrameRate();
		OptionButton_Changed(sender);
	}

	private void CheckFixedFrameRate()
	{
		if (_view.FixedFrameRateButton.SelectedIndex == 0)
		{
			_view.VerticalSyncButton.Enabled = true;
			return;
		}
		_view.VerticalSyncButton.SelectedIndex = 0;
		_view.VerticalSyncButton.Enabled = false;
	}

	private void VerticalSyncButton_Changed(IOptionButtonViewAdapter sender)
	{
		CheckVerticalSync();
		OptionButton_Changed(sender);
	}

	private void CheckVerticalSync()
	{
		if (_view.VerticalSyncButton.SelectedIndex == 0)
		{
			_view.FixedFrameRateButton.Enabled = true;
			return;
		}
		_view.FixedFrameRateButton.SelectedIndex = 0;
		_view.FixedFrameRateButton.Enabled = false;
	}

	private void FullScreenButton_Changed(IOptionButtonViewAdapter sender)
	{
		if (Game.State == GameState.MAIN_MENU)
		{
			Quality_OFF_ON selectedIndex = (Quality_OFF_ON)_view.FullScreenButton.SelectedIndex;
			StartCoroutine(WaitForScreenChange(selectedIndex == Quality_OFF_ON.ON));
		}
	}

	private void CheckFullScreen()
	{
		if (Game.State != GameState.MAIN_MENU)
		{
			_view.FullScreenButton.Enabled = false;
		}
	}

	private void CheckMotionBlur()
	{
		if (Game.Mode.IsLocalCoop())
		{
			_view.MotionBlurButton.SelectedIndex = 1;
			_view.MotionBlurButton.Enabled = false;
		}
	}

	private void FieldOfViewSlider_Changed(float value)
	{
		CheckFieldOfView(value);
		_optionsMenuPresenter.SetDirtySettings();
	}

	private void CheckFieldOfView(float value)
	{
		int num = 60 + (int)(value * 30f);
		_view.FieldOfViewSlider.ValueText = $"{num:f1}";
	}

	private void OptionButton_Changed(IOptionButtonViewAdapter sender)
	{
		if (!_presetChange)
		{
			_optionsMenuPresenter.SetTooltip(sender.GetTooltip());
			SetPresetToCustom();
		}
	}

	private void OptionWidget_MouseEnter(ISelectableViewAdapter sender)
	{
		IOptionWidgetViewAdapter optionWidgetViewAdapter = sender as IOptionWidgetViewAdapter;
		if (!optionWidgetViewAdapter.IsNullOrDestroyed())
		{
			_optionsMenuPresenter.SetTooltip(optionWidgetViewAdapter.GetTooltip());
		}
	}

	private void OptionWidget_MouseLeave(ISelectableViewAdapter sender)
	{
		_optionsMenuPresenter.SetTooltip(string.Empty);
	}

	private void OptionWidget_Selected(ISelectableViewAdapter sender)
	{
		_view.Scrollview.FocusElement(sender.transform);
		IOptionWidgetViewAdapter optionWidgetViewAdapter = sender as IOptionWidgetViewAdapter;
		if (!optionWidgetViewAdapter.IsNullOrDestroyed())
		{
			_optionsMenuPresenter.SetTooltip(optionWidgetViewAdapter.GetTooltip());
		}
	}

	private void SetPresetToCustom()
	{
		_optionsMenuPresenter.SetDirtySettings();
		_view.PresetButton.SelectedIndex = Quality_Preset.CUSTOM.GetIndex();
	}

	private void WireWidgetEvents(IOptionWidgetViewAdapter widget)
	{
		widget.MouseEnter += OptionWidget_MouseEnter;
		widget.MouseLeave += OptionWidget_MouseLeave;
		widget.Selected += OptionWidget_Selected;
	}

	private void UnwireWidgetEvents(IOptionWidgetViewAdapter widget)
	{
		widget.MouseEnter -= OptionWidget_MouseEnter;
		widget.MouseLeave -= OptionWidget_MouseLeave;
		widget.Selected -= OptionWidget_Selected;
	}

	private bool TryParseResolution(string resolution, out int width, out int height)
	{
		string[] array = resolution.Split('x');
		if (!int.TryParse(array[0].Trim(), out width))
		{
			width = 0;
			height = 0;
			return false;
		}
		if (!int.TryParse(array[1].Trim(), out height))
		{
			width = 0;
			height = 0;
			return false;
		}
		return true;
	}

	private void SetResolution()
	{
		if (TryParseResolution(_view.ResolutionButton.SelectedElement.Text, out var width, out var height))
		{
			Screen.SetResolution(width, height, Screen.fullScreen);
		}
	}

	private IEnumerator WaitForScreenChange(bool fullscreen)
	{
		int width = Screen.width;
		int height = Screen.height;
		Screen.fullScreen = fullscreen;
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		Screen.SetResolution(width, height, Screen.fullScreen);
	}
}
