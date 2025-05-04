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
    private DetonationManager detonationManager;
    private bool isMoving = false;
    private bool isDestroyed = false;
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    private bool isRainAnimating = false;
    public int moveCountRemaining = 0;
    public bool isPhased { get; private set; }

    public void Init(GridManager gridManager, Vector2Int startPos, int startLevel, float spawnHeight = 5f)
    {
        grid = gridManager;
        position = startPos;
        level = startLevel;
        transform.position = new Vector3(position.x, spawnHeight, position.y); // Use spawnHeight parameter
        detonationManager = FindAnyObjectByType<DetonationManager>();
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
            StartCoroutine(RainAnimation());
        }

        if (isRainingCube && isRainAnimating)
        {
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
                if (landingTile.IsAdvantaged)
                {
                    detonationManager.TriggerNextDetonation(position.x, position.y);
                }
            }
        }

        // Animate the forward movement
        position.y -= 1;
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

    public void CheckForCollisionOnLanding()
    {
        if (isDestroyed) return;

        // Find any cubes at this position
        foreach (CubeBehavior otherCube in FindObjectsOfType<CubeBehavior>())
        {
            if (otherCube != this &&
                otherCube.position.x == position.x &&
                otherCube.position.y == position.y &&
                !otherCube.isPhased)
            {
                // Found another cube at our position, trigger collision
                CubeCollisionController collisionController = GetComponent<CubeCollisionController>();
                if (collisionController == null)
                {
                    collisionController = gameObject.AddComponent<CubeCollisionController>();
                    collisionController.Initialize(FindObjectOfType<GridManager>());
                }

                Debug.Log($"Triggering collision between raining {CubeType} and static {otherCube.CubeType} at ({position.x}, {position.y})");
                collisionController.HandleCubeCollision(this, otherCube, position);

                // No need to check further collisions
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

        // Get the current position and calculate target position for this tick
        Vector3 startPos = transform.position;

        // Calculate the total height and divide into segments based on remaining moves
        float totalHeight = startPos.y - 1f; // Distance to ground level
        float segmentHeight = totalHeight / (moveCountRemaining > 0 ? moveCountRemaining : 1);

        // Calculate target position for this tick
        float targetY = startPos.y - segmentHeight;
        Vector3 targetPos = new Vector3(position.x, targetY, position.y);

        // Animate this segment of the fall
        float segmentDuration = 0.4f; // Adjust as needed to match movement interval
        float elapsed = 0f;

        while (elapsed < segmentDuration && !isDestroyed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / segmentDuration);

            // Simulate gravity with quadratic easing
            float easedT = t * t;

            transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            yield return null;
        }

        // Ensure we're at the target position
        if (!isDestroyed)
        {
            transform.position = targetPos;

            // Decrement the move count
            moveCountRemaining--;

            // Add bounce effect
            StartCoroutine(BounceEffect());

            // If we've reached zero remaining moves, notify that the cube has landed
            if (moveCountRemaining <= 0)
            {
                // Make sure we're at final ground position
                transform.position = new Vector3(position.x, 1f, position.y);

                // Notify the wave manager that this cube has landed vertically
                WaveManager waveManager = FindObjectOfType<WaveManager>();
                if (waveManager != null)
                {
                    waveManager.CubeRainLanded(this);
                }

                // No longer a raining cube
                isRainingCube = false;
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