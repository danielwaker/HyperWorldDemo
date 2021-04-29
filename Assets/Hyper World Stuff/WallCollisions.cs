using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollisions : MonoBehaviour
{
    private WorldBuilder wb;
    private bool entered;
    private static bool globalEntered;
    private int walls;

    private void Start()
    {
        wb = FindObjectOfType<WorldBuilder>();
    }

    // Update is called once per frame
    void Update()
    {
        WorldBuilder.Tile nearest = WorldBuilder.nearest;
        if (gameObject.name == "tile_" + nearest.coord && !entered && !globalEntered)
        {
            entered = true;
            globalEntered = true;
            //Debug.Log("allcolliders start start: " + WCollider.AllColliders.Count);
            for (int i = 1; i < 5; i++)
            {
                //var test = WCollider.AllColliders.Count;
                if (gameObject.transform.GetChild(i).gameObject.activeSelf)
                {
                    walls++;
                    var wc = gameObject.transform.GetChild(i).gameObject.GetComponent<WarpCollider>();
                    wc.boundingBoxes = new WarpCollider.Box[1];
                    wc.boundingBoxes[0] = new WarpCollider.Box();
                    wc.boundingBoxes[0].size = new Vector3(0.2f, 1f, 2f);
                    wc.GenerateColliders();
                }
            }
            //Debug.Log("allcolliders start end: " + WCollider.AllColliders.Count);
            //Debug.Log("walls start: " + walls);
        }
        else if (gameObject.name != "tile_" + nearest.coord && entered && globalEntered)
        {
            //Debug.Log("allcolliders end start: " + WCollider.AllColliders.Count);
            var test = WCollider.AllColliders.Count;
            int index = test - walls * 12;
            int count = walls * 12;
            WCollider.AllColliders.RemoveRange(index, count);
            for (int i = 1; i < 5; i++)
            {
                if(gameObject.transform.GetChild(i).gameObject.activeSelf)
                {
                    var wc = gameObject.transform.GetChild(i).gameObject.GetComponent<WarpCollider>();
                    wc.boundingBoxes[0] = null;
                    wc.boundingBoxes = new WarpCollider.Box[0];
                    wc.GenerateColliders();
                }
            }
            walls = 0;
            //Debug.Log("allcolliders end end: " + WCollider.AllColliders.Count);
            entered = false;
            globalEntered = false;
        }
    }
}
