using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class StaticCubeDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private Material highlightMaterial;

    [Header("Test Configuration")]
    [SerializeField] private int cubeRow = 3;        // Row for test cubes
    [SerializeField] private int tileEffectRow = 2;  // Row for tile effects
    [SerializeField] private float spawnHeight = 5f; // Height for falling cubes

    [Header("Drop Settings")]
    [SerializeField] private Enumerations.CubeType dropCubeType = Enumerations.CubeType.Black;
    [SerializeField] private bool dropOnCubes = true;
    [SerializeField] private bool dropOnTiles = true;
    [SerializeField] private bool autoTileEffects = true;
    [SerializeField] private int rainMoveCount = 3;  // Moves before landing

    [Header("Wave Control")]
    [SerializeField] private bool manualMode = true;
    [SerializeField] private KeyCode moveKey = KeyCode.M;
    [SerializeField] private KeyCode testKey = KeyCode.T;
    [SerializeField] private KeyCode clearKey = KeyCode.C;

    private bool debuggerActive = false;
    private Dictionary<int, GameObject> columnCubes = new Dictionary<int, GameObject>();
    private Dictionary<int, Enumerations.CubeType> columnCubeTypes = new Dictionary<int, Enumerations.CubeType>();
    public List<GameObject> debugObjects = new List<GameObject>();
    private Vector2 scrollPosition;
    private Coroutine autoMoveCoroutine;

    private void Start()
    {
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();

        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = new Color(0.3f, 0.5f, 1.0f, 0.5f);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            debuggerActive = !debuggerActive;
            if (debuggerActive) waveManager?.EnterDebugMode(manualMode);
            else waveManager?.ExitDebugMode();
        }

        if (!debuggerActive) return;

        if (Input.GetKeyDown(moveKey) && manualMode)
        {
            MoveWaveForward();
        }

        if (Input.GetKeyDown(testKey))
        {
            ExecuteTest();
        }

        if (Input.GetKeyDown(clearKey))
        {
            ClearDebugObjects();
        }
    }

    private void OnGUI()
    {
        if (!debuggerActive) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("STATIC CUBE DEBUGGER", GUI.skin.box);

        // Configuration options
        GUILayout.Label("Configuration:", GUI.skin.box);
        cubeRow = EditorIntField("Cube Row:", cubeRow);
        tileEffectRow = EditorIntField("Tile Effect Row:", tileEffectRow);

        // Drop settings
        GUILayout.Label("Drop Settings:", GUI.skin.box);
        dropOnCubes = GUILayout.Toggle(dropOnCubes, "Drop On Cubes");
        dropOnTiles = GUILayout.Toggle(dropOnTiles, "Drop On Tiles");
        autoTileEffects = GUILayout.Toggle(autoTileEffects, "Auto-Create Tile Effects");
        rainMoveCount = EditorIntField("Rain Move Count:", rainMoveCount);

        // Falling cube selection
        GUILayout.Label("Falling Cube Type:", GUI.skin.box);
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        int selectedIndex = System.Array.IndexOf(typeNames, dropCubeType.ToString());
        if (selectedIndex < 0) selectedIndex = 0;

        dropCubeType = (Enumerations.CubeType)System.Enum.Parse(
            typeof(Enumerations.CubeType),
            typeNames[GUILayout.SelectionGrid(selectedIndex, typeNames, 2)]);

        // Column cube setup
        GUILayout.Label("Column Setup:", GUI.skin.box);

        for (int col = 0; col < Mathf.Min(grid.Width, 6); col++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Col {col}", GUILayout.Width(50));

            // Clear this column button
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                ClearColumn(col);
            }

            // Cube type buttons
            if (GUILayout.Button("Gray", GUILayout.Width(60)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Normal);
            }

            if (GUILayout.Button("Green", GUILayout.Width(60)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Green);
            }

            if (GUILayout.Button("Black", GUILayout.Width(60)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Black);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Action buttons
        if (GUILayout.Button("Execute Test (T)"))
        {
            ExecuteTest();
        }

        if (GUILayout.Button("Clear All Debug Objects (C)"))
        {
            ClearDebugObjects();
        }

        if (GUILayout.Button(manualMode ? "Enable Auto-Movement" : "Enable Manual Movement"))
        {
            manualMode = !manualMode;
            if (waveManager != null) waveManager.manualControl = manualMode;

            if (manualMode)
            {
                StopAutoMode();
            }
            else
            {
                StartAutoMode();
            }
        }

        if (manualMode && GUILayout.Button("Move Wave Forward (M)"))
        {
            MoveWaveForward();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private int EditorIntField(string label, int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        string result = GUILayout.TextField(value.ToString(), GUILayout.Width(50));
        GUILayout.EndHorizontal();

        int parsedValue;
        if (int.TryParse(result, out parsedValue))
            return parsedValue;
        return value;
    }

    private void UpdateColumnCube(int column, Enumerations.CubeType cubeType)
    {
        // Remove existing cube if any
        if (columnCubes.ContainsKey(column) && columnCubes[column] != null)
        {
            Destroy(columnCubes[column]);
            debugObjects.Remove(columnCubes[column]);
            columnCubes.Remove(column);
        }

        // Set the new type
        columnCubeTypes[column] = cubeType;

        // Get the correct prefab
        int prefabIndex = (int)cubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {cubeType}");
            return;
        }

        // Create the new cube
        Vector3 spawnPos = new Vector3(column, 1f, cubeRow);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        // Initialize cube
        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior == null)
        {
            behavior = cube.AddComponent<CubeBehavior>();
            behavior.CubeType = cubeType;
        }

        behavior.Init(grid, new Vector2Int(column, cubeRow), 1, 1);

        // Update tile reference
        if (cubeRow >= 0 && cubeRow < grid.Height)
        {
            Tile tile = grid.tiles[column, cubeRow];
            if (tile != null)
            {
                tile.currentCube = behavior;
            }
        }

        // Update tracking
        columnCubes[column] = cube;
        debugObjects.Add(cube);

        // Always reset and reapply tile effect in the row below when changing cube type
        if (autoTileEffects && tileEffectRow >= 0 && tileEffectRow < grid.Height)
        {
            Tile effectTile = grid.tiles[column, tileEffectRow];
            if (effectTile != null)
            {
                // First reset the tile
                ResetTile(effectTile);

                // Then apply the new effect if needed
                if (cubeType != Enumerations.CubeType.Normal)
                {
                    ApplyTileEffect(column, cubeType);
                }
            }
        }
    }

    private void ApplyTileEffect(int column, Enumerations.CubeType cubeType)
    {
        if (grid == null || column < 0 || column >= grid.Width ||
            tileEffectRow < 0 || tileEffectRow >= grid.Height)
            return;

        Tile tile = grid.tiles[column, tileEffectRow];
        if (tile != null)
        {
            // Apply actual transformation
            tile.TransformTile(cubeType);
        }
    }

    private void ResetTile(Tile tile)
    {
        if (tile == null) return;

        // Call the tile's built-in reset method
        tile.ResetTile();
    }

    private void ClearColumn(int column)
    {
        // Clear the cube
        if (columnCubes.ContainsKey(column) && columnCubes[column] != null)
        {
            Destroy(columnCubes[column]);
            debugObjects.Remove(columnCubes[column]);
            columnCubes.Remove(column);
        }

        if (columnCubeTypes.ContainsKey(column))
        {
            columnCubeTypes.Remove(column);
        }

        // Clear the tile effect
        if (tileEffectRow >= 0 && tileEffectRow < grid.Height)
        {
            Tile tile = grid.tiles[column, tileEffectRow];
            if (tile != null)
            {
                ResetTile(tile);
            }
        }
    }

    private void ExecuteTest()
    {
        // Get list of columns to process
        List<int> columnsToProcess = new List<int>();

        // Add all populated columns
        foreach (int column in columnCubeTypes.Keys)
        {
            if (!columnsToProcess.Contains(column))
            {
                columnsToProcess.Add(column);
            }
        }

        // Drop cubes on all needed columns
        foreach (int column in columnsToProcess)
        {
            DropCubeOnColumn(column);
        }

        // Register with wave manager for movement
        List<CubeBehavior> newActiveCubes = new List<CubeBehavior>();
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null)
            {
                CubeBehavior cube = obj.GetComponent<CubeBehavior>();
                if (cube != null && cube.isRainingCube)
                {
                    newActiveCubes.Add(cube);
                }
            }
        }

        if (waveManager != null && newActiveCubes.Count > 0)
        {
            waveManager.EnterDebugMode(manualMode);

            foreach (CubeBehavior cube in newActiveCubes)
            {
                waveManager.RegisterRainCube(cube);
            }
        }
    }

    private void DropCubeOnColumn(int column)
    {
        if (column < 0 || column >= grid.Width) return;

        // Determine if we should drop on this column
        bool hasStaticCube = columnCubes.ContainsKey(column) && columnCubes[column] != null;
        bool hasTileEffect = false;

        if (tileEffectRow >= 0 && tileEffectRow < grid.Height)
        {
            Tile tile = grid.tiles[column, tileEffectRow];
            hasTileEffect = tile != null && tile.currentState == Enumerations.TileState.Transformed;
        }

        // Skip if no target to drop on
        if ((!dropOnCubes || !hasStaticCube) && (!dropOnTiles || !hasTileEffect)) return;

        // Get prefab for falling cube
        int prefabIndex = (int)dropCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {dropCubeType}");
            return;
        }

        // Spawn falling cubes
        if (dropOnCubes && hasStaticCube)
        {
            SpawnRainCube(column, cubeRow, dropCubeType);
        }

        if (dropOnTiles && hasTileEffect)
        {
            SpawnRainCube(column, tileEffectRow, dropCubeType);
        }
    }

    private void SpawnRainCube(int column, int row, Enumerations.CubeType cubeType)
    {
        Vector3 spawnPos = new Vector3(column, spawnHeight, row);
        GameObject cube = Instantiate(cubePrefabs[(int)cubeType], spawnPos, Quaternion.identity);

        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior == null)
        {
            behavior = cube.AddComponent<CubeBehavior>();
        }

        behavior.CubeType = cubeType;
        behavior.Init(grid, new Vector2Int(column, row), 1);
        behavior.isRainingCube = true;
        behavior.moveCountRemaining = rainMoveCount;

        debugObjects.Add(cube);
    }

    private void MoveWaveForward()
    {
        if (waveManager == null) return;

        waveManager.ManualMoveWaveForward();
    }

    private void ClearDebugObjects()
    {
        // Clear all spawned objects
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null) Destroy(obj);
        }

        debugObjects.Clear();
        columnCubes.Clear();
        columnCubeTypes.Clear();

        // Reset all effect tiles
        if (grid != null && tileEffectRow >= 0 && tileEffectRow < grid.Height)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Tile tile = grid.tiles[x, tileEffectRow];
                if (tile != null)
                {
                    ResetTile(tile);
                }
            }
        }

        // Reset wave manager state
        if (waveManager != null)
        {
            waveManager.ClearAllCubes();
        }
    }

    private void StartAutoMode()
    {
        StopAutoMode(); // Ensure any existing routine is stopped

        if (!manualMode)
        {
            autoMoveCoroutine = StartCoroutine(AutoMoveRoutine());
        }
    }

    private void StopAutoMode()
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
            autoMoveCoroutine = null;
        }
    }

    private IEnumerator AutoMoveRoutine()
    {
        while (debuggerActive && !manualMode)
        {
            // Wait a short time between moves
            yield return new WaitForSeconds(0.5f);

            // Check if any cubes are still active
            if (waveManager != null && waveManager.activeCubes.Count > 0)
            {
                MoveWaveForward();
            }
            else
            {
                // No more cubes, stop auto movement
                yield break;
            }
        }
    }



}