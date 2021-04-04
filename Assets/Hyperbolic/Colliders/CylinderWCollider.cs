using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CylinderWCollider : WCollider {
    //Parameters
    public Vector3 a, b;
    public float r;
    public bool capped;
    //Cache
    public Vector3 wc, wd;
    public float wr, wh;

    public CylinderWCollider(HyperObject _ho, Vector3 _a, Vector3 _b, float _r, bool _capped) {
        ho = _ho;
        a = HM.UnitToPoincare(_a);
        b = HM.UnitToPoincare(_b);
        r = HM.UnitToPoincareScale((_a + _b)*0.5f, _r);
        capped = _capped;
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out wc, out wd, out wr, out wh);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, wc, wd, wr, wh, capped);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _wc, out Vector3 _wd, out float _wr, out float _wh);
        return ClosestPoint(p, _wc, _wd, _wr, _wh, capped);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _wc, out Vector3 _wd, out float _wr, out float _wh) {
        //Transform original vertices into hyperbolic ones
        Vector3 wa = gv * a;
        Vector3 wb = gv * b;
        _wc = (wa + wb) * 0.5f;
        _wd = (wb - wa) * 0.5f;
        _wr = r * HM.PoincareScaleFactor(_wc);

        //Use unit height for better optimization
        _wh = _wd.magnitude;
        _wd /= _wh;
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 wc, Vector3 wd, float wr, float wh, bool capped) {
        //Find unit distance along line
        Vector3 pc = p - wc;
        float lp = Vector3.Dot(pc, wd);
        //Handle easier capped case
        if (capped) {
            lp = Mathf.Clamp(lp, -wh, wh);
        }
        //Project point to cylinder line
        pc -= lp * wd;
        //If inside the cylinder, closest point is on the line
        if (lp >= -wh && lp <= wh) {
            return wc + lp * wd + pc.normalized * wr;
        }
        //Clamp the line projection now
        lp = Mathf.Clamp(lp, -wh, wh);
        //Find distance squared from line
        float r2 = pc.sqrMagnitude;
        //Return closest point on disk
        if (r2 <= wr * wr) {
            return wc + lp * wd + pc;
        } else {
            pc *= wr / Mathf.Sqrt(r2);
            return wc + lp * wd + pc;
        }
    }

    public override void Draw() {
        DrawWireCylinder(wc + wd * wh, wc - wd * wh, wr);
    }

    public static void DrawWireCylinder(Vector3 pos, Vector3 pos2, float radius) {
#if UNITY_EDITOR
        Vector3 forward = pos2 - pos;
        if (float.IsNaN(forward.sqrMagnitude) || forward.sqrMagnitude < 1e-8) { return; }
        Quaternion rot = Quaternion.LookRotation(forward);
        float length = forward.magnitude;
        Matrix4x4 angleMatrix = Matrix4x4.TRS(pos, rot, Handles.matrix.lossyScale);
        Handles.color = Gizmos.color;
        using (new Handles.DrawingScope(angleMatrix)) {
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
            Handles.DrawWireDisc(new Vector3(0f, 0f, length), Vector3.forward, radius);
            Handles.DrawLine(new Vector3(radius, 0f, 0f), new Vector3(radius, 0f, length));
            Handles.DrawLine(new Vector3(-radius, 0f, 0f), new Vector3(-radius, 0f, length));
            Handles.DrawLine(new Vector3(0f, radius, 0f), new Vector3(0f, radius, length));
            Handles.DrawLine(new Vector3(0f, -radius, 0f), new Vector3(0f, -radius, length));
        }
#endif
    }
}
