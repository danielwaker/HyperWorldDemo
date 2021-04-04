using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DynamicTexture : MonoBehaviour {
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private int textureID;

    public Texture2D texture;

    void Awake() {
        meshRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        textureID = Shader.PropertyToID("_MainTex");
    }
    void Update() {
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetTexture(textureID, texture);
        meshRenderer.SetPropertyBlock(propBlock);
    }
}
