using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static StageData;

public class WaveManager : MonoBehaviour
{
    private class BlackCubeRainData
    {
        public Vector2Int targetPosition;
        public int countdown = 3; // Number of moves before landing
        public GameObject indicator;
    }
    private class ReturnQueueItem
    {
        public Enumerations.CubeType cubeType;
        public Vector2 position;
    }

    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] public GameObject[] cubePrefabs;
    [SerializeField] public bool useWaveConfiguration = false;
    [SerializeField] private PlayerManager player;
    [SerializeField] private DetonationManager detonationManager;
    [SerializeField] private CubeData cubeData;

    [Header("Wave Settings")]
    [SerializeField] public List<WaveData> waveConfiguration = new List<WaveData>();
    [SerializeField] public int waveSize = 3;
    [SerializeField] private float waveStartDelay = 0.75f;
    [SerializeField] public int MoveStep;
    [SerializeField] private float tileScale = 3f;

    [Header("Cube Type Chances")]
    [SerializeField][Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField][Range(0f, 1f)] private float blueCubeChance = 0.2f;
    // Black cubes make up the remainder

    [Header("Speed Controls")]
    [SerializeField] private float normalMoveInterval = 1.75f; // Default speed
    [SerializeField] private float fastMoveInterval = 0.1f;
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private GameObject returnIndicatorPrefab;

    [Header("UI References")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Highlight System")]
    [SerializeField] private Material tileHighlightMaterial;
    [SerializeField] private Material cubeHighlightMaterial;

    [Header("Wave Tracking")]
    [SerializeField] private int currentWaveIndex = 0;
    private WaveData currentWave;
    public int CurrentWaveIndex => currentWaveIndex;
    public WaveData CurrentWave => currentWave;
    private int normalCubesCaptured = 0;
    private int blueCubesCaptured = 0;
    private int cubesEscaped = 0;
    private int markersPlaced = 0;
    private int detonationsUsed = 0;

    [Header("Wave Completion")]
    [SerializeField] private bool trackWaveCompletion = true;
    [SerializeField] private int totalNonBlackCubes = 0;
    [SerializeField] private int processedNonBlackCubes = 0;

    // Events for wave completion
    public System.Action OnWaveCompleted;
    public System.Action<string> OnWaveCompletedWithReason;


    public bool isSpeedingUp = false;
    public List<CubeBehavior> activeCubes = new List<CubeBehavior>();
    public List<Vector2> escapedBlackCubePositions = new List<Vector2>();
    public bool waveActive = false;
    private Coroutine waveCoroutine;
    private List<ReturnQueueItem> returnQueue = new List<ReturnQueueItem>();
    private List<BlackCubeRainData> rainingBlackCubes = new List<BlackCubeRainData>();
    private bool isDebugWaveActive = false;
    public bool debugMode = false;
    public bool manualControl = false;

    // Message system
    private bool isMessageActive = false;
    private bool isPaused = false;
    private List<GameObject> activeHighlights = new List<GameObject>();
    private List<WaveMessage> activeMessageObjects = new List<WaveMessage>();
    private Queue<WaveMessage> pendingMessages = new Queue<WaveMessage>();
    private bool isProcessingMessageQueue = false;



    public void SetSpeedState(bool isSpeeding)
    {
        isSpeedingUp = isSpeeding;
    }

    private void Awake()
    {
        ValidateReferences();

    }

    private void ValidateReferences()
    {
        if (grid == null)
        {
            grid = FindObjectOfType<GridManager>();
            if (grid == null)
            {
                Debug.LogError("WaveManager requires a GridManager reference!");
                enabled = false;
                return;
            }

            tileScale = grid.TileScale;
        }

        if (player == null)
        {
            player = FindObjectOfType<PlayerManager>();
            if (player == null)
            {
                Debug.LogWarning("PlayerController reference not set in WaveManager!");
            }
        }

        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            Debug.LogError("WaveManager requires at least 3 cube prefabs (Normal, Blue, Black)!");
            enabled = false;
            return;
        }

        if (cubeData == null)
            cubeData = new CubeData();

        // Hide message panel initially
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    private void Update()
    {
        if (!waveActive && Input.GetKeyDown(KeyCode.Return))
        {
            StartWave();
        }

        if (showDebugInfo && Input.GetKeyDown(KeyCode.L))
        {
            DebugActiveCubes();
        }

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

    public void StartWave()
    {
        if (waveActive) return;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        // Set the current wave
        if (useWaveConfiguration && waveConfiguration.Count > 0)
        {
            currentWaveIndex = 0;
            currentWave = waveConfiguration[currentWaveIndex];
        }
        else
        {
            currentWave = null;
        }

        ResetWaveStatistics();
        waveCoroutine = StartCoroutine(RunWave());
        UpdateReturnVisuals();
    }

    private void NotifyMovementComplete()
    {
        MoveStep++;

        // Check for wave messages at this move step
        if (currentWave != null)
        {
            var messages = currentWave.messages.FindAll(m => m.DisplayMoveStep == MoveStep);
            foreach (var message in messages)
            {
                ShowMessage(message, message.RequirePause, message.AutoHideDelay);
            }
        }

        // Notify stage manager
        StageManager stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            stageManager.MoveStepComplete();
        }
    }

    private IEnumerator RunWave(bool resume = false)
    {
        waveActive = true;
        MoveStep = 0;

        // Show initial messages if available
        if (currentWave != null && !resume)
        {
            var initialMessages = currentWave.messages.FindAll(m => m.DisplayMoveStep == 0);
            foreach (var message in initialMessages)
            {
                ShowMessage(message, message.RequirePause, message.AutoHideDelay);
            }
        }

        // Toggle player input
        if (player != null && !debugMode)
        {
            player.enabled = true;
            player.ResetMarkers(); // Only reset markers when not in debug mode
        }

        // Reset all tile markers only when not in debug mode
        if (grid != null && !debugMode)
        {
            grid.ClearAllMarkers();
        }

        if (!resume)
            SpawnCubes();

        // Get wave-specific start delay
        float startDelay = currentWave != null ? currentWave.waveStartDelay : waveStartDelay;
        yield return new WaitForSeconds(startDelay);


        // Skip automatic wave progression if we're in manual control mode
        if (manualControl)
        {
            // Just wait indefinitely until manual control is disabled
            while (manualControl)
            {
                yield return null;
            }

            waveActive = false;
            waveCoroutine = null;
            yield break;
        }

        // Normal automatic wave progression
        bool cubesRemaining = true;
        while (cubesRemaining)
        {
            cubesRemaining = false;

            for (int i = activeCubes.Count - 1; i >= 0; i--)
            {
                if (i >= activeCubes.Count) continue; // Safety check for if list size changes

                CubeBehavior cube = activeCubes[i];
                if (cube != null)
                {
                    cube.ResetMovementState();
                    bool stillAlive = cube.MoveForward();

                    if (!stillAlive)
                    {
                        activeCubes.RemoveAt(i);
                    }
                    else
                    {
                        cubesRemaining = true;
                    }
                }
                else
                {
                    // Remove null references
                    activeCubes.RemoveAt(i);
                }
            }

            // Notify that a movement cycle is complete
            NotifyMovementComplete();

            if (activeCubes.Count == 0 && !debugMode)
                cubesRemaining = false;

            // Use appropriate move interval based on speed up state and current wave settings
            float normalInterval = currentWave != null ? currentWave.moveInterval : normalMoveInterval;
            float fastInterval = currentWave != null ? currentWave.fastMoveInterval : fastMoveInterval;
            float currentMoveInterval = isSpeedingUp ? fastInterval : normalInterval;

            yield return new WaitForSeconds(currentMoveInterval);
        }

        // Wave is complete, reset state
        if (grid != null)
        {
            grid.ClearAllMarkers();
        }

        waveActive = false;

        // Clear any highlights
        ClearAllHighlights();

        // Check for end-of-wave messages
        if (currentWave != null)
        {
            var endMessages = currentWave.messages.FindAll(m => m.DisplayMoveStep == -1);
            foreach (var message in endMessages)
            {
                ShowMessage(message, message.RequirePause, message.AutoHideDelay);
            }
        }

        // If there are more waves, advance to the next one
        if (useWaveConfiguration && currentWaveIndex < waveConfiguration.Count - 1)
        {
            StartCoroutine(AdvanceToNextWave());
        }
        else
        {
            // All waves complete
            waveCoroutine = null;
        }
    }

    // Message display system with pause functionality
    public void ShowMessage(WaveMessage message, bool pauseGameplay = false, float autoHideDelay = 0f)
    {
        if (messagePanel == null || messageText == null ||
            (message.DisplayMoveStep != MoveStep && message.DisplayMoveStep != -1))
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

    // Highlight system for tiles and cubes
    public void HighlightTile(int x, int y, Color color)
    {
        if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height)
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

    public void RegisterDebugWave(List<GameObject> debugCubes)
    {
        // Clear existing active cubes first
        activeCubes.Clear();

        // Convert GameObject references to CubeBehavior references
        foreach (GameObject obj in debugCubes)
        {
            if (obj != null)
            {
                CubeBehavior cube = obj.GetComponent<CubeBehavior>();
                if (cube != null)
                {
                    activeCubes.Add(cube);
                }
            }
        }

        // Disable automatic wave spawning (if applicable)
        isDebugWaveActive = true;
    }

    public void ClearAllCubes()
    {
        // Clear active cubes
        foreach (CubeBehavior cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }
        activeCubes.Clear();

        // Reset other wave-related states
        isDebugWaveActive = false;
    }

    public void RegisterEscapedBlackCube(Vector2 position)
    {
        // Keep using your existing returnQueue but ensure it's a black cube
        returnQueue.Add(new ReturnQueueItem
        {
            cubeType = Enumerations.CubeType.Black,
            position = position
        });

        // Log for debugging
        Debug.Log($"Black cube escaped at X={position.x}, queued for return");
    }

    public void RegisterEscapedCube(CubeBehavior cube)
    {
        if (cube.type != Enumerations.CubeType.Normal)
        {
            returnQueue.Add(new ReturnQueueItem
            {
                cubeType = cube.type,
                position = cube.position
            });

            // Log for debugging
            Debug.Log($"{cube.type} cube escaped at X={cube.position.x}, queued for return");
        }
    }


    public void EnterDebugMode(bool manual)
    {
        debugMode = true;
        manualControl = manual;
        // Reset any ongoing waves
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        waveActive = false;
    }

    public void ExitDebugMode()
    {
        debugMode = false;
        manualControl = false;
    }

    public void RegisterCube(CubeBehavior cube)
    {
        if (!activeCubes.Contains(cube))
        {
            activeCubes.Add(cube);
        }
    }

    public void ManualMoveWaveForward()
    {
        if (!debugMode) return;

        // Don't reset player markers when in debug mode
        bool resetMarkers = !debugMode;

        // Process one movement step for all active cubes
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (i >= activeCubes.Count) continue; // Safety check

            CubeBehavior cube = activeCubes[i];
            if (cube != null)
            {
                cube.ResetMovementState();
                bool stillAlive = cube.MoveForward();

                if (!stillAlive)
                {
                    activeCubes.RemoveAt(i);
                }
            }
            else
            {
                // Remove null references
                activeCubes.RemoveAt(i);
            }
        }

        // Notify that a movement cycle is complete
        NotifyMovementComplete();
    }

    public void SpawnCustomWave(List<WaveData> waveData, bool useDebugMode)
    {
        // Clear existing cubes
        ClearAllCubes();
        waveConfiguration.Clear();
        activeCubes.Clear();
        MoveStep = 0;

        // Set debug mode
        debugMode = useDebugMode;
        manualControl = useDebugMode;
        // Process wave data and spawn cubes
        foreach (var data in waveData)
        {
            waveConfiguration.Add(new WaveData
            {
                Index = data.Index,
                CubesData = data.CubesData
            }); ;
        }

        // Start wave if not in debug mode
        StartCoroutine(RunWave());

    }

    private void SpawnCubes()
    {
        activeCubes.Clear();
        player.ResetMarkers();

        // Reset wave completion tracking
        totalNonBlackCubes = 0;
        processedNonBlackCubes = 0;

        // Guard against missing grid
        if (grid == null) return;

        if (useWaveConfiguration && waveConfiguration.Count > 0)
        {
            GenerateConfigurationWave();
        }
        else
            GenerateRandomWave();

        // Count total non-black cubes for completion tracking
        CountNonBlackCubes();

        // Now spawn any escaped black cubes that need to "rain down"
        SpawnRainingBlackCubes();

        Debug.Log($"Wave started with {totalNonBlackCubes} non-black cubes to process");
    }

    private void GenerateRandomWave()
    {
        List<int> spawnZs = new List<int>();
        for (var i = 1; i <= waveSize; i++)
        {
            spawnZs.Add(grid.height - i);
        }

        foreach (int z in spawnZs)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3 spawnPos = new Vector3(x * tileScale, 1f * tileScale, z * tileScale);
                Enumerations.CubeType cubeType = GetRandomCubeType();

                cubeData.position = pos;
                cubeData.type = cubeType;


                // Guard against index out of bounds
                int prefabIndex = (int)cubeType;
                if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
                {
                    Debug.LogWarning($"Missing cube prefab for type {cubeType}");
                    continue;
                }

                GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);
                if (cube != null)
                {
                    CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                    if (cb == null)
                    {
                        cb = cube.AddComponent<CubeBehavior>();
                        cb.type = cubeType; // Set type since it wasn't in prefab
                    }

                    cb.Init(grid, cubeData, 1 * tileScale); // level 1 for all cubes in this version
                    activeCubes.Add(cb);
                }
            }
        }
    }

    private void GenerateConfigurationWave()
    {

        var cubes = waveConfiguration[currentWaveIndex].CubesData;

        foreach (var item in cubes)
        {
            Vector2Int pos = item.position;
            item.position = new Vector2Int(pos.x, pos.y);

            Vector3 spawnPos = new Vector3(pos.x * tileScale, 1f * tileScale, pos.y * tileScale);

            // Guard against index out of bounds
            int prefabIndex = (int)item.type;
            if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
            {
                Debug.LogWarning($"Missing cube prefab for type {item.type}");
                continue;
            }

            GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);
            if (cube != null)
            {
                CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                if (cb == null)
                {
                    cb = cube.AddComponent<CubeBehavior>();
                    cb.type = item.type;
                }

                cb.Init(grid, item, 1 * tileScale);
                activeCubes.Add(cb);
            }
        }
    }

    public void SpawnRainingBlackCubes()
    {
        if (escapedBlackCubePositions.Count == 0) return;

        // Get black cube prefab index
        int blackCubeIndex = (int)Enumerations.CubeType.Black;
        if (blackCubeIndex < 0 || blackCubeIndex >= cubePrefabs.Length || cubePrefabs[blackCubeIndex] == null)
        {
            Debug.LogWarning("Missing black cube prefab for raining mechanism");
            escapedBlackCubePositions.Clear();
            return;
        }

        // For each escaped black cube
        foreach (Vector2 position in escapedBlackCubePositions)
        {
            // Position is directly above the grid at the same X position
            // The Y is higher to create a raining effect
            Vector3 spawnPos = new Vector3(position.x, 5f, position.y);

            GameObject cube = Instantiate(cubePrefabs[blackCubeIndex], spawnPos, Quaternion.identity);
            if (cube != null)
            {
                CubeBehavior cb = cube.GetComponent<CubeBehavior>();
                if (cb == null)
                {
                    cb = cube.AddComponent<CubeBehavior>();
                    cb.type = Enumerations.CubeType.Black;
                    cb.isRainingCube = true;
                }

                activeCubes.Add(cb);

                // Add a rain controller component to handle the specialized behavior
                CubeCollisionController rainController = cube.AddComponent<CubeCollisionController>();
                rainController.Initialize(grid);

                // Don't add to active cubes - the rain controller will handle movement
            }
        }

        // Clear the list after spawning
        escapedBlackCubePositions.Clear();
    }

    private Enumerations.CubeType GetRandomCubeType()
    {
        float random = Random.value;
        if (random < normalCubeChance)
            return Enumerations.CubeType.Normal;
        else if (random < normalCubeChance + blueCubeChance)
            return Enumerations.CubeType.Blue;
        else
            return Enumerations.CubeType.Black;
    }

    private void DebugActiveCubes()
    {
        Debug.Log($"==== Active Cubes: {activeCubes.Count} ====");
        for (int i = 0; i < activeCubes.Count; i++)
        {
            CubeBehavior cube = activeCubes[i];
            if (cube != null)
            {
                Debug.Log($"[{i}] Cube at ({cube.position.x}, {cube.position.y}) of type {cube.type}");
            }
            else
            {
                Debug.Log($"[{i}] NULL CUBE REFERENCE");
            }
        }
    }

    // Add this new method for rain cubes to register with the wave system
    public void RegisterRainCube(CubeBehavior cube)
    {
        if (cube == null) return;

        // Ensure it's not already in the list
        if (!activeCubes.Contains(cube))
        {
            activeCubes.Add(cube);

            Debug.Log($"Rain cube registered: Type={cube.type}, " +
                      $"Grid Position=({cube.position.x}, {cube.position.y}), " +
                      $"World Position=({cube.transform.position.x}, {cube.transform.position.y}, {cube.transform.position.z}), " +
                      $"Moves Remaining={cube.moveCountRemaining}");
        }
    }

    public void CubeRainLanded(CubeBehavior cube)
    {
        if (cube == null) return;

        // The cube has completed its vertical falling animation
        // but it's still part of the wave system with moveCountRemaining

        // Update tile reference if needed
        Vector2Int pos = cube.position;
        Tile tile = null;
        if (grid != null && pos.x >= 0 && pos.x < grid.Width && pos.y >= 0 && pos.y < grid.Height)
        {
            tile = grid.tiles[pos.x, pos.y];
            if (tile != null)
            {
                // Only update the tile reference if this is the final landing
                // or if the tile doesn't have a cube yet
                if (cube.moveCountRemaining <= 0 || tile.currentCube == null)
                {
                    tile.ProcessCubeInteraction(cube);
                }
            }
        }

        // Check for collisions now that the cube has landed
        cube.CheckForCollisionOnLanding();
        if (tile != null)
        {
            if (tile.IsAdvantaged)
            {
                detonationManager.TriggerNextDetonation(tile.x, tile.y);
            }
        }

        Debug.Log($"Cube rain landed at ({pos.x}, {pos.y}), " +
                  $"world pos ({cube.transform.position.x}, {cube.transform.position.y}, {cube.transform.position.z}), " +
                  $"moves remaining: {cube.moveCountRemaining}");
    }

    private void UpdateReturnVisuals()
    {
        // Clear old indicators
        GameObject[] oldIndicators = GameObject.FindGameObjectsWithTag("ReturnIndicator");
        foreach (GameObject indicator in oldIndicators)
        {
            Destroy(indicator);
        }

        // Create new indicators
        foreach (ReturnQueueItem item in returnQueue)
        {
            Vector3 indicatorPos = new Vector3(item.position.x, 6f, item.position.y);
            GameObject indicator = Instantiate(returnIndicatorPrefab, indicatorPos, Quaternion.identity);
            indicator.tag = "ReturnIndicator";

            // Set color based on type
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                switch (item.cubeType)
                {
                    case Enumerations.CubeType.Black:
                        renderer.material.color = Color.black;
                        break;
                    case Enumerations.CubeType.Blue:
                        renderer.material.color = Color.blue;
                        break;
                }
            }
        }
    }

    // Called when the game is being shut down or scene is changing
    private void OnDestroy()
    {
        // Clean up any active coroutines
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // Clean up any remaining cubes
        foreach (var cube in activeCubes)
        {
            if (cube != null && cube.gameObject != null)
            {
                Destroy(cube.gameObject);
            }
        }

        activeCubes.Clear();
    }

    internal void ConfigureSpawn(List<CubeData> spawnData)
    {

    }

    // Add these methods to your WaveManager.cs
    public void PauseWave()
    {
        if (!waveActive) return;

        // Stop the coroutine but maintain state
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        // Enter debug/manual mode
        debugMode = true;
        manualControl = true;
        waveActive = false;
    }

    public void ResumeWave()
    {
        if (waveActive) return;

        // Restart the wave coroutine
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        // Start a new wave coroutine
        waveCoroutine = StartCoroutine(RunWave(true));

        // Exit manual mode but keep debug
        debugMode = true;
        manualControl = false;
    }

    private void ResetWaveStatistics()
    {
        normalCubesCaptured = 0;
        blueCubesCaptured = 0;
        cubesEscaped = 0;
        markersPlaced = 0;
        detonationsUsed = 0;
    }

    // Method to get the current marker limit
    public int MarkerChargeLimit()
    {
        if (currentWave != null && currentWave.limitMarkers)
        {
            return currentWave.maxMarkerCharge;
        }
        return -1; // No limit
    }

    public int MarkerCountLimit()
    {
        if (currentWave != null && currentWave.limitMarkers)
        {
            return currentWave.maxMarkerCount;
        }
        return -1; // No limit
    }

    public IEnumerator AdvanceToNextWave()
    {
        if (!useWaveConfiguration) yield return null;

        // Save statistics for the current wave
        if (currentWave != null)
        {
            currentWave.normalCubesCaptured = normalCubesCaptured;
            currentWave.blueCubesCaptured = blueCubesCaptured;
            currentWave.cubesEscaped = cubesEscaped;
            currentWave.markersPlaced = markersPlaced;
            currentWave.detonationsUsed = detonationsUsed;
        }

        // Move to next wave
        currentWaveIndex++;

        yield return new WaitForSeconds(3);

        if (currentWaveIndex < waveConfiguration.Count)
        {
            currentWave = waveConfiguration[currentWaveIndex];
            ResetWaveStatistics();

            // Start the new wave
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
            }
            waveCoroutine = StartCoroutine(RunWave());
        }
        else
        {
            // End of all waves
            currentWaveIndex = -1;
            currentWave = null;

            // Notify stage manager that all waves are complete
            StageManager stageManager = FindObjectOfType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnWaveCompleted();
            }
        }
    }

    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        switch (cubeType)
        {
            case Enumerations.CubeType.Normal:
                normalCubesCaptured++;
                break;
            case Enumerations.CubeType.Blue:
                blueCubesCaptured++;
                break;
        }

        // Notify stage manager
        StageManager stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            stageManager.OnCubeCaptured(cubeType);
        }
    }

    public void OnCubeEscaped(Enumerations.CubeType cubeType)
    {
        cubesEscaped++;

        // Notify stage manager
        StageManager stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            stageManager.OnCubeEscaped(cubeType);
        }
    }

    public void OnMarkerPlaced()
    {
        markersPlaced++;
    }

    public void OnDetonationUsed()
    {
        detonationsUsed++;
    }

    private void CountNonBlackCubes()
    {
        totalNonBlackCubes = 0;
        foreach (CubeBehavior cube in activeCubes)
        {
            if (cube != null && cube.type != Enumerations.CubeType.Black)
            {
                totalNonBlackCubes++;
            }
        }
    }

    public void OnNonBlackCubeProcessed(Enumerations.CubeType cubeType, bool wasCaptured)
    {
        if (!trackWaveCompletion || cubeType == Enumerations.CubeType.Black) return;

        processedNonBlackCubes++;

        string reason = wasCaptured ? "captured" : "escaped";
        Debug.Log($"Non-black cube {reason}. Progress: {processedNonBlackCubes}/{totalNonBlackCubes}");

        // Check if wave is complete
        if (processedNonBlackCubes >= totalNonBlackCubes)
        {
            CompleteWave(wasCaptured ? "All non-black cubes captured!" : "All non-black cubes processed!");
        }
    }

    private void CompleteWave(string reason)
    {
        Debug.Log($"Wave completed: {reason}");

        // Stop the wave
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        waveActive = false;

        // Clear remaining cubes (only black cubes should remain)
        for (int i = activeCubes.Count - 1; i >= 0; i--)
        {
            if (activeCubes[i] != null && activeCubes[i].type != Enumerations.CubeType.Black)
            {
                Destroy(activeCubes[i].gameObject);
                activeCubes.RemoveAt(i);
            }
        }

        // Notify listeners
        OnWaveCompleted?.Invoke();
        OnWaveCompletedWithReason?.Invoke(reason);

        // Show completion message
        StartCoroutine(ShowWaveCompletionMessage(reason));
    }

    private IEnumerator ShowWaveCompletionMessage(string reason)
    {
        // Create a simple wave completion message
        WaveMessage completionMessage = new WaveMessage
        {
            Message = $"Wave Complete!\n{reason}",
            RequirePause = true,
            AutoHideDelay = 0f
        };

        ShowMessage(completionMessage, true, 0f);

        yield return new WaitForSeconds(2f);

        // Check for next wave or end
        if (useWaveConfiguration && currentWaveIndex < waveConfiguration.Count - 1)
        {
            StartCoroutine(AdvanceToNextWave());
        }
        else
        {
            Debug.Log("All waves completed!");
        }
    }
}