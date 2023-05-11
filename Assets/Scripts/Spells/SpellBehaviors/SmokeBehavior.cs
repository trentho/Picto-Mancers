using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpellBehaviors/SmokeBehavior")]

public class SmokeBehavior : SpellBehavior
{

    public override void spellStartAction()
    {

    }

    public override void spellUpdateAction()
    {

    }

    public override void spellCollideAction(Collider other)
    {
        Debug.Log("smoke position: "+owner.transform.position);
    }

    public override void spellDestroyAction()
    {

    }
}
