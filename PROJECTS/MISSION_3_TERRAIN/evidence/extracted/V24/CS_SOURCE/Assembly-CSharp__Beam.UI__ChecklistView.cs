using System.Collections.Generic;
using Beam.Utilities;
using MEC;
using UnityEngine;

namespace Beam.UI;

public class ChecklistView : NotificationViewAdapterBase
{
	[SerializeField]
	private TMPLabelViewAdapter _titleLabel;

	[SerializeField]
	private TMPLabelViewAdapter _messageLabel;

	[SerializeField]
	private ChecklistElementController _checklistElementPrefab;

	[SerializeField]
	private Transform _checklistElementContainer;

	[SerializeField]
	private Animator _animator;

	private CoroutineHandle _hideSequence;

	public override ILabelViewAdapter TitleLabel => _titleLabel;

	public override ILabelViewAdapter MessageLabel => _messageLabel;

	private void OnDestroy()
	{
		Timing.KillCoroutines(_hideSequence);
	}

	public ChecklistElementController CreateChecklistElement()
	{
		ChecklistElementController checklistElementController = Object.Instantiate(_checklistElementPrefab);
		checklistElementController.transform.SetParent(_checklistElementContainer, worldPositionStays: false);
		return checklistElementController;
	}

	private void DestroyChecklistElements()
	{
		for (int i = 0; i < _checklistElementContainer.childCount; i++)
		{
			BeamTeam.Destroy(_checklistElementContainer.GetChild(i).gameObject);
		}
	}

	public override void Show()
	{
		_messageLabel.gameObject.SetActive(!string.IsNullOrEmpty(_messageLabel.Text));
		base.Show();
	}

	public override void Hide()
	{
		base.Hide();
		DestroyChecklistElements();
	}

	public override float Hide_Coroutine()
	{
		_hideSequence = Timing.RunCoroutine(Hide_Sequence());
		return Timing.WaitUntilDone(_hideSequence);
	}

	private IEnumerator<float> Hide_Sequence()
	{
		ChecklistElementController[] checklistElementControllers = _checklistElementContainer.GetComponentsInChildren<ChecklistElementController>();
		while (ChecklistElementViewIsBusyPredicate(checklistElementControllers))
		{
			yield return float.NegativeInfinity;
		}
		int stateNameHash = Animator.StringToHash("Close");
		_animator.Play(stateNameHash);
		float waitTime = Mathf.Abs(_animator.GetCurrentAnimatorStateInfo(0).length);
		yield return Timing.WaitForSeconds(waitTime);
		Hide();
	}

	private bool ChecklistElementViewIsBusyPredicate(ChecklistElementController[] checklistElements)
	{
		for (int i = 0; i < checklistElements?.Length; i++)
		{
			if (checklistElements[i].View.IsBusy)
			{
				return true;
			}
		}
		return false;
	}
}
