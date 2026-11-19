using Beam;
using LE_LevelEditor.Core;
using UnityEngine;

namespace LE_LevelEditor;

public class LE_DensityBounds : Singleton<LE_DensityBounds>
{
	private class DensityBound
	{
		public Bounds Bound;

		public int Density;

		public Material densityMat;

		public DensityBound(Bounds bound, int density)
		{
			Bound = bound;
			Density = density;
		}
	}

	[SerializeField]
	private Transform densityGridParent;

	[SerializeField]
	private GameObject densityProjector;

	private DensityBound[,] bounds;

	private int numberOfBounds = 20;

	private int boundSize = 25;

	private int zoneSize = 500;

	private void Start()
	{
		boundSize = zoneSize / numberOfBounds;
		bounds = new DensityBound[numberOfBounds, numberOfBounds];
		for (int i = 0; i < numberOfBounds; i++)
		{
			for (int j = 0; j < numberOfBounds; j++)
			{
				bounds[i, j] = new DensityBound(default, 0);
				bounds[i, j].Bound.size = new Vector3(boundSize, 200f, boundSize);
				bounds[i, j].Bound.center = new Vector3(250f, 0f, 250f) - new Vector3(i * boundSize, 0f, j * boundSize);
				GameObject gameObject = Object.Instantiate(densityProjector, new Vector3(250f, 150f, 250f) - new Vector3(i * boundSize, 0f, j * boundSize), Quaternion.identity);
				gameObject.transform.parent = densityGridParent;
				gameObject.transform.rotation = Quaternion.LookRotation(Vector3.down);
				bounds[i, j].densityMat = new Material(gameObject.GetComponent<Projector>().material);
				gameObject.GetComponent<Projector>().material = bounds[i, j].densityMat;
			}
		}
		InvokeRepeating("UpdateVisual", 1f, 1f);
	}

	private void UpdateVisual()
	{
		CheckDensity();
		for (int i = 0; i < numberOfBounds; i++)
		{
			for (int j = 0; j < numberOfBounds; j++)
			{
				float t = (float)bounds[i, j].Density / 100f;
				Color color = Color.Lerp(Color.green, Color.red, t);
				bounds[i, j].densityMat.color = color;
			}
		}
	}

	public bool CheckObjectDensity(LE_Object leObj, Vector3 position)
	{
		for (int i = 0; i < numberOfBounds; i++)
		{
			for (int j = 0; j < numberOfBounds; j++)
			{
				if (bounds[i, j].Bound.Contains(position) && bounds[i, j].Density + leObj.DensityWeight > 100)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void CheckDensity()
	{
		LE_Object[] array = Object.FindObjectsOfType<LE_Object>();
		for (int i = 0; i < numberOfBounds; i++)
		{
			for (int j = 0; j < numberOfBounds; j++)
			{
				bounds[i, j].Density = 0;
			}
		}
		for (int k = 0; k < array.Length; k++)
		{
			for (int l = 0; l < numberOfBounds; l++)
			{
				for (int m = 0; m < numberOfBounds; m++)
				{
					if (bounds[l, m].Bound.Contains(array[k].transform.position))
					{
						bounds[l, m].Density += array[k].DensityWeight;
					}
				}
			}
		}
	}
}
