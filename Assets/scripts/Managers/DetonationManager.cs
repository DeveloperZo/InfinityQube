using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
using System;

public class DetonationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Material detonationPointMaterial;
    [SerializeField] private Material flashMaterial;

    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color detonationPreviewColor = new Color(1f, 0.5f, 0f, 0.7f); // Orange preview

    private List<Vector2Int> detonationPoints = new List<Vector2Int>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();
    private Dictionary<Vector2Int, DetonationType> detonationTypes = new Dictionary<Vector2Int, DetonationType>();
    private List<Vector2Int> autoDetonationPoints = new List<Vector2Int>();

    // Preview system for showing detonation area
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("DetonationManager requires a GridManager reference!");
                enabled = false;
            }
        }

        waveManager = FindObjectOfType<WaveManager>();
    }

    public void RegisterDetonationPoint(Vector2Int position, DetonationType type = DetonationType.Standard, bool autoDetonate = false)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        // Register the detonation point
        if (!detonationPoints.Contains(position))
        {
            detonationPoints.Add(position);

            // Determine detonation type based on grid width if not specified
            if (type == DetonationType.Standard)
            {
                type = GetDetonationTypeFromGridWidth();
            }

            detonationTypes[position] = type;

            Debug.Log($"Detonation point registered at {position} with type {type} (area: {GetDetonationSize(type)}x{GetDetonationSize(type)}) - Ready for manual trigger");
        }
    }

    // Determine detonation type based on grid width
    private DetonationType GetDetonationTypeFromGridWidth()
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

    // Get detonation size from type
    private int GetDetonationSize(DetonationType type)
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


    // Show preview of detonation area when player hovers over detonation point
    public void ShowDetonationPreview(Vector2Int center)
    {
        if (!detonationPoints.Contains(center)) return;

        HideDetonationPreview(); // Clear any existing preview

        DetonationType type = detonationTypes.ContainsKey(center) ? detonationTypes[center] : DetonationType.Standard;
        int size = GetDetonationSize(type);

        List<Vector2Int> affectedPositions = GetDetonationArea(center, size);

        foreach (Vector2Int pos in affectedPositions)
        {
            if (IsValidPosition(pos))
            {
                GameObject preview = CreatePreviewMarker(pos);
                previewObjects.Add(preview);
            }
        }

        showingPreview = true;
        Debug.Log($"Showing detonation preview at {center} - {size}x{size} area affecting {affectedPositions.Count} tiles");
    }

    public void HideDetonationPreview()
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
        preview.name = $"DetonationPreview_{position.x}_{position.y}";
        preview.transform.position = worldPos;
        preview.transform.localScale = new Vector3(gridManager.TileScale * 0.9f, 0.1f, gridManager.TileScale * 0.9f);

        // Remove collider
        Destroy(preview.GetComponent<Collider>());

        // Set preview material
        Renderer renderer = preview.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMat = new Material(Shader.Find("Standard"));
            previewMat.color = detonationPreviewColor;
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

    // Calculate affected area for detonation
    private List<Vector2Int> GetDetonationArea(Vector2Int center, int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // Calculate start position (center the detonation area)
        int halfSize = size / 2;
        int startX = center.x - halfSize;
        int startY = center.y - halfSize;

        // For even-sized areas, adjust to make center the bottom-left of the center 2x2
        if (size % 2 == 0)
        {
            // No adjustment needed - center is bottom-left
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

    // Trigger the next available detonation
    public void TriggerNextDetonation(int x = -1, int y = -1)
    {
        if (detonationPoints.Count <= 0) return;

        Vector2Int position = detonationPoints[0];

        if (x >= 0 && y >= 0)
        {
            var targetedPosition = detonationPoints.FirstOrDefault(point => point.x == x && point.y == y);
            if (targetedPosition != Vector2Int.zero)
            {
                position = targetedPosition;
            }
        }

        PerformDetonation(position);
    }

    private void PerformDetonation(Vector2Int center)
    {
        if (!IsValidPosition(center)) return;

        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile == null) return;

        // Hide any preview
        HideDetonationPreview();

        // Get detonation type BEFORE removing from dictionary
        DetonationType type = detonationTypes.ContainsKey(center) ? detonationTypes[center] : GetDetonationTypeFromGridWidth();
        int size = GetDetonationSize(type);

        // Remove from detonation points list (detonation is consumed)
        detonationPoints.Remove(center);
        detonationTypes.Remove(center);

        // Reset the tile from primed state
        centerTile.ResetPrimedState();

        Debug.Log($"Player-triggered detonation: {size}x{size} area at {center}");

        // Get all affected positions
        List<Vector2Int> affectedPositions = GetDetonationArea(center, size);

        // Process each position in the detonation area
        foreach (Vector2Int position in affectedPositions)
        {
            if (IsValidPosition(position))
            {
                // Visual effect
                StartCoroutine(FlashTile(gridManager.tiles[position.x, position.y]));

                // Capture/process cubes at this position
                CaptureCubesAt(position);
            }
        }

        // Notify player manager about detonation use
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnDetonationUsed();
        }

        // Notify wave manager about detonation use
        if (waveManager != null)
        {
            waveManager.OnDetonationUsed();
        }
    }

    // Modified to be "capture" instead of "detonate" - cubes are captured, not destroyed
    private void CaptureCubesAt(Vector2Int position)
    {
        List<CubeBehavior> cubesAtPosition = new List<CubeBehavior>();

        // Find all cubes at this position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube == null || cube.isDestroyed) continue;

            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                cubesAtPosition.Add(cube);
            }
        }

        // Process each cube found
        foreach (CubeBehavior cube in cubesAtPosition)
        {
            Debug.Log($"Capturing {cube.type} cube at ({position.x}, {position.y}) via detonation");
            ProcessCubeCapture(cube, position);
        }

        if (cubesAtPosition.Count == 0)
        {
            Debug.Log($"No cubes found at position ({position.x}, {position.y}) to capture");
        }
    }

    // Process cube capture (similar to marker triggering)
    private void ProcessCubeCapture(CubeBehavior cube, Vector2Int position)
    {
        // Notify player of capture
        PlayerManager player = FindObjectOfType<PlayerManager>();

        switch (cube.type)
        {
            case Enumerations.CubeType.Normal:
                if (player != null) player.OnCubeCaptured(Enumerations.CubeType.Normal);
                Destroy(cube.gameObject);
                Debug.Log($"Normal cube captured at ({position.x}, {position.y})");
                break;

            case Enumerations.CubeType.Blue:
                if (player != null) player.OnCubeCaptured(Enumerations.CubeType.Blue);
                // Blue cubes can create new detonation points when captured
                RegisterDetonationPoint(position, DetonationType.Standard);
                Destroy(cube.gameObject);
                Debug.Log($"Blue cube captured at ({position.x}, {position.y}), new detonation point created");
                break;

            case Enumerations.CubeType.Black:
                if (player != null) player.OnCubeCaptured(Enumerations.CubeType.Black);
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

    // Check if there are any available detonation points
    public bool HasDetonationPoints() => detonationPoints.Count > 0;

    // Get the number of available detonation points
    public int DetonationPointCount => detonationPoints.Count;

    // Get the position of the next detonation point
    public Vector2Int GetNextDetonationPoint()
    {
        return detonationPoints.Count > 0 ? detonationPoints[0] : new Vector2Int(-1, -1);
    }

    // Clear all detonation points
    // Clear all detonation points
    public void ClearDetonationPoints()
    {
        HideDetonationPreview();

        foreach (Vector2Int position in detonationPoints)
        {
            if (IsValidPosition(position))
            {
                Tile tile = gridManager.tiles[position.x, position.y];
                if (tile != null)
                {
                    tile.ResetPrimedState();
                }
            }
        }

        detonationPoints.Clear();
        autoDetonationPoints.Clear();
        originalTileMaterials.Clear();
        detonationTypes.Clear();
    }

    // Helper method to check if a position is valid on the grid
    private bool IsValidPosition(Vector2Int position)
    {
        return gridManager != null &&
               position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    // Mark a tile as a detonation point
    private void MarkTileAsDetonationPoint(Vector2Int position)
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

                // Set blue material to match captured blue cube
                if (detonationPointMaterial != null)
                {
                    renderer.material = detonationPointMaterial;
                }
                else
                {
                    // Fallback: create a blue material
                    Material blueMaterial = new Material(Shader.Find("Standard"));
                    blueMaterial.color = Color.blue;
                    renderer.material = blueMaterial;
                }

                // Mark tile as having a detonation point to prevent highlight override
                tile.SetDetonationPoint(true);
            }
        }
    }

    // Reset a tile's material to its original
    private void ResetTileMaterial(Tile tile)
    {
        if (tile == null) return;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && originalTileMaterials.ContainsKey(tile))
        {
            renderer.material = originalTileMaterials[tile];
            originalTileMaterials.Remove(tile);

            // Remove detonation point flag
            tile.SetDetonationPoint(false);
        }
    }

    // Flash a tile temporarily
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

    public bool GetDetonationPoint(Vector2Int position)
    {
        return detonationPoints.Any(point => point == position);
    }



}