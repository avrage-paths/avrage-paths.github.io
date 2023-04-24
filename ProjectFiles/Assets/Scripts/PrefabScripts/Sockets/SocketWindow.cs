
#if (UNITY_EDITOR)
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine.Rendering;

/// <summary>
/// A tool used to manually connect sockets. Available in the toolbar under "Maze Tools"
/// </summary>

class SocketWindow : EditorWindow
{
    public static SocketWindow window;
    public string selected = "";
    public List<SocketHandler> selectedSockets = new List<SocketHandler>();

    [MenuItem("Maze Tools/Socket Helper")]
    public static void OpenSocketWindow()
    {
        if (window == null)
            window = ScriptableObject.CreateInstance(typeof(SocketWindow)) as SocketWindow;
        window.ShowUtility();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Add sockets in pairs to make them a pair to be connected.\nFirst is the destination, second is source", EditorStyles.boldLabel, GUILayout.Height(50));
        // get all the sockets in the selection that arent occupied
        var sockets = Selection.GetFiltered<SocketHandler>(SelectionMode.Deep);
        // we cant use sockets that are occupied, filter them from the options
        sockets = sockets.Where(socket => socket.isOpen()).ToArray();

        for (int i = 0; i < sockets.Length; i++)
        {
            // remove sockets that have already been selected
            if (selectedSockets.Contains(sockets[i]))
                continue;
            EditorGUILayout.LabelField(sockets[i]?.gridPiece?.name + "'s " + sockets[i].name);
            if (GUILayout.Button("Add"))
            {
                selectedSockets.Add(sockets[i]);

            }
        }
        //selectedButton = GUILayout.SelectionGrid(selectedButton, new string[] { "Socket", "Unsocket" }, 2, EditorStyles.miniButton);
        //var socketsToConnect = sockets.Where((socket, index) => selectedIndices[index] == 1).ToArray();
        var socketsToConnect = selectedSockets.ToArray();

        // create string of sockets to connect
        string toSelect = "";
        for (int i = 0; i < socketsToConnect.Length; i += 2)
        {
            if (i == socketsToConnect.Length - 1)
                break;
            toSelect += socketsToConnect[i].gridPiece?.name + "'s " + socketsToConnect[i].name + " <- " + socketsToConnect[i + 1].gridPiece?.name + "'s " + socketsToConnect[i + 1].name + "\n";
        }
        EditorGUILayout.LabelField("To connect:", toSelect, EditorStyles.wordWrappedLabel);

        if (GUILayout.Button("Connect Sockets"))
        {
            for (int i = 0; i < socketsToConnect.Length; i += 2)
            {
                SocketHandler.ConnectSockets(socketsToConnect[i], socketsToConnect[i + 1]);
            }
            ClearSelections();
        }

        // button to clear selectedIndicies array
        if (GUILayout.Button("Clear"))
        {
            ClearSelections();
        }

        if (GUILayout.Button("Close"))
        {
            window = null;
            Close();
        }
    }

    void ClearSelections()
    {
        selectedSockets.Clear();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}
#endif