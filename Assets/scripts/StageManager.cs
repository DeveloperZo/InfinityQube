using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEditor.VersionControl;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DetonationManager detonationManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private Text messageText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Highlight System")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Material tileHighlightMaterial;
    [SerializeField] private Material cubeHighlightMaterial;

    [Header("Stage Settings")]
    [SerializeField] private int currentStageIndex = -1;
    [SerializeField] private StageDB stageDatabase;

    private StageData currentStage;
    private bool stageInProgress = false;
    private int capturedCubeCount = 0;
    private int escapedCubeCount = 0;

    // Message system
    private bool isMessageActive = false;
    private bool isPaused = false;
    private List<GameObject> activeHighlights = new List<GameObject>();

    // Callbacks for stage events
    public delegate void StageEvent();
    public event StageEvent OnStageCompleted;
    public event StageEvent OnStageFailed;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
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

        // Hide message panel initially
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    private void Start()
    {
        // Start with first stage (can be tutorial or regular)
        LoadStage(currentStageIndex);
    }

    private void Update()
    {
        // Handle message confirmation
        if (isMessageActive && isPaused && Input.GetKeyDown(KeyCode.Space))
        {
            ResumeGameplay();
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
        if (waveManager != null && currentStage.waveConfigurations.Count > 0)
        {
            waveManager.waveConfiguration = currentStage.waveConfigurations;
            waveManager.useWaveConfiguration = true;
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

    // Message display system with pause functionality
    public void ShowMessage(StageMessage message, bool pauseGameplay = false, float autoHideDelay = 0f)
    {
        if (messagePanel == null || messageText == null || message.DisplayMoveStep != waveManager.MoveStep && message.DisplayMoveStep != -1)
            return;

        // Show message panel
        messagePanel.SetActive(true);
        messageText.text = message.Message;
        isMessageActive = true;

        // Show continue prompt if game is paused
        if (continuePrompt != null)
            continuePrompt.SetActive(pauseGameplay);

        // Pause if requested
        if (pauseGameplay)
        {
            Time.timeScale = 0f;
            isPaused = true;
        }
        else if (autoHideDelay > 0f)
        {
            // Auto-hide after delay if not pausing
            StartCoroutine(AutoHideMessage(autoHideDelay));
        }
    }

    private IEnumerator AutoHideMessage(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessage();
    }

    public void HideMessage()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);

        isMessageActive = false;
    }

    public void ResumeGameplay()
    {
        // Hide message
        HideMessage();

        // Resume game time
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
    }

    // Highlight system for tiles and cubes
    public void HighlightTile(int x, int y, Color color)
    {
        if (x < 0 || x >= gridManager.Width || y < 0 || y >= gridManager.Height)
            return;

        // Create a highlight at the tile position
        Vector3 position = new Vector3(x, 0.05f, y); // Slightly above tile
        GameObject highlight = CreateHighlight(position, color, tileHighlightMaterial);
        activeHighlights.Add(highlight);
    }

    public void HighlightCube(CubeBehavior cube, Color color)
    {
        if (cube == null)
            return;

        // Create a highlight that follows the cube
        GameObject highlight = CreateHighlight(cube.transform.position, color, cubeHighlightMaterial);

        // Setup to follow cube
        HighlightFollower follower = highlight.AddComponent<HighlightFollower>();
        follower.SetTarget(cube.transform);

        activeHighlights.Add(highlight);
    }

    public void HighlightMultipleTiles(List<Vector2Int> positions, Color color)
    {
        foreach (Vector2Int pos in positions)
        {
            HighlightTile(pos.x, pos.y, color);
        }
    }

    public void ClearAllHighlights()
    {
        foreach (GameObject highlight in activeHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }

        activeHighlights.Clear();
    }

    private GameObject CreateHighlight(Vector3 position, Color color, Material baseMaterial)
    {
        GameObject highlight = new GameObject("Highlight");
        highlight.transform.position = position;

        // Add mesh components
        MeshFilter meshFilter = highlight.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateHighlightMesh();

        MeshRenderer meshRenderer = highlight.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(baseMaterial);
        meshRenderer.material.color = color;

        // Add pulsing effect
        PulseEffect pulser = highlight.AddComponent<PulseEffect>();

        return highlight;
    }

    private Mesh CreateHighlightMesh()
    {
        // Create a simple quad mesh for highlighting
        Mesh mesh = new Mesh();

        float size = 0.9f; // Slightly smaller than a full tile
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-size/2, 0, -size/2),
            new Vector3(size/2, 0, -size/2),
            new Vector3(-size/2, 0, size/2),
            new Vector3(size/2, 0, size/2)
        };

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }

    // Helper component for highlights that follow cubes
    private class HighlightFollower : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset = new Vector3(0, 0.1f, 0); // Slight offset above

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        private void Update()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
            else
            {
                // Target was destroyed, destroy this highlight
                Destroy(gameObject);
            }
        }
    }

    // Simple pulse effect for highlights
    private class PulseEffect : MonoBehaviour
    {
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public float pulseSpeed = 2f;

        private Vector3 baseScale;

        private void Start()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float pulse = Mathf.Lerp(minScale, maxScale,
                (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);

            transform.localScale = baseScale * pulse;
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

    public void OnMoveStep(int step)
    {
        // This can be called from WaveManager at specific movement steps
        // We can use this to show contextual messages or highlights

        // Example: For stage 1, highlight on first move step
        if (currentStageIndex == -1 && step == 1)
        {
            // Show a hint message about marking tiles
            if (currentStage.messages.Count >= 2)
                ShowMessage(currentStage.messages[1], false, 5f);

            // Highlight a specific tile to mark
            HighlightTile(1, 1, Color.yellow);
        }
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

        // Show completion message if available
        if (currentStage.messages.Count >= 3)
            ShowMessage(currentStage.messages[2], true);

        // Clear highlights
        ClearAllHighlights();

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

        // Show failure message
        ShowMessage(new StageMessage() { Message = "Stage failed. Some cubes escaped. Try again." , DisplayMoveStep = -1}, true);

        // Clear highlights
        ClearAllHighlights();

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
        var anyMessage = currentStage.messages.Any(x => x.DisplayMoveStep == waveManager.MoveStep);
        if (anyMessage) 
        {
            var message = currentStage.messages.First(x => x.DisplayMoveStep == waveManager.MoveStep);
            ShowMessage(message, true);

        } 
    }
}