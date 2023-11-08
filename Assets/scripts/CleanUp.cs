using System.Collections.Generic;
using UnityEngine;
using static Tile;


public class CleanUp {
    struct CleanUpRule {
        public System.Func<GeneratorInput, Tile, bool> condition;
        public System.Func<Tile, Tile> consequence;
        public CleanUpRule(System.Func<GeneratorInput, Tile, bool> condition, System.Func<Tile, Tile> consequence) {
            this.condition      = condition;
            this.consequence    = consequence;
        }
    }


    // rules have to form a DAG or clean up causes inf. loop
    // rules ordered by (general -> specific ) for increased performance
    private static List<CleanUpRule> cleanUpRules = new List<CleanUpRule>(){
        //bridge -> black
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Ground_None && genInput.spawn_Content_None && tile.ground == Ground.Bridge; },
                            (           tile) => { tile.ground = Ground.None; return tile; }),
        
        //bridge -> white
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Ground_Ice && genInput.spawn_Content_None && tile.ground == Ground.Bridge; },
                            (           tile) => { tile.ground = Ground.Ice; return tile; }),
        
        //red -> black
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Ground_None && genInput.spawn_Content_None && tile.ground == Ground.Red; },
                            (           tile) => { tile.ground = Ground.None; return tile; }),
        
        //red -> white
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Ground_Ice && genInput.spawn_Content_None && tile.ground == Ground.Red; },
                            (           tile) => { tile.ground = Ground.Ice; return tile; }),
        
        //red -> bridge
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Ground_Bridge &&  tile.ground == Ground.Red; },
                            (           tile) => { tile.ground = Ground.Bridge; return tile; }),
        
        //content -> noContent
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Content_None && tile.content != Content.None; },
                            (           tile) => { tile.content = Content.None; return tile; }),

        //stone -> no stone + invert ground
        new CleanUpRule(    (genInput,  tile) => { return genInput.spawn_Content_None && genInput.spawn_Ground_None &&  genInput.spawn_Ground_Ice && tile.content == Content.Stone; },
                            (           tile) => { tile.setContent(Content.None, 0);
                                                   Debug.Assert(tile.ground==Ground.None || tile.ground==Ground.Ice);
                                                   tile.ground = (tile.ground==Ground.None)?Ground.Ice:Ground.None;
                                                   return tile; 
                                                  })
    };




    public static Maze cleanUp(GeneratorInput geninput, Maze mazeOld){
        if (!geninput.isCleaningUp)
            return mazeOld;

        Maze maze_cleanedUp = new Maze(mazeOld);

        //Maze has no solution: trivial
        if (maze_cleanedUp.getFitness() == Maze.UNSOLVABLE) {
            var bounds = maze_cleanedUp.getBounds();
            for (int i=0; i< bounds.x; i++)
                for (int j = 0; j < bounds.y; j++)
                    maze_cleanedUp.setTileAt(i, j, Ground.None);                     
            return maze_cleanedUp;
        }


        bool change_onLastIteration = true;
        while (change_onLastIteration){
            change_onLastIteration = false;

            if (geninput.isCleanUpTrimingSize) {
                try_removeRow(ref maze_cleanedUp, ref change_onLastIteration, false, geninput.isCleanUpMakingHarder);
                try_removeRow(ref maze_cleanedUp, ref change_onLastIteration, true, geninput.isCleanUpMakingHarder);
            }

            // for each tile
            var bounds = maze_cleanedUp.getBounds();
            for (int i=0; i<bounds.x; i++){
                for (int j = 0; j < bounds.y; j++){

                    //For each rule
                    foreach (CleanUpRule r in cleanUpRules) {
                        if (r.condition(geninput, maze_cleanedUp.getTileAt(new Vector2Int(i,j)))){
                            Maze maze_temp = new Maze(maze_cleanedUp); 
                            maze_temp.setTileAt(new Vector2Int(i, j), r.consequence(maze_temp.getTileAt(new Vector2Int(i, j))));

                            if (!geninput.isCleanUpMakingHarder && maze_temp.getFitness() == maze_cleanedUp.getFitness()
                            || geninput.isCleanUpMakingHarder && maze_temp.getFitness() >= maze_cleanedUp.getFitness()) 
                            {
                                maze_cleanedUp = maze_temp;
                                change_onLastIteration = true;
                            }
                        }
                    }

                }
            }           
        }

        return maze_cleanedUp;
    }


    
    public static void try_removeRow(ref Maze maze, ref bool changed, bool vertical, bool makeHarder) { 
        int row = 0;
        var size = vertical?maze.getHeight():maze.getWidth();

        while (row < size && size>1) {  //don't delete last row (size>1)
            if (!vertical && maze.getPlayerStart().x == row || vertical && maze.getPlayerStart().y == row) {
                row++;
                continue;
            }
                   
            Maze maze_temp = new Maze(maze);
            maze_temp.removeRow(vertical, row);
            if (!makeHarder && maze_temp.getFitness() == maze.getFitness()
            || makeHarder && maze_temp.getFitness() >= maze.getFitness())
            {
                maze = maze_temp;
                changed = true;
                size--;
            }else
                row++;
        }
    }

}