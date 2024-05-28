using UnityEngine;
using System;
using System.Collections.Generic;


public class GeneratorInput {
    //Generator 
    public int          nbrIndividuals          = 100;
    public int          nbrGenerations          = 100;
    public int          minFitness              = 20;
    public int          maxTime                 = 30;
    public bool         isCleaningUp            = true;
    public bool         isCleanUpMakingHarder   = false;
    public bool         isCleanUpTrimingSize    = true;

    //Maze 
    public int          width                   = 5;
    public int          height                  = 5;
    public byte         winCondition            = 0;

    //Tiles
    public bool         spawn_Ground_None       = true;
    public bool         spawn_Ground_Ice        = true;
    public bool         spawn_Ground_Bridge     = true;
    public bool         spawn_Ground_Red        = true;
    public bool         spawn_Content_None      = true;
    public bool         spawn_Content_Switch    = false;
    public bool         spawn_Content_Shift     = false;
    public bool         spawn_Content_Stone     = false;


    // returns empty string if no error
    public string validate() {
        string errorMessage = "";

        if (    !spawn_Ground_None      && !spawn_Ground_Ice 
            &&  !spawn_Ground_Bridge    && !spawn_Ground_Red)
            errorMessage += "<b>Error:</b> Select at least one ground tile.\n";

        if (    !spawn_Content_None    && !spawn_Content_Shift
            &&  !spawn_Content_Switch  && !spawn_Content_Stone)
            errorMessage += "<b>Error:</b> Select at least one content tile.\n";


        var wc = WinCondition.winConditions[winCondition];
        var errors = wc.validateGeneratorInput(this);
        if (errors != null) {
            foreach (var error in errors)
                errorMessage += "<b>Error:</b> Wincondition \"" + wc.name +"\" "+ error + "\n";           
        }
        
        return errorMessage;
    }
}





public class Generator{
    public const int maxNbrVisitedSteps  = 100000;

    //static
    public static   System.Random           rand                = new System.Random();  //in case I want to test with fixed seed
    public static   RandomTileGenerator     randTileGen;

    public enum GeneratorState { Idle, Generating, EndingPrematurly, CleaningUp, Solving, Done };
    public GeneratorState   generatorState;
    private List<Maze>      mazes;       
    public int              iteration;
    private long            startTime;
    public readonly GeneratorInput generatorInput;

    //stats to display to the user
    public float    highestFitness;  //only used for ui,  during iteration mazes[0] is not the highest until list gets sorted at the end
    public int      currentIndividual;


            
    public Generator(GeneratorInput input){
        this.generatorInput = input;
        generatorState = GeneratorState.Idle;
        iteration = 0;
        currentIndividual = 0;
        highestFitness = Maze.UNSOLVABLE;

        randTileGen = new RandomTileGenerator(generatorInput);
        mazes = new List<Maze>();
    }



    public void startEvolution(){
        startTime = DateTime.Now.Ticks;
        generatorState = GeneratorState.Generating;
        //initial mazes
        for (int i = 0; i < generatorInput.nbrIndividuals; i++){    
            currentIndividual = i;
            var maze = new Maze(generatorInput, randTileGen, rand);
            var fitness = maze.getFitness();  //most time consuming part, calculating it here makes aborting early more accurate
            mazes.Add(maze);

            // save highest fitness so UI can display it while evolution is running
            if (fitness > highestFitness)
                highestFitness = fitness;

            if (generatorState == GeneratorState.EndingPrematurly
            || fitness >= generatorInput.minFitness || passedTime() >= generatorInput.maxTime)      
            {
                mazes.Sort(new MazeSortHelper()); //uses maze.getFitness()
                return;
            }
        }
        mazes.Sort(new MazeSortHelper());
    }


    public void endEvolution(){
        var bestMaze = mazes[0];

        if (generatorInput.isCleaningUp) {
            generatorState = GeneratorState.CleaningUp;
            Play.maze = CleanUp.cleanUp(generatorInput, bestMaze);
        }else
            Play.maze = bestMaze;

        generatorState = GeneratorState.Solving;
        Play.solution = new Solution(ref Play.maze);

        generatorState = GeneratorState.Done;
    }


    public void evolve(){
        startEvolution();
        
        while (nextGeneration()) {
            /*..*/;
        }
        endEvolution();
    }


    public float passedTime(){ 
        if(generatorState != GeneratorState.Generating)
            return 0;
        return (float)((DateTime.Now.Ticks-startTime)/1e7);
    }



    public int getIteration(){ 
        return iteration;
    }

    


    //returns true if it completed an iteration
    public bool nextGeneration() {
        if (iteration >= generatorInput.nbrGenerations) 
            return false;

        iteration++;
        
        for(int i=0; i< generatorInput.nbrIndividuals; i++){
            currentIndividual = i;
            if (generatorState == GeneratorState.EndingPrematurly
            || highestFitness >= generatorInput.minFitness || passedTime() >= generatorInput.maxTime){
                mazes.Sort(new MazeSortHelper());
                return false;
            }

            var maze = new Maze(selectMazeForCrossover(), selectMazeForCrossover(), randTileGen, rand);
            var fitness = maze.getFitness();  //most time consuming part, calculating it here makes aborting on (passedTime() >= maxTime) more accurate, since Maze caches the result

            // save highest fitness so UI can display it while evolution is running
            if (fitness > highestFitness)
                highestFitness = fitness;

            mazes.Add(maze);
        }

        mazes.Sort(new MazeSortHelper());
        mazes.RemoveRange(generatorInput.nbrIndividuals -1, mazes.Count- generatorInput.nbrIndividuals);

        return true;
    }


    
    public class MazeSortHelper: IComparer<Maze>{
        public int Compare(Maze a, Maze b) {
            if (a.getFitness() > b.getFitness())
                return -1;
            return 1;
        }
    }


        
    //linear rank selection
    private Maze selectMazeForCrossover(){
        int totalWeight     = (generatorInput.nbrIndividuals)*(generatorInput.nbrIndividuals +1)/2;
        int selectionWeight = Generator.rand.Next(1, totalWeight+1);
        
        int sum = generatorInput.nbrIndividuals;
        for (int i = 0; i < generatorInput.nbrIndividuals; i++) {
            if (selectionWeight <= sum)
                return mazes[i];
            sum += generatorInput.nbrIndividuals - i -1;
        }   

        Debug.Assert(false);    //should be unreachable
        return mazes[mazes.Count-1];
    }
}