using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public SpellCard Card; // Will cycle this card through the available spells
    public float CardSwitchTime = 5f; // seconds shown per card
    private float cardSwitchTimer = 0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cardSwitchTimer += Time.deltaTime;
        if (cardSwitchTimer > CardSwitchTime)
        {
            cardSwitchTimer = 0f;
            Card.SpellIndex = (Card.SpellIndex + 1) % SpellManager.Instance.spellTypes.Count;
        }
    }
}
