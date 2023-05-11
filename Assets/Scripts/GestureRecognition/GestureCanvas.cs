using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using VolumetricLines;

// This should be spawned when the player starts drawing.
public class GestureCanvas : MonoBehaviour
{
	[HideInInspector] public Transform drawer; // The object drawing the gesture.

	public VolumetricLineBehavior linePrefab;
	public float smoothing = 0.1f;
	public float smoothTime = 0.07f;
	public float lineLength = 0.02f; // Length of each line segment in the drawing 
									 // (increase to improve performance)
	public float maxDrawingLength = 2f; // Length in which the line starts to shorten
	public float flattenTime = 2f; // Time it takes to flatten the drawing
	public float shortenTime = 2f; // Time it takes to shorten to maxDrawingLength
	public float failureTime = 0.5f; // Time it takes for the gesture to dissipate after failing
	public float successTime = 0.5f; // Time it takes for the gesture to turn green after succeeding
	public float successShortenTime = 0.5f; // Time it takes for the drawing to shorten to zero after turning green

	public Color successLineColor;
	public Color failureLineColor;

	List<Vector3> drawing = new List<Vector3>(); // drawing in 3D used for smoothing
	List<Vector3> startPosVelocities = new List<Vector3>(); // velocity of each start point
	List<Vector3> endPosVelocities = new List<Vector3>(); // velocity of each end point
	List<VolumetricLineBehavior> drawingLines = new List<VolumetricLineBehavior>();

	public ReadOnlyCollection<Vector3> Drawing { get => drawing.AsReadOnly(); }

	// Velocity of the point of drawing. Used to smoothen drawing motion
	Vector3 drawingVelocity = Vector3.zero;

	// Current position of the point of drawing
	Vector3 DrawingPosition { get => drawer.position; }

	bool finishedDrawing = false;
	bool success = false;
	float finishAnimationTime = 0f; // Current time since finished drawing

	float Length { get => drawingLines.Aggregate(0f, (length, line) => 
		length + (line.EndPos - line.StartPos).magnitude); }

	void Start()
	{
		drawing.Add(DrawingPosition + Vector3.down * 1.5e-3f); // Add initial point
		AddPoint(DrawingPosition); // Add initial line
	}

	public void FinishDrawing(bool success)
	{
		finishedDrawing = true;
		this.success = success;
		finishAnimationTime = 0f;
	}

	public (Vector3, Vector3) ComputeCentroidAndNormal()
	{
		// var (centroid, _, _, normal) = LinearAlgebraUtils.ComputePrincipalDirections(drawing.ToArray());
		// return (centroid, normal.normalized);

		var (centroid, _, _, _) = LinearAlgebraUtils.ComputePrincipalDirections(drawing.ToArray());
		return (centroid, drawer.forward);
	}

	public List<Vector2> GetDrawing2D()
	{
		// Want to project onto the plane centered at centroid with normal `normal`
		var (centroid, normal) = ComputeCentroidAndNormal();
		Vector3 yAxis = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
		Vector3 xAxis = Vector3.Cross(yAxis, normal).normalized;

		return drawing.Select(point =>
		{
			// Project the point onto the plane then convert to 2D
			Vector3 projected = Vector3.ProjectOnPlane(point - centroid, normal);

			Vector3 yComponent = Vector3.Project(projected, yAxis);
			Vector3 xComponent = projected - yComponent;

			return new Vector2(
				Mathf.Sign(Vector3.Dot(xComponent, xAxis)) * xComponent.magnitude,
				-Mathf.Sign(Vector3.Dot(yComponent, yAxis)) * yComponent.magnitude);
		}).ToList();
	}

	/// Adds a point to the end of the drawing
	void AddPoint(Vector3 point)
	{
		drawing.Add(point);

		var line = Instantiate(linePrefab.gameObject, transform).GetComponent<VolumetricLineBehavior>();
		line.gameObject.SetActive(true);
		line.StartPos = drawing[drawing.Count - 2];
		line.EndPos = point;
		drawingLines.Add(line);
		startPosVelocities.Add(Vector3.zero);
		endPosVelocities.Add(Vector3.zero);
	}

	// Moves the ith point in the drawing to another point 
	// Also updates the line renders with that point
	void MovePoint(int i, Vector3 to)
	{
		if (i < 0 || i >= drawing.Count)
			throw new ArgumentOutOfRangeException(i + " index out of bounds of "
				+ drawing.Count + " points");

		drawing[i] = to;
		if (i < drawing.Count - 1) drawingLines[i].StartPos = to;
		if (i > 0) drawingLines[i - 1].EndPos = to;
	}

	/// Shorten the drawing by moving last point towards previous, then destroying it
	/// time is the time it takes to shorten to zero
	void ShortenDrawing(float speed)
	{
		if (drawing.Count > 1) // Can't shorten drawing with 1 point
		{
			speed += lineLength;
			float time = (drawing[1] - drawing[0]).magnitude / speed;
			Vector3 velocity = startPosVelocities[0];
			MovePoint(0, Vector3.SmoothDamp(drawing[0], drawing[1], ref velocity, time));
			startPosVelocities[0] = velocity;

			if (drawing.Count > (finishedDrawing ? 1 : 2) && (drawing[1] - drawing[0]).magnitude < 1e-3f)
			{
				drawing.RemoveAt(0);
				Destroy(drawingLines[0]);
				drawingLines.RemoveAt(0);
				startPosVelocities.RemoveAt(0);
				endPosVelocities.RemoveAt(0);
				if (drawingLines.Count > 0)
					startPosVelocities[0] = velocity;
			}
		}
	}

	// Slowly movs all points onto a plane
	void Flatten()
	{
		var leadingPoints = drawing.TakeLast(Mathf.Min(drawing.Count / 2 + 3, drawing.Count)).ToArray();
		if (leadingPoints.Length > 2)
		{
			// var (centroid, _, _, normal) = LinearAlgebraUtils.ComputePrincipalDirections(leadingPoints);
		var (centroid, normal) = ComputeCentroidAndNormal();

			// Move centroid so that the last point is on the plane (everything moves towards the last point)
			centroid += Vector3.Project(drawing.Last() - centroid, normal);

			for (int i = 0; i < drawing.Count; i++)
			{
				// Project the point onto the plane
				Vector3 dist = Vector3.Project(drawing[i] - centroid, normal);
				Vector3 target = drawing[i] - dist;
				Vector3 velocity = i > 0 ? endPosVelocities[i-1] : startPosVelocities[i];
				MovePoint(i, Vector3.SmoothDamp(drawing[i], target, ref velocity, flattenTime));
				if (i > 0) endPosVelocities[i-1] = velocity;
				if (i < drawingLines.Count) startPosVelocities[i] = velocity;
			}
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (!finishedDrawing)
		{
			// Update drawing position
			Vector3 drawPoint = drawing.Last().SmoothTransform(DrawingPosition, ref drawingVelocity, 0, smoothing, smoothTime);
			MovePoint(drawing.Count - 1, drawPoint);

			// Start new line if last line is long enough
			// Don't want to make a new line every frame do this
			if ((drawing.Last() - drawing.SkipLast(1).Last()).magnitude > lineLength)
			{
				AddPoint(drawing.Last());
			}

			// Shorten drawing so the player can't draw too many lines
			float shortenSpeed = Mathf.Max(Length-maxDrawingLength, 0f) / shortenTime;
			ShortenDrawing(shortenSpeed);

			Flatten();
		}
		else
		{
			if (success) // Turn drawing green then shorten to zero
			{
				if (finishAnimationTime < successTime)
				{
					float frac = finishAnimationTime / successTime;
					int count = (int)Mathf.Min(drawingLines.Count * frac + 1, drawingLines.Count);
					for (int i = 0; i < count; i++)
					{
						if (drawingLines[i].LineColor != successLineColor)
							drawingLines[i].LineColor = successLineColor;
					}
				}
				else
				{
					float shortenSpeed = Length / successShortenTime;
					ShortenDrawing(shortenSpeed);
					if (drawing.Count <= 1) Destroy(gameObject);
				}
			}
			else // Instantly make drawing red and dissipate
			{
				if (finishAnimationTime < 1e-3)
				{
					foreach (var line in drawingLines)
					{
						line.LineColor = failureLineColor;
					}
				}
				else
				{
					for (int i = 0; i < drawingLines.Count; i++)
					{
						var line = drawingLines[i];
						Vector3 startVelocity = startPosVelocities[i];
						Vector3 endVelocity = endPosVelocities[i];
						Vector3 midPos = (line.StartPos + line.EndPos) / 2f;
						float beforeLength = (line.StartPos - line.EndPos).magnitude;

						line.StartPos = Vector3.SmoothDamp(line.StartPos, midPos, ref startVelocity, failureTime);
						line.EndPos = Vector3.SmoothDamp(line.EndPos, midPos, ref endVelocity, failureTime);

						startPosVelocities[i] = startVelocity;
						endPosVelocities[i] = endVelocity;

						float afterLength = (line.StartPos - line.EndPos).magnitude;
						line.LineWidth *= afterLength / beforeLength;

						// Destroy line when get small enough
						if (afterLength < 1e-3)
						{
							float velocity = -lineLength;
							line.LineWidth = Mathf.SmoothDamp(line.LineWidth, 0f, ref velocity, failureTime);
							// startPosVelocities[i] = new Vector3(velocity, 0, 0);

							if (line.LineWidth < 1e-3)
							{
								Destroy(line);
								drawingLines.RemoveAt(i);
								startPosVelocities.RemoveAt(i);
								endPosVelocities.RemoveAt(i);
							}
						}
						
						if (drawingLines.Count == 0) Destroy(gameObject);
					}
				}
			}
			finishAnimationTime += Time.deltaTime;
		}
	}
}
