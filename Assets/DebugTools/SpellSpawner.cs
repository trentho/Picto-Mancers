using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;
using System.Collections;

public class SpellSpawner : MonoBehaviour
{
	[SerializeField] TextMeshProUGUI currentSpellText;

	private int currentSpell = 0;


	void Update()
	{

		for (int i = 0; i < SpellManager.Instance.spellTypes.Count; i++)
		{
			if (Input.GetKeyDown(i + ""))
			{
				currentSpell = i;
				Debug.Log("Casting spell...");
				SpellManager.Instance?.CastSpell(currentSpell, transform.position, Vector3.right, Vector3.zero, 0);
			}
		}
		currentSpellText?.SetText("Current spell: " + SpellManager.Instance?.spellTypes[currentSpell]);
        
	}

}