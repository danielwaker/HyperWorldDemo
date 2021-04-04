using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class WorldBuilder : MonoBehaviour {
    public float bounryAO = 0.62f;
    public float fog = 0.0f;
    public bool enableStereo = false;
    public bool scaleTanK = true;
    public bool lattice3D = false;
    public static float globalBounryAO;
    public static float globalFog;

    public struct Tile {
        public Tile(GyroVector _gv, string _coord, string _tileName) {
            gv = _gv; coord = _coord; tileName = _tileName;
        }
        public GyroVector gv;
        public string coord;
        public string tileName;
    }

    private List<Tile> tiles = new List<Tile>();
    private HashSet<string> hyperMapHash = new HashSet<string>();

    private void ExpandMap(int len) {
        for (int i = 0; i < tiles.Count; ++i) {
            string coord = tiles[i].coord;
            GyroVector gv = tiles[i].gv;
            if (coord.Length == len) {
                char last = (coord.Length > 0 ? coord[coord.Length - 1] : '\0');
                if (last != 'L') {
                    TrySpawn(coord + "R", gv + new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f));
                }
                if (last != 'R') {
                    TrySpawn(coord + "L", gv + new Vector3(-HM.CELL_WIDTH, 0.0f, 0.0f));
                }
                if (last != 'D') {
                    TrySpawn(coord + "U", gv + new Vector3(0.0f, 0.0f, HM.CELL_WIDTH));
                }
                if (last != 'U') {
                    TrySpawn(coord + "D", gv + new Vector3(0.0f, 0.0f, -HM.CELL_WIDTH));
                }
                if (lattice3D) {
                    if (last != 'F') {
                        TrySpawn(coord + "B", gv + new Vector3(0.0f, HM.CELL_WIDTH, 0.0f));
                    }
                    if (last != 'B') {
                        TrySpawn(coord + "F", gv + new Vector3(0.0f, -HM.CELL_WIDTH, 0.0f));
                    }
                }
            }
        }
    }

    //A Hack to increase the accuracy of Order-6 tiling.  Ideally something like this should
    //be done for all tilings but I don't know the math well enough to do it.
    private static string ReduceCoord6(string coord) {
        string[] repSrc = { "URRDL", "ULLDR", "DRRUL", "DLLUR", "URD", "ULD", "DRU", "DLU" };
        string[] repDst = { "RULLD", "LURRD", "RDLLU", "LDRRU", "RUL", "LUR", "RDL", "LDR" };
        for (int i = 0; i < repSrc.Length; ++i) {
            if (coord.EndsWith(repSrc[i])) {
                string leftSide = coord.Substring(0, coord.Length - repSrc[i].Length);
                string rightSide = repDst[i];
                char leftChar = (leftSide.Length > 0 ? leftSide[leftSide.Length - 1] : '\0');
                char rightChar = rightSide[0];
                if ((leftChar == 'L' && rightChar == 'R') || (leftChar == 'R' && rightChar == 'L')) {
                    coord = leftSide.Substring(0, leftSide.Length - 1) + rightSide.Substring(1);
                } else {
                    coord = leftSide + rightSide;
                }
            }
        }
        return coord;
    }

    private bool TrySpawn(string coord, GyroVector gv) {
        if (HM.N == 6 && !lattice3D) {
            string reduced = ReduceCoord6(coord);
            if (hyperMapHash.Contains(reduced)) {
                return false;
            }
            hyperMapHash.Add(reduced);
        } else {
            for (int i = 0; i < tiles.Count; ++i) {
                GyroVector gv2 = tiles[i].gv;
                if ((gv.vec - gv2.vec).sqrMagnitude < 1e-6f) {
                //if ((gv.vec - gv2.vec).sqrMagnitude < 1e-6f) {
                    //Debug.Log(coord + " --> " + hyperMapCoords[i]);
                    return false;
                }
            }
        }
        GameObject hyperTile = GetTile(coord);
        tiles.Add(new Tile(gv, coord, (hyperTile ? hyperTile.name : "null")));
        if (hyperTile == null) {
            return false;
        }
        hyperTile.name = "tile_" + coord;
        HyperObject[] hyperObjects = hyperTile.GetComponentsInChildren<HyperObject>();
        foreach (HyperObject hyperObject in hyperObjects) {
            hyperObject.localGV += gv;
        }

        Debug.Log(coord + gv);

        return true;
    }

    //Override to get a tile from a coordinate (may be null for empty)
    public abstract GameObject GetTile(string coord);
    //Override to specify the geometry of the world and how far to expand it
    public abstract int MaxExpansion();

    protected virtual void Awake () {
        //Set the global boundary AO
        globalBounryAO = bounryAO;
        globalFog = fog;
        HM.useTanKHeight = scaleTanK;

        //Spawn the entire map
        int numExpand = MaxExpansion();
        if (HM.N == 2) {
            TrySpawn("", GyroVector.identity);
            TrySpawn("R", new GyroVector(HM.CELL_WIDTH, 0.0f, 0.0f));
        } else if (HM.N == 3) {
            TrySpawn("", GyroVector.identity);
            ExpandMap(0);
            string coord = tiles[1].coord;
            GyroVector gv = tiles[1].gv;
            TrySpawn(coord + "R", gv + new Vector3(HM.CELL_WIDTH, 0.0f, 0.0f));
        } else {
            TrySpawn("", GyroVector.identity);
            for (int i = 0; i < numExpand; ++i) {
                ExpandMap(i);
            }
        }
        Debug.Log("Spawned: " + tiles.Count);

        //Once all objects are spawned, create collisions
        WCollider.AllColliders.Clear();
        WarpCollider[] allWC = FindObjectsOfType<WarpCollider>();
        foreach (WarpCollider wc in allWC) {
            wc.GenerateColliders();
        }
        Debug.Log("Colliders: " + WCollider.AllColliders.Count);

        //UnityEngine.XR.XRSettings. = UnityEngine.XR.XRSettings.StereoRenderingMode.

        //Tell the main camera to replace shaders
        Camera cam = Camera.main;
        cam.ResetReplacementShader();
        if (enableStereo) {
            GameObject rightCamObj = cam.transform.GetChild(0).gameObject;
            Camera rightCam = rightCamObj.GetComponent<Camera>();
            rightCamObj.SetActive(true);
            //cam.stereoTargetEye = StereoTargetEyeMask.Left;
            //cam.stereoTargetEye = StereoTargetEyeMask.Both;
            //cam.rect = new Rect(0.0f, 0.0f, 0.5f, 1.0f);
            cam.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            if (HM.K > 0.0f) {
                cam.SetReplacementShader(Shader.Find("Custom/SphericalShaderLeft"), "HyperRenderType");
                rightCam.SetReplacementShader(Shader.Find("Custom/SphericalShaderRight"), "HyperRenderType");
            } else if (HM.K < 0.0f) {
                cam.SetReplacementShader(Shader.Find("Custom/HyperbolicShaderLeft"), "HyperRenderType");
                rightCam.SetReplacementShader(Shader.Find("Custom/HyperbolicShaderRight"), "HyperRenderType");
            } else {
                cam.SetReplacementShader(Shader.Find("Custom/EuclideanShaderLeft"), "HyperRenderType");
                rightCam.SetReplacementShader(Shader.Find("Custom/EuclideanShaderRight"), "HyperRenderType");
            }
        } else if (HM.K > 0.0f) {
            cam.SetReplacementShader(Shader.Find("Custom/SphericalShader"), "HyperRenderType");
        } else if (HM.K == 0.0f) {
            cam.SetReplacementShader(Shader.Find("Custom/EuclideanShader"), "HyperRenderType");
        }

        //Draw the H2xE or S2xE map in the editor while playing to avoid camera issues 
#if UNITY_EDITOR
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv) {
            if (HM.K > 0.0f) {
                sv.SetSceneViewShaderReplace(Shader.Find("Custom/S2xEShader"), "HyperRenderType");
            } else if (HM.K < 0.0f) {
                sv.SetSceneViewShaderReplace(Shader.Find("Custom/H2xEShader"), "HyperRenderType");
            } else {
                sv.SetSceneViewShaderReplace(Shader.Find("Custom/EuclideanShader"), "HyperRenderType");
            }
        }
#endif
    }

    private void Update()
    {
        /*if (InputManager.GetKeyUp(GameKey.DEBUG2))
        {
            foreach (Tile t in tiles)
            {
                print("TILE LIST: " + t.coord + " GV: " + t.gv);
            }
        }*/
    }

    public float NearestTileDistance(GyroVector gv) {
        float minDist = float.MaxValue;
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVector gv2 = tiles[i].gv;
            float dist = (gv - gv2).vec.sqrMagnitude;
            if (dist < minDist) {
                minDist = dist;
            }
        }
        return minDist;
    }

    public Tile[] SurroundingTiles(GyroVector gv)
    {
        List<KeyValuePair<Tile, float>> tileList = new List<KeyValuePair<Tile, float>>();
        for (int i = 0; i < tiles.Count; ++i)
        {
            GyroVector gv2 = tiles[i].gv;
            float dist = (gv - gv2).vec.sqrMagnitude;
            tileList.Add(new KeyValuePair<Tile,float>(tiles[i], dist));
        }
        tileList.Sort((x, y) => x.Value.CompareTo(y.Value));
        return new Tile[] { tileList[1].Key, tileList[2].Key, tileList[3].Key, tileList[4].Key };
    }

    public Tile NearestTile(GyroVector gv) {
        float minDist = float.MaxValue;
        int bestTileIx = 0;
        for (int i = 0; i < tiles.Count; ++i) {
            GyroVector gv2 = tiles[i].gv;
            float dist = (gv - gv2).vec.sqrMagnitude;
            if (dist < minDist) {
                minDist = dist;
                bestTileIx = i;
            }
        }
        return tiles[bestTileIx];
    }

    public static GameObject MakeTile(string tileStr, Dictionary<string, GameObject> map) {
        string tile = tileStr.Substring(0, tileStr.Length - 1);
        if (!map.ContainsKey(tile)) {
            Debug.LogWarning("Builder is missing tile: " + tile);
            return null;
        }
        return RotateTile(Instantiate(map[tile]), tileStr[tileStr.Length - 1]);
    }

    public static GameObject RotateTile(GameObject tile, char dir) {
        if (dir == '<') {
            tile.transform.Rotate(0.0f, -90.0f, 0.0f);
        } else if (dir == '>') {
            tile.transform.Rotate(0.0f, 90.0f, 0.0f);
        } else if (dir == 'v') {
            tile.transform.Rotate(0.0f, 180.0f, 0.0f);
        }
        return tile;
    }

    public static GameObject RotateTileRandom(GameObject tile, string coord) {
        return RotateTileRandom(tile, GetDeterministicHash(coord));
    }

    public static GameObject RotateTileRandom(GameObject tile, uint hash) {
        tile.transform.Rotate(0.0f, 90.0f * ((hash / 256) % 4), 0.0f);
        return tile;
    }

    public static GameObject RotateExpansion(GameObject tile, string coord) {
        char dir = coord[0];
        if (dir == 'R') {
            tile.transform.Rotate(0.0f, -90.0f, 0.0f);
        } else if (dir == 'L') {
            tile.transform.Rotate(0.0f, 90.0f, 0.0f);
        } else if (dir == 'U') {
            tile.transform.Rotate(0.0f, 180.0f, 0.0f);
        }
        return tile;
    }

    public static GameObject MakeRandomTileAndRotate(GameObject[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return RotateTileRandom(Instantiate(tiles[hash % tiles.Length]), hash);
    }

    public static GameObject MakeRandomTile(GameObject[] tiles, string coord) {
        if (tiles.Length == 0) { return null; }
        uint hash = GetDeterministicHash(coord);
        return Instantiate(tiles[hash % tiles.Length]);
    }

    public static void AddNamedTiles(Dictionary<string, GameObject> dict, GameObject[] tiles) {
        foreach (GameObject tile in tiles) {
            if (!dict.ContainsKey(tile.name)) {
                dict.Add(tile.name, tile);
            } else if (dict[tile.name] != tile) {
                Debug.LogError("Found 2 tiles with the same name: " + tile.name);
            }
        }
    }

    public static uint GetDeterministicHash(string str) {
        unchecked {
            uint hash = 5381;
            for (int i = 0; i < str.Length; i++) {
                hash += str[i];
                hash = (hash << 5) ^ (hash >> 3);
            }
            return hash;
        }
    }
}
