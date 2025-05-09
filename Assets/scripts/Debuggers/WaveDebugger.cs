using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using static Enumerations;

public class WaveDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerController;
    [SerializeField] private string saveLocation = "Assets/data/waves/";
    [SerializeField] public WaveData nextWave;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private int defaultWidth = 3;
    [SerializeField] private int defaultHeight = 3;
    [SerializeField] private bool centerOnScreen = true;
    [SerializeField] private Color pauseButtonColor = new Color(1f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color playSectionColor = new Color(0.2f, 0.7f, 0.3f, 0.8f);

    // Debug state
    private bool showDebugger = false;
    private Vector2 scrollPosition;
    private int selectedCubeType = 1; // 1=Normal, 2=Blue, 3=Black
    public List<GameObject> debugObjects = new List<GameObject>();

    // Grid settings
    private int gridWidth;
    private int gridHeight;

    private int waveWidth;
    private int waveHeight;
    private bool debugMode = true;
    private bool shouldUpdateGrid = false;

    // Wave state tracking
    private int[,] gridState;
    private int[,] buttonState; // 0=disabled, 1=normal, 2=green, 3=black
    private bool[,] buttonInteractable;
    private List<CubeBehavior> trackedCubes = new List<CubeBehavior>();
    private bool trackingActive = false;
    private float lastUpdateTime = 0f;
    private bool isPaused = false;
    private int waveOffsetY = 0; // How many rows to offset the wave from the bottom
    private bool autoResizeGrid = true;
    private bool autoAdjustOffset = true;

    // Speed controls
    private float currentMoveSpeed = 2f; // Default move speed (in seconds)
    private float minMoveSpeed = 1f;
    private float maxMoveSpeed = 4f;
    private bool runningWave = false;

    // UI settings
    private int buttonSize = 30;
    private int headerHeight = 50;
    private Rect windowRect;

    // Color settings for buttons
    private Color normalCubeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private Color greenCubeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    private Color blackCubeColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private Color clearButtonColor = new Color(0.9f, 0.9f, 0.9f, 0.1f); // Almost transparent white

    private void Start()
    {
        // Auto-find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerManager>();


        // Initialize grid

        if (gridManager.tiles == null)
        {
            CalculateWindowSize();
        }

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
        waveData.GridWidth = waveWidth;
        waveData.GridHeight = waveHeight;
        waveData.CubesData = new List<CubeData>();

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

    private void CalculateWindowSize()
    {
        // Calculate the window size with fixed position on left side
        int windowWidth = waveWidth * (buttonSize + 2) + 20;
        int windowHeight = waveHeight * (buttonSize + 2) + headerHeight + 320; // Added more space for controls

        // Position on left side of screen with margin
        windowRect = new Rect(
            20, // Fixed left margin
            60, // Fixed top margin (leave space for other UI)
            windowWidth,
            windowHeight
        );
    }

    private void Update()
    {
        // Toggle debugger visibility
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugger = !showDebugger;
            Debug.Log($"Wave Debugger visibility toggled: {showDebugger}");

            // If activating debugger, refresh cube state
            if (showDebugger && waveManager != null && waveManager.activeCubes.Count > 0)
            {
                StartTracking();
            }
        }

        // Update cube tracking (refresh at most 5 times per second)
        if (trackingActive && Time.time - lastUpdateTime > 0.2f)
        {
            UpdateTracking();
            lastUpdateTime = Time.time;
        }
    }

    private void InitializeGrid()
    {
        try
        {
            // Make sure dimensions are valid
            waveWidth = Mathf.Max(1, Mathf.Min(waveWidth, 12));
            waveHeight = Mathf.Max(1, Mathf.Min(waveHeight, 15));

            // Clear and recreate arrays
            gridState = new int[gridWidth, gridHeight];
            buttonState = new int[waveWidth, waveHeight];
            buttonInteractable = new bool[waveWidth, waveHeight];

            // Fill with normal cubes by default
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

            // Fallback to minimal grid
            waveWidth = 3;
            waveHeight = 3;
            gridState = new int[3, 3];
            buttonState = new int[3, 3];
            buttonInteractable = new bool[3, 3];

            // Fill with default values
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
    }

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
        UpdateTracking();
    }

    private void UpdateTracking()
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

    private void OnGUI()
    {
        if (!showDebugger) return;

        // Draw the debugger window
        windowRect = GUILayout.Window(0, windowRect, DrawDebuggerWindow, "Wave Debugger");
    }

    private void DrawDebuggerWindow(int windowID)
    {
        // Top controls
        DrawCubeTypeSelection();
        DrawPlayControls();

        // Grid dimensions
        DrawGridDimensions();

        // Wave dimensions - add this line
        DrawWaveDimensions();

        DrawActionButtons();
        DrawDebugModeToggle();
        DrawOffsetInfo();

        // Grid area
        DrawGridArea();
        DrawNextWaveSelector();

        // Stats
        GUILayout.Label(GetCubeStats());

        if (GUILayout.Button("Save Wave as Asset"))
        {
            SaveCurrentWaveAsAsset();
        }

        GUILayout.TextField(waveManager.MoveStep.ToString());
        // Make window draggable
        GUI.DragWindow();
    }

    private void DrawCubeTypeSelection()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cube Type:", GUILayout.Width(70));

        GUI.backgroundColor = normalCubeColor;
        if (GUILayout.Toggle(selectedCubeType == 1, "Normal", "Button"))
            selectedCubeType = 1;

        GUI.backgroundColor = greenCubeColor;
        if (GUILayout.Toggle(selectedCubeType == 2, "Blue", "Button"))
            selectedCubeType = 2;

        GUI.backgroundColor = blackCubeColor;
        GUIStyle blackButtonStyle = new GUIStyle(GUI.skin.button);
        blackButtonStyle.normal.textColor = Color.white;
        if (GUILayout.Toggle(selectedCubeType == 3, "Black", blackButtonStyle))
            selectedCubeType = 3;

        GUI.backgroundColor = clearButtonColor;
        if (GUILayout.Toggle(selectedCubeType == 0, "Clear", "Button"))
            selectedCubeType = 0;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawPlayControls()
    {
        // Play controls section background
        GUI.backgroundColor = playSectionColor;
        GUILayout.BeginVertical(GUI.skin.box);
        GUI.backgroundColor = Color.white;

        GUILayout.Label("Playback Controls:", GUI.skin.box);

        GUILayout.BeginHorizontal();

        // Pause/Resume button
        GUI.backgroundColor = isPaused ? pauseButtonColor : Color.white;
        string pauseButtonText = isPaused ? "Resume Wave" : "Pause Wave";

        if (GUILayout.Button(pauseButtonText))
        {
            TogglePause();
        }

        // Step Forward button (only enabled when paused)
        GUI.enabled = isPaused;
        if (GUILayout.Button("Step Forward"))
        {
            StepForward();
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        // Speed slider
        GUILayout.BeginHorizontal();
        GUILayout.Label("Move Speed:", GUILayout.Width(80));
        float newSpeed = GUILayout.HorizontalSlider(currentMoveSpeed, minMoveSpeed, maxMoveSpeed);
        if (newSpeed != currentMoveSpeed)
        {
            currentMoveSpeed = newSpeed;
            UpdateMoveSpeed();
        }
        GUILayout.Label($"{currentMoveSpeed:F2}s", GUILayout.Width(50));
        GUILayout.EndHorizontal();

        // Quick preset speeds
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Fast (0.1s)"))
        {
            currentMoveSpeed = 0.1f;
            UpdateMoveSpeed();
        }
        if (GUILayout.Button("Normal (0.5s)"))
        {
            currentMoveSpeed = 0.5f;
            UpdateMoveSpeed();
        }
        if (GUILayout.Button("Slow (1.0s)"))
        {
            currentMoveSpeed = 1.0f;
            UpdateMoveSpeed();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawGridDimensions()
    {
        GUILayout.BeginHorizontal();

        // Width controls with increment/decrement buttons
        GUILayout.Label("Grid Width:", GUILayout.Width(70));

        // Decrement button
        GUI.enabled = gridWidth > 3; // Minimum width is 3
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            if (gridWidth > 3)
            {
                gridWidth--;
            }
        }
        GUI.enabled = true;

        // Display current width
        GUILayout.Label(gridWidth.ToString(), GUILayout.Width(30), GUILayout.MinWidth(30));

        // Increment button
        GUI.enabled = gridWidth < 12; // Maximum width is 12
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            if (gridWidth < 12)
            {
                gridWidth++;
            }
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Height controls with increment/decrement buttons
        GUILayout.Label("Grid Height:", GUILayout.Width(70));

        // Decrement button
        GUI.enabled = gridHeight > 9; // Minimum height is 9 tiles
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            if (gridHeight > 9)
            {
                gridHeight--;
            }
        }
        GUI.enabled = true;

        // Display current height
        GUILayout.Label(gridHeight.ToString(), GUILayout.Width(30), GUILayout.MinWidth(30));

        // Increment button
        GUI.enabled = gridHeight < 15; // Maximum height is 15
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            if (gridHeight < 15)
            {
                gridHeight++;
            }
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();

        // Add Apply button in a new row for better visibility
        if (GUILayout.Button("Apply Grid Size"))
        {
            ApplyGridSize();
        }
    }

    private void DrawWaveDimensions()
    {
        GUILayout.Space(10);
        GUILayout.Label("Wave Editor Dimensions:", GUI.skin.box);

        GUILayout.BeginHorizontal();

        // Width controls with increment/decrement buttons
        GUILayout.Label("Wave Width:", GUILayout.Width(70));

        // Decrement button
        GUI.enabled = waveWidth > 3; // Minimum width is 3
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            if (waveWidth > 3)
            {
                waveWidth--;
                InitializeGrid(); // Reinitialize grid state with new dimensions
                CalculateWindowSize();
            }
        }
        GUI.enabled = true;

        // Display current width
        GUILayout.Label(waveWidth.ToString(), GUILayout.Width(30), GUILayout.MinWidth(30));

        // Increment button
        GUI.enabled = waveWidth < gridWidth; // Maximum limited by grid width
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            if (waveWidth < gridWidth)
            {
                waveWidth++;
                InitializeGrid(); // Reinitialize grid state with new dimensions
                CalculateWindowSize();
            }
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Height controls with increment/decrement buttons
        GUILayout.Label("Wave Height:", GUILayout.Width(70));

        // Decrement button
        GUI.enabled = waveHeight > 1; // Minimum height is 1 for wave
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            if (waveHeight > 1)
            {
                waveHeight--;
                InitializeGrid(); // Reinitialize grid state with new dimensions
                CalculateWindowSize();
            }
        }
        GUI.enabled = true;

        // Display current height
        GUILayout.Label(waveHeight.ToString(), GUILayout.Width(30), GUILayout.MinWidth(30));

        // Increment button
        GUI.enabled = waveHeight < Mathf.Min(gridHeight / 3, 15); // Maximum is 1/3 of grid height or 15
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            if (waveHeight < Mathf.Min(gridHeight / 3, 15))
            {
                waveHeight++;
                InitializeGrid(); // Reinitialize grid state with new dimensions
                CalculateWindowSize();
            }
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }



    private void DrawActionButtons()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear"))
        {
            ClearGrid();
        }

        if (GUILayout.Button("Randomize"))
        {
            RandomizeGrid();
        }

        if (GUILayout.Button("Spawn Wave"))
        {
            SpawnWave();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawDebugModeToggle()
    {
        debugMode = GUILayout.Toggle(debugMode, "Debug Mode (Manual Control)");

        // Add checkbox for grid updating
        if (nextWave != null)
        {
            shouldUpdateGrid = GUILayout.Toggle(shouldUpdateGrid, "Update Grid Size When Spawning");
        }
    }

    private void DrawGridArea()
    {
        GUILayout.Space(5);

        if (trackingActive)
        {
            DrawTrackedGrid();
        }
        else
        {
            DrawEditorGrid();
        }
    }

    private void DrawTrackedGrid()
    {
        // Make sure our arrays are initialized
        if (gridState == null || buttonState == null || buttonInteractable == null)
        {
            InitializeGrid();
        }

        GUILayout.Label("Currently tracking live wave - click cubes to modify them");

        // Make sure waveWidth and waveHeight are within sensible bounds
        int displayWidth = Mathf.Min(waveWidth, 12);
        int displayHeight = Mathf.Min(waveHeight, 15);

        for (int y = 0; y < displayHeight; y++)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < displayWidth; x++)
            {
                // Safety check to avoid index out of range
                bool isInteractable = false;
                int currentState = 0;

                if (x < buttonInteractable.GetLength(0) && y < buttonInteractable.GetLength(1))
                {
                    isInteractable = buttonInteractable[x, y];
                    if (x < buttonState.GetLength(0) && y < buttonState.GetLength(1))
                    {
                        currentState = buttonState[x, y];
                    }
                }

                if (isInteractable)
                {
                    // Set button color based on cube type
                    SetButtonColorForType(currentState);

                    if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        ChangeCubeType(x, y, selectedCubeType);
                    }
                }
                else
                {
                    // Disabled button
                    GUI.backgroundColor = disabledColor;
                    GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
                }
            }

            // Reset color
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Stop Tracking"))
        {
            trackingActive = false;
        }
    }

    private void DrawEditorGrid()
    {
        // Make sure our arrays are initialized
        if (gridState == null || buttonState == null || buttonInteractable == null)
        {
            InitializeGrid();
        }

        GUILayout.Label("Design custom wave pattern:");

        // Make sure waveWidth and waveHeight are within sensible bounds
        int displayWidth = Mathf.Min(waveWidth, 12);  // Limit to reasonable display size
        int displayHeight = Mathf.Min(waveHeight, 15);

        for (int y = 0; y < displayHeight; y++)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < displayWidth; x++)
            {
                // Safety check to avoid index out of range
                int currentState = 0;
                if (x < gridState.GetLength(0) && y < gridState.GetLength(1))
                {
                    currentState = gridState[x, y];
                }
                else
                {
                    Debug.LogWarning($"Attempted to access invalid gridState index: [{x},{y}]");
                }

                // Set button color based on cube type
                SetButtonColorForType(currentState);

                if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    // Safety check before setting
                    if (x < gridState.GetLength(0) && y < gridState.GetLength(1))
                    {
                        gridState[x, y] = selectedCubeType;
                    }
                    else
                    {
                        Debug.LogWarning($"Attempted to set invalid gridState index: [{x},{y}]");
                    }
                }
            }

            // Reset color
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }
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
                GUI.backgroundColor = greenCubeColor;
                break;
            case 3: // Black
                GUI.backgroundColor = blackCubeColor;
                break;
            default:
                GUI.backgroundColor = Color.white;
                break;
        }
    }

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
                UpdateTracking();
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
            UpdateTracking();
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
        UpdateTracking();
    }

    private string GetCubeStats()
    {
        int normalCount = 0;
        int greenCount = 0;
        int blackCount = 0;

        if (trackingActive)
        {
            foreach (var cube in trackedCubes)
            {
                if (cube == null) continue;

                switch (cube.type)
                {
                    case Enumerations.CubeType.Normal: normalCount++; break;
                    case Enumerations.CubeType.Blue: greenCount++; break;
                    case Enumerations.CubeType.Black: blackCount++; break;
                }
            }
        }
        else
        {
            for (int x = 0; x < waveWidth; x++)
            {
                for (int y = 0; y < waveHeight; y++)
                {
                    switch (gridState[x, y])
                    {
                        case 1: normalCount++; break;
                        case 2: greenCount++; break;
                        case 3: blackCount++; break;
                    }
                }
            }
        }

        return $"Normal: {normalCount}, Blue: {greenCount}, Black: {blackCount}";
    }

    private void RandomizeGrid()
    {
        if (trackingActive) return; // Cannot randomize during tracking

        int totalCells = waveWidth * waveHeight;
        int maxGreen = Mathf.FloorToInt(totalCells * 0.2f);
        int maxBlack = Mathf.FloorToInt(totalCells * 0.2f);

        int greenCount = Random.Range(1, maxGreen + 1);
        int blackCount = Random.Range(1, maxBlack + 1);

        // Reset all to normal cubes
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                gridState[x, y] = 1;
            }
        }

        // Place green cubes randomly
        PlaceRandomCubes(2, greenCount);

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

    private void DrawNextWaveSelector()
    {
        GUILayout.Space(10);
        GUILayout.Label("Next Wave Selection", GUI.skin.box);

        // Display current nextWave if any
        GUILayout.BeginHorizontal();
        string waveName = (nextWave != null) ? nextWave.name : "None";
        GUILayout.Label($"Current Next Wave: {waveName}");
        GUILayout.EndHorizontal();

        // Show details about wave size if available
        if (nextWave != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Wave Grid Size: {nextWave.GridWidth}x{nextWave.GridHeight} - " +
                            $"Cubes: {nextWave.CubesData.Count}");

            // Add update grid button
            if (GUILayout.Button("Update Grid", GUILayout.Width(80)))
            {
                ResizeGridForNextWave();
            }

            GUILayout.EndHorizontal();
        }

        // Add buttons for available waves
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Available Waves:");

        // Find all WaveData assets in the project
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);

            if (wave != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(wave.name))
                {
                    nextWave = wave;
                }
                GUILayout.EndHorizontal();
            }
        }
#endif

        // Clear button
        if (GUILayout.Button("Clear Next Wave"))
        {
            nextWave = null;
        }

        GUILayout.EndVertical();
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
        WaveData waveData = new WaveData() { Index = 0, CubesData = new List<CubeData>() };
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
        waveManager.SpawnCustomWave(new List<WaveData> { waveData }, debugMode);

        // Update move speed settings
        UpdateMoveSpeed();

        // Start tracking the new wave
        StartCoroutine(DelayedTracking());
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
    private IEnumerator DelayedTracking()
    {
        // Brief delay to allow cubes to spawn properly
        yield return new WaitForSeconds(0.1f);
        StartTracking();
    }

    // Toggle between pause and play states
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


    private void DrawOffsetInfo()
    {
        if (trackingActive)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Wave Position: Y-Offset={waveOffsetY} (Row {waveOffsetY} and above)");
            GUILayout.EndHorizontal();
        }
    }

    // Step forward a single move when paused
    private void StepForward()
    {
        if (!isPaused || waveManager == null) return;

        // Execute a single step forward
        waveManager.ManualMoveWaveForward();

        // Make sure to update tracking after the step
        UpdateTracking();
    }

    // Update the move speed in the WaveManager
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

    // Called when the game is being shut down or scene is changing
    private void OnDestroy()
    {
        // Ensure we don't leave the WaveManager in manual/debug mode
        if (waveManager != null)
        {
            waveManager.debugMode = false;
            waveManager.manualControl = false;
        }
    }
}