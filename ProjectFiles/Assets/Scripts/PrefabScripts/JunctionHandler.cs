using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Handle the configuration and behavior of a junction
/// </summary>
public class JunctionHandler : MonoBehaviour
{
    /// <summary>
    /// The sockets that belong to this junction
    /// </summary>
    public SocketController socketController;
    /// <summary>
    /// Is this piece used as a buffer between two junctions?
    /// </summary>
    public bool isBuffer = false;
    /// <summary>
    /// The type of junction this is
    /// </summary>
    public JunctionTypes junctionType;
    public int id;
    public int straightsFollowingJunction;
    public bool isTriggered = false;

    public GameObject endArea;

    /// <summary>
    /// Piggy-back off this junction to place the end area
    /// </summary>
    private void ReplaceWithEndArea()
    {
        GameObject spawnedArea = Instantiate(endArea, transform);
        SocketHandler endSocket = spawnedArea.GetComponent<EndArea>().socket;

        foreach (SocketHandler socket in socketController.sockets)
        {
            if (socket.isOpen())
            {
                // the end area needs to have a backwards socket for this...
                // TODO: use the socket the junction connects to rather than its open socket
                SocketHandler.MoveToSocket(socket.transform, endSocket.transform, endSocket.gridPiece);
                endSocket.Occupy();
                break;
            }
        }
        // avoid hiding the end area
        spawnedArea.transform.parent = null;
        // disable all mesh renderers and all colliders of this junction
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = false;
        }
        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // visibility system picks up the end area with this
        spawnedArea.transform.parent = transform;
        spawnedArea.name = name + "'s End Area";
    }

    /// <summary>
    /// Take the current junctin prefab, and modify it to whatever other
    /// segment it needs to be.
    /// Detaches all sockets for configuration!
    /// </summary>
    /// <param name="section">The type of section the junction will turn out to be.</param>
    /// <param name="parentIndex">The cardinal index of the parent socket used to properly orient the wall spawning.</param>
    public void ConfigureWalls(JunctionTypes section, int parentIndex)
    {
        if (section == JunctionTypes.End)
            return;

        name = section.ToString();
        junctionType = section;
        List<int> wallsToBlock = new List<int>();
        // make sure all sockets are open
        foreach (SocketHandler socket in socketController.sockets)
        {
            socket.Detach();
        }
        if (section is JunctionTypes.Start or JunctionTypes.End or JunctionTypes.LeftTurn or JunctionTypes.RightTurn or JunctionTypes.ThreeWayLeftRight)
            wallsToBlock.Add((parentIndex + 2) % 4);

        if (section is JunctionTypes.Start or JunctionTypes.End or JunctionTypes.Straight or JunctionTypes.LeftTurn or JunctionTypes.ThreeWayLeftStraight and not JunctionTypes.RightTurn)
            wallsToBlock.Add((parentIndex - 1 + 4) % 4);

        if (section is JunctionTypes.Start or JunctionTypes.End or JunctionTypes.Straight or JunctionTypes.RightTurn or JunctionTypes.ThreeWayRightStraight and not JunctionTypes.LeftTurn)
            wallsToBlock.Add((parentIndex + 1) % 4);

        foreach (int wall in wallsToBlock)
        {
            socketController.sockets[wall].Occupy();
        }

        //if (junctionType == JunctionTypes.End)
        //{
        //    ReplaceWithEndArea();
        //}
        }

    public void setJunctionID(int id)
    {
        this.id = id;
    }

    public void setJunctionStraights(int numStraights)
    {
        this.straightsFollowingJunction = numStraights;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PieceManager.instance.StepOnPiece(gameObject);
        }

        if (other.gameObject.tag == "GridPiece")
        {
            PieceManager.instance.AddCollidingPieces(gameObject, other.gameObject);
        }

        isTriggered = true;

    }
}
