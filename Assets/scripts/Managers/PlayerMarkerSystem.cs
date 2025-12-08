using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;
using UnityEngine.UIElements;

public class PlayerMarkerSystem : MonoBehaviour
{
    [Header("Marker Settings")]
    [SerializeField] private float perfectTimingWindow = 0.2f;

    [Header("Cube Marker Settings")]
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material poweredCubeMarkerMaterial;

    [Header("Visual Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);

    private Dictionary<Vector2Int, GameObject> temporaryMarkerOverlays = new Dictionary<Vector2Int, GameObject>();
    
    // Countdown text objects for auto-capture markers (recursion-type markers)
    private Dictionary<Vector2Int, TextMesh> markerCountdownTexts = new Dictionary<Vector2Int, TextMesh>();

    // Marker Collections - Five-tier system (Unit, Matrix, Recursion, Infinity, Cube)
    [SerializeField] public Queue<UnitMarker> UnitMarkers = new Queue<UnitMarker>();
    [SerializeField] public Queue<RecursionMarker> RecursionMarkers = new Queue<RecursionMarker>();
    [SerializeField] public Queue<MatrixMarker> MatrixMarkers = new Queue<MatrixMarker>();
    [SerializeField] public Queue<InfinityMarker> InfinityMarkers = new Queue<InfinityMarker>();
    public List<CubeMarker> cubeMarkers = new List<CubeMarker>();

    // Player cube tracking
    public List<CubeManager> playerCubes = new List<CubeManager>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;

    // Parent reference
    private PlayerActionManager actionManager;
    
    // New manager references for refactored code
    [Header("Manager References")]
    [SerializeField] private MarkerVisualManager visualManager;
    // Note: MarkerCollisionManager removed - collision handling is in this class
    
    // Active area markers that auto-capture and expire after N move forwards
    private List<ActiveAreaMarker> activeAreaMarkers = new List<ActiveAreaMarker>();
    
    /// <summary>
    /// Tracks an active area marker that can auto-capture cubes
    /// </summary>
    private class ActiveAreaMarker
    {
        public List<Vector2Int> positions;
        public int createdAtMoveStep;
        public int expiresAfterMoves; // Expires after this many move forwards
        public int remainingCharges; // Number of captures remaining (default 2)
        public int maxCharges; // Original max charges for display
        public Color markerColor;
        public string markerType;
        public bool autoCapture; // If true, automatically captures cubes entering the area
        
        public ActiveAreaMarker(List<Vector2Int> pos, int currentMoveStep, int duration, Color color, string type, bool autoCap = true, int charges = 2)
        {
            positions = new List<Vector2Int>(pos);
            createdAtMoveStep = currentMoveStep;
            expiresAfterMoves = duration;
            remainingCharges = charges;
            maxCharges = charges;
            markerColor = color;
            markerType = type;
            autoCapture = autoCap;
        }
        
        /// <summary>
        /// Checks if marker is expired (either by moves or by charges exhausted)
        /// </summary>
        public bool IsExpired(int currentMoveStep)
        {
            return remainingCharges <= 0 || (currentMoveStep - createdAtMoveStep) >= expiresAfterMoves;
        }
        
        /// <summary>
        /// Uses one charge. Returns true if capture should proceed, false if no charges left.
        /// </summary>
        public bool UseCharge()
        {
            if (remainingCharges <= 0) return false;
            remainingCharges--;
            return true;
        }
        
        /// <summary>
        /// Gets remaining move forwards before marker expires
        /// </summary>
        public int GetRemainingMoves(int currentMoveStep)
        {
            return Mathf.Max(0, expiresAfterMoves - (currentMoveStep - createdAtMoveStep));
        }
        
        /// <summary>
        /// Gets display text showing charges/moves remaining
        /// </summary>
        public string GetDisplayText(int currentMoveStep)
        {
            int movesLeft = GetRemainingMoves(currentMoveStep);
            return $"{remainingCharges}";  // Show charges - can be enhanced to show both
        }
    }
   


    #region Data Structures

    [System.Serializable]
    public class CubeMarker
    {
        public Vector2Int position;
        public CubeMarkerType type;
        public bool isPoweredUp = false;
        public float creationTime;
        public GameObject visualObject;
        public int size = 3; // Area size for cube marker (default 3x3, can be 2x2 for some types)

        public CubeMarker(Vector2Int pos, CubeMarkerType markerType, int markerSize = 3)
        {
            position = pos;
            type = markerType;
            size = markerSize;
            creationTime = Time.time;
        }
    }

    /// <summary>
    /// Cube marker types for markers created from matrix cube captures
    /// </summary>
    public enum CubeMarkerType
    {
        /// <summary>Unit cube marker: Basic targeting (formerly Individual/Light)</summary>
        Unit,
        /// <summary>Recursion cube marker: Enhanced targeting for recursion cubes (formerly Heavy)</summary>
        Recursion,
        /// <summary>Matrix cube marker: Area coverage (formerly Area)</summary>
        Matrix,
        /// <summary>Cube marker: Standard cube marker type</summary>
        Cube,
        

    }

    /// <summary>
    /// Marker types used for processing different marker behaviors
    /// </summary>
    public enum MarkerType
    {
        /// <summary>Unit marker: Basic targeting (formerly Individual/Light)</summary>
        Unit,
        /// <summary>Recursion marker: Enhanced marker for recursion cubes (formerly Heavy)</summary>
        Recursion,
        /// <summary>Matrix marker: Area coverage marker (formerly Area)</summary>
        Matrix,
        /// <summary>Infinity marker: Special marker for infinity cubes</summary>
        Infinity,
        /// <summary>Cube marker: Generated from matrix cube captures</summary>
        CubeMarker,
        

    }

    #endregion

    void Awake()
    {
        actionManager = FindFirstObjectByType<PlayerActionManager>();
    }
    
    void Start()
    {
        // Subscribe to wave step events for area marker processing
        GameEvents.OnWaveStep += OnWaveStep;
        
        // Initialize new managers if they exist
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
        // Note: Collision handling is implemented directly in this class
    }

    private     void OnDestroy()
    {
        // Unsubscribe from events
        GameEvents.OnWaveStep -= OnWaveStep;
        
        // Clean up all temporary overlays
        var overlaysToRemove = temporaryMarkerOverlays.Keys.ToList();
        foreach (var pos in overlaysToRemove)
        {
            ClearTileHighlight(pos);
        }
        
        // Clean up all countdown texts
        ClearAllMarkerCountdownTexts();
        
        // Clear active area markers
        activeAreaMarkers.Clear();
    }
    
    /// <summary>
    /// Called on each wave step to process active area markers
    /// </summary>
    private void OnWaveStep(int waveIndex, int stepNumber)
    {
        ProcessActiveAreaMarkers(stepNumber);
    }
    
    /// <summary>
    /// Process all active area markers - check for auto-captures and expiration
    /// Markers expire when charges exhausted OR move forwards elapsed (whichever first)
    /// </summary>
    private void ProcessActiveAreaMarkers(int currentMoveStep)
    {
        if (activeAreaMarkers.Count == 0) return;
        
        var markersToRemove = new List<ActiveAreaMarker>();
        
        foreach (var marker in activeAreaMarkers)
        {
            // Check for auto-capture at marker positions (if charges remain)
            if (marker.autoCapture && marker.remainingCharges > 0)
            {
                foreach (var pos in marker.positions)
                {
                    var cubesAtPos = FindAllCubesAt(pos);
                    foreach (var cube in cubesAtPos)
                    {
                        if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                        
                        // Try to use a charge and capture
                        if (marker.UseCharge())
                        {
                            if (ProcessCubeCapture(cube, pos, MarkerType.Recursion, null, false))
                            {
                                Debug.Log($"[PlayerMarkerSystem] Auto-captured {cube.type} at ({pos.x}, {pos.y}) by {marker.markerType} marker (charges left: {marker.remainingCharges})");
                            }
                        }
                        
                        // Only capture one cube per position per step
                        break;
                    }
                }
            }
            
            // Check if marker should be removed (charges exhausted OR moves expired)
            if (marker.IsExpired(currentMoveStep))
            {
                string reason = marker.remainingCharges <= 0 ? "charges exhausted" : "moves expired";
                Debug.Log($"[PlayerMarkerSystem] {marker.markerType} marker removed ({reason})");
                
                // Clear the visual markers and countdown text
                foreach (var pos in marker.positions)
                {
                    ClearTileHighlight(pos);
                    ClearMarkerCountdownText(pos);
                }
                markersToRemove.Add(marker);
            }
            else
            {
                // Update countdown display - show remaining charges
                foreach (var pos in marker.positions)
                {
                    UpdateMarkerCountdownText(pos, marker.remainingCharges);
                }
            }
        }
        
        // Remove expired markers
        foreach (var marker in markersToRemove)
        {
            activeAreaMarkers.Remove(marker);
        }
    }

    #region Unit Markers

    public bool PlaceUnitMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceUnitMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new UnitMarker(position, Time.time);
        marker.visualObject = CreateUnitMarkerVisual(position);

        UnitMarkers.Enqueue(marker);
        actionManager.ConsumeUnitCharge();

        // Record marker position for paired wave system
        RecordMarkerForPairedWave(position, MarkerMode.Unit);

        Debug.Log($"Unit marker placed at ({position.x}, {position.y})");
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
                DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseUnitMarker();
                actionManager.OnUnitMarkerRemoved(); // Decrement placement counter
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
        var cubes = FindAllCubesAt(position);
        bool success = false;

        // Trigger audio event for marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
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

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "unit", success, cubes.Count);
        }

        DestroyMarkerVisual(marker.visualObject);
        StartCoroutine(ShowMarkerTriggerEffect(position));

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

        var marker = new RecursionMarker(position, Time.time);
        marker.visualObject = CreateRecursionMarkerVisual(position);

        RecursionMarkers.Enqueue(marker);
        actionManager.ConsumeRecursionCharge();

        // Record marker position for paired wave system
        RecordMarkerForPairedWave(position, MarkerMode.Recursion);

        Debug.Log($"Recursion marker placed at ({position.x}, {position.y})");
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
                DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseRecursionMarker();
                actionManager.OnRecursionMarkerRemoved(); // Decrement placement counter
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
        var cubes = FindAllCubesAt(position);
        bool success = false;

        // Trigger audio event for marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            // Recursion markers are specifically designed for recursion cubes but work on all cube types
            success |= ProcessCubeCapture(cube, position, MarkerType.Recursion, marker);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "recursion", success, cubes.Count);
        }

        DestroyMarkerVisual(marker.visualObject);
        StartCoroutine(ShowMarkerTriggerEffect(position));

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

        MatrixMarker newMarker = new MatrixMarker(centerPosition, size, Time.time);
        newMarker.affectedPositions = GetAreaPositions(centerPosition, size);
        GameObject visual = CreateMatrixMarkerVisual(centerPosition);
        newMarker.visualObjects.Add(visual);

        MatrixMarkers.Enqueue(newMarker);
        actionManager.ConsumeMatrixCharge();

        // Record marker position for paired wave system (record center position)
        RecordMarkerForPairedWave(centerPosition, MarkerMode.Matrix);

        Debug.Log($"Matrix marker placed at ({centerPosition.x}, {centerPosition.y})");
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
                    DestroyMarkerVisual(visual);
                }
                actionManager.ReleaseMatrixMarker();
                actionManager.OnMatrixMarkerRemoved(); // Decrement placement counter
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

        // Trigger audio event for marker triggering
        Vector3 centerWorldPosition = actionManager.GridManager.GridToWorldPosition(marker.centerPosition.x, marker.centerPosition.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, centerWorldPosition);

        foreach (var visual in marker.visualObjects)
        {
            DestroyMarkerVisual(visual);
        }

        foreach (var position in marker.affectedPositions)
        {
            Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
            if (tile != null && position != marker.centerPosition)
            {
                SetTileHighlight(tile, new Color(0f, 1f, 0f, 0.7f), "AreaExpansion");
            }

            var cubes = FindAllCubesAt(position);
            totalCubesAffected += cubes.Count;
            foreach (var cube in cubes)
            {
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Matrix);
            }
            StartCoroutine(ShowMarkerTriggerEffect(position));
        }

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(marker.centerPosition, "matrix", anySuccess, totalCubesAffected);
        }

        StartCoroutine(ClearAreaExpansionAfterDelay(marker.affectedPositions, marker.centerPosition, 1f));
        return anySuccess;
    }



    private IEnumerator ClearAreaExpansionAfterDelay(List<Vector2Int> positions, Vector2Int centerPos, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var pos in positions)
        {
            ClearTileHighlight(pos);
        }
    }

    #endregion

    #region Infinity Markers

    public bool PlaceInfinityMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceInfinityMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new InfinityMarker(position, Time.time);
        marker.visualObject = CreateInfinityMarkerVisual(position);

        InfinityMarkers.Enqueue(marker);
        actionManager.ConsumeInfinityCharge();

        // Record marker position for paired wave system
        RecordMarkerForPairedWave(position, MarkerMode.Infinity);

        Debug.Log($"Infinity marker placed at ({position.x}, {position.y})");
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
                DestroyMarkerVisual(marker.visualObject);
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
        var cubes = FindAllCubesAt(position);
        bool success = false;

        // Trigger audio event for marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
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

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "infinity", success, cubes.Count);
        }

        DestroyMarkerVisual(marker.visualObject);
        StartCoroutine(ShowMarkerTriggerEffect(position));

        Debug.Log($"Infinity marker triggered at ({position.x}, {position.y}) - Perfect: {marker.isPerfectTiming}");
        return success;
    }

    private GameObject CreateInfinityMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Infinity = Deep black (dark charcoal for visibility)
            SetTileHighlight(tile, new Color(0.15f, 0.15f, 0.18f, 1f), "Infinity");
        }

        GameObject dummy = new GameObject($"InfinityMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    #endregion

    #region Cube Markers

    public void CreateCubeMarker(Vector2Int position, CubeMarkerType type = CubeMarkerType.Matrix, int size = 3)
    {
        var cubeMarker = new CubeMarker(position, type, size);
        cubeMarker.visualObject = CreateCubeMarkerVisual(position, type);

        cubeMarkers.Add(cubeMarker);

        Debug.Log($"Cube marker ({type}, size {size}x{size}) created at ({position.x}, {position.y})");
    }

    public bool TriggerNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        cubeMarkers.RemoveAt(0);

        return TriggerCubeMarkerAt(cubeMarker);
    }

    public bool TriggerCubeMarkerAt(CubeMarker cubeMarker)
    {
        cubeMarkersTriggered++;

        // Trigger audio event for cube marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(cubeMarker.position.x, cubeMarker.position.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition, 1.2f);
        
        DestroyMarkerVisual(cubeMarker.visualObject);

        // Use cube marker's size instead of hardcoded 3
        var tempMatrixMarker = new MatrixMarker(cubeMarker.position, cubeMarker.size, Time.time);
        tempMatrixMarker.affectedPositions = GetAreaPositions(cubeMarker.position, cubeMarker.size);
        return TriggerMatrixMarkerAt(tempMatrixMarker);
        
    }

    private bool TriggerSingleTileMarker(Vector2Int position)
    {
        var cubes = FindAllCubesAt(position);
        bool success = false;

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Unit);
        }

        StartCoroutine(ShowMarkerTriggerEffect(position));
        return success;
    }

    public bool PowerUpNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        if (cubeMarker.isPoweredUp) return false;

        cubeMarker.isPoweredUp = true;

        DestroyMarkerVisual(cubeMarker.visualObject);
        cubeMarker.visualObject = CreatePoweredCubeMarkerVisual(cubeMarker.position, cubeMarker.type);

        Debug.Log($"Cube marker powered up at ({cubeMarker.position.x}, {cubeMarker.position.y})");
        return true;
    }

    #endregion

    #region Audio Event Integration

    /// <summary>
    /// Triggers an audio event through the PlayerActionManager if available
    /// </summary>
    /// <param name="eventType">The type of audio event to trigger</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    private void TriggerMarkerAudioEvent(GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        if (actionManager != null)
        {
            // Use reflection to access the private TriggerAudioEvent method in PlayerActionManager
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
        else
        {
            Debug.LogWarning("[PlayerMarkerSystem] ActionManager reference is null, cannot trigger audio event");
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

        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) with {markerType} marker{(isSameTypeMatch ? " (same-type match!)" : "")}");

        // Generate cube markers based on collision type
        if (isSameTypeMatch)
        {
            // Same-type collision: generate enhanced cube marker
            switch (cube.type)
            {
                case CubeType.Matrix:
                    // Matrix+Matrix: 3x3 cube marker (enhanced reward)
                    CreateCubeMarker(position, CubeMarkerType.Matrix, 3);
                    break;
                case CubeType.Recursion:
                    // Recursion+Recursion: 2x2 cube marker (reward for matching)
                    CreateCubeMarker(position, CubeMarkerType.Recursion, 2);
                    break;
                case CubeType.Unit:
                    // Unit+Unit: No cube marker (too common)
                    break;
                case CubeType.Infinity:
                    // Infinity+Infinity: Defer to Task 2 design
                    // For now, no cube marker
                    break;
            }
        }
        else if (cube.type == CubeType.Matrix)
        {
            // Matrix cube captured by non-Matrix: standard 2x2 cube marker
            CreateCubeMarker(position, CubeMarkerType.Matrix, 2);
        }

        // Notify statistics manager about cube capture
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnCubeCaptured(position, cube.type.ToString(), markerType.ToString());
        }

        RemoveCubeFromWaveManager(cube);
        
        
        NotifyWaveManager(wm => wm.OnCubeCaptured(cube.type));
        Destroy(cube.gameObject);
        return true;
    }

    public List<CubeManager> FindAllCubesAt(Vector2Int position)
    {
        var cubes = new List<CubeManager>();
        
        // Use cached WaveManager reference instead of FindObjectsOfType
        var activeCubes = actionManager?.WaveManager?.activeCubes;
        if (activeCubes == null) return cubes;

        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed && 
                cube.position.x == position.x && cube.position.y == position.y &&
                !cubes.Contains(cube))
            {
                cubes.Add(cube);
            }
        }

        return cubes;
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

    /// <summary>
    /// Spawns player cubes from all marker types.
    /// Called during wave move forward to spawn cubes that move opposite to wave.
    /// - Unit markers → Unit cubes (single capture)
    /// - Matrix markers → Matrix cubes (area capture - 3x3)
    /// - Recursion markers → Recursion cubes (single capture)
    /// - Infinity markers → Infinity cubes (single capture)
    /// </summary>
    public void SpawnPlayerCubes()
    {
        if (actionManager == null || actionManager.WaveManager == null || actionManager.GridManager == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Cannot spawn player cubes - missing references");
            return;
        }

        var waveManager = actionManager.WaveManager;
        var grid = actionManager.GridManager;

        if (waveManager.cubePrefabs == null)
        {
            Debug.LogWarning("[PlayerMarkerSystem] Cannot spawn player cubes - cube prefabs not available");
            return;
        }

        int spawnedCount = 0;

        // Process Unit markers → Unit cubes
        var UnitMarkersArray = UnitMarkers.ToArray();
        foreach (var marker in UnitMarkersArray)
        {
            if (marker != null)
            {
                DestroyMarkerVisual(marker.visualObject);
                SpawnPlayerCubeAt(marker.position, CubeType.Unit, false);
                spawnedCount++;
            }
        }
        UnitMarkers.Clear();

        // Process Matrix markers → Matrix cubes (area capture)
        var matrixMarkersArray = MatrixMarkers.ToArray();
        foreach (var marker in matrixMarkersArray)
        {
            if (marker != null)
            {
                foreach (var visual in marker.visualObjects)
                {
                    DestroyMarkerVisual(visual);
                }
                SpawnPlayerCubeAt(marker.centerPosition, CubeType.Matrix, true); // isMatrix = true for area capture
                spawnedCount++;
            }
        }
        MatrixMarkers.Clear();

        // Process Recursion markers → Recursion cubes
        var RecursionMarkersArray = RecursionMarkers.ToArray();
        foreach (var marker in RecursionMarkersArray)
        {
            if (marker != null)
            {
                DestroyMarkerVisual(marker.visualObject);
                SpawnPlayerCubeAt(marker.position, CubeType.Recursion, false);
                spawnedCount++;
            }
        }
        RecursionMarkers.Clear();

        // Process Infinity markers → Infinity cubes
        var infinityMarkersArray = InfinityMarkers.ToArray();
        foreach (var marker in infinityMarkersArray)
        {
            if (marker != null)
            {
                DestroyMarkerVisual(marker.visualObject);
                SpawnPlayerCubeAt(marker.position, CubeType.Infinity, false);
                spawnedCount++;
            }
        }
        InfinityMarkers.Clear();

        if (spawnedCount > 0)
        {
            Debug.Log($"[PlayerMarkerSystem] Spawned {spawnedCount} player cubes from markers");
        }
    }

    /// <summary>
    /// Spawns a single player cube at the specified position with the corresponding cube type.
    /// </summary>
    private void SpawnPlayerCubeAt(Vector2Int position, CubeType cubeType, bool isMatrixCube)
    {
        var waveManager = actionManager.WaveManager;
        var grid = actionManager.GridManager;

        // Use the correct prefab for the cube type
        int prefabIndex = (int)cubeType;
        if (prefabIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[prefabIndex] == null)
        {
            // Fallback to Unit prefab if specific type not available
            prefabIndex = (int)CubeType.Unit;
            if (prefabIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[prefabIndex] == null)
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

        Vector3 spawnPos = grid.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Instantiate(waveManager.cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null)
        {
            cube = cubeObj.AddComponent<CubeManager>();
        }

        cube.Init(grid, cubeData, 2f);
        cube.isPlayerCube = true;
        cube.isMatrixCube = isMatrixCube; // For area capture logic
        cube.usePhysics = false;
        cube.ConfigurePlayerCubePhysics();

        MakeCubeTranslucent(cube);
        playerCubes.Add(cube);

        Debug.Log($"[PlayerMarkerSystem] Spawned {cubeType} player cube at ({position.x}, {position.y}){(isMatrixCube ? " (area capture)" : "")}");
    }

    /// <summary>
    /// Makes a cube translucent by modifying its material alpha.
    /// </summary>
    private void MakeCubeTranslucent(CubeManager cube)
    {
        if (cube == null) return;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer == null) return;

        Material originalMaterial = renderer.material;
        if (originalMaterial == null) return;

        // Create a new material instance to avoid affecting other cubes
        Material translucentMaterial = new Material(originalMaterial);
        
        // Set alpha to make it translucent (0.35 = 35% opacity for better visibility)
        Color color = translucentMaterial.color;
        color.a = 0.35f;
        translucentMaterial.color = color;

        // Enable transparency rendering mode
        if (translucentMaterial.HasProperty("_Mode"))
        {
            translucentMaterial.SetFloat("_Mode", 3); // Transparent mode
            translucentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            translucentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            translucentMaterial.SetInt("_ZWrite", 0);
            translucentMaterial.DisableKeyword("_ALPHATEST_ON");
            translucentMaterial.EnableKeyword("_ALPHABLEND_ON");
            translucentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            translucentMaterial.renderQueue = 3000;
        }

        renderer.material = translucentMaterial;
    }

    /// <summary>
    /// Moves all player cubes backward (increasing Y position) toward wave cubes.
    /// Removes destroyed cubes from the playerCubes list.
    /// </summary>
    public void MovePlayerCubesBackward()
    {
        if (playerCubes == null || playerCubes.Count == 0) return;

        int movedCount = 0;

        // Iterate in reverse to safely remove items during iteration
        for (int i = playerCubes.Count - 1; i >= 0; i--)
        {
            if (i >= playerCubes.Count) continue;

            var cube = playerCubes[i];
            if (cube == null)
            {
                playerCubes.RemoveAt(i);
                continue;
            }

            // Only move cubes that aren't currently animating (atomic movement)
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

    /// <summary>
    /// Checks for collisions between player cubes and wave cubes.
    /// Handles both same-tile collisions and adjacent cubes moving toward each other.
    /// </summary>
    public void CheckPlayerCubeCollisions()
    {
        if (playerCubes == null || playerCubes.Count == 0) return;
        if (actionManager?.GridManager == null) return;

        int collisionCount = 0;
        var gridManager = actionManager.GridManager;

        // Iterate through all player cubes
        for (int i = playerCubes.Count - 1; i >= 0; i--)
        {
            if (i >= playerCubes.Count) continue;

            var playerCube = playerCubes[i];
            if (playerCube == null || playerCube.isDestroyed)
            {
                playerCubes.RemoveAt(i);
                continue;
            }

            Vector2Int playerPos = playerCube.position;
            
            // Validate position bounds
            if (!IsValidPosition(playerPos, gridManager))
            {
                continue;
            }
            
            // Check collision at current position (normal case)
            if (ProcessCollisionAtPosition(playerCube, playerPos, ref collisionCount, ref i))
            {
                continue; // Collision handled, move to next player cube
            }
            
            // Check collision at previous position (adjacent cubes passing through)
            // Example: Wave at (x, y+1) → (x, y), Player at (x, y) → (x, y+1)
            // They collide at (x, y) - player's previous position
            Vector2Int playerPreviousPos = new Vector2Int(playerPos.x, playerPos.y - 1);
            if (IsValidPosition(playerPreviousPos, gridManager))
            {
                ProcessPassThroughCollision(playerCube, playerPos, playerPreviousPos, ref collisionCount, ref i);
            }
        }

        if (collisionCount > 0)
        {
            this.Log($"CheckPlayerCubeCollisions: Processed {collisionCount} collisions", true);
        }
    }
    
    /// <summary>
    /// Checks for collision at a specific position and processes it if found.
    /// Returns true if collision was found and processed.
    /// Uses comprehensive collision matrix to handle all 16 collision combinations.
    /// </summary>
    private bool ProcessCollisionAtPosition(CubeManager playerCube, Vector2Int position, ref int collisionCount, ref int playerCubeIndex)
    {
        var cubesAtPosition = FindAllCubesAt(position);
        
        foreach (var waveCube in cubesAtPosition)
        {
            if (waveCube == null || waveCube.isDestroyed || waveCube.isPlayerCube) continue;
            
            // Task 7: Check if wave cube is phaseable - if so, player cube passes through
            if (waveCube.type == CubeType.Infinity && waveCube.IsPhaseable())
            {
                Debug.Log($"[Task 7] Player cube passing through phaseable Infinity cube at ({position.x}, {position.y})");
                continue; // Skip collision, allow passing through
            }
            
            // Route to appropriate collision handler based on collision matrix
            CollisionResult result = HandleCollision(playerCube, waveCube, position);
            
            if (result.handled)
            {
                // Only destroy player cube if it should be destroyed (not for Infinity continue-up cases)
                if (result.destroyPlayerCube)
                {
                    HandlePlayerCubeDestruction(playerCube, ref collisionCount, ref playerCubeIndex);
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Result of collision handling
    /// </summary>
    private struct CollisionResult
    {
        public bool handled;
        public bool destroyPlayerCube;
        
        public CollisionResult(bool handled, bool destroyPlayerCube = true)
        {
            this.handled = handled;
            this.destroyPlayerCube = destroyPlayerCube;
        }
    }
    
    /// <summary>
    /// Central collision matrix handler. Routes all 16 collision combinations to their specific behaviors.
    /// </summary>
    private CollisionResult HandleCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int collisionPosition)
    {
        CubeType playerType = playerCube.type;
        CubeType waveType = waveCube.type;
        
        // Route to specific collision handler based on collision matrix
        switch (playerType)
        {
            case CubeType.Unit:
                return HandleUnitCollision(playerCube, waveCube, collisionPosition);
            
            case CubeType.Matrix:
                return HandleMatrixCollision(playerCube, waveCube, collisionPosition);
            
            case CubeType.Recursion:
                return HandleRecursionCollision(playerCube, waveCube, collisionPosition);
            
            case CubeType.Infinity:
                return HandleInfinityCollision(playerCube, waveCube, collisionPosition);
            
            default:
                Debug.LogWarning($"[PlayerMarkerSystem] Unknown player cube type: {playerType}");
                return new CollisionResult(false);
        }
    }
    
    /// <summary>
    /// Handles collision detection for adjacent cubes moving toward each other.
    /// Verifies the wave cube came from where the player cube is now.
    /// Uses comprehensive collision matrix.
    /// </summary>
    private void ProcessPassThroughCollision(CubeManager playerCube, Vector2Int playerPos, Vector2Int playerPreviousPos, 
        ref int collisionCount, ref int playerCubeIndex)
    {
        var cubesAtPreviousPos = FindAllCubesAt(playerPreviousPos);
        
        foreach (var waveCube in cubesAtPreviousPos)
        {
            if (waveCube == null || waveCube.isDestroyed || waveCube.isPlayerCube) continue;
            
            // Task 7: Check if wave cube is phaseable - if so, player cube passes through
            if (waveCube.type == CubeType.Infinity && waveCube.IsPhaseable())
            {
                Debug.Log($"[Task 7] Player cube passing through phaseable Infinity cube at ({playerPreviousPos.x}, {playerPreviousPos.y})");
                continue; // Skip collision, allow passing through
            }
            
            // Verify wave cube came from player's current position (confirms they passed through)
            Vector2Int waveCubeSourcePos = new Vector2Int(waveCube.position.x, waveCube.position.y + 1);
            if (waveCubeSourcePos == playerPos)
            {
                CollisionResult result = HandleCollision(playerCube, waveCube, playerPreviousPos);
                
                if (result.handled)
                {
                    // Only destroy player cube if it should be destroyed
                    if (result.destroyPlayerCube)
                    {
                        HandlePlayerCubeDestruction(playerCube, ref collisionCount, ref playerCubeIndex);
                    }
                    return;
                }
            }
        }
    }
    
    #region Collision Matrix Handlers
    
    /// <summary>
    /// Handles Unit cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleUnitCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Unit + Unit: Standard capture
                return new CollisionResult(ProcessCubeCapture(waveCube, position, MarkerType.Unit, null, false));
            
            case CubeType.Matrix:
                // Unit + Matrix: 2x2 area capture centered on collision point
                return new CollisionResult(HandleUnitMatrixCollision(position));
            
            case CubeType.Recursion:
                // Unit + Recursion: Column capture (auto-captures 3 cubes)
                return new CollisionResult(HandleColumnCapture(position, 3));
            
            case CubeType.Infinity:
                // Unit + Infinity: Face paint, Unit destroyed
                return HandleInfinityFacePaint(waveCube, playerCube, CubeType.Unit, position);
            
            default:
                return new CollisionResult(false);
        }
    }
    
    /// <summary>
    /// Handles Matrix cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleMatrixCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Matrix + Unit: 2x2 area capture from Matrix position
                return new CollisionResult(HandleMatrixAreaCapture(position, 2));
            
            case CubeType.Matrix:
                // Matrix + Matrix: 3x3 triggerable marker (enhanced reward)
                return new CollisionResult(HandleMatrixMatrixCollision(position));
            
            case CubeType.Recursion:
                // Matrix + Recursion: Degrading 2x2 marker
                return new CollisionResult(HandleMatrixRecursionCollision(position));
            
            case CubeType.Infinity:
                // Matrix + Infinity: Face paint, Matrix destroyed
                return HandleInfinityFacePaint(waveCube, playerCube, CubeType.Matrix, position);
            
            default:
                return new CollisionResult(false);
        }
    }
    
    /// <summary>
    /// Handles Recursion cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleRecursionCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Recursion + Unit: Column capture (auto-captures 3 cubes)
                return new CollisionResult(HandleColumnCapture(position, 3));
            
            case CubeType.Matrix:
                // Recursion + Matrix: Auto 1x3 vertical marker
                return new CollisionResult(HandleRecursionMatrixCollision(position));
            
            case CubeType.Recursion:
                // Recursion + Recursion: Cross marker (5 tiles)
                return new CollisionResult(HandleRecursionRecursionCollision(position));
            
            case CubeType.Infinity:
                // Recursion + Infinity: Face paint, Recursion destroyed
                return HandleInfinityFacePaint(waveCube, playerCube, CubeType.Recursion, position);
            
            default:
                return new CollisionResult(false);
        }
    }
    
    /// <summary>
    /// Handles Infinity cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleInfinityCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Infinity + Unit: Wave join (removes Unit, takes position, moves with wave)
                return HandleInfinityWaveJoin(playerCube, waveCube, position);
            
            case CubeType.Matrix:
                // Infinity + Matrix: Face paint, continue up
                return HandleInfinityFacePaint(waveCube, playerCube, CubeType.Matrix, position, false);
            
            case CubeType.Recursion:
                // Infinity + Recursion: Face paint, continue up
                return HandleInfinityFacePaint(waveCube, playerCube, CubeType.Recursion, position, false);
            
            case CubeType.Infinity:
                // Infinity + Infinity: Face paint, resonance
                return HandleInfinityInfinityCollision(waveCube, playerCube, position);
            
            default:
                return new CollisionResult(false);
        }
    }
    
    #endregion
    
    #region Specific Collision Behaviors
    
    /// <summary>
    /// Unit + Matrix: 2x2 area capture centered on collision point
    /// </summary>
    private bool HandleUnitMatrixCollision(Vector2Int centerPosition)
    {
        var areaPositions = GetAreaPositions(centerPosition, 2);
        bool anyCaptured = false;
        
        foreach (var areaPos in areaPositions)
        {
            var cubesAtArea = FindAllCubesAt(areaPos);
            foreach (var cube in cubesAtArea)
            {
                if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                if (ProcessCubeCapture(cube, areaPos, MarkerType.Matrix, null, false))
                {
                    anyCaptured = true;
                }
            }
        }
        
        return anyCaptured;
    }
    
    /// <summary>
    /// Matrix + Unit: Creates 2x2 manual marker (player triggers with R)
    /// Captures the Unit cube immediately, then creates a 2x2 cube marker for manual triggering
    /// </summary>
    private bool HandleMatrixAreaCapture(Vector2Int centerPosition, int areaSize)
    {
        // Capture the Unit cube at collision point first
        var cubesAtPosition = FindAllCubesAt(centerPosition);
        bool capturedUnit = false;
        
        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Unit)
            {
                if (ProcessCubeCapture(cube, centerPosition, MarkerType.Matrix, null, false))
                {
                    capturedUnit = true;
                    break;
                }
            }
        }
        
        // Create a 2x2 cube marker for manual triggering
        // All Matrix player cube collisions create triggerable markers
        CreateCubeMarker(centerPosition, CubeMarkerType.Matrix, areaSize);
        Debug.Log($"[PlayerMarkerSystem] Matrix+Unit collision - created {areaSize}x{areaSize} manual cube marker at ({centerPosition.x}, {centerPosition.y})");
        
        return capturedUnit || true; // Always return true since we created a marker
    }
    
    /// <summary>
    /// Matrix + Matrix: 3x3 triggerable marker (enhanced reward)
    /// Note: Cube marker creation is handled by ProcessCubeCapture when isSameTypeMatch=true
    /// </summary>
    private bool HandleMatrixMatrixCollision(Vector2Int centerPosition)
    {
        // Capture all cubes in 3x3 area
        // ProcessCubeCapture will create the 3x3 cube marker for same-type matches
        var areaPositions = GetAreaPositions(centerPosition, 3);
        bool anyCaptured = false;
        
        foreach (var areaPos in areaPositions)
        {
            var cubesAtArea = FindAllCubesAt(areaPos);
            foreach (var cube in cubesAtArea)
            {
                if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                // Pass isSameTypeMatch=true to trigger cube marker creation in ProcessCubeCapture
                if (ProcessCubeCapture(cube, areaPos, MarkerType.Matrix, null, true))
                {
                    anyCaptured = true;
                }
            }
        }
        
        return anyCaptured;
    }
    
    /// <summary>
    /// Matrix + Recursion: Creates 2x2 degrading manual marker (player triggers with R)
    /// Captures the Recursion cube immediately, then creates a 2x2 cube marker for manual triggering
    /// </summary>
    private bool HandleMatrixRecursionCollision(Vector2Int centerPosition)
    {
        // Capture the Recursion cube at collision point first
        var cubesAtPosition = FindAllCubesAt(centerPosition);
        bool capturedRecursion = false;
        
        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Recursion)
            {
                if (ProcessCubeCapture(cube, centerPosition, MarkerType.Matrix, null, false))
                {
                    capturedRecursion = true;
                    break;
                }
            }
        }
        
        // Create a 2x2 cube marker for manual triggering (regardless of capture success)
        // This follows the design: Matrix+Recursion = 2x2 degrading manual marker
        CreateCubeMarker(centerPosition, CubeMarkerType.Matrix, 2);
        Debug.Log($"[PlayerMarkerSystem] Matrix+Recursion collision - created 2x2 manual cube marker at ({centerPosition.x}, {centerPosition.y})");
        
        return capturedRecursion || true; // Always return true since we created a marker
    }
    
    /// <summary>
    /// Column capture: Creates auto-capture marker with charges
    /// Used for Unit+Recursion and Recursion+Unit
    /// Captures immediately if cube present (uses 1 charge), then auto-captures on wave movement
    /// Expires when charges exhausted OR move forwards elapsed (whichever first)
    /// </summary>
    private bool HandleColumnCapture(Vector2Int position, int charges = 2)
    {
        int expiresAfterMoves = 3;
        
        // Create visual column marker (1x3 vertical) with charge tracking
        CreateColumnMarker(position, 3, expiresAfterMoves, charges);
        
        // Try to capture cube at collision point immediately (uses 1 charge)
        var cubesAtPosition = FindAllCubesAt(position);
        bool capturedImmediately = false;
        
        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            
            if (ProcessCubeCapture(cube, position, MarkerType.Recursion, null, false))
            {
                // Decrement charge on the marker we just created
                var marker = activeAreaMarkers[activeAreaMarkers.Count - 1];
                marker.UseCharge();
                capturedImmediately = true;
                Debug.Log($"[PlayerMarkerSystem] Recursion column - immediate capture at ({position.x}, {position.y}), {marker.remainingCharges} charges left");
                break;
            }
        }
        
        if (!capturedImmediately)
        {
            Debug.Log($"[PlayerMarkerSystem] Recursion column - no cube at collision, marker will auto-capture on wave movement ({charges} charges, {expiresAfterMoves} moves)");
        }
        
        return true;
    }
    
    /// <summary>
    /// Creates a visual column marker (1x3 vertical) at the collision position.
    /// Marker expires when charges exhausted OR moves elapsed (whichever first)
    /// </summary>
    private void CreateColumnMarker(Vector2Int centerPosition, int height, int expiresAfterMoves = 3, int charges = 2)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Color markerColor = new Color(0.9f, 0.6f, 0.2f, 0.8f); // Amber/orange
        
        // Create visual markers for the column (tiles going down from collision)
        for (int y = 0; y < height; y++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x, centerPosition.y - y);
            if (IsValidPosition(pos))
            {
                positions.Add(pos);
                Tile tile = actionManager.GridManager.GetTileAt(pos.x, pos.y);
                if (tile != null)
                {
                    SetTileHighlight(tile, markerColor, "ColumnCapture");
                    // Show charges remaining
                    CreateMarkerCountdownText(pos, charges, Color.white);
                }
            }
        }
        
        // Get current move step from WaveManager
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        
        // Register as active area marker with charge tracking
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "ColumnCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);
        
        Debug.Log($"[PlayerMarkerSystem] Created column marker at ({centerPosition.x}, {centerPosition.y}) - {charges} charges, expires in {expiresAfterMoves} moves");
    }
    
    /// <summary>
    /// Recursion + Matrix: Auto 1x3 vertical marker with charges
    /// Captures immediately if cube present (uses 1 charge), then auto-captures on wave movement
    /// </summary>
    private bool HandleRecursionMatrixCollision(Vector2Int centerPosition)
    {
        int charges = 2;
        int expiresAfterMoves = 3;
        
        // Create visual 1x3 vertical marker (3 tiles going up from collision point)
        CreateVerticalMarker(centerPosition, 3, true, expiresAfterMoves, charges);
        
        // Try to capture cube at collision point immediately (uses 1 charge)
        var cubesAtPosition = FindAllCubesAt(centerPosition);
        bool capturedImmediately = false;
        
        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (ProcessCubeCapture(cube, centerPosition, MarkerType.Recursion, null, false))
            {
                // Decrement charge on the marker we just created
                var marker = activeAreaMarkers[activeAreaMarkers.Count - 1];
                marker.UseCharge();
                capturedImmediately = true;
                Debug.Log($"[PlayerMarkerSystem] Recursion+Matrix - immediate capture, {marker.remainingCharges} charges left");
                break;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Creates a visual vertical marker (1x3) at the collision position.
    /// Marker expires when charges exhausted OR moves elapsed (whichever first)
    /// </summary>
    private void CreateVerticalMarker(Vector2Int centerPosition, int height, bool goingUp, int expiresAfterMoves = 3, int charges = 2)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Color markerColor = new Color(0.3f, 0.8f, 0.9f, 0.8f); // Cyan/blue
        
        for (int y = 0; y < height; y++)
        {
            int yOffset = goingUp ? y : -y;
            Vector2Int pos = new Vector2Int(centerPosition.x, centerPosition.y + yOffset);
            if (IsValidPosition(pos))
            {
                positions.Add(pos);
                Tile tile = actionManager.GridManager.GetTileAt(pos.x, pos.y);
                if (tile != null)
                {
                    SetTileHighlight(tile, markerColor, "VerticalCapture");
                    // Show charges remaining
                    CreateMarkerCountdownText(pos, charges, Color.white);
                }
            }
        }
        
        // Get current move step from WaveManager
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        
        // Register as active area marker with charge tracking
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "VerticalCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);
        
        Debug.Log($"[PlayerMarkerSystem] Created vertical marker at ({centerPosition.x}, {centerPosition.y}) - {charges} charges, expires in {expiresAfterMoves} moves");
    }
    
    /// <summary>
    /// Recursion + Recursion: Cross marker (5 tiles) with charges
    /// Captures immediately if cube present (uses 1 charge), then auto-captures on wave movement
    /// </summary>
    private bool HandleRecursionRecursionCollision(Vector2Int centerPosition)
    {
        int charges = 2;
        int expiresAfterMoves = 3;
        
        // Create cross marker (5 tiles total)
        List<Vector2Int> crossPositions = new List<Vector2Int>();
        
        // Vertical line (3 tiles)
        for (int y = -1; y <= 1; y++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x, centerPosition.y + y);
            if (IsValidPosition(pos) && !crossPositions.Contains(pos))
            {
                crossPositions.Add(pos);
            }
        }
        
        // Horizontal line (3 tiles, center overlaps)
        for (int x = -1; x <= 1; x++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x + x, centerPosition.y);
            if (IsValidPosition(pos) && !crossPositions.Contains(pos))
            {
                crossPositions.Add(pos);
            }
        }
        
        // Create visual cross marker with charge tracking
        CreateCrossMarker(crossPositions, expiresAfterMoves, charges);
        
        // Try to capture cube at collision point immediately (uses 1 charge)
        var cubesAtCenter = FindAllCubesAt(centerPosition);
        bool capturedImmediately = false;
        
        foreach (var cube in cubesAtCenter)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            // Pass isSameTypeMatch=true to trigger cube marker creation in ProcessCubeCapture
            if (ProcessCubeCapture(cube, centerPosition, MarkerType.Recursion, null, true))
            {
                // Decrement charge on the marker we just created
                var marker = activeAreaMarkers[activeAreaMarkers.Count - 1];
                marker.UseCharge();
                capturedImmediately = true;
                Debug.Log($"[PlayerMarkerSystem] Recursion+Recursion - immediate capture, {marker.remainingCharges} charges left");
                break;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Creates a visual cross marker (5 tiles) at the collision positions.
    /// Marker expires when charges exhausted OR moves elapsed (whichever first)
    /// </summary>
    private void CreateCrossMarker(List<Vector2Int> positions, int expiresAfterMoves = 3, int charges = 2)
    {
        Color markerColor = new Color(0.7f, 0.3f, 0.8f, 0.8f); // Purple
        
        foreach (var pos in positions)
        {
            Tile tile = actionManager.GridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                SetTileHighlight(tile, markerColor, "CrossCapture");
                // Show charges remaining
                CreateMarkerCountdownText(pos, charges, Color.white);
            }
        }
        
        // Get current move step from WaveManager
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        
        // Register as active area marker with charge tracking
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "CrossCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);
        
        Debug.Log($"[PlayerMarkerSystem] Created cross marker with {positions.Count} tiles - expires in {expiresAfterMoves} moves or on capture");
    }
    
    /// <summary>
    /// Creates an auto-capture area marker from external sources (e.g., painted face grid touch).
    /// This creates visual markers that auto-capture cubes passing through without consuming player charges.
    /// Used by Tile.cs for RecursionFace and other painted face effects.
    /// </summary>
    /// <param name="positions">List of grid positions for the marker</param>
    /// <param name="markerType">Name/type of the marker for logging</param>
    /// <param name="markerColor">Visual color for the marker highlights</param>
    /// <param name="expiresAfterMoves">Number of wave moves before marker expires</param>
    /// <param name="charges">Number of auto-capture charges</param>
    public void CreateAutoCaptureAreaMarker(List<Vector2Int> positions, string markerType, Color markerColor, int expiresAfterMoves = 3, int charges = 2)
    {
        if (positions == null || positions.Count == 0) return;
        if (actionManager?.GridManager == null) return;
        
        // Create visual highlights for each position
        foreach (var pos in positions)
        {
            Tile tile = actionManager.GridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                SetTileHighlight(tile, markerColor, markerType);
                CreateMarkerCountdownText(pos, charges, Color.white);
            }
        }
        
        // Get current move step from WaveManager
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        
        // Register as active area marker with charge tracking
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, markerType, true, charges);
        activeAreaMarkers.Add(areaMarker);
        
        Debug.Log($"[PlayerMarkerSystem] Created {markerType} auto-capture marker with {positions.Count} tiles ({charges} charges, expires in {expiresAfterMoves} moves)");
    }
    
    /// <summary>
    /// Infinity + Unit: Wave join (removes Unit, takes position, moves with wave)
    /// </summary>
    private CollisionResult HandleInfinityWaveJoin(CubeManager playerInfinity, CubeManager waveUnit, Vector2Int position)
    {
        // Remove the Unit cube
        if (ProcessCubeCapture(waveUnit, position, MarkerType.Unit, null, false))
        {
            // Player Infinity takes the position and continues moving with wave
            // Don't destroy player Infinity - it continues up
            playerInfinity.position = position;
            return new CollisionResult(true, false); // Handled, but don't destroy player cube
        }
        
        return new CollisionResult(false);
    }
    
    /// <summary>
    /// Handles face painting for Infinity collisions.
    /// Paints the collision face and destroys player cube (unless continueUp is true).
    /// Responsibility: PlayerMarkerSystem handles collision detection and face painting coordination.
    /// The actual face painting is delegated to CubeManager.PaintFace().
    /// </summary>
    private CollisionResult HandleInfinityFacePaint(CubeManager waveInfinity, CubeManager playerCube, CubeType paintedType, Vector2Int position, bool destroyPlayerCube = true)
    {
        if (waveInfinity.type != CubeType.Infinity) 
            return new CollisionResult(false);
        
        // Determine which face was hit based on relative positions
        // Player cube is moving up, so it hits the front face of the wave cube
        CubeFace collisionFace = CubeFace.Front;
        
        // Paint the face with the appropriate status
        // Face effects when painted face lands on grid:
        // - InfinityFace: Creates infinity resonance (all infinity cubes phaseable for 2-4 move forwards)
        // - MatrixFace: Leaves a marker that can detonate for 3x3 capture area
        // - RecursionFace: Leaves a marker that forms a 5 tile + arrangement that auto captures
        FaceStatus faceStatus = paintedType switch
        {
            CubeType.Unit => FaceStatus.None, // Unit collisions don't paint faces (Unit is destroyed)
            CubeType.Matrix => FaceStatus.MatrixFace, // Matrix marker behavior (3x3 detonation marker)
            CubeType.Recursion => FaceStatus.RecursionFace, // Recursion marker behavior (5 tile + auto-capture)
            CubeType.Infinity => FaceStatus.InfinityFace, // Resonance effect (all infinity cubes phaseable)
            _ => FaceStatus.None
        };
        
        // Delegate face painting to CubeManager
        // CubeManager is responsible for managing face state and visuals
        waveInfinity.PaintFace(collisionFace, faceStatus, GetFaceColorForType(paintedType), -1);
        
        // Face effects are triggered when the painted face rotates to become the down face and touches the grid:
        // - InfinityFace: Creates infinity resonance (all infinity cubes phaseable for 2-4 move forwards)
        //   Implementation: Check GetActiveFaceStatus() == FaceStatus.InfinityFace in cube landing/movement logic
        // - MatrixFace: Creates a 3x3 detonation marker at the landing tile
        //   Implementation: Check GetActiveFaceStatus() == FaceStatus.MatrixFace, create cube marker with size 3
        // - RecursionFace: Creates a 5 tile + arrangement marker that auto-captures
        //   Implementation: Check GetActiveFaceStatus() == FaceStatus.RecursionFace, create cross marker pattern
        // TODO: Implement grid touch detection in Tile.HandleCubeLanding() or CubeManager.MoveForward()
        // to check GetActiveFaceStatus() and trigger appropriate marker creation via PlayerMarkerSystem
        
        Debug.Log($"[PlayerMarkerSystem] Painted {collisionFace} face of Infinity cube at ({position.x}, {position.y}) with {paintedType} type (status: {faceStatus})");
        
        // Return result indicating whether to destroy player cube
        return new CollisionResult(true, destroyPlayerCube);
    }
    
    /// <summary>
    /// Infinity + Infinity: Face paint + resonance effect
    /// </summary>
    private CollisionResult HandleInfinityInfinityCollision(CubeManager waveInfinity, CubeManager playerInfinity, Vector2Int position)
    {
        // Paint face for resonance
        CollisionResult result = HandleInfinityFacePaint(waveInfinity, playerInfinity, CubeType.Infinity, position, false);
        
        // TODO: When painted face touches grid, ALL Infinity cubes become phaseable
        // This will be handled by the face painting system when the face rotates down
        
        // Player Infinity continues up (don't destroy)
        return new CollisionResult(true, false);
    }
    
    /// <summary>
    /// Gets color for face painting based on cube type
    /// </summary>
    private Color GetFaceColorForType(CubeType type)
    {
        return type switch
        {
            CubeType.Unit => Color.gray,
            CubeType.Matrix => Color.cyan,
            CubeType.Recursion => new Color(0.8f, 0.5f, 0.2f), // Amber brown
            CubeType.Infinity => Color.black,
            _ => Color.white
        };
    }
    
    #endregion
    
    /// <summary>
    /// Destroys player cube and removes it from tracking list.
    /// </summary>
    private void HandlePlayerCubeDestruction(CubeManager playerCube, ref int collisionCount, ref int playerCubeIndex)
    {
        collisionCount++;
        if (playerCube != null && playerCube.gameObject != null)
        {
            Destroy(playerCube.gameObject);
        }
        playerCubes.RemoveAt(playerCubeIndex);
    }
    
    /// <summary>
    /// Validates if a position is within grid bounds.
    /// </summary>
    private bool IsValidPosition(Vector2Int position, GridManager gridManager)
    {
        return position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    /// <summary>
    /// Clears all player cubes, destroying their GameObjects and clearing the tracking list.
    /// Called during wave completion to prevent orphaned cubes between waves.
    /// </summary>
    public void ClearPlayerCubes()
    {
        if (playerCubes == null) return;

        int clearedCount = playerCubes.Count;

        // Destroy all player cube GameObjects
        foreach (var cube in playerCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }

        // Clear the list
        playerCubes.Clear();

        if (clearedCount > 0)
        {
            Debug.Log($"[PlayerMarkerSystem] Cleared {clearedCount} player cubes");
        }
    }

    #endregion

    #region Paired Wave System Integration

    /// <summary>
    /// Records marker position for paired wave inheritance system.
    /// Always records markers during any wave - they will be used when the wave is mirrored.
    /// </summary>
    private void RecordMarkerForPairedWave(Vector2Int position, MarkerMode markerType)
    {
        if (actionManager?.WaveManager == null) return;

        // Always record marker positions - they will be used when the wave is mirrored
        actionManager.WaveManager.RecordMarkerPosition(position, markerType);
    }

    #endregion

    #region Visual Creation Methods

    public GameObject CreateUnitMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Unit = Blue-gray (lighter variant for marker visibility)
            SetTileHighlight(tile, new Color(0.5f, 0.6f, 0.7f, 1f), "Unit");
        }

        GameObject dummy = new GameObject($"UnitMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    public GameObject CreateRecursionMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Recursion = Deep amber brown (warm brown-orange)
            SetTileHighlight(tile, new Color(0.8f, 0.5f, 0.2f, 1f), "Recursion");
        }

        GameObject dummy = new GameObject($"RecursionMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    public GameObject CreateMatrixMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Matrix = Vibrant light blue
            SetTileHighlight(tile, new Color(0.3f, 0.7f, 1f, 1f), "Matrix");
        }

        GameObject dummy = new GameObject($"MatrixMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }



    private GameObject CreateCubeMarkerVisual(Vector2Int position, CubeMarkerType type)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color highlightColor = type switch
            {
                CubeMarkerType.Unit => Color.magenta,
                CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f), // Dark magenta
                CubeMarkerType.Matrix => Color.cyan,
                CubeMarkerType.Cube => Color.yellow,
                _ => Color.white
            };
            string markerName = $"Cube{type}";
            SetTileHighlight(tile, highlightColor, markerName);
        }

        GameObject dummy = new GameObject($"CubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    private GameObject CreatePoweredCubeMarkerVisual(Vector2Int position, CubeMarkerType type)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color baseColor = type switch
            {
                CubeMarkerType.Unit => Color.magenta,
                CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f),
                CubeMarkerType.Matrix => Color.cyan,
                CubeMarkerType.Cube => Color.yellow,
                _ => Color.white
            };
            Color poweredColor = new Color(baseColor.r * 1.5f, baseColor.g * 1.5f, baseColor.b * 1.5f, baseColor.a);
            string markerName = $"PoweredCube{type}";
            SetTileHighlight(tile, poweredColor, markerName);
        }

        GameObject dummy = new GameObject($"PoweredCubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    #endregion

    #region Utility Methods

    private bool IsValidPosition(Vector2Int position)
    {
        return actionManager.GridManager.IsValidGridPosition(position);
    }

    private bool CanPlaceMarkerAt(Vector2Int position)
    {
        // Task 6: Check line divider restriction - markers can only be placed below the line (when enabled)
        if (actionManager?.GridManager != null && actionManager.GridManager.LineDividerEnabled)
        {
            if (!actionManager.GridManager.IsPositionBelowLine(position.y))
            {
                Debug.Log($"[Task 6] Cannot place marker at ({position.x}, {position.y}) - above line divider (row {actionManager.GridManager.LineDividerRow})");
                return false;
            }
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

    private void SetTileHighlight(Tile tile, Color color, string markerType)
    {
        Vector2Int pos = new Vector2Int(tile.x, tile.y);

        // Remove existing overlay if present
        ClearTileHighlight(tile);

        // Create temporary overlay object (similar to tile's state overlay system)
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overlay.name = $"ActionMarker_{markerType}_{tile.x}_{tile.y}";
        overlay.transform.SetParent(tile.transform);
        overlay.transform.localPosition = new Vector3(0, 0.52f, 0); // Slightly above tile overlay
        overlay.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);

        // Remove collider to avoid physics issues
        Destroy(overlay.GetComponent<Collider>());

        // Create material with highlight color
        Renderer overlayRenderer = overlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            Material highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = color;
            highlightMaterial.SetFloat("_Metallic", 0.3f);
            highlightMaterial.SetFloat("_Smoothness", 0.7f);

            // Enable emission for glow effect
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", color * 0.3f);

            overlayRenderer.material = highlightMaterial;
        }

        // Store overlay for cleanup
        temporaryMarkerOverlays[pos] = overlay;

        Debug.Log($"Created {markerType} highlight overlay at ({tile.x}, {tile.y}) with color {color}");
    }

    private void ClearTileHighlight(Tile tile)
    {
        Vector2Int pos = new Vector2Int(tile.x, tile.y);

        if (temporaryMarkerOverlays.TryGetValue(pos, out GameObject overlay))
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
            temporaryMarkerOverlays.Remove(pos);
            Debug.Log($"Cleared highlight overlay at ({tile.x}, {tile.y})");
        }
    }

    private void ClearTileHighlight(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            ClearTileHighlight(tile);
        }
    }
    
    #region Marker Countdown Text
    
    /// <summary>
    /// Creates a countdown text display on a tile for auto-capture markers (recursion-type)
    /// </summary>
    private void CreateMarkerCountdownText(Vector2Int position, int remainingMoves, Color textColor)
    {
        if (markerCountdownTexts.ContainsKey(position))
        {
            // Already exists, just update it
            UpdateMarkerCountdownText(position, remainingMoves);
            return;
        }
        
        Tile tile = actionManager?.GridManager?.GetTileAt(position.x, position.y);
        if (tile == null) return;
        
        // Create text object
        GameObject textObj = new GameObject($"MarkerCountdown_{position.x}_{position.y}");
        textObj.transform.SetParent(tile.transform);
        textObj.transform.localPosition = new Vector3(0, 1.0f, 0); // Above the tile overlay
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = remainingMoves.ToString();
        textMesh.fontSize = 12;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.15f;
        
        // Make text face camera (billboard style)
        if (Camera.main != null)
        {
            textObj.transform.LookAt(Camera.main.transform);
            textObj.transform.Rotate(0, 180, 0);
        }
        
        markerCountdownTexts[position] = textMesh;
        Debug.Log($"[PlayerMarkerSystem] Created countdown text at ({position.x}, {position.y}) showing {remainingMoves}");
    }
    
    /// <summary>
    /// Updates the countdown text for a marker position
    /// </summary>
    private void UpdateMarkerCountdownText(Vector2Int position, int remainingMoves)
    {
        if (!markerCountdownTexts.TryGetValue(position, out TextMesh textMesh)) return;
        if (textMesh == null)
        {
            markerCountdownTexts.Remove(position);
            return;
        }
        
        textMesh.text = remainingMoves.ToString();
        
        // Update color based on remaining moves (visual urgency)
        if (remainingMoves <= 1)
            textMesh.color = Color.red;
        else if (remainingMoves <= 2)
            textMesh.color = Color.yellow;
        else
            textMesh.color = Color.white;
        
        // Re-orient to face camera each update
        if (Camera.main != null && textMesh.gameObject != null)
        {
            textMesh.transform.LookAt(Camera.main.transform);
            textMesh.transform.Rotate(0, 180, 0);
        }
    }
    
    /// <summary>
    /// Removes the countdown text for a marker position
    /// </summary>
    private void ClearMarkerCountdownText(Vector2Int position)
    {
        if (markerCountdownTexts.TryGetValue(position, out TextMesh textMesh))
        {
            if (textMesh != null)
            {
                Destroy(textMesh.gameObject);
            }
            markerCountdownTexts.Remove(position);
            Debug.Log($"[PlayerMarkerSystem] Cleared countdown text at ({position.x}, {position.y})");
        }
    }
    
    /// <summary>
    /// Clears all marker countdown texts
    /// </summary>
    private void ClearAllMarkerCountdownTexts()
    {
        foreach (var kvp in markerCountdownTexts)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        markerCountdownTexts.Clear();
    }
    
    #endregion

    private void DestroyMarkerVisual(GameObject visual)
    {
        if (visual != null)
        {
            // Extract position from the visual object name or transform
            string[] nameParts = visual.name.Split('_');
            if (nameParts.Length >= 3)
            {
                if (int.TryParse(nameParts[nameParts.Length - 2], out int x) &&
                    int.TryParse(nameParts[nameParts.Length - 1], out int y))
                {
                    // Clear the temporary highlight overlay
                    ClearTileHighlight(new Vector2Int(x, y));
                    Debug.Log($"Cleared tile highlight at ({x}, {y}) after marker removal");
                }
            }

            Destroy(visual);
        }
    }

    #endregion

    #region Clear All Actions

    public void ClearAllActions()
    {
        HideAreaPreview();

        while (UnitMarkers.Count > 0)
        {
            var marker = UnitMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        // Note: Unit marker count managed by PlayerActionManager

        while (RecursionMarkers.Count > 0)
        {
            var marker = RecursionMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        // Note: Recursion marker count managed by PlayerActionManager

        while (MatrixMarkers.Count > 0)
        {
            var marker = MatrixMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                DestroyMarkerVisual(visual);
            }
        }
        // Note: Matrix marker count managed by PlayerActionManager

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
            DestroyMarkerVisual(cubeMarker.visualObject);
        }
        cubeMarkers.Clear();

        foreach (var preview in previewObjects)
        {
            if (preview != null) Destroy(preview);
        }
        previewObjects.Clear();
        showingPreview = false;

        // Clear any remaining temporary overlays
        var overlaysToRemove = temporaryMarkerOverlays.Keys.ToList();
        foreach (var pos in overlaysToRemove)
        {
            ClearTileHighlight(pos);
        }
        
        // Clear all countdown texts
        ClearAllMarkerCountdownTexts();
        
        // Clear active area markers (auto-capture markers)
        activeAreaMarkers.Clear();

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
        showingPreview = false;
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

    #region Effects

    private IEnumerator ShowMarkerTriggerEffect(Vector2Int position)
    {
        Vector3 worldPos = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0.1f);

        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = $"TriggerEffect_{position.x}_{position.y}";
        effect.transform.position = worldPos;
        effect.transform.localScale = Vector3.zero;

        Destroy(effect.GetComponent<Collider>());
        Renderer renderer = effect.GetComponent<Renderer>();

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);

            Color color = Color.white;
            color.a = 1f - t;
            renderer.material.color = color;

            yield return null;
        }

        Destroy(effect);
    }

    #endregion
}