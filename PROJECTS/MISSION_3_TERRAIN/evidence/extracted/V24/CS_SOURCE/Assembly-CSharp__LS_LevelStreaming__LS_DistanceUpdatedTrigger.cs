using UnityEngine;

namespace LS_LevelStreaming;

public class LS_DistanceUpdatedTrigger : LS_ITriggerByUpdatedPosition, LS_ITrigger
{
	private int m_updateFrequency;

	private Transform m_playerOrCameraTransform;

	private Vector3 m_triggeredObjectPosition;

	private float m_spawnDistance;

	private float m_despawnDistance;

	private bool m_isAlwaysVisibleWithOrthoCam;

	private bool m_isVisible;

	private int m_updateCounter;

	public LS_DistanceUpdatedTrigger(int p_updateFrequency, Transform p_playerOrCameraTransform, float p_spawnDistance, float p_despawnDistance, bool p_isAlwaysVisibleWithOrthoCam)
		: this(p_updateFrequency, p_playerOrCameraTransform, p_spawnDistance, p_despawnDistance)
	{
		m_isAlwaysVisibleWithOrthoCam = p_isAlwaysVisibleWithOrthoCam;
	}

	public LS_DistanceUpdatedTrigger(int p_updateFrequency, Transform p_playerOrCameraTransform, float p_spawnDistance, float p_despawnDistance)
	{
		m_updateFrequency = p_updateFrequency;
		if (m_updateFrequency > 0)
		{
			m_updateCounter = Random.Range(0, m_updateFrequency);
		}
		m_playerOrCameraTransform = p_playerOrCameraTransform;
		m_spawnDistance = p_spawnDistance;
		m_despawnDistance = p_despawnDistance;
		m_isVisible = false;
	}

	void LS_ITriggerByUpdatedPosition.Update(Vector3 p_newPosition)
	{
		m_triggeredObjectPosition = p_newPosition;
	}

	bool LS_ITrigger.IsVisible()
	{
		if (m_updateCounter <= 0)
		{
			m_updateCounter = m_updateFrequency;
			if (m_playerOrCameraTransform != null)
			{
				bool flag = false;
				if (m_isAlwaysVisibleWithOrthoCam)
				{
					Camera component = m_playerOrCameraTransform.GetComponent<Camera>();
					if (component != null && component.orthographic)
					{
						flag = true;
					}
				}
				if (flag)
				{
					m_isVisible = true;
				}
				else
				{
					float magnitude = (m_playerOrCameraTransform.position - m_triggeredObjectPosition).magnitude;
					if (m_spawnDistance >= magnitude)
					{
						m_isVisible = true;
					}
					else if (m_despawnDistance <= magnitude)
					{
						m_isVisible = false;
					}
				}
			}
		}
		else
		{
			m_updateCounter--;
		}
		return m_isVisible;
	}
}
