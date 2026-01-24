using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// ScriptableObject defining a test scenario for development iteration.
/// Scenarios capture setup conditions for reproducible testing.
/// </summary>
[CreateAssetMenu(fileName = "New Scenario", menuName = "Infinity Qube/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    #region Scenario Identity
    
    [Header("Scenario Identity")]
    [Tooltip("Display name for this scenario")]
    public string scenarioName;
    
    [Tooltip("Category for organization and filtering")]
    public ScenarioCategory category = ScenarioCategory.Feature;
    
    [TextArea(2, 4)]
    [Tooltip("What this scenario tests or demonstrates")]
    public string description;
    
    [Tooltip("Priority within category (lower = shown first)")]
    [Range(0, 100)] public int priority = 50;
    
    [Tooltip("Tags for filtering (e.g., 'collision', 'markers', 'wave')")]
    public List<string> tags = new List<string>();
    
    #endregion
    
    #region Stage Configuration
    
    [Header("Stage Configuration")]
    [Tooltip("Stage to load (null = keep current stage)")]
    public StageData stage;
    
    [Tooltip("Wave index to start at (0-based, -1 = don't change)")]
    [Range(-1, 20)] public int waveIndex = -1;
    
    #endregion
    
    #region Grid Setup
    
    [Header("Grid Setup")]
    [Tooltip("Clear existing cubes before applying scenario")]
    public bool clearExistingCubes = true;
    
    [Tooltip("Clear existing markers before applying scenario")]
    public bool clearExistingMarkers = true;
    
    [Tooltip("Grid override dimensions (0 = use stage defaults)")]
    [Range(0, 12)] public int gridWidthOverride = 0;
    [Range(0, 40)] public int gridHeightOverride = 0;
    
    #endregion
    
    #region Player Setup
    
    [Header("Player Setup")]
    [Tooltip("Reset player position")]
    public bool resetPlayerPosition = true;
    
    [Tooltip("Player starting position (if resetPlayerPosition is true)")]
    public Vector2Int playerPosition = new Vector2Int(2, 0);
    
    [Tooltip("Marker charges to set (-1 = don't change)")]
    [Range(-1, 99)] public int unitMarkerCharges = -1;
    [Range(-1, 99)] public int matrixMarkerCharges = -1;
    [Range(-1, 99)] public int recursionMarkerCharges = -1;
    [Range(-1, 99)] public int infinityMarkerCharges = -1;
    
    #endregion
    
    #region Cube Placements
    
    [Header("Cube Placements")]
    [Tooltip("Wave cubes to spawn (enemy cubes)")]
    public List<ScenarioCubePlacement> waveCubes = new List<ScenarioCubePlacement>();
    
    [Tooltip("Player cubes to spawn")]
    public List<ScenarioCubePlacement> playerCubes = new List<ScenarioCubePlacement>();
    
    #endregion
    
    #region Marker Placements
    
    [Header("Marker Placements")]
    [Tooltip("Markers to place on the grid")]
    public List<ScenarioMarkerPlacement> markers = new List<ScenarioMarkerPlacement>();
    
    #endregion
    
    #region Timing
    
    [Header("Timing")]
    [Tooltip("Time scale to set (0 = pause, 1 = normal)")]
    [Range(0f, 4f)] public float timeScale = 1f;
    
    [Tooltip("Start wave after loading scenario")]
    public bool startWaveOnLoad = true;
    
    [Tooltip("Pause game after loading (for inspection)")]
    public bool pauseOnLoad = false;
    
    #endregion
    
    #region Validation (Optional)
    
    [Header("Validation (Optional)")]
    [Tooltip("If true, scenario has expected outcomes for automated testing")]
    public bool hasValidation = false;
    
    [Tooltip("Expected captures (for validation scenarios)")]
    [Range(0, 50)] public int expectedCaptures = 0;
    
    [Tooltip("Expected escapes (for validation scenarios)")]
    [Range(0, 50)] public int expectedEscapes = 0;
    
    [Tooltip("Max moves before scenario should resolve")]
    [Range(0, 100)] public int maxMoves = 20;
    
    [TextArea(2, 4)]
    [Tooltip("Notes about expected behavior for manual verification")]
    public string expectedBehaviorNotes;
    
    #endregion
    
    #region Validation Methods
    
    /// <summary>
    /// Validates scenario data and returns list of issues found.
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(scenarioName))
            issues.Add("Scenario name is empty");
        
        // Validate cube positions if we have a stage
        if (stage != null)
        {
            int width = gridWidthOverride > 0 ? gridWidthOverride : stage.gridWidth;
            int height = gridHeightOverride > 0 ? gridHeightOverride : stage.gridHeight;
            
            foreach (var cube in waveCubes)
            {
                if (cube.position.x < 0 || cube.position.x >= width)
                    issues.Add($"Wave cube X position ({cube.position.x}) out of bounds");
                if (cube.position.y < 0 || cube.position.y >= height)
                    issues.Add($"Wave cube Y position ({cube.position.y}) out of bounds");
            }
            
            foreach (var cube in playerCubes)
            {
                if (cube.position.x < 0 || cube.position.x >= width)
                    issues.Add($"Player cube X position ({cube.position.x}) out of bounds");
                if (cube.position.y < 0 || cube.position.y >= height)
                    issues.Add($"Player cube Y position ({cube.position.y}) out of bounds");
            }
        }
        
        return issues;
    }
    
    #endregion
    
    #region Editor Helpers
    
    /// <summary>
    /// Gets a short summary string for display.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();
        
        if (waveCubes.Count > 0)
            parts.Add($"{waveCubes.Count} wave");
        if (playerCubes.Count > 0)
            parts.Add($"{playerCubes.Count} player");
        if (markers.Count > 0)
            parts.Add($"{markers.Count} markers");
        
        return parts.Count > 0 ? string.Join(", ", parts) : "Empty setup";
    }
    
    #endregion
}

#region Data Classes

/// <summary>
/// Scenario categories for organization.
/// </summary>
public enum ScenarioCategory
{
    /// <summary>Critical scenarios that must always pass</summary>
    Keystone,
    /// <summary>Tests for previously fixed bugs</summary>
    Regression,
    /// <summary>Tests for specific features</summary>
    Feature,
    /// <summary>Performance and edge case tests</summary>
    Stress,
    /// <summary>Quick test setups for iteration</summary>
    QuickTest
}

/// <summary>
/// Cube placement data for scenarios.
/// </summary>
[System.Serializable]
public class ScenarioCubePlacement
{
    [Tooltip("Type of cube to spawn")]
    public CubeType type = CubeType.Unit;
    
    [Tooltip("Grid position (X, Y)")]
    public Vector2Int position;
    
    [Tooltip("Cube level (1 = normal)")]
    [Range(1, 5)] public int level = 1;
}

/// <summary>
/// Marker placement data for scenarios.
/// </summary>
[System.Serializable]
public class ScenarioMarkerPlacement
{
    [Tooltip("Type of marker to place")]
    public MarkerMode markerMode = MarkerMode.Unit;
    
    [Tooltip("Grid position (X, Y)")]
    public Vector2Int position;
}

#endregion
