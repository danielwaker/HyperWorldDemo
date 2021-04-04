using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var ho = GetComponent<HyperObject>();
        Debug.Log("Local GV: " + ho.localGV.vec);
        Vector3 delta = WCollider.Collide(Vector3.zero, 0f, out Vector3 sinY, false, -ho.localGV);
        Debug.Log(delta);
        Debug.Log(delta.sqrMagnitude + " " + (delta.sqrMagnitude > 0.0f) + " " + gameObject.name);
    }
}
