using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using static Enumerations;
using UnityEditor;

public class WaveDebugger : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerController;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private string saveLocation = "Assets/data/waves/";
    [SerializeField] public WaveData nextWave;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private int defaultWidth = 3;
    [SerializeField] private int defaultHeight = 3;
    [SerializeField] private Color pauseButtonColor = new Color(1f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color playSectionColor = new Color(0.2f, 0.7f, 0.3f, 0.8f);

    // Scroll positions
    private Vector2 mainScrollPosition;
    private Vector2 gridScrollPosition;

    // Debug state
    private bool showDebugger = false;
    private int selectedCubeType = 1; // 1=Normal, 2=Blue, 3=Black
    public List<GameObject> debugObjects = new List<GameObject>();

    // Grid settings - actual game grid dimensions
    private int gridWidth;
    private int gridHeight;

    // Wave settings - editor dimensions
    private int waveWidth;
    private int waveHeight;
    private bool debugMode = true;
    private bool shouldUpdateGrid = false;

    // Wave state tracking
    private int[,] gridState;
    private int[,] buttonState;
    private bool[,] buttonInteractable;
    private List<CubeBehavior> trackedCubes = new List<CubeBehavior>();
    private bool trackingActive = false;
    private float lastUpdateTime = 0f;
    private bool isPaused = false;
    private int waveOffsetY = 0;
    private bool autoResizeGrid = true;
    private bool autoAdjustOffset = true;

    // Speed controls
    private float currentMoveSpeed = 0.75f;
    private float minMoveSpeed = 0.1f;
    private float maxMoveSpeed = 2f;
    private bool runningWave = false;

    // UI settings
    private int buttonSize = 30;
    private int headerHeight = 50;
    private Rect windowRect;

    // Color settings
    private Color normalCubeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private Color blueCubeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    private Color blackCubeColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private Color clearButtonColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);

    // Wave Settings
    private int maxMarkerCharge = 2;
    private int maxMarkerCount = 99;
    private float waveStartDelay = 0.75f;
    private float moveInterval = 0.75f;
    private float fastMoveInterval = 0.1f;
    private bool hasOwnSuccessCriteria = false;
    private int requiredCaptureCount = 0;
    private int maxAllowedEscapes = 0;
    private bool hideMessages = true;  // Default to true

    // Statistics tracking
    private int totalCubesSpawned = 0;
    private int cubesCaptured = 0;
    private int cubesEscaped = 0;
    private int markersUsed = 0;
    private int detonationPointsCreated = 0;

    private bool gridFoldout = true;
    private bool waveFoldout = true;
    private bool controlsFoldout = true;
    private bool cubeFoldout = true;
    private bool actionsFoldout = true;

    // Messages
    private List<WaveMessage> currentWaveMessages = new List<WaveMessage>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeReferences();
        RegisterEventListeners();

        if (gridManager.tiles == null)
        {
            InitializeDefaultGrid();
        }
    }

    private void Update()
    {
        HandleDebuggerToggle();
        UpdateTracking();
        AutoTrackSpawnedWaves();
    }

    private void OnGUI()
    {
        if (!showDebugger) return;
        windowRect = GUILayout.Window(0, windowRect, DrawDebuggerWindow, "Wave Debugger");
    }

    private void OnDestroy()
    {
        CleanupDebugState();
    }

    #endregion

    #region Initialization

    private void InitializeReferences()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerManager>();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
    }

    private void RegisterEventListeners()
    {
        // Hook into WaveManager events to track statistics
        if (waveManager != null)
        {
            // These would need to be added to WaveManager as events
            // waveManager.OnCubeCaptured += HandleCubeCaptured;
            // waveManager.OnCubeEscaped += HandleCubeEscaped;
        }
    }

    private void InitializeDefaultGrid()
    {
        gridWidth = defaultWidth;
        gridHeight = 9; // Default grid height
        waveWidth = defaultWidth;
        waveHeight = defaultHeight;
        InitializeGrid();
        CalculateWindowSize();
    }

    public void InitializeGrid()
    {
        try
        {
            // Validate dimensions
            waveWidth = Mathf.Max(1, Mathf.Min(waveWidth, 12));
            waveHeight = Mathf.Max(1, Mathf.Min(waveHeight, 15));

            // Initialize arrays
            gridState = new int[gridWidth, gridHeight];
            buttonState = new int[waveWidth, waveHeight];
            buttonInteractable = new bool[waveWidth, waveHeight];

            // Fill with default values
            for (int x = 0; x < waveWidth; x++)
            {
                for (int y = 0; y < waveHeight; y++)
                {
                    gridState[x, y] = 1; // Normal cube
                    buttonState[x, y] = 1; // Normal state
                    buttonInteractable[x, y] = true; // Interactive
                }
            }

            ApplyGridSize();
            Debug.Log($"Grid initialized with dimensions: {waveWidth}x{waveHeight}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing grid: {ex.Message}");
            FallbackGridInitialization();
        }
    }

    private void FallbackGridInitialization()
    {
        waveWidth = 3;
        waveHeight = 3;
        gridState = new int[3, 3];
        buttonState = new int[3, 3];
        buttonInteractable = new bool[3, 3];

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                gridState[x, y] = 1;
                buttonState[x, y] = 1;
                buttonInteractable[x, y] = true;
            }
        }
        ApplyGridSize();
    }

    #endregion

    #region Update Methods

    private void HandleDebuggerToggle()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugger = !showDebugger;
            Debug.Log($"Wave Debugger visibility toggled: {showDebugger}");

            if (showDebugger)
            {
                OnDebuggerOpened();
            }
        }
    }

    private void UpdateTracking()
    {
        if (trackingActive && Time.time - lastUpdateTime > 0.2f)
        {
            UpdateCubeTracking();
            lastUpdateTime = Time.time;
        }
    }

    private void AutoTrackSpawnedWaves()
    {
        if (showDebugger && !trackingActive && waveManager != null && waveManager.activeCubes.Count > 0)
        {
            StartTracking();
        }
    }

    private void OnDebuggerOpened()
    {
        // Store current grid state
        gridWidth = gridManager.Width;
        gridHeight = gridManager.Height;

        // Auto track if wave is active
        if (waveManager != null && waveManager.activeCubes.Count > 0)
        {
            StartTracking();
        }
        // Sync with active wave configuration
        else if (waveManager != null && waveManager.CurrentWave != null)
        {
            SyncWithWaveData(waveManager.CurrentWave);
        }
        else if (nextWave != null)
        {
            SyncWithWaveData(nextWave);
        }
        else
        {
            // Initialize with defaults
            waveWidth = Mathf.Min(gridWidth, defaultWidth);
            waveHeight = Mathf.Min(gridHeight / 3, defaultHeight);
            InitializeGrid();
        }

        CalculateWindowSize();
    }

    #endregion

    #region UI Drawing


    private void DrawDebuggerWindow(int windowID)
    {
        mainScrollPosition = GUILayout.BeginScrollView(mainScrollPosition);

        DrawStatusBar();

        gridFoldout = EditorGUILayout.Foldout(gridFoldout, "GRID CONFIGURATION");
        if (gridFoldout) DrawGridConfigurationSection();

        waveFoldout = EditorGUILayout.Foldout(waveFoldout, "WAVE CONFIGURATION");
        if (waveFoldout) DrawWaveConfigurationSection();

        controlsFoldout = EditorGUILayout.Foldout(controlsFoldout, "PLAY CONTROLS");
        if (controlsFoldout) DrawPlayControlsSection();

        cubeFoldout = EditorGUILayout.Foldout(cubeFoldout, "CUBE EDITOR");
        if (cubeFoldout) DrawCubeEditorSection();

        actionsFoldout = EditorGUILayout.Foldout(actionsFoldout, "ACTIONS");
        if (actionsFoldout) DrawActionsSection();

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void DrawStatusBar()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        string trackingStatus = trackingActive ? "TRACKING ACTIVE" : "EDITOR MODE";
        Color statusColor = trackingActive ? Color.green : Color.yellow;
        GUI.color = statusColor;
        GUILayout.Label(trackingStatus, GUILayout.Width(120));
        GUI.color = Color.white;

        if (trackingActive)
        {
            GUILayout.Label($"Cubes: {trackedCubes.Count} | Offset: Y={waveOffsetY}");
        }
        else
        {
            GUILayout.Label($"Grid: {gridWidth}x{gridHeight} | Wave: {waveWidth}x{waveHeight}");
        }

        GUILayout.EndHorizontal();
    }

    private void DrawGridConfigurationSection()
    {
        EditorGUILayout.BeginHorizontal();
        gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 3, 12);
        gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 9, 15);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Apply Grid Size", GUILayout.Height(30)))
        {
            ApplyGridSize();
        }
        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            ClearGrid();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Total Tiles: {gridWidth * gridHeight} | Corrupted: {CountCorruptedTiles()} | Enhanced: {CountEnhancedTiles()} | Marked: {CountMarkedTiles()}");
    }


    private void DrawWaveConfigurationSection()
    {
        EditorGUILayout.BeginHorizontal();
        DrawWaveMessages();
        waveWidth = EditorGUILayout.IntSlider("Wave Width", waveWidth, 1, gridWidth);
        waveHeight = EditorGUILayout.IntSlider("Wave Height", waveHeight, 1, gridHeight / 3);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Gameplay Settings", EditorStyles.boldLabel);
        maxMarkerCharge = EditorGUILayout.IntSlider("Max Marker Charges", maxMarkerCharge, 1, 4);
        maxMarkerCount = EditorGUILayout.IntSlider("Max Marker Coount", maxMarkerCount, 1, 100);
        hideMessages = EditorGUILayout.Toggle("Hide Messages", hideMessages);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Timing Settings", EditorStyles.boldLabel);
        waveStartDelay = EditorGUILayout.FloatField("Start Delay (s)", waveStartDelay);
        moveInterval = EditorGUILayout.FloatField("Move Interval (s)", moveInterval);
        fastMoveInterval = EditorGUILayout.FloatField("Fast Interval (s)", fastMoveInterval);       

        EditorGUILayout.Space(5);

        hasOwnSuccessCriteria = EditorGUILayout.Toggle("Custom Success Criteria", hasOwnSuccessCriteria);
        if (hasOwnSuccessCriteria)
        {
            requiredCaptureCount = EditorGUILayout.IntField("Required Captures", requiredCaptureCount);
            maxAllowedEscapes = EditorGUILayout.IntField("Max Allowed Escapes", maxAllowedEscapes);
        }

        EditorGUILayout.Space(5);
        
        DrawNextWaveSelector();

        if (GUILayout.Button("Randomize", GUILayout.Height(25)))
        {
            RandomizeGrid();
        }
        if (GUILayout.Button("Spawn Wave", GUILayout.Height(30)))
        {
            SpawnWave();
        }
        if (GUILayout.Button("Clear Wave", GUILayout.Height(30)))
        {
            ClearGrid();
        }
        if (GUILayout.Button("Save Wave", GUILayout.Height(30)))
        {
            SaveCurrentWaveAsAsset();
        }

        EditorGUILayout.Space(5);
        

        EditorGUILayout.LabelField($"Move Step: {waveManager.MoveStep} | Spawned: {totalCubesSpawned} | Captured: {cubesCaptured} | Escaped: {cubesEscaped} | Active: {trackedCubes.Count}");
    }


    private void DrawPlayControlsSection()
    {
        EditorGUILayout.BeginHorizontal();
        debugMode = GUILayout.Toggle(debugMode, "Debug Mode (Manual Control)");

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = isPaused ? pauseButtonColor : Color.white;
        if (GUILayout.Button(isPaused ? "Resume" : "Pause", GUILayout.Height(25)))
            TogglePause();

        GUI.enabled = isPaused;
        if (GUILayout.Button("Step Forward", GUILayout.Height(25)))
            StepForward();
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        currentMoveSpeed = EditorGUILayout.Slider("Speed", currentMoveSpeed, minMoveSpeed, maxMoveSpeed);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fast", GUILayout.Height(20))) { currentMoveSpeed = 0.5f; UpdateMoveSpeed(); }
        if (GUILayout.Button("Normal", GUILayout.Height(20))) { currentMoveSpeed = 1.5f; UpdateMoveSpeed(); }
        if (GUILayout.Button("Slow", GUILayout.Height(20))) { currentMoveSpeed = 2.5f; UpdateMoveSpeed(); }
        EditorGUILayout.EndHorizontal();

        debugMode = EditorGUILayout.Toggle("Debug Mode", debugMode);
        autoResizeGrid = EditorGUILayout.Toggle("Auto-Resize Grid", autoResizeGrid);
        autoAdjustOffset = EditorGUILayout.Toggle("Auto-Adjust Offset", autoAdjustOffset);
    }

    private void DrawCubeEditorSection()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(selectedCubeType == 1, "Normal", EditorStyles.miniButton))
            selectedCubeType = 1;
        if (GUILayout.Toggle(selectedCubeType == 2, "Blue", EditorStyles.miniButton))
            selectedCubeType = 2;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(selectedCubeType == 3, "Black", EditorStyles.miniButton))
            selectedCubeType = 3;
        if (GUILayout.Toggle(selectedCubeType == 0, "Clear", EditorStyles.miniButton))
            selectedCubeType = 0;
        EditorGUILayout.EndHorizontal();

        gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, GUILayout.Height(200));

        for (int y = 0; y < waveHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < waveWidth; x++)
            {
                SetButtonColorForType(gridState[x, y]);
                if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    gridState[x, y] = selectedCubeType;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }


    private void DrawActionsSection()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
            ClearGrid();

        if (GUILayout.Button("Randomize", GUILayout.Height(25)))
            RandomizeGrid();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Wave", GUILayout.Height(30)))
            SpawnWave();

        if (GUILayout.Button("Save as Asset", GUILayout.Height(30)))
            SaveCurrentWaveAsAsset();
        EditorGUILayout.EndHorizontal();
    }


    #endregion

    #region Grid Configuration UI

    #endregion

    #region Wave Identity UI

    private void DrawNextWaveSelector()
    {
        // Display current nextWave if any
        GUILayout.BeginHorizontal();
        string waveName = (nextWave != null) ? nextWave.name : "None";
        GUILayout.Label($"Current Wave: {waveName}");
        GUILayout.EndHorizontal();

        // Show available waves in a compact list
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Available Waves:");

        // Find all WaveData assets in the project
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData");
        int columnCount = 0;

        foreach (string guid in guids)
        {
            if (columnCount % 3 == 0)
                GUILayout.BeginHorizontal();

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);

            if (wave != null)
            {
                if (GUILayout.Button(wave.name, GUILayout.Width((windowRect.width - 60) / 3)))
                {
                    nextWave = wave;
                    SyncWithWaveData(wave);
                }
            }

            columnCount++;
            if (columnCount % 3 == 0)
                GUILayout.EndHorizontal();
        }

        if (columnCount % 3 != 0)
            GUILayout.EndHorizontal();
#endif

        GUILayout.EndVertical();

        // Actions for selected wave
        GUILayout.BeginHorizontal();
        if (nextWave != null && GUILayout.Button("Load Wave"))
        {
            SyncWithWaveData(nextWave);
        }
        if (GUILayout.Button("Clear Selection"))
        {
            nextWave = null;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawActiveWaveInfo()
    {
        if (waveManager != null && waveManager.CurrentWave != null)
        {
            GUILayout.Label("Active Wave Info:", GUI.skin.box);
            var wave = waveManager.CurrentWave;
            GUILayout.Label($"Name: {wave.name}");
            GUILayout.Label($"Size: {wave.GridWidth}x{wave.GridHeight}");
            GUILayout.Label($"Cubes: {wave.CubesData.Count}");
            GUILayout.Label($"Move Step: {waveManager.MoveStep}");
        }
    }

    private void DrawWaveMessages()
    {
        if (waveManager != null && waveManager.CurrentWave != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Wave Messages:", GUI.skin.box);

            var currentWave = waveManager.CurrentWave;
            if (!hideMessages && currentWave.messages != null && currentWave.messages.Count > 0)
            {
                foreach (var message in currentWave.messages)
                {
                    if (message.DisplayMoveStep == -1 || message.DisplayMoveStep == waveManager.MoveStep)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Label($"Step {message.DisplayMoveStep}: {message.Message}");
                        if (message.RequirePause)
                            GUILayout.Label("(Pauses Game)", GUILayout.Width(100));
                        GUILayout.EndVertical();
                    }
                }
            }
            else
            {
                GUILayout.Label(hideMessages ? "" : "No messages for this wave");
            }

            GUILayout.EndHorizontal();
        }
    }

    #endregion

    #region Wave Configuration UI
   
    #endregion

    #region Play Controls UI


    #endregion

    #region Cube Editor UI


    #endregion

    #region Actions UI


    #endregion

    #region Grid Management

    private void ApplyGridSize()
    {
        // Make sure grid height is at least 9 tiles
        gridHeight = Mathf.Max(gridHeight, 9);
        gridWidth = gridWidth < 3 ? Mathf.Max(3, waveWidth) : gridWidth;

        // Apply changes to the actual grid in the scene ONLY when explicitly requested
        if (gridManager != null)
        {
            bool needsResize = gridManager.Width != gridWidth || gridManager.height != gridHeight;

            if (needsResize)
            {
                // Destroy the existing grid
                gridManager.DestroyGrid();

                // Update grid dimensions
                gridManager.width = gridWidth;
                gridManager.height = gridHeight;

                // Generate new grid - this will recreate the tiles array
                gridManager.GenerateGrid();

                Debug.Log($"Applied new grid dimensions to scene: {gridWidth}x{gridHeight}");

                // Reset flag after updating
                shouldUpdateGrid = false;
            }
        }

        // Recreate the local grid arrays for the editor
        int[,] newGridState = new int[waveWidth, waveHeight];
        bool[,] newButtonInteractable = new bool[waveWidth, waveHeight];
        int[,] newButtonState = new int[waveWidth, waveHeight];

        // Copy existing values where possible
        if (gridState != null)
        {
            int oldWidth = Mathf.Min(gridState.GetLength(0), waveWidth);
            int oldHeight = Mathf.Min(gridState.GetLength(1), waveHeight);

            for (int x = 0; x < oldWidth; x++)
            {
                for (int y = 0; y < oldHeight; y++)
                {
                    newGridState[x, y] = gridState[x, y];

                    if (buttonInteractable != null && buttonState != null)
                    {
                        newButtonInteractable[x, y] = buttonInteractable[x, y];
                        newButtonState[x, y] = buttonState[x, y];
                    }
                }
            }
        }

        // Initialize any new cells
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                // Only set values for cells that weren't copied
                if (gridState == null || x >= gridState.GetLength(0) || y >= gridState.GetLength(1))
                {
                    newGridState[x, y] = 1; // Default to normal cube
                    newButtonInteractable[x, y] = true;
                    newButtonState[x, y] = 1;
                }
            }
        }

        // Update the arrays
        gridState = newGridState;
        buttonInteractable = newButtonInteractable;
        buttonState = newButtonState;

        // Update window size
        CalculateWindowSize();

        Debug.Log($"Applied new local wave dimensions for editor: {waveWidth}x{waveHeight}");
    }

    private void ResizeGridForNextWave()
    {
        if (nextWave == null || nextWave.GridWidth <= 0 || nextWave.GridHeight <= 0) return;

        // Set the wave dimensions to match the selected wave
        waveWidth = nextWave.GridWidth;
        waveHeight = nextWave.GridHeight;

        // Ensure the grid is at least as wide as the wave and at least 3 times as tall
        gridWidth = Mathf.Max(gridWidth, waveWidth);
        gridHeight = Mathf.Max(gridHeight, waveHeight * 3);

        // Ensure minimum grid size
        gridWidth = Mathf.Max(3, gridWidth);
        gridHeight = Mathf.Max(9, gridHeight);

        // Apply these changes to the actual grid in the scene
        InitializeGrid();
        CalculateWindowSize();
        Debug.Log($"Resized for wave: {nextWave.name} - Wave dimensions: {waveWidth}x{waveHeight}, Grid dimensions: {gridWidth}x{gridHeight}");
    }

    private void ClearGrid()
    {
        if (trackingActive)
        {
            // Stop tracking and clear all cubes
            trackingActive = false;
            waveManager.ClearAllCubes();
            trackedCubes.Clear();
        }

        // Set all cells to normal cubes
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                gridState[x, y] = 1;
            }
        }

        waveManager.StopAllCoroutines();
        waveManager.ClearAllCubes();
    }

    private void RandomizeGrid()
    {
        if (trackingActive) return; // Cannot randomize during tracking

        int totalCells = waveWidth * waveHeight;
        int maxBlue = Mathf.FloorToInt(totalCells * 0.2f);
        int maxBlack = Mathf.FloorToInt(totalCells * 0.2f);

        int blueCount = Random.Range(1, maxBlue + 1);
        int blackCount = Random.Range(1, maxBlack + 1);

        // Reset all to normal cubes
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                gridState[x, y] = 1;
            }
        }

        // Place blue cubes randomly
        PlaceRandomCubes(2, blueCount);

        // Place black cubes randomly
        PlaceRandomCubes(3, blackCount);
    }

    private void PlaceRandomCubes(int cubeType, int count)
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = 100; // Prevent infinite loop

        while (placed < count && attempts < maxAttempts)
        {
            int x = Random.Range(0, waveWidth);
            int y = Random.Range(0, waveHeight);

            // Only place if it's a normal cube (to avoid overwriting other special cubes)
            if (gridState[x, y] == 1)
            {
                gridState[x, y] = cubeType;
                placed++;
            }

            attempts++;
        }
    }

    #endregion

    #region Wave Management

    private void SyncWithWaveData(WaveData waveData)
    {
        if (waveData == null) return;

        // Sync dimensions
        waveWidth = waveData.GridWidth;
        waveHeight = waveData.GridHeight;

        // Sync all wave settings
        maxMarkerCharge = waveData.maxMarkerCharge;
        maxMarkerCount = waveData.maxMarkerCount;
        waveStartDelay = waveData.waveStartDelay;
        moveInterval = waveData.moveInterval;
        fastMoveInterval = waveData.fastMoveInterval;
        hasOwnSuccessCriteria = waveData.hasOwnSuccessCriteria;
        requiredCaptureCount = waveData.requiredCaptureCount;
        maxAllowedEscapes = waveData.maxAllowedEscapes;

        // Load messages
        if (waveData.messages != null)
        {
            currentWaveMessages = new List<WaveMessage>(waveData.messages);
        }
        else
        {
            currentWaveMessages.Clear();
        }

        // Initialize grid with updated dimensions
        InitializeGrid();
        CalculateWindowSize();

        Debug.Log($"Synced with wave data: Size={waveWidth}x{waveHeight}, Markers={maxMarkerCharge}");
    }

    private void SpawnWave()
    {
        if (waveManager == null)
        {
            Debug.LogError("WaveManager not found!");
            return;
        }

        // Stop tracking if active
        trackingActive = false;

        // Convert grid to wave data
        WaveData waveData = new WaveData()
        {
            Index = 0,
            CubesData = new List<CubeData>(),
            limitMarkers = true,
            maxMarkerCharge = maxMarkerCharge,
            maxMarkerCount = maxMarkerCount,
            waveStartDelay = waveStartDelay,
            moveInterval = moveInterval,
            fastMoveInterval = fastMoveInterval,
            hasOwnSuccessCriteria = hasOwnSuccessCriteria,
            requiredCaptureCount = requiredCaptureCount,
            maxAllowedEscapes = maxAllowedEscapes,
            messages = hideMessages ? new List<WaveMessage>(): new List<WaveMessage>(currentWaveMessages)
        };

        if (nextWave != null)
        {
            // First ensure grid is properly sized
            ResizeGridForNextWave();

            waveData.GridWidth = nextWave.GridWidth;
            waveData.GridHeight = nextWave.GridHeight;

            foreach (var cube in nextWave.CubesData)
            {
                CubeData newCube = new CubeData();
                newCube.type = cube.type;

                // Simple formula to position wave at the top of the grid:
                // gridManager.height - waveHeight + cube.position.y
                int yPosition = gridManager.height - waveHeight + cube.position.y;
                newCube.position = new Vector2Int(cube.position.x, yPosition);

                newCube.level = cube.level;
                waveData.CubesData.Add(newCube);

                Debug.Log($"Spawning {newCube.type} cube at position ({newCube.position.x}, {newCube.position.y}) - Original wave pos: ({cube.position.x}, {cube.position.y})");
            }
        }
        else
        {
            // Using the custom editor design - make sure the grid is big enough
            ApplyGridSize();

            waveData.GridWidth = waveWidth;
            waveData.GridHeight = waveHeight;

            for (int y = 0; y < waveHeight; y++)
            {
                for (int x = 0; x < waveWidth; x++)
                {
                    // Skip empty cells
                    if (gridState[x, y] == 0) continue;

                    CubeData newCube = new CubeData();

                    // Position cubes at the top of the grid - similar formula as above
                    // but invert y coordinate since editor has 0 at top
                    int editorY = waveHeight - 1 - y; // Convert to bottom-up coordinate
                    int yPosition = gridManager.height - waveHeight + editorY;

                    newCube.position = new Vector2Int(x, yPosition);
                    newCube.type = (CubeType)(gridState[x, y] - 1);
                    newCube.level = 1;
                    waveData.CubesData.Add(newCube);

                    Debug.Log($"Spawning custom {newCube.type} cube at position ({newCube.position.x}, {newCube.position.y}) - From editor pos: ({x}, {y})");
                }
            }
        }

        // Set appropriate wave manager flags
        waveManager.useWaveConfiguration = true;

        // Spawn the wave and start tracking
        isPaused = false; // Start unpaused
        runningWave = true;
        waveManager.waveActive = false;
        waveManager.waveConfiguration = new List<WaveData>() { waveData };
        waveManager.StartWave();
        
        // Update move speed settings
        UpdateMoveSpeed();

        // Start tracking the new wave
        StartCoroutine(DelayedTracking());
    }

    private void SaveCurrentWaveAsAsset()
    {
        if (!trackingActive && gridState == null)
        {
            Debug.LogWarning("No wave data to save");
            return;
        }

        // Create a new WaveData asset
        WaveData waveData = ScriptableObject.CreateInstance<WaveData>();

        // Save all wave settings
        waveData.GridWidth = waveWidth;
        waveData.GridHeight = waveHeight;
        waveData.maxMarkerCharge = maxMarkerCharge;
        waveData.maxMarkerCount = maxMarkerCount;
        waveData.waveStartDelay = waveStartDelay;
        waveData.moveInterval = moveInterval;
        waveData.fastMoveInterval = fastMoveInterval;
        waveData.hasOwnSuccessCriteria = hasOwnSuccessCriteria;
        waveData.requiredCaptureCount = requiredCaptureCount;
        waveData.maxAllowedEscapes = maxAllowedEscapes;
        waveData.limitMarkers = true; // Always true based on your requirements
        waveData.CubesData = new List<CubeData>();
        waveData.messages = new List<WaveMessage>(currentWaveMessages);

        // Find the highest position (closest to player) among cubes to use as reference
        int minY = int.MaxValue;

        // If tracking active cubes on screen
        if (trackingActive)
        {
            foreach (var cube in trackedCubes)
            {
                if (cube == null || cube.isDestroyed) continue;
                minY = Mathf.Min(minY, cube.position.y);
            }

            // Edge case: no valid cubes
            if (minY == int.MaxValue) minY = 0;

            // Populate with normalized positions (relative to the bottom-most cube)
            foreach (var cube in trackedCubes)
            {
                if (cube == null || cube.isDestroyed) continue;

                CubeData cubeData = new CubeData();
                cubeData.type = cube.type;
                cubeData.position = new Vector2Int(
                    cube.position.x,
                    cube.position.y - minY // Normalize Y position relative to the bottom row
                );
                cubeData.level = cube.level;

                waveData.CubesData.Add(cubeData);
            }
        }
        else
        {
            // Find the lowest populated row in the editor grid (highest Y value in the 2D array)
            int lowestRow = waveHeight - 1;
            while (lowestRow >= 0)
            {
                bool rowHasCubes = false;
                for (int x = 0; x < waveWidth; x++)
                {
                    if (gridState[x, lowestRow] > 0)
                    {
                        rowHasCubes = true;
                        break;
                    }
                }

                if (rowHasCubes) break;
                lowestRow--;
            }

            // Use editor grid state
            for (int x = 0; x < waveWidth; x++)
            {
                for (int y = 0; y <= lowestRow; y++) // Only process up to the lowest populated row
                {
                    if (gridState[x, y] > 0) // Not empty
                    {
                        CubeData cubeData = new CubeData();
                        cubeData.type = (Enumerations.CubeType)(gridState[x, y] - 1); // Convert from button state
                        cubeData.position = new Vector2Int(
                            x,
                            lowestRow - y // Invert Y and make it relative to the lowest row
                        );
                        cubeData.level = 1;

                        waveData.CubesData.Add(cubeData);
                    }
                }
            }
        }

        // Create directory if it doesn't exist
        if (!System.IO.Directory.Exists(saveLocation))
        {
            System.IO.Directory.CreateDirectory(saveLocation);
        }

        // Get unique name
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string assetPath = saveLocation + "Wave_" + timestamp + ".asset";

        // Save the asset
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(waveData, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Wave saved to: " + assetPath);
#endif
    }

    #endregion

    #region Tracking Management

    private void StartTracking()
    {
        if (waveManager == null || waveManager.activeCubes.Count == 0)
        {
            trackingActive = false;
            return;
        }

        trackingActive = true;
        trackedCubes = new List<CubeBehavior>(waveManager.activeCubes);

        // Calculate grid dimensions and offset based on active cubes
        int maxX = 0;
        int minY = int.MaxValue;
        int maxY = 0;

        foreach (var cube in trackedCubes)
        {
            maxX = Mathf.Max(maxX, cube.position.x + 1);
            minY = Mathf.Min(minY, cube.position.y);
            maxY = Mathf.Max(maxY, cube.position.y + 1);
        }

        // Calculate required height and offset
        int requiredHeight = maxY - minY;
        waveOffsetY = minY; // Store the minimum Y as our offset

        // Resize grid if needed
        if (autoResizeGrid)
        {
            waveWidth = Mathf.Max(waveWidth, maxX);
            waveHeight = Mathf.Max(waveHeight, requiredHeight);
            InitializeGrid();
            CalculateWindowSize();
        }

        Debug.Log($"Wave tracking started: Width={maxX}, MinY={minY}, MaxY={maxY}, " +
                  $"RequiredHeight={requiredHeight}, Offset={waveOffsetY}");

        // Reset button states
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                buttonState[x, y] = 0; // Start with all disabled
                buttonInteractable[x, y] = false;
            }
        }

        // Initialize button states based on current cubes
        UpdateCubeTracking();
    }

    private void UpdateCubeTracking()
    {
        if (!trackingActive) return;

        // Check if we have cubes to track
        trackedCubes = trackedCubes
            .Where(c => c != null && !c.isDestroyed)
            .ToList();

        if (trackedCubes.Count == 0)
        {
            trackingActive = false;
            return;
        }

        // Recalculate the offset if autoAdjustOffset is enabled
        if (autoAdjustOffset)
        {
            // Find the current min Y position among all tracked cubes
            int minY = int.MaxValue;
            int maxY = 0;

            foreach (var cube in trackedCubes)
            {
                minY = Mathf.Min(minY, cube.position.y);
                maxY = Mathf.Max(maxY, cube.position.y);
            }

            // Update offset to track the wave
            waveOffsetY = minY;
        }

        // Reset all button states
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                buttonState[x, y] = 0;
                buttonInteractable[x, y] = false;
            }
        }

        // Update button states based on current cube positions, applying the offset
        foreach (var cube in trackedCubes)
        {
            int x = cube.position.x;
            int y = cube.position.y - waveOffsetY; // Apply the offset

            // Ensure we're within grid bounds
            if (x >= 0 && x < waveWidth && y >= 0 && y < waveHeight)
            {
                buttonState[x, y] = (int)cube.type + 1; // Convert to button state
                buttonInteractable[x, y] = true; // Cube is present, button is interactive
            }
        }
    }

    #endregion

    #region Cube Management

    private void ChangeCubeType(int x, int y, int newType)
    {
        if (!trackingActive) return;

        // Apply the offset to get the actual position in the game
        int actualY = y + waveOffsetY;

        Debug.Log($"Attempting to change cube at display:({x}, {y}) actual:({x}, {actualY}) to type {newType}");

        // Find cube at this position (using actual position, not display position)
        CubeBehavior targetCube = null;
        foreach (var cube in trackedCubes)
        {
            if (cube != null && cube.position.x == x && cube.position.y == actualY)
            {
                targetCube = cube;
                Debug.Log($"Found target cube of type {targetCube.type} at position ({x}, {actualY})");
                break;
            }
        }

        if (targetCube != null)
        {
            if (newType == 0) // Special case: Clear/Remove the cube
            {
                Debug.Log($"Removing cube at ({x}, {y})");

                // Remove from tracking lists
                trackedCubes.Remove(targetCube);
                if (waveManager != null && waveManager.activeCubes.Contains(targetCube))
                {
                    waveManager.activeCubes.Remove(targetCube);
                }

                // Destroy the cube GameObject
                DestroyImmediate(targetCube.gameObject);

                // Update tracking after removal
                UpdateCubeTracking();
                return;
            }

            // For other types, replace the cube
            Enumerations.CubeType oldType = targetCube.type;
            Enumerations.CubeType newCubeType = (Enumerations.CubeType)(newType - 1);

            // If type hasn't changed, do nothing
            if (oldType == newCubeType) return;

            Debug.Log($"Replacing cube at ({x}, {y}) from type {oldType} to {newCubeType}");

            // Replace the cube
            ReplaceActiveCube(targetCube, newCubeType);

            // Update tracking
            UpdateCubeTracking();
        }
        else
        {
            Debug.LogWarning($"No cube found at position ({x}, {y})");
        }
    }

    private void ReplaceActiveCube(CubeBehavior oldCube, Enumerations.CubeType newType)
    {
        if (waveManager == null || oldCube == null) return;

        // Store cube position and properties
        Vector2Int position = oldCube.position;
        Vector3 worldPos = oldCube.transform.position;
        int moveCountRemaining = oldCube.moveCountRemaining;
        bool isRainingCube = oldCube.isRainingCube;

        Debug.Log($"Replacing cube: Position={position}, WorldPos={worldPos}, Type={oldCube.type} to {newType}");

        // Remove old cube from tracking and destroy
        trackedCubes.Remove(oldCube);
        waveManager.activeCubes.Remove(oldCube);

        // Use Destroy with a delay to avoid conflicts
        Destroy(oldCube.gameObject);

        // Wait a frame before creating the new cube
        StartCoroutine(SpawnReplacementCube(position, worldPos, newType, moveCountRemaining, isRainingCube));
    }

    private IEnumerator SpawnReplacementCube(Vector2Int position, Vector3 worldPos,
                                           Enumerations.CubeType newType, int moveCount, bool isRaining)
    {
        // Wait a frame to ensure old cube is gone
        yield return null;

        // Make sure the system still exists
        if (waveManager == null || !trackingActive) yield break;

        // Create new cube of the desired type
        int prefabIndex = (int)newType;
        if (prefabIndex < 0 || prefabIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Missing cube prefab for type {newType}");
            yield break;
        }

        // Spawn new cube
        GameObject newCubeObj = Instantiate(waveManager.cubePrefabs[prefabIndex], worldPos, Quaternion.identity);
        CubeBehavior newCube = newCubeObj.GetComponent<CubeBehavior>();

        if (newCube == null)
        {
            newCube = newCubeObj.AddComponent<CubeBehavior>();
        }

        // Set properties on the new cube
        CubeData cubeData = new CubeData();
        cubeData.type = newType;
        cubeData.position = position;

        newCube.Init(gridManager, cubeData, 1);
        newCube.moveCountRemaining = moveCount;
        newCube.isRainingCube = isRaining;

        // Set the world position after initialization
        newCube.transform.position = worldPos;

        // Add to tracking lists
        trackedCubes.Add(newCube);
        waveManager.activeCubes.Add(newCube);

        Debug.Log($"Successfully created replacement cube at {position} of type {newType}");

        // Update tracking display
        UpdateCubeTracking();
    }

    #endregion

    #region Play Control Methods

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (waveManager != null)
        {
            if (isPaused)
            {
                waveManager.PauseWave();
            }
            else
            {
                waveManager.ResumeWave();
                UpdateMoveSpeed(); // Ensure correct speed on resume
            }

            Debug.Log($"Wave {(isPaused ? "paused" : "resumed")} - Manual control: {waveManager.manualControl}");
        }
    }

    private void StepForward()
    {
        if (!isPaused || waveManager == null) return;

        // Execute a single step forward
        waveManager.ManualMoveWaveForward();

        // Make sure to update tracking after the step
        UpdateCubeTracking();
    }

    private void UpdateMoveSpeed()
    {
        if (waveManager != null)
        {
            // Directly modify the move intervals in WaveManager
            // Access using reflection to avoid modifying WaveManager class
            var normalSpeedField = waveManager.GetType().GetField("normalMoveInterval",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            var fastSpeedField = waveManager.GetType().GetField("fastMoveInterval",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            if (normalSpeedField != null)
            {
                normalSpeedField.SetValue(waveManager, currentMoveSpeed);
                Debug.Log($"Set normal move interval to {currentMoveSpeed}s");
            }

            if (fastSpeedField != null)
            {
                // Fast speed is always 20% of normal speed
                float fastSpeed = currentMoveSpeed * 0.2f;
                fastSpeedField.SetValue(waveManager, fastSpeed);
                Debug.Log($"Set fast move interval to {fastSpeed}s");
            }
        }
    }

    #endregion


    #region Helper Methods
    private int CountCorruptedTiles()
    {
        int count = 0;
        if (gridManager != null && gridManager.tiles != null)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Tile tile = gridManager.tiles[x, y];
                    if (tile != null && tile.IsBlackened)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    private int CountEnhancedTiles()
    {
        int count = 0;
        if (gridManager != null && gridManager.tiles != null)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Tile tile = gridManager.tiles[x, y];
                    if (tile != null && tile.IsAdvantaged)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    private int CountMarkedTiles()
    {
        int count = 0;
        if (gridManager != null && gridManager.tiles != null)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Tile tile = gridManager.tiles[x, y];
                    if (tile != null && tile.HasMarker)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }


    public void CalculateWindowSize()
    {
        windowRect = new Rect(10, 50, 420, Screen.height - 100);
    }


    private void SetButtonColorForType(int cubeType)
    {
        switch (cubeType)
        {
            case 0: // Disabled/Cleared
                GUI.backgroundColor = disabledColor;
                break;
            case 1: // Normal
                GUI.backgroundColor = normalCubeColor;
                break;
            case 2: // Blue
                GUI.backgroundColor = blueCubeColor;
                break;
            case 3: // Black
                GUI.backgroundColor = blackCubeColor;
                break;
            default:
                GUI.backgroundColor = Color.white;
                break;
        }
    }


    private IEnumerator DelayedTracking()
    {
        // Brief delay to allow cubes to spawn properly
        yield return new WaitForSeconds(0.1f);
        StartTracking();
    }


    private void CleanupDebugState()
    {
        // Clear tracked cubes
        trackedCubes.Clear();

        // Clear debug objects
        if (debugObjects != null)
        {
            foreach (var obj in debugObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            debugObjects.Clear();
        }

        // Reset tracking state
        trackingActive = false;
        runningWave = false;
        isPaused = false;

        // Stop all coroutines
        StopAllCoroutines();

        Debug.Log("WaveDebugger cleaned up");
    }


    #endregion


}