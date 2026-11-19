using UnityEngine;
using UnityEngine.EventSystems;

namespace uTools;

[AddComponentMenu("uTools/Tween/Button Scale(uTools)")]
public class uButtonScale : MonoBehaviour, uIPointHandler, IPointerEnterHandler, IEventSystemHandler, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IPointerExitHandler
{
	public RectTransform tweenTarget;

	public Vector3 enter = new Vector3(1.1f, 1.1f, 1.1f);

	public Vector3 down = new Vector3(1.05f, 1.05f, 1.05f);

	public float duration = 0.2f;

	private Vector3 mScale;

	private void Start()
	{
		if (tweenTarget == null)
		{
			tweenTarget = GetComponent<RectTransform>();
		}
		mScale = tweenTarget.localScale;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Scale(enter);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Scale(mScale);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		Scale(down);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		Scale(mScale);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
	}

	private void Scale(Vector3 to)
	{
		uTweenScale.Begin(tweenTarget.gameObject, tweenTarget.localScale, to, duration);
	}
}
