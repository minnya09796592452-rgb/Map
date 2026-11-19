using System.Linq;
using Beam;
using UnityEngine;

[ExecuteInEditMode]
public class BoundsTest : MonoBehaviour
{
	public Bounds bounds;

	[SerializeField]
	private BoxCollider _collider;

	private Vector3[] _points = new Vector3[8];

	private void OnDrawGizmos()
	{
		OOBB oobb = new OOBB(transform, _collider.size);
		float num = oobb.Size.x / 2f;
		float num2 = oobb.Size.y / 2f;
		float num3 = oobb.Size.z / 2f;
		_points[0] = new Vector3(num, num2, num3);
		_points[1] = new Vector3(0f - num, num2, num3);
		_points[2] = new Vector3(num, 0f - num2, num3);
		_points[3] = new Vector3(0f - num, 0f - num2, num3);
		_points[4] = new Vector3(num, num2, 0f - num3);
		_points[5] = new Vector3(0f - num, num2, 0f - num3);
		_points[6] = new Vector3(num, 0f - num2, 0f - num3);
		_points[7] = new Vector3(0f - num, 0f - num2, 0f - num3);
		Gizmos.color = Color.green;
		Vector3[] array = new Vector3[8];
		for (int i = 0; i < _points.Length; i++)
		{
			array[i] = transform.TransformPoint(_points[i]);
		}
		Vector3[] array2 = array;
		for (int j = 0; j < array2.Length; j++)
		{
			Gizmos.DrawSphere(array2[j], 0.2f);
		}
		Gizmos.color = Color.white;
		Test(oobb);
	}

	public void Test(OOBB oobb)
	{
		foreach (BoxCollider item in (from c in Physics.OverlapSphere(transform.position, 3f)
			where c != _collider && c is BoxCollider
			select c).Cast<BoxCollider>())
		{
			if (new OOBB(item.transform, item.size).Intersects(oobb))
			{
				Debug.Log("INTERSECTING!!");
			}
		}
	}
}
