using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

// Handles a hand of the player.
[RequireComponent(typeof(ActionBasedController))]
public class XRPlayerHand : MonoBehaviour
{
    public XRPlayerController XR { get; private set; }
    public XRPlayerController.Hand Hand { get; private set; }
    public ActionBasedController Controller { get; private set; }
    public XRRayInteractor UIInteractor { get; private set; }
    public GestureSpellCaster Caster { get; private set; }

    public enum InteractionState
    {
        Default, // Starting state. Not interacting with anything
        UI,      // Hovering over UI. Can only interact with UI in this state
        Drawing, // Drawing a gesture
        Holding, // Holding a spell. Cannot interact with UI or draw
    }
	private InteractionState state;
    public InteractionState State { get => state; set
	{
		if (state == value) return;

		if (value == InteractionState.Holding)
			Controller.SendHapticImpulse(0.3f, 0.3f);
		else if ((state == InteractionState.Default && value == InteractionState.UI)
			|| (state == InteractionState.UI && value == InteractionState.Default))
        	Controller.SendHapticImpulse(0.1f, 0.05f);

		switch ((state, value))
		{
			case (InteractionState.UI, InteractionState.Drawing):
			case (InteractionState.Drawing, InteractionState.UI):
			case (InteractionState.Holding, InteractionState.UI):
			case (InteractionState.Holding, InteractionState.Drawing):
				Debug.LogError("Tried to switch InteractionState of " + name + 
					" from " + state + " to " + value);
				break;
			default:
				state = value;
				break;
		}

		UIInteractor.enabled = state == InteractionState.Default || state == InteractionState.UI;
		Caster.enabled = state != InteractionState.UI;
	} }

	void Awake()
	{
		Controller = GetComponent<ActionBasedController>();
        UIInteractor = GetComponentInChildren<XRRayInteractor>();
        Caster = GetComponentInChildren<GestureSpellCaster>();
		State = InteractionState.Default;
	}

	void Start()
	{
		UIInteractor.hoverEntered.AddListener(_ => State = InteractionState.UI);
		UIInteractor.hoverExited.AddListener(_ => State = InteractionState.Default);
		UIInteractor.hoverEntered.AddListener(_ => Debug.Log("UIInteractor hover entered"));
	}

    // Should only be called by XRPlayerController
    public void Initialize(XRPlayerController xr, XRPlayerController.Hand hand)
    {
        XR = xr;
        Hand = hand;
    }

	void Update()
	{
		if (State == InteractionState.Drawing)
		{
			Controller.SendHapticImpulse(0.08f, 0.1f);
		}
	}
}
