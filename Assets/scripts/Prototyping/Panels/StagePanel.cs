using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Stage Panel - Time control and wave navigation.
/// Works independently of StageManager for prototyping flexibility.
/// </summary>
public class StagePanel : PrototypingPanelBase
{
    public override string PanelName => "Stage";
    public override string PanelIcon => "S";
    public override PrototypingCategory Category => PrototypingCategory.Stage;
    public override int Priority => 40;
    
    private bool showTime = true;
    private bool showWaveNav = true;
    private bool showStage = false;
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    
    public override void DrawGUI()
    {
        // Status - works without StageManager
        DrawStatus($"Time Scale: {Time.timeScale:F1}x");
        
        GUILayout.Space(5);
        
        // Time Control - Primary feature, always works
        showTime = DrawToggleSection("TIME CONTROL", showTime);
        if (showTime)
        {
            DrawSection("", () =>
            {
                GUILayout.Label($"Current: {Time.timeScale:F2}x");
                
                float speed = DrawSlider("Speed", Time.timeScale, 0, 4);
                if (Mathf.Abs(speed - Time.timeScale) > 0.01f) SetTime(speed);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pause")) SetTime(0);
                if (GUILayout.Button("0.25x")) SetTime(0.25f);
                if (GUILayout.Button("0.5x")) SetTime(0.5f);
                if (GUILayout.Button("1x")) SetTime(1);
                if (GUILayout.Button("2x")) SetTime(2);
                if (GUILayout.Button("4x")) SetTime(4);
                GUILayout.EndHorizontal();
            });
        }
        
        // Wave Navigation - works with WaveManager
        showWaveNav = DrawToggleSection("WAVE NAVIGATION", showWaveNav);
        if (showWaveNav)
        {
            DrawSection("", () =>
            {
                if (waveManager == null)
                {
                    GUILayout.Label("WaveManager not available");
                    return;
                }
                
                // Wave info
                string waveLabel = waveManager.GetWaveLabel() ?? "?";
                int totalWaves = waveManager.waveConfiguration?.Count ?? 0;
                bool waveActive = waveManager.waveActive;
                
                GUILayout.Label($"Wave: {waveLabel}" + (totalWaves > 0 ? $"/{totalWaves}" : "") + (waveActive ? " (Active)" : " (Stopped)"));
                GUILayout.Label($"Cubes: {waveManager.activeCubes?.Count ?? 0}");
                
                GUILayout.Space(3);
                
                // Wave control
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Wave"))
                {
                    waveManager.StartWave();
                    LogAction("Started wave");
                }
                if (GUILayout.Button("Stop Wave"))
                {
                    waveManager.StopWave();
                    LogAction("Stopped wave");
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Skip Wave"))
                {
                    waveManager.ForceCompleteWave();
                    LogAction("Skipped wave");
                }
                if (GUILayout.Button("Clear Cubes"))
                {
                    waveManager.ClearAllCubes();
                    LogAction("Cleared cubes");
                }
                GUILayout.EndHorizontal();
                
                // Wave index navigation (if using configuration)
                if (waveManager.useWaveConfiguration && totalWaves > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Jump to Wave:");
                    GUILayout.BeginHorizontal();
                    bool canGoPrev = waveManager.currentWaveIndex > 0;
                    bool canGoNext = waveManager.HasMoreWaves();
                    
                    GUI.enabled = canGoPrev;
                    if (GUILayout.Button("< Prev")) PrevWave();
                    GUI.enabled = canGoNext;
                    if (GUILayout.Button("Next >")) NextWave();
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            });
        }
        
        // Stage Control - Optional, only shows if StageManager is available
        bool hasStageManager = stageManager != null;
        showStage = DrawToggleSection("STAGE CONTROL" + (hasStageManager ? "" : " (Disabled)"), showStage);
        if (showStage)
        {
            DrawSection("", () =>
            {
                if (!hasStageManager)
                {
                    GUILayout.Label("StageManager is disabled or not available.");
                    GUILayout.Label("Use Wave panel for direct wave control.");
                    return;
                }
                
                string info = stageManager.CurrentStage != null 
                    ? $"Stage {stageManager.CurrentStageIndex}: {stageManager.CurrentStage.stageName}"
                    : "No active stage (auto-start disabled)";
                GUILayout.Label(info);
                
                GUILayout.Space(3);
                
                // Manual start button (for when auto-start is disabled)
                if (stageManager.CurrentStage == null)
                {
                    if (GUILayout.Button("Start First Stage", GUILayout.Height(28)))
                    {
                        stageManager.LoadStage(0);
                        LogAction("Manually started Stage 0");
                    }
                    GUILayout.Space(3);
                }
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("< Prev Stage")) stageManager.LoadPreviousStage();
                if (GUILayout.Button("Restart")) stageManager.RestartCurrentStage();
                if (GUILayout.Button("Next Stage >")) stageManager.LoadNextStage();
                GUILayout.EndHorizontal();
                
                // Auto-advance toggle
                GUILayout.Space(5);
                bool autoAdvance = stageManager.AutoAdvanceWaves;
                GUI.backgroundColor = autoAdvance ? Color.green : Color.gray;
                if (GUILayout.Button(autoAdvance ? "Auto-Advance: ON" : "Auto-Advance: OFF", GUILayout.Height(25)))
                {
                    stageManager.AutoAdvanceWaves = !autoAdvance;
                    LogAction($"Auto-advance: {(stageManager.AutoAdvanceWaves ? "ON" : "OFF")}");
                }
                GUI.backgroundColor = Color.white;
            });
        }
    }
    
    #region Actions
    private void SetTime(float scale) => Time.timeScale = Mathf.Clamp(scale, 0, 4);
    
    private void PrevWave()
    {
        if (waveManager == null || !waveManager.useWaveConfiguration) return;
        
        if (waveManager.currentWaveIndex > 0)
        {
            waveManager.currentWaveIndex--;
            waveManager.StopWave();
            waveManager.ClearAllCubes();
            LogAction($"Moved to wave: {waveManager.GetWaveLabel()}");
        }
    }
    
    private void NextWave()
    {
        if (waveManager == null || !waveManager.useWaveConfiguration) return;
        
        if (waveManager.HasMoreWaves())
        {
            waveManager.currentWaveIndex++;
            waveManager.StopWave();
            waveManager.ClearAllCubes();
            LogAction($"Moved to wave: {waveManager.GetWaveLabel()}");
        }
    }
    #endregion
}
