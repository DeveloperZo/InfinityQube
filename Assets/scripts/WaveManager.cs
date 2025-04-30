using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GridManager grid;
    public GameObject[] cubePrefabs;
   
    public PlayerController player;

    public int waveSize = 3;
    public float cubeMoveInterval = 0.25f;

    private List<CubeBehavior> activeCubes = new();
    private bool waveActive = false;

    void Update()
    {
        if (!waveActive && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(RunWave());
        }
    }

    IEnumerator RunWave()
    {
        waveActive = true;
        player.enabled = waveActive;

        // Reset all tile colors to their original state
        foreach (var tile in grid.tiles)
        {
            if (tile != null)
            {
                tile.ClearMarker(); // Ensures markers are cleared
            }
        }

        SpawnCubes();

        yield return new WaitForSeconds(0.75f);

        bool cubesRemaining = true;
        while (cubesRemaining)
        {
            cubesRemaining = false;

            for (int i = activeCubes.Count - 1; i >= 0; i--)
            {
                if (activeCubes[i] != null)
                {
                    bool stillAlive = activeCubes[i].MoveForward();
                    if (!stillAlive)
                        activeCubes.RemoveAt(i);
                    else
                        cubesRemaining = true;
                }
            }

            yield return new WaitForSeconds(cubeMoveInterval);
        }

        grid.ClearAllMarkers();
        player.ResetMarkers();
      
        waveActive = false;
        player.enabled = waveActive;
    }

    void SpawnCubes()
    {
        activeCubes.Clear();

        int[] spawnZs = { grid.height - 1, grid.height - 2, grid.height - 3 }; // Three full rows from back

        foreach (int z in spawnZs)
        {
            for (int x = 0; x < grid.width; x++)
            {
                Vector2Int pos = new(x, z);
                Vector3 spawnPos = new Vector3(x, 1f, z);

                Enumerations.CubeType cubeType = GetRandomCubeType();
                GameObject cube = Instantiate(cubePrefabs[(int)cubeType], spawnPos, Quaternion.identity);
                var cb = cube.GetComponent<CubeBehavior>();
                cb.Init(grid, pos, 1); // level 1 for all
                activeCubes.Add(cb);
            }
        }
    }

    private Enumerations.CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < 0.7f) return Enumerations.CubeType.Normal;     // 70% chance
        else if (random < 0.9f) return Enumerations.CubeType.Green; // 20% chance
        else return Enumerations.CubeType.Black;                    // 10% chance
    }

}

