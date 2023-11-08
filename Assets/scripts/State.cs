using System.Collections.Generic;
using UnityEngine;
using static Tile;


public struct State {
    
    public  Vector2Int  pos;                        
    public  Tile[,]     tiles;
    private bool        isTileOwner;

    //constructor for first state
    public State(Maze m) {
        this.pos            = m.getPlayerStart();
        this.tiles          = m.getTiles();
        this.isTileOwner    = true;
    }


    // constructor for non-first state
    // exact copy except tilesOwner set to false
    private State(State oldState) {
        this.pos            = oldState.pos;
        this.tiles          = oldState.tiles;
        this.isTileOwner    = false;
    }



    public List<State> getAllNextStates() {
        List<State> states = new List<State>();
        foreach(var d in Dir.DIRECTIONS){
            State? s = move(d); 
            if (s != null)
                states.Add(s.Value);
        }
        return states;
    }


    
    //Animator can be null -> dont store anything in it
    public State? move(Vector2Int dir, Animator animator = null) {
        Debug.Assert(dir.magnitude == 1);

        State nextState = new State(this);

        bool movedAtLeastOnce = false;

        while (true) {
            var pos_next = nextState.pos + dir;
            if (!insideBounds(pos_next))
                break;

            if (!canMoveFromTo(nextState.tileAtPlayer(), nextState.tileAt(pos_next), dir))
                break;
            
            if(animator != null)
                animator.pushNewAnimationGroup();

            //push stone
            if (nextState.tileAt(pos_next).content == Content.Stone) { 
                if (!nextState.moveStoneAt(pos_next, dir, animator)) {
                    if (animator != null)
                        animator.popAnimationGroup();   // so there isn't an empty animation group that get player
                    break;
                }
            }

            //move player
            nextState.pos += dir;
            movedAtLeastOnce = true;
            if (animator != null)
                animator.pushAnimation(new Animation(nextState.pos, dir, Animation.Type.PlayerMove));


            if (processContent(ref nextState, animator))
                break;
            
            if (nextState.tileAtPlayer().ground != Ground.Ice)
                break;
        }

        if (movedAtLeastOnce)
            return nextState;
        else return null;
    }




    public static bool canMoveFromTo(Tile from, Tile to, Vector2Int dir) {
        return  from.ground == Ground.Red 
            ||  from.ground == Ground.Bridge
            ||  to.ground   == Ground.Red
            ||  to.ground   == Ground.Bridge
            ||  from.ground == to.ground;
    }


    

    static bool processContent(ref State stepNext, Animator animator) {
        Content content = stepNext.tileAtPlayer().content;

        if (content == Content.None)
            return false;

        int contentInfo = stepNext.tileAtPlayer().contentInfo;

        switch (content) {
            case Content.Shift:         
                shiftRow(ref stepNext, animator, contentInfo);
                return true;

            case Content.LightSwitch:     
                invert3x3(ref stepNext);
                if (animator != null)
                    animator.pushAnimation(new Animation(stepNext.pos, new Vector2Int(), Animation.Type.Invert3x3));
                return true;

            case Content.Stone:
                // player never on top of stone
                // handeled in move()
                Debug.Assert(false);    
                return false;
            default:                
                Debug.Assert(false);                            
                return false;   
        }
    }


    static void shiftRow(ref State stepNext, Animator animator, int contentInfo) {
        Debug.Assert(contentInfo >= 0 && contentInfo < 4);

        stepNext.cloneTiles();
        stepNext.tiles[stepNext.pos.x, stepNext.pos.y].setContent(Content.None, 0); 

        var pos = stepNext.pos;
        
        Vector2Int size  = stepNext.getBounds();
        Vector2Int sign = Dir.DIRECTIONS[contentInfo] * -1;

        Tile tileAtPlayerPos = stepNext.tileAtPlayer();
        
        //vertical shift
        if (contentInfo % 2 == 0) {
            for (int i = 0; i < size.y - 1; i++)
                stepNext.tiles[pos.x, (pos.y + sign.y * i + size.y) % size.y] = stepNext.tiles[pos.x, (pos.y + sign.y * (i + 1) + size.y) % size.y];
            stepNext.tiles[pos.x, (pos.y + size.y - sign.y) % size.y] = tileAtPlayerPos;

        //horizontal shift
        }else {
            for (int i = 0; i < size.x - 1; i++)
                stepNext.tiles[(pos.x + sign.x * i + size.x) % size.x, pos.y] = stepNext.tiles[(pos.x + sign.x * (i + 1) + size.x) % size.x, pos.y];
            stepNext.tiles[(pos.x + size.x - sign.x) % size.x, pos.y] = tileAtPlayerPos;
        }

        if (animator != null)
            animator.pushAnimation(new Animation(pos, sign * -1, Animation.Type.Shift));

        //player pos
        stepNext.pos = new Vector2Int((stepNext.pos.x - sign.x + size.x) % size.x,
                                      (stepNext.pos.y - sign.y + size.y) % size.y);
    }



    static void invert3x3(ref State stepNext) {
        stepNext.cloneTiles();
        stepNext.tiles[stepNext.pos.x, stepNext.pos.y].setContent(Content.None, 0);
        var size = stepNext.getBounds();
        int xMin = Mathf.Max(stepNext.pos.x-1, 0);
        int xMax = Mathf.Min(stepNext.pos.x+1, size.x-1);
        int yMin = Mathf.Max(stepNext.pos.y-1, 0);
        int yMax = Mathf.Min(stepNext.pos.y+1, size.y-1);
        
        for (int i = xMin; i<=xMax; i++) {
            for (int j=yMin; j<=yMax; j++) {
                var ground = stepNext.tiles[i, j].ground;
                if      (ground == Ground.None)      stepNext.tiles[i, j].ground = Ground.Ice;
                else if (ground == Ground.Ice)       stepNext.tiles[i, j].ground = Ground.None;
            }
        }
    }


    
    // tries to move stone from pos to pos+dir
    // returns true if player is allowed to move
    bool moveStoneAt(Vector2Int pos, Vector2Int dir, Animator animator){
        Debug.Assert(dir.magnitude == 1);
        Debug.Assert(insideBounds(pos));
        Debug.Assert(tiles[pos.x,pos.y].content == Content.Stone);
           
        Vector2Int nextPos = pos + dir;
        if (!insideBounds(nextPos))
            return false;

        Tile tile_this = tileAt(pos);
        Tile tile_next = tileAt(pos+dir);

        if (tile_this.ground != tile_next.ground || tile_next.content != Content.None)
            return false;

        cloneTiles();   
        
        tiles[pos.x, pos.y].setContent(Content.None, 0);
        tiles[nextPos.x, nextPos.y].setContent(Content.Stone, 0);
        if (animator != null)
            animator.pushAnimation(new Animation(pos, dir, Animation.Type.StonePush));

        return true;
    }



    public bool insideBounds(Vector2Int pos) {
        return pos.x >= 0 && pos.y >= 0 && pos.x < getBounds().x && pos.y < getBounds().y;
    }

    public bool insideBounds(int x, int y) {
        return x >= 0 && y >= 0 && x < getBounds().x && y < getBounds().y;
    }


    // copy on write behaviour
    public void cloneTiles() {
        if (isTileOwner)
            return;
        tiles = (Tile[,])tiles.Clone();
        isTileOwner = true;
    }


    public override int GetHashCode(){
        int hash = 17;
        unchecked{
            foreach (var tile in tiles)
                hash = hash * 23 + tile.GetHashCode();
            hash = hash * 23 + pos.GetHashCode();
        }
        return hash;
    }


    public override bool Equals(object obj){
        State otherStep = (State)obj;
        if (otherStep.pos != pos)
            return false;

        //tiles ptr the same
        if (otherStep.tiles == tiles)
            return true;

        //tiles ptr not the same -> check each tile pair
        var bounds = getBounds();
        for (int i=0;i< bounds.x; i++)
            for (int j = 0; j < bounds.y; j++)
                if (!otherStep.tiles[i, j].Equals(tiles[i, j]))
                    return false;
        return true;
    }



    public Tile[,]      getTiles()                  { return tiles;}
    public Tile         tileAt(Vector2Int p)        { return tiles[p.x,p.y];}
    public Tile         tileAtPlayer()              { return tiles[pos.x,pos.y];}
    public Tile         getTileAt(int x, int y)     { return tiles[x,y];}
    public int          getWidth()                  { return tiles.GetLength(0); }
    public int          getHeight()                 { return tiles.GetLength(1); }
    public Vector2Int   getBounds()                 { return new Vector2Int(tiles.GetLength(0), tiles.GetLength(1)); }


    // check if n tiles in a row satisfy predicate
    public bool nInARow(int n, System.Func<Tile, bool> predicate){
        var bounds = getBounds();
        int count = 0;

        for (int i = 0; i < bounds.x; i++) { 
            for (int j = 0; j < bounds.y; j++){
                if (predicate(tiles[i, j]))
                    count++;
                else
                    count = 0;

                if (count == n)
                    return true;
            }
            count = 0;
        }

        for (int j = 0; j < bounds.y; j++){
            for (int i = 0; i < bounds.x; i++) { 
                if (predicate(tiles[i, j]))
                    count++;
                else
                    count = 0;

                if (count == n)
                    return true;
            }
            count = 0;
        }
        return false;
    }


    // check if at least n tiles satisfy predicate
    public bool atLeastNTiles(int n, System.Func<Tile, bool> predicate) {
        foreach (Tile t in tiles)
            if (predicate(t) && --n == 0)
                return true;
        return false;
    }


    // check if any tile satisfy predicate
    public bool anyTile(System.Func<Tile, bool> predicate){
        foreach (Tile t in tiles) 
            if (predicate(t))
                return true;
        return false;
    }

    // number of tiles that satisfy predicate
    public int countTiles(System.Func<Tile, bool> predicate) {
        int count = 0;
        foreach (Tile t in tiles)
            if (predicate(t))
                count++;
        return count;
    }



    // returns true if tiles[,] contains a single stone island
    // (a group of connect stones form an island)
    public bool singleStoneIsland(){
        var bounds = getBounds();
    
        //Implementation: search for stones than use depth first search to mark all connected stones as visited
        bool[,] visited = new bool[bounds.x, bounds.y];
        bool foundOneIsland = false;
    
        for (int i = 0; i < bounds.x; i++){
            for (int j = 0; j < bounds.y; j++){
                if ((tiles[i, j].content == Content.Stone) && !visited[i, j]) {
                    if (foundOneIsland) {
                        return false;
                    }else{ 
                        dfs_singleStoneIsland(i, j, visited);
                        foundOneIsland = true;
                    }
                }  
            }
        }
        return foundOneIsland;
    }
 

    private void dfs_singleStoneIsland(int x, int y, bool[,] visited){
        if (    !insideBounds(x, y) 
            ||  visited[x, y]
            ||  tiles[x, y].content != Content.Stone)
        { 
            return;
        }
    
        visited[x,y] = true;
    
        foreach(var d in Dir.DIRECTIONS)
            dfs_singleStoneIsland(x+d.x, y+d.y, visited);
    }

 
 
    public bool isSymetrical(){
        return isHorizontalSymetrical() 
            || isVerticalSymetrical();
    } 


    public bool isHorizontalSymetrical(){ 
        var bounds = getBounds();
        var halfbounds = (bounds - new Vector2Int(1, 1)) / 2 + new Vector2Int(1, 1);

        for (int y = 0; y < bounds.y; y++)
            for (int x = 0; x < halfbounds.x; x++)
                if (!Tile.isVerticalSymmetrical(tiles[x, y], tiles[bounds.x -1 -x, y]))
                    return false;

        return true;
    }


    public bool isVerticalSymetrical(){
        var bounds = getBounds();
        var halfbounds = (bounds - new Vector2Int(1, 1)) / 2 + new Vector2Int(1, 1);

        for (int x = 0; x < bounds.x; x++)
            for (int y = 0; y < halfbounds.y; y++)
                if(!Tile.isHorizontalSymmetrical(tiles[x, y], tiles[x, bounds.y -1 -y]))
                    return false;
        return true;
    }



    //Given 2 consecutive Steps, find direction from first to second step
    //return (0,0) if both steps not consecutive
    public static Vector2Int getDirectionFromTo(State s0, State s1){ 
        foreach(var dir in Dir.DIRECTIONS){ 
            var newStep = s0.move(dir);
            if(newStep != null && newStep.Equals(s1)) 
                return dir;
        }
        return new Vector2Int(0,0);
    }

}