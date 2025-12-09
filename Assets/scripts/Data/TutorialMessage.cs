using System;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Extended message class for the tutorial system that maintains compatibility with WaveMessage
/// while adding categorization and context-sensitive trigger conditions
/// </summary>
[Serializable]
public class TutorialMessage : WaveMessage
{
    [Header("Tutorial Extensions")]
    [Tooltip("Message importance level - affects display priority and behavior")]
    public MessageCategory category = MessageCategory.Important;
    
    [Tooltip("Unique identifier for this message - used for one-time tracking")]
    public string messageId;
    
    [Tooltip("Show this message only once per player session")]
    public bool showOnce = false;
    
    [Header("Context Triggers")]
    [Tooltip("Trigger when player has specific number of markers available")]
    public int triggerOnMarkerCount = -1;
    
    [Tooltip("Trigger when specific cube types are present on grid")]
    public List<CubeType> triggerOnCubeTypes = new List<CubeType>();
    
    [Tooltip("Trigger when player is within distance of any cube")]
    public float triggerOnCubeProximity = -1f;
    
    [Tooltip("Minimum time between this message and any other message")]
    public float cooldownSeconds = 2f;
    
    [Header("Progressive Disclosure")]
    [Tooltip("Alternative message for repeat encounters")]
    [TextArea(2, 4)]
    public string shortMessage;
    
    [Tooltip("Use short message after first display")]
    public bool useShortMessageOnRepeat = false;

    /// <summary>
    /// Get the appropriate message text based on repeat status and context
    /// </summary>
    public string GetDisplayMessage(bool isRepeat = false, GameContext context = null, bool enforceFormatting = true)
    {
        string message;
        
        if (isRepeat && useShortMessageOnRepeat && !string.IsNullOrEmpty(shortMessage))
        {
            message = shortMessage;
        }
        else
        {
            message = Message;
        }

        // Apply dynamic content processing if context provided
        if (context != null)
        {
            message = MessageFormatter.ProcessDynamicContent(message, context);
        }

        // Enforce formatting constraints if requested
        if (enforceFormatting)
        {
            message = MessageFormatter.EnforceTwoLineLimit(message);
        }

        return message;
    }

    /// <summary>
    /// Get formatted message with full progressive disclosure and validation
    /// </summary>
    public string GetFormattedMessage(ProgressiveDisclosureContext progressiveContext, bool enforceActionOriented = true)
    {
        string message = MessageFormatter.CreateProgressiveVersion(this, progressiveContext);
        
        if (enforceActionOriented && !MessageFormatter.IsActionOriented(message))
        {
            message = MessageFormatter.MakeActionOriented(message);
        }
        
        return MessageFormatter.EnforceTwoLineLimit(message);
    }

    /// <summary>
    /// Validate this message meets formatting requirements
    /// </summary>
    public new MessageValidationResult ValidateFormatting()
    {
        return MessageFormatter.ValidateMessage(Message);
    }

    /// <summary>
    /// Check if this message is immediately relevant given player capabilities
    /// </summary>
    public bool IsImmediatelyRelevant(GameContext context, PlayerCapabilities capabilities)
    {
        return MessageFormatter.IsImmediatelyRelevant(this, context, capabilities);
    }

    /// <summary>
    /// Check if this message should be displayed based on current game context
    /// </summary>
    public bool ShouldDisplay(GameContext context)
    {
        // Check marker count trigger
        if (triggerOnMarkerCount >= 0 && context.availableMarkers != triggerOnMarkerCount)
            return false;

        // Check cube type triggers
        if (triggerOnCubeTypes.Count > 0)
        {
            bool hasTriggerCube = false;
            foreach (var cubeType in triggerOnCubeTypes)
            {
                if (context.activeCubeTypes.Contains(cubeType))
                {
                    hasTriggerCube = true;
                    break;
                }
            }
            if (!hasTriggerCube) return false;
        }

        // Check proximity trigger
        if (triggerOnCubeProximity > 0 && context.nearestCubeDistance > triggerOnCubeProximity)
            return false;

        return true;
    }
}

/// <summary>
/// Context information for evaluating message triggers
/// </summary>
[Serializable]
public class GameContext
{
    public int availableMarkers;
    public List<CubeType> activeCubeTypes = new List<CubeType>();
    public float nearestCubeDistance = float.MaxValue;
    public Vector2Int playerPosition;
    public int currentMoveStep;
    public bool isGamePaused;
}
