using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndArea : MonoBehaviour
{
    public SocketHandler socket;
    public GameObject[] endWalls;
    //public static bool ended = false;

    /// <summary>
    /// The walls that should be adjusted in height based on the stretch value
    /// </summary>
    public GameObject[] adjustableWalls;
    public ConfettiController confetti;

    private void Start()
    {
        if (MazeGenerator.instance.wallStretchValue == 0)
            return;

        // Increase the wall height by first stretching in the y-axis,
        // then translating it up in the same axis by half the stretch
        // value.
        foreach (GameObject wall in adjustableWalls)
        {
            wall.transform.localScale = wall.transform.localScale + new Vector3(0, MazeGenerator.instance.wallStretchValue, 0);
            wall.transform.Translate(0, (MazeGenerator.instance.wallStretchValue / 2), 0);
        }

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            HandleEnd();
        }
    }

    void HandleEnd()
    {
        foreach (GameObject wall in endWalls)
        {
            wall.SetActive(true);
        }
        MazeGenerator.instance.EndMaze();
        confetti.StartConfetti();
    }
}
