using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpellCard : MonoBehaviour
{
    // public int cardNumber;
    public GameObject cardUsedParticles;
    private TextMeshPro spellName;
    private TextMeshPro manaCost;
    private TextMeshPro description;
    private MeshRenderer symbol;

    private int spellIndex = -1;
    public int SpellIndex
    {
        get => spellIndex;
        set
        {
            if (value == spellIndex) return;

            spellIndex = value;
            var particles = Instantiate(cardUsedParticles, transform);
            spellName.text = SpellManager.Instance.spellTypes[spellIndex].spellName;
            manaCost.text = SpellManager.Instance.spellTypes[spellIndex].manaCost.ToString();
            description.text = SpellManager.Instance.spellTypes[spellIndex].spellDesc;
            symbol.material = SpellManager.Instance.spellTypes[spellIndex].spellSymbol;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        spellName = transform.GetChild(0).GetComponent<TextMeshPro>();
        manaCost = transform.GetChild(1).GetComponent<TextMeshPro>();
        description = transform.GetChild(2).GetComponent<TextMeshPro>();
        symbol = transform.GetChild(3).GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    // void Update()
    // {
    //     if (spellName.text != SpellManager.Instance.spellDeck[cardNumber].spellName)
    //         Instantiate(cardUsedParticles, transform.position, new Quaternion());

    //     spellName.text = SpellManager.Instance.spellDeck[cardNumber].spellName;
    //     manaCost.text = SpellManager.Instance.spellDeck[cardNumber].manaCost.ToString();
    //     description.text = SpellManager.Instance.spellDeck[cardNumber].spellDesc;
    //     symbol.material = SpellManager.Instance.spellDeck[cardNumber].spellSymbol;
    // }
}
