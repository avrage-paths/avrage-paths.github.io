using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A very simple data class used as an intermediate when going to or from a CSV
///  Note that all the fields must be public for reflection to work as intended
/// </summary>
public class IntermediateParameterStorage
{
    /// <summary>
    /// dateTime for bookkeeping 
    /// </summary>

    /// <summary>
    /// All maze parameters
    /// </summary>
    public string dateTime;
    public int distance;
    public string dataRep;
    public string FourWays;
    public string ThreeWayLeftRights;
    public string ThreeWayLeftStraights;
    public string ThreeWayRightStraights;
    public string LeftTurns;
    public string RightTurns;
    public string Straights;
    public int bufferStraights;
    public int maxStraightsInRow;
    public int minStraightsInRow;
    public float wallHeight;
    public string wallTexture;
    public string floorTexture;
    public string ceilingTexture;
    public MovementTypes locomotionMethod;
    public bool shouldUseCeiling;
    public bool useExplicitOrdering;

    /// <summary>
    /// Empty constructor necessary to make an intial reference for the CSV manager to populate
    /// </summary>
    public IntermediateParameterStorage()
    {

    }

    /// <summary>
    /// A constructor for when we are going to save the data
    /// </summary>
    public IntermediateParameterStorage(int distance, MazeDataController.DataRepresentations dataRep, Dictionary<JunctionTypes, JunctionValues> junctions, int maxStraights, int minStraights,
        float wallHeight, string wallTexture, string floorTexture, string ceilingTexture, MovementTypes locomotionMethod, bool shouldUseCeiling, int bufferStraights, bool useExplicitOrdering)
    {

        this.dateTime = System.DateTime.Now.ToString("MM/dd/yyyy h:mm tt");

        this.distance = distance;

        this.dataRep = System.Enum.GetName(typeof(MazeDataController.DataRepresentations), dataRep);

        this.FourWays = junctions[JunctionTypes.FourWay].getCurrentValue(dataRep);

        this.ThreeWayLeftRights = junctions[JunctionTypes.ThreeWayLeftRight].getCurrentValue(dataRep);

        this.ThreeWayLeftStraights = junctions[JunctionTypes.ThreeWayLeftStraight].getCurrentValue(dataRep);

        this.ThreeWayRightStraights = junctions[JunctionTypes.ThreeWayRightStraight].getCurrentValue(dataRep);

        this.LeftTurns = junctions[JunctionTypes.LeftTurn].getCurrentValue(dataRep);

        this.RightTurns = junctions[JunctionTypes.RightTurn].getCurrentValue(dataRep);

        this.Straights = junctions[JunctionTypes.Straight].getCurrentValue(dataRep);

        this.maxStraightsInRow = maxStraights;

        this.minStraightsInRow = minStraights;

        this.wallHeight = wallHeight;

        this.wallTexture = wallTexture;

        this.floorTexture = floorTexture;

        this.ceilingTexture = ceilingTexture;

        this.locomotionMethod = locomotionMethod;

        this.shouldUseCeiling = shouldUseCeiling;

        this.bufferStraights = bufferStraights;

        this.useExplicitOrdering = useExplicitOrdering;

    }
}
