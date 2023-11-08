using UnityEngine;


public class Dir {
    public static readonly Vector2Int NORTH   = new Vector2Int(0, 1);
    public static readonly Vector2Int EAST    = new Vector2Int(1, 0);
    public static readonly Vector2Int SOUTH   = new Vector2Int(0, -1);
    public static readonly Vector2Int WEST    = new Vector2Int(-1, 0);

    public static readonly Vector2Int[] DIRECTIONS = new Vector2Int[] { NORTH, EAST, SOUTH, WEST };

    public static Vector2Int dir2vec(int dir) {
        return DIRECTIONS[dir];
    }

    public static int vec2dir(Vector2Int vec){
        Debug.Assert(vec.magnitude == 1);
        if (vec.x == 0)
            return vec.y == 1 ? 0 : 2;
        return vec.x == 1 ? 1 : 3;
    }
}