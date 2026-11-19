using System.Collections.Generic;
using UnityEngine;

namespace Beam.UI;

public class NullRadialMenuElement : NullObject<NullRadialMenuElement>, IRadialMenuElement<NullRadialMenuModel>
{
	public Transform transform => NullTransform.Instance;

	public NullRadialMenuModel SelectedValue => NullObject<NullRadialMenuModel>.Instance;

	public void Initialize(IList<NullRadialMenuModel> data)
	{
		Debug.LogError("NullRadialMenuElement::Initialize:: Not implemented!");
	}

	public void Enter()
	{
		Debug.LogError("NullRadialMenuElement::Enter:: Not implemented!");
	}

	public void Exit()
	{
		Debug.LogError("NullRadialMenuElement::Exit:: Not implemented!");
	}

	public bool NavigateLeft()
	{
		Debug.LogError("NullRadialMenuElement::NavigateLeft:: Not implemented!");
		return false;
	}

	public bool NavigateRight()
	{
		Debug.LogError("NullRadialMenuElement::NavigateRight:: Not implemented!");
		return false;
	}
}
