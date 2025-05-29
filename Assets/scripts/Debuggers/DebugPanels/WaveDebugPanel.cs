using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static Enumerations;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveDebugPanel : IDebugPanel
{
    public string PanelName => "Wave Editor";

    private WaveManager waveManager;
    private GridManager gridManager;
    private StageManager stageManager;

    // UI State
    private Vector2 scrollPosition;
    private bool showCurrentWave = true;
    private bool showWaveList = true;
    private bool showWaveEditor = false;
    private bool showCubeEditor = false;
    private bool showLifecycleControls = true;
    private bool showMessages = false;

    // Editor State
    private WaveData editingWave = null;
    private bool isCreatingNewWave = false;
    private string searchFilter = "";
    private int selectedWaveIndex = -1;

    // Wave Editor Fields
    private string waveName = "";
    private int waveIndex = 0;
    private int gridWidth = 3;
    private int gridHeight = 3;
    private bool limitMarkers = false;
    private int maxMarkerCharge = 2;
    private int maxMarkerCount = 99;
    private float waveStartDelay = 0.75f;
    private float moveInterval = 0.75f;
    private float fastMoveInterval = 0.1f;
    private bool hasOwnSuccessCriteria = false;
    private int requiredCaptureCount = 0;
    private int maxAllowedEscapes = 0;

    // Cube Editor State
    private int[,] cubeGrid;
    private int selectedCubeType = 1; // 1=Normal, 2=Blue, 3=Black, 0=Empty
    private Vector2 cubeGridScrollPosition;
    private bool showCubeStats = true;

    // Messages Editor
    private List<WaveMessage> editingMessages = new List<WaveMessage>();
    private int selectedMessageIndex = -1;
    private bool showMessageEditor = false;

    // Lifecycle Control State
    private bool debugModeEnabled = false;
    private bool manualControlEnabled = false;
    private float currentSpeedMultiplier = 1f;
    private bool overrideMessages = false;
    private bool showLiveCubeGrid = false;

    // Live cube tracking
    private Dictionary<Vector2Int, CubeBehavior> liveCubeMap = new Dictionary<Vector2Int, CubeBehavior>();

    public void Initialize()
    {
        waveManager = Object.FindObjectOfType<WaveManager>();
        gridManager = Object.FindObjectOfType<GridManager>();
        stageManager = Object.FindObjectOfType<StageManager>();

        InitializeCubeGrid();
    }

    public void Update()
    {
        // Update state tracking
        if (waveManager != null)
        {
            debugModeEnabled = waveManager.debugMode;
            manualControlEnabled = waveManager.manualControl;

            // Update live cube tracking
            UpdateLiveCubeTracking();
        }
    }

    private void UpdateLiveCubeTracking()
    {
        if (!showLiveCubeGrid || waveManager == null) return;

        liveCubeMap.Clear();
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                liveCubeMap[cube.position] = cube;
            }
        }
    }

    public void DrawPanel()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawPanelTabs();
        GUILayout.Space(5);

        if (showCurrentWave)
            DrawCurrentWaveSection();

        if (showLifecycleControls)
            DrawLifecycleSection();

        if (showWaveList)
            DrawWaveListSection();

        if (showWaveEditor)
            DrawWaveEditorSection();

        if (showCubeEditor)
            DrawCubeEditorSection();

        if (showMessages)
            DrawMessagesSection();

        GUILayout.EndScrollView();
    }

    private void DrawPanelTabs()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = showCurrentWave ? Color.cyan : Color.white;
        if (GUILayout.Button("Current", GUILayout.Height(25)))
            showCurrentWave = !showCurrentWave;

        GUI.backgroundColor = showLifecycleControls ? Color.cyan : Color.white;
        if (GUILayout.Button("Controls", GUILayout.Height(25)))
            showLifecycleControls = !showLifecycleControls;

        GUI.backgroundColor = showWaveList ? Color.cyan : Color.white;
        if (GUILayout.Button("List", GUILayout.Height(25)))
            showWaveList = !showWaveList;

        GUI.backgroundColor = showWaveEditor ? Color.cyan : Color.white;
        if (GUILayout.Button("Editor", GUILayout.Height(25)))
            showWaveEditor = !showWaveEditor;

        GUI.backgroundColor = showCubeEditor ? Color.cyan : Color.white;
        if (GUILayout.Button("Cubes", GUILayout.Height(25)))
            showCubeEditor = !showCubeEditor;

        GUI.backgroundColor = showMessages ? Color.cyan : Color.white;
        if (GUILayout.Button("Messages", GUILayout.Height(25)))
            showMessages = !showMessages;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawCurrentWaveSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CURRENT WAVE", GUI.skin.box);

        if (waveManager?.CurrentWave != null)
        {
            var wave = waveManager.CurrentWave;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Name: {wave.name}");
            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                LoadWaveForEditing(wave);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Index: {wave.Index}");
            GUILayout.Label($"Size: {wave.GridWidth}x{wave.GridHeight}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Cubes: {wave.CubesData.Count}");
            GUILayout.Label($"Messages: {wave.messages.Count}");
            GUILayout.EndHorizontal();

            // Cube breakdown
            var cubeStats = GetCubeStats(wave.CubesData);
            GUILayout.Label($"Normal: {cubeStats.normal}, Blue: {cubeStats.blue}, Black: {cubeStats.black}");

            // Timing settings
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Start Delay: {wave.waveStartDelay:F2}s");
            GUILayout.Label($"Move Interval: {wave.moveInterval:F2}s");
            GUILayout.EndHorizontal();

            // Active state
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Wave Active: {waveManager.waveActive}");
            GUILayout.Label($"Move Step: {waveManager.MoveStep}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Active Cubes: {waveManager.activeCubes.Count}");
            GUILayout.Label($"Speed Up: {waveManager.isSpeedingUp}");
            GUILayout.EndHorizontal();

            // Success criteria
            if (wave.hasOwnSuccessCriteria)
            {
                GUILayout.Label("Success Criteria:");
                if (wave.requiredCaptureCount > 0)
                    GUILayout.Label($"  • Capture {wave.requiredCaptureCount} cubes");
                if (wave.maxAllowedEscapes >= 0)
                    GUILayout.Label($"  • Max {wave.maxAllowedEscapes} escapes allowed");
            }

            // Wave statistics
            GUILayout.Label("Wave Statistics:");
            GUILayout.Label($"  Normal Captured: {wave.normalCubesCaptured}");
            GUILayout.Label($"  Blue Captured: {wave.blueCubesCaptured}");
            GUILayout.Label($"  Cubes Escaped: {wave.cubesEscaped}");
            GUILayout.Label($"  Markers Placed: {wave.markersPlaced}");
            GUILayout.Label($"  Detonations Used: {wave.detonationsUsed}");
        }
        else
        {
            GUILayout.Label("No wave loaded");
            if (GUILayout.Button("Start Random Wave"))
            {
                waveManager?.StartWave();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawLifecycleSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE LIFECYCLE CONTROLS", GUI.skin.box);

        // Unified reset and control
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Full Reset"))
        {
            FullReset();
        }
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Start"))
            waveManager?.StartWave();
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Pause"))
            waveManager?.PauseWave();
        GUI.backgroundColor = Color.blue;
        if (GUILayout.Button("Resume"))
            waveManager?.ResumeWave();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Stop"))
            waveManager?.StopWave();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Manual control
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Manual Step"))
            waveManager?.ManualMoveWaveForward();
        if (GUILayout.Button("Clear All Cubes"))
            waveManager?.ClearAllCubes();
        GUILayout.EndHorizontal();

        // Individual cube movement
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Move All Forward"))
            MoveAllCubesForward();
        if (GUILayout.Button("Move All Backward"))
            MoveAllCubesBackward();
        GUILayout.EndHorizontal();

        // Debug mode controls
        GUILayout.BeginHorizontal();
        GUILayout.Label("Debug Mode:");
        bool newDebugMode = GUILayout.Toggle(debugModeEnabled, "");
        if (newDebugMode != debugModeEnabled)
        {
            if (newDebugMode)
                waveManager?.EnterDebugMode(true);
            else
                waveManager?.ExitDebugMode();
        }
        GUILayout.EndHorizontal();

        // Message override
        GUILayout.BeginHorizontal();
        bool newOverrideMessages = GUILayout.Toggle(overrideMessages, "Override Messages (Hide All)");
        if (newOverrideMessages != overrideMessages)
        {
            overrideMessages = newOverrideMessages;
            // Show reload button when state changes
        }

        // Show reload button if message override state changed
        if (overrideMessages != GetOriginalMessageState())
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reload Wave"))
            {
                ReloadCurrentWave();
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Manual Control: {manualControlEnabled}");
        GUILayout.EndHorizontal();

        // Speed controls
        GUILayout.Label("Speed Controls:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.25x")) SetWaveSpeed(2f);
        if (GUILayout.Button("0.5x")) SetWaveSpeed(1.5f);
        if (GUILayout.Button("1x")) SetWaveSpeed(0.75f);
        if (GUILayout.Button("2x")) SetWaveSpeed(0.375f);
        if (GUILayout.Button("4x")) SetWaveSpeed(0.1875f);
        GUILayout.EndHorizontal();

        // Current speed display
        if (waveManager != null)
        {
            GUILayout.Label($"Normal Speed: {waveManager.normalMoveInterval:F2}s");
            GUILayout.Label($"Fast Speed: {waveManager.fastMoveInterval:F2}s");
        }

        GUILayout.EndVertical();
    }

    private void DrawWaveListSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE DATABASE", GUI.skin.box);

        // Search filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = GUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
            searchFilter = "";
        GUILayout.EndHorizontal();

        // Create new wave button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("+ Create New Wave", GUILayout.Height(30)))
        {
            StartCreatingNewWave();
        }
        GUI.backgroundColor = Color.white;

        // Load waves from project
#if UNITY_EDITOR
        string[] waveGuids = AssetDatabase.FindAssets("t:WaveData");

        if (waveGuids.Length > 0)
        {
            GUILayout.Label($"Available Waves: {waveGuids.Length}");

            foreach (string guid in waveGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);

                if (wave != null)
                {
                    // Apply search filter
                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        string searchLower = searchFilter.ToLower();
                        if (!wave.name.ToLower().Contains(searchLower) &&
                            !wave.Index.ToString().Contains(searchFilter))
                        {
                            continue;
                        }
                    }

                    DrawWaveListItem(wave, path);
                }
            }
        }
        else
        {
            GUILayout.Label("No wave assets found");
        }
#else
        GUILayout.Label("Wave list only available in editor mode");
#endif

        GUILayout.EndVertical();
    }

    private void DrawWaveListItem(WaveData wave, string assetPath)
    {
        bool isCurrent = waveManager?.CurrentWave == wave;
        bool isSelected = selectedWaveIndex == wave.Index;

        GUI.backgroundColor = isCurrent ? Color.yellow : (isSelected ? Color.cyan : Color.white);

        GUILayout.BeginVertical(GUI.skin.box);

        // Wave header
        GUILayout.BeginHorizontal();
        string statusText = isCurrent ? " (CURRENT)" : "";
        GUILayout.Label($"[{wave.Index}] {wave.name}{statusText}", GUI.skin.box);
        GUILayout.EndHorizontal();

        // Wave details
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Size: {wave.GridWidth}x{wave.GridHeight}");
        GUILayout.Label($"Cubes: {wave.CubesData.Count}");
        GUILayout.Label($"Messages: {wave.messages.Count}");
        GUILayout.EndHorizontal();

        var stats = GetCubeStats(wave.CubesData);
        GUILayout.Label($"Normal: {stats.normal}, Blue: {stats.blue}, Black: {stats.black}");

        // Action buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Width(50)))
        {
            LoadWaveForTesting(wave);
            selectedWaveIndex = wave.Index;
        }
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            LoadWaveForEditing(wave);
            selectedWaveIndex = wave.Index;
        }
        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            CreateWaveFromTemplate(wave);
        }
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("Delete Wave",
                $"Are you sure you want to delete {wave.name}?", "Delete", "Cancel"))
            {
                DeleteWave(assetPath);
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void DrawWaveEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);

        if (editingWave != null || isCreatingNewWave)
        {
            string title = isCreatingNewWave ? "CREATE NEW WAVE" : $"EDIT WAVE: {editingWave?.name}";
            GUILayout.Label(title, GUI.skin.box);

            DrawWaveEditorFields();
            DrawWaveEditorActions();
        }
        else
        {
            GUILayout.Label("WAVE EDITOR", GUI.skin.box);
            GUILayout.Label("Select a wave to edit or create a new one");

            if (GUILayout.Button("Create New Wave"))
            {
                StartCreatingNewWave();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawWaveEditorFields()
    {
        // Basic Info
        GUILayout.Label("Basic Information:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Wave Index:", GUILayout.Width(100));
        string indexStr = GUILayout.TextField(waveIndex.ToString(), GUILayout.Width(60));
        if (int.TryParse(indexStr, out int newIndex))
            waveIndex = newIndex;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Name:", GUILayout.Width(100));
        waveName = GUILayout.TextField(waveName);
        GUILayout.EndHorizontal();

        // Grid Configuration
        GUILayout.Label("Grid Configuration:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid Size:", GUILayout.Width(100));
        string widthStr = GUILayout.TextField(gridWidth.ToString(), GUILayout.Width(40));
        GUILayout.Label("x", GUILayout.Width(15));
        string heightStr = GUILayout.TextField(gridHeight.ToString(), GUILayout.Width(40));
        if (int.TryParse(widthStr, out int newWidth))
        {
            gridWidth = Mathf.Clamp(newWidth, 1, 12);
            ResizeCubeGrid();
        }
        if (int.TryParse(heightStr, out int newHeight))
        {
            gridHeight = Mathf.Clamp(newHeight, 1, 15);
            ResizeCubeGrid();
        }
        GUILayout.EndHorizontal();

        // Wave Settings
        GUILayout.Label("Wave Settings:", GUI.skin.box);

        limitMarkers = GUILayout.Toggle(limitMarkers, "Limit Markers");
        if (limitMarkers)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Charge:", GUILayout.Width(80));
            string chargeStr = GUILayout.TextField(maxMarkerCharge.ToString(), GUILayout.Width(40));
            if (int.TryParse(chargeStr, out int newCharge))
                maxMarkerCharge = Mathf.Clamp(newCharge, 1, 10);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Count:", GUILayout.Width(80));
            string countStr = GUILayout.TextField(maxMarkerCount.ToString(), GUILayout.Width(40));
            if (int.TryParse(countStr, out int newCount))
                maxMarkerCount = Mathf.Clamp(newCount, 1, 999);
            GUILayout.EndHorizontal();
        }

        // Timing Settings
        GUILayout.Label("Timing Settings:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Start Delay:", GUILayout.Width(80));
        string delayStr = GUILayout.TextField(waveStartDelay.ToString("F2"), GUILayout.Width(60));
        if (float.TryParse(delayStr, out float newDelay))
            waveStartDelay = Mathf.Clamp(newDelay, 0f, 10f);
        GUILayout.Label("s");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Move Interval:", GUILayout.Width(80));
        string intervalStr = GUILayout.TextField(moveInterval.ToString("F2"), GUILayout.Width(60));
        if (float.TryParse(intervalStr, out float newInterval))
            moveInterval = Mathf.Clamp(newInterval, 0.1f, 5f);
        GUILayout.Label("s");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Fast Interval:", GUILayout.Width(80));
        string fastStr = GUILayout.TextField(fastMoveInterval.ToString("F2"), GUILayout.Width(60));
        if (float.TryParse(fastStr, out float newFast))
            fastMoveInterval = Mathf.Clamp(newFast, 0.05f, 1f);
        GUILayout.Label("s");
        GUILayout.EndHorizontal();

        // Success Criteria
        GUILayout.Label("Success Criteria:", GUI.skin.box);

        hasOwnSuccessCriteria = GUILayout.Toggle(hasOwnSuccessCriteria, "Has Own Success Criteria");
        if (hasOwnSuccessCriteria)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Required Captures:", GUILayout.Width(120));
            string captureStr = GUILayout.TextField(requiredCaptureCount.ToString(), GUILayout.Width(60));
            if (int.TryParse(captureStr, out int newCaptures))
                requiredCaptureCount = Mathf.Max(0, newCaptures);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Escapes:", GUILayout.Width(120));
            string escapeStr = GUILayout.TextField(maxAllowedEscapes.ToString(), GUILayout.Width(60));
            if (int.TryParse(escapeStr, out int newEscapes))
                maxAllowedEscapes = Mathf.Max(-1, newEscapes);
            GUILayout.Label("(-1 = unlimited)");
            GUILayout.EndHorizontal();
        }
    }

    private void DrawWaveEditorActions()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save Wave", GUILayout.Height(30)))
        {
            SaveCurrentWave();
        }
        GUI.backgroundColor = Color.blue;
        if (GUILayout.Button("Save & Test", GUILayout.Height(30)))
        {
            SaveCurrentWave();
            TestCurrentWave();
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            CancelEditing();
        }
        GUILayout.EndHorizontal();

        if (!isCreatingNewWave && editingWave != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Original"))
            {
                LoadWaveForEditing(editingWave); // Reload original values
            }
            if (GUILayout.Button("Duplicate Wave"))
            {
                CreateWaveFromTemplate(editingWave);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawCubeEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE EDITOR", GUI.skin.box);

        // Mode selection
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = !showLiveCubeGrid ? Color.cyan : Color.white;
        if (GUILayout.Button("Design Mode"))
        {
            showLiveCubeGrid = false;
        }
        GUI.backgroundColor = showLiveCubeGrid ? Color.cyan : Color.white;
        if (GUILayout.Button("Live Mode"))
        {
            showLiveCubeGrid = true;
            if (!debugModeEnabled && waveManager != null)
            {
                waveManager.EnterDebugMode(true);
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (showLiveCubeGrid)
        {
            DrawLiveCubeEditor();
        }
        else if (editingWave != null || isCreatingNewWave)
        {
            DrawDesignCubeEditor();
        }
        else
        {
            GUILayout.Label("Select a wave to edit cubes or use Live Mode");
        }

        GUILayout.EndVertical();
    }

    private void DrawDesignCubeEditor()
    {
        GUILayout.Label("Design Mode - Edit wave template");

        DrawCubeTypeSelector();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load to Grid"))
        {
            LoadDesignToGrid();
        }
        if (GUILayout.Button("Spawn from Design"))
        {
            SpawnFromDesign();
        }
        GUILayout.EndHorizontal();

        DrawCubeGrid();
        if (showCubeStats)
            DrawCubeStats();
        DrawCubeEditorActions();
    }

    private void DrawLiveCubeEditor()
    {
        GUILayout.Label("Live Mode - Active cubes on grid");

        if (waveManager == null || waveManager.activeCubes.Count == 0)
        {
            GUILayout.Label("No active cubes. Start a wave to see live cubes.");
            return;
        }

        GUILayout.Label($"Active Cubes: {waveManager.activeCubes.Count}");

        // Live cube controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            // Could implement cube selection
        }
        if (GUILayout.Button("Clear Selection"))
        {
            // Could implement cube selection
        }
        GUILayout.EndHorizontal();

        DrawLiveCubeGrid();
        DrawLiveCubeStats();
    }

    private void DrawLiveCubeGrid()
    {
        if (gridManager == null) return;

        int gridW = gridManager.Width;
        int gridH = gridManager.Height;

        GUILayout.Label($"Live Grid ({gridW}x{gridH}):");

        cubeGridScrollPosition = GUILayout.BeginScrollView(cubeGridScrollPosition, GUILayout.Height(250));

        // Show a subset of the grid (top portion where cubes typically are)
        int displayHeight = Mathf.Min(12, gridH);
        int startY = gridH - displayHeight;

        for (int y = gridH - 1; y >= startY; y--) // Draw top to bottom
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{y}", GUILayout.Width(20)); // Row label

            for (int x = 0; x < gridW; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                bool hasCube = liveCubeMap.ContainsKey(pos);

                if (hasCube)
                {
                    var cube = liveCubeMap[pos];
                    SetCubeButtonColor((int)cube.type + 1);
                    string buttonText = GetCubeButtonText((int)cube.type + 1);

                    if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        // Individual cube controls
                        ShowCubeContextMenu(cube);
                    }
                }
                else
                {
                    GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    GUILayout.Button("·", GUILayout.Width(25), GUILayout.Height(25));
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndScrollView();
    }

    private void ShowCubeContextMenu(CubeBehavior cube)
    {
        // For now, just log the cube info and provide basic actions
        Debug.Log($"Cube {cube.type} at ({cube.position.x}, {cube.position.y})");

        // Could expand this to show a popup menu with:
        // - Move Forward
        // - Move Backward  
        // - Change Type
        // - Destroy
    }

    private void DrawLiveCubeStats()
    {
        var cubesByType = new Dictionary<CubeType, int>();
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                if (!cubesByType.ContainsKey(cube.type))
                    cubesByType[cube.type] = 0;
                cubesByType[cube.type]++;
            }
        }

        GUILayout.Label("Live Cube Distribution:");
        foreach (var kvp in cubesByType)
        {
            GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
        }
    }

    private void DrawCubeTypeSelector()
    {
        GUILayout.Label("Cube Type Selector:");
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = selectedCubeType == 0 ? Color.gray : Color.white;
        if (GUILayout.Button("Empty"))
            selectedCubeType = 0;

        GUI.backgroundColor = selectedCubeType == 1 ? Color.gray : Color.white;
        if (GUILayout.Button("Normal"))
            selectedCubeType = 1;

        GUI.backgroundColor = selectedCubeType == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Blue"))
            selectedCubeType = 2;

        GUI.backgroundColor = selectedCubeType == 3 ? Color.black : Color.white;
        if (GUILayout.Button("Black"))
            selectedCubeType = 3;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Label("Click grid cells to place selected cube type");
    }

    private void DrawCubeGrid()
    {
        GUILayout.Label($"Cube Grid ({gridWidth}x{gridHeight}):");

        cubeGridScrollPosition = GUILayout.BeginScrollView(cubeGridScrollPosition, GUILayout.Height(200));

        for (int y = gridHeight - 1; y >= 0; y--) // Draw top to bottom
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridWidth; x++)
            {
                SetCubeButtonColor(cubeGrid[x, y]);
                string buttonText = GetCubeButtonText(cubeGrid[x, y]);

                if (GUILayout.Button(buttonText, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    cubeGrid[x, y] = selectedCubeType;
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndScrollView();
    }

    private void DrawCubeStats()
    {
        var stats = GetCubeStatsFromGrid();
        GUILayout.Label($"Cube Count - Normal: {stats.normal}, Blue: {stats.blue}, Black: {stats.black}, Total: {stats.total}");
    }

    private void DrawCubeEditorActions()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            ClearCubeGrid();
        }
        if (GUILayout.Button("Fill Normal"))
        {
            FillCubeGrid(1);
        }
        if (GUILayout.Button("Randomize"))
        {
            RandomizeCubeGrid();
        }
        GUILayout.EndHorizontal();

        showCubeStats = GUILayout.Toggle(showCubeStats, "Show Cube Statistics");
    }

    private void DrawMessagesSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE MESSAGES", GUI.skin.box);

        if (editingWave != null || isCreatingNewWave)
        {
            GUILayout.Label($"Messages: {editingMessages.Count}");

            // Add new message button
            if (GUILayout.Button("+ Add Message"))
            {
                AddNewMessage();
            }

            // List existing messages
            for (int i = 0; i < editingMessages.Count; i++)
            {
                DrawMessageListItem(i, editingMessages[i]);
            }

            // Message editor
            if (showMessageEditor && selectedMessageIndex >= 0 && selectedMessageIndex < editingMessages.Count)
            {
                DrawMessageEditor(editingMessages[selectedMessageIndex]);
            }
        }
        else
        {
            GUILayout.Label("Select a wave to edit messages");
        }

        GUILayout.EndVertical();
    }

    private void DrawMessageListItem(int index, WaveMessage message)
    {
        bool isSelected = selectedMessageIndex == index;
        GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"[{index}] Step {message.DisplayMoveStep}");
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            selectedMessageIndex = index;
            showMessageEditor = true;
        }
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            editingMessages.RemoveAt(index);
            if (selectedMessageIndex >= editingMessages.Count)
                selectedMessageIndex = -1;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label(message.Message.Length > 50 ? message.Message.Substring(0, 50) + "..." : message.Message);

        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void DrawMessageEditor(WaveMessage message)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("MESSAGE EDITOR", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Move Step:", GUILayout.Width(80));
        string stepStr = GUILayout.TextField(message.DisplayMoveStep.ToString(), GUILayout.Width(60));
        if (int.TryParse(stepStr, out int newStep))
            message.DisplayMoveStep = newStep;
        GUILayout.Label("(-1 = end of wave)");
        GUILayout.EndHorizontal();

        GUILayout.Label("Message Text:");
        message.Message = GUILayout.TextArea(message.Message, GUILayout.Height(60));

        message.RequirePause = GUILayout.Toggle(message.RequirePause, "Requires Pause");

        if (!message.RequirePause)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Hide Delay:", GUILayout.Width(100));
            string delayStr = GUILayout.TextField(message.AutoHideDelay.ToString("F1"), GUILayout.Width(60));
            if (float.TryParse(delayStr, out float newDelay))
                message.AutoHideDelay = Mathf.Max(0f, newDelay);
            GUILayout.Label("seconds");
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close Editor"))
        {
            showMessageEditor = false;
            selectedMessageIndex = -1;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // Helper Methods

    private void InitializeCubeGrid()
    {
        cubeGrid = new int[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cubeGrid[x, y] = 0; // Empty by default
            }
        }
    }

    private void ResizeCubeGrid()
    {
        int[,] newGrid = new int[gridWidth, gridHeight];

        if (cubeGrid != null)
        {
            int oldWidth = cubeGrid.GetLength(0);
            int oldHeight = cubeGrid.GetLength(1);

            for (int x = 0; x < Mathf.Min(oldWidth, gridWidth); x++)
            {
                for (int y = 0; y < Mathf.Min(oldHeight, gridHeight); y++)
                {
                    newGrid[x, y] = cubeGrid[x, y];
                }
            }
        }

        cubeGrid = newGrid;
    }

    private void StartCreatingNewWave()
    {
        isCreatingNewWave = true;
        editingWave = null;

        // Set default values
        waveIndex = GetNextAvailableWaveIndex();
        waveName = $"Wave_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        gridWidth = 3;
        gridHeight = 3;
        limitMarkers = false;
        maxMarkerCharge = 2;
        maxMarkerCount = 99;
        waveStartDelay = 0.75f;
        moveInterval = 0.75f;
        fastMoveInterval = 0.1f;
        hasOwnSuccessCriteria = false;
        requiredCaptureCount = 0;
        maxAllowedEscapes = 0;
        editingMessages = new List<WaveMessage>();

        InitializeCubeGrid();
        showWaveEditor = true;
        showCubeEditor = true;
    }

    private void LoadWaveForEditing(WaveData wave)
    {
        editingWave = wave;
        isCreatingNewWave = false;

        // Load values from wave
        waveIndex = wave.Index;
        waveName = wave.name;
        gridWidth = wave.GridWidth;
        gridHeight = wave.GridHeight;
        limitMarkers = wave.limitMarkers;
        maxMarkerCharge = wave.maxMarkerCharge;
        maxMarkerCount = wave.maxMarkerCount;
        waveStartDelay = wave.waveStartDelay;
        moveInterval = wave.moveInterval;
        fastMoveInterval = wave.fastMoveInterval;
        hasOwnSuccessCriteria = wave.hasOwnSuccessCriteria;
        requiredCaptureCount = wave.requiredCaptureCount;
        maxAllowedEscapes = wave.maxAllowedEscapes;
        editingMessages = new List<WaveMessage>(overrideMessages ? null :  wave.messages);

        // Load cube data into grid
        InitializeCubeGrid();
        foreach (var cubeData in wave.CubesData)
        {
            if (cubeData.position.x >= 0 && cubeData.position.x < gridWidth &&
                cubeData.position.y >= 0 && cubeData.position.y < gridHeight)
            {
                cubeGrid[cubeData.position.x, cubeData.position.y] = (int)cubeData.type + 1;
            }
        }

        showWaveEditor = true;
        showCubeEditor = true;
    }

    private void LoadWaveForTesting(WaveData wave)
    {
        if (waveManager != null)
        {
            // Create a copy of the wave for testing
            WaveData testWave = ScriptableObject.CreateInstance<WaveData>();

            // Copy all properties
            testWave.Index = wave.Index;
            testWave.name = wave.name;
            testWave.GridWidth = wave.GridWidth;
            testWave.GridHeight = wave.GridHeight;
            testWave.limitMarkers = wave.limitMarkers;
            testWave.maxMarkerCharge = wave.maxMarkerCharge;
            testWave.maxMarkerCount = wave.maxMarkerCount;
            testWave.waveStartDelay = wave.waveStartDelay;
            testWave.moveInterval = wave.moveInterval;
            testWave.fastMoveInterval = wave.fastMoveInterval;
            testWave.hasOwnSuccessCriteria = wave.hasOwnSuccessCriteria;
            testWave.requiredCaptureCount = wave.requiredCaptureCount;
            testWave.maxAllowedEscapes = wave.maxAllowedEscapes;

            // Apply message override
            if (overrideMessages)
            {
                testWave.messages = new List<WaveMessage>(); // Empty list
            }
            else
            {
                testWave.messages = new List<WaveMessage>(wave.messages);
            }

            // Copy cube data and position at top of grid
            testWave.CubesData = new List<CubeData>();
            int spawnY = gridManager.Height - testWave.GridHeight;

            foreach (var cubeData in wave.CubesData)
            {
                testWave.CubesData.Add(new CubeData
                {
                    type = cubeData.type,
                    position = new Vector2Int(cubeData.position.x, spawnY + cubeData.position.y),
                    level = cubeData.level
                });
            }

            waveManager.waveConfiguration = new List<WaveData> { testWave };
            waveManager.useWaveConfiguration = true;
            waveManager.currentWaveIndex = 0;
        }
    }

    private void CreateWaveFromTemplate(WaveData templateWave)
    {
        LoadWaveForEditing(templateWave);
        isCreatingNewWave = true;
        editingWave = null;

        waveName = templateWave.name + "_Copy";
        waveIndex = GetNextAvailableWaveIndex();
    }

    private int GetNextAvailableWaveIndex()
    {
#if UNITY_EDITOR
        string[] waveGuids = AssetDatabase.FindAssets("t:WaveData");
        int maxIndex = -1;

        foreach (string guid in waveGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave != null)
            {
                maxIndex = Mathf.Max(maxIndex, wave.Index);
            }
        }

        return maxIndex + 1;
#else
        return 0;
#endif
    }

    private void SaveCurrentWave()
    {
#if UNITY_EDITOR
        WaveData waveToSave;

        if (isCreatingNewWave)
        {
            waveToSave = ScriptableObject.CreateInstance<WaveData>();
        }
        else
        {
            waveToSave = editingWave;
        }

        // Apply all values
        waveToSave.Index = waveIndex;
        waveToSave.name = waveName;
        waveToSave.GridWidth = gridWidth;
        waveToSave.GridHeight = gridHeight;
        waveToSave.limitMarkers = limitMarkers;
        waveToSave.maxMarkerCharge = maxMarkerCharge;
        waveToSave.maxMarkerCount = maxMarkerCount;
        waveToSave.waveStartDelay = waveStartDelay;
        waveToSave.moveInterval = moveInterval;
        waveToSave.fastMoveInterval = fastMoveInterval;
        waveToSave.hasOwnSuccessCriteria = hasOwnSuccessCriteria;
        waveToSave.requiredCaptureCount = requiredCaptureCount;
        waveToSave.maxAllowedEscapes = maxAllowedEscapes;


        // Convert cube grid to CubeData list
        waveToSave.CubesData = new List<CubeData>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (cubeGrid[x, y] > 0) // Not empty
                {
                    var cubeData = new CubeData
                    {
                        type = (CubeType)(cubeGrid[x, y] - 1),
                        position = new Vector2Int(x, y),
                        level = 1
                    };
                    waveToSave.CubesData.Add(cubeData);
                }
            }
        }

        if (isCreatingNewWave)
        {
            // Create new asset
            string assetPath = $"Assets/data/waves/{waveName}.asset";
            AssetDatabase.CreateAsset(waveToSave, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(waveToSave);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Wave {waveName} saved successfully!");

        // Exit editing mode
        CancelEditing();
#else
        Debug.LogWarning("Wave saving only available in editor mode");
#endif
    }

    private void TestCurrentWave()
    {
        if (waveManager != null)
        {
            // Create temporary wave data for testing
            var testWave = ScriptableObject.CreateInstance<WaveData>();
            testWave.Index = waveIndex;
            testWave.name = waveName + "_Test";
            testWave.GridWidth = gridWidth;
            testWave.GridHeight = gridHeight;
            testWave.CubesData = new List<CubeData>();

            // Convert cube grid
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (cubeGrid[x, y] > 0)
                    {
                        testWave.CubesData.Add(new CubeData
                        {
                            type = (CubeType)(cubeGrid[x, y] - 1),
                            position = new Vector2Int(x, y),
                            level = 1
                        });
                    }
                }
            }

            LoadWaveForTesting(testWave);
            waveManager.StartWave();
        }
    }

    private void CancelEditing()
    {
        editingWave = null;
        isCreatingNewWave = false;
        showWaveEditor = false;
        showCubeEditor = false;
        showMessageEditor = false;
        selectedMessageIndex = -1;
    }

    private void DeleteWave(string assetPath)
    {
#if UNITY_EDITOR
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.Refresh();
        Debug.Log($"Deleted wave at {assetPath}");
#endif
    }

    private void SetWaveSpeed(float newInterval)
    {
        if (waveManager != null)
        {
            // Use reflection to set private fields
            var normalField = waveManager.GetType().GetField("normalMoveInterval",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var fastField = waveManager.GetType().GetField("fastMoveInterval",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (normalField != null)
                normalField.SetValue(waveManager, newInterval);
            if (fastField != null)
                fastField.SetValue(waveManager, newInterval * 0.2f);
        }
    }

    private void SetCubeButtonColor(int cubeType)
    {
        switch (cubeType)
        {
            case 0: GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f); break; // Empty
            case 1: GUI.backgroundColor = Color.white; break; // Normal
            case 2: GUI.backgroundColor = Color.blue; break; // Blue
            case 3: GUI.backgroundColor = Color.black; break; // Black
            default: GUI.backgroundColor = Color.white; break;
        }
    }

    private string GetCubeButtonText(int cubeType)
    {
        switch (cubeType)
        {
            case 0: return "·";
            case 1: return "N";
            case 2: return "B";
            case 3: return "X";
            default: return "?";
        }
    }

    private (int normal, int blue, int black, int total) GetCubeStats(List<CubeData> cubes)
    {
        int normal = cubes.Count(c => c.type == CubeType.Normal);
        int blue = cubes.Count(c => c.type == CubeType.Blue);
        int black = cubes.Count(c => c.type == CubeType.Black);
        return (normal, blue, black, cubes.Count);
    }

    private (int normal, int blue, int black, int total) GetCubeStatsFromGrid()
    {
        int normal = 0, blue = 0, black = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                switch (cubeGrid[x, y])
                {
                    case 1: normal++; break;
                    case 2: blue++; break;
                    case 3: black++; break;
                }
            }
        }
        return (normal, blue, black, normal + blue + black);
    }

    private void ClearCubeGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cubeGrid[x, y] = 0;
            }
        }
    }

    private void FillCubeGrid(int cubeType)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cubeGrid[x, y] = cubeType;
            }
        }
    }

    private void RandomizeCubeGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float rand = Random.value;
                if (rand < 0.3f) cubeGrid[x, y] = 0; // Empty
                else if (rand < 0.7f) cubeGrid[x, y] = 1; // Normal
                else if (rand < 0.9f) cubeGrid[x, y] = 2; // Blue
                else cubeGrid[x, y] = 3; // Black
            }
        }
    }

    private void AddNewMessage()
    {
        editingMessages.Add(new WaveMessage
        {
            Message = "Enter message here...",
            DisplayMoveStep = 0,
            RequirePause = false,
            AutoHideDelay = 3f
        });
    }

    // New helper methods for enhanced functionality

    private void FullReset()
    {
        // Reset wave
        if (waveManager != null)
        {
            waveManager.StopWave();
            waveManager.ClearAllCubes();
        }

        // Clear any debug state
        liveCubeMap.Clear();

        if(editingWave != null)
        {
            waveManager.waveConfiguration = new List<WaveData> { editingWave };
            waveManager.useWaveConfiguration = true;
            waveManager.currentWaveIndex = 0;
        }


        Debug.Log("Full reset completed - Stage and Wave reset");
    }

    private void LoadDesignToGrid()
    {
        if (waveManager == null || gridManager == null) return;

        // Clear existing cubes
        waveManager.ClearAllCubes();

        // Create cubes based on design
        var tempWave = CreateTempWaveFromDesign();

        // Spawn cubes at top of grid
        foreach (var cubeData in tempWave.CubesData)
        {
            Vector3 worldPos = gridManager.GridToWorldPosition(
                cubeData.position.x,
                cubeData.position.y,
                2f);

            if (waveManager.cubePrefabs != null && (int)cubeData.type < waveManager.cubePrefabs.Length)
            {
                GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeData.type], worldPos, Quaternion.identity);
                var cube = cubeObj.GetComponent<CubeBehavior>();
                if (cube == null) cube = cubeObj.AddComponent<CubeBehavior>();

                cube.Init(gridManager, cubeData, 2f);
                waveManager.activeCubes.Add(cube);
            }
        }

        // Enable debug mode for manual control
        if (!debugModeEnabled)
        {
            waveManager.EnterDebugMode(true);
        }

        Debug.Log($"Loaded {tempWave.CubesData.Count} cubes to grid at top (Y={gridManager.Height - gridHeight})");
    }

    private void SpawnFromDesign()
    {
        if (waveManager == null) return;

        var tempWave = CreateTempWaveFromDesign();
        LoadWaveForTesting(tempWave);
        waveManager.StartWave();

        Debug.Log("Spawned wave from design");
    }

    private WaveData CreateTempWaveFromDesign()
    {
        var tempWave = ScriptableObject.CreateInstance<WaveData>();
        tempWave.Index = waveIndex;
        tempWave.name = waveName + "_Design";
        tempWave.GridWidth = gridWidth;
        tempWave.GridHeight = gridHeight;
        tempWave.limitMarkers = limitMarkers;
        tempWave.maxMarkerCharge = maxMarkerCharge;
        tempWave.maxMarkerCount = maxMarkerCount;
        tempWave.waveStartDelay = waveStartDelay;
        tempWave.moveInterval = moveInterval;
        tempWave.fastMoveInterval = fastMoveInterval;

        // Apply message override
        if (overrideMessages)
        {
            tempWave.messages = new List<WaveMessage>();
        }
        else
        {
            tempWave.messages = new List<WaveMessage>(editingMessages);
        }

        // Position cubes at top of grid
        tempWave.CubesData = new List<CubeData>();
        int spawnY = gridManager.Height - gridHeight;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (cubeGrid[x, y] > 0)
                {
                    tempWave.CubesData.Add(new CubeData
                    {
                        type = (CubeType)(cubeGrid[x, y] - 1),
                        position = new Vector2Int(x, spawnY + y),
                        level = 1
                    });
                }
            }
        }

        return tempWave;
    }

    private bool GetOriginalMessageState()
    {
        // Check if current wave originally had messages
        if (waveManager?.CurrentWave != null)
        {
            return waveManager.CurrentWave.messages.Count == 0;
        }
        return false; // Default to messages enabled
    }

    private void ReloadCurrentWave()
    {
        if (waveManager?.CurrentWave != null)
        {
            // Stop current wave
            waveManager.StopWave();

            // Reload with current message override setting
            var originalWave = waveManager.CurrentWave;
            originalWave.CubesData.ForEach(x => x.position = new Vector2Int(x.position.x, x.position.y - gridManager.height));
            LoadWaveForTesting(originalWave);

            Debug.Log($"Reloaded wave with messages {(overrideMessages ? "disabled" : "enabled")}");
        }
    }

    private void MoveAllCubesForward()
    {
        if (waveManager == null) return;

        foreach (var cube in waveManager.activeCubes.ToList())
        {
            if (cube != null && !cube.isDestroyed)
            {
                cube.MoveForward();
            }
        }

        Debug.Log("Moved all cubes forward");
    }

    private void MoveAllCubesBackward()
    {
        if (waveManager == null) return;

        foreach (var cube in waveManager.activeCubes.ToList())
        {
            if (cube != null && !cube.isDestroyed)
            {
                // Move cube backward (increase Y position)
                cube.position = new Vector2Int(cube.position.x, cube.position.y + 1);

                // Update transform position
                Vector3 newWorldPos = gridManager.GridToWorldPosition(cube.position.x, cube.position.y, 2f);
                cube.transform.position = newWorldPos;
            }
        }

        Debug.Log("Moved all cubes backward");
    }
}