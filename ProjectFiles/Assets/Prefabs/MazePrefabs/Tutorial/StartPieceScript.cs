using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class StartPieceScript : MonoBehaviour
{
    public bool isInTutorialMode = true;
    public GameObject textRef;
    public SocketController socketController;

    public delegate void StartPieceEntered(GameObject startPiece);
    public static StartPieceEntered startPieceEntered;

    public void OnTriggerEnter(Collider other)
    {
        if (!isInTutorialMode)
        {
            return;
        }

        if (other.gameObject.tag != "Player")
        {
            Debug.LogError("Something other than the player has entered the start piece");
            return;
        }



        SetupStart();
    }

    public void SetupStart()
    {
        isInTutorialMode = false;
        textRef.SetActive(false);

        this.GetComponent<JunctionHandler>().ConfigureWalls(JunctionTypes.Start, 2);

        startPieceEntered?.Invoke(gameObject);
    }
}
