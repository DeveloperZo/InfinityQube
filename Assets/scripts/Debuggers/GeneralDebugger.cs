using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

public class GeneralDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black

    [Header("Debug Controls")]
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.F1;
    [SerializeField] private KeyCode resetDebugKey = KeyCode.F2;
    [SerializeField] private KeyCode placeTargetKey = KeyCode.F3;
    [SerializeField] private KeyCode dropFallingKey = KeyCode.F4;
    [SerializeField] private float spawnHeight = 5f;

    [Header("Multi-Target Debug")]
    [SerializeField] private bool enableMultiTarget = false;
    [SerializeField] private Vector2Int multiGridSize = new Vector2Int(3, 3);
    [SerializeField] private float multiSpacing = 1.5f;
    [SerializeField] private KeyCode spawnAllKey = KeyCode.F6;
    [SerializeField] private KeyCode markRowKey = KeyCode.F7;


    // Cube testing state 
    private Enumerations.CubeType fallingCubeType = Enumerations.CubeType.Black;
    private Enumerations.CubeType targetCubeType = Enumerations.CubeType.Black;
    private bool debugModeActive = false;
    private List<GameObject> debugObjects = new List<GameObject>();
    private void SpawnMultipleTargets()
    {
        if (grid == null)
        {
            Debug.LogWarning("Grid reference missing!");
            return;
        }

        // Calculate center of the multi-grid
        Vector2Int centerPos = FindMarkedTile();
        if (centerPos.x < 0)
        {
            Debug.Log("No marked tile found. Place a marker first (Space).");
            return;
        }

        // Get correct prefab for target cubes
        int prefabIndex = (int)targetCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab at index {prefabIndex}");
            return;
        }

        // Calculate grid bounds
        int startX = Mathf.Max(0, centerPos.x - multiGridSize.x / 2);
        int endX = Mathf.Min(grid.Width - 1, centerPos.x + multiGridSize.x / 2);
        int startY = Mathf.Max(0, centerPos.y - multiGridSize.y / 2);
        int endY = Mathf.Min(grid.Height - 1, centerPos.y + multiGridSize.y / 2);

        // Spawn target cubes in grid pattern
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                // Don't spawn on the center (marked tile)
                if (x == centerPos.x && y == centerPos.y)
                    continue;

                // Check if there's already a cube at this position
                if (grid.tiles[x, y] != null && grid.tiles[x, y].currentCube != null)
                    continue;

                // Spawn the cube
                Vector3 spawnPos = new Vector3(x, 1f, y);
                GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

                // Initialize it
                CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
                if (behavior != null)
                {
                    behavior.Init(grid, new Vector2Int(x, y), 1);
                    behavior.CubeType = targetCubeType;
                }

                // Store the cube reference in the tile
                Tile tile = grid.tiles[x, y];
                if (tile != null)
                {
                    tile.currentCube = behavior;
                }

                debugObjects.Add(cube);
            }
        }
    }

    private void Start()
    {
        // Auto-find references if not set
        if (grid == null) grid = FindObjectOfType<GridManager>();

        // Find cube prefabs if not set
        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            cubePrefabs = new GameObject[3];

            Transform cubeParent = GameObject.Find("CubeParent")?.transform;
            if (cubeParent != null)
            {
                foreach (Transform child in cubeParent)
                {
                    if (child.name.Contains("Normal"))
                        cubePrefabs[0] = child.gameObject;
                    else if (child.name.Contains("Green"))
                        cubePrefabs[1] = child.gameObject;
                    else if (child.name.Contains("Black"))
                        cubePrefabs[2] = child.gameObject;
                }
            }
        }
    }

    private void MarkRowOfTiles()
    {
        if (grid == null) return;

        // Find current marker position
        Vector2Int markerPos = FindMarkedTile();
        if (markerPos.x < 0)
        {
            Debug.Log("No marked tile found. Place a marker first (Space) to define the row.");
            return;
        }

        // Mark the entire row
        for (int x = 0; x < grid.Width; x++)
        {
            // Skip tiles that already have markers
            if (grid.tiles[x, markerPos.y] != null && !grid.tiles[x, markerPos.y].HasMarker)
            {
                grid.PlaceMarker(x, markerPos.y);
            }
        }
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

        // Place target cube on marked tile
        if (Input.GetKeyDown(placeTargetKey))
        {
            PlaceTargetCubeOnMarker();
        }

        // Drop falling cube on marked tile
        if (Input.GetKeyDown(dropFallingKey))
        {
            DropFallingCubeOnMarker();
        }

        if (Input.GetKeyDown(spawnAllKey))
        {
            SpawnMultipleTargets();
        }

        if (Input.GetKeyDown(markRowKey))
        {
            MarkRowOfTiles();
        }
    }

    private void ClearDebugObjects()
    {
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null) Destroy(obj);
        }
        debugObjects.Clear();
    }

    private Vector2Int FindMarkedTile()
    {
        // Look for a marked tile
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.tiles[x, y] != null && grid.tiles[x, y].HasMarker)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1); // No marker found
    }

    private void PlaceTargetCubeOnMarker()
    {
        Vector2Int markerPos = FindMarkedTile();
        if (markerPos.x < 0)
        {
            Debug.Log("No marked tile found. Place a marker first (Space).");
            return;
        }

        // Get correct prefab
        int prefabIndex = (int)targetCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab at index {prefabIndex}");
            return;
        }

        // Spawn the cube
        Vector3 spawnPos = new Vector3(markerPos.x, 1f, markerPos.y);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        // Initialize it
        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior != null)
        {
            behavior.Init(grid, markerPos, 1);
            behavior.CubeType = targetCubeType;
        }

        // Store the cube reference in the tile
        Tile tile = grid.tiles[markerPos.x, markerPos.y];
        if (tile != null)
        {
            tile.currentCube = behavior;
        }

        debugObjects.Add(cube);
    }

    // In GeneralDebugger.cs
    private void DropFallingCubeOnMarker()
    {
        Vector2Int markerPos = FindMarkedTile();
        if (markerPos.x < 0)
        {
            Debug.Log("No marked tile found. Place a marker first (Space).");
            return;
        }

        // Get correct prefab
        int prefabIndex = (int)fallingCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab at index {prefabIndex}");
            return;
        }

        // Check if there's already a cube at this tile
        CubeBehavior existingCube = null;
        if (grid.tiles[markerPos.x, markerPos.y] != null)
        {
            existingCube = grid.tiles[markerPos.x, markerPos.y].currentCube;
        }

        // Spawn falling cube directly above the marker
        Vector3 spawnPos = new Vector3(markerPos.x, spawnHeight, markerPos.y);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        // Set the cube type
        CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
        if (behavior != null)
        {
            behavior.Init(grid, markerPos, 1);
            cube.transform.position = spawnPos;
            behavior.CubeType = fallingCubeType;
            behavior.isRainingCube = true;
        }

        // Add collision controller if it doesn't exist
        CubeCollisionController collisionController = cube.GetComponent<CubeCollisionController>();
        if (collisionController == null)
        {
            collisionController = cube.AddComponent<CubeCollisionController>();
            collisionController.Initialize(markerPos, grid);
        }

        // If there's already a cube at this position, trigger collision check immediately
        if (existingCube != null)
        {
            Debug.Log($"Cube dropped onto existing cube of type {existingCube.CubeType}. Checking collision...");

            // Add a delay to allow the cube to initialize fully
            StartCoroutine(CheckCollisionAfterDelay(behavior, existingCube, markerPos));
        }

        debugObjects.Add(cube);
    }

    private IEnumerator CheckCollisionAfterDelay(CubeBehavior droppedCube, CubeBehavior existingCube, Vector2Int position)
    {
        // Wait a frame to ensure everything is initialized
        yield return null;

        // Get the collision controller
        CubeCollisionController controller = droppedCube.GetComponent<CubeCollisionController>();
        if (controller != null)
        {
            // Manually trigger collision
            Debug.Log($"Manually triggering collision between {droppedCube.CubeType} cube and {existingCube.CubeType} cube at {position}");
            controller.HandleCubeCollision(droppedCube, existingCube, position);
        }
        else
        {
            Debug.LogError("No CubeCollisionController found on dropped cube!");
        }
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
                    controller.Initialize(kvp.Key, grid);
                }

                // Trigger collision
                Debug.Log($"Forcing collision between {cube1.CubeType} and {cube2.CubeType}");
                controller.HandleCubeCollision(cube1, cube2, kvp.Key);
            }
        }
    }

    private void OnGUI()
    {
        if (!debugModeActive) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 600));
        GUILayout.Label("=== CUBE DEBUGGER ===", GUI.skin.box);

        Vector2Int markerPos = FindMarkedTile();
        if (markerPos.x >= 0)
        {
            GUILayout.Label($"Marker at: X={markerPos.x}, Z={markerPos.y}");
        }
        else
        {
            GUILayout.Label("No marker placed. Use Space to place marker.");
        }

        GUILayout.Space(10);

        // Cube type selectors
        GUILayout.Label("Falling Cube Type:");
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        fallingCubeType = (Enumerations.CubeType)GUILayout.SelectionGrid(
            (int)fallingCubeType, typeNames, typeNames.Length);

        GUILayout.Space(5);

        GUILayout.Label("Target Cube Type:");
        targetCubeType = (Enumerations.CubeType)GUILayout.SelectionGrid(
            (int)targetCubeType, typeNames, typeNames.Length);

        GUILayout.Space(10);

        // Action buttons
        if (GUILayout.Button($"Place Target Cube (F3)"))
            PlaceTargetCubeOnMarker();

        if (GUILayout.Button($"Drop Falling Cube (F4)"))
            DropFallingCubeOnMarker();

        GUILayout.Space(5);

        if (GUILayout.Button($"Clear Debug Objects (F2)"))
            ClearDebugObjects();

        if (debugModeActive)
        {
            // Add at the end
            if (GUILayout.Button("Force Check All Collisions"))
            {
                CheckAllOverlappingCubes();
            }
        }
        GUILayout.Label("Multi-Target Debug:", GUI.skin.box);
        enableMultiTarget = GUILayout.Toggle(enableMultiTarget, "Enable Multi-Target");

        if (enableMultiTarget)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grid Size: ");
            if (GUILayout.Button("-"))
            {
                multiGridSize = new Vector2Int(Mathf.Max(1, multiGridSize.x - 1),
                                              Mathf.Max(1, multiGridSize.y - 1));
            }
            GUILayout.Label($"{multiGridSize.x}x{multiGridSize.y}");
            if (GUILayout.Button("+"))
            {
                multiGridSize = new Vector2Int(Mathf.Min(5, multiGridSize.x + 1),
                                              Mathf.Min(5, multiGridSize.y + 1));
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button($"Spawn Multiple Targets (F6)"))
                SpawnMultipleTargets();

            if (GUILayout.Button($"Mark Entire Row (F7)"))
                MarkRowOfTiles();
        }

        GUILayout.Space(10);
        GUILayout.EndArea();
    }
}