using Beam;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>
{
	public static Transform TRAIL_CONTAINER;

	[SerializeField]
	private Transform _trailContainer;

	public GameObject bubbleBurst;

	public GameObject PARTICLE_COOKING_SMOKE;

	public GameObject PARTICLE_FOOD_SPOIL;

	private void Awake()
	{
		TRAIL_CONTAINER = _trailContainer;
	}

	public static void CreateDamageParticle(IPlayer player)
	{
		RaycastHit lastHitInfo = player.GameInput.Raycaster.LastHitInfo;
		Vector3 point = lastHitInfo.point;
		Quaternion rotation = Quaternion.LookRotation(player.transform.position - point);
		GameObject particleFromMaterial = ParticleUtiliity.GetParticleFromMaterial(lastHitInfo.collider.sharedMaterial?.name);
		if (particleFromMaterial != null)
		{
			ParticleSystem component = ObjectPoolManager.CreatePooled(particleFromMaterial, point, rotation).GetComponent<ParticleSystem>();
			if ((bool)component)
			{
				component.GetComponent<Renderer>().enabled = true;
				component.time = 0f;
				component.Clear(withChildren: true);
				component.Play(withChildren: true);
			}
		}
	}

	public static GameObject CreateParticle(GameObject particle, Vector3 position, Vector3 rotation = default(Vector3))
	{
		if (!particle)
		{
			return null;
		}
		GameObject gameObject = ObjectPoolManager.CreatePooled(particle, position, Quaternion.Euler(rotation));
		ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
		if ((bool)component)
		{
			component.GetComponent<Renderer>().enabled = true;
			component.time = 0f;
			component.Clear(withChildren: true);
			component.Play(withChildren: true);
		}
		return gameObject;
	}

	public static GameObject CreateParticle(GameObject particle, Transform parent)
	{
		if (!particle)
		{
			return null;
		}
		GameObject gameObject = ObjectPoolManager.CreatePooled(particle, parent.position, Quaternion.identity);
		gameObject.transform.parent = parent;
		ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
		if ((bool)component)
		{
			component.GetComponent<Renderer>().enabled = true;
			component.time = 0f;
			component.Clear(withChildren: true);
			component.Play(withChildren: true);
		}
		return gameObject;
	}

	public static void DestroyParticle(GameObject particle)
	{
		if ((bool)particle)
		{
			ParticleSystem component = particle.GetComponent<ParticleSystem>();
			if ((bool)component)
			{
				component.Stop();
				component.time = 0f;
				component.Clear(withChildren: true);
				component.GetComponent<Renderer>().enabled = false;
			}
			ObjectPoolManager.DestroyPooled(particle);
		}
	}
}
