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
            collisionController.Initialize(startPos, gridManager);
        }
    }

    private void FixedUpdate()
    {
        if (isRainingCube && transform.position.y >= 1f)
        {
           transform.position = new Vector3(transform.position.x, transform.position.y-0.5f, transform.position.z);
            if(transform.position.y <= 1f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
            }

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

        // Store previous position for logging
        Vector2Int oldPos = position;
        position.y -= 1;
        // Debug logging to track cube movement
        //Debug.Log($"Cube moving from ({oldPos.x}, {oldPos.y}) to ({position.x}, {position.y})");

        // Check if this is a raining cube reaching the grid
        if (isRainingCube && position.y == grid.Height - 1)
        {
            // Now the cube has reached the grid, it follows normal rules
            isRainingCube = false;

            // Check if we're landing on another cube
            CheckForCubeBelow();
        }

        // Off the grid = escape (but raining cubes don't escape until they reach the grid)
        if ((!isRainingCube && position.y < 0) || position.x < 0 || position.x >= grid.Width)
        {
            // Special handling for black cubes that escape
            if (CubeType == Enumerations.CubeType.Black)
            {
                WaveManager waveManager = FindObjectOfType<WaveManager>();
                if (waveManager != null)
                {
                    waveManager.RegisterEscapedBlackCube(position);
                    //Debug.Log($"Black cube escaped at x={position.x}");
                }
            }

            //Debug.Log($"Cube escaped at level {level}");
            Destroy(gameObject);
            return false;
        }

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
}