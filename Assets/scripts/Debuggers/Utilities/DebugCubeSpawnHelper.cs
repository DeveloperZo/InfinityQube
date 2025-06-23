using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Shared utility for cube spawning operations across debug panels.
/// Provides consistent cube spawning, finding, and manipulation methods.
/// </summary>
public static class DebugCubeSpawnHelper
{
    #region Core Spawning Methods

    /// <summary>
    /// Spawns a cube at the specified grid position
    /// </summary>
    public static bool SpawnCubeAt(Vector2Int gridPosition, CubeType cubeType, GridManager gridManager, WaveManager waveManager)
    {
        if (gridManager == null || waveManager == null)
        {
            Debug.LogWarning("DebugCubeSpawnHelper: Missing required managers for cube spawning");
            return false;
        }

        if (!gridManager.IsValidGridPosition(gridPosition))
        {
            Debug.LogWarning($"DebugCubeSpawnHelper: Invalid grid position ({gridPosition.x}, {gridPosition.y})");
            return false;
        }

        if (waveManager.cubePrefabs == null || (int)cubeType >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"DebugCubeSpawnHelper: Cube prefab for type {cubeType} not available");
            return false;
        }

        try
        {
            // Convert grid position to world position
            Vector3 worldPosition = gridManager.GridToWorldPosition(gridPosition.x, gridPosition.y, 2f);
            
            // Instantiate the cube
            GameObject cubeObject = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPosition, Quaternion.identity);
            
            // Get or add cube manager component
            CubeManager cubeManager = cubeObject.GetComponent<CubeManager>();
            if (cubeManager == null)
            {
                cubeManager = cubeObject.AddComponent<CubeManager>();
            }

            // Initialize the cube
            CubeData cubeData = new CubeData
            {
                type = cubeType,
                position = gridPosition,
                level = 1
            };

            cubeManager.Init(gridManager, cubeData, 2f);
            
            // Add to wave manager's active cubes list
            if (waveManager.activeCubes == null)
            {
                waveManager.activeCubes = new List<CubeManager>();
            }
            waveManager.activeCubes.Add(cubeManager);

            Debug.Log($"DebugCubeSpawnHelper: Successfully spawned {cubeType} cube at grid ({gridPosition.x}, {gridPosition.y})");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DebugCubeSpawnHelper: Error spawning cube: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Spawns multiple cubes in a line pattern
    /// </summary>
    public static int SpawnCubeLinePattern(Vector2Int startPosition, CubeType cubeType, int count, Vector2Int direction, GridManager gridManager, WaveManager waveManager)
    {
        int successCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int spawnPosition = startPosition + (direction * i);
            
            if (SpawnCubeAt(spawnPosition, cubeType, gridManager, waveManager))
            {
                successCount++;
            }
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Spawned {successCount}/{count} cubes in line pattern");
        return successCount;
    }

    /// <summary>
    /// Spawns multiple cubes in a rectangular pattern
    /// </summary>
    public static int SpawnCubeRectPattern(Vector2Int startPosition, CubeType cubeType, int width, int height, GridManager gridManager, WaveManager waveManager)
    {
        int successCount = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int spawnPosition = new Vector2Int(startPosition.x + x, startPosition.y + y);
                
                if (SpawnCubeAt(spawnPosition, cubeType, gridManager, waveManager))
                {
                    successCount++;
                }
            }
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Spawned {successCount}/{width * height} cubes in {width}x{height} rectangle");
        return successCount;
    }

    #endregion

    #region Cube Finding and Selection

    /// <summary>
    /// Finds all cubes at the specified grid position
    /// </summary>
    public static List<CubeManager> FindCubesAt(Vector2Int gridPosition)
    {
        var cubesAtPosition = new List<CubeManager>();
        
        // Find all CubeManager components in the scene
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        
        foreach (var cube in allCubes)
        {
            if (cube.position.x == gridPosition.x && cube.position.y == gridPosition.y)
            {
                cubesAtPosition.Add(cube);
            }
        }
        
        return cubesAtPosition;
    }

    /// <summary>
    /// Finds the first cube of a specific type at the given position
    /// </summary>
    public static CubeManager FindCubeOfTypeAt(Vector2Int gridPosition, CubeType cubeType)
    {
        var cubesAtPosition = FindCubesAt(gridPosition);
        return cubesAtPosition.FirstOrDefault(cube => cube.type == cubeType);
    }

    /// <summary>
    /// Gets all cubes in the scene grouped by position
    /// </summary>
    public static Dictionary<Vector2Int, List<CubeManager>> GetCubesByPosition()
    {
        var cubesByPosition = new Dictionary<Vector2Int, List<CubeManager>>();
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        
        foreach (var cube in allCubes)
        {
            Vector2Int position = cube.position;
            
            if (!cubesByPosition.ContainsKey(position))
            {
                cubesByPosition[position] = new List<CubeManager>();
            }
            
            cubesByPosition[position].Add(cube);
        }
        
        return cubesByPosition;
    }

    /// <summary>
    /// Gets all cubes in the scene grouped by type
    /// </summary>
    public static Dictionary<CubeType, List<CubeManager>> GetCubesByType()
    {
        var cubesByType = new Dictionary<CubeType, List<CubeManager>>();
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        
        foreach (var cube in allCubes)
        {
            CubeType type = cube.type;
            
            if (!cubesByType.ContainsKey(type))
            {
                cubesByType[type] = new List<CubeManager>();
            }
            
            cubesByType[type].Add(cube);
        }
        
        return cubesByType;
    }

    #endregion

    #region Cube State Analysis

    /// <summary>
    /// Gets comprehensive statistics about all cubes in the scene
    /// </summary>
    public static CubeStatistics GetCubeStatistics()
    {
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        var stats = new CubeStatistics();
        
        stats.TotalCubes = allCubes.Length;
        stats.CubesByType = new Dictionary<CubeType, int>();
        stats.PaintedCubes = 0;
        stats.HealthyCubes = 0;
        stats.DamagedCubes = 0;
        
        foreach (var cube in allCubes)
        {
            // Count by type
            if (!stats.CubesByType.ContainsKey(cube.type))
            {
                stats.CubesByType[cube.type] = 0;
            }
            stats.CubesByType[cube.type]++;
            
            // Count painted cubes
            if (HasAnyPaintedFace(cube))
            {
                stats.PaintedCubes++;
            }
            
            // Count health status
            if (cube.currentHitPoints >= cube.maxHitPoints)
            {
                stats.HealthyCubes++;
            }
            else
            {
                stats.DamagedCubes++;
            }
        }
        
        return stats;
    }

    /// <summary>
    /// Checks if a cube has any painted faces
    /// </summary>
    public static bool HasAnyPaintedFace(CubeManager cube)
    {
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            if (cube.GetFaceStatus(face) != FaceStatus.None)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the number of painted faces on a cube
    /// </summary>
    public static int GetPaintedFaceCount(CubeManager cube)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            if (cube.GetFaceStatus(face) != FaceStatus.None)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region Cleanup and Management

    /// <summary>
    /// Destroys all cubes at the specified position
    /// </summary>
    public static int DestroyCubesAt(Vector2Int gridPosition, WaveManager waveManager = null)
    {
        var cubesAtPosition = FindCubesAt(gridPosition);
        int destroyedCount = 0;
        
        foreach (var cube in cubesAtPosition)
        {
            // Remove from wave manager's active cubes if provided
            if (waveManager?.activeCubes != null)
            {
                waveManager.activeCubes.Remove(cube);
            }
            
            Object.Destroy(cube.gameObject);
            destroyedCount++;
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Destroyed {destroyedCount} cubes at ({gridPosition.x}, {gridPosition.y})");
        return destroyedCount;
    }

    /// <summary>
    /// Destroys all cubes of a specific type
    /// </summary>
    public static int DestroyCubesOfType(CubeType cubeType, WaveManager waveManager = null)
    {
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        int destroyedCount = 0;
        
        foreach (var cube in allCubes)
        {
            if (cube.type == cubeType)
            {
                // Remove from wave manager's active cubes if provided
                if (waveManager?.activeCubes != null)
                {
                    waveManager.activeCubes.Remove(cube);
                }
                
                Object.Destroy(cube.gameObject);
                destroyedCount++;
            }
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Destroyed {destroyedCount} {cubeType} cubes");
        return destroyedCount;
    }

    /// <summary>
    /// Destroys all cubes in the scene
    /// </summary>
    public static int DestroyAllCubes(WaveManager waveManager = null)
    {
        var allCubes = Object.FindObjectsOfType<CubeManager>();
        int destroyedCount = allCubes.Length;
        
        // Clear wave manager's active cubes list if provided
        if (waveManager?.activeCubes != null)
        {
            waveManager.activeCubes.Clear();
        }
        
        foreach (var cube in allCubes)
        {
            Object.Destroy(cube.gameObject);
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Destroyed all {destroyedCount} cubes");
        return destroyedCount;
    }

    #endregion

    #region Advanced Spawning Patterns

    /// <summary>
    /// Spawns cubes in a custom pattern based on a pattern string
    /// Pattern format: "NRA" means Normal, Reinforced, Armored in a line
    /// </summary>
    public static int SpawnCubePattern(Vector2Int startPosition, string pattern, Vector2Int direction, GridManager gridManager, WaveManager waveManager)
    {
        if (string.IsNullOrEmpty(pattern))
            return 0;
            
        int successCount = 0;
        
        for (int i = 0; i < pattern.Length; i++)
        {
            CubeType cubeType = CharToCubeType(pattern[i]);
            if (cubeType == CubeType.Normal && pattern[i] != 'N') // Skip invalid characters
                continue;
                
            Vector2Int spawnPosition = startPosition + (direction * i);
            
            if (SpawnCubeAt(spawnPosition, cubeType, gridManager, waveManager))
            {
                successCount++;
            }
        }
        
        Debug.Log($"DebugCubeSpawnHelper: Spawned {successCount}/{pattern.Length} cubes from pattern '{pattern}'");
        return successCount;
    }

    /// <summary>
    /// Converts a character to a cube type for pattern spawning
    /// </summary>
    private static CubeType CharToCubeType(char c)
    {
        switch (char.ToUpper(c))
        {
            case 'N': return CubeType.Normal;
            case 'A': return CubeType.Armored;
            case 'R': return CubeType.Reinforced;
            case 'B': return CubeType.Berserker;
            case 'H': return CubeType.Heavy;
            default: return CubeType.Normal;
        }
    }

    #endregion

    #region Quick Access Methods

    /// <summary>
    /// Quick method to spawn a normal cube at player position offset
    /// </summary>
    public static bool QuickSpawnAtPlayer(PlayerManager playerManager, Vector2Int offset, GridManager gridManager, WaveManager waveManager, CubeType cubeType = CubeType.Normal)
    {
        if (playerManager == null)
            return false;
            
        Vector2Int spawnPosition = playerManager.currentTilePosition + offset;
        return SpawnCubeAt(spawnPosition, cubeType, gridManager, waveManager);
    }

    /// <summary>
    /// Quick method to spawn a test formation around a position
    /// </summary>
    public static int QuickSpawnTestFormation(Vector2Int centerPosition, GridManager gridManager, WaveManager waveManager)
    {
        var positions = new[]
        {
            centerPosition,
            centerPosition + Vector2Int.up,
            centerPosition + Vector2Int.down,
            centerPosition + Vector2Int.left,
            centerPosition + Vector2Int.right
        };
        
        var cubeTypes = new[]
        {
            CubeType.Normal,
            CubeType.Armored,
            CubeType.Reinforced,
            CubeType.Berserker,
            CubeType.Heavy
        };
        
        int spawnedCount = 0;
        for (int i = 0; i < positions.Length && i < cubeTypes.Length; i++)
        {
            if (SpawnCubeAt(positions[i], cubeTypes[i], gridManager, waveManager))
            {
                spawnedCount++;
            }
        }
        
        return spawnedCount;
    }

    #endregion
}

/// <summary>
/// Statistics about cubes in the scene
/// </summary>
public class CubeStatistics
{
    public int TotalCubes;
    public Dictionary<CubeType, int> CubesByType;
    public int PaintedCubes;
    public int HealthyCubes;
    public int DamagedCubes;
    
    public float PaintedPercentage => TotalCubes > 0 ? (float)PaintedCubes / TotalCubes * 100f : 0f;
    public float HealthyPercentage => TotalCubes > 0 ? (float)HealthyCubes / TotalCubes * 100f : 0f;
}
