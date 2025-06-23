using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Shared utility class for cube spawning operations in debug panels.
/// Eliminates code duplication and ensures consistent cube spawning behavior.
/// </summary>
public static class DebugCubeSpawnHelper
{
    #region Cube Spawning

    /// <summary>
    /// Spawns a cube at the specified grid position with proper validation and initialization.
    /// </summary>
    /// <param name="position">Grid position to spawn the cube</param>
    /// <param name="cubeType">Type of cube to spawn</param>
    /// <param name="gridManager">GridManager instance for position validation</param>
    /// <param name="waveManager">WaveManager instance for prefabs and cube tracking</param>
    /// <returns>True if cube was successfully spawned, false otherwise</returns>
    public static bool SpawnCubeAt(Vector2Int position, CubeType cubeType, GridManager gridManager, WaveManager waveManager)
    {
        // Validation checks
        if (gridManager == null)
        {
            Debug.LogError("DebugCubeSpawnHelper: GridManager is null");
            return false;
        }

        if (waveManager?.cubePrefabs == null)
        {
            Debug.LogError("DebugCubeSpawnHelper: WaveManager or cubePrefabs is null");
            return false;
        }

        if ((int)cubeType >= waveManager.cubePrefabs.Length)
        {
            Debug.LogError($"DebugCubeSpawnHelper: Invalid cube type {cubeType} - prefab not available");
            return false;
        }

        if (!gridManager.IsValidGridPosition(position))
        {
            Debug.LogWarning($"DebugCubeSpawnHelper: Invalid grid position ({position.x}, {position.y})");
            return false;
        }

        // Spawn the cube
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) 
        {
            cube = cubeObj.AddComponent<CubeManager>();
        }

        // Initialize cube data
        var cubeData = new CubeData 
        { 
            type = cubeType, 
            position = position, 
            level = 1 
        };

        cube.Init(gridManager, cubeData, 2f);
        waveManager.activeCubes.Add(cube);

        Debug.Log($"DebugCubeSpawnHelper: Spawned {cubeType} cube at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Spawns multiple cubes in a line pattern.
    /// </summary>
    /// <param name="startPosition">Starting position for the line</param>
    /// <param name="cubeType">Type of cubes to spawn</param>
    /// <param name="count">Number of cubes to spawn</param>
    /// <param name="direction">Direction vector for the line (normalized)</param>
    /// <param name="gridManager">GridManager instance</param>
    /// <param name="waveManager">WaveManager instance</param>
    /// <returns>Number of cubes successfully spawned</returns>
    public static int SpawnCubeLinePattern(Vector2Int startPosition, CubeType cubeType, int count, Vector2Int direction, GridManager gridManager, WaveManager waveManager)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2Int spawnPos = startPosition + direction * i;
            if (SpawnCubeAt(spawnPos, cubeType, gridManager, waveManager))
            {
                spawned++;
            }
        }
        return spawned;
    }

    #endregion

    #region Cube Finding and Selection

    /// <summary>
    /// Finds all cubes at the specified grid position.
    /// </summary>
    /// <param name="position">Grid position to search</param>
    /// <returns>List of cubes at the position</returns>
    public static List<CubeManager> FindCubesAt(Vector2Int position)
    {
        List<CubeManager> cubes = new List<CubeManager>();
        
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        foreach (CubeManager cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                cubes.Add(cube);
            }
        }
        
        return cubes;
    }

    /// <summary>
    /// Gets all active cubes in the scene, sorted by position.
    /// </summary>
    /// <returns>List of active cubes sorted by Y then X position</returns>
    public static List<CubeManager> GetAllActiveCubes()
    {
        return Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed)
            .OrderBy(c => c.position.y)
            .ThenBy(c => c.position.x)
            .ToList();
    }

    /// <summary>
    /// Gets all cubes of a specific type.
    /// </summary>
    /// <param name="cubeType">Type of cubes to find</param>
    /// <returns>List of cubes of the specified type</returns>
    public static List<CubeManager> GetCubesOfType(CubeType cubeType)
    {
        return Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed && c.type == cubeType)
            .OrderBy(c => c.position.y)
            .ThenBy(c => c.position.x)
            .ToList();
    }

    #endregion

    #region Cube Validation and Utilities

    /// <summary>
    /// Checks if a cube can be spawned at the specified position (no overlapping cubes).
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="gridManager">GridManager for validation</param>
    /// <returns>True if position is clear for spawning</returns>
    public static bool CanSpawnCubeAt(Vector2Int position, GridManager gridManager)
    {
        if (gridManager == null || !gridManager.IsValidGridPosition(position))
        {
            return false;
        }

        var cubesAtPosition = FindCubesAt(position);
        return cubesAtPosition.Count == 0;
    }

    /// <summary>
    /// Gets a color for UI display based on cube type.
    /// </summary>
    /// <param name="type">Cube type</param>
    /// <returns>Color for UI display</returns>
    public static Color GetCubeDisplayColor(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: return new Color(0.8f, 0.8f, 0.8f);
            case CubeType.Prime: return new Color(0.3f, 0.6f, 1f);
            case CubeType.Infinity: return new Color(0.3f, 0.3f, 0.3f);
            case CubeType.Dense: return new Color(0.8f, 0.4f, 0.8f);
            default: return Color.white;
        }
    }

    #endregion
}
