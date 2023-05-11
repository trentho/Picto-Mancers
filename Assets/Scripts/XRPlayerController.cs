using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public static class HandExtensions
{
    public static XRPlayerController.Hand Other(this XRPlayerController.Hand hand) => 
        hand == XRPlayerController.Hand.Left ? XRPlayerController.Hand.Right : XRPlayerController.Hand.Left;
}

[RequireComponent(typeof(XROrigin))]
public class XRPlayerController : MonoBehaviour
{
    [SerializeField] NetworkPlayer player; // the player that is controlled by this XRPlayerController
    [SerializeField] GameObject simulator; // Only spawned if not on headset
    [SerializeField] XRPlayerHand leftHand;
    [SerializeField] XRPlayerHand rightHand;

    public static XRPlayerController Main { get; private set; } // Main controller (first in scene)
    public XROrigin Origin { get; private set; }
    public Camera Camera { get => Origin.Camera; }
    public float CameraYaw { get => Origin.Camera.transform.rotation.eulerAngles.y; }
    public Vector3 CameraPosition { get => Camera.transform.position; }
    public Vector3 PlayerPosition { get => new Vector3(CameraPosition.x, transform.position.y, CameraPosition.z); }
    public Quaternion CameraRotation { get => Camera.transform.rotation; }
    public Quaternion PlayerRotation { get => Quaternion.AngleAxis(CameraYaw, Vector3.up); }
    public NetworkPlayer Player { get => player; }
	public CharacterController CharacterController { get; private set; }

    public enum Hand { Right, Left }
    public XRPlayerHand GetHand(Hand hand) => hand == XRPlayerController.Hand.Left ? leftHand : rightHand;
    public XRPlayerHand LeftHand { get => leftHand; }
    public XRPlayerHand RightHand { get => rightHand; }

    void Awake()
    {
        if (!XRSettings.enabled && simulator != null) Instantiate(simulator);

        LeftHand.Initialize(this, Hand.Left);
        RightHand.Initialize(this, Hand.Right);

        Application.runInBackground = true;
        Origin = GetComponent<XROrigin>();
		CharacterController = GetComponent<CharacterController>();
        if (Origin.Camera.tag == "MainCamera") Main = this;
        ConnectToPlayer(player);
    }

	public void ConnectToPlayer(NetworkPlayer player)
    {
        this.player = player;
        if (player != null)
        {
            //model hand tracking
            //player.GetComponent<NetworkPlayer>().handL.transform.parent = leftController.transform;
            //player.GetComponent<NetworkPlayer>().handR.transform.parent = rightController.transform;
            //render own mesh invisible
            if (Main == this)
                player.transform.GetChild(1).gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        }
    }

    void Update()
    {
		// idek exactly how this works. it just moves the camera and controller towards each other somehow
		Vector3 realOffset = Camera.transform.position - transform.position;
		realOffset.y = 0;
		if (realOffset.magnitude > 1f)
		{
			Camera.transform.parent.position -= (realOffset-realOffset.normalized)*10 * Time.deltaTime;
		}
		realOffset = Camera.transform.position - transform.position;
		realOffset.y = 0;
		if (realOffset.magnitude > 0.2f)
		{
			Vector3 initialPosition = transform.position;
			GetComponent<CharacterController>().Move((realOffset-realOffset.normalized*0.2f)*10 * Time.deltaTime);
			Vector3 offset = transform.position - initialPosition;
			offset.y = 0;
			Camera.transform.parent.position -= offset;
		}

        if (Player != null)
        {
            Vector3 camPos = Origin.Camera.transform.position;
			Player.movement.Value = new Vector3(camPos.x, transform.position.y, camPos.z) - Player.transform.position;
			Player.facingDirection.Value = Origin.Camera.transform.forward;
			Player.angle.Value = Vector3.Angle(Player.movement.Value, Origin.Camera.transform.forward);
            Player.transform.position = new Vector3(camPos.x, transform.position.y, camPos.z);
        }
    }
}
