using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Enumerations;

public class CubeManager : MonoBehaviour, IManagerDebugInterface
{
    [Header("Cube Properties")]
    [SerializeField] public int level = 1;
    [SerializeField] public Vector2Int position;
    [SerializeField] public CubeType type;
    [SerializeField] public Material material;
    [SerializeField] public GameObject prefab;
    [SerializeField] public float spawnHeight;
    [SerializeField] public int currentHitPoints = 3;
    [SerializeField] public int maxHitPoints = 3;
    [SerializeField] public int moveCount = 0;
    [System.NonSerialized] private CubeData cubeData;
    
    [Header("Movement Direction")]
    [SerializeField] private MovementDirection currentDirection = MovementDirection.Down;
    public MovementDirection CurrentDirection => currentDirection; // Public accessor
    [SerializeField] private int currentSegmentIndex = 0; // Which segment this cube is on

    [Header("Audio Configuration")]
    [SerializeField] 
    [Tooltip("ScriptableObject containing cube-specific audio configuration. Will use AudioManager's configuration if not assigned.")]
    private CubeAudioConfiguration cubeAudioConfig;
    
    [SerializeField]
    [Tooltip("AudioSource component for playing cube-specific audio. Will be created automatically if not assigned.")]
    private AudioSource cubeAudioSource;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.25f;
    public bool isRainingCube = false;

    [Header("Physics")]
    [SerializeField] public bool usePhysics = true;
    [SerializeField] private Rigidbody cubeRigidbody;
    [SerializeField] private Collider cubeCollider;

    [Header("Face Painting System")]
    [SerializeField] private FaceStatus[] faceStatuses = new FaceStatus[4]; // 4 cube faces
    [SerializeField] private Color[] faceColors = new Color[4]; // Visual colors for each face
    [SerializeField] private int[] faceDurations = new int[4]; // Remaining duration for each face (-1 = permanent)
    [SerializeField] private int[] faceCharges = new int[4]; // Grid touch charges for each face (default 1)
    [SerializeField] private GameObject[] faceIndicators = new GameObject[4]; // Visual indicators
    [SerializeField] private bool showFaceIndicators = true;
    private CubeFace[] currentFaceMapping = new CubeFace[4];
    
    // Face painting helper (SRP extraction)
    private CubeFacePainter facePainter;
    
    // Audio handler (SRP extraction)
    private CubeAudioHandler audioHandler;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs ;

    private GridManager grid;
    private PlayerActionManager playerActionManager;
    public bool isMoving = false;
    public bool isDestroyed = false;
    public bool isPlayerCube = false;
    public bool isMatrixCube = false; // True for Matrix cubes that capture in an area
    public bool stoppedAtEdge = false; // SEGMENT CONTROLLER: Cube has stopped at segment edge, waiting for transition
    [SerializeField] private bool isPhaseable = false; // Task 7: Phaseable state for resonance system
    [SerializeField] private int phaseableMovesRemaining = 0; // Task 7: Remaining moves in phaseable state
    
    // SEGMENT CONTROLLER: Track which segment this cube is on
    private GridSegmentController currentSegment;
    public GridSegmentController CurrentSegment => currentSegment;
    
    [Header("Materials")]
    [SerializeField] private Material phaseableMaterial; // Assign CosmicBlack_Transparent in Inspector
    [SerializeField] private Material playerCubeMaterial; // Assign translucent material for player cubes
    private Material originalMaterial; // Store original material before effects
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    public int moveCountRemaining = 0;
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
        // Multi-hit system: Recursion cubes require 2 hits to capture
        if (type == CubeType.Recursion && !isPlayerCube)
        {
            maxHitPoints = 2; // Wave Recursion cubes require 2 hits
            currentHitPoints = 2;
        }
        else
        {
            currentHitPoints = cubeData.Definition.maxHitPoints;
            maxHitPoints = cubeData.Definition.maxHitPoints;
        }

        material = cubeData.Definition?.material;
        prefab = cubeData.Definition?.prefab;

        // Add fallback material assignment for debug-spawned cubes
        if (material == null && grid != null)
        {
            material = grid.GetCubeTypeMaterial(type);
            this.Log($"Used fallback material for {type} cube from GridManager", EnableDebugLogs);
        }
        
        // Final check and warning if material is still null
        if (material == null)
        {
            this.LogWarning($"No material found for {type} cube - visual effects may not work correctly", EnableDebugLogs);
        }

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

        Vector3 worldPos = grid.GridToWorldPosition(position.x, position.y, spawnHeight);
        transform.position = worldPos;

        this.Log($"Cube {type} initialized at grid ({position.x}, {position.y}) -> world {worldPos}, HP: {currentHitPoints}/{maxHitPoints}", EnableDebugLogs);

        playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        gameObject.name = name;

        // Initialize face painter (SRP extraction)
        facePainter = new CubeFacePainter(
            this, faceStatuses, faceColors, faceDurations, faceCharges,
            faceIndicators, currentFaceMapping, showFaceIndicators, enableDebugLogs);
        facePainter.InitializeFaceSystem();
        facePainter.InitializeFaceMapping();
        
        SetupPhysics();
        SetupAudioSystem();
        UpdateDamageVisual();
        
        // Fire spawn event
        GameEvents.FireCubeSpawn(position, type);
        this.Log($"Fired GameEvents.OnCubeSpawn for {type} cube at ({position.x}, {position.y})", EnableDebugLogs);
    }
    
    // NOTE: ConfigurePath method removed - use SetSegmentController for segment-based direction
    
    /// <summary>
    /// Sets which segment this cube is on.
    /// Call this when spawning cubes on segment 1+.
    /// </summary>
    public void SetSegment(int segmentIndex)
    {
        currentSegmentIndex = segmentIndex;
        this.Log($"Cube {type} set to segment {segmentIndex}", EnableDebugLogs);
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets which segment this cube is currently on.
    /// </summary>
    public int CurrentSegmentIndex => currentSegmentIndex;
    
    /// <summary>
    /// SEGMENT CONTROLLER: Sets which segment controller this cube is on.
    /// </summary>
    public void SetSegmentController(GridSegmentController segment)
    {
        currentSegment = segment;
        if (segment != null)
        {
            currentSegmentIndex = segment.segmentIndex;
            currentDirection = segment.localDirection;
            this.Log($"Cube {type} set to segment controller {segment.segmentIndex}, direction: {currentDirection}", EnableDebugLogs);
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Changes the cube's movement direction.
    /// Used when wave transitions to move toward next segment.
    /// </summary>
    public void SetMovementDirection(MovementDirection direction)
    {
        currentDirection = direction;
        stoppedAtEdge = false; // Reset edge stop state since we're now moving in a new direction
        this.Log($"Cube {type} direction changed to {direction}", EnableDebugLogs);
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if cube is escaping based on current direction and position.
    /// For segment controllers, checks in the direction of movement only.
    /// Cubes can be "above" the grid (y >= height) when approaching from a segment transition.
    /// </summary>
    private bool CheckEscapeCondition()
    {
        // SEGMENT CONTROLLER: Check escape based on movement direction
        // Only escape if moving OFF the grid in the direction of travel
        if (currentSegment != null)
        {
            switch (currentDirection)
            {
                case MovementDirection.Down:
                    // Escaping when moving past bottom edge
                    return position.y < 0;
                case MovementDirection.Up:
                    // Escaping when moving past top edge (but allow being above grid when moving down)
                    return position.y >= currentSegment.height && currentDirection == MovementDirection.Up;
                case MovementDirection.Right:
                    // Escaping when moving past right edge
                    return position.x >= currentSegment.width;
                case MovementDirection.Left:
                    // Escaping when moving past left edge
                    return position.x < 0;
                default:
                    // Fallback: check X bounds and bottom edge only
                    return position.y < 0 || position.x < 0 || position.x >= currentSegment.width;
            }
        }
        
        // Legacy: Check against grid bounds
        switch (currentDirection)
        {
            case MovementDirection.Down:
                return position.y < 0;
            case MovementDirection.Up:
                return position.y >= grid.Height;
            case MovementDirection.Right:
                return position.x >= grid.Width;
            case MovementDirection.Left:
                return position.x < 0;
            default:
                return position.y < 0 || position.x < 0 || position.x >= grid.Width;
        }
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the next position based on the current movement direction.
    /// </summary>
    private Vector2Int GetNextPositionInDirection(MovementDirection direction)
    {
        switch (direction)
        {
            case MovementDirection.Down:
                return new Vector2Int(position.x, position.y - 1);
            case MovementDirection.Up:
                return new Vector2Int(position.x, position.y + 1);
            case MovementDirection.Right:
                return new Vector2Int(position.x + 1, position.y);
            case MovementDirection.Left:
                return new Vector2Int(position.x - 1, position.y);
            default:
                return new Vector2Int(position.x, position.y - 1);
        }
    }
    
    /// <summary>
    /// ADVANCED GRID: Gets the rotation needed for cube animation when moving in a direction.
    /// </summary>
    private Quaternion GetRotationForDirection(MovementDirection direction)
    {
        switch (direction)
        {
            case MovementDirection.Down:
                return Quaternion.Euler(-90f, 0f, 0f);
            case MovementDirection.Up:
                return Quaternion.Euler(90f, 0f, 0f);
            case MovementDirection.Right:
                return Quaternion.Euler(0f, 0f, -90f);
            case MovementDirection.Left:
                return Quaternion.Euler(0f, 0f, 90f);
            default:
                return Quaternion.Euler(-90f, 0f, 0f);
        }
    }

    public bool TakeDamage(int damage = 1)
    {
        if (isDestroyed) return false;

        currentHitPoints -= damage;
        this.Log($"{type} cube at ({position.x}, {position.y}) took {damage} damage. HP: {currentHitPoints}/{maxHitPoints}", EnableDebugLogs);

        UpdateDamageVisual();

        if (currentHitPoints <= 0)
        {
            // Play destruction sound when cube is destroyed by damage
            PlayDestructionSound();
            return true;
        }

        return false;
    }

    public void UpdateDamageVisual()
    {
        // Only apply damage visuals to recursion cubes
        if (type != CubeType.Recursion) return;

        // Calculate damage ratio (1.0 = full health, 0.0 = destroyed)
        float damageRatio = maxHitPoints > 0 ? (float)currentHitPoints / maxHitPoints : 1.0f;

        // Apply scale effect - cube shrinks as it takes damage
        float scaleMultiplier = Mathf.Lerp(0.85f, 1.0f, damageRatio);
        Vector3 targetScale = Vector3.one * tileSize * scaleMultiplier;
        
        // Only modify scale if not currently animating movement
        if (!isMoving)
        {
            transform.localScale = targetScale;
        }

        // Apply damage material effect only when damaged
        if (damageRatio < 1.0f)
        {
            Renderer cubeRenderer = GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                // Create new material based on original
                Material damagedMaterial = new Material(material != null ? material : cubeRenderer.material);
                
                // Lerp between gray (damaged) and original color based on damage ratio
                Color originalColor = material != null ? material.color : Color.white;
                Color damagedColor = Color.Lerp(Color.gray, originalColor, damageRatio);
                damagedMaterial.color = damagedColor;
                
                // Slightly reduce metallic and smoothness to show wear
                damagedMaterial.SetFloat("_Metallic", Mathf.Lerp(0.1f, 0.5f, damageRatio));
                damagedMaterial.SetFloat("_Smoothness", Mathf.Lerp(0.2f, 0.8f, damageRatio));
                
                cubeRenderer.material = damagedMaterial;
                
                this.Log($"Recursion cube at ({position.x}, {position.y}) visual damage updated: {damageRatio:F2} health ratio", EnableDebugLogs);
            }
        }
        else if (material != null)
        {
            // Restore original material when at full health
            Renderer cubeRenderer = GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material = material;
            }
        }
    }

    private void SetupPhysics()
    {
        if (!usePhysics) return;

        cubeCollider = GetComponent<Collider>();
        if (cubeCollider == null)
        {
            cubeCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Player cubes should be triggers to allow player to pass through
        // MeshColliders require convex to be true to be triggers
        if (isPlayerCube)
        {
            if (cubeCollider is MeshCollider meshCollider)
            {
                meshCollider.convex = true;
            }
            cubeCollider.isTrigger = true;
        }
        else
        {
            cubeCollider.isTrigger = false;
        }
        
        this.Log($"Collider setup complete for {name} cube (isTrigger: {cubeCollider.isTrigger})", EnableDebugLogs);
    }

    /// <summary>
    /// Configures physics for player cubes - makes collider a trigger so player can pass through.
    /// Should be called after isPlayerCube is set to true.
    /// </summary>
    public void ConfigurePlayerCubePhysics()
    {
        //if (!usePhysics) return;

        cubeCollider = GetComponent<Collider>();
        if (cubeCollider == null)
        {
            cubeCollider = gameObject.AddComponent<BoxCollider>();
        }

        // MeshColliders require convex to be true to be triggers
        if (cubeCollider is MeshCollider meshCollider)
        {
            meshCollider.convex = true;
        }

        cubeCollider.isTrigger = true;
        this.Log($"Player cube collider configured as trigger for {name} (convex: {(cubeCollider is MeshCollider mc ? mc.convex.ToString() : "N/A")})", EnableDebugLogs);
    }

    /// <summary>
    /// Applies the translucent material for player cubes.
    /// Call this after spawning a player cube.
    /// </summary>
    public void ApplyPlayerCubeMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        if (playerCubeMaterial != null)
        {
            renderer.material = playerCubeMaterial;
        }
        else
        {
            // Fallback: create translucent material at runtime
            Material baseMaterial = renderer.material;
            if (baseMaterial == null) return;

            Material translucentMaterial = new Material(baseMaterial);
            Color color = translucentMaterial.color;
            color.a = 0.35f;
            translucentMaterial.color = color;

            if (translucentMaterial.HasProperty("_Mode"))
            {
                translucentMaterial.SetFloat("_Mode", 3);
                translucentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                translucentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                translucentMaterial.SetInt("_ZWrite", 0);
                translucentMaterial.DisableKeyword("_ALPHATEST_ON");
                translucentMaterial.EnableKeyword("_ALPHABLEND_ON");
                translucentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                translucentMaterial.renderQueue = 3000;
            }

            renderer.material = translucentMaterial;
            Debug.LogWarning($"[CubeManager] playerCubeMaterial not assigned on {name} - using runtime fallback");
        }
    }

    /// <summary>
    /// Applies the wave cube material (opaque).
    /// Call this when a player cube joins the wave.
    /// </summary>
    public void ApplyWaveCubeMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"[CubeManager] ApplyWaveCubeMaterial: No renderer found on {name}");
            return;
        }

        string currentMat = renderer.material != null ? renderer.material.name : "null";
        
        if (material != null)
        {
            renderer.material = material;
            Debug.Log($"[CubeManager] ApplyWaveCubeMaterial: {name} changed from '{currentMat}' to '{material.name}'");
        }
        else
        {
            Debug.LogWarning($"[CubeManager] waveCubeMaterial not assigned on {name} (current: '{currentMat}'). Assign 'Wave Cube Material' on the prefab.");
        }
    }

    /// <summary>
    /// Sets up the audio system for this cube by configuring AudioSource and CubeAudioConfiguration
    /// </summary>
    private void SetupAudioSystem()
    {
        // Set up AudioSource component
        if (cubeAudioSource == null)
        {
            cubeAudioSource = GetComponent<AudioSource>();
        }
        
        if (cubeAudioSource == null)
        {
            cubeAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for cube-specific audio playback
        ConfigureCubeAudioSource();
        
        // Set up audio configuration
        if (cubeAudioConfig == null)
        {
            // Try to get from AudioManager if not assigned locally
            if (AudioManager.Instance != null && AudioManager.Instance.cubeAudioConfiguration != null)
            {
                cubeAudioConfig = AudioManager.Instance.cubeAudioConfiguration;
                this.Log($"Using CubeAudioConfiguration from AudioManager for {type} cube", EnableDebugLogs);
            }
            else
            {
                this.LogWarning($"No CubeAudioConfiguration available for {type} cube - audio will not play", EnableDebugLogs);
            }
        }
        
        // Validate audio configuration
        if (cubeAudioConfig != null)
        {
            var audioData = cubeAudioConfig.GetAudioData(type);
            if (audioData == null || !audioData.HasAnyAudioClips())
            {
                this.LogWarning($"No audio clips configured for cube type {type} in CubeAudioConfiguration", EnableDebugLogs);
            }
        }
        
        this.Log($"Audio system setup complete for {type} cube (AudioSource: {cubeAudioSource != null}, Config: {cubeAudioConfig != null})", EnableDebugLogs);
        
        // Initialize audio handler (SRP extraction)
        audioHandler = new CubeAudioHandler(this, cubeAudioSource, cubeAudioConfig, enableDebugLogs);
    }
    
    /// <summary>
    /// Configures the AudioSource component for optimal cube audio playback
    /// </summary>
    private void ConfigureCubeAudioSource()
    {
        if (cubeAudioSource == null) return;
        
        // Configure for 3D spatial audio
        cubeAudioSource.playOnAwake = false;
        cubeAudioSource.loop = false;
        cubeAudioSource.spatialBlend = 1f; // Full 3D spatial sound
        cubeAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        cubeAudioSource.maxDistance = 30f; // Reasonable distance for cube sounds
        cubeAudioSource.minDistance = 1f;
        cubeAudioSource.spread = 0f; // Directional sound
        cubeAudioSource.dopplerLevel = 0.1f; // Minimal doppler effect
        
        // Set default volume and pitch
        cubeAudioSource.volume = 0.8f;
        cubeAudioSource.pitch = 1f;
    }

    public void ResetMovementState()
    {
        isMoving = false;
    }

    #region Cube Audio Playback (Facade - delegates to CubeAudioHandler)
    
    public void PlayLandingSound()
        => audioHandler?.PlayLandingSound();

    public void PlayCaptureSound()
        => audioHandler?.PlayCaptureSound();

    public void PlayDestructionSound()
        => audioHandler?.PlayDestructionSound();

    public void PlaySpecialEffectSound()
        => audioHandler?.PlaySpecialEffectSound();

    public void OnCubeCapture()
        => audioHandler?.OnCubeCapture();

    public bool IsAudioSystemReady()
        => audioHandler?.IsAudioSystemReady() ?? false;

    public string GetAudioDiagnostics()
        => audioHandler?.GetAudioDiagnostics() ?? "Audio handler not initialized";
    
    #endregion

    private void OnDestroy()
    {
        isDestroyed = true;
        StopAllCoroutines();
        
        // Play destruction sound before cleanup
        if (cubeAudioSource != null && !cubeAudioSource.isPlaying)
        {
            PlayDestructionSound();
        }

        // Clean up face indicators via painter
        facePainter?.CleanupIndicators();
        
        // Clean up audio via handler
        audioHandler?.Cleanup();
    }

    /// <summary>
    /// Moves the cube forward by one position along the current path direction.
    /// ADVANCED GRID: Movement direction may change at turn points (L, C, S shapes).
    /// CUBE ESCAPE MECHANIC: When a cube moves outside grid bounds, it "escapes".
    /// Escape triggers failure conditions and affects stage progression.
    /// </summary>
    /// <returns>True if cube continues moving, false if cube escapes or is destroyed</returns>
    public bool MoveForward()
    {
        if (isMoving || isDestroyed) return true;
        
        // SEGMENT CONTROLLER: If stopped at edge, don't move
        if (stoppedAtEdge)
        {
            this.Log($"🛑 Cube {GetEffectiveType()} at ({position.x}, {position.y}) is stopped at edge - skipping move", EnableDebugLogs);
            return true; // Still alive, just not moving
        }

        this.Log($"Moving cube {GetEffectiveType()} from ({position.x}, {position.y}) forward (direction: {currentDirection})", EnableDebugLogs);

        // NOTE: GridPath turn point check removed - direction changes are handled by segment transitions
        // Cubes get their direction from their current segment's localDirection

        // SEGMENT CONTROLLER: Check if cube should STOP at segment edge (not escape)
        // Wave transition is handled at the wave level in MoveCubesForward
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null && waveManager.ShouldCubeStopAtEdge(this))
        {
            // Cube is at segment edge - just don't move, no notification needed
            this.Log($"🛑 EDGE: {GetEffectiveType()} at ({position.x}, {position.y}) at segment edge - skipping move", EnableDebugLogs);
            return true; // Cube is still alive, just not moving
        }
        
        // CUBE ESCAPE CONDITION: Check if cube is outside grid bounds based on current direction
        bool isEscaping = CheckEscapeCondition();
        if (isEscaping)
        {
            this.Log($"🚨 CUBE ESCAPE: {GetEffectiveType()} at ({position.x}, {position.y}) is off-grid. Direction: {currentDirection}", EnableDebugLogs);

            if (!isRainingCube || moveCountRemaining <= 0)
            {
                CubeType effectiveType = GetEffectiveType();
                
                // SEGMENT CONTROLLER: Check if we should transition instead of escape
                if (currentSegment != null && waveManager != null)
                {
                    bool shouldTransition = waveManager.HandleCubeAtSegmentEdge(this);
                    if (shouldTransition)
                    {
                        this.Log($"🔄 Cube {effectiveType} queued for segment transition", EnableDebugLogs);
                        
                        // Mark as destroyed BEFORE checking transition ready
                        // This ensures the cube count is accurate
                        isDestroyed = true;
                        
                        // Remove from active cubes list
                        waveManager.RemoveCubeFromActive(this);
                        
                        // Play fall-over effect and destroy
                        SpawnEscapeEffect();
                        Destroy(gameObject);
                        
                        // Check if all cubes are ready for transition
                        waveManager.CheckSegmentTransitionReady();
                        return false;
                    }
                    // If not transitioning, this is a terminal escape - fall through to normal handling
                }

                // ADVANCED GRID: Check if we're on a legacy multi-segment grid
                bool isMultiSegmentGrid = grid != null && grid.HasMultipleSegments;
                
                if (effectiveType == CubeType.Infinity)
                {
                    this.Log("🌟 Infinity cube escaped (special behavior - no penalty)", EnableDebugLogs);
                }
                else if (isMultiSegmentGrid && currentSegment == null)
                {
                    // LEGACY ADVANCED GRID MVP: Skip escape penalty for multi-segment grids
                    this.Log($"🔄 Cube escaped on multi-segment grid - no penalty (transition will occur)", EnableDebugLogs);
                }
                else
                {
                    // CUBE ESCAPE PROCESSING: Notify Wave system of cube escape
                    // Wave handles escape counting and failure conditions
                    if (waveManager != null)
                    {
                        waveManager.OnNonBlackCubeProcessed(effectiveType, false);
                        waveManager.OnCubeEscaped(effectiveType); // Wave handles escape logic
                    }
                    
                    // Fire escaped event for general game systems (audio, effects, statistics)
                    GameEvents.FireCubeEscaped(position, effectiveType);
                    this.Log($"🔥 Fired GameEvents.OnCubeEscaped for {effectiveType} cube at ({position.x}, {position.y})", EnableDebugLogs);
                    
                    this.Log($"❌ CUBE ESCAPED: {effectiveType} cube has left the play area", EnableDebugLogs);
                }

                // [POC] Spawn simple escape visual effect
                SpawnEscapeEffect();
                
                // Destroy the escaped cube
                Destroy(gameObject);
                return false; // Cube has escaped - movement chain broken
            }
        }

        Vector2Int oldPosition = position;
        
        // ADVANCED GRID: Move in current direction
        Vector2Int nextPos = GetNextPositionInDirection(currentDirection);
        
        position = nextPos;
        moveCount++;

        // Fire move event
        GameEvents.FireCubeMove(oldPosition, position, type);
        
        // Update face painting system
        facePainter?.RotateFaceMapping();
        facePainter?.ProcessFaceDurations();
        UpdateFaceRotationTracking(); // Enhanced face rotation tracking
        
        // Task 7: Decrement phaseable moves remaining
        if (isPhaseable && phaseableMovesRemaining > 0)
        {
            phaseableMovesRemaining--;
            if (phaseableMovesRemaining <= 0)
            {
                isPhaseable = false;
                UpdatePhaseableVisual(); // Reset visual when phaseable expires
                this.Log($"Phaseable state expired for {type} cube at ({position.x}, {position.y})", EnableDebugLogs);
            }
        }

        oldPosition = new Vector2Int(position.x, position.y + 1); // Previous position for face painting
        
        this.Log($"Cube moved to ({position.x}, {position.y}), move count: {moveCount}", EnableDebugLogs);

        StartCoroutine(AnimateMove(position));

        // Get landing tile - from segment controller if available, otherwise from grid
        Tile landingTile = null;
        if (currentSegment != null)
        {
            landingTile = currentSegment.GetTile(position.x, position.y);
        }
        else if (position.y >= 0 && position.x >= 0 && position.x < grid.Width)
        {
            landingTile = grid.tiles[position.x, position.y];
        }
        
        if (landingTile != null && !isDestroyed)
        {
            landingTile.HandleCubeLanding(this);
        }

        return true;
    }

    /// <summary>
    /// Moves the cube backward (up) by one position. Used for player-spawned cubes moving toward wave cubes.
    /// Player cubes are destroyed when reaching the top of the grid (position.y >= grid.Height).
    /// </summary>
    /// <returns>True if cube continues moving, false if cube is destroyed</returns>
    public bool MoveBackward()
    {
        if (isMoving || isDestroyed) return true;

        this.Log($"Moving player cube {GetEffectiveType()} from ({position.x}, {position.y}) backward", EnableDebugLogs);

        // Check if NEXT position would be out of bounds (top of grid)
        int nextY = position.y + 1;
        if (nextY >= grid.Height || position.x < 0 || position.x >= grid.Width)
        {
            this.Log($"🚨 PLAYER CUBE BOUNDARY: {GetEffectiveType()} at ({position.x}, {position.y}) will exceed grid boundary (next Y={nextY}). Grid bounds: {grid.Width}x{grid.Height}", EnableDebugLogs);

            // Destroy the player cube when it would exceed the top
            Destroy(gameObject);
            return false; // Cube destroyed - movement chain broken
        }

        Vector2Int oldPosition = position;
        position.y = nextY; // Move backward (increasing Y)
        moveCount++;

        // Fire move event
        GameEvents.FireCubeMove(oldPosition, position, type);
        
        // Update face painting system
        facePainter?.RotateFaceMapping();
        facePainter?.ProcessFaceDurations();
        UpdateFaceRotationTracking(); // Enhanced face rotation tracking
        
        // Task 7: Decrement phaseable moves remaining
        if (isPhaseable && phaseableMovesRemaining > 0)
        {
            phaseableMovesRemaining--;
            if (phaseableMovesRemaining <= 0)
            {
                isPhaseable = false;
                UpdatePhaseableVisual(); // Reset visual when phaseable expires
                this.Log($"Phaseable state expired for {type} cube at ({position.x}, {position.y})", EnableDebugLogs);
            }
        }

        this.Log($"Player cube moved to ({position.x}, {position.y}), move count: {moveCount}", EnableDebugLogs);

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
        Vector3 end;
        
        // SEGMENT CONTROLLER: Use segment controller's coordinate system
        if (currentSegment != null)
        {
            end = currentSegment.LocalToWorldPosition(newPos.x, newPos.y, 2f);
        }
        // ADVANCED GRID: Use correct segment's coordinate system (legacy)
        else if (currentSegmentIndex > 0 && grid.HasMultipleSegments && currentSegmentIndex < grid.SegmentCount)
        {
            var segment = grid.Segments[currentSegmentIndex];
            end = segment.LocalToWorldPosition(newPos.x, newPos.y, grid.TileSize, 2f);
        }
        else
        {
            end = grid.GridToWorldPosition(newPos.x, newPos.y, 2f);
        }

        this.Log($"Animating cube from {start} to {end} (grid pos {newPos}, segment {currentSegmentIndex})", EnableDebugLogs);

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        float actualMoveDuration = moveDuration;

        if (waveManager != null)
        {
            float currentInterval = waveManager.isSpeedingUp ? waveManager.fastMoveInterval :
                                   (waveManager.CurrentWave?.moveInterval ?? waveManager.normalMoveInterval);
            actualMoveDuration = Mathf.Min(moveDuration, currentInterval * 0.8f);
        }

        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        
        // SEGMENT CONTROLLER: Get the base rotation for this segment
        Quaternion segmentBaseRot = currentSegment != null ? currentSegment.WorldRotation : Quaternion.identity;
        
        // The tumbling rotation is in local space - multiplying applies it relative to startRot
        Quaternion localTumble = GetRotationForDirection(currentDirection);
        Quaternion endRot = startRot * localTumble;

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
            // SEGMENT CONTROLLER: Reset to segment's base rotation, not identity
            transform.rotation = segmentBaseRot;
            PlayLandingSound();
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


    #region Face Painting System (Facade - delegates to CubeFacePainter)

    public CubeFace GetCurrentDownFace()
        => facePainter?.GetCurrentDownFace() ?? CubeFace.Bottom;

    public FaceStatus GetActiveFaceStatus()
        => facePainter?.GetActiveFaceStatus() ?? FaceStatus.None;

    public FaceStatus GetPredictedFaceStatus(int movesAhead = 1)
        => facePainter?.GetPredictedFaceStatus(movesAhead) ?? FaceStatus.None;

    public bool WillPaintedFaceTouchGrid(int movesAhead = 1)
        => facePainter?.WillPaintedFaceTouchGrid(movesAhead) ?? false;

    public bool HasActiveFaceStatus(FaceStatus status)
        => facePainter?.HasActiveFaceStatus(status) ?? false;

    public CubeType GetEffectiveType()
        => facePainter?.GetEffectiveType() ?? type;

    public bool CanBeCaptured()
        => facePainter?.CanBeCaptured() ?? (type != CubeType.Infinity);

    /// <summary>
    /// Task 7: Gets whether this cube is currently phaseable (can be passed through)
    /// </summary>
    public bool IsPhaseable()
    {
        return isPhaseable && phaseableMovesRemaining > 0;
    }
    
    /// <summary>
    /// Task 7: Sets the phaseable state for this cube (used by resonance system)
    /// </summary>
    public void SetPhaseable(int movesRemaining = 2)
    {
        if (type != CubeType.Infinity)
        {
            this.LogWarning($"Attempted to set phaseable state on non-Infinity cube {type}", EnableDebugLogs);
            return;
        }
        
        isPhaseable = true;
        phaseableMovesRemaining = movesRemaining;
        this.Log($"Set phaseable state for {type} cube at ({position.x}, {position.y}) for {movesRemaining} moves", EnableDebugLogs);
        UpdatePhaseableVisual();
    }
    
    private void UpdatePhaseableVisual()
    {
        Renderer cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer == null) return;
        
        if (isPhaseable && phaseableMovesRemaining > 0)
        {
            if (originalMaterial == null)
                originalMaterial = cubeRenderer.material;
            
            if (phaseableMaterial != null)
                cubeRenderer.material = phaseableMaterial;
            
            this.Log($"[Task 7] Phaseable visual: {type} cube is phaseable ({phaseableMovesRemaining} moves) - TRANSPARENT", EnableDebugLogs);
        }
        else if (originalMaterial != null)
        {
            cubeRenderer.material = originalMaterial;
            this.Log($"[Task 7] Phaseable visual reset for {type} cube", EnableDebugLogs);
        }
    }

    public bool ShouldCreateDetonation()
        => facePainter?.ShouldCreateDetonation() ?? (type == CubeType.Matrix);

    public void PaintFace(CubeFace face, FaceStatus status, Color color, int duration = -1, int charges = 1)
        => facePainter?.PaintFace(face, status, color, duration, charges);

    public bool ConsumeActiveFaceCharge()
        => facePainter?.ConsumeActiveFaceCharge() ?? false;

    public int GetActiveFaceCharges()
        => facePainter?.GetActiveFaceCharges() ?? 0;

    public int GetFaceCharges(CubeFace face)
        => facePainter?.GetFaceCharges(face) ?? 0;

    public void PaintCurrentDownFace(FaceStatus status, Color color, int duration = -1)
        => facePainter?.PaintCurrentDownFace(status, color, duration);

    public void SetFaceStatus(CubeFace face, FaceStatus status, int duration = -1)
        => facePainter?.SetFaceStatus(face, status, duration);

    public FaceStatus GetFaceStatus(CubeFace face)
        => facePainter?.GetFaceStatus(face) ?? FaceStatus.None;

    public int GetFaceDuration(CubeFace face)
        => facePainter?.GetFaceDuration(face) ?? 0;

    public void ClearAllFaces()
        => facePainter?.ClearAllFaces();

    public void TestPaintFace(CubeFace face, FaceStatus status)
        => facePainter?.TestPaintFace(face, status);

    public void DebugShowAllFaces()
        => facePainter?.DebugShowAllFaces();

    public void DebugPrintFaceMapping()
        => facePainter?.DebugPrintFaceMapping();

    public CubeFace GetTopFace()
        => facePainter?.GetTopFace() ?? CubeFace.Top;

    private bool HasCorruptedDownFace()
        => facePainter?.HasCorruptedDownFace() ?? false;

    #endregion

    #region Marker Interaction and Corruption System

    /// <summary>
    /// Called when a marker hits this cube. Handles infinity cube face painting.
    /// </summary>
    public void OnMarkerHit()
    {
        if (type == CubeType.Infinity)
        {
            CubeFace topFace = GetTopFace();
            PaintFace(topFace, FaceStatus.InfinityFace, Color.black, -1);
            CreateMarkerHitEffect();
            this.Log($"Infinity cube at ({position.x}, {position.y}) hit by marker - top face painted for corruption", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Creates visual effect when marker hits cube
    /// </summary>
    private void CreateMarkerHitEffect()
    {
        // Create a temporary visual effect for marker hit
        GameObject hitEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitEffect.name = "MarkerHitEffect";
        hitEffect.transform.position = transform.position + Vector3.up * 0.6f;
        hitEffect.transform.localScale = Vector3.one * 0.3f;
        
        // Set up the effect material
        Renderer renderer = hitEffect.GetComponent<Renderer>();
        Material effectMaterial = new Material(Shader.Find("Standard"));
        effectMaterial.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange color
        effectMaterial.SetFloat("_Mode", 3); // Transparent mode
        effectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        effectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        effectMaterial.SetInt("_ZWrite", 0);
        effectMaterial.EnableKeyword("_ALPHABLEND_ON");
        effectMaterial.renderQueue = 3000;
        renderer.material = effectMaterial;
        
        // Remove collider to avoid interference
        Destroy(hitEffect.GetComponent<Collider>());
        
        // Destroy the effect after a short time
        Destroy(hitEffect, 1f);
    }

    /// <summary>
    /// Checks if this cube should trigger corruption based on its current state
    /// </summary>
    public void CheckForCorruption()
    {
        if (type == CubeType.Infinity && HasCorruptedDownFace())
        {
            // Coordinate with tile for corruption
            Vector2Int currentPos = position;
            if (grid != null && currentPos.x >= 0 && currentPos.x < grid.Width && 
                currentPos.y >= 0 && currentPos.y < grid.Height)
            {
                Tile currentTile = grid.tiles[currentPos.x, currentPos.y];
                if (currentTile != null)
                {
                    // Signal to tile that corruption should occur
                    // This provides the coordination point for the Tile.cs task
                    PrepareCorruptionForTile(currentTile);
                }
            }
        }
    }

    /// <summary>
    /// Prepares corruption coordination with tile system
    /// </summary>
    /// <param name="tile">The tile to coordinate corruption with</param>
    private void PrepareCorruptionForTile(Tile tile)
    {
        // This method provides the coordination point for tile corruption
        // The actual tile corruption logic will be implemented in the Tile.cs task
        this.Log($"Infinity cube at ({position.x}, {position.y}) preparing corruption for tile - ready for Tile.cs implementation", EnableDebugLogs);
        
        // Mark cube as having triggered corruption
        // Additional corruption state tracking can be added here if needed
    }

    /// <summary>
    /// Enhanced face rotation tracking for corruption mechanics
    /// </summary>
    private void UpdateFaceRotationTracking()
    {
        // Enhanced tracking of face rotation for corruption preparation
        // This ensures accurate face state during cube movement
        if (type == CubeType.Infinity)
        {
            // Track which face was painted and is now in the down position
            CheckForCorruption();
        }
    }

    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    /// <summary>
    /// [POC] Spawns a simple visual effect when cube escapes
    /// </summary>
    private void SpawnEscapeEffect()
    {
        
        this.Log("[POC] Spawned escape visual effect", EnableDebugLogs);
    }
    
    public string GetDebugStatus()
    {
        string status = isDestroyed ? "DESTROYED" : (isMoving ? "MOVING" : "IDLE");
        string effectiveType = GetEffectiveType().ToString();
        string audioStatus = IsAudioSystemReady() ? "AudioOK" : "NoAudio";
        return $"Cube {type}->{effectiveType}: @({position.x},{position.y}) HP:{currentHitPoints}/{maxHitPoints} ({status}) Face:{GetCurrentDownFace()} Audio:{audioStatus}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Cube Type"] = type.ToString(),
            ["Effective Type"] = GetEffectiveType().ToString(),
            ["Position"] = $"({position.x}, {position.y})",
            ["World Position"] = transform.position,
            ["Level"] = level,
            ["Hit Points"] = $"{currentHitPoints}/{maxHitPoints}",
            ["Move Count"] = moveCount,
            ["Is Moving"] = isMoving,
            ["Is Destroyed"] = isDestroyed,
            ["Is Raining Cube"] = isRainingCube,
            ["Move Count Remaining"] = moveCountRemaining,
            ["Current Down Face"] = GetCurrentDownFace(),
            ["Active Face Status"] = GetActiveFaceStatus(),
            ["Can Be Captured"] = CanBeCaptured(),
            ["Should Create Detonation"] = ShouldCreateDetonation(),
            ["Use Physics"] = usePhysics,
            ["Show Face Indicators"] = showFaceIndicators,
            ["Material Assigned"] = material != null,
            ["Prefab Assigned"] = prefab != null,
            ["Move Duration"] = moveDuration,
            ["Squash Duration"] = squashDuration,
            ["Rain Speed"] = rainSpeed,
            ["Rain Height"] = rainHeight,
            ["Target Row"] = targetRow,
            ["Tile Size"] = tileSize,
            
            // Audio system debug information
            ["Audio System Ready"] = IsAudioSystemReady(),
            ["AudioSource Configured"] = cubeAudioSource != null,
            ["CubeAudioConfig Assigned"] = cubeAudioConfig != null,
            ["Audio Currently Playing"] = cubeAudioSource?.isPlaying ?? false,
            ["Audio Volume"] = cubeAudioSource?.volume ?? 0f,
            ["Audio Pitch"] = cubeAudioSource?.pitch ?? 1f
        };
    }

    public void ResetToDefaults()
    {
        // Reset cube state to initial values
        isMoving = false;
        isDestroyed = false;
        moveCount = 0;
        moveCountRemaining = 0;
        isRainingCube = false;
        targetRow = -1;
        
        // Reset health to maximum
        currentHitPoints = maxHitPoints;
        
        // Clear all face paintings
        ClearAllFaces();
        
        // Reset physics state
        if (cubeRigidbody != null)
        {
            cubeRigidbody.linearVelocity = Vector3.zero;
            cubeRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Reset scale and rotation
        transform.localScale = new Vector3(tileSize, tileSize, tileSize);
        transform.rotation = Quaternion.identity;
        
        // Reset audio system
        if (cubeAudioSource != null)
        {
            cubeAudioSource.Stop();
            cubeAudioSource.clip = null;
            ConfigureCubeAudioSource(); // Reset to default audio settings
        }
        
        // Reset face mapping to original state
        facePainter?.InitializeFaceMapping();
        
        // Update visuals
        UpdateDamageVisual();
        
        if (EnableDebugLogs)
            this.Log($"Cube at ({position.x}, {position.y}) reset to defaults", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for cube settings
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for cube settings
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}