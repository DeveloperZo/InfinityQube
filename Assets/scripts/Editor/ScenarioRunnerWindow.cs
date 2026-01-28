using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple editor window for loading and testing scenarios.
/// </summary>
public class ScenarioRunnerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ScenarioData> allScenarios = new List<ScenarioData>();
    private ScenarioData selectedScenario;
    
    // Filters
    private bool showKeystone = true;
    private bool showFeature = true;
    private bool showDemo = true;
    private string searchFilter = "";
    
    [MenuItem("Tools/Infinity Qube/Scenarios/Scenario Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScenarioRunnerWindow>("Scenarios");
        window.minSize = new Vector2(400, 300);
        window.RefreshScenarioList();
    }
    
    private void OnEnable()
    {
        RefreshScenarioList();
    }
    
    private void RefreshScenarioList()
    {
        allScenarios.Clear();
        
        var guids = AssetDatabase.FindAssets("t:ScenarioData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioData>(path);
            if (scenario != null)
            {
                allScenarios.Add(scenario);
            }
        }
        
        allScenarios = allScenarios
            .OrderBy(s => s.category)
            .ThenBy(s => s.priority)
            .ThenBy(s => s.scenarioName)
            .ToList();
    }
    
    private void OnGUI()
    {
        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshScenarioList();
        }
        if (GUILayout.Button("Create D001", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            ScenarioEditorTools.CreateD001Scenario();
            RefreshScenarioList();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{allScenarios.Count} scenarios", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        // Filters
        EditorGUILayout.BeginHorizontal();
        showKeystone = GUILayout.Toggle(showKeystone, "Keystone", EditorStyles.miniButton);
        showFeature = GUILayout.Toggle(showFeature, "Feature", EditorStyles.miniButton);
        showDemo = GUILayout.Toggle(showDemo, "Demo", EditorStyles.miniButton);
        EditorGUILayout.EndHorizontal();
        
        searchFilter = EditorGUILayout.TextField("Search", searchFilter);
        
        EditorGUILayout.Space();
        
        // Split view
        EditorGUILayout.BeginHorizontal();
        
        // Left panel - scenario list
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawScenarioList();
        EditorGUILayout.EndVertical();
        
        // Right panel - details
        EditorGUILayout.BeginVertical();
        DrawScenarioDetails();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawScenarioList()
    {
        EditorGUILayout.LabelField("Scenarios", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        var filteredScenarios = allScenarios.Where(s => ShouldShowScenario(s)).ToList();
        
        foreach (var scenario in filteredScenarios)
        {
            bool isSelected = scenario == selectedScenario;
            
            GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
            if (GUILayout.Button(scenario.scenarioName, 
                isSelected ? EditorStyles.boldLabel : EditorStyles.label))
            {
                selectedScenario = scenario;
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private bool ShouldShowScenario(ScenarioData scenario)
    {
        bool categoryMatch = scenario.category switch
        {
            ScenarioCategory.Keystone => showKeystone,
            ScenarioCategory.Feature => showFeature,
            ScenarioCategory.Demo => showDemo,
            _ => true
        };
        
        if (!categoryMatch) return false;
        
        if (!string.IsNullOrEmpty(searchFilter))
        {
            bool nameMatch = scenario.scenarioName.ToLower().Contains(searchFilter.ToLower());
            bool tagMatch = scenario.tags != null && scenario.tags.Any(t => t.ToLower().Contains(searchFilter.ToLower()));
            if (!nameMatch && !tagMatch) return false;
        }
        
        return true;
    }
    
    private void DrawScenarioDetails()
    {
        if (selectedScenario == null)
        {
            EditorGUILayout.HelpBox("Select a scenario to view details", MessageType.Info);
            return;
        }
        
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Name:", selectedScenario.scenarioName);
        EditorGUILayout.LabelField("Category:", selectedScenario.category.ToString());
        EditorGUILayout.LabelField("Scene:", selectedScenario.SceneName);
        
        if (!string.IsNullOrEmpty(selectedScenario.description))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Description:");
            EditorGUILayout.TextArea(selectedScenario.description, EditorStyles.wordWrappedLabel);
        }
        
        if (selectedScenario.tags != null && selectedScenario.tags.Count > 0)
        {
            EditorGUILayout.LabelField("Tags:", string.Join(", ", selectedScenario.tags));
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"End Condition: {selectedScenario.endCondition}");
        EditorGUILayout.LabelField($"Timeout: {selectedScenario.timeoutSeconds}s | Max Steps: {selectedScenario.maxWaveSteps}");
        
        // Commands
        if (selectedScenario.commands != null && selectedScenario.commands.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Commands ({selectedScenario.commands.Count}):", EditorStyles.boldLabel);
            foreach (var cmd in selectedScenario.commands)
            {
                EditorGUILayout.LabelField($"  Step {cmd.executeOnStep}: {cmd.type} - {cmd.description}");
            }
        }
        
        // Assertions
        if (selectedScenario.assertions != null && selectedScenario.assertions.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Assertions ({selectedScenario.assertions.Count}):", EditorStyles.boldLabel);
            foreach (var assertion in selectedScenario.assertions)
            {
                EditorGUILayout.LabelField($"  {assertion.description}");
            }
        }
        
        if (!string.IsNullOrEmpty(selectedScenario.expectedBehaviorNotes))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notes:");
            EditorGUILayout.TextArea(selectedScenario.expectedBehaviorNotes, EditorStyles.wordWrappedLabel);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Actions
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = Application.isPlaying;
        
        if (GUILayout.Button("▶ Load Scenario", GUILayout.Height(30)))
        {
            LoadSelectedScenario();
        }
        
        GUI.enabled = true;
        
        if (GUILayout.Button("Select Asset", GUILayout.Height(30), GUILayout.Width(80)))
        {
            Selection.activeObject = selectedScenario;
            EditorGUIUtility.PingObject(selectedScenario);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to load scenarios", MessageType.Warning);
        }
    }
    
    private void LoadSelectedScenario()
    {
        if (selectedScenario == null || !Application.isPlaying) return;
        
        // Get or create ScenarioLoader
        var loader = ScenarioLoader.Instance ?? Object.FindFirstObjectByType<ScenarioLoader>();
        if (loader == null)
        {
            var go = new GameObject("ScenarioLoader");
            loader = go.AddComponent<ScenarioLoader>();
        }
        
        // Load the scenario
        bool success = loader.LoadScenario(selectedScenario);
        if (success)
        {
            Debug.Log($"[ScenarioWindow] ✅ Loaded: {selectedScenario.scenarioName}");
        }
        else
        {
            Debug.LogError($"[ScenarioWindow] ❌ Failed to load: {selectedScenario.scenarioName}");
        }
    }
}
