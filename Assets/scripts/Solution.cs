
using System.Collections.Generic;
using UnityEngine;

public class Solution {
    private List<Vector2Int>    directions;
    private List<State>         states;
    private int                 index_state = 0;       
    private bool                playing     = false;

    //related to play()
    private float       timeSinceLastAnimation          = 0;
    private float       solution_timeBetweenAnimations  = 0.3f;
    private Animator    animator                        = new Animator();


    public Solution(ref Maze maze) {
        directions = maze.getSolution();

        states = new List<State>();
        states.Add(new State(maze));

        if(directions != null)
            foreach (var dir in directions)
                states.Add(states[states.Count-1].move(dir).Value);
    }


    public void play(GameObject[,] gMazeTiles, GameObject gObjPlayer, UnityEngine.Events.UnityAction onAnimationEnd){
        if (!playing)
            return;

        if (index_state > 0 && index_state < states.Count) {
            if (animator.animate(states[index_state - 1], gMazeTiles, gObjPlayer, onAnimationEnd))
                return;
        }

        if (index_state > states.Count-2)
            return;

        timeSinceLastAnimation += Time.deltaTime;
        if(timeSinceLastAnimation > solution_timeBetweenAnimations){
            timeSinceLastAnimation = 0;
            var dir = directions[index_state];
                
            animator = new Animator();
            states[index_state].move(dir, animator);
            index_state++;
        }
    }


    public bool setIndex(int index) {
        if (index < 0 || index >= states.Count)
            return false;

        playing = false;
        this.index_state = index;

        return true;
    }


    public bool     IsPlaying()     {   return playing;             }
    public void     TogglePlaying() {   playing = !playing;         }
    public int      GetIndex()      {   return index_state;         }
    public int      StatesCount()   {   return states.Count;        }
    public State    currentState()  {   return states[index_state]; }
}
