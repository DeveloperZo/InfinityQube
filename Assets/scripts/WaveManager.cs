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
    [SerializeField] private float cubeMoveInterval = 0.25f;
    [SerializeField] private float waveStartDelay = 0.75f;
    
    [Header("Cube Type Chances")]
    [SerializeField] [Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float greenCubeChance = 0.2f;
    // Black cubes make up the remainder

    private List<CubeBehavior> activeCubes = new List<CubeBehavior>();
    private bool waveActive = false;
    private Coroutine waveCoroutine;

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

            yield return new WaitForSeconds(cubeMoveInterval);
        }

        // Wave is complete, reset state
        if (grid != null) 
        {
            grid.ClearAllMarkers();
        }
        
        if (player != null)
        {
            player.ResetMarkers();
            player.enabled = false;
        }
        
        waveActive = false;
        waveCoroutine = null;
    }

    private void SpawnCubes()
    {
        activeCubes.Clear();

        // Guard against missing grid
        if (grid == null) return;

        int[] spawnZs = { grid.Height - 1, grid.Height - 2, grid.Height - 3 }; // Three rows from back

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