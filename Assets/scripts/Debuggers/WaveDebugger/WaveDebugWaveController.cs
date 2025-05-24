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

    private void Awake()
    {
        InitializeReferences();
    }

    private void Update()
    {
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

    #region Wave State Management

    /// <summary>Initialize the internal wave state based on grid configurator</summary>
    public void InitializeWaveState()
    {
        waveState.Clear();

        for (int x = 0; x < gridConfig.WaveWidth; x++)
        {
            for (int y = 0; y < gridConfig.WaveHeight; y++)
            {
                int gridValue = gridConfig.GridState[x, y];
                if (gridValue > 0)
                {
                    CubeType cubeType = (CubeType)(gridValue - 1);
                    waveState[new Vector2Int(x, y)] = cubeType;
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

        // Cycle through cube types: Normal -> Blue -> Black -> Empty -> Normal
        CubeType currentType = waveState.ContainsKey(pos) ? waveState[pos] : CubeType.Normal;
        CubeType newType = GetNextCubeType(currentType);

        // Update internal state
        if (newType == CubeType.Normal && !waveState.ContainsKey(pos))
        {
            // Special case: if we're cycling to Normal but there's no cube, place Normal
            waveState[pos] = CubeType.Normal;
        }
        else if (newType == CubeType.Normal && currentType != CubeType.Normal)
        {
            // If cycling from another type to Normal
            waveState[pos] = CubeType.Normal;
        }
        else if (newType == CubeType.Normal)
        {
            // If cycling from Normal, remove the cube (empty space)
            waveState.Remove(pos);
        }
        else
        {
            waveState[pos] = newType;
        }

        // Update grid configurator to stay in sync
        SyncGridConfigWithWaveState();

        // If we're tracking an active wave, update the actual cubes
        if (isTrackingActiveWave)
        {
            UpdateActiveCubeAtPosition(x, y);
        }

        isDirty = true;
        Debug.Log($"Toggled cube at ({x}, {y}) to {(waveState.ContainsKey(pos) ? waveState[pos].ToString() : "Empty")}");
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

    /// <summary>Sync the grid configurator's GridState with our internal wave state</summary>
    private void SyncGridConfigWithWaveState()
    {
        // Clear the grid first
        for (int x = 0; x < gridConfig.WaveWidth; x++)
        {
            for (int y = 0; y < gridConfig.WaveHeight; y++)
            {
                gridConfig.GridState[x, y] = 0; // Empty
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
                gridConfig.GridState[pos.x, pos.y] = (int)cubeType + 1;
            }
        }
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

        // Stop any active tracking
        StopTracking();

        // Update grid dimensions
        gridConfig.SetWaveDimensions(waveData.GridWidth, waveData.GridHeight);
        gridConfig.InitializeGrid();

        // Clear current state
        waveState.Clear();

        // Load cube data
        foreach (var cubeData in waveData.CubesData)
        {
            Vector2Int pos = new Vector2Int(cubeData.position.x, cubeData.position.y);
            waveState[pos] = cubeData.type;
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
            CubeData cubeData = new CubeData
            {
                type = kvp.Value,
                position = kvp.Key,
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

            if (localX >= 0 && localX < gridConfig.WaveWidth &&
                localY >= 0 && localY < gridConfig.WaveHeight)
            {
                waveState[new Vector2Int(localX, localY)] = cube.type;
            }
        }

        SyncGridConfigWithWaveState();
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
}