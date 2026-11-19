using UnityEngine;

namespace uTools;

[AddComponentMenu("uTools/Tween/Tween Scale(uTools)")]
public class uTweenScale : uTweener
{
	public Vector3 from = Vector3.zero;

	public Vector3 to = Vector3.one;

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
			return cachedRectTransform.localScale;
		}
		set
		{
			cachedRectTransform.localScale = value;
		}
	}

	protected override void OnUpdate(float factor, bool isFinished)
	{
		value = from + factor * (to - from);
	}

	public static uTweenScale Begin(GameObject go, Vector3 from, Vector3 to, float duration = 1f, float delay = 0f)
	{
		uTweenScale uTweenScale2 = uTweener.Begin<uTweenScale>(go, duration);
		uTweenScale2.from = from;
		uTweenScale2.to = to;
		uTweenScale2.duration = duration;
		uTweenScale2.delay = delay;
		if (duration <= 0f)
		{
			uTweenScale2.Sample(1f, _isFinished: true);
			uTweenScale2.enabled = false;
		}
		return uTweenScale2;
	}
}
