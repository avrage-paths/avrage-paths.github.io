using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;


public class mainMenuPresenter
{
    public Action openMazePlayback { set => mazePlaybackButton.clicked += value; }
    public Action openLoadExperiment { set => loadExperimentButton.clicked += value; }
    public Action openCreateExperiment { set => createExperimentButton.clicked += value; }
    public Action exitApplication { set => exitButton.clicked += value; }
    private Button createMazeButton;
    private Button mazePlaybackButton;
    private Button createExperimentButton;
    private Button loadExperimentButton;
    private Button exitButton;

    public mainMenuPresenter(VisualElement root)
    {
        createMazeButton = root.Q<Button>("createMazeButton");
        mazePlaybackButton = root.Q<Button>("mazePlaybackButton");
        createExperimentButton = root.Q<Button>("createExperimentButton");
        loadExperimentButton = root.Q<Button>("loadExperimentButton");
        exitButton = root.Q<Button>("exitButton");

    }
}
