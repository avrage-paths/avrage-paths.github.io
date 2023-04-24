using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple button script for changing scenes
/// </summary>
public class ChangeSceneButton : MonoBehaviour
{
    // Start is called before the first frame update

    /// <summary>
    /// Simple wrapped for scene manager 
    /// </summary>
    /// <param name="sceneName">A string with the name of the scene</param>
    public void ChangeScene(string sceneName)
    {
        SceneManagerScript.instance.LoadScene(sceneName);
    }
}
