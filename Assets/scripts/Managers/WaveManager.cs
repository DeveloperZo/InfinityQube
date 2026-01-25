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

    // Statistics
    private int normalCubesCaptured = 0;
    private int blueCubesCaptured = 0;
    private int reinforcedCubesCaptured = 0;
    private int cubesEscaped = 0;
    /// <summary>
    /// Tracks Unit cube escapes for row penalty system.
    /// When unitCubesEscaped >= grid.Width, the bottom row is removed as a penalty.
    /// Counter resets after penalty is applied and at the start of each new wave.
    /// </summary>
    private int unitCubesEscaped = 0;
    
    /// <summary>
    /// Tracks player deaths for row penalty system.
    /// When playerDeaths >= 2, the bottom row is removed as a penalty.
    /// Counter resets after penalty is applied and at the start of each new wave.
    /// </summary>
    private int playerDeaths = 0;
    private int markersPlaced = 0;
    private int detonationsUsed = 0;

    // Wave Completion Tracking
    private int totalNonBlackCubes = 0;
    private int processedNonBlackCubes = 0;

    // Internal State
    private Coroutine waveCoroutine;
    private bool isPaused = false;
    private Queue<WaveMessage> pendingMessages = new Queue<WaveMessage>();
    private bool isProcessingMessageQueue = false;
    
    // Grid Height Override Tracking
    private int stageDefaultGridHeight = 0;  // Stored when stage starts
    private int currentGridHeightOverride = 0;  // 0 = using stage default, >0 = overridden
    
    // Segment transition tracking
    private int currentSegmentIndex = 0;
    private bool isTransitioning = false;
    private bool isInLateralPhase = false; // Legacy - kept for compatibility
    private bool waveStoppedAtEdge = false; // True when entire wave has stopped at segment edge
    private List<CubeData> transitionCubeData = new List<CubeData>(); // Cubes to respawn after transition
    
    // SEGMENT CONTROLLER: Wave containment at segment edge
    private List<CubeData> originalWaveFormation = new List<CubeData>(); // Original wave for respawn
    private int originalWaveDepth = 0; // Number of rows in original wave
    private int segmentStartMoveStep = 0; // MoveStep when wave started on current segment
    private int movesUntilEdge = 0; // Calculated moves until front row reaches edge
    private HashSet<CubeManager> stoppedAtEdge = new HashSet<CubeManager>(); // Legacy - kept for compatibility
    private bool waveContainedAtEdge = false; // Legacy - kept for compatibility
    
    // SEGMENT CONTROLLER: Multi-segment properties
    public bool HasSegmentControllers => grid != null && grid.HasSegmentControllers;
    private int SegmentControllerCount => grid != null ? grid.SegmentControllerCount : 0;
    private bool IsOnTerminalSegment => currentSegmentIndex >= SegmentControllerCount - 1;
    public GridSegmentController CurrentSegmentController => 
        grid != null && currentSegmentIndex < grid.SegmentControllerCount ? 
        grid.GetSegmentController(currentSegmentIndex) : null;
    
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

        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (grid == null) this.LogError("GridManager not found!");
        if (cubePrefabs == null || cubePrefabs.Length < 3) this.LogError("Need at least 3 cube prefabs!");
        if (audioManager == null) this.LogWarning("AudioManager not found! Audio events will not be triggered.", showDebugInfo);
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

        // ADVANCED GRID: After segment 0 clears, spawn same wave at segment 1's entry point
        if (grid != null && grid.HasMultipleSegments && currentSegmentIndex == 0)
        {
            Debug.Log("[WaveManager] Segment 0 cleared - transitioning to segment 1");
            
            // Wait a moment for visual effect
            yield return new WaitForSeconds(1.0f);
            
            // Switch to segment 1
            currentSegmentIndex = 1;
            grid.SetActiveSegment(1);
            
            // Spawn the same wave at segment 1's entry point
            SpawnWaveAtSegment1Entry();
            
            // Trigger camera rotation after spawning
            TriggerCameraRotation();
            
            // Continue wave loop on segment 1
            while (HasActiveCubes())
            {
                yield return ProcessWaveStep();
                yield return new WaitForSeconds(GetCurrentMoveInterval());
            }
        }

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

    #region Segment Transition (Advanced Grid)
    
    /// <summary>
    /// ADVANCED GRID: Checks if cubes have entered the overlap zone and should trigger transition.
    /// </summary>
    private void CheckSegmentTransition()
    {
        // Debug: Log transition check status
        Debug.Log($"[WaveManager] CheckSegmentTransition: isTransitioning={isTransitioning}, grid={grid != null}, HasMultipleSegments={grid?.HasMultipleSegments}, SegmentCount={grid?.SegmentCount}");
        
        if (isTransitioning || grid == null || !grid.HasMultipleSegments)
            return;
        
        var overlapBounds = grid.GetSegment1OverlapBounds();
        Debug.Log($"[WaveManager] Overlap bounds: minY={overlapBounds.minY}, maxY={overlapBounds.maxY}");
        
        if (overlapBounds.minY < 0)
            return;
        
        // Check if any cubes are in the overlap zone
        var cubesInOverlap = activeCubes.Where(c => 
            c != null && !c.isDestroyed && 
            c.position.y >= overlapBounds.minY && 
            c.position.y <= overlapBounds.maxY
        ).ToList();
        
        // Debug: Log cube positions
        if (activeCubes.Count > 0)
        {
            var positions = activeCubes.Where(c => c != null && !c.isDestroyed).Select(c => c.position.y).Distinct().OrderBy(y => y);
            Debug.Log($"[WaveManager] Active cube rows: {string.Join(", ", positions)} | Cubes in overlap: {cubesInOverlap.Count}");
        }
        
        if (cubesInOverlap.Count > 0)
        {
            DebugLog($"🔄 SEGMENT TRANSITION: {cubesInOverlap.Count} cubes entered overlap zone");
            StartCoroutine(PerformSegmentTransition(cubesInOverlap));
        }
    }
    
    /// <summary>
    /// ADVANCED GRID: Performs the segment transition - fall over effect, then respawn rotated.
    /// </summary>
    private IEnumerator PerformSegmentTransition(List<CubeManager> cubesInOverlap)
    {
        isTransitioning = true;
        
        // Store cube data for respawn (before destroying them)
        transitionCubeData.Clear();
        foreach (var cube in cubesInOverlap)
        {
            if (cube != null && !cube.isDestroyed)
            {
                // Store the cube's data for respawn in segment 2
                transitionCubeData.Add(new CubeData
                {
                    type = cube.type,
                    position = cube.position,
                    level = cube.level
                });
            }
        }
        
        // Play fall-over effect and destroy cubes
        yield return StartCoroutine(PlayFallOverEffect(cubesInOverlap));
        
        // Remove transitioned cubes from active list
        foreach (var cube in cubesInOverlap)
        {
            activeCubes.Remove(cube);
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }
        
        // Wait a moment for visual effect
        yield return new WaitForSeconds(0.3f);
        
        // Switch to segment 2
        currentSegmentIndex = 1;
        grid.SetActiveSegment(1);
        
        // Trigger camera rotation (if camera system exists)
        TriggerCameraRotation();
        
        // Wait for camera rotation
        yield return new WaitForSeconds(0.5f);
        
        // Respawn cubes at segment 2's spawn position
        RespawnCubesAtSegment2();
        
        isTransitioning = false;
        DebugLog($"🔄 SEGMENT TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// ADVANCED GRID: Plays the fall-over visual effect for transitioning cubes.
    /// </summary>
    private IEnumerator PlayFallOverEffect(List<CubeManager> cubes)
    {
        // Filter out any null or destroyed cubes first
        var validCubes = cubes.Where(c => c != null && c.gameObject != null && !c.isDestroyed).ToList();
        if (validCubes.Count == 0)
        {
            DebugLog($"🎬 No valid cubes for fall-over effect");
            yield break;
        }
        
        float fallDuration = 0.4f;
        float elapsed = 0f;
        
        // Store initial rotations
        var initialRotations = new Dictionary<CubeManager, Quaternion>();
        foreach (var cube in validCubes)
        {
            initialRotations[cube] = cube.transform.rotation;
        }
        
        // Animate fall-over (rotate 90 degrees forward)
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            float angle = Mathf.Lerp(0f, 90f, t);
            
            foreach (var cube in validCubes)
            {
                if (cube != null && cube.gameObject != null && initialRotations.ContainsKey(cube))
                {
                    // Rotate around X axis (fall forward)
                    cube.transform.rotation = initialRotations[cube] * Quaternion.Euler(angle, 0f, 0f);
                    
                    // Also move down slightly
                    Vector3 pos = cube.transform.position;
                    pos.y = Mathf.Lerp(2f, 0.5f, t);
                    cube.transform.position = pos;
                }
            }
            
            yield return null;
        }
        
        DebugLog($"🎬 Fall-over effect completed for {validCubes.Count} cubes");
    }
    
    /// <summary>
    /// ADVANCED GRID: Triggers the camera to transition to the current segment.
    /// </summary>
    private void TriggerCameraRotation()
    {
        var cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow == null)
        {
            DebugLog("⚠️ No CameraFollow found for segment transition");
            return;
        }
        
        // SEGMENT CONTROLLER: Use segment controller's camera settings
        if (HasSegmentControllers && CurrentSegmentController != null)
        {
            cameraFollow.TransitionToSegment(CurrentSegmentController);
            DebugLog($"📷 Camera transitioning to segment {currentSegmentIndex} using segment controller settings");
        }
        else
        {
            // Legacy: Use segment index
            cameraFollow.RotateForSegment(currentSegmentIndex);
            DebugLog($"📷 Camera rotation triggered for segment {currentSegmentIndex}");
        }
    }
    
    /// <summary>
    /// ADVANCED GRID: Respawns cubes at segment 2's spawn position with 90° rotation.
    /// </summary>
    private void RespawnCubesAtSegment2()
    {
        if (grid == null || grid.SegmentCount < 2)
            return;
        
        var segment2 = grid.Segments[1];
        int spawnRow = segment2.GetSpawnRow();
        
        DebugLog($"🔄 Respawning {transitionCubeData.Count} cubes at segment 2, row {spawnRow}");
        
        foreach (var cubeData in transitionCubeData)
        {
            // Create new position in segment 2's coordinate space
            // The X position maps to the same column, Y is the spawn row
            Vector2Int seg2Position = new Vector2Int(cubeData.position.x, spawnRow);
            
            // Get world position from segment 2
            Vector3 spawnWorldPos = segment2.LocalToWorldPosition(seg2Position.x, seg2Position.y, grid.TileSize, 2f);
            
            // Spawn the cube
            int prefabIndex = (int)cubeData.type;
            if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length)
            {
                GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], spawnWorldPos, segment2.GetWorldRotation());
                
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                var spawnData = new CubeData
                {
                    type = cubeData.type,
                    position = seg2Position,
                    level = cubeData.level
                };
                
                cube.Init(grid, spawnData, 2f);
                activeCubes.Add(cube);
                
                DebugLog($"  Respawned {cubeData.type} at segment 2 position ({seg2Position.x}, {seg2Position.y})");
            }
        }
        
        transitionCubeData.Clear();
    }
    
    #region Segment Controller Transitions
    
    // Track the MoveStep when first cube reached edge (for row offset calculation)
    private int transitionStartMoveStep = -1;
    
    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the edge of the current segment.
    /// Returns true if the cube should be queued for transition (not terminal segment).
    /// Returns false if this is the terminal segment (cube truly escapes).
    /// </summary>
    public bool HandleCubeAtSegmentEdge(CubeManager cube)
    {
        if (!HasSegmentControllers)
            return false; // Not using segment controllers, use legacy escape
        
        // If we're on the terminal segment, this is a real escape
        if (IsOnTerminalSegment)
        {
            DebugLog($"🚨 TERMINAL ESCAPE: {cube.type} escaped from terminal segment {currentSegmentIndex}");
            return false;
        }
        
        // Queue this cube for segment transition
        DebugLog($"🔄 SEGMENT EDGE: {cube.type} at edge of segment {currentSegmentIndex}, queuing for transition");
        
        // Track when first cube reaches edge to calculate row offsets
        if (transitionStartMoveStep < 0)
        {
            transitionStartMoveStep = MoveStep;
        }
        
        // Calculate row offset: cubes from row N of the wave reach the edge N steps after front row
        int rowOffset = MoveStep - transitionStartMoveStep;
        
        // Store cube data for respawn (only if not already captured/destroyed)
        if (!cube.isDestroyed)
        {
            transitionCubeData.Add(new CubeData
            {
                type = cube.type,
                position = new Vector2Int(cube.position.x, rowOffset), // Store X column and row offset
                level = cube.level
            });
        }
        
        return true; // Cube will be transitioned, not escaped
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the segment edge and STOPS (doesn't escape).
    /// </summary>
    public void HandleCubeStoppedAtEdge(CubeManager cube)
    {
        if (cube == null || cube.isDestroyed) return;
        
        stoppedAtEdge.Add(cube);
        DebugLog($"🛑 EDGE STOP: {cube.type} stopped at edge ({cube.position.x}, {cube.position.y}), direction: {cube.CurrentDirection}");
        
        // Check if wave is ready for transition
        var currentSegment = CurrentSegmentController;
        if (currentSegment != null && cube.CurrentDirection != currentSegment.localDirection)
        {
            // Cube is moving laterally (toward next segment) - check for segment transition
            CheckLateralSegmentTransition();
        }
        else
        {
            // Cube is moving in primary direction (down) - check for containment
            CheckWaveContainmentAtEdge();
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if the wave's leading edge has reached the segment boundary
    /// while moving laterally toward the next segment.
    /// Now handled by CheckWaveAtSegmentEdge - this is kept for backwards compatibility.
    /// </summary>
    private void CheckLateralSegmentTransition()
    {
        // Now handled by CheckWaveAtSegmentEdge which is called from MoveCubesForward
        CheckWaveAtSegmentEdge();
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Transitions cubes from current segment to next segment
    /// when they've reached the lateral boundary.
    /// </summary>
    private IEnumerator PerformLateralSegmentTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        DebugLog($"🔄 LATERAL TRANSITION: Moving from segment {currentSegmentIndex} to {currentSegmentIndex + 1}");
        
        // Store original wave for respawn on new segment
        // Convert actual positions to (column, rowOffset) format
        transitionCubeData.Clear();
        
        // Find the front row (highest Y) to calculate offsets
        int frontRow = 0;
        foreach (var cubeData in originalWaveFormation)
        {
            frontRow = Mathf.Max(frontRow, cubeData.position.y);
        }
        
        foreach (var cubeData in originalWaveFormation)
        {
            // Convert to (column, rowOffset) format
            int rowOffset = frontRow - cubeData.position.y;
            
            transitionCubeData.Add(new CubeData
            {
                type = cubeData.type,
                position = new Vector2Int(cubeData.position.x, rowOffset),
                level = cubeData.level
            });
        }
        
        // Destroy current cubes
        var currentCubes = activeCubes.Where(c => c != null && !c.isDestroyed).ToList();
        foreach (var cube in currentCubes)
        {
            if (cube.gameObject != null) Destroy(cube.gameObject);
        }
        activeCubes.Clear();
        stoppedAtEdge.Clear();
        
        // Advance to next segment
        currentSegmentIndex++;
        DebugLog($"🔄 Advanced to segment {currentSegmentIndex}");
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        // Respawn full wave on new segment
        RespawnCubesAtSegmentController();
        
        // Reset containment tracking for new segment
        waveContainedAtEdge = false;
        originalWaveFormation.Clear();
        TrackOriginalWaveFormation();
        
        yield return new WaitForSeconds(0.3f);
        
        isTransitioning = false;
        DebugLog($"🔄 LATERAL TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// Pre-checks if ANY cube in the wave is at the segment edge.
    /// Called BEFORE processing cube movements to prevent race conditions.
    /// </summary>
    private void PreCheckWaveAtEdge()
    {
        var segment = CurrentSegmentController;
        if (segment == null) return;
        
        // Check if there's a next segment - if not, this is terminal
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return;
        
        // Check each cube to see if any is at the edge
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            bool atEdge = false;
            switch (cube.CurrentDirection)
            {
                case MovementDirection.Down:
                    atEdge = cube.position.y <= 0;
                    break;
                case MovementDirection.Up:
                    atEdge = cube.position.y >= segment.height - 1;
                    break;
                case MovementDirection.Left:
                    atEdge = cube.position.x <= 0;
                    break;
                case MovementDirection.Right:
                    atEdge = cube.position.x >= segment.width - 1;
                    break;
            }
            
            if (atEdge)
            {
                waveStoppedAtEdge = true;
                DebugLog($"🛑 PRE-CHECK: Cube at ({cube.position.x},{cube.position.y}) is at edge - stopping entire wave");
                return;
            }
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if the wave has reached the segment edge.
    /// Triggered when waveStoppedAtEdge flag is set by ShouldCubeStopAtEdge.
    /// </summary>
    private void CheckWaveAtSegmentEdge()
    {
        if (isTransitioning) return;
        
        // Check if there's a next segment - if not, this is terminal segment
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return;
        
        // Debug: Log every 5 moves
        int movesSinceStart = MoveStep - segmentStartMoveStep;
        if (movesSinceStart % 5 == 0)
        {
            DebugLog($"📍 Edge check: MoveStep={MoveStep}, waveStoppedAtEdge={waveStoppedAtEdge}");
        }
        
        // Check if wave has been flagged as stopped at edge
        if (!waveStoppedAtEdge)
        {
            return; // Not at edge yet
        }
        
        // ENTIRE WAVE has reached the edge - trigger transition
        DebugLog($"✅ WAVE AT EDGE: MoveStep={MoveStep}, triggering transition");
        StartCoroutine(PerformEdgeTransitionToNextSegment());
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Legacy method - kept for compatibility but now uses CheckWaveAtSegmentEdge.
    /// </summary>
    private void CheckWaveContainmentAtEdge()
    {
        CheckWaveAtSegmentEdge();
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Performs transition when wave reaches segment edge.
    /// 1. Stops entire wave
    /// 2. Respawns missing NON-infinity cubes at edge
    /// 3. Transitions cubes to segment 1's coordinate system (positioned above segment 1's grid)
    /// 4. Wave moves "down" in segment 1, bringing cubes onto the grid
    /// </summary>
    private IEnumerator PerformEdgeTransitionToNextSegment()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        var currentSegment = CurrentSegmentController;
        var nextSegment = grid.GetSegmentController(currentSegmentIndex + 1);
        
        if (nextSegment == null)
        {
            DebugLog("❌ Cannot transition: no next segment");
            isTransitioning = false;
            yield break;
        }
        
        DebugLog($"🔄 EDGE TRANSITION: Wave stopped at segment {currentSegmentIndex} edge");
        
        // Step 1: Identify and respawn missing NON-infinity cubes
        RespawnMissingCubesAtEdge(currentSegment);
        
        // Step 2: Transition all cubes to segment 1's coordinate system
        // Cubes will be positioned ABOVE segment 1's grid (y = height + rowOffset)
        // Each move forward will bring them down onto the grid
        TransitionCubesToNextSegment(currentSegment, nextSegment);
        
        // Step 3: Advance to next segment and reset flags
        currentSegmentIndex++;
        isInLateralPhase = false;
        waveStoppedAtEdge = false; // Allow wave to move again
        
        // Step 4: Calculate moves until cubes reach segment 1's edge
        // After TRANSPOSE: old columns become new rows above grid
        // Wave depth on new segment = old wave WIDTH (column count)
        int oldWaveWidth = currentSegment.width; // Approximate - could track more precisely
        if (activeCubes.Count > 0)
        {
            // Get actual max column from cubes before transpose was applied
            // After transpose, max newY = toHeight + maxOldColumn
            int maxY = activeCubes.Where(c => c != null && !c.isDestroyed).Max(c => c.position.y);
            oldWaveWidth = maxY - nextSegment.height + 1;
        }
        movesUntilEdge = nextSegment.height + oldWaveWidth;
        segmentStartMoveStep = MoveStep;
        
        DebugLog($"🔄 Now on segment {currentSegmentIndex}, wave at y={nextSegment.height} (above grid)");
        DebugLog($"🔄 {movesUntilEdge} moves to reach segment {currentSegmentIndex}'s edge");
        
        // Update original wave formation for this segment
        originalWaveFormation.Clear();
        TrackOriginalWaveFormation();
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        isTransitioning = false;
        DebugLog($"🔄 EDGE TRANSITION COMPLETE: Wave moving down on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// Respawns missing NON-infinity cubes at the edge to restore full wave formation.
    /// </summary>
    private void RespawnMissingCubesAtEdge(GridSegmentController segment)
    {
        if (segment == null) return;
        
        // Build set of current cube positions (column, rowOffset from front)
        var currentPositions = new HashSet<string>();
        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                // At edge, cube.position.y is the row offset from front (0 = front row)
                currentPositions.Add($"{cube.position.x},{cube.position.y}");
            }
        }
        
        int respawnCount = 0;
        
        // Check each cube in original formation
        foreach (var originalCube in originalWaveFormation)
        {
            // Skip infinity cubes - they can't be captured so should always be present
            if (originalCube.type == CubeType.Infinity) continue;
            
            // Calculate expected position at edge
            int maxY = originalWaveFormation.Max(c => c.position.y);
            int rowOffset = maxY - originalCube.position.y; // 0 = front row
            string posKey = $"{originalCube.position.x},{rowOffset}";
            
            // Check if cube exists at this position
            if (!currentPositions.Contains(posKey))
            {
                // Respawn this cube at the edge
                Vector2Int localPos = new Vector2Int(originalCube.position.x, rowOffset);
                Vector3 worldPos = segment.LocalToWorldPosition(localPos.x, localPos.y, 2f);
                
                int prefabIndex = (int)originalCube.type;
                if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length && cubePrefabs[prefabIndex] != null)
                {
                    GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, segment.WorldRotation);
                    var cube = cubeObj.GetComponent<CubeManager>();
                    if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                    
                    var spawnData = new CubeData
                    {
                        type = originalCube.type,
                        position = localPos,
                        level = originalCube.level
                    };
                    
                    cube.Init(grid, spawnData, 2f);
                    cube.transform.position = worldPos;
                    cube.transform.rotation = segment.WorldRotation;
                    cube.SetSegmentController(segment);
                    
                    activeCubes.Add(cube);
                    currentPositions.Add(posKey);
                    respawnCount++;
                }
            }
        }
        
        if (respawnCount > 0)
        {
            DebugLog($"🔄 Respawned {respawnCount} missing non-infinity cubes at edge");
        }
    }
    
    /// <summary>
    /// Transitions all cubes from current segment to next segment's coordinate system.
    /// TRANSPOSE: Since direction changes 90°, we swap rows and columns.
    /// - Segment 0 row 0 → Segment 1 column 0
    /// - Segment 0 column 4 → Segment 1 row 0 (enters first)
    /// - Segment 0 column 0 → Segment 1 row N (enters last)
    /// </summary>
    private void TransitionCubesToNextSegment(GridSegmentController fromSegment, GridSegmentController toSegment)
    {
        int toHeight = toSegment.height;
        int maxColumn = fromSegment.width - 1; // e.g., 4 for 5-wide grid
        
        DebugLog($"🔄 Transitioning {activeCubes.Count} cubes to segment {currentSegmentIndex + 1} (TRANSPOSE)");
        DebugLog($"🔄 maxColumn={maxColumn}, toHeight={toHeight}");
        
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            // Current position at edge of segment 0:
            // x = column (0 = left, 4 = right)
            // y = row (0 = front at edge, higher = back)
            int oldColumn = cube.position.x;
            int oldRow = cube.position.y;
            
            // TRANSPOSE for 90° direction change:
            // Old row becomes new column: row 0 → column 0
            // Old column becomes new row (INVERTED): column 4 → row 0 (enters first)
            int newX = oldRow;  // Row 0 at edge → Column 0
            int newY = toHeight + (maxColumn - oldColumn);  // Column 4 → closest to grid, Column 0 → furthest
            
            DebugLog($"  Cube {cube.type}: ({oldColumn},{oldRow}) -> ({newX},{newY}) [transpose]");
            
            // Update cube position
            cube.position = new Vector2Int(newX, newY);
            
            // Assign to new segment (this sets direction to toSegment.localDirection)
            cube.SetSegmentController(toSegment);
            cube.stoppedAtEdge = false; // Reset stop flag
            
            // Calculate world position - cubes above grid still need valid positions
            Vector3 worldPos = CalculateWorldPositionAboveGrid(toSegment, newX, newY);
            cube.transform.position = worldPos;
            cube.transform.rotation = toSegment.WorldRotation;
        }
    }
    
    /// <summary>
    /// Calculates world position for a cube that may be above the grid (y >= height).
    /// </summary>
    private Vector3 CalculateWorldPositionAboveGrid(GridSegmentController segment, int x, int y)
    {
        // If within grid bounds, use normal calculation
        if (y < segment.height)
        {
            return segment.LocalToWorldPosition(x, y, 2f);
        }
        
        // For positions above the grid, extrapolate based on grid spacing
        // Get positions at two rows to calculate the row direction vector
        Vector3 topRowPos = segment.LocalToWorldPosition(x, segment.height - 1, 2f);
        Vector3 prevRowPos = segment.LocalToWorldPosition(x, segment.height - 2, 2f);
        
        // Row direction: going from lower Y to higher Y (direction cubes come FROM)
        Vector3 rowDirection = (topRowPos - prevRowPos).normalized;
        
        // Calculate how many rows above the top row
        int rowsAbove = y - (segment.height - 1);
        
        // Extrapolate position above the grid
        return topRowPos + (rowDirection * rowsAbove * segment.tileSize);
    }
    
    /// <summary>
    /// LEGACY: Performs transition after wave is contained at edge.
    /// Respawns ONLY captured cubes to restore full wave, then changes direction toward next segment.
    /// Does NOT destroy existing cubes - they continue moving in the new direction.
    /// </summary>
    private IEnumerator PerformEdgeContainmentTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        var currentSegment = CurrentSegmentController;
        var nextSegment = grid.GetSegmentController(currentSegmentIndex + 1);
        
        if (nextSegment == null)
        {
            DebugLog("❌ Cannot transition: no next segment");
            isTransitioning = false;
            yield break;
        }
        
        DebugLog($"🔄 EDGE TRANSITION: Wave at segment {currentSegmentIndex} edge");
        
        // Determine direction toward next segment
        MovementDirection newDirection = GetDirectionTowardSegment(currentSegment, nextSegment);
        DebugLog($"🔄 New movement direction: {newDirection} (toward segment {currentSegmentIndex + 1})");
        
        // Find which cubes from original wave are MISSING (captured/destroyed)
        var currentCubePositions = new HashSet<Vector2Int>();
        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                currentCubePositions.Add(cube.position);
            }
        }
        
        // Identify missing cubes by comparing to original formation
        var missingCubes = new List<CubeData>();
        foreach (var originalCube in originalWaveFormation)
        {
            // Map original position to current edge position
            // Original wave was at spawn row, now cubes are at edge (row 0 for Down direction)
            int edgeRow = 0; // Front row at edge
            int rowOffset = originalWaveFormation.Max(c => c.position.y) - originalCube.position.y;
            Vector2Int expectedPosition = new Vector2Int(originalCube.position.x, rowOffset);
            
            // Check if any active cube is at this position
            bool found = false;
            foreach (var cube in activeCubes)
            {
                if (cube != null && !cube.isDestroyed && cube.position.x == expectedPosition.x)
                {
                    // Check if cube is within the wave depth from edge
                    int cubeRowOffset = cube.position.y;
                    if (cubeRowOffset == rowOffset)
                    {
                        found = true;
                        break;
                    }
                }
            }
            
            if (!found)
            {
                missingCubes.Add(new CubeData
                {
                    type = originalCube.type,
                    position = expectedPosition,
                    level = originalCube.level
                });
            }
        }
        
        DebugLog($"🔄 Found {missingCubes.Count} missing cubes to respawn");
        
        // Respawn missing cubes at the edge
        foreach (var cubeData in missingCubes)
        {
            // Calculate spawn position at edge
            int spawnRow = cubeData.position.y; // Row offset from front
            int column = cubeData.position.x;
            
            Vector2Int localPos = new Vector2Int(column, spawnRow);
            Vector3 spawnWorldPos = currentSegment.LocalToWorldPosition(localPos.x, localPos.y, 2f);
            Quaternion cubeRotation = currentSegment.WorldRotation;
            
            int prefabIndex = (int)cubeData.type;
            if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length && cubePrefabs[prefabIndex] != null)
            {
                GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                var spawnData = new CubeData
                {
                    type = cubeData.type,
                    position = localPos,
                    level = cubeData.level
                };
                
                cube.Init(grid, spawnData, 2f);
                cube.transform.position = spawnWorldPos;
                cube.transform.rotation = cubeRotation;
                
                cube.SetSegmentController(currentSegment);
                // Set the new direction immediately
                cube.SetMovementDirection(newDirection);
                activeCubes.Add(cube);
                
                DebugLog($"  ✅ Respawned {cubeData.type} at local ({localPos.x}, {localPos.y})");
            }
        }
        
        // Change direction for ALL existing cubes
        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                cube.SetMovementDirection(newDirection);
            }
        }
        
        DebugLog($"🔄 All {activeCubes.Count} cubes now moving {newDirection} toward segment {currentSegmentIndex + 1}");
        
        // Calculate new movesUntilEdge based on lateral distance to next segment
        // For now, use the segment width as the distance
        int lateralDistance = currentSegment.width;
        movesUntilEdge = lateralDistance;
        segmentStartMoveStep = MoveStep;
        
        DebugLog($"🔄 Lateral movement phase: {movesUntilEdge} moves until segment boundary");
        
        // Mark that we're now in lateral phase
        isInLateralPhase = true;
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(0.5f);
        }
        
        yield return new WaitForSeconds(0.2f);
        
        isTransitioning = false;
        DebugLog($"🔄 EDGE TRANSITION COMPLETE: Wave moving toward segment {currentSegmentIndex + 1}");
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Performs the actual transition to the next segment.
    /// Called after cubes have moved laterally across segment 0 and reached segment 1's boundary.
    /// </summary>
    private IEnumerator PerformActualSegmentTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        var nextSegment = grid.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null)
        {
            DebugLog("❌ Cannot transition: no next segment");
            isTransitioning = false;
            yield break;
        }
        
        DebugLog($"🔄 SEGMENT TRANSITION: Moving from segment {currentSegmentIndex} to {currentSegmentIndex + 1}");
        
        // Advance to next segment
        currentSegmentIndex++;
        isInLateralPhase = false;
        
        // Update all cubes to new segment
        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                // Assign to new segment (this updates direction to segment's localDirection)
                cube.SetSegmentController(nextSegment);
                
                // Remap position to new segment's coordinate system
                // The cube's X position becomes its row offset from segment edge
                // The cube's Y position resets based on entry point
                int newX = cube.position.y; // Old row becomes new column
                int newY = nextSegment.height - 1 - cube.position.x; // Old column becomes new row (inverted)
                
                cube.position = new Vector2Int(newX, newY);
                
                // Update world position
                Vector3 worldPos = nextSegment.LocalToWorldPosition(cube.position.x, cube.position.y, 2f);
                cube.transform.position = worldPos;
                cube.transform.rotation = nextSegment.WorldRotation;
            }
        }
        
        // Calculate new movesUntilEdge for segment 1
        movesUntilEdge = nextSegment.height - originalWaveDepth;
        segmentStartMoveStep = MoveStep;
        
        // Track new wave formation
        originalWaveFormation.Clear();
        TrackOriginalWaveFormation();
        
        DebugLog($"🔄 Now on segment {currentSegmentIndex}, {activeCubes.Count} cubes, {movesUntilEdge} moves to edge");
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        isTransitioning = false;
        DebugLog($"🔄 SEGMENT TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// Determines the movement direction to reach the next segment from current segment.
    /// </summary>
    private MovementDirection GetDirectionTowardSegment(GridSegmentController from, GridSegmentController to)
    {
        // Calculate the direction based on segment positions
        Vector3 fromCenter = from.transform.position;
        Vector3 toCenter = to.transform.position;
        Vector3 direction = (toCenter - fromCenter).normalized;
        
        // Convert to segment-local direction
        // Transform direction to local space of the current segment
        Vector3 localDir = from.transform.InverseTransformDirection(direction);
        
        // Determine primary direction
        if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
        {
            return localDir.x > 0 ? MovementDirection.Right : MovementDirection.Left;
        }
        else
        {
            return localDir.z > 0 ? MovementDirection.Up : MovementDirection.Down;
        }
    }
    
    /// <summary>
    /// Gets the row offset from the original wave's front row.
    /// </summary>
    private int GetOriginalRowOffset(int originalRow)
    {
        if (originalWaveFormation.Count == 0) return 0;
        
        int minOriginalRow = originalWaveFormation.Min(c => c.position.y);
        return originalRow - minOriginalRow;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if a grid position is occupied by another cube.
    /// Used for wave containment - cubes stop behind other cubes.
    /// </summary>
    public bool IsPositionOccupiedByCube(Vector2Int position, CubeManager excludeCube = null)
    {
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            if (cube == excludeCube) continue;
            
            if (cube.position == position)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Gets a cube at a position ONLY if it has stopped at the edge.
    /// Returns null if the position is empty or contains a cube that's still moving.
    /// This allows normal wave movement while enabling containment stacking.
    /// </summary>
    public CubeManager GetStoppedCubeAtPosition(Vector2Int position, CubeManager excludeCube = null)
    {
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            if (cube == excludeCube) continue;
            
            // Only return cube if it's at this position AND stopped at edge
            if (cube.position == position && cube.stoppedAtEdge)
                return cube;
        }
        return null;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if cube should stop at edge instead of escaping.
    /// Returns true if cube is at the segment edge (next move would escape).
    /// Only applies to non-terminal segments.
    /// </summary>
    public bool ShouldCubeStopAtEdge(CubeManager cube)
    {
        if (cube == null) return false;
        
        // If wave is flagged as stopped at edge, ALL cubes stop
        if (waveStoppedAtEdge) return true;
        
        // Check if there's a next segment - if not, allow escape (terminal segment)
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return false;
        
        // Get segment for bounds check
        var segment = cube.CurrentSegment ?? CurrentSegmentController;
        if (segment == null) return false;
        
        // Check if cube is at the edge position (next move would escape)
        bool atEdge = false;
        switch (cube.CurrentDirection)
        {
            case MovementDirection.Down:
                atEdge = cube.position.y <= 0;
                break;
            case MovementDirection.Up:
                atEdge = cube.position.y >= segment.height - 1;
                break;
            case MovementDirection.Left:
                atEdge = cube.position.x <= 0;
                break;
            case MovementDirection.Right:
                atEdge = cube.position.x >= segment.width - 1;
                break;
        }
        
        if (atEdge)
        {
            waveStoppedAtEdge = true; // Flag entire wave to stop
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if all cubes have reached the segment edge and transition should occur.
    /// </summary>
    public void CheckSegmentTransitionReady()
    {
        if (!HasSegmentControllers || IsOnTerminalSegment || isTransitioning)
            return;
        
        // With new edge containment logic, this is handled by CheckWaveContainmentAtEdge
        // Keep for backwards compatibility but the new flow uses HandleCubeStoppedAtEdge
        
        // Count ALL active cubes still on the grid (including Infinity)
        // For segment transitions, ALL cubes must reach the edge before transitioning
        int activeCubesOnGrid = activeCubes.Count(c => c != null && !c.isDestroyed);
        
        // If all cubes have reached the edge (either queued for transition or captured)
        if (activeCubesOnGrid == 0 && transitionCubeData.Count > 0)
        {
            DebugLog($"🔄 SEGMENT TRANSITION READY: {transitionCubeData.Count} cubes ready to transition to segment {currentSegmentIndex + 1}");
            StartCoroutine(PerformSegmentControllerTransition());
        }
        else if (activeCubesOnGrid == 0 && transitionCubeData.Count == 0)
        {
            // All cubes captured - wave complete!
            DebugLog("✅ All cubes captured on segment - wave complete!");
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Performs the full segment transition sequence.
    /// </summary>
    private IEnumerator PerformSegmentControllerTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        DebugLog($"🔄 SEGMENT TRANSITION: Starting transition from segment {currentSegmentIndex} to {currentSegmentIndex + 1}");
        
        // Clean up remaining infinity cubes (they fall off)
        var infinityCubes = activeCubes.Where(c => c != null && !c.isDestroyed && c.type == CubeType.Infinity).ToList();
        if (infinityCubes.Count > 0)
        {
            DebugLog($"🔄 Removing {infinityCubes.Count} infinity cubes from previous segment");
            yield return StartCoroutine(PlayFallOverEffect(infinityCubes));
            
            foreach (var cube in infinityCubes)
            {
                activeCubes.Remove(cube);
                if (cube != null && cube.gameObject != null)
                {
                    Destroy(cube.gameObject);
                }
            }
        }
        
        // Advance to next segment
        currentSegmentIndex++;
        DebugLog($"🔄 Advanced to segment {currentSegmentIndex}");
        
        // NOTE: Camera now auto-follows player's segment in CameraFollow.LateUpdate()
        // No need to force camera rotation here - player may still be on previous segment
        
        // Brief delay before spawning cubes
        yield return new WaitForSeconds(0.3f);
        
        // Grant player brief invulnerability since cubes are about to spawn
        // This prevents instant death if player happens to be at spawn location
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        // Respawn cubes at new segment
        RespawnCubesAtSegmentController();
        
        // Wait for respawn to be visible
        yield return new WaitForSeconds(0.5f);
        
        isTransitioning = false;
        DebugLog($"🔄 SEGMENT TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Respawns queued cubes at the new segment.
    /// Cubes maintain the SAME configuration (no rotation) - only movement direction changes.
    /// The visual "rotation" effect comes from the camera and movement direction change.
    /// </summary>
    private void RespawnCubesAtSegmentController()
    {
        var currentSegment = CurrentSegmentController;
        if (currentSegment == null)
        {
            DebugLog($"❌ Cannot respawn: No segment controller for index {currentSegmentIndex}");
            return;
        }
        
        // Spawn at segment's spawn row (entry point)
        int baseSpawnRow = currentSegment.SpawnRow;
        
        DebugLog($"🔄 Respawning {transitionCubeData.Count} cubes at segment {currentSegmentIndex}");
        DebugLog($"   Base spawn row: {baseSpawnRow}, segment: {currentSegment.width}x{currentSegment.height}");
        DebugLog($"   Movement direction: {currentSegment.localDirection}");
        
        foreach (var cubeData in transitionCubeData)
        {
            // cubeData.position.x = original column
            // cubeData.position.y = row offset from front of wave (0 = front row, 1 = second row, etc.)
            int column = cubeData.position.x;
            int rowOffset = cubeData.position.y;
            
            // NO ROTATION - keep same column and row offset
            // Front row spawns at baseSpawnRow, subsequent rows spawn behind (lower Y)
            int spawnRow = baseSpawnRow - rowOffset;
            
            // Clamp to valid grid bounds
            spawnRow = Mathf.Clamp(spawnRow, 0, currentSegment.height - 1);
            column = Mathf.Clamp(column, 0, currentSegment.width - 1);
            
            Vector2Int localPos = new Vector2Int(column, spawnRow);
            
            // Calculate spawn world position using current segment's coordinate system
            Vector3 spawnWorldPos = currentSegment.LocalToWorldPosition(localPos.x, localPos.y, 2f);
            
            // Apply current segment's rotation so cubes face the correct direction
            Quaternion cubeRotation = currentSegment.WorldRotation;
            
            // Spawn the cube
            int prefabIndex = (int)cubeData.type;
            if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length && cubePrefabs[prefabIndex] != null)
            {
                // Instantiate at origin first - Init will set wrong position, we override after
                GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
                
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                // Initialize cube properties
                var spawnData = new CubeData
                {
                    type = cubeData.type,
                    position = localPos,
                    level = cubeData.level
                };
                
                cube.Init(grid, spawnData, 2f);
                
                // CRITICAL: Override position and rotation AFTER Init
                // Init uses grid.GridToWorldPosition which only works for segment 0
                // We need to use the segment controller's coordinate system
                cube.transform.position = spawnWorldPos;
                cube.transform.rotation = cubeRotation;
                
                // Assign to current segment (sets movement direction)
                cube.SetSegmentController(currentSegment);
                activeCubes.Add(cube);
                
                DebugLog($"  ✅ Respawned {cubeData.type} at local ({localPos.x}, {localPos.y}) world {spawnWorldPos}");
            }
        }
        
        transitionCubeData.Clear();
        transitionStartMoveStep = -1; // Reset for next transition
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Resets segment tracking to initial state.
    /// </summary>
    public void ResetSegmentState()
    {
        currentSegmentIndex = 0;
        isTransitioning = false;
        isInLateralPhase = false;
        waveStoppedAtEdge = false;
        transitionCubeData.Clear();
        transitionStartMoveStep = -1;
        
        // Reset edge containment tracking
        originalWaveFormation.Clear();
        stoppedAtEdge.Clear();
        waveContainedAtEdge = false;
        originalWaveDepth = 0;
        
        DebugLog("🔄 Segment state reset to segment 0");
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Repositions player to the lowest row of the current segment.
    /// </summary>
    private void RepositionPlayerForSegment()
    {
        var currentSegment = CurrentSegmentController;
        if (currentSegment == null || player == null)
        {
            DebugLog("❌ Cannot reposition player: missing segment controller or player");
            return;
        }
        
        // Position player at lowest row (Y=0), center column
        int centerX = currentSegment.width / 2;
        int bottomY = 0;
        
        // Use PlayerManager's segment-aware positioning method
        // This updates transform position, currentTilePosition, and playerStartPosition
        player.SetPositionOnSegment(currentSegment, centerX, bottomY);
        
        DebugLog($"🎮 Player repositioned to segment {currentSegmentIndex} at local ({centerX}, {bottomY})");
    }
    
    #endregion
    
    /// <summary>
    /// ADVANCED GRID: Checks if we should transition to the next segment.
    /// Returns true when all non-infinity cubes are cleared and we're still on segment 0.
    /// </summary>
    private bool ShouldTransitionToNextSegment()
    {
        if (grid == null || !grid.HasMultipleSegments)
            return false;
        
        // Only transition from segment 0
        if (currentSegmentIndex != 0)
            return false;
        
        // Check if only infinity cubes (or no cubes) remain
        bool onlyInfinityRemaining = activeCubes.All(c => c == null || c.isDestroyed || c.type == CubeType.Infinity);
        
        if (onlyInfinityRemaining && activeCubes.Count > 0)
        {
            Debug.Log($"[WaveManager] ShouldTransitionToNextSegment: Only infinity cubes remaining ({activeCubes.Count(c => c != null && !c.isDestroyed)})");
        }
        
        return onlyInfinityRemaining;
    }
    
    /// <summary>
    /// ADVANCED GRID: Handles transition from segment 0 to segment 1, including:
    /// - Destroying remaining infinity cubes (they fall off the edge)
    /// - Waiting for transition timer
    /// - Spawning new wave at segment 1 entry
    /// - Triggering camera rotation
    /// </summary>
    private IEnumerator HandleSegmentTransitionAndRespawn()
    {
        isTransitioning = true;
        Debug.Log("[WaveManager] HandleSegmentTransitionAndRespawn: Starting transition to segment 1");
        
        // Destroy remaining infinity cubes (they fall off the edge)
        var remainingCubes = activeCubes.Where(c => c != null && !c.isDestroyed).ToList();
        if (remainingCubes.Count > 0)
        {
            Debug.Log($"[WaveManager] Destroying {remainingCubes.Count} infinity cubes at segment edge");
            
            // Play fall-off effect
            yield return StartCoroutine(PlayFallOverEffect(remainingCubes));
            
            // Destroy the cubes
            foreach (var cube in remainingCubes)
            {
                activeCubes.Remove(cube);
                if (cube != null && cube.gameObject != null)
                {
                    Destroy(cube.gameObject);
                }
            }
        }
        
        // Wait for transition timer
        Debug.Log("[WaveManager] Waiting for segment transition timer...");
        yield return new WaitForSeconds(1.5f);
        
        // Switch to segment 1
        currentSegmentIndex = 1;
        grid.SetActiveSegment(1);
        Debug.Log("[WaveManager] Switched to segment 1");
        
        // Spawn new wave at segment 1's entry point
        SpawnWaveAtSegment1Entry();
        
        // Camera will rotate after first move forward (handled in MoveCubesForward)
        firstMoveOnSegment1 = true;
        
        isTransitioning = false;
        Debug.Log("[WaveManager] Segment transition complete - wave spawned on segment 1");
    }
    
    // Track if this is the first move on segment 1 (for camera rotation)
    private bool firstMoveOnSegment1 = false;
    
    /// <summary>
    /// ADVANCED GRID: Spawns the wave at segment 1's entry point.
    /// Cubes spawn at segment 1's highest row (entry) and move in -Y (local) which is -X (world, LEFT).
    /// </summary>
    private void SpawnWaveAtSegment1Entry()
    {
        if (grid == null || grid.SegmentCount < 2)
            return;
        
        var segment1 = grid.Segments[1];
        int entryRow = segment1.height - 1; // Top row of segment 1 (entry point)
        
        Debug.Log($"[WaveManager] Spawning wave at segment 1, entry row {entryRow}, segment rotation={segment1.rotationAngle}°");
        
        // Use the current wave configuration to spawn cubes
        if (useWaveConfiguration && currentWaveIndex < waveConfiguration.Count)
        {
            var waveData = waveConfiguration[currentWaveIndex];
            
            // Spawn cubes from wave data at the entry row
            foreach (var cubeData in waveData.cubes)
            {
                Vector2Int spawnPos = new Vector2Int(cubeData.position.x, entryRow);
                Vector3 worldPos = segment1.LocalToWorldPosition(spawnPos.x, spawnPos.y, grid.TileSize, 2f);
                
                int prefabIndex = (int)cubeData.type;
                if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length)
                {
                    // Instantiate at correct world position with segment rotation
                    GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, segment1.GetWorldRotation());
                    
                    var cube = cubeObj.GetComponent<CubeManager>();
                    if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                    
                    var spawnData = new CubeData
                    {
                        type = cubeData.type,
                        position = spawnPos,
                        level = cubeData.level
                    };
                    
                    // Init cube but don't let it reposition (we already set correct position)
                    cube.Init(grid, spawnData, 2f);
                    // Override position back to correct world position (Init repositions to segment 0)
                    cube.transform.position = worldPos;
                    cube.transform.rotation = segment1.GetWorldRotation();
                    // Tell the cube it's on segment 1 for correct coordinate system
                    cube.SetSegment(1);
                    
                    activeCubes.Add(cube);
                    
                    Debug.Log($"  Spawned {cubeData.type} at segment 1 local ({spawnPos.x}, {spawnPos.y}) -> world {worldPos}");
                }
            }
        }
        else
        {
            // Fallback: spawn a simple wave pattern
            Debug.Log("[WaveManager] No wave config - spawning default pattern on segment 1");
            for (int x = 0; x < grid.Width; x++)
            {
                Vector2Int spawnPos = new Vector2Int(x, entryRow);
                Vector3 worldPos = segment1.LocalToWorldPosition(spawnPos.x, spawnPos.y, grid.TileSize, 2f);
                
                CubeType type = (x % 2 == 0) ? CubeType.Unit : CubeType.Infinity;
                int prefabIndex = (int)type;
                
                if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length)
                {
                    GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, segment1.GetWorldRotation());
                    
                    var cube = cubeObj.GetComponent<CubeManager>();
                    if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                    
                    var spawnData = new CubeData
                    {
                        type = type,
                        position = spawnPos,
                        level = 0
                    };
                    
                    cube.Init(grid, spawnData, 2f);
                    // Override position back to correct world position
                    cube.transform.position = worldPos;
                    cube.transform.rotation = segment1.GetWorldRotation();
                    // Tell the cube it's on segment 1
                    cube.SetSegment(1);
                    
                    activeCubes.Add(cube);
                }
            }
        }
        
        Debug.Log($"[WaveManager] Spawned {activeCubes.Count} cubes at segment 1 entry");
    }
    
    /// <summary>
    /// ADVANCED GRID: Resets segment tracking (call when wave/stage resets).
    /// </summary>
    public void ResetSegmentTracking()
    {
        currentSegmentIndex = 0;
        isTransitioning = false;
        transitionCubeData.Clear();
        transitionStartMoveStep = -1; // Reset row offset tracking
        
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
        originalWaveFormation.Clear();
        stoppedAtEdge.Clear();
        waveContainedAtEdge = false;

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
    /// SEGMENT CONTROLLER: Records the original wave formation for respawn at segment edge.
    /// Calculates the wave depth and how many moves until the front row reaches the edge.
    /// </summary>
    private void TrackOriginalWaveFormation()
    {
        originalWaveFormation.Clear();
        
        if (activeCubes.Count == 0) return;
        
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            // Store original cube data
            originalWaveFormation.Add(new CubeData
            {
                type = cube.type,
                position = cube.position,
                level = cube.level
            });
            
            // Track row range
            minRow = Mathf.Min(minRow, cube.position.y);
            maxRow = Mathf.Max(maxRow, cube.position.y);
        }
        
        originalWaveDepth = (maxRow - minRow) + 1;
        
        // Record starting move step and calculate moves until edge
        // For Down movement: front row (minRow value among cubes with highest Y) needs to reach row 0
        // The front row is at maxRow, and needs (maxRow - 0) = maxRow moves to reach edge
        segmentStartMoveStep = MoveStep;
        movesUntilEdge = maxRow; // Front row at maxRow needs maxRow moves to reach row 0
        
        DebugLog($"📊 Wave tracked: {originalWaveFormation.Count} cubes, depth={originalWaveDepth}, front at row {maxRow}");
        DebugLog($"📊 Segment starts at MoveStep={segmentStartMoveStep}, edge in {movesUntilEdge} moves (MoveStep={segmentStartMoveStep + movesUntilEdge})");
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

    #region Message System
    private void ShowInitialMessages()
    {
        // Start coroutine to delay sequences until camera has panned to position
        // Sequences handle messages internally, so we don't need to check for messages
        StartCoroutine(ShowInitialMessagesDelayed());
    }
    
    private IEnumerator ShowInitialMessagesDelayed()
    {
        // Wait for camera to pan to default position (CameraFollow uses 0.25s smooth time)
        // Add extra buffer to ensure camera is fully positioned before showing messages
        yield return new WaitForSeconds(0.6f);

        // Process initial sequences (sequences handle messages internally)
        ProcessInitialSequences();
    }
    
    /// <summary>
    /// Processes highlight sequences at move step 0 (wave start)
    /// Only executes sequences with DisplayMoveStep == 0 that don't have trigger conditions
    /// </summary>
    private void ProcessInitialSequences()
    {
        if (CurrentWave?.highlightSequences == null || messageHighlightManager == null) return;
        
        var initialSequences = CurrentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == 0 &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);
        
        foreach (var sequence in initialSequences)
        {
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }

    // ProcessStepMessages removed - sequences now handle all messages via ProcessStepSequences
    
    /// <summary>
    /// Processes highlight sequences at the current move step
    /// </summary>
    private void ProcessStepSequences()
    {
        if (CurrentWave?.highlightSequences == null || messageHighlightManager == null) return;
        
        DebugLog($"ProcessStepSequences: Checking sequences for MoveStep={MoveStep}, total sequences={CurrentWave.highlightSequences.Count}");
        
        // Get sequences for current move step that aren't event-triggered
        var stepSequences = CurrentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == MoveStep &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);
        
        var sequencesList = stepSequences.ToList();
        DebugLog($"ProcessStepSequences: Found {sequencesList.Count} sequences to execute at MoveStep={MoveStep}");
        
        foreach (var sequence in sequencesList)
        {
            DebugLog($"ProcessStepSequences: Executing sequence with DisplayMoveStep={sequence.DisplayMoveStep}, targetType={sequence.targetType}, targetPosition=({sequence.targetPosition.x}, {sequence.targetPosition.y})");
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }

    /// <summary>
    /// Processes highlight sequences at wave end (DisplayMoveStep == -1)
    /// </summary>
    private void ProcessEndSequences()
    {
        if (CurrentWave?.highlightSequences == null || messageHighlightManager == null) return;

        var endSequences = CurrentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == -1 &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);

        foreach (var sequence in endSequences)
        {
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }

    /// <summary>
    /// POC: Show wave completion feedback message with progress and statistics.
    /// For Stage 1 tutorial, all waves pause for player input.
    /// </summary>
    private void ShowWaveCompletionMessage()
    {
        if (!showMessages) return;

        int waveNum = currentWaveIndex + 1;
        int totalWaves = waveConfiguration != null && waveConfiguration.Count > 0 ? waveConfiguration.Count : 1;
        
        // Simple, minimal message
        string message = $"Wave {waveNum}/{totalWaves}\n\n";
        
        // Add statistics only if there were failures
        int totalCaptured = normalCubesCaptured + blueCubesCaptured + reinforcedCubesCaptured;
        if (cubesEscaped > 0)
        {
            message += $"Captured: {totalCaptured}\nEscaped: {cubesEscaped}\n\n";
        }
        
        // Simple prompt
        message += "Press K to continue";
        
        // Use MessageHighlightManager if available, otherwise fallback
        if (messageHighlightManager != null)
        {
            messageHighlightManager.ShowMessage(message, true, 0f, MoveStep);
        }
        else
        {
            var completionMsg = new WaveMessage
            {
                Message = message,
                RequirePause = true, // POC: All tutorial waves pause for feedback
                AutoHideDelay = 0f
            };
            ShowMessage(completionMsg);
        }
        DebugLog($"ShowWaveCompletionMessage: Wave {waveNum}/{totalWaves} - Captured: {totalCaptured}, Escaped: {cubesEscaped}");
    }

    public void ShowMessage(WaveMessage message)
    {
        pendingMessages.Enqueue(message);
        if (!isProcessingMessageQueue && showMessages)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }

    private IEnumerator ProcessMessageQueue()
    {
        isProcessingMessageQueue = true;

        while (pendingMessages.Count > 0 && showMessages)
        {
            var message = pendingMessages.Dequeue();
            yield return DisplayMessage(message);
        }

        isProcessingMessageQueue = false;
    }

    private IEnumerator DisplayMessage(WaveMessage message)
    {
        if (messagePanel != null && messageText != null && showMessages)
        {
            messagePanel.SetActive(true);
            messageText.text = message.Message;
            
            // Notify statistics manager about message display
            if (PlayerStatisticsManager.Instance != null)
            {
                PlayerStatisticsManager.Instance.OnMessageDisplayed(message.Message, MoveStep);
            }

            bool wasSkipped = false;
            if (message.RequirePause)
            {
                isPaused = true;
                Time.timeScale = 0f;
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                Time.timeScale = 1f;
                isPaused = false;
            }
            else if (message.AutoHideDelay > 0)
            {
                float timer = 0f;
                while (timer < message.AutoHideDelay)
                {
                    if (Input.GetKeyDown(KeyCode.K)) // Allow skipping auto-hide messages
                    {
                        wasSkipped = true;
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            messagePanel.SetActive(false);
            
            // Notify statistics manager about message dismissal
            if (PlayerStatisticsManager.Instance != null)
            {
                PlayerStatisticsManager.Instance.OnMessageDismissed(message.Message, wasSkipped);
            }
        }
    }
    #endregion

    #region Statistics & Events
    public void OnCubeCaptured(CubeType cubeType)
    {
        switch (cubeType)
        {
            case CubeType.Unit: normalCubesCaptured++; break;
            case CubeType.Matrix: blueCubesCaptured++; break;
            case CubeType.Recursion: reinforcedCubesCaptured++; break;
        }

        // Trigger cube captured audio event
        if (audioManager != null)
        {
            // Find the captured cube to get its position
            var capturedCube = activeCubes.FirstOrDefault(c => c != null && c.type == cubeType);
            Vector3 cubePosition = Vector3.zero;
            if (capturedCube != null && grid != null)
            {
                cubePosition = grid.GridToWorldPosition(capturedCube.position.x, capturedCube.position.y, 2f);
            }
            audioManager.TriggerCubeAudioEvent(GameAudioEvent.CubeCaptured, cubeType, cubePosition);
            DebugLog($"🔊 Audio: Cube captured event triggered for {cubeType}");
        }

        NotifyStageManager(sm => sm.OnCubeCaptured(cubeType));
    }

    /// <summary>
    /// CUBE ESCAPE HANDLER: Called when a cube escapes the play area.
    /// This is where the escape mechanic is processed at the Wave level.
    /// Wave determines if escape threshold is exceeded and triggers wave failure if needed.
    /// </summary>
    /// <param name="cubeType">Type of cube that escaped</param>
    public void OnCubeEscaped(CubeType cubeType)
    {
        // Find the cube that's escaping to get its position
        var escapingCube = activeCubes.FirstOrDefault(c => c != null && c.type == cubeType && c.position.y <= 0);
        Vector2Int escapePosition = escapingCube != null ? escapingCube.position : Vector2Int.zero;
        
        // INCREMENT WAVE ESCAPE COUNTER
        cubesEscaped++;
        DebugLog($"🚨 CUBE ESCAPE: {cubeType} escaped from wave {currentWaveIndex}. Total escapes: {cubesEscaped}");
        
        // Notify statistics manager about cube escape
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnCubeEscaped(escapePosition, cubeType.ToString());
        }
        
        // Trigger cube escaped audio event
        if (audioManager != null && grid != null)
        {
            Vector3 escapeWorldPosition = grid.GridToWorldPosition(escapePosition.x, escapePosition.y, 2f);
            audioManager.TriggerCubeAudioEvent(GameAudioEvent.CubeEscaped, cubeType, escapeWorldPosition);
            DebugLog($"🔊 Audio: Cube escaped event triggered for {cubeType} at position {escapePosition}");
        }
        
        // CHECK WAVE FAILURE CONDITION: Does this wave have escape limits?
        if (CurrentWave != null && CurrentWave.hasOwnSuccessCriteria && CurrentWave.maxAllowedEscapes >= 0)
        {
            if (cubesEscaped > CurrentWave.maxAllowedEscapes)
            {
                DebugLog($"❌ WAVE FAILED: Too many escapes! ({cubesEscaped} > {CurrentWave.maxAllowedEscapes})");
                TriggerWaveFailure("Too many cube escapes");
                return;
            }
        }
        
        // Line divider system paused - unneeded complexity for now
        // TODO: Re-enable if needed after testing row penalty system
        // int penaltyRows = GetPenaltyRowsForCubeType(cubeType);
        // if (penaltyRows > 0 && grid != null)
        // {
        //     grid.MoveLineDivider(-penaltyRows, false);
        //     DebugLog($"[Task 6] Applied {penaltyRows} row penalty for {cubeType} escape");
        // }
        
        // Process as normal cube behavior for wave completion tracking
        if (cubeType == CubeType.Unit)
        {
            unitCubesEscaped++;
            DebugLog($"Unit cube escaped. Total Unit escapes: {unitCubesEscaped}/{grid?.Width ?? 0} (threshold: {grid?.Width ?? 0} for row penalty)");
            
            // Row Penalty: When escaped Unit cubes equals number of columns, remove bottom row
            if (grid != null && unitCubesEscaped >= grid.Width)
            {
                DebugLog($"⚠️ ROW PENALTY TRIGGERED: {unitCubesEscaped} Unit cubes escaped (equals grid width {grid.Width}). Removing bottom row!");
                grid.RemoveBottomRow();
                unitCubesEscaped = 0; // Reset counter after penalty
            }
            
            OnNonBlackCubeProcessed(cubeType, false); // false = not captured
            this.Log($"Normal cube escaped - wave completion check triggered", showDebugInfo);
        }
    }
    
    /// <summary>
    /// Task 6: Gets penalty rows for cube type based on design doc
    /// Unit: 1 row, Matrix: 2 rows, Recursion: 2 rows, Infinity: 0 rows
    /// </summary>
    private int GetPenaltyRowsForCubeType(CubeType cubeType)
    {
        switch (cubeType)
        {
            case CubeType.Unit:
                return 1;
            case CubeType.Matrix:
            case CubeType.Recursion:
                return 2;
            case CubeType.Infinity:
                return 0; // No penalty for Infinity (intended behavior)
            default:
                return 1;
        }
    }

    /// <summary>
    /// Called when player dies. Tracks deaths and applies row penalty at 2 deaths.
    /// </summary>
    public void OnPlayerDeath()
    {
        playerDeaths++;
        DebugLog($"💀 Player death recorded. Total deaths this wave: {playerDeaths}/2 (threshold: 2 for row penalty)");
        
        // Death Penalty: When player dies 2 times, remove bottom row
        if (grid != null && playerDeaths >= 2)
        {
            DebugLog($"⚠️ DEATH PENALTY TRIGGERED: {playerDeaths} player deaths (threshold: 2). Removing bottom row!");
            grid.RemoveBottomRow();
            playerDeaths = 0; // Reset counter after penalty
        }
    }

    /// <summary>
    /// WAVE FAILURE TRIGGER: Called when wave fails due to escape limit or other criteria.
    /// Notifies Stage via event system that this wave has failed.
    /// </summary>
    /// <param name="reason">Reason for wave failure (for debugging/feedback)</param>
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

    public void OnMarkerPlaced() => markersPlaced++;

    public void OnDetonationUsed() => detonationsUsed++;

    public void OnNonBlackCubeProcessed(CubeType cubeType, bool wasCaptured)
    {
        if (cubeType == CubeType.Infinity) return;

        processedNonBlackCubes++;
        DebugLog($"📊 Non-black cube processed: {processedNonBlackCubes}/{totalNonBlackCubes}");

        if (processedNonBlackCubes >= totalNonBlackCubes)
        {
            string reason = wasCaptured ? "All cubes captured!" : "All cubes processed!";
            
            // Task 6: Apply line divider reward for perfect wave clear
            if (wasCaptured && grid != null)
            {
                grid.MoveLineDivider(1, true);
                DebugLog($"[Task 6] Applied 1 row reward for perfect wave clear");
            }
            
            StartCoroutine(ShowCompletionMessage(reason));
        }
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
    {
        totalNonBlackCubes = activeCubes.Count(c => c != null && c.type != CubeType.Infinity);
        processedNonBlackCubes = 0;
    }

    private void ResetWaveStatistics()
    {
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        reinforcedCubesCaptured = 0;
        cubesEscaped = 0;
        unitCubesEscaped = 0;
        playerDeaths = 0; // Reset death counter at wave start
        markersPlaced = 0;
        detonationsUsed = 0;
        totalNonBlackCubes = 0;
        processedNonBlackCubes = 0;
        
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
            ["Pending Messages"] = pendingMessages.Count
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
        isPaused = false;
        
        // Reset statistics
        ResetWaveStatistics();
        
        // Clear message queue
        pendingMessages.Clear();
        isProcessingMessageQueue = false;
        
        // Hide any active messages
        if (messagePanel != null)
            messagePanel.SetActive(false);
        
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
