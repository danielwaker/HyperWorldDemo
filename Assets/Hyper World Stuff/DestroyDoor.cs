using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyDoor : MonoBehaviour
{

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            var wc = gameObject.transform.GetChild(0).gameObject.AddComponent<WarpCollider>();
            wc.boundingBoxes = new WarpCollider.Box[1];
            wc.boundingBoxes[0] = new WarpCollider.Box();
            wc.boundingBoxes[0].size = new Vector3(0.25f, 0.5f, 0.05f);
            wc.GenerateColliders();
        }
        else
        {
            Destroy(gameObject.transform.GetChild(0).gameObject);
        }
        if (SceneManager.GetActiveScene().buildIndex != SceneManager.sceneCountInBuildSettings - 1)
        {
            var wc = gameObject.transform.GetChild(1).gameObject.AddComponent<WarpCollider>();
            wc.boundingBoxes = new WarpCollider.Box[1];
            wc.boundingBoxes[0] = new WarpCollider.Box();
            //wc.boundingBoxes[0] = new WarpCollider.Box();
            wc.boundingBoxes[0].size = new Vector3(0.25f, 0.5f, 0.05f);
            wc.GenerateColliders();
        }
        else
        {
            Destroy(gameObject.transform.GetChild(1).gameObject);
        }

    }
}
