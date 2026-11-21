using UnityEngine;

namespace Beam;

public class MotorVehicleAnimation : VehicleAnimation<MotorVehicleMovement>
{
	[SerializeField]
	private Transform _rudder;

	[SerializeField]
	private Transform _propeller;

	[SerializeField]
	private ParticleSystem _engineSmoke;

	[SerializeField]
	private ParticleSystem _waterWake;

	private MotorVehicleMovement _motorVehicleMovement;

	protected override void Start()
	{
		_motorVehicleMovement = VehicleMovement;
		if (_motorVehicleMovement != null)
		{
			_motorVehicleMovement.FuelRanOut += MotorVehicleMovement_FuelRanOut;
		}
		base.Start();
	}

	protected override void OnDestroy()
	{
		if (_motorVehicleMovement != null)
		{
			_motorVehicleMovement.FuelRanOut -= MotorVehicleMovement_FuelRanOut;
		}
		base.OnDestroy();
	}

	protected override void VehicleMovement_Operated(bool active)
	{
		base.VehicleMovement_Operated(active);
		VehicleMovement.Operator.Character.SetInteger("Vehicle_Pose", active ? 4 : 0);
		if (!(_motorVehicleMovement.Fuel <= 0f))
		{
			if (active)
			{
				PlayParticles();
			}
			else
			{
				StopParticles();
			}
		}
	}

	protected override void VehicleMovement_Using()
	{
		if (!(_motorVehicleMovement == null) && _motorVehicleMovement.EngineIsRunning())
		{
			AnimateEngine();
			AnimatePropeller();
			UpdateParticles();
		}
	}

	protected virtual void MotorVehicleMovement_FuelRanOut()
	{
		StopParticles();
	}

	private void AnimateEngine()
	{
		if (!(_motorVehicleMovement == null))
		{
			float num = Mathf.Sin(Time.time * 25f * (2f - VehicleMovement.Throttle));
			Vector3 localEulerAngles = _rudder.localEulerAngles;
			localEulerAngles.y += num;
			_rudder.localEulerAngles = localEulerAngles;
		}
	}

	private void AnimatePropeller()
	{
		if (_propeller != null)
		{
			float zAngle = 2000f * VehicleMovement.Throttle * Time.deltaTime;
			_propeller.transform.Rotate(0f, 0f, zAngle);
		}
	}

	private void PlayParticles()
	{
		_engineSmoke?.Play();
		_waterWake?.Play();
	}

	private void StopParticles()
	{
		_engineSmoke?.Stop();
		_waterWake?.Stop();
	}

	private void UpdateParticles()
	{
		if (_engineSmoke != null)
		{
			ParticleSystem.EmissionModule emission = _engineSmoke.emission;
			emission.rateOverTime = Mathf.Clamp(20f * VehicleMovement.Throttle, 5f, 20f);
		}
		if (_waterWake != null)
		{
			ParticleSystem.EmissionModule emission2 = _waterWake.emission;
			emission2.rateOverDistance = Mathf.Clamp(10f * VehicleMovement.Throttle, 0f, 10f);
		}
	}
}
