using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] public GameObject tilePrefab;
    [SerializeField] public int width = 6;
    [SerializeField] public int height = 10;
    
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

    private void GenerateGrid()
    {
        tiles = new Tile[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";
                
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

    // Public getters for width and height
    public int Width => width;
    public int Height => height;
}