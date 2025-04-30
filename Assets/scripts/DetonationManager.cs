using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
                return;
            }
        }
    }

    // Register a tile as a detonation point (from capturing a green cube)
    public void RegisterDetonationPoint(Vector2Int position)
    {
        if (gridManager == null) return;
        
        // Validate position
        if (position.x < 0 || position.x >= gridManager.Width || 
            position.y < 0 || position.y >= gridManager.Height)
        {
            Debug.LogWarning($"Invalid detonation point position: {position}");
            return;
        }
        
        if (!detonationPoints.Contains(position))
        {
            detonationPoints.Add(position);

            // Change tile appearance to indicate detonation point
            Tile tile = gridManager.tiles[position.x, position.y];
            if (tile != null)
            {
                MarkTileAsDetonationPoint(tile);
                Debug.Log($"Detonation point registered at {position}");
            }
        }
    }

    private void MarkTileAsDetonationPoint(Tile tile)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && detonationPointMaterial != null)
        {
            // Store original material if not already stored
            if (!originalTileMaterials.ContainsKey(tile))
            {
                originalTileMaterials[tile] = renderer.material;
            }
            
            renderer.material = detonationPointMaterial;
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
        }
    }

    // Trigger detonation at a specific position
    public bool TriggerDetonation(Vector2Int position)
    {
        if (detonationPoints.Contains(position))
        {
            PerformDetonation(position);
            return true;
        }
        return false;
    }

    // Perform the actual detonation effect
    private void PerformDetonation(Vector2Int center)
    {
        Debug.Log($"Detonating at {center}");

        // Remove this detonation point
        detonationPoints.Remove(center);

        // Reset tile appearance
        if (center.x >= 0 && center.x < gridManager.Width && 
            center.y >= 0 && center.y < gridManager.Height)
        {
            Tile centerTile = gridManager.tiles[center.x, center.y];
            if (centerTile != null)
            {
                ResetTileMaterial(centerTile);
            }
        }

        // Affect a 3x3 area
        for (int x = center.x - 1; x <= center.x + 1; x++)
        {
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                if (x >= 0 && x < gridManager.Width && y >= 0 && y < gridManager.Height)
                {
                    // Highlight the affected tile
                    Tile tile = gridManager.tiles[x, y];
                    if (tile != null)
                    {
                        StartCoroutine(FlashTile(tile));
                    }

                    // Find and process cubes at this position
                    DetonateCubesAt(new Vector2Int(x, y));
                }
            }
        }
    }

    // Visual flash effect for tiles
    private IEnumerator FlashTile(Tile tile)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            Material originalMaterial = renderer.material;

            renderer.material.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            
            // Only restore if the tile still exists
            if (tile != null && renderer != null)
            {
                // Check if we have the original material stored
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
    }

    // Detonate cubes at a specific position
    private void DetonateCubesAt(Vector2Int position)
    {
        CubeBehavior[] allCubes = FindObjectsOfType<CubeBehavior>();

        foreach (var cube in allCubes)
        {
            if (cube == null) continue;
            
            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                if (cube.CubeType == Enumerations.CubeType.Black)
                {
                    // Penalty for black cubes
                    DamageTile(position.x, position.y);
                    Debug.Log("Black cube hit by detonation - penalty!");
                }
                else
                {
                    // Destroy other cube types
                    Destroy(cube.gameObject);
                }
            }
        }
    }

    // Apply damage effect to a tile
    private void DamageTile(int x, int y)
    {
        if (x < 0 || x >= gridManager.Width || y < 0 || y >= gridManager.Height)
            return;
            
        Tile tile = gridManager.tiles[x, y];
        if (tile != null)
        {
            // Visual indication of damage
            tile.transform.position = new Vector3(tile.transform.position.x, -0.2f, tile.transform.position.z);

            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.gray;
            }
        }
    }

    // Clear all detonation points
    public void ClearDetonationPoints()
    {
        // Reset tile appearances for all detonation points
        foreach (Vector2Int pos in detonationPoints)
        {
            if (pos.x >= 0 && pos.x < gridManager.Width && 
                pos.y >= 0 && pos.y < gridManager.Height)
            {
                Tile tile = gridManager.tiles[pos.x, pos.y];
                if (tile != null)
                {
                    ResetTileMaterial(tile);
                }
            }
        }

        detonationPoints.Clear();
        originalTileMaterials.Clear();
    }

    public bool HasDetonationPoints()
    {
        return detonationPoints.Count > 0;
    }

    public Vector2Int GetNextDetonationPoint()
    {
        if (detonationPoints.Count > 0)
        {
            return detonationPoints[0]; // Return the first detonation point
        }
        return new Vector2Int(-1, -1); // Indicate no points available
    }
}