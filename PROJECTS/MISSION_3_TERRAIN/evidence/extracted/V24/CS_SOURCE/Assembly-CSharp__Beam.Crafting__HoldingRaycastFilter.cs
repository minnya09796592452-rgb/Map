using Beam.Utilities;
using UnityEngine;

namespace Beam.Crafting;

public class HoldingRaycastFilter : IRaycastFilter
{
	private readonly Transform _holdingContainer;

	public HoldingRaycastFilter(Transform holdingContainer)
	{
		_holdingContainer = holdingContainer;
	}

	public bool Validate(RaycastHit hit)
	{
		if (_holdingContainer == null)
		{
			return true;
		}
		return !_holdingContainer.ContainsNestedChild(hit.transform);
	}
}
