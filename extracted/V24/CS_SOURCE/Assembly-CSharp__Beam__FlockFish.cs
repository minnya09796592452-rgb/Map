using System.Collections.Generic;
using Beam.Serialization.Json;
using Beam.Utilities;
using UnityEngine;

namespace Beam;

public class FlockFish : FishBase, IFixedTick, IPlayerProximityHandler
{
	[SerializeField]
	private FlockSettings _flockSettings;

	[SerializeField]
	private List<IAttracterRepeller> _attracters = new List<IAttracterRepeller>();

	[SerializeField]
	private List<IAttracterRepeller> _repellers = new List<IAttracterRepeller>();

	private List<Matrix4x4> _positions = new List<Matrix4x4>(100);

	private BoidFlock _flock;

	private int _ticks;

	FixedTickRate IFixedTick.Rate => FixedTickRate.Full;

	bool IFixedTick.Active => gameObject.activeInHierarchy;

	Vector3 IPlayerProximityHandler.Position => transform.position;

	float IPlayerProximityHandler.ProximityDistance => _flockSettings.BoundsRadius * 2f;

	bool IPlayerProximityHandler.InProximity { get; set; }

	public IList<IAttracterRepeller> Attracters => _attracters;

	public IList<IAttracterRepeller> Repellers => _repellers;

	public override Bounds GetBounds()
	{
		return new Bounds(Vector3.zero, ((_flockSettings.BoundsType == BoundsType.Sphere) ? Vector3.one : new Vector3(1f, 0f, 1f)) * (_flockSettings.BoundsRadius * 2f));
	}

	protected override void Start()
	{
	}

	private void OnEnable()
	{
		FixedTicker.Instance?.Add(this);
	}

	private void OnDisable()
	{
		FixedTicker.Instance?.Remove(this);
	}

	protected override void OnSubscribed(ICell cell)
	{
		if (_flock == null)
		{
			CreateBoids(_flockSettings.FlockCount);
		}
		else
		{
			ReinitializeBoids();
		}
		base.OnSubscribed(cell);
	}

	void IFixedTick.FixedTick()
	{
		if (!LevelLoader.IsLoading(gameObject) && FishRenderer.IsVisible())
		{
			UpdateBoids();
			FishRenderer.SetMatrices(_positions);
		}
	}

	private void CreateBoids(int populationCount)
	{
		_flock = new BoidFlock(_flockSettings.BoundsRadius, _flockSettings.SimulationCells);
		_positions = new List<Matrix4x4>(populationCount);
		for (int i = 0; i < populationCount; i++)
		{
			Vector3 position = transform.position + Random.insideUnitSphere * _flockSettings.BoundsRadius;
			Vector3 insideUnitSphere = Random.insideUnitSphere;
			Boid boid = new Boid(position, insideUnitSphere, _flockSettings);
			_flock.Boids.Add(boid);
			_positions.Add(Matrix4x4.TRS(boid.Position, Quaternion.identity, Vector3.one));
		}
		_flock.Rebuild();
		FishRenderer.SetMatrices(_positions);
	}

	private void ReinitializeBoids()
	{
		for (int i = 0; i < _flock.Boids.Count; i++)
		{
			Boid boid = _flock.Boids[i];
			Vector3 position = transform.position + Random.insideUnitSphere * _flockSettings.BoundsRadius;
			Vector3 insideUnitSphere = Random.insideUnitSphere;
			boid.Reinitialize(position, insideUnitSphere);
		}
		_flock.Rebuild();
		FishRenderer.SetMatrices(_positions);
	}

	private void UpdateBoids()
	{
		_ticks++;
		if (_ticks >= _flockSettings.SimulationTimestep)
		{
			for (int i = 0; i < _flock.Boids.Count; i++)
			{
				Boid boid = _flock.Boids[i];
				IList<Boid> boids = _flock.FindNeighborsNear(boid.Position.x, boid.Position.y, _flockSettings.CohesionDistance);
				boid.DoBounds(transform.position);
				boid.DoFlock(boids);
				boid.DoRepel(_repellers);
				boid.DoUpdate(Time.deltaTime, _ticks);
				_positions[i] = Matrix4x4.TRS(boid.Position, Quaternion.LookRotation(Vector3.Lerp(boid.Vel, boid.Velocity, Time.deltaTime).normalized, Vector3.up), Vector3.one);
			}
			_ticks = 0;
		}
		else
		{
			for (int j = 0; j < _flock.Boids.Count; j++)
			{
				Boid boid2 = _flock.Boids[j];
				boid2.DoBounds(transform.position);
				boid2.DoRepel(_repellers);
				boid2.DoUpdate(Time.deltaTime);
				_positions[j] = Matrix4x4.TRS(boid2.Position, Quaternion.LookRotation(Vector3.Lerp(boid2.Vel, boid2.Velocity, Time.deltaTime).normalized, Vector3.up), Vector3.one);
			}
		}
		_flock.Rebuild();
	}

	void IPlayerProximityHandler.OnEnteredPlayerProximity(PlayerData player)
	{
	}

	void IPlayerProximityHandler.OnLeftPlayerProximity(PlayerData player)
	{
	}

	public override void Catch()
	{
		base.Catch();
	}

	public override bool CheckPositionInBounds(Vector3 position)
	{
		for (int i = 0; i < _flock.Boids.Count; i++)
		{
			Boid boid = _flock.Boids[i];
			if ((position - transform.TransformPoint(boid.Position)).sqrMagnitude < BeamMath.Sqr(0.1f))
			{
				return true;
			}
		}
		return false;
	}

	public override bool CheckPositionInInterestRadius(Vector3 position)
	{
		return (transform.position - position).sqrMagnitude < BeamMath.Sqr(_flockSettings.BoundsRadius);
	}

	public override JObject Save()
	{
		JObject jObject = base.Save();
		jObject.AddField("Pop", _positions.Count);
		return jObject;
	}

	public override void Load(JObject data)
	{
		base.Load(data);
		CreateBoids(3);
	}
}
