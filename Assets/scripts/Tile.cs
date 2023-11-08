using UnityEngine;


/*
 *  only stores a single ushort
 *  Tile is composed of 3 sections (Ground, Content + ContentInfo)
 */
public struct Tile {
    //bit offsets
    public const int offset_Content                 = 3;
    public const int offset_ContentInfo             = 6;

    //bit masks
    public const int mask_Ground                    = 0b111;
    public const int mask_Content                   = 0b111     << offset_Content;
    public const int mask_ContentInfo               = 0b11      << offset_ContentInfo;
    public const int mask_ContentAndContentInfo     = mask_Content | mask_ContentInfo;

    //bit masks complements
    public const int maskComp_Ground                 = ~mask_Ground;
    public const int maskComp_Content                = ~mask_Content;
    public const int maskComp_ContentInfo            = ~mask_ContentInfo;
    public const int maskComp_ContentAndContentInfo  = ~mask_ContentAndContentInfo;


    public enum Ground{       
        None            = 0,
        Ice             = 1,
        Bridge          = 2,    
        Red             = 3,
    }


    public enum Content{
        None            = 0  << offset_Content,
        LightSwitch     = 1  << offset_Content,
        Stone           = 2  << offset_Content,
        Shift           = 3  << offset_Content,
    }


    private byte tile;

    //ctors
    public Tile(byte tile)  { this.tile = tile; }
    public Tile(int tile)   { this.tile = (byte)tile; }
    public static implicit operator Tile(Ground tile)   => new Tile((ushort)tile);
    public static implicit operator Tile(Content tile)  => new Tile((ushort)tile);
    public static implicit operator int(Tile tile)      => tile.tile;


    public Ground ground {
        get { return (Ground)(tile & mask_Ground); }
        set { tile = (byte)(tile & maskComp_Ground | (int)value); }
    }

    public Content content {
        get { return (Content)(tile & mask_Content); }
        set { tile = (byte)(tile & maskComp_Content | (int)value); }
    }

    public int contentInfo {
        get { return (tile & mask_ContentInfo) >> offset_ContentInfo; }  //[0,4[
        set { tile = (byte)(tile & maskComp_ContentInfo | ((int)value) << offset_ContentInfo); }
    }


    // sets content and contentInfo at the same time
    // more efficient than setting them individually
    public void setContent(Content content, int contentInfo) {
        Debug.Assert(contentInfo >= 0 && contentInfo < 4);
        tile = (byte) (tile & maskComp_ContentAndContentInfo | (int)content | (contentInfo << offset_ContentInfo));
    }


    public override int GetHashCode() {
        return tile;
    }

    public override bool Equals(object other) {
        return ((Tile)other).tile == tile;
    }



    public static bool isVerticalSymmetrical(Tile t1, Tile t2){
        //has to be at least same type of tile to be symmetrical
        if( t1.ground != t2.ground  ||  t1.content != t2.content)
            return false;
  
        //content   
        Content content = t1.content;
        switch(content){
            case Content.None:
            case Content.LightSwitch:
            case Content.Stone:
                break;
             case Content.Shift:
                 if(!isSimpleDir_vSym(t1.contentInfo, t2.contentInfo))
                     return false;
                break;
            default:
                Debug.Assert(false);
                break;
        }
        return true;
    }



    //left right sym
    public static bool isHorizontalSymmetrical(Tile t1, Tile t2){
        //has to be at least same type of tile to be symmetrical
        if ( t1.ground != t2.ground || t1.content != t2.content)
            return false;

        //content   
        Content content = t1.content;
        switch(content){
            case Content.None:
            case Content.LightSwitch:
            case Content.Stone:
                 break;
            case Content.Shift:
                if(!isSimpleDir_hSym(t1.contentInfo, t2.contentInfo))
                     return false;
                break;
            default:
                Debug.Assert(false);
                break;
        }
        return true;
    }


    // dirs in [0,3]   //{north, east, south, west}
    //return true if input (0,2), (2,0), (1,1) or (3,3)
    public static bool isSimpleDir_hSym(int dir1, int dir2){
        Debug.Assert(dir1>=0 && dir2>=0 && dir1<4 && dir2<4);
        return (6-dir1)%4 == dir2;
    }


    // dirs in [0,3]   //{north, east, south, west}
    //return true if input (0,0), (1,3), (2,2) or (3,1)
    public static bool isSimpleDir_vSym(int dir1, int dir2){
        Debug.Assert(dir1>=0 && dir2>=0 && dir1<4 && dir2<4);
        return (4-dir1)%4 == dir2;
    }

}