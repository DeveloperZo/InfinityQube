using UnityEngine;
using TMPro;
using static Enumerations;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    // No UI GameObject references needed - pure OnGUI implementation

    [Header("UI Settings")]
    [SerializeField] private bool showControlsAtStart = true;
    [SerializeField] private bool showTips = true;
    [SerializeField] private KeyCode toggleUIKey = KeyCode.Tab;
    [SerializeField] private Image[] waveIcons;

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
        Debug.Log("GameUI: Exit game requested");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
        
        // Force quit if Application.Quit() fails (backup)
        if (Application.isPlaying)
        {
            Debug.LogWarning("GameUI: Force quitting application");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
#endif
    }

    public void ToggleWaveIcon(int waveIndex, bool enable)
    {
        if (waveIndex > waveIcons.Length) return;

        
        waveIcons[waveIndex].color = enable ? Color.white : Color.black;
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
            boxStyle.alignment = TextAnchor.UpperLeft;

            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.clipping = TextClipping.Overflow;
            headerStyle.wordWrap = false;

            // Text style
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.normal.textColor = Color.white;
            textStyle.fontSize = 13;
            textStyle.wordWrap = true;
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.clipping = TextClipping.Overflow;
            textStyle.padding = new RectOffset(0, 0, 2, 2);

            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void DrawControlsPanel()
    {
        // Position on bottom-left with proper scaling
        float scaleFactor = Screen.width / 1920f; // Normalize to 1920p
        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f); // Reasonable limits
        
        int width = Mathf.RoundToInt(300 * scaleFactor);
        int height = Mathf.RoundToInt(270 * scaleFactor);
        int margin = Mathf.RoundToInt(20 * scaleFactor);
        
        Rect controlsRect = new Rect(margin, Screen.height - height - margin, width, height);
        GUILayout.BeginArea(controlsRect);
        GUILayout.BeginVertical(boxStyle);

        // Header
        GUILayout.Label("CONTROLS", headerStyle);
        GUILayout.Space(8);

        // Essential controls
        GUILayout.Label("K      Skip/Close Dialog", textStyle);
        GUILayout.Label("WASD  Move Player", textStyle);
        GUILayout.Label("1/2      Change Mode", textStyle);
        GUILayout.Label("F      Place Marker", textStyle);
        GUILayout.Label("R      Trigger Marker", textStyle);

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
        // Position on bottom-right with proper scaling
        float scaleFactor = Screen.width / 1920f; // Normalize to 1920p
        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f); // Reasonable limits
        
        int width = Mathf.RoundToInt(300 * scaleFactor);
        int height = Mathf.RoundToInt(180 * scaleFactor);
        int margin = Mathf.RoundToInt(20 * scaleFactor);
        
        Rect tipsRect = new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height);

        GUILayout.BeginArea(tipsRect);
        GUILayout.BeginVertical(boxStyle);

        // Header
        GUILayout.Label("QUICK TIPS", headerStyle);
        GUILayout.Space(8);

        // Helpful gameplay tips
        GUILayout.Label("• Place markers in cube paths", textStyle);
        GUILayout.Label("• Infinity cubes create detonations", textStyle);
        GUILayout.Label("• Avoid recursion cubes!", textStyle);
        GUILayout.Label("• Recursion markers for recursion cubes", textStyle);

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
        // Small toggle hint in top-right with proper scaling
        float scaleFactor = Screen.width / 1920f; // Normalize to 1920p
        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f); // Reasonable limits
        
        int width = Mathf.RoundToInt(140 * scaleFactor);
        int height = Mathf.RoundToInt(30 * scaleFactor);
        
        Rect toggleRect = new Rect(Screen.width - width - 10, 10, width, height);

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

        // Use TutorialMessageManager for enhanced dynamic content if available
        var tutorialManager = TutorialMessageManager.Instance;
        if (tutorialManager != null)
        {
            string enhancedTip = GetEnhancedDynamicTip(tutorialManager);
            if (!string.IsNullOrEmpty(enhancedTip))
                return enhancedTip;
        }

        // Fallback to original dynamic tip logic
        // 1. Marker availability checks (highest priority)
        if (playerActionManager != null)
        {
            // Check if unit markers are recharging
            if (playerActionManager.GetCurrentUnitCharges() == 0)
            {
                return "Unit markers recharging...";
            }

            // Suggest placing unit markers if available
            if (playerActionManager.CanPlaceUnitMarker())
            {
                return "Press F to place unit marker";
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
                    (playerActionManager.GetCurrentUnitMarkers() > 0 || 
                     playerActionManager.GetCurrentRecursionMarkers() > 0 || 
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

    /// <summary>
    /// Get enhanced dynamic tip using TutorialMessageManager formatting
    /// </summary>
    private string GetEnhancedDynamicTip(TutorialMessageManager tutorialManager)
    {
        // Build context-aware tip messages using the new formatting system
        var context = tutorialManager.GetCurrentContext();
        
        // Priority-based tip generation with dynamic variables
        
        // 1. Immediate danger/urgency (highest priority)
        if (context.nearestCubeDistance <= 2f)
        {
            string urgentTip = "Move quickly! Cube {cubeDistance:F1} tiles away";
            return tutorialManager.FormatMessageText(urgentTip);
        }
        
        // 2. Marker availability with action guidance
        if (playerActionManager != null)
        {
            if (playerActionManager.GetCurrentUnitCharges() == 0)
            {
                string rechargeTip = "Wait for markers to recharge ({markers} available)";
                return tutorialManager.FormatMessageText(rechargeTip);
            }
            
            if (playerActionManager.CanPlaceUnitMarker())
            {
                string placeTip = "Place unit marker at ({playerX},{playerY}) with F key";
                return tutorialManager.FormatMessageText(placeTip);
            }
        }
        
        // 3. Wave state guidance
        if (waveManager != null)
        {
            if (!waveManager.waveActive && context.availableMarkers > 0)
            {
                string startTip = "Start wave with ENTER - {markers} markers ready";
                return tutorialManager.FormatMessageText(startTip);
            }
            
            if (waveManager.waveActive && context.currentMoveStep > 0)
            {
                string progressTip = "Wave step {step} - stay alert for cubes";
                return tutorialManager.FormatMessageText(progressTip);
            }
        }
        
        // 4. Cube type specific guidance
        if (context.activeCubeTypes.Count > 0)
        {
            if (context.activeCubeTypes.Contains(CubeType.Recursion))
            {
                string recursionTip = "Avoid recursion cubes! Use recursion markers";
                return tutorialManager.FormatMessageText(recursionTip);
            }
            
            if (context.activeCubeTypes.Contains(CubeType.Infinity))
            {
                string infinityTip = "Target infinity cubes for detonations";
                return tutorialManager.FormatMessageText(infinityTip);
            }
        }
        
        // 5. General guidance based on experience
        if (context.currentMoveStep == 0)
        {
            string readyTip = "Press ENTER when ready to start";
            return tutorialManager.FormatMessageText(readyTip);
        }
        
        return string.Empty;
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
