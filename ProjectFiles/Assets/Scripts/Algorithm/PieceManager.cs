using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using System.Collections;

class Constants
{
    /// <summary>
    /// The opacity of hidden pieces
    /// </summary>
    public const float semiTransparentOpacity = 0.2f;
    /// <summary>
    /// The opacity of invisible pieces
    /// </summary>
    public const float invisibleOpacity = 0.0f;
    /// <summary>
    /// The opacity of visible pieces
    /// </summary>
    public const float visibleOpacity = 1.0f;
}

/// <summary>
/// Storage for all the pieces in the maze, with utility functions to help.
/// Also handles the visibilty and backtracking of pieces, relative to the player
/// </summary>
public class PieceManager : MonoBehaviour
{

    // Used to sort visibility HashSet
    class OrderSpawnedDecreasing : IComparer<GameObject>
    {
        public int Compare(GameObject x, GameObject y)
        {
            // Compare game object instance IDs in reverse order
            return instance.GetPiece(y).orderSpawned.CompareTo(instance.GetPiece(x).orderSpawned);
        }
    }


    /// <summary>
    /// Helper class for orginization of each piece
    /// </summary>
    public class Piece
    {
        /// <summary>
        /// Each colliding piece is connected to a single impossible space
        /// </summary>
        public class ImpossibleSpace
        {
            /// <summary>
            /// How many distinct impossible spaces there are
            /// </summary>
            public static int impossiblespaceCount = 0;
            /// <summary>
            /// The list of all pieces colliding in the impossible space
            /// </summary>
            private HashSet<Piece> collidingPieces;
            /// <summary>
            /// The amount of pieces visible in this impossible space
            /// </summary>
            public int visible = 0;

            public ImpossibleSpace(Piece piece)
            {
                collidingPieces = new HashSet<Piece>();
                Add(piece);
                impossiblespaceCount++;
            }

            /// <summary>
            /// Adds a piece to this impossible space
            /// </summary>
            /// <param name="piece">The piece to add</param>
            public void Add(Piece piece)
            {
                collidingPieces.Add(piece);
            }

            /// <summary>
            /// Goes through each piece in the impossible space and sets the opacity
            /// </summary>
            /// <param name="opacity"></param>
            public void SetMaterialOpacity()
            {
                HashSet<Piece> seenPieces = new HashSet<Piece>();

                foreach (Piece piece in collidingPieces)
                {
                    if (piece.visible)
                    {
                        Collider pieceCollider = piece.Center.GetComponent<Collider>();
                        Vector3 size = pieceCollider.bounds.size;
                        Vector3 center = pieceCollider.bounds.center;

                        Collider[] colliders = Physics.OverlapBox(center, size / 2);

                        foreach (Collider collider in colliders)
                        {
                            if (collider.tag != "GridPiece") continue;

                            Piece p = PieceManager.instance.GetPiece(collider.transform.gameObject);
                            p.SetMaterialOpacity(Constants.invisibleOpacity);

                            seenPieces.Add(p);
                        }

                        piece.SetMaterialOpacity(Constants.visibleOpacity);
                    }
                    else if (!seenPieces.Contains(piece))
                        piece.SetMaterialOpacity(Constants.semiTransparentOpacity);
                }
            }

            public override string ToString()
            {
                return String.Join(", ", collidingPieces.Select(x => x.Center.name).ToArray());
            }
        }

        /// <summary>
        /// A number representing when the piece was spawned starting with 0
        /// </summary>
        public int orderSpawned;
        /// <summary>
        /// Tells whether this piece is a buffer piece or not (1 buffer between each junction)
        /// </summary>
        public bool isBuffer;

        /// <summary>
        /// A reference to this piece's impossible space
        /// </summary>
        public ImpossibleSpace impossibleSpace = null;
        /// <summary>
        /// Checks if the impossible space exists
        /// </summary>
        private bool isImpossibleSpace => impossibleSpace != null;
        /// <summary>
        /// Whether or not this piece is visible
        /// </summary>
        private bool visible = false;

        /// <summary>
        /// The pieces this current piece can see
        /// </summary>
        private HashSet<GameObject> _visibility = null;
        /// <summary>
        /// The pieces this current piece can see
        /// </summary>
        public HashSet<GameObject> visibility
        {
            get { return _visibility; }   // get method
            set { _visibility = value; }  // set method
        }

        /// <summary>
        /// If the piece is currently stepped on
        /// </summary>
        private bool _steppedOn = false;
        /// <summary>
        /// If the piece is currently stepped on
        /// </summary>
        public bool steppedOn => _steppedOn;
        /// <summary>
        /// Tells us whether this piece has EVER been stepped on
        /// </summary>
        public bool hasEverBeenSteppedOn = false;

        /// <summary>
        /// Marks placed above pieces to show the user's path
        /// </summary>
        private GameObject userPathMark;

        /// <summary>
        /// The socket the user entered this piece
        /// </summary>
        private int entrySocket = -1;
        /// <summary>
        /// The socket the user exited this piece
        /// </summary>
        private int exitSocket = -1;
        /// <summary>
        /// Gets the direction the user exited this piece from
        /// </summary>
        public int exitDirection => exitSocket;

        /// <summary>
        /// A reference to the telemetry manager
        /// </summary>
        private static TelemetryManager Telemetry => TelemetryManager.instance;

        #region Telemetry
        /// <summary>
        /// How long the user spent on this piece
        /// </summary>
        public float timeSpentOnPiece = 0;
        /// <summary>
        /// The time the user last stepped on this piece
        /// </summary>
        public float lastTimeSteppedOn = -1f;

        /// <summary>
        /// How many times was this piece backtracked onto
        /// </summary>
        public int backtrackedOccurances = 0;

        /// <summary>
        /// Data container that encapsulates the data for stepping on a piece that is impossible
        /// </summary>
        public class ImpossibleSpaceOccurances : TelemetryManager.DataContainer
        {
            /// <summary>
            /// When this impossible space was encountered, relative to the start of the maze
            /// </summary>
            public float timestamp;
            /// <summary>
            /// The name of the piece was the stepped on in this impossible space
            /// </summary>
            public string pieceName;
            /// <summary>
            /// The pieces that were colliding with the stepped on piece when this impossible space was encountered <br/>
            /// These are the pieces responbile for making the space impossible
            /// </summary>
            public string collidingPieces;


            public ImpossibleSpaceOccurances(Piece piece, float timestamp)
            {
                this.timestamp = timestamp;
                this.pieceName = piece.Center.name;
                this.collidingPieces = piece.impossibleSpace.ToString();
            }
        }

        /// <summary>
        /// Send data to the Telemetry system
        /// </summary>
        private void RecordData()
        {
            if (isImpossibleSpace)
                Telemetry?.RecordData(new ImpossibleSpaceOccurances(this, MazeGenerator.instance.GetElapsedTime()));
        }

        #endregion

        public Piece(GameObject piece, int orderSpawned, bool isBuffer)
        {
            this.piece = piece;
            if (piece == null) return;

            // get all the different connections once we have the socket class setup.   
            sockets = piece.GetComponent<SocketController>().sockets;
            this.orderSpawned = orderSpawned;
            this.isBuffer = isBuffer;

            CreateMark();
        }

        /// <summary>
        /// The GameObject of this Piece data structure
        /// </summary>
        public GameObject Center => piece;
        /// <summary>
        /// The GameObject of this Piece data structure
        /// </summary>
        private readonly GameObject piece;

        /// <summary>
        /// Gets the junction type of the piece
        /// </summary>
        public JunctionTypes junctionType
        {
            get
            {
                JunctionHandler junctionHandler;

                piece.TryGetComponent(out junctionHandler);

                return junctionHandler == null ? JunctionTypes.End : junctionHandler.junctionType;
            }

        }

        /// <summary>
        /// Gets the piece's ID generated from the stack
        /// </summary>
        public int id => piece.GetComponent<JunctionHandler>().id;

        /// <summary>
        /// Int to know the number of buffer straights following this junction
        /// </summary>
        public int straightsFollowingJunction => piece.GetComponent<JunctionHandler>().straightsFollowingJunction;

        /// <summary>
        /// The different connections from this piece
        /// </summary>
        public SocketHandler[] sockets;

        public SocketHandler GetSocket(int dir)
        {
            if (junctionType == JunctionTypes.End) return null;

            return sockets[dir % sockets.Length];
        }

        /// <summary>
        /// Find the GameObject in the specified direction connected to this piece
        /// </summary>
        /// <param name="direction">The socket direction to retrieve</param>
        public GameObject GetDirection(int direction)
        {
            int dir = direction % sockets.Length;

            return sockets[dir].connectedSocket?.gridPiece;
        }

        /// <summary>
        /// Gets the opposite socket from a given direction (ie. Opposite socket of North is South. Opposite socket of East is West)
        /// </summary>
        public int OppositeSocket(int dir)
        {
            return (dir + sockets.Length / 2) % sockets.Length;
        }

        #region Visibility and Impossible Spaces
        /// <summary>
        /// Sets the visibility for the given piece and it's impossible space 
        /// </summary>
        /// <param name="visible">The new visibility</param>
        /// <returns>The impossible space of THIS piece</returns>
        public ImpossibleSpace SetVisibilty(bool visible)
        {
            if (piece == null) return null;

            // Set opacity of non-impossible spaces
            if (!isImpossibleSpace)
                SetMaterialOpacity(visible ? Constants.visibleOpacity : Constants.semiTransparentOpacity);

            SetLayer(visible ? "PlayerVisible" : "Researcher");
            SetVisible(visible);

            return impossibleSpace;
        }

        /// <summary>
        /// Changes the layer of the piece based on the visibility
        /// </summary>
        /// <param name="layer">The new layer (either "PlayerVisible" or "Researcher")</param>
        private void SetLayer(string layer)
        {
            Center.layer = LayerMask.NameToLayer(layer);
            HashSet<string> layers = new HashSet<string>{"PlayerVisible", "Researcher"};

            Transform[] children = Center.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform child in children)
            {
                // Skip objects that are not "PlayerVisible" or "Researcher"
                if (!layers.Contains(LayerMask.LayerToName(child.gameObject.layer))) continue;

                child.gameObject.layer = LayerMask.NameToLayer(layer);
            }

            // we only ever want the user path mark to be visible to the researcher
            userPathMark.gameObject.layer = LayerMask.NameToLayer("Researcher");
        }

        /// <summary>
        /// Assigns the visibility for this piece and updates the total visible in the impossible space
        /// </summary>
        /// <param name="visible"></param>
        private void SetVisible(bool visible)
        {
            // if this piece flips states (visible/invisible), adjust the impossible space counter
            if (isImpossibleSpace)
            {
                if (this.visible && !visible)
                    impossibleSpace.visible--;
                else if (!this.visible && visible)
                    impossibleSpace.visible++;
            }

            this.visible = visible;
        }

        /// <summary>
        /// Given a colliding piece, setup the impossible space and the corresponding collisions
        /// </summary>
        /// <param name="otherPiece"></param>
        public void AddToImpossibleSpace(GameObject otherPiece)
        {
            Piece piece = instance.GetPiece(otherPiece);

            // Merge or create a new impossible space
            if (impossibleSpace == null)
                impossibleSpace = piece.isImpossibleSpace ? piece.impossibleSpace : new ImpossibleSpace(this);

            // since we are colliding, we just spawned a piece.
            // thus, one piece must be visible and all others invisible
            if (!instance.replayingMaze)
            {
                SetMaterialOpacity(visible ? Constants.visibleOpacity : Constants.invisibleOpacity);
                impossibleSpace.visible++;
            }
            else
                SetMaterialOpacity(Constants.semiTransparentOpacity);

            // Add the other piece if its not in it yet
            impossibleSpace.Add(piece);
            piece.impossibleSpace = impossibleSpace;

            SetMaterialImpossibleSpaceParameters();
        }

        /// <summary>
        /// Set the opacity of all the materials in the researcher view
        /// </summary>
        /// <param name="opacity">New opacity (0 = invisible, 1 = opaque)</param>
        private void SetMaterialOpacity(float opacity)
        {
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] material = renderer.materials;

                foreach (Material mat in material)
                    mat.SetFloat("_Opacity", opacity);
            }
        }

        /// <summary>
        /// Updates the current amount of impossible pieces and sets the current to an impossible space
        /// </summary>
        private void SetMaterialImpossibleSpaceParameters()
        {
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] material = renderer.materials;

                foreach (Material mat in material)
                {
                    if (mat.HasProperty("_ImpossiblePieces"))
                    {
                        float impossiblePieces = mat.GetFloat("_ImpossiblePieces");

                        mat.SetFloat("_ImpossiblePieces", impossiblePieces + 0.1f);
                        mat.SetFloat("_ImpossibleSpace", 1.0f);
                    }
                }
            }
        }
        #endregion

        #region Stepping On Pieces
        /// <summary>
        /// When the user enters a piece, this determines if they stepped on it for the first time
        /// </summary>
        /// <param name="enterPosition">The point of collision between the user and the junction piece</param>
        public void StepOnPiece(Vector3 enterPosition)
        {
            int dir = instance.GetDirection(this, enterPosition);
            StepOnPiece(dir);
        }

        /// <summary>
        /// When the user enters a piece, this determines if they stepped on it for the first time
        /// </summary>
        /// <param name="dir">The direction the user is entering the piece from</param>
        public void StepOnPiece(int dir)
        {
            dir %= sockets.Length;

            if (entrySocket == -1)
            {
                entrySocket = dir;
                _steppedOn = true;

                //We need to keep up with the remaining pieces 
                if (!this.isBuffer)
                {
                    MazeStack?.removePieceFromRemaining(this.id);
                }
                RecordData();
                instance.UserPathPieces.Add(this);
            }
            else if (!instance.replayingMaze)
                userPathMark.SetActive(false);
        }

        /// <summary>
        /// When the user exits a piece, this determines if they are leaving from the same way they came in
        /// </summary>
        /// <param name="exitPosition">The point of collision between the user and the junction piece</param>
        public void StepOffPiece(Vector3 exitPosition)
        {
            int dir = instance.GetDirection(this, exitPosition);
            StepOffPiece(dir);
        }

        /// <summary>
        /// When the user exits a piece, this determines if they are leaving from the same way they came in
        /// </summary>
        /// <param name="dir">The direction the user is exiting the piece from</param>
        public void StepOffPiece(int dir)
        {
            // dir %= sockets.Length;

            // -1 means the piece has never been stepped on
            if (this.lastTimeSteppedOn != -1)
            {
                this.timeSpentOnPiece += Time.time - this.lastTimeSteppedOn;
            }

            if (entrySocket == dir && junctionType != JunctionTypes.End)
            {
                entrySocket = -1;

                if (!instance.replayingMaze)
                {
                    _steppedOn = false;
                    userPathMark.SetActive(false);
                    exitSocket = -1;

                    instance.UserPathPieces.Remove(this);

                    //They back-tracked so add it back to our remaining pieces. 
                    if (!this.isBuffer)
                    {
                        MazeStack?.addPieceToRemaining(this.id, this.junctionType, this.straightsFollowingJunction);

                    }

                    backtrackedOccurances++;
                }
            }
            else
                EnableMark(dir);
        }

        /// <summary>
        /// Instantiates the marker to indicate the user's path
        /// </summary>
        private void CreateMark()
        {
            userPathMark = instance.SpawnMarker(piece);
            userPathMark.SetActive(false);
        }

        /// <summary>
        /// Turns on the user's path marker
        /// </summary>
        /// <param name="dir">The direction the user is going</param>
        /// <param name="respawningMaze">Whether or not the maze is being loaded in, or being generated at runtime</param>
        public void EnableMark(int dir, bool respawningMaze = false)
        {
            if (respawningMaze || !instance.replayingMaze)
            {
                int rotation = (int)MazeGenerator.instance.starterPiece.transform.rotation.y + 90 * (dir + 1 % sockets.Length);


                // Only enable the marker on non-end pieces
                if (junctionType != JunctionTypes.End)
                {
                userPathMark.SetActive(true);
                userPathMark.transform.localRotation = Quaternion.Euler(0, rotation, 0);
                }

                exitSocket = dir;

                _steppedOn = true;

                // The marker spawns after the initial visibility call in MazeGenerator.cs, so it is unaffected
                if (respawningMaze)
                    SetVisibilty(visible);
            }
        }
        #endregion
    }

    /// <summary>
    /// The pieces that are currently in the user's taken path
    /// </summary>
    public List<Piece> UserPathPieces = new List<Piece>();

    /// <summary>
    /// The instance of the piece manager to be called from other classes
    /// </summary>
    public static PieceManager instance;

    private void OnDestroy()
    {
        instance = null;
    }

    void Awake()
    {
        //If theres's already one 
        if (instance != null)
        {
            Debug.LogWarning("Already detected a piece manager, replacing the old one with a new one");
            Destroy(instance.gameObject);
        }

        instance = this;
    }

    /// <summary>
    /// Are we currently replaying the maze?
    /// </summary>
    public bool replayingMaze = false;

    /// <summary>
    /// The GameObject of the user, used for replaying the maze
    /// </summary>
    [SerializeField]
    private GameObject userPrefabToSpawn;
    /// <summary>
    /// The GameObject of the marker to indicate the path of the user
    /// </summary>
    [SerializeField]
    private GameObject userPathMarker;

    /// <summary>
    /// A player for traversing through the maze replay
    /// </summary>
    private GameObject userReplay;
    /// <summary>
    /// The final path the player took
    /// </summary>
    public List<Piece> userPath;
    /// <summary>
    /// The index in the userPath for replaying the maze
    /// </summary>
    private int userPathIndex = 0;

    /// <summary>
    /// A list of all spawned pieces during this maze instance
    /// </summary>
    public Dictionary<int, Piece> spawnedPieces = new Dictionary<int, Piece>();
    /// <summary>
    /// The piece most recently stepped on by the player
    /// </summary>
    private Piece currentPiece = null;
    /// <summary>
    /// The pieces currently being shown to the player
    /// </summary>
    private HashSet<GameObject> visiblePieces => currentPiece?.visibility ?? new HashSet<GameObject>();
    /// <summary>
    /// Reference to the Maze Stack class
    /// </summary>
    private static MazeParameterManager MazeStack => MazeParameterManager.instance;

    /// <summary>
    /// Sets the current piece to the given GameObject
    /// </summary>
    /// <param name="piece">The gameobject to be the new current piece</param>
    public void SetCurrentPiece(GameObject piece)
    {
        this.currentPiece = GetPiece(piece);
    }

    /// <summary>
    /// Spawns the user path marker associated to a given piece
    /// </summary>
    /// <param name="piece">The piece to recieve the marker</param>
    /// <returns>The GameObject marker just created</returns>
    public GameObject SpawnMarker(GameObject piece)
    {
        return Instantiate(userPathMarker, piece.transform);
    }

    /// <summary>
    /// Given a gameObject with proper scripts attached (ex. JunctionHandler), add it to the list.
    /// </summary>
    /// <param name="piece"></param>
    public Piece AddPiece(GameObject piece, bool isBuffer = false)
    {
        return spawnedPieces[piece.GetInstanceID()] = new Piece(piece, spawnedPieces.Count, isBuffer);
    }

    /// <summary>
    /// Given a GameObject, return the Piece generated for it.
    /// If the piece is not found, it is added to the list and returned.
    /// </summary>
    /// <param name="piece"></param>
    /// <returns>A new piece if nothing is found, otherwise, the requested data</returns>
    public Piece GetPiece(GameObject piece)
    {
        if (piece == null) return null;

        // makes a new piece if one does not exist
        if (!spawnedPieces.ContainsKey(piece.GetInstanceID()))
            return AddPiece(piece);

        return spawnedPieces[piece.GetInstanceID()];
    }

    /// <summary>
    /// Given a GameObject's ID, return the Piece generated for it.
    /// </summary>
    /// <param name="instanceID">The GameObject's instanceID</param>
    /// <returns>null if nothing is found, otherwise, the requested data</returns>
    public Piece GetPiece(int instanceID)
    {
        return spawnedPieces[instanceID];
    }

    /// <summary>
    /// Given two overlapping pieces, adds them to their impossible space
    /// </summary>
    /// <param name="piece1">The first colliding piece GameObject</param>
    /// <param name="piece2">The second colliding piece GameObject</param>
    public void AddCollidingPieces(GameObject piece1, GameObject piece2)
    {
        // Add to the collision list for each respective piece
        GetPiece(piece1).AddToImpossibleSpace(piece2);
        GetPiece(piece2).AddToImpossibleSpace(piece1);
    }

    #region Maze Visibility and Generation
    /// <summary>
    /// Updates what pieces the user is able to see. Updates the stack based on visibility of pieces.
    /// </summary>
    private void UpdateVisiblePieces(Piece newPiece)
    {
        HashSet<GameObject> newVisibility = newPiece.visibility;
        HashSet<Piece.ImpossibleSpace> impossibleSpaces;

        impossibleSpaces = HandleVisiblePieces(newVisibility, newPiece);
        impossibleSpaces.UnionWith(HandleInvisiblePieces(newVisibility));

        // Get rid of empty pieces with no impossible spaces
        if (impossibleSpaces.Contains(null))
            impossibleSpaces.Remove(null);

        // Set the opacity of each impossible space
        foreach (Piece.ImpossibleSpace impossibleSpace in impossibleSpaces)
            impossibleSpace.SetMaterialOpacity();
    }

    /// <summary>
    /// Turns on visible pieces and pops the correct amount of pieces off the stack
    /// </summary>
    private HashSet<Piece.ImpossibleSpace> HandleVisiblePieces(HashSet<GameObject> newVisibility, Piece newPiece)
    {
        HashSet<Piece.ImpossibleSpace> impossibleSpaces = new HashSet<Piece.ImpossibleSpace>();

        // make all pieces in newVisibilty visible
        foreach (GameObject piece in newVisibility)
            impossibleSpaces.Add(GetPiece(piece).SetVisibilty(true));

        // If the piece has been stepped on before, we know the connected
        // pieces need to be accounted for in the stack
        if (newPiece.hasEverBeenSteppedOn)
        {
            HashSet<GameObject> toShow = new HashSet<GameObject>(newVisibility);
            toShow.ExceptWith(this.visiblePieces);

            foreach (GameObject piece in toShow)
            {
                Piece p = GetPiece(piece);

                if (MazeParameterManager.instance.shouldUseExplicitOrdering || 
                    p.isBuffer || p.steppedOn || p.junctionType == JunctionTypes.End) continue;

                MazeStack?.getNextPiece();
            }
        }

        return impossibleSpaces;
    }

    /// <summary>
    /// Hide pieces that are no longer visible and add correct pieces back to the stack
    /// </summary>
    /// <param name="newVisibility"></param>
    private HashSet<Piece.ImpossibleSpace> HandleInvisiblePieces(HashSet<GameObject> newVisibility)
    {
        HashSet<Piece.ImpossibleSpace> impossibleSpaces = new HashSet<Piece.ImpossibleSpace>();

        // get all the pieces that are in newVisibility but not in visiblePieces
        SortedSet<GameObject> toHide = new SortedSet<GameObject>(this.visiblePieces, new OrderSpawnedDecreasing());
        toHide.ExceptWith(newVisibility);

        // go through the pieces to hide in sorted order
        foreach (GameObject piece in toHide)
        {
            Piece p = GetPiece(piece);
            impossibleSpaces.Add(p.SetVisibilty(false));

            if (MazeParameterManager.instance.shouldUseExplicitOrdering || 
                p.isBuffer || p.steppedOn || p.junctionType == JunctionTypes.End) continue;

            MazeStack?.addToStack(GetPiece(piece).id, GetPiece(piece).junctionType, GetPiece(piece).straightsFollowingJunction);
        }

        return impossibleSpaces;
    }

    /// <summary>
    /// Returns the pieces that should be visible from the given piece, call when the user steps into a new piece
    /// </summary>
    /// <param name="start">The origin piece for the search</param>
    /// <returns></returns>
    public HashSet<GameObject> GetVisiblePieces(GameObject start)
    {
        // If we have already calculated the visibility, return it
        Piece piece = GetPiece(start);

        if (piece.visibility != null)
            return piece.visibility;

        HashSet<GameObject> visible = new HashSet<GameObject>();

        // Explore all the sockets
        for (int i = 0; i < piece.sockets.Length; i++)
        {
            // Clear pieces placed so we have a path unique to this socket
            MazeGenerator.instance.wipePiecesPlacedAndAddNewParent(piece);
            GetVisiblePieces(piece, i, ref visible);
        }
        // Store the visibility for later
        piece.visibility = visible;

        return visible;
    }

    /// <summary>
    /// Helper function to traverse the pieces
    /// </summary>
    /// <param name="piece">The origin piece</param>
    /// <param name="dir">The starting direction of the search</param>
    /// <param name="visible">A list to be updated when we go along the path</param>
    private void GetVisiblePieces(Piece piece, int dir, ref HashSet<GameObject> visible)
    {
        SocketHandler socket = piece.GetSocket(dir);

        while (!isWall(socket))
        {
            // if the socket on the current piece, in the same direction, is open
            if (socket.isOpen())
            {
                MazeParameterManager.JunctionData data = null;

                if (MazeParameterManager.instance.shouldUseExplicitOrdering)
                    data = MazeParameterManager.instance.getPiece(piece.id + 1);

                MazeGenerator.instance.SpawnPiece(socket, data);
            }

            visible.Add(piece.Center);

            // Add adjacent pieces along visible path
            AddBufferPiece(piece, dir, ref visible);

            // Get the piece connected to the current direction
            piece = GetPiece(piece.GetDirection(dir));
            socket = piece.GetSocket(dir);
        }

        visible.Add(piece.Center);

        AddBufferPiece(piece, dir, ref visible);
    }

    /// <summary>
    /// Returns if the given socket is a wall
    /// </summary>
    /// <param name="socket">The socket to be checked for</param>
    /// <returns></returns>
    private bool isWall(SocketHandler socket)
    {
        return socket == null || (!socket.isOpen() && socket.connectedSocket == null);
    }

    /// <summary>
    /// Add the other pieces of the final piece we arrive 
    /// </summary>
    /// <param name="piece">The piece to add buffers to</param>
    /// <param name="dir">The direction we are currently going</param>
    /// <param name="visible">The list of visible pieces to add to</param>
    private void AddBufferPiece(Piece piece, int dir, ref HashSet<GameObject> visible)
    {
        for (int i = 0; i < piece.sockets.Length; i++)
        {
            // skip the direction we are going or the direction we came from
            if (i == dir || i == piece.OppositeSocket(dir))
                continue;

            AddBufferPieceHelper(piece, i, ref visible);
        }
    }

    /// <summary>
    /// Helper function to add a buffer piece
    /// </summary>
    /// <param name="piece">The piece to add the buffer to</param>
    /// <param name="dir">The socket to add the buffer to</param>
    /// <param name="visible">The list of visible pieces</param>
    private void AddBufferPieceHelper(Piece piece, int dir, ref HashSet<GameObject> visible)
    {
        GameObject directionPiece = piece.GetDirection(dir);

        // Add the piece in the given direction to the set
        if (directionPiece)
            visible.Add(directionPiece);
    }
    #endregion

    /// <summary>
    /// Given a start and end, find the general direction of the traversal
    /// </summary>
    /// <param name="startPiece">The origin piece</param>
    /// <param name="endPosition">The destination position</param>
    /// <returns>The direction from the start piece to end piece based on the number of sockets on the start piece</returns>
    private int GetDirection(Piece startPiece, Vector3 endPosition)
    {
        int socketCount = startPiece.sockets.Length;
        Vector3 difference = (endPosition - startPiece.Center.transform.position);

        // flip x and z in atan2 since we consider -z as position 0
        float dir = Mathf.Atan2(difference.x, difference.z);

        // Convert (-PI, PI) => (0, 2PI) => (0, 1) => (0, n), where n is the number of sockets
        dir = (dir + Mathf.PI) / (2 * Mathf.PI) * socketCount % socketCount;

        int rDir = Mathf.RoundToInt(dir);

        // If we are inbetween sockets, check which one is possible
        if (Approximately(Mathf.Abs(dir - rDir), 0.5f, 0.1f))
        {
            if (startPiece.GetDirection(Mathf.FloorToInt(dir)))
                return Mathf.FloorToInt(dir);
            return Mathf.CeilToInt(dir);
        }

        return rDir;
    }

    /// <summary>
    /// Driver function to call GetDirection using positions instead of GameObjects
    /// </summary>
    /// <param name="startPiece">The piece to start the search from</param>
    /// <param name="endPiece">The piece we end the search on</param>
    /// <returns>The direction from the start piece to end piece based on the number of sockets on the start piece</returns>
    private int GetDirection(Piece startPiece, GameObject endPiece)
    {
        return GetDirection(startPiece, endPiece.transform.position);
    }

    /// <summary>
    /// Checks if two floats are approximately equal given some tolerance.
    /// </summary>
    private bool Approximately(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) < tolerance;
    }

    /// <summary>
    /// Given an end piece, get all the pieces along the way, starting from the current piece
    /// </summary>
    /// <param name="endPiece">The destination piece</param>
    /// <returns>The list of objects encountered when looking for a piece</returns>
    public HashSet<GameObject> FindPiece(GameObject endPiece)
    {
        HashSet<GameObject> piecesFound = new HashSet<GameObject>();
        Piece curPiece = currentPiece;

        int dir = GetDirection(currentPiece, endPiece);

        // Keep going until we find the piece or go out of bounds
        while (curPiece != null && !CheckConnectedPieces(curPiece, endPiece, dir))
        {
            // On the way to our destination, we will step on/off the piece
            curPiece.StepOnPiece(curPiece.OppositeSocket(dir));
            curPiece.StepOffPiece(dir);

            piecesFound.Add(curPiece.Center);

            // Goes to the next piece in the given direction
            curPiece = GetPiece(curPiece.GetDirection(dir));
        }

        // There was an error when searching for the piece
        if (curPiece == null) return null;

        StepOnEndPiece(curPiece, GetPiece(endPiece), dir);

        piecesFound.Add(endPiece);
        return piecesFound;
    }

    /// <summary>
    /// Helper function of FindPiece to step on the last piece after we find it
    /// </summary>
    private void StepOnEndPiece(Piece curPiece, Piece endPiece, int dir)
    {
        // We landed on the end piece
        if (curPiece == endPiece)
        {
            endPiece.StepOnPiece(curPiece.OppositeSocket(dir));
        }

        // The end piece is connected to the current piece
        else
        {
            endPiece.StepOnPiece(curPiece.Center.transform.position);

            curPiece.StepOnPiece(curPiece.OppositeSocket(dir));
            curPiece.StepOffPiece(endPiece.Center.transform.position);
        }

        if (endPiece.junctionType == JunctionTypes.End)
        {
            endPiece.StepOffPiece(dir);
        }
    }

    /// <summary>
    /// Checks to see if another specific piece is connected to it
    /// </summary>
    /// <param name="piece">The piece to check the connections of</param>
    /// <param name="lookingFor">The piece to search for</param>
    /// <returns></returns>
    private bool CheckConnectedPieces(Piece piece, GameObject lookingFor, int dir)
    {
        if (piece.Center == lookingFor) return true;

        for (int i = 0; i < piece.sockets.Length; i++)
        {
            // Don't check where we came from or where we are going
            if (i == dir || i == piece.OppositeSocket(dir))
                continue;

            if (piece.GetDirection(i) == lookingFor)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Steps on a piece at the given location
    /// </summary>
    /// <param name="endPiece">The piece being stepped on</param>
    public void StepOnPiece(GameObject endPiece)
    {
        if (endPiece is null) throw new Exception("End piece is null");
        if (currentPiece?.Center == endPiece) return;

        HashSet<GameObject> steppedOnPieces = GetSteppedOnPieces(endPiece);

        // Update the visibility of each piecee
        foreach (GameObject piece in steppedOnPieces)
            GetVisiblePieces(piece);

        // Update what the user actually sees
        UpdateVisiblePieces(GetPiece(endPiece));

        // All the pieces have now been stepped on
        UpdateEverSteppedOn(steppedOnPieces);
        SetCurrentPiece(endPiece);

        Piece pieceRef = GetPiece(endPiece);

        //Im scared, but what happens 
        if (!pieceRef.isBuffer)
        {
            MazeStack?.removePieceFromRemaining(pieceRef.id);
        }

        pieceRef.lastTimeSteppedOn = Time.time;
    }

    /// <summary>
    /// Finds all the pieces that have been stepped on
    /// </summary>
    /// <param name="endPiece">The destination piece</param>
    /// <returns>A HashSet containing all stepped on pieces</returns>
    private HashSet<GameObject> GetSteppedOnPieces(GameObject endPiece)
    {
        HashSet<GameObject> steppedOnPieces;

        if (visiblePieces.Contains(endPiece))
        {
            steppedOnPieces = FindPiece(endPiece);
        }
        else
        {
            // we didn't find the piece, so the piece is all alone somewhere
            steppedOnPieces = new HashSet<GameObject>();
            steppedOnPieces.Add(endPiece);
        }

        return steppedOnPieces;
    }

    /// <summary>
    /// Updates the boolean on each piece indicating if it was ever stepped on
    /// </summary>
    /// <param name="steppedOnPieces">The set of pieces to change</param>
    private void UpdateEverSteppedOn(HashSet<GameObject> steppedOnPieces)
    {
        foreach (GameObject piece in steppedOnPieces)
            GetPiece(piece).hasEverBeenSteppedOn = true;
    }

    /// <summary>
    /// Converts the maze into a JSON file
    /// </summary>
    [EasyButtons.Button]
    public void MazeToJSON()
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> {  new PieceConverter(),
                                                    new PieceDictionaryConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        string json = JsonConvert.SerializeObject(spawnedPieces, settings);
        System.IO.File.WriteAllText(CsvManager.instance.getCurrentExperimentPath() + "/ExportedMaze.json", json);
    }

    /// <summary>
    /// Takes a JSON file and reconstructs a maze
    /// </summary>
    [EasyButtons.Button]
    public void JSONToMaze(string pathToJSON)
    {
        string json = System.IO.File.ReadAllText(pathToJSON);

        Dictionary<int, ReconstructedPiece> pieces = JsonConvert.DeserializeObject<Dictionary<int, ReconstructedPiece>>(json);

        // Null sockets are represented as "-1" in the dictionary
        pieces.Add(-1, null);

        StartCoroutine(SpawnMaze(pieces));
    }

    /// <summary>
    /// Allows us to wait for the triggers to fire off on each piece. Otherwise, opacity is overwritten
    /// </summary>
    /// <param name="pieces">Dictionary of pieces from a deserialzed JSON file</param>
    /// <returns></returns>
    private IEnumerator SpawnMaze(Dictionary<int, ReconstructedPiece> pieces)
    {
        Piece startPiece = GetPiece(MazeGenerator.instance.PlaceStarter());
        startPiece.Center.GetComponent<StartPieceScript>().SetupStart();

        // The visualization of where the player is. Also helps with camera tracking
        userReplay = Instantiate(userPrefabToSpawn, startPiece.Center.transform.position, startPiece.Center.transform.rotation);
        userReplay.tag = "UserReplay";
        userPath = new List<Piece>();

        // Spawn the maze in each direction, starting from from the start piece
        SpawnMaze(pieces, pieces[0], startPiece);

        yield return new WaitForFixedUpdate();

        StepOnPiece(startPiece.Center);
    }

    /// <summary>
    /// DFS recreation of a maze
    /// </summary>
    /// <param name="pieces">A dictionary of all pieces extracted from a JSON file</param>
    /// <param name="pieceInfo">The data of the current piece</param>
    /// <param name="piece">The current piece</param>
    private void SpawnMaze(Dictionary<int, ReconstructedPiece> pieces, ReconstructedPiece pieceInfo, Piece piece)
    {
        if (piece == null) return;

        // Add the user's path mark
        if (pieceInfo.exitDirection != -1)
        {
            piece.EnableMark(pieceInfo.exitDirection, true);
            userPath.Add(piece);
        }

        pieceInfo.visited = true;

        // Check all directions
        for (int i = 0; i < piece.sockets.Length; i++)
        {
            ReconstructedPiece nextPiece = pieces[pieceInfo.connections[i]];
            if (nextPiece == null || nextPiece.visited) continue;

            MazeParameterManager.JunctionData data = new MazeParameterManager.JunctionData(0, nextPiece.currentJunction, 0);
            Piece spawnedPiece = GetPiece(MazeGenerator.instance.SpawnPiece(piece.sockets[i], data));
            SpawnMaze(pieces, nextPiece, spawnedPiece);
        }
    }

    /// <summary>
    /// Move the researcher one step forward in the user's recorded path
    /// </summary>
    [EasyButtons.Button]
    public void UserPathStepForward()
    {
        userPathIndex = Math.Min(userPathIndex + 1, userPath.Count - 1);

        StepOnPiece(userPath[userPathIndex].Center);
        userReplay.transform.position = currentPiece.Center.transform.position;
    }

    /// <summary>
    /// Move the researcher one step backward in the user's recorded path
    /// </summary>
    [EasyButtons.Button]
    public void UserPathStepBackward()
    {
        userPathIndex = Math.Max(userPathIndex - 1, 0);

        StepOnPiece(userPath[userPathIndex].Center);
        userReplay.transform.position = currentPiece.Center.transform.position;
    }

    /// <summary>
    /// Move the researcher to the given step in the user's recorded path
    /// </summary>
    [EasyButtons.Button]
    public void UserPathStepToPosition(int position)
    {
        position = Math.Max(position, 0);
        userPathIndex = Math.Min(position, userPath.Count - 1);

        StepOnPiece(userPath[userPathIndex].Center);
        userReplay.transform.position = currentPiece.Center.transform.position;
    }

    /// <summary>
    /// Useful for seeing the heatmap
    /// </summary>
    [EasyButtons.Button]
    public void TurnOffAllPieces()
    {
        foreach (KeyValuePair<int, Piece> piece in spawnedPieces)
        {
            piece.Value.SetVisibilty(false);
        }
    }

    /// <summary>
    /// Organized collection of information needed to recreate the maze
    /// </summary>
    class ReconstructedPiece
    {
        /// <summary>
        /// The junction type of this piece
        /// </summary>
        public JunctionTypes currentJunction;
        /// <summary>
        /// A list of what pieces (IDs) this piece is connected to
        /// </summary>
        public int[] connections;
        /// <summary>
        /// Where the user exited this piece from
        /// </summary>
        public int exitDirection;
        /// <summary>
        /// A boolean of whether or not this piece was visited (for DFS)
        /// </summary>
        public bool visited = false;

        public ReconstructedPiece(JunctionTypes currentJunction, int[] connections, int exitDirection)
        {
            this.currentJunction = currentJunction;
            this.connections = connections;
            this.exitDirection = exitDirection;
        }

        public override string ToString()
        {
            string str = currentJunction + "\nStepped On: " + (exitDirection != -1 ? "Yes" : "No") + "\n\n";

            for (int i = 0; i < connections.Length; i++)
                str += i + ": " + connections[i] + "\n";

            return str;
        }
    }


    // JSON converters
    /// <summary>
    /// Converts a dictionary of <int, Piece> into JSON and back into a dictionary of <int, ReconstructedPiece>
    /// </summary>
    class PieceDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<int, Piece>);
        }

        // reads in the data as reconstructed pieces
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            Dictionary<int, ReconstructedPiece> pieces = new Dictionary<int, ReconstructedPiece>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");

                var key = (int)Convert.ChangeType(reader.Value, typeof(int));

                reader.Read();
                var value = serializer.Deserialize<ReconstructedPiece>(reader);

                pieces.Add(key, value);
            }

            return pieces;
        }

        // only need this to make the start piece have an ID of 0 for looking up later
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Dictionary<int, Piece> pieces = (Dictionary<int, Piece>)value;

            writer.WriteStartObject();

            foreach (var pair in pieces)
            {
                StartPieceScript script;
                int key;

                if (pair.Value.Center.TryGetComponent(out script))
                    key = 0;
                else
                    key = pair.Key;

                writer.WritePropertyName(key.ToString());
                serializer.Serialize(writer, pair.Value);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Converts a piece to JSON and from JSON to a ReconstructedPiece
    /// </summary>
    class PieceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Piece) || objectType == typeof(ReconstructedPiece);
        }

        // reads in the value into the organized class, ReconstructedPiece
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);

            JunctionTypes junctionType = (JunctionTypes)JsonConvert.DeserializeObject<int>(jsonObject["junctionType"].ToString());
            int[] sockets = JsonConvert.DeserializeObject<int[]>(jsonObject["sockets"].ToString());
            int exitSocket = JsonConvert.DeserializeObject<int>(jsonObject["exitSocket"].ToString());

            ReconstructedPiece piece = new ReconstructedPiece(junctionType, sockets, exitSocket);

            return piece;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Piece piece = (Piece)value;
            JObject json = new JObject();

            json["junctionType"] = JToken.FromObject((int)piece.junctionType);
            json["sockets"] = JToken.FromObject(CreateSocketArray(piece.sockets));
            json["exitSocket"] = JToken.FromObject(piece.exitDirection);

            json.WriteTo(writer);
        }

        // Turns the piece socket array into an int array, with the values being the ID of the piece its connected to
        private int[] CreateSocketArray(SocketHandler[] oldSockets)
        {
            int[] sockets = new int[oldSockets.Length];
            for (int i = 0; i < sockets.Length; i++)
            {
                int id = -1;

                if (oldSockets[i].connectedSocket != null)
                {
                    StartPieceScript script;

                    if (oldSockets[i].connectedSocket.gridPiece.TryGetComponent(out script))
                        id = 0;
                    else
                        id = oldSockets[i].connectedSocket.gridPiece.GetInstanceID();
                }

                sockets[i] = id;
            }

            return sockets;
        }
    }
}


