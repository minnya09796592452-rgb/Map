using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam.Crafting;

public class ProjectileObject : ConstructionObject, IPickupable, IAnimatablePrimary, ICraftingDisplayableStorable, IDisplayableStorable, IStorable, IBase, IHasCraftingType, ISaveablePrefab, ISaveableReference, ISaveable, ICraftingMaterial, IDraggable, IPhysical, ISelectable, IProjectile
{
	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private ProjectileMovement _projectileMovement;

	[SerializeField]
	private BaseObjectProxy _proxy;

	private float _mass;

	private float _drag;

	private float _angularDrag;

	private RigidbodyInterpolation _interpolation;

	private CollisionDetectionMode _collision;

	public ProjectileMovement ProjectileMovement => _projectileMovement;

	public BaseObjectProxy Proxy => _proxy;

	protected override void Awake()
	{
		base.Awake();
		_projectileMovement.Initialize(this);
		_projectileMovement.enabled = false;
		CacheRigidbodyParameters();
		if (!LevelLoader.IsLoading())
		{
			rigidbody.useGravity = true;
		}
	}

	private void CacheRigidbodyParameters()
	{
		_mass = rigidbody.mass;
		_drag = rigidbody.drag;
		_angularDrag = rigidbody.angularDrag;
		_interpolation = rigidbody.interpolation;
		_collision = rigidbody.collisionDetectionMode;
	}

	public override void Attached()
	{
		base.Attached();
		rigidbody.isKinematic = !IsOwner;
	}

	protected override void OnPlayerDisconnect(PlayerDisconnectedEvent disconnectedEvent)
	{
		bool flag = !Owner.IsNullOrDestroyed();
		base.OnPlayerDisconnect(disconnectedEvent);
		if (!flag && !_projectileMovement.Thrower.IsNullOrDestroyed() && _projectileMovement.Thrower.Id == disconnectedEvent.PlayerId)
		{
			MultiplayerMng.Destroy(this);
		}
	}

	public override bool Store()
	{
		Reset();
		return base.Store();
	}

	protected override void OnTransformParentChanged()
	{
		base.OnTransformParentChanged();
		if (!rigidbody.IsNullOrDestroyed())
		{
			rigidbody.interpolation = ((ParentId != TransformId.Unknown) ? _interpolation : RigidbodyInterpolation.None);
		}
	}

	public virtual void Reset()
	{
		colliders.EnableColliders(enabled: true, isTrigger: false);
		this.CheckRigidbody(IsOwner);
		rigidbody.mass = _mass;
		rigidbody.drag = _drag;
		rigidbody.angularDrag = _angularDrag;
		rigidbody.interpolation = _interpolation;
		rigidbody.collisionDetectionMode = _collision;
		_projectileMovement.Reset();
		_projectileMovement.enabled = false;
		CanParent = true;
		CanDrag = true;
	}

	public virtual void Pierce(ProjectileHit hit)
	{
		Transform transform = hit.Transform;
		if (hit.Object.IsValid() && hit.Transform.gameObject.layer != Layers.PROJECTILE_HITBOXES)
		{
			transform = hit.Object.transform;
		}
		if (transform.CompareTag("Terrain"))
		{
			this.SetKinematic(kinematic: true);
			CanDrag = false;
			return;
		}
		bool isTrigger = hit.Object.IsValid() && hit.Object.transform.CompareTag("Shark");
		colliders.EnableColliders(enabled: true, isTrigger);
		this.RemoveRigidbody(IsOwner, immediate: true);
		if (transform != null)
		{
			this.ParentTo(transform);
		}
		CanParent = false;
		CanDrag = false;
	}

	public virtual void LocalFaunaPierce()
	{
		colliders.EnableColliders(enabled: true, isTrigger: true);
		this.RemoveRigidbody(IsOwner, immediate: true);
		CanParent = false;
		CanDrag = false;
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		if (!_projectileMovement.AttachedBase.IsNullOrDestroyed())
		{
			ISaveablePrefab saveablePrefab = _projectileMovement.AttachedBase as ISaveablePrefab;
			if (!saveablePrefab.IsNullOrDestroyed())
			{
				JObject jObject2 = new JObject();
				jObject2.AddField("AttachedId", saveablePrefab.ReferenceId.ToString());
				jObject2.AddField("Parent", transform.parent.name);
				jObject.AddField("ProjectileAttached", jObject2);
			}
		}
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		CacheRigidbodyParameters();
		JObject attachedData = data.GetField("ProjectileAttached");
		if (!attachedData)
		{
			return;
		}
		LevelLoader.CurrentLoader.DeferredLoading.Add(() =>
		{
			JObject field = attachedData.GetField("AttachedId");
			BaseObject baseObject = MultiplayerMng.FindObject<BaseObject>(field.GetValue<string>().ToMiniGuid());
			if (baseObject != null)
			{
				_projectileMovement.AttachedBase = baseObject;
				JObject field2 = attachedData.GetField("Parent");
				Transform transform = baseObject.transform.FindNestedChild(field2.GetValue<string>());
				ProjectileHit hit = new ProjectileHit(transform, baseObject);
				Pierce(hit);
				JSerializer.DeserializeInto(data.GetField("Transform"), base.transform);
				CompleteTransformSerialization();
			}
			else
			{
				Debug.LogError($"Projectile:: Couldn't find attached [{field}]");
				MultiplayerMng.Destroy(this, DestroyDelay.LATE);
			}
		});
	}
}
