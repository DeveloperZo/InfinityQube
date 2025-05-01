using UnityEngine;
using System.Collections;

public class CubeCollisionController : MonoBehaviour
{
    private GridManager grid;
    private DetonationManager detonationManager;

    public void Initialize(Vector2 position, GridManager gridManager)
    {
        grid = gridManager;
        detonationManager = FindObjectOfType<DetonationManager>();
    }

    // Method to handle all cube collisions
    public void HandleCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        if (sourceCube == null || targetCube == null) return;

        // Get cube types for clarity
        Enumerations.CubeType sourceType = sourceCube.CubeType;
        Enumerations.CubeType targetType = targetCube.CubeType;

        // Black cube colliding with Black cube
        if (sourceType == Enumerations.CubeType.Black && targetType == Enumerations.CubeType.Black)
        {
            // Blacken the tile
            Tile tile = grid.tiles[position.x, position.y];
            if (tile != null)
            {
                tile.TransformTile(Enumerations.CubeType.Black);
                // Destroy the target cube
                Destroy(targetCube.gameObject);
            }
        }
        // Black cube colliding with Green cube
        else if (sourceType == Enumerations.CubeType.Black && targetType == Enumerations.CubeType.Green)
        {
            // Trigger immediate detonation
            if (detonationManager != null)
            {
                detonationManager.RegisterDetonationPoint(position);
                detonationManager.TriggerNextDetonation();
            }
            // Destroy green cube, keep black cube
            Destroy(targetCube.gameObject);
        }
        // Black cube colliding with Normal cube
        else if (sourceType == Enumerations.CubeType.Black && targetType == Enumerations.CubeType.Normal)
        {
            // Black cube consumes normal cube
            Destroy(targetCube.gameObject);
        }
        // Green cube colliding with Normal cube
        else if (sourceType == Enumerations.CubeType.Green && targetType == Enumerations.CubeType.Normal)
        {
            // Green cube consumes normal cube
            Destroy(targetCube.gameObject);
        }
        // Green cube colliding with Black cube
        else if (sourceType == Enumerations.CubeType.Green && targetType == Enumerations.CubeType.Black)
        {
            // Green cube is consumed, triggers detonation
            if (detonationManager != null)
            {
                detonationManager.RegisterDetonationPoint(position);
                detonationManager.TriggerNextDetonation();
            }
            Destroy(sourceCube.gameObject);
        }
        // Green cube colliding with Green cube
        else if (sourceType == Enumerations.CubeType.Green && targetType == Enumerations.CubeType.Green)
        {
            // Create an enhanced tile with special properties
            Tile tile = grid.tiles[position.x, position.y];
            if (tile != null)
            {
                tile.TransformTile(Enumerations.CubeType.Green);
                // Enhanced tile logic to be implemented
                // Store some state in the tile about detonation power
            }
            // Merge cubes (destroy one)
            Destroy(targetCube.gameObject);
        }
        // Normal cube colliding with Normal cube
        else if (sourceType == Enumerations.CubeType.Normal && targetType == Enumerations.CubeType.Normal)
        {
            // Both are consumed
            Destroy(targetCube.gameObject);
            Destroy(sourceCube.gameObject);
        }
        else if (sourceType == Enumerations.CubeType.Blue && targetType == Enumerations.CubeType.Normal)
        {
            // Push normal cube one space forward
            if (targetCube != null)
            {
                // Calculate push direction (same as cube moving direction)
                Vector2Int pushPosition = new Vector2Int(targetCube.position.x, targetCube.position.y - 1);

                // Check if push position is valid
                if (pushPosition.y >= 0 && pushPosition.x >= 0 &&
                    pushPosition.x < grid.Width && pushPosition.y < grid.Height)
                {
                    // Move the normal cube
                    targetCube.position = pushPosition;
                    targetCube.transform.position = new Vector3(pushPosition.x, 1f, pushPosition.y);

                    // Update tile references
                    grid.tiles[position.x, position.y].currentCube = sourceCube;
                    grid.tiles[pushPosition.x, pushPosition.y].currentCube = targetCube;
                }
                else
                {
                    // Push off grid = destroy normal cube
                    Destroy(targetCube.gameObject);
                }
            }
            // Blue cube stops
            // Implement a "stopped" state for the blue cube
        }
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