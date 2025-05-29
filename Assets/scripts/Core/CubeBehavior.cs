using UnityEngine;
using System.Collections;
using static Enumerations;

public class CubeBehavior : MonoBehaviour
{
    [Header("Cube Properties")]
    [SerializeField] public int level = 1;
    [SerializeField] public Vector2Int position;
    [SerializeField] public CubeType type;
    [SerializeField] public Material material;
    [SerializeField] public GameObject prefab;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.25f;
    public bool isRainingCube = false;

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;
    [SerializeField] private Rigidbody cubeRigidbody;
    [SerializeField] private Collider cubeCollider;


    private GridManager grid;
    private DetonationManager detonationManager;
    private bool isMoving = false;
    public bool isDestroyed = false;
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    private bool isRainAnimating = false;
    public int moveCountRemaining = 0;
    private float tileScale = 3f; // Default scale value

    public void Init(GridManager gridManager, CubeData cubeData, float spawnHeight = 5f)
    {
        // Store reference to grid manager and get the tile scale
        grid = gridManager;
        tileScale = grid.TileScale;

        // Use the provided cube data
        name = cubeData.Definition.name;
        type = cubeData.type;
        position = cubeData.position;
        level = cubeData.level;
        isRainingCube = cubeData.isRainingCube;
        moveCountRemaining = cubeData.moveCountRemaining;

        // Set references
        material = cubeData.Definition.material;
        prefab = cubeData.Definition.prefab;

        // Scale the cube to match tile scale
        transform.localScale = new Vector3(tileScale, tileScale, tileScale);

        // Initialize position and references
        transform.position = new Vector3(position.x * tileScale, spawnHeight, position.y * tileScale);
        detonationManager = FindAnyObjectByType<DetonationManager>();
        gameObject.name = name;

        // Setup physics
        SetupPhysics();
    }

    private void SetupPhysics()
    {
        if (!usePhysics) return;

        // Add collider if not present
        cubeCollider = GetComponent<Collider>();
        if (cubeCollider == null)
        {
            cubeCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Make sure collider is NOT a trigger
        cubeCollider.isTrigger = false;

        Debug.Log($"Collider setup complete for {name} cube");
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
            return true;
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
                if (type == Enumerations.CubeType.Black)
                {
                    WaveManager waveManager = FindObjectOfType<WaveManager>();
                    if (waveManager != null)
                    {
                        waveManager.RegisterEscapedBlackCube(position);
                    }
                }
                else
                {
                    // Non-black cube escaped - notify wave manager
                    WaveManager waveManager = FindObjectOfType<WaveManager>();
                    if (waveManager != null)
                    {
                        waveManager.OnNonBlackCubeProcessed(type, false); // false = escaped
                        waveManager.OnCubeEscaped(type);
                    }
                }

                Destroy(gameObject);
                return false;
            }
        }

        // Animate the forward movement
        position.y -= 1;
        StartCoroutine(AnimateMove(position));

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

        return true;
    }

    public void CheckForCollisionOnLanding()
    {
        if (isDestroyed) return;

        // Find any cubes at this position
        foreach (CubeBehavior otherCube in FindObjectsOfType<CubeBehavior>())
        {
            if (otherCube != this &&
                otherCube.position.x == position.x &&
                otherCube.position.y == position.y)
            {
                // Found another cube at our position, trigger collision
                CubeCollisionController collisionController = GetComponent<CubeCollisionController>();
                if (collisionController == null)
                {
                    collisionController = gameObject.AddComponent<CubeCollisionController>();
                    collisionController.Initialize(FindObjectOfType<GridManager>());
                }

                Debug.Log($"Triggering collision between raining {type} and static {otherCube.type} at ({position.x}, {position.y})");
                collisionController.HandleCubeCollision(this, otherCube, position);

                // No need to check further collisions
                break;
            }
        }
    }

    private IEnumerator AnimateMove(Vector2Int newPos)
    {
        isMoving = true;

        // Calculate scaled world positions
        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x * tileScale, 2, newPos.y * tileScale);

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
        transform.localScale = new Vector3(tileScale * 1.05f, tileScale * 0.9f, tileScale * 1.05f);
        yield return new WaitForSeconds(squashDuration);

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileScale, tileScale, tileScale);

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
        float totalHeight = startPos.y - (1f * tileScale); // Distance to ground level with scale
        float segmentHeight = totalHeight / (moveCountRemaining > 0 ? moveCountRemaining : 1);

        // Calculate target position for this tick
        float targetY = startPos.y - segmentHeight;
        Vector3 targetPos = new Vector3(position.x * tileScale, targetY, position.y * tileScale);

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
                transform.position = new Vector3(position.x * tileScale, 1f * tileScale, position.y * tileScale);

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
        Vector3 squashedScale = new Vector3(tileScale * 1.2f, tileScale * 0.7f, tileScale * 1.2f);

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
}