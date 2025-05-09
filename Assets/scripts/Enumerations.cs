using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enumerations 
{
    public enum CubeType {Normal, Blue, Black }

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

    public enum StageType
    {
        Tutorial,    // Tutorial stages: focused on teaching mechanics
        Standard,    // Normal gameplay
        Challenge,   // Difficult stages with special conditions
        Bonus        // Special stages with unique rules
    }
}
