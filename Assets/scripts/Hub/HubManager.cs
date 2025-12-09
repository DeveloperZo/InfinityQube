using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages hub scene state and transitions to gameplay.
/// Singleton that persists across hub visits.
/// </summary>
public class HubManager : MonoBehaviour
{
    #region Singleton
    
    private static HubManager _instance;
    public static HubManager Instance => _instance;
    public static bool IsInitialized => _instance != null;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "Main";
    [SerializeField] private string hubSceneName = "Hub";
    
    [Header("Hub Buildings")]
    [SerializeField] private List<HubBuilding> hubBuildings = new List<HubBuilding>();
    
    [Header("Camera")]
    [SerializeField] private Camera hubCamera;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private int selectedStageIndex = 0;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The stage index selected for next gameplay session.
    /// </summary>
    public int SelectedStageIndex => selectedStageIndex;
    
    /// <summary>
    /// Current Axiom Shards (from SaveManager).
    /// </summary>
    public int AxiomShards => SaveManager.IsInitialized ? SaveManager.Instance.AxiomShards : 0;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        
        // Find hub buildings if not assigned
        if (hubBuildings.Count == 0)
        {
            hubBuildings.AddRange(FindObjectsByType<HubBuilding>(FindObjectsSortMode.None));
        }
        
        // Setup camera for click detection
        SetupCamera();
    }
    
    private void Start()
    {
        RefreshHubState();
        DebugLog("HubManager initialized");
    }
    
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
    
    #endregion
    
    #region Setup
    
    private void SetupCamera()
    {
        if (hubCamera == null)
        {
            hubCamera = Camera.main;
        }
        
        // Ensure camera has PhysicsRaycaster for UI events on 3D objects
        if (hubCamera != null && hubCamera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
        {
            hubCamera.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            DebugLog("Added PhysicsRaycaster to hub camera");
        }
    }
    
    #endregion
    
    #region Hub State
    
    /// <summary>
    /// Refresh hub building states based on current progression.
    /// </summary>
    public void RefreshHubState()
    {
        foreach (var building in hubBuildings)
        {
            if (building != null)
            {
                building.RefreshLockedState();
            }
        }
        
        DebugLog($"Hub state refreshed. Shards: {AxiomShards}");
    }
    
    #endregion
    
    #region Stage Selection
    
    /// <summary>
    /// Select a stage for the next gameplay session.
    /// </summary>
    public void SelectStage(int stageIndex)
    {
        // Check if stage is unlocked
        if (SaveManager.IsInitialized)
        {
            int highestUnlocked = SaveManager.Instance.Progression.highestStageUnlocked;
            if (stageIndex > highestUnlocked)
            {
                DebugLog($"Stage {stageIndex} is locked (highest unlocked: {highestUnlocked})");
                return;
            }
        }
        
        selectedStageIndex = stageIndex;
        DebugLog($"Selected stage: {stageIndex}");
    }
    
    /// <summary>
    /// Start the selected stage - transition to gameplay scene.
    /// </summary>
    public void StartSelectedStage()
    {
        StartStage(selectedStageIndex);
    }
    
    /// <summary>
    /// Start a specific stage - transition to gameplay scene.
    /// </summary>
    public void StartStage(int stageIndex)
    {
        DebugLog($"Starting stage {stageIndex}...");
        
        // Store selected stage for StageManager to pick up
        PlayerPrefs.SetInt("SelectedStage", stageIndex);
        PlayerPrefs.Save();
        
        // Load gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }
    
    #endregion
    
    #region Scene Transitions
    
    /// <summary>
    /// Return to hub from gameplay (call after stage complete).
    /// </summary>
    public static void ReturnToHub()
    {
        // Get hub scene name from instance or use default
        string hubScene = IsInitialized ? Instance.hubSceneName : "Hub";
        SceneManager.LoadScene(hubScene);
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[HubManager] {message}");
        }
    }
    
    #endregion
}

