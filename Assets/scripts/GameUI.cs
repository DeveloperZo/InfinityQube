using UnityEngine;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private bool showControlsPanel = true;
    [SerializeField] private bool showDetonationTracker = true;
    [SerializeField] private Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color headerColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool showTimeDistortionTracker = true;
    [SerializeField] private bool showRainCubeControls = true;

    // Rain cube controls
    [SerializeField] private Enumerations.CubeType rainCubeType = Enumerations.CubeType.Normal;
    [SerializeField] private int rainX = 2; // Default to middle of grid
    [SerializeField] private int rainY = 2;
    [SerializeField] private int rainMoveCount = 3;

    // References
    private GridManager grid;
    private DetonationManager detonationManager;
    private TimeDistortionManager timeDistortionManager;
    private WaveManager waveManager;

    // UI style caching
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private GUIStyle inputStyle;

    private void Start()
    {
        // Find references
        grid = FindObjectOfType<GridManager>();
        detonationManager = FindObjectOfType<DetonationManager>();
        timeDistortionManager = FindObjectOfType<TimeDistortionManager>();
        waveManager = FindObjectOfType<WaveManager>();

        // Initialize rain position to center of grid
        if (grid != null)
        {
            rainX = grid.Width / 2;
            rainY = grid.Height / 2;
        }
    }

    private void Update()
    {
        // Handle keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showControlsPanel = !showControlsPanel;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            showDetonationTracker = !showDetonationTracker;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showTimeDistortionTracker = !showTimeDistortionTracker;
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            showRainCubeControls = !showRainCubeControls;
        }

        // Rain cube shortcut
        if (Input.GetKeyDown(KeyCode.R) && showRainCubeControls)
        {
            RainCube();
        }
    }

    private void InitializeStyles()
    {
        // Panel style
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(2, 2, panelBackgroundColor);
        panelStyle.padding = new RectOffset(10, 10, 10, 10);

        // Header style
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = headerColor;
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        // Text style
        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = textColor;
        textStyle.fontSize = 14;

        // Button style
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;

        // Box style
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(8, 8, 8, 8);
        boxStyle.margin = new RectOffset(0, 0, 5, 5);

        // Input style
        inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = 14;
        inputStyle.alignment = TextAnchor.MiddleCenter;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {
        // Initialize styles if needed
        if (panelStyle == null)
        {
            InitializeStyles();
        }

        // Right panel (controls reference and charge tracker)
        if (showControlsPanel)
        {
            DrawControlsPanel();
        }

        if (showDetonationTracker)
        {
            DrawDetonationTracker();
        }

        if (showTimeDistortionTracker)
        {
            DrawTimeDistortionTracker();
        }

        if (showRainCubeControls)
        {
            DrawRainCubeControls();
        }
    }

    private void DrawControlsPanel()
    {
        // Controls panel on the right side
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 300));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("CONTROLS", headerStyle);

        GUILayout.Space(5);

        // Controls list
        GUILayout.BeginVertical(boxStyle);

        GUILayout.Label("F1: Toggle Controls Panel", textStyle);
        GUILayout.Label("F2: Toggle Detonation Panel", textStyle);
        GUILayout.Label("F3: Toggle Time Distortion Panel", textStyle);
        GUILayout.Label("F4: Toggle Rain Cube Controls", textStyle);
        GUILayout.Label("Space: Mark/Unmark Tile", textStyle);
        GUILayout.Label("D: Trigger Next Detonation", textStyle);
        GUILayout.Label("T: Trigger Time Distortion", textStyle);
        GUILayout.Label("R: Rain Cube at Selected Position", textStyle);
        GUILayout.Label("Arrow Keys: Move Selector", textStyle);
        GUILayout.Label("Shift: Speed Up (Hold)", textStyle);
        GUILayout.Label("Enter: Start New Wave", textStyle);

        GUILayout.EndVertical();

        // Toggle for showing panels
        GUILayout.Space(10);
        showDetonationTracker = GUILayout.Toggle(showDetonationTracker, "Show Detonation Tracker (F2)");
        showTimeDistortionTracker = GUILayout.Toggle(showTimeDistortionTracker, "Show Time Distortion Tracker (F3)");
        showRainCubeControls = GUILayout.Toggle(showRainCubeControls, "Show Rain Cube Controls (F4)");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawDetonationTracker()
    {
        // Refresh references if needed
        if (detonationManager == null)
        {
            detonationManager = FindObjectOfType<DetonationManager>();
        }
        if (grid == null)
        {
            grid = FindObjectOfType<GridManager>();
        }

        // Detonation tracker panel
        GUILayout.BeginArea(new Rect(Screen.width - 300, 320, 290, 300));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("DETONATION TRACKER", headerStyle);

        GUILayout.Space(5);

        if (detonationManager != null)
        {
            GUILayout.BeginVertical(boxStyle);

            // Count of active detonation points
            int count = detonationManager.DetonationPointCount;
            GUILayout.Label($"Active Detonation Points: {count}", textStyle);

            // Show next detonation position
            Vector2Int nextPoint = detonationManager.GetNextDetonationPoint();
            if (nextPoint.x >= 0)
            {
                GUILayout.Label($"Next Detonation: ({nextPoint.x}, {nextPoint.y})", textStyle);
            }
            else
            {
                GUILayout.Label("No pending detonations", textStyle);
            }

            // Button to trigger next detonation
            if (count > 0)
            {
                if (GUILayout.Button("Detonate Next (D)", buttonStyle))
                {
                    detonationManager.TriggerNextDetonation();
                }
            }

            GUILayout.EndVertical();

            // Find charged tiles in the grid
            if (grid != null)
            {
                GUILayout.Space(5);
                GUILayout.Label("Charged Tiles:", headerStyle);
                GUILayout.BeginVertical(boxStyle);

                bool foundCharged = false;

                // Create a scrollable area for many charged tiles
                Vector2 scrollPosition = Vector2.zero;
                GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

                for (int x = 0; x < grid.Width; x++)
                {
                    for (int y = 0; y < grid.Height; y++)
                    {
                        Tile tile = grid.tiles[x, y];
                        if (tile != null && tile.HasCharges)
                        {
                            foundCharged = true;
                            int charges = tile.DetonationCharges;
                            string chargeText = new string('★', charges);

                            GUILayout.Label($"Tile ({x}, {y}): {chargeText} ({GetDetonationSize(charges)})", textStyle);
                        }
                    }
                }

                GUILayout.EndScrollView();

                if (!foundCharged)
                {
                    GUILayout.Label("No charged tiles found", textStyle);
                }

                GUILayout.EndVertical();
            }
        }
        else
        {
            GUILayout.Label("Detonation Manager not found!", textStyle);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawTimeDistortionTracker()
    {
        // Refresh references if needed
        if (timeDistortionManager == null)
        {
            timeDistortionManager = FindObjectOfType<TimeDistortionManager>();
        }

        // Time distortion tracker panel
        GUILayout.BeginArea(new Rect(Screen.width - 300, 630, 290, 200));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("TIME DISTORTION TRACKER", headerStyle);

        GUILayout.Space(5);

        if (timeDistortionManager != null)
        {
            GUILayout.BeginVertical(boxStyle);

            // Count of active distortion points
            int count = timeDistortionManager.DistortionPointCount;
            GUILayout.Label($"Active Distortion Points: {count}", textStyle);

            // Show next distortion position
            Vector2Int nextPoint = timeDistortionManager.GetNextDistortionPoint();
            if (nextPoint.x >= 0)
            {
                GUILayout.Label($"Next Distortion: ({nextPoint.x}, {nextPoint.y})", textStyle);
            }
            else
            {
                GUILayout.Label("No pending distortions", textStyle);
            }

            // Button to trigger next distortion
            if (count > 0)
            {
                if (GUILayout.Button("Activate Distortion (T)", buttonStyle))
                {
                    timeDistortionManager.TriggerNextDistortion();
                }
            }

            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.Label("Time Distortion Manager not found!", textStyle);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawRainCubeControls()
    {
        // Rain cube controls panel
        GUILayout.BeginArea(new Rect(Screen.width - 300, 840, 290, 200));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("RAIN CUBE CONTROLS", headerStyle);

        GUILayout.Space(5);

        GUILayout.BeginVertical(boxStyle);

        // Cube type selection
        GUILayout.Label("Cube Type:", textStyle);
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        int typeIndex = System.Array.IndexOf(typeNames, rainCubeType.ToString());

        // Handle type selection
        GUILayout.BeginHorizontal();
        for (int i = 0; i < typeNames.Length; i++)
        {
            GUI.backgroundColor = (i == typeIndex) ? Color.green : Color.white;
            if (GUILayout.Button(typeNames[i], GUILayout.Width(70)))
            {
                rainCubeType = (Enumerations.CubeType)i;
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Position input
        GUILayout.Space(5);
        GUILayout.Label("Target Position:", textStyle);

        // X and Y input
        GUILayout.BeginHorizontal();
        GUILayout.Label("X:", GUILayout.Width(30));
        string xInput = GUILayout.TextField(rainX.ToString(), inputStyle, GUILayout.Width(50));
        GUILayout.Label("Y:", GUILayout.Width(30));
        string yInput = GUILayout.TextField(rainY.ToString(), inputStyle, GUILayout.Width(50));

        // Parse inputs
        int.TryParse(xInput, out rainX);
        int.TryParse(yInput, out rainY);

        if (grid != null)
        {
            rainX = Mathf.Clamp(rainX, 0, grid.Width - 1);
            rainY = Mathf.Clamp(rainY, 0, grid.Height - 1);
        }
        GUILayout.EndHorizontal();

        // Move count input
        GUILayout.BeginHorizontal();
        GUILayout.Label("Moves before landing:", textStyle);
        string moveInput = GUILayout.TextField(rainMoveCount.ToString(), inputStyle, GUILayout.Width(50));
        int.TryParse(moveInput, out rainMoveCount);
        rainMoveCount = Mathf.Max(1, rainMoveCount);
        GUILayout.EndHorizontal();

        // Rain button
        if (GUILayout.Button("Rain Cube (R)", buttonStyle))
        {
            RainCube();
        }

        GUILayout.EndVertical();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void RainCube()
    {
        if (grid == null || waveManager == null) return;

        // Make sure position is valid
        if (rainX < 0 || rainX >= grid.Width || rainY < 0 || rainY >= grid.Height)
        {
            Debug.LogWarning($"Invalid rain position: {rainX}, {rainY}");
            return;
        }

        // Get the appropriate prefab
        int prefabIndex = (int)rainCubeType;
        GameObject[] cubePrefabs = waveManager.cubePrefabs;

        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"No prefab found for cube type {rainCubeType}");
            return;
        }

        // Calculate spawn height based on move count
        float spawnHeight = 1f + rainMoveCount * 2f;

        // Create the cube above the target position
        Vector3 spawnPos = new Vector3(rainX, spawnHeight, rainY);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = rainCubeType;
            }

            // Initialize with the correct grid position
            behavior.Init(grid, new Vector2Int(rainX, rainY), 1);

            // Set raining properties
            behavior.isRainingCube = true;
            behavior.moveCountRemaining = rainMoveCount;

            // Register with wave manager
            waveManager.RegisterRainCube(behavior);

            Debug.Log($"Raining {rainCubeType} cube at ({rainX}, {rainY}), move count: {rainMoveCount}");
        }
    }

    private string GetDetonationSize(int charges)
    {
        switch (charges)
        {
            case 3:
                return "3x3 area";
            case 2:
                return "2x2 area";
            case 1:
                return "single tile";
            default:
                return "unknown";
        }
    }

    // Public methods to control UI visibility
    public void ToggleControlsPanel()
    {
        showControlsPanel = !showControlsPanel;
    }

    public void ToggleDetonationTracker()
    {
        showDetonationTracker = !showDetonationTracker;
    }

    public void ShowAllPanels()
    {
        showControlsPanel = true;
        showDetonationTracker = true;
        showTimeDistortionTracker = true;
        showRainCubeControls = true;
    }

    public void HideAllPanels()
    {
        showControlsPanel = false;
        showDetonationTracker = false;
        showTimeDistortionTracker = false;
        showRainCubeControls = false;
    }
}