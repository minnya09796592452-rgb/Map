using System;
using Beam;
using UnityEngine;

public class Distant_Plane_Manager : MonoBehaviour
{
	[SerializeField]
	private Transform _distantPlane;

	private DateTime _lastFlightTime;

	private int flightInterval = 100;

	private float _speed = 0.5f;

	private bool _activated;

	private void Start()
	{
		Activate(activate: false);
		InvokeRepeating("Poll_FlightPath", 10f, GameTime.OneGameHour);
	}

	private void Update()
	{
		if (_activated)
		{
			_distantPlane.transform.position += _distantPlane.transform.forward * _speed;
			Vector3 position = Singleton<Atmosphere>.Instance.transform.position;
			position.y = _distantPlane.transform.position.y;
			if (Vector3.Distance(_distantPlane.transform.position, position) > 2200f)
			{
				Activate(activate: false);
			}
		}
	}

	private void Poll_FlightPath()
	{
		if (!_activated && GameTime.GetHoursSince(_lastFlightTime) > (float)flightInterval)
		{
			SetNewFlightPath();
			Activate(activate: true);
		}
	}

	private void Activate(bool activate)
	{
		_activated = activate;
		_distantPlane.gameObject.SetActive(activate);
		if (!activate)
		{
			_lastFlightTime = GameTime.Now;
			flightInterval = UnityEngine.Random.Range(12, 48);
		}
	}

	private void SetNewFlightPath()
	{
		Vector3 position = Singleton<Atmosphere>.Instance.transform.position;
		_distantPlane.transform.position = RandomCircle(position, 2000f);
		Vector3 forward = position - _distantPlane.transform.position;
		forward.y = 0f;
		_distantPlane.transform.rotation = Quaternion.LookRotation(forward);
	}

	private Vector3 RandomCircle(Vector3 center, float radius)
	{
		float num = UnityEngine.Random.value * 360f;
		Vector3 result = default;
		result.x = center.x + radius * Mathf.Sin(num * (MathF.PI / 180f));
		result.z = center.z + radius * Mathf.Cos(num * (MathF.PI / 180f));
		result.y = 950f;
		return result;
	}
}
