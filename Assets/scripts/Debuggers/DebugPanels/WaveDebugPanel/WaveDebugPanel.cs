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
    private WaveGeneratorDebugPanel waveGeneratorPanel;

    // References
    private WaveManager waveManager;
    private GridManager gridManager;

    // UI State
    private bool showWaveControls = true;
    private bool showWaveEditor = true;
    private bool showCubeTools = true;
    private bool showWaveLibrary = true;
    private bool showWaveGenerator = false;
    
    // Fast Testing Mode
    private bool fastTestingMode = false;
    private bool previousMessageState = true;

    public override void Initialize()
    {
        base.Initialize(); // Initialize theme and performance systems
        
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
        
        waveGeneratorPanel = new WaveGeneratorDebugPanel();
        waveGeneratorPanel.Initialize(waveManager, gridManager);
    }

    public override void Update()
    {
        // Update sub-panels that need updates
        waveLibraryPanel?.Update();
    }

    protected override void DrawPanelContent()
    {
        DrawFastTestingControls();
        DrawSectionToggles();
        DebugUIHelpers.Space(5);

        // Draw active panels
        if (showWaveLibrary)
            waveLibraryPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnWaveChanged);

        if (showWaveControls)
            waveControlPanel?.DrawPanel(OnWaveChanged);

        if (showWaveEditor)
            waveEditorPanel?.DrawPanel(OnWaveChanged, OnSyncToGrid);

        if (showCubeTools)
            cubeToolsPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnSyncToGrid, OnCubeAdded, OnCubeRemoved);
            
        if (showWaveGenerator)
            waveGeneratorPanel?.DrawPanel(OnWaveChanged);
    }

    private void DrawFastTestingControls()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("🚀 FAST TESTING MODE", GUILayout.Width(140));
        
        bool newFastTestingMode = DebugUIHelpers.DrawToggleButton("Fast Testing", fastTestingMode, Color.yellow);
        if (newFastTestingMode != fastTestingMode)
        {
            SetFastTestingMode(newFastTestingMode);
        }
        
        if (fastTestingMode)
        {
            GUILayout.Label("(Messages Disabled)", GUILayout.Width(100));
            
            if (GUILayout.Button("Batch Test All", GUILayout.Width(100)))
            {
                BatchTestAllWaves();
            }
            
            if (GUILayout.Button("Quick Validate", GUILayout.Width(100)))
            {
                QuickValidateCurrentWave();
            }
        }
        
        GUILayout.EndHorizontal();
        DebugUIHelpers.Space(3);
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
        
        bool oldLibrary = showWaveLibrary;
        bool oldControls = showWaveControls;
        bool oldEditor = showWaveEditor;
        bool oldCubes = showCubeTools;
        bool oldGenerator = showWaveGenerator;
        
        showWaveLibrary = DrawSimpleToggle("Library", showWaveLibrary);
        showWaveControls = DrawSimpleToggle("Controls", showWaveControls);
        showWaveEditor = DrawSimpleToggle("Editor", showWaveEditor);
        showCubeTools = DrawSimpleToggle("Cubes", showCubeTools);
        showWaveGenerator = DrawSimpleToggle("Generator", showWaveGenerator);
        
        // Mark dirty if any toggle changed
        if (oldLibrary != showWaveLibrary || oldControls != showWaveControls ||
            oldEditor != showWaveEditor || oldCubes != showCubeTools ||
            oldGenerator != showWaveGenerator)
        {
            MarkDirty();
        }
        
        GUILayout.EndHorizontal();
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

    // Fast Testing Mode Methods
    private void SetFastTestingMode(bool enabled)
    {
        fastTestingMode = enabled;
        
        if (waveManager != null)
        {
            if (enabled)
            {
                // Store current message state and disable messages
                previousMessageState = waveManager.showMessages;
                waveManager.showMessages = false;
                Debug.Log("🚀 Fast Testing Mode ENABLED - Messages disabled for rapid testing");
            }
            else
            {
                // Restore previous message state
                waveManager.showMessages = previousMessageState;
                Debug.Log("🚀 Fast Testing Mode DISABLED - Messages restored");
            }
        }
    }

    private void BatchTestAllWaves()
    {
        if (waveLibraryPanel == null)
        {
            Debug.LogWarning("Wave library not available for batch testing");
            return;
        }

        var availableWaves = waveLibraryPanel.GetAvailableWaves();
        if (availableWaves == null || availableWaves.Count == 0)
        {
            Debug.LogWarning("No waves available for batch testing");
            return;
        }

        Debug.Log($"🧪 Starting batch test of {availableWaves.Count} waves...");
        
        int validWaves = 0;
        int invalidWaves = 0;
        
        foreach (var wave in availableWaves)
        {
            if (ValidateWave(wave))
            {
                validWaves++;
                Debug.Log($"✅ Wave '{wave.name}' is valid");
            }
            else
            {
                invalidWaves++;
                Debug.LogWarning($"❌ Wave '{wave.name}' has issues");
            }
        }
        
        Debug.Log($"🧪 Batch test complete: {validWaves} valid, {invalidWaves} invalid waves");
    }

    private void QuickValidateCurrentWave()
    {
        var currentWave = waveEditorPanel?.CurrentEditingWave;
        if (currentWave == null)
        {
            Debug.LogWarning("No current wave to validate");
            return;
        }

        if (ValidateWave(currentWave))
        {
            Debug.Log($"✅ Wave '{currentWave.name}' validation passed");
        }
        else
        {
            Debug.LogWarning($"❌ Wave '{currentWave.name}' validation failed");
        }
    }

    private bool ValidateWave(WaveData wave)
    {
        if (wave == null) return false;
        
        // Check basic properties
        if (string.IsNullOrEmpty(wave.name))
        {
            Debug.LogError($"Wave has no name");
            return false;
        }
        
        if (wave.GridWidth <= 0 || wave.GridHeight <= 0)
        {
            Debug.LogError($"Wave '{wave.name}' has invalid dimensions: {wave.GridWidth}x{wave.GridHeight}");
            return false;
        }
        
        if (wave.moveInterval <= 0 || wave.fastMoveInterval <= 0)
        {
            Debug.LogError($"Wave '{wave.name}' has invalid timing intervals");
            return false;
        }
        
        // Check cube data validity
        if (wave.CubesData != null)
        {
            foreach (var cube in wave.CubesData)
            {
                if (cube.position.x < 0 || cube.position.x >= wave.GridWidth ||
                    cube.position.y < 0 || cube.position.y >= wave.GridHeight)
                {
                    Debug.LogError($"Wave '{wave.name}' has cube at invalid position: ({cube.position.x}, {cube.position.y})");
                    return false;
                }
            }
        }
        
        return true;
    }
}

