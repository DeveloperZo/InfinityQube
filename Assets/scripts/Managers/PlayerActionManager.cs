using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;
using static PlayerActionManager;

public class PlayerActionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private WaveManager waveManager;

    [Header("Individual Marker Settings")]
    [SerializeField] public int maxIndividualMarkers = 3;
    [SerializeField] public float individualMarkerCooldown = 2f;
    [SerializeField] private Material individualMarkerMaterial;

    [Header("Area Marker Settings")]
    [SerializeField] public int maxAreaMarkers = 2;
    [SerializeField] public float areaMarkerCooldown = 4f;
    [SerializeField] private Material areaMarkerMaterial;
    [SerializeField] public int areaMarkerSize = 2; // 3x3 by default

    [Header("Marker Settings")]
    [SerializeField] private float perfectTimingWindow = 0.2f;


    [Header("Cube Marker Settings")]
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material poweredCubeMarkerMaterial;

    [Header("Input Settings")]
    [SerializeField] private KeyCode individualMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode areaMarkerKey = KeyCode.G;
    [SerializeField] private KeyCode triggerIndividualKey = KeyCode.R;
    [SerializeField] private KeyCode triggerAreaKey = KeyCode.T;
    [SerializeField] private KeyCode triggerCubeMarkerKey = KeyCode.Q;
    [SerializeField] private KeyCode powerUpCubeMarkerKey = KeyCode.E;

    [Header("Visual Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);

    // Individual Markers
    public Queue<IndividualMarker> individualMarkers = new Queue<IndividualMarker>();
    public int currentIndividualMarkers = 0;
    private float lastIndividualMarkerTime = 0f;

    // Area Markers
    public Queue<AreaMarker> areaMarkers = new Queue<AreaMarker>();
    public int currentAreaMarkers = 0;
    private float lastAreaMarkerTime = 0f;


    // Cube Markers (from blue cube captures)
    private List<CubeMarker> cubeMarkers = new List<CubeMarker>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    // Statistics
    public int individualMarkersPlaced = 0;
    public int areaMarkersPlaced = 0;
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;
    private bool inputEnabled = false;

    #region Data Structures

    [System.Serializable]
    public class IndividualMarker
    {
        public Vector2Int position;
        public float placementTime;
        public GameObject visualObject;
        public bool isPerfectTiming = false;

        public IndividualMarker(Vector2Int pos, float time)
        {
            position = pos;
            placementTime = time;
        }
    }

    [System.Serializable]
    public class AreaMarker
    {
        public Vector2Int centerPosition;
        public int size;
        public float placementTime;
        public List<GameObject> visualObjects = new List<GameObject>();
        public List<Vector2Int> affectedPositions = new List<Vector2Int>();

        public AreaMarker(Vector2Int center, int markerSize, float time)
        {
            centerPosition = center;
            size = markerSize;
            placementTime = time;
        }
    }


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

    public enum CubeMarkerType
    {
        Individual, // From area marker + blue cube
        Area       // From individual marker + blue cube
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        FindReferences();
        inputEnabled = true;

        // Reset cooldowns and charges for debugging
        lastIndividualMarkerTime = -individualMarkerCooldown; // Allow immediate use
        lastAreaMarkerTime = -areaMarkerCooldown; // Allow immediate use
        currentIndividualMarkers = 0;
        currentAreaMarkers = 0;
    }

    private void Update()
    {
        HandleInput();
        CheckCubeInteractions();
    }

    private void OnDestroy()
    {
        ClearAllMarkers();
    }

    #endregion

    #region Initialization

    private void FindReferences()
    {
        if (gridManager == null)
            gridManager = GridManager.Instance ?? FindObjectOfType<GridManager>();
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerManager>();
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();

        if (gridManager == null)
        {
            Debug.LogError("PlayerActionManager requires GridManager!");
            enabled = false;
        }
    }

    #endregion

    #region Input Handling

    public void SetInput(bool condition)
    {
       playerManager.isDead = condition;
    }
    private void HandleInput()
    {
        if (playerManager == null || !playerManager.IsAlive()) return;

        HandleIndividualMarkerInput();
        HandleAreaMarkerInput();
        HandleTriggerInputs();
        HandleCubeMarkerInputs();
    }

    private void HandleIndividualMarkerInput()
    {
        if (Input.GetKeyDown(individualMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (HasIndividualMarkerAt(playerPos))
            {
                RemoveIndividualMarkerAt(playerPos);
            }
            else if (CanPlaceIndividualMarker())
            {
                PlaceIndividualMarker(playerPos);
            }
        }
    }

    private void HandleAreaMarkerInput()
    {
        if (Input.GetKeyDown(areaMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (HasAreaMarkerAt(playerPos))
            {
                RemoveAreaMarkerAt(playerPos);
            }
            else if (CanPlaceAreaMarker())
            {
                PlaceAreaMarker(playerPos, areaMarkerSize);
            }
        }
    }

    private void HandleTriggerInputs()
    {
        if (Input.GetKeyDown(triggerIndividualKey))
        {
            TriggerNextIndividualMarker();
        }

        if (Input.GetKeyDown(triggerAreaKey))
        {
            TriggerNextAreaMarker();
        }
    }

    private void HandleCubeMarkerInputs()
    {
        if (Input.GetKeyDown(triggerCubeMarkerKey))
        {
            TriggerNextCubeMarker();
        }

        if (Input.GetKeyDown(powerUpCubeMarkerKey))
        {
            PowerUpNextCubeMarker();
        }
    }

    #endregion

    #region Individual Markers

    public bool CanPlaceIndividualMarker()
    {
        return currentIndividualMarkers < maxIndividualMarkers &&
               Time.time - lastIndividualMarkerTime >= individualMarkerCooldown;
    }

    public bool PlaceIndividualMarker(Vector2Int position)
    {
        if (!CanPlaceIndividualMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new IndividualMarker(position, Time.time);
        marker.visualObject = CreateIndividualMarkerVisual(position);

        individualMarkers.Enqueue(marker);
        currentIndividualMarkers++;
        lastIndividualMarkerTime = Time.time;
        individualMarkersPlaced++;

        Debug.Log($"Individual marker placed at ({position.x}, {position.y}). Count: {currentIndividualMarkers}/{maxIndividualMarkers}");
        return true;
    }

    public bool RemoveIndividualMarkerAt(Vector2Int position)
    {
        var markersArray = individualMarkers.ToArray();
        var newQueue = new Queue<IndividualMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                DestroyMarkerVisual(marker.visualObject);
                currentIndividualMarkers--;
                removed = true;
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        individualMarkers = newQueue;
        return removed;
    }

    public bool HasIndividualMarkerAt(Vector2Int position)
    {
        return individualMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextIndividualMarker()
    {
        if (individualMarkers.Count == 0) return false;

        var marker = individualMarkers.Dequeue();
        currentIndividualMarkers--;

        return TriggerIndividualMarkerAt(marker.position, marker);
    }

    private bool TriggerIndividualMarkerAt(Vector2Int position, IndividualMarker marker)
    {
        var cubes = FindAllCubesAt(position);
        bool success = false;

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Individual, marker);
        }

        // Check for perfect timing 
        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        DestroyMarkerVisual(marker.visualObject);
        StartCoroutine(ShowMarkerTriggerEffect(position));

        return success;
    }

    #endregion

    #region Area Markers

    public bool CanPlaceAreaMarker()
    {
        return currentAreaMarkers < maxAreaMarkers &&
               Time.time - lastAreaMarkerTime >= areaMarkerCooldown;
    }

    public bool PlaceAreaMarker(Vector2Int centerPosition, int size)
    {
        if (!CanPlaceAreaMarker() || !IsValidPosition(centerPosition))
            return false;

        // Only check if the CENTER position can have a marker
        if (!CanPlaceMarkerAt(centerPosition))
            return false;

        var marker = new AreaMarker(centerPosition, size, Time.time);

        // Calculate affected positions for later use, but don't create visuals for them yet
        marker.affectedPositions = GetAreaPositions(centerPosition, size);

        // Only create visual for the CENTER tile (green highlight)
        marker.visualObjects.Add(CreateAreaMarkerVisual(centerPosition));

        areaMarkers.Enqueue(marker);
        currentAreaMarkers++;
        lastAreaMarkerTime = Time.time;
        areaMarkersPlaced++;

        Debug.Log($"Area marker placed at center ({centerPosition.x}, {centerPosition.y}) - will affect {marker.affectedPositions.Count} tiles when triggered");
        return true;
    }

    public bool RemoveAreaMarkerAt(Vector2Int centerPosition)
    {
        var markersArray = areaMarkers.ToArray();
        var newQueue = new Queue<AreaMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.centerPosition == centerPosition && !removed)
            {
                foreach (var visual in marker.visualObjects)
                {
                    DestroyMarkerVisual(visual);
                }
                currentAreaMarkers--;
                removed = true;
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        areaMarkers = newQueue;
        return removed;
    }

    public bool HasAreaMarkerAt(Vector2Int centerPosition)
    {
        return areaMarkers.Any(m => m.centerPosition == centerPosition);
    }

    public bool TriggerNextAreaMarker()
    {
        if (areaMarkers.Count == 0) return false;

        var marker = areaMarkers.Dequeue();
        currentAreaMarkers--;

        return TriggerAreaMarkerAt(marker);
    }

    private bool TriggerAreaMarkerAt(AreaMarker marker)
    {
        bool anySuccess = false;

        Debug.Log($"Triggering area marker - expanding from center ({marker.centerPosition.x}, {marker.centerPosition.y}) to {marker.affectedPositions.Count} tiles");

        // Process cubes in all affected positions
        foreach (var position in marker.affectedPositions)
        {
            // Temporarily highlight the expanded area during trigger
            Tile tile = gridManager.GetTileAt(position.x, position.y);
            if (tile != null && position != marker.centerPosition) // Don't re-highlight center
            {
                SetTileHighlight(tile, new Color(0f, 1f, 0f, 0.7f), "AreaExpansion");
            }

            var cubes = FindAllCubesAt(position);
            foreach (var cube in cubes)
            {
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Area);
            }
            StartCoroutine(ShowMarkerTriggerEffect(position));
        }

        // Clear the center tile highlight
        DestroyMarkerVisual(marker.visualObjects[0]);

        // Clear expansion highlights after a delay
        StartCoroutine(ClearAreaExpansionAfterDelay(marker.affectedPositions, marker.centerPosition, 1f));

        return anySuccess;
    }

    private IEnumerator ClearAreaExpansionAfterDelay(List<Vector2Int> positions, Vector2Int centerPos, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var pos in positions)
        {
            if (pos != centerPos) // Don't clear center (already cleared)
            {
                Tile tile = gridManager.GetTileAt(pos.x, pos.y);
                if (tile != null)
                {
                    tile.ForceUpdateVisuals();
                }
            }
        }
    }
    private IEnumerator ClearAreaHighlightsAfterDelay(List<Vector2Int> positions, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var pos in positions)
        {
            Tile tile = gridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                tile.ForceUpdateVisuals();
            }
        }
    }
    #endregion


    #region Cube Markers

    private void CreateCubeMarker(Vector2Int position, CubeMarkerType type)
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
        DestroyMarkerVisual(cubeMarker.visualObject);

        if (cubeMarker.type == CubeMarkerType.Individual)
        {
            // Trigger as individual marker
            return TriggerIndividualMarkerAt(cubeMarker.position, new IndividualMarker(cubeMarker.position, Time.time));
        }
        else
        {
            // Trigger as area marker
            var tempAreaMarker = new AreaMarker(cubeMarker.position, areaMarkerSize, Time.time);
            tempAreaMarker.affectedPositions = GetAreaPositions(cubeMarker.position, areaMarkerSize);
            return TriggerAreaMarkerAt(tempAreaMarker);
        }
    }

    public bool PowerUpNextCubeMarker()
    {
        if (cubeMarkers.Count == 0) return false;

        var cubeMarker = cubeMarkers[0];
        if (cubeMarker.isPoweredUp) return false;

        cubeMarker.isPoweredUp = true;

        // Update visual
        DestroyMarkerVisual(cubeMarker.visualObject);
        cubeMarker.visualObject = CreatePoweredCubeMarkerVisual(cubeMarker.position, cubeMarker.type);

        Debug.Log($"Cube marker powered up at ({cubeMarker.position.x}, {cubeMarker.position.y})");
        return true;
    }

    #endregion

    #region Cube Interaction System

    public enum MarkerType
    {
        Individual,
        Area,
        CubeMarker
    }

    private void CheckCubeInteractions()
    {
        // This method is called from Update to check for cubes landing
        // Main cube processing happens in ProcessCubeCapture
    }

    private bool ProcessCubeCapture(CubeBehavior cube, Vector2Int position, MarkerType markerType, IndividualMarker individualMarker = null)
    {
        if (cube == null || cube.isDestroyed) return false;

        Debug.Log($"Processing {cube.type} cube capture at ({position.x}, {position.y}) with {markerType} marker");

        switch (cube.type)
        {
            case CubeType.Normal:
                return ProcessNormalCube(cube, position, markerType);

            case CubeType.Blue:
                return ProcessBlueCube(cube, position, markerType);

            case CubeType.Black:
                return ProcessBlackCube(cube, position, markerType);

            case CubeType.Reinforced:
                return ProcessReinforcedCube(cube, position, markerType);

            default:
                return false;
        }
    }

    
    private bool ProcessNormalCube(CubeBehavior cube, Vector2Int position, MarkerType markerType)
    {
        // Normal cubes are consumed in 1 hit regardless of marker type
        NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Normal));
        NotifyWaveManager(wm => wm.OnNonBlackCubeProcessed(CubeType.Normal, true));

        RemoveCubeFromWaveManager(cube);
        Destroy(cube.gameObject);

        Debug.Log($"Normal cube consumed at ({position.x}, {position.y})");
        return true;
    }

    private bool ProcessReinforcedCube(CubeBehavior cube, Vector2Int position, MarkerType markerType)
    {
        // Reinforced cubes require multiple hits
        bool isDestroyed = cube.TakeDamage(1);

        if (isDestroyed)
        {
            NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Reinforced));
            NotifyWaveManager(wm => wm.OnNonBlackCubeProcessed(CubeType.Reinforced, true));
            RemoveCubeFromWaveManager(cube);
            Destroy(cube.gameObject);
            Debug.Log($"Reinforced cube destroyed at ({position.x}, {position.y})");
            return true;
        }
        else
        {
            Debug.Log($"Reinforced cube damaged at ({position.x}, {position.y}) - still alive");
            return false; // Not destroyed yet
        }
    }
    private bool ProcessBlueCube(CubeBehavior cube, Vector2Int position, MarkerType markerType)
    {
        NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Blue));
        NotifyWaveManager(wm => wm.OnNonBlackCubeProcessed(CubeType.Blue, true));

        // Create appropriate cube marker based on marker type used
        CubeMarkerType cubeMarkerType = DetermineCubeMarkerType(markerType);
        CreateCubeMarker(position, cubeMarkerType);

        RemoveCubeFromWaveManager(cube);
        Destroy(cube.gameObject);

        Debug.Log($"Blue cube consumed at ({position.x}, {position.y}), created {cubeMarkerType} cube marker");
        return true;
    }

    private bool ProcessBlackCube(CubeBehavior cube, Vector2Int position, MarkerType markerType)
    {
        // Black cubes cannot be captured
        Debug.Log($"Black cube at ({position.x}, {position.y}) cannot be captured");
        return false;
    }

    private CubeMarkerType DetermineCubeMarkerType(MarkerType markerType)
    {
        switch (markerType)
        {
            case MarkerType.Individual:
                return CubeMarkerType.Area; // Individual + Blue = Area cube marker

            case MarkerType.Area:
                return CubeMarkerType.Individual; // Area + Blue = Individual cube marker

            default:
                return CubeMarkerType.Individual;
        }
    }

    #endregion

    #region Helper Methods

    private bool IsWithinPerfectTimingWindow(float placementTime)
    {
        float timeSincePlacement = Time.time - placementTime;
        return timeSincePlacement <= perfectTimingWindow;
    }

    private bool CanPlaceMarkerAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.GetTileAt(position);
        return tile != null && tile.CanBeMarked;
    }

    public bool IsValidPosition(Vector2Int position)
    {
        return gridManager != null &&
               position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    public List<Vector2Int> GetAreaPositions(Vector2Int center, int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // For 2x2: we want the center to be bottom-left of the 2x2 area
        // So positions are: center, center+(1,0), center+(0,1), center+(1,1)
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
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

    private List<CubeBehavior> FindAllCubesAt(Vector2Int position)
    {
        List<CubeBehavior> cubes = new List<CubeBehavior>();

        if (waveManager != null)
        {
            foreach (CubeBehavior cube in waveManager.activeCubes)
            {
                if (cube != null && !cube.isDestroyed &&
                    cube.position.x == position.x && cube.position.y == position.y)
                {
                    cubes.Add(cube);
                }
            }
        }

        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
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

    private void RemoveCubeFromWaveManager(CubeBehavior cube)
    {
        
        if (cube.CanBeCaptured() && waveManager != null && cube.type != CubeType.Black)
        {
            waveManager.activeCubes.Remove(cube);
        }
    }

    private void NotifyWaveManager(System.Action<WaveManager> action)
    {
        if (waveManager != null) action(waveManager);
    }

    #endregion

    #region Visual Creation Methods

    public GameObject CreateIndividualMarkerVisual(Vector2Int position)
    {
        // Use tile highlighting instead of 3D objects
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Set tile to red highlight for individual marker
            SetTileHighlight(tile, Color.red, "Individual");
        }

        // Return a dummy object for compatibility
        GameObject dummy = new GameObject($"IndividualMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    public GameObject CreateAreaMarkerVisual(Vector2Int position)
    {
        // Area marker only highlights the CENTER tile when placed
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Set tile to green highlight for area marker (center only)
            SetTileHighlight(tile, Color.green, "Area");
        }

        // Return a dummy object for compatibility
        GameObject dummy = new GameObject($"AreaMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    private GameObject CreateCubeMarkerVisual(Vector2Int position, CubeMarkerType type)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color highlightColor = type == CubeMarkerType.Individual ? Color.magenta : Color.cyan;
            string markerName = type == CubeMarkerType.Individual ? "CubeIndividual" : "CubeArea";
            SetTileHighlight(tile, highlightColor, markerName);
        }

        // Return a dummy object for compatibility
        GameObject dummy = new GameObject($"CubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    private GameObject CreatePoweredCubeMarkerVisual(Vector2Int position, CubeMarkerType type)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Powered markers get brighter/more saturated colors
            Color baseColor = type == CubeMarkerType.Individual ? Color.magenta : Color.cyan;
            Color poweredColor = new Color(baseColor.r * 1.5f, baseColor.g * 1.5f, baseColor.b * 1.5f, 1f);
            SetTileHighlight(tile, poweredColor, "Powered" + (type == CubeMarkerType.Individual ? "Individual" : "Area"));
        }

        GameObject dummy = new GameObject($"PoweredCubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    private void SetTileHighlight(Tile tile, Color color, string markerType)
    {
        // Store the marker type on the tile for tracking
        // You might need to add a markerType field to the Tile class

        // Create a material with the highlight color
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = color;
            highlightMaterial.SetFloat("_Metallic", 0.3f);
            highlightMaterial.SetFloat("_Smoothness", 0.7f);

            // Enable emission for glow effect
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", color * 0.3f);

            renderer.material = highlightMaterial;
        }

        Debug.Log($"Set {markerType} marker highlight at ({tile.x}, {tile.y}) with color {color}");
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
                    // Reset tile highlight
                    Tile tile = gridManager.GetTileAt(x, y);
                    if (tile != null)
                    {
                        tile.ForceUpdateVisuals(); // Reset to original material
                    }
                }
            }

            Destroy(visual);
        }
    }

    #endregion

    #region Public Interface

    public void ClearAllMarkers()
    {
        // Clear individual markers
        while (individualMarkers.Count > 0)
        {
            var marker = individualMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        currentIndividualMarkers = 0;

        // Clear area markers
        while (areaMarkers.Count > 0)
        {
            var marker = areaMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                DestroyMarkerVisual(visual);
            }
        }
        currentAreaMarkers = 0;


        // Clear cube markers
        foreach (var cubeMarker in cubeMarkers)
        {
            DestroyMarkerVisual(cubeMarker.visualObject);
        }
        cubeMarkers.Clear();

        // Clear preview objects
        foreach (var preview in previewObjects)
        {
            if (preview != null) Destroy(preview);
        }
        previewObjects.Clear();
    }

    // Resource availability checks
    public bool CanPlaceIndividualMarkerCheck() => CanPlaceIndividualMarker();
    public bool CanPlaceAreaMarkerCheck() => CanPlaceAreaMarker();

    // Cooldown information
    public float GetIndividualMarkerCooldownRemaining() =>
        Mathf.Max(0f, individualMarkerCooldown - (Time.time - lastIndividualMarkerTime));
    public float GetAreaMarkerCooldownRemaining() =>
        Mathf.Max(0f, areaMarkerCooldown - (Time.time - lastAreaMarkerTime));

    // Statistics
    public int GetIndividualMarkersPlaced() => individualMarkersPlaced;
    public int GetAreaMarkersPlaced() => areaMarkersPlaced;
    public int GetCubeMarkersTriggered() => cubeMarkersTriggered;
    public int GetPerfectTimingHits() => perfectTimingHits;

    public int GetCurrentIndividualMarkers() => currentIndividualMarkers;
    public int GetCurrentAreaMarkers() => currentAreaMarkers;
    public int GetCurrentCubeMarkers() => cubeMarkers.Count;

    public void ResetStatistics()
    {
        individualMarkersPlaced = 0;
        areaMarkersPlaced = 0;
        cubeMarkersTriggered = 0;
        perfectTimingHits = 0;
    }

    #endregion

    #region Legacy Support Methods (for debugging)

    // Keep some legacy methods for compatibility with existing debug panels
    public int CubeMarkerCount => cubeMarkers.Count;
    public bool HasCubeMarkers() => cubeMarkers.Count > 0;
    public Vector2Int GetNextCubeMarker() => cubeMarkers.Count > 0 ? cubeMarkers[0].position : new Vector2Int(-1, -1);

    // Legacy placement methods
    public bool PlacePlayerMarkerAt(Vector2Int position) => PlaceIndividualMarker(position);
    public bool TriggerPlayerMarkerAt(Vector2Int position) => TriggerIndividualMarkerAt(position, new IndividualMarker(position, Time.time));
    public void CreateCubeMarker(Vector2Int position, DetonationType type = DetonationType.Standard)
    {
        CreateCubeMarker(position, CubeMarkerType.Area);
    }

    public void ShowAreaPreview(Vector2Int center)
    {
        // Legacy preview method - simplified
        Debug.Log($"Area preview at {center} (legacy method)");
    }

    public void ClearAllActions()
    {
        HideAreaPreview();

        // Clear individual markers
        while (individualMarkers.Count > 0)
        {
            var marker = individualMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        currentIndividualMarkers = 0;

        // Clear area markers
        while (areaMarkers.Count > 0)
        {
            var marker = areaMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                DestroyMarkerVisual(visual);
            }
        }
        currentAreaMarkers = 0;


        // Clear cube markers and reset any tile materials
        foreach (var cubeMarker in cubeMarkers)
        {
            if (IsValidPosition(cubeMarker.position))
            {
                Tile tile = gridManager.GetTileAt(cubeMarker.position);
                if (tile != null)
                {
                    tile.SetDetonationPoint(false);
                    tile.ForceUpdateVisuals();
                }
            }
            DestroyMarkerVisual(cubeMarker.visualObject);
        }
        cubeMarkers.Clear();

        // Clear preview objects
        foreach (var preview in previewObjects)
        {
            if (preview != null) Destroy(preview);
        }
        previewObjects.Clear();
        showingPreview = false;

        Debug.Log("All player actions cleared");
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



    private IEnumerator ShowMarkerTriggerEffect(Vector2Int position)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 0.1f);

        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = $"TriggerEffect_{position.x}_{position.y}";
        effect.transform.position = worldPos;
        effect.transform.localScale = Vector3.zero;

        Destroy(effect.GetComponent<Collider>());

        Renderer renderer = effect.GetComponent<Renderer>();
        

        // Expand and fade
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
}