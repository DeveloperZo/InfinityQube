using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static Enumerations;

/// <summary>
/// Message polish event data for external system integration
/// </summary>
[System.Serializable]
public class MessagePolishEventData
{
    public string messageId;
    public string messageText;
    public MessageCategory category;
    public Vector3 worldPosition;
    public bool wasSkipped;
    public float animationDuration;
    
    public MessagePolishEventData(string id, string text, MessageCategory cat, Vector3 pos = default, bool skipped = false, float duration = 0.3f)
    {
        messageId = id;
        messageText = text;
        category = cat;
        worldPosition = pos;
        wasSkipped = skipped;
        animationDuration = duration;
    }
}

/// <summary>
/// UnityEvent types for message polish system integration
/// </summary>
[System.Serializable] public class MessageShowEvent : UnityEvent<MessagePolishEventData> { }
[System.Serializable] public class MessageHideEvent : UnityEvent<MessagePolishEventData> { }
[System.Serializable] public class MessageSkipEvent : UnityEvent<MessagePolishEventData> { }
[System.Serializable] public class MessageAudioEvent : UnityEvent<GameAudioEvent, MessagePolishEventData> { }
[System.Serializable] public class MessageAnimationEvent : UnityEvent<string, MessagePolishEventData> { }
[System.Serializable] public class MessageVoiceEvent : UnityEvent<string, string, int> { } // text, messageId, priority

/// <summary>
/// Comprehensive event system for message polish integration.
/// Provides UnityEvents for external system binding and internal event management.
/// Designed to work with existing AudioManager patterns while remaining extensible.
/// </summary>
public class MessagePolishEvents : MonoBehaviour
{
    #region UnityEvent Declarations
    [Header("Core Message Events")]
    [SerializeField, Tooltip("Triggered when a message begins displaying")]
    public MessageShowEvent OnMessageShow = new MessageShowEvent();
    
    [SerializeField, Tooltip("Triggered when a message finishes displaying")]
    public MessageHideEvent OnMessageHide = new MessageHideEvent();
    
    [SerializeField, Tooltip("Triggered when a message is skipped by user")]
    public MessageSkipEvent OnMessageSkip = new MessageSkipEvent();
    
    [Header("Animation Events")]
    [SerializeField, Tooltip("Triggered for animation system integration")]
    public MessageAnimationEvent OnMessageAnimation = new MessageAnimationEvent();
    
    [Header("Audio Events")]
    [SerializeField, Tooltip("Triggered for audio system integration")]
    public MessageAudioEvent OnMessageAudio = new MessageAudioEvent();
    
    [Header("Voice Events (Placeholder)")]
    [SerializeField, Tooltip("Triggered for voice system integration")]
    public MessageVoiceEvent OnMessageVoice = new MessageVoiceEvent();
    #endregion

    #region Inspector Configuration
    [Header("Polish Settings")]
    [SerializeField, Tooltip("Enable animation event triggering")]
    public bool enableAnimationEvents = true;
    
    [SerializeField, Tooltip("Enable audio event triggering")]
    public bool enableAudioEvents = true;
    
    [SerializeField, Tooltip("Enable voice event triggering (placeholder)")]
    public bool enableVoiceEvents = false;
    
    [Header("Animation Timing")]
    [SerializeField, Tooltip("Default animation duration for message transitions")]
    public float defaultAnimationDuration = 0.3f;
    
    [SerializeField, Tooltip("Animation duration for essential messages")]
    public float essentialMessageDuration = 0.5f;
    
    [SerializeField, Tooltip("Animation duration for contextual messages")]
    public float contextualMessageDuration = 0.2f;
    
    [Header("Audio Integration")]
    [SerializeField, Tooltip("Reference to AudioManager for event integration")]
    public AudioManager audioManager;
    
    [Header("Debug")]
    public bool EnableDebugLogs = true;
    #endregion

    #region Runtime State
    private Dictionary<string, MessagePolishEventData> activeMessages = new Dictionary<string, MessagePolishEventData>();
    private int eventSequenceId = 0;
    
    // Statistics
    private int totalEventsTriggered = 0;
    private int animationEventsTriggered = 0;
    private int audioEventsTriggered = 0;
    private int voiceEventsTriggered = 0;
    #endregion

    #region Properties
    public static MessagePolishEvents Instance { get; private set; }
    public int ActiveMessageCount => activeMessages.Count;
    public bool HasActiveMessages => activeMessages.Count > 0;
    public int TotalEventsTriggered => totalEventsTriggered;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        CacheAudioManager();
    }

    private void Start()
    {
        ValidateConfiguration();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DebugLog("Multiple MessagePolishEvents found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void CacheAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
            }
        }
    }

    private void ValidateConfiguration()
    {
        if (audioManager == null && enableAudioEvents)
        {
            DebugLog("AudioManager not found - audio events will not work properly");
        }

        // Ensure reasonable animation durations
        defaultAnimationDuration = Mathf.Max(0.1f, defaultAnimationDuration);
        essentialMessageDuration = Mathf.Max(0.1f, essentialMessageDuration);
        contextualMessageDuration = Mathf.Max(0.1f, contextualMessageDuration);
    }
    #endregion

    #region Event Triggering Methods
    /// <summary>
    /// Trigger message show events with polish integration
    /// </summary>
    /// <param name="messageId">Unique message identifier</param>
    /// <param name="messageText">Message content</param>
    /// <param name="category">Message category for context</param>
    /// <param name="worldPosition">Optional world position for spatial effects</param>
    public void TriggerMessageShow(string messageId, string messageText, MessageCategory category, Vector3 worldPosition = default)
    {
        float animDuration = GetAnimationDurationForCategory(category);
        var eventData = new MessagePolishEventData(messageId, messageText, category, worldPosition, false, animDuration);
        
        // Track active message
        activeMessages[messageId] = eventData;
        
        // Trigger UnityEvents
        OnMessageShow?.Invoke(eventData);
        totalEventsTriggered++;
        
        // Trigger animation events
        if (enableAnimationEvents)
        {
            OnMessageAnimation?.Invoke("show", eventData);
            animationEventsTriggered++;
        }
        
        // Trigger audio events
        if (enableAudioEvents)
        {
            TriggerAudioEvent(GameAudioEvent.MessageShow, eventData);
        }
        
        // Trigger voice events (placeholder)
        if (enableVoiceEvents)
        {
            OnMessageVoice?.Invoke(messageText, messageId, GetVoicePriorityForCategory(category));
            voiceEventsTriggered++;
        }
        
        DebugLog($"Message show events triggered: {messageId} ({category})");
    }

    /// <summary>
    /// Trigger message hide events with polish integration
    /// </summary>
    /// <param name="messageId">Unique message identifier</param>
    /// <param name="wasSkipped">Whether message was skipped by user</param>
    public void TriggerMessageHide(string messageId, bool wasSkipped = false)
    {
        if (!activeMessages.TryGetValue(messageId, out MessagePolishEventData eventData))
        {
            // Create minimal event data if message not tracked
            eventData = new MessagePolishEventData(messageId, "", MessageCategory.Contextual, default, wasSkipped);
        }
        else
        {
            eventData.wasSkipped = wasSkipped;
        }
        
        // Trigger UnityEvents
        OnMessageHide?.Invoke(eventData);
        totalEventsTriggered++;
        
        // Trigger animation events
        if (enableAnimationEvents)
        {
            OnMessageAnimation?.Invoke("hide", eventData);
            animationEventsTriggered++;
        }
        
        // Trigger audio events
        if (enableAudioEvents)
        {
GameAudioEvent audioEvent = wasSkipped ? GameAudioEvent.MessageSkip : GameAudioEvent.MessageHide;
            TriggerAudioEvent(audioEvent, eventData);
        }
        
        // Handle skip-specific events
        if (wasSkipped)
        {
            OnMessageSkip?.Invoke(eventData);
            DebugLog($"Message skip events triggered: {messageId}");
        }
        
        // Remove from active tracking
        activeMessages.Remove(messageId);
        
        DebugLog($"Message hide events triggered: {messageId} (skipped: {wasSkipped})");
    }

    /// <summary>
    /// Trigger animation-specific events for external animation systems
    /// </summary>
    /// <param name="animationType">Type of animation (show/hide/transition)</param>
    /// <param name="messageId">Message identifier for context</param>
    /// <param name="duration">Animation duration override</param>
    public void TriggerAnimationEvent(string animationType, string messageId, float duration = -1f)
    {
        if (!enableAnimationEvents) return;
        
        if (activeMessages.TryGetValue(messageId, out MessagePolishEventData eventData))
        {
            if (duration > 0f)
                eventData.animationDuration = duration;
            
            OnMessageAnimation?.Invoke(animationType, eventData);
            animationEventsTriggered++;
            
            DebugLog($"Animation event triggered: {animationType} for {messageId} (duration: {eventData.animationDuration:F2}s)");
        }
        else
        {
            DebugLog($"Cannot trigger animation event for unknown message: {messageId}");
        }
    }

    /// <summary>
    /// Trigger audio events through the game's audio system
    /// </summary>
    /// <param name="audioEvent">Type of audio event to trigger</param>
    /// <param name="eventData">Message event data for context</param>
    private void TriggerAudioEvent(GameAudioEvent audioEvent, MessagePolishEventData eventData)
    {
        if (!enableAudioEvents) return;
        
        // Trigger UnityEvent for external binding
        OnMessageAudio?.Invoke(audioEvent, eventData);
        audioEventsTriggered++;
        
        // Integrate with AudioManager if available
        if (audioManager != null)
        {
            audioManager.TriggerAudioEvent(audioEvent, eventData.worldPosition, GetAudioIntensityForCategory(eventData.category));
            DebugLog($"Audio event triggered via AudioManager: {audioEvent} for {eventData.messageId}");
        }
        else
        {
            DebugLog($"Audio event triggered (no AudioManager): {audioEvent} for {eventData.messageId}");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Get appropriate animation duration based on message category
    /// </summary>
    /// <param name="category">Message category</param>
    /// <returns>Animation duration in seconds</returns>
    private float GetAnimationDurationForCategory(MessageCategory category)
    {
        switch (category)
        {
            case MessageCategory.Essential:
                return essentialMessageDuration;
            case MessageCategory.Important:
                return defaultAnimationDuration;
            case MessageCategory.Contextual:
                return contextualMessageDuration;
            case MessageCategory.Debug:
                return contextualMessageDuration * 0.5f;
            default:
                return defaultAnimationDuration;
        }
    }

    /// <summary>
    /// Get voice priority based on message category (placeholder)
    /// </summary>
    /// <param name="category">Message category</param>
    /// <returns>Voice priority level</returns>
    private int GetVoicePriorityForCategory(MessageCategory category)
    {
        switch (category)
        {
            case MessageCategory.Essential:
                return 3; // Highest priority
            case MessageCategory.Important:
                return 2; // High priority
            case MessageCategory.Contextual:
                return 1; // Normal priority
            case MessageCategory.Debug:
                return 0; // Low priority
            default:
                return 1;
        }
    }

    /// <summary>
    /// Get audio intensity based on message category
    /// </summary>
    /// <param name="category">Message category</param>
    /// <returns>Audio intensity multiplier</returns>
    private float GetAudioIntensityForCategory(MessageCategory category)
    {
        switch (category)
        {
            case MessageCategory.Essential:
                return 1.2f; // Slightly louder for essential messages
            case MessageCategory.Important:
                return 1.0f; // Normal volume
            case MessageCategory.Contextual:
                return 0.8f; // Quieter for contextual hints
            case MessageCategory.Debug:
                return 0.6f; // Quiet for debug messages
            default:
                return 1.0f;
        }
    }
    #endregion

    #region Public Configuration Methods
    /// <summary>
    /// Set which polish features are enabled
    /// </summary>
    /// <param name="animations">Enable animation events</param>
    /// <param name="audio">Enable audio events</param>
    /// <param name="voice">Enable voice events (placeholder)</param>
    public void SetPolishEnabled(bool animations, bool audio, bool voice)
    {
        enableAnimationEvents = animations;
        enableAudioEvents = audio;
        enableVoiceEvents = voice;
        
        DebugLog($"Polish features updated - Animations: {animations}, Audio: {audio}, Voice: {voice}");
    }

    /// <summary>
    /// Get current polish enablement state
    /// </summary>
    /// <returns>Tuple indicating enabled features</returns>
    public (bool animations, bool audio, bool voice) GetPolishState()
    {
        return (enableAnimationEvents, enableAudioEvents, enableVoiceEvents);
    }

    /// <summary>
    /// Set animation durations for different message categories
    /// </summary>
    /// <param name="defaultDuration">Default animation duration</param>
    /// <param name="essentialDuration">Duration for essential messages</param>
    /// <param name="contextualDuration">Duration for contextual messages</param>
    public void SetAnimationDurations(float defaultDuration, float essentialDuration, float contextualDuration)
    {
        defaultAnimationDuration = Mathf.Max(0.1f, defaultDuration);
        essentialMessageDuration = Mathf.Max(0.1f, essentialDuration);
        contextualMessageDuration = Mathf.Max(0.1f, contextualDuration);
        
        DebugLog($"Animation durations updated - Default: {defaultAnimationDuration:F2}s, Essential: {essentialMessageDuration:F2}s, Contextual: {contextualMessageDuration:F2}s");
    }
    #endregion

    #region Integration Points for TutorialMessageManager
    /// <summary>
    /// Integration point for TutorialMessageManager to trigger show events
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="messageText">Message content</param>
    /// <param name="category">Message category</param>
    public void OnTutorialMessageShow(string messageId, string messageText, MessageCategory category)
    {
        TriggerMessageShow(messageId, messageText, category);
    }

    /// <summary>
    /// Integration point for TutorialMessageManager to trigger hide events
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="wasSkipped">Whether message was skipped</param>
    public void OnTutorialMessageHide(string messageId, bool wasSkipped = false)
    {
        TriggerMessageHide(messageId, wasSkipped);
    }

    /// <summary>
    /// Get recommended animation duration for TutorialMessageManager integration
    /// </summary>
    /// <param name="category">Message category</param>
    /// <returns>Recommended duration in seconds</returns>
    public float GetRecommendedAnimationDuration(MessageCategory category)
    {
        return GetAnimationDurationForCategory(category);
    }
    #endregion

    #region Statistics and Debug
    /// <summary>
    /// Get comprehensive event statistics
    /// </summary>
    /// <returns>Dictionary of event statistics</returns>
    public Dictionary<string, object> GetEventStatistics()
    {
        return new Dictionary<string, object>
        {
            ["Total Events Triggered"] = totalEventsTriggered,
            ["Animation Events"] = animationEventsTriggered,
            ["Audio Events"] = audioEventsTriggered,
            ["Voice Events"] = voiceEventsTriggered,
            ["Active Messages"] = ActiveMessageCount,
            ["Animation Events Enabled"] = enableAnimationEvents,
            ["Audio Events Enabled"] = enableAudioEvents,
            ["Voice Events Enabled"] = enableVoiceEvents,
            ["Default Animation Duration"] = defaultAnimationDuration,
            ["Essential Animation Duration"] = essentialMessageDuration,
            ["Contextual Animation Duration"] = contextualMessageDuration,
            ["AudioManager Available"] = audioManager != null
        };
    }

    /// <summary>
    /// Reset all event statistics
    /// </summary>
    public void ResetStatistics()
    {
        totalEventsTriggered = 0;
        animationEventsTriggered = 0;
        audioEventsTriggered = 0;
        voiceEventsTriggered = 0;
        eventSequenceId = 0;
        
        DebugLog("Event statistics reset");
    }

    private void DebugLog(string message)
    {
        if (EnableDebugLogs)
        {
            Debug.Log($"[MessagePolishEvents] {message}");
        }
    }
    #endregion

    #region Context Menu Testing
    [ContextMenu("Test Show Event")]
    public void TestShowEvent()
    {
        TriggerMessageShow("test_message", "This is a test message for polish integration.", MessageCategory.Important);
    }

    [ContextMenu("Test Hide Event")]
    public void TestHideEvent()
    {
        TriggerMessageHide("test_message", false);
    }

    [ContextMenu("Test Skip Event")]
    public void TestSkipEvent()
    {
        TriggerMessageHide("test_message", true);
    }

    [ContextMenu("Print Statistics")]
    public void PrintStatistics()
    {
        var stats = GetEventStatistics();
        var statsText = string.Join("\n", stats.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        Debug.Log($"[MessagePolishEvents] Statistics:\n{statsText}");
    }
    #endregion
}
