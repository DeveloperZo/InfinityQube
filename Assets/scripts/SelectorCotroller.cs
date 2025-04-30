using UnityEngine;

public class SelectorController : MonoBehaviour
{
    public GridManager grid;
    private int selX = 0, selZ = 0;

    void Start()
    {
        var renderer = GetComponent<Renderer>();
        renderer.material.color = Color.yellow;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) selX = Mathf.Max(0, selX - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) selX = Mathf.Min(grid.width - 1, selX + 1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) selZ = Mathf.Min(grid.height - 1, selZ + 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) selZ = Mathf.Max(0, selZ - 1);

        transform.position = new Vector3(selX, 0.2f, selZ);
    }

    public int X => selX;
    public int Z => selZ;
}
