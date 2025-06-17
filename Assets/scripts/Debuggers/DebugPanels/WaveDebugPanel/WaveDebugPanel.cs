using UnityEngine;
using static Enumerations;
using WaveDebugSystem;
using WaveDebugSystem.WaveDebugSystem;

public class WaveDebugPanel : DebugPanelBase
{
    public override string PanelName => "Wave Manager";
    public override DebugPanelGroup Group => DebugPanelGroup.Wave;

    // Sub-panels
    private WaveControlDebug waveControlPanel;
    private WaveEditorDebug waveEditorPanel;
    private CubeToolsDebug cubeToolsPanel;
    private WaveLibraryDebug waveLibraryPanel;

    // References
    private WaveManager waveManager;
    private GridManager gridManager;

    // UI State
    private bool showWaveControls = true;
    private bool showWaveEditor = true;
    private bool showCubeTools = true;
    private bool showWaveLibrary = true;

    public override void Initialize()
    {
        // Find references
        waveManager = Object.FindObjectOfType<WaveManager>();
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();

        // Initialize sub-panels

        waveLibraryPanel = new WaveLibraryDebug();
        waveLibraryPanel.Initialize(waveManager, gridManager);

        waveControlPanel = new WaveControlDebug();
        waveControlPanel.Initialize(waveManager, gridManager);

        waveEditorPanel = new WaveEditorDebug();
        waveEditorPanel.Initialize(waveManager, gridManager);

        cubeToolsPanel = new CubeToolsDebug();
        cubeToolsPanel.Initialize(waveManager, gridManager);

    }

    public override void Update()
    {
        // Update sub-panels that need updates
        waveLibraryPanel?.Update();
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        // Draw active panels
        if (showWaveLibrary)
            waveLibraryPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnWaveChanged);

        if (showWaveControls)
            waveControlPanel?.DrawPanel(OnWaveChanged);

        if (showWaveEditor)
            waveEditorPanel?.DrawPanel(OnWaveChanged, OnSyncToGrid);

        if (showCubeTools)
            cubeToolsPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnSyncToGrid, OnCubeAdded, OnCubeRemoved);


    }
    private void OnCubeAdded(Vector2Int gridPosition, CubeType cubeType)
    {
        // Notify wave editor that a cube was added
        waveEditorPanel?.AddCubeToWave(gridPosition, cubeType);
        Debug.Log($"Added {cubeType} cube to wave at grid ({gridPosition.x}, {gridPosition.y})");
    }

    private void OnCubeRemoved(Vector2Int gridPosition)
    {
        // Notify wave editor that a cube was removed
        waveEditorPanel?.RemoveCubeFromWave(gridPosition);
        Debug.Log($"Removed cube from wave at grid ({gridPosition.x}, {gridPosition.y})");
    }
    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showWaveLibrary = DrawToggleButton("Library", showWaveLibrary);
        showWaveControls = DrawToggleButton("Controls", showWaveControls);
        showWaveEditor = DrawToggleButton("Editor", showWaveEditor);
        showCubeTools = DrawToggleButton("Cubes", showCubeTools);
        
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    // Callback handlers for inter-panel communication
    private void OnWaveChanged(WaveData newWave)
    {
        if (newWave != null)
        {
            waveEditorPanel?.LoadWaveForEditing(newWave);

            // Auto-sync to grid if enabled
            if (waveControlPanel?.GetAutoSyncToGrid() == true)
            {
                OnSyncToGrid();
            }

            Debug.Log($"Wave changed to: {newWave.name} with {newWave.CubesData.Count} cubes");
        }
    }

    private void OnSyncToGrid()
    {
        var currentWave = waveEditorPanel?.CurrentEditingWave;
        if (currentWave == null || waveManager == null) return;

        // Sync grid to wave data OR capture grid state to wave
        if (waveManager.activeCubes.Count > 0)
        {
            // Grid has cubes - capture them to wave
            waveEditorPanel?.SyncWaveDataToGrid();
        }
        else
        {
            // Grid is empty - sync wave data to grid
            SyncWaveToGrid(currentWave);
        }
    }

    private void SyncWaveToGrid(WaveData wave)
    {
        if (wave == null || waveManager == null || gridManager == null) return;

        // Clear current cubes
        waveManager.ClearAllCubes();

        // Enable wave configuration mode
        waveManager.useWaveConfiguration = true;

        // Spawn cubes from wave configuration - Always spawn at top of grid
        foreach (var cubeData in wave.CubesData)
        {
            SpawnCubeFromData(cubeData, true);
        }

        Debug.Log($"Synced wave '{wave.name}' to grid - spawned {wave.CubesData.Count} cubes at top of grid");
    }

    private void SpawnCubeFromData(CubeData cubeData, bool forceTopSpawn = true)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeData.type >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Cannot spawn cube type {cubeData.type} - prefab not available");
            return;
        }

        // Always spawn at the top of the grid, ignoring wave Y
        Vector2Int spawnPosition = cubeData.position;
        if (forceTopSpawn)
        {
            spawnPosition.y = gridManager.Height - 1;
        }

        if (!gridManager.IsValidGridPosition(spawnPosition))
        {
            Debug.LogWarning($"Cannot spawn cube at invalid position ({spawnPosition.x}, {spawnPosition.y})");
            return;
        }

        Vector3 worldPos = gridManager.GridToWorldPosition(spawnPosition.x, spawnPosition.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeData.type], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        var adjustedCubeData = new CubeData
        {
            type = cubeData.type,
            position = spawnPosition,
            level = cubeData.level
        };

        cube.Init(gridManager, adjustedCubeData, 2f);
        waveManager.activeCubes.Add(cube);

        Debug.Log($"Spawned {cubeData.type} cube at grid ({spawnPosition.x}, {spawnPosition.y}) -> world {worldPos}");
    }

    // Public methods for external access (if needed)
    public WaveData GetCurrentEditingWave() => waveEditorPanel?.CurrentEditingWave;

    public void LoadWaveForEditing(WaveData wave)
    {
        waveEditorPanel?.LoadWaveForEditing(wave);
    }

    public void CreateNewWave()
    {
        waveEditorPanel?.CreateNewWave();
    }

    public void RefreshWaveLibrary()
    {
        waveLibraryPanel?.ForceRefresh();
    }
}

