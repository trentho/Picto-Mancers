using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReticleDistScale : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("POOOP");
        Vector3 campos = Camera.main.transform.position;
        if (Vector3.Distance(campos, transform.position) < 1)
            transform.localScale = new Vector3(0.2f,0.2f,0.2f)*Vector3.Distance(campos, transform.position);
        else
            transform.localScale = new Vector3(0.2f,0.2f,0.2f);
    }
}
