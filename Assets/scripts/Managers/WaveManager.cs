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
    
    // Mirrored wave flag - when true, cubes are already spawned from markers
    private bool isMirroredWaveActive = false;

    // Paired Wave System - Marker Position Recording
    // Stores marker positions from the previous wave for inheritance by the mirrored version
    private RecordedMarkerPositions previousWaveMarkers = null;
    
    // Tracks HasBeenMirrored state per wave instance
    private Dictionary<WaveData, bool> waveMirrorState = new Dictionary<WaveData, bool>();

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
        if (grid == null) grid = FindAnyObjectByType<GridManager>();
        if (player == null) player = FindAnyObjectByType<PlayerManager>();
        if (playerActionManager == null) playerActionManager = FindAnyObjectByType<PlayerActionManager>();
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
            // or if mirrored wave (cubes already spawned from markers)
            if (skipSpawn)
            {
                DebugLog("SetupWave: Skipping spawn - using existing cubes");
                CountNonBlackCubes(); // Count existing cubes for completion tracking
                // Don't show messages for custom waves (no wave configuration)
            }
            else if (!isMirroredWaveActive)
            {
                SpawnWaveCubes();
                ShowInitialMessages(); // Only show messages for configured waves
            }
            else
            {
                DebugLog("[PairedWave] Mirrored wave - cubes already spawned from markers");
                ShowInitialMessages(); // Show messages for mirrored waves if configured
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

        // Handle paired wave logic
        if (isMirroredWaveActive)
        {
            // Mirrored wave just completed - reset flag and advance to next config wave
            isMirroredWaveActive = false;
            DebugLog("[PairedWave] Mirrored wave completed, advancing to next config wave");
        }
        else if (CurrentWave != null)
        {
            // Config wave completed - spawn mirrored wave from recorded markers
            DebugLog("[PairedWave] Config wave completed, spawning mirrored wave from markers...");
            
            // Spawn the mirrored wave (markers will be cleared after use)
            StartCoroutine(SpawnMirroredWave());
            return; // Don't advance to next wave yet - we're spawning the mirrored version
        }

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
            
            // Spawn inherited cubes from previous wave markers (if this wave has been mirrored)
            SpawnInheritedCubes();
        }
        else
        {
            SpawnRandomCubes();
        }

        CountNonBlackCubes();
        DebugLog($"📦 Spawned {activeCubes.Count} cubes ({totalNonBlackCubes} non-black)");
    }

    /// <summary>
    /// Validates that wave size matches grid size, and resizes grid if needed.
    /// Ensures waves can only spawn within valid grid bounds.
    /// </summary>
    private void ValidateAndResizeGridForWave(WaveData wave)
    {
        if (wave == null || grid == null) return;

        // Check if wave size matches grid size
        bool needsResize = (wave.GridWidth != grid.Width || wave.GridHeight != grid.Height);
        
        if (needsResize)
        {
            DebugLog($"⚠️ Wave size ({wave.GridWidth}x{wave.GridHeight}) doesn't match grid size ({grid.Width}x{grid.Height}). Resizing grid to match wave.");
            
            // Validate wave dimensions are reasonable
            int newWidth = Mathf.Clamp(wave.GridWidth, 3, 20);
            int newHeight = Mathf.Clamp(wave.GridHeight, 9, 50);
            
            if (newWidth != wave.GridWidth || newHeight != wave.GridHeight)
            {
                DebugLog($"⚠️ Wave dimensions clamped from {wave.GridWidth}x{wave.GridHeight} to {newWidth}x{newHeight}");
            }
            
            // Resize grid to match wave
            grid.ResizeGrid(newWidth, newHeight);
            
            // Wait for grid to be ready (if in coroutine context, this will yield)
            // Note: This is called from SpawnWaveCubes which is called from SetupWave
            // which is called from RunWaveCoroutine, so we can't yield here.
            // Grid resize should be fast enough, but we log if it's not ready
            if (!grid.IsGridReady)
            {
                DebugLog("⚠️ Grid resize not complete, but continuing with wave spawn. Grid may not be fully ready.");
            }
        }
    }

    private void SpawnConfigurationCubes()
    {
        var wave = CurrentWave;
        foreach (var cubeData in wave.CubesData)
        {
            SpawnCube(cubeData);
        }
    }

    /// <summary>
    /// Spawns cubes at positions recorded from the previous wave.
    /// Only executes if this wave has HasBeenMirrored = true (mirrored version).
    /// Normalizes marker positions to wave's GridHeight constraints before spawning.
    /// </summary>
    private void SpawnInheritedCubes()
    {
        var wave = CurrentWave;
        if (wave == null) return;

        // Only spawn inherited cubes if this wave has been mirrored
        bool hasBeenMirrored = GetHasBeenMirrored(wave);
        if (!hasBeenMirrored) return;

        // Get recorded marker positions from the previous wave
        var recordedPositions = GetPreviousWaveMarkers();
        if (recordedPositions == null || recordedPositions.GetTotalMarkerCount() == 0)
        {
            DebugLog("[PairedWave] No recorded markers found from previous wave");
            return;
        }

        var rules = wave.markerSpawnRules;
        
        // Count total markers that should spawn cubes
        int totalMarkersToSpawn = 0;
        if (rules.unitSpawnsUnit) totalMarkersToSpawn += recordedPositions.unitMarkerPositions.Count;
        if (rules.recursionSpawnsRecursion) totalMarkersToSpawn += recordedPositions.recursionMarkerPositions.Count;
        if (rules.matrixSpawnsMatrix) totalMarkersToSpawn += recordedPositions.matrixMarkerPositions.Count;
        if (rules.infinitySpawnsInfinity) totalMarkersToSpawn += recordedPositions.infinityMarkerPositions.Count;
        
        DebugLog($"[PairedWave] Starting spawn: {totalMarkersToSpawn} markers should spawn cubes");

        // Collect all marker positions and normalize them together
        // This ensures markers are mapped to wave rows based on their relative Y positions
        List<Vector2Int> allMarkerPositions = new List<Vector2Int>();
        Dictionary<Vector2Int, CubeType> markerToCubeType = new Dictionary<Vector2Int, CubeType>();
        
        if (rules.unitSpawnsUnit)
        {
            foreach (var pos in recordedPositions.unitMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Unit;
            }
        }
        if (rules.recursionSpawnsRecursion)
        {
            foreach (var pos in recordedPositions.recursionMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Recursion;
            }
        }
        if (rules.matrixSpawnsMatrix)
        {
            foreach (var pos in recordedPositions.matrixMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Matrix;
            }
        }
        if (rules.infinitySpawnsInfinity)
        {
            foreach (var pos in recordedPositions.infinityMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Infinity;
            }
        }

        // Normalize all positions to wave constraints (ensures 1:1 mapping)
        Dictionary<Vector2Int, Vector2Int> normalizedPositions = NormalizeMarkerPositionsToWaveConstraints(allMarkerPositions, wave);

        // Track spawned cubes to ensure 1:1 mapping
        int inheritedCount = 0;
        int failedSpawns = 0;
        HashSet<Vector2Int> spawnedPositions = new HashSet<Vector2Int>();

        // Spawn cubes from all markers, ensuring each marker spawns exactly one cube
        foreach (var originalPos in allMarkerPositions)
        {
            if (!normalizedPositions.TryGetValue(originalPos, out Vector2Int normalizedPos))
            {
                DebugLog($"[PairedWave] ERROR: Marker at grid ({originalPos.x}, {originalPos.y}) was not normalized!");
                failedSpawns++;
                continue;
            }

            CubeType cubeType = markerToCubeType[originalPos];
            if (SpawnInheritedCubeAtNormalizedPosition(normalizedPos, cubeType))
            {
                inheritedCount++;
                spawnedPositions.Add(normalizedPos);
            }
            else
            {
                failedSpawns++;
                DebugLog($"[PairedWave] WARNING: Failed to spawn cube for marker at grid ({originalPos.x}, {originalPos.y})");
            }
        }

        // Validation: Ensure marker count matches spawned cube count
        if (inheritedCount != totalMarkersToSpawn)
        {
            DebugLog($"[PairedWave] ERROR: Marker count mismatch! Expected {totalMarkersToSpawn} cubes, spawned {inheritedCount}, failed {failedSpawns}");
        }
        else
        {
            DebugLog($"[PairedWave] Successfully spawned {inheritedCount} inherited cubes from {totalMarkersToSpawn} markers (1:1 mapping preserved)");
        }
    }

    /// <summary>
    /// Spawns a single inherited cube at the normalized wave position.
    /// Position is already normalized to wave coordinates (0 to GridHeight-1).
    /// SpawnCube will convert this to grid coordinates and spawn at top of grid.
    /// Always spawns cubes even if position overlaps with existing cubes (preserves 1:1 marker mapping).
    /// </summary>
    private bool SpawnInheritedCubeAtNormalizedPosition(Vector2Int normalizedPosition, CubeType cubeType)
    {
        var wave = CurrentWave;
        if (wave == null || grid == null)
        {
            DebugLog("[PairedWave] Cannot spawn inherited cube - missing wave or grid");
            return false;
        }

        // Validate normalized position (allow Y to exceed GridHeight to preserve marker count)
        if (normalizedPosition.x < 0 || normalizedPosition.x >= wave.GridWidth || normalizedPosition.y < 0)
        {
            DebugLog($"[PairedWave] Normalized position out of wave bounds: ({normalizedPosition.x}, {normalizedPosition.y})");
            return false;
        }

        // Calculate the final grid position where the cube will spawn
        // SpawnCube will convert wave Y to grid Y, so we need to calculate the final grid position
        // If normalizedPosition.y exceeds GridHeight, we need to calculate grid position differently
        var waveHeight = wave.GridHeight;
        int finalGridY;
        
        if (normalizedPosition.y < waveHeight)
        {
            // Normal case: within wave constraints
            finalGridY = grid.Height - (waveHeight - normalizedPosition.y);
        }
        else
        {
            // Extended case: beyond wave GridHeight, spawn at top of grid
            // Calculate offset from top: if normalizedY = GridHeight, spawn at grid.Height - 1
            // If normalizedY = GridHeight + 1, spawn at grid.Height - 2, etc.
            int offsetFromTop = normalizedPosition.y - waveHeight;
            finalGridY = grid.Height - 1 - offsetFromTop;
            
            // Clamp to valid grid bounds
            finalGridY = Mathf.Max(0, finalGridY);
        }
        
        Vector2Int finalGridPosition = new Vector2Int(normalizedPosition.x, finalGridY);
        
        // Create cube data with normalized wave coordinates
        // If position exceeds GridHeight, we need to spawn directly at calculated grid position
        // Otherwise, SpawnCube will handle the conversion
        if (normalizedPosition.y < waveHeight)
        {
            // Normal case: use SpawnCube which handles wave-to-grid conversion
            var cubeData = new CubeData
            {
                type = cubeType,
                position = normalizedPosition, // Wave coordinates (0 to GridHeight-1)
                level = 1
            };
            SpawnCube(cubeData);
        }
        else
        {
            // Extended case: spawn directly at calculated grid position
            // SpawnCube expects positions within GridHeight, so we spawn manually
            Vector3 worldPos = grid.GridToWorldPosition(finalGridPosition.x, finalGridPosition.y, 2f);
            int prefabIndex = (int)cubeType;
            if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length)
            {
                GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, Quaternion.identity);
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                // Create cube data with grid position (not wave position)
                var cubeData = new CubeData
                {
                    type = cubeType,
                    position = finalGridPosition, // Grid coordinates for extended positions
                    level = 1
                };
                cube.Init(grid, cubeData, 2f);
                activeCubes.Add(cube);
                DebugLog($"[PairedWave] Spawned extended-position cube directly at grid ({finalGridPosition.x}, {finalGridPosition.y})");
            }
        }
        
        // Visual feedback: Log inheritance spawn
        DebugLog($"[PairedWave] Spawned inherited {cubeType} cube at wave ({normalizedPosition.x}, {normalizedPosition.y}) -> final grid ({finalGridPosition.x}, {finalGridPosition.y})");
        
        return true;
    }

    /// <summary>
    /// Normalizes all marker positions to the wave's GridHeight constraints.
    /// Ensures 1:1 mapping - every marker gets a unique position, distributed across the grid.
    /// If markers exceed wave constraints, distributes them across available grid space.
    /// </summary>
    private Dictionary<Vector2Int, Vector2Int> NormalizeMarkerPositionsToWaveConstraints(List<Vector2Int> markerPositions, WaveData wave)
    {
        Dictionary<Vector2Int, Vector2Int> normalizedMap = new Dictionary<Vector2Int, Vector2Int>();
        
        if (markerPositions == null || markerPositions.Count == 0)
            return normalizedMap;

        // Group markers by column to preserve column structure
        Dictionary<int, List<Vector2Int>> markersByColumn = new Dictionary<int, List<Vector2Int>>();
        
        foreach (var pos in markerPositions)
        {
            int clampedX = Mathf.Clamp(pos.x, 0, wave.GridWidth - 1);
            if (!markersByColumn.ContainsKey(clampedX))
            {
                markersByColumn[clampedX] = new List<Vector2Int>();
            }
            markersByColumn[clampedX].Add(pos);
        }

        // For each column, sort markers by Y and normalize within that column
        // If a column has more markers than GridHeight, they'll extend beyond GridHeight
        foreach (var columnEntry in markersByColumn)
        {
            int column = columnEntry.Key;
            List<Vector2Int> columnMarkers = columnEntry.Value;
            
            // Sort by Y position (ascending - lower Y values first)
            columnMarkers.Sort((a, b) => a.y.CompareTo(b.y));
            
            // Map markers to rows within this column
            // First marker -> row 0, second -> row 1, etc.
            // If more markers than GridHeight, continue beyond GridHeight
            for (int i = 0; i < columnMarkers.Count; i++)
            {
                Vector2Int originalPos = columnMarkers[i];
                int normalizedY = i; // Sequential rows: 0, 1, 2, ...
                
                // If we exceed wave's GridHeight, we still spawn but log a warning
                if (normalizedY >= wave.GridHeight)
                {
                    DebugLog($"[PairedWave] Warning: Marker at grid ({originalPos.x}, {originalPos.y}) normalized to wave row {normalizedY} which exceeds wave GridHeight {wave.GridHeight}. Spawning anyway to preserve marker count.");
                }
                
                Vector2Int normalizedPos = new Vector2Int(column, normalizedY);
                normalizedMap[originalPos] = normalizedPos;
                
                DebugLog($"[PairedWave] Normalized marker: grid ({originalPos.x}, {originalPos.y}) -> wave ({column}, {normalizedY})");
            }
        }

        DebugLog($"[PairedWave] Normalized {markerPositions.Count} markers across {markersByColumn.Count} columns (wave GridHeight: {wave.GridHeight})");
        return normalizedMap;
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

        var initialMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == 0);
        foreach (var message in initialMessages)
        {
            ShowMessage(message);
        }
    }

    private void ProcessStepMessages()
    {
        if (CurrentWave?.messages == null || !showMessages) return;

        var stepMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == MoveStep);
        foreach (var message in stepMessages)
        {
            ShowMessage(message);
        }
    }

    private void ProcessEndMessages()
    {
        if (CurrentWave?.messages == null || !showMessages) return;

        var endMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == -1);
        foreach (var message in endMessages)
        {
            ShowMessage(message);
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
        
        var completionMsg = new WaveMessage
        {
            Message = message,
            RequirePause = true, // POC: All tutorial waves pause for feedback
            AutoHideDelay = 0f
        };

        ShowMessage(completionMsg);
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
            
            // Only configure player if we have a valid wave configuration
            if (useWaveConfiguration && waveConfiguration != null && currentWaveIndex >= 0 && currentWaveIndex < waveConfiguration.Count)
            {
                var wave = waveConfiguration[currentWaveIndex];
                playerActionManager.maxUnitMarkers = wave.maxUnitMarkerCount;
                playerActionManager.maxUnitMarkerCharges = wave.maxUnitMarkerCharge;

                playerActionManager.maxRecursionMarkers = wave.maxRecursionMarkerCount;
                playerActionManager.maxRecursionMarkerCharges = wave.maxRecursionMarkerCharge;

                playerActionManager.maxMatrixMarkers = wave.maxMatrixMarkerCount;
                playerActionManager.maxMatrixMarkerCharges = wave.maxMatrixMarkerCharge;
                
                // Validate and adjust current mode based on available marker types
                playerActionManager.ValidateCurrentMode();
            }
            else
            {
                // For custom waves without configuration, use default values or keep current settings
                DebugLog("ConfigurePlayer: No wave configuration available, using current player settings");
            }
        }

        //playerActionManager.ConfigureUI();
    }

    private void NotifyStepComplete()
    {
        
    }

    private void NotifyStageManager(System.Action<StageManager> action)
    {
        var stageManager = FindAnyObjectByType<StageManager>();
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

    /// <summary>
    /// Spawns the mirrored wave - cubes generated ONLY from previous wave's marker positions.
    /// Called automatically when a config wave completes.
    /// Mirrored waves contain only player-placed marker positions converted to cubes.
    /// </summary>
    public IEnumerator SpawnMirroredWave()
    {
        DebugLog("[PairedWave] Starting mirrored wave spawn...");
        
        // Wait for inheritance delay if configured
        if (CurrentWave != null && CurrentWave.inheritanceDelay > 0)
        {
            yield return new WaitForSeconds(CurrentWave.inheritanceDelay);
        }
        
        // Clear current cubes
        ClearAllCubes();
        ResetPlayer();
        
        // Get recorded marker positions
        var recordedPositions = GetPreviousWaveMarkers();
        if (recordedPositions == null || recordedPositions.GetTotalMarkerCount() == 0)
        {
            DebugLog("[PairedWave] No markers recorded - mirrored wave will be empty, skipping to next config wave");
            isMirroredWaveActive = false;
            ClearPreviousWaveMarkers();
            AdvanceToNextWave();
            yield break;
        }
        
        // Set mirrored wave flag BEFORE spawning (prevents SetupWave from spawning config cubes)
        isMirroredWaveActive = true;
        
        // Spawn cubes ONLY from marker positions (no base config cubes)
        SpawnCubesFromMarkers(recordedPositions);
        
        // Clear markers after they've been used for spawning
        ClearPreviousWaveMarkers();
        
        CountNonBlackCubes();
        DebugLog($"[PairedWave] Mirrored wave spawned: {activeCubes.Count} cubes from {recordedPositions.GetTotalMarkerCount()} markers");
        
        // Start the mirrored wave (SetupWave will skip spawning due to isMirroredWaveActive flag)
        StartWave();
    }
    
    /// <summary>
    /// Spawns cubes directly from recorded marker positions for MIRRORED waves.
    /// Respects MarkerSpawnRules configuration from current wave.
    /// Used for replacement mode: spawns ONLY marker-based cubes (no base config cubes).
    /// Positions are normalized to spawn at top of grid with Y-axis mirroring.
    /// </summary>
    private void SpawnCubesFromMarkers(RecordedMarkerPositions markers)
    {
        if (grid == null) return;
        
        int gridTop = grid.Height - 1;
        int spawnedCount = 0;
        
        // Get spawn rules from current wave (use defaults if no wave configured)
        var rules = CurrentWave?.markerSpawnRules ?? new MarkerSpawnRules();
        
        // Unit markers → Unit cubes (if rule enabled)
        if (rules.unitSpawnsUnit)
        {
            foreach (var pos in markers.unitMarkerPositions)
            {
                int spawnY = gridTop - NormalizeMarkerY(pos.y, markers);
                int spawnX = Mathf.Clamp(pos.x, 0, grid.Width - 1);
                SpawnCubeDirectly(spawnX, spawnY, CubeType.Unit);
                spawnedCount++;
            }
        }
        
        // Recursion markers → Recursion cubes (if rule enabled)
        if (rules.recursionSpawnsRecursion)
        {
            foreach (var pos in markers.recursionMarkerPositions)
            {
                int spawnY = gridTop - NormalizeMarkerY(pos.y, markers);
                int spawnX = Mathf.Clamp(pos.x, 0, grid.Width - 1);
                SpawnCubeDirectly(spawnX, spawnY, CubeType.Recursion);
                spawnedCount++;
            }
        }
        
        // Matrix markers → Matrix cubes (if rule enabled)
        if (rules.matrixSpawnsMatrix)
        {
            foreach (var pos in markers.matrixMarkerPositions)
            {
                int spawnY = gridTop - NormalizeMarkerY(pos.y, markers);
                int spawnX = Mathf.Clamp(pos.x, 0, grid.Width - 1);
                SpawnCubeDirectly(spawnX, spawnY, CubeType.Matrix);
                spawnedCount++;
            }
        }
        
        // Infinity markers → Infinity cubes (if rule enabled)
        if (rules.infinitySpawnsInfinity)
        {
            foreach (var pos in markers.infinityMarkerPositions)
            {
                int spawnY = gridTop - NormalizeMarkerY(pos.y, markers);
                int spawnX = Mathf.Clamp(pos.x, 0, grid.Width - 1);
                SpawnCubeDirectly(spawnX, spawnY, CubeType.Infinity);
                spawnedCount++;
            }
        }
        
        DebugLog($"[PairedWave] Spawned {spawnedCount} cubes from markers (rules applied)");
    }
    
    /// <summary>
    /// Normalizes marker Y position to a row index (0 = top row of spawn area).
    /// MIRRORS the Y axis: markers placed low (near player) spawn at top, markers placed high spawn lower.
    /// This creates the paired wave challenge - your marker placements become incoming cubes.
    /// </summary>
    private int NormalizeMarkerY(int markerY, RecordedMarkerPositions allMarkers)
    {
        // Find the range of Y positions in all markers
        int minY = int.MaxValue;
        int maxY = int.MinValue;
        
        foreach (var pos in allMarkers.unitMarkerPositions) { minY = Mathf.Min(minY, pos.y); maxY = Mathf.Max(maxY, pos.y); }
        foreach (var pos in allMarkers.recursionMarkerPositions) { minY = Mathf.Min(minY, pos.y); maxY = Mathf.Max(maxY, pos.y); }
        foreach (var pos in allMarkers.matrixMarkerPositions) { minY = Mathf.Min(minY, pos.y); maxY = Mathf.Max(maxY, pos.y); }
        foreach (var pos in allMarkers.infinityMarkerPositions) { minY = Mathf.Min(minY, pos.y); maxY = Mathf.Max(maxY, pos.y); }
        
        if (minY == int.MaxValue) return 0;
        
        // MIRRORED: Lower Y markers (near player) spawn at top (row 0)
        // Higher Y markers spawn further down
        return markerY - minY;
    }
    
    /// <summary>
    /// Spawns a single cube directly at grid position (no wave-to-grid conversion).
    /// </summary>
    private void SpawnCubeDirectly(int gridX, int gridY, CubeType type)
    {
        if (grid == null || cubePrefabs == null) return;
        
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length) return;
        
        // Clamp to grid bounds
        gridX = Mathf.Clamp(gridX, 0, grid.Width - 1);
        gridY = Mathf.Clamp(gridY, 0, grid.Height - 1);
        
        Vector2Int position = new Vector2Int(gridX, gridY);
        Vector3 worldPos = grid.GridToWorldPosition(gridX, gridY, 2f);
        
        GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
        
        var cubeData = new CubeData
        {
            type = type,
            position = position,
            level = 1
        };
        
        cube.Init(grid, cubeData, 2f);
        activeCubes.Add(cube);
        
        DebugLog($"[PairedWave] Spawned {type} at ({gridX}, {gridY})");
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
        CurrentWave.CubesData.Add(cubeData);

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
        CurrentWave.CubesData.RemoveAll(cd => cd.position == cube.position);

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
    /// Gets the display label for the current wave (e.g., "1", "1M", "2", "2M")
    /// </summary>
    public string GetWaveLabel()
    {
        int displayIndex = currentWaveIndex + 1; // 1-based for display
        return isMirroredWaveActive ? $"{displayIndex}M" : $"{displayIndex}";
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

        // Add paired wave debug information
        debugData["Is Mirrored Wave"] = isMirroredWaveActive;
        
        var recordedPositions = GetPreviousWaveMarkers();
        if (recordedPositions != null)
        {
            debugData["Recorded Unit Markers"] = recordedPositions.unitMarkerPositions.Count;
            debugData["Recorded Recursion Markers"] = recordedPositions.recursionMarkerPositions.Count;
            debugData["Recorded Matrix Markers"] = recordedPositions.matrixMarkerPositions.Count;
            debugData["Recorded Infinity Markers"] = recordedPositions.infinityMarkerPositions.Count;
            debugData["Total Recorded Markers"] = recordedPositions.GetTotalMarkerCount();
        }
        else
        {
            debugData["Recorded Markers"] = "None";
        }

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
        isMirroredWaveActive = false;
        
        // Reset statistics
        ResetWaveStatistics();
        
        // Clear message queue
        pendingMessages.Clear();
        isProcessingMessageQueue = false;
        
        // Clear previous wave markers
        ClearPreviousWaveMarkers();
        
        // Clear all mirror states
        ClearAllMirrorStates();
        
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

    #region Paired Wave System - Marker Position Recording
    
    // ============================================================================
    // PAIRED WAVE SYSTEM - Two Distinct Wave Modes
    // ============================================================================
    //
    // The paired wave system supports two distinct modes for spawning inherited cubes:
    //
    // 1. ADDITIVE MODE (SpawnInheritedCubes):
    //    - Used during standard wave setup when HasBeenMirrored = true
    //    - Spawns inherited cubes IN ADDITION TO base wave configuration cubes
    //    - Respects MarkerSpawnRules configuration from WaveData
    //    - Use case: Wave A has predefined cubes, plus inherited cubes from previous markers
    //
    // 2. REPLACEMENT MODE (SpawnMirroredWave -> SpawnCubesFromMarkers):
    //    - Used for dedicated mirrored waves
    //    - Spawns ONLY marker-inherited cubes (no base config cubes)
    //    - Respects MarkerSpawnRules configuration from WaveData
    //    - Use case: Wave B is entirely player-created from Wave A placements
    //
    // Both modes now respect the MarkerSpawnRules configuration, allowing designers
    // to control which marker types spawn which cube types per wave.
    //
    // ============================================================================

    /// <summary>
    /// Records marker positions from the current wave for inheritance by the mirrored version.
    /// Called when markers are placed during any wave.
    /// </summary>
    public void RecordMarkerPosition(Vector2Int position, MarkerMode markerType)
    {
        if (previousWaveMarkers == null)
        {
            previousWaveMarkers = new RecordedMarkerPositions();
        }

        previousWaveMarkers.RecordMarker(position, markerType);
        DebugLog($"[PairedWave] Recorded {markerType} marker at ({position.x}, {position.y}) for next mirrored wave");
    }
    
    /// <summary>
    /// Removes a recorded marker position (for undo functionality).
    /// Returns true if marker was found and removed.
    /// </summary>
    public bool UnrecordMarkerPosition(Vector2Int position, MarkerMode markerType)
    {
        if (previousWaveMarkers == null) return false;
        
        bool removed = previousWaveMarkers.RemoveMarker(position, markerType);
        if (removed)
        {
            DebugLog($"[PairedWave] Unrecorded {markerType} marker at ({position.x}, {position.y})");
        }
        return removed;
    }

    /// <summary>
    /// Gets recorded marker positions from the previous wave.
    /// Returns null if no markers were recorded.
    /// </summary>
    public RecordedMarkerPositions GetPreviousWaveMarkers()
    {
        return previousWaveMarkers;
    }

    /// <summary>
    /// Clears recorded marker positions (called when mirrored wave spawns or wave completes).
    /// </summary>
    public void ClearPreviousWaveMarkers()
    {
        previousWaveMarkers = null;
        DebugLog("[PairedWave] Cleared previous wave markers");
    }

    #endregion

    #region Paired Wave System - HasBeenMirrored State Management

    /// <summary>
    /// Gets the HasBeenMirrored state for a wave instance.
    /// </summary>
    public bool GetHasBeenMirrored(WaveData wave)
    {
        if (wave == null) return false;
        
        // Check runtime dictionary first
        if (waveMirrorState.TryGetValue(wave, out bool state))
        {
            return state;
        }
        
        // Fallback to wave's own flag (for runtime instances)
        return wave.HasBeenMirrored;
    }

    /// <summary>
    /// Sets the HasBeenMirrored state for a wave instance.
    /// </summary>
    public void SetHasBeenMirrored(WaveData wave, bool value)
    {
        if (wave == null) return;
        
        waveMirrorState[wave] = value;
        wave.HasBeenMirrored = value;
        DebugLog($"[PairedWave] Set HasBeenMirrored to {value} for wave {wave.name}");
    }

    /// <summary>
    /// Clears all HasBeenMirrored states (called when stage resets).
    /// </summary>
    public void ClearAllMirrorStates()
    {
        waveMirrorState.Clear();
        DebugLog("[PairedWave] Cleared all mirror states");
    }

    #endregion
}

/// <summary>
/// Stores marker positions recorded during a wave for inheritance by the mirrored version.
/// Marker types: Unit, Recursion, Matrix, Infinity
/// </summary>
[System.Serializable]
public class RecordedMarkerPositions
{
    public List<Vector2Int> unitMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> recursionMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> matrixMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> infinityMarkerPositions = new List<Vector2Int>();

    public RecordedMarkerPositions()
    {
        // No initialization needed
    }

    public void RecordMarker(Vector2Int position, MarkerMode markerType)
    {
        switch (markerType)
        {
            case MarkerMode.Unit:
                if (!unitMarkerPositions.Contains(position))
                    unitMarkerPositions.Add(position);
                break;
            case MarkerMode.Recursion:
                if (!recursionMarkerPositions.Contains(position))
                    recursionMarkerPositions.Add(position);
                break;
            case MarkerMode.Matrix:
                if (!matrixMarkerPositions.Contains(position))
                    matrixMarkerPositions.Add(position);
                break;
            case MarkerMode.Infinity:
                if (!infinityMarkerPositions.Contains(position))
                    infinityMarkerPositions.Add(position);
                break;
        }
    }
    
    public bool RemoveMarker(Vector2Int position, MarkerMode markerType)
    {
        switch (markerType)
        {
            case MarkerMode.Unit:
                return unitMarkerPositions.Remove(position);
            case MarkerMode.Recursion:
                return recursionMarkerPositions.Remove(position);
            case MarkerMode.Matrix:
                return matrixMarkerPositions.Remove(position);
            case MarkerMode.Infinity:
                return infinityMarkerPositions.Remove(position);
            default:
                return false;
        }
    }

    public int GetTotalMarkerCount()
    {
        return unitMarkerPositions.Count + recursionMarkerPositions.Count + 
               matrixMarkerPositions.Count + infinityMarkerPositions.Count;
    }
}

