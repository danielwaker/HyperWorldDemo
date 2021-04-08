using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperCamTexture : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Camera>().ResetReplacementShader();
        GetComponent<Camera>().SetReplacementShader(Shader.Find("Custom/EuclideanShader"), "HyperRenderType");
    }
}
