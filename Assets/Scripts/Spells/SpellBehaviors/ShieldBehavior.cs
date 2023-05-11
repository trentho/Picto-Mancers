using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(menuName = "SpellBehaviors/ShieldBehavior")]

public class ShieldBehavior : SpellBehavior
{
    public GameObject shieldPrefab;

    public override void spellStartAction()
    {
        Debug.Log("Shield behavior start");
        Vector3 direction = owner.direction;
        GameObject shield = Instantiate(shieldPrefab, this.owner.transform.position, Quaternion.LookRotation(direction));
        if (MultiplayerManager.Instance.IsConnected)
            shield.GetComponent<NetworkObject>().Spawn();
        Destroy(this.owner.gameObject);
    }

    public override void spellUpdateAction()
    {

    }

    public override void spellCollideAction(Collider other)
    {

    }

    public override void spellDestroyAction()
    {

    }
}
