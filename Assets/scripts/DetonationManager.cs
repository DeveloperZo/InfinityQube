using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DetonationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material detonationPointMaterial;
    
    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color flashColor = Color.green;
    
    private List<Vector2Int> detonationPoints = new List<Vector2Int>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();

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
    }

    // In DetonationManager.cs, update RegisterDetonationPoint
    public void RegisterDetonationPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return; // Skip blackened tiles

        if (!detonationPoints.Contains(position))
        {
            detonationPoints.Add(position);
            MarkTileAsDetonationPoint(position);
            Debug.Log($"Detonation point registered at {position}");
        }
    }

    // Trigger the next available detonation (called from PlayerController)
    public void TriggerNextDetonation()
    {
        if (detonationPoints.Count <= 0) return;
        
        Vector2Int position = detonationPoints[0];
        PerformDetonation(position);
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

    // Clear all detonation points (e.g., at the end of a wave)
    public void ClearDetonationPoints()
    {
        foreach (Vector2Int position in detonationPoints)
        {
            if (IsValidPosition(position))
            {
                ResetTileMaterial(gridManager.tiles[position.x, position.y]);
            }
        }
        
        detonationPoints.Clear();
        originalTileMaterials.Clear();
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
            if (renderer != null && detonationPointMaterial != null)
            {
                // Store original material
                if (!originalTileMaterials.ContainsKey(tile))
                {
                    originalTileMaterials[tile] = renderer.material;
                }
                
                renderer.material = detonationPointMaterial;
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

    // Perform the actual 3x3 detonation effect
    // In DetonationManager.cs
    private void PerformDetonation(Vector2Int center)
    {
        if (!IsValidPosition(center)) return;

        // Get the tile and its charge level
        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile == null) return;

        // Remove this detonation point from the list
        detonationPoints.Remove(center);
        ResetTileMaterial(centerTile);

        // Get the charge level
        int chargeLevel = centerTile.DetonationCharges;

        // Determine detonation area based on charge level
        switch (chargeLevel)
        {
            case 3: // Third charge (highest) - 3x3 area
                Debug.Log($"Detonating 3x3 area at {center} (charge level 3)");
                // Process a 3x3 grid centered on the detonation point
                for (int x = center.x - 1; x <= center.x + 1; x++)
                {
                    for (int y = center.y - 1; y <= center.y + 1; y++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        if (IsValidPosition(position))
                        {
                            // Visual effect
                            StartCoroutine(FlashTile(gridManager.tiles[x, y]));

                            // Process cubes at this position
                            DetonateCubesAt(position);
                        }
                    }
                }
                break;

            case 2: // Second charge - 2x2 area
                Debug.Log($"Detonating 2x2 area at {center} (charge level 2)");
                // Process 2x2 grid
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
                            DetonateCubesAt(position);
                        }
                    }
                }
                break;

            case 1: // First charge - single tile
                Debug.Log($"Detonating single tile at {center} (charge level 1)");
                // Just detonate the center tile
                StartCoroutine(FlashTile(centerTile));
                DetonateCubesAt(center);
                break;
            default:
                Debug.Log($"Detonating 3x3 area at {center} (default)");
                // Process a 3x3 grid centered on the detonation point
                for (int x = center.x - 1; x <= center.x + 1; x++)
                {
                    for (int y = center.y - 1; y <= center.y + 1; y++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        if (IsValidPosition(position))
                        {
                            // Visual effect
                            StartCoroutine(FlashTile(gridManager.tiles[x, y]));

                            // Process cubes at this position
                            DetonateCubesAt(position);
                        }
                    }
                }
                break;
        }

        // Reduce the charge level after detonation
        centerTile.ReduceCharge();

        // If the tile still has charges, re-register it as a detonation point
        if (centerTile.HasCharges)
        {
            RegisterDetonationPoint(center);
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

        // More visible flash effect
        renderer.material.color = flashColor;

        // Optional: Add a temporary visual marker for debugging
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.5f, tile.transform.position.z);
        marker.transform.localScale = Vector3.one * 0.3f;
        marker.GetComponent<Renderer>().material.color = flashColor;
        Destroy(marker.GetComponent<Collider>()); // Remove collider to avoid physics issues

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

        if (marker != null)
        {
            Destroy(marker);
        }
    }

    // Detonate all cubes at a specific position
    private void DetonateCubesAt(Vector2Int position)
    {
        // Find all cubes at this position by checking their actual grid positions
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube == null) continue;

            // Debug the comparison to help identify positioning issues
            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                Debug.Log($"Found cube to detonate at ({position.x}, {position.y}) of type {cube.CubeType}");
                ProcessCubeDetonation(cube, position);
            }
        }

        // Additional debugging - log if no cubes were found
        if (FindObjectsOfType<CubeBehavior>().All(c => c.position.x != position.x || c.position.y != position.y))
        {
            Debug.Log($"No cubes found at position ({position.x}, {position.y}) to detonate");
        }
    }

    // Process a specific cube's detonation
    private void ProcessCubeDetonation(CubeBehavior cube, Vector2Int position)
    {
        if (cube.CubeType == Enumerations.CubeType.Black)
        {
            // Apply penalty for black cubes
            //DamageTile(position);
        }
        else
        {
            // Destroy other cube types
            Destroy(cube.gameObject);
        }
    }

    // Apply damage effect to a tile
    private void DamageTile(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;
        
        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile != null)
        {
            // Visual damage indication
            tile.transform.position = new Vector3(
                tile.transform.position.x, 
                -0.2f, 
                tile.transform.position.z);

            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }
        }
    }
}