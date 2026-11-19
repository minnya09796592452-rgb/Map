using UnityEngine;

namespace Beam;

public struct OOBB(Transform transform, Vector3 size)
{
	public readonly Vector3 Center = transform.position;

	public readonly Vector3 Forward = transform.forward;

	public readonly Vector3 Right = transform.right;

	public readonly Vector3 Up = transform.up;

	public readonly Vector3 Size = size;

	public bool Intersects(OOBB oobb)
	{
		Vector3[] array = new Vector3[15]
		{
			Forward,
			Right,
			Up,
			oobb.Forward,
			oobb.Right,
			oobb.Up,
			Vector3.Cross(Forward, oobb.Forward).normalized,
			Vector3.Cross(Forward, oobb.Right).normalized,
			Vector3.Cross(Forward, oobb.Up).normalized,
			Vector3.Cross(Right, oobb.Forward).normalized,
			Vector3.Cross(Right, oobb.Right).normalized,
			Vector3.Cross(Right, oobb.Up).normalized,
			Vector3.Cross(Up, oobb.Forward).normalized,
			Vector3.Cross(Up, oobb.Right).normalized,
			Vector3.Cross(Up, oobb.Up).normalized
		};
		Vector3 lhs = Center - oobb.Center;
		Vector3 lhs2 = 0.5f * (Right * Size.x + Up * Size.y + Forward * Size.z);
		Vector3 lhs3 = 0.5f * (Right * Size.x + Up * Size.y - Forward * Size.z);
		Vector3 lhs4 = 0.5f * (Right * Size.x - Up * Size.y + Forward * Size.z);
		Vector3 lhs5 = 0.5f * (Right * Size.x - Up * Size.y - Forward * Size.z);
		Vector3 lhs6 = 0.5f * (oobb.Right * oobb.Size.x + oobb.Up * oobb.Size.y + oobb.Forward * oobb.Size.z);
		Vector3 lhs7 = 0.5f * (oobb.Right * oobb.Size.x + oobb.Up * oobb.Size.y - oobb.Forward * oobb.Size.z);
		Vector3 lhs8 = 0.5f * (oobb.Right * oobb.Size.x - oobb.Up * oobb.Size.y + oobb.Forward * oobb.Size.z);
		Vector3 lhs9 = 0.5f * (oobb.Right * oobb.Size.x - oobb.Up * oobb.Size.y - oobb.Forward * oobb.Size.z);
		float[] array2 = new float[2];
		float[] array3 = new float[2];
		float[] array4 = new float[2];
		float[] array5 = new float[2];
		for (uint num = 0u; num < 15; num++)
		{
			if (!(array[num] == Vector3.zero))
			{
				float f = Vector3.Dot(lhs, array[num]);
				array2[0] = Vector3.Dot(lhs2, array[num]);
				array2[1] = Vector3.Dot(lhs3, array[num]);
				array3[0] = Vector3.Dot(lhs6, array[num]);
				array3[1] = Vector3.Dot(lhs7, array[num]);
				array4[0] = Vector3.Dot(lhs4, array[num]);
				array4[1] = Vector3.Dot(lhs5, array[num]);
				array5[0] = Vector3.Dot(lhs8, array[num]);
				array5[1] = Vector3.Dot(lhs9, array[num]);
				if (Mathf.Abs(f) > Mathf.Abs(array2[0]) + Mathf.Abs(array3[0]) && Mathf.Abs(f) > Mathf.Abs(array2[1]) + Mathf.Abs(array3[1]) && Mathf.Abs(f) > Mathf.Abs(array4[0]) + Mathf.Abs(array5[0]) && Mathf.Abs(f) > Mathf.Abs(array4[1]) + Mathf.Abs(array5[1]))
				{
					return false;
				}
			}
		}
		return true;
	}
}
