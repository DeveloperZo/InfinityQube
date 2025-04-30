using UnityEngine;
using System.Collections.Generic;
using System;

public class DetonationManager : MonoBehaviour
{
    public GridManager gridManager;
    public Material detonationPointMaterial; // Green material for detonation points

    private List<Vector2Int> detonationPoints = new List<Vector2Int>();

    // Register a tile as a detonation point (from capturing a green cube)
    public void RegisterDetonationPoint(Vector2Int position)
    {
        if (!detonationPoints.Contains(position))
        {
            detonationPoints.Add(position);

            // Change tile appearance to indicate detonation point
            Tile tile = gridManager.tiles[position.x, position.y];
            if (tile != null)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null && detonationPointMaterial != null)
                {
                    renderer.material = detonationPointMaterial;
                }
            }

            Debug.Log($"Detonation point registered at {position}");
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

        // Remove this detonation point first
        detonationPoints.Remove(center);

        // Reset tile appearance
        Tile centerTile = gridManager.tiles[center.x, center.y];
        if (centerTile != null)
        {
            Renderer renderer = centerTile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white; // Reset to default color
            }
        }

        // Affect a 3x3 area
        for (int x = center.x - 1; x <= center.x + 1; x++)
        {
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                if (x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
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
    private System.Collections.IEnumerator FlashTile(Tile tile)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;

            renderer.material.color = Color.green;
            yield return new WaitForSeconds(0.3f);
            renderer.material.color = originalColor;
        }
    }

    // Detonate cubes at a specific position
    private void DetonateCubesAt(Vector2Int position)
    {
        CubeBehavior[] allCubes = FindObjectsOfType<CubeBehavior>();

        foreach (var cube in allCubes)
        {
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
            Tile tile = gridManager.tiles[pos.x, pos.y];
            if (tile != null)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.white; // Reset to default color
                }
            }
        }

        detonationPoints.Clear();
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