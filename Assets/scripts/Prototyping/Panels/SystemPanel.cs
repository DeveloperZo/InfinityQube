using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// System Panel - Monitoring and debugging.
/// </summary>
public class SystemPanel : PrototypingPanelBase
{
    public override string PanelName => "System";
    public override string PanelIcon => "Sys";
    public override PrototypingCategory Category => PrototypingCategory.System;
    public override int Priority => 50;
    
    private float fps;
    private float fpsTimer;
    
    private bool showPerf = true;
    private bool showManagers = false;
    private bool showGameplayToggles = true;
    private bool showPresets = false;
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void Update()
    {
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fps = 1f / Time.unscaledDeltaTime;
            fpsTimer = 0;
        }
    }
    
    public override void DrawGUI()
    {
        DrawStatus("System monitoring and debugging");
        
        GUILayout.Space(5);
        
        // Performance
        showPerf = DrawToggleSection("PERFORMANCE", showPerf);
        if (showPerf)
        {
            DrawSection("", () =>
            {
                GUILayout.Label($"FPS: {fps:F1}");
                GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / (1024f * 1024f):F1} MB");
                GUILayout.Label($"Time Scale: {Time.timeScale:F1}x");
                GUILayout.Label($"Active Cubes: {waveManager?.activeCubes?.Count ?? 0}");
            });
        }
        
        // Managers
        showManagers = DrawToggleSection("MANAGER STATUS", showManagers);
        if (showManagers)
        {
            DrawSection("", () =>
            {
                DrawManagerStatus("GridManager", gridManager);
                DrawManagerStatus("WaveManager", waveManager);
                DrawManagerStatus("PlayerManager", playerManager);
                DrawManagerStatus("StageManager", stageManager);
            });
        }
        
        // Gameplay Toggles
        showGameplayToggles = DrawToggleSection("GAMEPLAY TOGGLES", showGameplayToggles);
        if (showGameplayToggles)
        {
            DrawSection("", () =>
            {
                DrawGameplayToggles();
            });
        }
        
        // Presets
        showPresets = DrawToggleSection("PRESETS", showPresets);
        if (showPresets)
        {
            DrawSection("", () =>
            {
                var presets = PrototypingPresets.Instance;
                var names = presets.GetPresetNames();
                
                if (names.Count == 0)
                {
                    GUILayout.Label("No saved presets");
                }
                else
                {
                    foreach (var name in names.Take(5))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(name);
                        if (GUILayout.Button("Load", GUILayout.Width(50)))
                        {
                            presets.LoadPreset(name);
                        }
                        if (GUILayout.Button("Del", GUILayout.Width(40)))
                        {
                            presets.DeletePreset(name);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                
                GUILayout.Space(5);
                DrawButtonRow(
                    ("Validate Grid", ValidateGrid),
                    ("System Report", SystemReport)
                );
            });
        }
    }
    
    private void DrawManagerStatus(string name, object manager)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(120));
        var color = GUI.color;
        GUI.color = manager != null ? Color.green : Color.red;
        GUILayout.Label(manager != null ? "✓ OK" : "✗ NULL");
        GUI.color = color;
        GUILayout.EndHorizontal();
    }
    
    private void ValidateGrid()
    {
        if (gridManager == null) { Debug.LogWarning("GridManager not available"); return; }
        Debug.Log($"Grid Validation: {gridManager.Width}x{gridManager.Height}, Ready: {gridManager.IsGridReady}");
    }
    
    private void SystemReport()
    {
        Debug.Log("=== SYSTEM REPORT ===");
        Debug.Log($"Unity: {Application.unityVersion}");
        Debug.Log($"FPS: {fps:F1}");
        Debug.Log($"Memory: {System.GC.GetTotalMemory(false) / (1024f * 1024f):F1} MB");
        if (gridManager != null) Debug.Log($"Grid: {gridManager.Width}x{gridManager.Height}");
        if (waveManager != null) Debug.Log($"Cubes: {waveManager.activeCubes?.Count ?? 0}");
        Debug.Log("=== END REPORT ===");
    }
    
    #region Gameplay Toggles
    
    private void DrawGameplayToggles()
    {
        // Line Divider
        GUILayout.Label("Line Divider:");
        GUILayout.BeginHorizontal();
        
        bool lineDividerEnabled = gridManager?.LineDividerEnabled ?? false;
        GUI.backgroundColor = lineDividerEnabled ? Color.green : Color.gray;
        if (GUILayout.Button(lineDividerEnabled ? "ON" : "OFF", GUILayout.Width(50), GUILayout.Height(25)))
        {
            gridManager?.SetLineDividerEnabled(!lineDividerEnabled);
            LogAction($"Line Divider: {(!lineDividerEnabled ? "ON" : "OFF")}");
        }
        GUI.backgroundColor = Color.white;
        
        if (lineDividerEnabled && gridManager != null)
        {
            GUILayout.Label($"Row: {gridManager.LineDividerRow}", GUILayout.Width(60));
            if (GUILayout.Button("Up", GUILayout.Width(35)))
            {
                gridManager.MoveLineDivider(1, true);
            }
            if (GUILayout.Button("Down", GUILayout.Width(45)))
            {
                gridManager.MoveLineDivider(-1, false);
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Game state info
        GUILayout.Label("Game State:");
        int phaseableCubes = CountPhaseableCubes();
        GUILayout.Label($"  Phaseable Infinity cubes: {phaseableCubes}");
        GUILayout.Label($"  Active cubes: {waveManager?.activeCubes?.Count ?? 0}");
    }
    
    private int CountPhaseableCubes()
    {
        if (waveManager?.activeCubes == null) return 0;
        
        int count = 0;
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.type == CubeType.Infinity && cube.IsPhaseable())
            {
                count++;
            }
        }
        return count;
    }
    
    #endregion
}
