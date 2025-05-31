using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;

public class PlayerActionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material flashMaterial;

    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);

    [Header("Input Settings")]
    [SerializeField] private KeyCode placeMarkerKey = KeyCode.Space;
    [SerializeField] private KeyCode triggerMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode triggerCubeMarkerKey = KeyCode.D;
    [SerializeField] private KeyCode previewKey = KeyCode.P;

    // Player Markers (placed by player manually)
    private Queue<Vector2Int> playerMarkerQueue = new Queue<Vector2Int>();

    // Cube Markers (from captured blue cubes)
    private List<Vector2Int> cubeMarkers = new List<Vector2Int>();
    private Dictionary<Vector2Int, DetonationType> cubeMarkerTypes = new Dictionary<Vector2Int, DetonationType>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();

    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    // Statistics
    private int markersPlaced = 0;
    private int markersTriggered = 0;
    private int detonationsUsed = 0;
    private int normalCubesCaptured = 0;
    private int blueCubesCaptured = 0;
    private int blackCubesCaptured = 0;

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
    }

    private void Update()
    {
        HandleAllInput();
    }

    private void OnDestroy()
    {
        ClearAllActions();
    }
    #endregion

    #region Initialization
    private void FindReferences()
    {
        if (gridManager == null)
        {
            gridManager = GridManager.Instance ?? FindObjectOfType<GridManager>();
        }

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (waveManager == null)
        {
            waveManager = FindObjectOfType<WaveManager>();
        }

        if (gridManager == null)
        {
            Debug.LogError("PlayerActionManager requires GridManager reference!");
            enabled = false;
        }
    }
    #endregion

    #region Input Handling
    private void HandleAllInput()
    {
        if (playerManager == null || !playerManager.IsAlive()) return;

        HandleMarkerPlacement();
        HandleMarkerTrigger();
        HandleCubeMarkerTrigger();
        HandleAreaPreview();
    }

    private void HandleMarkerPlacement()
    {
        if (!Input.GetKeyDown(placeMarkerKey)) return;

        Vector2Int playerPos = playerManager.currentTilePosition;

        if (HasPlayerMarkerAt(playerPos))
        {
            RemovePlayerMarkerAt(playerPos);
        }
        else
        {
            PlacePlayerMarkerAt(playerPos);
        }
    }

    private void HandleMarkerTrigger()
    {
        if (!Input.GetKeyDown(triggerMarkerKey)) return;

        TriggerNextPlayerMarker();
    }

    private void HandleCubeMarkerTrigger()
    {
        if (!Input.GetKeyDown(triggerCubeMarkerKey)) return;

        TriggerNextCubeMarker();
    }

    private void HandleAreaPreview()
    {
        if (Input.GetKey(previewKey))
        {
            if (cubeMarkers.Count > 0 && !showingPreview)
            {
                ShowAreaPreview(cubeMarkers[0]);
            }
        }
        else if (Input.GetKeyUp(previewKey))
        {
            HideAreaPreview();
        }
    }
    #endregion

    #region Player Markers (Manual Placement)
    public bool CanPlacePlayerMarkerAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        return tile != null && tile.CanBeMarked;
    }

    public bool PlacePlayerMarkerAt(Vector2Int position)
    {
        if (!CanPlacePlayerMarkerAt(position)) return false;

        // Check marker limits
        int markerLimit = GetMarkerLimit();
        if (playerMarkerQueue.Count >= markerLimit)
        {
            Debug.Log($"Marker limit reached: {playerMarkerQueue.Count}/{markerLimit}");
            return false;
        }

        Tile tile = gridManager.tiles[position.x, position.y];
        tile.PlaceMarker();
        playerMarkerQueue.Enqueue(position);

        markersPlaced++;
        Debug.Log($"Player marker placed at ({position.x}, {position.y}). Total: {playerMarkerQueue.Count}/{markerLimit}");

        NotifyWaveManager(wm => wm.OnMarkerPlaced());
        return true;
    }

    public bool RemovePlayerMarkerAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || !tile.HasMarker) return false;

        tile.ClearMarker();

        // Remove from queue (rebuild queue without this position)
        var tempQueue = new Queue<Vector2Int>();
        while (playerMarkerQueue.Count > 0)
        {
            var pos = playerMarkerQueue.Dequeue();
            if (pos != position)
            {
                tempQueue.Enqueue(pos);
            }
        }
        playerMarkerQueue = tempQueue;

        Debug.Log($"Player marker removed from ({position.x}, {position.y}). Remaining: {playerMarkerQueue.Count}");
        return true;
    }

    public bool HasPlayerMarkerAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        return tile != null && tile.HasMarker;
    }

    public bool TriggerNextPlayerMarker()
    {
        if (playerMarkerQueue.Count == 0)
        {
            Debug.Log("No player markers to trigger");
            return false;
        }

        Vector2Int markerPos = playerMarkerQueue.Dequeue();
        return TriggerPlayerMarkerAt(markerPos);
    }

    public bool TriggerPlayerMarkerAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null) return false;

        bool hadRealMarker = tile.HasMarker;

        // Find cube at this position
        CubeBehavior cubeAtPosition = FindCubeAt(position);

        if (cubeAtPosition != null)
        {
            ProcessCubeCapture(cubeAtPosition, position, hadRealMarker ? "player marker" : "simulated marker");
        }
        else
        {
            Debug.Log($"Marker triggered at ({position.x}, {position.y}) but no cube found");
        }

        // Only trigger real markers, simulate visual effect for non-marker tiles
        if (hadRealMarker)
        {
            tile.TriggerMarker();
            markersTriggered++;
            Debug.Log($"Real player marker triggered at ({position.x}, {position.y})");
        }
        else
        {
            // Simulate marker trigger effect for area captures
            StartCoroutine(SimulateMarkerTrigger(position));
            Debug.Log($"Simulated marker trigger at ({position.x}, {position.y})");
        }

        return true;
    }

    public void ClearAllPlayerMarkers()
    {
        while (playerMarkerQueue.Count > 0)
        {
            var pos = playerMarkerQueue.Dequeue();
            if (IsValidPosition(pos))
            {
                Tile tile = gridManager.tiles[pos.x, pos.y];
                if (tile != null && tile.HasMarker)
                {
                    tile.ClearMarker();
                }
            }
        }

        Debug.Log("Cleared all player markers");
    }

    public int GetPlayerMarkerCount() => playerMarkerQueue.Count;

    public List<Vector2Int> GetAllPlayerMarkerPositions() => playerMarkerQueue.ToList();
    #endregion

    #region Cube Markers (Area Effects from Blue Cube Captures)
    public void CreateCubeMarker(Vector2Int position, DetonationType type = DetonationType.Standard)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        if (!cubeMarkers.Contains(position))
        {
            cubeMarkers.Add(position);

            if (type == DetonationType.Standard)
            {
                type = GetAreaTypeFromGridWidth();
            }

            cubeMarkerTypes[position] = type;
            MarkTileAsCubeMarker(position);

            Debug.Log($"Cube marker created at {position} with type {type} (area: {GetAreaSize(type)}x{GetAreaSize(type)})");
        }
    }

    public bool TriggerNextCubeMarker()
    {
        if (cubeMarkers.Count <= 0)
        {
            Debug.Log("No cube markers to trigger");
            return false;
        }

        Vector2Int position = cubeMarkers[0];
        return TriggerCubeMarkerAt(position);
    }

    public bool TriggerCubeMarkerAt(Vector2Int center)
    {
        if (!IsValidPosition(center)) return false;

        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile == null) return false;

        HideAreaPreview();

        DetonationType type = cubeMarkerTypes.ContainsKey(center) ? cubeMarkerTypes[center] : GetAreaTypeFromGridWidth();
        int size = GetAreaSize(type);

        cubeMarkers.Remove(center);
        cubeMarkerTypes.Remove(center);
        ResetTileMaterial(centerTile);

        Debug.Log($"Cube marker triggered: {size}x{size} area at {center}");

        List<Vector2Int> affectedPositions = GetAreaPositions(center, size);
        Debug.Log($"Area positions: {string.Join(", ", affectedPositions)}");

        // Process each position in the area as if it had a player marker
        foreach (Vector2Int position in affectedPositions)
        {
            if (IsValidPosition(position))
            {
                TriggerPlayerMarkerAt(position);
            }
        }

        detonationsUsed++;
        NotifyWaveManager(wm => wm.OnDetonationUsed());
        return true;
    }

    private IEnumerator SimulateMarkerTrigger(Vector2Int position)
    {
        if (!IsValidPosition(position)) yield break;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null) yield break;

        // Create temporary marker visual effect
        GameObject tempMarker = CreateTempMarkerEffect(position);

        // Show red marker briefly
        yield return new WaitForSeconds(0.1f);

        // Make it "activate" like a real marker (yellow flash)
        if (tempMarker != null)
        {
            Renderer markerRenderer = tempMarker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                Material activeMaterial = new Material(Shader.Find("Standard"));
                activeMaterial.color = Color.yellow;
                activeMaterial.SetFloat("_Metallic", 0.2f);
                activeMaterial.SetFloat("_Smoothness", 0.8f);
                markerRenderer.material = activeMaterial;
            }
        }

        // Wait for activation effect
        yield return new WaitForSeconds(0.3f);

        // Clean up temp marker
        if (tempMarker != null)
        {
            Destroy(tempMarker);
        }
    }

    private GameObject CreateTempMarkerEffect(Vector2Int position)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 0.5f);

        GameObject tempMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tempMarker.name = $"TempMarker_{position.x}_{position.y}";
        tempMarker.transform.position = worldPos;
        tempMarker.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);

        // Remove collider
        Collider markerCollider = tempMarker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        // Set initial marker color (red like regular markers)
        Renderer markerRenderer = tempMarker.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            Material markerMaterial = new Material(Shader.Find("Standard"));
            markerMaterial.color = Color.red;
            markerMaterial.SetFloat("_Metallic", 0.2f);
            markerMaterial.SetFloat("_Smoothness", 0.8f);
            markerRenderer.material = markerMaterial;
        }

        return tempMarker;
    }

    public bool HasCubeMarkers() => cubeMarkers.Count > 0;
    public int CubeMarkerCount => cubeMarkers.Count;
    public Vector2Int GetNextCubeMarker() => cubeMarkers.Count > 0 ? cubeMarkers[0] : new Vector2Int(-1, -1);

    public bool GetCubeMarker(Vector2Int position)
    {
        return cubeMarkers.Contains(position);
    }
    #endregion

    #region Cube Capture Logic
    private void ProcessCubeCapture(CubeBehavior cube, Vector2Int position, string captureMethod)
    {
        Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) via {captureMethod}");

        switch (cube.type)
        {
            case CubeType.Normal:
                normalCubesCaptured++;
                NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Normal));
                NotifyWaveManager(wm => wm.OnNonBlackCubeProcessed(CubeType.Normal, true));
                Destroy(cube.gameObject);
                break;

            case CubeType.Blue:
                blueCubesCaptured++;
                NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Blue));
                NotifyWaveManager(wm => wm.OnNonBlackCubeProcessed(CubeType.Blue, true));
                CreateCubeMarker(position);
                Destroy(cube.gameObject);
                break;

            case CubeType.Black:
                blackCubesCaptured++;
                NotifyWaveManager(wm => wm.OnCubeCaptured(CubeType.Black));
                Tile tile = gridManager.tiles[position.x, position.y];
                if (tile != null)
                {
                    tile.BlackenTile();
                }
                break;
        }

        RemoveCubeFromWaveManager(cube);
    }

    private void CaptureCubesAt(Vector2Int position)
    {
        // This method is now redundant since TriggerPlayerMarkerAt handles everything
        // Keeping for backward compatibility but it just delegates
        TriggerPlayerMarkerAt(position);
    }

    private List<CubeBehavior> FindAllCubesAt(Vector2Int position)
    {
        List<CubeBehavior> cubes = new List<CubeBehavior>();

        // Check wave manager's active cubes
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

        // Check all scene cubes (for manually spawned ones)
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

    private CubeBehavior FindCubeAt(Vector2Int position)
    {
        var cubes = FindAllCubesAt(position);
        return cubes.Count > 0 ? cubes[0] : null;
    }

    private void RemoveCubeFromWaveManager(CubeBehavior cube)
    {
        if (waveManager != null)
        {
            waveManager.activeCubes.Remove(cube);
        }
    }
    #endregion

    #region Area Preview System
    public void ShowAreaPreview(Vector2Int center)
    {
        if (!cubeMarkers.Contains(center)) return;

        HideAreaPreview();

        DetonationType type = cubeMarkerTypes.ContainsKey(center) ? cubeMarkerTypes[center] : DetonationType.Standard;
        int size = GetAreaSize(type);

        List<Vector2Int> affectedPositions = GetAreaPositions(center, size);

        foreach (Vector2Int pos in affectedPositions)
        {
            if (IsValidPosition(pos))
            {
                GameObject preview = CreatePreviewMarker(pos);
                previewObjects.Add(preview);
            }
        }

        showingPreview = true;
        Debug.Log($"Showing area preview at {center} - {size}x{size} area affecting {affectedPositions.Count} tiles");
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

    private GameObject CreatePreviewMarker(Vector2Int position)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 0.1f);

        GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        preview.name = $"AreaPreview_{position.x}_{position.y}";
        preview.transform.position = worldPos;
        preview.transform.localScale = new Vector3(gridManager.TileSize * 0.9f, 0.1f, gridManager.TileSize * 0.9f);

        Destroy(preview.GetComponent<Collider>());

        Renderer renderer = preview.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMat = new Material(Shader.Find("Standard"));
            previewMat.color = areaPreviewColor;
            previewMat.SetFloat("_Mode", 3);
            previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMat.SetInt("_ZWrite", 0);
            previewMat.DisableKeyword("_ALPHATEST_ON");
            previewMat.EnableKeyword("_ALPHABLEND_ON");
            previewMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMat.renderQueue = 3000;
            renderer.material = previewMat;
        }

        return preview;
    }
    #endregion

    #region Public Interface
    public void ClearAllActions()
    {
        HideAreaPreview();
        ClearAllPlayerMarkers();

        foreach (Vector2Int position in cubeMarkers)
        {
            if (IsValidPosition(position))
            {
                Tile tile = gridManager.tiles[position.x, position.y];
                if (tile != null)
                {
                    ResetTileMaterial(tile);
                }
            }
        }

        cubeMarkers.Clear();
        cubeMarkerTypes.Clear();
        originalTileMaterials.Clear();
    }

    // Statistics
    public int GetMarkersPlaced() => markersPlaced;
    public int GetMarkersTriggered() => markersTriggered;
    public int GetDetonationsUsed() => detonationsUsed;
    public int GetNormalCubesCaptured() => normalCubesCaptured;
    public int GetBlueCubesCaptured() => blueCubesCaptured;
    public int GetBlackCubesCaptured() => blackCubesCaptured;
    public int GetTotalCubesCaptured() => normalCubesCaptured + blueCubesCaptured + blackCubesCaptured;

    public void ResetStatistics()
    {
        markersPlaced = 0;
        markersTriggered = 0;
        detonationsUsed = 0;
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        blackCubesCaptured = 0;
    }
    #endregion

    #region Helper Methods
    private int GetMarkerLimit()
    {
        if (waveManager != null)
        {
            int waveLimit = waveManager.MarkerChargeLimit();
            if (waveLimit > 0) return waveLimit;
        }
        return 2; // Default limit
    }

    private DetonationType GetAreaTypeFromGridWidth()
    {
        if (gridManager == null) return DetonationType.Small;

        int width = gridManager.Width;
        if (width <= 3) return DetonationType.Small;
        else if (width <= 5) return DetonationType.Standard;
        else return DetonationType.Large;
    }

    private int GetAreaSize(DetonationType type)
    {
        switch (type)
        {
            case DetonationType.Single: return 1;
            case DetonationType.Small: return 2;
            case DetonationType.Standard: return 3;
            case DetonationType.Large: return 5;
            default: return 2;
        }
    }

    private List<Vector2Int> GetAreaPositions(Vector2Int center, int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        int halfSize = size / 2;
        int startX, startY;

        if (size == 2)
        {
            startX = center.x;
            startY = center.y;
        }
        else
        {
            startX = center.x - halfSize;
            startY = center.y - halfSize;
        }

        for (int x = startX; x < startX + size; x++)
        {
            for (int y = startY; y < startY + size; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidPosition(pos))
                {
                    positions.Add(pos);
                }
            }
        }

        return positions;
    }

    private bool IsValidPosition(Vector2Int position)
    {
        return gridManager != null &&
               position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    private void MarkTileAsCubeMarker(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile != null)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (!originalTileMaterials.ContainsKey(tile))
                {
                    originalTileMaterials[tile] = renderer.material;
                }

                if (cubeMarkerMaterial != null)
                {
                    renderer.material = cubeMarkerMaterial;
                }
                else
                {
                    Material blueMaterial = new Material(Shader.Find("Standard"));
                    blueMaterial.color = Color.blue;
                    renderer.material = blueMaterial;
                }

                tile.SetDetonationPoint(true);
            }
        }
    }

    private void ResetTileMaterial(Tile tile)
    {
        if (tile == null) return;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && originalTileMaterials.ContainsKey(tile))
        {
            renderer.material = originalTileMaterials[tile];
            originalTileMaterials.Remove(tile);
            tile.SetDetonationPoint(false);
        }
    }

    private IEnumerator FlashTile(Tile tile)
    {
        if (tile == null) yield break;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material originalMaterial = renderer.material;

        // Create a bright flash material
        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = Color.white;
        flashMat.SetFloat("_Metallic", 0.5f);
        flashMat.SetFloat("_Smoothness", 0.9f);

        // Apply flash
        renderer.material = flashMat;

        yield return new WaitForSeconds(flashDuration * 0.3f);

        // Quick fade to yellow
        flashMat.color = Color.yellow;
        renderer.material = flashMat;

        yield return new WaitForSeconds(flashDuration * 0.4f);

        // Fade back to original
        if (tile != null && renderer != null)
        {
            renderer.material = originalMaterial;
        }

        // Clean up
        if (flashMat != null)
        {
            Destroy(flashMat);
        }
    }

    private void NotifyWaveManager(System.Action<WaveManager> action)
    {
        if (waveManager != null) action(waveManager);
    }
    #endregion
}