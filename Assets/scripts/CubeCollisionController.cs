using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using static Enumerations;

public class CubeCollisionController : MonoBehaviour
{
    private GridManager grid;
    private DetonationManager detonationManager;

    public void Initialize(GridManager gridManager)
    {
        grid = gridManager;
        detonationManager = FindObjectOfType<DetonationManager>();
    }
    public void HandleCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        if (sourceCube == null || targetCube == null) return;

        // Get cube types for clarity
        Enumerations.CubeType sourceType = sourceCube.CubeType;
        Enumerations.CubeType targetType = targetCube.CubeType;

        Debug.Log($"Processing collision: {sourceType} cube colliding with {targetType} cube at ({position.x}, {position.y})");

        // If cubes are the same color, transform the tile
        if (sourceType == targetType && sourceType != Enumerations.CubeType.Normal)
        {
            // Transform the tile to this cube's color type
            Tile tile = grid.tiles[position.x, position.y];
            if (tile != null)
            {
                if (sourceType == Enumerations.CubeType.Black)
                {
                    // Black + Black = Blacken tile
                    BlackenTile(position);
                    Destroy(targetCube.gameObject);
                    return;
                }
                else if (sourceType == Enumerations.CubeType.Green)
                {
                    tile.TransformTile(sourceType);
                    detonationManager.RegisterDetonationPoint(new Vector2Int(tile.x, tile.y), DetonationType.Small);

                    // Consume both cubes after transformation
                    Destroy(sourceCube.gameObject);
                    Destroy(targetCube.gameObject);
                    return;
                }
            }
        }

        // Process based on the source cube type (assuming source is the falling/moving cube)
        switch (sourceType)
        {
            case Enumerations.CubeType.Black:
                HandleBlackCubeCollision(sourceCube, targetCube, position);
                break;
            case Enumerations.CubeType.Green:
                HandleGreenCubeCollision(sourceCube, targetCube, position);
                break;
            case Enumerations.CubeType.Normal:
                HandleNormalCubeCollision(sourceCube, targetCube, position);
                break;
        }
    }

    private void HandleBlackCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Black + Black = Blacken tile
                BlackenTile(position);
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                // Black + Green = Mark the tile for 2x2 auto-detonation
                MarkTileForAutoDetonation(position);
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Black + Normal = Consume normal
                Destroy(targetCube.gameObject);
                break;
        }

        // If landing on an enhanced tile, reduce its charge by one
        if (grid != null && IsValidPosition(position))
        {
            Tile tile = grid.tiles[position.x, position.y];
            if (tile != null && tile.HasCharges)
            {
                tile.ReduceCharge();
                Debug.Log($"Black cube landing reduced charge level on tile at ({position.x}, {position.y})");
            }
        }
    }

    private void HandleGreenCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Green + Black = Green consumed, triggers detonation
                if (FindObjectOfType<DetonationManager>() != null)
                {
                    FindObjectOfType<DetonationManager>().RegisterDetonationPoint(position, DetonationType.Small);
                    FindObjectOfType<DetonationManager>().TriggerNextDetonation();
                }
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                // Green + Green = Enhanced green tile and create detonation mark
                EnhanceGreenTile(position);
                // Register a detonation point at the tile position
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(position, DetonationType.Standard);
                }
                Destroy(targetCube.gameObject);
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Green + Normal = Consume normal and create detonation mark
                Destroy(targetCube.gameObject);
                // Register a detonation point at the tile position
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(position, DetonationType.Small);
                }
                break;
        }
    }

    private void HandleNormalCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Normal + Black = Normal consumed
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                // Normal + Green = Normal consumed
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Normal + Normal = Both consumed
                Destroy(sourceCube.gameObject);
                Destroy(targetCube.gameObject);
                break;
        }
    }

    private void BlackenTile(Vector2Int position)
    {
        if (grid == null || !IsValidPosition(position)) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            Debug.Log($"Black cube collision at ({position.x}, {position.y}). Blackening tile.");
            tile.BlackenTile();
        }
    }

    private void MarkTileForAutoDetonation(Vector2Int position)
    {
        if (grid == null || !IsValidPosition(position)) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            // Mark the tile (visually like a player marker)
            tile.PlaceMarker();

            // Register with DetonationManager for auto-detonation
            DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
            if (detonationManager != null)
            {
                detonationManager.RegisterDetonationPoint(
                    position,
                    DetonationType.Small, // 2x2 area
                    true // Auto-detonate on next move
                );

                Debug.Log($"Marked tile at ({position.x}, {position.y}) for 2x2 auto-detonation on next move");
            }
        }
    }

    private void EnhanceGreenTile(Vector2Int position)
    {
        if (grid == null || !IsValidPosition(position)) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            // First ensure it's transformed to green
            tile.TransformTile(Enumerations.CubeType.Green);


            // Register with detonation manager
            DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
            if (detonationManager != null)
            {
                detonationManager.RegisterDetonationPoint(position);
            }
        }
    }

    private IEnumerator VisualizeSquarePattern(Vector2Int center, int radius, Enumerations.CubeType type)
    {
        // Create visualization objects for the pattern
        List<GameObject> markers = new List<GameObject>();
        Color markerColor = (type == Enumerations.CubeType.Green) ?
            new Color(0, 1, 0, 0.5f) : new Color(0, 0.7f, 1f, 0.5f);

        // Create markers for each position in the square
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidPosition(pos))
                {
                    GameObject marker = CreateMarker(pos, markerColor);
                    markers.Add(marker);
                }
            }
        }

        // Let markers stay visible for a second
        yield return new WaitForSeconds(1f);

        // Destroy markers
        foreach (GameObject marker in markers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
    }

    private GameObject CreateMarker(Vector2Int position, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = new Vector3(position.x, 1.5f, position.y);
        marker.transform.localScale = Vector3.one * 0.4f;

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        // Remove collider to avoid physics interference
        Destroy(marker.GetComponent<Collider>());

        return marker;
    }

    private IEnumerator PulseTileColor(Renderer renderer, Color originalColor, Color pulseColor, float duration)
    {
        if (renderer == null) yield break;

        float elapsed = 0f;

        // Pulse to new color
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (duration / 2));

            renderer.material.color = Color.Lerp(originalColor, pulseColor, t);

            yield return null;
        }

        // Return to original color
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (duration / 2));

            renderer.material.color = Color.Lerp(pulseColor, originalColor, t);

            yield return null;
        }

        // Ensure final color
        renderer.material.color = originalColor;
    }

    private bool IsValidPosition(Vector2Int position)
    {
        return grid != null &&
               position.x >= 0 && position.x < grid.Width &&
               position.y >= 0 && position.y < grid.Height;
    }

    private void CreateDebugMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.SetParent(transform);
        marker.transform.localPosition = Vector3.up * 0.5f;
        marker.transform.localScale = Vector3.one * 0.2f;
        marker.GetComponent<Renderer>().material.color = Color.red;

        // Remove collider to avoid physics issues
        Destroy(marker.GetComponent<Collider>());

        // Name it for easy identification in hierarchy
        marker.name = "CollisionMarker";

        // Auto-destroy after 5 seconds
        Destroy(marker, 5f);
    }

}

