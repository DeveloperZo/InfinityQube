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

    [Header("UI Elements")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;
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

    private List<WaveMessage> activeMessageObjects = new List<WaveMessage>();
    private Queue<WaveMessage> pendingMessages = new Queue<WaveMessage>();
    private bool isProcessingMessageQueue = false;

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
        if (isPaused && Input.GetKeyDown(KeyCode.K))
        {
            HideCurrentMessage();

            // If no more messages, resume gameplay
            if (activeMessageObjects.Count == 0)
            {
                Time.timeScale = 1f;
                isPaused = false;
            }
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

        // Show initial message if available
        //var initialMessages = currentStage.messages.Where(m => m.DisplayMoveStep == 0).ToList();
        //if (initialMessages.Any())
        //{
        //    foreach (var message in initialMessages)
        //    {
        //        ShowMessage(message, message.RequirePause, message.AutoHideDelay);
        //    }
        //}

        Debug.Log($"Stage {stageNumber}: {stage.stageName} loaded");
        waveManager.StartWave();
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

    // Message display system with pause functionality
    public void ShowMessage(WaveMessage message, bool pauseGameplay = false, float autoHideDelay = 0f)
    {
        if (messagePanel == null || messageText == null ||
            (message.DisplayMoveStep != waveManager.MoveStep && message.DisplayMoveStep != -1))
            return;

        // Add message to queue
        pendingMessages.Enqueue(message);

        // Start processing queue if not already doing so
        if (!isProcessingMessageQueue)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }

    // Process messages one at a time
    private IEnumerator ProcessMessageQueue()
    {
        isProcessingMessageQueue = true;

        while (pendingMessages.Count > 0)
        {
            // Get next message
            WaveMessage currentMessage = pendingMessages.Dequeue();

            // Wait for any previous message to be closed if this message requires pause
            if (currentMessage.RequirePause && isPaused)
            {
                yield return new WaitUntil(() => !isPaused);
            }

            // Display the message
            DisplaySingleMessage(currentMessage);

            // If message requires pause, wait until it's closed
            if (currentMessage.RequirePause)
            {
                isPaused = true;
                Time.timeScale = 0f;
                yield return new WaitUntil(() => !isPaused);
            }
            else if (currentMessage.AutoHideDelay > 0)
            {
                // Wait for auto-hide delay
                yield return new WaitForSeconds(currentMessage.AutoHideDelay);
                HideCurrentMessage();
            }
        }

        isProcessingMessageQueue = false;
    }

    // Display a single message
    private void DisplaySingleMessage(WaveMessage message)
    {
        // Instantiate the message panel

        messagePanel.SetActive(true);
        messageText.text = message.Message;




        // Track this message
        activeMessageObjects.Add(message);

        // Apply highlights if specified
        if (message.HighlightTile)
        {
            foreach (var tile in message.highlightTiles)
            {
                HighlightTile(tile.x, tile.y, message.highlightColor);
            }

        }
    }

    // Update to hide only the most recent message
    private void HideCurrentMessage()
    {
        if (activeMessageObjects.Count > 0)
        {
            // Get the last message
            int lastIndex = activeMessageObjects.Count - 1;
            WaveMessage msgObj = activeMessageObjects[lastIndex];

            // Remove and destroy it
            activeMessageObjects.RemoveAt(lastIndex);
            if (msgObj.Message == messageText.text)
                messageText.text = "";

            messagePanel.SetActive(false);
        }
    }

   

    // Update HideMessage to be able to hide all messages
    public void HideAllMessages()
    {
        activeMessageObjects.Clear();
        pendingMessages.Clear();
        Time.timeScale = 1f;
        isPaused = false;
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
        ShowMessage(new WaveMessage() { Message = "Stage failed. Some cubes escaped. Try again." , DisplayMoveStep = -1}, true);

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

    }
}