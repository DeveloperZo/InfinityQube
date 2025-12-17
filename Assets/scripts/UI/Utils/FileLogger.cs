using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// File-based logging system that captures all Unity Debug.Log output to files.
/// Provides thread-safe, buffered writing with automatic log rotation.
/// </summary>
public class FileLogger : MonoBehaviour
{
    #region Singleton
    private static FileLogger instance;
    public static FileLogger Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("FileLogger");
                instance = go.AddComponent<FileLogger>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    #endregion

    #region Configuration
    [Header("File Settings")]
    [SerializeField] private bool enableFileLogging = true;
    [SerializeField] private string logDirectory = "Logs";
    [SerializeField] private string logFilePrefix = "InfinityQube";
    [SerializeField] private int maxFileSizeMB = 10;
    [SerializeField] private int maxLogFiles = 5;

    [Header("Logging Options")]
    [SerializeField] private bool includeTimestamp = true;
    [SerializeField] private bool includeStackTrace = false;
    [SerializeField] private bool logToConsole = true; // Also output to Unity console
    [SerializeField] private bool flushImmediately = false; // Flush after each write

    [Header("Performance")]
    [SerializeField] private int bufferSize = 100; // Number of logs to buffer before writing
    [SerializeField] private float flushInterval = 5f; // Seconds between automatic flushes
    #endregion

    #region Runtime State
    private string currentLogPath;
    private StreamWriter logWriter;
    private Queue<LogEntry> logBuffer = new Queue<LogEntry>();
    private readonly object bufferLock = new object();
    private float lastFlushTime;
    private bool isQuitting = false;
    #endregion

    #region Log Entry Structure
    private struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public DateTime timestamp;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Ensure singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (enableFileLogging)
        {
            InitializeLogging();
        }
    }

    private void OnEnable()
    {
        if (enableFileLogging)
        {
            // Only subscribe to logMessageReceivedThreaded - it handles ALL logs (main thread + background threads)
            // Using both logMessageReceived AND logMessageReceivedThreaded causes duplicate entries
            // since logMessageReceivedThreaded is fired for main thread logs too
            Application.logMessageReceivedThreaded += HandleLogThreaded;
        }
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enableFileLogging)
        {
            FlushBuffer();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && enableFileLogging)
        {
            FlushBuffer();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        if (enableFileLogging)
        {
            LogToFile("Application Quit", LogType.Log);
            CloseLogging();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            CloseLogging();
            instance = null;
        }
    }

    private void Update()
    {
        if (!enableFileLogging) return;

        // Periodic flush
        if (Time.time - lastFlushTime > flushInterval)
        {
            FlushBuffer();
            lastFlushTime = Time.time;
        }
    }
    #endregion

    #region Initialization
    private void InitializeLogging()
    {
        try
        {
            // Create logs directory
            string fullLogPath = Path.Combine(Application.persistentDataPath, logDirectory);
            if (!Directory.Exists(fullLogPath))
            {
                Directory.CreateDirectory(fullLogPath);
            }

            // Rotate old logs if needed
            RotateOldLogs(fullLogPath);

            // Create new log file
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{logFilePrefix}_{timestamp}.log";
            currentLogPath = Path.Combine(fullLogPath, fileName);

            // Open file for writing
            logWriter = new StreamWriter(currentLogPath, false, Encoding.UTF8)
            {
                AutoFlush = flushImmediately
            };

            // Write header
            WriteLogHeader();

            Debug.Log($"FileLogger: Initialized - Writing to {currentLogPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"FileLogger: Failed to initialize - {e.Message}");
            enableFileLogging = false;
        }
    }

    private void WriteLogHeader()
    {
        if (logWriter == null) return;

        logWriter.WriteLine("=====================================");
        logWriter.WriteLine($"InfinityQube Log File");
        logWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
        logWriter.WriteLine($"Platform: {Application.platform}");
        logWriter.WriteLine($"Device: {SystemInfo.deviceModel}");
        logWriter.WriteLine("=====================================");
        logWriter.WriteLine();
    }

    private void RotateOldLogs(string logPath)
    {
        try
        {
            // Get all log files
            DirectoryInfo dir = new DirectoryInfo(logPath);
            FileInfo[] files = dir.GetFiles($"{logFilePrefix}_*.log");

            // Sort by creation time (oldest first)
            Array.Sort(files, (x, y) => x.CreationTime.CompareTo(y.CreationTime));

            // Delete old files if we exceed max count
            while (files.Length >= maxLogFiles && files.Length > 0)
            {
                files[0].Delete();
                files = dir.GetFiles($"{logFilePrefix}_*.log");
                Array.Sort(files, (x, y) => x.CreationTime.CompareTo(y.CreationTime));
            }

            // Check file sizes and rotate if needed
            foreach (var file in files)
            {
                if (file.Length > maxFileSizeMB * 1024 * 1024)
                {
                    string archiveName = file.FullName.Replace(".log", "_archived.log");
                    file.MoveTo(archiveName);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"FileLogger: Failed to rotate logs - {e.Message}");
        }
    }
    #endregion

    #region Log Handling
    private void HandleLogThreaded(string logString, string stackTrace, LogType type)
    {
        if (!enableFileLogging || isQuitting) return;

        // Thread-safe handling
        lock (bufferLock)
        {
            logBuffer.Enqueue(new LogEntry
            {
                message = logString,
                stackTrace = stackTrace,
                type = type,
                timestamp = DateTime.Now
            });

            // Flush if buffer is full
            if (logBuffer.Count >= bufferSize)
            {
                FlushBuffer();
            }
        }
    }

    private void LogToFile(string message, LogType type, string stackTrace = "")
    {
        lock (bufferLock)
        {
            logBuffer.Enqueue(new LogEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
                timestamp = DateTime.Now
            });

            if (flushImmediately || logBuffer.Count >= bufferSize)
            {
                FlushBuffer();
            }
        }
    }

    private void FlushBuffer()
    {
        if (logWriter == null) return;

        lock (bufferLock)
        {
            try
            {
                while (logBuffer.Count > 0)
                {
                    LogEntry entry = logBuffer.Dequeue();
                    WriteLogEntry(entry);
                }

                logWriter.Flush();
            }
            catch (Exception e)
            {
                // Can't use Debug.Log here as it would create infinite loop
                Console.WriteLine($"FileLogger: Flush failed - {e.Message}");
            }
        }
    }

    private void WriteLogEntry(LogEntry entry)
    {
        if (logWriter == null) return;

        StringBuilder sb = new StringBuilder();

        // Timestamp
        if (includeTimestamp)
        {
            sb.Append($"[{entry.timestamp:HH:mm:ss.fff}] ");
        }

        // Log type
        sb.Append($"[{entry.type}] ");

        // Message
        sb.AppendLine(entry.message);

        // Stack trace (if enabled and available)
        if (includeStackTrace && !string.IsNullOrEmpty(entry.stackTrace))
        {
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(entry.stackTrace);
            sb.AppendLine();
        }

        logWriter.Write(sb.ToString());
    }
    #endregion

    #region Cleanup
    private void CloseLogging()
    {
        FlushBuffer();

        if (logWriter != null)
        {
            try
            {
                logWriter.WriteLine();
                logWriter.WriteLine("=====================================");
                logWriter.WriteLine($"Log Closed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine("=====================================");
                logWriter.Close();
                logWriter.Dispose();
                logWriter = null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"FileLogger: Failed to close log - {e.Message}");
            }
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Manually write a message to the log file
    /// </summary>
    public static void Log(string message)
    {
        if (Instance != null && Instance.enableFileLogging)
        {
            Instance.LogToFile(message, LogType.Log);
        }
        
        if (Instance == null || Instance.logToConsole)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// Manually write a warning to the log file
    /// </summary>
    public static void LogWarning(string message)
    {
        if (Instance != null && Instance.enableFileLogging)
        {
            Instance.LogToFile(message, LogType.Warning);
        }
        
        if (Instance == null || Instance.logToConsole)
        {
            Debug.LogWarning(message);
        }
    }

    /// <summary>
    /// Manually write an error to the log file
    /// </summary>
    public static void LogError(string message)
    {
        if (Instance != null && Instance.enableFileLogging)
        {
            Instance.LogToFile(message, LogType.Error);
        }
        
        if (Instance == null || Instance.logToConsole)
        {
            Debug.LogError(message);
        }
    }

    /// <summary>
    /// Force flush the log buffer to disk
    /// </summary>
    public static void Flush()
    {
        if (Instance != null && Instance.enableFileLogging)
        {
            Instance.FlushBuffer();
        }
    }

    /// <summary>
    /// Get the current log file path
    /// </summary>
    public static string GetLogPath()
    {
        return Instance != null ? Instance.currentLogPath : null;
    }

    /// <summary>
    /// Open the log directory in the file explorer
    /// </summary>
    public static void OpenLogDirectory()
    {
        if (Instance != null)
        {
            string path = Path.Combine(Application.persistentDataPath, Instance.logDirectory);
            if (Directory.Exists(path))
            {
                Application.OpenURL($"file:///{path}");
            }
        }
    }

    /// <summary>
    /// Write a separator line to the log
    /// </summary>
    public static void LogSeparator(string title = "")
    {
        string separator = string.IsNullOrEmpty(title) 
            ? "----------------------------------------" 
            : $"---------- {title} ----------";
        Log(separator);
    }

    /// <summary>
    /// Log a formatted table (useful for debugging grids, arrays, etc)
    /// </summary>
    public static void LogTable(string[,] data, string title = "")
    {
        if (!string.IsNullOrEmpty(title))
        {
            Log($"\n{title}:");
        }

        StringBuilder sb = new StringBuilder();
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                sb.Append(data[i, j].PadRight(12));
            }
            sb.AppendLine();
        }

        Log(sb.ToString());
    }
    #endregion

    #region Editor Helpers
#if UNITY_EDITOR
    [ContextMenu("Open Log Directory")]
    private void OpenLogDirectoryEditor()
    {
        OpenLogDirectory();
    }

    [ContextMenu("Test Log Output")]
    private void TestLogOutput()
    {
        LogSeparator("Test Log Output");
        Log("This is a regular log message");
        LogWarning("This is a warning message");
        LogError("This is an error message");
        
        // Test table logging
        string[,] testData = new string[,]
        {
            { "Name", "Value", "Status" },
            { "Test1", "123", "OK" },
            { "Test2", "456", "Warning" },
            { "Test3", "789", "Error" }
        };
        LogTable(testData, "Test Data Table");
        
        LogSeparator("End Test");
        Flush();
    }
#endif
    #endregion
}
