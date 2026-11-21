using System;
using System.Collections.Generic;
using Beam.Utilities;
using UnityEngine;

namespace Beam.Rendering;

public class DynamicImpostor : ImpostorBase
{
	public const float SQR_MAX_DISTANCE = 62500f;

	public static float ANGLE_BIAS = 1f;

	public static float DISTANCE_BIAS = 1f;

	private Bounds _bounds;

	private ImposterTexture _imposterTexture;

	public int maxTextureSize = 256;

	public float angleTolerance = 2.5f;

	public float distanceTolerance = 15f;

	public float zOffset;

	private int _lastTextureSize;

	private Vector3 _lastCameraVector = Vector3.zero;

	private bool _forceUpdate;

	private float _maxSize;

	private Camera _renderingCamera;

	[Header("Renderables Debug")]
	private List<RenderableProxy> _renderableProxies = new List<RenderableProxy>();

	public Bounds Bounds => _bounds;

	private void Start()
	{
		_renderingCamera = DynamicImposterManager.Instance.imposterRenderingCamera;
		_lastCameraVector = Vector3.zero;
		if (!DynamicImposterManager.Instance.Imposters.Contains(this))
		{
			DynamicImposterManager.Instance.Imposters.Add(this);
		}
	}

	private void OnDestroy()
	{
		Release();
		DynamicImposterManager.Instance.Imposters.Remove(this);
	}

	public override void Release()
	{
		ReleaseProxies();
		ReleaseTexture();
		base.Release();
	}

	public override void Refresh()
	{
		base.Refresh();
		ExtractRenderersAndBounds();
	}

	public override void SetRenderables(IEnumerable<Renderer> renderers)
	{
		foreach (Renderer renderer in renderers)
		{
			CreateRenderableProxy(renderer);
		}
		Refresh();
	}

	private void CreateRenderableProxy(Renderer renderer)
	{
		Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
		if (mesh == null)
		{
			Debug.LogError("No mesh.");
			return;
		}
		RenderableProxy renderableProxy = ImpostorManager.CreateRenderableProxy(mesh, renderer);
		renderableProxy.Renderer.gameObject.layer = DynamicImposterManager.Instance.imposterLayer;
		renderableProxy.transform.position = transform.position;
		renderableProxy.transform.rotation = transform.rotation;
		_renderableProxies.Add(renderableProxy);
	}

	private void ReleaseProxies()
	{
		using (IEnumerator<RenderableProxy> enumerator = _renderableProxies.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ImpostorManager.ReleaseRenderableProxy(enumerator.Current);
			}
		}
		_renderableProxies.Clear();
	}

	private void ExtractRenderersAndBounds()
	{
		_bounds = default;
		using (IEnumerator<RenderableProxy> enumerator = _renderableProxies.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				RenderableProxy current = enumerator.Current;
				if (_bounds.extents == Vector3.zero)
				{
					_bounds = current.Renderer.bounds;
				}
				else
				{
					_bounds.Encapsulate(current.Renderer.bounds);
				}
			}
		}
		_maxSize = _bounds.extents.magnitude * 2f;
		transform.position = _bounds.center;
		Transform parent = transform.parent;
		transform.SetParent(null);
		transform.localScale = new Vector3(_maxSize, _maxSize, _maxSize);
		transform.SetParent(parent);
	}

	public void ForceUpdate()
	{
		_forceUpdate = true;
	}

	public void UpdateImposter(IImpostorCameraData cameraData)
	{
		bool flag = isActiveAndEnabled && Lod.Renderers[0].enabled;
		if ((!flag && !_forceUpdate) || (_forceUpdate && !flag))
		{
			return;
		}
		Vector3 lastCameraVector = _bounds.center - cameraData.Camera.transform.position;
		bool flag2 = NeedsUpdate(cameraData) || _forceUpdate;
		if (cameraData.IsVisible(_bounds))
		{
			if (flag2 || IsTextureInvalid())
			{
				FitTexture();
				Render(cameraData.Camera);
				_lastCameraVector = lastCameraVector;
				_forceUpdate = false;
			}
			return;
		}
		bool flag3 = cameraData.IsVisibleInCache(_bounds);
		if (flag3 && DynamicImposterManager.Instance.cachingBehaviour == DynamicImposterManager.CachingBehaviour.preloadAndCacheInvisibleImposters)
		{
			if ((flag2 || IsTextureInvalid()) && DynamicImposterManager.Instance.GetPreloadLock())
			{
				FitTexture();
				Render(cameraData.Camera);
				_lastCameraVector = lastCameraVector;
				_forceUpdate = false;
			}
		}
		else if ((!flag3 || DynamicImposterManager.Instance.cachingBehaviour == DynamicImposterManager.CachingBehaviour.discardInvisibleImpostors) | flag2)
		{
			ReleaseTexture();
		}
	}

	private bool NeedsUpdate(IImpostorCameraData cameraData)
	{
		Vector3 vector = _bounds.center - cameraData.Camera.transform.position;
		float num = Vector3.Angle(vector, _lastCameraVector);
		float num2 = Mathf.Abs(vector.magnitude - _lastCameraVector.magnitude);
		if (!(num > angleTolerance * ANGLE_BIAS))
		{
			return num2 > _lastCameraVector.magnitude * (distanceTolerance * DISTANCE_BIAS / 100f);
		}
		return true;
	}

	public void Render(Camera camera)
	{
		Lod.Renderers.FirstOrDefault_NonAlloc().transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
		RenderImposter(camera);
	}

	private void RenderImposter(Camera camera)
	{
		if (_imposterTexture == null)
		{
			return;
		}
		if (_renderingCamera == null)
		{
			_renderingCamera = DynamicImposterManager.Instance.imposterRenderingCamera;
		}
		_renderingCamera.transform.position = camera.transform.position;
		_renderingCamera.transform.LookAt(_bounds.center);
		_renderingCamera.cullingMask = 1 << DynamicImposterManager.Instance.imposterLayer;
		Vector3 vector = camera.transform.position - _bounds.center;
		_renderingCamera.orthographic = false;
		float magnitude = (camera.transform.position - transform.position).magnitude;
		float fieldOfView = 2f * Mathf.Atan(_maxSize / (2f * magnitude)) * (180f / MathF.PI);
		_renderingCamera.fieldOfView = fieldOfView;
		_renderingCamera.nearClipPlane = vector.magnitude - _maxSize;
		_renderingCamera.farClipPlane = vector.magnitude + _maxSize;
		_renderingCamera.targetTexture = _imposterTexture.texture;
		using (IEnumerator<RenderableProxy> enumerator = _renderableProxies.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				enumerator.Current.Renderer.enabled = true;
			}
			_renderingCamera.Render();
			enumerator.Reset();
			while (enumerator.MoveNext())
			{
				enumerator.Current.Renderer.enabled = false;
			}
		}
		_renderingCamera.targetTexture = null;
	}

	private void FitTexture()
	{
		if (IsTextureInvalid() || _lastTextureSize != maxTextureSize)
		{
			AdjustTextureSize(maxTextureSize);
		}
		_lastTextureSize = maxTextureSize;
	}

	private void AdjustTextureSize(int size)
	{
		if (_imposterTexture == null || _imposterTexture.size != size)
		{
			if (_imposterTexture != null)
			{
				DynamicImposterManager.Instance.ReleaseRenderTexture(_imposterTexture);
				_imposterTexture = null;
			}
			_imposterTexture = DynamicImposterManager.Instance.GetRenderTexture(this, size);
			for (int i = 0; i < Lod.Renderers.Length; i++)
			{
				Renderer obj = Lod.Renderers[i];
				obj.material.mainTexture = _imposterTexture.texture;
				obj.material.SetFloat("_ZOffset", zOffset);
			}
		}
	}

	private void ReleaseTexture()
	{
		if (!IsTextureInvalid())
		{
			DynamicImposterManager.Instance.ReleaseRenderTexture(_imposterTexture);
			_imposterTexture = null;
		}
	}

	private bool IsTextureInvalid()
	{
		if (_imposterTexture != null)
		{
			return _imposterTexture.texture == null;
		}
		return true;
	}
}
