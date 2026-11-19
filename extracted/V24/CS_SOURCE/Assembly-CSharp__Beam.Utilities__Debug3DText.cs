using UnityEngine;

namespace Beam.Utilities;

public static class Debug3DText
{
	public static void Update(Transform parent, ref TextMesh textMesh, string value, float localOffset = 1.67f, bool attach = true, float localScale = 0.1f)
	{
		if (textMesh.IsNullOrDestroyed())
		{
			GameObject gameObject = new GameObject("DebugText", typeof(TextMesh))
			{
				layer = Layers.PLAYER
			};
			textMesh = gameObject.GetComponent<TextMesh>();
			textMesh.alignment = TextAlignment.Center;
			textMesh.anchor = TextAnchor.MiddleCenter;
		}
		GameObject gameObject2 = textMesh.gameObject;
		if (attach)
		{
			gameObject2.transform.parent = parent;
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = new Vector3(localScale, localScale, localScale);
			gameObject2.transform.localPosition = new Vector3(0f, localOffset, 0f);
		}
		else
		{
			gameObject2.transform.position = parent.position;
			gameObject2.transform.rotation = parent.rotation;
			gameObject2.transform.localScale = new Vector3(localScale, localScale, localScale);
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localPosition = Vector3.zero;
		}
		textMesh.text = value;
	}
}
