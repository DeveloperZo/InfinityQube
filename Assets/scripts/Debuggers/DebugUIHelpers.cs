using UnityEngine;
using System;

/// <summary>
/// Centralized utility class for standardized debug UI components.
/// Eliminates code duplication across debug panels and provides consistent styling.
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

    #region Common UI Components

    /// <summary>
    /// Draws a toggle button with consistent styling and behavior.
    /// This replaces the identical DrawToggleButton method duplicated across 7 panels.
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
    /// Draws a standardized section with consistent boxing and header styling.
    /// </summary>
    /// <param name="title">Section title</param>
    /// <param name="content">Action to draw section content</param>
    /// <param name="isExpanded">Whether section is currently expanded</param>
    public static void DrawSection(string title, System.Action content, bool isExpanded = true)
    {
        if (!isExpanded) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title, GUI.skin.box);
        content?.Invoke();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Draws a grid of buttons with consistent spacing and layout.
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
    /// Draws scrollable content with consistent styling and height management.
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
    /// Draws an integer field with +/- buttons for easy adjustment.
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
    /// Draws a Vector2Int field with individual X/Y controls.
    /// </summary>
    /// <param name="label">Field label</param>
    /// <param name="value">Current value</param>
    /// <param name="minX">Minimum X value</param>
    /// <param name="maxX">Maximum X value</param>
    /// <param name="minY">Minimum Y value</param>
    /// <param name="maxY">Maximum Y value</param>
    /// <returns>New value</returns>
    public static Vector2Int DrawVector2IntField(string label, Vector2Int value, 
                                                int minX = 0, int maxX = 100, 
                                                int minY = 0, int maxY = 100)
    {
        GUILayout.BeginHorizontal();
        
        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label, GUILayout.Width(80));
        }
        
        GUILayout.Label("X:", GUILayout.Width(15));
        string xStr = GUILayout.TextField(value.x.ToString(), GUILayout.Width(30));
        if (int.TryParse(xStr, out int newX))
        {
            value.x = Mathf.Clamp(newX, minX, maxX);
        }
        
        GUILayout.Label("Y:", GUILayout.Width(15));
        string yStr = GUILayout.TextField(value.y.ToString(), GUILayout.Width(30));
        if (int.TryParse(yStr, out int newY))
        {
            value.y = Mathf.Clamp(newY, minY, maxY);
        }
        
        GUILayout.EndHorizontal();
        
        return value;
    }

    /// <summary>
    /// Draws a status indicator with color coding.
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

    /// <summary>
    /// Draws a list of items with selection and action buttons.
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="items">List of items</param>
    /// <param name="selectedItem">Currently selected item</param>
    /// <param name="drawItem">Function to draw each item</param>
    /// <param name="onSelect">Action when item is selected</param>
    /// <param name="maxVisible">Maximum visible items</param>
    /// <param name="scrollPosition">Scroll position</param>
    /// <returns>Updated scroll position</returns>
    public static Vector2 DrawSelectableList<T>(System.Collections.Generic.IList<T> items, T selectedItem,
                                                System.Func<T, string> drawItem,
                                                System.Action<T> onSelect = null,
                                                int maxVisible = 10,
                                                Vector2 scrollPosition = default)
    {
        if (items == null || items.Count == 0)
        {
            GUILayout.Label("No items found");
            return scrollPosition;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(maxVisible * 25));

        foreach (T item in items)
        {
            bool isSelected = item != null && item.Equals(selectedItem);
            
            Color originalBgColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = SelectedItemColor;
            }

            GUILayout.BeginHorizontal(GUI.skin.box);
            
            string itemText = drawItem?.Invoke(item) ?? item?.ToString() ?? "null";
            GUILayout.Label(itemText);
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                onSelect?.Invoke(item);
            }
            
            GUILayout.EndHorizontal();
            GUI.backgroundColor = originalBgColor;
        }

        GUILayout.EndScrollView();
        return scrollPosition;
    }

    /// <summary>
    /// Draws a progress bar with text overlay.
    /// </summary>
    /// <param name="label">Progress bar label</param>
    /// <param name="current">Current value</param>
    /// <param name="max">Maximum value</param>
    /// <param name="width">Bar width</param>
    /// <param name="height">Bar height</param>
    public static void DrawProgressBar(string label, float current, float max, float width = 200f, float height = 20f)
    {
        if (!string.IsNullOrEmpty(label))
        {
            GUILayout.Label(label);
        }

        Rect rect = GUILayoutUtility.GetRect(width, height);
        
        // Background
        GUI.Box(rect, "");
        
        // Progress fill
        float progress = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        Rect fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * progress, rect.height - 4);
        
        Color fillColor = progress > 0.66f ? SuccessColor : progress > 0.33f ? WarningColor : CorruptedColor;
        Color originalColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = originalColor;
        
        // Text overlay
        string progressText = $"{current:F0}/{max:F0} ({progress:P0})";
        GUI.Label(rect, progressText, GUI.skin.label);
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
    /// Draws a horizontal separator line.
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
}
