using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class WarpCollider : MonoBehaviour {
    [System.Serializable]
    public class Box {
        [DraggablePoint] public Vector3 pos;
        public Vector3 rot;
        public Vector3 size;
    }
    [System.Serializable]
    public class Sphere {
        [DraggablePoint] public Vector3 center;
        public float radius;
    }
    [System.Serializable]
    public class Cylinder {
        [DraggablePoint] public Vector3 p1;
        [DraggablePoint] public Vector3 p2;
        public float radius;
        public bool capped = false;
    }
    [System.Serializable]
    public class Triangle {
        [DraggablePoint] public Vector3 a;
        [DraggablePoint] public Vector3 b;
        [DraggablePoint] public Vector3 c;
    }
    [System.Serializable]
    public class Plane {
        [DraggablePoint] public Vector3 p;
    }
    public Box[] boundingBoxes = new Box[0];
    public Sphere[] boundingSpheres = new Sphere[0];
    public Cylinder[] boundingCylinders = new Cylinder[0];
    public Triangle[] boundingTriangles = new Triangle[0];
    public Plane[] boundingPlanes = new Plane[0];

    private Quaternion tr;
    private Vector3 tp;
    private int startColliderIx = 0;
    private int endColliderIx = 0;
    private static int[] triangles = new int[6 * 6] {
        0, 1, 2, 1, 2, 3,
        0, 2, 4, 2, 4, 6,
        0, 4, 1, 4, 1, 5,
        7, 3, 6, 3, 6, 2,
        7, 5, 3, 5, 3, 1,
        7, 6, 5, 6, 5, 4
    };

    public void GenerateColliders() {
        HyperObject ho = GetComponentInParent<HyperObject>();
        Assert.IsNotNull(ho, "WColliders must be children of a HyperObject");
        tr = transform.rotation;
        tp = transform.position;
        startColliderIx = WCollider.AllColliders.Count;
        foreach (Box box in boundingBoxes) {
            Vector3[] verts = MakeVertices(box);
            int numTriVerts = (verts.Length > 4 ? triangles.Length : 6);
            for (int i = 0; i < numTriVerts; i += 3) {
                Vector3 v1 = verts[triangles[i]];
                Vector3 v2 = verts[triangles[i + 1]];
                Vector3 v3 = verts[triangles[i + 2]];
                WCollider.AllColliders.Add(new TriangleWCollider(ho, v1, v2, v3));
            }
        }
        foreach (Sphere sphere in boundingSpheres) {
            WCollider.AllColliders.Add(new SphereWCollider(ho, tp + tr * sphere.center, sphere.radius));
        }
        foreach (Cylinder cylinder in boundingCylinders) {
            WCollider.AllColliders.Add(new CylinderWCollider(ho, tp + tr * cylinder.p1, tp + tr * cylinder.p2, cylinder.radius, cylinder.capped));
        }
        foreach (Triangle triangle in boundingTriangles) {
            WCollider.AllColliders.Add(new TriangleWCollider(ho, tp + tr * triangle.a, tp + tr * triangle.b, tp + tr * triangle.c));
        }
        foreach (Plane plane in boundingPlanes) {
            WCollider.AllColliders.Add(new PlaneWCollider(ho, tp + tr * plane.p));
        }
        endColliderIx = WCollider.AllColliders.Count;
    }

    public void UpdateMesh(GyroVector gv) {
        //Prevent colliders from crossing through infinity in spherical geometry
        if (HM.K > 0.0f && gv.vec.sqrMagnitude >= 2.0) {
            return;
        }

        //Update this object's colliders
        for (int i = startColliderIx; i < endColliderIx; ++i) {
            WCollider.AllColliders[i].active = true;
            WCollider.AllColliders[i].UpdateHyperbolic(gv);
        }
    }

    public void ResetActive() {
        for (int i = startColliderIx; i < endColliderIx; ++i) {
            WCollider.AllColliders[i].active = false;
        }
    }

    public Vector3 Collide(Vector3 p, float r, out Vector3 sinY) {
        return WCollider.Collide(p, r, out sinY, startColliderIx, endColliderIx);
    }

#if UNITY_EDITOR
    public void OnDrawGizmos() {
        tr = transform.rotation;
        tp = transform.position;
        Gizmos.color = Color.green;
        if (UnityEditor.EditorApplication.isPlaying ||
            UnityEditor.EditorApplication.isPaused) {
            //This is being viewed while the game is playing.
            //So draw the warped colliders.
            for (int i = startColliderIx; i < endColliderIx; ++i) {
                WCollider.AllColliders[i].Draw();
            }
        } else {
            //This is being viewed in edit mode.
            //Do not draw the warped colliders.
            foreach (Box box in boundingBoxes) {
                Vector3[] verts = MakeVertices(box);
                int numTriVerts = (verts.Length > 4 ? triangles.Length : 6);
                for (int i = 0; i < numTriVerts; i += 3) {
                    Vector3 v1 = verts[triangles[i]];
                    Vector3 v2 = verts[triangles[i + 1]];
                    Vector3 v3 = verts[triangles[i + 2]];
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v2, v3);
                    Gizmos.DrawLine(v3, v1);
                }
            }
            foreach (Sphere sphere in boundingSpheres) {
                Gizmos.DrawWireSphere(tp + tr*sphere.center, sphere.radius);
            }
            foreach (Cylinder cylinder in boundingCylinders) {
                CylinderWCollider.DrawWireCylinder(tp + tr * cylinder.p1, tp + tr * cylinder.p2, cylinder.radius);
                if (cylinder.capped) {
                    Gizmos.DrawWireSphere(tp + tr * cylinder.p1, cylinder.radius);
                    Gizmos.DrawWireSphere(tp + tr * cylinder.p2, cylinder.radius);
                }
            }
            foreach (Triangle triangle in boundingTriangles) {
                Gizmos.DrawLine(triangle.a, triangle.b);
                Gizmos.DrawLine(triangle.b, triangle.c);
                Gizmos.DrawLine(triangle.c, triangle.a);
            }
            foreach (Plane plane in boundingPlanes) {
                Gizmos.DrawWireSphere(plane.p, 0.05f);
            }
        }
    }
#endif

    private Vector3[] MakeVertices(Box box) {
        //Extract useful properties
        Vector3 c = box.pos;
        Vector3 s = box.size * 0.5f;
        Quaternion r = Quaternion.Euler(box.rot);
        Quaternion tr = transform.rotation;
        Vector3 tp = transform.position;

        //Return all vertices of the box
        if (s.x == 0.0f) {
            return new Vector3[4] {
                tp + tr * (c + r * new Vector3(0.0f,  s.y,  s.z)),
                tp + tr * (c + r * new Vector3(0.0f, -s.y,  s.z)),
                tp + tr * (c + r * new Vector3(0.0f,  s.y, -s.z)),
                tp + tr * (c + r * new Vector3(0.0f, -s.y, -s.z)),
            };
        } else if (s.y == 0.0f) {
            return new Vector3[4] {
                tp + tr * (c + r * new Vector3( s.x, 0.0f,  s.z)),
                tp + tr * (c + r * new Vector3(-s.x, 0.0f,  s.z)),
                tp + tr * (c + r * new Vector3( s.x, 0.0f, -s.z)),
                tp + tr * (c + r * new Vector3(-s.x, 0.0f, -s.z)),
            };
        } else if (s.z == 0.0f) {
            return new Vector3[4] {
                tp + tr * (c + r * new Vector3( s.x,  s.y, 0.0f)),
                tp + tr * (c + r * new Vector3(-s.x,  s.y, 0.0f)),
                tp + tr * (c + r * new Vector3( s.x, -s.y, 0.0f)),
                tp + tr * (c + r * new Vector3(-s.x, -s.y, 0.0f)),
            };
        } else {
            return new Vector3[8] {
                tp + tr * (c + r * new Vector3( s.x,  s.y,  s.z)),
                tp + tr * (c + r * new Vector3(-s.x,  s.y,  s.z)),
                tp + tr * (c + r * new Vector3( s.x, -s.y,  s.z)),
                tp + tr * (c + r * new Vector3(-s.x, -s.y,  s.z)),
                tp + tr * (c + r * new Vector3( s.x,  s.y, -s.z)),
                tp + tr * (c + r * new Vector3(-s.x,  s.y, -s.z)),
                tp + tr * (c + r * new Vector3( s.x, -s.y, -s.z)),
                tp + tr * (c + r * new Vector3(-s.x, -s.y, -s.z))
            };
        }
    }
}
