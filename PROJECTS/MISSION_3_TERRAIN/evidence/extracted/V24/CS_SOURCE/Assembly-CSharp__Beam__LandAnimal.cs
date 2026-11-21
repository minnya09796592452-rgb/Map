using System;
using Beam.Events;
using Beam.Utilities;
using Funlabs;
using UdpKit;
using UnityEngine;

namespace Beam;

public class LandAnimal : StateMachine<LandAnimal>
{
	public class ReplicateState : MultiplayerMessage
	{
		[Replicate]
		public LandAnimal stateMachine;

		public LandAnimalState newState;

		public override void OnPeer()
		{
			if ((bool)stateMachine)
			{
				stateMachine.CurrentState = newState;
			}
		}

		public override void Read(UdpPacket packet)
		{
			base.Read(packet);
			newState = (LandAnimalState)packet.ReadByte();
		}

		public override void Write(UdpPacket packet)
		{
			base.Write(packet);
			packet.WriteByte((byte)newState);
		}
	}

	public class ReplicateAnimation : MultiplayerMessage
	{
		[Replicate]
		public byte AnimationId;

		[Replicate]
		public LandAnimal StateMachine;

		public override void OnPeer()
		{
			if ((bool)StateMachine)
			{
				StateMachine.PlayAnimation(AnimationId);
			}
		}
	}

	[SerializeField]
	private LandAnimalState _startingState;

	public Vector3 StartingPosition;

	protected float breakingMultiplier = 1f;

	public float AvoidanceCheckRate;

	private float avoidanceCheckRateTimer;

	private float _lastGroundNormalCheck;

	[SerializeField]
	protected Animator _animator;

	[SerializeField]
	private LandSpecies _species;

	[SerializeField]
	protected float turnSpeed = 1f;

	protected HidingSpot _hidingspot;

	protected HidingSpotController _hidingSpotController;

	protected Vector3 _waypointPosition;

	protected float _stuckWaypointTime;

	[SerializeField]
	protected float minExploreHeight;

	[SerializeField]
	protected float maxExploreHeight = 500f;

	public LayerMask _groundNormalMask;

	public LayerMask _avoidanceMask;

	[SerializeField]
	protected Collider _movementCollider;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	protected Rigidbody _rigidbody;

	[SerializeField]
	private bool IgnoreCollisionWithPlayer = true;

	protected float _controllerVelocity;

	protected float _currentSpeed;

	protected float _adjustedSpeed;

	private Vector3 _lastPosition;

	private float _velocityStuckTimer;

	protected IPlayer closestPlayer;

	protected float distanceToClosestPlayer;

	[SerializeField]
	private float wideRayDistance = 1.9f;

	private float longRayDistance = 1.5f;

	[SerializeField]
	private float _avoidanceRayYOffset = -0.05f;

	public LandSpecies Species => _species;

	public event Action Attacked;

	public event Action Killed;

	protected virtual void OnAttacked()
	{
		Attacked?.Invoke();
	}

	protected virtual void OnKilled()
	{
		Killed?.Invoke();
	}

	protected void Start()
	{
		CurrentState = _startingState;
		if (StartingPosition == Vector3.zero)
		{
			StartingPosition = transform.position;
		}
		EventManager.AddListener<PlayerDisconnectedEvent>(EventManager_PlayerDisconnected);
	}

	private void OnDestroy()
	{
		Attacked = null;
		Killed = null;
		EventManager.RemoveListener<PlayerDisconnectedEvent>(EventManager_PlayerDisconnected);
	}

	private void EventManager_PlayerDisconnected(PlayerDisconnectedEvent disconnectedEvent)
	{
		if (!closestPlayer.IsNullOrDestroyed() && closestPlayer.Id == disconnectedEvent.PlayerId)
		{
			closestPlayer = null;
			distanceToClosestPlayer = 500f;
		}
	}

	protected void Setup()
	{
		PlayerRegistry.AllPlayers.ForEach_NonAlloc(IgnorePlayerCollider);
		_hidingSpotController = Singleton<HidingSpotController>.Instance;
		longRayDistance = wideRayDistance - 0.4f;
		if ((bool)_animator)
		{
			_animator.applyRootMotion = false;
		}
	}

	private void IgnorePlayerCollider(IPlayer player)
	{
		if (_movementCollider != null && IgnoreCollisionWithPlayer)
		{
			Physics.IgnoreCollision(_movementCollider, player.Movement.CharacterController);
		}
	}

	public void Kill()
	{
		CurrentState = LandAnimalState.Dying;
		OnKilled();
	}

	protected bool CheckWaypointDist(float dist = 0.75f)
	{
		return (new Vector2(_waypointPosition.x, _waypointPosition.z) - new Vector2(transform.position.x, transform.position.z)).magnitude < dist;
	}

	protected void MatchGroundNormal()
	{
		Quaternion quaternion = Quaternion.identity;
		_lastGroundNormalCheck += FixedTime.TimeStep(this);
		if (_lastGroundNormalCheck > AvoidanceCheckRate)
		{
			if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 10f, _groundNormalMask, QueryTriggerInteraction.Ignore) && hitInfo.normal != Vector3.zero)
			{
				quaternion = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
			}
			_lastGroundNormalCheck = 0f;
		}
		Vector3 vector = new Vector3(_waypointPosition.x, transform.position.y, _waypointPosition.z) - transform.position;
		if (vector != Vector3.zero)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, quaternion * Quaternion.LookRotation(vector), turnSpeed * FixedTime.TimeStep(this));
		}
	}

	protected bool FindHidingSpot(float dist = 40f)
	{
		_hidingspot = _hidingSpotController.FindHidingSpot(_species, _rigidbody.position, dist, ValidateLandingSpot);
		if (_hidingspot != null)
		{
			_hidingspot.HideChild = this;
			_hidingspot.Occupied = true;
			return true;
		}
		return false;
	}

	protected float CalculateTrueVelocity()
	{
		Vector3 vector = (transform.position - _lastPosition) / FixedTime.TimeStep(this);
		_lastPosition = transform.position;
		return vector.magnitude;
	}

	protected void NewAvoidWaypoint(float dist = 4f, float zigzag = 0f)
	{
		_stuckWaypointTime = 0f;
		IPlayer player = PlayerUtilities.FindClosestPlayer(transform.position);
		Vector3 vector = transform.position - player.transform.position;
		Vector3 vector2 = Vector3.Lerp(transform.forward, vector.normalized, 0.8f);
		_waypointPosition = transform.position + vector2.normalized * dist + transform.right * UnityEngine.Random.Range(0f - zigzag, zigzag);
	}

	protected void NewExploreWaypoint(float dist)
	{
		_stuckWaypointTime = 0f;
		float num = ((UnityEngine.Random.value < 0.5f) ? 1 : (-1));
		Vector3 vector = Vector3.Lerp(transform.forward, UnityEngine.Random.insideUnitSphere * num, 0.4f);
		_waypointPosition = transform.position + vector * dist;
	}

	protected void NewHideWaypoint()
	{
		if ((bool)_hidingspot)
		{
			_stuckWaypointTime = 0f;
			_waypointPosition = _hidingspot.transform.position;
		}
	}

	protected void NewChaseWaypoint(Vector3 targetPosition)
	{
		_waypointPosition = targetPosition;
		_stuckWaypointTime = 0f;
	}

	protected void NewChaseWaypoint(Vector3 targetPosition, float waypointDistance, float closeness = 1f)
	{
		targetPosition.y = transform.position.y;
		Vector3 vector = targetPosition - transform.position;
		float num = Mathf.Min(Mathf.Clamp(vector.magnitude - closeness, 0.01f, float.PositiveInfinity), waypointDistance);
		_waypointPosition = transform.position + vector.normalized * num;
		_stuckWaypointTime = 0f;
	}

	protected bool ValidateLandingSpot(HidingSpot hidingSpot)
	{
		return PlayerUtilities.FindClosestPlayer(transform.position, 20f).IsNullOrDestroyed();
	}

	protected void CheckBounds()
	{
		if (transform.position.y < minExploreHeight || transform.position.y > maxExploreHeight)
		{
			_waypointPosition = StartingPosition;
		}
	}

	protected void CheckIfStuck()
	{
		_stuckWaypointTime += FixedTime.TimeStep(this);
		if (_controllerVelocity < 0.03f)
		{
			_velocityStuckTimer += FixedTime.TimeStep(this);
		}
		else
		{
			_velocityStuckTimer = 0f;
		}
		if (_velocityStuckTimer > 3f)
		{
			_velocityStuckTimer = 0f;
			transform.Rotate(0f, 180f, 0f);
			NewExploreWaypoint(5f);
		}
		if (_stuckWaypointTime > 15f)
		{
			NewExploreWaypoint(5f);
		}
	}

	protected void SEARCH_FOR_PLAYER()
	{
		closestPlayer = PlayerUtilities.FindClosestPlayer(transform.position);
		distanceToClosestPlayer = float.PositiveInfinity;
		if (!closestPlayer.IsNullOrDestroyed())
		{
			distanceToClosestPlayer = (transform.position - closestPlayer.transform.position).magnitude;
		}
	}

	protected bool CAN_SEE_PLAYER(IPlayer player)
	{
		float num = 100f;
		Vector3 vector = transform.position + transform.up * _avoidanceRayYOffset;
		if (Vector3.Angle(player.transform.position - vector, transform.forward) <= num && !Physics.Linecast(vector, player.transform.position, out var _, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			return true;
		}
		return false;
	}

	protected void SteerAvoidance()
	{
		if (avoidanceCheckRateTimer != 0f && avoidanceCheckRateTimer < AvoidanceCheckRate)
		{
			avoidanceCheckRateTimer += FixedTime.TimeStep(this);
			return;
		}
		avoidanceCheckRateTimer = 0.01f;
		Vector3 forward = transform.forward;
		Vector3 vector = transform.position + transform.up * _avoidanceRayYOffset;
		Vector3 vector2 = Quaternion.AngleAxis(0f, transform.up) * forward;
		Vector3 zero = Vector3.zero;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		float num = 0f;
		float num2 = 0f;
		float num3 = 25f;
		float num4 = 0.25f;
		vector2 = Quaternion.AngleAxis(num3, transform.up) * forward;
		zero = vector + vector2 * wideRayDistance;
		if (Physics.Raycast(vector, vector2, out var hitInfo, wideRayDistance, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			num = Mathf.Lerp(0f - num4, 0f, hitInfo.distance / wideRayDistance);
			flag = true;
		}
		if ((zero.y < minExploreHeight && transform.position.y > minExploreHeight) || (zero.y > maxExploreHeight && transform.position.y < maxExploreHeight))
		{
			num = 0f - num4;
			flag = true;
		}
		vector2 = Quaternion.AngleAxis(0f - num3, transform.up) * forward;
		zero = vector + vector2 * wideRayDistance;
		if (Physics.Raycast(vector, vector2, out hitInfo, wideRayDistance, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			num2 = Mathf.Lerp(num4, 0f, hitInfo.distance / wideRayDistance);
			flag2 = true;
		}
		if ((zero.y < minExploreHeight && transform.position.y > minExploreHeight) || (zero.y > maxExploreHeight && transform.position.y < maxExploreHeight))
		{
			num2 = num4;
			flag2 = true;
		}
		vector2 = Quaternion.AngleAxis(0f, transform.up) * forward;
		zero = vector + vector2 * longRayDistance;
		if (Physics.Raycast(vector, vector2, out hitInfo, longRayDistance, _avoidanceMask, QueryTriggerInteraction.Ignore) || (zero.y > maxExploreHeight && transform.position.y < maxExploreHeight))
		{
			if (num2 == 0f && num == 0f)
			{
				num = Mathf.Lerp(-1f, 0f, hitInfo.distance / longRayDistance);
			}
			flag3 = true;
		}
		Vector3 origin = vector + transform.forward * wideRayDistance;
		vector2 = -transform.up;
		float maxDistance = 3f;
		bool flag4 = true;
		if (Physics.Raycast(origin, vector2, out hitInfo, maxDistance, _groundNormalMask, QueryTriggerInteraction.Ignore) && hitInfo.point.y > minExploreHeight)
		{
			flag4 = false;
		}
		if (flag | flag2 | flag3 | flag4)
		{
			if (flag & flag2)
			{
				if (transform.InverseTransformPoint(_waypointPosition).x > 0f)
				{
					num = 0f;
					num2 = 1f;
				}
				else
				{
					num = 1f;
					num2 = 0f;
				}
			}
			if (flag4)
			{
				num = 1f;
				num2 = 0f;
			}
			float num5 = num + num2;
			int num6 = 2;
			Vector3 vector3 = forward * num6;
			Vector3 vector4 = transform.right * num6 * num5;
			_waypointPosition = transform.position + vector3 + vector4;
		}
		float value = (Mathf.Abs(num2) + num2) * 100f;
		float b = 1f - Mathf.Clamp(value, 0f, 0.5f);
		breakingMultiplier = Mathf.Lerp(breakingMultiplier, b, FixedTime.TimeStep(this) * (1f + _currentSpeed));
	}

	public void ReplicatedChangeState(LandAnimalState NewState)
	{
		if (Game.Mode.IsServer())
		{
			ReplicateState replicateState = new ReplicateState();
			replicateState.newState = NewState;
			replicateState.stateMachine = this;
			replicateState.Post();
		}
		CurrentState = NewState;
	}

	public void ReplicatedPlayAnimation(byte animationId)
	{
		if (entity.IsOwner)
		{
			ReplicateAnimation replicateAnimation = new ReplicateAnimation();
			replicateAnimation.AnimationId = animationId;
			replicateAnimation.StateMachine = this;
			replicateAnimation.Post();
		}
		PlayAnimation(animationId);
	}
}
