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
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
        InitializeState();
        EnableDebugLogs = true;
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

        // Main wave loop
        while (HasActiveCubes())
        {
            yield return ProcessWaveStep();
            yield return new WaitForSeconds(GetCurrentMoveInterval());
        }

        CompleteWave();
    }

    private void SetupWave(bool resume, bool skipSpawn = false)
    {
        if (!resume)
        {
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

    private IEnumerator ProcessWaveStep()
    {
        MoveCubesForward(); // This now includes player cube spawning
        
        // Move player cubes backward after wave cubes have moved
        if (playerActionManager != null && playerActionManager.MarkerSystem != null)
        {
            playerActionManager.MarkerSystem.MovePlayerCubesBackward();
            playerActionManager.MarkerSystem.CheckPlayerCubeCollisions();
        }
        
        ProcessStepMessages();
        ProcessStepSequences();
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

        ProcessEndMessages();

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

    #region Cube Management
    private void SpawnWaveCubes()
    {
        ClearAllCubes();
        ResetPlayer();

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
        
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (i >= activeCubes.Count) continue;
            
            var cube = activeCubes[i];
            if (cube == null)
            {
                activeCubes.RemoveAt(i);
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

                if (!stillAlive)
                {
                    activeCubes.RemoveAt(i);
                }
            }
        }
        MoveStep++;
        
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

        MoveCubesForward();
        MoveStep++;
        ProcessStepMessages();
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
        ProcessStepMessages();
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
        if (CurrentWave?.messages == null || !showMessages) return;

        // Start coroutine to delay messages until camera has panned to position
        StartCoroutine(ShowInitialMessagesDelayed());
    }
    
    private IEnumerator ShowInitialMessagesDelayed()
    {
        // Wait for camera to pan to default position (CameraFollow uses 0.25s smooth time)
        // Add extra buffer to ensure camera is fully positioned before showing messages
        yield return new WaitForSeconds(0.6f);

        // Process initial sequences first (if any)
        ProcessInitialSequences();
        
        // Then show initial messages (using MessageHighlightManager if available, otherwise fallback)
        var initialMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == 0);
        foreach (var message in initialMessages)
        {
            if (messageHighlightManager != null)
            {
                // Use MessageHighlightManager for unified message handling
                messageHighlightManager.ShowMessage(message.Message, message.RequirePause, message.AutoHideDelay, MoveStep);
            }
            else
            {
                // Fallback to old system
                ShowMessage(message);
            }
        }
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

    private void ProcessStepMessages()
    {
        if (CurrentWave?.messages == null || !showMessages) return;

        var stepMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == MoveStep);
        foreach (var message in stepMessages)
        {
            if (messageHighlightManager != null)
            {
                // Use MessageHighlightManager for unified message handling
                messageHighlightManager.ShowMessage(message.Message, message.RequirePause, message.AutoHideDelay, MoveStep);
            }
            else
            {
                // Fallback to old system
                ShowMessage(message);
            }
        }
    }
    
    /// <summary>
    /// Processes highlight sequences at the current move step
    /// </summary>
    private void ProcessStepSequences()
    {
        if (CurrentWave?.highlightSequences == null || messageHighlightManager == null) return;
        
        // Get sequences for current move step that aren't event-triggered
        var stepSequences = CurrentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == MoveStep &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);
        
        foreach (var sequence in stepSequences)
        {
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }

    private void ProcessEndMessages()
    {
        if (CurrentWave?.messages == null || !showMessages) return;

        var endMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == -1);
        foreach (var message in endMessages)
        {
            if (messageHighlightManager != null)
            {
                // Use MessageHighlightManager for unified message handling
                messageHighlightManager.ShowMessage(message.Message, message.RequirePause, message.AutoHideDelay, MoveStep);
            }
            else
            {
                // Fallback to old system
                ShowMessage(message);
            }
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
        
        // Task 6: Apply line divider penalty for cube escape
        int penaltyRows = GetPenaltyRowsForCubeType(cubeType);
        if (penaltyRows > 0 && grid != null)
        {
            grid.MoveLineDivider(-penaltyRows, false);
            DebugLog($"[Task 6] Applied {penaltyRows} row penalty for {cubeType} escape");
        }
        
        // Process as normal cube behavior for wave completion tracking
        if (cubeType == CubeType.Unit)
        {
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
        markersPlaced = 0;
        detonationsUsed = 0;
        totalNonBlackCubes = 0;
        processedNonBlackCubes = 0;
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

    public bool EnableDebugLogs { get; set; } = true;

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
