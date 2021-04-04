using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class HMTest
    {
        //Additional assertions
        static void AssertEqual(Vector3 a, Vector3 b, float tolerance=1e-4f) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
        }
        static void AssertEqual(Vector4 a, Vector4 b, float tolerance = 1e-4f) {
            Assert.AreEqual(a.x, b.x, tolerance);
            Assert.AreEqual(a.y, b.y, tolerance);
            Assert.AreEqual(a.z, b.z, tolerance);
            Assert.AreEqual(a.w, b.w, tolerance);
        }
        static void AssertEqual(Quaternion a, Quaternion b, float tolerance = 1e-4f) {
            float m = ((b.w < 0.0f) == (a.w < 0.0f) ? 1.0f : -1.0f);
            Assert.AreEqual(a.x, m * b.x, tolerance);
            Assert.AreEqual(a.y, m * b.y, tolerance);
            Assert.AreEqual(a.z, m * b.z, tolerance);
            Assert.AreEqual(a.w, m * b.w, tolerance);
        }
        static void AssertEqual(GyroVector a, GyroVector b, float tolerance = 1e-4f) {
            AssertEqual(a.vec, b.vec, tolerance);
            AssertEqual(a.gyr, b.gyr, tolerance);
        }
        static string ToStr(Vector3 x) {
            return "(" + ((double)x.x).ToString("F9") + ", " +
                   ((double)x.y).ToString("F9") + ", " +
                   ((double)x.z).ToString("F9") + ")";
        }
        static string ToStr(Quaternion x) {
            return "[" + ((double)x.x).ToString("F9") + ", " +
                   ((double)x.y).ToString("F9") + ", " +
                   ((double)x.z).ToString("F9") + ", " +
                   ((double)x.w).ToString("F9") + "]";
        }

        [Test]
        public void TestConversions()
        {
            for (int i = 3; i <= 5; i++) {
                for (int j = 0; j < 2; ++j) {
                    HM.SetTileType(i);
                    HM.useTanKHeight = (j == 1);

                    Vector3 u = new Vector3(0.2f, 0.7f, 0.6f);
                    Vector3 n = new Vector3(0.5f, -0.2f, 0.3f);
                    Vector3 k = HM.UnitToKlein(u);
                    Vector3 p = HM.KleinToPoincare(k);

                    //Test inverse conversions
                    AssertEqual(k, HM.PoincareToKlein(p));
                    AssertEqual(u, HM.KleinToUnit(k));

                    //Test combined transforms
                    AssertEqual(p, HM.UnitToPoincare(u));
                    AssertEqual(u, HM.PoincareToUnit(p));

                    //Test vector conversions
                    AssertEqual(n.normalized, HM.KleinToPoincare(k, HM.PoincareToKlein(p, n)));
                }
            }
        }

        [Test]
        public void TestGyroVectorInverse()
        {
            GyroVector a = new GyroVector(new Vector3(0.1f, 0.3f, 0.5f), Quaternion.Euler(20.0f, -40.0f, 30.0f));
            GyroVector b = new GyroVector(new Vector3(-0.5f, 0.2f, -0.4f), Quaternion.Euler(90.0f, -10.0f, -40.0f));
            GyroVector c = new GyroVector(new Vector3(-0.2f, 0.0f, 0.6f), Quaternion.Euler(10.0f, -15.0f, 0.0f));
            Vector3 d = new Vector3(0.3f, -0.3f, 0.2f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(GyroVector.identity, -GyroVector.identity);
                AssertEqual(GyroVector.identity, a + (-a));
                AssertEqual(GyroVector.identity, a - a);
                AssertEqual(a - b, a + (-b));
                AssertEqual(b, (-a) + (a + b));
                AssertEqual(a - d, a + (-d));
                AssertEqual(a, (a + d) - d);
                AssertEqual(a, (a - d) + d);
                AssertEqual(d, ((d + a) - a).vec);
                AssertEqual(d, ((d - a) + a).vec);
                AssertEqual(Quaternion.identity, ((d + a) - a).gyr);
                AssertEqual(Quaternion.identity, ((d - a) + a).gyr);

                //Test associativity
                AssertEqual((a + b) + c, a + (b + c));
            }
        }

        [Test]
        public void TestGyroVectorMultiply() {
            GyroVector g = new GyroVector(new Vector3(0.1f, 0.3f, 0.5f), Quaternion.Euler(20.0f, -40.0f, 30.0f));
            Vector3 a = new Vector3(0.3f, -0.4f, 0.2f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);
                AssertEqual(g * a, (a + g) * Vector3.zero);
                AssertEqual(g * a, (a + g).Point());
            }
        }

        [Test]
        public void TestMobiusOperations()
        {
            Vector3 a = new Vector3(-0.2f, 0.5f, -0.1f);
            Vector3 b = new Vector3(0.1f, 0.2f, 0.3f);
            Vector3 c = new Vector3(0.4f, 0.0f, 0.4f);
            Vector3 x = new Vector3(0.7f, 0.1f, 0.2f);
            Quaternion q = Quaternion.Euler(20.0f, 50.0f, 80.0f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                //Basic Gyration Identities
                AssertEqual(Quaternion.identity, HM.MobiusGyr(Vector3.zero, a));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, Vector3.zero));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, a));
                AssertEqual(Quaternion.identity, HM.MobiusGyr(a, b) * HM.MobiusGyr(b, a));

                //Basic Addition Identities
                AssertEqual(a, HM.MobiusAdd(a, Vector3.zero));
                AssertEqual(a, HM.MobiusAdd(Vector3.zero, a));

                //Rotation invariance
                AssertEqual(q * HM.MobiusAdd(a, b), HM.MobiusAdd(q * a, q * b));
                AssertEqual(q * HM.MobiusGyr(a, b) * Quaternion.Inverse(q), HM.MobiusGyr(q * a, q * b));

                //Combination Identities
                AssertEqual(HM.MobiusAdd(a, b), HM.MobiusGyr(a, b) * HM.MobiusAdd(b, a));
                AssertEqual(HM.MobiusGyr(a, b), HM.MobiusGyr(HM.MobiusAdd(a, b), b));
                AssertEqual(HM.MobiusGyr(a, b), HM.MobiusGyr(a, HM.MobiusAdd(b, a)));

                //Associativity identities
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, x)), HM.MobiusAdd(HM.MobiusAdd(a, b), HM.MobiusGyr(a, b) * x));
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, x)), HM.MobiusGyr(a, b) * HM.MobiusAdd(HM.MobiusAdd(b, a), x));
                AssertEqual(HM.MobiusAdd(HM.MobiusAdd(a, b), x), HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusGyr(b, a) * x)));

                //Multi-Composition
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusAdd(c, x))),
                            HM.MobiusAdd(HM.MobiusAdd(a, HM.MobiusAdd(b, c)), HM.MobiusGyr(a, HM.MobiusAdd(b, c)) * HM.MobiusGyr(b, c) * x));
                AssertEqual(HM.MobiusAdd(a, HM.MobiusAdd(b, HM.MobiusAdd(c, x))),
                            HM.MobiusGyr(a, HM.MobiusAdd(b, c)) * HM.MobiusGyr(b, c) * HM.MobiusAdd(HM.MobiusAdd(c, HM.MobiusAdd(b, a)), x));
            }
        }

        [Test]
        public void TestCombinedAddGyr()
        {
            Vector3 a = new Vector3(-0.2f, 0.5f, -0.1f);
            Vector3 b = new Vector3(0.1f, 0.2f, 0.3f);

            for (int i = 3; i <= 5; i++) {
                HM.SetTileType(i);

                HM.MobiusAddGyr(a, b, out Vector3 sum, out Quaternion gyr);
                AssertEqual(HM.MobiusAdd(a, b), sum);
                AssertEqual(HM.MobiusGyr(b, a), gyr);
            }
        }

        [Test]
        public void TestWalkHyperbolicWalk()
        {
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, HM.CELL_WIDTH);  //Up
            x += new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            x += new Vector3(0.0f, 0.0f, -HM.CELL_WIDTH); //Down
            x += new Vector3(-HM.CELL_WIDTH, 0.0f, 0.0f); //Left
            x += new Vector3(0.0f, 0.0f, HM.CELL_WIDTH);  //Up

            //We should be back where we started but with a 90 degree rotation
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.Euler(0.0f, 90.0f, 0.0f), x.gyr);
        }

        [Test]
        public void TestWalkEuclidean()
        {
            HM.SetTileType(4);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, HM.CELL_WIDTH);  //Up
            AssertEqual(new Vector3(0.0f, 0.0f, HM.CELL_WIDTH), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            AssertEqual(new Vector3(HM.CELL_WIDTH, 0.0f, HM.CELL_WIDTH), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3(0.0f, 0.0f, -HM.CELL_WIDTH); //Down
            AssertEqual(new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f), x.vec);
            AssertEqual(Quaternion.identity, x.gyr);

            x += new Vector3(-HM.CELL_WIDTH, 0.0f, 0.0f); //Left
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.identity, x.gyr);
        }

        [Test]
        public void TestWalkSpherical()
        {
            HM.SetTileType(3);

            //Start at the origin and walk along tiles in the spherical plane
            GyroVector x = GyroVector.identity;
            x += new Vector3(0.0f, 0.0f, HM.CELL_WIDTH);  //Up
            x += new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f);  //Right
            x += new Vector3(0.0f, 0.0f, -HM.CELL_WIDTH); //Down

            //We should be back where we started but with a -90 degree rotation
            AssertEqual(Vector3.zero, x.vec);
            AssertEqual(Quaternion.Euler(0.0f, -90.0f, 0.0f), x.gyr);
        }

        [Test]
        public void TestAccuracy()
        {
            const int ITERS = 5;
            const float DELTA = 0.5f;
            HM.SetTileType(5);

            //Start at the origin and walk along tiles in the hyperbolic plane
            GyroVector x = GyroVector.identity;
            for (int i = 0; i < ITERS; ++i) {
                x += new Vector3(0.0f, 0.0f, DELTA);  //Up
                x += new Vector3(DELTA, 0.0f, 0.0f);  //Right
            }
            for (int i = 0; i < ITERS; ++i) {
                x -= new Vector3(DELTA, 0.0f, 0.0f);  //Left
                x -= new Vector3(0.0f, 0.0f, DELTA);  //Down
            }

            AssertEqual(Vector3.zero, x.vec, 1e-4f);
            AssertEqual(Quaternion.identity, x.gyr, 1e-4f);
        }

        [Test]
        public void TestLimits()
        {
            HM.SetTileType(5);
            GyroVector a = new GyroVector(new Vector3(0.9999f, 0.0f, 0.0f));
            GyroVector b = new GyroVector(new Vector3(0.9998f, 0.0f, 0.0f));
            GyroVector x = a - b - a + b;
            AssertEqual(GyroVector.identity, x, 1e-3f);

            //Make sure identity on Mobius addition maintains EXACT original value.
            GyroVector c = new GyroVector(new Vector3(-0.283646286f, 0.0f, -0.956248403f), new Quaternion(0.0f, 0.973488510f, 0.0f, -0.228755936f));
            AssertEqual(c, c + Vector3.zero, 0.0f);
            AssertEqual(c, Vector3.zero + c, 0.0f);
            AssertEqual(c, c + GyroVector.identity, 0.0f);
            AssertEqual(c, GyroVector.identity + c, 0.0f);
        }

        [Test]
        public void TestProjectToPlane()
        {
            HM.SetTileType(5);
            Vector3 a = new Vector3(0.9f, 0.01f, 0.43f);

            //Project the point to the plane using Klein conversion
            Vector3 k = HM.PoincareToKlein(a);
            k.y = 0.0f;
            Vector3 proj = HM.KleinToPoincare(k);

            //Use the built-in gyrovector converter and compare
            GyroVector gv = new GyroVector(a).ProjectToPlane();
            AssertEqual(proj, gv.Point());
        }
    }
}
