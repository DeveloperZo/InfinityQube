using UnityEngine;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private bool showControlsPanel = true;
    [SerializeField] private Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color headerColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color textColor = Color.white;

    // Rain cube controls
    [SerializeField] private Enumerations.CubeType rainCubeType = Enumerations.CubeType.Normal;
    [SerializeField] private int rainX = 2; // Default to middle of grid
    [SerializeField] private int rainY = 2;
    [SerializeField] private int rainMoveCount = 3;

    // References
    private GridManager grid;
    private DetonationManager detonationManager;


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

        // Initialize rain position to center of grid
        if (grid != null)
        {
            rainX = grid.Width / 2;
            rainY = grid.Height / 2;
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
            DrawDetonationTracker();
            DrawPlayerStatus();
            DrawPlayerStatistics();
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

        GUILayout.Label("F1: Static Debugger", textStyle);
        GUILayout.Label("F2: Wave Debugger", textStyle);
        GUILayout.Label("R: Rain Cube at Selected Position", textStyle);

        GUILayout.Label("Space: Mark/Unmark Current Tile", textStyle);
        GUILayout.Label("D: Trigger Next Detonation", textStyle);
        GUILayout.Label("T: Trigger Time Distortion", textStyle);
        
        GUILayout.Label("Arrow Keys: Move Selector", textStyle);
        GUILayout.Label("Shift: Speed Up (Hold)", textStyle);
        GUILayout.Label("Enter: Start New Wave", textStyle);

        GUILayout.EndVertical();

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
                nextPoint = detonationManager.GetNextDetonationPoint();
                if (nextPoint.x >= 0)
                {
                    // Determine area size based on grid width
                    string areaSize = "2x2"; // Default
                    if (grid != null)
                    {
                        int width = grid.Width;
                        if (width <= 3) areaSize = "2x2";
                        else if (width <= 5) areaSize = "3x3";
                        else areaSize = "5x5";
                    }

                    GUILayout.Label($"Next Detonation: ({nextPoint.x}, {nextPoint.y})", textStyle);
                    GUILayout.Label($"Area: {areaSize}", textStyle);
                    GUILayout.Label("Hold P to preview area", textStyle);
                }
                else
                {
                    GUILayout.Label("No pending detonations", textStyle);
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
                        if (tile != null && (tile.HasCharges || detonationManager.GetDetonationPoint(new Vector2Int(tile.x, tile.y)) ))
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
    private void DrawPlayerStatus()
    {
        // Player status panel
        GUILayout.BeginArea(new Rect(Screen.width - 300, 630, 290, 80));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("PLAYER STATUS", headerStyle);

        GUILayout.Space(5);

        if (FindObjectOfType<PlayerManager>() != null)
        {
            var player = FindObjectOfType<PlayerManager>();

            GUILayout.BeginVertical(boxStyle);

            if (player.IsAlive())
            {
                GUI.color = Color.green;
                GUILayout.Label("ALIVE", textStyle);
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("DEAD - Respawning...", textStyle);
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.Label("Player not found!", textStyle);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawPlayerStatistics()
    {
        // Player statistics panel
        GUILayout.BeginArea(new Rect(10, Screen.height - 250, 320, 240));

        // Panel background
        GUILayout.BeginVertical(panelStyle);

        // Header
        GUILayout.Label("PLAYER STATISTICS", headerStyle);

        GUILayout.Space(5);

        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            PlayerStatistics stats = player.GetCurrentStatistics();

            GUILayout.BeginVertical(boxStyle);

            // Cube statistics
            GUILayout.Label("CUBES:", GUI.skin.box);
            GUILayout.Label($"Normal Captured: {stats.normalCubesCaptured}", textStyle);
            GUILayout.Label($"Blue Captured: {stats.blueCubesCaptured}", textStyle);
            GUILayout.Label($"Black Captured: {stats.blackCubesCaptured}", textStyle);
            GUILayout.Label($"Escaped: {stats.cubesEscaped}", textStyle);
            GUILayout.Label($"Capture Rate: {stats.captureRate:P1}", textStyle);

            GUILayout.Space(3);

            // Action statistics  
            GUILayout.Label("ACTIONS:", GUI.skin.box);
            GUILayout.Label($"Markers Placed: {stats.markersPlaced}", textStyle);
            GUILayout.Label($"Markers Triggered: {stats.markersTriggered}", textStyle);
            GUILayout.Label($"Detonations: {stats.detonationsUsed}", textStyle);
            GUILayout.Label($"Moves: {stats.movesCount}", textStyle);

            GUILayout.Space(3);

            // Player statistics
            GUILayout.Label("PLAYER:", GUI.skin.box);
            GUILayout.Label($"Deaths: {stats.playerDeaths}", textStyle);
            GUILayout.Label($"Time Alive: {stats.timeAlive:F1}s", textStyle);
            GUILayout.Label($"Death Rate: {stats.deathRate:F2}/min", textStyle);

            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.Label("Player not found!", textStyle);
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

}