using UnityEngine;

/// <summary>
/// Ensures FileLogger is initialized at game start.
/// Add this to a GameObject in your first scene or make it a prefab.
/// </summary>
public class LoggingInitializer : MonoBehaviour
{
    [Header("Logging Configuration")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool showLogPathOnStart = true;
    
    private void Awake()
    {
        if (autoInitialize)
        {
            // This will create the FileLogger singleton if it doesn't exist
            var logger = FileLogger.Instance;
            
            if (logger != null && showLogPathOnStart)
            {
                string logPath = FileLogger.GetLogPath();
                if (!string.IsNullOrEmpty(logPath))
                {
                    Debug.Log($"[LoggingInitializer] File logging active - Writing to: {logPath}");
                    
                    // Log initial system info
                    FileLogger.LogSeparator("Game Session Started");
                    FileLogger.Log($"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    FileLogger.Log($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                    FileLogger.LogSeparator();
                }
            }
        }
    }
    
#if UNITY_EDITOR
    [ContextMenu("Open Log Folder")]
    private void OpenLogFolder()
    {
        FileLogger.OpenLogDirectory();
    }
    
    [ContextMenu("Show Current Log Path")]
    private void ShowLogPath()
    {
        string path = FileLogger.GetLogPath();
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log($"Current log file: {path}");
        }
        else
        {
            Debug.Log("No active log file");
        }
    }
#endif
}
