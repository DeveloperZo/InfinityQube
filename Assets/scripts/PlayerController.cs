using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public GridManager grid;
    public int maxMarkers = 2;
    private int currentMarkers = 0;
    private int selX = 0, selZ = 0;
    private DetonationManager detonationManager;

    private Renderer selectorRenderer;
    private Queue<Vector2Int> markerQueue = new Queue<Vector2Int>(); // Queue to track marker order

    void Start()
    {
        selectorRenderer = GetComponent<Renderer>();
        if (selectorRenderer != null)
        {
            selectorRenderer.material.color = Color.yellow;
        }
        
        detonationManager = FindObjectOfType<DetonationManager>();
    }

    void Update()
    {
        // Movement
        if (Input.GetKeyDown(KeyCode.LeftArrow)) selX = Mathf.Max(0, selX - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) selX = Mathf.Min(grid.width - 1, selX + 1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) selZ = Mathf.Min(grid.height - 1, selZ + 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) selZ = Mathf.Max(0, selZ - 1);

        // Visually move the selector cube
        transform.position = new Vector3(selX, 0.2f, selZ);

        // Place marker (blue)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Tile currentTile = grid.tiles[selX, selZ];

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
                markerQueue = new Queue<Vector2Int>(markerQueue.Where(pos => pos != new Vector2Int(selX, selZ))); // Remove from queue
                currentMarkers--;
            }
        }

        // Trigger detonation in order (D key)
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (markerQueue.Count > 0)
            {
                Vector2Int markerPos = markerQueue.Dequeue();

                // Ensure detonation position is within bounds
                if (markerPos.x < 0 || markerPos.x >= grid.width || markerPos.y < 0 || markerPos.y >= grid.height)
                {
                    Debug.LogWarning("Detonation position is out of bounds and will be ignored.");
                    return;
                }

                Tile tile = grid.tiles[markerPos.x, markerPos.y];

                if (tile.HasMarker)
                {
                    tile.TriggerMarker();
                    currentMarkers--;
                    Debug.Log($"Detonated marker at {markerPos}");
                }
            }
        }
    }

    public void ResetMarkers()
    {
        currentMarkers = 0;
    }
}