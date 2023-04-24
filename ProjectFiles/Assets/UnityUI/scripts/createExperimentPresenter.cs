using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class createExperimentPresenter
{
    public Action openMainMenu { set => returnToMainButton.clicked += value; }
    public Action saveConditions { set => saveConditionButton.clicked += value; }

    private Button returnToMainButton, saveConditionButton, addNewConditionButton;
    private TextField experimentNameField;
    private ScrollView conditionsScrollView;
    private VisualTreeAsset conditionTemplate;
    public string defaultExperimentName = "Experiment Name", defaultConditionName = "Condition Name";

    public createExperimentPresenter(VisualElement root)
    {
        returnToMainButton = root.Q<Button>("returnToMainButton");

        saveConditionButton = root.Q<Button>("saveConditionButton");
        saveConditionButton.clicked += () =>
        {


            string experimentName = experimentNameField.text;
            ExperimentDataController.instance.setExperimentId(experimentName);

            List<string> conditions = FinalizeConditionList(conditionsScrollView.contentContainer.Children());
            ExperimentDataController.instance.setNewConditionList(conditions);

            CsvManager.instance.storeConditionInfo(conditions.Select(x => x).ToList(), experimentDir: experimentName);


        };

        experimentNameField = root.Q<TextField>("experimentNameField");
        experimentNameField.RegisterCallback<FocusOutEvent>(validateExperimentName);
        defaultExperimentName = experimentNameField.text;

        conditionTemplate = Resources.Load<VisualTreeAsset>("UnityUI/Condition");



        conditionsScrollView = root.Q<ScrollView>("conditionsScrollView");
        // an initial condition is added to the scrollview
        // use it to get the default condition name
        defaultConditionName = AddNewCondition(conditionsScrollView.contentContainer).Q<TextField>("conditionNameField").text;

        addNewConditionButton = root.Q<Button>("addNewConditionButton");
        addNewConditionButton.clicked += () => AddNewCondition(conditionsScrollView.contentContainer);
    }
    public void resetCreateExperimentPage()
    {
        experimentNameField.value = defaultExperimentName;

        int count = conditionsScrollView.contentContainer.childCount;

        for (int i = 0; i < count; ++i)
            conditionsScrollView.contentContainer.RemoveAt(conditionsScrollView.contentContainer.childCount - 1);

        AddNewCondition(conditionsScrollView.contentContainer);
    }

    public void validateExperimentName(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);
        bool invalidName = false;

        try
        {
            string p = Path.GetFullPath(inputBox.text);
        }
        catch
        {
            invalidName = true;
        }

        inputBox.value = Regex.Replace(inputBox.value, @"[^\w]*", String.Empty);

        if (string.IsNullOrWhiteSpace(inputBox.text) || invalidName)
        {
            inputBox.value = defaultExperimentName;
        }
    }

    public VisualElement AddNewCondition(VisualElement root)
    {
        VisualElement spawnedCondition = conditionTemplate.Instantiate();
        root.Add(spawnedCondition);
        HandleCondition(spawnedCondition);
        // doesnt work, you have to wait until its finished drawing or something
        // conditionsScrollView.ScrollTo(spawnedCondition);
        return spawnedCondition;
    }

    public void HandleCondition(VisualElement condition)
    {
        Button deleteConditionButton = condition.Q<Button>("deleteConditionButton");
        deleteConditionButton.clicked += () => DeleteCondition(condition);

        TextField conditionNameField = condition.Q<TextField>("conditionNameField");
        conditionNameField.RegisterCallback<FocusOutEvent>(validateConditionName);
    }

    public void validateConditionName(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);
        if (string.IsNullOrWhiteSpace(inputBox.text))
        {
            inputBox.value = defaultConditionName;
        }
    }

    public void DeleteCondition(VisualElement condition)
    {
        // experiment must have at least one condition
        if (condition.parent.childCount > 1)
        {
            condition.RemoveFromHierarchy();
        }
    }

    public class Condition
    {
        public string name;

        override public string ToString()
        {
            return name;
        }
    }

    public List<String> FinalizeConditionList(IEnumerable<VisualElement> conditions)
    {
        HashSet<String> uniqueConditionNames = new HashSet<string>();

        foreach (VisualElement condition in conditions)
        {
            TextField conditionNameField = condition.Q<TextField>("conditionNameField");
            uniqueConditionNames.Add(conditionNameField.text);
        }

        return uniqueConditionNames.ToList();
    }

}