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

    [Header("Face Painting System")]
    [SerializeField] private FaceStatus[] faceStatuses = new FaceStatus[6]; // 6 cube faces
    [SerializeField] private Color[] faceColors = new Color[6]; // Visual colors for each face
    [SerializeField] private int[] faceDurations = new int[6]; // Remaining duration for each face (-1 = permanent)
    [SerializeField] private int moveCount = 0; // Track rotations
    [SerializeField] private GameObject[] faceIndicators = new GameObject[6]; // Visual indicators
    [SerializeField] private bool showFaceIndicators = true;


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

        InitializeFaceSystem();

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

        Debug.Log($"Moving cube {GetEffectiveType()} from ({position.x}, {position.y}) forward");

        // Check for off-grid conditions using effective type
        if (position.y < 0 || position.x < 0 || position.x >= grid.Width)
        {
            Debug.Log($"Cube {GetEffectiveType()} at ({position.x}, {position.y}) is off-grid. Grid bounds: {grid.Width}x{grid.Height}");

            if (!isRainingCube || moveCountRemaining <= 0)
            {
                CubeType effectiveType = GetEffectiveType();

                if (effectiveType == CubeType.Black)
                {
                    Debug.Log("Cube with corrupted face escaped (acts as black)");
                }
                else
                {
                    WaveManager waveManager = FindObjectOfType<WaveManager>();
                    if (waveManager != null)
                    {
                        waveManager.OnNonBlackCubeProcessed(effectiveType, false);
                        waveManager.OnCubeEscaped(effectiveType);
                    }
                    Debug.Log($"Cube with {effectiveType} behavior escaped");
                }

                Destroy(gameObject);
                return false;
            }
        }

        // Update position and rotation
        position.y -= 1;
        moveCount++; // This rotates the cube faces

        // Process face durations
        ProcessFaceDurations();

        Debug.Log($"Cube moved to ({position.x}, {position.y}), move count: {moveCount}");

        // Start animation
        StartCoroutine(AnimateMove(position));

        // Process landing on tiles
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


    #region Face Painting System

    public void PaintFace(CubeFace face, FaceStatus status, Color color, int duration = -1)
    {
        int faceIndex = (int)face;
        faceStatuses[faceIndex] = status;
        faceColors[faceIndex] = color;
        faceDurations[faceIndex] = duration;

        UpdateFaceVisuals();
        Debug.Log($"Painted {face} of cube at ({position.x}, {position.y}) with {status} status");
    }

    public void PaintCurrentDownFace(FaceStatus status, Color color, int duration = -1)
    {
        CubeFace downFace = GetCurrentDownFace();
        PaintFace(downFace, status, color, duration);
    }

    public FaceStatus GetActiveFaceStatus()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceStatuses[(int)downFace];
    }

    public bool HasActiveFaceStatus(FaceStatus status)
    {
        return GetActiveFaceStatus() == status;
    }

    public CubeFace GetCurrentDownFace()
    {
        // Simple 4-step rotation cycle (bottom face rotates between faces 0,2,1,3)
        int[] rotationMap = { 0, 2, 1, 3 }; // Bottom, Front, Top, Back
        return (CubeFace)rotationMap[moveCount % 4];
    }

    public CubeType GetEffectiveType()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.Corrupted:
                return CubeType.Black; // Behaves like black cube
            case FaceStatus.Enhanced:
                return type == CubeType.Normal ? CubeType.Blue : type; // Normal becomes blue
            default:
                return type; // No change
        }
    }

    public bool CanBeCaptured()
    {
        // Check if current active face allows capture
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.Corrupted:
                return false; // Corrupted face = acts like black cube
            default:
                return type != CubeType.Black; // Normal capture rules
        }
    }

    public bool ShouldCreateDetonation()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();
        return activeStatus == FaceStatus.Enhanced ||
               type == CubeType.Blue;
    }

    private void ProcessFaceDurations()
    {
        for (int i = 0; i < 6; i++)
        {
            if (faceDurations[i] > 0)
            {
                faceDurations[i]--;
                if (faceDurations[i] == 0)
                {
                    // Status expired
                    faceStatuses[i] = FaceStatus.None;
                    faceColors[i] = Color.white;
                }
            }
        }
        UpdateFaceVisuals();
    }

    private void InitializeFaceSystem()
    {
        // Initialize all faces to no status
        for (int i = 0; i < 6; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
        }

        if (showFaceIndicators)
        {
            CreateFaceIndicators();
        }
    }

    private void CreateFaceIndicators()
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = $"FaceIndicator_{(CubeFace)i}";
            indicator.transform.SetParent(transform);

            // Position on cube surface
            PositionFaceIndicator(indicator, (CubeFace)i);

            // Make transparent
            Renderer renderer = indicator.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1, 1, 1, 0.8f);
            renderer.material = mat;

            // Remove collider
            Destroy(indicator.GetComponent<Collider>());

            indicator.SetActive(false); // Hidden by default
            faceIndicators[i] = indicator;
        }
    }

    private void PositionFaceIndicator(GameObject indicator, CubeFace face)
    {
        float offset = 0.51f; // Just outside cube surface
        Vector3 scale = new Vector3(0.7f, 0.7f, 1f);

        switch (face)
        {
            case CubeFace.Bottom:
                indicator.transform.localPosition = new Vector3(0, -offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(90, 0, 0);
                break;
            case CubeFace.Top:
                indicator.transform.localPosition = new Vector3(0, offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case CubeFace.Front:
                indicator.transform.localPosition = new Vector3(0, 0, offset);
                break;
            case CubeFace.Back:
                indicator.transform.localPosition = new Vector3(0, 0, -offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
        }

        indicator.transform.localScale = scale;
    }

    private void UpdateFaceVisuals()
    {
        if (!showFaceIndicators || faceIndicators == null) return;

        CubeFace currentDownFace = GetCurrentDownFace();

        for (int i = 0; i < 6; i++)
        {
            if (faceIndicators[i] == null) continue;

            bool hasStatus = faceStatuses[i] != FaceStatus.None;
            faceIndicators[i].SetActive(hasStatus);

            if (hasStatus)
            {
                Renderer renderer = faceIndicators[i].GetComponent<Renderer>();
                Material mat = renderer.material;

                // Set color
                mat.color = faceColors[i];

                // Highlight active (down-facing) status
                if ((CubeFace)i == currentDownFace)
                {
                    mat.color = new Color(faceColors[i].r, faceColors[i].g, faceColors[i].b, 1f);
                    // Add glow effect
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", faceColors[i] * 0.3f);
                }
                else
                {
                    mat.color = new Color(faceColors[i].r, faceColors[i].g, faceColors[i].b, 0.6f);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    #endregion

}