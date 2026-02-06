using UnityEngine;
using System;
using System.IO;
using System.Text;

/// <summary>
/// Dedicated logging system for scenario tests.
/// Writes logs to: [persistentDataPath]/Logs/Scenarios/[ScenarioName]/[timestamp].log
/// (Same location as FileLogger: C:/Users/[user]/AppData/LocalLow/[Company]/[Product]/)
/// Keeps the last N logs per scenario for easy comparison.
/// </summary>
public static class ScenarioLogger
{
    #region Configuration
    
    private const string SCENARIOS_LOG_FOLDER = "Logs/Scenarios";
    private const int MAX_LOGS_PER_SCENARIO = 5;
    
    #endregion
    
    #region State
    
    private static string currentScenarioName;
    private static string currentLogPath;
    private static StreamWriter logWriter;
    private static StringBuilder logBuffer = new StringBuilder();
    private static bool isActive = false;
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Start logging for a scenario. Creates a new log file.
    /// </summary>
    public static void StartScenario(string scenarioName)
    {
        if (isActive)
        {
            EndScenario();
        }
        
        currentScenarioName = scenarioName;
        isActive = true;
        
        try
        {
            // Create scenario-specific directory
            string basePath = GetScenarioLogDirectory(scenarioName);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            
            // Rotate old logs
            RotateLogs(basePath, scenarioName);
            
            // Create new log file
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{scenarioName}_{timestamp}.log";
            currentLogPath = Path.Combine(basePath, fileName);
            
            // Open file for writing
            logWriter = new StreamWriter(currentLogPath, false, Encoding.UTF8)
            {
                AutoFlush = true // Flush immediately for reliability
            };
            
            // Write header
            WriteHeader(scenarioName);
            
            Debug.Log($"[ScenarioLogger] Started logging to: {currentLogPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScenarioLogger] Failed to start logging: {e.Message}");
            isActive = false;
        }
    }
    
    /// <summary>
    /// Log a message for the current scenario.
    /// </summary>
    public static void Log(string message)
    {
        if (!isActive) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}] {message}";
        
        WriteLine(line);
    }
    
    /// <summary>
    /// Log a warning for the current scenario.
    /// </summary>
    public static void LogWarning(string message)
    {
        if (!isActive) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}] ⚠️ WARNING: {message}";
        
        WriteLine(line);
    }
    
    /// <summary>
    /// Log an error for the current scenario.
    /// </summary>
    public static void LogError(string message)
    {
        if (!isActive) return;
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}] ❌ ERROR: {message}";
        
        WriteLine(line);
    }
    
    /// <summary>
    /// Log a separator line.
    /// </summary>
    public static void LogSeparator(string title = "")
    {
        if (!isActive) return;
        
        string line = string.IsNullOrEmpty(title)
            ? "═══════════════════════════════════════════"
            : $"═══════════════ {title} ═══════════════";
        
        WriteLine(line);
    }
    
    /// <summary>
    /// Log scenario results summary.
    /// </summary>
    public static void LogResults(string scenarioName, bool passed, float elapsedTime, int steps,
        int captures, int escapes, int deaths, int assertionsPassed, int assertionsTotal)
    {
        LogSeparator("SCENARIO RESULTS");
        WriteLine($"Scenario: {scenarioName}");
        WriteLine($"Result: {(passed ? "✅ PASSED" : "❌ FAILED")}");
        WriteLine($"Time: {elapsedTime:F2}s | Steps: {steps}");
        WriteLine($"Captures: {captures} | Escapes: {escapes} | Deaths: {deaths}");
        WriteLine($"Assertions: {assertionsPassed}/{assertionsTotal} passed");
        LogSeparator();
    }
    
    /// <summary>
    /// Log an assertion result.
    /// </summary>
    public static void LogAssertion(string description, bool passed, int expected, int actual, int evaluatedAtStep)
    {
        string icon = passed ? "✅" : "❌";
        string stepInfo = evaluatedAtStep >= 0 ? $" (step {evaluatedAtStep})" : " (completion)";
        WriteLine($"  {icon} {description}{stepInfo}");
        WriteLine($"      Expected: {expected}, Actual: {actual}");
    }
    
    /// <summary>
    /// End logging for the current scenario and close the file.
    /// </summary>
    public static void EndScenario()
    {
        if (!isActive) return;
        
        try
        {
            WriteFooter();
            
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
                logWriter = null;
            }
            
            Debug.Log($"[ScenarioLogger] Finished logging: {currentLogPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScenarioLogger] Failed to end logging: {e.Message}");
        }
        finally
        {
            isActive = false;
            currentScenarioName = null;
            currentLogPath = null;
        }
    }
    
    /// <summary>
    /// Get the path to the current log file.
    /// </summary>
    public static string GetCurrentLogPath()
    {
        return currentLogPath;
    }
    
    /// <summary>
    /// Get the directory containing logs for a scenario.
    /// </summary>
    public static string GetScenarioLogDirectory(string scenarioName)
    {
        // Use persistentDataPath (same as FileLogger) for consistent log location
        // C:/Users/[user]/AppData/LocalLow/[Company]/[Product]/Logs/Scenarios/[scenarioName]
        string basePath = Path.Combine(Application.persistentDataPath, SCENARIOS_LOG_FOLDER);
        return Path.Combine(basePath, scenarioName);
    }
    
    /// <summary>
    /// Get the most recent log file for a scenario.
    /// </summary>
    public static string GetLatestLogPath(string scenarioName)
    {
        string dir = GetScenarioLogDirectory(scenarioName);
        if (!Directory.Exists(dir)) return null;
        
        var files = Directory.GetFiles(dir, "*.log");
        if (files.Length == 0) return null;
        
        // Sort by creation time descending
        Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
        return files[0];
    }
    
    /// <summary>
    /// Read the contents of a log file.
    /// </summary>
    public static string ReadLog(string logPath)
    {
        if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
            return null;
        
        return File.ReadAllText(logPath);
    }
    
    #endregion
    
    #region Private Methods
    
    private static void WriteLine(string line)
    {
        if (logWriter != null)
        {
            logWriter.WriteLine(line);
        }
    }
    
    private static void WriteHeader(string scenarioName)
    {
        WriteLine("═══════════════════════════════════════════");
        WriteLine($"SCENARIO: {scenarioName}");
        WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        WriteLine($"Unity: {Application.unityVersion}");
        WriteLine("═══════════════════════════════════════════");
        WriteLine("");
    }
    
    private static void WriteFooter()
    {
        WriteLine("");
        WriteLine("═══════════════════════════════════════════");
        WriteLine($"Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        WriteLine("═══════════════════════════════════════════");
    }
    
    private static void RotateLogs(string directory, string scenarioName)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*.log");
            if (files.Length < MAX_LOGS_PER_SCENARIO) return;
            
            // Sort by creation time (oldest first)
            Array.Sort(files, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
            
            // Delete oldest files until we're under the limit
            int toDelete = files.Length - MAX_LOGS_PER_SCENARIO + 1; // +1 to make room for new log
            for (int i = 0; i < toDelete && i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ScenarioLogger] Failed to rotate logs: {e.Message}");
        }
    }
    
    #endregion
}
