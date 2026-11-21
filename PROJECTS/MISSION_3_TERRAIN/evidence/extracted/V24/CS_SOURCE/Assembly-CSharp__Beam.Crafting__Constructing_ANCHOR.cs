using Beam.Events;
using Beam.Serialization.Json;
using Beam.UI;
using Beam.Utilities;
using Ceto;
using Funlabs;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Crafting;

public class Constructing_ANCHOR : Constructing, IInteractable, IBase, IHasCraftingType
{
	[SerializeField]
	private Transform _packedAnchorTransform;

	[SerializeField]
	private GameObject _deployedAnchorGameObject;

	[SerializeField]
	private Transform _deployedAnchorTransform;

	[SerializeField]
	private VerletLine _deployedAnchorRope;

	private Construction_STRUCTURE_RAFT _raftStructure;

	private bool _serverJoinInProgressAttemptDeploy;

	private bool _deployed;

	[HideInInspector]
	public UnityEvent OnDeploy;

	[HideInInspector]
	public UnityEvent OnRetrieve;

	public bool CanInteract { get; set; } = true;

	protected Construction_STRUCTURE_RAFT RaftStructure
	{
		get
		{
			if (_raftStructure == null)
			{
				_raftStructure = Structure as Construction_STRUCTURE_RAFT;
			}
			return _raftStructure;
		}
	}

	public string GetInteractionDescription(int playerId, IBase obj)
	{
		if (!_deployed)
		{
			return "ITEM_INTERACTION_DESCRIPTION_DROP";
		}
		return "ITEM_INTERACTION_DESCRIPTION_RETRIEVE";
	}

	protected override void Start()
	{
		base.Start();
		EventManager.AddListener<ServerJoinInProgressStarted>(OnServerJoinInProgressStarted);
		EventManager.AddListener<ServerJoinInProgressEnded>(OnServerJoinInProgressEnded);
	}

	private void OnDestroy()
	{
		EventManager.RemoveListener<ServerJoinInProgressStarted>(OnServerJoinInProgressStarted);
		EventManager.RemoveListener<ServerJoinInProgressEnded>(OnServerJoinInProgressEnded);
		if (_deployed)
		{
			Retrieve();
		}
		if (_deployedAnchorTransform != null)
		{
			BeamTeam.Destroy(_deployedAnchorTransform.gameObject);
		}
	}

	private void OnServerJoinInProgressStarted(ServerJoinInProgressStarted @event)
	{
		_serverJoinInProgressAttemptDeploy = false;
		if (!_deployed && !PlayerRegistry.LocalPlayer.IsNullOrDestroyed() && PlayerRegistry.LocalPlayer.IsInitialized && !PlayerRegistry.LocalPlayer.Movement.IsNullOrDestroyed() && !Structure.IsNullOrDestroyed() && !(Structure.transform != PlayerRegistry.LocalPlayer.Movement.ActivePlatform))
		{
			_serverJoinInProgressAttemptDeploy = true;
			Interact(PlayerRegistry.LocalPlayer);
		}
	}

	private void OnServerJoinInProgressEnded(ServerJoinInProgressEnded @event)
	{
		if (_deployed && _serverJoinInProgressAttemptDeploy)
		{
			LocalizedNotification.Post(PlayerRegistry.LocalPlayer, NotificationPriority.Low, 4f, "ANCHOR_NTF_SERVER_JIP_DEPLOYED");
			_serverJoinInProgressAttemptDeploy = false;
		}
	}

	public override void Damage(float damage, IBase damager)
	{
		base.Damage(damage, damager);
	}

	public bool Interact(IPlayer player)
	{
		if (!_deployed)
		{
			if (!CanDeploy())
			{
				LocalizedNotification.Post(player, NotificationPriority.Low, 4f, "ANCHOR_NTF_CANT_DROP_HERE_DESC");
				return false;
			}
			Deploy(GetAnchorPlacement(transform.position));
			return true;
		}
		if (_deployed)
		{
			Retrieve();
			return true;
		}
		return false;
	}

	public bool InteractWithObject(IPlayer player, IBase obj)
	{
		return Interact(player);
	}

	private void Deploy(Placement placement)
	{
		_deployedAnchorGameObject.SetActive(value: true);
		_deployedAnchorTransform.position = placement.Position + new Vector3(0f, 0.9f, 0f);
		_deployedAnchorTransform.rotation = placement.Rotation;
		_deployedAnchorTransform.parent = transform.parent.parent;
		_deployedAnchorRope.gameObject.SetActive(value: true);
		_packedAnchorTransform.gameObject.SetActive(value: false);
		RaftStructure.StructureBuoyancy.EnableWind(enable: false);
		RaftStructure.StructureBuoyancy.SetDampening(0.7f);
		_deployed = true;
		_raftStructure.AnchorsDeployed++;
		OnDeploy?.Invoke();
	}

	public void Retrieve()
	{
		_deployedAnchorGameObject.SetActive(value: false);
		_deployedAnchorTransform.parent = _deployedAnchorGameObject.transform;
		_deployedAnchorTransform.localPosition = Vector3.zero;
		_deployedAnchorRope.gameObject.SetActive(value: false);
		_packedAnchorTransform.gameObject.SetActive(value: true);
		_deployed = false;
		_raftStructure.AnchorsDeployed--;
		if (_raftStructure.AnchorsDeployed <= 0)
		{
			RaftStructure.StructureBuoyancy.Rigidbody.constraints = RigidbodyConstraints.None;
			RaftStructure.StructureBuoyancy.EnableWind(enable: true);
			RaftStructure.StructureBuoyancy.ResetDampening();
		}
		OnRetrieve?.Invoke();
	}

	private bool CanDeploy()
	{
		if (Singleton<StrandedWorld>.Instance == null)
		{
			return true;
		}
		float height = Singleton<StrandedWorld>.Instance.GetHeight(transform.position);
		if (height < Ceto.Ocean.Instance.level && height >= -100f)
		{
			return true;
		}
		return false;
	}

	private Placement GetAnchorPlacement(Vector3 position)
	{
		if (Physics.Raycast(position, Vector3.down, out var hitInfo, 100f, (1 << Layers.TERRAIN) | (1 << Layers.TERRAIN_OBJECTS)))
		{
			return new Placement
			{
				Position = hitInfo.point,
				Rotation = Quaternion.LookRotation(Vector3.forward, hitInfo.normal)
			};
		}
		return new Placement
		{
			Position = position - Vector3.up * 15f,
			Rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up)
		};
	}

	public override void OnTransformSerialisationIdChanged()
	{
		base.OnTransformSerialisationIdChanged();
		if (!Game.Mode.IsMaster() && _deployed)
		{
			Deploy(GetAnchorPlacement(transform.position));
		}
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		jObject.AddField("Deployed", _deployed);
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		if (data.GetField("Deployed").GetValue<bool>())
		{
			LevelLoader.CurrentLoader.DeferredLoading.Add(() =>
			{
				Deploy(GetAnchorPlacement(transform.position));
			});
		}
	}
}
