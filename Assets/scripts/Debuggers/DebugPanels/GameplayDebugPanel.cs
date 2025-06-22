using static Enumerations;
using UnityEngine;
using System.Linq;

public class GameplayDebugPanel : DebugPanelBase
{
    public override string PanelName => "Gameplay";
    public override DebugPanelGroup Group => DebugPanelGroup.Gameplay;

    private StageManager stageManager;

    // UI State
    private bool showStageDetails = true;
    private bool showStageNavigation = true;
    private bool showGameState = true;
    private bool showSystemOverview = false;

    public override void Initialize()
    {
        stageManager = Object.FindObjectOfType<StageManager>();
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        DebugUIHelpers.Space();

        if (showStageDetails) DrawStageDetailsSection();
        if (showStageNavigation) DrawStageNavigationSection();
        if (showGameState) DrawGameStateSection();
        if (showSystemOverview) DrawSystemOverviewSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showStageDetails = DebugUIHelpers.DrawToggleButton("Stage Details", showStageDetails);
        showStageNavigation = DebugUIHelpers.DrawToggleButton("Navigation", showStageNavigation);
        showGameState = DebugUIHelpers.DrawToggleButton("Game State", showGameState);
        showSystemOverview = DebugUIHelpers.DrawToggleButton("Overview", showSystemOverview);
        GUILayout.EndHorizontal();
    }

    private void DrawStageDetailsSection()
    {
        DebugUIHelpers.DrawSection("STAGE DETAILS", () => {
            if (stageManager?.CurrentStage != null)
            {
                var stage = stageManager.CurrentStage;
                
                // Current stage information
                GUILayout.Label($"Stage #{stageManager.CurrentStageIndex}: {stage.stageName}");
                GUILayout.Label($"Type: {stage.stageType}");
                GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
                GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");
                
                DebugUIHelpers.DrawStatusIndicator("Progress", stageManager.IsStageInProgress);
                
                if (!string.IsNullOrEmpty(stage.objective))
                {
                    GUILayout.Label($"Objective: {stage.objective}");
                }
                
                // Stage requirements
                if (stage.requiredCaptureCount > 0)
                {
                    GUILayout.Label($"Required Captures: {stage.requiredCaptureCount}");
                }
                if (stage.maxAllowedEscapes >= 0)
                {
                    GUILayout.Label($"Max Escapes: {stage.maxAllowedEscapes}");
                }
                
                // Quick stage controls
                DebugUIHelpers.DrawButtonGrid(new[] {
                    ("Restart", () => stageManager.RestartCurrentStage()),
                    ("Force Win", () => stageManager.ForceCompleteStage(true)),
                    ("Force Fail", () => stageManager.ForceCompleteStage(false))
                });
            }
            else
            {
                GUILayout.Label("No active stage");
            }
        });
    }

    private void DrawStageNavigationSection()
    {
        DebugUIHelpers.DrawSection("STAGE NAVIGATION", () => {
            if (stageManager != null)
            {
                // Available stages
                var availableStages = stageManager.GetAvailableStages();
                GUILayout.Label($"Available Stages: {availableStages.Count}");
                
                // Navigation controls
                DebugUIHelpers.DrawButtonGrid(new[] {
                    ("Previous", () => stageManager.LoadPreviousStage()),
                    ("Next", () => stageManager.LoadNextStage()),
                    ("Reset to First", () => stageManager.ResetToFirstStage())
                });
                
                DebugUIHelpers.Space();
                
                // Direct stage loading
                GUILayout.Label("Load Specific Stage:");
                GUILayout.BeginHorizontal();
                foreach (int stageId in availableStages.Take(6)) // Show first 6
                {
                    bool isCurrent = stageId == stageManager.CurrentStageIndex;
                    DebugUIHelpers.WithBackgroundColor(
                        isCurrent ? DebugUIHelpers.SelectedItemColor : Color.white,
                        () => {
                            if (GUILayout.Button($"{stageId}", GUILayout.Width(30)))
                            {
                                stageManager.LoadStage(stageId);
                            }
                        }
                    );
                }
                GUILayout.EndHorizontal();
                
                if (availableStages.Count > 6)
                {
                    GUILayout.Label($"... and {availableStages.Count - 6} more stages");
                }
            }
            else
            {
                GUILayout.Label("StageManager not found");
            }
        });
    }

    private void DrawGameStateSection()
    {
        DebugUIHelpers.DrawSection("GAME STATE", () => {
            if (stageManager != null)
            {
                // Stage attempts tracking
                var attempts = stageManager.GetStageAttempts();
                if (attempts.Count > 0)
                {
                    GUILayout.Label($"Current Stage Attempts: {attempts.GetValueOrDefault(stageManager.CurrentStageIndex, 0)}");
                    GUILayout.Label($"Total Attempts Tracked: {attempts.Count} stages");
                }
                
                DebugUIHelpers.Space();
                
                // Game flow settings (if accessible)
                GUILayout.Label("Game Flow:");
                GUILayout.Label("• Stage progression handled by StageManager");
                GUILayout.Label("• Wave management delegated to WaveDebugPanel");
                GUILayout.Label("• Player coordination in PlayerActionPanel");
                
                DebugUIHelpers.Space();
                
                // Quick system status
                var waveManager = Object.FindObjectOfType<WaveManager>();
                var playerManager = Object.FindObjectOfType<PlayerManager>();
                var gridManager = GridManager.Instance;
                
                DebugUIHelpers.DrawStatusIndicator("Wave System", waveManager != null);
                DebugUIHelpers.DrawStatusIndicator("Player System", playerManager != null);
                DebugUIHelpers.DrawStatusIndicator("Grid System", gridManager != null);
            }
        });
    }

    private void DrawSystemOverviewSection()
    {
        DebugUIHelpers.DrawSection("SYSTEM OVERVIEW", () => {
            GUILayout.Label("Manager Coverage by Panel:");
            GUILayout.Label("• GameplayPanel: StageManager (this panel)");
            GUILayout.Label("• WavePanel: WaveManager");
            GUILayout.Label("• GridPanel: GridManager + Tiles");
            GUILayout.Label("• PlayerActionPanel: PlayerManager + Actions");
            GUILayout.Label("• CubePanel: CubeManager + Face Painting");
            GUILayout.Label("• TestingPanel: Cross-system Testing");
            
            DebugUIHelpers.Space();
            
            // Quick links to other panels
            GUILayout.Label("Quick Panel Access:");
            var debugSystem = Object.FindObjectOfType<DebugSystem>();
            if (debugSystem != null)
            {
                DebugUIHelpers.DrawButtonGrid(new[] {
                    ("Wave Panel", () => Debug.Log("Switch to Wave Panel (F12 + Tab)")),
                    ("Grid Panel", () => Debug.Log("Switch to Grid Panel (F12 + Tab)")),
                    ("Testing Panel", () => Debug.Log("Switch to Testing Panel (F12 + Tab)"))
                });
            }
        });
    }
}
