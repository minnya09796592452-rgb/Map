using System.Collections;
using Beam;
using UnityEngine;

public class Whale_Creature : Piscus_Creature
{
	private const byte ANIMATION_DIVE = 2;

	private const byte ANIMATION_ROLLOVER = 3;

	[SerializeField]
	private Animation _anim;

	[SerializeField]
	private Transform _airhole;

	[SerializeField]
	private ParticleSystem _tailWaterSpray;

	[SerializeField]
	private ParticleSystem _spitParticle;

	[SerializeField]
	private Whale_TailForce _whaleTailForce;

	private float _lastSpitTime = 60f;

	private float _nextSpitTime = 60f;

	private float _lastWhaleNoise;

	private float _nextWhaleNoise = 60f;

	private float _curCirclingTime;

	private float _circlingTime;

	protected override void Initialize()
	{
		if (_anim != null)
		{
			_anim.wrapMode = WrapMode.Loop;
			_anim["Dive"].wrapMode = WrapMode.Once;
			_anim["RollOver"].wrapMode = WrapMode.Once;
			_anim["RollOver"].speed = 0.3f;
			_anim["Dive"].layer = 1;
			_anim["RollOver"].layer = 1;
		}
		base.Initialize();
	}

	protected override void UPDATE_MOVEMENT(bool avoidance = true)
	{
		base.UPDATE_MOVEMENT(avoidance);
		if ((PiscusBehaviour)CurrentState == PiscusBehaviour.CircleTarget)
		{
			Attempt_Spray();
		}
		AttemptWhaleSound();
		CheckWaterSpray();
		CheckWaterSpit();
		if (_anim != null)
		{
			_anim["Swim"].speed = Mathf.Clamp(CurrentSpeed / 4f, 0.2f, 1f);
			_anim.CrossFade("Swim", 2f);
		}
	}

	protected override IEnumerator CircleTarget_EnterState()
	{
		if (CurrentTarget == null)
		{
			CurrentState = PiscusBehaviour.Explore;
			yield break;
		}
		Vector3 vector = Avoid(new Vector3(CurrentTarget.transform.position.x, transform.position.y, CurrentTarget.transform.position.z), 100f);
		Vector2 vector2 = ClampPositionToCircle(new Vector2(CurrentTarget.transform.position.x, CurrentTarget.transform.position.z), MaxCircleRadius, new Vector2(vector.x, vector.z));
		Vector3 vector3 = new Vector3(vector2.x, transform.position.y, vector2.y) - transform.position;
		MovementPosition = transform.position + vector3.normalized * 6f;
		MovementPosition = ClampToBounds(MovementPosition);
		_curCirclingTime = 0f;
		CurrentSpeed = MinSpeed;
	}

	protected override void CircleTarget_FixedUpdate()
	{
		if (CurrentTarget == null)
		{
			ReplicatedChangeState(PiscusBehaviour.Explore, CurrentTarget);
			return;
		}
		CurrentSpeed = MinSpeed;
		_curCirclingTime += FixedTime.TimeStep(this);
		if (_circlingTime > 0f && _curCirclingTime > _circlingTime)
		{
			_curCirclingTime = 0f;
			ReplicatedChangeState(PiscusBehaviour.Flee, CurrentTarget);
			return;
		}
		_ = (transform.position - CurrentTarget.transform.position).magnitude;
		if ((transform.position - MovementPosition).magnitude < 4f)
		{
			float y = _airhole.transform.position.y;
			if (y > 0f && Random.value < 0.05f)
			{
				MovementPosition = transform.position + transform.forward * 6f + -transform.up * 20f;
				MovementPosition = ClampToBounds(MovementPosition);
				ReplicatedPlayAnimation(2);
			}
			else if (y > 0f && Random.value < 0.05f)
			{
				MovementPosition = transform.position + transform.forward * 6f;
				MovementPosition = ClampToBounds(MovementPosition);
				ReplicatedPlayAnimation(3);
			}
			else
			{
				Vector3 forward = transform.forward;
				if (Random.value < 0.05f)
				{
					float num = ((Random.value < 0.5f) ? 1 : (-1));
					Vector3 vector = Vector3.Lerp(forward, Random.insideUnitSphere * num, 0.4f);
					MovementPosition = CurrentTarget.transform.position + vector * CircleRadius;
					MovementPosition = ClampToBounds(MovementPosition);
				}
				else
				{
					Vector3 vector2 = transform.position + forward * 6f;
					Vector2 vector3 = ClampPositionToCircle(new Vector2(CurrentTarget.transform.position.x, CurrentTarget.transform.position.z), CircleRadius, new Vector2(vector2.x, vector2.z));
					float y2 = Mathf.Lerp(MovementPosition.y, CurrentTarget.transform.position.y, 8f * FixedTime.TimeStep(this));
					Vector3 movementPosition = new Vector3(vector3.x, y2, vector3.y);
					MovementPosition = movementPosition;
					MovementPosition = ClampToBounds(MovementPosition);
				}
			}
		}
		UPDATE_MOVEMENT();
	}

	private void Dive()
	{
		Spray();
		if (_anim != null)
		{
			_anim.CrossFade("Dive", 0.5f);
		}
		StartCoroutine(_whaleTailForce.DiveForce());
	}

	private void RollOver()
	{
		Spray();
		if (_anim != null)
		{
			_anim.CrossFade("RollOver", 0.5f);
		}
		StartCoroutine(_whaleTailForce.DiveForce());
	}

	private void Attempt_Spray()
	{
		if (_lastSpitTime > _nextSpitTime)
		{
			Spray();
		}
	}

	private void Spray()
	{
		if (_airhole.position.y >= -0.25f)
		{
			_lastSpitTime = 0f;
			_nextSpitTime = Random.Range(15, 60);
			_spitParticle.Play(withChildren: true);
			OnAttackedRaft();
		}
	}

	private void AttemptWhaleSound()
	{
		_lastWhaleNoise += FixedTime.TimeStep(this);
		if (!(_lastWhaleNoise < _nextWhaleNoise) && !(Vector3.Distance(CurrentTarget.transform.position, transform.position) > 75f))
		{
			PlayerWhaleSound();
		}
	}

	private void PlayerWhaleSound()
	{
		_lastWhaleNoise = 0f;
		_nextWhaleNoise = Random.Range(30, 120);
		OnAttacked();
	}

	private void CheckWaterSpray()
	{
		if (_tailWaterSpray.transform.position.y > 0f)
		{
			if (_tailWaterSpray.isStopped)
			{
				_tailWaterSpray.Play(withChildren: true);
			}
		}
		else if (_tailWaterSpray.isPlaying)
		{
			_tailWaterSpray.Stop(withChildren: true);
		}
	}

	private void CheckWaterSpit()
	{
		if (_lastSpitTime < _nextSpitTime + 10f)
		{
			_lastSpitTime += FixedTime.TimeStep(this);
		}
	}

	protected override void PlayAnimation(byte animationId)
	{
		base.PlayAnimation(animationId);
		switch (animationId)
		{
		case 2:
			Dive();
			break;
		case 3:
			RollOver();
			break;
		}
	}
}
