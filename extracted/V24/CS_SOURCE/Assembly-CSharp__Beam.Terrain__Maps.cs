using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam.Terrain;

public static class Maps
{
	public const string MAP_EXTENSION = ".map";

	public const string MAP_PREFIX = "MAP_";

	public const string MAP_EDITOR_SUFFIX = "_EDITOR";

	public const string MAP_OBJECT_SUFFIX = "_OBJECT";

	public const string MAP_HEIGHT_SUFFIX = "_HEIGHT";

	public const string MAP_INTERNAL_ID = "INTERNAL";

	public const string MAP_PROCEDURAL_ID = "INTERNAL_PROCEDURAL";

	public const string MAP_MISSION_ID = "INTERNAL_MISSION";

	private static List<Map> _defaultMapList;

	private static List<Map> _remoteMaps;

	private static List<Map> _userMapList;

	public static IList<Map> DefaultMapList => _defaultMapList;

	public static IEnumerable<Map> RemoteMaps => _remoteMaps;

	public static IEnumerable<Map> UserMapList => _userMapList;

	public static IEnumerable<Map> AllMapList => _defaultMapList.Concat(_userMapList).Concat(_remoteMaps);

	static Maps()
	{
		_defaultMapList = new List<Map>();
		_remoteMaps = new List<Map>();
		_userMapList = new List<Map>();
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
	}

	private static LoadingResult CreateMapFolders()
	{
		try
		{
			if (!Directory.Exists(FilePath.LOCAL_MAPS_FOLDER))
			{
				Directory.CreateDirectory(FilePath.LOCAL_MAPS_FOLDER);
			}
		}
		catch (Exception ex)
		{
			return new LoadingResult
			{
				Message = "MAP_MSG_MAP_DIR_NOT_FOUND " + ex.Message,
				Succeeded = false
			};
		}
		return new LoadingResult
		{
			Succeeded = true
		};
	}

	public static bool ValidateMapFolder(string mapPath, string mapName)
	{
		if (File.Exists(PathTools.Combine(mapPath, mapName + "_EDITOR.map")) && File.Exists(PathTools.Combine(mapPath, mapName + "_OBJECT.map")))
		{
			return File.Exists(PathTools.Combine(mapPath, mapName + "_HEIGHT.map"));
		}
		return false;
	}

	public static string GetMapFolderName(Map map)
	{
		return "MAP_" + MapTools.FormatNameForSave(map.EditorData.Id);
	}

	public static void SaveMap(Map map, string directoryPath)
	{
		string mapFolderName = GetMapFolderName(map);
		string text = PathTools.Combine(directoryPath, mapFolderName);
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		using FileStream streamEditor = new FileStream(PathTools.Combine(text, mapFolderName + "_EDITOR.map"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		using FileStream streamObject = new FileStream(PathTools.Combine(text, mapFolderName + "_OBJECT.map"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		using FileStream streamHeight = new FileStream(PathTools.Combine(text, mapFolderName + "_HEIGHT.map"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		map.Save(streamEditor, streamObject, streamHeight);
	}

	public static void Save(this Map map, Stream streamEditor, Stream streamObject, Stream streamHeight)
	{
		if (!MPExtensions.IsCompressStableValuesInitialized())
		{
			MPExtensions.InitializeCompressStableValues();
		}
		try
		{
			new BinaryFormatter().Serialize(streamEditor, map.EditorData);
		}
		catch (Exception ex)
		{
			Debug.LogError("Map::SaveMap:: Error saving map editor data: " + ex.Message);
			throw;
		}
		try
		{
			using StreamWriter streamWriter = new StreamWriter(streamObject);
			streamWriter.Write(map.ObjectData.ToString());
		}
		catch (Exception ex2)
		{
			Debug.LogError("Map::SaveMap:: Error saving map object data: " + ex2.Message);
			throw;
		}
		try
		{
			using BinaryWriter binaryWriter = new BinaryWriter(streamHeight);
			for (int i = 0; i < 257; i++)
			{
				for (int j = 0; j < 257; j++)
				{
					binaryWriter.Write(map.HeightmapData[i, j].Compress());
				}
			}
		}
		catch (Exception ex3)
		{
			Debug.LogError("Map::SaveMap:: Error saving map heightmap data: " + ex3.Message);
			throw;
		}
	}

	public static Map LoadMap(string mapPath, string mapName, bool checkVersion = false)
	{
		using FileStream editorStream = new FileStream(PathTools.Combine(mapPath, mapName + "_EDITOR.map"), FileMode.Open, FileAccess.Read);
		using FileStream objectStream = new FileStream(PathTools.Combine(mapPath, mapName + "_OBJECT.map"), FileMode.Open, FileAccess.Read);
		using FileStream heightStream = new FileStream(PathTools.Combine(mapPath, mapName + "_HEIGHT.map"), FileMode.Open, FileAccess.Read);
		Map map = new Map();
		map.Load(editorStream, objectStream, heightStream, checkVersion);
		return map;
	}

	public static void CreateTextures(this Map map)
	{
		if (!(map?.EditorData?.LargePreviewImage != null))
		{
			Texture2D texture2D = new Texture2D(1, 1);
			texture2D.LoadImage(map.EditorData.LargePreviewImageBytes);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			map.EditorData.LargePreviewImage = texture2D;
			map.EditorData.Valid = true;
			if (map.EditorData.Biome == Zone.BiomeType.CARRIER)
			{
				map.EditorData.SmallPreviewImage = Resources.Load("Icons/WorldMap_Icon_Carrier") as Texture2D;
			}
			else if (map.EditorData.Biome == Zone.BiomeType.MISSION)
			{
				map.EditorData.SmallPreviewImage = Resources.Load("Icons/WorldMap_Icon_Mission") as Texture2D;
			}
			else
			{
				map.EditorData.SmallPreviewImage = MapTools.CreateHeightmapTexture(map.HeightmapData);
			}
		}
	}

	public static void Load(this Map map, Stream editorStream, Stream objectStream, Stream heightStream, bool checkVersion, bool createTextures = true)
	{
		if (!MPExtensions.IsCompressStableValuesInitialized())
		{
			MPExtensions.InitializeCompressStableValues();
		}
		try
		{
			MapEditorData editorData = (MapEditorData)new BinaryFormatter().Deserialize(editorStream);
			map.EditorData = editorData;
		}
		catch (Exception ex)
		{
			Debug.LogError("Map::LoadMap:: Error loading map editor data:\n\n" + ex.Message + "\n" + ex.StackTrace);
			throw;
		}
		if (checkVersion)
		{
			try
			{
				if (!SaveManager.ValidateVersionNumber(new Version(map.EditorData.GameVersionNumber)))
				{
					throw new VersionException("Map [" + map.EditorData.Name + "] is out of date and no longer supported.");
				}
			}
			catch (ArgumentNullException)
			{
				throw new VersionException("Map [" + map.EditorData.Name + "] is out of date and no longer supported.");
			}
		}
		try
		{
			using StreamReader streamReader = new StreamReader(objectStream);
			try
			{
				JObject jObject = new JObject(streamReader.ReadToEnd());
				PatchObjectsReferenceId(jObject);
				map.ObjectData = jObject;
			}
			catch (JSerializerException ex3)
			{
				Debug.LogError(ex3.Message);
				throw;
			}
		}
		catch (Exception ex4)
		{
			Debug.LogError("Map::LoadMap:: Error loading map [" + map.EditorData.Name + "] object data:\n\n" + ex4.Message + "\n" + ex4.StackTrace);
			throw;
		}
		try
		{
			using BinaryReader binaryReader = new BinaryReader(heightStream);
			binaryReader.BaseStream.Seek(0L, SeekOrigin.Begin);
			float[,] array = new float[257, 257];
			for (int i = 0; i < 257; i++)
			{
				for (int j = 0; j < 257; j++)
				{
					ushort value = binaryReader.ReadUInt16().ToCompressStable();
					array[i, j] = value.Decompress();
				}
			}
			map.HeightmapData = array;
		}
		catch (Exception ex5)
		{
			Debug.LogError("Map::LoadMap:: Error loading map heightmap data:\n\n" + ex5.Message + "\n" + ex5.StackTrace);
			throw;
		}
		if (createTextures)
		{
			map.CreateTextures();
		}
	}

	private static void PatchObjectsReferenceId(JObject objects)
	{
		if (objects == null || objects.Children == null)
		{
			return;
		}
		foreach (JObject child in objects.Children)
		{
			if (child != null)
			{
				uint prefabId = Prefabs.GetPrefabId(child);
				if (prefabId != 0 && Prefabs.GetReferenceId(child, safe: false).IsDefault())
				{
					Prefabs.SetReferenceId(child, MiniGuid.NewFrom(Prefabs.GetPosition(child), prefabId, 48879));
				}
			}
		}
	}

	public static IEnumerable<Map> LoadDefaultMaps(out LoadingResult result)
	{
		IList<Map> list = LoadMaps(Directory.GetDirectories(FilePath.DEFAULT_MAPS_FOLDER), out result);
		if (list != null && list.Count > 0)
		{
			_defaultMapList.Clear();
			_defaultMapList.AddRange(list);
		}
		return _defaultMapList;
	}

	public static IEnumerable<Map> LoadLocalMaps(out LoadingResult result)
	{
		_userMapList.Clear();
		result = CreateMapFolders();
		if (!result.Succeeded)
		{
			return _userMapList;
		}
		IList<Map> list = LoadMaps(Directory.GetDirectories(FilePath.LOCAL_MAPS_FOLDER), out result);
		if (list != null && list.Count > 0)
		{
			_userMapList.AddRange(list);
		}
		return _userMapList;
	}

	public static IEnumerable<Map> LoadRemoteMaps(IEnumerable<string> paths, out LoadingResult result)
	{
		IList<Map> list = LoadMaps(paths, out result);
		if (list != null && list.Count > 0)
		{
			_remoteMaps.Clear();
			foreach (Map item in list)
			{
				item.EditorData.Source = MapEditorData.EMapSource.Community;
				_remoteMaps.Add(item);
			}
		}
		return _remoteMaps;
	}

	private static IList<Map> LoadMaps(IEnumerable<string> paths, out LoadingResult result)
	{
		result = new LoadingResult
		{
			Succeeded = true
		};
		if (paths == null)
		{
			Debug.LogError("Map paths were null.");
			return null;
		}
		if (paths.Count() == 0)
		{
			Debug.LogWarning("Map paths were empty.");
			return null;
		}
		List<Map> list = new List<Map>();
		try
		{
			foreach (string path in paths)
			{
				string fileName = Path.GetFileName(path);
				if (ValidateMapFolder(path, fileName))
				{
					try
					{
						Map item = LoadMap(path, fileName, checkVersion: true);
						list.Add(item);
					}
					catch (Exception ex)
					{
						LoadingResult loadingResult = result;
						loadingResult.Message = loadingResult.Message + "MAP_MSG_ERROR_LOADING_MAP " + path + "\n\n" + ex.Message;
						result.Succeeded = false;
						Debug.LogError("Map::LoadMaps:: " + ex.Message);
					}
				}
				else
				{
					LoadingResult obj = result;
					obj.Message = obj.Message + "MAP_MSG_ERROR_LOADING_MAP_FILES_NOT_FOUND " + path + "\n ";
					result.Succeeded = false;
					Debug.LogError("Error loading map. Not all files were found: " + path);
				}
			}
		}
		catch (Exception ex2)
		{
			result.Message = "MAP_MSG_ERROR_LOADING_MAPS " + ex2.Message;
			result.Succeeded = false;
			Debug.LogError("Map::LoadMaps:: " + ex2.Message);
		}
		return list;
	}

	public static void DeleteUserMap(Map map)
	{
		if (map != null)
		{
			_userMapList.Remove(map);
			DeleteMap(map);
		}
	}

	public static void DeleteMap(Map map)
	{
		if (map != null)
		{
			string text = PathTools.Combine(FilePath.LOCAL_MAPS_FOLDER, GetMapFolderName(map));
			DeleteMapFolder(new string[1] { text });
		}
	}

	public static void DeleteMapFolder(string[] folder)
	{
		try
		{
			for (int i = 0; i < folder.Length; i++)
			{
				Directory.Delete(folder[i], recursive: true);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Map::DeleteMapFolder:: Error deleting map folder: " + ex.Message);
		}
	}

	private static void DeleteFilesInFolder(string folder)
	{
		try
		{
			string[] files = Directory.GetFiles(folder);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Map::DeleteFilesInFolder:: Error deleting files in folder: " + ex.Message);
		}
	}
}
