using System;
using System.Collections.Generic;
using Beam;
using Beam.Language;
using Beam.UI;
using Beam.Utilities;
using LE_LevelEditor.Core;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using uMyGUI;

namespace LE_LevelEditor.UI;

public class LE_GUIInterface_uGUIimpl : LE_GUIInterface_uGUIimplBase
{
	private LE_GUI3dTerrain m_GUI3dTerrain;

	[SerializeField]
	private float CAM_GIZMO_RIGHT_PIXEL_OFFSET = 265f;

	[SerializeField]
	private float CAM_OBLIQUE_RIGHT_PIXEL_OFFSET = 250f;

	[SerializeField]
	private uMyGUI_TexturePicker TERRAIN_BRUSH_PICKER;

	[SerializeField]
	private uMyGUI_TexturePicker TERRAIN_HEIGHTMAP_PICKER;

	[SerializeField]
	private Slider TERRAIN_EDIT_BRUSH_SIZE_SLIDER;

	[SerializeField]
	private Slider TERRAIN_EDIT_BRUSH_AMOUNT_SLIDER;

	[SerializeField]
	private Slider TERRAIN_EDIT_BRUSH_TARGET_VALUE_SLIDER_HEIGHT;

	[SerializeField]
	private Button TERRAIN_READ_SET_HEIGHT_BTN;

	[SerializeField]
	private RectTransform TERRAIN_MENU;

	[SerializeField]
	private string TERRAIN_MENU_FADE_IN_ANIM = "TabRotatedFadeIn";

	[SerializeField]
	private RectTransform TERRAIN_UI_MODE_EDIT;

	[SerializeField]
	private uMyGUI_TreeBrowser OBJECT_BROWSER;

	[SerializeField]
	private uMyGUI_Draggable OBJECT_PREVIEW_DRAGGABLE;

	[SerializeField]
	private RawImage OBJECT_PREVIEW_IMAGE;

	[SerializeField]
	private TMPLabelViewAdapter OBJECT_PREVIEW_MESSAGE;

	[SerializeField]
	private Button OBJECT_FOCUS_SELECTED_BTN;

	[SerializeField]
	private Button OBJECT_DUPLICATE_SELECTED_BTN;

	[SerializeField]
	private Button OBJECT_DELETE_SELECTED_BTN;

	[SerializeField]
	private Button TERRAIN_GENERATE_PERLIN;

	[SerializeField]
	private Slider TERRAIN_GENERATE_PERLIN_FREQ;

	[SerializeField]
	private Slider TERRAIN_GENERATE_PERLIN_AMPLITUDE;

	[SerializeField]
	private Slider TERRAIN_GENERATE_PERLIN_OCTAVES;

	[SerializeField]
	private Slider TERRAIN_GENERATE_PERLIN_BLEND;

	[SerializeField]
	private Button TERRAIN_GENERATE_NORMALISE;

	[SerializeField]
	private Slider TERRAIN_GENERATE_NORMALISE_MIN;

	[SerializeField]
	private Slider TERRAIN_GENERATE_NORMALISE_MAX;

	[SerializeField]
	private Slider TERRAIN_GENERATE_NORMALISE_BLEND;

	[SerializeField]
	private Button TERRAIN_SMOOTH;

	[SerializeField]
	private AudioSource BUTTON_CLICK_AUDIO_SOURCE;

	private Dictionary<uMyGUI_TreeBrowser.Node, LE_Object> m_nodeToObject = new Dictionary<uMyGUI_TreeBrowser.Node, LE_Object>();

	protected override void Start()
	{
		base.Start();
		if (LE_GUIInterface.Instance != null)
		{
			LE_GUIInterface.Delegates delegates = LE_GUIInterface.Instance.delegates;
			delegates.GetCameraPerspectiveGizmoRightPixelOffset = (LE_GUIInterface.Delegates.GetFloatFunction)Delegate.Combine(delegates.GetCameraPerspectiveGizmoRightPixelOffset, new LE_GUIInterface.Delegates.GetFloatFunction(GetCameraPerspectiveGizmoRightPixelOffset));
			LE_GUIInterface.Delegates delegates2 = LE_GUIInterface.Instance.delegates;
			delegates2.GetObliqueCameraPerspectiveRightPixelOffset = (LE_GUIInterface.Delegates.GetFloatFunction)Delegate.Combine(delegates2.GetObliqueCameraPerspectiveRightPixelOffset, new LE_GUIInterface.Delegates.GetFloatFunction(GetObliqueCameraPerspectiveRightPixelOffset));
			LE_GUIInterface.Delegates delegates3 = LE_GUIInterface.Instance.delegates;
			delegates3.SetTerrainUIMode = (Action<LE_GUIInterface.Delegates.ETerrainUIMode>)Delegate.Combine(delegates3.SetTerrainUIMode, new Action<LE_GUIInterface.Delegates.ETerrainUIMode>(SetTerrainUIMode));
			LE_GUIInterface.Delegates delegates4 = LE_GUIInterface.Instance.delegates;
			delegates4.SetTerrainEditBrushSize = (Action<float>)Delegate.Combine(delegates4.SetTerrainEditBrushSize, new Action<float>(SetTerrainEditBrushSize));
			LE_GUIInterface.Delegates delegates5 = LE_GUIInterface.Instance.delegates;
			delegates5.SetTerrainEditBrushAmount = (Action<float>)Delegate.Combine(delegates5.SetTerrainEditBrushAmount, new Action<float>(SetTerrainEditBrushAmount));
			LE_GUIInterface.Delegates delegates6 = LE_GUIInterface.Instance.delegates;
			delegates6.SetTerrainEditBrushTargetValue = (Action<float>)Delegate.Combine(delegates6.SetTerrainEditBrushTargetValue, new Action<float>(SetTerrainEditBrushTargetValue));
			LE_GUIInterface.Delegates delegates7 = LE_GUIInterface.Instance.delegates;
			delegates7.SetTerrainIsReadingSetHeight = (Action<bool>)Delegate.Combine(delegates7.SetTerrainIsReadingSetHeight, new Action<bool>(SetTerrainIsReadingSetHeight));
			LE_GUIInterface.Delegates delegates8 = LE_GUIInterface.Instance.delegates;
			delegates8.SetObjects = (Action<LE_ObjectMap>)Delegate.Combine(delegates8.SetObjects, new Action<LE_ObjectMap>(SetObjects));
			LE_GUIInterface.Delegates delegates9 = LE_GUIInterface.Instance.delegates;
			delegates9.IsObjectDragged = (LE_GUIInterface.Delegates.GetBoolFunction)Delegate.Combine(delegates9.IsObjectDragged, new LE_GUIInterface.Delegates.GetBoolFunction(IsObjectDragged));
			LE_GUIInterface.Delegates delegates10 = LE_GUIInterface.Instance.delegates;
			delegates10.SetDraggableObjectState = (Action<LE_GUIInterface.Delegates.EDraggedObjectState>)Delegate.Combine(delegates10.SetDraggableObjectState, new Action<LE_GUIInterface.Delegates.EDraggedObjectState>(SetDraggableObjectState));
			LE_GUIInterface.Delegates delegates11 = LE_GUIInterface.Instance.delegates;
			delegates11.SetDraggableObjectMessage = (Action<string>)Delegate.Combine(delegates11.SetDraggableObjectMessage, new Action<string>(SetDraggableObjectMessage));
			LE_GUIInterface.Delegates delegates12 = LE_GUIInterface.Instance.delegates;
			delegates12.SetIsSelectedObjectFocusBtnInteractable = (Action<bool>)Delegate.Combine(delegates12.SetIsSelectedObjectFocusBtnInteractable, new Action<bool>(SetIsSelectedObjectFocusBtnInteractable));
			LE_GUIInterface.Delegates delegates13 = LE_GUIInterface.Instance.delegates;
			delegates13.SetIsSelectedObjectDuplicateBtnInteractable = (Action<bool>)Delegate.Combine(delegates13.SetIsSelectedObjectDuplicateBtnInteractable, new Action<bool>(SetIsSelectedObjectDuplicateBtnInteractable));
			LE_GUIInterface.Delegates delegates14 = LE_GUIInterface.Instance.delegates;
			delegates14.SetIsSelectedObjectDeleteBtnInteractable = (Action<bool>)Delegate.Combine(delegates14.SetIsSelectedObjectDeleteBtnInteractable, new Action<bool>(SetIsSelectedObjectDeleteBtnInteractable));
			LE_GUIInterface.EventHandlers events = LE_GUIInterface.Instance.events;
			events.OnTerrainGenerateShoremap = (EventHandler)Delegate.Combine(events.OnTerrainGenerateShoremap, new EventHandler(OnTerrainGenerateShoremap));
			LE_GUIInterface.EventHandlers events2 = LE_GUIInterface.Instance.events;
			events2.OnTerrainGenerateCustomHeightmap = (EventHandler)Delegate.Combine(events2.OnTerrainGenerateCustomHeightmap, new EventHandler(OnTerrainGenerateCustomHeightmap));
			LE_GUIInterface.EventHandlers events3 = LE_GUIInterface.Instance.events;
			events3.OnTerrainGeneratePerlin = (EventHandler)Delegate.Combine(events3.OnTerrainGeneratePerlin, new EventHandler(OnTerrainGeneratePerlin));
			LE_GUIInterface.EventHandlers events4 = LE_GUIInterface.Instance.events;
			events4.OnTerrainGenerateNormalise = (EventHandler)Delegate.Combine(events4.OnTerrainGenerateNormalise, new EventHandler(OnTerrainGenerateNormalise));
			LE_GUIInterface.EventHandlers events5 = LE_GUIInterface.Instance.events;
			events5.OnTerrainGenerateIsland = (EventHandler)Delegate.Combine(events5.OnTerrainGenerateIsland, new EventHandler(OnTerrainGenerateIsland));
			LE_GUIInterface.EventHandlers events6 = LE_GUIInterface.Instance.events;
			events6.OnTerrainSmooth = (EventHandler)Delegate.Combine(events6.OnTerrainSmooth, new EventHandler(OnTerrainSmooth));
			SetupTexturePickerEvents("TERRAIN_BRUSH_PICKER", TERRAIN_BRUSH_PICKER, ref LE_GUIInterface.Instance.delegates.SetTerrainBrushes, LE_GUIInterface.Instance.OnTerrainBrushChanged, ref LE_GUIInterface.Instance.events.OnTerrainBrushChanged);
			SetupTexturePickerEvents("TERRAIN_HEIGHTMAP_PICKER", TERRAIN_HEIGHTMAP_PICKER, ref LE_GUIInterface.Instance.delegates.SetTerrainHeightmapTextures, LE_GUIInterface.Instance.OnTerrainHeightmapTextureChanged, ref LE_GUIInterface.Instance.events.OnTerrainHeightmapTextureChanged);
			LE_GUIInterface.Delegates delegates15 = LE_GUIInterface.Instance.delegates;
			delegates15.ShowPopupConfirmDeleteObject = (Action<Action<bool>>)Delegate.Combine(delegates15.ShowPopupConfirmDeleteObject, new Action<Action<bool>>(ShowPopupConfirmDeleteObject));
		}
		if (OBJECT_PREVIEW_DRAGGABLE == null)
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: OBJECT_PREVIEW_DRAGGABLE was not set in the inspector -> drag&drop will not work!");
		}
		if (OBJECT_PREVIEW_IMAGE == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: OBJECT_PREVIEW_IMAGE was not set in the inspector!");
		}
		if (OBJECT_PREVIEW_MESSAGE == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: OBJECT_PREVIEW_MESSAGE was not set in the inspector!");
		}
		if (OBJECT_FOCUS_SELECTED_BTN == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: OBJECT_FOCUS_SELECTED_BTN was not set in the inspector!");
		}
		if (OBJECT_DUPLICATE_SELECTED_BTN == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: OBJECT_DUPLICATE_SELECTED_BTN was not set in the inspector!");
		}
		if (OBJECT_DELETE_SELECTED_BTN == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: OBJECT_DELETE_SELECTED_BTN was not set in the inspector!");
		}
		if (BUTTON_CLICK_AUDIO_SOURCE == null)
		{
			Debug.LogWarning("LE_GUIInterface_uGUIimpl: BUTTON_CLICK_AUDIO_SOURCE was not set in the inspector!");
		}
	}

	private Rewired.Player GetPlayerInput()
	{
		return ReInput.players.GetPlayer(0);
	}

	private void OnTerrainGenerateCustomHeightmap(object p_obj, EventArgs p_args)
	{
		if (Singleton<LE_LevelEditorMain>.Instance.GUI3dTerrain.Heightmap == null)
		{
			DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_TITLE", "MAP_EDITOR_DB_NO_SELECTION_DESC", DialogueButton.Ok, DialogueIcon.Warning);
			return;
		}
		DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_TITLE", "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_DESC", DialogueButton.YesNo, DialogueIcon.Warning).Accept += () =>
		{
			if ((bool)Singleton<LE_ZoneTileGenerator>.Instance)
			{
				Singleton<LE_ZoneTileGenerator>.Instance.terrain.terrainData.SetHeights(0, 0, Singleton<LE_LevelEditorMain>.Instance.GUI3dTerrain.Heightmap.Heights);
			}
		};
	}

	private void OnTerrainGeneratePerlin(object p_obj, EventArgs p_args)
	{
		DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_TITLE", "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_DESC", DialogueButton.YesNo, DialogueIcon.Warning).Accept += () =>
		{
			DialogueBoxViewAdapter dialogue = DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATING_HEIGHTMAP_TITLE", "MAP_EDITOR_DB_GENERATING_HEIGHTMAP_DESC", DialogueButton.None, DialogueIcon.Information);
			BeamTeam.InvokeDelayed(0.5f, () =>
			{
				TerrainGeneratePerlin();
				dialogue.Destroy();
			});
		};
	}

	private void TerrainGeneratePerlin()
	{
		if (Singleton<LE_ZoneTileGenerator>.Instance != null)
		{
			Singleton<LE_ZoneTileGenerator>.Instance.GenerateAdvancedPerlin((int)TERRAIN_GENERATE_PERLIN_FREQ.value, TERRAIN_GENERATE_PERLIN_AMPLITUDE.value, (int)TERRAIN_GENERATE_PERLIN_OCTAVES.value, TERRAIN_GENERATE_PERLIN_BLEND.value);
		}
	}

	private void OnTerrainGenerateNormalise(object p_obj, EventArgs p_args)
	{
		DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_TITLE", "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_DESC", DialogueButton.YesNo, DialogueIcon.Warning).Accept += () =>
		{
			if (Singleton<LE_ZoneTileGenerator>.Instance != null)
			{
				Singleton<LE_ZoneTileGenerator>.Instance.GenerateAdvancedNormalise(TERRAIN_GENERATE_NORMALISE_MIN.value, TERRAIN_GENERATE_NORMALISE_MAX.value, TERRAIN_GENERATE_NORMALISE_BLEND.value);
			}
		};
	}

	private void OnTerrainGenerateIsland(object p_obj, EventArgs p_args)
	{
		DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_GENERATE_ISLAND_TITLE", "MAP_EDITOR_DB_GENERATE_HEIGHTMAP_DESC", DialogueButton.YesNo, DialogueIcon.Warning).Accept += () =>
		{
			if (Singleton<LE_ZoneTileGenerator>.Instance != null)
			{
				Singleton<LE_ZoneTileGenerator>.Instance.GenerateRandomIsland();
			}
		};
	}

	private void OnTerrainGenerateShoremap(object p_obj, EventArgs p_args)
	{
		if (Singleton<LE_ZoneTileGenerator>.Instance != null)
		{
			Singleton<LE_ZoneTileGenerator>.Instance.GenerateShoreMap();
		}
	}

	private void OnTerrainSmooth(object p_obj, EventArgs p_args)
	{
		DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_SMOOTH_TERRAIN_TITLE", "MAP_EDITOR_DB_SMOOTH_TERRAIN_DESC", DialogueButton.YesNo, DialogueIcon.Warning).Accept += () =>
		{
			if (Singleton<LE_ZoneTileGenerator>.Instance != null)
			{
				Singleton<LE_ZoneTileGenerator>.Instance.GenerateSmooth();
			}
		};
	}

	private void SetupTexturePickerEvents(string p_errorPickerName, uMyGUI_TexturePicker p_texPicker, ref Action<Texture2D[], int> r_setTexturesDelegate, Action<int> p_buttonCallback, ref EventHandler<LE_GUIInterface.EventHandlers.IntEventArgs> r_onChangedEvent)
	{
		if (p_texPicker != null)
		{
			r_setTexturesDelegate = (Action<Texture2D[], int>)Delegate.Combine(r_setTexturesDelegate, (Action<Texture2D[], int>)((Texture2D[] p_textures, int p_selectedIndex) =>
			{
				p_texPicker.SetTextures(p_textures, p_selectedIndex);
				AddButtonClickSoundsToGeneratedUI(p_texPicker.transform as RectTransform);
			}));
			p_texPicker.ButtonCallback = p_buttonCallback;
			r_onChangedEvent = (EventHandler<LE_GUIInterface.EventHandlers.IntEventArgs>)Delegate.Combine(r_onChangedEvent, (EventHandler<LE_GUIInterface.EventHandlers.IntEventArgs>)((object p_obj, LE_GUIInterface.EventHandlers.IntEventArgs p_args) =>
			{
				if (p_texPicker != null)
				{
					p_texPicker.SetSelection(p_args.Value);
				}
			}));
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: " + p_errorPickerName + " is not set in inspector!");
		}
	}

	private float GetCameraPerspectiveGizmoRightPixelOffset()
	{
		return CAM_GIZMO_RIGHT_PIXEL_OFFSET * ((float)Screen.height / m_transform.rect.height);
	}

	private float GetObliqueCameraPerspectiveRightPixelOffset()
	{
		return CAM_OBLIQUE_RIGHT_PIXEL_OFFSET * ((float)Screen.height / m_transform.rect.height);
	}

	private void SetTerrainUIMode(LE_GUIInterface.Delegates.ETerrainUIMode p_mode)
	{
		if (TERRAIN_UI_MODE_EDIT != null)
		{
			bool flag = p_mode == LE_GUIInterface.Delegates.ETerrainUIMode.EDIT;
			TERRAIN_UI_MODE_EDIT.gameObject.SetActive(flag);
			if (flag)
			{
				TERRAIN_UI_MODE_EDIT.gameObject.SendMessage("uMyGUI_OnActivateTab", SendMessageOptions.DontRequireReceiver);
			}
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainUIMode: TERRAIN_UI_MODE_EDIT is not set in inspector!");
		}
		if (TERRAIN_MENU != null && !string.IsNullOrEmpty(TERRAIN_MENU_FADE_IN_ANIM) && TERRAIN_MENU.gameObject.activeInHierarchy)
		{
			Animation component = TERRAIN_MENU.GetComponent<Animation>();
			if (component != null)
			{
				component.Play(TERRAIN_MENU_FADE_IN_ANIM);
			}
		}
		else if (TERRAIN_MENU == null)
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainUIMode: TERRAIN_MENU is not set in inspector!");
		}
	}

	private void SetTerrainEditBrushSize(float p_brushSize)
	{
		if (TERRAIN_EDIT_BRUSH_SIZE_SLIDER != null)
		{
			TERRAIN_EDIT_BRUSH_SIZE_SLIDER.value = p_brushSize;
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainEditBrushSize: TERRAIN_EDIT_BRUSH_SIZE_SLIDER is not set in inspector!");
		}
	}

	private void SetTerrainEditBrushAmount(float p_brushAmount)
	{
		if (TERRAIN_EDIT_BRUSH_AMOUNT_SLIDER != null)
		{
			TERRAIN_EDIT_BRUSH_AMOUNT_SLIDER.value = p_brushAmount;
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainEditBrushAmount: TERRAIN_EDIT_BRUSH_AMOUNT_SLIDER is not set in inspector!");
		}
	}

	private void SetTerrainEditBrushTargetValue(float p_brushTargetValue)
	{
		if (TERRAIN_EDIT_BRUSH_TARGET_VALUE_SLIDER_HEIGHT != null)
		{
			TERRAIN_EDIT_BRUSH_TARGET_VALUE_SLIDER_HEIGHT.value = p_brushTargetValue;
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainEditBrushTargetValue: TERRAIN_EDIT_BRUSH_TARGET_VALUE_SLIDER_HEIGHT is not set in inspector!");
		}
	}

	private void SetTerrainIsReadingSetHeight(bool p_isReadingSetHeight)
	{
		if (TERRAIN_READ_SET_HEIGHT_BTN != null)
		{
			TERRAIN_READ_SET_HEIGHT_BTN.interactable = !p_isReadingSetHeight;
			TextMeshProUGUI component = TERRAIN_READ_SET_HEIGHT_BTN.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
			string term = (p_isReadingSetHeight ? "MAP_EDITOR_TERRAIN_BUTTON_CLICK_TO_READ_TERRAIN_HEIGHT" : "MAP_EDITOR_TERRAIN_BUTTON_READ_TERRAIN_PAINT_HEIGHT");
			component.text = Localization.GetLanguageHandler().Localize(term);
			component.color = (p_isReadingSetHeight ? Color.red : (Color.white * 0.196f + Color.black));
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetTerrainIsReadingSetHeight: TERRAIN_READ_SET_HEIGHT_BTN is not set in inspector!");
		}
	}

	private void SetObjects(LE_ObjectMap p_objectMap)
	{
		if (OBJECT_BROWSER != null)
		{
			OBJECT_BROWSER.BuildTree(SetObjectsRecursive(p_objectMap, 0));
			AddButtonClickSoundsToGeneratedUI(OBJECT_BROWSER.transform as RectTransform);
			uMyGUI_TreeBrowser oBJECT_BROWSER = OBJECT_BROWSER;
			oBJECT_BROWSER.OnInnerNodeClick = (EventHandler<uMyGUI_TreeBrowser.NodeClickEventArgs>)Delegate.Combine(oBJECT_BROWSER.OnInnerNodeClick, (EventHandler<uMyGUI_TreeBrowser.NodeClickEventArgs>)((object p_object, uMyGUI_TreeBrowser.NodeClickEventArgs p_args) =>
			{
				AddButtonClickSoundsToGeneratedUI(OBJECT_BROWSER.transform as RectTransform);
			}));
			uMyGUI_TreeBrowser oBJECT_BROWSER2 = OBJECT_BROWSER;
			oBJECT_BROWSER2.OnLeafNodeClick = (EventHandler<uMyGUI_TreeBrowser.NodeClickEventArgs>)Delegate.Combine(oBJECT_BROWSER2.OnLeafNodeClick, (EventHandler<uMyGUI_TreeBrowser.NodeClickEventArgs>)((object p_object, uMyGUI_TreeBrowser.NodeClickEventArgs p_args) =>
			{
				if (!m_nodeToObject.TryGetValue(p_args.ClickedNode, out var value))
				{
					Debug.LogError("LE_GUIInterface_uGUIimpl: could not find selected node's object!");
				}
				if (value != null)
				{
					LE_GUIInterface.Instance.OnObjectSelectDraggable(value);
				}
			}));
		}
		else
		{
			Debug.LogError("LE_GUIInterface_uGUIimpl: SetObjects: OBJECT_BROWSER is not set in inspector!");
		}
	}

	private uMyGUI_TreeBrowser.Node[] SetObjectsRecursive(LE_ObjectMap p_objectMap, int p_indentLevel)
	{
		List<uMyGUI_TreeBrowser.Node> list = new List<uMyGUI_TreeBrowser.Node>();
		foreach (LE_ObjectMap subObjectMap in p_objectMap.SubObjectMaps)
		{
			if (subObjectMap != null)
			{
				LE_ObjectCategoryNode.SendMessageInitData p_sendMessageData = new LE_ObjectCategoryNode.SendMessageInitData(subObjectMap.CategoryName, p_indentLevel);
				list.Add(new uMyGUI_TreeBrowser.Node(p_sendMessageData, SetObjectsRecursive(subObjectMap, p_indentLevel + 1)));
			}
		}
		foreach (LE_Object objectPrefab in p_objectMap.ObjectPrefabs)
		{
			if (objectPrefab != null)
			{
				uMyGUI_TreeBrowser.Node node = new uMyGUI_TreeBrowser.Node(new LE_ObjectPrefabNode.SendMessageInitData(objectPrefab, p_indentLevel), null);
				list.Add(node);
				m_nodeToObject.Add(node, objectPrefab);
			}
			else
			{
				Debug.LogError("LE_GUIInterface_uGUIimpl: SetObjectsRecursive: object from map '" + p_objectMap.CategoryName + "' at index '' is null!");
			}
		}
		return list.ToArray();
	}

	private bool IsObjectDragged()
	{
		if (OBJECT_PREVIEW_DRAGGABLE != null)
		{
			return OBJECT_PREVIEW_DRAGGABLE.IsDragged;
		}
		return false;
	}

	private void SetDraggableObjectState(LE_GUIInterface.Delegates.EDraggedObjectState p_state)
	{
		if (OBJECT_PREVIEW_IMAGE != null)
		{
			switch (p_state)
			{
			case LE_GUIInterface.Delegates.EDraggedObjectState.NOT_PLACEABLE:
				OBJECT_PREVIEW_IMAGE.color = Color.red;
				break;
			case LE_GUIInterface.Delegates.EDraggedObjectState.IN_3D_PREVIEW:
				OBJECT_PREVIEW_IMAGE.color = Color.clear;
				break;
			default:
				OBJECT_PREVIEW_IMAGE.color = Color.white;
				break;
			}
		}
	}

	private void SetDraggableObjectMessage(string p_message)
	{
		if (OBJECT_PREVIEW_MESSAGE != null)
		{
			OBJECT_PREVIEW_MESSAGE.Text = p_message;
		}
	}

	private void SetIsSelectedObjectFocusBtnInteractable(bool p_isInteractable)
	{
		if (OBJECT_FOCUS_SELECTED_BTN != null)
		{
			OBJECT_FOCUS_SELECTED_BTN.interactable = p_isInteractable;
		}
	}

	private void SetIsSelectedObjectDuplicateBtnInteractable(bool p_isInteractable)
	{
		if (OBJECT_DUPLICATE_SELECTED_BTN != null)
		{
			OBJECT_DUPLICATE_SELECTED_BTN.interactable = p_isInteractable;
		}
	}

	private void SetIsSelectedObjectDeleteBtnInteractable(bool p_isInteractable)
	{
		if (OBJECT_DELETE_SELECTED_BTN != null)
		{
			OBJECT_DELETE_SELECTED_BTN.interactable = p_isInteractable;
		}
	}

	private void ShowPopupConfirmDeleteObject(Action<bool> p_confirmCallback)
	{
		DialogueBoxViewAdapter dialogueBoxViewAdapter = DialogueBox.Show(GetPlayerInput(), "MAP_EDITOR_DB_DELETE_OBJECT_TITLE", "MAP_EDITOR_DB_DELETE_OBJECT_DESC", DialogueButton.YesNo);
		dialogueBoxViewAdapter.Accept += () =>
		{
			p_confirmCallback(obj: true);
		};
		dialogueBoxViewAdapter.Cancel += () =>
		{
			p_confirmCallback(obj: false);
		};
	}

	private void AddButtonClickSoundsToGeneratedUI(RectTransform p_root)
	{
		if (p_root != null && BUTTON_CLICK_AUDIO_SOURCE != null)
		{
			Button[] componentsInChildren = p_root.GetComponentsInChildren<Button>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].onClick.RemoveListener(OnButtonClickSound);
				componentsInChildren[i].onClick.AddListener(OnButtonClickSound);
			}
			Toggle[] componentsInChildren2 = p_root.GetComponentsInChildren<Toggle>(includeInactive: true);
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].onValueChanged.RemoveListener(OnToggleClickSound);
				componentsInChildren2[j].onValueChanged.AddListener(OnToggleClickSound);
			}
		}
	}

	private void OnButtonClickSound()
	{
		if (BUTTON_CLICK_AUDIO_SOURCE != null)
		{
			BUTTON_CLICK_AUDIO_SOURCE.Play();
		}
	}

	private void OnToggleClickSound(bool p_isOn)
	{
		if (BUTTON_CLICK_AUDIO_SOURCE != null)
		{
			BUTTON_CLICK_AUDIO_SOURCE.Play();
		}
	}
}
