using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LE_LevelEditor.UI;

public class LE_GUIInterface_uGUIimplBase : MonoBehaviour
{
	protected RectTransform m_transform;

	protected virtual void Start()
	{
		if (LE_GUIInterface.Instance != null)
		{
			LE_GUIInterface.Delegates delegates = LE_GUIInterface.Instance.delegates;
			delegates.IsCursorOverUI = (LE_GUIInterface.Delegates.GetBoolFunction)Delegate.Combine(delegates.IsCursorOverUI, new LE_GUIInterface.Delegates.GetBoolFunction(IsCursorOverUI));
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimplBase: Start: could not find LE_GUIInterface!");
		}
		m_transform = (RectTransform)transform;
		if (m_transform == null)
		{
			Debug.LogError("LE_GUIInterface_uGUIimplBase: this script must be attached to a RectTransform!");
		}
	}

	protected virtual bool IsCursorOverUI()
	{
		return EventSystem.main.IsPointerOverGameObject();
	}
}
