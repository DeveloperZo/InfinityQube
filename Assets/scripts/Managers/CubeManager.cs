using UnityEngine;
using System.Collections;
using static Enumerations;

public class CubeManager : MonoBehaviour
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
    private float tileScale = 3f;
    private float tileSize = 1f;

    public void Init(GridManager gridManager, CubeData cubeData, float spawnHeight = 2f)
    {
        grid = gridManager;
        tileSize = grid.TileSize;

        name = cubeData.Definition?.name ?? cubeData.type.ToString();
        type = cubeData.type;
        position = cubeData.position;
        level = cubeData.level;
        isRainingCube = cubeData.isRainingCube;
        moveCountRemaining = cubeData.moveCountRemaining;
        currentHitPoints = cubeData.Definition.maxHitPoints;
        maxHitPoints = cubeData.Definition.maxHitPoints;

        material = cubeData.Definition?.material;
        prefab = cubeData.Definition?.prefab;

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

        Vector3 worldPos = grid.GridToWorldPosition(position.x, position.y, spawnHeight);
        transform.position = worldPos;

        Debug.Log($"Cube {type} initialized at grid ({position.x}, {position.y}) -> world {worldPos}, HP: {currentHitPoints}/{maxHitPoints}");

        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        gameObject.name = name;

        InitializeFaceSystem();
        InitializeFaceMapping();
        SetupPhysics();
        UpdateDamageVisual();
    }

    public bool TakeDamage(int damage = 1)
    {
        if (isDestroyed) return false;

        currentHitPoints -= damage;
        Debug.Log($"{type} cube at ({position.x}, {position.y}) took {damage} damage. HP: {currentHitPoints}/{maxHitPoints}");

        UpdateDamageVisual();

        if (currentHitPoints <= 0)
        {
            return true;
        }

        return false;
    }

    private void UpdateDamageVisual()
    {
        // Damage visual implementation
    }

    private void SetupPhysics()
    {
        if (!usePhysics) return;

        cubeCollider = GetComponent<Collider>();
        if (cubeCollider == null)
        {
            cubeCollider = gameObject.AddComponent<BoxCollider>();
        }

        cubeCollider.isTrigger = false;
        Debug.Log($"Collider setup complete for {name} cube");
    }

    public void ResetMovementState()
    {
        isMoving = false;
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        StopAllCoroutines();

        // Clean up face indicators
        for (int i = 0; i < faceIndicators.Length; i++)
        {
            if (faceIndicators[i] != null)
            {
                Destroy(faceIndicators[i]);
                faceIndicators[i] = null;
            }
        }
    }

    public bool MoveForward()
    {
        if (isMoving || isDestroyed) return true;

        Debug.Log($"Moving cube {GetEffectiveType()} from ({position.x}, {position.y}) forward");

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

        position.y -= 1;
        moveCount++;

        RotateFaceMapping();
        ProcessFaceDurations();

        Debug.Log($"Cube moved to ({position.x}, {position.y}), move count: {moveCount}");

        StartCoroutine(AnimateMove(position));

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

        Vector3 start = transform.position;
        Vector3 end = grid.GridToWorldPosition(newPos.x, newPos.y, 2f);

        Debug.Log($"Animating cube from {start} to {end} (grid pos {newPos})");

        WaveManager waveManager = FindObjectOfType<WaveManager>();
        float actualMoveDuration = moveDuration;

        if (waveManager != null)
        {
            float currentInterval = waveManager.isSpeedingUp ? waveManager.fastMoveInterval :
                                   (waveManager.CurrentWave?.moveInterval ?? waveManager.normalMoveInterval);
            actualMoveDuration = Mathf.Min(moveDuration, currentInterval * 0.8f);
        }

        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90f, 0f, 0f);

        while (elapsed < actualMoveDuration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / actualMoveDuration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        if (!isDestroyed)
        {
            transform.position = end;
            transform.rotation = Quaternion.identity;
        }

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileSize * 1.05f, tileSize * 0.9f, tileSize * 1.05f);

        float squashTime = Mathf.Min(squashDuration, actualMoveDuration * 0.3f);
        yield return new WaitForSeconds(squashTime);

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

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


    #region Face Painting System - FIXED

    public CubeFace GetCurrentDownFace()
    {
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
                return CubeType.Black;
            case FaceStatus.Enhanced:
                return type == CubeType.Normal ? CubeType.Blue : type;
            default:
                return type;
        }
    }

    public bool CanBeCaptured()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.Corrupted:
                return false;
            default:
                return type != CubeType.Black;
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
        faceIndicators[faceIndex].SetActive(true);
        UpdateFaceVisuals();
        Debug.Log($"Painted {face} of cube at ({position.x}, {position.y}) with {status} status, duration: {duration}");
    }

    public void PaintCurrentDownFace(FaceStatus status, Color color, int duration = -1)
    {
        CubeFace downFace = GetCurrentDownFace();
        PaintFace(downFace, status, color, duration);
    }

    private void ProcessFaceDurations()
    {
        bool anyChanged = false;
        for (int i = 0; i < 4; i++)
        {
            if (faceDurations[i] > 0)
            {
                faceDurations[i]--;
                if (faceDurations[i] == 0)
                {
                    faceStatuses[i] = FaceStatus.None;
                    faceColors[i] = Color.white;
                    anyChanged = true;
                    faceIndicators[i].SetActive(false);
                    Debug.Log($"Face {(CubeFace)i} paint status expired on cube at ({position.x}, {position.y})");
                }
            }
        }

        if (anyChanged)
        {
            UpdateFaceVisuals();
        }
    }

    private void InitializeFaceSystem()
    {
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
            indicator.name = $"FaceIndicator_{(CubeFace)i}_{position.x}_{position.y}";
            indicator.transform.SetParent(transform);

            // Position and orient the face indicator correctly
            PositionFaceIndicator(indicator, (CubeFace)i);

            // Set up renderer with proper material
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
        float offset = (0.55f); // Very close to cube surface, just barely hovering
        Vector3 scale = new Vector3( 1f, 1f, 1f); // Larger indicators for better visibility

        // Position based on the ORIGINAL face position on the cube
        switch (originalFace)
        {
            case CubeFace.Bottom: // Original bottom face (Y-)
                indicator.transform.localPosition = new Vector3(0, -offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(90, 180, 0); // Face up (outward from bottom)
                break;

            case CubeFace.Top: // Original top face (Y+)
                indicator.transform.localPosition = new Vector3(0, offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(-90, 180, 0); // Face down (outward from top)
                break;

            case CubeFace.Front: // Original front face (Z+)
                indicator.transform.localPosition = new Vector3(0, 0, offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face toward camera (outward from front)
                break;

            case CubeFace.Back: // Original back face (Z-)
                indicator.transform.localPosition = new Vector3(0, 0, -offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 0, 0); // Face toward camera (outward from back)
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

        UpdateFaceIndicatorPositions();
        
    }

    private void UpdateFaceIndicatorPositions()
    {
        if (!showFaceIndicators || faceIndicators == null) return;

        float offset = (0.55f);

        for (int i = 0; i < 4; i++)
        {
            if (faceIndicators[i] == null) continue;

            CubeFace originalFace = (CubeFace)i;

            // Find where this original face is currently positioned
            FacePosition currentPosition = GetFacePosition(originalFace);

            // Position the indicator based on the current FacePosition
            Vector3 newPosition = Vector3.one;
            Quaternion newRotation = Quaternion.identity;

            switch (currentPosition)
            {
                case FacePosition.Down:
                    newPosition = new Vector3(0, -offset, 0);
                    newRotation = Quaternion.Euler(90, 180, 0);
                    break;
                case FacePosition.Up:
                    newPosition = new Vector3(0, offset, 0);
                    newRotation = Quaternion.Euler(-270, 180, 0);
                    break;
                case FacePosition.Forward:
                    newPosition = new Vector3(0, 0, offset);
                    newRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case FacePosition.Back:
                    newPosition = new Vector3(0, 0, -offset);
                    newRotation = Quaternion.Euler(0, 0, 0);
                    break;
            }

            faceIndicators[i].transform.localPosition = newPosition;
            faceIndicators[i].transform.localRotation = newRotation;
            faceIndicators[i].GetComponent<Renderer>().material.color = faceColors[i];
        }
    }

    private FacePosition GetFacePosition(CubeFace originalFace)
    {
        // Find where this original face is currently positioned
        for (int i = 0; i < 4; i++)
        {
            if (currentFaceMapping[i] == originalFace)
            {
                switch (i)
                {
                    case 0: return FacePosition.Down;
                    case 1: return FacePosition.Up;
                    case 2: return FacePosition.Forward;
                    case 3: return FacePosition.Back;
                    default: return FacePosition.Down;
                }
            }
        }
        return FacePosition.Down; // Fallback
    }
    private void EnsureFaceIndicatorOrientation(GameObject indicator, CubeFace originalFace)
    {
        if (indicator == null) return;

        // Reset the indicator's rotation to always face outward from its assigned face
        // This ensures that no matter how the cube has rotated, the painted surface is visible
        switch (originalFace)
        {
            case CubeFace.Bottom:
                // For bottom face, the quad should face upward (away from cube bottom)
                indicator.transform.localRotation = Quaternion.Euler(90, 180, 0);
                break;

            case CubeFace.Top:
                // For top face, the quad should face downward (away from cube top)
                indicator.transform.localRotation = Quaternion.Euler(-90, 180, 0);
                break;

            case CubeFace.Front:
                // For front face, the quad should face forward (away from cube front)
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;

            case CubeFace.Back:
                // For back face, the quad should face backward (away from cube back)
                indicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
        }
    }

    private System.Collections.IEnumerator PulseFaceIndicator(GameObject indicator, int faceIndex)
    {
        if (indicator == null || isDestroyed) yield break;

        // Only pulse if this is still the active face
        CubeFace activeFace = GetCurrentDownFace();
        if ((int)activeFace != faceIndex) yield break;

        Vector3 originalScale = indicator.transform.localScale;
        Vector3 pulseScale = originalScale * 1.2f;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration && indicator != null && indicator.activeInHierarchy && !isDestroyed)
        {
            // Check if still the active face
            if ((int)GetCurrentDownFace() != faceIndex) break;

            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 2f, 1f); // Pulse twice per duration
            if (indicator != null)
            {
                indicator.transform.localScale = Vector3.Lerp(originalScale, pulseScale, t * 0.3f);
            }
            yield return null;
        }

        if (indicator != null && !isDestroyed)
        {
            indicator.transform.localScale = originalScale;
        }
    }

    // Debug and testing methods
    public void TestPaintFace(CubeFace face, FaceStatus status)
    {
        Color color = status == FaceStatus.Corrupted ? Color.black : Color.blue;
        PaintFace(face, status, color, 5);
        Debug.Log($"Test painted {face} with {status} status");
    }

    public void DebugShowAllFaces()
    {
        if (!showFaceIndicators) return;

        Color[] testColors = { Color.red, Color.green, Color.blue, Color.yellow };
        FaceStatus[] testStatuses = { FaceStatus.Corrupted, FaceStatus.Enhanced, FaceStatus.Corrupted, FaceStatus.Enhanced };

        for (int i = 0; i < 4; i++)
        {
            PaintFace((CubeFace)i, testStatuses[i], testColors[i], -1);
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

        // Update visuals immediately after rotation to ensure proper orientation
        UpdateFaceVisuals();
    }

    // Public methods for external testing
    public void SetFaceStatus(CubeFace face, FaceStatus status, int duration = -1)
    {
        Color color = status == FaceStatus.Corrupted ? Color.red :
                     status == FaceStatus.Enhanced ? Color.blue : Color.white;
        PaintFace(face, status, color, duration);
    }

    public FaceStatus GetFaceStatus(CubeFace face)
    {
        return faceStatuses[(int)face];
    }

    public int GetFaceDuration(CubeFace face)
    {
        return faceDurations[(int)face];
    }

    public void ClearAllFaces()
    {
        for (int i = 0; i < 4; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
        }
        UpdateFaceVisuals();
        Debug.Log($"Cleared all face statuses on cube at ({position.x}, {position.y})");
    }

    #endregion
}