using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using Valve.VR;

public class Hand : MonoBehaviour
{
    public SteamVR_Input_Sources handInput;
    public SteamVR_Action_Boolean trigger;
    public SteamVR_Action_Boolean up;
    public SteamVR_Action_Boolean down;
    public SteamVR_Action_Boolean left;
    public SteamVR_Action_Boolean right;
    private HyperObject ho;
    private WarpCollider wc;
    public GameObject map;
    private WorldBuilder wb;

    // Start is called before the first frame update
    void Start()
    {
        trigger.AddOnStateUpListener(Trigger, handInput);
        up.AddOnStateUpListener(Directional, handInput);
        down.AddOnStateUpListener(Directional, handInput);
        left.AddOnStateUpListener(Directional, handInput);
        right.AddOnStateUpListener(Directional, handInput);
        ho = GetComponent<HyperObject>();
        wc = GetComponent<WarpCollider>();
        wb = FindObjectOfType<WorldBuilder>();
    }

    // Update is called once per frame
    void Update()
    {
        float height = GetComponentInParent<Player>().height;
        var pose = PoseDataSource.GetDataFromSource(TrackedPoseDriver.TrackedPose.Center, out Pose resultPose);
        var pose2 = PoseDataSource.GetDataFromSource(TrackedPoseDriver.TrackedPose.RightPose, out Pose resultPoseRight);
        float heightLeft = Mathf.Log10(resultPoseRight.position.y - height) * 0.125f;
        var xLeft = (resultPoseRight.position.x - 0.5f) * 0.1f;
        var zLeft = (resultPoseRight.position.z - 0.5f) * 0.1f;
        //print("height left: " + heightLeft + " actual: " + resultPoseRight.position.y);
        var handBefore = GetComponent<HyperObject>().localGV.vec;
        //print("MAG: " + (resultPoseRight.position - handBefore).magnitude);
        //if ((resultPoseLeft.position - leftHand.GetComponent<HyperObject>().localGV.vec).magnitude > 0.0001f)
        {
            //Vector3 delta = WCollider.Collide(Vector3.zero, wc.boundingSpheres[0].radius, out Vector3 sinY2, false, -ho.localGV);
           // string daname = WCollider.Collide2(Vector3.zero, wc.boundingSpheres[0].radius, out Vector3 sinY3, false, -ho.localGV);

            //print("HAND COLLIDE: " + daname + " " + delta.sqrMagnitude);
            ho.localGV.vec = resultPoseRight.position - resultPose.position;
            ho.localGV.vec *= 0.125f;
            if (map.activeSelf) map.GetComponent<HyperObject>().localGV = ho.localGV;
            //if (delta.sqrMagnitude <= 0.0f)
            {
                //Collision occurred and object needs a push-back equal to delta to avoid collision.

            }

        }
        //print("HANDROTY" + transform.eulerAngles.y);
        /*leftHand.GetComponent<HyperObject>().localGV = new GyroVector(
            Mathf.Cos(Mathf.Deg2Rad * resultPose.rotation.eulerAngles.x) * 0.03f,
            heightLeft,
            Mathf.Sin(Mathf.Deg2Rad * resultPose.rotation.eulerAngles.z) * 0.03f);*/
        //print("HAND: " + ho.localGV);
        //print("HAND DELTA: " + (ho.localGV - handBefore));
    }

    private void Trigger(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        WorldBuilder.Tile[] tiles = wb.SurroundingTiles(-HyperObject.worldGV);
        foreach (WorldBuilder.Tile s in tiles)
        {
            print("SURROUNDING TILE: " + s.coord);
        }

        //SceneManager.LoadScene("Hyper World 1");
        GetComponentInParent<Player>().map.ToggleMap();
    }

    private void Directional(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        string dir = fromAction.fullPath.Split('/').Last();
        WorldBuilder.Tile[] tiles = wb.SurroundingTiles(-HyperObject.worldGV);
        var nearest = wb.NearestTile(-HyperObject.worldGV);

        WorldBuilder.Tile up = default;
        WorldBuilder.Tile down = default;
        WorldBuilder.Tile left = default;
        WorldBuilder.Tile right = default;
        WorldBuilder.Tile wc = default;

        print("DIR NEAREST: " + nearest.coord);
        foreach (WorldBuilder.Tile t in tiles)
        {
            print("DIR " + t.coord + " " + t.gv.vec);
        }
        for (int i = 0; i < 4; i++)
        {
            var t = tiles[i];
            List<WorldBuilder.Tile> temp = tiles.ToList();
            temp.RemoveAt(i);
            if (t.coord == "U" + nearest.coord || t.coord == nearest.coord + "U")
            {
                up = t;
            }
            else if (t.coord == "R" + nearest.coord || t.coord == nearest.coord + "R")
            {
                right = t;
            }
            else if (t.coord == "L" + nearest.coord || t.coord == nearest.coord + "L")
            {
                left = t;
            }
            else if (t.coord == "D" + nearest.coord || t.coord == nearest.coord + "D")
            {
                down = t;
            }
            else if (nearest.coord.Length > 0 && t.coord == nearest.coord.Substring(1) && nearest.coord[0] == 'D')
            {
                up = t;
            }
            else if (nearest.coord.Length > 0 && t.coord == nearest.coord.Substring(1) && nearest.coord[0] == 'U')
            {
                down = t;
            }
            else if (nearest.coord.Length > 0 && t.coord == nearest.coord.Substring(1) && nearest.coord[0] == 'L')
            {
                right = t;
            }
            else if (nearest.coord.Length > 0 && t.coord == nearest.coord.Substring(1) && nearest.coord[0] == 'R')
            {
                left = t;
            }
            else
            {
                wc = t;
            }
        }

        print("DIR HAND " + transform.eulerAngles.y);
        if (transform.eulerAngles.y > 45 && transform.eulerAngles.y < 135)
        {
            print("DIR YOU ARE FACING RIGHT");
            if (dir == "SnapTurnRight")
            {
                if (up.Equals(default(WorldBuilder.Tile))) up = wc;
                HyperObject.worldGV = -up.gv;
            }
            else if (dir == "SnapTurnLeft")
            {
                if (down.Equals(default(WorldBuilder.Tile))) down = wc;
                HyperObject.worldGV = -down.gv;
            }
            else if (dir == "Down")
            {
                if (left.Equals(default(WorldBuilder.Tile))) left = wc;
                HyperObject.worldGV = -left.gv;
            }
            else if (dir == "Up")
            {
                if (right.Equals(default(WorldBuilder.Tile))) right = wc;
                HyperObject.worldGV = -right.gv;
            }
        }
        else if (transform.eulerAngles.y > 135 && transform.eulerAngles.y < 225)
        {
            print("DIR YOU ARE FACING BACK");
            if (dir == "Down")
            {
                if (up.Equals(default(WorldBuilder.Tile))) up = wc;
                HyperObject.worldGV = -up.gv;
            }
            else if (dir == "Up")
            {
                if (down.Equals(default(WorldBuilder.Tile))) down = wc;
                HyperObject.worldGV = -down.gv;
            }
            else if (dir == "SnapTurnRight")
            {
                if (left.Equals(default(WorldBuilder.Tile))) left = wc;
                HyperObject.worldGV = -left.gv;
            }
            else if (dir == "SnapTurnLeft")
            {
                if (right.Equals(default(WorldBuilder.Tile))) right = wc;
                HyperObject.worldGV = -right.gv;
            }
        }
        else if (transform.eulerAngles.y > 225 && transform.eulerAngles.y < 315)
        {
            print("DIR YOU ARE FACING LEFT");
            if (dir == "SnapTurnLeft")
            {
                if (up.Equals(default(WorldBuilder.Tile))) up = wc;
                HyperObject.worldGV = -up.gv;
            }
            else if (dir == "SnapTurnRight")
            {
                if (down.Equals(default(WorldBuilder.Tile))) down = wc;
                HyperObject.worldGV = -down.gv;
            }
            else if (dir == "Up")
            {
                if (left.Equals(default(WorldBuilder.Tile))) left = wc;
                HyperObject.worldGV = -left.gv;
            }
            else if (dir == "Down")
            {
                if (right.Equals(default(WorldBuilder.Tile))) right = wc;
                HyperObject.worldGV = -right.gv;
            }
        }
        else if (transform.eulerAngles.y > 315 || transform.eulerAngles.y < 45)
        {
            print("DIR YOU ARE FACING FORWARD");
            if (dir == "Up")
            {
                if (up.Equals(default(WorldBuilder.Tile))) up = wc;
                HyperObject.worldGV = -up.gv;
            }
            else if (dir == "Down")
            {
                if (down.Equals(default(WorldBuilder.Tile))) down = wc;
                HyperObject.worldGV = -down.gv;
            }
            else if (dir == "SnapTurnLeft")
            {
                if (left.Equals(default(WorldBuilder.Tile))) left = wc;
                HyperObject.worldGV = -left.gv;
            }
            else if (dir == "SnapTurnRight")
            {
                if (right.Equals(default(WorldBuilder.Tile))) right = wc;
                HyperObject.worldGV = -right.gv;
            }
        }
    }

    private void OnDestroy()
    {
        trigger.RemoveOnStateUpListener(Trigger, handInput);
        up.RemoveOnStateUpListener(Directional, handInput);
        down.RemoveOnStateUpListener(Directional, handInput);
        left.RemoveOnStateUpListener(Directional, handInput);
        right.RemoveOnStateUpListener(Directional, handInput);
    }
}
