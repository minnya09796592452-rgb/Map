using Beam;
using Beam.Utilities;
using UnityEngine;

public class Ladders : MonoBehaviour
{
	public bool isTree;

	public Transform LandPos;

	[HideInInspector]
	public BoxCollider LadderCollider;

	private void Awake()
	{
		LadderCollider = GetComponent<BoxCollider>();
	}

	private void OnTriggerEnter(Collider collided)
	{
		if (collided.CompareTag("Player"))
		{
			IPlayer ladder = collided.GetInterface<IPlayer>();
			SetLadder(ladder);
		}
	}

	private void OnTriggerExit(Collider collided)
	{
		if (collided.CompareTag("Player"))
		{
			IPlayer player = collided.GetInterface<IPlayer>();
			ResetLadder(player);
		}
	}

	private void SetLadder(IPlayer player)
	{
		if (!player.IsNullOrDestroyed())
		{
			player.Movement.activeLadder = this;
		}
	}

	private void ResetLadder(IPlayer player)
	{
		if (!player.IsNullOrDestroyed() && player.Movement.activeLadder == this)
		{
			if (player.Movement.ClimbingLadder)
			{
				player.Movement.CharacterController.Move(-player.Movement.transform.forward * 1f + player.Movement.transform.up * 0.2f * Time.deltaTime);
			}
			player.Movement.ExitLadder();
		}
	}
}
