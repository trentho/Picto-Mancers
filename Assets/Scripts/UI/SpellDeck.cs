using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellDeck : MonoBehaviour
{
    public List<SpellCard> cards;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            // Probably makes more sense to use a UnityEvent in SpellManager here instead of doing this every update
            // Also spell deck should be a list of int indices
            var spellManager = SpellManager.Instance;
            cards[i].SpellIndex = spellManager.spellTypes.IndexOf(spellManager.spellDeck[i]);
        }
    }
}
