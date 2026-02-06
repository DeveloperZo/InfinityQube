using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using static Enumerations;

public class WaveManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Core References")]
    [SerializeField] public GridManager grid;
    [SerializeField] public GameObject[] cubePrefabs;
    [SerializeField] public PlayerManager player;
    [SerializeField] public PlayerActionManager playerActionManager;
    [SerializeField] public GameUI gameUI;
    private AudioManager audioManager;
    private MessageHighlightManager messageHighlightManager;
    
    [Header("Sub-Controllers (SRP Extraction)")]
    [Tooltip("Handles segment-to-segment transitions. Auto-created if null.")]
    [SerializeField] private WaveSegmentController segmentController;
    [Tooltip("Tracks wave statistics (captures, escapes, completion). Auto-created if null.")]
    [SerializeField] private WaveStatisticsTracker statisticsTracker;
    [SerializeField] private WaveMessageController messageController;


    [Header("Wave Configuration")]
    public bool useWaveConfiguration = false;
    public List<WaveData> waveConfiguration = new List<WaveData>();
    public int currentWaveIndex = 0;

    [Header("Speed & Timing")]
    public float normalMoveInterval = 1.75f;
    public float fastMoveInterval = 0.1f;
    public float waveStartDelay = 0.75f;
    public KeyCode speedUpKey = KeyCode.LeftShift;




    [Header("Random Wave Settings")]
    public int waveSize = 3;
    [Range(0f, 1f)] public float normalCubeChance = 0.7f;
    [Range(0f, 1f)] public float blueCubeChance = 0.2f;

    [Header("Debug & Testing")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    public bool debugMode = false;
    public bool manualControl = false;
    public bool showDebugInfo = false;
    public bool showMessages = true;

    [Header("UI References")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public GameObject continuePrompt;

    [Header("Events")]
    /// <summary>
    /// Event triggered when a wave completes successfully.
    /// StageManager subscribes to this to control stage progression.
    /// </summary>
    [SerializeField] public UnityEngine.Events.UnityEvent<int> OnWaveComplete;
    /// <summary>
    /// Event triggered when a wave fails.
    /// StageManager subscribes to this to handle failure cases.
    /// </summary>
    [SerializeField] public UnityEngine.Events.UnityEvent<int> OnWaveFailed;
    /// <summary>
    /// Event triggered when all waves in a stage are complete.
    /// StageManager subscribes to this to finalize stage completion.
    /// </summary>
    [SerializeField] public UnityEngine.Events.UnityEvent OnAllWavesComplete;
    #endregion

    #region Runtime State
    // Wave State
    public List<CubeManager> activeCubes = new List<CubeManager>();
    public bool waveActive = false;
    public int MoveStep = 0;
    public WaveData CurrentWave => useWaveConfiguration && currentWaveIndex < waveConfiguration.Count ? waveConfiguration[currentWaveIndex] : null;
    
    // Speed Control
    public bool isSpeedingUp = false;

    // Statistics - delegates to WaveStatisticsTracker
    private int normalCubesCaptured => statisticsTracker?.NormalCubesCaptured ?? 0;
    private int blueCubesCaptured => statisticsTracker?.BlueCubesCaptured ?? 0;
    private int reinforcedCubesCaptured => statisticsTracker?.ReinforcedCubesCaptured ?? 0;
    private int cubesEscaped => statisticsTracker?.CubesEscaped ?? 0;
    private int unitCubesEscaped => statisticsTracker?.UnitCubesEscaped ?? 0;
    private int playerDeaths => statisticsTracker?.PlayerDeaths ?? 0;
    private int markersPlaced => statisticsTracker?.MarkersPlaced ?? 0;
    private int detonationsUsed => statisticsTracker?.DetonationsUsed ?? 0;

    // Wave Completion Tracking - delegates to WaveStatisticsTracker
    private int totalNonBlackCubes => statisticsTracker?.TotalNonBlackCubes ?? 0;
    private int processedNonBlackCubes => statisticsTracker?.ProcessedNonBlackCubes ?? 0;

    // Internal State
    private Coroutine waveCoroutine;
    // isPaused, pendingMessages, isProcessingMessageQueue now delegated to WaveMessageController
    
    // Grid Height Override Tracking
    private int stageDefaultGridHeight = 0;  // Stored when stage starts
    private int currentGridHeightOverride = 0;  // 0 = using stage default, >0 = overridden
    
    // Segment transition tracking - delegates to WaveSegmentController
    private int currentSegmentIndex => segmentController?.CurrentSegmentIndex ?? 0;
    private bool isTransitioning => segmentController?.IsTransitioning ?? false;
    private bool waveStoppedAtEdge => segmentController?.WaveStoppedAtEdge ?? false;
    
    // SEGMENT CONTROLLER: Wave containment - delegates to WaveSegmentController
    private List<CubeData> originalWaveFormation => segmentController?.OriginalWaveFormation ?? new List<CubeData>();
    private int originalWaveDepth => segmentController?.OriginalWaveDepth ?? 0;
    private int segmentStartMoveStep => segmentController?.SegmentStartMoveStep ?? 0;
    private int movesUntilEdge => segmentController?.MovesUntilEdge ?? 0;
    
    // SEGMENT CONTROLLER: Multi-segment properties
    public bool HasSegmentControllers => grid != null && grid.HasSegmentControllers;
    private int SegmentControllerCount => grid != null ? grid.SegmentControllerCount : 0;
    private bool IsOnTerminalSegment => currentSegmentIndex >= SegmentControllerCount - 1;
    public GridSegmentController CurrentSegmentController => 
        grid != null && currentSegmentIndex < grid.SegmentControllerCount ? 
        grid.GetSegmentController(currentSegmentIndex) : null;
    
    // Sub-controller access (for debugging/testing)
    public WaveSegmentController SegmentControllerComponent => segmentController;
    public WaveStatisticsTracker StatisticsTrackerComponent => statisticsTracker;
    public WaveMessageController MessageControllerComponent => messageController;
    
    // NOTE: Segment layout prefab is now handled at stage level via StageData.segmentLayoutPrefab
    // GridManager.HandleStageStart() instantiates and configures segments for each stage
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
        InitializeState();
    }
    
    private void OnEnable()
    {
        // Subscribe to stage events to track default grid height
        GameEvents.OnStageStart += HandleStageStart;
    }
    
    private void OnDisable()
    {
        GameEvents.OnStageStart -= HandleStageStart;
    }

    private void Update()
    {
        HandleInput();
        HandleSpeedControl();
        HandleDebugCommands();
    }

    private void OnDestroy()
    {
        CleanupWave();
    }
    
    /// <summary>
    /// Handles stage start event to capture default grid height and path for override tracking.
    /// </summary>
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        if (stageData != null && grid != null)
        {
            stageDefaultGridHeight = stageData.gridHeight;
            currentGridHeightOverride = 0;  // Reset override when stage starts
            
            // NOTE: GridPath configuration removed - use segment controllers for multi-segment layouts
            
            DebugLog($"Stage {stageIndex} started: Default grid height = {stageDefaultGridHeight}");
        }
    }
    #endregion

    #region Initialization
    private void FindReferences()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (player == null) player = FindFirstObjectByType<PlayerManager>();
        if (playerActionManager == null) playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        if (messageHighlightManager == null) messageHighlightManager = FindFirstObjectByType<MessageHighlightManager>();
        if (audioManager == null) audioManager = AudioManager.Instance;
        
        // Initialize sub-controllers (SRP extraction)
        InitializeSegmentController();
        InitializeStatisticsTracker();
        InitializeMessageController();

        ValidateReferences();
    }
    
    /// <summary>
    /// Initializes the WaveSegmentController sub-component.
    /// Creates one if not assigned in Inspector.
    /// </summary>
    private void InitializeSegmentController()
    {
        if (segmentController == null)
        {
            // Try to find existing controller as child
            segmentController = GetComponentInChildren<WaveSegmentController>();
            
            // Create new controller if not found
            if (segmentController == null)
            {
                var controllerObj = new GameObject("WaveSegmentController");
                controllerObj.transform.SetParent(transform);
                segmentController = controllerObj.AddComponent<WaveSegmentController>();
                DebugLog("Created WaveSegmentController as child object");
            }
        }
        
        // Initialize controller with references
        segmentController.Initialize(this, grid, player, cubePrefabs, enableDebugLogs);
    }
    
    /// <summary>
    /// Initializes the WaveStatisticsTracker sub-component.
    /// Creates one if not assigned in Inspector.
    /// </summary>
    private void InitializeStatisticsTracker()
    {
        if (statisticsTracker == null)
        {
            // Try to find existing tracker as child
            statisticsTracker = GetComponentInChildren<WaveStatisticsTracker>();
            
            // Create new tracker if not found
            if (statisticsTracker == null)
            {
                var trackerObj = new GameObject("WaveStatisticsTracker");
                trackerObj.transform.SetParent(transform);
                statisticsTracker = trackerObj.AddComponent<WaveStatisticsTracker>();
                DebugLog("Created WaveStatisticsTracker as child object");
            }
        }
        
        // Initialize tracker with references
        statisticsTracker.Initialize(this, grid, audioManager, enableDebugLogs);
    }

    /// <summary>
    /// Initializes the WaveMessageController sub-component.
    /// Creates one if not assigned in Inspector.
    /// </summary>
    private void InitializeMessageController()
    {
        if (messageController == null)
        {
            // Try to find existing controller as child
            messageController = GetComponentInChildren<WaveMessageController>();
            
            // Create new controller if not found
            if (messageController == null)
            {
                var controllerObj = new GameObject("WaveMessageController");
                controllerObj.transform.SetParent(transform);
                messageController = controllerObj.AddComponent<WaveMessageController>();
                DebugLog("Created WaveMessageController as child object");
            }
        }
        
        // Initialize controller with references
        messageController.Initialize(this, messageHighlightManager, messagePanel, messageText, enableDebugLogs);
    }

    private void ValidateReferences()
    {
        if (grid == null) this.LogError("GridManager not found!");
        if (cubePrefabs == null || cubePrefabs.Length < 3) this.LogError("Need at least 3 cube prefabs!");
        if (audioManager == null) this.LogWarning("AudioManager not found! Audio events will not be triggered.", showDebugInfo);
        if (segmentController == null) this.LogWarning("WaveSegmentController not initialized!", showDebugInfo);
        if (messageController == null) this.LogWarning("WaveMessageController not initialized!", showDebugInfo);
    }

    private void InitializeState()
    {
        if (messagePanel != null) messagePanel.SetActive(false);
        ResetWaveStatistics();
    }
    #endregion

    #region Wave Control - Main Interface
    public void StartWave()
    {
        if (waveActive) return;

        DebugLog("🌊 Starting Wave...");

        // Reset debug mode to allow wave to run automatically
        debugMode = false;
        manualControl = false;

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        ResetWaveStatistics();
        
        // Trigger wave start audio event
        if (audioManager != null)
        {
            audioManager.TriggerAudioEvent(GameAudioEvent.WaveStarted, Vector3.zero);
            DebugLog("🔊 Audio: Wave start event triggered");
        }
        
        // Configure player respawn delay from wave data (if set) or stage default
        if (player != null && CurrentWave != null)
        {
            int waveRespawnMoves = CurrentWave.respawnDelayMoves;
            // Get stage default from StageManager if available
            var stageManager = FindFirstObjectByType<StageManager>();
            int stageDefault = stageManager?.CurrentStage?.respawnDelayMoves ?? 1;
            player.ConfigureRespawnDelay(stageDefault, waveRespawnMoves);
        }
        
        // Fire GameEvents
        GameEvents.FireWaveStart(currentWaveIndex, CurrentWave);
        gameUI.ToggleWaveIcon(currentWaveIndex, true);
        DebugLog($"Fired GameEvents.OnWaveStart for wave {currentWaveIndex}");
        
        waveCoroutine = StartCoroutine(RunWaveCoroutine());
    }

    /// <summary>
    /// Starts a wave without spawning new cubes (uses existing cubes in activeCubes).
    /// Useful for starting custom waves that were already spawned.
    /// </summary>
    public void StartWaveWithoutSpawning()
    {
        if (waveActive) return;

        if (activeCubes.Count == 0)
        {
            DebugLog("⚠️ No cubes to start - use StartWave() instead");
            return;
        }

        DebugLog("🌊 Starting Wave (without spawning)...");

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        ResetWaveStatistics();
        
        // Disable debug mode and manual control so wave runs automatically
        debugMode = false;
        manualControl = false;
        
        // Trigger wave start audio event
        if (audioManager != null)
        {
            audioManager.TriggerAudioEvent(GameAudioEvent.WaveStarted, Vector3.zero);
            DebugLog("🔊 Audio: Wave start event triggered");
        }
        
        // Configure player respawn delay from wave data (if set) or stage default
        if (player != null && CurrentWave != null)
        {
            int waveRespawnMoves = CurrentWave.respawnDelayMoves;
            // Get stage default from StageManager if available
            var stageManager = FindFirstObjectByType<StageManager>();
            int stageDefault = stageManager?.CurrentStage?.respawnDelayMoves ?? 1;
            player.ConfigureRespawnDelay(stageDefault, waveRespawnMoves);
        }
        
        // Fire GameEvents
        GameEvents.FireWaveStart(currentWaveIndex, CurrentWave);
        if (gameUI != null) gameUI.ToggleWaveIcon(currentWaveIndex, true);
        DebugLog($"Fired GameEvents.OnWaveStart for wave {currentWaveIndex}");
        
        // Start coroutine with skipSpawn flag - wave will run automatically
        waveCoroutine = StartCoroutine(RunWaveCoroutine(skipSpawn: true));
    }

    public void PauseWave()
    {
        if (!waveActive) return;

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        debugMode = true;
        manualControl = true;
        waveActive = false;

        DebugLog("⏸️ Wave Paused - Manual Control Enabled");
    }

    public void ResumeWave()
    {
        if (waveActive) return;

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(RunWaveCoroutine(resume: true));
        debugMode = true;
        manualControl = false;

        DebugLog("▶️ Wave Resumed");
    }
    
    /// <summary>
    /// Pauses wave movement for validation (keeps wave active, just stops cube movement)
    /// Used by MessageHighlightManager for tutorial sequences
    /// </summary>
    public void PauseWaveForValidation()
    {
        if (!waveActive) return;
        
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        // Set manualControl to stop the RunWaveCoroutine loop
        manualControl = true;
        // Don't set waveActive = false, just stop the coroutine
        // This allows the wave to resume from where it was
        
        DebugLog("⏸️ Wave paused for validation");
    }
    
    /// <summary>
    /// Resumes wave movement after validation
    /// Used by MessageHighlightManager for tutorial sequences
    /// </summary>
    public void ResumeWaveAfterValidation()
    {
        if (!waveActive) return; // Wave must be active to resume
        
        // Clear manualControl to allow RunWaveCoroutine to continue
        manualControl = false;
        
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(RunWaveCoroutine(resume: true));
        
        DebugLog("▶️ Wave resumed after validation");
    }

    public void StartNextWave()
    {
        if (useWaveConfiguration && currentWaveIndex < waveConfiguration.Count)
        {
            StartCoroutine(DelayedWaveStart());
        }
        else
        {
            DebugLog("⚠️ No more waves to start");
        }
    }

    public bool HasMoreWaves()
    {
        return useWaveConfiguration && currentWaveIndex < waveConfiguration.Count - 1;
    }

    public void ResetToFirstWave()
    {
        currentWaveIndex = 0;
        DebugLog("🔄 Reset to first wave");
    }

    public void StopWave()
    {
        // Stop the wave coroutine but don't clear cubes
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        waveActive = false;
        // Enable debug mode so manual controls work
        debugMode = true;
        DebugLog("⏹️ Wave Stopped (cubes remain, debug mode enabled for manual control)");
    }

    /// <summary>
    /// Loads a specific wave for play/testing.
    /// </summary>
    public void LoadWave(WaveData wave)
    {
        if (wave == null)
        {
            DebugLog("⚠️ Cannot load null wave");
            return;
        }
        
        // Stop any current wave
        if (waveActive) StopWave();
        
        // Add wave to configuration if not present
        if (!waveConfiguration.Contains(wave))
        {
            waveConfiguration.Insert(0, wave);
        }
        
        // Set current wave index
        currentWaveIndex = waveConfiguration.IndexOf(wave);
        useWaveConfiguration = true;
        
        DebugLog($"📋 Loaded wave: {wave.name}");
    }

    /// <summary>
    /// Gets list of available waves from configuration and resources.
    /// </summary>
    public List<WaveData> GetAvailableWaves()
    {
        var waves = new List<WaveData>();
        
        // Add configured waves
        if (waveConfiguration != null)
        {
            waves.AddRange(waveConfiguration);
        }
        
        return waves;
    }

    /// <summary>
    /// Force completes the current wave (for testing/prototyping).
    /// </summary>
    public void ForceCompleteWave()
    {
        if (!waveActive && activeCubes.Count == 0)
        {
            DebugLog("⚠️ No active wave to complete");
            return;
        }
        
        DebugLog("⏭️ Force completing wave...");
        
        // Clear all cubes
        ClearAllCubes();
        
        // Stop the wave coroutine
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        waveActive = false;
        
        // Fire completion event
        GameEvents.FireWaveComplete(currentWaveIndex);
        
        // Advance to next wave if available
        if (HasMoreWaves())
        {
            currentWaveIndex++;
            DebugLog($"➡️ Advanced to wave {currentWaveIndex}");
        }
    }
    #endregion

    #region Wave Execution
    private IEnumerator RunWaveCoroutine(bool resume = false, bool skipSpawn = false)
    {
        waveActive = true;
        if (!resume) MoveStep = 0;

        SetupWave(resume, skipSpawn);

        yield return new WaitForSeconds(GetWaveStartDelay());

        if (manualControl)
        {
            // Manual mode - wait for external control
            while (manualControl) yield return null;
            waveActive = false;
            yield break;
        }

        // Main wave loop - segment 0
        while (HasActiveCubes())
        {
            yield return ProcessWaveStep();
            yield return new WaitForSeconds(GetCurrentMoveInterval());
        }

        // NOTE: Legacy "ADVANCED GRID" segment transition code removed.
        // Segment transitions are now handled by WaveSegmentController via edge containment.

        CompleteWave();
    }

    private void SetupWave(bool resume, bool skipSpawn = false)
    {
        if (!resume)
        {
            // Apply grid height override before spawning (if needed)
            ApplyGridHeightOverride();
            
            // ADVANCED GRID: Apply path override if wave specifies one
            ApplyGridPathOverride();
            
            // Skip spawning if skipSpawn flag is set (custom waves already spawned)
            if (skipSpawn)
            {
                DebugLog("SetupWave: Skipping spawn - using existing cubes");
                CountNonBlackCubes(); // Count existing cubes for completion tracking
                // Don't show messages for custom waves (no wave configuration)
            }
            else
            {
                SpawnWaveCubes();
                ShowInitialMessages(); // Only show messages for configured waves
            }
        }

        ConfigurePlayer();
    }
    
    /// <summary>
    /// ADVANCED GRID: Applies grid path override from current wave if specified.
    /// NOTE: Segment layout is now handled at stage level via StageData.segmentLayoutPrefab
    /// GridManager.HandleStageStart() instantiates and configures segments for each stage.
    /// </summary>
    private void ApplyGridPathOverride()
    {
        // Segment layout is now configured at stage level, not wave level
        // See GridManager.ConfigureSegmentLayoutFromStage() for implementation
    }

    private IEnumerator ProcessWaveStep()
    {
        // Apply default directions to swap markers before move forward
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.ApplyDefaultDirectionsToSwapMarkers();
        }
        
        MoveCubesForward(); // This now includes player cube spawning
        
        // Move player cubes backward after wave cubes have moved
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.MovePlayerCubesBackward();
            playerActionManager.MarkerSystem.CheckPlayerCubeCollisions();
        }
        
        ProcessStepSequences(); // Sequences handle both messages and highlights
        NotifyStepComplete();

        yield return null;
    }

    private void CompleteWave()
    {
        waveActive = false;
        waveCoroutine = null;

        if (grid != null) grid.ClearAllMarkers();

        // Clear player cubes when wave completes
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.ClearPlayerCubes();
        }

        // Trigger wave completion audio event
        if (audioManager != null)
        {
            audioManager.TriggerAudioEvent(GameAudioEvent.WaveCompleted, Vector3.zero);
            DebugLog("🔊 Audio: Wave completion event triggered");
        }

        // Fire GameEvents
        GameEvents.FireWaveComplete(currentWaveIndex);
        DebugLog($"Fired GameEvents.OnWaveComplete for wave {currentWaveIndex}");

        ProcessEndSequences(); // Sequences handle end-of-wave messages

        // POC: Show wave completion message before advancing
        ShowWaveCompletionMessage();

        // Trigger wave completion event
        OnWaveComplete?.Invoke(currentWaveIndex);
        DebugLog($"🎯 Triggered OnWaveComplete event for wave {currentWaveIndex}");

        // Check if all waves are complete
        if (useWaveConfiguration && currentWaveIndex >= waveConfiguration.Count - 1)
        {
            OnAllWavesComplete?.Invoke();
            DebugLog("🏁 Triggered OnAllWavesComplete event");
        }
        else
        {
            // Auto-advance to next wave (can be overridden by stage manager)
            AdvanceToNextWave();
        }

        DebugLog("✅ Wave Completed");
    }
    #endregion

    #region Segment Controller Transitions (Facade - delegates to WaveSegmentController)
    
    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the edge of the current segment.
    /// Returns true if the cube should be queued for transition (not terminal segment).
    /// Returns false if this is the terminal segment (cube truly escapes).
    /// </summary>
    public bool HandleCubeAtSegmentEdge(CubeManager cube)
        => segmentController?.HandleCubeAtSegmentEdge(cube) ?? false;

    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the segment edge and STOPS (doesn't escape).
    /// </summary>
    public void HandleCubeStoppedAtEdge(CubeManager cube)
        => segmentController?.HandleCubeStoppedAtEdge(cube);

    /// <summary>
    /// SEGMENT CONTROLLER: Checks if cube should stop at edge instead of escaping.
    /// Returns true if cube is at the segment edge (next move would escape).
    /// Only applies to non-terminal segments.
    /// </summary>
    public bool ShouldCubeStopAtEdge(CubeManager cube)
        => segmentController?.ShouldCubeStopAtEdge(cube) ?? false;

    /// <summary>
    /// SEGMENT CONTROLLER: Checks if all cubes have reached the segment edge and transition should occur.
    /// </summary>
    public void CheckSegmentTransitionReady()
        => segmentController?.CheckSegmentTransitionReady();

    /// <summary>
    /// SEGMENT CONTROLLER: Checks if a grid position is occupied by another cube.
    /// Used for wave containment - cubes stop behind other cubes.
    /// </summary>
    public bool IsPositionOccupiedByCube(Vector2Int position, CubeManager excludeCube = null)
        => segmentController?.IsPositionOccupiedByCube(position, excludeCube) ?? false;

    /// <summary>
    /// SEGMENT CONTROLLER: Resets segment tracking to initial state.
    /// </summary>
    public void ResetSegmentState()
        => segmentController?.ResetSegmentState();

    /// <summary>
    /// Pre-checks if ANY cube in the wave is at the segment edge.
    /// Called BEFORE processing cube movements to prevent race conditions.
    /// </summary>
    private void PreCheckWaveAtEdge()
        => segmentController?.PreCheckWaveAtEdge();

    /// <summary>
    /// SEGMENT CONTROLLER: Checks if the wave has reached the segment edge.
    /// </summary>
    private void CheckWaveAtSegmentEdge()
        => segmentController?.CheckWaveAtSegmentEdge();

    /// <summary>
    /// SEGMENT CONTROLLER: Records the original wave formation for respawn at segment edge.
    /// </summary>
    private void TrackOriginalWaveFormation()
        => segmentController?.TrackOriginalWaveFormation();

    /// <summary>
    /// ADVANCED GRID: Resets segment tracking (call when wave/stage resets).
    /// </summary>
    public void ResetSegmentTracking()
    {
        segmentController?.ResetSegmentTracking();
        
        if (grid != null)
        {
            grid.SetActiveSegment(0);
        }
        
        // Reset camera to default/segment 0 settings
        var cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            if (HasSegmentControllers && grid.SegmentControllerCount > 0)
            {
                var primarySegment = grid.GetSegmentController(0);
                cameraFollow.SetSegmentInstant(primarySegment);
            }
            else
            {
                cameraFollow.ResetToDefault();
            }
        }
        
        DebugLog("🔄 Segment tracking reset");
    }

    #endregion

    #region Cube Management
    private void SpawnWaveCubes()
    {
        ClearAllCubes();
        ResetPlayer();
        
        // Debug: Log segment controller status
        DebugLog($"🔧 SEGMENT STATUS: HasSegmentControllers={HasSegmentControllers}, Count={SegmentControllerCount}, IsTerminal={IsOnTerminalSegment}");
        
        // Clear segment edge tracking for new wave
        segmentController?.ClearEdgeTracking();

        // Validate and adjust grid size to match wave size if needed
        if (useWaveConfiguration && CurrentWave != null)
        {
            ValidateAndResizeGridForWave(CurrentWave);
            SpawnConfigurationCubes();
        }
        else
        {
            SpawnRandomCubes();
        }
        
        // SEGMENT CONTROLLER: Track original wave formation for edge containment
        TrackOriginalWaveFormation();

        CountNonBlackCubes();
        DebugLog($"📦 Spawned {activeCubes.Count} cubes ({totalNonBlackCubes} non-black)");
    }
    
    /// <summary>
    /// Validates that wave spawn area fits within the grid.
    /// Does NOT resize the grid - grid size is controlled by StageData.
    /// Wave's GridWidth/GridHeight represent the spawn area dimensions, not grid size.
    /// </summary>
    private void ValidateAndResizeGridForWave(WaveData wave)
    {
        if (wave == null || grid == null) return;

        // Wave's GridWidth/GridHeight is the spawn area, not the grid size
        // Just validate that spawn area fits within grid
        if (wave.GridWidth > grid.Width)
        {
            DebugLog($"⚠️ Wave spawn width ({wave.GridWidth}) exceeds grid width ({grid.Width}). Cubes may spawn out of bounds.");
        }
        
        // GridHeight is the number of rows of cubes at top of grid - this is fine as long as grid is tall enough
        DebugLog($"Wave spawn area: {wave.GridWidth}x{wave.GridHeight}, Grid: {grid.Width}x{grid.Height}");
    }
    
    /// <summary>
    /// Applies grid height override from current wave if specified.
    /// Once overridden, grid stays at that height until stage end or another override.
    /// </summary>
    private void ApplyGridHeightOverride()
    {
        if (CurrentWave == null || grid == null) return;
        
        int targetHeight = stageDefaultGridHeight;  // Default to stage height
        
        // Check if current wave has an override
        if (CurrentWave.overrideGridHeight > 0)
        {
            targetHeight = CurrentWave.overrideGridHeight;
            currentGridHeightOverride = targetHeight;
            DebugLog($"📏 Wave {currentWaveIndex} overrides grid height to {targetHeight} (stage default: {stageDefaultGridHeight})");
        }
        else if (currentGridHeightOverride > 0)
        {
            // No override in this wave, but previous wave set an override - keep it
            targetHeight = currentGridHeightOverride;
            DebugLog($"📏 Keeping grid height override from previous wave: {targetHeight}");
        }
        
        // Resize grid if height needs to change (width always uses stage default)
        if (grid.Height != targetHeight)
        {
            var stageManager = FindFirstObjectByType<StageManager>();
            int stageWidth = stageManager?.CurrentStage?.gridWidth ?? grid.Width;
            
            DebugLog($"📐 Resizing grid from {grid.Width}x{grid.Height} to {stageWidth}x{targetHeight}");
            grid.ResizeGrid(stageWidth, targetHeight);
        }
    }
    
    /// <summary>
    /// Resets grid height and path override tracking (called when stage ends).
    /// </summary>
    public void ResetGridHeightOverride()
    {
        currentGridHeightOverride = 0;
        stageDefaultGridHeight = 0;
        
        DebugLog("🔄 Grid height override reset");
    }

    private void SpawnConfigurationCubes()
    {
        var wave = CurrentWave;
        foreach (var cubeData in wave.cubes)
        {
            SpawnCube(cubeData);
        }
    }

    private void SpawnRandomCubes()
    {
        // Generate cubes in top rows of grid
        for (int row = 1; row <= waveSize; row++)
        {
            int z = grid.Height - row;
            for (int x = 0; x < grid.Width; x++)
            {
                var cubeData = CreateRandomCubeData(x, z);
                SpawnCube(cubeData);
            }
        }
    }

    private void SpawnCube(CubeData cubeData)
    {
        int prefabIndex = (int)cubeData.type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length)
        {
            this.LogError($"Invalid prefab index {prefabIndex} for cube type {cubeData.type}");
            return;
        }
        
        // Calculate grid position WITHOUT modifying original cubeData
        var waveHeight = waveConfiguration.Count > 0 ? waveConfiguration[currentWaveIndex].GridHeight : waveSize;
        var gridLocalHeight = grid.Height - (waveHeight - cubeData.position.y);
        
        // Validate bounds before spawning
        if (cubeData.position.x < 0 || cubeData.position.x >= grid.Width)
        {
            this.LogError($"Cube spawn X position ({cubeData.position.x}) out of bounds (0-{grid.Width - 1})");
            return;
        }
        
        if (gridLocalHeight < 0 || gridLocalHeight >= grid.Height)
        {
            this.LogError($"Cube spawn Y position ({gridLocalHeight}) out of bounds (0-{grid.Height - 1}). Wave height: {waveHeight}, cube Y: {cubeData.position.y}, grid height: {grid.Height}");
            return;
        }
        
        // Use local position for spawning (preserve original wave data)
        Vector2Int spawnPosition = new Vector2Int(cubeData.position.x, gridLocalHeight);
        
        // DUPLICATE CHECK: Skip spawning if a cube already exists at this position
        foreach (var existingCube in activeCubes)
        {
            if (existingCube != null && !existingCube.isDestroyed && 
                existingCube.position.x == spawnPosition.x && existingCube.position.y == spawnPosition.y)
            {
                this.LogWarning($"Skipping duplicate cube spawn at ({spawnPosition.x}, {spawnPosition.y}) - cube already exists at this position");
                return;
            }
        }
        
        Vector3 spawnPos = grid.GridToWorldPosition(spawnPosition.x, spawnPosition.y, 2f);
        this.Log($"Spawning {cubeData.type} cube at grid ({spawnPosition.x}, {spawnPosition.y}) -> world {spawnPos}", showDebugInfo);

        GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        // Create a copy of cubeData with the calculated grid position
        var spawnData = new CubeData
        {
            type = cubeData.type,
            position = spawnPosition,
            level = cubeData.level
        };
        
        cube.Init(grid, spawnData, 2f);
        
        // SEGMENT CONTROLLER: Set segment controller on cube
        if (HasSegmentControllers && CurrentSegmentController != null)
        {
            cube.SetSegmentController(CurrentSegmentController);
            this.Log($"Cube assigned to segment controller {currentSegmentIndex}", showDebugInfo);
        }
        // NOTE: Legacy GridPath configuration removed - segment controllers handle direction
        
        activeCubes.Add(cube);

        this.Log($"Cube spawned successfully. Active cubes: {activeCubes.Count}", showDebugInfo);
    }

    private CubeData CreateRandomCubeData(int x, int z)
    {
        return new CubeData
        {
            type = GetRandomCubeType(),
            position = new Vector2Int(x, z),
            level = 1
        };
    }

    private void MoveCubesForward()
    {
        // Spawn player cubes from unit markers before moving wave cubes
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.SpawnPlayerCubes();
        }
        
        // CRITICAL: Pre-check if ANY cube is at the edge BEFORE processing movements
        // This prevents race conditions where back rows move before front row sets the stop flag
        if (HasSegmentControllers && !IsOnTerminalSegment && !waveStoppedAtEdge)
        {
            PreCheckWaveAtEdge();
        }
        
        // If wave is stopped at edge, skip all movement (will transition next)
        if (waveStoppedAtEdge)
        {
            DebugLog($"🛑 Wave stopped at edge - skipping all cube movement this frame");
            MoveStep++;
            CheckWaveAtSegmentEdge();
            return;
        }
        
        // Create a snapshot of cubes to process (avoids modification during iteration)
        var cubesToProcess = new List<CubeManager>(activeCubes);
        
        foreach (var cube in cubesToProcess)
        {
            if (cube == null || cube.isDestroyed)
            {
                activeCubes.Remove(cube);
                continue;
            }

            // Only move cubes that aren't currently animating (atomic movement)
            if (!cube.isMoving)
            {
                cube.ResetMovementState();
                bool stillAlive = cube.MoveForward();

                // Trigger cube landing audio event after movement
                if (stillAlive && audioManager != null && grid != null)
                {
                    Vector3 cubeWorldPosition = grid.GridToWorldPosition(cube.position.x, cube.position.y, 2f);
                    audioManager.TriggerCubeAudioEvent(GameAudioEvent.CubeLanded, cube.type, cubeWorldPosition);
                    DebugLog($"🔊 Audio: Cube landing event triggered for {cube.type} at position {cube.position}");
                }

                // Only remove if not already removed by MoveForward (e.g., segment transition)
                if (!stillAlive && activeCubes.Contains(cube))
                {
                    activeCubes.Remove(cube);
                }
            }
        }
        MoveStep++;
        
        // SEGMENT CONTROLLER: Check if wave has reached segment edge after this move
        if (HasSegmentControllers && !IsOnTerminalSegment && !isTransitioning)
        {
            CheckWaveAtSegmentEdge();
        }
        
        // Track free moves: moves where only Infinity cubes (or no cubes) are present
        bool onlyInfinityOrEmpty = activeCubes.Count == 0 || activeCubes.All(c => c != null && c.type == CubeType.Infinity);
        if (onlyInfinityOrEmpty)
        {
            var scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                scoreManager.RecordFreeMove();
            }
        }
        
        // Fire GameEvents
        GameEvents.FireWaveStep(currentWaveIndex, MoveStep);
        
        // Calculate and fire progress event
        if (totalNonBlackCubes > 0)
        {
            float progress = (processedNonBlackCubes / (float)totalNonBlackCubes) * 100f;
            GameEvents.FireWaveProgress(currentWaveIndex, progress);
        }
        
    } 

    public void ClearAllCubes()
    {
        foreach (var cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
                Destroy(cube.gameObject);
        }
        activeCubes.Clear();

        // Clear player cubes as well
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.ClearPlayerCubes();
        }
    }
    #endregion

    #region Manual Control (for debugging)
    public void ManualMoveWaveForward()
    {
        // Allow manual movement when wave is stopped (debug mode) or when wave is active
        if (!debugMode && !waveActive) return;

        MoveCubesForward(); // MoveCubesForward() already increments MoveStep
        ProcessStepSequences(); // Sequences handle both messages and highlights
        NotifyStepComplete();

        DebugLog($"🔧 Manual Step Forward: {MoveStep}");
    }

    /// <summary>
    /// Manually moves all wave cubes backward (up) by one step.
    /// Only works when wave is stopped (not active) or in debug mode.
    /// </summary>
    public void ManualMoveWaveBackward()
    {
        if (!debugMode && waveActive) return;

        if (activeCubes.Count == 0)
        {
            DebugLog("⚠️ No cubes to move backward");
            return;
        }

        // Move all cubes backward (up - increase Y)
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (i >= activeCubes.Count) continue;
            
            var cube = activeCubes[i];
            if (cube == null)
            {
                activeCubes.RemoveAt(i);
                continue;
            }

            // Only move cubes that aren't currently animating
            if (!cube.isMoving)
            {
                cube.ResetMovementState();
                
                // Move backward: increase Y (move up)
                int nextY = cube.position.y + 1;
                if (nextY < grid.Height && cube.position.x >= 0 && cube.position.x < grid.Width)
                {
                    Vector2Int oldPosition = cube.position;
                    cube.position = new Vector2Int(cube.position.x, nextY);
                    
                    // Update cube's world position
                    Vector3 worldPos = grid.GridToWorldPosition(cube.position.x, cube.position.y, 2f);
                    cube.transform.position = worldPos;
                    
                    // Fire move event
                    GameEvents.FireCubeMove(oldPosition, cube.position, cube.type);
                }
            }
        }
        
        if (MoveStep > 0) MoveStep--;
        ProcessStepSequences(); // Sequences handle both messages and highlights
        NotifyStepComplete();

        DebugLog($"🔧 Manual Step Backward: {MoveStep}");
    }

    public void EnterDebugMode(bool manual)
    {
        debugMode = true;
        manualControl = manual;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        waveActive = false;

        DebugLog($"🔧 Debug Mode: Manual={manual}");
    }

    public void ExitDebugMode()
    {
        debugMode = false;
        manualControl = false;
        DebugLog("🔧 Debug Mode Disabled");
    }
    #endregion

    #region Message System (Facade - delegates to WaveMessageController)
    
    /// <summary>
    /// Shows initial messages when wave starts. Delegates to messageController.
    /// </summary>
    private void ShowInitialMessages()
        => messageController?.ShowInitialMessages();

    /// <summary>
    /// Processes highlight sequences at the current move step. Delegates to messageController.
    /// </summary>
    private void ProcessStepSequences()
        => messageController?.ProcessStepSequences();

    /// <summary>
    /// Processes highlight sequences at wave end. Delegates to messageController.
    /// </summary>
    private void ProcessEndSequences()
        => messageController?.ProcessEndSequences();

    /// <summary>
    /// Shows wave completion feedback message. Delegates to messageController.
    /// </summary>
    private void ShowWaveCompletionMessage()
    {
        if (!showMessages) return;
        int totalWaves = waveConfiguration != null && waveConfiguration.Count > 0 ? waveConfiguration.Count : 1;
        messageController?.ShowWaveCompletionMessage(showMessages, currentWaveIndex, totalWaves,
            normalCubesCaptured, blueCubesCaptured, reinforcedCubesCaptured, cubesEscaped);
    }

    /// <summary>
    /// Enqueues a message for display. Delegates to messageController.
    /// </summary>
    public void ShowMessage(WaveMessage message)
        => messageController?.ShowMessage(message, showMessages);

    /// <summary>
    /// Displays a message with optional pause. Delegates to messageController.
    /// </summary>
    private IEnumerator DisplayMessage(WaveMessage message)
    {
        if (messageController != null)
        {
            yield return messageController.DisplayMessage(message, showMessages);
        }
    }
    
    // State access for message controller
    private bool isPaused => messageController?.IsPaused ?? false;
    private bool isProcessingMessageQueue => messageController?.IsProcessingMessageQueue ?? false;
    #endregion

    #region Statistics & Events (Facade - delegates to WaveStatisticsTracker)
    
    /// <summary>
    /// Records a cube capture event. Delegates to statisticsTracker.
    /// </summary>
    public void OnCubeCaptured(CubeType cubeType)
        => statisticsTracker?.OnCubeCaptured(cubeType);

    /// <summary>
    /// CUBE ESCAPE HANDLER: Called when a cube escapes the play area.
    /// Delegates to statisticsTracker.
    /// </summary>
    public void OnCubeEscaped(CubeType cubeType)
        => statisticsTracker?.OnCubeEscaped(cubeType);

    /// <summary>
    /// Called when player dies. Delegates to statisticsTracker.
    /// </summary>
    public void OnPlayerDeath()
        => statisticsTracker?.OnPlayerDeath();

    /// <summary>
    /// Records a marker placement. Delegates to statisticsTracker.
    /// </summary>
    public void OnMarkerPlaced()
        => statisticsTracker?.OnMarkerPlaced();

    /// <summary>
    /// Records a detonation use. Delegates to statisticsTracker.
    /// </summary>
    public void OnDetonationUsed()
        => statisticsTracker?.OnDetonationUsed();

    /// <summary>
    /// Called when a non-black cube is processed. Delegates to statisticsTracker.
    /// </summary>
    public void OnNonBlackCubeProcessed(CubeType cubeType, bool wasCaptured)
        => statisticsTracker?.OnNonBlackCubeProcessed(cubeType, wasCaptured);

    /// <summary>
    /// WAVE FAILURE TRIGGER: Called when wave fails due to escape limit or other criteria.
    /// Notifies Stage via event system that this wave has failed.
    /// </summary>
    private void TriggerWaveFailure(string reason)
    {
        DebugLog($"🔴 WAVE FAILURE: {reason}");
        
        // Stop current wave
        waveActive = false;
        
        // Show failure feedback to player
        if (showMessages)
        {
            var failureMessage = new WaveMessage
            {
                Message = $"Wave Failed!\n{reason}",
                AutoHideDelay = 3f,
                RequirePause = true
            };
            ShowMessage(failureMessage);
        }
        
        // Trigger wave failure event for StageManager
        OnWaveFailed?.Invoke(currentWaveIndex);
        DebugLog($"🎯 Triggered OnWaveFailed event for wave {currentWaveIndex}");
    }
    
    /// <summary>
    /// Called by WaveStatisticsTracker when wave failure is triggered.
    /// Public facade for the private TriggerWaveFailure method.
    /// </summary>
    public void TriggerWaveFailureFromTracker(string reason)
    {
        TriggerWaveFailure(reason);
    }
    
    /// <summary>
    /// Called by WaveStatisticsTracker when wave completion is detected.
    /// Shows the completion message and handles wave ending.
    /// </summary>
    public void ShowWaveCompletionFromTracker(string reason)
    {
        StartCoroutine(ShowCompletionMessage(reason));
    }

    private IEnumerator ShowCompletionMessage(string reason)
    {
        var message = new WaveMessage
        {
            Message = $"Wave Complete!\n{reason}",
            RequirePause = true,
            AutoHideDelay = 0f
        };

        yield return DisplayMessage(message);
        CompleteWave();
    }
    #endregion

    #region Utility Methods
    private void HandleInput()
    {
        if (!waveActive && Input.GetKeyDown(KeyCode.Return))
        {
            StartWave();
        }

        if (isPaused && Input.GetKeyDown(KeyCode.K))
        {
            // Message confirmation handled in DisplayMessage
        }
    }
 
    private void HandleSpeedControl()
    {
        bool wasSpeedingUp = isSpeedingUp;
        isSpeedingUp = Input.GetKey(speedUpKey);

        if (isSpeedingUp != wasSpeedingUp)
        {
            SetSpeedState(isSpeedingUp);
        }
    }

    private void HandleDebugCommands()
    {
        if (!showDebugInfo) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            DebugActiveCubes();
        }
    }

    public void SetSpeedState(bool isSpeeding)
    {
        isSpeedingUp = isSpeeding;
    }

    /// <summary>
    /// Sets the wave speed multiplier for prototyping tools.
    /// 1.0 = normal speed, 2.0 = double speed, 0.5 = half speed.
    /// </summary>
    private float speedMultiplier = 1f;
    public void SetWaveSpeed(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.25f, 4f);
        DebugLog($"Wave speed set to {speedMultiplier:F1}x");
    }

    private float GetCurrentMoveInterval()
    {
        float normal = CurrentWave?.moveInterval ?? normalMoveInterval;
        float fast = CurrentWave?.fastMoveInterval ?? fastMoveInterval;
        float baseInterval = isSpeedingUp ? fast : normal;
        // Apply speed multiplier (higher multiplier = faster = shorter interval)
        return baseInterval / speedMultiplier;
    }

    private float GetWaveStartDelay()
    {
        return CurrentWave?.waveStartDelay ?? waveStartDelay;
    }

    private bool HasActiveCubes()
    {
        return activeCubes.Count > 0 && !debugMode;
    }
    
    /// <summary>
    /// Checks if only Infinity cubes (or no cubes) remain in the wave.
    /// This indicates a "safe" state where no penalties will occur from cube escapes.
    /// </summary>
    public bool HasOnlyInfinityCubesRemaining()
    {
        if (activeCubes.Count == 0) return true;
        return activeCubes.All(c => c != null && !c.isDestroyed && c.type == CubeType.Infinity);
    }

    private CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < normalCubeChance) return CubeType.Unit;
        if (random < normalCubeChance + blueCubeChance) return CubeType.Matrix;
        return CubeType.Infinity;
    }

    private void CountNonBlackCubes()
        => statisticsTracker?.CountNonBlackCubes();

    private void ResetWaveStatistics()
    {
        statisticsTracker?.ResetStatistics();
        
        // ADVANCED GRID: Reset segment tracking
        ResetSegmentTracking();
    }

    private void ResetPlayer()
    {
        if (player != null && !debugMode)
        {
            player.enabled = true;
            player.ResetMarkers();
        }
    }

    private void ConfigurePlayer()
    {
        if (player != null && !debugMode)
        {
            player.enabled = true;
            
            // NOTE: Marker economy (charges, max on grid) is now managed by PlayerActionManager
            // via GameEvents.OnStageStart and GameEvents.OnWaveStart subscriptions.
            // This method should NOT set marker charges directly - that would override the
            // stage/wave grant system.
            
            // Only validate current mode based on available marker types
            if (playerActionManager != null)
            {
                playerActionManager.ValidateCurrentMode();
            }
        }
    }

    private void NotifyStepComplete()
    {
        
    }

    private void NotifyStageManager(System.Action<StageManager> action)
    {
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null) action(stageManager);
    }

    private void AdvanceToNextWave()
    {
        if (!useWaveConfiguration) return;

        currentWaveIndex++;
        DebugLog($"📈 Advanced to wave index {currentWaveIndex}");
        
        // Note: Wave starting is now controlled by StageManager via events
        // The auto-start logic is removed to prevent circular dependencies
    }

    private IEnumerator DelayedWaveStart()
    {
        yield return new WaitForSeconds(waveStartDelay);
        StartWave();
    }

    private void CleanupWave()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        waveActive = false;
        ClearAllCubes();
    }

    private void DebugActiveCubes()
    {
        DebugLog($"==== Active Cubes: {activeCubes.Count} ====");
        for (int i = 0; i < activeCubes.Count; i++)
        {
            var cube = activeCubes[i];
            if (cube != null)
            {
                DebugLog($"[{i}] {cube.type} at ({cube.position.x}, {cube.position.y})");
            }
        }
    }

    private void DebugLog(string message)
    {
        this.Log(message, showDebugInfo);
    }
    #endregion

    #region Public Properties (for Debuggers)

    public int CurrentWaveIndex => currentWaveIndex;
    #endregion


    #region Public Methods
    public void AddCube(Vector2Int wavePosition, CubeType type)
    {
        if (CurrentWave == null)
        {
            this.LogError("No active wave configuration to add a cube.");
            return;
        }

        // Add cube to wave configuration
        var cubeData = new CubeData { position = wavePosition, type = type };
        CurrentWave.cubes.Add(cubeData);

        // Spawn cube in the grid
        SpawnCube(cubeData);
    }

    /// <summary>
    /// Removes a cube from the active cubes list without destroying it.
    /// Used for segment transitions where cube will be respawned.
    /// </summary>
    public void RemoveCubeFromActive(CubeManager cube)
    {
        if (cube != null)
        {
            activeCubes.Remove(cube);
            DebugLog($"🔄 Removed {cube.type} from active cubes (for transition)");
        }
    }
    
    public void RemoveCube(CubeManager cube)
    {
        if (CurrentWave == null)
        {
            this.LogError("No active wave configuration to remove a cube.");
            return;
        }

        // Remove cube from wave configuration
        CurrentWave.cubes.RemoveAll(cd => cd.position == cube.position);

        // Remove cube from the grid
        activeCubes.Remove(cube);
        if (cube != null && cube.gameObject != null)
        {
            Destroy(cube.gameObject);
        }
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
        string status = waveActive ? "ACTIVE" : "STOPPED";
        string speedState = isSpeedingUp ? "FAST" : "NORMAL";
        string waveLabel = GetWaveLabel();
        return $"Wave {waveLabel}: {status} ({speedState}) Step:{MoveStep} Cubes:{activeCubes.Count}";
    }
    
    /// <summary>
    /// Gets the display label for the current wave (e.g., "1", "2", "3")
    /// </summary>
    public string GetWaveLabel()
    {
        int displayIndex = currentWaveIndex + 1; // 1-based for display
        return $"{displayIndex}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Wave Active"] = waveActive,
            ["Current Wave Index"] = currentWaveIndex,
            ["Move Step"] = MoveStep,
            ["Active Cubes"] = activeCubes.Count,
            ["Is Speeding Up"] = isSpeedingUp,
            ["Debug Mode"] = debugMode,
            ["Manual Control"] = manualControl,
            ["Is Paused"] = isPaused,
            ["Show Messages"] = showMessages,
            ["Normal Cubes Captured"] = normalCubesCaptured,
            ["Blue Cubes Captured"] = blueCubesCaptured,
            ["Reinforced Cubes Captured"] = reinforcedCubesCaptured,
            ["Cubes Escaped"] = cubesEscaped,
            ["Markers Placed"] = markersPlaced,
            ["Detonations Used"] = detonationsUsed,
            ["Total Non-Black Cubes"] = totalNonBlackCubes,
            ["Processed Non-Black Cubes"] = processedNonBlackCubes,
            ["Current Move Interval"] = GetCurrentMoveInterval(),
            ["Wave Start Delay"] = GetWaveStartDelay(),
            ["Use Wave Configuration"] = useWaveConfiguration,
            ["Pending Messages"] = messageController?.PendingMessagesCount ?? 0
        };

        return debugData;
    }

    public void ResetToDefaults()
    {
        // Stop current wave
        StopWave();
        
        // Reset wave index
        currentWaveIndex = 0;
        MoveStep = 0;
        
        // Reset flags
        waveActive = false;
        isSpeedingUp = false;
        debugMode = false;
        manualControl = false;
        
        // Reset statistics
        ResetWaveStatistics();
        
        // Reset message controller state (clears queue, hides panel)
        messageController?.ResetState();
        
        if (EnableDebugLogs)
            this.Log("Reset to defaults completed", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for wave settings
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for wave settings
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}
