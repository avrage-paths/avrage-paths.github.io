using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using UnityEditor;

/// <summary>
/// A single socket that is then managed (as a group) by a SocketController
/// </summary>
public class SocketHandler : MonoBehaviour
{
    /// <summary>
    /// The parent object of the socket; the object that will be moved when the socketting to another piece
    /// </summary>
    public GameObject gridPiece;
    /// <summary>
    /// A physical representation of the socket (position AND direction).<br></br>
    /// Used for debugging and visualizing the socket
    /// </summary>
    public GameObject arrow;
    /// <summary>
    /// Can either be occupied because its socketed or there is a wall 
    /// </summary>
    public bool isOccupied = false;
    /// <summary>
    /// The socket that is currently socketed to this socket.
    /// Null if there is no socketed object.
    /// </summary>
    public SocketHandler connectedSocket = null;

    /// <summary>
    /// The wall placed over the socket if is occupied, but not socketed to anything (aka blocked)
    /// </summary>
    public GameObject socketWall = null;

    /// <summary>
    /// The index of this socket in the list of sockets
    /// </summary>
    public int index = -1;

    /// <summary>
    /// The total number of sockets that this socket is a part of
    /// </summary>
    public int socketListCount = 0;

    /// <summary>
    /// The index of the socket opposite of this one (ie. North and South   or   East and West)
    /// </summary>
    public int OppositeSocketIndex => (index + socketListCount / 2) % socketListCount;

    /// <summary>
    /// Sets this socket as occupied
    /// </summary>
    /// <param name="occupier">The socket that is being connected to, if any</param>
    /// <returns>returns true if successully socketed</returns>
    public bool Occupy(SocketHandler occupier = null)
    {
        // if already occupied, detach whatever is currently occupying the socket
        // TODO: should we use isOpen() here? idc
        if (this.isOccupied && occupier is not null)
        {
            Debug.LogError(gridPiece.name + "'s " + "socket " + $"{(this.name)}" + "is already occupied, but tried to occupy it!");
            return false;
        }
        isOccupied = true;
        connectedSocket = occupier;
        if (!occupier && socketWall)
        {
            socketWall.SetActive(true);
        }
        arrow.SetActive(false);
        return true;
    }

    /// <summary>
    /// Detach the socket from whatever it is currently socketed to, if it is
    /// </summary>
    public void Detach()
    {
        // nothing to detach...
        if (this.isOpen())
        {
            return;
        }
        // we have to mark this as detached or else we create a cyclical detachment war
        SocketHandler tempConnectedSocket = this.connectedSocket;
        this.connectedSocket = null;

        // also detach whatever we were socketed to
        if (tempConnectedSocket)
        {
            tempConnectedSocket.Detach();
        }
        else if (socketWall)
        {
            socketWall.SetActive(false);
        }
        this.isOccupied = false;
        arrow.SetActive(true);
    }

    /// <returns>True if this socket can be sockted to</returns>
    public bool isOpen()
    {
        return !this.isOccupied;
    }

    /// <summary>
    /// Given two sockets, move the object in the first socket to the second socket
    /// </summary>
    /// <param name="socketToConnectTo">The "parent" socket</param>
    /// <param name="socketToMove">The socket that is being connected to the parent</param>
    /// <param name="objectToMove">The gameobject that socketToMove belongs to</param>
    [Button]
    public static void MoveToSocket(Transform socketToConnectTo, Transform socketToMove, GameObject objectToMove)
    {
        // we want to pivot about the socket
        GameObject pivot = new GameObject("Socketed Piece Pivot"), prevParent = objectToMove.transform.parent?.gameObject;

        //Instantiate(pivotPrefab, pivot.transform);

        // copy the socket's position and rotation
        pivot.transform.position = socketToMove.position;
        pivot.transform.rotation = socketToMove.rotation;
        // make it the pivot
        objectToMove.transform.parent = pivot.transform;
        // socket it to the socket
        pivot.transform.position = socketToConnectTo.position;
        pivot.transform.rotation = socketToConnectTo.rotation * Quaternion.Euler(0, 180, 0);

        // if the object had a parent, restore the OG parent and delete the pivot
        if (prevParent)
        {
            objectToMove.transform.parent = prevParent.transform;
            // delete the pivot
            if (Application.isEditor && !Application.isPlaying)
                Object.DestroyImmediate(pivot);
            else
                Object.Destroy(pivot);
            pivot = null;
        }
    }

    /// <summary>
    /// Connect the sockets together, both logically and physically
    /// </summary>
    /// <param name="socketToConnectTo"></param>
    /// <param name="socketToMove"></param>
    [Button]
    public static void ConnectSockets(SocketHandler socketToConnectTo, SocketHandler socketToMove)
    {
        if (!socketToConnectTo.isOpen() || !socketToMove.isOpen())
        {
            Debug.LogError("Tried to connect sockets, but one of the sockets is already occupied!");
            return;
        }

        // if either of the gridpieces are marked as static, we can't move them
        if (socketToConnectTo.gridPiece.isStatic || socketToMove.gridPiece.isStatic)
        {
            Debug.LogError("Tried to connect sockets, but one of the gridpieces is static!");
            return;
        }

        MoveToSocket(socketToConnectTo.transform, socketToMove.transform, socketToMove.gridPiece);

        socketToConnectTo.GetComponent<SocketHandler>().Occupy(socketToMove);
        socketToMove.GetComponent<SocketHandler>().Occupy(socketToConnectTo);
    }

    /// <summary>
    /// Connects this socket to the given socket
    /// </summary>
    /// <param name="socketToConnectTo"></param>
    public void ConnectTo(SocketHandler socketToConnectTo)
    {
        ConnectSockets(socketToConnectTo, this);
    }

    public override string ToString()
    {
        // Ex: SegmentPrefab (10)'s NorthSocket
        return (gridPiece?.name ?? "") + "'s " + $"{(this.name)}";
    }
}
