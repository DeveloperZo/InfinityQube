using UnityEngine;
using static Enumerations;

/// <summary>
/// Example implementation of IInputFeedbackHook for testing and demonstration.
/// This hook provides basic debug logging feedback for all input events.
/// Can be used as a template for creating actual polish feedback implementations.
/// </summary>
public class ExampleDebugFeedbackHook : MonoBehaviour, IInputFeedbackHook
{
    [Header("Example Hook Settings")]
    [SerializeField] private bool enabled = true;
    [SerializeField] private int priority = 0;
    [SerializeField] private float intensity = 1.0f;
    [SerializeField] private bool verboseLogging = true;

    [Header("Runtime State")]
    [SerializeField] private bool isReady = true;
    [SerializeField] private int eventsProcessed = 0;

    #region IInputFeedbackHook Implementation

    public void OnModeSwitch(MarkerMode previousMode, MarkerMode newMode, Vector2Int playerPosition)
    {
        if (!ShouldProcessEvent()) return;

        eventsProcessed++;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Mode switch: {previousMode} -> {newMode} at {playerPosition} (Intensity: {intensity:F2})");
        }
        
        // Example: Could trigger screen flash, controller vibration, or sound effect here
        // For now, just demonstrate hook execution with debug output
    }

    public void OnMarkerPlace(MarkerMode markerMode, Vector2Int position, bool wasReplacement)
    {
        if (!ShouldProcessEvent()) return;

        eventsProcessed++;
        
        string actionType = wasReplacement ? "replaced" : "placed";
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Marker {actionType}: {markerMode} at {position} (Intensity: {intensity:F2})");
        }
        
        // Example: Different feedback intensity based on marker type and action
        float feedbackIntensity = intensity * (wasReplacement ? 0.5f : 1.0f);
        
        // Example polish implementations could:
        // - Trigger haptic feedback with different patterns per marker type
        // - Show screen effects (particle systems, UI animations)
        // - Play spatial audio with position-based 3D sound
    }

    public void OnMarkerTrigger(MarkerMode markerMode, Vector2Int position, int targetCount)
    {
        if (!ShouldProcessEvent()) return;

        eventsProcessed++;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Marker triggered: {markerMode} at {position} affecting {targetCount} targets (Intensity: {intensity:F2})");
        }
        
        // Example: Scale feedback intensity based on targets affected
        float scaledIntensity = intensity * Mathf.Clamp(targetCount / 10f, 0.1f, 2.0f);
        
        // Example polish implementations could:
        // - Stronger haptic feedback for larger area effects
        // - Camera shake proportional to impact
        // - Dynamic audio based on number of targets
    }

    public void OnCubeMarkerTrigger(string cubeMarkerType, Vector2Int position, string effect)
    {
        if (!ShouldProcessEvent()) return;

        eventsProcessed++;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Cube marker triggered: {cubeMarkerType} at {position} - {effect} (Intensity: {intensity:F2})");
        }
        
        // Example: Different feedback for different cube marker types
        // - "Cube" markers could have standard feedback
        // - "PowerUp" markers could have enhanced/special feedback
    }

    public void OnActionFailed(string actionType, string failureReason, Vector2Int playerPosition, float intensity)
    {
        if (!ShouldProcessEvent()) return;

        eventsProcessed++;
        
        // Apply our intensity multiplier to the provided intensity
        float finalIntensity = intensity * this.intensity;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Action failed: {actionType} - {failureReason} at {playerPosition} (Intensity: {finalIntensity:F2})");
        }
        
        // Example: Error feedback could include:
        // - Subtle haptic feedback to indicate failure
        // - Red screen flash or UI indication
        // - Error sound with spatial positioning
        // - Different feedback intensity based on failure severity
    }

    public bool IsReady()
    {
        return isReady && gameObject.activeInHierarchy;
    }

    public int GetPriority()
    {
        return priority;
    }

    public string GetHookName()
    {
        return "Example Debug Hook";
    }

    public void SetIntensity(float intensity)
    {
        this.intensity = Mathf.Clamp(intensity, 0f, 2f);
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Intensity set to: {this.intensity:F2}");
        }
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Enabled state set to: {enabled}");
        }
    }

    public bool IsEnabled()
    {
        return enabled;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Initialize hook state
        isReady = true;
        eventsProcessed = 0;
        
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Hook initialized with priority {priority} and intensity {intensity:F2}");
        }
    }

    private void Start()
    {
        // Auto-register this hook with the InputFeedbackManager if available
        InputFeedbackManager feedbackManager = FindObjectOfType<InputFeedbackManager>();
        if (feedbackManager != null)
        {
            bool registered = feedbackManager.RegisterHook(this);
            if (registered && verboseLogging)
            {
                Debug.Log($"[ExampleDebugFeedbackHook] Successfully registered with InputFeedbackManager");
            }
        }
        else if (verboseLogging)
        {
            Debug.LogWarning($"[ExampleDebugFeedbackHook] No InputFeedbackManager found in scene - hook will not receive events");
        }
    }

    private void OnDestroy()
    {
        // Auto-unregister when destroyed
        InputFeedbackManager feedbackManager = FindObjectOfType<InputFeedbackManager>();
        if (feedbackManager != null)
        {
            feedbackManager.UnregisterHook(this);
            if (verboseLogging)
            {
                Debug.Log($"[ExampleDebugFeedbackHook] Unregistered from InputFeedbackManager");
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determines if this hook should process the current event
    /// </summary>
    /// <returns>True if event should be processed</returns>
    private bool ShouldProcessEvent()
    {
        return enabled && isReady && gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Public method to get statistics about this hook
    /// </summary>
    /// <returns>Number of events processed by this hook</returns>
    public int GetEventsProcessed()
    {
        return eventsProcessed;
    }

    /// <summary>
    /// Reset the event counter (useful for testing)
    /// </summary>
    public void ResetEventCounter()
    {
        eventsProcessed = 0;
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Event counter reset");
        }
    }

    /// <summary>
    /// Simulate a readiness state change (useful for testing)
    /// </summary>
    /// <param name="ready">New readiness state</param>
    public void SetReady(bool ready)
    {
        isReady = ready;
        if (verboseLogging)
        {
            Debug.Log($"[ExampleDebugFeedbackHook] Readiness set to: {ready}");
        }
    }

    #endregion

    #region Inspector Utilities (Editor Only)

    #if UNITY_EDITOR
    /// <summary>
    /// Test method to manually trigger mode switch feedback (Editor only)
    /// </summary>
    [ContextMenu("Test Mode Switch")]
    private void TestModeSwitch()
    {
        OnModeSwitch(MarkerMode.Unit, MarkerMode.Recursion, new Vector2Int(5, 5));
    }

    /// <summary>
    /// Test method to manually trigger marker place feedback (Editor only)
    /// </summary>
    [ContextMenu("Test Marker Place")]
    private void TestMarkerPlace()
    {
        OnMarkerPlace(MarkerMode.Matrix, new Vector2Int(3, 3), false);
    }

    /// <summary>
    /// Test method to manually trigger action failed feedback (Editor only)
    /// </summary>
    [ContextMenu("Test Action Failed")]
    private void TestActionFailed()
    {
        OnActionFailed("place", "No charges available", new Vector2Int(2, 2), 0.8f);
    }
    #endif

    #endregion
}
