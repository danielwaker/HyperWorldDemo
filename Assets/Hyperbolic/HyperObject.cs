using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class HyperObject : MonoBehaviour
{
    private Renderer[] hyperRenderers;
    private WarpCollider[] warpColliders;
    private MaterialPropertyBlock propBlock;

    private int hyperRotID;
    private int hyperTileRotID;
    private int hyperMapRotID;
    private int enabledID;
    private int projInterpID;
    private int kleinVID;
    private int camHeightID;
    private int debugColorID;
    private int tanKHeightID;

    public static GyroVector worldGV = GyroVector.identity;
    public static Vector2 worldLook = new Vector2(0, 1);
    public static float worldInterp = 0.0f;
    public static float camHeight;
    public static float debugColor;

    [HideInInspector] public GyroVector localGV = GyroVector.identity;
    [HideInInspector] public GyroVector composedGV = GyroVector.identity;
    private GyroVector mapGV = GyroVector.identity;
    public Vector3 hyperPos;
    public bool fixedLocalY = false;
    public bool fixedLocalXZ = false;

    void Awake() {
        propBlock = new MaterialPropertyBlock();
        hyperRotID = Shader.PropertyToID("_HyperRot");
        hyperTileRotID = Shader.PropertyToID("_HyperTileRot");
        hyperMapRotID = Shader.PropertyToID("_HyperMapRot");
        enabledID = Shader.PropertyToID("_Enable");
        projInterpID = Shader.PropertyToID("_Proj");
        kleinVID = Shader.PropertyToID("_KleinV");
        camHeightID = Shader.PropertyToID("_CamHeight");
        debugColorID = Shader.PropertyToID("_DebugColor");
        tanKHeightID = Shader.PropertyToID("_TanKHeight");
        //Debug.Log("this is the starting " + transform.name + hyperPos);
        //Copy starting position into the local gyrovector.
        localGV.vec = hyperPos;
    }

    void Start() {
        //Make sure all objects created during awake are available before getting components
        hyperRenderers = null;
        warpColliders = null;
        AddChildObject(gameObject);
    }

    public void AddChildObject(GameObject obj) {
        //Disable dynamic occlusion
        Renderer[] newHyperRenderers = obj.GetComponentsInChildren<Renderer>(true);
        WarpCollider[] newWarpColliders = obj.GetComponentsInChildren<WarpCollider>(true);
        foreach (Renderer hyperRenderer in newHyperRenderers) {
            hyperRenderer.allowOcclusionWhenDynamic = false;
        }

        //Disable frustum culling
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters) {
            if (meshFilter.sharedMesh) {
                meshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, 1000f * Vector3.one);
            }
        }

        //Merge the arrays
        if (hyperRenderers == null) {
            hyperRenderers = newHyperRenderers;
            warpColliders = newWarpColliders;
        } else {
            Array.Resize(ref hyperRenderers, hyperRenderers.Length + newHyperRenderers.Length);
            Array.Resize(ref warpColliders, warpColliders.Length + newWarpColliders.Length);
            Array.Copy(newHyperRenderers, 0, hyperRenderers, hyperRenderers.Length - newHyperRenderers.Length, newHyperRenderers.Length);
            Array.Copy(newWarpColliders, 0, warpColliders, warpColliders.Length - newWarpColliders.Length, newWarpColliders.Length);
        }
    }

    private static bool IsBehindView(GyroVector gv) {
        Vector3 ci = gv.gyr * gv.vec;
        float dp = ci.x * worldLook.x + ci.z * worldLook.y;
        return dp <= 0.0f;
    }

    void LateUpdate() {
        //Calculate the hyper-rotation from the player's point of view
        if (fixedLocalXZ) {
            composedGV = localGV;
            mapGV = localGV;
        } else {
            composedGV = localGV + worldGV;
            mapGV = localGV + worldGV.ProjectToPlane();
        }

        //Reset colliders
        foreach (WarpCollider warpCollider in warpColliders) {
            warpCollider.ResetActive();
        }

        //Calculate if the center of the object is really far away
        float dist2 = composedGV.vec.sqrMagnitude;

        //Check how far away tiles are and don't draw ones that are too far to be seen.
        if (HM.K < 0.0f && HM.N < 10 && ((dist2 > 0.9975f) || (dist2 > 0.99f && (IsBehindView(composedGV))))) {
            foreach (Renderer hyperRenderer in hyperRenderers) {
                hyperRenderer.enabled = false;
                //Debug.Log(gameObject.name + " is having their hyper renderer disabled.");
            }
        } else {
            //Update shader properties
            if (gameObject.name.Contains("CoordCube"))
            {
                if (!(HM.K < 0.0f))
                    Debug.Log(gameObject.name + " fails on the HM.K < 0.0f because HM.K is " + HM.K);
                if (!(HM.N < 10))
                    Debug.Log(gameObject.name + " fails on the HM.N < 10 because HM.N is " + HM.N);
                if (!(dist2 > 0.9975f))
                    Debug.Log(gameObject.name + " fails on dist2 > 0.99f because dist2 is " + dist2);
                if (!(IsBehindView(composedGV)))
                    Debug.Log(gameObject.name + " fails on the IsBehindView");
            }
            foreach (Renderer hyperRenderer in hyperRenderers) {
                hyperRenderer.enabled = true;
                hyperRenderer.GetPropertyBlock(propBlock);
#if UNITY_EDITOR
                propBlock.SetFloat(enabledID, (Application.isEditor && !EditorApplication.isPlaying) ? 0.0f : 1.0f);
#else
                propBlock.SetFloat(enabledID, 1.0f);
#endif
                propBlock.SetMatrix(hyperRotID, composedGV.ToMatrix());
                propBlock.SetMatrix(hyperTileRotID, localGV.ToMatrix());
                propBlock.SetMatrix(hyperMapRotID, mapGV.ToMatrix());
                propBlock.SetFloat(projInterpID, worldInterp);
                propBlock.SetFloat(kleinVID, HM.KLEIN_V);
                propBlock.SetFloat(tanKHeightID, HM.useTanKHeight ? 1.0f : 0.0f);
                propBlock.SetFloat(camHeightID, fixedLocalY ? 0.0f : camHeight);
                propBlock.SetFloat(debugColorID, debugColor);
                hyperRenderer.SetPropertyBlock(propBlock);
            }

            //Only update collisions if the object is relatively close by
            //if (dist2 < 0.8f) {
            foreach (WarpCollider warpCollider in warpColliders) {
                warpCollider.UpdateMesh(composedGV);
            }
            //}
        }
    }

    //When updating an object dynamically, this may need to be called manually.
    public void UpdateCollisions() {
        Debug.Assert(!fixedLocalY && !fixedLocalXZ);
        GyroVector gv = localGV + worldGV;
        foreach (WarpCollider warpCollider in warpColliders) {
            warpCollider.UpdateMesh(gv);
        }
    }
}
