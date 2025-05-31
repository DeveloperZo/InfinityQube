using UnityEngine;
using System.Linq;

public class GameControlPanel : IDebugPanel
{
    public string PanelName => "Game Control";

    private StageManager stageManager;
    private WaveManager waveManager;
    private PlayerManager playerManager;
    private GridManager gridManager;
    private PlayerActionManager playerActionManager;

    // Update tracking
    private int lastMoveStep = -1;
    private bool lastWaveActive = false;
    private Vector2Int lastPlayerPos = new Vector2Int(-1, -1);

    public void Initialize()
    {
        stageManager = Object.FindObjectOfType<StageManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        gridManager = GridManager.Instance;
        playerActionManager = Object.FindObjectOfType<PlayerActionManager>();
    }

    public void Update()
    {
        // Track changes for live updates
        if (waveManager != null)
        {
            if (lastMoveStep != waveManager.MoveStep)
            {
                lastMoveStep = waveManager.MoveStep;
            }

            if (lastWaveActive != waveManager.waveActive)
            {
                lastWaveActive = waveManager.waveActive;
            }
        }

        if (playerManager != null)
        {
            if (lastPlayerPos != playerManager.currentTilePosition)
            {
                lastPlayerPos = playerManager.currentTilePosition;
            }
        }
    }

    public void DrawPanel()
    {
        DrawGameStatus();
        GUILayout.Space(10);
        DrawQuickControls();
        GUILayout.Space(10);
        DrawStageControls();
        GUILayout.Space(10);
        DrawWaveControls();
        GUILayout.Space(10);
        DrawPlayerControls();
    }

    private void DrawGameStatus()
    {
        GUILayout.Label("GAME STATUS", GUI.skin.box);

        // Stage info
        if (stageManager?.CurrentStage != null)
        {
            GUILayout.Label($"Stage: {stageManager.CurrentStageIndex} - {stageManager.CurrentStage.stageName}");
            GUILayout.Label($"Progress: {(stageManager.IsStageInProgress ? "ACTIVE" : "INACTIVE")}");
        }

        // Wave info
        if (waveManager != null)
        {
            GUILayout.Label($"Wave: {waveManager.CurrentWaveIndex + 1} | Step: {waveManager.MoveStep}");
            GUILayout.Label($"Active: {waveManager.waveActive} | Cubes: {waveManager.activeCubes.Count}");
            GUILayout.Label($"Debug: {waveManager.debugMode} | Manual: {waveManager.manualControl}");
        }

        // Player info
        if (playerManager != null)
        {
            GUILayout.Label($"Player: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})");
            GUILayout.Label($"Alive: {playerManager.IsAlive()} | Deaths: {playerManager.playerDeaths}");
        }
    }

    private void DrawQuickControls()
    {
        GUILayout.Label("QUICK CONTROLS", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart Stage"))
            stageManager?.RestartCurrentStage();
        if (GUILayout.Button("Next Stage"))
            stageManager?.LoadNextStage();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause Wave"))
            waveManager?.PauseWave();
        if (GUILayout.Button("Resume Wave"))
            waveManager?.ResumeWave();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill Player"))
            playerManager?.Kill();
        if (GUILayout.Button("Reset Stats"))
            playerManager?.ResetStatistics();
        GUILayout.EndHorizontal();
    }

    private void DrawStageControls()
    {
        GUILayout.Label("STAGE CONTROLS", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev"))
            stageManager?.LoadPreviousStage();
        if (GUILayout.Button("Restart"))
            stageManager?.RestartCurrentStage();
        if (GUILayout.Button("Next"))
            stageManager?.LoadNextStage();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Win"))
            stageManager?.ForceCompleteStage(true);
        if (GUILayout.Button("Force Fail"))
            stageManager?.ForceCompleteStage(false);
        GUILayout.EndHorizontal();

        // Quick stage selection
        if (stageManager != null)
        {
            var availableStages = stageManager.GetAvailableStages();

            GUILayout.Label("Quick Load:");
            GUILayout.BeginHorizontal();
            foreach (int stageId in availableStages.Take(4))
            {
                if (GUILayout.Button($"S{stageId}", GUILayout.Width(40)))
                {
                    stageManager.LoadStage(stageId);
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawWaveControls()
    {
        GUILayout.Label("WAVE CONTROLS", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start"))
            waveManager?.StartWave();
        if (GUILayout.Button("Pause"))
            waveManager?.PauseWave();
        if (GUILayout.Button("Resume"))
            waveManager?.ResumeWave();
        if (GUILayout.Button("Stop"))
            waveManager?.StopWave();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Step"))
            waveManager?.ManualMoveWaveForward();
        if (GUILayout.Button("Clear"))
            waveManager?.ClearAllCubes();
        GUILayout.EndHorizontal();

        // Debug mode toggle
        if (waveManager != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Debug Mode:");
            bool newDebugMode = GUILayout.Toggle(waveManager.debugMode, "");
            if (newDebugMode != waveManager.debugMode)
            {
                if (newDebugMode)
                    waveManager.EnterDebugMode(true);
                else
                    waveManager.ExitDebugMode();
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPlayerControls()
    {
        GUILayout.Label("PLAYER CONTROLS", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill"))
            playerManager?.Kill();
        if (GUILayout.Button("Reset Stats"))
            playerManager?.ResetStatistics();
        if (GUILayout.Button("Clear Markers"))
            playerManager?.ResetMarkers();
        GUILayout.EndHorizontal();

        // Quick stats
        if (playerManager != null)
        {
            int totalCaptured = playerManager.normalCubesCaptured + playerManager.blueCubesCaptured + playerManager.blackCubesCaptured;
            GUILayout.Label($"Captured: {totalCaptured} | Escaped: {playerManager.cubesEscaped}");
            GUILayout.Label($"Markers: {playerManager.markersPlaced} | Detonations: {playerManager.detonationsUsed}");
        }

        // System controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Markers"))
            gridManager?.ClearAllMarkers();
        if (GUILayout.Button("Clear Detonations"))
            playerActionManager?.ClearAllActions();
        GUILayout.EndHorizontal();
    }
}