using System.Collections.Generic;
using UnityEngine;
using static Tile;


public struct Maze{

    private Tile[,]     tiles;
    private Vector2Int  posStart;

    private byte winconditionIdx;

    private State?      lastState;   // null if unsolved or unsolvable 
    private short       nbrSteps;  

    public const int UNSOLVABLE = -1;
    public const int UNSOLVED   = -2;


    //ctor new random Maze
    public Maze(GeneratorInput genInput, RandomTileGenerator randTileGen, System.Random rand){
        tiles = new Tile[genInput.width, genInput.height];
        for (int i = 0; i < genInput.width; i++)
            for (int j = 0; j < genInput.height; j++)
                tiles[i, j] = randTileGen.generate(rand);

        posStart = new Vector2Int(rand.Next(0, genInput.width), rand.Next(0, genInput.height));
        winconditionIdx = genInput.winCondition;
        nbrSteps = UNSOLVED;
        lastState = null;
    }


    //copy ctor
    //doesn't copy lastStep and nbrSteps
    public Maze(Maze other){
        this.tiles              = (Tile[,])other.tiles.Clone();
        this.posStart           = other.posStart;
        this.winconditionIdx    = other.winconditionIdx;
        this.nbrSteps           = UNSOLVED;
        this.lastState           = null;
    }


    //create new maze by crossing 2 others and mutating it afterwards
    public Maze(Maze m0, Maze m1, RandomTileGenerator randTileGen, System.Random rand){
        Debug.Assert(m0.getBounds()             == m1.getBounds());
        Debug.Assert(m0.winconditionIdx.Equals(m1.winconditionIdx));

        this.winconditionIdx = m0.winconditionIdx;

        var bounds = m1.getBounds();

        tiles = new Tile[bounds.x, bounds.y];
        bool splitHorizontal = rand.Next(0, 2) == 0;
        int slpitPos = rand.Next(1, splitHorizontal ? Mathf.Max(1, bounds.x - 2) : Mathf.Max(1, bounds.y - 2));
        for (int i=0; i< bounds.x; i++){
            for(int j=0; j< bounds.y; j++){
                if((splitHorizontal?i:j) < slpitPos)
                    tiles[i,j] = m0.tiles[i,j];
                else
                    tiles[i,j] = m1.tiles[i,j];
            }
        }

        //starting positing
        posStart = rand.Next(0, 2) == 0 ? m0.posStart : m1.posStart;

        //mutate start
        const int chance_mutateStart = 10;
        if (rand.Next(0, chance_mutateStart) == 0) 
            posStart = new Vector2Int(rand.Next(0, bounds.x), rand.Next(0, bounds.y));

        //mutate single random tile     
        tiles[rand.Next(0, bounds.x), rand.Next(0, bounds.y)] = randTileGen.generate(rand);

        nbrSteps = UNSOLVED;
        lastState = null;
    }
    



    private void solve(){
        const int maxNbrVisitedSteps =  100000;

        if (nbrSteps != UNSOLVED) // already solved 
            return;

        State firstStep = new State(this);

        // mark maze as unsolvable if 
        //   - first state already satisfies the win condition (don't want 0 steps solutions)
        //   - or first stap fails precondition (misses basic requirements to ever reach win condition)
        if (WinCondition.winConditions[winconditionIdx].verify(firstStep)
        || WinCondition.winConditions[winconditionIdx].precondition != null && !WinCondition.winConditions[winconditionIdx].precondition(firstStep)
        || WinCondition.winConditions[winconditionIdx].name == "Get stuck" && firstStep.getAllNextStates().Count == 0)
        {
            nbrSteps = UNSOLVABLE;
            return;
        }

        nbrSteps = 0;
        List<State>     queue_current   = new List<State>();
        List<State>     queue_next      = new List<State>();
        HashSet<State>  visitedSteps    = new HashSet<State>();

        queue_current.Add(firstStep);
        while (queue_current.Count > 0) {
            nbrSteps++;

            if (visitedSteps.Count > maxNbrVisitedSteps){
                nbrSteps = UNSOLVABLE;
                return;
            }

            foreach (State s in queue_current){
                bool atLeastOneStepFound = false;                   //used only in win condition:get stuck...
                foreach (State n in s.getAllNextStates()){
                    atLeastOneStepFound = true;
                    if (WinCondition.winConditions[winconditionIdx].verify(n)){
                        lastState = n;
                        return;
                    }

                    //add to queue_next if not visited yet
                    if (visitedSteps.Add(n))
                        queue_next.Add(n);
                }
                if (WinCondition.winConditions[winconditionIdx].name == "Get stuck" && !atLeastOneStepFound) {
                    nbrSteps -= 1;
                    lastState = s;
                    return;
                }
            }

            queue_current.Clear();  
            var temp        = queue_current;
            queue_current   = queue_next;
            queue_next      = temp;
        }
        nbrSteps = UNSOLVABLE;
    }


    

    //returns null if unsolvable
    public List<Vector2Int> getSolution(){ 
        if(nbrSteps == UNSOLVED)    solve();     
        if(nbrSteps == UNSOLVABLE)  return null; 

        List<Vector2Int> solution = new List<Vector2Int>();

        Debug.Assert(lastState != null);
        Dictionary<State, int> visitedStates = new Dictionary<State, int>();
        var rootState   = new State(this);
        var targetState = lastState.Value;
        bool found = dfs_state(ref rootState, nbrSteps, ref targetState, solution, visitedStates);
        Debug.Assert(found);
        Debug.Assert(solution.Count == nbrSteps);

        solution.Reverse();
        return solution;
    }


    // dfs search to find path to targetState (that we know for a fact exists at specific depth)
    // only compares to target state at targetDepth
    // visitedStates needs to be a Dictionary instead of a Hashset in order to track the depth the states where last seen in
    //   since a state leading to the target state could have been seen but dfs was canceled prematurely
    private bool dfs_state(ref State current, int remainingDepth, ref State target, List<Vector2Int> solution, Dictionary<State, int> visitedStates) {
        remainingDepth--;
        foreach (var dir in Dir.DIRECTIONS) {
            State? next = current.move(dir);
            if (next == null)
                continue;

            if (!visitedStates.ContainsKey(next.Value))
                visitedStates.Add(next.Value, remainingDepth);
            else{ 
                if (visitedStates[next.Value] < remainingDepth)
                    visitedStates[next.Value] = remainingDepth;
                else
                    continue;
            }

            var stateNext = next.Value;

            if (    remainingDepth == 0 && stateNext.Equals(target)
                ||  remainingDepth > 0 && dfs_state(ref stateNext, remainingDepth, ref target, solution, visitedStates)) {
                solution.Add(dir);
                return true;
            }
        }

        return false;
    }


    public int getNbrSteps() {
        if (nbrSteps == UNSOLVED)
            solve();
        return nbrSteps;
    }


    public float getFitness(){
        return getNbrSteps();
    }

        
    public Tile[,] getTiles() {
        Tile[,] finalTiles = (Tile[,])tiles.Clone();
        finalTiles[posStart.x, posStart.y].setContent(Content.None, 0);
        return finalTiles;
    }


    public bool insideBounds(Vector2Int pos) {
        return insideBounds(pos.x, pos.y);
    }

    public bool insideBounds(int x, int y) {
        return x >= 0 && y >= 0 && x < getBounds().x && y < getBounds().y;
    }
	
	
    public void removeRow(bool removeHorizontalRow, int index) {
        //index inside of bounds and at the end of this function still need to be >= 1
        Debug.Assert( removeHorizontalRow && getBounds().y > 1 && index < getBounds().y
                  || !removeHorizontalRow && getBounds().x > 1 && index < getBounds().x);
        //dont remove row at player pos
        Debug.Assert(removeHorizontalRow && posStart.y != index
                 || !removeHorizontalRow && posStart.x != index);

        Vector2Int newSize = getBounds();
        if (removeHorizontalRow)    newSize.y--;
        else                        newSize.x--;

        var newTiles = new Tile[newSize.x, newSize.y];

        for (int i=0; i<newSize.x; i++) { 
            for (int j=0; j<newSize.y; j++) {
                int oldX = (!removeHorizontalRow && i >= index) ? i+1 : i;
                int oldY = ( removeHorizontalRow && j >= index) ? j+1 : j;
                newTiles[i, j] = tiles[oldX, oldY];
            }
        }

        tiles = newTiles;

        //decrement player position if removed row preceds it
        if (removeHorizontalRow && index < posStart.y)
            posStart.y--;
      
        if (!removeHorizontalRow && index < posStart.x)
            posStart.x--;
    }


    public void         setTileAt(Vector2Int pos, Tile tile)    { tiles[pos.x, pos.y] = tile;}
    public void         setTileAt(int x, int y, Tile tile)      { tiles[x, y] = tile;}
    public Tile         getTileAt(int x, int y)                 { return tiles[x, y];}
    public Tile         getTileAt(Vector2Int pos)               { return tiles[pos.x, pos.y];}
    public Vector2Int   getPlayerStart()                        { return posStart; }
    public Vector2Int   getBounds()                             { return new Vector2Int(tiles.GetLength(0), tiles.GetLength(1)); }
    public int          getWidth()                              { return tiles.GetLength(0); }
    public int          getHeight()                             { return tiles.GetLength(1); }
    public int          getWinCondition()                       { return winconditionIdx; }




}