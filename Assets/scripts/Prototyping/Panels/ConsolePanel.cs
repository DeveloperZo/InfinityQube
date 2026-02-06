using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Console Panel - View Unity console logs in-game.
/// Shows recent logs, warnings, and errors with filtering.
/// Essential for debugging without switching to the editor.
/// </summary>
public class ConsolePanel : PrototypingPanelBase
{
    public override string PanelName => "Console";
    public override string PanelIcon => "📋";
    public override PrototypingCategory Category => PrototypingCategory.System;
    public override int Priority => 55;
    
    #region Log Storage
    private struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public float timestamp;
        public int count;
    }
    
    private List<LogEntry> logEntries = new List<LogEntry>();
    private const int MAX_LOGS = 100;
    private Vector2 scrollPosition;
    
    // Filters
    private bool showLogs = true;
    private bool showWarnings = true;
    private bool showErrors = true;
    private bool collapseIdentical = true;
    private bool autoScroll = true;
    private string filterText = "";
    
    // Stats
    private int logCount = 0;
    private int warningCount = 0;
    private int errorCount = 0;
    
    // Selected log for detail view
    private int selectedLogIndex = -1;
    private bool showStackTrace = false;
    #endregion
    
    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to Unity log callback
        Application.logMessageReceived += HandleLog;
    }
    
    public void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
    
    private void HandleLog(string message, string stackTrace, LogType type)
    {
        // Update counts
        switch (type)
        {
            case LogType.Log:
                logCount++;
                break;
            case LogType.Warning:
                warningCount++;
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                errorCount++;
                break;
        }
        
        // Check for duplicate (collapse)
        if (collapseIdentical && logEntries.Count > 0)
        {
            var lastEntry = logEntries[logEntries.Count - 1];
            if (lastEntry.message == message && lastEntry.type == type)
            {
                lastEntry.count++;
                logEntries[logEntries.Count - 1] = lastEntry;
                return;
            }
        }
        
        // Add new entry
        logEntries.Add(new LogEntry
        {
            message = message,
            stackTrace = stackTrace,
            type = type,
            timestamp = Time.unscaledTime,
            count = 1
        });
        
        // Trim if too many
        while (logEntries.Count > MAX_LOGS)
        {
            logEntries.RemoveAt(0);
        }
        
        // Auto-scroll
        if (autoScroll)
        {
            scrollPosition.y = float.MaxValue;
        }
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void DrawGUI()
    {
        // Status bar with counts
        DrawStatusBar();
        
        GUILayout.Space(3);
        
        // Filter bar
        DrawFilterBar();
        
        GUILayout.Space(3);
        
        // Log list
        DrawLogList();
        
        // Stack trace detail (if selected)
        if (showStackTrace && selectedLogIndex >= 0 && selectedLogIndex < logEntries.Count)
        {
            DrawStackTrace();
        }
    }
    
    private void DrawStatusBar()
    {
        GUILayout.BeginHorizontal();
        
        // Counts with colors
        var defaultColor = GUI.color;
        
        GUI.color = showLogs ? Color.white : Color.gray;
        if (GUILayout.Button($"Log: {logCount}", GUILayout.Width(70)))
        {
            showLogs = !showLogs;
        }
        
        GUI.color = showWarnings ? Color.yellow : Color.gray;
        if (GUILayout.Button($"Warn: {warningCount}", GUILayout.Width(70)))
        {
            showWarnings = !showWarnings;
        }
        
        GUI.color = showErrors ? Color.red : Color.gray;
        if (GUILayout.Button($"Err: {errorCount}", GUILayout.Width(70)))
        {
            showErrors = !showErrors;
        }
        
        GUI.color = defaultColor;
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            ClearLogs();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawFilterBar()
    {
        GUILayout.BeginHorizontal();
        
        GUILayout.Label("Filter:", GUILayout.Width(40));
        filterText = GUILayout.TextField(filterText, GUILayout.Width(120));
        
        if (!string.IsNullOrEmpty(filterText))
        {
            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                filterText = "";
            }
        }
        
        GUILayout.FlexibleSpace();
        
        // Options
        collapseIdentical = GUILayout.Toggle(collapseIdentical, "Collapse", GUILayout.Width(70));
        autoScroll = GUILayout.Toggle(autoScroll, "Auto↓", GUILayout.Width(55));
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawLogList()
    {
        // Calculate height for log area
        float logAreaHeight = showStackTrace ? 200f : 350f;
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(logAreaHeight));
        
        var filteredLogs = GetFilteredLogs();
        
        if (filteredLogs.Count == 0)
        {
            GUILayout.Label("No logs to display", GUI.skin.box);
        }
        else
        {
            for (int i = 0; i < filteredLogs.Count; i++)
            {
                DrawLogEntry(filteredLogs[i], i);
            }
        }
        
        GUILayout.EndScrollView();
    }
    
    private List<LogEntry> GetFilteredLogs()
    {
        var filtered = new List<LogEntry>();
        
        foreach (var entry in logEntries)
        {
            // Type filter
            if (!ShouldShowType(entry.type))
                continue;
            
            // Text filter
            if (!string.IsNullOrEmpty(filterText))
            {
                if (!entry.message.ToLower().Contains(filterText.ToLower()))
                    continue;
            }
            
            filtered.Add(entry);
        }
        
        return filtered;
    }
    
    private bool ShouldShowType(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return showLogs;
            case LogType.Warning:
                return showWarnings;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return showErrors;
            default:
                return true;
        }
    }
    
    private void DrawLogEntry(LogEntry entry, int index)
    {
        // Determine color
        Color bgColor = GetLogColor(entry.type);
        bool isSelected = index == selectedLogIndex;
        
        if (isSelected)
        {
            bgColor = Color.Lerp(bgColor, Color.white, 0.3f);
        }
        
        GUI.backgroundColor = bgColor;
        
        GUILayout.BeginHorizontal(GUI.skin.box);
        
        // Icon
        string icon = GetLogIcon(entry.type);
        GUILayout.Label(icon, GUILayout.Width(20));
        
        // Message (truncated)
        string displayMessage = entry.message;
        if (displayMessage.Length > 80)
        {
            displayMessage = displayMessage.Substring(0, 77) + "...";
        }
        
        // Replace newlines for display
        displayMessage = displayMessage.Replace("\n", " ");
        
        if (GUILayout.Button(displayMessage, GUI.skin.label))
        {
            if (selectedLogIndex == index)
            {
                showStackTrace = !showStackTrace;
            }
            else
            {
                selectedLogIndex = index;
                showStackTrace = true;
            }
        }
        
        // Count badge
        if (entry.count > 1)
        {
            GUILayout.Label($"({entry.count})", GUILayout.Width(35));
        }
        
        GUILayout.EndHorizontal();
        
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawStackTrace()
    {
        GUILayout.Space(5);
        
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Stack Trace:", GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            CopyLogToClipboard(selectedLogIndex);
        }
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            showStackTrace = false;
        }
        GUILayout.EndHorizontal();
        
        if (selectedLogIndex >= 0 && selectedLogIndex < logEntries.Count)
        {
            var entry = logEntries[selectedLogIndex];
            
            // Full message
            GUILayout.Label("Message:", GUI.skin.box);
            GUILayout.Label(entry.message);
            
            // Stack trace
            if (!string.IsNullOrEmpty(entry.stackTrace))
            {
                GUILayout.Space(3);
                GUILayout.Label("Stack:", GUI.skin.box);
                
                // Show first few lines of stack
                string[] lines = entry.stackTrace.Split('\n');
                int linesToShow = Mathf.Min(lines.Length, 8);
                for (int i = 0; i < linesToShow; i++)
                {
                    string line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var smallStyle = new GUIStyle(GUI.skin.label);
                        smallStyle.fontSize = 10;
                        GUILayout.Label(line, smallStyle);
                    }
                }
                
                if (lines.Length > 8)
                {
                    GUILayout.Label($"... {lines.Length - 8} more lines");
                }
            }
        }
        
        GUILayout.EndVertical();
    }
    
    private Color GetLogColor(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return new Color(0.2f, 0.2f, 0.25f);
            case LogType.Warning:
                return new Color(0.4f, 0.35f, 0.1f);
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return new Color(0.4f, 0.15f, 0.15f);
            default:
                return Color.gray;
        }
    }
    
    private string GetLogIcon(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return "ℹ";
            case LogType.Warning:
                return "⚠";
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return "✖";
            default:
                return "•";
        }
    }
    
    private void ClearLogs()
    {
        logEntries.Clear();
        logCount = 0;
        warningCount = 0;
        errorCount = 0;
        selectedLogIndex = -1;
        showStackTrace = false;
        LogAction("Console cleared");
    }
    
    private void CopyLogToClipboard(int index)
    {
        if (index < 0 || index >= logEntries.Count) return;
        
        var entry = logEntries[index];
        string text = $"[{entry.type}] {entry.message}\n\n{entry.stackTrace}";
        GUIUtility.systemCopyBuffer = text;
        LogAction("Log copied to clipboard");
    }
}
