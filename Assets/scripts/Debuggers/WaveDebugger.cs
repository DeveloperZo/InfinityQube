using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class WaveDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerController playerController;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private int defaultWidth = 5;
    [SerializeField] private int defaultHeight = 2;
    [SerializeField] private bool centerOnScreen = true;
    [SerializeField] private Color pauseButtonColor = new Color(1f, 0.5f, 0.5f, 1f);

    // Debug state
    private bool showDebugger = false;
    private Vector2 scrollPosition;
    private int selectedCubeType = 1; // 1=Normal, 2=Green, 3=Black
    public List<GameObject> debugObjects = new List<GameObject>();

    // Grid settings
    private int waveWidth;
    private int waveHeight;
    private bool debugMode = true;

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
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();

        // Initialize settings
        waveWidth = defaultWidth;
        waveHeight = defaultHeight;

        // Initialize grid
        InitializeGrid();
        CalculateWindowSize();
    }

    private void CalculateWindowSize()
    {
        // Calculate the window size and position
        int windowWidth = waveWidth * (buttonSize + 2) + 20;
        int windowHeight = waveHeight * (buttonSize + 2) + headerHeight + 150; // Added more space for new controls

        if (centerOnScreen)
        {
            windowRect = new Rect(
                (Screen.width - windowWidth) / 2,
                (Screen.height - windowHeight) / 2,
                windowWidth,
                windowHeight
            );
        }
        else
        {
            windowRect = new Rect(20, 20, windowWidth, windowHeight);
        }
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
        gridState = new int[waveWidth, waveHeight];
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
                buttonState[x, y] = (int)cube.CubeType + 1; // Convert to button state
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
        DrawPauseControls();
        DrawGridDimensions();
        DrawActionButtons();
        DrawDebugModeToggle();
        DrawOffsetInfo(); // Add this line to display offset info

        // Grid area
        DrawGridArea();

        // Stats
        GUILayout.Label(GetCubeStats());

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
        if (GUILayout.Toggle(selectedCubeType == 2, "Green", "Button"))
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

    private void DrawPauseControls()
    {
        GUILayout.BeginHorizontal();

        // Pause/Unpause button
        GUI.backgroundColor = isPaused ? pauseButtonColor : Color.white;
        string pauseButtonText = isPaused ? "Resume Wave" : "Pause Wave";

        if (GUILayout.Button(pauseButtonText))
        {
            TogglePause();
        }

        GUI.backgroundColor = Color.white;

        // Step Forward button (only enabled when paused)
        GUI.enabled = isPaused;
        if (GUILayout.Button("Step Forward"))
        {
            StepForward();
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }

    private void DrawGridDimensions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Width:", GUILayout.Width(40));
        string widthStr = GUILayout.TextField(waveWidth.ToString(), GUILayout.Width(30));
        if (int.TryParse(widthStr, out int newWidth) && newWidth != waveWidth && newWidth >= 2 && newWidth <= 12)
        {
            waveWidth = newWidth;
            InitializeGrid();
            CalculateWindowSize();
        }

        GUILayout.Label("Height:", GUILayout.Width(40));
        string heightStr = GUILayout.TextField(waveHeight.ToString(), GUILayout.Width(30));
        if (int.TryParse(heightStr, out int newHeight) && newHeight != waveHeight && newHeight >= 2 && newHeight <= 15)
        {
            waveHeight = newHeight;
            InitializeGrid();
            CalculateWindowSize();
        }
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
        GUILayout.Label("Currently tracking live wave - click cubes to modify them");

        for (int y = waveHeight - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < waveWidth; x++)
            {
                if (buttonInteractable[x, y])
                {
                    int currentState = buttonState[x, y];

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
        GUILayout.Label("Design custom wave pattern:");

        for (int y = waveHeight - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < waveWidth; x++)
            {
                int currentState = gridState[x, y];

                // Set button color based on cube type
                SetButtonColorForType(currentState);

                if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    gridState[x, y] = selectedCubeType;
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
            case 2: // Green
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
                Debug.Log($"Found target cube of type {targetCube.CubeType} at position ({x}, {actualY})");
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
            Enumerations.CubeType oldType = targetCube.CubeType;
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

        Debug.Log($"Replacing cube: Position={position}, WorldPos={worldPos}, Type={oldCube.CubeType} to {newType}");

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
        newCube.CubeType = newType;
        newCube.Init(gridManager, position, 1);
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

                switch (cube.CubeType)
                {
                    case Enumerations.CubeType.Normal: normalCount++; break;
                    case Enumerations.CubeType.Green: greenCount++; break;
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

        return $"Normal: {normalCount}, Green: {greenCount}, Black: {blackCount}";
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
        List<WaveData> waveData = new List<WaveData>();

        for (int y = 0; y < waveHeight; y++)
        {
            for (int x = 0; x < waveWidth; x++)
            {
                // Skip empty cells
                if (gridState[x, y] == 0) continue;

                waveData.Add(new WaveData
                {
                    cubeType = (Enumerations.CubeType)(gridState[x, y] - 1), // Convert to enum (0-based)
                    position = new Vector2Int(x, y), // Invert Y axis
                    waveIndex = 0 // Single wave for now
                });
            }
        }

        // Spawn the wave
        waveManager.useWaveConfiguration = true;
        
        waveManager.SpawnCustomWave(waveData, false);

        // Start tracking the new wave
        StartTracking();
    }

    // New methods for pause functionality
    private void TogglePause()
    {
        isPaused = !isPaused;

        if (waveManager != null)
        {
            // Update wave manager state
            waveManager.waveActive = !isPaused;
            waveManager.debugMode = true;
            waveManager.manualControl = isPaused;

            Debug.Log($"Wave {(isPaused ? "paused" : "resumed")} - Wave active: {waveManager.waveActive}, Manual control: {waveManager.manualControl}");
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
    private void StepForward()
    {
        if (!isPaused || waveManager == null) return;

        // Execute a single step forward
        waveManager.ManualMoveWaveForward();

        // Make sure to update tracking after the step
        UpdateTracking();
    }

    [System.Serializable]
    public class WaveData
    {
        public Enumerations.CubeType cubeType;
        public Vector2Int position;
        public int waveIndex;
    }
}