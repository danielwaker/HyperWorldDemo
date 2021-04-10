using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class WCollider {
    public static List<WCollider> AllColliders = new List<WCollider>();
    public static float GROUND_PAD_RATIO = 1.01f;

    //True if this object has a valid cache.
    public bool active = false;
    //The root hyper-object of this collider.
    public HyperObject ho = null;

    public static Vector3 Collide(Vector3 p, float r, out Vector3 sinY, bool useCache = true, GyroVector gv = new GyroVector(), string name = "")
    {
        return Collide(p, r, out sinY, 0, AllColliders.Count, useCache, gv, name);
    }

    public static string Collide2(Vector3 p, float r, out Vector3 sinY, bool useCache = true, GyroVector gv = new GyroVector(), string name = "")
    {
        return Collide2(p, r, out sinY, 0, AllColliders.Count, useCache, gv, name);
    }

    public static Vector3 Collide(Vector3 p, float r, out Vector3 sinY, int ix_from, int ix_to, bool useCache = true, GyroVector gv = new GyroVector(), string name = "") {
        sinY = Vector3.zero;
        Vector3 displacement = Vector3.zero;
        Vector3 delta = Vector3.zero;
        int z;
        float padR2 = r * r * GROUND_PAD_RATIO * GROUND_PAD_RATIO;
        for (int i = ix_from; i < ix_to; ++i) {
            //Profiler.BeginSample("c o l ide");
            WCollider c = AllColliders[i];
            //Debug.Log("daname: " + c.ho.name + i);
            if (c.ho.name != name)
            {
                if (useCache)
                {
                    if (!c.active) { continue; }
                    delta = c.ClosestPoint(p) - p;
                }
                else
                {
                    delta = c.ClosestPoint(p, c.ho.localGV + gv) - p;
                    //Debug.Log("DELTA: " + delta + " LOCAL GV: " + (gv.Equals(new GyroVector(0f, 0f, 0f))));
                }
                float distSq = delta.sqrMagnitude;
                if (distSq < padR2)
                {
                    float deltaMag = Mathf.Sqrt(distSq);
                    float sY = (deltaMag != 0) ? (-delta.y / deltaMag) : 0;
                    sinY.y = Mathf.Max(sinY.y, sY);                   
                    if (deltaMag < r)
                    {
                        displacement = (deltaMag != 0) ? (displacement - delta * ((r - deltaMag) / deltaMag)) : Vector3.zero;
                        p += displacement;
                        sinY.x = Mathf.Max(sinY.x, sY);
                        sinY.z = Mathf.Min(sinY.z, sY);
                    }
                    if (sinY != Vector3.zero)
                        z = 0;
                }
            }
            //Profiler.EndSample();
        }
        if (displacement != Vector3.zero)
            z = 0;
        return displacement;
    }

    public static string Collide2(Vector3 p, float r, out Vector3 sinY, int ix_from, int ix_to, bool useCache = true, GyroVector gv = new GyroVector(), string name = "")
    {
        string daname = "";
        sinY = Vector3.zero;
        Vector3 displacement = Vector3.zero;
        Vector3 delta = Vector3.zero;
        int z;
        float padR2 = r * r * GROUND_PAD_RATIO * GROUND_PAD_RATIO;
        for (int i = ix_from; i < ix_to; ++i)
        {
            WCollider c = AllColliders[i];
            if (c.ho.name != name)
            {
                if (useCache)
                {
                    if (!c.active) { continue; }
                    delta = c.ClosestPoint(p) - p;
                }
                else
                {
                    delta = c.ClosestPoint(p, c.ho.localGV + gv) - p;
                    //Debug.Log("DELTA: " + delta + " LOCAL GV: " + (gv.Equals(new GyroVector(0f, 0f, 0f))));
                }
                float distSq = delta.sqrMagnitude;
                if (distSq < padR2)
                {
                    float deltaMag = Mathf.Sqrt(distSq);
                    float sY = (deltaMag != 0) ? (-delta.y / deltaMag) : 0;
                    sinY.y = Mathf.Max(sinY.y, sY);
                    if (deltaMag < r)
                    {
                        displacement = (deltaMag != 0) ? (displacement - delta * ((r - deltaMag) / deltaMag)) : Vector3.zero;
                        daname = c.ho.name;
                        p += displacement;
                        sinY.x = Mathf.Max(sinY.x, sY);
                        sinY.z = Mathf.Min(sinY.z, sY);
                    }
                    if (sinY != Vector3.zero)
                        z = 0;
                }
            }
        }
        if (displacement != Vector3.zero)
            z = 0;
        return daname;
    }

    public abstract void UpdateHyperbolic(GyroVector gv);
    public abstract Vector3 ClosestPoint(Vector3 p);
    public abstract Vector3 ClosestPoint(Vector3 p, GyroVector gv);
    public abstract void Draw();
}
