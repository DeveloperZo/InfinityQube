using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Player Panel - Marker and player control.
/// </summary>
public class PlayerPanel : PrototypingPanelBase
{
    public override string PanelName => "Player";
    public override string PanelIcon => "👤";
    public override PrototypingCategory Category => PrototypingCategory.Player;
    public override int Priority => 30;
    
    private Vector2Int targetPos = Vector2Int.zero;
    private bool trackPlayer = true;
    
    private bool showMarkers = true;
    private bool showPlayer = true;
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("M", PlaceMarker) { Group = QuickActionGroup.Player, Priority = 30, Tooltip = "Place marker" },
            new QuickAction("🧹", ClearMarkers) { Group = QuickActionGroup.Player, Priority = 35, Tooltip = "Clear markers" }
        };
    }
    
    public override void Update()
    {
        if (trackPlayer && playerManager != null)
        {
            targetPos = playerManager.currentTilePosition;
        }
    }
    
    public override void DrawGUI()
    {
        string info = playerManager != null 
            ? $"Player: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})"
            : "Player not available";
        DrawStatus(info);
        
        GUILayout.Space(5);
        
        // Markers
        showMarkers = DrawToggleSection("MARKERS", showMarkers);
        if (showMarkers)
        {
            DrawSection("", () =>
            {
                trackPlayer = GUILayout.Toggle(trackPlayer, "Track Player Position");
                
                if (!trackPlayer)
                {
                    int maxX = (gridManager?.Width ?? 10) - 1;
                    int maxY = (gridManager?.Height ?? 20) - 1;
                    targetPos.x = DrawIntStepper("X", targetPos.x, 0, maxX);
                    targetPos.y = DrawIntStepper("Y", targetPos.y, 0, maxY);
                }
                else
                {
                    GUILayout.Label($"Target: ({targetPos.x}, {targetPos.y})");
                }
                
                GUILayout.Space(5);
                DrawButtonRow(
                    ("Place Marker", PlaceMarker),
                    ("Clear All", ClearMarkers)
                );
                
                GUILayout.Space(5);
                GUILayout.Label("Quick Place (row 1):");
                GUILayout.BeginHorizontal();
                int width = gridManager?.Width ?? 10;
                for (int x = 0; x < Mathf.Min(width, 12); x++)
                {
                    int col = x;
                    if (GUILayout.Button($"{x}", GUILayout.Width(28)))
                    {
                        gridManager?.PlaceMarker(col, 1);
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                GUILayout.Label("Patterns:");
                DrawButtonRow(
                    ("Diagonal", DiagonalPattern),
                    ("Grid", GridPattern),
                    ("Random 5", Random5)
                );
            });
        }
        
        // Player Control
        showPlayer = DrawToggleSection("PLAYER CONTROL", showPlayer);
        if (showPlayer)
        {
            DrawSection("", () =>
            {
                GUILayout.Label("Teleport:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("←")) Teleport(-1, 0);
                if (GUILayout.Button("→")) Teleport(1, 0);
                if (GUILayout.Button("↓")) Teleport(0, -1);
                if (GUILayout.Button("↑")) Teleport(0, 1);
                if (GUILayout.Button("Center")) TeleportCenter();
                GUILayout.EndHorizontal();
            });
        }
    }
    
    #region Actions
    private void PlaceMarker()
    {
        gridManager?.PlaceMarker(targetPos.x, targetPos.y);
    }
    
    private void ClearMarkers()
    {
        gridManager?.ClearAllMarkers();
    }
    
    private void DiagonalPattern()
    {
        if (gridManager == null) return;
        int min = Mathf.Min(gridManager.Width, gridManager.Height);
        for (int i = 0; i < min; i++)
            gridManager.PlaceMarker(i, i);
    }
    
    private void GridPattern()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x += 3)
            for (int y = 0; y < gridManager.Height; y += 3)
                gridManager.PlaceMarker(x, y);
    }
    
    private void Random5()
    {
        if (gridManager == null) return;
        for (int i = 0; i < 5; i++)
            gridManager.PlaceMarker(Random.Range(0, gridManager.Width), Random.Range(0, gridManager.Height));
    }
    
    private void Teleport(int dx, int dy)
    {
        if (playerManager == null || gridManager == null) return;
        var newPos = playerManager.currentTilePosition + new Vector2Int(dx, dy);
        newPos.x = Mathf.Clamp(newPos.x, 0, gridManager.Width - 1);
        newPos.y = Mathf.Clamp(newPos.y, 0, gridManager.Height - 1);
        
        playerManager.currentTilePosition = newPos;
        playerManager.transform.position = gridManager.GridToWorldPosition(newPos.x, newPos.y, 0);
    }
    
    private void TeleportCenter()
    {
        if (playerManager == null || gridManager == null) return;
        int x = gridManager.Width / 2;
        playerManager.currentTilePosition = new Vector2Int(x, 0);
        playerManager.transform.position = gridManager.GridToWorldPosition(x, 0, 0);
    }
    #endregion
}
