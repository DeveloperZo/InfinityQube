using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private Material highlightMaterial; // For highlighting rain target tile

    [Header("Wave Settings")]
    [SerializeField] private int waveSize = 3;
    [SerializeField][Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField][Range(0f, 1f)] private float greenCubeChance = 0.2f;
    [SerializeField][Range(0f, 1f)] private float blackCubeChance = 0.1f;
    [SerializeField] private bool autoCalculateBlackChance = true;

    [Header("Rain Controls")]
    [SerializeField] private Enumerations.CubeType rainCubeType;
    [SerializeField] private int rainMoveCount = 3; // Number of forward moves before landing
    private int selectedColumn = 0;
    private int selectedRow = 0;
    private GameObject highlightObject;

    [Header("Manual Wave Control")]
    [SerializeField] private bool manualWaveControl = true;
    [SerializeField] private float stepDelay = 0.25f;

    private bool debuggerActive = false;
    private Vector2 scrollPosition;
    private List<GameObject> debugObjects = new List<GameObject>();
    private bool isProcessing = false;
    private Material originalTileMaterial;

    private void Awake()
    {
        // Auto-find references if not set
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();

        // Create highlight material if not set
        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = new Color(0.3f, 0.5f, 1.0f, 0.7f); // Blue highlight
        }

        // Validate prefabs
        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            Debug.LogError("WaveDebugger requires at least 3 cube prefabs (Normal, Green, Black)!");
            enabled = false;
        }
    }

    private void Update()
    {
        // Toggle debugger with key
        if (Input.GetKeyDown(KeyCode.F5))
        {
            debuggerActive = !debuggerActive;
            Debug.Log($"Wave Debugger: {(debuggerActive ? "Active" : "Inactive")}");

            // Update highlight when toggling
            if (debuggerActive)
            {
                UpdateTileHighlight();
            }
            else
            {
                ClearTileHighlight();
            }
        }
    }

    private void OnDestroy()
    {
        // Ensure we remove the highlight when destroyed
        ClearTileHighlight();
    }

    private void OnGUI()
    {
        if (!debuggerActive) return;

        // Main debugger panel
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("=== WAVE DEBUGGER ===", GUI.skin.box);

        // Wave configuration section
        GUILayout.Label("Wave Configuration:", GUI.skin.box);

        waveSize = Mathf.Clamp(EditorIntField("Wave Size:", waveSize), 1, 10);

        GUILayout.Label("Cube Distribution:");
        normalCubeChance = EditorSlider("Normal %:", normalCubeChance * 100, 0f, 100f) / 100f;
        greenCubeChance = EditorSlider("Green %:", greenCubeChance * 100, 0f, 100f) / 100f;

        if (autoCalculateBlackChance)
        {
            blackCubeChance = 1f - normalCubeChance - greenCubeChance;
            GUILayout.Label($"Black %: {blackCubeChance * 100:F1}");
        }
        else
        {
            blackCubeChance = EditorSlider("Black %:", blackCubeChance * 100, 0f, 100f) / 100f;
        }

        autoCalculateBlackChance = GUILayout.Toggle(autoCalculateBlackChance,
            "Auto-calculate Black cube chance");

        // Validate probabilities sum to 1.0
        float totalChance = normalCubeChance + greenCubeChance + blackCubeChance;
        if (!Mathf.Approximately(totalChance, 1.0f))
        {
            GUILayout.Label($"Warning: Chances sum to {totalChance:F2}, not 1.0",
                GUI.skin.box);

            if (GUILayout.Button("Normalize Probabilities"))
            {
                NormalizeProbabilities();
            }
        }

        GUILayout.Space(10);

        manualWaveControl = GUILayout.Toggle(manualWaveControl, "Enable Manual Wave Control");

        GUI.enabled = !isProcessing;

        // Wave controls 
        if (GUILayout.Button("Spawn Wave"))
        {
            SpawnDebugWave();
        }

        if (GUILayout.Button("Reset Grid & Clear Cubes"))
        {
            ClearAllCubes();
            ResetGrid();
        }

        // Manual wave movement
        if (manualWaveControl)
        {
            if (GUILayout.Button("Move Wave Forward"))
            {
                MoveWaveForward();
            }
        }

        GUILayout.Space(10);

        // Rain cube controls
        GUILayout.Label("Rain Single Cube:", GUI.skin.box);

        // Rain cube type selection
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        int selectedIndex = System.Array.IndexOf(typeNames, rainCubeType.ToString());
        if (selectedIndex < 0) selectedIndex = 0;

        rainCubeType = (Enumerations.CubeType)System.Enum.Parse(
            typeof(Enumerations.CubeType),
            typeNames[GUILayout.SelectionGrid(selectedIndex, typeNames, 2)]);

        // Rain target selection
        GUILayout.Label("Rain Target Position:");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Column: ");

        // Create buttons for each column
        if (grid != null)
        {
            for (int i = 0; i < grid.Width; i++)
            {
                GUI.backgroundColor = (selectedColumn == i) ? Color.green : Color.white;
                if (GUILayout.Button(i.ToString(), GUILayout.Width(25)))
                {
                    selectedColumn = i;
                    UpdateTileHighlight();
                }
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Row selection (Z-coordinate)
        GUILayout.BeginHorizontal();
        GUILayout.Label("Row: ");

        // Create buttons for accessible rows
        if (grid != null)
        {
            int maxDisplayRows = Mathf.Min(grid.Height, 10); // Limit to avoid UI clutter
            for (int i = 0; i < maxDisplayRows; i++)
            {
                GUI.backgroundColor = (selectedRow == i) ? Color.green : Color.white;
                if (GUILayout.Button(i.ToString(), GUILayout.Width(25)))
                {
                    selectedRow = i;
                    UpdateTileHighlight();
                }
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Move count before landing
        rainMoveCount = Mathf.Clamp(EditorIntField("Moves before landing:", rainMoveCount), 1, 10);

        if (GUILayout.Button("Rain Cube"))
        {
            RainCube(selectedColumn, selectedRow, rainCubeType, rainMoveCount);
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        // Status information
        GUILayout.Label("Debug Status:", GUI.skin.box);
        int activeCubesCount = 0;
        if (waveManager != null && waveManager.activeCubes != null)
        {
            activeCubesCount = waveManager.activeCubes.Count;
        }
        GUILayout.Label($"Active Cubes: {activeCubesCount}");
        GUILayout.Label($"Debug Objects: {debugObjects.Count}");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private float EditorSlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        float result = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.Label($"{result:F1}", GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return result;
    }

    private int EditorIntField(string label, int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        string result = GUILayout.TextField(value.ToString(), GUILayout.Width(50));
        GUILayout.EndHorizontal();

        int parsedValue;
        if (int.TryParse(result, out parsedValue))
            return parsedValue;
        return value;
    }

    private void NormalizeProbabilities()
    {
        float sum = normalCubeChance + greenCubeChance + blackCubeChance;
        if (sum <= 0)
        {
            // Default values if sum is zero
            normalCubeChance = 0.7f;
            greenCubeChance = 0.2f;
            blackCubeChance = 0.1f;
            return;
        }

        normalCubeChance /= sum;
        greenCubeChance /= sum;
        blackCubeChance /= sum;
    }

    private void SpawnDebugWave()
    {
        if (grid == null) return;

        // First clear any existing cubes
        ClearAllCubes();

        // Prepare cube counts based on probabilities
        int totalCubes = waveSize * grid.Width;
        int normalCount = Mathf.RoundToInt(totalCubes * normalCubeChance);
        int greenCount = Mathf.RoundToInt(totalCubes * greenCubeChance);
        // Black cubes fill the remainder to ensure exact wave size
        int blackCount = totalCubes - normalCount - greenCount;

        Debug.Log($"Spawning debug wave: {normalCount} normal, {greenCount} green, {blackCount} black cubes");

        // Create a list of cube types to spawn
        List<Enumerations.CubeType> typesToSpawn = new List<Enumerations.CubeType>();
        for (int i = 0; i < normalCount; i++) typesToSpawn.Add(Enumerations.CubeType.Normal);
        for (int i = 0; i < greenCount; i++) typesToSpawn.Add(Enumerations.CubeType.Green);
        for (int i = 0; i < blackCount; i++) typesToSpawn.Add(Enumerations.CubeType.Black);

        // Shuffle to randomize placement
        ShuffleList(typesToSpawn);

        // Fill the wave rows
        int cubeIndex = 0;
        for (int z = grid.Height - 1; z > grid.Height - 1 - waveSize; z--)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (cubeIndex >= typesToSpawn.Count) break;

                Vector2Int pos = new Vector2Int(x, z);
                SpawnCube(pos, typesToSpawn[cubeIndex]);
                cubeIndex++;
            }
        }

        // Register with wave manager
        if (waveManager != null)
        {
            waveManager.EnterDebugMode(manualWaveControl);

            // Add cubes to wave manager
            foreach (GameObject obj in debugObjects)
            {
                if (obj != null)
                {
                    CubeBehavior cube = obj.GetComponent<CubeBehavior>();
                    if (cube != null)
                    {
                        waveManager.RegisterCube(cube);
                    }
                }
            }
        }
    }

    private void SpawnCube(Vector2Int position, Enumerations.CubeType type)
    {
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {type}");
            return;
        }

        Vector3 spawnPos = new Vector3(position.x, 1f, position.y);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = type;
            }

            behavior.Init(grid, position, 1);
            debugObjects.Add(cube);

            // Make sure the tile knows about this cube
            UpdateTileReference(position, behavior);
        }
    }

    private void RainCube(int column, int row, Enumerations.CubeType type, int moveCount)
    {
        if (grid == null || column < 0 || column >= grid.Width ||
            row < 0 || row >= grid.Height) return;

        // Find prefab for this type
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {type}");
            return;
        }

        // Calculate spawn position - above the grid
        float spawnHeight = 5f;  // Fixed height above grid
        Vector3 spawnPos = new Vector3(column, spawnHeight, row);

        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = type;
            }

            // IMPORTANT: Set the grid position in Vector2Int where x = column, y = row
            Vector2Int gridPos = new Vector2Int(column, row);
            behavior.Init(grid, gridPos, 1);

            // Mark as a raining cube with move count
            behavior.isRainingCube = true;
            behavior.moveCountRemaining = moveCount;

            debugObjects.Add(cube);

            // Register with wave manager but don't add to active cubes yet
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(behavior);
            }

            Debug.Log($"Created rain cube of type {type} at column {column}, row {row} with {moveCount} moves remaining");
        }
    }

    private void MoveWaveForward()
    {
        if (waveManager == null || !manualWaveControl) return;

        isProcessing = true;
        StartCoroutine(ProcessWaveStep());
    }

    private IEnumerator ProcessWaveStep()
    {
        if (waveManager != null)
        {
            waveManager.ManualMoveWaveForward();
        }

        yield return new WaitForSeconds(stepDelay);
        isProcessing = false;
    }

    private void ClearAllCubes()
    {
        // Clear debug objects first
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null) Destroy(obj);
        }
        debugObjects.Clear();

        // Clear any remaining cubes from WaveManager
        if (waveManager != null)
        {
            waveManager.ClearAllCubes();
            waveManager.ExitDebugMode();
        }
        else
        {
            // Fallback if wave manager reference is missing
            foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
            {
                if (cube != null && cube.gameObject != null)
                {
                    Destroy(cube.gameObject);
                }
            }
        }
    }

    private void ResetGrid()
    {
        if (grid == null) return;

        // Reset all tiles to normal state
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.tiles[x, y] != null)
                {
                    grid.tiles[x, y].ResetTile();
                }
            }
        }

        // Restore highlight
        UpdateTileHighlight();
    }

    private void UpdateTileReference(Vector2Int position, CubeBehavior cube)
    {
        if (grid == null || position.x < 0 || position.x >= grid.Width ||
            position.y < 0 || position.y >= grid.Height) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            tile.currentCube = cube;
        }
    }

    private void UpdateTileHighlight()
    {
        // Remove previous highlight if it exists
        ClearTileHighlight();

        // Make sure grid and position are valid
        if (grid == null || selectedColumn < 0 || selectedColumn >= grid.Width ||
            selectedRow < 0 || selectedRow >= grid.Height) return;

        Tile targetTile = grid.tiles[selectedColumn, selectedRow];
        if (targetTile == null) return;

        // Store original material
        Renderer tileRenderer = targetTile.GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalTileMaterial = tileRenderer.material;

            // Apply highlight material
            tileRenderer.material = highlightMaterial;

            // Create highlight marker
            highlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlightObject.name = "RainTargetHighlight";

            // Position slightly above the tile with a small vertical offset
            highlightObject.transform.position = new Vector3(
                selectedColumn,
                0.05f, // Slightly raised
                selectedRow
            );

            // Scale down slightly to make it visually distinct
            highlightObject.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);

            // Apply the highlight material
            Renderer highlightRenderer = highlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.material = highlightMaterial;
            }

            // Remove collider to avoid physics interference
            Collider highlightCollider = highlightObject.GetComponent<Collider>();
            if (highlightCollider != null)
            {
                Destroy(highlightCollider);
            }
        }
    }

    private void ClearTileHighlight()
    {
        // Restore original material if possible
        if (grid != null && originalTileMaterial != null &&
            selectedColumn >= 0 && selectedColumn < grid.Width &&
            selectedRow >= 0 && selectedRow < grid.Height)
        {
            Tile targetTile = grid.tiles[selectedColumn, selectedRow];
            if (targetTile != null)
            {
                Renderer tileRenderer = targetTile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material = originalTileMaterial;
                }
            }
        }

        // Destroy highlight object
        if (highlightObject != null)
        {
            Destroy(highlightObject);
            highlightObject = null;
        }
    }

    // Helper to shuffle a list (Fisher-Yates)
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + Random.Range(0, n - i);
            T temp = list[r];
            list[r] = list[i];
            list[i] = temp;
        }
    }
}