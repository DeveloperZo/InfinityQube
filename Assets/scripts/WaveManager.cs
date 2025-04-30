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
    [SerializeField] [Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float greenCubeChance = 0.2f;
    // Black cubes make up the remainder

    [Header("Speed Controls")]
    [SerializeField] private float normalMoveInterval = 0.75f; // Default speed
    [SerializeField] private float fastMoveInterval = 0.1f;
    public bool isSpeedingUp = false;
    private List<CubeBehavior> activeCubes = new List<CubeBehavior>();
    private List<int> escapedBlackCubePositions = new List<int>();
    private bool waveActive = false;
    private Coroutine waveCoroutine;



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
    }

    private void StartWave()
    {
        if (waveActive) return;
        
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        
        waveCoroutine = StartCoroutine(RunWave());
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
        SpawnCubes();

        yield return new WaitForSeconds(waveStartDelay);

        // Run the wave until all cubes are resolved
        bool cubesRemaining = true;
        while (cubesRemaining)
        {
            cubesRemaining = false;

            for (int i = activeCubes.Count - 1; i >= 0; i--)
            {
                if (i >= activeCubes.Count) continue; // Safety check for if list size changes during iteration
                
                CubeBehavior cube = activeCubes[i];
                if (cube != null)
                {
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
    
    private void SpawnCubes()
    {
        activeCubes.Clear();

        // Guard against missing grid
        if (grid == null) return;
        List<int> spawnZs = new List<int>();
        for (var i = 1; i <=waveSize; i++)
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
// In WaveManager.cs, modify the SpawnRainingBlackCubes method
// In WaveManager.cs
// In WaveManager.cs
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

    private IEnumerator AnimateRainingCube(CubeBehavior cube)
    {
        if (cube == null) yield break;
        
        // Dramatic fall animation
        float startHeight = 4f;
        float fallDuration = 0.8f;
        float elapsed = 0f;
        Vector3 targetPos = new Vector3(cube.position.x, 1f, cube.position.y);
        Vector3 startPos = new Vector3(cube.position.x, startHeight, cube.position.y);
        
        // Scale effect (starts slightly larger)
        cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        
        // Create a random rotation axis so each cube spins differently
        Vector3 rotationAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
        
        float rotationSpeed = Random.Range(180f, 360f); // degrees per second
        
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            
            // Easing function for dramatic fall (accelerating)
            float eased = t * t;
            
            // Calculate position with easing
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, eased);
            cube.transform.position = newPos;
            
            // Gradually scale back to normal
            cube.transform.localScale = Vector3.Lerp(
                new Vector3(1.2f, 1.2f, 1.2f),
                Vector3.one,
                eased
            );
            
            // Random rotation during fall
            cube.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        // Ensure final position/scale are exact
        cube.transform.position = targetPos;
        cube.transform.localScale = Vector3.one;
        cube.transform.rotation = Quaternion.identity; // Reset rotation at end
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