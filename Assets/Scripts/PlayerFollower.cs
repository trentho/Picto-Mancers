using UnityEngine;

// Follows the player. Useful for UI
public class PlayerFollower : MonoBehaviour
{
	Vector3 positionOffset = Vector3.zero;
	float angleOffset = 0f;

	Vector3 velocity = Vector3.zero;
	bool recentering = false;

	float angularVelocity = 0.0f;
	bool recenteringAngle = false;

	[SerializeField] XRPlayerController xr;

	// Height from the ground
	public float MinHeight = Mathf.NegativeInfinity;
	public float MaxHeight = Mathf.Infinity;

	public float MinFollowDistance = .2f;
	public float MaxFollowDistance = .5f;
	public float FollowTime = 0.2f;

	public float MinFollowAngle = 25f;
	public float MaxFollowAngle = 45f;
	public float FollowAngleTime = 0.2f;

	public bool IsFollowing = true;

	// used when IsFollowing is set to false
	private float lastTargetAngle;
	private Vector3 lastTargetPosition;

	private float TargetAngle
	{
		get
		{
			if (!IsFollowing) return lastTargetAngle;
			return lastTargetAngle = xr.Camera.transform.rotation.eulerAngles.y + angleOffset;
		}
	}
	private Vector3 TargetPosition
	{
		get
		{
			if (!IsFollowing) return lastTargetPosition;
			Vector3 target = xr.Camera.transform.position;
			target += Quaternion.AngleAxis(TargetAngle, Vector3.up) * positionOffset; // apply offset
			float ground = xr.transform.position.y;
			target.y = Mathf.Clamp(target.y, ground + MinHeight, ground + MaxHeight); // clamp height
			return lastTargetPosition = target;
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		xr ??= XRPlayerController.Main; // There must be an XRPlayerController in the scene

        // Initial offsets are used
		positionOffset = transform.position - xr.Camera.transform.position;
		angleOffset = transform.rotation.eulerAngles.y - xr.Camera.transform.eulerAngles.y;

        // Set last targets for when IsFollowing starts as false
        lastTargetPosition = transform.position;
        lastTargetAngle = transform.rotation.eulerAngles.y;
	}

	// Update is called once per frame
	void Update()
	{
		transform.position = transform.position.SmoothTransform(TargetPosition, ref velocity,
			MinFollowDistance, MaxFollowDistance, ref recentering, FollowTime);

		Vector3 euler = transform.rotation.eulerAngles;
		euler.y = euler.y.SmoothTransformAngle(TargetAngle, ref angularVelocity,
			MinFollowAngle, MaxFollowAngle, ref recenteringAngle, FollowAngleTime, 1f);
		transform.rotation = Quaternion.Euler(euler);
	}
}
