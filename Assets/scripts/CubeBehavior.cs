using UnityEngine;
using System.Collections;

public class CubeBehavior : MonoBehaviour
{
    [Header("Cube Properties")]
    [SerializeField] public int level = 1;
    [SerializeField] public Vector2Int position;
    [SerializeField] public Enumerations.CubeType CubeType;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.25f;
    public bool isRainingCube = false;
    private GridManager grid;
    private CubeCollisionController collisionController;
    private bool isMoving = false;
    private bool isDestroyed = false;
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    private bool isRainAnimating = false;
    public int moveCountRemaining = 0;
    public bool isPhased { get; private set; }

    public void Init(GridManager gridManager, Vector2Int startPos, int startLevel)
    {
        if (gridManager == null)
        {
            Debug.LogError("CubeBehavior initialized with null GridManager!");
            Destroy(gameObject);
            return;
        }

        grid = gridManager;
        position = startPos;
        level = startLevel;
        transform.position = new Vector3(position.x, 1f, position.y);

        // Reset any movement flags when initializing
        isMoving = false;
        isDestroyed = false;

        // Reset rotation
        transform.rotation = Quaternion.identity;
        // Find or create collision controller
        collisionController = GetComponent<CubeCollisionController>();
        if (collisionController == null)
        {
            collisionController = gameObject.AddComponent<CubeCollisionController>();
            collisionController.Initialize(gridManager);
        }
    }

    private void FixedUpdate()
    {
        if (isRainingCube && !isRainAnimating && transform.position.y > 1f)
        {
            // Start the vertical rain animation
            StartCoroutine(RainAnimation());
        }
    }
    // Add this method to reset movement state
    public void ResetMovementState()
    {
        isMoving = false;
    }

    private void OnDestroy()
    {
        // Mark as destroyed to prevent issues during coroutines
        isDestroyed = true;
        StopAllCoroutines();
    }

    // In CubeBehavior.cs
    public bool MoveForward()
    {
        if (isMoving || isDestroyed) return true;

        // Handle raining cubes and moveCount
        if (isRainingCube)
        {
            if (moveCountRemaining > 0)
            {
                moveCountRemaining--;

                // Don't actually move forward until moveCount is depleted
                if (moveCountRemaining > 0)
                {
                    return true;
                }

                // Ready to join normal cube flow
                isRainingCube = false;
                Debug.Log("Rain cube now part of normal cube flow");
            }
        }

        // Normal movement logic
        Vector2Int oldPos = position;
        position.y -= 1;  // Remember: in grid coordinates, y is the row (Z in 3D space)

        if (isPhased)
        {
            transform.position = new Vector3(position.x, 1f, position.y);
            return true;
        }

        // Check for off-grid conditions
        if (position.y < 0 || position.x < 0 || position.x >= grid.Width)
        {
            // Should the cube escape?
            if (!isRainingCube || moveCountRemaining <= 0)
            {
                // Handle black cubes that escape
                if (CubeType == Enumerations.CubeType.Black)
                {
                    WaveManager waveManager = FindObjectOfType<WaveManager>();
                    if (waveManager != null)
                    {
                        waveManager.RegisterEscapedBlackCube(position);
                    }
                }

                Destroy(gameObject);
                return false;
            }
        }

        // Process landing on tiles
        if (position.y >= 0 && position.x >= 0 && position.x < grid.Width)
        {
            Tile landingTile = grid.tiles[position.x, position.y];
            if (landingTile != null && !isDestroyed)
            {
                landingTile.HandleCubeLanding(this);
            }
        }

        // Animate the forward movement
        StartCoroutine(AnimateMove(position));
        return true;
    }

    public void HandleVerticalImpact(Vector2Int position, GridManager grid)
    {
        Tile tile = grid.tiles[position.x, position.y];

        // Find cube at landing position (if any)
        CubeBehavior targetCube = tile.currentCube;

        if (targetCube == null)
        {
            tile.currentCube = this;
        }
        else
        {
            // Use collision controller to handle the interaction
            collisionController.HandleCubeCollision(this, targetCube, position);
        }
    }

    private void CheckForCubeBelow()
    {
        if (CubeType != Enumerations.CubeType.Black) return;

        // Find if there's a cube at our position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube != this && // Not checking against ourselves
                cube.position.x == position.x &&
                cube.position.y == position.y)
            {
                // We found a cube below us, replace it
                StartCoroutine(ReplaceExistingCube(cube));
                break;
            }
        }
    }

    private IEnumerator ReplaceExistingCube(CubeBehavior targetCube)
    {
        if (targetCube == null) yield break;

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

        // Remove the target cube
        Destroy(targetCube.gameObject);
    }

    private IEnumerator AnimateMove(Vector2Int newPos)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x, 1f, newPos.y);
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90f, 0f, 0f); // 90° roll forward

        while (elapsed < moveDuration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.Lerp(startRot, endRot, t), t);

            yield return null;
        }

        // Ensure position is exactly at destination
        if (!isDestroyed)
        {
            transform.position = end;
        }

        if (isDestroyed) yield break;

        // Weighty visual squash
        transform.position = end;
        transform.localScale = new Vector3(1.05f, 0.9f, 1.05f);
        yield return new WaitForSeconds(squashDuration);

        if (isDestroyed) yield break;

        transform.localScale = Vector3.one;

        // Check for marker interaction (guard against destroyed tiles)
        if (grid != null && newPos.x >= 0 && newPos.x < grid.Width &&
            newPos.y >= 0 && newPos.y < grid.Height)
        {
            Tile tile = grid.tiles[newPos.x, newPos.y];
            if (tile != null && tile.HasMarker)
            {
                tile.ProcessCubeInteraction(this);
            }
        }

        isMoving = false;
    }

    private IEnumerator RainAnimation()
    {
        isRainAnimating = true;

        // Calculate the target position (directly below, maintaining X and Z)
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, 1f, startPos.z);

        float fallDuration = Mathf.Max(0.5f, (startPos.y - 1f) / 5f);
        float elapsed = 0f;

        while (elapsed < fallDuration && !isDestroyed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);

            // Simulate gravity with quadratic easing
            float easedT = t * t;

            transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            yield return null;
        }

        // Ensure final position
        if (!isDestroyed)
        {
            transform.position = targetPos;

            // Add bounce effect
            StartCoroutine(BounceEffect());

            // Notify the wave manager that this cube has landed vertically
            // It will still be part of the wave and move forward with moveCount
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.CubeRainLanded(this);
            }
        }

        isRainAnimating = false;
    }

    private IEnumerator BounceEffect()
    {
        if (isDestroyed) yield break;

        Vector3 originalScale = transform.localScale;
        Vector3 squashedScale = new Vector3(1.2f, 0.7f, 1.2f);

        // Squash on impact
        float squashDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < squashDuration && !isDestroyed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashDuration);

            transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);

            yield return null;
        }

        // Return to normal
        elapsed = 0f;
        while (elapsed < squashDuration && !isDestroyed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashDuration);

            transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);

            yield return null;
        }

        // Ensure final scale
        if (!isDestroyed)
        {
            transform.localScale = originalScale;
        }
    }

    public void SetPhased(bool phased)
    {
        isPhased = phased;

        // Update collider based on phased state
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = !phased;
        }
    }
}