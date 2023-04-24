using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

public class createMazePresenter
{
    public Action openMainMenu { set => confirmModalYesButton.clicked += value; }
    private Button returnToMenuButton;

    List<TextField> parameterInputElements;
    List<Toggle> randomInputElements;
    List<TextField> straightInputElements;

    List<Button> conditionsList;

    List<CompletedCondition> completedConditions;

    private int currentConditionIndex;

    private int completeConditionCount = 0;

    private int junctionCount = 0;

    private Button percentageToggleButton;
    private bool percentageFlag;

    private VisualElement percentageSumDisplay;
    private Label currentPercentageDisplay;
    private float percentageSumFloat = 0;

    private VisualElement totalDistanceSumDisplay;

    private Label currentDistanceDisplay;

    private long totalDistanceSumInt = 1;


    private TextField distanceDisplay;
    private int distanceEntered = 1;

    private Button generateButton;
    private int generateButtonIssues = 0;

    private int randomTogglesOn;

    private Toggle explicitToggle;

    Label experimentName;

    Button saveExperimentButton;

    VisualElement conditionsSideBar;

    private List<Button> tabButtonList;

    private List<DropdownField> dropdowns;

    private Toggle enableCeiling;

    private TextField wallHeight;

    private DropdownField locomotionMethodDropdown;

    VisualElement pathPieceDisplay;

    VisualElement visualSettingDisplay;

    VisualElement userSettingDisplay;

    GroupBox ceilingTextureHolder, wallTextureHolder, floorTextureHolder;

    VisualElement confirmModal;
    Button confirmModalYesButton, confirmModalNoButton;

    public createMazePresenter(VisualElement root)
    {
        returnToMenuButton = root.Q<Button>("returnToMainButton");
        returnToMenuButton.clicked += () => confirmModal.style.display = DisplayStyle.Flex;

        confirmModal = root.Q<VisualElement>("overlay");

        confirmModalYesButton = confirmModal.Q<Button>("confirmModalYesButton");
        confirmModalYesButton.clicked += () => confirmModal.style.display = DisplayStyle.None;
        confirmModalYesButton.clicked += () => {        
            saveExperimentButton.clicked -= saveExperiment;
            saveExperimentButton.style.backgroundColor = new Color((float)0.2, (float)0.05882353, (float)0.05882353);
        };

        confirmModalNoButton = confirmModal.Q<Button>("confirmModalNoButton");
        confirmModalNoButton.clicked += () => confirmModal.style.display = DisplayStyle.None;

        percentageSumDisplay = root.Q<VisualElement>("currentPercentageBox");
        currentPercentageDisplay = percentageSumDisplay.Q<Label>("currentPercentageDisplay");

        totalDistanceSumDisplay = root.Q<VisualElement>("currentDistanceBox");
        currentDistanceDisplay = totalDistanceSumDisplay.Q<Label>("currentDistanceDisplay");

        parameterInputElements = root.Query<TextField>(className: "parameterInput").ToList();
        parameterInputElements.ForEach(elem => elem.RegisterCallback<FocusOutEvent>(checkIfInputValid));
        parameterInputElements.ForEach(elem => elem.RegisterCallback<FocusInEvent>(eraseOldInputFromSum));

        straightInputElements = root.Query<TextField>(className: "minmaxInput").ToList();
        straightInputElements.ForEach(elem => elem.RegisterCallback<FocusOutEvent>(checkIfMinMaxStraightInputValid));

        randomInputElements = root.Query<Toggle>(className: "randomToggleInput").ToList();
        randomInputElements.ForEach(elem => elem.RegisterCallback<ClickEvent>(cleanInputIfRandomToggle));
        randomTogglesOn = randomInputElements.Count;

        distanceDisplay = root.Q<TextField>("distanceInput");
        distanceDisplay.RegisterCallback<FocusOutEvent>(checkDistanceInput);

        generateButton = root.Q<Button>("saveConditionButton");
        generateButton.clicked += saveCurrentCondition;

        explicitToggle = root.Q<Toggle>("explicitToggle");

        experimentName = root.Q<Label>("experimentName");

        conditionsSideBar = root.Q<ScrollView>("conditionBox").Q<VisualElement>("unity-content-container");

        saveExperimentButton = root.Q<Button>("saveExperimentButton");

        setUpPercentToggleButton(root);

        setUpStraightInt();

        setUpTabButtons(root);

        setUpVisualSettingTab(visualSettingDisplay);

        setUpUserSettingTab(userSettingDisplay);

    }

    private void setUpVisualSettingTab(VisualElement root)
    {
        List<String> listOfTextures = MazeDataController.instance.getAllTexturesFromResources().ToList();

        dropdowns = root.Query<DropdownField>().ToList();

        dropdowns[0].choices = listOfTextures;
        ceilingTextureHolder = root.Q<GroupBox>("ceilingTexturePreview");
        SetupTexturePreviews(dropdowns[0], ceilingTextureHolder);

        dropdowns[1].choices = listOfTextures;
        wallTextureHolder = root.Q<GroupBox>("wallTexturePreview");
        SetupTexturePreviews(dropdowns[1], wallTextureHolder);

        dropdowns[2].choices = listOfTextures;
        floorTextureHolder = root.Q<GroupBox>("floorTexturePreview");
        SetupTexturePreviews(dropdowns[2], floorTextureHolder);

        enableCeiling = root.Q<Toggle>("ceilingToggle");

        wallHeight = root.Q<TextField>("wallHeightInput");

        wallHeight.RegisterCallback<FocusOutEvent>(checkNumberInput);

    }

    public void SetupTexturePreviews(DropdownField dropdown, VisualElement textureHolder)
    {
        Image imageTexture = new Image();
        imageTexture.scaleMode = ScaleMode.StretchToFill;
        dropdown.value = dropdown.choices[0];
        imageTexture.image = Resources.Load<Texture2D>($"Materials/{dropdown.value}");
        textureHolder.Add(imageTexture);
        dropdown.RegisterValueChangedCallback(evt =>
        {
            imageTexture.image = Resources.Load<Texture2D>($"Materials/{evt.newValue}");
        });
    }

    private void setUpUserSettingTab(VisualElement root)
    {
        locomotionMethodDropdown = root.Q<DropdownField>();

        locomotionMethodDropdown.choices = MazeDataController.instance.getAllLocomotionMethods();

        locomotionMethodDropdown.value = locomotionMethodDropdown.choices[0];
    }

    private void setUpTabButtons(VisualElement root)
    {
        tabButtonList = root.Query<Button>(className: "navbarButton").ToList();

        pathPieceDisplay = root.Q<VisualElement>("pathPanelDisplay");

        visualSettingDisplay = root.Q<GroupBox>("visualPanelDisplay");

        userSettingDisplay = root.Q<GroupBox>("userPanelDisplay");

        tabButtonList[0].RegisterCallback<ClickEvent>(evt =>
        {
            pathPieceDisplay.Display(true);
            visualSettingDisplay.Display(false);
            userSettingDisplay.Display(false);
        });

        tabButtonList[1].RegisterCallback<ClickEvent>(evt =>
        {
            pathPieceDisplay.Display(false);
            visualSettingDisplay.Display(true);
            userSettingDisplay.Display(false);
        });

        tabButtonList[2].RegisterCallback<ClickEvent>(evt =>
        {
            pathPieceDisplay.Display(false);
            visualSettingDisplay.Display(false);
            userSettingDisplay.Display(true);
        });
    }

    public void setUpExperimentInCreateMazeUI()
    {
        clearAllPieceInputField();

        // reset the conditions bar
        int conditionsCount = conditionsSideBar.childCount;
        for (int i = 0; i < conditionsCount; ++i)
            conditionsSideBar.RemoveAt(conditionsSideBar.childCount - 1);


        completedConditions = new List<CompletedCondition>();
        ExperimentDataController expData = ExperimentDataController.instance;
        experimentName.text = expData.getExperimentId();
        conditionsList = setUpConditionsButtons(expData.getConditions());
    }

    private List<Button> setUpConditionsButtons(List<String> conditionNames)
    {
        List<Button> conditionButtons = new List<Button>();

        for (int i = 0; i < conditionNames.Count; ++i)
        {
            Button b = new Button();
            b.AddToClassList("conditionsButton");
            b.text = conditionNames[i];
            b.name = i.ToString();

            VisualElement checkmark = new VisualElement();
            checkmark.AddToClassList("incompleteCheckmark");

            b.Add(checkmark);

            b.RegisterCallback<ClickEvent>(switchToThiscondition);

            conditionsSideBar.Add(b);
            conditionButtons.Add(b);

            // create a spot for the condition to be saved in
            completedConditions.Add(new CompletedCondition());
            completedConditions[i].completedFlag = false;
        }

        currentConditionIndex = 0;
        completeConditionCount = 0;

        conditionButtons[currentConditionIndex].RemoveFromClassList("conditionsButton");
        conditionButtons[currentConditionIndex].AddToClassList("conditionsButtonSelected");

        return conditionButtons;
    }

    private void switchToThiscondition(ClickEvent evt)
    {
        conditionsList[currentConditionIndex].RemoveFromClassList("conditionsButtonSelected");
        conditionsList[currentConditionIndex].AddToClassList("conditionsButton");

        completedConditions[currentConditionIndex] = saveAllInputEntries();
        completedConditions[currentConditionIndex].completedFlag = true;


        Button b = (evt.target as Button);

        currentConditionIndex = int.Parse(b.name);

        b.RemoveFromClassList("conditionsButton");
        b.AddToClassList("conditionsButtonSelected");

        if (completedConditions[currentConditionIndex].completedFlag)
            fillInputsFromStoredCondition();
    }

    private void fillInputsFromStoredCondition()
    {
        // reset all fields
        clearAllPieceInputField();

        CompletedCondition condition = completedConditions[currentConditionIndex];

        distanceEntered = condition.distance;

        // if this condition was set to percents, and we're currently not on percents
        if (condition.dataRep == MazeDataController.DataRepresentations.Percentage && !percentageFlag)
        {
            onClickTogglePercentButton();

            // set total percentage from pieces display
            percentageSumFloat = 0;
            addValueToInfoBox(condition.totalPercentFromPieces);
        }

        else
        {
            // set total distance sum display
            totalDistanceSumInt = 0;
            addValueToInfoBox(condition.totalDistanceFromPieces);
        }

        distanceDisplay.style.backgroundColor = Color.clear;
        distanceDisplay.value = condition.distance.ToString();

        straightInputElements[0].value = condition.minStraights.ToString();
        straightInputElements[1].value = condition.maxStraights.ToString();

        List<float> listOfInputs = condition.pathPieceValues;
        List<bool> listOfRandoms = condition.pathPieceRandomBools;

        explicitToggle.value = condition.explicitToggle;

        for (int i = 0; i < listOfInputs.Count; ++i)
        {
            randomInputElements[i].value = listOfRandoms[i];

            parameterInputElements[i].style.backgroundColor = Color.clear;

            // if this was set to random
            if (listOfRandoms[i])
                parameterInputElements[i].value = "";

            else if (percentageFlag)
                parameterInputElements[i].value = listOfInputs[i].ToString() + "%";

            else
                parameterInputElements[i].value = ((int)listOfInputs[i]).ToString();

        }

        // set up visual tab
        dropdowns[0].value = condition.ceilTexture;
        dropdowns[1].value = condition.wallTexture;
        dropdowns[2].value = condition.floorTexture;

        enableCeiling.value = condition.enableCeilFlag;

        wallHeight.value = condition.wallHeight.ToString();

        // set up User-tab
        locomotionMethodDropdown.value = condition.locomotionMethod;

    }

    private void setUpStraightPercent()
    {
        randomInputElements[0].value = false;
        parameterInputElements[0].value = "50%";
        addValueToInfoBox((float)50);
    }

    private void setUpStraightInt()
    {
        int temp = distanceEntered / 2;

        if (distanceEntered % 2 == 1)
            ++temp;

        int currentStraightVal;

        if (int.TryParse(parameterInputElements[0].value, out currentStraightVal))
            temp = temp > currentStraightVal ? temp : currentStraightVal;

        if (temp <= distanceEntered && temp > 0)
        {
            if (parameterInputElements[0].style.backgroundColor == Color.red)
                enableGenerateButton();

            parameterInputElements[0].style.backgroundColor = Color.clear;
        }


        randomInputElements[0].value = false;
        parameterInputElements[0].value = (temp).ToString();
        addValueToInfoBox(temp);
    }

    private void checkNumberInput(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);

        float n;

        if (float.TryParse(inputBox.value, out n))
        {
            if (n < 0)
                inputBox.value = "0";
        }
        else
        {
            inputBox.value = "0";
        }
    }

    private void checkDistanceInput(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);

        if (int.TryParse(inputBox.value, out int n))
        {
            distanceEntered = int.Parse(inputBox.value);

            if (distanceEntered < 1)
            {
                distanceDisplay.value = 1.ToString();
                distanceEntered = 1;
                return;
            }

            distanceDisplay.value = distanceEntered.ToString();

            if (!percentageFlag)
            {
                int temp;

                // erase old straights value from total distance
                if (int.TryParse(parameterInputElements[0].value, out temp))
                    addValueToInfoBox(-temp);

                randomInputElements[0].value = false;

                int straightVal;


                if (int.TryParse(parameterInputElements[0].value, out straightVal))
                {

                    int minStraights = (distanceEntered / 2) + (distanceEntered % 2);

                    if (junctionCount > minStraights)
                    {
                        // assign min straights to be the junction amount
                        parameterInputElements[0].value = junctionCount.ToString();

                    }

                    else if (straightVal > minStraights)
                    {

                    }

                    else
                        parameterInputElements[0].value = minStraights.ToString();
                }
                else
                    parameterInputElements[0].value = junctionCount.ToString();

                setUpStraightInt();
            }

            
        }
        else
        {
            distanceDisplay.value = 1.ToString();
            distanceEntered = 1;
            setUpStraightInt();
        }

        checkIfMinMaxStraightInputValid(new FocusOutEvent(){target = straightInputElements[0]});
        checkIfMinMaxStraightInputValid(new FocusOutEvent(){target = straightInputElements[1]});
        

        if (!percentageFlag)
        {
            float currDistanceValue = float.Parse(currentDistanceDisplay.text);

            if (currDistanceValue > distanceEntered)
                totalDistanceSumDisplay.style.color = Color.red;

            else
                totalDistanceSumDisplay.style.color = Color.white;
        }
    }

    private void setUpPercentToggleButton(VisualElement root)
    {
        percentageToggleButton = root.Q<Button>("percentageToggleButton");
        percentageFlag = false;
        percentageToggleButton.clicked += () => onClickTogglePercentButton();
    }

    private void onClickTogglePercentButton()
    {
        // if currently on percent, switch off to int
        if (percentageFlag)
        {
            percentageToggleButton.style.flexDirection = FlexDirection.Row;
            percentageSumDisplay.Display(false);
            totalDistanceSumDisplay.Display(true);
        }

        // if not, switch to percent
        else
        {
            percentageToggleButton.style.flexDirection = FlexDirection.RowReverse;
            percentageSumDisplay.Display(true);
            totalDistanceSumDisplay.Display(false);
        }

        percentageFlag = !percentageFlag;

        if (percentageFlag)
        {
            // setUpStraightPercent();
            for (int i = 0; i < parameterInputElements.Count; ++i)
                if (parameterInputElements[i].value.Length != 0)
                    parameterInputElements[i].value = (float.Parse(parameterInputElements[i].value) / distanceEntered * 100).ToString() + "%";


            percentageSumFloat = totalDistanceSumInt / (float)distanceEntered * 100;
            currentPercentageDisplay.text = percentageSumFloat.ToString() + "%";

            percentageSumDisplay.style.color = totalDistanceSumDisplay.style.color;


        }
        else
        {
            // setUpStraightInt();
            totalDistanceSumInt = (long)(distanceEntered * float.Parse(currentPercentageDisplay.text.Remove(currentPercentageDisplay.text.Length - 1)) / 100);
            currentDistanceDisplay.text = totalDistanceSumInt.ToString();

            for (int i = 0; i < parameterInputElements.Count; ++i)
                if (parameterInputElements[i].value.Length != 0)
                    parameterInputElements[i].value = (float.Parse(parameterInputElements[i].value.Remove(parameterInputElements[i].value.Length - 1)) / 100 * distanceEntered).ToString();

            totalDistanceSumDisplay.style.color = percentageSumDisplay.style.color;
        }
    }

    private void cleanInputIfRandomToggle(ClickEvent evt)
    {
        Toggle toggleBox = (evt.target as Toggle);

        // if random toggle is enabled, clear textinput
        if ((toggleBox.value))
        {
            TextField inputBox = toggleBox.parent.parent.Q<TextField>(className: "parameterInput");

            randomTogglesOn += 1;

            if (inputBox.style.backgroundColor == Color.red)
                enableGenerateButton();

            if (inputBox.value.Length > 1 && inputBox.value[inputBox.value.Length - 1] == '%')
                inputBox.value = inputBox.value.Substring(0, inputBox.value.Length - 1);

            if (isNumeric(inputBox.value))
            {
                float temp = float.Parse(inputBox.value);

                if (temp > 0)
                {
                    if (percentageFlag)
                    {
                        addValueToInfoBox(-float.Parse(inputBox.value));
                    }

                    else if (int.TryParse(inputBox.value, out int n))
                    {
                        addValueToInfoBox(-int.Parse(inputBox.value));
                    }
                }
            }

            inputBox.style.backgroundColor = Color.clear;
            inputBox.value = "";

        }

        // if random box is disabled
        else
        {
            TextField inputBox = toggleBox.parent.parent.Q<TextField>(className: "parameterInput");
            inputBox.value = "0";
            randomTogglesOn -= 1;

            // if this is the straights
            if (inputBox == parameterInputElements[0])
            {
                setUpStraightInt();
            }

            if (percentageFlag)
            {
                // if this is the straight input box
                if (inputBox == parameterInputElements[0])
                {
                    setUpStraightPercent();
                }
                else
                    inputBox.value += '%';
            }

        }


    }

    private void eraseOldInputFromSum(FocusInEvent evt)
    {
        TextField inputBox = (evt.target as TextField);

        if (inputBox.value.Length > 1 && inputBox.value[inputBox.value.Length - 1] == '%')
            inputBox.value = inputBox.value.Substring(0, inputBox.value.Length - 1);

        if (inputBox.style.backgroundColor == Color.red)
            enableGenerateButton();

        if (isNumeric(inputBox.value))
        {
            inputBox.style.backgroundColor = Color.clear;

            float temp = float.Parse(inputBox.value);

            if (temp < 0)
                return;

            if (percentageFlag)
            {
                addValueToInfoBox(-float.Parse(inputBox.value));
            }
            else if (int.TryParse(inputBox.value, out int n))
            {
                addValueToInfoBox(-(int.Parse(inputBox.value)));
            }

            if (inputBox != parameterInputElements[0])
                junctionCount -= int.Parse(inputBox.value);
        }
    }

    private void checkIfInputValid(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);

        inputBox.parent.Q<Toggle>("junctionToggle").value = false;
        randomTogglesOn -= 1;

        if (inputBox.value.Length > 1 && inputBox.value[inputBox.value.Length - 1] == '%')
            inputBox.value = inputBox.value.Substring(0, inputBox.value.Length - 1);


        if (isNumeric(inputBox.value))
        {
            inputBox.style.backgroundColor = Color.clear;
            float temp = float.Parse(inputBox.value);

            if (temp >= 0)
            {
                int inputInt;

                if (percentageFlag)
                {
                    // special edge case for straight percentages
                    if (inputBox == parameterInputElements[0] && (temp < 50 || temp > 100))
                    {
                        addValueToInfoBox(float.Parse(inputBox.value));
                        inputBox.style.backgroundColor = Color.red;
                        disableGenerateButton();
                    }
                    else
                    {
                        addValueToInfoBox(float.Parse(inputBox.value));
                        inputBox.value += '%';
                    }
                }

                // if this is on int toggle
                else if (int.TryParse(inputBox.value, out inputInt))
                {
                    if (inputBox == parameterInputElements[0])
                    {

                        if (temp < (distanceEntered / 2) || temp > distanceEntered)
                        {
                            // addValueToInfoBox(int.Parse(inputBox.value));
                            inputBox.style.backgroundColor = Color.red;
                            disableGenerateButton();
                        }
                        checkIfMinMaxStraightInputValid(new FocusOutEvent(){target = straightInputElements[0]});
                        checkIfMinMaxStraightInputValid(new FocusOutEvent(){target = straightInputElements[1]});

                    }
                    // if this is a junction
                    else
                    {
                        junctionCount += inputInt;
                        Debug.Log("Junction count is: " + junctionCount);

                        randomInputElements[0].value = false;

                        int straightVal;

                        // check if we can correct for junction count
                        if (int.TryParse(parameterInputElements[0].value, out straightVal))
                        {

                            // if number of junctions is greater than min straights
                            if (junctionCount > ((distanceEntered / 2) + (distanceEntered % 2)))
                            {
                                // erase old value from sum
                                addValueToInfoBox(-straightVal);

                                // assign min straights to be the junction amount
                                parameterInputElements[0].value = junctionCount.ToString();

                            }
                            else
                            {
                                addValueToInfoBox(inputInt);
                                return;
                            }
                        }
                        else
                            parameterInputElements[0].value = junctionCount.ToString();

                        addValueToInfoBox(junctionCount);
                        
                    }

                    addValueToInfoBox(inputInt);
                }
            }

            else
            {
                inputBox.style.backgroundColor = Color.red;
                disableGenerateButton();
            }
        }

        else
        {
            inputBox.style.backgroundColor = Color.red;
            disableGenerateButton();
        }
    }

    // used to add values to percentage box display
    private void addValueToInfoBox(float value)
    {
        if (percentageFlag)
        {
            percentageSumFloat += value;

            if (percentageSumFloat > 100)
                currentPercentageDisplay.style.color = Color.red;

            else
                currentPercentageDisplay.style.color = Color.white;

            currentPercentageDisplay.text = percentageSumFloat.ToString() + "%";
        }
    }

    // used to add values to discrete box display
    private void addValueToInfoBox(long value)
    {
        totalDistanceSumInt += (value);

        if (totalDistanceSumInt > distanceEntered)
            totalDistanceSumDisplay.style.color = Color.red;

        else
            totalDistanceSumDisplay.style.color = Color.white;

        currentDistanceDisplay.text = totalDistanceSumInt.ToString();

    }

    private void clearAllPieceInputField()
    {
        parameterInputElements.ForEach(elem => elem.value = "");
        parameterInputElements.ForEach(elem => elem.style.backgroundColor = Color.clear);

        randomInputElements.ForEach(elem => elem.value = true);
        randomTogglesOn = randomInputElements.Count;

        percentageSumFloat = 100;
        totalDistanceSumInt = 1;

        percentageSumDisplay.style.color = Color.white;
        totalDistanceSumDisplay.style.color = Color.white;

        currentPercentageDisplay.text = "100%";
        currentDistanceDisplay.text = "1";

        // currentPercentageDisplay.style.color = Color.white;
        // currentDistanceDisplay.style.color = Color.white;

        generateButtonIssues = 0;
        enableGenerateButton();

        junctionCount = 0;

        explicitToggle.value = false;

        // reset min and max straights
        straightInputElements[0].value = "1";
        straightInputElements[1].value = "1";
        straightInputElements[0].style.backgroundColor = Color.clear;
        straightInputElements[1].style.backgroundColor = Color.clear;

        // reset distance
        distanceEntered = 1;
        distanceDisplay.value = "1";
        distanceDisplay.style.backgroundColor = Color.clear;

        // reset straights to be 1 by default
        parameterInputElements[0].value = "1";
        randomInputElements[0].value = false;

        // reset display back to path as main screen
        pathPieceDisplay.Display(true);
        userSettingDisplay.Display(false);
        visualSettingDisplay.Display(false);

        // reset drop downs in visual setting textures back to default
        dropdowns[0].value = dropdowns[0].choices[0];
        dropdowns[1].value = dropdowns[1].choices[0];
        dropdowns[2].value = dropdowns[2].choices[0];

        // reset ceiling flag
        enableCeiling.value = false;


        // reset wall height
        wallHeight.value = "0";

        // reset locomotion to default
        locomotionMethodDropdown.value = locomotionMethodDropdown.choices[0];

    }
    private static bool isNumeric(string s)
    {
        return float.TryParse(s, out float n);
    }

    private void disableGenerateButton()
    {
        generateButtonIssues++;
        generateButton.style.backgroundColor = new Color((float)0.2, (float)0.05882353, (float)0.05882353);
        generateButton.style.color = Color.white;
    }

    private void enableGenerateButton()
    {
        generateButtonIssues--;

        if (generateButtonIssues < 0)
            generateButtonIssues = 0;

        if (generateButtonIssues == 0)
        {
            generateButton.style.backgroundColor = Color.white;
            generateButton.style.color = Color.black;
        }
    }

    private void checkIfMinMaxStraightInputValid(FocusOutEvent evt)
    {
        TextField inputBox = (evt.target as TextField);
        int temp;

        Debug.Log(inputBox.value);

        if (inputBox.style.backgroundColor == Color.red)
            enableGenerateButton();

        if (int.TryParse(inputBox.value, out temp))
        {
            inputBox.style.backgroundColor = Color.clear;

            // if minStraights
            if (inputBox == straightInputElements[0])
            {
                if (temp <= 0)
                {
                    inputBox.value = 1.ToString();
                    return;
                }

                if (percentageFlag)
                    return;

                int numStraights;

                // if currently no number of straights input
                if (!int.TryParse(parameterInputElements[0].value, out numStraights))
                    numStraights = distanceEntered / 2;

                if (temp > numStraights)
                {
                    straightInputElements[0].style.backgroundColor = Color.red;
                    disableGenerateButton();
                    return;
                }

                int requiredMinStraights = (distanceEntered - numStraights) * temp;

                if (numStraights < requiredMinStraights)
                {
                    straightInputElements[0].style.backgroundColor = Color.red;
                    disableGenerateButton();
                }


                // if there is a valid input in the max box
                if (straightInputElements[1].style.backgroundColor != Color.red && straightInputElements[0].style.backgroundColor != Color.red)
                {
                    int maxStraightsInput = 1;

                    // if they enter a value that is greater than maxStraights set max to minStraights
                    if (int.TryParse(straightInputElements[1].value, out maxStraightsInput))
                    {
                        if (temp > maxStraightsInput)
                            straightInputElements[1].value = temp.ToString();

                    }
                    else
                    {
                        straightInputElements[1].value = temp.ToString();
                    }


                }
                // if there is not a valid input in the max box, fill it in with same values
                else
                {
                    straightInputElements[1].value = temp.ToString();
                    straightInputElements[1].style.backgroundColor = Color.clear;
                    enableGenerateButton();
                }
            }

            // if maxStraights
            else
            { 
                int currStraights;

                // if they try to enter a max too big
                if (int.TryParse(parameterInputElements[0].value, out currStraights))
                {

                    //get minimum required straights/junctions
                    int calc = (distanceEntered / 2) + (distanceEntered % 2);

                    // if curr straights is less than required set it to required amount
                    if (currStraights < calc)
                        currStraights = calc;

                    // formula for current number of junctions
                    calc = distanceEntered - currStraights;

                    // formula for left over straights
                    calc = currStraights - calc;

                    // if number inputted is greater than leftover, set to leftover
                    if (temp > calc)
                    {
                        temp = calc;
                        inputBox.value = temp.ToString();
                    }

                    if (temp < (calc / (junctionCount + 1)))
                    {  
                        temp = calc;
                        inputBox.value = temp.ToString();
                    }
                }

                // if there is a valid input in the min box
                if (straightInputElements[0].style.backgroundColor != Color.red)
                {
                    int minStraightsInput = 1;

                    // if they enter a value that is less than minStraights set it to minStraights
                    if (int.TryParse(straightInputElements[0].value, out minStraightsInput))
                    {
                        if (temp < minStraightsInput)
                            inputBox.value = minStraightsInput.ToString();
                    }
                    // else
                    // {
                    //     inputBox.value = minStraightsInput.ToString();
                    // }


                }
                // if there is not a valid input in the min box, fill it in with default
                else
                {
                    straightInputElements[0].value = 1.ToString();
                    straightInputElements[0].style.backgroundColor = Color.clear;
                    enableGenerateButton();
                }

            }
        }

        else
        {
            inputBox.style.backgroundColor = Color.red;
            disableGenerateButton();
        }
    }

    private CompletedCondition saveAllInputEntries()
    {
        CompletedCondition condition = new CompletedCondition();

        condition.distance = distanceEntered;
        condition.dataRep = percentageFlag ?
            MazeDataController.DataRepresentations.Percentage : MazeDataController.DataRepresentations.Integer;

        condition.minStraights = Int32.Parse(straightInputElements[0].text);
        condition.maxStraights = Int32.Parse(straightInputElements[1].text);

        condition.ceilTexture = dropdowns[0].value;
        condition.wallTexture = dropdowns[1].value;
        condition.floorTexture = dropdowns[2].value;

        condition.enableCeilFlag = enableCeiling.value;

        condition.wallHeight = float.Parse(wallHeight.value);

        condition.locomotionMethod = locomotionMethodDropdown.value;

        condition.explicitToggle = explicitToggle.value;

        List<float> inputVals = new List<float>();
        List<bool> randomVals = new List<bool>();

        for (int i = 0; i < parameterInputElements.Count; ++i)
        {
            // if a number was entered
            if (!randomInputElements[i].value)
            {
                randomVals.Add(false);

                string s = parameterInputElements[i].value;

                if (percentageFlag)
                    s = s.Substring(0, s.Length - 1);

                inputVals.Add(float.Parse(s));
            }

            else
            {
                randomVals.Add(true);
                inputVals.Add(0);
            }

        }

        condition.totalDistanceFromPieces = totalDistanceSumInt;

        condition.totalPercentFromPieces = percentageSumFloat;

        condition.pathPieceValues = inputVals;
        condition.pathPieceRandomBools = randomVals;
        condition.completedFlag = true;

        return condition;
    }

    private void saveCurrentCondition()
    {
        Button buttonClicked = conditionsList[currentConditionIndex];

        VisualElement checkmark = buttonClicked.Children().ToList()[0];

        // save everything to a private class
        completedConditions[currentConditionIndex] = saveAllInputEntries();

        if (checkmark.ClassListContains("incompleteCheckmark"))
        {
            checkmark.RemoveFromClassList("incompleteCheckmark");
            checkmark.AddToClassList("completeCheckmark");

            if (++completeConditionCount == conditionsList.Count)
                enableExperimentButton();
        }

        buttonClicked.AddToClassList("conditionsButton");
        buttonClicked.RemoveFromClassList("conditionsButtonSelected");

        Button nextButton = conditionsList[currentConditionIndex];

        nextButton.RemoveFromClassList("conditionsButton");
        nextButton.AddToClassList("conditionsButtonSelected");

    }
    //Now to be save maze 

    private void enableExperimentButton()
    {
        saveExperimentButton.style.backgroundColor = new Color((float)0.0731132, (float)0.2924528, (float)0.1408505);

        saveExperimentButton.clicked += saveExperiment;

    }

    private void saveExperiment()
    {
        MazeDataController data = MazeDataController.instance;

        string experimentPath = CsvManager.instance.getDirectoryPathFromExplorer();
        if (experimentPath == "")
            return;



        CsvManager.instance.setUpExperimentFileStructure(experimentPath); 

        saveExperimentButton.clicked -= saveExperiment;
        saveExperimentButton.style.backgroundColor = new Color((float)0.2, (float)0.05882353, (float)0.05882353);

        List<JunctionTypes> enumList = new List<JunctionTypes>(){
            JunctionTypes.Straight,
            JunctionTypes.ThreeWayRightStraight,
            JunctionTypes.ThreeWayLeftRight,
            JunctionTypes.ThreeWayLeftStraight,
            JunctionTypes.RightTurn,
            JunctionTypes.FourWay,
            JunctionTypes.LeftTurn
        };

        for (int i = 0; i < completedConditions.Count; ++i)
        {
            CompletedCondition condition = completedConditions[i];

            data.setDistance(condition.distance);
            data.setMinStraights(condition.minStraights);
            data.setMaxStraights(condition.maxStraights);

            data.setDataRep(condition.dataRep);

            data.setShouldUseCeiling(condition.enableCeilFlag);

            data.setCeilingTexture(condition.ceilTexture);
            data.setWallTexture(condition.wallTexture);
            data.setFloorTexture(condition.floorTexture);

            data.setWallHeight(condition.wallHeight);

            data.setLocomotionMethod(condition.locomotionMethod);

            data.setExplicitOrdering(condition.explicitToggle);

            List<float> pathInputList = condition.pathPieceValues;
            List<bool> pathRandomValList = condition.pathPieceRandomBools;

            for (int z = 0; z < pathInputList.Count; ++z)
            {
                // if a number was entered
                if (!pathRandomValList[z])
                {
                    data.setPieceRandom(false, enumList[z]);

                    if (condition.dataRep == MazeDataController.DataRepresentations.Percentage)
                    {
                        data.setJuncForFloat(pathInputList[z], enumList[z]);
                    }
                    else
                        data.setJuncForInt((int)pathInputList[z], enumList[z]);

                }
                else
                {
                    data.setPieceRandom(true, enumList[z]);
                }

            }

            CsvManager.instance.saveMazeConditions(data.getIntermediateDataRep(), conditionsList[i].text);

            //If we use explicit ordering 
            if (data.getExplicitOrdering())
            {
                //We need to early generate ALL the data 
                //This will force the object to make our stack 
                data.makeSingletonObjectToPassMaze();

                //Now we can use that object to get our stack info 
                CsvManager.instance.saveExplicitStackInfo(MazeParameterManager.instance.stackToJuncDataArray(), conditionsList[i].text);
    }
        }
        // return to main menu, bypassing the confirmation modal
        confirmModalYesButton.SendEvent(new NavigationSubmitEvent() { target = confirmModalYesButton });
    }

}

class CompletedCondition
{
    public bool completedFlag;
    public int distance;

    public long totalDistanceFromPieces;

    public float totalPercentFromPieces;

    public int maxStraights;
    public int minStraights;

    public MazeDataController.DataRepresentations dataRep;

    public List<float> pathPieceValues;
    public List<bool> pathPieceRandomBools;

    public float wallHeight;

    public string ceilTexture;
    public string wallTexture;
    public string floorTexture;

    public bool enableCeilFlag;

    public string locomotionMethod;

    public bool explicitToggle;

}
