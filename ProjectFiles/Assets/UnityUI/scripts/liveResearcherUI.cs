using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class liveResearcherUI : MonoBehaviour
{
    VisualElement sidePanel;

    Dictionary<string, VisualElement> groupBoxMap;

    Dictionary<string, Label> dataMap;

    // singleton
    public static liveResearcherUI instance;

    private Button abortButton;

    VisualElement confirmModal;
    Button confirmModalYesButton, confirmModalNoButton;

    void Awake()
    {
        if (instance != null)
        {
            //Delete the duplicate
            Destroy(instance.gameObject);
        }
        instance = this;

        StartPieceScript.startPieceEntered += UpdateWrapper;

    }

    private void UpdateWrapper(GameObject o)
    {
        StartCoroutine(startUIUpdates());
    }

    void OnDestroy()
    {
        StartPieceScript.startPieceEntered -= UpdateWrapper;
        StopCoroutine("startUIUpdates");
    }

    private IEnumerator startUIUpdates(int rateInSeconds = 1)
    {
        while (true)
        {
            UpdateResearcherUI();
            yield return new WaitForSeconds(rateInSeconds);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SetupUI();
    }

    private bool hasSetUpUI = false;
    private void SetupUI()
    {
        if (hasSetUpUI)
            return;
        hasSetUpUI = true;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        groupBoxMap = new Dictionary<string, VisualElement>();
        dataMap = new Dictionary<string, Label>();

        sidePanel = root.Q<VisualElement>("unity-content-container");

        AddDataGroup("Experiment Info");
        AddNewDataBoxToGroup("Experiment Info", "Participant ID", "0");
        AddNewDataBoxToGroup("Experiment Info", "Experiment Name", "0");
        AddNewDataBoxToGroup("Experiment Info", "Condition Name", "0");

        AddDataGroup("General Info");
        AddNewDataBoxToGroup("General Info", "Time Elapsed", "0m 0s");
        //AddNewDataBoxToGroup("General Info", "Time Remaining", "1m 6s");
        AddNewDataBoxToGroup("General Info", "Distance Remaining", "0");
        //AddNewDataBoxToGroup("General Info", "Longest Path", "153");

        AddDataGroup("Maze Info");
        AddNewDataBoxToGroup("Maze Info", "Pieces Generated", "0");
        AddNewDataBoxToGroup("Maze Info", "Impossible Spaces", "0");
        AddNewDataBoxToGroup("Maze Info", "Straights", "0");
        AddNewDataBoxToGroup("Maze Info", "Four-Ways", "0");
        AddNewDataBoxToGroup("Maze Info", "Left Turns", "0");
        AddNewDataBoxToGroup("Maze Info", "Right Turns", "0");
        AddNewDataBoxToGroup("Maze Info", "Three-Way Left-Straights", "0");
        AddNewDataBoxToGroup("Maze Info", "Three-Way Right-Straights", "0");
        AddNewDataBoxToGroup("Maze Info", "Three-Way Left-Rights", "0");
        //AddNewDataBoxToGroup("Maze Info", "Vertical Shafts", "0");


        AddDataGroup("Movement Info");
        // AddNewDataBoxToGroup("Movement Info", "Time Idle", "32s");
        // AddNewDataBoxToGroup("Movement Info", "Time paused", "8s");
        AddNewDataBoxToGroup("Movement Info", "Backtrack Occurances", "0");

        // updateData("Three-Way Rights", "99");

        // AddDataGroup("RANDOM");
        // AddNewDataBoxToGroup("RANDOM", "R", "32s");
        // AddNewDataBoxToGroup("RANDOM", "N", "8s");
        // AddNewDataBoxToGroup("RANDOM", "G", "30");

        abortButton = root.Q<Button>("abortButton");
        confirmModal = root.Q<VisualElement>("overlay");

        abortButton.clicked += () => confirmModal.style.display = DisplayStyle.Flex;

        confirmModalYesButton = confirmModal.Q<Button>("confirmModalYesButton");
        confirmModalYesButton.clicked += () =>
        {
            confirmModal.style.display = DisplayStyle.None;
            MazeGenerator.instance.EndMaze();
            abortButton.text = "Aborting...";
            abortButton.SetEnabled(false);
        };

        confirmModalNoButton = confirmModal.Q<Button>("confirmModalNoButton");
        confirmModalNoButton.clicked += () => confirmModal.style.display = DisplayStyle.None;

    }

    VisualElement createDataInput(string dataTitle, string dataValue)
    {
        VisualElement infoBox = new VisualElement();
        infoBox.AddToClassList("infoBox");

        Label dataTitleLabel = new Label();
        dataTitleLabel.AddToClassList("dataLabel");
        dataTitleLabel.text = dataTitle;

        Label dataEntryLabel = new Label();
        dataEntryLabel.AddToClassList("dataEntry");
        dataEntryLabel.text = dataValue;

        infoBox.Add(dataTitleLabel);
        infoBox.Add(dataEntryLabel);

        dataMap.Add(dataTitle, dataEntryLabel);

        return infoBox;
    }

    public void AddDataGroup(string groupName)
    {
        // creates the container
        VisualElement groupBox = new VisualElement();
        groupBox.AddToClassList("infoGroupBox");

        // create and add the title to the container
        Label groupTitle = new Label();
        groupTitle.AddToClassList("sectionLabel");
        groupTitle.text = groupName;
        groupBox.Add(groupTitle);

        // add the container to sidePanel
        sidePanel.Add(groupBox);

        // store the container in the map
        groupBoxMap.Add(groupName, groupBox);

    }

    public void AddNewDataBoxToGroup(string groupName, string dataTitle, string dataVal)
    {
        VisualElement ret;
        groupBoxMap.TryGetValue(groupName, out ret);

        if (ret == default(VisualElement))
            return;

        ret.Add(createDataInput(dataTitle, dataVal));
    }

    public void updateData(string dataName, string newVal)
    {
        if (!hasSetUpUI)
            SetupUI();
        Label ret;
        dataMap.TryGetValue(dataName, out ret);

        if (ret == default(Label))
            return;

        ret.text = newVal;
    }

    /// <summary>
    /// Aggregates relevent data from the game and updates the researcher UI
    /// </summary>
    private void UpdateResearcherUI()
    {
        var spawnedPieces = PieceManager.instance.spawnedPieces;
        int remainingDistance = Mathf.Max(0, (MazeParameterManager.instance.mazeDistance - (PieceManager.instance.UserPathPieces?.Count ?? 0)));
        liveResearcherUI.instance.updateData("Distance Remaining", remainingDistance.ToString());
        liveResearcherUI.instance.updateData("Pieces Generated", spawnedPieces.Count.ToString());
        liveResearcherUI.instance.updateData("Impossible Spaces", PieceManager.Piece.ImpossibleSpace.impossiblespaceCount.ToString());

        liveResearcherUI.instance.updateData("Straights", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.Straight).ToString());
        liveResearcherUI.instance.updateData("Four-Ways", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.FourWay).ToString());
        liveResearcherUI.instance.updateData("Left Turns", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.LeftTurn).ToString());
        liveResearcherUI.instance.updateData("Right Turns", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.RightTurn).ToString());
        liveResearcherUI.instance.updateData("Three-Way Left-Straights", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.ThreeWayLeftStraight).ToString());
        liveResearcherUI.instance.updateData("Three-Way Right-Straights", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.ThreeWayRightStraight).ToString());
        liveResearcherUI.instance.updateData("Three-Way Left-Rights", spawnedPieces.Values.Count(x => x.junctionType == JunctionTypes.ThreeWayLeftRight).ToString());

        liveResearcherUI.instance.updateData("Backtrack Occurances", spawnedPieces.Values.Aggregate(0, (acc, x) => acc + x.backtrackedOccurances).ToString());

        int totalSeconds = (int)MazeGenerator.instance.GetElapsedTime();
        int seconds = totalSeconds % 60;
        int minutes = totalSeconds / 60;
        string time = $"{minutes}m {seconds}s";
        liveResearcherUI.instance.updateData("Time Elapsed", time);
    }

}
