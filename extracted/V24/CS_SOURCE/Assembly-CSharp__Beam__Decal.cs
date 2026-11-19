using System.Collections;
using UnityEngine;

namespace Beam;

public class Decal : MonoBehaviour
{
	private ITerrainProvider _terrain;

	[SerializeField]
	private float _size = 1f;

	[SerializeField]
	private float _offset = 0.03f;

	private IEnumerator Start()
	{
		Decal decal = this;
		ITerrainProvider instance = Singleton<StrandedWorld>.Instance;
		decal._terrain = instance ?? NullObject<NullTerrainProvider>.Instance;
		while (LevelLoader.IsLoading())
		{
			yield return null;
		}
		GenerateDecalMesh();
	}

	private void GenerateDecalMesh()
	{
		Mesh mesh = new Mesh();
		float size = _size;
		int num = 8;
		int num2 = num + 1;
		int num3 = num + 1;
		int num4 = num2 * num3;
		Vector3[] array = new Vector3[num4];
		Vector2[] array2 = new Vector2[num4];
		Vector2 b = new Vector2(1f / ((float)num3 - 1f), 1f / ((float)num2 - 1f));
		Vector3 a = new Vector3(size / ((float)num3 - 1f), 1f, size / ((float)num2 - 1f));
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num3; j++)
			{
				Vector3 b2 = new Vector3(j, 0f, i);
				Vector3 position = Vector3.Scale(a, b2) - new Vector3(size / 2f, 0f, size / 2f);
				Vector3 worldPosition = transform.TransformPoint(position);
				float height = _terrain.GetHeight(worldPosition);
				array[j * num3 + i] = transform.InverseTransformPoint(new Vector3(worldPosition.x, height + _offset, worldPosition.z));
				array2[j * num3 + i] = Vector2.Scale(new Vector2(j, i), b);
			}
		}
		mesh.vertices = array;
		mesh.uv = array2;
		int num5 = num + 1;
		int[] array3 = new int[num * num * 6];
		int k = 0;
		int num6 = 0;
		for (; k < num; k++)
		{
			for (int l = 0; l < num; l++)
			{
				array3[num6++] = k * num5 + l;
				array3[num6++] = k * num5 + (l + 1);
				array3[num6++] = (k + 1) * num5 + (l + 1);
				array3[num6++] = k * num5 + l;
				array3[num6++] = (k + 1) * num5 + l + 1;
				array3[num6++] = (k + 1) * num5 + l;
			}
		}
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		GetComponent<MeshFilter>().mesh = mesh;
	}
}
