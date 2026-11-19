using System.Collections.Generic;
using Beam.Utilities;
using UnityEngine;

namespace Beam.UI;

public class IndicatorPresenter : ControllerBase, INeedPlayerComponent
{
	[SerializeField]
	private IndicatorViewBase _view;

	private List<IndicatorElementBase> _indicators = new List<IndicatorElementBase>();

	private IPlayer _player;

	IPlayer INeedPlayerComponent.Player => _player;

	private void Start()
	{
		if (_view == null)
		{
			Debug.LogError("IndicatorPresenter:: No view component assigned!");
			_view = NullIndicatorViewBase.Instance;
		}
	}

	public override void SetView(ViewAdapterBase view)
	{
		_view = view as IndicatorViewBase;
		if (_view == null)
		{
			Debug.LogError("IndicatorPresenter:: No view component assigned!");
			_view = NullIndicatorViewBase.Instance;
		}
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
		_player.Statistics.DirectionalDamage += PlayerStatistics_DirectionalDamage;
	}

	private void OnDestroy()
	{
		if (_player != null && _player.Statistics != null)
		{
			_player.Statistics.DirectionalDamage -= PlayerStatistics_DirectionalDamage;
		}
	}

	public void NewIndicator(IndicatorDataBase data)
	{
		if (data != null)
		{
			IndicatorElementBase indicatorElementBase = Object.Instantiate(_view.IndicatorPrefab);
			indicatorElementBase.Initialize(data);
			AddIndicator(indicatorElementBase);
		}
	}

	private void Update()
	{
		if (_player == null)
		{
			return;
		}
		for (int i = 0; i < _indicators.Count; i++)
		{
			IndicatorElementBase indicatorElementBase = _indicators[i];
			if (indicatorElementBase.IsNullOrDestroyed())
			{
				_indicators.Remove(indicatorElementBase);
				break;
			}
			Vector3 forward = _player.transform.forward;
			Vector3 vector = indicatorElementBase.Data.Direction - _player.transform.position;
			vector.y = 0f;
			vector.Normalize();
			float dot = 0f - Vector3.Dot(Vector3.Cross(forward, vector), _player.transform.up);
			float angle = Vector3.Angle(vector, forward);
			angle = AngleCircumference(dot, angle);
			indicatorElementBase.transform.localRotation = Quaternion.Slerp(indicatorElementBase.transform.localRotation, Quaternion.AngleAxis(angle, new Vector3(0f, 0f, 1f)), 10f * Time.deltaTime);
		}
	}

	public void AddIndicator(IndicatorElementBase indicator)
	{
		if (!_indicators.Contains(indicator))
		{
			indicator.transform.SetParent(_view.IndicatorContainer.Transform, worldPositionStays: false);
			_indicators.Add(indicator);
		}
	}

	public void RemoveIndicator(IndicatorElementBase indicator)
	{
		_indicators.Remove(indicator);
	}

	public void DestroyIndicator(IndicatorElementBase indicator)
	{
		RemoveIndicator(indicator);
		BeamTeam.Destroy(indicator.gameObject);
	}

	private void PlayerStatistics_DirectionalDamage(IndicatorDataBase data)
	{
		NewIndicator(data);
	}

	public float AngleCircumference(float dot, float angle)
	{
		float num = angle;
		float num2 = 360f;
		num = angle;
		if (dot < 0f)
		{
			num = num2 - angle;
		}
		return num;
	}
}
