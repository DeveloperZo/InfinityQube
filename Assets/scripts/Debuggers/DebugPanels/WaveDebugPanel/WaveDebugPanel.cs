using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static Enumerations;
using WaveDebugSystem.WaveDebugSystem;
using WaveDebugSystem;

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
    private bool showWaveLibrary = false;

    public override void Initialize()
    {
        // Find references
        waveManager = Object.FindObjectOfType<WaveManager>();
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();

        // Initialize sub-panels
        waveControlPanel = new WaveControlDebug();
        waveControlPanel.Initialize(waveManager, gridManager);

        waveEditorPanel = new WaveEditorDebug();
        waveEditorPanel.Initialize(waveManager, gridManager);

        cubeToolsPanel = new CubeToolsDebug();
        cubeToolsPanel.Initialize(waveManager, gridManager);

        waveLibraryPanel = new WaveLibraryDebug();
        waveLibraryPanel.Initialize(waveManager, gridManager);
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
        if (showWaveControls)
            waveControlPanel?.DrawPanel(OnWaveChanged);

        if (showWaveEditor)
            waveEditorPanel?.DrawPanel(OnWaveChanged, OnSyncToGrid);

        if (showCubeTools)
            cubeToolsPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnSyncToGrid);

        if (showWaveLibrary)
            waveLibraryPanel?.DrawPanel(waveEditorPanel?.CurrentEditingWave, OnWaveChanged);
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showWaveControls = DrawToggleButton("Controls", showWaveControls);
        showWaveEditor = DrawToggleButton("Editor", showWaveEditor);
        showCubeTools = DrawToggleButton("Cubes", showCubeTools);
        showWaveLibrary = DrawToggleButton("Library", showWaveLibrary);
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
        }
    }

    private void OnSyncToGrid()
    {
        var currentWave = waveEditorPanel?.CurrentEditingWave;
        if (currentWave == null || waveManager == null) return;

        SyncWaveToGrid(currentWave);
    }

    private void SyncWaveToGrid(WaveData wave)
    {
        if (wave == null || waveManager == null || gridManager == null) return;

        // Clear current cubes
        waveManager.ClearAllCubes();

        // Spawn cubes from wave configuration - Fixed to spawn at top of grid
        waveManager.useWaveConfiguration = true;
        foreach (var cubeData in wave.CubesData)
        {
            SpawnCubeFromData(cubeData);
        }

        Debug.Log($"Synced wave '{wave.name}' to grid - spawned {wave.CubesData.Count} cubes");
    }

    private void SpawnCubeFromData(CubeData cubeData)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeData.type >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Cannot spawn cube type {cubeData.type} - prefab not available");
            return;
        }

        if (!gridManager.IsValidGridPosition(cubeData.position))
        {
            Debug.LogWarning($"Cannot spawn cube at invalid position ({cubeData.position.x}, {cubeData.position.y})");
            return;
        }

        // Spawn at the specified grid position with height offset
        Vector3 worldPos = gridManager.GridToWorldPosition(cubeData.position.x, cubeData.position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeData.type], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        cube.Init(gridManager, cubeData, 2f);
        waveManager.activeCubes.Add(cube);

        Debug.Log($"Spawned {cubeData.type} cube at grid ({cubeData.position.x}, {cubeData.position.y}) -> world {worldPos}");
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
