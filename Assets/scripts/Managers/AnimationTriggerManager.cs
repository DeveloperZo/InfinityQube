using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Central manager for coordinating animation trigger events throughout the mode system.
/// Provides a unified interface for triggering animation events and managing animation receivers.
/// Integrates with Unity's animation system and supports custom animation controllers.
/// </summary>
public class AnimationTriggerManager : MonoBehaviour, IManagerDebugInterface
{
    [Header("Animation Settings")]
    [SerializeField] private bool enableAnimationTriggers = true;
    [SerializeField] private float defaultAnimationDuration = 1.0f;
    [SerializeField] private AnimationCurve defaultIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Debug Settings")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool logAllTriggers = false;
    [SerializeField] private bool visualizeTriggersInScene = false;
    
    // Registered animation receivers
    private Dictionary<AnimationTriggerPoint, List<IAnimationTriggerReceiver>> triggerReceivers;
    private List<IAnimationTriggerReceiver> allReceivers;
    
    // Unity animation integration
    private Animator unityAnimator;
    private Dictionary<AnimationTriggerPoint, string> animatorTriggerNames;
    
    // Statistics and monitoring
    private Dictionary<AnimationTriggerPoint, int> triggerCounts;
    private float lastTriggerTime;
    private AnimationTriggerPoint lastTriggerType;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeManager();
    }
    
    private void Start()
    {
        SetupUnityAnimatorIntegration();
        ValidateManager();
    }
    
    private void InitializeManager()
    {
        // Initialize collections
        triggerReceivers = new Dictionary<AnimationTriggerPoint, List<IAnimationTriggerReceiver>>();
        allReceivers = new List<IAnimationTriggerReceiver>();
        triggerCounts = new Dictionary<AnimationTriggerPoint, int>();
        animatorTriggerNames = new Dictionary<AnimationTriggerPoint, string>();
        
        // Initialize trigger counts for all trigger points
        foreach (AnimationTriggerPoint triggerPoint in System.Enum.GetValues(typeof(AnimationTriggerPoint)))
        {
            triggerReceivers[triggerPoint] = new List<IAnimationTriggerReceiver>();
            triggerCounts[triggerPoint] = 0;
        }
        
        // Setup default animator trigger names
        SetupDefaultAnimatorTriggerNames();
        
        if (EnableDebugLogs)
        {
            Debug.Log("[AnimationTriggerManager] Manager initialized successfully");
        }
    }
    
    private void SetupDefaultAnimatorTriggerNames()
    {
        // Map animation trigger points to Unity Animator trigger parameter names
        animatorTriggerNames[AnimationTriggerPoint.ModeSwitch] = "TriggerModeSwitch";
        animatorTriggerNames[AnimationTriggerPoint.MarkerPlace] = "TriggerMarkerPlace";
        animatorTriggerNames[AnimationTriggerPoint.MarkerTrigger] = "TriggerMarkerTrigger";
        animatorTriggerNames[AnimationTriggerPoint.UIUpdate] = "TriggerUIUpdate";
        animatorTriggerNames[AnimationTriggerPoint.ActionFailed] = "TriggerActionFailed";
        animatorTriggerNames[AnimationTriggerPoint.ActionSuccess] = "TriggerActionSuccess";
        animatorTriggerNames[AnimationTriggerPoint.CubeMarkerAction] = "TriggerCubeMarkerAction";
        animatorTriggerNames[AnimationTriggerPoint.ResourceRegeneration] = "TriggerResourceRegeneration";
    }
    
    private void SetupUnityAnimatorIntegration()
    {
        // Look for Animator component on this GameObject or parent
        unityAnimator = GetComponentInParent<Animator>();
        
        if (unityAnimator == null)
        {
            // Try to find PlayerActionManager's Animator
            var playerActionManager = FindFirstObjectByType<PlayerActionManager>();
            if (playerActionManager != null)
            {
                unityAnimator = playerActionManager.GetComponentInChildren<Animator>();
            }
        }
        
        if (unityAnimator != null && EnableDebugLogs)
        {
            Debug.Log($"[AnimationTriggerManager] Unity Animator integration enabled: {unityAnimator.name}");
        }
        else if (EnableDebugLogs)
        {
            Debug.Log("[AnimationTriggerManager] No Unity Animator found - trigger events will only call registered receivers");
        }
    }
    
    private void ValidateManager()
    {
        if (!enableAnimationTriggers)
        {
            Debug.LogWarning("[AnimationTriggerManager] Animation triggers are disabled - no trigger events will be processed");
        }
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[AnimationTriggerManager] Validation complete. Triggers enabled: {enableAnimationTriggers}, Receivers: {allReceivers.Count}");
        }
    }
    
    #endregion
    
    #region Public API - Trigger Events
    
    /// <summary>
    /// Triggers an animation event for the specified trigger point with context data
    /// </summary>
    /// <param name="triggerPoint">The animation trigger point to activate</param>
    /// <param name="context">Context data for the animation trigger</param>
    public void TriggerAnimation(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        if (!enableAnimationTriggers)
            return;
            
        // Update statistics
        triggerCounts[triggerPoint]++;
        lastTriggerTime = Time.time;
        lastTriggerType = triggerPoint;
        
        // Log trigger event if enabled
        if (logAllTriggers || EnableDebugLogs)
        {
            Debug.Log($"[AnimationTriggerManager] Triggering {triggerPoint} at position {context.primaryPosition} with intensity {context.intensity:F2}");
        }
        
        // Notify all registered receivers for this trigger point
        if (triggerReceivers.ContainsKey(triggerPoint))
        {
            var receivers = triggerReceivers[triggerPoint].Where(r => r != null && r.IsReceiverActive()).ToList();
            
            foreach (var receiver in receivers)
            {
                try
                {
                    receiver.OnAnimationTrigger(triggerPoint, context);
                    
                    if (EnableDebugLogs)
                    {
                        Debug.Log($"[AnimationTriggerManager] Notified receiver: {receiver.GetReceiverName()}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AnimationTriggerManager] Error in receiver {receiver.GetReceiverName()}: {e.Message}");
                }
            }
        }
        
        // Trigger Unity Animator if available
        TriggerUnityAnimator(triggerPoint, context);
        
        // Visual debugging in scene view
        if (visualizeTriggersInScene)
        {
            VisualizeTriggersInScene(triggerPoint, context);
        }
    }
    
    /// <summary>
    /// Convenience method to trigger animation with minimal context
    /// </summary>
    /// <param name="triggerPoint">The animation trigger point to activate</param>
    /// <param name="position">Primary position for the trigger</param>
    /// <param name="intensity">Intensity of the trigger (0.0 to 1.0)</param>
    public void TriggerAnimation(AnimationTriggerPoint triggerPoint, Vector3 position, float intensity = 1.0f)
    {
        var context = AnimationTriggerContext.Create(position, intensity);
        TriggerAnimation(triggerPoint, context);
    }
    
    /// <summary>
    /// Triggers mode switch animation with proper context
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="fromMode">Previous marker mode</param>
    /// <param name="toMode">New marker mode</param>
    /// <param name="intensity">Intensity of the mode switch</param>
    public void TriggerModeSwitch(Vector3 playerPosition, MarkerMode fromMode, MarkerMode toMode, float intensity = 1.0f)
    {
        var context = AnimationTriggerContext.CreateModeSwitchContext(playerPosition, fromMode, toMode, intensity);
        TriggerAnimation(AnimationTriggerPoint.ModeSwitch, context);
    }
    
    /// <summary>
    /// Triggers marker placement animation with proper context
    /// </summary>
    /// <param name="markerPosition">Position where marker was placed</param>
    /// <param name="markerMode">Type of marker that was placed</param>
    /// <param name="wasReplacement">Whether this replaced an existing marker</param>
    /// <param name="intensity">Intensity of the placement effect</param>
    public void TriggerMarkerPlace(Vector3 markerPosition, MarkerMode markerMode, bool wasReplacement = false, float intensity = 1.0f)
    {
        var context = AnimationTriggerContext.CreateMarkerContext(markerPosition, markerMode, intensity, defaultAnimationDuration);
        context.additionalData = wasReplacement ? "replacement" : "new";
        TriggerAnimation(AnimationTriggerPoint.MarkerPlace, context);
    }
    
    /// <summary>
    /// Triggers marker trigger animation with proper context
    /// </summary>
    /// <param name="markerPosition">Position of the triggered marker</param>
    /// <param name="markerMode">Type of marker that was triggered</param>
    /// <param name="targetCount">Number of targets affected</param>
    /// <param name="intensity">Intensity of the trigger effect</param>
    public void TriggerMarkerTrigger(Vector3 markerPosition, MarkerMode markerMode, int targetCount = 1, float intensity = 1.0f)
    {
        var context = AnimationTriggerContext.CreateMarkerContext(markerPosition, markerMode, intensity, defaultAnimationDuration);
        context.additionalData = $"targets: {targetCount}";
        context.dataReference = targetCount;
        TriggerAnimation(AnimationTriggerPoint.MarkerTrigger, context);
    }
    
    #endregion
    
    #region Public API - Receiver Management
    
    /// <summary>
    /// Registers an animation receiver to listen for specific trigger points
    /// </summary>
    /// <param name="receiver">The receiver to register</param>
    /// <param name="triggerPoints">Specific trigger points to listen for</param>
    public void RegisterReceiver(IAnimationTriggerReceiver receiver, params AnimationTriggerPoint[] triggerPoints)
    {
        if (receiver == null)
        {
            Debug.LogWarning("[AnimationTriggerManager] Cannot register null receiver");
            return;
        }
        
        // Add to all receivers list if not already present
        if (!allReceivers.Contains(receiver))
        {
            allReceivers.Add(receiver);
        }
        
        // Register for specific trigger points
        foreach (var triggerPoint in triggerPoints)
        {
            if (!triggerReceivers[triggerPoint].Contains(receiver))
            {
                triggerReceivers[triggerPoint].Add(receiver);
                
                if (EnableDebugLogs)
                {
                    Debug.Log($"[AnimationTriggerManager] Registered {receiver.GetReceiverName()} for {triggerPoint}");
                }
            }
        }
    }
    
    /// <summary>
    /// Registers an animation receiver to listen for all trigger points
    /// </summary>
    /// <param name="receiver">The receiver to register</param>
    public void RegisterReceiverForAllTriggers(IAnimationTriggerReceiver receiver)
    {
        var allTriggerPoints = System.Enum.GetValues(typeof(AnimationTriggerPoint)).Cast<AnimationTriggerPoint>().ToArray();
        RegisterReceiver(receiver, allTriggerPoints);
    }
    
    /// <summary>
    /// Unregisters an animation receiver from all trigger points
    /// </summary>
    /// <param name="receiver">The receiver to unregister</param>
    public void UnregisterReceiver(IAnimationTriggerReceiver receiver)
    {
        if (receiver == null)
            return;
            
        // Remove from all trigger point lists
        foreach (var triggerPoint in triggerReceivers.Keys.ToList())
        {
            triggerReceivers[triggerPoint].Remove(receiver);
        }
        
        // Remove from all receivers list
        allReceivers.Remove(receiver);
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[AnimationTriggerManager] Unregistered {receiver.GetReceiverName()} from all triggers");
        }
    }
    
    /// <summary>
    /// Gets the count of active receivers for a specific trigger point
    /// </summary>
    /// <param name="triggerPoint">The trigger point to query</param>
    /// <returns>Number of active receivers for the trigger point</returns>
    public int GetReceiverCount(AnimationTriggerPoint triggerPoint)
    {
        if (!triggerReceivers.ContainsKey(triggerPoint))
            return 0;
            
        return triggerReceivers[triggerPoint].Count(r => r != null && r.IsReceiverActive());
    }
    
    /// <summary>
    /// Gets the total count of all registered receivers
    /// </summary>
    /// <returns>Total number of registered receivers</returns>
    public int GetTotalReceiverCount()
    {
        return allReceivers.Count(r => r != null && r.IsReceiverActive());
    }
    
    #endregion
    
    #region Unity Animator Integration
    
    private void TriggerUnityAnimator(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        if (unityAnimator == null || !unityAnimator.isActiveAndEnabled)
            return;
            
        if (animatorTriggerNames.ContainsKey(triggerPoint))
        {
            string triggerName = animatorTriggerNames[triggerPoint];
            
            try
            {
                // Set trigger in Unity Animator
                unityAnimator.SetTrigger(triggerName);
                
                // Optionally set additional parameters for context
                SetAnimatorContextParameters(context);
                
                if (EnableDebugLogs)
                {
                    Debug.Log($"[AnimationTriggerManager] Triggered Unity Animator: {triggerName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AnimationTriggerManager] Failed to trigger Unity Animator parameter '{triggerName}': {e.Message}");
            }
        }
    }
    
    private void SetAnimatorContextParameters(AnimationTriggerContext context)
    {
        if (unityAnimator == null)
            return;
            
        // Set common context parameters if they exist in the Animator
        try
        {
            // Try to set intensity parameter
            if (HasAnimatorParameter("TriggerIntensity"))
            {
                unityAnimator.SetFloat("TriggerIntensity", context.intensity);
            }
            
            // Try to set duration parameter
            if (HasAnimatorParameter("TriggerDuration"))
            {
                unityAnimator.SetFloat("TriggerDuration", context.duration);
            }
            
            // Try to set marker mode parameter
            if (HasAnimatorParameter("MarkerMode"))
            {
                unityAnimator.SetInteger("MarkerMode", (int)context.markerMode);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AnimationTriggerManager] Error setting Animator context parameters: {e.Message}");
        }
    }
    
    private bool HasAnimatorParameter(string parameterName)
    {
        if (unityAnimator == null || unityAnimator.runtimeAnimatorController == null)
            return false;
            
        foreach (var parameter in unityAnimator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Sets custom Unity Animator trigger name for a specific trigger point
    /// </summary>
    /// <param name="triggerPoint">The trigger point to customize</param>
    /// <param name="animatorTriggerName">The Unity Animator trigger parameter name</param>
    public void SetAnimatorTriggerName(AnimationTriggerPoint triggerPoint, string animatorTriggerName)
    {
        animatorTriggerNames[triggerPoint] = animatorTriggerName;
        
        if (EnableDebugLogs)
        {
            Debug.Log($"[AnimationTriggerManager] Set custom animator trigger: {triggerPoint} -> {animatorTriggerName}");
        }
    }
    
    #endregion
    
    #region Visual Debugging
    
    private void VisualizeTriggersInScene(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        // Create temporary visual indicator in scene for debugging
        if (!Application.isPlaying)
            return;
            
        Color debugColor = GetTriggerDebugColor(triggerPoint);
        
        // Draw debug sphere at trigger position
        Debug.DrawRay(context.primaryPosition, Vector3.up * context.intensity * 2f, debugColor, 1f);
        
        // If there's a secondary position, draw a line between them
        if (context.secondaryPosition != Vector3.zero)
        {
            Debug.DrawLine(context.primaryPosition, context.secondaryPosition, debugColor, 1f);
        }
    }
    
    private Color GetTriggerDebugColor(AnimationTriggerPoint triggerPoint)
    {
        switch (triggerPoint)
        {
            case AnimationTriggerPoint.ModeSwitch: return Color.blue;
            case AnimationTriggerPoint.MarkerPlace: return Color.green;
            case AnimationTriggerPoint.MarkerTrigger: return Color.red;
            case AnimationTriggerPoint.UIUpdate: return Color.yellow;
            case AnimationTriggerPoint.ActionFailed: return Color.magenta;
            case AnimationTriggerPoint.ActionSuccess: return Color.cyan;
            case AnimationTriggerPoint.CubeMarkerAction: return Color.white;
            case AnimationTriggerPoint.ResourceRegeneration: return new Color(0.5f, 1f, 0.5f);
            default: return Color.gray;
        }
    }

    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        int totalTriggers = triggerCounts.Values.Sum();
        int activeReceivers = GetTotalReceiverCount();
        
        return $"AnimationTrigger: Enabled:{enableAnimationTriggers} Triggers:{totalTriggers} Receivers:{activeReceivers} Last:{lastTriggerType}@{lastTriggerTime:F1}s";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Animation Triggers Enabled"] = enableAnimationTriggers,
            ["Total Triggers Fired"] = triggerCounts.Values.Sum(),
            ["Total Active Receivers"] = GetTotalReceiverCount(),
            ["Last Trigger Type"] = lastTriggerType.ToString(),
            ["Last Trigger Time"] = lastTriggerTime,
            ["Unity Animator Available"] = unityAnimator != null,
            ["Visual Debug Enabled"] = visualizeTriggersInScene,
            ["Log All Triggers"] = logAllTriggers,
            ["Default Animation Duration"] = defaultAnimationDuration
        };
        
        // Add trigger counts per type
        foreach (var kvp in triggerCounts)
        {
            debugData[$"Triggers - {kvp.Key}"] = kvp.Value;
        }
        
        // Add receiver counts per trigger point
        foreach (var triggerPoint in triggerReceivers.Keys)
        {
            debugData[$"Receivers - {triggerPoint}"] = GetReceiverCount(triggerPoint);
        }
        
        return debugData;
    }
    
    public void ResetToDefaults()
    {
        // Reset trigger counts
        foreach (var triggerPoint in triggerCounts.Keys.ToList())
        {
            triggerCounts[triggerPoint] = 0;
        }
        
        // Reset timing
        lastTriggerTime = 0f;
        lastTriggerType = AnimationTriggerPoint.UIUpdate;
        
        // Clear all receivers
        foreach (var triggerPoint in triggerReceivers.Keys.ToList())
        {
            triggerReceivers[triggerPoint].Clear();
        }
        allReceivers.Clear();
        
        // Reset settings to defaults
        enableAnimationTriggers = true;
        defaultAnimationDuration = 1.0f;
        showDebugLogs = false;
        logAllTriggers = false;
        visualizeTriggersInScene = false;
        
        if (EnableDebugLogs)
        {
            Debug.Log("[AnimationTriggerManager] Reset to defaults completed");
        }
    }
    
    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading
        if (EnableDebugLogs)
            Debug.Log($"[AnimationTriggerManager] Loading configuration: {configName} (not yet implemented)");
    }
    
    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving
        if (EnableDebugLogs)
            Debug.Log($"[AnimationTriggerManager] Saving configuration: {configName} (not yet implemented)");
    }
    
    #endregion
}
