using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpellBehaviors/MeteorBehavior")]

public class MeteorBehavior : SpellBehavior
{
    public override void spellStartAction()
    {
        // Rigidbody ownerRB = owner.GetComponent<Rigidbody>();

        // RaycastHit hit;
        // Debug.Log("owner pos: "+owner.transform.position);
        // Debug.Log("owner vel: "+ownerRB.velocity.normalized);
        // bool didHit = Physics.Raycast(owner.transform.position, owner.GetComponent<SpellBase>().direction.normalized, out hit, Mathf.Infinity, rayCastHitLayer);
        // Debug.Log("didhit: "+didHit);
        // Vector3 landingPoint = hit.point;
        // Debug.Log("landing point: "+landingPoint);

        // owner.transform.position = landingPoint + new Vector3(0, 100, 0);
        owner.rigidbody.velocity = speed * new Vector3(0, -1, 0);
        owner.transform.position = owner.transform.position + new Vector3(0, 50, 0);
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
