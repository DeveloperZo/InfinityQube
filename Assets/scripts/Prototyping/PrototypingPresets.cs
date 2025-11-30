using UnityEngine;
using System.Collections.Generic;
using System.IO;
using static Enumerations;

/// <summary>
/// Preset system for saving and loading prototyping configurations.
/// Saves wave setups, grid states, and test scenarios for quick recall.
/// </summary>
public class PrototypingPresets
{
    #region Singleton
    private static PrototypingPresets _instance;
    public static PrototypingPresets Instance => _instance ??= new PrototypingPresets();
    #endregion
    
    #region Constants
    private const string PRESETS_FOLDER = "PrototypingPresets";
    private const string PRESET_EXTENSION = ".json";
    private const int MAX_PRESETS = 20;
    #endregion
    
    #region State
    private Dictionary<string, PrototypingPreset> loadedPresets = new Dictionary<string, PrototypingPreset>();
    private string presetsPath;
    #endregion
    
    #region Initialization
    private PrototypingPresets()
    {
        presetsPath = Path.Combine(Application.persistentDataPath, PRESETS_FOLDER);
        EnsureDirectoryExists();
        LoadAllPresets();
    }
    
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(presetsPath))
        {
            Directory.CreateDirectory(presetsPath);
            Debug.Log($"[PrototypingPresets] Created presets folder: {presetsPath}");
        }
    }
    #endregion
    
    #region Preset Management
    /// <summary>
    /// Save current game state as a preset
    /// </summary>
    public bool SavePreset(string name, string description = "")
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[PrototypingPresets] Cannot save preset with empty name");
            return false;
        }
        
        // Sanitize name
        name = SanitizeFileName(name);
        
        var preset = new PrototypingPreset
        {
            name = name,
            description = description,
            createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            version = 1
        };
        
        // Capture current state
        CaptureGridState(preset);
        CaptureWaveState(preset);
        CaptureGameState(preset);
        
        // Save to file
        string json = JsonUtility.ToJson(preset, true);
        string filePath = GetPresetPath(name);
        
        try
        {
            File.WriteAllText(filePath, json);
            loadedPresets[name] = preset;
            Debug.Log($"[PrototypingPresets] Saved preset: {name}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PrototypingPresets] Failed to save preset: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load and apply a preset
    /// </summary>
    public bool LoadPreset(string name)
    {
        if (!loadedPresets.TryGetValue(name, out var preset))
        {
            // Try loading from file
            string filePath = GetPresetPath(name);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[PrototypingPresets] Preset not found: {name}");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                preset = JsonUtility.FromJson<PrototypingPreset>(json);
                loadedPresets[name] = preset;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PrototypingPresets] Failed to load preset: {e.Message}");
                return false;
            }
        }
        
        // Apply preset
        ApplyPreset(preset);
        Debug.Log($"[PrototypingPresets] Loaded preset: {name}");
        return true;
    }
    
    /// <summary>
    /// Delete a preset
    /// </summary>
    public bool DeletePreset(string name)
    {
        string filePath = GetPresetPath(name);
        
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                loadedPresets.Remove(name);
                Debug.Log($"[PrototypingPresets] Deleted preset: {name}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PrototypingPresets] Failed to delete preset: {e.Message}");
                return false;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get list of all available presets
    /// </summary>
    public List<string> GetPresetNames()
    {
        return new List<string>(loadedPresets.Keys);
    }
    
    /// <summary>
    /// Get preset info without loading
    /// </summary>
    public PrototypingPreset GetPresetInfo(string name)
    {
        return loadedPresets.TryGetValue(name, out var preset) ? preset : null;
    }
    #endregion
    
    #region State Capture
    private void CaptureGridState(PrototypingPreset preset)
    {
        var gridManager = GridManager.Instance;
        if (gridManager == null) return;
        
        preset.gridWidth = gridManager.Width;
        preset.gridHeight = gridManager.Height;
        
        // Capture tile states
        preset.tileStates = new List<TileStateData>();
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                var tile = gridManager.GetTileAt(x, y);
                if (tile == null) continue;
                
                // Only save non-normal tiles
                if (tile.IsMatrixd || tile.IsBlackened || tile.HasMarker || !tile.IsPlayable)
                {
                    preset.tileStates.Add(new TileStateData
                    {
                        x = x,
                        y = y,
                        isMatrixd = tile.IsMatrixd,
                        isBlackened = tile.IsBlackened,
                        hasMarker = tile.HasMarker,
                        isFallen = !tile.IsPlayable
                    });
                }
            }
        }
    }
    
    private void CaptureWaveState(PrototypingPreset preset)
    {
        var waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager == null) return;
        
        preset.waveActive = waveManager.waveActive;
        preset.currentWaveName = waveManager.CurrentWave?.name ?? "";
        preset.moveInterval = waveManager.normalMoveInterval;
        preset.fastMoveInterval = waveManager.fastMoveInterval;
        preset.showMessages = waveManager.showMessages;
        
        // Capture active cubes
        preset.cubeStates = new List<CubeStateData>();
        if (waveManager.activeCubes != null)
        {
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube == null) continue;
                
                preset.cubeStates.Add(new CubeStateData
                {
                    type = cube.type,
                    x = cube.position.x,
                    y = cube.position.y,
                    level = 1 // Default level
                });
            }
        }
    }
    
    private void CaptureGameState(PrototypingPreset preset)
    {
        preset.timeScale = Time.timeScale;
        
        var stageManager = Object.FindFirstObjectByType<StageManager>();
        if (stageManager != null)
        {
            preset.currentStageIndex = stageManager.CurrentStageIndex;
        }
    }
    #endregion
    
    #region State Application
    private void ApplyPreset(PrototypingPreset preset)
    {
        ApplyGridState(preset);
        ApplyWaveState(preset);
        ApplyGameState(preset);
    }
    
    private void ApplyGridState(PrototypingPreset preset)
    {
        var gridManager = GridManager.Instance;
        if (gridManager == null) return;
        
        // Resize grid if needed
        if (gridManager.Width != preset.gridWidth || gridManager.Height != preset.gridHeight)
        {
            gridManager.ResizeGrid(preset.gridWidth, preset.gridHeight);
        }
        
        // Reset all tiles first
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                var tile = gridManager.GetTileAt(x, y);
                tile?.ResetTile();
                tile?.RestoreTile();
            }
        }
        
        // Apply saved tile states
        foreach (var tileState in preset.tileStates)
        {
            var tile = gridManager.GetTileAt(tileState.x, tileState.y);
            if (tile == null) continue;
            
            if (tileState.isFallen)
                tile.MakeTileFall();
            else if (tileState.isBlackened)
                tile.BlackenTile();
            else if (tileState.isMatrixd)
                tile.MatrixTile();
            
            if (tileState.hasMarker)
                gridManager.PlaceMarker(tileState.x, tileState.y);
        }
    }
    
    private void ApplyWaveState(PrototypingPreset preset)
    {
        var waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager == null) return;
        
        // Apply wave settings
        waveManager.normalMoveInterval = preset.moveInterval;
        waveManager.fastMoveInterval = preset.fastMoveInterval;
        waveManager.showMessages = preset.showMessages;
        
        // Clear existing cubes
        waveManager.ClearAllCubes();
        
        // Spawn saved cubes
        var gridManager = GridManager.Instance;
        if (gridManager != null && waveManager.cubePrefabs != null)
        {
            foreach (var cubeState in preset.cubeStates)
            {
                int typeIndex = (int)cubeState.type;
                if (typeIndex >= waveManager.cubePrefabs.Length) continue;
                
                Vector2Int pos = new Vector2Int(cubeState.x, cubeState.y);
                if (!gridManager.IsValidGridPosition(pos)) continue;
                
                Vector3 worldPos = gridManager.GridToWorldPosition(pos.x, pos.y, 2f);
                GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
                
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                var cubeData = new CubeData
                {
                    type = cubeState.type,
                    position = pos,
                    level = cubeState.level
                };
                
                cube.Init(gridManager, cubeData, 2f);
                waveManager.activeCubes.Add(cube);
            }
        }
    }
    
    private void ApplyGameState(PrototypingPreset preset)
    {
        Time.timeScale = preset.timeScale;
        
        var stageManager = Object.FindFirstObjectByType<StageManager>();
        if (stageManager != null && preset.currentStageIndex >= 0)
        {
            stageManager.LoadStage(preset.currentStageIndex);
        }
    }
    #endregion
    
    #region Utility
    private void LoadAllPresets()
    {
        loadedPresets.Clear();
        
        if (!Directory.Exists(presetsPath)) return;
        
        var files = Directory.GetFiles(presetsPath, "*" + PRESET_EXTENSION);
        foreach (var file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                var preset = JsonUtility.FromJson<PrototypingPreset>(json);
                if (preset != null && !string.IsNullOrEmpty(preset.name))
                {
                    loadedPresets[preset.name] = preset;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PrototypingPresets] Failed to load preset file {file}: {e.Message}");
            }
        }
        
        Debug.Log($"[PrototypingPresets] Loaded {loadedPresets.Count} presets from {presetsPath}");
    }
    
    private string GetPresetPath(string name)
    {
        return Path.Combine(presetsPath, name + PRESET_EXTENSION);
    }
    
    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
        {
            name = name.Replace(c, '_');
        }
        return name.Trim();
    }
    #endregion
}

#region Data Classes
[System.Serializable]
public class PrototypingPreset
{
    public string name;
    public string description;
    public string createdAt;
    public int version;
    
    // Grid state
    public int gridWidth;
    public int gridHeight;
    public List<TileStateData> tileStates = new List<TileStateData>();
    
    // Wave state
    public bool waveActive;
    public string currentWaveName;
    public float moveInterval;
    public float fastMoveInterval;
    public bool showMessages;
    public List<CubeStateData> cubeStates = new List<CubeStateData>();
    
    // Game state
    public float timeScale;
    public int currentStageIndex;
}

[System.Serializable]
public class TileStateData
{
    public int x;
    public int y;
    public bool isMatrixd;
    public bool isBlackened;
    public bool hasMarker;
    public bool isFallen;
}

[System.Serializable]
public class CubeStateData
{
    public CubeType type;
    public int x;
    public int y;
    public int level;
}
#endregion

