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

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;

    // Parent reference
    private PlayerActionManager actionManager;

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

        var marker = new UnitMarker(position, Time.time);
        marker.visualObject = visualManager?.CreateUnitMarkerVisual(position);

        UnitMarkers.Enqueue(marker);
        actionManager.ConsumeUnitCharge(position);

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
        var cubes = FindAllCubesAt(position);
        bool success = false;

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

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "unit", success, cubes.Count);
        }

        visualManager?.DestroyMarkerVisual(marker.visualObject);
        visualManager?.ShowMarkerTriggerEffect(position);

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
        marker.visualObject = visualManager?.CreateRecursionMarkerVisual(position);

        RecursionMarkers.Enqueue(marker);
        actionManager.ConsumeRecursionCharge();

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
        var cubes = FindAllCubesAt(position);
        bool success = false;

        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Recursion, marker);
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
        visualManager?.ShowMarkerTriggerEffect(position);

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
        GameObject visual = visualManager?.CreateMatrixMarkerVisual(centerPosition);
        if (visual != null) newMarker.visualObjects.Add(visual);

        MatrixMarkers.Enqueue(newMarker);
        actionManager.ConsumeMatrixCharge();

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

        Vector3 centerWorldPosition = actionManager.GridManager.GridToWorldPosition(marker.centerPosition.x, marker.centerPosition.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, centerWorldPosition);

        foreach (var visual in marker.visualObjects)
        {
            visualManager?.DestroyMarkerVisual(visual);
        }

        foreach (var position in marker.affectedPositions)
        {
            Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
            if (tile != null && position != marker.centerPosition)
            {
                visualManager?.SetTileHighlight(tile, new Color(0f, 1f, 0f, 0.7f), "AreaExpansion");
            }

            var cubes = FindAllCubesAt(position);
            totalCubesAffected += cubes.Count;
            foreach (var cube in cubes)
            {
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Matrix);
            }
            visualManager?.ShowMarkerTriggerEffect(position);
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

        var marker = new InfinityMarker(position, Time.time);
        marker.visualObject = visualManager?.CreateInfinityMarkerVisual(position);

        InfinityMarkers.Enqueue(marker);
        actionManager.ConsumeInfinityCharge();

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
        var cubes = FindAllCubesAt(position);
        bool success = false;

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

        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "infinity", success, cubes.Count);
        }

        visualManager?.DestroyMarkerVisual(marker.visualObject);
        visualManager?.ShowMarkerTriggerEffect(position);

        Debug.Log($"Infinity marker triggered at ({position.x}, {position.y}) - Perfect: {marker.isPerfectTiming}");
        return success;
    }

    #endregion

    #region Cube Markers

    public void CreateCubeMarker(Vector2Int position, CubeMarkerType type = CubeMarkerType.Matrix, int size = 3, int uses = -1)
    {
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
        cubeMarker.visualObject = visualManager?.CreateCubeMarkerVisual(position, type);

        cubeMarkers.Add(cubeMarker);

        Debug.Log($"Cube marker ({type}, size {size}x{size}, {finalUses} uses) created at ({position.x}, {position.y})");
    }

    public bool TriggerNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        
        // Decrement uses
        cubeMarker.remainingUses--;
        
        // Only remove if no uses left
        if (cubeMarker.remainingUses <= 0)
        {
            cubeMarkers.RemoveAt(0);
        }
        else
        {
            Debug.Log($"[Concentrated Expansion] Cube marker has {cubeMarker.remainingUses} uses remaining");
        }

        return TriggerCubeMarkerAt(cubeMarker, cubeMarker.remainingUses <= 0);
    }

    public bool TriggerCubeMarkerAt(CubeMarker cubeMarker, bool destroyVisual = true)
    {
        cubeMarkersTriggered++;

        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(cubeMarker.position.x, cubeMarker.position.y);
        TriggerMarkerAudioEvent(GameAudioEvent.MarkerTriggered, worldPosition, 1.2f);

        // Only destroy visual if this is the last use
        if (destroyVisual)
        {
            visualManager?.DestroyMarkerVisual(cubeMarker.visualObject);
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

        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) with {markerType} marker{(isSameTypeMatch ? " (same-type match!)" : "")}");

        if (isSameTypeMatch)
        {
            switch (cube.type)
            {
                case CubeType.Matrix:
                    CreateCubeMarker(position, CubeMarkerType.Matrix, 3);
                    break;
                case CubeType.Recursion:
                    CreateCubeMarker(position, CubeMarkerType.Recursion, 2);
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

        // Show capture success visual feedback
        if (visualManager != null)
        {
            visualManager.ShowCaptureSuccessEffect(position, cube.type);
        }
        
        RemoveCubeFromWaveManager(cube);
        NotifyWaveManager(wm => wm.OnCubeCaptured(cube.type));
        Destroy(cube.gameObject);
        return true;
    }

    public List<CubeManager> FindAllCubesAt(Vector2Int position)
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
                SpawnPlayerCubeAt(marker.position, CubeType.Unit, false);
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
                SpawnPlayerCubeAt(marker.centerPosition, CubeType.Matrix, true);
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
                SpawnPlayerCubeAt(marker.position, CubeType.Recursion, false);
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

    private void SpawnPlayerCubeAt(Vector2Int position, CubeType cubeType, bool isMatrixCube)
    {
        var waveManager = actionManager.WaveManager;
        var grid = actionManager.GridManager;

        int prefabIndex = (int)cubeType;
        if (prefabIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[prefabIndex] == null)
        {
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
        cube.isMatrixCube = isMatrixCube;
        cube.usePhysics = false;
        cube.ConfigurePlayerCubePhysics();

        MakeCubeTranslucent(cube);
        playerCubes.Add(cube);

        Debug.Log($"[PlayerMarkerSystem] Spawned {cubeType} player cube at ({position.x}, {position.y}){(isMatrixCube ? " (area capture)" : "")}");
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

    private bool CanPlaceMarkerAt(Vector2Int position)
    {
        // Task 6: Check line divider restriction - player must be in safe zone (below line)
        if (actionManager?.GridManager != null && actionManager.GridManager.LineDividerEnabled)
        {
            if (!actionManager.GridManager.IsPlayerInSafeZone())
            {
                Debug.Log($"[Task 6] Cannot place marker - player is above line divider (danger zone)");
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
