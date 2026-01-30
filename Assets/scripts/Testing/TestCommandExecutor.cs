using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Executes test commands during automated test stages.
/// Subscribes to game events, tracks metrics, and evaluates assertions.
/// </summary>
public class TestCommandExecutor : MonoBehaviour
{
    #region Singleton
    
    public static TestCommandExecutor Instance { get; private set; }
    
    #endregion
    
    #region Inspector
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region State
    
    private StageData activeStage;
    private WaveData activeWave;
    private float startTime;
    private bool isRunning;
    
    // Step tracking
    private int globalStepCount;
    private int currentWaveStepCount;
    private int currentWaveIndex;
    
    // Metrics
    private int capturedCubes;
    private int escapedCubes;
    private int playerDeaths;
    private int markersPlaced;
    
    // Position tracking
    private HashSet<Vector2Int> tilesVisited = new HashSet<Vector2Int>();
    private Vector2Int lastLoggedPosition = new Vector2Int(-999, -999);
    
    // Command execution
    private HashSet<int> executedCommandIndices = new HashSet<int>();
    private PlayerManager playerManager;
    private PlayerActionManager actionManager;
    
    // Results
    private List<AssertionResult> results = new List<AssertionResult>();
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Fired when test completes with results.
    /// </summary>
    public static event System.Action<StageData, bool, List<AssertionResult>> OnTestCompleted;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private void Update()
    {
        if (!isRunning || activeStage == null) return;
        
        // Check timeout
        if (activeStage.testTimeout > 0)
        {
            float elapsed = Time.time - startTime;
            if (elapsed >= activeStage.testTimeout)
            {
                Log($"Test timed out after {elapsed:F1}s");
                CompleteTest();
            }
        }
    }
    
    #endregion
    
    #region Event Subscription
    
    private void SubscribeToEvents()
    {
        GameEvents.OnStageStart += HandleStageStart;
        GameEvents.OnStageComplete += HandleStageComplete;
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveStep += HandleWaveStep;
        GameEvents.OnWaveComplete += HandleWaveComplete;
        GameEvents.OnCubeCaptured += HandleCubeCaptured;
        GameEvents.OnCubeEscaped += HandleCubeEscaped;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnMarkerPlaced += HandleMarkerPlaced;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnStageStart -= HandleStageStart;
        GameEvents.OnStageComplete -= HandleStageComplete;
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveStep -= HandleWaveStep;
        GameEvents.OnWaveComplete -= HandleWaveComplete;
        GameEvents.OnCubeCaptured -= HandleCubeCaptured;
        GameEvents.OnCubeEscaped -= HandleCubeEscaped;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnMarkerPlaced -= HandleMarkerPlaced;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        if (stageData == null || !stageData.isTestStage)
        {
            // Not a test stage, ignore
            return;
        }
        
        Log($"Test stage detected: {stageData.stageName}");
        StartTest(stageData);
    }
    
    private void HandleStageComplete(int stageIndex, bool success)
    {
        if (!isRunning) return;
        
        Log($"Stage completed (success: {success})");
        CompleteTest();
    }
    
    private void HandleWaveStart(int waveIndex, WaveData waveData)
    {
        currentWaveIndex = waveIndex;
        currentWaveStepCount = 0;
        executedCommandIndices.Clear(); // Reset for new wave
        
        // Check if this wave has test configuration
        if (waveData != null && waveData.isTestWave)
        {
            activeWave = waveData;
            
            if (!isRunning)
            {
                // Start test from wave-level config
                StartTestFromWave(waveData);
            }
            
            Log($"Test wave {waveIndex} started: {waveData.waveName}");
            ScenarioLogger.Log($"Test wave {waveIndex} started");
            ScenarioLogger.Log($"  Commands: {waveData.testCommands?.Count ?? 0}");
            ScenarioLogger.Log($"  Max steps: {waveData.maxTestSteps}");
        }
        else if (isRunning)
        {
            Log($"Wave {waveIndex} started");
            ScenarioLogger.Log($"Wave {waveIndex} started");
        }
    }
    
    private void StartTestFromWave(WaveData waveData)
    {
        activeWave = waveData;
        startTime = Time.time;
        isRunning = true;
        
        // Reset state
        globalStepCount = 0;
        currentWaveStepCount = 0;
        capturedCubes = 0;
        escapedCubes = 0;
        playerDeaths = 0;
        markersPlaced = 0;
        executedCommandIndices.Clear();
        results.Clear();
        tilesVisited.Clear();
        lastLoggedPosition = new Vector2Int(-999, -999);
        
        // Cache managers
        playerManager = FindFirstObjectByType<PlayerManager>();
        actionManager = FindFirstObjectByType<PlayerActionManager>();
        
        // Start logging
        ScenarioLogger.StartScenario(waveData.waveName);
        ScenarioLogger.Log($"Test Configuration (Wave-Level):");
        ScenarioLogger.Log($"  Commands: {waveData.testCommands?.Count ?? 0}");
        ScenarioLogger.Log($"  Assertions: {waveData.testAssertions?.Count ?? 0}");
        ScenarioLogger.Log($"  Max Steps: {waveData.maxTestSteps}");
        
        Log($"Test started from wave: {waveData.waveName}");
    }
    
    private void HandleWaveStep(int waveIndex, int step)
    {
        if (!isRunning) return;
        
        currentWaveStepCount = step;
        globalStepCount++;
        
        // Track player position
        TrackPlayerPosition();
        
        ScenarioLogger.Log($"Step {step} (global: {globalStepCount})");
        
        // Execute commands for this step
        ExecuteCommandsForStep();
        
        // Check max steps - wave-level takes priority over stage-level
        int maxSteps = GetMaxTestSteps();
        if (maxSteps > 0 && currentWaveStepCount >= maxSteps)
        {
            Log($"Max test steps ({maxSteps}) reached at wave step {currentWaveStepCount}");
            CompleteTest();
        }
    }
    
    private int GetMaxTestSteps()
    {
        // Wave-level max steps takes priority
        if (activeWave != null && activeWave.maxTestSteps > 0)
            return activeWave.maxTestSteps;
        
        // Fall back to stage-level
        if (activeStage != null && activeStage.maxTestSteps > 0)
            return activeStage.maxTestSteps;
        
        return 0;
    }
    
    private void TrackPlayerPosition()
    {
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
        
        if (playerManager != null)
        {
            Vector2Int currentPos = playerManager.currentTilePosition;
            
            // Track unique tiles visited
            if (!tilesVisited.Contains(currentPos))
            {
                tilesVisited.Add(currentPos);
                ScenarioLogger.Log($"  📍 New tile visited: ({currentPos.x}, {currentPos.y}) - Total unique: {tilesVisited.Count}");
            }
            
            // Log position changes
            if (currentPos != lastLoggedPosition)
            {
                ScenarioLogger.Log($"  🚶 Player at: ({currentPos.x}, {currentPos.y})");
                lastLoggedPosition = currentPos;
            }
        }
    }
    
    private void HandleWaveComplete(int waveIndex)
    {
        if (!isRunning) return;
        
        Log($"Wave {waveIndex} completed");
        ScenarioLogger.Log($"Wave {waveIndex} completed");
    }
    
    private void HandleCubeCaptured(Vector2Int pos, CubeType type)
    {
        if (!isRunning) return;
        
        capturedCubes++;
        ScenarioLogger.Log($"Cube captured: {type} at ({pos.x}, {pos.y}) - Total: {capturedCubes}");
    }
    
    private void HandleCubeEscaped(Vector2Int pos, CubeType type)
    {
        if (!isRunning) return;
        
        escapedCubes++;
        ScenarioLogger.Log($"Cube escaped: {type} at ({pos.x}, {pos.y}) - Total: {escapedCubes}");
    }
    
    private void HandlePlayerDeath(Vector2Int pos)
    {
        if (!isRunning) return;
        
        playerDeaths++;
        ScenarioLogger.Log($"Player died at ({pos.x}, {pos.y}) - Total deaths: {playerDeaths}");
    }
    
    private void HandleMarkerPlaced(Vector2Int pos, MarkerType type)
    {
        if (!isRunning) return;
        
        markersPlaced++;
        ScenarioLogger.Log($"Marker placed: {type} at ({pos.x}, {pos.y}) - Total: {markersPlaced}");
    }
    
    #endregion
    
    #region Test Lifecycle
    
    private void StartTest(StageData stageData)
    {
        activeStage = stageData;
        startTime = Time.time;
        isRunning = true;
        
        // Reset state
        globalStepCount = 0;
        currentWaveStepCount = 0;
        currentWaveIndex = 0;
        capturedCubes = 0;
        escapedCubes = 0;
        playerDeaths = 0;
        markersPlaced = 0;
        executedCommandIndices.Clear();
        results.Clear();
        tilesVisited.Clear();
        lastLoggedPosition = new Vector2Int(-999, -999);
        
        // Cache managers
        playerManager = FindFirstObjectByType<PlayerManager>();
        actionManager = FindFirstObjectByType<PlayerActionManager>();
        
        // Start logging
        ScenarioLogger.StartScenario(stageData.stageName);
        ScenarioLogger.Log($"Test Configuration:");
        ScenarioLogger.Log($"  Commands: {stageData.testCommands?.Count ?? 0}");
        ScenarioLogger.Log($"  Assertions: {stageData.testAssertions?.Count ?? 0}");
        ScenarioLogger.Log($"  Max Steps: {stageData.maxTestSteps}");
        ScenarioLogger.Log($"  Timeout: {stageData.testTimeout}s");
        
        Log($"Test started: {stageData.stageName}");
    }
    
    private void CompleteTest()
    {
        if (!isRunning) return;
        
        isRunning = false;
        float elapsed = Time.time - startTime;
        
        // Get test name from wave (priority) or stage
        string testName = activeWave?.waveName ?? activeStage?.stageName ?? "Unknown";
        int maxSteps = GetMaxTestSteps();
        
        Log($"Test completing: {testName}");
        
        // Evaluate assertions
        EvaluateAssertions();
        
        // Calculate result
        bool allPassed = results.Count == 0 || results.TrueForAll(r => r.passed);
        int passedCount = results.FindAll(r => r.passed).Count;
        
        // Log results
        ScenarioLogger.LogSeparator("TEST RESULTS");
        ScenarioLogger.Log($"Test: {testName}");
        ScenarioLogger.Log($"Result: {(allPassed ? "PASSED" : "FAILED")}");
        ScenarioLogger.Log($"Time: {elapsed:F2}s");
        ScenarioLogger.Log($"Steps: {currentWaveStepCount} (max: {maxSteps})");
        ScenarioLogger.Log($"Captures: {capturedCubes} | Escapes: {escapedCubes} | Deaths: {playerDeaths}");
        ScenarioLogger.Log($"Tiles visited: {tilesVisited.Count} unique positions");
        ScenarioLogger.Log($"Assertions: {passedCount}/{results.Count} passed");
        
        foreach (var result in results)
        {
            string icon = result.passed ? "✓" : "✗";
            ScenarioLogger.Log($"  [{icon}] {result.assertion.description}: expected {result.assertion.expectedValue}, got {result.actualValue}");
        }
        
        ScenarioLogger.EndScenario();
        
        // Fire event
        OnTestCompleted?.Invoke(activeStage, allPassed, results);
        
        Log($"Test {(allPassed ? "PASSED" : "FAILED")}: {testName}");
        Debug.Log($"[TestCommandExecutor] {(allPassed ? "✅ PASSED" : "❌ FAILED")}: {testName}");
        
        // Exit play mode if configured (check stage-level setting)
        bool shouldExitPlayMode = activeStage?.exitPlayModeOnComplete ?? false;
        if (shouldExitPlayMode)
        {
#if UNITY_EDITOR
            Log("Exiting play mode...");
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        
        activeStage = null;
        activeWave = null;
    }
    
    #endregion
    
    #region Command Execution
    
    private void ExecuteCommandsForStep()
    {
        // Get commands from wave (priority) or stage
        var commands = GetActiveCommands();
        if (commands == null || commands.Count == 0) return;
        
        for (int i = 0; i < commands.Count; i++)
        {
            if (executedCommandIndices.Contains(i)) continue;
            
            var cmd = commands[i];
            // Wave-level commands use wave step count (not global)
            int targetStep = cmd.useGlobalStep ? globalStepCount : currentWaveStepCount;
            
            if (cmd.executeOnStep == targetStep)
            {
                ExecuteCommand(cmd);
                executedCommandIndices.Add(i);
            }
        }
    }
    
    private List<TestCommand> GetActiveCommands()
    {
        // Wave-level commands take priority
        if (activeWave != null && activeWave.testCommands != null && activeWave.testCommands.Count > 0)
            return activeWave.testCommands;
        
        // Fall back to stage-level
        if (activeStage != null && activeStage.testCommands != null)
            return activeStage.testCommands;
        
        return null;
    }
    
    private List<TestAssertion> GetActiveAssertions()
    {
        // Wave-level assertions take priority
        if (activeWave != null && activeWave.testAssertions != null && activeWave.testAssertions.Count > 0)
            return activeWave.testAssertions;
        
        // Fall back to stage-level
        if (activeStage != null && activeStage.testAssertions != null)
            return activeStage.testAssertions;
        
        return null;
    }
    
    private void ExecuteCommand(TestCommand cmd)
    {
        Log($"Executing: {cmd.commandType} - {cmd.description}");
        ScenarioLogger.Log($"⚡ Command: {cmd.commandType} - {cmd.description}");
        
        switch (cmd.commandType)
        {
            case TestCommandType.Move:
                ExecuteMoveCommand(cmd);
                break;
                
            case TestCommandType.PlaceMarker:
                ExecutePlaceMarkerCommand(cmd);
                break;
                
            case TestCommandType.Wait:
                // Do nothing - just for timing
                break;
        }
    }
    
    private void ExecuteMoveCommand(TestCommand cmd)
    {
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
        
        if (playerManager != null)
        {
            // Check if player is dead (respawning)
            if (playerManager.isDead)
            {
                Log($"  ⚠️ Player is dead, skipping move command");
                ScenarioLogger.LogWarning($"Player dead, skipped move to ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
                return;
            }
            
            Vector2Int fromPos = playerManager.currentTilePosition;
            
            // Use SetPosition for instant/reliable positioning in tests
            // Note: MoveTo() uses physics movement which is too slow for test command timing
            // Future: Could add a "wait for arrival" coroutine if visual movement is needed
            playerManager.SetPosition(cmd.targetPosition.x, cmd.targetPosition.y);
            
            Log($"  → Player moved: ({fromPos.x}, {fromPos.y}) → ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
            ScenarioLogger.Log($"  🎯 Player moved: ({fromPos.x}, {fromPos.y}) → ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
        }
        else
        {
            Log($"  ❌ PlayerManager not found");
            ScenarioLogger.LogError("PlayerManager not found for move command");
        }
    }
    
    private void ExecutePlaceMarkerCommand(TestCommand cmd)
    {
        if (actionManager == null)
        {
            actionManager = FindFirstObjectByType<PlayerActionManager>();
        }
        
        if (actionManager != null)
        {
            bool success = false;
            
            switch (cmd.markerType)
            {
                case MarkerType.Unit:
                    success = actionManager.PlaceUnitMarker(cmd.targetPosition);
                    break;
                case MarkerType.Recursion:
                    success = actionManager.PlaceRecursionMarker(cmd.targetPosition);
                    break;
                case MarkerType.Matrix:
                    success = actionManager.PlaceMatrixMarker(cmd.targetPosition, 3);
                    break;
                case MarkerType.Cube:
                    Log($"  ⚠️ Cube markers are auto-generated, cannot place manually");
                    ScenarioLogger.LogWarning("Attempted to place Cube marker manually");
                    return;
            }
            
            if (success)
            {
                Log($"  → Placed {cmd.markerType} marker at ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
            }
            else
            {
                Log($"  ⚠️ Failed to place {cmd.markerType} marker at ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
                ScenarioLogger.LogWarning($"Failed to place {cmd.markerType} marker at ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
            }
        }
        else
        {
            Log($"  ❌ PlayerActionManager not found");
            ScenarioLogger.LogError("PlayerActionManager not found for marker command");
        }
    }
    
    #endregion
    
    #region Assertion Evaluation
    
    private void EvaluateAssertions()
    {
        var assertions = GetActiveAssertions();
        if (assertions == null || assertions.Count == 0) return;
        
        foreach (var assertion in assertions)
        {
            int actual = GetMetricValue(assertion.metric);
            bool passed = assertion.Evaluate(actual);
            
            results.Add(new AssertionResult
            {
                assertion = assertion,
                actualValue = actual,
                passed = passed
            });
            
            string icon = passed ? "✅" : "❌";
            Log($"{icon} Assertion: {assertion.description} - expected {assertion.expectedValue}, got {actual}");
        }
    }
    
    private int GetMetricValue(TestMetric metric)
    {
        return metric switch
        {
            TestMetric.CapturedCubes => capturedCubes,
            TestMetric.EscapedCubes => escapedCubes,
            TestMetric.PlayerDeaths => playerDeaths,
            TestMetric.WaveSteps => currentWaveStepCount,
            TestMetric.MarkersPlaced => markersPlaced,
            TestMetric.GlobalSteps => globalStepCount,
            TestMetric.TilesVisited => tilesVisited.Count,
            _ => 0
        };
    }
    
    #endregion
    
    #region Logging
    
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TestCommandExecutor] {message}");
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Check if a test is currently running.
    /// </summary>
    public bool IsTestRunning => isRunning;
    
    /// <summary>
    /// Get the currently active test stage.
    /// </summary>
    public StageData ActiveTest => activeStage;
    
    /// <summary>
    /// Force complete the current test.
    /// </summary>
    public void ForceCompleteTest()
    {
        if (isRunning)
        {
            Log("Force completing test");
            CompleteTest();
        }
    }
    
    #endregion
    
    #region Result Type
    
    public class AssertionResult
    {
        public TestAssertion assertion;
        public int actualValue;
        public bool passed;
    }
    
    #endregion
}
