using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private StageDB stageDatabase;

    [Header("Stage Settings")]
    [SerializeField] private bool startWithTutorial = true;
    [SerializeField] private int currentStageIndex = -1;

    private StageData currentStage;
    private bool stageInProgress = false;
    private int capturedCubeCount = 0;
    private int escapedCubeCount = 0;

    private void Awake()
    {
        // Automatically find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();

        // Initialize stage database
        if (stageDatabase == null)
        {
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
            if (stageDatabase == null)
            {
                Debug.LogError("StageDatabase not found in Resources folder!");
                // Create a temporary database with the tutorial
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
        // Start with tutorial or first regular stage
        if (startWithTutorial)
        {
            LoadStage(-1); // The first tutorial stage
        }
        else
        {
            LoadStage(1); // The first regular stage
        }
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

        // Show tutorial UI if applicable
        //ShowTutorialUI(stage.isTutorial);

        Debug.Log($"Stage {stageNumber}: {stage.stageName} loaded");
    }

    private void ConfigureGrid()
    {
        // Regenerate grid with stage dimensions
        if (gridManager != null)
        {
            gridManager.width = currentStage.gridWidth;
            gridManager.height = currentStage.gridHeight;

            // Destroy old grid
            gridManager.DestroyGrid();

            // Create new grid
            gridManager.GenerateGrid();
        }
    }

    private void ConfigureWaveManager()
    {
        if (waveManager != null)
        {

        }
    }

    private void SetPlayerPosition()
    {
        if (playerController != null)
        {
            playerController.SetPosition(currentStage.playerStartPosition.x, currentStage.playerStartPosition.y);

            // Set marker limit if applicable
            if (currentStage.limitMarkers)
            {
                playerController.SetMaxMarkers(currentStage.maxMarkers);
            }
            else
            {
                playerController.SetMaxMarkers(2); // Default value
            }
        }
    }

    private void ShowTutorialUI(bool show)
    {
        if (tutorialUI != null)
        {
            tutorialUI.SetActive(show);

            // Populate tutorial messages if available

        }
    }

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

    private void CheckStageCompletion()
    {
        // Success conditions
        bool success = false;

        // Check capture count
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

        // Implement stage completion effects
        // For example: Show completion UI, play sounds, etc.

        // Proceed to next stage after delay
        StartCoroutine(DelayedNextStage());
    }

    private void FailStage()
    {
        stageInProgress = false;
        Debug.Log($"Stage {currentStageIndex} failed!");

        // Implement stage failure effects
        // For example: Show failure UI, play sounds, etc.

        // Restart the same stage after delay
        StartCoroutine(DelayedRestartStage());
    }

    private IEnumerator DelayedNextStage()
    {
        yield return new WaitForSeconds(3.0f);
        LoadStage(currentStageIndex + 1);
    }

    private IEnumerator DelayedRestartStage()
    {
        yield return new WaitForSeconds(2.0f);
        LoadStage(currentStageIndex);
    }
}