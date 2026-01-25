using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles coordinate conversion between grid space and world space.
/// Extracted from GridManager as part of SRP refactoring.
/// Works with both legacy grids and segment controller-based grids.
/// </summary>
public class GridCoordinateConverter
{
    #region References
    private readonly GridManager grid;
    private readonly float tileSize;
    private readonly int width;
    private readonly int height;
    #endregion

    #region Constructor
    public GridCoordinateConverter(GridManager gridManager)
    {
        grid = gridManager;
        tileSize = gridManager.tileSize;
        width = gridManager.Width;
        height = gridManager.Height;
    }
    #endregion

    #region Grid to World Conversion
    /// <summary>
    /// Converts grid coordinates to world position.
    /// Uses segment controllers if available, otherwise uses legacy calculation.
    /// </summary>
    public Vector3 GridToWorldPosition(int x, int y, float heightOffset = 0)
    {
        // SEGMENT CONTROLLERS: Use first segment's coordinate system
        if (grid.HasSegmentControllers && grid.SegmentControllerCount > 0)
        {
            var primarySegment = grid.GetSegmentController(0);
            return primarySegment.LocalToWorldPosition(x, y, heightOffset);
        }
        
        // Legacy: Use calculated grid offset
        Vector3 basePosition = grid.transform.position + grid.CalculatedGridOffset;
        return new Vector3(x * tileSize, heightOffset, y * tileSize) + basePosition;
    }
    #endregion

    #region World to Grid Conversion
    /// <summary>
    /// Converts world position to grid coordinates.
    /// Uses segment controllers if available, otherwise uses legacy calculation.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        // SEGMENT CONTROLLERS: Use first segment's coordinate system
        if (grid.HasSegmentControllers && grid.SegmentControllerCount > 0)
        {
            var primarySegment = grid.GetSegmentController(0);
            return primarySegment.WorldToLocalPosition(worldPosition);
        }
        
        // Legacy: Use calculated grid offset
        Vector3 basePosition = grid.transform.position + grid.CalculatedGridOffset;
        Vector3 localPos = worldPosition - basePosition;

        int x = Mathf.RoundToInt(localPos.x / tileSize);
        int y = Mathf.RoundToInt(localPos.z / tileSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }
    
    /// <summary>
    /// ADVANCED GRID: Converts world position to the appropriate segment's local coordinates.
    /// Returns (segmentIndex, localX, localY). SegmentIndex is -1 if not on any segment.
    /// </summary>
    public (int segmentIndex, Vector2Int localPos) WorldToSegmentLocalPosition(Vector3 worldPosition)
    {
        // Check segment 0 first (main grid)
        Vector3 seg0Base = grid.transform.position + grid.CalculatedGridOffset;
        Vector3 localPos0 = worldPosition - seg0Base;
        int x0 = Mathf.RoundToInt(localPos0.x / tileSize);
        int y0 = Mathf.RoundToInt(localPos0.z / tileSize);
        
        if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
        {
            return (0, new Vector2Int(x0, y0));
        }
        
        // Check other segments
        if (grid.HasMultipleSegments)
        {
            for (int i = 1; i < grid.SegmentCount; i++)
            {
                var segment = grid.Segments[i];
                var localCoord = segment.WorldToLocalPosition(worldPosition, tileSize);
                
                if (localCoord.x >= 0 && localCoord.y >= 0 && 
                    localCoord.x < segment.width && localCoord.y < segment.height)
                {
                    return (i, localCoord);
                }
            }
        }
        
        // Not on any segment - return segment 0's clamped position as fallback
        x0 = Mathf.Clamp(x0, 0, width - 1);
        y0 = Mathf.Clamp(y0, 0, height - 1);
        return (-1, new Vector2Int(x0, y0));
    }
    #endregion

    #region Position Validation
    /// <summary>
    /// ADVANCED GRID: Checks if a world position is on a valid, playable tile on ANY segment.
    /// </summary>
    public bool IsWorldPositionValid(Vector3 worldPosition)
    {
        var (segmentIndex, localPos) = WorldToSegmentLocalPosition(worldPosition);
        
        if (segmentIndex < 0)
            return false;
        
        if (segmentIndex == 0)
        {
            Tile tile = grid.GetTileAt(localPos);
            return tile != null && tile.IsPlayable;
        }
        else
        {
            var segment = grid.Segments[segmentIndex];
            if (segment.tiles != null && segment.IsValidLocalPosition(localPos.x, localPos.y))
            {
                var tile = segment.tiles[localPos.x, localPos.y];
                return tile != null && tile.IsPlayable;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a grid position is valid on the main grid (segment 0).
    /// </summary>
    public bool IsValidGridPosition(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            Tile tile = grid.GetTileAt(x, y);
            return tile != null && tile.IsPlayable;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a grid position is valid on the main grid (segment 0).
    /// </summary>
    public bool IsValidGridPosition(Vector2Int pos)
    {
        return IsValidGridPosition(pos.x, pos.y);
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if a position is valid on ANY segment (for player movement across segments).
    /// </summary>
    public bool IsValidPositionOnAnySegment(int x, int y)
    {
        // Check segment 0 (main grid)
        if (IsValidGridPosition(x, y))
            return true;
        
        // For multi-segment grids, check other segments
        if (grid.HasMultipleSegments)
        {
            for (int i = 1; i < grid.SegmentCount; i++)
            {
                var segment = grid.Segments[i];
                if (segment.IsValidLocalPosition(x, y) && segment.tiles != null)
                {
                    var tile = segment.tiles[x, y];
                    if (tile != null && tile.IsPlayable)
                        return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the segment index for a given position, or -1 if not on any segment.
    /// </summary>
    public int GetSegmentForPosition(int x, int y)
    {
        // Check segment 0 first
        if (x >= 0 && x < width && y >= 0 && y < height)
            return 0;
        
        // Check other segments
        if (grid.HasMultipleSegments)
        {
            for (int i = 1; i < grid.SegmentCount; i++)
            {
                if (grid.Segments[i].IsValidLocalPosition(x, y))
                    return i;
            }
        }
        
        return -1;
    }
    #endregion

    #region Tile Access
    /// <summary>
    /// ADVANCED GRID: Gets the tile at a world position from any segment.
    /// </summary>
    public Tile GetTileAtWorldPositionAnySegment(Vector3 worldPosition)
    {
        // SEGMENT CONTROLLERS: Check segment controllers first
        if (grid.HasSegmentControllers)
        {
            return grid.GetTileAtWorldPositionFromControllers(worldPosition);
        }
        
        // Legacy multi-segment support
        var (segmentIndex, localPos) = WorldToSegmentLocalPosition(worldPosition);
        
        if (segmentIndex < 0)
            return null;
        
        if (segmentIndex == 0)
        {
            return grid.GetTileAt(localPos);
        }
        else if (segmentIndex < grid.SegmentCount)
        {
            var segment = grid.Segments[segmentIndex];
            if (segment.tiles != null && segment.IsValidLocalPosition(localPos.x, localPos.y))
            {
                return segment.tiles[localPos.x, localPos.y];
            }
        }
        
        return null;
    }

    /// <summary>
    /// Gets the tile at the specified world position using grid conversion.
    /// </summary>
    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return grid.GetTileAt(gridPos);
    }
    #endregion
}
