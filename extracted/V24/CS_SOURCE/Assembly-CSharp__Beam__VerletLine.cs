using System.Collections.Generic;
using UnityEngine;

namespace Beam;

public class VerletLine : MonoBehaviour
{
	public Transform CableStart;

	public Transform CableEnd;

	public float CableLength = 10f;

	public int CableSegments = 10;

	public int SolverIterations = 1;

	[SerializeField]
	private LineRenderer _lineRenderer;

	private readonly List<VParticle> _particles = new List<VParticle>();

	private float _timeRemainder;

	private float SubstepTime = 0.02f;

	private void Start()
	{
		Vector3 vector = CableEnd.position - CableStart.position;
		_particles.Clear();
		int num = CableSegments + 1;
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)i / (float)num;
			VParticle vParticle = new VParticle(CableStart.transform.position + num2 * vector);
			vParticle.Free = i != 0 && i != num - 1;
			_particles.Add(vParticle);
		}
		_lineRenderer.positionCount = _particles.Count;
	}

	private void Update()
	{
		VParticle vParticle = _particles[0];
		vParticle.Position = (vParticle.OldPosition = CableStart.position);
		VParticle vParticle2 = _particles[CableSegments];
		vParticle2.Position = (vParticle2.OldPosition = CableEnd.position);
		Vector3 gravity = Physics.gravity;
		float num = Mathf.Max(SubstepTime, 0.005f);
		_timeRemainder += Time.deltaTime;
		while (_timeRemainder > num)
		{
			PreformSubstep(num, gravity);
			_timeRemainder -= num;
		}
	}

	private void LateUpdate()
	{
		for (int i = 0; i < _particles.Count; i++)
		{
			VParticle vParticle = _particles[i];
			_lineRenderer.SetPosition(i, vParticle.Position);
		}
	}

	private void PreformSubstep(float substepTime, Vector3 gravity)
	{
		VerletIntegrate(substepTime, gravity);
		SolveConstraints();
	}

	private void VerletIntegrate(float substepTime, Vector3 gravity)
	{
		int num = CableSegments + 1;
		float num2 = substepTime * substepTime;
		for (int i = 0; i < num; i++)
		{
			VParticle vParticle = _particles[i];
			if (vParticle.Free)
			{
				Vector3 vector = vParticle.Position - vParticle.OldPosition;
				Vector3 position = vParticle.Position + vector + num2 * gravity;
				vParticle.OldPosition = vParticle.Position;
				vParticle.Position = position;
			}
		}
	}

	private void SolveConstraints()
	{
		float distance = CableLength / (float)CableSegments;
		for (int i = 0; i < SolverIterations; i++)
		{
			for (int j = 0; j < CableSegments; j++)
			{
				VParticle particleA = _particles[j];
				VParticle particleB = _particles[j + 1];
				SolveDistanceConstraint(particleA, particleB, distance);
			}
		}
	}

	private void SolveDistanceConstraint(VParticle particleA, VParticle particleB, float distance)
	{
		Vector3 vector = particleB.Position - particleA.Position;
		float magnitude = vector.magnitude;
		float num = (magnitude - distance) / magnitude;
		if (particleA.Free && particleB.Free)
		{
			particleA.Position += num * 0.5f * vector;
			particleB.Position -= num * 0.5f * vector;
		}
		else if (particleA.Free)
		{
			particleA.Position += num * vector;
		}
		else if (particleB.Free)
		{
			particleB.Position -= num * vector;
		}
	}
}
