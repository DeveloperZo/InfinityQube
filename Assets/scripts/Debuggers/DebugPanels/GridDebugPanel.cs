using UnityEngine;

public class GridDebugPanel : DebugPanelBase
{
    public string PanelName => "Grid";
    public override DebugPanelGroup PanelGroup => DebugPanelGroup.Grid;

    private GridManager gridManager;

    public void Initialize()
    {
        gridManager = GridManager.Instance;
    }

    public void Update()
    {
        // Update logic if needed
    }

    public void DrawPanel()
    {
        DrawGridInfo();
        GUILayout.Space(10);
        DrawGridControls();
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
        }
    }

    private void DrawGridControls()
    {
        GUILayout.Label("GRID CONTROLS", GUI.skin.box);

        if (GUILayout.Button("Print Grid Info"))
        {
            gridManager?.DebugPrintGridInfo();
        }

        if (GUILayout.Button("Clear All Markers"))
        {
            gridManager?.ClearAllMarkers();
        }
    }
}