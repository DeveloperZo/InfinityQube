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

    // Marker Collections - Five-tier system (Unit, Prime, Recursion, Infinity, Cube)
    [SerializeField] public Queue<UnitMarker> UnitMarkers = new Queue<UnitMarker>();
    [SerializeField] public Queue<RecursionMarker> RecursionMarkers = new Queue<RecursionMarker>();
    [SerializeField] public Queue<PrimeMarker> PrimeMarkers = new Queue<PrimeMarker>();
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
   


    #region Data Structures

    [System.Serializable]
    public class CubeMarker
    {
        public Vector2Int position;
        public CubeMarkerType type;
        public bool isPoweredUp = false;
        public float creationTime;
        public GameObject visualObject;

        public CubeMarker(Vector2Int pos, CubeMarkerType markerType)
        {
            position = pos;
            type = markerType;
            creationTime = Time.time;
        }
    }

    /// <summary>
    /// Cube marker types for markers created from prime cube captures
    /// </summary>
    public enum CubeMarkerType
    {
        /// <summary>Unit cube marker: Basic targeting (formerly Individual/Light)</summary>
        Unit,
        /// <summary>Recursion cube marker: Enhanced targeting for recursion cubes (formerly Heavy)</summary>
        Recursion,
        /// <summary>Prime cube marker: Area coverage (formerly Area)</summary>
        Prime,
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
        /// <summary>Prime marker: Area coverage marker (formerly Area)</summary>
        Prime,
        /// <summary>Infinity marker: Special marker for infinity cubes</summary>
        Infinity,
        /// <summary>Cube marker: Generated from prime cube captures</summary>
        CubeMarker,
        

    }

    #endregion

    void Awake()
    {
        
        actionManager = FindFirstObjectByType<PlayerActionManager>();
    }
    public void Initialize(PlayerActionManager parent)
    {
        actionManager = parent;
    }

    private void OnDestroy()
    {
        // Clean up all temporary overlays
        var overlaysToRemove = temporaryMarkerOverlays.Keys.ToList();
        foreach (var pos in overlaysToRemove)
        {
            ClearTileHighlight(pos);
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

    #region Prime Markers

    public bool PlacePrimeMarker(Vector2Int centerPosition, int size)
    {
        if (!actionManager.CanPlacePrimeMarker() || !IsValidPosition(centerPosition))
            return false;

        if (!CanPlaceMarkerAt(centerPosition))
            return false;

        PrimeMarker newMarker = new PrimeMarker(centerPosition, size, Time.time);
        newMarker.affectedPositions = GetAreaPositions(centerPosition, size);
        GameObject visual = CreatePrimeMarkerVisual(centerPosition);
        newMarker.visualObjects.Add(visual);

        PrimeMarkers.Enqueue(newMarker);
        actionManager.ConsumePrimeCharge();

        // Record marker position for paired wave system (record center position)
        RecordMarkerForPairedWave(centerPosition, MarkerMode.Prime);

        Debug.Log($"Prime marker placed at ({centerPosition.x}, {centerPosition.y})");
        return true;
    }

    public bool RemovePrimeMarkerAt(Vector2Int centerPosition)
    {
        var markersArray = PrimeMarkers.ToArray();
        var newQueue = new Queue<PrimeMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.centerPosition == centerPosition && !removed)
            {
                foreach (var visual in marker.visualObjects)
                {
                    DestroyMarkerVisual(visual);
                }
                actionManager.ReleasePrimeMarker();
                actionManager.OnPrimeMarkerRemoved(); // Decrement placement counter
                removed = true;
                Debug.Log($"Removed prime marker at ({centerPosition.x}, {centerPosition.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        PrimeMarkers = newQueue;
        return removed;
    }

    public bool HasPrimeMarkerAt(Vector2Int centerPosition)
    {
        return PrimeMarkers.Any(m => m.centerPosition == centerPosition);
    }

    public bool TriggerNextPrimeMarker()
    {
        if (PrimeMarkers.Count == 0) return false;

        var marker = PrimeMarkers.Dequeue();
        actionManager.ReleasePrimeMarker();

        return TriggerPrimeMarkerAt(marker);
    }

    private bool TriggerPrimeMarkerAt(PrimeMarker marker)
    {
        bool anySuccess = false;
        int totalCubesAffected = 0;

        Debug.Log($"Triggering prime marker - expanding from center ({marker.centerPosition.x}, {marker.centerPosition.y}) to {marker.affectedPositions.Count} tiles");

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
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Prime);
            }
            StartCoroutine(ShowMarkerTriggerEffect(position));
        }

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(marker.centerPosition, "prime", anySuccess, totalCubesAffected);
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

    public void CreateCubeMarker(Vector2Int position, CubeMarkerType type = CubeMarkerType.Prime)
    {
        var cubeMarker = new CubeMarker(position, type);
        cubeMarker.visualObject = CreateCubeMarkerVisual(position, type);

        cubeMarkers.Add(cubeMarker);

        Debug.Log($"Cube marker ({type}) created at ({position.x}, {position.y})");
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

        var tempPrimeMarker = new PrimeMarker(cubeMarker.position, 3, Time.time);
        tempPrimeMarker.affectedPositions = GetAreaPositions(cubeMarker.position, 3);
        return TriggerPrimeMarkerAt(tempPrimeMarker);
        
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

    private bool ProcessCubeCapture(CubeManager cube, Vector2Int position, MarkerType markerType, object marker = null)
    {
        if (cube == null || cube.isDestroyed) return false;

        if (!cube.CanBeCaptured())
        {
            Debug.Log($"Cube at ({position.x}, {position.y}) cannot be captured due to face status: {cube.GetActiveFaceStatus()}");
            return false;
        }

        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) with {markerType} marker");

        if (cube.type == CubeType.Prime)
        {

            CreateCubeMarker(position, CubeMarkerType.Prime);
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

    private List<CubeManager> FindAllCubesAt(Vector2Int position)
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
    /// - Prime markers → Prime cubes (area capture - 3x3)
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

        // Process Prime markers → Prime cubes (area capture)
        var primeMarkersArray = PrimeMarkers.ToArray();
        foreach (var marker in primeMarkersArray)
        {
            if (marker != null)
            {
                foreach (var visual in marker.visualObjects)
                {
                    DestroyMarkerVisual(visual);
                }
                SpawnPlayerCubeAt(marker.centerPosition, CubeType.Prime, true); // isPrime = true for area capture
                spawnedCount++;
            }
        }
        PrimeMarkers.Clear();

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
    private void SpawnPlayerCubeAt(Vector2Int position, CubeType cubeType, bool isPrimeCube)
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
        cube.isPrimeCube = isPrimeCube; // For area capture logic
        cube.usePhysics = false;
        cube.ConfigurePlayerCubePhysics();

        MakeCubeTranslucent(cube);
        playerCubes.Add(cube);

        Debug.Log($"[PlayerMarkerSystem] Spawned {cubeType} player cube at ({position.x}, {position.y}){(isPrimeCube ? " (area capture)" : "")}");
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
    /// Prime cubes capture in an area (3x3), other cubes capture single target.
    /// </summary>
    private bool ProcessCollisionAtPosition(CubeManager playerCube, Vector2Int position, ref int collisionCount, ref int playerCubeIndex)
    {
        bool anyCaptured = false;

        if (playerCube.isPrimeCube)
        {
            // Prime cube: capture in 3x3 area
            var areaPositions = GetAreaPositions(position, 3);
            foreach (var areaPos in areaPositions)
            {
                var cubesAtArea = FindAllCubesAt(areaPos);
                foreach (var cube in cubesAtArea)
                {
                    if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                    
                    if (ProcessCubeCapture(cube, areaPos, MarkerType.Prime, null))
                    {
                        anyCaptured = true;
                    }
                }
            }
        }
        else
        {
            // Unit/Recursion/Infinity cube: capture single target
            var cubesAtPosition = FindAllCubesAt(position);
            foreach (var cube in cubesAtPosition)
            {
                if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                
                if (ProcessCubeCapture(cube, position, MarkerType.Unit, null))
                {
                    anyCaptured = true;
                    break; // Only capture one cube for non-Prime
                }
            }
        }

        if (anyCaptured)
        {
            HandlePlayerCubeDestruction(playerCube, ref collisionCount, ref playerCubeIndex);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Handles collision detection for adjacent cubes moving toward each other.
    /// Verifies the wave cube came from where the player cube is now.
    /// Prime cubes capture in an area, others capture single target.
    /// </summary>
    private void ProcessPassThroughCollision(CubeManager playerCube, Vector2Int playerPos, Vector2Int playerPreviousPos, 
        ref int collisionCount, ref int playerCubeIndex)
    {
        bool anyCaptured = false;

        if (playerCube.isPrimeCube)
        {
            // Prime cube: check area around previous position
            var areaPositions = GetAreaPositions(playerPreviousPos, 3);
            foreach (var areaPos in areaPositions)
            {
                var cubesAtArea = FindAllCubesAt(areaPos);
                foreach (var cube in cubesAtArea)
                {
                    if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                    
                    if (ProcessCubeCapture(cube, areaPos, MarkerType.Prime, null))
                    {
                        anyCaptured = true;
                    }
                }
            }
        }
        else
        {
            // Single target capture
            var cubesAtPreviousPos = FindAllCubesAt(playerPreviousPos);
            foreach (var cube in cubesAtPreviousPos)
            {
                if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
                
                // Verify wave cube came from player's current position (confirms they passed through)
                Vector2Int waveCubeSourcePos = new Vector2Int(cube.position.x, cube.position.y + 1);
                if (waveCubeSourcePos == playerPos)
                {
                    if (ProcessCubeCapture(cube, playerPreviousPos, MarkerType.Unit, null))
                    {
                        anyCaptured = true;
                        break;
                    }
                }
            }
        }

        if (anyCaptured)
        {
            HandlePlayerCubeDestruction(playerCube, ref collisionCount, ref playerCubeIndex);
        }
    }
    
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

    public GameObject CreatePrimeMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Prime = Vibrant light blue
            SetTileHighlight(tile, new Color(0.3f, 0.7f, 1f, 1f), "Prime");
        }

        GameObject dummy = new GameObject($"PrimeMarker_{position.x}_{position.y}");
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
                CubeMarkerType.Prime => Color.cyan,
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
                CubeMarkerType.Prime => Color.cyan,
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
        return !HasUnitMarkerAt(position) && !HasRecursionMarkerAt(position) && !HasPrimeMarkerAt(position);
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

        while (PrimeMarkers.Count > 0)
        {
            var marker = PrimeMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                DestroyMarkerVisual(visual);
            }
        }
        // Note: Prime marker count managed by PlayerActionManager

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