using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class Scaler : MonoBehaviour
{
	[SerializeField]
	private float _borderPx;

	private void Update()
	{
		RectTransform rectTransform = (RectTransform)transform.parent;
		float width = rectTransform.rect.width;
		float num;
		if ((num = rectTransform.rect.height) > width)
		{
			num = width;
		}
		num -= _borderPx * 2f;
		((RectTransform)transform).sizeDelta = new Vector2(num, num);
	}
}
