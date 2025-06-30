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

    // Marker Collections - Four-tier system
    [SerializeField] public Queue<LightMarker> lightMarkers = new Queue<LightMarker>();
    [SerializeField] public Queue<HeavyMarker> heavyMarkers = new Queue<HeavyMarker>();
    [SerializeField] public Queue<PrimeMarker> primeMarkers = new Queue<PrimeMarker>();
    public List<CubeMarker> cubeMarkers = new List<CubeMarker>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;

    // Parent reference
    private PlayerActionManager actionManager;

    // Public accessors for new marker system
    public Queue<LightMarker> LightMarkers => lightMarkers;
    public Queue<HeavyMarker> HeavyMarkers => heavyMarkers;
    public Queue<PrimeMarker> PrimeMarkers => primeMarkers;
    


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
        /// <summary>Light cube marker: Basic targeting (formerly Individual)</summary>
        Light,
        /// <summary>Heavy cube marker: Enhanced targeting for recursion cubes (NEW)</summary>
        Heavy,
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
        /// <summary>Light marker: Basic targeting (formerly Individual)</summary>
        Light,
        /// <summary>Heavy marker: Enhanced marker for recursion cubes (NEW)</summary>
        Heavy,
        /// <summary>Prime marker: Area coverage marker (formerly Area)</summary>
        Prime,
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

    #region Light Markers

    public bool PlaceLightMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceLightMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new LightMarker(position, Time.time);
        marker.visualObject = CreateLightMarkerVisual(position);

        lightMarkers.Enqueue(marker);
        actionManager.ConsumeLightCharge();

        Debug.Log($"Light marker placed at ({position.x}, {position.y})");
        return true;
    }

    public bool RemoveLightMarkerAt(Vector2Int position)
    {
        var markersArray = lightMarkers.ToArray();
        var newQueue = new Queue<LightMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseLightMarker();
                removed = true;
                Debug.Log($"Removed light marker at ({position.x}, {position.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        lightMarkers = newQueue;
        return removed;
    }

    public bool HasLightMarkerAt(Vector2Int position)
    {
        return lightMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextLightMarker()
    {
        if (lightMarkers.Count == 0) return false;

        var marker = lightMarkers.Dequeue();
        actionManager.ReleaseLightMarker();

        return TriggerLightMarkerAt(marker.position, marker);
    }

    private bool TriggerLightMarkerAt(Vector2Int position, LightMarker marker)
    {
        var cubes = FindAllCubesAt(position);
        bool success = false;

        // Trigger audio event for marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
        TriggerMarkerAudioEvent(Enumerations.GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Light, marker);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "light", success, cubes.Count);
        }

        DestroyMarkerVisual(marker.visualObject);
        StartCoroutine(ShowMarkerTriggerEffect(position));

        return success;
    }



    #endregion

    #region Heavy Markers

    public bool PlaceHeavyMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceHeavyMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new HeavyMarker(position, Time.time);
        marker.visualObject = CreateHeavyMarkerVisual(position);

        heavyMarkers.Enqueue(marker);
        actionManager.ConsumeHeavyCharge();

        Debug.Log($"Heavy marker placed at ({position.x}, {position.y})");
        return true;
    }

    public bool RemoveHeavyMarkerAt(Vector2Int position)
    {
        var markersArray = heavyMarkers.ToArray();
        var newQueue = new Queue<HeavyMarker>();
        bool removed = false;

        foreach (var marker in markersArray)
        {
            if (marker.position == position && !removed)
            {
                DestroyMarkerVisual(marker.visualObject);
                actionManager.ReleaseHeavyMarker();
                removed = true;
                Debug.Log($"Removed heavy marker at ({position.x}, {position.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        heavyMarkers = newQueue;
        return removed;
    }

    public bool HasHeavyMarkerAt(Vector2Int position)
    {
        return heavyMarkers.Any(m => m.position == position);
    }

    public bool TriggerNextHeavyMarker()
    {
        if (heavyMarkers.Count == 0) return false;

        var marker = heavyMarkers.Dequeue();
        actionManager.ReleaseHeavyMarker();

        return TriggerHeavyMarkerAt(marker.position, marker);
    }

    private bool TriggerHeavyMarkerAt(Vector2Int position, HeavyMarker marker)
    {
        var cubes = FindAllCubesAt(position);
        bool success = false;

        // Trigger audio event for marker triggering
        Vector3 worldPosition = actionManager.GridManager.GridToWorldPosition(position.x, position.y);
        TriggerMarkerAudioEvent(Enumerations.GameAudioEvent.MarkerTriggered, worldPosition);

        foreach (var cube in cubes)
        {
            // Heavy markers are specifically designed for recursion cubes but work on all cube types
            success |= ProcessCubeCapture(cube, position, MarkerType.Heavy, marker);
        }

        if (success && IsWithinPerfectTimingWindow(marker.placementTime))
        {
            perfectTimingHits++;
            marker.isPerfectTiming = true;
        }

        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerTriggered(position, "heavy", success, cubes.Count);
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

        primeMarkers.Enqueue(newMarker);
        actionManager.ConsumePrimeCharge();

        Debug.Log($"Prime marker placed at ({centerPosition.x}, {centerPosition.y})");
        return true;
    }

    public bool RemovePrimeMarkerAt(Vector2Int centerPosition)
    {
        var markersArray = primeMarkers.ToArray();
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
                removed = true;
                Debug.Log($"Removed prime marker at ({centerPosition.x}, {centerPosition.y})");
            }
            else
            {
                newQueue.Enqueue(marker);
            }
        }

        primeMarkers = newQueue;
        return removed;
    }

    public bool HasPrimeMarkerAt(Vector2Int centerPosition)
    {
        return primeMarkers.Any(m => m.centerPosition == centerPosition);
    }

    public bool TriggerNextPrimeMarker()
    {
        if (primeMarkers.Count == 0) return false;

        var marker = primeMarkers.Dequeue();
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
        TriggerMarkerAudioEvent(Enumerations.GameAudioEvent.MarkerTriggered, centerWorldPosition);

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
        TriggerMarkerAudioEvent(Enumerations.GameAudioEvent.MarkerTriggered, worldPosition, 1.2f);
        
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
            success |= ProcessCubeCapture(cube, position, MarkerType.Light);
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
    private void TriggerMarkerAudioEvent(Enumerations.GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
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
            // Create cube marker based on the marker type that captured the prime cube
            CubeMarkerType cubeMarkerType = markerType switch
            {
                MarkerType.Light => CubeMarkerType.Light,
                MarkerType.Heavy => CubeMarkerType.Heavy,
                MarkerType.Prime => CubeMarkerType.Prime,
                _ => CubeMarkerType.Cube
            };
            CreateCubeMarker(position, cubeMarkerType);
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
        var allCubes = FindObjectsOfType<CubeManager>();
        var cubes = new List<CubeManager>();

        foreach (var cube in allCubes)
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

    #region Visual Creation Methods

    public GameObject CreateLightMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            SetTileHighlight(tile, Color.red, "Light");
        }

        GameObject dummy = new GameObject($"LightMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    public GameObject CreateHeavyMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            SetTileHighlight(tile, new Color(0.8f, 0.2f, 0.2f, 1f), "Heavy"); // Dark red for heavy markers
        }

        GameObject dummy = new GameObject($"HeavyMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }



    public GameObject CreatePrimeMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            SetTileHighlight(tile, Color.green, "Prime");
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
                CubeMarkerType.Light => Color.magenta,
                CubeMarkerType.Heavy => new Color(0.7f, 0.2f, 0.7f, 1f), // Dark magenta
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
                CubeMarkerType.Light => Color.magenta,
                CubeMarkerType.Heavy => new Color(0.7f, 0.2f, 0.7f, 1f),
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
        return !HasLightMarkerAt(position) && !HasHeavyMarkerAt(position) && !HasPrimeMarkerAt(position);
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

        while (lightMarkers.Count > 0)
        {
            var marker = lightMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        // Note: Light marker count managed by PlayerActionManager

        while (heavyMarkers.Count > 0)
        {
            var marker = heavyMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        // Note: Heavy marker count managed by PlayerActionManager

        while (primeMarkers.Count > 0)
        {
            var marker = primeMarkers.Dequeue();
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