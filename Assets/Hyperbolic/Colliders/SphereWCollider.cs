using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereWCollider : WCollider {
    //Parameters
    public Vector3 c;
    public float r;
    //Cache
    public Vector3 wc;
    public float wr;

    public SphereWCollider(HyperObject _ho, Vector3 _c, float _r) {
        ho = _ho;
        c = HM.UnitToPoincare(_c);
        r = HM.UnitToPoincareScale(_c, _r);
    }

    public override void UpdateHyperbolic(GyroVector gv) {
        MakeHyperbolic(gv, out wc, out wr);
    }

    public override Vector3 ClosestPoint(Vector3 p) {
        return ClosestPoint(p, wc, wr);
    }

    public override Vector3 ClosestPoint(Vector3 p, GyroVector gv) {
        MakeHyperbolic(gv, out Vector3 _wc, out float _wr);
        return ClosestPoint(p, _wc, _wr);
    }

    private void MakeHyperbolic(GyroVector gv, out Vector3 _wc, out float _wr) {
        //Transform original vertices into hyperbolic ones
        _wc = gv * c;
        _wr = r * HM.PoincareScaleFactor(_wc);
    }

    private static Vector3 ClosestPoint(Vector3 p, Vector3 wc, float wr) {
        return wc + (p - wc).normalized * wr;
    }

    public override void Draw() {
        Gizmos.DrawWireSphere(wc, wr);
    }
}
