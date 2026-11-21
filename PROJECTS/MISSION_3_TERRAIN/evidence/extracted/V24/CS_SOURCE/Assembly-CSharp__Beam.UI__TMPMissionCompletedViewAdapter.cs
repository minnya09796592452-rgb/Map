using Beam.Utilities;
using UnityEngine;

namespace Beam.UI;

public class TMPMissionCompletedViewAdapter : MissionCompletedViewAdapterBase
{
	[SerializeField]
	private GameObjectViewAdapter _panel;

	[SerializeField]
	private Transform _rewardElementContainer;

	[SerializeField]
	private MissionRewardElement _rewardElementPrefab;

	[SerializeField]
	private TMPActionHudElementViewAdapter _actionLabelPrefab;

	[SerializeField]
	private Transform _actionLabelContainer;

	public override IGameObjectViewAdapter Panel => _panel;

	public override IMissionRewardElement CreateRewardElement()
	{
		if (_rewardElementPrefab != null)
		{
			MissionRewardElement missionRewardElement = Object.Instantiate(_rewardElementPrefab);
			missionRewardElement.transform.SetParent(_rewardElementContainer, worldPositionStays: false);
			return missionRewardElement;
		}
		return NullObject<NullMissionRewardElement>.Instance;
	}

	public override ActionHudElementViewAdapterBase CreateActionLabel()
	{
		if (_actionLabelPrefab != null)
		{
			TMPActionHudElementViewAdapter tMPActionHudElementViewAdapter = Object.Instantiate(_actionLabelPrefab);
			tMPActionHudElementViewAdapter.transform.SetParent(_actionLabelContainer, worldPositionStays: false);
			return tMPActionHudElementViewAdapter;
		}
		return NullActionHudElementViewAdapter.Instance;
	}

	public override void DestroyRewardElements()
	{
		BeamTeam.DestroyAllChildren(_rewardElementContainer);
	}

	public override void DestroyActionLabels()
	{
		BeamTeam.DestroyAllChildren(_actionLabelContainer);
	}
}
