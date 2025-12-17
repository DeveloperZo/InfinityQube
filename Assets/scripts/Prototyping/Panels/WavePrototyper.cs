using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Wave Prototyper - Wave and cube control panel.
/// Features a visual wave editor grid for configuring and spawning waves.
/// Can track live waves on board or design new waves to spawn.
/// </summary>
public class WavePrototyper : PrototypingPanelBase
{
    public override string PanelName => "Wave";
    public override string PanelIcon => "W";
    public override PrototypingCategory Category => PrototypingCategory.Wave;
    public override int Priority => 10;
    
    // Mode: Track live board vs design new wave
    private enum EditorMode { TrackBoard, DesignNew }
    private EditorMode currentMode = EditorMode.TrackBoard;
    
    // Wave Editor State (for DesignNew mode)
    private int waveWidth = 6;
    private int waveHeight = 3;
    private CubeType?[,] waveGrid; // null = empty, CubeType = cube
    private CubeType selectedBrush = CubeType.Unit;
    private bool eraseMode = false;
    
    // Runtime state
    private float waveSpeed = 1f;
    private bool isPaused = false;
    
    // Custom wave tracking
    private bool hasCustomWave = false;
    private CubeType?[,] lastSpawnedWave = null;
    private int lastSpawnedWaveWidth = 0;
    private int lastSpawnedWaveHeight = 0;
    
    // Section toggles
    private bool showWaveEditor = true;
    private bool showWaveControls = true;
    private bool showQuickSpawn = false;
    
    public override void Initialize()
    {
        base.Initialize();
        InitializeWaveGrid();
    }
    
    private void InitializeWaveGrid()
    {
        waveGrid = new CubeType?[waveWidth, waveHeight];
        // Start with empty grid
        for (int x = 0; x < waveWidth; x++)
            for (int y = 0; y < waveHeight; y++)
                waveGrid[x, y] = null;
    }
    
    public override void Update()
    {
        // Sync pause state with time scale
        isPaused = Time.timeScale == 0f;
    }
    
    // Get all active cubes from the wave manager
    private List<CubeManager> GetActiveCubes()
    {
        if (waveManager?.activeCubes == null) return new List<CubeManager>();
        return waveManager.activeCubes.Where(c => c != null && !c.isDestroyed).ToList();
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        // No quick actions - all controls are in panel
        return new List<QuickAction>();
    }
    
    public override void DrawGUI()
    {
        var activeCubes = GetActiveCubes();
        
        // Status with wave label (1, 1M, 2, 2M, etc.)
        string waveLabel = waveManager?.GetWaveLabel() ?? "?";
        string modeStr = currentMode == EditorMode.TrackBoard ? "TRACK" : "DESIGN";
        string status = waveManager?.waveActive == true 
            ? $"{modeStr} | Wave {waveLabel} | Cubes: {activeCubes.Count}" 
            : $"{modeStr} | Wave {waveLabel} (stopped) | Cubes: {activeCubes.Count}";
        DrawStatus(status);
        
        GUILayout.Space(5);
        
        // Mode Toggle
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = currentMode == EditorMode.TrackBoard ? Color.cyan : Color.white;
        if (GUILayout.Button("Track Board", GUILayout.Height(25)))
        {
            currentMode = EditorMode.TrackBoard;
        }
        GUI.backgroundColor = currentMode == EditorMode.DesignNew ? Color.cyan : Color.white;
        if (GUILayout.Button("Design New", GUILayout.Height(25)))
        {
            currentMode = EditorMode.DesignNew;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Wave Editor - different view based on mode
        showWaveEditor = DrawToggleSection(currentMode == EditorMode.TrackBoard ? "LIVE WAVE VIEW" : "WAVE DESIGNER", showWaveEditor);
        if (showWaveEditor)
        {
            if (currentMode == EditorMode.TrackBoard)
            {
                DrawLiveWaveView(activeCubes);
            }
            else
            {
                DrawWaveDesigner();
            }
        }
        
        // Wave Controls
        showWaveControls = DrawToggleSection("WAVE CONTROLS", showWaveControls);
        if (showWaveControls)
        {
            DrawSection("", () =>
            {
                // Wave status
                string waveStatus = "";
                if (hasCustomWave)
                {
                    waveStatus = $"Custom Wave | Cubes: {GetActiveCubes().Count}";
                }
                else if (waveManager?.useWaveConfiguration == true)
                {
                    string waveLabel = waveManager?.GetWaveLabel() ?? "?";
                    int totalWaves = waveManager?.waveConfiguration?.Count ?? 0;
                    waveStatus = $"Wave {waveLabel}/{totalWaves} (Configured)";
                }
                else
                {
                    waveStatus = "No wave configured";
                }
                DrawStatus(waveStatus);
                
                GUILayout.Space(3);
                
                // Main control buttons
                DrawButtonRow(
                    ("Start", StartWave),
                    ("Stop", StopWave),
                    ("Respawn", RespawnWave),
                    (isPaused ? "Resume" : "Pause", TogglePause)
                );
                
                // Manual step controls - for testing highlight sequences
                GUILayout.Space(5);
                GUILayout.Label("Step Control:");
                GUILayout.Label($"Current Step: {waveManager?.MoveStep ?? 0}");
                GUILayout.BeginHorizontal();
                
                // Step backward - only enabled if MoveStep > 0
                bool canStepBack = waveManager != null && waveManager.MoveStep > 0;
                GUI.enabled = canStepBack;
                if (GUILayout.Button("◄ Step -"))
                {
                    StepBackward();
                }
                
                // Step forward - always enabled (will auto-enable debug mode if needed)
                GUI.enabled = true;
                if (GUILayout.Button("Step + ►"))
                {
                    StepForward();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                
                if (waveManager != null && !waveManager.waveActive)
                {
                    GUILayout.Label("⚠ Decrement doesn't restore state (uncapture cubes, despawn player cubes)", GUI.skin.box);
                }
                
                DrawButtonRow(
                    ("Clear", ClearCubes),
                    ("Complete", ForceComplete)
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
        
        // Quick Spawn
        showQuickSpawn = DrawToggleSection("QUICK SPAWN", showQuickSpawn);
        if (showQuickSpawn)
        {
            DrawSection("", () =>
            {
                GUILayout.Label("Spawn single cube at top row:");
                GUILayout.BeginHorizontal();
                int width = gridManager?.Width ?? 10;
                for (int x = 0; x < Mathf.Min(width, 12); x++)
                {
                    int col = x;
                    if (GUILayout.Button($"{x}", GUILayout.Width(28)))
                    {
                        SpawnSingleCube(col, selectedBrush);
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                DrawButtonRow(
                    ("Move Down", MoveDown),
                    ("Drop All", DropAll)
                );
            });
        }
    }
    
    #region Wave Editor - Live View
    private void DrawLiveWaveView(List<CubeManager> activeCubes)
    {
        DrawSection("", () =>
        {
            if (activeCubes.Count == 0)
            {
                GUILayout.Label("No cubes on board. Start a wave or switch to Design mode.");
                return;
            }
            
            // Brush selection for editing
            GUILayout.Label("Edit Brush (click cube to change):");
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = eraseMode ? Color.red : Color.white;
            if (GUILayout.Button("Delete", GUILayout.Width(55)))
            {
                eraseMode = true;
            }
            
            foreach (CubeType type in System.Enum.GetValues(typeof(CubeType)))
            {
                GUI.backgroundColor = (!eraseMode && type == selectedBrush) ? GetCubeColor(type) : Color.white;
                string label = type.ToString().Substring(0, 1);
                if (GUILayout.Button(label, GUILayout.Width(28)))
                {
                    selectedBrush = type;
                    eraseMode = false;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Build visual grid from active cubes
            int gridW = gridManager?.Width ?? 10;
            int gridH = gridManager?.Height ?? 20;
            
            // Find bounds of cubes
            int minY = gridH, maxY = 0;
            foreach (var cube in activeCubes)
            {
                minY = Mathf.Min(minY, cube.position.y);
                maxY = Mathf.Max(maxY, cube.position.y);
            }
            
            // Clamp display range
            minY = Mathf.Max(0, minY);
            maxY = Mathf.Min(gridH - 1, maxY);
            
            if (maxY < minY)
            {
                GUILayout.Label("No cubes visible in grid range.");
                return;
            }
            
            GUILayout.Label($"Live Grid (rows {minY}-{maxY}, click to edit):");
            
            // Draw grid from top to bottom
            for (int y = maxY; y >= minY; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{y}", GUILayout.Width(20));
                
                for (int x = 0; x < Mathf.Min(gridW, 12); x++)
                {
                    int cx = x, cy = y;
                    
                    // Find cube at this position
                    var cubeAtPos = activeCubes.FirstOrDefault(c => c.position.x == x && c.position.y == y);
                    
                    if (cubeAtPos != null)
                    {
                        GUI.backgroundColor = GetCubeColor(cubeAtPos.type);
                        string label = cubeAtPos.type.ToString().Substring(0, 1);
                        if (GUILayout.Button(label, GUILayout.Width(28), GUILayout.Height(28)))
                        {
                            if (eraseMode)
                                DestroyCube(cubeAtPos);
                            else
                                ChangeCubeType(cubeAtPos, selectedBrush);
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = Color.gray;
                        if (GUILayout.Button("·", GUILayout.Width(28), GUILayout.Height(28)))
                        {
                            if (!eraseMode)
                                SpawnCubeAt(cx, cy, selectedBrush);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(5);
            
            // Quick actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Row Top"))
            {
                AddRowAtTop();
            }
            if (GUILayout.Button("Copy to Designer"))
            {
                CopyBoardToDesigner(activeCubes);
            }
            GUILayout.EndHorizontal();
        });
    }
    #endregion
    
    #region Wave Editor - Designer
    private void DrawWaveDesigner()
    {
        DrawSection("", () =>
        {
            // Size controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size:", GUILayout.Width(40));
            
            int newWidth = waveWidth;
            int newHeight = waveHeight;
            
            GUILayout.Label("W:", GUILayout.Width(20));
            if (GUILayout.Button("-", GUILayout.Width(25)) && waveWidth > 1) newWidth--;
            GUILayout.Label($"{waveWidth}", GUILayout.Width(25));
            if (GUILayout.Button("+", GUILayout.Width(25)) && waveWidth < 15) newWidth++;
            
            GUILayout.Space(10);
            
            GUILayout.Label("H:", GUILayout.Width(20));
            if (GUILayout.Button("-", GUILayout.Width(25)) && waveHeight > 1) newHeight--;
            GUILayout.Label($"{waveHeight}", GUILayout.Width(25));
            if (GUILayout.Button("+", GUILayout.Width(25)) && waveHeight < 10) newHeight++;
            
            GUILayout.EndHorizontal();
            
            // Resize if needed
            if (newWidth != waveWidth || newHeight != waveHeight)
            {
                ResizeWaveGrid(newWidth, newHeight);
            }
            
            GUILayout.Space(5);
            
            // Brush selection
            GUILayout.Label("Brush:");
            GUILayout.BeginHorizontal();
            
            // Erase mode
            GUI.backgroundColor = eraseMode ? Color.red : Color.white;
            if (GUILayout.Button("Empty", GUILayout.Width(55)))
            {
                eraseMode = true;
            }
            
            // Cube type brushes
            foreach (CubeType type in System.Enum.GetValues(typeof(CubeType)))
            {
                GUI.backgroundColor = (!eraseMode && type == selectedBrush) ? GetCubeColor(type) : Color.white;
                string label = type.ToString().Substring(0, 1); // First letter
                if (GUILayout.Button(label, GUILayout.Width(28)))
                {
                    selectedBrush = type;
                    eraseMode = false;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Wave grid - draw from top to bottom (highest row first)
            GUILayout.Label("Design Grid (click to edit):");
            for (int y = waveHeight - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < waveWidth; x++)
                {
                    int cx = x, cy = y; // Capture for closure
                    
                    CubeType? cell = waveGrid[x, y];
                    GUI.backgroundColor = cell.HasValue ? GetCubeColor(cell.Value) : Color.gray;
                    
                    string label = cell.HasValue ? cell.Value.ToString().Substring(0, 1) : "·";
                    if (GUILayout.Button(label, GUILayout.Width(28), GUILayout.Height(28)))
                    {
                        if (eraseMode)
                            waveGrid[cx, cy] = null;
                        else
                            waveGrid[cx, cy] = selectedBrush;
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(5);
            
            // Wave editor actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Wave"))
            {
                SpawnConfiguredWave();
            }
            if (GUILayout.Button("Clear Grid"))
            {
                ClearWaveGrid();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Unit"))
            {
                FillGrid(CubeType.Unit);
            }
            if (GUILayout.Button("Fill Matrix"))
            {
                FillGrid(CubeType.Matrix);
            }
            if (GUILayout.Button("Fill Recursion"))
            {
                FillGrid(CubeType.Recursion);
            }
            GUILayout.EndHorizontal();
        });
    }
    
    private void ResizeWaveGrid(int newWidth, int newHeight)
    {
        CubeType?[,] newGrid = new CubeType?[newWidth, newHeight];
        
        // Copy existing data
        for (int x = 0; x < Mathf.Min(waveWidth, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(waveHeight, newHeight); y++)
            {
                newGrid[x, y] = waveGrid[x, y];
            }
        }
        
        waveWidth = newWidth;
        waveHeight = newHeight;
        waveGrid = newGrid;
    }
    
    private void ClearWaveGrid()
    {
        for (int x = 0; x < waveWidth; x++)
            for (int y = 0; y < waveHeight; y++)
                waveGrid[x, y] = null;
    }
    
    private void FillGrid(CubeType type)
    {
        for (int x = 0; x < waveWidth; x++)
            for (int y = 0; y < waveHeight; y++)
                waveGrid[x, y] = type;
    }
    
    private void AddRowAtTop()
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        int gridW = Mathf.Min(gridManager.Width, 12);
        
        for (int x = 0; x < gridW; x++)
        {
            SpawnCubeAt(x, topY, selectedBrush);
        }
        LogAction($"Added row of {selectedBrush} at top");
    }
    
    private void CopyBoardToDesigner(List<CubeManager> activeCubes)
    {
        if (activeCubes.Count == 0) return;
        
        // Find bounds
        int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
        foreach (var cube in activeCubes)
        {
            minX = Mathf.Min(minX, cube.position.x);
            maxX = Mathf.Max(maxX, cube.position.x);
            minY = Mathf.Min(minY, cube.position.y);
            maxY = Mathf.Max(maxY, cube.position.y);
        }
        
        // Resize designer grid to fit
        int newWidth = maxX - minX + 1;
        int newHeight = maxY - minY + 1;
        ResizeWaveGrid(Mathf.Min(newWidth, 15), Mathf.Min(newHeight, 10));
        ClearWaveGrid();
        
        // Copy cubes
        foreach (var cube in activeCubes)
        {
            int x = cube.position.x - minX;
            int y = cube.position.y - minY;
            if (x >= 0 && x < waveWidth && y >= 0 && y < waveHeight)
            {
                waveGrid[x, y] = cube.type;
            }
        }
        
        // Switch to design mode
        currentMode = EditorMode.DesignNew;
        LogAction($"Copied {activeCubes.Count} cubes to designer");
    }
    
    private Color GetCubeColor(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: return new Color(0.8f, 0.5f, 0.2f); // Orange
            case CubeType.Matrix: return new Color(0.2f, 0.5f, 0.8f); // Blue
            case CubeType.Recursion: return new Color(0.6f, 0.2f, 0.6f); // Purple
            case CubeType.Infinity: return new Color(0.1f, 0.1f, 0.1f); // Black
            default: return Color.white;
        }
    }
    #endregion
    
    #region Wave Spawning
    private void SpawnConfiguredWave()
    {
        if (waveManager == null || gridManager == null) return;
        
        // Stop any active wave and clear cubes
        waveManager.StopWave();
        waveManager.ClearAllCubes();
        
        // Spawn cubes from wave grid at top of actual grid
        int gridTop = gridManager.Height - 1;
        int count = 0;
        
        for (int y = 0; y < waveHeight; y++)
        {
            for (int x = 0; x < waveWidth; x++)
            {
                if (waveGrid[x, y].HasValue)
                {
                    // Calculate grid position: wave Y 0 = bottom of wave pattern, waveHeight-1 = top
                    // So we spawn from top of grid downward
                    int gridY = gridTop - (waveHeight - 1 - y);
                    int gridX = x;
                    
                    // Clamp to grid bounds
                    if (gridX >= 0 && gridX < gridManager.Width && gridY >= 0 && gridY < gridManager.Height)
                    {
                        SpawnCubeAt(gridX, gridY, waveGrid[x, y].Value);
                        count++;
                    }
                }
            }
        }
        
        // Track custom wave state
        hasCustomWave = true;
        lastSpawnedWaveWidth = waveWidth;
        lastSpawnedWaveHeight = waveHeight;
        
        // Deep copy the wave grid for respawn
        lastSpawnedWave = new CubeType?[waveWidth, waveHeight];
        for (int x = 0; x < waveWidth; x++)
        {
            for (int y = 0; y < waveHeight; y++)
            {
                lastSpawnedWave[x, y] = waveGrid[x, y];
            }
        }
        
        // Switch to track mode to see results
        currentMode = EditorMode.TrackBoard;
        LogAction($"Spawned designed wave: {count} cubes (not started - use Start button)");
    }
    
    private CubeManager SpawnCubeAt(int x, int y, CubeType type)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return null;
        int typeIndex = (int)type;
        if (typeIndex >= waveManager.cubePrefabs.Length) return null;
        
        var pos = new Vector2Int(x, y);
        if (!gridManager.IsValidGridPosition(pos)) return null;
        
        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 2f);
        var cubeObj = Object.Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        cube.Init(gridManager, new CubeData { type = type, position = pos, level = 1 }, 2f);
        waveManager.activeCubes.Add(cube);
        
        return cube;
    }
    
    private void SpawnSingleCube(int col, CubeType type)
    {
        if (gridManager == null) return;
        int topY = gridManager.Height - 1;
        SpawnCubeAt(col, topY, type);
    }
    
    private void ChangeCubeType(CubeManager cube, CubeType newType)
    {
        if (cube == null || waveManager?.cubePrefabs == null) return;
        if (cube.type == newType) return;
        
        // Store position
        var pos = cube.position;
        
        // Remove old cube
        waveManager.activeCubes.Remove(cube);
        Object.Destroy(cube.gameObject);
        
        // Spawn new cube at same position
        SpawnCubeAt(pos.x, pos.y, newType);
        
        LogAction($"Changed cube at ({pos.x},{pos.y}) to {newType}");
    }
    
    private void DestroyCube(CubeManager cube)
    {
        if (cube == null) return;
        
        var pos = cube.position;
        waveManager?.activeCubes.Remove(cube);
        Object.Destroy(cube.gameObject);
        LogAction($"Destroyed cube at ({pos.x},{pos.y})");
    }
    #endregion
    
    #region Wave Control Actions
    private void StartWave()
    {
        if (waveManager == null) return;
        
        // If we have a custom wave spawned, start it without clearing
        if (hasCustomWave && waveManager.activeCubes.Count > 0)
        {
            waveManager.StartWaveWithoutSpawning();
            LogAction("Started custom wave");
        }
        else if (waveManager.useWaveConfiguration)
        {
            // Start configured wave (normal behavior)
            waveManager.StartWave();
            LogAction($"Started wave {waveManager.GetWaveLabel()}");
        }
        else if (waveManager.activeCubes.Count > 0)
        {
            // Cubes exist but no config - start anyway
            waveManager.StartWaveWithoutSpawning();
            LogAction("Started wave with existing cubes");
        }
        else
        {
            LogAction("No cubes to start - spawn a wave first");
        }
    }
    
    private void StopWave()
    {
        if (waveManager == null) return;
        waveManager.StopWave();
        LogAction("Stopped wave");
    }
    
    private void StepForward()
    {
        if (waveManager == null) return;
        
        // Enter debug mode if needed to allow manual control
        if (!waveManager.debugMode && !waveManager.waveActive)
        {
            waveManager.EnterDebugMode(true);
        }
        
        waveManager.ManualMoveWaveForward();
        LogAction($"Stepped wave forward to step {waveManager.MoveStep}");
    }
    
    private void StepBackward()
    {
        if (waveManager == null) return;
        
        // Enter debug mode if needed to allow manual control
        if (!waveManager.debugMode && waveManager.waveActive)
        {
            waveManager.EnterDebugMode(true);
        }
        
        waveManager.ManualMoveWaveBackward();
        LogAction($"Stepped wave backward to step {waveManager.MoveStep}");
    }
    
    private void RespawnWave()
    {
        if (waveManager == null) return;
        
        if (hasCustomWave && lastSpawnedWave != null)
        {
            // Respawn custom wave from stored grid
            waveManager.StopWave();
            waveManager.ClearAllCubes();
            SpawnWaveFromGrid(lastSpawnedWave, lastSpawnedWaveWidth, lastSpawnedWaveHeight);
            LogAction("Respawned custom wave");
        }
        else if (waveManager.useWaveConfiguration)
        {
            // Original respawn logic for configured waves
            int currentIndex = waveManager.currentWaveIndex;
            
            waveManager.StopWave();
            waveManager.ClearAllCubes();
            waveManager.MoveStep = 0;
            isPaused = false;
            Time.timeScale = 1f;
            
            waveManager.currentWaveIndex = currentIndex;
            gridManager?.ClearAllMarkers();
            waveManager.StartWave();
            
            LogAction($"Respawned wave {waveManager.GetWaveLabel()}");
        }
        else
        {
            LogAction("No wave to respawn - spawn or load a wave first");
        }
    }
    
    private void SpawnWaveFromGrid(CubeType?[,] grid, int width, int height)
    {
        if (waveManager == null || gridManager == null || grid == null) return;
        
        int gridTop = gridManager.Height - 1;
        int count = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y].HasValue)
                {
                    int gridY = gridTop - (height - 1 - y);
                    int gridX = x;
                    
                    if (gridX >= 0 && gridX < gridManager.Width && gridY >= 0 && gridY < gridManager.Height)
                    {
                        SpawnCubeAt(gridX, gridY, grid[x, y].Value);
                        count++;
                    }
                }
            }
        }
        
        hasCustomWave = true;
        LogAction($"Respawned {count} cubes from stored wave");
    }
    
    private void TogglePause()
    {
        if (waveManager == null) return;
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Time.timeScale = 0f;
            LogAction("Wave paused");
        }
        else
        {
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
        LogAction("Cleared all cubes");
    }
    
    private void SetSpeed(float speed)
    {
        waveSpeed = speed;
        waveManager?.SetWaveSpeed(speed);
        LogAction($"Wave speed: {speed}x");
    }
    
    private void MoveDown()
    {
        if (waveManager == null) return;
        
        bool wasDebug = waveManager.debugMode;
        waveManager.debugMode = true;
        waveManager.ManualMoveWaveForward();
        waveManager.debugMode = wasDebug;
    }
    
    private void DropAll()
    {
        if (waveManager == null) return;
        
        bool wasDebug = waveManager.debugMode;
        waveManager.debugMode = true;
        
        for (int i = 0; i < 5; i++)
            waveManager.ManualMoveWaveForward();
        
        waveManager.debugMode = wasDebug;
    }
    #endregion
}
