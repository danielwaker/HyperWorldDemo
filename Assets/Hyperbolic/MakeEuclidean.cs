using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeEuclidean : MonoBehaviour
{
    private Renderer[] hyperRenderers;
    private MaterialPropertyBlock propBlock;

    private int enabledID;

    void Awake()
    {
        hyperRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        enabledID = Shader.PropertyToID("_Enable");
        foreach (Renderer hyperRenderer in hyperRenderers) {
            hyperRenderer.allowOcclusionWhenDynamic = false;
            hyperRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(enabledID, 0.0f);
            hyperRenderer.SetPropertyBlock(propBlock);
        }
    }
}
