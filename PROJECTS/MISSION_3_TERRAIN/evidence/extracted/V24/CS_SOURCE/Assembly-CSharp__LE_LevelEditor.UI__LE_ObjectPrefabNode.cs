using Beam.UI;
using LE_LevelEditor.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LE_LevelEditor.UI;

public class LE_ObjectPrefabNode : MonoBehaviour
{
	public class SendMessageInitData
	{
		public readonly LE_Object m_object;

		public readonly int m_indentLevel;

		public SendMessageInitData(LE_Object p_object, int p_indentLevel)
		{
			m_object = p_object;
			m_indentLevel = p_indentLevel;
		}
	}

	[SerializeField]
	private TMPLabelViewAdapter m_text;

	[SerializeField]
	private RawImage m_image;

	[SerializeField]
	private Image m_selectionImage;

	public RawImage Image => m_image;

	public void uMyGUI_TreeBrowser_InitNode(object p_data)
	{
		if (m_text != null && m_image != null)
		{
			if (p_data is SendMessageInitData)
			{
				SendMessageInitData sendMessageInitData = (SendMessageInitData)p_data;
				Texture2D texture2D = (Texture2D)Resources.Load("WorldEditor/ObjectIcons/" + sendMessageInitData.m_object.name);
				if (texture2D != null)
				{
					m_image.texture = texture2D;
				}
				else
				{
					Debug.LogError("LE_ObjectPrefabNode: uMyGUI_TreeBrowser_InitNode: object '" + sendMessageInitData.m_object.name);
				}
				m_text.Text = sendMessageInitData.m_object.EditorObjectName;
				Vector2 offsetMax = ((RectTransform)m_text.transform).offsetMax;
				offsetMax.x -= (float)sendMessageInitData.m_indentLevel * 10f;
				((RectTransform)m_text.transform).offsetMax = offsetMax;
			}
			else if (p_data is LE_ObjectCategoryNode.SendMessageInitData)
			{
				LE_ObjectCategoryNode.SendMessageInitData sendMessageInitData2 = (LE_ObjectCategoryNode.SendMessageInitData)p_data;
				m_image.gameObject.SetActive(value: false);
				m_text.Text = LE_ObjectCategoryNode.GetCleanCategoryName(sendMessageInitData2.m_categoryName);
			}
			else
			{
				Debug.LogError("LE_ObjectPrefabNode: uMyGUI_TreeBrowser_InitNode: expected p_data to be a LE_ObjectPrefabNode.SendMessageInitData! p_data: " + p_data);
			}
		}
		else
		{
			Debug.LogError("LE_ObjectPrefabNode: uMyGUI_TreeBrowser_InitNode: m_text or m_image were not set via inspector!");
		}
	}

	public void OnSelected()
	{
		LE_ObjectDragDrop componentInParent = GetComponentInParent<LE_ObjectDragDrop>();
		if (componentInParent != null)
		{
			componentInParent.OnObjectSelected(this);
		}
		else
		{
			Debug.LogError("LE_ObjectPrefabNode: OnSelected: could not find LE_ObjectDragDrop in parents!");
		}
		ShowSelection();
	}

	public void ShowSelection()
	{
		if (m_selectionImage != null)
		{
			m_selectionImage.color = Color.green;
		}
		if (m_text != null)
		{
			m_text.Color = Color.green;
		}
	}

	public void HideSelection()
	{
		if (m_selectionImage != null)
		{
			m_selectionImage.color = Color.white;
		}
		if (m_text != null)
		{
			m_text.Color = Color.white;
		}
	}
}
