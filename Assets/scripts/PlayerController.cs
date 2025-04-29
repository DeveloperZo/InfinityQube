using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GridManager grid;
    public int maxMarkers = 2;
    private int currentMarkers = 0;
    private int selX = 0, selY = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) selX = Mathf.Max(0, selX - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) selX = Mathf.Min(grid.width - 1, selX + 1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) selY = Mathf.Min(grid.height - 1, selY + 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) selY = Mathf.Max(0, selY - 1);

        if (Input.GetKeyDown(KeyCode.Space) && currentMarkers < maxMarkers)
        {
            if (grid.PlaceMarker(selX, selY))
            {
                currentMarkers++;
            }
        }
    }
}
