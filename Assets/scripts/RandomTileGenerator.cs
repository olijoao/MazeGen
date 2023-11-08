

using System.Collections.Generic;
using System.Diagnostics;
using static Tile;


//Helper class to generate random tiles
public class RandomTileGenerator{

    private List<int> spawnableGrounds;
    private List<int> spawnableContents;

    public RandomTileGenerator(GeneratorInput genInput) {
        spawnableGrounds = new List<int>();
        if (genInput.spawn_Ground_None)     spawnableGrounds.Add((int)Ground.None);
        if (genInput.spawn_Ground_Ice)      spawnableGrounds.Add((int)Ground.Ice);
        if (genInput.spawn_Ground_Bridge)   spawnableGrounds.Add((int)Ground.Bridge);
        if (genInput.spawn_Ground_Red)      spawnableGrounds.Add((int)Ground.Red);

        spawnableContents = new List<int>();
        if (genInput.spawn_Content_None)    spawnableContents.Add((int)Content.None);
        if (genInput.spawn_Content_Switch)  spawnableContents.Add((int)Content.LightSwitch);
        if (genInput.spawn_Content_Shift)   spawnableContents.Add((int)Content.Shift);
        if (genInput.spawn_Content_Stone)   spawnableContents.Add((int)Content.Stone);

        Debug.Assert(spawnableGrounds.Count > 0);
        Debug.Assert(spawnableContents.Count  > 0);
    }


    public Tile generate(System.Random rand) { 
        int groundPart = spawnableGrounds[rand.Next(0, spawnableGrounds.Count)];
        int eventPart;
        int eventInfoPart;

        if (groundPart == (int)Ground.None || groundPart == (int)Ground.Ice)
            eventPart = spawnableContents[rand.Next(0, spawnableContents.Count)];
        else //red and bridge don't have content
            eventPart = 0;

        if (eventPart == (int)Content.Shift)
            eventInfoPart = rand.Next(0, 4) << Tile.offset_ContentInfo;
        else
            eventInfoPart = 0;

        return new Tile(groundPart | eventPart | eventInfoPart);
    }
}
