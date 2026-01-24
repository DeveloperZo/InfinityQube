using UnityEngine;
using UnityEditor;
using System.IO;
using static Enumerations;

/// <summary>
/// Editor tools for the scenario system.
/// Creates sample scenarios and provides utilities for scenario management.
/// </summary>
public class ScenarioEditorTools : Editor
{
    // Using Resources folder so scenarios can be loaded at runtime via Resources.LoadAll
    private const string SCENARIOS_PATH = "Assets/Resources/Scenarios";
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Create Sample Scenarios")]
    public static void CreateSampleScenarios()
    {
        EnsureScenariosFolderExists();
        
        // Create Keystone scenarios
        CreateScenario_UnitVsUnit();
        CreateScenario_MatrixCollision();
        CreateScenario_RecursionMechanics();
        
        // Create QuickTest scenarios
        CreateScenario_FullRow();
        CreateScenario_EmptyGrid();
        
        AssetDatabase.Refresh();
        Debug.Log("[ScenarioEditorTools] Sample scenarios created in " + SCENARIOS_PATH);
    }
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Open Scenarios Folder")]
    public static void OpenScenariosFolder()
    {
        EnsureScenariosFolderExists();
        var folder = AssetDatabase.LoadAssetAtPath<Object>(SCENARIOS_PATH);
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Create Empty Scenario")]
    public static void CreateEmptyScenario()
    {
        EnsureScenariosFolderExists();
        
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "New Scenario";
        scenario.category = ScenarioCategory.QuickTest;
        scenario.description = "Describe what this scenario tests";
        
        string path = AssetDatabase.GenerateUniqueAssetPath($"{SCENARIOS_PATH}/NewScenario.asset");
        AssetDatabase.CreateAsset(scenario, path);
        AssetDatabase.SaveAssets();
        
        Selection.activeObject = scenario;
        EditorGUIUtility.PingObject(scenario);
        
        Debug.Log($"[ScenarioEditorTools] Created new scenario at {path}");
    }
    
    private static void EnsureScenariosFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(SCENARIOS_PATH))
        {
            // Create nested folders in Resources so scenarios are loadable at runtime
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder("Assets/Resources", "Scenarios");
            Debug.Log("[ScenarioEditorTools] Created Resources/Scenarios folder");
        }
    }
    
    #region Sample Scenario Creators
    
    private static void CreateScenario_UnitVsUnit()
    {
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "Unit vs Unit Collision";
        scenario.category = ScenarioCategory.Keystone;
        scenario.description = "Basic collision: Player Unit cube meets Wave Unit cube head-on.";
        scenario.tags.Add("collision");
        scenario.tags.Add("unit");
        scenario.priority = 10;
        
        // Grid setup
        scenario.clearExistingCubes = true;
        scenario.clearExistingMarkers = true;
        
        // Player position
        scenario.resetPlayerPosition = true;
        scenario.playerPosition = new Vector2Int(3, 0);
        
        // Wave cube at top center
        scenario.waveCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Unit,
            position = new Vector2Int(3, 15),
            level = 1
        });
        
        // Player cube below player
        scenario.playerCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Unit,
            position = new Vector2Int(3, 2),
            level = 1
        });
        
        // Timing
        scenario.timeScale = 0.5f;
        scenario.startWaveOnLoad = true;
        scenario.pauseOnLoad = false;
        
        // Validation
        scenario.hasValidation = true;
        scenario.expectedCaptures = 1;
        scenario.expectedEscapes = 0;
        scenario.maxMoves = 15;
        scenario.expectedBehaviorNotes = "Player cube should capture wave cube on collision.";
        
        string path = $"{SCENARIOS_PATH}/Keystone_UnitVsUnit.asset";
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
    
    private static void CreateScenario_MatrixCollision()
    {
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "Matrix 3x3 Pattern";
        scenario.category = ScenarioCategory.Keystone;
        scenario.description = "Matrix cube collision testing 3x3 area effect.";
        scenario.tags.Add("collision");
        scenario.tags.Add("matrix");
        scenario.tags.Add("area");
        scenario.priority = 20;
        
        scenario.clearExistingCubes = true;
        scenario.clearExistingMarkers = true;
        scenario.resetPlayerPosition = true;
        scenario.playerPosition = new Vector2Int(3, 0);
        
        // Wave cubes in a 3x3 pattern
        for (int x = 2; x <= 4; x++)
        {
            for (int y = 14; y <= 16; y++)
            {
                scenario.waveCubes.Add(new ScenarioCubePlacement
                {
                    type = CubeType.Unit,
                    position = new Vector2Int(x, y),
                    level = 1
                });
            }
        }
        
        // Player Matrix cube
        scenario.playerCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Matrix,
            position = new Vector2Int(3, 10),
            level = 1
        });
        
        scenario.timeScale = 0.5f;
        scenario.startWaveOnLoad = true;
        
        scenario.hasValidation = true;
        scenario.expectedCaptures = 9;
        scenario.maxMoves = 10;
        scenario.expectedBehaviorNotes = "Matrix cube should capture all 9 unit cubes in 3x3 area.";
        
        string path = $"{SCENARIOS_PATH}/Keystone_MatrixCollision.asset";
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
    
    private static void CreateScenario_RecursionMechanics()
    {
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "Recursion Multi-Hit";
        scenario.category = ScenarioCategory.Keystone;
        scenario.description = "Recursion cube collision mechanics - multiple hits required.";
        scenario.tags.Add("collision");
        scenario.tags.Add("recursion");
        scenario.priority = 30;
        
        scenario.clearExistingCubes = true;
        scenario.clearExistingMarkers = true;
        scenario.resetPlayerPosition = true;
        scenario.playerPosition = new Vector2Int(3, 0);
        
        // Single Recursion wave cube
        scenario.waveCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Recursion,
            position = new Vector2Int(3, 15),
            level = 1
        });
        
        // Multiple player cubes in a column
        scenario.playerCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Recursion,
            position = new Vector2Int(3, 2),
            level = 1
        });
        scenario.playerCubes.Add(new ScenarioCubePlacement
        {
            type = CubeType.Unit,
            position = new Vector2Int(3, 4),
            level = 1
        });
        
        scenario.timeScale = 0.5f;
        scenario.startWaveOnLoad = true;
        
        scenario.hasValidation = true;
        scenario.expectedCaptures = 1;
        scenario.maxMoves = 20;
        scenario.expectedBehaviorNotes = "Recursion wave cube requires multiple hits to capture.";
        
        string path = $"{SCENARIOS_PATH}/Keystone_RecursionMechanics.asset";
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
    
    private static void CreateScenario_FullRow()
    {
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "Full Row Spawn";
        scenario.category = ScenarioCategory.QuickTest;
        scenario.description = "Quick test: Full row of wave cubes for testing marker placement.";
        scenario.tags.Add("quick");
        scenario.tags.Add("stress");
        scenario.priority = 10;
        
        scenario.clearExistingCubes = true;
        scenario.clearExistingMarkers = true;
        scenario.resetPlayerPosition = true;
        scenario.playerPosition = new Vector2Int(3, 0);
        
        // Full row at top
        for (int x = 0; x < 6; x++)
        {
            scenario.waveCubes.Add(new ScenarioCubePlacement
            {
                type = CubeType.Unit,
                position = new Vector2Int(x, 18),
                level = 1
            });
        }
        
        // Give player plenty of markers
        scenario.unitMarkerCharges = 10;
        scenario.matrixMarkerCharges = 5;
        scenario.recursionMarkerCharges = 5;
        
        scenario.timeScale = 1f;
        scenario.startWaveOnLoad = true;
        
        string path = $"{SCENARIOS_PATH}/QuickTest_FullRow.asset";
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
    
    private static void CreateScenario_EmptyGrid()
    {
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.scenarioName = "Empty Grid";
        scenario.category = ScenarioCategory.QuickTest;
        scenario.description = "Clean slate: Empty grid for manual testing and experimentation.";
        scenario.tags.Add("quick");
        scenario.tags.Add("sandbox");
        scenario.priority = 5;
        
        scenario.clearExistingCubes = true;
        scenario.clearExistingMarkers = true;
        scenario.resetPlayerPosition = true;
        scenario.playerPosition = new Vector2Int(3, 0);
        
        // Give player unlimited markers for sandbox testing
        scenario.unitMarkerCharges = 99;
        scenario.matrixMarkerCharges = 99;
        scenario.recursionMarkerCharges = 99;
        scenario.infinityMarkerCharges = 99;
        
        scenario.timeScale = 1f;
        scenario.startWaveOnLoad = false; // Don't start wave - sandbox mode
        scenario.pauseOnLoad = false;
        
        string path = $"{SCENARIOS_PATH}/QuickTest_EmptyGrid.asset";
        if (!File.Exists(path))
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
    
    #endregion
}
