using UnityEngine;

namespace Beam.UI.MapEditor;

public class UMapHandle : ViewAdapterBase, IMapHandle, IViewAdapter
{
	[SerializeField]
	private RectTransform _handle;

	[SerializeField]
	private UTextureViewAdapter _handleSprite;

	public Vector3 Position => transform.position;

	public ITextureViewAdapter HandleSprite => _handleSprite;

	public override void Show()
	{
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(value: true);
			base.Show();
		}
	}

	public override void Hide()
	{
		if (gameObject.activeSelf)
		{
			gameObject.SetActive(value: false);
			base.Hide();
		}
	}

	public void SetSize(Vector2 size)
	{
		_handle.sizeDelta = size;
	}

	public void SetScale(Vector2 scale)
	{
		_handle.localScale = new Vector3(scale.x, scale.y, 1f);
	}

	public void SetPosition(Vector2 screenPosition)
	{
		RectTransform rectTransform = (RectTransform)transform.parent;
		float num = rectTransform.sizeDelta.x / (float)Screen.width;
		float num2 = rectTransform.sizeDelta.y / (float)Screen.height;
		Vector2 anchoredPosition = new Vector2(screenPosition.x * num, screenPosition.y * num2);
		((RectTransform)transform).anchoredPosition = anchoredPosition;
	}
}
