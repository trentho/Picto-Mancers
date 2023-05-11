using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAutoDestroy : MonoBehaviour
{
    private ParticleSystem parts;

    // Start is called before the first frame update
    void Start()
    {
        parts = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!parts.isEmitting && parts.particleCount==0)
            Destroy(this.gameObject);
    }
}
