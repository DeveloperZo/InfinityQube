using UnityEngine;
using System;
/// <summary>
/// Defines a sequence of actions: pause, message, highlight, resume
/// Used by HighlightManager to create guided tutorial experiences
/// </summary>
[Serializable]
public class HighlightSequence
{
    [Header("Sequence Configuration")]
    [Tooltip("Move step when this sequence should execute (-1 = end of wave, 0 = wave start)")]
    public int DisplayMoveStep = 0;
    
    [Tooltip("Optional: Pause the game before showing message/highlight")]
    public bool pauseGame = true;
    
    [Header("Message (Optional)")]
    [Tooltip("Optional: Message text to display. Leave empty to skip message.")]
    [TextArea(2, 4)]
    public string messageText = "";
    
    [Tooltip("If message requires player to press K to continue")]
    public bool messageRequirePause = true;
    
    [Tooltip("Auto-hide message after this many seconds (0 = wait for K press)")]
    public float messageAutoHideDelay = 0f;
    
    [Header("Highlight Target")]
    [Tooltip("What to highlight: Tile, Cube, or None")]
    public HighlightTargetType targetType = HighlightTargetType.Cube;
    
    [Tooltip("Grid position to highlight (for Tile or Cube)")]
    public Vector2Int targetPosition;
    
    [Tooltip("Cube type to highlight (ONLY used if targetType is Cube - ignored for Tile targets)")]
    public Enumerations.CubeType targetCubeType = Enumerations.CubeType.Unit;
    
    [Header("Highlight Settings")]
    [Tooltip("Highlight color")]
    public Color highlightColor = new Color(0.3f, 0.8f, 0.3f, 0.4f);
    
    [Tooltip("Should highlight pulse")]
    public bool shouldPulse = false;
    
    [Tooltip("Number of move steps to show highlight before auto-clearing (0 = until manually cleared or target captured)")]
    public int highlightDuration = 0;
    
    [Tooltip("Clear highlight when target is captured (for cubes)")]
    public bool clearOnCapture = true;
    
    [Header("Resume")]
    [Tooltip("Resume game after sequence completes (only if pauseGame was true)")]
    public bool resumeGame = true;
    
    [Header("Trigger Conditions (Optional)")]
    [Tooltip("Trigger this sequence when a marker is placed at this position (0 = trigger at wave start)")]
    public Vector2Int triggerOnMarkerAtPosition = Vector2Int.zero;
    
    [Tooltip("Trigger this sequence when a cube is captured at this position (0 = trigger at wave start)")]
    public Vector2Int triggerOnCaptureAtPosition = Vector2Int.zero;
    
    [Header("Validation (Optional)")]
    [Tooltip("If true, pause wave and wait for marker placement at highlighted tile before continuing")]
    public bool requireMarkerPlacementValidation = false;
    
    [Tooltip("Message to show if marker is placed incorrectly")]
    public string validationFailureMessage = "Place your marker on the highlighted tile.";
}

/// <summary>
/// Type of target to highlight
/// </summary>
public enum HighlightTargetType
{
    None,   // No highlight
    Tile,   // Highlight a tile (for marker placement guidance)
    Cube    // Highlight a cube (for capture guidance)
}

