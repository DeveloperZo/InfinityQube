using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GridManager grid;
    public int maxMarkers = 3;
    private int currentMarkers = 0;
    private int selX = 0, selZ = 0;

    private Renderer selectorRenderer;

    void Start()
    {
        selectorRenderer = GetComponent<Renderer>();
        if (selectorRenderer != null)
        {
            selectorRenderer.material.color = Color.yellow;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) selX = Mathf.Max(0, selX - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) selX = Mathf.Min(grid.width - 1, selX + 1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) selZ = Mathf.Min(grid.height - 1, selZ + 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) selZ = Mathf.Max(0, selZ - 1);

        // Visually move the selector cube
        transform.position = new Vector3(selX, 0.2f, selZ);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Tile currentTile = grid.tiles[selX, selZ];

            if (!currentTile.HasMarker)
            {
                if (currentMarkers < maxMarkers)
                {
                    currentTile.PlaceMarker();
                    currentMarkers++;
                }
            }
            else
            {
                currentTile.ClearMarker();
                currentMarkers--;
            }
        }
    }

    public void ResetMarkers()
    {
        currentMarkers = 0;
    }
}
