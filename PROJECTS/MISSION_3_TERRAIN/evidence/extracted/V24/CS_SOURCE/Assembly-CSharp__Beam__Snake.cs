using System;
using System.Collections;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam;

public class Snake : LandAnimal
{
	public class ReplicatePlayRattle : MultiplayerMessage
	{
		[Replicate]
		public LandAnimal animal;

		public override void OnPeer()
		{
			Snake snake = (Snake)animal;
			if (snake != null)
			{
				snake.ReplicatedPlayRattleNoise();
			}
		}
	}

	public class ReplicateAttack_Bite : MultiplayerMessage
	{
		[Replicate]
		public LandAnimal Animal;

		[Replicate]
		public IPlayer Player;

		[Replicate]
		public bool CorrectDistance;

		public override void OnPeer()
		{
			Snake snake = (Snake)Animal;
			if (snake != null && Player != null)
			{
				snake.ReplicatedAttack_Bite(Player, CorrectDistance);
			}
		}
	}

	[SerializeField]
	private Transform _head;

	private const float _exploreRange = 20f;

	private const float _distance_Attack = 1.5f;

	private const float _distance_AttackStance = 6.5f;

	private const float _distance_AvoidPlayer = 10f;

	private const float _distance_FleePlayer = 8f;

	private bool _canAttack = true;

	private float _attackDamage;

	[SerializeField]
	private float _normalAttackDamage = 75f;

	[SerializeField]
	private float _hardAttackDamage = 90f;

	private const float walkSpeed = 0.5f;

	private const float fleeSpeed = 2.1f;

	private bool _canMove;

	private float _currentIdleTime;

	private float _idleTime;

	private float _lastAttackTime;

	private float _attackCooldownSeconds = 5f;

	private float _headRaise;

	private float _headRaiseLerped;

	private float _lastRattle = 100f;

	private float _rattleTimer;

	private float _currentHidingTime;

	private float _chaseTimer;

	private float _fleeTime;

	public event Action Rattled;

	private void Awake()
	{
		Setup();
		SetDifficultyParams();
		_rigidbody.mass = 1f;
	}

	private void SetDifficultyParams()
	{
		_attackDamage = _normalAttackDamage;
		if (PlayerUtilities.GetServerGameDifficulty() == EGameDifficultyMode.Hard)
		{
			_attackDamage = _hardAttackDamage;
		}
		if (PlayerUtilities.GetServerWildlifeMode() == EGameWildlifeMode.Passive)
		{
			_canAttack = false;
		}
	}

	private void OnDestroy()
	{
		Rattled = null;
	}

	public override void FixedTick()
	{
		if (_rigidbody.useGravity)
		{
			_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			_rigidbody.useGravity = false;
		}
		if (!Game.Mode.IsClient())
		{
			_currentSpeed = Mathf.Lerp(_currentSpeed, _adjustedSpeed, FixedTime.TimeStep(this) * 2f);
			if (_canMove)
			{
				_waypointPosition = new Vector3(_waypointPosition.x, transform.position.y, _waypointPosition.z);
				CheckBounds();
				SteerAvoidance();
				MatchGroundNormal();
				_ = (_waypointPosition - transform.position).normalized;
				Vector3 vector = _waypointPosition - transform.position;
				vector.Normalize();
				float num = Mathf.PingPong(Time.time * (_currentSpeed * 0.5f), 2f) - 1f;
				Vector3 vector2 = transform.right * num * 1.5f * FixedTime.TimeStep(this);
				Vector3 vector3 = vector * _currentSpeed + vector2;
				vector3.y = 0f;
				Vector3 force = vector3 - _rigidbody.velocity;
				force.y = 0f;
				_rigidbody.AddForce(force, ForceMode.VelocityChange);
			}
			_rigidbody.AddForce(Physics.gravity, ForceMode.Acceleration);
			_controllerVelocity = CalculateTrueVelocity();
			if (_lastAttackTime < _attackCooldownSeconds + 1f)
			{
				_lastAttackTime += FixedTime.TimeStep(this);
			}
			if (_lastRattle < _rattleTimer + 1f)
			{
				_lastRattle += FixedTime.TimeStep(this);
			}
			Animation_Move();
		}
	}

	protected override void ReplicantUpdate()
	{
		if (!LevelLoader.IsLoading())
		{
			_controllerVelocity = CalculateTrueVelocity();
			Animation_Move();
		}
	}

	private IEnumerator Idle_EnterState()
	{
		_canMove = false;
		_currentIdleTime = 0f;
		_idleTime = UnityEngine.Random.Range(30, 120);
		_waypointPosition = transform.position + transform.forward;
		MatchGroundNormal();
		yield break;
	}

	private void Idle_Update()
	{
		_currentIdleTime += FixedTime.TimeStep(this);
		if (_currentIdleTime > _idleTime)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		if (!closestPlayer.IsNullOrDestroyed() && distanceToClosestPlayer < 10f)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
		}
		SEARCH_FOR_PLAYER();
	}

	private IEnumerator Explore_EnterState()
	{
		NewExploreWaypoint(20f);
		_canMove = true;
		_adjustedSpeed = 0.5f;
		_stuckWaypointTime = 0f;
		yield break;
	}

	private void Explore_Update()
	{
		_adjustedSpeed = 0.5f;
		if (!closestPlayer.IsNullOrDestroyed() && distanceToClosestPlayer < 10f)
		{
			_adjustedSpeed = 0.1f;
			if (CheckWaypointDist())
			{
				NewAvoidWaypoint();
			}
			if (distanceToClosestPlayer < 8f)
			{
				ReplicatedChangeState(LandAnimalState.Flee);
				return;
			}
		}
		if (distanceToClosestPlayer > 10f && CheckWaypointDist())
		{
			float value = UnityEngine.Random.value;
			if (value < 0.25f)
			{
				NewExploreWaypoint(20f);
				return;
			}
			if (value < 0.45f && FindHidingSpot())
			{
				ReplicatedChangeState(LandAnimalState.Hide);
				return;
			}
			ReplicatedChangeState(LandAnimalState.Idle);
		}
		CheckIfStuck();
		SEARCH_FOR_PLAYER();
	}

	private IEnumerator Hide_EnterState()
	{
		NewHideWaypoint();
		_canMove = true;
		_stuckWaypointTime = 0f;
		_currentHidingTime = 0f;
		_adjustedSpeed = 2.1f;
		yield break;
	}

	private void Hide_Update()
	{
		if (_hidingspot == null)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		_currentHidingTime += FixedTime.TimeStep(this);
		if (_currentHidingTime > 30f)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
		}
		else if ((new Vector2(_hidingspot.transform.position.x, _hidingspot.transform.position.z) - new Vector2(transform.position.x, transform.position.z)).magnitude < 0.75f)
		{
			ReplicatedChangeState(LandAnimalState.Idle);
		}
		else if (CheckWaypointDist())
		{
			NewHideWaypoint();
		}
	}

	private IEnumerator Hide_ExitState()
	{
		if (_hidingspot != null)
		{
			_hidingspot.HideChild = null;
			_hidingspot.Occupied = false;
			_hidingspot = null;
		}
		yield break;
	}

	private IEnumerator Chase_EnterState()
	{
		if (closestPlayer.IsNullOrDestroyed())
		{
			ReplicatedChangeState(LandAnimalState.Idle);
			yield break;
		}
		_canMove = true;
		_adjustedSpeed = 2.1f;
		_stuckWaypointTime = 0f;
		_chaseTimer = 0f;
	}

	private void Chase_Update()
	{
		_chaseTimer += FixedTime.TimeStep(this);
		if (closestPlayer.IsNullOrDestroyed() || _chaseTimer > 20f)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		if (distanceToClosestPlayer > 20f)
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		if (distanceToClosestPlayer < 1.5f)
		{
			ReplicatedChangeState(LandAnimalState.AttackStance);
			return;
		}
		if (CheckWaypointDist())
		{
			NewChaseWaypoint(closestPlayer.transform.position);
		}
		CheckIfStuck();
		SEARCH_FOR_PLAYER();
	}

	private IEnumerator Flee_EnterState()
	{
		NewAvoidWaypoint();
		_canMove = true;
		_fleeTime = 0f;
		_adjustedSpeed = 2.1f;
		_stuckWaypointTime = 0f;
		yield break;
	}

	private void Flee_Update()
	{
		if (CheckWaypointDist())
		{
			NewAvoidWaypoint();
		}
		_fleeTime += FixedTime.TimeStep(this);
		if (closestPlayer.IsNullOrDestroyed())
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		if (_canAttack && distanceToClosestPlayer < 6.5f && _lastAttackTime > _attackCooldownSeconds)
		{
			ReplicatedChangeState(LandAnimalState.AttackStance);
		}
		if ((distanceToClosestPlayer > 20f && _fleeTime > 10f) || _fleeTime > 30f)
		{
			ReplicatedChangeState((!FindHidingSpot()) ? LandAnimalState.Explore : LandAnimalState.Hide);
		}
		CheckIfStuck();
		SEARCH_FOR_PLAYER();
	}

	private IEnumerator AttackStance_EnterState()
	{
		_canMove = false;
		_headRaise = 1f;
		_lastAttackTime = _attackCooldownSeconds;
		MatchGroundNormal();
		yield break;
	}

	private void ReplicatedPlayRattleNoise()
	{
		if (Game.Mode.IsServer())
		{
			ReplicatePlayRattle replicatePlayRattle = new ReplicatePlayRattle();
			replicatePlayRattle.animal = this;
			replicatePlayRattle.Post();
		}
		Rattled?.Invoke();
	}

	private void AttackStance_Update()
	{
		if (closestPlayer.IsNullOrDestroyed())
		{
			ReplicatedChangeState(LandAnimalState.Explore);
			return;
		}
		if (distanceToClosestPlayer > 10f)
		{
			ReplicatedChangeState(LandAnimalState.Idle);
			return;
		}
		if (distanceToClosestPlayer < 1.5f && _lastAttackTime > _attackCooldownSeconds)
		{
			Vector3 direction = closestPlayer.PlayerCamera.transform.position + -Vector3.up * 0.17f - _head.position;
			if (!Physics.Raycast(transform.position, direction, out var _, direction.magnitude, _avoidanceMask, QueryTriggerInteraction.Ignore))
			{
				ReplicatedAttack_Bite(closestPlayer);
				StartCoroutine(AttackFleeDelay());
				_lastAttackTime = 0f;
				return;
			}
		}
		if (distanceToClosestPlayer < 6.5f && _lastRattle > _rattleTimer)
		{
			_lastRattle = 0f;
			_rattleTimer = UnityEngine.Random.Range(3, 9);
			ReplicatedPlayRattleNoise();
		}
		_headRaise = ((distanceToClosestPlayer < 10f && distanceToClosestPlayer > 2f) ? 1 : 0);
		SEARCH_FOR_PLAYER();
	}

	private IEnumerator AttackFleeDelay()
	{
		yield return new WaitForSeconds(0.5f);
		ReplicatedChangeState(LandAnimalState.Flee);
	}

	private IEnumerator AttackStance_ExitState()
	{
		_headRaise = 0f;
		yield break;
	}

	private void ReplicatedAttack_Bite(IPlayer player, bool correctDistance = false)
	{
		transform.LookAt(player.transform.position);
		transform.rotation = Quaternion.LookRotation(player.transform.position - transform.position);
		OnAttacked();
		Animation_Attack();
		if (Game.Mode.IsMaster())
		{
			correctDistance = (player.transform.position - _head.position).sqrMagnitude < BeamMath.Sqr(3f);
			ReplicateAttack_Bite replicateAttack_Bite = new ReplicateAttack_Bite();
			replicateAttack_Bite.Player = player;
			replicateAttack_Bite.Animal = this;
			replicateAttack_Bite.CorrectDistance = correctDistance;
			replicateAttack_Bite.Post();
		}
		if (correctDistance)
		{
			player.Statistics.ApplyDamage(_attackDamage, transform.position, "Snake Bite");
			player.Statistics.ApplyStatusEffect(StatusEffect.POISON);
		}
	}

	private IEnumerator Dying_EnterState()
	{
		_rigidbody.mass = 1f;
		_rigidbody.constraints = RigidbodyConstraints.None;
		Animation_Die();
		BeamTeam.Destroy(this);
		yield break;
	}

	public void TakeDamage()
	{
		SEARCH_FOR_PLAYER();
		if (closestPlayer.IsNullOrDestroyed())
		{
			ReplicatedChangeState(LandAnimalState.Explore);
		}
		else if (distanceToClosestPlayer < 10f)
		{
			if (_canAttack && _lastAttackTime > _attackCooldownSeconds)
			{
				ReplicatedChangeState(LandAnimalState.Chase);
			}
			else
			{
				ReplicatedChangeState(LandAnimalState.Flee);
			}
		}
	}

	private void Animation_Die()
	{
		if (!(_animator == null))
		{
			_animator.CrossFade("Die", 0.2f);
		}
	}

	private void Animation_Attack()
	{
		if (!(_animator == null))
		{
			_animator.Play("Attack");
		}
	}

	private void Animation_Move()
	{
		if (!(_animator == null))
		{
			_animator.SetFloat("Speed", _controllerVelocity);
			_headRaiseLerped = Mathf.Lerp(_headRaiseLerped, _headRaise, 3f * FixedTime.TimeStep(this));
			_animator.SetFloat("AngerValue", _headRaiseLerped);
		}
	}
}
