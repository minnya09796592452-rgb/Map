using System.Collections;
using Beam;
using UnityEngine;

namespace LE_LevelEditor.Logic;

public class LE_ScreenshotCamera : Singleton<LE_ScreenshotCamera>
{
	[SerializeField]
	private Animation _cameraShutter;

	private Transform _camTransform;

	private Camera _cam;

	private int replacementCounter;

	private void Start()
	{
		_cam = Camera.main;
		_camTransform = _cam.GetComponent<Transform>();
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) && !Singleton<LE_LevelEditorMain>.Instance.IsMouseOverGUI())
		{
			RenderLevelPreviewScreenshot();
		}
	}

	private void RenderLevelPreviewScreenshot()
	{
		gameObject.GetComponent<AudioSource>().Play();
		_cameraShutter.Play();
		StartCoroutine(DrawLevelPreview());
	}

	private IEnumerator DrawLevelPreview()
	{
		int imageWidth = 540;
		int imageHeight = 320;
		Singleton<LE_LevelEditorMain>.Instance.Boundary.SetActive(value: false);
		yield return new WaitForEndOfFrame();
		Texture2D texture2D = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, mipChain: false, linear: false);
		_ = _cam.pixelRect;
		Rect source = new Rect((float)Screen.width * 0.5f - (float)imageWidth * 0.5f, (float)Screen.height * 0.5f - (float)imageHeight * 0.5f, imageWidth, imageHeight);
		_cam.Render();
		texture2D.ReadPixels(source, 0, 0);
		texture2D.Apply(updateMipmaps: false);
		if (Singleton<LE_LevelEditorMain>.Instance.MapScreenshots.Count >= 8)
		{
			Singleton<LE_LevelEditorMain>.Instance.MapScreenshots[replacementCounter] = texture2D;
			replacementCounter++;
			if (replacementCounter >= 8)
			{
				replacementCounter = 0;
			}
		}
		else
		{
			Singleton<LE_LevelEditorMain>.Instance.MapScreenshots.Add(texture2D);
		}
		Singleton<LE_LevelEditorMain>.Instance.Boundary.SetActive(value: true);
	}

	public void TakeManualScreenShots()
	{
		_ = _camTransform.position;
		_camTransform.transform.position = new Vector3(165f, 0f, 113f);
		_camTransform.transform.LookAt(Vector3.zero);
		RenderLevelPreviewScreenshot();
		_camTransform.position = new Vector3(-165f, 0f, 113f);
		_camTransform.LookAt(Vector3.zero);
		RenderLevelPreviewScreenshot();
		_camTransform.position = new Vector3(165f, 0f, -113f);
		_camTransform.LookAt(Vector3.zero);
		RenderLevelPreviewScreenshot();
		_camTransform.position = new Vector3(-165f, 0f, -113f);
		_camTransform.LookAt(Vector3.zero);
		RenderLevelPreviewScreenshot();
	}
}
