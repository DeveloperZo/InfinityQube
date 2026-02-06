using System.Collections;
using System.Linq;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Tracks wave statistics including cube captures, escapes, and completion.
/// Extracted from WaveManager as part of SRP refactoring.
/// WaveManager maintains facade methods that delegate to this tracker.
/// </summary>
public class WaveStatisticsTracker : MonoBehaviour
{
    #region References
    private WaveManager waveManager;
    private GridManager grid;
    private AudioManager audioManager;
    
    // Logging
    private bool enableDebugLogs;
    #endregion

    #region Statistics State
    // Capture statistics
    private int normalCubesCaptured = 0;
    private int blueCubesCaptured = 0;
    private int reinforcedCubesCaptured = 0;
    private int cubesEscaped = 0;
    
    /// <summary>
    /// Tracks Unit cube escapes for row penalty system.
    /// When unitCubesEscaped >= grid.Width, the bottom row is removed as a penalty.
    /// Counter resets after penalty is applied and at the start of each new wave.
    /// </summary>
    private int unitCubesEscaped = 0;
    
    /// <summary>
    /// Tracks player deaths for row penalty system.
    /// When playerDeaths >= 2, the bottom row is removed as a penalty.
    /// Counter resets after penalty is applied and at the start of each new wave.
    /// </summary>
    private int playerDeaths = 0;
    private int markersPlaced = 0;
    private int detonationsUsed = 0;

    // Wave Completion Tracking
    private int totalNonBlackCubes = 0;
    private int processedNonBlackCubes = 0;
    #endregion

    #region Properties
    public int NormalCubesCaptured => normalCubesCaptured;
    public int BlueCubesCaptured => blueCubesCaptured;
    public int ReinforcedCubesCaptured => reinforcedCubesCaptured;
    public int CubesEscaped => cubesEscaped;
    public int UnitCubesEscaped => unitCubesEscaped;
    public int PlayerDeaths => playerDeaths;
    public int MarkersPlaced => markersPlaced;
    public int DetonationsUsed => detonationsUsed;
    public int TotalNonBlackCubes => totalNonBlackCubes;
    public int ProcessedNonBlackCubes => processedNonBlackCubes;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the statistics tracker with references to parent manager and dependencies.
    /// </summary>
    public void Initialize(WaveManager manager, GridManager gridManager, AudioManager audio, bool debugLogs)
    {
        waveManager = manager;
        grid = gridManager;
        audioManager = audio;
        enableDebugLogs = debugLogs;
        
        DebugLog("WaveStatisticsTracker initialized");
    }
    
    /// <summary>
    /// Updates debug logging state from parent manager.
    /// </summary>
    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }
    #endregion

    #region Public API - Statistics Management
    
    /// <summary>
    /// Resets all wave statistics to initial values.
    /// Called at the start of each new wave.
    /// </summary>
    public void ResetStatistics()
    {
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        reinforcedCubesCaptured = 0;
        cubesEscaped = 0;
        unitCubesEscaped = 0;
        playerDeaths = 0;
        markersPlaced = 0;
        detonationsUsed = 0;
        totalNonBlackCubes = 0;
        processedNonBlackCubes = 0;
        
        DebugLog("📊 Statistics reset for new wave");
    }
    
    /// <summary>
    /// Counts and stores the total non-black (non-Infinity) cubes for completion tracking.
    /// </summary>
    public void CountNonBlackCubes()
    {
        totalNonBlackCubes = waveManager.activeCubes.Count(c => c != null && !c.isDestroyed && c.type != CubeType.Infinity);
        processedNonBlackCubes = 0;
        DebugLog($"📊 Counted {totalNonBlackCubes} non-black cubes for wave completion tracking");
    }
    
    /// <summary>
    /// Records a cube capture event.
    /// </summary>
    public void OnCubeCaptured(CubeType cubeType)
    {
        switch (cubeType)
        {
            case CubeType.Unit: normalCubesCaptured++; break;
            case CubeType.Matrix: blueCubesCaptured++; break;
            case CubeType.Recursion: reinforcedCubesCaptured++; break;
        }

        // Trigger cube captured audio event
        if (audioManager != null)
        {
            var capturedCube = waveManager.activeCubes.FirstOrDefault(c => c != null && c.type == cubeType);
            Vector3 cubePosition = Vector3.zero;
            if (capturedCube != null && grid != null)
            {
                cubePosition = grid.GridToWorldPosition(capturedCube.position.x, capturedCube.position.y, 2f);
            }
            audioManager.TriggerCubeAudioEvent(GameAudioEvent.CubeCaptured, cubeType, cubePosition);
            DebugLog($"🔊 Audio: Cube captured event triggered for {cubeType}");
        }

        // Notify StageManager
        NotifyStageManager(sm => sm.OnCubeCaptured(cubeType));
    }
    
    /// <summary>
    /// CUBE ESCAPE HANDLER: Called when a cube escapes the play area.
    /// Processes the escape mechanic at the Wave level.
    /// </summary>
    public void OnCubeEscaped(CubeType cubeType)
    {
        // Find the cube that's escaping to get its position
        var escapingCube = waveManager.activeCubes.FirstOrDefault(c => c != null && c.type == cubeType && c.position.y <= 0);
        Vector2Int escapePosition = escapingCube != null ? escapingCube.position : Vector2Int.zero;
        
        // INCREMENT WAVE ESCAPE COUNTER
        cubesEscaped++;
        DebugLog($"🚨 CUBE ESCAPE: {cubeType} escaped from wave {waveManager.currentWaveIndex}. Total escapes: {cubesEscaped}");
        
        // Notify statistics manager about cube escape
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnCubeEscaped(escapePosition, cubeType.ToString());
        }
        
        // Trigger cube escaped audio event
        if (audioManager != null && grid != null)
        {
            Vector3 escapeWorldPosition = grid.GridToWorldPosition(escapePosition.x, escapePosition.y, 2f);
            audioManager.TriggerCubeAudioEvent(GameAudioEvent.CubeEscaped, cubeType, escapeWorldPosition);
            DebugLog($"🔊 Audio: Cube escaped event triggered for {cubeType} at position {escapePosition}");
        }
        
        // CHECK WAVE FAILURE CONDITION
        var currentWave = waveManager.CurrentWave;
        if (currentWave != null && currentWave.hasOwnSuccessCriteria && currentWave.maxAllowedEscapes >= 0)
        {
            if (cubesEscaped > currentWave.maxAllowedEscapes)
            {
                DebugLog($"❌ WAVE FAILED: Too many escapes! ({cubesEscaped} > {currentWave.maxAllowedEscapes})");
                waveManager.TriggerWaveFailureFromTracker("Too many cube escapes");
                return;
            }
        }
        
        // Process as normal cube behavior for wave completion tracking
        if (cubeType == CubeType.Unit)
        {
            unitCubesEscaped++;
            DebugLog($"Unit cube escaped. Total Unit escapes: {unitCubesEscaped}/{grid?.Width ?? 0} (threshold: {grid?.Width ?? 0} for row penalty)");
            
            // Row Penalty: When escaped Unit cubes equals number of columns, remove bottom row
            if (grid != null && unitCubesEscaped >= grid.Width)
            {
                DebugLog($"⚠️ ROW PENALTY TRIGGERED: {unitCubesEscaped} Unit cubes escaped (equals grid width {grid.Width}). Removing bottom row!");
                grid.RemoveBottomRow();
                unitCubesEscaped = 0; // Reset counter after penalty
            }
            
            OnNonBlackCubeProcessed(cubeType, false); // false = not captured
        }
    }
    
    /// <summary>
    /// Called when player dies. Tracks deaths and applies row penalty at 2 deaths.
    /// </summary>
    public void OnPlayerDeath()
    {
        playerDeaths++;
        DebugLog($"💀 Player death recorded. Total deaths this wave: {playerDeaths}/2 (threshold: 2 for row penalty)");
        
        // Death Penalty: When player dies 2 times, remove bottom row
        if (grid != null && playerDeaths >= 2)
        {
            DebugLog($"⚠️ DEATH PENALTY TRIGGERED: {playerDeaths} player deaths (threshold: 2). Removing bottom row!");
            grid.RemoveBottomRow();
            playerDeaths = 0; // Reset counter after penalty
        }
    }
    
    /// <summary>
    /// Records a marker placement.
    /// </summary>
    public void OnMarkerPlaced() => markersPlaced++;
    
    /// <summary>
    /// Records a detonation use.
    /// </summary>
    public void OnDetonationUsed() => detonationsUsed++;
    
    /// <summary>
    /// Called when a non-black cube is processed (captured or escaped).
    /// Tracks wave completion progress.
    /// </summary>
    public void OnNonBlackCubeProcessed(CubeType cubeType, bool wasCaptured)
    {
        if (cubeType == CubeType.Infinity) return;

        processedNonBlackCubes++;
        DebugLog($"📊 Non-black cube processed: {processedNonBlackCubes}/{totalNonBlackCubes}");

        if (processedNonBlackCubes >= totalNonBlackCubes)
        {
            string reason = wasCaptured ? "All cubes captured!" : "All cubes processed!";
            
            // Notify WaveManager to show completion message
            waveManager.ShowWaveCompletionFromTracker(reason);
        }
    }
    
    /// <summary>
    /// Gets penalty rows for cube type based on design doc.
    /// Unit: 1 row, Matrix: 2 rows, Recursion: 2 rows, Infinity: 0 rows
    /// </summary>
    public int GetPenaltyRowsForCubeType(CubeType cubeType)
    {
        switch (cubeType)
        {
            case CubeType.Unit:
                return 1;
            case CubeType.Matrix:
            case CubeType.Recursion:
                return 2;
            case CubeType.Infinity:
                return 0;
            default:
                return 1;
        }
    }
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Helper to notify StageManager via callback.
    /// </summary>
    private void NotifyStageManager(System.Action<StageManager> action)
    {
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null)
        {
            action(stageManager);
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WaveStatisticsTracker] {message}");
        }
    }
    #endregion
}
