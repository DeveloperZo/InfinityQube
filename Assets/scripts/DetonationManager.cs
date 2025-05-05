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
    [SerializeField] private Material detonationPointMaterial;
    [SerializeField] private Material flashMaterial;
    
    [Header("Effects")]
    [SerializeField] private float flashDuration = 0.3f;
    
    private List<Vector2Int> detonationPoints = new List<Vector2Int>();
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();
    private Dictionary<Vector2Int, bool> slashDetonationPoints = new Dictionary<Vector2Int, bool>();

    private Dictionary<Vector2Int, DetonationType> detonationTypes = new Dictionary<Vector2Int, DetonationType>();
    private List<Vector2Int> autoDetonationPoints = new List<Vector2Int>();

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
    public void RegisterDetonationPoint(Vector2Int position, DetonationType type = DetonationType.Standard, bool autoDetonate = false)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return; // Skip blackened tiles

        // Register the detonation point
        if (!detonationPoints.Contains(position))
        {
            detonationPoints.Add(position);
            detonationTypes[position] = type;
            if(!tile.IsAdvantaged)
                MarkTileAsDetonationPoint(position);

            // If this should auto-detonate on next wave movement, add to that list
            if (autoDetonate && !autoDetonationPoints.Contains(position))
            {
                autoDetonationPoints.Add(position);
            }

            Debug.Log($"Detonation point registered at {position} with type {type}, autoDetonate: {autoDetonate}");
        }
    }

    public void ProcessAutoDetonations()
    {
        if (autoDetonationPoints.Count <= 0) return;

        // Create a copy of the list to avoid issues when modifying during iteration
        List<Vector2Int> pointsToDetonate = new List<Vector2Int>(autoDetonationPoints);
        autoDetonationPoints.Clear();

        foreach (Vector2Int position in pointsToDetonate)
        {
            if (detonationPoints.Contains(position))
            {
                Debug.Log($"Auto-detonating at {position}");
                PerformDetonation(position);
            }
        }
    }

    // Trigger the next available detonation (called from PlayerController)
    public void TriggerNextDetonation(int x=-1, int y=-1)
    {
        if (detonationPoints.Count <= 0) return;
        
        Vector2Int position = detonationPoints[0];

        if(x>0 && y > 0)
        {
            var targetedPosition = detonationPoints.First(point => point.x == x  && point.y == y);
            if(targetedPosition != null)
            {
                position = targetedPosition;
            }
        }

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

    public void RegisterSlashDetonationPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        // Use Dictionary to track which points are from slash patterns
        if (!slashDetonationPoints.ContainsKey(position))
        {
            // Still add to regular detonation points for UI tracking
            if (!detonationPoints.Contains(position))
            {
                detonationPoints.Add(position);
            }

            slashDetonationPoints[position] = true;
            MarkTileAsDetonationPoint(position);
            Debug.Log($"Slash detonation point registered at {position}");
        }
    }

    // Trigger detonation for all points created in a slash pattern
    public void TriggerSlashDetonation(Vector2Int center)
    {
        // Get all detonation points that are part of this slash
        List<Vector2Int> slashPoints = new List<Vector2Int>();
        bool useForwardSlash = (center.x + center.y) % 2 == 0;

        // Identify the 3 points in the slash
        if (useForwardSlash) // / pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x + offset, center.y + offset);
                if (IsValidPosition(pos) && slashDetonationPoints.ContainsKey(pos))
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
                if (IsValidPosition(pos) && slashDetonationPoints.ContainsKey(pos))
                {
                    slashPoints.Add(pos);
                }
            }
        }

        // Trigger detonation for each point in a special way (single tile, not 3x3)
        foreach (Vector2Int point in slashPoints)
        {
            PerformSingleTileDetonation(point);

            // Remove from tracking
            slashDetonationPoints.Remove(point);
            detonationPoints.Remove(point);
        }
    }

    // Special detonation that only affects the exact tile (not 3x3 area)
    private void PerformSingleTileDetonation(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        // Get the tile
        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null) return;

        // Reset the tile appearance
        ResetTileMaterial(tile);

        // Visual effect - flash the tile
        StartCoroutine(FlashTile(tile));

        // Process cubes at this position only
        DetonateCubesAt(position);
    }

    // Perform the actual 3x3 detonation effect
    // In DetonationManager.cs
    // In DetonationManager.cs
    private void PerformDetonation(Vector2Int center)
    {
        if (!IsValidPosition(center)) return;

        // Get the tile
        Tile centerTile = gridManager.tiles[center.x, center.y];
        
        if (centerTile == null) return;
        // Reduce the charge level after detonation if the tile has charges
        if (centerTile.HasCharges)
        {
            centerTile.ReduceCharge();
        }


        // Remove this detonation point from the list
        
        if(!centerTile.HasCharges || !centerTile.IsAdvantaged)
        {
            detonationPoints.Remove(center);
            autoDetonationPoints.Remove(center); // Ensure it's removed from auto list too
        }

        ResetTileMaterial(centerTile);

        // Determine detonation size based on type or charge level
        int detonationSize = 2; // Default is now 2 (2x2 area)

        // If we have a specific type registered, use it
        if (detonationTypes.ContainsKey(center))
        {
            switch (detonationTypes[center])
            {
                case DetonationType.Standard:
                    detonationSize = 3; // 3x3 area
                    break;
                case DetonationType.Small:
                    detonationSize = 2; // 2x2 area
                    break;
                case DetonationType.Single:
                    detonationSize = 1; // Just this tile
                    break;
            }
            if (!centerTile.HasCharges || !centerTile.IsAdvantaged)
            {
                // Remove from tracking
                detonationTypes.Remove(center);
            }
        }
        // Otherwise use the tile's charge level (if any)
        else if (centerTile.HasCharges)
        {
            // Use charge level to determine size (detonation charges will be max 2 by default)
            detonationSize = centerTile.DetonationCharges;
        }

        Debug.Log($"Detonating {detonationSize}x{detonationSize} area at {center}");

        // For 2x2 detonation, the center tile is the bottom-left corner
        int startX = center.x;
        int startY = center.y;

        if(detonationSize == 3)
        {
            startX--;
            startY--;
        }

        // Process the detonation area
        for (int x = startX; x < startX + detonationSize; x++)
        {
            for (int y = startY; y < startY + detonationSize; y++)
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
    }

    // Flash a tile temporarily
    private IEnumerator FlashTile(Tile tile)
    {
        if (tile == null) yield break;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material originalMaterial = renderer.material;

        // More visible flash effect
        renderer.material = flashMaterial;

        // Optional: Add a temporary visual marker for debugging
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.5f, tile.transform.position.z);
        marker.transform.localScale = Vector3.one * 0.3f;
        marker.GetComponent<Renderer>().material = flashMaterial;
        Destroy(marker.GetComponent<Collider>()); // Remove collider to avoid physics issues

        yield return new WaitForSeconds(flashDuration);

        // Only restore if the tile still exists
        if (tile != null && renderer != null)
        {
            renderer.material = originalMaterial;
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
        else if(cube.CubeType == CubeType.Green)
        {
            RegisterDetonationPoint(position, DetonationType.Standard);
            Destroy(cube.gameObject);
        }
        else
        {
            // Destroy other cube types
            Destroy(cube.gameObject);
        }
    }

    public bool GetDetonationPoint(Vector2Int position)
    {
        return detonationPoints.Any(point => point == position);
    }
}