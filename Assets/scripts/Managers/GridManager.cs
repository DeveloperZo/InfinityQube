using UnityEngine;
using static Enumerations;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] public GameObject tilePrefab;
    [SerializeField] public CubeTypeDefinitions cubeTypeDefinitions;
    [SerializeField] public int width = 5;
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

    public void GenerateGrid()
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

    public static GridManager Instance { get
        {
            return FindObjectOfType<GridManager>();
        }
    }

    public CubeTypeDefinition GetCubeDefinition(CubeType type)
    {
        return cubeTypeDefinitions.GetDefinition(type);
    }
}