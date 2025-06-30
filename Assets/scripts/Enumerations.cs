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
    /// <summary>
    /// Defines the different types of cubes in the game
    /// </summary>
    public enum CubeType 
    {
        /// <summary>Basic cube type (formerly Normal)</summary>
        Unit,
        /// <summary>Area coverage cube type (formerly Blue)</summary>
        Prime,
        /// <summary>Special corruption cube type (formerly Black)</summary>
        Infinity,
        /// <summary>Enhanced durability cube type (formerly Reinforced)</summary>
        Recursion,
        
    }

    /// <summary>
    /// Defines the four-tier marker system for targeting
    /// </summary>
    public enum MarkerType
    {
        /// <summary>Light marker: Basic targeting (formerly Individual)</summary>
        Light,
        /// <summary>Heavy marker: Enhanced marker for recursion cubes (NEW)</summary>
        Heavy,
        /// <summary>Prime marker: Area coverage marker (formerly Area)</summary>
        Prime,
        /// <summary>Cube marker: Generated from prime cube captures</summary>
        Cube
    }

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

    /// <summary>
    /// Defines message importance and display priority for the tutorial system
    /// </summary>
    public enum MessageCategory
    {
        /// <summary>Critical messages that block gameplay until acknowledged</summary>
        Essential,
        /// <summary>Important guidance that should be prominently displayed</summary>
        Important,
        /// <summary>Contextual hints that enhance understanding but don't interrupt flow</summary>
        Contextual,
        /// <summary>Debug and development messages for testing</summary>
        Debug
    }

    /// <summary>
    /// Defines all game audio events for the event-driven audio system
    /// </summary>
    public enum GameAudioEvent
    {
        // Cube Events
        CubeLanded,         // When a cube lands on the grid
        CubeCaptured,       // When a cube is captured by the player
        CubeEscaped,        // When a cube escapes the grid
        
        // Player Events
        PlayerMoved,        // When the player moves position
        
        // Marker Events
        LightMarkerPlaced,  // When a light marker is placed
        PrimeMarkerPlaced,  // When a prime marker is placed
        HeavyMarkerPlaced,  // When a heavy marker is placed
        MarkerTriggered,    // When any marker is triggered
        
        // Wave Events
        WaveStarted,        // When a new wave begins
        WaveCompleted,      // When a wave is completed
        
        // System Events
        ResourceRegeneration // When resources regenerate
    }
}
