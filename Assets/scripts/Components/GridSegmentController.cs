using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Scene-based grid segment controller. Place this on a GameObject in the scene,
/// position/rotate it as needed, and GridManager will use it for tile generation.
/// Tiles are created as children of this transform, so the segment's position and
/// rotation automatically apply to all tiles.
/// </summary>
public class GridSegmentController : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("Segment Identity")]
    [Tooltip("Index of this segment (0 = primary/spawn segment). Used for ordering.")]
    public int segmentIndex = 0;
    
    [Header("Grid Dimensions")]
    [Tooltip("Number of columns (X axis)")]
    public int width = 5;
    
    [Tooltip("Number of rows (Z axis in local space)")]
    public int height = 20;
    
    [Tooltip("Size of each tile in world units")]
    public float tileSize = 3f;
    
    [Header("Movement Configuration")]
    [Tooltip("Direction cubes move through this segment (in local segment space)")]
    public MovementDirection localDirection = MovementDirection.Down;
    
    [Header("Camera Configuration")]
    [Tooltip("Camera rotation (Euler angles) when viewing this segment. Set in scene to match desired view.")]
    public Vector3 cameraRotation = new Vector3(50f, -15f, 0f);
    
    [Tooltip("Camera position offset from player when viewing this segment")]
    public Vector3 cameraOffset = new Vector3(-7.5f, 22.5f, -12.5f);
    
    [Header("Debug")]
    [Tooltip("Show segment bounds in Scene view")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;
    
    #endregion
    
    #region Runtime State
    
    /// <summary>
    /// Tiles belonging to this segment (indexed by local x, y)
    /// </summary>
    [System.NonSerialized]
    public Tile[,] tiles;
    
    /// <summary>
    /// Whether this segment has been initialized with tiles
    /// </summary>
    [System.NonSerialized]
    public bool isInitialized = false;
    
    /// <summary>
    /// List of tile GameObjects for cleanup
    /// </summary>
    private List<GameObject> tileObjects = new List<GameObject>();
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// World position of this segment (from transform)
    /// </summary>
    public Vector3 WorldPosition => transform.position;
    
    /// <summary>
    /// World rotation of this segment (from transform)
    /// </summary>
    public Quaternion WorldRotation => transform.rotation;
    
    /// <summary>
    /// Gets the spawn row for this segment (top row)
    /// </summary>
    public int SpawnRow => height - 1;
    
    /// <summary>
    /// Gets the escape row for this segment (bottom row)
    /// </summary>
    public int EscapeRow => 0;
    
    #endregion
    
    #region Coordinate Conversion
    
    /// <summary>
    /// Converts local segment coordinates to world position.
    /// Uses the segment's transform for positioning and rotation.
    /// </summary>
    public Vector3 LocalToWorldPosition(int localX, int localY, float heightOffset = 0f)
    {
        // Local position within segment (before transform)
        Vector3 localPos = new Vector3(localX * tileSize, heightOffset, localY * tileSize);
        
        // Transform to world space using this segment's transform
        return transform.TransformPoint(localPos);
    }
    
    /// <summary>
    /// Converts world position to local segment coordinates.
    /// Returns (-1, -1) if position is outside this segment.
    /// </summary>
    public Vector2Int WorldToLocalPosition(Vector3 worldPos)
    {
        // Transform from world space to local space
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        
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
    /// Checks if a world position is within this segment's bounds.
    /// Uses a tolerance for smoother transitions at boundaries.
    /// </summary>
    public bool ContainsWorldPosition(Vector3 worldPos)
    {
        // Transform from world space to local space
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        
        // Use continuous bounds checking with tolerance for boundary transitions
        float tolerance = tileSize * 0.5f; // Half a tile tolerance at edges
        
        float minX = -tolerance;
        float maxX = (width - 1) * tileSize + tolerance;
        float minZ = -tolerance;
        float maxZ = (height - 1) * tileSize + tolerance;
        
        return localPos.x >= minX && localPos.x <= maxX &&
               localPos.z >= minZ && localPos.z <= maxZ;
    }
    
    /// <summary>
    /// Gets the continuous local position (not rounded to tile indices).
    /// Useful for smooth movement calculations.
    /// </summary>
    public Vector3 WorldToLocalContinuous(Vector3 worldPos)
    {
        return transform.InverseTransformPoint(worldPos);
    }
    
    /// <summary>
    /// Checks if a world position is strictly within this segment (no tolerance).
    /// Returns true only if position maps to a valid tile index.
    /// </summary>
    public bool ContainsWorldPositionStrict(Vector3 worldPos)
    {
        Vector2Int local = WorldToLocalPosition(worldPos);
        return local.x >= 0 && local.y >= 0;
    }
    
    /// <summary>
    /// Checks if a local position is valid within this segment.
    /// </summary>
    public bool IsValidLocalPosition(int localX, int localY)
    {
        return localX >= 0 && localX < width && localY >= 0 && localY < height;
    }
    
    /// <summary>
    /// Checks if a local position is valid within this segment.
    /// </summary>
    public bool IsValidLocalPosition(Vector2Int pos)
    {
        return IsValidLocalPosition(pos.x, pos.y);
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
    /// Checks if a local position is at the escape edge (would fall off on next move).
    /// </summary>
    public bool IsAtEscapeEdge(Vector2Int localPos)
    {
        Vector2Int next = GetNextPosition(localPos);
        return !IsValidLocalPosition(next);
    }
    
    #endregion
    
    #region Tile Management
    
    /// <summary>
    /// Initializes the tile array. Called by GridManager during generation.
    /// </summary>
    public void InitializeTileArray()
    {
        tiles = new Tile[width, height];
        tileObjects.Clear();
        isInitialized = false;
    }
    
    /// <summary>
    /// Registers a tile at the given local position.
    /// </summary>
    public void RegisterTile(int localX, int localY, Tile tile, GameObject tileObj)
    {
        if (tiles == null)
        {
            InitializeTileArray();
        }
        
        if (IsValidLocalPosition(localX, localY))
        {
            tiles[localX, localY] = tile;
            tileObjects.Add(tileObj);
        }
    }
    
    /// <summary>
    /// Gets the tile at a local position.
    /// </summary>
    public Tile GetTile(int localX, int localY)
    {
        if (tiles == null || !IsValidLocalPosition(localX, localY))
        {
            return null;
        }
        return tiles[localX, localY];
    }
    
    /// <summary>
    /// Gets the tile at a local position.
    /// </summary>
    public Tile GetTile(Vector2Int localPos)
    {
        return GetTile(localPos.x, localPos.y);
    }
    
    /// <summary>
    /// Gets the tile at a world position (if within this segment).
    /// </summary>
    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int local = WorldToLocalPosition(worldPos);
        if (local.x < 0) return null;
        return GetTile(local);
    }
    
    /// <summary>
    /// Marks segment as fully initialized.
    /// </summary>
    public void MarkInitialized()
    {
        isInitialized = true;
        Debug.Log($"[GridSegmentController] Segment {segmentIndex} initialized: {width}x{height} tiles at {transform.position}");
    }
    
    /// <summary>
    /// Clears all tiles from this segment.
    /// </summary>
    public void ClearTiles()
    {
        foreach (var tileObj in tileObjects)
        {
            if (tileObj != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tileObj);
                }
                else
                {
                    DestroyImmediate(tileObj);
                }
            }
        }
        
        tileObjects.Clear();
        tiles = null;
        isInitialized = false;
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
        
        if (tileSize <= 0)
            issues.Add($"Segment {segmentIndex}: Tile size must be positive");
        
        return issues;
    }
    
    #endregion
    
    #region Editor Support
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        // Draw segment bounds
        Vector3 corner00 = LocalToWorldPosition(0, 0, 0.1f);
        Vector3 corner10 = LocalToWorldPosition(width - 1, 0, 0.1f);
        Vector3 corner01 = LocalToWorldPosition(0, height - 1, 0.1f);
        Vector3 corner11 = LocalToWorldPosition(width - 1, height - 1, 0.1f);
        
        // Offset to tile centers
        Vector3 halfTile = transform.TransformDirection(new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f));
        corner00 += halfTile;
        corner10 += halfTile;
        corner01 += halfTile;
        corner11 += halfTile;
        
        // Draw rectangle
        Gizmos.DrawLine(corner00, corner10);
        Gizmos.DrawLine(corner10, corner11);
        Gizmos.DrawLine(corner11, corner01);
        Gizmos.DrawLine(corner01, corner00);
        
        // Draw direction arrow
        Gizmos.color = Color.yellow;
        Vector3 center = (corner00 + corner11) * 0.5f;
        Vector3 arrowDir = Vector3.zero;
        
        switch (localDirection)
        {
            case MovementDirection.Down:
                arrowDir = transform.TransformDirection(Vector3.back) * (height * tileSize * 0.3f);
                break;
            case MovementDirection.Up:
                arrowDir = transform.TransformDirection(Vector3.forward) * (height * tileSize * 0.3f);
                break;
            case MovementDirection.Right:
                arrowDir = transform.TransformDirection(Vector3.right) * (width * tileSize * 0.3f);
                break;
            case MovementDirection.Left:
                arrowDir = transform.TransformDirection(Vector3.left) * (width * tileSize * 0.3f);
                break;
        }
        
        Gizmos.DrawLine(center, center + arrowDir);
        
        // Draw segment index label position
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw individual tile positions when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = LocalToWorldPosition(x, y, 0.05f);
                Gizmos.DrawWireCube(pos + transform.TransformDirection(new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f)), 
                    new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));
            }
        }
    }
    
    #endregion
    
    #region Context Menu
    
    [ContextMenu("Log Segment Info")]
    private void LogSegmentInfo()
    {
        Debug.Log($"[GridSegmentController] Segment {segmentIndex}:");
        Debug.Log($"  Position: {transform.position}");
        Debug.Log($"  Rotation: {transform.eulerAngles}");
        Debug.Log($"  Dimensions: {width}x{height}");
        Debug.Log($"  Tile Size: {tileSize}");
        Debug.Log($"  Direction: {localDirection}");
        Debug.Log($"  Corner (0,0): {LocalToWorldPosition(0, 0)}");
        Debug.Log($"  Corner ({width-1},{height-1}): {LocalToWorldPosition(width-1, height-1)}");
        Debug.Log($"  Initialized: {isInitialized}");
    }
    
    #endregion
}
