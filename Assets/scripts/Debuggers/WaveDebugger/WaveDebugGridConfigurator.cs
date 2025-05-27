using UnityEngine;

public class WaveDebugGridConfigurator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Settings")]
    [SerializeField] private int defaultWidth = 3;
    [SerializeField] private int defaultHeight = 3;

    public int[,] gridState;
    public int gridWidth;
    public int gridHeight;
    private int waveWidth;
    private int waveHeight;

    public int WaveWidth => waveWidth;
    public int WaveHeight => waveHeight;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        gridWidth = defaultWidth;
        gridHeight = Mathf.Max(9, defaultHeight * 3);
        waveWidth = defaultWidth;
        waveHeight = defaultHeight;
        InitializeGrid();
    }

    public void SetWaveDimensions(int w, int h)
    {
        waveWidth = Mathf.Clamp(w, 1, 12);
        waveHeight = Mathf.Clamp(h, 1, 15);
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        gridState = new int[waveWidth, 15];
        for (int x = 0; x < waveWidth; x++)
            for (int y = 0; y < 15; y++)
                gridState[x, y] = 1; // Default: normal
    }

    public void ApplyGridSize()
    {
        // Make sure grid height is at least 15 tiles
        gridHeight = Mathf.Max(gridHeight, 15);
        gridWidth = gridWidth < 3 ? Mathf.Max(3, waveWidth) : gridWidth;

        // Apply changes to the actual grid in the scene
        if (gridManager != null)
        {
            bool needsResize = gridManager.Width != gridWidth || gridManager.height != gridHeight;

            if (needsResize)
            {
                Debug.Log($"ApplyGridSize: Resizing grid to {gridWidth}x{gridHeight}");

                // Use the proper resize method
                gridManager.ResizeGrid(gridWidth, gridHeight);
            }
        }

        // Recreate the local grid arrays for the editor
        int[,] newGridState = new int[waveWidth, waveHeight];

        // Copy existing values where possible
        if (gridState != null)
        {
            int oldWidth = Mathf.Min(gridState.GetLength(0), waveWidth);
            int oldHeight = Mathf.Min(gridState.GetLength(1), waveHeight);

            for (int x = 0; x < oldWidth; x++)
            {
                for (int y = 0; y < oldHeight; y++)
                {
                    newGridState[x, y] = gridState[x, y];
                }
            }
        }

        // Initialize any new cells
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                // Only set values for cells that weren't copied
                if (gridState == null || x >= gridState.GetLength(0) || y >= gridState.GetLength(1))
                {
                    newGridState[x, y] = 1; // Default to normal cube
                }
            }
        }

        // Update the arrays
        gridState = newGridState;

        Debug.Log($"Applied new local wave dimensions for editor: {waveWidth}x{waveHeight}");
    }

    public void ClearGrid()
    {
        for (int x = 0; x < waveWidth; x++)
            for (int y = 0; y < waveHeight; y++)
                gridState[x, y] = 1;
    }

    public void RandomizeGrid()
    {
        int total = waveWidth * waveHeight;
        int maxBlue = Mathf.FloorToInt(total * 0.2f);
        int maxBlack = Mathf.FloorToInt(total * 0.2f);
        int blueCount = Random.Range(1, maxBlue + 1);
        int blackCount = Random.Range(1, maxBlack + 1);
        ClearGrid();
        PlaceRandomCubes(2, blueCount);
        PlaceRandomCubes(3, blackCount);
    }

    private void PlaceRandomCubes(int type, int count)
    {
        int placed = 0;
        int attempts = 0;
        while (placed < count && attempts < 100)
        {
            int x = Random.Range(0, waveWidth);
            int y = Random.Range(0, waveHeight);
            if (gridState[x, y] == 1)
            {
                gridState[x, y] = type;
                placed++;
            }
            attempts++;
        }
    }
}