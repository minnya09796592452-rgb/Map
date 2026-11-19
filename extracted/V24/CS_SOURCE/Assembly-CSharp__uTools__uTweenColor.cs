using UnityEngine;
using UnityEngine.UI;

namespace uTools;

[AddComponentMenu("uTools/Tween/Tween Color(uTools)")]
public class uTweenColor : uTweener
{
	public Color from = Color.white;

	public Color to = Color.white;

	public bool includeChilds;

	private Text mText;

	private Light mLight;

	private Material mMat;

	private Image mImage;

	private SpriteRenderer mSpriteRender;

	private Color mColor = Color.white;

	public Color colorValue
	{
		get
		{
			return mColor;
		}
		set
		{
			SetColor(transform, value);
			mColor = value;
		}
	}

	protected override void OnUpdate(float factor, bool isFinished)
	{
		colorValue = Color.Lerp(from, to, factor);
	}

	public static uTweenColor Begin(GameObject go, float duration, float delay, Color from, Color to)
	{
		uTweenColor uTweenColor2 = uTweener.Begin<uTweenColor>(go, duration);
		uTweenColor2.from = from;
		uTweenColor2.to = to;
		uTweenColor2.delay = delay;
		if (duration <= 0f)
		{
			uTweenColor2.Sample(1f, _isFinished: true);
			uTweenColor2.enabled = false;
		}
		return uTweenColor2;
	}

	private void SetColor(Transform _transform, Color _color)
	{
		mText = _transform.GetComponent<Text>();
		if (mText != null)
		{
			mText.color = _color;
		}
		mLight = _transform.GetComponent<Light>();
		if (mLight != null)
		{
			mLight.color = _color;
		}
		mImage = _transform.GetComponent<Image>();
		if (mImage != null)
		{
			mImage.color = _color;
		}
		mSpriteRender = _transform.GetComponent<SpriteRenderer>();
		if (mSpriteRender != null)
		{
			mSpriteRender.color = _color;
		}
		if (_transform.GetComponent<Renderer>() != null)
		{
			mMat = _transform.GetComponent<Renderer>().material;
			if (mMat != null)
			{
				mMat.color = _color;
			}
		}
		if (includeChilds)
		{
			for (int i = 0; i < _transform.childCount; i++)
			{
				Transform child = _transform.GetChild(i);
				SetColor(child, _color);
			}
		}
	}
}
