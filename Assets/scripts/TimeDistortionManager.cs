// New class: TimeDistortionManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeDistortionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material distortionPointMaterial; // A blue material

    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color flashColor = new Color(0.5f, 0.7f, 1f); // Light blue
    [SerializeField] private float freezeDuration = 2f; // How many wave cycles to freeze

    private List<Vector2Int> distortionPoints = new List<Vector2Int>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();
    private Dictionary<Vector2Int, bool> slashDistortionPoints = new Dictionary<Vector2Int, bool>();


    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("TimeDistortionManager requires a GridManager reference!");
                enabled = false;
            }
        }
    }

    // Register a distortion point when a blue cube is captured
    public void RegisterDistortionPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return; // Skip blackened tiles

        if (!distortionPoints.Contains(position))
        {
            distortionPoints.Add(position);
            MarkTileAsDistortionPoint(position);
            Debug.Log($"Time distortion point registered at {position}");
        }
    }

    // Trigger the next available time distortion (called from PlayerController)
    public void TriggerNextDistortion()
    {
        if (distortionPoints.Count <= 0) return;

        Vector2Int position = distortionPoints[0];
        PerformTimeDistortion(position);
    }

    // Check if there are any available distortion points
    public bool HasDistortionPoints() => distortionPoints.Count > 0;

    // Get the number of available distortion points
    public int DistortionPointCount => distortionPoints.Count;

    // Get the position of the next distortion point
    public Vector2Int GetNextDistortionPoint()
    {
        return distortionPoints.Count > 0 ? distortionPoints[0] : new Vector2Int(-1, -1);
    }

    // Mark a tile as a distortion point
    private void MarkTileAsDistortionPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile != null)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null && distortionPointMaterial != null)
            {
                // Store original material
                if (!originalTileMaterials.ContainsKey(tile))
                {
                    originalTileMaterials[tile] = renderer.material;
                }

                renderer.material = distortionPointMaterial;
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
        }
    }

    // Clear all distortion points (e.g., at the end of a wave)
    public void ClearDistortionPoints()
    {
        foreach (Vector2Int position in distortionPoints)
        {
            if (IsValidPosition(position))
            {
                ResetTileMaterial(gridManager.tiles[position.x, position.y]);
            }
        }

        distortionPoints.Clear();
        originalTileMaterials.Clear();
    }
    public void RegisterSlashDistortionPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        if (!slashDistortionPoints.ContainsKey(position))
        {
            // Still add to regular distortion points for UI tracking
            if (!distortionPoints.Contains(position))
            {
                distortionPoints.Add(position);
            }

            slashDistortionPoints[position] = true;
            MarkTileAsDistortionPoint(position);
            Debug.Log($"Slash time distortion point registered at {position}");
        }
    }

    // Trigger all points in a slash pattern immediately
    public void TriggerSlashDistortion(Vector2Int center)
    {
        List<Vector2Int> slashPoints = new List<Vector2Int>();
        bool useForwardSlash = (center.x + center.y) % 2 == 0;

        // Identify the 3 points in the slash
        if (useForwardSlash) // / pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x + offset, center.y + offset);
                if (IsValidPosition(pos) && slashDistortionPoints.ContainsKey(pos))
                {
                    slashPoints.Add(pos);
                }
            }
        }
        else // \ pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x - offset, center.y + offset);
                if (IsValidPosition(pos) && slashDistortionPoints.ContainsKey(pos))
                {
                    slashPoints.Add(pos);
                }
            }
        }

        // Trigger distortion for each point in a special way (single tile, not 2x2)
        foreach (Vector2Int point in slashPoints)
        {
            PerformSingleTileDistortion(point);

            // Remove from tracking
            slashDistortionPoints.Remove(point);
            distortionPoints.Remove(point);
        }
    }

    // Special distortion that only affects the exact tile (not 2x2 area)
    private void PerformSingleTileDistortion(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        // Get the tile
        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null) return;

        // Reset the tile appearance
        ResetTileMaterial(tile);

        // Visual effect
        StartCoroutine(FlashTile(tile));

        // Process just this position
        FreezeOrConsumeCubesAt(position);
    }

    // Perform the actual 2x2 time distortion effect
    private void PerformTimeDistortion(Vector2Int center)
    {
        if (!IsValidPosition(center)) return;

        // Remove this distortion point from the list
        distortionPoints.Remove(center);

        // Get the tile
        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile == null) return;
        if (slashDistortionPoints.ContainsKey(center))
        {
            PerformSingleTileDistortion(center);
            slashDistortionPoints.Remove(center);
            distortionPoints.Remove(center);
            return;
        }
        ResetTileMaterial(centerTile);

        Debug.Log($"Performing 2x2 time distortion at {center}");

        // Process a 2x2 grid centered on the distortion point
        for (int x = center.x; x <= center.x + 1; x++)
        {
            for (int y = center.y; y <= center.y + 1; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (IsValidPosition(position))
                {
                    // Visual effect
                    StartCoroutine(FlashTile(gridManager.tiles[x, y]));

                    // Process cubes at this position
                    FreezeOrConsumeCubesAt(position);
                }
            }
        }
    }

    // Flash a tile temporarily
    private IEnumerator FlashTile(Tile tile)
    {
        if (tile == null) yield break;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material originalMaterial = renderer.material;
        Color originalColor = renderer.material.color;

        renderer.material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);

        // Only restore if the tile still exists
        if (tile != null && renderer != null)
        {
            if (originalTileMaterials.ContainsKey(tile))
            {
                renderer.material = originalTileMaterials[tile];
            }
            else
            {
                renderer.material = originalMaterial;
                renderer.material.color = originalColor;
            }
        }
    }

    // Process cubes at a specific position - freeze non-normals and consume normals
    private void FreezeOrConsumeCubesAt(Vector2Int position)
    {
        // Find all cubes at this position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube == null) continue;

            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                if (cube.CubeType == Enumerations.CubeType.Normal)
                {
                    // Consume normal cubes
                    Debug.Log($"Consuming normal cube at {position}");
                    Destroy(cube.gameObject);
                }
                else
                {
                    // Freeze non-normal cubes
                    Debug.Log($"Freezing {cube.CubeType} cube at {position}");
                    ApplyTimeFreeze(cube);
                }
            }
        }
    }

    // Apply time freeze effect to a cube
    private void ApplyTimeFreeze(CubeBehavior cube)
    {
        if (cube == null) return;

        // Add or update time freeze component
        TimeFrozenTag frozenTag = cube.GetComponent<TimeFrozenTag>();
        if (frozenTag == null)
        {
            frozenTag = cube.gameObject.AddComponent<TimeFrozenTag>();

            // Store original color
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                frozenTag.originalColor = cubeRenderer.material.color;

                // Set to blue tint
                cubeRenderer.material.color = new Color(0.7f, 0.8f, 1f);
            }
        }

        // Set freeze duration - how many wave cycles to skip
        frozenTag.frozenDuration = freezeDuration;

        // Show visual indicator
        StartCoroutine(PulseFreezeCube(cube, flashDuration * 2));
    }

    // Visual pulse effect for frozen cubes
    private IEnumerator PulseFreezeCube(CubeBehavior cube, float duration)
    {
        if (cube == null) yield break;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer == null) yield break;

        // Store original scale
        Vector3 originalScale = cube.transform.localScale;
        Vector3 pulseScale = originalScale * 1.2f;

        // Pulse outward
        float elapsed = 0f;
        while (elapsed < duration / 2)
        {
            if (cube == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (duration / 2));

            cube.transform.localScale = Vector3.Lerp(originalScale, pulseScale, t);

            yield return null;
        }

        // Pulse back
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            if (cube == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (duration / 2));

            cube.transform.localScale = Vector3.Lerp(pulseScale, originalScale, t);

            yield return null;
        }

        // Ensure original scale
        if (cube != null)
        {
            cube.transform.localScale = originalScale;
        }
    }

    // Helper method to check if a position is valid on the grid
    private bool IsValidPosition(Vector2Int position)
    {
        return gridManager != null &&
               position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }
}