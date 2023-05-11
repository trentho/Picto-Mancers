using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningAOE : MonoBehaviour
{
    public float upTime;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(waitRoutine());
    }

    IEnumerator waitRoutine() {
        yield return new WaitForSeconds(upTime);
        Destroy(this.gameObject);
    }
}
