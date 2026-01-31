using UnityEngine;
using Sirenix.OdinInspector;
using System;

/// <summary>
/// Defines a sequence of actions: pause, message, highlight, resume
/// Used by HighlightManager to create guided tutorial experiences
/// </summary>
[Serializable]
public class HighlightSequence
{
    [TableColumnWidth(60, Resizable = false)]
    [LabelText("Step")]
    [Tooltip("Move step when this sequence should execute (-1 = end of wave, 0 = wave start)")]
    public int DisplayMoveStep = 0;
    
    [TableColumnWidth(50, Resizable = false)]
    [LabelText("Pause")]
    [Tooltip("Optional: Pause the game before showing message/highlight")]
    public bool pauseGame = true;
    
    [TableColumnWidth(200)]
    [LabelText("Message")]
    [Tooltip("Optional: Message text to display. Leave empty to skip message.")]
    [TextArea(1, 2)]
    public string messageText = "";
    
    [FoldoutGroup("Message Settings")]
    [ShowIf("@!string.IsNullOrEmpty(messageText)")]
    [LabelText("Require K")]
    [Tooltip("If message requires player to press K to continue")]
    public bool messageRequirePause = true;
    
    [FoldoutGroup("Message Settings")]
    [ShowIf("@!string.IsNullOrEmpty(messageText)")]
    [LabelText("Auto-hide")]
    [Tooltip("Auto-hide message after this many seconds (0 = wait for K press)")]
    public float messageAutoHideDelay = 0f;
    
    [FoldoutGroup("Highlight Target")]
    [Tooltip("What to highlight: Tile, Cube, or None")]
    public HighlightTargetType targetType = HighlightTargetType.Cube;
    
    [FoldoutGroup("Highlight Target")]
    [ShowIf("@targetType != HighlightTargetType.None")]
    [Tooltip("Grid position to highlight (for Tile or Cube)")]
    public Vector2Int targetPosition;
    
    [FoldoutGroup("Highlight Target")]
    [ShowIf("@targetType == HighlightTargetType.Cube")]
    [Tooltip("Cube type to highlight (ONLY used if targetType is Cube - ignored for Tile targets)")]
    public Enumerations.CubeType targetCubeType = Enumerations.CubeType.Unit;
    
    [FoldoutGroup("Highlight Settings")]
    [Tooltip("Highlight color")]
    public Color highlightColor = new Color(0.3f, 0.8f, 0.3f, 0.4f);
    
    [FoldoutGroup("Highlight Settings")]
    [Tooltip("Should highlight pulse")]
    public bool shouldPulse = false;
    
    [FoldoutGroup("Highlight Settings")]
    [Tooltip("Number of move steps to show highlight before auto-clearing (0 = until manually cleared or target captured)")]
    public int highlightDuration = 0;
    
    [FoldoutGroup("Highlight Settings")]
    [ShowIf("@targetType == HighlightTargetType.Cube")]
    [Tooltip("Clear highlight when target is captured (for cubes)")]
    public bool clearOnCapture = true;
    
    [FoldoutGroup("Resume")]
    [ShowIf("pauseGame")]
    [Tooltip("Resume game after sequence completes (only if pauseGame was true)")]
    public bool resumeGame = true;
    
    [FoldoutGroup("Trigger Conditions")]
    [InfoBox("Leave at (0,0) to trigger at DisplayMoveStep timing", InfoMessageType.None)]
    [Tooltip("Trigger this sequence when a marker is placed at this position (0 = trigger at wave start)")]
    public Vector2Int triggerOnMarkerAtPosition = Vector2Int.zero;
    
    [FoldoutGroup("Trigger Conditions")]
    [Tooltip("Trigger this sequence when a cube is captured at this position (0 = trigger at wave start)")]
    public Vector2Int triggerOnCaptureAtPosition = Vector2Int.zero;
    
    [FoldoutGroup("Validation")]
    [Tooltip("If true, pause wave and wait for marker placement at highlighted tile before continuing")]
    public bool requireMarkerPlacementValidation = false;
    
    [FoldoutGroup("Validation")]
    [ShowIf("requireMarkerPlacementValidation")]
    [TextArea(1, 2)]
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
