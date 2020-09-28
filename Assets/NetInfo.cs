using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetInfo : MonoBehaviour
{

    public string ID = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateMat(Color col)
    {
        Renderer r = GetComponent<Renderer>();
        if(r)
        {
            r.material.SetColor("_Color", col);
        }
    }
}
