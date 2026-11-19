using UnityEngine;
using UnityEngine.UI;

public class UIMouseFollow : MonoBehaviour
{
	[SerializeField]
	private CanvasScaler _canvasScaler;

	private void Update()
	{
		GetComponent<RectTransform>().anchoredPosition = new Vector2(Input.mousePosition.x * _canvasScaler.referenceResolution.x / (float)Screen.width, Input.mousePosition.y * _canvasScaler.referenceResolution.y / (float)Screen.height);
	}
}
