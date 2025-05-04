using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue

    [Header("Wave Settings")]
    [SerializeField] private int waveSize = 3;
    [SerializeField][Range(0f, 1f)] private float normalCubeChance = 0.7f;
    [SerializeField][Range(0f, 1f)] private float greenCubeChance = 0.2f;
    [SerializeField][Range(0f, 1f)] private float blackCubeChance = 0.1f;
    [SerializeField] private bool autoCalculateBlackChance = true;

    [Header("Rain Controls")]
    [SerializeField] private Enumerations.CubeType rainCubeType;
    [SerializeField] private int rainColumn = 0;
    [SerializeField] private float rainHeight = 5f;
    [SerializeField] private float rainSpeed = 3f; // Units per second

    [Header("Manual Wave Control")]
    [SerializeField] private bool manualWaveControl = true;
    [SerializeField] private float stepDelay = 0.25f;

    private bool debuggerActive = false;
    private Vector2 scrollPosition;
    private List<GameObject> debugObjects = new List<GameObject>();
    private bool isProcessing = false;

    private void Awake()
    {
        // Auto-find references if not set
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();

        // Validate prefabs
        if (cubePrefabs == null || cubePrefabs.Length < 3)
        {
            Debug.LogError("WaveDebugger requires at least 3 cube prefabs (Normal, Green, Black)!");
            enabled = false;
        }
    }

    private void Update()
    {
        // Toggle debugger with key
        if (Input.GetKeyDown(KeyCode.F5))
        {
            debuggerActive = !debuggerActive;
            Debug.Log($"Wave Debugger: {(debuggerActive ? "Active" : "Inactive")}");
        }
    }

    private void OnGUI()
    {
        if (!debuggerActive) return;

        // Main debugger panel
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("=== WAVE DEBUGGER ===", GUI.skin.box);

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
        if (manualWaveControl)
        {
            if (GUILayout.Button("Move Wave Forward"))
            {
                MoveWaveForward();
            }
        }

        GUILayout.Space(10);

        // Rain cube controls
        GUILayout.Label("Rain Single Cube:", GUI.skin.box);

        // Rain cube type selection
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        int selectedIndex = System.Array.IndexOf(typeNames, rainCubeType.ToString());
        if (selectedIndex < 0) selectedIndex = 0;

        rainCubeType = (Enumerations.CubeType)System.Enum.Parse(
            typeof(Enumerations.CubeType),
            typeNames[GUILayout.SelectionGrid(selectedIndex, typeNames, 2)]);

        // Column selection
        GUILayout.BeginHorizontal();
        GUILayout.Label("Column: ");

        // Create buttons for each column
        if (grid != null)
        {
            for (int i = 0; i < grid.Width; i++)
            {
                GUI.backgroundColor = (rainColumn == i) ? Color.green : Color.white;
                if (GUILayout.Button(i.ToString(), GUILayout.Width(25)))
                {
                    rainColumn = i;
                }
            }
        }
        else
        {
            rainColumn = EditorIntField("", rainColumn);
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        rainHeight = EditorSlider("Rain Height:", rainHeight, 2f, 10f);
        rainSpeed = EditorSlider("Rain Speed:", rainSpeed, 1f, 10f);

        if (GUILayout.Button("Rain Cube"))
        {
            RainCube(rainColumn, rainCubeType);
        }

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
            if (grid.tiles != null && position.x >= 0 && position.x < grid.Width &&
                position.y >= 0 && position.y < grid.Height)
            {
                Tile tile = grid.tiles[position.x, position.y];
                if (tile != null)
                {
                    tile.currentCube = behavior;
                }
            }
        }
    }

    private void RainCube(int column, Enumerations.CubeType type)
    {
        if (grid == null || column < 0 || column >= grid.Width) return;

        // Find prefab for this type
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {type}");
            return;
        }

        // Spawn the cube high above the column
        Vector3 spawnPos = new Vector3(column, rainHeight, grid.Height - 1);
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = type;
            }

            Vector2Int targetPos = new Vector2Int(column, grid.Height - 1);
            behavior.Init(grid, targetPos, 1);

            // Set as raining cube to enable smooth fall animation
            behavior.isRainingCube = true;
            behavior.rainSpeed = rainSpeed; // Set custom rain speed

            debugObjects.Add(cube);

            // Register with wave manager for movement with the wave
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(behavior);
            }
        }
    }

    private void MoveWaveForward()
    {
        if (waveManager == null || !manualWaveControl) return;

        isProcessing = true;
        StartCoroutine(ProcessWaveStep());
    }

    private IEnumerator ProcessWaveStep()
    {
        if (waveManager != null)
        {
            waveManager.ManualMoveWaveForward();
        }

        yield return new WaitForSeconds(stepDelay);
        isProcessing = false;
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