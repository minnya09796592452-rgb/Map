using System;
using Beam;
using Beam.Events;
using Funlabs;
using UnityEngine;
using kode80.Clouds;

public class AtmosphereClouds : MonoBehaviour
{
	[SerializeField]
	private Shader _standard;

	[SerializeField]
	private Shader _volumetric;

	[SerializeField]
	private Renderer _cloudsRenderer;

	[SerializeField]
	private Gradient _cloudColor;

	[SerializeField]
	public Color _cloudBaseColorMultiplierBlendColor;

	[SerializeField]
	private float _cloudBaseColorMultiplier = 1f;

	public TimeOfDayKeyFrame[] keyFrames;

	[SerializeField]
	private Texture2D _overcastTexture;

	[SerializeField]
	private float _overcastTextureScale = 1f;

	[SerializeField]
	private float _overcastTextureSpeed = 0.2f;

	private Vector2 _overcastTextureDirection;

	private kode80Clouds _volumetricClouds;

	private bool _volumetricCloudsSupported;

	private void Start()
	{
		_volumetricClouds = UnityEngine.Object.FindObjectOfType<kode80Clouds>();
		_volumetricCloudsSupported = SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11");
		if (_volumetricCloudsSupported)
		{
			Singleton<Atmosphere>.Instance.SunsetEvent += Atmosphere_SunsetEvent;
		}
		else
		{
			_volumetricClouds.gameObject.SetActive(value: false);
		}
		LoadOptions();
		EventManager.AddListener<OptionsAppliedEvent>(EventManager_OptionsApplied);
		SortKeyFrames();
	}

	private void OnDestroy()
	{
		EventManager.RemoveListener<OptionsAppliedEvent>(EventManager_OptionsApplied);
		if (_volumetricCloudsSupported && (bool)Singleton<Atmosphere>.Instance)
		{
			Singleton<Atmosphere>.Instance.SunsetEvent -= Atmosphere_SunsetEvent;
		}
	}

	private void EventManager_OptionsApplied(OptionsAppliedEvent e)
	{
		LoadOptions();
	}

	public void LoadOptions()
	{
		switch ((!Game.Mode.IsLocalCoop() && _volumetricCloudsSupported) ? Options.GraphicsSettings.Clouds : Quality_VolumetricClouds.OFF)
		{
		case Quality_VolumetricClouds.OFF:
			_volumetricClouds.enabled = false;
			_cloudsRenderer.sharedMaterial.shader = _standard;
			break;
		case Quality_VolumetricClouds.LOW:
			_volumetricClouds.enabled = true;
			_volumetricClouds.subPixelSize = kode80Clouds.SubPixelSize.Sub4x4;
			_volumetricClouds.maxIterations = 48;
			_cloudsRenderer.sharedMaterial.shader = _volumetric;
			break;
		case Quality_VolumetricClouds.MEDIUM:
			_volumetricClouds.enabled = true;
			_volumetricClouds.subPixelSize = kode80Clouds.SubPixelSize.Sub4x4;
			_volumetricClouds.maxIterations = 64;
			_cloudsRenderer.sharedMaterial.shader = _volumetric;
			break;
		case Quality_VolumetricClouds.HIGH:
			_volumetricClouds.enabled = true;
			_volumetricClouds.subPixelSize = kode80Clouds.SubPixelSize.Sub2x2;
			_volumetricClouds.maxIterations = 64;
			_cloudsRenderer.sharedMaterial.shader = _volumetric;
			break;
		}
	}

	private void Atmosphere_SunsetEvent()
	{
		_volumetricClouds.sunLight = (Singleton<Atmosphere>.Instance.DayTime ? Singleton<Atmosphere>.Instance.SunIlluminary.Light : Singleton<Atmosphere>.Instance.MoonIlluminary.Light);
	}

	private void Update()
	{
		Color value = _cloudColor.Evaluate(Singleton<GameTime>.Instance.NormalizedTime);
		_cloudsRenderer.sharedMaterial.SetColor("_CloudColor", value);
		UpdateVolumetricCloudsColors();
		UpdateVolumetricCloudsLighting();
		Vector2 vector = Wind.Direction;
		vector *= Time.timeSinceLevelLoad * _overcastTextureSpeed;
		Shader.SetGlobalVector("_OvercastTex_Coords", new Vector4(1f, 1f, vector.x, vector.y) * (1f / _overcastTextureScale));
		Shader.SetGlobalFloat("_Overcast", Mathf.Max(Mathf.Pow(Singleton<Atmosphere>.Instance.Altitude, 0.3f)));
		Shader.SetGlobalTexture("_OvercastTex", _overcastTexture ?? Texture2D.whiteTexture);
	}

	private void UpdateVolumetricCloudsColors()
	{
		if (_volumetricCloudsSupported && _volumetricClouds.enabled)
		{
			_volumetricClouds.cloudTopColor = _cloudColor.Evaluate(Singleton<GameTime>.Instance.NormalizedTime);
			_volumetricClouds.cloudBaseColor = UnityEngine.RenderSettings.ambientSkyColor * (_cloudBaseColorMultiplierBlendColor * _cloudBaseColorMultiplier);
		}
	}

	private void UpdateVolumetricCloudsLighting()
	{
		if (_volumetricCloudsSupported && _volumetricClouds.enabled && keyFrames.Length >= 1)
		{
			float x = Singleton<Atmosphere>.Instance.SunIlluminary.Transform.localEulerAngles.x;
			TimeOfDayKeyFrame timeOfDayKeyFrame = KeyFrameBelowAngle(x);
			TimeOfDayKeyFrame timeOfDayKeyFrame2 = KeyFrameAboveAngle(x);
			float num = timeOfDayKeyFrame2.angle - timeOfDayKeyFrame.angle;
			float t = ((num == 0f) ? 0.5f : ((x - timeOfDayKeyFrame.angle) / num));
			_volumetricClouds.sunScalar = Mathf.Lerp(timeOfDayKeyFrame.SunScalar, timeOfDayKeyFrame2.SunScalar, t);
			_volumetricClouds.ambientScalar = Mathf.Lerp(timeOfDayKeyFrame.AmbientScalar, timeOfDayKeyFrame2.AmbientScalar, t);
		}
	}

	public void SortKeyFrames()
	{
		Array.Sort(keyFrames, (TimeOfDayKeyFrame a, TimeOfDayKeyFrame b) => a.angle.CompareTo(b.angle));
	}

	private TimeOfDayKeyFrame KeyFrameBelowAngle(float angle)
	{
		return KeyFrameForAngle(angle, findGreaterThan: false);
	}

	private TimeOfDayKeyFrame KeyFrameAboveAngle(float angle)
	{
		return KeyFrameForAngle(angle, findGreaterThan: true);
	}

	private TimeOfDayKeyFrame KeyFrameForAngle(float angle, bool findGreaterThan)
	{
		if (keyFrames.Length != 0)
		{
			TimeOfDayKeyFrame timeOfDayKeyFrame = keyFrames[0];
			TimeOfDayKeyFrame timeOfDayKeyFrame2 = keyFrames[keyFrames.Length - 1];
			if (angle <= timeOfDayKeyFrame.angle)
			{
				return timeOfDayKeyFrame;
			}
			if (angle >= timeOfDayKeyFrame2.angle)
			{
				return timeOfDayKeyFrame2;
			}
			for (int i = 0; i < keyFrames.Length; i++)
			{
				timeOfDayKeyFrame = keyFrames[i];
				timeOfDayKeyFrame2 = ((i < keyFrames.Length - 1) ? keyFrames[i + 1] : timeOfDayKeyFrame);
				if (angle >= timeOfDayKeyFrame.angle && angle <= timeOfDayKeyFrame2.angle)
				{
					if (!findGreaterThan)
					{
						return timeOfDayKeyFrame;
					}
					return timeOfDayKeyFrame2;
				}
			}
		}
		return null;
	}
}
