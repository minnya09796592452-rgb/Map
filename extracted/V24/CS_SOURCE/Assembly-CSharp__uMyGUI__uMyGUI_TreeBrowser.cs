using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace uMyGUI;

public class uMyGUI_TreeBrowser : MonoBehaviour
{
	public class Node
	{
		public readonly object SendMessageData;

		public readonly Node[] Children;

		public Node(object p_sendMessageData, Node[] p_children)
		{
			SendMessageData = p_sendMessageData;
			Children = p_children;
		}
	}

	public class NodeClickEventArgs : EventArgs
	{
		public readonly Node ClickedNode;

		public NodeClickEventArgs(Node p_clickedNode)
		{
			ClickedNode = p_clickedNode;
		}
	}

	private class InternalNode
	{
		public readonly Node m_node;

		public GameObject m_instance;

		public int m_indentLevel;

		public RectTransform m_transform;

		public bool m_isFoldout;

		public float m_minY;

		public InternalNode(Node p_node, GameObject p_instance, int p_indentLevel)
		{
			m_node = p_node;
			m_instance = p_instance;
			m_indentLevel = p_indentLevel;
			m_transform = m_instance.GetComponent<RectTransform>();
		}
	}

	[SerializeField]
	private GameObject m_innerNodePrefab;

	[SerializeField]
	private GameObject m_leafNodePrefab;

	[SerializeField]
	private float m_offsetStart;

	[SerializeField]
	private float m_offsetEnd;

	[SerializeField]
	private float m_padding = 4f;

	[SerializeField]
	private float m_indentSize = 20f;

	public EventHandler<NodeClickEventArgs> OnInnerNodeClick;

	public EventHandler<NodeClickEventArgs> OnLeafNodeClick;

	private RectTransform m_rectTransform;

	private List<InternalNode> m_nodes = new List<InternalNode>();

	public GameObject InnerNodePrefab
	{
		get
		{
			return m_innerNodePrefab;
		}
		set
		{
			m_innerNodePrefab = value;
		}
	}

	public GameObject LeafNodePrefab
	{
		get
		{
			return m_leafNodePrefab;
		}
		set
		{
			m_leafNodePrefab = value;
		}
	}

	public float OffsetStart
	{
		get
		{
			return m_offsetStart;
		}
		set
		{
			m_offsetStart = value;
		}
	}

	public float OffsetEnd
	{
		get
		{
			return m_offsetEnd;
		}
		set
		{
			m_offsetEnd = value;
		}
	}

	public float Padding
	{
		get
		{
			return m_padding;
		}
		set
		{
			m_padding = value;
		}
	}

	public float IndentSize
	{
		get
		{
			return m_indentSize;
		}
		set
		{
			m_indentSize = value;
		}
	}

	private RectTransform RTransform
	{
		get
		{
			if (!(m_rectTransform != null))
			{
				return m_rectTransform = GetComponent<RectTransform>();
			}
			return m_rectTransform;
		}
	}

	public void BuildTree(Node[] p_rootNodes)
	{
		BuildTree(p_rootNodes, 0, 0);
	}

	public void BuildTree(Node[] p_rootNodes, int p_insertAt, int p_indentLevel)
	{
		if (m_innerNodePrefab != null && m_leafNodePrefab != null)
		{
			List<InternalNode> list = new List<InternalNode>();
			float num = 0f;
			for (int i = 0; i < p_rootNodes.Length; i++)
			{
				if (p_rootNodes[i] == null)
				{
					continue;
				}
				int num2;
				GameObject gameObject;
				if (p_rootNodes[i].Children != null)
				{
					num2 = ((p_rootNodes[i].Children.Length != 0) ? 1 : 0);
					if (num2 != 0)
					{
						gameObject = UnityEngine.Object.Instantiate(m_innerNodePrefab);
						goto IL_0073;
					}
				}
				else
				{
					num2 = 0;
				}
				gameObject = UnityEngine.Object.Instantiate(m_leafNodePrefab);
				goto IL_0073;
				IL_0073:
				if (p_rootNodes[i].SendMessageData != null)
				{
					if (!gameObject.activeInHierarchy)
					{
						Debug.LogError("uMyGUI_TreeBrowser: BuildTree: node has SendMessageData set, but instance is inactive! SendMessage call will fail! Make your prefab active!");
					}
					gameObject.SendMessage("uMyGUI_TreeBrowser_InitNode", p_rootNodes[i].SendMessageData);
				}
				InternalNode internalNode = new InternalNode(p_rootNodes[i], gameObject, p_indentLevel);
				list.Add(internalNode);
				if (num2 != 0)
				{
					SetupInnerNode(internalNode);
				}
				else
				{
					SetupLeafNode(internalNode);
				}
				RectTransform component = gameObject.GetComponent<RectTransform>();
				float height = component.rect.height;
				SetRectTransformPosition(component, p_insertAt + i, height, p_indentLevel);
				internalNode.m_minY = component.anchoredPosition.y - height;
				num = internalNode.m_minY;
			}
			if (p_insertAt < m_nodes.Count)
			{
				float p_moveDist = ((p_insertAt != 0) ? (num - m_nodes[p_insertAt - 1].m_minY) : num);
				UpdateNodePosition(p_insertAt, p_moveDist);
			}
			if (p_insertAt < m_nodes.Count)
			{
				m_nodes.InsertRange(p_insertAt, list);
			}
			else
			{
				m_nodes.AddRange(list);
			}
			if (m_nodes.Count > 0)
			{
				RTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(m_nodes[m_nodes.Count - 1].m_minY - RTransform.rect.yMax - m_offsetEnd));
			}
		}
		else
		{
			Debug.LogError("uMyGUI_TreeBrowser: BuildTree: you must provide the InnerNodePrefab and LeafNodePrefab in the inspector or via script!");
		}
	}

	private void SetRectTransformPosition(RectTransform p_transform, int p_positionIndex, float p_size, int p_indentLevel)
	{
		p_transform.SetParent(RTransform, worldPositionStays: false);
		Vector2 anchoredPosition = p_transform.anchoredPosition;
		anchoredPosition.x += (float)p_indentLevel * m_indentSize;
		anchoredPosition.y -= m_offsetStart + (float)p_positionIndex * (p_size + m_padding);
		p_transform.anchoredPosition = anchoredPosition;
	}

	private void UpdateNodePosition(int p_startIndex, float p_moveDist)
	{
		for (int i = p_startIndex; i < m_nodes.Count; i++)
		{
			Vector2 anchoredPosition = m_nodes[i].m_transform.anchoredPosition;
			anchoredPosition.y += p_moveDist;
			m_nodes[i].m_transform.anchoredPosition = anchoredPosition;
			m_nodes[i].m_minY = anchoredPosition.y - m_nodes[i].m_transform.rect.height;
		}
	}

	private void SetupInnerNode(InternalNode p_node)
	{
		if (p_node.m_instance.GetComponent<Button>() != null)
		{
			p_node.m_instance.GetComponent<Button>().onClick.AddListener(() =>
			{
				ToggleInnerNodeFoldout(p_node);
			});
		}
		else if (p_node.m_instance.GetComponent<Toggle>() != null)
		{
			p_node.m_instance.GetComponent<Toggle>().onValueChanged.AddListener((bool p_isOn) =>
			{
				ToggleInnerNodeFoldout(p_node);
			});
		}
		else
		{
			Debug.LogError("uMyGUI_TreeBrowser: BuildTree: the inner node prefabs must have either a Button or a Toggle script attached to the root. Otherwise they cannot fold out!");
		}
	}

	private void SetupLeafNode(InternalNode p_node)
	{
		if (p_node.m_instance.GetComponent<Button>() != null)
		{
			p_node.m_instance.GetComponent<Button>().onClick.AddListener(() =>
			{
				SafeCallOnLeafNodeClick(p_node);
			});
		}
		else if (p_node.m_instance.GetComponent<Toggle>() != null)
		{
			p_node.m_instance.GetComponent<Toggle>().onValueChanged.AddListener((bool p_isOn) =>
			{
				SafeCallOnLeafNodeClick(p_node);
			});
		}
	}

	private void ToggleInnerNodeFoldout(InternalNode p_node)
	{
		int num = m_nodes.IndexOf(p_node);
		p_node.m_isFoldout = !p_node.m_isFoldout;
		if (p_node.m_isFoldout)
		{
			BuildTree(p_node.m_node.Children, num + 1, p_node.m_indentLevel + 1);
		}
		else
		{
			float num2 = 0f;
			for (int i = 0; i < p_node.m_node.Children.Length; i++)
			{
				int index = num + p_node.m_node.Children.Length - i;
				InternalNode internalNode = m_nodes[index];
				num2 += internalNode.m_transform.rect.height;
				if (internalNode.m_isFoldout)
				{
					ToggleInnerNodeFoldout(internalNode);
				}
				m_nodes.RemoveAt(index);
				UnityEngine.Object.Destroy(internalNode.m_instance);
			}
			UpdateNodePosition(num + 1, num2);
			RTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RTransform.sizeDelta.y - num2);
		}
		if (OnInnerNodeClick != null)
		{
			OnInnerNodeClick(this, new NodeClickEventArgs(p_node.m_node));
		}
	}

	private void SafeCallOnLeafNodeClick(InternalNode p_node)
	{
		if (OnLeafNodeClick != null)
		{
			OnLeafNodeClick(this, new NodeClickEventArgs(p_node.m_node));
		}
	}
}
