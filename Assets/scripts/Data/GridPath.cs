using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Defines a movement path for cubes on the grid.
/// Paths consist of segments with different directions, connected by turn points.
/// Cubes maintain their formation but movement direction rotates at corners.
/// </summary>
[System.Serializable]
public class GridPath
{
    #region Configuration
    
    [Header("Path Type")]
    [Tooltip("Predefined path type or custom")]
    public GridPathType pathType = GridPathType.Standard;
    
    [Header("Path Segments")]
    [Tooltip("List of path segments defining the movement path")]
    public List<PathSegment> segments = new List<PathSegment>();
    
    [Header("Turn Points")]
    [Tooltip("Grid positions where cubes change direction")]
    public List<TurnPoint> turnPoints = new List<TurnPoint>();
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Creates a path configuration based on the path type.
    /// </summary>
    public static GridPath CreatePath(GridPathType pathType, int gridWidth, int gridHeight)
    {
        var path = new GridPath { pathType = pathType };
        
        switch (pathType)
        {
            case GridPathType.Standard:
                path.ConfigureStandardPath(gridWidth, gridHeight);
                break;
            case GridPathType.L_Shape:
                path.ConfigureLShapePath(gridWidth, gridHeight);
                break;
            case GridPathType.C_Shape:
                path.ConfigureCShapePath(gridWidth, gridHeight);
                break;
            case GridPathType.S_Shape:
                path.ConfigureSShapePath(gridWidth, gridHeight);
                break;
            case GridPathType.Custom:
                // Custom paths must be configured manually
                break;
        }
        
        return path;
    }
    
    #endregion
    
    #region Path Configuration
    
    /// <summary>
    /// Standard path: straight down from top to bottom
    /// </summary>
    private void ConfigureStandardPath(int gridWidth, int gridHeight)
    {
        segments.Clear();
        turnPoints.Clear();
        
        // Single segment: down for the full height
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Down,
            startRow = gridHeight - 1,
            endRow = 0,
            column = -1, // -1 means all columns
            length = gridHeight
        });
    }
    
    /// <summary>
    /// L-shape path: Two separate segments (NO turn points in v1)
    /// - Segment 0: Cubes move straight down until they escape at y=0
    /// - After segment 0 clears, wave respawns at segment 1
    /// - Segment 1: Cubes move straight down (in segment 1's local orientation)
    /// 
    /// Turn points are NOT used in this version - each segment is treated as a
    /// separate grid and the transition happens via wave respawn, not cube turning.
    /// </summary>
    private void ConfigureLShapePath(int gridWidth, int gridHeight)
    {
        segments.Clear();
        turnPoints.Clear(); // NO turn points for L-shape v1 - just straight movement per segment
        
        // Segment 0: Move straight down from top to bottom (standard behavior)
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Down,
            startRow = gridHeight - 1,
            endRow = 0,
            column = -1,
            length = gridHeight
        });
        
        // Note: Segment 1 movement is handled separately when the wave respawns there
        // Each segment has its own coordinate system and cubes move "down" in local space
    }
    
    /// <summary>
    /// C-shape path: down → right → up (U-turn)
    /// Creates a path that curves back up
    /// </summary>
    private void ConfigureCShapePath(int gridWidth, int gridHeight)
    {
        segments.Clear();
        turnPoints.Clear();
        
        int firstTurnRow = gridHeight / 4; // First turn at 1/4 from bottom
        int horizontalLength = gridWidth / 2; // How far to go right before turning up
        
        // Segment 1: Move down from top to first turn
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Down,
            startRow = gridHeight - 1,
            endRow = firstTurnRow,
            column = -1,
            length = gridHeight - firstTurnRow
        });
        
        // First turn point: down → right
        turnPoints.Add(new TurnPoint
        {
            position = new Vector2Int(0, firstTurnRow),
            fromDirection = MovementDirection.Down,
            toDirection = MovementDirection.Right,
            affectsAllColumns = true
        });
        
        // Segment 2: Move right
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Right,
            startColumn = 0,
            endColumn = horizontalLength,
            row = firstTurnRow,
            length = horizontalLength
        });
        
        // Second turn point: right → up
        turnPoints.Add(new TurnPoint
        {
            position = new Vector2Int(horizontalLength, firstTurnRow),
            fromDirection = MovementDirection.Right,
            toDirection = MovementDirection.Up,
            affectsAllColumns = true
        });
        
        // Segment 3: Move up to escape
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Up,
            startRow = firstTurnRow,
            endRow = gridHeight + 5, // Extended for escape
            column = horizontalLength,
            length = gridHeight - firstTurnRow + 5
        });
    }
    
    /// <summary>
    /// S-shape path: down → right → down → right (snake pattern)
    /// Creates a serpentine path for extended gameplay
    /// </summary>
    private void ConfigureSShapePath(int gridWidth, int gridHeight)
    {
        segments.Clear();
        turnPoints.Clear();
        
        int firstTurnRow = (gridHeight * 2) / 3; // First turn at 2/3 from bottom
        int secondTurnRow = gridHeight / 3; // Second turn at 1/3 from bottom
        int midColumn = gridWidth / 2;
        
        // Segment 1: Move down from top to first turn
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Down,
            startRow = gridHeight - 1,
            endRow = firstTurnRow,
            column = -1,
            length = gridHeight - firstTurnRow
        });
        
        // First turn: down → right
        turnPoints.Add(new TurnPoint
        {
            position = new Vector2Int(0, firstTurnRow),
            fromDirection = MovementDirection.Down,
            toDirection = MovementDirection.Right,
            affectsAllColumns = true
        });
        
        // Segment 2: Move right to mid point
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Right,
            startColumn = 0,
            endColumn = midColumn,
            row = firstTurnRow,
            length = midColumn
        });
        
        // Second turn: right → down
        turnPoints.Add(new TurnPoint
        {
            position = new Vector2Int(midColumn, firstTurnRow),
            fromDirection = MovementDirection.Right,
            toDirection = MovementDirection.Down,
            affectsAllColumns = true
        });
        
        // Segment 3: Move down to second turn
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Down,
            startRow = firstTurnRow,
            endRow = secondTurnRow,
            column = midColumn,
            length = firstTurnRow - secondTurnRow
        });
        
        // Third turn: down → right
        turnPoints.Add(new TurnPoint
        {
            position = new Vector2Int(midColumn, secondTurnRow),
            fromDirection = MovementDirection.Down,
            toDirection = MovementDirection.Right,
            affectsAllColumns = true
        });
        
        // Segment 4: Move right to escape
        segments.Add(new PathSegment
        {
            direction = MovementDirection.Right,
            startColumn = midColumn,
            endColumn = gridWidth * 2,
            row = secondTurnRow,
            length = gridWidth * 2 - midColumn
        });
    }
    
    #endregion
    
    #region Path Queries
    
    /// <summary>
    /// Gets the movement direction for a cube at the given position.
    /// </summary>
    public MovementDirection GetDirectionAtPosition(Vector2Int position, MovementDirection currentDirection)
    {
        // Check if at a turn point
        foreach (var turn in turnPoints)
        {
            if (IsAtTurnPoint(position, turn) && currentDirection == turn.fromDirection)
            {
                return turn.toDirection;
            }
        }
        
        return currentDirection;
    }
    
    /// <summary>
    /// Checks if a position is at a turn point.
    /// </summary>
    private bool IsAtTurnPoint(Vector2Int position, TurnPoint turn)
    {
        if (turn.affectsAllColumns)
        {
            // Check row only (all columns turn at this row)
            return position.y == turn.position.y;
        }
        
        // Exact position match
        return position == turn.position;
    }
    
    /// <summary>
    /// Gets the next position for a cube moving in the given direction.
    /// </summary>
    public Vector2Int GetNextPosition(Vector2Int current, MovementDirection direction)
    {
        switch (direction)
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
    /// Gets the rotation angle for the given movement direction.
    /// Used for visual cube rotation during turns.
    /// </summary>
    public float GetRotationAngle(MovementDirection direction)
    {
        switch (direction)
        {
            case MovementDirection.Down:
                return 0f;
            case MovementDirection.Right:
                return 90f;
            case MovementDirection.Up:
                return 180f;
            case MovementDirection.Left:
                return 270f;
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Gets the initial movement direction for the path.
    /// </summary>
    public MovementDirection GetInitialDirection()
    {
        if (segments.Count > 0)
        {
            return segments[0].direction;
        }
        return MovementDirection.Down;
    }
    
    /// <summary>
    /// Checks if the path type is standard (no turns).
    /// </summary>
    public bool IsStandardPath()
    {
        return pathType == GridPathType.Standard;
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validates the path configuration.
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        if (pathType == GridPathType.Custom && segments.Count == 0)
        {
            issues.Add("Custom path has no segments defined");
        }
        
        if (pathType != GridPathType.Standard && turnPoints.Count == 0)
        {
            issues.Add($"Path type {pathType} should have turn points but none are defined");
        }
        
        // Validate segment continuity
        for (int i = 0; i < segments.Count - 1; i++)
        {
            var current = segments[i];
            var next = segments[i + 1];
            
            // Check that segments connect properly
            // (detailed validation would depend on specific path geometry)
        }
        
        return issues;
    }
    
    #endregion
}

/// <summary>
/// Defines a single segment of a grid path.
/// A segment represents movement in one direction for a certain distance.
/// </summary>
[System.Serializable]
public class PathSegment
{
    [Tooltip("Movement direction for this segment")]
    public MovementDirection direction;
    
    [Tooltip("Starting row (for vertical segments)")]
    public int startRow;
    
    [Tooltip("Ending row (for vertical segments)")]
    public int endRow;
    
    [Tooltip("Column constraint (-1 = all columns)")]
    public int column = -1;
    
    [Tooltip("Starting column (for horizontal segments)")]
    public int startColumn;
    
    [Tooltip("Ending column (for horizontal segments)")]
    public int endColumn;
    
    [Tooltip("Row constraint (for horizontal segments)")]
    public int row;
    
    [Tooltip("Length of this segment in grid units")]
    public int length;
}

/// <summary>
/// Defines a turn point where cubes change movement direction.
/// Cubes maintain their formation but rotate their movement at these points.
/// </summary>
[System.Serializable]
public class TurnPoint
{
    [Tooltip("Grid position of the turn point")]
    public Vector2Int position;
    
    [Tooltip("Direction cubes are moving from")]
    public MovementDirection fromDirection;
    
    [Tooltip("Direction cubes will move to after turn")]
    public MovementDirection toDirection;
    
    [Tooltip("If true, all cubes in the formation turn when reaching this row/column")]
    public bool affectsAllColumns = true;
    
    [Tooltip("Optional: specific columns affected by this turn point")]
    public List<int> affectedColumns = new List<int>();
}
