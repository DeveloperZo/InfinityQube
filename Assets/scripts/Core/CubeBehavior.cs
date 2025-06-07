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
    [SerializeField] public float spawnHeight;
    [SerializeField] public int currentHitPoints = 1;
    [SerializeField] public int maxHitPoints = 1;
    [System.NonSerialized] private CubeData cubeData;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.25f;
    public bool isRainingCube = false;

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;
    [SerializeField] private Rigidbody cubeRigidbody;
    [SerializeField] private Collider cubeCollider;


    private GridManager grid;
    private PlayerActionManager playerActionManager;
    public bool isMoving = false;
    public bool isDestroyed = false;
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    private bool isRainAnimating = false;
    public int moveCountRemaining = 0;
    private float tileScale = 3f; // Default scale value
    private float tileSize = 1f;

    public void Init(GridManager gridManager, CubeData cubeData, float spawnHeight = 2f)
    {
        // Store reference to grid manager and get the tile scale
        grid = gridManager;
        tileSize = grid.TileSize;

        // Use the provided cube data
        name = cubeData.Definition?.name ?? cubeData.type.ToString();
        type = cubeData.type;
        position = cubeData.position;
        level = cubeData.level;
        isRainingCube = cubeData.isRainingCube;
        moveCountRemaining = cubeData.moveCountRemaining;
        currentHitPoints = cubeData.Definition.maxHitPoints;
        maxHitPoints = cubeData.Definition.maxHitPoints;


        // Set references
        material = cubeData.Definition?.material;
        prefab = cubeData.Definition?.prefab;

        // Scale the cube to match tile scale
        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

        // FIXED: Use grid's coordinate conversion instead of manual calculation
        Vector3 worldPos = grid.GridToWorldPosition(position.x, position.y, spawnHeight);
        transform.position = worldPos;

        Debug.Log($"Cube {type} initialized at grid ({position.x}, {position.y}) -> world {worldPos}, HP: {currentHitPoints}/{maxHitPoints}");

        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        gameObject.name = name;

        // Setup physics
        SetupPhysics();

        // Update visual based on current hit points
        UpdateDamageVisual();
    }
    public bool TakeDamage(int damage = 1)
    {
        if (isDestroyed) return false;

        // Get the cube data from the grid position or component

        currentHitPoints -= damage;

        Debug.Log($"{type} cube at ({position.x}, {position.y}) took {damage} damage. HP: {currentHitPoints}/{maxHitPoints}");

        UpdateDamageVisual();

        // Return true if cube is destroyed
        if (currentHitPoints <= 0)
        {
            return true; // Cube should be destroyed
        }

        return false; // Cube still alive
    }

    private void UpdateDamageVisual()
    {


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

        Debug.Log($"Moving cube {type} from ({position.x}, {position.y}) forward");

        // Check for off-grid conditions
        if (position.y < 0 || position.x < 0 || position.x >= grid.Width)
        {
            Debug.Log($"Cube {type} at ({position.x}, {position.y}) is off-grid. Grid bounds: {grid.Width}x{grid.Height}");

            // Should the cube escape?
            if (!isRainingCube || moveCountRemaining <= 0)
            {
                // Handle black cubes that escape
                if (type == Enumerations.CubeType.Black)
                {
                    Debug.Log("Black cube escaped");
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
                    Debug.Log($"Non-black cube {type} escaped");
                }

                Destroy(gameObject);
                return false;
            }
        }

        // Update logical position immediately (atomic movement)
        position.y -= 1;
        Debug.Log($"Cube {type} moved to ({position.x}, {position.y})");

        // Start animation to catch up to the logical position
        StartCoroutine(AnimateMove(position));

        // Process landing on tiles immediately (based on logical position)
        if (position.y >= 0 && position.x >= 0 && position.x < grid.Width)
        {
            Tile landingTile = grid.tiles[position.x, position.y];
            if (landingTile != null && !isDestroyed)
            {
                landingTile.HandleCubeLanding(this);

            }
        }

        return true;
    }


    private IEnumerator AnimateMove(Vector2Int newPos)
    {
        isMoving = true;

        // Calculate world positions using grid conversion
        Vector3 start = transform.position;
        Vector3 end = grid.GridToWorldPosition(newPos.x, newPos.y, 2f); // Keep cubes slightly above ground

        Debug.Log($"Animating cube from {start} to {end} (grid pos {newPos})");

        // Get the current move interval from WaveManager to sync animation speed
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        float actualMoveDuration = moveDuration;

        if (waveManager != null)
        {
            // Use the current move interval but cap it at our normal duration
            float currentInterval = waveManager.isSpeedingUp ? waveManager.fastMoveInterval :
                                   (waveManager.CurrentWave?.moveInterval ?? waveManager.normalMoveInterval);
            actualMoveDuration = Mathf.Min(moveDuration, currentInterval * 0.8f); // Use 80% of interval for smooth movement
        }

        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90f, 0f, 0f); // 90° roll forward

        while (elapsed < actualMoveDuration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / actualMoveDuration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.Lerp(startRot, endRot, t), t);

            yield return null;
        }

        // Ensure position is exactly at destination
        if (!isDestroyed)
        {
            transform.position = end;
            transform.rotation = Quaternion.identity; // Reset rotation to prevent drift
        }

        if (isDestroyed) yield break;

        // Weighty visual squash - also speed this up proportionally
        transform.localScale = new Vector3(tileSize * 1.05f, tileSize * 0.9f, tileSize * 1.05f);

        float squashTime = Mathf.Min(squashDuration, actualMoveDuration * 0.3f); // Squash takes 30% of move time
        yield return new WaitForSeconds(squashTime);

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

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

}