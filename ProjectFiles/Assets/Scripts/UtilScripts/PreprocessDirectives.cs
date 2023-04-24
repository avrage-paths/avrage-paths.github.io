
using System;
using System.IO;
using System.Linq;
#if (UNITY_EDITOR)
using UnityEditor;
using UnityEditor.Build;
#endif
using UnityEngine;

/// <summary>
/// Simple data class to write filenames to json 
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
/// A wrapper for the Build report class supplied by unity. This function on build will carry over
/// the names of the textures to the persistant data path so they can be easily loaded in standalone builds
/// </summary>
#if (UNITY_EDITOR)
class PreprocessDirectives : IPreprocessBuildWithReport
{
    /// <summary>
    /// Callback for unity to use
    /// </summary>
    public int callbackOrder { get { return 0; } }

    /// <summary>
    /// Override for unitys built in function, will save the textures names to file on build 
    /// </summary>
    /// <param name="report">The Build report object</param>
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {


        //The Resources folder path
        string resourcsPath = Application.dataPath + "/Resources/Materials";

        //Get file names except the ".meta" extension
        string[] fileNames = Directory.GetFiles(resourcsPath)
            .Where(x => Path.GetExtension(x) != ".meta").ToArray();


        for (int i = 0; i < fileNames.Length; i++)
            fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);

        //Convert the Names to Json to make it easier to access when reading it
        FileNameInfo fileInfo = new FileNameInfo(fileNames);
        string fileInfoJson = JsonUtility.ToJson(fileInfo);

        //Save the json to the Resources folder as "FileNames.txt"
        File.WriteAllText(Application.dataPath + "/Resources/FileNames.txt", fileInfoJson);

        AssetDatabase.Refresh();
    }
}
#endif
