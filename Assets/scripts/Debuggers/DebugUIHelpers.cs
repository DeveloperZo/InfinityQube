using UnityEngine;
using System;

/// <summary>
/// Simple utility class for basic debug UI components using Unity's built-in GUI styling.
/// </summary>
public static class DebugUIHelpers
{
    #region Color Constants
    public static readonly Color ActiveToggleColor = Color.cyan;
    public static readonly Color InactiveToggleColor = Color.white;
    public static readonly Color SelectedItemColor = Color.yellow;
    public static readonly Color CorruptedColor = Color.red;
    public static readonly Color EnhancedColor = Color.blue;
    public static readonly Color WarningColor = new Color(1f, 0.8f, 0f);
    public static readonly Color SuccessColor = Color.green;
    #endregion

    #region Simple UI Components

    /// <summary>
    /// Draws a simple toggle button with basic Unity styling.
    /// </summary>
    /// <param name="label">Button text</param>
    /// <param name="current">Current toggle state</param>
    /// <param name="activeColor">Color when active (defaults to cyan)</param>
    /// <returns>New toggle state</returns>
    public static bool DrawToggleButton(string label, bool current, Color? activeColor = null)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = current ? (activeColor ?? ActiveToggleColor) : InactiveToggleColor;
        
        bool clicked = GUILayout.Button(label, GUILayout.Height(25));
        
        GUI.backgroundColor = originalColor;
        return clicked ? !current : current;
    }

    /// <summary>
    /// Draws a simple section with basic Unity styling.
    /// </summary>
    /// <param name="title">Section title</param>
    /// <param name="content">Action to draw section content</param>
    /// <param name="isExpanded">Whether section is currently expanded</param>
    public static void DrawSection(string title, System.Action content, bool isExpanded = true)
    {
        if (!isExpanded) return;

        GUILayout.BeginVertical(GUI.skin.box);
        
        if (!string.IsNullOrEmpty(title))
        {
            GUILayout.Label(title, GUI.skin.label);
            Space(3);
        }
        
        content?.Invoke();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Draws a grid of buttons with basic Unity styling.
    /// </summary>
    /// <param name="buttonData">Array of (label, action) pairs</param>
    /// <param name="buttonsPerRow">Number of buttons per row</param>
    public static void DrawButtonGrid((string label, System.Action action)[] buttonData, int buttonsPerRow = 3)
    {
        for (int i = 0; i < buttonData.Length; i += buttonsPerRow)
        {
            GUILayout.BeginHorizontal();
            
            for (int j = 0; j < buttonsPerRow && i + j < buttonData.Length; j++)
            {
                var (label, action) = buttonData[i + j];
                if (GUILayout.Button(label))
                {
                    action?.Invoke();
                }
            }
            
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Draws simple scrollable content with basic Unity styling.
    /// </summary>
    /// <param name="scrollPosition">Current scroll position</param>
    /// <param name="content">Action to draw scrollable content</param>
    /// <param name="maxHeight">Maximum height (defaults to 300)</param>
    /// <returns>Updated scroll position</returns>
    public static Vector2 DrawScrollableContent(Vector2 scrollPosition, System.Action content, float maxHeight = 300f)
    {
        Vector2 newScrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(maxHeight));
        content?.Invoke();
        GUILayout.EndScrollView();
        return newScrollPosition;
    }

    /// <summary>
    /// Draws an integer field with +/- buttons using basic Unity styling.
    /// </summary>
    /// <param name="label">Field label</param>
    /// <param name="value">Current value</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    /// <param name="step">Step size for +/- buttons</param>
    /// <param name="labelWidth">Width of label</param>
    /// <returns>New value</returns>
    public static int DrawIntField(string label, int value, int min = int.MinValue, int max = int.MaxValue, 
                                   int step = 1, float labelWidth = 60f)
    {
        GUILayout.BeginHorizontal();
        
        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label, GUILayout.Width(labelWidth));
        }
        
        if (GUILayout.Button("-", GUILayout.Width(20)) && value > min)
        {
            value -= step;
        }
        
        string valueStr = GUILayout.TextField(value.ToString(), GUILayout.Width(40));
        if (int.TryParse(valueStr, out int newValue))
        {
            value = Mathf.Clamp(newValue, min, max);
        }
        
        if (GUILayout.Button("+", GUILayout.Width(20)) && value < max)
        {
            value += step;
        }
        
        GUILayout.EndHorizontal();
        
        return Mathf.Clamp(value, min, max);
    }

    /// <summary>
    /// Draws a simple status indicator with color coding.
    /// </summary>
    /// <param name="label">Status label</param>
    /// <param name="isActive">Whether status is active</param>
    /// <param name="activeText">Text when active</param>
    /// <param name="inactiveText">Text when inactive</param>
    /// <param name="activeColor">Color when active</param>
    /// <param name="inactiveColor">Color when inactive</param>
    public static void DrawStatusIndicator(string label, bool isActive, 
                                          string activeText = "ACTIVE", string inactiveText = "INACTIVE",
                                          Color? activeColor = null, Color? inactiveColor = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}:");
        
        Color originalColor = GUI.color;
        GUI.color = isActive ? (activeColor ?? SuccessColor) : (inactiveColor ?? Color.gray);
        GUILayout.Label(isActive ? activeText : inactiveText);
        GUI.color = originalColor;
        
        GUILayout.EndHorizontal();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Temporarily changes GUI color and restores it automatically.
    /// </summary>
    /// <param name="color">Color to use</param>
    /// <param name="action">Action to execute with the color</param>
    public static void WithColor(Color color, System.Action action)
    {
        Color originalColor = GUI.color;
        GUI.color = color;
        action?.Invoke();
        GUI.color = originalColor;
    }

    /// <summary>
    /// Temporarily changes GUI background color and restores it automatically.
    /// </summary>
    /// <param name="backgroundColor">Background color to use</param>
    /// <param name="action">Action to execute with the background color</param>
    public static void WithBackgroundColor(Color backgroundColor, System.Action action)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = backgroundColor;
        action?.Invoke();
        GUI.backgroundColor = originalColor;
    }

    /// <summary>
    /// Adds consistent spacing between UI elements.
    /// </summary>
    /// <param name="pixels">Number of pixels of space (defaults to 5)</param>
    public static void Space(float pixels = 5f)
    {
        GUILayout.Space(pixels);
    }

    /// <summary>
    /// Draws a simple horizontal separator line.
    /// </summary>
    public static void DrawSeparator()
    {
        GUILayout.Space(5);
        
        Rect rect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 1);
        Color originalColor = GUI.color;
        GUI.color = Color.gray;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = originalColor;
        
        GUILayout.Space(5);
    }
    
    #endregion
    
    #region Simple Initialization
    
    /// <summary>
    /// Simple initialization - no complex theme setup needed.
    /// </summary>
    public static void InitializeTheme()
    {
        // No complex initialization needed - using basic Unity GUI
        Debug.Log("DebugUIHelpers: Using simple Unity GUI styling");
    }

    /// <summary>
    /// Draws a Vector2Int field with +/- buttons for X and Y using basic Unity styling.
    /// </summary>
    /// <param name="label">Field label</param>
    /// <param name="targetPosition">Current Vector2Int value</param>
    /// <param name="minWidth">Minimum X value</param>
    /// <param name="maxWidth">Maximum X value</param>
    /// <param name="minHeight">Minimum Y value</param>
    /// <param name="maxHeight">Maximum Y value</param>
    /// <returns>New Vector2Int value</returns>
    public static Vector2Int DrawVector2IntField(string label, Vector2Int targetPosition, int minWidth, int maxWidth, int minHeight, int maxHeight)
    {
        GUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label, GUILayout.Width(60f));
        }

        // X field
        GUILayout.Label("X", GUILayout.Width(12f));
        if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.x > minWidth)
        {
            targetPosition.x--;
        }
        string xStr = GUILayout.TextField(targetPosition.x.ToString(), GUILayout.Width(40));
        if (int.TryParse(xStr, out int newX))
        {
            targetPosition.x = Mathf.Clamp(newX, minWidth, maxWidth);
        }
        if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.x < maxWidth)
        {
            targetPosition.x++;
        }

        // Y field
        GUILayout.Label("Y", GUILayout.Width(12f));
        if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.y > minHeight)
        {
            targetPosition.y--;
        }
        string yStr = GUILayout.TextField(targetPosition.y.ToString(), GUILayout.Width(40));
        if (int.TryParse(yStr, out int newY))
        {
            targetPosition.y = Mathf.Clamp(newY, minHeight, maxHeight);
        }
        if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.y < maxHeight)
        {
            targetPosition.y++;
        }

        GUILayout.EndHorizontal();
        return new Vector2Int(
            Mathf.Clamp(targetPosition.x, minWidth, maxWidth),
            Mathf.Clamp(targetPosition.y, minHeight, maxHeight)
        );
    }
    public static Vector2 DrawVector2Field(string label, Vector2 targetPosition, int minWidth, int maxWidth, int minHeight, int maxHeight)
    {
        GUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label, GUILayout.Width(60f));
        }

        // X field
        GUILayout.Label("X", GUILayout.Width(12f));
        if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.x > minWidth)
        {
            targetPosition.x--;
        }
        string xStr = GUILayout.TextField(targetPosition.x.ToString(), GUILayout.Width(40));
        if (int.TryParse(xStr, out int newX))
        {
            targetPosition.x = Mathf.Clamp(newX, minWidth, maxWidth);
        }
        if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.x < maxWidth)
        {
            targetPosition.x++;
        }

        // Y field
        GUILayout.Label("Y", GUILayout.Width(12f));
        if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.y > minHeight)
        {
            targetPosition.y--;
        }
        string yStr = GUILayout.TextField(targetPosition.y.ToString(), GUILayout.Width(40));
        if (int.TryParse(yStr, out int newY))
        {
            targetPosition.y = Mathf.Clamp(newY, minHeight, maxHeight);
        }
        if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.y < maxHeight)
        {
            targetPosition.y++;
        }

        GUILayout.EndHorizontal();
        return new Vector2(
            Mathf.Clamp(targetPosition.x, minWidth, maxWidth),
            Mathf.Clamp(targetPosition.y, minHeight, maxHeight)
        );
    }

    #endregion
}
