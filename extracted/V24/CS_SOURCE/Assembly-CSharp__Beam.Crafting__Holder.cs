using System;
using System.Collections;
using System.Collections.Generic;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using Sirenix.OdinInspector;
using UdpKit;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Crafting;

public class Holder : SerializedMonoBehaviour, IHolder, INeedPlayerComponent, IHotkeySource, IGameInputHandler, IGameActionProvider, ISaveable
{
	private class PostReplicatePickup : I_MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[I_Replicate]
		public IPickupable Pickupable;

		[Replicate]
		public bool Animate;

		public override void OnPeer()
		{
			Player.Holder.ReplicatedPickup(Pickupable, Animate);
		}
	}

	private class ReplicatePickup : I_MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[I_Replicate]
		public IPickupable Pickupable;

		[Replicate]
		public bool Animate;

		public override void OnPeer()
		{
			Player.Holder.Pickup(Pickupable, Animate);
		}
	}

	private class ReplicateSelect : MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[Replicate]
		public IPickupable Pickupable;

		public override void OnPeer()
		{
			Player.Holder.Select(Pickupable);
		}
	}

	private class ReplicateRelease : I_MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[I_Replicate]
		public IPickupable Pickupable;

		public override void OnPeer()
		{
			Player.Holder.Release(Pickupable);
		}
	}

	public class ReplicateHolderCommand : MultiplayerMessage
	{
		public enum Command
		{
			SelectHands,
			DropCurrent
		}

		[Replicate]
		public IPlayer Player;

		public Command Cmd;

		public override void OnPeer()
		{
			switch (Cmd)
			{
			case Command.SelectHands:
				Player.Holder.SelectHands();
				break;
			case Command.DropCurrent:
				Player.Holder.DropCurrent();
				break;
			}
		}

		public override void Read(UdpPacket packet)
		{
			base.Read(packet);
			Cmd = (Command)packet.ReadByte();
		}

		public override void Write(UdpPacket packet)
		{
			base.Write(packet);
			packet.WriteByte((byte)Cmd);
		}

		public override string ToString()
		{
			return base.ToString() + $"({Cmd})";
		}
	}

	private class ReplicateSwitchCurrent : MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[Replicate]
		public ISaveablePrefab RaycasterCurrentObject;

		public ReplicateSwitchCurrent()
		{
		}

		public ReplicateSwitchCurrent(IPlayer player)
		{
			Player = player;
			RaycasterCurrentObject = player.User.Controller.Raycaster.CurrentObject as ISaveablePrefab;
		}

		public override void OnPeer()
		{
			Player.User.Controller.Raycaster.SetCurrentObject(RaycasterCurrentObject as IBase);
			Player.Holder.SwitchCurrent();
		}
	}

	[SerializeField]
	private Transform _holdingContainer;

	[SerializeField]
	private ISlotStorageProvider<IPickupable> _storageProvider;

	private ISlotStorage<IPickupable> _storage;

	private bool _loaded;

	private IPickupable _currentObject;

	private IPickupable _currentlyPicking;

	[SerializeField]
	private EventItemProxies _proxies;

	private IPlayer _player;

	private IDynamicParentProvider _dynamicParenter;

	private IGameInputController _controller;

	private bool _poll = true;

	private List<IGameAction> _gameActions = new List<IGameAction>();

	private GameAction _gameAction = new GameAction();

	private int _objectResourceCount;

	private bool _objectiveComplete;

	[SerializeField]
	private UnityEvent _onGatheredResources;

	public ISlotStorage<IPickupable> Storage => _storage;

	public bool Loaded => _loaded;

	public IPickupable CurrentObject => _currentObject;

	public EventItemProxies Proxies => _proxies;

	IPlayer INeedPlayerComponent.Player => _player;

	public UnityEvent OnGatheredResources => _onGatheredResources;

	public event Action<IPickupable> SelectedItemChanged;

	public event Action<IPickupable> PickedUp;

	public event Action<IPickupable> Dropped;

	protected virtual void OnSelectedItemChanged(IPickupable pickupable)
	{
		SelectedItemChanged?.Invoke(pickupable);
	}

	protected virtual void OnPickedUp(IPickupable pickupable)
	{
		PickedUp?.Invoke(pickupable);
	}

	protected virtual void OnDropped(IPickupable pickupable)
	{
		Dropped?.Invoke(pickupable);
	}

	private void Awake()
	{
		_storage = _storageProvider.GetSlotStorage();
		_storage.SelectionChanged += Storage_SelectionChanged;
		_storage.Pushed += Storage_Pushed;
	}

	private void Start()
	{
		_dynamicParenter = UnityEngine.Object.FindObjectOfType<StrandedWorld>();
	}

	public void Initialize(IGameInputController controller)
	{
		_controller = controller;
		_controller.Interacter.InteractingFellThrough += Interacter_InteractingFellThrough;
		_controller.Raycaster.SetFilter(new HoldingRaycastFilter(_holdingContainer));
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
		_player.GameInput.Subscribe(this);
		_player.Movement.BeginClimbing += PlayerMovement_BeginClimbing;
		_player.Movement.EndClimbing += PlayerMovement_EndClimbing;
		_gameAction.PlayerActionRequiredDescription = "PLAYER_ACTION_PRESS";
		_gameAction.ElementRequiredId = 9;
		_gameAction.ResultingActionDescription = "PLAYER_ACTION_PICK_UP";
		_gameAction.Action = Pickup_GameAction;
		_gameActions.Add(_gameAction);
	}

	public void SetEnabled(bool enabled)
	{
		_poll = enabled;
	}

	private void OnDestroy()
	{
		PickedUp = null;
		Dropped = null;
		SelectedItemChanged = null;
		if (_player.IsValid())
		{
			if (_player.GameInput != null)
			{
				_player.GameInput.Unsubscribe(this);
			}
			if (_player.Movement != null)
			{
				_player.Movement.BeginClimbing -= PlayerMovement_BeginClimbing;
				_player.Movement.EndClimbing -= PlayerMovement_EndClimbing;
			}
		}
		if (_storage.IsValid())
		{
			_storage.SelectionChanged -= Storage_SelectionChanged;
			_storage.Pushed -= Storage_Pushed;
		}
		if (_controller.IsValid())
		{
			_controller.Interacter.InteractingFellThrough -= Interacter_InteractingFellThrough;
		}
	}

	private void PlayerMovement_BeginClimbing()
	{
		HideCurrent();
	}

	private void PlayerMovement_EndClimbing()
	{
		ShowCurrent();
	}

	public void CheckInput()
	{
		if (_poll && _player.IsOwner)
		{
			if (_player.Input.GetButtonDown(29))
			{
				ReplicatedSwitchCurrent();
			}
			else if (_player.Input.GetButtonDown(10))
			{
				ReplicatedDropCurrent();
			}
			else if (_player.Input.GetButtonDown(17))
			{
				ReplicatedSelectHands();
			}
		}
	}

	IEnumerable<IGameAction> IGameActionProvider.GetGameActions()
	{
		IPickupable pickupable = _controller.Raycaster.CurrentObject as IPickupable;
		if (IsValidPickupable(pickupable))
		{
			return _gameActions;
		}
		return null;
	}

	private void Interacter_InteractingFellThrough()
	{
		if (_poll)
		{
			IPickupable pickupable = _controller.Raycaster.CurrentObject as IPickupable;
			if (IsValidPickupable(pickupable))
			{
				PostReplicatedPickup(pickupable);
			}
		}
	}

	private void Pickup_GameAction(IBase obj)
	{
		IPickupable pickupable = obj as IPickupable;
		if (IsValidPickupable(pickupable))
		{
			Pickup(pickupable);
		}
	}

	public bool IsPickingUp(IPickupable pickupable)
	{
		if (_currentlyPicking != pickupable)
		{
			return CurrentObject == pickupable;
		}
		return true;
	}

	public bool CanPickup(IPickupable pickupable, bool notification = true)
	{
		bool flag = true;
		IDisintegratable disintegratable = pickupable as IDisintegratable;
		foreach (IPlayer allPlayer in PlayerRegistry.AllPlayers)
		{
			if (allPlayer.Dragger.IsDragging && allPlayer.Dragger.Draggable == pickupable)
			{
				flag = false;
			}
			if (allPlayer.Holder._currentlyPicking == pickupable)
			{
				flag = false;
			}
			if (allPlayer.Holder.CurrentObject == pickupable)
			{
				flag = false;
			}
			if (allPlayer.Crafter.CurrentCraftable != pickupable && allPlayer.Crafter.CraftingWithMaterial(pickupable))
			{
				flag = false;
			}
			if (allPlayer.Skinner.CurrentSkinnable == pickupable)
			{
				flag = false;
			}
		}
		if (disintegratable != null && disintegratable.Disintegrating)
		{
			flag = false;
		}
		if (pickupable.IsNullOrDestroyed())
		{
			flag = false;
		}
		else if (pickupable.IsPickedUp && pickupable.OwnerId != _player.Id)
		{
			flag = false;
		}
		if (flag)
		{
			flag = _currentObject.IsNullOrDestroyed() || _storage.CanPush(pickupable, notification);
		}
		else
		{
			_storage.CanPush(pickupable, notification);
		}
		return flag;
	}

	public bool Pickup(IPickupable pickupable, bool animate = true)
	{
		if (CanPickup(pickupable))
		{
			StartCoroutine(Pickup_Sequence(pickupable, animate));
			return true;
		}
		return false;
	}

	private IEnumerator Pickup_Sequence(IPickupable pickupable, bool animate)
	{
		_controller.CurrentHandler = this;
		_currentlyPicking = pickupable;
		if (animate)
		{
			_player.Character.Animater.PlayPickupAnimation();
			yield return new WaitForSeconds(0.3f);
		}
		if (!pickupable.IsNullOrDestroyed() && pickupable.gameObject.activeInHierarchy)
		{
			PickupCallback(pickupable);
			yield return Reload(pickupable);
		}
		_controller.CurrentHandler = null;
		_currentlyPicking = null;
	}

	private void PickupCallback(IPickupable pickupable)
	{
		if (_currentObject.IsNullOrDestroyed())
		{
			Select(pickupable);
		}
		else
		{
			_storage.Push(pickupable);
		}
		OnPickedUp(pickupable);
		CheckTutorialObjective();
	}

	public void DropCurrent()
	{
		DropAndPop(ref _currentObject);
	}

	private void DropAndPop(ref IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed() && !_player.Statistics.IsKnockedOut)
		{
			IPickupable obj = pickupable;
			Drop(ref pickupable);
			_storage.PopNext(obj);
			_controller.Raycaster.Reset();
		}
	}

	private void Drop(ref IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed())
		{
			if (!pickupable.gameObject.activeInHierarchy)
			{
				pickupable.gameObject.SetActive(value: true);
			}
			pickupable.Release();
			pickupable.transform.parent = null;
			pickupable.TransformParent = _dynamicParenter?.GetParent();
			pickupable.Parent();
			pickupable.Hold(holding: false);
			pickupable.SetOwner(null);
			_player.Character.Hold(null);
			OnDropped(pickupable);
			pickupable = null;
		}
	}

	private void SwitchCurrent()
	{
		if (!_currentObject.IsNullOrDestroyed())
		{
			IPickupable pickupable = _controller.Raycaster.CurrentObject as IPickupable;
			if (IsValidPickupable(pickupable))
			{
				Drop(ref _currentObject);
				Pickup(pickupable, animate: false);
			}
		}
	}

	private bool IsValidPickupable(IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed() && pickupable.CanPickUp)
		{
			return _currentObject != pickupable;
		}
		return false;
	}

	public void HideCurrent()
	{
		HidePickupable(_currentObject);
	}

	public void ShowCurrent()
	{
		ShowPickupable(_currentObject);
	}

	private void HidePickupable(IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed() && pickupable.gameObject.activeSelf)
		{
			pickupable.gameObject.SetActive(value: false);
		}
	}

	private void ShowPickupable(IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed() && !pickupable.gameObject.activeSelf)
		{
			pickupable.gameObject.SetActive(value: true);
			pickupable.SetLOD(0);
		}
	}

	public bool CheckCurrent()
	{
		if (_currentObject == null)
		{
			return _proxies.CheckProxiesActive();
		}
		return true;
	}

	public void ReleaseAndDestroyCurrent()
	{
		if (!_currentObject.IsNullOrDestroyed())
		{
			ReleaseAndDestroy(_currentObject);
		}
	}

	private void Release(IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed())
		{
			if (_currentObject == pickupable)
			{
				DropCurrent();
			}
			else
			{
				_storage.Pop(pickupable);
			}
		}
	}

	public void ReleaseAndDestroy(IPickupable pickupable)
	{
		if (!pickupable.IsNullOrDestroyed())
		{
			Release(pickupable);
			pickupable.transform.CheckForProjectilesInChildren();
			MultiplayerMng.Destroy((SaveablePrefab)pickupable);
		}
	}

	public bool Select(IPickupable pickupable)
	{
		if (pickupable == null)
		{
			return false;
		}
		if (pickupable == _currentObject)
		{
			return true;
		}
		_storage.Pop(pickupable);
		if (!_currentObject.IsNullOrDestroyed() && !_storage.Push(_currentObject))
		{
			_storage.Push(pickupable);
			_storage.CanPush(_currentObject);
			return false;
		}
		if (_player.Movement.ClimbingLadder)
		{
			ShowCurrent();
			HidePickupable(pickupable);
		}
		else
		{
			ShowPickupable(pickupable);
		}
		_currentObject = pickupable;
		_currentObject.SetOwner(_player);
		_currentObject.Store();
		_currentObject.transform.CheckForProjectilesInChildren();
		_currentObject.transform.parent = null;
		_currentObject.TransformParent = _holdingContainer;
		_currentObject.Parent();
		_currentObject.transform.localRotation = Quaternion.identity;
		_currentObject.transform.localPosition = Vector3.zero;
		_currentObject.transform.localScale = Vector3.one;
		_currentObject.Hold(holding: true);
		_player.Character.Hold(_currentObject);
		OnSelectedItemChanged(_currentObject);
		Reload(null, manual: true);
		_controller.Raycaster.Reset();
		return true;
	}

	private void SelectHands()
	{
		if (!_currentObject.IsNullOrDestroyed() && _storage.Push(_currentObject))
		{
			EmptyHands();
		}
	}

	private void EmptyHands()
	{
		_player.Character.Hold(null);
		_controller.Raycaster.Reset();
		_currentObject = null;
		OnSelectedItemChanged(null);
	}

	private void CheckTutorialObjective()
	{
		if (!_objectiveComplete)
		{
			_objectResourceCount++;
			if (_objectResourceCount > 4)
			{
				OnGatheredResources?.Invoke();
				_objectiveComplete = true;
			}
		}
	}

	public void Storage_SelectionChanged(IPickupable pickupable)
	{
		if (pickupable.IsNullOrDestroyed())
		{
			SelectHands();
		}
		else
		{
			Select(pickupable);
		}
	}

	private void Storage_Pushed(IPickupable pickupable, bool success)
	{
	}

	private Coroutine Reload(IPickupable pickupable, bool manual = false)
	{
		IReloadable reloadable = _currentObject as IReloadable;
		if (!reloadable.IsNullOrDestroyed() && !reloadable.Reloaded && (manual || reloadable.CanReloadWith(pickupable)))
		{
			return StartCoroutine(Reload_Sequence(reloadable, pickupable));
		}
		return null;
	}

	private IEnumerator Reload_Sequence(IReloadable reloadable, IPickupable pickupable)
	{
		_controller.CurrentHandler = this;
		reloadable.Reload(pickupable);
		if (_player.Character.Animater.CurrentAnimationType != AnimationType.SPEAR_SECONDARY)
		{
			yield return _player.Character.Animater.Play(reloadable.ReloadAnimation);
		}
		_controller.CurrentHandler = null;
	}

	public bool ReplicatedPickup(IPickupable pickupable, bool animate = true)
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicatePickup replicatePickup = new ReplicatePickup();
			replicatePickup.Player = _player;
			replicatePickup.Pickupable = pickupable;
			replicatePickup.Animate = animate;
			replicatePickup.Post();
		}
		return Pickup(pickupable, animate);
	}

	private void PostReplicatedPickup(IPickupable pickupable, bool animate = true)
	{
		if (_player.Id == 1 && Game.Mode.IsMultiplayer())
		{
			PostReplicatePickup postReplicatePickup = new PostReplicatePickup();
			postReplicatePickup.Player = _player;
			postReplicatePickup.Pickupable = pickupable;
			postReplicatePickup.Animate = animate;
			postReplicatePickup.Post();
		}
		else
		{
			ReplicatedPickup(pickupable, animate);
		}
	}

	public bool ReplicatedSelect(IPickupable pickupable)
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateSelect replicateSelect = new ReplicateSelect();
			replicateSelect.Player = _player;
			replicateSelect.Pickupable = pickupable;
			replicateSelect.Post();
		}
		return Select(pickupable);
	}

	private void ReplicatedSelectHands()
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateHolderCommand replicateHolderCommand = new ReplicateHolderCommand();
			replicateHolderCommand.Player = _player;
			replicateHolderCommand.Cmd = ReplicateHolderCommand.Command.SelectHands;
			replicateHolderCommand.Post();
		}
		SelectHands();
	}

	private void ReplicatedSwitchCurrent()
	{
		if (Game.Mode.IsMultiplayer())
		{
			new ReplicateSwitchCurrent(_player).Post();
		}
		SwitchCurrent();
	}

	public void ReplicatedRelease(IPickupable pickupable)
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateRelease replicateRelease = new ReplicateRelease();
			replicateRelease.Player = _player;
			replicateRelease.Pickupable = pickupable;
			replicateRelease.Post();
		}
		Release(pickupable);
	}

	public void ReplicatedDropCurrent()
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateHolderCommand replicateHolderCommand = new ReplicateHolderCommand();
			replicateHolderCommand.Player = _player;
			replicateHolderCommand.Cmd = ReplicateHolderCommand.Command.DropCurrent;
			replicateHolderCommand.Post();
		}
		DropCurrent();
	}

	public JObject Save()
	{
		JObject jObject = new JObject();
		if (!_currentObject.IsNullOrDestroyed())
		{
			JObject jObject2 = SavingUtilities.SavePrefabToJObject(_currentObject);
			if (jObject2 != null)
			{
				jObject.AddField("CurrentObject", jObject2);
			}
			else
			{
				Debug.LogError("Holder::Save:: Error saving current object.\n\nName: " + _currentObject.gameObject.name + "\nReference Id: " + _currentObject.ReferenceId.ToString());
			}
		}
		else
		{
			jObject.AddField("CurrentObject", "Empty");
		}
		return jObject;
	}

	public void Load(JObject data)
	{
		JObject field = data.GetField("CurrentObject");
		if (field.IsValid() && !field.IsNull() && field.Type != JObject.EToken.STRING)
		{
			SavingUtilities.LoadPrefabFromJData(MultiplayerZoneSync.Usage.Holder, field, (SaveablePrefab instance) =>
			{
				instance?.transform?.SetParent(null);
			}, (SaveablePrefab instance) =>
			{
				Select((IPickupable)instance.GetComponent<BaseObject>());
				_loaded = true;
			});
		}
		else
		{
			_loaded = true;
		}
	}
}
