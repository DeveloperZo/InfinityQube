using UnityEngine;
using System;
using static Enumerations;

/// <summary>
/// Interface defining input feedback integration points for player actions.
/// Provides hooks for haptic feedback, screen effects, and advanced audio processing
/// when input actions occur. This establishes the foundation for polish features
/// while maintaining clean architecture separation.
/// </summary>
public interface IInputFeedbackHook
{
    #region Mode Switch Hooks
    /// <summary>
    /// Called when the player switches marker modes via input
    /// </summary>
    /// <param name="previousMode">The mode being switched from</param>
    /// <param name="newMode">The mode being switched to</param>
    /// <param name="playerPosition">Current player position for spatial feedback</param>
    void OnModeSwitch(MarkerMode previousMode, MarkerMode newMode, Vector2Int playerPosition);
    #endregion

    #region Marker Placement Hooks
    /// <summary>
    /// Called when a marker is successfully placed by player input
    /// </summary>
    /// <param name="markerMode">Type of marker that was placed</param>
    /// <param name="position">Grid position where marker was placed</param>
    /// <param name="wasReplacement">True if this replaced an existing marker</param>
    void OnMarkerPlace(MarkerMode markerMode, Vector2Int position, bool wasReplacement);
    
    /// <summary>
    /// Called when a marker is successfully triggered by player input
    /// </summary>
    /// <param name="markerMode">Type of marker that was triggered</param>
    /// <param name="position">Grid position of the triggered marker</param>
    /// <param name="targetCount">Number of targets affected by the trigger</param>
    void OnMarkerTrigger(MarkerMode markerMode, Vector2Int position, int targetCount);
    #endregion

    #region Cube Marker Hooks
    /// <summary>
    /// Called when a cube marker is triggered via input
    /// </summary>
    /// <param name="cubeMarkerType">Type of cube marker triggered</param>
    /// <param name="position">Position of the cube marker</param>
    /// <param name="effect">Description of the effect caused</param>
    void OnCubeMarkerTrigger(string cubeMarkerType, Vector2Int position, string effect);
    #endregion

    #region Error Feedback Hooks
    /// <summary>
    /// Called when a player action fails due to constraints or limitations
    /// </summary>
    /// <param name="actionType">Type of action that failed (place, trigger, switch)</param>
    /// <param name="failureReason">Human-readable reason for the failure</param>
    /// <param name="playerPosition">Current player position for spatial feedback</param>
    /// <param name="intensity">Failure severity (0.0-1.0) for proportional feedback</param>
    void OnActionFailed(string actionType, string failureReason, Vector2Int playerPosition, float intensity = 0.5f);
    #endregion

    #region Polish State Queries
    /// <summary>
    /// Check if this feedback hook is ready to provide feedback
    /// </summary>
    /// <returns>True if the hook can process feedback events</returns>
    bool IsReady();
    
    /// <summary>
    /// Get the priority of this feedback hook for execution order
    /// </summary>
    /// <returns>Priority value (higher = executed first)</returns>
    int GetPriority();
    
    /// <summary>
    /// Get a descriptive name for this feedback hook (for debugging)
    /// </summary>
    /// <returns>Human-readable name identifying this hook</returns>
    string GetHookName();
    #endregion

    #region Configuration
    /// <summary>
    /// Set the intensity multiplier for this feedback hook
    /// </summary>
    /// <param name="intensity">Intensity multiplier (0.0-2.0)</param>
    void SetIntensity(float intensity);
    
    /// <summary>
    /// Enable or disable this feedback hook
    /// </summary>
    /// <param name="enabled">Whether the hook should process events</param>
    void SetEnabled(bool enabled);
    
    /// <summary>
    /// Get current enablement state of this hook
    /// </summary>
    /// <returns>True if hook is enabled and processing events</returns>
    bool IsEnabled();
    #endregion
}
