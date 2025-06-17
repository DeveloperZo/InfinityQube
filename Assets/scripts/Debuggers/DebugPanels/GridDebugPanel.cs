using static Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridDebugPanel : DebugPanelBase
{
    public override string PanelName => "Grid Manager";
    public override DebugPanelGroup Group => DebugPanelGroup.Core;

    private GridManager gridManager;
    private PlayerManager playerManager;

    // UI State
    private bool showGridManagement = true;
    private bool showTileManipulation = true;
    private bool showGridInspection = true;
    private bool showGridOperations = false;
    private Vector2 tileListScroll;

    // Tile manipulation controls
    private int selectedTileState = 0; // 0=Normal, 1=Primed, 2=Blackened, 3=Enhanced
    private Vector2Int targetTilePosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;
    private int enhancementCharges = 3;

    // Grid view settings
    private bool showEmptyTiles = false;
    private bool showOnlySpecialTiles = false;
    private int maxTilesToShow = 20;

    public override void Initialize()
    {
        gridManager = GridManager.Instance;
        playerManager = Object.FindObjectOfType<PlayerManager>();

        if (playerManager != null)
        {
            targetTilePosition = playerManager.currentTilePosition;
        }
    }

    public override void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            targetTilePosition = playerManager.currentTilePosition;
        }
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showGridManagement) DrawGridManagementSection();
        if (showTileManipulation) DrawTileManipulationSection();
        if (showGridInspection) DrawGridInspectionSection();
        if (showGridOperations) DrawGridOperationsSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showGridManagement = DrawToggleButton("Grid Mgmt", showGridManagement);
        showTileManipulation = DrawToggleButton("Tile Edit", showTileManipulation);
        showGridInspection = DrawToggleButton("Inspection", showGridInspection);
        showGridOperations = DrawToggleButton("Operations", showGridOperations);
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawGridManagementSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("GRID MANAGEMENT", GUI.skin.box);

        if (gridManager != null)
        {
            // Current grid info
            GUILayout.Label($"Current Grid: {gridManager.Width}x{gridManager.Height}");
            GUILayout.Label($"Tile Size: {gridManager.TileSize}");
            GUILayout.Label($"Grid Ready: {gridManager.IsGridReady}");
            GUILayout.Label($"Playable Rows: {gridManager.GetPlayableRowCount()}/{gridManager.Height}");

            GUILayout.Space(5);

            // Grid resize controls with increment/decrement
            GUILayout.Label("Resize Grid:", GUI.skin.box);

            // Width controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Width:", GUILayout.Width(50));
            if (GUILayout.Button("-", GUILayout.Width(30)) && gridManager.Width > 3)
            {
                gridManager.ResizeGrid(gridManager.Width - 1, gridManager.Height);
            }
            GUILayout.Label($"{gridManager.Width}", GUILayout.Width(30));
            if (GUILayout.Button("+", GUILayout.Width(30)) && gridManager.Width < 20)
            {
                gridManager.ResizeGrid(gridManager.Width + 1, gridManager.Height);
            }
            GUILayout.EndHorizontal();

            // Height controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Height:", GUILayout.Width(50));
            if (GUILayout.Button("-", GUILayout.Width(30)) && gridManager.Height > 10)
            {
                gridManager.ResizeGrid(gridManager.Width, gridManager.Height - 1);
            }
            GUILayout.Label($"{gridManager.Height}", GUILayout.Width(30));
            if (GUILayout.Button("+", GUILayout.Width(30)) && gridManager.Height < 50)
            {
                gridManager.ResizeGrid(gridManager.Width, gridManager.Height + 1);
            }
            GUILayout.EndHorizontal();

            // Grid operations
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate"))
            {
                gridManager.RegenerateGrid();
            }
            if (GUILayout.Button("Debug Info"))
            {
                gridManager.DebugPrintGridInfo();
            }
            GUILayout.EndHorizontal();

            // Row management
            GUILayout.Space(5);
            GUILayout.Label("Row Management:", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Bottom Row"))
            {
                gridManager.RemoveBottomRow();
            }
            if (GUILayout.Button("Test Row Fall"))
            {
                TestRowFall();
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("GridManager not found!");
        }

        GUILayout.EndVertical();
    }

    private void DrawTileManipulationSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE MANIPULATION", GUI.skin.box);

        // Target position controls
        DrawTargetPositionControls();

        // Tile state selector
        DrawTileStateSelector();

        // Tile operations
        DrawTileOperations();

        GUILayout.EndVertical();
    }

    private void DrawTargetPositionControls()
    {
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Track Player");

        if (!autoTrackPlayer)
        {
            GUILayout.Label("X:", GUILayout.Width(15));
            if (GUILayout.Button("-", GUILayout.Width(20)) && targetTilePosition.x > 0)
                targetTilePosition.x--;
            GUILayout.Label($"{targetTilePosition.x}", GUILayout.Width(20));
            if (GUILayout.Button("+", GUILayout.Width(20)) && targetTilePosition.x < (gridManager?.Width - 1 ?? 10))
                targetTilePosition.x++;

            GUILayout.Label("Y:", GUILayout.Width(15));
            if (GUILayout.Button("-", GUILayout.Width(20)) && targetTilePosition.y > 0)
                targetTilePosition.y--;
            GUILayout.Label($"{targetTilePosition.y}", GUILayout.Width(20));
            if (GUILayout.Button("+", GUILayout.Width(20)) && targetTilePosition.y < (gridManager?.Height - 1 ?? 20))
                targetTilePosition.y++;
        }
        else
        {
            GUILayout.Label($"Target: ({targetTilePosition.x}, {targetTilePosition.y})");
        }
        GUILayout.EndHorizontal();

        // Show target tile info
        if (gridManager != null && gridManager.IsValidGridPosition(targetTilePosition))
        {
            Tile targetTile = gridManager.GetTileAt(targetTilePosition);
            if (targetTile != null)
            {
                string tileInfo = GetTileStateText(targetTile);
                GUILayout.Label($"Current State: {tileInfo}");
            }
        }
    }

    private void DrawTileStateSelector()
    {
        GUILayout.Label("Set Tile State:");
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = selectedTileState == 0 ? Color.gray : Color.white;
        if (GUILayout.Button("Normal")) selectedTileState = 0;

        GUI.backgroundColor = selectedTileState == 1 ? Color.blue : Color.white;
        if (GUILayout.Button("Primed")) selectedTileState = 1;

        GUI.backgroundColor = selectedTileState == 2 ? Color.black : Color.white;
        if (GUILayout.Button("Blackened")) selectedTileState = 2;

        GUI.backgroundColor = selectedTileState == 3 ? Color.yellow : Color.white;
        if (GUILayout.Button("Enhanced")) selectedTileState = 3;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Enhancement charges control
        if (selectedTileState == 3)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Charges:", GUILayout.Width(60));
            if (GUILayout.Button("-", GUILayout.Width(20)) && enhancementCharges > 1)
                enhancementCharges--;
            GUILayout.Label($"{enhancementCharges}", GUILayout.Width(20));
            if (GUILayout.Button("+", GUILayout.Width(20)) && enhancementCharges < 10)
                enhancementCharges++;
            GUILayout.EndHorizontal();
        }
    }

    private void DrawTileOperations()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to Target"))
        {
            ApplyTileState(targetTilePosition);
        }
        if (GUILayout.Button("Reset Target"))
        {
            ResetTile(targetTilePosition);
        }
        GUILayout.EndHorizontal();

        // Batch operations
        GUILayout.Label("Batch Operations:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to Row"))
        {
            ApplyToRow(targetTilePosition.y);
        }
        if (GUILayout.Button("Apply to Column"))
        {
            ApplyToColumn(targetTilePosition.x);
        }
        if (GUILayout.Button("Apply 3x3 Area"))
        {
            ApplyToArea(targetTilePosition, 1);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawGridInspectionSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("GRID INSPECTION", GUI.skin.box);

        // Filters and controls
        GUILayout.BeginHorizontal();
        showEmptyTiles = GUILayout.Toggle(showEmptyTiles, "Show Empty");
        showOnlySpecialTiles = GUILayout.Toggle(showOnlySpecialTiles, "Special Only");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Max:", GUILayout.Width(30));
        if (GUILayout.Button("-", GUILayout.Width(20)) && maxTilesToShow > 5)
            maxTilesToShow -= 5;
        GUILayout.Label($"{maxTilesToShow}", GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(20)) && maxTilesToShow < 100)
            maxTilesToShow += 5;
        GUILayout.EndHorizontal();

        // Tile statistics
        if (gridManager != null)
        {
            var stats = GetTileStatistics();
            GUILayout.Label($"Total: {stats.total} | Marked: {stats.marked} | Special: {stats.special} | Fallen: {stats.fallen}");
        }

        // Tile list
        tileListScroll = GUILayout.BeginScrollView(tileListScroll, GUILayout.MaxHeight(300));
        DrawTileList();
        GUILayout.EndScrollView();

        GUILayout.EndVertical();
    }

    private void DrawGridOperationsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("GRID OPERATIONS", GUI.skin.box);

        // Marker operations
        GUILayout.Label("Markers:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Markers"))
        {
            gridManager?.ClearAllMarkers();
        }
        if (GUILayout.Button("Test Diagonal Pattern"))
        {
            PlaceTestMarkerPattern();
        }
        GUILayout.EndHorizontal();

        // Tile state operations
        GUILayout.Label("Tile States:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All Tiles"))
        {
            ResetAllTiles();
        }
        if (GUILayout.Button("Blacken Border"))
        {
            BlackenBorderTiles();
        }
        GUILayout.EndHorizontal();

        // Pattern operations - simplified to one type each
        GUILayout.Label("Patterns:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prime Cross"))
        {
            CreateCrossPattern(TileState.Transformed); // Will use PrimeTile
        }
        if (GUILayout.Button("Blacken Checkerboard"))
        {
            CreateCheckerboardPattern(TileState.Transformed); // Will use BlackenTile  
        }
        if (GUILayout.Button("Enhance Top Row"))
        {
            EnhanceTopRow();
        }
        GUILayout.EndHorizontal();

        // Restore operations
        GUILayout.Label("Restore:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restore All Fallen"))
        {
            RestoreAllFallenTiles();
        }
        if (GUILayout.Button("Test Grid Bounds"))
        {
            TestGridBounds();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawTileList()
    {
        if (gridManager == null) return;

        var tiles = GetFilteredTiles();
        int shown = 0;

        foreach (var tile in tiles)
        {
            if (shown >= maxTilesToShow) break;

            DrawTileListItem(tile);
            shown++;
        }

        if (tiles.Count > maxTilesToShow)
        {
            GUILayout.Label($"... and {tiles.Count - maxTilesToShow} more tiles");
        }
    }

    private void DrawTileListItem(Tile tile)
    {
        Vector2Int position = new Vector2Int(tile.x, tile.y);
        bool isTarget = position == targetTilePosition;
        GUI.backgroundColor = isTarget ? Color.yellow : GetTileStateColor(tile);

        GUILayout.BeginVertical(GUI.skin.box);

        // Header line
        GUILayout.BeginHorizontal();
        GUILayout.Label($"({tile.x},{tile.y})", GUILayout.Width(60));

        string stateText = GetTileStateText(tile);
        GUILayout.Label(stateText, GUILayout.Width(120));

        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            targetTilePosition = position;
            autoTrackPlayer = false;
        }
        GUILayout.EndHorizontal();

        // Quick actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("R", GUILayout.Width(25))) // Reset
        {
            ResetTile(position);
        }
        if (GUILayout.Button("M", GUILayout.Width(25))) // Mark
        {
            gridManager.PlaceMarker(tile.x, tile.y);
        }
        if (GUILayout.Button("B", GUILayout.Width(25))) // Blacken
        {
            tile.BlackenTile();
        }
        if (GUILayout.Button("E", GUILayout.Width(25))) // Enhance
        {
            tile.AdvantageTile(enhancementCharges);
        }
        if (GUILayout.Button("P", GUILayout.Width(25))) // Prime
        {
            tile.PrimeTile();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
        GUILayout.Space(2);
    }

    // Helper methods for tile operations
    private void ApplyTileState(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        switch (selectedTileState)
        {
            case 0: // Normal
                ResetTile(position);
                break;
            case 1: // Primed
                tile.PrimeTile();
                break;
            case 2: // Blackened
                tile.BlackenTile();
                break;
            case 3: // Enhanced
                tile.AdvantageTile(enhancementCharges);
                break;
        }

        Debug.Log($"Applied tile state {selectedTileState} to ({position.x}, {position.y})");
    }

    private void ResetTile(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile != null)
        {
            tile.ResetTile();
            tile.RestoreTile(); // In case it was fallen
        }
    }

    private void ApplyToRow(int row)
    {
        if (gridManager == null) return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            ApplyTileState(new Vector2Int(x, row));
        }
        Debug.Log($"Applied tile state to row {row}");
    }

    private void ApplyToColumn(int column)
    {
        if (gridManager == null) return;

        for (int y = 0; y < gridManager.Height; y++)
        {
            ApplyTileState(new Vector2Int(column, y));
        }
        Debug.Log($"Applied tile state to column {column}");
    }

    private void ApplyToArea(Vector2Int center, int radius)
    {
        if (gridManager == null) return;

        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (gridManager.IsValidGridPosition(pos))
                {
                    ApplyTileState(pos);
                }
            }
        }
        Debug.Log($"Applied tile state to 3x3 area around ({center.x}, {center.y})");
    }

    // Grid operations
    private void TestRowFall()
    {
        if (gridManager == null) return;

        // Make tiles in row 1 fall for testing
        for (int x = 0; x < gridManager.Width; x++)
        {
            Tile tile = gridManager.GetTileAt(x, 1);
            if (tile != null)
            {
                tile.MakeTileFall();
            }
        }
        Debug.Log("Made row 1 fall for testing");
    }

    private void ResetAllTiles()
    {
        if (gridManager == null) return;

        int resetCount = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null)
                {
                    tile.ResetTile();
                    tile.RestoreTile();
                    resetCount++;
                }
            }
        }
        Debug.Log($"Reset {resetCount} tiles to normal state");
    }

    private void BlackenBorderTiles()
    {
        if (gridManager == null) return;

        int blackenedCount = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                if (x == 0 || x == gridManager.Width - 1 || y == 0 || y == gridManager.Height - 1)
                {
                    Tile tile = gridManager.GetTileAt(x, y);
                    if (tile != null)
                    {
                        tile.BlackenTile();
                        blackenedCount++;
                    }
                }
            }
        }
        Debug.Log($"Blackened {blackenedCount} border tiles");
    }

    private void EnhanceTopRow()
    {
        if (gridManager == null) return;

        int enhancedCount = 0;
        int topRow = gridManager.Height - 1;
        for (int x = 0; x < gridManager.Width; x++)
        {
            Tile tile = gridManager.GetTileAt(x, topRow);
            if (tile != null)
            {
                tile.AdvantageTile(enhancementCharges);
                enhancedCount++;
            }
        }
        Debug.Log($"Enhanced {enhancedCount} tiles in top row with {enhancementCharges} charges");
    }

    private void CreateCheckerboardPattern(TileState stateType)
    {
        if (gridManager == null) return;

        int patternCount = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    Tile tile = gridManager.GetTileAt(x, y);
                    if (tile != null)
                    {
                        tile.BlackenTile(); // Only blacken tiles in checkerboard pattern
                        patternCount++;
                    }
                }
            }
        }
        Debug.Log($"Created checkerboard pattern on {patternCount} tiles");
    }

    private void CreateCrossPattern(TileState stateType)
    {
        if (gridManager == null) return;

        int centerX = gridManager.Width / 2;
        int centerY = gridManager.Height / 2;
        int patternCount = 0;

        // Horizontal line
        for (int x = 0; x < gridManager.Width; x++)
        {
            Tile tile = gridManager.GetTileAt(x, centerY);
            if (tile != null)
            {
                tile.PrimeTile();
                patternCount++;
            }
        }

        // Vertical line
        for (int y = 0; y < gridManager.Height; y++)
        {
            Tile tile = gridManager.GetTileAt(centerX, y);
            if (tile != null)
            {
                tile.PrimeTile();
                patternCount++;
            }
        }
        Debug.Log($"Created cross pattern with {patternCount} primed tiles");
    }

    private void RestoreAllFallenTiles()
    {
        if (gridManager == null) return;

        int restoredCount = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null && !tile.IsPlayable)
                {
                    tile.RestoreTile();
                    restoredCount++;
                }
            }
        }
        Debug.Log($"Restored {restoredCount} fallen tiles");
    }

    private void PlaceTestMarkerPattern()
    {
        if (gridManager == null) return;

        // Place markers in a diagonal pattern
        int markersPlaced = 0;
        for (int i = 0; i < Mathf.Min(gridManager.Width, gridManager.Height); i++)
        {
            if (gridManager.PlaceMarker(i, i))
            {
                markersPlaced++;
            }
        }
        Debug.Log($"Placed {markersPlaced} test markers in diagonal pattern");
    }

    private void TestGridBounds()
    {
        if (gridManager == null) return;

        Debug.Log("=== GRID BOUNDS TEST ===");
        Debug.Log($"Grid Size: {gridManager.Width}x{gridManager.Height}");
        Debug.Log($"World Bounds: {gridManager.MinWorldBounds} to {gridManager.MaxWorldBounds}");

        // Test coordinate conversion
        Vector2Int testGrid = new Vector2Int(1, 5);
        Vector3 worldPos = gridManager.GridToWorldPosition(testGrid.x, testGrid.y);
        Vector2Int backToGrid = gridManager.WorldToGridPosition(worldPos);

        Debug.Log($"Grid ({testGrid.x}, {testGrid.y}) -> World {worldPos} -> Grid ({backToGrid.x}, {backToGrid.y})");

        // Test boundary positions
        Vector2Int[] testPositions = {
            new Vector2Int(0, 0),
            new Vector2Int(gridManager.Width - 1, gridManager.Height - 1),
            new Vector2Int(-1, -1),
            new Vector2Int(gridManager.Width, gridManager.Height)
        };

        foreach (var pos in testPositions)
        {
            bool isValid = gridManager.IsValidGridPosition(pos);
            Debug.Log($"Position ({pos.x}, {pos.y}): Valid = {isValid}");
        }
    }

    // Utility methods
    private List<Tile> GetFilteredTiles()
    {
        var tiles = new List<Tile>();

        if (gridManager == null) return tiles;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile == null) continue;

                // Apply filters
                bool isSpecial = tile.HasMarker || tile.IsBlackened || tile.IsAdvantaged ||
                               tile.IsPrimed || tile.CanPaintCubes || !tile.IsPlayable;

                if (showOnlySpecialTiles && !isSpecial) continue;
                if (!showEmptyTiles && !isSpecial) continue;

                tiles.Add(tile);
            }
        }

        return tiles.OrderBy(t => t.y).ThenBy(t => t.x).ToList();
    }

    private (int total, int marked, int special, int fallen) GetTileStatistics()
    {
        int total = 0, marked = 0, special = 0, fallen = 0;

        if (gridManager == null) return (total, marked, special, fallen);

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null)
                {
                    total++;
                    if (tile.HasMarker) marked++;
                    if (!tile.IsPlayable) fallen++;
                    if (tile.IsBlackened || tile.IsAdvantaged || tile.IsPrimed || tile.CanPaintCubes)
                        special++;
                }
            }
        }

        return (total, marked, special, fallen);
    }

    private Color GetTileStateColor(Tile tile)
    {
        if (!tile.IsPlayable) return new Color(0.5f, 0.5f, 0.5f); // Gray for fallen
        if (tile.IsBlackened) return new Color(0.3f, 0.3f, 0.3f); // Dark gray
        if (tile.IsAdvantaged) return new Color(1f, 1f, 0.3f); // Yellow
        if (tile.IsPrimed) return new Color(0.3f, 0.6f, 1f); // Blue
        if (tile.HasMarker) return new Color(1f, 0.3f, 0.3f); // Red
        if (tile.CanPaintCubes) return new Color(0.8f, 0.4f, 0.8f); // Purple
        return Color.white;
    }

    private string GetTileStateText(Tile tile)
    {
        if (!tile.IsPlayable) return "FALLEN";
        if (tile.IsBlackened) return "Blackened";
        if (tile.IsAdvantaged) return $"Enhanced({tile.DetonationCharges})";
        if (tile.IsPrimed) return "Primed";
        if (tile.HasMarker) return "Marked";
        if (tile.CanPaintCubes) return $"Painter({tile.PaintStatus})";
        return "Normal";
    }
}