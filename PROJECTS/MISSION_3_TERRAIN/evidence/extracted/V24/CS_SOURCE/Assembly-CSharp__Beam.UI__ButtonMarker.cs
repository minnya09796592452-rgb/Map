using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beam.UI;

public class ButtonMarker : MonoBehaviour
{
	[SerializeField]
	private Image _marker;

	private GameObject _previous;

	private void Update()
	{
		GameObject currentSelectedGameObject = EventSystem.main.currentSelectedGameObject;
		if (currentSelectedGameObject == null)
		{
			_marker.enabled = false;
			_marker.transform.SetParent(null, worldPositionStays: false);
		}
		else if (currentSelectedGameObject != _previous)
		{
			SetPosition(currentSelectedGameObject.transform as RectTransform);
			_previous = currentSelectedGameObject;
		}
	}

	public void SetPosition(RectTransform rect)
	{
		if (!(rect == null))
		{
			_marker.transform.SetParent(rect, worldPositionStays: false);
			_marker.rectTransform.anchoredPosition3D = new Vector3(16f, 0f, 0f);
			_marker.enabled = true;
		}
	}
}
