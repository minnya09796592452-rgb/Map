using UnityEngine;

namespace uTools;

[AddComponentMenu("uTools/Tween/Tween Rotation(uTools)")]
public class uTweenRotation : uTweener
{
	public Vector3 from;

	public Vector3 to;

	private RectTransform mRectTransfrom;

	public RectTransform target;

	public RectTransform cacheRectTransfrom
	{
		get
		{
			if (target == null)
			{
				mRectTransfrom = GetComponent<RectTransform>();
			}
			else
			{
				mRectTransfrom = target;
			}
			return mRectTransfrom;
		}
	}

	public Quaternion value
	{
		get
		{
			return cacheRectTransfrom.localRotation;
		}
		set
		{
			cacheRectTransfrom.localRotation = value;
		}
	}

	protected override void OnUpdate(float _factor, bool _isFinished)
	{
		value = Quaternion.Euler(Vector3.Lerp(from, to, _factor));
	}

	public static uTweenRotation Begin(GameObject go, Vector3 from, Vector3 to, float duration = 1f, float delay = 0f)
	{
		uTweenRotation uTweenRotation2 = uTweener.Begin<uTweenRotation>(go, duration);
		uTweenRotation2.from = from;
		uTweenRotation2.to = to;
		uTweenRotation2.duration = duration;
		uTweenRotation2.delay = delay;
		if (duration <= 0f)
		{
			uTweenRotation2.Sample(1f, _isFinished: true);
			uTweenRotation2.enabled = false;
		}
		return uTweenRotation2;
	}
}
