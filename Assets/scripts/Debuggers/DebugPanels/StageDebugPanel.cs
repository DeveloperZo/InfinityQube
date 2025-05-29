using UnityEngine;
using System.Linq;

public class StageDebugPanel : IDebugPanel
{
    public string PanelName => "Stage";

    private StageManager stageManager;
    private WaveManager waveManager;

    public void Initialize()
    {
        stageManager = Object.FindObjectOfType<StageManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
    }

    public void Update()
    {
        // No specific update logic needed
    }

    public void DrawPanel()
    {
        DrawCurrentStageInfo();
        GUILayout.Space(10);
        DrawStageNavigation();
        GUILayout.Space(10);
        DrawStageSelection();
        GUILayout.Space(10);
        DrawStageHistory();
    }

    private void DrawCurrentStageInfo()
    {
        GUILayout.Label("CURRENT STAGE", GUI.skin.box);

        if (stageManager?.CurrentStage != null)
        {
            var stage = stageManager.CurrentStage;
            GUILayout.Label($"ID: {stageManager.CurrentStageIndex}");
            GUILayout.Label($"Name: {stage.stageName}");
            GUILayout.Label($"Description: {stage.description}");
            GUILayout.Label($"Objective: {stage.objective}");
            GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
            GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");
            GUILayout.Label($"Status: {(stageManager.IsStageInProgress ? "ACTIVE" : "INACTIVE")}");

            if (waveManager != null)
            {
                GUILayout.Label($"Current Wave: {waveManager.CurrentWaveIndex + 1}/{stage.waveConfigurations.Count}");
                GUILayout.Label($"Move Step: {waveManager.MoveStep}");
            }
        }
        else
        {
            GUILayout.Label("No stage loaded");
        }
    }

    private void DrawStageNavigation()
    {
        GUILayout.Label("NAVIGATION", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous Stage"))
            stageManager?.LoadPreviousStage();
        if (GUILayout.Button("Restart Current"))
            stageManager?.RestartCurrentStage();
        if (GUILayout.Button("Next Stage"))
            stageManager?.LoadNextStage();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to First"))
            stageManager?.ResetToFirstStage();
        if (GUILayout.Button("Force Complete"))
            stageManager?.ForceCompleteStage(true);
        if (GUILayout.Button("Force Fail"))
            stageManager?.ForceCompleteStage(false);
        GUILayout.EndHorizontal();
    }

    private void DrawStageSelection()
    {
        GUILayout.Label("STAGE SELECTION", GUI.skin.box);

        if (stageManager != null)
        {
            var availableStages = stageManager.GetAvailableStages();

            GUILayout.Label($"Available Stages: {availableStages.Count}");

            foreach (int stageId in availableStages.Take(8))
            {
                bool isCurrent = stageId == stageManager.CurrentStageIndex;
                GUI.backgroundColor = isCurrent ? Color.yellow : Color.white;

                if (GUILayout.Button($"Load Stage {stageId}{(isCurrent ? " (Current)" : "")}"))
                {
                    stageManager.LoadStage(stageId);
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawStageHistory()
    {
        GUILayout.Label("STAGE HISTORY", GUI.skin.box);

        if (stageManager != null)
        {
            var attempts = stageManager.GetStageAttempts();

            if (attempts.Count > 0)
            {
                GUILayout.Label("Attempts per stage:");
                foreach (var kvp in attempts.Take(5))
                {
                    GUILayout.Label($"Stage {kvp.Key}: {kvp.Value} attempts");
                }
            }
            else
            {
                GUILayout.Label("No attempts recorded yet");
            }
        }
    }
}