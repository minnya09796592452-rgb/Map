using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Beam.UI;
using MEC;
using UnityEngine;

namespace Beam.Utilities;

public static class BeamTeam
{
	public static CoroutineHandle RunCoroutine(this Timing timing, IEnumerator<float> enumerator, Action<Exception> onComplete)
	{
		return timing.RunCoroutineOnInstance(RunThrowingIterator(enumerator, onComplete));
	}

	public static IEnumerator<float> RunThrowingIterator(IEnumerator<float> enumerator, Action<Exception> onComplete)
	{
		while (true)
		{
			float current;
			try
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				current = enumerator.Current;
			}
			catch (Exception obj)
			{
				onComplete(obj);
				break;
			}
			onComplete(null);
			yield return current;
		}
	}

	public static float WaitForTask<T>(Func<Task<T>> task, Action<T> callback)
	{
		return Timing.WaitUntilDone(Timing.RunCoroutine(WaitForTask_Sequence(task, callback)));
	}

	private static IEnumerator<float> WaitForTask_Sequence<T>(Func<Task<T>> task, Action<T> callback)
	{
		Task<T> t = Task.Run(task);
		while (!t.IsCompleted)
		{
			yield return 0f;
		}
		callback?.Invoke(t.Result);
	}

	public static void InvokeDelayed(float delay, Action callback)
	{
		if (callback != null)
		{
			Timing.RunCoroutine(InvokeDelayed_Sequence(delay, callback));
		}
	}

	private static IEnumerator<float> InvokeDelayed_Sequence(float delay, Action callback)
	{
		yield return float.NegativeInfinity;
		float timer = 0f;
		while (timer < delay)
		{
			timer += Time.unscaledDeltaTime;
			yield return 0f;
		}
		callback?.Invoke();
	}

	public static void CopyDirectory(string srcDir, string dstDir, bool overwrite = false)
	{
		if (!Directory.Exists(srcDir))
		{
			Debug.LogError(srcDir + " does not exist!");
			return;
		}
		if (!Directory.Exists(dstDir))
		{
			Directory.CreateDirectory(dstDir);
		}
		foreach (string item in from name in Directory.EnumerateFiles(srcDir)
			where !name.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase)
			select name)
		{
			File.Copy(item, Path.Combine(dstDir, Path.GetFileName(item)), overwrite);
		}
		string[] directories = Directory.GetDirectories(srcDir);
		foreach (string text in directories)
		{
			CopyDirectory(text, Path.Combine(dstDir, Path.GetFileName(text)));
		}
	}

	public static float LinearToDecibel(float value)
	{
		if (!(value > 0f))
		{
			return -144f;
		}
		return 20f * Mathf.Log10(value);
	}

	public static void SafeStopCoroutine(this MonoBehaviour behaviour, Coroutine coroutine)
	{
		if (coroutine != null)
		{
			behaviour.StopCoroutine(coroutine);
		}
	}

	public static TInterface GetInterface<TInterface>(this GameObject gameObject) where TInterface : class
	{
		return gameObject.GetComponent(typeof(TInterface)) as TInterface;
	}

	public static TInterface GetInterface<TInterface>(this Component behaviour) where TInterface : class
	{
		return behaviour.GetComponent(typeof(TInterface)) as TInterface;
	}

	public static TInterface GetInterfaceInChildren<TInterface>(this Component behaviour, bool includeInactive) where TInterface : class
	{
		return behaviour.GetComponentInChildren(typeof(TInterface), includeInactive) as TInterface;
	}

	public static TInterface[] GetInterfaces<TInterface>(this Component behaviour) where TInterface : class
	{
		return behaviour.GetComponents(typeof(TInterface)) as TInterface[];
	}

	public static TInterface[] GetInterfacesInChildren<TInterface>(this Component behaviour, bool includeInactive) where TInterface : class
	{
		return behaviour.GetComponentsInChildren(typeof(TInterface), includeInactive) as TInterface[];
	}

	public static Transform FindChildWithTag(this Transform transform, string tag)
	{
		Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform2 in componentsInChildren)
		{
			if (transform2.CompareTag(tag))
			{
				return transform2;
			}
		}
		return null;
	}

	public static bool IsNullOrDestroyed(this object value)
	{
		return value?.Equals(null) ?? true;
	}

	public static bool IsValid(this object value)
	{
		return !value.IsNullOrDestroyed();
	}

	public static void Destroy<T>(T component) where T : UnityEngine.Object
	{
		if ((bool)component)
		{
			UnityEngine.Object.Destroy(component);
			component = null;
		}
	}

	public static void DestroyAll<T>(T[] components) where T : UnityEngine.Object
	{
		for (int num = components.Length - 1; num >= 0; num--)
		{
			Destroy(components[num]);
		}
	}

	public static void DestroyImmediate<T>(T component, bool sceneObjects = false) where T : UnityEngine.Object
	{
		if ((bool)component)
		{
			UnityEngine.Object.DestroyImmediate(component, sceneObjects);
			component = null;
		}
	}

	public static void DestroyAllImmediate<T>(T[] components, bool sceneObjects = false) where T : UnityEngine.Object
	{
		for (int num = components.Length - 1; num >= 0; num--)
		{
			DestroyImmediate(components[num], sceneObjects);
		}
	}

	public static void DestroyAllChildren(Transform transform)
	{
		if (transform == null)
		{
			return;
		}
		Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int num = componentsInChildren.Length - 1; num >= 0; num--)
		{
			Transform transform2 = componentsInChildren[num];
			if (!(transform2 == transform) && !transform2.IsNullOrDestroyed())
			{
				Destroy(transform2.gameObject);
			}
		}
	}

	public static void DestroyAllChildrenImmediate(Transform transform)
	{
		if (transform == null)
		{
			return;
		}
		Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int num = componentsInChildren.Length - 1; num >= 0; num--)
		{
			Transform transform2 = componentsInChildren[num];
			if (!(transform2 == transform) && !transform2.IsNullOrDestroyed())
			{
				DestroyImmediate(transform2.gameObject);
			}
		}
	}

	public static void DestroyAllChildren(ITransformViewAdapter transform)
	{
		if (transform == null || transform.Transform == null)
		{
			return;
		}
		foreach (Transform item in transform.Transform)
		{
			Destroy(item.gameObject);
		}
	}
}
