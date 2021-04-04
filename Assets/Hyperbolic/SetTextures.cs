using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SetTextures : MonoBehaviour {
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Mesh meshCopy;
    private int textureID;
    private int aomapID;
    private int boundaryAOID;
    private int ambientID;
    private int suppressAOID;
    private int colorID;
    private int fogID;

    public Texture2D texture;
    public Texture2D aomap;
    public float ambient = 0.6f;
    public float suppressAO = 0.0f;
    public Color colorize = Color.white;
    public float overrideBoundaryAO = -1.0f;

    public static Dictionary<int, Mesh> alteredMesh = new Dictionary<int, Mesh>();

    private void Awake() {
        meshRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        textureID = Shader.PropertyToID("_MainTex");
        aomapID = Shader.PropertyToID("_AOTex");
        boundaryAOID = Shader.PropertyToID("_BoundaryAO");
        suppressAOID = Shader.PropertyToID("_SuppressAO");
        ambientID = Shader.PropertyToID("_Ambient");
        colorID = Shader.PropertyToID("_Color");
        fogID = Shader.PropertyToID("_Fog");

        if (texture == null) { texture = Texture2D.whiteTexture; }
        if (aomap == null) { aomap = Texture2D.whiteTexture; }

        UpdateTextures();
    }

    public void UpdateTextures() {
        if (propBlock != null) {
            float boundaryAO = (overrideBoundaryAO >= 0.0f ? overrideBoundaryAO : WorldBuilder.globalBounryAO);
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetTexture(textureID, texture);
            propBlock.SetTexture(aomapID, aomap);
            propBlock.SetFloat(boundaryAOID, boundaryAO);
            propBlock.SetFloat(fogID, WorldBuilder.globalFog);
            propBlock.SetFloat(suppressAOID, suppressAO);
            propBlock.SetFloat(ambientID, ambient);
            propBlock.SetColor(colorID, colorize);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void UpdateColorOnly() {
        if (propBlock != null) {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorID, colorize);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
}
