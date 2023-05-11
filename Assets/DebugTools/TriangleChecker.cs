using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleChecker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		foreach(Transform child in transform)
		{
			int triangleCount = 0;
			foreach(MeshFilter m in child.GetComponentsInChildren<MeshFilter>())
			{
				triangleCount += m.mesh.triangles.Length;
			}
			Debug.Log(child.name + " has " + triangleCount.ToString() + " triangles");
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
