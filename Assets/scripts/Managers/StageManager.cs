using UnityEngine;
using UnityEngine.SceneManagement;
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

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
        InitializeStageDatabase();
        EnableDebugLogs = true;
    }

    private void Start()
    {
        // Subscribe to wave manager events
        if (waveManager != null)
        {
            // Subscribe to private UnityEvents via reflection or add public accessors
            SubscribeToWaveEvents();
        }

        if (startingStageIndex != -1)
        {
            LoadStage(startingStageIndex);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from wave manager events
        if (waveManager != null)
        {
            UnsubscribeFromWaveEvents();
        }
    }

    private void SubscribeToWaveEvents()
    {
        if (waveManager.OnWaveComplete == null)
            waveManager.OnWaveComplete = new UnityEngine.Events.UnityEvent<int>();
        if (waveManager.OnWaveFailed == null)
            waveManager.OnWaveFailed = new UnityEngine.Events.UnityEvent<int>();
        if (waveManager.OnAllWavesComplete == null)
            waveManager.OnAllWavesComplete = new UnityEngine.Events.UnityEvent();

        waveManager.OnWaveComplete.AddListener(OnWaveCompleted);
        waveManager.OnWaveFailed.AddListener(OnWaveFailed);
        waveManager.OnAllWavesComplete.AddListener(OnAllWavesCompleted);

        DebugLog("Subscribed to WaveManager events");
    }

    private void UnsubscribeFromWaveEvents()
    {
        if (waveManager.OnWaveComplete != null)
            waveManager.OnWaveComplete.RemoveListener(OnWaveCompleted);
        if (waveManager.OnWaveFailed != null)
            waveManager.OnWaveFailed.RemoveListener(OnWaveFailed);
        if (waveManager.OnAllWavesComplete != null)
            waveManager.OnAllWavesComplete.RemoveListener(OnAllWavesCompleted);

        DebugLog("Unsubscribed from WaveManager events");
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
                this.LogError("StageDatabase not found in Resources!");
                stageDatabase = CreateFallbackDatabase();
            }
        }

        stageDatabase.Initialize();
        this.Log("Stage database initialized", EnableDebugLogs);
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
            this.LogError($"Stage {stageNumber} not found!");
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

        
        // Fire GameEvents
        GameEvents.FireStageStart(stageNumber, stage);
        DebugLog($"Fired GameEvents.OnStageStart for stage {stageNumber}");

        DebugLog($"Stage {stageNumber}: '{stage.stageName}' loaded successfully (Attempt #{stageAttempts[stageNumber]})");

        // Start the first wave via event system (no direct call)
        if (waveManager != null)
        {
            yield return new WaitForSeconds(0.1f); // Brief delay to ensure everything is ready
            waveManager.ResetToFirstWave(); // Ensure we start from wave 0
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

        // Note: Escape-based failure is now handled at Wave level
        // Waves will trigger OnWaveFailed event if their escape limits are exceeded

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


        // Fire GameEvents
        GameEvents.FireStageComplete(CurrentStageIndex, success);
        DebugLog($"Fired GameEvents.OnStageComplete for stage {CurrentStageIndex}, success: {success}");

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

        // Check if this is Stage 1 (CurrentStageIndex == 0) - Demo completion
        if (CurrentStageIndex == 0)
        {
            DebugLog("Demo completed! Showing completion message and returning to splash screen.");
            
            // Show demo completion message
            if (waveManager != null)
            {
                var completionMessage = new WaveMessage
                {
                    Message = "Congratulations!\n\nYou've completed the InfinityQube demo!\n\nThank you for playing!",
                    RequirePause = true,
                    AutoHideDelay = 0f
                };
                
                waveManager.ShowMessage(completionMessage);
                
                // Wait for message to be dismissed
                yield return new WaitForSeconds(1f);
            }
            
            // Transition back to Splash scene
            DebugLog("Transitioning back to Splash scene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Splash");
        }
        else
        {
            // Normal stage progression for non-demo stages
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
    }

    private IEnumerator HandleStageFailure()
    {
        yield return new WaitForSeconds(stageTransitionDelay);

        if (restartOnFailure)
        {

            // Fire GameEvents
            GameEvents.FireStageRestart(CurrentStageIndex);
            DebugLog($"Fired GameEvents.OnStageRestart for stage {CurrentStageIndex}");
            
            RestartCurrentStage();
        }
    }
    #endregion

    #region Debug Interface

    private void DebugLog(string message)
    {
        this.Log(message, EnableDebugLogs);
    }
    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs { get; set; } = true;

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
        
        this.Log("Reset to defaults completed", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for stage settings
        this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for stage settings
        this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion

    #region Wave Event Handlers
    /// <summary>
    /// Event handler for when a wave completes successfully.
    /// This method replaces the direct call pattern to avoid circular dependencies.
    /// Flow: WaveManager.CompleteWave() → OnWaveComplete event → this method
    /// </summary>
    /// <param name="waveIndex">Index of the completed wave</param>
    private void OnWaveCompleted(int waveIndex)
    {
        DebugLog($"🎯 Wave {waveIndex} completed via event");
        
        if (!IsStageInProgress) return;

        // Check if there are more waves in this stage
        if (waveManager != null && waveManager.HasMoreWaves())
        {
            DebugLog("Starting next wave...");
            StartCoroutine(DelayedNextWave());
        }
        else
        {
            DebugLog("All waves completed, checking stage completion...");
            CheckStageCompletion();
        }
    }

    private void OnWaveFailed(int waveIndex)
    {
        DebugLog($"❌ Wave {waveIndex} failed via event");
        
        if (!IsStageInProgress) return;

        // Handle wave failure - could restart wave or fail stage
        CompleteStage(false);
    }

    private void OnAllWavesCompleted()
    {
        DebugLog("🏁 All waves completed via event");
        
        if (!IsStageInProgress) return;

        CheckStageCompletion();
    }

    private IEnumerator DelayedNextWave()
    {
        yield return new WaitForSeconds(stageTransitionDelay);
        if (waveManager != null)
        {
            waveManager.StartNextWave();
        }
    }
    #endregion

    /// <summary>
    /// Shows feedback to the player when a cube escapes.
    /// Uses the existing WaveManager message system for consistent UI.
    /// </summary>
    /// <param name="cubeType">Type of cube that escaped</param>
    private void ShowEscapeFeedback(Enumerations.CubeType cubeType)
    {
        if (waveManager == null || !waveManager.showMessages) return;

        int remaining = CurrentStage.maxAllowedEscapes - escapedCubeCount;
        string feedbackMessage = "";

        if (remaining > 0)
        {
            feedbackMessage = $"⚠️ Cube Escaped!\n{cubeType} cube fell off the grid.\n\nEscapes: {escapedCubeCount}/{CurrentStage.maxAllowedEscapes}\nRemaining: {remaining}";
        }
        else
        {
            feedbackMessage = $"🚨 STAGE FAILED!\nToo many cubes escaped!\n\nFinal count: {escapedCubeCount}/{CurrentStage.maxAllowedEscapes}";
        }

        var escapeMessage = new WaveMessage
        {
            Message = feedbackMessage,

        };

        waveManager.ShowMessage(escapeMessage);
        DebugLog($"📢 Escape feedback shown: {feedbackMessage.Replace('\n', ' ')}");
    }
}