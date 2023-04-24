using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valve.VR.InteractionSystem;


/// <summary>
/// Backend for the UI to take in user input to eventually make the maze from 
/// </summary>
public class MazeDataController : MonoBehaviour
{
    [SerializeField]
    private Material floor;
    [SerializeField]
    private Material wall;
    [SerializeField]
    private Material ceiling;

    public class ChangeMaterials : MonoBehaviour
    {
        /// <summary>
        /// The floor texture to set
        /// </summary>
        private Texture floorTex;

        /// <summary>
        /// The wall texture to set
        /// </summary>
        private Texture wallTex;

        /// <summary>
        /// The ceiling texture to set
        /// </summary>
        private Texture ceilingTex;

        /// <summary>
        /// Set the floor to be the given texture
        /// </summary>
        /// <param name="tex">The texture to set</param>
        public void SetFloorTexture(Texture tex)
        {
            if (tex == null)
                Debug.LogError("Setting floor to a null texture!");
            MazeDataController.instance.floor.mainTexture = tex;
        }

        /// <summary>
        /// Set the wall to be the given texture
        /// </summary>
        /// <param name="tex">The texture to set</param>
        public void SetWallTexture(Texture tex)
        {
            MazeDataController.instance.wall.mainTexture = tex;
        }

        /// <summary>
        /// Set the ceiling to be the given texture
        /// </summary>
        /// <param name="tex">The texture to set</param>
        public void SetCeilingTexture(Texture tex)
        {
            MazeDataController.instance.ceiling.mainTexture = tex;
        }

        private void Awake()
        {
            floorTex = MazeDataController.instance.floor.mainTexture;
            wallTex = MazeDataController.instance.wall.mainTexture;
            ceilingTex = MazeDataController.instance.ceiling.mainTexture;
        }

        private void OnDestroy()
        {
            MazeDataController.instance.floor.mainTexture = floorTex;
            MazeDataController.instance.wall.mainTexture = wallTex;
            MazeDataController.instance.ceiling.mainTexture = ceilingTex;
        }
    }

    //Pub-Sub goods for notifying alt text outside of input field 
    /// <summary>
    /// Singelton reference
    /// </summary>
    public static MazeDataController instance;

    /// <summary>
    /// Delegate
    /// </summary>
    public delegate void OnDataChanged();
    public OnDataChanged onDataChanged;

    /// <summary>
    /// Max straight in a row parameter
    /// </summary>
    private int maxStraightsInARow = 2;

    /// <summary>
    /// Min straight in a row parameter
    /// </summary>
    private int minStraightsInARow = 1;

    /// <summary>
    /// The total number of connecting/buffer straights we have 
    /// </summary>
    private int bufferStraights = 0;

    //The condition this maze maps to 
    /// <summary>
    /// The condition these params correspond to
    /// </summary>
    private string condition;

    /// <summary>
    /// Total maze distance parameter
    /// </summary>
    private int distance = 1;

    /// <summary>
    /// The height for the maze walls 
    /// </summary>
    private float wallHeight = 1;

    /// <summary>
    /// Eventual texture for the wall (will probably just be a path to a resource folder) 
    /// </summary>
    private string wallTexture = "";

    /// <summary>
    /// Eventual texture for the floors, should just be a path
    /// </summary>
    private string floorTexture = "";

    /// <summary>
    /// Eventual texture for the ceilings if there are ceilings 
    /// </summary>
    private string ceilingTexture = "";

    /// <summary>
    /// Boolean for whether or not the maze should use ceilings
    /// </summary>
    private bool shouldUseCeiling = false;

    /// <summary>
    /// Eventually will be some locomotion enum? 
    /// </summary>
    private MovementTypes locomotionMethod = MovementTypes.HeadDirectedSteering;

    /// <summary>
    /// Dictionary for tracking junction values
    /// </summary>
    private Dictionary<JunctionTypes, JunctionValues> junctions = new Dictionary<JunctionTypes, JunctionValues>();

    /// <summary>
    /// Dictionary for error checking 
    /// </summary>
    private Dictionary<ErrorTypes, bool> errors = new Dictionary<ErrorTypes, bool>();

    /// <summary>
    /// Allowed data representations for the user to use 
    /// </summary>
    public enum DataRepresentations
    {
        Integer,
        Percentage
    }

    /// <summary>
    /// data representation parameter
    /// </summary>
    private DataRepresentations dataRep = DataRepresentations.Percentage;

    //We need some varaibles to control our distribution of random numbers 
    /// <summary>
    /// unused choice, used for ensuring even distribution when generating piece types
    /// </summary>
    private int unusedChoice = 0;

    /// <summary>
    /// Flag for how to maze should save, load, and how the algorithm will operate 
    /// </summary>
    private bool useExplicitOrdering = false; 

    /// <summary>
    /// range of unused choice, used for ensuring even distribution when generating piece types
    /// </summary>
    private int rangeOfUnusedChoice = 1;

    /// <summary>
    /// Object to handle adjusting the textures for the maze 
    /// </summary>
    private ChangeMaterials mat = new ChangeMaterials();


    public void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of data controller found!!");
            return;
        }

        instance = this;

        Array juncTypes = Enum.GetValues(typeof(JunctionTypes));
        foreach (JunctionTypes juncType in juncTypes)
        {
            this.junctions[juncType] = new JunctionValues(juncType);
        }

        //Initialize all of our error's to false to start. 
        Array errorTypes = Enum.GetValues(typeof(ErrorTypes));
        foreach (ErrorTypes errorType in errorTypes)
        {
            this.errors[errorType] = false;
        }


    }

    /// <summary>
    /// Callback function for our delegate
    /// </summary>
    private void invokeCallback()
    {
        if (onDataChanged != null)
            onDataChanged.Invoke();

    }


    /// <summary>
    /// Reads the intermediate parameter storage in to the data controller. Used for loading maze from condition
    /// </summary>
    /// <param name="data">IntermediateParameterStorage class populated with all the maze parameters for this given condition</param>
    public void paramStorageToDataClass(IntermediateParameterStorage data)
    {
        //Set the distance
        setDistance(data.distance);
        //Set the data rep
        setDataRep(stringToDataRep(data.dataRep));
        //Set Max straights 
        setMaxStraights(data.maxStraightsInRow);
        //Set Min straights 
        setMinStraights(data.minStraightsInRow);
        //Set the buffer straights 
        setBufferStraights(data.bufferStraights);
        //Set wall height 
        setWallHeight(data.wallHeight);
        //Set locomotion method 
        setLocomotionMethod(data.locomotionMethod.ToString());
        //Set floor texture textur
        setFloorTexture(data.floorTexture);
        //Set the wall texture 
        setWallTexture(data.wallTexture);
        //Set the celing texture
        setCeilingTexture(data.ceilingTexture);
        //Set whether or not we should use ceilings 
        setShouldUseCeiling(data.shouldUseCeiling);
        //Set whether we should use explicit ordering 
        setExplicitOrdering(data.useExplicitOrdering); 

        //Set emotion scheme 

        //Set all of our junction values 
        csvToJunctionHelper(data.FourWays, JunctionTypes.FourWay);
        csvToJunctionHelper(data.ThreeWayLeftRights, JunctionTypes.ThreeWayLeftRight);
        csvToJunctionHelper(data.ThreeWayLeftStraights, JunctionTypes.ThreeWayLeftStraight);
        csvToJunctionHelper(data.ThreeWayRightStraights, JunctionTypes.ThreeWayRightStraight);
        csvToJunctionHelper(data.LeftTurns, JunctionTypes.LeftTurn);
        csvToJunctionHelper(data.RightTurns, JunctionTypes.RightTurn);
        csvToJunctionHelper(data.Straights, JunctionTypes.Straight);

    }

    /// <summary>
    /// Simple setter for the number of buffer straights 
    /// </summary>
    /// <param name="numBuffers">An int representing the number of buffer pieces</param>
    public void setBufferStraights(int numBuffers)
    {
        this.bufferStraights = numBuffers;
    }

    /// <summary>
    /// Simple setter for the condition
    /// </summary>
    /// <param name="condtion">A string that represents the current condition</param>
    public void setCondition(string condtion)
    {
        this.condition = condtion;
    }

    /// <summary>
    /// Simple getter for condition
    /// </summary>
    /// <param name="condtion">Returns the condition unique to this maze</param>
    public void getCondition(string condtion)
    {
        this.condition = condtion;
    }

    /// <summary>
    /// Simple setter for explicit ordering
    /// </summary>
    /// <param name="explicitOrdering">Should the maze use explicit ordering</param>
    public void setExplicitOrdering(bool explicitOrdering)
    {
        this.useExplicitOrdering = explicitOrdering;
    }

    /// <summary>
    /// Simple getter for explicit ordering
    /// </summary>
    /// <returns>bools representing if the maze should use explicit ordering</returns>
    public bool getExplicitOrdering()
    {return this.useExplicitOrdering;}

    /// <summary>
    /// Helper function for reading from csv back to data controller. Will set piece to value or random depending on what it needs 
    /// </summary>
    /// <param name="junctionValue">A string representing an int or string "Random"</param>
    /// <param name="juncType">An enum value representing the specific type of junction we are setting the value for"</param>
    private void csvToJunctionHelper(string junctionValue, JunctionTypes juncType)
    {
        if (junctionValue == "Random")
            setPieceRandom(true, juncType);
        else
        {
            setPieceRandom(false, juncType);
            setPieceValue(junctionValue, juncType);
        }
    }

    /// <summary>
    /// Helper function for reading from csv back to data controller. Converts from string to datarep 
    /// </summary>
    /// <param name="dataRep">A string representing the data representation, either "Integer" or "Percentage"</param>
    /// <returns> The corresponding enum value matching the data rep</returns>
    private DataRepresentations stringToDataRep(string dataRep)
    {
        if (dataRep == Enum.GetName(typeof(DataRepresentations), DataRepresentations.Integer))
        {
            return DataRepresentations.Integer;
        }
        else
            return DataRepresentations.Percentage;
    }


    /// <summary>
    /// Wrapper function to update errors 
    /// </summary>
    private void updateErrors()
    {
        //First iteration of this functions im just going to make it check everything every time 
        //Alternatively using an "error" areas way so I can tell where it's concerned would be helpful 

        updateJunctionErrors();

    }

    /// <summary>
    /// Takes the maze parameters and forms an intermediate parameter object that we can read to csv
    /// </summary>
    /// <returns></returns>
    public IntermediateParameterStorage getIntermediateDataRep()
    {
        return new IntermediateParameterStorage(distance, dataRep, junctions, maxStraightsInARow, minStraightsInARow, wallHeight, wallTexture, floorTexture, ceilingTexture, locomotionMethod, shouldUseCeiling, bufferStraights, useExplicitOrdering);
    }

    /// <summary>
    /// Takes the maze parameters and forms an intermediate parameter object that we can read to csv
    /// </summary>
    /// <returns></returns>
    public IntermediateParameterStorage getIntermediateDataRepPostRandomPopulation()
    {
        return new IntermediateParameterStorage(distance, dataRep, getNewJunctionsFromJuncsDict(populateRandomValues(distance, junctions)), maxStraightsInARow, minStraightsInARow, wallHeight, wallTexture, floorTexture, ceilingTexture,
            locomotionMethod, shouldUseCeiling, bufferStraights, useExplicitOrdering);
    }

    /// <summary>
    /// Simple getter for the maze wall height
    /// </summary>
    /// <param name="wallHeight">A float representing the height for the walls in the maze</param>
    public void setWallHeight(float wallHeight)
    {
        this.wallHeight = wallHeight;

    }

    /// <summary>
    /// Simple getter for wall height
    /// </summary>
    /// <returns>A float representing the height of the walls in the maze</returns>
    public float getWallHeight()
    {
        return this.wallHeight;
    }

    /// <summary>
    /// Sets the wall texture for the maze to a path from the resources folder
    /// </summary>
    /// <param name="wallTexture">A path to the relevant texture to be used</param>
    public void setWallTexture(string wallTexture)
    {
        this.wallTexture = wallTexture;
        mat.SetWallTexture(stringToTexture(this.wallTexture));
    }

    /// <summary>
    /// Gets the path for the relevant wall texture to be used 
    /// </summary>
    /// <returns>The path to the wall texture</returns>
    public string getWallTexture()
    {
        //We eventually want to redirect to a default texture if nothing is selected
        return this.wallTexture;
    }

    /// <summary>
    /// Sets the floor texture to a relevant path from the resources folder
    /// </summary>
    /// <param name="floorTexture">A path to the texture that they want for the maze floors</param>
    public void setFloorTexture(string floorTexture)
    {
        this.floorTexture = floorTexture;
        mat.SetFloorTexture(stringToTexture(floorTexture));
    }

    /// <summary>
    /// Function that converts our texture into a sprite so that it can eventually be displayed on the UI 
    /// this function assumes that the textures in resources will be 2D textures
    /// </summary>
    /// <param name="textureName">The name of the texture that we want the sprite for</param>
    /// <returns>A Sprite for the supplied texture</returns>
    public Sprite textureToSprite(string textureName)
    {
        Texture currentTexture = stringToTexture(textureName);
        Sprite mySprite = Sprite.Create((Texture2D)currentTexture, new Rect(0.0f, 0.0f, currentTexture.width, currentTexture.height), new Vector2(0.5f, 0.5f), 100.0f);
        return mySprite;
    }

    /// <summary>
    /// Simple getter for the floor textures path 
    /// </summary>
    /// <returns>Returns a string representing a path to the texture of interest</returns>
    public string getFloorTexture()
    {
        return this.floorTexture;
    }

    /// <summary>
    /// Duplicate nested class to get around in build vs no build issues
    /// </summary>
    [Serializable]
    public class FileNameInfo
    {
        public string[] fileNames;

        public FileNameInfo(string[] fileNames)
        {
            this.fileNames = fileNames;
        }
    }

    /// <summary>
    /// Gets all the filenames for the available materials
    /// </summary>
    /// <returns>a list of strings containing all the filenames</returns>
    public string[] getAllTexturesFromResources()
    {
#if (UNITY_EDITOR)
        string resourcsPath = Application.dataPath + "/Resources/Materials";

        //Get file names except the ".meta" extension
        string[] fileNames = Directory.GetFiles(resourcsPath)
            .Where(x => Path.GetExtension(x) != ".meta").ToArray();


        for (int i = 0; i < fileNames.Length; i++)
            fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);

        FileNameInfo fileInfo = new FileNameInfo(fileNames);
        string fileInfoJson = JsonUtility.ToJson(fileInfo);

        //Save the json to the Resources folder as "FileNames.txt"
        File.WriteAllText(Application.dataPath + "/Resources/FileNames.txt", fileInfoJson);

        AssetDatabase.Refresh();
#endif

        TextAsset fileNamesLocal = Resources.Load<TextAsset>("Filenames");
        FileNameInfo fileInfoLocal = JsonUtility.FromJson<FileNameInfo>(fileNamesLocal.text);
        return fileInfoLocal.fileNames;
    }

    /// <summary>
    /// Loads the given name of the texture from the resources folder
    /// </summary>
    /// <param name="textureName">The given name of the texture with no file extension</param>
    /// <returns>A texture corresponding to the name, or null if the file doesn't exist</returns>
    public Texture stringToTexture(string textureName)
    {

        return Resources.Load<Texture>("Materials/" + textureName);
    }

    /// <summary>
    /// Sets the locomotion method relevant to this maze
    /// </summary>
    /// <param name="locoMethod">Takes in a string (maybe eventually should take in an enum) that represents the locomotion method to use</param>
    public void setLocomotionMethod(string locoMethod)
    {

        MovementTypes thisMovement = (MovementTypes)Enum.Parse(typeof(MovementTypes), locoMethod);

        this.locomotionMethod = thisMovement;
    }

    /// <summary>
    /// Simple getter for the locomotion method 
    /// </summary>
    /// <returns>Returns a string representing the locomotion method the user is using</returns>
    public List<string> getAllLocomotionMethods()
    {
        List<string> locomotionMethods = new List<string>();
        foreach (string s in Enum.GetNames(typeof(MovementTypes)))
        {
            locomotionMethods.Add(s);
        }

        return locomotionMethods;
    }

    /// <summary>
    /// Sets the texture for the ceiling of the maze. 
    /// </summary>
    /// <param name="ceilingTexture">The path to the relevant texture</param>
    public void setCeilingTexture(string ceilingTexture)
    {
        this.ceilingTexture = ceilingTexture;
        mat.SetCeilingTexture(stringToTexture(ceilingTexture));

    }

    /// <summary>
    /// Simple getter for the ceiling texture
    /// </summary>
    /// <returns>Returns the path to the ceiling texture</returns>
    public string getCeilingTexture()
    {
        return this.ceilingTexture;
    }

    /// <summary>
    /// Simple setter for whether or not the maze should generate with ceilings 
    /// </summary>
    /// <param name="shouldUseCeiling"></param>
    public void setShouldUseCeiling(bool shouldUseCeiling)
    {
        this.shouldUseCeiling = shouldUseCeiling;
    }

    /// <summary>
    /// Simple getter for whether the maze should use ceilings 
    /// </summary>
    /// <returns>Returns true if maze should generate with a ceiling</returns>
    public bool getShouldUseCeiling()
    {
        return this.shouldUseCeiling;
    }

    //This will simply carry over all the information
    /// <summary>
    /// Wrapper function to establish the junction weighting based off the user parameters, then read that 
    /// data to the object we pass to the maze to use during generation
    /// </summary>
    public void makeSingletonObjectToPassMaze()
    {

        //They should have error checked already, but we will be safe 

        //StartCoroutine(wait());

        if (getError() == null)
        {

            //Before we fill in our random values, lets make sure our helper gets read to csv. 
            //IntermediateParameterStorage parameterInfo = new IntermediateParameterStorage(distance, dataRep, junctions, maxStraightsInARow, minStraightsInARow, wallHeight, wallTexture, floorTexture, ceilingTexture, locomotionMethod, shouldUseCeiling);




            //Dictionary<JunctionTypes, int> junctionsDict = getJuncsDict(distance, junctions);
            Dictionary<JunctionTypes, int> junctionsDict = populateRandomValues(distance, junctions);


            //IntermediateParameterStorage postPopulatedParamInfo = new IntermediateParameterStorage(distance, dataRep, getNewJunctionsFromJuncsDict(junctionsDict), maxStraightsInARow, minStraightsInARow, wallHeight, wallTexture, floorTexture, ceilingTexture, locomotionMethod, shouldUseCeiling);


            //We have no errors, so we're safe to carry over all the data 
            MazeParameterManager.instance.InitializeOrOverWrite(distance, maxStraightsInARow, minStraightsInARow, junctionsDict, wallHeight, this.locomotionMethod, shouldUseCeiling, this.useExplicitOrdering);


        }
        else
        {
            //Just dump all the errors to log for now (eventually should be on UI) 
            string concatinatedErors = "Failed making stack because errors present:\n";
            foreach (var error in getAllErrors())
            {
                concatinatedErors += error.Key.ToString() + "\n";
            }
            Debug.LogError(concatinatedErors);
        }


    }

    /// <summary>
    /// Helper for saving the values after we fill in randoms
    /// </summary>
    /// <param name="juncs">The junction values after filling in random and removing the connecting straights</param>
    /// <returns>A new dictionary using those values</returns>
    private Dictionary<JunctionTypes, JunctionValues> getNewJunctionsFromJuncsDict(Dictionary<JunctionTypes, int> juncs)
    {
        Dictionary<JunctionTypes, JunctionValues> newDict = new Dictionary<JunctionTypes, JunctionValues>();
        foreach (JunctionTypes junction in juncs.Keys)
        {
            JunctionValues curJunc = new JunctionValues(junction);
            curJunc.setNumberOfThisJunction(juncs[junction]);
            curJunc.setIsRandom(false);
            newDict.Add(junction, curJunc);
        }

        return newDict;
    }


    /// <summary>
    /// Function to ensure valid user input and updates the errors object. 
    /// </summary>
    private void updateJunctionErrors()
    {
        if (!doJunctionsAddUp())
        {
            if (dataRep == DataRepresentations.Integer)
                this.errors[ErrorTypes.JuncDistributionDoesntMatchDistance] = true;
            else
                this.errors[ErrorTypes.JuncDistributionNot100Percent] = true;
        }
        else
        {
            this.errors[ErrorTypes.JuncDistributionDoesntMatchDistance] = false;
            this.errors[ErrorTypes.JuncDistributionNot100Percent] = false;
        }
        invokeCallback();
    }

    /// <summary>
    /// Function to return all errors present
    /// </summary>
    /// <returns> Dictionary mapping the error type to true or false depending on whether its present</returns>
    public Dictionary<ErrorTypes, bool> getAllErrors()
    {
        return this.errors;
    }

    /// <summary>
    /// Gets the first error from our errors dictionary or null for none. 
    /// </summary>
    /// <returns> The enum type of the error present</returns>
    public ErrorTypes? getError()
    {
        //We don't want to assault the user, so we're just going to pop the first one thats true. 
        foreach (var item in this.errors)
        {
            if (item.Value == true)
            {
                return item.Key;
            }
        }

        //If there are no errors, return null
        return null;

    }

    /// <summary>
    /// Simple getter for the data rep
    /// </summary>
    /// <returns> The corresponding enum value matching the data rep</returns>
    public DataRepresentations getDataRep()
    {
        return dataRep;
    }

    /// <summary>
    /// Simple Setter for the max straights 
    /// </summary>
    /// <param name="maxStraights">Integer representing the max number of straights that can be spawned in a row</param>
    public void setMaxStraights(int maxStraights)
    {
        if (maxStraights < 1)
            throw new ValueException("Error, can not define a percentage greater than 100%");
        else
            this.maxStraightsInARow = maxStraights;
    }

    /// <summary>
    /// Simple Setter for the min straights 
    /// </summary>
    /// <param name="minStraights">Integer representing the min number of straights that can be spawned in a row</param>
    public void setMinStraights(int minStraights)
    {
        this.minStraightsInARow = minStraights;
    }

    /// <summary>
    /// Simple getter for the max straights 
    /// </summary>
    /// <returns> an integer representing the max number of straights </returns>
    public int getMaxStraightsInARow()
    {
        return this.maxStraightsInARow;
    }

    /// <summary>
    /// Function to calculate the minimum number of straights based on distance and the number of junctions added
    /// </summary>
    /// <returns> an integer representing the number of connecting straights required for the current configuration</returns>
    public int getMinimumStraights()
    {
        //This is currently assuming we don't care if a straight or a junction is connected directly to the end. 
        //Currently, just every piece that is not a straight will have a connecting straight 
        int count = 0;
        foreach (var junc in junctions)
        {
            if (junc.Key != JunctionTypes.Straight)
            {

                count += junc.Value.getNumJunctions(this.dataRep, this.distance);
            }
        }

        int temp = distance - (distance / (1 + minStraightsInARow));


        return Math.Max(count * minStraightsInARow, temp);
    }

    /// <summary>
    /// Wrapper function to get the current value of a piece
    /// </summary>
    /// <param name="junc">Enum type representing the type of junction we are requesting information about</param>
    /// <returns>a string representation of the piece value "Random" an integer, or a float</returns>
    public string getPieceValue(JunctionTypes junc)
    {
        return this.junctions[junc].getCurrentValue(this.dataRep);
    }

    /// <summary>
    /// Wrapper function to get the alternate piece value based on the datarep
    /// </summary>
    /// <param name="junc">Enum type representing the type of junction we are requesting information about</param>
    /// <returns>a string representation of the piece value "Random" an integer, or a float</returns>
    public string getPieceAltValue(JunctionTypes junc)
    {
        return this.junctions[junc].getAltValue(this.dataRep, distance);
    }

    /// <summary>
    /// Simple setter for data rep param
    /// </summary>
    /// <param name="dataRepReq">Enum type representing the data representation</param>
    public void setDataRep(DataRepresentations dataRepReq)
    {
        this.dataRep = dataRepReq;
        //Switch over our percent to int errors if needed 
        updateErrors();
    }

    /// <summary>
    /// Simple setter for distance param
    /// </summary>
    /// <param name="distance">integer representing the total maze distance</param>
    public void setDistance(int distance)
    {
        if (distance > 0)
        {
            this.distance = distance;
            updateErrors();
            //If they update the distance, then we need to update the alt values 

        }
        else
        {
            throw new ValueException("distance of 0 or below is not supported!");
        }


    }



    //Takes the value of junction as a string and the type of junctions 
    //If data rep is int, attempt to parse and int, otherwise will attempt float
    /// <summary>
    /// Function that based on the current data representation, attempts to parse an Int or a float
    /// then sets the corresponding junction value if parsing was succesful 
    /// </summary>
    /// <param name="juncVal">String representing the value for the junction should be int or float</param>
    /// <param name="junc">Enum type representing the junction we're interested in changing the value for</param>
    public void setPieceValue(string juncVal, JunctionTypes junc)
    {

        //uses the enum type to consult our list of junction data objects
        //We can make the assumption using dataRep. If percent, then we know its float. Else its int 
        if (this.dataRep == DataRepresentations.Integer)
        {
            //Make sure that we get an integer value 
            if (int.TryParse(juncVal, out int result))
            {
                setJuncForInt(result, junc);

            }
            else
            {
                //They may have given float or random chars
                throw new ValueException("Error: can not parse input into integer value!");
            }
        }
        else
        {

            if (float.TryParse(juncVal, out float result))
            {
                //All of our values are going to be percentage representations

                setJuncForFloat(result, junc);

            }
            else
            {
                throw new ValueException("Error: can not parse input into float value!");
            }
        }
    }


    //Setting the value to random prevents it from being used in our calculation 
    /// <summary>
    /// Simple wrapper function for setting a junction type to random
    /// </summary>
    /// <param name="isRandom">Boolean representing whether this junction should be random</param>
    /// <param name="junc">Enum type representing the junction we're interested in changing the value for</param>
    public void setPieceRandom(bool isRandom, JunctionTypes junc)
    {
        this.junctions[junc].setIsRandom(isRandom);
        updateJunctionErrors();
    }


    //Theres a chanve this will throw a value exception, but we need to wait til we're on the UI to do anything with it 
    /// <summary>
    /// Wrapper function for setting junction equal to a float value
    /// </summary>
    /// <param name="juncVal">Float representing the percentage out of 100, for this junction</param>
    /// <param name="junc">Enum type representing the junction we're interested in changing the value for</param>
    public void setJuncForFloat(float juncVal, JunctionTypes junc)
    {
        juncVal = (float)juncVal / (float)100;

        this.junctions[junc].setPercentOfTotalJunctions(juncVal);
        updateJunctionErrors();
    }

    /// <summary>
    /// Wrapper function for setting junction equal to a integer value
    /// </summary>
    /// <param name="juncVal">integer representing the number of this junction</param>
    /// <param name="junc">Enum type representing the junction we're interested in changing the value for</param>
    public void setJuncForInt(int juncVal, JunctionTypes junc)
    {
        this.junctions[junc].setNumberOfThisJunction(juncVal);
        updateJunctionErrors();
    }

    /// <summary>
    /// Iterates through all of the junctions and determines if at least 1 is set to random
    /// </summary>
    /// <returns>True if a junction is random, and false if otherwise</returns>
    private bool isAJuncRandom()
    {
        foreach (var junc in this.junctions)
        {
            if (junc.Value.getIsRandom())
                return true;
        }

        return false;
    }

    /// <summary>
    /// If datarep is set to Integer, checks if all the junctions = distance
    /// otherwise, checks if all the junctions add up to 100%
    /// </summary>
    /// <returns> true if a valid distribution, and false if they've supplied to many junctions</returns>
    private bool doJunctionsAddUp()
    {
        //For Ints: just see if sum = numJunctions 
        if (this.dataRep == DataRepresentations.Integer)
        {
            int total = 0;
            foreach (var item in this.junctions)
            {
                total += item.Value.getNumberOfJunction();
            }
            //Note if we change how numJunctions operates we may need to change this to be null safe 
            if (total == distance || (total < distance && isAJuncRandom()))
                return true;


        }
        //For percent, just see if they total 1
        else
        {
            float total = 0;
            foreach (var item in this.junctions)
            {
                total += item.Value.getPercentOfTotalJunctions();
            }
            //Note if we change how numJunctions operates we may need to change this to be null safe 
            if (total == 1 || (total < 1 && isAJuncRandom()))
                return true;
        }
        //Both if's failed 
        return false;

    }

    /// <summary>
    /// Simple boolean check to see if we can have straights in the distribution
    /// This might need to get more complicated eventually
    /// </summary>
    /// <returns>false if we can't have straights outside of the connecting straights, and true otherwise</returns>
    private bool canHaveMoreStraights(int distance, int straights)
    {
        //Also need to keep a tab of how many straights we've added 
        //and we can have (max - min) * junctionsThatAren'tStraights

        //If we added a straight would it throw things off? 
        int newStraights = straights + 1;

        //This would mean that the new max number of junctions we can have is 
        int newJunctions = distance - newStraights;

        bool result = !(newStraights > newJunctions * maxStraightsInARow);
        Debug.Log("The result for can we have more straights is " + result);
        return result; 
    }


    private Dictionary<JunctionTypes, int> populateRandomValues(int distance, Dictionary<JunctionTypes, JunctionValues> junctions)
    {
        Dictionary<JunctionTypes, int> juncWeights = new Dictionary<JunctionTypes, int>();

        List<JunctionTypes> randomJuncs = new List<JunctionTypes>();

        int totalJuncs = 0;

        foreach (var junc in junctions)
        {
            //Check if its a random value (we ignore start and end because those don't go into the stack) 
            if (junc.Value.getIsRandom() && junc.Value.juncType != JunctionTypes.End && junc.Value.juncType != JunctionTypes.Start)
            {
                //If they set straights to random
                if (junc.Value.juncType == JunctionTypes.Straight)
                {
                    //We need to supply the bare minimum before we roll for random
                    juncWeights[junc.Key] = getMinimumStraights();

                    totalJuncs += getMinimumStraights(); 

                    //Check if we can have more straights: 
                    if (canHaveMoreStraights(distance, juncWeights[junc.Key]))
                        //Add the junction to our random junctions 
                        randomJuncs.Add(junc.Key);
                }
                else
                {
                    //Just set it to 0 for now 
                    juncWeights[junc.Key] = 0;
                    randomJuncs.Add(junc.Key);
                }


            }
            //If the junction supplied is not random or an edge case piece
            else
            {
                //It's not a random value, so just add it directly to our weights
                juncWeights[junc.Key] = junc.Value.getNumJunctions(this.dataRep, distance);

                totalJuncs += junc.Value.getNumJunctions(this.dataRep, distance);
            }
        }

        

        int remainingDistance = distance - totalJuncs;
        //Get the max value 
        int maxVal = randomJuncs.Count;



        while (remainingDistance > 0)
        {
            //Get a random number 0 to max 
            int juncIndex = getRandomIntEvenDistribution(maxVal, 5000);

            if (randomJuncs[juncIndex] == JunctionTypes.Straight)
            {
                //We need to make sure that placing this straights won't put us over 
                if (!canHaveMoreStraights(distance, juncWeights[randomJuncs[juncIndex]]))
                    continue; 

            }

            juncWeights[randomJuncs[juncIndex]] += 1;
            remainingDistance--;

        }

        return juncWeights;
    }


    //This will fill all randoms as well as ensure we get integer form 
    /// <summary>
    /// This function will take our distribution of junctions, ensure everything is read to integer form 
    /// (if the data representation is Percentage), and then populate all of the junctions marked as 
    /// random, following an even distribution
    /// </summary>
    /// <param name="distance">integer representing the total distance of the maze</param>
    /// <param name="junctions">Dictionary mapping junction type to JunctionValues data class</param>
    /// <returns>Returns a dictionary mapping junction type to its equavalent amount</returns>
    private Dictionary<JunctionTypes, int> getJuncsDict(int distance, Dictionary<JunctionTypes, JunctionValues> junctions)
    {
        Dictionary<JunctionTypes, int> juncWeights = new Dictionary<JunctionTypes, int>();

        List<JunctionTypes> randomJuncs = new List<JunctionTypes>();

        int totalJuncs = 0;

        bool isOnlyStraights = true;

        //Read over the values and establish the weighting 
        foreach (var junc in junctions)
        {
            //Check if its a random value (we ignore start and end because those don't go into the stack) 
            if (junc.Value.getIsRandom() && junc.Value.juncType != JunctionTypes.End && junc.Value.juncType != JunctionTypes.Start)
            {
                //Check if theres any extra room for straights in our distribution
                if (junc.Key == JunctionTypes.Straight && !canHaveMoreStraights(distance, getMinimumStraights()))
                {
                    //Add the connecting straights to the total juncs so they're removed from remaining distance
                    totalJuncs += getMinimumStraights();
                    //There's no room once we account for the connecting straights, so just set it to 0 so it doesn't show in stack
                    juncWeights[junc.Key] = 0;
                }
                else
                {
                    //Add the connecting straights to the total juncs so they're removed from remaining distance
                    if (junc.Key == JunctionTypes.Straight) 
                        totalJuncs += getMinimumStraights();

                    //Add the junction to our random junctions 
                    randomJuncs.Add(junc.Key);
                    //Set its weight to 0 since we start off with none 
                    juncWeights[junc.Key] = 0;
                }
            }
            else
            {
                //If our straights are not marked as random
                if (junc.Key == JunctionTypes.Straight)
                {
                    //We need to update the value they supplied to remove the connecting straights 
                    int straightValue = junc.Value.getNumJunctions(this.dataRep, distance);
                    totalJuncs += straightValue;

                    straightValue -= getMinimumStraights();


                    //Include any remaining straights in our distribution
                    juncWeights[junc.Key] = straightValue;

                    //Acount for the total number we're at 
                    //totalJuncs += straightValue;
                }
                else
                {
                    if (junc.Value.juncType != JunctionTypes.End && junc.Value.juncType != JunctionTypes.Start)
                        isOnlyStraights = false;
                    //It's not a random value, so just add it directly to our weights
                    juncWeights[junc.Key] = junc.Value.getNumJunctions(this.dataRep, distance);
                    //Acount for the total number we're at we need to include the connecting straights 
                    totalJuncs += junc.Value.getNumJunctions(this.dataRep, distance) + (junc.Value.getNumJunctions(this.dataRep, distance) * minStraightsInARow);
                }
            }
        }


        //Subtract all of our non-random junctions from distance
        int remainingDistance = distance - totalJuncs;
        if (isOnlyStraights && remainingDistance == 0)
        {
            juncWeights[JunctionTypes.Straight] += getMinimumStraights();
        }

        //Get the max value 
        int maxVal = randomJuncs.Count;
        while (remainingDistance > 0)
        {
            //Get a random number 0 to max 
            int juncIndex = getRandomIntEvenDistribution(maxVal, 5000);

            //If its a straight
            if (randomJuncs[juncIndex] == JunctionTypes.Straight)
            {
                //Straights have no connecting straights, so this is fine 
                juncWeights[randomJuncs[juncIndex]] += 1;
                remainingDistance--;
            }
            else
            {
                //Its a piece other than a straight, so lets make sure that we still have room for two pieces. 
                if (remainingDistance >= 2)
                {
                    //We have room for another piece, so add it 
                    juncWeights[randomJuncs[juncIndex]] += 1;
                    //Remove the piece and its connecting straight from remaining distance
                    remainingDistance -= 1;
                }
                else
                {
                    //Forcing a straight is our only option 
                    juncWeights[JunctionTypes.Straight] += 1;
                    remainingDistance--;
                }
            }

        }
        return juncWeights;
    }

    //Lower entropyMeasure, higher the entropy
    /// <summary>
    /// This function generates integers following an even distribution
    /// </summary>
    /// <param name="maxValue">Integer representing the maximum value that can be returned</param>
    /// <param name="entropyMeasure">An integer representing the entropy, a lower value produces higher entropy</param>
    /// <returns>An integer from 0 - maxValue</returns>
    private int getRandomIntEvenDistribution(int maxValue, int entropyMeasure)
    {

        int counter = 0;
        while (true)
        {
            if (counter > 100)
            {
                Debug.LogError("Infinite loop detected");
                return -1;
            }
            else
                counter++;

            if (this.rangeOfUnusedChoice >= maxValue)
            {
                if (this.unusedChoice < this.rangeOfUnusedChoice - this.rangeOfUnusedChoice % maxValue)
                {
                    //Get our return value 
                    int value = this.unusedChoice % maxValue;

                    //Update our unusedChoice
                    this.unusedChoice = this.unusedChoice / maxValue;

                    //Update our range 
                    this.rangeOfUnusedChoice = this.rangeOfUnusedChoice / maxValue;

                    return value;
                }
                else
                {
                    this.unusedChoice = this.unusedChoice % maxValue;
                    this.rangeOfUnusedChoice = this.rangeOfUnusedChoice % maxValue;
                }
            }

            int randomNum = UnityEngine.Random.Range(0, entropyMeasure + 1);
            this.unusedChoice = this.unusedChoice * entropyMeasure + randomNum;
            this.rangeOfUnusedChoice = this.rangeOfUnusedChoice * entropyMeasure;
        }

    }


}

//The parameters should control exceptions that are relvant to them explicitly 
//e.g. no exceptions for junctions totals not adding (i.e. they don't care what the other junction values are) 
/// <summary>
/// Data class used to represent a single junction 
/// </summary>
public class JunctionValues
{
    /// <summary>
    /// float representing its percent of the whole  
    /// </summary>
    private float percentOfTotalJunctions;

    /// <summary>
    /// int representing how many of this junction there are 
    /// </summary>
    private int numberOfJunction;

    /// <summary>
    /// bool to represent if this junction has been marked as random 
    /// </summary>
    private bool isRandom;

    /// <summary>
    /// Enum type to know which kind of junction it is 
    /// </summary>
    public JunctionTypes juncType;

    /// <summary>
    /// Constructor to intialize the junction to default values (starting as random)
    /// </summary>
    /// <param name="juncType">Enum value representing the type of junction</param>
    public JunctionValues(JunctionTypes juncType)
    {
        //We must access is random before trying to access int or float values 
        this.isRandom = true;
        this.juncType = juncType;

        //Because we opted for a random toggle, we can init to 0 
        this.numberOfJunction = 0;
        this.percentOfTotalJunctions = 0;
    }

    /// <summary>
    /// Simple getter for Percentage data rep 
    /// </summary>
    /// <returns>a float representing the percent out of 100 for this junction</returns>
    public float getPercentOfTotalJunctions()
    {
        if (!this.isRandom)
            return this.percentOfTotalJunctions;
        else
            return 0;
    }

    /// <summary>
    /// Simple getter for the number of this junction
    /// </summary>
    /// <returns>the number of the junction, or 0 if set to random</returns>
    public int getNumberOfJunction()
    {
        //We return 0 if random for distance calculations 
        if (!this.isRandom)
            return this.numberOfJunction;
        else
            return 0;
    }

    /// <summary>
    /// Simple getter for the random value 
    /// </summary>
    /// <returns>true if random, or false otherwise</returns>
    public bool getIsRandom()
    { return this.isRandom; }

    /// <summary>
    /// Simple setter for percentage data representation. Throws exceptions if percent is invalid
    /// </summary>
    public void setPercentOfTotalJunctions(float percent)
    {

        if (percent > 1)
            throw new ValueException("Error, can not define a percentage greater than 100%");
        else if (percent < 0)
            throw new ValueException("Error, can not define a negative percentage");
        else
            this.percentOfTotalJunctions = percent;
        this.isRandom = false;

    }

    /// <summary>
    /// Simple setter for integer data representation. Throws exceptions if value is invalid
    /// </summary>
    public void setNumberOfThisJunction(int numJunc)
    {
        if (numJunc < 0)
            throw new ValueException("Error, can not specify a negative number of junctions");
        this.numberOfJunction = numJunc;
    }

    /// <summary>
    /// A getter for the junctions value based on the data representation
    /// </summary>
    /// <param name="dataRep">The current data representation either percentage or integer</param>
    /// <returns>a string of the current value, "Random" if set to random</returns>
    public string getCurrentValue(MazeDataController.DataRepresentations dataRep)
    {
        if (this.isRandom)
            return "Random";
        else if (dataRep == MazeDataController.DataRepresentations.Integer)
            return this.numberOfJunction.ToString();
        else
            return this.percentOfTotalJunctions.ToString();
    }

    //This function serves to supply the UI with supplemental information 
    //If data is displayed in integers, will return the percentage approximation. 
    /// <summary>
    /// Returns the opposing data type based on the data representation
    /// </summary>
    /// <param name="currentRep">The current Data representation, either float or int</param>
    /// <param name="distance">The total distance for the maze, used to infer the other type</param>
    /// <returns>String with junctions opposing data representation, or "Random" if set to random</returns>
    public string getAltValue(MazeDataController.DataRepresentations currentRep, int distance)
    {

        if (this.isRandom)
            return "Random";
        //If its not random and 0, just return 0 
        else if (currentRep == MazeDataController.DataRepresentations.Integer)
        {
            //Quick 0 check 
            if (distance == 0)
                return "0";

            //we want to return a percent 
            return ((float)this.numberOfJunction / distance).ToString("#.##");
        }
        else
        {
            //Its not random, and current dataRep is precent, so we want to return int.
            //if they don't define num junctions we won't show an alternative 
            return (this.percentOfTotalJunctions * distance).ToString("#");
        }
    }

    /// <summary>
    /// Simple setter for marking junction as random 
    /// </summary>
    /// <param name="isRandom">A bool representing whether or not this junction is random</param>
    public void setIsRandom(bool isRandom)
    {
        this.isRandom = isRandom;
    }

    /// <summary>
    /// Calculates or returns the integer value for this junction
    /// </summary>
    /// <param name="distance">The total distance of the maze, used to infer int value if dataRep is percent</param>
    /// <param name="dataRep">The current dataRepresentation, either float or integer</param>
    /// <returns>an integer dentoting how many of this junction there are</returns>
    public int getNumJunctions(MazeDataController.DataRepresentations dataRep, int distance)
    {
        if (dataRep == MazeDataController.DataRepresentations.Integer)
        {
            //They are inputing intergers, so just use the integer
            return numberOfJunction;
        }
        else
        {
            int val = (int)(distance * percentOfTotalJunctions);
            //They are using floats so use the float to calculate 


            return val;
        }
    }

}



/// <summary>
/// Simple Custom Exception class
/// </summary>
class ValueException : Exception
{
    public ValueException()
        : base() { }

    public ValueException(string message)
        : base(message) { }
}

/// <summary>
/// Simple Custom Exception class
/// </summary>
class JunctionMismatchException : Exception
{
    public JunctionMismatchException()
        : base() { }

    public JunctionMismatchException(string message)
        : base(message) { }
}
