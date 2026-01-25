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

    [Header("Segment-Based Grid (New)")]
    [Tooltip("Use segment controllers from scene instead of generating programmatically")]
    public bool useSegmentControllers = false;
    
    [Tooltip("Segment controllers in scene. If empty and useSegmentControllers is true, will auto-find in children.")]
    [SerializeField] private List<GridSegmentController> segmentControllers = new List<GridSegmentController>();
    
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
    
    // LEGACY: Path visuals (deprecated - use segment controllers instead)
    private List<GameObject> pathVisuals = new List<GameObject>();
    
    // ADVANCED GRID: Segment-based grid
    private List<GridSegment> gridSegments = new List<GridSegment>();
    private int activeSegmentIndex = 0;

    // Object pooling (if enabled)
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTiles = new List<GameObject>();
    
    // Task 6: Line divider runtime state
    private bool lineDividerStyled = false;
    private Material lineDividerMaterial;
    private PlayerManager playerManager;
    private bool playerWasBelowLine = true; // Track previous state to avoid constant updates
    
    // Segment layout prefab instance tracking
    private GameObject instantiatedSegmentLayout = null;
    #endregion

    #region Properties
    public static GridManager Instance { get; private set; }
    public int Width => width;
    public int Height => height;
    public float TileSize => tileSize;
    public bool IsGridReady => isGridReady && tiles != null;
    public Vector3 GridCenter => transform.position + calculatedGridOffset;
    public Vector3 CalculatedGridOffset => calculatedGridOffset;
    public Vector3 MinWorldBounds => minWorldBounds;
    public Vector3 MaxWorldBounds => maxWorldBounds;
    
    // Task 6: Line divider properties
    public bool LineDividerEnabled => enableLineDivider;
    public int LineDividerRow => lineDividerRow;
    
    // Multi-segment grid detection
    public bool HasAdvancedPath => HasSegmentControllers; // Uses segment controllers
    
    // ADVANCED GRID: Segment properties
    public List<GridSegment> Segments => gridSegments;
    public int ActiveSegmentIndex => activeSegmentIndex;
    public GridSegment ActiveSegment => gridSegments.Count > activeSegmentIndex ? gridSegments[activeSegmentIndex] : null;
    public int SegmentCount => gridSegments.Count;
    public bool HasMultipleSegments => gridSegments.Count > 1;
    
    // SEGMENT CONTROLLERS: Scene-based segments
    public List<GridSegmentController> SegmentControllers => segmentControllers;
    public int SegmentControllerCount => segmentControllers.Count;
    public bool HasSegmentControllers => useSegmentControllers && segmentControllers.Count > 0;
    public GridSegmentController GetSegmentController(int index) => 
        index >= 0 && index < segmentControllers.Count ? segmentControllers[index] : null;
    
    /// <summary>
    /// Clears all registered segment controllers.
    /// Used when switching to a wave with custom segment layout.
    /// </summary>
    public void ClearSegmentControllers()
    {
        segmentControllers.Clear();
        DebugLog("Cleared all segment controllers");
    }
    
    /// <summary>
    /// Registers a segment controller for use.
    /// Used when applying wave segment layout prefabs.
    /// </summary>
    public void RegisterSegmentController(GridSegmentController segment)
    {
        if (segment == null) return;
        
        if (!segmentControllers.Contains(segment))
        {
            segmentControllers.Add(segment);
            // Keep sorted by segment index
            segmentControllers.Sort((a, b) => a.segmentIndex.CompareTo(b.segmentIndex));
            DebugLog($"Registered segment controller {segment.segmentIndex}");
        }
    }
    
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
        
        // Configure segment layout from stage's prefab
        ConfigureSegmentLayoutFromStage(stageData);
        
        // Configure line divider from stage data
        ConfigureLineDivider(stageData);
        
        DebugLog($"Grid configured for stage {stageIndex}: LineDivider at row {lineDividerRow}, Segments: {SegmentControllerCount}");
    }
    
    /// <summary>
    /// Instantiates and configures segment layout from stage's prefab.
    /// </summary>
    private void ConfigureSegmentLayoutFromStage(StageData stageData)
    {
        // Clean up any previously instantiated segment layout
        CleanupInstantiatedSegmentLayout();
        
        if (!stageData.HasSegmentLayoutPrefab)
        {
            DebugLog("No segment layout prefab for this stage - using default single-segment grid");
            return;
        }
        
        DebugLog($"Instantiating segment layout prefab for stage: {stageData.stageName}");
        
        // Instantiate the segment layout prefab
        instantiatedSegmentLayout = Instantiate(stageData.segmentLayoutPrefab);
        instantiatedSegmentLayout.name = $"StageSegmentLayout_{stageData.stageNumber}";
        
        // Find all GridSegmentController components in the instantiated prefab
        var newSegments = instantiatedSegmentLayout.GetComponentsInChildren<GridSegmentController>();
        
        if (newSegments.Length == 0)
        {
            DebugLog("WARNING: Segment layout prefab has no GridSegmentController components!");
            Destroy(instantiatedSegmentLayout);
            instantiatedSegmentLayout = null;
            return;
        }
        
        // Clear existing segment controllers and register the new ones
        ClearSegmentControllers();
        foreach (var segment in newSegments)
        {
            RegisterSegmentController(segment);
        }
        
        // Enable segment controller mode
        useSegmentControllers = true;
        
        // Generate tiles for each segment if not already initialized
        foreach (var segment in newSegments)
        {
            if (!segment.isInitialized)
            {
                GenerateTilesForSegment(segment);
                segment.MarkInitialized();
            }
        }
        
        DebugLog($"Registered {newSegments.Length} segments from stage layout prefab");
    }
    
    /// <summary>
    /// Cleans up any instantiated segment layout from a previous stage.
    /// </summary>
    private void CleanupInstantiatedSegmentLayout()
    {
        if (instantiatedSegmentLayout != null)
        {
            DebugLog("Cleaning up instantiated segment layout");
            Destroy(instantiatedSegmentLayout);
            instantiatedSegmentLayout = null;
        }
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
            DrawPathGizmos();
        }
    }
    
    /// <summary>
    /// DEPRECATED: Path gizmos removed - segment controllers handle their own visualization.
    /// </summary>
    private void DrawPathGizmos()
    {
        // No-op - segment controllers handle visualization
    }
    
    /// <summary>
    /// DEPRECATED: Path flow lines removed - use segment controllers.
    /// </summary>
    private void DrawPathFlowLines()
    {
        // No-op - segment controllers handle visualization
    }
    
    /// <summary>
    /// ADVANCED GRID: Converts MovementDirection to a world-space direction vector.
    /// </summary>
    private Vector3 GetDirectionVector(MovementDirection direction)
    {
        switch (direction)
        {
            case MovementDirection.Down: return -Vector3.forward;
            case MovementDirection.Up: return Vector3.forward;
            case MovementDirection.Right: return Vector3.right;
            case MovementDirection.Left: return -Vector3.right;
            default: return -Vector3.forward;
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

        DebugLog($"Generating grid: {width}x{height} with tile size {tileSize}, Segments: {SegmentControllerCount}");

        // NEW: Check for segment controllers first (scene-based segments)
        if (useSegmentControllers)
        {
            CollectSegmentControllers();
            if (segmentControllers.Count > 0)
            {
                GenerateGridFromSegmentControllers();
                return;
            }
            else
            {
                Debug.LogWarning("[GridManager] useSegmentControllers is true but no controllers found. Falling back to standard generation.");
            }
        }

        // NOTE: L_Shape grid generation removed - use segment controllers for multi-segment layouts
        // Segment controllers are configured in scene and provide their own tile generation
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
    
    #region Segment Controller Grid Generation
    
    /// <summary>
    /// Collects segment controllers from the scene.
    /// If none are assigned in inspector, searches in children.
    /// </summary>
    private void CollectSegmentControllers()
    {
        // If already assigned in inspector, just validate and sort
        if (segmentControllers.Count > 0)
        {
            // Remove any null entries
            segmentControllers.RemoveAll(s => s == null);
            
            // Sort by segment index
            segmentControllers.Sort((a, b) => a.segmentIndex.CompareTo(b.segmentIndex));
            
            Debug.Log($"[GridManager] Using {segmentControllers.Count} segment controllers from inspector");
            return;
        }
        
        // Auto-find in children
        var foundControllers = GetComponentsInChildren<GridSegmentController>();
        if (foundControllers.Length > 0)
        {
            segmentControllers.AddRange(foundControllers);
            segmentControllers.Sort((a, b) => a.segmentIndex.CompareTo(b.segmentIndex));
            Debug.Log($"[GridManager] Found {segmentControllers.Count} segment controllers in children");
        }
        else
        {
            // Try finding in scene (not recommended but fallback)
            var allControllers = FindObjectsByType<GridSegmentController>(FindObjectsSortMode.None);
            if (allControllers.Length > 0)
            {
                segmentControllers.AddRange(allControllers);
                segmentControllers.Sort((a, b) => a.segmentIndex.CompareTo(b.segmentIndex));
                Debug.Log($"[GridManager] Found {segmentControllers.Count} segment controllers in scene");
            }
        }
    }
    
    /// <summary>
    /// Generates the grid using segment controllers.
    /// Each segment controller defines its own position, rotation, and dimensions.
    /// Tiles are parented under each segment's transform.
    /// </summary>
    private void GenerateGridFromSegmentControllers()
    {
        Debug.Log($"[GridManager] Generating grid from {segmentControllers.Count} segment controllers");
        
        // Clear old grid segments (legacy)
        gridSegments.Clear();
        activeSegmentIndex = 0;
        
        // Use first segment's dimensions for main tiles array (backwards compatibility)
        var primarySegment = segmentControllers[0];
        width = primarySegment.width;
        height = primarySegment.height;
        tileSize = primarySegment.tileSize;
        
        // Initialize main tiles array (for segment 0 / backwards compatibility)
        tiles = new Tile[width, height];
        
        if (useObjectPooling)
        {
            InitializeTilePool();
        }
        
        // Generate tiles for each segment
        int totalTiles = 0;
        foreach (var segController in segmentControllers)
        {
            GenerateTilesForSegmentController(segController);
            totalTiles += segController.width * segController.height;
        }
        
        // Recalculate grid metrics based on all segments
        CalculateGridMetricsFromSegments();
        
        FinalizeGridGeneration();
        
        Debug.Log($"[GridManager] Segment controller grid complete: {segmentControllers.Count} segments, {totalTiles} total tiles");
    }
    
    /// <summary>
    /// Public method to generate tiles for a segment.
    /// Used when applying wave segment layout prefabs at runtime.
    /// </summary>
    public void GenerateTilesForSegment(GridSegmentController segment)
    {
        GenerateTilesForSegmentController(segment);
    }
    
    /// <summary>
    /// Generates tiles for a single segment controller.
    /// Tiles are parented under the segment's transform.
    /// </summary>
    private void GenerateTilesForSegmentController(GridSegmentController segment)
    {
        if (segment == null) return;
        
        segment.InitializeTileArray();
        int tilesCreated = 0;
        
        Debug.Log($"[GridManager] Generating tiles for Segment {segment.segmentIndex}: {segment.width}x{segment.height} at {segment.transform.position}");
        
        for (int x = 0; x < segment.width; x++)
        {
            for (int y = 0; y < segment.height; y++)
            {
                CreateTileForSegmentController(segment, x, y);
                tilesCreated++;
            }
        }
        
        segment.MarkInitialized();
        Debug.Log($"[GridManager] Segment {segment.segmentIndex} complete: {tilesCreated} tiles created");
    }
    
    /// <summary>
    /// Creates a single tile for a segment controller, parented under the segment.
    /// </summary>
    private void CreateTileForSegmentController(GridSegmentController segment, int localX, int localY)
    {
        // Calculate LOCAL position (segment's transform handles world position/rotation)
        Vector3 localPosition = new Vector3(localX * segment.tileSize, 0f, localY * segment.tileSize);
        
        GameObject tileObj = GetTileObject();
        if (tileObj == null)
        {
            Debug.LogError($"[GridManager] GetTileObject() returned null for Seg{segment.segmentIndex}_{localX}_{localY}!");
            return;
        }
        
        // Configure tile object - PARENT UNDER SEGMENT
        tileObj.name = $"Tile_{localX}_{localY}";
        tileObj.transform.SetParent(segment.transform);
        tileObj.transform.localPosition = localPosition;
        tileObj.transform.localRotation = Quaternion.identity; // No rotation - segment handles it
        tileObj.transform.localScale = new Vector3(segment.tileSize, 1f, segment.tileSize);
        
        // Setup tile component
        Tile tile = tileObj.GetComponent<Tile>();
        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }
        tile.Init(localX, localY);
        
        // Register with segment controller
        segment.RegisterTile(localX, localY, tile, tileObj);
        
        // Also store in main tiles array if this is segment 0 (backwards compatibility)
        if (segment.segmentIndex == 0 && localX < width && localY < height)
        {
            tiles[localX, localY] = tile;
        }
        
        if (useObjectPooling)
        {
            activeTiles.Add(tileObj);
        }
    }
    
    /// <summary>
    /// Recalculates grid metrics (bounds) based on all segment controllers.
    /// </summary>
    private void CalculateGridMetricsFromSegments()
    {
        if (segmentControllers.Count == 0) return;
        
        var primarySegment = segmentControllers[0];
        
        // Set calculatedGridOffset to match primary segment's position
        // This ensures legacy code that uses calculatedGridOffset works correctly
        calculatedGridOffset = primarySegment.transform.position - transform.position;
        
        // Find bounding box across all segments
        Vector3 minBounds = Vector3.one * float.MaxValue;
        Vector3 maxBounds = Vector3.one * float.MinValue;
        
        foreach (var segment in segmentControllers)
        {
            // Check all corners of this segment
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    int cornerX = x == 0 ? 0 : segment.width - 1;
                    int cornerY = y == 0 ? 0 : segment.height - 1;
                    Vector3 worldPos = segment.LocalToWorldPosition(cornerX, cornerY);
                    
                    minBounds = Vector3.Min(minBounds, worldPos);
                    maxBounds = Vector3.Max(maxBounds, worldPos);
                }
            }
        }
        
        // Add tile size padding
        maxBounds += new Vector3(primarySegment.tileSize, 0, primarySegment.tileSize);
        
        minWorldBounds = minBounds;
        maxWorldBounds = maxBounds;
        
        Debug.Log($"[GridManager] Grid bounds: min={minBounds}, max={maxBounds}");
    }
    
    #endregion
    
    #region L-Shape Grid Generation
    
    /// <summary>
    /// ADVANCED GRID: Generates an L-shaped grid using two segments.
    /// Segment 1: Vertical (width x height) - standard orientation
    /// Segment 2: Horizontal (width x (height + width)) - rotated 90°, with overlap
    /// </summary>
    private void GenerateLShapeGrid()
    {
        Debug.Log($"[GridManager] GenerateLShapeGrid() - Creating Segment 1 = {width}x{height}, Segment 2 = {width}x{height + width}");
        DebugLog($"Generating L-Shape grid: Segment 1 = {width}x{height}, Segment 2 = {width}x{height + width}");
        
        // Clear existing segments
        gridSegments.Clear();
        activeSegmentIndex = 0;
        
        // Create the two segments
        gridSegments = GridSegment.CreateLShape(width, height);
        Debug.Log($"[GridManager] Created {gridSegments.Count} segments");
        
        // Calculate world offsets for segment 2
        CalculateSegmentOffsets();
        
        // Generate tiles for segment 1 (uses standard tile array)
        tiles = new Tile[width, height];
        if (useObjectPooling)
        {
            InitializeTilePool();
        }
        Debug.Log($"[GridManager] Generating tiles for segment 0...");
        GenerateSegmentTiles(gridSegments[0]);
        
        // Generate tiles for segment 2 (stored in segment's own array)
        Debug.Log($"[GridManager] Generating tiles for segment 1...");
        GenerateSegmentTiles(gridSegments[1]);
        
        FinalizeGridGeneration();
        
        int seg2TileCount = gridSegments[1].width * gridSegments[1].height;
        int seg2NonOverlapTiles = seg2TileCount - (gridSegments[1].overlapRows * gridSegments[1].width);
        Debug.Log($"[GridManager] L-Shape COMPLETE: Seg1={width * height} tiles, Seg2={seg2NonOverlapTiles} new tiles (+ {gridSegments[1].overlapRows * gridSegments[1].width} shared)");
        DebugLog($"L-Shape grid generated: {gridSegments.Count} segments, Segment 1 tiles: {width * height}, Segment 2 tiles: {seg2TileCount}");
    }
    
    /// <summary>
    /// ADVANCED GRID: Calculates world offsets for each segment so they connect properly.
    /// </summary>
    private void CalculateSegmentOffsets()
    {
        if (gridSegments.Count < 2) return;
        
        var seg0 = gridSegments[0];
        var seg1 = gridSegments[1];
        
        // Segment 0 starts at the calculated grid offset
        seg0.worldOffset = transform.position + calculatedGridOffset;
        
        // L-SHAPE POSITIONING:
        // We want seg1's entry point (col 0, row height-1) to be adjacent to seg0's exit (col 0, row 0)
        //
        // Step 1: Calculate where seg0 (0, 0) is in world space
        Vector3 seg0ExitPoint = seg0.worldOffset; // col 0, row 0 is at the offset
        
        // Step 2: Calculate where we WANT seg1's entry point to be (one tile to the LEFT)
        Vector3 desiredSeg1Entry = seg0ExitPoint + new Vector3(-tileSize, 0, 0);
        
        // Step 3: Calculate what offset seg1 needs so its (0, height-1) lands at desiredSeg1Entry
        // With +90° rotation: local (0, 0, y*tileSize) → world-relative (y*tileSize, 0, 0)
        int seg1EntryRow = seg1.height - 1;
        Vector3 localToWorld_EntryOffset = new Vector3(seg1EntryRow * tileSize, 0, 0); // After +90° rotation
        
        // offset + localToWorld_EntryOffset = desiredSeg1Entry
        // offset = desiredSeg1Entry - localToWorld_EntryOffset
        seg1.worldOffset = desiredSeg1Entry - localToWorld_EntryOffset;
        
        Debug.Log($"[GridManager] L-Shape positioning:");
        Debug.Log($"  tileSize={tileSize}");
        Debug.Log($"  Seg0: offset={seg0.worldOffset}, size={width}x{height}");
        Debug.Log($"  Seg1: offset={seg1.worldOffset}, size={seg1.width}x{seg1.height}, rotation={seg1.rotationAngle}°, entryRow={seg1EntryRow}");
        Debug.Log($"  Seg0 exit (col 0, row 0): {seg0ExitPoint}");
        Debug.Log($"  Desired Seg1 entry: {desiredSeg1Entry}");
        Debug.Log($"  localToWorld offset for entry: {localToWorld_EntryOffset}");
        
        // Verify by computing seg1's entry point
        Vector3 actualSeg1Entry = seg1.LocalToWorldPosition(0, seg1EntryRow, tileSize);
        Debug.Log($"  Actual Seg1 entry (col 0, row {seg1EntryRow}): {actualSeg1Entry}");
        Debug.Log($"  Gap: {Vector3.Distance(seg0ExitPoint, actualSeg1Entry)} units (should be ~{tileSize})");
        
        DebugLog($"Segment offsets calculated: Seg0={seg0.worldOffset}, Seg1={seg1.worldOffset}");
    }
    
    /// <summary>
    /// ADVANCED GRID: Generates tiles for a specific segment.
    /// </summary>
    private void GenerateSegmentTiles(GridSegment segment)
    {
        if (segment == null) return;
        
        // Initialize segment's tile array
        segment.tiles = new Tile[segment.width, segment.height];
        
        int tilesCreated = 0;
        int tilesLinked = 0;
        
        Debug.Log($"[GridManager] GenerateSegmentTiles: Seg{segment.segmentIndex} dimensions {segment.width}x{segment.height}, overlap={segment.overlapRows} at row {segment.overlapStartRow}");
        
        for (int x = 0; x < segment.width; x++)
        {
            for (int y = 0; y < segment.height; y++)
            {
                // Skip overlap zone tiles for segment 2 (they're already created by segment 1)
                if (segment.segmentIndex > 0 && segment.IsInOverlapZone(x, y))
                {
                    // Link to segment 1's tile instead of creating new one
                    // Overlap zone: seg2 local (x, y) corresponds to seg1 local (x, y - overlapStartRow)
                    int seg1Y = y - segment.overlapStartRow;
                    if (seg1Y >= 0 && seg1Y < height && x < width)
                    {
                        segment.tiles[x, y] = tiles[x, seg1Y];
                        tilesLinked++;
                    }
                    continue;
                }
                
                CreateSegmentTileAtPosition(segment, x, y);
                tilesCreated++;
            }
        }
        
        segment.isInitialized = true;
        Debug.Log($"[GridManager] Seg{segment.segmentIndex} complete: {tilesCreated} tiles created, {tilesLinked} tiles linked");
        DebugLog($"Generated tiles for {segment.segmentName}: {segment.width}x{segment.height}");
    }
    
    /// <summary>
    /// ADVANCED GRID: Creates a tile at a specific position within a segment.
    /// </summary>
    private void CreateSegmentTileAtPosition(GridSegment segment, int localX, int localY)
    {
        Vector3 worldPosition = segment.LocalToWorldPosition(localX, localY, tileSize, 0f);
        GameObject tileObj = GetTileObject();
        
        if (tileObj == null)
        {
            Debug.LogError($"[GridManager] GetTileObject() returned null for Seg{segment.segmentIndex}_{localX}_{localY}!");
            return;
        }
        
        // Configure tile object
        tileObj.name = $"Tile_Seg{segment.segmentIndex}_{localX}_{localY}";
        tileObj.transform.SetParent(transform);
        tileObj.transform.position = worldPosition;
        tileObj.transform.rotation = segment.GetWorldRotation();
        tileObj.transform.localScale = new Vector3(tileSize, 1f, tileSize);
        
        // Ensure tile is at ground level
        Vector3 finalPos = tileObj.transform.position;
        finalPos.y = 0f;
        tileObj.transform.position = finalPos;
        
        // Setup tile component
        Tile tile = tileObj.GetComponent<Tile>();
        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }
        tile.Init(localX, localY);
        
        // Store in segment's array
        segment.tiles[localX, localY] = tile;
        
        // Also store in main tiles array if this is segment 0
        if (segment.segmentIndex == 0 && localX < width && localY < height)
        {
            tiles[localX, localY] = tile;
        }
        
        if (useObjectPooling)
        {
            activeTiles.Add(tileObj);
        }
    }
    
    #endregion

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
    
    /// <summary>
    /// ADVANCED GRID: Regenerates the grid as an L-shape with multiple segments.
    /// Called when switching from Standard to L_Shape path type.
    /// </summary>
    public void RegenerateAsLShape()
    {
        Debug.Log($"[GridManager] RegenerateAsLShape() called - destroying existing {width}x{height} grid");
        DebugLog("Regenerating grid as L-Shape...");
        
        // Destroy existing tiles
        DestroyGrid();
        isGridGenerated = false;
        isGridReady = false;
        
        // Recalculate metrics
        CalculateGridMetrics();
        
        Debug.Log($"[GridManager] Metrics recalculated, regenerating grid");
        
        // Regenerate grid (segment controllers handle multi-segment layouts)
        RegenerateGrid();
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

    #region Segment Management
    
    /// <summary>
    /// ADVANCED GRID: Sets the active segment index.
    /// </summary>
    public void SetActiveSegment(int index)
    {
        if (index >= 0 && index < gridSegments.Count)
        {
            activeSegmentIndex = index;
            DebugLog($"Active segment set to {index}: {gridSegments[index].segmentName}");
        }
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets a tile from the specified segment.
    /// </summary>
    public Tile GetSegmentTile(int segmentIndex, int localX, int localY)
    {
        if (segmentIndex < 0 || segmentIndex >= gridSegments.Count)
            return null;
        
        var segment = gridSegments[segmentIndex];
        if (!segment.IsValidLocalPosition(localX, localY))
            return null;
        
        return segment.tiles?[localX, localY];
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets a tile from the active segment.
    /// </summary>
    public Tile GetActiveSegmentTile(int localX, int localY)
    {
        return GetSegmentTile(activeSegmentIndex, localX, localY);
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if a position in the given segment is in the overlap zone.
    /// </summary>
    public bool IsInOverlapZone(int segmentIndex, int localX, int localY)
    {
        if (segmentIndex < 0 || segmentIndex >= gridSegments.Count)
            return false;
        
        return gridSegments[segmentIndex].IsInOverlapZone(localX, localY);
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the overlap zone bounds for segment 1 (the first segment).
    /// Returns (minY, maxY) where cubes entering this range should trigger transition.
    /// </summary>
    public (int minY, int maxY) GetSegment1OverlapBounds()
    {
        if (gridSegments.Count < 2)
            return (-1, -1);
        
        // Segment 1's overlap zone is at its bottom rows
        int overlapSize = gridSegments[1].overlapRows;
        return (0, overlapSize - 1);
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if a cube at the given position should trigger segment transition.
    /// For segment 1, this is when cubes enter the overlap zone at the bottom.
    /// </summary>
    public bool ShouldTriggerSegmentTransition(int segmentIndex, int localY)
    {
        if (!HasMultipleSegments || segmentIndex != 0)
            return false;
        
        var bounds = GetSegment1OverlapBounds();
        return localY >= bounds.minY && localY <= bounds.maxY;
    }
    
    /// <summary>
    /// ADVANCED GRID: Converts world position to segment and local coordinates.
    /// Returns (segmentIndex, localX, localY). SegmentIndex is -1 if not on any segment.
    /// </summary>
    public (int segmentIndex, int localX, int localY) WorldToSegmentPosition(Vector3 worldPos)
    {
        for (int i = 0; i < gridSegments.Count; i++)
        {
            var localPos = gridSegments[i].WorldToLocalPosition(worldPos, tileSize);
            if (localPos.x >= 0 && localPos.y >= 0)
            {
                return (i, localPos.x, localPos.y);
            }
        }
        return (-1, -1, -1);
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the world position for a segment's tile.
    /// </summary>
    public Vector3 SegmentToWorldPosition(int segmentIndex, int localX, int localY, float heightOffset = 0f)
    {
        if (segmentIndex < 0 || segmentIndex >= gridSegments.Count)
            return Vector3.zero;
        
        return gridSegments[segmentIndex].LocalToWorldPosition(localX, localY, tileSize, heightOffset);
    }
    
    #endregion

    #region Coordinate Conversion
    public Vector3 GridToWorldPosition(int x, int y, float heightOffset = 0)
    {
        // SEGMENT CONTROLLERS: Use first segment's coordinate system
        if (HasSegmentControllers && segmentControllers.Count > 0)
        {
            var primarySegment = segmentControllers[0];
            return primarySegment.LocalToWorldPosition(x, y, heightOffset);
        }
        
        // Legacy: Use calculated grid offset
        Vector3 basePosition = transform.position + calculatedGridOffset;
        return new Vector3(x * tileSize, heightOffset, y * tileSize) + basePosition;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        // SEGMENT CONTROLLERS: Use first segment's coordinate system
        if (HasSegmentControllers && segmentControllers.Count > 0)
        {
            var primarySegment = segmentControllers[0];
            return primarySegment.WorldToLocalPosition(worldPosition);
        }
        
        // Legacy: Use calculated grid offset
        Vector3 basePosition = transform.position + calculatedGridOffset;
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
        Vector3 seg0Base = transform.position + calculatedGridOffset;
        Vector3 localPos0 = worldPosition - seg0Base;
        int x0 = Mathf.RoundToInt(localPos0.x / tileSize);
        int y0 = Mathf.RoundToInt(localPos0.z / tileSize);
        
        if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
        {
            return (0, new Vector2Int(x0, y0));
        }
        
        // Check other segments
        if (HasMultipleSegments)
        {
            for (int i = 1; i < gridSegments.Count; i++)
            {
                var segment = gridSegments[i];
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
            Tile tile = GetTileAt(localPos);
            return tile != null && tile.IsPlayable;
        }
        else
        {
            var segment = gridSegments[segmentIndex];
            if (segment.tiles != null && segment.IsValidLocalPosition(localPos.x, localPos.y))
            {
                var tile = segment.tiles[localPos.x, localPos.y];
                return tile != null && tile.IsPlayable;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the tile at a world position from any segment.
    /// </summary>
    public Tile GetTileAtWorldPositionAnySegment(Vector3 worldPosition)
    {
        // SEGMENT CONTROLLERS: Check segment controllers first
        if (HasSegmentControllers)
        {
            return GetTileAtWorldPositionFromControllers(worldPosition);
        }
        
        // Legacy multi-segment support
        var (segmentIndex, localPos) = WorldToSegmentLocalPosition(worldPosition);
        
        if (segmentIndex < 0)
            return null;
        
        if (segmentIndex == 0)
        {
            return GetTileAt(localPos);
        }
        else if (segmentIndex < gridSegments.Count)
        {
            var segment = gridSegments[segmentIndex];
            if (segment.tiles != null && segment.IsValidLocalPosition(localPos.x, localPos.y))
            {
                return segment.tiles[localPos.x, localPos.y];
            }
        }
        
        return null;
    }

    public bool IsValidGridPosition(int x, int y)
    {
        // For multi-segment grids, check segment 0 first (standard behavior)
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            Tile tile = GetTileAt(x, y);
            return tile != null && tile.IsPlayable;
        }
        
        return false;
    }

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
        if (HasMultipleSegments)
        {
            for (int i = 1; i < gridSegments.Count; i++)
            {
                var segment = gridSegments[i];
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
        if (HasMultipleSegments)
        {
            for (int i = 1; i < gridSegments.Count; i++)
            {
                if (gridSegments[i].IsValidLocalPosition(x, y))
                    return i;
            }
        }
        
        return -1;
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
    
    #region Segment Controller Tile Access
    
    /// <summary>
    /// Gets the segment controller that contains the given world position.
    /// Returns null if no segment contains the position.
    /// </summary>
    public GridSegmentController GetSegmentControllerAtWorldPosition(Vector3 worldPos)
    {
        if (!HasSegmentControllers) return null;
        
        foreach (var segment in segmentControllers)
        {
            if (segment != null && segment.ContainsWorldPosition(worldPos))
            {
                return segment;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the tile at a world position, checking all segment controllers.
    /// </summary>
    public Tile GetTileAtWorldPositionFromControllers(Vector3 worldPos)
    {
        if (!HasSegmentControllers)
        {
            return GetTileAtWorldPosition(worldPos);
        }
        
        foreach (var segment in segmentControllers)
        {
            if (segment != null)
            {
                Tile tile = segment.GetTileAtWorldPosition(worldPos);
                if (tile != null)
                {
                    return tile;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if a world position is valid (on any segment).
    /// Works with both segment controllers and legacy segments.
    /// </summary>
    public bool IsValidWorldPosition(Vector3 worldPos)
    {
        if (HasSegmentControllers)
        {
            foreach (var segment in segmentControllers)
            {
                if (segment != null && segment.ContainsWorldPosition(worldPos))
                {
                    return true;
                }
            }
            return false;
        }
        else if (HasMultipleSegments)
        {
            // Use legacy segment checking
            var (segmentIndex, localPos) = WorldToSegmentLocalPosition(worldPos);
            return segmentIndex >= 0;
        }
        else
        {
            Vector2Int gridPos = WorldToGridPosition(worldPos);
            return IsValidGridPosition(gridPos);
        }
    }
    
    /// <summary>
    /// Gets segment index and local position for a world position (segment controllers).
    /// Returns (-1, invalid) if position is not on any segment.
    /// </summary>
    public (int segmentIndex, Vector2Int localPos) WorldToSegmentControllerPosition(Vector3 worldPos)
    {
        if (!HasSegmentControllers)
        {
            // Fallback to legacy
            return WorldToSegmentLocalPosition(worldPos);
        }
        
        for (int i = 0; i < segmentControllers.Count; i++)
        {
            var segment = segmentControllers[i];
            if (segment != null)
            {
                Vector2Int local = segment.WorldToLocalPosition(worldPos);
                if (local.x >= 0 && local.y >= 0)
                {
                    return (segment.segmentIndex, local);
                }
            }
        }
        
        return (-1, new Vector2Int(-1, -1));
    }
    
    /// <summary>
    /// Gets tile from a segment controller by index and local position.
    /// </summary>
    public Tile GetTileFromController(int segmentIndex, int localX, int localY)
    {
        var segment = GetSegmentController(segmentIndex);
        return segment?.GetTile(localX, localY);
    }
    
    /// <summary>
    /// Gets tile from a segment controller by index and local position.
    /// </summary>
    public Tile GetTileFromController(int segmentIndex, Vector2Int localPos)
    {
        return GetTileFromController(segmentIndex, localPos.x, localPos.y);
    }
    
    #endregion
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
        
        // Safety check: ensure we have a valid row to remove
        if (rowToRemove >= height)
        {
            DebugLog($"⚠️ ROW PENALTY: Cannot remove row {rowToRemove} - exceeds grid height {height}. Aborting.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        // Safety check: prevent removing if we'd have too few rows left
        int remainingRows = height - (rowToRemove + 1);
        if (remainingRows < 3)
        {
            DebugLog($"⚠️ ROW PENALTY: Cannot remove row {rowToRemove} - would leave only {remainingRows} rows. Minimum 3 rows required. Aborting.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        DebugLog($"⚠️ ROW PENALTY: Removing bottom row {rowToRemove} due to Unit cube escape penalty (will leave {remainingRows} rows)");
        
        // Fire start event (for future animation systems)
        OnBottomRowRemovalStarted?.Invoke(rowToRemove);
        
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
        
        // Update grid bounds BEFORE adjusting player (so AdjustPlayerPosition knows the old bottom)
        int oldBottom = bottom;
        bottom = Mathf.Min(bottom + 1, height - 1);
        
        // Adjust player position if they were on the removed row
        // Pass rowToRemove to the adjustment method
        AdjustPlayerPositionAfterRowRemoval(rowToRemove);
        
        // Fire completion event (for future animation systems)
        OnBottomRowRemovalCompleted?.Invoke(rowToRemove);
        
        isRemovingBottomRow = false;
        DebugLog($"✅ ROW PENALTY: Bottom row {rowToRemove} removal complete. New bottom: {bottom}, Grid height: {height}, Remaining playable rows: {height - bottom}");
    }
    
    /// <summary>
    /// Adjusts player position after a row has been removed.
    /// Called from RemoveBottomRowCoroutine with the specific row that was removed.
    /// </summary>
    private void AdjustPlayerPositionAfterRowRemoval(int removedRow)
    {
        var playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null) return;
        
        int playerY = playerManager.currentTilePosition.y;
        
        // If player was on or below the removed row, move them up
        if (playerY <= removedRow)
        {
            // Find the lowest available row above the removed row
            int safeRow = FindLowestPlayableRow();
            if (safeRow > removedRow)
            {
                DebugLog($"⚠️ ROW PENALTY: Moving player from row {playerY} (removed row {removedRow}) to safe row {safeRow}");
                playerManager.SetPosition(playerManager.currentTilePosition.x, safeRow);
            }
            else
            {
                DebugLog($"⚠️ ROW PENALTY: Player at row {playerY} but no safe row found above {removedRow}. Grid may be too small.");
                // Emergency fallback: move to top row
                playerManager.SetPosition(playerManager.currentTilePosition.x, height - 1);
            }
        }
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

    /// <summary>
    /// Finds the lowest playable row in the grid.
    /// Starts from bottom+1 (above the current bottom row) to avoid removed rows.
    /// </summary>
    private int FindLowestPlayableRow()
    {
        // Start from one row above the current bottom (which may have been removed)
        int startRow = Mathf.Max(bottom + 1, 1);
        
        for (int y = startRow; y < height; y++)
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

    #region Advanced Grid Path (DEPRECATED)
    
    // NOTE: GridPathType-based configuration is deprecated
    // Use GridSegmentController components in scene for multi-segment layouts
    
    /// <summary>
    /// DEPRECATED: Use GridSegmentController for multi-segment layouts.
    /// This method is kept for backward compatibility but does nothing.
    /// </summary>
    [System.Obsolete("Use GridSegmentController for multi-segment layouts")]
    public void ConfigureGridPath(StageData stageData)
    {
        // No-op - segment controllers handle grid layouts now
        Debug.Log("[GridManager] ConfigureGridPath is deprecated - use GridSegmentController");
    }
    
    /// <summary>
    /// DEPRECATED: Use segment controllers for multi-segment layouts.
    /// </summary>
    [System.Obsolete("Use GridSegmentController for multi-segment layouts")]
    public List<Vector3> GetTurnPointWorldPositions()
    {
        return new List<Vector3>(); // No-op - segment controllers handle visualization
    }
    
    /// <summary>
    /// Gets the movement direction at a grid position.
    /// For segment-based grids, returns the direction from the segment controller.
    /// </summary>
    public MovementDirection GetDirectionAtPosition(Vector2Int position, MovementDirection currentDirection)
    {
        // For segment controllers, each segment has its own localDirection
        // The cube's direction is set by its segment controller
        return currentDirection; // Return current - direction changes happen at segment transitions
    }
    
    /// <summary>
    /// DEPRECATED: Visualizes turn points - no longer used with segment controllers.
    /// </summary>
    private void VisualizeTurnPoints()
    {
        // No-op - segment controllers handle their own visualization
    }
    
    /// <summary>
    /// DEPRECATED: Resets path to standard. Use segment controllers instead.
    /// </summary>
    [System.Obsolete("Use GridSegmentController for multi-segment layouts")]
    public void ResetGridPath()
    {
        ClearPathVisuals();
        DebugLog("Grid path reset (deprecated - use segment controllers)");
    }
    
    /// <summary>
    /// DEPRECATED: Path visuals are no longer used. Segment controllers handle visualization.
    /// </summary>
    [System.Obsolete("Use GridSegmentController for multi-segment layouts")]
    public void CreatePathVisuals()
    {
        ClearPathVisuals();
        // No-op - segment controllers handle their own visualization
    }
    
    // NOTE: TurnPoint visual methods removed - segment controllers handle visualization now
    // The following methods were removed: CreateTurnPointVisual, CreateSingleTurnIndicator, CreatePathDirectionIndicators
    // Segment boundaries are now managed by GridSegmentController components in the scene
    
    
    /// <summary>
    /// ADVANCED GRID: Clears all path visual indicators.
    /// </summary>
    public void ClearPathVisuals()
    {
        foreach (var visual in pathVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }
        pathVisuals.Clear();
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
        string segmentInfo = HasSegmentControllers ? $" Segments:{SegmentControllerCount}" : "";
        return $"Grid: {width}x{height} ({status}) Tiles:{width * height} Markers:{GetMarkerCount()} Playable:{GetPlayableRowCount()}rows{segmentInfo}";
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
            ["Cube Definitions Assigned"] = cubeTypeDefinitions != null,
            
            // Segment information
            ["Has Segment Controllers"] = HasSegmentControllers,
            ["Segment Controller Count"] = SegmentControllerCount,
            ["Has Advanced Path"] = HasAdvancedPath
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
        
        // ADVANCED GRID: Reset path to standard
        ResetGridPath();
        
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