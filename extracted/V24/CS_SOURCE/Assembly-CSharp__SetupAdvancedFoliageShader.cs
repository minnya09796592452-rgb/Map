using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[AddComponentMenu("AFS/Setup Advanced Foliage Shader")]
public class SetupAdvancedFoliageShader : MonoBehaviour
{
	public bool isLinear;

	public bool AmbientLightingSH;

	public GameObject TerrianLight0;

	public GameObject TerrianLight1;

	public GameObject DirectionalLightReference;

	private Vector3 DirectlightDir;

	private Light Sunlight;

	private Vector3 SunLightCol;

	private float SunLuminance;

	public bool GrassApproxTrans;

	public int AFSFog_Mode = 2;

	public string AFSShader_Folder = "Assets/Advanced Foliage Shader v4/Shaders/";

	public bool disableFoginShader;

	public Vector4 Wind = new Vector4(0.85f, 0.075f, 0.4f, 0.5f);

	[Range(0.01f, 2f)]
	public float WindFrequency = 0.25f;

	[Range(0.1f, 25f)]
	public float WaveSizeFoliageShader = 10f;

	[Range(0f, 10f)]
	public float LeafTurbulenceFoliageShader = 4f;

	[Range(0.01f, 1f)]
	public float WindMultiplierForGrassshader = 1f;

	[Range(0f, 10f)]
	public float WaveSizeForGrassshader = 1f;

	[Range(0f, 1f)]
	public float WindJitterFrequencyForGrassshader = 0.25f;

	[Range(0f, 1f)]
	public float WindJitterScaleForGrassshader = 0.15f;

	public bool SyncWindDir;

	[Range(0f, 10f)]
	public float WindMuliplierForTreeShaderPrimary = 1f;

	[Range(0f, 10f)]
	public float WindMuliplierForTreeShaderSecondary = 1f;

	public Vector4 WindMuliplierForTreeShader = new Vector4(1f, 1f, 1f, 1f);

	private float temp_WindFrequency = 0.25f;

	private float temp_WindJitterFrequency = 0.25f;

	private const float TwoPI = MathF.PI * 2f;

	private float freqSpeed = 0.05f;

	private float domainTime_Wind;

	private float domainTime_2ndBending;

	private float domainTime_Grass;

	[Range(0f, 1f)]
	public float RainAmount;

	[Range(0f, 1f)]
	public float VertexLitAlphaCutOff = 0.3f;

	public Color VertexLitTranslucencyColor = new Color(0.73f, 0.85f, 0.4f, 1f);

	[Range(0f, 1f)]
	public float VertexLitTranslucencyViewDependency = 0.7f;

	[Range(0f, 1f)]
	public float VertexLitShadowStrength = 0.8f;

	public Color VertexLitSpecularReflectivity = new Color(0.2f, 0.2f, 0.2f, 1f);

	[Range(0f, 100f)]
	public Vector2 AfsSpecFade = new Vector2(30f, 10f);

	public Texture TerrainFoliageNrmSpecMap;

	public bool AutoSyncToTerrain;

	public Terrain SyncedTerrain;

	public bool AutoSyncInPlaymode;

	public float DetailDistanceForGrassShader = 80f;

	[Range(0f, 1000f)]
	public float BillboardStart = 50f;

	[Range(0f, 30f)]
	public float BillboardFadeLenght = 5f;

	public bool GrassAnimateNormal;

	public Color GrassWavingTint = Color.white;

	public Color AFSTreeColor = Color.black;

	private Camera MainCam;

	public bool TreeBillboardShadows;

	[Range(10f, 100f)]
	public float BillboardFadeOutLength = 60f;

	public bool BillboardAdjustToCamera = true;

	[Range(10f, 90f)]
	public float BillboardAngleLimit = 30f;

	public Color BillboardShadowColor;

	[Range(0f, 4f)]
	public float BillboardAmbientLightFactor = 1f;

	[Range(0f, 2f)]
	public float BillboardAmbientLightDesaturationFactor = 0.7f;

	public bool AutosyncShadowColor;

	public bool EnableCameraLayerCulling;

	[Range(10f, 300f)]
	public int SmallDetailsDistance = 70;

	[Range(10f, 300f)]
	public int MediumDetailsDistance = 90;

	public bool AllGrassObjectsCombined;

	private Vector4 TempWind;

	private float GrassWindSpeed;

	private float SinusWave;

	private Vector4 TriangleWaves;

	private float Oscillation;

	private Vector3 CameraForward = new Vector3(0f, 0f, 0f);

	private Vector3 ShadowCameraForward = new Vector3(0f, 0f, 0f);

	private Vector3 CameraForwardVec;

	private float rollingX;

	private float rollingXShadow;

	private Vector3 lightDir;

	private Vector3 templightDir;

	private float CameraAngle;

	private Terrain[] allTerrains;

	private Vector3[] fLight = new Vector3[9];

	private Vector4[] vCoeff = new Vector4[3];

	private void Awake()
	{
		afsSyncFrequencies();
		afsCheckColorSpace();
		afsLightingSettings();
		afsUpdateWind();
		afsUpdateRain();
		afsSetupTerrainEngine();
		afsAutoSyncToTerrain();
		afsUpdateGrassTreesBillboards();
		afsSetupCameraLayerCulling();
		afsFogSettings();
		afsSetupGrassShader();
	}

	public void Update()
	{
		afsLightingSettings();
		afsUpdateRain();
		afsAutoSyncToTerrain();
		afsUpdateGrassTreesBillboards();
	}

	public void FixedUpdate()
	{
		afsUpdateWind();
	}

	private void afsSyncFrequencies()
	{
		temp_WindFrequency = WindFrequency;
		temp_WindJitterFrequency = WindJitterFrequencyForGrassshader;
		domainTime_Wind = 0f;
		domainTime_2ndBending = 0f;
		domainTime_Grass = 0f;
	}

	private void afsCheckColorSpace()
	{
	}

	private void afsLightingSettings()
	{
		if (DirectionalLightReference != null)
		{
			DirectlightDir = DirectionalLightReference.transform.forward;
			if (Sunlight == null)
			{
				Sunlight = DirectionalLightReference.GetComponent<Light>();
			}
			if (!Sunlight.enabled)
			{
				Shader.SetGlobalVector("_AfsDirectSunDir", new Vector4(0f, 0f, 0f, 0f));
			}
			else
			{
				SunLightCol = new Vector3(Sunlight.color.r, Sunlight.color.g, Sunlight.color.b) * Sunlight.intensity;
				SunLuminance = Vector3.Dot(SunLightCol, new Vector3(0.22f, 0.707f, 0.071f));
				Shader.SetGlobalVector("_AfsDirectSunDir", new Vector4(DirectlightDir.x, DirectlightDir.y, DirectlightDir.z, SunLuminance));
				Shader.SetGlobalVector("_AfsDirectSunCol", SunLightCol);
			}
		}
		else
		{
			Shader.SetGlobalVector("_AfsDirectSunDir", new Vector4(0f, 0f, 0f, 0f));
			Shader.SetGlobalVector("_AfsDirectSunCol", new Vector3(0f, 0f, 0f));
		}
		UpdateLightingForClassicBillboards();
		Shader.SetGlobalVector("_AfsSpecFade", new Vector4(AfsSpecFade.x, AfsSpecFade.y, 1f, 1f));
		if (GrassApproxTrans)
		{
			Shader.EnableKeyword("_AFS_GRASS_APPROXTRANS");
		}
		else
		{
			Shader.DisableKeyword("_AFS_GRASS_APPROXTRANS");
		}
	}

	private void afsFogSettings()
	{
		if (disableFoginShader)
		{
			Shader.EnableKeyword("_AFS_DEFERRED");
		}
		else
		{
			Shader.DisableKeyword("_AFS_DEFERRED");
		}
	}

	private void afsSetupGrassShader()
	{
		if (Application.isPlaying || AllGrassObjectsCombined)
		{
			Shader.DisableKeyword("IN_EDITMODE");
			Shader.EnableKeyword("IN_PLAYMODE");
		}
		else
		{
			Shader.DisableKeyword("IN_PLAYMODE");
			Shader.EnableKeyword("IN_EDITMODE");
		}
	}

	private void afsSetupTerrainEngine()
	{
		Shader.SetGlobalFloat("_AfsAlphaCutOff", VertexLitAlphaCutOff);
		Shader.SetGlobalFloat("_AfsTranslucencyViewDependency", VertexLitTranslucencyViewDependency);
		Shader.SetGlobalFloat("_AfsShadowStrength", VertexLitShadowStrength);
		Shader.SetGlobalColor("_AfsTranslucencyColor", VertexLitTranslucencyColor);
		if (isLinear)
		{
			Shader.SetGlobalColor("_AfsSpecularReflectivity", VertexLitSpecularReflectivity.linear);
		}
		else
		{
			Shader.SetGlobalColor("_AfsSpecularReflectivity", VertexLitSpecularReflectivity);
		}
		if (TerrainFoliageNrmSpecMap != null)
		{
			Shader.SetGlobalTexture("_TerrianBumpTransSpecMap", TerrainFoliageNrmSpecMap);
		}
		else
		{
			Shader.SetGlobalTexture("_TerrianBumpTransSpecMap", null);
		}
	}

	private void afsUpdateWind()
	{
		if (SyncWindDir)
		{
			Wind = new Vector4(transform.forward.x, transform.forward.y, transform.forward.z, Wind.w);
		}
		TempWind = Wind;
		TempWind.w *= 2f;
		Shader.SetGlobalVector("_Wind", TempWind);
		Shader.SetGlobalFloat("_WindFrequency", WindFrequency);
		domainTime_Wind += temp_WindFrequency * Time.deltaTime * 2f;
		domainTime_2ndBending += Time.deltaTime;
		Shader.SetGlobalVector("_AfsTimeFrequency", new Vector4(domainTime_Wind, domainTime_2ndBending, 0.375f * (1f + temp_WindFrequency * LeafTurbulenceFoliageShader), 0.193f * (1f + temp_WindFrequency * LeafTurbulenceFoliageShader)));
		temp_WindFrequency = Mathf.MoveTowards(temp_WindFrequency, WindFrequency, freqSpeed);
		SinusWave = Mathf.Sin(domainTime_Wind);
		TriangleWaves = SmoothTriangleWave(new Vector4(SinusWave, SinusWave * 0.8f, 0f, 0f));
		Oscillation = TriangleWaves.x + TriangleWaves.y * TriangleWaves.y;
		Oscillation = (Oscillation + 8f) * 0.125f * TempWind.w;
		TempWind.x *= Oscillation;
		TempWind.z *= Oscillation;
		Shader.SetGlobalFloat("_AfsWaveSize", 0.5f / WaveSizeFoliageShader);
		Shader.SetGlobalFloat("_AfsWindJitterScale", WindJitterScaleForGrassshader * 10f);
		Shader.SetGlobalVector("_AfsGrassWind", TempWind * WindMultiplierForGrassshader);
		domainTime_Grass += temp_WindJitterFrequency * Time.deltaTime;
		temp_WindJitterFrequency = Mathf.MoveTowards(temp_WindJitterFrequency, WindJitterFrequencyForGrassshader, freqSpeed);
		GrassWindSpeed = domainTime_Grass * 0.1f;
		Shader.SetGlobalVector("_AfsWaveAndDistance", new Vector4(GrassWindSpeed, WaveSizeForGrassshader, TempWind.w, DetailDistanceForGrassShader * DetailDistanceForGrassShader));
		Shader.SetGlobalVector("_AFSWindMuliplier", WindMuliplierForTreeShader);
	}

	private void afsUpdateRain()
	{
	}

	private void afsAutoSyncToTerrain()
	{
		if (AutoSyncToTerrain && SyncedTerrain != null)
		{
			DetailDistanceForGrassShader = SyncedTerrain.detailObjectDistance;
			BillboardStart = SyncedTerrain.treeBillboardDistance;
			if (Application.isPlaying && !TreeBillboardShadows)
			{
				BillboardFadeLenght = SyncedTerrain.treeCrossFadeLength;
			}
			GrassWavingTint = SyncedTerrain.terrainData.wavingGrassTint;
		}
	}

	private void afsUpdateGrassTreesBillboards()
	{
		Shader.SetGlobalColor("_AfsWavingTint", GrassWavingTint);
		Shader.SetGlobalColor("_AfsTreeColor", AFSTreeColor);
		Shader.SetGlobalVector("_AfsTerrainTrees", new Vector4(BillboardStart, BillboardFadeLenght, BillboardFadeOutLength, 0f));
		if (BillboardAdjustToCamera)
		{
			if ((bool)Camera.main)
			{
				MainCam = Camera.main;
				CameraForward = MainCam.transform.forward;
				ShadowCameraForward = CameraForward;
				rollingX = MainCam.transform.eulerAngles.x;
				if (rollingX >= 270f)
				{
					rollingX -= 270f;
					rollingX = 90f - rollingX;
					rollingXShadow = rollingX;
				}
				else
				{
					rollingXShadow = 0f - rollingX;
					if (rollingX > BillboardAngleLimit)
					{
						rollingX = Mathf.Lerp(rollingX, 0f, rollingX / BillboardAngleLimit - 1f);
					}
					rollingX *= -1f;
				}
			}
		}
		else
		{
			rollingX = 0f;
			rollingXShadow = 0f;
		}
		CameraForward *= rollingX / 90f;
		ShadowCameraForward *= rollingXShadow / 90f;
		Shader.SetGlobalVector("_AfsBillboardCameraForward", new Vector4(CameraForward.x, CameraForward.y, CameraForward.z, 1f));
		Shader.SetGlobalVector("_AfsBillboardShadowCameraForward", new Vector4(ShadowCameraForward.x, ShadowCameraForward.y, ShadowCameraForward.z, 1f));
	}

	private void afsSetupCameraLayerCulling()
	{
		if (EnableCameraLayerCulling)
		{
			for (int i = 0; i < Camera.allCameras.Length; i++)
			{
				float[] array = new float[32];
				array = Camera.allCameras[i].layerCullDistances;
				array[8] = SmallDetailsDistance;
				array[9] = MediumDetailsDistance;
				Camera.allCameras[i].layerCullDistances = array;
				array = null;
			}
		}
	}

	private void UpdateLightingForClassicBillboards()
	{
		if (RenderSettings.ambientMode == AmbientMode.Skybox)
		{
			AmbientLightingSH = true;
			UdpateSHLightingforBillboards();
			Shader.EnableKeyword("AFS_SH_AMBIENT");
			Shader.DisableKeyword("AFS_COLOR_AMBIENT");
			Shader.DisableKeyword("AFS_GRADIENT_AMBIENT");
		}
		else if (RenderSettings.ambientMode == AmbientMode.Trilight)
		{
			AmbientLightingSH = false;
			if (isLinear)
			{
				Shader.SetGlobalColor("_AfsSkyColor", RenderSettings.ambientSkyColor.linear);
				Shader.SetGlobalColor("_AfsGroundColor", RenderSettings.ambientGroundColor.linear);
				Shader.SetGlobalColor("_AfsEquatorColor", RenderSettings.ambientEquatorColor.linear);
			}
			else
			{
				Shader.SetGlobalColor("_AfsSkyColor", RenderSettings.ambientSkyColor);
				Shader.SetGlobalColor("_AfsGroundColor", RenderSettings.ambientGroundColor);
				Shader.SetGlobalColor("_AfsEquatorColor", RenderSettings.ambientEquatorColor);
			}
			Shader.DisableKeyword("AFS_SH_AMBIENT");
			Shader.DisableKeyword("AFS_COLOR_AMBIENT");
			Shader.EnableKeyword("AFS_GRADIENT_AMBIENT");
		}
		else if (RenderSettings.ambientMode == AmbientMode.Flat)
		{
			AmbientLightingSH = false;
			Shader.SetGlobalVector("_AfsAmbientColor", RenderSettings.ambientLight);
			Shader.DisableKeyword("AFS_SH_AMBIENT");
			Shader.EnableKeyword("AFS_COLOR_AMBIENT");
			Shader.DisableKeyword("AFS_GRADIENT_AMBIENT");
		}
	}

	private void UdpateSHLightingforBillboards()
	{
		fLight[0].x = RenderSettings.ambientProbe[0, 0];
		fLight[0].y = RenderSettings.ambientProbe[1, 0];
		fLight[0].z = RenderSettings.ambientProbe[2, 0];
		fLight[1].x = RenderSettings.ambientProbe[0, 1];
		fLight[1].y = RenderSettings.ambientProbe[1, 1];
		fLight[1].z = RenderSettings.ambientProbe[2, 1];
		fLight[2].x = RenderSettings.ambientProbe[0, 2];
		fLight[2].y = RenderSettings.ambientProbe[1, 2];
		fLight[2].z = RenderSettings.ambientProbe[2, 2];
		fLight[3].x = RenderSettings.ambientProbe[0, 3];
		fLight[3].y = RenderSettings.ambientProbe[1, 3];
		fLight[3].z = RenderSettings.ambientProbe[2, 3];
		fLight[4].x = RenderSettings.ambientProbe[0, 4];
		fLight[4].y = RenderSettings.ambientProbe[1, 4];
		fLight[4].z = RenderSettings.ambientProbe[2, 4];
		fLight[5].x = RenderSettings.ambientProbe[0, 5];
		fLight[5].y = RenderSettings.ambientProbe[1, 5];
		fLight[5].z = RenderSettings.ambientProbe[2, 5];
		fLight[6].x = RenderSettings.ambientProbe[0, 6];
		fLight[6].y = RenderSettings.ambientProbe[1, 6];
		fLight[6].z = RenderSettings.ambientProbe[2, 6];
		fLight[7].x = RenderSettings.ambientProbe[0, 7];
		fLight[7].y = RenderSettings.ambientProbe[1, 7];
		fLight[7].z = RenderSettings.ambientProbe[2, 7];
		fLight[8].x = RenderSettings.ambientProbe[0, 8];
		fLight[8].y = RenderSettings.ambientProbe[1, 8];
		fLight[8].z = RenderSettings.ambientProbe[2, 8];
		float num = 3.5449078f;
		float num2 = 1f / (2f * num);
		float num3 = Mathf.Sqrt(3f) / (3f * num);
		float num4 = Mathf.Sqrt(15f) / (8f * num);
		float num5 = Mathf.Sqrt(5f) / (16f * num);
		float num6 = 0.5f * num4;
		for (int i = 0; i < 3; i++)
		{
			vCoeff[i].x = (0f - num3) * fLight[3][i];
			vCoeff[i].y = (0f - num3) * fLight[1][i];
			vCoeff[i].z = num3 * fLight[2][i];
			vCoeff[i].w = num2 * fLight[0][i] - num5 * fLight[6][i];
		}
		Shader.SetGlobalVector("afs_SHAr", vCoeff[0] * RenderSettings.ambientIntensity);
		Shader.SetGlobalVector("afs_SHAg", vCoeff[1] * RenderSettings.ambientIntensity);
		Shader.SetGlobalVector("afs_SHAb", vCoeff[2] * RenderSettings.ambientIntensity);
		for (int i = 0; i < 3; i++)
		{
			vCoeff[i].x = num4 * fLight[4][i];
			vCoeff[i].y = (0f - num4) * fLight[5][i];
			vCoeff[i].z = 3f * num5 * fLight[6][i];
			vCoeff[i].w = (0f - num4) * fLight[7][i];
		}
		Shader.SetGlobalVector("afs_SHBr", vCoeff[0] * RenderSettings.ambientIntensity);
		Shader.SetGlobalVector("afs_SHBg", vCoeff[1] * RenderSettings.ambientIntensity);
		Shader.SetGlobalVector("afs_SHBb", vCoeff[2] * RenderSettings.ambientIntensity);
		vCoeff[0].x = num6 * fLight[8][0];
		vCoeff[0].y = num6 * fLight[8][1];
		vCoeff[0].z = num6 * fLight[8][2];
		vCoeff[0].w = 1f;
		Shader.SetGlobalVector("afs_SHC", vCoeff[0] * RenderSettings.ambientIntensity);
	}

	private Color Desaturate(float r, float g, float b)
	{
		float num = 0.3f * r + 0.59f * g + 0.11f * b;
		r = num * BillboardAmbientLightDesaturationFactor + r * (1f - BillboardAmbientLightDesaturationFactor);
		g = num * BillboardAmbientLightDesaturationFactor + g * (1f - BillboardAmbientLightDesaturationFactor);
		b = num * BillboardAmbientLightDesaturationFactor + b * (1f - BillboardAmbientLightDesaturationFactor);
		return new Color(r, g, b, 1f);
	}

	private float CubicSmooth(float x)
	{
		return x * x * (3f - 2f * x);
	}

	private float TriangleWave(float x)
	{
		return Mathf.Abs((x + 0.5f) % 1f * 2f - 1f);
	}

	private float SmoothTriangleWave(float x)
	{
		return CubicSmooth(TriangleWave(x));
	}

	private Vector4 CubicSmooth(Vector4 x)
	{
		x = Vector4.Scale(x, x);
		x = Vector4.Scale(x, new Vector4(3f, 3f, 3f, 3f) - 2f * x);
		return x;
	}

	private Vector4 TriangleWave(Vector4 x)
	{
		x = (x + new Vector4(0.5f, 0.5f, 0.5f, 0.5f)) * 2f - new Vector4(1f, 1f, 1f, 1f);
		return AbsVecFour(x);
	}

	private Vector4 SmoothTriangleWave(Vector4 x)
	{
		return CubicSmooth(TriangleWave(x));
	}

	private Vector4 FracVecFour(Vector4 a)
	{
		a.x %= 1f;
		a.y %= 1f;
		a.z %= 1f;
		a.w %= 1f;
		return a;
	}

	private Vector4 AbsVecFour(Vector4 a)
	{
		a.x = Mathf.Abs(a.x);
		a.y = Mathf.Abs(a.y);
		a.z = Mathf.Abs(a.z);
		a.w = Mathf.Abs(a.w);
		return a;
	}
}
