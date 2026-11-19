using Beam;
using UnityEngine;

public class Boss_Squid_Environment : MonoBehaviour
{
	[SerializeField]
	private Transform _buoy;

	[SerializeField]
	private Transform _buoyChain;

	[SerializeField]
	private Light _buoyLight;

	[SerializeField]
	private AudioSource _buoyBellAudio;

	private float _buoyBellTime;

	private float _buoyBellTimer;

	private void Start()
	{
		TriggerLight();
		if ((bool)Singleton<Atmosphere>.Instance)
		{
			Singleton<Atmosphere>.Instance.SunsetEvent += TriggerLight;
		}
	}

	private void OnDestroy()
	{
		if ((bool)Singleton<Atmosphere>.Instance)
		{
			Singleton<Atmosphere>.Instance.SunsetEvent -= TriggerLight;
		}
	}

	private void Update()
	{
		_buoyChain.position = new Vector3(_buoyChain.position.x, _buoy.position.y, _buoyChain.position.z);
		_buoyBellTime += Time.deltaTime;
		if (_buoyBellTime > _buoyBellTimer)
		{
			_buoyBellTime = 0f;
			_buoyBellTimer = Random.Range(5, 15);
			_buoyBellAudio.pitch = Random.Range(0.95f, 1.05f);
			_buoyBellAudio.Play();
			if (Random.value < 0.1f)
			{
				_buoyBellTimer = 1f;
			}
		}
	}

	private void TriggerLight()
	{
		_buoyLight.enabled = !Singleton<Atmosphere>.Instance.DayTime;
	}
}
