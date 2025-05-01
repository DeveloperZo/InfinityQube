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

    // References
    private GridManager grid;
    private DetonationManager detonationManager;
    private TimeDistortionManager timeDistortionManager;

    // UI style caching
    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;

    private void Start()
    {
        // Find references
        grid = FindObjectOfType<GridManager>();
        detonationManager = FindObjectOfType<DetonationManager>();
        timeDistortionManager = FindObjectOfType<TimeDistortionManager>();

        // Initialize styles on first use
        InitializeStyles();
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

        GUILayout.Label("F1: Toggle Debug Mode", textStyle);
        GUILayout.Label("F2: Clear All Debug Objects", textStyle);
        GUILayout.Label("F3: Execute Test (Drop Cubes)", textStyle);
        GUILayout.Label("Space: Mark/Unmark Tile", textStyle);
        GUILayout.Label("D: Trigger Next Detonation", textStyle);
        GUILayout.Label("Arrow Keys: Move Selector", textStyle);
        GUILayout.Label("Shift: Speed Up (Hold)", textStyle);
        GUILayout.Label("Enter: Start New Wave", textStyle);

        GUILayout.EndVertical();

        // Toggle for showing panels
        GUILayout.Space(10);
        showDetonationTracker = GUILayout.Toggle(showDetonationTracker, "Show Detonation Tracker");
        showTimeDistortionTracker = GUILayout.Toggle(showTimeDistortionTracker, "Show Time Distortion Tracker");

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
                if (GUILayout.Button("Detonate Next", buttonStyle))
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
        GUILayout.BeginArea(new Rect(Screen.width - 300, 630, 290, 300));

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
    }

    public void HideAllPanels()
    {
        showControlsPanel = false;
        showDetonationTracker = false;
    }
}