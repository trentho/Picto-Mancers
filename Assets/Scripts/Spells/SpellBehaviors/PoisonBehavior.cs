using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(menuName = "SpellBehaviors/PoisonBehavior")]

public class PoisonBehavior : SpellBehavior
{
    const float DAMAGE_TIME = 2f; // damage every this amount of seconds
    float startTime;
    float touchTime;
    public override void spellStartAction()
    {
        // Rigidbody ownerRB = owner.GetComponent<Rigidbody>();

        // RaycastHit hit;
        // bool didHit = Physics.Raycast(owner.transform.position, owner.GetComponent<SpellBase>().direction.normalized, out hit, Mathf.Infinity, rayCastHitLayer);
        // Vector3 target = hit.point;
        startTime = Time.time;
        touchTime = 0f;
        owner.transform.position = new Vector3(owner.transform.position.x,0.5f,owner.transform.position.z);
    }

    public override void spellUpdateAction()
    {
        Collider[] hitCheck;
        if (owner != null)
        {
            hitCheck = Physics.OverlapSphere(owner.transform.position, 1, playerLayer);
            foreach (Collider collider in hitCheck)
            {
                NetworkPlayer player = collider.GetComponentInParent<NetworkPlayer>();
                if (player != null && player.IsOwnedByServer != isServer()) // only collide with opponent
                {
                    touchTime += Time.deltaTime;
                    if (touchTime > DAMAGE_TIME) {
                        Debug.Log("pooop!");
                        GameManager.Instance.changeHealth(-damage, !isServer());
                        touchTime = 0f;
                    }
                    return;
                }
            }
        }
        // Not colliding with player:
        touchTime = 0f;
    }

    public override void spellCollideAction(Collider other)
    {
        
    }

    public override void spellDestroyAction()
    {
        
    }
}
