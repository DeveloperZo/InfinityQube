using System;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Interface defining polish integration points for message display system.
/// Provides hooks for animation, audio, and voice integration without implementing actual polish features.
/// This establishes the foundation for future enhancement while maintaining clean architecture.
/// </summary>
public interface IMessagePolishHooks
{
    #region Animation Hooks
    /// <summary>
    /// Called when a message is about to be shown - hook for entrance animations
    /// </summary>
    /// <param name="messageId">Unique identifier for the message</param>
    /// <param name="messageText">The text content of the message</param>
    /// <param name="category">Category/importance level of the message</param>
    void OnMessageShowAnimationHook(string messageId, string messageText, MessageCategory category);
    
    /// <summary>
    /// Called when a message is about to be hidden - hook for exit animations
    /// </summary>
    /// <param name="messageId">Unique identifier for the message</param>
    /// <param name="wasSkipped">Whether the message was skipped by user input</param>
    void OnMessageHideAnimationHook(string messageId, bool wasSkipped);
    
    /// <summary>
    /// Called when message display state changes - hook for animation state management
    /// </summary>
    /// <param name="isVisible">Whether message UI is currently visible</param>
    /// <param name="animationDuration">Expected duration of state transition animation</param>
    void OnMessageVisibilityChanged(bool isVisible, float animationDuration = 0.3f);
    #endregion

    #region Audio Integration Hooks
    /// <summary>
    /// Called when a message is shown - hook for audio event integration
    /// </summary>
    /// <param name="messageId">Unique identifier for the message</param>
    /// <param name="category">Category/importance level affecting audio choice</param>
    /// <param name="position">Optional world position for spatial audio</param>
    void OnMessageAudioEvent(string messageId, MessageCategory category, Vector3 position = default);
    
    /// <summary>
    /// Called when user skips a message - hook for skip audio feedback
    /// </summary>
    /// <param name="messageId">Unique identifier for the skipped message</param>
    void OnMessageSkipAudioEvent(string messageId);
    
    /// <summary>
    /// Called when message timing events occur - hook for timing-related audio
    /// </summary>
    /// <param name="eventType">Type of timing event (show/hide/timeout)</param>
    /// <param name="messageId">Message identifier for context</param>
    void OnMessageTimingAudioEvent(string eventType, string messageId);
    #endregion

    #region Voice System Hooks (Placeholder)
    /// <summary>
    /// Called when a message should be read aloud - placeholder for future voice integration
    /// </summary>
    /// <param name="messageText">Text to be spoken</param>
    /// <param name="messageId">Message identifier for voice caching</param>
    /// <param name="priority">Voice playback priority</param>
    void OnMessageVoiceRequest(string messageText, string messageId, int priority = 0);
    
    /// <summary>
    /// Called to stop any currently playing voice - placeholder for voice control
    /// </summary>
    void OnMessageVoiceStop();
    
    /// <summary>
    /// Called to check if voice system is available - placeholder for voice capability detection
    /// </summary>
    /// <returns>True if voice system is ready, false otherwise</returns>
    bool IsVoiceSystemAvailable();
    #endregion

    #region Polish State Queries
    /// <summary>
    /// Check if animation system is ready for message polish
    /// </summary>
    /// <returns>True if animations can be applied to messages</returns>
    bool IsAnimationSystemReady();
    
    /// <summary>
    /// Check if audio system is ready for message polish
    /// </summary>
    /// <returns>True if audio events can be triggered for messages</returns>
    bool IsAudioSystemReady();
    
    /// <summary>
    /// Get the recommended animation duration for message transitions
    /// </summary>
    /// <param name="category">Message category affecting timing</param>
    /// <returns>Duration in seconds for smooth transitions</returns>
    float GetRecommendedAnimationDuration(MessageCategory category);
    #endregion

    #region Polish Configuration
    /// <summary>
    /// Set whether polish features should be active
    /// </summary>
    /// <param name="enableAnimations">Enable message animations</param>
    /// <param name="enableAudio">Enable message audio feedback</param>
    /// <param name="enableVoice">Enable message voice reading (placeholder)</param>
    void SetPolishEnabled(bool enableAnimations, bool enableAudio, bool enableVoice);
    
    /// <summary>
    /// Get current polish enablement state
    /// </summary>
    /// <returns>Tuple indicating which polish features are enabled</returns>
    (bool animations, bool audio, bool voice) GetPolishState();
    #endregion
}
