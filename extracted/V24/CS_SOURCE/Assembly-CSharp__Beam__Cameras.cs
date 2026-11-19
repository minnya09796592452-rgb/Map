using System;
using System.Collections.Generic;
using System.IO;
using Beam.Events;
using Beam.Serialization.Json;
using Beam.UI;
using Beam.Utilities;
using Ceto;
using UnityEngine;

namespace Beam;

public class Cameras : Singleton<Cameras>
{
	private static List<IGameCamera> _AllCameras = new List<IGameCamera>();

	private static Camera _Main;

	private static Camera _Current;

	private static float _DefaultFov;

	private static Plane[] _planes;

	private Camera _postCamera;

	public static IReadOnlyList<IGameCamera> AllCameras => _AllCameras;

	public static Camera Main
	{
		get
		{
			if (_Main == null)
			{
				_Main = Camera.main;
			}
			return _Main;
		}
	}

	public static Camera Current
	{
		get
		{
			if (_Current == null)
			{
				return Main;
			}
			return _Current;
		}
	}

	public static float DefaultFOV => _DefaultFov;

	public Dictionary<Camera, IImpostorCameraData> CameraData { get; } = new Dictionary<Camera, IImpostorCameraData>();

	public static event Action<Camera> OnPreCull;

	public static Camera GetPostCamera()
	{
		return Singleton<Cameras>.Instance?._postCamera;
	}

	public static bool TryGetCameraData(Camera camera, out IImpostorCameraData cameraData)
	{
		cameraData = null;
		return Singleton<Cameras>.Instance?.CameraData.TryGetValue(camera, out cameraData) ?? false;
	}

	private void Awake()
	{
		_planes = new Plane[6];
		_postCamera = CreatePostCamera();
	}

	private void Start()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(Camera_OnPreCull));
		EventManager.AddListener<OptionsAppliedEvent>(EventManager_OptionsApplied);
		LoadOptions();
	}

	protected override void OnDestroy()
	{
		OnPreCull = null;
		_AllCameras.Clear();
		_Main = null;
		_Current = null;
		DestroyCamera(_postCamera);
		_planes = null;
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(Camera_OnPreCull));
		EventManager.RemoveListener<OptionsAppliedEvent>(EventManager_OptionsApplied);
		base.OnDestroy();
	}

	private Camera CreatePostCamera()
	{
		Camera camera = UnityEngine.Object.Instantiate(Resources.Load<Camera>("Prefabs/PostCamera"));
		camera.gameObject.name = "~PostCamera";
		camera.transform.SetParent(transform);
		camera.transform.position = Vector3.zero;
		camera.transform.rotation = Quaternion.identity;
		return camera;
	}

	public Camera CreateCamera(Camera copy)
	{
		Camera camera = UnityEngine.Object.Instantiate(copy);
		camera.gameObject.name = "~Camera";
		camera.transform.SetParent(transform);
		camera.transform.position = Vector3.zero;
		camera.transform.rotation = Quaternion.identity;
		camera.rect = new Rect(0f, 0f, 1f, 1f);
		CameraUtils component = camera.GetComponent<CameraUtils>();
		if ((object)component != null)
		{
			component.RemoveComponents();
			return camera;
		}
		return camera;
	}

	private void DestroyCamera(Camera camera)
	{
		CameraData.Remove(camera);
		BeamTeam.Destroy(camera.gameObject);
	}

	public static void AddCamera(IGameCamera playerCamera)
	{
		_AllCameras.Add(playerCamera);
	}

	public static void RemoveCamera(IGameCamera playerCamera)
	{
		_AllCameras.Remove(playerCamera);
	}

	private void EventManager_OptionsApplied(OptionsAppliedEvent e)
	{
		LoadOptions();
	}

	private void LoadOptions()
	{
		float num = 60f + Options.GraphicsSettings.FieldOfView * 15f;
		if (num < 60f || num > 75f)
		{
			Debug.LogError("Cameras::LoadOptions:: Something went wrong. Saved field of view value was out of bounds.Field of view is now set to 75.0");
			num = 75f;
		}
		_DefaultFov = num;
		_AllCameras.SetFieldOfView(_DefaultFov);
	}

	public static bool IsUnderwater(Transform t)
	{
		if (Ceto.Ocean.Instance == null || t == null)
		{
			return false;
		}
		return t.position.y < Ceto.Ocean.Instance.QueryWaves(t.position.x, t.position.z);
	}

	public static bool IsVisible(Bounds bounds)
	{
		for (int i = 0; i < _AllCameras.Count; i++)
		{
			IGameCamera gameCamera = _AllCameras[i];
			BeamGeometryUtility.CalculateFrustumPlanes(_planes, gameCamera.Camera.projectionMatrix * gameCamera.Camera.worldToCameraMatrix);
			if (GeometryUtility.TestPlanesAABB(_planes, bounds))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsCulled(Vector3 position, float cullingDistance)
	{
		for (int i = 0; i < _AllCameras.Count; i++)
		{
			if ((_AllCameras[i].Camera.transform.position - position).sqrMagnitude < BeamMath.Sqr(cullingDistance))
			{
				return false;
			}
		}
		return true;
	}

	private void Camera_OnPreCull(Camera camera)
	{
		if (!(camera == null) && (camera.CompareTag("MainCamera") || camera.CompareTag("PlayerCamera")))
		{
			_Current = camera;
			OnPreCull?.Invoke(_Current);
			CalculateFrustumCorners(_Current);
		}
	}

	private void CalculateFrustumCorners(Camera camera)
	{
		float nearClipPlane = camera.nearClipPlane;
		float farClipPlane = camera.farClipPlane;
		float fieldOfView = camera.fieldOfView;
		float aspect = camera.aspect;
		Matrix4x4 identity = Matrix4x4.identity;
		float num = fieldOfView * 0.5f;
		Vector3 vector = camera.transform.right * nearClipPlane * Mathf.Tan(num * (MathF.PI / 180f)) * aspect;
		Vector3 vector2 = camera.transform.up * nearClipPlane * Mathf.Tan(num * (MathF.PI / 180f));
		Vector3 vector3 = camera.transform.forward * nearClipPlane - vector + vector2;
		float num2 = vector3.magnitude * farClipPlane / nearClipPlane;
		vector3.Normalize();
		vector3 *= num2;
		Vector3 vector4 = camera.transform.forward * nearClipPlane + vector + vector2;
		vector4.Normalize();
		vector4 *= num2;
		Vector3 vector5 = camera.transform.forward * nearClipPlane + vector - vector2;
		vector5.Normalize();
		vector5 *= num2;
		Vector3 vector6 = camera.transform.forward * nearClipPlane - vector - vector2;
		vector6.Normalize();
		vector6 *= num2;
		identity.SetRow(0, vector3);
		identity.SetRow(1, vector4);
		identity.SetRow(2, vector5);
		identity.SetRow(3, vector6);
		Shader.SetGlobalMatrix("_ShaderFrustumCorners", identity);
	}

	private void DoScreenshot()
	{
		Vector3 down = Vector3.down;
		Vector3 position = StrandedWorld.GetZone(Vector3.zero).transform.position + Vector3.up * 128f;
		CaptureScreen(position, down, 1920, 1080, "default");
	}

	private void SaveManager_BeforeWrite(JObject rootData)
	{
		Zone zone = StrandedWorld.GetZone(Main.transform.position);
		Vector3 down = Vector3.down;
		Vector3 vector = zone.transform.position + Vector3.up * 128f;
		string text = ((Game.Mode == GameMode.SinglePlayer) ? "dynamic" : "dynamic_COOP");
		CaptureScreen(vector, down, Screen.width, Screen.height, text);
		JObject jObject = new JObject();
		jObject.AddField("Pos", JSerializer.Serialize(vector));
		jObject.AddField("Dir", JSerializer.Serialize(down));
		rootData.AddField("ScreenCap", jObject);
	}

	private void CaptureScreen(Vector3 position, Vector3 direction, int width, int height, string name)
	{
		Debug.Log("Cameras::CaptureScreen:: Capturing Screen...");
		Camera camera = CreateCamera(_postCamera);
		camera.depth = GetPostCamera().depth + 2f;
		Camera camera2 = CreateCamera(PlayerRegistry.AllPlayers[0].PlayerCamera.Camera);
		camera2.depth = camera.depth - 1f;
		if (!CameraData.ContainsKey(camera2))
		{
			CameraData.Add(camera2, new AlwaysCameraData(camera2));
		}
		Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
		RenderTexture temporary = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
		RenderInto(camera2, temporary, position, rotation);
		RenderInto(camera, temporary, position, rotation);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(temporary.width, temporary.height, TextureFormat.RGB24, mipChain: false);
		texture2D.ReadPixels(new Rect(0f, 0f, texture2D.width, texture2D.height), 0, 0);
		texture2D.Apply(updateMipmaps: false);
		RenderTexture.active = active;
		temporary.Release();
		DestroyCamera(camera2);
		DestroyCamera(camera);
		Debug.Log("Cameras::CaptureScreen:: Writing screenshot to file...");
		string path = PathTools.Combine(SaveManager.GetSaveFolderPath(), name + ".bytes");
		try
		{
			using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				byte[] array = texture2D.EncodeToPNG();
				fileStream.Write(array, 0, array.Length);
			}
			Debug.Log("Cameras::CaptureScreen:: Complete.");
		}
		catch (Exception)
		{
			Debug.LogError("Cameras::CaptureScreen:: Error writing screenshot to file: ");
			LocalizedNotification localizedNotification = new LocalizedNotification(new Notification());
			localizedNotification.Priority = NotificationPriority.Immediate;
			localizedNotification.Duration = 8f;
			localizedNotification.PlayerId = 0;
			localizedNotification.MessageText.SetTerm("Error writing screenshot to file. Please contact Beam Team on the Steam Forum.");
			localizedNotification.Raise();
		}
	}

	private void RenderInto(Camera camera, RenderTexture target, Vector3 position, Quaternion rotation)
	{
		camera.transform.position = position;
		camera.transform.rotation = rotation;
		camera.targetTexture = target;
		camera.Render();
	}
}
