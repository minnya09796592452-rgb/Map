using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beam.AccountServices;
using Beam.Crafting;
using Beam.Serialization.Json;
using Beam.Terrain;
using Beam.Utilities;
using Newtonsoft.Json.Linq;
using Rewired;
using UnityEngine;

namespace Beam;

public static class LegacyUtilities
{
	private const string LEGACY_SUFFIX = "_LEGACY";

	public static void LoadPlayer(Beam.Serialization.Json.JObject persistentData)
	{
		if (persistentData.GetField("CraftingCombinations") != null)
		{
			LoadPre_64_04_Player(persistentData);
		}
		else
		{
			PlayerRegistry.Load(persistentData.GetField("PlayerRegistry"));
		}
	}

	private static void LoadPre_64_04_Player(Beam.Serialization.Json.JObject persistentData)
	{
		Beam.Serialization.Json.JObject data = persistentData["CraftingCombinations"]["Combinations"];
		Beam.Serialization.Json.JObject jObject = new Beam.Serialization.Json.JObject();
		jObject.AddField("Combinations", data);
		Beam.Serialization.Json.JObject field = persistentData.GetField("PlayerRegistry");
		foreach (Beam.Serialization.Json.JObject child in field.Children)
		{
			child.AddField("Crafter", jObject);
		}
		UnityEngine.Debug.Log("LegacyUtilities:: Loading pre-0.64.04 player data...");
		PlayerRegistry.Load(field);
	}

	public static bool LoadStrandedWorldPlayerPosition_PreCoop(Beam.Serialization.Json.JObject data, out Vector3 position)
	{
		position = Vector3.zero;
		Beam.Serialization.Json.JObject field = data.GetField("PlayerPosition");
		if (field != null)
		{
			position = JSerializer.Deserialize<Vector3>(field);
			return true;
		}
		return false;
	}

	public static bool LoadStrandedWorldPlayerZoneId_PreCoop(JToken data, out string zoneId)
	{
		zoneId = string.Empty;
		JToken jToken = data["PlayerZoneId"];
		if (jToken != null)
		{
			zoneId = jToken.ToObject<string>();
			return true;
		}
		return false;
	}

	public static void LoadCompletedMissionGyroPieces(CraftingCombinations craftingCombinations)
	{
		CraftingCombination craftingCombination = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "EEL TROPHY");
		if (craftingCombination != null && craftingCombination.Unlocked)
		{
			CraftingCombination craftingCombination2 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "GYROCOPTER FRAME");
			if (craftingCombination2 != null && !craftingCombination2.Unlocked)
			{
				craftingCombination2.Unlocked = true;
			}
			CraftingCombination craftingCombination3 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "AIRCRAFT ENGINE PART");
			if (craftingCombination3 != null && !craftingCombination3.Unlocked)
			{
				craftingCombination3.Unlocked = true;
			}
		}
		CraftingCombination craftingCombination4 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "SHARK TROPHY");
		if (craftingCombination4 != null && craftingCombination4.Unlocked)
		{
			CraftingCombination craftingCombination5 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "GYROCOPTER MOTOR");
			if (craftingCombination5 != null && !craftingCombination5.Unlocked)
			{
				craftingCombination5.Unlocked = true;
			}
			CraftingCombination craftingCombination6 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "AIRCRAFT RUDDER PART");
			if (craftingCombination6 != null && !craftingCombination6.Unlocked)
			{
				craftingCombination6.Unlocked = true;
			}
		}
		CraftingCombination craftingCombination7 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "SQUID TROPHY");
		if (craftingCombination7 != null && craftingCombination7.Unlocked)
		{
			CraftingCombination craftingCombination8 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "GYROCOPTER ROTORS");
			if (craftingCombination8 != null && !craftingCombination8.Unlocked)
			{
				craftingCombination8.Unlocked = true;
			}
			CraftingCombination craftingCombination9 = craftingCombinations.Combinations.FirstOrDefault((CraftingCombination combo) => combo.Name == "AIRCRAFT PROPELLER PART");
			if (craftingCombination9 != null && !craftingCombination9.Unlocked)
			{
				craftingCombination9.Unlocked = true;
			}
		}
	}

	private static bool IsLegacySaveFile()
	{
		if (SaveManager.CompareSaveFileVersion(GameMode.SinglePlayer, new Version(0, 71, 0)) == VersionStatus.Newer || SaveManager.CompareSaveFileVersion(GameMode.Coop_2, new Version(0, 71, 0)) == VersionStatus.Newer || SaveManager.CompareSaveFileVersion(GameMode.Coop_Server, new Version(0, 75, 0)) == VersionStatus.Newer)
		{
			return false;
		}
		return true;
	}

	private static bool IsLegacyWorld()
	{
		if (!Directory.Exists(FilePath.WORLD_FOLDER))
		{
			return false;
		}
		IEnumerable<string> enumerable = from x in Directory.GetDirectories(FilePath.WORLD_FOLDER)
			select Path.GetFileName(x) into x
			where !string.IsNullOrEmpty(x)
			select x;
		if (enumerable == null || enumerable.Count() == 0)
		{
			return false;
		}
		return enumerable.Contains("0_0");
	}

	private static void UseLegacyBackups()
	{
		string saveFilePath = SaveManager.GetSaveFilePath(GameMode.SinglePlayer);
		if (File.Exists(saveFilePath + "_LEGACY"))
		{
			File.Move(saveFilePath, saveFilePath + "_LEGACY2");
			File.Move(saveFilePath + "_LEGACY", saveFilePath);
			UnityEngine.Debug.Log("Using legacy save backup for 1P upgrade...");
		}
		saveFilePath = SaveManager.GetSaveFilePath(GameMode.Coop_2);
		if (File.Exists(saveFilePath + "_LEGACY"))
		{
			File.Move(saveFilePath, saveFilePath + "_LEGACY2");
			File.Move(saveFilePath + "_LEGACY", saveFilePath);
			UnityEngine.Debug.Log("Using legacy save backup for 2P upgrade...");
		}
		saveFilePath = SaveManager.GetSaveFilePath(GameMode.Coop_Server);
		if (File.Exists(saveFilePath + "_LEGACY"))
		{
			File.Move(saveFilePath, saveFilePath + "_LEGACY2");
			File.Move(saveFilePath + "_LEGACY", saveFilePath);
			UnityEngine.Debug.Log("Using legacy save backup for MP upgrade...");
		}
	}

	public static void UpgradeLegacyGame()
	{
		if (IsLegacySaveFile() && IsLegacyWorld())
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			UpgradeLegacyWorld();
			UpgradeLegacySaveFiles();
			GC.Collect();
			UnityEngine.Debug.Log($"Upgraded world in: {stopwatch.ElapsedMilliseconds}ms");
		}
	}

	private static void UpgradeLegacyWorld()
	{
		UnityEngine.Debug.Log("LegacyUtilities::UpgradeLegacyWorld()");
		World.UpgradeLegacyWorld(out var _);
	}

	private static void UpgradeLegacySaveFiles()
	{
		UseLegacyBackups();
		UnityEngine.Debug.Log("LegacyUtilities::UpgradeLegacySaveFiles()");
		if (SaveManager.CheckSaveFileVersion(GameMode.SinglePlayer) || SaveManager.CheckSaveFileVersion(GameMode.Coop_2) || SaveManager.CheckSaveFileVersion(GameMode.Coop_Server))
		{
			SaveManager.BackupSaveFiles("_LEGACY");
			UpgradeSaveFile(GameMode.SinglePlayer);
			UpgradeSaveFile(GameMode.Coop_2);
			UpgradeSaveFile(GameMode.Coop_Server);
		}
	}

	private static void UpgradeSaveFile(GameMode mode)
	{
		UnityEngine.Debug.Log($"LegacyUtilities::UpgradeSaveFile:: Upgrading {mode} save file...");
		if (!SaveManager.CheckSaveFileVersion(mode))
		{
			return;
		}
		Beam.Serialization.Json.JObject jObject = SaveManager.LoadSaveFile(mode);
		if (jObject == null || jObject.IsNull())
		{
			UnityEngine.Debug.LogError("Root data null. Could not upgrade..");
			return;
		}
		Beam.Serialization.Json.JObject persistentData = SaveManager.GetPersistentData(jObject);
		if (persistentData == null || persistentData.IsNull())
		{
			UnityEngine.Debug.LogError("Persistent data null. Could not upgrade..");
			return;
		}
		Beam.Serialization.Json.JObject field = persistentData.GetField("StrandedWorld");
		if (field == null || field.IsNull())
		{
			UnityEngine.Debug.LogError("StrandedWorld data null. Could not upgrade..");
			return;
		}
		Beam.Serialization.Json.JObject field2 = field.GetField("Zones");
		if (field2 == null || field2.IsNull())
		{
			UnityEngine.Debug.LogError("Zone data null. Could not upgrade..");
			return;
		}
		Beam.Serialization.Json.JObject jObject2 = new Beam.Serialization.Json.JObject();
		int i = 0;
		int num = 0;
		for (; i < 7; i++)
		{
			int num2 = 0;
			while (num2 < 7)
			{
				Map map = World.MapList[num];
				Beam.Serialization.Json.JObject jObject3 = new Beam.Serialization.Json.JObject();
				jObject3.AddField("Name", map.EditorData.Name);
				jObject3.AddField("Id", map.EditorData.Id);
				jObject3.AddField("Version", map.EditorData.VersionNumber);
				jObject2.Add(jObject3);
				num2++;
				num++;
			}
		}
		int num3 = 1;
		int num4 = 1;
		int j = 0;
		int num5 = 0;
		for (; j < 5; j++)
		{
			int num6 = 0;
			while (num6 < 5)
			{
				Beam.Serialization.Json.JObject value = field2.Children[num5];
				int num7 = j + num3;
				int num8 = num6 + num4;
				jObject2.Children[num8 * 7 + num7] = value;
				num6++;
				num5++;
			}
		}
		FixRigidbodyWorldPositions_Pre_72_00(jObject2);
		field["Zones"] = jObject2;
		Beam.Serialization.Json.JObject field3 = field.GetField("PlayersData");
		Vector2[] array = new Vector2[2];
		Vector3[] array2 = new Vector3[field3.Children.Count];
		for (int k = 0; k < field3.Children.Count; k++)
		{
			Beam.Serialization.Json.JObject jObject4 = field3.Children[k];
			string zoneId = jObject4.GetField("ZoneId").GetValue<string>();
			int num9 = World.MapList.FindIndex((Map map2) => map2.EditorData.Id == zoneId);
			if (num9 != -1)
			{
				array[k] = World.GenerationZonePositons[num9];
				Vector3 vector = ConvertLegacyWorldPositionToLocal(JSerializer.Deserialize<Vector3>(jObject4.GetField("Position")), World.GenerationZonePositons[num9]);
				array2[k] = vector;
				Beam.Serialization.Json.JObject value2 = JSerializer.Serialize(array2[k]);
				jObject4["Position"] = value2;
			}
		}
		Beam.Serialization.Json.JObject field4 = persistentData.GetField("PlayerRegistry");
		for (int num10 = 0; num10 < field4.Children.Count; num10++)
		{
			Beam.Serialization.Json.JObject jObject5 = field4.Children[num10];
			Beam.Serialization.Json.JObject value3 = JSerializer.Serialize(array2[num10]);
			jObject5["Movement"]["Transform"]["localPosition"] = value3;
		}
		FixPiscusFollowerWorldPositions(persistentData, array);
		Beam.Serialization.Json.JObject jObject6 = new Beam.Serialization.Json.JObject();
		jObject6.AddField("Version", JSerializer.Serialize(BuildInformation.BuildVersion.ToPaddedString()));
		jObject6.AddField("Persistent", persistentData);
		Beam.Serialization.Json.JObject field5 = jObject.GetField("CustomSettings");
		if (field5 != null)
		{
			jObject6.AddField("CustomSettings", field5);
		}
		Beam.Serialization.Json.JObject field6 = jObject.GetField("ScreenCapture");
		if (field6 != null)
		{
			jObject6.AddField("ScreenCapture", field6);
		}
		SaveManager.SaveToFile(SaveManager.GetSaveFilePath(mode), jObject6);
	}

	private static void FixPiscusFollowerWorldPositions(Beam.Serialization.Json.JObject persistentData, Vector2[] newPositions)
	{
		Beam.Serialization.Json.JObject field = persistentData.GetField("PiscusFollower");
		if (field != null)
		{
			Beam.Serialization.Json.JObject jObject = new Beam.Serialization.Json.JObject();
			for (int i = 0; i < field.Children.Count; i++)
			{
				_ = field.Children[i];
				Vector3 zero = Vector3.zero;
				zero.x = newPositions[i].x;
				zero.z = newPositions[i].y;
				jObject.AddField("CenterGridPos_" + i, JSerializer.Serialize(zero));
			}
			persistentData["PiscusFollower"] = jObject;
		}
	}

	private static void FixRigidbodyWorldPositions_Pre_72_00(Beam.Serialization.Json.JObject zonesData)
	{
		foreach (Beam.Serialization.Json.JObject child in zonesData.Children)
		{
			string id = child.GetField("Id").GetValue<string>();
			int num = World.MapList.FindIndex((Map map) => map.EditorData.Id == id);
			if (num != -1)
			{
				Beam.Serialization.Json.JObject field = child.GetField("Objects");
				if (field != null && !field.IsNull())
				{
					RecursiveFixRigidbodyWorldPositions(field, World.GenerationZonePositons[num]);
				}
			}
		}
	}

	private static void RecursiveFixRigidbodyWorldPositions(Beam.Serialization.Json.JObject rootData, Vector3 newOrigin)
	{
		foreach (Beam.Serialization.Json.JObject child in rootData.Children)
		{
			if (child.IsNull())
			{
				continue;
			}
			Beam.Serialization.Json.JObject field = child.GetField("Rigidbody");
			if (field != null)
			{
				FixRigidbodyWorldPosition(field, newOrigin);
			}
			Beam.Serialization.Json.JObject field2 = child.GetField("Ragdolls");
			if (field2 != null && !field2.IsNull())
			{
				foreach (Beam.Serialization.Json.JObject child2 in field2.Children)
				{
					FixRigidbodyWorldPosition(child2, newOrigin);
				}
			}
			RecursiveFixRigidbodyWorldPositions(child, newOrigin);
		}
	}

	private static void FixRigidbodyWorldPosition(Beam.Serialization.Json.JObject rigidbodyData, Vector3 newOrigin)
	{
		if (rigidbodyData == null || rigidbodyData.IsNull())
		{
			UnityEngine.Debug.LogError("FixRigidbodyWorldPosition:: Rigidbody data is null or empty.");
			return;
		}
		Vector3 vector = ConvertLegacyWorldPositionToLocal(rigidbodyData.GetField("position"), newOrigin);
		rigidbodyData["position"] = JSerializer.Serialize(vector);
	}

	public static Vector3 ConvertLegacyWorldPositionToLocal(Beam.Serialization.Json.JObject worldPositionData, Vector2 localOrigin)
	{
		return ConvertLegacyWorldPositionToLocal(JSerializer.Deserialize<Vector3>(worldPositionData), localOrigin);
	}

	public static Vector3 ConvertLegacyWorldPositionToLocal(Vector3 worldPosition, Vector2 localOrigin)
	{
		Vector3 vector = worldPosition;
		vector.x -= 315f;
		vector.z -= 315f;
		float num = BeamMath.Frac(vector.x / 630f);
		float num2 = BeamMath.Frac(vector.z / 630f);
		float num3 = num * 630f;
		float num4 = num2 * 630f;
		float num5 = (float)Math.Sign(vector.x) * 315f;
		float num6 = (float)Math.Sign(vector.z) * 315f;
		num3 -= num5;
		num4 -= num6;
		Vector3 zero = Vector3.zero;
		zero.x = localOrigin.x + num3;
		zero.z = localOrigin.y + num4;
		zero.y = vector.y;
		return zero;
	}

	public static bool ResetInput_Pre_65_05()
	{
		Version fileVersion = Options.GetFileVersion();
		if (fileVersion == null)
		{
			return false;
		}
		VersionStatus versionStatus = (VersionStatus)fileVersion.CompareTo(new Version(0, 65, 5));
		if (versionStatus == VersionStatus.Equal || versionStatus == VersionStatus.Newer)
		{
			return false;
		}
		Options.Load();
		Rewired.Player player = ReInput.players.GetPlayer(0);
		player.controllers.maps.LoadDefaultMaps(ControllerType.Mouse);
		player.controllers.maps.LoadDefaultMaps(ControllerType.Keyboard);
		player.controllers.maps.LoadDefaultMaps(ControllerType.Joystick);
		Options.InputSettings = new InputSettings();
		Options.Save();
		UnityEngine.Debug.Log("LegacyUtilities:: Reset input for pre-0.65.05 settings.");
		return true;
	}

	public static bool HasSaveFilePre_74_00(GameMode mode, out string filePath)
	{
		filePath = string.Empty;
		string userId = GAccountService.Get().LocalUser.UserInfo.UserId;
		if (string.IsNullOrWhiteSpace(userId))
		{
			return false;
		}
		string text = PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, userId);
		if (!Directory.Exists(text))
		{
			return false;
		}
		string text2 = PathTools.Combine(text, userId + FilePath.GetSaveFileExtension(mode));
		if (!File.Exists(text2))
		{
			return false;
		}
		filePath = text2;
		return true;
	}

	public static void MigrateSaveDataPre_74_00()
	{
		string filePath = string.Empty;
		string filePath2 = string.Empty;
		if ((0u | (HasSaveFilePre_74_00(GameMode.SinglePlayer, out filePath) ? 1u : 0u) | (HasSaveFilePre_74_00(GameMode.Coop_2, out filePath2) ? 1u : 0u)) != 0)
		{
			string text = PathTools.Combine(FilePath.SAVE_FOLDER, FilePath.GetSlotFolderName(0));
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string destFileName = PathTools.Combine(text, "data" + FilePath.GetSaveFileExtension(GameMode.SinglePlayer));
			string destFileName2 = PathTools.Combine(text, "data" + FilePath.GetSaveFileExtension(GameMode.Coop_2));
			if (File.Exists(filePath))
			{
				File.Copy(filePath, destFileName, overwrite: true);
			}
			if (File.Exists(filePath2))
			{
				File.Copy(filePath2, destFileName2, overwrite: true);
			}
			BeamTeam.CopyDirectory(PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Editor/World"), PathTools.Combine(text, "World"));
			BeamTeam.CopyDirectory(PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Editor/Heightmaps"), PathTools.Combine(text, "Heightmaps"));
			BeamTeam.CopyDirectory(PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Editor/Maps"), FilePath.LOCAL_MAPS_FOLDER);
			string text2 = PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Options.json");
			if (File.Exists(text2))
			{
				File.Copy(text2, FilePath.OPTIONS_FILE, overwrite: true);
			}
			string text3 = PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Editor/Screenshots/dynamic.bytes");
			if (File.Exists(text3))
			{
				File.Copy(text3, PathTools.Combine(text, "dynamic.bytes"), overwrite: true);
			}
			string text4 = PathTools.Combine(FilePath.LEGACY_SAVE_FOLDER, "Editor/Screenshots/dynamic_COOP.bytes");
			if (File.Exists(text4))
			{
				File.Copy(text4, PathTools.Combine(text, "dynamic_COOP.bytes"), overwrite: true);
			}
		}
	}
}
