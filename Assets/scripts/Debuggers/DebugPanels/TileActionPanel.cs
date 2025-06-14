using UnityEngine;
using static Enumerations;
using System.Collections.Generic;
using System.Linq;

public class TileActionPanel : IDebugPanel
{
    public string PanelName => "Tile Actions";

    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;

    // UI State
    private bool showTileEditor = true;
    private bool showFacePaintingControls = true;
    private bool showTileInspector = true;
    private bool showTestScenarios = true;

    // Tile Editor
    private int selectedTileState = 0;
    private bool showTileGrid = false;
    private Vector2 tileGridScrollPosition = Vector2.zero;
    private int enhancedTileCharges = 3;

    // Face Painting Controls
    private int selectedFaceStatus = 1; // 1 = Corrupted, 2 = Enhanced
    private int facePaintDuration = 3;
    private bool paintOnLanding = true;
    private bool paintOnExit = false;
    private bool showFacePaintGrid = false;
    private Vector2 facePaintGridScrollPosition = Vector2.zero;

    // Tile Inspector
    private Vector2Int inspectorPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;

    public void Initialize()
    {
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();

        if (playerManager != null)
        {
            inspectorPosition = playerManager.currentTilePosition;
        }
    }

    public void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            inspectorPosition = playerManager.currentTilePosition;
        }
    }

    public void DrawPanel()
    {
        DrawPanelTabs();
        GUILayout.Space(5);

        if (showTileEditor)
            DrawTileEditorSection();

        if (showFacePaintingControls)
            DrawFacePaintingSection();

        if (showTileInspector)
            DrawTileInspectorSection();

        if (showTestScenarios)
            DrawTestScenariosSection();
    }

    private void DrawPanelTabs()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = showTileEditor ? Color.cyan : Color.white;
        if (GUILayout.Button("Tile Editor", GUILayout.Height(25)))
            showTileEditor = !showTileEditor;

        GUI.backgroundColor = showFacePaintingControls ? Color.cyan : Color.white;
        if (GUILayout.Button("Face Paint", GUILayout.Height(25)))
            showFacePaintingControls = !showFacePaintingControls;

        GUI.backgroundColor = showTileInspector ? Color.cyan : Color.white;
        if (GUILayout.Button("Inspector", GUILayout.Height(25)))
            showTileInspector = !showTileInspector;

        GUI.backgroundColor = showTestScenarios ? Color.cyan : Color.white;
        if (GUILayout.Button("Test Scenarios", GUILayout.Height(25)))
            showTestScenarios = !showTestScenarios;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    #region Tile Editor Section

    private void DrawTileEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE EDITOR", GUI.skin.box);
        if (GUILayout.Button("Test: Face Mapping & Rotation"))
        {
            TestFaceMappingRotation();
        }
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
        if (GUILayout.Button("Set Player Tile"))
        {
            SetPlayerTileState(selectedTileState);
        }
        if (GUILayout.Button("Set Inspector Tile"))
        {
            SetTileState(inspectorPosition, selectedTileState);
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
                bool isInspectorTile = (x == inspectorPosition.x && y == inspectorPosition.y);

                if (isPlayerTile)
                {
                    GUI.backgroundColor = Color.green;
                }
                else if (isInspectorTile)
                {
                    GUI.backgroundColor = Color.magenta;
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

                string buttonText = GetTileButtonText(tile, isPlayerTile, isInspectorTile);

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

    #endregion

    #region Face Painting Section

    private void DrawFacePaintingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTING SYSTEM", GUI.skin.box);

        // Face Status Selector
        DrawFaceStatusSelector();

        // Duration Control
        GUILayout.BeginHorizontal();
        GUILayout.Label("Duration:", GUILayout.Width(60));
        string durationStr = GUILayout.TextField(facePaintDuration.ToString(), GUILayout.Width(40));
        if (int.TryParse(durationStr, out int newDuration))
            facePaintDuration = Mathf.Clamp(newDuration, -1, 10);
        GUILayout.Label("(-1 = permanent)");
        GUILayout.EndHorizontal();

        // Painting Triggers
        GUILayout.Label("Paint Triggers:");
        paintOnLanding = GUILayout.Toggle(paintOnLanding, "Paint on Landing");
        paintOnExit = GUILayout.Toggle(paintOnExit, "Paint on Exit");

        GUILayout.Space(5);

        // Quick Setup Buttons
        GUILayout.Label("Quick Tile Setup:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Corruption Tile"))
        {
            SetupTilePainting(inspectorPosition, FaceStatus.Corrupted, Color.red, facePaintDuration);
        }
        if (GUILayout.Button("Enhancement Tile"))
        {
            SetupTilePainting(inspectorPosition, FaceStatus.Enhanced, Color.blue, facePaintDuration);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player Tile: Corrupt"))
        {
            if (playerManager != null)
                SetupTilePainting(playerManager.currentTilePosition, FaceStatus.Corrupted, Color.red, facePaintDuration);
        }
        if (GUILayout.Button("Player Tile: Enhance"))
        {
            if (playerManager != null)
                SetupTilePainting(playerManager.currentTilePosition, FaceStatus.Enhanced, Color.blue, facePaintDuration);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Inspector Tile Paint"))
        {
            ClearTilePainting(inspectorPosition);
        }
        if (GUILayout.Button("Clear All Tile Paint"))
        {
            ClearAllTilePainting();
        }
        GUILayout.EndHorizontal();

        showFacePaintGrid = GUILayout.Toggle(showFacePaintGrid, "Show Face Paint Grid");
        if (showFacePaintGrid)
        {
            DrawFacePaintGrid();
        }

        GUILayout.EndVertical();
    }

    private void DrawFaceStatusSelector()
    {
        GUILayout.Label("Face Status to Paint:");
        GUILayout.BeginHorizontal();

        Color originalColor = GUI.backgroundColor;

        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted"))
            selectedFaceStatus = 1;

        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Enhanced"))
            selectedFaceStatus = 2;

        GUI.backgroundColor = originalColor;
        GUILayout.EndHorizontal();
    }

    private void DrawFacePaintGrid()
    {
        if (gridManager == null) return;

        GUILayout.Label("Face Paint Grid (C=Corrupt, E=Enhance):");

        Color originalColor = GUI.backgroundColor;

        facePaintGridScrollPosition = GUILayout.BeginScrollView(facePaintGridScrollPosition, GUILayout.Height(120));

        int playerX = playerManager?.currentTilePosition.x ?? 3;
        int playerY = playerManager?.currentTilePosition.y ?? 5;

        int startX = Mathf.Max(0, playerX - 3);
        int endX = Mathf.Min(gridManager.Width - 1, playerX + 3);
        int startY = Mathf.Max(0, playerY - 2);
        int endY = Mathf.Min(gridManager.Height - 1, playerY + 2);

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
                else if (HasFacePaintingSetup(tile))
                {
                    GUI.backgroundColor = Color.yellow; // Tile has face painting
                }
                else
                {
                    GUI.backgroundColor = Color.white;
                }

                string buttonText = GetFacePaintButtonText(tile, isPlayerTile);

                if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                    Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
                    SetupTilePainting(pos, status, color, facePaintDuration);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = originalColor;
        GUILayout.EndScrollView();
    }

    #endregion

    #region Tile Inspector Section

    private void DrawTileInspectorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE INSPECTOR", GUI.skin.box);

        // Position Controls
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Auto-track Player");
        if (!autoTrackPlayer)
        {
            GUILayout.Label("X:", GUILayout.Width(20));
            string xStr = GUILayout.TextField(inspectorPosition.x.ToString(), GUILayout.Width(40));
            if (int.TryParse(xStr, out int newX))
                inspectorPosition.x = Mathf.Clamp(newX, 0, gridManager?.Width - 1 ?? 10);

            GUILayout.Label("Y:", GUILayout.Width(20));
            string yStr = GUILayout.TextField(inspectorPosition.y.ToString(), GUILayout.Width(40));
            if (int.TryParse(yStr, out int newY))
                inspectorPosition.y = Mathf.Clamp(newY, 0, gridManager?.Height - 1 ?? 20);
        }
        GUILayout.EndHorizontal();

        if (gridManager != null && gridManager.IsValidGridPosition(inspectorPosition))
        {
            Tile inspectedTile = gridManager.GetTileAt(inspectorPosition);
            DrawTileDetails(inspectedTile, inspectorPosition);
        }
        else
        {
            GUILayout.Label("Invalid position or no tile found");
        }

        GUILayout.EndVertical();
    }

    private void DrawTileDetails(Tile tile, Vector2Int position)
    {
        if (tile == null)
        {
            GUILayout.Label($"Position ({position.x}, {position.y}): NULL TILE");
            return;
        }

        GUILayout.Label($"Position ({position.x}, {position.y}):");
        GUILayout.Label($"State: {GetTileStateDescription(tile)}");
        GUILayout.Label($"Has Marker: {tile.HasMarker}");
        GUILayout.Label($"Has Charges: {tile.HasCharges} ({tile.DetonationCharges})");
        GUILayout.Label($"Is Playable: {tile.IsPlayable}");
        GUILayout.Label($"Has Detonation Point: {tile.HasDetonationPoint}");

        // Face painting info
        GUILayout.Space(3);
        GUILayout.Label("Face Painting Info:", GUI.skin.box);
        DrawFacePaintingInfo(tile);

        // Cubes on this tile
        DrawCubesOnTile(position);
    }

    private void DrawFacePaintingInfo(Tile tile)
    {
        // This would require extending the Tile class to expose face painting status
        // For now, show what we can determine
        bool hasFacePainting = HasFacePaintingSetup(tile);
        GUILayout.Label($"Has Face Painting: {hasFacePainting}");

        if (hasFacePainting)
        {
            GUILayout.Label("  Paint on Landing: Active");
            // Could add more details if Tile class exposes them
        }
    }

    private void DrawCubesOnTile(Vector2Int position)
    {
        var cubes = FindCubesAt(position);

        GUILayout.Space(3);
        GUILayout.Label($"Cubes on Tile: {cubes.Count}", GUI.skin.box);

        foreach (var cube in cubes)
        {
            if (cube != null)
            {
                GUILayout.Label($"  {cube.type} | Face: {cube.GetCurrentDownFace()} | Status: {cube.GetActiveFaceStatus()}");
                GUILayout.Label($"  Effective Type: {cube.GetEffectiveType()} | Can Capture: {cube.CanBeCaptured()}");
            }
        }
    }

    #endregion

    #region Test Scenarios Section

    private void DrawTestScenariosSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTING TEST SCENARIOS", GUI.skin.box);

        if (GUILayout.Button("Test: Corrupted Face (Normal→Black behavior)"))
        {
            TestCorruptedFace();
        }

        if (GUILayout.Button("Test: Enhanced Face (Normal→Blue behavior)"))
        {
            TestEnhancedFace();
        }

        if (GUILayout.Button("Test: Multiple Face Rotations"))
        {
            TestMultipleFaceRotations();
        }

        if (GUILayout.Button("Test: Face Duration Expiry"))
        {
            TestFaceDurationExpiry();
        }

        if (GUILayout.Button("Test: Mixed Face Statuses"))
        {
            TestMixedFaceStatuses();
        }

        GUILayout.Space(5);
        DrawActiveCubeFaceStatus();

        GUILayout.EndVertical();
    }

    private void DrawActiveCubeFaceStatus()
    {
        GUILayout.Label("ACTIVE CUBES FACE STATUS", GUI.skin.box);

        var allCubes = Object.FindObjectsOfType<CubeBehavior>();
        int shownCubes = 0;

        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && shownCubes < 5) // Limit display
            {
                GUILayout.Label($"Cube at ({cube.position.x},{cube.position.y}):");
                GUILayout.Label($"  Type: {cube.type} → Effective: {cube.GetEffectiveType()}");
                GUILayout.Label($"  Current Face: {cube.GetCurrentDownFace()}");
                GUILayout.Label($"  Face Status: {cube.GetActiveFaceStatus()}");
                GUILayout.Label($"  Can Capture: {cube.CanBeCaptured()}");
                GUILayout.Space(2);
                shownCubes++;
            }
        }

        if (shownCubes == 0)
        {
            GUILayout.Label("No active cubes found");
        }
        else if (allCubes.Length > 5)
        {
            GUILayout.Label($"... and {allCubes.Length - 5} more cubes");
        }
    }

    #endregion

    #region Helper Methods

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
                    ClearTilePainting(new Vector2Int(x, y)); // Also clear face painting
                }
            }
        }

        Debug.Log("Reset all tiles to normal state and cleared face painting");
    }

    private void CreateTestTilePattern()
    {
        if (gridManager == null || playerManager == null) return;

        Vector2Int center = playerManager.currentTilePosition;

        // Create a pattern around the player
        SetTileState(new Vector2Int(center.x - 1, center.y + 1), 1); // Primed
        SetTileState(new Vector2Int(center.x + 1, center.y + 1), 1); // Primed
        SetTileState(new Vector2Int(center.x, center.y + 2), 3);     // Enhanced
        SetTileState(new Vector2Int(center.x - 1, center.y - 1), 2); // Corrupted
        SetTileState(new Vector2Int(center.x + 1, center.y - 1), 2); // Corrupted

        // Add face painting to some tiles
        SetupTilePainting(new Vector2Int(center.x - 2, center.y), FaceStatus.Corrupted, Color.red, 3);
        SetupTilePainting(new Vector2Int(center.x + 2, center.y), FaceStatus.Enhanced, Color.blue, 3);

        Debug.Log("Created test tile pattern with face painting around player");
    }

    private void SetupTilePainting(Vector2Int position, FaceStatus status, Color color, int duration)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        tile.SetupFacePainting(status, color, duration, paintOnLanding, paintOnExit);
        Debug.Log($"Set up {status} face painting at ({position.x}, {position.y}) for {duration} moves");
    }

    private void ClearTilePainting(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        tile.DisableFacePainting();
        Debug.Log($"Cleared face painting at ({position.x}, {position.y})");
    }

    private void ClearAllTilePainting()
    {
        if (gridManager == null) return;

        int cleared = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null && HasFacePaintingSetup(tile))
                {
                    tile.DisableFacePainting();
                    cleared++;
                }
            }
        }

        Debug.Log($"Cleared face painting from {cleared} tiles");
    }

    private bool HasFacePaintingSetup(Tile tile)
    {
        return tile != null && tile.canPaintCubes;
    }

    private List<CubeBehavior> FindCubesAt(Vector2Int position)
    {
        List<CubeBehavior> cubes = new List<CubeBehavior>();

        foreach (CubeBehavior cube in Object.FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                cubes.Add(cube);
            }
        }

        return cubes;
    }

    private string GetTileStateDescription(Tile tile)
    {
        if (tile.IsBlackened) return "Corrupted";
        if (tile.IsPrimed) return "Primed";
        if (tile.HasCharges) return $"Enhanced ({tile.DetonationCharges})";
        if (tile.HasMarker) return "Has Marker";
        return "Normal";
    }

    private string GetTileButtonText(Tile tile, bool isPlayer, bool isInspector)
    {
        if (isPlayer) return "P";
        if (isInspector) return "I";
        if (tile == null) return "?";
        if (tile.IsBlackened) return "X";
        if (tile.IsPrimed) return "○";
        if (tile.HasCharges) return tile.DetonationCharges.ToString();
        if (tile.HasMarker) return "M";
        return "·";
    }

    private string GetFacePaintButtonText(Tile tile, bool isPlayer)
    {
        if (isPlayer) return "P";
        if (tile == null) return "?";
        if (HasFacePaintingSetup(tile)) return "F"; // F for Face painting
        return "·";
    }

    #endregion

    #region Test Scenario Methods

    private void TestCorruptedFace()
    {
        Vector2Int testPos = new Vector2Int(2, 10);

        // Set up corruption painting tile
        SetupTilePainting(testPos, FaceStatus.Corrupted, Color.red, 3);

        // Spawn a normal cube above it
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 1), CubeType.Normal);

        Debug.Log("Corrupted face test: Normal cube will be painted with corruption when it lands. Should act like black cube when interacted with.");
    }

    private void TestEnhancedFace()
    {
        Vector2Int testPos = new Vector2Int(3, 10);

        // Set up enhancement painting tile
        SetupTilePainting(testPos, FaceStatus.Enhanced, Color.blue, 3);

        // Spawn a normal cube above it
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 1), CubeType.Normal);

        Debug.Log("Enhanced face test: Normal cube will be painted with enhancement when it lands. Should create detonation when captured.");
    }

    private void TestMultipleFaceRotations()
    {
        Vector2Int testPos = new Vector2Int(1, 12);

        // Create multiple painting tiles in a line
        for (int i = 0; i < 3; i++)
        {
            FaceStatus status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            Color color = status == FaceStatus.Corrupted ? Color.red : Color.blue;
            SetupTilePainting(new Vector2Int(testPos.x, testPos.y + i), status, color, 5);
        }

        // Spawn cube at top
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 4), CubeType.Normal);

        Debug.Log("Multiple face rotation test: Cube will be painted multiple times as it moves, rotating faces each step.");
    }

    private void TestFaceDurationExpiry()
    {
        Vector2Int testPos = new Vector2Int(4, 10);

        // Set up short duration painting
        SetupTilePainting(testPos, FaceStatus.Corrupted, Color.red, 1); // Only 1 move

        // Spawn cube above
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 3), CubeType.Normal);

        Debug.Log("Face duration expiry test: Cube will lose painted status after 1 move.");
    }

    private void TestMixedFaceStatuses()
    {
        Vector2Int testPos = new Vector2Int(0, 10);

        // Create a pattern of different painting tiles
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y), FaceStatus.Corrupted, Color.red, 3);
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y + 1), FaceStatus.Enhanced, Color.blue, 3);
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y + 2), FaceStatus.Corrupted, Color.red, 2);

        // Spawn cube to move through all
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 4), CubeType.Normal);

        Debug.Log("Mixed face statuses test: Cube will accumulate different face paintings on different faces as it moves.");
    }

    private void SpawnCubeAt(Vector2Int position, CubeType cubeType)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeType >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Cannot spawn {cubeType} cube - prefab not found");
            return;
        }

        // Remove any existing cube at this position
        var existingCubes = FindCubesAt(position);
        foreach (var cube in existingCubes)
        {
            if (cube != null)
            {
                Object.Destroy(cube.gameObject);
            }
        }

        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPos, Quaternion.identity);

        var cubeBehavior = cubeObj.GetComponent<CubeBehavior>();
        if (cubeBehavior == null) cubeBehavior = cubeObj.AddComponent<CubeBehavior>();

        var cubeData = new CubeData
        {
            type = cubeType,
            position = position,
            level = 1
        };

        cubeBehavior.Init(gridManager, cubeData, 2f);

        // Add to wave manager if available
        if (waveManager != null)
        {
            waveManager.activeCubes.Add(cubeBehavior);
        }

        Debug.Log($"Spawned {cubeType} cube at ({position.x}, {position.y}) for testing");
    }

    private void TestFaceMappingRotation()
    {
        Vector2Int testPos = new Vector2Int(2, 10);
        SpawnCubeAt(testPos, CubeType.Normal);

        var cubes = FindCubesAt(testPos);
        if (cubes.Count > 0)
        {
            var cube = cubes[0];

            // Paint the original bottom face red
            cube.TestPaintFace(CubeFace.Bottom, FaceStatus.Corrupted);
            // Paint the original front face blue  
            cube.TestPaintFace(CubeFace.Front, FaceStatus.Enhanced);

            cube.DebugPrintFaceMapping();

            Debug.Log("Watch the cube roll - red face (corrupted) and blue face (enhanced) will rotate around the cube!");
        }
    }

    #endregion
}