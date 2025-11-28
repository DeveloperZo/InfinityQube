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
    public float respawnDelay = 2.0f;
    public float respawnInvulnerabilityTime = 2.0f;
    public bool debugDeathOverride = false;

    [Header("Physics & Collision")]
    public LayerMask cubeLayer = -1;
    public float collisionCheckRadius = 0.5f;

    [Header("Debug")]
    public bool showTileInfo = false;
    #endregion

    #region Runtime State
    // Position & Movement
    public Vector2Int currentTilePosition = new Vector2Int(0, 0);
    private Vector3 currentVelocity = Vector3.zero;
    private bool isMoving = false;
    private bool isSpeedingUp = false;

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
    public int tilesPrimed = 0;
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
    #endregion

    #region Events
    public System.Action<PlayerStatistics> OnStatisticsUpdated;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRespawned;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        EnableDebugLogs = true;
        InitializePlayer();
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

    private void OnDestroy()
    {
        CleanupPlayer();
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
            grid = GridManager.Instance ?? FindObjectOfType<GridManager>();
            if (grid == null)
            {
                this.LogError("PlayerManager requires GridManager!");
                enabled = false;
                return;
            }
        }

        playerActionManager = FindObjectOfType<PlayerActionManager>();
        if (playerActionManager == null)
        {
            this.LogWarning("PlayerActionManager not found - player actions features limited", EnableDebugLogs);
        }

        waveManager = FindObjectOfType<WaveManager>();
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
    }

    private void SetInitialPosition()
    {
        SetPosition(grid.Width / 2, 0); // Center bottom
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
        // Get grid bounds
        Vector3 minBounds = grid.GridToWorldPosition(0, 0);
        Vector3 maxBounds = grid.GridToWorldPosition(grid.Width - 1, grid.Height - 1);

        // Clamp to grid bounds with smooth approach
        Vector3 clampedPosition = desiredPosition;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minBounds.z, maxBounds.z);
        clampedPosition.y = 0f; // Keep at ground level

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
        Vector2Int xOnlyGridPos = grid.WorldToGridPosition(xOnlyPosition);

        if (!IsPositionBlockedByCube(xOnlyGridPos) && IsValidTilePosition(xOnlyGridPos))
        {
            return ClampToGridBounds(xOnlyPosition);
        }

        // Try moving along Z-axis only
        Vector3 zOnlyMovement = new Vector3(0, 0, slideDirection.z) * currentVelocity.magnitude * Time.deltaTime;
        Vector3 zOnlyPosition = currentPos + zOnlyMovement;
        Vector2Int zOnlyGridPos = grid.WorldToGridPosition(zOnlyPosition);

        if (!IsPositionBlockedByCube(zOnlyGridPos) && IsValidTilePosition(zOnlyGridPos))
        {
            return ClampToGridBounds(zOnlyPosition);
        }

        // If we can't slide, stay at current position
        return currentPos;
    }

    private Vector3 ClampToGridBounds(Vector3 position)
    {
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
        Vector2Int newTilePosition = grid.WorldToGridPosition(transform.position);
        bool tileChanged = (currentTilePosition.x != newTilePosition.x || currentTilePosition.y != newTilePosition.y);

        if (tileChanged)
        {
            HandleTileChange(newTilePosition);
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

        // Set hover on new tile
        if (IsValidTilePosition(newPosition))
        {
            currentHoveredTile = grid.tiles[newPosition.x, newPosition.y];
            if (currentHoveredTile != null)
            {
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

        enabled = false;
        OnPlayerDied?.Invoke();

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        isDead = false;
        respawnInvulnerabilityTimer = respawnInvulnerabilityTime;
        currentVelocity = Vector3.zero;

        SetPosition(0, 0); // Respawn at center bottom
        enabled = true;
        DebugLog($"Player respawned with {respawnInvulnerabilityTime}s invulnerability");
        OnPlayerRespawned?.Invoke();
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
        x = Mathf.Clamp(x, 0, grid.Width - 1);
        z = Mathf.Clamp(z, 0, grid.Height - 1);

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
        if (pos.x < 0 || pos.x >= grid.Width || pos.y < 0 || pos.y >= grid.Height) return false;

        Tile tile = grid.GetTileAt(pos.x, pos.y);
        return tile != null && tile.IsPlayable;
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

    public bool EnableDebugLogs { get; set; } = true;

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