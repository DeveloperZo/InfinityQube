using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Executes scenario commands and tracks metrics for assertion evaluation.
/// Subscribes to game events to track captures, escapes, deaths, etc.
/// </summary>
public class ScenarioRunner : MonoBehaviour
{
    #region Singleton
    
    public static ScenarioRunner Instance { get; private set; }
    
    #endregion
    
    #region State
    
    private ScenarioData activeScenario;
    private float startTime;
    private int waveSteps;
    private bool isRunning;
    
    // Command execution
    private HashSet<int> executedCommandIndices = new HashSet<int>();
    private PlayerManager playerManager;
    private PlayerActionManager actionManager;
    
    // Tracked metrics
    private int capturedCubes;
    private int escapedCubes;
    private int playerDeaths;
    private int markersPlaced;
    
    // Results
    private List<AssertionResult> results = new List<AssertionResult>();
    
    #endregion
    
    #region Events
    
    public static event System.Action<ScenarioData, bool, List<AssertionResult>> OnScenarioCompleted;
    
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
        }
    }
    
    private void Start()
    {
        SubscribeToEvents();
        
        // Subscribe to scenario loader
        if (ScenarioLoader.Instance != null)
        {
            ScenarioLoader.Instance.OnScenarioLoaded += OnScenarioLoaded;
        }
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        if (ScenarioLoader.Instance != null)
        {
            ScenarioLoader.Instance.OnScenarioLoaded -= OnScenarioLoaded;
        }
    }
    
    private void Update()
    {
        if (!isRunning || activeScenario == null) return;
        
        // Check timeout
        float elapsed = Time.time - startTime;
        if (elapsed >= activeScenario.timeoutSeconds)
        {
            Log($"Scenario timed out after {elapsed:F1}s");
            CompleteScenario();
        }
        
        // Check max steps
        if (waveSteps >= activeScenario.maxWaveSteps)
        {
            Log($"Max wave steps ({activeScenario.maxWaveSteps}) reached");
            CompleteScenario();
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Start running a scenario (called when scene loads)
    /// </summary>
    public void StartScenario(ScenarioData scenario)
    {
        if (scenario == null) return;
        
        activeScenario = scenario;
        startTime = Time.time;
        waveSteps = 0;
        isRunning = true;
        
        // Reset metrics
        capturedCubes = 0;
        escapedCubes = 0;
        playerDeaths = 0;
        markersPlaced = 0;
        results.Clear();
        executedCommandIndices.Clear();
        
        // Cache managers
        playerManager = FindFirstObjectByType<PlayerManager>();
        actionManager = FindFirstObjectByType<PlayerActionManager>();
        
        Log($"Started scenario: {scenario.scenarioName}");
        Log($"  Commands: {scenario.commands?.Count ?? 0}");
        Log($"  End condition: {scenario.endCondition}");
        Log($"  Timeout: {scenario.timeoutSeconds}s");
        Log($"  Max steps: {scenario.maxWaveSteps}");
        Log($"  Assertions: {scenario.assertions?.Count ?? 0}");
        
        ScenarioLogger.Log($"Started scenario: {scenario.scenarioName}");
        ScenarioLogger.Log($"  End condition: {scenario.endCondition}");
        
        // Execute step 0 commands immediately
        ExecuteCommandsForStep(0);
    }
    
    /// <summary>
    /// Force complete the scenario
    /// </summary>
    public void CompleteScenario()
    {
        if (!isRunning) return;
        
        isRunning = false;
        float elapsed = Time.time - startTime;
        
        Log($"Scenario complete: {activeScenario.scenarioName}");
        Log($"  Time: {elapsed:F2}s, Steps: {waveSteps}");
        Log($"  Captures: {capturedCubes}, Escapes: {escapedCubes}, Deaths: {playerDeaths}");
        
        // Evaluate assertions
        EvaluateAssertions();
        
        // Log results
        bool allPassed = results.TrueForAll(r => r.passed);
        LogResults(allPassed, elapsed);
        
        // Fire event
        OnScenarioCompleted?.Invoke(activeScenario, allPassed, results);
        
        // End logging
        ScenarioLogger.EndScenario();
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnScenarioLoaded(ScenarioData scenario)
    {
        StartScenario(scenario);
    }
    
    private void OnCubeCaptured(Vector2Int pos, CubeType type)
    {
        capturedCubes++;
        Log($"Cube captured (total: {capturedCubes})");
        CheckEndCondition();
    }
    
    private void OnCubeEscaped(Vector2Int pos, CubeType type)
    {
        escapedCubes++;
        Log($"Cube escaped (total: {escapedCubes})");
        CheckEndCondition();
    }
    
    private void OnPlayerDied(Vector2Int pos)
    {
        playerDeaths++;
        Log($"Player died (total: {playerDeaths})");
        
        if (activeScenario?.endCondition == ScenarioEndCondition.PlayerDeath)
        {
            CompleteScenario();
        }
    }
    
    private void OnMarkerPlaced(Vector2Int position, MarkerType type)
    {
        markersPlaced++;
    }
    
    private void OnWaveStep(int waveIndex, int step)
    {
        waveSteps = step;
        ScenarioLogger.Log($"Wave step {step}");
        
        // Execute commands for this step
        ExecuteCommandsForStep(step);
    }
    
    private void OnWaveComplete(int waveIndex)
    {
        if (activeScenario?.endCondition == ScenarioEndCondition.WaveComplete)
        {
            CompleteScenario();
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void SubscribeToEvents()
    {
        GameEvents.OnCubeCaptured += OnCubeCaptured;
        GameEvents.OnCubeEscaped += OnCubeEscaped;
        GameEvents.OnPlayerDeath += OnPlayerDied;
        GameEvents.OnMarkerPlaced += OnMarkerPlaced;
        GameEvents.OnWaveStep += OnWaveStep;
        GameEvents.OnWaveComplete += OnWaveComplete;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnCubeCaptured -= OnCubeCaptured;
        GameEvents.OnCubeEscaped -= OnCubeEscaped;
        GameEvents.OnPlayerDeath -= OnPlayerDied;
        GameEvents.OnMarkerPlaced -= OnMarkerPlaced;
        GameEvents.OnWaveStep -= OnWaveStep;
        GameEvents.OnWaveComplete -= OnWaveComplete;
    }
    
    private void CheckEndCondition()
    {
        if (!isRunning || activeScenario == null) return;
        
        if (activeScenario.endCondition == ScenarioEndCondition.AllCubesResolved)
        {
            // Check if all cubes are resolved (need to check WaveManager)
            var waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null && waveManager.activeCubes.Count == 0 && waveSteps > 0)
            {
                CompleteScenario();
            }
        }
    }
    
    #endregion
    
    #region Command Execution
    
    private void ExecuteCommandsForStep(int step)
    {
        if (activeScenario?.commands == null) return;
        
        for (int i = 0; i < activeScenario.commands.Count; i++)
        {
            var cmd = activeScenario.commands[i];
            if (cmd.executeOnStep == step && !executedCommandIndices.Contains(i))
            {
                ExecuteCommand(cmd);
                executedCommandIndices.Add(i);
            }
        }
    }
    
    private void ExecuteCommand(ScenarioCommand cmd)
    {
        Log($"⚡ Executing: {cmd.type} - {cmd.description}");
        ScenarioLogger.Log($"⚡ Command: {cmd.type} - {cmd.description}");
        
        switch (cmd.type)
        {
            case CommandType.Move:
                ExecuteMoveCommand(cmd);
                break;
                
            case CommandType.PlaceMarker:
                ExecutePlaceMarkerCommand(cmd);
                break;
                
            case CommandType.Wait:
                // Do nothing - just for timing
                break;
        }
    }
    
    private void ExecuteMoveCommand(ScenarioCommand cmd)
    {
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
        
        if (playerManager != null)
        {
            // Use MoveTo for realistic movement with animations
            playerManager.MoveTo(cmd.targetPosition.x, cmd.targetPosition.y);
            Log($"  → Player moving to ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
        }
        else
        {
            Log($"  ❌ PlayerManager not found");
        }
    }
    
    private void ExecutePlaceMarkerCommand(ScenarioCommand cmd)
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
                    // Cube markers are auto-generated, can't be placed manually
                    Log($"  ⚠️ Cube markers cannot be placed manually");
                    break;
            }
            
            if (success)
                Log($"  → Placed {cmd.markerType} marker at ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
            else
                Log($"  ❌ Failed to place {cmd.markerType} marker at ({cmd.targetPosition.x}, {cmd.targetPosition.y})");
        }
        else
        {
            Log($"  ❌ PlayerActionManager not found");
        }
    }
    
    private void EvaluateAssertions()
    {
        if (activeScenario == null) return;
        
        foreach (var assertion in activeScenario.assertions)
        {
            int actual = GetMetricValue(assertion.type);
            bool passed = assertion.Evaluate(actual);
            
            results.Add(new AssertionResult
            {
                assertion = assertion,
                actualValue = actual,
                passed = passed
            });
            
            string icon = passed ? "✅" : "❌";
            Log($"{icon} {assertion.description}: expected {assertion.expectedValue}, got {actual}");
            ScenarioLogger.LogAssertion(assertion.description, passed, assertion.expectedValue, actual, waveSteps);
        }
    }
    
    private int GetMetricValue(AssertionType type)
    {
        return type switch
        {
            AssertionType.CapturedCubes => capturedCubes,
            AssertionType.EscapedCubes => escapedCubes,
            AssertionType.PlayerDeaths => playerDeaths,
            AssertionType.WaveSteps => waveSteps,
            AssertionType.MarkersPlaced => markersPlaced,
            _ => 0
        };
    }
    
    private void LogResults(bool allPassed, float elapsed)
    {
        int passedCount = results.FindAll(r => r.passed).Count;
        
        ScenarioLogger.LogSeparator("RESULTS");
        ScenarioLogger.Log($"Scenario: {activeScenario.scenarioName}");
        ScenarioLogger.Log($"Result: {(allPassed ? "PASSED" : "FAILED")}");
        ScenarioLogger.Log($"Time: {elapsed:F2}s | Steps: {waveSteps}");
        ScenarioLogger.Log($"Captures: {capturedCubes} | Escapes: {escapedCubes} | Deaths: {playerDeaths}");
        ScenarioLogger.Log($"Assertions: {passedCount}/{results.Count} passed");
        
        Debug.Log($"[ScenarioRunner] {(allPassed ? "✅ PASSED" : "❌ FAILED")}: {activeScenario.scenarioName}");
    }
    
    private void Log(string message)
    {
        Debug.Log($"[ScenarioRunner] {message}");
        ScenarioLogger.Log(message);
    }
    
    #endregion
    
    #region Result Type
    
    public class AssertionResult
    {
        public ScenarioAssertion assertion;
        public int actualValue;
        public bool passed;
    }
    
    #endregion
}
