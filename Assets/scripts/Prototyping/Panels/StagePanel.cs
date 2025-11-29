using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Stage Panel - Stage flow and time control.
/// </summary>
public class StagePanel : PrototypingPanelBase
{
    public override string PanelName => "Stage";
    public override string PanelIcon => "🎭";
    public override PrototypingCategory Category => PrototypingCategory.Stage;
    public override int Priority => 40;
    
    private bool showStage = true;
    private bool showWaveAdvance = true;
    private bool showTime = true;
    private bool showWinLose = true;
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("⏸", () => SetTime(0)) { Group = QuickActionGroup.Stage, Priority = 40, Tooltip = "Pause", IsHighlighted = () => Time.timeScale == 0 },
            new QuickAction("1x", () => SetTime(1)) { Group = QuickActionGroup.Stage, Priority = 41, Tooltip = "Normal" },
            new QuickAction("2x", () => SetTime(2)) { Group = QuickActionGroup.Stage, Priority = 42, Tooltip = "Fast" },
            new QuickAction("🔄", Restart) { Group = QuickActionGroup.Stage, Priority = 45, Tooltip = "Restart" }
        };
    }
    
    public override void DrawGUI()
    {
        string info = stageManager?.CurrentStage != null 
            ? $"Stage {stageManager.CurrentStageIndex}: {stageManager.CurrentStage.stageName}"
            : "No active stage";
        DrawStatus(info);
        DrawStatus($"Time Scale: {Time.timeScale:F1}x | In Progress: {stageManager?.IsStageInProgress ?? false}");
        
        GUILayout.Space(5);
        
        // Stage Control
        showStage = DrawToggleSection("STAGE CONTROL", showStage);
        if (showStage)
        {
            DrawSection("", () =>
            {
                DrawButtonRow(
                    ("◀ Prev", PrevStage),
                    ("🔄 Restart", Restart),
                    ("Next ▶", NextStage)
                );
                
                if (stageManager?.CurrentStage != null)
                {
                    var stage = stageManager.CurrentStage;
                    GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
                    GUILayout.Label($"Waves: {stage.waveConfigurations?.Count ?? 0}");
                }
                
                GUILayout.Space(5);
                GUILayout.Label("Jump to Stage:");
                GUILayout.BeginHorizontal();
                var stages = stageManager?.GetAvailableStages() ?? new List<int>();
                foreach (int id in stages.Take(8))
                {
                    int stageId = id;
                    GUI.backgroundColor = stageManager?.CurrentStageIndex == id ? Color.green : Color.white;
                    if (GUILayout.Button($"{id}", GUILayout.Width(30)))
                    {
                        stageManager?.LoadStage(stageId);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            });
        }
        
        // Wave Auto-Advance
        showWaveAdvance = DrawToggleSection("WAVE AUTO-ADVANCE", showWaveAdvance);
        if (showWaveAdvance)
        {
            DrawSection("", () =>
            {
                bool autoAdvance = stageManager?.AutoAdvanceWaves ?? true;
                string statusText = autoAdvance 
                    ? "Enabled (waves will auto-advance on completion)" 
                    : "Disabled (waves will wait for manual control)";
                
                GUI.backgroundColor = autoAdvance ? Color.green : Color.yellow;
                if (GUILayout.Button(autoAdvance ? "✓ Auto-Advance Waves" : "✗ Auto-Advance Waves", GUILayout.Height(28)))
                {
                    if (stageManager != null)
                    {
                        stageManager.AutoAdvanceWaves = !autoAdvance;
                        LogAction($"Auto-advance waves: {(stageManager.AutoAdvanceWaves ? "Enabled" : "Disabled")}");
                    }
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(3);
                DrawStatus(statusText);
                
                GUILayout.Space(5);
                
                // Wave navigation (only if using wave configuration)
                if (waveManager?.useWaveConfiguration == true)
                {
                    bool canGoPrev = waveManager.currentWaveIndex > 0;
                    bool canGoNext = waveManager.HasMoreWaves();
                    
                    GUILayout.Label("Wave Navigation:");
                    GUILayout.BeginHorizontal();
                    GUI.enabled = canGoPrev;
                    if (GUILayout.Button("◀ Prev Wave"))
                    {
                        PrevWave();
                    }
                    GUI.enabled = canGoNext;
                    if (GUILayout.Button("Next Wave ▶"))
                    {
                        NextWave();
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    
                    string waveLabel = waveManager?.GetWaveLabel() ?? "?";
                    int totalWaves = waveManager?.waveConfiguration?.Count ?? 0;
                    DrawStatus($"Current: Wave {waveLabel}/{totalWaves}");
                }
            });
        }
        
        // Time Control
        showTime = DrawToggleSection("TIME CONTROL", showTime);
        if (showTime)
        {
            DrawSection("", () =>
            {
                float speed = DrawSlider("Speed", Time.timeScale, 0, 4);
                if (speed != Time.timeScale) SetTime(speed);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("⏸ 0x")) SetTime(0);
                if (GUILayout.Button("0.5x")) SetTime(0.5f);
                if (GUILayout.Button("1x")) SetTime(1);
                if (GUILayout.Button("2x")) SetTime(2);
                if (GUILayout.Button("4x")) SetTime(4);
                GUILayout.EndHorizontal();
            });
        }
        
        // Win/Lose
        showWinLose = DrawToggleSection("WIN/LOSE", showWinLose);
        if (showWinLose)
        {
            DrawSection("", () =>
            {
                DrawButtonRow(
                    ("🏆 Force Win", ForceWin),
                    ("💀 Force Lose", ForceLose)
                );
                DrawButtonRow(
                    ("⏭ Skip Wave", SkipWave),
                    ("🔄 Restart", Restart)
                );
            });
        }
    }
    
    #region Actions
    private void PrevStage() => stageManager?.LoadPreviousStage();
    private void NextStage() => stageManager?.LoadNextStage();
    private void Restart() => stageManager?.RestartCurrentStage();
    private void ForceWin() => stageManager?.ForceCompleteStage(true);
    private void ForceLose() => stageManager?.ForceCompleteStage(false);
    private void SkipWave() => waveManager?.ForceCompleteWave();
    private void SetTime(float scale) => Time.timeScale = Mathf.Clamp(scale, 0, 4);
    
    private void PrevWave()
    {
        if (waveManager == null || !waveManager.useWaveConfiguration) return;
        
        if (waveManager.currentWaveIndex > 0)
        {
            waveManager.currentWaveIndex--;
            waveManager.StopWave();
            waveManager.ClearAllCubes();
            LogAction($"Moved to previous wave: {waveManager.GetWaveLabel()}");
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
            LogAction($"Moved to next wave: {waveManager.GetWaveLabel()}");
        }
    }
    #endregion
}
