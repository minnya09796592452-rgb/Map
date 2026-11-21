using Beam;
using Beam.Utilities;
using Funlabs;
using UnityEngine;
using UnityEngine.Events;

public class Boss_Squid_Tentacle : MonoBehaviour
{
	public class ReplicateAttackTarget : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Creature;

		[Replicate]
		public IPlayer Player;

		public override void OnPeer()
		{
			((Boss_Squid)Creature).Tentacle.AttackTarget(Player);
		}
	}

	public class ReplicatePushAndDamagePlayer : MultiplayerMessage
	{
		[Replicate]
		public Piscus_Creature Creature;

		[Replicate]
		public IPlayer Player;

		[Replicate]
		public Vector3 Direction;

		[Replicate]
		public float Damage;

		public override void OnPeer()
		{
			((Boss_Squid)Creature).Tentacle.PushAndDamagePlayer(Player, Direction, Damage);
		}
	}

	[SerializeField]
	private Boss_Squid _bossSquid;

	[SerializeField]
	private Transform _Grab;

	[SerializeField]
	private SkinnedMeshRenderer _renderer;

	private Animation _animation;

	private Vector3 _grabTargetPosition;

	private float _grabSpeed = 0.75f;

	private float _reachDist;

	private bool _targetGrabbed;

	private bool _tentacleAttacking;

	private AnimationState _animReach;

	private AnimationState _animMiss;

	private AnimationState _animGrab;

	public UnityEvent Attacked;

	public UnityEvent AttackedGrabbed;

	public UnityEvent AttackedPushed;

	private void Start()
	{
		_animation = GetComponent<Animation>();
		_animReach = _animation["Reach_1"];
		_animMiss = _animation["Miss"];
		_animGrab = _animation["Grab"];
		_animation.wrapMode = WrapMode.ClampForever;
		_animGrab.wrapMode = WrapMode.Once;
		_animGrab.layer = 2;
	}

	private IPlayer FindClosestPlayer(Vector3 position)
	{
		IPlayer player = PlayerUtilities.FindClosestPlayer(position, out var _);
		if (!player.IsNullOrDestroyed())
		{
			return player;
		}
		return null;
	}

	private void Update()
	{
		if (_renderer.enabled != _tentacleAttacking)
		{
			_renderer.enabled = _tentacleAttacking;
		}
		if (!_tentacleAttacking)
		{
			return;
		}
		transform.LookAt(_grabTargetPosition);
		IPlayer player = FindClosestPlayer(_Grab.position);
		if (player != null)
		{
			if (Vector3.Distance(_Grab.position, player.transform.position) < 1f && !_targetGrabbed)
			{
				_targetGrabbed = true;
				_reachDist = _animReach.time;
				HitTarget(player);
			}
			float num = _animReach.length * _animReach.normalizedTime;
			if (num >= _animReach.length)
			{
				MissTarget();
			}
			else if (_animReach.speed < 0f && num <= 0f)
			{
				_tentacleAttacking = false;
			}
			else if (_animMiss.length * _animMiss.normalizedTime >= _animMiss.length)
			{
				_tentacleAttacking = false;
			}
		}
	}

	public void TakeDamage()
	{
		if (_tentacleAttacking)
		{
			IPlayer player = FindClosestPlayer(_Grab.position);
			if (player == null)
			{
				_animation.Stop();
			}
			else if (Vector3.Distance(_Grab.position, player.transform.position) < 12f)
			{
				_reachDist = _animReach.time;
				_animation.Stop();
				_animation.CrossFade("Grab", 0.03f);
				_animReach.time = _reachDist;
				_animReach.speed = 0f - _grabSpeed;
				_animation.Play("Reach_1");
			}
		}
	}

	public void AttackTarget(IPlayer player)
	{
		_tentacleAttacking = true;
		ReachForTarget(player);
	}

	private void ReachForTarget(IPlayer player)
	{
		_targetGrabbed = false;
		_grabTargetPosition = player.transform.position;
		_animation.Stop();
		_animGrab.time = 0f;
		_animReach.time = 0f;
		_animReach.speed = _grabSpeed;
		_animation.Play("Reach_1");
		Attacked?.Invoke();
	}

	private void HitTarget(IPlayer player)
	{
		_animation.Stop();
		_animation.CrossFade("Grab", 0.03f);
		_animReach.time = _reachDist;
		_animReach.speed = 0f - _grabSpeed;
		_animation.Play("Reach_1");
		if (player.IsOwner)
		{
			if (Random.value < 0.75f)
			{
				Grab_Player(player);
			}
			else
			{
				Push_Player(player);
			}
		}
	}

	private void MissTarget()
	{
		_animation.Play("Miss");
	}

	private void Grab_Player(IPlayer player)
	{
		AttackedGrabbed?.Invoke();
		Vector3 vector = new Vector3(transform.position.x, player.transform.position.y, transform.position.z);
		Vector3 position = player.transform.position;
		Vector3 direction = vector - position;
		ReplicatedPushAndDamagePlayer(player, direction, 25f);
	}

	private void Push_Player(IPlayer player)
	{
		AttackedPushed?.Invoke();
		Vector3 position = transform.position;
		Vector3 direction = player.transform.position - position;
		ReplicatedPushAndDamagePlayer(player, direction, 25f);
	}

	private void PushAndDamagePlayer(IPlayer player, Vector3 direction, float damage)
	{
		player.Movement.AddVelocity(direction, 3f);
		player.Statistics.ApplyDamage(damage, transform.position, "Tentacle");
	}

	public void ReplicatedAttackTarget(IPlayer player)
	{
		if (Game.Mode.IsMaster())
		{
			ReplicateAttackTarget replicateAttackTarget = new ReplicateAttackTarget();
			replicateAttackTarget.Creature = _bossSquid.GetComponent<Piscus_Creature>();
			replicateAttackTarget.Player = player;
			replicateAttackTarget.Post();
		}
		AttackTarget(player);
	}

	private void ReplicatedPushAndDamagePlayer(IPlayer player, Vector3 direction, float damage)
	{
		if (Game.Mode.IsServer())
		{
			ReplicatePushAndDamagePlayer replicatePushAndDamagePlayer = new ReplicatePushAndDamagePlayer();
			replicatePushAndDamagePlayer.Creature = _bossSquid.GetComponent<Piscus_Creature>();
			replicatePushAndDamagePlayer.Player = player;
			replicatePushAndDamagePlayer.Direction = direction;
			replicatePushAndDamagePlayer.Damage = damage;
			replicatePushAndDamagePlayer.Post();
		}
		PushAndDamagePlayer(player, direction, damage);
	}
}
