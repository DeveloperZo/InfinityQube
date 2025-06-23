using static Enumerations;
using UnityEngine;

public class TestingDebugPanel : DebugPanelBase
{
    public override string PanelName => "Testing";
    public override DebugPanelGroup Group => DebugPanelGroup.Testing;

    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;

    // UI State
    private bool showFacePainting = true;
    private bool showTestScenarios = true;
    private bool showAdvancedTests = false;
    private bool showIntegrationTests = true;

    // Face Painting Controls
    private int selectedFaceStatus = 1; // 1 = Corrupted, 2 = Enhanced
    private int facePaintDuration = 3;
    private bool paintOnLanding = true;
    private bool paintOnExit = false;

    public override void Initialize()
    {
        base.Initialize(); // Initialize theme and performance systems
        
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
    }

    protected override void DrawPanelContent()
    {
        // Simple test content
        GUILayout.Label("=== TESTING DEBUG PANEL ===");
        GUILayout.Label("This panel is for integration testing!");
        
        GUILayout.Space(10);
        
        // Show manager status
        GUILayout.Label($"Grid Manager: {(gridManager != null ? "Found" : "Missing")}");
        GUILayout.Label($"Player Manager: {(playerManager != null ? "Found" : "Missing")}");
        GUILayout.Label($"Wave Manager: {(waveManager != null ? "Found" : "Missing")}");
        
        GUILayout.Space(10);
        
        // Simple test buttons
        if (GUILayout.Button("Test Button 1"))
        {
            Debug.Log("Testing Panel: Button 1 clicked!");
        }
        
        if (GUILayout.Button("Test Button 2"))
        {
            Debug.Log("Testing Panel: Button 2 clicked!");
        }
        
        try
        {
            DrawSectionToggles();
            GUILayout.Space(5);

            if (showFacePainting) DrawSimpleFacePaintingSection();
            if (showTestScenarios) DrawSimpleTestScenariosSection();
            if (showAdvancedTests) DrawSimpleAdvancedTestsSection();
            if (showIntegrationTests) DrawSimpleIntegrationTestsSection();
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"Testing Panel Error: {e.Message}");
        }
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        
        GUI.backgroundColor = showFacePainting ? Color.yellow : Color.white;
        if (GUILayout.Button("Face Paint")) showFacePainting = !showFacePainting;
        
        GUI.backgroundColor = showTestScenarios ? Color.yellow : Color.white;
        if (GUILayout.Button("Scenarios")) showTestScenarios = !showTestScenarios;
        
        GUI.backgroundColor = showAdvancedTests ? Color.yellow : Color.white;
        if (GUILayout.Button("Advanced")) showAdvancedTests = !showAdvancedTests;
        
        GUI.backgroundColor = showIntegrationTests ? Color.yellow : Color.white;
        if (GUILayout.Button("Integration")) showIntegrationTests = !showIntegrationTests;
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }
    
    private void DrawSimpleFacePaintingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTING (Simplified)", GUI.skin.box);
        GUILayout.Label("Face painting tools would go here.");
        if (GUILayout.Button("Test Face Paint"))
        {
            Debug.Log("Face painting test clicked!");
        }
        GUILayout.EndVertical();
    }
    
    private void DrawSimpleTestScenariosSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TEST SCENARIOS (Simplified)", GUI.skin.box);
        GUILayout.Label("Test scenarios would go here.");
        if (GUILayout.Button("Run Test Scenario"))
        {
            Debug.Log("Test scenario clicked!");
        }
        GUILayout.EndVertical();
    }
    
    private void DrawSimpleAdvancedTestsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ADVANCED TESTS (Simplified)", GUI.skin.box);
        GUILayout.Label("Advanced tests would go here.");
        if (GUILayout.Button("Run Advanced Test"))
        {
            Debug.Log("Advanced test clicked!");
        }
        GUILayout.EndVertical();
    }
    
    private void DrawSimpleIntegrationTestsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("INTEGRATION TESTS (Simplified)", GUI.skin.box);
        GUILayout.Label("Integration tests would go here.");
        if (GUILayout.Button("Run Integration Test"))
        {
            Debug.Log("Integration test clicked!");
        }
        GUILayout.EndVertical();
    }

    private void DrawFacePaintingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTING", GUI.skin.box);

        // Face status selector
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted")) selectedFaceStatus = 1;
        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Enhanced")) selectedFaceStatus = 2;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Duration and trigger controls
        facePaintDuration = DebugUIHelpers.DrawIntField("Duration:", facePaintDuration, -1, 10);
        GUILayout.Label("(-1 = permanent)");

        paintOnLanding = GUILayout.Toggle(paintOnLanding, "Paint on Landing");
        paintOnExit = GUILayout.Toggle(paintOnExit, "Paint on Exit");

        // Quick setup buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Player Tile"))
        {
            if (playerManager != null)
            {
                FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
                SetupTilePainting(playerManager.currentTilePosition, status, color, facePaintDuration);
            }
        }
        if (GUILayout.Button("Clear Player Tile"))
        {
            if (playerManager != null)
            {
                ClearTilePainting(playerManager.currentTilePosition);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Paint")) ClearAllTilePainting();
        if (GUILayout.Button("Test Pattern")) CreateFacePaintPattern();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawTestScenariosSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TEST SCENARIOS", GUI.skin.box);

        if (GUILayout.Button("Test: Corrupted Face (Normal→Black)"))
        {
            TestCorruptedFace();
        }

        if (GUILayout.Button("Test: Enhanced Face (Normal→Blue)"))
        {
            TestEnhancedFace();
        }

        if (GUILayout.Button("Test: Face Rotation & Duration"))
        {
            TestFaceRotationAndDuration();
        }

        if (GUILayout.Button("Test: Mixed Face Statuses"))
        {
            TestMixedFaceStatuses();
        }

        if (GUILayout.Button("Test: Action + Face Combo"))
        {
            TestActionFaceCombo();
        }

        GUILayout.Space(5);
        DrawActiveCubeFaceStatus();

        GUILayout.EndVertical();
    }

    private void DrawAdvancedTestsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ADVANCED TESTS", GUI.skin.box);

        if (GUILayout.Button("Stress Test: 50 Cubes + Actions"))
        {
            StressTestCubesAndActions();
        }

        if (GUILayout.Button("Performance Test: Face Painting"))
        {
            PerformanceTestFacePainting();
        }

        if (GUILayout.Button("Edge Case: Grid Boundaries"))
        {
            TestGridBoundaries();
        }

        if (GUILayout.Button("Integration Test: Full Gameplay"))
        {
            IntegrationTestFullGameplay();
        }

        if (GUILayout.Button("Debug: Print All System States"))
        {
            DebugPrintAllStates();
        }

        GUILayout.EndVertical();
    }

    private void DrawActiveCubeFaceStatus()
    {
        GUILayout.Label("ACTIVE CUBES FACE STATUS", GUI.skin.box);

        var allCubes = Object.FindObjectsOfType<CubeManager>();
        int shownCubes = 0;

        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && shownCubes < 2)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y}):");

                // Show current down face and status
                CubeFace activeFace = cube.GetCurrentDownFace();
                FaceStatus activeStatus = cube.GetActiveFaceStatus();
                GUILayout.Label($"Active Face: {activeFace} ({activeStatus})");
                GUILayout.Label($"Effective Type: {cube.GetEffectiveType()}");
                GUILayout.Label($"Can Capture: {cube.CanBeCaptured()}");

                // Quick paint buttons for current face
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Paint Corrupt", GUILayout.Width(80)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.Corrupted, 5);
                }
                if (GUILayout.Button("Paint Enhance", GUILayout.Width(80)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.Enhanced, 5);
                }
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.None, 0);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                shownCubes++;
            }
        }

        if (shownCubes == 0)
        {
            GUILayout.Label("No active cubes found");
        }
    }

    // Test scenario implementations
    private void TestCorruptedFace()
    {
        Vector2Int testPos = new Vector2Int(2, 10);
        SetupTilePainting(testPos, FaceStatus.Corrupted, Color.red, 3);
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 1), CubeType.Normal);
        Debug.Log("Corrupted face test: Normal cube will act like black cube when painted");
    }

    private void TestEnhancedFace()
    {
        Vector2Int testPos = new Vector2Int(3, 10);
        SetupTilePainting(testPos, FaceStatus.Enhanced, Color.blue, 3);
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 1), CubeType.Normal);
        Debug.Log("Enhanced face test: Normal cube will create detonation when captured");
    }

    private void TestFaceRotationAndDuration()
    {
        Vector2Int testPos = new Vector2Int(1, 12);
        for (int i = 0; i < 3; i++)
        {
            FaceStatus status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            Color color = status == FaceStatus.Corrupted ? Color.red : Color.blue;
            SetupTilePainting(new Vector2Int(testPos.x, testPos.y + i), status, color, 2);
        }
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 4), CubeType.Normal);
        Debug.Log("Face rotation test: Cube will be painted multiple times with different durations");
    }

    private void TestMixedFaceStatuses()
    {
        Vector2Int testPos = new Vector2Int(0, 10);
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y), FaceStatus.Corrupted, Color.red, 3);
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y + 1), FaceStatus.Enhanced, Color.blue, 3);
        SetupTilePainting(new Vector2Int(testPos.x, testPos.y + 2), FaceStatus.Corrupted, Color.red, 2);
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 4), CubeType.Normal);
        Debug.Log("Mixed face test: Cube will accumulate different face paintings");
    }

    private void TestActionFaceCombo()
    {
        Vector2Int testPos = new Vector2Int(2, 8);

        // Set up face painting tile
        SetupTilePainting(testPos, FaceStatus.Enhanced, Color.blue, 5);

        // Spawn normal cube
        SpawnCubeAt(new Vector2Int(testPos.x, testPos.y + 2), CubeType.Normal);

        // Place action marker
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        if (actionManager != null)
        {
            actionManager.PlaceIndividualMarker(testPos);
        }

        Debug.Log("Action + Face combo test: Normal cube will be enhanced, then captured with marker");
    }

    private void StressTestCubesAndActions()
    {
        Debug.Log("Starting stress test...");

        // Spawn many cubes
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 15; y < gridManager.Height; y++)
            {
                CubeType type = (x + y) % 3 == 0 ? CubeType.Blue : CubeType.Normal;
                SpawnCubeAt(new Vector2Int(x, y), type);
            }
        }

        // Place many markers
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        if (actionManager != null)
        {
            for (int x = 0; x < gridManager.Width; x += 2)
            {
                actionManager.PlaceIndividualMarker(new Vector2Int(x, 10));
            }
        }

        Debug.Log("Stress test setup complete - watch performance!");
    }

    private void PerformanceTestFacePainting()
    {
        Debug.Log("Performance test: Setting up face painting on many tiles...");

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 5; y < 15; y++)
            {
                FaceStatus status = (x + y) % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                Color color = status == FaceStatus.Corrupted ? Color.red : Color.blue;
                SetupTilePainting(new Vector2Int(x, y), status, color, 10);
            }
        }

        Debug.Log("Performance test setup complete - monitor frame rate!");
    }

    private void TestGridBoundaries()
    {
        Debug.Log("Testing grid boundary conditions...");

        // Test at grid edges
        SpawnCubeAt(new Vector2Int(0, gridManager.Height - 1), CubeType.Normal);
        SpawnCubeAt(new Vector2Int(gridManager.Width - 1, gridManager.Height - 1), CubeType.Blue);

        // Test near bottom
        SpawnCubeAt(new Vector2Int(gridManager.Width / 2, 1), CubeType.Black);

        Debug.Log("Boundary test cubes spawned - watch for edge case handling!");
    }

    private void IntegrationTestFullGameplay()
    {
        Debug.Log("Setting up full gameplay integration test...");

        // Create a complete test scenario
        Vector2Int center = new Vector2Int(gridManager.Width / 2, 10);

        // Mixed cubes
        SpawnCubeAt(new Vector2Int(center.x - 1, center.y + 3), CubeType.Normal);
        SpawnCubeAt(new Vector2Int(center.x, center.y + 3), CubeType.Blue);
        SpawnCubeAt(new Vector2Int(center.x + 1, center.y + 3), CubeType.Black);

        // Face painting tiles
        SetupTilePainting(new Vector2Int(center.x - 1, center.y + 1), FaceStatus.Enhanced, Color.blue, 5);
        SetupTilePainting(new Vector2Int(center.x + 1, center.y + 1), FaceStatus.Corrupted, Color.red, 5);

        // Tile states
        Tile tile = gridManager.GetTileAt(center.x, center.y);
        tile?.PrimeTile();

        // Actions
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        if (actionManager != null)
        {
            actionManager.PlaceIndividualMarker(new Vector2Int(center.x - 1, center.y));
            actionManager.PlaceAreaMarker(center, 2);
        }

        Debug.Log("Integration test complete - all systems engaged!");
    }

    private void DebugPrintAllStates()
    {
        Debug.Log("=== COMPLETE SYSTEM STATE DEBUG ===");

        // Grid state
        if (gridManager != null)
        {
            Debug.Log($"Grid: {gridManager.Width}x{gridManager.Height}, Ready: {gridManager.IsGridReady}");
            Debug.Log($"Markers: {gridManager.GetMarkerCount()}, Playable Rows: {gridManager.GetPlayableRowCount()}");
        }

        // Cube states
        var cubes = Object.FindObjectsOfType<CubeManager>();
        Debug.Log($"Active Cubes: {cubes.Length}");
        foreach (var cube in cubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                Debug.Log($"  {cube.type} at ({cube.position.x},{cube.position.y}) - Face: {cube.GetCurrentDownFace()}, Status: {cube.GetActiveFaceStatus()}");
            }
        }

        // Action states
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        if (actionManager != null)
        {
            Debug.Log($"Actions - Individual: {actionManager.GetCurrentIndividualMarkers()}, Area: {actionManager.GetCurrentAreaMarkers()}, Cube: {actionManager.GetCurrentCubeMarkers()}");
        }

        Debug.Log("=== END SYSTEM STATE ===");
    }

    // Helper methods
    private void SetupTilePainting(Vector2Int position, FaceStatus status, Color color, int duration)
    {
        DebugTileHelper.SetupTilePainting(position, status, color, duration, gridManager, paintOnLanding, paintOnExit);
    }

    private void ClearTilePainting(Vector2Int position)
    {
        DebugTileHelper.ClearTilePainting(position, gridManager);
    }

    private void ClearAllTilePainting()
    {
        DebugTileHelper.ClearAllTilePainting(gridManager);
    }

    private void CreateFacePaintPattern()
    {
        Vector2Int center = playerManager?.currentTilePosition ?? Vector2Int.zero;
        DebugTileHelper.CreateFacePaintPattern(center, gridManager, playerManager);
    }

    private void SpawnCubeAt(Vector2Int position, CubeType cubeType)
    {
        DebugCubeSpawnHelper.SpawnCubeAt(position, cubeType, gridManager, waveManager);
    }
    
    private void DrawIntegrationTestsSection()
    {
        DebugUIHelpers.DrawSection("INTEGRATION TESTS", () => {
            GUILayout.Label("Cross-Manager Testing Scenarios:");
            
            // Manager coordination tests
            DebugUIHelpers.DrawButtonGrid(new (string, System.Action)[] {
                ("Stage + Wave Test", () => TestStageWaveCoordination()),
                ("Player + Grid Test", () => TestPlayerGridInteraction()),
                ("All Systems Test", () => TestAllSystemsIntegration())
            });
            
            DebugUIHelpers.Space();
            
            // System health check
            GUILayout.Label("System Health Check:");
            var stageManager = Object.FindObjectOfType<StageManager>();
            var waveManager = Object.FindObjectOfType<WaveManager>();
            var playerManager = Object.FindObjectOfType<PlayerManager>();
            var gridManager = GridManager.Instance;
            
            DebugUIHelpers.DrawStatusIndicator("Stage Manager", stageManager != null && stageManager.CurrentStage != null);
            DebugUIHelpers.DrawStatusIndicator("Wave Manager", waveManager != null);
            DebugUIHelpers.DrawStatusIndicator("Player Manager", playerManager != null && playerManager.IsAlive());
            DebugUIHelpers.DrawStatusIndicator("Grid Manager", gridManager != null && gridManager.IsGridReady);
            
            DebugUIHelpers.Space();
            
            // Quick system resets
            GUILayout.Label("System Resets:");
            DebugUIHelpers.DrawButtonGrid(new (string, System.Action)[] {
                ("Reset All Stats", () => ResetAllSystemStats()),
                ("Clear All Markers", () => ClearAllSystemMarkers()),
                ("Reset to Clean State", () => ResetToCleanGameState())
            });
        });
    }
    
    private void TestStageWaveCoordination()
    {
        var stageManager = Object.FindObjectOfType<StageManager>();
        
        if (stageManager != null && waveManager != null)
        {
            Debug.Log("Testing Stage-Wave coordination...");
            Debug.Log($"Current Stage: {stageManager.CurrentStageIndex}, Wave: {waveManager.CurrentWaveIndex}");
            Debug.Log($"Stage in progress: {stageManager.IsStageInProgress}, Wave active: {waveManager.waveActive}");
        }
    }
    
    private void TestPlayerGridInteraction()
    {
        if (playerManager != null && gridManager != null)
        {
            Debug.Log("Testing Player-Grid interaction...");
            var playerPos = playerManager.currentTilePosition;
            var tile = gridManager.GetTileAt(playerPos.x, playerPos.y);
            Debug.Log($"Player at ({playerPos.x}, {playerPos.y}), Tile playable: {tile?.IsPlayable}");
        }
    }
    
    private void TestAllSystemsIntegration()
    {
        Debug.Log("=== FULL SYSTEM INTEGRATION TEST ===");
        
        // Test all manager availability
        var stageManager = Object.FindObjectOfType<StageManager>();
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        
        Debug.Log($"Managers Found: Stage={stageManager != null}, Wave={waveManager != null}, Player={playerManager != null}, Grid={gridManager != null}, Actions={actionManager != null}");
        
        if (stageManager?.CurrentStage != null)
        {
            Debug.Log($"Current gameplay state: Stage {stageManager.CurrentStageIndex} ({stageManager.CurrentStage.stageName})");
        }
        
        // Test cross-system communication
        IntegrationTestFullGameplay();
    }
    
    private void ResetAllSystemStats()
    {
        playerManager?.ResetStatistics();
        Debug.Log("Reset all player statistics");
    }
    
    private void ClearAllSystemMarkers()
    {
        var actionManager = Object.FindObjectOfType<PlayerActionManager>();
        
        gridManager?.ClearAllMarkers();
        actionManager?.ClearAllActions();
        Debug.Log("Cleared all markers and actions");
    }
    
    private void ResetToCleanGameState()
    {
        var stageManager = Object.FindObjectOfType<StageManager>();
        
        // Clear cubes
        waveManager?.ClearAllCubes();
        
        // Reset markers and actions
        ClearAllSystemMarkers();
        
        // Reset player stats
        ResetAllSystemStats();
        
        Debug.Log("Reset to clean game state");
    }
}