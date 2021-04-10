using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBuilder : WorldBuilder
{
    public static int RADIUS = 5;
    public GameObject debug_tile;
    public GameObject test_tile;
    public int tile_type;

    public override int MaxExpansion()
    {
        HM.SetTileType(tile_type);
        return RADIUS;
    }

    public override GameObject GetTile(string coord)
    {
        if (coord == "" 
            //|| coord == "U" || coord == "D" || coord == "L" || coord == "R"
            )
            return Instantiate(test_tile);
        else
            return Instantiate(debug_tile);
    }
}
