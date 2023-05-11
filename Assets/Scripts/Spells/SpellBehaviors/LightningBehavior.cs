using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpellBehaviors/LightningBehavior")]

public class LightningBehavior : SpellBehavior
{
    public GameObject AOEPrefab;

    public override void spellStartAction()
    {

    }

    public override void spellUpdateAction()
    {

    }

    public override void spellCollideAction(Collider other)
    {

    }

    public override void spellDestroyAction()
    {
        Collider[] hitCheck;
        if (owner != null)
        {
            hitCheck = Physics.OverlapSphere(owner.transform.position, 1, playerLayer);
            foreach (Collider player in hitCheck)
            {
                GameManager.Instance.changeHealth(-damage, !isServer());
            }
        }
    }
}
