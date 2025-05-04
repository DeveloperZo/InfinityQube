using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private DetonationManager detonationManager;

    [Header("Wave Settings")]
    [SerializeField] private int waveSize = 3;
    [SerializeField][Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField][Range(0f, 1f)] private float greenCubeChance = 0.2f;
    [SerializeField][Range(0f, 1f)] private float blackCubeChance = 0.1f;
    [SerializeField] private bool autoCalculateBlackChance = true;
    [SerializeField] private int escapedBlackCubesCount = 0;

    [Header("Tile Selection")]
    [SerializeField] private int selectedTileX = 0;
    [SerializeField] private int selectedTileY = 0;
    [SerializeField] private Material selectedTileMaterial;
    [SerializeField] private Material normalTileMaterial;
    [SerializeField] private int advantagedTileCharges = 3;

    [Header("Wave Control")]
    [SerializeField] private bool manualWaveControl = true;
    [SerializeField] private float stepDelay = 0.25f;
    [SerializeField] private float autoMoveInterval = 0.5f;

    private bool debuggerActive = false;
    private Vector2 scrollPosition;
    public List<GameObject> debugObjects = new List<GameObject>();
    private bool isProcessing = false;
    private GameObject selectedTileHighlight;
    private Coroutine autoMoveCoroutine;
    private Tile lastHighlightedTile;

    private void Awake()
    {
        // Auto-find references if not set
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (detonationManager == null) detonationManager = FindObjectOfType<DetonationManager>();

        // Create materials if not assigned
        if (selectedTileMaterial == null)
        {
            selectedTileMaterial = new Material(Shader.Find("Standard"));
            selectedTileMaterial.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        }

        if (normalTileMaterial == null)
        {
            normalTileMaterial = new Material(Shader.Find("Standard"));
            normalTileMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        // Validate prefabs
        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            Debug.LogError("WaveDebugger requires at least 3 cube prefabs (Normal, Green, Black)!");
            enabled = false;
        }
    }

    private void Start()
    {
        CreateTileHighlight();
    }

    private void OnDestroy()
    {
        DestroyTileHighlight();
        StopAllCoroutines();
    }

    private void Update()
    {
        // Toggle debugger with key
        if (Input.GetKeyDown(KeyCode.F2))
        {
            debuggerActive = !debuggerActive;
            if (debuggerActive)
            {
                selectedTileHighlight.SetActive(true);
                UpdateTileHighlightPosition();
            }
            else
            {
                selectedTileHighlight.SetActive(false);
            }
            Debug.Log($"Wave Debugger: {(debuggerActive ? "Active" : "Inactive")}");
        }

        if (!debuggerActive) return;

        // Move wave forward with key
        if (Input.GetKeyDown(KeyCode.M))
        {
            MoveWaveForward();
        }

        // Transform selected tile
        if (Input.GetKeyDown(KeyCode.B))
        {
            BlackenSelectedTile();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            AdvantageSelectedTile();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            TriggerSelectedTile();
        }
    }

    private void CreateTileHighlight()
    {
        DestroyTileHighlight();

        selectedTileHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectedTileHighlight.name = "TileHighlight";
        selectedTileHighlight.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);

        Renderer renderer = selectedTileHighlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = selectedTileMaterial;
            renderer.material.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        }

        Collider collider = selectedTileHighlight.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        selectedTileHighlight.SetActive(debuggerActive);
        UpdateTileHighlightPosition();
    }

    private void DestroyTileHighlight()
    {
        if (selectedTileHighlight != null)
        {
            Destroy(selectedTileHighlight);
            selectedTileHighlight = null;
        }
    }

    private void UpdateTileHighlightPosition()
    {
        if (selectedTileHighlight == null || grid == null) return;

        // Restore material on previously highlighted tile if it exists
        if (lastHighlightedTile != null)
        {
            // Don't change the material if the tile was modified
            if (!lastHighlightedTile.IsBlackened && !lastHighlightedTile.HasCharges)
            {
                Renderer tileRenderer = lastHighlightedTile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material = normalTileMaterial;
                }
            }
        }

        // Validate selected coordinates
        selectedTileX = Mathf.Clamp(selectedTileX, 0, grid.Width - 1);
        selectedTileY = Mathf.Clamp(selectedTileY, 0, grid.Height - 1);

        // Position the highlight above the selected tile
        selectedTileHighlight.transform.position = new Vector3(selectedTileX, 0.1f, selectedTileY);

        // Store reference to current highlighted tile
        if (grid.tiles != null && selectedTileX >= 0 && selectedTileX < grid.Width &&
            selectedTileY >= 0 && selectedTileY < grid.Height)
        {
            lastHighlightedTile = grid.tiles[selectedTileX, selectedTileY];
        }
    }

    private void BlackenSelectedTile()
    {
        if (grid == null || !IsValidPosition(selectedTileX, selectedTileY)) return;

        Tile tile = grid.tiles[selectedTileX, selectedTileY];
        if (tile != null)
        {
            tile.ResetTile();
            tile.BlackenTile();
            Debug.Log($"Blackened tile at ({selectedTileX}, {selectedTileY})");
        }
    }

    private void AdvantageSelectedTile()
    {
        if (grid == null || !IsValidPosition(selectedTileX, selectedTileY)) return;

        Tile tile = grid.tiles[selectedTileX, selectedTileY];
        if (tile != null)
        {
            tile.ResetTile();
            tile.AdvantageTile(advantagedTileCharges);
            detonationManager.RegisterDetonationPoint(new Vector2Int(selectedTileX, selectedTileY), Enumerations.DetonationType.Small);
            Debug.Log($"Advantaged tile at ({selectedTileX}, {selectedTileY}) with {advantagedTileCharges} charges");
        }
    }

    private void TriggerSelectedTile()
    {
        if (grid == null || !IsValidPosition(selectedTileX, selectedTileY)) return;

        Tile tile = grid.tiles[selectedTileX, selectedTileY];
        if (tile != null && tile.HasCharges && detonationManager != null)
        {
            detonationManager.TriggerNextDetonation(selectedTileX, selectedTileY);
            Debug.Log($"Triggered advantaged tile at ({selectedTileX}, {selectedTileY})");
        }
        else
        {
            Debug.Log("Cannot trigger selected tile (not advantaged or no charges)");
        }
    }

    private void OnGUI()
    {
        if (!debuggerActive) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("WAVE DEBUGGER", GUI.skin.box);

        // Wave configuration section
        GUILayout.Label("Wave Configuration:", GUI.skin.box);

        waveSize = Mathf.Clamp(EditorIntField("Wave Size:", waveSize), 1, 10);

        GUILayout.Label("Cube Distribution:");
        normalCubeChance = EditorSlider("Normal %:", normalCubeChance * 100, 0f, 100f) / 100f;
        greenCubeChance = EditorSlider("Green %:", greenCubeChance * 100, 0f, 100f) / 100f;

        if (autoCalculateBlackChance)
        {
            blackCubeChance = 1f - normalCubeChance - greenCubeChance;
            GUILayout.Label($"Black %: {blackCubeChance * 100:F1}");
        }
        else
        {
            blackCubeChance = EditorSlider("Black %:", blackCubeChance * 100, 0f, 100f) / 100f;
        }

        autoCalculateBlackChance = GUILayout.Toggle(autoCalculateBlackChance,
            "Auto-calculate Black cube chance");

        // Escaped Black Cubes Counter
        GUILayout.Space(10);
        GUILayout.Label("Escaped Black Cubes:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", GUILayout.Width(30)))
        {
            escapedBlackCubesCount = Mathf.Max(0, escapedBlackCubesCount - 1);
            UpdateEscapedBlackCubes();
        }

        escapedBlackCubesCount = EditorIntField("Count:", escapedBlackCubesCount);

        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            escapedBlackCubesCount++;
            UpdateEscapedBlackCubes();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Update Escaped Cubes"))
        {
            UpdateEscapedBlackCubes();
        }

        // Validate probabilities sum to 1.0
        float totalChance = normalCubeChance + greenCubeChance + blackCubeChance;
        if (!Mathf.Approximately(totalChance, 1.0f))
        {
            GUILayout.Label($"Warning: Chances sum to {totalChance:F2}, not 1.0",
                GUI.skin.box);

            if (GUILayout.Button("Normalize Probabilities"))
            {
                NormalizeProbabilities();
            }
        }

        GUILayout.Space(10);

        // Selected Tile Information
        GUILayout.Label("Selected Tile:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Position:", GUILayout.Width(60));

        // X coordinate with +/- buttons
        GUILayout.Label("X:", GUILayout.Width(15));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            selectedTileX = Mathf.Max(0, selectedTileX - 1);
            UpdateTileHighlightPosition();
        }
        GUILayout.Label(selectedTileX.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            selectedTileX = Mathf.Min(grid.Width - 1, selectedTileX + 1);
            UpdateTileHighlightPosition();
        }

        // Y coordinate with +/- buttons
        GUILayout.Label("Y:", GUILayout.Width(15));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            selectedTileY = Mathf.Max(0, selectedTileY - 1);
            UpdateTileHighlightPosition();
        }
        GUILayout.Label(selectedTileY.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            selectedTileY = Mathf.Min(grid.Height - 1, selectedTileY + 1);
            UpdateTileHighlightPosition();
        }

        GUILayout.EndHorizontal();

        // Tile state info
        if (IsValidPosition(selectedTileX, selectedTileY) && grid.tiles != null)
        {
            Tile tile = grid.tiles[selectedTileX, selectedTileY];
            if (tile != null)
            {
                GUILayout.Label($"State: {(tile.IsBlackened ? "Blackened" : tile.HasCharges ? "Advantaged" : "Normal")}");
                if (tile.HasCharges)
                {
                    GUILayout.Label($"Charges: {tile.DetonationCharges}");
                }
            }
        }

        // Advantaged tile charges setter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Advantage Charges:", GUILayout.Width(120));
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            advantagedTileCharges = Mathf.Max(1, advantagedTileCharges - 1);
        }
        GUILayout.Label(advantagedTileCharges.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            advantagedTileCharges = Mathf.Min(3, advantagedTileCharges + 1);
        }
        GUILayout.EndHorizontal();

        // Tile transformation buttons
        if (GUILayout.Button("Blacken Tile (B)"))
        {
            BlackenSelectedTile();
        }
        if (GUILayout.Button("Advantage Tile (G)"))
        {
            AdvantageSelectedTile();
        }
        if (GUILayout.Button("Trigger Tile (T)"))
        {
            TriggerSelectedTile();
        }

        GUILayout.Space(10);

        manualWaveControl = GUILayout.Toggle(manualWaveControl, "Enable Manual Wave Control");

        GUI.enabled = !isProcessing;

        // Wave controls 
        if (GUILayout.Button("Spawn Wave"))
        {
            SpawnDebugWave();
        }

        if (GUILayout.Button("Reset Grid & Clear Cubes"))
        {
            ClearAllCubes();
            ResetGrid();
        }

        // Manual wave movement
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Move Wave Forward (M)"))
        {
            MoveWaveForward();
        }
        if (GUILayout.Button(autoMoveCoroutine == null ? "Auto Move" : "Stop Auto Move"))
        {
            ToggleAutoMove();
        }
        GUILayout.EndHorizontal();

        GUI.enabled = true;

        GUILayout.Space(10);

        // Status information
        GUILayout.Label("Debug Status:", GUI.skin.box);
        int activeCubesCount = 0;
        if (waveManager != null && waveManager.activeCubes != null)
        {
            activeCubesCount = waveManager.activeCubes.Count;
        }
        GUILayout.Label($"Active Cubes: {activeCubesCount}");
        GUILayout.Label($"Debug Objects: {debugObjects.Count}");
        GUILayout.Label($"Escaped Black Cubes: {escapedBlackCubesCount}");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private float EditorSlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        float result = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.Label($"{result:F1}", GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return result;
    }

    private int EditorIntField(string label, int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        string result = GUILayout.TextField(value.ToString(), GUILayout.Width(50));
        GUILayout.EndHorizontal();

        int parsedValue;
        if (int.TryParse(result, out parsedValue))
            return parsedValue;
        return value;
    }

    private void NormalizeProbabilities()
    {
        float sum = normalCubeChance + greenCubeChance + blackCubeChance;
        if (sum <= 0)
        {
            // Default values if sum is zero
            normalCubeChance = 0.7f;
            greenCubeChance = 0.2f;
            blackCubeChance = 0.1f;
            return;
        }

        normalCubeChance /= sum;
        greenCubeChance /= sum;
        blackCubeChance /= sum;
    }

    private void UpdateEscapedBlackCubes()
    {
        if (waveManager == null) return;

        // Clear existing escaped black cubes
        waveManager.escapedBlackCubePositions.Clear();

        // Add the specified number of escaped black cubes
        for (int i = 0; i < escapedBlackCubesCount; i++)
        {
            // Distribute evenly across columns
            int x = i % grid.Width;
            waveManager.escapedBlackCubePositions.Add(new Vector2(x, grid.Height - 1));
        }

        Debug.Log($"Updated escaped black cubes count to {escapedBlackCubesCount}");
    }

    private void SpawnDebugWave()
    {
        if (grid == null) return;

        // First clear any existing cubes
        ClearAllCubes();

        // Prepare cube counts based on probabilities
        int totalCubes = waveSize * grid.Width;
        int normalCount = Mathf.RoundToInt(totalCubes * normalCubeChance);
        int greenCount = Mathf.RoundToInt(totalCubes * greenCubeChance);
        // Black cubes fill the remainder to ensure exact wave size
        int blackCount = totalCubes - normalCount - greenCount;

        Debug.Log($"Spawning debug wave: {normalCount} normal, {greenCount} green, {blackCount} black cubes");

        // Create a list of cube types to spawn
        List<Enumerations.CubeType> typesToSpawn = new List<Enumerations.CubeType>();
        for (int i = 0; i < normalCount; i++) typesToSpawn.Add(Enumerations.CubeType.Normal);
        for (int i = 0; i < greenCount; i++) typesToSpawn.Add(Enumerations.CubeType.Green);
        for (int i = 0; i < blackCount; i++) typesToSpawn.Add(Enumerations.CubeType.Black);

        // Shuffle to randomize placement
        ShuffleList(typesToSpawn);

        // Fill the wave rows
        int cubeIndex = 0;
        for (int z = grid.Height - 1; z > grid.Height - 1 - waveSize; z--)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (cubeIndex >= typesToSpawn.Count) break;

                Vector2Int pos = new Vector2Int(x, z);
                SpawnCube(pos, typesToSpawn[cubeIndex]);
                cubeIndex++;
            }
        }

        // Register with wave manager
        if (waveManager != null)
        {
            waveManager.EnterDebugMode(manualWaveControl);

            // Add cubes to wave manager
            foreach (GameObject obj in debugObjects)
            {
                if (obj != null)
                {
                    CubeBehavior cube = obj.GetComponent<CubeBehavior>();
                    if (cube != null)
                    {
                        waveManager.RegisterCube(cube);
                    }
                }
            }
        }
    }

    private void SpawnCube(Vector2Int position, Enumerations.CubeType type)
    {
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {type}");
            return;
        }

        Vector3 spawnPos = new Vector3(position.x, 1f, position.y);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = type;
            }

            behavior.Init(grid, position, 1);
            debugObjects.Add(cube);

            // Make sure the tile knows about this cube
            UpdateTileReference(position, behavior);
        }
    }

    private void MoveWaveForward()
    {
        if (waveManager == null) return;
        waveManager.debugMode = true;
        waveManager.manualControl = true;
        // Move the wave forward manually
        waveManager.ManualMoveWaveForward();
    }

    private void ToggleAutoMove()
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
            autoMoveCoroutine = null;
        }
        else
        {
            autoMoveCoroutine = StartCoroutine(AutoMoveCoroutine());
        }
    }

    private IEnumerator AutoMoveCoroutine()
    {
        waveManager.debugMode = true;
        waveManager.manualControl = true;

        while (true)
        {
            MoveWaveForward();
            yield return new WaitForSeconds(autoMoveInterval);

            // Check if we still have active cubes
            if (waveManager.activeCubes.Count == 0)
            {
                break;
            }
        }

        autoMoveCoroutine = null;
    }

    private void ClearAllCubes()
    {
        // Clear debug objects first
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null) Destroy(obj);
        }
        debugObjects.Clear();

        // Clear any remaining cubes from WaveManager
        if (waveManager != null)
        {
            waveManager.ClearAllCubes();
            waveManager.ExitDebugMode();
        }
        else
        {
            // Fallback if wave manager reference is missing
            foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
            {
                if (cube != null && cube.gameObject != null)
                {
                    Destroy(cube.gameObject);
                }
            }
        }
    }

    private void ResetGrid()
    {
        if (grid == null) return;

        // Reset all tiles to normal state
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.tiles[x, y] != null)
                {
                    grid.tiles[x, y].ResetTile();
                }
            }
        }
    }

    private void UpdateTileReference(Vector2Int position, CubeBehavior cube)
    {
        if (grid == null || position.x < 0 || position.x >= grid.Width ||
            position.y < 0 || position.y >= grid.Height) return;

        Tile tile = grid.tiles[position.x, position.y];
        if (tile != null)
        {
            tile.currentCube = cube;
        }
    }

    // Helper to check if a position is valid on the grid
    private bool IsValidPosition(int x, int y)
    {
        return grid != null && x >= 0 && x < grid.Width && y >= 0 && y < grid.Height;
    }

    // Helper to shuffle a list (Fisher-Yates)
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + Random.Range(0, n - i);
            T temp = list[r];
            list[r] = list[i];
            list[i] = temp;
        }
    }
}