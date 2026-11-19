using UnityEngine;

public class Piscus_Animation : MonoBehaviour
{
	[SerializeField]
	private Animation _anim;

	[SerializeField]
	private Animator _animatorController;

	private float _normalDirection;

	public Animator AnimatorController => _animatorController;

	public void Animation_Setup()
	{
		if (_anim != null)
		{
			_anim.wrapMode = WrapMode.Loop;
			if ((bool)_anim["Attack"])
			{
				_anim["Attack"].layer = 2;
				_anim["Attack"].wrapMode = WrapMode.Once;
			}
			if ((bool)_anim["Damage_Left"])
			{
				_anim["Damage_Left"].layer = 1;
				_anim["Damage_Left"].wrapMode = WrapMode.Once;
			}
			if ((bool)_anim["Damage_Right"])
			{
				_anim["Damage_Right"].layer = 1;
				_anim["Damage_Right"].wrapMode = WrapMode.Once;
			}
			if ((bool)_anim["Attacking"])
			{
				_anim["Attacking"].wrapMode = WrapMode.Once;
				_anim["Attacking"].layer = 2;
			}
			if ((bool)_anim["Swimming_Left"])
			{
				_anim["Swimming_Left"].wrapMode = WrapMode.Loop;
			}
			if ((bool)_anim["Swimming_Right"])
			{
				_anim["Swimming_Right"].wrapMode = WrapMode.Loop;
			}
		}
	}

	public void UpdateAnimation(Vector3 movementPosition, float currentSpeed, float minSpeed, float maxSpeed)
	{
		float a = 0f;
		Vector3 vector = transform.InverseTransformPoint(movementPosition);
		a = Mathf.Lerp(a, Mathf.Atan2(vector.x, vector.z), 4f * Time.deltaTime);
		_normalDirection = Mathf.Lerp(_normalDirection, a, Time.deltaTime * 2f);
		if (_anim != null)
		{
			if (_normalDirection < -0.005f && (bool)_anim["Swimming_Left"])
			{
				_anim["Swimming_Left"].speed = Mathf.Clamp(currentSpeed / 2f, 0.5f, 2f);
				_anim.CrossFade("Swimming_Left", 0.4f);
			}
			else if (_normalDirection > 0.005f && (bool)_anim["Swimming_Right"])
			{
				_anim["Swimming_Right"].speed = Mathf.Clamp(currentSpeed / 2f, 0.5f, 2f);
				_anim.CrossFade("Swimming_Right", 0.4f);
			}
			else
			{
				_anim["Swimming"].speed = Mathf.Clamp(currentSpeed / 2f, 0.5f, 2f);
				_anim.CrossFade("Swimming", 0.4f);
			}
		}
		if ((bool)_animatorController)
		{
			_animatorController.SetFloat("Direction", _normalDirection);
			float value = (currentSpeed - minSpeed) / (maxSpeed - minSpeed);
			_animatorController.SetFloat("Speed", value);
		}
	}

	public void Animation_Damage(Vector3 targetPosition)
	{
		if (_anim != null)
		{
			float num = 0f;
			Vector3 vector = transform.InverseTransformPoint(targetPosition);
			num = Mathf.Atan2(vector.x, vector.z);
			if (num < -0.05f && (bool)_anim["Damage_Left"])
			{
				_anim.CrossFade("Damage_Left", 0.2f);
			}
			else if (num > 0.05f && (bool)_anim["Damage_Right"])
			{
				_anim.CrossFade("Damage_Right", 0.2f);
			}
			else if ((bool)_anim["Damage_Left"])
			{
				_anim.CrossFade("Damage_Left", 0.2f);
			}
		}
		if ((bool)_animatorController)
		{
			_animatorController.SetTrigger("TakeDamage");
		}
	}

	public void Animation_Bite()
	{
		if (_anim != null && (bool)_anim["Attack"])
		{
			_anim.CrossFade("Attack", 0.05f);
		}
		if ((bool)_animatorController)
		{
			_animatorController.SetTrigger("Attack");
		}
	}

	public void Animation_Attacking()
	{
		if (_anim != null && (bool)_anim["Attacking"])
		{
			_anim.CrossFade("Attacking");
		}
		if ((bool)_animatorController)
		{
			_animatorController.SetTrigger("Attack");
		}
	}

	public void Animation_Attack_Complete()
	{
		if (_anim != null && (bool)_anim["Attacking"] && (bool)_anim["Attack"])
		{
			_anim["Attack"].enabled = false;
			_anim["Attacking"].enabled = false;
		}
	}
}
