using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;

    [Header("Settings")]
    [SerializeField] private int maxMarkers = 2;
    [SerializeField] private Color selectorColor = Color.yellow;
    [SerializeField] private float selectorHeight = 0.2f;
    [Header("Speed Control")]
    [SerializeField] private KeyCode speedUpKey = KeyCode.LeftShift;

    private int currentMarkers = 0;
    private int selX = 0, selZ = 0;
    private DetonationManager detonationManager;
    private Renderer selectorRenderer;
    private Queue<Vector2Int> markerQueue = new Queue<Vector2Int>(); // Track marker order
    private bool isInitialized = false;
    private bool isSpeedingUp = false;

    private void Awake()
    {
        selectorRenderer = GetComponent<Renderer>();
        if (selectorRenderer == null)
        {
            Debug.LogError("Selector requires a Renderer component!");
            enabled = false;
            return;
        }
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
        
        // Set selector color
        if (selectorRenderer != null && selectorRenderer.material != null)
        {
            selectorRenderer.material.color = selectorColor;
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
        HandleMarkerPlacement();
        HandleMarkerTrigger();
        HandleDetonation();
        HandleSpeedControl();
    }

    private void HandleMovement()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selX = Mathf.Max(0, selX - 1);
            moved = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selX = Mathf.Min(grid.Width - 1, selX + 1);
            moved = true;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selZ = Mathf.Min(grid.Height - 1, selZ + 1);
            moved = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selZ = Mathf.Max(0, selZ - 1);
            moved = true;
        }
        
        int newX = selX;
        int newZ = selZ;

        if (moved)
        {
            // Check if the target tile is valid to move to
            if (IsValidMoveTarget(newX, newZ))
            {
                selX = newX;
                selZ = newZ;
                UpdateSelectorPosition();
            }
            else
            {
                // Blocked movement - could add visual/audio feedback here
                Debug.Log("Movement blocked: tile is corrupted");
            }
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
        transform.position = new Vector3(selX, selectorHeight, selZ);
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
            int effectiveMaxMarkers = maxMarkers;
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                int waveLimit = waveManager.GetCurrentMarkerLimit();
                if (waveLimit > 0)
                {
                    effectiveMaxMarkers = waveLimit;
                }
            }

            if (!currentTile.HasMarker)
            {
                if (currentMarkers < effectiveMaxMarkers)
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
        maxMarkers = Mathf.Max(1, max);
    }
}