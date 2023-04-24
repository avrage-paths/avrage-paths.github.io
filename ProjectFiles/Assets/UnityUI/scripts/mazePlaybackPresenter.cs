using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using SFB;

public class mazePlaybackPresenter
{
    public Action openMainMenu { set => returnToMainButton.clicked += value; }
    public Action saveConditions { set => confirmSelectionButton.clicked += value; }
    private Button returnToMainButton, confirmSelectionButton, importMazeButton;
    private Label experimentNameLabel, conditionNameLabel, participantNameLabel, timestampLabel;

    public mazePlaybackPresenter(VisualElement root)
    {
        returnToMainButton = root.Q<Button>("returnToMainButton");

        confirmSelectionButton = root.Q<Button>("startPlaybackButton");
        // an initial condition is added to the scrollview
        // use it to get the default condition name
        //defaultConditionName = AddNewCondition(conditionsScrollView.contentContainer).Q<TextField>("conditionNameField").text;

        importMazeButton = root.Q<Button>("importMazeButton");
        importMazeButton.clicked += ImportPlayback;

        conditionNameLabel = root.Q<Label>("conditionNameLabel");
        experimentNameLabel = root.Q<Label>("experimentNameLabel");
        participantNameLabel = root.Q<Label>("participantNameLabel");
        timestampLabel = root.Q<Label>("timestampLabel");

        confirmSelectionButton.clicked += LoadPlayback;
        confirmSelectionButton.SetEnabled(false);

    }
    string mazePath;


    public void ImportPlayback()
    {
        mazePath = StandaloneFileBrowser.OpenFilePanel("Choose Maze JSON", "", "json", false)?.FirstOrDefault();
        if (string.IsNullOrEmpty(mazePath))
        {
            conditionNameLabel.text = participantNameLabel.text = timestampLabel.text = experimentNameLabel.text = "ERROR GETTING FILE";
            confirmSelectionButton.SetEnabled(false);
            return;
        }
        confirmSelectionButton.SetEnabled(true);

        // get the folder that the file pointed to by mazePath is in
        // so if the directory is ...User/MazePlayback/MyMaze.json, we want to get MazePlayback
        string subdir = System.IO.Path.GetDirectoryName(mazePath);
        string conditionName = subdir.Substring(subdir.LastIndexOf("\\") + 1);
        string timestamp = System.Text.RegularExpressions.Regex.Match(conditionName, @"(?<=_).*$").Value;
        // remove alphanumerics from the condition name using regex
        conditionName = System.Text.RegularExpressions.Regex.Replace(conditionName, "[^a-zA-Z]", "");

        subdir = System.IO.Path.GetDirectoryName(subdir);
        string participantName = subdir.Substring(subdir.LastIndexOf("\\") + 1);

        subdir = System.IO.Path.GetDirectoryName(subdir);
        string experimentName = subdir.Substring(subdir.LastIndexOf("\\") + 1);

        conditionNameLabel.text = conditionName;
        experimentNameLabel.text = experimentName;
        participantNameLabel.text = participantName;
        timestampLabel.text = timestamp;


    }

    public void LoadPlayback()
    {
        if (mazePath.Length != 0)
        {
            MazePlaybackManager.instance.LoadMazeAsync(mazePath);
        }
    }

}