using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Extension methods for transforming and rasterizing a drawing
public static class GestureDrawing
{
	public static (Vector2, Vector2) AABB(this List<Vector2> drawing) => drawing.Aggregate(
		(Vector2.positiveInfinity, Vector2.negativeInfinity),
		(aabb, p) => (
			new Vector2(Mathf.Min(aabb.Item1.x, p.x), Mathf.Min(aabb.Item1.y, p.y)),
			new Vector2(Mathf.Max(aabb.Item2.x, p.x), Mathf.Max(aabb.Item2.y, p.y))
		)
	);

	public static List<Vector2> Normalized(this List<Vector2> drawing, float size)
	{
		var (min, max) = drawing.AABB();
		var dim = max - min;
		var mySize = Mathf.Max(dim.x, dim.y, 0.0001f);
		min = (min + max) / 2f - new Vector2(mySize, mySize) / 2f;
		return drawing.Select(p => (p - min) * size / mySize).AsParallel().ToList();
	}

	public static List<Vector2> Translated(this List<Vector2> drawing, float x, float y)
	{
		return drawing.Select(p => p + new Vector2(x, y)).AsParallel().ToList();
	}

	public static float[,] Rasterized(this List<Vector2> drawing)
	{
		float width = 2.5f;
		float sample_resolution = 0.6f; // how much step per sample

		// Defines alpha based on the distance from the line
		Func<float, float> alpha_function = dist =>
		{
			dist /= width / 2f; // Normalized distance

			// flat until 0.25 width, then linear to zero
			float slope = 1f / (1f - 0.25f);
			return Mathf.Clamp(-slope * dist + slope, 0f, 1f);
		};

		return ParallelEnumerable.Range(1, Mathf.Max(drawing.Count - 1, 0))
		.Aggregate(new float[28, 28], (raster, i) =>
		{
			float[,] line_raster = new float[28, 28];
			bool[,] visited = new bool[28, 28];

			// Draw a line from p1 to p2
			var (p1, p2) = (drawing[i - 1], drawing[i]);

			var dir = p2 - p1;
			var length = dir.magnitude;
			dir.Normalize();
			var perp = new Vector2(-dir.y, dir.x);

			Func<float, float, (Vector2, int, int)?> get_pixel = (float l, float w) =>
			{
				var p = p1 + dir * l + perp * w;
				var (x, y) = ((int)p.x, (int)p.y);
				var pixel = new Vector2((float)x, (float)y);
				if (x < 0 || y < 0 || x >= 28 || y >= 28 || visited[x, y])
				{
					return null;
				}
				else
				{
					visited[x, y] = true;
					return (pixel, x, y);
				}
			};

			// Draw the line
			for (int wi = 0; wi <= width / sample_resolution; wi++)
			{
				float w = -width / 2f + wi * sample_resolution;

				// Draw line
				for (int li = 0; li <= length / sample_resolution; li++)
				{
					float l = li * sample_resolution;
					var pixel = get_pixel(l, w);
					if (pixel != null)
					{
						var (p, x, y) = pixel ?? (new Vector2(0, 0), 0, 0);
						var dist = (p - p1).Project(perp).magnitude;
						line_raster[x, y] = alpha_function(dist);
					}
				}

				// Draw caps
				for (int di = 0; di <= width / 2f / sample_resolution; di++)
				{
					float d = di * sample_resolution;
					
					// Cap 1
					var pixel = get_pixel(-d, w);
					if (pixel != null)
					{
						var (p, x, y) = pixel ?? (new Vector2(0, 0), 0, 0);
						float dist = (p - p1).magnitude;
						line_raster[x, y] = alpha_function(dist);
					}

					// Cap 2
					pixel = get_pixel(length + d, w);
					if (pixel != null)
					{
						var (p, x, y) = pixel ?? (new Vector2(0, 0), 0, 0);
						float dist = (p - p2).magnitude;
						line_raster[x, y] = alpha_function(dist);
					}
				}
			}

			// Draw the line raster onto the main raster
			// Use max function so all lines look like they come from one
			// Update line_raster instead of raster in case race conditions could happen
			for (int x = 0; x < 28; x++)
			{
				for (int y = 0; y < 28; y++)
				{
					line_raster[x, y] = Mathf.Max(raster[x, y], line_raster[x, y]);
				}
			}
            return line_raster;
		}).Transposed();
	}

	// Prints a raster (only 1s and 0s) for debugging
	public static void PrintRaster(float[,] raster)
	{
        String str = "";
        for (int x = 0; x < 28; x++)
        {
            for (int y = 0; y < 28; y++)
            {
                str += raster[x, y] < 0.1f ? "." : "*";
            }
            str += "\n";
        }
        Debug.Log(str);
	}


}
