using System.Collections.Generic;
using Beam.Serialization.Json;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam.Crafting;

[ExecuteInEditMode]
public class Connector : MonoBehaviour
{
	private enum EConnectMode
	{
		Free,
		RequireConnector
	}

	[SerializeField]
	private Constructing _constructing;

	private List<Constructing> _constructions = new List<Constructing>();

	[SerializeField]
	private ConnectorType _type;

	[SerializeField]
	private AttributeType _maximumTier;

	[SerializeField]
	private EConnectMode _connectMode;

	[SerializeField]
	private bool _canSet = true;

	[SerializeField]
	private bool _isValid;

	[SerializeField]
	private bool _checkType = true;

	[SerializeField]
	[Tooltip("Don't snap to collider center, used transform position instead")]
	private bool _useTransformPositionForSnapping;

	private SphereCollider _collider;

	private ConnectorCheck[] _checks;

	public Constructing Constructing
	{
		get
		{
			return _constructing;
		}
		set
		{
			_constructing = value;
		}
	}

	public IList<Constructing> Constructions => _constructions;

	public ConnectorType Type => _type;

	public AttributeType MaximumTier => _maximumTier;

	public bool CanSet => _canSet;

	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (_canSet)
			{
				_isValid = value;
			}
		}
	}

	public SphereCollider Collider
	{
		get
		{
			if (_collider == null)
			{
				_collider = GetComponent<SphereCollider>();
			}
			return _collider;
		}
	}

	public IList<ConnectorCheck> Checks
	{
		get
		{
			if (_checks == null)
			{
				_checks = GetComponents<ConnectorCheck>();
			}
			return _checks;
		}
	}

	public void Enable(bool enabled)
	{
		Collider.enabled = enabled;
	}

	public bool ConnectTo(Connector connector, Placement placement)
	{
		if (!connector)
		{
			if (_connectMode == EConnectMode.RequireConnector)
			{
				return false;
			}
			bool flag = true;
			foreach (ConnectorCheck check in Checks)
			{
				flag = check.Check(placement, null);
				if (!flag)
				{
					break;
				}
			}
			return flag;
		}
		if (!connector.IsValid)
		{
			return false;
		}
		if (_checkType && _type != connector.Type)
		{
			return false;
		}
		if (_maximumTier > AttributeType.None && _constructing.CraftingType.AttributeType > _maximumTier)
		{
			return false;
		}
		if ((_type == ConnectorType.WALL || _type == ConnectorType.DOOR) && (!connector.Constructing || !connector.Constructing.Structure))
		{
			return false;
		}
		for (int i = 0; i < Checks.Count; i++)
		{
			if (!Checks[i].Check(placement, connector))
			{
				return false;
			}
		}
		placement.Position = (_useTransformPositionForSnapping ? connector.transform.position : connector.Collider.transform.TransformPoint(connector.Collider.center));
		placement.Rotation = connector.transform.rotation;
		return true;
	}

	public void Refresh(GameObject placingObject = null)
	{
		_constructions.Clear();
		IList<Constructing> componentsInSphere = BeamPhysics.GetComponentsInSphere<Constructing>(Collider.transform.TransformPoint(Collider.center), Collider.radius, 1 << transform.parent.gameObject.layer);
		for (int i = 0; i < componentsInSphere.Count; i++)
		{
			Constructing constructing = componentsInSphere[i];
			if (constructing != _constructing)
			{
				_constructions.Add(constructing);
			}
		}
		if (componentsInSphere.Count == 0 && placingObject != null && placingObject.activeInHierarchy)
		{
			Constructing inParent = placingObject.transform.GetInParent<Constructing>();
			if (inParent != _constructing)
			{
				_constructions.Add(inParent);
			}
		}
		IsValid = _constructions.Count == 0;
	}

	public int GetLocalId()
	{
		return _constructing.GetConnectorId(this);
	}

	public JObject Save()
	{
		JObject jObject = new JObject();
		jObject.AddField("IsValid", JSerializer.Serialize(IsValid));
		jObject.AddField("Type", JSerializer.Serialize(Type));
		JObject data = JSerializer.Serialize(Collider);
		jObject.AddField(Collider.GetType().Name, data);
		if (!_isValid)
		{
			JObject jObject2 = new JObject();
			for (int i = 0; i < _constructions.Count; i++)
			{
				Constructing constructing = _constructions[i];
				jObject2.Add(JSerializer.Serialize(constructing.ReferenceId.ToString()));
			}
			jObject.AddField("Constructions", jObject2);
		}
		return jObject;
	}

	public void Load(JObject data)
	{
		JObject field = data.GetField("IsValid");
		_isValid = field.GetValue<bool>();
		if (_isValid)
		{
			return;
		}
		JObject constructionsData = data.GetField("Constructions");
		if (constructionsData == null || constructionsData.IsNull())
		{
			return;
		}
		LevelLoader.CurrentLoader.DeferredLoading.Add(() =>
		{
			foreach (JObject child in constructionsData.Children)
			{
				MiniGuid referenceId = child.GetValue<string>().ToMiniGuid();
				Constructing constructing = null;
				constructing = ((!Constructing.Structure.IsValid()) ? MultiplayerMng.FindObject<Constructing>(referenceId) : Constructing.Structure.transform.FindObject<Constructing>(referenceId));
				if (constructing != null)
				{
					_constructions.Add(constructing);
				}
				else
				{
					Debug.LogError("Connector::Load:: Error finding construction by reference [" + child?.ToString() + "]");
				}
			}
		});
	}
}
