using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 6;
    public int height = 6;
    public Tile[,] tiles;

    void Start()
    {
        tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity);
                tileObj.name = $"Tile_{x}_{y}";
                Tile tile = tileObj.AddComponent<Tile>();
                tile.Init(x, y);
                tiles[x, y] = tile;
            }
        }
    }

    public bool PlaceMarker(int x, int y)
    {
        if (tiles[x, y].HasMarker) return false;
        tiles[x, y].PlaceMarker();
        return true;
    }
}