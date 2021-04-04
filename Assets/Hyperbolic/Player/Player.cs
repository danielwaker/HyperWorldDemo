using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SpatialTracking;
using Valve.VR;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {
    public const float HEAD_BOB_FREQ = 12.0f;
    public const float HEAD_BOB_MAX = 0.008f;
    public const float CAP_RADIUS = 0.4f; //Relative to height
    public const float GRAVITY = -4.0f; //Relative to height
    public const float JUMP_SPEED = 2.2f; //Relative to height
    public const float LAG_LOOK_X = 0.05f;
    public const float LAG_LOOK_Y = 0.05f;
    public const float LAG_MOVE_X = 0.05f;
    public const float LAG_MOVE_Y = 0.05f;
    public const float MIN_WALK_SLOPE = 0.8f; //sin of the angle between normal and horizontal

    public float LOCKED_MAX_Y = 10.0f; //Degrees
    public float LOCKED_MAX_X = 10.0f; //Degrees
    public float sensitivityX = 60.0f;
    public float sensitivityY = 60.0f;
    public float height = 0.1f;
    public float walkingSpeed = 2.0f;
    public Vector3 startHyper = new Vector3(0, 0, 0);
    public Map map;
    public GameObject mapCam;
    public GameObject mainCamera;
    public bool VR;
    private Vector3 VR_delta;
    private Vector3 previousVR_position = new Vector3(0,0,0);
    //private SteamVR_Action_Vector3 uhhh;

    public GameObject[] preCollisionCallbacks;

    public float rotationX { get; private set; } //Degrees
    public float rotationY { get; private set; } //Degrees
    private float lockedRotationX; //Degrees
    private float lockedRotationY; //Degrees
    private float smoothRotationX; //Degrees
    private float smoothRotationY; //Degrees
    private float smoothDX;
    private float smoothDY;
    private float headBobState;
    private float timeSinceGrounded;
    [HideInInspector] public float velocityY;
    private Quaternion camRotation;
    private GameObject lockedNPC;
    private HyperObject lockedHO;
    private Vector3 forwardBias;
    private bool isLocked;
    private float projCur;
    private float projTransition;
    private float projNew;
    [HideInInspector] public Vector3 inputDelta;
    [HideInInspector] public Vector3 outputDelta;
    private AsyncOperation sceneUp;
    private AsyncOperation sceneDown;

#if UNITY_EDITOR
    private static string placeNPCText; //Debug
#endif

    private void Awake() {
        //Initialize world position and rotation
        HyperObject.worldGV = -new GyroVector(startHyper);
        camRotation = mainCamera.transform.localRotation;
        Debug.Assert(transform.localScale == Vector3.one);
    }

    void Start() {
        //Initialize identity rotations
        smoothRotationX = rotationX = 0.0f;
        smoothRotationY = rotationY = 0.0f;
        smoothDX = 0.0f;
        smoothDY = 0.0f;

        //Other initialization
        headBobState = 0.0f;
        velocityY = 0.0f;
        projNew = projCur;
        projTransition = 0.0f;
        timeSinceGrounded = 100.0f;
        lockedNPC = null;

        /*if (SceneManager.GetActiveScene().buildIndex < SceneManager.sceneCountInBuildSettings - 1)
        {
            sceneUp = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
            sceneUp.allowSceneActivation = false;
        }
        if (SceneManager.GetActiveScene().buildIndex > 0)
        {
            sceneDown = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex - 1);
            sceneDown.allowSceneActivation = false;
        }*/
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.grey;
        Gizmos.DrawSphere(BodyCollisionPoint(), CollisionRadius());
        Gizmos.DrawSphere(HeadCollisionPoint(), CollisionRadius());
    }

    public Vector3 GetForwardBias() {
        return forwardBias;
    }

    public Vector3 HeadCollisionPoint() {
        return new Vector3(0.0f, height, 0.0f);
    }

    public Vector3 BodyCollisionPoint() {
        return new Vector3(0.0f, CollisionRadius(), 0.0f);
    }

    public float CollisionRadius() {
        return height * CAP_RADIUS;
    }

    void Update() {
        Debug.Log(HyperObject.worldGV + "Player");
        //Update projection
        if (projTransition > 0.0f && projCur != projNew) {
            projTransition = Mathf.Max(projTransition - Time.deltaTime, 0.0f);
            float a = Mathf.SmoothStep(0.0f, 1.0f, projTransition);
            HyperObject.worldInterp = projCur*a + projNew*(1-a);
        } else {
            projCur = projNew;
            HyperObject.worldInterp = projCur;
            if (InputManager.GetKeyDown(GameKey.PROJECTION_CHANGE)) {
                projTransition = 1.0f;
                projNew += 1.0f;
                if (projNew >= 2.0f) {
                    projNew = -1.0f;
                }
            }
        }

        if (IsLocked()) {
            if (!VR)
            {
                //Update raw rotations
                float lookVert = InputManager.GetAxis(GameAxis.LOOK_VERTICAL);
                float lookHoriz = InputManager.GetAxis(GameAxis.LOOK_HORIZONTAL);
                lockedRotationY += lookVert * sensitivityY * 0.02f;
                lockedRotationX += lookHoriz * sensitivityX * 0.02f;

                //Clamp raw rotations to valid ranges
                lockedRotationY = Mathf.Clamp(lockedRotationY, -LOCKED_MAX_Y, LOCKED_MAX_Y);
                lockedRotationX = Mathf.Clamp(lockedRotationX, -LOCKED_MAX_X, LOCKED_MAX_X);
            }

            //Handle locked-on NPCs
            if (lockedNPC) {
                //Look at the locked-on NPC if applicable
                GyroVector otherGV = lockedHO.composedGV;
                Vector3 trueDelta = otherGV * HM.UnitToPoincare(lockedNPC.transform.position);
                rotationX = Mathf.Atan2(trueDelta.x, trueDelta.z) * Mathf.Rad2Deg;
                rotationY = -10.0f;

                //Let the NPC know that they should look at the player
                lockedNPC.SendMessage("LookAtPlayer", this);

                //Remove all velocity to prevent jolt after event finishes.
                smoothDX = 0.0f;
                smoothDY = 0.0f;
            }
        } else {
            //Check if map needs to be toggled
            if (map && InputManager.GetKeyDown(GameKey.MAP_TOGGLE)) {
                map.ToggleMap();
            }

            if (!VR)
            {
                //Update if there was any locked rotation
                rotationX += lockedRotationX;
                rotationY += lockedRotationY;
                lockedRotationX = 0.0f;
                lockedRotationY = 0.0f;

                //Update raw rotations
                float lookVert = InputManager.GetAxis(GameAxis.LOOK_VERTICAL);
                float lookHoriz = InputManager.GetAxis(GameAxis.LOOK_HORIZONTAL);
                rotationY += lookVert * sensitivityY * 0.02f;
                rotationX += lookHoriz * sensitivityX * 0.02f;
            }           
        }

        if (!VR)
        {
            //Clamp raw rotations to valid ranges
            //rotationY = Mathf.Clamp(rotationY, -80.0f, 80.0f);
            rotationY = Mathf.Clamp(rotationY, -90.0f, 90.0f);
            while (rotationX > 180.0f) { rotationX -= 360.0f; }
            while (rotationX < -180.0f) { rotationX += 360.0f; }

            //Apply smoothing (time dependent)
            ModPi(ref smoothRotationX, rotationX + lockedRotationX);
            float smooth_x = Mathf.Pow(2.0f, -Time.deltaTime / LAG_LOOK_X);
            float smooth_y = Mathf.Pow(2.0f, -Time.deltaTime / LAG_LOOK_Y);
            smoothRotationX = smoothRotationX * smooth_x + (rotationX + lockedRotationX) * (1 - smooth_x);
            smoothRotationY = smoothRotationY * smooth_y + (rotationY + lockedRotationY) * (1 - smooth_y);
        }

        //Get the rotation you will be at next as a Quaternion
        Quaternion yQuaternion = (!VR) ? mainCamera.transform.rotation : Quaternion.AngleAxis(smoothRotationY, Vector3.left);
        Quaternion xQuaternion = (!VR) ? mainCamera.transform.rotation : Quaternion.AngleAxis(smoothRotationX, Vector3.up);

        //Rotate the main camera
        Quaternion xRot = (!VR) ? camRotation : xQuaternion;
        mainCamera.transform.localRotation = xRot * yQuaternion;

        //DEBUG HACK:
#if UNITY_EDITOR
        if (InputManager.GetKeyDown(GameKey.DEBUG1)) {
            float ang = (xQuaternion * Quaternion.Inverse(HyperObject.worldGV.gyr)).eulerAngles.y;
            Vector3 v = -HyperObject.worldGV.vec;
            Debug.Log(ang + " [" + v.x + " " + v.y + " " + v.z + "]");

            const string TEXT_ASSET = "Assets/Editor/PlaceNPC.txt";
            placeNPCText += ang + " " + v.x + " " + v.y + " " + v.z + "\n";
            File.WriteAllText(TEXT_ASSET, placeNPCText);
        }
        if (InputManager.GetKeyDown(GameKey.DEBUG2)) {
            float ang = (xQuaternion * Quaternion.Inverse(HyperObject.worldGV.gyr)).eulerAngles.y;
            Vector3 v = -HyperObject.worldGV.vec;
            WorldBuilder.Tile nearest = FindObjectOfType<WorldBuilder>().NearestTile(-HyperObject.worldGV);
            Debug.Log("tile: " + nearest.coord + " " + nearest.tileName + " " + ang + " [" + v.x + " " + v.y + " " + v.z + "]");
        }
#endif
        Vector3 displacement = Vector3.zero;
        if (!IsLocked()) {
            //Update how long the player has been on the ground
            timeSinceGrounded += Time.deltaTime;

            //Calculate new velocity after gravity
            velocityY += GRAVITY * height * Time.deltaTime;

            float dx;
            float dy;
            if (!VR)
            {
                //Get keyboard or joystick input
                dx = InputManager.GetAxis(GameAxis.MOVE_SIDEWAYS);
                dy = InputManager.GetAxis(GameAxis.MOVE_FORWARDS);
            }
            else
            {
                var pose = PoseDataSource.GetDataFromSource(TrackedPoseDriver.TrackedPose.Center, out Pose resultPose);
                height = (resultPose.position.y-0.75f)*0.125f;
                print("CAM: " + resultPose.rotation.eulerAngles);
                print("NEAREST: " + FindObjectOfType<WorldBuilder>().NearestTile(-HyperObject.worldGV).coord);

                print("height" + height);
                VR_delta = resultPose.position - previousVR_position;
                if (Mathf.Abs(VR_delta.x) > 0.001f)
                {
                    dx = (VR_delta.x > 0) ? 1 : -1;
                    //dx *= VR_delta.x;
                }
                else
                {
                    dx = 0;
                }
                if (Mathf.Abs(VR_delta.z) > 0.001f)
                {
                    dy = (VR_delta.z > 0) ? 1 : -1;
                    //dy *= VR_delta.y * 100;
                }
                else
                {
                    dy = 0;
                }
                print("VR X: " + VR_delta.x + " VR Y: " + VR_delta.y);
                print("VR POS: " + resultPose.position);
                print("VR DELTA: " + VR_delta);
                previousVR_position = resultPose.position;
            }
            print("dx: " + dx + " dy: " + dy);

            float smooth_dx = Mathf.Pow(2.0f, -Time.deltaTime / LAG_MOVE_X);
            float smooth_dy = Mathf.Pow(2.0f, -Time.deltaTime / LAG_MOVE_Y);
            smoothDX = smoothDX * smooth_dx + dx * (1 - smooth_dx);
            smoothDY = smoothDY * smooth_dy + dy * (1 - smooth_dy);
            if (VR)
            {
                //if (dx != 0) smoothDX *= dx * (1 + Mathf.Abs(VR_delta.x * 100));
                //if (dy != 0) smoothDY *= dy * (1 + Mathf.Abs(VR_delta.y * 100));
            }
            print("sm X: " + smoothDX + " sm Y: " + smoothDY);

            inputDelta = Vector3.ClampMagnitude(new Vector3(smoothDX, 0.0f, smoothDY), 1.0f);
            inputDelta = HM.HyperTranslate(xRot * inputDelta * (walkingSpeed * height * Time.deltaTime));

            //testy.addonstate;

            //Allow other objects to resolve their own collisions first and/or
            //possibly alter player's input velocity.
            if (preCollisionCallbacks != null) {
                for (int i = 0; i < preCollisionCallbacks.Length; ++i) {
                    preCollisionCallbacks[i].SendMessage("OnPreCollide", this);
                }
            }

            //Do collisions manually since dynamic meshes don't behave well with Unity physics.
            displacement = IteratedCollide(inputDelta, CollisionRadius(), out Vector3 sinY, 2);

            //Check if the player is grounded
            bool isGrounded = (HyperObject.worldGV.vec.y >= 0.0f || sinY.y >= MIN_WALK_SLOPE);
            if (isGrounded) {
                timeSinceGrounded = 0.0f;
            }
            bool isRecentGrounded = (timeSinceGrounded < 0.1f);

            //Cancel y velocity if on ground
            if (velocityY < 0.0f && isGrounded) {
                velocityY = 0.0f;
            }

            //Jump if allowable
            if (velocityY <= 0.0f && isRecentGrounded && InputManager.GetKeyDown(GameKey.JUMP) && !IsLocked()) {
                velocityY = JUMP_SPEED * height;
            }

            //Cancel y velocity if hitting head
            if (sinY.z < -0.1f && velocityY > 0.0f) {
                velocityY = 0.0f;
            }

            //Do not allow upward pushes if the collision is not from a walkable surface (prevents oscillation)
            if (displacement.y > 0.0f && sinY.x < MIN_WALK_SLOPE) {
                displacement.y = 0.0f;
            }

            //Get the velocity vector
            Vector3 velocityDelta = Vector3.up * HM.TanK(velocityY * Time.deltaTime);

            //Map that world displacement to a hyperbolic one
            outputDelta = displacement + velocityDelta;
            HyperObject.worldGV -= outputDelta;
            HyperObject.worldGV.vec.y = Mathf.Min(HyperObject.worldGV.vec.y, 0.0f);
            HyperObject.worldGV.AlignUpVector();

            print("WORLD: " + HyperObject.worldGV);
            /*if (InputManager.GetKeyDown(GameKey.DEBUG2))
            {
                string[] tiles = FindObjectOfType<WorldBuilder>().SurroundingTiles(HyperObject.worldGV);
                foreach (string s in tiles)
                {
                    print("SURROUNDING TILE: " + s);
                }
            }*/

            /*foreach(SteamVR_Input_Sources s in SteamVR_Input_Source.GetAllSources())
            {
                print("SOURCES: " + SteamVR_Input_Source.GetSource());
            }*/

            //if (InputManager.GetKeyDown(GameKey.DEBUG2)) HyperObject.worldGV = FindObjectOfType<WorldBuilder>().NearestTile(HyperObject.worldGV).gv;
        }

        //Apply head bobbing
        float normalized_speed = displacement.magnitude / (height * walkingSpeed * Time.deltaTime);
        float maxBobOffset = height * HEAD_BOB_MAX * normalized_speed;
        float headDelta = maxBobOffset * Mathf.Sin(headBobState * HEAD_BOB_FREQ);
        if (normalized_speed > 0.25f) {
            headBobState += Mathf.Min(1.0f, (normalized_speed - 0.25f) * 10.0f) * Time.deltaTime;
        } else {
            headBobState = 0.0f;
        }

        //Rotate the held map
        if (mapCam && map) {
            mapCam.transform.localRotation = xRot;
            map.UpdateRotation(camRotation, xQuaternion);
        }

        //Update the Camera height
        HyperObject.camHeight = HM.TanK(height + headDelta);

        //Update world look vector
        Vector3 worldLook = xRot * Vector3.forward;
        HyperObject.worldLook = new Vector2(worldLook.x, worldLook.z);

        //Update forward bias point for NPC interaction
        forwardBias = xQuaternion * new Vector3(0.0f, 0.0f, height);
    }

    public bool IsLocked() {
        return isLocked;
    }

    public void LookAt(GameObject npc) {
        lockedNPC = npc;
        lockedHO = lockedNPC.GetComponentInParent<HyperObject>();
        Assert.IsNotNull(lockedHO);
    }

    public void Lock() {
        isLocked = true;
        LOCKED_MAX_X = 10.0f;
        LOCKED_MAX_Y = 10.0f;
    }

    public void Unlock() {
        lockedNPC = null;
        isLocked = false;
    }

    public void SetRotation(float lookX, float lookY) {
        rotationX = lookX;
        rotationY = lookY;
    }

    //Places the first value in a range that can be interpolated with the second value
    static void ModPi(ref float a, float b) {
        if (a - b > 180.0f) {
            a -= 360.0f;
        } else if (a - b < -180.0f) {
            a += 360.0f;
        }
    }

    public Vector3 IteratedCollide(Vector3 inDelta, float r, out Vector3 sinY, int iters) {
        Vector3 p1 = BodyCollisionPoint();
        Vector3 p2 = HeadCollisionPoint();
        Vector3 delta = inDelta;
        sinY = Vector3.zero;
        for (int i = 0; i < iters; ++i) {
            var name = WCollider.Collide2(p1 + delta, r, out Vector3 useless, name: "Hand");
            if (name == "Up" && SceneManager.GetActiveScene().buildIndex < SceneManager.sceneCountInBuildSettings - 1)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                //sceneDown = null;
                //sceneUp.allowSceneActivation = true;
            }
            else if (name == "Down" && SceneManager.GetActiveScene().buildIndex > 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
                //sceneUp = null;
                //sceneDown.allowSceneActivation = true;
            }
            delta += WCollider.Collide(p1 + delta, r, out Vector3 bodySinY, name: "Hand");
            delta += WCollider.Collide(p2 + delta, r, out Vector3 headSinY, name: "Hand");
            sinY.x = Mathf.Max(sinY.x, bodySinY.x);
            sinY.y = Mathf.Max(sinY.y, bodySinY.y);
            sinY.z = Mathf.Min(sinY.z, headSinY.z);
        }
        return delta * 0.9f;
    }
}
