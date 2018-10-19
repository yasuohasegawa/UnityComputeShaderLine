using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    private ComputeShaderLine line;
    private Vector3 maxPos = new Vector3(5f, 0.5f, 3f);

    // Use this for initialization
    void Start () {

        line = GetComponent<ComputeShaderLine>();

        int index = 0;
        int numLine = 30;
        for (int i = 0; i < ComputeShaderLine.numeberOfLines; i++)
        {
            float dist = 5f;
            float deg = 360f / (float)numLine;
            float rad = (deg * (float)index) * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * dist;
            float z = Mathf.Sin(rad) * dist;

            if (index < numLine + 1)
            {
                //line.CreateHermitecurve(new Vector3(x, Random.Range(-maxPos.y, maxPos.y), z));
                line.AddPosition(new Vector3(x, 0f, z));
            }
            index++;
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
