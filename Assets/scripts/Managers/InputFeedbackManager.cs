using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Manager component for the input feedback hook system.
/// Manages registration and execution of feedback hooks that respond to input events.
/// Designed for performance with minimal overhead when no hooks are registered.
/// </summary>
public class InputFeedbackManager : MonoBehaviour, IManagerDebugInterface
{
    [Header("Input Feedback Settings")]
    [SerializeField] private bool enableFeedbackSystem = true;
    [SerializeField] private float globalIntensityMultiplier = 1.0f;
    [SerializeField] private int maxHooksPerFrame = 10;

    // Hook storage - optimized for minimal allocation when empty
    private readonly List<IInputFeedbackHook> feedbackHooks = new List<IInputFeedbackHook>();
    private readonly Dictionary<string, IInputFeedbackHook> namedHooks = new Dictionary<string, IInputFeedbackHook>();
    
    // Performance tracking
    private int frameHookExecutions = 0;
    private int totalHookExecutions = 0;
    private float lastFrameTime = 0f;

    #region Unity Lifecycle

    private void Awake()
    {
        // Initialize hook collections
        feedbackHooks.Clear();
        namedHooks.Clear();
        
        if (EnableDebugLogs)
        {
            Debug.Log("[InputFeedbackManager] Initialized with feedback system enabled: " + enableFeedbackSystem);
        }
    }

    private void LateUpdate()
    {
        // Reset per-frame counters
        frameHookExecutions = 0;
        lastFrameTime = Time.time;
    }

    #endregion

    #region Hook Registration Management

    /// <summary>
    /// Register a feedback hook for input events
    /// </summary>
    /// <param name="hook">The hook implementation to register</param>
    /// <returns>True if successfully registered, false if hook was null or already registered</returns>
    public bool RegisterHook(IInputFeedbackHook hook)
    {
        if (hook == null)
        {
            if (EnableDebugLogs)
                Debug.LogWarning("[InputFeedbackManager] Attempted to register null hook");
            return false;
        }

        if (feedbackHooks.Contains(hook))
        {
            if (EnableDebugLogs)
                Debug.LogWarning($"[InputFeedbackManager] Hook {hook.GetHookName()} is already registered");
            return false;
        }

        feedbackHooks.Add(hook);
        namedHooks[hook.GetHookName()] = hook;

        // Sort by priority (higher priority first)
        feedbackHooks.Sort((a, b) => b.GetPriority().CompareTo(a.GetPriority()));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Registered hook: {hook.GetHookName()} (Priority: {hook.GetPriority()})");
        }

        return true;
    }

    /// <summary>
    /// Unregister a feedback hook
    /// </summary>
    /// <param name="hook">The hook to unregister</param>
    /// <returns>True if successfully unregistered, false if hook was not found</returns>
    public bool UnregisterHook(IInputFeedbackHook hook)
    {
        if (hook == null)
            return false;

        bool removed = feedbackHooks.Remove(hook);
        if (removed)
        {
            namedHooks.Remove(hook.GetHookName());
            
            if (EnableDebugLogs)
            {
                Debug.Log($"[InputFeedbackManager] Unregistered hook: {hook.GetHookName()}");
            }
        }

        return removed;
    }

    /// <summary>
    /// Unregister a hook by name
    /// </summary>
    /// <param name="hookName">Name of the hook to unregister</param>
    /// <returns>True if successfully unregistered</returns>
    public bool UnregisterHook(string hookName)
    {
        if (namedHooks.TryGetValue(hookName, out IInputFeedbackHook hook))
        {
            return UnregisterHook(hook);
        }
        return false;
    }

    /// <summary>
    /// Clear all registered hooks
    /// </summary>
    public void ClearAllHooks()
    {
        int hookCount = feedbackHooks.Count;
        feedbackHooks.Clear();
        namedHooks.Clear();

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Cleared {hookCount} registered hooks");
        }
    }

    /// <summary>
    /// Get a registered hook by name
    /// </summary>
    /// <param name="hookName">Name of the hook to retrieve</param>
    /// <returns>The hook if found, null otherwise</returns>
    public IInputFeedbackHook GetHook(string hookName)
    {
        namedHooks.TryGetValue(hookName, out IInputFeedbackHook hook);
        return hook;
    }

    #endregion

    #region Input Event Triggers

    /// <summary>
    /// Trigger mode switch feedback for all registered hooks
    /// </summary>
    public void TriggerModeSwitch(MarkerMode previousMode, MarkerMode newMode, Vector2Int playerPosition)
    {
        if (!enableFeedbackSystem || feedbackHooks.Count == 0)
            return;

        ExecuteHooks(hook => hook.OnModeSwitch(previousMode, newMode, playerPosition));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Triggered mode switch feedback: {previousMode} -> {newMode} at {playerPosition}");
        }
    }

    /// <summary>
    /// Trigger marker placement feedback for all registered hooks
    /// </summary>
    public void TriggerMarkerPlace(MarkerMode markerMode, Vector2Int position, bool wasReplacement)
    {
        if (!enableFeedbackSystem || feedbackHooks.Count == 0)
            return;

        ExecuteHooks(hook => hook.OnMarkerPlace(markerMode, position, wasReplacement));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Triggered marker place feedback: {markerMode} at {position} (replacement: {wasReplacement})");
        }
    }

    /// <summary>
    /// Trigger marker trigger feedback for all registered hooks
    /// </summary>
    public void TriggerMarkerTrigger(MarkerMode markerMode, Vector2Int position, int targetCount)
    {
        if (!enableFeedbackSystem || feedbackHooks.Count == 0)
            return;

        ExecuteHooks(hook => hook.OnMarkerTrigger(markerMode, position, targetCount));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Triggered marker trigger feedback: {markerMode} at {position} affecting {targetCount} targets");
        }
    }

    /// <summary>
    /// Trigger cube marker trigger feedback for all registered hooks
    /// </summary>
    public void TriggerCubeMarkerTrigger(string cubeMarkerType, Vector2Int position, string effect)
    {
        if (!enableFeedbackSystem || feedbackHooks.Count == 0)
            return;

        ExecuteHooks(hook => hook.OnCubeMarkerTrigger(cubeMarkerType, position, effect));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Triggered cube marker feedback: {cubeMarkerType} at {position} with effect: {effect}");
        }
    }

    /// <summary>
    /// Trigger action failure feedback for all registered hooks
    /// </summary>
    public void TriggerActionFailed(string actionType, string failureReason, Vector2Int playerPosition, float intensity = 0.5f)
    {
        if (!enableFeedbackSystem || feedbackHooks.Count == 0)
            return;

        // Apply global intensity multiplier
        float adjustedIntensity = intensity * globalIntensityMultiplier;

        ExecuteHooks(hook => hook.OnActionFailed(actionType, failureReason, playerPosition, adjustedIntensity));

        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Triggered action failed feedback: {actionType} - {failureReason} at {playerPosition} (intensity: {adjustedIntensity:F2})");
        }
    }

    #endregion

    #region Hook Execution

    /// <summary>
    /// Execute an action on all ready and enabled hooks with performance tracking
    /// </summary>
    /// <param name="action">The action to execute on each hook</param>
    private void ExecuteHooks(System.Action<IInputFeedbackHook> action)
    {
        if (frameHookExecutions >= maxHooksPerFrame)
        {
            if (EnableDebugLogs)
                Debug.LogWarning($"[InputFeedbackManager] Hook execution limit reached this frame ({maxHooksPerFrame})");
            return;
        }

        int executedCount = 0;
        
        for (int i = 0; i < feedbackHooks.Count && frameHookExecutions < maxHooksPerFrame; i++)
        {
            var hook = feedbackHooks[i];
            
            if (hook != null && hook.IsEnabled() && hook.IsReady())
            {
                try
                {
                    action(hook);
                    executedCount++;
                    frameHookExecutions++;
                    totalHookExecutions++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[InputFeedbackManager] Error executing hook {hook.GetHookName()}: {e.Message}");
                }
            }
        }

        if (EnableDebugLogs && executedCount > 0)
        {
            Debug.Log($"[InputFeedbackManager] Executed action on {executedCount} hooks");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get the number of currently registered hooks
    /// </summary>
    public int GetRegisteredHookCount() => feedbackHooks.Count;

    /// <summary>
    /// Get the number of enabled and ready hooks
    /// </summary>
    public int GetActiveHookCount() => feedbackHooks.Count(h => h != null && h.IsEnabled() && h.IsReady());

    /// <summary>
    /// Enable or disable the entire feedback system
    /// </summary>
    public void SetFeedbackSystemEnabled(bool enabled)
    {
        enableFeedbackSystem = enabled;
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Feedback system enabled: {enabled}");
        }
    }

    /// <summary>
    /// Set the global intensity multiplier for all feedback
    /// </summary>
    public void SetGlobalIntensity(float intensity)
    {
        globalIntensityMultiplier = Mathf.Clamp(intensity, 0f, 2f);
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[InputFeedbackManager] Global intensity set to: {globalIntensityMultiplier:F2}");
        }
    }

    /// <summary>
    /// Get all registered hook names
    /// </summary>
    public string[] GetRegisteredHookNames() => namedHooks.Keys.ToArray();

    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs { get; set; } = false;

    public string GetDebugStatus()
    {
        return $"InputFeedback: Enabled:{enableFeedbackSystem} Hooks:{feedbackHooks.Count}({GetActiveHookCount()}) Executions:{totalHookExecutions} Intensity:{globalIntensityMultiplier:F1}x";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Feedback System Enabled"] = enableFeedbackSystem,
            ["Registered Hooks"] = feedbackHooks.Count,
            ["Active Hooks"] = GetActiveHookCount(),
            ["Global Intensity Multiplier"] = globalIntensityMultiplier,
            ["Total Hook Executions"] = totalHookExecutions,
            ["Frame Hook Executions"] = frameHookExecutions,
            ["Max Hooks Per Frame"] = maxHooksPerFrame,
            ["Last Frame Time"] = lastFrameTime,
            ["Registered Hook Names"] = string.Join(", ", GetRegisteredHookNames())
        };
    }

    public void ResetToDefaults()
    {
        // Clear all hooks
        ClearAllHooks();
        
        // Reset settings
        enableFeedbackSystem = true;
        globalIntensityMultiplier = 1.0f;
        maxHooksPerFrame = 10;
        
        // Reset counters
        frameHookExecutions = 0;
        totalHookExecutions = 0;
        lastFrameTime = 0f;
        
        if (EnableDebugLogs)
            Debug.Log("[InputFeedbackManager] Reset to defaults completed");
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading from ScriptableObject or JSON
        if (EnableDebugLogs)
            Debug.Log($"[InputFeedbackManager] Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving to ScriptableObject or JSON
        if (EnableDebugLogs)
            Debug.Log($"[InputFeedbackManager] Saving configuration: {configName} (not yet implemented)");
    }

    #endregion
}
