using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(menuName = "SpellBehaviors/CounterspellBehavior")]

public class CounterspellBehavior : SpellBehavior
{
    public LayerMask spellLayer;
    public AudioClip counterSound;

    public override void spellStartAction()
    {

    }

    public override void spellUpdateAction()
    {
        Collider[] hitCheck;
        hitCheck = Physics.OverlapSphere(owner.transform.position, 1, spellLayer);
        foreach (Collider collider in hitCheck)
        {
            var otherOwner = collider.GetComponent<SpellBase>();
            if (otherOwner == this.owner) continue;
            SpellManager.Instance?.GiftSpellClientRpc(owner.isServerPlayer, otherOwner.spellIndex, owner.hand);
            SoundManager.Instance.playSound(counterSound);
            Destroy(otherOwner.gameObject);
            Destroy(this.owner.gameObject); // Destroys owner without playing the particles
        }
    }

    public override void spellCollideAction(Collider other)
    {

        // if (owner != null)
        // {
        //     //hitCheck = Physics.OverlapSphere(owner.transform.position, 1, spellLayer);
        //     //foreach (Collider spell in hitCheck)
        //     if ((spellLayer | 1 << other.gameObject.layer) == spellLayer)
        //     {
        //         var spell = other.gameObject.GetComponent<SpellBase>();
        //         SpellManager.Instance?.GiftSpellClientRpc(owner.GetComponent<SpellBase>().isServerPlayer, spell.spellIndex.Value, spell.hand);
        //         Destroy(other.gameObject);
        //         //SoundManager.Instance.playSound(counterSound);
        //     }
        // }
    }

    public override void spellDestroyAction()
    {
        
    }
}
