using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    void Start()
    {
        if (PieceManager.instance.replayingMaze)
        {
            Destroy(gameObject);
            return;
        }

        // connect a starting piece to this tutorial prefab
        // start pieces should be in "tutorial mode" when spawned
        SocketHandler startSocket = GetComponent<SocketController>().sockets[0];
        GameObject startPiece = MazeGenerator.instance.PlaceStarter();
        // assumes the first socket is the open one!
        startPiece.GetComponent<SocketController>().sockets[0].ConnectTo(startSocket);

        StartPieceScript.startPieceEntered += StartPieceEntered;
    }

    void StartPieceEntered(GameObject startPiece)
    {

        Destroy(gameObject);
    }

    void OnDisable()
    {
        StartPieceScript.startPieceEntered -= StartPieceEntered;
    }
}
