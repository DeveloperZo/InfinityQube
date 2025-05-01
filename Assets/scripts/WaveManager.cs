using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameObject[] cubePrefabs;
    [SerializeField] private PlayerController player;

    [Header("Wave Settings")]
    [SerializeField] private int waveSize = 3;
    [SerializeField] private float waveStartDelay = 0.75f;

    [Header("Cube Type Chances")]
    [SerializeField][Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField][Range(0f, 1f)] private float greenCubeChance = 0.2f;
    // Black cubes make up the remainder

    [Header("Speed Controls")]
    [SerializeField] private float normalMoveInterval = 0.75f; // Default speed
    [SerializeField] private float fastMoveInterval = 0.1f;
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private GameObject returnIndicatorPrefab;

    public bool isSpeedingUp = false;
    public List<CubeBehavior> activeCubes = new List<CubeBehavior>();
    private List<int> escapedBlackCubePositions = new List<int>();
    private bool waveActive = false;
    private Coroutine waveCoroutine;
    private List<ReturnQueueItem> returnQueue = new List<ReturnQueueItem>();
    private class ReturnQueueItem
    {
        public Enumerations.CubeType cubeType;
        public int xPosition;
    }

    public void SetSpeedState(bool isSpeeding)
    {
        isSpeedingUp = isSpeeding;
    }

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (grid == null)
        {
            grid = FindObjectOfType<GridManager>();
            if (grid == null)
            {
                Debug.LogError("WaveManager requires a GridManager reference!");
                enabled = false;
                return;
            }
        }

        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("PlayerController reference not set in WaveManager!");
            }
        }

        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            Debug.LogError("WaveManager requires at least 3 cube prefabs (Normal, Green, Black)!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!waveActive && Input.GetKeyDown(KeyCode.Return))
        {
            StartWave();
        }

        if (showDebugInfo && Input.GetKeyDown(KeyCode.L))
        {
            DebugActiveCubes();
        }
    }

    private void StartWave()
    {
        if (waveActive) return;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        
        waveCoroutine = StartCoroutine(RunWave());
        UpdateReturnVisuals();
    }

    private IEnumerator RunWave()
    {
        waveActive = true;

        // Toggle player input
        if (player != null)
        {
            player.enabled = true;
        }

        // Reset all tile markers
        if (grid != null)
        {
            grid.ClearAllMarkers();
        }

        // Spawn the cubes
        ProcessReturnQueue();
        SpawnCubes();

        yield return new WaitForSeconds(waveStartDelay);

        // Run the wave until all cubes are resolved
        bool cubesRemaining = true;
        while (cubesRemaining)
        {
            cubesRemaining = false;

            if (showDebugInfo)
            {
                Debug.Log($"--- Wave movement cycle: {activeCubes.Count} active cubes ---");
            }

            for (int i = activeCubes.Count - 1; i >= 0; i--)
            {
                if (i >= activeCubes.Count) continue; // Safety check for if list size changes during iteration

                CubeBehavior cube = activeCubes[i];
                if (cube != null)
                {
                    // Explicitly reset movement state to ensure cubes can move
                    cube.ResetMovementState();

                    if (showDebugInfo)
                    {
                        Debug.Log($"Moving cube at ({cube.position.x}, {cube.position.y}) of type {cube.CubeType}");
                    }

                    bool stillAlive = cube.MoveForward();
                    if (!stillAlive)
                    {
                        activeCubes.RemoveAt(i);
                        if (showDebugInfo) Debug.Log("Cube was removed from active list");
                    }
                    else
                    {
                        cubesRemaining = true;
                        if (showDebugInfo) Debug.Log($"Cube now at ({cube.position.x}, {cube.position.y})");
                    }
                }
                else
                {
                    // Remove null references
                    activeCubes.RemoveAt(i);
                    if (showDebugInfo) Debug.Log("Removed null cube reference from active list");
                }
            }

            // Use the appropriate move interval based on speed up state
            float currentMoveInterval = isSpeedingUp ? fastMoveInterval : normalMoveInterval;
            yield return new WaitForSeconds(currentMoveInterval);
        }

        // Wave is complete, reset state
        if (grid != null)
        {
            grid.ClearAllMarkers();
        }

        waveActive = false;
        waveCoroutine = null;
    }

    public void RegisterEscapedBlackCube(int xPosition)
    {
        escapedBlackCubePositions.Add(xPosition);
    }

    // Add this new method for rain cubes to register with the wave system
    public void RegisterRainCube(CubeBehavior cube)
    {
        if (cube == null) return;

        // Ensure it's not already in the list
        if (!activeCubes.Contains(cube))
        {
            activeCubes.Add(cube);
            Debug.Log($"Rain cube registered at ({cube.position.x}, {cube.position.y}) of type {cube.CubeType}");
        }
    }

    public void RegisterEscapedCube(CubeBehavior cube)
    {
        if (cube.CubeType != Enumerations.CubeType.Normal)
        {
            returnQueue.Add(new ReturnQueueItem
            {
                cubeType = cube.CubeType,
                xPosition = cube.position.x
            });

            // Log for debugging
            Debug.Log($"{cube.CubeType} cube escaped at X={cube.position.x}, queued for return");
        }
    }

    private void ProcessReturnQueue()
    {
        // Process all queued cubes before starting a new wave
        foreach (ReturnQueueItem item in returnQueue)
        {
            SpawnReturningCube(item.cubeType, item.xPosition);
        }

        returnQueue.Clear();
    }

    private void SpawnReturningCube(Enumerations.CubeType cubeType, int xPosition)
    {
        int prefabIndex = (int)cubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length) return;

        GameObject cube = Instantiate(cubePrefabs[prefabIndex],
                                      new Vector3(xPosition, 5f, grid.Height),
                                      Quaternion.identity);

        // Use your existing RainCubeController
        RainCubeController controller = cube.AddComponent<RainCubeController>();
        controller.Initialize(xPosition, grid);
    }

    private void SpawnCubes()
    {
        activeCubes.Clear();
        player.ResetMarkers();

        // Guard against missing grid
        if (grid == null) return;
        List<int> spawnZs = new List<int>();
        for (var i = 1; i <= waveSize; i++)
        {
            spawnZs.Add(grid.height - i);
        }

        foreach (int z in spawnZs)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3 spawnPos = new Vector3(x, 1f, z);

                Enumerations.CubeType cubeType = GetRandomCubeType();

                // Guard against index out of bounds
                int prefabIndex = (int)cubeType;
                if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
                {
                    Debug.LogWarning($"Missing cube prefab for type {cubeType}");
                    continue;
                }

                GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);
                if (cube != null)
                {
                    CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                    if (cb == null)
                    {
                        cb = cube.AddComponent<CubeBehavior>();
                        cb.CubeType = cubeType; // Set type since it wasn't in prefab
                    }

                    cb.Init(grid, pos, 1); // level 1 for all cubes in this version
                    activeCubes.Add(cb);
                }
            }
        }

        // Now spawn any escaped black cubes that need to "rain down"
        SpawnRainingBlackCubes();
    }

    private void SpawnRainingBlackCubes()
    {
        if (escapedBlackCubePositions.Count == 0) return;

        // Get black cube prefab index
        int blackCubeIndex = (int)Enumerations.CubeType.Black;
        if (blackCubeIndex < 0 || blackCubeIndex >= cubePrefabs.Length || cubePrefabs[blackCubeIndex] == null)
        {
            Debug.LogWarning("Missing black cube prefab for raining mechanism");
            escapedBlackCubePositions.Clear();
            return;
        }

        // For each escaped black cube
        foreach (int x in escapedBlackCubePositions)
        {
            // Position is directly above the grid at the same X position
            // The Y is higher to create a raining effect
            Vector3 spawnPos = new Vector3(x, 5f, grid.Height - 1);

            GameObject cube = Instantiate(cubePrefabs[blackCubeIndex], spawnPos, Quaternion.identity);
            if (cube != null)
            {
                CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                if (cb == null)
                {
                    cb = cube.AddComponent<CubeBehavior>();
                    cb.CubeType = Enumerations.CubeType.Black;
                }

                // Add a rain controller component to handle the specialized behavior
                RainCubeController rainController = cube.AddComponent<RainCubeController>();
                rainController.Initialize(x, grid);

                // Don't add to active cubes - the rain controller will handle movement
            }
        }

        // Clear the list after spawning
        escapedBlackCubePositions.Clear();
    }

    private Enumerations.CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < normalCubeChance)
            return Enumerations.CubeType.Normal;
        else if (random < normalCubeChance + greenCubeChance)
            return Enumerations.CubeType.Green;
        else
            return Enumerations.CubeType.Black;
    }

    private void DebugActiveCubes()
    {
        Debug.Log($"==== Active Cubes: {activeCubes.Count} ====");
        for (int i = 0; i < activeCubes.Count; i++)
        {
            CubeBehavior cube = activeCubes[i];
            if (cube != null)
            {
                Debug.Log($"[{i}] Cube at ({cube.position.x}, {cube.position.y}) of type {cube.CubeType}");
            }
            else
            {
                Debug.Log($"[{i}] NULL CUBE REFERENCE");
            }
        }
    }

    private void UpdateReturnVisuals()
    {
        // Clear old indicators
        GameObject[] oldIndicators = GameObject.FindGameObjectsWithTag("ReturnIndicator");
        foreach (GameObject indicator in oldIndicators)
        {
            Destroy(indicator);
        }

        // Create new indicators
        foreach (ReturnQueueItem item in returnQueue)
        {
            Vector3 indicatorPos = new Vector3(item.xPosition, 6f, grid.Height);
            GameObject indicator = Instantiate(returnIndicatorPrefab, indicatorPos, Quaternion.identity);
            indicator.tag = "ReturnIndicator";

            // Set color based on type
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                switch (item.cubeType)
                {
                    case Enumerations.CubeType.Black:
                        renderer.material.color = Color.black;
                        break;
                    case Enumerations.CubeType.Green:
                        renderer.material.color = Color.green;
                        break;
                }
            }
        }
    }

    // Called when the game is being shut down or scene is changing
    private void OnDestroy()
    {
        // Clean up any active coroutines
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // Clean up any remaining cubes
        foreach (var cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }

        activeCubes.Clear();
    }
}