
using UnityEngine;
using UnityEngine.UIElements;

public class menuController : MonoBehaviour
{
    private VisualElement mainMenu;
    private VisualElement createMazeMenu;
    private VisualElement mazePlayback;
    private VisualElement createExperimentMenu;
    private VisualElement loadExperimentMenu;

    createMazePresenter createMazePresenter;
    createExperimentPresenter createExperimentPresenter;
    loadExperimentPresenter loadExperimentPresenter;
    mazePlaybackPresenter menuPresenter;

    mainMenuPresenter mainMenuPresenter;

    // Start is called before the first frame update
    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        mainMenu = root.Q("mainMenuUXML");
        createMazeMenu = root.Q("createMazeMenu");
        mazePlayback = root.Q("mazePlaybackMenu");
        createExperimentMenu = root.Q("createExperimentMenu");
        loadExperimentMenu = root.Q("loadExperimentMenu");

        setUpStartMenu();
        setUpCreateMazeMenu();
        setUpCreateExperimentMenu();
        setUpLoadExperimentMenu();
        setUpMazePlaybackMenu();
    }

    private void setUpStartMenu()
    {
        mainMenuPresenter = new mainMenuPresenter(mainMenu);
        mainMenuPresenter.openMazePlayback = () => ToggleMenu(mazePlayback);
        mainMenuPresenter.openCreateExperiment = () => ToggleMenu(createExperimentMenu);
        mainMenuPresenter.openLoadExperiment = () => ToggleMenu(loadExperimentMenu);

        mainMenuPresenter.exitApplication = () => {
            #if (UNITY_EDITOR)
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        };
    }



    private void setUpCreateMazeMenu()
    {
        createMazePresenter = new createMazePresenter(createMazeMenu);

        // enables action for returning to main menu from Create Maze
        createMazePresenter.openMainMenu = () => ToggleMenu(mainMenu);
    }

    private void setUpCreateExperimentMenu()
    {
        createExperimentPresenter = new createExperimentPresenter(createExperimentMenu);

        // enables action for returning to main menu from Create Experiment
        createExperimentPresenter.openMainMenu = () => ToggleMenu(mainMenu);
        createExperimentPresenter.saveConditions = () => ToggleMenu(createMazeMenu);
        mainMenuPresenter.openCreateExperiment = () => createExperimentPresenter.resetCreateExperimentPage();
        createExperimentPresenter.saveConditions = () => createMazePresenter.setUpExperimentInCreateMazeUI();
    }

    private void setUpLoadExperimentMenu()
    {
        loadExperimentPresenter = new loadExperimentPresenter(loadExperimentMenu);

        // enables action for returning to main menu from Load Experiment
        loadExperimentPresenter.openMainMenu = () => ToggleMenu(mainMenu);
    }

    private void setUpMazePlaybackMenu()
    {
        menuPresenter = new mazePlaybackPresenter(mazePlayback);
        menuPresenter.openMainMenu = () => ToggleMenu(mainMenu);
    }

    private void ToggleMenu(VisualElement menuToEnable)
    {
        mainMenu.Display(false);
        createMazeMenu.Display(false);
        mazePlayback.Display(false);
        createExperimentMenu.Display(false);
        loadExperimentMenu.Display(false);

        menuToEnable.Display(true);
    }
}
