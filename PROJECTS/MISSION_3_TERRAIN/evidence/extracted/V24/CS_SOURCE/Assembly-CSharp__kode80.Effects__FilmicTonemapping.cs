using UnityEngine;

namespace kode80.Effects;

[ExecuteInEditMode]
public class FilmicTonemapping : MonoBehaviour
{
	[Range(0f, 16f)]
	public float exposure = 1.5f;

	private Material _material;

	private void Start()
	{
	}

	private void Update()
	{
	}

	[ImageEffectTransformsToLDR]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (_material == null)
		{
			_material = new Material(Shader.Find("Hidden/kode80/Effects/FilmicTonemapping"));
			_material.hideFlags = HideFlags.HideAndDontSave;
		}
		_material.SetFloat("_Exposure", exposure);
		Graphics.Blit(source, destination, _material);
	}
}
