using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MapCamera : MonoBehaviour {
    public bool squareAspectRatio = true;
    public Shader euclidean;
    public Shader spherical;
    public Shader hyperbolic;
    private Camera cam;
    private bool isEuclidean;

	void Start() {
        //Get the camera component and update aspect ratio
        cam = GetComponent<Camera>();
        if (squareAspectRatio) {
            cam.aspect = 1.0f;
        }

        //Replace the shader with a Euclidean camera axis
        if (HM.K > 0.0f) {
            cam.SetReplacementShader(spherical, "HyperRenderType");
        } else if (HM.K < 0.0f) {
            cam.SetReplacementShader(hyperbolic, "HyperRenderType");
        } else {
            cam.SetReplacementShader(euclidean, "HyperRenderType");
            isEuclidean = true;
        }
    }

    void Update() {
        //This equation keeps the camera zoomed and centered well in all projections
        //if (!isEuclidean) 
            cam.orthographicSize = 0.6f + 0.5f * (HyperObject.worldInterp - 1.0f) * (HyperObject.worldInterp - 1.0f);
    }
}
