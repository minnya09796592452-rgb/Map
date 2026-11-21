using System;
using System.Collections.Generic;
using System.Linq;
using Beam;
using UnityEngine;

public class BEAMSeaFortGenerator : MonoBehaviour, IGenerationObject
{
	[SerializeField]
	private Transform startingPoint;

	public Module[] Modules;

	public Module[] StartModule;

	private static System.Random _randomSeed;

	[SerializeField]
	private int minIteration = 1;

	[SerializeField]
	private int maxIteration = 4;

	private void Start()
	{
		_randomSeed = Singleton<StrandedWorld>.Instance.GenerateLocalSeed(transform.position);
		int num = _randomSeed.Next(minIteration, maxIteration);
		Module module = UnityEngine.Object.Instantiate(StartModule[_randomSeed.Next(StartModule.Length)], startingPoint.position, transform.rotation);
		module.transform.parent = transform.parent;
		module.SetUpSupport(seaLadderAccess: true);
		List<ModuleConnector> list = new List<ModuleConnector>(module.GetExits());
		for (int i = 0; i < num; i++)
		{
			List<ModuleConnector> list2 = new List<ModuleConnector>();
			foreach (ModuleConnector item in list)
			{
				string random = GetRandom(item.Tags);
				Module module2 = UnityEngine.Object.Instantiate(GetRandomWithTag(Modules, random));
				module2.transform.parent = transform.parent;
				bool upSupport = _randomSeed.NextDouble() < 0.20000000298023224;
				module2.SetUpSupport(upSupport);
				ModuleConnector[] exits = module2.GetExits();
				ModuleConnector exitToMatch = exits.FirstOrDefault((ModuleConnector x) => x.IsDefault) ?? GetRandom(exits);
				MatchExits(item, exitToMatch);
				list2.AddRange(exits.Where((ModuleConnector e) => e != exitToMatch));
			}
			list = list2;
		}
	}

	private void MatchExits(ModuleConnector oldExit, ModuleConnector newExit)
	{
		Transform parent = newExit.transform.parent.parent;
		parent.RotateAround(angle: Azimuth(-oldExit.transform.forward) - Azimuth(newExit.transform.forward), point: newExit.transform.position, axis: Vector3.up);
		Vector3 vector = oldExit.transform.position - newExit.transform.position;
		parent.transform.position += vector;
	}

	private static TItem GetRandom<TItem>(TItem[] array)
	{
		return array[_randomSeed.Next(0, array.Length)];
	}

	private static Module GetRandomWithTag(IEnumerable<Module> modules, string tagToMatch)
	{
		return GetRandom(modules.Where((Module m) => m.Tags.Contains(tagToMatch)).ToArray());
	}

	private static float Azimuth(Vector3 vector)
	{
		return Vector3.Angle(Vector3.forward, vector) * Mathf.Sign(vector.x);
	}
}
