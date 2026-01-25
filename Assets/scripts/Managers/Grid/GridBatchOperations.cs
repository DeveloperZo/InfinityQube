using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles batch tile operations for the grid.
/// Extracted from GridManager as part of SRP refactoring.
/// GridManager maintains facade methods that delegate to this helper.
/// </summary>
public class GridBatchOperations
{
    #region References
    private readonly GridManager grid;
    private readonly bool enableDebugLogs;
    #endregion

    #region Constructor
    public GridBatchOperations(GridManager gridManager, bool debugLogs = false)
    {
        grid = gridManager;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Batch State Operations
    /// <summary>
    /// Applies tile states to multiple tiles in batch.
    /// </summary>
    public void BatchSetTileStates(Dictionary<Vector2Int, TileState> stateMap)
    {
        if (grid.tiles == null || stateMap == null) return;

        int appliedCount = 0;
        foreach (var kvp in stateMap)
        {
            Vector2Int pos = kvp.Key;
            TileState state = kvp.Value;
            
            Tile tile = grid.GetTileAt(pos);
            if (tile != null && tile.IsPlayable)
            {
                ApplyTileState(tile, state);
                appliedCount++;
            }
        }

        DebugLog($"Batch applied tile states to {appliedCount}/{stateMap.Count} tiles");
    }

    /// <summary>
    /// Applies a tile state pattern to the grid.
    /// </summary>
    public void ApplyTileStatePattern(TileStatePattern pattern)
    {
        if (pattern == null || grid.tiles == null) return;

        Dictionary<Vector2Int, TileState> stateMap = new Dictionary<Vector2Int, TileState>();
        
        foreach (var entry in pattern.entries)
        {
            Vector2Int pos = pattern.basePosition + entry.offset;
            if (grid.IsValidGridPosition(pos))
            {
                stateMap[pos] = entry.state;
            }
        }

        BatchSetTileStates(stateMap);
        DebugLog($"Applied tile state pattern '{pattern.name}' with {pattern.entries.Count} entries");
    }
    #endregion

    #region Preset Operations
    /// <summary>
    /// Creates a tile state preset from current grid state.
    /// </summary>
    public TileStatePreset CreateTileStatePreset(string presetName, List<Vector2Int> positions)
    {
        TileStatePreset preset = new TileStatePreset
        {
            name = presetName,
            entries = new List<TileStateEntry>()
        };

        foreach (var pos in positions)
        {
            Tile tile = grid.GetTileAt(pos);
            if (tile != null)
            {
                preset.entries.Add(new TileStateEntry
                {
                    position = pos,
                    state = tile.currentState,
                    hasMarker = tile.HasMarker,
                    isBlackened = tile.IsBlackened,
                    isMatrixd = tile.IsMatrixd
                });
            }
        }

        DebugLog($"Created tile state preset '{presetName}' with {preset.entries.Count} entries");
        return preset;
    }

    /// <summary>
    /// Restores grid state from a preset.
    /// </summary>
    public void RestoreFromPreset(TileStatePreset preset)
    {
        if (preset == null || grid.tiles == null) return;

        int restoredCount = 0;
        foreach (var entry in preset.entries)
        {
            Tile tile = grid.GetTileAt(entry.position);
            if (tile != null && tile.IsPlayable)
            {
                RestoreTileFromEntry(tile, entry);
                restoredCount++;
            }
        }

        DebugLog($"Restored {restoredCount}/{preset.entries.Count} tiles from preset '{preset.name}'");
    }
    #endregion

    #region Batch Marker Operations
    /// <summary>
    /// Batch operations for markers.
    /// </summary>
    public void BatchSetMarkers(List<Vector2Int> positions, bool placeMarkers)
    {
        if (grid.tiles == null || positions == null) return;

        int processedCount = 0;
        foreach (var pos in positions)
        {
            bool success = placeMarkers ? grid.PlaceMarker(pos.x, pos.y) : grid.RemoveMarker(pos.x, pos.y);
            if (success) processedCount++;
        }

        string action = placeMarkers ? "placed" : "removed";
        DebugLog($"Batch {action} markers: {processedCount}/{positions.Count} successful");
    }
    #endregion

    #region Batch Transform Operations
    /// <summary>
    /// Batch tile transformation operations.
    /// </summary>
    public void BatchTransformTiles(List<Vector2Int> positions, CubeType transformType)
    {
        if (grid.tiles == null || positions == null) return;

        int transformedCount = 0;
        foreach (var pos in positions)
        {
            Tile tile = grid.GetTileAt(pos);
            if (tile != null && tile.IsPlayable)
            {
                tile.TransformTile(transformType);
                transformedCount++;
            }
        }

        DebugLog($"Batch transformed {transformedCount}/{positions.Count} tiles to {transformType} type");
    }

    /// <summary>
    /// Reset multiple tiles to normal state.
    /// </summary>
    public void BatchResetTiles(List<Vector2Int> positions)
    {
        if (grid.tiles == null || positions == null) return;

        int resetCount = 0;
        foreach (var pos in positions)
        {
            Tile tile = grid.GetTileAt(pos);
            if (tile != null)
            {
                tile.ResetTile();
                resetCount++;
            }
        }

        DebugLog($"Batch reset {resetCount}/{positions.Count} tiles to normal state");
    }
    #endregion

    #region Query Operations
    /// <summary>
    /// Get tiles in a rectangular area.
    /// </summary>
    public List<Tile> GetTilesInArea(Vector2Int topLeft, Vector2Int bottomRight)
    {
        List<Tile> tilesInArea = new List<Tile>();
        
        for (int x = topLeft.x; x <= bottomRight.x; x++)
        {
            for (int y = topLeft.y; y <= bottomRight.y; y++)
            {
                Tile tile = grid.GetTileAt(x, y);
                if (tile != null)
                {
                    tilesInArea.Add(tile);
                }
            }
        }

        return tilesInArea;
    }

    /// <summary>
    /// Get tiles matching specific criteria.
    /// </summary>
    public List<Tile> GetTilesWithState(TileState state)
    {
        List<Tile> matchingTiles = new List<Tile>();
        
        if (grid.tiles == null) return matchingTiles;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Tile tile = grid.GetTileAt(x, y);
                if (tile != null && tile.currentState == state)
                {
                    matchingTiles.Add(tile);
                }
            }
        }

        return matchingTiles;
    }

    /// <summary>
    /// Get all tiles with markers.
    /// </summary>
    public List<Tile> GetMarkedTiles()
    {
        List<Tile> markedTiles = new List<Tile>();
        
        if (grid.tiles == null) return markedTiles;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Tile tile = grid.GetTileAt(x, y);
                if (tile != null && tile.HasMarker)
                {
                    markedTiles.Add(tile);
                }
            }
        }

        return markedTiles;
    }
    #endregion

    #region Helper Methods
    private void ApplyTileState(Tile tile, TileState state)
    {
        switch (state)
        {
            case TileState.Normal:
                tile.ResetTile();
                break;
            case TileState.Transformed:
                // Keep existing transformation
                break;
        }
    }

    private void RestoreTileFromEntry(Tile tile, TileStateEntry entry)
    {
        // Reset tile first
        tile.ResetTile();
        
        // Apply saved state
        if (entry.isBlackened)
        {
            tile.BlackenTile();
        }
        else if (entry.isMatrixd)
        {
            tile.MatrixTile();
        }
        
        // Apply marker if needed
        if (entry.hasMarker && tile.CanBeMarked)
        {
            tile.PlaceMarker();
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GridBatchOperations] {message}");
        }
    }
    #endregion
}
