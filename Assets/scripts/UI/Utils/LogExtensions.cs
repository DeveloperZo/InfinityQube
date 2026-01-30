using UnityEngine;

/// <summary>
/// Provides consistent logging functionality across all managers with standardized formatting.
/// Extension methods allow any class to use these methods with automatic class name prefixing.
/// 
/// === LOGGING CATEGORY REFERENCE ===
/// 
/// CRITICAL (keep ON for debugging - default enabled):
///   - StageManager       : Stage loading, transitions, configuration
///   - WaveManager        : Wave progression, cube spawning, completion
///   - GridManager        : Grid state, segment setup, tile operations
///   - PlayerManager      : Player death, movement issues, collision
///   - PlayerActionManager: Marker economy (always on - uses direct Debug.Log)
/// 
/// NOISY (safe to disable - default OFF):
///   - CubeCollisionManager   : High-frequency collision checks
///   - MarkerVisualManager    : Visual marker updates
///   - AudioManager           : Audio event playback
///   - AudioDebugSystem       : Audio system internals
///   - MessageHighlightManager: UI highlight updates
///   - SaveManager            : Save/load operations
/// 
/// MODERATE (case-by-case):
///   - WaveSegmentController  : Segment transitions
///   - GridRowManager         : Row removal animations
///   - PlayerMarkerSystem     : Marker placement details
///   - AttunementManager      : Attunement changes
///   - ScoreManager           : Score calculations
/// 
/// To toggle logging: Set EnableDebugLogs in Inspector for each manager.
/// Errors (LogError) always log regardless of flag settings.
/// </summary>
public static class LogExtensions
{
    /// <summary>
    /// Logs a message with the calling class name as prefix.
    /// </summary>
    /// <param name="caller">The object calling the log method (uses 'this')</param>
    /// <param name="message">The message to log</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled (default: true)</param>
    public static void Log(this object caller, string message, bool EnableDebugLogs = true)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        Debug.Log($"[{prefix}] {message}");
    }
    
    /// <summary>
    /// Logs a warning message with the calling class name as prefix.
    /// </summary>
    /// <param name="caller">The object calling the log method (uses 'this')</param>
    /// <param name="message">The warning message to log</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled (default: true)</param>
    public static void LogWarning(this object caller, string message, bool EnableDebugLogs = true)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        Debug.LogWarning($"[{prefix}] {message}");
    }
    
    /// <summary>
    /// Logs an error message with the calling class name as prefix.
    /// Errors are always logged regardless of debug settings.
    /// </summary>
    /// <param name="caller">The object calling the log method (uses 'this')</param>
    /// <param name="message">The error message to log</param>
    public static void LogError(this object caller, string message)
    {
        string prefix = caller.GetType().Name;
        Debug.LogError($"[{prefix}] {message}");
    }
    
    /// <summary>
    /// Logs a formatted message with additional context for state changes.
    /// </summary>
    /// <param name="caller">The object calling the log method</param>
    /// <param name="state">The state being changed</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled</param>
    public static void LogStateChange(this object caller, string state, object oldValue, object newValue, bool EnableDebugLogs = true)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        Debug.Log($"[{prefix}] {state} changed: {oldValue} → {newValue}");
    }
    
    /// <summary>
    /// Logs a formatted message for action execution with optional timing.
    /// </summary>
    /// <param name="caller">The object calling the log method</param>
    /// <param name="action">The action being performed</param>
    /// <param name="details">Additional details about the action</param>
    /// <param name="duration">Optional duration in milliseconds</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled</param>
    public static void LogAction(this object caller, string action, string details = "", float? duration = null, bool EnableDebugLogs = true)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        string message = $"[{prefix}] {action}";
        
        if (!string.IsNullOrEmpty(details))
            message += $": {details}";
            
        if (duration.HasValue)
            message += $" ({duration.Value:F2}ms)";
            
        Debug.Log(message);
    }
    
    /// <summary>
    /// Logs initialization status with dependency information.
    /// </summary>
    /// <param name="caller">The object calling the log method</param>
    /// <param name="dependencies">Array of dependency status strings</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled</param>
    public static void LogInitialization(this object caller, bool EnableDebugLogs = true, params string[] dependencies)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        string message = $"[{prefix}] Initialized";
        
        if (dependencies != null && dependencies.Length > 0)
        {
            message += " - Dependencies: " + string.Join(", ", dependencies);
        }
        
        Debug.Log(message);
    }
    
    /// <summary>
    /// Logs performance metrics with operation count and duration.
    /// </summary>
    /// <param name="caller">The object calling the log method</param>
    /// <param name="operation">The operation being measured</param>
    /// <param name="count">Number of operations performed</param>
    /// <param name="duration">Duration in milliseconds</param>
    /// <param name="EnableDebugLogs">Whether debug logging is enabled</param>
    public static void LogPerformance(this object caller, string operation, int count, float duration, bool EnableDebugLogs = true)
    {
        if (!EnableDebugLogs) return;
        
        string prefix = caller.GetType().Name;
        float avgTime = count > 0 ? duration / count : 0f;
        Debug.Log($"[{prefix}] PERF - {operation}: {count} operations in {duration:F2}ms (avg: {avgTime:F4}ms)");
    }
}
