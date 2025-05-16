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

    [Header("Settings")]
    [SerializeField] private int maxMarkerCharge = 2;
    [SerializeField] private int maxMarkerCount = 99;

    [Header("Speed Control")]
    [SerializeField]
    private float worldSpeed = 3f;
    [SerializeField] private KeyCode speedUpKey = KeyCode.LeftShift;
    [SerializeField] private Animator _anim;
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private bool isMoving = false;
    private Coroutine moveRoutine;

    private int currentMarkers = 0;
    private int selX = 0, selZ = 0;
    private DetonationManager detonationManager;

    private Queue<Vector2Int> markerQueue = new Queue<Vector2Int>(); // Track marker order
    private bool isInitialized = false;
    private bool isSpeedingUp = false;

    private void Awake()
    {

    }

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
        
        isInitialized = true;
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            // Reset the selector position when enabled
            UpdateSelectorPosition();
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        HandleMovement();
        TrackAndLogTilePosition();
        HandleMarkerPlacement();
        HandleMarkerTrigger();
        HandleDetonation();
        HandleSpeedControl();
    }

    private void HandleMovement()
    {
        // 1) Read raw input axes
        float h = 0, v = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) h = -1;
        if (Input.GetKey(KeyCode.RightArrow)) h = +1;
        if (Input.GetKey(KeyCode.UpArrow)) v = +1;
        if (Input.GetKey(KeyCode.DownArrow)) v = -1;

        Vector3 dir = new Vector3(h, 0, v);

        // 2) If any input, face and run
        bool isRunning = dir.sqrMagnitude > 0.01f;
        _anim.SetBool(IsRunningHash, isRunning);

        if (isRunning)
        {
            // rotate smoothly (or instantly) to face move direction
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // 3) translate in world space
            transform.Translate(dir.normalized * worldSpeed * Time.deltaTime, Space.World);
        }
    }

    private void TrackAndLogTilePosition()
    {
        // Assuming your grid origin is at (0, 0) and each tile is 1 unit
        int tileX = Mathf.FloorToInt(transform.position.x + 0.5f);
        int tileZ = Mathf.FloorToInt(transform.position.z + 0.5f);

        // clamp within your grid bounds
        tileX = Mathf.Clamp(tileX, 0, grid.Width - 1);
        tileZ = Mathf.Clamp(tileZ, 0, grid.Height - 1);

        // only log when it changes
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

    private void UpdateSelectorPosition()
    {
        transform.position = new Vector3(selX, 1, selZ);
    }

    // In PlayerController.cs, update HandleMarkerPlacement
    private void HandleMarkerPlacement()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
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

    // In PlayerController.cs - Update() method
    private void CheckForGameOver()
    {
        // Check all active cubes for collision with player
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == selX && cube.position.y == selZ)
            {
                
                return;
            }
        }
    }

    public void SetPosition(int x, int z)
    {
        selX = Mathf.Clamp(x, 0, grid.Width - 1);
        selZ = Mathf.Clamp(z, 0, grid.Height - 1);
        UpdateSelectorPosition();
    }

    public void SetMaxMarkers(int max)
    {
        maxMarkerCharge = Mathf.Max(1, max);
    }
}