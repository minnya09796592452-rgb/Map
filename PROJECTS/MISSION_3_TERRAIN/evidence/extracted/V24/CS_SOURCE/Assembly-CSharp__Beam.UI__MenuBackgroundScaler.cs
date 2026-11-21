using UnityEngine;
using UnityEngine.UI;

namespace Beam.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(RawImage))]
public class MenuBackgroundScaler : MonoBehaviour
{
	[SerializeField]
	private float _width;

	[SerializeField]
	private float _height;

	[SerializeField]
	private RectTransform _fillRect;

	[SerializeField]
	private RectTransform _fadeRect;

	[SerializeField]
	private float _maxFadeWidth;

	private Canvas _canvas;

	private RawImage _image;

	private void OnEnable()
	{
		if (_image == null)
		{
			_image = GetComponent<RawImage>();
		}
		_image.rectTransform.anchorMin = new Vector2(0f, 0.5f);
		_image.rectTransform.anchorMax = new Vector2(0f, 0.5f);
		_image.rectTransform.pivot = new Vector2(0f, 0.5f);
	}

	private void Update()
	{
		if (_canvas == null)
		{
			_canvas = GetComponentInParent<Canvas>();
		}
		Vector2 sizeDelta = ((RectTransform)_canvas.transform).sizeDelta;
		if (sizeDelta.x / sizeDelta.y < 1.7777778f)
		{
			_image.rectTransform.sizeDelta = new Vector3(sizeDelta.y * 1.7777778f, sizeDelta.y, 1f);
		}
		else
		{
			_image.rectTransform.sizeDelta = new Vector3(1920f, sizeDelta.y, 1f);
			_fillRect.sizeDelta = new Vector2(sizeDelta.x - _image.rectTransform.sizeDelta.x, _fillRect.sizeDelta.y);
			_fadeRect.sizeDelta = new Vector2(Mathf.Clamp(_fillRect.rect.width * 4f, 0f, _maxFadeWidth), _fadeRect.sizeDelta.y);
		}
		_image.rectTransform.anchoredPosition = Vector2.zero;
	}
}
