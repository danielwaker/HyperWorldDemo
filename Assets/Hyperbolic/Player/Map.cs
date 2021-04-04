using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour {
    private const float LAG_HALF_LIFE = 0.05f; //seconds
    private const float DEGREES_TILT = 40.0f; //degrees
    private const float ANIM_SPEED = 2.0f;

    private Quaternion followRotation = Quaternion.identity;
    private Quaternion camRotation = Quaternion.identity;
    private float mapInterp = 0.0f;
    private int colorID;
    private MaterialPropertyBlock propBlock;
    private MeshRenderer paperRenderer;
    private HyperObject ho;
    private int mapDest = 0;

    public GameObject mapCam;
    public GameObject paper;

    void Awake() {
        propBlock = new MaterialPropertyBlock();
        colorID = Shader.PropertyToID("_Color");
        paperRenderer = paper.GetComponent<MeshRenderer>();
        transform.localRotation = Quaternion.AngleAxis(DEGREES_TILT, Vector3.right);
        ho = GetComponent<HyperObject>();
    }

    void Update() {
        //Update animation
        if (mapDest == 0) {
            mapInterp = Mathf.Max(mapInterp - Time.deltaTime * ANIM_SPEED, 0.0f);
            if (mapInterp == 0.0f) {
                gameObject.SetActive(false);
                mapCam.SetActive(false);
            }
        } else if (mapDest == 1) {
            mapInterp = Mathf.Min(mapInterp + Time.deltaTime * ANIM_SPEED, 1.0f);
        }

        //Apply transformation
        float a = 1.0f - mapInterp;
        a *= a / (2 * a * (a - 1) + 1);
        //transform.localRotation = camRotation * Quaternion.AngleAxis(a * DEGREES_TILT, Vector3.right);

        //Update transparency
        paperRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(colorID, new Vector4(1.0f, 1.0f, 1.0f, mapInterp * 0.8f));
        paperRenderer.SetPropertyBlock(propBlock);
    }

    public void ToggleMap() {
        //Toggle if map is shown
        mapDest = 1 - mapDest;
        if (mapDest == 1) {
            gameObject.SetActive(true);
            mapCam.SetActive(true);
        }
    }

    public void UpdateRotation(Quaternion camRot, Quaternion viewRot) {
        float smooth_lerp = Mathf.Pow(2.0f, -Time.deltaTime / LAG_HALF_LIFE);
        followRotation = Quaternion.Lerp(followRotation, viewRot, 1.0f - smooth_lerp);
        camRotation = camRot * followRotation;
    }
}
