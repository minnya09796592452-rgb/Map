using UnityEngine;

public class DifficultyHatPropellar : MonoBehaviour
{
	[SerializeField]
	private Transform _hatPropellar;

	private void Update()
	{
		float yAngle = 80f * Time.deltaTime;
		_hatPropellar.transform.Rotate(0f, yAngle, 0f);
	}
}
