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
        CleanupWave();
        DebugLog("⏹️ Wave Stopped");
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
    private IEnumerator RunWaveCoroutine(bool resume = false)
    {
        waveActive = true;
        if (!resume) MoveStep = 0;

        SetupWave(resume);

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

    private void SetupWave(bool resume)
    {
        if (!resume)
        {
            SpawnWaveCubes();
            ShowInitialMessages();
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

        // Handle HasBeenMirrored logic: If wave hasn't been mirrored, flip flag and spawn mirrored version
        if (CurrentWave != null)
        {
            bool hasBeenMirrored = GetHasBeenMirrored(CurrentWave);
            
            if (!hasBeenMirrored)
            {
                // Wave completed for first time - flip flag and spawn mirrored version
                // Note: Don't clear markers yet - they're needed for SpawnMirroredWave()
                SetHasBeenMirrored(CurrentWave, true);
                DebugLog($"[PairedWave] Wave completed, HasBeenMirrored flipped to true. Spawning mirrored version...");
                
                // Spawn the mirrored version of this wave (markers will be cleared after use)
                StartCoroutine(SpawnMirroredWave());
                return; // Don't advance to next wave yet - we're spawning the mirrored version
            }
            else
            {
                // Wave has been mirrored already - clear markers and advance to next wave
                ClearPreviousWaveMarkers();
                DebugLog("[PairedWave] Mirrored wave completed, advancing to next wave");
            }
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

        if (useWaveConfiguration && CurrentWave != null)
        {
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
        if (rules.lightSpawnsUnit) totalMarkersToSpawn += recordedPositions.lightMarkerPositions.Count;
        if (rules.heavySpawnsRecursion) totalMarkersToSpawn += recordedPositions.heavyMarkerPositions.Count;
        if (rules.primeSpawnsPrime) totalMarkersToSpawn += recordedPositions.primeMarkerPositions.Count;
        if (rules.infinitySpawnsInfinity) totalMarkersToSpawn += recordedPositions.infinityMarkerPositions.Count;
        
        DebugLog($"[PairedWave] Starting spawn: {totalMarkersToSpawn} markers should spawn cubes");

        // Collect all marker positions and normalize them together
        // This ensures markers are mapped to wave rows based on their relative Y positions
        List<Vector2Int> allMarkerPositions = new List<Vector2Int>();
        Dictionary<Vector2Int, CubeType> markerToCubeType = new Dictionary<Vector2Int, CubeType>();
        
        if (rules.lightSpawnsUnit)
        {
            foreach (var pos in recordedPositions.lightMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Unit;
            }
        }
        if (rules.heavySpawnsRecursion)
        {
            foreach (var pos in recordedPositions.heavyMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Recursion;
            }
        }
        if (rules.primeSpawnsPrime)
        {
            foreach (var pos in recordedPositions.primeMarkerPositions)
            {
                allMarkerPositions.Add(new Vector2Int(pos.x, pos.y));
                markerToCubeType[pos] = CubeType.Prime;
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

            // Check if this normalized position was already used (only if allowOverlap is false)
            if (!rules.allowOverlap && spawnedPositions.Contains(normalizedPos))
            {
                // Position collision - but we still need to spawn this cube
                // Allow overlap to preserve marker count
                DebugLog($"[PairedWave] Position collision at wave ({normalizedPos.x}, {normalizedPos.y}), but allowing overlap to preserve marker count");
            }

CubeType cubeType = markerToCubeType[originalPos];
            if (SpawnInheritedCubeAtNormalizedPosition(normalizedPos, cubeType, true)) // Always allow overlap to preserve count
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
    /// </summary>
    private bool SpawnInheritedCubeAtNormalizedPosition(Vector2Int normalizedPosition, CubeType cubeType, bool allowOverlap)
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

        // Check for overlap with existing cubes (check at the grid position where it will spawn)
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
        
        bool hasOverlap = HasCubeAtPosition(finalGridPosition);
        
        if (hasOverlap && !allowOverlap)
        {
            // Overlap not allowed - skip this spawn
            DebugLog($"[PairedWave] Skipping inherited cube at wave ({normalizedPosition.x}, {normalizedPosition.y}) -> final grid ({finalGridPosition.x}, {finalGridPosition.y}) due to overlap");
            return false;
        }

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

    /// <summary>
    /// Checks if there's already a cube at the specified position.
    /// </summary>
    private bool HasCubeAtPosition(Vector2Int position)
    {
        foreach (var cube in activeCubes)
        {
            if (cube != null && cube.position == position)
            {
                return true;
            }
        }
        return false;
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
        // Spawn player cubes from light markers before moving wave cubes
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
        if (!debugMode) return;

        MoveCubesForward();
        MoveStep++;
        ProcessStepMessages();
        NotifyStepComplete();

        DebugLog($"🔧 Manual Step: {MoveStep}");
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
            case CubeType.Prime: blueCubesCaptured++; break;
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
        
        // Process as normal cube behavior for wave completion tracking
        if (cubeType == CubeType.Unit)
        {
            OnNonBlackCubeProcessed(cubeType, false); // false = not captured
            this.Log($"Normal cube escaped - wave completion check triggered", showDebugInfo);
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
        if (random < normalCubeChance + blueCubeChance) return CubeType.Prime;
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
            playerActionManager.maxLightMarkers = waveConfiguration[currentWaveIndex].maxLightMarkerCount;
            playerActionManager.maxLightMarkerCharges = waveConfiguration[currentWaveIndex].maxLightMarkerCharge;

            playerActionManager.maxPrimeMarkers = waveConfiguration[currentWaveIndex].maxPrimeMarkerCount;
            playerActionManager.maxPrimeMarkerCharges = waveConfiguration[currentWaveIndex].maxPrimeMarkerCharge;
            
            // Validate and adjust current mode based on available marker types
            playerActionManager.ValidateCurrentMode();
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
    /// Spawns the mirrored version of the current wave using marker positions from the previous wave.
    /// Called automatically when a wave completes for the first time (HasBeenMirrored = false).
    /// Can also be called manually from debug panels.
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
        
        // Spawn base cubes from wave configuration
        SpawnConfigurationCubes();
        
        // Spawn inherited cubes from previous wave markers
        SpawnInheritedCubes();
        
        // Clear markers after they've been used for spawning
        ClearPreviousWaveMarkers();
        
        CountNonBlackCubes();
        DebugLog($"[PairedWave] Mirrored wave spawned: {activeCubes.Count} cubes ({totalNonBlackCubes} non-black)");
        
        // Start the mirrored wave
        StartWave();
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

    #region Paired Wave System - Ghost Preview

    /// <summary>
    /// Gets preview positions for inherited cubes that will spawn in the mirrored version of this wave.
    /// Returns list of normalized positions (in wave coordinates) and cube types for visualization.
    /// Positions are normalized to wave's GridHeight constraints, matching actual spawn behavior.
    /// </summary>
    public List<GhostPreviewData> GetGhostPreviewPositions()
    {
        var previews = new List<GhostPreviewData>();
        
        if (CurrentWave == null) return previews;
        
        // Only show previews if this wave hasn't been mirrored yet and has markers recorded
        bool hasBeenMirrored = GetHasBeenMirrored(CurrentWave);
        if (hasBeenMirrored) return previews;

        // Get recorded marker positions from current wave
        var recordedPositions = GetPreviousWaveMarkers();
        if (recordedPositions == null || recordedPositions.GetTotalMarkerCount() == 0)
        {
            return previews;
        }

        // Use current wave's spawn rules for preview
        var rules = CurrentWave.markerSpawnRules;

        // Collect all marker positions that will spawn cubes
        List<Vector2Int> allMarkerPositions = new List<Vector2Int>();
        Dictionary<Vector2Int, CubeType> markerToCubeType = new Dictionary<Vector2Int, CubeType>();
        
        if (rules.lightSpawnsUnit)
        {
            foreach (var pos in recordedPositions.lightMarkerPositions)
            {
                allMarkerPositions.Add(pos);
                markerToCubeType[pos] = CubeType.Unit;
            }
        }
        if (rules.heavySpawnsRecursion)
        {
            foreach (var pos in recordedPositions.heavyMarkerPositions)
            {
                allMarkerPositions.Add(pos);
                markerToCubeType[pos] = CubeType.Recursion;
            }
        }
        if (rules.primeSpawnsPrime)
        {
            foreach (var pos in recordedPositions.primeMarkerPositions)
            {
                allMarkerPositions.Add(pos);
                markerToCubeType[pos] = CubeType.Prime;
            }
        }
        if (rules.infinitySpawnsInfinity)
        {
            foreach (var pos in recordedPositions.infinityMarkerPositions)
            {
                allMarkerPositions.Add(pos);
                markerToCubeType[pos] = CubeType.Infinity;
            }
        }

        // Normalize all positions to wave constraints (same logic as SpawnInheritedCubes)
        Dictionary<Vector2Int, Vector2Int> normalizedPositions = NormalizeMarkerPositionsToWaveConstraints(allMarkerPositions, CurrentWave);

        // Generate previews with normalized positions
        foreach (var originalPos in allMarkerPositions)
        {
            if (normalizedPositions.TryGetValue(originalPos, out Vector2Int normalizedPos))
            {
                // Convert normalized wave position to final grid position for preview
                // Use same logic as SpawnInheritedCubeAtNormalizedPosition
                var waveHeight = CurrentWave.GridHeight;
                int finalGridY;
                
                if (normalizedPos.y < waveHeight)
                {
                    // Normal case: within wave constraints
                    finalGridY = grid != null ? grid.Height - (waveHeight - normalizedPos.y) : normalizedPos.y;
                }
                else
                {
                    // Extended case: beyond wave GridHeight
                    int offsetFromTop = normalizedPos.y - waveHeight;
                    finalGridY = grid != null ? grid.Height - 1 - offsetFromTop : normalizedPos.y;
                    finalGridY = Mathf.Max(0, finalGridY);
                }
                
                Vector2Int previewGridPos = new Vector2Int(normalizedPos.x, finalGridY);
                
                previews.Add(new GhostPreviewData 
                { 
                    position = previewGridPos, // Grid position for visualization
                    cubeType = markerToCubeType[originalPos]
                });
            }
        }

        return previews;
    }

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
        return $"Wave {currentWaveIndex}: {status} ({speedState}) Step:{MoveStep} Cubes:{activeCubes.Count} Mode:{(debugMode ? "DEBUG" : "NORMAL")}";
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
        if (CurrentWave != null)
        {
            bool hasBeenMirrored = GetHasBeenMirrored(CurrentWave);
            debugData["Has Been Mirrored"] = hasBeenMirrored;
            
            var recordedPositions = GetPreviousWaveMarkers();
            if (recordedPositions != null)
            {
                debugData["Recorded Light Markers"] = recordedPositions.lightMarkerPositions.Count;
                debugData["Recorded Heavy Markers"] = recordedPositions.heavyMarkerPositions.Count;
                debugData["Recorded Prime Markers"] = recordedPositions.primeMarkerPositions.Count;
                debugData["Total Recorded Markers"] = recordedPositions.GetTotalMarkerCount();
            }
            
            var ghostPreviews = GetGhostPreviewPositions();
            debugData["Ghost Preview Count"] = ghostPreviews.Count;
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
/// </summary>
[System.Serializable]
public class RecordedMarkerPositions
{
    public List<Vector2Int> lightMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> heavyMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> primeMarkerPositions = new List<Vector2Int>();
    public List<Vector2Int> infinityMarkerPositions = new List<Vector2Int>();

    public RecordedMarkerPositions()
    {
        // No initialization needed
    }

    public void RecordMarker(Vector2Int position, MarkerMode markerType)
    {
        switch (markerType)
        {
            case MarkerMode.Light:
                if (!lightMarkerPositions.Contains(position))
                    lightMarkerPositions.Add(position);
                break;
            case MarkerMode.Heavy:
                if (!heavyMarkerPositions.Contains(position))
                    heavyMarkerPositions.Add(position);
                break;
            case MarkerMode.Prime:
                if (!primeMarkerPositions.Contains(position))
                    primeMarkerPositions.Add(position);
                break;
            // Infinity markers not yet implemented, but structure is ready
        }
    }

    public int GetTotalMarkerCount()
    {
        return lightMarkerPositions.Count + heavyMarkerPositions.Count + 
               primeMarkerPositions.Count + infinityMarkerPositions.Count;
    }
}

/// <summary>
/// Data structure for ghost preview visualization of future cube spawns.
/// </summary>
[System.Serializable]
public class GhostPreviewData
{
    public Vector2Int position;
    public CubeType cubeType;
}
