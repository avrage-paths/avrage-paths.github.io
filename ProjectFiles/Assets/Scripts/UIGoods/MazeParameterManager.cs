using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//This game object will be Initialized by the MazeDataController which works as a backend for the UI 
/// <summary>
/// The object to be passed to the maze to use during generation
/// </summary>
public class MazeParameterManager : MonoBehaviour
{
    /// <summary>
    /// The singleton instance
    /// </summary>
    public static MazeParameterManager instance;

    /// <summary>
    /// The stack thats used to influence piece ordering and distribution,
    /// essentially ties to whats visible to the participant
    /// </summary>
    private Stack<JunctionData> juncStack = new Stack<JunctionData>();
    public int mazeDistance = 0;

    /// <summary>
    /// Simple dictionary to give us 0(1) lookup for preventing duplicates from entering the stack
    /// Simply maps the ID to true or false on whether its present in the stack 
    /// </summary>
    private Dictionary<int, bool> stackIDLookup = new Dictionary<int, bool>();

    /// <summary>
    /// A dictionary representing all of the pieces the user has yet to physically traverse
    /// used to ensure path lengths and distribution is kept when the stack gets to short 
    /// Maps piece id to a tuple containing (the junction type, the number of straights that follows this junction)
    /// </summary>
    private Dictionary<int, (JunctionTypes JuncType, int NumStraights)> remainingPieces = new Dictionary<int, (JunctionTypes, int)>();


    Dictionary<int, JunctionData> explicitOrdering = new Dictionary<int, JunctionData>(); 

    /// <summary>
    /// Parameter that controls the height of the walls in the maze
    /// </summary>
    public float wallHeight;

    /// <summary>
    /// The locomotion method used by the participant
    /// </summary>
    public MovementTypes movementType;

    /// <summary>
    /// Bool to determine whether or not the maze should spawn with ceilings
    /// </summary>
    public bool shouldUseCeiling;

    /// <summary>
    /// Int to determine the number of connecting straights spawned concurrently
    /// </summary>
    public int minStraightsInARow;

    /// <summary>
    /// Simple flag for whether the maze should use explicit ordering
    /// </summary>
    public bool shouldUseExplicitOrdering; 

    public string experimentName, conditionName, participantID;

    //Handle the singleton so we don't need to pass a reference 
    public void Awake()
    {

        //If theres already a csv manager
        if (instance != null)
        {
            //delete the old one 
            Destroy(instance.gameObject);

        }

        //make the new one 
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// A makeshift constructor for a mono-behavior, will update all the infomation for the class
    /// </summary>
    /// <param name="distance">The total distance of the maze</param>
    /// <param name="maxStraightsInARow">The max straights in a row</param>
    /// <param name="junctions">The junction distribution</param>
    public void InitializeOrOverWrite(int distance, int maxStraightsInARow, int minStraightsInARow, Dictionary<JunctionTypes, int> junctions, float wallHeight, MovementTypes movementType, bool shouldUseCeiling, bool shouldUseExplicitOrdering)
    {
        //We are passed a list of all numbered pieces so lets make the stack based off them. 
        this.wallHeight = wallHeight;
        this.movementType = movementType;
        this.shouldUseCeiling = shouldUseCeiling;
        this.minStraightsInARow = minStraightsInARow;
        this.shouldUseExplicitOrdering = shouldUseExplicitOrdering;
        Debug.Log("Use explicit ordering is " + this.shouldUseExplicitOrdering);
        juncStack.Clear();
        stackIDLookup.Clear();
        remainingPieces.Clear();
        mazeDistance = distance;
        populateStack(junctions, distance, maxStraightsInARow, minStraightsInARow);
    }


    public void storeListForExplicit(List<JunctionData> junctions)
    {
        foreach (JunctionData junction in junctions)
            this.explicitOrdering[junction.id] = junction;
    }

    public JunctionData getPiece(int nextPieceId)
    {
        if (nextPieceId <= 0)
            nextPieceId = 1; 

        if (!this.explicitOrdering.ContainsKey(nextPieceId))
        {
            Debug.LogError("We did not find a piece with id " + nextPieceId + " the only keys we have are ");
            foreach (int key in explicitOrdering.Keys)
                Debug.LogError("Key " + key);
            return new JunctionData(-1, JunctionTypes.End, 0);
        }
        else
            return this.explicitOrdering[nextPieceId];
    }

    /// <summary>
    /// Used to add a piece back onto the stack. Ensures no duplicates
    /// </summary>
    /// <param name="id">The id of the piece were adding back on</param>
    /// <param name="junc">The type of piece</param>
    public void addToStack(int id, JunctionTypes junc, int numStraights)
    {

        if (stackIDLookup.ContainsKey(id) || !remainingPieces.ContainsKey(id))
        {
            //This is a duplicate, so ignore
            return;
        }


        juncStack.Push(new JunctionData(id, junc, numStraights));
        stackIDLookup.Add(id, true);
    }

    /// <summary>
    /// Gets the next piece for the maze to generate from the stack. 
    /// If the stack is empty but we still have pieces remaining, spawn from those instead
    /// </summary>
    /// <returns></returns>
    public JunctionData getNextPiece()
    {
        if (juncStack.Count == 0)
            return new JunctionData(-1, JunctionTypes.End, 0);

        JunctionData junc = juncStack.Pop();

        stackIDLookup.Remove(junc.id);

        //Check to see if it was a duplicate, and if it was go until there are no more 
        while (!remainingPieces.ContainsKey(junc.id))
        {

            if (juncStack.Count == 0)
                return new JunctionData(-1, JunctionTypes.End, 0);

            junc = juncStack.Pop();

            stackIDLookup.Remove(junc.id);
        }

        return junc;
    }


    /// <summary>
    /// Reference to the data container for telemetry
    /// </summary>
    public class JunctionData : TelemetryManager.DataContainer
    {
        public JunctionTypes junctionType;
        public int numStraights;
        public int id;

        public JunctionData()
        {

        }

        public JunctionData(int id, JunctionTypes junc, int numStraights)
        {
            this.id = id;
            this.junctionType = junc;
            this.numStraights = numStraights;
        }
    }

    /// <summary>
    /// Write the stack to telemerty manager to eventually be stored to file
    /// </summary>
    public void RecordStack()
    {
        JunctionData[] juncArray = stackToJuncDataArray(); 
        foreach (JunctionData junction in juncArray)
        {
            TelemetryManager.instance.RecordData(junction, "MazeStack");
        }
    }

    public JunctionData[] stackToJuncDataArray()
    {
        JunctionData[] juncArray = new JunctionData[juncStack.Count];
        int count = 0;
        foreach (JunctionData curJunc in juncStack)
        {
            Debug.Log("Looking at piece " + curJunc);
            juncArray[count] = new JunctionData(curJunc.id, curJunc.junctionType, curJunc.numStraights);
            count++;
        }

        Array.Reverse(juncArray);

        return juncArray;
    }

    /// <summary>
    /// Helper function to non-destructively print the contents of the stack 
    /// </summary>
    /// <param name="juncStack">The stack of junctions to be printed</param>
    /// <returns></returns>
    private string printResultingStack(Stack<JunctionData> juncStack)
    {
        string stackReturn = "";

        Debug.Log("Printing the stack!");

        foreach (var junction in juncStack)
        {
            JunctionTypes junctionTy = junction.junctionType;

            string res = junction.junctionType.ToString() + " " + junction.id + " with straights following it: " + junction.numStraights +"\n";
            stackReturn += res;
            Debug.Log(res);

        }
        return stackReturn;
    }


    /// <summary>
    /// When the stack gets to small, we need to call upon another data object to ensure that path lengths and distributions are met. 
    /// This function returns a random piece from the remaining pieces object, in other words, a piece that hasn't yet been stepped on by the participant
    /// </summary>
    /// <param name="piecesPlacedOnCurrentPath">The pieces including the parent junction, placed along a specific socket</param>
    /// <returns>A tuple with the first value being the pieces, id, and the second value being the junctions type</returns>
    public JunctionData getNextAvailablePiece(Dictionary<int, JunctionTypes> piecesPlacedOnCurrentPath)
    {
        //We just need to get a piece from remaining pieces that isn't in piecesPlacedOnCurrentPath
        //We could use except, but we only care about one value, so lets implement our own so we can early terminate 
        //Remaining pieces should be pretty small by the time this is reached so lets just iterate through it 

        bool focusPerformance = true;

        if (focusPerformance)
        {
            foreach (var junc in remainingPieces)
            {
                //Make sure we haven't placed the piece and its not the spawn junction
                if (!piecesPlacedOnCurrentPath.ContainsKey(junc.Key))
                {


                    //we found a piece we haven't placed yet. 
                    return new JunctionData(junc.Key, junc.Value.JuncType, junc.Value.NumStraights);
                }
            }
        }
        else
        {
            //We instead want to focus a wider variety of paths 
            List<int> keys = new List<int>(remainingPieces.Keys);
            int startingIndex = Random.Range(0, keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                startingIndex += i;
                if (keys.Count - 1 == 0)
                {
                    return new JunctionData(keys[0], remainingPieces[keys[0]].JuncType, remainingPieces[keys[0]].NumStraights);
                }

                startingIndex = startingIndex % (keys.Count);


                //Make sure we haven't placed the piece and its not the spawn junction
                if (!piecesPlacedOnCurrentPath.ContainsKey(keys[startingIndex]))
                {


                    //we found a piece we haven't placed yet. 
                    return new JunctionData(keys[startingIndex], remainingPieces[keys[startingIndex]].JuncType, remainingPieces[keys[startingIndex]].NumStraights);
                }

            }
        }

        //We should never hit this 
        Debug.LogError("We were unable to find a remaining piece that we haven't yet placed!");
        return new JunctionData(-1, JunctionTypes.End, 0);
    }

    //Function to check if the path terminated when we still have remaining pieces 
    //This will only be called when the stack is empty 
    /// <summary>
    /// Function to check if we should override the end piece with a piece from remaining pieces
    /// </summary>
    /// <param name="piecesPlaced">An integer that represents the number of pieces placed along the path connected to a specific socket</param>
    /// <returns>true if we should replace the end piece, false otherwise</returns>
    public bool shouldUpdatePieceType(int piecesPlaced)
    {

        //We're trying to place an end when we haven't used all the remaining pieces 
        if (piecesPlaced < remainingPieces.Count)
        {
            return true;
        }
        else
            return false;
    }


    /// <summary>
    /// Function to write the stack to persistentDataPath
    /// </summary>
    public void saveStack()
    {
        string res = printResultingStack(juncStack);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/InitialStack.txt", res);
    }

    /// <summary>
    /// This function takes in the distribution and the maze parameters and forms the stack to be used by the maze generation 
    /// It will exclude the connecting straights from the stack as each junction will know how many straights follow it 
    /// </summary>
    /// <param name="junctions">The distribution of junctions</param>
    /// <param name="distance">The total distance of the maze</param>
    /// <param name="maxStraightsInARow">The max number of straights that can be placed concurrently on the stack</param>
    /// <param name="minStraightsInARow">The min number of straights that can be placed concurrently on the stack</param>
    private void populateStack(Dictionary<JunctionTypes, int> junctions, int distance, int maxStraightsInARow, int minStraightsInARow)
    {
        //This will be our list of pieces to pick and choose from 
        List<JunctionTypes> megaList = new List<JunctionTypes>();

        //If they only want a maze of length one, just give em a stack with a single straight
        if (distance == 1)
        {
            juncStack.Push(new JunctionData(1, JunctionTypes.Straight, 0));
            remainingPieces.Add(1, (JunctionTypes.Straight, 0));
            return; 
        }

        //Lets first handle all of the straights 
        //Each junction will be assinged a value for the number of straights attached to the end of it 
        int numJunc = getNumJunctions(junctions);

        if (numJunc < 1)
        {
            //Special case with all straights 
            int StraightIdCount = 1; 
            for (int i = 0; i < junctions[JunctionTypes.Straight]; i++)
            {
                juncStack.Push(new JunctionData(StraightIdCount, JunctionTypes.Straight, 0));
                remainingPieces.Add(StraightIdCount, (JunctionTypes.Straight, 0));
                StraightIdCount++;
            }

            return; 
        }

        //Get the buckets of straights to assign to the junctions 
        List<int> straights = getBucketsOfStraights(numJunc, junctions[JunctionTypes.Straight], minStraightsInARow, maxStraightsInARow);


        foreach (int straightValue in straights)

        //Shuffle the list for better random
        for (int i = 0; i < megaList.Count; i++)
        {
            int temp = straights[i];
            int randomIndex = UnityEngine.Random.Range(0, straights.Count);
            straights[i] = straights[randomIndex];
            straights[randomIndex] = temp;
        }

        //Set the value of our straights to 0 since now they've been accounted for 
        junctions[JunctionTypes.Straight] = 0;

        //Now lets form a list containing all of the pieces so we can decide a random ordering for the stack 
        //Go through the junction values
        foreach (var junction in junctions)
        {
            //Add them to a list to essentially form a weighted list 
            for (int i = 0; i < junction.Value; i++)
                megaList.Add(junction.Key);
        }

        //Lets get better random behavior and shuffle the list 
        for (int i = 0; i < megaList.Count; i++)
        {
            JunctionTypes temp = megaList[i];
            int randomIndex = Random.Range(0, megaList.Count);
            megaList[i] = megaList[randomIndex];
            megaList[randomIndex] = temp;
        }

        int idCount = 1;
        while (megaList.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, megaList.Count);
            juncStack.Push(new JunctionData(idCount, megaList[index], straights[0]));
            remainingPieces.Add(idCount, (megaList[index], straights[0]));
            straights.RemoveAt(0); 
            idCount++;
            megaList.RemoveAt(index);
        }


    }

    /// <summary>
    /// This functions will take divide the straights amongst the number of junctions such that each junction 
    /// will have a value of straights between our min and our max 
    /// This functions also doesn't error check and assume the straights are possible to distribute with the given parameters 
    /// </summary>
    /// <param name="numJunctions">The number of junctions in the maze</param>
    /// <param name="numStraights">The number of straights to divide up</param>
    /// <param name="minStraightsInARow">The minimum number of straights that can be assigned to any junction</param>
    /// <param name="maxStraightsInARow">The maximum number of straights that can be assigned to any junction</param>
    /// <returns>A list wth numJunction amount of ints, where each int is between the min and max but sums to numStraights</returns>
    private List<int> getBucketsOfStraights(int numJunctions, int numStraights, int minStraightsInARow, int maxStraightsInARow)
    {

        System.Random rand = new System.Random();
        List<int> buckets = new List<int>(new int[numJunctions]);
        int remainingSum = numStraights;

        for (int i = 0; i < numJunctions - 1; i++)
        {
            int minValue = Math.Max(minStraightsInARow, remainingSum - maxStraightsInARow * (numJunctions - i - 1));
            int maxValue = Math.Min(maxStraightsInARow, remainingSum - minStraightsInARow * (numJunctions - i - 1));

            int value = rand.Next(minValue, maxValue + 1);
            buckets[i] = value;
            remainingSum -= value;
        }

        buckets[numJunctions - 1] = remainingSum;
        return buckets;

    }


    /// <summary>
    /// Function to remove a piece from the remaining pieces after a participant steps on it 
    /// </summary>
    /// <param name="id">The id of the piece to be removed</param>
    public void removePieceFromRemaining(int id)
    {
        //Start piece and connecting straights default to 0, and end piece -1 we don't want to include them. 
        //StepOnPiece is called twice as a result of the telportation case handling, so prevent a piece from being "removed" twice. 
        if (id <= 0 || !remainingPieces.ContainsKey(id))
            return;



        remainingPieces.Remove(id);
    }

    /// <summary>
    /// Function to add a piece back to our list of remaining pieces in the event the participant back tracks
    /// </summary>
    /// <param name="id">The id of the piece to be added to ensure no duplicates</param>
    /// <param name="junc">The type of junction to be added</param>
    public void addPieceToRemaining(int id, JunctionTypes junc, int numStraights)
    {
        //Start piece and connecting straights default to 0, we don't want to include them. 
        if (remainingPieces.ContainsKey(id) || id <= 0)
            //Don't think this will get called, but lets ensure we avoid dupes 
            return;

        remainingPieces.Add(id, (junc, numStraights));
    }

    /// <summary>
    /// Function to get the number of connecting straights required for the maze
    /// </summary>
    /// <param name="junctions">The distribution of junctions in the maze</param>
    /// <returns>The number of connecting straights (straight pieces between junctions) for the maze</returns>
    public int getNumJunctions(Dictionary<JunctionTypes, int> junctions)
    {
        //This is currently assuming we don't care if a straight or a junction is connected directly to the end. 
        //Currently, just every piece that is not a straight will have a connecting straight 
        int count = 0;
        foreach (var junc in junctions)
        {
            if (junc.Key != JunctionTypes.Straight)
            {

                count += junc.Value;
            }
        }
        return count;
    }
}