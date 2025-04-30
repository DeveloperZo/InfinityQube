using UnityEngine;
using System.Collections;

public class RainCubeController : MonoBehaviour
{
    private int targetX;
    public GridManager grid;
    public WaveManager waveManager;
    private float fallSpeed = 3f;
    private bool hasLanded = false;
    private bool isWaitingForTarget = true;

    // The cube stays in fixed position until it finds a target
    public void Initialize(int x, GridManager gridManager)
    {
        targetX = x;
        grid = gridManager;

        // Start the hovering and tracking coroutine
        StartCoroutine(HoverAndTrackTargets());
    }

    private IEnumerator HoverAndTrackTargets()
    {
        // Initial position - hovering above the grid
        Vector3 hoverPosition = new Vector3(targetX, 7f, grid.Height - 1);
        transform.position = hoverPosition;

        // Slight hover animation
        Vector3 hoverUp = hoverPosition + new Vector3(0, 0.2f, 0);
        Vector3 hoverDown = hoverPosition - new Vector3(0, 0.2f, 0);
        float hoverSpeed = 0.5f;
        bool hovering = true;

        while (isWaitingForTarget)
        {
            // Check for cubes moving below
            CubeBehavior targetCube = FindTargetCube();

            if (targetCube != null)
            {
                // Found a target - start falling to intercept
                isWaitingForTarget = false;
                StartCoroutine(FallOntoTarget(targetCube));
                break;
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
        // Look for cubes in the same X column that are moving
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == targetX && // Same X position
                cube.gameObject != gameObject && // Not this cube
                !hasLanded) // We haven't landed yet
            {
                return cube;
            }
        }

        return null;
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

        // Calculate the fall
        Vector3 startPos = transform.position;

        while (true)
        {
            // Check if target is still valid
            if (targetCube == null || targetCube.gameObject == null)
            {
                // Target was destroyed - go back to hovering
                isWaitingForTarget = true;
                StartCoroutine(HoverAndTrackTargets());
                yield break;
            }

            // Move toward the target cube position
            Vector3 targetPos = targetCube.transform.position + Vector3.up * 0.1f; // Slightly above target
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * fallSpeed);

            // Check if we're close enough to land
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                // Land on target
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

        // Take the target's position
        Vector2Int cubePos = targetCube.position;
        bool landingOnBlackCube = targetCube.CubeType == Enumerations.CubeType.Black;

        // Remove the target cube
        Destroy(targetCube.gameObject);

        // If landing on a black cube, blacken the tile
        if (landingOnBlackCube && grid != null &&
            cubePos.x >= 0 && cubePos.x < grid.Width &&
            cubePos.y >= 0 && cubePos.y < grid.Height)
        {
            Tile tile = grid.tiles[cubePos.x, cubePos.y];
            if (tile != null)
            {
                // Add BlackenTile method to Tile.cs if you want this feature
                // tile.BlackenTile();
            }
        }

        // Convert this into a normal cube that follows wave rules
        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            thisCube.Init(grid, cubePos, 1);

            // CRITICAL: Get the wave manager and properly add this cube
            if (waveManager != null)
            {
                // Direct method call instead of reflection
                waveManager.AddCubeToActiveList(thisCube);
            }
        }

        // Remove this controller
        Destroy(this);
    }
}