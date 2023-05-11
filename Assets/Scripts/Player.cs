using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
	private Animator anim;
	[HideInInspector] public NetworkVariable<float> angle
         = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
	[HideInInspector] public NetworkVariable<Vector3> facingDirection
         = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
	[HideInInspector] public NetworkVariable<Vector3> movement
         = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
	public GameObject wizardMesh;
	public GameObject handL;
	public GameObject handR;
	public GameObject neck;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			ConnectToXRController();
		}
	}

	private void Start()
	{
		anim = GetComponentInChildren<Animator>();
		wizardMesh = transform.GetChild(1).gameObject;
	}

	void ConnectToXRController()
	{
		if (XRPlayerController.Main != null)
		{
			XRPlayerController.Main.ConnectToPlayer(this);
			//neck.gameObject.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
		}
		else
		{
			Debug.LogWarning("Could not find an XRController to connect player to");
		}
	}

	void Update()
	{
        Vector3 facing = facingDirection.Value;
        facing.y = 0;
		wizardMesh.transform.rotation = Quaternion.LookRotation(facing);
		float movementDirection = angle.Value + 90;
		anim.SetFloat("Direction", movementDirection);
		anim.SetBool("IsMoving", movement.Value.magnitude > 0.005);
	}
}
