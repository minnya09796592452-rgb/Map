using System;
using System.Collections.Generic;
using Beam;
using Beam.Rendering;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

public class Module : SaveablePrefab
{
	[Serializable]
	public class Spawn
	{
		public string Name;

		public Transform Position;

		public GameObject Obj;

		public float SpawnChance;
	}

	public string[] Tags;

	public GameObject FortLadder;

	private bool _seaLadderAccess;

	[SerializeField]
	private List<Spawn> _objectSpawn;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private LodController _lodController;

	private bool _customMap;

	public List<Spawn> ObjectSpawn => _objectSpawn;

	public ModuleConnector[] GetExits()
	{
		return GetComponentsInChildren<ModuleConnector>();
	}

	public void Start()
	{
		if (!LevelLoader.IsLoading())
		{
			Initialize();
		}
	}

	private void Initialize()
	{
		if (_objectSpawn.Count > 0)
		{
			SpawnObjects();
		}
	}

	public override void OnCustomMapLoaded()
	{
		base.OnCustomMapLoaded();
		_customMap = true;
		Initialize();
	}

	private void SetupSpawnedObject(SaveablePrefab saveable)
	{
		if (!saveable.IsValid())
		{
			return;
		}
		InteractiveObject component = saveable.GetComponent<InteractiveObject>();
		if ((bool)component)
		{
			component.transform.parent = transform.parent;
			component.TransformParent = transform.parent;
			if (_customMap)
			{
				component.OnCustomMapLoaded();
			}
		}
	}

	private void SpawnObject(GameObject objToSpawn, Vector3 pos, Quaternion rot)
	{
		if (objToSpawn.TryGetComponent<ISaveablePrefab>(out var component))
		{
			uint prefabId = component.PrefabId;
			MiniGuid referenceId = MiniGuid.NewFrom(pos, prefabId, 48879);
			if (Game.Mode.IsMaster() || !component.IsMultiplayerEntity)
			{
				SaveablePrefab saveable = MultiplayerMng.Instantiate<SaveablePrefab>(prefabId, pos, rot, referenceId);
				SetupSpawnedObject(saveable);
			}
			else
			{
				MultiplayerZoneSync.Instance.LinkData(MultiplayerZoneSync.Usage.Module, referenceId, (MultiplayerZoneSync.Command cmd) =>
				{
					SetupSpawnedObject(cmd.Instance);
				});
			}
		}
		else
		{
			UnityEngine.Object.Instantiate(objToSpawn, pos, rot);
		}
	}

	private void SpawnObjects()
	{
		System.Random random = Singleton<StrandedWorld>.Instance.GenerateLocalSeed(transform.position);
		foreach (Spawn item in _objectSpawn)
		{
			if ((float)(random.Next(1000) / 10) <= item.SpawnChance)
			{
				SpawnObject(item.Obj, item.Position.position, item.Position.rotation);
			}
		}
	}

	public void SetUpSupport(bool seaLadderAccess = false)
	{
		if (!(FortLadder == null))
		{
			_seaLadderAccess = seaLadderAccess;
			FortLadder.SetActive(seaLadderAccess);
		}
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		JObject data = JSerializer.Serialize(transform);
		jObject.AddField("Transform", data);
		jObject.AddField("hasLadder", JSerializer.Serialize(_seaLadderAccess));
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		if (data != null && !data.IsNull())
		{
			JSerializer.DeserializeInto(data.GetField("Transform"), transform);
			CompleteTransformSerialization();
			bool value = data.GetField("hasLadder").GetValue<bool>();
			_seaLadderAccess = value;
		}
		SetUpSupport(_seaLadderAccess);
	}
}
