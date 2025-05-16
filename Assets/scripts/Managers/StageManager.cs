using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEditor.VersionControl;
using TMPro;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerManager playerController;
    [SerializeField] private DetonationManager detonationManager;

    [Header("Stage Settings")]
    [SerializeField] private int currentStageIndex = -1;
    [SerializeField] private StageDB stageDatabase;

    private StageData currentStage;
    private bool stageInProgress = false;
    private int capturedCubeCount = 0;
    private int escapedCubeCount = 0;

    // Callbacks for stage events
    public delegate void StageEvent();
    public event StageEvent OnStageCompleted;
    public event StageEvent OnStageFailed;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerManager>();
        if (detonationManager == null) detonationManager = FindObjectOfType<DetonationManager>();

        // Initialize stage database
        if (stageDatabase == null)
        {
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
            if (stageDatabase == null)
            {
                Debug.LogError("StageDatabase not found in Resources folder!");
                stageDatabase = ScriptableObject.CreateInstance<StageDB>();
                stageDatabase.Initialize();
            }
        }
        else
        {
            stageDatabase.Initialize();
        }
    }

    private void Start()
    {
        // Start with first stage (can be tutorial or regular)
        LoadStage(currentStageIndex);
    }

    public void LoadStage(int stageNumber)
    {
        StageData stage = stageDatabase.GetStage(stageNumber);

        if (stage == null)
        {
            Debug.LogWarning($"Stage {stageNumber} not found in the database!");
            return;
        }

        // Reset counters
        capturedCubeCount = 0;
        escapedCubeCount = 0;
        stageInProgress = true;

        // Store current stage
        currentStageIndex = stageNumber;
        currentStage = stage;

        // Configure grid based on stage data
        ConfigureGrid();

        // Configure wave manager
        ConfigureWaveManager();

        // Set player position
        SetPlayerPosition();

        Debug.Log($"Stage {stageNumber}: {stage.stageName} loaded");
        waveManager.StartWave();
    }

    private void ConfigureGrid()
    {
        // Regenerate grid with stage dimensions
        if (gridManager != null)
        {
            // Destroy old grid
            gridManager.DestroyGrid();

            gridManager.width = currentStage.gridWidth;
            gridManager.height = currentStage.gridHeight;


            // Create new grid
            gridManager.GenerateGrid();
        }
    }

    private void ConfigureWaveManager()
    {
        if (waveManager != null && currentStage.waveConfigurations.Count > 0)
        {
            List<WaveData> wavesWithOffSet = UpdateWavePositionsWithGridSize(currentStage.waveConfigurations);
            // Clear previous wave configurations
            waveManager.waveConfiguration = wavesWithOffSet;

            waveManager.useWaveConfiguration = true;
        }
    }

    private List<WaveData> UpdateWavePositionsWithGridSize(List<WaveData> waves)
    {
        List<WaveData> updatedWaves = waves.ToList();

        foreach (var wave in waves)
        {
            foreach (var cube in wave.CubesData)
            {
                // Adjust the position of each cube in the wave
                cube.position.y = gridManager.height - (wave.GridHeight - cube.position.y);
            }
        }

        return updatedWaves;
    }

    private void SetPlayerPosition()
    {
        if (playerController != null)
        {
            playerController.SetPosition(currentStage.playerStartPosition.x, currentStage.playerStartPosition.y);
        }
    }

    // Event handlers for game events
    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        if (!stageInProgress) return;

        capturedCubeCount++;
        CheckStageCompletion();
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
        if (!stageInProgress) return;

        escapedCubeCount++;
        CheckStageCompletion();
    }

    public void OnWaveCompleted()
    {
        // This will be called from WaveManager when a wave ends
        CheckStageCompletion();
    }

    private void CheckStageCompletion()
    {
        // Each stage can have unique completion conditions
        bool success = false;

        // Check general success conditions
        if (currentStage.requiredCaptureCount > 0 && capturedCubeCount >= currentStage.requiredCaptureCount)
        {
            success = true;
        }

        // Check if too many escapes
        if (currentStage.maxAllowedEscapes >= 0 && escapedCubeCount > currentStage.maxAllowedEscapes)
        {
            // Failed the stage
            FailStage();
            return;
        }

        if (success)
        {
            CompleteStage();
        }
    }

    private void CompleteStage()
    {
        stageInProgress = false;
        Debug.Log($"Stage {currentStageIndex} completed!");

        // Trigger the completion event
        if (OnStageCompleted != null)
            OnStageCompleted.Invoke();

        // Proceed to next stage after delay
        StartCoroutine(DelayedNextStage());
    }

    private void FailStage()
    {
        stageInProgress = false;
        Debug.Log($"Stage {currentStageIndex} failed!");

        // Trigger the failure event
        if (OnStageFailed != null)
            OnStageFailed.Invoke();

        // Restart the same stage after delay
        StartCoroutine(DelayedRestartStage());
    }

    private IEnumerator DelayedNextStage()
    {
        yield return new WaitForSeconds(1.0f);
        LoadStage(currentStageIndex + 1);
    }

    private IEnumerator DelayedRestartStage()
    {
        yield return new WaitForSeconds(1.0f);
        LoadStage(currentStageIndex);
    }

    internal void MoveStepComplete()
    {
        // Handle any stage-specific move step logic here
    }
}