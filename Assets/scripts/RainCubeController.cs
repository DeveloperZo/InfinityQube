using UnityEngine;
using System.Collections;

public class RainCubeController : MonoBehaviour
{
    private int targetX;
    private GridManager grid;
    private float fallSpeed = 3f;
    private bool hasLanded = false;
    private bool isWaitingForTarget = true;
    private float initialWaitTime = 0.5f; // Wait a bit before starting to look for targets

    // The cube stays in fixed position until it finds a target
    public void Initialize(int x, GridManager gridManager)
    {
        targetX = x;
        grid = gridManager;

        // CRITICAL CHANGE: Initialize position directly above its column
        transform.position = new Vector3(targetX, 7f, grid.Height - 1);

        // Start the hovering and tracking coroutine
        StartCoroutine(HoverAndTrackTargets());

        // Add a visual indicator for debugging
        CreateDebugMarker();
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
        marker.name = "RainMarker";

        // Auto-destroy after 5 seconds
        Destroy(marker, 5f);
    }

    private IEnumerator HoverAndTrackTargets()
    {
        // Wait a short time before starting to look for targets
        // This gives other cubes time to settle into position
        yield return new WaitForSeconds(initialWaitTime);

        // Create a fixed hover position directly above the column
        Vector3 hoverPosition = new Vector3(targetX, 7f, grid.Height - 1);

        // Ensure we start at the hover position
        transform.position = hoverPosition;

        // Slight hover animation
        Vector3 hoverUp = hoverPosition + new Vector3(0, 0.2f, 0);
        Vector3 hoverDown = hoverPosition - new Vector3(0, 0.2f, 0);
        float hoverSpeed = 0.5f;
        bool hovering = true;

        // How long to wait after a wave before checking for targets
        float postWaveDelay = 0.5f;
        float timeSinceLastCheck = 0f;

        while (isWaitingForTarget)
        {
            timeSinceLastCheck += Time.deltaTime;

            // Only check for targets periodically to avoid falling during a wave movement
            if (timeSinceLastCheck >= postWaveDelay)
            {
                // Check for cubes below
                CubeBehavior targetCube = FindTargetCube();

                if (targetCube != null)
                {
                    // Found a target - start falling to intercept
                    isWaitingForTarget = false;
                    StartCoroutine(FallOntoTarget(targetCube));
                    break;
                }

                timeSinceLastCheck = 0f;
            }

            // Hover animation while waiting
            if (hovering)
            {
                transform.position = Vector3.MoveTowards(transform.position, hoverUp, Time.deltaTime * hoverSpeed);
                if (Vector3.Distance(transform.position, hoverUp) < 0.01f)
                    hovering = false;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, hoverDown, Time.deltaTime * hoverSpeed);
                if (Vector3.Distance(transform.position, hoverDown) < 0.01f)
                    hovering = true;
            }

            yield return null;
        }
    }

    private CubeBehavior FindTargetCube()
    {
        // Start from the top of the grid and move down
        for (int z = grid.Height - 1; z >= 0; z--)
        {
            // Look for any cube in this column at this level
            foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
            {
                if (cube.position.x == targetX && // Same X position 
                    cube.position.y == z && // Check specific row
                    cube.gameObject != gameObject) // Not this cube
                {
                    Debug.Log($"Rain cube found target at ({cube.position.x}, {cube.position.y})");
                    return cube;
                }
            }
        }

        // Alternative: check if we can directly land on the grid
        // Check if we can land directly on the bottom row
        bool hasBlockingCube = false;
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == targetX && cube.gameObject != gameObject)
            {
                hasBlockingCube = true;
                break;
            }
        }

        // If no blocking cubes in this column, land on the grid
        if (!hasBlockingCube)
        {
            Debug.Log("Rain cube will land directly on the grid - no targets in column");
            StartCoroutine(LandOnGrid());
            return null;
        }

        return null;
    }

    private IEnumerator LandOnGrid()
    {
        // Calculate target position at the top of the grid
        Vector3 targetPos = new Vector3(targetX, 1f, grid.Height - 1);
        Vector3 startPos = transform.position;

        // Fall straight down
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / fallSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Use quadratic easing for accelerating fall
            float easedT = t * t;
            transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            yield return null;
        }

        // Ensure we're exactly at the target position
        transform.position = targetPos;

        // Squash effect when landing
        transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;

        // Convert to normal cube
        Vector2Int gridPos = new Vector2Int(targetX, grid.Height - 1);

        // Reset cube rotation
        transform.rotation = Quaternion.identity;

        // Convert this into a normal cube that follows wave rules
        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            thisCube.StopAllCoroutines();
            thisCube.Init(grid, gridPos, 1);

            // Register with wave manager
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(thisCube);
                Debug.Log($"Rain cube landed on grid at position ({gridPos.x}, {gridPos.y}) and registered with wave manager");
            }
        }

        // Remove this controller
        Destroy(this);
    }

    private IEnumerator FallOntoTarget(CubeBehavior targetCube)
    {
        if (targetCube == null)
        {
            // Target was destroyed or removed - go back to hovering
            isWaitingForTarget = true;
            StartCoroutine(HoverAndTrackTargets());
            yield break;
        }

        // Calculate straight vertical drop to the target
        Vector3 targetPos = new Vector3(targetX, targetCube.transform.position.y + 1.1f, targetCube.position.y);
        Vector3 startPos = transform.position;

        float distance = Mathf.Abs(startPos.y - targetPos.y);
        float duration = distance / fallSpeed;
        float elapsed = 0f;

        // Fall straight down to the target
        while (elapsed < duration)
        {
            // Check if target is still valid
            if (targetCube == null || targetCube.gameObject == null)
            {
                // Target was destroyed - go back to hovering
                isWaitingForTarget = true;
                StartCoroutine(HoverAndTrackTargets());
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Use quadratic easing for accelerating fall
            float easedT = t * t;

            // Only modify Y position for straight drop
            Vector3 currentPos = transform.position;
            float newY = Mathf.Lerp(startPos.y, targetPos.y, easedT);
            transform.position = new Vector3(currentPos.x, newY, currentPos.z);

            // Check if we're close enough to land
            if (Mathf.Abs(transform.position.y - targetPos.y) < 0.1f)
            {
                // Land on target
                transform.position = new Vector3(targetX, targetPos.y, targetCube.position.y);
                yield return StartCoroutine(LandOn(targetCube));
                break;
            }

            yield return null;
        }
    }

    private IEnumerator LandOn(CubeBehavior targetCube)
    {
        if (targetCube == null) yield break;

        hasLanded = true;

        // Flash effect on the target cube
        Renderer renderer = targetCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }

        // Squash effect
        Vector3 originalScale = targetCube.transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            targetCube.transform.localScale = Vector3.Lerp(
                originalScale,
                new Vector3(originalScale.x * 1.4f, originalScale.y * 0.1f, originalScale.z * 1.4f),
                t
            );

            yield return null;
        }

        // Get the position and type before destroying the target cube
        Vector2Int cubePos = targetCube.position;
        bool landingOnBlackCube = targetCube.CubeType == Enumerations.CubeType.Black;

        // Remove the target cube
        Destroy(targetCube.gameObject);

        // If landing on a black cube, blacken the tile (if you want this feature)
        if (landingOnBlackCube && grid != null &&
            cubePos.x >= 0 && cubePos.x < grid.Width &&
            cubePos.y >= 0 && cubePos.y < grid.Height)
        {
            Tile tile = grid.tiles[cubePos.x, cubePos.y];
            if (tile != null)
            {
                // If you implement BlackenTile method, uncomment this
                // tile.BlackenTile();
            }
        }

        // CRITICAL CHANGE: Move transform position explicitly before initializing
        transform.position = new Vector3(cubePos.x, 1f, cubePos.y);

        // Reset cube rotation to identity before converting
        transform.rotation = Quaternion.identity;

        // Convert this into a normal cube that follows wave rules
        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            // Clear any existing state that might interfere
            thisCube.StopAllCoroutines();

            // Reset the cube to make sure it has fresh state
            thisCube.Init(grid, cubePos, 1);

            // Add to the wave manager's active cubes
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(thisCube);
                Debug.Log($"Rain cube at position ({cubePos.x}, {cubePos.y}) registered with wave manager");
            }
            else
            {
                Debug.LogError("Wave manager not found - rain cube cannot be registered!");
            }
        }

        // Remove this controller (but not the GameObject)
        Destroy(this);
    }
}