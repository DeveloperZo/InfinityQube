using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

[CreateAssetMenu(fileName = "New Stage", menuName = "Infinity Qube/Stage Data")]
public class StageData : ScriptableObject
{
    #region Stage Identity
    
    [Header("Stage Identity")]
    public int stageNumber;
    public string stageName;
    public StageType stageType = StageType.Standard;
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string objective;
    
    #endregion

    #region Grid Configuration
    
    [Header("Grid Configuration")]
    [Range(4, 12)] public int gridWidth = 6;
    [Range(10, 30)] public int gridHeight = 20;
    public Vector2Int playerStartPosition = new Vector2Int(2, 0);
    
    [Tooltip("Number of move steps before player respawns after death (default: 1 move)")]
    [Range(1, 10)] public int respawnDelayMoves = 1;
    
    [Header("Segment Layout")]
    [Tooltip("Prefab containing GridSegmentController objects for multi-segment stages. If null, uses single-segment grid.")]
    public GameObject segmentLayoutPrefab;
    
    /// <summary>
    /// Returns true if this stage has a segment layout prefab configured.
    /// </summary>
    public bool HasSegmentLayoutPrefab => segmentLayoutPrefab != null;
    
    #endregion

    #region Line Divider
    
    [Header("Line Divider")]
    [Tooltip("Enable or disable the line divider system for this stage")]
    public bool enableLineDivider = true;
    [Tooltip("Starting Y position of the line divider (danger zone above)")]
    [Range(0, 20)] public int lineDividerStartY = 10;
    [Tooltip("How much the line moves up per escape")]
    [Range(0, 3)] public int lineDividerEscapePenalty = 1;
    [Tooltip("How much the line moves down per capture")]
    [Range(0, 3)] public int lineDividerCaptureReward = 1;
    
    #endregion

    #region Marker Economy (Stage Grants)
    
    [Header("Marker Economy - Stage Grants")]
    [Tooltip("Marker charges granted at stage start (SETS inventory to these values)")]
    public MarkerGrants stageGrants = new MarkerGrants();
    
    [Tooltip("If true, wave grants come from WaveData. If false, waves use inspector defaults. Stage grants + Wave grants are BOTH applied (combinatorial).")]
    public bool waveGrantsFromWaveData = false;
    
    #endregion

    #region Attunement Configuration
    
    [Header("Attunement Configuration")]
    [Tooltip("If true, attunements are locked and cannot be changed during this stage")]
    public bool lockAttunements = true;
    [Tooltip("Specific attunements allowed for this stage (empty = all unlocked attunements)")]
    public List<string> allowedAttunementIds = new List<string>();
    
    #endregion

    #region Wave Configuration
    
    [Header("Wave Configuration")]
    public List<WaveData> waveConfigurations = new List<WaveData>();
    
    #endregion

    #region Success Conditions
    
    [Header("Success Conditions")]
    public bool requireAllCubesDestroyed = false;
    [Tooltip("Minimum cubes to capture (0 = no requirement)")]
    public int requiredCaptureCount = 0;
    [Tooltip("Maximum escapes allowed (0 = no escapes allowed)")]
    public int maxAllowedEscapes = 0;
    
    #endregion

    #region Runtime Statistics
    
    [Header("Runtime Statistics (Read-Only)")]
    public StageStatistics playerStatistics;
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validates stage data and returns list of issues found.
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(stageName))
            issues.Add("Stage name is empty");
            
        if (waveConfigurations == null || waveConfigurations.Count == 0)
            issues.Add("No wave configurations defined");
            
        if (gridWidth < 4)
            issues.Add($"Grid width ({gridWidth}) too small, minimum is 4");
            
        if (gridHeight < 10)
            issues.Add($"Grid height ({gridHeight}) too small, minimum is 10");
            
        if (playerStartPosition.x < 0 || playerStartPosition.x >= gridWidth)
            issues.Add($"Player start X ({playerStartPosition.x}) out of grid bounds");
            
        // Only validate line divider position if it's enabled
        if (enableLineDivider && lineDividerStartY > gridHeight)
            issues.Add($"Line divider start ({lineDividerStartY}) above grid height ({gridHeight})");
        
        // Validate each wave
        for (int i = 0; i < waveConfigurations.Count; i++)
        {
            var wave = waveConfigurations[i];
            if (wave == null)
            {
                issues.Add($"Wave {i} is null");
                continue;
            }
            
            var waveIssues = wave.Validate(gridWidth);
            foreach (var issue in waveIssues)
            {
                issues.Add($"Wave {i}: {issue}");
            }
        }
        
        // NOTE: GridPath validation removed - use GridSegmentController for multi-segment layouts
        
        return issues;
    }
    
    #endregion
}

/// <summary>
/// Marker charges granted to player. Used for stage and wave grants.
/// NOTE: Unit markers are INFINITE with move-based regeneration - only max-on-grid matters.
/// </summary>
[System.Serializable]
public class MarkerGrants
{
    [Header("Unit Marker (INFINITE with move-based regeneration)")]
    [Tooltip("Unit markers are infinite - this only limits how many can be on grid at once")]
    [Range(1, 10)] public int unitMaxOnGrid = 5;
    [Tooltip("Number of wave moves required to regenerate one Unit marker charge (0 = use default 3)")]
    [Range(0, 10)] public int unitMarkerRechargeRate = 0;
    [Tooltip("Maximum Unit marker charges in the regeneration pool (0 = use default 3)")]
    [Range(0, 10)] public int maxUnitMarkerCharges = 0;
    
    [Header("Matrix Marker (inventory-based)")]
    [Range(0, 10)] public int matrixCharges = 2;
    [Range(0, 5)] public int matrixMaxOnGrid = 2;
    
    [Header("Recursion Marker (inventory-based)")]
    [Range(0, 10)] public int recursionCharges = 2;
    [Range(0, 5)] public int recursionMaxOnGrid = 2;
    
    [Header("Infinity Marker (inventory-based)")]
    [Range(0, 5)] public int infinityCharges = 1;
    [Range(0, 3)] public int infinityMaxOnGrid = 1;
}