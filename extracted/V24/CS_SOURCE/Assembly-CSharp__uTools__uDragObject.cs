using UnityEngine;
using UnityEngine.EventSystems;

namespace uTools;

[AddComponentMenu("uTools/Internal/Drag Object(uTools)")]
public class uDragObject : MonoBehaviour, IDragHandler, IEventSystemHandler
{
	public RectTransform target;

	private RectTransform cacheTarget
	{
		get
		{
			if (target == null)
			{
				target = GetComponent<RectTransform>();
			}
			return target;
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector3 localPosition = cacheTarget.localPosition;
		Vector3 to = localPosition + new Vector3(eventData.delta.x, eventData.delta.y, 0f);
		uTweenPosition.Begin(gameObject, localPosition, to, 0.02f);
	}
}
