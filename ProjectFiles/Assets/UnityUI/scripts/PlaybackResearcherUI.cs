using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

public class PlaybackResearcherUI : MonoBehaviour
{
    VisualElement sidePanel;
    VisualElement bigSidePanel;
    VisualElement miniSidePanel;

    Dictionary<string, VisualElement> groupBoxMap;
    Dictionary<string, Label> dataMap;

    PieceManager pieceManager;

    SliderInt sliderUI;
    Button mainMenuButton;

    TextField sliderVisualValueDisplay;

    Button autoPlayButton;
    Button autoPauseButton;

    Coroutine currentPlayBackCoroutine;

    int playSpeed = 1;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        pieceManager = GameObject.FindGameObjectWithTag("UserReplay").GetComponent<PieceManager>();

        groupBoxMap = new Dictionary<string, VisualElement>();
        dataMap = new Dictionary<string, Label>();

        // sidePanel = root.Q<VisualElement>("unity-content-container");

        bigSidePanel = root.Q<VisualElement>("sidePanel");
        miniSidePanel = root.Q<VisualElement>("miniSidePanel");

        root.Q<Button>("MinButton").clicked += shrinkSideBar;
        root.Q<Button>("MaxButton").clicked += expandSideBar;

        sliderUI = root.Q<SliderInt>("timelineSlider");
        sliderUI.RegisterCallback<ChangeEvent<int>>(onSliderChange);

        root.Q<Button>("leftSliderButton").clicked += () => sliderUI.value -= 1;
        root.Q<Button>("leftSliderButton").clicked += () => pieceManager.UserPathStepBackward();

        root.Q<Button>("rightSliderButton").clicked += () => sliderUI.value += 1;
        root.Q<Button>("rightSliderButton").clicked += () => pieceManager.UserPathStepForward();

        yield return new WaitUntil(() => pieceManager.userPath != null);
        sliderUI.highValue = pieceManager.userPath.Count - 1;

        sliderVisualValueDisplay = root.Q<TextField>("sliderValueDisplay");
        sliderVisualValueDisplay.RegisterCallback<FocusOutEvent>(sliderValueManuallyEntered);

        root.Q<Label>("sliderHighValueDisplay").text = sliderUI.highValue.ToString();

        root.Q<Button>("returnToMainButton").clicked += () => SceneManagerScript.instance.LoadScene(SceneManagerScript.instance.startingScene);

        autoPlayButton = root.Q<Button>("autoPlayButton");
        autoPauseButton = root.Q<Button>("autoPauseButton");

        autoPlayButton.clicked += autoPlayback;
        autoPauseButton.clicked += stopPlayback;

        root.Q<TextField>("stepsPerSecondInput").RegisterCallback<FocusOutEvent>(setPlayBackSpeed);
    }

    void onSliderChange(ChangeEvent<int> evt)
    {
        sliderVisualValueDisplay.value = evt.newValue.ToString();
        pieceManager.UserPathStepToPosition(evt.newValue);
    }

    void sliderValueManuallyEntered(FocusOutEvent evt)
    {
        TextField inputBox = evt.target as TextField;

        int temp;

        if (int.TryParse(inputBox.value, out temp))
        {
            if (temp < 0)
                temp = 0;

            else if (temp > sliderUI.highValue)
                temp = sliderUI.highValue;

            sliderUI.value = temp;
        }

        else
        {
            sliderUI.value = 0;
        }


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
        Label ret;
        dataMap.TryGetValue(dataName, out ret);

        if (ret == default(Label))
            return;

        ret.text = newVal;
    }

    public void shrinkSideBar()
    {
        bigSidePanel.Display(false);
        miniSidePanel.Display(true);
    }

    public void expandSideBar()
    {
        bigSidePanel.Display(true);
        miniSidePanel.Display(false);
    }

    private void autoPlayback()
    {
        autoPlayButton.Display(false);
        autoPauseButton.Display(true);
        currentPlayBackCoroutine = StartCoroutine(runPlayBack());
    }

    IEnumerator runPlayBack()
    {
        while (sliderUI.value < sliderUI.highValue)
        {
            sliderUI.value += 1;
            yield return new WaitForSeconds(1.0f / playSpeed);
        }

        stopPlayback();
    }

    private void stopPlayback()
    {
        if (currentPlayBackCoroutine != null)
            StopCoroutine(currentPlayBackCoroutine);
        autoPlayButton.Display(true);
        autoPauseButton.Display(false);
    }

    void setPlayBackSpeed(FocusOutEvent evt)
    {
        TextField inputbox = evt.target as TextField;

        if (int.TryParse(inputbox.value, out playSpeed))
        {
            if (playSpeed < 1)
            {
                playSpeed = 1;
                inputbox.value = playSpeed.ToString();
            }
        }

        else
        {
            playSpeed = 1;
            inputbox.value = playSpeed.ToString();
        }
    }

}
