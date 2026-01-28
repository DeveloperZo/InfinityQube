using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scenario loader - auto-runs assigned scenario when scene loads.
/// Place in scene with a ScenarioData reference to auto-execute on Start.
/// </summary>
public class ScenarioLoader : MonoBehaviour
{
    #region Singleton
    
    public static ScenarioLoader Instance { get; private set; }
    
    #endregion
    
    #region Inspector
    
    [Header("Auto-Run Configuration")]
    [Tooltip("Scenario to auto-run when scene loads (if set)")]
    [SerializeField] private ScenarioData autoRunScenario;
    
    [Tooltip("Delay before starting scenario (for manager initialization)")]
    [SerializeField] private float startDelay = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region State
    
    private ScenarioData currentScenario;
    private List<ScenarioData> scenarios = new List<ScenarioData>();
    
    #endregion
    
    #region Events
    
    public event System.Action<ScenarioData> OnScenarioLoaded;
    public event System.Action<ScenarioData, string> OnScenarioLoadFailed;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        RefreshScenarioList();
        Log($"ScenarioLoader initialized with {scenarios.Count} scenarios");
        
        // Auto-run if scenario is assigned
        if (autoRunScenario != null)
        {
            Log($"Auto-running scenario: {autoRunScenario.scenarioName}");
            StartCoroutine(AutoRunAfterDelay());
        }
    }
    
    private IEnumerator AutoRunAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        RunScenario(autoRunScenario);
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Run a scenario (starts logging and fires event for ScenarioRunner to pick up)
    /// </summary>
    public bool RunScenario(ScenarioData scenario)
    {
        if (scenario == null)
        {
            LogWarning("Cannot run null scenario");
            return false;
        }
        
        Log($"Running scenario: {scenario.scenarioName}");
        
        // Start logging for this scenario
        ScenarioLogger.StartScenario(scenario.scenarioName);
        
        currentScenario = scenario;
        
        // Fire event for ScenarioRunner to pick up
        OnScenarioLoaded?.Invoke(scenario);
        
        return true;
    }
    
    /// <summary>
    /// Load a scenario (switches scene if needed, then runs)
    /// </summary>
    public bool LoadScenario(ScenarioData scenario)
    {
        if (scenario == null)
        {
            LogWarning("Cannot load null scenario");
            return false;
        }
        
        string sceneName = scenario.SceneName;
        
        // If no scene specified or already in scene, just run
        if (string.IsNullOrEmpty(sceneName) || SceneManager.GetActiveScene().name == sceneName)
        {
            return RunScenario(scenario);
        }
        
        Log($"Loading scene: {sceneName}");
        
        currentScenario = scenario;
        StartCoroutine(LoadSceneAndRun(scenario, sceneName));
        return true;
    }
    
    /// <summary>
    /// Load scenario by name
    /// </summary>
    public bool LoadScenarioByName(string name)
    {
        var scenario = scenarios.Find(s => s.scenarioName == name || s.name == name);
        if (scenario == null)
        {
            LogError($"Scenario not found: {name}");
            return false;
        }
        return LoadScenario(scenario);
    }
    
    /// <summary>
    /// Get current loaded scenario
    /// </summary>
    public ScenarioData GetCurrentScenario() => currentScenario;
    
    /// <summary>
    /// Get all available scenarios
    /// </summary>
    public List<ScenarioData> GetAllScenarios() => new List<ScenarioData>(scenarios);
    
    /// <summary>
    /// Refresh scenario list from Resources
    /// </summary>
    public void RefreshScenarioList()
    {
        scenarios.Clear();
        
#if UNITY_EDITOR
        // In editor, find all ScenarioData assets
        var guids = AssetDatabase.FindAssets("t:ScenarioData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioData>(path);
            if (scenario != null)
            {
                scenarios.Add(scenario);
            }
        }
        Log($"[Editor] Found {scenarios.Count} scenarios");
#else
        // In builds, load from Resources
        var loaded = Resources.LoadAll<ScenarioData>("Scenarios");
        scenarios.AddRange(loaded);
        Log($"[Runtime] Loaded {scenarios.Count} scenarios from Resources");
#endif
        
        // Sort by priority
        scenarios = scenarios.OrderBy(s => s.priority).ToList();
    }
    
    #endregion
    
    #region Private Methods
    
    private IEnumerator LoadSceneAndRun(ScenarioData scenario, string sceneName)
    {
        var asyncOp = SceneManager.LoadSceneAsync(sceneName);
        if (asyncOp == null)
        {
            LogError($"Failed to load scene: {sceneName}");
            OnScenarioLoadFailed?.Invoke(scenario, $"Scene '{sceneName}' not found");
            yield break;
        }
        
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        
        Log($"Scene loaded: {sceneName}");
        
        // Wait a frame for scene to initialize
        yield return null;
        
        RunScenario(scenario);
    }
    
    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[ScenarioLoader] {message}");
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ScenarioLoader] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ScenarioLoader] {message}");
    }
    
    #endregion
}
