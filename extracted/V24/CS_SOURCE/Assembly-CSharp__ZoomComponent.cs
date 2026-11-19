using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomComponent : MonoBehaviour, IScrollHandler, IEventSystemHandler
{
	[SerializeField]
	private RectTransform _target;

	[SerializeField]
	private RectTransform _viewport;

	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private Vector2 _border;

	[SerializeField]
	private float _minScale = 0.3f;

	[SerializeField]
	private float _maxScale = 1f;

	[SerializeField]
	private float _step = 0.25f;

	private float _currentScale = 1f;

	private int _childCount;

	public void SetScale(float scale)
	{
		_currentScale = Mathf.Clamp(scale, _minScale, _maxScale);
		_target.localScale = new Vector3(_currentScale, _currentScale, 1f);
	}

	public void SetMinScale(float minScale)
	{
		_minScale = minScale;
		if (_currentScale < _minScale)
		{
			SetScale(_minScale);
		}
	}

	public void SetMaxScale(float maxScale)
	{
		_maxScale = maxScale;
		if (_currentScale > _maxScale)
		{
			SetScale(_maxScale);
		}
	}

	private void Awake()
	{
		if (_target.anchorMin != new Vector2(0.5f, 0.5f) || _target.anchorMax != new Vector2(0.5f, 0.5f))
		{
			Debug.LogWarning("Anchors are not set to Middle(center)");
		}
		_camera = transform.GetComponentInParent<Canvas>().worldCamera;
	}

	public void Reset()
	{
		_target.pivot = new Vector2(0.5f, 0.5f);
		_target.localPosition = Vector3.zero;
	}

	public void EncapsulateContent()
	{
		int childCount = _target.childCount;
		if (_childCount == childCount)
		{
			return;
		}
		_childCount = childCount;
		Bounds bounds = default;
		for (int i = 0; i < _target.childCount; i++)
		{
			RectTransform rectTransform = _target.GetChild(i) as RectTransform;
			if (!(rectTransform == null))
			{
				Bounds bounds2 = new Bounds(rectTransform.localPosition, Vector3.one * rectTransform.rect.width);
				bounds.Encapsulate(bounds2);
			}
		}
		bounds.extents += new Vector3(_border.x, _border.y, 0f);
		_target.sizeDelta = new Vector2(bounds.size.x, bounds.size.y);
	}

	public void ScaleToFitContent()
	{
		EncapsulateContent();
		float num = Mathf.Max(_target.sizeDelta.x, _target.sizeDelta.y);
		float num2 = Mathf.Min(_viewport.rect.width, _viewport.rect.height) / num;
		num2 *= 0.999f;
		SetMinScale(num2);
		SetScale(num2);
	}

	void IScrollHandler.OnScroll(PointerEventData eventData)
	{
		float num = eventData.scrollDelta.y * _step;
		float newScaleValue = _currentScale + num;
		ApplyScale(newScaleValue, eventData.position);
	}

	private void ApplyScale(float newScaleValue, Vector2 position)
	{
		float scale = Mathf.Clamp(newScaleValue, _minScale, _maxScale);
		if (!scale.Equals(_currentScale))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_target, position, _camera, out var localPoint);
			SetPivot(_target, localPoint);
			SetScale(scale);
		}
	}

	private void SetPivot(RectTransform rectTransform, Vector2 localPoint)
	{
		Rect rect = rectTransform.rect;
		float x = (float)(((double)localPoint.x - (double)rect.x) / ((double)rect.xMax - (double)rect.x));
		float y = (float)(((double)localPoint.y - (double)rect.y) / ((double)rect.yMax - (double)rect.y));
		Vector2 vector = new Vector2(x, y);
		Vector2 vector2 = (rectTransform.pivot - vector) * _currentScale;
		Vector2 size = rect.size;
		Vector3 vector3 = new Vector3(vector2.x * size.x, vector2.y * size.y) * -1f;
		rectTransform.pivot = vector;
		rectTransform.localPosition += vector3;
	}
}
