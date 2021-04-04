using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public bool stop = true;
    // Start is called before the first frame update
    void Start()
    {
        //GetComponent<HyperObject>().hyperPos += new Vector3(0.5f, 0f, 0.5f);
        /*for (int i = 0; i < 10; i++)
        {
            //Debug.Log(i);
            //for (int j = 0; j < 10; j++)
            {
                //sphere.SetActive(true);
                var newSphere = Instantiate(sphere);
                newSphere.GetComponent<Transform>().position += new Vector3(0.1f, i * 0.1f, 0);
                newSphere.SetActive(true);
                newSphere.name += i;
                newSphere.GetComponent<HyperObject>().localGV += new GyroVector(i, 0, 0);
                Debug.Log("YOOO " + newSphere.GetComponent<HyperObject>().localGV);
            }
        }*/

        /*GetComponent<HyperObject>().localGV = new GyroVector(0f, 0.25f, -0.5f);
        if (gameObject.name == "CSphere")
            GetComponent<HyperObject>().localGV += new GyroVector(-0.25f, 0f, 0f);
        else
            GetComponent<HyperObject>().localGV += new GyroVector(0.75f, 0f, 0f);
        GetComponent<Transform>().localPosition = Vector3.zero;*/
    }

    // Update is called once per frame
    void LateUpdate()
    {
        var ho = GetComponent<HyperObject>();
        var co = GetComponent<WarpCollider>();
        ho.UpdateCollisions();
        if (co.boundingSpheres.Length > 0)
        {
            var radius = co.boundingSpheres[0].radius;
            Debug.Log("Local GV: " + ho.localGV.vec);
            Vector3 delta = WCollider.Collide(Vector3.zero, radius, out Vector3 sinY, false, -ho.localGV);
            Debug.Log(delta);
            Debug.Log(delta.sqrMagnitude + " " + (delta.sqrMagnitude > 0.0f) + " " + gameObject.name);
            if (gameObject.name != "CSphere" && stop)
                ho.localGV += new GyroVector(0, -0.001f, 0);
            if (delta.sqrMagnitude > Mathf.Pow(0.5f, 3f) * Mathf.Pow(gameObject.transform.localScale.x, 2) && gameObject.name == "CSphere")
                transform.parent.GetComponentInChildren<Test>().stop = false;
        }
        else if (co.boundingBoxes.Length > 0)
        {
            var radius = co.boundingBoxes[0].size.x/2;
            Debug.Log("Local GV: " + ho.localGV.vec);
            Vector3 delta = WCollider.Collide(Vector3.zero, radius, out Vector3 sinY, false, -ho.localGV, gameObject.name);
            Debug.Log(delta);
            Debug.Log(delta.sqrMagnitude + " " + (delta.sqrMagnitude > 0.0f) + " " + gameObject.name);
            if (gameObject.name != "CCube" && stop)
                ho.localGV += new GyroVector(0, -0.001f, 0);
            if (false && delta.sqrMagnitude > 0.1f*Mathf.Pow(gameObject.transform.localScale.x, 2) && gameObject.name == "GCube")
            {
                //ho.localGV += new GyroVector(0, 0.001f, 0);
                transform.parent.GetComponentInChildren<Test>().stop = false;
            }
        }
        ho.UpdateCollisions();
    }
}
