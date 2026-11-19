using Beam;
using Beam.Crafting;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

public class ConstructionObject_SPEAR : ProjectileObject, IChargeable, IUseable, IAnimatablePrimary, IBase, IHasCraftingType, IReloadable, IAnimatableSecondary, IProjectile
{
	private class ReplicateKillAndAttach : I_MultiplayerMessage
	{
		[I_Replicate]
		public SaveablePrefab Spearead;

		[Replicate]
		public ConstructionObject_SPEAR Spear;

		public override void OnPeer()
		{
			Spear.KillAndAttach((ISpearedInstance)Spearead);
		}
	}

	private class ReplicateLocalFaunaCreateSpearedInstanceOnMaster : MultiplayerMessage
	{
		[Replicate]
		public ConstructionObject_SPEAR Spear;

		[Replicate]
		public uint PrefabId;

		[Replicate]
		public Vector3 Position;

		[Replicate]
		public Quaternion Rotation;

		public override void OnPeer()
		{
			Spear.ReplicatedKillAndAttach(LocalFaunaCreateSpeared(PrefabId, Position, Rotation));
		}
	}

	[SerializeField]
	private Transform _spearAttachingAnchor;

	[SerializeField]
	private bool _canUseSecondary;

	[SerializeField]
	private bool _canSpearFish;

	private bool _reloaded = true;

	[Header("Secondary Animations")]
	[SerializeField]
	private AnimationType _secondaryIdle;

	[SerializeField]
	private AnimationType _secondary;

	[SerializeField]
	private AnimationType _reloadAnimation;

	[SerializeField]
	private float _spearSpeed;

	private ISpearedInstance _attachedChild;

	[SerializeField]
	[HideInInspector]
	private BaseActionUnityEvent _charged;

	[SerializeField]
	[HideInInspector]
	private BaseActionUnityEvent _usedSecondary;

	public bool CanUseSecondary
	{
		get
		{
			return _canUseSecondary;
		}
		set
		{
			_canUseSecondary = value;
		}
	}

	public bool Reloaded => _reloaded;

	public AnimationType SecondaryIdle => _secondaryIdle;

	public AnimationType Secondary => _secondary;

	public AnimationType ReloadAnimation => _reloadAnimation;

	BaseActionUnityEvent IChargeable.Charged => _charged;

	BaseActionUnityEvent IChargeable.UsedSecondary => _usedSecondary;

	public override bool ValidatePrimary(IBase obj)
	{
		if (_attachedChild.IsValid())
		{
			CollectAttached();
			return false;
		}
		return base.ValidatePrimary(obj);
	}

	public override void Use()
	{
		base.Use();
	}

	private void CollectAttached()
	{
		if (_attachedChild.IsValid() && (Owner.Holder.Storage.CanPush(_attachedChild.PickupableObject) || Owner.Holder.CurrentObject.IsNullOrDestroyed()))
		{
			Owner.Holder.Pickup(_attachedChild.PickupableObject);
		}
	}

	public bool ValidateSecondary(IBase obj)
	{
		return _attachedChild.IsNullOrDestroyed();
	}

	public void PreSecondary()
	{
		ActionEventData.Populate(this, null);
		_charged.Invoke(this, ActionEventData);
	}

	public void UseSecondary()
	{
		ActionEventData.Populate(this, null);
		ActionEventData.Position = Owner.transform.position;
		_usedSecondary.Invoke(this, ActionEventData);
		IPlayer owner = Owner;
		BeamRay ray = owner.PlayerCamera.GetRay(7f);
		float huntingMultiplier = owner.PlayerSkills.HuntingMultiplier;
		owner.Holder.DropCurrent();
		float num = Vector3.Distance(transform.position, ray.OriginPoint);
		transform.transform.position = ray.OriginPoint;
		transform.transform.LookAt(ray.EndPoint);
		transform.transform.position += transform.transform.forward * num;
		ProjectileMovement.enabled = true;
		ProjectileMovement.Fire(owner);
		rigidbody.AddForce(transform.forward * _spearSpeed * huntingMultiplier, ForceMode.Impulse);
	}

	public void PostSecondary()
	{
	}

	public bool CanReloadWith(IPickupable pickupable)
	{
		return false;
	}

	public void Reload(IPickupable pickupable)
	{
	}

	public override void DamageObject(IBase obj)
	{
		if (!StabObject(obj) && !obj.transform.GetComponent<Shootable>().IsNullOrDestroyed())
		{
			base.DamageObject(obj);
		}
	}

	private bool StabObject(IBase obj)
	{
		ISpearable spearable = obj as ISpearable;
		if (spearable.IsNullOrDestroyed() || !spearable.CanSpear)
		{
			return false;
		}
		if (!_canSpearFish && spearable.Type == InteractiveType.ANIMALS_FISH)
		{
			return false;
		}
		if (spearable.IsLocalFauna && spearable.LocalFaunaCheckHit(_spearAttachingAnchor.position))
		{
			LocalFaunaHandleSpearedInstanceOnMaster(spearable.LocalFaunaSpearedInstancePrefabId, obj.transform.position, obj.transform.rotation);
			spearable.LocalFaunaRelease();
			return true;
		}
		ISpearedInstance spearedInstance = spearable.GetSpearedInstance(_spearAttachingAnchor.position);
		if (KillAndAttach(spearedInstance))
		{
			return true;
		}
		return false;
	}

	private void LocalFaunaHandleSpearedInstanceOnMaster(uint prefabId, Vector3 position, Quaternion rotation)
	{
		if (Game.Mode.IsMaster())
		{
			ReplicatedKillAndAttach(LocalFaunaCreateSpeared(prefabId, position, rotation));
			return;
		}
		ReplicateLocalFaunaCreateSpearedInstanceOnMaster replicateLocalFaunaCreateSpearedInstanceOnMaster = new ReplicateLocalFaunaCreateSpearedInstanceOnMaster();
		replicateLocalFaunaCreateSpearedInstanceOnMaster.Spear = this;
		replicateLocalFaunaCreateSpearedInstanceOnMaster.PrefabId = prefabId;
		replicateLocalFaunaCreateSpearedInstanceOnMaster.Position = position;
		replicateLocalFaunaCreateSpearedInstanceOnMaster.Rotation = rotation;
		replicateLocalFaunaCreateSpearedInstanceOnMaster.Post();
	}

	private bool KillAndAttach(ISpearedInstance speared)
	{
		if (!speared.IsNullOrDestroyed())
		{
			speared.Kill(OwnerId);
			IPhysical physicsObject = speared.PhysicsObject;
			IDraggable draggable = physicsObject as IDraggable;
			if (!draggable.IsNullOrDestroyed())
			{
				draggable.CanDrag = false;
			}
			physicsObject.RemoveRigidbody((physicsObject as ISaveablePrefab)?.IsOwner ?? true);
			physicsObject.transform.CheckForProjectilesInChildren();
			physicsObject.transform.parent = null;
			physicsObject.TransformParent = _spearAttachingAnchor;
			physicsObject.Parent();
			physicsObject.CanParent = false;
			physicsObject.transform.localPosition = Vector3.zero;
			physicsObject.transform.localRotation = Quaternion.identity;
			speared.PickupableObject.PickedUp += SpearedInstance_PickedUp;
			_attachedChild = speared;
			return true;
		}
		return false;
	}

	private static ISpearedInstance LocalFaunaCreateSpeared(uint prefabId, Vector3 position, Quaternion rotation)
	{
		Interactive_FISH interactive_FISH = MultiplayerMng.Instantiate<Interactive_FISH>(prefabId, MiniGuid.New());
		interactive_FISH.transform.position = position;
		interactive_FISH.transform.rotation = rotation;
		interactive_FISH.Parent();
		return interactive_FISH;
	}

	private void SpearedInstance_PickedUp(IPickupable sender)
	{
		if (!_attachedChild.IsNullOrDestroyed())
		{
			_attachedChild.PhysicsObject.EnablePhysics();
			_attachedChild.PhysicsObject.CanParent = true;
			_attachedChild.PickupableObject.PickedUp -= SpearedInstance_PickedUp;
			_attachedChild = null;
		}
	}

	private void ReplicatedKillAndAttach(ISpearedInstance speared)
	{
		bool flag = KillAndAttach(speared);
		if (Game.Mode.IsMultiplayer() & flag)
		{
			ReplicateKillAndAttach replicateKillAndAttach = new ReplicateKillAndAttach();
			replicateKillAndAttach.Spearead = (SaveablePrefab)speared;
			replicateKillAndAttach.Spear = this;
			replicateKillAndAttach.Post();
		}
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		if (!_attachedChild.IsNullOrDestroyed())
		{
			if (!Owner.IsNullOrDestroyed() && (Owner.Holder.Storage.Has(this) || (!Owner.Holder.CurrentObject.IsNullOrDestroyed() && Owner.Holder.CurrentObject.Equals(this))))
			{
				JObject jObject2 = SavingUtilities.SavePrefabToJObject(_attachedChild.PickupableObject);
				if (jObject2 != null)
				{
					jObject.AddField("AttachedChildData", jObject2);
				}
			}
			else
			{
				jObject.AddField("AttachedChild", JSerializer.Serialize(_attachedChild.PickupableObject.ReferenceId.ToString()));
			}
		}
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		JObject field = data.GetField("AttachedChildData");
		if (field != null)
		{
			SavingUtilities.LoadPrefabFromJData(MultiplayerZoneSync.Usage.Spear, field, null, (SaveablePrefab instance) =>
			{
				KillAndAttach((ISpearedInstance)instance);
			});
			return;
		}
		JObject field2 = data.GetField("AttachedChild");
		if (!field2)
		{
			return;
		}
		MiniGuid childReferenceData = field2.GetValue<string>().ToMiniGuid();
		LevelLoader.CurrentLoader.DeferredLoading.Add(() =>
		{
			InteractiveObject interactiveObject = MultiplayerMng.FindObject<InteractiveObject>(childReferenceData);
			if (interactiveObject != null)
			{
				KillAndAttach((ISpearedInstance)interactiveObject);
			}
			else
			{
				Debug.LogError($"InteractiveObject_SPEAR::Load:: Error finding child [{childReferenceData}]");
			}
		});
	}
}
