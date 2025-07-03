using UnityEngine;
using System.IO;

/// <summary>
/// Debug panel for monitoring and controlling file logging
/// </summary>
public class LoggingDebugPanel : DebugPanelBase
{
    #region Properties
    public override string PanelName => "Logging";
    public override string Group => "System";
    #endregion

    #region Runtime State
    private string currentLogPath = "";
    private long currentLogSize = 0;
    private int loggedMessageCount = 0;
    private bool showRecentLogs = false;
    private Vector2 recentLogsScroll;
    private string[] recentLogLines = new string[0];
    private int maxRecentLines = 50;
    #endregion

    #region Initialization
    protected override void OnInitialize()
    {
        // Initial setup if needed
        UpdateLogInfo();
    }
    #endregion

    #region Panel Drawing
    protected override void DrawPanelContent()
    {
        FileLogger logger = FileLogger.Instance;
        if (logger == null)
        {
            DrawNoLoggerUI();
            return;
        }

        DrawLoggerStatusSection();
        GUILayout.Space(10);
        
        DrawLoggerControlsSection();
        GUILayout.Space(10);
        
        DrawLogFileInfoSection();
        GUILayout.Space(10);
        
        DrawTestLoggingSection();
        GUILayout.Space(10);
        
        if (showRecentLogs)
        {
            DrawRecentLogsSection();
        }
    }
    #endregion

    #region UI Sections
    private void DrawNoLoggerUI()
    {
        DrawSectionHeader("File Logger Not Active");
        
        if (GUILayout.Button("Create FileLogger Instance", GUILayout.Height(30)))
        {
            GameObject go = new GameObject("FileLogger");
            go.AddComponent<FileLogger>();
            Debug.Log("Created FileLogger instance");
        }
        
        GUILayout.Label("Or add LoggingInitializer to a GameObject in your scene");
    }

    private void DrawLoggerStatusSection()
    {
        DrawSectionHeader("Logger Status");
        
        FileLogger logger = FileLogger.Instance;
        bool isEnabled = IsLoggerEnabled(logger);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Status:", GUILayout.Width(100));
        GUILayout.Label(isEnabled ? "ACTIVE" : "DISABLED", 
            isEnabled ? "box" : "box", GUILayout.Width(100));
        GUILayout.EndHorizontal();
        
        if (isEnabled)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Messages Logged:", GUILayout.Width(100));
            GUILayout.Label(loggedMessageCount.ToString());
            GUILayout.EndHorizontal();
        }
    }

    private void DrawLoggerControlsSection()
    {
        DrawSectionHeader("Logger Controls");
        
        FileLogger logger = FileLogger.Instance;
        
        // Enable/Disable toggle
        bool currentEnabled = IsLoggerEnabled(logger);
        bool newEnabled = GUILayout.Toggle(currentEnabled, "Enable File Logging");
        if (newEnabled != currentEnabled)
        {
            SetLoggerEnabled(logger, newEnabled);
        }
        
        GUILayout.Space(5);
        
        // Logger options (if we could access them)
        DrawLoggerOptions();
        
        GUILayout.Space(5);
        
        // Action buttons
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Flush Buffer"))
        {
            FileLogger.Flush();
            Debug.Log("Flushed log buffer to disk");
        }
        
        if (GUILayout.Button("Open Log Directory"))
        {
            FileLogger.OpenLogDirectory();
        }
        
        GUILayout.EndHorizontal();
    }

    private void DrawLoggerOptions()
    {
        GUILayout.Label("Logger Options:", "box");
        
        // Since we can't access private fields, we'll show what options would be available
        GUILayout.Label("• Timestamp: Enabled");
        GUILayout.Label("• Stack Trace: Configurable");
        GUILayout.Label("• Console Output: Enabled");
        GUILayout.Label("• Buffer Size: 100 entries");
        GUILayout.Label("• Flush Interval: 5 seconds");
    }

    private void DrawLogFileInfoSection()
    {
        DrawSectionHeader("Log File Information");
        
        UpdateLogInfo();
        
        if (!string.IsNullOrEmpty(currentLogPath))
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Log:", GUILayout.Width(80));
            GUILayout.TextField(Path.GetFileName(currentLogPath));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size:", GUILayout.Width(80));
            GUILayout.Label(FormatFileSize(currentLogSize));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Directory:", GUILayout.Width(80));
            string dir = Path.GetDirectoryName(currentLogPath);
            if (GUILayout.Button(dir, "label"))
            {
                Application.OpenURL($"file:///{dir}");
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            showRecentLogs = GUILayout.Toggle(showRecentLogs, "Show Recent Log Entries");
            
            if (showRecentLogs && GUILayout.Button("Refresh Log Preview"))
            {
                LoadRecentLogLines();
            }
        }
        else
        {
            GUILayout.Label("No active log file");
        }
    }

    private void DrawTestLoggingSection()
    {
        DrawSectionHeader("Test Logging");
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Test Log"))
        {
            FileLogger.Log($"Test log message at {Time.time:F2}");
            loggedMessageCount++;
        }
        
        if (GUILayout.Button("Test Warning"))
        {
            FileLogger.LogWarning($"Test warning at {Time.time:F2}");
            loggedMessageCount++;
        }
        
        if (GUILayout.Button("Test Error"))
        {
            FileLogger.LogError($"Test error at {Time.time:F2}");
            loggedMessageCount++;
        }
        
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Log System Info"))
        {
            LogSystemInfo();
        }
        
        if (GUILayout.Button("Log Memory Stats"))
        {
            LogMemoryStats();
        }
        
        if (GUILayout.Button("Create Test Table"))
        {
            CreateTestTable();
        }
    }

    private void DrawRecentLogsSection()
    {
        DrawSectionHeader($"Recent Log Entries (Last {maxRecentLines} lines)");
        
        recentLogsScroll = GUILayout.BeginScrollView(recentLogsScroll, GUILayout.Height(200));
        
        foreach (string line in recentLogLines)
        {
            // Color code based on log type
            if (line.Contains("[Error]"))
                GUI.color = Color.red;
            else if (line.Contains("[Warning]"))
                GUI.color = Color.yellow;
            else if (line.Contains("[Log]"))
                GUI.color = Color.white;
            else
                GUI.color = Color.gray;
                
            GUILayout.Label(line);
            GUI.color = Color.white;
        }
        
        GUILayout.EndScrollView();
    }
    #endregion

    #region Helper Methods
    private void UpdateLogInfo()
    {
        currentLogPath = FileLogger.GetLogPath();
        
        if (!string.IsNullOrEmpty(currentLogPath) && File.Exists(currentLogPath))
        {
            FileInfo fileInfo = new FileInfo(currentLogPath);
            currentLogSize = fileInfo.Length;
        }
        else
        {
            currentLogSize = 0;
        }
    }

    private void LoadRecentLogLines()
    {
        if (string.IsNullOrEmpty(currentLogPath) || !File.Exists(currentLogPath))
        {
            recentLogLines = new string[] { "No log file found" };
            return;
        }

        try
        {
            // Read last N lines from file
            var lines = new System.Collections.Generic.List<string>();
            using (var reader = new StreamReader(currentLogPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                    if (lines.Count > maxRecentLines)
                    {
                        lines.RemoveAt(0);
                    }
                }
            }
            recentLogLines = lines.ToArray();
        }
        catch (System.Exception e)
        {
            recentLogLines = new string[] { $"Error reading log: {e.Message}" };
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    private bool IsLoggerEnabled(FileLogger logger)
    {
        // Since enableFileLogging is private, we check if we can get a log path
        return !string.IsNullOrEmpty(FileLogger.GetLogPath());
    }

    private void SetLoggerEnabled(FileLogger logger, bool enabled)
    {
        // We can't directly set enableFileLogging, so we'll log this action
        Debug.Log($"Note: Cannot directly toggle file logging at runtime. Restart required.");
        FileLogger.Log($"File logging toggle requested: {enabled}");
    }

    private void LogSystemInfo()
    {
        FileLogger.LogSeparator("System Information");
        FileLogger.Log($"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        FileLogger.Log($"Game Time: {Time.time:F2} seconds");
        FileLogger.Log($"Frame: {Time.frameCount}");
        FileLogger.Log($"FPS: {1f / Time.deltaTime:F1}");
        FileLogger.Log($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        FileLogger.Log($"Platform: {Application.platform}");
        FileLogger.Log($"Unity Version: {Application.unityVersion}");
        FileLogger.LogSeparator();
        
        loggedMessageCount += 8;
    }

    private void LogMemoryStats()
    {
        FileLogger.LogSeparator("Memory Statistics");
        FileLogger.Log($"Total Reserved Memory: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)} MB");
        FileLogger.Log($"Total Allocated Memory: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
        FileLogger.Log($"Mono Heap Size: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / (1024 * 1024)} MB");
        FileLogger.Log($"Mono Used Size: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024 * 1024)} MB");
        FileLogger.LogSeparator();
        
        loggedMessageCount += 6;
    }

    private void CreateTestTable()
    {
        string[,] testData = new string[,]
        {
            { "Component", "Status", "Count", "Time" },
            { "StageManager", "Active", "1", Time.time.ToString("F2") },
            { "WaveManager", "Idle", "0", "0.00" },
            { "GridManager", "Ready", "64", "N/A" },
            { "CubeManager", "Active", "12", Time.time.ToString("F2") }
        };
        
        FileLogger.LogTable(testData, "Manager Status Table");
        loggedMessageCount++;
    }
    #endregion

    #region Update
    public override void Update()
    {
        // Periodically update log info
        if (Time.frameCount % 60 == 0) // Every second at 60fps
        {
            UpdateLogInfo();
        }
    }
    #endregion
}
