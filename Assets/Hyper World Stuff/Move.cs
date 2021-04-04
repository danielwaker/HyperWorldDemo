using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private float ranX;
    private float ranZ;

    // Start is called before the first frame update
    void Start()
    {
        ranX = Random.Range(-1f, 1f)*0.001f;
        ranZ = Random.Range(-1f, 1f)*0.001f;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        var ho = GetComponent<HyperObject>();
        //Debug.Log(ho.composedGV + gameObject.name);
        ho.localGV += new GyroVector(ranX, 0, ranZ);
    }
}
