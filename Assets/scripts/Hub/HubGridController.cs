using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lightweight grid controller for the Hub scene.
/// Manages player navigation and hub cube interactions.
/// </summary>
public class HubGridController : MonoBehaviour
{
    #region Singleton
    
    private static HubGridController _instance;
    public static HubGridController Instance => _instance;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2Int playerStartPosition = new Vector2Int(2, 2);
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Color tileColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showGridGizmos = true;
    
    #endregion
    
    #region Runtime State
    
    private GameObject playerObject;
    private Vector2Int currentPlayerPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private Dictionary<Vector2Int, HubCube> hubCubes = new Dictionary<Vector2Int, HubCube>();
    private List<GameObject> tileObjects = new List<GameObject>();
    
    #endregion
    
    #region Properties
    
    public int Width => gridWidth;
    public int Height => gridHeight;
    public Vector2Int PlayerPosition => currentPlayerPosition;
    public bool IsMoving => isMoving;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    private void Start()
    {
        InitializeGrid();
        SpawnPlayer();
        RegisterHubCubes();
    }
    
    private void Update()
    {
        HandleInput();
        UpdatePlayerMovement();
    }
    
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeGrid()
    {
        // Create visual tiles if prefab provided
        if (tilePrefab != null)
        {
            CreateTileVisuals();
        }
        
        DebugLog($"Hub grid initialized: {gridWidth}x{gridHeight}");
    }
    
    private void CreateTileVisuals()
    {
        // Clear existing tiles
        foreach (var tile in tileObjects)
        {
            if (tile != null) Destroy(tile);
        }
        tileObjects.Clear();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, z));
                worldPos.y = -0.01f; // Slightly below cubes
                
                GameObject tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                tile.name = $"HubTile_{x}_{z}";
                
                // Apply material/color
                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null && tileMaterial != null)
                {
                    renderer.material = tileMaterial;
                    renderer.material.color = tileColor;
                }
                
                tileObjects.Add(tile);
            }
        }
    }
    
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            // Create simple cube as player placeholder
            playerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerObject.name = "HubPlayer";
            playerObject.transform.localScale = Vector3.one * 0.8f;
            
            var renderer = playerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.6f, 1f); // Blue tint
            }
            
            // Remove collider to avoid physics issues
            var collider = playerObject.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
        else
        {
            playerObject = Instantiate(playerPrefab);
            playerObject.name = "HubPlayer";
        }
        
        currentPlayerPosition = playerStartPosition;
        targetWorldPosition = GridToWorld(currentPlayerPosition);
        playerObject.transform.position = targetWorldPosition;
        
        DebugLog($"Player spawned at {currentPlayerPosition}");
    }
    
    private void RegisterHubCubes()
    {
        // Find all HubCubes in the scene and register them
        var cubes = FindObjectsByType<HubCube>(FindObjectsSortMode.None);
        foreach (var cube in cubes)
        {
            Vector2Int gridPos = WorldToGrid(cube.transform.position);
            hubCubes[gridPos] = cube;
            cube.SetGridPosition(gridPos);
            DebugLog($"Registered HubCube '{cube.CubeName}' at {gridPos}");
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInput()
    {
        if (isMoving) return;
        
        // Check if any UI panel is open
        if (HubUIManager.Instance != null && HubUIManager.Instance.IsPanelOpen) return;
        
        Vector2Int moveDir = Vector2Int.zero;
        
        // WASD / Arrow keys
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            moveDir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            moveDir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            moveDir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            moveDir = Vector2Int.right;
        
        if (moveDir != Vector2Int.zero)
        {
            TryMove(moveDir);
        }
        
        // Interact with current tile (F or Space)
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
        {
            TryInteract();
        }
    }
    
    private void TryMove(Vector2Int direction)
    {
        Vector2Int newPos = currentPlayerPosition + direction;
        
        // Bounds check
        if (!IsValidPosition(newPos))
        {
            DebugLog($"Cannot move to {newPos} - out of bounds");
            return;
        }
        
        // Check if occupied by blocking cube
        if (hubCubes.TryGetValue(newPos, out HubCube cube) && cube.BlocksMovement)
        {
            DebugLog($"Cannot move to {newPos} - blocked by {cube.CubeName}");
            return;
        }
        
        // Execute move
        currentPlayerPosition = newPos;
        targetWorldPosition = GridToWorld(newPos);
        isMoving = true;
        
        DebugLog($"Moving to {newPos}");
        
        // Check for auto-interact cubes
        if (cube != null && cube.InteractOnEnter)
        {
            cube.Interact();
        }
    }
    
    private void TryInteract()
    {
        if (hubCubes.TryGetValue(currentPlayerPosition, out HubCube cube))
        {
            DebugLog($"Interacting with {cube.CubeName}");
            cube.Interact();
        }
        else
        {
            // Check adjacent positions for interaction
            Vector2Int[] adjacent = new Vector2Int[]
            {
                currentPlayerPosition + Vector2Int.up,
                currentPlayerPosition + Vector2Int.down,
                currentPlayerPosition + Vector2Int.left,
                currentPlayerPosition + Vector2Int.right
            };
            
            foreach (var pos in adjacent)
            {
                if (hubCubes.TryGetValue(pos, out cube))
                {
                    DebugLog($"Interacting with adjacent {cube.CubeName}");
                    cube.Interact();
                    return;
                }
            }
        }
    }
    
    #endregion
    
    #region Movement
    
    private void UpdatePlayerMovement()
    {
        if (!isMoving || playerObject == null) return;
        
        playerObject.transform.position = Vector3.MoveTowards(
            playerObject.transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );
        
        if (Vector3.Distance(playerObject.transform.position, targetWorldPosition) < 0.01f)
        {
            playerObject.transform.position = targetWorldPosition;
            isMoving = false;
        }
    }
    
    #endregion
    
    #region Grid Utilities
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return gridOrigin + new Vector3(
            gridPos.x * tileSize,
            0f,
            gridPos.y * tileSize
        );
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 relative = worldPos - gridOrigin;
        return new Vector2Int(
            Mathf.RoundToInt(relative.x / tileSize),
            Mathf.RoundToInt(relative.z / tileSize)
        );
    }
    
    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }
    
    /// <summary>
    /// Register a hub cube at runtime.
    /// </summary>
    public void RegisterCube(HubCube cube, Vector2Int position)
    {
        hubCubes[position] = cube;
        cube.SetGridPosition(position);
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[HubGridController] {message}");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = gridOrigin + new Vector3(x * tileSize, 0, z * tileSize);
                Gizmos.DrawWireCube(pos, new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));
            }
        }
        
        // Draw player start
        Gizmos.color = Color.green;
        Vector3 startPos = gridOrigin + new Vector3(playerStartPosition.x * tileSize, 0.5f, playerStartPosition.y * tileSize);
        Gizmos.DrawWireSphere(startPos, 0.3f);
    }
    
    #endregion
}
