using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CubeCollisionController : MonoBehaviour
{
    private GridManager grid;
    private DetonationManager detonationManager;

    public void Initialize(Vector2 position, GridManager gridManager)
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
        else if (sourceType == Enumerations.CubeType.Blue)
        {
            HandleBlueCubeCollision(sourceCube, targetCube, position);
        }
        else if (sourceType == Enumerations.CubeType.Red)
        {
            HandleRedCubeCollision(sourceCube, targetCube, position);
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
                // Black + Green = Diagonal detonation (slash pattern)
                RegisterDiagonalPattern(position, Enumerations.CubeType.Green);
                // Immediate trigger of the created detonation points
                TriggerDiagonalDetonation(position);
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Blue:
                // Black + Blue = Diagonal time freeze (slash pattern)
                RegisterDiagonalPattern(position, Enumerations.CubeType.Blue);
                // Immediate trigger of the created time distortion points
                TriggerDiagonalTimeDistortion(position);
                Destroy(targetCube.gameObject);
                break;
            case Enumerations.CubeType.Red:
                // Black + Red = Similar to Black + Blue but with transience effect
                RegisterDiagonalPattern(position, Enumerations.CubeType.Red);
                // Immediate trigger of transience effect in a diagonal pattern
                TriggerDiagonalTransience(position);
                Destroy(targetCube.gameObject);
                break;
            case Enumerations.CubeType.Normal:
                // Black + Normal = Consume normal
                Destroy(targetCube.gameObject);
                break;
        }
    }

    // Creates a diagonal slash pattern
    private void RegisterDiagonalPattern(Vector2Int center, Enumerations.CubeType cubeType)
    {
        if (grid == null) return;

        // Determine if we use \ or / pattern (alternate based on position)
        bool useForwardSlash = (center.x + center.y) % 2 == 0;

        List<Vector2Int> slashPositions = new List<Vector2Int>();

        // Register points in the diagonal pattern
        if (useForwardSlash) // / pattern
        {
            // Top-left to bottom-right diagonal
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x + offset, center.y + offset);
                if (IsValidPosition(pos))
                {
                    slashPositions.Add(pos);
                }
            }
        }
        else // \ pattern
        {
            // Top-right to bottom-left diagonal
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x - offset, center.y + offset);
                if (IsValidPosition(pos))
                {
                    slashPositions.Add(pos);
                }
            }
        }

        // Register the points with the appropriate manager
        if (cubeType == Enumerations.CubeType.Green)
        {
            DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
            if (detonationManager != null)
            {
                foreach (Vector2Int pos in slashPositions)
                {
                    detonationManager.RegisterSlashDetonationPoint(pos);
                }
            }
        }
        else if (cubeType == Enumerations.CubeType.Blue)
        {
            TimeDistortionManager timeManager = FindObjectOfType<TimeDistortionManager>();
            if (timeManager != null)
            {
                foreach (Vector2Int pos in slashPositions)
                {
                    timeManager.RegisterSlashDistortionPoint(pos);
                }
            }
        }
    }

    // Immediately trigger the created detonation points
    private void TriggerDiagonalDetonation(Vector2Int center)
    {
        DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
        if (detonationManager != null)
        {
            detonationManager.TriggerSlashDetonation(center);
        }
    }

    // Immediately trigger the created time distortion points
    private void TriggerDiagonalTimeDistortion(Vector2Int center)
    {
        TimeDistortionManager timeManager = FindObjectOfType<TimeDistortionManager>();
        if (timeManager != null)
        {
            timeManager.TriggerSlashDistortion(center);
        }
    }

    private void TriggerDiagonalTransience(Vector2Int center)
    {
        TransienceManager transManager = FindObjectOfType<TransienceManager>();
        if (transManager != null)
        {
            // Similar structure to the diagonal time distortion but for transience
            transManager.TriggerSlashDistortion(center);
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

            case Enumerations.CubeType.Blue:
                // Green + Blue = Line detonation (placeholder)
                if (detonationManager != null)
                {
                    RegisterLinePattern(position, Enumerations.CubeType.Green);
                    detonationManager.TriggerNextDetonation();
                }
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Green + Normal = Consume normal
                Destroy(targetCube.gameObject);
                break;
        }
    }

    private void HandleBlueCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Blue + Black = Blue consumed, pushes black back
                PushCubeBack(targetCube, position);
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                // Blue + Green = Push green forward
                PushCubeForward(targetCube, position);
                break;

            case Enumerations.CubeType.Blue:
                // Blue + Blue = Create larger time field (placeholder)
                CreateTimeField(position, 2);
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Blue + Normal = Push normal forward
                PushCubeForward(targetCube, position);
                break;
        }
    }
    private void HandleRedCubeCollision(CubeBehavior sourceCube, CubeBehavior targetCube, Vector2Int position)
    {
        switch (targetCube.CubeType)
        {
            case Enumerations.CubeType.Black:
                Destroy(sourceCube.gameObject);
                break;

            case Enumerations.CubeType.Green:
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Blue:
                Destroy(targetCube.gameObject);
                break;

            case Enumerations.CubeType.Normal:
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

            case Enumerations.CubeType.Blue:
                // Normal + Blue = Normal consumed
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

    private void PushCubeForward(CubeBehavior cube, Vector2Int position)
    {
        if (cube == null || grid == null) return;

        // Calculate push direction (same as cube moving direction)
        Vector2Int pushPosition = new Vector2Int(cube.position.x, cube.position.y - 1);

        // Check if push position is valid
        if (IsValidPosition(pushPosition))
        {
            // Move the cube
            cube.position = pushPosition;
            cube.transform.position = new Vector3(pushPosition.x, 1f, pushPosition.y);

            // Update tile references
            grid.tiles[position.x, position.y].currentCube = null;
            grid.tiles[pushPosition.x, pushPosition.y].currentCube = cube;
        }
        else
        {
            // Push off grid = destroy cube
            Destroy(cube.gameObject);
        }
    }

    private void PushCubeBack(CubeBehavior cube, Vector2Int position)
    {
        if (cube == null || grid == null) return;

        // Calculate push direction (opposite of cube moving direction)
        Vector2Int pushPosition = new Vector2Int(cube.position.x, cube.position.y + 1);

        // Check if push position is valid
        if (IsValidPosition(pushPosition))
        {
            // Move the cube
            cube.position = pushPosition;
            cube.transform.position = new Vector3(pushPosition.x, 1f, pushPosition.y);

            // Update tile references
            grid.tiles[position.x, position.y].currentCube = null;
            grid.tiles[pushPosition.x, pushPosition.y].currentCube = cube;
        }
    }
    private void RegisterLinePattern(Vector2Int center, Enumerations.CubeType cubeType)
    {
        if (grid == null || detonationManager == null) return;

        // Register points in a horizontal line
        for (int x = 0; x < grid.Width; x++)
        {
            Vector2Int pos = new Vector2Int(x, center.y);
            detonationManager.RegisterDetonationPoint(pos);
        }

        // Visual debug helper
        StartCoroutine(VisualizeLinePattern(center, true, cubeType));
    }

    private void CreateTimeField(Vector2Int center, int radius)
    {
        if (grid == null) return;

        // Create time field in a square area
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidPosition(pos))
                {
                    ApplyTimeFreeze(pos);
                }
            }
        }

        // Visual debug helper
        StartCoroutine(VisualizeSquarePattern(center, radius, Enumerations.CubeType.Blue));
    }

    private void ApplyTimeFreeze(Vector2Int position)
    {
        // Find any cubes at this position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                // Add time freeze component
                TimeFrozenTag frozenTag = cube.gameObject.AddComponent<TimeFrozenTag>();
                if (frozenTag != null)
                {
                    frozenTag.frozenDuration = 2f; // Freeze for 2 movement cycles

                    // Visual effect for frozen cube
                    Renderer cubeRenderer = cube.GetComponent<Renderer>();
                    if (cubeRenderer != null)
                    {
                        // Store original color
                        frozenTag.originalColor = cubeRenderer.material.color;

                        // Set to blue tint
                        cubeRenderer.material.color = new Color(0.7f, 0.8f, 1f);
                    }
                }
            }
        }

        // Also mark the tile
        if (IsValidPosition(position) && grid.tiles[position.x, position.y] != null)
        {
            // Visual indication on tile
            Tile tile = grid.tiles[position.x, position.y];
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                // Light blue tint
                StartCoroutine(PulseTileColor(tileRenderer,
                    tileRenderer.material.color,
                    new Color(0.7f, 0.8f, 1f),
                    1.5f));
            }
        }
    }

    private IEnumerator VisualizePattern(Vector2Int center, bool forwardSlash, Enumerations.CubeType type)
    {
        // Create visualization objects for the pattern
        List<GameObject> markers = new List<GameObject>();
        Color markerColor = (type == Enumerations.CubeType.Green) ?
            new Color(0, 1, 0, 0.5f) : new Color(0, 0.7f, 1f, 0.5f);

        // Create markers for each position in the pattern
        if (forwardSlash) // / pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x + offset, center.y + offset);
                if (IsValidPosition(pos))
                {
                    GameObject marker = CreateMarker(pos, markerColor);
                    markers.Add(marker);
                }
            }
        }
        else // \ pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x - offset, center.y + offset);
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

    private IEnumerator VisualizeLinePattern(Vector2Int center, bool horizontal, Enumerations.CubeType type)
    {
        // Create visualization objects for the pattern
        List<GameObject> markers = new List<GameObject>();
        Color markerColor = (type == Enumerations.CubeType.Green) ?
            new Color(0, 1, 0, 0.5f) : new Color(0, 0.7f, 1f, 0.5f);

        if (horizontal)
        {
            // Create markers for each position in the horizontal line
            for (int x = 0; x < grid.Width; x++)
            {
                Vector2Int pos = new Vector2Int(x, center.y);
                GameObject marker = CreateMarker(pos, markerColor);
                markers.Add(marker);
            }
        }
        else
        {
            // Create markers for each position in the vertical line
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int pos = new Vector2Int(center.x, y);
                GameObject marker = CreateMarker(pos, markerColor);
                markers.Add(marker);
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