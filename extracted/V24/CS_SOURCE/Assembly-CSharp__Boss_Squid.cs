using System.Collections;
using System.Collections.Generic;
using Beam;
using Beam.Utilities;
using Funlabs;
using MEC;
using UnityEngine;

public class Boss_Squid : Piscus_Creature
{
	public class ReplicateIntroSetup : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Creature;

		public override void OnPeer()
		{
			((Boss_Squid)Creature).Setup_Intro();
		}
	}

	private bool _introSetup;

	[SerializeField]
	private Boss_Squid_Tentacle _tentacle;

	private Vector3 _goalStartPosition;

	private Quaternion _goalStartRotation;

	public Boss_Squid_Tentacle Tentacle => _tentacle;

	protected override void Start()
	{
		base.Start();
		Timing.RunCoroutine(Start_Sequence());
	}

	private IEnumerator<float> Start_Sequence()
	{
		while (LevelLoader.IsLoading())
		{
			yield return 0f;
		}
		PostAssetLoad();
	}

	private void PostAssetLoad()
	{
		CurrentState = PiscusBehaviour.Idle;
		BiomePosition = transform.position;
		BiomeSize = 100f;
		_goalStartPosition = transform.position;
		_goalStartRotation = transform.rotation;
		SetDifficultyParams();
	}

	protected override void Initialize()
	{
	}

	protected override void SEARCH_FOR_PLAYER()
	{
		IPlayer currentTarget = PlayerUtilities.FindClosestPlayer(transform.position);
		CurrentTarget = currentTarget;
	}

	public override void ApplyDamage(float damageTaken)
	{
		base.ApplyDamage(damageTaken);
		Setup_Intro();
		_tentacle.TakeDamage();
	}

	protected IEnumerator Idle_EnterState()
	{
		CurrentSpeed = 0f;
		Animation.AnimatorController.SetFloat("Speed", 0f);
		transform.LookAt(transform.position + Vector3.up);
		yield break;
	}

	protected void Idle_FixedUpdate()
	{
		SEARCH_FOR_PLAYER();
		if (CurrentTarget == null)
		{
			CurrentState = PiscusBehaviour.Explore;
			return;
		}
		if (!_introSetup)
		{
			if (Vector3.Distance(CurrentTarget.transform.position, transform.position) < EngagePlayerDistance)
			{
				ReplicatedIntroSetup();
			}
			return;
		}
		UPDATE_MOVEMENT(checkAvoidance: false);
		if (Vector3.Distance(MovementPosition, transform.position) < 4f)
		{
			ReplicatedChangeState(PiscusBehaviour.CircleTarget, CurrentTarget);
		}
	}

	private void Setup_Intro()
	{
		if (!_introSetup)
		{
			_introSetup = true;
			MovementPosition = transform.position + transform.forward * 20f + transform.right * 25f;
			MovementPosition = ClampToBounds(MovementPosition);
			CurrentSpeed = MaxSpeed;
			OnAwakened();
		}
	}

	protected override void Damage_Avoid()
	{
	}

	protected override void Attempt_Attacks()
	{
		if (CanAttack)
		{
			if (CurrentTarget.Movement.InWater)
			{
				Attempt_Attack_Player_InWater();
			}
			else
			{
				Attempt_Tentacle_GrabAttack();
			}
		}
	}

	protected override bool IsAttackPositionValid_Player_InWater(Vector3 attackPosition)
	{
		if (attackPosition.y < transform.position.y - 1f)
		{
			return false;
		}
		return base.IsAttackPositionValid_Player_InWater(attackPosition);
	}

	private void Attempt_Tentacle_GrabAttack()
	{
		if (!CheckAttackTimer())
		{
			Vector3 a = new Vector3(CurrentTarget.transform.position.x, 0f, CurrentTarget.transform.position.z);
			Vector3 b = new Vector3(transform.position.x, 0f, transform.position.z);
			float num = Vector3.Distance(a, b);
			if (!(num > 40f) && !(num < 5f) && !(Vector3.Angle(-transform.forward, CurrentTarget.transform.position - transform.position) > 110f) && !(transform.position.y < -5f))
			{
				_tentacle.ReplicatedAttackTarget(CurrentTarget);
				LastAttack *= 0.5f;
			}
		}
	}

	private void Attempt_Tentacle_RaftAttack()
	{
		if (CurrentTarget == null)
		{
			return;
		}
		Transform activePlatform = CurrentTarget.Movement.ActivePlatform;
		if (activePlatform == null || activePlatform.transform.position.y > 5f)
		{
			return;
		}
		Vector3 vector = transform.position + transform.forward * 20f;
		Vector2 vector2 = ClampPositionToCircle(new Vector2(activePlatform.transform.position.x, activePlatform.transform.position.z), MaxCircleRadius, new Vector2(vector.x, vector.z));
		MovementPosition = new Vector3(vector2.x, Mathf.Lerp(MovementPosition.y, -0.65f, 8f * FixedTime.TimeStep(this)), vector2.y);
		MovementPosition = ClampToBounds(MovementPosition);
		if (!(transform.position.y < activePlatform.position.y - 5f))
		{
			Vector3 direction = activePlatform.position - transform.position;
			if (!Physics.SphereCast(transform.position, AvoidanceRayWidth, direction, out var hitInfo, direction.magnitude, 1 << Layers.TERRAIN, QueryTriggerInteraction.Ignore) || !hitInfo.collider)
			{
				_tentacle.ReplicatedAttackTarget(CurrentTarget);
			}
		}
	}

	public void Reset()
	{
		if (CurrentState.Equals(PiscusBehaviour.AttackingPlayer))
		{
			ReplicatedFinishAttack();
		}
		_introSetup = false;
		transform.position = _goalStartPosition;
		transform.rotation = _goalStartRotation;
		CurrentTarget = null;
		ReplicatedChangeState(PiscusBehaviour.Idle, CurrentTarget);
		transform.DestroyProjectilesInChildren();
	}

	private void ReplicatedIntroSetup()
	{
		if (Game.Mode.IsServer())
		{
			ReplicateIntroSetup replicateIntroSetup = new ReplicateIntroSetup();
			replicateIntroSetup.Creature = this;
			replicateIntroSetup.Post();
		}
		Setup_Intro();
	}

	protected override void EventManager_PlayerDisconnected_Core(PlayerDisconnectedEvent disconnectedEvent)
	{
		if (disconnectedEvent.PlayerId == 1)
		{
			if (CurrentAttackTarget != null && PlayerUtilities.GetPlayerId_Safe(CurrentAttackTarget) == disconnectedEvent.PlayerId)
			{
				FinishAttack(completedAttack: true);
			}
			if (CurrentTarget != null && PlayerUtilities.GetPlayerId_Safe(CurrentTarget) == disconnectedEvent.PlayerId)
			{
				IPlayer playerFromId = PlayerUtilities.GetPlayerFromId(0);
				CurrentTarget = playerFromId;
			}
		}
	}
}
