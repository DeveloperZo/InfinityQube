using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class WaveManager : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Core References")]
    public GridManager grid;
    public GameObject[] cubePrefabs;
    public PlayerManager player;
    public DetonationManager detonationManager;

    [Header("Wave Configuration")]
    public bool useWaveConfiguration = false;
    public List<WaveData> waveConfiguration = new List<WaveData>();
    public int currentWaveIndex = 0;

    [Header("Speed & Timing")]
    public float normalMoveInterval = 1.75f;
    public float fastMoveInterval = 0.1f;
    public float waveStartDelay = 0.75f;

    [Header("Random Wave Settings")]
    public int waveSize = 3;
    [Range(0f, 1f)] public float normalCubeChance = 0.7f;
    [Range(0f, 1f)] public float blueCubeChance = 0.2f;

    [Header("Debug & Testing")]
    public bool debugMode = false;
    public bool manualControl = false;
    public bool showDebugInfo = false;

    [Header("UI References")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public GameObject continuePrompt;
    #endregion

    #region Runtime State
    // Wave State
    public List<CubeBehavior> activeCubes = new List<CubeBehavior>();
    public bool waveActive = false;
    public int MoveStep = 0;
    public WaveData CurrentWave => useWaveConfiguration && currentWaveIndex < waveConfiguration.Count ? waveConfiguration[currentWaveIndex] : null;

    // Speed Control
    public bool isSpeedingUp = false;

    // Statistics
    private int normalCubesCaptured = 0;
    private int blueCubesCaptured = 0;
    private int cubesEscaped = 0;
    private int markersPlaced = 0;
    private int detonationsUsed = 0;

    // Wave Completion Tracking
    private int totalNonBlackCubes = 0;
    private int processedNonBlackCubes = 0;

    // Internal State
    private Coroutine waveCoroutine;
    private bool isPaused = false;
    private Queue<WaveMessage> pendingMessages = new Queue<WaveMessage>();
    private bool isProcessingMessageQueue = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        FindReferences();
        InitializeState();
    }

    private void Update()
    {
        HandleInput();
        HandleDebugCommands();
    }

    private void OnDestroy()
    {
        CleanupWave();
    }
    #endregion

    #region Initialization
    private void FindReferences()
    {
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (player == null) player = FindObjectOfType<PlayerManager>();
        if (detonationManager == null) detonationManager = FindObjectOfType<DetonationManager>();

        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (grid == null) Debug.LogError("WaveManager: GridManager not found!");
        if (cubePrefabs == null || cubePrefabs.Length < 3) Debug.LogError("WaveManager: Need at least 3 cube prefabs!");
    }

    private void InitializeState()
    {
        if (messagePanel != null) messagePanel.SetActive(false);
        ResetWaveStatistics();
    }
    #endregion

    #region Wave Control - Main Interface
    public void StartWave()
    {
        if (waveActive) return;

        DebugLog("🌊 Starting Wave...");

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        ResetWaveStatistics();
        waveCoroutine = StartCoroutine(RunWaveCoroutine());
    }

    public void PauseWave()
    {
        if (!waveActive) return;

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        debugMode = true;
        manualControl = true;
        waveActive = false;

        DebugLog("⏸️ Wave Paused - Manual Control Enabled");
    }

    public void ResumeWave()
    {
        if (waveActive) return;

        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(RunWaveCoroutine(resume: true));
        debugMode = true;
        manualControl = false;

        DebugLog("▶️ Wave Resumed");
    }

    public void StopWave()
    {
        CleanupWave();
        DebugLog("⏹️ Wave Stopped");
    }
    #endregion

    #region Wave Execution
    private IEnumerator RunWaveCoroutine(bool resume = false)
    {
        waveActive = true;
        if (!resume) MoveStep = 0;

        SetupWave(resume);

        yield return new WaitForSeconds(GetWaveStartDelay());

        if (manualControl)
        {
            // Manual mode - wait for external control
            while (manualControl) yield return null;
            waveActive = false;
            yield break;
        }

        // Main wave loop
        while (HasActiveCubes())
        {
            yield return ProcessWaveStep();
            yield return new WaitForSeconds(GetCurrentMoveInterval());
        }

        CompleteWave();
    }

    private void SetupWave(bool resume)
    {
        if (!resume)
        {
            SpawnWaveCubes();
            ShowInitialMessages();
        }

        ConfigurePlayer();
    }

    private IEnumerator ProcessWaveStep()
    {
        MoveCubesForward();
        MoveStep++;

        ProcessStepMessages();
        NotifyStepComplete();

        yield return null;
    }

    private void CompleteWave()
    {
        waveActive = false;
        waveCoroutine = null;

        if (grid != null) grid.ClearAllMarkers();

        ProcessEndMessages();
        AdvanceToNextWave();

        DebugLog("✅ Wave Completed");
    }
    #endregion

    #region Cube Management
    private void SpawnWaveCubes()
    {
        ClearAllCubes();
        ResetPlayer();

        if (useWaveConfiguration && CurrentWave != null)
        {
            SpawnConfigurationCubes();
        }
        else
        {
            SpawnRandomCubes();
        }

        CountNonBlackCubes();
        DebugLog($"📦 Spawned {activeCubes.Count} cubes ({totalNonBlackCubes} non-black)");
    }

    private void SpawnConfigurationCubes()
    {
        var wave = CurrentWave;
        foreach (var cubeData in wave.CubesData)
        {
            SpawnCube(cubeData);
        }
    }

    private void SpawnRandomCubes()
    {
        // Generate cubes in top rows of grid
        for (int row = 1; row <= waveSize; row++)
        {
            int z = grid.Height - row;
            for (int x = 0; x < grid.Width; x++)
            {
                var cubeData = CreateRandomCubeData(x, z);
                SpawnCube(cubeData);
            }
        }
    }

    private void SpawnCube(CubeData cubeData)
    {
        int prefabIndex = (int)cubeData.type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length)
        {
            Debug.LogError($"Invalid prefab index {prefabIndex} for cube type {cubeData.type}");
            return;
        }

        Vector3 spawnPos = grid.GridToWorldPosition(cubeData.position.x, cubeData.position.y, 2f);
        Debug.Log($"Spawning {cubeData.type} cube at grid ({cubeData.position.x}, {cubeData.position.y}) -> world {spawnPos}");

        GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeBehavior>();
        if (cube == null) cube = cubeObj.AddComponent<CubeBehavior>();

        cube.Init(grid, cubeData, 2f);
        activeCubes.Add(cube);

        Debug.Log($"Cube spawned successfully. Active cubes: {activeCubes.Count}");
    }

    private CubeData CreateRandomCubeData(int x, int z)
    {
        return new CubeData
        {
            type = GetRandomCubeType(),
            position = new Vector2Int(x, z),
            level = 1
        };
    }

    private void MoveCubesForward()
    {
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (i >= activeCubes.Count) continue;

            var cube = activeCubes[i];
            if (cube == null)
            {
                activeCubes.RemoveAt(i);
                continue;
            }

            cube.ResetMovementState();
            bool stillAlive = cube.MoveForward();

            if (!stillAlive)
            {
                activeCubes.RemoveAt(i);
            }
        }
    }

    public void ClearAllCubes()
    {
        foreach (var cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
                Destroy(cube.gameObject);
        }
        activeCubes.Clear();
    }
    #endregion

    #region Manual Control (for debugging)
    public void ManualMoveWaveForward()
    {
        if (!debugMode) return;

        MoveCubesForward();
        MoveStep++;
        ProcessStepMessages();
        NotifyStepComplete();

        DebugLog($"🔧 Manual Step: {MoveStep}");
    }

    public void EnterDebugMode(bool manual)
    {
        debugMode = true;
        manualControl = manual;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        waveActive = false;

        DebugLog($"🔧 Debug Mode: Manual={manual}");
    }

    public void ExitDebugMode()
    {
        debugMode = false;
        manualControl = false;
        DebugLog("🔧 Debug Mode Disabled");
    }
    #endregion

    #region Message System
    private void ShowInitialMessages()
    {
        if (CurrentWave?.messages == null) return;

        var initialMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == 0);
        foreach (var message in initialMessages)
        {
            ShowMessage(message);
        }
    }

    private void ProcessStepMessages()
    {
        if (CurrentWave?.messages == null) return;

        var stepMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == MoveStep);
        foreach (var message in stepMessages)
        {
            ShowMessage(message);
        }
    }

    private void ProcessEndMessages()
    {
        if (CurrentWave?.messages == null) return;

        var endMessages = CurrentWave.messages.Where(m => m.DisplayMoveStep == -1);
        foreach (var message in endMessages)
        {
            ShowMessage(message);
        }
    }

    public void ShowMessage(WaveMessage message)
    {
        pendingMessages.Enqueue(message);
        if (!isProcessingMessageQueue)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }

    private IEnumerator ProcessMessageQueue()
    {
        isProcessingMessageQueue = true;

        while (pendingMessages.Count > 0)
        {
            var message = pendingMessages.Dequeue();
            yield return DisplayMessage(message);
        }

        isProcessingMessageQueue = false;
    }

    private IEnumerator DisplayMessage(WaveMessage message)
    {
        if (messagePanel != null && messageText != null)
        {
            messagePanel.SetActive(true);
            messageText.text = message.Message;

            if (message.RequirePause)
            {
                isPaused = true;
                Time.timeScale = 0f;
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                Time.timeScale = 1f;
                isPaused = false;
            }
            else if (message.AutoHideDelay > 0)
            {
                yield return new WaitForSeconds(message.AutoHideDelay);
            }

            messagePanel.SetActive(false);
        }
    }
    #endregion

    #region Statistics & Events
    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        switch (cubeType)
        {
            case Enumerations.CubeType.Normal: normalCubesCaptured++; break;
            case Enumerations.CubeType.Blue: blueCubesCaptured++; break;
        }

        NotifyStageManager(sm => sm.OnCubeCaptured(cubeType));
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
        cubesEscaped++;
        NotifyStageManager(sm => sm.OnCubeEscaped(cubeType));
    }

    public void OnMarkerPlaced() => markersPlaced++;

    public void OnDetonationUsed() => detonationsUsed++;

    public void OnNonBlackCubeProcessed(Enumerations.CubeType cubeType, bool wasCaptured)
    {
        if (cubeType == Enumerations.CubeType.Black) return;

        processedNonBlackCubes++;
        DebugLog($"📊 Non-black cube processed: {processedNonBlackCubes}/{totalNonBlackCubes}");

        if (processedNonBlackCubes >= totalNonBlackCubes)
        {
            string reason = wasCaptured ? "All cubes captured!" : "All cubes processed!";
            StartCoroutine(ShowCompletionMessage(reason));
        }
    }

    private IEnumerator ShowCompletionMessage(string reason)
    {
        var message = new WaveMessage
        {
            Message = $"Wave Complete!\n{reason}",
            RequirePause = true,
            AutoHideDelay = 0f
        };

        yield return DisplayMessage(message);
        CompleteWave();
    }
    #endregion

    #region Utility Methods
    private void HandleInput()
    {
        if (!waveActive && Input.GetKeyDown(KeyCode.Return))
        {
            StartWave();
        }

        if (isPaused && Input.GetKeyDown(KeyCode.K))
        {
            // Message confirmation handled in DisplayMessage
        }
    }

    private void HandleDebugCommands()
    {
        if (!showDebugInfo) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            DebugActiveCubes();
        }
    }

    public void SetSpeedState(bool isSpeeding)
    {
        isSpeedingUp = isSpeeding;
    }

    private float GetCurrentMoveInterval()
    {
        float normal = CurrentWave?.moveInterval ?? normalMoveInterval;
        float fast = CurrentWave?.fastMoveInterval ?? fastMoveInterval;
        return isSpeedingUp ? fast : normal;
    }

    private float GetWaveStartDelay()
    {
        return CurrentWave?.waveStartDelay ?? waveStartDelay;
    }

    private bool HasActiveCubes()
    {
        return activeCubes.Count > 0 && !debugMode;
    }

    private Enumerations.CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < normalCubeChance) return Enumerations.CubeType.Normal;
        if (random < normalCubeChance + blueCubeChance) return Enumerations.CubeType.Blue;
        return Enumerations.CubeType.Black;
    }

    private void CountNonBlackCubes()
    {
        totalNonBlackCubes = activeCubes.Count(c => c != null && c.type != Enumerations.CubeType.Black);
        processedNonBlackCubes = 0;
    }

    private void ResetWaveStatistics()
    {
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        cubesEscaped = 0;
        markersPlaced = 0;
        detonationsUsed = 0;
        totalNonBlackCubes = 0;
        processedNonBlackCubes = 0;
    }

    private void ResetPlayer()
    {
        if (player != null && !debugMode)
        {
            player.enabled = true;
            player.ResetMarkers();
        }
    }

    private void ConfigurePlayer()
    {
        if (player != null && !debugMode)
        {
            player.enabled = true;
        }
    }

    private void NotifyStepComplete()
    {
        
    }

    private void NotifyStageManager(System.Action<StageManager> action)
    {
        var stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null) action(stageManager);
    }

    private void AdvanceToNextWave()
    {
        if (!useWaveConfiguration) return;

        currentWaveIndex++;
        if (currentWaveIndex < waveConfiguration.Count)
        {
            StartCoroutine(DelayedWaveStart());
        }
    }

    private IEnumerator DelayedWaveStart()
    {
        yield return new WaitForSeconds(3f);
        StartWave();
    }

    private void CleanupWave()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        waveActive = false;
        ClearAllCubes();
    }

    private void DebugActiveCubes()
    {
        DebugLog($"==== Active Cubes: {activeCubes.Count} ====");
        for (int i = 0; i < activeCubes.Count; i++)
        {
            var cube = activeCubes[i];
            if (cube != null)
            {
                DebugLog($"[{i}] {cube.type} at ({cube.position.x}, {cube.position.y})");
            }
        }
    }

    private void DebugLog(string message)
    {
        if (showDebugInfo) Debug.Log($"[WaveManager] {message}");
    }
    #endregion

    #region Public Properties (for Debuggers)
    public int MarkerChargeLimit() => CurrentWave?.limitMarkers == true ? CurrentWave.maxMarkerCharge : -1;
    public int MarkerCountLimit() => CurrentWave?.limitMarkers == true ? CurrentWave.maxMarkerCount : -1;
    public int CurrentWaveIndex => currentWaveIndex;
    #endregion
}