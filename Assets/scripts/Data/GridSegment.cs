using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Defines a rectangular grid segment for advanced grid layouts (L, C, S shapes).
/// Each segment is a standard grid with its own coordinate system, position, and rotation.
/// Segments connect via overlap zones where cubes transition between them.
/// </summary>
[System.Serializable]
public class GridSegment
{
    #region Configuration
    
    [Header("Segment Identity")]
    [Tooltip("Index of this segment in the path (0 = first/spawn segment)")]
    public int segmentIndex;
    
    [Tooltip("Display name for this segment")]
    public string segmentName;
    
    [Header("Dimensions")]
    [Tooltip("Width of this segment (columns)")]
    public int width = 5;
    
    [Tooltip("Height of this segment (rows)")]
    public int height = 15;
    
    [Header("World Positioning")]
    [Tooltip("World position offset from grid origin")]
    public Vector3 worldOffset = Vector3.zero;
    
    [Tooltip("Rotation in degrees (0 = down, 90 = right, 180 = up, 270 = left)")]
    public float rotationAngle = 0f;
    
    [Header("Movement")]
    [Tooltip("Direction cubes move through this segment (in local segment space)")]
    public MovementDirection localDirection = MovementDirection.Down;
    
    [Header("Overlap Configuration")]
    [Tooltip("Number of rows that overlap with the previous segment")]
    public int overlapRows = 0;
    
    [Tooltip("Starting row of the overlap zone in this segment's local coordinates")]
    public int overlapStartRow = 0;
    
    #endregion
    
    #region Runtime State
    
    /// <summary>
    /// Tiles belonging to this segment (indexed by local x,y)
    /// </summary>
    [System.NonSerialized]
    public Tile[,] tiles;
    
    /// <summary>
    /// Whether this segment has been initialized
    /// </summary>
    [System.NonSerialized]
    public bool isInitialized = false;
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Creates a standard vertical segment (cubes move down).
    /// </summary>
    public static GridSegment CreateVerticalSegment(int index, int width, int height, Vector3 offset)
    {
        return new GridSegment
        {
            segmentIndex = index,
            segmentName = $"Segment_{index}_Vertical",
            width = width,
            height = height,
            worldOffset = offset,
            rotationAngle = 0f,
            localDirection = MovementDirection.Down,
            overlapRows = 0,
            overlapStartRow = 0
        };
    }
    
    /// <summary>
    /// Creates a horizontal segment (cubes move LEFT in world space).
    /// For the player, after camera rotation, this still appears as "down".
    /// Rotation is +90° so cubes at high local Y are on the RIGHT, and moving -Y moves them LEFT.
    /// </summary>
    public static GridSegment CreateHorizontalSegment(int index, int width, int height, Vector3 offset, int overlapRows)
    {
        return new GridSegment
        {
            segmentIndex = index,
            segmentName = $"Segment_{index}_Horizontal",
            width = width,
            height = height,
            worldOffset = offset,
            rotationAngle = 90f, // Rotated +90° so cubes move from RIGHT to LEFT (world -X)
            localDirection = MovementDirection.Down, // Still "down" in local space
            overlapRows = overlapRows,
            overlapStartRow = height - overlapRows // Overlap is at the "top" of this segment
        };
    }
    
    /// <summary>
    /// Creates an L-shape grid with two segments.
    /// Segment 1: Vertical (width x height)
    /// Segment 2: Horizontal (width x (height + overlapSize)), rotated 90°
    /// </summary>
    public static List<GridSegment> CreateLShape(int width, int height)
    {
        var segments = new List<GridSegment>();
        
        int overlapSize = width; // Overlap zone is width x width (square)
        
        // Segment 1: Vertical (standard orientation)
        var seg1 = CreateVerticalSegment(0, width, height, Vector3.zero);
        seg1.segmentName = "Segment_0_Vertical_Main";
        segments.Add(seg1);
        
        // Segment 2: Horizontal (rotated 90°)
        // Height is original height + overlap so waves can spawn at the "top" which overlaps with seg1's bottom
        int seg2Height = height + overlapSize;
        
        // Position segment 2 so its overlap zone aligns with segment 1's bottom
        // Segment 2 is rotated 90°, so its local Y becomes world X
        Vector3 seg2Offset = new Vector3(0, 0, 0); // Will be calculated based on tile positions
        
        var seg2 = CreateHorizontalSegment(1, width, seg2Height, seg2Offset, overlapSize);
        seg2.segmentName = "Segment_1_Horizontal_Extension";
        segments.Add(seg2);
        
        return segments;
    }
    
    #endregion
    
    #region Coordinate Conversion
    
    /// <summary>
    /// Converts local segment coordinates to world position.
    /// </summary>
    public Vector3 LocalToWorldPosition(int localX, int localY, float tileSize, float heightOffset = 0f)
    {
        Vector3 localPos = new Vector3(localX * tileSize, heightOffset, localY * tileSize);
        
        // Apply rotation
        if (rotationAngle != 0f)
        {
            localPos = Quaternion.Euler(0, rotationAngle, 0) * localPos;
        }
        
        // Apply offset
        return localPos + worldOffset;
    }
    
    /// <summary>
    /// Converts world position to local segment coordinates.
    /// Returns (-1, -1) if position is outside this segment.
    /// </summary>
    public Vector2Int WorldToLocalPosition(Vector3 worldPos, float tileSize)
    {
        // Remove offset
        Vector3 localPos = worldPos - worldOffset;
        
        // Reverse rotation
        if (rotationAngle != 0f)
        {
            localPos = Quaternion.Euler(0, -rotationAngle, 0) * localPos;
        }
        
        int x = Mathf.RoundToInt(localPos.x / tileSize);
        int y = Mathf.RoundToInt(localPos.z / tileSize);
        
        // Check bounds
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return new Vector2Int(-1, -1);
        }
        
        return new Vector2Int(x, y);
    }
    
    /// <summary>
    /// Checks if a local position is within the overlap zone.
    /// </summary>
    public bool IsInOverlapZone(int localX, int localY)
    {
        if (overlapRows <= 0) return false;
        
        return localY >= overlapStartRow && localY < overlapStartRow + overlapRows;
    }
    
    /// <summary>
    /// Checks if a local position is valid within this segment.
    /// </summary>
    public bool IsValidLocalPosition(int localX, int localY)
    {
        return localX >= 0 && localX < width && localY >= 0 && localY < height;
    }
    
    /// <summary>
    /// Gets the spawn row for this segment (top row, accounting for overlap).
    /// </summary>
    public int GetSpawnRow()
    {
        return height - 1;
    }
    
    /// <summary>
    /// Gets the escape row for this segment (bottom row).
    /// </summary>
    public int GetEscapeRow()
    {
        return 0;
    }
    
    /// <summary>
    /// Gets the row where cubes enter the overlap zone (triggers transition).
    /// </summary>
    public int GetOverlapEntryRow()
    {
        if (segmentIndex == 0)
        {
            // First segment: overlap is at the bottom
            return overlapRows > 0 ? overlapRows - 1 : 0;
        }
        else
        {
            // Other segments: overlap is at the top
            return overlapStartRow;
        }
    }
    
    #endregion
    
    #region Movement
    
    /// <summary>
    /// Gets the next local position when moving in this segment's direction.
    /// </summary>
    public Vector2Int GetNextPosition(Vector2Int current)
    {
        switch (localDirection)
        {
            case MovementDirection.Down:
                return new Vector2Int(current.x, current.y - 1);
            case MovementDirection.Up:
                return new Vector2Int(current.x, current.y + 1);
            case MovementDirection.Right:
                return new Vector2Int(current.x + 1, current.y);
            case MovementDirection.Left:
                return new Vector2Int(current.x - 1, current.y);
            default:
                return current;
        }
    }
    
    /// <summary>
    /// Gets the world rotation for objects in this segment.
    /// </summary>
    public Quaternion GetWorldRotation()
    {
        return Quaternion.Euler(0, rotationAngle, 0);
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validates the segment configuration.
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        if (width < 1)
            issues.Add($"Segment {segmentIndex}: Width must be at least 1");
        
        if (height < 1)
            issues.Add($"Segment {segmentIndex}: Height must be at least 1");
        
        if (overlapRows < 0)
            issues.Add($"Segment {segmentIndex}: Overlap rows cannot be negative");
        
        if (overlapRows > 0 && overlapStartRow + overlapRows > height)
            issues.Add($"Segment {segmentIndex}: Overlap zone exceeds segment height");
        
        return issues;
    }
    
    #endregion
}
