using System;
using Beam;
using Beam.Utilities;
using SharpNeatLib.Maths;
using UnityEngine;
using UnityEngine.Rendering;

public class Atmosphere : Singleton<Atmosphere>
{
	[Serializable]
	public class Illuminary
	{
		[SerializeField]
		private GameObject _gameObject;

		private Transform _transform;

		public Light Light;

		public Renderer Renderer;

		[Range(0f, 8f)]
		[SerializeField]
		private float _intensity;

		private float _currentIntensity;

		[SerializeField]
		private Gradient _gradient;

		public GameObject GameObject => _gameObject;

		public Transform Transform
		{
			get
			{
				if (!_transform)
				{
					_transform = _gameObject.transform;
				}
				return _transform;
			}
		}

		public float Intensity => _intensity;

		public float CurrentIntensity
		{
			get
			{
				return _currentIntensity;
			}
			set
			{
				_currentIntensity = value;
			}
		}

		public Gradient Gradient => _gradient;

		public void Enable()
		{
			Light.enabled = true;
			if (Renderer != null)
			{
				Renderer.enabled = true;
			}
		}

		public void Disable()
		{
			Light.enabled = false;
			if (Renderer != null)
			{
				Renderer.enabled = false;
			}
		}
	}

	[SerializeField]
	private Illuminary _sunIlluminary;

	[SerializeField]
	private Illuminary _moonIlluminary;

	[SerializeField]
	private MeshRenderer _skyDome;

	private bool _dayTime;

	private float _latitude;

	private float _longitude;

	private float _date = 180f;

	private float _theta;

	private float _sunOrbitRadius;

	private float _altitude;

	[SerializeField]
	private Gradient _skyColorPolariser;

	[SerializeField]
	private Gradient _ambientSkyColorGradient;

	[SerializeField]
	private Gradient _ambientGroundColorGradient;

	[SerializeField]
	private Gradient _ambientUnderwaterGroundColorGradient;

	[SerializeField]
	[HideInInspector]
	private Texture2D _transmittance;

	[SerializeField]
	[HideInInspector]
	private Texture2D _inscatter;

	private Texture3D _inscatterEncoded;

	[SerializeField]
	[HideInInspector]
	private Texture2D _irradiance;

	[SerializeField]
	private RenderTexture _atmosphereLookupTexture;

	private Material _atmosphereLookupMaterial;

	private Color _ambientLightColor;

	private Color _ambientGroundColor;

	[Range(0f, 1f)]
	[SerializeField]
	private float _mieG = 0.85f;

	private float _lastUpdatedTime = -1f;

	[Range(0f, 0.1f)]
	[SerializeField]
	public float MilitaryTimeDiffToUpdate = 0.005f;

	public Illuminary SunIlluminary => _sunIlluminary;

	public Illuminary MoonIlluminary => _moonIlluminary;

	public Illuminary CurrentIlluminary
	{
		get
		{
			if (!_dayTime)
			{
				return _moonIlluminary;
			}
			return _sunIlluminary;
		}
	}

	public bool DayTime => _dayTime;

	public float Altitude => _altitude;

	public Gradient AmbientGroundColorGradient => _ambientGroundColorGradient;

	public Gradient AmbientUnderwaterGroundColorGradient => _ambientUnderwaterGroundColorGradient;

	public Texture2D Transmittance => _transmittance;

	public Texture2D Inscatter => _inscatter;

	public Texture2D Irradiance => _irradiance;

	public event EmptyEventHandler SunsetEvent;

	private bool CheckUpdate()
	{
		if (Options.GraphicsSettings.ShadowQuality > Quality_Shadows.MEDIUM)
		{
			return true;
		}
		float militaryTime = Singleton<GameTime>.Instance.MilitaryTime;
		if ((double)_lastUpdatedTime < 0.0)
		{
			_lastUpdatedTime = militaryTime;
			return true;
		}
		if (Mathf.PingPong(militaryTime - _lastUpdatedTime, 24f) >= MilitaryTimeDiffToUpdate)
		{
			_lastUpdatedTime = militaryTime;
			return true;
		}
		return false;
	}

	protected virtual void OnSunsetEvent()
	{
		SunsetEvent?.Invoke();
	}

	private void Awake()
	{
		_sunOrbitRadius = _skyDome.transform.localScale.x * 0.99f;
		_inscatterEncoded = ConvertLookUpTable(_inscatter);
		_atmosphereLookupTexture = CreateBRDFTexture();
		_atmosphereLookupMaterial = CreateBRDFMaterial();
		SetStaticShaderUniforms();
		SetMaterialUniforms(_skyDome.sharedMaterial);
		RenderSettings.ambientMode = AmbientMode.Trilight;
		RenderSettings.ambientIntensity = 1f;
	}

	public RenderTexture CreateBRDFTexture()
	{
		RenderTexture renderTexture = (_atmosphereLookupTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGBHalf));
		renderTexture.filterMode = FilterMode.Bilinear;
		renderTexture.useMipMap = true;
		renderTexture.autoGenerateMips = true;
		return renderTexture;
	}

	public Material CreateBRDFMaterial()
	{
		Shader shader = Shader.Find("Beam Team/Atmosphere/Atmosphere BRDF");
		if (shader != null)
		{
			Material material = new Material(shader);
			SetMaterialUniforms(material);
			return material;
		}
		Debug.LogError("Atmosphere::Start: Atmosphere BRDF Shader could not be found.");
		return null;
	}

	private void Start()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(Camera_OnPreCull));
	}

	protected override void OnDestroy()
	{
		BeamTeam.Destroy(_atmosphereLookupMaterial);
		BeamTeam.Destroy(_atmosphereLookupTexture);
		BeamTeam.Destroy(_inscatterEncoded);
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(Camera_OnPreCull));
		base.OnDestroy();
	}

	private void Camera_OnPreCull(Camera camera)
	{
		if (!(camera == null) && (camera.CompareTag("MainCamera") || camera.CompareTag("PlayerCamera")))
		{
			ClampSkyPosition(camera.transform.position);
			CalculateIlluminaryColors(camera);
			CalculateAmbientColor(camera);
		}
	}

	public static Texture3D ConvertLookUpTable(Texture2D texture)
	{
		if ((bool)texture)
		{
			if (texture.height != Mathf.FloorToInt(Mathf.Sqrt(texture.width)))
			{
				Debug.LogWarning("Atmosphere::ConvertLookUpTable: The given 2D texture " + texture.name + " cannot be used as a 3D LUT.");
				return null;
			}
			int num = 16;
			int height = texture.height;
			int num2 = 16;
			Color[] pixels = texture.GetPixels();
			Color[] array = new Color[pixels.Length];
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < height; j++)
				{
					for (int k = 0; k < num2; k++)
					{
						array[i + j * num + k * num * height] = pixels[i + j * num * num2 + k * num];
					}
				}
			}
			Texture3D texture3D = new Texture3D(num, height, num2, TextureFormat.ARGB32, mipChain: false);
			texture3D.wrapMode = TextureWrapMode.Clamp;
			texture3D.filterMode = FilterMode.Bilinear;
			texture3D.SetPixels(array);
			texture3D.Apply();
			return texture3D;
		}
		Debug.LogError("Atmosphere::ConvertLookUpTable(): Couldn't load 3D LUT texture. Texture3D will be disabled.");
		return null;
	}

	private void SetStaticShaderUniforms()
	{
		float value = (float)(new FastRandom(StrandedWorld.WORLD_SEED).NextDouble() * 1000.0);
		Shader.SetGlobalFloat("_RandomSeed", value);
		Shader.SetGlobalFloat("_SkyFade", 0f);
		Shader.SetGlobalFloat("_StormFade", 0f);
	}

	private void SetDynamicShaderUniforms()
	{
		Shader.SetGlobalColor("_SunColor", _sunIlluminary.Light.color.linear);
		Shader.SetGlobalFloat("_SunIntensity", _sunIlluminary.Light.intensity);
		Shader.SetGlobalVector("_SunPosition", _sunIlluminary.Transform.position);
		Shader.SetGlobalVector("_SunDirection", _sunIlluminary.Transform.forward * -1f);
		Shader.SetGlobalVector("_MoonDirection", _moonIlluminary.Transform.forward * -1f);
	}

	public void SetMaterialUniforms(Material material)
	{
		if (!(material == null))
		{
			material.SetTexture("_Transmittance", _transmittance);
			material.SetTexture("_Inscatter", _inscatterEncoded);
			material.SetTexture("_Irradiance", _irradiance);
			material.SetFloat("_MieG", _mieG);
			material.SetFloat("Rg", 6360000f);
			material.SetFloat("Rt", 6420000f);
			material.SetFloat("Rl", 6421000f);
			material.SetFloat("RES_R", 16f);
			material.SetFloat("RES_MU", 16f);
			material.SetFloat("RES_MU_S", 16f);
			material.SetFloat("RES_NU", 1f);
		}
	}

	private void ClampSkyPosition(Vector3 position)
	{
		Vector3 position2 = position;
		position2.y = 0f;
		transform.position = position2;
	}

	private void Update()
	{
		if (CheckUpdate())
		{
			CalculateIlluminaryPositions();
			SetDynamicShaderUniforms();
			SetMaterialUniforms(_skyDome.sharedMaterial);
			SetMaterialUniforms(_atmosphereLookupMaterial);
			ComputeAtmosphereLookupTexture(_atmosphereLookupMaterial, Singleton<GameTime>.Instance.NormalizedTime);
		}
	}

	public void ComputeAtmosphereLookupTexture(Material lookupMaterial, float normalizedTime)
	{
		lookupMaterial.SetColor("_PolarisingColor", _skyColorPolariser.Evaluate(normalizedTime));
		Graphics.Blit(null, _atmosphereLookupTexture, lookupMaterial);
		Shader.SetGlobalTexture("_SkyMap", _atmosphereLookupTexture);
	}

	private void CalculateIlluminaryPositions()
	{
		float f = MathF.PI / 180f * _latitude;
		float num = Mathf.Sin(f);
		float num2 = Mathf.Cos(f);
		float num3 = MathF.PI / 180f * _longitude;
		float f2 = 0.4093f * Mathf.Sin(MathF.PI / 184f * (_date - 81f));
		float num4 = Mathf.Sin(f2);
		float num5 = Mathf.Cos(f2);
		float num6 = (int)(_longitude / 15f);
		float num7 = MathF.PI / 12f * num6;
		float num8 = Singleton<GameTime>.Instance.MilitaryTime + 0.17f * Mathf.Sin(0.03369f * (_date - 80f)) - 0.129f * Mathf.Sin(MathF.PI * 2f / 355f * (_date - 8f)) + 12f / MathF.PI * (num7 - num3);
		float f3 = MathF.PI / 12f * num8;
		float num9 = Mathf.Sin(f3);
		float num10 = Mathf.Cos(f3);
		float num11 = Mathf.Asin(num * num4 - num2 * num5 * num10);
		float y = (0f - num5) * num9;
		float x = num2 * num4 - num * num5 * num10;
		float num12 = Mathf.Atan2(y, x);
		_theta = MathF.PI / 2f - num11;
		float phi = num12;
		_sunIlluminary.Transform.position = transform.position + transform.rotation * SphericalToCartesian(_sunOrbitRadius, _theta, phi);
		_sunIlluminary.Transform.LookAt(transform.position);
		_moonIlluminary.Transform.position = transform.position + transform.rotation * SphericalToCartesian(_sunOrbitRadius, _theta + MathF.PI, phi);
		_moonIlluminary.Transform.LookAt(transform.position);
		_altitude = MathF.PI / 2f - _theta;
		float num13 = Mathf.Abs(_altitude);
		float num14 = MathF.PI / 180f;
		float num15 = MathF.PI / 15f;
		if (_altitude > 0f)
		{
			_sunIlluminary.Enable();
			_moonIlluminary.Disable();
			float num16 = Mathf.Lerp(0f, _sunIlluminary.Intensity, num13 / num14);
			_sunIlluminary.CurrentIntensity = ((num13 < num14) ? num16 : _sunIlluminary.Intensity);
			_sunIlluminary.Light.intensity = _sunIlluminary.CurrentIntensity;
			if (!_dayTime)
			{
				_dayTime = true;
				OnSunsetEvent();
			}
		}
		else
		{
			_moonIlluminary.Enable();
			_sunIlluminary.Disable();
			float num17 = Mathf.Lerp(0f, _moonIlluminary.Intensity, num13 / num15);
			_moonIlluminary.CurrentIntensity = ((num13 < num15) ? num17 : _moonIlluminary.Intensity);
			_moonIlluminary.Light.intensity = _moonIlluminary.CurrentIntensity;
			if (_dayTime)
			{
				_dayTime = false;
				OnSunsetEvent();
			}
		}
	}

	private void CalculateIlluminaryColors(Camera camera)
	{
		bool flag = Cameras.IsUnderwater(camera.transform);
		float normalizedTime = Singleton<GameTime>.Instance.NormalizedTime;
		Color color = _sunIlluminary.Gradient.Evaluate(normalizedTime);
		Color color2 = _moonIlluminary.Gradient.Evaluate(normalizedTime);
		WeatherEvent weatherEvent = Singleton<AtmosphereStorm>.Instance?.CurrentWeatherEvent;
		if (weatherEvent != null && weatherEvent.HasStarted)
		{
			color = weatherEvent.GetSunColor(color);
			color2 = weatherEvent.GetMoonColor(color2);
		}
		_sunIlluminary.Light.color = color;
		_moonIlluminary.Light.color = color2;
		if (flag)
		{
			float num = Singleton<CetoSettings>.Instance.BelowVisibility * 1.5f;
			float num2 = Mathf.Clamp01((camera.transform.position.y + num) / num);
			CurrentIlluminary.Light.color = Color.Lerp(CurrentIlluminary.Light.color * new Color(0.5f, 0.9f, 1f), CurrentIlluminary.Light.color, num2);
			CurrentIlluminary.Light.intensity = CurrentIlluminary.CurrentIntensity * num2;
		}
	}

	private void CalculateAmbientColor(Camera camera)
	{
		bool flag = Cameras.IsUnderwater(camera.transform);
		float normalizedTime = Singleton<GameTime>.Instance.NormalizedTime;
		_ambientLightColor = _ambientSkyColorGradient.Evaluate(normalizedTime) * _skyColorPolariser.Evaluate(normalizedTime);
		_ambientGroundColor = (flag ? _ambientUnderwaterGroundColorGradient.Evaluate(normalizedTime) : _ambientGroundColorGradient.Evaluate(normalizedTime));
		WeatherEvent weatherEvent = Singleton<AtmosphereStorm>.Instance?.CurrentWeatherEvent;
		if (weatherEvent != null && weatherEvent.HasStarted)
		{
			_ambientLightColor = weatherEvent.GetAmbientSkyColor(_ambientLightColor);
			_ambientGroundColor = weatherEvent.GetAmbientGroundColor(_ambientGroundColor);
		}
		if (flag)
		{
			float num = Singleton<CetoSettings>.Instance.BelowVisibility * 1.5f;
			float num2 = Mathf.Clamp01((camera.transform.position.y + num) / num);
			_ambientLightColor *= num2;
			_ambientGroundColor *= num2;
		}
		RenderSettings.sun = CurrentIlluminary.Light;
		RenderSettings.ambientLight = _ambientLightColor;
		RenderSettings.ambientEquatorColor = Color.Lerp(_ambientGroundColor, _ambientLightColor, 0.6f);
		RenderSettings.ambientGroundColor = _ambientGroundColor;
	}

	private Vector3 SphericalToCartesian(float radius, float theta, float phi)
	{
		float num = Mathf.Sin(theta);
		float num2 = Mathf.Cos(theta);
		float num3 = Mathf.Sin(phi);
		float num4 = Mathf.Cos(phi);
		Vector3 zero = Vector3.zero;
		zero.x = radius * num * num4;
		zero.y = radius * num2;
		zero.z = radius * num * num3;
		return zero;
	}
}
