using System.Collections.Generic;
using Beam.Crafting;
using Beam.Events;
using Beam.Language;
using Beam.Utilities;
using UnityEngine;

namespace Beam.UI;

public class HudPresenter : ControllerBase, INeedPlayerComponent, IRefresh
{
	private const float DEFAULT_CROSSHAIR_SCALE = 0.3f;

	[SerializeField]
	private HudViewAdapterBase _view;

	private IPlayer _player;

	private Raycaster _raycaster;

	private Interacter _interacter;

	private Crafter _crafter;

	private List<IGameActionProvider> _gameActionProviders = new List<IGameActionProvider>();

	private Stack<ActionHudElementViewAdapterBase> _actionElementStack = new Stack<ActionHudElementViewAdapterBase>();

	private List<ActionHudElementViewAdapterBase> _activeActionElements = new List<ActionHudElementViewAdapterBase>();

	private bool _showCursor;

	private bool _showAction;

	private bool _suspended;

	IPlayer INeedPlayerComponent.Player => _player;

	public void Start()
	{
		if (_view == null)
		{
			Debug.LogError("HudPresenter:: No view component assigned!");
			_view = NullHudViewAdapter.Instance;
		}
		EventManager.AddListener<OptionsAppliedEvent>(EventManager_OptionsAppliedEvent);
		EventManager.AddListener<EndGameSequenceEvent>(EventManager_EndGameSequenceEvent);
		LoadOptions();
		Reset();
	}

	public override void SetView(ViewAdapterBase view)
	{
		_view = view as HudViewAdapterBase;
		if (_view == null)
		{
			Debug.LogError("HudPresenter:: No view component assigned!");
			_view = NullHudViewAdapter.Instance;
		}
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
		_player.Movement.ClimbChanged += PlayerMovement_ClimbChanged;
		_raycaster = _player.GameInput.Raycaster;
		_raycaster.EnterObject += Raycaster_EnterObject;
		_raycaster.LeaveObject += Raycaster_LeaveObject;
		_interacter = _player.GameInput.Interacter;
		_interacter.InteractingEnabled += Interacter_InteractingEnabled;
		_interacter.InteractingUpdated += Interacter_InteractingUpdated;
		_interacter.InteractingFinished += Interacter_InteractingFinished;
		_crafter = _player.Crafter;
		_crafter.StartedPlacing += Crafter_StartedPlacing;
		_gameActionProviders.Add(_player.Holder);
		_gameActionProviders.Add(_interacter);
		_gameActionProviders.Add(_player.PlayerUI.Canvas.GetComponentInChildren<StorageMenuPresenter>());
	}

	private void OnDestroy()
	{
		if (!_player.IsNullOrDestroyed() && _player.Movement != null)
		{
			_player.Movement.ClimbChanged -= PlayerMovement_ClimbChanged;
		}
		if (_interacter != null)
		{
			_interacter.InteractingEnabled -= Interacter_InteractingEnabled;
			_interacter.InteractingUpdated -= Interacter_InteractingUpdated;
			_interacter.InteractingFinished -= Interacter_InteractingFinished;
		}
		if (_raycaster != null)
		{
			_raycaster.EnterObject -= Raycaster_EnterObject;
			_raycaster.LeaveObject -= Raycaster_LeaveObject;
		}
		if (_crafter != null)
		{
			_crafter.StartedPlacing -= Crafter_StartedPlacing;
		}
		EventManager.RemoveListener<OptionsAppliedEvent>(EventManager_OptionsAppliedEvent);
		EventManager.RemoveListener<EndGameSequenceEvent>(EventManager_EndGameSequenceEvent);
	}

	public void LoadOptions()
	{
		_showAction = Options.GeneralSettings.HudInformation == Quality_HUDInformation.ITEM_NAMES;
		_showCursor = Options.GeneralSettings.ShowCrosshair == Quality_OFF_ON.ON;
		Refresh(RefreshUIMode.Refresh);
		Debug.Log("HudPresenter::LoadOptions:: Loaded options.");
	}

	private void EventManager_OptionsAppliedEvent(OptionsAppliedEvent e)
	{
		LoadOptions();
	}

	private void EventManager_EndGameSequenceEvent(EndGameSequenceEvent e)
	{
		if (Game.Mode == GameMode.Coop_2 && _player.Id == 1)
		{
			Suspend(suspend: true);
		}
	}

	private ActionHudElementViewAdapterBase PopActionElement()
	{
		ActionHudElementViewAdapterBase actionHudElementViewAdapterBase = ((_actionElementStack.Count <= 0) ? Object.Instantiate(_view.ActionHudElementPrefab) : _actionElementStack.Pop());
		actionHudElementViewAdapterBase.transform.SetParent(_view.ActionHudElementsContainer.Transform, worldPositionStays: false);
		_activeActionElements.Add(actionHudElementViewAdapterBase);
		return actionHudElementViewAdapterBase;
	}

	private void PushActionElement(ActionHudElementViewAdapterBase element)
	{
		element.transform.SetParent(_view.PooledActionHudElementsContainer.Transform, worldPositionStays: false);
		_actionElementStack.Push(element);
	}

	private void PoolActionElements()
	{
		for (int i = 0; i < _activeActionElements.Count; i++)
		{
			ActionHudElementViewAdapterBase element = _activeActionElements[i];
			PushActionElement(element);
		}
		_activeActionElements.Clear();
	}

	private void Interacter_InteractingEnabled(bool enabled)
	{
		_view.DefaultCursor.Color = Color.white;
		_view.DefaultCursor.Enabled = enabled || _showCursor;
		_view.InteractingCursor.Color = Color.white;
		_view.InteractingCursor.Enabled = enabled;
	}

	private void Interacter_InteractingUpdated(float time)
	{
		float num = Mathf.Clamp(0.3f + 0.6f * (4f * time), 0f, 0.9f);
		_view.DefaultCursor.transform.localScale = Vector3.one * num;
	}

	private void Interacter_InteractingFinished()
	{
		_view.DefaultCursor.transform.localScale = Vector3.one * 0.3f;
		Refresh(RefreshUIMode.Refresh);
	}

	private void Raycaster_EnterObject(IBase obj)
	{
		Select(obj);
		_view.ClimbUI.gameObject.SetActive(_player.Movement.ClimbAvailable && CurrentObjectIsActiveLadder());
	}

	private void Raycaster_LeaveObject(IBase obj)
	{
		FadeOutAndReset();
	}

	private void Crafter_StartedPlacing()
	{
		FadeOutAndReset();
	}

	private void Select(IBase obj)
	{
		if (_suspended || _player.Statistics.IsKnockedOut)
		{
			return;
		}
		ISelectable selectable = obj as ISelectable;
		if (selectable.IsNullOrDestroyed())
		{
			FadeOutAndReset();
			return;
		}
		LeanTween.cancel(gameObject);
		_view.DefaultCursor.Enabled = _showCursor;
		_view.ItemNameLabel.Text = obj.DisplayName;
		_view.ItemActionGroup.Alpha = 1f;
		_view.ItemActionGroup.transform.gameObject.SetActive(_showAction);
		PoolActionElements();
		for (int i = 0; i < _gameActionProviders.Count; i++)
		{
			IEnumerable<IGameAction> gameActions = _gameActionProviders[i].GetGameActions();
			if (gameActions == null)
			{
				continue;
			}
			IPickupable pickupable = selectable as IPickupable;
			bool flag = false;
			if (!pickupable.IsNullOrDestroyed())
			{
				flag = pickupable.CanPickUp && (!_player.Holder.CanPickup(pickupable, notification: false) || _player.Movement.Vehicle != null);
			}
			foreach (IGameAction item in gameActions)
			{
				if (RewiredUtilities.IsActionEnabled(_player.Input?.id ?? 0, item.ElementRequiredId) && (!flag || item.ElementRequiredId != 9))
				{
					ActionHudElementViewAdapterBase element = PopActionElement();
					FormatActionElement(element, item.PlayerActionRequiredDescription, item.ElementRequiredId, item.ResultingActionDescription);
				}
			}
		}
		_view.ActionHudElementsContainer.Transform.gameObject.SetActive(_activeActionElements.Count > 0);
	}

	private void FormatActionElement(ActionHudElementViewAdapterBase element, string playerActionTerm, int elementRequired, string resultActionTerm)
	{
		string text = Localization.GetLanguageHandler().Localize("PLAYER_ACTION_SENTENCE", playerActionTerm, "PLAYER_ACTION_PREPOSITION", resultActionTerm);
		text = PlatformLocalization.GetHandler().SubstituteControllerGlyphs(text, _player.Id, elementRequired);
		element.PressLabel.Text = text;
	}

	private void FadeOutAndReset()
	{
		if (_view.ItemActionGroup.Alpha == 0f)
		{
			Reset();
		}
		else
		{
			LeanTween.value(gameObject, FadeCallback, 1f, 0f, 0.2f).setOnComplete(Reset);
		}
	}

	private void Reset()
	{
		LeanTween.cancel(gameObject);
		Color white = Color.white;
		white.a = 0.5f;
		_view.DefaultCursor.Color = white;
		_view.DefaultCursor.Enabled = !_suspended && _showCursor;
		_view.InteractingCursor.Enabled = false;
		_view.ItemNameLabel.Text = "";
		_view.ItemActionGroup.Alpha = 1f;
		_view.ItemActionGroup.transform.gameObject.SetActive(value: false);
	}

	public void Suspend(bool suspend)
	{
		_suspended = suspend;
		Refresh((!_suspended) ? RefreshUIMode.Refresh : RefreshUIMode.Clear);
	}

	public void Refresh(RefreshUIMode mode)
	{
		switch (mode)
		{
		case RefreshUIMode.Clear:
			Reset();
			break;
		case RefreshUIMode.Refresh:
			Select(_raycaster.CurrentObject);
			break;
		}
	}

	private void PlayerMovement_ClimbChanged(bool value)
	{
		if (CurrentObjectIsActiveLadder())
		{
			_view.ClimbUI.gameObject.SetActive(value);
		}
		if (_player.Movement.activeLadder.IsNullOrDestroyed())
		{
			_view.ClimbUI.gameObject.SetActive(value: false);
		}
		if (_view.ClimbUI.gameObject.activeSelf)
		{
			FormatActionElement(_view.ClimbUI, "PLAYER_ACTION_HOLD", 4, "PLAYER_ACTION_CLIMB");
		}
	}

	private void FadeCallback(float value)
	{
		_view.ItemActionGroup.Alpha = value;
		Color white = Color.white;
		white.a = value;
		_view.InteractingCursor.Color = white;
		white.a = Mathf.Clamp(white.a, 0.5f, 1f);
		_view.DefaultCursor.Color = white;
	}

	private bool CurrentObjectIsActiveLadder()
	{
		if (_player.Movement.activeLadder.IsNullOrDestroyed())
		{
			return false;
		}
		if (_raycaster.CurrentObject.IsNullOrDestroyed())
		{
			return false;
		}
		return _player.Movement.activeLadder.transform.parent.gameObject.GetInstanceID() == _raycaster.CurrentObject.gameObject.GetInstanceID();
	}
}
