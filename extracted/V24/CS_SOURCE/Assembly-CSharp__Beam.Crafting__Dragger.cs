using System.Collections;
using System.Collections.Generic;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam.Crafting;

public class Dragger : MonoBehaviour, IGameInputHandler, INeedPlayerComponent, IDragger
{
	private class ReplicateStartDrag : MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[Replicate]
		public ISaveablePrefab Draggable;

		[Replicate]
		public float Distance;

		[Replicate]
		public Vector3 JointPosition;

		public override void OnPeer()
		{
			Player.Dragger.StartDraging(Draggable, Distance, JointPosition);
		}
	}

	private class ReplicateStopDrag : MultiplayerMessage
	{
		[Replicate]
		public IPlayer Player;

		[Replicate]
		public bool ThrowObject;

		public override void OnPeer()
		{
			Dragger dragger = Player.Dragger;
			dragger._throwObject = ThrowObject;
			dragger._isDragging = false;
		}
	}

	private const float DRAGGING_OFFSET_Y = 0.2f;

	private const float WAIT_TO_ENABLE_AFTER_THROW = 1f;

	[SerializeField]
	private LayerMask _draggingLayerMask;

	private IPlayer _player;

	private IDraggable _draggable;

	private bool _localCanDrag = true;

	private IGameInputController _controller;

	private bool _isDragging;

	private bool _throwObject;

	IPlayer INeedPlayerComponent.Player => _player;

	public IDraggable Draggable
	{
		get
		{
			if (!IsPickedUp(_draggable) && !IsOwned(_draggable))
			{
				return _draggable;
			}
			return null;
		}
	}

	public bool CanDrag { get; set; } = true;

	public bool IsDragging => _isDragging;

	public void Initialize(IGameInputController controller)
	{
		_controller = controller;
		_localCanDrag = true;
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
	}

	public void CheckInput()
	{
		if (_isDragging || _controller.Raycaster.CurrentObject == null || !_player.IsOwner || _player.Input.GetButton(7) || !_player.Input.GetButtonDown(8) || !CanDrag || !_localCanDrag || !(_controller.Raycaster.CurrentObject is IDraggable { CanDrag: not false, IsDragging: false } draggable))
		{
			return;
		}
		_draggable = draggable;
		foreach (IPlayer allPlayer in PlayerRegistry.AllPlayers)
		{
			if (allPlayer != _player && allPlayer.Dragger._draggable != null && allPlayer.Dragger._draggable == draggable)
			{
				return;
			}
		}
		ReplicatedStartDrag(_draggable as ISaveablePrefab, _controller.Raycaster.LastHitInfo.distance, _controller.Raycaster.LastHitInfo.point);
	}

	private bool IsOwned(IDraggable draggable)
	{
		return ((draggable as InteractiveObject)?.OwnerId ?? (-1)) != -1;
	}

	private bool IsPickedUp(IDraggable draggable)
	{
		return (draggable as InteractiveObject)?.IsPickedUp ?? false;
	}

	private IEnumerator Drag(ISaveablePrefab draggable, float distance, Vector3 jointPosition)
	{
		_draggable = draggable as IDraggable;
		_controller.CurrentHandler = this;
		if (_player.IsOwner)
		{
			_player.Input.SetAndCacheMapsEnabled(state: false, 0);
			_player.Input.SetAndCacheMapsEnabled(state: true, 14);
		}
		_isDragging = true;
		_throwObject = false;
		bool isKinematic = _draggable.rigidbody.isKinematic;
		RigidbodyInterpolation interpolation = _draggable.rigidbody.interpolation;
		CollisionDetectionMode collisionDetection = _draggable.rigidbody.collisionDetectionMode;
		SaveablePrefab saveablePrefab = _draggable as SaveablePrefab;
		if (Game.Mode.IsMultiplayer() && saveablePrefab != null)
		{
			yield return StartCoroutine(SetLocalDrag(saveablePrefab, enabled: true));
		}
		_player.PlayerCamera.DraggingJoint.Joint.connectedBody = ((IDraggable)draggable).rigidbody;
		_player.PlayerCamera.DraggingJoint.Joint.transform.position = jointPosition;
		_player.PlayerCamera.DraggingJoint.Joint.anchor = Vector3.zero;
		_draggable.IsDragging = true;
		_draggable.BeginDrag();
		_draggable.rigidbody.drag = 8f;
		_draggable.rigidbody.angularDrag = 8f;
		_draggable.rigidbody.isKinematic = false;
		_draggable.rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
		_draggable.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		float currentMass = _draggable.rigidbody.mass;
		while (!_draggable.IsNullOrDestroyed() && _isDragging && !IsOwned(_draggable) && !IsPickedUp(_draggable))
		{
			if (_draggable.IsNullOrDestroyed())
			{
				yield return null;
			}
			_draggable.rigidbody.mass = Mathf.Lerp(_draggable.rigidbody.mass, _player.Movement.ActivePlatform.IsNullOrDestroyed() ? currentMass : 0.01f, Time.deltaTime * 3f);
			BeamRay ray = _player.PlayerCamera.GetRay(distance);
			if (Physics.Raycast(ray.BaseRay, out var hitInfo, ray.Length, _draggingLayerMask))
			{
				_player.PlayerCamera.DraggingJoint.Rigidbody.MovePosition(hitInfo.point + new Vector3(0f, 0.2f, 0f));
			}
			else
			{
				_player.PlayerCamera.DraggingJoint.Rigidbody.MovePosition(ray.EndPoint + new Vector3(0f, 0.2f, 0f));
			}
			if (_player.IsOwner)
			{
				if (!_player.Input.GetButton(8) || !_draggable.CanDrag || !CanDrag || !_localCanDrag)
				{
					ReplicatedStopDrag(throwObject: false);
				}
				if (_player.Input.GetButtonDown(7))
				{
					ReplicatedStopDrag(throwObject: true);
					if (Game.Mode.IsMultiplayer())
					{
						_localCanDrag = false;
						StartCoroutine(WaitToEnableDrag(1f));
					}
				}
			}
			yield return null;
		}
		if (!_draggable.IsNullOrDestroyed())
		{
			_draggable.rigidbody.WakeUp();
			_draggable.IsDragging = false;
			_draggable.EndDrag();
			_draggable.rigidbody.drag = _draggable.CurrentDrag;
			_draggable.rigidbody.angularDrag = _draggable.CurrentAngularDrag;
			_draggable.rigidbody.isKinematic = isKinematic;
			_draggable.rigidbody.interpolation = interpolation;
			_draggable.rigidbody.collisionDetectionMode = collisionDetection;
			_draggable.rigidbody.mass = currentMass;
			_player.PlayerCamera.DraggingJoint.Joint.connectedBody = null;
			if (Game.Mode.IsMultiplayer() && saveablePrefab != null)
			{
				yield return StartCoroutine(SetLocalDrag(saveablePrefab, enabled: false));
			}
			if (_throwObject)
			{
				_draggable.rigidbody.AddForce(_player.PlayerCamera.Camera.transform.forward * 6f, ForceMode.VelocityChange);
			}
			else
			{
				_draggable.rigidbody.AddForce(_player.PlayerCamera.Camera.transform.up * -0.1f, ForceMode.Impulse);
			}
		}
		_isDragging = false;
		_draggable = null;
		_controller.CurrentHandler = null;
		if (_player.IsOwner)
		{
			_player.Input.SetAndCacheMapsEnabled(state: true, 0);
			_player.Input.SetAndCacheMapsEnabled(state: false, 14);
		}
	}

	private IEnumerator SetLocalDrag(SaveablePrefab saveablePrefab, bool enabled)
	{
		TransformId parentId = (enabled ? TransformId.Unknown : saveablePrefab.gameObject.transform.parent.GetTransfromId());
		InteractiveObject_PALM_LOG interactiveObject_PALM_LOG = saveablePrefab as InteractiveObject_PALM_LOG;
		if (!interactiveObject_PALM_LOG.IsNullOrDestroyed())
		{
			HashSet<InteractiveObject_PALM_LOG> island = new HashSet<InteractiveObject_PALM_LOG>();
			GatherPalmLogsIsland(island, interactiveObject_PALM_LOG);
			if (enabled && Game.Mode.IsClient())
			{
				yield return StartCoroutine(RecreatePhysicsSetupForPalmLogsIsland(island));
			}
			{
				foreach (InteractiveObject_PALM_LOG item in island)
				{
					item.ParentId = parentId;
					item.SetKinematic(!enabled && Game.Mode.IsClient());
				}
				yield break;
			}
		}
		saveablePrefab.ParentId = parentId;
		InteractiveObject interactiveObject = saveablePrefab as InteractiveObject;
		if (!interactiveObject.IsNullOrDestroyed())
		{
			interactiveObject.SetKinematic(!enabled && Game.Mode.IsClient());
		}
	}

	private void GatherPalmLogsIsland(HashSet<InteractiveObject_PALM_LOG> island, InteractiveObject_PALM_LOG log)
	{
		if (!log.IsNullOrDestroyed() && !island.Contains(log))
		{
			island.Add(log);
			GatherPalmLogsIsland(island, log.ConnectedAboveLog);
			GatherPalmLogsIsland(island, log.ConnectedBelowLog);
		}
	}

	private IEnumerator RecreatePhysicsSetupForPalmLogsIsland(HashSet<InteractiveObject_PALM_LOG> island)
	{
		foreach (InteractiveObject_PALM_LOG item in island)
		{
			if (!item.FixedJoint.IsNullOrDestroyed())
			{
				item.FixedJoint.connectedBody = null;
				BeamTeam.Destroy(item.FixedJoint);
				item.FixedJoint = null;
			}
		}
		yield return null;
		foreach (InteractiveObject_PALM_LOG item2 in island)
		{
			item2.RemoveRigidbody(IsOwner: true);
		}
		yield return null;
		foreach (InteractiveObject_PALM_LOG item3 in island)
		{
			item3.CheckRigidbody(IsOwner: true);
		}
		yield return null;
		foreach (InteractiveObject_PALM_LOG item4 in island)
		{
			if (!item4.ConnectedAboveLog.IsNullOrDestroyed())
			{
				item4.FixedJoint = item4.gameObject.AddComponent<FixedJoint>();
				item4.FixedJoint.connectedBody = item4.ConnectedAboveLog.rigidbody;
			}
		}
		yield return null;
	}

	public void StartDraging(ISaveablePrefab draggable, float distance, Vector3 jointPosition)
	{
		StartCoroutine(Drag(draggable, distance, jointPosition));
	}

	public void ReplicatedStartDrag(ISaveablePrefab draggable, float distance, Vector3 jointPosition)
	{
		if (!draggable.IsNullOrDestroyed())
		{
			StartDraging(draggable, distance, jointPosition);
			if (Game.Mode.IsMultiplayer())
			{
				ReplicateStartDrag replicateStartDrag = new ReplicateStartDrag();
				replicateStartDrag.Player = _player;
				replicateStartDrag.Draggable = draggable;
				replicateStartDrag.Distance = distance;
				replicateStartDrag.JointPosition = jointPosition;
				replicateStartDrag.Post();
			}
		}
	}

	public void ReplicatedStopDrag(bool throwObject)
	{
		_isDragging = false;
		_throwObject = throwObject;
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateStopDrag replicateStopDrag = new ReplicateStopDrag();
			replicateStopDrag.Player = _player;
			replicateStopDrag.ThrowObject = throwObject;
			replicateStopDrag.Post();
		}
	}

	public IEnumerator WaitToEnableDrag(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		_localCanDrag = true;
	}
}
