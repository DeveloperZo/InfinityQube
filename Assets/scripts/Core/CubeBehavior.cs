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
    [SerializeField] public int moveCount = 0;
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
    [SerializeField] private FaceStatus[] faceStatuses = new FaceStatus[4]; // 4 cube faces
    [SerializeField] private Color[] faceColors = new Color[4]; // Visual colors for each face
    [SerializeField] private int[] faceDurations = new int[4]; // Remaining duration for each face (-1 = permanent)
    [SerializeField] private GameObject[] faceIndicators = new GameObject[4]; // Visual indicators
    [SerializeField] private bool showFaceIndicators = true;
    private CubeFace[] currentFaceMapping = new CubeFace[4];

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
        InitializeFaceMapping();

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

        // Check for off-grid conditions
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

        // Update position
        position.y -= 1;
        moveCount++;

        // IMPORTANT: Rotate face mapping when cube moves
        RotateFaceMapping();

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


    // Replace the face indicator methods in CubeBehavior.cs with these fixed versions

    #region Face Painting System - FIXED

    public CubeFace GetCurrentDownFace()
    {
        // Return which original face is currently in the bottom (grid-touching) position
        return currentFaceMapping[0];
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
        return activeStatus == FaceStatus.Enhanced || type == CubeType.Blue;
    }

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

    private void ProcessFaceDurations()
    {
        for (int i = 0; i < 4; i++)
        {
            if (faceDurations[i] > 0)
            {
                faceDurations[i]--;
                if (faceDurations[i] == 0)
                {
                    // Status expired
                    faceStatuses[i] = FaceStatus.None;
                    faceColors[i] = Color.white;
                    Debug.Log($"Face {(CubeFace)i} paint status expired on cube at ({position.x}, {position.y})");
                }
            }
        }
        UpdateFaceVisuals();
    }

    private void InitializeFaceSystem()
    {
        // Initialize all faces to no status
        for (int i = 0; i < 4; i++)
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
        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = $"FaceIndicator_{(CubeFace)i}";
            indicator.transform.SetParent(transform);

            // Position on cube surface - these positions are for the ORIGINAL faces
            PositionFaceIndicator(indicator, (CubeFace)i);

            // Set up renderer with transparent material
            Renderer renderer = indicator.GetComponent<Renderer>();
            Material mat = CreateFaceIndicatorMaterial();
            renderer.material = mat;

            // Remove collider
            Destroy(indicator.GetComponent<Collider>());

            indicator.SetActive(false); // Hidden by default
            faceIndicators[i] = indicator;
        }
    }

    private void PositionFaceIndicator(GameObject indicator, CubeFace originalFace)
    {
        float offset = 0.52f; // Just outside cube surface
        Vector3 scale = new Vector3(0.8f, 0.8f, 1f);

        // Position based on the ORIGINAL face position on the cube
        switch (originalFace)
        {
            case CubeFace.Bottom: // Original bottom face
                indicator.transform.localPosition = new Vector3(0, -offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(90, 0, 0);
                break;

            case CubeFace.Top: // Original top face
                indicator.transform.localPosition = new Vector3(0, offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;

            case CubeFace.Front: // Original front face
                indicator.transform.localPosition = new Vector3(0, 0, offset);
                indicator.transform.localRotation = Quaternion.identity;
                break;

            case CubeFace.Back: // Original back face
                indicator.transform.localPosition = new Vector3(0, 0, -offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
        }

        indicator.transform.localScale = scale;
    }

    private Material CreateFaceIndicatorMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1, 1, 1, 0.8f);

        // Set up for transparency
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private void UpdateFaceVisuals()
    {
        if (!showFaceIndicators || faceIndicators == null) return;

        CubeFace currentDownFace = GetCurrentDownFace();

        for (int i = 0; i < 4; i++)
        {
            if (faceIndicators[i] == null) continue;

            bool hasStatus = faceStatuses[i] != FaceStatus.None;
            bool isActiveFace = (CubeFace)i == currentDownFace;

            // Only show indicators that have a painted status
            faceIndicators[i].SetActive(hasStatus);

            if (hasStatus)
            {
                UpdateFaceIndicatorAppearance(i, isActiveFace);
            }
        }
    }

    private void UpdateFaceIndicatorAppearance(int faceIndex, bool isActiveFace)
    {
        GameObject indicator = faceIndicators[faceIndex];
        if (indicator == null) return;

        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer == null) return;

        // Create new material instance
        Material mat = new Material(renderer.material);

        // Visual style based on face status
        FaceStatus status = faceStatuses[faceIndex];
        switch (status)
        {
            case FaceStatus.Corrupted:
                mat.color = new Color(0.2f, 0.2f, 0.2f, isActiveFace ? 0.9f : 0.6f); // Dark overlay
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.red * (isActiveFace ? 0.3f : 0.1f));
                break;

            case FaceStatus.Enhanced:
                mat.color = new Color(0.3f, 0.6f, 1f, isActiveFace ? 0.8f : 0.5f); // Blue overlay
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.blue * (isActiveFace ? 0.4f : 0.2f));
                break;

            default:
                Color baseColor = faceColors[faceIndex];
                mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, isActiveFace ? 0.8f : 0.5f);
                mat.DisableKeyword("_EMISSION");
                break;
        }

        // Scale active face slightly larger and add pulsing
        if (isActiveFace)
        {
            indicator.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            StartCoroutine(PulseFaceIndicator(indicator));
        }
        else
        {
            indicator.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }

        renderer.material = mat;
    }

    private System.Collections.IEnumerator PulseFaceIndicator(GameObject indicator)
    {
        if (indicator == null) yield break;

        Vector3 originalScale = indicator.transform.localScale;
        Vector3 pulseScale = originalScale * 1.15f;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration && indicator != null && indicator.activeInHierarchy)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 3f, 1f); // Pulse 3 times per duration
            if (indicator != null)
            {
                indicator.transform.localScale = Vector3.Lerp(originalScale, pulseScale, t * 0.3f);
            }
            yield return null;
        }

        if (indicator != null)
        {
            indicator.transform.localScale = originalScale;
        }
    }

    // Debug and testing methods
    public void TestPaintFace(CubeFace face, FaceStatus status)
    {
        Color color = status == FaceStatus.Corrupted ? Color.red : Color.blue;
        PaintFace(face, status, color, 5);
        Debug.Log($"Test painted {face} with {status} status");
    }

    public void DebugShowAllFaces()
    {
        if (!showFaceIndicators) return;

        Color[] testColors = { Color.red, Color.green, Color.blue, Color.yellow };
        for (int i = 0; i < 4; i++)
        {
            PaintFace((CubeFace)i, FaceStatus.Enhanced, testColors[i], -1);
        }

        Debug.Log($"Debug: All faces painted with different colors. Current down face: {GetCurrentDownFace()}");
    }

    public void DebugPrintFaceMapping()
    {
        Debug.Log($"Face Mapping for cube at ({position.x}, {position.y}):");
        Debug.Log($"  Bottom position: {currentFaceMapping[0]}");
        Debug.Log($"  Top position: {currentFaceMapping[1]}");
        Debug.Log($"  Front position: {currentFaceMapping[2]}");
        Debug.Log($"  Back position: {currentFaceMapping[3]}");
        Debug.Log($"  Current down face: {GetCurrentDownFace()}");
        Debug.Log($"  Active face status: {GetActiveFaceStatus()}");
    }

    private void InitializeFaceMapping()
    {
        // Initially, faces are in their original positions
        currentFaceMapping[0] = CubeFace.Bottom;  // Bottom position has original bottom face
        currentFaceMapping[1] = CubeFace.Top;     // Top position has original top face  
        currentFaceMapping[2] = CubeFace.Front;   // Front position has original front face
        currentFaceMapping[3] = CubeFace.Back;    // Back position has original back face

        Debug.Log($"Face mapping initialized for cube at ({position.x}, {position.y})");
    }

    private void RotateFaceMapping()
    {
        // Forward roll rotation: Bottom->Front, Front->Top, Top->Back, Back->Bottom
        CubeFace temp = currentFaceMapping[0]; // Store current bottom
        currentFaceMapping[0] = currentFaceMapping[3]; // Back moves to Bottom
        currentFaceMapping[3] = currentFaceMapping[1]; // Top moves to Back  
        currentFaceMapping[1] = currentFaceMapping[2]; // Front moves to Top
        currentFaceMapping[2] = temp;                  // Bottom moves to Front

        Debug.Log($"Face mapping rotated: Bottom={currentFaceMapping[0]}, Top={currentFaceMapping[1]}, Front={currentFaceMapping[2]}, Back={currentFaceMapping[3]}");
    }

    #endregion

}