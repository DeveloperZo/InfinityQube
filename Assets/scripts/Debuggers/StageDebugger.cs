using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class StageDebugger : MonoBehaviour
{
    #region Inspector Configuration
    [Header("References")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerManager;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [SerializeField] private bool showDebugger = false;
    #endregion

    #region Runtime State
    private Vector2 scrollPosition;
    private Rect windowRect = new Rect(10, 10, 350, 500);
    private bool foldoutStages = true;
    private bool foldoutWaves = true;
    private bool foldoutActions = true;
    #endregion

    private void Start()
    {
        FindReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugger = !showDebugger;
        }
    }

    private void OnGUI()
    {
        if (!showDebugger) return;
        windowRect = GUILayout.Window(1, windowRect, DrawDebugWindow, "Stage Debugger");
    }

    private void FindReferences()
    {
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerManager == null) playerManager = FindObjectOfType<PlayerManager>();
    }

    private void DrawDebugWindow(int windowID)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawCurrentStageInfo();
        DrawStageControls();
        DrawWaveControls();
        DrawQuickActions();

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void DrawCurrentStageInfo()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CURRENT STAGE", GUI.skin.box);

        if (stageManager != null && stageManager.CurrentStage != null)
        {
            var stage = stageManager.CurrentStage;
            GUILayout.Label($"Stage: {stageManager.CurrentStageIndex}");
            GUILayout.Label($"Name: {stage.stageName}");
            GUILayout.Label($"Status: {(stageManager.IsStageInProgress ? "ACTIVE" : "INACTIVE")}");
            GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");

            if (waveManager != null)
            {
                GUILayout.Label($"Current Wave: {waveManager.CurrentWaveIndex + 1}/{stage.waveConfigurations.Count}");
                GUILayout.Label($"Active Cubes: {waveManager.activeCubes.Count}");
            }
        }
        else
        {
            GUILayout.Label("No stage loaded");
        }

        GUILayout.EndVertical();
    }

    private void DrawStageControls()
    {
        foldoutStages = EditorGUILayout.Foldout(foldoutStages, "STAGE CONTROLS");
        if (!foldoutStages) return;

        GUILayout.BeginVertical(GUI.skin.box);

        // Quick stage navigation
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Stage"))
        {
            stageManager?.LoadPreviousStage();
        }
        if (GUILayout.Button("Restart"))
        {
            stageManager?.RestartCurrentStage();
        }
        if (GUILayout.Button("Next Stage"))
        {
            stageManager?.LoadNextStage();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to First"))
        {
            stageManager?.ResetToFirstStage();
        }
        if (GUILayout.Button("Force Complete"))
        {
            stageManager?.ForceCompleteStage(true);
        }
        if (GUILayout.Button("Force Fail"))
        {
            stageManager?.ForceCompleteStage(false);
        }
        GUILayout.EndHorizontal();

        // Stage selection
        if (stageManager != null)
        {
            var availableStages = stageManager.GetAvailableStages();
            GUILayout.Label("Load Specific Stage:");

            foreach (int stageId in availableStages.Take(6)) // Show first 6 stages
            {
                if (GUILayout.Button($"Stage {stageId}"))
                {
                    stageManager.LoadStage(stageId);
                }
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawWaveControls()
    {
        foldoutWaves = EditorGUILayout.Foldout(foldoutWaves, "WAVE CONTROLS");
        if (!foldoutWaves || waveManager == null) return;

        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Wave"))
        {
            waveManager.StartWave();
        }
        if (GUILayout.Button("Pause"))
        {
            waveManager.PauseWave();
        }
        if (GUILayout.Button("Resume"))
        {
            waveManager.ResumeWave();
        }
        if (GUILayout.Button("Stop"))
        {
            waveManager.StopWave();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Manual Step"))
        {
            waveManager.ManualMoveWaveForward();
        }
        if (GUILayout.Button("Clear Cubes"))
        {
            waveManager.ClearAllCubes();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label($"Move Step: {waveManager.MoveStep}");
        GUILayout.Label($"Debug Mode: {waveManager.debugMode}");
        GUILayout.Label($"Manual Control: {waveManager.manualControl}");

        GUILayout.EndVertical();
    }

    private void DrawQuickActions()
    {
        foldoutActions = EditorGUILayout.Foldout(foldoutActions, "QUICK ACTIONS");
        if (!foldoutActions) return;

        GUILayout.BeginVertical(GUI.skin.box);

        if (GUILayout.Button("Reset Player Stats"))
        {
            playerManager?.ResetStatistics();
        }

        if (GUILayout.Button("Kill Player"))
        {
            playerManager?.Kill();
        }

        if (GUILayout.Button("Clear All Markers"))
        {
            FindObjectOfType<GridManager>()?.ClearAllMarkers();
        }

        if (GUILayout.Button("Clear Detonations"))
        {
            FindObjectOfType<DetonationManager>()?.ClearDetonationPoints();
        }

        GUILayout.EndVertical();
    }
}