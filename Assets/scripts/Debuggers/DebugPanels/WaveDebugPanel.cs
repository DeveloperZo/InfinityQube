using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static Enumerations;

public class WaveDebugPanel : DebugPanelBase
{
    public override string PanelName => "Wave Manager";
    public override DebugPanelGroup Group => DebugPanelGroup.Wave;

    private WaveManager waveManager;
    private GridManager gridManager;

    // UI State
    private bool showWaveControls = true;
    private bool showWaveEditor = true;
    private bool showCubeTools = true;
    private bool showWaveList = false;

    // Wave Editor State
    private WaveData currentEditingWave;
    private int selectedCubeType = 0; // Normal
    private bool isPlacementMode = false;
    private Vector2 waveListScroll;
    private Vector2 cubeListScroll;

    // Available wave assets
    private List<WaveData> availableWaves = new List<WaveData>();
    private bool needsWaveRefresh = true;

    // Grid sync state
    private bool autoSyncToGrid = true;

    public override void Initialize()
    {
        waveManager = Object.FindObjectOfType<WaveManager>();
        gridManager = GridManager.Instance;
        RefreshAvailableWaves();
    }

    public override void Update()
    {
        if (needsWaveRefresh)
        {
            RefreshAvailableWaves();
            needsWaveRefresh = false;
        }
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showWaveControls) DrawWaveControlsSection();
        if (showWaveEditor) DrawWaveEditorSection();
        if (showCubeTools) DrawCubeToolsSection();
        if (showWaveList) DrawWaveListSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showWaveControls = DrawToggleButton("Controls", showWaveControls);
        showWaveEditor = DrawToggleButton("Editor", showWaveEditor);
        showCubeTools = DrawToggleButton("Cubes", showCubeTools);
        showWaveList = DrawToggleButton("Waves", showWaveList);
        GUILayout.EndHorizontal();
    }
    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawWaveControlsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE CONTROLS", GUI.skin.box);

        if (waveManager != null)
        {
            // Current wave status
            GUILayout.Label($"Wave: {waveManager.CurrentWaveIndex + 1}/{waveManager.waveConfiguration.Count}");
            GUILayout.Label($"Step: {waveManager.MoveStep} | Active: {waveManager.waveActive}");
            GUILayout.Label($"Cubes: {waveManager.activeCubes.Count} | Speed: {(waveManager.isSpeedingUp ? "FAST" : "NORMAL")}");

            // Top actions
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start/Reset"))
            {
                waveManager.StopWave();
                waveManager.StartWave();
            }
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Pause")) waveManager.PauseWave();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Step ▶")) waveManager.ManualMoveWaveForward();
            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("◀ Step Back")) StepWaveBackward();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear"))
            {
                waveManager.StopWave();
                waveManager.ClearAllCubes();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Wave navigation
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀◀ Prev") && waveManager.currentWaveIndex > 0)
            {
                waveManager.currentWaveIndex--;
                LoadWaveForEditing(waveManager.CurrentWave);
                SyncWaveToGrid();
            }
            GUILayout.Label($"Wave {waveManager.currentWaveIndex + 1}", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Next ▶▶") && waveManager.currentWaveIndex < waveManager.waveConfiguration.Count - 1)
            {
                waveManager.currentWaveIndex++;
                LoadWaveForEditing(waveManager.CurrentWave);
                SyncWaveToGrid();
            }
            GUILayout.EndHorizontal();

            // Debug controls
            GUILayout.BeginHorizontal();
            bool newDebug = GUILayout.Toggle(waveManager.debugMode, "Debug Mode");
            if (newDebug != waveManager.debugMode)
            {
                if (newDebug) waveManager.EnterDebugMode(true);
                else waveManager.ExitDebugMode();
            }
            autoSyncToGrid = GUILayout.Toggle(autoSyncToGrid, "Auto Sync Grid");
            GUILayout.EndHorizontal();

            // Current wave info
            if (waveManager.CurrentWave != null)
            {
                var wave = waveManager.CurrentWave;
                GUILayout.Label($"Current: {wave.name} | {wave.GridWidth}x{wave.GridHeight} | {wave.CubesData.Count} cubes");
                GUILayout.Label($"Timing: {wave.moveInterval:F1}s / {wave.fastMoveInterval:F1}s");
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawWaveEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE EDITOR", GUI.skin.box);

        // Quick actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("New Wave")) CreateNewWave();
        if (GUILayout.Button("Load Current")) LoadCurrentWaveForEditing();
        if (GUILayout.Button("Save Wave") && currentEditingWave != null) SaveCurrentWave();
        if (GUILayout.Button("Refresh Assets")) needsWaveRefresh = true;
        GUILayout.EndHorizontal();

        if (currentEditingWave != null)
        {
            GUILayout.Space(3);

            // Wave name
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(40));
            string newName = GUILayout.TextField(currentEditingWave.name);
            if (newName != currentEditingWave.name)
            {
                currentEditingWave.name = newName;
            }
            GUILayout.EndHorizontal();

            // Dimensions with increment/decrement
            DrawDimensionControls();

            // Wave properties
            DrawWavePropertiesEditor();

            // Test and sync controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Wave")) TestCurrentWave();
            if (GUILayout.Button("Sync to Grid")) SyncWaveToGrid();
            if (GUILayout.Button("Clear All Cubes"))
            {
                currentEditingWave.CubesData.Clear();
                if (autoSyncToGrid) SyncWaveToGrid();
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void DrawDimensionControls()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Dimensions:");

        // Grid dimensions
        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid:", GUILayout.Width(35));
        if (GUILayout.Button("-", GUILayout.Width(20)) && gridManager.Width > 3)
        {
            gridManager.ResizeGrid(gridManager.Width - 1, gridManager.Height);
        }
        GUILayout.Label($"{gridManager.Width}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            gridManager.ResizeGrid(gridManager.Width + 1, gridManager.Height);
        }

        GUILayout.Label("x", GUILayout.Width(10));

        if (GUILayout.Button("-", GUILayout.Width(20)) && gridManager.Height > 10)
        {
            gridManager.ResizeGrid(gridManager.Width, gridManager.Height - 1);
        }
        GUILayout.Label($"{gridManager.Height}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            gridManager.ResizeGrid(gridManager.Width, gridManager.Height + 1);
        }
        GUILayout.EndHorizontal();

        // Wave dimensions
        GUILayout.BeginHorizontal();
        GUILayout.Label("Wave:", GUILayout.Width(35));
        if (GUILayout.Button("-", GUILayout.Width(20)) && currentEditingWave.GridWidth > 1)
        {
            currentEditingWave.GridWidth--;
            ClampCubesToWaveBounds();
        }
        GUILayout.Label($"{currentEditingWave.GridWidth}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(20)) && currentEditingWave.GridWidth < gridManager.Width)
        {
            currentEditingWave.GridWidth++;
        }

        GUILayout.Label("x", GUILayout.Width(10));

        if (GUILayout.Button("-", GUILayout.Width(20)) && currentEditingWave.GridHeight > 1)
        {
            currentEditingWave.GridHeight--;
            ClampCubesToWaveBounds();
        }
        GUILayout.Label($"{currentEditingWave.GridHeight}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            currentEditingWave.GridHeight++;
        }
        GUILayout.EndHorizontal();

        // Validation
        if (currentEditingWave.GridWidth > gridManager.Width)
        {
            GUI.color = Color.red;
            GUILayout.Label("Wave width exceeds grid width!");
            GUI.color = Color.white;
        }

        GUILayout.EndVertical();
    }

    private void DrawCubeToolsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE TOOLS", GUI.skin.box);

        if (currentEditingWave != null)
        {
            // Cube type selector
            GUILayout.Label("Cube Type:");
            GUILayout.BeginHorizontal();
            if (DrawCubeTypeButton("Normal", 0, Color.gray)) selectedCubeType = 0;
            if (DrawCubeTypeButton("Blue", 1, Color.blue)) selectedCubeType = 1;
            if (DrawCubeTypeButton("Black", 2, Color.black)) selectedCubeType = 2;
            if (DrawCubeTypeButton("Reinforced", 3, Color.magenta)) selectedCubeType = 3;
            GUILayout.EndHorizontal();

            // Placement controls
            GUILayout.BeginHorizontal();
            isPlacementMode = GUILayout.Toggle(isPlacementMode, "Placement Mode");
            if (GUILayout.Button("Fill Random")) FillWaveRandom();
            if (GUILayout.Button("Fill Row")) FillTopRow();
            GUILayout.EndHorizontal();

            if (isPlacementMode)
            {
                GUILayout.Label("Click grid below to place/remove cubes");
            }

            // Visual cube grid editor - shows current wave or grid state
            DrawCubeGrid();

            GUILayout.Space(5);

            // Cube list with modification options
            DrawCubeList();
        }
        else
        {
            GUILayout.Label("Create or load a wave to edit cubes");
            if (GUILayout.Button("Quick New Wave"))
            {
                CreateNewWave();
            }
        }

        GUILayout.EndVertical();
    }

    private bool DrawCubeTypeButton(string label, int type, Color color)
    {
        GUI.backgroundColor = selectedCubeType == type ? color : Color.white;
        bool result = GUILayout.Button(label);
        GUI.backgroundColor = Color.white;
        return result;
    }
    private void DrawWaveListSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("AVAILABLE WAVES", GUI.skin.box);

        waveListScroll = GUILayout.BeginScrollView(waveListScroll, GUILayout.Height(200));

        // Show waves from assets/data/waves
        foreach (var wave in availableWaves)
        {
            if (wave == null) continue;

            bool isCurrent = currentEditingWave == wave;
            bool isActive = waveManager != null && waveManager.CurrentWave != null &&
                           waveManager.CurrentWave.name == wave.name;

            GUI.backgroundColor = isCurrent ? Color.yellow : (isActive ? Color.green : Color.white);
            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.Label(wave.name, GUILayout.Width(120));
            GUILayout.Label($"{wave.CubesData.Count}c", GUILayout.Width(30));
            GUILayout.Label($"{wave.GridWidth}x{wave.GridHeight}", GUILayout.Width(40));

            if (GUILayout.Button("Edit", GUILayout.Width(35)))
            {
                LoadWaveForEditing(wave);
            }

            if (GUILayout.Button("Load", GUILayout.Width(35)))
            {
                LoadWaveToManager(wave);
            }

            if (GUILayout.Button("Copy", GUILayout.Width(35)))
            {
                CopyWave(wave);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                DeleteWaveAsset(wave);
            }

            GUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndScrollView();

        // Asset management
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh")) needsWaveRefresh = true;
        if (GUILayout.Button("Open Folder")) OpenWavesFolder();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawWavePropertiesEditor()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Properties:");

        // Timing controls
        GUILayout.BeginHorizontal();
        GUILayout.Label("Move:", GUILayout.Width(40));
        string intervalStr = GUILayout.TextField(currentEditingWave.moveInterval.ToString("F1"), GUILayout.Width(40));
        if (float.TryParse(intervalStr, out float newInterval))
            currentEditingWave.moveInterval = Mathf.Max(0.1f, newInterval);

        GUILayout.Label("Fast:", GUILayout.Width(30));
        string fastStr = GUILayout.TextField(currentEditingWave.fastMoveInterval.ToString("F1"), GUILayout.Width(40));
        if (float.TryParse(fastStr, out float newFast))
            currentEditingWave.fastMoveInterval = Mathf.Max(0.05f, newFast);

        GUILayout.Label("Delay:", GUILayout.Width(35));
        string delayStr = GUILayout.TextField(currentEditingWave.waveStartDelay.ToString("F1"), GUILayout.Width(40));
        if (float.TryParse(delayStr, out float newDelay))
            currentEditingWave.waveStartDelay = Mathf.Max(0f, newDelay);
        GUILayout.EndHorizontal();

        // Marker limits
        GUILayout.BeginHorizontal();
        currentEditingWave.limitMarkers = GUILayout.Toggle(currentEditingWave.limitMarkers, "Limit Markers");
        if (currentEditingWave.limitMarkers)
        {
            GUILayout.Label("Max:", GUILayout.Width(30));
            string maxStr = GUILayout.TextField(currentEditingWave.maxMarkerCount.ToString(), GUILayout.Width(30));
            if (int.TryParse(maxStr, out int newMax))
                currentEditingWave.maxMarkerCount = Mathf.Max(1, newMax);

            GUILayout.Label("Charge:", GUILayout.Width(45));
            string chargeStr = GUILayout.TextField(currentEditingWave.maxMarkerCharge.ToString(), GUILayout.Width(30));
            if (int.TryParse(chargeStr, out int newCharge))
                currentEditingWave.maxMarkerCharge = Mathf.Max(1, newCharge);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawCubeGrid()
    {
        if (currentEditingWave == null) return;

        GUILayout.Label("Grid Editor:");

        // Show either current wave cubes or active cubes from manager
        var cubesToShow = GetDisplayCubes();

        // Grid representation (top to bottom)
        for (int y = currentEditingWave.GridHeight - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{y}:", GUILayout.Width(15));

            for (int x = 0; x < currentEditingWave.GridWidth; x++)
            {
                var cubeAtPos = cubesToShow.FirstOrDefault(c => c.position.x == x && c.position.y == y);

                Color buttonColor = Color.white;
                string buttonText = "·";

                if (cubeAtPos != null)
                {
                    switch (cubeAtPos.type)
                    {
                        case CubeType.Normal: buttonColor = Color.gray; buttonText = "N"; break;
                        case CubeType.Blue: buttonColor = Color.blue; buttonText = "B"; break;
                        case CubeType.Black: buttonColor = Color.black; buttonText = "X"; break;
                        case CubeType.Reinforced: buttonColor = Color.magenta; buttonText = "R"; break;
                    }
                }

                GUI.backgroundColor = buttonColor;
                if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (isPlacementMode)
                    {
                        HandleGridClick(x, y);
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }
    }

    private void DrawCubeList()
    {
        GUILayout.Label($"Cubes ({currentEditingWave.CubesData.Count}):");

        cubeListScroll = GUILayout.BeginScrollView(cubeListScroll, GUILayout.Height(100));

        for (int i = currentEditingWave.CubesData.Count - 1; i >= 0; i--)
        {
            var cube = currentEditingWave.CubesData[i];
            GUILayout.BeginHorizontal();

            // Type indicator
            GUI.backgroundColor = GetCubeColor(cube.type);
            GUILayout.Label(GetCubeSymbol(cube.type), GUILayout.Width(20));
            GUI.backgroundColor = Color.white;

            GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y})", GUILayout.Width(120));

            // Quick type change buttons
            if (GUILayout.Button("N", GUILayout.Width(20))) cube.type = CubeType.Normal;
            if (GUILayout.Button("B", GUILayout.Width(20))) cube.type = CubeType.Blue;
            if (GUILayout.Button("X", GUILayout.Width(20))) cube.type = CubeType.Black;

            if (GUILayout.Button("Del", GUILayout.Width(30)))
            {
                currentEditingWave.CubesData.RemoveAt(i);
                if (autoSyncToGrid) SyncWaveToGrid();
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // Core functionality methods
    private void HandleGridClick(int x, int y)
    {
        var existingCube = currentEditingWave.CubesData.FirstOrDefault(c => c.position.x == x && c.position.y == y);

        if (existingCube != null)
        {
            // Remove existing cube
            currentEditingWave.CubesData.Remove(existingCube);
        }
        else
        {
            // Add new cube
            var newCube = new CubeData
            {
                type = (CubeType)selectedCubeType,
                position = new Vector2Int(x, y),
                level = 1
            };
            currentEditingWave.CubesData.Add(newCube);
        }

        if (autoSyncToGrid) SyncWaveToGrid();
    }

    private void CreateNewWave()
    {
        currentEditingWave = ScriptableObject.CreateInstance<WaveData>();
        currentEditingWave.name = $"NewWave_{System.DateTime.Now:HHmmss}";
        currentEditingWave.GridWidth = Mathf.Min(5, gridManager.Width);
        currentEditingWave.GridHeight = 3;
        currentEditingWave.moveInterval = 1.5f;
        currentEditingWave.fastMoveInterval = 0.1f;
        currentEditingWave.waveStartDelay = 0.75f;
        currentEditingWave.CubesData = new List<CubeData>();
        currentEditingWave.limitMarkers = false;
        currentEditingWave.maxMarkerCount = 3;
        currentEditingWave.maxMarkerCharge = 2;
    }

    private void LoadCurrentWaveForEditing()
    {
        if (waveManager?.CurrentWave != null)
        {
            LoadWaveForEditing(waveManager.CurrentWave);
        }
    }

    private void LoadWaveForEditing(WaveData wave)
    {
        if (wave == null) return;

        currentEditingWave = Object.Instantiate(wave);
        if (autoSyncToGrid) SyncWaveToGrid();
    }

    private void SyncWaveToGrid()
    {
        if (currentEditingWave == null || waveManager == null) return;

        // Clear current cubes
        waveManager.ClearAllCubes();

        // Spawn cubes from wave configuration
        foreach (var cubeData in currentEditingWave.CubesData)
        {
            SpawnCubeFromData(cubeData);
        }
    }

    private void SpawnCubeFromData(CubeData cubeData)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeData.type >= waveManager.cubePrefabs.Length) return;
        if (!gridManager.IsValidGridPosition(cubeData.position)) return;

        Vector3 worldPos = gridManager.GridToWorldPosition(cubeData.position.x, cubeData.position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeData.type], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        cube.Init(gridManager, cubeData, 2f);
        waveManager.activeCubes.Add(cube);
    }

    private void SaveCurrentWave()
    {
        if (currentEditingWave == null) return;

        string wavesPath = "Assets/data/waves";

#if UNITY_EDITOR
        // Ensure directory exists
        if (!UnityEditor.AssetDatabase.IsValidFolder(wavesPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/data", "waves");
        }

        string assetPath = $"{wavesPath}/{currentEditingWave.name}.asset";

        // Check if asset already exists
        var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);
        if (existingAsset != null)
        {
            // Update existing asset
            UnityEditor.EditorUtility.CopySerialized(currentEditingWave, existingAsset);
            UnityEditor.EditorUtility.SetDirty(existingAsset);
        }
        else
        {
            // Create new asset
            UnityEditor.AssetDatabase.CreateAsset(currentEditingWave, assetPath);
        }

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Wave '{currentEditingWave.name}' saved to {assetPath}");
        needsWaveRefresh = true;
#endif
    }

    private void RefreshAvailableWaves()
    {
        availableWaves.Clear();

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/data/waves" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave != null)
            {
                availableWaves.Add(wave);
            }
        }
#endif

        availableWaves.Sort((a, b) => string.Compare(a.name, b.name));
    }

    private void DeleteWaveAsset(WaveData wave)
    {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(wave);
        if (!string.IsNullOrEmpty(path))
        {
            UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.Refresh();
            needsWaveRefresh = true;
            if (currentEditingWave == wave)
            {
                currentEditingWave = null;
            }
        }
#endif
    }

    private void OpenWavesFolder()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder("Assets/data/waves");
#endif
    }

    private void LoadWaveToManager(WaveData wave)
    {
        if (waveManager == null || wave == null) return;

        // Add to wave manager configuration if not already there
        if (!waveManager.waveConfiguration.Contains(wave))
        {
            waveManager.waveConfiguration.Add(wave);
        }

        // Set as current wave
        waveManager.currentWaveIndex = waveManager.waveConfiguration.IndexOf(wave);
        LoadWaveForEditing(wave);
        SyncWaveToGrid();
    }

    private void TestCurrentWave()
    {
        if (currentEditingWave == null || waveManager == null) return;

        LoadWaveToManager(currentEditingWave);
        waveManager.StartWave();
    }

    private void CopyWave(WaveData original)
    {
        currentEditingWave = Object.Instantiate(original);
        currentEditingWave.name = original.name + "_Copy";
    }

    private void FillWaveRandom()
    {
        if (currentEditingWave == null) return;

        currentEditingWave.CubesData.Clear();

        for (int x = 0; x < currentEditingWave.GridWidth; x++)
        {
            for (int y = 0; y < currentEditingWave.GridHeight; y++)
            {
                if (Random.value < 0.6f) // 60% chance to place a cube
                {
                    var cubeData = new CubeData
                    {
                        type = (CubeType)Random.Range(0, 3), // Normal, Blue, Black only
                        position = new Vector2Int(x, y),
                        level = 1
                    };
                    currentEditingWave.CubesData.Add(cubeData);
                }
            }
        }

        if (autoSyncToGrid) SyncWaveToGrid();
    }

    private void FillTopRow()
    {
        if (currentEditingWave == null) return;

        int topRow = currentEditingWave.GridHeight - 1;

        // Remove existing cubes in top row
        currentEditingWave.CubesData.RemoveAll(c => c.position.y == topRow);

        // Add cubes across top row
        for (int x = 0; x < currentEditingWave.GridWidth; x++)
        {
            var cubeData = new CubeData
            {
                type = (CubeType)selectedCubeType,
                position = new Vector2Int(x, topRow),
                level = 1
            };
            currentEditingWave.CubesData.Add(cubeData);
        }

        if (autoSyncToGrid) SyncWaveToGrid();
    }

    private void ClampCubesToWaveBounds()
    {
        if (currentEditingWave == null) return;

        currentEditingWave.CubesData.RemoveAll(c =>
            c.position.x >= currentEditingWave.GridWidth ||
            c.position.y >= currentEditingWave.GridHeight);
    }

    private void StepWaveBackward()
    {
        // This is complex - would need to save wave states or reconstruct previous state
        Debug.Log("Step backward not implemented - would require wave state history");
    }

    private List<CubeData> GetDisplayCubes()
    {
        // Show wave cubes if editing, or current active cubes if synced
        if (currentEditingWave != null)
        {
            return currentEditingWave.CubesData;
        }

        // Fallback to showing active cubes from manager
        var activeCubes = new List<CubeData>();
        if (waveManager != null)
        {
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube != null && !cube.isDestroyed)
                {
                    activeCubes.Add(new CubeData
                    {
                        type = cube.type,
                        position = cube.position,
                        level = cube.level
                    });
                }
            }
        }
        return activeCubes;
    }

    private Color GetCubeColor(CubeType type)
    {
        switch (type)
        {
            case CubeType.Normal: return Color.gray;
            case CubeType.Blue: return Color.blue;
            case CubeType.Black: return Color.black;
            case CubeType.Reinforced: return Color.magenta;
            default: return Color.white;
        }
    }

    private string GetCubeSymbol(CubeType type)
    {
        switch (type)
        {
            case CubeType.Normal: return "N";
            case CubeType.Blue: return "B";
            case CubeType.Black: return "X";
            case CubeType.Reinforced: return "R";
            default: return "?";
        }
    }
}