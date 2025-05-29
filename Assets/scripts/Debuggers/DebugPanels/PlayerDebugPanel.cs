using UnityEngine;

public class PlayerDebugPanel : IDebugPanel
{
    public string PanelName => "Player";

    private PlayerManager playerManager;

    public void Initialize()
    {
        playerManager = Object.FindObjectOfType<PlayerManager>();
    }

    public void Update()
    {
        // No specific update logic needed
    }

    public void DrawPanel()
    {
        DrawPlayerInfo();
        GUILayout.Space(10);
        DrawPlayerControls();
        GUILayout.Space(10);
        DrawPlayerStats();
        GUILayout.Space(10);
        DrawPlayerSettings();
    }

    private void DrawPlayerInfo()
    {
        GUILayout.Label("PLAYER INFO", GUI.skin.box);

        if (playerManager != null)
        {
            GUILayout.Label($"Position: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})");
            GUILayout.Label($"World Pos: {playerManager.transform.position}");
            GUILayout.Label($"Alive: {playerManager.IsAlive()}");
            GUILayout.Label($"Deaths: {playerManager.playerDeaths}");
            GUILayout.Label($"Dead: {playerManager.isDead}");
            GUILayout.Label($"Time Alive: {playerManager.timeAlive:F1}s");
            GUILayout.Label($"Total Play Time: {playerManager.totalPlayTime:F1}s");
        }
        else
        {
            GUILayout.Label("PlayerManager not found");
        }
    }

    private void DrawPlayerControls()
    {
        GUILayout.Label("PLAYER CONTROLS", GUI.skin.box);

        if (playerManager == null) return;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill Player"))
            playerManager.Kill();
        if (GUILayout.Button("Reset Statistics"))
            playerManager.ResetStatistics();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Markers"))
            playerManager.ResetMarkers();
        if (GUILayout.Button("Max Markers"))
            playerManager.SetMaxMarkers(10);
        GUILayout.EndHorizontal();

        // Position controls
        GUILayout.Label("Set Position:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("(0,0)", GUILayout.Width(50)))
            playerManager.SetPosition(0, 0);
        if (GUILayout.Button("Center", GUILayout.Width(50)))
        {
            var grid = GridManager.Instance;
            if (grid != null)
                playerManager.SetPosition(grid.Width / 2, grid.Height / 2);
        }
        if (GUILayout.Button("Bottom", GUILayout.Width(50)))
        {
            var grid = GridManager.Instance;
            if (grid != null)
                playerManager.SetPosition(grid.Width / 2, 0);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawPlayerStats()
    {
        GUILayout.Label("PLAYER STATISTICS", GUI.skin.box);

        if (playerManager == null) return;

        // Cube statistics
        GUILayout.Label("Cubes:");
        GUILayout.Label($"  Normal Captured: {playerManager.normalCubesCaptured}");
        GUILayout.Label($"  Blue Captured: {playerManager.blueCubesCaptured}");
        GUILayout.Label($"  Black Captured: {playerManager.blackCubesCaptured}");
        GUILayout.Label($"  Escaped: {playerManager.cubesEscaped}");

        int totalCaptured = playerManager.normalCubesCaptured + playerManager.blueCubesCaptured + playerManager.blackCubesCaptured;
        GUILayout.Label($"  Total Captured: {totalCaptured}");

        // Action statistics
        GUILayout.Label("Actions:");
        GUILayout.Label($"  Markers Placed: {playerManager.markersPlaced}");
        GUILayout.Label($"  Markers Triggered: {playerManager.markersTriggered}");
        GUILayout.Label($"  Detonations Used: {playerManager.detonationsUsed}");
        GUILayout.Label($"  Moves Count: {playerManager.movesCount}");

        // Tile statistics
        GUILayout.Label("Tiles:");
        GUILayout.Label($"  Corrupted: {playerManager.tilesCorrupted}");
        GUILayout.Label($"  Primed: {playerManager.tilesPrimed}");
        GUILayout.Label($"  Enhanced: {playerManager.tilesEnhanced}");
    }

    private void DrawPlayerSettings()
    {
        GUILayout.Label("PLAYER SETTINGS", GUI.skin.box);

        if (playerManager == null) return;

        GUILayout.Label($"Max Marker Charge: {playerManager.maxMarkerCharge}");
        GUILayout.Label($"Max Marker Count: {playerManager.maxMarkerCount}");
        GUILayout.Label($"Acceleration: {playerManager.acceleration}");
        GUILayout.Label($"Deceleration: {playerManager.deceleration}");
        GUILayout.Label($"Respawn Delay: {playerManager.respawnDelay}s");
        GUILayout.Label($"Invulnerability Time: {playerManager.respawnInvulnerabilityTime}s");

        // Debug toggles
        GUILayout.BeginHorizontal();
        GUILayout.Label("Debug Logs:");
        playerManager.enableDebugLogs = GUILayout.Toggle(playerManager.enableDebugLogs, "");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Show Tile Info:");
        playerManager.showTileInfo = GUILayout.Toggle(playerManager.showTileInfo, "");
        GUILayout.EndHorizontal();
    }
}