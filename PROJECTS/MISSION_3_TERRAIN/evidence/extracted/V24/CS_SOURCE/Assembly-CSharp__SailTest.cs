using UnityEngine;

public class SailTest : MonoBehaviour
{
	public Transform Sail;

	public Transform Rudder;

	public float bowDir;

	public float headWind;

	public float keelDir;

	public float sailDir;

	public float sailDotWind;

	public float directionalFactor;

	[Header("Force")]
	public float Force;

	private void OnDrawGizmos()
	{
		Vector3 left = Vector3.left;
		if ((bool)Rudder)
		{
			bowDir = Vector3.Dot(transform.forward, left);
			headWind = Mathf.Clamp01(bowDir * 0.5f + 0.5f);
			keelDir = Mathf.Abs(bowDir);
			keelDir += 1f - Mathf.Abs(Vector3.Dot(Sail.forward, transform.forward));
			keelDir = Mathf.Clamp01(keelDir);
			sailDir = 1f - Mathf.Abs(Vector3.Dot(Sail.forward, left));
			sailDotWind = Mathf.Clamp01(0f - Vector3.Dot(Sail.forward, left));
			directionalFactor = Mathf.Lerp(sailDotWind, 1f, Mathf.Clamp01(bowDir));
			sailDir *= directionalFactor;
			float num = keelDir * sailDir * headWind;
			Vector3 vector = transform.forward * Mathf.Cos(Rudder.localRotation.y) - transform.right * Mathf.Sin(Rudder.localRotation.y);
			float num2 = 30f * num;
			num2 *= 5f;
			num2 = Mathf.Clamp(num2, 0f, 30f);
			Vector3 vector2 = vector * num2;
			Force = vector2.magnitude;
			Vector3 vector3 = vector2;
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, transform.position + Sail.forward);
			Gizmos.DrawWireSphere(transform.position + Sail.forward, 0.2f);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, transform.position + left);
			Gizmos.DrawWireSphere(transform.position + left, 0.2f);
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, transform.position + vector3);
			Gizmos.DrawWireSphere(transform.position + vector3, 0.1f);
			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(transform.position, transform.position + transform.forward);
			Gizmos.DrawWireSphere(transform.position + transform.forward, 0.2f);
			Gizmos.color = Color.white;
		}
	}
}
