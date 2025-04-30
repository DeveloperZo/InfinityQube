using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
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

        if (moved)
        {
            UpdateSelectorPosition();
        }
    }

    private void UpdateSelectorPosition()
    {
        transform.position = new Vector3(selX, selectorHeight, selZ);
    }

    private void HandleMarkerPlacement()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (selX < 0 || selX >= grid.Width || selZ < 0 || selZ >= grid.Height)
                return;
                
            Tile currentTile = grid.tiles[selX, selZ];
            if (currentTile == null) return;

            if (!currentTile.HasMarker)
            {
                if (currentMarkers < maxMarkers)
                {
                    currentTile.PlaceMarker();
                    markerQueue.Enqueue(new Vector2Int(selX, selZ)); // Track marker order
                    currentMarkers++;
                }
            }
            else
            {
                currentTile.ClearMarker();
                // Use a new queue excluding this position
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
        if (detonationManager.HasDetonationPoints())
        {
            detonationManager.TriggerNextDetonation();
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
}