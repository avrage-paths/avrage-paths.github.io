using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// For anything that wants to have sockets
/// </summary>
public class SocketController : MonoBehaviour
{
    public SocketHandler[] sockets;

    /// <summary>
    /// Setup the sockets to have information about the list they are a part of
    /// </summary>
    private void Awake()
    {
        for (int i = 0; i < sockets.Length; i++)
        {
            sockets[i].index = i;
            sockets[i].socketListCount = sockets.Length;
        }
    }
}
