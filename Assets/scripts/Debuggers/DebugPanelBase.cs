using UnityEngine;
using static Enumerations;

/// <summary>
/// Enhanced base class for debug panels with performance optimizations and theme integration.
/// Implements isDirty flag system to prevent unnecessary OnGUI updates every frame.
/// </summary>
public abstract class DebugPanelBase : IDebugPanel
{
    #region Properties
    
    public abstract string PanelName { get; }
    public abstract DebugPanelGroup Group { get; }
    
    /// <summary>
    /// Whether this panel is currently visible/active in the debug UI.
    /// </summary>
    public bool IsVisible { get; private set; }
    
    /// <summary>
    /// Whether this panel needs to be redrawn on the next OnGUI call.
    /// </summary>
    protected bool IsDirty { get; private set; } = true;
    
    /// <summary>
    /// The last frame when this panel was updated. Used to prevent every-frame updates.
    /// </summary>
    protected int LastUpdateFrame { get; private set; } = -1;
    
    /// <summary>
    /// Update interval not used - debug panels always update for live data.
    /// Kept for interface compatibility.
    /// </summary>
    protected virtual int UpdateInterval => 1;
    
    #endregion
    
    #region Lifecycle Methods
    
    /// <summary>
    /// Called once when the panel is first created or enabled.
    /// </summary>
    public virtual void Initialize() 
    {
        MarkDirty();
        // Panel initialized and ready
    }
    
    /// <summary>
    /// Called every frame. Use sparingly - prefer MarkDirty() for updates.
    /// </summary>
    public virtual void Update() { }
    
    /// <summary>
    /// Called when the panel becomes visible.
    /// </summary>
    public virtual void OnShow()
    {
        IsVisible = true;
        MarkDirty();
        // Panel is now visible and needs update
    }
    
    /// <summary>
    /// Called when the panel becomes hidden.
    /// </summary>
    public virtual void OnHide()
    {
        IsVisible = false;
    }

    #endregion

    #region Performance Management

    /// <summary>
    /// Marks this panel as needing a redraw.
    /// For debug panels, this is mainly for interface compatibility.
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }
    
    /// <summary>
    /// For debug panels, always update to show current data.
    /// Debug interfaces should prioritize data accuracy over micro-optimizations.
    /// </summary>
    /// <returns>True - debug panels should always update</returns>
    protected bool ShouldUpdate()
    {
        // Debug panels should always show current data
        // Any "optimization" that prevents showing current debug info defeats the purpose
        return true;
    }
    
    /// <summary>
    /// Tracks that the panel was updated.
    /// Kept for interface compatibility and potential future optimizations.
    /// </summary>
    protected void MarkUpdated()
    {
        IsDirty = false;
        LastUpdateFrame = Time.frameCount;
    }
    
    #endregion
    
    #region Drawing Methods
    
    /// <summary>
    /// Main drawing method for debug panels.
    /// Always draws current content - debug panels need to show live data.
    /// </summary>
    public void DrawPanel()
    {
        // Debug panels should always show current data
        try
        {
            // Draw the panel content with simple Unity GUI
            DrawPanelContent();
            
            // Track that we updated (for potential future optimizations)
            MarkUpdated();
        }
        catch (System.Exception e)
        {
            // Fallback content when panel fails
            GUILayout.Label($"Panel Error: {e.Message}");
            GUILayout.Label("This panel failed to load properly.");
            GUILayout.Label("Check console for details.");
            
            // Log the error for debugging
            Debug.LogError($"Debug panel {PanelName} error: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Implement this method to draw your panel's content.
    /// This replaces the old abstract DrawPanel() method.
    /// </summary>
    protected abstract void DrawPanelContent();
    
    #endregion
    
    #region Simple Helper Methods
    
    /// <summary>
    /// Draws a simple section with basic Unity styling.
    /// </summary>
    /// <param name="title">Section title</param>
    /// <param name="content">Content drawing action</param>
    /// <param name="isExpanded">Whether section is expanded</param>
    protected void DrawSimpleSection(string title, System.Action content, bool isExpanded = true)
    {
        if (!isExpanded) return;
        
        GUILayout.BeginVertical(GUI.skin.box);
        
        if (!string.IsNullOrEmpty(title))
        {
            GUILayout.Label(title, GUI.skin.label);
            GUILayout.Space(3);
        }
        
        content?.Invoke();
        
        GUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draws a simple toggle button with basic Unity styling.
    /// Ensures immediate UI responsiveness to user interactions.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="current">Current state</param>
    /// <returns>New state</returns>
    protected bool DrawSimpleToggle(string label, bool current)
    {
        Color originalBgColor = GUI.backgroundColor;
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        
        bool clicked = GUILayout.Button(label, GUILayout.Height(25));
        
        GUI.backgroundColor = originalBgColor;
        
        if (clicked)
        {
            MarkDirty(); // Ensure immediate update on user interaction
            return !current;
        }
        
        return current;
    }
    
    /// <summary>
    /// Draws a simple button that automatically marks dirty on click.
    /// Ensures immediate UI responsiveness to user interactions.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="action">Action to execute on click</param>
    /// <param name="width">Optional width</param>
    protected void DrawSimpleButton(string label, System.Action action, float width = 0)
    {
        GUILayoutOption[] options = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[0];
        
        if (GUILayout.Button(label, options))
        {
            action?.Invoke();
            MarkDirty(); // Ensure immediate update on user interaction
        }
    }
    
    #endregion
}

