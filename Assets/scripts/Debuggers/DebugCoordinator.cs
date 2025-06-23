using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lightweight coordinator for cross-system debug operations and scenario management.
/// Orchestrates complex multi-manager scenarios and provides unified debug interfaces.
/// </summary>
public class DebugCoordinator : MonoBehaviour
{
    #region Singleton Pattern
    public static DebugCoordinator Instance { get; private set; }
    #endregion

    #region Inspector Configuration
    [Header("Debug Coordinator Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoDiscoverManagers = true;
    [SerializeField] private float operationTimeout = 5.0f;

    [Header("Scenario Management")]
    [SerializeField] private string defaultScenarioName = "Default";
    [SerializeField] private bool saveScenarioOnStart = false;
    #endregion

    #region Runtime State
    private List<IManagerDebugInterface> discoveredManagers = new List<IManagerDebugInterface>();
    private Dictionary<string, IManagerDebugInterface> managersByName = new Dictionary<string, IManagerDebugInterface>();
    private Dictionary<string, DebugScenario> savedScenarios = new Dictionary<string, DebugScenario>();
    
    // Cross-system operation tracking
    private bool isOperationInProgress = false;
    private string currentOperationName = "";
    private float operationStartTime = 0f;
    
    // Performance monitoring
    private Dictionary<string, float> operationTimes = new Dictionary<string, float>();
    private Dictionary<string, int> operationCounts = new Dictionary<string, int>();
    #endregion

    #region Properties
    public int DiscoveredManagerCount => discoveredManagers.Count;
    public bool IsOperationInProgress => isOperationInProgress;
    public string CurrentOperation => currentOperationName;
    public IReadOnlyList<IManagerDebugInterface> DiscoveredManagers => discoveredManagers.AsReadOnly();
    public IReadOnlyDictionary<string, DebugScenario> SavedScenarios => savedScenarios;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeCoordinator();
    }

    private void Update()
    {
        MonitorOperationTimeout();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            DebugLog("Multiple DebugCoordinators found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeCoordinator()
    {
        DebugLog("Initializing DebugCoordinator...");

        if (autoDiscoverManagers)
        {
            DiscoverAllManagers();
        }

        if (saveScenarioOnStart)
        {
            SaveCurrentScenario(defaultScenarioName);
        }

        DebugLog($"DebugCoordinator initialized with {discoveredManagers.Count} managers");
    }
    #endregion

    #region Manager Discovery
    /// <summary>
    /// Discovers all managers implementing IManagerDebugInterface in the scene
    /// </summary>
    public void DiscoverAllManagers()
    {
        discoveredManagers.Clear();
        managersByName.Clear();

        // Find all MonoBehaviour components implementing IManagerDebugInterface
        var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        
        foreach (var mono in allMonoBehaviours)
        {
            if (mono is IManagerDebugInterface debugInterface)
            {
                RegisterManager(debugInterface, mono.GetType().Name);
            }
        }

        DebugLog($"Discovery complete: Found {discoveredManagers.Count} managers with debug interfaces");
    }

    /// <summary>
    /// Manually registers a manager with the coordinator
    /// </summary>
    public void RegisterManager(IManagerDebugInterface manager, string name = null)
    {
        if (manager == null) return;

        if (name == null)
        {
            name = manager.GetType().Name;
        }

        if (!discoveredManagers.Contains(manager))
        {
            discoveredManagers.Add(manager);
            managersByName[name] = manager;
            DebugLog($"Registered manager: {name}");
        }
    }

    /// <summary>
    /// Unregisters a manager from the coordinator
    /// </summary>
    public void UnregisterManager(IManagerDebugInterface manager)
    {
        if (manager == null) return;

        discoveredManagers.Remove(manager);
        
        // Remove from name dictionary
        var keysToRemove = managersByName.Where(kvp => kvp.Value == manager).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            managersByName.Remove(key);
        }

        DebugLog($"Unregistered manager: {manager.GetType().Name}");
    }

    /// <summary>
    /// Gets a manager by name
    /// </summary>
    public T GetManager<T>() where T : class, IManagerDebugInterface
    {
        return discoveredManagers.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets a manager by name
    /// </summary>
    public IManagerDebugInterface GetManager(string name)
    {
        return managersByName.TryGetValue(name, out var manager) ? manager : null;
    }
    #endregion

    #region Cross-System Operations
    /// <summary>
    /// Resets all discovered managers to their default states
    /// </summary>
    public void ResetAllManagersToDefaults()
    {
        StartOperation("Reset All to Defaults");

        int successCount = 0;
        int errorCount = 0;

        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.ResetToDefaults();
                successCount++;
                DebugLog($"Reset {manager.GetType().Name} to defaults");
            }
            catch (System.Exception e)
            {
                errorCount++;
                DebugLog($"Error resetting {manager.GetType().Name}: {e.Message}");
            }
        }

        EndOperation();
        DebugLog($"Reset operation completed: {successCount} success, {errorCount} errors");
    }

    /// <summary>
    /// Enables debug logging for all managers
    /// </summary>
    public void EnableDebugLoggingForAll()
    {
        StartOperation("Enable Debug Logging");

        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.EnableDebugLogs = true;
            }
            catch (System.Exception e)
            {
                DebugLog($"Error enabling debug logs for {manager.GetType().Name}: {e.Message}");
            }
        }

        EndOperation();
        DebugLog("Debug logging enabled for all managers");
    }

    /// <summary>
    /// Disables debug logging for all managers
    /// </summary>
    public void DisableDebugLoggingForAll()
    {
        StartOperation("Disable Debug Logging");

        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.EnableDebugLogs = false;
            }
            catch (System.Exception e)
            {
                DebugLog($"Error disabling debug logs for {manager.GetType().Name}: {e.Message}");
            }
        }

        EndOperation();
        DebugLog("Debug logging disabled for all managers");
    }

    /// <summary>
    /// Gets comprehensive debug status from all managers
    /// </summary>
    public Dictionary<string, string> GetAllManagerStatuses()
    {
        var statuses = new Dictionary<string, string>();

        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                string status = manager.GetDebugStatus();
                statuses[managerName] = status;
            }
            catch (System.Exception e)
            {
                string managerName = manager?.GetType().Name ?? "Unknown";
                statuses[managerName] = $"Error: {e.Message}";
            }
        }

        return statuses;
    }

    /// <summary>
    /// Gets comprehensive debug data from all managers
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetAllManagerDebugData()
    {
        var allData = new Dictionary<string, Dictionary<string, object>>();

        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                var debugData = manager.GetDebugData();
                allData[managerName] = debugData;
            }
            catch (System.Exception e)
            {
                string managerName = manager?.GetType().Name ?? "Unknown";
                allData[managerName] = new Dictionary<string, object> { ["Error"] = e.Message };
            }
        }

        return allData;
    }
    #endregion

    #region Scenario Management
    /// <summary>
    /// Saves the current state of all managers as a scenario
    /// </summary>
    public void SaveCurrentScenario(string scenarioName)
    {
        if (string.IsNullOrEmpty(scenarioName))
        {
            DebugLog("Cannot save scenario: name is empty");
            return;
        }

        StartOperation($"Save Scenario: {scenarioName}");

        var scenario = new DebugScenario
        {
            name = scenarioName,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            managerConfigurations = new Dictionary<string, Dictionary<string, object>>()
        };

        // Capture current state from all managers
        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                var debugData = manager.GetDebugData();
                scenario.managerConfigurations[managerName] = debugData;
            }
            catch (System.Exception e)
            {
                DebugLog($"Error capturing state from {manager.GetType().Name}: {e.Message}");
            }
        }

        savedScenarios[scenarioName] = scenario;
        EndOperation();
        DebugLog($"Saved scenario '{scenarioName}' with {scenario.managerConfigurations.Count} manager states");
    }

    /// <summary>
    /// Loads and applies a saved scenario
    /// </summary>
    public void LoadScenario(string scenarioName)
    {
        if (!savedScenarios.TryGetValue(scenarioName, out var scenario))
        {
            DebugLog($"Scenario '{scenarioName}' not found");
            return;
        }

        StartOperation($"Load Scenario: {scenarioName}");

        int loadedCount = 0;
        int errorCount = 0;

        // Apply scenario configuration to each manager
        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                
                if (scenario.managerConfigurations.ContainsKey(managerName))
                {
                    // For now, we'll use the LoadConfiguration method with the scenario name
                    // Future enhancement: implement actual state restoration
                    manager.LoadConfiguration(scenarioName);
                    loadedCount++;
                }
            }
            catch (System.Exception e)
            {
                errorCount++;
                DebugLog($"Error loading scenario for {manager.GetType().Name}: {e.Message}");
            }
        }

        EndOperation();
        DebugLog($"Loaded scenario '{scenarioName}': {loadedCount} managers configured, {errorCount} errors");
    }

    /// <summary>
    /// Deletes a saved scenario
    /// </summary>
    public void DeleteScenario(string scenarioName)
    {
        if (savedScenarios.Remove(scenarioName))
        {
            DebugLog($"Deleted scenario '{scenarioName}'");
        }
        else
        {
            DebugLog($"Scenario '{scenarioName}' not found for deletion");
        }
    }

    /// <summary>
    /// Gets list of all saved scenario names
    /// </summary>
    public List<string> GetScenarioNames()
    {
        return savedScenarios.Keys.ToList();
    }

    /// <summary>
    /// Creates a quick test scenario with predefined settings
    /// </summary>
    public void CreateTestScenario(string name)
    {
        StartOperation($"Create Test Scenario: {name}");

        // Reset all managers to defaults first
        ResetAllManagersToDefaults();

        // Apply some test configurations
        // This is a placeholder for more sophisticated test scenario creation
        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.EnableDebugLogs = true;
            }
            catch (System.Exception e)
            {
                DebugLog($"Error configuring test scenario for {manager.GetType().Name}: {e.Message}");
            }
        }

        // Save the configured state
        SaveCurrentScenario(name);
        EndOperation();
        DebugLog($"Created test scenario '{name}'");
    }
    #endregion

    #region Quick Actions
    /// <summary>
    /// Performs comprehensive system validation across all managers
    /// </summary>
    public void ValidateAllSystems()
    {
        StartOperation("Validate All Systems");

        var results = new Dictionary<string, bool>();
        int validCount = 0;
        int invalidCount = 0;

        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                
                // Basic validation: check if manager responds to debug calls
                var status = manager.GetDebugStatus();
                var debugData = manager.GetDebugData();
                
                bool isValid = !string.IsNullOrEmpty(status) && debugData != null;
                results[managerName] = isValid;
                
                if (isValid)
                {
                    validCount++;
                    DebugLog($"✓ {managerName}: Valid");
                }
                else
                {
                    invalidCount++;
                    DebugLog($"✗ {managerName}: Invalid response");
                }
            }
            catch (System.Exception e)
            {
                invalidCount++;
                string managerName = manager?.GetType().Name ?? "Unknown";
                results[managerName] = false;
                DebugLog($"✗ {managerName}: Exception - {e.Message}");
            }
        }

        EndOperation();
        DebugLog($"System validation complete: {validCount} valid, {invalidCount} invalid");
    }

    /// <summary>
    /// Generates comprehensive system report
    /// </summary>
    public void GenerateSystemReport()
    {
        StartOperation("Generate System Report");

        DebugLog("=== SYSTEM REPORT ===");
        DebugLog($"Debug Coordinator: {discoveredManagers.Count} managers discovered");
        DebugLog($"Saved Scenarios: {savedScenarios.Count}");
        DebugLog($"Operations Performed: {operationCounts.Count}");

        // Manager statuses
        DebugLog("--- Manager Statuses ---");
        var statuses = GetAllManagerStatuses();
        foreach (var kvp in statuses)
        {
            DebugLog($"{kvp.Key}: {kvp.Value}");
        }

        // Performance metrics
        DebugLog("--- Performance Metrics ---");
        foreach (var kvp in operationTimes)
        {
            int count = operationCounts.GetValueOrDefault(kvp.Key, 0);
            float avgTime = count > 0 ? kvp.Value / count : 0f;
            DebugLog($"{kvp.Key}: {count} times, avg {avgTime:F3}s");
        }

        // Scenario list
        DebugLog("--- Saved Scenarios ---");
        foreach (var scenario in savedScenarios.Values)
        {
            DebugLog($"'{scenario.name}' ({scenario.timestamp}): {scenario.managerConfigurations.Count} managers");
        }

        DebugLog("=== END REPORT ===");
        EndOperation();
    }

    /// <summary>
    /// Applies emergency reset to all systems
    /// </summary>
    public void EmergencyReset()
    {
        StartOperation("Emergency Reset");

        DebugLog("EMERGENCY RESET: Resetting all systems to safe state...");

        // Force reset all managers
        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.ResetToDefaults();
                manager.EnableDebugLogs = enableDebugLogs;
            }
            catch (System.Exception e)
            {
                DebugLog($"Emergency reset error for {manager.GetType().Name}: {e.Message}");
            }
        }

        // Clear operation state
        isOperationInProgress = false;
        currentOperationName = "";

        DebugLog("Emergency reset completed");
        EndOperation();
    }
    #endregion

    #region Performance Monitoring
    private void StartOperation(string operationName)
    {
        if (isOperationInProgress)
        {
            DebugLog($"Warning: Starting '{operationName}' while '{currentOperationName}' is in progress");
        }

        isOperationInProgress = true;
        currentOperationName = operationName;
        operationStartTime = Time.time;

        DebugLog($"Started operation: {operationName}");
    }

    private void EndOperation()
    {
        if (!isOperationInProgress) return;

        float duration = Time.time - operationStartTime;
        string opName = currentOperationName;

        // Record performance metrics
        if (!operationTimes.ContainsKey(opName))
        {
            operationTimes[opName] = 0f;
            operationCounts[opName] = 0;
        }

        operationTimes[opName] += duration;
        operationCounts[opName]++;

        DebugLog($"Completed operation: {opName} ({duration:F3}s)");

        isOperationInProgress = false;
        currentOperationName = "";
        operationStartTime = 0f;
    }

    private void MonitorOperationTimeout()
    {
        if (isOperationInProgress && Time.time - operationStartTime > operationTimeout)
        {
            DebugLog($"Operation timeout: {currentOperationName} exceeded {operationTimeout}s");
            EndOperation();
        }
    }
    #endregion

    #region Integration with Debug Panels
    /// <summary>
    /// Gets coordinator controls data for debug panels
    /// </summary>
    public Dictionary<string, object> GetCoordinatorDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Discovered Managers"] = discoveredManagers.Count,
            ["Saved Scenarios"] = savedScenarios.Count,
            ["Current Operation"] = currentOperationName,
            ["Operation In Progress"] = isOperationInProgress,
            ["Total Operations"] = operationCounts.Values.Sum(),
            ["Debug Logs Enabled"] = enableDebugLogs,
            ["Auto Discover"] = autoDiscoverManagers,
            ["Operation Timeout"] = operationTimeout
        };
    }

    /// <summary>
    /// Gets scenario selection data for debug panels
    /// </summary>
    public List<string> GetScenarioListForUI()
    {
        var scenarios = GetScenarioNames();
        scenarios.Sort();
        return scenarios;
    }

    /// <summary>
    /// Gets manager status summary for debug panels
    /// </summary>
    public string GetManagerStatusSummary()
    {
        var workingCount = 0;
        var errorCount = 0;

        foreach (var manager in discoveredManagers)
        {
            try
            {
                manager.GetDebugStatus(); // Test if manager responds
                workingCount++;
            }
            catch
            {
                errorCount++;
            }
        }

        return $"{workingCount} working, {errorCount} errors";
    }
    #endregion

    #region Utility Methods
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DebugCoordinator] {message}");
        }
    }

    /// <summary>
    /// Gets readable operation time statistics
    /// </summary>
    public string GetPerformanceStatistics()
    {
        if (operationCounts.Count == 0)
        {
            return "No operations performed yet";
        }

        var stats = new System.Text.StringBuilder();
        stats.AppendLine("Performance Statistics:");

        foreach (var kvp in operationCounts.OrderByDescending(x => x.Value))
        {
            float totalTime = operationTimes.GetValueOrDefault(kvp.Key, 0f);
            float avgTime = totalTime / kvp.Value;
            stats.AppendLine($"  {kvp.Key}: {kvp.Value}x, avg {avgTime:F3}s");
        }

        return stats.ToString();
    }
    #endregion
}

#region Data Structures
/// <summary>
/// Represents a saved debug scenario with manager configurations
/// </summary>
[System.Serializable]
public class DebugScenario
{
    public string name;
    public string timestamp;
    public Dictionary<string, Dictionary<string, object>> managerConfigurations;
}
#endregion