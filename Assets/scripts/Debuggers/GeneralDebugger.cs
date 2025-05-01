using UnityEngine;
using System.Collections.Generic;

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

    // Cube testing state 
    private Enumerations.CubeType fallingCubeType = Enumerations.CubeType.Black;
    private Enumerations.CubeType targetCubeType = Enumerations.CubeType.Black;
    private bool debugModeActive = false;
    private List<GameObject> debugObjects = new List<GameObject>();

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
        }

        // Use the existing RainCubeController
        RainCubeController controller = cube.AddComponent<RainCubeController>();
        controller.Initialize(markerPos, grid);

        debugObjects.Add(cube);
    }

    private void OnGUI()
    {
        if (!debugModeActive) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 300));
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

        GUILayout.EndArea();
    }
}