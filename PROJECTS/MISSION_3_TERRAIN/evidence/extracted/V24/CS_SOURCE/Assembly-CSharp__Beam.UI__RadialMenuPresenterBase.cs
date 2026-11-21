using System;
using System.Collections.Generic;
using Beam.Utilities;
using Rewired;
using SharpNeatLib.Maths;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.UI;

public abstract class RadialMenuPresenterBase<TModel, TElement, TView> : ControllerBase where TModel : class, IRadialMenuModel where TElement : MonoBehaviour, IRadialMenuElement<TModel> where TView : RadialMenuViewAdapterBase<TModel, TElement>
{
	public enum EElementDistributionType
	{
		None,
		Even,
		FanAroundNorth
	}

	[SerializeField]
	private TView _view;

	protected Rewired.Player Input;

	[SerializeField]
	private float _sensitivity = 7f;

	[SerializeField]
	private int _elementLayoutCount = 8;

	[SerializeField]
	private EElementDistributionType _elementsDistributionType = EElementDistributionType.Even;

	[SerializeField]
	private float _angleSteps;

	[SerializeField]
	private float _elementRadius = 240f;

	[SerializeField]
	private float _selectorRadius;

	[SerializeField]
	private float _rotationAngle;

	[Range(1f, 100f)]
	[SerializeField]
	private float _angleThreshold = 100f;

	[Range(0f, 1f)]
	[SerializeField]
	private float _activationPercentage = 0.7f;

	[SerializeField]
	private float _easeTime = 0.15f;

	[SerializeField]
	private LeanTweenType _easeInType = LeanTweenType.easeOutCubic;

	[SerializeField]
	private LeanTweenType _easeOutType = LeanTweenType.easeInCubic;

	private IList<IList<TModel>> _models;

	private Stack<TElement> _elementsStack = new Stack<TElement>();

	private List<TElement> _activeElements = new List<TElement>();

	private List<TElement> _extraElements = new List<TElement>();

	private TElement _currentElement;

	private HashSet<int> _reservedIndexes = new HashSet<int>();

	private Vector3 _selectorMove;

	private Vector3 _selectingPosition;

	private bool _animating;

	private bool _active;

	private int _fadeInTweenId;

	private int _fadeOutTweenId;

	private bool _secondaryPressTime;

	private float _secondaryPressTimer;

	private FastRandom _random = new FastRandom();

	[SerializeField]
	private UnityEvent _opened;

	[SerializeField]
	private UnityEvent _closed;

	public TView View => _view;

	public virtual string Title { get; set; } = string.Empty;

	public int ElementLayoutCount
	{
		get
		{
			return _elementLayoutCount;
		}
		set
		{
			_elementLayoutCount = value;
		}
	}

	protected List<TElement> ActiveElements => _activeElements;

	protected TElement CurrentElement => _currentElement;

	public event Action<IRadialMenuElement<TModel>> SelectionChanged;

	public event Action<IRadialMenuElement<TModel>> ElementClick;

	public event Action<IRadialMenuElement<TModel>> ElementSecondaryClick;

	public event Action<IRadialMenuElement<TModel>> ElementSecondaryShortPress;

	protected virtual void OnSelectionChanged(IRadialMenuElement<TModel> element)
	{
		SelectionChanged?.Invoke(element);
	}

	protected virtual void OnElementClick(IRadialMenuElement<TModel> element)
	{
		ElementClick?.Invoke(element);
	}

	protected virtual void OnElementSecondaryClick(IRadialMenuElement<TModel> element)
	{
		ElementSecondaryClick?.Invoke(element);
	}

	protected virtual void OnElementSecondaryShortPress(IRadialMenuElement<TModel> element)
	{
		ElementSecondaryShortPress?.Invoke(element);
	}

	protected virtual void Start()
	{
		if (_view == null)
		{
			Debug.LogError(GetType()?.ToString() + ":: No view component assigned!");
			_view = NullRadialMenuViewAdapterBase.Instance as TView;
		}
		CreateElements();
		Hide();
	}

	protected virtual void OnDestroy()
	{
		SelectionChanged = null;
		ElementClick = null;
		ElementSecondaryClick = null;
		ElementSecondaryShortPress = null;
	}

	public override void SetView(ViewAdapterBase view)
	{
		_view = view as TView;
		if (_view == null)
		{
			Debug.LogError(GetType()?.ToString() + ":: No view component assigned!");
			_view = NullRadialMenuViewAdapterBase.Instance as TView;
		}
	}

	public void Initialize(IList<IList<TModel>> models)
	{
		_models = models;
	}

	private void Update()
	{
		if (Input.GetButtonDown(30))
		{
			Element_Click(_currentElement);
		}
		else if (_secondaryPressTime)
		{
			float buttonShortPressTime = Input.controllers.maps.GetInputBehavior(0).buttonShortPressTime;
			_secondaryPressTimer += Time.deltaTime;
			if (_secondaryPressTimer > buttonShortPressTime || !Input.GetButton(31))
			{
				_secondaryPressTime = false;
				if (_secondaryPressTimer < buttonShortPressTime)
				{
					Element_SecondaryClick(_currentElement);
				}
				else
				{
					Element_SecondaryShortPress(_currentElement);
				}
			}
		}
		else if (Input.GetButtonDown(31))
		{
			_secondaryPressTimer = 0f;
			_secondaryPressTime = true;
		}
	}

	public virtual void Show()
	{
		LeanTween.cancel(gameObject, callOnComplete: true);
		_selectingPosition = Vector3.zero;
		Refresh();
		FadeInCanvas_OnBegin();
		LeanTween.value(gameObject, FadeInCanvas_OnUpdate, _view.CanvasGroup.transform.localScale.x, 1f, _easeTime).setOnComplete(FadeInCanvas_OnComplete).setEase(_easeInType)
			.setIgnoreTimeScale(useUnScaledTime: true);
		_opened?.Invoke();
		_view.Show();
	}

	public virtual void Hide()
	{
		LeanTween.cancel(gameObject, callOnComplete: true);
		_currentElement = null;
		_animating = true;
		LeanTween.value(gameObject, FadeInCanvas_OnUpdate, _view.CanvasGroup.transform.localScale.x, 0f, _easeTime).setOnComplete(() =>
		{
			FadeInCanvas_OnComplete();
			_closed?.Invoke();
			_view.Hide();
		}).setEase(_easeOutType)
			.setIgnoreTimeScale(useUnScaledTime: true);
	}

	public void Refresh()
	{
		LeanTween.cancel(gameObject, callOnComplete: true);
		Element_Exit(_currentElement);
		_currentElement = null;
		PoolElements();
		Layout();
	}

	private TElement CreateElement()
	{
		return UnityEngine.Object.Instantiate(_view.ElementPrefab);
	}

	private void CreateElements()
	{
		for (int i = 0; i < _elementLayoutCount; i++)
		{
			TElement element = CreateElement();
			PushElement(element);
		}
	}

	private void PoolElements()
	{
		foreach (TElement activeElement in _activeElements)
		{
			PushElement(activeElement);
		}
		foreach (TElement extraElement in _extraElements)
		{
			extraElement.transform.SetParent(_view.PooledElementContainer.Transform, worldPositionStays: false);
		}
		_activeElements.Clear();
		_extraElements.Clear();
		_reservedIndexes.Clear();
		BeamTeam.DestroyAllChildren(_view.ElementContainer);
	}

	protected TElement PopElement()
	{
		TElement val = ((_elementsStack.Count > 0) ? _elementsStack.Pop() : CreateElement());
		val.transform.SetParent(_view.ElementContainer.Transform, worldPositionStays: false);
		return val;
	}

	private void PushElement(TElement element)
	{
		_elementsStack.Push(element);
		element.transform.SetParent(_view.PooledElementContainer.Transform, worldPositionStays: false);
	}

	public void UpdateSelector()
	{
		float value = Input.GetAxis(24) / (50f / _sensitivity);
		float value2 = Input.GetAxis(25) / (50f / _sensitivity);
		float x = Mathf.Clamp(value, -1f, 1f);
		float y = Mathf.Clamp(value2, -1f, 1f);
		Vector3 selectingPosition = _selectingPosition;
		_selectorMove = selectingPosition + new Vector3(x, y, 0f) * _elementRadius;
		_view.SelectorImage.transform.localPosition = GetConstrainedPosition(transform.localPosition, _selectorMove);
		_selectingPosition = GetConstrainedPosition(transform.localPosition, _selectorMove);
		Vector2 vector = _view.SelectorImage.transform.position - transform.position;
		float num = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
		_view.SelectorImage.transform.rotation = Quaternion.AngleAxis(num - 90f, Vector3.forward);
		CheckSelectorPosition(_selectingPosition);
	}

	private void CheckSelectorPosition(Vector3 position)
	{
		CheckElements(_activeElements, position);
		CheckElements(_extraElements, position);
	}

	private void CheckElements(IList<TElement> elements, Vector3 position)
	{
		float current = Angle(new Vector2(position.x, position.y));
		float angleStep = GetAngleStep();
		foreach (TElement element in elements)
		{
			float target = Angle(element.transform.position - transform.position);
			if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < angleStep / 2f * _angleThreshold / 100f && _active && !_animating)
			{
				if (_currentElement != element)
				{
					if (!_currentElement.IsNullOrDestroyed())
					{
						Element_Exit(_currentElement);
					}
					_currentElement = element;
					Element_Enter(element);
				}
			}
			else if (_currentElement == element)
			{
				Element_Exit(element);
				_currentElement = null;
			}
		}
	}

	private void LayoutElementPlaceholders()
	{
		_random.Reinitialise(1);
		int num = Mathf.Min(_reservedIndexes.Count + (_models?.Count ?? 0), _elementLayoutCount);
		for (int i = 0; i < num; i++)
		{
			Transform transform = UnityEngine.Object.Instantiate(_view.ElementPlaceholderPrefab.Transform);
			transform.transform.SetParent(_view.ElementContainer.Transform, worldPositionStays: false);
			transform.transform.SetAsFirstSibling();
			float angleForIndex = GetAngleForIndex(i);
			float x = Mathf.Sin(angleForIndex);
			float y = Mathf.Cos(angleForIndex);
			transform.transform.localPosition = new Vector3(x, y, 0f) * _elementRadius;
			transform.transform.localEulerAngles = new Vector3(0f, 0f, (float)_random.Next() * 360f);
		}
	}

	protected virtual void Layout()
	{
		LayoutElementPlaceholders();
		if (_models == null || _models.Count == 0)
		{
			return;
		}
		float rotationOffsetDeg = GetRotationOffsetDeg();
		int i = 0;
		int num = 0;
		for (; i < _elementLayoutCount; i++)
		{
			if (!_reservedIndexes.Contains(i))
			{
				TElement val = PopElement();
				float angleForIndex = GetAngleForIndex(i);
				float x = Mathf.Sin(angleForIndex);
				float y = Mathf.Cos(angleForIndex);
				val.transform.localPosition = new Vector3(x, y, 0f) * _elementRadius;
				val.transform.localEulerAngles = new Vector3(0f, 0f, 0f - rotationOffsetDeg);
				val.Initialize(_models[num]);
				_activeElements.Add(val);
				num++;
				if (num > _models.Count - 1)
				{
					break;
				}
			}
		}
		_view.ElementContainer.Transform.localEulerAngles = new Vector3(0f, 0f, rotationOffsetDeg);
	}

	public void SelectCurrentElement()
	{
		Element_Select(_currentElement);
	}

	protected virtual void InsertElement(TElement element, int index)
	{
		if (!_reservedIndexes.Contains(index))
		{
			element.transform.SetParent(_view.ElementContainer.Transform, worldPositionStays: false);
			float rotationOffsetDeg = GetRotationOffsetDeg();
			float f = MathF.PI * 2f / (float)_elementLayoutCount * (float)index;
			float x = Mathf.Sin(f);
			float y = Mathf.Cos(f);
			element.transform.localPosition = new Vector3(x, y, 0f) * _elementRadius;
			element.transform.localEulerAngles = new Vector3(0f, 0f, 0f - rotationOffsetDeg);
			_extraElements.Add(element);
			_reservedIndexes.Add(index);
		}
	}

	protected virtual void InsertElementAtAngle(TElement element, float ang, bool respectOffset = false)
	{
		element.transform.SetParent(_view.ElementContainer.Transform, worldPositionStays: false);
		float rotationOffsetDeg = GetRotationOffsetDeg();
		float f = (respectOffset ? ang : (ang + rotationOffsetDeg)) * (MathF.PI / 180f);
		float x = Mathf.Sin(f);
		float y = Mathf.Cos(f);
		element.transform.localPosition = new Vector3(x, y, 0f) * _elementRadius;
		element.transform.localEulerAngles = new Vector3(0f, 0f, 0f - rotationOffsetDeg);
		_extraElements.Add(element);
	}

	private void Element_Select(TElement element)
	{
		if (element != null)
		{
			OnSelectionChanged(element);
		}
	}

	protected virtual void Element_Enter(TElement element)
	{
		LeanTween.cancel(gameObject, _fadeOutTweenId);
		element.Enter();
		TModel selectedValue = element.SelectedValue;
		if (selectedValue == null)
		{
			Clear();
		}
		else
		{
			_view.TitleLabel.Text = selectedValue.GetDisplayName();
			_view.DescriptionLabel.Text = selectedValue.GetDescription();
		}
		FadeDetailsCanvasGroup_OnUpdate(1f);
	}

	private void Element_Exit(TElement element)
	{
		if (!(element == null))
		{
			element.Exit();
			if (element.SelectedValue == null)
			{
				FadeDetailsCanvasGroup_OnUpdate(1f);
			}
			else
			{
				_fadeOutTweenId = LeanTween.value(gameObject, FadeDetailsCanvasGroup_OnUpdate, _view.DetailsCanvasGroup.Alpha, 0f, 0.2f).setOnComplete(FadeDetailsCanvasGroup_OnComplete).setEase(LeanTweenType.easeOutCubic)
					.setIgnoreTimeScale(useUnScaledTime: true)
					.id;
			}
		}
	}

	private void Element_Click(TElement element)
	{
		if (element != null)
		{
			OnElementClick(element);
		}
	}

	private void Element_SecondaryClick(TElement element)
	{
		if (element != null)
		{
			OnElementSecondaryClick(element);
		}
	}

	private void Element_SecondaryShortPress(TElement element)
	{
		if (element != null)
		{
			OnElementSecondaryShortPress(element);
		}
	}

	public void NavigateLeft()
	{
		if (_currentElement != null)
		{
			_currentElement.NavigateLeft();
			Element_Enter(_currentElement);
		}
	}

	public void NavigateRight()
	{
		if (_currentElement != null)
		{
			_currentElement.NavigateRight();
			Element_Enter(_currentElement);
		}
	}

	private void FadeInCanvas_OnBegin()
	{
		_animating = true;
		_view.CanvasGroup.Alpha = 0f;
		_view.CanvasGroup.transform.localScale = 0.6f * Vector3.one;
		_view.DetailsCanvasGroup.Alpha = 1f;
		Clear();
	}

	private void FadeInCanvas_OnUpdate(float value)
	{
		_view.CanvasGroup.Alpha = value;
		_view.CanvasGroup.transform.localScale = (0.6f + 0.4f * value) * Vector3.one;
	}

	private void FadeInCanvas_OnComplete()
	{
		_animating = false;
		_view.CanvasGroup.Alpha = 1f;
		_view.CanvasGroup.transform.localScale = Vector3.one;
	}

	private void FadeDetailsCanvasGroup_OnUpdate(float value)
	{
		_view.DetailsCanvasGroup.Alpha = value;
	}

	private void FadeDetailsCanvasGroup_OnComplete()
	{
		Clear();
		LeanTween.value(gameObject, FadeDetailsCanvasGroup_OnUpdate, _view.DetailsCanvasGroup.Alpha, 1f, 0.2f).setEase(LeanTweenType.easeOutCubic).setIgnoreTimeScale(useUnScaledTime: true);
	}

	protected virtual void Clear()
	{
		_view.TitleLabel.Text = Title;
		_view.DescriptionLabel.Text = "";
	}

	private float GetAngleStep()
	{
		if (_elementsDistributionType == EElementDistributionType.Even)
		{
			return 360f / (float)_elementLayoutCount;
		}
		return _angleSteps;
	}

	private float GetAngleForIndex(int index)
	{
		if (_elementsDistributionType == EElementDistributionType.Even)
		{
			return MathF.PI * 2f / (float)_elementLayoutCount * (float)index;
		}
		return GetAngleStep() * (MathF.PI / 180f) * (float)index;
	}

	private float GetRotationOffsetDeg()
	{
		return _elementsDistributionType switch
		{
			EElementDistributionType.Even => 0f, 
			EElementDistributionType.FanAroundNorth => (float)(_elementLayoutCount - 1) * GetAngleStep() / 2f, 
			_ => _rotationAngle, 
		};
	}

	private Vector3 GetConstrainedPosition(Vector3 midPoint, Vector3 endPoint)
	{
		float magnitude = (midPoint - endPoint).magnitude;
		_active = magnitude > _selectorRadius * _activationPercentage;
		if (magnitude > _selectorRadius)
		{
			return (endPoint - midPoint).normalized * _selectorRadius + midPoint;
		}
		return endPoint;
	}

	private float Angle(Vector2 angle)
	{
		if (angle.x < 0f)
		{
			return 360f - Mathf.Atan2(angle.x, angle.y) * 57.29578f * -1f;
		}
		return Mathf.Atan2(angle.x, angle.y) * 57.29578f;
	}
}
