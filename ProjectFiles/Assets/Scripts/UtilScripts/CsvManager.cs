using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SFB;

/// <summary>
/// Manager for exporting / importing csv files
/// </summary>
public class CsvManager : MonoBehaviour
{
    //Lets make it a singleton 
    public static CsvManager instance;

    public string experimentDir;

    /// <summary>
    /// AppData path incase we don't have a directory
    /// </summary>
    private string baseDataLoc;

    /// <summary>
    /// The name of the csv containing the previous experiment directory
    /// </summary>
    private string experimentLookupFile = "sessionInfo.csv";

    /// <summary>
    /// The name of the directrory where we will add all the conditions csv
    /// </summary>
    private string conditionParamsDir = "conditionParams";

    /// <summary>
    /// The name of the file where we will add the list of conditions for easy lookup
    /// </summary>
    private string conditionLookupFile = "conditionsLookup.csv";

    /// <summary>
    /// Save the stack with this name if they requestExplicit ordering 
    /// </summary>
    private string stackLookupName = "orderingFile.csv"; 

    /// <summary>
    /// The file path used by the current experiment
    /// </summary>
    private string currentExperimentPath;

    /// <summary>
    /// Store the names of the conditions so they persist while we get the data for them 
    /// </summary>
    private List<string> storedConditions;


    /// <summary>
    /// Stores the name of the experiment so it persists while we get data for the conditions 
    /// </summary>
    private string storedExperimentName;

    /// <summary>
    /// Simple function to return the name of the conditions 
    /// </summary>
    /// <returns>Returns the string thats the name of the condition lookup file</returns>
    public string getconditionLookup()
    {
        return conditionLookupFile;
    }

    public void Awake()
    {
        //If theres already a csv manager
        if (instance != null)
        {
            //delete the old one 
            Debug.LogWarning("Deleting the old csv manager");
            Destroy(instance.gameObject);
        }

        //make the new one 
        instance = this;
        DontDestroyOnLoad(gameObject);


        baseDataLoc = Application.persistentDataPath;
    }

    /// <summary>
    /// Helper to get the saved stack back from disk
    /// </summary>
    /// <param name="pathToExperimentDir">A path to the experiment directory where everything is saved</param>
    /// <param name="condition">The condition they want the stack for</param>
    /// <returns></returns>
    public List<MazeParameterManager.JunctionData> getExplicitStackInfo(string pathToExperimentDir, string condition)
    {
        List<MazeParameterManager.JunctionData> juncs = new List<MazeParameterManager.JunctionData>();
        juncs = Sinbad.CsvUtil.LoadObjects<MazeParameterManager.JunctionData>(pathToExperimentDir + "/" + conditionParamsDir + "/" + condition + "-" + this.stackLookupName, true);

        foreach (MazeParameterManager.JunctionData junctionData in juncs)
            Debug.Log("Recieved junction " + junctionData); 

        return juncs;
    }

    //Opens the native file explorer and returns a path to the selected directory 
    /// <summary>
    /// Wrapper class for the standalone file browser. Opens folder panel for user to choose a path
    /// </summary>
    /// <returns> string representing the path to the directory, or a blank string if they cancel</returns>
    public string getDirectoryPathFromExplorer()
    {
        //We false it so that it can only return a single string 
        string[] path = StandaloneFileBrowser.OpenFolderPanel("Choose a save directory", "", false);


        if (path.Length > 0)
            return path[0];
        else
            return "";
    }

    //Returns empty string if they cancel 
    /// <summary>
    /// Wrapper class for the standalone file browser. Opens file panel for user to choose a path
    /// </summary>
    /// <returns> string representing the path to a file, or a blank string if they cancel</returns>
    public string getFilePathFromExplorer()
    {

        var extensions = new[]
        {
            new ExtensionFilter("Text Files", "txt", "csv")
        };

        //We false it so that it can only return a single string 
        string[] path = StandaloneFileBrowser.OpenFilePanel("Choose a save directory", "", extensions, false);


        if (path.Length > 0)
            return path[0];
        else
            return "";
    }

    /// <summary>
    /// Used to store condition info temporaily to later be used for file writting 
    /// </summary>
    /// <param name="conditions">A list of all of the conditions</param>
    /// <param name="experimentDir">The name of the experiment</param>
    public void storeConditionInfo(List<string> conditions, string experimentDir = "defaultExperiment")
    {
        this.storedConditions = conditions;
        this.experimentDir = experimentDir;
    }


    //By the time they move on to populate conditions, we'll have the experiment dir and location and all the conditions they want to populate 
    /// <summary>
    /// Sets up the file structure for an experiment: Creates the directory for the experiment, as well as saves the conditions to an internal directory
    /// </summary>
    /// <param name="conditions">The list of conditions relevant to the experiment.</param>
    /// <param name="outerMostDir">The a string representing the path to the directory where the experiment directory will be created.</param>
    /// <param name="experimentDir">A string representing the name of the experiment.</param>
    public void setUpExperimentFileStructure(string outerMostDir)
    {
        string experimentDir = this.experimentDir;
        List<string> conditions = this.storedConditions;

        //Currently if they don't supply experiment, default 
        if (experimentDir == "")
        {
            experimentDir = "defaultExperiment";
        }

        //If they don't supply a directory, default to app data
        if (outerMostDir == "")
        {
            this.experimentDir = baseDataLoc + "/" + experimentDir;
        }
        //Might want to remove this eventually 
        else
        {
            this.experimentDir = outerMostDir + "/" + experimentDir;
        }

        //Check if the dir exists
        if (!Directory.Exists(this.experimentDir))
        {
            //It doesn't so make it 
            Directory.CreateDirectory(this.experimentDir);
        }

        //Just re-write the session info file (we may only need to do this if it doesn't exist, but its safer here) 

        this.currentExperimentPath = this.experimentDir;

        //storeLastUsedExperimentDir(this.experimentDir);

        //Make a nice dir for use to store all the param files. 
        if (!Directory.Exists(this.experimentDir + "/" + conditionParamsDir))
            Directory.CreateDirectory(this.experimentDir + "/" + conditionParamsDir);

        List<StringContainer> list = new List<StringContainer>();

        foreach (string condition in conditions)
        {
            list.Add(new StringContainer(condition));
        }


        //Write the list of conditions to the file 
        makeCsvWithMultipleEntrys(list, conditionLookupFile);

    }

    /// <summary>
    /// Used to save the stack when they require explcit ordering 
    /// </summary>
    /// <param name="stack">The stack being saved should actually be an array here</param>
    /// <param name="condition">The condition corresponding to this stack</param>
    public void saveExplicitStackInfo(MazeParameterManager.JunctionData[] stack, string condition)
    {
        Debug.Log("Our conditions params dir is " + conditionParamsDir);
        //I hate arrays 
        List<MazeParameterManager.JunctionData> juncs = new List<MazeParameterManager.JunctionData>(); 
        foreach (MazeParameterManager.JunctionData junc in stack)
            juncs.Add(junc);

        Sinbad.CsvUtil.SaveObjects(juncs, this.currentExperimentPath + "/" + conditionParamsDir + "/" + condition + "-" + this.stackLookupName);
    }

    //Only works if they just made an experiment 
    /// <summary>
    /// Quick getter for if they just set up an experiment 
    /// </summary>
    /// <returns>A string representing the path to the current experiment directory</returns>
    public string getCurrentExperimentPath()
    {
        return currentExperimentPath;
    }

    //Filepath can ideally be loaded from disk (should map to the relevant experiment dir) 
    /// <summary>
    /// Creates necessary directories for the current run. Also sets instance variable of the path
    /// for telemetry manager to later use to save csvs
    /// </summary>
    /// <param name="experimentDir">A string representing the path to the experiment directory that has already been created.</param>
    /// <param name="participantID">A string representing ID of the participant, used to create an outter directory for each condition they run through</param>
    /// <param name="condition">A string the name of the current condition being run.</param>

    public void setUpParticipantFileStructure(string experimentDir, string participantID, string condition)
    {
        //Experiment name 
        //Subject ID 
        //Condition 
        this.currentExperimentPath = experimentDir + "/" + participantID + "/" + condition + System.DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss");
        //Make a nice dir for use to store all the param files. 
        //This creates both the participant dir and the condition dir 
        if (!Directory.Exists(this.currentExperimentPath))
            Directory.CreateDirectory(this.currentExperimentPath);


    }

    //this path should include the path of the outermost dir 
    //Filepath can ideally be loaded from disk (should map to the relevant experiment dir) 
    /// <summary>
    /// Returns the cached list of conditions, to be used on the UI side for presenting. 
    /// </summary>
    /// <param name="pathToExperimentDir">A string representing the path to the experiment directory that has the conditions directory within it</param>
    /// <returns> List of strings of conditions unique to the experiment.</returns>
    public List<string> getConditionListFromFile(string pathToExperimentDir)
    {
        //Make our list of conditions 
        List<StringContainer> conditions = new List<StringContainer>();
        List<string> list = new List<string>();
        //Set up the file path to point to conditions lookup.csv
        string tempPath = pathToExperimentDir + "/" + conditionLookupFile;
        //Check if its invalid 
        if (!File.Exists(tempPath))
            return list; 
        //Load to csv 
        conditions = Sinbad.CsvUtil.LoadObjects<StringContainer>(tempPath, true); 




        //Dataclass back to strings to make things easier on UI 
        foreach (StringContainer condition in conditions)
        {
            list.Add(condition.str); 
        }

        return list; 
    }

    /// <summary>
    /// Wrapper class for the csv util. Assumes that the experiment and or the participant file structures have already been made. Will save a csv with a single row. 
    /// </summary>
    /// <param name="obj">Any object that has public fields that you wish to write to a csv</param>
    /// <param name="fileName">A string ending with .csv representing the name of the csv to be saved</param>
    public void makeCsvWithSingleDataEntry<T>(T obj, string fileName)
    {
        Sinbad.CsvUtil.SaveObject(obj, this.currentExperimentPath + "/" + fileName);
    }

    /// <summary>
    /// Wrapper class for the csv util. Assumes that the experiment and or the participant file structures have already been made. Will save a csv with a multiple rows. 
    /// </summary>
    /// <param name="objs">Any enumerable structure of objects where the objects have public fields that you wish to write to a csv</param>
    /// <param name="fileName">A string ending with .csv representing the name of the csv to be saved</param>
    public void makeCsvWithMultipleEntrys<T>(IEnumerable<T> objs, string fileName)
    {
        Sinbad.CsvUtil.SaveObjects(objs, this.currentExperimentPath + "/" + fileName);
    }

    /// <summary>
    /// Saves the current experiment to appData for easy future lookup 
    /// </summary>
    /// <param name="lastUsedExperiment">A string representing the path of the current experiment</param>
    private void storeLastUsedExperimentDir(int lastUsedExperiment)
    {
        //this.runLookupFile = this.experimentDir + '/' + runLookupFile;
        var info = new CsvDataContainer(this.baseDataLoc + "/" + this.experimentLookupFile, lastUsedExperiment);
        Sinbad.CsvUtil.SaveObject(info, this.baseDataLoc + "/" + this.experimentLookupFile);
    }


    /// <summary>
    /// Gets the last used experiment directory from app data 
    /// </summary>
    /// <returns>Returns a string with the last experiment directory</returns>
    private int getLastExperimentDir()
    {
        var info = new CsvDataContainer();
        Sinbad.CsvUtil.LoadObject(this.baseDataLoc + "/" + this.experimentLookupFile, ref info);
        return info.lastRun;
    }

    //File path is a path to the relevant experiment dir 
    //Condition corresponds to the maze parameters they are trying to load 
    /// <summary>
    /// Returns the intermediate parameter storage for a given condition. To eventually be processed by the MazeDataController 
    /// </summary>
    /// <param name="filepath">A string that represents the path to the relevant experiment directory</param>
    /// <param name="condition">A string representing the current name of the condition, retrieved from the list of conditions saved to the directory</param>
    /// <returns>Intermediate Parameter Storage object</returns>
    public IntermediateParameterStorage getMazeParams(string filepath, string condition)
    {
        IntermediateParameterStorage info = new IntermediateParameterStorage();
        Sinbad.CsvUtil.LoadObject(filepath + "/" + conditionParamsDir + "/" + condition + ".csv", ref info);
        return info;
    }

    /// <summary>
    /// Saves the parameters of the maze to a csv file. Assumes that experimentFileStructure has already been set up.
    /// </summary>
    /// <param name="mazeParams">An IntermediateParameterStorage object populated with the parameters of the given maze loadout</param>
    /// <param name="condition">A string representing the given name of the condition.</param>
    public void saveMazeConditions(IntermediateParameterStorage mazeParams, string condition)
    {

        Sinbad.CsvUtil.SaveObject(mazeParams, this.currentExperimentPath + "/" + conditionParamsDir + "/" + condition + ".csv");
    }




}
