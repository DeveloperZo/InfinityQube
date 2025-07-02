using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveMessage
{
    [TextArea(3, 5)]
    public string Message;

    public int DisplayMoveStep = -1;  // -1 means show at any time

    public bool RequirePause = false;

    public float AutoHideDelay = 5f;  // Seconds to auto-hide if not paused

    [Header("Highlight Options")]
    public bool HighlightTile = false;
    public List<Vector2Int> highlightTiles = new List<Vector2Int>();
    public Color highlightColor = Color.yellow;
    
    /// <summary>
    /// Validate this WaveMessage for formatting compliance with new 2-line system
    /// </summary>
    public MessageValidationResult ValidateFormatting()
    {
        return MessageFormatter.ValidateMessage(Message);
    }
    
    /// <summary>
    /// Get formatted version of this message with 2-line enforcement
    /// </summary>
    public string GetFormattedMessage(GameContext context = null, bool enforceActionOriented = false)
    {
        string message = Message;
        
        // Apply dynamic content processing if context provided
        if (context != null)
        {
            message = MessageFormatter.ProcessDynamicContent(message, context);
        }
        
        // Apply action-oriented formatting if requested
        if (enforceActionOriented && !MessageFormatter.IsActionOriented(message))
        {
            message = MessageFormatter.MakeActionOriented(message);
        }
        
        // Enforce 2-line limit
        return MessageFormatter.EnforceTwoLineLimit(message);
    }
}
