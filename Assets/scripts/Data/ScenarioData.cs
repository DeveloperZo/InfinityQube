using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// ScriptableObject defining a test scenario.
/// Points to a scene that contains base setup. Commands drive player actions during the test.
/// </summary>
[CreateAssetMenu(fileName = "New Scenario", menuName = "Infinity Qube/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    #region Identity
    
    [Header("Identity")]
    [Tooltip("Display name for this scenario")]
    public string scenarioName;
    
    [Tooltip("Category for organization")]
    public ScenarioCategory category = ScenarioCategory.Feature;
    
    [TextArea(2, 4)]
    [Tooltip("What this scenario tests")]
    public string description;
    
    [Tooltip("Priority (lower = shown first)")]
    [Range(0, 100)] public int priority = 50;
    
    [Tooltip("Tags for filtering")]
    public List<string> tags = new List<string>();
    
    #endregion
    
    #region Scene
    
    [Header("Scene")]
    [Tooltip("Scene to load for this scenario")]
    public UnityEngine.Object sceneAsset;
    
    /// <summary>
    /// Get scene name from the scene asset
    /// </summary>
    public string SceneName
    {
        get
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
                if (path.EndsWith(".unity"))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(path);
                }
            }
#endif
            return sceneAsset != null ? sceneAsset.name : "";
        }
    }
    
    #endregion
    
    #region Commands
    
    [Header("Commands")]
    [Tooltip("Commands to execute during the scenario (ordered by step)")]
    public List<ScenarioCommand> commands = new List<ScenarioCommand>();
    
    #endregion
    
    #region Timing
    
    [Header("Timing")]
    [Tooltip("Max time before scenario times out (seconds)")]
    [Range(5f, 120f)] public float timeoutSeconds = 30f;
    
    [Tooltip("Max wave steps before scenario ends")]
    [Range(1, 50)] public int maxWaveSteps = 10;
    
    #endregion
    
    #region Success Conditions
    
    [Header("Success Conditions")]
    [Tooltip("What triggers scenario completion")]
    public ScenarioEndCondition endCondition = ScenarioEndCondition.AllCubesResolved;
    
    [Tooltip("Assertions to validate at completion")]
    public List<ScenarioAssertion> assertions = new List<ScenarioAssertion>();
    
    #endregion
    
    #region Documentation
    
    [Header("Documentation")]
    [Tooltip("Reference to feature documentation")]
    public string featureDocRef;
    
    [TextArea(2, 6)]
    [Tooltip("Expected behavior notes")]
    public string expectedBehaviorNotes;
    
    #endregion
}

#region Commands

[System.Serializable]
public class ScenarioCommand
{
    [Tooltip("Wave step to execute this command")]
    public int executeOnStep;
    
    [Tooltip("Type of command")]
    public CommandType type;
    
    [Tooltip("Target position for Move commands")]
    public Vector2Int targetPosition;
    
    [Tooltip("Marker type for PlaceMarker commands")]
    public MarkerType markerType;
    
    [Tooltip("Description of what this command does")]
    public string description;
    
    // Factory methods
    public static ScenarioCommand Move(int step, Vector2Int pos, string desc = "")
    {
        return new ScenarioCommand
        {
            executeOnStep = step,
            type = CommandType.Move,
            targetPosition = pos,
            description = string.IsNullOrEmpty(desc) ? $"Move to ({pos.x}, {pos.y})" : desc
        };
    }
    
    public static ScenarioCommand PlaceMarker(int step, Vector2Int pos, MarkerType marker, string desc = "")
    {
        return new ScenarioCommand
        {
            executeOnStep = step,
            type = CommandType.PlaceMarker,
            targetPosition = pos,
            markerType = marker,
            description = string.IsNullOrEmpty(desc) ? $"Place {marker} at ({pos.x}, {pos.y})" : desc
        };
    }
}

public enum CommandType
{
    Move,           // Move player to position
    PlaceMarker,    // Place a marker
    Wait            // Do nothing (for timing)
}

#endregion

#region Enums

public enum ScenarioCategory
{
    Keystone,   // Critical path tests
    Feature,    // Feature-specific tests
    Edge,       // Edge case tests
    Regression, // Bug regression tests
    Demo        // Demo/showcase scenarios
}

public enum ScenarioEndCondition
{
    AllCubesResolved,  // All cubes captured or escaped
    WaveComplete,      // Current wave ends
    Timeout,           // Time limit reached
    MaxSteps,          // Max wave steps reached
    Manual,            // User manually ends
    PlayerDeath        // Player dies
}

#endregion

#region Assertion Types

[System.Serializable]
public class ScenarioAssertion
{
    [Tooltip("What metric to check")]
    public AssertionType type;
    
    [Tooltip("Expected value")]
    public int expectedValue;
    
    [Tooltip("How to compare")]
    public ComparisonOp comparison = ComparisonOp.Equals;
    
    [Tooltip("Description of this assertion")]
    public string description;
    
    /// <summary>
    /// Evaluate assertion against actual value
    /// </summary>
    public bool Evaluate(int actual)
    {
        return comparison switch
        {
            ComparisonOp.Equals => actual == expectedValue,
            ComparisonOp.NotEquals => actual != expectedValue,
            ComparisonOp.GreaterThan => actual > expectedValue,
            ComparisonOp.LessThan => actual < expectedValue,
            ComparisonOp.GreaterOrEqual => actual >= expectedValue,
            ComparisonOp.LessOrEqual => actual <= expectedValue,
            _ => false
        };
    }
    
    // Factory methods
    public static ScenarioAssertion Equals(AssertionType type, int value, string desc = "")
    {
        return new ScenarioAssertion
        {
            type = type,
            expectedValue = value,
            comparison = ComparisonOp.Equals,
            description = string.IsNullOrEmpty(desc) ? $"{type} should equal {value}" : desc
        };
    }
}

public enum AssertionType
{
    CapturedCubes,
    EscapedCubes,
    WaveSteps,
    PlayerDeaths,
    MarkersPlaced
}

public enum ComparisonOp
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual
}

#endregion
