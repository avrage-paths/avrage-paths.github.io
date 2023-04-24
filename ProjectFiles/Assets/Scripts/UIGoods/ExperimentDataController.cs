using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A controller for handling experiments
/// </summary>
public class ExperimentDataController : MonoBehaviour
{
    /// <summary>
    /// The directory where all the experiments will be saved
    /// </summary>
    private string rootFolder;
    /// <summary>
    /// The ID of the current experiment
    /// </summary>
    private string experimentID;
    /// <summary>
    /// The list of conditions unique to this experiment
    /// </summary>
    private List<string> conditions = new List<string>();
    private int currentCondition = 0;

    /// <summary>
    /// The singleton instance of the controller
    /// </summary>
    public static ExperimentDataController instance;


    public void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of data controller found!!");
            return;
        }

        instance = this;

    }


    //Theres no gaurentee these values are populated already so we need to get them ourselves 
    //CSVManager has the ability to get directory paths 
    //Change
    /// <summary>
    /// Loads a maze from a condition and creates the parameter controller to pass to the maze for generation
    /// </summary>
    /// <param name="pathToExperimentDir">The path to the current experiment directory</param>
    /// <param name="condition">A string representing the name of the condition that we are trying to load</param>
    public void loadMazeFromCondition(string pathToExperimentDir, string condition, string participantID = "")
    {
        //We need to find the right maze 
        //Load the csv
        IntermediateParameterStorage mazeParams = CsvManager.instance.getMazeParams(pathToExperimentDir, condition);
        //Read the intermediate parameters to the data class 
        MazeDataController.instance.paramStorageToDataClass(mazeParams);
        //Make the singleton to pass to the maze object 
        MazeDataController.instance.makeSingletonObjectToPassMaze();
        if (mazeParams.useExplicitOrdering)
        {
            //If we're using explicit ordering, force the stack to overwrite. 
            MazeParameterManager.instance.storeListForExplicit(CsvManager.instance.getExplicitStackInfo(pathToExperimentDir, condition));
        }

        MazeParameterManager.instance.experimentName = pathToExperimentDir.Substring(pathToExperimentDir.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
        MazeParameterManager.instance.conditionName = condition;
        MazeParameterManager.instance.participantID = participantID;

    }

    //Return true if we can increment, false if we're out of space
    /// <summary>
    /// Increment the condition nmber to the next condition
    /// </summary>
    /// <returns>Returns true if successfully incremented, false otherwise</returns>
    public bool incrementCondition()
    {

        if (!(currentCondition + 1 >= conditions.Count))
        {
            currentCondition += 1;
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Gets the current condition from our list of conditions 
    /// </summary>
    /// <returns>Returns the current condition</returns>
    public string getCurrentCondition()
    {

        if (this.conditions.Count <= 0)
            return "defaultCondition";
        else
            return conditions[this.currentCondition];
    }

    /// <summary>
    /// Simple setter for a new condition
    /// </summary>
    /// <param name="conditionID">A string representing the name of the condition</param>
    public void setNewCondition(string conditionID)
    {
        this.conditions.Add(conditionID);
    }

    /// <summary>
    /// Set the entire condition list in one go 
    /// </summary>
    /// <param name="conditionIDList">A list of string representing all the names of the conditions for the maze</param>
    public void setNewConditionList(List<string> conditionIDList)
    {
        this.conditions = conditionIDList;
    }

    /// <summary>
    /// A simple setter for the directory where all the experiments will be stored
    /// </summary>
    /// <param name="outterMostID">The path of the directory where all the experiments will be stored</param>
    public void setOutterMostDirectory(string outterMostID)
    {
        this.rootFolder = outterMostID;
    }

    /// <summary>
    /// Simple setter for the name of the experiment
    /// </summary>
    /// <param name="experimentID">The name of the experiment</param>
    public void setExperimentId(string experimentID)
    {
        this.experimentID = experimentID;
    }

    /// <summary>
    /// Simple getter for the name of the experiment
    /// </summary>
    /// <returns>A string representing the name of the experiment</returns>
    public string getExperimentId()
    {
        return this.experimentID;
    }

    /// <summary>
    /// Simple getter for the path to the directory where all the experiments are saved
    /// </summary>
    /// <returns>A string representing the path </returns>
    public string getOutterMostDir()
    {
        return this.rootFolder;
    }

    /// <summary>
    /// Simple getter for the conditions
    /// </summary>
    /// <returns>A list of strings contatining the names of our conditions</returns>
    public List<string> getConditions()
    {
        return conditions;
    }
}
