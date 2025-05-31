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
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material flashMaterial;

    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);

    // Cube Markers (from captured blue cubes)
    private List<Vector2Int> cubeMarkers = new List<Vector2Int>();
    private Dictionary<Vector2Int, DetonationType> cubeMarkerTypes = new Dictionary<Vector2Int, DetonationType>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();

    // Preview system for showing area effects
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("PlayerActionManager requires a GridManager reference!");
                enabled = false;
            }
        }

        waveManager = FindObjectOfType<WaveManager>();
    }

    #region Player Marker Actions (Single Tile)

    public bool CanPlacePlayerMarker(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        return tile != null && tile.CanBeMarked;
    }

    public bool PlacePlayerMarker(Vector2Int position)
    {
        if (!CanPlacePlayerMarker(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        tile.PlaceMarker();

        Debug.Log($"Player marker placed at ({position.x}, {position.y})");
        return true;
    }

    public bool TriggerPlayerMarker(Vector2Int position)
    {
        if (!IsValidPosition(position)) return false;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || !tile.HasMarker) return false;

        // Find cube at this position
        CubeBehavior cubeAtPosition = FindCubeAt(position);

        if (cubeAtPosition != null)
        {
            ProcessPlayerMarkerCubeCapture(cubeAtPosition, tile, position);
        }
        else
        {
            Debug.Log($"Player marker triggered at ({position.x}, {position.y}) but no cube found");
        }

        // Trigger the marker (handles visual feedback and cleanup)
        tile.TriggerMarker();
        return true;
    }

    private void ProcessPlayerMarkerCubeCapture(CubeBehavior cube, Tile tile, Vector2Int position)
    {
        Debug.Log($"Player marker capturing {cube.type} cube at ({position.x}, {position.y})");

        switch (cube.type)
        {
            case CubeType.Normal:
                // Normal cubes are simply captured
                NotifyPlayerCubeCapture(CubeType.Normal);
                Destroy(cube.gameObject);
                break;

            case CubeType.Blue:
                // Blue cubes create cube markers when captured
                NotifyPlayerCubeCapture(CubeType.Blue);
                CreateCubeMarker(position);
                Destroy(cube.gameObject);
                break;

            case CubeType.Black:
                // Black cubes corrupt the tile and remain
                NotifyPlayerCubeCapture(CubeType.Black);
                tile.BlackenTile();
                // Black cube remains (not destroyed)
                break;
        }
    }

    #endregion

    #region Cube Markers (Area Effects)

    public void CreateCubeMarker(Vector2Int position, DetonationType type = DetonationType.Standard)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        // Register the cube marker
        if (!cubeMarkers.Contains(position))
        {
            cubeMarkers.Add(position);

            // Determine area type based on grid width if not specified
            if (type == DetonationType.Standard)
            {
                type = GetAreaTypeFromGridWidth();
            }

            cubeMarkerTypes[position] = type;

            // Mark the tile visually
            MarkTileAsCubeMarker(position);

            Debug.Log($"Cube marker created at {position} with type {type} (area: {GetAreaSize(type)}x{GetAreaSize(type)})");
        }
    }

    public void TriggerNextCubeMarker(int x = -1, int y = -1)
    {
        if (cubeMarkers.Count <= 0) return;

        Vector2Int position = cubeMarkers[0];

        if (x >= 0 && y >= 0)
        {
            var targetedPosition = cubeMarkers.FirstOrDefault(point => point.x == x && point.y == y);
            if (targetedPosition != Vector2Int.zero)
            {
                position = targetedPosition;
            }
        }

        TriggerCubeMarker(position);
    }

    private void TriggerCubeMarker(Vector2Int center)
    {
        if (!IsValidPosition(center)) return;

        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile == null) return;

        // Hide any preview
        HideAreaPreview();

        // Get area type BEFORE removing from dictionary
        DetonationType type = cubeMarkerTypes.ContainsKey(center) ? cubeMarkerTypes[center] : GetAreaTypeFromGridWidth();
        int size = GetAreaSize(type);

        // Remove from cube markers list (marker is consumed)
        cubeMarkers.Remove(center);
        cubeMarkerTypes.Remove(center);

        // Reset the tile appearance
        ResetTileMaterial(centerTile);

        Debug.Log($"Cube marker triggered: {size}x{size} area at {center}");

        // Get all affected positions
        List<Vector2Int> affectedPositions = GetAreaPositions(center, size);
        Debug.Log($"Area positions: {string.Join(", ", affectedPositions)}");

        // Process each position in the area
        foreach (Vector2Int position in affectedPositions)
        {
            if (IsValidPosition(position))
            {
                Debug.Log($"Processing area position: ({position.x}, {position.y})");

                // Visual effect
                StartCoroutine(FlashTile(gridManager.tiles[position.x, position.y]));

                // Capture cubes at this position
                CaptureCubesAt(position);
            }
        }

        // Notify managers about cube marker use
        NotifyPlayerActionUsed();
    }

    private void CaptureCubesAt(Vector2Int position)
    {
        List<CubeBehavior> cubesAtPosition = new List<CubeBehavior>();

        // Find all cubes at this position - check both WaveManager's active cubes and all CubeBehavior objects
        if (FindObjectOfType<WaveManager>() != null)
        {
            var waveManager = FindObjectOfType<WaveManager>();
            foreach (CubeBehavior cube in waveManager.activeCubes)
            {
                if (cube != null && !cube.isDestroyed &&
                    cube.position.x == position.x && cube.position.y == position.y)
                {
                    cubesAtPosition.Add(cube);
                }
            }
        }

        // Also check all CubeBehavior objects in scene (for manually spawned cubes)
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y &&
                !cubesAtPosition.Contains(cube))
            {
                cubesAtPosition.Add(cube);
            }
        }

        Debug.Log($"Found {cubesAtPosition.Count} cubes at position ({position.x}, {position.y})");

        // Process each cube found
        foreach (CubeBehavior cube in cubesAtPosition)
        {
            Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) via cube marker");
            ProcessCubeMarkerCapture(cube, position);

            // Remove from wave manager's active list
            if (FindObjectOfType<WaveManager>() != null)
            {
                var waveManager = FindObjectOfType<WaveManager>();
                waveManager.activeCubes.Remove(cube);
            }
        }

        if (cubesAtPosition.Count == 0)
        {
            Debug.Log($"No cubes found at position ({position.x}, {position.y}) to capture");
        }
    }

    private void ProcessCubeMarkerCapture(CubeBehavior cube, Vector2Int position)
    {
        switch (cube.type)
        {
            case CubeType.Normal:
                NotifyPlayerCubeCapture(CubeType.Normal);
                Destroy(cube.gameObject);
                Debug.Log($"Normal cube captured at ({position.x}, {position.y})");
                break;

            case CubeType.Blue:
                NotifyPlayerCubeCapture(CubeType.Blue);
                // Blue cubes can create new cube markers when captured by area effect
                CreateCubeMarker(position, DetonationType.Standard);
                Destroy(cube.gameObject);
                Debug.Log($"Blue cube captured at ({position.x}, {position.y}), new cube marker created");
                break;

            case CubeType.Black:
                NotifyPlayerCubeCapture(CubeType.Black);
                // Black cubes corrupt the tile when captured
                Tile tile = gridManager.tiles[position.x, position.y];
                if (tile != null)
                {
                    tile.BlackenTile();
                }
                // Black cube stays (not destroyed) - they're difficult to capture
                Debug.Log($"Black cube captured at ({position.x}, {position.y}), tile corrupted");
                break;
        }
    }

    #endregion

    #region Area Preview System

    public void ShowAreaPreview(Vector2Int center)
    {
        if (!cubeMarkers.Contains(center)) return;

        HideAreaPreview(); // Clear any existing preview

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

        // Remove collider
        Destroy(preview.GetComponent<Collider>());

        // Set preview material
        Renderer renderer = preview.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMat = new Material(Shader.Find("Standard"));
            previewMat.color = areaPreviewColor;
            previewMat.SetFloat("_Mode", 3); // Transparent mode
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

    public bool HasCubeMarkers() => cubeMarkers.Count > 0;
    public int CubeMarkerCount => cubeMarkers.Count;
    public Vector2Int GetNextCubeMarker() => cubeMarkers.Count > 0 ? cubeMarkers[0] : new Vector2Int(-1, -1);

    public void ClearAllActions()
    {
        HideAreaPreview();

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

    public bool GetCubeMarker(Vector2Int position)
    {
        return cubeMarkers.Any(point => point == position);
    }

    #endregion

    #region Helper Methods

    private DetonationType GetAreaTypeFromGridWidth()
    {
        if (gridManager == null) return DetonationType.Small;

        int width = gridManager.Width;

        if (width <= 3)
        {
            return DetonationType.Small; // 2x2
        }
        else if (width <= 5)
        {
            return DetonationType.Standard; // 3x3
        }
        else // width >= 7
        {
            return DetonationType.Large; // 5x5
        }
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

        // For 2x2 area, center should be bottom-left of the 2x2
        // For 3x3 area, center should be middle
        int halfSize = size / 2;
        int startX, startY;

        if (size == 2)
        {
            // For 2x2, treat center as bottom-left corner
            startX = center.x;
            startY = center.y;
        }
        else
        {
            // For odd sizes (3x3, 5x5), center the area
            startX = center.x - halfSize;
            startY = center.y - halfSize;
        }

        Debug.Log($"Area calculation: size={size}, center=({center.x},{center.y}), start=({startX},{startY})");

        for (int x = startX; x < startX + size; x++)
        {
            for (int y = startY; y < startY + size; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidPosition(pos))
                {
                    positions.Add(pos);
                    Debug.Log($"Added area position: ({x}, {y})");
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

    private CubeBehavior FindCubeAt(Vector2Int position)
    {
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                return cube;
            }
        }
        return null;
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
                // Store original material if not already stored
                if (!originalTileMaterials.ContainsKey(tile))
                {
                    originalTileMaterials[tile] = renderer.material;
                }

                // Set cube marker material
                if (cubeMarkerMaterial != null)
                {
                    renderer.material = cubeMarkerMaterial;
                }
                else
                {
                    // Fallback: create a blue material
                    Material blueMaterial = new Material(Shader.Find("Standard"));
                    blueMaterial.color = Color.blue;
                    renderer.material = blueMaterial;
                }

                // Mark tile as having a cube marker
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

            // Remove cube marker flag
            tile.SetDetonationPoint(false);
        }
    }

    private IEnumerator FlashTile(Tile tile)
    {
        if (tile == null) yield break;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material originalMaterial = renderer.material;

        if (flashMaterial != null)
        {
            renderer.material = flashMaterial;
        }

        yield return new WaitForSeconds(flashDuration);

        if (tile != null && renderer != null)
        {
            renderer.material = originalMaterial;
        }
    }

    private void NotifyPlayerCubeCapture(CubeType cubeType)
    {
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnCubeCaptured(cubeType);
        }
    }

    private void NotifyPlayerActionUsed()
    {
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnDetonationUsed(); // This could be renamed to OnActionUsed later
        }

        if (waveManager != null)
        {
            waveManager.OnDetonationUsed(); // This could be renamed to OnActionUsed later
        }
    }

    #endregion
}