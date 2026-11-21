using System.Collections.Generic;
using UnityEngine;

namespace uTools;

public class uTweenPath : uTweenValue
{
	public RectTransform target;

	public List<Vector3> paths;

	private int mIndex = -1;

	private int mPathsCount;

	private bool mCache;

	private int pathIndex
	{
		get
		{
			return mIndex;
		}
		set
		{
			if (mIndex != value)
			{
				mIndex = value;
				Debug.Log(target.localPosition);
				uTweenPosition.Begin(target.gameObject, target.localPosition, paths[mIndex], duration / (float)paths.Count).loopStyle = LoopStyle.Loop;
			}
		}
	}

	private void Cache()
	{
		mCache = true;
		if (paths.Count > 1)
		{
			mPathsCount = paths.Count - 1;
		}
		if (target == null)
		{
			target = GetComponent<RectTransform>();
		}
		from = 0f;
		to = mPathsCount;
	}

	private void Update()
	{
	}

	protected override void ValueUpdate(float _factor, bool _isFinished)
	{
		if (!mCache)
		{
			Cache();
		}
		pathIndex = Mathf.FloorToInt(_factor);
		Debug.Log(pathIndex);
	}
}
