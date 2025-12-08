using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Collision Panel - Testing cube collision behaviors.
/// Enables easy simulation of all 16 collision combinations from the collision matrix.
/// Supports testing by type (Unit vs all, Matrix vs all, etc.) or individual collisions.
/// </summary>
public class CollisionPanel : PrototypingPanelBase
{
    public override string PanelName => "Collision";
    public override string PanelIcon => "C";
    public override PrototypingCategory Category => PrototypingCategory.Testing;
    public override int Priority => 15;
    
    #region Test Configuration
    
    // Player cube type selection
    private CubeType selectedPlayerType = CubeType.Unit;
    
    // Wave cube type selection (which types to spawn in wave)
    private bool spawnUnitWave = true;
    private bool spawnMatrixWave = false;
    private bool spawnRecursionWave = false;
    private bool spawnInfinityWave = false;
    
    // Test positioning - spacing accounts for Matrix 3x3 area effects
    private int testColumn = 2; // Which column to test in
    private int waveSpawnRow = -1; // -1 = auto (top of grid)
    private int playerSpawnRow = 2; // Where player cube starts
    private int spacing = 4; // Horizontal spacing - 4 prevents 3x3 overlap
    
    // Grid requirements for full collision testing
    private const int MIN_GRID_WIDTH = 18; // Need room for 4 cubes with spacing
    private const int MIN_GRID_HEIGHT = 25; // Need room for 4 rows of tests
    
    // Section toggles
    private bool showCollisionMatrix = true;
    private bool showQuickPresets = true;
    private bool showCustomTest = false;
    private bool showTestControls = true;
    
    // Test state
    private bool testActive = false;
    private string lastTestDescription = "";
    private int testsRun = 0;
    
    #endregion
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void DrawGUI()
    {
        DrawStatus($"Collision Testing | Tests Run: {testsRun}");
        
        GUILayout.Space(5);
        
        // Collision Matrix Quick Reference
        showCollisionMatrix = DrawToggleSection("COLLISION MATRIX", showCollisionMatrix);
        if (showCollisionMatrix)
        {
            DrawCollisionMatrixReference();
        }
        
        // Quick Presets (Type vs All)
        showQuickPresets = DrawToggleSection("QUICK PRESETS", showQuickPresets);
        if (showQuickPresets)
        {
            DrawQuickPresets();
        }
        
        // Custom Test Setup
        showCustomTest = DrawToggleSection("CUSTOM TEST", showCustomTest);
        if (showCustomTest)
        {
            DrawCustomTestSetup();
        }
        
        // Test Controls
        showTestControls = DrawToggleSection("TEST CONTROLS", showTestControls);
        if (showTestControls)
        {
            DrawTestControls();
        }
        
        // Last test result
        if (!string.IsNullOrEmpty(lastTestDescription))
        {
            GUILayout.Space(5);
            var style = new GUIStyle(GUI.skin.box);
            style.wordWrap = true;
            GUILayout.Label($"Last Test: {lastTestDescription}", style);
        }
    }
    
    #region Collision Matrix Reference
    
    private void DrawCollisionMatrixReference()
    {
        DrawSection("", () =>
        {
            GUILayout.Label("Click to test specific collision:");
            
            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("P\\W", GUILayout.Width(40));
            GUILayout.Label("Unit", GUILayout.Width(55));
            GUILayout.Label("Matrix", GUILayout.Width(55));
            GUILayout.Label("Recur", GUILayout.Width(55));
            GUILayout.Label("Inf", GUILayout.Width(45));
            GUILayout.EndHorizontal();
            
            // Matrix rows
            DrawMatrixRow(CubeType.Unit, "Unit");
            DrawMatrixRow(CubeType.Matrix, "Matrix");
            DrawMatrixRow(CubeType.Recursion, "Recur");
            DrawMatrixRow(CubeType.Infinity, "Inf");
            
            GUILayout.Space(5);
            GUILayout.Label("P = Player cube type, W = Wave cube type", GUI.skin.box);
        });
    }
    
    private void DrawMatrixRow(CubeType playerType, string rowLabel)
    {
        GUILayout.BeginHorizontal();
        
        GUI.backgroundColor = GetCubeColor(playerType);
        GUILayout.Label(rowLabel, GUILayout.Width(40));
        GUI.backgroundColor = Color.white;
        
        // Buttons for each wave type
        foreach (CubeType waveType in new[] { CubeType.Unit, CubeType.Matrix, CubeType.Recursion, CubeType.Infinity })
        {
            string tooltip = GetCollisionBehavior(playerType, waveType);
            GUI.backgroundColor = GetCubeColor(waveType);
            
            float width = waveType == CubeType.Infinity ? 45 : 55;
            if (GUILayout.Button(new GUIContent(GetCollisionIcon(playerType, waveType), tooltip), GUILayout.Width(width), GUILayout.Height(25)))
            {
                RunSingleCollisionTest(playerType, waveType);
            }
        }
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }
    
    private string GetCollisionIcon(CubeType playerType, CubeType waveType)
    {
        // Short text codes representing collision outcomes
        if (waveType == CubeType.Infinity)
        {
            return "FP"; // Face paint
        }
        
        if (playerType == CubeType.Infinity && waveType == CubeType.Unit)
        {
            return "WJ"; // Wave join
        }
        
        if (playerType == waveType)
        {
            // Same type matches
            return playerType switch
            {
                CubeType.Matrix => "3x3",
                CubeType.Recursion => "+",
                CubeType.Infinity => "RES",
                _ => "1"
            };
        }
        
        // Area effects
        if (playerType == CubeType.Unit && waveType == CubeType.Matrix)
            return "2x2";
        if (playerType == CubeType.Matrix && waveType == CubeType.Unit)
            return "2x2";
        if (playerType == CubeType.Unit && waveType == CubeType.Recursion)
            return "C3";
        if (playerType == CubeType.Recursion && waveType == CubeType.Unit)
            return "C3";
        if (playerType == CubeType.Recursion && waveType == CubeType.Matrix)
            return "1x3";
        if (playerType == CubeType.Matrix && waveType == CubeType.Recursion)
            return "2x2";
            
        return "1";
    }
    
    private string GetCollisionBehavior(CubeType playerType, CubeType waveType)
    {
        return (playerType, waveType) switch
        {
            (CubeType.Unit, CubeType.Unit) => "Standard capture",
            (CubeType.Unit, CubeType.Matrix) => "2x2 area capture",
            (CubeType.Unit, CubeType.Recursion) => "Column capture (3 cubes)",
            (CubeType.Unit, CubeType.Infinity) => "Face paint, Unit destroyed",
            
            (CubeType.Matrix, CubeType.Unit) => "2x2 area capture",
            (CubeType.Matrix, CubeType.Matrix) => "3x3 triggerable marker",
            (CubeType.Matrix, CubeType.Recursion) => "Degrading 2x2 marker",
            (CubeType.Matrix, CubeType.Infinity) => "Face paint, Matrix destroyed",
            
            (CubeType.Recursion, CubeType.Unit) => "Column capture (3 cubes)",
            (CubeType.Recursion, CubeType.Matrix) => "Auto 1x3 vertical marker",
            (CubeType.Recursion, CubeType.Recursion) => "Cross marker (5 tiles)",
            (CubeType.Recursion, CubeType.Infinity) => "Face paint, Recursion destroyed",
            
            (CubeType.Infinity, CubeType.Unit) => "Wave join (takes position)",
            (CubeType.Infinity, CubeType.Matrix) => "Face paint, continue up",
            (CubeType.Infinity, CubeType.Recursion) => "Face paint, continue up",
            (CubeType.Infinity, CubeType.Infinity) => "Face paint, resonance",
            
            _ => "Unknown collision"
        };
    }
    
    #endregion
    
    #region Quick Presets
    
    private void DrawQuickPresets()
    {
        DrawSection("", () =>
        {
            GUILayout.Label("Test Player Type vs All Wave Types:");
            
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = GetCubeColor(CubeType.Unit);
            if (GUILayout.Button("Unit vs All", GUILayout.Height(28)))
            {
                RunTypeVsAllTest(CubeType.Unit);
            }
            GUI.backgroundColor = GetCubeColor(CubeType.Matrix);
            if (GUILayout.Button("Matrix vs All", GUILayout.Height(28)))
            {
                RunTypeVsAllTest(CubeType.Matrix);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = GetCubeColor(CubeType.Recursion);
            if (GUILayout.Button("Recursion vs All", GUILayout.Height(28)))
            {
                RunTypeVsAllTest(CubeType.Recursion);
            }
            GUI.backgroundColor = GetCubeColor(CubeType.Infinity);
            if (GUILayout.Button("Infinity vs All", GUILayout.Height(28)))
            {
                RunTypeVsAllTest(CubeType.Infinity);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            GUILayout.Label("Special Tests:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Same-Type", GUILayout.Height(25)))
            {
                RunAllSameTypeTests();
            }
            if (GUILayout.Button("Area Effects", GUILayout.Height(25)))
            {
                RunAllAreaEffectTests();
            }
            if (GUILayout.Button("ALL 16", GUILayout.Height(25)))
            {
                RunAllCollisionTests();
            }
            GUILayout.EndHorizontal();
        });
    }
    
    #endregion
    
    #region Custom Test Setup
    
    private void DrawCustomTestSetup()
    {
        DrawSection("", () =>
        {
            // Player type selection
            GUILayout.Label("Player Cube Type:");
            GUILayout.BeginHorizontal();
            foreach (CubeType type in System.Enum.GetValues(typeof(CubeType)))
            {
                GUI.backgroundColor = selectedPlayerType == type ? GetCubeColor(type) : Color.gray;
                if (GUILayout.Button(type.ToString(), GUILayout.Height(28)))
                {
                    selectedPlayerType = type;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Wave types selection
            GUILayout.Label("Wave Cube Types:");
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = spawnUnitWave ? GetCubeColor(CubeType.Unit) : Color.gray;
            if (GUILayout.Button(spawnUnitWave ? "✓ Unit" : "Unit"))
            {
                spawnUnitWave = !spawnUnitWave;
            }
            
            GUI.backgroundColor = spawnMatrixWave ? GetCubeColor(CubeType.Matrix) : Color.gray;
            if (GUILayout.Button(spawnMatrixWave ? "✓ Matrix" : "Matrix"))
            {
                spawnMatrixWave = !spawnMatrixWave;
            }
            
            GUI.backgroundColor = spawnRecursionWave ? GetCubeColor(CubeType.Recursion) : Color.gray;
            if (GUILayout.Button(spawnRecursionWave ? "✓ Recur" : "Recur"))
            {
                spawnRecursionWave = !spawnRecursionWave;
            }
            
            GUI.backgroundColor = spawnInfinityWave ? GetCubeColor(CubeType.Infinity) : Color.gray;
            if (GUILayout.Button(spawnInfinityWave ? "✓ Inf" : "Inf"))
            {
                spawnInfinityWave = !spawnInfinityWave;
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Position settings
            testColumn = DrawIntStepper("Column", testColumn, 0, (gridManager?.Width ?? 10) - 1);
            playerSpawnRow = DrawIntStepper("P Row", playerSpawnRow, 0, 5);
            spacing = DrawIntStepper("Spacing", spacing, 1, 4);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Run Custom Test", GUILayout.Height(28)))
            {
                RunSelectedTest();
            }
        });
    }
    
    #endregion
    
    #region Test Controls
    
    private void DrawTestControls()
    {
        DrawSection("", () =>
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Height(26)))
            {
                ResetTest();
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(26)))
            {
                ClearAll();
            }
            if (GUILayout.Button("Step", GUILayout.Height(26)))
            {
                StepTest();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", GUILayout.Height(24)))
            {
                Time.timeScale = 0f;
            }
            if (GUILayout.Button("Resume", GUILayout.Height(24)))
            {
                Time.timeScale = 1f;
            }
            if (GUILayout.Button("0.25x", GUILayout.Height(24)))
            {
                Time.timeScale = 0.25f;
            }
            GUILayout.EndHorizontal();
            
            // Active cubes info
            int activeCubes = waveManager?.activeCubes?.Count(c => c != null && !c.isDestroyed) ?? 0;
            int playerCubes = GetPlayerCubeCount();
            GUILayout.Label($"Active: {activeCubes} wave, {playerCubes} player cubes");
        });
    }
    
    #endregion
    
    #region Test Execution
    
    private void RunSingleCollisionTest(CubeType playerType, CubeType waveType)
    {
        ClearAll();
        
        // Setup test
        int col = testColumn;
        int waveRow = gridManager?.Height - 2 ?? 23;
        
        // Spawn wave cube at top
        SpawnWaveCube(col, waveRow, waveType);
        
        // Spawn player cube below
        SpawnPlayerCube(col, playerSpawnRow, playerType);
        
        // Start wave movement
        StartWaveMovement();
        
        testsRun++;
        lastTestDescription = $"{playerType} vs {waveType}: {GetCollisionBehavior(playerType, waveType)}";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    private void RunTypeVsAllTest(CubeType playerType)
    {
        ClearAll();
        EnsureGridSize();
        
        int gridWidth = gridManager?.Width ?? MIN_GRID_WIDTH;
        int waveRow = gridManager?.Height - 2 ?? 23;
        int col = 1;
        
        // Spawn all four wave cube types in a row
        CubeType[] waveTypes = { CubeType.Unit, CubeType.Matrix, CubeType.Recursion, CubeType.Infinity };
        
        foreach (var waveType in waveTypes)
        {
            if (col < gridWidth - 2)
            {
                // Spawn wave cube at top
                SpawnWaveCube(col, waveRow, waveType);
                
                // Spawn player cube of same type below
                SpawnPlayerCube(col, playerSpawnRow, playerType);
                
                col += spacing; // Use spacing to prevent area effect overlap
            }
        }
        
        StartWaveMovement();
        
        testsRun++;
        lastTestDescription = $"{playerType} vs ALL: Testing 4 collision types";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    private void RunAllSameTypeTests()
    {
        ClearAll();
        EnsureGridSize();
        
        int waveRow = gridManager?.Height - 2 ?? 23;
        int col = 1;
        
        CubeType[] types = { CubeType.Unit, CubeType.Matrix, CubeType.Recursion, CubeType.Infinity };
        
        foreach (var type in types)
        {
            SpawnWaveCube(col, waveRow, type);
            SpawnPlayerCube(col, playerSpawnRow, type);
            col += spacing;
        }
        
        StartWaveMovement();
        
        testsRun++;
        lastTestDescription = "Same-Type: U/U, M/M, R/R, I/I";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    private void RunAllAreaEffectTests()
    {
        ClearAll();
        EnsureGridSize();
        
        int waveRow = gridManager?.Height - 2 ?? 23;
        
        // Test area effect collisions
        var areaTests = new (CubeType player, CubeType wave)[]
        {
            (CubeType.Unit, CubeType.Matrix),    // 2x2 area
            (CubeType.Matrix, CubeType.Unit),    // 2x2 area
            (CubeType.Matrix, CubeType.Matrix),  // 3x3 area
            (CubeType.Recursion, CubeType.Recursion) // Cross pattern
        };
        
        int col = 1;
        foreach (var (player, wave) in areaTests)
        {
            SpawnWaveCube(col, waveRow, wave);
            SpawnPlayerCube(col, playerSpawnRow, player);
            col += spacing;
        }
        
        StartWaveMovement();
        
        testsRun++;
        lastTestDescription = "Area Effects: 2x2, 2x2, 3x3, Cross";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    private void RunAllCollisionTests()
    {
        ClearAll();
        EnsureGridSize();
        
        int waveRow = gridManager?.Height - 2 ?? 23;
        CubeType[] types = { CubeType.Unit, CubeType.Matrix, CubeType.Recursion, CubeType.Infinity };
        
        // 4 rows, one for each player type
        // Each row has 4 wave cubes (one of each type)
        // This tests all 16 collision combinations
        int rowSpacing = 5; // Vertical spacing between rows for wave to travel
        
        for (int rowIndex = 0; rowIndex < types.Length; rowIndex++)
        {
            CubeType playerType = types[rowIndex];
            int rowWaveY = waveRow - (rowIndex * rowSpacing);
            int rowPlayerY = playerSpawnRow;
            
            // Make sure wave row is above player row
            if (rowWaveY <= rowPlayerY + 3) continue;
            
            int col = 1;
            foreach (var waveType in types)
            {
                // Spawn wave cube at this row
                SpawnWaveCube(col, rowWaveY, waveType);
                
                // Spawn player cube below
                SpawnPlayerCube(col, rowPlayerY, playerType);
                
                col += spacing;
            }
        }
        
        StartWaveMovement();
        
        testsRun++;
        lastTestDescription = "ALL 16: 4 rows x 4 columns (U/M/R/I vs U/M/R/I)";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    /// <summary>
    /// Ensures grid is large enough for collision testing
    /// </summary>
    private void EnsureGridSize()
    {
        if (gridManager == null) return;
        
        bool needsResize = false;
        int newWidth = gridManager.Width;
        int newHeight = gridManager.Height;
        
        if (gridManager.Width < MIN_GRID_WIDTH)
        {
            newWidth = MIN_GRID_WIDTH;
            needsResize = true;
        }
        
        if (gridManager.Height < MIN_GRID_HEIGHT)
        {
            newHeight = MIN_GRID_HEIGHT;
            needsResize = true;
        }
        
        if (needsResize)
        {
            gridManager.ResizeGrid(newWidth, newHeight);
            LogAction($"Resized grid to {newWidth}x{newHeight} for collision testing");
        }
    }
    
    private void RunSelectedTest()
    {
        ClearAll();
        EnsureGridSize();
        
        int waveRow = gridManager?.Height - 2 ?? 23;
        int col = testColumn;
        
        // Spawn selected wave types
        List<CubeType> selectedWaveTypes = new List<CubeType>();
        if (spawnUnitWave) selectedWaveTypes.Add(CubeType.Unit);
        if (spawnMatrixWave) selectedWaveTypes.Add(CubeType.Matrix);
        if (spawnRecursionWave) selectedWaveTypes.Add(CubeType.Recursion);
        if (spawnInfinityWave) selectedWaveTypes.Add(CubeType.Infinity);
        
        if (selectedWaveTypes.Count == 0)
        {
            LogAction("Select at least one wave cube type!");
            return;
        }
        
        foreach (var waveType in selectedWaveTypes)
        {
            SpawnWaveCube(col, waveRow, waveType);
            SpawnPlayerCube(col, playerSpawnRow, selectedPlayerType);
            col += spacing + 1;
        }
        
        StartWaveMovement();
        
        testsRun++;
        string waveTypesStr = string.Join(", ", selectedWaveTypes.Select(t => t.ToString()));
        lastTestDescription = $"{selectedPlayerType} vs [{waveTypesStr}]";
        LogAction($"Testing: {lastTestDescription}");
    }
    
    #endregion
    
    #region Cube Spawning
    
    private void SpawnWaveCube(int x, int y, CubeType type)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int typeIndex = (int)type;
        if (typeIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[typeIndex] == null)
        {
            Debug.LogWarning($"[CollisionPanel] No prefab for cube type {type}");
            return;
        }
        
        var pos = new Vector2Int(x, y);
        if (!gridManager.IsValidGridPosition(pos)) return;
        
        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 2f);
        var cubeObj = Object.Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        cube.Init(gridManager, new CubeData { type = type, position = pos, level = 1 }, 2f);
        cube.isPlayerCube = false;
        waveManager.activeCubes.Add(cube);
        
        Debug.Log($"[CollisionPanel] Spawned wave {type} cube at ({x}, {y})");
    }
    
    private void SpawnPlayerCube(int x, int y, CubeType type)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        var playerActionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        if (playerActionManager?.MarkerSystem == null)
        {
            Debug.LogWarning("[CollisionPanel] PlayerActionManager or MarkerSystem not found");
            return;
        }
        
        int typeIndex = (int)type;
        if (typeIndex >= waveManager.cubePrefabs.Length || waveManager.cubePrefabs[typeIndex] == null)
        {
            Debug.LogWarning($"[CollisionPanel] No prefab for cube type {type}");
            return;
        }
        
        var pos = new Vector2Int(x, y);
        if (!gridManager.IsValidGridPosition(pos)) return;
        
        Vector3 worldPos = gridManager.GridToWorldPosition(x, y, 2f);
        var cubeObj = Object.Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        cube.Init(gridManager, new CubeData { type = type, position = pos, level = 1 }, 2f);
        cube.isPlayerCube = true;
        cube.isMatrixCube = type == CubeType.Matrix;
        cube.usePhysics = false;
        cube.ConfigurePlayerCubePhysics();
        
        // Make translucent
        MakeCubeTranslucent(cube);
        
        playerActionManager.MarkerSystem.playerCubes.Add(cube);
        
        Debug.Log($"[CollisionPanel] Spawned player {type} cube at ({x}, {y})");
    }
    
    private void MakeCubeTranslucent(CubeManager cube)
    {
        if (cube == null) return;
        
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer == null) return;
        
        Material originalMaterial = renderer.material;
        if (originalMaterial == null) return;
        
        Material translucentMaterial = new Material(originalMaterial);
        Color color = translucentMaterial.color;
        color.a = 0.35f;
        translucentMaterial.color = color;
        
        if (translucentMaterial.HasProperty("_Mode"))
        {
            translucentMaterial.SetFloat("_Mode", 3);
            translucentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            translucentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            translucentMaterial.SetInt("_ZWrite", 0);
            translucentMaterial.DisableKeyword("_ALPHATEST_ON");
            translucentMaterial.EnableKeyword("_ALPHABLEND_ON");
            translucentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            translucentMaterial.renderQueue = 3000;
        }
        
        renderer.material = translucentMaterial;
    }
    
    #endregion
    
    #region Test Controls Implementation
    
    private void StartWaveMovement()
    {
        if (waveManager == null) return;
        
        testActive = true;
        Time.timeScale = 0.5f; // Slower for observation
        waveManager.StartWaveWithoutSpawning();
    }
    
    private void ResetTest()
    {
        // Re-run the last test configuration
        if (!string.IsNullOrEmpty(lastTestDescription))
        {
            RunSelectedTest();
        }
        else
        {
            ClearAll();
        }
    }
    
    private void ClearAll()
    {
        testActive = false;
        Time.timeScale = 1f;
        
        // Clear wave cubes
        waveManager?.StopWave();
        waveManager?.ClearAllCubes();
        
        // Clear player cubes
        var playerActionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        playerActionManager?.MarkerSystem?.ClearPlayerCubes();
        
        // Clear markers
        gridManager?.ClearAllMarkers();
        
        LogAction("Cleared all test cubes and markers");
    }
    
    private void StepTest()
    {
        if (waveManager == null) return;
        
        // Pause and step
        Time.timeScale = 0f;
        waveManager.ManualMoveWaveForward();
        
        // Also move player cubes backward
        var playerActionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        playerActionManager?.MarkerSystem?.MovePlayerCubesBackward();
        playerActionManager?.MarkerSystem?.CheckPlayerCubeCollisions();
    }
    
    private int GetPlayerCubeCount()
    {
        var playerActionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        return playerActionManager?.MarkerSystem?.playerCubes?.Count(c => c != null && !c.isDestroyed) ?? 0;
    }
    
    #endregion
    
    #region Utility
    
    private Color GetCubeColor(CubeType type)
    {
        return type switch
        {
            CubeType.Unit => new Color(0.8f, 0.5f, 0.2f),       // Orange
            CubeType.Matrix => new Color(0.2f, 0.5f, 0.8f),     // Blue
            CubeType.Recursion => new Color(0.6f, 0.2f, 0.6f),  // Purple
            CubeType.Infinity => new Color(0.15f, 0.15f, 0.2f), // Dark
            _ => Color.white
        };
    }
    
    #endregion
}

