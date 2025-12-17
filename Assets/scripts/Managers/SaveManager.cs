using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Enumerations;

// Note: Uses Unity's JsonUtility for serialization.
// JsonUtility is compatible with Steam Cloud (file-based sync).
// If you need advanced JSON features, add com.unity.nuget.newtonsoft-json package.

/// <summary>
/// Manages player progression persistence.
/// Steam Cloud Ready: Uses Application.persistentDataPath with JSON serialization.
/// 
/// Save triggers:
/// - On attunement unlock/equip
/// - On stage complete
/// - On currency change
/// - On application quit
/// 
/// Usage:
/// - SaveManager.Instance.Progression.axiomShards
/// - SaveManager.Instance.Save()
/// </summary>
public class SaveManager : MonoBehaviour, IManagerDebugInterface
{
    #region Singleton
    
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;
    
    /// <summary>
    /// Check if SaveManager exists and is initialized.
    /// </summary>
    public static bool IsInitialized => _instance != null && _instance._isInitialized;
    
    #endregion
    
    #region Constants
    
    /// <summary>
    /// Current save format version. Increment when changing save structure.
    /// </summary>
    private const int CURRENT_SAVE_VERSION = 1;
    
    /// <summary>
    /// Axiom Shards earned per wave completion (first clear only).
    /// </summary>
    public const int SHARDS_PER_WAVE = 100;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Save Settings")]
    [Tooltip("Auto-save on significant events (stage complete, unlock, etc.)")]
    [SerializeField] private bool autoSaveEnabled = true;
    
    #endregion
    
    #region Runtime State
    
    private SaveData _saveData;
    private bool _isInitialized = false;
    private bool _isDirty = false; // Track unsaved changes
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Current player progression data.
    /// </summary>
    public PlayerProgression Progression => _saveData?.progression;
    
    /// <summary>
    /// Current Axiom Shards balance.
    /// </summary>
    public int AxiomShards => Progression?.axiomShards ?? 0;
    
    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => _isDirty;
    
    #endregion
    
    #region IManagerDebugInterface
    
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }
    
    public string GetDebugStatus()
    {
        if (!_isInitialized) return "NOT INITIALIZED";
        
        int unlocked = Progression?.unlockedAttunements?.Count ?? 0;
        int cleared = Progression?.clearedStageIndices?.Count ?? 0;
        string dirty = _isDirty ? " [UNSAVED]" : "";
        
        return $"Shards: {AxiomShards} | Unlocked: {unlocked} | Stages: {cleared}{dirty}";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Initialized"] = _isInitialized,
            ["Axiom Shards"] = AxiomShards,
            ["Unlocked Attunements"] = Progression?.unlockedAttunements?.Count ?? 0,
            ["Cleared Stages"] = Progression?.clearedStageIndices?.Count ?? 0,
            ["Highest Stage"] = Progression?.highestStageUnlocked ?? 0,
            ["Hub - Resonance Chamber"] = Progression?.resonanceAlignmentUnlocked ?? false,
            ["Hub - Chronicle"] = Progression?.observationChronicleUnlocked ?? false,
            ["Has Unsaved Changes"] = _isDirty,
            ["Auto Save Enabled"] = autoSaveEnabled,
            ["Save Path"] = SavePaths.ProgressionFile
        };
    }
    
    /// <summary>
    /// Reset SaveManager to fresh state (deletes save file).
    /// </summary>
    public void ResetToDefaults()
    {
        DebugLog("ResetToDefaults called - deleting save and resetting");
        DeleteSave();
    }
    
    /// <summary>
    /// Load a named save configuration.
    /// SaveManager uses a single save file, so this reloads the current save.
    /// </summary>
    public void LoadConfiguration(string configName)
    {
        DebugLog($"LoadConfiguration called with '{configName}' - reloading save");
        Load();
    }
    
    /// <summary>
    /// Save current state to a named configuration.
    /// SaveManager uses a single save file, so this saves to the standard location.
    /// </summary>
    public void SaveConfiguration(string configName)
    {
        DebugLog($"SaveConfiguration called with '{configName}' - saving");
        Save();
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            DebugLog("Duplicate SaveManager detected, destroying this instance");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    private void OnApplicationQuit()
    {
        // Always save on quit
        if (_isDirty)
        {
            DebugLog("Application quitting with unsaved changes, saving...");
            Save();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Save on mobile background (if applicable)
        if (pauseStatus && _isDirty)
        {
            DebugLog("Application pausing with unsaved changes, saving...");
            Save();
        }
    }
    
    private void Update()
    {
        // Debug: F3 to force save
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugLog("F3 pressed - forcing save");
            Save();
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to game events for progression tracking
        GameEvents.OnWaveComplete += HandleWaveComplete;
        GameEvents.OnStageComplete += HandleStageComplete;
        DebugLog("Subscribed to GameEvents for progression tracking");
    }
    
    private void OnDisable()
    {
        // Unsubscribe from game events
        GameEvents.OnWaveComplete -= HandleWaveComplete;
        GameEvents.OnStageComplete -= HandleStageComplete;
    }
    
    #endregion
    
    #region GameEvent Handlers
    
    /// <summary>
    /// Current stage index, cached from StageManager for wave completion rewards.
    /// </summary>
    private int _currentStageIndex = 0;
    
    /// <summary>
    /// Set current stage index (called by StageManager when stage starts).
    /// </summary>
    public void SetCurrentStage(int stageIndex)
    {
        _currentStageIndex = stageIndex;
        DebugLog($"Current stage set to {stageIndex}");
    }
    
    /// <summary>
    /// Handle wave complete event from GameEvents.
    /// </summary>
    private void HandleWaveComplete(int waveIndex)
    {
        // Get current stage from StageManager if available
        int stageIndex = _currentStageIndex;
        
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null && stageManager.CurrentStage != null)
        {
            // Get stage index from StageManager
            stageIndex = stageManager.CurrentStageIndex;
        }
        
        OnWaveComplete(stageIndex, waveIndex);
    }
    
    /// <summary>
    /// Handle stage complete event from GameEvents.
    /// </summary>
    private void HandleStageComplete(int stageIndex, bool success)
    {
        OnStageComplete(stageIndex, success);
    }
    
    #endregion
    
    #region Initialization
    
    private void Initialize()
    {
        EnsureSaveDirectory();
        Load();
        _isInitialized = true;
        
        DebugLog($"SaveManager initialized. Shards: {AxiomShards}");
    }
    
    private void EnsureSaveDirectory()
    {
        string saveFolder = SavePaths.SaveFolder;
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            DebugLog($"Created save directory: {saveFolder}");
        }
    }
    
    #endregion
    
    #region Save/Load
    
    /// <summary>
    /// Save current progression to disk.
    /// </summary>
    public void Save()
    {
        if (_saveData == null)
        {
            DebugLog("Cannot save: SaveData is null");
            return;
        }
        
        try
        {
            // Update metadata
            _saveData.lastSaveTime = DateTime.UtcNow.ToString("o");
            _saveData.platform = Application.platform.ToString();
            _saveData.saveVersion = CURRENT_SAVE_VERSION;
            
            // Serialize to JSON (Unity's JsonUtility)
            string json = JsonUtility.ToJson(_saveData, true);
            
            // Write to file
            File.WriteAllText(SavePaths.ProgressionFile, json);
            
            _isDirty = false;
            DebugLog($"Saved progression to {SavePaths.ProgressionFile}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load progression from disk.
    /// </summary>
    public void Load()
    {
        string filePath = SavePaths.ProgressionFile;
        
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                _saveData = JsonUtility.FromJson<SaveData>(json);
                
                // Validate loaded data
                if (_saveData == null)
                {
                    DebugLog("Loaded null SaveData, creating new save");
                    _saveData = CreateNewSave();
                }
                else if (_saveData.progression == null)
                {
                    DebugLog("Loaded SaveData with null progression, initializing");
                    _saveData.progression = new PlayerProgression();
                }
                
                // Handle version migration
                if (_saveData.saveVersion < CURRENT_SAVE_VERSION)
                {
                    MigrateData(_saveData.saveVersion);
                }
                
                DebugLog($"Loaded save v{_saveData.saveVersion} with {AxiomShards} shards");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load save: {e.Message}");
                DebugLog("Creating new save due to load failure");
                _saveData = CreateNewSave();
            }
        }
        else
        {
            DebugLog("No save file found, creating new save");
            _saveData = CreateNewSave();
            Save(); // Create initial save file
        }
        
        _isDirty = false;
    }
    
    /// <summary>
    /// Create a fresh save with default values.
    /// </summary>
    private SaveData CreateNewSave()
    {
        return new SaveData
        {
            saveVersion = CURRENT_SAVE_VERSION,
            lastSaveTime = DateTime.UtcNow.ToString("o"),
            platform = Application.platform.ToString(),
            progression = new PlayerProgression()
        };
    }
    
    /// <summary>
    /// Migrate save data from older versions.
    /// </summary>
    private void MigrateData(int fromVersion)
    {
        DebugLog($"Migrating save from v{fromVersion} to v{CURRENT_SAVE_VERSION}");
        
        // Future migrations go here
        // if (fromVersion < 2) { /* migrate v1 to v2 */ }
        
        _saveData.saveVersion = CURRENT_SAVE_VERSION;
        _isDirty = true;
    }
    
    /// <summary>
    /// Delete save file and reset to fresh state.
    /// Use for debugging or "New Game" functionality.
    /// </summary>
    public void DeleteSave()
    {
        string filePath = SavePaths.ProgressionFile;
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            DebugLog("Deleted save file");
        }
        
        _saveData = CreateNewSave();
        _isDirty = false;
        Save();
        
        DebugLog("Reset to new save");
    }
    
    #endregion
    
    #region Currency Operations
    
    /// <summary>
    /// Award Axiom Shards to the player.
    /// </summary>
    public void AwardShards(int amount, string reason = null)
    {
        if (amount <= 0) return;
        
        Progression.AddShards(amount);
        _isDirty = true;
        
        DebugLog($"Awarded {amount} shards{(reason != null ? $" ({reason})" : "")}. Total: {AxiomShards}");
        
        if (autoSaveEnabled)
        {
            Save();
        }
    }
    
    /// <summary>
    /// Try to spend Axiom Shards. Returns true if successful.
    /// </summary>
    public bool TrySpendShards(int amount, string reason = null)
    {
        if (Progression.TrySpendShards(amount))
        {
            _isDirty = true;
            DebugLog($"Spent {amount} shards{(reason != null ? $" ({reason})" : "")}. Remaining: {AxiomShards}");
            
            if (autoSaveEnabled)
            {
                Save();
            }
            return true;
        }
        
        DebugLog($"Failed to spend {amount} shards (have {AxiomShards})");
        return false;
    }
    
    #endregion
    
    #region Stage Operations
    
    /// <summary>
    /// Handle wave completion - award shards if first playthrough of stage.
    /// </summary>
    public void OnWaveComplete(int stageIndex, int waveIndex)
    {
        // Only award shards on first playthrough
        if (!Progression.HasClearedStage(stageIndex))
        {
            AwardShards(SHARDS_PER_WAVE, $"Wave {waveIndex + 1} complete");
        }
        else
        {
            DebugLog($"Wave {waveIndex + 1} complete (replay - no shards)");
        }
    }
    
    /// <summary>
    /// Handle stage completion - calculate score, award shards, mark as cleared.
    /// </summary>
    public void OnStageComplete(int stageIndex, bool success)
    {
        if (!success) return;
        
        bool wasFirstClear = !Progression.HasClearedStage(stageIndex);
        Progression.MarkStageCleared(stageIndex);
        _isDirty = true;
        
        // Calculate score and award shards (first clear only)
        if (wasFirstClear && ScoreManager.IsInitialized)
        {
            // Calculate final score with grade
            int totalWaves = 1; // Default
            var stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager?.CurrentStage != null)
            {
                totalWaves = stageManager.CurrentStage.waveConfigurations?.Count ?? 1;
            }
            
            int baseShards = totalWaves * SHARDS_PER_WAVE;
            var scoreResult = ScoreManager.Instance.CalculateStageResult(baseShards);
            
            // Award shards based on grade
            AwardShards(scoreResult.finalShards, $"Stage {stageIndex} complete ({scoreResult.grade})");
            
            // Record lifetime stats
            Progression.RecordStageCompletion(stageIndex, scoreResult.totalMovesUsed);
            Progression.RecordCubesCaptured(scoreResult.totalCubesCaptured);
            Progression.RecordCubesEscaped(scoreResult.totalEscapes);
            
            DebugLog($"Stage {stageIndex} first clear! Grade: {scoreResult.grade} ({scoreResult.gradePercentage:F0}%) - Shards: {scoreResult.finalShards}");
            
            // Check hub unlocks
            if (stageIndex >= 3)
            {
                DebugLog("Hub areas unlocked: Resonance Alignment Chamber, Observation Chronicle");
            }
        }
        else if (wasFirstClear)
        {
            // Fallback if ScoreManager not available
            AwardShards(SHARDS_PER_WAVE, $"Stage {stageIndex} complete");
            DebugLog($"Stage {stageIndex} first clear (no scoring)");
        }
        else
        {
            DebugLog($"Stage {stageIndex} replay complete (no shards)");
        }
        
        if (autoSaveEnabled)
        {
            Save();
        }
    }
    
    /// <summary>
    /// Check if replaying a previously cleared stage.
    /// </summary>
    public bool IsReplayingStage(int stageIndex)
    {
        return Progression.HasClearedStage(stageIndex);
    }
    
    #endregion
    
    #region Attunement Operations
    
    /// <summary>
    /// Try to unlock an attunement by spending shards.
    /// </summary>
    public bool TryUnlockAttunement(string attunementId, int cost)
    {
        if (Progression.IsAttunementUnlocked(attunementId))
        {
            DebugLog($"Attunement {attunementId} already unlocked");
            return false;
        }
        
        if (TrySpendShards(cost, $"Unlock {attunementId}"))
        {
            Progression.UnlockAttunement(attunementId);
            DebugLog($"Unlocked attunement: {attunementId}");
            
            if (autoSaveEnabled)
            {
                Save();
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Equip an attunement for a marker type.
    /// </summary>
    public bool EquipAttunement(MarkerMode mode, string attunementId)
    {
        // Unit markers have no attunements
        if (mode == MarkerMode.Unit)
        {
            DebugLog("Cannot equip attunement to Unit markers");
            return false;
        }
        
        // Empty string = unequip (always valid)
        if (string.IsNullOrEmpty(attunementId))
        {
            Progression.SetEquippedAttunement(mode, "");
            _isDirty = true;
            DebugLog($"Unequipped attunement from {mode}");
            
            if (autoSaveEnabled)
            {
                Save();
            }
            return true;
        }
        
        // Must be unlocked to equip
        if (!Progression.IsAttunementUnlocked(attunementId))
        {
            DebugLog($"Cannot equip locked attunement: {attunementId}");
            return false;
        }
        
        Progression.SetEquippedAttunement(mode, attunementId);
        _isDirty = true;
        DebugLog($"Equipped {attunementId} to {mode}");
        
        if (autoSaveEnabled)
        {
            Save();
        }
        return true;
    }
    
    /// <summary>
    /// Get the currently equipped attunement ID for a marker type.
    /// Returns empty string if none equipped.
    /// </summary>
    public string GetEquippedAttunement(MarkerMode mode)
    {
        return Progression?.GetEquippedAttunement(mode) ?? "";
    }
    
    /// <summary>
    /// Check if a specific attunement is currently equipped.
    /// </summary>
    public bool HasAttunementEquipped(MarkerMode mode, string attunementId)
    {
        return GetEquippedAttunement(mode) == attunementId;
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }
    
    #endregion
}

/// <summary>
/// Static helper for save file paths.
/// Steam Cloud Ready: All paths under Application.persistentDataPath.
/// </summary>
public static class SavePaths
{
    /// <summary>
    /// Root folder for all save files.
    /// </summary>
    public static string SaveFolder => 
        Path.Combine(Application.persistentDataPath, "SaveData");
    
    /// <summary>
    /// Main progression save file.
    /// </summary>
    public static string ProgressionFile => 
        Path.Combine(SaveFolder, "progression.json");
}

