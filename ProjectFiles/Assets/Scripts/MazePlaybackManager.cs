using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MazePlaybackManager : MonoBehaviour
{
    public static MazePlaybackManager instance;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public void LoadMazeAsync(string mazePath)
    {
        StartCoroutine(LoadMazeHelper(mazePath));
    }

    IEnumerator LoadMazeHelper(string mazePath)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Load Previous Maze");

        while (!asyncLoad.isDone)
        {
            yield return null;
        }


        PieceManager.instance.JSONToMaze(mazePath);
    }
}
