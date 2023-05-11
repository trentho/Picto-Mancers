using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShieldObject : NetworkBehaviour
{
    public float upTime;
    public GameObject particles;

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(particles, transform);
        if (IsServer || !MultiplayerManager.Instance.IsConnected) // despawn if server or offline
        {
            StartCoroutine(waitRoutine());
        }
    }

    IEnumerator waitRoutine() {
        yield return new WaitForSeconds(upTime);
        Destroy(this.gameObject);
    }
}
