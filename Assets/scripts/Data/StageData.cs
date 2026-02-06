using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using static Enumerations;

[CreateAssetMenu(fileName = "New Stage", menuName = "Infinity Qube/Stage Data")]
public class StageData : ScriptableObject
{
    #region Stage Identity
    
    [TabGroup("Main", "Identity")]
    [Title("Stage Identity")]
    [HorizontalGroup("Main/Identity/BasicInfo", LabelWidth = 90)]
    [LabelText("Number")]
    public int stageNumber;
    
    [HorizontalGroup("Main/Identity/BasicInfo")]
    [LabelText("Name")]
    [Required("Stage name is required")]
    public string stageName;
    
    [TabGroup("Main", "Identity")]
    [LabelText("Stage Type")]
    [EnumToggleButtons]
    public StageType stageType = StageType.Standard;
    
    [TabGroup("Main", "Identity")]
    [LabelText("Description")]
    [TextArea(2, 4)]
    public string description;
    
    [TabGroup("Main", "Identity")]
    [LabelText("Objective")]
    [TextArea(2, 3)]
    public string objective;
    
    #endregion

    #region Grid Configuration
    
    [TabGroup("Main", "Grid")]
    [Title("Grid Dimensions")]
    [HorizontalGroup("Main/Grid/Dimensions", LabelWidth = 50)]
    [LabelText("Width")]
    [Range(4, 12)] public int gridWidth = 6;
    
    [HorizontalGroup("Main/Grid/Dimensions")]
    [LabelText("Height")]
    [Range(10, 50)] public int gridHeight = 20;
    
    [TabGroup("Main", "Grid")]
    [Title("Player")]
    [HorizontalGroup("Main/Grid/Player", LabelWidth = 100)]
    [LabelText("Start Position")]
    public Vector2Int playerStartPosition = new Vector2Int(2, 0);
    
    [HorizontalGroup("Main/Grid/Player")]
    [LabelText("Respawn Delay")]
    [Tooltip("Move steps before respawn after death")]
    [Range(1, 10)] public int respawnDelayMoves = 1;
    
    [TabGroup("Main", "Grid")]
    [Title("Advanced")]
    [LabelText("Segment Layout Prefab")]
    [Tooltip("Multi-segment layout prefab. Leave empty for single-segment grid.")]
    [AssetsOnly]
    public GameObject segmentLayoutPrefab;
    
    /// <summary>
    /// Returns true if this stage has a segment layout prefab configured.
    /// </summary>
    public bool HasSegmentLayoutPrefab => segmentLayoutPrefab != null;
    
    [TabGroup("Main", "Grid")]
    [FoldoutGroup("Main/Grid/Line Divider Settings")]
    [LabelText("Enable Line Divider")]
    [ToggleLeft]
    [Tooltip("Enable the danger zone line divider system")]
    public bool enableLineDivider = true;
    
    [FoldoutGroup("Main/Grid/Line Divider Settings")]
    [ShowIf("enableLineDivider")]
    [LabelText("Starting Y Position")]
    [LabelWidth(120)]
    [Tooltip("Y position where line divider starts (danger zone above)")]
    [Range(0, 20)] public int lineDividerStartY = 10;
    
    [FoldoutGroup("Main/Grid/Line Divider Settings")]
    [ShowIf("enableLineDivider")]
    [HorizontalGroup("Main/Grid/Line Divider Settings/Adjustments", LabelWidth = 110)]
    [LabelText("Escape Penalty")]
    [Tooltip("Line moves UP by this amount per escape")]
    [Range(0, 3)] public int lineDividerEscapePenalty = 1;
    
    [FoldoutGroup("Main/Grid/Line Divider Settings")]
    [ShowIf("enableLineDivider")]
    [HorizontalGroup("Main/Grid/Line Divider Settings/Adjustments")]
    [LabelText("Capture Reward")]
    [Tooltip("Line moves DOWN by this amount per capture")]
    [Range(0, 3)] public int lineDividerCaptureReward = 1;
    
    #endregion

    #region Marker Economy (Stage Grants)
    
    [TabGroup("Main", "Economy")]
    [Title("Starting Inventory", "Marker charges granted at stage start")]
    [InfoBox("Stage grants SET inventory to these values. Wave grants ADD to inventory.", InfoMessageType.None)]
    [HideLabel]
    public MarkerGrants stageGrants = new MarkerGrants();
    
    [TabGroup("Main", "Economy")]
    [Title("Wave Grant Settings")]
    [LabelText("Use Wave-Specific Grants")]
    [ToggleLeft]
    [Tooltip("If enabled, each wave's grants come from WaveData. Otherwise uses defaults.")]
    public bool waveGrantsFromWaveData = false;
    
    [TabGroup("Main", "Economy")]
    [FoldoutGroup("Main/Economy/Attunement Restrictions")]
    [LabelText("Lock Attunements")]
    [ToggleLeft]
    [Tooltip("Prevent player from changing attunements during this stage")]
    public bool lockAttunements = true;
    
    [FoldoutGroup("Main/Economy/Attunement Restrictions")]
    [ShowIf("lockAttunements")]
    [LabelText("Allowed Attunements")]
    [Tooltip("Specific attunement IDs allowed (empty = all unlocked attunements)")]
    [ListDrawerSettings(ShowFoldout = false)]
    public List<string> allowedAttunementIds = new List<string>();
    
    #endregion

    #region Wave Configuration
    
    [TabGroup("Main", "Waves")]
    [Title("Wave List")]
    [InfoBox("$WavesSummary", InfoMessageType.None)]
    [ListDrawerSettings(
        ShowIndexLabels = true,
        ListElementLabelName = "waveName",
        DraggableItems = true,
        ShowItemCount = true,
        HideAddButton = false,
        HideRemoveButton = false
    )]
    [Required("At least one wave is required")]
    public List<WaveData> waveConfigurations = new List<WaveData>();
    
    private string WavesSummary => waveConfigurations == null || waveConfigurations.Count == 0 
        ? "No waves configured - drag WaveData assets here" 
        : $"{waveConfigurations.Count} wave(s) configured";
    
    [TabGroup("Main", "Waves")]
    [FoldoutGroup("Main/Waves/Stage Success Conditions")]
    [LabelText("Require All Cubes Destroyed")]
    [ToggleLeft]
    [Tooltip("Stage only succeeds if all cubes are captured")]
    public bool requireAllCubesDestroyed = false;
    
    [FoldoutGroup("Main/Waves/Stage Success Conditions")]
    [LabelText("Required Captures")]
    [LabelWidth(130)]
    [Tooltip("Minimum captures required (0 = no minimum)")]
    [Range(0, 100)]
    public int requiredCaptureCount = 0;
    
    [FoldoutGroup("Main/Waves/Stage Success Conditions")]
    [LabelText("Max Allowed Escapes")]
    [LabelWidth(130)]
    [Tooltip("Maximum escapes before failure (0 = no escapes allowed, -1 = unlimited)")]
    [Range(-1, 50)]
    public int maxAllowedEscapes = 0;
    
    #endregion

    #region Test Configuration
    
    [TabGroup("Main", "Testing")]
    [Title("Automated Testing")]
    [InfoBox("Enable to run this stage as an automated test scenario with scripted commands and assertions.", InfoMessageType.Info)]
    [LabelText("Enable Test Mode")]
    [ToggleLeft]
    [PropertyOrder(-1)]
    public bool isTestStage = false;
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestStage")]
    [Title("Test Commands")]
    [InfoBox("Commands execute at specified wave steps (movement, marker placement, etc.)", InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = false, MinScrollViewHeight = 100, MaxScrollViewHeight = 300)]
    public List<TestCommand> testCommands = new List<TestCommand>();
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestStage")]
    [Title("Assertions")]
    [InfoBox("Assertions validate test results at completion", InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = false, MinScrollViewHeight = 80, MaxScrollViewHeight = 200)]
    public List<TestAssertion> testAssertions = new List<TestAssertion>();
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestStage")]
    [FoldoutGroup("Main/Testing/Test Limits")]
    [LabelText("Max Test Steps")]
    [LabelWidth(100)]
    [Tooltip("Auto-complete after this many steps (0 = run all waves)")]
    [Range(0, 100)] public int maxTestSteps = 0;
    
    [FoldoutGroup("Main/Testing/Test Limits")]
    [ShowIf("isTestStage")]
    [LabelText("Timeout (sec)")]
    [LabelWidth(100)]
    [Tooltip("Fail test after this many seconds (0 = no timeout)")]
    [Range(0, 120)] public float testTimeout = 30f;
    
    [FoldoutGroup("Main/Testing/Test Limits")]
    [ShowIf("isTestStage")]
    [LabelText("Exit Play Mode on Complete")]
    [ToggleLeft]
    [Tooltip("Automatically exit play mode when test finishes (for CI/automation)")]
    public bool exitPlayModeOnComplete = false;
    
    #endregion

    #region Runtime Statistics
    
    [FoldoutGroup("Runtime Statistics", expanded: false)]
    [ReadOnly]
    public StageStatistics playerStatistics;
    
    #endregion
    
    #region Validation
    
    [FoldoutGroup("Validation", expanded: false)]
    [Button("Validate Stage", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    [PropertyOrder(100)]
    public void ValidateAndLog()
    {
        var issues = Validate();
        if (issues.Count == 0)
        {
            Debug.Log($"[StageData] {stageName}: Validation PASSED ✓");
        }
        else
        {
            Debug.LogWarning($"[StageData] {stageName}: {issues.Count} validation issue(s):");
            foreach (var issue in issues)
            {
                Debug.LogWarning($"  • {issue}");
            }
        }
    }
    
    [FoldoutGroup("Validation")]
    [ShowInInspector, ReadOnly]
    [ShowIf("@Validate().Count > 0")]
    [InfoBox("$ValidationErrorsSummary", InfoMessageType.Warning)]
    private string ValidationErrorsSummary
    {
        get
        {
            var issues = Validate();
            return issues.Count == 0 ? "" : $"{issues.Count} issue(s) found - click 'Validate Stage' for details";
        }
    }
    
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
        if (waveConfigurations != null)
        {
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
    [FoldoutGroup("Unit Marker")]
    [Title("Unit Marker", "Infinite supply with regeneration")]
    [HorizontalGroup("Unit Marker/Grid", LabelWidth = 80)]
    [LabelText("Max On Grid")]
    [Tooltip("Maximum Unit markers allowed on grid simultaneously")]
    [Range(1, 10)] public int unitMaxOnGrid = 5;
    
    [HorizontalGroup("Unit Marker/Grid")]
    [LabelText("Max Charges")]
    [Tooltip("Max charges in regeneration pool (0 = default 3)")]
    [Range(0, 10)] public int maxUnitMarkerCharges = 0;
    
    [FoldoutGroup("Unit Marker")]
    [LabelText("Recharge Rate")]
    [LabelWidth(100)]
    [Tooltip("Moves per charge regenerated (0 = default 3)")]
    [Range(0, 10)] public int unitMarkerRechargeRate = 0;
    
    [FoldoutGroup("Matrix Marker")]
    [Title("Matrix Marker", "Inventory-based")]
    [HorizontalGroup("Matrix Marker/Row", LabelWidth = 80)]
    [LabelText("Charges")]
    [Range(0, 10)] public int matrixCharges = 2;
    
    [HorizontalGroup("Matrix Marker/Row")]
    [LabelText("Max On Grid")]
    [Range(0, 5)] public int matrixMaxOnGrid = 2;
    
    [FoldoutGroup("Recursion Marker")]
    [Title("Recursion Marker", "Inventory-based")]
    [HorizontalGroup("Recursion Marker/Row", LabelWidth = 80)]
    [LabelText("Charges")]
    [Range(0, 10)] public int recursionCharges = 2;
    
    [HorizontalGroup("Recursion Marker/Row")]
    [LabelText("Max On Grid")]
    [Range(0, 5)] public int recursionMaxOnGrid = 2;
    
    [FoldoutGroup("Infinity Marker")]
    [Title("Infinity Marker", "Inventory-based")]
    [HorizontalGroup("Infinity Marker/Row", LabelWidth = 80)]
    [LabelText("Charges")]
    [Range(0, 5)] public int infinityCharges = 1;
    
    [HorizontalGroup("Infinity Marker/Row")]
    [LabelText("Max On Grid")]
    [Range(0, 3)] public int infinityMaxOnGrid = 1;
}

#region Test Classes

/// <summary>
/// Command to execute during an automated test stage.
/// Commands can move the player, place markers, or wait.
/// </summary>
[System.Serializable]
public class TestCommand
{
    [TableColumnWidth(50, Resizable = false)]
    [LabelText("Step")]
    [Tooltip("Wave step at which to execute this command")]
    public int executeOnStep;
    
    [TableColumnWidth(45, Resizable = false)]
    [LabelText("Glbl")]
    [Tooltip("Use global step count across all waves")]
    public bool useGlobalStep = false;
    
    [TableColumnWidth(90, Resizable = false)]
    [LabelText("Command")]
    [Tooltip("Type of command to execute")]
    public TestCommandType commandType;
    
    [TableColumnWidth(70, Resizable = false)]
    [LabelText("Position")]
    [Tooltip("Target position for Move/PlaceMarker commands")]
    public Vector2Int targetPosition;
    
    [TableColumnWidth(80, Resizable = false)]
    [LabelText("Marker")]
    [ShowIf("@commandType == TestCommandType.PlaceMarker")]
    [Tooltip("Marker type for PlaceMarker commands")]
    public MarkerType markerType;
    
    [LabelText("Notes")]
    [Tooltip("Description for logging")]
    public string description;
    
    /// <summary>
    /// Create a Move command.
    /// </summary>
    public static TestCommand Move(int step, Vector2Int position, string desc = "", bool globalStep = false)
    {
        return new TestCommand
        {
            executeOnStep = step,
            useGlobalStep = globalStep,
            commandType = TestCommandType.Move,
            targetPosition = position,
            description = string.IsNullOrEmpty(desc) ? $"Move to ({position.x}, {position.y})" : desc
        };
    }
    
    /// <summary>
    /// Create a PlaceMarker command.
    /// </summary>
    public static TestCommand PlaceMarker(int step, Vector2Int position, MarkerType marker, string desc = "", bool globalStep = false)
    {
        return new TestCommand
        {
            executeOnStep = step,
            useGlobalStep = globalStep,
            commandType = TestCommandType.PlaceMarker,
            targetPosition = position,
            markerType = marker,
            description = string.IsNullOrEmpty(desc) ? $"Place {marker} at ({position.x}, {position.y})" : desc
        };
    }
    
    /// <summary>
    /// Create a Wait command (no-op for timing).
    /// </summary>
    public static TestCommand Wait(int step, string desc = "", bool globalStep = false)
    {
        return new TestCommand
        {
            executeOnStep = step,
            useGlobalStep = globalStep,
            commandType = TestCommandType.Wait,
            description = string.IsNullOrEmpty(desc) ? "Wait" : desc
        };
    }
}

/// <summary>
/// Types of commands that can be executed during test stages.
/// </summary>
public enum TestCommandType
{
    Move,           // Move player to target position
    PlaceMarker,    // Place a marker at target position
    Wait            // Do nothing (for timing/spacing)
}

/// <summary>
/// Assertion to validate at the end of a test stage.
/// </summary>
[System.Serializable]
public class TestAssertion
{
    [TableColumnWidth(100, Resizable = false)]
    [LabelText("Metric")]
    [Tooltip("Metric to check")]
    public TestMetric metric;
    
    [TableColumnWidth(90, Resizable = false)]
    [LabelText("Compare")]
    [Tooltip("How to compare actual vs expected")]
    public ComparisonOp comparison = ComparisonOp.Equals;
    
    [TableColumnWidth(60, Resizable = false)]
    [LabelText("Value")]
    [Tooltip("Expected value")]
    public int expectedValue;
    
    [LabelText("Notes")]
    [Tooltip("Description for logging")]
    public string description;
    
    /// <summary>
    /// Evaluate assertion against actual value.
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
    
    /// <summary>
    /// Create an Equals assertion.
    /// </summary>
    public static TestAssertion Equals(TestMetric metric, int value, string desc = "")
    {
        return new TestAssertion
        {
            metric = metric,
            expectedValue = value,
            comparison = ComparisonOp.Equals,
            description = string.IsNullOrEmpty(desc) ? $"{metric} should equal {value}" : desc
        };
    }
    
    /// <summary>
    /// Create a GreaterOrEqual assertion.
    /// </summary>
    public static TestAssertion AtLeast(TestMetric metric, int value, string desc = "")
    {
        return new TestAssertion
        {
            metric = metric,
            expectedValue = value,
            comparison = ComparisonOp.GreaterOrEqual,
            description = string.IsNullOrEmpty(desc) ? $"{metric} should be at least {value}" : desc
        };
    }
}

/// <summary>
/// Metrics that can be asserted in test stages.
/// </summary>
public enum TestMetric
{
    CapturedCubes,
    EscapedCubes,
    PlayerDeaths,
    WaveSteps,
    MarkersPlaced,
    GlobalSteps,
    TilesVisited
}

/// <summary>
/// Comparison operators for test assertions.
/// </summary>
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
