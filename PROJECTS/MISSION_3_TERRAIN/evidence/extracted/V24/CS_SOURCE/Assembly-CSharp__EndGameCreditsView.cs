using System;
using System.Collections;
using Beam.UI;
using UnityEngine;
using UnityEngine.UI;

public class EndGameCreditsView : MonoBehaviour
{
	[SerializeField]
	private GameObject _container;

	[SerializeField]
	private RectTransform _credits;

	[SerializeField]
	private TMPLabelViewAdapter _copyrightText;

	[SerializeField]
	private TMPLabelViewAdapter _creditsText;

	[SerializeField]
	private GameObject _headerGroup;

	[SerializeField]
	private GameObject _footerGroup;

	[SerializeField]
	private RectTransform _footer;

	[SerializeField]
	private float _speed = 1f;

	private RectTransform _canvas;

	public event Action Finished;

	private void OnDestroy()
	{
		Finished = null;
	}

	public void SetCreditsText(string text)
	{
		_creditsText.Text = text;
	}

	public void SetCopyrightText(string text)
	{
		_copyrightText.Text = text;
	}

	public void Show()
	{
		if (!_container.activeInHierarchy)
		{
			_container.SetActive(value: true);
		}
		_canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
		Vector3 vector = _credits.anchoredPosition;
		vector.y = 0f;
		_credits.anchoredPosition = vector;
		_footerGroup.GetComponent<LayoutElement>().minHeight = _canvas.rect.height * 1.1f;
		StartCoroutine(Scroll_Sequence());
	}

	public void Hide()
	{
		if (_container.activeInHierarchy)
		{
			_container.SetActive(value: false);
		}
	}

	private IEnumerator Scroll_Sequence()
	{
		yield return new WaitForSeconds(3f);
		bool stop = false;
		while (!stop)
		{
			Vector3 vector = _credits.anchoredPosition;
			vector.y += _speed * Time.deltaTime;
			_credits.anchoredPosition = vector;
			if (vector.y > _canvas.rect.height / 2f + _credits.rect.height - _footer.rect.height / 2f)
			{
				stop = true;
			}
			yield return null;
		}
		yield return new WaitForSeconds(3f);
		Finished?.Invoke();
	}
}
