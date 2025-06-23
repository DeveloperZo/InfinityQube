using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;

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

    // Marker Collections
    [SerializeField] public Queue<IndividualMarker> individualMarkers = new Queue<IndividualMarker>();
    [SerializeField] public Queue<AreaMarker> areaMarkers = new Queue<AreaMarker>();
    public List<CubeMarker> cubeMarkers = new List<CubeMarker>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;

    // Parent reference
    private PlayerActionManager actionManager;

    public Queue<IndividualMarker> IndividualMarkers => individualMarkers;
    public Queue<AreaMarker> AreaMarkers => areaMarkers;

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

    public enum CubeMarkerType
    {
        Individual,
        Area
    }

    public enum MarkerType
    {
        Individual,
        Area,
        CubeMarker
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

    #region Individual Markers

    public bool PlaceIndividualMarker(Vector2Int position)
    {
        if (!actionManager.CanPlaceIndividualMarker() || !IsValidPosition(position))
            return false;

        if (!CanPlaceMarkerAt(position))
            return false;

        var marker = new IndividualMarker(position, Time.time);
        marker.visualObject = CreateIndividualMarkerVisual(position);

        individualMarkers.Enqueue(marker);
        actionManager.ConsumeIndividualCharge();

        Debug.Log($"Individual marker placed at ({position.x}, {position.y})");
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
                actionManager.ReleaseIndividualMarker();
                removed = true;
                Debug.Log($"Removed individual marker at ({position.x}, {position.y})");
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
        actionManager.ReleaseIndividualMarker();

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

    public bool PlaceAreaMarker(Vector2Int centerPosition, int size)
    {
        if (!actionManager.CanPlaceAreaMarker() || !IsValidPosition(centerPosition))
            return false;

        if (!CanPlaceMarkerAt(centerPosition))
            return false;

        AreaMarker newMarker = new AreaMarker(centerPosition, size, Time.time);
        newMarker.affectedPositions = GetAreaPositions(centerPosition, size);
        GameObject visual = CreateAreaMarkerVisual(centerPosition);
        newMarker.visualObjects.Add(visual);

        areaMarkers.Enqueue(newMarker);
        actionManager.ConsumeAreaCharge();

        Debug.Log($"Area marker placed at ({centerPosition.x}, {centerPosition.y})");
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
                actionManager.ReleaseAreaMarker();
                removed = true;
                Debug.Log($"Removed area marker at ({centerPosition.x}, {centerPosition.y})");
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
        actionManager.ReleaseAreaMarker();

        return TriggerAreaMarkerAt(marker);
    }

    private bool TriggerAreaMarkerAt(AreaMarker marker)
    {
        bool anySuccess = false;

        Debug.Log($"Triggering area marker - expanding from center ({marker.centerPosition.x}, {marker.centerPosition.y}) to {marker.affectedPositions.Count} tiles");

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
            foreach (var cube in cubes)
            {
                anySuccess |= ProcessCubeCapture(cube, position, MarkerType.Area);
            }
            StartCoroutine(ShowMarkerTriggerEffect(position));
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

    public void CreateCubeMarker(Vector2Int position, CubeMarkerType type = CubeMarkerType.Area)
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

        var tempAreaMarker = new AreaMarker(cubeMarker.position, 3, Time.time);
        tempAreaMarker.affectedPositions = GetAreaPositions(cubeMarker.position, 3);
        return TriggerAreaMarkerAt(tempAreaMarker);
        
    }

    private bool TriggerSingleTileMarker(Vector2Int position)
    {
        var cubes = FindAllCubesAt(position);
        bool success = false;

        foreach (var cube in cubes)
        {
            success |= ProcessCubeCapture(cube, position, MarkerType.Individual);
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

    #region Cube Interaction System

    private bool ProcessCubeCapture(CubeManager cube, Vector2Int position, MarkerType markerType, IndividualMarker individualMarker = null)
    {
        if (cube == null || cube.isDestroyed) return false;

        if (!cube.CanBeCaptured())
        {
            Debug.Log($"Cube at ({position.x}, {position.y}) cannot be captured due to face status: {cube.GetActiveFaceStatus()}");
            return false;
        }

        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) with {markerType} marker");

        if (cube.type == CubeType.Blue)
        {
            CreateCubeMarker(position, markerType == MarkerType.Individual ? CubeMarkerType.Individual : CubeMarkerType.Area);
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
        if (cube.CanBeCaptured() && actionManager.WaveManager != null && cube.type != CubeType.Black)
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

    public GameObject CreateIndividualMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            SetTileHighlight(tile, Color.red, "Individual");
        }

        GameObject dummy = new GameObject($"IndividualMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    public GameObject CreateAreaMarkerVisual(Vector2Int position)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            SetTileHighlight(tile, Color.green, "Area");
        }

        GameObject dummy = new GameObject($"AreaMarker_{position.x}_{position.y}");
        dummy.transform.position = actionManager.GridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    private GameObject CreateCubeMarkerVisual(Vector2Int position, CubeMarkerType type)
    {
        Tile tile = actionManager.GridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color highlightColor = type == CubeMarkerType.Individual ? Color.magenta : Color.cyan;
            string markerName = type == CubeMarkerType.Individual ? "CubeIndividual" : "CubeArea";
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
            Color baseColor = type == CubeMarkerType.Individual ? Color.magenta : Color.cyan;
            Color poweredColor = new Color(baseColor.r * 1.5f, baseColor.g * 1.5f, baseColor.b * 1.5f, baseColor.a);
            string markerName = type == CubeMarkerType.Individual ? "PoweredCubeIndividual" : "PoweredCubeArea";
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
        return !HasIndividualMarkerAt(position) && !HasAreaMarkerAt(position);
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

        while (individualMarkers.Count > 0)
        {
            var marker = individualMarkers.Dequeue();
            DestroyMarkerVisual(marker.visualObject);
        }
        actionManager.currentIndividualMarkers = 0;

        while (areaMarkers.Count > 0)
        {
            var marker = areaMarkers.Dequeue();
            foreach (var visual in marker.visualObjects)
            {
                DestroyMarkerVisual(visual);
            }
        }
        actionManager.currentAreaMarkers = 0;

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