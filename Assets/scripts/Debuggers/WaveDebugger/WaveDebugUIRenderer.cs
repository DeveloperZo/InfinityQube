using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveDebugUIRenderer : MonoBehaviour
{
    private WaveDebugGridConfigurator gridConfig;
    private WaveDebugWaveController waveController;
    private WaveDebugDataCollector dataCollector;

    [Header("UI Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private int buttonSize = 30;
    [SerializeField] private int windowPadding = 10;

    [Header("Colors")]
    [SerializeField] private Color normalCubeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color blueCubeColor = new Color(0.3f, 0.3f, 1f, 1f);
    [SerializeField] private Color blackCubeColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color emptyCubeColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);
    [SerializeField] private Color trackingActiveColor = Color.green;
    [SerializeField] private Color editorModeColor = Color.yellow;

    // UI State
    private bool showDebugger = false;
    private Vector2 mainScrollPosition;
    private Vector2 gridScrollPosition;
    private Rect windowRect;

    // Foldout states
    private bool gridFoldout = true;
    private bool waveInfoFoldout = true;
    private bool cubeEditorFoldout = true;
    private bool actionsFoldout = true;
    private bool loadSaveFoldout = true;

    private void Awake()
    {
        gridConfig = GetComponent<WaveDebugGridConfigurator>();
        waveController = GetComponent<WaveDebugWaveController>();
        dataCollector = GetComponent<WaveDebugDataCollector>();

        // Initialize window size
        windowRect = new Rect(10, 50, 450, Screen.height - 100);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugger = !showDebugger;

            if (showDebugger)
            {
                OnDebuggerOpened();
            }
        }
    }

    private void OnGUI()
    {
        if (!showDebugger) return;

        windowRect = GUILayout.Window(0, windowRect, DrawDebuggerWindow, "Wave Debugger v2.0");
    }

    private void OnDebuggerOpened()
    {
        // Sync with current wave state when debugger opens
        waveController.OnDebuggerOpened();
    }

    #region Main Window Drawing

    private void DrawDebuggerWindow(int windowID)
    {
        mainScrollPosition = GUILayout.BeginScrollView(mainScrollPosition);

        DrawStatusHeader();

        if (gridFoldout = EditorGUILayout.Foldout(gridFoldout, "GRID CONFIGURATION"))
        {
            DrawGridConfigurationSection();
        }

        if (waveInfoFoldout = EditorGUILayout.Foldout(waveInfoFoldout, "WAVE INFORMATION"))
        {
            DrawWaveInformationSection();
        }

        if (cubeEditorFoldout = EditorGUILayout.Foldout(cubeEditorFoldout, "CUBE EDITOR"))
        {
            DrawCubeEditorSection();
        }

        if (loadSaveFoldout = EditorGUILayout.Foldout(loadSaveFoldout, "LOAD / SAVE"))
        {
            DrawLoadSaveSection();
        }

        if (actionsFoldout = EditorGUILayout.Foldout(actionsFoldout, "ACTIONS"))
        {
            DrawActionsSection();
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    #endregion

    #region UI Sections

    private void DrawStatusHeader()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        // Tracking status
        bool isTracking = waveController != null && (waveController.GetType()
            .GetField("isTrackingActiveWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(waveController) as bool? ?? false);

        GUI.color = isTracking ? trackingActiveColor : editorModeColor;
        GUILayout.Label(isTracking ? "TRACKING" : "EDITOR", GUILayout.Width(80));
        GUI.color = Color.white;

        // Stats
        if (isTracking)
        {
            GUILayout.Label($"Spawned: {dataCollector.TotalSpawned} | Removed: {dataCollector.TotalRemoved}");
        }
        else
        {
            int cubeCount = waveController != null ? waveController.GetCubeCount() : 0;
            GUILayout.Label($"Cubes: {cubeCount} | {waveController?.GetCurrentWaveInfo() ?? "No Wave"}");
        }

        // Unsaved changes indicator
        if (waveController != null && waveController.HasUnsavedChanges())
        {
            GUI.color = Color.yellow;
            GUILayout.Label("*", GUILayout.Width(15));
            GUI.color = Color.white;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawGridConfigurationSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Grid size controls with increment/decrement
        EditorGUILayout.LabelField("Grid Dimensions:", EditorStyles.boldLabel);

        var gridManager = FindObjectOfType<GridManager>();
        int currentGridWidth = gridManager != null ? gridManager.width : 6;
        int currentGridHeight = gridManager != null ? gridManager.height : 10;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Grid Width:", GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            int newWidth = Mathf.Max(3, currentGridWidth - 1);
            waveController.UpdateGridDimensions(newWidth, currentGridHeight);
        }
        GUILayout.Label(currentGridWidth.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            int newWidth = Mathf.Min(15, currentGridWidth + 1);
            waveController.UpdateGridDimensions(newWidth, currentGridHeight);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Grid Height:", GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            int newHeight = Mathf.Max(9, currentGridHeight - 1);
            waveController.UpdateGridDimensions(currentGridWidth, newHeight);
        }
        GUILayout.Label(currentGridHeight.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            int newHeight = Mathf.Min(20, currentGridHeight + 1);
            waveController.UpdateGridDimensions(currentGridWidth, newHeight);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Wave size controls
        EditorGUILayout.LabelField("Wave Dimensions:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Wave Width:", GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            int newWidth = Mathf.Max(3, gridConfig.WaveWidth - 1);
            waveController.UpdateWaveDimensions(newWidth, gridConfig.WaveHeight);
        }
        GUILayout.Label(gridConfig.WaveWidth.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            int newWidth = Mathf.Min(8, gridConfig.WaveWidth + 1);
            waveController.UpdateWaveDimensions(newWidth, gridConfig.WaveHeight);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Wave Height:", GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            int newHeight = Mathf.Max(3, gridConfig.WaveHeight - 1);
            waveController.UpdateWaveDimensions(gridConfig.WaveWidth, newHeight);
        }
        GUILayout.Label(gridConfig.WaveHeight.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            int newHeight = Mathf.Min(8, gridConfig.WaveHeight + 1);
            waveController.UpdateWaveDimensions(gridConfig.WaveWidth, newHeight);
        }
        EditorGUILayout.EndHorizontal();

        // Grid actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize"))
        {
            gridConfig.RandomizeGrid();
            waveController.InitializeWaveState();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawWaveInformationSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Current wave info
        string waveInfo = waveController?.GetCurrentWaveInfo() ?? "No Wave Loaded";
        EditorGUILayout.LabelField("Current Wave:", waveInfo);

        // Cube statistics
        if (waveController != null)
        {
            int normalCount = waveController.GetCubeCountByType(Enumerations.CubeType.Normal);
            int blueCount = waveController.GetCubeCountByType(Enumerations.CubeType.Blue);
            int blackCount = waveController.GetCubeCountByType(Enumerations.CubeType.Black);
            int totalCount = waveController.GetCubeCount();

            EditorGUILayout.LabelField("Cube Counts:", $"Normal: {normalCount}, Blue: {blueCount}, Black: {blackCount}, Total: {totalCount}");
            EditorGUILayout.LabelField("Active Cubes:", waveController.GetActiveCubeCount().ToString());
        }

        // Wave Manager State Controls
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Wave Manager State:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wave Active:", GUILayout.Width(100));
        EditorGUILayout.LabelField(waveController != null ? waveController.IsWaveActive().ToString() : "Unknown");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Debug Mode:", GUILayout.Width(100));
        bool debugMode = waveController != null ? waveController.IsDebugMode() : false;
        bool newDebugMode = EditorGUILayout.Toggle(debugMode);
        if (newDebugMode != debugMode && waveController != null)
        {
            waveController.SetDebugMode(newDebugMode);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Manual Control:", GUILayout.Width(100));
        bool manualControl = waveController != null ? waveController.IsManualControl() : false;
        bool newManualControl = EditorGUILayout.Toggle(manualControl);
        if (newManualControl != manualControl && waveController != null)
        {
            waveController.SetManualControl(newManualControl);
        }
        EditorGUILayout.EndHorizontal();

        // Data collector stats
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Session Stats:", $"Spawned: {dataCollector.TotalSpawned}, Removed: {dataCollector.TotalRemoved}");

        EditorGUILayout.EndVertical();
    }

    private void DrawCubeEditorSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("Click cubes to cycle: Empty → Normal → Blue → Black → Empty", EditorStyles.helpBox);

        // Cube grid
        gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, GUILayout.Height(250));

        var waveState = waveController?.GetCurrentWaveState() ?? new Dictionary<Vector2Int, Enumerations.CubeType>();

        for (int y = 0; y < gridConfig.WaveHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < gridConfig.WaveWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                bool hasCube = waveState.ContainsKey(pos);
                Enumerations.CubeType cubeType = hasCube ? waveState[pos] : Enumerations.CubeType.Normal;

                // Set button color based on cube type
                if (hasCube)
                {
                    switch (cubeType)
                    {
                        case Enumerations.CubeType.Normal:
                            GUI.backgroundColor = normalCubeColor;
                            break;
                        case Enumerations.CubeType.Blue:
                            GUI.backgroundColor = blueCubeColor;
                            break;
                        case Enumerations.CubeType.Black:
                            GUI.backgroundColor = blackCubeColor;
                            break;
                    }
                }
                else
                {
                    GUI.backgroundColor = emptyCubeColor;
                }

                // Draw button
                string buttonText = hasCube ? GetCubeTypeShortName(cubeType) : "·";
                if (GUILayout.Button(buttonText, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    waveController.ToggleCubeAtPosition(x, y);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Quick tools
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Normal"))
        {
            FillAllPositions(Enumerations.CubeType.Normal);
        }
        if (GUILayout.Button("Clear All"))
        {
            waveController.ResetWave();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawLoadSaveSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Save current wave
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Wave"))
        {
            waveController.SaveCurrentWaveAsAsset();
        }

        bool hasChanges = waveController != null && waveController.HasUnsavedChanges();
        GUI.enabled = hasChanges;
        if (GUILayout.Button("Save As..."))
        {
            // Could implement a custom save dialog here
            waveController.SaveCurrentWaveAsAsset();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Load existing waves
        EditorGUILayout.LabelField("Load Existing Wave:", EditorStyles.boldLabel);

#if UNITY_EDITOR
        string[] waveGuids = AssetDatabase.FindAssets("t:WaveData");
        if (waveGuids.Length > 0)
        {
            int buttonsPerRow = 2;
            int currentButton = 0;

            foreach (string guid in waveGuids)
            {
                if (currentButton % buttonsPerRow == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                string path = AssetDatabase.GUIDToAssetPath(guid);
                WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);

                if (wave != null)
                {
                    string buttonLabel = $"{wave.name}\n({wave.GridWidth}x{wave.GridHeight})";
                    if (GUILayout.Button(buttonLabel, GUILayout.Height(40)))
                    {
                        waveController.ForceLoadWave(wave);  // Use ForceLoadWave instead
                    }
                }

                currentButton++;

                if (currentButton % buttonsPerRow == 0 || currentButton == waveGuids.Length)
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No wave assets found", EditorStyles.helpBox);
        }
#endif

        EditorGUILayout.EndVertical();
    }

    private void DrawActionsSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Wave control buttons
        EditorGUILayout.LabelField("Wave Controls:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Start Wave", GUILayout.Height(30)))
        {
            waveController.StartWave();
        }

        if (GUILayout.Button("Stop Wave", GUILayout.Height(30)))
        {
            waveController.StopWave();
        }

        if (GUILayout.Button("Reset Wave", GUILayout.Height(30)))
        {
            waveController.ResetCurrentWave();
        }

        EditorGUILayout.EndHorizontal();

        // Manual control
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Step Forward", GUILayout.Height(25)))
        {
            waveController.StepWaveForward();
        }

        bool isTracking = waveController != null && (waveController.GetType()
            .GetField("isTrackingActiveWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(waveController) as bool? ?? false);

        if (isTracking)
        {
            if (GUILayout.Button("Stop Tracking", GUILayout.Height(25)))
            {
                waveController.StopTracking();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Utility actions
        // Utility actions
        EditorGUILayout.LabelField("Utilities:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Data Collector"))
        {
            dataCollector.Reset();
        }
        if (GUILayout.Button("Clear Active Cubes"))
        {
            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.ClearAllCubes();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Helper Methods

    private void FillAllPositions(Enumerations.CubeType cubeType)
    {
        for (int x = 0; x < gridConfig.WaveWidth; x++)
        {
            for (int y = 0; y < gridConfig.WaveHeight; y++)
            {
                waveController.SetCubeAtPosition(x, y, cubeType);
            }
        }
    }

    private string GetCubeTypeShortName(Enumerations.CubeType cubeType)
    {
        switch (cubeType)
        {
            case Enumerations.CubeType.Normal:
                return "N";
            case Enumerations.CubeType.Blue:
                return "B";
            case Enumerations.CubeType.Black:
                return "X";
            default:
                return "";
        }
    }

    #endregion
}