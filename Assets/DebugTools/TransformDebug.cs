using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugTransform : MonoBehaviour
{

    private TextMeshPro debugText;
    [SerializeField] private Transform trackedObject;

    // Start is called before the first frame update
    void Start()
    {
        debugText = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        debugText.text = trackedObject.ToString()
         + "\npos: " + trackedObject.transform.position.ToString()
         + "\nrot: " + trackedObject.transform.rotation.ToString()
         + "\nscl: " + trackedObject.transform.localScale.ToString();
    }
}
