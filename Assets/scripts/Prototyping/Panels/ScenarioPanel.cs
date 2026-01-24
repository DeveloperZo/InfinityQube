using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Scenario Panel - Load and manage test scenarios for rapid iteration.
/// Integrates with ScenarioLoader for reproducible testing.
/// </summary>
public class ScenarioPanel : PrototypingPanelBase
{
    public override string PanelName => "Scenarios";
    public override string PanelIcon => "S";
    public override PrototypingCategory Category => PrototypingCategory.Testing;
    public override int Priority => 10; // High priority - near Quick panel
    
    #region State
    
    private ScenarioLoader scenarioLoader;
    private List<ScenarioData> allScenarios = new List<ScenarioData>();
    private Dictionary<ScenarioCategory, List<ScenarioData>> scenariosByCategory = new Dictionary<ScenarioCategory, List<ScenarioData>>();
    
    // UI State
    private Vector2 scrollPosition;
    private ScenarioCategory selectedCategory = ScenarioCategory.Keystone;
    private string filterTag = "";
    private bool showKeystone = true;
    private bool showRegression = true;
    private bool showFeature = true;
    private bool showStress = false;
    private bool showQuickTest = true;
    private bool showScenarioDetails = false;
    private ScenarioData selectedScenario;
    
    // Styles
    private GUIStyle categoryButtonStyle;
    private GUIStyle scenarioButtonStyle;
    private GUIStyle activeScenarioStyle;
    private GUIStyle tagStyle;
    private bool stylesInitialized;
    
    #endregion
    
    #region Initialization
    
    public override void Initialize()
    {
        base.Initialize();
        RefreshScenarioLoader();
    }
    
    private void RefreshScenarioLoader()
    {
        scenarioLoader = ScenarioLoader.Instance;
        if (scenarioLoader == null)
        {
            scenarioLoader = Object.FindFirstObjectByType<ScenarioLoader>();
        }
        
        RefreshScenarioList();
    }
    
    private void RefreshScenarioList()
    {
        if (scenarioLoader == null)
        {
            // Try to load from Resources directly if no loader
            allScenarios = new List<ScenarioData>(Resources.LoadAll<ScenarioData>(""));
        }
        else
        {
            allScenarios = scenarioLoader.GetAllScenarios();
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
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        categoryButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        
        scenarioButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 4, 4, 4)
        };
        
        activeScenarioStyle = new GUIStyle(scenarioButtonStyle);
        activeScenarioStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.3f, 0.8f));
        
        tagStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Italic
        };
        tagStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        
        stylesInitialized = true;
    }
    
    #endregion
    
    #region GUI Drawing
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("Reload", () => ReloadLastScenario(), "↻")
            {
                Tooltip = "Reload last scenario (Shift+F5)",
                IsEnabled = () => scenarioLoader?.GetLastLoadedScenario() != null
            }
        };
    }
    
    public override void DrawGUI()
    {
        InitStyles();
        
        if (scenarioLoader == null)
        {
            RefreshScenarioLoader();
        }
        
        // Status bar
        DrawStatusBar();
        
        GUILayout.Space(5);
        
        // Quick Actions
        DrawQuickActions();
        
        GUILayout.Space(5);
        
        // Category Filters
        DrawCategoryFilters();
        
        GUILayout.Space(5);
        
        // Scenario List
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        DrawScenarioList();
        GUILayout.EndScrollView();
        
        // Selected Scenario Details
        if (showScenarioDetails && selectedScenario != null)
        {
            DrawScenarioDetails();
        }
    }
    
    private void DrawStatusBar()
    {
        var current = scenarioLoader?.GetCurrentScenario();
        var last = scenarioLoader?.GetLastLoadedScenario();
        
        GUILayout.BeginHorizontal(GUI.skin.box);
        
        if (current != null)
        {
            GUILayout.Label($"Active: {current.scenarioName}", GUILayout.ExpandWidth(true));
        }
        else if (last != null)
        {
            GUILayout.Label($"Last: {last.scenarioName} (Shift+F5 to reload)", GUILayout.ExpandWidth(true));
        }
        else
        {
            GUILayout.Label($"{allScenarios.Count} scenarios available", GUILayout.ExpandWidth(true));
        }
        
        if (GUILayout.Button("↻", GUILayout.Width(25)))
        {
            RefreshScenarioList();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawQuickActions()
    {
        GUILayout.BeginHorizontal();
        
        GUI.enabled = scenarioLoader?.GetLastLoadedScenario() != null;
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("↻ Reload Last (Shift+F5)", GUILayout.Height(28)))
        {
            ReloadLastScenario();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        if (GUILayout.Button("Clear Scene", GUILayout.Height(28), GUILayout.Width(90)))
        {
            ClearScene();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawCategoryFilters()
    {
        GUILayout.Label("Categories:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        
        DrawCategoryToggle(ScenarioCategory.Keystone, ref showKeystone, new Color(1f, 0.4f, 0.4f));
        DrawCategoryToggle(ScenarioCategory.Regression, ref showRegression, new Color(1f, 0.7f, 0.3f));
        DrawCategoryToggle(ScenarioCategory.Feature, ref showFeature, new Color(0.4f, 0.7f, 1f));
        
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        
        DrawCategoryToggle(ScenarioCategory.Stress, ref showStress, new Color(0.8f, 0.4f, 0.8f));
        DrawCategoryToggle(ScenarioCategory.QuickTest, ref showQuickTest, new Color(0.5f, 0.8f, 0.5f));
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    
    private void DrawCategoryToggle(ScenarioCategory category, ref bool isVisible, Color color)
    {
        int count = scenariosByCategory.ContainsKey(category) ? scenariosByCategory[category].Count : 0;
        
        GUI.backgroundColor = isVisible ? color : new Color(0.5f, 0.5f, 0.5f);
        if (GUILayout.Button($"{category} ({count})", categoryButtonStyle, GUILayout.Height(24)))
        {
            isVisible = !isVisible;
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawScenarioList()
    {
        bool anyDrawn = false;
        
        if (showKeystone) anyDrawn |= DrawCategorySection(ScenarioCategory.Keystone, new Color(1f, 0.4f, 0.4f));
        if (showRegression) anyDrawn |= DrawCategorySection(ScenarioCategory.Regression, new Color(1f, 0.7f, 0.3f));
        if (showFeature) anyDrawn |= DrawCategorySection(ScenarioCategory.Feature, new Color(0.4f, 0.7f, 1f));
        if (showStress) anyDrawn |= DrawCategorySection(ScenarioCategory.Stress, new Color(0.8f, 0.4f, 0.8f));
        if (showQuickTest) anyDrawn |= DrawCategorySection(ScenarioCategory.QuickTest, new Color(0.5f, 0.8f, 0.5f));
        
        if (!anyDrawn)
        {
            GUILayout.Label("No scenarios in selected categories.\nCreate scenarios via Assets > Create > Infinity Qube > Scenario Data");
        }
    }
    
    private bool DrawCategorySection(ScenarioCategory category, Color headerColor)
    {
        if (!scenariosByCategory.ContainsKey(category) || scenariosByCategory[category].Count == 0)
        {
            return false;
        }
        
        var scenarios = scenariosByCategory[category];
        
        // Category header
        var headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.textColor = headerColor;
        headerStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label($"── {category} ──", headerStyle);
        
        // Scenario buttons
        foreach (var scenario in scenarios)
        {
            DrawScenarioButton(scenario);
        }
        
        GUILayout.Space(5);
        return true;
    }
    
    private void DrawScenarioButton(ScenarioData scenario)
    {
        bool isActive = scenarioLoader?.GetCurrentScenario() == scenario;
        bool isLast = scenarioLoader?.GetLastLoadedScenario() == scenario;
        
        GUILayout.BeginHorizontal();
        
        // Main load button
        var style = isActive ? activeScenarioStyle : scenarioButtonStyle;
        string prefix = isActive ? "▶ " : isLast ? "◉ " : "  ";
        string label = $"{prefix}{scenario.scenarioName}";
        
        if (!string.IsNullOrEmpty(scenario.description))
        {
            label += $"\n  {TruncateText(scenario.description, 40)}";
        }
        
        if (GUILayout.Button(label, style, GUILayout.Height(scenario.description?.Length > 0 ? 38 : 24)))
        {
            LoadScenario(scenario);
        }
        
        // Info button
        GUI.backgroundColor = selectedScenario == scenario && showScenarioDetails ? Color.cyan : Color.white;
        if (GUILayout.Button("i", GUILayout.Width(22), GUILayout.Height(scenario.description?.Length > 0 ? 38 : 24)))
        {
            if (selectedScenario == scenario && showScenarioDetails)
            {
                showScenarioDetails = false;
            }
            else
            {
                selectedScenario = scenario;
                showScenarioDetails = true;
            }
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawScenarioDetails()
    {
        GUILayout.Space(5);
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label($"Details: {selectedScenario.scenarioName}", GUI.skin.box);
        
        if (!string.IsNullOrEmpty(selectedScenario.description))
        {
            GUILayout.Label(selectedScenario.description, GUI.skin.box);
        }
        
        GUILayout.Label(selectedScenario.GetSummary());
        
        // Tags
        if (selectedScenario.tags != null && selectedScenario.tags.Count > 0)
        {
            GUILayout.Label($"Tags: {string.Join(", ", selectedScenario.tags)}", tagStyle);
        }
        
        // Setup info
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Wave Cubes: {selectedScenario.waveCubes?.Count ?? 0}", GUILayout.Width(100));
        GUILayout.Label($"Player Cubes: {selectedScenario.playerCubes?.Count ?? 0}", GUILayout.Width(110));
        GUILayout.Label($"Markers: {selectedScenario.markers?.Count ?? 0}", GUILayout.Width(80));
        GUILayout.EndHorizontal();
        
        // Timing
        GUILayout.Label($"Time Scale: {selectedScenario.timeScale:F1}x | Start Wave: {selectedScenario.startWaveOnLoad} | Pause: {selectedScenario.pauseOnLoad}");
        
        // Validation info
        if (selectedScenario.hasValidation)
        {
            GUILayout.Label($"Expected: {selectedScenario.expectedCaptures} captures, {selectedScenario.expectedEscapes} escapes (max {selectedScenario.maxMoves} moves)");
            
            if (!string.IsNullOrEmpty(selectedScenario.expectedBehaviorNotes))
            {
                GUILayout.Label($"Notes: {selectedScenario.expectedBehaviorNotes}", tagStyle);
            }
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load This Scenario"))
        {
            LoadScenario(selectedScenario);
        }
        if (GUILayout.Button("Close", GUILayout.Width(60)))
        {
            showScenarioDetails = false;
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
    
    #endregion
    
    #region Actions
    
    private void LoadScenario(ScenarioData scenario)
    {
        if (scenarioLoader != null)
        {
            scenarioLoader.LoadScenario(scenario);
        }
        else
        {
            LogAction($"ScenarioLoader not available - cannot load {scenario.scenarioName}");
        }
    }
    
    private void ReloadLastScenario()
    {
        if (scenarioLoader != null)
        {
            scenarioLoader.ReloadLastScenario();
        }
    }
    
    private void ClearScene()
    {
        waveManager?.StopWave();
        waveManager?.ClearAllCubes();
        gridManager?.ClearAllMarkers();
        
        var actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        actionManager?.MarkerSystem?.ClearAllActions();
        actionManager?.MarkerSystem?.ClearPlayerCubes();
        
        Time.timeScale = 1f;
        LogAction("Cleared scene");
    }
    
    #endregion
    
    #region Keyboard Shortcuts
    
    public override void Update()
    {
        if (!IsVisible) return;
        
        // Quick number keys to load scenarios by index in current category
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) && !Input.GetKey(KeyCode.LeftControl))
            {
                LoadScenarioByIndex(i - 1);
            }
        }
    }
    
    private void LoadScenarioByIndex(int index)
    {
        // Get visible scenarios in order
        var visibleScenarios = new List<ScenarioData>();
        
        if (showKeystone && scenariosByCategory.ContainsKey(ScenarioCategory.Keystone))
            visibleScenarios.AddRange(scenariosByCategory[ScenarioCategory.Keystone]);
        if (showRegression && scenariosByCategory.ContainsKey(ScenarioCategory.Regression))
            visibleScenarios.AddRange(scenariosByCategory[ScenarioCategory.Regression]);
        if (showFeature && scenariosByCategory.ContainsKey(ScenarioCategory.Feature))
            visibleScenarios.AddRange(scenariosByCategory[ScenarioCategory.Feature]);
        if (showStress && scenariosByCategory.ContainsKey(ScenarioCategory.Stress))
            visibleScenarios.AddRange(scenariosByCategory[ScenarioCategory.Stress]);
        if (showQuickTest && scenariosByCategory.ContainsKey(ScenarioCategory.QuickTest))
            visibleScenarios.AddRange(scenariosByCategory[ScenarioCategory.QuickTest]);
        
        if (index < visibleScenarios.Count)
        {
            LoadScenario(visibleScenarios[index]);
        }
    }
    
    #endregion
    
    #region Utility
    
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\n", " ").Replace("\r", "");
        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }
    
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    
    #endregion
}
