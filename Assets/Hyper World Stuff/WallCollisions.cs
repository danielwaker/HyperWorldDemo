using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollisions : MonoBehaviour
{
    private WorldBuilder wb;
    private bool entered;

    private void Start()
    {
        wb = FindObjectOfType<WorldBuilder>();
    }

    // Update is called once per frame
    void Update()
    {
        WorldBuilder.Tile nearest = WorldBuilder.nearest;
        if (gameObject.name == "tile_" + nearest.coord && !entered)
        {
            entered = true;
            for (int i = 1; i < 5; i++)
            {
                var test = WCollider.AllColliders.Count;
                var wc = gameObject.transform.GetChild(i).gameObject.GetComponent<WarpCollider>();
                wc.boundingBoxes = new WarpCollider.Box[1];
                wc.boundingBoxes[0] = new WarpCollider.Box();
                wc.boundingBoxes[0].size = new Vector3(0.2f, 1f, 2f);
                wc.GenerateColliders();
            }
        }
        else if (gameObject.name != "tile_" + nearest.coord && entered)
        {
            entered = false;
            var test = WCollider.AllColliders.Count;
            int index = test - 4 * 12;
            int count = 4 * 12;
            WCollider.AllColliders.RemoveRange(index, count);
            for (int i = 1; i < 5; i++)
            {
                var wc = gameObject.transform.GetChild(i).gameObject.GetComponent<WarpCollider>();
                wc.boundingBoxes[0] = null;
                wc.boundingBoxes = new WarpCollider.Box[0];
                wc.GenerateColliders();
            }
        }
    }
}
