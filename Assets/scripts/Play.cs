using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


public class Play : MonoBehaviour {

    //static so can be changed by Generator scene
    public static Maze maze;
    public static Solution solution;

    //prefabs
    public GameObject prefabPlayer;
    public GameObject prefabTile;
    
    //UI
    public RectTransform ui_solutionBody;
    public Text ui_winCondition;
    public Text ui_stepCount;
    public Canvas canvas;

    public Button button_solutionToggle;
    public Button button_solution_first;
    public Button button_solution_previous;
    public Button button_solution_play;
    public Button button_solution_next;
    public Button button_solution_last;

    public Button button_undo;
    public Button button_restart;

    public Color color_mode_solution;
    public Color color_mode_play;

    // sounds
    public AudioClip audio_win;
    public AudioClip audio_lightSwitch;
    public AudioClip audio_shift;


    //History: quick and dirty undo behaviour
    //history.size() always >= 1
    private List<State> history;
    private State       history_current()           { Debug.Assert(history.Count>0); return history[history.Count-1];}
    private State       history_previous()          { Debug.Assert(history.Count>1); return history[history.Count-2];}
    private void        history_commit(State s)     { history.Add(s); }
    public  void        history_undo()              { if(history.Count>1) history.RemoveAt(history.Count-1);}
    public  void        history_restart()           { Debug.Assert(history.Count > 0); history.RemoveRange(1, history.Count-1);}

    private Animator animator = new Animator();

    private GameObject      gObjParentMaze;
    private GameObject[,]   gMazeTiles;
    private GameObject      gObjPlayer;

    private bool mode_solution = false;
    


    void Awake() {
        Debug.Assert(solution != null);
        Camera.main.backgroundColor = (mode_solution) ? color_mode_solution : color_mode_play;
        ui_solutionBody.gameObject.SetActive(mode_solution);
    }


    void Start() {
        Debug.Assert(animator != null);
        history = new List<State>();
        history_commit(new State(maze));
        createGameObjects(history_current());
        UpdateUI();
        animator.clear();
    }


    void Update() {
        //solution
        if (mode_solution) {
            solution.play(gMazeTiles, gObjPlayer, () => { createGameObjects(solution.currentState());
                updateSolutionButtons();
                UpdateUI();
            });
            if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
                toogleShowingSolution();

        //animation
        }else if (history.Count > 1
                && animator.animate(history_previous(), gMazeTiles, gObjPlayer, () => { createGameObjects(history_current()); UpdateUI(); })) { 
                    return;
        
        // user input
        }else{
            //animation
            if (Input.GetKey(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                MovePlayer(Dir.NORTH);
            else if (Input.GetKey(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                MovePlayer(Dir.SOUTH);
            else if (Input.GetKey(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                MovePlayer(Dir.WEST);
            else if (Input.GetKey(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                MovePlayer(Dir.EAST);
            else if (Input.GetKeyDown(KeyCode.R))
                Restart();
            else if (Input.GetKeyDown(KeyCode.Z))
                Undo();
            else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
                loadGenerateScene();
            else if(Input.GetKeyDown(KeyCode.H))
                toogleShowingSolution();
        }   
    }


    public void loadGenerateScene(){ 
        SceneManager.LoadScene ("Generate");
    }


    public void Undo(){
        if (mode_solution)
            return;
        history_undo();
        createGameObjects(history_current());
        UpdateUI();
    }


    public void Restart(){
        if (mode_solution)
            return;
        history_restart();
        animator.clear();
        createGameObjects(history_current());
        UpdateUI();
    }



    public void UpdateUI(){
        //winCondition
        State state             = mode_solution ? solution.currentState() : history_current();
        
        bool solved;
        var winCondition = WinCondition.winConditions[maze.getWinCondition()];
        if (winCondition.name == "Get stuck") { 
            solved = true;
            foreach (var dir in Dir.DIRECTIONS)
                if (state.move(dir) != null) {
                    solved = false;
                    break;
                }
        }else 
            solved = winCondition.verify(state);

        if (solved)
            playAudio(audio_win);

        ui_winCondition.color   = solved ? Color.green : Color.black;
        ui_winCondition.text    = winCondition.name;


        //stepcount
        int stepcount           = mode_solution ? solution.GetIndex() : history.Count - 1;
        int nbrStepsSolution    = maze.getNbrSteps();
        if (nbrStepsSolution > 0)
            ui_stepCount.text       = stepcount + " / " + nbrStepsSolution;
        else
            ui_stepCount.text       = stepcount + " / no solution";
        ui_stepCount.color = (stepcount <= nbrStepsSolution) ? Color.black : Color.red;
    }



    public void MovePlayer(Vector2Int dir) {
        State? nextStep = history_current().move(dir, animator);
        if (nextStep != null) {
            history_commit(nextStep.Value);
            if(!animator.animate(history_previous(), 
                                gMazeTiles, 
                                gObjPlayer, 
                                ()=> { createGameObjects(history_current()); UpdateUI(); } ))
            {
                createGameObjects(history_current());
                UpdateUI();
            }
        }else
            animator.clear();   // Step.move might still push animations even if it return null
    }


    public void toogleShowingSolution() {
        mode_solution = !mode_solution;
        ui_solutionBody.gameObject.SetActive(mode_solution);
        button_solutionToggle.GetComponent<Image>().color = (mode_solution) ? new Color(1, 0.6f, 0) : Color.white;
        Camera.main.backgroundColor = (mode_solution) ? color_mode_solution : color_mode_play;
        State currentState = mode_solution?solution.currentState():history_current();
        createGameObjects(currentState);
        if (mode_solution)
            updateSolutionButtons();
        else if(solution.IsPlaying())
            solution.TogglePlaying();
        UpdateUI();

        button_undo.interactable    = !mode_solution;
        button_restart.interactable = !mode_solution;
    }



    public void Solution_PlayPause() {
        Debug.Assert(mode_solution);
        solution.TogglePlaying();
        updateSolutionButtons();
        UpdateUI();
    }


    public void Solution_next() {
        Debug.Assert(mode_solution);
        solution.setIndex(solution.GetIndex()+1);
        createGameObjects(solution.currentState());
        updateSolutionButtons();
        UpdateUI();
    }


    public void Solution_previous() {
        Debug.Assert(mode_solution);
        solution.setIndex(solution.GetIndex() - 1);
        createGameObjects(solution.currentState());
        updateSolutionButtons();
        UpdateUI();
    }


    public void Solution_first() {
        Debug.Assert(mode_solution);
        solution.setIndex(0);
        createGameObjects(solution.currentState());
        updateSolutionButtons();
        UpdateUI();
    }


    public void Solution_last() {
        Debug.Assert(mode_solution);
        solution.setIndex(solution.StatesCount()- 1);
        createGameObjects(solution.currentState());
        updateSolutionButtons();
        UpdateUI();
    }

    public void updateSolutionButtons() {
        button_solution_first.interactable      = 
        button_solution_previous.interactable   = solution.GetIndex() > 0;

        button_solution_play.interactable       = 
        button_solution_next.interactable       =
        button_solution_last.interactable       = solution.GetIndex() < solution.StatesCount() - 1;

        button_solution_play.GetComponentInChildren<Text>().text = solution.IsPlaying() ? "||" : ">";
    }


    public void createGameObjects(State state) {

        Vector2Int size = state.getBounds();

        //size 1 shows 2x2 tiles in 1:1 screen ratio
        var aspect = Camera.main.aspect;

        float cameraSize = (float)(Mathf.Max(size.x/aspect, size.y))/2;
        cameraSize += 1f; //adds 1f of a tile size one each size as padding
        Camera.main.orthographicSize = cameraSize;
        Camera.main.transform.position = new Vector3((float)size.x / 2 - 0.5f, 5, (float)size.y / 2 - 0.5f);

        canvas.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(1, size.x-2), size.y);

        //delete previous GameObjects
        if (gObjParentMaze != null)
            Destroy(gObjParentMaze);
        gObjParentMaze = new GameObject("Maze");

        //create Tiles
        gMazeTiles = new GameObject[size.x, size.y];
        var tiles = state.getTiles();
        for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++){
                GameObject go_tile = Instantiate(prefabTile, gObjParentMaze.transform);
                go_tile.transform.position = new Vector3(i, 0, j);
                go_tile.GetComponent<MeshRenderer>().material.SetInt("_Tile", tiles[i, j]);
                go_tile.GetComponent<MeshRenderer>().material.SetInt("_highlighted", 0);
                go_tile.GetComponent<MeshRenderer>().material.SetFloat("_posStoneX", 0);
                go_tile.GetComponent<MeshRenderer>().material.SetFloat("_posStoneY", 0);
                gMazeTiles[i, j] = go_tile;
            }
        }

        Vector2Int playPos0 = state.pos;
        gObjPlayer = GameObject.Instantiate(prefabPlayer, new Vector3(playPos0.x, 1, playPos0.y), Quaternion.identity, gObjParentMaze.transform);
 
    }


    void playAudio(AudioClip clip) {
        var audioSource = Camera.main.gameObject.GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
