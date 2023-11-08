
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class UI_Generator : MonoBehaviour {

    public Canvas root;

    // Generator
    public TMP_InputField input_gen_maxGenerations;
    public TMP_InputField input_gen_nbrIndividuals;
    public TMP_InputField input_gen_maxTime;
    public TMP_InputField input_gen_minFitness;
    public Toggle input_gen_cleanUp;
    public Toggle input_gen_cleanUpHarder;
    public Toggle input_gen_cleanUpTrimSize;

    // Maze
    public TMP_InputField input_maze_width;
    public TMP_InputField input_maze_height;
    public TMP_Dropdown input_wincondition;

    // Tiles
    public Toggle input_tile_ground;
    public Toggle input_tile_ice;
    public Toggle input_tile_bridge;
    public Toggle input_tile_red;
    public Toggle input_tile_NoContent;
    public Toggle input_tile_LightSwitch;
    public Toggle input_tile_Stone;
    public Toggle input_tile_Shift;

    //Generate
    public Button button_generate;
    public GridLayoutGroup genStats;    // contains 8x TMP_text
    public TMP_Text text_generatorInfo;

    //Error message
    public TMP_Text ui_text_error;

    //private
    private GeneratorInput genInput = Generator.generatorInput;


    void Start() {
        Generator.getInstance().generatorState = Generator.GeneratorState.Idle;

        Application.targetFrameRate = 60;

        // Default values: Generator
        input_gen_maxGenerations.text   = genInput.nbrGenerations.ToString();
        input_gen_nbrIndividuals.text   = genInput.nbrIndividuals.ToString();
        input_gen_maxTime.text          = genInput.maxTime.ToString();
        input_gen_minFitness.text       = genInput.minFitness.ToString();
        input_gen_cleanUp.isOn          = genInput.isCleaningUp;
        input_gen_cleanUpHarder.isOn    = genInput.isCleanUpMakingHarder;
        input_gen_cleanUpTrimSize.isOn  = genInput.isCleanUpTrimingSize;

        // Default values: Maze
        input_maze_width.text           = genInput.width.ToString();
        input_maze_height.text          = genInput.height.ToString();

        // Default values: Wincondition
        input_wincondition.options.Clear();
        List<string> names_wc = new List<string>();
        foreach (WinCondition wc in WinCondition.winConditions)
            input_wincondition.options.Add(new TMP_Dropdown.OptionData(wc.name));
        input_wincondition.value = WinCondition.winConditions.FindIndex(0, x => x.name == WinCondition.winConditions[genInput.winCondition].name);
        input_wincondition.RefreshShownValue();

        // UI Listeners
        input_gen_cleanUp.onValueChanged.AddListener(           (bool b) => { genInput.isCleaningUp = b; });
        input_gen_cleanUpHarder.onValueChanged.AddListener(     (bool b) => { genInput.isCleanUpMakingHarder = b; });
        input_gen_cleanUpTrimSize.onValueChanged.AddListener(   (bool b) => { genInput.isCleanUpTrimingSize = b; });

        input_gen_maxGenerations.onEndEdit.AddListener((string s) 
            => { if (s == "" || s== "-") s = "0"; 
                genInput.nbrGenerations         = Mathf.Clamp(int.Parse(s), 1, 10000);
                input_gen_maxGenerations.text   = genInput.nbrGenerations.ToString();});

        input_gen_nbrIndividuals.onEndEdit.AddListener((string s) 
            => { if (s == "" || s== "-") s = "0"; 
                genInput.nbrIndividuals         = Mathf.Clamp(int.Parse(s), 1, 10000);
                input_gen_nbrIndividuals.text   = genInput.nbrIndividuals.ToString();});

        input_gen_maxTime.onEndEdit.AddListener((string s) 
            => { if (s == "" || s== "-") s = "0"; 
                genInput.maxTime            = Mathf.Clamp(int.Parse(s), 1, 10000);
                input_gen_maxTime.text      = genInput.maxTime.ToString();});

        input_gen_minFitness.onEndEdit.AddListener((string s) 
            => { if (s == "" || s== "-") s = "0"; 
                genInput.minFitness         = Mathf.Clamp(int.Parse(s), 1, 1000);
                input_gen_minFitness.text   = genInput.minFitness.ToString();});

        input_maze_width.onEndEdit.AddListener((string s)
            => { if (s == "" || s== "-") s = "0"; 
                genInput.width          = Mathf.Clamp(int.Parse(s), 1, 16);
                input_maze_width.text   = genInput.width.ToString();});

        input_maze_height.onEndEdit.AddListener((string s)
            => { if (s == "" || s== "-") s = "0"; 
                genInput.height     = Mathf.Clamp(int.Parse(s), 1, 16);
                input_maze_height.text  = genInput.height.ToString();});

        input_wincondition.onValueChanged.AddListener((int i) => { genInput.winCondition = (byte)i; });

        // generation stats
        genStats.transform.GetChild(0).GetComponent<TMP_Text>().text = "best maze:";
        genStats.transform.GetChild(1).GetComponent<TMP_Text>().text = ":";
        genStats.transform.GetChild(2).GetComponent<TMP_Text>().text = "time:";
        genStats.transform.GetChild(3).GetComponent<TMP_Text>().text = "";
        genStats.transform.GetChild(4).GetComponent<TMP_Text>().text = "iteration:";
        genStats.transform.GetChild(5).GetComponent<TMP_Text>().text = "";
        genStats.transform.GetChild(6).GetComponent<TMP_Text>().text = "individual:";
        genStats.transform.GetChild(7).GetComponent<TMP_Text>().text = "";
        genStats.gameObject.SetActive(false);

        // button generate
        button_generate.onClick.AddListener(onGeneratorButtonPress);

        // Tiles
        input_tile_ground.isOn      = genInput.spawn_Ground_None;
        input_tile_ice.isOn         = genInput.spawn_Ground_Ice;
        input_tile_bridge.isOn      = genInput.spawn_Ground_Bridge;
        input_tile_red.isOn         = genInput.spawn_Ground_Red;
        input_tile_NoContent.isOn   = genInput.spawn_Content_None;
        input_tile_LightSwitch.isOn = genInput.spawn_Content_Switch;
        input_tile_Stone.isOn       = genInput.spawn_Content_Stone;
        input_tile_Shift.isOn       = genInput.spawn_Content_Shift;

        input_tile_ground.image.material.SetInt("_highlighted",         genInput.spawn_Ground_None      ? 1 : 0);
        input_tile_ice.image.material.SetInt("_highlighted",            genInput.spawn_Ground_Ice       ? 1 : 0);
        input_tile_bridge.image.material.SetInt("_highlighted",         genInput.spawn_Ground_Bridge    ? 1 : 0);
        input_tile_red.image.material.SetInt("_highlighted",            genInput.spawn_Ground_Red       ? 1 : 0);
        input_tile_NoContent.image.material.SetInt("_highlighted",      genInput.spawn_Content_None     ? 1 : 0);
        input_tile_LightSwitch.image.material.SetInt("_highlighted",    genInput.spawn_Content_Switch   ? 1 : 0);
        input_tile_Stone.image.material.SetInt("_highlighted",          genInput.spawn_Content_Stone    ? 1 : 0);
        input_tile_Shift.image.material.SetInt("_highlighted",          genInput.spawn_Content_Shift    ? 1 : 0);

        input_tile_ground.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Ground_None = b; input_tile_ground.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_ice.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Ground_Ice = b; input_tile_ice.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_bridge.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Ground_Bridge = b; input_tile_bridge.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_red.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Ground_Red = b; input_tile_red.image.material.SetInt("_highlighted", b ? 1 : 0); });

        input_tile_NoContent.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Content_None = b; input_tile_NoContent.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_LightSwitch.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Content_Switch = b; input_tile_LightSwitch.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_Stone.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Content_Stone = b; input_tile_Stone.image.material.SetInt("_highlighted", b ? 1 : 0); });
        input_tile_Shift.onValueChanged.AddListener(
            (bool b) => { genInput.spawn_Content_Shift = b; input_tile_Shift.image.material.SetInt("_highlighted", b ? 1 : 0); });

        // tooltips tiles
        input_tile_ground.GetComponent<ToolTipSource>().tooltipText         = "<b>Ground</b>";
        input_tile_ice.GetComponent<ToolTipSource>().tooltipText            = "<b>Ice:</b> player slides on it";
        input_tile_bridge.GetComponent<ToolTipSource>().tooltipText         = "<b>Bridge:</b> bridge between ground and ice tiles.";
        input_tile_red.GetComponent<ToolTipSource>().tooltipText            = "<b>Red</b> used in certain wincondtions. Behaves like bridge tile.";
        input_tile_NoContent.GetComponent<ToolTipSource>().tooltipText      = "<b>No Content</b>";
        input_tile_LightSwitch.GetComponent<ToolTipSource>().tooltipText    = "<b>Light Switch:</b> Inverts colors of surrounding 3x3 tiles";
        input_tile_Stone.GetComponent<ToolTipSource>().tooltipText          = "<b>Stone:</b> can be pushed into ground tile of same color";
        input_tile_Shift.GetComponent<ToolTipSource>().tooltipText          = "<b>Shift:</b> shifts row in indicated direction by one";

        ui_text_error.text = "";
        text_generatorInfo.gameObject.SetActive(false);
   
    }



    public void onGeneratorButtonPress() {
        if (Generator.getInstance().generatorState == Generator.GeneratorState.Idle) {
            text_generatorInfo.gameObject.SetActive(false);

            //validateInput
            string errorMessage = genInput.validate();
            ui_text_error.text = errorMessage;
            if (errorMessage != "")
                return;

            // make sure all input fields have been processed
            var inputFields = root.GetComponentsInChildren<TMP_InputField>();
            foreach (var inputField in inputFields)
                inputField.onEndEdit.Invoke(inputField.text);

            genStats.gameObject.SetActive(true);

            enableUI(false);
            (button_generate.transform.GetChild(0)).GetComponent<TMP_Text>().text = "Continue";
            Generator.startGeneration(genInput);
        
        }else if(Generator.getInstance().generatorState == Generator.GeneratorState.Generating){
            Generator.getInstance().generatorState = Generator.GeneratorState.EndingPrematurly;

            //ui
            genStats.gameObject.SetActive(false);
            enableUI(true);
            (button_generate.transform.GetChild(0)).GetComponent<TMP_Text>().text = "Generate";
            button_generate.interactable = false;

            //play scene will be loaded in Update()
        }

    }


    void Update(){
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            onGeneratorButtonPress();

        // handle tab navigation
        if (Input.GetKeyDown(KeyCode.Tab)){
            if (EventSystem.current.currentSelectedGameObject != null) { 
                Selectable next;
                if(Input.GetKey(KeyCode.LeftShift))
                    next = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
                else
                    next = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
                if (next != null) {
                    InputField inputField = next.GetComponent<InputField>();
                    if (inputField != null) 
                        inputField.OnPointerClick(new PointerEventData(EventSystem.current));

                    EventSystem.current.SetSelectedGameObject(next.gameObject, new BaseEventData(EventSystem.current));
                }
            }
        }

        Generator generator = Generator.getInstance();


        switch (generator.generatorState) {
            case Generator.GeneratorState.Generating:
            case Generator.GeneratorState.EndingPrematurly:
                var fitness = generator.highestFitness == Maze.UNSOLVABLE ? 0 : generator.highestFitness;
                genStats.transform.GetChild(1).GetComponent<TMP_Text>().text = fitness + "/" + Generator.generatorInput.minFitness;
                genStats.transform.GetChild(3).GetComponent<TMP_Text>().text = (int)generator.passedTime() + "/" + Generator.generatorInput.maxTime;
                genStats.transform.GetChild(5).GetComponent<TMP_Text>().text = generator.iteration + "/" + Generator.generatorInput.nbrGenerations;
                genStats.transform.GetChild(7).GetComponent<TMP_Text>().text = generator.currentIndividual + "/" + Generator.generatorInput.nbrIndividuals;
                break;
            case Generator.GeneratorState.CleaningUp:
                text_generatorInfo.text = "cleaning up...";
                text_generatorInfo.gameObject.SetActive(true);
                genStats.gameObject.SetActive(false);
                break;
            case Generator.GeneratorState.Done:
                genStats.gameObject.SetActive(false);
                generator.generatorState = Generator.GeneratorState.Idle;
                if (generator.highestFitness == Maze.UNSOLVABLE) {
                    text_generatorInfo.text = "no maze found";
                    text_generatorInfo.gameObject.SetActive(true);
                    button_generate.interactable = true;
                    (button_generate.transform.GetChild(0)).GetComponent<TMP_Text>().text = "Generate";
                    enableUI(true); 
                }else{ 
                    SceneManager.LoadScene ("Play");
                }
                break;
            case Generator.GeneratorState.Solving:
                text_generatorInfo.text = "solving...";
                text_generatorInfo.gameObject.SetActive(true);
                genStats.gameObject.SetActive(false);
                break;
            case Generator.GeneratorState.Idle:
                break;
            default:
                Debug.Assert(false);
                break;
        }


    }


    void enableUI(bool enabled) {
        foreach (var obj in root.GetComponentsInChildren<TMP_InputField>())
            obj.enabled = enabled;
        foreach (var obj in root.GetComponentsInChildren<Toggle>())
            obj.enabled = enabled;
        foreach (var obj in root.GetComponentsInChildren<TMP_Dropdown>())
            obj.enabled = enabled;
    }


    public void quitApplication() {
        Application.Quit();
    }


}
