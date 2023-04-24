using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages scene loading and unloading
/// </summary>
public class SceneManagerScript : MonoBehaviour
{
    public static SceneManagerScript instance;


    //[SerializeField] public GameObject loadingScreenCanvas;
    //[SerializeField] public Image progressBar;

    [SerializeField] public string startingScene;




    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void clearLastScene()
    {
        var lastSceneIndex = SceneManager.sceneCount - 1;

        var lastScene = SceneManager.GetSceneAt(lastSceneIndex);

        SceneManager.UnloadSceneAsync(lastScene);
    }


    public void LoadScene(string sceneName)
    {
        //If we want to load another scene in parallel we use additive
        //Also we over-write loadedScene to track the last page to load
        var loadedScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);


        //Lets prevent an instant transition because thats disorientating 
        loadedScene.allowSceneActivation = false;

        //loadingScreenCanvas.SetActive(true);

        while (loadedScene.progress < 0.9f)
        {
            //progressBar.fillAmount = loadedScene.progress;
        }

        loadedScene.allowSceneActivation = true;

        //loadingScreenCanvas.SetActive(false);
    }


    //Unused
    public void LoadSceneAdditive(string sceneName)
    {
        //If we want to load another scene in parallel we use additive
        //Also we over-write loadedScene to track the last page to load
        var loadedScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);


        //Lets prevent an instant transition because thats disorientating 
        loadedScene.allowSceneActivation = false;

        //loadingScreenCanvas.SetActive(true);

        while (loadedScene.progress < 0.9f)
        {
            //  progressBar.fillAmount = loadedScene.progress;
        }

        loadedScene.allowSceneActivation = true;

        //loadingScreenCanvas.SetActive(false);
    }


    // Start is called before the first frame update
    void Start()
    {
        LoadScene(startingScene);
    }

    // Update is called once per frame
    void Update()
    {

    }

}
