using UnityEngine;

/// <summary>
/// Simple debug UI styling using basic Unity GUI.skin styles.
/// Replaces complex theme system with reliable, basic styling.
/// </summary>
public static class DebugTheme
{
    #region Simple Color Constants
    public static readonly Color Active = Color.cyan;
    public static readonly Color Inactive = Color.gray;
    public static readonly Color Success = Color.green;
    public static readonly Color Warning = new Color(1f, 0.8f, 0f);
    public static readonly Color Error = Color.red;
    public static readonly Color Selected = Color.yellow;
    public static readonly Color Border = Color.gray;
    #endregion
    
    #region Basic Style Access
    
    /// <summary>
    /// Gets the basic box style using Unity defaults.
    /// </summary>
    public static GUIStyle GetBoxStyle()
    {
        return GUI.skin.box;
    }
    
    /// <summary>
    /// Gets a simple header style using Unity defaults.
    /// </summary>
    public static GUIStyle GetHeaderStyle()
    {
        return GUI.skin.label; // Use default label for headers
    }
    
    /// <summary>
    /// Gets the basic text style using Unity defaults.
    /// </summary>
    public static GUIStyle GetTextStyle()
    {
        return GUI.skin.label;
    }
    
    /// <summary>
    /// Gets the basic button style using Unity defaults.
    /// </summary>
    public static GUIStyle GetButtonStyle()
    {
        return GUI.skin.button;
    }
    
    /// <summary>
    /// Gets a smaller button style using Unity defaults.
    /// </summary>
    public static GUIStyle GetSmallButtonStyle()
    {
        return GUI.skin.button; // Same as regular button for simplicity
    }
    
    /// <summary>
    /// Gets the toggle button style using Unity defaults.
    /// </summary>
    public static GUIStyle GetToggleStyle()
    {
        return GUI.skin.button;
    }
    
    /// <summary>
    /// Gets the label style using Unity defaults.
    /// </summary>
    public static GUIStyle GetLabelStyle()
    {
        return GUI.skin.label;
    }
    
    /// <summary>
    /// Gets the text field style using Unity defaults.
    /// </summary>
    public static GUIStyle GetTextFieldStyle()
    {
        return GUI.skin.textField;
    }
    
    #endregion
}
