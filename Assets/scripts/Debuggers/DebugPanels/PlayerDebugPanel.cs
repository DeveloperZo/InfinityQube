using UnityEngine;

public class PlayerDebugPanel : DebugPanelBase
{
    public string PanelName => "Player Editor";
    public override DebugPanelGroup PanelGroup => DebugPanelGroup.Player;

    private PlayerManager playerManager;
    private GridManager gridManager;

    // UI State
    private Vector2 scrollPosition;
    private bool showPlayerInfo = true;
    private bool showPlayerControls = true;
    private bool showOverrides = true;
    private bool showStatistics = true;
    private bool showSettings = false;

    // Override States
    private bool overridePlayerDeath = false;
    private bool overrideMarkerLimits = false;
    private bool overrideMovement = false;
    private bool overrideInvulnerability = false;

    // Override Values
    private int overriddenMaxMarkers = 10;
    private int overriddenCurrentMarkers = 0;
    private float overriddenMoveSpeed = 15f;
    private bool forcedInvulnerability = false;
    private float invulnerabilityTimeOverride = 5f;

    // Position Controls
    private int targetPositionX = 0;
    private int targetPositionY = 0;
    private bool showPositionGrid = false;

    // Statistics Modification
    private bool showStatModifiers = false;
    private int addNormalCubes = 0;
    private int addBlueCubes = 0;
    private int addBlackCubes = 0;
    private int addEscapedCubes = 0;
    private int addDeaths = 0;

    public void Initialize()
    {
        playerManager = Object.FindObjectOfType<PlayerManager>();
        gridManager = GridManager.Instance;

        if (playerManager != null)
        {
            // Initialize with current values
            targetPositionX = playerManager.currentTilePosition.x;
            targetPositionY = playerManager.currentTilePosition.y;
        }
    }

    public void Update()
    {
        // Apply overrides continuously
        ApplyActiveOverrides();
    }

    public void DrawPanel()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawPanelTabs();
        GUILayout.Space(5);

        if (showPlayerInfo)
            DrawPlayerInfoSection();

        if (showPlayerControls)
            DrawPlayerControlsSection();

        if (showOverrides)
            DrawOverridesSection();

        if (showStatistics)
            DrawStatisticsSection();

        if (showSettings)
            DrawSettingsSection();

        GUILayout.EndScrollView();
    }

    private void DrawPanelTabs()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = showPlayerInfo ? Color.cyan : Color.white;
        if (GUILayout.Button("Info", GUILayout.Height(25)))
            showPlayerInfo = !showPlayerInfo;

        GUI.backgroundColor = showPlayerControls ? Color.cyan : Color.white;
        if (GUILayout.Button("Controls", GUILayout.Height(25)))
            showPlayerControls = !showPlayerControls;

        GUI.backgroundColor = showOverrides ? Color.cyan : Color.white;
        if (GUILayout.Button("Overrides", GUILayout.Height(25)))
            showOverrides = !showOverrides;

        GUI.backgroundColor = showStatistics ? Color.cyan : Color.white;
        if (GUILayout.Button("Stats", GUILayout.Height(25)))
            showStatistics = !showStatistics;

        GUI.backgroundColor = showSettings ? Color.cyan : Color.white;
        if (GUILayout.Button("Settings", GUILayout.Height(25)))
            showSettings = !showSettings;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawPlayerInfoSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER INFORMATION", GUI.skin.box);

        if (playerManager != null)
        {
            // Basic state
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Position: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})");
            GUILayout.Label($"World: {playerManager.transform.position:F1}");
            GUILayout.EndHorizontal();

            // Status indicators
            GUILayout.BeginHorizontal();
            GUI.color = playerManager.IsAlive() ? Color.green : Color.red;
            GUILayout.Label($"Status: {(playerManager.IsAlive() ? "ALIVE" : "DEAD")}");
            GUI.color = Color.white;
            GUILayout.Label($"Deaths: {playerManager.playerDeaths}");
            GUILayout.EndHorizontal();

            // Time information
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Time Alive: {playerManager.timeAlive:F1}s");
            GUILayout.Label($"Total Play Time: {playerManager.totalPlayTime:F1}s");
            GUILayout.EndHorizontal();

            // Movement state
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Acceleration: {playerManager.acceleration:F1}");
            GUILayout.Label($"Deceleration: {playerManager.deceleration:F1}");
            GUILayout.EndHorizontal();

            // Marker information
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Max Markers: {playerManager.maxMarkerCharge}");
            GUILayout.Label($"Max Count: {playerManager.maxMarkerCount}");
            GUILayout.EndHorizontal();

            // Current tile info
            if (gridManager != null && gridManager.IsValidGridPosition(playerManager.currentTilePosition))
            {
                var tile = gridManager.GetTileAt(playerManager.currentTilePosition);
                if (tile != null)
                {
                    GUILayout.Label("Current Tile:");
                    GUILayout.Label($"  Has Marker: {tile.HasMarker}");
                    GUILayout.Label($"  Is Blackened: {tile.IsBlackened}");
                    GUILayout.Label($"  Is Primed: {tile.IsPrimed}");
                    GUILayout.Label($"  Charges: {tile.DetonationCharges}");
                }
            }
        }
        else
        {
            GUILayout.Label("PlayerManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawPlayerControlsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER CONTROLS", GUI.skin.box);

        if (playerManager == null)
        {
            GUILayout.Label("PlayerManager not found");
            return;
        }

        // Life controls
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Kill Player"))
            playerManager.Kill();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Revive Player"))
        {
            if (playerManager.isDead)
            {
                // Force revive by setting isDead to false and re-enabling
                playerManager.isDead = false;
                playerManager.enabled = true;
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Position controls
        GUILayout.Label("Position Controls:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("X:", GUILayout.Width(20));
        string xStr = GUILayout.TextField(targetPositionX.ToString(), GUILayout.Width(40));
        if (int.TryParse(xStr, out int newX))
            targetPositionX = Mathf.Clamp(newX, 0, gridManager?.Width - 1 ?? 10);

        GUILayout.Label("Y:", GUILayout.Width(20));
        string yStr = GUILayout.TextField(targetPositionY.ToString(), GUILayout.Width(40));
        if (int.TryParse(yStr, out int newY))
            targetPositionY = Mathf.Clamp(newY, 0, gridManager?.Height - 1 ?? 20);

        if (GUILayout.Button("Set Position"))
        {
            playerManager.SetPosition(targetPositionX, targetPositionY);
        }
        GUILayout.EndHorizontal();

        // Quick position buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Bottom Left"))
        {
            playerManager.SetPosition(0, 0);
            targetPositionX = 0; targetPositionY = 0;
        }
        if (GUILayout.Button("Bottom Center"))
        {
            int centerX = gridManager?.Width / 2 ?? 3;
            playerManager.SetPosition(centerX, 0);
            targetPositionX = centerX; targetPositionY = 0;
        }
        if (GUILayout.Button("Center"))
        {
            int centerX = gridManager?.Width / 2 ?? 3;
            int centerY = gridManager?.Height / 2 ?? 10;
            playerManager.SetPosition(centerX, centerY);
            targetPositionX = centerX; targetPositionY = centerY;
        }
        GUILayout.EndHorizontal();

        // Visual position grid toggle
        showPositionGrid = GUILayout.Toggle(showPositionGrid, "Show Position Grid");
        if (showPositionGrid)
        {
            DrawPositionGrid();
        }

        // Marker controls
        GUILayout.Label("Marker Controls:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Markers"))
            playerManager.ResetMarkers();
        if (GUILayout.Button("Set Max Markers (10)"))
            playerManager.SetMaxMarkers(10);
        if (GUILayout.Button("Set Max Markers (99)"))
            playerManager.SetMaxMarkers(99);
        GUILayout.EndHorizontal();

        // Statistics controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Statistics"))
            playerManager.ResetStatistics();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawOverridesSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER OVERRIDES", GUI.skin.box);

        if (playerManager == null)
        {
            GUILayout.Label("PlayerManager not found");
            return;
        }

        // Death override
        GUILayout.BeginHorizontal();
        bool newDeathOverride = GUILayout.Toggle(overridePlayerDeath, "Override Player Death (Invulnerable)");
        if (newDeathOverride != overridePlayerDeath)
        {
            overridePlayerDeath = newDeathOverride;
            if (overridePlayerDeath)
            {
                Debug.Log("Player death override enabled - player is now invulnerable");
            }
        }
        GUILayout.EndHorizontal();

        // Marker limit override
        GUILayout.BeginHorizontal();
        overrideMarkerLimits = GUILayout.Toggle(overrideMarkerLimits, "Override Marker Limits");
        GUILayout.EndHorizontal();

        if (overrideMarkerLimits)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Markers:", GUILayout.Width(80));
            string maxStr = GUILayout.TextField(overriddenMaxMarkers.ToString(), GUILayout.Width(60));
            if (int.TryParse(maxStr, out int newMax))
                overriddenMaxMarkers = Mathf.Clamp(newMax, 1, 999);

            if (GUILayout.Button("Apply"))
            {
                playerManager.SetMaxMarkers(overriddenMaxMarkers);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Count:", GUILayout.Width(80));
            string currentStr = GUILayout.TextField(overriddenCurrentMarkers.ToString(), GUILayout.Width(60));
            if (int.TryParse(currentStr, out int newCurrent))
                overriddenCurrentMarkers = Mathf.Clamp(newCurrent, 0, overriddenMaxMarkers);

            if (GUILayout.Button("Set"))
            {
                // Would need to modify PlayerManager to support setting current marker count
                Debug.Log($"Setting current markers to {overriddenCurrentMarkers} (requires PlayerManager update)");
            }
            GUILayout.EndHorizontal();
        }

        // Movement override
        GUILayout.BeginHorizontal();
        overrideMovement = GUILayout.Toggle(overrideMovement, "Override Movement Speed");
        GUILayout.EndHorizontal();

        if (overrideMovement)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", GUILayout.Width(50));
            string speedStr = GUILayout.TextField(overriddenMoveSpeed.ToString("F1"), GUILayout.Width(60));
            if (float.TryParse(speedStr, out float newSpeed))
                overriddenMoveSpeed = Mathf.Clamp(newSpeed, 1f, 50f);
            GUILayout.EndHorizontal();
        }

        // Invulnerability override
        GUILayout.BeginHorizontal();
        overrideInvulnerability = GUILayout.Toggle(overrideInvulnerability, "Force Invulnerability Time");
        GUILayout.EndHorizontal();

        if (overrideInvulnerability)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Time:", GUILayout.Width(50));
            string invulnStr = GUILayout.TextField(invulnerabilityTimeOverride.ToString("F1"), GUILayout.Width(60));
            if (float.TryParse(invulnStr, out float newTime))
                invulnerabilityTimeOverride = Mathf.Clamp(newTime, 0f, 60f);
            GUILayout.Label("seconds");
            GUILayout.EndHorizontal();
        }

        // Override status display
        if (overridePlayerDeath || overrideMarkerLimits || overrideMovement || overrideInvulnerability)
        {
            GUILayout.Space(5);
            GUI.backgroundColor = Color.yellow;
            GUILayout.Label("ACTIVE OVERRIDES:", GUI.skin.box);
            if (overridePlayerDeath) GUILayout.Label(" Death Protection");
            if (overrideMarkerLimits) GUILayout.Label($" Marker Limits ({overriddenMaxMarkers})");
            if (overrideMovement) GUILayout.Label($" Movement Speed ({overriddenMoveSpeed:F1})");
            if (overrideInvulnerability) GUILayout.Label($" Invulnerability ({invulnerabilityTimeOverride:F1}s)");
            GUI.backgroundColor = Color.white;
        }

        GUILayout.EndVertical();
    }

    private void DrawStatisticsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER STATISTICS", GUI.skin.box);

        if (playerManager == null)
        {
            GUILayout.Label("PlayerManager not found");
            return;
        }

        // Current statistics display
        GUILayout.Label("Current Statistics:");
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Cubes Captured:");
        GUILayout.Label($"  Normal: {playerManager.normalCubesCaptured}");
        GUILayout.Label($"  Blue: {playerManager.blueCubesCaptured}");
        GUILayout.Label($"  Black: {playerManager.blackCubesCaptured}");
        GUILayout.Label($"  Escaped: {playerManager.cubesEscaped}");
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Actions:");
        GUILayout.Label($"  Markers Placed: {playerManager.markersPlaced}");
        GUILayout.Label($"  Markers Triggered: {playerManager.markersTriggered}");
        GUILayout.Label($"  Detonations: {playerManager.detonationsUsed}");
        GUILayout.Label($"  Moves: {playerManager.movesCount}");
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Tiles:");
        GUILayout.Label($"  Corrupted: {playerManager.tilesCorrupted}");
        GUILayout.Label($"  Primed: {playerManager.tilesPrimed}");
        GUILayout.Label($"  Enhanced: {playerManager.tilesEnhanced}");
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Player:");
        GUILayout.Label($"  Deaths: {playerManager.playerDeaths}");
        GUILayout.Label($"  Time Alive: {playerManager.timeAlive:F1}s");
        GUILayout.Label($"  Total Time: {playerManager.totalPlayTime:F1}s");
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Statistics modification
        showStatModifiers = GUILayout.Toggle(showStatModifiers, "Show Stat Modifiers");
        if (showStatModifiers)
        {
            DrawStatisticsModifiers();
        }

        GUILayout.EndVertical();
    }

    private void DrawStatisticsModifiers()
    {
        GUILayout.Label("Add to Statistics:");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Normal Cubes:", GUILayout.Width(100));
        string normalStr = GUILayout.TextField(addNormalCubes.ToString(), GUILayout.Width(50));
        if (int.TryParse(normalStr, out int newNormal))
            addNormalCubes = newNormal;
        if (GUILayout.Button("Add"))
        {
            playerManager.normalCubesCaptured += addNormalCubes;
            addNormalCubes = 0;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Blue Cubes:", GUILayout.Width(100));
        string blueStr = GUILayout.TextField(addBlueCubes.ToString(), GUILayout.Width(50));
        if (int.TryParse(blueStr, out int newBlue))
            addBlueCubes = newBlue;
        if (GUILayout.Button("Add"))
        {
            playerManager.blueCubesCaptured += addBlueCubes;
            addBlueCubes = 0;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Black Cubes:", GUILayout.Width(100));
        string blackStr = GUILayout.TextField(addBlackCubes.ToString(), GUILayout.Width(50));
        if (int.TryParse(blackStr, out int newBlack))
            addBlackCubes = newBlack;
        if (GUILayout.Button("Add"))
        {
            playerManager.blackCubesCaptured += addBlackCubes;
            addBlackCubes = 0;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Escaped:", GUILayout.Width(100));
        string escapedStr = GUILayout.TextField(addEscapedCubes.ToString(), GUILayout.Width(50));
        if (int.TryParse(escapedStr, out int newEscaped))
            addEscapedCubes = newEscaped;
        if (GUILayout.Button("Add"))
        {
            playerManager.cubesEscaped += addEscapedCubes;
            addEscapedCubes = 0;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Deaths:", GUILayout.Width(100));
        string deathsStr = GUILayout.TextField(addDeaths.ToString(), GUILayout.Width(50));
        if (int.TryParse(deathsStr, out int newDeaths))
            addDeaths = newDeaths;
        if (GUILayout.Button("Add"))
        {
            playerManager.playerDeaths += addDeaths;
            addDeaths = 0;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSettingsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER SETTINGS", GUI.skin.box);

        if (playerManager == null)
        {
            GUILayout.Label("PlayerManager not found");
            return;
        }

        // Debug toggles
        GUILayout.BeginHorizontal();
        GUILayout.Label("Enable Debug Logs:");
        playerManager.enableDebugLogs = GUILayout.Toggle(playerManager.enableDebugLogs, "");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Show Tile Info:");
        playerManager.showTileInfo = GUILayout.Toggle(playerManager.showTileInfo, "");
        GUILayout.EndHorizontal();

        // Physics settings
        GUILayout.Label("Movement Settings:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Acceleration:", GUILayout.Width(100));
        string accelStr = GUILayout.TextField(playerManager.acceleration.ToString("F1"), GUILayout.Width(60));
        if (float.TryParse(accelStr, out float newAccel))
            playerManager.acceleration = Mathf.Clamp(newAccel, 1f, 50f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Deceleration:", GUILayout.Width(100));
        string decelStr = GUILayout.TextField(playerManager.deceleration.ToString("F1"), GUILayout.Width(60));
        if (float.TryParse(decelStr, out float newDecel))
            playerManager.deceleration = Mathf.Clamp(newDecel, 1f, 50f);
        GUILayout.EndHorizontal();

        // Respawn settings
        GUILayout.Label("Respawn Settings:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Respawn Delay:", GUILayout.Width(100));
        string respawnStr = GUILayout.TextField(playerManager.respawnDelay.ToString("F1"), GUILayout.Width(60));
        if (float.TryParse(respawnStr, out float newRespawn))
            playerManager.respawnDelay = Mathf.Clamp(newRespawn, 0f, 10f);
        GUILayout.Label("s");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Invulnerability:", GUILayout.Width(100));
        string invulnStr = GUILayout.TextField(playerManager.respawnInvulnerabilityTime.ToString("F1"), GUILayout.Width(60));
        if (float.TryParse(invulnStr, out float newInvuln))
            playerManager.respawnInvulnerabilityTime = Mathf.Clamp(newInvuln, 0f, 60f);
        GUILayout.Label("s");
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawPositionGrid()
    {
        if (gridManager == null) return;

        GUILayout.Label("Position Grid (click to teleport):");

        // Show a small section of the grid
        int displayWidth = Mathf.Min(8, gridManager.Width);
        int displayHeight = Mathf.Min(6, gridManager.Height);

        for (int y = displayHeight - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < displayWidth; x++)
            {
                bool isPlayerPos = (x == playerManager.currentTilePosition.x && y == playerManager.currentTilePosition.y);
                GUI.backgroundColor = isPlayerPos ? Color.green : Color.white;

                if (GUILayout.Button($"{x},{y}", GUILayout.Width(40), GUILayout.Height(25)))
                {
                    playerManager.SetPosition(x, y);
                    targetPositionX = x;
                    targetPositionY = y;
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }

    private void ApplyActiveOverrides()
    {
        if (playerManager == null) return;

        // Apply movement speed override
        if (overrideMovement)
        {
            playerManager.acceleration = overriddenMoveSpeed;
            playerManager.deceleration = overriddenMoveSpeed * 1.33f;
        }

        // Apply invulnerability override
        if (overrideInvulnerability)
        {
            playerManager.respawnInvulnerabilityTime = invulnerabilityTimeOverride;
        }

        // Death override is handled in the CheckForCollisions override below
    }

    // This would require adding a method to PlayerManager to check overrides
    public bool ShouldPreventDeath()
    {
        return overridePlayerDeath;
    }
}