using UnityEngine;
using System.Linq;
using static Enumerations;

public class CoreDebugPanel : DebugPanelBase
{
    public override string PanelName => "Core Systems";
    public override DebugPanelGroup Group => DebugPanelGroup.Core;

    private GridManager gridManager;
    private StageManager stageManager;
    private WaveManager waveManager;
    private PlayerManager playerManager;
    private PlayerActionManager playerActionManager;

    // Performance tracking
    private float lastFrameRate = 0f;
    private float frameRateUpdateTimer = 0f;
    private const float FRAMERATE_UPDATE_INTERVAL = 0.5f;

    // UI State
    private bool showSystemInfo = true;
    private bool showGridInfo = true;
    private bool showGameControl = true;
    private bool showTimeControls = false;

    public override void Initialize()
    {
        gridManager = GridManager.Instance;
        stageManager = Object.FindObjectOfType<StageManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        playerActionManager = Object.FindObjectOfType<PlayerActionManager>();
    }

    public override void Update()
    {
        frameRateUpdateTimer += Time.unscaledDeltaTime;
        if (frameRateUpdateTimer >= FRAMERATE_UPDATE_INTERVAL)
        {
            lastFrameRate = 1f / Time.unscaledDeltaTime;
            frameRateUpdateTimer = 0f;
        }
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showSystemInfo) DrawSystemSection();
        if (showGridInfo) DrawGridSection();
        if (showGameControl) DrawGameControlSection();
        if (showTimeControls) DrawTimeControlSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showSystemInfo = DrawToggleButton("System", showSystemInfo);
        showGridInfo = DrawToggleButton("Grid", showGridInfo);
        showGameControl = DrawToggleButton("Control", showGameControl);
        showTimeControls = DrawToggleButton("Time", showTimeControls);
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawSystemSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("SYSTEM", GUI.skin.box);

        GUILayout.Label($"FPS: {lastFrameRate:F0} | Time: {Time.timeScale:F2}x");
        GUILayout.Label($"Frame: {Time.frameCount} | Real: {Time.realtimeSinceStartup:F1}s");
        GUILayout.Label($"Unity: {Application.unityVersion} | {Application.platform}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("GC.Collect")) System.GC.Collect();
        if (GUILayout.Button("Log State")) LogSystemState();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawGridSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("GRID", GUI.skin.box);

        if (gridManager != null)
        {
            GUILayout.Label($"Size: {gridManager.Width}x{gridManager.Height}");
            GUILayout.Label($"Ready: {gridManager.IsGridReady} | Markers: {gridManager.GetMarkerCount()}");
            GUILayout.Label($"Playable Rows: {gridManager.GetPlayableRowCount()}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Markers")) gridManager.ClearAllMarkers();
            if (GUILayout.Button("Print Info")) gridManager.DebugPrintGridInfo();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("GridManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawGameControlSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("GAME CONTROL", GUI.skin.box);

        // Status Display
        if (stageManager?.CurrentStage != null)
        {
            GUILayout.Label($"Stage: {stageManager.CurrentStageIndex} - {stageManager.CurrentStage.stageName}");
        }
        if (waveManager != null)
        {
            GUILayout.Label($"Wave: {waveManager.CurrentWaveIndex + 1} | Step: {waveManager.MoveStep}");
            GUILayout.Label($"Active: {waveManager.waveActive} | Cubes: {waveManager.activeCubes.Count}");
        }
        if (playerManager != null)
        {
            GUILayout.Label($"Player: ({playerManager.currentTilePosition.x}, {playerManager.currentTilePosition.y}) | Deaths: {playerManager.playerDeaths}");
        }

        GUILayout.Space(3);

        // Quick Controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart Stage")) stageManager?.RestartCurrentStage();
        if (GUILayout.Button("Next Stage")) stageManager?.LoadNextStage();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Wave")) waveManager?.StartWave();
        if (GUILayout.Button("Pause Wave")) waveManager?.PauseWave();
        if (GUILayout.Button("Step Wave")) waveManager?.ManualMoveWaveForward();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill Player")) playerManager?.Kill();
        if (GUILayout.Button("Clear Actions")) playerActionManager?.ClearAllActions();
        GUILayout.EndHorizontal();

        // Stage Selection
        if (stageManager != null)
        {
            var availableStages = stageManager.GetAvailableStages();
            if (availableStages.Any())
            {
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

        GUILayout.EndVertical();
    }

    private void DrawTimeControlSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TIME CONTROL", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause")) Time.timeScale = 0f;
        if (GUILayout.Button("Play")) Time.timeScale = 1f;
        if (GUILayout.Button("Fast")) Time.timeScale = 4f;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.25x")) Time.timeScale = 0.25f;
        if (GUILayout.Button("0.5x")) Time.timeScale = 0.5f;
        if (GUILayout.Button("2x")) Time.timeScale = 2f;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void LogSystemState()
    {
        Debug.Log("=== CORE SYSTEM STATE ===");
        Debug.Log($"FPS: {lastFrameRate:F1} | Time Scale: {Time.timeScale}");
        if (gridManager != null)
            Debug.Log($"Grid: {gridManager.Width}x{gridManager.Height}, Ready: {gridManager.IsGridReady}");
        if (stageManager != null)
            Debug.Log($"Stage: {stageManager.CurrentStageIndex}, Progress: {stageManager.IsStageInProgress}");
        Debug.Log("=== END CORE STATE ===");
    }
}