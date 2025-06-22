using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized theme and style management for debug UI with performance caching.
/// Adapts GameUI.cs patterns for consistent styling across all debug panels.
/// </summary>
public static class DebugTheme
{
    #region Theme Configuration
    
    public enum ThemeVariant
    {
        Dark,
        Light
    }
    
    public static ThemeVariant CurrentTheme { get; set; } = ThemeVariant.Dark;
    
    #endregion
    
    #region Color Definitions
    
    // Dark theme colors
    private static readonly Dictionary<string, Color> DarkColors = new Dictionary<string, Color>
    {
        ["Background"] = new Color(0.1f, 0.1f, 0.1f, 0.85f),
        ["BackgroundLight"] = new Color(0.15f, 0.15f, 0.15f, 0.9f),
        ["Header"] = new Color(0.2f, 0.8f, 1f),
        ["Text"] = Color.white,
        ["TextSecondary"] = new Color(0.8f, 0.8f, 0.8f),
        ["Border"] = new Color(0.3f, 0.3f, 0.3f),
        ["Active"] = Color.cyan,
        ["Inactive"] = Color.gray,
        ["Success"] = Color.green,
        ["Warning"] = new Color(1f, 0.8f, 0f),
        ["Error"] = Color.red,
        ["Selected"] = Color.yellow,
        ["Button"] = new Color(0.2f, 0.2f, 0.2f, 0.9f),
        ["ButtonHover"] = new Color(0.3f, 0.3f, 0.3f, 0.9f)
    };
    
    // Light theme colors
    private static readonly Dictionary<string, Color> LightColors = new Dictionary<string, Color>
    {
        ["Background"] = new Color(0.9f, 0.9f, 0.9f, 0.85f),
        ["BackgroundLight"] = new Color(0.95f, 0.95f, 0.95f, 0.9f),
        ["Header"] = new Color(0.1f, 0.3f, 0.6f),
        ["Text"] = Color.black,
        ["TextSecondary"] = new Color(0.2f, 0.2f, 0.2f),
        ["Border"] = new Color(0.6f, 0.6f, 0.6f),
        ["Active"] = new Color(0f, 0.5f, 1f),
        ["Inactive"] = new Color(0.5f, 0.5f, 0.5f),
        ["Success"] = new Color(0f, 0.6f, 0f),
        ["Warning"] = new Color(0.8f, 0.5f, 0f),
        ["Error"] = new Color(0.8f, 0f, 0f),
        ["Selected"] = new Color(1f, 0.8f, 0f),
        ["Button"] = new Color(0.8f, 0.8f, 0.8f, 0.9f),
        ["ButtonHover"] = new Color(0.7f, 0.7f, 0.7f, 0.9f)
    };
    
    #endregion
    
    #region Cached Styles
    
    private static readonly Dictionary<string, GUIStyle> cachedStyles = new Dictionary<string, GUIStyle>();
    private static readonly Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
    private static ThemeVariant lastTheme = ThemeVariant.Dark;
    
    #endregion
    
    #region Color Access
    
    /// <summary>
    /// Gets a color from the current theme.
    /// </summary>
    /// <param name="colorName">Name of the color</param>
    /// <returns>Color value</returns>
    public static Color GetColor(string colorName)
    {
        var colors = CurrentTheme == ThemeVariant.Dark ? DarkColors : LightColors;
        return colors.TryGetValue(colorName, out Color color) ? color : Color.magenta;
    }
    
    // Convenience properties for commonly used colors
    public static Color Background => GetColor("Background");
    public static Color BackgroundLight => GetColor("BackgroundLight");
    public static Color Header => GetColor("Header");
    public static Color Text => GetColor("Text");
    public static Color TextSecondary => GetColor("TextSecondary");
    public static Color Border => GetColor("Border");
    public static Color Active => GetColor("Active");
    public static Color Inactive => GetColor("Inactive");
    public static Color Success => GetColor("Success");
    public static Color Warning => GetColor("Warning");
    public static Color Error => GetColor("Error");
    public static Color Selected => GetColor("Selected");
    public static Color Button => GetColor("Button");
    public static Color ButtonHover => GetColor("ButtonHover");
    
    #endregion
    
    #region Style Management
    
    /// <summary>
    /// Initializes or refreshes all cached styles. Call this when theme changes.
    /// </summary>
    public static void RefreshStyles()
    {
        if (lastTheme != CurrentTheme)
        {
            ClearCache();
            lastTheme = CurrentTheme;
        }
        
        // Initialize all standard styles
        GetBoxStyle();
        GetHeaderStyle();
        GetTextStyle();
        GetButtonStyle();
        GetSmallButtonStyle();
        GetToggleStyle();
        GetLabelStyle();
        GetTextFieldStyle();
    }
    
    /// <summary>
    /// Clears all cached styles and textures. Use when switching themes.
    /// </summary>
    public static void ClearCache()
    {
        // Destroy cached textures to prevent memory leaks
        foreach (var texture in cachedTextures.Values)
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }
        
        cachedTextures.Clear();
        cachedStyles.Clear();
    }
    
    #endregion
    
    #region Standard Styles
    
    /// <summary>
    /// Gets the standard box style for panels and sections.
    /// </summary>
    public static GUIStyle GetBoxStyle()
    {
        return GetOrCreateStyle("Box", () =>
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = GetTexture("BoxBackground", Background);
            style.padding = new RectOffset(15, 15, 15, 15);
            style.margin = new RectOffset(5, 5, 5, 5);
            style.border = new RectOffset(4, 4, 4, 4);
            return style;
        });
    }
    
    /// <summary>
    /// Gets the header style for section titles.
    /// </summary>
    public static GUIStyle GetHeaderStyle()
    {
        return GetOrCreateStyle("Header", () =>
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Header;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(5, 5, 5, 5);
            return style;
        });
    }
    
    /// <summary>
    /// Gets the standard text style.
    /// </summary>
    public static GUIStyle GetTextStyle()
    {
        return GetOrCreateStyle("Text", () =>
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Text;
            style.fontSize = 12;
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleLeft;
            return style;
        });
    }
    
    /// <summary>
    /// Gets the standard button style.
    /// </summary>
    public static GUIStyle GetButtonStyle()
    {
        return GetOrCreateStyle("Button", () =>
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.background = GetTexture("ButtonNormal", Button);
            style.hover.background = GetTexture("ButtonHover", ButtonHover);
            style.active.background = GetTexture("ButtonActive", Active);
            style.normal.textColor = Text;
            style.fontSize = 12;
            style.padding = new RectOffset(10, 10, 5, 5);
            style.margin = new RectOffset(2, 2, 2, 2);
            return style;
        });
    }
    
    /// <summary>
    /// Gets a smaller button style for compact interfaces.
    /// </summary>
    public static GUIStyle GetSmallButtonStyle()
    {
        return GetOrCreateStyle("SmallButton", () =>
        {
            var style = GetButtonStyle();
            style.fontSize = 10;
            style.padding = new RectOffset(5, 5, 3, 3);
            style.margin = new RectOffset(1, 1, 1, 1);
            return style;
        });
    }
    
    /// <summary>
    /// Gets the toggle button style.
    /// </summary>
    public static GUIStyle GetToggleStyle()
    {
        return GetOrCreateStyle("Toggle", () =>
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.background = GetTexture("ToggleNormal", Inactive);
            style.normal.textColor = Text;
            style.fontSize = 11;
            style.padding = new RectOffset(8, 8, 4, 4);
            style.margin = new RectOffset(2, 2, 2, 2);
            return style;
        });
    }
    
    /// <summary>
    /// Gets the label style for general text.
    /// </summary>
    public static GUIStyle GetLabelStyle()
    {
        return GetOrCreateStyle("Label", () =>
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Text;
            style.fontSize = 11;
            return style;
        });
    }
    
    /// <summary>
    /// Gets the text field style.
    /// </summary>
    public static GUIStyle GetTextFieldStyle()
    {
        return GetOrCreateStyle("TextField", () =>
        {
            var style = new GUIStyle(GUI.skin.textField);
            style.normal.background = GetTexture("TextFieldBackground", BackgroundLight);
            style.normal.textColor = Text;
            style.fontSize = 11;
            style.padding = new RectOffset(5, 5, 3, 3);
            return style;
        });
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets or creates a cached style.
    /// </summary>
    private static GUIStyle GetOrCreateStyle(string name, System.Func<GUIStyle> creator)
    {
        string key = $"{CurrentTheme}_{name}";
        
        if (!cachedStyles.TryGetValue(key, out GUIStyle style) || style == null)
        {
            style = creator();
            cachedStyles[key] = style;
        }
        
        return style;
    }
    
    /// <summary>
    /// Gets or creates a cached texture with the specified color.
    /// Adapts the MakeTexture pattern from GameUI.cs.
    /// </summary>
    public static Texture2D GetTexture(string name, Color color)
    {
        string key = $"{CurrentTheme}_{name}_{ColorUtility.ToHtmlStringRGBA(color)}";
        
        if (!cachedTextures.TryGetValue(key, out Texture2D texture) || texture == null)
        {
            texture = MakeTexture(2, 2, color);
            cachedTextures[key] = texture;
        }
        
        return texture;
    }
    
    /// <summary>
    /// Creates a solid color texture. Adapted from GameUI.cs MakeTexture method.
    /// </summary>
    private static Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Switches to the specified theme and refreshes all styles.
    /// </summary>
    /// <param name="theme">Theme to switch to</param>
    public static void SetTheme(ThemeVariant theme)
    {
        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            RefreshStyles();
        }
    }
    
    /// <summary>
    /// Toggles between dark and light themes.
    /// </summary>
    public static void ToggleTheme()
    {
        SetTheme(CurrentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark);
    }
    
    #endregion
}
