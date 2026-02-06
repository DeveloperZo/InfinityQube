using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// System Panel - Monitoring and debugging.
/// Provides performance metrics, manager status, and debug toggles.
/// </summary>
public class SystemPanel : PrototypingPanelBase
{
    public override string PanelName => "System";
    public override string PanelIcon => "Sys";
    public override PrototypingCategory Category => PrototypingCategory.System;
    public override int Priority => 50;
    
    // Performance tracking
    private float fps;
    private float fpsMin = float.MaxValue;
    private float fpsMax = 0;
    private float fpsTimer;
    private float[] fpsHistory = new float[30];
    private int fpsHistoryIndex = 0;
    
    // Section toggles
    private bool showPerf = true;
    private bool showManagers = false;
    private bool showDebugToggles = true;
    private bool showGameplayToggles = true;
    private bool showPresets = false;
    
    // Cached references for additional managers
    private PlayerActionManager actionManager;
    private AudioManager audioManager;
    
    public override void Initialize()
    {
        base.Initialize();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        audioManager = Object.FindFirstObjectByType<AudioManager>();
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void Update()
    {
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.1f) // Update more frequently for smoother graph
        {
            fps = 1f / Time.unscaledDeltaTime;
            fpsMin = Mathf.Min(fpsMin, fps);
            fpsMax = Mathf.Max(fpsMax, fps);
            
            // Update history for graph
            fpsHistory[fpsHistoryIndex] = fps;
            fpsHistoryIndex = (fpsHistoryIndex + 1) % fpsHistory.Length;
            
            fpsTimer = 0;
        }
    }
    
    public override void DrawGUI()
    {
        // Compact status with key metrics
        string status = $"FPS: {fps:F0} | Mem: {System.GC.GetTotalMemory(false) / (1024f * 1024f):F0}MB | Time: {Time.timeScale:F1}x";
        DrawStatus(status);
        
        GUILayout.Space(5);
        
        // Performance
        showPerf = DrawToggleSection("PERFORMANCE", showPerf);
        if (showPerf)
        {
            DrawPerformanceSection();
        }
        
        // Debug Toggles - Manager debug options
        showDebugToggles = DrawToggleSection("DEBUG TOGGLES", showDebugToggles);
        if (showDebugToggles)
        {
            DrawDebugToggles();
        }
        
        // Managers
        showManagers = DrawToggleSection("MANAGER STATUS", showManagers);
        if (showManagers)
        {
            DrawManagerSection();
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
        showPresets = DrawToggleSection("PRESETS & TOOLS", showPresets);
        if (showPresets)
        {
            DrawPresetsSection();
        }
    }
    
    #region Performance Section
    private void DrawPerformanceSection()
    {
        DrawSection("", () =>
        {
            // FPS with color coding
            Color fpsColor = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
            GUI.color = fpsColor;
            GUILayout.Label($"FPS: {fps:F1} (Min: {fpsMin:F0} / Max: {fpsMax:F0})");
            GUI.color = Color.white;
            
            // Simple FPS bar graph
            GUILayout.BeginHorizontal();
            GUILayout.Label("Graph:", GUILayout.Width(45));
            float graphWidth = 200f;
            float barWidth = graphWidth / fpsHistory.Length;
            
            Rect graphRect = GUILayoutUtility.GetRect(graphWidth, 20);
            GUI.Box(graphRect, "");
            
            for (int i = 0; i < fpsHistory.Length; i++)
            {
                int idx = (fpsHistoryIndex + i) % fpsHistory.Length;
                float normalizedFps = Mathf.Clamp01(fpsHistory[idx] / 120f);
                float barHeight = normalizedFps * 18f;
                
                Color barColor = fpsHistory[idx] >= 60 ? Color.green : (fpsHistory[idx] >= 30 ? Color.yellow : Color.red);
                
                Rect barRect = new Rect(graphRect.x + i * barWidth, graphRect.y + 18f - barHeight, barWidth - 1, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                fpsMin = fps;
                fpsMax = fps;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(3);
            
            // Memory
            long memoryBytes = System.GC.GetTotalMemory(false);
            GUILayout.Label($"Memory: {memoryBytes / (1024f * 1024f):F1} MB (GC Gen0: {System.GC.CollectionCount(0)})");
            
            // Object counts
            GUILayout.Label($"Active Cubes: {waveManager?.activeCubes?.Count ?? 0}");
            
            int playerCubeCount = actionManager?.MarkerSystem?.playerCubes?.Count(c => c != null && !c.isDestroyed) ?? 0;
            GUILayout.Label($"Player Cubes: {playerCubeCount}");
            
            int markerCount = gridManager?.GetMarkerCount() ?? 0;
            GUILayout.Label($"Active Markers: {markerCount}");
        });
    }
    #endregion
    
    #region Debug Toggles
    private void DrawDebugToggles()
    {
        DrawSection("", () =>
        {
            // WaveManager debug mode
            if (waveManager != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("WaveManager Debug:", GUILayout.Width(130));
                GUI.backgroundColor = waveManager.debugMode ? Color.green : Color.gray;
                if (GUILayout.Button(waveManager.debugMode ? "ON" : "OFF", GUILayout.Width(50)))
                {
                    waveManager.EnterDebugMode(!waveManager.debugMode);
                    LogAction($"WaveManager debug: {(waveManager.debugMode ? "ON" : "OFF")}");
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            // GridManager debug
            if (gridManager != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("GridManager Debug:", GUILayout.Width(130));
                bool gridDebug = gridManager.EnableDebugLogs;
                GUI.backgroundColor = gridDebug ? Color.green : Color.gray;
                if (GUILayout.Button(gridDebug ? "ON" : "OFF", GUILayout.Width(50)))
                {
                    gridManager.EnableDebugLogs = !gridDebug;
                    LogAction($"GridManager debug: {(!gridDebug ? "ON" : "OFF")}");
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            // Audio debug
            if (audioManager != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("AudioManager Debug:", GUILayout.Width(130));
                bool audioDebug = audioManager.EnableDebugLogs;
                GUI.backgroundColor = audioDebug ? Color.green : Color.gray;
                if (GUILayout.Button(audioDebug ? "ON" : "OFF", GUILayout.Width(50)))
                {
                    audioManager.EnableDebugLogs = !audioDebug;
                    LogAction($"AudioManager debug: {(!audioDebug ? "ON" : "OFF")}");
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(5);
            
            // Quick toggle all
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All Debug"))
            {
                SetAllDebugLogs(true);
            }
            if (GUILayout.Button("Disable All Debug"))
            {
                SetAllDebugLogs(false);
            }
            GUILayout.EndHorizontal();
        });
    }
    
    private void SetAllDebugLogs(bool enabled)
    {
        if (waveManager != null) waveManager.EnterDebugMode(enabled);
        if (gridManager != null) gridManager.EnableDebugLogs = enabled;
        if (audioManager != null) audioManager.EnableDebugLogs = enabled;
        LogAction($"All debug logs: {(enabled ? "ON" : "OFF")}");
    }
    #endregion
    
    #region Manager Status
    private void DrawManagerSection()
    {
        DrawSection("", () =>
        {
            DrawManagerStatus("GridManager", gridManager, gridManager?.IsGridReady.ToString() ?? "N/A");
            DrawManagerStatus("WaveManager", waveManager, waveManager?.waveActive.ToString() ?? "N/A");
            DrawManagerStatus("PlayerManager", playerManager);
            DrawManagerStatus("StageManager", stageManager, stageManager?.CurrentStage?.stageName ?? "No stage");
            DrawManagerStatus("PlayerActionManager", actionManager, actionManager?.GetCurrentMode().ToString() ?? "N/A");
            DrawManagerStatus("AudioManager", audioManager);
            
            GUILayout.Space(5);
            DrawButtonRow(
                ("Refresh Refs", RefreshManagerRefs),
                ("System Report", SystemReport)
            );
        });
    }
    
    private void DrawManagerStatus(string name, object manager, string extraInfo = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(140));
        var color = GUI.color;
        GUI.color = manager != null ? Color.green : Color.red;
        GUILayout.Label(manager != null ? "✓" : "✗", GUILayout.Width(20));
        GUI.color = color;
        
        if (extraInfo != null && manager != null)
        {
            GUILayout.Label(extraInfo, GUILayout.Width(100));
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void RefreshManagerRefs()
    {
        CacheManagerReferences();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        audioManager = Object.FindFirstObjectByType<AudioManager>();
        LogAction("Manager references refreshed");
    }
    #endregion
    
    #region Gameplay Toggles
    private void DrawGameplayToggles()
    {
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
    
    #region Presets Section
    private void DrawPresetsSection()
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
            
            GUILayout.Label("Tools:");
            DrawButtonRow(
                ("Validate Grid", ValidateGrid),
                ("Force GC", ForceGarbageCollection)
            );
            
            DrawButtonRow(
                ("Screenshot", TakeScreenshot),
                ("Print Hierarchy", PrintHierarchy)
            );
        });
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
        Debug.Log($"FPS: {fps:F1} (Min: {fpsMin:F0} / Max: {fpsMax:F0})");
        Debug.Log($"Memory: {System.GC.GetTotalMemory(false) / (1024f * 1024f):F1} MB");
        Debug.Log($"Time Scale: {Time.timeScale:F2}");
        if (gridManager != null) Debug.Log($"Grid: {gridManager.Width}x{gridManager.Height}, Ready: {gridManager.IsGridReady}");
        if (waveManager != null) Debug.Log($"Wave: {waveManager.GetWaveLabel()}, Active: {waveManager.waveActive}, Cubes: {waveManager.activeCubes?.Count ?? 0}");
        if (stageManager != null) Debug.Log($"Stage: {stageManager.CurrentStage?.stageName ?? "None"}");
        Debug.Log("=== END REPORT ===");
    }
    
    private void ForceGarbageCollection()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        LogAction("Forced garbage collection");
    }
    
    private void TakeScreenshot()
    {
        string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        ScreenCapture.CaptureScreenshot(filename);
        LogAction($"Screenshot saved: {filename}");
    }
    
    private void PrintHierarchy()
    {
        Debug.Log("=== SCENE HIERARCHY (Key Objects) ===");
        
        var managers = new string[] { "GridManager", "WaveManager", "PlayerManager", "StageManager", "AudioManager" };
        foreach (var name in managers)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                Debug.Log($"  {name}: {obj.transform.position}");
            }
        }
        
        Debug.Log("=== END HIERARCHY ===");
    }
    #endregion
}
