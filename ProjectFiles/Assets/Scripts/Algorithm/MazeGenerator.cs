using System;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using System.Linq;
using Random = UnityEngine.Random;
using System.Collections;
using Valve.VR.InteractionSystem;

/// <summary>
/// Mangages the maze generation
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    public static MazeGenerator instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject junctionPrefab;
    public GameObject startPrefab;
    public GameObject endPrefab;
    public GameObject tutorialPrefab;

    /// <summary>
    /// The number of pieces spawned in the currently generated maze.
    /// Reset to 0 when ResetMaze() is called.
    /// </summary>
    int pieceCount = 0;

    /// <summary>
    /// When did the simulation start
    /// </summary>
    public float startTime = 0f;

    /// <summary>
    /// When did the simulation end
    /// </summary>
    public float endTime = 0f;

    /// <summary>
    /// A list to keep track of the path stemming from a single socket.
    /// Will be reset in pieceManager and used in Mazegenerator to ensure path lengths are being met. 
    /// </summary>
    private Dictionary<int, JunctionTypes> piecesPlacedOnCurrentPath = new Dictionary<int, JunctionTypes>();

    /// <summary>
    /// How many pieces should be used as buffers between junctions?
    /// Acts as a minimum straight distance
    /// </summary>
    public int bufferLength = 1;

    // TODO: Make this be part of researcher parameters
    /// <summary>
    /// How much to stretch the adjustable walls by
    /// </summary>
    public float wallStretchValue = 2.0f;

    /// <summary>
    /// The base height of walls if they are not modified (3 meters).
    /// </summary>
    private const float baseWallHeight = 3.0f;

    /// <summary>
    /// A boolean that allows for maze pieces to have ceilings or not.
    /// </summary>
    /// TODO: Connect this to the UI front end
    public bool hasCeiling = false;

    /// <summary>
    /// The base height of ceilings if they are not modified (3.5 meters)
    /// </summary>
    private const float baseCeilingHeight = baseWallHeight + 0.5f;

    /// <summary>
    /// The walls that are adjusted by their height
    /// </summary>
    private Transform[] adjustableWalls;

    void Start()
    {
        mazeEnded = false;
        startTime = endTime = 0;

        MazeParameterManager.instance?.RecordStack();

        this.wallStretchValue = MazeParameterManager.instance.wallHeight;
        this.hasCeiling = MazeParameterManager.instance.shouldUseCeiling;
        this.bufferLength = MazeParameterManager.instance.minStraightsInARow;

        liveResearcherUI.instance.updateData("Participant ID", MazeParameterManager.instance.participantID);
        liveResearcherUI.instance.updateData("Experiment Name", MazeParameterManager.instance.experimentName);
        liveResearcherUI.instance.updateData("Condition Name", MazeParameterManager.instance.conditionName);


        if (Player.instance != null)
        {
            //Make sure the player position is good to go 
            Player.instance.transform.position = new Vector3(0, 0, 0);
            Player.instance.transform.rotation = new Quaternion(0, 0, 0, 0);

            //Set up the players locomotion method
            ControlSystemManager controlManager = Player.instance.GetComponent<ControlSystemManager>();
            controlManager.enableControlSystem(MazeParameterManager.instance.movementType);
        }
        else
            Debug.LogError("ERROR: We can't find a reference to the Player! This probably means, you don't have a headset connected and steam VR is freaking out. " +
                "Try rebooting the application after ensuring that SteamVR is working properly and your headset is connected!");

        // We don't want this if replaying the maze
        if (!PieceManager.instance.replayingMaze)
            StartPieceScript.startPieceEntered += GenerateMaze;

        SetupWallsAndCeilings();
    }

    void OnDisable()
    {
        StartPieceScript.startPieceEntered -= GenerateMaze;
        ResetWallsAndCeilings();
    }

    /// <summary>
    /// Adjust the height of walls and enables ceilings in the maze
    /// if the condition has been toggled.
    /// </summary>
    private void SetupWallsAndCeilings()
    {
        // Get the instantiated tutorial game object for editing
        tutorialPrefab = GameObject.Find("TutorialPrefab");

        // Since this is inside of the start function of MazeGenerator,
        // it will only occur once, which is inexpensive

        // Get the walls of the start and junction prefabs
        Transform[] startChildren =
            startPrefab.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag("AdjustableWall")).ToArray();

        Transform[] junctionChildren =
            junctionPrefab.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag("AdjustableWall")).ToArray();

        Transform[] tutorialChildren =
            tutorialPrefab.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag("AdjustableWall")).ToArray();

        adjustableWalls = startChildren.Concat(junctionChildren).Concat(tutorialChildren).ToArray();

        foreach (Transform wall in adjustableWalls)
        {
            wall.localScale += new Vector3(0, wallStretchValue, 0);
            wall.localPosition = new Vector3(wall.localPosition.x, wall.localScale.y / 2, wall.localPosition.z);
        }

        // Get ceilings
        Transform startCeiling = startPrefab.transform.Find("Ceiling");
        Transform junctionCeiling = junctionPrefab.transform.Find("Ceiling");
        Transform tutorialCeiling = tutorialPrefab.transform.Find("Ceiling");
        Transform endCeiling = endPrefab.transform.Find("Ceiling");

        // make sure ceilings are set correctly
        startCeiling.gameObject.SetActive(hasCeiling);
        junctionCeiling.gameObject.SetActive(hasCeiling);
        tutorialCeiling.gameObject.SetActive(hasCeiling);
        endCeiling.gameObject.SetActive(hasCeiling);

        startCeiling.localPosition = new Vector3(0, baseWallHeight + wallStretchValue + 0.5f, 0);
        junctionCeiling.localPosition = new Vector3(0, baseWallHeight + wallStretchValue + 0.5f, 0);
        tutorialCeiling.localPosition = new Vector3(0, baseWallHeight + wallStretchValue, 0);
        endCeiling.localPosition = new Vector3(0, baseWallHeight + wallStretchValue + 0.5f, 0);
    }

    /// <summary>
    /// Resets the walls to their original heights and
    /// disables ceilings if they were originally enabled.
    /// </summary>
    private void ResetWallsAndCeilings()
    {
        // Reset back the scales of the prefabs so that
        // they do not appear strange in the editor
        foreach (Transform wall in adjustableWalls)
        {
            if (!wall)
            {
                Debug.LogError("Tried adjusting a wall that doesn't exist.");
                continue;
            }

            // Modifying the prefabs in the script modifies them in their actual
            // file, which is why baseWallHeight is being used.
            wall.localScale = new Vector3(wall.localScale.x, baseWallHeight, wall.localScale.z);
            wall.localPosition = new Vector3(wall.localPosition.x, wall.localScale.y / 2, wall.localPosition.z);
        }

        // Get ceilings
        Transform startCeiling = startPrefab.transform.Find("Ceiling");
        Transform junctionCeiling = junctionPrefab.transform.Find("Ceiling");
        Transform endCeiling = endPrefab.transform.Find("Ceiling");

        // revert ceilings to their original state
        startCeiling.gameObject.SetActive(false);
        junctionCeiling.gameObject.SetActive(false);
        endCeiling.gameObject.SetActive(false);

        startCeiling.localPosition = new Vector3(startCeiling.localPosition.x,
            baseCeilingHeight, startCeiling.localPosition.z);

        junctionCeiling.localPosition = new Vector3(junctionCeiling.localPosition.x,
            baseCeilingHeight, junctionCeiling.localPosition.z);

        endCeiling.localPosition = new Vector3(endCeiling.localPosition.x,
            baseCeilingHeight, endCeiling.localPosition.z);
    }

    /// <summary>
    /// Get the time elapsed since the maze started generating
    /// </summary>
    /// <returns></returns>
    public float GetElapsedTime()
    {
        if (endTime == 0)
            return Time.time - startTime;
        else
            return endTime - startTime;
    }

    /// <summary>
    /// Clear the pieces placed on the old sockets path, and and the parent junction to the list
    /// </summary>
    /// <param name="parent">The parent junction piece to which this path is being spawned from</param>
    public void wipePiecesPlacedAndAddNewParent(PieceManager.Piece parent)
    {

        piecesPlacedOnCurrentPath.Clear();

        piecesPlacedOnCurrentPath.Add(parent.id, parent.junctionType);
    }

    /// <summary>
    /// Spawn a piece on the given socket, using the socket on the given direction
    /// </summary>
    /// <param name="toConnectTo"></param>
    /// <param name="pieceID"></param>
    public GameObject SpawnPiece(SocketHandler toConnectTo, MazeParameterManager.JunctionData junc = null)
    {
        int directionToUse = toConnectTo.OppositeSocketIndex;

        //This will break things if you don't actually use the UI to generate 
        JunctionTypes junctionType;
        int id = -1;
        int straightsFollowing = 0;

        // the socket we are going to put a piece on
        // put a piece on the socket
        if (junc == null)
        {
            if (MazeParameterManager.instance != null)
            {
                junc = MazeParameterManager.instance.getNextPiece();

                //We need to check if we have prematurely placed an end piece 
                if (junc.junctionType == JunctionTypes.End)
                {

                    foreach (int currentID in piecesPlacedOnCurrentPath.Keys)


                    //We're trying to place an end piece, so lets see if it should actually be placed we do -1 to exlude the parent, which we didn't place. 
                    if (MazeParameterManager.instance.shouldUpdatePieceType(this.piecesPlacedOnCurrentPath.Count - 1))
                    {

                        //The path was cut short, so replace the end piece with something else 
                        junc = MazeParameterManager.instance.getNextAvailablePiece(this.piecesPlacedOnCurrentPath);
                    }
                }
            }
        }

                //Get the junction type so we know what to spawn
        junctionType = junc.junctionType;
                //Get the junction ID so we can ensure no duplicates in the future 
        id = junc.id;
                //Get the number of straights following this junction 
                straightsFollowing = junc.numStraights;


        GameObject prefabToSpawn = junctionPrefab;
        if (junctionType == JunctionTypes.End)
            {
            prefabToSpawn = endPrefab;
            }

        GameObject spawnedPiece = Instantiate(prefabToSpawn, transform);
        JunctionHandler spawnedHandler = spawnedPiece.GetComponent<JunctionHandler>();
        spawnedHandler.ConfigureWalls(junctionType, directionToUse);
        spawnedHandler.setJunctionID(id);
        spawnedHandler.setJunctionStraights(straightsFollowing);

        //TelemetryManager.instance.RecordData(TelemetryManager.DataCategory.MazeData.ToString(), data);
        PieceManager.instance.AddPiece(spawnedPiece).SetVisibilty(false);

        if (!PieceManager.instance.replayingMaze && !MazeParameterManager.instance.shouldUseExplicitOrdering)
        {

            piecesPlacedOnCurrentPath.Add(id, junctionType);
        }


        pieceCount++;

        spawnedPiece.name += " [" + pieceCount + "]";

        SocketController spawnedSocketController = spawnedPiece.GetComponent<SocketController>();
        // find a socket that is open
        SocketHandler socketToUse = spawnedSocketController.sockets[directionToUse % spawnedSocketController.sockets.Length];

        if (!socketToUse.isOpen())
        {
            Debug.LogError("We cant connect to a socket that is not open");
            return null;
        }

        // put the new piece on the socket
        socketToUse.ConnectTo(toConnectTo);


        if (!PieceManager.instance.replayingMaze)
            EnforceBufferStraights(spawnedHandler, junctionType, directionToUse, straightsFollowing, id);

        return spawnedPiece;
    }

    /// <summary>
    /// Given a newly spawned junction, determine if it needs buffers attached to it. 
    /// If it does, attach them
    /// </summary>
    /// <param name="spawnedHandler"></param>
    /// <param name="junctionType"></param>
    /// <param name="directionToUse"></param>
    public void EnforceBufferStraights(JunctionHandler spawnedHandler, JunctionTypes junctionType, int directionToUse, int numStraights, int id)
    {
        // straights dont need a buffer
        if (junctionType == JunctionTypes.Straight)
        {
            return;
        }
        // fill each open socket that is not in the direction we are going with a straight
        SocketHandler[] sockets = spawnedHandler.socketController.sockets;
        if (sockets == null)
        {
            return;
        }

        for (int currDir = 0; currDir < sockets.Length; currDir++)
        {
            if (currDir == directionToUse)
            {
                continue;
            }

            if (sockets[currDir].isOpen())
            {
                JunctionHandler currHandler = spawnedHandler;
                for (int i = 0; i < numStraights; i++)
                {
                    SocketHandler[] socketsToUse = currHandler.socketController.sockets;
                    // spawn a straight and connect it
                    GameObject spawnedPiece = Instantiate(junctionPrefab, transform);
                    pieceCount++;
                    JunctionHandler straightHandler = spawnedPiece.GetComponent<JunctionHandler>();
                    straightHandler.ConfigureWalls(JunctionTypes.Straight, currDir);
                    straightHandler.isBuffer = true;
                    straightHandler.setJunctionID(id);
                    SocketHandler straightSocket = spawnedPiece.GetComponent<SocketController>().sockets[(currDir + socketsToUse.Length / 2) % socketsToUse.Length];
                    SocketHandler junctionSocket = currHandler.socketController.sockets[currDir];
                    straightSocket.ConnectTo(junctionSocket);
                    spawnedPiece.name = spawnedHandler.gameObject.name + $"'s buffer" + (i == 0 ? " " : " (" + i + ")");
                    PieceManager.instance.AddPiece(spawnedPiece, isBuffer: true).SetVisibilty(false);
                    currHandler = straightHandler;
                }
            }
        }
    }

    /// <summary>
    /// Start off the maze generation
    /// </summary>
    /// <param name="starterPiece"></param>
    [Button]
    public void GenerateMaze(GameObject starterPiece = null)
    {
        startTime = Time.time;
    }

    /// <summary>
    /// Data container for maze summary statistics
    /// </summary>
    public class MazeSummary : TelemetryManager.DataContainer
    {
        /// <summary>
        /// The total number of pieces spawned in the maze
        /// </summary>
        public int piecesSpawned;
        /// <summary>
        /// The time it took to finish the maze, relative to when generation started
        /// </summary>
        public float timeToFinish;
        /// <summary>
        /// The total number of pieces spawned that created an impossible space
        /// </summary>
        public int impossibleSpacesSpawned;
        /// <summary>
        /// The total number of pieces along the user's path that created an impossible space
        /// </summary>
        public int impossibleSpacesOnPath;
        /// <summary>
        /// The total number of times that the user backtracked
        /// </summary>
        public int totalBacktrackedOccurances;
        #region Piece Distribution
        /// <summary>
        /// The percent of spawned pieces that are straight
        /// </summary>
        public float straightPercent;
        /// <summary>
        /// The percent of spawned pieces that are two way. This culminates all types of two way pieces
        /// </summary>
        public float twoWayPercent;
        /// <summary>
        /// The percent of spawned pieces that are three way. This culminates all types of three way pieces
        /// </summary>
        public float threeWayPercent;
        /// <summary>
        /// The percent of spawned pieces that are four way
        /// </summary>
        public float fourWayPercent;

        #endregion

        /// <summary>
        /// Given a list of pieces, get various summary statistics from it
        /// </summary>
        /// <returns></returns>
        public MazeSummary AggregateResults(List<PieceManager.Piece> pieces)
        {
            float userPathLength = pieces.Count;
            straightPercent = pieces.FindAll(x => x.junctionType == JunctionTypes.Straight).Count / userPathLength;
            twoWayPercent = pieces.FindAll(x => x.junctionType == JunctionTypes.LeftTurn || x.junctionType == JunctionTypes.RightTurn).Count / userPathLength;
            threeWayPercent = pieces.FindAll(x => x.junctionType == JunctionTypes.ThreeWayLeftRight || x.junctionType == JunctionTypes.ThreeWayLeftStraight || x.junctionType == JunctionTypes.ThreeWayRightStraight).Count / userPathLength;
            fourWayPercent = pieces.FindAll(x => x.junctionType == JunctionTypes.FourWay).Count / userPathLength;
            totalBacktrackedOccurances = pieces.Aggregate(0, (acc, x) => acc + x.backtrackedOccurances);
            impossibleSpacesOnPath = pieces.Aggregate(0, (acc, x) => acc + (x.impossibleSpace == null ? 0 : 1));
            return this;
        }
    }

    /// <summary>
    /// Information about the final path the user took through the maze
    /// </summary>
    public class UserPath : TelemetryManager.DataContainer
    {
        /// <summary>
        /// The type of junction this piece is
        /// </summary>
        public string pieceType;
        /// <summary>
        /// How much time was spent on this specific piece, in seconds
        /// </summary>
        public float timeSpentOnPiece;
        /// <summary>
        /// How many times this specific piece was backtracked on
        /// </summary>
        public int backtrackedOccurances;
    }

    /// <summary>
    /// Ensure we dont try to end the maze more than once
    /// Mostly used for if the experiment is aborted
    /// </summary>
    private bool mazeEnded = false;

    /// <summary>
    /// Deem the maze as finished and start the process of cleaning up
    /// </summary>
    public void EndMaze()
    {
        if (mazeEnded)
        {
            return;
        }
        mazeEnded = true;
        endTime = Time.time;

        MazeSummary summary = new MazeSummary
        {
            piecesSpawned = pieceCount,
            timeToFinish = GetElapsedTime(),
            impossibleSpacesSpawned = PieceManager.Piece.ImpossibleSpace.impossiblespaceCount
        };

        List<PieceManager.Piece> userPieces = new List<PieceManager.Piece>();
        GetUserPieces(PieceManager.instance.GetPiece(starterPiece), ref userPieces);
        summary.AggregateResults(userPieces);
        TelemetryManager.instance.RecordData(summary);

        for (int i = 0; i < userPieces.Count; i++)
        {
            TelemetryManager.instance.RecordData(new UserPath { pieceType = userPieces[i].junctionType.ToString(), timeSpentOnPiece = userPieces[i].timeSpentOnPiece, backtrackedOccurances = userPieces[i].backtrackedOccurances });
        }

        TelemetryManager.instance.ExportData();

        StartCoroutine(waitToSwitchScene(5));
    }

    /// <summary>
    /// Get all the pieces the user used to get to the end of the maze, in a linked-list fashion
    /// </summary>
    /// <param name="piece"></param>
    /// <param name="userPath"></param>
    private void GetUserPieces(PieceManager.Piece piece, ref List<PieceManager.Piece> userPath)
    {
        int max = 1000;
        if (piece == null) return;

        userPath.Add(piece);
        bool hasNext = true;
        while (hasNext && max-- > 0)
        {
            hasNext = false;
            for (int i = 0; i < piece.sockets.Length; i++)
            {
                PieceManager.Piece nextPiece = PieceManager.instance.GetPiece(piece.sockets[i].connectedSocket?.gridPiece);
                if (nextPiece == null) continue;
                if (nextPiece.steppedOn && !userPath.Contains(nextPiece))
                {
                    piece = nextPiece;
                    userPath.Add(piece);
                    hasNext = true;
                    break;
                }
            }
        }
        if (max <= 0)
        {
            Debug.LogError("Tried to get a user path that is over " + max + " pieces long. Something is probably wrong.");
        }
    }

    /// <summary>
    /// Simple coroutine to wait before switching the scene back to the main menu after a participant has finished the maze
    /// </summary>
    /// <param name="time">The time in seconds for it to wait</param>
    /// <returns>N/A</returns>
    IEnumerator waitToSwitchScene(int time)
    {
        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(time);

        PieceManager.instance.MazeToJSON();
        SceneManagerScript.instance.LoadScene(SceneManagerScript.instance.startingScene);
    }

    /// <summary>
    /// The beginning piece of the maze
    /// </summary>
    public GameObject starterPiece;
    /// <summary>
    /// Place the starter piece, which the rest of the maze is generated from
    /// </summary>
    /// <returns></returns>
    public GameObject PlaceStarter()
    {
        starterPiece = Instantiate(startPrefab, transform);
        PieceManager.instance.AddPiece(starterPiece);
        return starterPiece;
    }
}
