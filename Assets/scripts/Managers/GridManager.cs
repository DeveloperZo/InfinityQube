using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

public class GridManager : MonoBehaviour, IManagerDebugInterface
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
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    public bool showGridGizmos = false;
    public Color gizmoColor = Color.cyan;
    
    [Header("Task 6: Line Divider System")]
    [SerializeField] private bool enableLineDivider = false; // Toggle line divider system on/off (default: OFF for testing)
    [SerializeField] private int lineDividerRow = 10; // Default divider position (middle of 20-row grid)
    [SerializeField] private GameObject lineDividerVisual; // Visual indicator for line divider
    [SerializeField] private Color lineDividerColorSafe = new Color(0.2f, 0.5f, 1f, 0.7f); // Blue - player below line
    [SerializeField] private Color lineDividerColorDanger = new Color(1f, 0.2f, 0.2f, 0.7f); // Red - player above line
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
    
    // Task 6: Line divider runtime state
    private bool lineDividerStyled = false;
    private Material lineDividerMaterial;
    private PlayerManager playerManager;
    private bool playerWasBelowLine = true; // Track previous state to avoid constant updates
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
    
    // Task 6: Line divider properties
    public bool LineDividerEnabled => enableLineDivider;
    public int LineDividerRow => lineDividerRow;
    /// <summary>
    /// Task 6: Checks if a position is below the line divider (or always true if disabled)
    /// </summary>
    public bool IsPositionBelowLine(int y) => !enableLineDivider || y < lineDividerRow;
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
        // NOTE: Do NOT call InitializeLineDivider() here - line divider is configured
        // by stage data via HandleStageStart() → ConfigureLineDivider()
        // The serialized Inspector values are only used as fallback defaults.
        
        // Get PlayerManager reference for line divider color updates
        playerManager = FindFirstObjectByType<PlayerManager>();
        
        // Ensure line divider visual is hidden until stage configures it
        if (lineDividerVisual != null)
        {
            lineDividerVisual.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to stage events for line divider configuration
        GameEvents.OnStageStart += HandleStageStart;
    }
    
    private void OnDisable()
    {
        GameEvents.OnStageStart -= HandleStageStart;
    }
    
    private void Update()
    {
        // Task 6: Update line divider color based on player position
        UpdateLineDividerColor();
    }
    
    /// <summary>
    /// Configure grid and line divider from StageData
    /// </summary>
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        if (stageData == null) return;
        
        // Configure line divider from stage data
        ConfigureLineDivider(stageData);
        
        DebugLog($"Grid configured for stage {stageIndex}: LineDivider at row {lineDividerRow}");
    }
    
    /// <summary>
    /// Configure line divider settings from StageData
    /// </summary>
    public void ConfigureLineDivider(StageData stageData)
    {
        if (stageData == null) return;
        
        // Check explicit enable flag first, then validate position is meaningful
        bool positionValid = stageData.lineDividerStartY > 0 && stageData.lineDividerStartY < stageData.gridHeight;
        bool shouldEnable = stageData.enableLineDivider && positionValid;
        enableLineDivider = shouldEnable;
        
        if (shouldEnable)
        {
            lineDividerRow = stageData.lineDividerStartY;
            
            // Store penalty/reward values for use in MoveLineDivider
            _lineDividerEscapePenalty = stageData.lineDividerEscapePenalty;
            _lineDividerCaptureReward = stageData.lineDividerCaptureReward;
            
            DebugLog($"Line divider configured: Row={lineDividerRow}, Penalty={_lineDividerEscapePenalty}, Reward={_lineDividerCaptureReward}");
        }
        else
        {
            DebugLog($"Line divider DISABLED for stage (enableLineDivider={stageData.enableLineDivider}, positionValid={positionValid})");
        }
        
        UpdateLineDividerVisual();
    }
    
    // Cached line divider movement values
    private int _lineDividerEscapePenalty = 1;
    private int _lineDividerCaptureReward = 1;
    
    /// <summary>
    /// Move line divider based on escape (penalty) or capture (reward)
    /// </summary>
    public void OnCubeEscaped()
    {
        if (_lineDividerEscapePenalty > 0)
        {
            MoveLineDivider(_lineDividerEscapePenalty, false); // Move up (penalty)
        }
    }
    
    /// <summary>
    /// Move line divider based on capture (reward)
    /// </summary>
    public void OnCubeCaptured()
    {
        if (_lineDividerCaptureReward > 0)
        {
            MoveLineDivider(-_lineDividerCaptureReward, true); // Move down (reward)
        }
    }
    
    /// <summary>
    /// Task 6: Initializes the line divider system
    /// </summary>
    private void InitializeLineDivider()
    {
        if (!enableLineDivider)
        {
            DebugLog($"Line divider system DISABLED - marker placement unrestricted");
            return;
        }
        
        // Set default line divider position to middle of grid if not set
        if (lineDividerRow <= 0 || lineDividerRow >= height)
        {
            lineDividerRow = height / 2;
            DebugLog($"Line divider initialized to row {lineDividerRow} (middle of {height}-row grid)");
        }
        
        UpdateLineDividerVisual();
        DebugLog($"Line divider system ENABLED at row {lineDividerRow}");
    }
    
    /// <summary>
    /// Task 6: Moves the line divider up or down
    /// </summary>
    public void MoveLineDivider(int rows, bool isReward = true)
    {
        if (!enableLineDivider)
        {
            // Silently skip when disabled - no log spam
            return;
        }
        
        int oldRow = lineDividerRow;
        lineDividerRow = Mathf.Clamp(lineDividerRow + rows, 1, height - 1);
        
        string direction = rows > 0 ? "up" : "down";
        string reason = isReward ? "reward" : "penalty";
        DebugLog($"[Task 6] Line divider moved {direction} from row {oldRow} to row {lineDividerRow} ({reason})");
        
        UpdateLineDividerVisual();
    }
    
    /// <summary>
    /// Task 6: Enables or disables the line divider system at runtime
    /// </summary>
    public void SetLineDividerEnabled(bool enabled)
    {
        enableLineDivider = enabled;
        DebugLog($"[Task 6] Line divider system {(enabled ? "ENABLED" : "DISABLED")}");
        
        if (enabled)
        {
            InitializeLineDivider();
        }
        else
        {
            UpdateLineDividerVisual();
        }
    }
    
    /// <summary>
    /// Task 6: Updates the visual indicator for the line divider
    /// </summary>
    private void UpdateLineDividerVisual()
    {
        if (!enableLineDivider)
        {
            // Hide visual when disabled
            if (lineDividerVisual != null)
            {
                lineDividerVisual.SetActive(false);
            }
            return;
        }
        
        if (lineDividerVisual != null)
        {
            // Style the assigned visual (only once)
            if (!lineDividerStyled)
            {
                StyleLineDividerVisual();
                lineDividerStyled = true;
            }
            
            lineDividerVisual.SetActive(true);
            PositionLineDividerVisual();
            DebugLog($"[Task 6] Line divider visual positioned at row {lineDividerRow}");
        }
        else
        {
            DebugLog($"[Task 6] Line divider at row {lineDividerRow} (no visual assigned)");
        }
    }
    
    /// <summary>
    /// Task 6: Styles an assigned line divider visual (removes collider, applies material)
    /// Assign any Cube or GameObject in the Inspector - this will style it at runtime
    /// </summary>
    private void StyleLineDividerVisual()
    {
        if (lineDividerVisual == null) return;
        
        // Remove collider if present - visual only, no physics
        Collider col = lineDividerVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Create and apply transparent material (start with safe/blue color)
        Renderer renderer = lineDividerVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            lineDividerMaterial = new Material(Shader.Find("Standard"));
            lineDividerMaterial.color = lineDividerColorSafe; // Start with blue (safe)
            lineDividerMaterial.SetFloat("_Mode", 3); // Transparent mode
            lineDividerMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineDividerMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineDividerMaterial.SetInt("_ZWrite", 0);
            lineDividerMaterial.DisableKeyword("_ALPHATEST_ON");
            lineDividerMaterial.EnableKeyword("_ALPHABLEND_ON");
            lineDividerMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lineDividerMaterial.renderQueue = 3000;
            renderer.material = lineDividerMaterial;
        }
        
        DebugLog("[Task 6] Styled line divider visual");
    }
    
    /// <summary>
    /// Task 6: Updates line divider color based on player position
    /// Blue = player below line (safe, can place markers)
    /// Red = player above line (danger, cannot place markers)
    /// </summary>
    private void UpdateLineDividerColor()
    {
        // Skip if line divider disabled or no visual/material
        if (!enableLineDivider || lineDividerMaterial == null || playerManager == null) return;
        
        // Check if player is below the line
        bool playerIsBelowLine = playerManager.currentTilePosition.y < lineDividerRow;
        
        // Only update color if state changed (performance optimization)
        if (playerIsBelowLine != playerWasBelowLine)
        {
            playerWasBelowLine = playerIsBelowLine;
            Color targetColor = playerIsBelowLine ? lineDividerColorSafe : lineDividerColorDanger;
            lineDividerMaterial.color = targetColor;
            
            DebugLog($"[Task 6] Line divider color changed: {(playerIsBelowLine ? "BLUE (safe)" : "RED (danger)")}");
        }
    }
    
    /// <summary>
    /// Task 6: Checks if player is currently in safe zone (below line divider)
    /// Returns true if line divider is disabled OR player is below the line
    /// </summary>
    public bool IsPlayerInSafeZone()
    {
        if (!enableLineDivider) return true; // Always safe if disabled
        if (playerManager == null) return true; // Default to safe if no player
        return playerManager.currentTilePosition.y < lineDividerRow;
    }
    
    /// <summary>
    /// Task 6: Positions the line divider visual at the current divider row
    /// </summary>
    private void PositionLineDividerVisual()
    {
        if (lineDividerVisual == null) return;
        
        // Position: center of grid width, at the divider row boundary
        // The line sits at the BOTTOM edge of the divider row (markers allowed below, not on or above)
        float gridWidth = (width - 1) * tileSize;
        float centerX = gridWidth / 2f;
        
        // Position at the boundary between lineDividerRow-1 and lineDividerRow
        Vector3 lineWorldPos = GridToWorldPosition(0, lineDividerRow, 0.1f); // Slightly above ground
        lineWorldPos.x = centerX + (transform.position + calculatedGridOffset).x;
        lineWorldPos.z -= tileSize * 0.5f; // Position at the boundary between rows
        
        lineDividerVisual.transform.position = lineWorldPos;
        
        // Scale: span full grid width, thin line
        float lineWidth = gridWidth + tileSize; // Extend slightly past edges
        float lineHeight = 5f; // Vertical height
        float lineDepth = 0.08f; // Thin depth
        lineDividerVisual.transform.localScale = new Vector3(lineWidth, lineHeight, lineDepth);
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
            this.LogWarning("Multiple GridManagers found! Destroying duplicate.", EnableDebugLogs);
            Destroy(gameObject);
            return;
        }
    }

    private void ValidateConfiguration()
    {
        if (tilePrefab == null)
        {
            this.LogError("Tile prefab is not assigned in GridManager!");
            enabled = false;
            return;
        }

        if (cubeTypeDefinitions == null)
        {
            this.LogWarning("CubeTypeDefinitions not assigned - some features may not work", EnableDebugLogs);
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

        // Update dimensions with validation
        newWidth = Mathf.Max(3, newWidth);
        newHeight = Mathf.Max(9, newHeight);

        // Only proceed if dimensions actually changed
        if (width == newWidth && height == newHeight)
        {
            DebugLog("Grid dimensions unchanged, skipping resize");
            return;
        }

        // Store existing tile data before resize
        Tile[,] oldTiles = tiles;
        bool hadExistingGrid = tiles != null && isGridGenerated;

        // Update dimensions
        width = newWidth;
        height = newHeight;

        // Recalculate grid metrics
        CalculateGridMetrics();

        // If we had an existing grid, preserve what we can
        if (hadExistingGrid)
        {
            ResizeExistingGrid(oldTiles, oldWidth, oldHeight);
        }
        else
        {
            // Generate fresh grid
            RegenerateGrid();
        }

        DebugLog($"Grid successfully resized to {width}x{height}");
    }

    private void ResizeExistingGrid(Tile[,] oldTiles, int oldWidth, int oldHeight)
    {
        DebugLog($"Preserving existing grid data during resize");

        // Create new tiles array
        tiles = new Tile[width, height];

        // Copy existing tiles that fit in new dimensions and update their scale/position
        int preservedCount = 0;
        for (int x = 0; x < Mathf.Min(oldWidth, width); x++)
        {
            for (int y = 0; y < Mathf.Min(oldHeight, height); y++)
            {
                if (oldTiles[x, y] != null)
                {
                    tiles[x, y] = oldTiles[x, y];
                    
                    // Update tile scale and position to match new grid metrics
                    GameObject tileObj = oldTiles[x, y].gameObject;
                    Vector3 newWorldPosition = GridToWorldPosition(x, y, 0f);
                    tileObj.transform.position = newWorldPosition;
                    tileObj.transform.localScale = new Vector3(tileSize, 1f, tileSize);
                    
                    preservedCount++;
                }
            }
        }

        // Destroy tiles that no longer fit
        int destroyedCount = 0;
        for (int x = 0; x < oldWidth; x++)
        {
            for (int y = 0; y < oldHeight; y++)
            {
                // Destroy tiles outside new bounds
                if ((x >= width || y >= height) && oldTiles[x, y] != null)
                {
                    DestroyTileObject(oldTiles[x, y].gameObject);
                    destroyedCount++;
                }
            }
        }

        // Create new tiles for expanded areas
        int createdCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null)
                {
                    CreateTileAtPosition(x, y);
                    createdCount++;
                }
            }
        }

        // Update grid state
        isGridGenerated = true;
        isGridReady = true;

        // Update any systems that depend on grid size
        NotifyGridResized();

        DebugLog($"Grid resize complete: {preservedCount} preserved, {destroyedCount} destroyed, {createdCount} created");
    }

    private void NotifyGridResized()
    {
        // Recalculate world bounds with new dimensions
        UpdateWorldBounds();
        
        // Clamp player position to new grid bounds
        var playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
            // Get player's current world position and convert to grid position
            Vector2Int worldGridPos = WorldToGridPosition(playerManager.transform.position);
            Vector2Int clampedPos = new Vector2Int(
                Mathf.Clamp(worldGridPos.x, 0, width - 1),
                Mathf.Clamp(worldGridPos.y, 0, height - 1)
            );

            // Always update player position after resize to ensure they're on valid tile
            playerManager.SetPosition(clampedPos.x, clampedPos.y);
            DebugLog($"Player position set to ({clampedPos.x}, {clampedPos.y}) after grid resize to {width}x{height}");
        }

        // Update camera if it exists
        var cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ForceUpdatePosition();
        }

        // Clear any wave cubes that are now outside bounds
        var waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            CleanupOutOfBoundsCubes(waveManager);
        }
    }

    private void CleanupOutOfBoundsCubes(WaveManager waveManager)
    {
        var cubesToRemove = new List<CubeManager>();

        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !IsValidGridPosition(cube.position))
            {
                cubesToRemove.Add(cube);
            }
        }

        foreach (var cube in cubesToRemove)
        {
            waveManager.activeCubes.Remove(cube);
            if (cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }

        if (cubesToRemove.Count > 0)
        {
            DebugLog($"Removed {cubesToRemove.Count} cubes that were outside new grid bounds");
        }
    }

    // Helper method to ensure DestroyTileObject works with both pooled and non-pooled tiles
    private void DestroyTileObject(GameObject tileObj)
    {
        if (tileObj == null) return;

        if (useObjectPooling)
        {
            ReturnTileToPool(tileObj);
        }
        else
        {
            Destroy(tileObj);
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
            this.LogWarning("CubeTypeDefinitions not assigned!", EnableDebugLogs);
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
        if (EnableDebugLogs)
            this.Log(message, EnableDebugLogs);
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
    #endregion

    #region Batch Tile Operations
    /// <summary>
    /// Applies tile states to multiple tiles in batch
    /// </summary>
    public void BatchSetTileStates(Dictionary<Vector2Int, TileState> stateMap)
    {
        if (tiles == null || stateMap == null) return;

        int appliedCount = 0;
        foreach (var kvp in stateMap)
        {
            Vector2Int pos = kvp.Key;
            TileState state = kvp.Value;
            
            Tile tile = GetTileAt(pos);
            if (tile != null && tile.IsPlayable)
            {
                ApplyTileState(tile, state);
                appliedCount++;
            }
        }

        DebugLog($"Batch applied tile states to {appliedCount}/{stateMap.Count} tiles");
    }

    /// <summary>
    /// Applies a tile state pattern to the grid
    /// </summary>
    public void ApplyTileStatePattern(TileStatePattern pattern)
    {
        if (pattern == null || tiles == null) return;

        Dictionary<Vector2Int, TileState> stateMap = new Dictionary<Vector2Int, TileState>();
        
        foreach (var entry in pattern.entries)
        {
            Vector2Int pos = pattern.basePosition + entry.offset;
            if (IsValidGridPosition(pos))
            {
                stateMap[pos] = entry.state;
            }
        }

        BatchSetTileStates(stateMap);
        DebugLog($"Applied tile state pattern '{pattern.name}' with {pattern.entries.Count} entries");
    }

    /// <summary>
    /// Creates a tile state preset from current grid state
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
            Tile tile = GetTileAt(pos);
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
    /// Restores grid state from a preset
    /// </summary>
    public void RestoreFromPreset(TileStatePreset preset)
    {
        if (preset == null || tiles == null) return;

        int restoredCount = 0;
        foreach (var entry in preset.entries)
        {
            Tile tile = GetTileAt(entry.position);
            if (tile != null && tile.IsPlayable)
            {
                RestoreTileFromEntry(tile, entry);
                restoredCount++;
            }
        }

        DebugLog($"Restored {restoredCount}/{preset.entries.Count} tiles from preset '{preset.name}'");
    }

    /// <summary>
    /// Batch operations for markers
    /// </summary>
    public void BatchSetMarkers(List<Vector2Int> positions, bool placeMarkers)
    {
        if (tiles == null || positions == null) return;

        int processedCount = 0;
        foreach (var pos in positions)
        {
            bool success = placeMarkers ? PlaceMarker(pos.x, pos.y) : RemoveMarker(pos.x, pos.y);
            if (success) processedCount++;
        }

        string action = placeMarkers ? "placed" : "removed";
        DebugLog($"Batch {action} markers: {processedCount}/{positions.Count} successful");
    }

    /// <summary>
    /// Batch tile transformation operations
    /// </summary>
    public void BatchTransformTiles(List<Vector2Int> positions, CubeType transformType)
    {
        if (tiles == null || positions == null) return;

        int transformedCount = 0;
        foreach (var pos in positions)
        {
            Tile tile = GetTileAt(pos);
            if (tile != null && tile.IsPlayable)
            {
                tile.TransformTile(transformType);
                transformedCount++;
            }
        }

        DebugLog($"Batch transformed {transformedCount}/{positions.Count} tiles to {transformType} type");
    }

    /// <summary>
    /// Get tiles in a rectangular area
    /// </summary>
    public List<Tile> GetTilesInArea(Vector2Int topLeft, Vector2Int bottomRight)
    {
        List<Tile> tilesInArea = new List<Tile>();
        
        for (int x = topLeft.x; x <= bottomRight.x; x++)
        {
            for (int y = topLeft.y; y <= bottomRight.y; y++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null)
                {
                    tilesInArea.Add(tile);
                }
            }
        }

        return tilesInArea;
    }

    /// <summary>
    /// Get tiles matching specific criteria
    /// </summary>
    public List<Tile> GetTilesWithState(TileState state)
    {
        List<Tile> matchingTiles = new List<Tile>();
        
        if (tiles == null) return matchingTiles;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.currentState == state)
                {
                    matchingTiles.Add(tile);
                }
            }
        }

        return matchingTiles;
    }

    /// <summary>
    /// Get all tiles with markers
    /// </summary>
    public List<Tile> GetMarkedTiles()
    {
        List<Tile> markedTiles = new List<Tile>();
        
        if (tiles == null) return markedTiles;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTileAt(x, y);
                if (tile != null && tile.HasMarker)
                {
                    markedTiles.Add(tile);
                }
            }
        }

        return markedTiles;
    }

    /// <summary>
    /// Reset multiple tiles to normal state
    /// </summary>
    public void BatchResetTiles(List<Vector2Int> positions)
    {
        if (tiles == null || positions == null) return;

        int resetCount = 0;
        foreach (var pos in positions)
        {
            Tile tile = GetTileAt(pos);
            if (tile != null)
            {
                tile.ResetTile();
                resetCount++;
            }
        }

        DebugLog($"Batch reset {resetCount}/{positions.Count} tiles to normal state");
    }

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
    #endregion

    #region Row Management
    
    /// <summary>
    /// Event fired when bottom row removal starts (for animation hooks)
    /// </summary>
    public System.Action<int> OnBottomRowRemovalStarted;
    
    /// <summary>
    /// Event fired when bottom row removal completes (for animation hooks)
    /// </summary>
    public System.Action<int> OnBottomRowRemovalCompleted;
    
    private bool isRemovingBottomRow = false; // Prevent concurrent removals
    
    /// <summary>
    /// Removes the bottom row with a controlled visual transition.
    /// Uses coroutine for smooth animation and provides hooks for future animation systems.
    /// Works even if called at wave end - coroutine completes independently.
    /// </summary>
    public void RemoveBottomRow()
    {
        if (!IsGridReady) return;
        if (isRemovingBottomRow) 
        {
            DebugLog("RemoveBottomRow: Already removing a row, skipping duplicate call");
            return;
        }
        
        StartCoroutine(RemoveBottomRowCoroutine());
    }
    
    private IEnumerator RemoveBottomRowCoroutine()
    {
        isRemovingBottomRow = true;
        int rowToRemove = bottom;
        DebugLog($"Removing bottom row {rowToRemove} due to Unit cube escape penalty");
        
        // Fire start event (for future animation systems)
        OnBottomRowRemovalStarted?.Invoke(rowToRemove);
        
        // Safety check: ensure we have a valid row to remove
        if (rowToRemove >= height)
        {
            DebugLog($"⚠️ Cannot remove row {rowToRemove} - exceeds grid height {height}. Aborting.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        // Collect all tiles and cubes in the row
        List<Tile> tilesToRemove = new List<Tile>();
        List<CubeManager> cubesToRemove = new List<CubeManager>();
        
        for (int x = 0; x < width; x++)
        {
            Tile tile = GetTileAt(x, rowToRemove);
            if (tile != null)
            {
                tilesToRemove.Add(tile);
            }
        }
        
        // Find cubes on this row
        var allCubes = FindObjectsByType<CubeManager>(FindObjectsSortMode.None);
        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.position.y == rowToRemove)
            {
                cubesToRemove.Add(cube);
            }
        }
        
        // Simple transition: fade out tiles and cubes
        // TODO: Replace with proper animation system in future
        float transitionDuration = 0.5f; // Simple fade duration
        float elapsed = 0f;
        
        // Store initial renderers for fade (using MaterialPropertyBlock to avoid modifying shared materials)
        Dictionary<Tile, Renderer> tileRenderers = new Dictionary<Tile, Renderer>();
        Dictionary<CubeManager, Renderer> cubeRenderers = new Dictionary<CubeManager, Renderer>();
        Dictionary<Tile, MaterialPropertyBlock> tilePropertyBlocks = new Dictionary<Tile, MaterialPropertyBlock>();
        Dictionary<CubeManager, MaterialPropertyBlock> cubePropertyBlocks = new Dictionary<CubeManager, MaterialPropertyBlock>();
        
        foreach (var tile in tilesToRemove)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                tileRenderers[tile] = renderer;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                tilePropertyBlocks[tile] = block;
            }
        }
        
        foreach (var cube in cubesToRemove)
        {
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                cubeRenderers[cube] = renderer;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                cubePropertyBlocks[cube] = block;
            }
        }
        
        // Fade out animation using MaterialPropertyBlock (safe for shared materials)
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / transitionDuration);
            
            // Fade tiles
            foreach (var tile in tilesToRemove)
            {
                if (tile != null && tileRenderers.ContainsKey(tile) && tilePropertyBlocks.ContainsKey(tile))
                {
                    Renderer renderer = tileRenderers[tile];
                    MaterialPropertyBlock block = tilePropertyBlocks[tile];
                    
                    Color color = block.GetColor("_Color");
                    if (color == Color.clear) color = Color.white; // Default if no color property
                    color.a = alpha;
                    block.SetColor("_Color", color);
                    renderer.SetPropertyBlock(block);
                }
            }
            
            // Fade cubes (handle gracefully if destroyed during wave end)
            foreach (var cube in cubesToRemove)
            {
                if (cube != null && !cube.isDestroyed && cubeRenderers.ContainsKey(cube) && cubePropertyBlocks.ContainsKey(cube))
                {
                    Renderer renderer = cubeRenderers[cube];
                    if (renderer != null) // Additional null check in case cube was destroyed
                    {
                        MaterialPropertyBlock block = cubePropertyBlocks[cube];
                        
                        Color color = block.GetColor("_Color");
                        if (color == Color.clear) color = Color.white; // Default if no color property
                        color.a = alpha;
                        block.SetColor("_Color", color);
                        renderer.SetPropertyBlock(block);
                    }
                }
            }
            
            yield return null;
        }
        
        // Safety check: Verify grid is still valid and row is still within bounds
        // (Grid might have been resized during transition, though unlikely between waves)
        if (!IsGridReady || rowToRemove >= height)
        {
            DebugLog($"⚠️ Grid state changed during removal. Row {rowToRemove} no longer valid (height: {height}). Aborting cleanup.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        // Cleanup: Actually remove tiles and cubes
        // Note: This happens even if wave ended - grid state persists between waves
        foreach (var tile in tilesToRemove)
        {
            if (tile != null)
            {
                tile.MakeTileFall();
            }
        }
        
        // Remove cubes that still exist (some may have been destroyed at wave end)
        foreach (var cube in cubesToRemove)
        {
            if (cube != null && !cube.isDestroyed)
            {
                DebugLog($"Removing cube at ({cube.position.x}, {cube.position.y}) - row fell");
                Destroy(cube.gameObject);
            }
        }
        
        // Adjust player position if they were on the removed row
        AdjustPlayerPosition();
        
        // Update grid bounds (persists between waves)
        // Safety: Clamp bottom to ensure it doesn't exceed grid height
        bottom = Mathf.Min(bottom + 1, height - 1);
        
        // Fire completion event (for future animation systems)
        OnBottomRowRemovalCompleted?.Invoke(rowToRemove);
        
        isRemovingBottomRow = false;
        DebugLog($"Bottom row {rowToRemove} removal complete. New bottom: {bottom} (grid height: {height})");
    }

    private void RemoveCubesOnRow(int row)
    {
        var allCubes = FindObjectsByType<CubeManager>(FindObjectsSortMode.None);
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
        var playerManager = FindFirstObjectByType<PlayerManager>();
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

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        string status = isGridReady ? "READY" : "NOT_READY";
        return $"Grid: {width}x{height} ({status}) Tiles:{width * height} Markers:{GetMarkerCount()} Playable:{GetPlayableRowCount()}rows";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Grid Dimensions"] = $"{width}x{height}",
            ["Tile Size"] = tileSize,
            ["Total Tiles"] = width * height,
            ["Grid Ready"] = isGridReady,
            ["Grid Generated"] = isGridGenerated,
            ["Center At Origin"] = centerGridAtOrigin,
            ["Grid Offset"] = calculatedGridOffset,
            ["Min World Bounds"] = minWorldBounds,
            ["Max World Bounds"] = maxWorldBounds,
            ["Marker Count"] = GetMarkerCount(),
            ["Playable Row Count"] = GetPlayableRowCount(),
            ["Bottom Row"] = bottom,
            ["Use Object Pooling"] = useObjectPooling,
            ["Pool Size"] = useObjectPooling ? pooledTileCount : 0,
            ["Active Tiles"] = useObjectPooling ? activeTiles.Count : 0,
            ["Available Pool Tiles"] = useObjectPooling ? tilePool.Count : 0,
            ["Show Grid Gizmos"] = showGridGizmos,
            ["Tile Prefab Assigned"] = tilePrefab != null,
            ["Cube Definitions Assigned"] = cubeTypeDefinitions != null
        };
    }

    public void ResetToDefaults()
    {
        // Store original settings to restore later
        int originalWidth = width;
        int originalHeight = height;
        float originalTileSize = tileSize;
        bool originalCenterAtOrigin = centerGridAtOrigin;
        Vector3 originalGridOffset = gridOffset;
        
        // Destroy current grid
        DestroyGrid();
        
        // Reset grid state
        bottom = 0;
        isGridGenerated = false;
        isGridReady = false;
        
        // Clear any cached data
        if (useObjectPooling)
        {
            ResetPoolingSystem();
        }
        
        // Restore original dimensions and settings
        width = originalWidth;
        height = originalHeight;
        tileSize = originalTileSize;
        centerGridAtOrigin = originalCenterAtOrigin;
        gridOffset = originalGridOffset;
        
        // Recalculate grid metrics
        CalculateGridMetrics();
        
        // Regenerate grid
        GenerateGrid();
        
        if (EnableDebugLogs)
            this.Log("Reset to defaults completed", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for grid settings
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for grid settings
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}

#region Data Structures for Batch Operations
/// <summary>
/// Represents a pattern of tile states that can be applied to the grid
/// </summary>
[System.Serializable]
public class TileStatePattern
{
    public string name;
    public Vector2Int basePosition;
    public List<TileStatePatternEntry> entries = new List<TileStatePatternEntry>();
}

/// <summary>
/// Single entry in a tile state pattern
/// </summary>
[System.Serializable]
public class TileStatePatternEntry
{
    public Vector2Int offset;
    public TileState state;
}

/// <summary>
/// Preset that stores complete tile states for restoration
/// </summary>
[System.Serializable]
public class TileStatePreset
{
    public string name;
    public List<TileStateEntry> entries = new List<TileStateEntry>();
}

/// <summary>
/// Complete state information for a single tile
/// </summary>
[System.Serializable]
public class TileStateEntry
{
    public Vector2Int position;
    public TileState state;
    public bool hasMarker;
    public bool isBlackened;
    public bool isMatrixd;
    public int charges;
}
#endregion