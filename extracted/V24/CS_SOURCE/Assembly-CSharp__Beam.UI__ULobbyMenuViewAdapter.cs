using System;
using System.Collections.Generic;
using Beam.Utilities;
using Funlabs;
using UdpKit;
using UnityEngine;

namespace Beam.UI;

public class ULobbyMenuViewAdapter : LobbyMenuViewAdapterBase
{
	[SerializeField]
	private GameObject _panel;

	[SerializeField]
	private ULobbyElement _lobbyElementButtonPrefab;

	[SerializeField]
	private ULobbyEntryElement _lobbyEntryElementPrefab;

	[SerializeField]
	private Transform _lobbyButtonContainer;

	[SerializeField]
	private UScrollviewViewAdapter _scrollView;

	[SerializeField]
	private Transform _lobbyEntryElementContainer;

	[SerializeField]
	private TMPLabelViewAdapter _retrievingLabel;

	[SerializeField]
	private TMPLabelViewAdapter _noLobbyEntriesLabel;

	[SerializeField]
	private UButtonViewAdapter _enterCodeButton;

	[SerializeField]
	private UButtonViewAdapter _backButton;

	[SerializeField]
	private UButtonViewAdapter _applyButton;

	[SerializeField]
	private UScrollviewViewAdapter _crollview;

	[SerializeField]
	private TMPOptionButtonViewAdapter _regionOptionButton;

	[SerializeField]
	private TMPLabelViewAdapter _regionLabel;

	private HashSet<ILobbyElement> _lobbyButtons = new HashSet<ILobbyElement>();

	public override IButtonViewAdapter EnterCodeButton => _enterCodeButton;

	public override IButtonViewAdapter BackButton => _backButton;

	public override IButtonViewAdapter ApplyButton => _applyButton;

	public override IScrollviewViewAdapter Scrollview => _crollview;

	public override IOptionButtonViewAdapter RegionOptionButton => _regionOptionButton;

	public override ILabelViewAdapter RegionLabel => _regionLabel;

	public override IEnumerable<ILobbyElement> LobyButtons => _lobbyButtons;

	private void Awake()
	{
		Visible = _panel.activeSelf;
	}

	public override void Show()
	{
		if (!_panel.activeSelf)
		{
			_panel.SetActive(value: true);
		}
		base.Show();
	}

	public override void Hide()
	{
		if (_panel.activeSelf)
		{
			BeamTeam.DestroyAllChildren(_lobbyButtonContainer);
			BeamTeam.DestroyAllChildren(_lobbyEntryElementContainer);
			_lobbyButtons.Clear();
			_panel.SetActive(value: false);
		}
		base.Hide();
	}

	public override ILobbyElement CreateLobbyButton(Session_Filter sessionFilter)
	{
		ULobbyElement uLobbyElement = UnityEngine.Object.Instantiate(_lobbyElementButtonPrefab);
		uLobbyElement.transform.SetParent(_lobbyButtonContainer, worldPositionStays: false);
		uLobbyElement.SetLobbyFilter(sessionFilter);
		uLobbyElement.Selected += SessionFilterButton_Selected;
		_lobbyButtons.Add(uLobbyElement);
		return uLobbyElement;
	}

	private ULobbyEntryElement CreateLobbyEntryElement()
	{
		ULobbyEntryElement uLobbyEntryElement = UnityEngine.Object.Instantiate(_lobbyEntryElementPrefab);
		uLobbyEntryElement.transform.SetParent(_lobbyEntryElementContainer, worldPositionStays: false);
		return uLobbyEntryElement;
	}

	public override void UpdateLobbydEntries(Map<Guid, UdpSession> sessionList)
	{
		foreach (Transform item in _lobbyEntryElementContainer)
		{
			ULobbyEntryElement component = item.GetComponent<ULobbyEntryElement>();
			if (!component.IsNullOrDestroyed() && sessionList.TryFind(component.GetGuid(), out var value) && !value.GetToken().IsNullOrDestroyed())
			{
				component.SetSession(component.GetGuid(), value, component.GetSessionFilter());
			}
		}
	}

	public override ULobbyEntryElement CreateLobbydEntries(List<UdpSession> sessionList, Session_Filter selectedFilter, Guid selectedGuid, Action<object> onEntryButtonClick, Action<ISelectableViewAdapter> onEntryButtonSelect)
	{
		CleanButtons();
		int num = 1;
		ULobbyEntryElement uLobbyEntryElement = null;
		foreach (UdpSession session in sessionList)
		{
			MultiplayerTokens.Session token = session.GetToken();
			if (token != null && token.IsShowable())
			{
				ULobbyEntryElement uLobbyEntryElement2 = CreateLobbyEntryElement();
				uLobbyEntryElement2.SetSession(session.Id, session, selectedFilter);
				uLobbyEntryElement2.SetRank(num);
				num++;
				if (uLobbyEntryElement == null)
				{
					uLobbyEntryElement = uLobbyEntryElement2;
				}
				if (uLobbyEntryElement2.GetGuid() == selectedGuid)
				{
					uLobbyEntryElement = uLobbyEntryElement2;
				}
				TMPLabelButtonViewAdapter component = uLobbyEntryElement2.gameObject.GetComponent<TMPLabelButtonViewAdapter>();
				component.Click += onEntryButtonClick;
				component.Selected += onEntryButtonSelect;
			}
		}
		Canvas.ForceUpdateCanvases();
		return uLobbyEntryElement;
	}

	public override IButtonViewAdapter GetFirstButton()
	{
		IButtonViewAdapter buttonViewAdapter = _lobbyButtons.FirstOrDefault_NonAlloc();
		if (buttonViewAdapter.IsNullOrDestroyed())
		{
			return _backButton;
		}
		return buttonViewAdapter;
	}

	public override void CleanButtons()
	{
		foreach (Transform item in _lobbyEntryElementContainer)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void SessionFilterButton_Selected(ISelectableViewAdapter widget)
	{
		_scrollView.FocusElement(widget.transform);
	}

	public void ToggleNoEntriesFoundLabel(bool value)
	{
		_noLobbyEntriesLabel.transform.gameObject.SetActive(value);
	}
}
