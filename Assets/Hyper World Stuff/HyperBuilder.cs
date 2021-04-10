using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperBuilder : WorldBuilder
{
    public static int RADIUS = 5;
    public GameObject debug_tile;
    public GameObject center_tile;
    public GameObject outer_tile;
    public int tileType = 4;

    public override int MaxExpansion()
    {
        HM.SetTileType(tileType);
        return RADIUS;
    }

    public override GameObject GetTile(string coord)
    {
        if (coord == ""
            //|| coord == "U" || coord == "D" || coord == "L" || coord == "R"
            )
            return Instantiate(center_tile);
        else if (coord.Length == RADIUS)
        {
            GameObject tile = Instantiate(outer_tile);
            char noWall = coord[0];
            char noWall2 = coord[4];
            if (noWall == 'L')
                tile.transform.GetChild(1).gameObject.SetActive(false);
            else if (noWall == 'R')
                tile.transform.GetChild(2).gameObject.SetActive(false);
            else if (noWall == 'D')
                tile.transform.GetChild(3).gameObject.SetActive(false);
            else if (noWall == 'U')
                tile.transform.GetChild(4).gameObject.SetActive(false);
            if (tileType == 4 && noWall != noWall2)
            {
                if (noWall2 == 'L')
                    tile.transform.GetChild(1).gameObject.SetActive(false);
                else if (noWall2 == 'R')
                    tile.transform.GetChild(2).gameObject.SetActive(false);
                else if (noWall2 == 'D')
                    tile.transform.GetChild(3).gameObject.SetActive(false);
                else if (noWall2 == 'U')
                    tile.transform.GetChild(4).gameObject.SetActive(false);
            }
            return tile;
        }
        else
        {
            GameObject d = Instantiate(debug_tile);
            /*if (coord == "LLD")
                d.tag = "find";
            Instantiate(GameObject.FindGameObjectWithTag("find").
            transform.Find("Letter(Clone)")
            , GameObject.Find("tile_").transform);*/
            //Instantiate(d.transform.Find("Letter(Clone)"), GameObject.Find("tile_").transform);
            return d;
        }
    }
}
