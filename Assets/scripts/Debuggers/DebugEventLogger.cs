#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Debug component that logs all GameEvents to the console for testing and verification.
/// Only compiles in Unity Editor to avoid debug overhead in builds.
/// </summary>
public class DebugEventLogger : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Logging Settings")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool showTimestamp = true;
    [SerializeField] private bool showEventCounts = true;
    
    [Header("Event Filters")]
    [SerializeField] private bool logStageEvents = true;
    [SerializeField] private bool logWaveEvents = true;
    [SerializeField] private bool logCubeEvents = true;
    [SerializeField] private bool logPlayerEvents = true;
    
    [Header("Display")]
    [SerializeField] private bool useColoredLogs = true;
    #endregion

    #region Runtime State
    private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
    private float sessionStartTime;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        sessionStartTime = Time.time;
        InitializeEventCounts();
    }

    private void OnEnable()
    {
        if (!enableLogging) return;
        
        SubscribeToEvents();
        LogEvent("SESSION", "Event Logger Enabled", Color.white);
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        
        if (showEventCounts && enableLogging)
        {
            LogEventSummary();
        }
        
        LogEvent("SESSION", "Event Logger Disabled", Color.white);
    }
    #endregion

    #region Event Subscriptions
    private void SubscribeToEvents()
    {
        // Stage Events
        if (logStageEvents)
        {
            GameEvents.OnStageStart += (index, data) => 
                LogEvent("STAGE", $"Stage {index} Started - '{data?.stageName ?? "Unknown"}'", Color.cyan);
                
            GameEvents.OnStageComplete += (index, success) => 
                LogEvent("STAGE", $"Stage {index} Completed - Success: {success}", Color.cyan);
                
            GameEvents.OnStageRestart += (index) => 
                LogEvent("STAGE", $"Stage {index} Restarted", Color.cyan);
        }
        
        // Wave Events
        if (logWaveEvents)
        {
            GameEvents.OnWaveStart += (index, data) => 
                LogEvent("WAVE", $"Wave {index} Started", Color.yellow);
                
            GameEvents.OnWaveStep += (index, step) => 
            {
                if (step % 5 == 0) // Log every 5th step to reduce spam
                    LogEvent("WAVE", $"Wave {index} - Step {step}", Color.yellow);
            };
                
            GameEvents.OnWaveComplete += (index) => 
                LogEvent("WAVE", $"Wave {index} Completed", Color.yellow);
                
            GameEvents.OnWaveProgress += (index, progress) => 
            {
                if (Mathf.Approximately(progress % 25f, 0f)) // Log at 25% intervals
                    LogEvent("WAVE", $"Wave {index} - Progress: {progress:F1}%", Color.yellow);
            };
        }
        
        // Cube Events
        if (logCubeEvents)
        {
            GameEvents.OnCubeSpawn += (pos, type) => 
                LogEvent("CUBE", $"{type} Spawned at ({pos.x}, {pos.y})", Color.green);
                
            GameEvents.OnCubeMove += (oldPos, newPos, type) => 
            {
                // Only log significant moves
                if (oldPos.y - newPos.y > 1 || Mathf.Abs(oldPos.x - newPos.x) > 0)
                    LogEvent("CUBE", $"{type} Moved: ({oldPos.x},{oldPos.y}) -> ({newPos.x},{newPos.y})", Color.green);
            };
                
            GameEvents.OnCubeCaptured += (pos, type) => 
                LogEvent("CUBE", $"{type} Captured at ({pos.x}, {pos.y})", Color.green);
                
            GameEvents.OnCubeEscaped += (pos, type) => 
                LogEvent("CUBE", $"{type} Escaped at ({pos.x}, {pos.y})", Color.green);
        }
        
        // Player Events
        if (logPlayerEvents)
        {
            GameEvents.OnPlayerMove += (oldPos, newPos) => 
                LogEvent("PLAYER", $"Moved: ({oldPos.x},{oldPos.y}) -> ({newPos.x},{newPos.y})", Color.white);
                
            GameEvents.OnMarkerPlaced += (pos, type) => 
                LogEvent("PLAYER", $"{type} Marker placed at ({pos.x}, {pos.y})", Color.white);
                
            GameEvents.OnPlayerDeath += (pos) => 
                LogEvent("PLAYER", $"Player died at ({pos.x}, {pos.y})", Color.white);
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        // Stage Events
        GameEvents.OnStageStart -= (index, data) => { };
        GameEvents.OnStageComplete -= (index, success) => { };
        GameEvents.OnStageRestart -= (index) => { };
        
        // Wave Events
        GameEvents.OnWaveStart -= (index, data) => { };
        GameEvents.OnWaveStep -= (index, step) => { };
        GameEvents.OnWaveComplete -= (index) => { };
        GameEvents.OnWaveProgress -= (index, progress) => { };
        
        // Cube Events
        GameEvents.OnCubeSpawn -= (pos, type) => { };
        GameEvents.OnCubeMove -= (oldPos, newPos, type) => { };
        GameEvents.OnCubeCaptured -= (pos, type) => { };
        GameEvents.OnCubeEscaped -= (pos, type) => { };
        
        // Player Events
        GameEvents.OnPlayerMove -= (oldPos, newPos) => { };
        GameEvents.OnMarkerPlaced -= (pos, type) => { };
        GameEvents.OnPlayerDeath -= (pos) => { };
    }
    #endregion

    #region Logging Methods
    private void LogEvent(string category, string message, Color color)
    {
        if (!enableLogging) return;
        
        // Update event count
        if (!eventCounts.ContainsKey(category))
            eventCounts[category] = 0;
        eventCounts[category]++;
        
        // Build log message
        string timestamp = showTimestamp ? $"[{Time.time - sessionStartTime:F2}s] " : "";
        string fullMessage = $"{timestamp}[GameEvents.{category}] {message}";
        
        // Log with or without color
        if (useColoredLogs && Application.isEditor)
        {
            Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{fullMessage}</color>");
        }
        else
        {
            Debug.Log(fullMessage);
        }
    }
    
    private void InitializeEventCounts()
    {
        eventCounts["SESSION"] = 0;
        eventCounts["STAGE"] = 0;
        eventCounts["WAVE"] = 0;
        eventCounts["CUBE"] = 0;
        eventCounts["PLAYER"] = 0;
    }
    
    private void LogEventSummary()
    {
        Debug.Log("===== GameEvents Session Summary =====");
        Debug.Log($"Session Duration: {Time.time - sessionStartTime:F2} seconds");
        
        int totalEvents = 0;
        foreach (var kvp in eventCounts)
        {
            if (kvp.Value > 0)
            {
                Debug.Log($"{kvp.Key} Events: {kvp.Value}");
                totalEvents += kvp.Value;
            }
        }
        
        Debug.Log($"Total Events Logged: {totalEvents}");
        Debug.Log("=====================================");
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Toggle logging on/off at runtime
    /// </summary>
    public void SetLoggingEnabled(bool enabled)
    {
        if (enableLogging != enabled)
        {
            enableLogging = enabled;
            
            if (enabled)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }
    }
    
    /// <summary>
    /// Get current event counts
    /// </summary>
    public Dictionary<string, int> GetEventCounts()
    {
        return new Dictionary<string, int>(eventCounts);
    }
    
    /// <summary>
    /// Reset event counts
    /// </summary>
    [ContextMenu("Reset Event Counts")]
    public void ResetEventCounts()
    {
        InitializeEventCounts();
        sessionStartTime = Time.time;
        LogEvent("SESSION", "Event counts reset", Color.white);
    }
    
    /// <summary>
    /// Log current event counts
    /// </summary>
    [ContextMenu("Show Event Summary")]
    public void ShowEventSummary()
    {
        LogEventSummary();
    }
    
    /// <summary>
    /// Test all event types
    /// </summary>
    [ContextMenu("Test All Events")]
    public void TestAllEvents()
    {
        Debug.Log("===== Testing All GameEvents =====");
        
        // Test Stage Events
        GameEvents.FireStageStart(0, null);
        GameEvents.FireStageComplete(0, true);
        GameEvents.FireStageRestart(0);
        
        // Test Wave Events
        GameEvents.FireWaveStart(0, null);
        GameEvents.FireWaveStep(0, 1);
        GameEvents.FireWaveProgress(0, 50f);
        GameEvents.FireWaveComplete(0);
        
        // Test Cube Events
        GameEvents.FireCubeSpawn(Vector2Int.zero, Enumerations.CubeType.Unit);
        GameEvents.FireCubeMove(Vector2Int.zero, Vector2Int.one, Enumerations.CubeType.Unit);
        GameEvents.FireCubeCaptured(Vector2Int.one, Enumerations.CubeType.Unit);
        GameEvents.FireCubeEscaped(Vector2Int.zero, Enumerations.CubeType.Unit);
        
        // Test Player Events
        GameEvents.FirePlayerMove(Vector2Int.zero, Vector2Int.one);
        GameEvents.FireMarkerPlaced(Vector2Int.one, Enumerations.MarkerType.Light);
        GameEvents.FirePlayerDeath(Vector2Int.one);
        
        Debug.Log("===== Test Complete =====");
    }
    #endregion

    #region Debug Display
    private void OnGUI()
    {
        if (!enableLogging || !showEventCounts) return;
        
        // Display event counts in corner of screen
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.BeginVertical("box");
        GUILayout.Label("GameEvents Monitor");
        
        foreach (var kvp in eventCounts)
        {
            if (kvp.Value > 0)
            {
                GUILayout.Label($"{kvp.Key}: {kvp.Value}");
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endregion
}
#endif
