
using System.Collections.Generic;
using static Tile;


public class WinCondition {
    public readonly string                                      name;
    public readonly string                                      tooltip;
    public readonly System.Func<State, bool>                    verify;
    public readonly System.Func<GeneratorInput, List<string>>   validateGeneratorInput;
    public readonly System.Func<State, bool>                    precondition;   // optional performance boost, doesnt' try to solve mazes that don't satisfy precondition 

    public WinCondition(string name, 
                        string tooltip, 
                        System.Func<State, bool>                    verify,
                        System.Func<GeneratorInput, List<string>>   validateGeneratorInput,
                        System.Func<State, bool>                    precondition = null,
                        System.Func<State, int>                     heuristic    = null)
    {
        this.name                   = name;
        this.tooltip                = tooltip;
        this.verify                 = verify;
        this.precondition           = precondition;
        this.validateGeneratorInput = validateGeneratorInput;
    }


    //WinConditions
    public readonly static List<WinCondition> winConditions = new List<WinCondition>{
        
        new WinCondition(
            "Reach red tile",
            "reach one red tile",
            (State s) => { return Ground.Red == s.getTileAt(s.pos.x, s.pos.y).ground; },
            (GeneratorInput g) => {
                if(g.spawn_Ground_Red)  return null;
                return new List<string>{"needs to spawn red tiles."};
            },
            (State s) => { return s.anyTile((Tile t) => {return t.ground == Ground.Red;});}),


        new WinCondition(
            "No light switches",
            "get rid of all light switches",
            (State s) => { return !s.anyTile( (Tile t) => t.content == Content.LightSwitch);},
            (GeneratorInput g) => {
                if(g.spawn_Content_Switch) return null;
                return new List<string>{"needs to spawn light switches."};
            }),
            //no need to add precondition, since if there is no light switch, win condition is met on starting state
       

        new WinCondition(
            "3 Stones in a Row",
            "connect 3 stones in row, both colors count",
            (State s) => { return s.nInARow(3, (Tile t) => { return t.content == Content.Stone;}); },
            (GeneratorInput g) => {
                List<string> errors = new();
                if(!g.spawn_Content_Stone)      errors.Add("needs to spawn stones.");
                if(g.width < 3 && g.height < 3) errors.Add("needs at least one of the dimensions to be 3 or higher.");
                if(errors.Count == 0)
                    return null;
                return errors;
            },
            (State s) => { return ( s.getWidth()>=3 || s.getHeight()>=3)
                                &&  s.atLeastNTiles(3, (Tile t) => {return t.content == Content.Stone ;}); }),


        new WinCondition(
            "Stone Island",
            "all stones (of both colors) must be orthogonally connected",
            (State s) => { return s.singleStoneIsland(); },
            (GeneratorInput g) => {
                if(g.spawn_Content_Stone) return null;
                return new List<string>{"needs to spawn stones."};
            },
            (State s) => { return s.atLeastNTiles(2, (Tile t) => {return t.content == Content.Stone;}); }),
        

        new WinCondition(
            "No touching Stones",
            "no stones (of both colors) can be orthogonally connected to another stone",
            (State s) => { return !s.anyStonesOrthogonallyTouching(); },
            (GeneratorInput g) => {
                if(g.spawn_Content_Stone) return null;
                return new List<string>{"needs to spawn stones."};
            },
            (State s) => { return s.atLeastNTiles(2, (Tile t) => {return t.content == Content.Stone;}); }),


        new WinCondition(
            "Symmetry",
            "make maze either horizontally or vertically symmetrical (player included)",
            (State s) => {
                return s.pos.x*2==s.getWidth()-1  && s.isHorizontalSymetrical()
                    || s.pos.y*2==s.getHeight()-1 && s.isVerticalSymetrical();
            },
            (GeneratorInput g) => {
                List<string> errors = new List<string>();
                if(!(g.spawn_Content_Shift||g.spawn_Content_Stone||g.spawn_Content_Switch))
                    errors.Add("needs to spawn at least one of the tiles \"Light switch\", \"Stone\" or \"Shift\".");
                if(g.width%2 == 0 && g.height%2==0)
                    errors.Add("needs at least one odd dimension.");
                return errors.Count==0?null:errors;
            }),
            //no usefull precondition


        new WinCondition(
            "Uniform ground",
            "make every ground the same color",
            (State s) => {
                return s.allGroundsSameColor();
            },
            (GeneratorInput g) => {
                List<string> errors = new List<string>();
                if(!g.spawn_Content_Switch)
                    errors.Add("needs to spawn \"Light switch.\"");
                if(g.width<3 || g.height<3)
                    errors.Add("both dimensions need to be at least 3 or higher.");
                return errors.Count==0?null:errors;
            }),
            //no usefull precondition


        new WinCondition(
            "Get stuck",
            "get into state where player has no valid moves left",
            (State s) => { return false; },  //handeled inside of Maze.solve()
            (GeneratorInput g) => { 
                if(g.spawn_Content_Shift) return null;
                return new List<string>{"needs to spawn shift tiles."}; 
            })
            //no usefull precondition
    };


}