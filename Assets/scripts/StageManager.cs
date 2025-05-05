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

    [Header("Stage Settings")]
    [SerializeField] private bool startWithTutorial = true;
    [SerializeField] private int currentStageIndex = -1;

    private StageData currentStage;
    private Dictionary<int, StageData> stageDatabase = new Dictionary<int, StageData>();


    private int capturedCubeCount = 0;
    private int escapedCubeCount = 0;

    private void Awake()
    {
        // Automatically find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();

        // Initialize stage database
        InitializeStageDatabase();
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

    private void InitializeStageDatabase()
    {
        // Add tutorial stages (negative numbers)
        stageDatabase.Add(-1, StageData.CreateTutorialMinus1());

        // Add regular stages (positive numbers)
        // Stage 1, 2, 3, etc. will be added here...
    }

    public void LoadStage(int stageNumber)
    {
        if (!stageDatabase.ContainsKey(stageNumber))
        {
            Debug.LogWarning($"Stage {stageNumber} not found in the database!");
            return;
        }

        // Reset counters
        capturedCubeCount = 0;
        escapedCubeCount = 0;

        // Store current stage
        currentStageIndex = stageNumber;
        currentStage = stageDatabase[stageNumber];

        // Configure grid based on stage data
        ConfigureGrid();

        // Configure wave manager
        ConfigureWaveManager();

        // Set player position
        SetPlayerPosition();


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
            // Set wave parameters
            waveManager.waveSize = (currentStage.waveSize);

            // Set cube chances
            waveManager.cubeChances = new[]
            {
                currentStage.normalCubeChance,
                currentStage.greenCubeChance,
                currentStage.blackCubeChance
            };

            // Set specific cube placements if any
            if (currentStage.specificCubePlacements.Count > 0)
            {
                List<CubeSpawnData> spawnData = new List<CubeSpawnData>();

                foreach (var placement in currentStage.specificCubePlacements)
                {
                    spawnData.Add(new CubeSpawnData
                    {
                        cubeType = placement.cubeType,
                        position = placement.position,
                        waveIndex = placement.waveIndex
                    });
                }

                waveManager.ConfigureSpawn(spawnData);
            }
            else
            {
                waveManager.ClearAllCubes();
            }
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

    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        capturedCubeCount++;
        CheckStageCompletion();
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
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
        Debug.Log($"Stage {currentStageIndex} completed!");


        // Proceed to next stage after delay
        StartCoroutine(DelayedNextStage());
    }

    private void FailStage()
    {
        Debug.Log($"Stage {currentStageIndex} failed!");


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


public class CubeSpawnData
{
    public Enumerations.CubeType cubeType;
    public Vector2Int position;
    public int waveIndex = 0;
}