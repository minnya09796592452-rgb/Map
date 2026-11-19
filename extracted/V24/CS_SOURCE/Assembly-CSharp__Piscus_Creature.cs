using System;
using System.Collections;
using Beam;
using Beam.Events;
using Beam.Utilities;
using Ceto;
using Funlabs;
using Photon.Bolt;
using UdpKit;
using UnityEngine;
using UnityEngine.Events;

public class Piscus_Creature : StateMachine<Piscus_Creature>, IPlayerProximityHandler
{
	public enum PiscusBehaviour
	{
		Idle,
		Explore,
		CircleTarget,
		AttackPlayer,
		AttackingPlayer,
		AttackRaft,
		AttackJump,
		Flee,
		Die
	}

	public class ReplicateFinishAttack : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Animal;

		[Replicate]
		public bool CompletedAttack;

		[Replicate]
		public bool Destroyed;

		public override void OnPeer()
		{
			Piscus_Creature animal = Animal;
			if (animal != null)
			{
				animal.ReplicatedFinishAttack(CompletedAttack, Destroyed);
			}
		}
	}

	public class ReplicateAttackBite : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Animal;

		[Replicate]
		public float Force;

		public override void OnPeer()
		{
			Piscus_Creature animal = Animal;
			if (animal != null)
			{
				animal.ReplicatedAttack_Player_Bite(Force);
			}
		}
	}

	public class ReplicateAttackNudge : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Animal;

		[Replicate]
		public IPlayer CurrentTarget;

		public override void OnPeer()
		{
			Animal.CurrentTarget = CurrentTarget;
			Animal.Attack_Nudge();
		}
	}

	public class ReplicateAnimations : MultiplayerMessage
	{
		[Replicate]
		public byte AnimationId;

		[Replicate]
		public Piscus_Creature StateMachine;

		public override void OnPeer()
		{
			StateMachine.PlayAnimation(AnimationId);
		}
	}

	public class ReplicateState : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature StateMachine;

		public PiscusBehaviour NewState;

		[Replicate]
		public IPlayer CurrentTarget;

		[Replicate]
		public bool FinishAttack;

		public override void OnPeer()
		{
			if ((bool)StateMachine)
			{
				StateMachine.ReplicatedChangeState(NewState, CurrentTarget, FinishAttack);
			}
		}

		public override void Read(UdpPacket packet)
		{
			base.Read(packet);
			NewState = (PiscusBehaviour)packet.ReadByte();
		}

		public override void Write(UdpPacket packet)
		{
			base.Write(packet);
			packet.WriteByte((byte)NewState);
		}
	}

	private const byte ANIMATION_BITE = 0;

	private const byte ANIMATION_ATTACKING = 1;

	protected const byte PISCUS_CREATURE_ANIMATION_COUNT = 2;

	[SerializeField]
	private bool _applyDifficulty;

	private bool _debug;

	[SerializeField]
	private PiscusBehaviour _debugCurrentState;

	[SerializeField]
	private string _debugWaypointType;

	private PiscusManager _manager;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private InteractiveObject _interactiveObject;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private Rigidbody _rigidBody;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private Piscus_Animation _animation;

	private IPlayer _currentTarget;

	[Header("AVOIDANCE")]
	[SerializeField]
	private LayerMask _avoidanceMask;

	[SerializeField]
	private int _avoidanceRayDistance = 6;

	[SerializeField]
	private float _avoidanceRayWidth = 1f;

	private float _timeSinceLastAvoidance;

	[Header("MOVEMENT")]
	[SerializeField]
	private float _minSpeed = 3f;

	[SerializeField]
	private float _maxSpeed = 10f;

	private float _lerpedSpeed;

	private float _currentSpeed;

	[SerializeField]
	private float _minExploreDepth = -30f;

	[SerializeField]
	private float _maxExploreDepth;

	[SerializeField]
	private float _waypointArrivalDist = 2f;

	private Vector3 _biomePosition;

	private float _biomeSize;

	private Vector3 _movementPosition;

	private float _distanceToMovePos;

	private const float _waypointDistance = 12f;

	[Header("BEHAVIOUR")]
	[SerializeField]
	private float _circleRadius = 15f;

	[SerializeField]
	private float _maxCircleRadius = 30f;

	[SerializeField]
	private float _randCircleFreq = 0.5f;

	[SerializeField]
	private float _targetOutOfReachTime = -1f;

	[SerializeField]
	private float _circleTargetTime = -1f;

	private float _currentCircleTime;

	[SerializeField]
	private bool _avoidRepellent = true;

	[Header("ATTACKING")]
	[SerializeField]
	private bool _canAttack;

	[SerializeField]
	private bool _canAttackRafts;

	[SerializeField]
	private bool _canAttackJump;

	[SerializeField]
	private bool _canAttackNudge;

	private IPlayer _currentAttackTarget;

	[SerializeField]
	private float _engagePlayerDistance = 10f;

	[SerializeField]
	private float _attackRate = 10f;

	[SerializeField]
	private float _aggression = 1f;

	private float _attackPoints;

	private float _attackDamage;

	[SerializeField]
	private float _normalAttackDamage = 100f;

	private float _attackCritDamage;

	[SerializeField]
	private float _normalAttackCritDamage = 200f;

	[SerializeField]
	private float _attackEventChance = 0.3f;

	[SerializeField]
	private float _attackRaftForce = 10f;

	[SerializeField]
	private Transform _mouthPosition;

	[SerializeField]
	private Transform _mouthPlayerPosition;

	private float _lastAttack = 100f;

	private float _lastNudgeAttackTime = 100f;

	private float _attackingTimer;

	private float _targetBehindBossTimer;

	private float _wayPointStuckTime;

	private float _underTerrainTime;

	private float _targetOutOfReachTimer;

	private float _fleeTimer;

	[Header("EVENTS")]
	public UnityEvent EnteredTargetProximity;

	public UnityEvent LeftTargetProximity;

	public UnityEvent AttackedPlayer;

	private bool _playerFreezed;

	private bool _jumped;

	private float _jumpTimer;

	private float _outOfBoundsTimer;

	private float _rayRightAngle = 20f;

	private float _rayLeftAngle = 20f;

	private float _cornerTrapTimer;

	private bool hit_Left;

	private bool hit_Right;

	private bool hit_Forward;

	private bool hit_ForwardUp;

	private bool selectTurnLeft;

	private bool selectTurnRight;

	private float hit_Left_Weight;

	private float hit_Right_Weight;

	private float newInputSteer;

	Vector3 IPlayerProximityHandler.Position => transform.position;

	float IPlayerProximityHandler.ProximityDistance => _engagePlayerDistance;

	bool IPlayerProximityHandler.InProximity { get; set; }

	public new object CurrentState
	{
		get
		{
			return base.CurrentState;
		}
		set
		{
			if (CurrentState == null || (PiscusBehaviour)CurrentState != PiscusBehaviour.Die)
			{
				base.CurrentState = value;
			}
		}
	}

	public PiscusManager Manager
	{
		get
		{
			return _manager;
		}
		set
		{
			_manager = value;
		}
	}

	public Piscus_Animation Animation => _animation;

	public IPlayer CurrentTarget
	{
		get
		{
			return _currentTarget;
		}
		set
		{
			_currentTarget = value;
		}
	}

	public float AvoidanceRayWidth => _avoidanceRayWidth;

	public float MinSpeed => _maxSpeed;

	public float MaxSpeed => _maxSpeed;

	public float CurrentSpeed
	{
		get
		{
			return _currentSpeed;
		}
		set
		{
			_currentSpeed = value;
		}
	}

	public Vector3 BiomePosition
	{
		get
		{
			return _biomePosition;
		}
		set
		{
			_biomePosition = value;
		}
	}

	public float BiomeSize
	{
		get
		{
			return _biomeSize;
		}
		set
		{
			_biomeSize = value;
		}
	}

	public Vector3 MovementPosition
	{
		get
		{
			return _movementPosition;
		}
		set
		{
			_movementPosition = value;
		}
	}

	public float CircleRadius => _circleRadius;

	public float MaxCircleRadius => _maxCircleRadius;

	public bool CanAttack => _canAttack;

	public bool CanAttackRafts => _canAttackRafts;

	public bool CanAttackJump => _canAttackJump;

	public bool CanAttackNudge => _canAttackNudge;

	public IPlayer CurrentAttackTarget
	{
		get
		{
			return _currentAttackTarget;
		}
		set
		{
			_currentAttackTarget = value;
		}
	}

	public float EngagePlayerDistance
	{
		get
		{
			return _engagePlayerDistance;
		}
		set
		{
			_engagePlayerDistance = value;
		}
	}

	public float LastAttack
	{
		get
		{
			return _lastAttack;
		}
		set
		{
			_lastAttack = value;
		}
	}

	public event Action Awakened;

	public event Action Attacked;

	public event Action AttackedRaft;

	public event Action Jumped;

	public event Action Landed;

	void IPlayerProximityHandler.OnEnteredPlayerProximity(PlayerData playerData)
	{
		EnteredTargetProximity?.Invoke();
	}

	void IPlayerProximityHandler.OnLeftPlayerProximity(PlayerData playerData)
	{
		LeftTargetProximity?.Invoke();
	}

	protected virtual void OnAwakened()
	{
		Awakened?.Invoke();
	}

	protected virtual void OnAttacked()
	{
		Attacked?.Invoke();
	}

	protected virtual void OnAttackedRaft()
	{
		AttackedRaft?.Invoke();
	}

	protected virtual void OnJumped()
	{
		Jumped?.Invoke();
	}

	protected virtual void OnLanded()
	{
		Landed?.Invoke();
	}

	private void Awake()
	{
		_rigidBody.isKinematic = true;
	}

	protected virtual void Start()
	{
		Initialize();
		EventManager.AddListener<PlayerDisconnectedEvent>(EventManager_PlayerDisconnected);
	}

	protected virtual void Initialize()
	{
		_rigidBody.isKinematic = true;
		Beam.Singleton<PiscusInspectionManager>.Instance?.piscuses.Add(this);
		PlayerProximityManager.Instance?.Subcribe(this);
		Setup_Position();
		SetDifficultyParams();
		CurrentState = PiscusBehaviour.Explore;
	}

	public void SetDifficultyParams()
	{
		_attackDamage = _normalAttackDamage;
		_attackCritDamage = _normalAttackCritDamage;
		if (PlayerUtilities.GetServerGameDifficulty() == EGameDifficultyMode.Hard)
		{
			_attackDamage = _normalAttackDamage * 1.2f;
			_attackCritDamage = _normalAttackCritDamage * 1.2f;
		}
		if (_applyDifficulty && PlayerUtilities.GetServerWildlifeMode() == EGameWildlifeMode.Passive)
		{
			_canAttack = false;
			_canAttackJump = false;
			_canAttackRafts = false;
			if (_circleTargetTime == -1f)
			{
				_circleTargetTime = 240f;
			}
		}
	}

	private void OnDestroy()
	{
		EventManager.RemoveListener<PlayerDisconnectedEvent>(EventManager_PlayerDisconnected);
		FinishAttack(completedAttack: false, destroyed: true);
		Beam.Singleton<PiscusInspectionManager>.Instance?.piscuses.Remove(this);
		PlayerProximityManager.Instance?.Unsubscribe(this);
		Awakened = null;
		Attacked = null;
		AttackedRaft = null;
		Jumped = null;
		Landed = null;
		AttackedPlayer.RemoveAllListeners();
		AttackedPlayer = null;
	}

	public void Setup_Position()
	{
		if (_manager != null)
		{
			_biomePosition = new Vector3(_manager.transform.position.x, 0f, _manager.transform.position.z);
			transform.position = _manager.transform.position;
		}
		else
		{
			_biomePosition = new Vector3(transform.position.x, 0f, transform.position.z);
			BiomeSize = 50f;
		}
	}

	protected virtual void UPDATE_MOVEMENT(bool checkAvoidance = true)
	{
		float num = FixedTime.TimeStep(this);
		float num2 = 0f;
		if (!Game.Mode.IsClient())
		{
			if (CheckAttackTimer())
			{
				_lastAttack += num;
			}
			if (checkAvoidance)
			{
				SteerAvoidance();
			}
			_distanceToMovePos = (transform.position - _movementPosition).magnitude;
			CheckWaypointBounds();
			CheckWaypointStuck();
			float num3 = Mathf.Lerp(_currentSpeed, 0.2f, Mathf.Clamp01(_cornerTrapTimer));
			num2 = ((_cornerTrapTimer > 0f) ? num3 : _currentSpeed);
			((IPiscus_CreatureState)state).CurrentSpeed = num2;
			((IPiscus_CreatureState)state).MovementPosition = _movementPosition;
		}
		else
		{
			num2 = ((IPiscus_CreatureState)state).CurrentSpeed;
			_movementPosition = ((IPiscus_CreatureState)state).MovementPosition;
		}
		_lerpedSpeed = Mathf.Lerp(_lerpedSpeed, num2, 2f * num);
		float num4 = 1f + Mathf.Clamp(_lerpedSpeed, _minSpeed, _maxSpeed);
		Quaternion rotation = Quaternion.Lerp(transform.localRotation, Quaternion.LookRotation(_movementPosition - transform.position), num4 * num);
		Vector3 velocity = transform.forward * _lerpedSpeed * num;
		Apply_Movement(rotation, velocity);
		_animation.UpdateAnimation(_movementPosition, _lerpedSpeed, _minSpeed, _maxSpeed);
	}

	protected void Apply_Movement(Quaternion rotation, Vector3 velocity)
	{
		if (!Game.Mode.IsClient())
		{
			_rigidBody.MoveRotation(rotation);
			_rigidBody.MovePosition(transform.position + velocity);
		}
	}

	protected override void ReplicantFixedUpdate()
	{
		if (!LevelLoader.IsLoading())
		{
			if (_currentTarget != null && (PiscusBehaviour)CurrentState == PiscusBehaviour.AttackingPlayer)
			{
				BiteSnapUpdate();
			}
			UPDATE_MOVEMENT();
		}
	}

	private void BiteSnapUpdate()
	{
		_currentTarget.transform.position = _mouthPlayerPosition.position + _mouthPlayerPosition.up * (_currentTarget.Movement.GetCharacterHeight() / 2f);
		_currentTarget.transform.rotation = _mouthPlayerPosition.transform.rotation;
	}

	protected IEnumerator Explore_EnterState()
	{
		_currentSpeed = _minSpeed;
		_currentTarget = null;
		NewExploreWaypoint();
		yield break;
	}

	protected void Explore_FixedUpdate()
	{
		if (!LevelLoader.IsLoading(gameObject))
		{
			_distanceToMovePos = (transform.position - _movementPosition).magnitude;
			if (_distanceToMovePos < _waypointArrivalDist)
			{
				NewExploreWaypoint();
			}
			SEARCH_FOR_PLAYER();
			UPDATE_MOVEMENT();
		}
	}

	protected virtual void SEARCH_FOR_PLAYER()
	{
		IPlayer player = PlayerUtilities.FindClosestPlayer(transform.position, out var _, _engagePlayerDistance);
		if (player.IsNullOrDestroyed())
		{
			ReplicatedChangeState(PiscusBehaviour.Explore, _currentTarget);
		}
		else if ((player.transform.position - _biomePosition).sqrMagnitude < BeamMath.Sqr(BiomeSize))
		{
			_currentTarget = player;
			if (_circleTargetTime != 0f)
			{
				ReplicatedChangeState(CheckTargetRepellent() ? PiscusBehaviour.Flee : PiscusBehaviour.CircleTarget, _currentTarget);
			}
		}
	}

	protected virtual IEnumerator CircleTarget_EnterState()
	{
		if (_currentTarget == null)
		{
			CurrentState = PiscusBehaviour.Explore;
			yield break;
		}
		Vector3 vector = Avoid(new Vector3(_currentTarget.transform.position.x, transform.position.y, _currentTarget.transform.position.z), 100f);
		Vector2 vector2 = ClampPositionToCircle(new Vector2(_currentTarget.transform.position.x, _currentTarget.transform.position.z), _maxCircleRadius, new Vector2(vector.x, vector.z));
		Vector3 vector3 = new Vector3(vector2.x, transform.position.y, vector2.y) - transform.position;
		_movementPosition = transform.position + vector3.normalized * 6f;
		_movementPosition = ClampToBounds(_movementPosition);
		if (_debug)
		{
			_debugWaypointType = "CircleEnter";
		}
		_currentCircleTime = 0f;
		_currentSpeed = _minSpeed;
	}

	protected virtual void CircleTarget_FixedUpdate()
	{
		if (_currentTarget == null)
		{
			ReplicatedChangeState(PiscusBehaviour.Explore, _currentTarget);
			return;
		}
		if (CirclingTimerExpiry())
		{
			_currentSpeed = _minSpeed;
			ReplicatedChangeState(PiscusBehaviour.Flee, _currentTarget);
			return;
		}
		if (TargetOutOfReach())
		{
			_currentSpeed = _minSpeed;
			ReplicatedChangeState(PiscusBehaviour.Flee, _currentTarget);
			return;
		}
		float magnitude = (transform.position - _currentTarget.transform.position).magnitude;
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			SEARCH_FOR_PLAYER();
			if (_currentTarget == null)
			{
				return;
			}
			if (CheckTargetRepellent())
			{
				ReplicatedChangeState(PiscusBehaviour.Flee, _currentTarget);
				return;
			}
			float num = Mathf.Clamp01((_maxSpeed - _minSpeed) * _attackPoints);
			float value = _minSpeed + num;
			_currentSpeed = Mathf.Clamp(value, _minSpeed, _maxSpeed);
			if (_timeSinceLastAvoidance > 1f)
			{
				float num2 = _circleRadius + (_maxCircleRadius - _circleRadius) * _attackPoints;
				Vector3 vector = transform.forward;
				if (UnityEngine.Random.value < _randCircleFreq && magnitude > _circleRadius)
				{
					float num3 = ((UnityEngine.Random.value < 0.5f) ? 1 : (-1));
					Vector3 vector2 = Vector3.Lerp(vector, UnityEngine.Random.insideUnitSphere * num3, 0.4f);
					Vector3 b = _currentTarget.transform.position + vector2 * num2;
					_movementPosition = Vector3.Lerp(transform.position + transform.forward, b, 0.5f);
					_movementPosition = ClampToBounds(_movementPosition);
					if (_debug)
					{
						_debugWaypointType = "CircleRandom";
					}
				}
				else
				{
					if (_targetBehindBossTimer > 5f)
					{
						vector = Quaternion.AngleAxis((UnityEngine.Random.value < 0.5f) ? 30 : (-30), Vector3.up) * -transform.forward;
						_targetBehindBossTimer = 0f;
					}
					Vector2 center = new Vector2(_currentTarget.transform.position.x, _currentTarget.transform.position.z);
					Vector3 vector3 = transform.position + vector * 6f;
					Vector2 position = new Vector2(vector3.x, vector3.z);
					Vector2 vector4 = ClampPositionToCircle(center, num2, position);
					float y = Mathf.Lerp(_movementPosition.y, _currentTarget.transform.position.y, 8f * FixedTime.TimeStep(this));
					_movementPosition = Vector3.Lerp(b: new Vector3(vector4.x, y, vector4.y), a: transform.position + transform.forward, t: 0.5f);
					_movementPosition = ClampToBounds(_movementPosition);
					if (_debug)
					{
						_debugWaypointType = "Circle";
					}
				}
			}
			else
			{
				NewExploreWaypoint(6f);
			}
		}
		CalculateAttack();
		UPDATE_MOVEMENT();
	}

	private bool CirclingTimerExpiry()
	{
		_currentCircleTime += FixedTime.TimeStep(this);
		if (_circleTargetTime > 0f && _currentCircleTime > _circleTargetTime)
		{
			_currentCircleTime = 0f;
			return true;
		}
		return false;
	}

	private void CalculateAttack()
	{
		if (_canAttack)
		{
			_attackPoints += _aggression * 0.01f * FixedTime.TimeStep(this);
			_attackPoints = Mathf.Clamp01(_attackPoints);
			if (_attackPoints >= 1f)
			{
				Attempt_Attacks();
			}
			else if (_canAttackNudge)
			{
				Attempt_Player_Attack_Nudge();
			}
		}
	}

	protected virtual void Attempt_Attacks()
	{
		if (_currentTarget != null)
		{
			if (_currentTarget.Movement.InWater)
			{
				Attempt_Attack_Player_InWater();
			}
			else if (_canAttackRafts && _currentTarget.Movement.ActivePlatform != null)
			{
				Attempt_Attack_Raft();
			}
			else if (_canAttackJump && Validate_Jump_Attack())
			{
				Attempt_Attack_Jump();
			}
		}
	}

	private void Attempt_Player_Attack_Nudge()
	{
		if (_currentTarget == null || !_currentTarget.Movement.InWater)
		{
			return;
		}
		if (_lastNudgeAttackTime < 30f)
		{
			_lastNudgeAttackTime += FixedTime.TimeStep(this);
		}
		if (_mouthPosition != null && _lastNudgeAttackTime > 10f && _lastAttack > 5f)
		{
			if ((_mouthPosition.position - _currentTarget.transform.position).magnitude < 2.5f && Vector3.Angle(transform.forward, _currentTarget.transform.position - transform.position) < 120f)
			{
				_targetBehindBossTimer = 0f;
				ReplicatedAttack_Nudge();
			}
			else
			{
				_targetBehindBossTimer += FixedTime.TimeStep(this);
			}
		}
	}

	public void Attack_Nudge()
	{
		_animation.Animation_Damage(_currentTarget.transform.position);
		Vector3 dir = _currentTarget.transform.position - _mouthPosition.position;
		_currentTarget.Movement.AddVelocity(dir, 6f);
		_currentTarget.Statistics.ApplyDamage(_attackDamage * 0.2f, transform.position, "Shark Nudge");
		_lastNudgeAttackTime = 0f;
	}

	public void Attempt_Attack_Player_InWater(bool retaliation = false)
	{
		if (_currentTarget == null || CheckAttackTimer())
		{
			return;
		}
		Vector3 velocity = _currentTarget.Movement.GetVelocity();
		Vector3 vector = _currentTarget.transform.position + velocity;
		if (!IsAttackPositionValid_Player_InWater(vector))
		{
			return;
		}
		if (Vector3.Angle(transform.forward, _currentTarget.transform.position - transform.position) > 100f)
		{
			_targetBehindBossTimer += FixedTime.TimeStep(this);
			return;
		}
		_targetBehindBossTimer = 0f;
		bool flag = true;
		Vector3 direction = vector - transform.position;
		if (Physics.SphereCast(transform.position, _avoidanceRayWidth, direction, out var hitInfo, direction.magnitude, _avoidanceMask, QueryTriggerInteraction.Ignore) && hitInfo.collider.gameObject.transform != _currentTarget.transform && hitInfo.collider.gameObject.transform != transform)
		{
			flag = false;
		}
		if (flag)
		{
			_currentAttackTarget = _currentTarget;
			ReplicatedChangeState(PiscusBehaviour.AttackPlayer, _currentTarget);
		}
	}

	protected virtual bool IsAttackPositionValid_Player_InWater(Vector3 attackPosition)
	{
		return attackPosition.y < 0f;
	}

	protected IEnumerator AttackPlayer_EnterState()
	{
		if (_currentTarget == null)
		{
			FinishAttack();
			yield break;
		}
		AttackedPlayer?.Invoke();
		Vector3 velocity = _currentTarget.Movement.GetVelocity();
		_movementPosition = _currentTarget.transform.position + velocity;
		_movementPosition = ClampToBounds(_movementPosition, minClamp: false);
		if (_debug)
		{
			_debugWaypointType = "AttackPlayerEnter";
		}
	}

	protected void AttackPlayer_FixedUpdate()
	{
		if (_currentTarget == null)
		{
			ReplicatedFinishAttack();
			return;
		}
		if ((_mouthPosition.position - _currentTarget.transform.position).magnitude < 4.5f)
		{
			ReplicatedPlayAnimation(0);
		}
		float magnitude = _currentTarget.Movement.GetVelocity().magnitude;
		_currentSpeed = _maxSpeed + magnitude;
		_currentSpeed = Mathf.Clamp(_currentSpeed, _minSpeed, _maxSpeed);
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			if ((_mouthPosition.position - _currentTarget.transform.position).magnitude < 3f && Vector3.Angle(transform.forward, _currentTarget.transform.position - transform.position) < 90f)
			{
				if (UnityEngine.Random.value < _attackEventChance)
				{
					ReplicatedChangeState(PiscusBehaviour.AttackingPlayer, _currentTarget);
				}
				else if (_currentTarget == null)
				{
					ReplicatedFinishAttack();
				}
				else
				{
					ReplicatedAttack_Player_Bite();
				}
				return;
			}
			FinishAttack();
		}
		UPDATE_MOVEMENT();
	}

	protected IEnumerator AttackPlayer_ExitState()
	{
		if (!CurrentState.Equals(PiscusBehaviour.AttackingPlayer))
		{
			ReplicatedFinishAttack();
		}
		yield break;
	}

	public void Attack_Player_Bite(float force = 6f)
	{
		_currentTarget.Statistics.ApplyDamage(_attackDamage, transform.position, "Shark Bite");
		Vector3 dir = _currentTarget.transform.position - transform.position;
		_currentTarget.Movement.AddVelocity(dir, force);
		OnAttacked();
		FinishAttack(completedAttack: true);
	}

	protected IEnumerator AttackingPlayer_EnterState()
	{
		if (_currentTarget == null)
		{
			FinishAttack();
			yield break;
		}
		AttackedPlayer?.Invoke();
		FreezePlayerControls(freeze: true);
		_animation.Animation_Attacking();
		_currentTarget.Statistics.ApplyStatusEffect(StatusEffect.BLEEDING);
		_currentTarget.Statistics.ApplyDamage(_attackCritDamage, transform.position, "Shark Bite");
		NewExploreWaypoint();
		OnAttacked();
	}

	protected void AttackingPlayer_FixedUpdate()
	{
		if (_currentTarget == null)
		{
			ReplicatedFinishAttack();
			return;
		}
		BiteSnapUpdate();
		_currentSpeed = _minSpeed;
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			NewExploreWaypoint();
		}
		_attackingTimer += FixedTime.TimeStep(this);
		if (_attackingTimer > 5f)
		{
			_attackingTimer = 0f;
			Vector3 velocityDir = _currentTarget.transform.position - transform.position;
			_currentTarget.Movement.ReplicatedAddVelocity(velocityDir, 6f);
			ReplicatedFinishAttack(completedAttack: true);
		}
		UPDATE_MOVEMENT();
	}

	protected IEnumerator AttackingPlayer_ExitState()
	{
		ReplicatedFinishAttack();
		yield break;
	}

	private void FreezePlayerControls(bool freeze)
	{
		if (_currentTarget == null)
		{
			return;
		}
		if (freeze)
		{
			_playerFreezed = true;
			if ((bool)_currentTarget.Movement)
			{
				++_currentTarget.Movement.IsBusy;
				_currentTarget.PlayerCamera.MouseLook.enabled = false;
			}
		}
		else if (_playerFreezed)
		{
			_playerFreezed = false;
			if ((bool)_currentTarget.Movement)
			{
				--_currentTarget.Movement.IsBusy;
				_currentTarget.PlayerCamera.MouseLook.enabled = _currentTarget.IsOwner;
			}
		}
	}

	protected IEnumerator AttackRaft_EnterState()
	{
		if (_currentTarget == null)
		{
			FinishAttack();
			yield break;
		}
		Transform activePlatform = _currentTarget.Movement.ActivePlatform;
		if (activePlatform == null)
		{
			FinishAttack();
			yield break;
		}
		AttackedPlayer?.Invoke();
		Vector3 vector = Vector3.zero;
		if ((bool)activePlatform.GetComponent<Rigidbody>())
		{
			vector = activePlatform.GetComponent<Rigidbody>().velocity;
		}
		else
		{
			Debug.LogWarning("Piscus_Creature :: No Rigidbody found on Raft target");
		}
		_movementPosition = activePlatform.transform.position + vector;
		_movementPosition = ClampToBounds(_movementPosition);
		if (_debug)
		{
			_debugWaypointType = "AttackRaftEnter";
		}
	}

	protected void AttackRaft_FixedUpdate()
	{
		if (_currentTarget == null)
		{
			ReplicatedFinishAttack();
			return;
		}
		Transform activePlatform = _currentTarget.Movement.ActivePlatform;
		if (activePlatform == null)
		{
			ReplicatedFinishAttack();
			return;
		}
		_currentSpeed = _maxSpeed;
		if ((_mouthPosition.position - activePlatform.transform.position).magnitude < 5f)
		{
			ReplicatedPlayAnimation(0);
		}
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			if ((_mouthPosition.position - activePlatform.transform.position).magnitude < 3f)
			{
				if ((bool)activePlatform.GetComponent<Rigidbody>())
				{
					activePlatform.GetComponent<Rigidbody>().AddForceAtPosition(transform.forward * _attackRaftForce + Vector3.up * 2f, _mouthPosition.position, ForceMode.Impulse);
				}
				OnAttackedRaft();
				_lastAttack = 0f;
			}
			ReplicatedFinishAttack();
		}
		UPDATE_MOVEMENT();
	}

	public void Attempt_Attack_Raft()
	{
		if (CheckAttackTimer())
		{
			return;
		}
		Transform activePlatform = _currentTarget.Movement.ActivePlatform;
		if (activePlatform == null || activePlatform.transform.position.y > 5f)
		{
			return;
		}
		bool flag = transform.position.y < activePlatform.transform.position.y - 3f;
		Vector3 direction = activePlatform.position - transform.position;
		if (Physics.SphereCast(transform.position, _avoidanceRayWidth, direction, out var hitInfo, direction.magnitude, 1 << Layers.TERRAIN, QueryTriggerInteraction.Ignore) && (bool)hitInfo.collider)
		{
			flag = false;
		}
		if (Physics.SphereCast(transform.position, _avoidanceRayWidth, -transform.up, out hitInfo, 3f, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		if (flag)
		{
			ReplicatedChangeState(PiscusBehaviour.AttackRaft, _currentTarget);
			return;
		}
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			Vector3 vector = transform.position + transform.forward * 12f;
			Vector2 vector2 = ClampPositionToCircle(new Vector2(activePlatform.transform.position.x, activePlatform.transform.position.z), _maxCircleRadius, new Vector2(vector.x, vector.z));
			_movementPosition = new Vector3(vector2.x, activePlatform.transform.position.y - 5f, vector2.y);
			_movementPosition = ClampToBounds(_movementPosition);
			if (_debug)
			{
				_debugWaypointType = "AttackRaftPrep";
			}
		}
	}

	private bool Validate_Jump_Attack()
	{
		if (CheckAttackTimer())
		{
			return false;
		}
		if (_currentTarget == null)
		{
			return false;
		}
		float num = 4f;
		if (_currentTarget.transform.position.y > num)
		{
			return false;
		}
		if (Physics.Raycast(new Vector3(_currentTarget.transform.position.x, 0f, _currentTarget.transform.position.z), Vector3.down, out var hitInfo, 8f, 1 << Layers.TERRAIN, QueryTriggerInteraction.Ignore) && (bool)hitInfo.collider && hitInfo.collider.gameObject.tag == "Terrain")
		{
			return false;
		}
		return true;
	}

	public void Attempt_Attack_Jump()
	{
		if (CheckAttackTimer() || _currentTarget == null)
		{
			return;
		}
		Vector3 vector = transform.position + transform.forward * 12f;
		Vector2 vector2 = ClampPositionToCircle(new Vector2(_currentTarget.transform.position.x, _currentTarget.transform.position.z), _maxCircleRadius, new Vector2(vector.x, vector.z));
		_movementPosition = new Vector3(vector2.x, Mathf.Lerp(_movementPosition.y, -6.5f, 8f * FixedTime.TimeStep(this)), vector2.y);
		_movementPosition = ClampToBounds(_movementPosition);
		if (_debug)
		{
			_debugWaypointType = "JumpAttackPrep";
		}
		Vector3 a = new Vector3(_currentTarget.transform.position.x, 0f, _currentTarget.transform.position.z);
		Vector3 b = new Vector3(transform.position.x, 0f, transform.position.z);
		float num = Vector3.Distance(a, b);
		if ((!(num < _circleRadius * 0.5f) || !(num > _circleRadius * 0.2f)) && !(transform.position.y < -8f) && !(transform.position.y > -1f))
		{
			Vector3 direction = NewJumpWaypoint() - transform.position;
			if (!Physics.SphereCast(transform.position, _avoidanceRayWidth, direction, out var hitInfo, direction.magnitude, 1 << Layers.TERRAIN, QueryTriggerInteraction.Ignore) || !hitInfo.collider)
			{
				_currentAttackTarget = _currentTarget;
				ReplicatedChangeState(PiscusBehaviour.AttackJump, _currentTarget);
			}
		}
	}

	protected IEnumerator AttackJump_EnterState()
	{
		_jumped = false;
		_movementPosition = NewJumpWaypoint();
		if (_debug)
		{
			_debugWaypointType = "JumpAttack";
		}
		AttackedPlayer?.Invoke();
		yield break;
	}

	protected void AttackJump_Update()
	{
		_jumpTimer += FixedTime.TimeStep(this);
		if (_jumpTimer > 10f)
		{
			ReplicatedFinishAttack();
			return;
		}
		_currentSpeed = _maxSpeed * 2f;
		UPDATE_MOVEMENT(checkAvoidance: false);
		float magnitude = (_currentTarget.transform.position - _mouthPosition.transform.position).magnitude;
		if (magnitude < 5.5f)
		{
			ReplicatedPlayAnimation(1);
		}
		if (!_jumped)
		{
			if (_distanceToMovePos < _waypointArrivalDist)
			{
				_jumped = true;
				OnJumped();
				_movementPosition = _currentTarget.transform.position + Vector3.up * 1f;
				if (_debug)
				{
					_debugWaypointType = "JumpAttackPlayer";
				}
			}
			return;
		}
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			_movementPosition = NewJumpLandWaypoint();
			if (_debug)
			{
				_debugWaypointType = "JumpAttackLand";
			}
			if (magnitude < 2f)
			{
				ReplicatedPlayAnimation(1);
				if (_currentTarget == null)
				{
					ReplicatedFinishAttack();
				}
				else
				{
					ReplicatedAttack_Player_Bite(10f);
				}
				return;
			}
		}
		if (transform.position.y < 0f)
		{
			_lastAttack = 0f;
			ReplicatedFinishAttack();
			OnLanded();
		}
	}

	private Vector3 NewJumpLandWaypoint()
	{
		Vector3 result = transform.position + transform.forward * 10f;
		result.y = -10f;
		return result;
	}

	private Vector3 NewJumpWaypoint()
	{
		Vector3 a = new Vector3(_currentTarget.transform.position.x, 0f, _currentTarget.transform.position.z);
		Vector3 b = new Vector3(transform.position.x, 0f, transform.position.z);
		return Vector3.Lerp(a, b, 0.5f) + Vector3.up * 2f;
	}

	protected void FinishAttack(bool completedAttack = false, bool destroyed = false)
	{
		if (completedAttack)
		{
			_lastAttack = 0f;
			_attackPoints = 0f;
		}
		FreezePlayerControls(freeze: false);
		_animation.Animation_Attack_Complete();
		_currentSpeed = _maxSpeed;
		if (!destroyed)
		{
			CurrentState = ((_currentTarget == null) ? PiscusBehaviour.Explore : PiscusBehaviour.CircleTarget);
		}
		if ((_currentTarget != null) & completedAttack)
		{
			_movementPosition = transform.position + transform.forward * 6f;
			_movementPosition = ClampToBounds(_movementPosition);
			if (_debug)
			{
				_debugWaypointType = "FinishAttack";
			}
		}
		_currentAttackTarget = null;
		_currentTarget = null;
		if (!destroyed && Game.Mode.IsMaster())
		{
			SEARCH_FOR_PLAYER();
		}
	}

	protected IEnumerator Flee_EnterState()
	{
		if (_currentTarget == null)
		{
			CurrentState = PiscusBehaviour.Explore;
			yield break;
		}
		_movementPosition = Avoid(_currentTarget.transform.position, 6f, UnityEngine.Random.Range(1, 6));
		_movementPosition = ClampToBounds(_movementPosition);
		_fleeTimer = 0f;
		if (_debug)
		{
			_debugWaypointType = "EnterFleeAvoid";
		}
	}

	protected void Flee_FixedUpdate()
	{
		_fleeTimer += FixedTime.TimeStep(this);
		if (_fleeTimer > 30f)
		{
			_fleeTimer = 0f;
			ReplicatedChangeState(PiscusBehaviour.Explore, _currentTarget);
			return;
		}
		if (_currentTarget == null)
		{
			ReplicatedChangeState(PiscusBehaviour.Explore, _currentTarget);
			return;
		}
		_distanceToMovePos = (transform.position - _movementPosition).magnitude;
		if (_distanceToMovePos < _waypointArrivalDist)
		{
			_movementPosition = Avoid(_currentTarget.transform.position, 6f, UnityEngine.Random.Range(1, 6));
			_movementPosition = ClampToBounds(_movementPosition);
			if (_debug)
			{
				_debugWaypointType = "FleeAvoid";
			}
		}
		if ((transform.position - _currentTarget.transform.position).magnitude > _engagePlayerDistance)
		{
			ReplicatedChangeState(PiscusBehaviour.Explore, _currentTarget);
		}
		UPDATE_MOVEMENT();
	}

	protected virtual void Die()
	{
	}

	public IEnumerator Die_EnterState()
	{
		Animation.AnimatorController.SetTrigger("Dead");
		Die();
		yield return new WaitForSeconds(4f);
		IDisintegratable disintegratable = _interactiveObject as IDisintegratable;
		if (!disintegratable.IsNullOrDestroyed())
		{
			disintegratable.Disintegrate();
		}
		else
		{
			_interactiveObject.Destroy();
		}
	}

	public void Damaged(IBase sender, IBaseActionEventData data)
	{
		_interactiveObject.HealthPoints -= data.Value;
		ApplyDamage(data.Value);
	}

	public virtual void ApplyDamage(float damageTaken)
	{
		if (_currentTarget == null)
		{
			return;
		}
		_animation.Animation_Damage(_currentTarget.transform.position);
		if (_interactiveObject.HealthPoints <= 0f)
		{
			if (CurrentState.Equals(PiscusBehaviour.AttackingPlayer))
			{
				ReplicatedChangeState(PiscusBehaviour.Die, _currentTarget, finishAttack: true);
			}
			else
			{
				ReplicatedChangeState(PiscusBehaviour.Die, _currentTarget);
			}
		}
		else
		{
			if (_currentAttackTarget != null)
			{
				return;
			}
			Damage_Avoid();
			if (_canAttack)
			{
				if (UnityEngine.Random.value < 0.5f)
				{
					_attackPoints = 1f;
					Attempt_Attack_Player_InWater(retaliation: true);
				}
				_lastAttack = _attackRate;
				_attackPoints += _aggression * 0.05f;
				_attackPoints = Mathf.Clamp01(_attackPoints);
			}
			else
			{
				CurrentState = PiscusBehaviour.Flee;
			}
		}
	}

	protected virtual void Damage_Avoid()
	{
		_currentSpeed = _maxSpeed;
		_movementPosition = Avoid(_currentTarget.transform.position + _currentTarget.Movement.GetVelocity(), 10f);
		_movementPosition = ClampToBounds(_movementPosition);
		if (_debug)
		{
			_debugWaypointType = "DamageAvoid";
		}
	}

	private bool CheckTargetRepellent()
	{
		if (_avoidRepellent && _currentTarget != null)
		{
			return _currentTarget.Statistics.HasStatusEffect(StatusEffect.SHARK_REPELLENT);
		}
		return false;
	}

	private void CheckWaypointBounds()
	{
		if ((transform.position - _biomePosition).magnitude > BiomeSize)
		{
			_outOfBoundsTimer += FixedTime.TimeStep(this);
			if (_distanceToMovePos < _waypointArrivalDist)
			{
				Vector3 normalized = (_biomePosition - transform.position).normalized;
				_movementPosition = transform.position + normalized * 12f;
				_movementPosition = ClampToBounds(_movementPosition);
				if (_debug)
				{
					_debugWaypointType = "BiomeCenterDirection";
				}
			}
		}
		else
		{
			_outOfBoundsTimer = 0f;
		}
		if (_outOfBoundsTimer > 10f)
		{
			ResetPosition();
		}
	}

	private void ResetPosition()
	{
		transform.position = _biomePosition;
		NewExploreWaypoint();
	}

	private void CheckWaypointStuck()
	{
		if (_distanceToMovePos < 5f)
		{
			_wayPointStuckTime += FixedTime.TimeStep(this);
		}
		else
		{
			_wayPointStuckTime = 0f;
		}
		if (_wayPointStuckTime > 10f)
		{
			_wayPointStuckTime = 0f;
			_distanceToMovePos = 0f;
		}
	}

	public bool CheckAttackTimer()
	{
		if (_lastAttack < _attackRate)
		{
			return true;
		}
		return false;
	}

	private void SteerAvoidance()
	{
		Vector3 forward = transform.forward;
		Vector3 vector = transform.position + -transform.up * 0.2f;
		int num = (int)((float)_avoidanceRayDistance * 1.5f);
		if (Physics.SphereCast(vector, _avoidanceRayWidth, Quaternion.AngleAxis(0f - _rayLeftAngle, transform.up) * forward, out var hitInfo, _avoidanceRayDistance, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			hit_Left_Weight = Mathf.Lerp(1f, 0.05f, hitInfo.distance / (float)_avoidanceRayDistance);
			hit_Left = true;
		}
		else
		{
			hit_Left_Weight = 0f;
			hit_Left = false;
		}
		if (Physics.SphereCast(vector, _avoidanceRayWidth, Quaternion.AngleAxis(_rayRightAngle, transform.up) * forward, out hitInfo, _avoidanceRayDistance, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			hit_Right_Weight = Mathf.Lerp(1f, 0.05f, hitInfo.distance / (float)_avoidanceRayDistance);
			hit_Right = true;
		}
		else
		{
			hit_Right_Weight = 0f;
			hit_Right = false;
		}
		if (Physics.SphereCast(vector, _avoidanceRayWidth, forward, out hitInfo, num, _avoidanceMask, QueryTriggerInteraction.Ignore))
		{
			hit_Forward = true;
		}
		else
		{
			hit_Forward = false;
		}
		if (_timeSinceLastAvoidance < 30f)
		{
			_timeSinceLastAvoidance += FixedTime.TimeStep(this);
		}
		if (Physics.Raycast(vector + Vector3.up * 20f, -Vector3.up, out hitInfo, 20f, 1 << Layers.TERRAIN, QueryTriggerInteraction.Ignore))
		{
			_underTerrainTime += FixedTime.TimeStep(this);
		}
		else if (!hit_Left && !hit_Right && !hit_Forward)
		{
			_underTerrainTime = 0f;
		}
		if (_underTerrainTime > 5f)
		{
			_cornerTrapTimer = 0f;
			if (_underTerrainTime > 60f)
			{
				_underTerrainTime = 0f;
				ResetPosition();
			}
			return;
		}
		if (hit_Left && hit_Right)
		{
			_cornerTrapTimer += FixedTime.TimeStep(this);
			if (_cornerTrapTimer > 0.5f && !selectTurnLeft && !selectTurnRight)
			{
				if (hit_Left_Weight > hit_Right_Weight)
				{
					selectTurnLeft = true;
				}
				else
				{
					selectTurnRight = true;
				}
			}
		}
		else if (_cornerTrapTimer > 0f)
		{
			_cornerTrapTimer -= FixedTime.TimeStep(this);
		}
		if ((!hit_Left && !hit_Right) || _cornerTrapTimer <= 0.1f)
		{
			selectTurnLeft = false;
			selectTurnRight = false;
		}
		if (hit_Left || hit_Right || hit_Forward)
		{
			_timeSinceLastAvoidance = 0f;
			newInputSteer = hit_Left_Weight + (0f - hit_Right_Weight);
			if (selectTurnRight)
			{
				newInputSteer = hit_Left_Weight + hit_Right_Weight;
			}
			else if (selectTurnLeft)
			{
				newInputSteer = 0f - (hit_Left_Weight + hit_Right_Weight);
			}
			newInputSteer = Mathf.Clamp(newInputSteer, -1f, 1f);
			float num2 = _waypointArrivalDist * 2f;
			Vector3 vector2 = forward * num2;
			Vector3 vector3 = transform.right * num2 * newInputSteer;
			_movementPosition = transform.position + vector2 + vector3;
			_movementPosition.y = transform.position.y;
			if (hit_Forward)
			{
				_movementPosition += transform.up;
				if (_debug)
				{
					Debug.DrawRay(vector, forward * num + transform.up, Color.yellow);
				}
			}
			_movementPosition = ClampToBounds(_movementPosition, minClamp: true, boundsClamp: false);
			if (_debug)
			{
				_debugWaypointType = "Avoid";
			}
		}
		if (_debug)
		{
			_debugCurrentState = (PiscusBehaviour)CurrentState;
			Debug.DrawRay(transform.position, _movementPosition - transform.position, Color.green);
			Debug.DrawRay(transform.position, _biomePosition - transform.position, Color.red);
			if (hit_Left)
			{
				Debug.DrawRay(vector, Quaternion.AngleAxis(0f - _rayLeftAngle, transform.up) * forward * _avoidanceRayDistance, Color.red);
			}
			else
			{
				Debug.DrawRay(vector, Quaternion.AngleAxis(0f - _rayLeftAngle, transform.up) * forward * _avoidanceRayDistance, Color.white);
			}
			if (hit_Right)
			{
				Debug.DrawRay(vector, Quaternion.AngleAxis(_rayRightAngle, transform.up) * forward * _avoidanceRayDistance, Color.red);
			}
			else
			{
				Debug.DrawRay(vector, Quaternion.AngleAxis(_rayRightAngle, transform.up) * forward * _avoidanceRayDistance, Color.white);
			}
			if (hit_Forward)
			{
				Debug.DrawRay(vector, forward * num, Color.red);
			}
			else
			{
				Debug.DrawRay(vector, Quaternion.AngleAxis(0f, transform.up) * forward * num, Color.green);
			}
		}
	}

	public Vector3 Avoid(Vector3 targetPos, float dist = 6f, float zigzag = 1f)
	{
		Vector3 vector = transform.position - targetPos;
		return transform.position + vector.normalized * dist + transform.right * UnityEngine.Random.Range(0f - zigzag, zigzag);
	}

	private void NewExploreWaypoint(float dist = 12f)
	{
		float num = ((UnityEngine.Random.value < 0.5f) ? 1 : (-1));
		Vector3 vector = Vector3.Lerp(transform.forward, UnityEngine.Random.insideUnitSphere * num, 0.4f);
		Vector3 vector2 = transform.position + vector * dist;
		Vector2 vector3 = ClampPositionToCircle(new Vector2(_biomePosition.x, _biomePosition.z), BiomeSize, new Vector2(vector2.x, vector2.z));
		_movementPosition = new Vector3(vector3.x, vector2.y, vector3.y);
		_movementPosition = ClampToBounds(_movementPosition);
		if (_debug)
		{
			_debugWaypointType = "Explore";
		}
	}

	public Vector3 ClampToBounds(Vector3 point, bool minClamp = true, bool boundsClamp = true)
	{
		Vector2 vector = (boundsClamp ? ClampPositionToCircle(new Vector2(_biomePosition.x, _biomePosition.z), BiomeSize, new Vector2(point.x, point.z)) : new Vector2(point.x, point.z));
		Vector3 vector2 = point;
		if (Ocean.Instance != null)
		{
			float num = Ocean.Instance.FindMaxDisplacement(inculdeOverlays: false);
			float num2 = Ocean.Instance.level - num;
			float max = Mathf.Min(_maxExploreDepth, num2 - _maxExploreDepth);
			vector2.y = Mathf.Clamp(vector2.y, _minExploreDepth, max);
		}
		else
		{
			vector2.y = Mathf.Clamp(vector2.y, _minExploreDepth, _maxExploreDepth);
		}
		return new Vector3(vector.x, vector2.y, vector.y);
	}

	public Vector2 ClampPositionToCircle(Vector2 center, float radius, Vector2 position)
	{
		Vector2 vector = position - center;
		float magnitude = vector.magnitude;
		if (radius < magnitude)
		{
			Vector2 vector2 = vector / magnitude;
			position = center + vector2 * radius;
		}
		return position;
	}

	private bool TargetOutOfReach()
	{
		if (_currentTarget == null)
		{
			return true;
		}
		if (_targetOutOfReachTime <= 0f)
		{
			return false;
		}
		bool flag = !_currentTarget.Movement.InWater && _currentTarget.Movement.ActivePlatform == null;
		if (Vector3.Distance(_currentTarget.transform.position, _biomePosition) > BiomeSize)
		{
			flag = true;
		}
		if (flag)
		{
			_targetOutOfReachTimer += FixedTime.TimeStep(this);
			if (_targetOutOfReachTimer >= _targetOutOfReachTime)
			{
				_targetOutOfReachTimer = 0f;
				return true;
			}
		}
		else
		{
			_targetOutOfReachTimer = 0f;
		}
		return false;
	}

	protected override void PlayAnimation(byte animationId)
	{
		switch (animationId)
		{
		case 0:
			_animation.Animation_Bite();
			break;
		case 1:
			_animation.Animation_Attacking();
			break;
		}
	}

	public void PlayDamageAnimation(IPlayer Player)
	{
		if (Player != null)
		{
			_animation.Animation_Damage(Player.transform.position);
		}
	}

	public void ReplicatedFinishAttack(bool completedAttack = false, bool destroyed = false)
	{
		if (Game.Mode.IsMaster())
		{
			ReplicateFinishAttack replicateFinishAttack = new ReplicateFinishAttack();
			replicateFinishAttack.Animal = this;
			replicateFinishAttack.CompletedAttack = completedAttack;
			replicateFinishAttack.Destroyed = destroyed;
			replicateFinishAttack.Post();
		}
		FinishAttack(completedAttack, destroyed);
	}

	public void ReplicatedAttack_Player_Bite(float force = 6f)
	{
		if (Game.Mode.IsMaster())
		{
			ReplicateAttackBite replicateAttackBite = new ReplicateAttackBite();
			replicateAttackBite.Animal = this;
			replicateAttackBite.Force = force;
			replicateAttackBite.Post();
		}
		Attack_Player_Bite(force);
	}

	public void ReplicatedAttack_Nudge()
	{
		if (Game.Mode.IsMaster())
		{
			ReplicateAttackNudge replicateAttackNudge = new ReplicateAttackNudge();
			replicateAttackNudge.Animal = this;
			replicateAttackNudge.CurrentTarget = _currentTarget;
			replicateAttackNudge.Post();
		}
		Attack_Nudge();
	}

	public void ReplicatedPlayAnimation(byte animationId)
	{
		if (Game.Mode.IsServer())
		{
			ReplicateAnimations replicateAnimations = new ReplicateAnimations();
			replicateAnimations.AnimationId = animationId;
			replicateAnimations.StateMachine = this;
			replicateAnimations.Post();
		}
		PlayAnimation(animationId);
	}

	public void ReplicatedChangeState(PiscusBehaviour NewState, IPlayer currentTarget, bool finishAttack = false)
	{
		if (currentTarget != _currentTarget || NewState != (PiscusBehaviour)CurrentState || finishAttack)
		{
			if (Game.Mode.IsServer() || NewState == PiscusBehaviour.Die)
			{
				ReplicateState replicateState = new ReplicateState();
				replicateState.NewState = NewState;
				replicateState.StateMachine = this;
				replicateState.CurrentTarget = currentTarget;
				replicateState.FinishAttack = finishAttack;
				replicateState.Post();
			}
			if (finishAttack)
			{
				FinishAttack();
			}
			if (Game.Mode.IsClient())
			{
				_currentTarget = currentTarget;
			}
			CurrentState = NewState;
		}
	}

	public void EventManager_PlayerDisconnected(PlayerDisconnectedEvent disconnectedEvent)
	{
		EventManager_PlayerDisconnected_Core(disconnectedEvent);
	}

	protected virtual void EventManager_PlayerDisconnected_Core(PlayerDisconnectedEvent disconnectedEvent)
	{
		if (_currentAttackTarget != null && PlayerUtilities.GetPlayerId_Safe(_currentAttackTarget) == disconnectedEvent.PlayerId)
		{
			FinishAttack();
		}
		if (_currentTarget != null && PlayerUtilities.GetPlayerId_Safe(_currentTarget) == disconnectedEvent.PlayerId)
		{
			_currentTarget = null;
			if (isActiveAndEnabled)
			{
				CurrentState = PiscusBehaviour.Explore;
			}
		}
	}
}
