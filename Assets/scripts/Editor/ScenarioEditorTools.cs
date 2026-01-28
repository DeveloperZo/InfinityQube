using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tools for creating scenario assets.
/// </summary>
public class ScenarioEditorTools : Editor
{
    private const string SCENARIOS_PATH = "Assets/Resources/Scenarios";
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Create D001 Scenario")]
    public static void CreateD001Scenario()
    {
        EnsureFolderExists();
        
        var scenario = ScriptableObject.CreateInstance<ScenarioData>();
        
        scenario.scenarioName = "D001_BasicPlayerDeath";
        scenario.category = ScenarioCategory.Keystone;
        scenario.description = "Basic death test: Player moves left, right, then center where cube hits.\n" +
                               "Player should die once and respawn.";
        scenario.priority = 10;
        scenario.tags = new List<string> { "death", "player", "keystone" };
        
        // Commands - player moves around before returning to death position
        scenario.commands = new List<ScenarioCommand>
        {
            ScenarioCommand.Move(0, new Vector2Int(2, 2), "Move left"),
            ScenarioCommand.Move(2, new Vector2Int(4, 2), "Move right"),
            ScenarioCommand.Move(4, new Vector2Int(3, 2), "Return to center - collision path")
        };
        
        scenario.timeoutSeconds = 30f;
        scenario.maxWaveSteps = 10;
        scenario.endCondition = ScenarioEndCondition.PlayerDeath;
        
        scenario.assertions = new List<ScenarioAssertion>
        {
            ScenarioAssertion.Equals(AssertionType.PlayerDeaths, 1, "Player should die exactly once")
        };
        
        scenario.featureDocRef = "3.4 Cube System - Player Death";
        scenario.expectedBehaviorNotes = "Scene has cube at (3,8) and player at (3,2).\n" +
                                          "Player moves: left → right → center.\n" +
                                          "Cube reaches player at step 6, causing death.";
        
        SaveScenario(scenario, "D001_BasicPlayerDeath");
        
        AssetDatabase.Refresh();
        Debug.Log("[ScenarioEditorTools] Created D001_BasicPlayerDeath scenario");
    }
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Open Logs Folder")]
    public static void OpenLogsFolder()
    {
        string logsPath = System.IO.Path.Combine(Application.persistentDataPath, "Logs", "Scenarios");
        if (!System.IO.Directory.Exists(logsPath))
        {
            System.IO.Directory.CreateDirectory(logsPath);
        }
        EditorUtility.RevealInFinder(logsPath);
    }
    
    private static void EnsureFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(SCENARIOS_PATH))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Scenarios");
        }
    }
    
    private static void SaveScenario(ScenarioData scenario, string fileName)
    {
        string path = $"{SCENARIOS_PATH}/{fileName}.asset";
        
        var existing = AssetDatabase.LoadAssetAtPath<ScenarioData>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(scenario, existing);
            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(scenario, path);
        }
        
        AssetDatabase.SaveAssets();
    }
}
