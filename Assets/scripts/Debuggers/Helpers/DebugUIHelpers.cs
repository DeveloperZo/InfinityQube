using UnityEngine;
using System;
using static Enumerations;

/// <summary>
/// Shared UI utility methods for debug panels.
/// Provides consistent styling, colors, and common UI patterns.
/// </summary>
public static class DebugUIHelpers
{
    #region Color Constants
    public static readonly Color SuccessColor = new Color(0.4f, 0.8f, 0.4f);
    public static readonly Color WarningColor = new Color(0.9f, 0.8f, 0.3f);
    public static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f);
    public static readonly Color CorruptedColor = new Color(0.8f, 0.3f, 0.3f);
    public static readonly Color EnhancedColor = new Color(0.3f, 0.3f, 0.8f);
    public static readonly Color InfoColor = new Color(0.7f, 0.7f, 0.9f);
    public static readonly Color SelectedItemColor = Color.yellow;
    #endregion

    #region Color and Style Utilities

    /// <summary>
    /// Executes an action with a temporary color override
    /// </summary>
    public static void WithColor(Color color, System.Action action)
    {
        Color originalColor = GUI.color;
        GUI.color = color;
        try
        {
            action();
        }
        finally
        {
            GUI.color = originalColor;
        }
    }

    /// <summary>
    /// Executes an action with a temporary background color override
    /// </summary>
    public static void WithBackgroundColor(Color color, System.Action action)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        try
        {
            action();
        }
        finally
        {
            GUI.backgroundColor = originalColor;
        }
    }

    /// <summary>
    /// Gets appropriate display color for a cube type
    /// </summary>
    public static Color GetCubeDisplayColor(CubeType cubeType)
    {
        switch (cubeType)
        {
            case CubeType.Unit: return Color.gray;
            case CubeType.Prime: return new Color(0.8f, 0.8f, 0.8f);
            case CubeType.Recursion: return new Color(0.6f, 0.4f, 0.2f);
            case CubeType.Infinity: return new Color(0.3f, 0.3f, 0.3f);
            default: return Color.gray;
        }
    }

    #endregion

    #region Layout Utilities

    /// <summary>
    /// Adds consistent spacing
    /// </summary>
    public static void Space(float pixels = 10f)
    {
        GUILayout.Space(pixels);
    }

    /// <summary>
    /// Draws a horizontal separator line
    /// </summary>
    public static void DrawSeparator()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }

    #endregion

    #region Toggle and Button Utilities

    /// <summary>
    /// Draws a styled toggle button with optional background color
    /// </summary>
    public static bool DrawToggleButton(string label, bool currentValue, Color? activeColor = null)
    {
        Color bgColor = currentValue ? (activeColor ?? SuccessColor) : Color.white;
        
        bool result = currentValue;
        WithBackgroundColor(bgColor, () =>
        {
            result = GUILayout.Button(label, GUILayout.Height(25));
        });
        
        return result ? !currentValue : currentValue;
    }

    /// <summary>
    /// Draws a simple toggle for section visibility
    /// </summary>
    public static bool DrawSimpleToggle(string label, bool currentValue)
    {
        return GUILayout.Toggle(currentValue, label);
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

    #endregion

    #region Input Field Utilities
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
    /// <summary>
    /// Draws an integer input field with label and constraints
    /// </summary>
    public static int DrawIntField(string label, int currentValue, int min = int.MinValue, int max = int.MaxValue)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        
        string stringValue = GUILayout.TextField(currentValue.ToString(), GUILayout.Width(60));
        
        if (int.TryParse(stringValue, out int newValue))
        {
            newValue = Mathf.Clamp(newValue, min, max);
        }
        else
        {
            newValue = currentValue;
        }
        
        GUILayout.EndHorizontal();
        return newValue;
    }

    /// <summary>
    /// Draws a duration control with predefined options
    /// </summary>
    public static int DrawDurationControl(string label, int currentDuration)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        
        // Quick duration buttons
        if (GUILayout.Button("1s", GUILayout.Width(30))) currentDuration = 1;
        if (GUILayout.Button("3s", GUILayout.Width(30))) currentDuration = 3;
        if (GUILayout.Button("5s", GUILayout.Width(30))) currentDuration = 5;
        if (GUILayout.Button("∞", GUILayout.Width(30))) currentDuration = -1;
        
        // Custom input
        string durationStr = GUILayout.TextField(currentDuration.ToString(), GUILayout.Width(40));
        if (int.TryParse(durationStr, out int parsed))
        {
            currentDuration = parsed;
        }
        
        GUILayout.EndHorizontal();
        return currentDuration;
    }

    /// <summary>
    /// Draws a face status selector
    /// </summary>
    public static int DrawFaceStatusSelector(int selectedStatus)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Status:", GUILayout.Width(50));
        
        WithBackgroundColor(selectedStatus == 1 ? CorruptedColor : Color.white, () =>
        {
            if (GUILayout.Button("Corrupted", GUILayout.Width(80)))
                selectedStatus = 1;
        });
        
        WithBackgroundColor(selectedStatus == 2 ? EnhancedColor : Color.white, () =>
        {
            if (GUILayout.Button("Enhanced", GUILayout.Width(80)))
                selectedStatus = 2;
        });
        
        GUILayout.EndHorizontal();
        return selectedStatus;
    }

    #endregion

    #region Position and Target Controls

    /// <summary>
    /// Draws target position controls with auto-tracking option
    /// </summary>
    public static (Vector2Int position, bool autoTrack) DrawTargetPositionControls(
        string label, Vector2Int currentPosition, bool autoTrack, 
        PlayerManager playerManager, GridManager gridManager)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(label, GUI.skin.box);
        
        // Auto-track toggle
        bool newAutoTrack = GUILayout.Toggle(autoTrack, "Auto-track player");
        
        // Position display and manual controls
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Position: ({currentPosition.x}, {currentPosition.y})", GUILayout.Width(120));
        
        Vector2Int newPosition = currentPosition;
        
        if (!newAutoTrack)
        {
            // Manual position controls
            if (GUILayout.Button("←", GUILayout.Width(25))) newPosition.x--;
            if (GUILayout.Button("→", GUILayout.Width(25))) newPosition.x++;
            if (GUILayout.Button("↓", GUILayout.Width(25))) newPosition.y--;
            if (GUILayout.Button("↑", GUILayout.Width(25))) newPosition.y++;
        }
        
        if (GUILayout.Button("To Player", GUILayout.Width(70)) && playerManager != null)
        {
            newPosition = playerManager.currentTilePosition;
            newAutoTrack = false;
        }
        
        GUILayout.EndHorizontal();
        
        // Validate position
        if (gridManager != null && !gridManager.IsValidGridPosition(newPosition))
        {
            WithColor(ErrorColor, () =>
            {
                GUILayout.Label($"Position ({newPosition.x}, {newPosition.y}) is outside grid bounds!");
            });
        }
        
        GUILayout.EndVertical();
        
        return (newPosition, newAutoTrack);
    }

    #endregion

    #region Status Display Utilities

    /// <summary>
    /// Draws a status indicator with color coding
    /// </summary>
    public static void DrawStatusIndicator(string label, bool isGood, string statusText = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        
        Color statusColor = isGood ? SuccessColor : ErrorColor;
        string displayText = statusText ?? (isGood ? "OK" : "ERROR");
        
        WithColor(statusColor, () =>
        {
            GUILayout.Label(displayText);
        });
        
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws a progress bar with label
    /// </summary>
    public static void DrawProgressBar(string label, float progress, string progressText = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        
        // Draw progress bar background
        Rect progressRect = GUILayoutUtility.GetRect(100, 20);
        GUI.Box(progressRect, "");
        
        // Draw progress fill
        Rect fillRect = new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height);
        WithBackgroundColor(SuccessColor, () =>
        {
            GUI.Box(fillRect, "");
        });
        
        // Draw progress text
        string displayText = progressText ?? $"{progress:P0}";
        GUI.Label(progressRect, displayText, GUI.skin.label);
        
        GUILayout.EndHorizontal();
    }

    #endregion

    #region Grid and World Position Utilities

    /// <summary>
    /// Converts grid position to display string
    /// </summary>
    public static string GridPositionToString(Vector2Int gridPos)
    {
        return $"({gridPos.x}, {gridPos.y})";
    }

    /// <summary>
    /// Converts world position to display string
    /// </summary>
    public static string WorldPositionToString(Vector3 worldPos)
    {
        return $"({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})";
    }

    /// <summary>
    /// Draws position information in a consistent format
    /// </summary>
    public static void DrawPositionInfo(string label, Vector2Int gridPos, Vector3? worldPos = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        GUILayout.Label($"Grid: {GridPositionToString(gridPos)}", GUILayout.Width(100));
        
        if (worldPos.HasValue)
        {
            GUILayout.Label($"World: {WorldPositionToString(worldPos.Value)}");
        }
        
        GUILayout.EndHorizontal();
    }

    #endregion

    #region Validation Utilities

    /// <summary>
    /// Validates and clamps a grid position to valid bounds
    /// </summary>
    public static Vector2Int ValidateGridPosition(Vector2Int position, GridManager gridManager)
    {
        if (gridManager == null) return position;
        
        int clampedX = Mathf.Clamp(position.x, 0, gridManager.Width - 1);
        int clampedY = Mathf.Clamp(position.y, 0, gridManager.Height - 1);
        
        return new Vector2Int(clampedX, clampedY);
    }

    /// <summary>
    /// Checks if a position is within valid grid bounds
    /// </summary>
    public static bool IsValidGridPosition(Vector2Int position, GridManager gridManager)
    {
        if (gridManager == null) return false;
        return gridManager.IsValidGridPosition(position);
    }

    #endregion
}
