using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveDebugWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private WaveDebugGridConfigurator gridConfig;
    [SerializeField] private WaveDebugDataCollector dataCollector;

    [Header("Save/Load Settings")]
    [SerializeField] private string saveLocation = "Assets/data/waves/";
    [SerializeField] private WaveData currentWaveData;
    [SerializeField] private string lastLoadedWaveName = "";

    [Header("Wave State")]
    [SerializeField] private bool isTrackingActiveWave = false;
    [SerializeField] private List<CubeBehavior> trackedCubes = new List<CubeBehavior>();
    [SerializeField] private int waveOffsetY = 0;

    // Wave configuration tracking
    private Dictionary<Vector2Int, CubeType> waveState = new Dictionary<Vector2Int, CubeType>();
    private bool isDirty = false;
    private WaveData lastTrackedWave = null;
    private int lastWaveIndex = -1;

    private void Awake()
    {
        InitializeReferences();
    }

    private void Update()
    {
        CheckForWaveManagerUpdates();

        if (isTrackingActiveWave)
        {
            UpdateActiveWaveTracking();
        }
    }

    private void InitializeReferences()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (gridConfig == null) gridConfig = GetComponent<WaveDebugGridConfigurator>();
        if (dataCollector == null) dataCollector = GetComponent<WaveDebugDataCollector>();

        if (gridConfig != null)
        {
            // Initialize wave state when grid config changes
            InitializeWaveState();
        }
    }

    /// <summary>Force sync when debugger is opened - call this from UI</summary>
    public void OnDebuggerOpened()
    {
        SyncWithWaveManager();
    }

    #region Wave State Management

    /// <summary>Initialize the internal wave state based on grid configurator</summary>
    public void InitializeWaveState()
    {
        waveState.Clear();

        // Check if we're currently tracking an active wave
        if (isTrackingActiveWave && trackedCubes.Count > 0)
        {
            // Load from active cubes
            SyncWaveStateWithActiveCubes();
        }
        else
        {
            // Load from grid configurator state
            for (int x = 0; x < gridConfig.WaveWidth; x++)
            {
                for (int y = 0; y < gridConfig.WaveHeight; y++)
                {
                    int gridValue = gridConfig.gridState[x, y];
                    if (gridValue > 0)
                    {
                        CubeType cubeType = (CubeType)(gridValue - 1);
                        waveState[new Vector2Int(x, y)] = cubeType;
                    }
                }
            }
        }

        isDirty = false;
        Debug.Log($"Wave state initialized with {waveState.Count} cubes");
    }

    /// <summary>Toggle a cube type at the specified grid position</summary>
    public void ToggleCubeAtPosition(int x, int y)
    {
        if (x < 0 || x >= gridConfig.WaveWidth || y < 0 || y >= gridConfig.WaveHeight)
        {
            Debug.LogWarning($"Position ({x}, {y}) is out of bounds");
            return;
        }

        Vector2Int pos = new Vector2Int(x, y);

        // Get current state
        bool hasCube = waveState.ContainsKey(pos);
        CubeType currentType = hasCube ? waveState[pos] : CubeType.Normal;

        // Cycle through: Empty -> Normal -> Blue -> Black -> Empty
        if (!hasCube)
        {
            // Empty -> Normal
            waveState[pos] = CubeType.Normal;
            Debug.Log($"Added Normal cube at ({x}, {y})");
        }
        else
        {
            switch (currentType)
            {
                case CubeType.Normal:
                    waveState[pos] = CubeType.Blue;
                    Debug.Log($"Changed to Blue cube at ({x}, {y})");
                    break;
                case CubeType.Blue:
                    waveState[pos] = CubeType.Black;
                    Debug.Log($"Changed to Black cube at ({x}, {y})");
                    break;
                case CubeType.Black:
                    waveState.Remove(pos);
                    Debug.Log($"Removed cube at ({x}, {y})");
                    break;
            }
        }

        // Update grid configurator to stay in sync
        SyncGridConfigWithWaveState();

        // If we're tracking an active wave, update the actual cubes
        if (isTrackingActiveWave)
        {
            UpdateActiveCubeAtPosition(x, y);
        }

        isDirty = true;
    }

    /// <summary>Set a specific cube type at the specified position</summary>
    public void SetCubeAtPosition(int x, int y, CubeType cubeType)
    {
        if (x < 0 || x >= gridConfig.WaveWidth || y < 0 || y >= gridConfig.WaveHeight)
        {
            Debug.LogWarning($"Position ({x}, {y}) is out of bounds");
            return;
        }

        Vector2Int pos = new Vector2Int(x, y);

        if (cubeType == CubeType.Normal && !waveState.ContainsKey(pos))
        {
            // Don't add Normal cubes to empty spaces by default
            return;
        }

        waveState[pos] = cubeType;
        SyncGridConfigWithWaveState();

        if (isTrackingActiveWave)
        {
            UpdateActiveCubeAtPosition(x, y);
        }

        isDirty = true;
    }

    /// <summary>Clear a cube at the specified position</summary>
    public void ClearCubeAtPosition(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        waveState.Remove(pos);
        SyncGridConfigWithWaveState();

        if (isTrackingActiveWave)
        {
            RemoveActiveCubeAtPosition(x, y);
        }

        isDirty = true;
    }

    private CubeType GetNextCubeType(CubeType current)
    {
        switch (current)
        {
            case CubeType.Normal:
                return CubeType.Blue;
            case CubeType.Blue:
                return CubeType.Black;
            case CubeType.Black:
                return CubeType.Normal; // This will trigger removal in ToggleCubeAtPosition
            default:
                return CubeType.Normal;
        }
    }

    /// <summary>Sync the grid configurator's gridState with our internal wave state</summary>
    private void SyncGridConfigWithWaveState()
    {
        // Clear the grid first
        for (int x = 0; x < gridConfig.WaveWidth; x++)
        {
            for (int y = 0; y < gridConfig.WaveHeight; y++)
            {
                gridConfig.gridState[x, y] = 0; // Empty
            }
        }

        // Set cubes from wave state
        foreach (var kvp in waveState)
        {
            Vector2Int pos = kvp.Key;
            CubeType cubeType = kvp.Value;

            if (pos.x >= 0 && pos.x < gridConfig.WaveWidth &&
                pos.y >= 0 && pos.y < gridConfig.WaveHeight)
            {
                gridConfig.gridState[pos.x, pos.y] = (int)cubeType + 1;
            }
        }

        Debug.Log($"Synced grid config with wave state. Wave state has {waveState.Count} cubes");
        Debug.Log($"Grid state preview: {string.Join(", ", waveState.Select(kvp => $"({kvp.Key.x},{kvp.Key.y}):{gridConfig.gridState[kvp.Key.x, kvp.Key.y]}"))}");
    }

    #endregion

    #region Wave Loading and Saving

    /// <summary>Load a wave from WaveData asset</summary>
    public void LoadWave(WaveData waveData)
    {
        if (waveData == null)
        {
            Debug.LogWarning("Cannot load null wave data");
            return;
        }

        Debug.Log($"Loading wave: {waveData.name}");

        // Don't stop tracking if we're just loading a blueprint
        // Only stop if we're forcing a new wave

        // Update grid dimensions first
        gridConfig.SetWaveDimensions(waveData.GridWidth, waveData.GridHeight);

        // Ensure grid is big enough
        if (gridManager != null)
        {
            int requiredGridWidth = Mathf.Max(gridManager.width, waveData.GridWidth);
            int requiredGridHeight = Mathf.Max(gridManager.height, waveData.GridHeight * 3);

            if (requiredGridWidth > gridManager.width || requiredGridHeight > gridManager.height)
            {
                UpdateGridDimensions(requiredGridWidth, requiredGridHeight);
            }
        }

        // Clear current state
        waveState.Clear();

        // Load cube data
        foreach (var cubeData in waveData.CubesData)
        {
            Vector2Int pos = new Vector2Int(cubeData.position.x, cubeData.position.y);
            waveState[pos] = cubeData.type;

            Debug.Log($"Loading cube: {cubeData.type} at position ({pos.x}, {pos.y}) in {gridConfig.WaveWidth}x{gridConfig.WaveHeight} grid");
        }

        // Sync with grid configurator
        SyncGridConfigWithWaveState();

        // Store reference and update UI state
        currentWaveData = waveData;
        lastLoadedWaveName = waveData.name;
        isDirty = false;

        Debug.Log($"Loaded wave: {waveData.name} ({waveData.GridWidth}x{waveData.GridHeight}) with {waveData.CubesData.Count} cubes");
    }

    /// <summary>Create a new WaveData from current wave state</summary>
    public WaveData CreateWaveDataFromCurrentState()
    {
        WaveData newWave = ScriptableObject.CreateInstance<WaveData>();

        // Basic properties
        newWave.GridWidth = gridConfig.WaveWidth;
        newWave.GridHeight = gridConfig.WaveHeight;
        newWave.Index = 0;

        // Default wave settings
        newWave.limitMarkers = true;
        newWave.maxMarkerCharge = 2;
        newWave.maxMarkerCount = 99;
        newWave.waveStartDelay = 0.75f;
        newWave.moveInterval = 0.75f;
        newWave.fastMoveInterval = 0.1f;

        // Create cube data list
        newWave.CubesData = new List<CubeData>();

        foreach (var kvp in waveState)
        {
            // Convert display coordinates back to wave data coordinates
            int waveX = kvp.Key.x;
            int waveY = (gridConfig.WaveHeight - 1) - kvp.Key.y;

            CubeData cubeData = new CubeData
            {
                type = kvp.Value,
                position = new Vector2Int(waveX, waveY),
                level = 1
            };
            newWave.CubesData.Add(cubeData);
        }

        // Copy messages if we have a current wave loaded
        if (currentWaveData != null)
        {
            newWave.messages = new List<WaveMessage>(currentWaveData.messages);
            newWave.maxMarkerCharge = currentWaveData.maxMarkerCharge;
            newWave.maxMarkerCount = currentWaveData.maxMarkerCount;
            newWave.waveStartDelay = currentWaveData.waveStartDelay;
            newWave.moveInterval = currentWaveData.moveInterval;
            newWave.fastMoveInterval = currentWaveData.fastMoveInterval;
        }
        else
        {
            newWave.messages = new List<WaveMessage>();
        }

        return newWave;
    }

    /// <summary>Save current wave state as a new asset</summary>
    public void SaveCurrentWaveAsAsset()
    {
        WaveData waveToSave = CreateWaveDataFromCurrentState();

        if (!System.IO.Directory.Exists(saveLocation))
        {
            System.IO.Directory.CreateDirectory(saveLocation);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = string.IsNullOrEmpty(lastLoadedWaveName)
            ? $"Wave_{timestamp}"
            : $"{lastLoadedWaveName}_Modified_{timestamp}";

        string assetPath = $"{saveLocation}{fileName}.asset";

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(waveToSave, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Wave saved to: {assetPath}");

        // Update our current reference
        currentWaveData = waveToSave;
        lastLoadedWaveName = fileName;
        isDirty = false;
#else
        Debug.LogWarning("Save functionality only available in editor");
#endif
    }

    /// <summary>Reset the wave to empty state</summary>
    public void ResetWave()
    {
        StopTracking();
        waveState.Clear();

        // Clear the grid configurator state as well
        gridConfig.ClearGrid();

        // Sync to ensure everything is cleared
        SyncGridConfigWithWaveState();

        currentWaveData = null;
        lastLoadedWaveName = "";
        isDirty = false;

        Debug.Log("Wave reset to empty state");
    }

    #endregion

    #region Wave Spawning

    /// <summary>Spawn the current wave configuration</summary>
    public void SpawnWave()
    {
        if (waveManager == null)
        {
            Debug.LogError("WaveManager not found!");
            return;
        }

        // Create wave data from current state
        WaveData waveToSpawn = CreateWaveDataFromCurrentState();

        if (waveToSpawn.CubesData.Count == 0)
        {
            Debug.LogWarning("No cubes to spawn!");
            return;
        }

        // Ensure grid is properly sized
        gridConfig.ApplyGridSize();

        // Convert local positions to world grid positions
        foreach (var cubeData in waveToSpawn.CubesData)
        {
            // Position cubes at the top of the grid
            int worldY = gridManager.Height - gridConfig.WaveHeight + cubeData.position.y;
            cubeData.position = new Vector2Int(cubeData.position.x, worldY);
        }

        // Stop any current wave
        waveManager.ClearAllCubes();

        // Configure wave manager
        waveManager.useWaveConfiguration = true;
        waveManager.waveConfiguration = new List<WaveData> { waveToSpawn };

        // Reset data collector
        dataCollector.Reset();

        // Start the wave
        waveManager.StartWave();

        // Begin tracking after a short delay
        StartCoroutine(DelayedTrackingStart());

        Debug.Log($"Spawned wave with {waveToSpawn.CubesData.Count} cubes");
    }

    private IEnumerator DelayedTrackingStart()
    {
        yield return new WaitForSeconds(0.1f);
        StartTracking();
    }

    #endregion

    #region Active Wave Tracking

    /// <summary>Start tracking the active wave cubes</summary>
    public void StartTracking()
    {
        if (waveManager == null || waveManager.activeCubes.Count == 0)
        {
            Debug.LogWarning("No active cubes to track");
            return;
        }

        isTrackingActiveWave = true;
        trackedCubes = new List<CubeBehavior>(waveManager.activeCubes);

        // Calculate wave offset
        CalculateWaveOffset();

        // Sync wave state with actual cubes
        SyncWaveStateWithActiveCubes();

        Debug.Log($"Started tracking {trackedCubes.Count} active cubes with offset Y={waveOffsetY}");
    }

    /// <summary>Stop tracking active wave</summary>
    public void StopTracking()
    {
        isTrackingActiveWave = false;
        trackedCubes.Clear();
        waveOffsetY = 0;

        Debug.Log("Stopped tracking active wave");
    }

    private void UpdateActiveWaveTracking()
    {
        if (!isTrackingActiveWave || waveManager == null)
            return;

        // Remove destroyed cubes
        trackedCubes = trackedCubes.Where(c => c != null && !c.isDestroyed).ToList();

        // Check if wave is complete
        if (trackedCubes.Count == 0 && waveManager.activeCubes.Count == 0)
        {
            StopTracking();
            return;
        }

        // Update tracked cubes list if new cubes appeared
        foreach (var cube in waveManager.activeCubes)
        {
            if (!trackedCubes.Contains(cube))
            {
                trackedCubes.Add(cube);
                dataCollector.RecordCubeSpawned(cube);
            }
        }

        // Record removed cubes
        var currentActiveCubes = new HashSet<CubeBehavior>(waveManager.activeCubes);
        foreach (var cube in trackedCubes.ToList())
        {
            if (!currentActiveCubes.Contains(cube))
            {
                dataCollector.RecordCubeRemoved(cube);
                trackedCubes.Remove(cube);
            }
        }
    }

    private void CalculateWaveOffset()
    {
        if (trackedCubes.Count == 0) return;

        int minY = trackedCubes.Min(c => c.position.y);
         waveOffsetY = minY;
    }

    private void SyncWaveStateWithActiveCubes()
    {
        waveState.Clear();

        foreach (var cube in trackedCubes)
        {
            int localX = cube.position.x;
            int localY = cube.position.y - waveOffsetY;

            // Flip Y coordinate to match UI display (UI shows top-to-bottom, wave data is bottom-to-top)
            int displayY = (gridConfig.WaveHeight - 1) - localY;

            if (localX >= 0 && localX < gridConfig.WaveWidth &&
                displayY >= 0 && displayY < gridConfig.WaveHeight)
            {
                waveState[new Vector2Int(localX, displayY)] = cube.type;
            }
        }

        SyncGridConfigWithWaveState();
    }

    private void CheckForWaveManagerUpdates()
    {
        if (waveManager == null) return;

        // Check if wave manager has an active wave and we're not tracking
        bool hasActiveCubes = waveManager.activeCubes.Count > 0;
        bool waveChanged = waveManager.CurrentWave != lastTrackedWave;
        bool waveIndexChanged = waveManager.CurrentWaveIndex != lastWaveIndex;

        if (waveChanged || waveIndexChanged)
        {
            Debug.Log($"Wave change detected: CurrentWave={waveManager.CurrentWave?.name}, Index={waveManager.CurrentWaveIndex}");
            SyncWithWaveManager();
        }
        else if (hasActiveCubes && !isTrackingActiveWave)
        {
            // New cubes spawned, start tracking
            StartTracking();
        }
        else if (!hasActiveCubes && isTrackingActiveWave)
        {
            // Wave ended, stop tracking but keep the wave data loaded
            StopTracking();
        }
    }

    /// <summary>Sync with current WaveManager state</summary>
    private void SyncWithWaveManager()
    {
        if (waveManager == null) return;

        // Update tracking variables
        lastTrackedWave = waveManager.CurrentWave;
        lastWaveIndex = waveManager.CurrentWaveIndex;

        // Check if there's an active wave to track
        if (waveManager.waveActive && waveManager.activeCubes.Count > 0)
        {
            Debug.Log("Active wave detected - starting tracking");
            StartTracking();
        }
        else if (waveManager.CurrentWave != null)
        {
            // Load the current wave configuration
            Debug.Log($"Loading current wave configuration: {waveManager.CurrentWave.name}");
            LoadWave(waveManager.CurrentWave);
        }
        else
        {
            // No wave active, initialize empty
            Debug.Log("No active wave - initializing empty state");
            InitializeWaveState();
        }
    }

    #endregion

    #region Active Cube Manipulation

    private void UpdateActiveCubeAtPosition(int localX, int localY)
    {
        if (!isTrackingActiveWave) return;

        int worldY = localY + waveOffsetY;

        // Find existing cube at this position
        CubeBehavior existingCube = trackedCubes.FirstOrDefault(c =>
            c.position.x == localX && c.position.y == worldY);

        Vector2Int localPos = new Vector2Int(localX, localY);

        if (waveState.ContainsKey(localPos))
        {
            // Should have a cube here
            CubeType desiredType = waveState[localPos];

            if (existingCube == null)
            {
                // Create new cube
                SpawnCubeAtPosition(localX, worldY, desiredType);
            }
            else if (existingCube.type != desiredType)
            {
                // Replace existing cube
                ReplaceCubeAtPosition(existingCube, desiredType);
            }
        }
        else
        {
            // Should not have a cube here
            if (existingCube != null)
            {
                RemoveCubeAtPosition(existingCube);
            }
        }
    }

    private void RemoveActiveCubeAtPosition(int localX, int localY)
    {
        if (!isTrackingActiveWave) return;

        int worldY = localY + waveOffsetY;
        CubeBehavior existingCube = trackedCubes.FirstOrDefault(c =>
            c.position.x == localX && c.position.y == worldY);

        if (existingCube != null)
        {
            RemoveCubeAtPosition(existingCube);
        }
    }

    private void SpawnCubeAtPosition(int x, int y, CubeType cubeType)
    {
        if (waveManager == null || waveManager.cubePrefabs == null) return;

        int prefabIndex = (int)cubeType;
        if (prefabIndex < 0 || prefabIndex >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Invalid cube type: {cubeType}");
            return;
        }

        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 1f);
        GameObject cubeObj = Instantiate(waveManager.cubePrefabs[prefabIndex], worldPos, Quaternion.identity);

        CubeBehavior cube = cubeObj.GetComponent<CubeBehavior>();
        if (cube == null)
        {
            cube = cubeObj.AddComponent<CubeBehavior>();
        }

        CubeData cubeData = new CubeData
        {
            type = cubeType,
            position = new Vector2Int(x, y),
            level = 1
        };

        cube.Init(gridManager, cubeData);

        // Add to tracking
        trackedCubes.Add(cube);
        waveManager.activeCubes.Add(cube);

        Debug.Log($"Spawned {cubeType} cube at ({x}, {y})");
    }

    private void ReplaceCubeAtPosition(CubeBehavior oldCube, CubeType newType)
    {
        Vector2Int position = oldCube.position;
        Vector3 worldPos = oldCube.transform.position;

        // Remove old cube
        RemoveCubeAtPosition(oldCube);

        // Spawn new cube
        SpawnCubeAtPosition(position.x, position.y, newType);
    }

    private void RemoveCubeAtPosition(CubeBehavior cube)
    {
        trackedCubes.Remove(cube);
        waveManager.activeCubes.Remove(cube);

        if (cube.gameObject != null)
        {
            DestroyImmediate(cube.gameObject);
        }
    }

    #endregion

    #region Public Interface

    /// <summary>Get the current wave state for UI display</summary>
    public Dictionary<Vector2Int, CubeType> GetCurrentWaveState()
    {
        return new Dictionary<Vector2Int, CubeType>(waveState);
    }

    /// <summary>Check if the current wave has unsaved changes</summary>
    public bool HasUnsavedChanges()
    {
        return isDirty;
    }

    /// <summary>Get information about the currently loaded wave</summary>
    public string GetCurrentWaveInfo()
    {
        if (currentWaveData != null)
        {
            return $"{currentWaveData.name} ({currentWaveData.GridWidth}x{currentWaveData.GridHeight})";
        }
        else if (!string.IsNullOrEmpty(lastLoadedWaveName))
        {
            return $"{lastLoadedWaveName} (Modified)";
        }
        else
        {
            return $"New Wave ({gridConfig.WaveWidth}x{gridConfig.WaveHeight})";
        }
    }

    /// <summary>Get the current cube count</summary>
    public int GetCubeCount()
    {
        return waveState.Count;
    }

    /// <summary>Get cube count by type</summary>
    public int GetCubeCountByType(CubeType cubeType)
    {
        return waveState.Values.Count(t => t == cubeType);
    }

    #endregion

    #region Wave Control Methods

    /// <summary>Start/Resume the current wave</summary>
    public void StartWave()
    {
        EnsureWaveManagerState();
        if (waveManager == null) return;

        // If we have cubes but wave isn't active, resume
        if (waveManager.activeCubes.Count > 0 && !waveManager.waveActive)
        {
            waveManager.debugMode = true;
            waveManager.manualControl = false;
            waveManager.ResumeWave();
            Debug.Log("Wave resumed");
        }
        // If no cubes, spawn the current wave
        else if (waveManager.activeCubes.Count == 0)
        {
            SpawnWave();
        }
        // If wave is already active, just ensure it's not in manual mode
        else if (waveManager.waveActive)
        {
            waveManager.manualControl = false;
            Debug.Log("Wave already active, disabled manual control");
        }
    }

    /// <summary>Stop/Pause the current wave</summary>
    public void StopWave()
    {
        EnsureWaveManagerState();
        if (waveManager == null) return;

        if (waveManager.waveActive)
        {
            waveManager.PauseWave();
            Debug.Log("Wave paused - manual control enabled");
        }
        else
        {
            // Force manual control even if wave isn't active
            waveManager.debugMode = true;
            waveManager.manualControl = true;
            Debug.Log("Manual control enabled");
        }
    }
    /// <summary>Ensure wave manager is in correct state for debugger operations</summary>
    private void EnsureWaveManagerState()
    {
        if (waveManager == null) return;

        // Always ensure debug mode is enabled when using debugger
        if (!waveManager.debugMode)
        {
            waveManager.debugMode = true;
            Debug.Log("Enabled debug mode for wave manager");
        }
    }
    /// <summary>Reset the current wave</summary>
    public void ResetCurrentWave()
    {
        if (waveManager == null) return;

        // Clear all active cubes
        waveManager.ClearAllCubes();

        // Stop tracking
        StopTracking();

        // If we have a current wave loaded, respawn it
        if (currentWaveData != null)
        {
            SpawnWave();
        }

        Debug.Log("Wave reset");
    }

    /// <summary>Manually step the wave forward one move</summary>
    public void StepWaveForward()
    {
        EnsureWaveManagerState();
        if (waveManager == null) return;

        if (waveManager.manualControl)
        {
            waveManager.ManualMoveWaveForward();
            Debug.Log("Wave stepped forward manually");
        }
        else
        {
            Debug.LogWarning("Wave must be stopped to step manually");
        }
    }

    /// <summary>Force load a specific wave (even if one is active)</summary>
    public void ForceLoadWave(WaveData waveData)
    {
        if (waveData == null) return;

        // Clear any active cubes but don't break wave manager state
        if (waveManager != null && waveManager.activeCubes.Count > 0)
        {
            waveManager.ClearAllCubes();
            StopTracking();
        }

        // Load the wave data
        LoadWave(waveData);

        Debug.Log($"Force loaded wave: {waveData.name}");
    }

    #endregion

    #region Grid Management Methods

    /// <summary>Update grid dimensions and auto-resize if needed</summary>
    public void UpdateGridDimensions(int newGridWidth, int newGridHeight)
    {
        if (gridManager == null) return;

        // Ensure grid is at least as big as the wave
        int requiredWidth = Mathf.Max(newGridWidth, gridConfig.WaveWidth);
        int requiredHeight = Mathf.Max(newGridHeight, gridConfig.WaveHeight * 3);

        Debug.Log($"Updating grid from {gridManager.width}x{gridManager.height} to {requiredWidth}x{requiredHeight}");

        // Store current wave state
        var tempWaveState = new Dictionary<Vector2Int, CubeType>(waveState);

        // Use the proper resize method
        gridManager.ResizeGrid(requiredWidth, requiredHeight);

        // Update grid configurator to match
        gridConfig.gridWidth = requiredWidth;
        gridConfig.gridHeight = requiredHeight;

        // Reinitialize grid configurator arrays
        gridConfig.InitializeGrid();

        // Restore wave state if it fits in new dimensions
        waveState.Clear();
        foreach (var kvp in tempWaveState)
        {
            if (kvp.Key.x < gridConfig.WaveWidth && kvp.Key.y < gridConfig.WaveHeight)
            {
                waveState[kvp.Key] = kvp.Value;
            }
        }

        // Sync everything
        SyncGridConfigWithWaveState();

        Debug.Log($"Grid successfully resized to {requiredWidth}x{requiredHeight}");
    }

    /// <summary>Update wave dimensions and auto-resize grid if needed</summary>
    public void UpdateWaveDimensions(int newWaveWidth, int newWaveHeight)
    {
        Debug.Log($"Updating wave dimensions from {gridConfig.WaveWidth}x{gridConfig.WaveHeight} to {newWaveWidth}x{newWaveHeight}");

        // Check if grid needs to be expanded
        bool needsGridResize = false;
        int requiredGridWidth = gridManager.width;
        int requiredGridHeight = gridManager.height;

        if (newWaveWidth > gridManager.width)
        {
            requiredGridWidth = newWaveWidth;
            needsGridResize = true;
        }

        if (newWaveHeight * 3 > gridManager.height)
        {
            requiredGridHeight = newWaveHeight * 3;
            needsGridResize = true;
        }

        // Resize grid first if needed
        if (needsGridResize)
        {
            UpdateGridDimensions(requiredGridWidth, requiredGridHeight);
        }

        // Update wave dimensions
        gridConfig.SetWaveDimensions(newWaveWidth, newWaveHeight);
        gridConfig.InitializeGrid();

        // Reinitialize wave state with new dimensions
        InitializeWaveState();

        Debug.Log($"Wave dimensions updated to {newWaveWidth}x{newWaveHeight}");
    }

    #endregion
    #region Wave State Access Methods

    /// <summary>Get current wave manager state for UI display</summary>
    public bool IsWaveActive()
    {
        return waveManager != null && waveManager.waveActive;
    }

    /// <summary>Get manual control state</summary>
    public bool IsManualControl()
    {
        return waveManager != null && waveManager.manualControl;
    }

    /// <summary>Get debug mode state</summary>
    public bool IsDebugMode()
    {
        return waveManager != null && waveManager.debugMode;
    }

    /// <summary>Set manual control mode</summary>
    public void SetManualControl(bool manual)
    {
        if (waveManager != null)
        {
            waveManager.manualControl = manual;
            Debug.Log($"Manual control set to: {manual}");
        }
    }

    /// <summary>Set debug mode</summary>
    public void SetDebugMode(bool debug)
    {
        if (waveManager != null)
        {
            waveManager.debugMode = debug;
            Debug.Log($"Debug mode set to: {debug}");
        }
    }

    /// <summary>Get active cube count</summary>
    public int GetActiveCubeCount()
    {
        return waveManager != null ? waveManager.activeCubes.Count : 0;
    }

    #endregion
}