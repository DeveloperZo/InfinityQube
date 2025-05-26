using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    private int lastLoggedTileX = -1;
    private int lastLoggedTileZ = -1;

    [Header("Player Death")]
    [SerializeField] private bool isDead = false;
    [SerializeField] private float respawnDelay = 2.0f;

    [Header("Player Statistics")]
    [SerializeField] private int normalCubesCaptured = 0;
    [SerializeField] private int blueCubesCaptured = 0;
    [SerializeField] private int blackCubesCaptured = 0;
    [SerializeField] private int cubesEscaped = 0;
    [SerializeField] private int markersPlaced = 0;
    [SerializeField] private int markersTriggered = 0;
    [SerializeField] private int detonationsUsed = 0;
    [SerializeField] private int tilesCorrupted = 0;
    [SerializeField] private int tilesEnhanced = 0;
    [SerializeField] private int playerDeaths = 0;
    [SerializeField] private int movesCount = 0;
    [SerializeField] private float timeAlive = 0f;
    [SerializeField] private float totalPlayTime = 0f;


    [Header("Settings")]
    [SerializeField] private int maxMarkerCharge = 2;
    [SerializeField] private int maxMarkerCount = 99;
    [SerializeField] private float tileScale = 3f; // Added tile scale parameter

    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    private Vector3 currentVelocity = Vector3.zero;


    [Header("Speed Control")]
    [SerializeField]
    private float worldSpeed = 3f;
    [SerializeField] private KeyCode speedUpKey = KeyCode.LeftShift;
    [SerializeField] private Animator _anim;
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private bool isMoving = false;
    private Coroutine moveRoutine;

    private int currentMarkers = 0;
    private Vector2Int currentTilePosition = new Vector2Int(0, 0);
    private DetonationManager detonationManager;

    private Queue<Vector2Int> markerQueue = new Queue<Vector2Int>(); // Track marker order
    private bool isInitialized = false;
    private bool isSpeedingUp = false;
    private Vector2Int lastPosition;
    private float sessionStartTime;

    public System.Action<PlayerStatistics> OnStatisticsUpdated;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRespawned;

    private void Start()
    {
        if (grid == null)
        {
            grid = FindObjectOfType<GridManager>();
            if (grid == null)
            {
                Debug.LogError("PlayerController requires a GridManager reference!");
                enabled = false;
                return;
            }
        }

        detonationManager = FindObjectOfType<DetonationManager>();
        if (detonationManager == null)
        {
            Debug.LogWarning("DetonationManager not found in scene. Detonation functionality will be limited.");
        }

        // Initialize state
        isDead = false;

        // Initialize statistics
        lastPosition = new Vector2Int(-1, -1);
        sessionStartTime = Time.time;

        // Initialize the current position based on the player's world position
        UpdateCurrentTilePosition();
        isInitialized = true;
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            UpdateCurrentTilePosition();
        }
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (!isDead)
        {
            timeAlive += Time.deltaTime;
        }
        totalPlayTime += Time.deltaTime;
        
        if (isDead) return;

        HandleMovement();
        TrackAndLogTilePosition();
        HandleMarkerPlacement();
        HandleMarkerTrigger();
        HandleDetonation();
        HandleSpeedControl();
        
        //TrackMovement();

        CheckForCollisions();
    }

    private void HandleMovement()
    {
        // Get input direction
        Vector3 inputDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) inputDirection.x = -1;
        if (Input.GetKey(KeyCode.RightArrow)) inputDirection.x = 1;
        if (Input.GetKey(KeyCode.UpArrow)) inputDirection.z = 1;
        if (Input.GetKey(KeyCode.DownArrow)) inputDirection.z = -1;

        // Normalize diagonal movement
        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }

        Vector3 targetVelocity = inputDirection * worldSpeed;
        bool isMoving = inputDirection.magnitude > 0f;

        // Smooth acceleration/deceleration
        if (isMoving)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        // Update animation based on actual velocity
        if (_anim != null)
        {
            bool shouldAnimate = currentVelocity.magnitude > 0.1f;
            _anim.SetBool(IsRunningHash, shouldAnimate);
        }

        // Rotate to face movement direction
        if (currentVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // Apply movement
        if (currentVelocity.magnitude > 0.01f)
        {
            Vector3 newPosition = transform.position + currentVelocity * Time.deltaTime;

            // Apply grid boundaries with tile scale
            float minX = 0f;
            float maxX = (grid.Width - 1) * tileScale;
            float minZ = 0f;
            float maxZ = (grid.Height - 1) * tileScale;

            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

            transform.position = newPosition;
        }

        // Update current tile position after movement
        UpdateCurrentTilePosition();
    }

    private void UpdateCurrentTilePosition()
    {
        // Calculate the tile position based on world position with scale
        int tileX = Mathf.RoundToInt(transform.position.x / tileScale);
        int tileZ = Mathf.RoundToInt(transform.position.z / tileScale);

        // Clamp to grid bounds
        tileX = Mathf.Clamp(tileX, 0, grid.Width - 1);
        tileZ = Mathf.Clamp(tileZ, 0, grid.Height - 1);

        // Update the current tile position
        currentTilePosition = new Vector2Int(tileX, tileZ);
    }

    private void TrackAndLogTilePosition()
    {
        // Get the current tile position
        int tileX = currentTilePosition.x;
        int tileZ = currentTilePosition.y;

        // Only log when it changes
        if (tileX != lastLoggedTileX || tileZ != lastLoggedTileZ)
        {
            lastLoggedTileX = tileX;
            lastLoggedTileZ = tileZ;

            Debug.Log($"WorldPos={transform.position:F2}  →  Tile=({tileX},{tileZ})");
        }
    }

    private bool IsValidMoveTarget(int x, int z)
    {
        // Check grid bounds
        if (x < 0 || x >= grid.Width || z < 0 || z >= grid.Height)
            return false;

        // Check if tile is corrupted
        Tile tile = grid.tiles[x, z];
        return tile != null && !tile.IsBlackened;
    }

    private void HandleMarkerPlacement()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int selX = currentTilePosition.x;
            int selZ = currentTilePosition.y;

            if (selX < 0 || selX >= grid.Width || selZ < 0 || selZ >= grid.Height)
                return;

            Tile currentTile = grid.tiles[selX, selZ];
            if (currentTile == null) return;

            // Skip blackened tiles
            if (currentTile.IsBlackened)
            {
                // Optional: add feedback that this tile can't be marked
                return;
            }

            // Get wave-specific marker limit if available
            int markerCount = maxMarkerCount;
            int markerChargeCount = this.maxMarkerCharge;
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                int waveChargeLimit = waveManager.MarkerChargeLimit();
                if (waveChargeLimit > 0)
                {
                    markerChargeCount = waveChargeLimit;
                }
                int waveCountLimit = waveManager.MarkerCountLimit();
                if (waveCountLimit > 0)
                {
                    markerCount = waveCountLimit;
                }
            }

            if (!currentTile.HasMarker)
            {
                if (currentMarkers < markerChargeCount)
                {
                    currentTile.PlaceMarker();
                    markerQueue.Enqueue(new Vector2Int(selX, selZ));
                    currentMarkers++;
                    OnMarkerPlaced();

                    // Notify wave manager that a marker was placed
                    if (waveManager != null)
                    {
                        waveManager.OnMarkerPlaced();
                    }
                }
            }
            else
            {
                currentTile.ClearMarker();
                markerQueue = new Queue<Vector2Int>(
                    markerQueue.Where(pos => pos.x != selX || pos.y != selZ));
                currentMarkers--;
            }
        }
    }

    private void HandleMarkerTrigger()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (markerQueue.Count > 0)
            {
                Vector2Int markerPos = markerQueue.Dequeue();

                // Ensure position is within bounds
                if (markerPos.x < 0 || markerPos.x >= grid.Width ||
                    markerPos.y < 0 || markerPos.y >= grid.Height)
                {
                    Debug.LogWarning("Detonation position is out of bounds and will be ignored.");
                    return;
                }

                Tile tile = grid.tiles[markerPos.x, markerPos.y];
                if (tile != null && tile.HasMarker)
                {
                    tile.TriggerMarker();
                    currentMarkers--;
                    OnMarkerTriggered();
                    Debug.Log($"Detonated marker at {markerPos}");
                }
            }
        }
    }

    private void HandleDetonation()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("D key pressed, checking for detonation points...");
            if (detonationManager != null && detonationManager.HasDetonationPoints())
            {
                Debug.Log($"Triggering next detonation (Points available: {detonationManager.DetonationPointCount})");
                detonationManager.TriggerNextDetonation();
                OnDetonationUsed();
            }
            else
            {
                Debug.Log("No detonation points available or DetonationManager is null");
                if (detonationManager == null)
                {
                    detonationManager = FindObjectOfType<DetonationManager>();
                    Debug.Log($"Attempted to find DetonationManager: {(detonationManager != null ? "Found" : "Not found")}");
                }
            }
        }
    }

    private void HandleSpeedControl()
    {
        bool wasSpeedingUp = isSpeedingUp;
        isSpeedingUp = Input.GetKey(speedUpKey);

        // Only notify on state changes to avoid constant calls
        if (isSpeedingUp != wasSpeedingUp)
        {
            // Find and notify WaveManager
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.SetSpeedState(isSpeedingUp);
            }
        }
    }

    public void ResetMarkers()
    {
        currentMarkers = 0;
        markerQueue.Clear();
    }

    private void CheckForCollisions()
    {
        if (isDead) return;

        // Check all active cubes for collision with player
        CubeBehavior[] allCubes = FindObjectsOfType<CubeBehavior>();
        foreach (CubeBehavior cube in allCubes)
        {
            if (cube == null || cube.isDestroyed) continue;

            // Check if cube and player are on the same tile
            if (cube.position.x == currentTilePosition.x && cube.position.y == currentTilePosition.y)
            {
                Debug.Log($"Player collision with {cube.type} cube at ({currentTilePosition.x}, {currentTilePosition.y})");
                Die();
                break; // Only need to process one collision
            }
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        playerDeaths++;
        UpdateStatistics();
        Debug.Log($"Player died! Total deaths: {playerDeaths}");

        // Stop all input and movement
        enabled = false;

        // Notify other systems
        OnPlayerDied?.Invoke();

        // Optional: Add death effects here (animation, sound, screen shake, etc.)

        // Handle respawn
        StartCoroutine(HandleDeath());
    }

    private System.Collections.IEnumerator HandleDeath()
    {
        // Wait before respawning
        yield return new WaitForSeconds(respawnDelay);

        // Respawn player
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        // Reset state
        isDead = false;

        // Reset position to start (you can customize this spawn point)
        if (grid != null)
        {
            SetPosition(0, 0); // Or get spawn point from StageManager
        }

        // Re-enable movement and input
        enabled = true;
        currentVelocity = Vector3.zero;
        Debug.Log("Player respawned!");
        OnPlayerRespawned?.Invoke();
    }

    // Public methods for other systems
    public bool IsAlive() => !isDead;

    public void Kill()
    {
        if (!isDead)
            Die();
    }

    public void SetPosition(int x, int z)
    {
        x = Mathf.Clamp(x, 0, grid.Width - 1);
        z = Mathf.Clamp(z, 0, grid.Height - 1);

        // Update physical position with tile scale - center the player on the tile
        transform.position = new Vector3(x * tileScale, transform.position.y, z * tileScale);

        // Update current tile position
        currentTilePosition = new Vector2Int(x, z);

        // Update logged positions
        lastLoggedTileX = x;
        lastLoggedTileZ = z;
    }

    public void SetMaxMarkers(int max)
    {
        maxMarkerCharge = Mathf.Max(1, max);
    }

    private void TrackMovement()
    {
        // Only track movement when player actually changes tiles, not continuous movement
        if (lastPosition.x != currentTilePosition.x || lastPosition.y != currentTilePosition.y)
        {
            if (lastPosition.x != -1 && lastPosition.y != -1) // Don't count the initial position set
            {
                movesCount++;
                Debug.Log($"Player moved to tile ({currentTilePosition.x}, {currentTilePosition.y}). Total moves: {movesCount}");
            }
            lastPosition = currentTilePosition;
        }
    }

    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        switch (cubeType)
        {
            case Enumerations.CubeType.Normal:
                normalCubesCaptured++;
                break;
            case Enumerations.CubeType.Blue:
                blueCubesCaptured++;
                break;
            case Enumerations.CubeType.Black:
                blackCubesCaptured++;
                break;
        }

        UpdateStatistics();
        Debug.Log($"Player captured {cubeType} cube. Total: Normal={normalCubesCaptured}, Blue={blueCubesCaptured}, Black={blackCubesCaptured}");
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
        cubesEscaped++;
        UpdateStatistics();
        Debug.Log($"Cube escaped. Total escapes: {cubesEscaped}");
    }

    public void OnMarkerPlaced()
    {
        markersPlaced++;
        UpdateStatistics();
    }

    public void OnMarkerTriggered()
    {
        markersTriggered++;
        UpdateStatistics();
    }

    public void OnDetonationUsed()
    {
        detonationsUsed++;
        UpdateStatistics();
    }

    public void OnTileCorrupted()
    {
        tilesCorrupted++;
        UpdateStatistics();
        Debug.Log($"Tile corrupted. Total corrupted tiles: {tilesCorrupted}");
    }

    public void OnTileEnhanced()
    {
        tilesEnhanced++;
        UpdateStatistics();
        Debug.Log($"Tile enhanced. Total enhanced tiles: {tilesEnhanced}");
    }

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
        Debug.Log("Player statistics reset");
    }
}