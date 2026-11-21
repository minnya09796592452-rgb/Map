using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Language;
using Beam.Utilities;
using Beam.Utilities.Reflection;
using MEC;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace Beam.UI;

public class InputMenuPresenter : MonoBehaviour, IOptionPanel
{
	private enum AssignedPlayer
	{
		[Description("ASSIGNED_PLAYER_NONE", "")]
		None,
		[Description("ASSIGNED_PLAYER_P1", "")]
		P1,
		[Description("ASSIGNED_PLAYER_P2", "")]
		P2
	}

	[SerializeField]
	private OptionsMenuPresenter _optionsMenuPresenter;

	private IInputMenuViewAdapter _view;

	private IInputFieldButtonViewAdapter _currentButton;

	private DialogueBoxViewAdapter _dialogue;

	private CoroutineHandle _timeoutSequence;

	private bool _initialized;

	private List<IOptionCategoryHeader> _optionCategories = new List<IOptionCategoryHeader>();

	private IOptionButtonViewAdapter _toggleRadialMenusButton;

	private IOptionButtonViewAdapter _crouchToggleButton;

	private IOptionButtonViewAdapter _sprintToggleButton;

	private IOptionButtonViewAdapter _autowalkButton;

	private IOptionSliderViewAdapter _smoothnessSlider;

	private IOptionSliderViewAdapter _sensitivitySlider;

	private IOptionButtonViewAdapter _invertYButton;

	private Dictionary<string, ControllerAssignment> _controllerAssignments = new Dictionary<string, ControllerAssignment>();

	private bool _dirtyMappings;

	public void Initialize(IInputMenuViewAdapter view)
	{
		if (!_initialized)
		{
			_view = view;
			SetControlEvents();
			InputMapper.Default.options.timeout = 5f;
			InputMapper.Default.options.allowKeyboardKeysWithModifiers = false;
			InputMapper.Default.options.allowKeyboardModifierKeyAsPrimary = true;
			InputMapper.Default.InputMappedEvent += InputMapper_InputMapped;
			InputMapper.Default.ConflictFoundEvent += InputMapper_ConflictFound;
			InputMapper.Default.StartedEvent += InputMapper_Started;
			InputMapper.Default.StoppedEvent += InputMapper_Stopped;
			InputMapper.Default.CanceledEvent += InputMapper_Cancelled;
			InputMapper.Default.ErrorEvent += InputMapper_Error;
			InputMapper.Default.TimedOutEvent += InputMapper_TimedOut;
			_initialized = true;
		}
	}

	private void OnDestroy()
	{
		if (_initialized)
		{
			InputMapper.Default.InputMappedEvent -= InputMapper_InputMapped;
			InputMapper.Default.ConflictFoundEvent -= InputMapper_ConflictFound;
			InputMapper.Default.StartedEvent -= InputMapper_Started;
			InputMapper.Default.StoppedEvent -= InputMapper_Stopped;
			InputMapper.Default.CanceledEvent -= InputMapper_Cancelled;
			InputMapper.Default.ErrorEvent -= InputMapper_Error;
			InputMapper.Default.TimedOutEvent -= InputMapper_TimedOut;
		}
	}

	public Transform GetCategoriesContainer()
	{
		return _view.CategoriesContainer;
	}

	public void Show()
	{
		if (!_view.Visible)
		{
			_view.Show();
			_view.DefaultButton.Show();
			_optionsMenuPresenter.SetApplyButtonEvent(ApplyButton_Click);
			PopulateControls();
			_optionsMenuPresenter.ResetDirtySettings();
			ReInput.ControllerConnectedEvent += Rewired_OnControllerConnected;
			ReInput.ControllerPreDisconnectEvent += Rewired_OnControllerPreDisconnect;
		}
	}

	public void Hide()
	{
		if (_view.Visible)
		{
			if (_dirtyMappings)
			{
				Options.Load();
				_dirtyMappings = false;
			}
			_view.DefaultButton.Hide();
			_view.Hide();
			ReInput.ControllerConnectedEvent -= Rewired_OnControllerConnected;
			ReInput.ControllerPreDisconnectEvent -= Rewired_OnControllerPreDisconnect;
		}
	}

	void IOptionPanel.Apply()
	{
		ApplyButton_Click(null);
	}

	private void SetControlEvents()
	{
		_view.DefaultButton.Click += DefaultButton_Click;
	}

	private void PopulateControls()
	{
		_controllerAssignments.Clear();
		foreach (KeyValuePair<string, ControllerAssignment> controllerAssignment in Options.InputSettings.ControllerAssignments)
		{
			if (!_controllerAssignments.ContainsKey(controllerAssignment.Key))
			{
				_controllerAssignments.Add(controllerAssignment.Key, null);
			}
			_controllerAssignments[controllerAssignment.Key] = controllerAssignment.Value;
		}
		CreateOptionCategories();
	}

	private void RefreshControls()
	{
	}

	private void CreateOptionCategories()
	{
		_optionCategories.Clear();
		_optionsMenuPresenter.DestroyOptionCategoryHeaders();
		_view.DestroyInputOptionButtons();
		_view.DestroyInputOptionSliders();
		_view.DestroyInputFields();
		CreateInputOptionButtons();
		CreateControllerCategory();
		CreateInputFieldButtons();
		_view.SetNavigation(_optionCategories);
	}

	private void CreateInputOptionButtons()
	{
		IOptionCategoryHeader header = CreateInputCategoryHeader("OPTIONS_INPUT_MENU_LABEL_GENERAL");
		_toggleRadialMenusButton = CreateInputButton(header, "OPTIONS_INPUT_MENU_BUTTON_TOGGLE_RADIAL_MENUS", typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_toggleRadialMenusButton.SelectedIndex = Options.InputSettings.ToggleRadialMenus.GetIndex();
		_optionsMenuPresenter.UpdateNavigation(_toggleRadialMenusButton.transform.GetComponent<Button>());
		_crouchToggleButton = CreateInputButton(header, "OPTIONS_INPUT_MENU_BUTTON_CROUCH_TOGGLE", typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_crouchToggleButton.SelectedIndex = Options.InputSettings.CrouchToggle.GetIndex();
		_sprintToggleButton = CreateInputButton(header, "OPTIONS_INPUT_MENU_BUTTON_SPRINT_TOGGLE", typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_sprintToggleButton.SelectedIndex = Options.InputSettings.SprintToggle.GetIndex();
		_autowalkButton = CreateInputButton(header, "OPTIONS_INPUT_MENU_BUTTON_AUTO_WALK", typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_autowalkButton.SelectedIndex = Options.InputSettings.AutoWalk.GetIndex();
		IOptionCategoryHeader header2 = CreateInputCategoryHeader("OPTIONS_INPUT_MENU_LABEL_CAMERA");
		_smoothnessSlider = CreateInputSlider(header2, "OPTIONS_INPUT_MENU_BUTTON_SMOOTHNESS");
		_smoothnessSlider.Value = Options.InputSettings.MouseSmoothness;
		_smoothnessSlider.Refresh();
		_sensitivitySlider = CreateInputSlider(header2, "OPTIONS_INPUT_MENU_BUTTON_SENSITIVITY");
		_sensitivitySlider.Value = Options.InputSettings.MouseSensitivity;
		_sensitivitySlider.Refresh();
		_invertYButton = CreateInputButton(header2, "OPTIONS_INPUT_MENU_BUTTON_INVERT_Y", typeof(Quality_OFF_ON).GetAttributes<DescriptionAttribute>());
		_invertYButton.SelectedIndex = Options.InputSettings.InvertY.GetIndex();
	}

	private IOptionButtonViewAdapter CreateInputButton(IOptionCategoryHeader header, string name, IEnumerable<IOptionElementData> values)
	{
		IOptionButtonViewAdapter optionButtonViewAdapter = _view.CreateInputOptionButton(header);
		optionButtonViewAdapter.Text = name;
		optionButtonViewAdapter.Initialize(values);
		optionButtonViewAdapter.Selected += OptionWidget_Selected;
		optionButtonViewAdapter.SelectionChanged += OptionWidget_SelectionChanged;
		return optionButtonViewAdapter;
	}

	private IOptionSliderViewAdapter CreateInputSlider(IOptionCategoryHeader header, string name)
	{
		IOptionSliderViewAdapter optionSliderViewAdapter = _view.CreateInputOptionSlider(header);
		((IOptionWidgetViewAdapter)optionSliderViewAdapter).Text = name;
		optionSliderViewAdapter.Selected += OptionWidget_Selected;
		optionSliderViewAdapter.ValueChanged += OptionSlider_ValueChanged;
		return optionSliderViewAdapter;
	}

	private void CreateInputFieldButtons()
	{
		Rewired.Player player = ReInput.players.GetPlayer(0);
		IOptionCategoryHeader header = CreateInputCategoryHeader("OPTIONS_INPUT_MENU_LABEL_CONTROL_MAPPING");
		foreach (InputMapCategory userAssignableMapCategory in ReInput.mapping.UserAssignableMapCategories)
		{
			InputCategory actionCategory = ReInput.mapping.GetActionCategory(userAssignableMapCategory.name);
			if (actionCategory == null)
			{
				continue;
			}
			CreateInputCategorySubheader(header, LocalizationTerms.GetActionCategoryTerm(userAssignableMapCategory.name));
			ControllerMap firstMapInCategory = player.controllers.maps.GetFirstMapInCategory(ControllerType.Mouse, 0, userAssignableMapCategory.id);
			ControllerMap firstMapInCategory2 = player.controllers.maps.GetFirstMapInCategory(ControllerType.Keyboard, 0, userAssignableMapCategory.id);
			foreach (InputAction item in ReInput.mapping.UserAssignableActionsInCategory(actionCategory.id, sort: true))
			{
				string actionName = (string.IsNullOrEmpty(item.descriptiveName) ? item.name : item.descriptiveName);
				if (item.type == InputActionType.Button)
				{
					IOptionInputFieldButtonViewAdapter optionInputFieldButtonViewAdapter = CreateInputFieldControl(header, LocalizationTerms.GetActionTerm(actionName));
					CreateInputButton(optionInputFieldButtonViewAdapter, firstMapInCategory2, item.id, ActionElementPredicate);
					CreateInputButton(optionInputFieldButtonViewAdapter, firstMapInCategory, item.id, ActionElementPredicate);
					optionInputFieldButtonViewAdapter.SetNavigation();
					Navigation navigation = _view.DefaultButton.transform.GetComponent<Button>().navigation;
					navigation.mode = Navigation.Mode.Explicit;
					navigation.selectOnUp = optionInputFieldButtonViewAdapter.transform.GetComponent<Selectable>();
					_view.DefaultButton.transform.GetComponent<Button>().navigation = navigation;
				}
				else if (item.type == InputActionType.Axis)
				{
					IOptionInputFieldButtonViewAdapter optionInputFieldButtonViewAdapter2 = CreateInputFieldControl(header, LocalizationTerms.GetActionTerm(actionName));
					CreateInputButton(optionInputFieldButtonViewAdapter2, null, item.id, AxisRange.Full, AxisActionElementPredicate);
					CreateInputButton(optionInputFieldButtonViewAdapter2, firstMapInCategory, item.id, AxisRange.Full, AxisActionElementPredicate, onlyAxes: true);
					optionInputFieldButtonViewAdapter2.SetNavigation();
					string actionName2 = (string.IsNullOrEmpty(item.positiveDescriptiveName) ? (item.descriptiveName + " +") : item.positiveDescriptiveName);
					IOptionInputFieldButtonViewAdapter optionInputFieldButtonViewAdapter3 = CreateInputFieldControl(header, LocalizationTerms.GetActionTerm(actionName2));
					CreateInputButton(optionInputFieldButtonViewAdapter3, firstMapInCategory2, item.id, AxisRange.Positive, PositiveActionElementPredicate);
					CreateInputButton(optionInputFieldButtonViewAdapter3, firstMapInCategory, item.id, AxisRange.Positive, PositiveActionElementPredicate);
					optionInputFieldButtonViewAdapter3.SetNavigation();
					string actionName3 = (string.IsNullOrEmpty(item.negativeDescriptiveName) ? (item.descriptiveName + " -") : item.negativeDescriptiveName);
					IOptionInputFieldButtonViewAdapter optionInputFieldButtonViewAdapter4 = CreateInputFieldControl(header, LocalizationTerms.GetActionTerm(actionName3));
					CreateInputButton(optionInputFieldButtonViewAdapter4, firstMapInCategory2, item.id, AxisRange.Negative, NegativeActionElementPredicate);
					CreateInputButton(optionInputFieldButtonViewAdapter4, firstMapInCategory, item.id, AxisRange.Negative, NegativeActionElementPredicate);
					optionInputFieldButtonViewAdapter4.SetNavigation();
				}
			}
		}
	}

	private IOptionCategoryHeader CreateInputCategoryHeader(string name)
	{
		IOptionCategoryHeader optionCategoryHeader = _optionsMenuPresenter.CreateOptionCategoryHeader();
		optionCategoryHeader.Label.Text = name;
		optionCategoryHeader.Id = name.GetHashCode();
		_optionCategories.Add(optionCategoryHeader);
		return optionCategoryHeader;
	}

	private IInputMapCategoryHeader CreateInputCategorySubheader(IOptionCategoryHeader header, string name)
	{
		IInputMapCategoryHeader inputMapCategoryHeader = _view.CreateInputCategorySubheader(header);
		inputMapCategoryHeader.Label.Text = name;
		return inputMapCategoryHeader;
	}

	private IOptionInputFieldButtonViewAdapter CreateInputFieldControl(IOptionCategoryHeader header, string name)
	{
		IOptionInputFieldButtonViewAdapter optionInputFieldButtonViewAdapter = _view.CreateInputField(header);
		optionInputFieldButtonViewAdapter.Text = name;
		optionInputFieldButtonViewAdapter.Selected += OptionWidget_Selected;
		return optionInputFieldButtonViewAdapter;
	}

	private IInputFieldButtonViewAdapter CreateInputButton(IOptionInputFieldButtonViewAdapter inputFieldOptionButton, ControllerMap controllerMap, int actionId, Func<ActionElementMap, int, bool> predicate)
	{
		return CreateInputButton(inputFieldOptionButton, controllerMap, actionId, AxisRange.Full, predicate);
	}

	private IInputFieldButtonViewAdapter CreateInputButton(IOptionInputFieldButtonViewAdapter inputFieldOptionButton, ControllerMap controllerMap, int actionId, AxisRange axisRange, Func<ActionElementMap, int, bool> predicate, bool onlyAxes = false)
	{
		ActionElementMap actionElementMap = null;
		bool flag = controllerMap != null;
		if (flag)
		{
			foreach (ActionElementMap allMap in controllerMap.AllMaps)
			{
				if (predicate(allMap, actionId))
				{
					actionElementMap = allMap;
					break;
				}
			}
		}
		IInputFieldButtonViewAdapter inputFieldButtonViewAdapter;
		if (actionElementMap != null)
		{
			bool flag2 = actionElementMap.elementIdentifierId > -1 && !actionElementMap.elementIdentifierName.Equals("NONE", StringComparison.InvariantCultureIgnoreCase);
			inputFieldButtonViewAdapter = inputFieldOptionButton.CreateButton();
			inputFieldButtonViewAdapter.Text = (flag2 ? actionElementMap.elementIdentifierName : "...");
			inputFieldButtonViewAdapter.Type = (flag2 ? InputFieldButtonType.ReassignOrRemove : InputFieldButtonType.Add);
			inputFieldButtonViewAdapter.Info = new InputFieldButtonInfo
			{
				ControllerMap = controllerMap,
				ActionId = actionId,
				ActionRange = axisRange,
				ActionElementMapId = actionElementMap.id,
				AllowOnlyAxesMapping = onlyAxes
			};
		}
		else
		{
			inputFieldButtonViewAdapter = inputFieldOptionButton.CreateButton();
			if (flag)
			{
				inputFieldButtonViewAdapter.Text = "...";
				inputFieldButtonViewAdapter.Type = InputFieldButtonType.Add;
				inputFieldButtonViewAdapter.Info = new InputFieldButtonInfo
				{
					ControllerMap = controllerMap,
					ActionId = actionId,
					ActionRange = axisRange,
					AllowOnlyAxesMapping = onlyAxes
				};
			}
			else
			{
				inputFieldButtonViewAdapter.Text = string.Empty;
				inputFieldButtonViewAdapter.Enabled = false;
			}
		}
		inputFieldButtonViewAdapter.Click += InputFieldButton_Click;
		return inputFieldButtonViewAdapter;
	}

	private void CreateControllerCategory()
	{
		IOptionCategoryHeader header = CreateInputCategoryHeader("OPTIONS_INPUT_MENU_LABEL_CONTROLLERS");
		IEnumerable<IOptionElementData> attributes = typeof(AssignedPlayer).GetAttributes<DescriptionAttribute>();
		IOptionButtonViewAdapter optionButtonViewAdapter = CreateInputButton(header, "OPTIONS_INPUT_MENU_BUTTON_KEYBOARD_MOUSE", attributes);
		optionButtonViewAdapter.SelectedIndex = AssignedPlayer.P1.GetIndex();
		optionButtonViewAdapter.Enabled = false;
		Joystick[] joysticks = ReInput.controllers.GetJoysticks();
		Joystick[] array = joysticks;
		foreach (Joystick joystick in array)
		{
			JoystickOptionButton joystickOptionButton = CreateJoystickOptionButton(joystick);
			if (joysticks.Length == 1 && _optionsMenuPresenter.CurrentPlayerInput.id == 1)
			{
				joystickOptionButton.Enabled = false;
			}
		}
		IOptionCategoryHeader header2 = CreateInputCategoryHeader("OPTIONS_INPUT_MENU_LABEL_CONTROLLER_MAPPING");
		_view.CreateControllerLayoutImage(header2).Selected += OptionWidget_Selected;
	}

	private JoystickOptionButton CreateJoystickOptionButton(Joystick joystick)
	{
		IOptionCategoryHeader header = _optionCategories.First((IOptionCategoryHeader x) => x.Id == "OPTIONS_INPUT_MENU_LABEL_CONTROLLERS".GetHashCode());
		IEnumerable<IOptionElementData> enumerable = typeof(AssignedPlayer).GetAttributes<DescriptionAttribute>();
		if (_optionsMenuPresenter.CurrentPlayerInput.id == 1 && ReInput.controllers.joystickCount > 1)
		{
			enumerable = enumerable.Where((IOptionElementData x) => x.Text != "ASSIGNED_PLAYER_NONE");
		}
		IOptionButtonViewAdapter optionButtonViewAdapter = CreateInputButton(header, joystick.name, enumerable);
		string text = joystick.deviceInstanceGuid.ToString();
		JoystickOptionButton joystickOptionButton = new JoystickOptionButton(optionButtonViewAdapter, text);
		joystickOptionButton.SelectionChanged += JoystickOptionButton_SelectionChanged;
		AssignedPlayer assignedPlayer = AssignedPlayer.None;
		if (_controllerAssignments.TryGetValue(text, out var value))
		{
			assignedPlayer = (AssignedPlayer)(value.PlayerId + 1);
		}
		joystickOptionButton.SelectedIndex = assignedPlayer.GetIndex();
		_view.Buttons.Remove(optionButtonViewAdapter);
		_view.Buttons.Add(joystickOptionButton);
		return joystickOptionButton;
	}

	private void Rewired_OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		if (args.controllerType == ControllerType.Joystick)
		{
			Joystick joystick = args.controller as Joystick;
			CreateJoystickOptionButton(joystick);
		}
	}

	private void Rewired_OnControllerPreDisconnect(ControllerStatusChangedEventArgs args)
	{
		if (args.controllerType != ControllerType.Joystick)
		{
			return;
		}
		string value = args.controller.deviceInstanceGuid.ToString();
		foreach (IOptionButtonViewAdapter button in _view.Buttons)
		{
			if (button is JoystickOptionButton joystickOptionButton && joystickOptionButton.DeviceInstanceId.Equals(value))
			{
				_view.RemoveButton(joystickOptionButton);
				break;
			}
		}
	}

	private void ApplyButton_Click(object sender)
	{
		Options.InputSettings.ToggleRadialMenus = (Quality_OFF_ON)_toggleRadialMenusButton.SelectedIndex;
		Options.InputSettings.CrouchToggle = (Quality_OFF_ON)_crouchToggleButton.SelectedIndex;
		Options.InputSettings.SprintToggle = (Quality_OFF_ON)_sprintToggleButton.SelectedIndex;
		Options.InputSettings.AutoWalk = (Quality_OFF_ON)_autowalkButton.SelectedIndex;
		Options.InputSettings.MouseSmoothness = _smoothnessSlider.Value;
		Options.InputSettings.MouseSensitivity = _sensitivitySlider.Value;
		Options.InputSettings.InvertY = (Quality_OFF_ON)_invertYButton.SelectedIndex;
		Options.InputSettings.ControllerAssignments = new Dictionary<string, ControllerAssignment>(_controllerAssignments);
		_optionsMenuPresenter.Save();
	}

	private void DefaultButton_Click(object sender)
	{
		DialogueBox.Show(ReInput.players.GetPlayer(0), "OPTIONS_INPUT_MENU_DB_RESET_TITLE", "OPTIONS_INPUT_MENU_DB_RESET_DESC", DialogueButton.YesNo).Accept += () =>
		{
			LoadDefaults();
			PopulateControls();
			_optionsMenuPresenter.SetDirtySettings();
			_optionsMenuPresenter.ResetDirtySettings();
		};
	}

	private void OptionWidget_Selected(ISelectableViewAdapter widget)
	{
		_view.Scrollview.FocusElement(widget.transform);
	}

	private void OptionWidget_SelectionChanged(IOptionWidgetViewAdapter widget)
	{
		_optionsMenuPresenter.SetDirtySettings();
	}

	private void OptionSlider_ValueChanged(float value)
	{
		_optionsMenuPresenter.SetDirtySettings();
	}

	private void JoystickOptionButton_SelectionChanged(IOptionButtonViewAdapter button)
	{
		if (!(button is JoystickOptionButton joystickOptionButton))
		{
			return;
		}
		Joystick joystick = ReInput.controllers.Joysticks.FirstOrDefault_NonAlloc((Joystick joy, string arg) => joy.isConnected && joy.deviceInstanceGuid.ToString() == arg, joystickOptionButton.DeviceInstanceId);
		if (joystick == null)
		{
			Debug.LogError("JoystickOptionButton_SelectionChanged:: No matching joystick found for [" + joystickOptionButton.Text + "].");
			return;
		}
		AssignedPlayer selectedIndex = (AssignedPlayer)button.SelectedIndex;
		if (selectedIndex == AssignedPlayer.None)
		{
			UnassignController(joystick.id);
			return;
		}
		int playerId = (int)(selectedIndex - 1);
		AssignController(playerId, joystick.id);
	}

	private void InputFieldButton_Click(object sender)
	{
		IInputFieldButtonViewAdapter inputFieldButton = sender as IInputFieldButtonViewAdapter;
		if (inputFieldButton == null || _dialogue != null)
		{
			return;
		}
		InputMapper.Context context = new InputMapper.Context();
		context.controllerMap = inputFieldButton.Info.ControllerMap;
		context.actionId = inputFieldButton.Info.ActionId;
		context.actionRange = inputFieldButton.Info.ActionRange;
		context.actionElementMapToReplace = inputFieldButton.Info.ControllerMap.GetElementMap(inputFieldButton.Info.ActionElementMapId);
		bool allowOnlyAxes = inputFieldButton.Info.AllowOnlyAxesMapping;
		InputMapper.Default.options.allowButtons = !allowOnlyAxes;
		_currentButton = inputFieldButton;
		switch (_currentButton.Type)
		{
		case InputFieldButtonType.Add:
			StartMapping(context, allowOnlyAxes);
			break;
		case InputFieldButtonType.ReassignOrRemove:
		{
			DialogueBoxViewAdapter dialogueBoxViewAdapter = DialogueBox.Show(ReInput.players.GetPlayer(0), "OPTIONS_INPUT_MENU_DB_REASSIGN_TITLE", "OPTIONS_INPUT_MENU_DB_REASSIGN_DESC", "OPTIONS_INPUT_MENU_DB_BUTTON_REASSIGN", "OPTIONS_INPUT_MENU_DB_BUTTON_REMOVE", "OPTIONS_INPUT_MENU_DB_BUTTON_CANCEL");
			dialogueBoxViewAdapter.Accept += () =>
			{
				StartMapping(context, allowOnlyAxes);
			};
			dialogueBoxViewAdapter.Decline += () =>
			{
				ClearMapping(inputFieldButton);
				_currentButton.Select();
				_currentButton = null;
			};
			break;
		}
		}
	}

	private void StartMapping(InputMapper.Context context, bool allowOnlyAxes)
	{
		string remappingDialogueMessage = GetRemappingDialogueMessage(context.controllerMap.controllerType, allowOnlyAxes);
		_dialogue = DialogueBox.Show(ReInput.players.GetPlayer(0), "OPTIONS_INPUT_MENU_DB_ASSIGN_TITLE", remappingDialogueMessage, DialogueButton.None);
		InputMapper.Default.Start(context);
	}

	private void ClearMapping(IInputFieldButtonViewAdapter inputFieldButton)
	{
		ActionElementMap elementMap = inputFieldButton.Info.ControllerMap.GetElementMap(inputFieldButton.Info.ActionElementMapId);
		if (ClearElementMapAssignment(elementMap))
		{
			inputFieldButton.Text = "...";
			inputFieldButton.Type = InputFieldButtonType.Add;
			ClearMatchingElementMapsFromControllers(elementMap);
			_optionsMenuPresenter.SetDirtySettings();
		}
	}

	private void BeginRemapping()
	{
		_timeoutSequence = Timing.RunCoroutine(InputMapper_TimeoutSequence());
		ReInput.players.GetPlayer(0).SetAndCacheMapsEnabled(state: false, 8);
	}

	private void EndMapping()
	{
		if (_timeoutSequence.IsRunning)
		{
			Timing.KillCoroutines(_timeoutSequence);
		}
		if (_dialogue != null)
		{
			_dialogue.Destroy();
			_dialogue = null;
		}
		if (_currentButton != null)
		{
			_currentButton.Select();
			_currentButton = null;
		}
		ReInput.players.GetPlayer(0).SetAndCacheMapsEnabled(state: true, 8);
	}

	private void InputMapper_Started(InputMapper.StartedEventData eventData)
	{
		BeginRemapping();
	}

	private void InputMapper_Stopped(InputMapper.StoppedEventData eventData)
	{
		EndMapping();
	}

	private void InputMapper_Cancelled(InputMapper.CanceledEventData eventData)
	{
		EndMapping();
	}

	private void InputMapper_Error(InputMapper.ErrorEventData eventData)
	{
		EndMapping();
	}

	private void InputMapper_TimedOut(InputMapper.TimedOutEventData eventData)
	{
	}

	private void InputMapper_InputMapped(InputMapper.InputMappedEventData eventData)
	{
		_currentButton.Text = eventData.actionElementMap.elementIdentifierName;
		_currentButton.Type = InputFieldButtonType.ReassignOrRemove;
		_currentButton.Info.ActionElementMapId = eventData.actionElementMap.id;
		_dirtyMappings = true;
		_optionsMenuPresenter.SetDirtySettings();
		RemapMatchingElementMapsInControllers(eventData.actionElementMap);
	}

	private void RemapMatchingElementMapsInControllers(ActionElementMap newActionElementMap)
	{
		ElementAssignment elementAssignment = new ElementAssignment(newActionElementMap.controllerMap.controllerType, newActionElementMap.elementType, newActionElementMap.elementIdentifierId, newActionElementMap.axisRange, newActionElementMap.keyCode, newActionElementMap.modifierKeyFlags, newActionElementMap.actionId, newActionElementMap.axisContribution, invert: false);
		foreach (ControllerMap allMap in newActionElementMap.controllerMap.player.controllers.maps.GetAllMaps(newActionElementMap.controllerMap.controllerType))
		{
			foreach (ActionElementMap item in from aem in allMap.GetElementMapsWithAction(newActionElementMap.actionId)
				where aem.elementIdentifierId != newActionElementMap.elementIdentifierId && aem.axisContribution == newActionElementMap.axisContribution
				select aem)
			{
				elementAssignment.elementMapId = item.id;
				_ = item.elementIdentifierName;
				allMap.ReplaceElementMap(elementAssignment, out var _);
			}
		}
	}

	private void ClearMatchingElementMapsFromControllers(ActionElementMap removedActionElementMap)
	{
		foreach (ControllerMap item in from map in removedActionElementMap.controllerMap.player.controllers.maps.GetAllMaps()
			where map.controllerType == removedActionElementMap.controllerMap.controllerType
			select map)
		{
			foreach (ActionElementMap item2 in from aem in item.GetElementMapsWithAction(removedActionElementMap.actionId)
				where aem.elementIdentifierId != 0 && aem.axisContribution == removedActionElementMap.axisContribution
				select aem)
			{
				ClearElementMapAssignment(item2);
			}
		}
	}

	private bool ClearElementMapAssignment(ActionElementMap actionElementMap)
	{
		_ = ReInput.mapping.GetAction(actionElementMap.actionId).descriptiveName;
		_ = actionElementMap.elementIdentifierName;
		_ = ReInput.mapping.GetMapCategory(actionElementMap.controllerMap.categoryId).name;
		ElementAssignment elementAssignment = new ElementAssignment(actionElementMap.controllerMap.controllerType, actionElementMap.elementType, -1, actionElementMap.axisRange, KeyCode.None, ModifierKeyFlags.None, actionElementMap.actionId, actionElementMap.axisContribution, invert: false);
		elementAssignment.elementMapId = actionElementMap.id;
		if (!actionElementMap.controllerMap.ReplaceElementMap(elementAssignment, out var _))
		{
			return false;
		}
		return true;
	}

	private void InputMapper_ConflictFound(InputMapper.ConflictFoundEventData eventData)
	{
		Debug.LogError("InputMenu::InputMapper_ConflictFound::");
		eventData.responseCallback(InputMapper.ConflictResponse.Cancel);
		eventData.inputMapper.Stop();
		string text = Localization.GetLanguageHandler().Localize("OPTIONS_INPUT_MENU_DB_CONFLICT_DESC", eventData.assignment.elementDisplayName, eventData.assignment.action.descriptiveName);
		ElementAssignmentConflictInfo elementAssignmentConflictInfo = eventData.conflicts.FirstOrDefault_NonAlloc();
		text = text + "[" + elementAssignmentConflictInfo.elementDisplayName + " - " + elementAssignmentConflictInfo.action.descriptiveName + "]";
		DialogueBox.Show(ReInput.players.GetPlayer(0), "OPTIONS_INPUT_MENU_DB_CONFLICT_TITLE", text, DialogueButton.Ok);
	}

	private void LoadDefaults()
	{
		Rewired.Player player = ReInput.players.GetPlayer(0);
		player.controllers.maps.LoadDefaultMaps(ControllerType.Mouse);
		player.controllers.maps.LoadDefaultMaps(ControllerType.Keyboard);
		Options.InputSettings = new InputSettings();
		Options.Save();
		Options.Load();
	}

	private void AssignController(int playerId, int controllerId)
	{
		Joystick joystick = ReInput.controllers.GetJoystick(controllerId);
		if (joystick == null)
		{
			Debug.LogError("Something went wrong. Controller id not found. Disconnected?");
			return;
		}
		ControllerAssignment controllerAssignment = new ControllerAssignment();
		controllerAssignment.ControllerName = joystick.name;
		controllerAssignment.ControllerGuid = joystick.deviceInstanceGuid.ToString();
		controllerAssignment.PlayerId = playerId;
		if (!_controllerAssignments.ContainsKey(controllerAssignment.ControllerGuid))
		{
			_controllerAssignments.Add(controllerAssignment.ControllerGuid, null);
		}
		_controllerAssignments[controllerAssignment.ControllerGuid] = controllerAssignment;
	}

	private void UnassignController(int controllerId)
	{
		Joystick joystick = ReInput.controllers.GetJoystick(controllerId);
		if (joystick == null)
		{
			Debug.LogError("Something went wrong. Controller id not found. Disconnected?");
		}
		else
		{
			_controllerAssignments.Remove(joystick.deviceInstanceGuid.ToString());
		}
	}

	public bool ActionElementPredicate(ActionElementMap actionElementMap, int actionId)
	{
		if (actionElementMap != null)
		{
			return actionElementMap.actionId == actionId;
		}
		return false;
	}

	public bool AxisActionElementPredicate(ActionElementMap actionElementMap, int actionId)
	{
		if (actionElementMap != null && actionElementMap.actionId == actionId && actionElementMap.elementType != ControllerElementType.Button)
		{
			return actionElementMap.axisType != AxisType.Split;
		}
		return false;
	}

	public bool PositiveActionElementPredicate(ActionElementMap actionElementMap, int actionId)
	{
		if (actionElementMap != null && actionElementMap.actionId == actionId && actionElementMap.axisType != AxisType.Normal)
		{
			return actionElementMap.axisContribution == Pole.Positive;
		}
		return false;
	}

	public bool NegativeActionElementPredicate(ActionElementMap actionElementMap, int actionId)
	{
		if (actionElementMap != null && actionElementMap.actionId == actionId && actionElementMap.axisType != AxisType.Normal)
		{
			return actionElementMap.axisContribution == Pole.Negative;
		}
		return false;
	}

	private string GetRemappingDialogueMessage(ControllerType controllerType, bool onlyAxesAllowed)
	{
		switch (controllerType)
		{
		default:
			return "OPTIONS_INPUT_MENU_DB_ASSIGN_DESC_BUTTON_OR_AXIS";
		case ControllerType.Mouse:
			if (!onlyAxesAllowed)
			{
				return "OPTIONS_INPUT_MENU_DB_ASSIGN_DESC_MOUSE_BUTTON_OR_AXIS";
			}
			return "OPTIONS_INPUT_MENU_DB_ASSIGN_DESC_AXIS";
		case ControllerType.Keyboard:
			return "OPTIONS_INPUT_MENU_DB_ASSIGN_DESC_ANY_KEY";
		}
	}

	private IEnumerator<float> InputMapper_TimeoutSequence()
	{
		string message = _dialogue.Message.Text;
		while (true)
		{
			_dialogue.Message.Text = $"{message} ({Mathf.CeilToInt(InputMapper.Default.timeRemaining)})";
			yield return 0f;
		}
	}

	private Rewired.Player GetPlayer(AssignedPlayer value)
	{
		try
		{
			switch (value)
			{
			case AssignedPlayer.P1:
				return RewiredUtilities.Players[0];
			case AssignedPlayer.P2:
				return RewiredUtilities.Players[1];
			}
		}
		catch (IndexOutOfRangeException exception)
		{
			Debug.LogException(exception);
			return null;
		}
		return null;
	}
}
