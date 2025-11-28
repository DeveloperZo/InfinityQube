using System;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Central event system for all major game transitions and state changes.
/// Provides type-safe events that can be subscribed to by any system.
/// </summary>
public static class GameEvents
{
    #region Stage Events
    /// <summary>
    /// Fired when a stage begins loading and setup
    /// </summary>
    public static event Action<int, StageData> OnStageStart;
    
    /// <summary>
    /// Fired when a stage is completed (success or failure)
    /// </summary>
    public static event Action<int, bool> OnStageComplete;
    
    /// <summary>
    /// Fired when a stage is restarted after failure
    /// </summary>
    public static event Action<int> OnStageRestart;
    #endregion

    #region Wave Events
    /// <summary>
    /// Fired when a new wave begins
    /// </summary>
    public static event Action<int, WaveData> OnWaveStart;
    
    /// <summary>
    /// Fired on each wave step/tick when cubes move forward
    /// </summary>
    public static event Action<int, int> OnWaveStep; // waveIndex, stepNumber
    
    /// <summary>
    /// Fired when a wave is completed (all cubes processed)
    /// </summary>
    public static event Action<int> OnWaveComplete;
    
    /// <summary>
    /// Fired periodically to report wave progress
    /// </summary>
    public static event Action<int, float> OnWaveProgress; // waveIndex, progressPercent
    #endregion

    #region Cube Events
    /// <summary>
    /// Fired when a cube is spawned on the grid
    /// </summary>
    public static event Action<Vector2Int, CubeType> OnCubeSpawn;
    
    /// <summary>
    /// Fired when a cube moves from one position to another
    /// </summary>
    public static event Action<Vector2Int, Vector2Int, CubeType> OnCubeMove; // oldPos, newPos, type
    
    /// <summary>
    /// Fired when a cube is successfully captured by the player
    /// </summary>
    public static event Action<Vector2Int, CubeType> OnCubeCaptured;
    
    /// <summary>
    /// Fired when a cube escapes off the bottom of the grid
    /// </summary>
    public static event Action<Vector2Int, CubeType> OnCubeEscaped;
    #endregion

    #region Player Events
    /// <summary>
    /// Fired when the player moves to a new position
    /// </summary>
    public static event Action<Vector2Int, Vector2Int> OnPlayerMove; // oldPos, newPos
    
    /// <summary>
    /// Fired when the player places any type of marker
    /// </summary>
    public static event Action<Vector2Int, MarkerType> OnMarkerPlaced;
    
    /// <summary>
    /// Fired when the player dies/fails
    /// </summary>
    public static event Action<Vector2Int> OnPlayerDeath; // deathPosition
    #endregion

    #region Debug Settings
    /// <summary>
    /// Enable debug logging for all event firing
    /// </summary>
    public static bool debugEvents = false;
    #endregion

    #region Helper Methods - Stage Events
    /// <summary>
    /// Fire stage start event with null checking and optional debug logging
    /// </summary>
    public static void FireStageStart(int stageIndex, StageData stageData)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireStageStart: Stage {stageIndex} - {stageData?.stageName ?? "Unknown"}");
        
        OnStageStart?.Invoke(stageIndex, stageData);
    }
    
    /// <summary>
    /// Fire stage complete event with null checking and optional debug logging
    /// </summary>
    public static void FireStageComplete(int stageIndex, bool success)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireStageComplete: Stage {stageIndex} - Success: {success}");
        
        OnStageComplete?.Invoke(stageIndex, success);
    }
    
    /// <summary>
    /// Fire stage restart event with null checking and optional debug logging
    /// </summary>
    public static void FireStageRestart(int stageIndex)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireStageRestart: Stage {stageIndex}");
        
        OnStageRestart?.Invoke(stageIndex);
    }
    #endregion

    #region Helper Methods - Wave Events
    /// <summary>
    /// Fire wave start event with null checking and optional debug logging
    /// </summary>
    public static void FireWaveStart(int waveIndex, WaveData waveData)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireWaveStart: Wave {waveIndex}");
        
        OnWaveStart?.Invoke(waveIndex, waveData);
    }
    
    /// <summary>
    /// Fire wave step event with null checking and optional debug logging
    /// </summary>
    public static void FireWaveStep(int waveIndex, int stepNumber)
    {
        if (debugEvents && stepNumber % 5 == 0) // Log every 5th step to reduce spam
            Debug.Log($"[GameEvents] FireWaveStep: Wave {waveIndex}, Step {stepNumber}");
        
        OnWaveStep?.Invoke(waveIndex, stepNumber);
    }
    
    /// <summary>
    /// Fire wave complete event with null checking and optional debug logging
    /// </summary>
    public static void FireWaveComplete(int waveIndex)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireWaveComplete: Wave {waveIndex}");
        
        OnWaveComplete?.Invoke(waveIndex);
    }
    
    /// <summary>
    /// Fire wave progress event with null checking and optional debug logging
    /// </summary>
    public static void FireWaveProgress(int waveIndex, float progressPercent)
    {
        if (debugEvents && Mathf.Approximately(progressPercent % 25f, 0f)) // Log at 25% intervals
            Debug.Log($"[GameEvents] FireWaveProgress: Wave {waveIndex}, Progress {progressPercent:F1}%");
        
        OnWaveProgress?.Invoke(waveIndex, progressPercent);
    }
    #endregion

    #region Helper Methods - Cube Events
    /// <summary>
    /// Fire cube spawn event with null checking and optional debug logging
    /// </summary>
    public static void FireCubeSpawn(Vector2Int position, CubeType cubeType)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireCubeSpawn: {cubeType} at ({position.x}, {position.y})");
        
        OnCubeSpawn?.Invoke(position, cubeType);
    }
    
    /// <summary>
    /// Fire cube move event with null checking and optional debug logging
    /// </summary>
    public static void FireCubeMove(Vector2Int oldPosition, Vector2Int newPosition, CubeType cubeType)
    {
        // Only log significant moves to reduce spam
        if (debugEvents && (oldPosition.y - newPosition.y > 1 || Mathf.Abs(oldPosition.x - newPosition.x) > 0))
            Debug.Log($"[GameEvents] FireCubeMove: {cubeType} from ({oldPosition.x}, {oldPosition.y}) to ({newPosition.x}, {newPosition.y})");
        
        OnCubeMove?.Invoke(oldPosition, newPosition, cubeType);
    }
    
    /// <summary>
    /// Fire cube captured event with null checking and optional debug logging
    /// </summary>
    public static void FireCubeCaptured(Vector2Int position, CubeType cubeType)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireCubeCaptured: {cubeType} at ({position.x}, {position.y})");
        
        OnCubeCaptured?.Invoke(position, cubeType);
    }
    
    /// <summary>
    /// Fire cube escaped event with null checking and optional debug logging
    /// </summary>
    public static void FireCubeEscaped(Vector2Int position, CubeType cubeType)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireCubeEscaped: {cubeType} at ({position.x}, {position.y})");
        
        OnCubeEscaped?.Invoke(position, cubeType);
    }
    #endregion

    #region Helper Methods - Player Events
    /// <summary>
    /// Fire player move event with null checking and optional debug logging
    /// </summary>
    public static void FirePlayerMove(Vector2Int oldPosition, Vector2Int newPosition)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FirePlayerMove: ({oldPosition.x}, {oldPosition.y}) to ({newPosition.x}, {newPosition.y})");
        
        OnPlayerMove?.Invoke(oldPosition, newPosition);
    }
    
    /// <summary>
    /// Fire marker placed event with null checking and optional debug logging
    /// </summary>
    public static void FireMarkerPlaced(Vector2Int position, MarkerType markerType)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FireMarkerPlaced: {markerType} at ({position.x}, {position.y})");
        
        OnMarkerPlaced?.Invoke(position, markerType);
    }
    
    /// <summary>
    /// Fire player death event with null checking and optional debug logging
    /// </summary>
    public static void FirePlayerDeath(Vector2Int deathPosition)
    {
        if (debugEvents)
            Debug.Log($"[GameEvents] FirePlayerDeath: Player died at ({deathPosition.x}, {deathPosition.y})");
        
        OnPlayerDeath?.Invoke(deathPosition);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Get the total number of subscribers across all events (for debugging)
    /// </summary>
    public static int GetTotalSubscriberCount()
    {
        int count = 0;
        
        // Stage events
        count += OnStageStart?.GetInvocationList()?.Length ?? 0;
        count += OnStageComplete?.GetInvocationList()?.Length ?? 0;
        count += OnStageRestart?.GetInvocationList()?.Length ?? 0;
        
        // Wave events
        count += OnWaveStart?.GetInvocationList()?.Length ?? 0;
        count += OnWaveStep?.GetInvocationList()?.Length ?? 0;
        count += OnWaveComplete?.GetInvocationList()?.Length ?? 0;
        count += OnWaveProgress?.GetInvocationList()?.Length ?? 0;
        
        // Cube events
        count += OnCubeSpawn?.GetInvocationList()?.Length ?? 0;
        count += OnCubeMove?.GetInvocationList()?.Length ?? 0;
        count += OnCubeCaptured?.GetInvocationList()?.Length ?? 0;
        count += OnCubeEscaped?.GetInvocationList()?.Length ?? 0;
        
        // Player events
        count += OnPlayerMove?.GetInvocationList()?.Length ?? 0;
        count += OnMarkerPlaced?.GetInvocationList()?.Length ?? 0;
        count += OnPlayerDeath?.GetInvocationList()?.Length ?? 0;
        
        return count;
    }
    
    /// <summary>
    /// Clear all event subscribers (use with caution - mainly for testing)
    /// </summary>
    public static void ClearAllSubscribers()
    {
        // Stage events
        OnStageStart = null;
        OnStageComplete = null;
        OnStageRestart = null;
        
        // Wave events
        OnWaveStart = null;
        OnWaveStep = null;
        OnWaveComplete = null;
        OnWaveProgress = null;
        
        // Cube events
        OnCubeSpawn = null;
        OnCubeMove = null;
        OnCubeCaptured = null;
        OnCubeEscaped = null;
        
        // Player events
        OnPlayerMove = null;
        OnMarkerPlaced = null;
        OnPlayerDeath = null;
        
        if (debugEvents)
            Debug.Log("[GameEvents] ClearAllSubscribers: All event subscribers cleared");
    }
    #endregion
}
