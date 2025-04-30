using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RainCubeDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject blackCubePrefab;
    [SerializeField] private GridManager grid;
    [SerializeField] private Transform platform;

    [Header("Debug Controls")]
    [SerializeField] private float hoverHeight = 6f; // Adjust this height
    [SerializeField] private bool autoSpawnTestCubes = false;
    [SerializeField] private float autoSpawnInterval = 3f;
    [SerializeField] private KeyCode spawnCubeKey = KeyCode.T;
    [SerializeField] private KeyCode toggleDebugModeKey = KeyCode.Y;
    [SerializeField] private KeyCode resetKey = KeyCode.R; // Reset key
    [SerializeField] private KeyCode multiDropKey = KeyCode.G;

    [Header("Test Parameters")]
    [SerializeField] private int spawnColumn = 3;
    [SerializeField] private bool showDebugVisualization = true;

    private Coroutine autoSpawnCoroutine;
    private bool debugMode = false;
    private List<GameObject> spawnedTestCubes = new List<GameObject>(); // Properly initialized

    private void Start()
    {
        // Initialize the list
        this.spawnedTestCubes = new List<GameObject>();

        if (grid == null)
            grid = FindObjectOfType<GridManager>();

        if (grid == null)
        {
            Debug.LogError("RainCubeDebugger requires a GridManager reference!");
            enabled = false;
            return;
        }

        if (blackCubePrefab == null)
        {
            // Try to find it by name
            blackCubePrefab = Resources.Load<GameObject>("CubePrefab_Black");
            if (blackCubePrefab == null)
            {
                Debug.LogError("RainCubeDebugger requires a BlackCube prefab reference!");
                enabled = false;
                return;
            }
        }
    }
    private void ResetDebugTest()
    {
        // Check if the list exists
        if (spawnedTestCubes == null)
        {
            spawnedTestCubes = new List<GameObject>();
            return;
        }

        // Clear all spawned test cubes
        foreach (GameObject cube in spawnedTestCubes)
        {
            if (cube != null)
            {
                Destroy(cube);
            }
        }

        // Clear the list
        spawnedTestCubes.Clear();

        // Stop auto-spawn if active
        if (autoSpawnCoroutine != null)
        {
            StopCoroutine(autoSpawnCoroutine);
            autoSpawnCoroutine = null;

            // Restart if needed
            if (autoSpawnTestCubes)
            {
                autoSpawnCoroutine = StartCoroutine(AutoSpawnCubes());
            }
        }

        Debug.Log("Debug test reset - all test objects cleared");
    }

    private void Update()
    {
        // Toggle debug mode
        if (Input.GetKeyDown(toggleDebugModeKey))
        {
            debugMode = !debugMode;

            if (debugMode)
            {
                Debug.Log("Rain Cube Debug Mode: ACTIVE");
                if (autoSpawnTestCubes && autoSpawnCoroutine == null)
                {
                    autoSpawnCoroutine = StartCoroutine(AutoSpawnCubes());
                }
            }
            else
            {
                Debug.Log("Rain Cube Debug Mode: OFF");
                if (autoSpawnCoroutine != null)
                {
                    StopCoroutine(autoSpawnCoroutine);
                    autoSpawnCoroutine = null;
                }
            }
        }

        // Manual test cube spawn
        if (debugMode && Input.GetKeyDown(spawnCubeKey))
        {
            SpawnTestRainCube();
        }

        if (showDebugVisualization && debugMode)
        {
            DrawDebugVisualization();
        }

        if(debugMode && Input.GetKeyDown(resetKey))
        {
            ResetDebugTest();
        }
        if (debugMode && Input.GetKeyDown(multiDropKey))
        {
            DropCubesInAllColumns();
        }
    }
    private void DropCubesInAllColumns()
    {
        for (int x = 0; x < grid.Width; x++)
        {
            Vector3 spawnPos = new Vector3(x, hoverHeight, grid.Height - 1);

            GameObject cube = Instantiate(blackCubePrefab, spawnPos, Quaternion.identity);
            if (cube != null)
            {
                cube.name = $"TestRainCube_{x}_{System.DateTime.Now.Ticks}";
                spawnedTestCubes.Add(cube);

                TestRainCubeDebugger controller = cube.AddComponent<TestRainCubeDebugger>();
                controller.Initialize(x, grid, hoverHeight);
            }
        }

        Debug.Log($"Dropped test cubes in all columns");
    }
    private void OnDestroy()
    {
        // Clean up any remaining test objects and ensure the list is properly handled
        if (spawnedTestCubes != null)
        {
            foreach (GameObject cube in spawnedTestCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
            spawnedTestCubes.Clear();
        }
    }

    private IEnumerator AutoSpawnCubes()
    {
        while (debugMode)
        {
            SpawnTestRainCube();
            yield return new WaitForSeconds(autoSpawnInterval);
        }
    }
    // Update the SpawnTestRainCube method in RainCubeDebugger.cs
    private void SpawnTestRainCube()
    {
        // Spawn at exactly the specified column and height
        Vector3 spawnPos = new Vector3(spawnColumn, hoverHeight, grid.Height - 1);
        
        // Instantiate the black cube
        GameObject cube = Instantiate(blackCubePrefab, spawnPos, Quaternion.identity);
        if (cube != null)
        {
            // Add a unique name for easier debugging
            string uniqueId = System.DateTime.Now.Ticks.ToString();
            cube.name = $"TestRainCube_{spawnColumn}_{uniqueId}";
            
            Debug.Log($"Test Rain Cube spawned at column {spawnColumn}, height {hoverHeight}");
            
            // Add to our tracking list
            spawnedTestCubes.Add(cube);
            
            // Attach the controller that immediately falls
            TestRainCubeDebugger controller = cube.AddComponent<TestRainCubeDebugger>();
            controller.Initialize(spawnColumn, grid, hoverHeight);
        }
    }

    private void DrawDebugVisualization()
    {
        // Draw a line down from the spawn position
        Vector3 start = new Vector3(spawnColumn, hoverHeight, grid.Height - 1);
        Vector3 end = new Vector3(spawnColumn, 0, grid.Height - 1);
        Debug.DrawLine(start, end, Color.red);

        // Draw a sphere at the spawn point
        Debug.DrawLine(start + Vector3.left * 0.2f, start + Vector3.right * 0.2f, Color.yellow);
        Debug.DrawLine(start + Vector3.forward * 0.2f, start + Vector3.back * 0.2f, Color.yellow);
        Debug.DrawLine(start + Vector3.up * 0.2f, start + Vector3.down * 0.2f, Color.yellow);
    }


    private void OnGUI()
    {
        if (!debugMode) return;

        // Simple debug UI
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("--- Rain Cube Debugger ---");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Hover Height: {hoverHeight}");
        if (GUILayout.Button("+"))
            hoverHeight += 0.5f;
        if (GUILayout.Button("-"))
            hoverHeight -= 0.5f;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Spawn Column: {spawnColumn}");
        if (GUILayout.Button("+"))
            spawnColumn = Mathf.Min(grid.Width - 1, spawnColumn + 1);
        if (GUILayout.Button("-"))
            spawnColumn = Mathf.Max(0, spawnColumn - 1);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Spawn Test Cube"))
            SpawnTestRainCube();

        autoSpawnTestCubes = GUILayout.Toggle(autoSpawnTestCubes, "Auto Spawn Cubes");

        GUILayout.EndArea();
    }
}
