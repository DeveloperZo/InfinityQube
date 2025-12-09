using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Central database for all stage configurations.
/// Provides stage lookup, validation, and runtime instantiation.
/// </summary>
[CreateAssetMenu(fileName = "StageDatabase", menuName = "Infinity Qube/Stage Database")]
public class StageDB : ScriptableObject
{
    #region Inspector Fields
    
    [Header("Stage Collection")]
    [SerializeField] private List<StageData> stages = new List<StageData>();
    
    [Header("Database Info (Read-Only)")]
    [SerializeField] private int _stageCount;
    [SerializeField] private int _totalWaveCount;
    [SerializeField] private List<string> _validationErrors = new List<string>();
    
    #endregion
    
    #region Runtime State
    
    private Dictionary<int, StageData> _stageMap = new Dictionary<int, StageData>();
    private bool _initialized = false;
    
    #endregion
    
    #region Properties
    
    /// <summary>Number of stages in database.</summary>
    public int StageCount => stages?.Count ?? 0;
    
    /// <summary>All stages in the database (read-only).</summary>
    public IReadOnlyList<StageData> Stages => stages;
    
    /// <summary>Whether database has been initialized.</summary>
    public bool IsInitialized => _initialized;
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initialize the database for runtime use.
    /// </summary>
    public void Initialize()
    {
        if (_initialized && _stageMap.Any()) return;

        _stageMap.Clear();

        foreach (var stage in stages)
        {
            if (stage != null)
            {
                if (_stageMap.ContainsKey(stage.stageNumber))
                {
                    Debug.LogWarning($"[StageDB] Duplicate stage number {stage.stageNumber}: {stage.stageName}");
                }
                _stageMap[stage.stageNumber] = stage;
            }
        }

        _initialized = true;
        Debug.Log($"[StageDB] Initialized with {_stageMap.Count} stages");
    }
    
    /// <summary>
    /// Force re-initialization of the database.
    /// </summary>
    public void Reinitialize()
    {
        _initialized = false;
        Initialize();
    }
    
    #endregion
    
    #region Stage Access
    
    /// <summary>
    /// Get a stage by ID. Returns an instantiated copy for runtime modification.
    /// </summary>
    public StageData GetStage(int stageId)
    {
        if (!_initialized)
            Initialize();

        if (_stageMap.TryGetValue(stageId, out StageData stage))
        {
            // Create runtime copy to avoid modifying the asset
            var newStage = Instantiate(stage);
            newStage.waveConfigurations = stage.waveConfigurations
                .Select(w => w != null ? Instantiate(w) : null)
                .ToList();
            return newStage;
        }

        Debug.LogWarning($"[StageDB] Stage {stageId} not found!");
        return null;
    }
    
    /// <summary>
    /// Get stage by ID without instantiation (for read-only access).
    /// </summary>
    public StageData GetStageReference(int stageId)
    {
        if (!_initialized)
            Initialize();
            
        return _stageMap.TryGetValue(stageId, out StageData stage) ? stage : null;
    }

    /// <summary>
    /// Get all available stage IDs, sorted.
    /// </summary>
    public List<int> GetAllStageIds()
    {
        if (!_initialized)
            Initialize();

        var ids = new List<int>(_stageMap.Keys);
        ids.Sort();
        return ids;
    }
    
    /// <summary>
    /// Get stages by type.
    /// </summary>
    public List<StageData> GetStagesByType(StageType type)
    {
        if (!_initialized)
            Initialize();
            
        return stages.Where(s => s != null && s.stageType == type).ToList();
    }
    
    /// <summary>
    /// Check if a stage exists.
    /// </summary>
    public bool HasStage(int stageId)
    {
        if (!_initialized)
            Initialize();
            
        return _stageMap.ContainsKey(stageId);
    }
    
    /// <summary>
    /// Get the next stage ID after the given one.
    /// </summary>
    public int GetNextStageId(int currentStageId)
    {
        var ids = GetAllStageIds();
        int index = ids.IndexOf(currentStageId);
        if (index >= 0 && index < ids.Count - 1)
            return ids[index + 1];
        return -1; // No next stage
    }
    
    #endregion
    
    #region Stage Management
    
    /// <summary>
    /// Add a stage to the database.
    /// </summary>
    public void AddStage(StageData stage)
    {
        if (stage == null) return;
        
        if (!_initialized)
            Initialize();

        if (!stages.Contains(stage))
            stages.Add(stage);

        _stageMap[stage.stageNumber] = stage;
        UpdateEditorStats();
    }
    
    /// <summary>
    /// Remove a stage from the database.
    /// </summary>
    public bool RemoveStage(int stageId)
    {
        if (!_initialized)
            Initialize();
            
        if (_stageMap.TryGetValue(stageId, out StageData stage))
        {
            stages.Remove(stage);
            _stageMap.Remove(stageId);
            UpdateEditorStats();
            return true;
        }
        return false;
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validate all stages in the database.
    /// </summary>
    public List<string> ValidateAll()
    {
        var allIssues = new List<string>();
        
        // Check for empty database
        if (stages == null || stages.Count == 0)
        {
            allIssues.Add("Database is empty - no stages defined");
            return allIssues;
        }
        
        // Check for duplicate stage numbers
        var stageNumbers = new HashSet<int>();
        foreach (var stage in stages)
        {
            if (stage == null)
            {
                allIssues.Add("Null stage reference in database");
                continue;
            }
            
            if (stageNumbers.Contains(stage.stageNumber))
            {
                allIssues.Add($"Duplicate stage number: {stage.stageNumber} ({stage.stageName})");
            }
            stageNumbers.Add(stage.stageNumber);
            
            // Validate individual stage
            var stageIssues = stage.Validate();
            foreach (var issue in stageIssues)
            {
                allIssues.Add($"Stage {stage.stageNumber} ({stage.stageName}): {issue}");
            }
        }
        
        // Check for stage number gaps
        if (stageNumbers.Count > 0)
        {
            int min = stageNumbers.Min();
            int max = stageNumbers.Max();
            for (int i = min; i <= max; i++)
            {
                if (!stageNumbers.Contains(i))
                {
                    allIssues.Add($"Gap in stage numbers: Stage {i} is missing");
                }
            }
        }
        
        _validationErrors = allIssues;
        return allIssues;
    }
    
    #endregion
    
    #region Editor Support
    
    private void OnValidate()
    {
        UpdateEditorStats();
    }
    
    private void UpdateEditorStats()
    {
        _stageCount = stages?.Count ?? 0;
        _totalWaveCount = stages?.Sum(s => s?.waveConfigurations?.Count ?? 0) ?? 0;
    }
    
    #endregion
}