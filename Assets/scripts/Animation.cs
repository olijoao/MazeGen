using System.Collections.Generic;
using UnityEngine;
using System;
using static Tile;


public class Animation{
    public enum Type { Invert3x3, Shift, PlayerMove, StonePush };
    public Vector2Int pos;
    public Vector2Int dir;
    public Type type;

    public Animation(Vector2Int pos, Vector2Int dir, Type type) {
        this.pos    = pos;
        this.dir    = dir;
        this.type   = type;
    }
}



public class Animator {
    private float   t_animation = -1;     // animating if >= 0
    private float   animationSpeed = 4;
    private int     lastAnimationIndex;  //used to make sure the current animation gets played to the end before starting new one


    private List<List<Animation>> animations = new List<List<Animation>>(); 

    public void pushAnimation(Animation animation) {
        if (animations.Count == 0)
            pushNewAnimationGroup();
        animations[animations.Count - 1].Add(animation);
    }

    public void pushNewAnimationGroup() {
        animations.Add(new List<Animation>());
    }

    public void popAnimationGroup(){
        if (animations.Count == 0)
            return;
        animations.RemoveAt(animations.Count - 1);
    }

    public void clear() {
        animations.Clear();
    }


    //returns true if currently animating
    public bool animate(State step, GameObject[,] gMazeTiles, GameObject gObjPlayer, UnityEngine.Events.UnityAction onAnimationEnd) {
        if (animations.Count == 0)
            return false;

        //animation start
        if (t_animation == -1){ 
            lastAnimationIndex = 0;
            if (animations.Count > 0)
                t_animation = 0;
            else
                return false;
        }

        float t_animation_last = t_animation;
        t_animation += Time.deltaTime * animationSpeed;
        int currentAnimationIndex = (int)t_animation;

        //make sure previous animation has played to its end
        if (lastAnimationIndex < currentAnimationIndex) { 
            foreach (Animation animation in animations[lastAnimationIndex]) {
                animate_internal(1.0f, t_animation_last, animation, step, gMazeTiles, gObjPlayer);
            }
        }

        //animation end
        if (t_animation > animations.Count){
            t_animation = -1;
            animations.Clear();
            onAnimationEnd();
            return false;
        }

        lastAnimationIndex = currentAnimationIndex;

        float t = Mathf.Clamp(t_animation - (int)t_animation, 0.0f, 1.0f);
        float t_last = Mathf.Clamp(t_animation_last - (int)t_animation_last, 0.0f, 1.0f);
        foreach (Animation animation in animations[currentAnimationIndex]) {
            animate_internal(t, t_last, animation, step, gMazeTiles, gObjPlayer);
        }

        return true;
    }


    private void animate_internal(float t01_animation, float t01_animation_last, Animation animation, State step, GameObject[,] gMazeTiles, GameObject gObjPlayer){
        t01_animation = Math.Min(Math.Max(0, t01_animation),1);

        switch (animation.type) {

            case Animation.Type.PlayerMove:{
                gObjPlayer.transform.position += new Vector3(animation.dir.x, 0, animation.dir.y) * Time.deltaTime * animationSpeed;
                break;
            }

            case Animation.Type.Invert3x3:{
                if(t01_animation_last < 0.1 && 0.1f <= t01_animation ) {
                    var audioSource = Camera.main.gameObject.GetComponent<AudioSource>();
                    audioSource.PlayOneShot(GameObject.Find("Main").GetComponent<Play>().audio_lightSwitch);
                }

                animateConsumeContent(t01_animation, animation, step, gMazeTiles);
                if (t01_animation < 0.5) 
                    break;
                var size = step.getBounds();
                int xMin = (animation.pos.x == 0)        ?0:-1;
                int yMin = (animation.pos.y == 0)        ?0:-1;
                int xMax = (animation.pos.x == size.x-1) ?0:+1;
                int yMax = (animation.pos.y == size.y-1) ?0:+1;
                
                for (int i = xMin; i <= xMax; i++){
                    int x = animation.pos.x + i;
                    for (int j = yMin; j <= yMax; j++){
                        int y = animation.pos.y + j;
                        gMazeTiles[x,y].GetComponent<MeshRenderer>().material.SetVector("_switch_pos", new Vector4(i,j,0,0));
                        gMazeTiles[x,y].GetComponent<MeshRenderer>().material.SetFloat("_switch_radius", (t01_animation-0.5f)/0.5f);
                    }
                }
                break;
            }


            case Animation.Type.StonePush:{ 
                var pos             = animation.pos;
                var dir             = animation.dir;
                var posNext         = pos + dir;
                var stonePosOldTile = new Vector2(-dir.x, -dir.y) * t01_animation;
                var stonePosNewTile = new Vector2(dir.x, dir.y) * (1.0f - t01_animation);

                //move out of current
                gMazeTiles[pos.x, pos.y].GetComponent<MeshRenderer>().material.SetVector("_stone_pos", new Vector4(stonePosOldTile.x, stonePosOldTile.y, 0,0));

                //move in into next
                Tile tileNewPos = new Tile(gMazeTiles[posNext.x, posNext.y].GetComponent<MeshRenderer>().material.GetInt("_Tile"));
                tileNewPos.content = Content.Stone;
                tileNewPos.contentInfo = 0;
                gMazeTiles[posNext.x, posNext.y].GetComponent<MeshRenderer>().material.SetInt("_Tile", tileNewPos);
                gMazeTiles[posNext.x, posNext.y].GetComponent<MeshRenderer>().material.SetVector("_stone_pos", new Vector4(stonePosNewTile.x, stonePosNewTile.y, 0,0));
                break;
            }


            case Animation.Type.Shift: {
                //sound
                if(t01_animation_last < 0.3 && 0.3f <= t01_animation ) {
                    var audioSource = Camera.main.gameObject.GetComponent<AudioSource>();
                    audioSource.PlayOneShot(GameObject.Find("Main").GetComponent<Play>().audio_shift);
                }

                animateConsumeContent(t01_animation, animation, step, gMazeTiles);

                Vector2Int pos  = animation.pos;
                Vector2Int dir  = animation.dir;
                Vector2Int size = step.getBounds();
                
                //determine position of tile that moves around
                Vector3Int p0 = new();
                if      (dir == Dir.NORTH) { p0 = new Vector3Int(pos.x,       0, size.y - 1); }
                else if (dir == Dir.SOUTH) { p0 = new Vector3Int(pos.x,       0, 0); }
                else if (dir == Dir.EAST)  { p0 = new Vector3Int(size.x - 1,  0, pos.y); }
                else if (dir == Dir.WEST)  { p0 = new Vector3Int(0,           0, pos.y); }

                // tile going out
                if (t01_animation < 0.5f){
                    if (pos.x == p0.x && pos.y == p0.z) { 
                        gObjPlayer.transform.parent = gMazeTiles[pos.x, animation.pos.y].transform;
                        gObjPlayer.transform.localPosition = new Vector3(0, 0, 0);
                    }
                    Vector3 p1 = p0 + new Vector3(dir.x, 0, dir.y);
                    gMazeTiles[p0.x, p0.z].transform.position = Vector3.Lerp(p0, p1, t01_animation / 0.5f);

                }

                //tile coming in
                 if (t01_animation >= 0.5f){
                    // player moves with tile under him
                    gObjPlayer.transform.parent = gMazeTiles[animation.pos.x, animation.pos.y].transform;
                    gObjPlayer.transform.localPosition = new Vector3(0, 0, 0);

                        Vector3 p4, p5;
                    p5 = p0;
                    if (dir.x == 0) p5.z = size.y - 1 - p5.z;
                    else p5.x = size.x - 1 - p5.x;
                    p4 = p5 - new Vector3(dir.x, 0, dir.y);
                    gMazeTiles[p0.x, p0.z].transform.position = Vector3.Lerp(p4, p5, (t01_animation-0.5f) / 0.5f);

                    // push all the other tiles
                    int i = (dir == Dir.NORTH || dir == Dir.EAST)?0:1;
                    int range = 0;
                    if(dir == Dir.NORTH)        range = size.y - 1;
                    else if(dir == Dir.SOUTH)   range = size.y;
                    else if(dir == Dir.EAST)    range = size.x-1;
                    else if(dir == Dir.WEST)    range = size.x;
                    
                    if (dir == Dir.NORTH || dir == Dir.SOUTH)
                        for (; i < range; i++)
                            gMazeTiles[p0.x, i].transform.position = Vector3.Lerp(new Vector3(p0.x, 0, i) + new Vector3(dir.x, 0, dir.y), new Vector3(p0.x, 0, i), (1.0f - t01_animation) / 0.5f);
                    else
                        for (; i < range; i++)
                            gMazeTiles[i, p0.z].transform.position = Vector3.Lerp(new Vector3(i, 0, p0.z) + new Vector3(dir.x, 0, dir.y), new Vector3(i, 0, p0.z), (1.0f - t01_animation) / 0.5f);
                }
                break;
            }

            default:{
                Debug.Assert(false);
                break;
            }
        }
    }   



    private void animateConsumeContent(float t01_animation, Animation animation, State step, GameObject[,] gMazeTiles) {
        if (t01_animation > 0.5){
            var pos = animation.pos;
            var tile = step.tiles[pos.x, pos.y];
            tile.setContent(Content.None, 0);
            gMazeTiles[pos.x, pos.y].GetComponent<MeshRenderer>().material.SetInt("_Tile", tile);
        }
    }



}
