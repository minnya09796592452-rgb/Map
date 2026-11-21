using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beam;
using Beam.AccountServices;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.UI;
using Beam.Utilities;
using Funlabs;
using MEC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveManager
{
	private class ReplicateSavingStateMsg : MultiplayerMessage
	{
		[Replicate]
		public bool Saving;

		public override void OnPeer()
		{
			Game.Mode.IsMultiplayer();
			UpdateSavingSpinner(Saving);
		}
	}

	private class ReplicateSavedNotificationMsg : MultiplayerMessage
	{
		public override void OnPeer()
		{
			LocalizedNotification.Post(PlayerRegistry.LocalPlayer, NotificationPriority.Immediate, 4f, "BED_NTF_SAVED_DESC");
		}
	}

	public delegate void SaveCallback(int result);

	private class ReplicateSaveErrorDialogueMsg : MultiplayerMessage
	{
		[Replicate]
		public IPlayer InvokingPlayer;

		[Replicate]
		public int ResultCode;

		public override void OnPeer()
		{
			Game.Mode.IsClient();
			ReplicateSaveErrorDialogue(InvokingPlayer, ResultCode);
		}
	}

	private static bool _isSaving;

	public static bool IsSaving => _isSaving;

	public static event Action PreSave;

	public static event Action<Beam.Serialization.Json.JObject> BeforeWrite;

	public static event Action<GameMode> DeletedSaveFile;

	public static Beam.Serialization.Json.JObject GetPersistentData(Beam.Serialization.Json.JObject root)
	{
		if (root == null || root.IsNull())
		{
			Debug.LogError("SaveManager::LoadSaveFile:: Root was null or empty.");
			return null;
		}
		Beam.Serialization.Json.JObject field = root.GetField("Persistent");
		if (field != null && !field.IsNull())
		{
			return field;
		}
		Debug.LogError("SaveManager::LoadSaveFile:: [Persistent] node is null, empty or missing.");
		return null;
	}

	public static string GetSaveFolderPath()
	{
		return PathTools.Combine(FilePath.SAVE_FOLDER, FilePath.GetSlotFolderName(Options.GeneralSettings.LastSaveSlotUsed));
	}

	public static string GetSaveFilePath(GameMode mode)
	{
		return PathTools.Combine(GetSaveFolderPath(), "data" + FilePath.GetSaveFileExtension(mode));
	}

	public static bool TryGetSaveFilePath(GameMode mode, out string saveFilePath)
	{
		saveFilePath = string.Empty;
		if (!GAccountService.Get().Initialized)
		{
			Debug.LogError("SaveManager::CheckSaveFileExists:: DRM not initialized.");
			return false;
		}
		string saveFolderPath = GetSaveFolderPath();
		if (string.IsNullOrEmpty(saveFolderPath) || !Directory.Exists(saveFolderPath))
		{
			return false;
		}
		string saveFilePath2 = GetSaveFilePath(mode);
		if (string.IsNullOrEmpty(saveFilePath2))
		{
			return false;
		}
		saveFilePath = saveFilePath2;
		return File.Exists(saveFilePath);
	}

	public static bool CheckSaveFileVersion(GameMode mode)
	{
		try
		{
			Version saveFileVersion = GetSaveFileVersion(mode);
			if (saveFileVersion == null)
			{
				return false;
			}
			if (!ValidateVersionNumber(saveFileVersion))
			{
				Debug.LogError($"SaveManager::CheckSaveFileVersion:: {mode} save file is an older version.");
				return false;
			}
			Debug.Log($"SaveManager::CheckSaveFileVersion:: Valid {mode} save file version [" + saveFileVersion?.ToString() + "]");
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("SaveManager::CheckSaveFileVersion:: There was an error parsing save file version:\n" + ex.Message);
			return false;
		}
	}

	public static VersionStatus CompareSaveFileVersion(GameMode mode, Version otherVersion)
	{
		if (otherVersion == null)
		{
			return VersionStatus.Newer;
		}
		Version saveFileVersion = GetSaveFileVersion(mode);
		if (saveFileVersion == null)
		{
			return VersionStatus.Older;
		}
		return (VersionStatus)saveFileVersion.CompareTo(otherVersion);
	}

	public static Version GetSaveFileVersion(GameMode mode)
	{
		if (!GAccountService.Get().Initialized)
		{
			Debug.LogError("SaveManager::GetSaveFileVersion:: Steam not initialized.");
			return null;
		}
		if (!TryGetSaveFilePath(mode, out var saveFilePath))
		{
			Debug.Log($"SaveManager::GetSaveFileVersion:: {mode} save file does not exist.");
			return null;
		}
		try
		{
			using FileStream stream = new FileStream(saveFilePath, FileMode.Open, FileAccess.Read);
			using StreamReader streamReader = new StreamReader(stream);
			streamReader.Read();
			bool flag = streamReader.Peek() == 13;
			if (flag)
			{
				streamReader.Read();
			}
			bool flag2 = streamReader.Peek() == 10;
			if (flag2)
			{
				streamReader.Read();
			}
			string version;
			if (flag | flag2)
			{
				version = streamReader.ReadLine().Trim().Substring(12, 7);
			}
			else
			{
				char[] array = new char[20];
				streamReader.Read(array, 0, array.Length);
				version = new string(array).Substring(11, 7);
			}
			return new Version(version);
		}
		catch (Exception ex)
		{
			Debug.LogError("SaveManager::GetSaveFileVersion:: Error loading version data from save file:\n\n" + ex.Message);
			return null;
		}
	}

	public static bool ValidateVersionNumber(Version version)
	{
		VersionStatus versionStatus = (VersionStatus)version.CompareTo(BuildInformation.MinimumVersion);
		if (versionStatus != VersionStatus.Equal)
		{
			return versionStatus == VersionStatus.Newer;
		}
		return true;
	}

	public static bool ValidateSaveFile(GameMode mode)
	{
		if (CheckSaveFileVersion(mode))
		{
			return ValidateSaveFileFast(mode);
		}
		return false;
	}

	private static bool ValidateSaveFileFast(GameMode mode)
	{
		if (!TryGetSaveFilePath(mode, out var saveFilePath))
		{
			Debug.LogError("SaveManager:: Error loading save file. File does not exist.");
			return false;
		}
		try
		{
			using FileStream stream = new FileStream(saveFilePath, FileMode.Open, FileAccess.Read);
			using StreamReader reader = new StreamReader(stream);
			using JsonTextReader reader2 = new JsonTextReader(reader);
			JToken jToken = JToken.ReadFrom(reader2);
			if (jToken == null || !jToken.HasValues)
			{
				Debug.LogError("SaveManager:: Error loading save file. Data was null or empty.");
				return false;
			}
			JToken jToken2 = jToken.SelectToken("Persistent.StrandedWorld");
			if (jToken2.IsNullOrDestroyed() || !jToken2.HasValues)
			{
				Debug.LogError("SaveManager:: Error loading save file. Stranded world data was null or empty.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("SaveManager:: Error loading save file:\n\n" + ex);
			return false;
		}
	}

	public static Beam.Serialization.Json.JObject LoadSaveFile(GameMode mode)
	{
		if (!TryGetSaveFilePath(mode, out var saveFilePath))
		{
			Debug.LogError($"SaveManager::LoadSaveFile:: Error loading {mode} save file. File does not exist.");
			return null;
		}
		Beam.Serialization.Json.JObject jObject = null;
		using (FileStream stream = new FileStream(saveFilePath, FileMode.Open, FileAccess.Read))
		{
			using StreamReader streamReader = new StreamReader(stream);
			jObject = new Beam.Serialization.Json.JObject(streamReader.ReadToEnd());
		}
		if (jObject.IsNull())
		{
			Debug.LogError("SaveManager::LoadSaveFile:: Error loading save file. Data was null or empty.");
			return null;
		}
		Beam.Serialization.Json.JObject jObject2 = jObject.Get("Persistent/StrandedWorld");
		if (jObject2.IsNullOrDestroyed() || jObject2.IsNull())
		{
			Debug.LogError("SaveManager::LoadSaveFile:: Error loading save file. Stranded world data was null or empty.");
			return null;
		}
		Beam.Serialization.Json.JObject field = jObject.GetField("CustomSettings");
		if (field != null)
		{
			GameSettings gameSettings = new GameSettings();
			if (!SavingUtilities.Load(gameSettings, field))
			{
				Debug.LogError("SaveManager::Load:: Could not load [CustomSettings] data.");
			}
			Options.CustomSettings = gameSettings;
		}
		else
		{
			Debug.LogError("SaveManager::Load:: Old Save Detected. No [CustomSettings] data was found.");
		}
		return jObject;
	}

	public static JToken LoadSaveFileFast(string path)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
			using StreamReader reader = new StreamReader(stream);
			using JsonTextReader reader2 = new JsonTextReader(reader);
			JToken jToken = JToken.ReadFrom(reader2);
			if (jToken == null)
			{
				throw new Exception("Root data null.");
			}
			if (!jToken.HasValues)
			{
				throw new Exception("Root data empty.");
			}
			return jToken;
		}
		catch (Exception ex)
		{
			Debug.LogError("SaveManager:: Error loading save file:\n\n" + ex);
			return null;
		}
	}

	public static void BackupSaveFiles(string suffix = "")
	{
		if (string.IsNullOrEmpty(suffix))
		{
			suffix = "_BACKUP";
		}
		if (TryGetSaveFilePath(GameMode.SinglePlayer, out var saveFilePath))
		{
			string destFileName = saveFilePath + suffix;
			File.Copy(saveFilePath, destFileName, overwrite: true);
		}
		if (TryGetSaveFilePath(GameMode.Coop_2, out saveFilePath))
		{
			string destFileName2 = saveFilePath + suffix;
			File.Copy(saveFilePath, destFileName2, overwrite: true);
		}
		if (TryGetSaveFilePath(GameMode.Coop_Server, out saveFilePath))
		{
			string destFileName3 = saveFilePath + suffix;
			File.Copy(saveFilePath, destFileName3, overwrite: true);
		}
	}

	public static void DeleteSaveFile(GameMode mode)
	{
		if (TryGetSaveFilePath(mode, out var saveFilePath) && File.Exists(saveFilePath))
		{
			File.Delete(saveFilePath);
			DeletedSaveFile?.Invoke(mode);
		}
	}

	public static void DeleteSaveFiles()
	{
		DeleteSaveFile(GameMode.SinglePlayer);
		DeleteSaveFile(GameMode.Coop_2);
		DeleteSaveFile(GameMode.Coop_Server);
	}

	public static void SaveGame(IPlayer invokingPlayer)
	{
		if (_isSaving)
		{
			return;
		}
		_isSaving = true;
		MultiplayerMng.UpdateSessionToken_IsBusy(isBusy: true);
		ReplicateSavingState(saving: true);
		Save(invokingPlayer, (int result) =>
		{
			MultiplayerMng.UpdateSessionToken_IsBusy(isBusy: false);
			ReplicateSavingState(saving: false);
			if (result == 0)
			{
				LocalizedNotification.Post(PlayerRegistry.LocalPlayer, NotificationPriority.Immediate, 4f, "BED_NTF_SAVED_DESC");
				new ReplicateSavedNotificationMsg().Post();
			}
			else
			{
				ReplicateSaveErrorDialogue(invokingPlayer, result);
			}
			_isSaving = false;
		});
	}

	private static void ReplicateSavingState(bool saving)
	{
		Game.Mode.IsMultiplayer();
		UpdateSavingSpinner(saving);
		ReplicateSavingStateMsg replicateSavingStateMsg = new ReplicateSavingStateMsg();
		replicateSavingStateMsg.Saving = saving;
		replicateSavingStateMsg.Post();
	}

	private static void UpdateSavingSpinner(bool active)
	{
		if (active)
		{
			SpinnerWidget uIComponent = GameUI.GetUIComponent<SpinnerWidget>();
			if (uIComponent != null)
			{
				uIComponent.Show();
			}
		}
		else
		{
			SpinnerWidget uIComponent2 = GameUI.GetUIComponent<SpinnerWidget>();
			if (uIComponent2 != null)
			{
				uIComponent2.Hide();
			}
		}
	}

	private static CoroutineHandle Save(IPlayer player, SaveCallback callback)
	{
		return Timing.RunCoroutine(Save_Sequence(player, callback));
	}

	private static IEnumerator<float> Save_Sequence(IPlayer player, SaveCallback callback)
	{
		bool succeeded = true;
		Beam.Serialization.Json.JObject save = new Beam.Serialization.Json.JObject();
		Beam.Serialization.Json.JObject persistent = new Beam.Serialization.Json.JObject();
		yield return Timing.WaitUntilDone(Timing.Instance.RunCoroutine(GenerateSave_Sequence(save, persistent), (Exception ex) =>
		{
			if (ex != null)
			{
				succeeded = false;
				Debug.LogException(ex);
				callback(57005);
			}
		}));
		if (!succeeded)
		{
			yield break;
		}
		yield return Timing.WaitUntilDone(Timing.Instance.RunCoroutine(AfterSave_Sequence(player, save, callback), (Exception ex) =>
		{
			if (ex != null)
			{
				succeeded = false;
				Debug.LogException(ex);
				callback(48879);
			}
		}));
	}

	private static IEnumerator<float> AfterSave_Sequence(IPlayer player, Beam.Serialization.Json.JObject save, SaveCallback callback)
	{
		try
		{
			BeforeWrite?.Invoke(save);
		}
		catch (Exception)
		{
			Debug.Log("SaveManager::Save:: Errors were thrown in BeforeWrite event.");
			throw;
		}
		using (FileStream stream = new FileStream(GetSaveFilePath(Game.Mode), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
		{
			using StreamWriter streamWriter = new StreamWriter(stream);
			string value = save.ToString();
			streamWriter.Write(value);
		}
		Debug.LogError("Yield 4");
		yield return 0f;
		int result = 0;
		callback(result);
		Debug.LogError("Yield 5");
		yield return 0f;
		GC.Collect();
		Debug.LogError("Garbage collect finished");
	}

	public static IEnumerator<float> GenerateSave_Sequence(Beam.Serialization.Json.JObject save, Beam.Serialization.Json.JObject persistent)
	{
		try
		{
			PreSave?.Invoke();
		}
		catch (Exception)
		{
			Debug.Log("SaveManager::Save:: Errors were thrown in PreSave event.");
			throw;
		}
		PlayerRegistry.Save();
		try
		{
			PreSave?.Invoke();
		}
		catch (Exception ex2)
		{
			Debug.Log("SaveManager::Save:: Errors were thrown in PreSave event.\n\n" + ex2);
		}
		Beam.Serialization.Json.JObject data = PlayerRegistry.Save();
		persistent.AddField("PlayerRegistry", data);
		(from behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
			where behaviour is IPersistentSaveable
			orderby !(behaviour is StrandedWorld)
			select behaviour).Cast<IPersistentSaveable>().ToList().ForEach((IPersistentSaveable saveable) =>
		{
			Beam.Serialization.Json.JObject jObject2 = SavingUtilities.Save(saveable);
			if (jObject2 != null)
			{
				persistent.AddField(saveable.GetType().Name, jObject2);
			}
			else
			{
				Debug.LogError("SaveManager::Save:: Error saving a persistent object.\n\nName: " + saveable.GetType().Name);
			}
		});
		save.AddField("Version", BuildInformation.BuildVersion.ToPaddedString());
		save.AddField("Persistent", persistent);
		LevelLoader.Instance?.PatchServerClientCopy(save);
		Beam.Serialization.Json.JObject jObject = SavingUtilities.Save(Options.CustomSettings);
		if (jObject != null)
		{
			save.AddField("CustomSettings", jObject);
		}
		yield return 0f;
	}

	public static bool SaveToFile(string path, Beam.Serialization.Json.JObject rootData)
	{
		if (string.IsNullOrEmpty(path))
		{
			Debug.LogError("Path is null or empty!");
			return false;
		}
		if (rootData == null || rootData.IsNull())
		{
			Debug.LogError("Root data is null or empty!");
			return false;
		}
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(rootData.ToString());
			return true;
		}
		catch (Exception ex)
		{
			Debug.Log("SaveManager:: Error saving progress:\n\n" + ex);
		}
		return false;
	}

	public static void Save(GameMode mode, Beam.Serialization.Json.JObject save)
	{
		SaveToFile(GetSaveFilePath(mode), save);
	}

	public static bool ChangeCurrentSlot(int slotID)
	{
		Debug.Log("SaveManager:: Selecting slot " + slotID);
		int lastSaveSlotUsed = Options.GeneralSettings.LastSaveSlotUsed;
		Options.GeneralSettings.LastSaveSlotUsed = slotID;
		string slotFolderName = FilePath.GetSlotFolderName(slotID);
		FilePath.WORLD_FOLDER = PathTools.Combine(FilePath.SAVE_FOLDER, slotFolderName, "World");
		FilePath.HEIGHTMAPS_FOLDER = PathTools.Combine(FilePath.SAVE_FOLDER, slotFolderName, "Heightmaps");
		return lastSaveSlotUsed != slotID;
	}

	private static void ReplicateSaveErrorDialogue(IPlayer invokingPlayer, int resultCode)
	{
		if (invokingPlayer.Id == PlayerRegistry.LocalPlayer.Id)
		{
			UpdateSavingSpinner(active: false);
			int pId = invokingPlayer.Id;
			int prevMap = invokingPlayer.Input.GetCurrentMapId();
			invokingPlayer.Input.GetOtherPlayers().DisableMaps();
			invokingPlayer.Input.SetAndCacheMapsEnabled(state: false, prevMap);
			invokingPlayer.Input.SetAndCacheMapsEnabled(state: true, 16);
			invokingPlayer.Input.SetCursorEnabled(enabled: true);
			string prettyResultMsg = resultCode.GetPrettyResultMsg("SAVE_DB_ERROR_SAVING_DESC");
			DialogueBox.Show(PlayerRegistry.LocalPlayer.Input, "SAVE_DB_ERROR_TITLE", prettyResultMsg, DialogueButton.Ok, DialogueIcon.Warning).Accept += () =>
			{
				IPlayer playerFromId = PlayerUtilities.GetPlayerFromId(pId);
				playerFromId.Input.GetOtherPlayers().RestoreMapsEnabled();
				playerFromId.Input.SetAndCacheMapsEnabled(state: false, 16);
				playerFromId.Input.SetAndCacheMapsEnabled(state: true, prevMap);
				playerFromId.Input.SetCursorEnabled(enabled: false);
			};
		}
		else if (Game.Mode.IsServer())
		{
			ReplicateSaveErrorDialogueMsg replicateSaveErrorDialogueMsg = new ReplicateSaveErrorDialogueMsg();
			replicateSaveErrorDialogueMsg.InvokingPlayer = invokingPlayer;
			replicateSaveErrorDialogueMsg.ResultCode = resultCode;
			replicateSaveErrorDialogueMsg.Post();
		}
	}

	public static CoroutineHandle SaveInternal(IPlayer player, SaveCallback callback)
	{
		return Save(player, callback);
	}
}
