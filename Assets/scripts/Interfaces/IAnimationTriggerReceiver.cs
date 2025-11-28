using UnityEngine;
using static Enumerations;

/// <summary>
/// Interface for receiving animation trigger events from the animation trigger system.
/// Components implementing this interface can register with AnimationTriggerManager 
/// to receive callbacks for specific animation trigger points.
/// </summary>
public interface IAnimationTriggerReceiver
{
    /// <summary>
    /// Called when an animation trigger point is activated.
    /// Implementation should handle the trigger event appropriately for the specific component.
    /// </summary>
    /// <param name="triggerPoint">The type of animation trigger that was activated</param>
    /// <param name="context">Context data providing details about the trigger event</param>
    void OnAnimationTrigger(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context);
    
    /// <summary>
    /// Gets the display name for this animation receiver (used for debugging)
    /// </summary>
    /// <returns>Human-readable name for this receiver</returns>
    string GetReceiverName();
    
    /// <summary>
    /// Indicates whether this receiver is currently active and should receive triggers
    /// </summary>
    /// <returns>True if the receiver should receive triggers, false otherwise</returns>
    bool IsReceiverActive();
}

/// <summary>
/// Context data structure providing details about animation trigger events.
/// Contains all necessary information for animation receivers to respond appropriately.
/// </summary>
[System.Serializable]
public struct AnimationTriggerContext
{
    /// <summary>
    /// Primary position associated with the trigger (grid or world coordinates)
    /// </summary>
    public Vector3 primaryPosition;
    
    /// <summary>
    /// Secondary position for triggers that involve two locations
    /// </summary>
    public Vector3 secondaryPosition;
    
    /// <summary>
    /// Intensity or magnitude of the trigger event (0.0 to 1.0)
    /// </summary>
    public float intensity;
    
    /// <summary>
    /// Duration hint for animation timing (in seconds)
    /// </summary>
    public float duration;
    
    /// <summary>
    /// Specific marker mode associated with the trigger (if applicable)
    /// </summary>
    public MarkerMode markerMode;
    
    /// <summary>
    /// Additional string data for context-specific information
    /// </summary>
    public string additionalData;
    
    /// <summary>
    /// Generic object reference for passing complex data
    /// </summary>
    public object dataReference;
    
    /// <summary>
    /// Creates a basic context with minimal data
    /// </summary>
    /// <param name="position">Primary position for the trigger</param>
    /// <param name="intensity">Intensity of the trigger (0.0 to 1.0)</param>
    /// <returns>Animation trigger context with basic data</returns>
    public static AnimationTriggerContext Create(Vector3 position, float intensity = 1.0f)
    {
        return new AnimationTriggerContext
        {
            primaryPosition = position,
            secondaryPosition = Vector3.zero,
            intensity = Mathf.Clamp01(intensity),
            duration = 1.0f,
            markerMode = MarkerMode.Light,
            additionalData = string.Empty,
            dataReference = null
        };
    }
    
    /// <summary>
    /// Creates a context for marker-related triggers
    /// </summary>
    /// <param name="position">Position of the marker action</param>
    /// <param name="mode">Marker mode associated with the action</param>
    /// <param name="intensity">Intensity of the trigger</param>
    /// <param name="duration">Suggested duration for animations</param>
    /// <returns>Animation trigger context for marker actions</returns>
    public static AnimationTriggerContext CreateMarkerContext(Vector3 position, MarkerMode mode, float intensity = 1.0f, float duration = 1.0f)
    {
        return new AnimationTriggerContext
        {
            primaryPosition = position,
            secondaryPosition = Vector3.zero,
            intensity = Mathf.Clamp01(intensity),
            duration = duration,
            markerMode = mode,
            additionalData = $"{mode} marker action",
            dataReference = null
        };
    }
    
    /// <summary>
    /// Creates a context for mode switch triggers
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="fromMode">Previous marker mode</param>
    /// <param name="toMode">New marker mode</param>
    /// <param name="intensity">Intensity of the transition</param>
    /// <returns>Animation trigger context for mode switches</returns>
    public static AnimationTriggerContext CreateModeSwitchContext(Vector3 playerPosition, MarkerMode fromMode, MarkerMode toMode, float intensity = 1.0f)
    {
        return new AnimationTriggerContext
        {
            primaryPosition = playerPosition,
            secondaryPosition = Vector3.zero,
            intensity = Mathf.Clamp01(intensity),
            duration = 0.5f, // Quick mode switch animation
            markerMode = toMode,
            additionalData = $"Mode switch: {fromMode} -> {toMode}",
            dataReference = new { fromMode, toMode }
        };
    }
}
