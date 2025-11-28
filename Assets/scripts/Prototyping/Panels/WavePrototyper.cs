using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Wave Prototyper - Wave and cube control panel.
/// </summary>
public class WavePrototyper : PrototypingPanelBase
{
    public override string PanelName => "Wave";
    public override string PanelIcon => "🌊";
    public override PrototypingCategory Category => PrototypingCategory.Wave;
    public override int Priority => 10;
    
    // State
    private float waveSpeed = 1f;
    private bool isPaused = false;
    private CubeType selectedCubeType = CubeType.Unit;
    private int spawnColumn = 0;
    
    // Section toggles
    private bool showWaveControls = true;
    private bool showCubeSpawning = true;
    private bool showActiveCubes = false;
    
    public override void Update()
    {
        // Sync pause state with time scale
        isPaused = Time.timeScale == 0f;
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("⏮", RespawnWave) { Group = QuickActionGroup.Wave, Priority = 1, Tooltip = "Respawn wave" },
            new QuickAction(isPaused ? "▶" : "⏸", TogglePause) { Group = QuickActionGroup.Wave, Priority = 2, IsHighlighted = () => isPaused },
            new QuickAction("U", () => SpawnCube(CubeType.Unit)) { Group = QuickActionGroup.Wave, Priority = 10, Tooltip = "Unit cube" },
            new QuickAction("P", () => SpawnCube(CubeType.Prime)) { Group = QuickActionGroup.Wave, Priority = 11, Tooltip = "Prime cube" },
            new QuickAction("R", () => SpawnCube(CubeType.Recursion)) { Group = QuickActionGroup.Wave, Priority = 12, Tooltip = "Recursion cube" },
            new QuickAction("🗑", ClearCubes) { Group = QuickActionGroup.Wave, Priority = 20, Tooltip = "Clear cubes" }
        };
    }
    
    public override void DrawGUI()
    {
        // Status
        string status = waveManager?.waveActive == true 
            ? $"Wave Active | Cubes: {waveManager.activeCubes?.Count ?? 0}" 
            : "No active wave";
        DrawStatus(status);
        
        GUILayout.Space(5);
        
        // Wave Controls
        showWaveControls = DrawToggleSection("WAVE CONTROLS", showWaveControls);
        if (showWaveControls)
        {
            DrawSection("", () =>
            {
                DrawButtonRow(
                    ("⏮ Respawn", RespawnWave),
                    (isPaused ? "▶ Resume" : "⏸ Pause", TogglePause),
                    ("⏭ Next", NextWave)
                );
                
                DrawButtonRow(
                    ("🗑 Clear", ClearCubes),
                    ("⚡ Complete", ForceComplete)
                );
                
                GUILayout.Space(5);
                waveSpeed = DrawSlider("Speed", waveSpeed, 0.25f, 4f);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.5x")) SetSpeed(0.5f);
                if (GUILayout.Button("1x")) SetSpeed(1f);
                if (GUILayout.Button("2x")) SetSpeed(2f);
                if (GUILayout.Button("4x")) SetSpeed(4f);
                GUILayout.EndHorizontal();
            });
        }
        
        // Cube Spawning
        showCubeSpawning = DrawToggleSection("CUBE SPAWNING", showCubeSpawning);
        if (showCubeSpawning)
        {
            DrawSection("", () =>
            {
                GUILayout.Label("Cube Type:");
                GUILayout.BeginHorizontal();
                foreach (CubeType type in System.Enum.GetValues(typeof(CubeType)))
                {
                    GUI.backgroundColor = (type == selectedCubeType) ? Color.cyan : Color.white;
                    if (GUILayout.Button(type.ToString()))
                    {
                        selectedCubeType = type;
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                int maxCol = (gridManager?.Width ?? 10) - 1;
                spawnColumn = DrawIntStepper("Column", spawnColumn, 0, maxCol);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Spawn at Column"))
                {
                    SpawnAtColumn(spawnColumn);
                }
                if (GUILayout.Button("Spawn Center"))
                {
                    SpawnCube(selectedCubeType);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                GUILayout.Label("Quick Spawn (top row):");
                GUILayout.BeginHorizontal();
                int width = gridManager?.Width ?? 10;
                for (int x = 0; x < Mathf.Min(width, 12); x++)
                {
                    int col = x;
                    if (GUILayout.Button($"{x}", GUILayout.Width(28)))
                    {
                        SpawnAtColumn(col);
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                DrawButtonRow(
                    ("Line", SpawnLine),
                    ("V-Shape", SpawnVShape),
                    ("Random 5", SpawnRandom5)
                );
                
                DrawButtonRow(
                    ("↓ Move", MoveDown),
                    ("⏬ Drop", DropAll)
                );
            });
        }
        
        // Active Cubes
        showActiveCubes = DrawToggleSection($"ACTIVE CUBES ({waveManager?.activeCubes?.Count ?? 0})", showActiveCubes);
        if (showActiveCubes && waveManager?.activeCubes != null)
        {
            DrawSection("", () =>
            {
                foreach (var cube in waveManager.activeCubes.Take(10))
                {
                    if (cube == null) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{cube.type} @ ({cube.position.x},{cube.position.y})");
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        waveManager.activeCubes.Remove(cube);
                        Object.Destroy(cube.gameObject);
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
                if (waveManager.activeCubes.Count > 10)
                {
                    GUILayout.Label($"... +{waveManager.activeCubes.Count - 10} more");
                }
            });
        }
    }
    
    #region Actions
    private void RespawnWave()
    {
        if (waveManager == null) return;
        
        // Store current wave index before stopping
        int currentIndex = waveManager.currentWaveIndex;
        
        // Stop and clear everything
        waveManager.StopWave();
        waveManager.ClearAllCubes();
        
        // Reset wave state
        waveManager.MoveStep = 0;
        isPaused = false;
        
        // Make sure we're on the same wave
        waveManager.currentWaveIndex = currentIndex;
        
        // Clear grid markers
        gridManager?.ClearAllMarkers();
        
        // Start fresh
        waveManager.StartWave();
        
        LogAction($"Respawned wave {currentIndex}");
    }
    
    private void TogglePause()
    {
        if (waveManager == null) return;
        isPaused = !isPaused;
        
        if (isPaused)
        {
            // Pause by setting time scale to 0
            Time.timeScale = 0f;
            LogAction("Wave paused");
        }
        else
        {
            // Resume by restoring time scale
            Time.timeScale = 1f;
            LogAction("Wave resumed");
        }
    }
    
    private void NextWave()
    {
        waveManager?.ForceCompleteWave();
    }
    
    private void ForceComplete()
    {
        waveManager?.ForceCompleteWave();
    }
    
    private void ClearCubes()
    {
        waveManager?.ClearAllCubes();
    }
    
    private void SetSpeed(float speed)
    {
        waveSpeed = speed;
        waveManager?.SetWaveSpeed(speed);
        LogAction($"Wave speed: {speed}x");
    }
    
    private void SpawnCube(CubeType type)
    {
        if (waveManager == null || gridManager == null) return;
        
        int topY = gridManager.Height - 1;
        int centerX = gridManager.Width / 2;
        SpawnCubeAt(centerX, topY, type);
    }
    
    private void SpawnAtColumn(int col)
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        SpawnCubeAt(col, topY, selectedCubeType);
    }
    
    private void SpawnCubeAt(int x, int y, CubeType type)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        if ((int)type >= waveManager.cubePrefabs.Length) return;
        
        var pos = new Vector2Int(x, y);
        if (!gridManager.IsValidGridPosition(pos)) return;
        
        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 2f);
        var cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)type], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        cube.Init(gridManager, new CubeData { type = type, position = pos, level = 1 }, 2f);
        waveManager.activeCubes.Add(cube);
    }
    
    private void SpawnLine()
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        for (int x = 0; x < gridManager.Width; x++)
            SpawnCubeAt(x, topY, selectedCubeType);
    }
    
    private void SpawnVShape()
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        int c = gridManager.Width / 2;
        SpawnCubeAt(c, topY, selectedCubeType);
        SpawnCubeAt(c - 1, topY - 1, selectedCubeType);
        SpawnCubeAt(c + 1, topY - 1, selectedCubeType);
    }
    
    private void SpawnRandom5()
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(0, gridManager.Width);
            var t = (CubeType)Random.Range(0, System.Enum.GetValues(typeof(CubeType)).Length);
            SpawnCubeAt(x, topY, t);
        }
    }
    
    private void MoveDown()
    {
        if (waveManager == null) return;
        
        // Enable debug mode temporarily to allow manual move
        bool wasDebug = waveManager.debugMode;
        waveManager.debugMode = true;
        waveManager.ManualMoveWaveForward();
        waveManager.debugMode = wasDebug;
    }
    
    private void DropAll()
    {
        if (waveManager == null) return;
        
        // Enable debug mode temporarily
        bool wasDebug = waveManager.debugMode;
        waveManager.debugMode = true;
        
        for (int i = 0; i < 5; i++)
            waveManager.ManualMoveWaveForward();
        
        waveManager.debugMode = wasDebug;
    }
    #endregion
}
