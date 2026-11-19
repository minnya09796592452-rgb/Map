using System;
using Beam;
using Beam.Crafting;
using Beam.Language;
using Beam.UI;
using Beam.Utilities;
using Funlabs;
using Photon.Bolt;
using Rewired.Utils;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods
{
	private class ReplicateCopyProjectilesTo : I_MultiplayerMessage
	{
		[Replicate]
		public SaveablePrefab Source;

		[I_Replicate]
		public IBase Destination;

		public override void OnPeer()
		{
			Source.CopyProjectilesTo(Destination);
		}
	}

	private class ReplicateCopyTransformTo : I_MultiplayerMessage
	{
		[Replicate]
		public SaveablePrefab Source;

		[I_Replicate]
		public IBase Destination;

		[Replicate]
		public bool Broad;

		public override void OnPeer()
		{
			Source.CopyTransformsTo(Destination, Broad);
		}
	}

	public static float ToCelsius(this float fahrenheit)
	{
		return (fahrenheit - 32f) * 0.555555f;
	}

	public static void EnableColliders(this Collider[] colliders, bool enabled, bool isTrigger)
	{
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = enabled;
			colliders[i].isTrigger = isTrigger;
		}
	}

	public static Transform FindInChildren(this Transform transform, string name)
	{
		int childCount = transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (child.name == name)
			{
				return child;
			}
			Transform transform2 = child.FindInChildren(name);
			if (transform2 != null)
			{
				return transform2;
			}
		}
		return null;
	}

	public static void CopyTransformsToRecursive(this Transform source, Transform destination)
	{
		destination.position = source.position;
		destination.rotation = source.rotation;
		foreach (Transform item in destination)
		{
			Transform transform2 = source.Find(item.name);
			if ((bool)transform2)
			{
				transform2.CopyTransformsToRecursive(item);
			}
		}
	}

	private static void BroadCopyTransformToRecursive(this Transform source, Transform destination)
	{
		foreach (Transform item in source)
		{
			Transform transform2 = destination.transform.FindInChildren(item.name);
			if (!transform2.IsNullOrDestroyed())
			{
				transform2.position = item.position;
				transform2.rotation = item.rotation;
			}
			item.BroadCopyTransformToRecursive(destination);
		}
	}

	public static void CopyTransformsTo(this SaveablePrefab source, IBase destination, bool broad = false)
	{
		if (broad)
		{
			destination.transform.position = source.transform.position;
			destination.transform.rotation = source.transform.rotation;
			source.transform.BroadCopyTransformToRecursive(destination.transform);
		}
		else
		{
			source.transform.CopyTransformsToRecursive(destination.transform);
		}
	}

	public static ProjectileObject[] GetProjectiles(this Transform transform)
	{
		return transform.GetComponentsInChildren<ProjectileObject>();
	}

	public static Vector3[] GetPositions(this ProjectileObject[] projectiles, bool local)
	{
		Vector3[] array = new Vector3[projectiles.Length];
		for (int i = 0; i < projectiles.Length; i++)
		{
			array[i] = (local ? projectiles[i].transform.localPosition : projectiles[i].transform.position);
		}
		return array;
	}

	public static Quaternion[] GetRotations(this ProjectileObject[] projectiles, bool local)
	{
		Quaternion[] array = new Quaternion[projectiles.Length];
		for (int i = 0; i < projectiles.Length; i++)
		{
			array[i] = (local ? projectiles[i].transform.localRotation : projectiles[i].transform.rotation);
		}
		return array;
	}

	public static void SetPositions(this ProjectileObject[] projectiles, Vector3[] positions, bool local)
	{
		if (projectiles.Length != positions.Length)
		{
			throw new Exception("SetPositions projecitles and positions have different lenghts");
		}
		for (int i = 0; i < projectiles.Length; i++)
		{
			if (local)
			{
				projectiles[i].transform.localPosition = positions[i];
			}
			else
			{
				projectiles[i].transform.position = positions[i];
			}
		}
	}

	public static void SetRotation(this ProjectileObject[] projectiles, Quaternion[] rotations, bool local)
	{
		if (projectiles.Length != rotations.Length)
		{
			throw new Exception("SetPositions projecitles and rotations have different lenghts");
		}
		for (int i = 0; i < projectiles.Length; i++)
		{
			if (local)
			{
				projectiles[i].transform.localRotation = rotations[i];
			}
			else
			{
				projectiles[i].transform.rotation = rotations[i];
			}
		}
	}

	public static void AttachTo(this ProjectileObject[] projectiles, IBase destination)
	{
		foreach (ProjectileObject obj in projectiles)
		{
			obj.transform.parent = null;
			obj.TransformParent = destination.transform;
			obj.Parent();
			obj.ProjectileMovement.AttachedBase = destination;
		}
	}

	public static void CopyProjectilesTo(this SaveablePrefab source, IBase destination)
	{
		source.transform.GetProjectiles().AttachTo(destination);
	}

	public static void ReplicatedCopyProjectilesTo(this SaveablePrefab source, IBase destination)
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateCopyProjectilesTo replicateCopyProjectilesTo = new ReplicateCopyProjectilesTo();
			replicateCopyProjectilesTo.Source = source;
			replicateCopyProjectilesTo.Destination = destination;
			replicateCopyProjectilesTo.Post();
		}
		source.CopyProjectilesTo(destination);
	}

	public static void ReplicatedCopyTransformsTo(this SaveablePrefab source, IBase destination, bool broad = false)
	{
		if (Game.Mode.IsMultiplayer())
		{
			ReplicateCopyTransformTo replicateCopyTransformTo = new ReplicateCopyTransformTo();
			replicateCopyTransformTo.Source = source;
			replicateCopyTransformTo.Destination = destination;
			replicateCopyTransformTo.Broad = broad;
			replicateCopyTransformTo.Post();
		}
		source.CopyTransformsTo(destination, broad);
	}

	public static bool HasConstraint(this UsingConstraint value, UsingConstraint flag)
	{
		return (value & flag) != 0;
	}

	public static string ToPaddedString(this Version version)
	{
		return $"{version.Major}.{version.Minor:00}.{version.Build:00}";
	}

	public static string ToPaddedString2(this Version version)
	{
		string text = $"{version.Major}{version.Minor:000}";
		if (Application.isPlaying && BoltNetwork.IsDebugMode)
		{
			text += "d";
		}
		return text;
	}

	public static bool IsTransient(this int resultCode)
	{
		if (!Enum.IsDefined(typeof(EResultCode), resultCode))
		{
			return false;
		}
		if (resultCode == 48879 || resultCode == 51966 || resultCode == 57005)
		{
			return true;
		}
		return false;
	}

	public static string ToPrettyString(this int resultCode)
	{
		string text = $"hr=0x{resultCode:X8}";
		if (Enum.IsDefined(typeof(EResultCode), resultCode))
		{
			return $"{(EResultCode)resultCode} ({text})";
		}
		return "E_UNHANDLED (" + text + ")";
	}

	public static string GetPrettyResultMsg(this int resultCode, string term)
	{
		string text = Localization.GetLanguageHandler().Localize(term, resultCode.ToPrettyString());
		if (resultCode.IsTransient())
		{
			text = text + "\n" + Localization.GetLanguageHandler().Localize("TRANSIENT_RESULT_CODE_TRY_AGAIN_MSG");
		}
		return text;
	}

	public static void SetNavigation<T>(this T component, NavigationDirection direction, T element) where T : Component
	{
		Selectable selectable = component?.GetComponent<Selectable>();
		if (selectable == null)
		{
			return;
		}
		Selectable selectable2 = element?.GetComponent<Selectable>();
		if (!(selectable2 == null))
		{
			Navigation navigation = selectable.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			switch (direction)
			{
			case NavigationDirection.Left:
				navigation.selectOnLeft = selectable2;
				break;
			case NavigationDirection.Right:
				navigation.selectOnRight = selectable2;
				break;
			case NavigationDirection.Up:
				navigation.selectOnUp = selectable2;
				break;
			case NavigationDirection.Down:
				navigation.selectOnDown = selectable2;
				break;
			}
			selectable.navigation = navigation;
		}
	}

	public static void FocusElement(this IScrollviewViewAdapter scrollview, Transform selectable)
	{
		RectTransform rectTransform = (RectTransform)scrollview.transform;
		RectTransform rectTransform2 = (RectTransform)selectable;
		Rect child = MathTools.TransformRect(rectTransform2.rect, rectTransform2, rectTransform);
		Rect rect = rectTransform.rect;
		Rect rect2 = rectTransform.rect;
		if (!MathTools.RectContains(rect2, child) && GetOffsetToContainRect(rect2, child, out var offset))
		{
			RectTransform rectTransform3 = (RectTransform)scrollview.Content.Transform;
			Vector2 anchoredPosition = rectTransform3.anchoredPosition;
			anchoredPosition.x = Mathf.Clamp(anchoredPosition.x + offset.x, 0f, Mathf.Abs(rect.width - rectTransform3.sizeDelta.x));
			anchoredPosition.y = Mathf.Clamp(anchoredPosition.y + offset.y, 0f, Mathf.Abs(rect.height - rectTransform3.sizeDelta.y));
			rectTransform3.anchoredPosition = anchoredPosition;
		}
	}

	public static bool GetOffsetToContainRect(Rect container, Rect child, out Vector2 offset)
	{
		offset = Vector2.zero;
		if (container.width < child.width)
		{
			return false;
		}
		if (container.height < child.height)
		{
			return false;
		}
		if (child.xMin < container.xMin)
		{
			offset.x += container.xMin - child.xMin;
		}
		if (child.yMin < container.yMin)
		{
			offset.y += container.yMin - child.yMin;
		}
		if (child.xMax > container.xMax)
		{
			offset.x += container.xMax - child.xMax;
		}
		if (child.yMax > container.yMax)
		{
			offset.y += container.yMax - child.yMax;
		}
		return true;
	}
}
