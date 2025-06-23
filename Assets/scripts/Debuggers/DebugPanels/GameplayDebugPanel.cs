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
        base.Initialize(); // Initialize theme and performance systems
        
        stageManager = Object.FindObjectOfType<StageManager>();
    }

    protected override void DrawPanelContent()
    {
        // Simple test content first
        GUILayout.Label("=== GAMEPLAY DEBUG PANEL ===");
        GUILayout.Label("This panel is working!");
        
        try
        {
            DrawSectionToggles();
            GUILayout.Space(5);

            if (showStageDetails) DrawStageDetailsSection();
            if (showStageNavigation) DrawStageNavigationSection();
            if (showGameState) DrawGameStateSection();
            if (showSystemOverview) DrawSystemOverviewSection();
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"Gameplay Panel Error: {e.Message}");
        }
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        
        // Simplified toggles without theme dependencies
        GUI.backgroundColor = showStageDetails ? Color.yellow : Color.white;
        if (GUILayout.Button("Stage Details")) showStageDetails = !showStageDetails;
        
        GUI.backgroundColor = showStageNavigation ? Color.yellow : Color.white;
        if (GUILayout.Button("Navigation")) showStageNavigation = !showStageNavigation;
        
        GUI.backgroundColor = showGameState ? Color.yellow : Color.white;
        if (GUILayout.Button("Game State")) showGameState = !showGameState;
        
        GUI.backgroundColor = showSystemOverview ? Color.yellow : Color.white;
        if (GUILayout.Button("Overview")) showSystemOverview = !showSystemOverview;
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawStageDetailsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("STAGE DETAILS", GUI.skin.box);
        
        try
        {
            if (stageManager?.CurrentStage != null)
            {
                var stage = stageManager.CurrentStage;
                
                // Current stage information
                GUILayout.Label($"Stage #{stageManager.CurrentStageIndex}: {stage.stageName}");
                GUILayout.Label($"Description: {stage.description}");
                GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
                GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");
                
                GUILayout.Label($"Progress: {(stageManager.IsStageInProgress ? "In Progress" : "Not Started")}");
                
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
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restart")) stageManager.RestartCurrentStage();
                if (GUILayout.Button("Force Win")) stageManager.ForceCompleteStage(true);
                if (GUILayout.Button("Force Fail")) stageManager.ForceCompleteStage(false);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("No active stage");
            }
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"Stage Details Error: {e.Message}");
        }
        
        GUILayout.EndVertical();
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
                DebugUIHelpers.DrawButtonGrid(new (string, System.Action)[] {
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
                    GUILayout.Label($"Current Stage Attempts: {attempts[stageManager.CurrentStageIndex] }");
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
                DebugUIHelpers.DrawButtonGrid(new (string, System.Action)[] {
                    ("Wave Panel", () => Debug.Log("Switch to Wave Panel (F12 + Tab)")),
                    ("Grid Panel", () => Debug.Log("Switch to Grid Panel (F12 + Tab)")),
                    ("Testing Panel", () => Debug.Log("Switch to Testing Panel (F12 + Tab)"))
                });
            }
        });
    }
}
