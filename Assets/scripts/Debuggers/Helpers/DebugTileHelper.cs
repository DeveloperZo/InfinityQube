using UnityEngine;
using static Enumerations;

/// <summary>
/// Shared utility class for tile painting and manipulation operations in debug panels.
/// Eliminates code duplication and ensures consistent tile handling behavior.
/// </summary>
public static class DebugTileHelper
{
    #region Tile Face Painting

    /// <summary>
    /// Sets up face painting on a tile at the specified position.
    /// </summary>
    /// <param name="position">Grid position of the tile</param>
    /// <param name="status">Face status to apply when cubes pass through</param>
    /// <param name="color">Color for the face painting</param>
    /// <param name="duration">Duration of the face painting effect (-1 for permanent)</param>
    /// <param name="gridManager">GridManager instance for tile access</param>
    /// <param name="paintOnLanding">Whether to paint when cube lands on tile</param>
    /// <param name="paintOnExit">Whether to paint when cube exits tile</param>
    /// <returns>True if tile painting was set up successfully</returns>
    public static bool SetupTilePainting(Vector2Int position, FaceStatus status, Color color, int duration, 
                                        GridManager gridManager, bool paintOnLanding = true, bool paintOnExit = false)
    {
        if (gridManager == null)
        {
            Debug.LogError("DebugTileHelper: GridManager is null");
            return false;
        }

        if (!gridManager.IsValidGridPosition(position))
        {
            Debug.LogWarning($"DebugTileHelper: Invalid grid position ({position.x}, {position.y})");
            return false;
        }

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
        {
            Debug.LogWarning($"DebugTileHelper: No tile found at position ({position.x}, {position.y})");
            return false;
        }

        tile.SetupFacePainting(status, color, duration, paintOnLanding, paintOnExit);
        Debug.Log($"DebugTileHelper: Setup face painting at ({position.x}, {position.y}) - Status: {status}, Duration: {duration}");
        return true;
    }

    /// <summary>
    /// Clears face painting from a tile at the specified position.
    /// </summary>
    /// <param name="position">Grid position of the tile</param>
    /// <param name="gridManager">GridManager instance for tile access</param>
    /// <returns>True if tile painting was cleared successfully</returns>
    public static bool ClearTilePainting(Vector2Int position, GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError("DebugTileHelper: GridManager is null");
            return false;
        }

        if (!gridManager.IsValidGridPosition(position))
        {
            Debug.LogWarning($"DebugTileHelper: Invalid grid position ({position.x}, {position.y})");
            return false;
        }

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
        {
            Debug.LogWarning($"DebugTileHelper: No tile found at position ({position.x}, {position.y})");
            return false;
        }

        tile.DisableFacePainting();
        Debug.Log($"DebugTileHelper: Cleared face painting at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Clears face painting from all tiles in the grid.
    /// </summary>
    /// <param name="gridManager">GridManager instance</param>
    /// <returns>Number of tiles that had face painting cleared</returns>
    public static int ClearAllTilePainting(GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError("DebugTileHelper: GridManager is null");
            return 0;
        }

        int clearedCount = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                if (tile != null && tile.CanPaintCubes)
                {
                    tile.DisableFacePainting();
                    clearedCount++;
                }
            }
        }

        Debug.Log($"DebugTileHelper: Cleared face painting from {clearedCount} tiles");
        return clearedCount;
    }

    #endregion

    #region Tile Pattern Operations

    /// <summary>
    /// Creates a face painting pattern around a center position.
    /// </summary>
    /// <param name="center">Center position for the pattern</param>
    /// <param name="gridManager">GridManager instance</param>
    /// <param name="playerManager">PlayerManager for position reference (optional)</param>
    /// <returns>Number of tiles painted</returns>
    public static int CreateFacePaintPattern(Vector2Int center, GridManager gridManager, PlayerManager playerManager = null)
    {
        if (gridManager == null) return 0;

        // Use player position if no center specified and player is available
        if (center == Vector2Int.zero && playerManager != null)
        {
            center = playerManager.currentTilePosition;
        }

        int painted = 0;

        // Create a cross pattern with different face statuses
        if (SetupTilePainting(new Vector2Int(center.x - 2, center.y), FaceStatus.Corrupted, Color.red, 3, gridManager))
            painted++;
        
        if (SetupTilePainting(new Vector2Int(center.x + 2, center.y), FaceStatus.Enhanced, Color.blue, 3, gridManager))
            painted++;
        
        if (SetupTilePainting(new Vector2Int(center.x, center.y + 2), FaceStatus.Corrupted, Color.red, 5, gridManager))
            painted++;

        Debug.Log($"DebugTileHelper: Created face paint pattern - {painted} tiles painted");
        return painted;
    }

    /// <summary>
    /// Sets up face painting on a row of tiles.
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="status">Face status to apply</param>
    /// <param name="color">Color for face painting</param>
    /// <param name="duration">Duration of effect</param>
    /// <param name="gridManager">GridManager instance</param>
    /// <returns>Number of tiles painted</returns>
    public static int SetupRowFacePainting(int row, FaceStatus status, Color color, int duration, GridManager gridManager)
    {
        if (gridManager == null) return 0;

        int painted = 0;
        for (int x = 0; x < gridManager.Width; x++)
        {
            if (SetupTilePainting(new Vector2Int(x, row), status, color, duration, gridManager))
            {
                painted++;
            }
        }

        Debug.Log($"DebugTileHelper: Set up face painting on row {row} - {painted} tiles painted");
        return painted;
    }

    /// <summary>
    /// Sets up face painting on a column of tiles.
    /// </summary>
    /// <param name="column">Column index</param>
    /// <param name="status">Face status to apply</param>
    /// <param name="color">Color for face painting</param>
    /// <param name="duration">Duration of effect</param>
    /// <param name="gridManager">GridManager instance</param>
    /// <returns>Number of tiles painted</returns>
    public static int SetupColumnFacePainting(int column, FaceStatus status, Color color, int duration, GridManager gridManager)
    {
        if (gridManager == null) return 0;

        int painted = 0;
        for (int y = 0; y < gridManager.Height; y++)
        {
            if (SetupTilePainting(new Vector2Int(column, y), status, color, duration, gridManager))
            {
                painted++;
            }
        }

        Debug.Log($"DebugTileHelper: Set up face painting on column {column} - {painted} tiles painted");
        return painted;
    }

    #endregion

    #region Tile State Utilities

    /// <summary>
    /// Gets a descriptive string for a tile's current state.
    /// </summary>
    /// <param name="tile">Tile to describe</param>
    /// <returns>Human-readable tile state description</returns>
    public static string GetTileStateDescription(Tile tile)
    {
        if (tile == null) return "NULL";

        if (!tile.IsPlayable) return "FALLEN";
        if (tile.IsBlackened) return "Blackened";
        if (tile.IsPrimed) return "Primed";
        if (tile.HasMarker) return "Marked";
        if (tile.CanPaintCubes) return $"Painter({tile.PaintStatus})";
        
        return "Normal";
    }

    /// <summary>
    /// Gets a color for UI display based on tile state.
    /// </summary>
    /// <param name="tile">Tile to get color for</param>
    /// <returns>Color for UI display</returns>
    public static Color GetTileDisplayColor(Tile tile)
    {
        if (tile == null) return Color.gray;

        if (!tile.IsPlayable) return new Color(0.5f, 0.5f, 0.5f); // Gray for fallen
        if (tile.IsBlackened) return new Color(0.3f, 0.3f, 0.3f); // Dark gray
        if (tile.IsPrimed) return new Color(0.3f, 0.6f, 1f); // Blue
        if (tile.HasMarker) return new Color(1f, 0.3f, 0.3f); // Red
        if (tile.CanPaintCubes) return new Color(0.8f, 0.4f, 0.8f); // Purple
        
        return Color.white;
    }

    /// <summary>
    /// Checks if a tile has any special state (not normal).
    /// </summary>
    /// <param name="tile">Tile to check</param>
    /// <returns>True if tile has special state</returns>
    public static bool IsSpecialTile(Tile tile)
    {
        if (tile == null) return false;

        return !tile.IsPlayable || tile.IsBlackened || 
               tile.IsPrimed || tile.HasMarker || tile.CanPaintCubes;
    }

    #endregion
}
