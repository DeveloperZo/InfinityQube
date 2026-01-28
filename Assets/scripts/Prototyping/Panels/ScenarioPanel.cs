using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scenario Panel - Load and manage test scenarios.
/// Scenarios are scene-based: each scenario points to a scene that contains all setup.
/// </summary>
public class ScenarioPanel : PrototypingPanelBase
{
    public override string PanelName => "Scenarios";
    public override string PanelIcon => "S";
    public override PrototypingCategory Category => PrototypingCategory.Testing;
    public override int Priority => 10;
    
    #region State
    
    private ScenarioLoader scenarioLoader;
    private List<ScenarioData> allScenarios = new List<ScenarioData>();
    private Dictionary<ScenarioCategory, List<ScenarioData>> scenariosByCategory = new Dictionary<ScenarioCategory, List<ScenarioData>>();
    
    // UI State
    private Vector2 scrollPosition;
    private bool showKeystone = true;
    private bool showFeature = true;
    private bool showDemo = true;
    private ScenarioData selectedScenario;
    
    // Results
    private ScenarioData lastCompletedScenario;
    private bool lastPassed;
    private List<ScenarioRunner.AssertionResult> lastResults = new List<ScenarioRunner.AssertionResult>();
    
    // Styles
    private GUIStyle scenarioButtonStyle;
    private GUIStyle activeScenarioStyle;
    private bool stylesInitialized;
    
    #endregion
    
    #region Initialization
    
    public override void Initialize()
    {
        base.Initialize();
        RefreshScenarios();
        
        // Subscribe to completion events
        ScenarioRunner.OnScenarioCompleted += OnScenarioCompleted;
    }
    
    private void OnDestroy()
    {
        ScenarioRunner.OnScenarioCompleted -= OnScenarioCompleted;
    }
    
    private void RefreshScenarios()
    {
        scenarioLoader = ScenarioLoader.Instance ?? Object.FindFirstObjectByType<ScenarioLoader>();
        
        if (scenarioLoader != null)
        {
            allScenarios = scenarioLoader.GetAllScenarios();
        }
        else
        {
            allScenarios = new List<ScenarioData>(Resources.LoadAll<ScenarioData>("Scenarios"));
        }
        
        // Group by category
        scenariosByCategory.Clear();
        foreach (ScenarioCategory cat in System.Enum.GetValues(typeof(ScenarioCategory)))
        {
            scenariosByCategory[cat] = allScenarios
                .Where(s => s.category == cat)
                .OrderBy(s => s.priority)
                .ThenBy(s => s.scenarioName)
                .ToList();
        }
    }
    
    private void OnScenarioCompleted(ScenarioData scenario, bool passed, List<ScenarioRunner.AssertionResult> results)
    {
        lastCompletedScenario = scenario;
        lastPassed = passed;
        lastResults = results ?? new List<ScenarioRunner.AssertionResult>();
        
        LogAction($"Scenario {scenario?.scenarioName}: {(passed ? "PASSED" : "FAILED")}");
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        scenarioButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 4, 4, 4)
        };
        
        activeScenarioStyle = new GUIStyle(scenarioButtonStyle);
        activeScenarioStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.3f, 0.8f));
        
        stylesInitialized = true;
    }
    
    #endregion
    
    #region GUI Drawing
    
    public override void DrawGUI()
    {
        InitStyles();
        
        // Status
        DrawStatusBar();
        GUILayout.Space(5);
        
        // Filters
        GUILayout.BeginHorizontal();
        DrawToggle("Keystone", ref showKeystone, new Color(1f, 0.4f, 0.4f));
        DrawToggle("Feature", ref showFeature, new Color(0.4f, 0.7f, 1f));
        DrawToggle("Demo", ref showDemo, new Color(0.5f, 0.8f, 0.5f));
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Scenario List
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        DrawScenarioList();
        GUILayout.EndScrollView();
        
        // Results
        if (lastCompletedScenario != null)
        {
            DrawResults();
        }
    }
    
    private void DrawStatusBar()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        
        var current = scenarioLoader?.GetCurrentScenario();
        if (current != null)
        {
            GUILayout.Label($"Current: {current.scenarioName}");
        }
        else
        {
            GUILayout.Label($"{allScenarios.Count} scenarios");
        }
        
        if (GUILayout.Button("↻", GUILayout.Width(25)))
        {
            RefreshScenarios();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawToggle(string label, ref bool value, Color color)
    {
        GUI.backgroundColor = value ? color : Color.gray;
        if (GUILayout.Button(label, GUILayout.Height(24)))
        {
            value = !value;
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawScenarioList()
    {
        if (showKeystone) DrawCategory(ScenarioCategory.Keystone, new Color(1f, 0.4f, 0.4f));
        if (showFeature) DrawCategory(ScenarioCategory.Feature, new Color(0.4f, 0.7f, 1f));
        if (showDemo) DrawCategory(ScenarioCategory.Demo, new Color(0.5f, 0.8f, 0.5f));
    }
    
    private void DrawCategory(ScenarioCategory category, Color color)
    {
        if (!scenariosByCategory.ContainsKey(category) || scenariosByCategory[category].Count == 0)
            return;
        
        var headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.textColor = color;
        headerStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label($"── {category} ──", headerStyle);
        
        foreach (var scenario in scenariosByCategory[category])
        {
            DrawScenarioButton(scenario);
        }
        
        GUILayout.Space(5);
    }
    
    private void DrawScenarioButton(ScenarioData scenario)
    {
        bool isCurrent = scenarioLoader?.GetCurrentScenario() == scenario;
        bool isLastCompleted = lastCompletedScenario == scenario;
        
        string statusIcon = "";
        if (isLastCompleted)
        {
            statusIcon = lastPassed ? " ✅" : " ❌";
        }
        
        var style = isCurrent ? activeScenarioStyle : scenarioButtonStyle;
        string prefix = isCurrent ? "▶ " : "  ";
        string label = $"{prefix}{scenario.scenarioName}{statusIcon}";
        
        if (!string.IsNullOrEmpty(scenario.description))
        {
            label += $"\n  {Truncate(scenario.description, 45)}";
        }
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button(label, style, GUILayout.Height(scenario.description?.Length > 0 ? 38 : 24)))
        {
            LoadScenario(scenario);
        }
        
        // Info button
        GUI.backgroundColor = selectedScenario == scenario ? Color.cyan : Color.white;
        if (GUILayout.Button("i", GUILayout.Width(22), GUILayout.Height(scenario.description?.Length > 0 ? 38 : 24)))
        {
            selectedScenario = selectedScenario == scenario ? null : scenario;
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.EndHorizontal();
        
        // Details
        if (selectedScenario == scenario)
        {
            DrawScenarioDetails(scenario);
        }
    }
    
    private void DrawScenarioDetails(ScenarioData scenario)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label($"Scene: {scenario.SceneName}");
        GUILayout.Label($"End: {scenario.endCondition} | Timeout: {scenario.timeoutSeconds}s | Steps: {scenario.maxWaveSteps}");
        
        if (scenario.tags?.Count > 0)
        {
            GUILayout.Label($"Tags: {string.Join(", ", scenario.tags)}");
        }
        
        // Commands
        if (scenario.commands?.Count > 0)
        {
            GUILayout.Label($"Commands ({scenario.commands.Count}):");
            foreach (var cmd in scenario.commands)
            {
                GUILayout.Label($"  Step {cmd.executeOnStep}: {cmd.type} - {cmd.description}",
                    new GUIStyle(GUI.skin.label) { fontSize = 10 });
            }
        }
        
        if (scenario.assertions?.Count > 0)
        {
            GUILayout.Label($"Assertions: {scenario.assertions.Count}");
        }
        
        if (!string.IsNullOrEmpty(scenario.expectedBehaviorNotes))
        {
            GUILayout.Label(scenario.expectedBehaviorNotes, new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic });
        }
        
        GUILayout.EndVertical();
    }
    
    private void DrawResults()
    {
        GUILayout.Space(5);
        
        var color = lastPassed ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
        GUI.backgroundColor = color;
        GUILayout.BeginVertical(GUI.skin.box);
        GUI.backgroundColor = Color.white;
        
        GUILayout.Label($"{(lastPassed ? "✅ PASSED" : "❌ FAILED")}: {lastCompletedScenario.scenarioName}");
        
        if (lastResults.Count > 0)
        {
            int passed = lastResults.Count(r => r.passed);
            GUILayout.Label($"Assertions: {passed}/{lastResults.Count}");
            
            foreach (var result in lastResults)
            {
                string icon = result.passed ? "✅" : "❌";
                GUILayout.Label($"  {icon} {result.assertion.description}");
                if (!result.passed)
                {
                    GUILayout.Label($"      Expected: {result.assertion.expectedValue}, Got: {result.actualValue}");
                }
            }
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            lastCompletedScenario = null;
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
    
    #endregion
    
    #region Actions
    
    private void LoadScenario(ScenarioData scenario)
    {
        if (scenarioLoader == null)
        {
            var go = new GameObject("ScenarioLoader");
            scenarioLoader = go.AddComponent<ScenarioLoader>();
        }
        
        scenarioLoader.LoadScenario(scenario);
        LogAction($"Loading: {scenario.scenarioName}");
    }
    
    #endregion
    
    #region Utility
    
    private string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\n", " ");
        return text.Length <= max ? text : text.Substring(0, max - 3) + "...";
    }
    
    private Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
    
    #endregion
}
