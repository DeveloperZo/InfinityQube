using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

[CreateAssetMenu(fileName = "New Wave", menuName = "Infinity Qube/Wave Data")]
public class WaveData : ScriptableObject
{
    #region Wave Identity
    
    [Header("Wave Identity")]
    [Tooltip("Wave index within the stage (0-based)")]
    public int Index = 0;  // Keep exact name for asset deserialization
    [Tooltip("Display name for this wave (optional)")]
    public string waveName;
    
    // Alias property for new code style
    public int waveIndex { get => Index; set => Index = value; }
    
    #endregion

    #region Wave Grid (Spawn Area)
    
    [Header("Wave Grid (Spawn Area)")]
    [Tooltip("Width of the spawn area for this wave")]
    [Range(1, 12)] public int GridWidth = 3;  // Keep exact name for asset deserialization
    [Tooltip("Height/depth of the spawn area")]
    [Range(1, 10)] public int GridHeight = 3;  // Keep exact name for asset deserialization
    
    // Alias properties for new code style
    public int spawnWidth { get => GridWidth; set => GridWidth = value; }
    public int spawnHeight { get => GridHeight; set => GridHeight = value; }
    
    #endregion

    #region Cube Configuration
    
    [Header("Cube Configuration")]
    [Tooltip("Cubes spawned in this wave with positions and types")]
    public List<CubeData> CubesData = new List<CubeData>();  // Keep exact name for asset deserialization
    
    // Alias property for new code style
    public List<CubeData> cubes { get => CubesData; set => CubesData = value; }
    
    #endregion

    #region Marker Grants (Wave-Level)
    
    [Header("Marker Grants (Added at Wave Start)")]
    [Tooltip("If true, these grants ADD to current inventory. If false, they SET inventory.")]
    public bool grantsAddToInventory = true;
    
    [Space(5)]
    // NOTE: Unit markers are INFINITE with move-based regeneration - wave grants don't affect them
    
    [Tooltip("Matrix marker charges granted (0 = no grant)")]
    [Range(0, 10)] public int grantMatrixCharges = 0;
    
    [Tooltip("Recursion marker charges granted (0 = no grant)")]
    [Range(0, 10)] public int grantRecursionCharges = 0;
    
    [Tooltip("Infinity marker charges granted (0 = no grant)")]
    [Range(0, 5)] public int grantInfinityCharges = 0;
    
    #endregion

    #region Marker Caps (Wave-Level Overrides)
    
    [Header("Marker Caps (0 = Use Stage Defaults)")]
    [Tooltip("Override max Unit markers on grid (0 = use stage default)")]
    [Range(0, 10)] public int overrideUnitMaxOnGrid = 0;
    
    [Tooltip("Override max Matrix markers on grid (0 = use stage default)")]
    [Range(0, 5)] public int overrideMatrixMaxOnGrid = 0;
    
    [Tooltip("Override max Recursion markers on grid (0 = use stage default)")]
    [Range(0, 5)] public int overrideRecursionMaxOnGrid = 0;
    
    [Tooltip("Override max Infinity markers on grid (0 = use stage default)")]
    [Range(0, 3)] public int overrideInfinityMaxOnGrid = 0;
    
    [Tooltip("Override Unit marker recharge rate in moves per charge (0 = use stage default, typically 3 moves)")]
    [Range(0, 10)] public int overrideUnitMarkerRechargeRate = 0;
    
    #endregion

    #region Wave Timing
    
    [Header("Wave Timing")]
    [Tooltip("Delay before wave starts spawning")]
    [Range(0f, 10f)] public float waveStartDelay = 1f;
    
    [Tooltip("Time between cube movements (normal speed)")]
    [Range(0.1f, 3f)] public float moveInterval = 0.5f;
    
    [Tooltip("Time between cube movements (fast-forward)")]
    [Range(0.05f, 1f)] public float fastMoveInterval = 0.1f;
    
    [Tooltip("Number of move steps before player respawns after death (0 = use stage default, typically 1)")]
    [Range(0, 10)] public int respawnDelayMoves = 0;
    
    #endregion

    #region Wave Success Criteria
    
    [Header("Wave Success Criteria")]
    [Tooltip("If true, wave has its own success criteria separate from stage")]
    public bool hasOwnSuccessCriteria = false;
    
    [Tooltip("Minimum captures required for wave success (0 = no requirement)")]
    [Range(0, 50)] public int requiredCaptureCount = 0;
    
    [Tooltip("Maximum escapes allowed (0 = no escapes allowed, -1 = unlimited)")]
    [Range(-1, 20)] public int maxAllowedEscapes = -1;
    
    #endregion

    // Messages removed - use highlightSequences instead (sequences contain messageText field)
    
    #region Highlight Sequences
    
    [Header("Highlight Sequences")]
    [Tooltip("Guided sequences: pause → message → highlight → resume. Executed at DisplayMoveStep timing.")]
    public List<HighlightSequence> highlightSequences = new List<HighlightSequence>();
    
    #endregion

    #region Runtime Statistics (Not Serialized)
    
    [Header("Runtime Statistics")]
    [SerializeField] private WaveStatistics _runtimeStats = new WaveStatistics();
    public WaveStatistics RuntimeStats => _runtimeStats;
    
    /// <summary>
    /// Reset runtime statistics for a new playthrough.
    /// </summary>
    public void ResetRuntimeStats()
    {
        _runtimeStats = new WaveStatistics();
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validates wave data and returns list of issues found.
    /// </summary>
    public List<string> Validate(int stageGridWidth = 6)
    {
        var issues = new List<string>();
        
        if (spawnWidth < 1)
            issues.Add("Spawn width must be at least 1");
            
        if (spawnWidth > stageGridWidth)
            issues.Add($"Spawn width ({spawnWidth}) exceeds stage grid width ({stageGridWidth})");
            
        if (cubes == null || cubes.Count == 0)
            issues.Add("No cubes defined in wave");
            
        if (moveInterval <= 0)
            issues.Add("Move interval must be positive");
            
        if (fastMoveInterval >= moveInterval)
            issues.Add("Fast move interval should be less than normal move interval");
        
        // Validate cube positions
        if (cubes != null)
        {
            for (int i = 0; i < cubes.Count; i++)
            {
                var cube = cubes[i];
                if (cube.position.x < 0 || cube.position.x >= spawnWidth)
                    issues.Add($"Cube {i} X position ({cube.position.x}) out of spawn bounds (0-{spawnWidth - 1})");
                if (cube.position.y < 0 || cube.position.y >= spawnHeight)
                    issues.Add($"Cube {i} Y position ({cube.position.y}) out of spawn bounds (0-{spawnHeight - 1})");
            }
        }
        
        return issues;
    }
    
    #endregion
}

/// <summary>
/// Runtime statistics for a single wave playthrough.
/// </summary>
[System.Serializable]
public class WaveStatistics
{
    public int unitCubesCaptured;
    public int matrixCubesCaptured;
    public int recursionCubesCaptured;
    public int infinityCubesCaptured;
    public int cubesEscaped;
    public int markersPlaced;
    public int markersTriggererd;
    public float completionTime;
}