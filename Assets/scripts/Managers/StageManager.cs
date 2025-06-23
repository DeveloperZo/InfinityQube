using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class StageManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerController;
    [SerializeField] private PlayerActionManager playerActionManager;

    [Header("Stage Database")]
    [SerializeField] private StageDB stageDatabase;
    [SerializeField] private int startingStageIndex = 0; // Tutorial stage

    [Header("Stage Flow")]
    [SerializeField] private bool autoAdvanceStages = true;
    [SerializeField] private float stageTransitionDelay = 2f;
    [SerializeField] private bool restartOnFailure = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showStageInfo = false;
    #endregion

    #region Runtime State
    // Current Stage
    public int CurrentStageIndex { get; private set; }
    public StageData CurrentStage { get; private set; }
    public bool IsStageInProgress { get; private set; }

    // Stage Statistics
    private int capturedCubeCount = 0;
    private int escapedCubeCount = 0;
    private float stageStartTime = 0f;

    // Stage History for debugging
    private List<int> completedStages = new List<int>();
    private Dictionary<int, int> stageAttempts = new Dictionary<int, int>();
    #endregion

    #region Events
    public event Action<StageData> OnStageStarted;
    public event Action<StageData, bool> OnStageCompleted; // stage, success
    public event Action<StageData> OnStageRestarted;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
        InitializeStageDatabase();
    }

    private void Start()
    {
        if (startingStageIndex != -1)
        {
            LoadStage(startingStageIndex);
        }
    }

    #endregion

    #region Initialization
    private void FindReferences()
    {
        if (gridManager == null) gridManager = GridManager.Instance;
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerManager>();
        if (playerActionManager == null) playerActionManager = FindObjectOfType<PlayerActionManager>();
    }

    private void InitializeStageDatabase()
    {
        if (stageDatabase == null)
        {
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
            if (stageDatabase == null)
            {
                Debug.LogError("StageDatabase not found in Resources!");
                stageDatabase = CreateFallbackDatabase();
            }
        }

        stageDatabase.Initialize();
        DebugLog("Stage database initialized");
    }

    private StageDB CreateFallbackDatabase()
    {
        StageDB fallback = ScriptableObject.CreateInstance<StageDB>();
        fallback.Initialize();
        return fallback;
    }
    #endregion

    #region Public Interface - For Debuggers
    public void LoadStage(int stageNumber)
    {
        StartCoroutine(LoadStageCoroutine(stageNumber));
    }

    public void RestartCurrentStage()
    {
        if (CurrentStage != null)
        {
            DebugLog($"Restarting stage {CurrentStageIndex}");
            LoadStage(CurrentStageIndex);
        }
    }

    public void LoadNextStage()
    {
        LoadStage(CurrentStageIndex + 1);
    }

    public void LoadPreviousStage()
    {
        if (CurrentStageIndex > -1)
        {
            LoadStage(CurrentStageIndex - 1);
        }
    }

    public void ResetToFirstStage()
    {
        LoadStage(startingStageIndex);
    }

    public void ForceCompleteStage(bool success = true)
    {
        if (IsStageInProgress)
        {
            DebugLog($"Force completing stage {CurrentStageIndex} with success: {success}");
            CompleteStage(success);
        }
    }

    public List<int> GetAvailableStages()
    {
        return stageDatabase.GetAllStageIds();
    }

    public Dictionary<int, int> GetStageAttempts()
    {
        return new Dictionary<int, int>(stageAttempts);
    }
    #endregion

    #region Stage Loading
    private IEnumerator LoadStageCoroutine(int stageNumber)
    {
        DebugLog($"Loading Stage {stageNumber}...");

        StageData stage = stageDatabase.GetStage(stageNumber);
        if (stage == null)
        {
            Debug.LogError($"Stage {stageNumber} not found!");
            yield break;
        }

        // Clean up previous stage
        CleanupCurrentStage();

        // Reset state
        IsStageInProgress = false;
        capturedCubeCount = 0;
        escapedCubeCount = 0;
        stageStartTime = Time.time;

        // Store stage info
        CurrentStageIndex = stageNumber;
        CurrentStage = stage;

        // Track attempts
        if (!stageAttempts.ContainsKey(stageNumber))
            stageAttempts[stageNumber] = 0;
        stageAttempts[stageNumber]++;

        // Configure systems
        yield return StartCoroutine(ConfigureForStage(stage));

        // Mark stage as started
        IsStageInProgress = true;

        // Fire events
        OnStageStarted?.Invoke(stage);

        DebugLog($"Stage {stageNumber}: '{stage.stageName}' loaded successfully (Attempt #{stageAttempts[stageNumber]})");

        // Start the first wave
        if (waveManager != null)
        {
            yield return new WaitForSeconds(0.1f); // Brief delay to ensure everything is ready
            waveManager.StartWave();
        }
    }

    private IEnumerator ConfigureForStage(StageData stage)
    {
        // Configure grid
        yield return StartCoroutine(ConfigureGrid(stage));

        // Configure wave manager
        ConfigureWaveManager(stage);

        // Configure player
        ConfigurePlayer(stage);

        // Configure detonation manager
        ConfigureDetonationManager(stage);
    }

    private IEnumerator ConfigureGrid(StageData stage)
    {
        if (gridManager == null) yield break;

        DebugLog($"Configuring grid: {stage.gridWidth}x{stage.gridHeight}");

        if (gridManager.Width != stage.gridWidth || gridManager.Height != stage.gridHeight)
        {
            gridManager.ResizeGrid(stage.gridWidth, stage.gridHeight);

            while (!gridManager.IsGridReady)
            {
                yield return null;
            }
        }

        gridManager.ClearAllMarkers();
    }

    private void ConfigureWaveManager(StageData stage)
    {
        if (waveManager == null || stage.waveConfigurations.Count == 0) return;

        DebugLog($"Configuring {stage.waveConfigurations.Count} waves");


        waveManager.waveConfiguration = stage.waveConfigurations;
        waveManager.useWaveConfiguration = true;
        waveManager.currentWaveIndex = 0; // Reset to first wave
    }

    private void ConfigurePlayer(StageData stage)
    {
        if (playerController == null) return;

        DebugLog($"Setting player start position: ({stage.playerStartPosition.x}, {stage.playerStartPosition.y})");
        playerController.SetPosition(stage.playerStartPosition.x, stage.playerStartPosition.y);
        playerController.ResetStatistics();
    }

    private void ConfigureDetonationManager(StageData stage)
    {
        if (playerActionManager == null) return;

        playerActionManager.ClearAllActions();
    }


    private void CleanupCurrentStage()
    {
        if (waveManager != null)
        {
            waveManager.StopWave();
        }

        if (playerActionManager != null)
        {
            playerActionManager.ClearAllActions();
        }
    }
    #endregion

    #region Stage Completion Logic
    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        if (!IsStageInProgress) return;

        capturedCubeCount++;
        DebugLog($"Cube captured: {cubeType}. Total: {capturedCubeCount}");

        CheckStageCompletion();
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
        if (!IsStageInProgress) return;

        escapedCubeCount++;
        DebugLog($"Cube escaped: {cubeType}. Total escapes: {escapedCubeCount}");

        CheckStageCompletion();
    }

    public void OnWaveCompleted()
    {
        if (!IsStageInProgress) return;

        DebugLog("Wave completed, checking stage completion...");
        CheckStageCompletion();
    }

    private void CheckStageCompletion()
    {
        if (CurrentStage == null) return;

        bool success = false;
        bool failure = false;

        // Check success conditions
        if (CurrentStage.requiredCaptureCount > 0 && capturedCubeCount >= CurrentStage.requiredCaptureCount)
        {
            success = true;
        }

        // Check failure conditions
        if (CurrentStage.maxAllowedEscapes >= 0 && escapedCubeCount > CurrentStage.maxAllowedEscapes)
        {
            failure = true;
        }

        if (success && !failure)
        {
            CompleteStage(true);
        }
        else if (failure)
        {
            CompleteStage(false);
        }
    }

    private void CompleteStage(bool success)
    {
        if (!IsStageInProgress) return;

        IsStageInProgress = false;
        float completionTime = Time.time - stageStartTime;

        string result = success ? "SUCCESS" : "FAILED";
        DebugLog($"Stage {CurrentStageIndex} completed: {result} (Time: {completionTime:F1}s)");

        if (success && !completedStages.Contains(CurrentStageIndex))
        {
            completedStages.Add(CurrentStageIndex);
        }

        // Fire events
        OnStageCompleted?.Invoke(CurrentStage, success);

        if (success)
        {
            StartCoroutine(HandleStageSuccess());
        }
        else
        {
            StartCoroutine(HandleStageFailure());
        }
    }

    private IEnumerator HandleStageSuccess()
    {
        yield return new WaitForSeconds(stageTransitionDelay);

        if (autoAdvanceStages)
        {
            int nextStage = CurrentStageIndex + 1;
            if (stageDatabase.GetStage(nextStage) != null)
            {
                LoadStage(nextStage);
            }
            else
            {
                DebugLog("All stages completed!");
            }
        }
    }

    private IEnumerator HandleStageFailure()
    {
        yield return new WaitForSeconds(stageTransitionDelay);

        if (restartOnFailure)
        {
            OnStageRestarted?.Invoke(CurrentStage);
            RestartCurrentStage();
        }
    }
    #endregion

    #region Debug Interface

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[StageManager] {message}");
    }
    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        string stageName = CurrentStage?.stageName ?? "None";
        string status = IsStageInProgress ? "IN_PROGRESS" : "IDLE";
        return $"Stage {CurrentStageIndex}: {stageName} ({status}) Captured:{capturedCubeCount} Escaped:{escapedCubeCount}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Current Stage Index"] = CurrentStageIndex,
            ["Current Stage Name"] = CurrentStage?.stageName ?? "None",
            ["Is Stage In Progress"] = IsStageInProgress,
            ["Stage Start Time"] = stageStartTime,
            ["Captured Cube Count"] = capturedCubeCount,
            ["Escaped Cube Count"] = escapedCubeCount,
            ["Auto Advance Stages"] = autoAdvanceStages,
            ["Restart On Failure"] = restartOnFailure,
            ["Stage Transition Delay"] = stageTransitionDelay,
            ["Completed Stages"] = string.Join(", ", completedStages),
            ["Available Stages"] = GetAvailableStages().Count,
            ["Stage Attempts"] = stageAttempts.Count,
            ["Current Stage Grid Size"] = CurrentStage != null ? $"{CurrentStage.gridWidth}x{CurrentStage.gridHeight}" : "N/A",
            ["Current Stage Player Start"] = CurrentStage?.playerStartPosition.ToString() ?? "N/A",
            ["Current Stage Waves"] = CurrentStage?.waveConfigurations?.Count ?? 0,
            ["Required Capture Count"] = CurrentStage?.requiredCaptureCount ?? 0,
            ["Max Allowed Escapes"] = CurrentStage?.maxAllowedEscapes ?? 0
        };
    }

    public void ResetToDefaults()
    {
        // Stop any current stage
        if (IsStageInProgress)
        {
            CleanupCurrentStage();
        }
        
        // Reset to starting stage
        CurrentStageIndex = startingStageIndex;
        CurrentStage = null;
        IsStageInProgress = false;
        
        // Reset statistics
        capturedCubeCount = 0;
        escapedCubeCount = 0;
        stageStartTime = 0f;
        
        // Clear stage history
        completedStages.Clear();
        stageAttempts.Clear();
        
        // Load starting stage if valid
        if (startingStageIndex >= 0)
        {
            LoadStage(startingStageIndex);
        }
        
        if (EnableDebugLogs)
            Debug.Log("[StageManager] Reset to defaults completed");
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for stage settings
        if (EnableDebugLogs)
            Debug.Log($"[StageManager] Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for stage settings
        if (EnableDebugLogs)
            Debug.Log($"[StageManager] Saving configuration: {configName} (not yet implemented)");
    }

    #endregion
}