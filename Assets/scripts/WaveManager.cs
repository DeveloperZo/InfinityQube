using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static StageData;

public class WaveManager : MonoBehaviour
{
    private class BlackCubeRainData
    {
        public Vector2Int targetPosition;
        public int countdown = 3; // Number of moves before landing
        public GameObject indicator;
    }
    private class ReturnQueueItem
    {
        public Enumerations.CubeType cubeType;
        public Vector2 position;
    }

    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] public GameObject[] cubePrefabs;
    [SerializeField] public bool useWaveConfiguration = false;
    [SerializeField] private PlayerController player;
    [SerializeField] private DetonationManager detonationManager;
    [SerializeField] public List<WaveConfiguration> waveConfiguration = new List<WaveConfiguration>();

    [Serialize] private CubeData cubeData;

    [Header("Wave Settings")]
    [SerializeField] public int waveSize = 3;
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
    public List<Vector2> escapedBlackCubePositions = new List<Vector2>();
    public bool waveActive = false;
    private Coroutine waveCoroutine;
    private List<ReturnQueueItem> returnQueue = new List<ReturnQueueItem>();
    private List<BlackCubeRainData> rainingBlackCubes = new List<BlackCubeRainData>();
    private bool isDebugWaveActive = false;
    public bool debugMode = false;
    public bool manualControl = false;
    internal float[] cubeChances;

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

    public void StartWave()
    {
        if (waveActive) return;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        
        waveCoroutine = StartCoroutine(RunWave());
        UpdateReturnVisuals();
    }

    private void NotifyMovementComplete()
    {
        // Find and notify all AutoDetonationTag components
        DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
        if (detonationManager != null)
        {
            detonationManager.ProcessAutoDetonations();
        }
    }


    private IEnumerator RunWave()
    {
        waveActive = true;

        // Toggle player input
        if (player != null && !debugMode)
        {
            player.enabled = true;
            player.ResetMarkers(); // Only reset markers when not in debug mode
        }

        // Reset all tile markers only when not in debug mode
        if (grid != null && !debugMode)
        {
            grid.ClearAllMarkers();
        }

        
         SpawnCubes();
        

        yield return new WaitForSeconds(waveStartDelay);

        // Skip automatic wave progression if we're in manual control mode
        if (debugMode && manualControl)
        {
            // Just wait indefinitely until manual control is disabled
            while (debugMode && manualControl)
            {
                yield return null;
            }

            waveActive = false;
            waveCoroutine = null;
            yield break;
        }

        // Normal automatic wave progression
        bool cubesRemaining = true;
        while (cubesRemaining)
        {
            cubesRemaining = false;

            for (int i = activeCubes.Count - 1; i >= 0; i--)
            {
                if (i >= activeCubes.Count) continue; // Safety check for if list size changes

                CubeBehavior cube = activeCubes[i];
                if (cube != null)
                {
                    cube.ResetMovementState();
                    bool stillAlive = cube.MoveForward();

                    if (!stillAlive)
                    {
                        activeCubes.RemoveAt(i);
                    }
                    else
                    {
                        cubesRemaining = true;
                    }
                }
                else
                {
                    // Remove null references
                    activeCubes.RemoveAt(i);
                }
            }

            // Notify that a movement cycle is complete
            NotifyMovementComplete();

            cubesRemaining = !debugMode;

            // Use appropriate move interval based on speed up state
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

    public void RegisterDebugWave(List<GameObject> debugCubes)
    {
        // Clear existing active cubes first
        activeCubes.Clear();

        // Convert GameObject references to CubeBehavior references
        foreach (GameObject obj in debugCubes)
        {
            if (obj != null)
            {
                CubeBehavior cube = obj.GetComponent<CubeBehavior>();
                if (cube != null)
                {
                    activeCubes.Add(cube);
                }
            }
        }

        // Disable automatic wave spawning (if applicable)
        isDebugWaveActive = true;
    }

    public void ClearAllCubes()
    {
        // Clear active cubes
        foreach (CubeBehavior cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }
        activeCubes.Clear();

        // Reset other wave-related states
        isDebugWaveActive = false;
    }

    public void RegisterEscapedBlackCube(Vector2 position)
    {
        // Keep using your existing returnQueue but ensure it's a black cube
        returnQueue.Add(new ReturnQueueItem
        {
            cubeType = Enumerations.CubeType.Black,
            position = position
        });

        // Log for debugging
        Debug.Log($"Black cube escaped at X={position.x}, queued for return");
    }

    public void RegisterEscapedCube(CubeBehavior cube)
    {
        if (cube.type != Enumerations.CubeType.Normal)
        {
            returnQueue.Add(new ReturnQueueItem
            {
                cubeType = cube.type,
                position = cube.position
            });

            // Log for debugging
            Debug.Log($"{cube.type} cube escaped at X={cube.position.x}, queued for return");
        }
    }


    public void EnterDebugMode(bool manual)
    {
        debugMode = true;
        manualControl = manual;
        // Reset any ongoing waves
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        waveActive = false;
    }

    public void ExitDebugMode()
    {
        debugMode = false;
        manualControl = false;
    }

    public void RegisterCube(CubeBehavior cube)
    {
        if (!activeCubes.Contains(cube))
        {
            activeCubes.Add(cube);
        }
    }

    public void ManualMoveWaveForward()
    {
        if (!debugMode) return;

        // Don't reset player markers when in debug mode
        bool resetMarkers = !debugMode;

        // Process one movement step for all active cubes
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (i >= activeCubes.Count) continue; // Safety check

            CubeBehavior cube = activeCubes[i];
            if (cube != null)
            {
                cube.ResetMovementState();
                bool stillAlive = cube.MoveForward();

                if (!stillAlive)
                {
                    activeCubes.RemoveAt(i);
                }
            }
            else
            {
                // Remove null references
                activeCubes.RemoveAt(i);
            }
        }

        // Notify that a movement cycle is complete
        NotifyMovementComplete();
    }

    public void SpawnCustomWave(List<WaveDebugger.WaveData> waveData, bool useDebugMode)
    {
        // Clear existing cubes
        ClearAllCubes();
        waveConfiguration.Clear();
        activeCubes.Clear();

        // Set debug mode
        debugMode = useDebugMode;
        manualControl = useDebugMode;
        // Process wave data and spawn cubes
        foreach (var data in waveData)
        {
            waveConfiguration.Add(new WaveConfiguration
            {
                Index = data.waveIndex,
                CubesData = data.cubesData
            }); ;
        }

        // Start wave if not in debug mode
        StartCoroutine(RunWave());

    }

    private void SpawnCubes()
    {
        activeCubes.Clear();
        player.ResetMarkers();

        // Guard against missing grid
        if (grid == null) return;

        if (useWaveConfiguration && waveConfiguration.Count > 0)
        {
            GenerateConfigurationWave();
        }
        else
            GenerateRandomWave();

        // Now spawn any escaped black cubes that need to "rain down"
        SpawnRainingBlackCubes();
    }

    private void GenerateRandomWave()
    {
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

                cubeData.position = pos;
                cubeData.type = cubeType;


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
                        cb.type = cubeType; // Set type since it wasn't in prefab
                    }

                    cb.Init(grid, cubeData, 1); // level 1 for all cubes in this version
                    activeCubes.Add(cb);
                }
            }
        }
    }

    private void GenerateConfigurationWave()
    {
        foreach (var spawnData in waveConfiguration)
        {

            var cubes = spawnData.CubesData;

            foreach (var item in cubes)
            {
                Vector2Int pos = item.position;
                item.position = new Vector2Int(pos.x, 0 - (pos.y - grid.Height));

                Vector3 spawnPos = new Vector3(pos.x, 1f, 0 - (pos.y - grid.Height));

                // Guard against index out of bounds
                int prefabIndex = (int)item.type;
                if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
                {
                    Debug.LogWarning($"Missing cube prefab for type {item.type}");
                    continue;
                }

                GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);
                if (cube != null)
                {
                    CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                    if (cb == null)
                    {
                        cb = cube.AddComponent<CubeBehavior>();
                        cb.type = item.type;
                    }

                    cb.Init(grid, item, 1);
                    activeCubes.Add(cb);
                }
            }

        }
    }

    public void SpawnRainingBlackCubes()
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
        foreach (Vector2 position in escapedBlackCubePositions)
        {
            // Position is directly above the grid at the same X position
            // The Y is higher to create a raining effect
            Vector3 spawnPos = new Vector3(position.x, 5f, position.y);

            GameObject cube = Instantiate(cubePrefabs[blackCubeIndex], spawnPos, Quaternion.identity);
            if (cube != null)
            {
                CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                if (cb == null)
                {
                    cb = cube.AddComponent<CubeBehavior>();
                    cb.type = Enumerations.CubeType.Black;
                    cb.isRainingCube = true;
                }

                activeCubes.Add(cb);

                // Add a rain controller component to handle the specialized behavior
                CubeCollisionController rainController = cube.AddComponent<CubeCollisionController>();
                rainController.Initialize(grid);

                // Don't add to active cubes - the rain controller will handle movement
            }
        }

        // Clear the list after spawning
        escapedBlackCubePositions.Clear();
    }

    private Enumerations.CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < cubeChances[0])
            return Enumerations.CubeType.Normal;
        else if (random < cubeChances[0] + cubeChances[1])
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
                Debug.Log($"[{i}] Cube at ({cube.position.x}, {cube.position.y}) of type {cube.type}");
            }
            else
            {
                Debug.Log($"[{i}] NULL CUBE REFERENCE");
            }
        }
    }

    // Add this new method for rain cubes to register with the wave system
    public void RegisterRainCube(CubeBehavior cube)
    {
        if (cube == null) return;

        // Ensure it's not already in the list
        if (!activeCubes.Contains(cube))
        {
            activeCubes.Add(cube);

            Debug.Log($"Rain cube registered: Type={cube.type}, " +
                      $"Grid Position=({cube.position.x}, {cube.position.y}), " +
                      $"World Position=({cube.transform.position.x}, {cube.transform.position.y}, {cube.transform.position.z}), " +
                      $"Moves Remaining={cube.moveCountRemaining}");
        }
    }

    public void CubeRainLanded(CubeBehavior cube)
    {
        if (cube == null) return;

        // The cube has completed its vertical falling animation
        // but it's still part of the wave system with moveCountRemaining

        // Update tile reference if needed
        Vector2Int pos = cube.position;
        Tile tile = null;
        if (grid != null && pos.x >= 0 && pos.x < grid.Width && pos.y >= 0 && pos.y < grid.Height)
        {
            tile = grid.tiles[pos.x, pos.y];
            if (tile != null)
            {
                // Only update the tile reference if this is the final landing
                // or if the tile doesn't have a cube yet
                if (cube.moveCountRemaining <= 0 || tile.currentCube == null)
                {
                    tile.ProcessCubeInteraction(cube);
                }
            }
        }

        // Check for collisions now that the cube has landed
        cube.CheckForCollisionOnLanding();
        if(tile != null)
        {
            if (tile.IsAdvantaged)
            {
                detonationManager.TriggerNextDetonation(tile.x, tile.y);
            }
        }

        Debug.Log($"Cube rain landed at ({pos.x}, {pos.y}), " +
                  $"world pos ({cube.transform.position.x}, {cube.transform.position.y}, {cube.transform.position.z}), " +
                  $"moves remaining: {cube.moveCountRemaining}");
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
            Vector3 indicatorPos = new Vector3(item.position.x, 6f, item.position.y);
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

    internal void ConfigureSpawn(List<CubeData> spawnData)
    {
        
    }
}