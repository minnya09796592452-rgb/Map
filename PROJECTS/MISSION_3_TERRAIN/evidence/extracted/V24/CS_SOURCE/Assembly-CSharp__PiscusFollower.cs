using System;
using Beam;
using Beam.Serialization.Json;
using Funlabs;
using UnityEngine;

public class PiscusFollower : Singleton<PiscusFollower>, IPersistentSaveable, ISaveable
{
	[Serializable]
	public class CreatureToSpawn
	{
		public GameObject CreatureObj;

		public float Rarity;
	}

	private class PlayerGridGroup
	{
		private int PlayerId;

		public SpawnGrid[,] SpawnGrids;

		public IPlayer Player => PlayerUtilities.GetPlayerFromId(PlayerId);

		public PlayerGridGroup(int playerId)
		{
			PlayerId = playerId;
		}
	}

	private class SpawnGrid
	{
		public Transform Grid;

		public PiscusManager piscusManager;

		public bool CanGenerate = true;

		public bool Generated;

		public SpawnGrid(Transform grid)
		{
			Grid = grid;
		}
	}

	private const int GRID_DIMENSION = 250;

	private const int GRID_SIZE = 3;

	private Vector3[] _loadedCenterGridPos = new Vector3[2];

	public CreatureToSpawn[] Creatures;

	private PlayerGridGroup[] _playerGridGroup;

	private bool _removedWildlifeSetting;

	private void Start()
	{
		if (!LevelLoader.IsLoading() && (bool)Singleton<StrandedWorld>.Instance)
		{
			Singleton<StrandedWorld>.Instance.WorldGenerated += CreateGrid;
		}
	}

	private void SetDifficultyParams()
	{
		_removedWildlifeSetting = PlayerUtilities.GetServerWildlifeMode() == EGameWildlifeMode.Removed;
	}

	protected override void OnDestroy()
	{
		if ((bool)Singleton<StrandedWorld>.Instance)
		{
			Singleton<StrandedWorld>.Instance.WorldGenerated -= CreateGrid;
		}
		base.OnDestroy();
	}

	private void CreateGrid()
	{
		if (Game.Mode.IsClient())
		{
			return;
		}
		SetDifficultyParams();
		int count = PlayerRegistry.AllPlayers.Count;
		_playerGridGroup = new PlayerGridGroup[count];
		for (int i = 0; i < PlayerRegistry.AllPlayers.Count; i++)
		{
			_ = PlayerRegistry.AllPlayers[i];
			_playerGridGroup[i] = new PlayerGridGroup(i);
		}
		for (int j = 0; j < _playerGridGroup.Length; j++)
		{
			PlayerGridGroup playerGridGroup = _playerGridGroup[j];
			playerGridGroup.SpawnGrids = new SpawnGrid[3, 3];
			Transform transform = new GameObject("PlayerGrid_" + j).transform;
			transform.transform.parent = base.transform;
			for (int k = 0; k < 3; k++)
			{
				for (int l = 0; l < 3; l++)
				{
					Transform transform2 = new GameObject("Spawn_Grid_" + k + "_" + l).transform;
					transform2.parent = transform;
					transform2.gameObject.layer = gameObject.layer;
					playerGridGroup.SpawnGrids[k, l] = new SpawnGrid(transform2);
					playerGridGroup.SpawnGrids[k, l].CanGenerate = true;
				}
			}
			Vector3 position = _loadedCenterGridPos[j];
			playerGridGroup.SpawnGrids[1, 1].Grid.position = position;
		}
		InvokeRepeating("MoveGrid", 0f, 1f);
		UpdateGridPositions();
	}

	private void MoveGrid()
	{
		if (LevelLoader.IsServerJoinInProgress)
		{
			return;
		}
		for (int i = 0; i < _playerGridGroup.Length; i++)
		{
			PlayerGridGroup playerGridGroup = _playerGridGroup[i];
			if (playerGridGroup.Player != null && playerGridGroup.SpawnGrids[2, 2] == null)
			{
				return;
			}
		}
		for (int j = 0; j < _playerGridGroup.Length; j++)
		{
			PlayerGridGroup playerGridGroup2 = _playerGridGroup[j];
			if (playerGridGroup2.Player == null)
			{
				continue;
			}
			Vector3 position = playerGridGroup2.Player.transform.position;
			SpawnGrid spawnGrid = null;
			int num = 0;
			int num2 = 0;
			for (int k = 0; k < 3; k++)
			{
				for (int l = 0; l < 3; l++)
				{
					Vector3 position2 = playerGridGroup2.SpawnGrids[k, l].Grid.position;
					if (position.x >= position2.x - 125f && position.x <= position2.x + 125f && position.z >= position2.z - 125f && position.z <= position2.z + 125f)
					{
						spawnGrid = playerGridGroup2.SpawnGrids[k, l];
						num = 1 - k;
						num2 = 1 - l;
						break;
					}
				}
			}
			if (spawnGrid == playerGridGroup2.SpawnGrids[1, 1])
			{
				continue;
			}
			SpawnGrid[,] array = new SpawnGrid[3, 3];
			for (int m = 0; m < 3; m++)
			{
				for (int n = 0; n < 3; n++)
				{
					int num3 = m + num;
					int num4 = n + num2;
					if (num3 < 0 || num3 > 2 || num4 < 0 || num4 > 2)
					{
						if ((bool)playerGridGroup2.SpawnGrids[m, n].piscusManager)
						{
							UnityEngine.Object.Destroy(playerGridGroup2.SpawnGrids[m, n].piscusManager.gameObject);
						}
						playerGridGroup2.SpawnGrids[m, n].CanGenerate = true;
						playerGridGroup2.SpawnGrids[m, n].Generated = false;
					}
					if (num3 < 0)
					{
						num3 = 2;
					}
					else if (num3 > 2)
					{
						num3 = 0;
					}
					if (num4 < 0)
					{
						num4 = 2;
					}
					else if (num4 > 2)
					{
						num4 = 0;
					}
					array[num3, num4] = playerGridGroup2.SpawnGrids[m, n];
					playerGridGroup2.SpawnGrids[m, n].Grid.gameObject.name = "Spawn_Grid_" + num3 + "_" + num4;
				}
			}
			playerGridGroup2.SpawnGrids = array;
			UpdateGridPositions();
		}
	}

	private void UpdateGridPositions()
	{
		for (int i = 0; i < _playerGridGroup.Length; i++)
		{
			PlayerGridGroup playerGridGroup = _playerGridGroup[i];
			if (playerGridGroup.Player == null)
			{
				continue;
			}
			playerGridGroup.SpawnGrids[0, 0].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x - 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z + 250f);
			playerGridGroup.SpawnGrids[0, 1].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x - 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z);
			playerGridGroup.SpawnGrids[0, 2].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x - 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z - 250f);
			playerGridGroup.SpawnGrids[1, 0].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z + 250f);
			playerGridGroup.SpawnGrids[1, 2].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z - 250f);
			playerGridGroup.SpawnGrids[2, 0].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x + 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z + 250f);
			playerGridGroup.SpawnGrids[2, 1].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x + 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z);
			playerGridGroup.SpawnGrids[2, 2].Grid.position = new Vector3(playerGridGroup.SpawnGrids[1, 1].Grid.position.x + 250f, transform.position.y, playerGridGroup.SpawnGrids[1, 1].Grid.position.z - 250f);
			if (_removedWildlifeSetting)
			{
				break;
			}
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					SpawnGrid spawnGrid = playerGridGroup.SpawnGrids[j, k];
					if (spawnGrid.CanGenerate)
					{
						CreateGridObjects(spawnGrid);
					}
				}
			}
		}
	}

	private void CreateGridObjects(SpawnGrid grid)
	{
		Zone[] zones = Singleton<StrandedWorld>.Instance.Zones;
		foreach (Zone zone in zones)
		{
			if ((grid.Grid.position - zone.transform.position).magnitude < 150f || grid.Grid.position.x > 2500f || grid.Grid.position.x < -2500f || grid.Grid.position.z > 2500f || grid.Grid.position.z < -2500f)
			{
				return;
			}
		}
		for (int j = 0; j < _playerGridGroup.Length; j++)
		{
			PlayerGridGroup playerGridGroup = _playerGridGroup[j];
			if (playerGridGroup.Player == null)
			{
				continue;
			}
			for (int k = 0; k < 3; k++)
			{
				for (int l = 0; l < 3; l++)
				{
					if ((grid.Grid.position - playerGridGroup.SpawnGrids[k, l].Grid.transform.position).magnitude < 10f && playerGridGroup.SpawnGrids[k, l].Generated)
					{
						return;
					}
				}
			}
		}
		grid.CanGenerate = false;
		grid.Generated = true;
		GameObject gameObject = ChooseCreature();
		if (!(gameObject == null))
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, grid.Grid.position, Quaternion.identity);
			grid.piscusManager = gameObject2.GetComponent<PiscusManager>();
			grid.piscusManager.SpawnDistance = 150f;
			gameObject2.transform.parent = grid.Grid.transform;
			grid.piscusManager.transform.position = grid.Grid.position;
		}
	}

	private GameObject ChooseCreature()
	{
		float num = 0f;
		CreatureToSpawn[] creatures = Creatures;
		foreach (CreatureToSpawn creatureToSpawn in creatures)
		{
			num += creatureToSpawn.Rarity;
		}
		float num2 = UnityEngine.Random.value * num;
		for (int j = 0; j < Creatures.Length; j++)
		{
			if (num2 < Creatures[j].Rarity)
			{
				return Creatures[j].CreatureObj;
			}
			num2 -= Creatures[j].Rarity;
		}
		return null;
	}

	public JObject Save()
	{
		JObject jObject = new JObject();
		for (int i = 0; i < _playerGridGroup.Length; i++)
		{
			PlayerGridGroup playerGridGroup = _playerGridGroup[i];
			jObject.AddField("CenterGridPos_" + i, JSerializer.Serialize(playerGridGroup.SpawnGrids[1, 1].Grid.position));
		}
		return jObject;
	}

	public void Load(JObject data)
	{
		int playersCount = Game.Mode.GetPlayersCount();
		_loadedCenterGridPos = new Vector3[playersCount];
		if (data != null && !data.IsNull())
		{
			JObject field = data.GetField("CenterGridPos");
			if (field != null)
			{
				Vector3 vector = JSerializer.Deserialize<Vector3>(field);
				for (int i = 0; i < playersCount; i++)
				{
					_loadedCenterGridPos[i] = vector;
				}
			}
			for (int j = 0; j < playersCount; j++)
			{
				JObject field2 = data.GetField("CenterGridPos_" + j);
				if (field2 != null)
				{
					_loadedCenterGridPos[j] = JSerializer.Deserialize<Vector3>(field2);
				}
			}
		}
		if ((bool)Singleton<StrandedWorld>.Instance)
		{
			Singleton<StrandedWorld>.Instance.WorldGenerated += CreateGrid;
		}
		CreateGrid();
	}
}
