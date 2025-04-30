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

    [Header("Test Parameters")]
    [SerializeField] private int spawnColumn = 3;
    [SerializeField] private bool showDebugVisualization = true;

    private Coroutine autoSpawnCoroutine;
    private bool debugMode = false;
    private List<GameObject> spawnedTestCubes = new List<GameObject>(); // Properly initialized

    private void Start()
    {
        // Initialize the list
        spawnedTestCubes = new List<GameObject>();

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

    // In RainCubeDebugger.cs, update the SpawnTestRainCube method
    private void SpawnTestRainCube()
    {
        // Add a small random offset to prevent perfect stacking of debugging cubes
        float randomOffsetX = Random.Range(-0.05f, 0.05f);
        float randomOffsetZ = Random.Range(-0.05f, 0.05f);
        Vector3 spawnPos = new Vector3(
            spawnColumn + randomOffsetX,
            hoverHeight,
            grid.Height - 1 + randomOffsetZ
        );

        // Instantiate the black cube
        GameObject cube = Instantiate(blackCubePrefab, spawnPos, Quaternion.identity);
        if (cube != null)
        {
            // Add a unique name for easier debugging
            string uniqueId = System.DateTime.Now.Ticks.ToString();
            cube.name = $"TestRainCube_{spawnColumn}_{uniqueId}";

            Debug.Log($"Test Rain Cube {uniqueId} spawned at column {spawnColumn}, height {hoverHeight}");

            // Add to our tracking list
            spawnedTestCubes.Add(cube);

            // Attach the real rain controller
            TestRainCubeController controller = cube.AddComponent<TestRainCubeController>();
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

// Simplified test controller specifically for debugging
public class TestRainCubeController : MonoBehaviour
{
    private int targetX;
    private GridManager grid;
    private float hoverHeight;
    private float fallSpeed = 4f;
    private bool hasLanded = false;
    private bool isTracking = true;
    private string uniqueId; // Add unique ID for each controller

    public void Initialize(int x, GridManager gridManager, float height)
    {
        targetX = x;
        grid = gridManager;
        hoverHeight = height;
        uniqueId = System.Guid.NewGuid().ToString(); // Generate unique ID

        // Start hovering
        StartCoroutine(HoverAndTrack());
    }

    private IEnumerator HoverAndTrack()
    {
        // Set initial position
        transform.position = new Vector3(targetX, hoverHeight, grid.Height - 1);

        // Visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.localScale = Vector3.one * 0.3f;
        indicator.transform.position = transform.position;
        indicator.GetComponent<Renderer>().material.color = Color.red;
        Destroy(indicator.GetComponent<Collider>());
        indicator.transform.SetParent(transform);

        // Add slight random offset to position to avoid perfect overlap with other rain cubes
        float randomOffset = Random.Range(-0.1f, 0.1f);
        Vector3 basePosition = transform.position + new Vector3(randomOffset, 0, randomOffset);

        while (isTracking)
        {
            // Simple hover animation
            float hoverOffset = Mathf.Sin(Time.time * 2 + Random.Range(0f, 6.28f)) * 0.1f;
            transform.position = basePosition + Vector3.up * hoverOffset;

            // Check for target cubes
            CubeBehavior targetCube = FindTargetBelow();
            if (targetCube != null)
            {
                Debug.Log($"Rain cube {uniqueId}: Target cube found at ({targetCube.position.x}, {targetCube.position.y})");
                Destroy(indicator);
                yield return StartCoroutine(FallOnto(targetCube));
                break;
            }

            yield return null;
        }
    }

    private CubeBehavior FindTargetBelow()
    {
        // Cast a ray down to find cubes
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, 10f);

        // Sort hits by distance
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            CubeBehavior cube = hit.collider.GetComponent<CubeBehavior>();
            if (cube != null && cube.gameObject != gameObject)
            {
                // Only target cubes, not grid tiles
                if (hit.collider.gameObject.CompareTag("Tile"))
                {
                    continue;
                }
                return cube;
            }
        }

        return null;
    }

    private IEnumerator FallOnto(CubeBehavior targetCube)
    {
        if (targetCube == null) yield break;

        isTracking = false;

        Debug.Log($"Rain cube {uniqueId}: Starting fall animation");

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetCube.transform.position + Vector3.up * 0.1f;

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / fallSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (targetCube == null)
            {
                Debug.Log($"Rain cube {uniqueId}: Target cube destroyed during fall");
                Destroy(gameObject);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Quadratic easing for acceleration
            float eased = t * t;
            transform.position = Vector3.Lerp(startPos, targetPos, eased);

            yield return null;
        }

        if (targetCube != null)
        {
            Debug.Log($"Rain cube {uniqueId}: Landing on target cube");
            yield return StartCoroutine(LandOnCube(targetCube));
        }
    }

    private IEnumerator LandOnCube(CubeBehavior targetCube)
    {
        // Flash effect
        Renderer renderer = targetCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color original = renderer.material.color;
            renderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = original;
        }

        // Squash effect
        Vector3 originalScale = targetCube.transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            targetCube.transform.localScale = Vector3.Lerp(
                originalScale,
                new Vector3(originalScale.x * 1.5f, originalScale.y * 0.1f, originalScale.z * 1.5f),
                t
            );

            yield return null;
        }

        // Replace target cube
        Vector2Int position = targetCube.position;
        Destroy(targetCube.gameObject);

        // Take its place in the grid
        transform.position = new Vector3(position.x, 1f, position.y);

        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            thisCube.Init(grid, position, 1);

            // Add to wave manager's active cubes - you'll need a proper way to do this
            // in your actual implementation
            Debug.Log($"Rain cube {uniqueId}: Now in grid at position ({position.x}, {position.y})");
        }

        // Remove this controller
        Destroy(this);
    }

}