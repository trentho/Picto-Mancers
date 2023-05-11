using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpellBehaviors/ScatterBehavior")]

public class ScatterBehavior : SpellBehavior
{
    
    public override void spellStartAction()
    {
        
    }

    public override void spellUpdateAction()
    {
        
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
