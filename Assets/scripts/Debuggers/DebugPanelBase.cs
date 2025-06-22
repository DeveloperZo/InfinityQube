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
    /// How many frames to wait between automatic updates when not dirty.
    /// Default is 30 frames (approximately 0.5 seconds at 60 FPS).
    /// </summary>
    protected virtual int UpdateInterval => 30;
    
    #endregion
    
    #region Lifecycle Methods
    
    /// <summary>
    /// Called once when the panel is first created or enabled.
    /// </summary>
    public virtual void Initialize() 
    {
        DebugTheme.RefreshStyles();
        MarkDirty();
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
    /// Marks this panel as needing a redraw on the next OnGUI call.
    /// Call this whenever the panel's state changes.
    /// </summary>
    protected void MarkDirty()
    {
        IsDirty = true;
    }
    
    /// <summary>
    /// Determines if the panel should update this frame based on dirty state and update interval.
    /// </summary>
    /// <returns>True if the panel should update this frame</returns>
    protected bool ShouldUpdate()
    {
        int currentFrame = Time.frameCount;
        
        // Always update if dirty
        if (IsDirty)
        {
            return true;
        }
        
        // Update at specified intervals even when not dirty (for live data)
        if (currentFrame - LastUpdateFrame >= UpdateInterval)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Marks the panel as updated and clears the dirty flag.
    /// </summary>
    protected void MarkUpdated()
    {
        IsDirty = false;
        LastUpdateFrame = Time.frameCount;
    }
    
    #endregion
    
    #region Drawing Methods
    
    /// <summary>
    /// Main drawing method with performance optimization.
    /// Only calls DrawPanelContent() when necessary.
    /// </summary>
    public void DrawPanel()
    {
        if (!IsVisible)
        {
            return;
        }
        
        // Only update if necessary
        if (ShouldUpdate())
        {
            // Ensure theme styles are initialized
            DebugTheme.RefreshStyles();
            
            // Draw the panel content
            DrawPanelContent();
            
            // Mark as updated
            MarkUpdated();
        }
    }
    
    /// <summary>
    /// Implement this method to draw your panel's content.
    /// This replaces the old abstract DrawPanel() method.
    /// </summary>
    protected abstract void DrawPanelContent();
    
    #endregion
    
    #region Theme Integration
    
    /// <summary>
    /// Draws a themed section with consistent styling.
    /// </summary>
    /// <param name="title">Section title</param>
    /// <param name="content">Content drawing action</param>
    /// <param name="isExpanded">Whether section is expanded</param>
    protected void DrawThemedSection(string title, System.Action content, bool isExpanded = true)
    {
        if (!isExpanded) return;
        
        GUILayout.BeginVertical(DebugTheme.GetBoxStyle());
        
        if (!string.IsNullOrEmpty(title))
        {
            GUILayout.Label(title, DebugTheme.GetHeaderStyle());
            DebugUIHelpers.Space(3);
        }
        
        content?.Invoke();
        
        GUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draws a themed toggle button using the current theme.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="current">Current state</param>
    /// <returns>New state</returns>
    protected bool DrawThemedToggle(string label, bool current)
    {
        Color originalBgColor = GUI.backgroundColor;
        GUI.backgroundColor = current ? DebugTheme.Active : DebugTheme.Inactive;
        
        bool clicked = GUILayout.Button(label, DebugTheme.GetToggleStyle(), GUILayout.Height(25));
        
        GUI.backgroundColor = originalBgColor;
        
        if (clicked)
        {
            MarkDirty(); // Mark dirty when state changes
            return !current;
        }
        
        return current;
    }
    
    /// <summary>
    /// Draws themed text with the standard text style.
    /// </summary>
    /// <param name="text">Text to display</param>
    protected void DrawThemedText(string text)
    {
        GUILayout.Label(text, DebugTheme.GetTextStyle());
    }
    
    /// <summary>
    /// Draws a themed button that automatically marks dirty on click.
    /// </summary>
    /// <param name="label">Button label</param>
    /// <param name="action">Action to execute on click</param>
    /// <param name="width">Optional width</param>
    protected void DrawThemedButton(string label, System.Action action, float width = 0)
    {
        GUILayoutOption[] options = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[0];
        
        if (GUILayout.Button(label, DebugTheme.GetButtonStyle(), options))
        {
            action?.Invoke();
            MarkDirty();
        }
    }
    
    #endregion
}

