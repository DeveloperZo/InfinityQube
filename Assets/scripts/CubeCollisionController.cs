using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CubeCollisionController : MonoBehaviour
{
    private GridManager grid;
    private DetonationManager detonationManager;

    public void Initialize(GridManager gridManager)
    {
        grid = gridManager;
        detonationManager = FindObjectOfType<DetonationManager>();
    }
    public IEnumerator DelayedLanding(Vector2 targetPosition)
    {
        // Wait for 3 movement cycles
        yield return new WaitForSeconds(3 * 0.5f); // Assuming 0.5s per movement

        // Check target position for existing cubes
        Vector2Int landingPos = new Vector2Int((int)targetPosition.x, (int)targetPosition.y);
        CubeBehavior targetCube = FindCubeAtPosition(landingPos);

        if (targetCube != null)
        {
            // Handle special case for green cubes
            if (targetCube.CubeType == Enumerations.CubeType.Green)
            {
                // Trigger a smaller 2x2 detonation
                if (detonationManager != null)
                {
                    // Register the detonation point with a special "size 2" flag
                    Tile tile = grid.tiles[landingPos.x, landingPos.y];
                    if (tile != null)
                    {
                        // This assumes you've added a method to trigger a specific sized detonation
                        detonationManager.RegisterDetonationPoint(landingPos);
                        detonationManager.TriggerNextDetonation();
                    }
                }
            }

            // Consume the target cube
            Destroy(targetCube.gameObject);
        }
        else
        {
            // If no cube is present, corrupt the tile
            if (grid != null && IsValidPosition(landingPos))
            {
                Tile tile = grid.tiles[landingPos.x, landingPos.y];
                if (tile != null)
                {
                    tile.BlackenTile();
                }
            }
        }

        // Register the black cube with the wave manager to start moving
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            CubeBehavior cubeBehavior = GetComponent<CubeBehavior>();
            if (cubeBehavior != null)
            {
                // IMPORTANT: Preserve the cube's grid position here
                waveManager.RegisterRainCube(cubeBehavior);

                // Debug the cube's position for verification
                Debug.Log($"DelayedLanding registered cube at grid pos ({cubeBehavior.position.x}, {cubeBehavior.position.y}), " +
                         $"world pos ({cubeBehavior.transform.position.x}, {cubeBehavior.transform.position.y}, {cubeBehavior.transform.position.z})");
            }
        }
    }

    private CubeBehavior FindCubeAtPosition(Vector2Int position)
    {
        // Find any cube at the given position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                return cube;
            }
        }
        return null;
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
                tile.TransformTile(sourceType);

                // Consume both cubes after transformation
                Destroy(sourceCube.gameObject);
                Destroy(targetCube.gameObject);
                return;
            }
        }

        // Otherwise, route to specific collision handlers based on cube types
        if (sourceType == Enumerations.CubeType.Black)
        {
            HandleBlackCubeCollision(sourceCube, targetCube, position);
        }
        else if (sourceType == Enumerations.CubeType.Green)
        {
            HandleGreenCubeCollision(sourceCube, targetCube, position);
        }
        else if (sourceType == Enumerations.CubeType.Normal)
        {
            HandleNormalCubeCollision(sourceCube, targetCube, position);
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
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Black + Normal = Consume normal
                Destroy(targetCube.gameObject);
                break;
        }
    }

    private void HandleGreenCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Green + Black = Green consumed, triggers detonation
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(position);
                    detonationManager.TriggerNextDetonation();
                }
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                // Green + Green = Enhanced green tile
                EnhanceGreenTile(position);
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Green + Normal = Consume normal
                Destroy(targetCube.gameObject);
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
                Destroy(targetCube.gameObject);
                Destroy(sourceCube.gameObject);
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
            tile.TransformTile(Enumerations.CubeType.Black);
        }
    }

    private void EnhanceGreenTile(Vector2Int position)
    {
        if (grid == null || !IsValidPosition(position)) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            // Apply green transformation (will handle enhancement if already transformed)
            tile.TransformTile(Enumerations.CubeType.Green);

            // Register detonation point
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

