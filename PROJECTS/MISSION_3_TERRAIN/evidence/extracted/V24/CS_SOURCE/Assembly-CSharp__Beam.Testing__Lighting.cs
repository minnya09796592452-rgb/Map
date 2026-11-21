using System;
using UnityEngine;

namespace Beam.Testing;

public class Lighting : MonoBehaviour
{
	[SerializeField]
	private Light _sunIlluminary;

	[SerializeField]
	private Light _moonIlluminary;

	[SerializeField]
	private float _sunLightIntensity;

	[SerializeField]
	private float _moonLightIntensity;

	[SerializeField]
	private Gradient _sunLightColorGradient;

	[SerializeField]
	private Gradient _moonLightColorGradient;

	[SerializeField]
	[HideInInspector]
	private float _timeOfDay = 12f;

	private float _latitude;

	private float _longitude;

	private float _date = 180f;

	private float _sunOrbitRadius;

	private float _altitude;

	[SerializeField]
	private Gradient _ambientGroundColorGradient;

	[SerializeField]
	private ReflectionProbe _environmentReflectionProbe;

	private Material _skydomeMaterial;

	[SerializeField]
	private Texture2D _transmittance;

	[SerializeField]
	private Texture2D _inscatter;

	private Texture3D _inscatterEncoded;

	[SerializeField]
	private Texture2D _irradiance;

	[SerializeField]
	private Texture2D _ambientLUT;

	private Color _ambientLightColor;

	private Transform _transform;

	private Transform _mainCameraTransform;

	public Light SunIlluminary => _sunIlluminary;

	public Light MoonIlluminary => _moonIlluminary;

	public float TimeOfDay
	{
		get
		{
			return _timeOfDay;
		}
		set
		{
			_timeOfDay = value;
		}
	}

	public Gradient AmbientGroundColorGradient
	{
		get
		{
			return _ambientGroundColorGradient;
		}
		set
		{
			_ambientGroundColorGradient = value;
		}
	}

	public Texture2D Transmittance
	{
		get
		{
			return _transmittance;
		}
		set
		{
			_transmittance = value;
		}
	}

	public Texture2D Inscatter
	{
		get
		{
			return _inscatter;
		}
		set
		{
			_inscatter = value;
		}
	}

	public Texture2D Irradiance
	{
		get
		{
			return _irradiance;
		}
		set
		{
			_irradiance = value;
		}
	}

	private void Awake()
	{
		_transform = GetComponent<Transform>();
		_mainCameraTransform = Camera.main.GetComponent<Transform>();
		_sunOrbitRadius = _transform.localScale.x * 0.999f;
		ConvertLookUpTable(_inscatter);
		InitializeShaders();
		_sunIlluminary.shadowStrength = 0.98f;
		_moonIlluminary.shadowStrength = 0.98f;
		RenderSettings.ambientIntensity = 1f;
		if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11"))
		{
			_environmentReflectionProbe.gameObject.SetActive(value: true);
		}
		_skydomeMaterial = GetComponent<Renderer>().sharedMaterial;
		InitializeMaterial(_skydomeMaterial);
	}

	private void ConvertLookUpTable(Texture2D texture)
	{
		if ((bool)texture)
		{
			if (texture.height != Mathf.FloorToInt(Mathf.Sqrt(texture.width)))
			{
				Debug.LogWarning("Atmosphere::ConvertLookUpTable(): The given 2D texture " + texture.name + " cannot be used as a 3D LUT.");
				return;
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
			if ((bool)_inscatterEncoded)
			{
				UnityEngine.Object.DestroyImmediate(_inscatterEncoded);
			}
			_inscatterEncoded = new Texture3D(num, height, num2, TextureFormat.ARGB32, mipChain: false);
			_inscatterEncoded.wrapMode = TextureWrapMode.Clamp;
			_inscatterEncoded.filterMode = FilterMode.Bilinear;
			_inscatterEncoded.SetPixels(array);
			_inscatterEncoded.Apply();
		}
		else
		{
			Debug.LogError("Atmosphere::ConvertLookUpTable(): Couldn't load 3D LUT texture. Texture3D will be disabled.");
		}
	}

	private void InitializeShaders()
	{
		Shader.SetGlobalFloat("_SkyFade", 0f);
		Shader.SetGlobalFloat("_StormFade", 0f);
	}

	public void InitializeMaterial(Material material)
	{
		if ((bool)material)
		{
			material.SetTexture("_Transmittance", _transmittance);
			material.SetTexture("_Inscatter", _inscatterEncoded);
			material.SetTexture("_Irradiance", _irradiance);
			material.SetFloat("_MieG", 0.8f);
			material.SetFloat("Rg", 6360000f);
			material.SetFloat("Rt", 6420000f);
			material.SetFloat("Rl", 6421000f);
			material.SetFloat("RES_R", 16f);
			material.SetFloat("RES_MU", 16f);
			material.SetFloat("RES_MU_S", 16f);
			material.SetFloat("RES_NU", 1f);
		}
	}

	private void Update()
	{
		ClampSkyPosition();
		AdvanceTimeOfDay();
		CalculateAmbientColor();
		CalculateSunAndMoonPosition();
		InitializeMaterial(_skydomeMaterial);
		Shader.SetGlobalColor("_SunColor", _sunIlluminary.color.linear);
		Shader.SetGlobalFloat("_SunIntensity", _sunIlluminary.intensity);
		Shader.SetGlobalVector("_SunPosition", _sunIlluminary.transform.position);
		Shader.SetGlobalVector("_SunDirection", _sunIlluminary.transform.forward * -1f);
		Shader.SetGlobalVector("_MoonDirection", _moonIlluminary.transform.forward * -1f);
	}

	private void AdvanceTimeOfDay()
	{
		if (Input.GetKey(KeyCode.KeypadPlus))
		{
			_timeOfDay += Time.deltaTime;
			if (_timeOfDay > 24f)
			{
				_timeOfDay = 0f;
			}
		}
		else if (Input.GetKey(KeyCode.KeypadMinus))
		{
			_timeOfDay -= Time.deltaTime;
			if (_timeOfDay < 0f)
			{
				_timeOfDay = 24f;
			}
		}
	}

	private void ClampSkyPosition()
	{
		Vector3 position = _mainCameraTransform.position;
		position.y = 0f;
		_transform.position = position;
	}

	private void CalculateSunAndMoonPosition()
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
		float num8 = _timeOfDay + 0.17f * Mathf.Sin(0.03369f * (_date - 80f)) - 0.129f * Mathf.Sin(MathF.PI * 2f / 355f * (_date - 8f)) + 12f / MathF.PI * (num7 - num3);
		float f3 = MathF.PI / 12f * num8;
		float num9 = Mathf.Sin(f3);
		float num10 = Mathf.Cos(f3);
		float num11 = Mathf.Asin(num * num4 - num2 * num5 * num10);
		float y = (0f - num5) * num9;
		float x = num2 * num4 - num * num5 * num10;
		float num12 = Mathf.Atan2(y, x);
		float num13 = MathF.PI / 2f - num11;
		float phi = num12;
		_sunIlluminary.transform.position = _transform.position + _transform.rotation * SphericalToCartesian(_sunOrbitRadius, num13, phi);
		_sunIlluminary.transform.LookAt(_transform.position);
		_moonIlluminary.transform.position = _transform.position + _transform.rotation * SphericalToCartesian(_sunOrbitRadius, num13 + MathF.PI, phi);
		_moonIlluminary.transform.LookAt(_transform.position);
		CalculateSunAndMoonColor(num13);
	}

	private void CalculateSunAndMoonColor(float theta)
	{
		_altitude = MathF.PI / 2f - theta;
		float num = Mathf.Abs(_altitude);
		float num2 = MathF.PI / 180f;
		float num3 = MathF.PI / 15f;
		if (_altitude > 0f)
		{
			_moonIlluminary.enabled = false;
			_sunIlluminary.enabled = true;
			float num4 = Mathf.Lerp(0f, _sunLightIntensity, num / num2);
			_sunIlluminary.intensity = ((num < num2) ? num4 : _sunLightIntensity);
		}
		else
		{
			_moonIlluminary.enabled = true;
			_sunIlluminary.enabled = false;
			float num5 = Mathf.Lerp(0f, _moonLightIntensity, num / num3);
			_moonIlluminary.intensity = ((num < num3) ? num5 : _moonLightIntensity);
		}
		float time = 1f / 24f * _timeOfDay;
		Color color = _sunLightColorGradient.Evaluate(time);
		Color color2 = _moonLightColorGradient.Evaluate(time);
		_sunIlluminary.color = color;
		_moonIlluminary.color = color2;
		Color color3 = _ambientGroundColorGradient.Evaluate(time);
		RenderSettings.ambientLight = _ambientLightColor;
		RenderSettings.ambientEquatorColor = Color.Lerp(color3, _ambientLightColor, 0.6f);
		RenderSettings.ambientGroundColor = color3;
		if (_environmentReflectionProbe.gameObject.activeSelf)
		{
			_environmentReflectionProbe.backgroundColor = color3;
		}
	}

	private void CalculateAmbientColor()
	{
		int num = (int)_timeOfDay;
		int x = num + 1;
		float t = _timeOfDay - (float)(int)_timeOfDay;
		Color pixel = _ambientLUT.GetPixel(num, 0);
		Color pixel2 = _ambientLUT.GetPixel(x, 0);
		_ambientLightColor = Color.Lerp(pixel, pixel2, t);
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
