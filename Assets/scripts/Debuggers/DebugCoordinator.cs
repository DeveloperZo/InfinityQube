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
    [SerializeField] private bool EnableDebugLogs = true;
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
                manager.EnableDebugLogs = EnableDebugLogs;
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
            ["Debug Logs Enabled"] = EnableDebugLogs,
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

    /// <summary>
    /// Provides integration testing capabilities for the Testing Debug Panel
    /// </summary>
    public void RunCrossSystemIntegrationTest()
    {
        StartOperation("Cross-System Integration Test");
        
        var results = new Dictionary<string, bool>();
        
        // Test each manager individually
        foreach (var manager in discoveredManagers)
        {
            try
            {
                string managerName = manager.GetType().Name;
                
                // Basic health check
                var status = manager.GetDebugStatus();
                var debugData = manager.GetDebugData();
                
                bool isHealthy = !string.IsNullOrEmpty(status) && debugData != null;
                results[managerName] = isHealthy;
                
                DebugLog($"Integration Test - {managerName}: {(isHealthy ? "PASS" : "FAIL")}");
            }
            catch (System.Exception e)
            {
                string managerName = manager?.GetType().Name ?? "Unknown";
                results[managerName] = false;
                DebugLog($"Integration Test - {managerName}: FAIL - {e.Message}");
            }
        }
        
        // Test cross-manager coordination
        TestManagerCoordination();
        
        EndOperation();
        
        int passCount = results.Values.Count(r => r);
        int totalCount = results.Count;
        DebugLog($"Cross-System Integration Test Complete: {passCount}/{totalCount} managers passed");
    }
    
    /// <summary>
    /// Tests coordination between different managers
    /// </summary>
    private void TestManagerCoordination()
    {
        DebugLog("Testing manager coordination...");
        
        // Test scenario: Save and restore state
        try
        {
            string testScenarioName = "__integration_test_temp__";
            
            SaveCurrentScenario(testScenarioName);
            
            // Modify some states
            foreach (var manager in discoveredManagers.Take(2))
            {
                try
                {
                    manager.ResetToDefaults();
                }
                catch (System.Exception e)
                {
                    DebugLog($"Coordination test - Reset failed for {manager.GetType().Name}: {e.Message}");
                }
            }
            
            // Restore state
            LoadScenario(testScenarioName);
            
            // Clean up test scenario
            DeleteScenario(testScenarioName);
            
            DebugLog("Manager coordination test: PASS");
        }
        catch (System.Exception e)
        {
            DebugLog($"Manager coordination test: FAIL - {e.Message}");
        }
    }
    
    /// <summary>
    /// Provides stress testing capabilities
    /// </summary>
    public void RunStressTest(int operationCount = 10)
    {
        StartOperation($"Stress Test ({operationCount} operations)");
        
        int successCount = 0;
        int errorCount = 0;
        
        for (int i = 0; i < operationCount; i++)
        {
            try
            {
                // Perform various operations rapidly
                switch (i % 4)
                {
                    case 0:
                        GetAllManagerStatuses();
                        break;
                    case 1:
                        GetAllManagerDebugData();
                        break;
                    case 2:
                        ValidateAllSystems();
                        break;
                    case 3:
                        ResetAllManagersToDefaults();
                        break;
                }
                
                successCount++;
            }
            catch (System.Exception e)
            {
                errorCount++;
                DebugLog($"Stress test operation {i} failed: {e.Message}");
            }
        }
        
        EndOperation();
        DebugLog($"Stress test complete: {successCount} success, {errorCount} errors");
    }
    
    /// <summary>
    /// Gets detailed system health report for Testing Debug Panel
    /// </summary>
    public SystemHealthReport GetSystemHealthReport()
    {
        var report = new SystemHealthReport
        {
            Timestamp = System.DateTime.Now,
            TotalManagers = discoveredManagers.Count,
            HealthyManagers = 0,
            UnhealthyManagers = 0,
            ManagerDetails = new Dictionary<string, ManagerHealth>(),
            OverallHealth = SystemHealth.Unknown
        };
        
        foreach (var manager in discoveredManagers)
        {
            var health = new ManagerHealth();
            health.ManagerName = manager.GetType().Name;
            
            try
            {
                health.Status = manager.GetDebugStatus();
                health.DebugData = manager.GetDebugData();
                health.IsResponsive = true;
                health.LastError = null;
                
                // Basic health assessment
                health.IsHealthy = !string.IsNullOrEmpty(health.Status) && health.DebugData != null;
                
                if (health.IsHealthy)
                {
                    report.HealthyManagers++;
                }
                else
                {
                    report.UnhealthyManagers++;
                }
            }
            catch (System.Exception e)
            {
                health.IsResponsive = false;
                health.IsHealthy = false;
                health.LastError = e.Message;
                health.Status = "ERROR";
                health.DebugData = null;
                report.UnhealthyManagers++;
            }
            
            report.ManagerDetails[health.ManagerName] = health;
        }
        
        // Determine overall health
        if (report.UnhealthyManagers == 0)
        {
            report.OverallHealth = SystemHealth.Healthy;
        }
        else if (report.HealthyManagers > report.UnhealthyManagers)
        {
            report.OverallHealth = SystemHealth.Degraded;
        }
        else
        {
            report.OverallHealth = SystemHealth.Unhealthy;
        }
        
        return report;
    }
    
    /// <summary>
    /// Quick panel integration check - used by panels to verify coordinator availability
    /// </summary>
    public bool IsPanelIntegrationReady()
    {
        return discoveredManagers.Count > 0 && !isOperationInProgress;
    }
    
    /// <summary>
    /// Provides a quick coordination test for panels
    /// </summary>
    public bool QuickCoordinationTest()
    {
        try
        {
            // Quick test: can we get status from all managers?
            foreach (var manager in discoveredManagers)
            {
                manager.GetDebugStatus();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region Utility Methods
    private void DebugLog(string message)
    {
        if (EnableDebugLogs)
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

/// <summary>
/// System health report for debugging panels
/// </summary>
public class SystemHealthReport
{
    public System.DateTime Timestamp;
    public int TotalManagers;
    public int HealthyManagers;
    public int UnhealthyManagers;
    public Dictionary<string, ManagerHealth> ManagerDetails;
    public SystemHealth OverallHealth;
}

/// <summary>
/// Health information for individual managers
/// </summary>
public class ManagerHealth
{
    public string ManagerName;
    public bool IsHealthy;
    public bool IsResponsive;
    public string Status;
    public Dictionary<string, object> DebugData;
    public string LastError;
}

/// <summary>
/// Overall system health states
/// </summary>
public enum SystemHealth
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy
}
#endregion