using UnityEngine;
using UnityEditor;

/// <summary>
/// Resets static state for fast Play Mode iteration (when Domain Reload is disabled).
/// Use Menu: Tools > Reset Play State (Ctrl+Shift+R) before playing if you see stale state.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStateReset
{
    static PlayModeStateReset()
    {
        // Auto-reset when entering Play Mode
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Reset before entering Play Mode
            ResetAllStaticState();
        }
    }

    [MenuItem("Tools/Reset Play State %#r")] // Ctrl+Shift+R
    public static void ResetAllStaticState()
    {
        Debug.Log("[PlayModeStateReset] Resetting static state for clean Play Mode...");

        // Reset GameEvents (clear all subscribers)
        ResetGameEvents();

        // Log completion
        Debug.Log("[PlayModeStateReset] Static state reset complete.");
    }

    private static void ResetGameEvents()
    {
        // Use reflection to clear static event delegates in GameEvents
        var gameEventsType = typeof(GameEvents);
        if (gameEventsType == null)
        {
            Debug.LogWarning("[PlayModeStateReset] GameEvents type not found");
            return;
        }

        // Get all static events and clear them
        var fields = gameEventsType.GetFields(
            System.Reflection.BindingFlags.Static | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic);

        int clearedCount = 0;
        foreach (var field in fields)
        {
            if (typeof(System.Delegate).IsAssignableFrom(field.FieldType))
            {
                field.SetValue(null, null);
                clearedCount++;
            }
        }

        Debug.Log($"[PlayModeStateReset] Cleared {clearedCount} GameEvents delegates");
    }
}
