using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;

/// <summary>
/// Manages player marker placement, triggering, and cube interactions.
/// Delegates collision handling to CubeCollisionManager and visuals to MarkerVisualManager.
/// </summary>
public class PlayerMarkerSystem : MonoBehaviour
{
    #region Inspector Configuration

    [Header("Marker Settings")]
    [SerializeField] private float perfectTimingWindow = 0.2f;

    [Header("Cube Marker Settings")]
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material poweredCubeMarkerMaterial;

    [Header("Manager References")]
    [SerializeField] private MarkerVisualManager visualManager;
    [SerializeField] private CubeCollisionManager collisionManager;

    #endregion

    #region Runtime State

    // Marker Collections - Four-tier player-placed marker system (Unit, Matrix, Recursion, Infinity)
    // Cube Markers are generated resources from collisions, not player-placed markers
    [SerializeField] public Queue<UnitMarker> UnitMarkers = new Queue<UnitMarker>();
    [SerializeField] public Queue<RecursionMarker> RecursionMarkers = new Queue<RecursionMarker>();
    [SerializeField] public Queue<MatrixMarker> MatrixMarkers = new Queue<MatrixMarker>();
    [SerializeField] public Queue<InfinityMarker> InfinityMarkers = new Queue<InfinityMarker>();
    public List<CubeMarker> cubeMarkers = new List<CubeMarker>(); // Generated resources, separate from player-placed markers
    public List<SwapMarker> swapMarkers = new List<SwapMarker>(); // Swap markers created from collisions

    // Player cube tracking
    public List<CubeManager> playerCubes = new List<CubeManager>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    
    // Swap preview system
    private List<GameObject> swapPreviewIcons = new List<GameObject>();
    private SwapMarker currentPreviewedSwapMarker = null;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;

    // Parent reference
    private PlayerActionManager actionManager;
    
    // Segment-aware references
    private WaveManager waveManager;
    private PlayerManager playerManager;

    #endregion

    #region Data Structures

    [System.Serializable]
    public class CubeMarker
    {
        public Vector2Int position;
        public CubeMarkerType type;
        public bool isPoweredUp = false;
        public float creationTime;
        public GameObject visualObject;
        public int size = 3;
        public int remainingUses = 1; // Default 1 trigger, Concentrated Expansion adds +1

        public CubeMarker(Vector2Int pos, CubeMarkerType markerType, int markerSize = 3, int uses = 1)
        {
            position = pos;
            type = markerType;
            size = markerSize;
            creationTime = Time.time;
            remainingUses = uses;
        }
    }

    public enum CubeMarkerType
    {
        Unit,
        Recursion,
        Matrix,
        Cube,
    }

    public enum MarkerType
    {
        Unit,
        Recursion,
        Matrix,
        Infinity,
        CubeMarker,
    }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        actionManager = FindFirstObjectByType<PlayerActionManager>();
    }

    void Start()
    {
        InitializeManagers();
    }

    public void Initialize(PlayerActionManager parent)
    {
        actionManager = parent;
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        if (actionManager == null) return;

        // Cache segment-aware references
        waveManager = actionManager.WaveManager;
        playerManager = actionManager.PlayerManager;
        
        // Initialize visual manager
        if (visualManager == null)
        {
            visualManager = GetComponent<MarkerVisualManager>();
            if (visualManager == null)
            {
                visualManager = gameObject.AddComponent<MarkerVisualManager>();
            }
        }
        visualManager?.Initialize(actionManager.GridManager);

        // Initialize collision manager
        if (collisionManager == null)
        {
            collisionManager = GetComponent<CubeCollisionManager>();
            if (collisionManager == null)
            {
                collisionManager = gameObject.AddComponent<CubeCollisionManager>();
            }
        }
        collisionManager?.Initialize(this, actionManager, actionManager.GridManager, visualManager);
    }

    private void OnDestroy()
    {
        // Clean up preview objects
        foreach (var preview in previewObjects)
        {
            if (preview != null) Destroy(preview);
        }
        previewObjects.Clear();
    }

    #endregion

    #region Unit Markers

    public bool PlaceUnitMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceUnitMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        // SEGMENT-AWARE: Get the wave's current segment for marker placement
        GridSegmentController waveSegment = waveManager?.CurrentSegmentController;
        
        var marker = new UnitMarker(position, Time.time, waveSegment);
        marker.visualObject = visualManager?.CreateUnitMarkerVisual(position, waveSegment);

        UnitMarkers.Enqueue(marker);
        actionManager.ConsumeUnitCharge(position);

        Debug.Log($"Unit marker placed at ({position.x}, {position.y}){(waveSegment != null ? $" on segment {waveSegment.segmentIndex}" : "")}");
        return true;
    }

    public bool RemoveUnitMarkerAt(Vector2Int position)
    {
        var markersArray = UnitMarkers.ToArray();
        var newQueue = new Queue<UnitMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseUnitMarker();
                actionManager.OnUnitMarkerRemoved();
                removed = true;
                Debug.Log($"Removed unit marker at ({position.x}, {position.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        UnitMarkers = newQueue;
        return removed;
    }

    public bool HasUnitMarkerAt(Vector2Int position)
    {
        return UnitMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextUnitMarker()
    {
        if (UnitMarkers.Count == 0) return false;

        var marker = UnitMarkers.Dequeue();
        actionManager.ReleaseUnitMarker();

        return TriggerUnitMarkerAt(marker.position, marker);
    }

    private bool TriggerUnitMarkerAt(Vector2Int position, UnitMarker marker)
    {
        // SEGMENT-AWARE: Use marker's segment for lookups
        var cubes = FindAllCubesAt(position, marker.segment);
        bool success = false;

        Vector3 worldPosition = GetWorldPositionOnSegment(position, marker.segment);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Unit, marker);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "unit", success, cubes.Count);
        }

        visualManager?.DestroyMarkerVisual(marker.visualObject);
        visualManager?.ShowMarkerTriggerEffect(position, marker.segment);

        return success;
    }

    #endregion

    #region Recursion Markers

    public bool PlaceRecursionMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceRecursionMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        // SEGMENT-AWARE: Get the wave's current segment for marker placement
        GridSegmentController waveSegment = waveManager?.CurrentSegmentController;
        
        var marker = new RecursionMarker(position, Time.time, waveSegment);
        marker.visualObject = visualManager?.CreateRecursionMarkerVisual(position, waveSegment);

        RecursionMarkers.Enqueue(marker);
        actionManager.ConsumeRecursionCharge();

        Debug.Log($"Recursion marker placed at ({position.x}, {position.y}){(waveSegment != null ? $" on segment {waveSegment.segmentIndex}" : "")}");
        return true;
    }

    public bool RemoveRecursionMarkerAt(Vector2Int position)
    {
        var markersArray = RecursionMarkers.ToArray();
        var newQueue = new Queue<RecursionMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseRecursionMarker();
                actionManager.OnRecursionMarkerRemoved();
                removed = true;
                Debug.Log($"Removed recursion marker at ({position.x}, {position.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        RecursionMarkers = newQueue;
        return removed;
    }

    public bool HasRecursionMarkerAt(Vector2Int position)
    {
        return RecursionMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextRecursionMarker()
    {
        if (RecursionMarkers.Count == 0) return false;

        var marker = RecursionMarkers.Dequeue();
        actionManager.ReleaseRecursionMarker();

        return TriggerRecursionMarkerAt(marker.position, marker);
    }

    private bool TriggerRecursionMarkerAt(Vector2Int position, RecursionMarker marker)
    {
        // SEGMENT-AWARE: Use marker's segment for lookups
        var cubes = FindAllCubesAt(position, marker.segment);
        bool success = false;

        Vector3 worldPosition = GetWorldPositionOnSegment(position, marker.segment);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            // Placed marker captures do NOT trigger same-type match bonus
            // Same-type bonuses only come from direct cube-to-cube collision
            success |= ProcessCubeCapture(cube, position, MarkerType.Recursion, marker, false);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "recursion", success, cubes.Count);
        }

        visualManager?.DestroyMarkerVisual(marker.visualObject);
        visualManager?.ShowMarkerTriggerEffect(position, marker.segment);

        return success;
    }

    #endregion

    #region Matrix Markers

    public bool PlaceMatrixMarker(Vector2Int centerPosition, int size)
    {
        if (!actionManager.CanPlaceMatrixMarker() || !IsValidPosition(centerPosition))
            return false;

        if (!CanPlaceMarkerAt(centerPosition))
            return false;

        // SEGMENT-AWARE: Get the wave's current segment for marker placement
        GridSegmentController waveSegment = waveManager?.CurrentSegmentController;
        
        MatrixMarker newMarker = new MatrixMarker(centerPosition, size, Time.time, waveSegment);
        newMarker.affectedPositions = GetAreaPositions(centerPosition, size);
        GameObject visual = visualManager?.CreateMatrixMarkerVisual(centerPosition, waveSegment);
        if (visual != null) newMarker.visualObjects.Add(visual);

        MatrixMarkers.Enqueue(newMarker);
        actionManager.ConsumeMatrixCharge();

        Debug.Log($"Matrix marker placed at ({centerPosition.x}, {centerPosition.y}){(waveSegment != null ? $" on segment {waveSegment.segmentIndex}" : "")}");
        return true;
    }

    public bool RemoveMatrixMarkerAt(Vector2Int centerPosition)
    {
        var markersArray = MatrixMarkers.ToArray();
        var newQueue = new Queue<MatrixMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.centerPosition == centerPosition && !removed)
            {
                foreach (var visual in marker.visualObjects)
                {
                    visualManager?.DestroyMarkerVisual(visual);
                }
                actionManager.ReleaseMatrixMarker();
                actionManager.OnMatrixMarkerRemoved();
                removed = true;
                Debug.Log($"Removed matrix marker at ({centerPosition.x}, {centerPosition.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        MatrixMarkers = newQueue;
        return removed;
    }

    public bool HasMatrixMarkerAt(Vector2Int centerPosition)
    {
        return MatrixMarkers.Any(m => m.centerPosition == centerPosition);
    }

    public bool TriggerNextMatrixMarker()
    {
        if (MatrixMarkers.Count == 0) return false;

        var marker = MatrixMarkers.Dequeue();
        actionManager.ReleaseMatrixMarker();

        return TriggerMatrixMarkerAt(marker);
    }

    private bool TriggerMatrixMarkerAt(MatrixMarker marker)
    {
        bool anySuccess = false;
        int totalCubesAffected = 0;

        Debug.Log($"Triggering matrix marker - expanding from center ({marker.centerPosition.x}, {marker.centerPosition.y}) to {marker.affectedPositions.Count} tiles");

        // SEGMENT-AWARE: Use marker's segment for world position
        Vector3 centerWorldPosition = GetWorldPositionOnSegment(marker.centerPosition, marker.segment);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, centerWorldPosition);

        foreach (var visual in marker.visualObjects)
        {
            visualManager?.DestroyMarkerVisual(visual);
        }

        foreach (var position in marker.affectedPositions)
        {
            // SEGMENT-AWARE: Get tile from marker's segment
            Tile tile = GetTileOnSegment(position, marker.segment);
            if (tile != null && position != marker.centerPosition)
            {
                visualManager?.SetTileHighlight(tile, new Color(0f, 1f, 0f, 0.7f), "AreaExpansion");
            }

            // Process wave cubes - SEGMENT-AWARE
            var cubes = FindAllCubesAt(position, marker.segment);
            totalCubesAffected += cubes.Count;
            foreach (var cube in cubes)
            {
                // Area marker captures do NOT trigger same-type match bonus
                // 3x3 markers only come from direct Matrix cube + Matrix cube collision
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Matrix, null, false);
            }

            // Process player cubes in the area (destroy them without creating cube markers) - SEGMENT-AWARE
            var playerCubesAtPosition = FindPlayerCubesAt(position, marker.segment);
            totalCubesAffected += playerCubesAtPosition.Count;
            foreach (var playerCube in playerCubesAtPosition)
            {
                if (playerCube != null && !playerCube.isDestroyed && playerCube.type != CubeType.Infinity)
                {
                    Debug.Log($"Matrix marker destroying player {playerCube.type} cube at ({position.x}, {position.y})");
                    DestroyPlayerCube(playerCube);
                    anySuccess = true;
                }
            }

            visualManager?.ShowMarkerTriggerEffect(position, marker.segment);
        }

        // Hide icons after capture completes
        foreach (var position in marker.affectedPositions)
        {
            // SEGMENT-AWARE: Get tile from marker's segment
            Tile tile = GetTileOnSegment(position, marker.segment);
            if (tile != null)
            {
                CubeMarkerStrobeEffect effect = tile.GetComponent<CubeMarkerStrobeEffect>();
                if (effect != null)
                {
                    effect.HideIcon();
                }
            }
        }

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(marker.centerPosition, "matrix", anySuccess, totalCubesAffected);
        }

        visualManager?.ClearAreaExpansionAfterDelay(marker.affectedPositions, marker.centerPosition, 1f);
        return anySuccess;
    }

    #endregion

    #region Infinity Markers

    public bool PlaceInfinityMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceInfinityMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        // SEGMENT-AWARE: Get the wave's current segment for marker placement
        GridSegmentController waveSegment = waveManager?.CurrentSegmentController;
        
        var marker = new InfinityMarker(position, Time.time, waveSegment);
        marker.visualObject = visualManager?.CreateInfinityMarkerVisual(position, waveSegment);

        InfinityMarkers.Enqueue(marker);
        actionManager.ConsumeInfinityCharge();

        Debug.Log($"Infinity marker placed at ({position.x}, {position.y}){(waveSegment != null ? $" on segment {waveSegment.segmentIndex}" : "")}");
        return true;
    }

    public bool RemoveInfinityMarkerAt(Vector2Int position)
    {
        var markersArray = InfinityMarkers.ToArray();
        var newQueue = new Queue<InfinityMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseInfinityMarker();
                removed = true;
                Debug.Log($"Removed infinity marker at ({position.x}, {position.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        InfinityMarkers = newQueue;
        return removed;
    }

    public bool HasInfinityMarkerAt(Vector2Int position)
    {
        return InfinityMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextInfinityMarker()
    {
        if (InfinityMarkers.Count == 0) return false;

        var marker = InfinityMarkers.Dequeue();
        actionManager.ReleaseInfinityMarker();

        return TriggerInfinityMarkerAt(marker.position, marker);
    }

    private bool TriggerInfinityMarkerAt(Vector2Int position, InfinityMarker marker)
    {
        // SEGMENT-AWARE: Use marker's segment for lookups
        var cubes = FindAllCubesAt(position, marker.segment);
        bool success = false;

        Vector3 worldPosition = GetWorldPositionOnSegment(position, marker.segment);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Infinity, marker);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "infinity", success, cubes.Count);
        }

        visualManager?.DestroyMarkerVisual(marker.visualObject);
        visualManager?.ShowMarkerTriggerEffect(position, marker.segment);

        Debug.Log($"Infinity marker triggered at ({position.x}, {position.y}) - Perfect: {marker.isPerfectTiming}");
        return success;
    }

    #endregion

    #region Swap Markers

    /// <summary>
    /// Creates a swap marker at the specified position with given charges.
    /// Swap markers are manually triggered (like cube markers).
    /// </summary>
    public void CreateSwapMarker(Vector2Int position, int charges, GridSegmentController segment = null)
    {
        // Check if a swap marker already exists at this position to prevent duplicates
        var existingMarker = swapMarkers.FirstOrDefault(m => m.position == position);
        if (existingMarker != null)
        {
            Debug.LogWarning($"[PlayerMarkerSystem] Swap marker already exists at ({position.x}, {position.y}), skipping duplicate creation");
            return;
        }

        var swapMarker = new SwapMarker(position, Time.time, charges, segment);
        swapMarker.visualObject = visualManager?.CreateSwapMarkerVisual(position, segment);

        swapMarkers.Add(swapMarker);

        Debug.Log($"[PlayerMarkerSystem] Swap marker created at ({position.x}, {position.y}) with {charges} charge(s){(segment != null ? $" on segment {segment.segmentIndex}" : "")}");
    }

    /// <summary>
    /// Applies default direction to swap markers that don't have a selection yet.
    /// Called when move forward occurs.
    /// </summary>
    public void ApplyDefaultDirectionsToSwapMarkers()
    {
        foreach (var swapMarker in swapMarkers)
        {
            // If direction is still at default (Horizontal) and hasn't been explicitly set, apply default
            if (swapMarker.swapDirection == SwapDirection.Horizontal && swapMarker.defaultSwapDirection == SwapDirection.Horizontal)
            {
                // Direction is already default, no change needed
                // But if player never selected, we want to ensure it's set
                swapMarker.swapDirection = swapMarker.defaultSwapDirection;
                
                // For empowered swaps, set opposite capture direction
                if (swapMarker.isEmpowered)
                {
                    swapMarker.captureDirection = SwapDirection.Vertical; // Opposite of horizontal swap
                }
                
                Debug.Log($"[PlayerMarkerSystem] Applied default direction (Horizontal) to swap marker at ({swapMarker.position.x}, {swapMarker.position.y})");
            }
        }
    }

    /// <summary>
    /// Triggers the next swap marker in the list (FIFO order).
    /// </summary>
    public bool TriggerNextSwapMarker()
    {
        if (swapMarkers.Count == 0) return false;

        var swapMarker = swapMarkers[0];
        swapMarkers.RemoveAt(0);

        // Destroy visual immediately
        if (swapMarker.visualObject != null)
        {
            visualManager?.DestroyMarkerVisual(swapMarker.visualObject);
        }

        return ExecuteSwap(swapMarker);
    }

    /// <summary>
    /// Executes a swap based on the swap marker configuration.
    /// + pattern: N↔S and W↔E swaps around center position.
    /// Edge handling: At edges, swap the two adjacent cells instead of failing.
    /// </summary>
    private bool ExecuteSwap(SwapMarker swapMarker)
    {
        if (swapMarker == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Attempted to execute null swap marker");
            return false;
        }

        Vector2Int centerPos = swapMarker.position;
        GridSegmentController segment = swapMarker.segment ?? waveManager?.CurrentSegmentController;

        // Use default direction if player didn't select one
        SwapDirection swapDir = swapMarker.swapDirection;
        if (swapDir == SwapDirection.Horizontal && swapMarker.defaultSwapDirection != SwapDirection.Horizontal)
        {
            // Check if default was set (this happens on move forward if no selection)
            swapDir = swapMarker.defaultSwapDirection;
        }

        Debug.Log($"[PlayerMarkerSystem] Executing swap at ({centerPos.x}, {centerPos.y}) - Direction: {swapDir}, Empowered: {swapMarker.isEmpowered}");

        GridManager grid = actionManager?.GridManager;
        if (grid == null)
        {
            Debug.LogError("[PlayerMarkerSystem] GridManager not found for swap execution");
            return false;
        }

        bool swapExecuted = false;

        if (swapDir == SwapDirection.Horizontal)
        {
            // Horizontal swap: determine positions based on edge detection
            Vector2Int pos1, pos2;
            
            if (centerPos.x == 0)
            {
                // Left edge: swap columns 0 and 1
                pos1 = new Vector2Int(0, centerPos.y);
                pos2 = new Vector2Int(1, centerPos.y);
                Debug.Log($"[PlayerMarkerSystem] Edge swap (left): ({pos1.x},{pos1.y}) ↔ ({pos2.x},{pos2.y})");
            }
            else if (centerPos.x == grid.Width - 1)
            {
                // Right edge: swap columns (width-2) and (width-1)
                pos1 = new Vector2Int(grid.Width - 2, centerPos.y);
                pos2 = new Vector2Int(grid.Width - 1, centerPos.y);
                Debug.Log($"[PlayerMarkerSystem] Edge swap (right): ({pos1.x},{pos1.y}) ↔ ({pos2.x},{pos2.y})");
            }
            else
            {
                // Normal: swap W ↔ E around center
                pos1 = new Vector2Int(centerPos.x - 1, centerPos.y);
                pos2 = new Vector2Int(centerPos.x + 1, centerPos.y);
            }
            
            if (IsValidSwapPosition(pos1, grid) && IsValidSwapPosition(pos2, grid))
            {
                swapExecuted |= SwapCubesAtPositions(pos1, pos2, segment);
            }
        }
        else // Vertical
        {
            // Vertical swap: determine positions based on edge detection
            Vector2Int pos1, pos2;
            
            if (centerPos.y == 0)
            {
                // Bottom edge: swap rows 0 and 1
                pos1 = new Vector2Int(centerPos.x, 0);
                pos2 = new Vector2Int(centerPos.x, 1);
                Debug.Log($"[PlayerMarkerSystem] Edge swap (bottom): ({pos1.x},{pos1.y}) ↔ ({pos2.x},{pos2.y})");
            }
            else if (centerPos.y == grid.Height - 1)
            {
                // Top edge: swap rows (height-2) and (height-1)
                pos1 = new Vector2Int(centerPos.x, grid.Height - 2);
                pos2 = new Vector2Int(centerPos.x, grid.Height - 1);
                Debug.Log($"[PlayerMarkerSystem] Edge swap (top): ({pos1.x},{pos1.y}) ↔ ({pos2.x},{pos2.y})");
            }
            else
            {
                // Normal: swap N ↔ S around center
                pos1 = new Vector2Int(centerPos.x, centerPos.y + 1);
                pos2 = new Vector2Int(centerPos.x, centerPos.y - 1);
            }
            
            if (IsValidSwapPosition(pos1, grid) && IsValidSwapPosition(pos2, grid))
            {
                swapExecuted |= SwapCubesAtPositions(pos1, pos2, segment);
            }
        }

        // If empowered, also capture along the capture axis
        if (swapMarker.isEmpowered && swapExecuted)
        {
            ExecuteCaptureAxis(swapMarker, centerPos, segment);
        }

        // Trigger audio feedback
        Vector3 worldPosition = GetWorldPositionOnSegment(centerPos, segment, 0f);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        return swapExecuted;
    }

    /// <summary>
    /// Checks if a position is valid for swapping (within grid bounds).
    /// </summary>
    private bool IsValidSwapPosition(Vector2Int position, GridManager grid)
    {
        return position.x >= 0 && position.x < grid.Width &&
               position.y >= 0 && position.y < grid.Height;
    }

    /// <summary>
    /// Swaps cubes at two positions. Handles empty cells (∞ can swap with empty).
    /// </summary>
    private bool SwapCubesAtPositions(Vector2Int pos1, Vector2Int pos2, GridSegmentController segment)
    {
        // Find cubes at both positions (single cube per position per design)
        var cube1 = FindCubeAtPosition(pos1, segment);
        var cube2 = FindCubeAtPosition(pos2, segment);

        // If both positions are empty, nothing to swap
        if (cube1 == null && cube2 == null)
        {
            return false;
        }

        // Swap positions
        if (cube1 != null)
        {
            cube1.position = pos2;
            // Update visual position
            UpdateCubeVisualPosition(cube1, pos2, segment);
        }

        if (cube2 != null)
        {
            cube2.position = pos1;
            // Update visual position
            UpdateCubeVisualPosition(cube2, pos1, segment);
        }

        Debug.Log($"[PlayerMarkerSystem] Swapped cubes: ({pos1.x}, {pos1.y}) ↔ ({pos2.x}, {pos2.y})");
        return true;
    }

    /// <summary>
    /// Finds a single cube at a position (per design, only one cube per position).
    /// </summary>
    private CubeManager FindCubeAtPosition(Vector2Int position, GridSegmentController segment)
    {
        // Check wave cubes first
        var waveCubes = FindAllCubesAt(position, segment);
        if (waveCubes.Count > 0)
        {
            return waveCubes[0]; // Single cube per position
        }

        // Check player cubes
        var playerCubesAtPos = FindPlayerCubesAt(position, segment);
        if (playerCubesAtPos.Count > 0)
        {
            return playerCubesAtPos[0]; // Single cube per position
        }

        return null;
    }

    /// <summary>
    /// Updates the visual position of a cube after swap.
    /// </summary>
    private void UpdateCubeVisualPosition(CubeManager cube, Vector2Int newPosition, GridSegmentController segment)
    {
        if (cube == null || cube.isDestroyed) return;

        Vector3 worldPos;
        if (segment != null && cube.CurrentSegment != null)
        {
            worldPos = segment.LocalToWorldPosition(newPosition.x, newPosition.y, 2f);
        }
        else
        {
            GridManager grid = actionManager?.GridManager;
            if (grid == null) return;
            worldPos = grid.GridToWorldPosition(newPosition.x, newPosition.y, 2f);
        }

        // Animate to new position
        StartCoroutine(AnimateCubeSwap(cube, worldPos));
    }

    /// <summary>
    /// Animates cube movement to new position after swap.
    /// </summary>
    private IEnumerator AnimateCubeSwap(CubeManager cube, Vector3 targetPosition)
    {
        if (cube == null || cube.isDestroyed) yield break;

        Vector3 startPos = cube.transform.position;
        float duration = 0.3f; // Quick swap animation
        float elapsed = 0f;

        while (elapsed < duration && cube != null && !cube.isDestroyed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cube.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        if (cube != null && !cube.isDestroyed)
        {
            cube.transform.position = targetPosition;
        }
    }

    /// <summary>
    /// Executes capture along the capture axis for empowered swaps.
    /// Infinity cubes are NOT captured (only moved).
    /// </summary>
    private void ExecuteCaptureAxis(SwapMarker swapMarker, Vector2Int centerPos, GridSegmentController segment)
    {
        SwapDirection captureDir = swapMarker.captureDirection;
        Vector2Int pos1, pos2;

        if (captureDir == SwapDirection.Horizontal)
        {
            // Capture W and E positions
            pos1 = new Vector2Int(centerPos.x - 1, centerPos.y);
            pos2 = new Vector2Int(centerPos.x + 1, centerPos.y);
        }
        else // Vertical
        {
            // Capture N and S positions
            pos1 = new Vector2Int(centerPos.x, centerPos.y + 1);
            pos2 = new Vector2Int(centerPos.x, centerPos.y - 1);
        }

        GridManager grid = actionManager?.GridManager;
        if (grid == null) return;

        // Capture cubes at both positions (except Infinity)
        if (IsValidSwapPosition(pos1, grid))
        {
            CaptureCubesAtPosition(pos1, segment);
        }
        if (IsValidSwapPosition(pos2, grid))
        {
            CaptureCubesAtPosition(pos2, segment);
        }
    }

    /// <summary>
    /// Captures cubes at a position (except Infinity cubes).
    /// </summary>
    private void CaptureCubesAtPosition(Vector2Int position, GridSegmentController segment)
    {
        var cubes = FindAllCubesAt(position, segment);
        foreach (var cube in cubes)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            
            // Infinity cubes cannot be captured
            if (cube.type == CubeType.Infinity)
            {
                continue;
            }

            // Process capture
            ProcessCubeCapture(cube, position, MarkerType.Recursion, null, false);
        }
    }

    /// <summary>
    /// Gets the swap marker at the specified position (if player is hovering over it).
    /// </summary>
    public SwapMarker GetSwapMarkerAtPosition(Vector2Int position)
    {
        return swapMarkers.FirstOrDefault(m => m.position == position);
    }

    /// <summary>
    /// Shows preview icons above N/S/E/W positions for swap marker.
    /// </summary>
    public void ShowSwapPreview(SwapMarker swapMarker)
    {
        if (swapMarker == null) return;
        
        // Hide previous preview if different marker
        if (currentPreviewedSwapMarker != swapMarker)
        {
            HideSwapPreview();
            currentPreviewedSwapMarker = swapMarker;
        }
        
        // Don't recreate if already showing
        if (swapPreviewIcons.Count > 0) return;
        
        Vector2Int centerPos = swapMarker.position;
        GridSegmentController segment = swapMarker.segment ?? waveManager?.CurrentSegmentController;
        
        // Create preview icons for N, S, E, W positions
        Vector2Int[] positions = new Vector2Int[]
        {
            new Vector2Int(centerPos.x, centerPos.y + 1), // North
            new Vector2Int(centerPos.x, centerPos.y - 1), // South
            new Vector2Int(centerPos.x + 1, centerPos.y), // East
            new Vector2Int(centerPos.x - 1, centerPos.y)  // West
        };
        
        string[] labels = new string[] { "N", "S", "E", "W" };
        
        GridManager grid = actionManager?.GridManager;
        if (grid == null) return;
        
        for (int i = 0; i < positions.Length; i++)
        {
            Vector2Int pos = positions[i];
            
            // Only show if position is valid
            if (pos.x >= 0 && pos.x < grid.Width && pos.y >= 0 && pos.y < grid.Height)
            {
                Vector3 worldPos = GetWorldPositionOnSegment(pos, segment, 1.5f); // Above tile
                
                // Create simple icon (text or sprite)
                GameObject icon = new GameObject($"SwapPreview_{labels[i]}_{pos.x}_{pos.y}");
                icon.transform.position = worldPos;
                
                // Add text mesh for label (simple preview)
                var textMesh = icon.AddComponent<TextMesh>();
                textMesh.text = labels[i];
                textMesh.fontSize = 20;
                textMesh.color = new Color(0.9f, 0.6f, 0.2f, 0.8f); // Amber/orange
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                
                // Face camera
                icon.transform.LookAt(Camera.main.transform);
                icon.transform.Rotate(0, 180, 0);
                
                swapPreviewIcons.Add(icon);
            }
        }
        
        UpdateSwapPreview(swapMarker);
    }

    /// <summary>
    /// Updates preview icons based on selected swap direction.
    /// </summary>
    public void UpdateSwapPreview(SwapMarker swapMarker)
    {
        if (swapMarker == null || swapPreviewIcons.Count == 0) return;
        
        // Highlight the positions that will be swapped
        // For horizontal: W and E
        // For vertical: N and S
        
        for (int i = 0; i < swapPreviewIcons.Count; i++)
        {
            var icon = swapPreviewIcons[i];
            if (icon == null) continue;
            
            var textMesh = icon.GetComponent<TextMesh>();
            if (textMesh == null) continue;
            
            // Determine which positions are active based on direction
            bool isActive = false;
            string label = textMesh.text;
            
            if (swapMarker.swapDirection == SwapDirection.Horizontal)
            {
                // Horizontal swap: W ↔ E
                isActive = (label == "W" || label == "E");
            }
            else // Vertical
            {
                // Vertical swap: N ↔ S
                isActive = (label == "N" || label == "S");
            }
            
            // Update color based on active state
            textMesh.color = isActive 
                ? new Color(1f, 0.8f, 0.3f, 1f) // Bright amber for active
                : new Color(0.9f, 0.6f, 0.2f, 0.5f); // Dim amber for inactive
        }
    }

    /// <summary>
    /// Hides swap preview icons.
    /// </summary>
    public void HideSwapPreview()
    {
        foreach (var icon in swapPreviewIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        swapPreviewIcons.Clear();
        currentPreviewedSwapMarker = null;
    }

    #endregion

    #region Cube Markers

    public void CreateCubeMarker(Vector2Int position, CubeMarkerType type = CubeMarkerType.Matrix, int size = 3, int uses = -1)
    {
        // Check if a cube marker already exists at this position to prevent duplicates
        // This prevents multiple detonations from the same collision
        var existingMarker = cubeMarkers.FirstOrDefault(m => m.position == position && m.type == type);
        if (existingMarker != null)
        {
            Debug.LogWarning($"[PlayerMarkerSystem] Cube marker already exists at ({position.x}, {position.y}), skipping duplicate creation");
            return;
        }
        
        // If uses not specified, calculate from attunement (Matrix only)
        int finalUses = uses;
        if (uses < 0)
        {
            finalUses = 1;
            if (type == CubeMarkerType.Matrix && AttunementManager.IsInitialized)
            {
                // Concentrated Expansion: +1 use for Matrix markers
                finalUses = AttunementManager.Instance.GetMatrixChargesPerTile();
            }
        }
        
        var cubeMarker = new CubeMarker(position, type, size, finalUses);
        cubeMarker.visualObject = visualManager?.CreateCubeMarkerVisual(position, type, size);

        cubeMarkers.Add(cubeMarker);

        Debug.Log($"Cube marker ({type}, size {size}x{size}, {finalUses} uses) created at ({position.x}, {position.y})");
    }

    public bool TriggerNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        
        // Prevent multiple simultaneous triggers - remove marker from list immediately
        // This ensures the marker can't be triggered again even if the method is called multiple times
        cubeMarkers.RemoveAt(0);
        
        // Decrement uses
        cubeMarker.remainingUses--;
        
        // If marker has remaining uses, add it back to the front of the queue
        if (cubeMarker.remainingUses > 0)
        {
            cubeMarkers.Insert(0, cubeMarker);
            Debug.Log($"[Concentrated Expansion] Cube marker has {cubeMarker.remainingUses} uses remaining");
        }
        else
        {
            // Marker is fully consumed, destroy visual immediately
            visualManager?.DestroyMarkerVisual(cubeMarker.visualObject);
        }

        return TriggerCubeMarkerAt(cubeMarker, cubeMarker.remainingUses <= 0);
    }

    public bool TriggerCubeMarkerAt(CubeMarker cubeMarker, bool destroyVisual = true)
    {
        // Prevent triggering if marker is null or already destroyed
        if (cubeMarker == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Attempted to trigger null cube marker");
            return false;
        }

        cubeMarkersTriggered++;

        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(cubeMarker.position.x, cubeMarker.position.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition, 1.2f);

        // Get affected positions and show icons above tiles
        List<Vector2Int> affectedPositions = GetAreaPositions(cubeMarker.position, cubeMarker.size);
        Color iconColor = cubeMarker.type switch
        {
            CubeMarkerType.Unit => Color.magenta,
            CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f),
            CubeMarkerType.Matrix => Color.cyan,
            CubeMarkerType.Cube => Color.yellow,
            _ => Color.white
        };
        
        // Show icons above all affected tiles (including bottom-left which had the beam)
        foreach (var pos in affectedPositions)
        {
            Tile tile = actionManager.GridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                CubeMarkerStrobeEffect effect = tile.GetComponent<CubeMarkerStrobeEffect>();
                if (effect != null)
                {
                    // Disable beam and show icon on all tiles
                    effect.SetEnabled(false);
                    effect.ShowIcon(iconColor);
                }
            }
        }

        // Disable or destroy visual based on remaining uses
        if (cubeMarker.visualObject != null)
        {
            if (destroyVisual)
            {
                // Last use - destroy completely
                visualManager?.DestroyMarkerVisual(cubeMarker.visualObject);
                cubeMarker.visualObject = null;
            }
        }

        var tempMatrixMarker = new MatrixMarker(cubeMarker.position, cubeMarker.size, Time.time);
        tempMatrixMarker.affectedPositions = GetAreaPositions(cubeMarker.position, cubeMarker.size);
        return TriggerMatrixMarkerAt(tempMatrixMarker);
    }

    public bool PowerUpNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        if (cubeMarker.isPoweredUp) return false;

        cubeMarker.isPoweredUp = true;

        visualManager?.DestroyMarkerVisual(cubeMarker.visualObject);
        cubeMarker.visualObject = visualManager?.CreatePoweredCubeMarkerVisual(cubeMarker.position, cubeMarker.type);

        Debug.Log($"Cube marker powered up at ({cubeMarker.position.x}, {cubeMarker.position.y})");
        return true;
    }

    #endregion

    #region Audio Event Integration

    private void TriggerMarkerAudioEvent(GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        if (actionManager != null)
        {
            var method = typeof(PlayerActionManager).GetMethod("TriggerAudioEvent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(actionManager, new object[] { eventType, worldPosition, intensity });
            }
            else
            {
                Debug.LogWarning("[PlayerMarkerSystem] Could not find TriggerAudioEvent method in PlayerActionManager");
            }
        }
    }

    #endregion

    #region Cube Interaction System

    public bool ProcessCubeCapture(CubeManager cube, Vector2Int position, MarkerType markerType, object marker = null, bool isSameTypeMatch = false)
    {
        if (cube == null || cube.isDestroyed) return false;

        if (!cube.CanBeCaptured())
        {
            Debug.Log($"Cube at ({position.x}, {position.y}) cannot be captured due to face status: {cube.GetActiveFaceStatus()}");
            return false;
        }

        // Multi-hit system: Recursion cubes require 2 hits to capture
        // EXCEPTION: Same-type match (Recursion+Recursion) is instant capture
        if (cube.type == CubeType.Recursion && !cube.isPlayerCube && !isSameTypeMatch)
        {
            // Apply damage instead of instant capture
            bool destroyed = cube.TakeDamage(1);
            
            if (!destroyed)
            {
                // Cube survived, show hit feedback but don't capture yet
                Debug.Log($"[PlayerMarkerSystem] Recursion cube at ({position.x}, {position.y}) hit! HP: {cube.currentHitPoints}/{cube.maxHitPoints}");
                
                // Show hit visual feedback
                if (visualManager != null)
                {
                    visualManager.ShowCaptureSuccessEffect(position, cube.type, cube.CurrentSegment);
                }
                
                return true; // Hit registered, but cube not captured yet
            }
            // If destroyed (HP <= 0), fall through to capture logic below
        }
        // Same-type match (Recursion+Recursion) bypasses multi-hit for instant capture

        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) with {markerType} marker{(isSameTypeMatch ? " (same-type match!)" : "")}");

        if (isSameTypeMatch)
        {
            switch (cube.type)
            {
                case CubeType.Matrix:
                    CreateCubeMarker(position, CubeMarkerType.Matrix, 3);
                    break;
                case CubeType.Recursion:
                    // Recursion + Recursion: Creates swap marker (handled in collision, not here)
                    // But if we get here, it means the cube was captured, so grant swap marker
                    // This is handled in HandleRecursionRecursionEmpoweredSwap
                    break;
            }
        }
        else if (cube.type == CubeType.Matrix)
        {
            CreateCubeMarker(position, CubeMarkerType.Matrix, 2);
        }

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnCubeCaptured(position, cube.type.ToString(), markerType.ToString());
        }

        // Fire cube capture event (this triggers GameEvents.OnCubeCaptured)
        cube.OnCubeCapture();

        // Show capture success visual feedback - SEGMENT-AWARE: use cube's segment
        if (visualManager != null)
        {
            visualManager.ShowCaptureSuccessEffect(position, cube.type, cube.CurrentSegment);
        }
        
        RemoveCubeFromWaveManager(cube);
        NotifyWaveManager(wm => wm.OnCubeCaptured(cube.type));
        Destroy(cube.gameObject);
        return true;
    }

    public List<CubeManager> FindAllCubesAt(Vector2Int position)
    {
        // Default to wave's current segment for backward compatibility
        return FindAllCubesAt(position, waveManager?.CurrentSegmentController);
    }
    
    /// <summary>
    /// Finds all cubes at a position on a specific segment.
    /// Segment-aware: only returns cubes that are on the specified segment.
    /// </summary>
    public List<CubeManager> FindAllCubesAt(Vector2Int position, GridSegmentController segment)
    {
        var cubes = new List<CubeManager>();

        var activeCubes = actionManager?.WaveManager?.activeCubes;
        if (activeCubes == null) return cubes;

        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y &&
                !cubes.Contains(cube))
            {
                // SEGMENT-AWARE: Check segment match by INDEX (not object reference)
                // This prevents issues if segment controller references differ but represent same segment
                if (segment != null && cube.CurrentSegment != null)
                {
                    if (cube.CurrentSegment.segmentIndex != segment.segmentIndex)
                        continue; // Skip cubes on different segments
                }
                cubes.Add(cube);
            }
        }

        return cubes;
    }

    public List<CubeManager> FindPlayerCubesAt(Vector2Int position)
    {
        // Default to wave's current segment for backward compatibility
        return FindPlayerCubesAt(position, waveManager?.CurrentSegmentController);
    }
    
    /// <summary>
    /// Finds all player cubes at a position on a specific segment.
    /// Segment-aware: only returns cubes that are on the specified segment.
    /// </summary>
    public List<CubeManager> FindPlayerCubesAt(Vector2Int position, GridSegmentController segment)
    {
        var cubes = new List<CubeManager>();

        if (playerCubes == null) return cubes;

        foreach (var cube in playerCubes)
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y &&
                !cubes.Contains(cube))
            {
                // SEGMENT-AWARE: Check segment match by INDEX (not object reference)
                // This prevents issues if segment controller references differ but represent same segment
                if (segment != null && cube.CurrentSegment != null)
                {
                    if (cube.CurrentSegment.segmentIndex != segment.segmentIndex)
                        continue; // Skip cubes on different segments
                }
                cubes.Add(cube);
            }
        }

        return cubes;
    }

    private void DestroyPlayerCube(CubeManager cube)
    {
        if (cube == null || cube.isDestroyed) return;

        // Remove from player cubes list
        if (playerCubes != null && playerCubes.Contains(cube))
        {
            playerCubes.Remove(cube);
        }

        // Destroy the game object
        if (cube.gameObject != null)
        {
            Destroy(cube.gameObject);
        }
    }

    private void RemoveCubeFromWaveManager(CubeManager cube)
    {
        if (cube.CanBeCaptured() && actionManager.WaveManager != null && cube.type != CubeType.Infinity)
        {
            actionManager.WaveManager.activeCubes.Remove(cube);
        }
    }

    private void NotifyWaveManager(System.Action<WaveManager> action)
    {
        if (actionManager.WaveManager != null) action(actionManager.WaveManager);
    }

    #endregion

    #region Player Cube System

    public void SpawnPlayerCubes()
    {
        if (actionManager == null || actionManager.WaveManager == null || actionManager.GridManager == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Cannot spawn player cubes - missing references");
            return;
        }

        var waveManager = actionManager.WaveManager;

        if (waveManager.cubePrefabs == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Cannot spawn player cubes - cube prefabs not available");
            return;
        }

        int spawnedCount = 0;

        // Process Unit markers
        var UnitMarkersArray = UnitMarkers.ToArray();
        foreach (var marker in UnitMarkersArray)
        {
            if (marker != null)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseUnitMarker(); // Release the on-grid slot
                SpawnPlayerCubeAt(marker.position, CubeType.Unit, false, marker.segment);
                spawnedCount++;
            }
        }
        UnitMarkers.Clear();

        // Process Matrix markers
        var matrixMarkersArray = MatrixMarkers.ToArray();
        foreach (var marker in matrixMarkersArray)
        {
            if (marker != null)
            {
                foreach (var visual in marker.visualObjects)
                {
                    visualManager?.DestroyMarkerVisual(visual);
                }
                actionManager.ReleaseMatrixMarker(); // Release the on-grid slot
                SpawnPlayerCubeAt(marker.centerPosition, CubeType.Matrix, true, marker.segment);
                spawnedCount++;
            }
        }
        MatrixMarkers.Clear();

        // Process Recursion markers
        var RecursionMarkersArray = RecursionMarkers.ToArray();
        foreach (var marker in RecursionMarkersArray)
        {
            if (marker != null)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseRecursionMarker(); // Release the on-grid slot
                SpawnPlayerCubeAt(marker.position, CubeType.Recursion, false, marker.segment);
                spawnedCount++;
            }
        }
        RecursionMarkers.Clear();

        // Process Infinity markers
        var infinityMarkersArray = InfinityMarkers.ToArray();
        foreach (var marker in infinityMarkersArray)
        {
            if (marker != null)
            {
                visualManager?.DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseInfinityMarker(); // Release the on-grid slot
                SpawnPlayerCubeAt(marker.position, CubeType.Infinity, false, marker.segment);
                spawnedCount++;
            }
        }
        InfinityMarkers.Clear();

        if (spawnedCount > 0)
        {
            Debug.Log($"[PlayerMarkerSystem] Spawned {spawnedCount} player cubes from markers");
        }
    }

    private void SpawnPlayerCubeAt(Vector2Int position, CubeType cubeType, bool isMatrixCube, GridSegmentController markerSegment = null)
    {
        var wm = actionManager.WaveManager;
        var grid = actionManager.GridManager;

        int prefabIndex = (int)cubeType;
        if (prefabIndex >= wm.cubePrefabs.Length || wm.cubePrefabs[prefabIndex] == null)
        {
            prefabIndex = (int)CubeType.Unit;
            if (prefabIndex >= wm.cubePrefabs.Length || wm.cubePrefabs[prefabIndex] == null)
            {
                Debug.LogWarning($"[PlayerMarkerSystem] No cube prefab available for type {cubeType}");
                return;
            }
            Debug.LogWarning($"[PlayerMarkerSystem] Prefab for {cubeType} not found, using Unit prefab");
        }

        var cubeData = new CubeData
        {
            type = cubeType,
            position = position,
            level = 1
        };

        // SEGMENT-AWARE: Use the marker's segment (where it was placed), fallback to wave's current segment
        // This fixes a regression where cubes would spawn at wrong position after segment transition
        GridSegmentController targetSegment = markerSegment ?? wm.CurrentSegmentController;
        Vector3 spawnPos;
        
        if (targetSegment != null)
        {
            // Use marker's segment for position calculation (not wave's current segment)
            spawnPos = targetSegment.LocalToWorldPosition(position.x, position.y, 2f);
            Debug.Log($"[PlayerMarkerSystem] Spawning player cube on marker's segment {targetSegment.segmentIndex}");
        }
        else
        {
            // Fallback to grid-based position
            spawnPos = grid.GridToWorldPosition(position.x, position.y, 2f);
        }
        
        GameObject cubeObj = Instantiate(wm.cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null)
        {
            cube = cubeObj.AddComponent<CubeManager>();
        }

        cube.Init(grid, cubeData, 2f);
        cube.isPlayerCube = true;
        cube.isMatrixCube = isMatrixCube;
        cube.usePhysics = false;
        cube.ConfigurePlayerCubePhysics();
        
        // SEGMENT-AWARE: Assign player cube to marker's segment
        if (targetSegment != null)
        {
            cube.SetSegmentController(targetSegment);
        }

        MakeCubeTranslucent(cube);
        playerCubes.Add(cube);

        Debug.Log($"[PlayerMarkerSystem] Spawned {cubeType} player cube at ({position.x}, {position.y}){(isMatrixCube ? " (area capture)" : "")}{(targetSegment != null ? $" on segment {targetSegment.segmentIndex}" : "")}");
    }

    private void MakeCubeTranslucent(CubeManager cube)
    {
        if (cube == null) return;
        cube.ApplyPlayerCubeMaterial();
    }

    public void MovePlayerCubesBackward()
    {
        if (playerCubes == null || playerCubes.Count == 0) return;

        int movedCount = 0;

        for (int i = playerCubes.Count - 1; i >= 0; i--)
        {
            if (i >= playerCubes.Count) continue;

            var cube = playerCubes[i];
            if (cube == null)
            {
                playerCubes.RemoveAt(i);
                continue;
            }

            if (!cube.isMoving)
            {
                cube.ResetMovementState();
                bool stillAlive = cube.MoveBackward();

                if (!stillAlive)
                {
                    playerCubes.RemoveAt(i);
                }
                else
                {
                    movedCount++;
                }
            }
        }

        if (movedCount > 0)
        {
            Debug.Log($"[PlayerMarkerSystem] Moved {movedCount} player cubes backward");
        }
    }

    public void CheckPlayerCubeCollisions()
    {
        collisionManager?.CheckPlayerCubeCollisions(playerCubes);
    }

    public void ClearPlayerCubes()
    {
        if (playerCubes == null) return;

        int clearedCount = playerCubes.Count;

        foreach (var cube in playerCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }

        playerCubes.Clear();

        if (clearedCount > 0)
        {
            Debug.Log($"[PlayerMarkerSystem] Cleared {clearedCount} player cubes");
        }
    }

    #endregion

    #region Auto-Capture Area Markers (Delegated to CubeCollisionManager)

    /// <summary>
    /// Creates an auto-capture area marker from external sources.
    /// Delegated to CubeCollisionManager for processing.
    /// </summary>
    public void CreateAutoCaptureAreaMarker(List<Vector2Int> positions, string markerType, Color markerColor, int expiresAfterMoves = 3, int charges = 2)
    {
        collisionManager?.CreateAutoCaptureAreaMarker(positions, markerType, markerColor, expiresAfterMoves, charges);
    }

    #endregion

    #region Utility Methods

    private bool IsValidPosition(Vector2Int position)
    {
        return actionManager.GridManager.IsValidGridPosition(position);
    }
    
    /// <summary>
    /// Gets the world position for a local position, using segment if provided.
    /// </summary>
    private Vector3 GetWorldPositionOnSegment(Vector2Int position, GridSegmentController segment, float yOffset = 0f)
    {
        if (segment != null)
        {
            return segment.LocalToWorldPosition(position.x, position.y, yOffset);
        }
        return actionManager.GridManager.GridToWorldPosition(position.x, position.y, yOffset);
    }
    
    /// <summary>
    /// Gets the tile at position, using segment if provided, otherwise falling back to grid manager.
    /// </summary>
    private Tile GetTileOnSegment(Vector2Int position, GridSegmentController segment)
    {
        if (segment != null)
        {
            return segment.GetTile(position.x, position.y);
        }
        return actionManager.GridManager.GetTileAt(position.x, position.y);
    }
    
    /// <summary>
    /// Checks if the player is on the same segment as the wave.
    /// Player actions (marker placement, triggering) require segment matching.
    /// </summary>
    /// <returns>True if player is on the same segment as the wave, false otherwise</returns>
    public bool IsPlayerOnWaveSegment()
    {
        // If no segment controllers, always allow (single-segment stage)
        if (waveManager == null || !waveManager.HasSegmentControllers)
            return true;
            
        // Get player's current segment
        var playerSegment = playerManager?.CurrentSegment;
        
        // Get wave's current segment
        var waveSegment = waveManager.CurrentSegmentController;
        
        // If either is null, allow (fallback behavior)
        if (playerSegment == null || waveSegment == null)
            return true;
            
        // Compare segment indices
        return playerSegment.segmentIndex == waveSegment.segmentIndex;
    }
    
    /// <summary>
    /// Gets the reason why player actions are blocked, if any.
    /// </summary>
    /// <returns>Error message if blocked, null if allowed</returns>
    public string GetSegmentMismatchReason()
    {
        if (IsPlayerOnWaveSegment())
            return null;
            
        var playerSegment = playerManager?.CurrentSegment;
        var waveSegment = waveManager?.CurrentSegmentController;
        
        int playerSegIndex = playerSegment?.segmentIndex ?? -1;
        int waveSegIndex = waveSegment?.segmentIndex ?? -1;
        
        return $"Player is on segment {playerSegIndex} but wave is on segment {waveSegIndex}. Move to the wave's segment to place markers.";
    }

    private bool CanPlaceMarkerAt(Vector2Int position)
    {
        // SEGMENT CHECK: Player must be on the same segment as the wave
        if (!IsPlayerOnWaveSegment())
        {
            Debug.Log($"[PlayerMarkerSystem] Cannot place marker - player not on wave's segment. {GetSegmentMismatchReason()}");
            return false;
        }
        
        return !HasUnitMarkerAt(position) &&
               !HasRecursionMarkerAt(position) &&
               !HasMatrixMarkerAt(position) &&
               !HasInfinityMarkerAt(position);
    }

    public List<Vector2Int> GetAreaPositions(Vector2Int center, int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        if (size == 2)
        {
            positions = Get2x2Positions(center);
        }
        else if (size == 3)
        {
            positions = Get3x3Positions(center);
        }

        return positions;
    }

    private List<Vector2Int> Get2x2Positions(Vector2Int center)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                if (IsValidPosition(pos))
                {
                    positions.Add(pos);
                }
            }
        }

        return positions;
    }

    private List<Vector2Int> Get3x3Positions(Vector2Int center)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                if (IsValidPosition(pos))
                {
                    positions.Add(pos);
                }
            }
        }

        return positions;
    }

    private bool IsWithinPerfectTimingWindow(float placementTime)
    {
        return Time.time - placementTime <= perfectTimingWindow;
    }

    #endregion

    #region Clear All Actions

    public void ClearAllActions()
    {
        HideAreaPreview();

        while (UnitMarkers.Count > 0)
        {
            var marker = UnitMarkers.Dequeue();
            visualManager?.DestroyMarkerVisual(marker.visualObject);
        }

        while (RecursionMarkers.Count > 0)
        {
            var marker = RecursionMarkers.Dequeue();
            visualManager?.DestroyMarkerVisual(marker.visualObject);
        }

        while (MatrixMarkers.Count > 0)
        {
            var marker = MatrixMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                visualManager?.DestroyMarkerVisual(visual);
            }
        }

        foreach (var cubeMarker in cubeMarkers)
        {
            if (IsValidPosition(cubeMarker.position))
            {
                Tile tile = actionManager.GridManager.GetTileAt(cubeMarker.position);
                if (tile != null)
                {
                    tile.SetDetonationPoint(false);
                    tile.ForceUpdateVisuals();
                }
            }
            visualManager?.DestroyMarkerVisual(cubeMarker.visualObject);
        }
        cubeMarkers.Clear();

        // Clear swap markers
        foreach (var swapMarker in swapMarkers)
        {
            visualManager?.DestroyMarkerVisual(swapMarker.visualObject);
        }
        swapMarkers.Clear();
        
        // Clear swap preview
        HideSwapPreview();

        foreach (var preview in previewObjects)
        {
            if (preview != null) Destroy(preview);
        }
        previewObjects.Clear();

        // Clear visual manager highlights and countdown texts
        visualManager?.ClearAllTileHighlights();
        visualManager?.ClearAllMarkerCountdownTexts();

        // Clear collision manager active area markers
        collisionManager?.ClearActiveAreaMarkers();

        Debug.Log("All action markers and highlights cleared");
    }

    public void HideAreaPreview()
    {
        foreach (GameObject preview in previewObjects)
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
        previewObjects.Clear();
    }

    #endregion

    #region Statistics and Info

    public int GetCubeMarkersTriggered() => cubeMarkersTriggered;
    public int GetPerfectTimingHits() => perfectTimingHits;
    public int GetCurrentCubeMarkers() => cubeMarkers.Count;
    public Vector2Int GetNextCubeMarker() => cubeMarkers.Count > 0 ? cubeMarkers[0].position : new Vector2Int(-1, -1);

    public void ResetStatistics()
    {
        cubeMarkersTriggered = 0;
        perfectTimingHits = 0;
    }

    #endregion
}
