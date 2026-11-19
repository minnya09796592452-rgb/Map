using System.Collections.Generic;
using Beam.Utilities;
using UnityEngine;

namespace Beam.UI;

public class ULeaderboardsMenuViewAdapter : LeaderboardsMenuViewAdapterBase
{
	[SerializeField]
	private GameObject _panel;

	[SerializeField]
	private ULeaderboardElement _leaderboardButtonPrefab;

	[SerializeField]
	private LeaderboardEntryElement _leaderboardEntryElementPrefab;

	[SerializeField]
	private Transform _leaderboardButtonContainer;

	[SerializeField]
	private UScrollviewViewAdapter _scrollView;

	[SerializeField]
	private Transform _leaderboardEntryElementContainer;

	[SerializeField]
	private LeaderboardEntryElement _userLeaderboardEntryElement;

	[SerializeField]
	private TMPLabelViewAdapter _retrievingLabel;

	[SerializeField]
	private TMPLabelViewAdapter _noLeaderboardEntriesLabel;

	[SerializeField]
	private UButtonViewAdapter _backButton;

	private HashSet<ILeaderboardElement> _leaderboardButtons = new HashSet<ILeaderboardElement>();

	private ELeaderboardDisplayType _leaderboardDisplayType;

	public override IButtonViewAdapter BackButton => _backButton;

	public override IEnumerable<ILeaderboardElement> LeaderboardButtons => _leaderboardButtons;

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
			BeamTeam.DestroyAllChildren(_leaderboardButtonContainer);
			BeamTeam.DestroyAllChildren(_leaderboardEntryElementContainer);
			_leaderboardButtons.Clear();
			_panel.SetActive(value: false);
		}
		base.Hide();
	}

	public override ILeaderboardElement CreateLeaderboardButton(LeaderboardData leaderboardData)
	{
		ULeaderboardElement uLeaderboardElement = Object.Instantiate(_leaderboardButtonPrefab);
		uLeaderboardElement.transform.SetParent(_leaderboardButtonContainer, worldPositionStays: false);
		uLeaderboardElement.SetLeaderboardData(leaderboardData);
		uLeaderboardElement.Selected += LeaderboardButton_Selected;
		_leaderboardButtons.Add(uLeaderboardElement);
		return uLeaderboardElement;
	}

	private LeaderboardEntryElement CreateLeaderboardEntryElement()
	{
		LeaderboardEntryElement leaderboardEntryElement = Object.Instantiate(_leaderboardEntryElementPrefab);
		leaderboardEntryElement.transform.SetParent(_leaderboardEntryElementContainer, worldPositionStays: false);
		return leaderboardEntryElement;
	}

	public override void CreateLeaderboardEntries(IEnumerable<LeaderboardEntryData> leaderboardEntries)
	{
		BeamTeam.DestroyAllChildrenImmediate(_leaderboardEntryElementContainer);
		int num = 1;
		foreach (LeaderboardEntryData leaderboardEntry in leaderboardEntries)
		{
			LeaderboardEntryElement leaderboardEntryElement = CreateLeaderboardEntryElement();
			PopulateLeaderboardEntryElement(leaderboardEntryElement, leaderboardEntry);
			leaderboardEntryElement.SetRank(num++);
		}
		bool flag = _leaderboardEntryElementContainer.childCount > 0;
		_leaderboardEntryElementContainer.gameObject.SetActive(flag);
		_noLeaderboardEntriesLabel.transform.gameObject.SetActive(!flag);
		_retrievingLabel.transform.gameObject.SetActive(value: false);
	}

	public override void SetLeaderboard(LeaderboardData leaderboardData)
	{
		_leaderboardDisplayType = leaderboardData.DisplayType;
		_leaderboardEntryElementContainer.gameObject.SetActive(value: false);
		_noLeaderboardEntriesLabel.transform.gameObject.SetActive(value: false);
		_retrievingLabel.transform.gameObject.SetActive(value: true);
	}

	public override void SetUserLeaderboardEntry(LeaderboardEntryData leaderboardEntryData)
	{
		PopulateLeaderboardEntryElement(_userLeaderboardEntryElement, leaderboardEntryData);
	}

	private void PopulateLeaderboardEntryElement(LeaderboardEntryElement leaderboardEntryElement, LeaderboardEntryData leaderboardEntryData)
	{
		leaderboardEntryElement.SetDisplayType(_leaderboardDisplayType);
		leaderboardEntryElement.SetUsername(leaderboardEntryData.Username);
		leaderboardEntryElement.SetRank(leaderboardEntryData.Rank);
		leaderboardEntryElement.SetValue(leaderboardEntryData.Value);
	}

	public override IButtonViewAdapter GetFirstButton()
	{
		IButtonViewAdapter buttonViewAdapter = _leaderboardButtons.FirstOrDefault_NonAlloc();
		if (buttonViewAdapter.IsNullOrDestroyed())
		{
			return _backButton;
		}
		return buttonViewAdapter;
	}

	private void LeaderboardButton_Selected(ISelectableViewAdapter widget)
	{
		_scrollView.FocusElement(widget.transform);
	}
}
