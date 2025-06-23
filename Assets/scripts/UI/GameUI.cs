using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    // No UI GameObject references needed - pure OnGUI implementation

    [Header("UI Settings")]
    [SerializeField] private bool showControlsAtStart = true;
    [SerializeField] private bool showTips = true;
    [SerializeField] private KeyCode toggleUIKey = KeyCode.Tab;

    // References
    private PlayerManager playerManager;
    private WaveManager waveManager;
    private PlayerActionManager playerActionManager;

    // UI state
    private bool controlsVisible = true;
    private bool tipsVisible = true;

    // Style caching
    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        FindReferences();
        InitializeUI();
    }

    private void Update()
    {
        HandleInput();
        UpdateDynamicInfo();
    }

    private void FindReferences()
    {
        playerManager = FindObjectOfType<PlayerManager>();
        waveManager = FindObjectOfType<WaveManager>();
        playerActionManager = FindObjectOfType<PlayerActionManager>();
    }

    private void InitializeUI()
    {
        controlsVisible = showControlsAtStart;
        tipsVisible = showTips;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleUIKey))
        {
            ToggleUI();
        }

        // Reset functionality
        if (Input.GetKeyDown(KeyCode.P))
        {
            RestartLevel();
        }

        // Exit functionality
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }

    private void RestartLevel()
    {
        var stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            stageManager.RestartCurrentStage();
        }
        else
        {
            // Fallback: reload current scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void ToggleUI()
    {
        controlsVisible = !controlsVisible;
        tipsVisible = controlsVisible; // Link them together
    }

    private void UpdateDynamicInfo()
    {
        // This method can be used for any real-time calculations
        // All display happens in OnGUI now
    }

    private void OnGUI()
    {
        InitializeStyles();

        if (controlsVisible)
        {
            DrawControlsPanel();
        }

        if (tipsVisible)
        {
            DrawTipsPanel();
        }

        //DrawStatusInfo();
        DrawToggleHint();
    }

    private void InitializeStyles()
    {
        if (boxStyle == null)
        {
            // Clean, modern box style
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f));
            boxStyle.padding = new RectOffset(15, 15, 15, 15);
            boxStyle.margin = new RectOffset(10, 10, 10, 10);

            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;

            // Text style
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.normal.textColor = Color.white;
            textStyle.fontSize = 13;
            textStyle.wordWrap = true;

            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
        }
    }

    private void DrawControlsPanel()
    {
        // Position on bottom-left
        Rect controlsRect = new Rect(20, Screen.height - 300, 300, 270);
        GUILayout.BeginArea(controlsRect);
        GUILayout.BeginVertical(boxStyle);

        // Header
        GUILayout.Label("CONTROLS", headerStyle);
        GUILayout.Space(8);

        // Essential controls
        GUILayout.Label("WASD  Move Player", textStyle);
        GUILayout.Label("F      Light Marker", textStyle);
        GUILayout.Label("V      Heavy Marker", textStyle);
        GUILayout.Label("G      Prime Marker", textStyle);
        GUILayout.Label("R      Trigger Light", textStyle);
        GUILayout.Label("Y      Trigger Heavy", textStyle);
        GUILayout.Label("T      Trigger Prime", textStyle);
        GUILayout.Label("Q      Trigger Cube Marker", textStyle);
        GUILayout.Label("E      Power Up Cube Marker", textStyle);
        GUILayout.Label("K      Close Dialog", textStyle);

        GUILayout.Space(5);

        // System controls
        GUILayout.Label("P      Restart Level", textStyle);
        GUILayout.Label("ESC    Quit Game", textStyle);

        GUILayout.Space(5);

        // Current marker count if available
        if (playerActionManager != null)
        {
            int cubeMarkers = playerActionManager.GetCurrentCubeMarkers();
            if (cubeMarkers > 0)
            {
                GUILayout.Label($"Detonations Ready: {cubeMarkers}", textStyle);
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawTipsPanel()
    {
        // Position on bottom-right
        Rect tipsRect = new Rect(Screen.width - 320, Screen.height - 250, 300, 180);

        GUILayout.BeginArea(tipsRect);
        GUILayout.BeginVertical(boxStyle);

        // Header
        GUILayout.Label("QUICK TIPS", headerStyle);
        GUILayout.Space(8);

        // Helpful gameplay tips
        GUILayout.Label("• Place markers in cube paths", textStyle);
        GUILayout.Label("• Infinity cubes create detonations", textStyle);
        GUILayout.Label("• Avoid dense cubes!", textStyle);
        GUILayout.Label("• Heavy markers for dense cubes", textStyle);

        // Dynamic tip based on game state
        string dynamicTip = GetDynamicTip();
        if (!string.IsNullOrEmpty(dynamicTip))
        {
            GUILayout.Space(3);
            GUI.color = Color.yellow;
            GUILayout.Label($"💡 {dynamicTip}", textStyle);
            GUI.color = Color.white;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawStatusInfo()
    {
        // Top-left status display
        Rect statusRect = new Rect(20, 220, 250, 80);

        GUILayout.BeginArea(statusRect);
        GUILayout.BeginVertical(boxStyle);

        // Wave status
        if (waveManager != null)
        {
            if (waveManager.waveActive)
            {
                GUILayout.Label($"Wave Active - Step {waveManager.MoveStep}", textStyle);
            }
            else
            {
                GUILayout.Label("Press ENTER to start wave", textStyle);
            }
        }

        // Score
        if (playerManager != null)
        {
            var stats = playerManager.GetCurrentStatistics();
            GUILayout.Label($"Captured: {stats.TotalCubesCaptured} | Escaped: {stats.cubesEscaped}", textStyle);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawToggleHint()
    {
        // Small toggle hint in top-right
        Rect toggleRect = new Rect(Screen.width - 150, 10, 140, 30);

        GUILayout.BeginArea(toggleRect);

        // Semi-transparent background
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUILayout.Label($"TAB to {(controlsVisible ? "hide" : "show")} UI", textStyle);
        GUI.color = Color.white;

        GUILayout.EndArea();
    }

    private string GetDynamicTip()
    {
        if (playerManager == null) return "";

        // 1. Marker availability checks (highest priority)
        if (playerActionManager != null)
        {
            // Check if light markers are recharging
            if (playerActionManager.GetCurrentLightCharges() == 0)
            {
                return "Light markers recharging...";
            }

            // Suggest placing light markers if available
            if (playerActionManager.CanPlaceLightMarker())
            {
                return "Press F to place light marker";
            }
        }

        // 2. Cube proximity detection
        var gridManager = GridManager.Instance;
        if (gridManager != null && waveManager != null)
        {
            Vector2Int playerPos = playerManager.currentTilePosition;
            
            // Check for cubes within 2 tiles of player
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube != null && !cube.isDestroyed)
                {
                    Vector2Int cubePos = cube.position;
                    float distance = Vector2Int.Distance(playerPos, cubePos);
                    
                    if (distance <= 2f)
                    {
                        return "Cubes approaching! Place markers ahead";
                    }
                }
            }
        }

        // 3. Wave status guidance
        if (waveManager != null)
        {
            // If wave is not active but markers are placed, suggest starting
            if (!waveManager.waveActive)
            {
                // Check if any markers are placed
                if (playerActionManager != null && 
                    (playerActionManager.GetCurrentLightMarkers() > 0 || 
                     playerActionManager.GetCurrentHeavyMarkers() > 0 || 
                     playerActionManager.GetCurrentPrimeMarkers() > 0))
                {
                    return "Press ENTER to start wave";
                }
            }

            // Check for cube markers (from captured prime cubes)
            if (playerActionManager != null)
            {
                int cubeMarkers = playerActionManager.GetCurrentCubeMarkers();
                if (cubeMarkers > 0)
                {
                    return "Prime cubes give cube markers - press Q to detonate";
                }
            }
        }

        // 4. Wave start prompt (lower priority)
        if (waveManager != null && !waveManager.waveActive)
        {
            return "Press ENTER to start the wave";
        }

        // 5. Grid state warnings (lowest priority)
        if (gridManager != null)
        {
            int playableRows = gridManager.GetPlayableRowCount();
            int totalRows = gridManager.Height;

            if (playableRows < totalRows)
            {
                return $"Warning: {totalRows - playableRows} row(s) have fallen!";
            }

            if (playableRows <= 5)
            {
                return "Critical: Very few rows remain!";
            }
        }

        return "";
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

    // Public methods for external control
    public void ShowUI()
    {
        controlsVisible = true;
        tipsVisible = true;
    }

    public void HideUI()
    {
        controlsVisible = false;
        tipsVisible = false;
    }

    public void ShowControlsOnly()
    {
        controlsVisible = true;
        tipsVisible = false;
    }

    public void ShowTipsOnly()
    {
        controlsVisible = false;
        tipsVisible = true;
    }
}
