using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runtime system for loading and managing test scenarios.
/// Integrates with the PrototypingSystem for rapid iteration.
/// </summary>
public class ScenarioLoader : MonoBehaviour
{
    #region Singleton
    
    public static ScenarioLoader Instance { get; private set; }
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Configuration")]
    [Tooltip("All available scenarios (auto-populated in editor)")]
    [SerializeField] private List<ScenarioData> scenarios = new List<ScenarioData>();
    
    [Tooltip("Hotkey to reload last scenario")]
    [SerializeField] private KeyCode reloadHotkey = KeyCode.F5;
    
    [Tooltip("Require modifier key for hotkeys")]
    [SerializeField] private bool requireShiftForHotkey = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private ScenarioData currentScenario;
    private ScenarioData lastLoadedScenario;
    
    // Manager references (cached)
    private WaveManager waveManager;
    private GridManager gridManager;
    private PlayerManager playerManager;
    private PlayerActionManager actionManager;
    private StageManager stageManager;
    
    #endregion
    
    #region Events
    
    /// <summary>Fired when a scenario is loaded</summary>
    public event System.Action<ScenarioData> OnScenarioLoaded;
    
    /// <summary>Fired when scenario loading fails</summary>
    public event System.Action<ScenarioData, string> OnScenarioLoadFailed;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        CacheManagerReferences();
        
        // Auto-discover scenarios if list is empty
        if (scenarios.Count == 0)
        {
            RefreshScenarioList();
        }
        
        Log($"ScenarioLoader initialized with {scenarios.Count} scenarios");
    }
    
    private void Update()
    {
        HandleHotkeys();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #endregion
    
    #region Manager References
    
    private void CacheManagerReferences()
    {
        waveManager = Object.FindFirstObjectByType<WaveManager>();
        gridManager = GridManager.Instance;
        playerManager = Object.FindFirstObjectByType<PlayerManager>();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        stageManager = Object.FindFirstObjectByType<StageManager>();
    }
    
    private void EnsureManagerReferences()
    {
        if (waveManager == null) waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (gridManager == null) gridManager = GridManager.Instance;
        if (playerManager == null) playerManager = Object.FindFirstObjectByType<PlayerManager>();
        if (actionManager == null) actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        if (stageManager == null) stageManager = Object.FindFirstObjectByType<StageManager>();
    }
    
    #endregion
    
    #region Hotkeys
    
    private void HandleHotkeys()
    {
        bool modifierPressed = !requireShiftForHotkey || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (modifierPressed && Input.GetKeyDown(reloadHotkey))
        {
            ReloadLastScenario();
        }
    }
    
    #endregion
    
    #region Public API - Loading
    
    /// <summary>
    /// Load and apply a scenario.
    /// </summary>
    public bool LoadScenario(ScenarioData scenario)
    {
        if (scenario == null)
        {
            LogWarning("Cannot load null scenario");
            return false;
        }
        
        EnsureManagerReferences();
        
        Log($"Loading scenario: {scenario.scenarioName}");
        
        try
        {
            // 1. Handle stage loading - use stageNumber directly
            if (scenario.stage != null && stageManager != null)
            {
                stageManager.LoadStage(scenario.stage.stageNumber);
            }
            
            // 2. Set wave index
            if (scenario.waveIndex >= 0 && waveManager != null)
            {
                waveManager.StopWave();
                waveManager.currentWaveIndex = scenario.waveIndex;
            }
            
            // 3. Clear existing state
            if (scenario.clearExistingCubes)
            {
                waveManager?.StopWave();
                waveManager?.ClearAllCubes();
                actionManager?.MarkerSystem?.ClearPlayerCubes();
            }
            
            if (scenario.clearExistingMarkers)
            {
                gridManager?.ClearAllMarkers();
                actionManager?.MarkerSystem?.ClearAllActions();
            }
            
            // 4. Apply grid overrides
            if (scenario.gridWidthOverride > 0 || scenario.gridHeightOverride > 0)
            {
                int width = scenario.gridWidthOverride > 0 ? scenario.gridWidthOverride : gridManager?.Width ?? 6;
                int height = scenario.gridHeightOverride > 0 ? scenario.gridHeightOverride : gridManager?.Height ?? 20;
                gridManager?.ResizeGrid(width, height);
            }
            
            // 5. Reset player position
            if (scenario.resetPlayerPosition && playerManager != null && gridManager != null)
            {
                playerManager.currentTilePosition = scenario.playerPosition;
                playerManager.transform.position = gridManager.GridToWorldPosition(
                    scenario.playerPosition.x, scenario.playerPosition.y, 0);
            }
            
            // 6. Set marker charges
            if (actionManager != null)
            {
                if (scenario.unitMarkerCharges >= 0)
                    actionManager.SetUnitMarkerCharges(scenario.unitMarkerCharges);
                if (scenario.matrixMarkerCharges >= 0)
                    actionManager.SetMatrixMarkerCharges(scenario.matrixMarkerCharges);
                if (scenario.recursionMarkerCharges >= 0)
                    actionManager.SetRecursionMarkerCharges(scenario.recursionMarkerCharges);
                if (scenario.infinityMarkerCharges >= 0)
                    actionManager.SetInfinityMarkerCharges(scenario.infinityMarkerCharges);
            }
            
            // 7. Spawn wave cubes
            foreach (var cube in scenario.waveCubes)
            {
                SpawnCube(cube, isPlayerCube: false);
            }
            
            // 8. Spawn player cubes
            foreach (var cube in scenario.playerCubes)
            {
                SpawnCube(cube, isPlayerCube: true);
            }
            
            // 9. Place markers
            foreach (var marker in scenario.markers)
            {
                PlaceMarker(marker);
            }
            
            // 10. Apply timing
            Time.timeScale = scenario.timeScale;
            
            if (scenario.startWaveOnLoad && waveManager != null)
            {
                waveManager.StartWaveWithoutSpawning();
            }
            
            if (scenario.pauseOnLoad)
            {
                Time.timeScale = 0f;
            }
            
            // Track current and last scenario
            currentScenario = scenario;
            lastLoadedScenario = scenario;
            
            Log($"Scenario loaded: {scenario.scenarioName} ({scenario.GetSummary()})");
            OnScenarioLoaded?.Invoke(scenario);
            
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"Failed to load scenario '{scenario.scenarioName}': {e.Message}");
            OnScenarioLoadFailed?.Invoke(scenario, e.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Load a scenario by name.
    /// </summary>
    public bool LoadScenarioByName(string name)
    {
        var scenario = scenarios.FirstOrDefault(s => s.scenarioName == name);
        if (scenario == null)
        {
            scenario = scenarios.FirstOrDefault(s => s.name == name);
        }
        
        if (scenario == null)
        {
            LogWarning($"Scenario not found: {name}");
            return false;
        }
        
        return LoadScenario(scenario);
    }
    
    /// <summary>
    /// Reload the last loaded scenario.
    /// </summary>
    public bool ReloadLastScenario()
    {
        if (lastLoadedScenario == null)
        {
            LogWarning("No scenario to reload");
            return false;
        }
        
        Log($"Reloading scenario: {lastLoadedScenario.scenarioName}");
        return LoadScenario(lastLoadedScenario);
    }
    
    #endregion
    
    #region Public API - Query
    
    /// <summary>
    /// Get all available scenarios.
    /// </summary>
    public List<ScenarioData> GetAllScenarios()
    {
        return new List<ScenarioData>(scenarios);
    }
    
    /// <summary>
    /// Get scenarios filtered by category.
    /// </summary>
    public List<ScenarioData> GetScenariosByCategory(ScenarioCategory category)
    {
        return scenarios
            .Where(s => s.category == category)
            .OrderBy(s => s.priority)
            .ThenBy(s => s.scenarioName)
            .ToList();
    }
    
    /// <summary>
    /// Get scenarios filtered by tag.
    /// </summary>
    public List<ScenarioData> GetScenariosByTag(string tag)
    {
        return scenarios
            .Where(s => s.tags != null && s.tags.Contains(tag))
            .OrderBy(s => s.priority)
            .ToList();
    }
    
    /// <summary>
    /// Get all unique tags across all scenarios.
    /// </summary>
    public List<string> GetAllTags()
    {
        return scenarios
            .Where(s => s.tags != null)
            .SelectMany(s => s.tags)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }
    
    /// <summary>
    /// Get the currently loaded scenario.
    /// </summary>
    public ScenarioData GetCurrentScenario()
    {
        return currentScenario;
    }
    
    /// <summary>
    /// Get the last loaded scenario (for reload functionality).
    /// </summary>
    public ScenarioData GetLastLoadedScenario()
    {
        return lastLoadedScenario;
    }
    
    #endregion
    
    #region Public API - Management
    
    /// <summary>
    /// Register a scenario at runtime.
    /// </summary>
    public void RegisterScenario(ScenarioData scenario)
    {
        if (scenario != null && !scenarios.Contains(scenario))
        {
            scenarios.Add(scenario);
            Log($"Registered scenario: {scenario.scenarioName}");
        }
    }
    
    /// <summary>
    /// Refresh the scenario list from Resources and AssetDatabase (editor only).
    /// Scenarios should be placed in Assets/Resources/Scenarios/
    /// </summary>
    public void RefreshScenarioList()
    {
        scenarios.Clear();
        
#if UNITY_EDITOR
        // In editor, find ALL ScenarioData assets in project
        var guids = AssetDatabase.FindAssets("t:ScenarioData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioData>(path);
            if (scenario != null && !scenarios.Contains(scenario))
            {
                scenarios.Add(scenario);
            }
        }
        Log($"[Editor] Found {scenarios.Count} scenarios via AssetDatabase");
#else
        // At runtime, load from Resources/Scenarios folder
        var foundScenarios = Resources.LoadAll<ScenarioData>("Scenarios");
        scenarios.AddRange(foundScenarios);
        
        // Also check root Resources folder for backwards compatibility
        var rootScenarios = Resources.LoadAll<ScenarioData>("");
        foreach (var scenario in rootScenarios)
        {
            if (!scenarios.Contains(scenario))
            {
                scenarios.Add(scenario);
            }
        }
        Log($"[Runtime] Found {scenarios.Count} scenarios via Resources");
#endif
        
        // Sort by category, then priority, then name
        scenarios = scenarios
            .OrderBy(s => s.category)
            .ThenBy(s => s.priority)
            .ThenBy(s => s.scenarioName)
            .ToList();
        
        Log($"Refreshed scenario list: {scenarios.Count} total scenarios");
    }
    
    #endregion
    
    #region Helper Methods
    
    private void SpawnCube(ScenarioCubePlacement placement, bool isPlayerCube)
    {
        if (waveManager?.cubePrefabs == null || gridManager == null) return;
        
        int typeIndex = (int)placement.type;
        if (typeIndex >= waveManager.cubePrefabs.Length)
        {
            LogWarning($"No prefab for cube type {placement.type}");
            return;
        }
        
        if (!gridManager.IsValidGridPosition(placement.position))
        {
            LogWarning($"Invalid position for cube: {placement.position}");
            return;
        }
        
        Vector3 worldPos = gridManager.GridToWorldPosition(placement.position.x, placement.position.y, 2f);
        var cubeObj = Instantiate(waveManager.cubePrefabs[typeIndex], worldPos, Quaternion.identity);
        var cube = cubeObj.GetComponent<CubeManager>() ?? cubeObj.AddComponent<CubeManager>();
        
        var cubeData = new CubeData
        {
            type = placement.type,
            position = placement.position,
            level = placement.level
        };
        
        cube.Init(gridManager, cubeData, 2f);
        cube.isPlayerCube = isPlayerCube;
        
        if (isPlayerCube)
        {
            cube.isMatrixCube = placement.type == CubeType.Matrix;
            cube.usePhysics = false;
            cube.ConfigurePlayerCubePhysics();
            cube.ApplyPlayerCubeMaterial();
            actionManager?.MarkerSystem?.playerCubes?.Add(cube);
        }
        else
        {
            waveManager.activeCubes?.Add(cube);
        }
    }
    
    private void PlaceMarker(ScenarioMarkerPlacement placement)
    {
        if (gridManager == null || actionManager == null) return;
        
        if (!gridManager.IsValidGridPosition(placement.position))
        {
            LogWarning($"Invalid position for marker: {placement.position}");
            return;
        }
        
        // Store current mode, set to desired mode, place marker, restore
        var currentMode = actionManager.GetCurrentMode();
        actionManager.SetMode(placement.markerMode);
        gridManager.PlaceMarker(placement.position.x, placement.position.y);
        actionManager.SetMode(currentMode);
    }
    
    // GetStageIndex removed - using stage.stageNumber directly with StageManager.LoadStage
    
    #endregion
    
    #region Logging
    
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ScenarioLoader] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ScenarioLoader] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ScenarioLoader] {message}");
    }
    
    #endregion
}
