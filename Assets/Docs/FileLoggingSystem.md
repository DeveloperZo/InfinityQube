# InfinityQube File Logging System

## Overview
The InfinityQube file logging system captures all Unity Debug.Log output to files for easier debugging and diagnostics. This includes logs from StageManager, DebugSystem, and all other game components.

## Setup

### Method 1: Using LoggingInitializer (Recommended)
1. Add the `LoggingInitializer` component to a GameObject in your first scene
2. Configure the settings in the inspector:
   - **Auto Initialize**: Enable to start logging automatically
   - **Show Log Path On Start**: Display the log file location in console

### Method 2: Manual Setup
1. Create a GameObject with the `FileLogger` component
2. Configure settings in the FileLogger inspector

## File Logger Settings

### File Settings
- **Enable File Logging**: Master toggle for file logging
- **Log Directory**: Folder name within Unity's persistent data path (default: "Logs")
- **Log File Prefix**: Prefix for log file names (default: "InfinityQube")
- **Max File Size MB**: Maximum size before rotation (default: 10MB)
- **Max Log Files**: Number of log files to keep (default: 5)

### Logging Options
- **Include Timestamp**: Add timestamps to each log entry
- **Include Stack Trace**: Include stack traces (useful for errors)
- **Log To Console**: Also output to Unity console
- **Flush Immediately**: Write to disk immediately (impacts performance)

### Performance Settings
- **Buffer Size**: Number of logs to buffer before writing (default: 100)
- **Flush Interval**: Seconds between automatic flushes (default: 5)

## Log File Location

Log files are stored in Unity's persistent data path:
- Windows: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Logs\`
- macOS: `~/Library/Application Support/<CompanyName>/<ProductName>/Logs/`
- Linux: `~/.config/unity3d/<CompanyName>/<ProductName>/Logs/`

## Using the Debug Panel

1. Press F12 to open the Debug System
2. Navigate to the "Logging" tab
3. From here you can:
   - View current log file info
   - Open the log directory
   - Flush the buffer manually
   - Create test log entries
   - View recent log entries

## API Usage

### Basic Logging
```csharp
// Use FileLogger static methods
FileLogger.Log("Regular log message");
FileLogger.LogWarning("Warning message");
FileLogger.LogError("Error message");

// Or use regular Unity logging (automatically captured)
Debug.Log("This will be captured to file");
```

### Advanced Features
```csharp
// Force flush buffer to disk
FileLogger.Flush();

// Get current log file path
string logPath = FileLogger.GetLogPath();

// Open log directory in file explorer
FileLogger.OpenLogDirectory();

// Log with separators
FileLogger.LogSeparator("Section Title");

// Log formatted tables
string[,] data = new string[,] {
    { "Name", "Value" },
    { "Health", "100" },
    { "Score", "5000" }
};
FileLogger.LogTable(data, "Player Stats");
```

## Log File Format

Log entries follow this format:
```
[HH:mm:ss.fff] [LogType] Message
```

Example:
```
[14:23:45.123] [Log] StageManager: Loading stage 1
[14:23:45.456] [Warning] WaveManager: No wave data found
[14:23:45.789] [Error] CubeManager: Failed to spawn cube
```

## Tips

1. **Performance**: Disable stack traces and immediate flushing for better performance
2. **Debugging**: Enable stack traces when tracking down specific errors
3. **File Size**: Logs rotate automatically when reaching max size
4. **Thread Safety**: The logger is thread-safe and can handle logs from multiple threads

## Troubleshooting

### Logs not appearing
1. Check that FileLogger is enabled in the inspector
2. Verify the log directory exists and is writable
3. Check Unity's persistent data path permissions

### Performance impact
1. Increase buffer size to reduce write frequency
2. Disable immediate flushing
3. Disable stack trace collection

### Finding log files
1. Use the Debug Panel's "Open Log Directory" button
2. Or call `FileLogger.OpenLogDirectory()` from code
3. Check the console for the log path on startup
