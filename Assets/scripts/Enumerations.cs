using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Enumerations 
{
    public enum FaceStatus
    {
        None,
        Corrupted,      // Acts like black cube when active
        Enhanced,       // Creates detonation when captured
    }

    public enum CubeFace
    {
        Bottom = 0, Top = 1, Front = 2, Back = 3
    }

    public enum FacePosition
    {
        Down, Up, Forward, Back
    }
    public enum CubeType {Normal, Blue, Black, Reinforced }

    public enum TileState
    {
        Normal,
        Transformed 
    }

    public enum DetonationType
    {
        Large,
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
    public enum DebugPanelGroup
    {
        Core,           // Grid, Game Control, System
        Wave,
        Cube,
        Gameplay,       // Wave, Stage, Player
        Content,        // Tiles, Cubes, Actions
        Testing         // Face Painting, Scenarios
    }
}
