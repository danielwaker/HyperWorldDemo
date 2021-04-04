using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleWCollider : WCollider {
    //Parameters
    public Vector3 p1, p2, p3;
    //Cache
    public Vector3 a, b, c;

    public TriangleWCollider(HyperObject _ho, Vector3 _p1, Vector3 _p2, Vector3 _p3) {
        ho = _ho;
        p1 = HM.UnitToPoincare(_p1);
        p2 = HM.UnitToPoincare(_p2);
        p3 = HM.UnitToPoincare(_p3);
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out a, out b, out c);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, a, b, c);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _a, out Vector3 _b, out Vector3 _c);
        return ClosestPoint(p, _a, _b, _c);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _a, out Vector3 _b, out Vector3 _c) {
        //Transform original vertices into hyperbolic ones
        Vector3 q1 = gv * p1;
        Vector3 q2 = gv * p2;
        Vector3 q3 = gv * p3;

        //Save into more efficient 'corner' format
        _a = q1 - q2;
        _b = q3 - q2;
        _c = q2;
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b, Vector3 c) {
        Vector3 v = c - p;

        float aa = Vector3.Dot(a, a);
        float ab = Vector3.Dot(a, b);
        float bb = Vector3.Dot(b, b);
        float av = Vector3.Dot(a, v);
        float bv = Vector3.Dot(b, v);

        float det = aa*bb - ab*ab;
        float s   = ab*bv - bb*av;
        float t   = ab*av - aa*bv;

        if (s + t < det) {
            if (s < 0.0f) {
                if (t < 0.0f) {
                    if (av < 0.0f) {
                        s = Mathf.Clamp(-av / aa, 0.0f, 1.0f);
                        t = 0.0f;
                    } else {
                        s = 0.0f;
                        t = Mathf.Clamp(-bv / bb, 0.0f, 1.0f);
                    }
                } else {
                    s = 0.0f;
                    t = Mathf.Clamp(-bv / bb, 0.0f, 1.0f);
                }
            } else if (t < 0.0f) {
                s = Mathf.Clamp(-av / aa, 0.0f, 1.0f);
                t = 0.0f;
            } else {
                float invDet = 1.0f / det;
                s *= invDet;
                t *= invDet;
            }
        } else {
            if (s < 0.0f) {
                float tmp0 = ab + av;
                float tmp1 = bb + bv;
                if (tmp1 > tmp0) {
                    float numer = tmp1 - tmp0;
                    float denom = aa - 2*ab + bb;
                    s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                    t = 1 - s;
                } else {
                    t = Mathf.Clamp(-bv / bb, 0.0f, 1.0f);
                    s = 0.0f;
                }
            } else if (t < 0.0f) {
                if (aa + av > ab + bv) {
                    float numer = bb + bv - ab - av;
                    float denom = aa - 2*ab + bb;
                    s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                    t = 1 - s;
                } else {
                    s = Mathf.Clamp(-bv / bb, 0.0f, 1.0f);
                    t = 0.0f;
                }
            } else {
                float numer = bb + bv - ab - av;
                float denom = aa - 2*ab + bb;
                s = Mathf.Clamp(numer / denom, 0.0f, 1.0f);
                t = 1.0f - s;
            }
        }

        return c + a*s + b*t;
    }

    public override void Draw() {
        Gizmos.DrawLine(c, c + a);
        Gizmos.DrawLine(c, c + b);
        Gizmos.DrawLine(c + a, c + b);
    }
}
