using System;
using UnityEngine;
using UnityEngine.Assertions;

//HyperMath abbreviated for easier usage
public class HM {
    //The hyperbolic width of a tile
    public static float CELL_WIDTH = 0.0f;
    //The location of each vertex in Klein coordinates
    public static float KLEIN_V = 0.0f;
    //Curvature class (-1=Hyperbolic, 0=Euclidean, 1=Spherical)
    public static float K = 0.0f;
    //Number of square tiles that connect at each vertex
    public static int N = 4;

    //If true, stretch the mesh height to make it easier to deal with
    public static bool useTanKHeight = true;

    public static void SetTileType(int n) {
        //Do calculations in double precision because this only needs to be called once
        //and it is very important that these number be as accurate as possible.
        //The tiny epsilon is added at the end to hide small gaps between tiles.
        N = n;
        if (n == 4) {
            K = 0.0f;
            KLEIN_V = 0.5f;
            CELL_WIDTH = 0.5f;
        } else {
            K = (n < 4 ? 1.0f : -1.0f);
            double a = Math.PI / 4;
            double b = Math.PI / n;
            double c = Math.Cos(a) * Math.Cos(b) / (Math.Sin(a) * Math.Sin(b));
            double s = Math.Sqrt(0.5 * Math.Abs(c - 1.0) / (c + 1.0));
            double r = Math.Cos(b) / Math.Sin(a);
            KLEIN_V = (float)(s / (0.5 - K * s * s) + (3e-4 / n));
            CELL_WIDTH = (float)(Math.Sqrt(Math.Abs(r * r - 1.0)) / r - 1e-5);
        }
    }

    //Inverse hyperbolic trig functions (.NET doesn't provide these for some reason) 
    public static double Acosh(double x) {
        return Math.Log(x + Math.Sqrt(x*x - 1));
    }
    public static double Atanh(double x) {
        return 0.5 * Math.Log((1.0 + x) / (1.0 - x));
    }

    //Curvature-dependent tangent
    public static float TanK(float x) {
        if (K > 0.0f) {
            return Mathf.Tan(x);
        } else if (K < 0.0f) {
            return (float)Math.Tanh(x);
        } else {
            return x;
        }
    }
    //Curvature-dependent inverse tangent
    public static float AtanK(float x) {
        if (K > 0.0f) {
            return Mathf.Atan(x);
        } else if (K < 0.0f) {
            return 0.5f * Mathf.Log((1.0f + x)/(1.0f - x));
        } else {
            return x;
        }
    }

    //3D Möbius addition (non-commutative, non-associative)
    //NOTE: This is much more numerically stable than the one in Ungar's paper.
    public static Vector3 MobiusAdd(Vector3 a, Vector3 b) {
        Vector3 c = K * Vector3.Cross(a, b);
        float d = 1.0f - K * Vector3.Dot(a, b);
        Vector3 t = a + b;
        return (t*d + Vector3.Cross(c, t)) / (d*d + c.sqrMagnitude);
    }

    //3D Möbius gyration
    public static Quaternion MobiusGyr(Vector3 a, Vector3 b) {
        //We're actually doing this operation:
        //  Quaternion.AngleAxis(180.0f, MobiusAdd(a, b)) * Quaternion.AngleAxis(180.0f, a + b);
        //But the precision is better (and faster) by doing the way below:
        Vector3 c = K*Vector3.Cross(a, b);
        float d = 1.0f - K*Vector3.Dot(a, b);
        Quaternion q = new Quaternion(c.x, c.y, c.z, d);
        q.Normalize();
        return q;
    }

    //Optimization to combine Möbius addition and gyration operations.
    //Equivalent to sum = MobiusAdd(a,b); gyr = MobiusGyr(b,a);
    public static void MobiusAddGyr(Vector3 a, Vector3 b, out Vector3 sum, out Quaternion gyr) {
        Vector3 c = K * Vector3.Cross(a, b);
        float d = 1.0f - K * Vector3.Dot(a, b);
        Vector3 t = a + b;
        sum = (t * d + Vector3.Cross(c, t)) / (d * d + c.sqrMagnitude);
        gyr = new Quaternion(c.x, c.y, c.z, -d);
        gyr.Normalize();
    }

    //Point conversion between Klein and Poincaré
    public static Vector3 KleinToPoincare(Vector3 p) {
        return p / (Mathf.Sqrt(1.0f + K * p.sqrMagnitude) + 1.0f);
    }
    public static Vector3 PoincareToKlein(Vector3 p) {
        return p * 2.0f / (1.0f - K * p.sqrMagnitude);
    }
    //Plane normal conversion between Klein and Poincaré
    public static Vector3 KleinToPoincare(Vector3 p, Vector3 n) {
        return ((1.0f + Mathf.Sqrt(1.0f + K * p.sqrMagnitude)) * n + (K * Vector3.Dot(n, p)) * p).normalized;
    }
    public static Vector3 PoincareToKlein(Vector3 p, Vector3 n) {
        return ((1.0f + K * p.sqrMagnitude) * n - (2.0f * K * Vector3.Dot(n, p)) * p).normalized;
    }

    //Conversions between Unit tile coordinates and Klein coordinates
    public static Vector3 UnitToKlein(Vector3 p) {
        p *= KLEIN_V;
        if (useTanKHeight) {
            p.y = TanK(p.y) * Mathf.Sqrt(1.0f + K * (p.x * p.x + p.z * p.z));
        }
        return p;
    }
    public static Vector3 KleinToUnit(Vector3 p) {
        if (useTanKHeight) {
            p.y = AtanK(p.y / Mathf.Sqrt(1.0f + K * (p.x * p.x + p.z * p.z)));
        }
        return p / KLEIN_V;
    }

    //Composite conversions
    public static Vector3 UnitToPoincare(Vector3 u) {
        return KleinToPoincare(UnitToKlein(u));
    }
    public static Vector3 PoincareToUnit(Vector3 u) {
        return KleinToUnit(PoincareToKlein(u));
    }

    //Other conversions
    public static float UnitToPoincareScale(Vector3 u, float r) {
        u = UnitToKlein(u);
        float p = Mathf.Sqrt(1.0f + K * u.sqrMagnitude);
        return r * KLEIN_V / (p * (p + 1));
    }
    public static float PoincareScaleFactor(Vector3 p) {
        return 1.0f + K * p.sqrMagnitude;
    }

    //Apply a translation to the a hyper-rotation
    public static Vector3 HyperTranslate(float dx, float dz) {
        return HyperTranslate(new Vector3(dx, 0.0f, dz));
    }
    public static Vector3 HyperTranslate(float dx, float dy, float dz) {
        return HyperTranslate(new Vector3(dx, dy, dz));
    }
    public static Vector3 HyperTranslate(Vector3 d) {
        float mag = d.magnitude;
        if (mag < 1e-5f) {
            return Vector3.zero;
        }
        return d * (TanK(mag) / mag);
    }

    //Returns the up-vector at a given point
    public static Vector3 UpVector(Vector3 p) {
        float u = 1.0f + K * p.sqrMagnitude;
        float v = -2.0f * K * p.y;
        return (u * Vector3.up + v * p).normalized;
    }
    //Swing-Twist decomposition of a quaternion
    public static Quaternion SwingTwist(Quaternion q, Vector3 d) {
        Vector3 ra = new Vector3(q.x, q.y, q.z);
        Vector3 p = Vector3.Project(ra, d);
        return (new Quaternion(p.x, p.y, p.z, q.w)).normalized;
    }

    public static GyroVector CoordToHyper(string s) {
        GyroVector gv = GyroVector.identity;
        for (int i = 0; i < s.Length; ++i) {
            switch(s[i]) {
                case 'R':
                    gv += new Vector3(CELL_WIDTH, 0.0f, 0.0f);
                    break;
                case 'L':
                    gv += new Vector3(-CELL_WIDTH, 0.0f, 0.0f);
                    break;
                case 'U':
                    gv += new Vector3(0.0f, 0.0f, CELL_WIDTH);
                    break;
                case 'D':
                    gv += new Vector3(0.0f, 0.0f, -CELL_WIDTH);
                    break;
                default:
                    break;
            }
        }
        return gv;
    }
}

//Data structure to hold a full Möbius transform
public struct GyroVector
{
    //Identity element
    public static readonly GyroVector identity = new GyroVector(Vector3.zero);

    //Members
    public Vector3 vec;     //This is the hyperbolic offset vector or position
    public Quaternion gyr;  //This is the post-rotation as a result of holonomy

    //Constructors
    public GyroVector(float x, float y, float z) { vec = new Vector3(x,y,z); gyr = Quaternion.identity; }
    public GyroVector(Vector3 _vec) { vec = _vec; gyr = Quaternion.identity; }
    public GyroVector(Vector3 _vec, Quaternion _gyr) { vec = _vec; gyr = _gyr.normalized; }

    //Compose the GyroVector with a Möbius Translation
    public static GyroVector operator+(GyroVector gv, Vector3 delta) {
        HM.MobiusAddGyr(gv.vec, Quaternion.Inverse(gv.gyr) * delta, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv.gyr * newGyr);
    }
    public static GyroVector operator+(Vector3 delta, GyroVector gv) {
        HM.MobiusAddGyr(delta, gv.vec, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv.gyr * newGyr);
    }
    public static GyroVector operator+(GyroVector gv1, GyroVector gv2) {
        HM.MobiusAddGyr(gv1.vec, Quaternion.Inverse(gv1.gyr) * gv2.vec, out Vector3 newVec, out Quaternion newGyr);
        return new GyroVector(newVec, gv2.gyr * gv1.gyr * newGyr);
    }

    //Inverse GyroVector
    public static GyroVector operator-(GyroVector gv) {
        return new GyroVector(-(gv.gyr * gv.vec), Quaternion.Inverse(gv.gyr));
    }

    //Inverse composition
    public static GyroVector operator-(GyroVector gv, Vector3 delta) {
        return gv + (-delta);
    }
    public static GyroVector operator-(Vector3 delta, GyroVector gv) {
        return delta + (-gv);
    }
    public static GyroVector operator-(GyroVector gv1, GyroVector gv2) {
        return gv1 + (-gv2);
    }

    //Apply the full GyroVector to a point
    public static Vector3 operator*(GyroVector gv, Vector3 pt) {
        return gv.gyr * HM.MobiusAdd(gv.vec, pt);
    }
    public Vector3 Point() {
        return gyr * vec;
    }
    //Apply just the Möbius addition to a point
    public Vector3 MobiusAdd(Vector3 pt) {
        return HM.MobiusAdd(vec, pt);
    }

    //Aligns the rotation of the gyrovector so that up is up
    public void AlignUpVector() {
        //TODO: I'm sure all this math could simplify eventually...
        Vector3 newAxis = HM.UpVector(vec);
        Quaternion newBasis = Quaternion.FromToRotation(newAxis, Vector3.up);
        Quaternion twist = HM.SwingTwist(gyr, newAxis);
        gyr = newBasis * twist;
    }

    //Projects the Gyrovector to the ground plane
    public GyroVector ProjectToPlane() {
        //Remove the y-component form the Klein projection
        float m = HM.K * vec.sqrMagnitude;
        float d = 1.0f + m;
        float s = 2.0f / (1.0f - m + Mathf.Sqrt(d * d - 4.0f * HM.K * vec.y * vec.y));
        Vector3 newVec = new Vector3(vec.x * s, 0.0f, vec.z * s);
        //Remove any out-of-plane rotation (normalization happens in constructor)
        return new GyroVector(newVec, new Quaternion(0.0f, gyr.y, 0.0f, gyr.w));
    }

    //Convert to a matrix so the shader can read it
    public Matrix4x4 ToMatrix() {
        return Matrix4x4.TRS(vec, gyr, Vector3.one);
    }

    //Human readable form
    public override string ToString() {
        return "(" + ((double)vec.x).ToString("F9") + ", " +
               ((double)vec.y).ToString("F9") + ", " +
               ((double)vec.z).ToString("F9") + ") [" +
               ((double)gyr.x).ToString("F9") + ", " +
               ((double)gyr.y).ToString("F9") + ", " +
               ((double)gyr.z).ToString("F9") + ", " +
               ((double)gyr.w).ToString("F9") + "]";
    }
}
