using System;
using System.Collections;
using System.Collections.Generic;
using Beam.Events;
using Beam.Utilities;
using Funlabs;
using UnityEngine;

namespace Beam;

public class FlockController : MonoBehaviour
{
	private class ReplicatedRepositionOriginMsg : MultiplayerMessage
	{
		[Replicate]
		public Vector3 Position;

		public override void OnPeer()
		{
			UnityEngine.Object.FindObjectOfType<FlockController>().RepositionOrigin(Position);
		}
	}

	private static FlockController _Instance;

	private LandingSpotController _landingSpotController;

	[SerializeField]
	private Transform _activeContainer;

	[SerializeField]
	private Transform _deactivatedContainer;

	[SerializeField]
	private LayerMask _avoidanceMask;

	private Vector3 _origin;

	[Header("Flock Bounds")]
	[SerializeField]
	private Vector3 _center;

	[SerializeField]
	private Vector2 _size;

	[SerializeField]
	private Vector2 _spawnBuffer;

	[Header("Waypoints")]
	[SerializeField]
	private float _waypointDistance = 10f;

	[Tooltip("Bias for new waypoint direction between flock's forward direction and random direction.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _waypointForwardBias = 0.3f;

	[Tooltip("Bias for new waypoint height between flock's height and random height.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float _waypointHeightBias = 0.5f;

	[Header("Flocks")]
	[SerializeField]
	private FlockProvider _flockProvider;

	public FlockAudio _flockAudioPrefab;

	private IList<FlockData> _flockData;

	private Dictionary<FlockSpecies, FlockData> _availableFlocks = new Dictionary<FlockSpecies, FlockData>();

	private string _currentZoneName;

	private string _nmlZoneName = "";

	private string _endGameZoneName = "";

	public static FlockController Instance => _Instance;

	public Transform ActiveContainer => _activeContainer;

	public Transform DeactivatedContainer => _deactivatedContainer;

	public LayerMask AvoidanceMask => _avoidanceMask;

	public IDictionary<FlockSpecies, FlockData> AvailableFlocks => _availableFlocks;

	public Vector3 ControllerPosition => _origin + _center;

	public float MaxLandingSpotDistance => 30f;

	private void Awake()
	{
		if (_Instance != null)
		{
			BeamTeam.Destroy(gameObject);
		}
		else
		{
			_Instance = this;
		}
	}

	private IEnumerator Start()
	{
		_landingSpotController = Singleton<LandingSpotController>.Instance;
		if (!Game.Mode.IsMaster())
		{
			yield break;
		}
		while (LevelLoader.IsLoadingScene())
		{
			yield return null;
		}
		_flockData = _flockProvider.GetFlocks();
		CreateFlocks();
		EventManager.AddListener<EndGameSequenceEvent>(EventManager_EndGameSequenceEvent);
		if ((bool)Singleton<StrandedWorld>.Instance)
		{
			_nmlZoneName = Singleton<StrandedWorld>.Instance.NmlZone.name;
			Zone zone = Singleton<StrandedWorld>.Instance.FindEndGameZone();
			if ((bool)zone)
			{
				_endGameZoneName = zone.name;
			}
		}
		InvokeRepeating("PollZone", 1f, 1f);
		InvokeRepeating("PollFlockEvents", 1f, 1f);
	}

	private void OnDestroy()
	{
		if (!(_Instance != this))
		{
			_Instance = null;
			if (Game.Mode.IsMaster())
			{
				EventManager.RemoveListener<EndGameSequenceEvent>(EventManager_EndGameSequenceEvent);
			}
		}
	}

	private void EventManager_EndGameSequenceEvent(EndGameSequenceEvent e)
	{
		StopAllFlockAudio();
	}

	private void PollZone()
	{
		IPlayer player = PlayerRegistry.AllPlayers.FirstOrDefault_NonAlloc();
		if (player.IsNullOrDestroyed())
		{
			return;
		}
		Zone zone = StrandedWorld.GetZone(player.transform.position);
		if ((bool)zone && !(zone.name == _currentZoneName))
		{
			_currentZoneName = zone.name;
			DeactivateFlocks();
			if (!(_currentZoneName == _nmlZoneName) && !(_currentZoneName == _endGameZoneName))
			{
				ReplicatedRepositionOrigin(zone.transform.position);
				RefreshAvailableFlocks();
			}
		}
	}

	private void RepositionOrigin(Vector3 position)
	{
		Vector3 origin = position;
		origin.y = 0f;
		_origin = origin;
	}

	private void CreateFlocks()
	{
		foreach (FlockData flockDatum in _flockData)
		{
			_availableFlocks.Add(flockDatum.Species, flockDatum);
			FlockAudio audio = CreateFlockAudio(flockDatum);
			_availableFlocks[flockDatum.Species].Audio = audio;
			for (int i = 0; i < flockDatum.Population; i++)
			{
				CreateFlockChild(flockDatum);
			}
		}
	}

	public void StopAllFlockAudio()
	{
		foreach (FlockData flockDatum in _flockData)
		{
			_availableFlocks[flockDatum.Species].Audio.Stop();
		}
	}

	private FlockChild CreateFlockChild(FlockData flockData)
	{
		FlockChild component = MultiplayerMng.Instantiate<SaveablePrefab>(flockData.Prefab.GetComponent<SaveablePrefab>().PrefabId, MiniGuid.New()).GetComponent<FlockChild>();
		component.transform.SetParent(_deactivatedContainer, worldPositionStays: false);
		component.Initialize(this, _landingSpotController);
		flockData.Children.Add(component);
		return component;
	}

	private FlockAudio CreateFlockAudio(FlockData flockData)
	{
		FlockAudio flockAudio = UnityEngine.Object.Instantiate(_flockAudioPrefab);
		flockAudio.transform.SetParent(_deactivatedContainer, worldPositionStays: false);
		flockAudio.AmbientClip = flockData.FlockAudioClip;
		return flockAudio;
	}

	private void InitializeFlockChild(FlockChild flockChild, Vector3 startingPosition)
	{
		flockChild.transform.position = startingPosition + UnityEngine.Random.onUnitSphere * flockChild.LocalWaypointSpreadRadius;
		flockChild.Reset();
		flockChild.SetLocalWaypoint();
		flockChild.ReplicatedChangeState(FlockChildState.Flying, Vector3.zero);
	}

	private void PollFlockEvents()
	{
		if (_currentZoneName == _nmlZoneName || _currentZoneName == _endGameZoneName)
		{
			return;
		}
		foreach (KeyValuePair<FlockSpecies, FlockData> availableFlock in _availableFlocks)
		{
			FlockData value = availableFlock.Value;
			if ((!value.Active || value.Migrating) && CheckTime(value.StartTime, value.EndTime))
			{
				if (UnityEngine.Random.value * 100f <= (float)value.Percentage)
				{
					StartFlockEvent(value);
				}
			}
			else if (value.Active && !CheckTime(value.StartTime, value.EndTime))
			{
				StopFlockEvent(value);
			}
			if (value.Migrating && value.Children.All_NonAlloc((FlockChild x) => (FlockChildState)x.CurrentState == FlockChildState.Migrated))
			{
				DeactivateFlock(value);
			}
		}
	}

	private void StartFlockEvent(FlockData flockData)
	{
		Vector3 startingPosition = GetStartingPosition();
		flockData.Migrating = false;
		flockData.Waypoint = GetRandomPositionInBounds();
		SetNewWaypointForSpecies(flockData.Species, UnityEngine.Random.onUnitSphere);
		flockData.Audio.transform.position = startingPosition;
		ActivateFlock(flockData);
		foreach (FlockChild child in flockData.Children)
		{
			InitializeFlockChild(child, startingPosition);
		}
	}

	private void StopFlockEvent(FlockData flockData)
	{
		if (flockData.Migrating)
		{
			return;
		}
		Vector3 startingPosition = GetStartingPosition();
		flockData.Migrating = true;
		flockData.Waypoint = startingPosition;
		flockData.Audio.TargetPosition = startingPosition;
		foreach (FlockChild child in flockData.Children)
		{
			child.SpeciesWaypoint = flockData.Waypoint;
			child.SetLocalWaypoint();
			child.Migrate();
		}
	}

	private void ActivateFlock(FlockData flockData)
	{
		foreach (FlockChild child in flockData.Children)
		{
			child.transform.SetParent(_activeContainer);
		}
		flockData.Audio.transform.SetParent(_activeContainer);
		flockData.Active = true;
	}

	private void DeactivateFlock(FlockData flockData)
	{
		if (!flockData.Active)
		{
			return;
		}
		foreach (FlockChild child in flockData.Children)
		{
			DeactivateFlockChild(child);
		}
		flockData.Audio.transform.SetParent(_deactivatedContainer);
		flockData.Active = false;
	}

	private void DeactivateFlocks()
	{
		foreach (KeyValuePair<FlockSpecies, FlockData> availableFlock in _availableFlocks)
		{
			FlockData value = availableFlock.Value;
			DeactivateFlock(value);
		}
	}

	public void DeactivateFlockChild(FlockChild flockChild)
	{
		flockChild.transform.SetParent(_deactivatedContainer);
	}

	private void RefreshAvailableFlocks()
	{
		_flockData = _flockProvider.GetFlocks();
	}

	public void SetNewWaypointForSpecies(FlockSpecies species, Vector3 forward)
	{
		Vector3 newWaypoint = GetNewWaypoint(_availableFlocks[species].Waypoint, forward);
		foreach (FlockChild child in _availableFlocks[species].Children)
		{
			child.SpeciesWaypoint = newWaypoint;
		}
		_availableFlocks[species].Waypoint = newWaypoint;
		_availableFlocks[species].Audio.TargetPosition = newWaypoint;
	}

	private Vector3 GetNewWaypoint(Vector3 previousWaypoint, Vector3 forward)
	{
		Vector3 vector = Vector3.Lerp(forward, UnityEngine.Random.onUnitSphere, _waypointForwardBias) * _waypointDistance;
		if (Vector3Tools.DistanceOnAxis(ControllerPosition, previousWaypoint + vector, Vector3Tools.xz) > _size.x)
		{
			vector.x *= -1f;
			vector.z *= -1f;
		}
		Vector3 result = previousWaypoint + vector;
		result.y = Mathf.Lerp(previousWaypoint.y, result.y, _waypointHeightBias);
		result.y = Mathf.Clamp(result.y, ControllerPosition.y, ControllerPosition.y + _size.y);
		return result;
	}

	private Vector3 GetStartingPosition()
	{
		Vector3 vector = ControllerPosition - PlayerRegistry.AllPlayers[0].transform.position;
		vector.y = 0f;
		vector.Normalize();
		return ControllerPosition + vector * (_size.x + _spawnBuffer.x);
	}

	public void RemoveChildFromFlock(FlockChild flockChild)
	{
		if (_availableFlocks.TryGetValue(flockChild.Species, out var value))
		{
			value.Children.Remove(flockChild);
			FlockChild flockChild2 = CreateFlockChild(value);
			if (value.Active)
			{
				flockChild2.transform.SetParent(_activeContainer);
			}
			flockChild2.SpeciesWaypoint = value.Waypoint;
			InitializeFlockChild(flockChild2, GetStartingPosition());
			if (value.Migrating)
			{
				flockChild2.Migrate();
			}
		}
	}

	private Vector3 GetRandomPositionInBounds()
	{
		Vector3 result = ControllerPosition + UnityEngine.Random.insideUnitSphere * _size.x;
		result.y = ControllerPosition.y + UnityEngine.Random.Range(0f, _size.y);
		return result;
	}

	private Vector3 GetRandomPositionOnBounds()
	{
		Vector3 result = ControllerPosition + UnityEngine.Random.onUnitSphere * _size.x;
		result.y = ControllerPosition.y + UnityEngine.Random.Range(0f, _size.y);
		return result;
	}

	private void ReplicatedRepositionOrigin(Vector3 position)
	{
		if (Game.Mode.IsServer())
		{
			ReplicatedRepositionOriginMsg replicatedRepositionOriginMsg = new ReplicatedRepositionOriginMsg();
			replicatedRepositionOriginMsg.Position = position;
			replicatedRepositionOriginMsg.Post();
		}
		RepositionOrigin(position);
	}

	private bool CheckTime(float startTime, float endTime)
	{
		return IsTimeBetween(GameTime.Now.TimeOfDay, TimeSpan.FromHours(startTime), TimeSpan.FromHours(endTime));
	}

	private bool IsTimeBetween(TimeSpan time, TimeSpan startTime, TimeSpan endTime)
	{
		if (endTime == startTime)
		{
			return true;
		}
		if (endTime < startTime)
		{
			if (!(time <= endTime))
			{
				return time >= startTime;
			}
			return true;
		}
		if (time >= startTime)
		{
			return time <= endTime;
		}
		return false;
	}
}
