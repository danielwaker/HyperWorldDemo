using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomFloor : MonoBehaviour {
    private Renderer floorRenderer;
    private MaterialPropertyBlock propBlock;
    private int colorID;
    private Vector4 randColor;
    public bool notFloor;
    public string tileCode;

    private void Start() {
        floorRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colorID = Shader.PropertyToID("_Color");
        string coord;
        if (!notFloor) coord = transform.parent.name.Substring(5);
        else coord = tileCode;
        System.Random rand = new System.Random(coord.GetHashCode());
        Vector3 randVector = new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
        randVector = randVector * 0.5f + Vector3.one * 0.5f;
        randColor = new Vector4(randVector.x, randVector.y, randVector.z, 1.0f);

        floorRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(colorID, randColor);
        floorRenderer.SetPropertyBlock(propBlock);
    }
}
