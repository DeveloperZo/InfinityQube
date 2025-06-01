using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

public class GridManager : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Grid Setup")]
    public GameObject tilePrefab;
    public CubeTypeDefinitions cubeTypeDefinitions;

    [Header("Grid Dimensions")]
    public int width = 5;
    public int bottom = 0;
    public int height = 20;
    public float tileSize = 1f;

    [Header("Grid Positioning")]
    public bool centerGridAtOrigin = true;
    public Vector3 gridOffset = Vector3.zero; // Manual offset if needed

    [Header("Visual Settings")]
    public Material defaultTileMaterial;
    public Material[] specialTileMaterials; // For different tile states

    [Header("Performance")]
    public bool useObjectPooling = false;
    public int pooledTileCount = 100;

    [Header("Debug")]
    public bool showGridGizmos = false;
    public bool enableDebugLogs = true;
    public Color gizmoColor = Color.cyan;
    #endregion

    #region Runtime State
    [HideInInspector] public Tile[,] tiles;
    private bool isGridGenerated = false;
    private bool isGridReady = false;

    // Grid bounds cache for performance
    private Vector3 minWorldBounds;
    private Vector3 maxWorldBounds;
    private Vector3 calculatedGridOffset;

    // Object pooling (if enabled)
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTiles = new List<GameObject>();
    #endregion

    #region Properties
    public static GridManager Instance { get; private set; }
    public int Width => width;
    public int Height => height;
    public float TileSize => tileSize;
    public bool IsGridReady => isGridReady && tiles != null;
    public Vector3 GridCenter => transform.position + calculatedGridOffset;
    public Vector3 MinWorldBounds => minWorldBounds;
    public Vector3 MaxWorldBounds => maxWorldBounds;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        ValidateConfiguration();
        CalculateGridMetrics();
    }

    private void Start()
    {
        GenerateGrid();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGridGizmos)
        {
            DrawGridGizmos();
        }
    }

    private void OnDestroy()
    {
        CleanupGrid();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple GridManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void ValidateConfiguration()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned in GridManager!");
            enabled = false;
            return;
        }

        if (cubeTypeDefinitions == null)
        {
            Debug.LogWarning("CubeTypeDefinitions not assigned - some features may not work");
        }

        // Ensure reasonable bounds
        width = Mathf.Max(3, width);
        height = Mathf.Max(9, height);
        tileSize = Mathf.Max(0.1f, tileSize);
    }

    private void CalculateGridMetrics()
    {
        // Calculate grid offset for centering
        if (centerGridAtOrigin)
        {
            calculatedGridOffset = new Vector3(
                -width * tileSize * 0.5f + tileSize * 0.5f,
                0f,
                -height * tileSize * 0.5f + tileSize * 0.5f
            ) + gridOffset;
        }
        else
        {
            calculatedGridOffset = gridOffset;
        }

        // Update world bounds
        UpdateWorldBounds();

        DebugLog($"Grid metrics calculated: {width}x{height}, TileSize: {tileSize}, Offset: {calculatedGridOffset}");

        // DEBUG: Test a few coordinate conversions
        Vector3 testPos = GridToWorldPosition(1, 5, 0f);
        DebugLog($"DEBUG: Grid (1,5) converts to world {testPos}");
    }

    private void UpdateWorldBounds()
    {
        Vector3 basePos = transform.position + calculatedGridOffset;
        minWorldBounds = basePos;
        maxWorldBounds = basePos + new Vector3(
            (width - 1) * tileSize,
            0f,
            (height - 1) * tileSize
        );
    }
    #endregion

    #region Grid Generation
    public void GenerateGrid()
    {
        if (isGridGenerated && tiles != null)
        {
            DebugLog("Grid already generated, use RegenerateGrid() to force regeneration");
            return;
        }

        DebugLog($"Generating grid: {width}x{height} with tile size {tileSize}");

        StartGridGeneration();
    }

    private void StartGridGeneration()
    {
        tiles = new Tile[width, height];

        if (useObjectPooling)
        {
            InitializeTilePool();
        }

        GenerateAllTiles();
        FinalizeGridGeneration();
    }

    private void GenerateAllTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateTileAtPosition(x, y);
            }
        }
    }

    private void CreateTileAtPosition(int x, int y)
    {
        Vector3 worldPosition = GridToWorldPosition(x, y, 0f);
        GameObject tileObj = GetTileObject();

        ConfigureTileObject(tileObj, x, y, worldPosition);

        Tile tile = SetupTileComponent(tileObj, x, y);
        tiles[x, y] = tile;

        if (useObjectPooling)
        {
            activeTiles.Add(tileObj);
        }
    }

    private GameObject GetTileObject()
    {
        if (useObjectPooling && tilePool.Count > 0)
        {
            GameObject pooledTile = tilePool.Dequeue();
            pooledTile.SetActive(true);
            return pooledTile;
        }

        return Instantiate(tilePrefab);
    }

    private void ConfigureTileObject(GameObject tileObj, int x, int y, Vector3 worldPosition)
    {
        tileObj.name = $"Tile_{x}_{y}";
        tileObj.transform.SetParent(transform);
        tileObj.transform.position = worldPosition;
        tileObj.transform.localScale = new Vector3(tileSize, 1f, tileSize);

        // Ensure tile is at ground level
        Vector3 finalPos = tileObj.transform.position;
        finalPos.y = 0f;
        tileObj.transform.position = finalPos;
    }

    private Tile SetupTileComponent(GameObject tileObj, int x, int y)
    {
        Tile tile = tileObj.GetComponent<Tile>();
        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }

        tile.Init(x, y);
        return tile;
    }

    private void FinalizeGridGeneration()
    {
        isGridGenerated = true;
        isGridReady = true;

        DebugLog($"Grid generation complete: {width * height} tiles created and positioned");
    }

    public void RegenerateGrid()
    {
        DebugLog("Force regenerating grid...");
        DestroyGrid();
        isGridGenerated = false;
        isGridReady = false;
        CalculateGridMetrics();
        GenerateGrid();
    }
    #endregion

    #region Grid Management
    public void ResizeGrid(int newWidth, int newHeight)
    {
        DebugLog($"Resizing grid from {width}x{height} to {newWidth}x{newHeight}");

        // Store old values for comparison
        int oldWidth = width;
        int oldHeight = height;

        // Update dimensions
        width = Mathf.Max(3, newWidth);
        height = Mathf.Max(9, newHeight);

        // Only regenerate if dimensions actually changed
        if (width != oldWidth || height != oldHeight)
        {
            CalculateGridMetrics();
            RegenerateGrid();
            DebugLog($"Grid successfully resized to {width}x{height}");
        }
        else
        {
            DebugLog("Grid dimensions unchanged, skipping regeneration");
        }
    }

    public void DestroyGrid()
    {
        DebugLog("Destroying existing grid...");

        if (tiles != null)
        {
            DestroyAllTiles();
            tiles = null;
        }

        CleanupRemainingChildren();
        ResetPoolingSystem();

        isGridGenerated = false;
        isGridReady = false;
        DebugLog("Grid destruction complete");
    }

    private void DestroyAllTiles()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y] != null)
                {
                    DestroyTileObject(tiles[x, y].gameObject);
                }
            }
        }
    }

    private void DestroyTileObject(GameObject tileObj)
    {
        if (useObjectPooling)
        {
            ReturnTileToPool(tileObj);
        }
        else
        {
            Destroy(tileObj);
        }
    }

    private void CleanupRemainingChildren()
    {
        // Destroy any remaining child objects that weren't in the tiles array
        foreach (Transform child in transform)
        {
            if (!useObjectPooling || !tilePool.Contains(child.gameObject))
            {
                Destroy(child.gameObject);
            }
        }
    }
    #endregion

    #region Coordinate Conversion
    public Vector3 GridToWorldPosition(int x, int y, float heightOffset = 0)
    {
        Vector3 basePosition = transform.position + calculatedGridOffset;
        return new Vector3(x * tileSize, heightOffset, y * tileSize) + basePosition;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 basePosition = transform.position + calculatedGridOffset;
        Vector3 localPos = worldPosition - basePosition;

        int x = Mathf.RoundToInt(localPos.x / tileSize);
        int y = Mathf.RoundToInt(localPos.z / tileSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }

    public bool IsValidGridPosition(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        // Check if the tile has fallen
        Tile tile = GetTileAt(x, y);
        return tile != null && tile.IsPlayable;
    }

    public bool IsValidGridPosition(Vector2Int pos)
    {
        return IsValidGridPosition(pos.x, pos.y);
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || tiles == null)
            return null;

        return tiles[x, y]; // Return the tile even if fallen - let caller check IsPlayable
    }

    public Tile GetTileAt(Vector2Int pos)
    {
        return GetTileAt(pos.x, pos.y);
    }

    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return GetTileAt(gridPos);
    }
    #endregion

    #region Marker Management
    public bool PlaceMarker(int x, int y)
    {
        if (!IsValidGridPosition(x, y))
        {
            DebugLog($"Attempted to place marker at invalid position: ({x},{y})");
            return false;
        }

        Tile tile = GetTileAt(x, y);
        if (tile == null || tile.HasMarker)
        {
            return false;
        }

        tile.PlaceMarker();
        DebugLog($"Marker placed at ({x},{y})");
        return true;
    }

    public bool RemoveMarker(int x, int y)
    {
        if (!IsValidGridPosition(x, y))
            return false;

        Tile tile = GetTileAt(x, y);
        if (tile == null || !tile.HasMarker)
            return false;

        tile.ClearMarker();
        DebugLog($"Marker removed from ({x},{y})");
        return true;
    }

    public void ClearAllMarkers()
    {
        if (tiles == null) return;

        int markersCleared = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.HasMarker)
                {
                    tile.ClearMarker();
                    markersCleared++;
                }
            }
        }

        DebugLog($"Cleared {markersCleared} markers from grid");
    }

    public int GetMarkerCount()
    {
        if (tiles == null) return 0;

        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.HasMarker)
                    count++;
            }
        }
        return count;
    }
    #endregion

    #region Object Pooling System
    private void InitializeTilePool()
    {
        if (!useObjectPooling) return;

        DebugLog($"Initializing tile pool with {pooledTileCount} tiles");

        for (int i = 0; i < pooledTileCount; i++)
        {
            GameObject pooledTile = Instantiate(tilePrefab);
            pooledTile.SetActive(false);
            pooledTile.transform.SetParent(transform);
            tilePool.Enqueue(pooledTile);
        }
    }

    private void ReturnTileToPool(GameObject tileObj)
    {
        if (!useObjectPooling)
        {
            Destroy(tileObj);
            return;
        }

        tileObj.SetActive(false);
        tileObj.transform.SetParent(transform);

        // Reset tile state
        Tile tile = tileObj.GetComponent<Tile>();
        if (tile != null)
        {
            tile.ResetTile();
        }

        tilePool.Enqueue(tileObj);
        activeTiles.Remove(tileObj);
    }

    private void ResetPoolingSystem()
    {
        if (!useObjectPooling) return;

        // Return all active tiles to pool
        foreach (var tile in activeTiles.ToArray())
        {
            ReturnTileToPool(tile);
        }

        activeTiles.Clear();
    }
    #endregion

    #region Cube Type Definitions
    public CubeTypeDefinition GetCubeDefinition(CubeType type)
    {
        if (cubeTypeDefinitions == null)
        {
            Debug.LogWarning("CubeTypeDefinitions not assigned!");
            return null;
        }

        return cubeTypeDefinitions.GetDefinition(type);
    }

    public Material GetCubeTypeMaterial(CubeType type)
    {
        var definition = GetCubeDefinition(type);
        return definition?.material;
    }

    public GameObject GetCubeTypePrefab(CubeType type)
    {
        var definition = GetCubeDefinition(type);
        return definition?.prefab;
    }
    #endregion

    #region Debug & Gizmos
    private void DrawGridGizmos()
    {
        if (!isGridReady) return;

        Gizmos.color = gizmoColor;

        // Draw grid bounds
        Vector3 center = GridCenter;
        Vector3 size = new Vector3(width * tileSize, 0.1f, height * tileSize);
        Gizmos.DrawWireCube(center, size);

        // Draw individual tile positions
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 tilePos = GridToWorldPosition(x, y, 0.05f);
                Gizmos.DrawWireCube(tilePos, Vector3.one * tileSize * 0.9f);
            }
        }
    }

    public void DebugPrintGridInfo()
    {
        DebugLog("=== GRID DEBUG INFO ===");
        DebugLog($"Dimensions: {width}x{height}");
        DebugLog($"Tile Size: {tileSize}");
        DebugLog($"Grid Offset: {calculatedGridOffset}");
        DebugLog($"World Bounds: {minWorldBounds} to {maxWorldBounds}");
        DebugLog($"Grid Ready: {isGridReady}");
        DebugLog($"Markers: {GetMarkerCount()}");

        if (useObjectPooling)
        {
            DebugLog($"Pool: {tilePool.Count} available, {activeTiles.Count} active");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[GridManager] {message}");
    }
    #endregion

    #region Utility Methods
    private void CleanupGrid()
    {
        if (tiles != null)
        {
            DestroyGrid();
        }
    }

    // Keep backward compatibility
    [System.Obsolete("Use TileSize instead")]
    public float TileScale => tileSize;
    #endregion

    #region Row Management
    public void RemoveBottomRow()
    {
        if (!IsGridReady) return;

        DebugLog("Removing bottom row due to normal cube escape");

        // Make all tiles in row 0 fall
        for (int x = 0; x < width; x++)
        {
            Tile tile = GetTileAt(x, bottom);
            if (tile != null)
            {
                tile.MakeTileFall();
            }
        }

        // Remove any cubes that were on row 0
        RemoveCubesOnRow(bottom);

        // Adjust player position if they were on row 0
        AdjustPlayerPosition();
        bottom++;
        DebugLog("Bottom row removal complete");
    }

    private void RemoveCubesOnRow(int row)
    {
        var allCubes = FindObjectsOfType<CubeBehavior>();
        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.position.y == row)
            {
                DebugLog($"Removing cube at ({cube.position.x}, {cube.position.y}) - row fell");
                Destroy(cube.gameObject);
            }
        }
    }

    private void AdjustPlayerPosition()
    {
        var playerManager = FindObjectOfType<PlayerManager>();
        if (playerManager != null && playerManager.currentTilePosition.y == 0)
        {
            // Find the lowest available row
            int safeRow = FindLowestPlayableRow();
            if (safeRow > 0)
            {
                DebugLog($"Moving player from fallen row 0 to row {safeRow}");
                playerManager.SetPosition(playerManager.currentTilePosition.x, safeRow);
            }
        }
    }

    private int FindLowestPlayableRow()
    {
        for (int y = 1; y < height; y++)
        {
            bool rowIsPlayable = false;
            for (int x = 0; x < width; x++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.IsPlayable)
                {
                    rowIsPlayable = true;
                    break;
                }
            }
            if (rowIsPlayable) return y;
        }
        return height - 1; // Fallback to top row
    }

    public int GetPlayableRowCount()
    {
        int playableRows = 0;
        for (int y = 0; y < height; y++)
        {
            bool hasPlayableTile = false;
            for (int x = 0; x < width; x++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.IsPlayable)
                {
                    hasPlayableTile = true;
                    break;
                }
            }
            if (hasPlayableTile) playableRows++;
        }
        return playableRows;
    }

    public bool IsRowPlayable(int row)
    {
        if (!IsValidGridPosition(0, row)) return false;

        for (int x = 0; x < width; x++)
        {
            Tile tile = GetTileAt(x, row);
            if (tile != null && tile.IsPlayable)
            {
                return true;
            }
        }
        return false;
    }
    #endregion
}