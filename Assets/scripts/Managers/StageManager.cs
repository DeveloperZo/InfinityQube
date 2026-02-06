using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using static Enumerations;

public class StageManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerController;
    [SerializeField] private PlayerActionManager playerActionManager;
    [SerializeField] private GameUI gameUI;

    [Header("Stage Database")]
    [SerializeField] private StageDB stageDatabase;
    [SerializeField] public int startingStageIndex = 0; // Tutorial stage

    [Header("Stage Flow")]
    [SerializeField] private bool autoAdvanceStages = true;
    [SerializeField] private bool autoAdvanceWaves = true;
    [SerializeField] private float stageTransitionDelay = 2f;
    [SerializeField] private bool restartOnFailure = true;

    [Header("Prototyping")]
    [Tooltip("When true, stages will not auto-start. Use Prototyping window to manually start.")]
    [SerializeField] private bool disableAutoStart = true;

    [Header("Debug")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    [SerializeField] private bool showStageInfo = false;
    #endregion

    #region Runtime State
    // Current Stage
    public int CurrentStageIndex { get; private set; }
    public StageData CurrentStage { get; private set; }
    public bool IsStageInProgress { get; private set; }
    
    /// <summary>
    /// Returns true if the current stage is a tutorial stage (Stage 0 or StageType.Tutorial).
    /// Use this instead of checking CurrentStageIndex == 0 directly.
    /// </summary>
    public bool IsTutorialStage => CurrentStageIndex == 0 || CurrentStage?.stageType == StageType.Tutorial;
    
    /// <summary>
    /// Returns true if the current stage is the Hub (no waves, player interacts with fixed cubes).
    /// </summary>
    public bool IsHubStage => CurrentStage?.stageType == StageType.Hub;

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
        
    }

    private void Start()
    {
        // Subscribe to wave manager events
        if (waveManager != null)
        {
            // Subscribe to private UnityEvents via reflection or add public accessors
            SubscribeToWaveEvents();
        }

        // Check if a stage was selected from Hub (highest priority)
        if (PlayerPrefs.HasKey("SelectedStage"))
        {
            int selectedStage = PlayerPrefs.GetInt("SelectedStage", 0);
            PlayerPrefs.DeleteKey("SelectedStage");
            PlayerPrefs.Save();
            DebugLog($"SelectedStage flag detected from Hub, starting stage {selectedStage}");
            LoadStage(selectedStage);
            return;
        }

        // Check if tutorial should be started from Splash screen (bypasses disableAutoStart)
        bool shouldStartTutorial = PlayerPrefs.GetInt("StartTutorial", 0) == 1;
        if (shouldStartTutorial)
        {
            PlayerPrefs.DeleteKey("StartTutorial");
            PlayerPrefs.Save();
            DebugLog("StartTutorial flag detected from Splash screen, starting tutorial stage");
            if (startingStageIndex != -1)
            {
                LoadStage(startingStageIndex);
                return;
            }
        }

        // Only auto-start if not disabled (for prototyping mode)
        if (!disableAutoStart && startingStageIndex != -1)
        {
            LoadStage(startingStageIndex);
        }
        else if (disableAutoStart)
        {
            Debug.Log("[StageManager] Auto-start disabled. Use Prototyping window to start stages manually.");
        }
    }
    
    /// <summary>
    /// Allows external control of auto-start (for prototyping tools)
    /// </summary>
    public bool DisableAutoStart
    {
        get => disableAutoStart;
        set => disableAutoStart = value;
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
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerManager>();
        if (playerActionManager == null) playerActionManager = FindFirstObjectByType<PlayerActionManager>();
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

    /// <summary>
    /// Load a stage by name. Searches all stages in the database.
    /// Useful for loading test stages by name rather than number.
    /// </summary>
    public void LoadStageByName(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
        {
            this.LogError("LoadStageByName: Stage name is empty");
            return;
        }
        
        var stage = stageDatabase.Stages.FirstOrDefault(s => s.stageName == stageName);
        if (stage != null)
        {
            DebugLog($"Found stage '{stageName}' with number {stage.stageNumber}");
            LoadStage(stage.stageNumber);
        }
        else
        {
            this.LogError($"LoadStageByName: Stage not found: {stageName}");
        }
    }

    /// <summary>
    /// Get all test stages from the database.
    /// </summary>
    public List<StageData> GetTestStages()
    {
        return stageDatabase.Stages.Where(s => s != null && s.isTestStage).ToList();
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

    public bool AutoAdvanceWaves
    {
        get => autoAdvanceWaves;
        set => autoAdvanceWaves = value;
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
        gameUI.ResetWaveIcons();
        var count = Enumerable.Range(0, stage.waveConfigurations.Count).ToList();

        foreach (var waveIndex in count)
        {
            gameUI.ToggleWaveIcon(waveIndex, false);
        }

        DebugLog($"Stage {stageNumber}: '{stage.stageName}' loaded successfully (Attempt #{stageAttempts[stageNumber]})");

        // Skip wave processing for Hub stages - player moves freely and interacts with cubes
        if (stage.stageType == StageType.Hub)
        {
            DebugLog("Hub stage loaded - waves disabled, player can explore freely");
            yield break;
        }

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
        Debug.Log($"[StageManager] ConfigureForStage: Starting configuration for stage {stage.stageName}");
        
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

        DebugLog($"ConfigureWaveManager: Configuring {stage.waveConfigurations.Count} waves for stage '{stage.stageName}'");

        // Ensure wave manager is in clean state
        waveManager.StopWave();
        waveManager.waveConfiguration = stage.waveConfigurations;
        waveManager.useWaveConfiguration = true;
        waveManager.currentWaveIndex = 0; // Reset to first wave
        
        // Log wave configuration for debugging
        for (int i = 0; i < stage.waveConfigurations.Count && i < 3; i++)
        {
            var wave = stage.waveConfigurations[i];
            DebugLog($"ConfigureWaveManager: Wave {i + 1} - {wave.cubes.Count} cubes");
        }
    }

    private void ConfigurePlayer(StageData stage)
    {
        if (playerController == null)
        {
            Debug.LogWarning("[StageManager] ConfigurePlayer: playerController is null!");
            return;
        }

        Debug.Log($"[StageManager] Setting player start position: ({stage.playerStartPosition.x}, {stage.playerStartPosition.y})");
        playerController.SetStartPosition(stage.playerStartPosition.x, stage.playerStartPosition.y);
        playerController.SetPosition(stage.playerStartPosition.x, stage.playerStartPosition.y);
        playerController.ResetStatistics();
        
        // Configure respawn delay from stage default (wave will override if set)
        playerController.ConfigureRespawnDelay(stage.respawnDelayMoves);
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
            // Reset grid height override when stage ends
            waveManager.ResetGridHeightOverride();
        }

        if (playerActionManager != null)
        {
            playerActionManager.ClearAllActions();
        }
    }

    /// <summary>
    /// Comprehensive cleanup before scene changes to handle DontDestroyOnLoad objects
    /// </summary>
    private void CleanupBeforeSceneChange()
    {
        DebugLog("CleanupBeforeSceneChange: Starting comprehensive cleanup...");
        
        // First do normal stage cleanup
        CleanupCurrentStage();
        
        // Destroy specific DontDestroyOnLoad managers
        var objectsToDestroy = new List<string>()
        {
            "MessageProgressTracker",
            "DebugCoordinator", 
            "PlayerStatisticsManager",
            "AudioManager",
            "BuildInfo",
            "FileLogger",
            "PlayerManager",
            "UI"
        };
        
        foreach (string objName in objectsToDestroy)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                DebugLog($"CleanupBeforeSceneChange: Destroying DontDestroyOnLoad object - {objName}");
                Destroy(obj);
            }
        }
        
        // Destroy scene root objects (GameWorld and its children)
        GameObject gameWorld = GameObject.Find("GameWorld");
        if (gameWorld != null)
        {
            DebugLog("CleanupBeforeSceneChange: Destroying GameWorld and children");
            Destroy(gameWorld);
        }
        
        // Also find and destroy the game grid and other core game objects
        GameObject grid = GameObject.Find("Grid");
        if (grid != null) 
        {
            DebugLog("CleanupBeforeSceneChange: Destroying Grid");
            Destroy(grid);
        }
        
        // Destroy all managers in the current scene
        foreach (var manager in FindObjectsOfType<MonoBehaviour>())
        {
            if (manager.GetType().Name.EndsWith("Manager") && manager.gameObject.scene.name != null)
            {
                DebugLog($"CleanupBeforeSceneChange: Destroying {manager.GetType().Name}");
                Destroy(manager.gameObject);
            }
        }
    }
    #endregion

    #region Stage Completion Logic
    public void OnCubeCaptured(CubeType cubeType)
    {
        if (!IsStageInProgress) return;

        capturedCubeCount++;
        DebugLog($"Cube captured: {cubeType}. Total: {capturedCubeCount}");

        CheckStageCompletion();
    }

    private void CheckStageCompletion()
    {
        if (CurrentStage == null || !IsStageInProgress) 
        {
            DebugLog("CheckStageCompletion: Stage not active or null, skipping check");
            return;
        }

        bool success = false;
        bool failure = false;

        // For Tutorial stage (Stage 0), DON'T check completion here - it's handled by OnAllWavesCompleted
        if (IsTutorialStage)
        {
            // Tutorial stage should ONLY complete when OnAllWavesCompleted is called
            // This method is called after individual cube captures, so we should NOT complete here
            DebugLog("CheckStageCompletion: Tutorial stage - skipping check (will complete via OnAllWavesCompleted)");
            return;
        }
        else if (CurrentStage.requiredCaptureCount > 0)
        {
            // Other stages may have capture requirements
            success = capturedCubeCount >= CurrentStage.requiredCaptureCount;
            DebugLog($"CheckStageCompletion: Capture requirement check - {capturedCubeCount}/{CurrentStage.requiredCaptureCount}");
        }

        // Note: Escape-based failure is now handled at Wave level
        // Waves will trigger OnWaveFailed event if their escape limits are exceeded

        if (success && !failure)
        {
            DebugLog("CheckStageCompletion: Stage success criteria met, completing stage");
            CompleteStage(true);
        }
        else if (failure)
        {
            DebugLog("CheckStageCompletion: Stage failure criteria met, failing stage");
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
        DebugLog($"HandleStageSuccess: Stage {CurrentStageIndex} completed successfully");
        
        // Brief pause before showing completion message
        yield return new WaitForSeconds(stageTransitionDelay);

        // Check if this is the Tutorial stage (Stage 0) - special completion sequence
        if (autoAdvanceStages)
        {
            
            // Show demo completion message
            if (waveManager != null && waveManager.showMessages)
            {
                // Calculate final statistics
                float totalTime = Time.time - stageStartTime;
                string timeStr = $"{Mathf.FloorToInt(totalTime / 60)}:{(totalTime % 60):00.0}";
                
                var completionMessage = new WaveMessage
                {
                    Message = $"Stage {CurrentStageIndex} Complete\n\n" +
                             $"Time: {timeStr}\n" +
                             $"Cubes Captured: {capturedCubeCount}\n\n" +
                             "Press K to return to menu",
                    RequirePause = true,
                    AutoHideDelay = 0f
                };
                
                DebugLog($"HandleStageSuccess: Showing completion message with stats - Time: {timeStr}, Captured: {capturedCubeCount}");
                waveManager.ShowMessage(completionMessage);
                
                // Wait for player to dismiss message (K key press)
                // The message uses RequirePause=true, so WaveManager will wait for K key
                // We wait for the panel to become inactive, with a fallback K key check
                float timeout = 30f; // Safety timeout
                float elapsed = 0f;
                while (waveManager.messagePanel != null && waveManager.messagePanel.activeSelf && elapsed < timeout)
                {
                    // Fallback: Check for K key directly in case message system has issues
                    if (Input.GetKeyDown(KeyCode.K))
                    {
                        DebugLog("HandleStageSuccess: K key detected, forcing message dismissal");
                        if (waveManager.messagePanel != null)
                        {
                            waveManager.messagePanel.SetActive(false);
                        }
                        // Resume time in case it's still paused
                        Time.timeScale = 1f;
                        break;
                    }
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                
                // Ensure time is resumed
                Time.timeScale = 1f;
                
                DebugLog("HandleStageSuccess: Completion message dismissed by player");
            }
            else
            {
                DebugLog("HandleStageSuccess: WARNING - WaveManager or messages disabled, skipping completion message");
                // Still wait a bit before transitioning
                yield return new WaitForSeconds(2f);
            }
            
            // Transition back to Splash scene
            DebugLog("HandleStageSuccess: Loading Splash scene...");

            int nextStage = CurrentStageIndex + 1;
            if (stageDatabase.GetStage(nextStage) != null)
            {
                LoadStage(nextStage);
            }
            else
            {
                CleanupBeforeSceneChange();
                DebugLog("All stages completed!");
                 // Load splash scene with Single mode to ensure current scene is unloaded
                SceneManager.LoadScene("Splash", LoadSceneMode.Single);
                DebugLog("HandleStageSuccess: Scene load initiated");
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
        
        if (!IsStageInProgress) 
        {
            DebugLog($"OnWaveCompleted: Stage not in progress, ignoring wave {waveIndex} completion");
            return;
        }

        // Log wave transition state for debugging
        int currentWave = waveIndex + 1;
        int totalWaves = CurrentStage?.waveConfigurations?.Count ?? 0;
        DebugLog($"OnWaveCompleted: Wave {currentWave}/{totalWaves} complete. Has more waves: {waveManager?.HasMoreWaves() ?? false}");

        // Check if there are more waves in this stage
        if (waveManager != null && waveManager.HasMoreWaves())
        {
            if (autoAdvanceWaves)
            {
                DebugLog($"OnWaveCompleted: Transitioning to next wave after {stageTransitionDelay}s delay...");
                StartCoroutine(DelayedNextWave());
            }
            else
            {
                DebugLog("OnWaveCompleted: Auto-advance disabled, waiting for manual control");
            }
        }
        else
        {
            DebugLog("OnWaveCompleted: All waves completed, checking stage completion criteria...");
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
        
        if (!IsStageInProgress) 
        {
            DebugLog("OnAllWavesCompleted: Stage not in progress, ignoring event");
            return;
        }

        // For Tutorial stage (Stage 0), this is when we complete the stage
        if (IsTutorialStage)
        {
            DebugLog("OnAllWavesCompleted: Tutorial stage - all waves complete, marking stage as success");
            CompleteStage(true);
        }
        else
        {
            // For other stages, check normal completion criteria
            // If no specific requirements are set, completing all waves means stage success
            if (CurrentStage != null)
            {
                bool hasNoRequirements = CurrentStage.requiredCaptureCount == 0 && !CurrentStage.requireAllCubesDestroyed;
                DebugLog($"OnAllWavesCompleted: Stage {CurrentStageIndex} - requiredCaptureCount: {CurrentStage.requiredCaptureCount}, requireAllCubesDestroyed: {CurrentStage.requireAllCubesDestroyed}, hasNoRequirements: {hasNoRequirements}");
                
                if (hasNoRequirements)
                {
                    DebugLog("OnAllWavesCompleted: All waves complete with no specific requirements - completing stage as success");
                    CompleteStage(true);
                }
                else
                {
                    // Check specific completion criteria (capture count, etc.)
                    DebugLog("OnAllWavesCompleted: Stage has specific requirements - checking completion criteria");
                    CheckStageCompletion();
                }
            }
            else
            {
                DebugLog("OnAllWavesCompleted: CurrentStage is null - cannot check completion");
            }
        }
    }

    private IEnumerator DelayedNextWave()
    {
        DebugLog($"DelayedNextWave: Starting {stageTransitionDelay}s transition delay...");
        
        // POC: Ensure minimum transition time for tutorial readability
        float adjustedDelay = stageTransitionDelay;
        if (IsTutorialStage)
        {
            adjustedDelay = Mathf.Max(stageTransitionDelay, 3f); // At least 3 seconds for tutorial
            if (adjustedDelay != stageTransitionDelay)
            {
                DebugLog($"DelayedNextWave: Adjusted delay for tutorial from {stageTransitionDelay}s to {adjustedDelay}s");
            }
        }
        
        yield return new WaitForSeconds(adjustedDelay);
        
        if (waveManager != null)
        {
            DebugLog($"DelayedNextWave: Transition complete, starting wave {waveManager.currentWaveIndex + 1}");
            waveManager.StartNextWave();
        }
        else
        {
            DebugLog("DelayedNextWave: ERROR - WaveManager is null, cannot start next wave!");
        }
    }
    #endregion

    /// <summary>
    /// Shows feedback to the player when a cube escapes.
    /// Uses the existing WaveManager message system for consistent UI.
    /// </summary>
    /// <param name="cubeType">Type of cube that escaped</param>
    private void ShowEscapeFeedback(CubeType cubeType)
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