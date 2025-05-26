using UnityEngine;
using static Enumerations;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] public GameObject tilePrefab;
    [SerializeField] public CubeTypeDefinitions cubeTypeDefinitions;
    [SerializeField] public int width = 5;
    [SerializeField] public int height = 20;
    [SerializeField] public float tileScale = 3f; // Added scale parameter
    
    [HideInInspector] public Tile[,] tiles;

    private void Awake()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned in GridManager!");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        tiles = new Tile[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Apply scale to the position calculation
                Vector3 position = new Vector3(x * tileScale, 1f, y * tileScale);
                GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";
                
                // Scale the tile GameObject
                tileObj.transform.localScale = new Vector3(tileScale, 1, tileScale);
                
                Tile tile = tileObj.GetComponent<Tile>();
                if (tile == null)
                {
                    tile = tileObj.AddComponent<Tile>();
                }
                
                tile.Init(x, y);
                tiles[x, y] = tile;
            }
        }
    }

    public bool PlaceMarker(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogWarning($"Attempted to place marker at invalid position: {x},{y}");
            return false;
        }

        if (tiles[x, y] == null || tiles[x, y].HasMarker)
            return false;
            
        tiles[x, y].PlaceMarker();
        return true;
    }

    public void ClearAllMarkers()
    {
        if (tiles == null) return;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null && tiles[x, y].HasMarker)
                    tiles[x, y].ClearMarker();
            }
        }
    }

    public void ResizeGrid(int newWidth, int newHeight)
    {
        Debug.Log($"Resizing grid from {width}x{height} to {newWidth}x{newHeight}");

        // Destroy existing grid
        DestroyGrid();

        // Update properties first
        width = newWidth;
        height = newHeight;


        // Regenerate with new dimensions
        GenerateGrid();

        Debug.Log($"Grid successfully resized to {width}x{height}");
    }

    public void DestroyGrid()
    {
        if (tiles != null)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (tiles[x, y] != null)
                    {
                        Destroy(tiles[x, y].gameObject);
                    }
                }
            }

            tiles = null;
        }

        // Destroy any child objects (in case there are other grid elements)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Public getters for width and height
    public int Width => width;
    public int Height => height;
    public float TileScale => tileScale; // Added getter for tile scale

    public static GridManager Instance { get
        {
            return FindObjectOfType<GridManager>();
        }
    }

    public CubeTypeDefinition GetCubeDefinition(CubeType type)
    {
        return cubeTypeDefinitions.GetDefinition(type);
    }

    // Helper method to convert grid position to world position
    public Vector3 GridToWorldPosition(int x, int y, float heightOffset = 0)
    {
        return new Vector3(x * tileScale, heightOffset, y * tileScale);
    }

    // Helper method to convert world position to grid position
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / tileScale);
        int y = Mathf.FloorToInt(worldPosition.z / tileScale);
        
        // Clamp to grid bounds
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        
        return new Vector2Int(x, y);
    }
}