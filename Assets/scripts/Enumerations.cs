using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enumerations 
{
    public enum CubeType {Normal, Green, Black }

    public enum TileState
    {
        Normal,
        Transformed 
    }

    public enum DetonationType
    {
        Standard, // 3x3 area
        Small,    // 2x2 area
        Single    // Just the targeted tile
    }
}
