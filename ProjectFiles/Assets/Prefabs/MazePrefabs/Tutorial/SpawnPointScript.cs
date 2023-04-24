using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointScript : MonoBehaviour
{
    public GameObject userPrefabToSpawn;
    void Start()
    {
        TutorialScript tutorialScript = GetComponentInParent<TutorialScript>();
        if (PieceManager.instance.replayingMaze) return;

        Instantiate(userPrefabToSpawn, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
