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
    #endregion
}
