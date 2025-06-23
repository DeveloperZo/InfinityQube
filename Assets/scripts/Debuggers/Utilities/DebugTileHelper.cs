using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Shared utility for tile manipulation and analysis across debug panels.
/// Provides consistent tile painting, state checking, and display methods.
/// </summary>
public static class DebugTileHelper
{
    #region Tile State Analysis

    /// <summary>
    /// Gets a descriptive string for the tile's current state
    /// </summary>
    public static string GetTileStateDescription(Tile tile)
    {
        if (tile == null)
            return "No tile";
            
        var parts = new List<string>();
        
        // Base tile type
        parts.Add(tile.TileType.ToString());
        
        // Special states
        if (tile.IsAdvantaged)
            parts.Add($"Advantaged ({tile.DetonationCharges} charges)");
            
        if (tile.CanPaintCubes)
            parts.Add($"Painter ({tile.PaintStatus}, {tile.PaintDuration}s)");
            
        if (tile.IsCorrupted)
            parts.Add("Corrupted");
            
        if (tile.IsEnhanced)
            parts.Add("Enhanced");
            
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Gets appropriate display color for a tile based on its state
    /// </summary>
    public static Color GetTileDisplayColor(Tile tile)
    {
        if (tile == null)
            return Color.gray;
            
        // Priority order for coloring
        if (tile.CanPaintCubes)
        {
            return tile.PaintStatus == FaceStatus.Corrupted ? 
                new Color(0.8f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 0.8f);
        }
        
        if (tile.IsAdvantaged)
            return new Color(0.9f, 0.8f, 0.3f); // Yellow for advantaged
            
        if (tile.IsCorrupted)
            return new Color(0.7f, 0.2f, 0.2f); // Dark red for corrupted
            
        if (tile.IsEnhanced)
            return new Color(0.2f, 0.2f, 0.7f); // Dark blue for enhanced
            
        // Default tile type colors
        switch (tile.TileType)
        {
            case TileType.Normal:
                return Color.white;
            case TileType.Speed:
                return new Color(0.7f, 1f, 0.7f); // Light green
            case TileType.Teleporter:
                return new Color(1f, 0.7f, 1f); // Light magenta
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Checks if a tile has any active effects
    /// </summary>
    public static bool HasActiveEffects(Tile tile)
    {
        if (tile == null)
            return false;
            
        return tile.CanPaintCubes || tile.IsAdvantaged || tile.IsCorrupted || tile.IsEnhanced;
    }

    /// <summary>
    /// Gets a summary of all tiles in an area
    /// </summary>
    public static TileAreaSummary GetAreaSummary(Vector2Int center, int radius, GridManager gridManager)
    {
        var summary = new TileAreaSummary();
        
        if (gridManager == null)
            return summary;
            
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                
                if (!gridManager.IsValidGridPosition(position))
                    continue;
                    
                Tile tile = gridManager.GetTileAt(position);
                if (tile == null)
                    continue;
                    
                summary.TotalTiles++;
                
                if (tile.CanPaintCubes)
                    summary.PaintingTiles++;
                    
                if (tile.IsAdvantaged)
                    summary.AdvantagedTiles++;
                    
                if (tile.IsCorrupted)
                    summary.CorruptedTiles++;
                    
                if (tile.IsEnhanced)
                    summary.EnhancedTiles++;
                    
                if (!summary.TileTypeCount.ContainsKey(tile.TileType))
                    summary.TileTypeCount[tile.TileType] = 0;
                summary.TileTypeCount[tile.TileType]++;
            }
        }
        
        return summary;
    }

    #endregion

    #region Tile Painting and Manipulation

    /// <summary>
    /// Sets up tile painting at the specified position
    /// </summary>
    public static bool SetupTilePainting(Vector2Int position, FaceStatus paintStatus, Color paintColor, 
        int duration, GridManager gridManager, bool clearExisting = false, bool logResult = true)
    {
        if (gridManager == null)
        {
            if (logResult) Debug.LogWarning("DebugTileHelper: GridManager is null");
            return false;
        }
        
        if (!gridManager.IsValidGridPosition(position))
        {
            if (logResult) Debug.LogWarning($"DebugTileHelper: Invalid position ({position.x}, {position.y})");
            return false;
        }
        
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
        {
            if (logResult) Debug.LogWarning($"DebugTileHelper: No tile found at ({position.x}, {position.y})");
            return false;
        }
        
        try
        {
            // Clear existing painting if requested
            if (clearExisting && tile.CanPaintCubes)
            {
                ClearTilePainting(position, gridManager);
            }
            
            // Set up the painting
            tile.SetupPainting(paintStatus, paintColor, duration);
            
            if (logResult)
            {
                Debug.Log($"DebugTileHelper: Setup {paintStatus} painting at ({position.x}, {position.y}) with {duration}s duration");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            if (logResult) Debug.LogError($"DebugTileHelper: Error setting up tile painting: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears tile painting at the specified position
    /// </summary>
    public static bool ClearTilePainting(Vector2Int position, GridManager gridManager, bool logResult = true)
    {
        if (gridManager == null)
        {
            if (logResult) Debug.LogWarning("DebugTileHelper: GridManager is null");
            return false;
        }
        
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
        {
            if (logResult) Debug.LogWarning($"DebugTileHelper: No tile found at ({position.x}, {position.y})");
            return false;
        }
        
        try
        {
            tile.ClearPainting();
            
            if (logResult)
            {
                Debug.Log($"DebugTileHelper: Cleared painting at ({position.x}, {position.y})");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            if (logResult) Debug.LogError($"DebugTileHelper: Error clearing tile painting: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets up advantaged status on a tile
    /// </summary>
    public static bool SetupAdvantaged(Vector2Int position, int charges, GridManager gridManager, bool logResult = true)
    {
        if (gridManager == null)
        {
            if (logResult) Debug.LogWarning("DebugTileHelper: GridManager is null");
            return false;
        }
        
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
        {
            if (logResult) Debug.LogWarning($"DebugTileHelper: No tile found at ({position.x}, {position.y})");
            return false;
        }
        
        try
        {
            tile.SetAdvantaged(charges);
            
            if (logResult)
            {
                Debug.Log($"DebugTileHelper: Set advantaged status with {charges} charges at ({position.x}, {position.y})");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            if (logResult) Debug.LogError($"DebugTileHelper: Error setting advantaged status: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears advantaged status on a tile
    /// </summary>
    public static bool ClearAdvantaged(Vector2Int position, GridManager gridManager, bool logResult = true)
    {
        if (gridManager == null)
            return false;
            
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
            return false;
            
        try
        {
            tile.ClearAdvantaged();
            
            if (logResult)
            {
                Debug.Log($"DebugTileHelper: Cleared advantaged status at ({position.x}, {position.y})");
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            if (logResult) Debug.LogError($"DebugTileHelper: Error clearing advantaged status: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Sets up painting on multiple tiles in a pattern
    /// </summary>
    public static int SetupPaintingPattern(Vector2Int startPosition, Vector2Int direction, int count, 
        FaceStatus paintStatus, Color paintColor, int duration, GridManager gridManager)
    {
        int successCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int position = startPosition + (direction * i);
            
            if (SetupTilePainting(position, paintStatus, paintColor, duration, gridManager, false, false))
            {
                successCount++;
            }
        }
        
        Debug.Log($"DebugTileHelper: Set up painting on {successCount}/{count} tiles in pattern");
        return successCount;
    }

    /// <summary>
    /// Clears all painting in an area
    /// </summary>
    public static int ClearPaintingInArea(Vector2Int center, int radius, GridManager gridManager)
    {
        int clearedCount = 0;
        
        if (gridManager == null)
            return 0;
            
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                
                if (ClearTilePainting(position, gridManager, false))
                {
                    clearedCount++;
                }
            }
        }
        
        Debug.Log($"DebugTileHelper: Cleared painting on {clearedCount} tiles in area");
        return clearedCount;
    }

    /// <summary>
    /// Sets up advantaged status on multiple tiles in a pattern
    /// </summary>
    public static int SetupAdvantagedPattern(Vector2Int startPosition, Vector2Int direction, int count, 
        int charges, GridManager gridManager)
    {
        int successCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int position = startPosition + (direction * i);
            
            if (SetupAdvantaged(position, charges, gridManager, false))
            {
                successCount++;
            }
        }
        
        Debug.Log($"DebugTileHelper: Set up advantaged status on {successCount}/{count} tiles in pattern");
        return successCount;
    }

    #endregion

    #region Tile Finding and Analysis

    /// <summary>
    /// Finds all tiles with painting capability in a radius
    /// </summary>
    public static List<Vector2Int> FindPaintingTilesInRadius(Vector2Int center, int radius, GridManager gridManager)
    {
        var paintingTiles = new List<Vector2Int>();
        
        if (gridManager == null)
            return paintingTiles;
            
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                
                if (!gridManager.IsValidGridPosition(position))
                    continue;
                    
                Tile tile = gridManager.GetTileAt(position);
                if (tile != null && tile.CanPaintCubes)
                {
                    paintingTiles.Add(position);
                }
            }
        }
        
        return paintingTiles;
    }

    /// <summary>
    /// Finds all advantaged tiles in a radius
    /// </summary>
    public static List<Vector2Int> FindAdvantagedTilesInRadius(Vector2Int center, int radius, GridManager gridManager)
    {
        var advantagedTiles = new List<Vector2Int>();
        
        if (gridManager == null)
            return advantagedTiles;
            
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                
                if (!gridManager.IsValidGridPosition(position))
                    continue;
                    
                Tile tile = gridManager.GetTileAt(position);
                if (tile != null && tile.IsAdvantaged)
                {
                    advantagedTiles.Add(position);
                }
            }
        }
        
        return advantagedTiles;
    }

    /// <summary>
    /// Gets detailed information about a specific tile
    /// </summary>
    public static TileInfo GetTileInfo(Vector2Int position, GridManager gridManager)
    {
        var info = new TileInfo();
        info.Position = position;
        info.IsValid = false;
        
        if (gridManager == null)
            return info;
            
        if (!gridManager.IsValidGridPosition(position))
            return info;
            
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null)
            return info;
            
        info.IsValid = true;
        info.TileType = tile.TileType;
        info.CanPaintCubes = tile.CanPaintCubes;
        info.PaintStatus = tile.PaintStatus;
        info.PaintDuration = tile.PaintDuration;
        info.IsAdvantaged = tile.IsAdvantaged;
        info.DetonationCharges = tile.DetonationCharges;
        info.IsCorrupted = tile.IsCorrupted;
        info.IsEnhanced = tile.IsEnhanced;
        info.HasActiveEffects = HasActiveEffects(tile);
        info.StateDescription = GetTileStateDescription(tile);
        info.DisplayColor = GetTileDisplayColor(tile);
        
        return info;
    }

    #endregion

    #region Quick Setup Methods

    /// <summary>
    /// Quick setup for corrupted painting
    /// </summary>
    public static bool QuickSetupCorruptedPainting(Vector2Int position, int duration, GridManager gridManager)
    {
        return SetupTilePainting(position, FaceStatus.Corrupted, Color.red, duration, gridManager);
    }

    /// <summary>
    /// Quick setup for enhanced painting
    /// </summary>
    public static bool QuickSetupEnhancedPainting(Vector2Int position, int duration, GridManager gridManager)
    {
        return SetupTilePainting(position, FaceStatus.Enhanced, Color.blue, duration, gridManager);
    }

    /// <summary>
    /// Quick setup for a test area with mixed tile effects
    /// </summary>
    public static int QuickSetupTestArea(Vector2Int center, GridManager gridManager)
    {
        int setupCount = 0;
        
        // Center: Advantaged tile
        if (SetupAdvantaged(center, 3, gridManager, false))
            setupCount++;
            
        // Cardinal directions: Painting tiles
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var paintStatuses = new[] { FaceStatus.Corrupted, FaceStatus.Enhanced, FaceStatus.Corrupted, FaceStatus.Enhanced };
        var colors = new[] { Color.red, Color.blue, Color.red, Color.blue };
        
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int position = center + directions[i];
            if (SetupTilePainting(position, paintStatuses[i], colors[i], 5, gridManager, false, false))
                setupCount++;
        }
        
        Debug.Log($"DebugTileHelper: Quick setup test area with {setupCount} configured tiles");
        return setupCount;
    }

    #endregion
}

/// <summary>
/// Summary information about tiles in an area
/// </summary>
public class TileAreaSummary
{
    public int TotalTiles;
    public int PaintingTiles;
    public int AdvantagedTiles;
    public int CorruptedTiles;
    public int EnhancedTiles;
    public Dictionary<TileType, int> TileTypeCount = new Dictionary<TileType, int>();
    
    public float PaintingPercentage => TotalTiles > 0 ? (float)PaintingTiles / TotalTiles * 100f : 0f;
    public float AdvantagedPercentage => TotalTiles > 0 ? (float)AdvantagedTiles / TotalTiles * 100f : 0f;
}

/// <summary>
/// Detailed information about a specific tile
/// </summary>
public class TileInfo
{
    public Vector2Int Position;
    public bool IsValid;
    public TileType TileType;
    public bool CanPaintCubes;
    public FaceStatus PaintStatus;
    public int PaintDuration;
    public bool IsAdvantaged;
    public int DetonationCharges;
    public bool IsCorrupted;
    public bool IsEnhanced;
    public bool HasActiveEffects;
    public string StateDescription;
    public Color DisplayColor;
}
