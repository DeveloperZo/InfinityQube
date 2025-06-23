using System.Collections.Generic;

/// <summary>
/// Standardized debug interface that all managers can implement to provide 
/// consistent debug capabilities across the system. This interface standardizes
/// existing debug patterns found across managers and provides a foundation for
/// enhanced debug panel integration.
/// </summary>
public interface IManagerDebugInterface
{
    /// <summary>
    /// Gets or sets whether debug logging is enabled for this manager.
    /// Standardizes the existing enableDebugLogs pattern found across managers.
    /// </summary>
    bool EnableDebugLogs { get; set; }
    
    /// <summary>
    /// Gets a human-readable string describing the current status of this manager.
    /// Should include key state information for quick debugging overview.
    /// </summary>
    /// <returns>Status string suitable for display in debug panels</returns>
    string GetDebugStatus();
    
    /// <summary>
    /// Gets a dictionary of debug data containing key-value pairs of manager state.
    /// Keys should be descriptive names, values can be any serializable type.
    /// This provides structured access to manager state for debug panels and tools.
    /// </summary>
    /// <returns>Dictionary containing debug data with string keys and object values</returns>
    Dictionary<string, object> GetDebugData();
    
    /// <summary>
    /// Resets the manager to its default state. Implementation should restore
    /// initial configuration values and clear any runtime state as appropriate.
    /// Useful for testing scenarios and debug workflows.
    /// </summary>
    void ResetToDefaults();
    
    /// <summary>
    /// Loads configuration settings from a named configuration.
    /// Implementation details depend on manager needs - can use ScriptableObjects,
    /// JSON files, or other persistence mechanisms as appropriate.
    /// </summary>
    /// <param name="configName">Name of the configuration to load</param>
    void LoadConfiguration(string configName);
    
    /// <summary>
    /// Saves current manager state to a named configuration.
    /// Allows debug scenarios and manager states to be persisted and recalled.
    /// Implementation should handle persistence mechanism appropriate for the manager.
    /// </summary>
    /// <param name="configName">Name to save the configuration under</param>
    void SaveConfiguration(string configName);
}
