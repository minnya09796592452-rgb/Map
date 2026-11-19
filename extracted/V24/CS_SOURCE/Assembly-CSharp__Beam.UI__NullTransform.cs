using UnityEngine;

namespace Beam.UI;

public class NullTransform : MonoBehaviour
{
	private static Transform _Instance;

	public static Transform Instance
	{
		get
		{
			if (_Instance == null)
			{
				_Instance = NullGameObject.Instance.transform;
			}
			_Instance.transform.position = Vector3.zero;
			_Instance.transform.rotation = Quaternion.identity;
			Debug.LogError("NullTransform:: ");
			return _Instance;
		}
	}
}
