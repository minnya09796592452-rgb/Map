using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beam.UI;

public class TMPOptionInputFieldButtonViewAdapter : USelectableViewAdapter, IOptionInputFieldButtonViewAdapter, IOptionWidgetViewAdapter, ISelectableViewAdapter, IViewAdapter, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, ISelectHandler
{
	[SerializeField]
	private TMPLabelViewAdapter _label;

	[SerializeField]
	private TMPInputFieldButtonViewAdapter _inputFieldButtonPrefab;

	[SerializeField]
	private Transform _inputFieldButtonContainer;

	private List<IInputFieldButtonViewAdapter> _buttons = new List<IInputFieldButtonViewAdapter>();

	protected ILabelViewAdapter Label
	{
		get
		{
			if (!_label)
			{
				_label = GetComponent<TMPLabelViewAdapter>();
			}
			return _label;
		}
	}

	public string Text
	{
		get
		{
			return Label.Text;
		}
		set
		{
			Label.Text = value;
		}
	}

	public IList<IInputFieldButtonViewAdapter> Buttons => _buttons;

	public string GetTooltip()
	{
		return string.Empty;
	}

	protected override void OnDestroy()
	{
		_buttons.Clear();
		base.OnDestroy();
	}

	public IInputFieldButtonViewAdapter CreateButton()
	{
		TMPInputFieldButtonViewAdapter tMPInputFieldButtonViewAdapter = Object.Instantiate(_inputFieldButtonPrefab);
		tMPInputFieldButtonViewAdapter.transform.SetParent(_inputFieldButtonContainer, worldPositionStays: false);
		_buttons.Add(tMPInputFieldButtonViewAdapter);
		return tMPInputFieldButtonViewAdapter;
	}

	public void SetNavigation()
	{
		IInputFieldButtonViewAdapter inputFieldButtonViewAdapter = _buttons[0];
		IInputFieldButtonViewAdapter inputFieldButtonViewAdapter2 = _buttons[1];
		if (inputFieldButtonViewAdapter.Enabled)
		{
			transform.SetNavigation(NavigationDirection.Right, inputFieldButtonViewAdapter.transform);
			inputFieldButtonViewAdapter.transform.SetNavigation(NavigationDirection.Left, transform);
			if (inputFieldButtonViewAdapter2.Enabled)
			{
				inputFieldButtonViewAdapter.transform.SetNavigation(NavigationDirection.Right, inputFieldButtonViewAdapter2.transform);
				inputFieldButtonViewAdapter2.transform.SetNavigation(NavigationDirection.Left, inputFieldButtonViewAdapter.transform);
			}
		}
		else if (inputFieldButtonViewAdapter2.Enabled)
		{
			transform.SetNavigation(NavigationDirection.Right, inputFieldButtonViewAdapter2.transform);
			inputFieldButtonViewAdapter2.transform.SetNavigation(NavigationDirection.Left, transform);
		}
	}

	void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
	{
		OnMouseEnter(this);
	}

	void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
	{
		OnMouseLeave(this);
	}

	void ISelectHandler.OnSelect(BaseEventData eventData)
	{
		OnSelected(this);
	}
}
