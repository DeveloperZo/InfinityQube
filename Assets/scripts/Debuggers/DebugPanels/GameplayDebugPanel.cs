using static Enumerations;
using UnityEngine;

public class GameplayDebugPanel : DebugPanelBase
{
    public override string PanelName => "Gameplay";
    public override DebugPanelGroup Group => DebugPanelGroup.Gameplay;

    private StageManager stageManager;
    private WaveManager waveManager;
    private PlayerManager playerManager;
    private PlayerActionManager playerActionManager;

    // UI State
    private bool showStageDetails = true;
    private bool showWaveDetails = true;
    private bool showPlayerDetails = true;
    private bool showActionDetails = false;

    public override void Initialize()
    {
        stageManager = Object.FindObjectOfType<StageManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        playerActionManager = Object.FindObjectOfType<PlayerActionManager>();
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showStageDetails) DrawStageSection();
        if (showWaveDetails) DrawWaveSection();
        if (showPlayerDetails) DrawPlayerSection();
        if (showActionDetails) DrawActionSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showStageDetails = DrawToggleButton("Stage", showStageDetails);
        showWaveDetails = DrawToggleButton("Wave", showWaveDetails);
        showPlayerDetails = DrawToggleButton("Player", showPlayerDetails);
        showActionDetails = DrawToggleButton("Actions", showActionDetails);
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawStageSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("STAGE", GUI.skin.box);

        if (stageManager?.CurrentStage != null)
        {
            var stage = stageManager.CurrentStage;
            GUILayout.Label($"#{stageManager.CurrentStageIndex}: {stage.stageName}");
            GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
            GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");
            GUILayout.Label($"Progress: {(stageManager.IsStageInProgress ? "ACTIVE" : "INACTIVE")}");

            if (!string.IsNullOrEmpty(stage.objective))
            {
                GUILayout.Label($"Objective: {stage.objective}");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart")) stageManager.RestartCurrentStage();
            if (GUILayout.Button("Win")) stageManager.ForceCompleteStage(true);
            if (GUILayout.Button("Fail")) stageManager.ForceCompleteStage(false);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No active stage");
        }

        GUILayout.EndVertical();
    }

    private void DrawWaveSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("WAVE", GUI.skin.box);

        if (waveManager != null)
        {
            GUILayout.Label($"Wave: {waveManager.CurrentWaveIndex + 1}/{waveManager.waveConfiguration.Count}");
            GUILayout.Label($"Step: {waveManager.MoveStep} | Active: {waveManager.waveActive}");
            GUILayout.Label($"Cubes: {waveManager.activeCubes.Count}");
            GUILayout.Label($"Debug: {waveManager.debugMode} | Manual: {waveManager.manualControl}");
            GUILayout.Label($"Speed: {(waveManager.isSpeedingUp ? "FAST" : "NORMAL")}");

            // Wave controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start")) waveManager.StartWave();
            if (GUILayout.Button("Pause")) waveManager.PauseWave();
            if (GUILayout.Button("Step")) waveManager.ManualMoveWaveForward();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cubes")) waveManager.ClearAllCubes();
            bool newDebug = GUILayout.Toggle(waveManager.debugMode, "Debug");
            if (newDebug != waveManager.debugMode)
            {
                if (newDebug) waveManager.EnterDebugMode(true);
                else waveManager.ExitDebugMode();
            }
            GUILayout.EndHorizontal();

            // Current wave info
            if (waveManager.CurrentWave != null)
            {
                var wave = waveManager.CurrentWave;
                GUILayout.Label($"Current Wave: {wave.GridWidth}x{wave.GridHeight}");
                GUILayout.Label($"Cubes: {wave.CubesData.Count} | Interval: {wave.moveInterval}s");
                if (wave.limitMarkers)
                {
                    GUILayout.Label($"Marker Limit: {wave.maxMarkerCount} (charge: {wave.maxMarkerCharge})");
                }
            }
        }
        else
        {
            GUILayout.Label("WaveManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawPlayerSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("PLAYER", GUI.skin.box);

        if (playerManager != null)
        {
            GUILayout.Label($"Position: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y})");
            GUILayout.Label($"Alive: {playerManager.IsAlive()} | Deaths: {playerManager.playerDeaths}");
            GUILayout.Label($"Moves: {playerManager.movesCount} | Time: {playerManager.totalPlayTime:F1}s");

            // Statistics
            var stats = playerManager.GetCurrentStatistics();
            GUILayout.Label($"Captured: {stats.TotalCubesCaptured} | Escaped: {stats.cubesEscaped}");
            GUILayout.Label($"Markers: {stats.markersPlaced} | Detonations: {stats.detonationsUsed}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill")) playerManager.Kill();
            if (GUILayout.Button("Reset Stats")) playerManager.ResetStatistics();
            if (GUILayout.Button("Clear Markers")) playerManager.ResetMarkers();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawActionSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTIONS", GUI.skin.box);

        if (playerActionManager != null)
        {
            GUILayout.Label($"Individual: {playerActionManager.GetCurrentIndividualMarkers()}/3");
            GUILayout.Label($"Area: {playerActionManager.GetCurrentAreaMarkers()}/2");
            GUILayout.Label($"Cube Markers: {playerActionManager.GetCurrentCubeMarkers()}");

            float individualCD = playerActionManager.GetIndividualMarkerCooldownRemaining();
            float areaCD = playerActionManager.GetAreaMarkerCooldownRemaining();
            if (individualCD > 0) GUILayout.Label($"Individual CD: {individualCD:F1}s");
            if (areaCD > 0) GUILayout.Label($"Area CD: {areaCD:F1}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Individual")) playerActionManager.TriggerNextIndividualMarker();
            if (GUILayout.Button("Trigger Area")) playerActionManager.TriggerNextAreaMarker();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Cube")) playerActionManager.TriggerNextCubeMarker();
            if (GUILayout.Button("Clear All")) playerActionManager.ClearAllActions();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerActionManager not found");
        }

        GUILayout.EndVertical();
    }
}
