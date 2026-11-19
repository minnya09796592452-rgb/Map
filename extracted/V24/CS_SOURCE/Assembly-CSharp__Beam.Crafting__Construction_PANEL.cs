using System.Collections.Generic;
using Beam.Serialization.Json;
using UnityEngine;

namespace Beam.Crafting;

public class Construction_PANEL : Constructing, IHaveAttribute
{
	[Header("ATTRIBUTE")]
	[SerializeField]
	private IconType _type = IconType.Construction;

	[SerializeField]
	private GameObject _cap;

	[SerializeField]
	private Transform _leftCapPosition;

	[SerializeField]
	private Transform _rightCapPosition;

	private bool _hasLeftCap;

	private bool _hasRightCap;

	IconType IHaveAttribute.Type => _type;

	float IHaveAttribute.GetValue()
	{
		return HealthPoints / MaximumHealthPoints;
	}

	protected override void DoPlace(IList<Connector> connectors)
	{
		base.DoPlace(connectors);
		CheckForCaps();
	}

	protected override void UpdateConnectorsAtPlacement(IList<Connector> connectors)
	{
		for (int i = 0; i < connectors.Count; i++)
		{
			Connector connector = connectors[i];
			if (!(connector.Constructing == this) || connector.Type != ConnectorType.DOOR)
			{
				connector.Refresh();
			}
		}
		for (int j = 0; j < MyConnectors.Length; j++)
		{
			Connector connector2 = MyConnectors[j];
			if (connector2.Type != ConnectorType.DOOR)
			{
				connector2.Refresh();
			}
		}
	}

	protected override bool CheckPlacementIsSupported(IList<Connector> connectors)
	{
		foreach (Connector connector in connectors)
		{
			if (connector.Constructing is Construction_FOUNDATION)
			{
				return true;
			}
		}
		foreach (Connector connector2 in connectors)
		{
			if ((bool)connector2.Constructing.SnappedConnector && connector2.Constructing.SnappedConnector.Constructing is Construction_FOUNDATION)
			{
				return true;
			}
		}
		return false;
	}

	private void CheckForCaps()
	{
		if (SnappedConnector.Constructing.ColliderType != GhostCollider.ColliderType.WALL_HALF)
		{
			if (!Physics.CheckSphere(_leftCapPosition.position - new Vector3(0f, 0.14f, 0f), 0.14f, 1 << Layers.CONSTRUCTIONS))
			{
				CreateCap(_leftCapPosition.position);
				_hasLeftCap = true;
			}
			if (!Physics.CheckSphere(_rightCapPosition.position - new Vector3(0f, 0.14f, 0f), 0.14f, 1 << Layers.CONSTRUCTIONS))
			{
				CreateCap(_rightCapPosition.position);
				_hasRightCap = true;
			}
		}
	}

	private void CreateCap(Vector3 position)
	{
		GameObject gameObject = Object.Instantiate(_cap, position, Quaternion.identity);
		gameObject.transform.parent = transform;
		gameObject.transform.localRotation = Quaternion.identity;
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		jObject.AddField("HasLeftCap", JSerializer.Serialize(_hasLeftCap));
		jObject.AddField("HasRightCap", JSerializer.Serialize(_hasRightCap));
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		JObject field = data.GetField("HasLeftCap");
		_hasLeftCap = field.GetValue<bool>();
		JObject field2 = data.GetField("HasRightCap");
		_hasRightCap = field2.GetValue<bool>();
		if (_hasLeftCap)
		{
			CreateCap(_leftCapPosition.position);
		}
		if (_hasRightCap)
		{
			CreateCap(_rightCapPosition.position);
		}
	}
}
