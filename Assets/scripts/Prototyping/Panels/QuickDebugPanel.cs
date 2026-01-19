using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Quick Debug Panel - Fast access to common debug actions.
/// Provides one-click actions, state snapshots, and quick test scenarios.
/// Optimized for rapid iteration during playtesting.
/// </summary>
public class QuickDebugPanel : PrototypingPanelBase
{
    public override string PanelName => "Quick";
    public override string PanelIcon => "Q";
    public override PrototypingCategory Category => PrototypingCategory.System;
    public override int Priority => 5; // Highest priority - first tab
    
    #region State
    private PlayerActionManager actionManager;
    private bool showQuickActions = true;
    private bool showStateControls = true;
    private bool showTestScenarios = true;
    private bool showHotkeys = false;
    
    // Snapshot state
    private struct GameSnapshot
    {
        public int waveIndex;
        public float timeScale;
        public Vector2Int playerPosition;
        public int unitCharges;
        public int matrixCharges;
        public int recursionCharges;
        public int infinityCharges;
        public bool hasSnapshot;
    }
    private GameSnapshot savedSnapshot;
    #endregion
    
    public override void Initialize()
    {
        base.Initialize();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void DrawGUI()
    {
        if (actionManager == null)
        {
            actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        }
        
        // Compact status line
        string status = BuildStatusLine();
        DrawStatus(status);
        
        GUILayout.Space(5);
        
        // Quick Actions - Most used buttons
        showQuickActions = DrawToggleSection("QUICK ACTIONS", showQuickActions);
        if (showQuickActions)
        {
            DrawQuickActions();
        }
        
        // State Controls
        showStateControls = DrawToggleSection("STATE CONTROLS", showStateControls);
        if (showStateControls)
        {
            DrawStateControls();
        }
        
        // Test Scenarios
        showTestScenarios = DrawToggleSection("TEST SCENARIOS", showTestScenarios);
        if (showTestScenarios)
        {
            DrawTestScenarios();
        }
        
        // Hotkeys Reference
        showHotkeys = DrawToggleSection("HOTKEYS", showHotkeys);
        if (showHotkeys)
        {
            DrawHotkeys();
        }
    }
    
    private string BuildStatusLine()
    {
        var parts = new List<string>();
        
        // Time
        if (Time.timeScale == 0)
            parts.Add("PAUSED");
        else if (Mathf.Abs(Time.timeScale - 1f) > 0.01f)
            parts.Add($"{Time.timeScale:F1}x");
        
        // Wave
        if (waveManager != null)
        {
            string waveLabel = waveManager.GetWaveLabel() ?? "?";
            int cubes = waveManager.activeCubes?.Count ?? 0;
            parts.Add($"W{waveLabel}:{cubes}c");
        }
        
        // Player position
        if (playerManager != null)
        {
            var pos = playerManager.currentTilePosition;
            parts.Add($"P({pos.x},{pos.y})");
        }
        
        return string.Join(" | ", parts);
    }
    
    #region Quick Actions
    private void DrawQuickActions()
    {
        DrawSection("", () =>
        {
            // Row 1: Time controls
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Time.timeScale == 0 ? Color.yellow : Color.white;
            if (GUILayout.Button(Time.timeScale == 0 ? "▶ PLAY" : "⏸ PAUSE", GUILayout.Height(30)))
            {
                Time.timeScale = Time.timeScale == 0 ? 1f : 0f;
            }
            
            GUI.backgroundColor = Mathf.Abs(Time.timeScale - 0.25f) < 0.01f ? Color.cyan : Color.white;
            if (GUILayout.Button("¼x", GUILayout.Height(30), GUILayout.Width(35)))
            {
                Time.timeScale = 0.25f;
            }
            
            GUI.backgroundColor = Mathf.Abs(Time.timeScale - 0.5f) < 0.01f ? Color.cyan : Color.white;
            if (GUILayout.Button("½x", GUILayout.Height(30), GUILayout.Width(35)))
            {
                Time.timeScale = 0.5f;
            }
            
            GUI.backgroundColor = Mathf.Abs(Time.timeScale - 1f) < 0.01f ? Color.cyan : Color.white;
            if (GUILayout.Button("1x", GUILayout.Height(30), GUILayout.Width(35)))
            {
                Time.timeScale = 1f;
            }
            
            GUI.backgroundColor = Mathf.Abs(Time.timeScale - 2f) < 0.01f ? Color.cyan : Color.white;
            if (GUILayout.Button("2x", GUILayout.Height(30), GUILayout.Width(35)))
            {
                Time.timeScale = 2f;
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Row 2: Wave controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Wave", GUILayout.Height(26)))
            {
                waveManager?.StartWave();
                LogAction("Started wave");
            }
            if (GUILayout.Button("Stop Wave", GUILayout.Height(26)))
            {
                waveManager?.StopWave();
                LogAction("Stopped wave");
            }
            if (GUILayout.Button("Respawn", GUILayout.Height(26)))
            {
                RespawnCurrentWave();
            }
            GUILayout.EndHorizontal();
            
            // Row 3: Clear controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cubes", GUILayout.Height(26)))
            {
                waveManager?.ClearAllCubes();
                LogAction("Cleared cubes");
            }
            if (GUILayout.Button("Clear Markers", GUILayout.Height(26)))
            {
                gridManager?.ClearAllMarkers();
                actionManager?.MarkerSystem?.ClearAllActions();
                LogAction("Cleared markers");
            }
            if (GUILayout.Button("Clear ALL", GUILayout.Height(26)))
            {
                ClearEverything();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Row 4: Marker refill
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Refill All Markers", GUILayout.Height(28)))
            {
                RefillAllMarkers();
            }
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button("Unlimited Mode", GUILayout.Height(28)))
            {
                EnableUnlimitedMode();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        });
    }
    #endregion
    
    #region State Controls
    private void DrawStateControls()
    {
        DrawSection("", () =>
        {
            // Snapshot controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("📷 Save Snapshot", GUILayout.Height(26)))
            {
                SaveSnapshot();
            }
            
            GUI.enabled = savedSnapshot.hasSnapshot;
            if (GUILayout.Button("📂 Load Snapshot", GUILayout.Height(26)))
            {
                LoadSnapshot();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            
            if (savedSnapshot.hasSnapshot)
            {
                GUILayout.Label($"Snapshot: Wave {savedSnapshot.waveIndex}, Player ({savedSnapshot.playerPosition.x},{savedSnapshot.playerPosition.y})", 
                    GUI.skin.box);
            }
            
            GUILayout.Space(5);
            
            // Wave step controls
            GUILayout.Label("Manual Step (for testing):");
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("◀ Step Back", GUILayout.Height(25)))
            {
                StepWaveBack();
            }
            
            GUILayout.Label($"Step: {waveManager?.MoveStep ?? 0}", GUILayout.Width(60));
            
            if (GUILayout.Button("Step Fwd ▶", GUILayout.Height(25)))
            {
                StepWaveForward();
            }
            GUILayout.EndHorizontal();
        });
    }
    #endregion
    
    #region Test Scenarios
    private void DrawTestScenarios()
    {
        DrawSection("", () =>
        {
            GUILayout.Label("Quick Test Setups:");
            
            // Row 1
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unit vs Unit"))
            {
                SetupCollisionTest(CubeType.Unit, CubeType.Unit);
            }
            if (GUILayout.Button("Matrix 3x3"))
            {
                SetupCollisionTest(CubeType.Matrix, CubeType.Matrix);
            }
            if (GUILayout.Button("Recursion Cross"))
            {
                SetupCollisionTest(CubeType.Recursion, CubeType.Recursion);
            }
            GUILayout.EndHorizontal();
            
            // Row 2
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Full Row"))
            {
                SpawnFullRowTest();
            }
            if (GUILayout.Button("Mixed Types"))
            {
                SpawnMixedWaveTest();
            }
            if (GUILayout.Button("Stress Test"))
            {
                SpawnStressTest();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Special tests
            GUILayout.Label("Marker Tests:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Unit@Player"))
            {
                PlaceMarkerAtPlayer(MarkerMode.Unit);
            }
            if (GUILayout.Button("Place Matrix@Player"))
            {
                PlaceMarkerAtPlayer(MarkerMode.Matrix);
            }
            if (GUILayout.Button("Place Recur@Player"))
            {
                PlaceMarkerAtPlayer(MarkerMode.Recursion);
            }
            GUILayout.EndHorizontal();
        });
    }
    #endregion
    
    #region Hotkeys
    private void DrawHotkeys()
    {
        DrawSection("", () =>
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 11;
            
            GUILayout.Label("Global:", style);
            GUILayout.Label("  F12 - Toggle this panel", style);
            GUILayout.Label("  1/2/3/4 - Select marker type", style);
            GUILayout.Label("  F - Place marker (hold)", style);
            GUILayout.Label("  Space - Trigger marker", style);
            
            GUILayout.Space(3);
            GUILayout.Label("Movement:", style);
            GUILayout.Label("  WASD/Arrows - Move player", style);
            
            GUILayout.Space(3);
            GUILayout.Label("Debug (when panel open):", style);
            GUILayout.Label("  P - Pause/Resume", style);
            GUILayout.Label("  R - Respawn wave", style);
            GUILayout.Label("  C - Clear all", style);
        });
    }
    #endregion
    
    #region Action Implementations
    private void RespawnCurrentWave()
    {
        if (waveManager == null) return;
        
        int currentIndex = waveManager.currentWaveIndex;
        waveManager.StopWave();
        waveManager.ClearAllCubes();
        waveManager.MoveStep = 0;
        Time.timeScale = 1f;
        
        if (waveManager.useWaveConfiguration)
        {
            waveManager.currentWaveIndex = currentIndex;
            gridManager?.ClearAllMarkers();
            waveManager.StartWave();
        }
        
        LogAction($"Respawned wave {waveManager.GetWaveLabel()}");
    }
    
    private void ClearEverything()
    {
        waveManager?.StopWave();
        waveManager?.ClearAllCubes();
        gridManager?.ClearAllMarkers();
        actionManager?.MarkerSystem?.ClearAllActions();
        actionManager?.MarkerSystem?.ClearPlayerCubes();
        Time.timeScale = 1f;
        LogAction("Cleared everything");
    }
    
    private void RefillAllMarkers()
    {
        if (actionManager == null) return;
        actionManager.RefillUnitMarkerCharges();
        actionManager.RefillMatrixMarkerCharges();
        actionManager.RefillRecursionMarkerCharges();
        actionManager.RefillInfinityMarkerCharges();
        LogAction("Refilled all markers");
    }
    
    private void EnableUnlimitedMode()
    {
        if (actionManager == null) return;
        
        actionManager.unitMarkerRechargeRate = 1;
        actionManager.maxUnitMarkerCharges = 99;
        actionManager.maxRecursionMarkerCharges = 99;
        actionManager.maxMatrixMarkerCharges = 99;
        actionManager.maxInfinityMarkerCharges = 99;
        actionManager.maxUnitMarkers = 99;
        actionManager.maxRecursionMarkers = 99;
        actionManager.maxMatrixMarkers = 99;
        actionManager.maxInfinityMarkers = 99;
        
        RefillAllMarkers();
        LogAction("Unlimited mode enabled");
    }
    
    private void SaveSnapshot()
    {
        savedSnapshot = new GameSnapshot
        {
            waveIndex = waveManager?.currentWaveIndex ?? 0,
            timeScale = Time.timeScale,
            playerPosition = playerManager?.currentTilePosition ?? Vector2Int.zero,
            unitCharges = actionManager?.GetUnitMarkerCharges() ?? 0,
            matrixCharges = actionManager?.GetMatrixMarkerCharges() ?? 0,
            recursionCharges = actionManager?.GetRecursionMarkerCharges() ?? 0,
            infinityCharges = actionManager?.GetInfinityMarkerCharges() ?? 0,
            hasSnapshot = true
        };
        LogAction("Snapshot saved");
    }
    
    private void LoadSnapshot()
    {
        if (!savedSnapshot.hasSnapshot) return;
        
        // Clear current state
        waveManager?.StopWave();
        waveManager?.ClearAllCubes();
        gridManager?.ClearAllMarkers();
        
        // Restore state
        if (waveManager != null && waveManager.useWaveConfiguration)
        {
            waveManager.currentWaveIndex = savedSnapshot.waveIndex;
        }
        
        Time.timeScale = savedSnapshot.timeScale;
        
        if (playerManager != null && gridManager != null)
        {
            playerManager.currentTilePosition = savedSnapshot.playerPosition;
            playerManager.transform.position = gridManager.GridToWorldPosition(
                savedSnapshot.playerPosition.x, savedSnapshot.playerPosition.y, 0);
        }
        
        // Start wave at saved position
        waveManager?.StartWave();
        
        LogAction("Snapshot loaded");
    }
    
    private void StepWaveBack()
    {
        if (waveManager == null) return;
        
        if (!waveManager.debugMode)
        {
            waveManager.EnterDebugMode(true);
        }
        
        waveManager.ManualMoveWaveBackward();
        LogAction($"Step back to {waveManager.MoveStep}");
    }
    
    private void StepWaveForward()
    {
        if (waveManager == null) return;
        
        if (!waveManager.debugMode && !waveManager.waveActive)
        {
            waveManager.EnterDebugMode(true);
        }
        
        waveManager.ManualMoveWaveForward();
        LogAction($"Step forward to {waveManager.MoveStep}");
    }
    
    private void SetupCollisionTest(CubeType playerType, CubeType waveType)
    {
        ClearEverything();
        
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int centerX = gridManager.Width / 2;
        int waveY = gridManager.Height - 2;
        int playerY = 2;
        
        // Spawn wave cube
        SpawnTestCube(centerX, waveY, waveType, false);
        
        // Spawn player cube
        SpawnTestCube(centerX, playerY, playerType, true);
        
        // Start wave movement
        waveManager.StartWaveWithoutSpawning();
        Time.timeScale = 0.5f;
        
        LogAction($"Setup: {playerType} vs {waveType}");
    }
    
    private void SpawnTestCube(int x, int y, CubeType type, bool isPlayer)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int typeIndex = (int)type;
        if (typeIndex >= waveManager.cubePrefabs.Length) return;
        
        var pos = new Vector2Int(x, y);
        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 2f);
        var cubeObj = Object.Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        cube.Init(gridManager, new CubeData { type = type, position = pos, level = 1 }, 2f);
        cube.isPlayerCube = isPlayer;
        
        if (isPlayer)
        {
            cube.isMatrixCube = type == CubeType.Matrix;
            cube.usePhysics = false;
            cube.ConfigurePlayerCubePhysics();
            cube.ApplyPlayerCubeMaterial();
            actionManager?.MarkerSystem?.playerCubes.Add(cube);
        }
        else
        {
            waveManager.activeCubes.Add(cube);
        }
    }
    
    private void SpawnFullRowTest()
    {
        ClearEverything();
        
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int waveY = gridManager.Height - 2;
        
        for (int x = 0; x < gridManager.Width; x++)
        {
            SpawnTestCube(x, waveY, CubeType.Unit, false);
        }
        
        waveManager.StartWaveWithoutSpawning();
        LogAction("Full row spawned");
    }
    
    private void SpawnMixedWaveTest()
    {
        ClearEverything();
        
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int waveY = gridManager.Height - 2;
        CubeType[] types = { CubeType.Unit, CubeType.Matrix, CubeType.Recursion, CubeType.Infinity };
        
        int x = 1;
        foreach (var type in types)
        {
            if (x < gridManager.Width - 1)
            {
                SpawnTestCube(x, waveY, type, false);
                x += 3;
            }
        }
        
        waveManager.StartWaveWithoutSpawning();
        LogAction("Mixed wave spawned");
    }
    
    private void SpawnStressTest()
    {
        ClearEverything();
        
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        // Fill top 5 rows with cubes
        for (int y = gridManager.Height - 1; y >= gridManager.Height - 5; y--)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                CubeType type = (CubeType)((x + y) % 3); // Cycle Unit, Matrix, Recursion
                SpawnTestCube(x, y, type, false);
            }
        }
        
        waveManager.StartWaveWithoutSpawning();
        LogAction("Stress test: 5 rows spawned");
    }
    
    private void PlaceMarkerAtPlayer(MarkerMode mode)
    {
        if (actionManager == null || playerManager == null || gridManager == null) return;
        
        var oldMode = actionManager.GetCurrentMode();
        actionManager.SetMode(mode);
        
        Vector2Int pos = playerManager.currentTilePosition;
        gridManager.PlaceMarker(pos.x, pos.y);
        
        actionManager.SetMode(oldMode);
        LogAction($"Placed {mode} at ({pos.x}, {pos.y})");
    }
    #endregion
    
    #region Keyboard Input
    public override void Update()
    {
        // Only handle shortcuts when panel is visible
        if (!IsVisible) return;
        
        // P - Pause/Resume
        if (Input.GetKeyDown(KeyCode.P))
        {
            Time.timeScale = Time.timeScale == 0 ? 1f : 0f;
        }
        
        // R - Respawn
        if (Input.GetKeyDown(KeyCode.R) && !Input.GetKey(KeyCode.LeftControl))
        {
            RespawnCurrentWave();
        }
        
        // C - Clear all
        if (Input.GetKeyDown(KeyCode.C) && !Input.GetKey(KeyCode.LeftControl))
        {
            ClearEverything();
        }
    }
    #endregion
}
