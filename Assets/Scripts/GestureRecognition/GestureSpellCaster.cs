using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Barracuda;

[RequireComponent(typeof(XRRayInteractor), typeof(XRInteractorLineVisual))]
public class GestureSpellCaster : MonoBehaviour
{
	public InputActionProperty DrawGestureAction; // Action for starting and ending drawing
    public GestureCanvas CanvasPrefab;
	[SerializeField] NNModel spellModelAsset;

    public GameObject Reticle;
    public float ReticleHover = 0.1f;

    /// Invoked when a gesture is drawn. Passes the index of the 
    /// spell drawn (-1 if garbage).
	public UnityEvent<int> OnGestureDrawn;

    XRPlayerHand hand;
    XRRayInteractor rayInteractor;
    XRInteractorLineVisual lineVisual;

    public GestureCanvas CurrentCanvas { get; private set; } // null if not currently drawing
	public int heldSpell = -1; // Spell currently being held

	Model spellRuntimeModel;
	IWorker gestureRecognitionWorker;

    void Awake()
    {
        hand = GetComponentInParent<XRPlayerHand>();
        rayInteractor = GetComponent<XRRayInteractor>();
        lineVisual = GetComponent<XRInteractorLineVisual>();
    }

    void Start()
    {
        rayInteractor.enabled = hand.State == XRPlayerHand.InteractionState.Holding;

		DrawGestureAction.action.started += ctx =>
		{
            if (!isActiveAndEnabled) return;
			if (heldSpell >= 0 && hand.State == XRPlayerHand.InteractionState.Holding)
            {
                CastSpell();
            }
			else if (CurrentCanvas == null)
            {
                StartDrawing();
            }
            else Debug.LogWarning("Unexpected draw gesture action when already drawing.");
		};
		DrawGestureAction.action.canceled += ctx =>
		{
            if (!isActiveAndEnabled) return;
			if (CurrentCanvas != null) StopDrawing();
		};

        MultiplayerManager.Instance.OnConnected += () =>
        {
            if (hand.State == XRPlayerHand.InteractionState.Holding) 
                UnholdSpell();
        };

		spellRuntimeModel = ModelLoader.Load(spellModelAsset);
		gestureRecognitionWorker = WorkerFactory.CreateWorker(
            WorkerFactory.Type.ComputePrecompiled, spellRuntimeModel);
    }

    void OnDestroy()
    {
        gestureRecognitionWorker.Dispose();
    }

    // Returns (position, normal, hit)
    public (Vector3, Vector3, bool) GetRaycastHit()
    {
        RaycastHit hit;
        if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
        {
            return (hit.point, hit.normal, true);
        }

        // Normal if not hitting anything:
        bool isLine = rayInteractor.lineType == XRRayInteractor.LineType.StraightLine;
        Vector3 normal = isLine ? -transform.forward : Vector3.up;

        Vector3[] points = {};
        int numPoints;
        if (rayInteractor.GetLinePoints(ref points, out numPoints))
        {
            return (points.Last(), normal, false);
        }
        return (transform.position, normal, false);
    }

    // Important: check that XRPlayerHand.State != Holding before calling
    public void HoldSpell(int spellIndex)
    {
        Debug.Assert(heldSpell == -1);
        Debug.Assert(hand.State != XRPlayerHand.InteractionState.Holding);

        hand.State = XRPlayerHand.InteractionState.Holding;
        heldSpell = spellIndex;
        var spell = SpellManager.Instance.spellTypes[spellIndex];
        rayInteractor.lineType = spell.arcTrajectory ? XRRayInteractor.LineType.ProjectileCurve : XRRayInteractor.LineType.StraightLine;
        rayInteractor.maxRaycastDistance = (!spell.arcTrajectory && spell.instantaneousTravel) ? spell.speed : 5;
        rayInteractor.velocity = spell.speed == 0 ? 10 : spell.speed;
        rayInteractor.enabled = true;
    }

    // Hold the spell but don't cast it
    // Important: check that XRPlayerHand.State == Holding before calling
    public void UnholdSpell() 
    {
        Debug.Assert(heldSpell != -1);
        Debug.Assert(hand.State == XRPlayerHand.InteractionState.Holding);

        rayInteractor.enabled = false;
        hand.State = XRPlayerHand.InteractionState.Default;
        heldSpell = -1;
    }

    // Unhold and cast the spell
    void CastSpell() 
    {
        Debug.Assert(heldSpell != -1);
        Debug.Assert(hand.State == XRPlayerHand.InteractionState.Holding);

        hand.Controller.SendHapticImpulse(0.8f, 0.1f);

        SpellManager.Instance?.CastSpell(
            heldSpell, transform.position, transform.forward, GetRaycastHit().Item1, hand.Hand);
        UnholdSpell();
    }

    void StartDrawing()
    {
        Debug.Assert(CurrentCanvas == null);
        Debug.Assert(hand.State != XRPlayerHand.InteractionState.Drawing);

        CurrentCanvas = Instantiate(CanvasPrefab).GetComponent<GestureCanvas>();
        CurrentCanvas.drawer = transform;
        hand.State = XRPlayerHand.InteractionState.Drawing;
    }

    void StopDrawing()
    {
        // Get the drawn gesture as a list of points
        var drawing = CurrentCanvas.GetDrawing2D();

		// Rasterize the drawing
        var raster = drawing.Normalized(23).Translated(2.5f, 2.5f).Rasterized();
        // GestureDrawing.PrintRaster(raster);

        // Run through ML model
		Tensor input = new Tensor(1, 28, 28, 1, raster.Cast<float>().ToArray());
		gestureRecognitionWorker.Execute(input);
		Tensor output = gestureRecognitionWorker.PeekOutput();
        // Debug.Log(string.Join(", ", output.AsFloats()));
		int gestureIndex = output.ArgMax()[0];
		input.Dispose();
		output.Dispose();

		int[] classes = {
            0,  // circle -> fireball
            2,  // gate -> shield
            1,  // lightning -> lightning
            4,  // star -> meteor
            5,  // waves -> poison
            3,  // spiral -> smoke
            6,  // bounce -> counterspell
            -1, // garbage -> garbage
        };
		var spellIndex = classes[gestureIndex];

        OnGestureDrawn.Invoke(spellIndex);

		if (SpellManager.Instance.SelectSpell(spellIndex))
		{
            HoldSpell(spellIndex);
            CurrentCanvas.FinishDrawing(true);
		}
        else
        {
            hand.State = XRPlayerHand.InteractionState.Default;
            CurrentCanvas.FinishDrawing(false);
        }
        CurrentCanvas = null;
    }

    void Update()
    {
        Reticle.SetActive(false);
        if (heldSpell != -1)
        {
            var spell = SpellManager.Instance.spellTypes[heldSpell];
            //if (spell.instantaneousTravel || spell.arcTrajectory)
            {
                var (position, normal, hit) = GetRaycastHit();
                if (hit) position += normal * ReticleHover;
                Reticle.transform.position = position;
                Reticle.transform.GetChild(0).GetComponent<MeshRenderer>().material = SpellManager.Instance.spellTypes[heldSpell].spellSymbol;
                // Direction the "forward" direction of the reticle points (ie if it were an arrow)
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward+transform.up, normal);
                Reticle.transform.rotation = Quaternion.LookRotation(forward, normal);

                // Scale reticle based on distance
                float distance = Vector3.Distance(Camera.main.transform.position, position);
                float scale = Mathf.Clamp(distance/3, 0.01f, 1) * 0.2f;
                Reticle.transform.localScale = transform.localScale = new Vector3(scale, scale, scale);
                Reticle.SetActive(true);
            }
        }
    }
}
