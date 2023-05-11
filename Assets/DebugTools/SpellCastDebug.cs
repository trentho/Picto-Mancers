using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;
using System.Collections;

public class SpellCastDebug : MonoBehaviour
{
	[SerializeField] XRPlayerController xr; // defaults to main controller
	[SerializeField] TextMeshProUGUI currentSpellText;
	
	[SerializeField] float delay = 0;

	private int currentSpell = 0;

	// Start is called before the first frame update
	void Start()
	{
		xr ??= XRPlayerController.Main; // set controller if not assigned

		// XR controller
		if (xr != null)
		{
			// left hand to select spell
			xr.LeftHand.Controller.activateAction.action.started += ctx =>
			{
				currentSpell = (currentSpell + 1) % SpellManager.Instance.spellTypes.Count;
				SpellManager.Instance?.SelectSpell(currentSpell);
			};
			// right hand to cast spell
			xr.RightHand.Controller.activateAction.action.started += ctx =>
			{
				Transform trans = xr.RightHand.transform;
				StartCoroutine(CastSpellDelayed(trans.position, trans.forward));
			};
		}
	}

	void Update()
	{
		// Keyboard
		if (Input.GetKeyDown("space") && xr == null)
			StartCoroutine(CastSpellDelayed(transform.position, new Vector3(0, 0, 1)));

		for (int i = 0; i < SpellManager.Instance.spellTypes.Count; i++)
		{
			if (Input.GetKeyDown(i + ""))
			{
				//SpellManager.Instance?.SelectSpell(i);
				currentSpell = i;
				XRPlayerController.Main.RightHand.Caster.HoldSpell(currentSpell);
			}
		}

	
		// if (SpellManager.Instance.selectedSpell != null)
		// 	currentSpellText?.SetText("Current spell: " + SpellManager.Instance?.selectedSpell.name);
	}

    IEnumerator CastSpellDelayed(Vector3 position, Vector3 direction) {
        yield return new WaitForSeconds(delay);
		SpellManager.Instance?.CastSpell(currentSpell, position, direction, Vector3.zero, XRPlayerController.Hand.Right);
    }
}