using UnityEngine;

namespace Beam;

public static class BaseExtensions
{
	public static void SetLayerRecursively(this Transform transform, int layer, int ignoreLayerMask = 0)
	{
		if ((ignoreLayerMask & (1 << transform.gameObject.layer)) == 0)
		{
			transform.gameObject.layer = layer;
		}
		foreach (Transform item in transform)
		{
			item.SetLayerRecursively(layer, ignoreLayerMask);
		}
	}
}
