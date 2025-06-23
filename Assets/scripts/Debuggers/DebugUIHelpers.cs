using UnityEngine;
using System;
using static Enumerations;

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

    #region Common Debug UI Patterns

    /// <summary>
    /// Draws target position controls with auto-track player functionality.
    /// This is a common pattern used across multiple debug panels.
    /// </summary>
    /// <param name="label">Label for the controls</param>
    /// <param name="targetPosition">Current target position</param>
    /// <param name="autoTrackPlayer">Whether auto-tracking player is enabled</param>
    /// <param name="playerManager">PlayerManager for auto-tracking</param>
    /// <param name="gridManager">GridManager for bounds checking</param>
    /// <returns>Updated (targetPosition, autoTrackPlayer) values</returns>
    public static (Vector2Int targetPosition, bool autoTrackPlayer) DrawTargetPositionControls(
        string label, Vector2Int targetPosition, bool autoTrackPlayer, 
        PlayerManager playerManager, GridManager gridManager)
    {
        GUILayout.BeginHorizontal();
        bool newAutoTrack = GUILayout.Toggle(autoTrackPlayer, "Track Player");
        GUILayout.EndHorizontal();

        Vector2Int newTargetPosition = targetPosition;

        if (!newAutoTrack)
        {
            // Manual position controls
            newTargetPosition = DrawVector2IntField(label, targetPosition, 
                0, gridManager?.Width - 1 ?? 10, 0, gridManager?.Height - 1 ?? 20);
        }
        else
        {
            // Show following position
            if (playerManager != null)
            {
                newTargetPosition = playerManager.currentTilePosition;
            }
            GUILayout.Label($"Following: ({newTargetPosition.x}, {newTargetPosition.y})");
        }

        return (newTargetPosition, newAutoTrack);
    }

    /// <summary>
    /// Draws a face status selector (Corrupted/Enhanced) commonly used in debug panels.
    /// </summary>
    /// <param name="selectedFaceStatus">Current selection (1=Corrupted, 2=Enhanced)</param>
    /// <returns>Updated face status selection</returns>
    public static int DrawFaceStatusSelector(int selectedFaceStatus)
    {
        GUILayout.BeginHorizontal();
        
        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        bool corruptedClicked = GUILayout.Button("Corrupted");
        
        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        bool enhancedClicked = GUILayout.Button("Enhanced");
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (corruptedClicked) return 1;
        if (enhancedClicked) return 2;
        return selectedFaceStatus;
    }

    /// <summary>
    /// Draws cube selection UI with common action buttons.
    /// </summary>
    /// <param name="cube">Cube to display</param>
    /// <param name="isSelected">Whether this cube is currently selected</param>
    /// <param name="onSelect">Action to call when Select button is clicked</param>
    /// <param name="paintDuration">Duration for face painting operations</param>
    /// <returns>True if any action was performed on the cube</returns>
    public static bool DrawCubeSelectionUI(CubeManager cube, bool isSelected, System.Action onSelect, int paintDuration = 3)
    {
        if (cube == null) return false;

        bool actionPerformed = false;
        Color originalBgColor = GUI.backgroundColor;
        GUI.backgroundColor = isSelected ? SelectedItemColor : GetCubeDisplayColor(cube.type);

        GUILayout.BeginVertical(GUI.skin.box);

        // Header line
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y})", GUILayout.Width(140));

        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            onSelect?.Invoke();
            actionPerformed = true;
        }
        GUILayout.EndHorizontal();

        // Status line
        var activeFace = cube.GetCurrentDownFace();
        var activeStatus = cube.GetActiveFaceStatus();
        var effectiveType = cube.GetEffectiveType();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Face: {activeFace} ({activeStatus})", GUILayout.Width(120));

        if (effectiveType != cube.type)
        {
            GUI.color = Color.yellow;
            GUILayout.Label($"→ {effectiveType}");
            GUI.color = Color.white;
        }
        GUILayout.EndHorizontal();

        // Quick actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("C", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.Corrupted, paintDuration);
            actionPerformed = true;
        }
        if (GUILayout.Button("E", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.Enhanced, paintDuration);
            actionPerformed = true;
        }
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.None, 0);
            actionPerformed = true;
        }
        if (GUILayout.Button("Debug", GUILayout.Width(50)))
        {
            cube.DebugPrintFaceMapping();
            actionPerformed = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.backgroundColor = originalBgColor;
        Space(2);

        return actionPerformed;
    }

    /// <summary>
    /// Gets a display color for cube types - moved from individual panels.
    /// </summary>
    /// <param name="type">Cube type</param>
    /// <returns>Color for UI display</returns>
    public static Color GetCubeDisplayColor(CubeType type)
    {
        switch (type)
        {
            case CubeType.Normal: return new Color(0.8f, 0.8f, 0.8f);
            case CubeType.Blue: return new Color(0.3f, 0.6f, 1f);
            case CubeType.Black: return new Color(0.3f, 0.3f, 0.3f);
            case CubeType.Reinforced: return new Color(0.8f, 0.4f, 0.8f);
            default: return Color.white;
        }
    }

    /// <summary>
    /// Draws a duration control field with common settings used in face painting.
    /// </summary>
    /// <param name="label">Label for the control</param>
    /// <param name="duration">Current duration value</param>
    /// <param name="showPermanentNote">Whether to show the permanent duration note</param>
    /// <returns>Updated duration value</returns>
    public static int DrawDurationControl(string label, int duration, bool showPermanentNote = true)
    {
        int newDuration = DrawIntField(label, duration, -1, 20);
        if (showPermanentNote)
        {
            GUILayout.Label("(-1 = permanent)");
        }
        return newDuration;
    }

    #endregion
}
