using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpellBehaviors/BoomerangBehavior")]

public class BoomerangBehavior : SpellBehavior
{

    private Vector3 startPos;

    public override void spellStartAction()
    {
        startPos = owner.transform.position;
    }

    public override void spellUpdateAction()
    {
        //return to player
        owner.rigidbody.velocity += (startPos - owner.transform.position).normalized/40;
    }

    public override void spellCollideAction(Collider other)
    {
        if ((playerLayer | 1 << other.gameObject.layer) == playerLayer)
        {
            GameManager.Instance.changeHealth(-damage, !isServer());
        }
    }

    public override void spellDestroyAction()
    {
        
    }

}
