using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coords : MonoBehaviour
{
    public bool first = false;
    public Texture2D[] numbers;
    public Texture2D comma;
    public Texture2D minus;
    public GameObject letterPrefab;
    [Range(1, 10)]
    public int coordsDistance;
    // Start is called before the first frame update 

    void Start()
    {
        if (first)
        {
            for (int i = -coordsDistance + 1; i < coordsDistance; i++)
            {
                //Debug.Log(i);
                for (int j = -coordsDistance + 1; j < coordsDistance; j++)
                {
                    var newCoord = Instantiate(gameObject);
                    newCoord.GetComponent<Coords>().first = false;
                    newCoord.name += i * 10 + j;

                    int tex = 5;
                    int i_a = 0;
                    int j_a = 0;
                    if (i < 0)
                    {
                        tex++;
                        i_a = 1;
                    }
                    if (j < 0)
                    {
                        tex++;
                        j_a = 1;
                    }
                    float offsetX = 0.5f * tex;
                    for (int k = 0; k < tex; k++)
                    {
                        Texture2D curTex;
                        if (k == 0 + i_a) curTex = numbers[Mathf.Abs(i / 10)];
                        else if (k == 1 + i_a) curTex = numbers[Mathf.Abs(i % 10)];
                        else if (k == 2 + i_a) curTex = comma;
                        else if (k == 3 + i_a + j_a) curTex = numbers[Mathf.Abs(j / 10)];
                        else if (k == 4 + i_a + j_a) curTex = numbers[Mathf.Abs(j % 10)];
                        else curTex = minus;
                        /*switch (k)
                        {
                            case 0: curTex = numbers[i/10]; break;
                            case 1: curTex = numbers[i%10]; break;
                            case 2: curTex = comma; break;
                            case 3: curTex = numbers[j/10]; break;
                            case 4: curTex = numbers[j%10]; break;
                            default: curTex = minus; break;
                        }*/
                        GameObject letterObj = Instantiate(letterPrefab, newCoord.transform);
                        float x = (k - offsetX) * letterObj.transform.localScale.x;
                        letterObj.transform.Translate(x*0.1f, 0.0f, 0.0f);
                        letterObj.GetComponent<HyperObject>().localGV += new GyroVector(i * 0.1f, 0.005f, j * 0.1f);
                        letterObj.transform.parent = newCoord.transform;
                        Renderer letterRenderer = letterObj.GetComponent<Renderer>();
                        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                        letterRenderer.GetPropertyBlock(propertyBlock);
                        propertyBlock.SetTexture("_MainTex", curTex);
                        letterRenderer.SetPropertyBlock(propertyBlock);
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*if (!GetComponent<MeshRenderer>().enabled)
        {
            GetComponent<MeshRenderer>().enabled = true;
            Debug.Log("Mesh not enabled for " + gameObject.name);
        }
        else
            Debug.Log("Mesh enabled for " + gameObject.name);*/
    }
}
