using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using SFB;

public class loadExperimentPresenter
{
    public Action openMainMenu { set => returnToMainButton.clicked += value; }
    public Action saveConditions { set => confirmSelectionButton.clicked += value; }
    private Button returnToMainButton, confirmSelectionButton, importExperimentButton;
    private Label experimentNameField;
    private ScrollView conditionsScrollView;
    private VisualTreeAsset conditionTemplate;
    public string defaultExperimentName = "Experiment Name", defaultConditionName = "Condition Name";
    public TextField participantIDField;
    List<VisualElement> listOfButtons;
    int currentButtonIndex;

    String path;

    public loadExperimentPresenter(VisualElement root)
    {
        returnToMainButton = root.Q<Button>("returnToMainButton");

        confirmSelectionButton = root.Q<Button>("confirmSelectionButton");
        experimentNameField = root.Q<Label>("experimentNameField");
        //experimentNameField.RegisterCallback<FocusOutEvent>(validateExperimentName);
        defaultExperimentName = experimentNameField.text;

        conditionTemplate = Resources.Load<VisualTreeAsset>("UnityUI/Condition");

        conditionsScrollView = root.Q<ScrollView>("conditionsScrollView");
        // an initial condition is added to the scrollview
        // use it to get the default condition name
        //defaultConditionName = AddNewCondition(conditionsScrollView.contentContainer).Q<TextField>("conditionNameField").text;

        importExperimentButton = root.Q<Button>("importExperimentButton");
        participantIDField = root.Q<TextField>("participantIDField");
        participantIDField.RegisterCallback<FocusOutEvent>(validateParticipantID);

        importExperimentButton.clicked += () => LoadExperiment();

        confirmSelectionButton.clicked += loadExperimentConditionToMaze;
        confirmSelectionButton.SetEnabled(false);

    }

    private bool validExperiment, validParticipantID;
    private void UpdateValidations(bool experiment, bool participant)
    {
        validExperiment = experiment;
        validParticipantID = participant;
        confirmSelectionButton.SetEnabled(validExperiment && validParticipantID);
    }

    private void validateParticipantID(FocusOutEvent evt)
    {
        if (participantIDField.text.Length != 0)
            UpdateValidations(validExperiment, true);
        else
            UpdateValidations(validExperiment, false);
    }
    string experimentName;
    private void LoadExperiment()
    {

        var folderPanelResult = StandaloneFileBrowser.OpenFolderPanel("Load Experiment", "", false);

        if (folderPanelResult == null || folderPanelResult.Length <= 0)
            return;

        path = folderPanelResult[0];

        if (path.Length == 0)
            return;

        //Get the list of conditions 
        List<string> conditions = CsvManager.instance.getConditionListFromFile(path);

        //If its blank than this is not a valid experiement dir 
        if (conditions.Count < 1)
        {
            //They didn't choose a valid experiment directory
            experimentNameField.text = "Invalid Directory Selected";

            for (int i = conditionsScrollView.contentContainer.childCount; i > 0; --i)
                conditionsScrollView.contentContainer.RemoveAt(i - 1);

            UpdateValidations(false, validParticipantID);
            return;
        }

        UpdateValidations(true, validParticipantID);



        for (int i = conditionsScrollView.contentContainer.childCount; i > 0; --i)
            conditionsScrollView.contentContainer.RemoveAt(i - 1);

        experimentName = path.Substring(path.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
        experimentNameField.text = experimentName;



        int index = 0;

        foreach (string condition in conditions)
        {
            Button spawnedCondition = new Button();
            spawnedCondition.text = condition;
            spawnedCondition.name = index.ToString();
            spawnedCondition.AddToClassList("conditionsButton");

            conditionsScrollView.contentContainer.Add(spawnedCondition);
            spawnedCondition.RegisterCallback<ClickEvent>(evt =>
            {
                currSelectedConditionButton = evt.target as Button;

                int buttonIndex = int.Parse(currSelectedConditionButton.name);

                if (buttonIndex == currentButtonIndex) return;

                currSelectedConditionButton.RemoveFromClassList("conditionsButton");
                currSelectedConditionButton.AddToClassList("conditionsButtonSelected");

                listOfButtons[currentButtonIndex].RemoveFromClassList("conditionsButtonSelected");
                listOfButtons[currentButtonIndex].AddToClassList("conditionsButton");

                currentButtonIndex = buttonIndex;
            });

            index++;
        }

        listOfButtons = conditionsScrollView.contentContainer.Children().ToList();
        currentButtonIndex = 0;
        listOfButtons[0].RemoveFromClassList("conditionsButton");
        listOfButtons[0].AddToClassList("conditionsButtonSelected");
        currSelectedConditionButton = listOfButtons[0] as Button;
    }

    public class Condition
    {
        public string name;

        override public string ToString()
        {
            return name;
        }
    }

    private Button currSelectedConditionButton;
    private void loadExperimentConditionToMaze()
    {
        if (currSelectedConditionButton == null)
        {
            Debug.LogError("No condition selected");
            return;
        }
        string participantID = participantIDField.text;


        ExperimentDataController.instance.loadMazeFromCondition(path, currSelectedConditionButton.text, participantID);
        CsvManager.instance.setUpParticipantFileStructure(path, participantID, currSelectedConditionButton.text);
        SceneManagerScript.instance.LoadScene("Maze_Generation");

    }

}