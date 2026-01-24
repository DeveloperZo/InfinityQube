using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class PlayerManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Core References")]
    public GridManager grid;
    public Animator _anim;

    [Header("Movement Settings")]
    public float acceleration = 15f;
    public float deceleration = 20f;
    public KeyCode speedUpKey = KeyCode.LeftShift;

    [Header("Marker Settings")]
    public int maxMarkerCharge = 2;
    public int maxMarkerCount = 99;

    [Header("Death & Respawn")]
    [Tooltip("Legacy: Time-based respawn delay (used as fallback if move-based system unavailable)")]
    public float respawnDelay = 2.0f;
    public float respawnInvulnerabilityTime = 2.0f;
    public bool debugDeathOverride = false;

    [Header("Physics & Collision")]
    public LayerMask cubeLayer = -1;
    public float collisionCheckRadius = 0.5f;

    [Header("Debug")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    public bool showTileInfo = false;
    #endregion

    #region Runtime State
    // Position & Movement
    public Vector2Int currentTilePosition = new Vector2Int(0, 0);
    private Vector3 currentVelocity = Vector3.zero;
    private bool isMoving = false;

    // Tile Interaction
    private Tile currentHoveredTile = null;
    private int lastLoggedTileX = -1;
    private int lastLoggedTileZ = -1;

    // Markers
    private int currentMarkers = 0;
    private Queue<Vector2Int> markerQueue = new Queue<Vector2Int>();

    // Death System
    public bool isDead = false;
    private float respawnInvulnerabilityTimer = 0f;
    private int respawnDelayMoves = 1; // Default: respawn after 1 move
    private int movesUntilRespawn = 0; // Tracks moves remaining until respawn
    private bool waitingForRespawn = false; // True when dead and waiting for move steps

    // Statistics
    public int normalCubesCaptured = 0;
    public int blueCubesCaptured = 0;
    public int blackCubesCaptured = 0;
    public int reinforcedCubesCaptured = 0;
    public int cubesEscaped = 0;
    public int markersPlaced = 0;
    public int markersTriggered = 0;
    public int detonationsUsed = 0;
    public int tilesCorrupted = 0;
    public int tilesMatrixd = 0;
    public int tilesEnhanced = 0;
    public int playerDeaths = 0;
    public int movesCount = 0;
    public float timeAlive = 0f;
    public float totalPlayTime = 0f;

    // Internal State
    private PlayerActionManager playerActionManager;
    private WaveManager waveManager;
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private bool isInitialized = false;
    private Vector2Int lastPosition;
    private float sessionStartTime;
    private Vector2Int playerStartPosition = Vector2Int.zero; // Store start position for respawn
    #endregion

    #region Events
    public System.Action<PlayerStatistics> OnStatisticsUpdated;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRespawned;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        
        InitializePlayer();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupPlayer();
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateTimers();
       
        if (isDead) return;

        HandleMovement();
        HandleTileTracking();
        TrackMovement();
        CheckForCollisions();
    }

    #endregion

    #region Initialization
    private void InitializePlayer()
    {
        FindReferences();
        InitializeState();
        SetInitialPosition();

        isInitialized = true;
        DebugLog("✅ Player Initialized");
    }

    private void FindReferences()
    {
        if (grid == null)
        {
            grid = GridManager.Instance ?? FindFirstObjectByType<GridManager>();
            if (grid == null)
            {
                this.LogError("PlayerManager requires GridManager!");
                enabled = false;
                return;
            }
        }

        playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        if (playerActionManager == null)
        {
            this.LogWarning("PlayerActionManager not found - player actions features limited", EnableDebugLogs);
        }

        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager == null)
        {
            this.LogWarning("WaveManager not found - cube collision features limited", EnableDebugLogs);
        }
    }

    private void InitializeState()
    {
        isDead = false;
        lastPosition = new Vector2Int(-1, -1);
        sessionStartTime = Time.time;
        respawnInvulnerabilityTimer = 0f;
        playerStartPosition = Vector2Int.zero; // Will be set by SetInitialPosition or ConfigurePlayer
        waitingForRespawn = false;
        movesUntilRespawn = 0;
        respawnDelayMoves = 1; // Default until configured from stage/wave
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnWaveStep += OnWaveStep;
        GameEvents.OnCubeCaptured += HandleCubeCaptured;
        GameEvents.OnCubeEscaped += HandleCubeEscaped;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStep -= OnWaveStep;
        GameEvents.OnCubeCaptured -= HandleCubeCaptured;
        GameEvents.OnCubeEscaped -= HandleCubeEscaped;
    }
    
    private void HandleCubeCaptured(Vector2Int position, Enumerations.CubeType cubeType)
    {
        // Update capture counters
        switch (cubeType)
        {
            case Enumerations.CubeType.Unit: normalCubesCaptured++; break;
            case Enumerations.CubeType.Matrix: blueCubesCaptured++; break;
            case Enumerations.CubeType.Recursion: reinforcedCubesCaptured++; break;
            case Enumerations.CubeType.Infinity: blackCubesCaptured++; break;
        }
        
        UpdateStatistics();
        DebugLog($"Cube captured: {cubeType}. Total: {normalCubesCaptured + blueCubesCaptured + reinforcedCubesCaptured + blackCubesCaptured}");
    }
    
    private void HandleCubeEscaped(Vector2Int position, Enumerations.CubeType cubeType)
    {
        cubesEscaped++;
        UpdateStatistics();
        DebugLog($"Cube escaped: {cubeType}. Total escapes: {cubesEscaped}");
    }

    /// <summary>
    /// Called by StageManager to set the player's start position from stage data.
    /// </summary>
    public void SetStartPosition(int x, int y)
    {
        playerStartPosition = new Vector2Int(x, y);
        DebugLog($"Start position set to ({x}, {y})");
    }
    
    /// <summary>
    /// Configures respawn delay from stage/wave data.
    /// Wave data takes precedence if set (non-zero), otherwise uses stage default.
    /// </summary>
    public void ConfigureRespawnDelay(int stageDefaultMoves, int waveMoves = 0)
    {
        if (waveMoves > 0)
        {
            respawnDelayMoves = waveMoves;
            DebugLog($"Respawn delay configured from wave: {waveMoves} move(s)");
        }
        else
        {
            respawnDelayMoves = stageDefaultMoves;
            DebugLog($"Respawn delay configured from stage: {stageDefaultMoves} move(s)");
        }
    }

    private void SetInitialPosition()
    {
        // Store start position for respawn
        playerStartPosition = new Vector2Int(grid.Width / 2, 0);
        SetPosition(playerStartPosition.x, playerStartPosition.y);
    }
    #endregion


    #region Movement System
    private void HandleMovement()
    {
        ProcessMovementInput();
        ApplyMovementWithCollisionSmoothing();
        UpdateAnimations();
        UpdateCurrentTilePosition();
    }

    private void ProcessMovementInput()
    {
        Vector3 inputDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) inputDirection.x = -1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) inputDirection.x = 1;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) inputDirection.z = 1;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) inputDirection.z = -1;

        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }

        Vector3 targetVelocity = inputDirection * acceleration;
        isMoving = inputDirection.magnitude > 0f;

        // Smooth velocity changes for better feel
        if (isMoving)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            // Faster deceleration for snappier stopping
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }
    }

    private void ApplyMovementWithCollisionSmoothing()
    {
        if (currentVelocity.magnitude < 0.01f) return;

        // Rotate to face movement direction smoothly
        if (currentVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
        }

        CharacterController controller = GetComponent<CharacterController>();
        if (controller == null) return;

        Vector3 movement = currentVelocity * Time.deltaTime;
        Vector3 desiredPosition = transform.position + movement;

        // Check for collisions and smooth handling
        Vector3 finalPosition = CalculateCollisionSafePosition(desiredPosition);
        Vector3 actualMovement = finalPosition - transform.position;

        // Apply the collision-safe movement
        if (actualMovement.magnitude > 0.001f)
        {
            controller.Move(actualMovement);
        }

        // Handle collision feedback for velocity
        HandleCollisionVelocityAdjustment(movement, actualMovement);
    }

    private Vector3 CalculateCollisionSafePosition(Vector3 desiredPosition)
    {
        desiredPosition.y = 0f; // Keep at ground level
        
        // SEGMENT CONTROLLERS: For segment controller grids, check all segments
        if (grid != null && grid.HasSegmentControllers)
        {
            // Check if position is valid on any segment
            if (IsWorldPositionValidOnAnySegment(desiredPosition))
            {
                // Check for cube collisions at future position
                if (!IsWorldPositionBlockedByCube(desiredPosition))
                {
                    return desiredPosition;
                }
                else
                {
                    // Find closest safe position
                    Vector2Int blockedPos = grid.WorldToGridPosition(desiredPosition);
                    return FindClosestSafePosition(desiredPosition, blockedPos);
                }
            }
            else
            {
                // Position not valid on any segment - try sliding or stay at current position
                return FindClosestSafePosition(desiredPosition, Vector2Int.zero);
            }
        }
        
        // ADVANCED GRID: For legacy multi-segment grids, don't clamp to segment 0 bounds
        if (grid != null && grid.HasMultipleSegments)
        {
            // Check if position is valid on any segment
            if (IsWorldPositionValidOnAnySegment(desiredPosition))
            {
                // Check for cube collisions at future position
                if (!IsWorldPositionBlockedByCube(desiredPosition))
                {
                    return desiredPosition;
                }
                else
                {
                    // Find closest safe position
                    Vector2Int blockedPos = grid.WorldToGridPosition(desiredPosition);
                    return FindClosestSafePosition(desiredPosition, blockedPos);
                }
            }
            else
            {
                // Position not valid on any segment - stay at current position
                return transform.position;
            }
        }
        
        // Standard single-segment logic
        Vector3 minBounds = grid.GridToWorldPosition(0, 0);
        Vector3 maxBounds = grid.GridToWorldPosition(grid.Width - 1, grid.Height - 1);

        // Clamp to grid bounds with smooth approach
        Vector3 clampedPosition = desiredPosition;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minBounds.z, maxBounds.z);

        // Check for cube collisions at future position
        Vector2Int futureGridPos = grid.WorldToGridPosition(clampedPosition);

        if (IsPositionBlockedByCube(futureGridPos))
        {
            // Find the closest safe position
            clampedPosition = FindClosestSafePosition(clampedPosition, futureGridPos);
        }

        return clampedPosition;
    }

    private bool IsPositionBlockedByCube(Vector2Int gridPos)
    {
        // First check if position is valid and playable
        if (!IsValidTilePosition(gridPos)) return true;

        Tile tile = grid.GetTileAt(gridPos.x, gridPos.y);
        if (tile == null || !tile.IsPlayable) return true;

        // Check for cubes at this position using cached WaveManager reference
        if (waveManager?.activeCubes == null) return false;
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            // Skip player cubes - player can pass through them
            if (cube.isPlayerCube) continue;
            
            // Task 7: Skip phaseable Infinity cubes - player can pass through them
            if (cube.type == Enumerations.CubeType.Infinity && cube.IsPhaseable())
            {
                continue;
            }
            
            if (cube.position.x == gridPos.x && cube.position.y == gridPos.y)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 FindClosestSafePosition(Vector3 desiredPosition, Vector2Int blockedGridPos)
    {
        Vector3 currentPos = transform.position;

        // Try to slide along the collision surface
        Vector3 slideDirection = (desiredPosition - currentPos).normalized;

        // Try moving along X-axis only
        Vector3 xOnlyMovement = new Vector3(slideDirection.x, 0, 0) * currentVelocity.magnitude * Time.deltaTime;
        Vector3 xOnlyPosition = currentPos + xOnlyMovement;

        if (IsWorldPositionValidOnAnySegment(xOnlyPosition) && !IsWorldPositionBlockedByCube(xOnlyPosition))
        {
            return ClampToGridBounds(xOnlyPosition);
        }

        // Try moving along Z-axis only
        Vector3 zOnlyMovement = new Vector3(0, 0, slideDirection.z) * currentVelocity.magnitude * Time.deltaTime;
        Vector3 zOnlyPosition = currentPos + zOnlyMovement;

        if (IsWorldPositionValidOnAnySegment(zOnlyPosition) && !IsWorldPositionBlockedByCube(zOnlyPosition))
        {
            return ClampToGridBounds(zOnlyPosition);
        }

        // If we can't slide, stay at current position
        return currentPos;
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if a world position is blocked by a cube on any segment.
    /// </summary>
    private bool IsWorldPositionBlockedByCube(Vector3 worldPos)
    {
        if (!IsWorldPositionValidOnAnySegment(worldPos)) 
            return true;
        
        // Check for cubes at this position using cached WaveManager reference
        if (waveManager?.activeCubes == null) return false;
        
        // Get the tile at this world position
        Tile tile = grid.GetTileAtWorldPositionAnySegment(worldPos);
        if (tile == null || !tile.IsPlayable) return true;
        
        // For cube collision, need to check cube world positions
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            if (cube.isPlayerCube) continue;
            
            // Skip phaseable Infinity cubes
            if (cube.type == Enumerations.CubeType.Infinity && cube.IsPhaseable())
                continue;
            
            // Check cube world position against our position
            float distance = Vector3.Distance(
                new Vector3(cube.transform.position.x, 0, cube.transform.position.z),
                new Vector3(worldPos.x, 0, worldPos.z)
            );
            
            if (distance < grid.TileSize * 0.5f)
            {
                return true;
            }
        }
        
        return false;
    }

    private Vector3 ClampToGridBounds(Vector3 position)
    {
        // SEGMENT CONTROLLERS: For segment controller grids, check if position is valid on ANY segment
        if (grid != null && grid.HasSegmentControllers)
        {
            // If the position is valid on any segment, don't clamp
            if (grid.IsValidWorldPosition(position))
            {
                position.y = 0f;
                return position;
            }
            // Otherwise, use overall grid bounds
            position.x = Mathf.Clamp(position.x, grid.MinWorldBounds.x, grid.MaxWorldBounds.x);
            position.z = Mathf.Clamp(position.z, grid.MinWorldBounds.z, grid.MaxWorldBounds.z);
            position.y = 0f;
            return position;
        }
        
        // ADVANCED GRID: For legacy multi-segment grids, check if position is valid on ANY segment
        if (grid != null && grid.HasMultipleSegments)
        {
            // If the position is valid on any segment, don't clamp
            if (grid.IsWorldPositionValid(position))
            {
                position.y = 0f;
                return position;
            }
            // Otherwise, clamp to segment 0 bounds as fallback
        }
        
        Vector3 minBounds = grid.GridToWorldPosition(0, 0);
        Vector3 maxBounds = grid.GridToWorldPosition(grid.Width - 1, grid.Height - 1);

        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.z = Mathf.Clamp(position.z, minBounds.z, maxBounds.z);
        position.y = 0f;

        return position;
    }

    private void HandleCollisionVelocityAdjustment(Vector3 intendedMovement, Vector3 actualMovement)
    {
        // If we didn't move as much as intended, reduce velocity in that direction
        if (intendedMovement.magnitude > 0.001f && actualMovement.magnitude < intendedMovement.magnitude * 0.9f)
        {
            // Calculate which direction was blocked
            Vector3 blockedDirection = (intendedMovement - actualMovement).normalized;

            // Reduce velocity in the blocked direction to prevent slingshot
            Vector3 velocityInBlockedDirection = Vector3.Project(currentVelocity, blockedDirection);
            currentVelocity -= velocityInBlockedDirection * 0.8f; // Dampen the blocked velocity

            DebugLog($"🚧 Movement blocked, dampening velocity. Blocked direction: {blockedDirection}");
        }
    }

    private void UpdateAnimations()
    {
        if (_anim != null)
        {
            // Use a slightly higher threshold for animation to prevent micro-movements
            bool shouldAnimate = currentVelocity.magnitude > 0.2f;
            _anim.SetBool(IsRunningHash, shouldAnimate);
        }
    }

    private void UpdateCurrentTilePosition()
    {
        // SEGMENT CONTROLLERS: Use world position-based tile tracking for smoother transitions
        if (grid.HasSegmentControllers)
        {
            UpdateCurrentTilePositionForSegments();
            return;
        }
        
        Vector2Int newTilePosition = grid.WorldToGridPosition(transform.position);
        bool tileChanged = (currentTilePosition.x != newTilePosition.x || currentTilePosition.y != newTilePosition.y);

        if (tileChanged)
        {
            HandleTileChange(newTilePosition);
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLERS: Updates tile position tracking using world position for multi-segment grids.
    /// This provides smoother transitions between segments.
    /// </summary>
    private void UpdateCurrentTilePositionForSegments()
    {
        // Get the tile at current world position from any segment
        Tile newTile = grid.GetTileAtWorldPositionFromControllers(transform.position);
        
        // Only update if we're on a different tile
        if (newTile != currentHoveredTile)
        {
            HandleTileChangeForSegments(newTile);
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLERS: Handles tile change for multi-segment grids using direct tile reference.
    /// </summary>
    private void HandleTileChangeForSegments(Tile newTile)
    {
        // Clear hover from old tile
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetPlayerHover(false);
        }
        
        // Update current tile reference
        currentHoveredTile = newTile;
        
        // Update grid position for compatibility (use world-to-local conversion)
        if (newTile != null)
        {
            // Find which segment this tile belongs to and get local position
            var segment = grid.GetSegmentControllerAtWorldPosition(transform.position);
            if (segment != null)
            {
                Vector2Int localPos = segment.WorldToLocalPosition(transform.position);
                currentTilePosition = localPos;
                DebugLog($"🚶 Player on Segment {segment.segmentIndex} at local ({localPos.x}, {localPos.y})");
            }
            
            // Set hover color based on current marker mode
            var actionManager = FindFirstObjectByType<PlayerActionManager>();
            if (actionManager != null)
            {
                Color hoverColor = PlayerActionManager.GetMarkerModeColor(actionManager.GetCurrentMode());
                newTile.SetHoverColor(hoverColor);
            }
            
            newTile.SetPlayerHover(true);
        }
        else
        {
            DebugLog($"🚶 Player at world pos {transform.position} - no tile found");
        }
    }

    private void HandleTileChange(Vector2Int newPosition)
    {
        DebugLog($"🚶 Player moved from ({currentTilePosition.x}, {currentTilePosition.y}) to ({newPosition.x}, {newPosition.y})");

        // Clear hover from old tile
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetPlayerHover(false);
        }

        currentTilePosition = newPosition;

        // Set hover on new tile with current marker mode color
        if (IsValidTilePosition(newPosition))
        {
            currentHoveredTile = grid.tiles[newPosition.x, newPosition.y];
            if (currentHoveredTile != null)
            {
                // Set hover color based on current marker mode
                var actionManager = FindFirstObjectByType<PlayerActionManager>();
                if (actionManager != null)
                {
                    Color hoverColor = PlayerActionManager.GetMarkerModeColor(actionManager.GetCurrentMode());
                    currentHoveredTile.SetHoverColor(hoverColor);
                }
                
                currentHoveredTile.SetPlayerHover(true);
            }
        }
    }
    #endregion

    #region Tile Tracking & Debug
    private void HandleTileTracking()
    {
        TrackAndLogTilePosition();
        HandleDebugDisplay();
    }

    private void TrackAndLogTilePosition()
    {
        int tileX = currentTilePosition.x;
        int tileZ = currentTilePosition.y;

        if (tileX != lastLoggedTileX || tileZ != lastLoggedTileZ)
        {
            lastLoggedTileX = tileX;
            lastLoggedTileZ = tileZ;
            DebugLog($"WorldPos={transform.position:F2} → Tile=({tileX},{tileZ})");
        }
    }

    private void HandleDebugDisplay()
    {
        // OnGUI info is handled separately
    }

    private void OnGUI()
    {
        if (!showTileInfo || currentHoveredTile == null) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"Current Tile: ({currentTilePosition.x}, {currentTilePosition.y})");
        GUI.Label(new Rect(10, 30, 300, 20), $"Has Marker: {currentHoveredTile.HasMarker}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Is Blackened: {currentHoveredTile.IsBlackened}");
    }
    #endregion


    #region Death & Respawn System
    public void Die()
    {
        if (isDead || respawnInvulnerabilityTimer > 0f) return;

        isDead = true;
        playerDeaths++;
        UpdateStatistics();

        DebugLog($"💀 Player died! Total deaths: {playerDeaths}");

        // Notify WaveManager for death penalty tracking
        if (waveManager != null)
        {
            waveManager.OnPlayerDeath();
        }

        // Notify ScoreManager
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.RecordPlayerDeath();
        }

        enabled = false;
        OnPlayerDied?.Invoke();

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        // Wait for move steps instead of time
        waitingForRespawn = true;
        movesUntilRespawn = respawnDelayMoves;
        
        DebugLog($"💀 Death sequence: Waiting for {movesUntilRespawn} move step(s) before respawn");
        
        // Fallback: If no move steps occur within reasonable time, use time-based respawn
        float fallbackTime = respawnDelay * 2f; // Give extra time for moves to occur
        float elapsed = 0f;
        
        while (waitingForRespawn && movesUntilRespawn > 0 && elapsed < fallbackTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // If still waiting (moves didn't happen), use time-based fallback
        if (waitingForRespawn)
        {
            DebugLog($"⚠️ Respawn: Move steps didn't occur, using time-based fallback");
            yield return new WaitForSeconds(respawnDelay);
        }
        
        RespawnPlayer();
    }
    
    /// <summary>
    /// Called when a move step occurs. Counts down moves until respawn.
    /// </summary>
    private void OnWaveStep(int waveIndex, int stepNumber)
    {
        if (!waitingForRespawn || !isDead) return;
        
        movesUntilRespawn--;
        DebugLog($"💀 Respawn countdown: {movesUntilRespawn} move(s) remaining");
        
        if (movesUntilRespawn <= 0)
        {
            waitingForRespawn = false;
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        isDead = false;
        waitingForRespawn = false;
        movesUntilRespawn = 0;
        respawnInvulnerabilityTimer = respawnInvulnerabilityTime;
        currentVelocity = Vector3.zero;

        // Find a safe respawn position (no cubes, preferably at start position or bottom row)
        Vector2Int respawnPos = FindSafeRespawnPosition();
        SetPosition(respawnPos.x, respawnPos.y);
        
        enabled = true;
        DebugLog($"🔄 Player respawned at ({respawnPos.x}, {respawnPos.y}) with {respawnInvulnerabilityTime}s invulnerability");
        OnPlayerRespawned?.Invoke();
    }

    /// <summary>
    /// Finds a safe position for respawn, avoiding cubes.
    /// Prefers start position, then tries bottom row, then finds any safe position.
    /// </summary>
    private Vector2Int FindSafeRespawnPosition()
    {
        if (grid == null || waveManager == null)
        {
            // Fallback to start position if systems unavailable
            return playerStartPosition;
        }

        // First, try the start position
        if (IsPositionSafe(playerStartPosition))
        {
            DebugLog($"Respawn: Using start position ({playerStartPosition.x}, {playerStartPosition.y})");
            return playerStartPosition;
        }

        // Try bottom row (above current bottom if rows were removed)
        int bottomRow = grid.bottom;
        for (int x = 0; x < grid.Width; x++)
        {
            Vector2Int pos = new Vector2Int(x, bottomRow);
            if (IsPositionSafe(pos))
            {
                DebugLog($"Respawn: Using bottom row position ({x}, {bottomRow})");
                return pos;
            }
        }

        // Try rows above bottom
        for (int y = bottomRow + 1; y < Mathf.Min(bottomRow + 5, grid.Height); y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsPositionSafe(pos))
                {
                    DebugLog($"Respawn: Using safe position ({x}, {y})");
                    return pos;
                }
            }
        }

        // Last resort: return start position even if not safe (better than crashing)
        DebugLog($"⚠️ Respawn: No safe position found, using start position ({playerStartPosition.x}, {playerStartPosition.y})");
        return playerStartPosition;
    }

    /// <summary>
    /// Checks if a position is safe for respawn (no cubes present).
    /// </summary>
    private bool IsPositionSafe(Vector2Int pos)
    {
        if (grid == null || !IsValidTilePosition(pos)) return false;

        // Check if any cube is at this position
        if (waveManager?.activeCubes != null)
        {
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube == null || cube.isDestroyed) continue;
                if (cube.position.x == pos.x && cube.position.y == pos.y)
                {
                    return false; // Cube present, not safe
                }
            }
        }

        return true; // No cubes found, position is safe
    }

    private void CheckForCollisions() 
    {
        if (isDead || respawnInvulnerabilityTimer > 0f) return;

        // Use cached WaveManager reference instead of FindObjectsOfType every frame
        if (waveManager?.activeCubes == null) return;
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;

            // Skip player cubes - player can pass through them unharmed
            if (cube.isPlayerCube) continue;

            if (cube.position.x == currentTilePosition.x && cube.position.y == currentTilePosition.y)
            {
                DebugLog($"Collision with {cube.type} cube at ({currentTilePosition.x}, {currentTilePosition.y})");
                
                bool willCauseDeath = !debugDeathOverride;
                
                // Notify statistics manager about collision
                if (PlayerStatisticsManager.Instance != null)
                {
                    PlayerStatisticsManager.Instance.OnCubeCollision(currentTilePosition, cube.type.ToString(), willCauseDeath);
                }
                
                if (willCauseDeath)
                {
                    Die();
                }
                else
                {
                    DebugLog("Death prevented by debug override");
                }
                break;
            }
        }
    }
    #endregion

    #region Statistics & Events

    private void UpdateStatistics()
    {
        OnStatisticsUpdated?.Invoke(GetCurrentStatistics());
    }

    public PlayerStatistics GetCurrentStatistics()
    {
        return new PlayerStatistics
        {
            normalCubesCaptured = this.normalCubesCaptured,
            blueCubesCaptured = this.blueCubesCaptured,
            blackCubesCaptured = this.blackCubesCaptured,
            reinforcedCubesCaptured = this.reinforcedCubesCaptured,
            cubesEscaped = this.cubesEscaped,
            markersPlaced = this.markersPlaced,
            markersTriggered = this.markersTriggered,
            detonationsUsed = this.detonationsUsed,
            tilesCorrupted = this.tilesCorrupted,
            tilesEnhanced = this.tilesEnhanced,
            playerDeaths = this.playerDeaths,
            movesCount = this.movesCount,
            timeAlive = this.timeAlive,
            totalPlayTime = this.totalPlayTime,
        };
    }

    public void ResetStatistics()
    {
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        blackCubesCaptured = 0;
        cubesEscaped = 0;
        markersPlaced = 0;
        markersTriggered = 0;
        detonationsUsed = 0;
        tilesCorrupted = 0;
        tilesEnhanced = 0;
        playerDeaths = 0;
        movesCount = 0;
        timeAlive = 0f;
        totalPlayTime = 0f;
        lastPosition = new Vector2Int(-1, -1);
        sessionStartTime = Time.time;

        UpdateStatistics();
        DebugLog("📊 Statistics reset");
    }
    #endregion

    #region Public Interface
    public bool IsAlive() => !isDead;

    public void Kill()
    {
        if (!isDead) Die();
    }

    public void SetPosition(int x, int z)
    {
        // Clamp to valid grid bounds (respecting removed rows)
        int minY = grid != null ? grid.bottom : 0;
        x = Mathf.Clamp(x, 0, grid.Width - 1);
        z = Mathf.Clamp(z, minY, grid.Height - 1);

        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetPlayerHover(false);
        }

        Vector3 worldPos = grid.GridToWorldPosition(x, z, 0f);

        // Handle CharacterController properly
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false; // Temporarily disable
            transform.position = worldPos;
            controller.enabled = true; // Re-enable
        }
        else
        {
            transform.position = worldPos;
        }

        // Reset velocity to prevent sliding
        currentVelocity = Vector3.zero;

        currentTilePosition = new Vector2Int(x, z);
        lastLoggedTileX = x;
        lastLoggedTileZ = z;

        if (IsValidTilePosition(currentTilePosition))
        {
            currentHoveredTile = grid.tiles[x, z];
            if (currentHoveredTile != null)
            {
                currentHoveredTile.SetPlayerHover(true);
            }
        }
    }

    public void SetMaxMarkers(int max)
    {
        maxMarkerCharge = Mathf.Max(1, max);
    }

    public void ResetMarkers()
    {
        currentMarkers = 0;
        markerQueue.Clear();
    }
    #endregion

    #region Utility Methods
    private void UpdateTimers()
    {
        if (!isDead) timeAlive += Time.deltaTime;
        totalPlayTime += Time.deltaTime;

        if (respawnInvulnerabilityTimer > 0f)
        {
            respawnInvulnerabilityTimer -= Time.deltaTime;
        }
    }

    private void TrackMovement()
    {
        if (lastPosition.x != currentTilePosition.x || lastPosition.y != currentTilePosition.y)
        {
            if (lastPosition.x != -1 && lastPosition.y != -1)
            {
                movesCount++;
                DebugLog($"🚶 Move #{movesCount} to ({currentTilePosition.x}, {currentTilePosition.y})");
            }
            lastPosition = currentTilePosition;
        }
    }

    private bool IsValidTilePosition(Vector2Int pos)
    {
        // Use GridManager's validation
        if (grid == null) return false;
        return grid.IsValidGridPosition(pos);
    }
    
    /// <summary>
    /// ADVANCED GRID: Checks if a world position is valid on any segment.
    /// </summary>
    private bool IsWorldPositionValidOnAnySegment(Vector3 worldPos)
    {
        if (grid == null) return false;
        
        // SEGMENT CONTROLLERS: Check all segment controllers
        if (grid.HasSegmentControllers)
        {
            return grid.IsValidWorldPosition(worldPos);
        }
        
        // For legacy multi-segment grids, check world position against all segments
        if (grid.HasMultipleSegments)
        {
            return grid.IsWorldPositionValid(worldPos);
        }
        
        // Standard single-segment check
        Vector2Int gridPos = grid.WorldToGridPosition(worldPos);
        return grid.IsValidGridPosition(gridPos);
    }

    private void CleanupPlayer()
    {
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetPlayerHover(false);
        }
    }

    private void DebugLog(string message)
    {
        this.Log(message, EnableDebugLogs);
    }
    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        string status = isDead ? "DEAD" : "ALIVE";
        string invuln = respawnInvulnerabilityTimer > 0f ? " (INVULN)" : "";
        return $"Player: {status}{invuln} @({currentTilePosition.x},{currentTilePosition.y}) Moves:{movesCount} Deaths:{playerDeaths}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Is Alive"] = !isDead,
            ["Current Position"] = $"({currentTilePosition.x}, {currentTilePosition.y})",
            ["World Position"] = transform.position,
            ["Is Moving"] = isMoving,
            ["Current Velocity"] = currentVelocity,
            ["Respawn Invulnerability"] = respawnInvulnerabilityTimer,
            ["Moves Count"] = movesCount,
            ["Player Deaths"] = playerDeaths,
            ["Time Alive"] = timeAlive,
            ["Total Play Time"] = totalPlayTime,
            ["Normal Cubes Captured"] = normalCubesCaptured,
            ["Blue Cubes Captured"] = blueCubesCaptured,
            ["Black Cubes Captured"] = blackCubesCaptured,
            ["Reinforced Cubes Captured"] = reinforcedCubesCaptured,
            ["Cubes Escaped"] = cubesEscaped,
            ["Markers Placed"] = markersPlaced,
            ["Markers Triggered"] = markersTriggered,
            ["Detonations Used"] = detonationsUsed,
            ["Tiles Corrupted"] = tilesCorrupted,
            ["Tiles Enhanced"] = tilesEnhanced,
            ["Max Marker Charge"] = maxMarkerCharge,
            ["Current Markers"] = currentMarkers,
            ["Debug Death Override"] = debugDeathOverride
        };
    }

    public void ResetToDefaults()
    {
        // Reset position to initial location
        SetPosition(grid.Width / 2, 0);
        
        // Reset state
        isDead = false;
        respawnInvulnerabilityTimer = 0f;
        currentVelocity = Vector3.zero;
        isMoving = false;
        
        // Reset markers
        ResetMarkers();
        
        // Reset statistics
        ResetStatistics();
        
        // Clear hover tile
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetPlayerHover(false);
            currentHoveredTile = null;
        }
        
        if (EnableDebugLogs)
            this.Log("Reset to defaults completed", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for player settings
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for player settings
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}