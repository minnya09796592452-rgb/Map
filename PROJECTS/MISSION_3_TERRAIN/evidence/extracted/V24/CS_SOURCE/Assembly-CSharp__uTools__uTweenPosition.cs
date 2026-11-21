using UnityEngine;

namespace uTools;

[AddComponentMenu("uTools/Tween/Tween Position(uTools)")]
public class uTweenPosition : uTweener
{
	public Vector3 from;

	public Vector3 to;

	private RectTransform mRectTransform;

	public RectTransform cachedRectTransform
	{
		get
		{
			if (mRectTransform == null)
			{
				mRectTransform = GetComponent<RectTransform>();
			}
			return mRectTransform;
		}
	}

	public Vector3 value
	{
		get
		{
			return cachedRectTransform.localPosition;
		}
		set
		{
			cachedRectTransform.localPosition = value;
		}
	}

	protected override void OnUpdate(float factor, bool isFinished)
	{
		value = from + factor * (to - from);
	}

	public static uTweenPosition Begin(GameObject go, Vector3 from, Vector3 to, float duration = 1f, float delay = 0f)
	{
		uTweenPosition uTweenPosition2 = uTweener.Begin<uTweenPosition>(go, duration);
		uTweenPosition2.from = from;
		uTweenPosition2.to = to;
		uTweenPosition2.duration = duration;
		uTweenPosition2.delay = delay;
		if (duration <= 0f)
		{
			uTweenPosition2.Sample(1f, _isFinished: true);
			uTweenPosition2.enabled = false;
		}
		return uTweenPosition2;
	}

	[ContextMenu("Set 'From' to current value")]
	public override void SetStartToCurrentValue()
	{
		from = value;
	}

	[ContextMenu("Set 'To' to current value")]
	public override void SetEndToCurrentValue()
	{
		to = value;
	}

	[ContextMenu("Assume value of 'From'")]
	public override void SetCurrentValueToStart()
	{
		value = from;
	}

	[ContextMenu("Assume value of 'To'")]
	public override void SetCurrentValueToEnd()
	{
		value = to;
	}
}
