using UnityEngine;

public class SystemDebugPanel : IDebugPanel
{
    public string PanelName => "System";

    private GridManager gridManager;
    private PlayerActionManager playerActionManager;

    // Performance tracking
    private float lastFrameRate = 0f;
    private float frameRateUpdateTimer = 0f;
    private const float FRAMERATE_UPDATE_INTERVAL = 0.5f;

    public void Initialize()
    {
        gridManager = GridManager.Instance;
        playerActionManager = Object.FindObjectOfType<PlayerActionManager>();
    }

    public void Update()
    {
        // Update frame rate periodically
        frameRateUpdateTimer += Time.unscaledDeltaTime;
        if (frameRateUpdateTimer >= FRAMERATE_UPDATE_INTERVAL)
        {
            lastFrameRate = 1f / Time.unscaledDeltaTime;
            frameRateUpdateTimer = 0f;
        }
    }

    public void DrawPanel()
    {
        DrawSystemInfo();
        GUILayout.Space(10);
        DrawTimeControls();
        GUILayout.Space(10);
        DrawGridInfo();
        GUILayout.Space(10);
        DrawDetonationInfo();
        GUILayout.Space(10);
        DrawSystemActions();
    }

    private void DrawSystemInfo()
    {
        GUILayout.Label("SYSTEM INFO", GUI.skin.box);

        GUILayout.Label($"FPS: {lastFrameRate:F0}");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
        GUILayout.Label($"Frame Count: {Time.frameCount}");
        GUILayout.Label($"Real Time: {Time.realtimeSinceStartup:F1}s");
        GUILayout.Label($"Game Time: {Time.time:F1}s");
        GUILayout.Label($"Unity Version: {Application.unityVersion}");
        GUILayout.Label($"Platform: {Application.platform}");
    }

    private void DrawTimeControls()
    {
        GUILayout.Label("TIME CONTROLS", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause"))
            Time.timeScale = 0f;
        if (GUILayout.Button("Resume"))
            Time.timeScale = 1f;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.25x"))
            Time.timeScale = 0.25f;
        if (GUILayout.Button("0.5x"))
            Time.timeScale = 0.5f;
        if (GUILayout.Button("2x"))
            Time.timeScale = 2f;
        if (GUILayout.Button("4x"))
            Time.timeScale = 4f;
        GUILayout.EndHorizontal();

        GUILayout.Label($"Current: {Time.timeScale:F2}x");
    }

    private void DrawGridInfo()
    {
        GUILayout.Label("GRID INFO", GUI.skin.box);

        if (gridManager != null)
        {
            GUILayout.Label($"Size: {gridManager.Width}x{gridManager.Height}");
            GUILayout.Label($"Tile Size: {gridManager.TileSize}");
            GUILayout.Label($"Ready: {gridManager.IsGridReady}");
            GUILayout.Label($"Markers: {gridManager.GetMarkerCount()}");
            GUILayout.Label($"Center: {gridManager.GridCenter}");
            GUILayout.Label($"Bounds: {gridManager.MinWorldBounds} to {gridManager.MaxWorldBounds}");

            if (GUILayout.Button("Print Grid Debug Info"))
            {
                gridManager.DebugPrintGridInfo();
            }
        }
        else
        {
            GUILayout.Label("GridManager not found");
        }
    }

    private void DrawDetonationInfo()
    {
        GUILayout.Label("DETONATION INFO", GUI.skin.box);

        if (playerActionManager != null)
        {
            GUILayout.Label($"Detonation Points: {playerActionManager.CubeMarkerCount}");
            GUILayout.Label($"Has Points: {playerActionManager.HasCubeMarkers()}");

            Vector2Int nextPoint = playerActionManager.GetNextCubeMarker();
            if (nextPoint.x >= 0)
            {
                GUILayout.Label($"Next Point: ({nextPoint.x}, {nextPoint.y})");
            }
            else
            {
                GUILayout.Label("Next Point: None");
            }

            if (GUILayout.Button("Clear All Detonations"))
            {
                playerActionManager.ClearAllActions();
            }
        }
        else
        {
            GUILayout.Label("DetonationManager not found");
        }
    }

    private void DrawSystemActions()
    {
        GUILayout.Label("SYSTEM ACTIONS", GUI.skin.box);

        if (GUILayout.Button("Clear All Markers"))
        {
            gridManager?.ClearAllMarkers();
        }

        if (GUILayout.Button("Clear All Detonations"))
        {
            playerActionManager?.ClearAllActions();
        }

        if (GUILayout.Button("Force Garbage Collection"))
        {
            System.GC.Collect();
        }

        if (GUILayout.Button("Log System State"))
        {
            Debug.Log("=== SYSTEM STATE DEBUG ===");
            Debug.Log($"FPS: {lastFrameRate:F1}");
            Debug.Log($"Time Scale: {Time.timeScale}");
            if (gridManager != null)
                Debug.Log($"Grid: {gridManager.Width}x{gridManager.Height}, Ready: {gridManager.IsGridReady}");
            Debug.Log("=== END SYSTEM STATE ===");
        }
    }
}
