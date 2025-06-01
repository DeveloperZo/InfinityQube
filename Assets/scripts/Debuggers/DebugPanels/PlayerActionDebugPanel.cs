using UnityEngine;
using static Enumerations;


public class PlayerActionDebugPanel : IDebugPanel
{
    public string PanelName => "Player Actions";

    private PlayerActionManager actionManager;
    private PlayerManager playerManager;
    private GridManager gridManager;
    private WaveManager waveManager;

    // UI State
    private bool showActionControls = true;
    private bool showCubeSpawner = true;
    private bool showTileEditor = true;
    private bool showActionInfo = true;

    // Cube Spawner
    private int cubeSpawnRow = 15;
    private int selectedCubeType = 1;
    private bool showCubeGrid = true;
    private Vector2 cubeGridScrollPosition = Vector2.zero;

    // Tile Editor
    private int selectedTileState = 0;
    private bool showTileGrid = false;
    private Vector2 tileGridScrollPosition = Vector2.zero;
    private int enhancedTileCharges = 3;

    // Action Testing
    private bool autoTriggerMarkers = false;
    private float autoTriggerDelay = 2f;
    private bool showAreaPreviews = true;
    private Vector2Int testMarkerPosition = new Vector2Int(3, 5);

    // Quick Test Scenarios
    private bool showQuickScenarios = false;

    public void Initialize()
    {
        actionManager = Object.FindObjectOfType<PlayerActionManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        gridManager = Object.FindObjectOfType<GridManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();

        if (gridManager != null)
        {
            cubeSpawnRow = Mathf.Max(10, gridManager.Height - 5);
        }
    }

    private static float lastTriggerTime = 0f;

    public void Update()
    {
        if (autoTriggerMarkers && actionManager != null && actionManager.HasCubeMarkers())
        {
            if (Time.time - lastTriggerTime > autoTriggerDelay)
            {
                actionManager.TriggerNextCubeMarker();
                lastTriggerTime = Time.time;
            }
        }
    }

    public void DrawPanel()
    {
        DrawPanelTabs();
        GUILayout.Space(5);

        if (showActionInfo)
            DrawActionInfoSection();

        if (showActionControls)
            DrawActionControlsSection();

        if (showCubeSpawner)
            DrawCubeSpawnerSection();

        if (showTileEditor)
            DrawTileEditorSection();
    }

    private void DrawPanelTabs()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = showActionInfo ? Color.cyan : Color.white;
        if (GUILayout.Button("Info", GUILayout.Height(25)))
            showActionInfo = !showActionInfo;

        GUI.backgroundColor = showActionControls ? Color.cyan : Color.white;
        if (GUILayout.Button("Actions", GUILayout.Height(25)))
            showActionControls = !showActionControls;

        GUI.backgroundColor = showCubeSpawner ? Color.cyan : Color.white;
        if (GUILayout.Button("Cube Spawner", GUILayout.Height(25)))
            showCubeSpawner = !showCubeSpawner;

        GUI.backgroundColor = showTileEditor ? Color.cyan : Color.white;
        if (GUILayout.Button("Tile Editor", GUILayout.Height(25)))
            showTileEditor = !showTileEditor;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawActionInfoSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER ACTION INFO", GUI.skin.box);

        if (actionManager != null)
        {
            GUILayout.Label($"Active Cube Markers: {actionManager.CubeMarkerCount}");

            Vector2Int nextMarker = actionManager.GetNextCubeMarker();
            if (nextMarker.x >= 0)
            {
                GUILayout.Label($"Next Marker: ({nextMarker.x}, {nextMarker.y})");

                if (GUILayout.Button("Preview Next Area") && showAreaPreviews)
                {
                    actionManager.ShowAreaPreview(nextMarker);
                }
            }
            else
            {
                GUILayout.Label("No cube markers available");
            }

            int playerMarkers = 0;
            if (gridManager != null)
            {
                playerMarkers = gridManager.GetMarkerCount();
            }
            GUILayout.Label($"Player Markers Placed: {playerMarkers}");

            if (playerManager != null)
            {
                GUILayout.Label($"Player Position: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})");

                if (gridManager != null && gridManager.IsValidGridPosition(playerManager.currentTilePosition))
                {
                    var tile = gridManager.GetTileAt(playerManager.currentTilePosition);
                    if (tile != null)
                    {
                        GUILayout.Label($"Current Tile: {GetTileStateDescription(tile)}");
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("PlayerActionManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawActionControlsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTION CONTROLS", GUI.skin.box);

        if (actionManager == null)
        {
            GUILayout.Label("PlayerActionManager not found");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label("Manual Controls:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trigger Next Cube Marker"))
        {
            actionManager.TriggerNextCubeMarker();
        }
        if (GUILayout.Button("Clear All Actions"))
        {
            actionManager.ClearAllActions();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Test Marker Placement:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("X:", GUILayout.Width(20));
        string xStr = GUILayout.TextField(testMarkerPosition.x.ToString(), GUILayout.Width(40));
        if (int.TryParse(xStr, out int newX))
            testMarkerPosition.x = Mathf.Clamp(newX, 0, gridManager?.Width - 1 ?? 10);

        GUILayout.Label("Y:", GUILayout.Width(20));
        string yStr = GUILayout.TextField(testMarkerPosition.y.ToString(), GUILayout.Width(40));
        if (int.TryParse(yStr, out int newY))
            testMarkerPosition.y = Mathf.Clamp(newY, 0, gridManager?.Height - 1 ?? 20);

        if (GUILayout.Button("Place Player Marker"))
        {
            actionManager.PlacePlayerMarkerAt(testMarkerPosition);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trigger Player Marker"))
        {
            actionManager.TriggerPlayerMarkerAt(testMarkerPosition);
        }
        if (GUILayout.Button("Create Cube Marker"))
        {
            actionManager.CreateCubeMarker(testMarkerPosition);
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Automation:");
        autoTriggerMarkers = GUILayout.Toggle(autoTriggerMarkers, "Auto-Trigger Cube Markers");

        if (autoTriggerMarkers)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Delay:", GUILayout.Width(50));
            string delayStr = GUILayout.TextField(autoTriggerDelay.ToString("F1"), GUILayout.Width(50));
            if (float.TryParse(delayStr, out float newDelay))
                autoTriggerDelay = Mathf.Clamp(newDelay, 0.5f, 10f);
            GUILayout.Label("seconds");
            GUILayout.EndHorizontal();
        }

        showAreaPreviews = GUILayout.Toggle(showAreaPreviews, "Show Area Previews");

        showQuickScenarios = GUILayout.Toggle(showQuickScenarios, "Show Quick Scenarios");
        if (showQuickScenarios)
        {
            DrawQuickScenarios();
        }

        GUILayout.EndVertical();
    }

    private void DrawCubeSpawnerSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE SPAWNER", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Spawn Row:", GUILayout.Width(80));
        string rowStr = GUILayout.TextField(cubeSpawnRow.ToString(), GUILayout.Width(50));
        if (int.TryParse(rowStr, out int newRow))
            cubeSpawnRow = Mathf.Clamp(newRow, 0, gridManager?.Height - 1 ?? 20);

        if (GUILayout.Button("Set to Top"))
        {
            cubeSpawnRow = gridManager?.Height - 1 ?? 20;
        }
        if (GUILayout.Button("Set to Mid"))
        {
            cubeSpawnRow = gridManager?.Height / 2 ?? 10;
        }
        GUILayout.EndHorizontal();

        DrawCubeTypeSelector();

        showCubeGrid = GUILayout.Toggle(showCubeGrid, "Show Cube Placement Grid");
        if (showCubeGrid)
        {
            DrawCubeSpawnerGrid();
        }

        GUILayout.Label("Quick Spawn:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Normal Row"))
        {
            SpawnCubeRow(CubeType.Normal);
        }
        if (GUILayout.Button("Spawn Blue Row"))
        {
            SpawnCubeRow(CubeType.Blue);
        }
        if (GUILayout.Button("Spawn Mixed Row"))
        {
            SpawnMixedCubeRow();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Row"))
        {
            ClearCubeRow();
        }
        if (GUILayout.Button("Clear All Cubes"))
        {
            ClearAllCubes();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawTileEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE EDITOR", GUI.skin.box);

        DrawTileStateSelector();

        if (selectedTileState == 3)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Charges:", GUILayout.Width(60));
            string chargesStr = GUILayout.TextField(enhancedTileCharges.ToString(), GUILayout.Width(40));
            if (int.TryParse(chargesStr, out int newCharges))
                enhancedTileCharges = Mathf.Clamp(newCharges, 1, 5);
            GUILayout.EndHorizontal();
        }

        showTileGrid = GUILayout.Toggle(showTileGrid, "Show Tile State Grid");
        if (showTileGrid)
        {
            DrawTileStateGrid();
        }

        GUILayout.Label("Quick Actions:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prime Player Tile"))
        {
            SetPlayerTileState(1);
        }
        if (GUILayout.Button("Corrupt Player Tile"))
        {
            SetPlayerTileState(2);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All Tiles"))
        {
            ResetAllTiles();
        }
        if (GUILayout.Button("Create Test Pattern"))
        {
            CreateTestTilePattern();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawCubeTypeSelector()
    {
        GUILayout.Label("Cube Type:");
        GUILayout.BeginHorizontal();

        Color originalColor = GUI.backgroundColor;

        GUI.backgroundColor = selectedCubeType == 0 ? Color.gray : Color.white;
        if (GUILayout.Button("Empty"))
            selectedCubeType = 0;

        GUI.backgroundColor = selectedCubeType == 1 ? Color.gray : Color.white;
        if (GUILayout.Button("Normal"))
            selectedCubeType = 1;

        GUI.backgroundColor = selectedCubeType == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Blue"))
            selectedCubeType = 2;

        GUI.backgroundColor = selectedCubeType == 3 ? Color.black : Color.white;
        if (GUILayout.Button("Black"))
            selectedCubeType = 3;

        GUI.backgroundColor = originalColor;
        GUILayout.EndHorizontal();
    }

    private void DrawCubeSpawnerGrid()
    {
        if (gridManager == null) return;

        GUILayout.Label($"Cube Grid (Row {cubeSpawnRow}):");

        Color originalColor = GUI.backgroundColor;

        cubeGridScrollPosition = GUILayout.BeginScrollView(cubeGridScrollPosition, GUILayout.Height(100));

        GUILayout.BeginHorizontal();
        for (int x = 0; x < gridManager.Width; x++)
        {
            bool hasCube = HasCubeAt(new Vector2Int(x, cubeSpawnRow));
            CubeType existingType = GetCubeTypeAt(new Vector2Int(x, cubeSpawnRow));

            if (hasCube)
            {
                SetCubeButtonColor((int)existingType + 1);
            }
            else
            {
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            }

            string buttonText = hasCube ? GetCubeButtonText((int)existingType + 1) : "·";

            if (GUILayout.Button(buttonText, GUILayout.Width(30), GUILayout.Height(30)))
            {
                if (selectedCubeType == 0)
                {
                    RemoveCubeAt(new Vector2Int(x, cubeSpawnRow));
                }
                else
                {
                    SpawnCubeAt(new Vector2Int(x, cubeSpawnRow), (CubeType)(selectedCubeType - 1));
                }
            }
        }
        GUILayout.EndHorizontal();

        GUI.backgroundColor = originalColor;
        GUILayout.EndScrollView();
    }

    private void DrawTileStateSelector()
    {
        GUILayout.Label("Tile State:");
        GUILayout.BeginHorizontal();

        Color originalColor = GUI.backgroundColor;

        GUI.backgroundColor = selectedTileState == 0 ? Color.green : Color.white;
        if (GUILayout.Button("Normal"))
            selectedTileState = 0;

        GUI.backgroundColor = selectedTileState == 1 ? Color.blue : Color.white;
        if (GUILayout.Button("Primed"))
            selectedTileState = 1;

        GUI.backgroundColor = selectedTileState == 2 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted"))
            selectedTileState = 2;

        GUI.backgroundColor = selectedTileState == 3 ? Color.yellow : Color.white;
        if (GUILayout.Button("Enhanced"))
            selectedTileState = 3;

        GUI.backgroundColor = originalColor;
        GUILayout.EndHorizontal();
    }

    private void DrawTileStateGrid()
    {
        if (gridManager == null) return;

        GUILayout.Label("Tile State Grid (click to set state):");

        Color originalColor = GUI.backgroundColor;

        tileGridScrollPosition = GUILayout.BeginScrollView(tileGridScrollPosition, GUILayout.Height(150));

        int playerX = playerManager?.currentTilePosition.x ?? 3;
        int playerY = playerManager?.currentTilePosition.y ?? 5;

        int startX = Mathf.Max(0, playerX - 4);
        int endX = Mathf.Min(gridManager.Width - 1, playerX + 4);
        int startY = Mathf.Max(0, playerY - 3);
        int endY = Mathf.Min(gridManager.Height - 1, playerY + 3);

        for (int y = endY; y >= startY; y--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{y}", GUILayout.Width(20));

            for (int x = startX; x <= endX; x++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                bool isPlayerTile = (x == playerX && y == playerY);

                if (isPlayerTile)
                {
                    GUI.backgroundColor = Color.green;
                }
                else if (tile != null)
                {
                    if (tile.IsBlackened) GUI.backgroundColor = Color.red;
                    else if (tile.IsPrimed) GUI.backgroundColor = Color.blue;
                    else if (tile.HasCharges) GUI.backgroundColor = Color.yellow;
                    else if (tile.HasMarker) GUI.backgroundColor = Color.cyan;
                    else GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = Color.gray;
                }

                string buttonText = GetTileButtonText(tile, isPlayerTile);

                if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    SetTileState(new Vector2Int(x, y), selectedTileState);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = originalColor;
        GUILayout.EndScrollView();
    }

    private void DrawQuickScenarios()
    {
        GUILayout.Label("Quick Test Scenarios:");

        if (GUILayout.Button("Mark Player Tile"))
        {
            if (playerManager != null && actionManager != null)
            {
                actionManager.PlacePlayerMarkerAt(playerManager.currentTilePosition);
            }
        }

        if (GUILayout.Button("Blue Cube Capture Test"))
        {
            Vector2Int spawnPos = new Vector2Int(
                1,
                cubeSpawnRow);
            SpawnCubeAt(spawnPos, CubeType.Blue);
            actionManager.PlacePlayerMarkerAt(spawnPos);
        }

        if (GUILayout.Button("Chain Reaction Test"))
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2Int pos = new Vector2Int(
                    i,
                    cubeSpawnRow);
                SpawnCubeAt(pos, CubeType.Blue);
            }
            actionManager.PlacePlayerMarkerAt(new Vector2Int(0, cubeSpawnRow));
        }

        if (GUILayout.Button("Area Effect Test"))
        {
            Vector2Int center = new Vector2Int(
                playerManager.currentTilePosition.x,
                playerManager.currentTilePosition.y + 3);

            // Create a 2x2 grid of cubes for testing area capture
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                    CubeType type = (x == 0 && y == 0) ? CubeType.Blue : CubeType.Normal;
                    SpawnCubeAt(pos, type);
                }
            }

            // Create cube marker at center for 2x2 area effect
            actionManager.CreateCubeMarker(center);
            Debug.Log($"Created 2x2 test area with cube marker at ({center.x}, {center.y})");
        }

        if (GUILayout.Button("Cascading Blue Test"))
        {
            Vector2Int center = new Vector2Int(
                playerManager.currentTilePosition.x,
                playerManager.currentTilePosition.y + 4);

            // Create a pattern where blue cubes will cascade
            // First blue cube that will be captured and create a marker
            SpawnCubeAt(center, CubeType.Blue);

            // Second blue cube that should be captured by the first's marker
            SpawnCubeAt(new Vector2Int(center.x + 1, center.y), CubeType.Blue);

            // Some normal cubes in the area
            SpawnCubeAt(new Vector2Int(center.x, center.y + 1), CubeType.Normal);
            SpawnCubeAt(new Vector2Int(center.x + 1, center.y + 1), CubeType.Normal);

            // Place initial player marker to start the cascade
            actionManager.PlacePlayerMarkerAt(center);

            Debug.Log($"Created cascading blue cube test at ({center.x}, {center.y})");
        }
    }

    // Helper Methods
    private bool HasCubeAt(Vector2Int position)
    {
        foreach (CubeBehavior cube in Object.FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                return true;
            }
        }
        return false;
    }

    private CubeType GetCubeTypeAt(Vector2Int position)
    {
        foreach (CubeBehavior cube in Object.FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                return cube.type;
            }
        }
        return CubeType.Normal;
    }

    private void RemoveCubeAt(Vector2Int position)
    {
        foreach (CubeBehavior cube in Object.FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                Object.Destroy(cube.gameObject);
                break;
            }
        }
    }

    private void SpawnCubeAt(Vector2Int position, CubeType cubeType)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeType >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Cannot spawn {cubeType} cube - prefab not found");
            return;
        }

        RemoveCubeAt(position);

        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeBehavior>();
        if (cube == null) cube = cubeObj.AddComponent<CubeBehavior>();

        var cubeData = new CubeData
        {
            type = cubeType,
            position = position,
            level = 1
        };

        cube.Init(gridManager, cubeData, 2f);
        // IMPORTANT: Register the cube with the tile so it can be captured
        if (gridManager.IsValidGridPosition(position))
        {
            Tile tile = gridManager.GetTileAt(position);
            if (tile != null)
            {
                tile.ProcessCubeInteraction(cube);
            }
        }
        if (waveManager != null)
        {
            waveManager.activeCubes.Add(cube);
        }
        Debug.Log($"Spawned {cubeType} cube at ({position.x}, {position.y})");
    }

    private void SpawnCubeRow(CubeType cubeType)
    {
        if (gridManager == null) return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            SpawnCubeAt(new Vector2Int(x, cubeSpawnRow), cubeType);
        }
    }

    private void SpawnMixedCubeRow()
    {
        if (gridManager == null) return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            CubeType type = (x % 3) switch
            {
                0 => CubeType.Normal,
                1 => CubeType.Blue,
                _ => CubeType.Normal
            };
            SpawnCubeAt(new Vector2Int(x, cubeSpawnRow), type);
        }
    }

    private void ClearCubeRow()
    {
        if (gridManager == null) return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            RemoveCubeAt(new Vector2Int(x, cubeSpawnRow));
        }
    }

    private void ClearAllCubes()
    {
        foreach (CubeBehavior cube in Object.FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null)
            {
                Object.Destroy(cube.gameObject);
            }
        }
    }

    private void SetTileState(Vector2Int position, int state)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        switch (state)
        {
            case 0:
                tile.ResetTile();
                break;
            case 1:
                tile.PrimeTile();
                break;
            case 2:
                tile.BlackenTile();
                break;
            case 3:
                tile.AdvantageTile(enhancedTileCharges);
                break;
        }

        Debug.Log($"Set tile ({position.x}, {position.y}) to state {state}");
    }

    private void SetPlayerTileState(int state)
    {
        if (playerManager != null)
        {
            SetTileState(playerManager.currentTilePosition, state);
        }
    }

    private void ResetAllTiles()
    {
        if (gridManager == null) return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null)
                {
                    tile.ResetTile();
                }
            }
        }

        Debug.Log("Reset all tiles to normal state");
    }

    private void CreateTestTilePattern()
    {
        if (gridManager == null || playerManager == null) return;

        Vector2Int center = playerManager.currentTilePosition;

        SetTileState(new Vector2Int(center.x - 1, center.y + 1), 1);
        SetTileState(new Vector2Int(center.x + 1, center.y + 1), 1);
        SetTileState(new Vector2Int(center.x, center.y + 2), 3);
        SetTileState(new Vector2Int(center.x - 1, center.y - 1), 2);
        SetTileState(new Vector2Int(center.x + 1, center.y - 1), 2);

        Debug.Log("Created test tile pattern around player");
    }

    private string GetTileStateDescription(Tile tile)
    {
        if (tile.IsBlackened) return "Corrupted";
        if (tile.IsPrimed) return "Primed";
        if (tile.HasCharges) return $"Enhanced ({tile.DetonationCharges})";
        if (tile.HasMarker) return "Has Marker";
        return "Normal";
    }

    private string GetTileButtonText(Tile tile, bool isPlayer)
    {
        if (isPlayer) return "P";
        if (tile == null) return "?";
        if (tile.IsBlackened) return "X";
        if (tile.IsPrimed) return "○";
        if (tile.HasCharges) return tile.DetonationCharges.ToString();
        if (tile.HasMarker) return "M";
        return "·";
    }

    private void SetCubeButtonColor(int cubeType)
    {
        switch (cubeType)
        {
            case 0: GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f); break;
            case 1: GUI.backgroundColor = Color.white; break;
            case 2: GUI.backgroundColor = Color.blue; break;
            case 3: GUI.backgroundColor = Color.black; break;
            default: GUI.backgroundColor = Color.white; break;
        }
    }

    private string GetCubeButtonText(int cubeType)
    {
        switch (cubeType)
        {
            case 0: return "·";
            case 1: return "N";
            case 2: return "B";
            case 3: return "X";
            default: return "?";
        }
    }
}