using UnityEngine;
using UnityEngine.UI;

namespace uTools;

[AddComponentMenu("uTools/Tween/Tween Alpha(uTools)")]
public class uTweenAlpha : uTweenValue
{
	public bool includeChilds;

	private Text mText;

	private Light mLight;

	private Material mMat;

	private Image mImage;

	private SpriteRenderer mSpriteRender;

	private float mAlpha;

	public float alpha
	{
		get
		{
			return mAlpha;
		}
		set
		{
			SetAlpha(transform, value);
			mAlpha = value;
		}
	}

	protected override void ValueUpdate(float value, bool isFinished)
	{
		alpha = value;
	}

	private void SetAlpha(Transform _transform, float _alpha)
	{
		Color white = Color.white;
		mText = _transform.GetComponent<Text>();
		if (mText != null)
		{
			white = mText.color;
			white.a = _alpha;
			mText.color = white;
		}
		mLight = _transform.GetComponent<Light>();
		if (mLight != null)
		{
			white = mLight.color;
			white.a = _alpha;
			mLight.color = white;
		}
		mImage = _transform.GetComponent<Image>();
		if (mImage != null)
		{
			white = mImage.color;
			white.a = _alpha;
			mImage.color = white;
		}
		mSpriteRender = _transform.GetComponent<SpriteRenderer>();
		if (mSpriteRender != null)
		{
			white = mSpriteRender.color;
			white.a = _alpha;
			mSpriteRender.color = white;
		}
		if (_transform.GetComponent<Renderer>() != null)
		{
			mMat = _transform.GetComponent<Renderer>().material;
			if (mMat != null)
			{
				white = mMat.color;
				white.a = _alpha;
				mMat.color = white;
			}
		}
		if (includeChilds)
		{
			for (int i = 0; i < _transform.childCount; i++)
			{
				Transform child = _transform.GetChild(i);
				SetAlpha(child, _alpha);
			}
		}
	}
}
