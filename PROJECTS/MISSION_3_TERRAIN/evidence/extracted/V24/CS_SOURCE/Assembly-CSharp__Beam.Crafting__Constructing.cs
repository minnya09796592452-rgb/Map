using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Serialization;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beam.Crafting;

public class Constructing : BaseObject, IConstructable, ICraftable, IBase, IHasCraftingType, IPlaceable, ISelectable
{
	[Serializable]
	public class ConstructingLevel
	{
		public GameObject LevelGameObject;
	}

	public enum ERotationMode
	{
		None,
		Free
	}

	private class ReplicateCreateConstruction : I_MultiplayerMessage
	{
		[I_Replicate]
		public Construction_STRUCTURE Structure;

		[Replicate]
		public int StructureLayer;

		[Replicate]
		public uint PrefabId;

		[Replicate]
		public MiniGuid ReferenceId;

		[Replicate]
		public Vector3 Position;

		[Replicate]
		public Quaternion Rotation;

		public override void OnPeer()
		{
			Structure.gameObject.layer = StructureLayer;
			Constructing constructing = MultiplayerMng.Instantiate<Constructing>(PrefabId, ReferenceId);
			constructing.transform.position = Position;
			constructing.transform.rotation = Rotation;
			constructing.Structure = Structure;
			constructing.Place();
		}
	}

	[SerializeField]
	private bool handlePlayerColission;

	public const float UNIT_WIDTH = 0.3f;

	public const float ONE_UNIT = 3f;

	public const float ONE_HALF_UNIT = 1.5f;

	public const float CONNECTOR_PANEL_DIAMTER = 0.6f;

	public const float CONNECTOR_FOUNDATION_DIAMETER = 1.2f;

	public const float SNAPPING_THRESHOLD = 0.2f;

	public const float SPHERECAST_RADIUS = 0.3f;

	[PREFAB_PROCESSOR_GetComponent]
	[HideInInspector]
	[SerializeField]
	private Selecting _selecting;

	[Header("CRAFTING")]
	[SerializeField]
	private GhostCollider.ColliderType _colliderType;

	[SerializeField]
	private bool _craftImmediate;

	[SerializeField]
	private GameObject _structurePrefab;

	private Connector _snappedConnector;

	private Connector _prevSnappedConnector;

	[SerializeField]
	private float _placingDistance = 4f;

	[SerializeField]
	private ERotationMode _rotationMode;

	[SerializeField]
	private int _rotationAngle = 90;

	private float _proximityDistance = 4.5f;

	private Placement _previousPlacement = new Placement();

	[SerializeField]
	private GameObject _ghostObject;

	[SerializeField]
	private Mesh _ghostMesh;

	[SerializeField]
	private Renderer _ghostRenderer;

	private Construction_STRUCTURE _structure;

	[PREFAB_PROCESSOR_GetComponent]
	[SerializeField]
	private Connector _myConnector;

	[HideInInspector]
	[SerializeField]
	private Connector[] _myConnectors;

	[FormerlySerializedAs("_onCrafted")]
	[SerializeField]
	[HideInInspector]
	private InteracterUnityEvent _crafted;

	[Header("PROXIMITY CHECKING")]
	[SerializeField]
	private ConstructingProximityCheckMode _proximityChecking;

	private IConstructingProximityCheck _proximityCheck;

	[PREFAB_PROCESSOR_GetComponents]
	[SerializeField]
	private PlacementModifier[] _placementModifiers;

	[SerializeField]
	private LayerMask _connectorLayerMask;

	public float MaximumHealthPoints;

	[SerializeField]
	private ConstructingLevel[] _constructingLevels;

	[SerializeField]
	private Collider[] _constructingLevelColliders;

	[PREFAB_PROCESSOR_GetComponents]
	[SerializeField]
	private Collider[] _colliders;

	[SerializeField]
	[FormerlySerializedAs("nestedPlacingCollider")]
	private bool _nestedPlacingCollider;

	private bool _snapped;

	private int _previousLevel;

	[HideInInspector]
	public Vector3 Pos;

	[HideInInspector]
	public Quaternion Rot;

	public Selecting Selecting => _selecting;

	public int CreatorId { get; set; }

	public GhostCollider.ColliderType ColliderType => _colliderType;

	public bool CraftImmediate => _craftImmediate;

	protected GameObject StructurePrefab => _structurePrefab;

	public Connector SnappedConnector
	{
		get
		{
			return _snappedConnector;
		}
		set
		{
			_snappedConnector = value;
		}
	}

	public int Walls
	{
		get
		{
			int num = 0;
			Connector[] myConnectors = _myConnectors;
			foreach (Connector connector in myConnectors)
			{
				if (connector.Type == ConnectorType.WALL && connector.CanSet && !connector.IsValid)
				{
					num++;
				}
			}
			return num;
		}
	}

	public float PlacingDistance => _placingDistance;

	public ERotationMode RotationMode => _rotationMode;

	protected float ProximityDistance
	{
		get
		{
			return _proximityDistance;
		}
		set
		{
			_proximityDistance = value;
		}
	}

	public Mesh GhostMesh => _ghostMesh;

	public Material[] GhostMaterials => _ghostRenderer.sharedMaterials;

	public Construction_STRUCTURE Structure
	{
		get
		{
			return _structure;
		}
		set
		{
			_structure = value;
			CanParent = _structure == null;
		}
	}

	public Connector MyConnector => _myConnector;

	public Connector[] MyConnectors => _myConnectors;

	protected LayerMask ConnectorLayerMask => _connectorLayerMask;

	public Collider[] Colliders => _colliders;

	public bool IsSnapped => _snapped;

	public bool IsStructurePrefabAMultiplayerEntity()
	{
		if (!(_structurePrefab == null))
		{
			return _structurePrefab.GetComponent<SaveablePrefab>()?.IsMultiplayerEntity ?? false;
		}
		return false;
	}

	public void SetSnapped(Connector snappedConnector)
	{
		_snapped = snappedConnector != null;
		_prevSnappedConnector = _snappedConnector;
		_snappedConnector = snappedConnector;
	}

	protected override void Awake()
	{
		base.Awake();
		InitializeProximityCheck();
		CheckHealth();
	}

	private void InitializeProximityCheck()
	{
		if (_proximityChecking == ConstructingProximityCheckMode.None)
		{
			_proximityCheck = new NoConstructingProximityCheck();
		}
		else
		{
			_proximityCheck = new DefaultConstructingProximityCheck();
		}
	}

	public virtual void OnBeginCrafting()
	{
	}

	public virtual bool OwnerPlacing(Transform ghost, BeamRay ray, float yRotation)
	{
		Placement placement = new Placement();
		placement.Position = ghost.position;
		placement.Rotation = ghost.rotation;
		for (int i = 0; i < _placementModifiers.Length; i++)
		{
			placement = _placementModifiers[i].Modify(placement, ray);
		}
		bool result = false;
		_prevSnappedConnector = _snappedConnector;
		_snappedConnector = null;
		_snapped = false;
		IList<Connector> componentSphereCastAll = BeamPhysics.GetComponentSphereCastAll<Connector>(ray.BaseRay, 0.3f, ray.Length, _connectorLayerMask.value);
		if (componentSphereCastAll.Count > 0)
		{
			for (int j = 0; j < componentSphereCastAll.Count; j++)
			{
				Connector connector = componentSphereCastAll[j];
				_snapped = _myConnector.ConnectTo(connector, placement);
				if (!_snapped)
				{
					continue;
				}
				placement = CheckPlacement(placement);
				_snappedConnector = connector;
				if (_snappedConnector != _prevSnappedConnector)
				{
					Ghost component = ghost.GetComponent<Ghost>();
					IList<Constructing> constructions;
					if (!_snappedConnector.Constructing.Structure.DisableGhostCollisionOnSnap)
					{
						IList<Constructing> list = _snappedConnector.Constructing.Structure.Constructions.Where((Constructing c) => c is Construction_RAFT).ToList();
						constructions = list;
					}
					else
					{
						constructions = _snappedConnector.Constructing.Structure.Constructions;
					}
					component.SetCollisionForConstructions(constructions, ignore: true);
				}
				result = true;
				break;
			}
		}
		else
		{
			result = _myConnector.ConnectTo(null, placement);
		}
		if ((bool)_prevSnappedConnector && _prevSnappedConnector != _snappedConnector && (bool)_prevSnappedConnector.Constructing && (bool)_prevSnappedConnector.Constructing.Structure)
		{
			ghost.GetComponent<Ghost>().SetCollisionForConstructions(_prevSnappedConnector.Constructing.Structure.Constructions, ignore: false);
		}
		_prevSnappedConnector = null;
		if (_snapped)
		{
			Quaternion rotation = placement.Rotation;
			if (_rotationMode != ERotationMode.None)
			{
				float y = Mathf.Round(yRotation / (float)_rotationAngle) * (float)_rotationAngle;
				rotation *= Quaternion.Euler(new Vector3(0f, y, 0f));
			}
			ghost.position = placement.Position;
			ghost.rotation = rotation;
		}
		else
		{
			ghost.position = placement.Position;
			ghost.rotation = placement.Rotation;
			if (_rotationMode != ERotationMode.None)
			{
				ghost.rotation *= Quaternion.Euler(new Vector3(0f, yRotation, 0f));
			}
		}
		return result;
	}

	public virtual bool ReplicantPlacing(Transform ghost, BeamRay ray, float yRotation)
	{
		Placement placement = new Placement();
		placement.Position = ghost.position;
		placement.Rotation = ghost.rotation;
		for (int i = 0; i < _placementModifiers.Length; i++)
		{
			placement = _placementModifiers[i].Modify(placement, ray);
		}
		bool flag = false;
		if (_snapped)
		{
			_myConnector.ConnectTo(_snappedConnector, placement);
			placement = CheckPlacement(placement);
			if (_snappedConnector != _prevSnappedConnector && _snappedConnector.Constructing.Structure.DisableGhostCollisionOnSnap)
			{
				Ghost component = ghost.GetComponent<Ghost>();
				IList<Constructing> constructions;
				if (!_snappedConnector.Constructing.Structure.DisableGhostCollisionOnSnap)
				{
					IList<Constructing> list = _snappedConnector.Constructing.Structure.Constructions.Where((Constructing c) => c is Construction_RAFT).ToList();
					constructions = list;
				}
				else
				{
					constructions = _snappedConnector.Constructing.Structure.Constructions;
				}
				component.SetCollisionForConstructions(constructions, ignore: true);
			}
			flag = true;
		}
		else
		{
			flag = _myConnector.ConnectTo(null, placement);
		}
		if ((bool)_prevSnappedConnector && _prevSnappedConnector != _snappedConnector && (bool)_prevSnappedConnector.Constructing && (bool)_prevSnappedConnector.Constructing.Structure)
		{
			ghost.GetComponent<Ghost>().SetCollisionForConstructions(_prevSnappedConnector.Constructing.Structure.Constructions, ignore: false);
		}
		_prevSnappedConnector = null;
		if (_snapped)
		{
			Quaternion rotation = placement.Rotation;
			if (_rotationMode != ERotationMode.None)
			{
				float y = Mathf.Round(yRotation / (float)_rotationAngle) * (float)_rotationAngle;
				rotation *= Quaternion.Euler(new Vector3(0f, y, 0f));
			}
			ghost.position = placement.Position;
			ghost.rotation = rotation;
		}
		else
		{
			ghost.position = placement.Position;
			ghost.rotation = placement.Rotation;
			if (_rotationMode != ERotationMode.None)
			{
				ghost.rotation *= Quaternion.Euler(new Vector3(0f, yRotation, 0f));
			}
		}
		return flag;
	}

	public virtual void Place()
	{
		IList<Connector> connectorsAtPlacement = GetConnectorsAtPlacement();
		if (true)
		{
			DoPlace(connectorsAtPlacement);
		}
		else
		{
			Dismantle();
		}
	}

	public virtual void OnCrafted(IPlayer player)
	{
		_crafted?.Invoke(player);
	}

	protected virtual IList<Connector> GetConnectorsAtPlacement()
	{
		return BeamPhysics.GetComponentsInBounds<Connector>(_colliders, _connectorLayerMask);
	}

	protected virtual void DoPlace(IList<Connector> connectors)
	{
		UpdateConnectorsAtPlacement(connectors);
		CheckStructure();
	}

	protected virtual void UpdateConnectorsAtPlacement(IList<Connector> connectors)
	{
		for (int i = 0; i < connectors.Count; i++)
		{
			Connector connector = connectors[i];
			if (_nestedPlacingCollider)
			{
				connector.Refresh(gameObject);
			}
			else
			{
				connector.Refresh();
			}
		}
		for (int j = 0; j < _myConnectors.Length; j++)
		{
			Connector connector2 = _myConnectors[j];
			if (_nestedPlacingCollider)
			{
				connector2.Refresh(gameObject);
			}
			else
			{
				connector2.Refresh();
			}
		}
	}

	protected virtual bool CheckPlacementIsSupported(IList<Connector> connectors)
	{
		return true;
	}

	public bool CheckProximity(Transform ghost)
	{
		return _proximityCheck.CheckProximity(this, ghost, _proximityDistance);
	}

	public virtual void CheckStructure()
	{
		if (_snappedConnector != null)
		{
			Structure = _snappedConnector.Constructing.Structure;
		}
		else if (Structure.IsNullOrDestroyed())
		{
			uint prefabId = StructurePrefab.GetComponent<SaveablePrefab>().PrefabId;
			Structure = MultiplayerMng.Instantiate<Construction_STRUCTURE>(prefabId, MiniGuid.NewFrom(ReferenceId), Singleton<StrandedWorld>.Instance?.NmlZone.SaveContainer);
			Structure.transform.position = transform.position;
			Structure.transform.rotation = transform.rotation;
			Structure.transform.localScale = Vector3.one;
			Structure.gameObject.layer = gameObject.layer;
			if (Game.Mode.IsServer() && Prefabs.IsMultiplayerEntity(prefabId))
			{
				ReplicateCreateConstruction replicateCreateConstruction = new ReplicateCreateConstruction();
				replicateCreateConstruction.Structure = Structure;
				replicateCreateConstruction.StructureLayer = gameObject.layer;
				replicateCreateConstruction.PrefabId = PrefabId;
				replicateCreateConstruction.ReferenceId = ReferenceId;
				replicateCreateConstruction.Position = transform.position;
				replicateCreateConstruction.Rotation = transform.rotation;
				replicateCreateConstruction.Post();
			}
		}
		transform.parent = null;
		TransformParent = Structure.transform;
		Parent();
		transform.localScale = Vector3.one;
		Structure.AddConstruction(this);
	}

	protected virtual Placement CheckPlacement(Placement placement)
	{
		if ((_previousPlacement.Position - placement.Position).magnitude > 0.2f)
		{
			_previousPlacement.Position = placement.Position;
			_previousPlacement.Rotation = placement.Rotation;
		}
		return _previousPlacement;
	}

	public bool HasMaxHealth()
	{
		return HealthPoints == MaximumHealthPoints;
	}

	public virtual void CheckHealth()
	{
		HealthPoints = Mathf.Min(HealthPoints, MaximumHealthPoints);
		if (_constructingLevels == null || _constructingLevels.Length == 0)
		{
			return;
		}
		int a = Mathf.FloorToInt((HealthPoints - 25f) / MaximumHealthPoints / 0.33f);
		a = Mathf.Max(0, Mathf.Min(a, _constructingLevels.Length - 1));
		bool flag = false;
		for (int i = 0; i < _constructingLevels.Length; i++)
		{
			ConstructingLevel obj = _constructingLevels[i];
			bool flag2 = i == a;
			obj.LevelGameObject.SetActive(flag2);
			if (!flag2)
			{
				continue;
			}
			for (int j = 0; j < _constructingLevelColliders.Length; j++)
			{
				if (_previousLevel == _constructingLevels.Length - 2 && a == _constructingLevels.Length - 1 && handlePlayerColission)
				{
					Collider[] array = Physics.OverlapBox(_constructingLevelColliders[j].bounds.center, _constructingLevelColliders[j].bounds.extents * 0.7f);
					foreach (Collider collider in array)
					{
						if (collider.gameObject.layer == Layers.PLAYER && collider.GetType() == typeof(CharacterController))
						{
							if (FindSuitableTranslatePoint(collider, out var translatePoint) && !flag)
							{
								collider.transform.Translate(translatePoint, Space.World);
								flag = true;
							}
							else
							{
								collider.transform.Translate(Vector3.up);
							}
						}
					}
				}
				_previousLevel = a;
				_constructingLevelColliders[j].isTrigger = a != _constructingLevels.Length - 1;
			}
		}
	}

	public override void Damage(float damage, IBase damager)
	{
		base.Damage(damage, damager);
		CheckHealth();
	}

	public virtual void Dismantle()
	{
		transform.CheckForProjectilesInChildren();
		gameObject.SetActive(value: false);
		MultiplayerMng.Destroy(this);
	}

	public override void Destroy()
	{
		IList<Connector> connectorsAtPlacement = GetConnectorsAtPlacement();
		Dismantle();
		UpdateConnectorsAtPlacement(connectorsAtPlacement);
		if ((bool)Structure)
		{
			Structure.RemoveConstruction(this);
		}
	}

	public int GetConnectorId(Connector connector)
	{
		if (connector == _myConnector)
		{
			return 0;
		}
		for (int i = 0; i < _myConnectors.Length; i++)
		{
			if (_myConnectors[i] == connector)
			{
				return i + 1;
			}
		}
		return -1;
	}

	public Connector GetConnectorById(int id)
	{
		if (id < 0)
		{
			return null;
		}
		if (id == 0)
		{
			return _myConnector;
		}
		return _myConnectors[id - 1];
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		if (Structure != null)
		{
			JObject jObject2 = new JObject();
			jObject2.AddField("Transform", JSerializer.Serialize(Structure.transform));
			jObject.AddField("Parent", jObject2);
		}
		JObject jObject3 = new JObject();
		for (int i = 0; i < _myConnectors.Length; i++)
		{
			try
			{
				JObject data = _myConnectors[i].Save();
				jObject3.Add(data);
			}
			catch (Exception ex)
			{
				Debug.LogError("Constructing::Load:: Error saving a connector [" + _myConnectors[i].name + "]\n" + ex);
			}
		}
		jObject.AddField("Connectors", jObject3);
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		CheckHealth();
		JObject field = data.GetField("Transform");
		Pos = JSerializer.Deserialize<Vector3>(field.GetField("localPosition"));
		Rot = JSerializer.Deserialize<Quaternion>(field.GetField("localRotation"));
		JObject field2 = data.GetField("Connectors");
		for (int i = 0; i < _myConnectors.Length; i++)
		{
			if (i >= field2.Children.Count)
			{
				Debug.LogError("Missing data for connector '" + _myConnectors[i].name + "'");
				continue;
			}
			try
			{
				JObject jObject = field2.Children[i];
				if (jObject != null)
				{
					_myConnectors[i].Load(jObject);
				}
				else
				{
					Debug.LogError("No saved data for connector [" + _myConnectors[i].name + "]");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading a connector [" + _myConnectors[i].name + "]\n" + ex);
			}
		}
	}

	private bool FindSuitableTranslatePoint(Collider hit, out Vector3 translatePoint)
	{
		CharacterController component = hit.GetComponent<CharacterController>();
		Vector3 vector = hit.transform.position + (component.bounds.size.y - component.radius) * 1.5f * Vector3.up;
		Vector3 vector2 = hit.transform.position + (component.bounds.size.y + component.radius) * 0.5f * Vector3.up;
		LayerMask physicsLayerMask = GetPhysicsLayerMask(Layers.PLAYER);
		physicsLayerMask = (int)physicsLayerMask ^ LayerMask.GetMask(LayerMask.LayerToName(Layers.PLAYER));
		Collider[] array = Physics.OverlapCapsule(vector, vector2, component.radius, physicsLayerMask);
		if (array.Length == 0)
		{
			translatePoint = Vector3.up;
			return true;
		}
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				for (int k = -1; k < 2; k++)
				{
					Vector3 vector3 = new Vector3(j, 0f, k);
					if (!Physics.CheckCapsule(vector + vector3, vector2 + vector3, component.radius, physicsLayerMask))
					{
						translatePoint = Vector3.up + vector3;
						return true;
					}
				}
			}
		}
		translatePoint = Vector3.zero;
		return false;
	}

	private LayerMask GetPhysicsLayerMask(int currentLayer)
	{
		int num = 0;
		for (int i = 0; i < 32; i++)
		{
			if (!Physics.GetIgnoreLayerCollision(currentLayer, i))
			{
				num |= 1 << i;
			}
		}
		return num;
	}
}
