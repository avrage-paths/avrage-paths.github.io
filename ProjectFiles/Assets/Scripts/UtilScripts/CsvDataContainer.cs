using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A small class used as an intermediate when getting or saving the last used file location
/// </summary>
public class CsvDataContainer
{
    //This will be unused as of now for permission reasons 
    /// <summary>
    /// The location where the file is saved 
    /// </summary>
    public string fileLocation;

    /// <summary>
    /// The last used experiment directory 
    /// </summary>
    public int lastRun = 0;

    /// <summary>
    /// Simple Constructor for when we want to save the object
    /// </summary>
    public CsvDataContainer(string fileLoc, int runNum)
    {
        this.fileLocation = fileLoc;
        this.lastRun = runNum;
    }

    /// <summary>
    /// A simple constructor for when we want to load the object
    /// </summary>
    public CsvDataContainer()
    {
    }
}
