using static Enumerations;

/// <summary>
/// Interface for debug panels with performance optimization support.
/// </summary>
public interface IDebugPanel
{
    string PanelName { get; }
    DebugPanelGroup Group { get; }
    
    /// <summary>
    /// Whether this panel is currently visible/active.
    /// </summary>
    bool IsVisible { get; }
    
    void Initialize();
    void Update();
    void DrawPanel();
    
    /// <summary>
    /// Called when the panel becomes visible.
    /// </summary>
    void OnShow();
    
    /// <summary>
    /// Called when the panel becomes hidden.
    /// </summary>
    void OnHide();
}
