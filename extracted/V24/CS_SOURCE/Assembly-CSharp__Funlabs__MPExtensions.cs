using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Beam;
using Beam.Serialization.Json;
using Beam.Terrain;
using Beam.Utilities;
using Photon.Bolt.Utils;
using UdpKit;
using UdpKit.Platform;
using UnityEngine;

namespace Funlabs;

public static class MPExtensions
{
	public static char[] _sessionCodeBase = null;

	private static ushort[] _toCompressStable = new ushort[65536];

	public static void SetTransformCloseTo(this IPlayer player, IPlayer other)
	{
		player.transform.position = other.transform.position + other.transform.right * 0.7f;
		player.transform.rotation = other.transform.rotation;
	}

	public static MultiplayerTokens.Session GetToken(this UdpSession session)
	{
		if (session.Source != UdpSessionSource.Photon)
		{
			return null;
		}
		try
		{
			if (!(session.GetProtocolToken() is MultiplayerTokens.Session session2) || session2.Version != BuildInformation.MultiplayerVersion || session2.Platform != MultiplayerTokens.Session.GetPlatform())
			{
				return null;
			}
			return session2;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string ToStringSafe(this object o)
	{
		if (!o.IsNullOrDestroyed())
		{
			return o.ToString();
		}
		return "null";
	}

	public static string GetDisplayNameSafe(this IDisplayableStorable b)
	{
		if (!b.IsNullOrDestroyed())
		{
			return b.GetDisplayName();
		}
		return "null";
	}

	public static bool IsMultiplayer(this GameMode mode)
	{
		if (mode != GameMode.Coop_Server)
		{
			return mode == GameMode.Coop_Client;
		}
		return true;
	}

	public static bool IsClient(this GameMode mode)
	{
		return mode == GameMode.Coop_Client;
	}

	public static bool IsMaster(this GameMode mode)
	{
		return !mode.IsClient();
	}

	public static bool IsServer(this GameMode mode)
	{
		return mode == GameMode.Coop_Server;
	}

	public static bool IsCoop(this GameMode mode)
	{
		return mode != GameMode.SinglePlayer;
	}

	public static bool IsLocalCoop(this GameMode mode)
	{
		if ((uint)(mode - 1) <= 2u)
		{
			return true;
		}
		return false;
	}

	public static bool IsSinglePlayer(this GameMode mode)
	{
		return mode == GameMode.SinglePlayer;
	}

	public static int GetPlayersCount(this GameMode mode)
	{
		switch (mode)
		{
		case GameMode.Coop_2:
		case GameMode.Coop_Server:
		case GameMode.Coop_Client:
			return 2;
		case GameMode.Coop_3:
			return 3;
		case GameMode.Coop_4:
			return 4;
		default:
			return 1;
		}
	}

	public static int GetLocalPlayersCount(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Coop_2 => 2, 
			GameMode.Coop_3 => 3, 
			GameMode.Coop_4 => 4, 
			_ => 1, 
		};
	}

	public static bool IsGame(this GameState state)
	{
		if (state != GameState.NEW_GAME)
		{
			return state == GameState.LOAD_GAME;
		}
		return true;
	}

	public static bool IsMenu(this GameState state)
	{
		if (state != GameState.MAIN_MENU)
		{
			return state == GameState.MAP_EDITOR;
		}
		return true;
	}

	public static TransformId GetTransfromId(this Transform transform)
	{
		if (transform.IsNullOrDestroyed())
		{
			return TransformId.Null;
		}
		if (transform == MultiplayerMng.ReceivedEntities)
		{
			return TransformId.ReceivedEntities;
		}
		if (transform == Beam.Singleton<StrandedWorld>.Instance?.NmlZone.SaveContainer)
		{
			return TransformId.SaveContainer;
		}
		if (transform == Beam.Singleton<PiscusInspectionManager>.Instance?.transform)
		{
			return TransformId.PiscusManager;
		}
		if (transform == FlockController.Instance?.ActiveContainer)
		{
			return TransformId.FlockControllerActive;
		}
		if (transform == FlockController.Instance?.DeactivatedContainer)
		{
			return TransformId.FlockControllerDeactivated;
		}
		if (!Beam.Singleton<StrandedWorld>.Instance.IsNullOrDestroyed() && !Beam.Singleton<StrandedWorld>.Instance.Zones.IsNullOrDestroyed())
		{
			for (byte b = 0; b < Beam.Singleton<StrandedWorld>.Instance.Zones.Length; b++)
			{
				if (transform == Beam.Singleton<StrandedWorld>.Instance.Zones[b].SaveContainer)
				{
					return (TransformId)(7 + b);
				}
			}
		}
		return TransformId.Unknown;
	}

	public static Transform GetTransfrom(this TransformId containerId)
	{
		return containerId switch
		{
			TransformId.Unknown => null, 
			TransformId.Null => null, 
			TransformId.ReceivedEntities => MultiplayerMng.ReceivedEntities, 
			TransformId.SaveContainer => Beam.Singleton<StrandedWorld>.Instance.NmlZone.SaveContainer, 
			TransformId.PiscusManager => Beam.Singleton<PiscusInspectionManager>.Instance.transform, 
			TransformId.FlockControllerActive => FlockController.Instance.ActiveContainer, 
			TransformId.FlockControllerDeactivated => FlockController.Instance.DeactivatedContainer, 
			_ => Beam.Singleton<StrandedWorld>.Instance.Zones[(int)(containerId - 7)].SaveContainer, 
		};
	}

	public static bool IsValid(this TransformId transformId)
	{
		return transformId != TransformId.Unknown;
	}

	public static bool IsUnknown(this TransformId transformId)
	{
		return transformId == TransformId.Unknown;
	}

	public static string GetPath(this Transform transform)
	{
		if (transform.IsNullOrDestroyed())
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(transform.gameObject.name);
		Transform parent = transform.parent;
		while (!parent.IsNullOrDestroyed())
		{
			stringBuilder.Insert(0, "/");
			stringBuilder.Insert(0, parent.gameObject.name);
			parent = parent.parent;
		}
		return stringBuilder.ToString();
	}

	public static Quaternion ExtractRotation(this Matrix4x4 matrix)
	{
		Vector3 forward = default;
		forward.x = matrix.m02;
		forward.y = matrix.m12;
		forward.z = matrix.m22;
		Vector3 upwards = default;
		upwards.x = matrix.m01;
		upwards.y = matrix.m11;
		upwards.z = matrix.m21;
		return Quaternion.LookRotation(forward, upwards);
	}

	public static Vector3 ExtractPosition(this Matrix4x4 matrix)
	{
		Vector3 result = default;
		result.x = matrix.m03;
		result.y = matrix.m13;
		result.z = matrix.m23;
		return result;
	}

	public static Vector3 ExtractScale(this Matrix4x4 matrix)
	{
		Vector3 result = default;
		result.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
		result.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
		result.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
		return result;
	}

	public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
	{
		transform.localScale = matrix.ExtractScale();
		transform.rotation = matrix.ExtractRotation();
		transform.position = matrix.ExtractPosition();
	}

	public static int Range(this System.Random random, int min, int max)
	{
		double num = random.NextDouble();
		return min + (int)num * (max - min);
	}

	public static Rect Inflate(this Rect rect, Vector2 value)
	{
		return new Rect(rect.x - value.x, rect.y - value.y, rect.width + 2f * value.x, rect.height + 2f * value.y);
	}

	public static byte[] ToArray(this JObject data, byte[] header = null)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true);
		if (header != null && header.Length != 0)
		{
			gZipStream.Write(header, 0, header.Length);
		}
		using (StreamWriter streamWriter = new StreamWriter(gZipStream))
		{
			streamWriter.Write(data.ToString());
		}
		return memoryStream.ToArray();
	}

	public static JObject ToJObject(this byte[] data, byte[] header = null)
	{
		using MemoryStream stream = new MemoryStream(data);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
		if (header != null && header.Length != 0)
		{
			gZipStream.Read(header, 0, header.Length);
		}
		using StreamReader streamReader = new StreamReader(gZipStream);
		return new JObject(streamReader.ReadToEnd());
	}

	public static void Write(this BinaryWriter bw, Vector2 p)
	{
		bw.Write(p.x);
		bw.Write(p.y);
	}

	public static Vector2 ReadVector2(this BinaryReader br)
	{
		return new Vector2(br.ReadSingle(), br.ReadSingle());
	}

	public static byte[] ToArray(this World.Bundle world, byte[] header = null)
	{
		using MemoryStream memoryStream = new MemoryStream();
		world.Save(memoryStream, header);
		return memoryStream.ToArray();
	}

	public static World.Bundle ToWorld(this byte[] data, bool createTextures, byte[] header = null)
	{
		World.Bundle result = default;
		using MemoryStream stream = new MemoryStream(data);
		result.Load(stream, createTextures, header);
		return result;
	}

	public static byte[] ToArray(this Map[] maps, byte[] header = null)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
		{
			if (header != null && header.Length != 0)
			{
				gZipStream.Write(header, 0, header.Length);
			}
			using BinaryWriter binaryWriter = new BinaryWriter(gZipStream);
			for (int i = 0; i < 7; i++)
			{
				Map map = maps[i];
				using MemoryStream memoryStream2 = new MemoryStream();
				using MemoryStream memoryStream3 = new MemoryStream();
				using MemoryStream memoryStream4 = new MemoryStream();
				map.Save(memoryStream2, memoryStream3, memoryStream4);
				byte[] array = memoryStream2.ToArray();
				byte[] array2 = memoryStream3.ToArray();
				byte[] array3 = memoryStream4.ToArray();
				binaryWriter.Write(array.Length);
				binaryWriter.Write(array2.Length);
				binaryWriter.Write(array3.Length);
				binaryWriter.Write(array);
				binaryWriter.Write(array2);
				binaryWriter.Write(array3);
			}
		}
		return memoryStream.ToArray();
	}

	public static Map[] ToMaps(this byte[] data, bool createTextures, byte[] header = null)
	{
		Map[] array = new Map[7];
		using MemoryStream stream = new MemoryStream(data);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
		if (header != null && header.Length != 0)
		{
			gZipStream.Read(header, 0, header.Length);
		}
		using BinaryReader binaryReader = new BinaryReader(gZipStream);
		for (int i = 0; i < 7; i++)
		{
			Map map = new Map();
			int num = binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			int num3 = binaryReader.ReadInt32();
			byte[] buffer = new byte[num];
			byte[] buffer2 = new byte[num2];
			byte[] buffer3 = new byte[num3];
			binaryReader.Read(buffer, 0, num);
			binaryReader.Read(buffer2, 0, num2);
			binaryReader.Read(buffer3, 0, num3);
			using (MemoryStream editorStream = new MemoryStream(buffer, 0, num))
			{
				using MemoryStream objectStream = new MemoryStream(buffer2, 0, num2);
				using MemoryStream heightStream = new MemoryStream(buffer3, 0, num3);
				map.Load(editorStream, objectStream, heightStream, checkVersion: false, createTextures);
			}
			array[i] = map;
		}
		return array;
	}

	public static void CopyToClipboard(this string value)
	{
		GUIUtility.systemCopyBuffer = value;
	}

	public static int RegionIndexToCloudRegionEnumIndex(this int regionIndex)
	{
		int num = regionIndex;
		if (num > 3)
		{
			num--;
		}
		if (num > 12)
		{
			num--;
		}
		return num;
	}

	public static int CloudRegionEnumIndexToRegionIndex(this int regionIndex)
	{
		int num = regionIndex;
		if (num > 3)
		{
			num++;
		}
		if (num > 12)
		{
			num++;
		}
		return num;
	}

	private static void CreateSessionCodeBase()
	{
		if (_sessionCodeBase.IsNullOrDestroyed())
		{
			_sessionCodeBase = "0123456789qwertyuiopasdfghjklzxcvbnm".ToCharArray();
		}
	}

	public static string ToCode(this string value, int realRegionIndex)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "null";
		}
		CreateSessionCodeBase();
		int num = Mathf.Abs(value.GetHashCode());
		string text = string.Empty;
		int num2 = _sessionCodeBase.Length;
		do
		{
			text = _sessionCodeBase[num % num2] + text;
			num /= num2;
		}
		while (num > 0);
		return _sessionCodeBase[realRegionIndex] + text;
	}

	public static int GetRegionIndex(this string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return -1;
		}
		CreateSessionCodeBase();
		char firstChar = code.ToLower().ToCharArray().FirstOrDefault_NonAlloc();
		return Array.FindIndex(_sessionCodeBase, (char c) => c == firstChar);
	}

	public static int GetNodeIndex(this string node)
	{
		if (string.IsNullOrEmpty(node))
		{
			return -1;
		}
		if (node[0] != '#')
		{
			return -1;
		}
		if (!int.TryParse(node.Substring(1), NumberStyles.Number, Beam.Serialization.Json.ExtensionMethods.EnglishCulture, out var result))
		{
			return -1;
		}
		return result;
	}

	public static JObject GetZoneData(this JObject zonesData, string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return zonesData?.Children?.FirstOrDefault_NonAlloc((JObject data, int arg) =>
		{
			JObject field = data.GetField("Id");
			return string.Equals((field != null) ? field.GetValue<string>() : null, id);
		}, 0);
	}

	public static JObject GetNode(this JObject data, string node)
	{
		if (data.IsNullOrDestroyed())
		{
			return null;
		}
		switch (data.Type)
		{
		case JObject.EToken.ARRAY:
		{
			int nodeIndex = node.GetNodeIndex();
			if (data.Children.IsNullOrDestroyed() || nodeIndex < 0 || nodeIndex >= data.Children.Count)
			{
				return null;
			}
			return data.Children[nodeIndex];
		}
		case JObject.EToken.OBJECT:
			return data[node];
		default:
			return null;
		}
	}

	public static JObject Get(this JObject data, string[] nodes, int numNodes = -1)
	{
		if (data.IsNullOrDestroyed())
		{
			return null;
		}
		if (-1 == numNodes)
		{
			numNodes = nodes.Length;
		}
		for (int i = 0; i < numNodes; i++)
		{
			data = data.GetNode(nodes[i]);
			if (data.IsNullOrDestroyed())
			{
				return null;
			}
		}
		return data;
	}

	public static JObject Get(this JObject data, string path)
	{
		if (data.IsNullOrDestroyed())
		{
			return null;
		}
		string[] nodes = path.Split('/');
		return data.Get(nodes);
	}

	public static JObject Set(this JObject data, string path, JObject field)
	{
		if (data.IsNullOrDestroyed() || field.IsNullOrDestroyed())
		{
			return data;
		}
		string[] array = path.Split('/');
		JObject jObject = data;
		for (int i = 0; i < array.Length - 1; i++)
		{
			string text = array[i];
			int nodeIndex = text.GetNodeIndex();
			if (nodeIndex != -1)
			{
				if (jObject.Type != JObject.EToken.NULL && jObject.Type != JObject.EToken.ARRAY)
				{
					return data;
				}
				int num = jObject.Children?.Count ?? 0;
				int num2 = nodeIndex + 1 - num;
				for (int j = 0; j < num2; j++)
				{
					jObject.Add(new JObject());
				}
				jObject = jObject.Children[nodeIndex];
			}
			else
			{
				JObject jObject2 = jObject[text];
				if (jObject2.IsNullOrDestroyed())
				{
					jObject.AddField(text, jObject2 = new JObject());
				}
				jObject = jObject2;
			}
		}
		string text2 = array[^1];
		int nodeIndex2 = text2.GetNodeIndex();
		if (nodeIndex2 != -1)
		{
			if (jObject.Type != JObject.EToken.NULL && jObject.Type != JObject.EToken.ARRAY)
			{
				return data;
			}
			int num3 = jObject.Children?.Count ?? 0;
			int num4 = nodeIndex2 + 1 - num3;
			for (int k = 0; k < num4; k++)
			{
				jObject.Add(new JObject());
			}
			jObject.Children[nodeIndex2] = field;
		}
		else
		{
			jObject.SetField(text2, field);
		}
		return data;
	}

	public static JObject Clear(this JObject data, string path)
	{
		if (data.IsNullOrDestroyed())
		{
			return data;
		}
		string[] array = path.Split('/');
		JObject jObject = data.Get(array, array.Length - 1);
		if (jObject.IsNullOrDestroyed())
		{
			return data;
		}
		string text = array[^1];
		int nodeIndex = text.GetNodeIndex();
		if (nodeIndex != -1)
		{
			if (nodeIndex >= (jObject.Children?.Count ?? 0))
			{
				return data;
			}
			jObject.Children[nodeIndex] = new JObject();
		}
		else
		{
			nodeIndex = jObject.Keys?.IndexOf(text) ?? (-1);
			if (nodeIndex == -1)
			{
				return data;
			}
			jObject.Keys.RemoveAt(nodeIndex);
			jObject.Children.RemoveAt(nodeIndex);
		}
		return data;
	}

	public static JObject Clear(this JObject data, string[] paths)
	{
		foreach (string path in paths)
		{
			data.Clear(path);
		}
		return data;
	}

	public static JObject CopyFrom(this JObject dst, JObject src, string[] srcPaths, string[] dstPaths)
	{
		if (src == null || dst == null)
		{
			return dst;
		}
		int num = Mathf.Min(srcPaths.Length, dstPaths.Length);
		for (int i = 0; i < num; i++)
		{
			dst.Set(dstPaths[i], src.Get(srcPaths[i]));
		}
		return dst;
	}

	public static JObject ApplyPatch(this JObject data, JObject patch)
	{
		if (data.IsNullOrDestroyed() || patch.IsNullOrDestroyed())
		{
			return data;
		}
		if (data.Type != patch.Type && data.Type != JObject.EToken.NULL)
		{
			return data;
		}
		switch (patch.Type)
		{
		case JObject.EToken.OBJECT:
		{
			if (patch.Keys.IsNullOrDestroyed() || patch.Children.IsNullOrDestroyed())
			{
				return data;
			}
			int count2 = patch.Children.Count;
			for (int k = 0; k < count2; k++)
			{
				JObject jObject = data[patch.Keys[k]];
				if (jObject.IsNullOrDestroyed())
				{
					data.AddField(patch.Keys[k], patch.Children[k]);
				}
				else
				{
					jObject.ApplyPatch(patch.Children[k]);
				}
			}
			break;
		}
		case JObject.EToken.ARRAY:
		{
			if (patch.Children.IsNullOrDestroyed())
			{
				return data;
			}
			int count = patch.Children.Count;
			int num = data.Children?.Count ?? 0;
			int num2 = count - num;
			for (int i = 0; i < num2; i++)
			{
				data.Add(new JObject());
			}
			for (int j = 0; j < count; j++)
			{
				if (patch.Children[j].Type != JObject.EToken.NULL)
				{
					if (data.Children[j].Type == JObject.EToken.NULL)
					{
						data.Children[j] = patch.Children[j];
					}
					else
					{
						data.Children[j].ApplyPatch(patch.Children[j]);
					}
				}
			}
			break;
		}
		case JObject.EToken.NULL:
			return data;
		default:
			data.Data = patch.Data;
			break;
		}
		return data;
	}

	public static JObject MakeNewRefenceIds(this JObject data)
	{
		if (data == null)
		{
			return data;
		}
		if (data.Type == JObject.EToken.STRING)
		{
			if (MiniGuid.IsMiniGuid(data.GetValue<string>()))
			{
				data.Data = MiniGuid.New().ToString();
			}
			return data;
		}
		if (data.Children == null)
		{
			return data;
		}
		int count = data.Children.Count;
		for (int i = 0; i < count; i++)
		{
			data.Children[i].MakeNewRefenceIds();
		}
		return data;
	}

	public static string[] RemoveEmpty(this string[] tokens)
	{
		int num = 0;
		string[] array = tokens;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Length != 0)
			{
				num++;
			}
		}
		string[] array2 = new string[num];
		num = 0;
		array = tokens;
		foreach (string text in array)
		{
			if (text.Length != 0)
			{
				array2[num++] = text;
			}
		}
		return array2;
	}

	public static PhotonPlatformConfig Copy(this PhotonPlatformConfig config)
	{
		return new PhotonPlatformConfig
		{
			Region = config.Region,
			UsePunchThrough = config.UsePunchThrough,
			AppId = config.AppId,
			AuthenticationValues = config.AuthenticationValues,
			ConnectionProtocol = config.ConnectionProtocol,
			SerializationProtocol = config.SerializationProtocol,
			ForceExternalEndPoint = config.ForceExternalEndPoint,
			RoomCreateTimeout = config.RoomCreateTimeout,
			RoomJoinTimeout = config.RoomJoinTimeout
		};
	}

	public static ushort Compress(this float value)
	{
		if ((double)value > 1.0)
		{
			value = 1f;
		}
		if ((double)value < 0.0)
		{
			value = 0f;
		}
		return (ushort)(value * 65535f);
	}

	public static float Decompress(this ushort value)
	{
		return (float)(int)value / 65535f;
	}

	public static bool IsCompressStable(this ushort value)
	{
		return value == value.Decompress().Compress();
	}

	private static ushort GetCompressStable(this ushort value)
	{
		if (value.IsCompressStable())
		{
			return value;
		}
		int num = 1;
		while (true)
		{
			if (value - num >= 0 && ((ushort)(value - num)).IsCompressStable())
			{
				return (ushort)(value - num);
			}
			if (value + num <= 65535 && ((ushort)(value + num)).IsCompressStable())
			{
				break;
			}
			num++;
		}
		return (ushort)(value + num);
	}

	public static ushort ToCompressStable(this ushort value)
	{
		return _toCompressStable[value];
	}

	public static void InitializeCompressStableValues()
	{
		for (int i = 0; i <= 65535; i++)
		{
			_toCompressStable[i] = ((ushort)i).GetCompressStable();
		}
	}

	public static bool IsCompressStableValuesInitialized()
	{
		return _toCompressStable[32768] != 0;
	}

	public static T GetInParent<T>(this Transform transform) where T : ISaveablePrefab
	{
		if (transform.IsNullOrDestroyed())
		{
			return default;
		}
		T component = transform.GetComponent<T>();
		if (!component.IsNullOrDestroyed() && component.IsValidPrefab)
		{
			return component;
		}
		return transform.parent.GetInParent<T>();
	}

	public static bool IsReplicable(this Rigidbody component)
	{
		if (null == component)
		{
			return true;
		}
		return !component.transform.GetInParent<ISaveablePrefab>().IsNullOrDestroyed();
	}

	public static bool IsReplicable(this Collider component)
	{
		if (null == component)
		{
			return true;
		}
		if (component is TerrainCollider)
		{
			return component.transform.GetComponentInParent<Zone>() != null;
		}
		return !component.transform.GetInParent<ISaveablePrefab>().IsNullOrDestroyed();
	}

	public static T GetFirstComponentInParent<T>(this Transform transform) where T : Component
	{
		if (null == transform)
		{
			return null;
		}
		T component = transform.GetComponent<T>();
		if (component != null)
		{
			return component;
		}
		return transform.parent.GetComponentInParent<T>();
	}

	public static Vector3 ReadVector3(this UdpPacket packet)
	{
		return new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
	}

	public static Quaternion ReadQuaternion(this UdpPacket packet)
	{
		return new Quaternion(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
	}

	public static MiniGuid ReadMiniGuid(this UdpPacket packet)
	{
		return new MiniGuid(packet.ReadByte(), packet.ReadByte(), packet.ReadByte(), packet.ReadByte(), packet.ReadByte(), packet.ReadByte(), packet.ReadByte(), packet.ReadByte());
	}

	public static T ReadSaveablePrefab<T>(this UdpPacket packet) where T : ISaveablePrefab
	{
		if (packet.ReadBool())
		{
			return MultiplayerMng.FindObject<T>(packet.ReadULong());
		}
		return MultiplayerMng.FindObject<T>(packet.ReadMiniGuid());
	}

	public static Rigidbody ReadRigidbody(this UdpPacket packet)
	{
		ISaveablePrefab saveablePrefab = packet.ReadSaveablePrefab<ISaveablePrefab>();
		int num = packet.ReadByte() - 1;
		if (num != -1)
		{
			if (saveablePrefab == null)
			{
				return null;
			}
			return saveablePrefab.AllRigidbodies[num];
		}
		return null;
	}

	public static Collider ReadCollider(this UdpPacket packet)
	{
		if (packet.ReadBool())
		{
			int num = packet.ReadByte() - 1;
			if (num != -1)
			{
				StrandedWorld instance = Beam.Singleton<StrandedWorld>.Instance;
				if ((object)instance == null)
				{
					return null;
				}
				return instance.Zones[num].GetComponentInChildren<TerrainCollider>();
			}
			return null;
		}
		ISaveablePrefab saveablePrefab = packet.ReadSaveablePrefab<ISaveablePrefab>();
		int num2 = packet.ReadByte() - 1;
		if (num2 != -1)
		{
			if (saveablePrefab == null)
			{
				return null;
			}
			return saveablePrefab.AllColliders[num2];
		}
		return null;
	}

	public static void WriteVector3(this UdpPacket packet, Vector3 value)
	{
		packet.WriteFloat(value.x);
		packet.WriteFloat(value.y);
		packet.WriteFloat(value.z);
	}

	public static void WriteQuaternion(this UdpPacket packet, Quaternion value)
	{
		packet.WriteFloat(value.x);
		packet.WriteFloat(value.y);
		packet.WriteFloat(value.z);
		packet.WriteFloat(value.w);
	}

	public static void WriteMiniGuid(this UdpPacket packet, MiniGuid value)
	{
		packet.WriteByte(value._d0);
		packet.WriteByte(value._d1);
		packet.WriteByte(value._d2);
		packet.WriteByte(value._d3);
		packet.WriteByte(value._d4);
		packet.WriteByte(value._d5);
		packet.WriteByte(value._d6);
		packet.WriteByte(value._d7);
	}

	public static void WriteSaveablePrefab(this UdpPacket packet, ISaveablePrefab value)
	{
		if (value == null)
		{
			packet.WriteBool(value: false);
			packet.WriteMiniGuid(default);
		}
		else if (value.IsMultiplayerEntity)
		{
			packet.WriteBool(value: true);
			packet.WriteULong(value.Entity.NetworkId.PackedValue);
		}
		else
		{
			packet.WriteBool(value: false);
			packet.WriteMiniGuid(value.ReferenceId);
		}
	}

	public static void WriteRigidbody(this UdpPacket packet, Rigidbody rigidbody)
	{
		ISaveablePrefab saveablePrefab = (((object)rigidbody != null) ? rigidbody.transform.GetInParent<ISaveablePrefab>() : null);
		if (null == rigidbody || saveablePrefab == null)
		{
			packet.WriteSaveablePrefab(null);
			packet.WriteByte(0);
		}
		else
		{
			packet.WriteSaveablePrefab(saveablePrefab);
			packet.WriteByte((byte)(saveablePrefab.GetRigidbodyId(rigidbody) + 1));
		}
	}

	public static void WriteCollider(this UdpPacket packet, Collider collider)
	{
		if (null == collider)
		{
			packet.WriteBool(value: true);
			packet.WriteByte(0);
			return;
		}
		if (collider is TerrainCollider)
		{
			packet.WriteBool(value: true);
			packet.WriteByte((byte)(Beam.Singleton<StrandedWorld>.Instance.GetZoneId(collider.transform.GetComponentInParent<Zone>()) + 1));
			return;
		}
		ISaveablePrefab inParent = collider.transform.GetInParent<ISaveablePrefab>();
		if (inParent == null)
		{
			packet.WriteBool(value: true);
			packet.WriteByte(0);
		}
		else
		{
			packet.WriteBool(value: false);
			packet.WriteSaveablePrefab(inParent);
			packet.WriteByte((byte)(inParent.GetColliderId(collider) + 1));
		}
	}

	public static float XZSqr(this Vector3 v)
	{
		return v.x * v.x + v.z * v.z;
	}
}
