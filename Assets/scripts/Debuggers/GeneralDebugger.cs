using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

public class GeneralDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private WaveDebugger waveDebugger; // Normal, Green, Black, Blue

    [Header("Rain Controls")]
    [SerializeField] private bool keepRainCubesInPlace = true;
    [SerializeField] private int rainMoveCount = 3;

    [Header("Debug Controls")]
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.F1;
    [SerializeField] private KeyCode resetDebugKey = KeyCode.F2;
    [SerializeField] private KeyCode executeTestKey = KeyCode.F3;
    [SerializeField] private float spawnHeight = 5f;
    [SerializeField] private int defaultRowZ = 3; // Default test row
    [SerializeField] private int effectRowZ = 2;  // Row for tile effects (one below the test row)

    // Cube testing state 
    private Enumerations.CubeType fallingCubeType = Enumerations.CubeType.Black;
    private bool debugModeActive = false;
    private List<GameObject> debugObjects = new List<GameObject>();

    // Column state tracking
    private Dictionary<int, Enumerations.CubeType> columnCubeTypes = new Dictionary<int, Enumerations.CubeType>();
    private Dictionary<int, GameObject> columnCubes = new Dictionary<int, GameObject>();

    // Testing options
    private bool dropOnEmptyColumns = false;
    private bool dropOnCubes = false;
    private bool dropOnTiles = false;
    private bool autoTileEffects = true;
    private bool showWaveOptions = true;
    private Vector2 scrollPosition;

    private void Start()
    {
        // Auto-find references if not set
        if (grid == null) grid = FindObjectOfType<GridManager>();
    }

    private void Update()
    {
        // Toggle debug mode
        if (Input.GetKeyDown(toggleDebugKey))
        {
            debugModeActive = !debugModeActive;
            Debug.Log($"Debug Mode: {(debugModeActive ? "ON" : "OFF")}");
        }

        if (!debugModeActive) return;

        // Reset debug objects only
        if (Input.GetKeyDown(resetDebugKey))
        {
            ClearDebugObjects();
        }

        // Execute the configured test
        if (Input.GetKeyDown(executeTestKey))
        {
            ExecuteTest();
        }
    }

    public void TestTransformTile(int column, Enumerations.CubeType cubeType)
    {
        if (grid == null || column < 0 || column >= grid.Width) return;

        // Transform the tile in the test row
        Tile tile = grid.tiles[column, defaultRowZ];
        if (tile != null)
        {
            tile.TransformTile(cubeType);
            Debug.Log($"Transformed tile at column {column} to {cubeType} type");
        }
    }

    public void TestLandCubeOnTransformedTile(int column, Enumerations.CubeType cubeType)
    {
        if (grid == null || column < 0 || column >= grid.Width) return;

        // Get the tile
        Tile tile = grid.tiles[column, defaultRowZ];
        if (tile == null) return;

        // Create test cube
        Vector3 spawnPos = new Vector3(column, 1f, defaultRowZ);
        GameObject cube = Instantiate(cubePrefabs[(int)cubeType], spawnPos, Quaternion.identity);

        // Set up cube behavior
        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior != null)
        {
            behavior.Init(grid, new Vector2Int(column, defaultRowZ), 1);
            behavior.CubeType = cubeType;

            // Trigger landing on transformed tile
            tile.HandleCubeLanding(behavior);
        }
    }

    private void ClearDebugObjects()
    {
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null) Destroy(obj);
        }
        debugObjects.Clear();
        columnCubes.Clear();

        // Reset all tiles in the test area
        if (grid != null)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                // Reset effect row tiles
                if (effectRowZ >= 0 && effectRowZ < grid.Height)
                {
                    Tile tile = grid.tiles[x, effectRowZ];
                    if (tile != null)
                    {
                        ResetTile(tile);
                    }
                }
            }
        }
    }

    private void ResetTile(Tile tile)
    {
        // Call the ResetTile method on the tile if it exists
        // This assumes you've added this method to the Tile class
        System.Reflection.MethodInfo resetMethod = typeof(Tile).GetMethod("ResetTile",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (resetMethod != null)
        {
            resetMethod.Invoke(tile, null);
        }
        else
        {
            // Fallback reset
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }

            // Reset position
            tile.transform.position = new Vector3(tile.transform.position.x, 0f, tile.transform.position.z);
        }
    }

    private void ClearColumn(int column)
    {
        Tile cubeTile = grid.tiles[column, defaultRowZ];
        ResetTile(cubeTile);

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

        // Reset effect tile
        if (autoTileEffects && grid != null && effectRowZ >= 0 && effectRowZ < grid.Height)
        {
            Tile tile = grid.tiles[column, effectRowZ];
            if (tile != null)
            {
                ResetTile(tile);
            }
        }
    }

    private void ExecuteTest()
    {
        // Get occupied columns
        List<int> columnsToProcess = new List<int>();

        // Add all populated columns first
        foreach (int column in columnCubeTypes.Keys)
        {
            if (!columnsToProcess.Contains(column))
            {
                columnsToProcess.Add(column);
            }
        }

        // If dropping on empty columns is enabled, add all columns
        if (dropOnEmptyColumns)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!columnsToProcess.Contains(x))
                {
                    columnsToProcess.Add(x);
                }
            }
        }

        // Drop cubes on all columns
        foreach (int column in columnsToProcess)
        {
            DropCubeOnColumn(column);
        }
    }

    // In GeneralDebugger.cs - DropCubeOnColumn method
    private void DropCubeOnColumn(int column)
    {
        if (column < 0 || column >= grid.Width)
        {
            Debug.LogWarning($"Column {column} is out of bounds");
            return;
        }

        Vector2Int targetPos = new Vector2Int(column, defaultRowZ);

        // Get the correct prefab for falling cube
        int prefabIndex = (int)fallingCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab at index {prefabIndex}");
            return;
        }

        if (dropOnCubes)
        {
            // Calculate spawn height based on move count
            float spawnHeight = rainMoveCount * 2f + 1f;

            // Spawn falling cube above the column
            Vector3 spawnPos = new Vector3(column, spawnHeight, defaultRowZ);
            GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

            // Set up the falling cube
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior != null)
            {
                behavior.Init(grid, targetPos, 1);
                behavior.CubeType = fallingCubeType;
                behavior.isRainingCube = true;
                behavior.moveCountRemaining = keepRainCubesInPlace ? int.MaxValue : rainMoveCount;

                // Start falling animation coroutine
                StartCoroutine(AnimateCubeFalling(behavior, spawnPos, column, defaultRowZ));
            }

            // Add collision controller if needed
            CubeCollisionController collisionController = cube.GetComponent<CubeCollisionController>();
            if (collisionController == null)
            {
                collisionController = cube.AddComponent<CubeCollisionController>();
                collisionController.Initialize(grid);
            }

            debugObjects.Add(cube);
        }

        if (dropOnTiles)
        {
            // Also add a cube to the effect row for triggering tile effects
            if (grid != null && effectRowZ >= 0 && effectRowZ < grid.Height)
            {
                Tile effectTile = grid.tiles[column, effectRowZ];
                if (effectTile != null && effectTile.currentState == Enumerations.TileState.Transformed)
                {
                    // Don't drop a cube on blackened tiles (can't be triggered)
                    if (!effectTile.IsBlackened)
                    {
                        // Spawn a test cube to trigger the tile effect
                        Vector3 effectSpawnPos = new Vector3(column, spawnHeight, effectRowZ);
                        GameObject effectCube = Instantiate(cubePrefabs[prefabIndex], effectSpawnPos, Quaternion.identity);

                        // Set up the effect cube
                        CubeBehavior effectBehavior = effectCube.GetComponent<CubeBehavior>();
                        if (effectBehavior != null)
                        {
                            effectBehavior.Init(grid, new Vector2Int(column, effectRowZ), 1);
                            effectBehavior.CubeType = fallingCubeType;
                            effectBehavior.isRainingCube = true;

                            // Start falling animation coroutine
                            StartCoroutine(AnimateCubeFalling(effectBehavior, effectSpawnPos, column, effectRowZ));
                        }

                        // Add collision controller if needed
                        CubeCollisionController effectController = effectCube.GetComponent<CubeCollisionController>();
                        if (effectController == null)
                        {
                            effectController = effectCube.AddComponent<CubeCollisionController>();
                            effectController.Initialize(grid);
                        }

                        debugObjects.Add(effectCube);
                    }
                }
            }
        }
    }

    private IEnumerator AnimateCubeFalling(CubeBehavior cube, Vector3 startPos, int x, int z)
    {
        if (cube == null) yield break;

        // Target position (1 unit above the ground)
        Vector3 targetPos = new Vector3(x, 1f, z);

        // Fall duration
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null || cube.gameObject == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-in falling animation
            float easedT = 1 - Mathf.Pow(1 - t, 2); // Quadratic ease-out

            // Update position
            cube.transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            yield return null;
        }

        // Ensure final position
        if (cube != null && cube.gameObject != null)
        {
            cube.transform.position = targetPos;

            // Small bounce effect
            StartCoroutine(BounceCube(cube));
        }
    }

    private IEnumerator BounceCube(CubeBehavior cube)
    {
        if (cube == null || cube.gameObject == null) yield break;

        Vector3 originalPos = cube.transform.position;
        Vector3 squashScale = new Vector3(1.2f, 0.8f, 1.2f);
        Vector3 originalScale = cube.transform.localScale;

        // Squash
        float squashDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < squashDuration)
        {
            if (cube == null || cube.gameObject == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashDuration);

            cube.transform.localScale = Vector3.Lerp(originalScale, squashScale, t);

            yield return null;
        }

        // Return to original scale
        elapsed = 0f;
        while (elapsed < squashDuration)
        {
            if (cube == null || cube.gameObject == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashDuration);

            cube.transform.localScale = Vector3.Lerp(squashScale, originalScale, t);

            yield return null;
        }

        // Ensure final scale
        if (cube != null && cube.gameObject != null)
        {
            cube.transform.localScale = originalScale;

            // Check for cubes below
            CheckForCubeBelow(cube);
        }
    }

    private void CheckForCubeBelow(CubeBehavior cube)
    {
        if (cube == null || grid == null) return;

        Vector2Int position = cube.position;

        // Check if there's a cube at this position already
        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null && tile.currentCube != null && tile.currentCube != cube)
        {
            CubeBehavior targetCube = tile.currentCube;

            // Get collision controller
            CubeCollisionController controller = cube.GetComponent<CubeCollisionController>();
            if (controller == null)
            {
                controller = cube.AddComponent<CubeCollisionController>();
                controller.Initialize(grid);
            }

            // Trigger collision
            Debug.Log($"Triggering collision between falling {cube.CubeType} and {targetCube.CubeType} at ({position.x}, {position.y})");
            controller.HandleCubeCollision(cube, targetCube, position);
        }
        else if (tile != null)
        {
            // Update tile reference
            tile.currentCube = cube;

            // If tile has a marker, trigger it
            if (tile.HasMarker)
            {
                tile.ProcessCubeInteraction(cube);
                tile.TriggerMarker();
            }
        }
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
            Debug.LogWarning($"Invalid cube prefab at index {prefabIndex}");
            return;
        }

        // Create the new cube
        Vector3 spawnPos = new Vector3(column, 1f, defaultRowZ);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        // Initialize cube
        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior != null)
        {
            behavior.Init(grid, new Vector2Int(column, defaultRowZ), 1);
            behavior.CubeType = cubeType;
        }

        // Update tile reference
        Tile tile = grid.tiles[column, defaultRowZ];
        if (tile != null)
        {
            tile.currentCube = behavior;
        }

        // Update tracking
        columnCubes[column] = cube;
        debugObjects.Add(cube);

        // Always reset and reapply tile effect in the row below when changing cube type
        if (autoTileEffects && grid != null && effectRowZ >= 0 && effectRowZ < grid.Height)
        {
            Tile effectTile = grid.tiles[column, effectRowZ];
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
            effectRowZ < 0 || effectRowZ >= grid.Height)
            return;

        Tile tile = grid.tiles[column, effectRowZ];
        if (tile != null)
        {
            // Apply actual transformation
            tile.TransformTile(cubeType);

            // If it's a green cube and we want to test multiple charges
            if (cubeType == Enumerations.CubeType.Green)
            {
                // Add a second charge for testing (Optional)
                // tile.EnhanceGreenTile();
            }
        }
    }

    private void OnGUI()
    {
        if (!debugModeActive) return;

        scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 700, 700), scrollPosition, new Rect(0, 0, 700, 1500));

        GUILayout.Label("=== STATIC CUBE DEBUGGER ===", GUI.skin.box);

        GUILayout.Space(5);
        GUILayout.Label($"Test Row: {defaultRowZ}, Effect Row: {effectRowZ}");
        dropOnCubes = GUILayout.Toggle(dropOnCubes, "Drop On Cubes");
        dropOnTiles = GUILayout.Toggle(dropOnTiles, "Drop On Tiles");

        // Coordinate with wave debugger
        if (waveDebugger != null)
        {
            showWaveOptions = GUILayout.Toggle(showWaveOptions, "Show Wave Options");
        }

        // Column cube setup
        GUILayout.Label("Column Setup:", GUI.skin.box);

        for (int col = 0; col < Mathf.Min(grid.Width, 6); col++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Col {col}", GUILayout.Width(50));

            // Clear this column button
            if (GUILayout.Button("Clear", GUILayout.Width(75)))
            {
                ClearColumn(col);
            }

            // Cube type buttons
            if (GUILayout.Button("Gray", GUILayout.Width(75)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Normal);
            }

            if (GUILayout.Button("Green", GUILayout.Width(75)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Green);
            }

            if (GUILayout.Button("Black", GUILayout.Width(75)))
            {
                UpdateColumnCube(col, Enumerations.CubeType.Black);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Falling cube selection
        GUILayout.Label("Falling Cube Type:", GUI.skin.box);
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        if (typeNames.Length > 0)
        {
            // Skip "None" type if it exists
            int startIndex = System.Array.IndexOf(typeNames, "None") >= 0 ? 1 : 0;
            string[] validTypes = new string[typeNames.Length - startIndex];
            for (int i = startIndex; i < typeNames.Length; i++)
            {
                validTypes[i - startIndex] = typeNames[i];
            }

            // Button grid for types
            int selectedIndex = System.Array.IndexOf(validTypes, fallingCubeType.ToString());
            if (selectedIndex < 0) selectedIndex = 0;

            fallingCubeType = (Enumerations.CubeType)System.Enum.Parse(
                typeof(Enumerations.CubeType),
                validTypes[GUILayout.SelectionGrid(selectedIndex, validTypes, 2)]);
        }

        GUILayout.Space(10);

        // Options
        GUILayout.Label("Test Options:", GUI.skin.box);
        dropOnEmptyColumns = GUILayout.Toggle(dropOnEmptyColumns, "Drop On Empty Columns Too");

        // Toggle for automatic tile effects
        bool oldAutoTileEffects = autoTileEffects;
        autoTileEffects = GUILayout.Toggle(autoTileEffects, "Auto-Create Tile Effects Below Cubes");

        // If toggled off, reset tile effects
        if (oldAutoTileEffects && !autoTileEffects && grid != null)
        {
            // Reset all effect tiles
            for (int x = 0; x < grid.Width; x++)
            {
                if (effectRowZ >= 0 && effectRowZ < grid.Height)
                {
                    Tile tile = grid.tiles[x, effectRowZ];
                    if (tile != null)
                    {
                        ResetTile(tile);
                    }
                }
            }
        }
        // If toggled on, apply effects
        else if (!oldAutoTileEffects && autoTileEffects)
        {
            // Apply effects for all existing cubes
            foreach (var kvp in columnCubeTypes)
            {
                if (kvp.Value != Enumerations.CubeType.Normal)
                {
                    ApplyTileEffect(kvp.Key, kvp.Value);
                }
            }
        }

        GUILayout.Space(10);

        // Action buttons
        if (GUILayout.Button($"Execute Test (F3)"))
        {
            ExecuteTest();
        }

        if (GUILayout.Button($"Clear All Debug Objects (F2)"))
        {
            ClearDebugObjects();
        }

        if (GUILayout.Button("Force Check All Collisions"))
        {
            CheckAllOverlappingCubes();
        }
        GUILayout.Space(20);
        GUILayout.Label("Debugger Coordination:", GUI.skin.box);

        if (waveDebugger != null)
        {
            if (GUILayout.Button("Switch to Wave Debugger (F5)"))
            {
                // This will toggle WaveDebugger on and this debugger off
                SendMessage("SetDebuggerState", false);
                waveDebugger.SendMessage("SetDebuggerState", true);
            }
        }


        GUI.EndScrollView();
    }

    public void SetDebuggerState(bool active)
    {
        debugModeActive = active;
    }

    private void CheckAllOverlappingCubes()
    {
        // Group all cubes by position
        Dictionary<Vector2Int, List<CubeBehavior>> cubesByPosition = new Dictionary<Vector2Int, List<CubeBehavior>>();

        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            Vector2Int pos = cube.position;
            if (!cubesByPosition.ContainsKey(pos))
            {
                cubesByPosition[pos] = new List<CubeBehavior>();
            }
            cubesByPosition[pos].Add(cube);
        }

        // Process all positions with multiple cubes
        foreach (var kvp in cubesByPosition)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.Log($"Found {kvp.Value.Count} cubes at position ({kvp.Key.x}, {kvp.Key.y})");

                // Get the first two cubes for collision
                CubeBehavior cube1 = kvp.Value[0];
                CubeBehavior cube2 = kvp.Value[1];

                // Get or add collision controller to the first cube
                CubeCollisionController controller = cube1.GetComponent<CubeCollisionController>();
                if (controller == null)
                {
                    controller = cube1.AddComponent<CubeCollisionController>();
                    controller.Initialize(grid);
                }

                // Trigger collision
                Debug.Log($"Forcing collision between {cube1.CubeType} and {cube2.CubeType}");
                controller.HandleCubeCollision(cube1, cube2, kvp.Key);
            }
        }
    }
}