using System.Collections.Generic;
using Beam.Developer.UI;
using Beam.Serialization;
using Funlabs;
using UnityEngine;

namespace Beam;

public class DeveloperMode : MonoBehaviour, INeedPlayerComponent
{
	private class ReplicateToggleGod : MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		public override void OnPeer()
		{
			Player.DeveloperMode.ToggleGodMode();
		}
	}

	private class ReplicateCreate : MultiplayerMessage
	{
		[Replicate]
		public short PrefabId;

		[Replicate]
		public Vector3 Pos;

		public override void OnPeer()
		{
			MultiplayerMng.Instantiate<SaveablePrefab>((uint)PrefabId, Pos, Quaternion.identity, MiniGuid.New());
		}
	}

	public bool startInFlyMode;

	public KeyCode toggleMode = KeyCode.G;

	public KeyCode flyUp = KeyCode.E;

	public KeyCode flyDown = KeyCode.Q;

	[SerializeField]
	private List<GameObject> _items;

	[SerializeField]
	private List<GameObject> _tools;

	[SerializeField]
	private List<GameObject> _consumables;

	private float curSpeed;

	private float minFlySpeed = 20f;

	private float nrmFlySpeed = 35f;

	private float maxFlySpeed = 75f;

	private bool _inFlyMode;

	private IPlayer _player;

	private CharacterController _characterController;

	public bool InFlyMode => _inFlyMode;

	public IPlayer Player => _player;

	private void Awake()
	{
		_characterController = GetComponent<CharacterController>();
		_inFlyMode = startInFlyMode;
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
	}

	public void Create(GameObject prefab, float offset = 0f)
	{
		uint valueOrDefault = (prefab?.GetComponent<ISaveablePrefab>()?.PrefabId).GetValueOrDefault();
		if (valueOrDefault != 0)
		{
			Create(valueOrDefault, offset);
		}
	}

	public void Create(uint prefabId, float offset = 0f)
	{
		Vector3 forward = Camera.main.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		bool flag = Prefabs.TryGetMultiplayerPrefabName(prefabId, out var _);
		Vector3 vector = transform.position + forward + Vector3.up * offset;
		if (flag)
		{
			if (Game.Mode.IsClient())
			{
				ReplicateCreate replicateCreate = new ReplicateCreate();
				replicateCreate.PrefabId = (short)prefabId;
				replicateCreate.Pos = vector;
				replicateCreate.Post();
			}
			else
			{
				MultiplayerMng.Instantiate<SaveablePrefab>(prefabId, vector, Quaternion.identity, MiniGuid.New());
			}
		}
		else
		{
			MultiplayerMng.Instantiate<SaveablePrefab>(prefabId, vector, Quaternion.identity, MiniGuid.New());
			ReplicateCreate replicateCreate2 = new ReplicateCreate();
			replicateCreate2.PrefabId = (short)prefabId;
			replicateCreate2.Pos = vector;
			replicateCreate2.Post();
		}
	}

	private void FixedUpdate()
	{
		if (_inFlyMode && _player.IsOwner && (!DeveloperConsolePresenter.Instance || !DeveloperConsolePresenter.Instance.IsOpen))
		{
			float axis = _player.Input.GetAxis(0);
			float axis2 = _player.Input.GetAxis(1);
			curSpeed = nrmFlySpeed;
			if (_player.Input.GetButton(6))
			{
				curSpeed = maxFlySpeed;
			}
			if (_player.Input.GetButton(5))
			{
				curSpeed = minFlySpeed;
			}
			Vector3 direction = new Vector3(axis, 0f, axis2);
			direction = _player.PlayerCamera.transform.TransformDirection(direction);
			direction *= curSpeed;
			_characterController.Move(direction * Time.deltaTime);
		}
	}

	private void ToggleGodMode()
	{
		if (!_inFlyMode)
		{
			_player.Movement.GodMode = true;
			_player.Movement.falling = false;
			_player.Movement.grounded = false;
			_player.Movement.InWater = false;
			_player.Movement.ActivePlatform = null;
			_player.Movement.activeLadder = null;
			_player.Movement.activeLedge = null;
			_inFlyMode = true;
		}
		else if (_inFlyMode)
		{
			_player.Movement.activeLadder = null;
			_player.Movement.ActivePlatform = null;
			_player.Movement.GodMode = false;
			_inFlyMode = false;
		}
	}

	public bool Teleport(int zoneId)
	{
		if (zoneId < 0 || zoneId > 48)
		{
			return false;
		}
		Zone zone = Singleton<StrandedWorld>.Instance.Zones[zoneId];
		Teleport(zone.transform.position);
		return true;
	}

	public bool Teleport(Vector3 position)
	{
		if (!_inFlyMode)
		{
			ReplicatedToggleGodMode();
		}
		_player.transform.position = position + Vector3.up * 22.5f;
		return true;
	}

	public void ReplicatedToggleGodMode()
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateToggleGod replicateToggleGod = new ReplicateToggleGod();
			replicateToggleGod.Player = _player;
			replicateToggleGod.Post();
		}
		ToggleGodMode();
	}
}
