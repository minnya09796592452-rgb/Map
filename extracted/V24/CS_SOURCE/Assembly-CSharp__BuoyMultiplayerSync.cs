using Beam;
using Beam.Rendering;
using Funlabs;
using Photon.Bolt;
using UnityEngine;

public class BuoyMultiplayerSync : MultiplayerBehaviour
{
	[SerializeField]
	private GameObject _buoy;

	public override void Attached()
	{
		state.SetTransforms(((IBuoyState)state).Transform, _buoy.transform);
	}

	private void Start()
	{
		if (Game.Mode.IsClient())
		{
			_buoy.GetComponent<Rigidbody>().isKinematic = true;
			TryToAttachToBouy();
		}
	}

	public void TryToAttachToBouy()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag("Mission_Squid");
		if (gameObject != null)
		{
			transform.SetParent(gameObject.transform, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
			GetComponent<LodControllerChild>().AdoptParentLodGroup();
		}
	}
}
