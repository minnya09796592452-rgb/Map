using UnityEngine;

namespace Beam.UI;

public class WatchScreenViewAdapter : WatchScreenViewAdapterBase
{
	[SerializeField]
	private Renderer _watchScreenRenderer;

	private Renderer _watchRenderer;

	[SerializeField]
	private Renderer _watchRendererMale;

	[SerializeField]
	private Renderer _watchRendererFemale;

	[SerializeField]
	private Transform _watchMaleScreenPos;

	[SerializeField]
	private Transform _watchFemaleScreenPos;

	[SerializeField]
	private Light _light;

	public override void SetWatchMesh(int type)
	{
		if (type == 0)
		{
			_watchRenderer = _watchRendererMale;
			_watchRendererMale.enabled = true;
			_watchRendererFemale.enabled = false;
			_watchScreenRenderer.transform.position = _watchMaleScreenPos.position;
		}
		else
		{
			_watchRenderer = _watchRendererFemale;
			_watchRendererFemale.enabled = true;
			_watchRendererMale.enabled = false;
			_watchScreenRenderer.transform.position = _watchFemaleScreenPos.position;
		}
	}

	public override void SetWatchTexture(Texture texture)
	{
		_watchScreenRenderer.material.mainTexture = texture;
	}

	public override void SetBacklight(bool enabled)
	{
		if (!(_watchRenderer == null))
		{
			_light.intensity = (enabled ? 1f : 0.3f);
			for (int i = 0; i < _watchRenderer.sharedMaterials.Length; i++)
			{
				_watchRenderer.sharedMaterials[i].EnableKeyword("_EMISSION");
				_watchRenderer.sharedMaterials[i].SetColor("_EmissionColor", enabled ? Color.Lerp(Color.black, Color.white, 0.5f) : Color.Lerp(Color.black, Color.white, 0.2f));
			}
		}
	}
}
