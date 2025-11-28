using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Grid Designer - Tile manipulation and grid layout.
/// </summary>
public class GridDesigner : PrototypingPanelBase
{
    public override string PanelName => "Grid";
    public override string PanelIcon => "🎨";
    public override PrototypingCategory Category => PrototypingCategory.Grid;
    public override int Priority => 20;
    
    private TileState selectedState = TileState.Normal;
    private Vector2Int targetPos = Vector2Int.zero;
    
    private bool showResize = true;
    private bool showTiles = true;
    private bool showPatterns = true;
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("Reset", ResetAll) { Group = QuickActionGroup.Grid, Priority = 20, Tooltip = "Reset tiles" }
        };
    }
    
    public override void DrawGUI()
    {
        string info = gridManager != null 
            ? $"Grid: {gridManager.Width}x{gridManager.Height} | Ready: {gridManager.IsGridReady}"
            : "Grid not available";
        DrawStatus(info);
        
        GUILayout.Space(5);
        
        // Resize
        showResize = DrawToggleSection("RESIZE GRID", showResize);
        if (showResize)
        {
            DrawSection("", () =>
            {
                int w = gridManager?.Width ?? 10;
                int h = gridManager?.Height ?? 20;
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Width:", GUILayout.Width(50));
                if (GUILayout.Button("-", GUILayout.Width(25)) && w > 3) Resize(w - 1, h);
                GUILayout.Label($"{w}", GUILayout.Width(30));
                if (GUILayout.Button("+", GUILayout.Width(25)) && w < 20) Resize(w + 1, h);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Height:", GUILayout.Width(50));
                if (GUILayout.Button("-", GUILayout.Width(25)) && h > 10) Resize(w, h - 1);
                GUILayout.Label($"{h}", GUILayout.Width(30));
                if (GUILayout.Button("+", GUILayout.Width(25)) && h < 50) Resize(w, h + 1);
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                GUILayout.Label("Presets:");
                DrawButtonRow(
                    ("6x15", () => Resize(6, 15)),
                    ("10x20", () => Resize(10, 20)),
                    ("15x30", () => Resize(15, 30))
                );
            });
        }
        
        // Tile State
        showTiles = DrawToggleSection("TILE STATE", showTiles);
        if (showTiles)
        {
            DrawSection("", () =>
            {
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = selectedState == TileState.Normal ? Color.cyan : Color.white;
                if (GUILayout.Button("Normal")) selectedState = TileState.Normal;
                GUI.backgroundColor = selectedState == TileState.Transformed ? Color.cyan : Color.white;
                if (GUILayout.Button("Primed")) selectedState = TileState.Transformed;
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                GUILayout.Label($"Target: ({targetPos.x}, {targetPos.y})");
                
                int maxX = (gridManager?.Width ?? 10) - 1;
                int maxY = (gridManager?.Height ?? 20) - 1;
                targetPos.x = DrawIntStepper("X", targetPos.x, 0, maxX);
                targetPos.y = DrawIntStepper("Y", targetPos.y, 0, maxY);
                
                DrawButtonRow(
                    ("Apply", () => ApplyState(targetPos)),
                    ("Row", () => ApplyRow(targetPos.y)),
                    ("Column", () => ApplyColumn(targetPos.x))
                );
            });
        }
        
        // Patterns
        showPatterns = DrawToggleSection("PATTERNS", showPatterns);
        if (showPatterns)
        {
            DrawSection("", () =>
            {
                DrawButtonRow(
                    ("Checkerboard", Checkerboard),
                    ("Cross", Cross),
                    ("Border", Border)
                );
                DrawButtonRow(
                    ("Diagonal", Diagonal),
                    ("Random", RandomPattern),
                    ("Clear All", ResetAll)
                );
                
                GUILayout.Space(5);
                DrawButtonRow(
                    ("Regenerate", () => gridManager?.RegenerateGrid()),
                    ("Debug Info", () => gridManager?.DebugPrintGridInfo())
                );
            });
        }
    }
    
    #region Actions
    private void Resize(int w, int h)
    {
        gridManager?.ResizeGrid(w, h);
    }
    
    private void ApplyState(Vector2Int pos)
    {
        var tile = gridManager?.GetTileAt(pos);
        if (tile == null) return;
        
        if (selectedState == TileState.Normal)
            tile.ResetTile();
        else
            tile.PrimeTile();
    }
    
    private void ApplyRow(int y)
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
            ApplyState(new Vector2Int(x, y));
    }
    
    private void ApplyColumn(int x)
    {
        if (gridManager == null) return;
        for (int y = 0; y < gridManager.Height; y++)
            ApplyState(new Vector2Int(x, y));
    }
    
    private void ResetAll()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
            for (int y = 0; y < gridManager.Height; y++)
                gridManager.GetTileAt(x, y)?.ResetTile();
    }
    
    private void Checkerboard()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
            for (int y = 0; y < gridManager.Height; y++)
                if ((x + y) % 2 == 0)
                    gridManager.GetTileAt(x, y)?.PrimeTile();
    }
    
    private void Cross()
    {
        if (gridManager == null) return;
        int cx = gridManager.Width / 2;
        int cy = gridManager.Height / 2;
        for (int x = 0; x < gridManager.Width; x++)
            gridManager.GetTileAt(x, cy)?.PrimeTile();
        for (int y = 0; y < gridManager.Height; y++)
            gridManager.GetTileAt(cx, y)?.PrimeTile();
    }
    
    private void Border()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
        {
            gridManager.GetTileAt(x, 0)?.PrimeTile();
            gridManager.GetTileAt(x, gridManager.Height - 1)?.PrimeTile();
        }
        for (int y = 0; y < gridManager.Height; y++)
        {
            gridManager.GetTileAt(0, y)?.PrimeTile();
            gridManager.GetTileAt(gridManager.Width - 1, y)?.PrimeTile();
        }
    }
    
    private void Diagonal()
    {
        if (gridManager == null) return;
        int min = Mathf.Min(gridManager.Width, gridManager.Height);
        for (int i = 0; i < min; i++)
            gridManager.GetTileAt(i, i)?.PrimeTile();
    }
    
    private void RandomPattern()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
            for (int y = 0; y < gridManager.Height; y++)
                if (Random.value < 0.2f)
                    gridManager.GetTileAt(x, y)?.PrimeTile();
    }
    #endregion
}
