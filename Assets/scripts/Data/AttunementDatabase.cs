using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Central database for all attunement definitions.
/// Create via: Assets > Create > Infinity Qube > Attunement Database
/// </summary>
[CreateAssetMenu(fileName = "AttunementDatabase", menuName = "Infinity Qube/Attunement Database")]
public class AttunementDatabase : ScriptableObject
{
    #region Inspector Fields
    
    [Header("Attunement Collection")]
    [Tooltip("All attunements available in the game")]
    [SerializeField] private List<AttunementData> attunements = new List<AttunementData>();
    
    [Header("Database Info (Read-Only)")]
    [SerializeField] private int _totalCount;
    [SerializeField] private int _matrixCount;
    [SerializeField] private int _recursionCount;
    [SerializeField] private int _infinityCount;
    
    #endregion
    
    #region Runtime State
    
    private Dictionary<string, AttunementData> _attunementMap;
    private Dictionary<MarkerMode, List<AttunementData>> _byMarkerMode;
    private bool _initialized = false;
    
    #endregion
    
    #region Properties
    
    /// <summary>Total number of attunements.</summary>
    public int Count => attunements?.Count ?? 0;
    
    /// <summary>All attunements (read-only).</summary>
    public IReadOnlyList<AttunementData> AllAttunements => attunements;
    
    /// <summary>Whether database is initialized for runtime use.</summary>
    public bool IsInitialized => _initialized;
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initialize the database for runtime use.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        
        _attunementMap = new Dictionary<string, AttunementData>();
        _byMarkerMode = new Dictionary<MarkerMode, List<AttunementData>>
        {
            [MarkerMode.Matrix] = new List<AttunementData>(),
            [MarkerMode.Recursion] = new List<AttunementData>(),
            [MarkerMode.Infinity] = new List<AttunementData>()
        };
        
        foreach (var attunement in attunements)
        {
            if (attunement == null) continue;
            
            // Map by ID
            if (_attunementMap.ContainsKey(attunement.attunementId))
            {
                Debug.LogWarning($"[AttunementDB] Duplicate ID: {attunement.attunementId}");
                continue;
            }
            _attunementMap[attunement.attunementId] = attunement;
            
            // Map by marker mode
            if (_byMarkerMode.ContainsKey(attunement.markerMode))
            {
                _byMarkerMode[attunement.markerMode].Add(attunement);
            }
        }
        
        _initialized = true;
        Debug.Log($"[AttunementDB] Initialized: {_attunementMap.Count} attunements");
    }
    
    /// <summary>
    /// Force re-initialization.
    /// </summary>
    public void Reinitialize()
    {
        _initialized = false;
        Initialize();
    }
    
    #endregion
    
    #region Query Methods
    
    /// <summary>
    /// Get attunement by ID.
    /// </summary>
    public AttunementData GetById(string id)
    {
        if (!_initialized) Initialize();
        return _attunementMap.TryGetValue(id, out var data) ? data : null;
    }
    
    /// <summary>
    /// Get all attunements for a marker mode.
    /// </summary>
    public List<AttunementData> GetByMarkerMode(MarkerMode mode)
    {
        if (!_initialized) Initialize();
        return _byMarkerMode.TryGetValue(mode, out var list) ? new List<AttunementData>(list) : new List<AttunementData>();
    }
    
    /// <summary>
    /// Get all attunements of a specific theme.
    /// </summary>
    public List<AttunementData> GetByTheme(AttunementTheme theme)
    {
        if (!_initialized) Initialize();
        return attunements.Where(a => a != null && a.theme == theme).ToList();
    }
    
    /// <summary>
    /// Get all attunements with a specific effect.
    /// </summary>
    public AttunementData GetByEffect(MarkerMode mode, AttunementEffect effect)
    {
        if (!_initialized) Initialize();
        
        if (_byMarkerMode.TryGetValue(mode, out var list))
        {
            return list.FirstOrDefault(a => a.effect == effect);
        }
        return null;
    }
    
    /// <summary>
    /// Check if an attunement ID exists.
    /// </summary>
    public bool HasAttunement(string id)
    {
        if (!_initialized) Initialize();
        return _attunementMap.ContainsKey(id);
    }
    
    /// <summary>
    /// Get all attunement IDs.
    /// </summary>
    public List<string> GetAllIds()
    {
        if (!_initialized) Initialize();
        return new List<string>(_attunementMap.Keys);
    }
    
    #endregion
    
    #region Conversion
    
    /// <summary>
    /// Convert all attunements to runtime definitions dictionary.
    /// Used by AttunementManager for backward compatibility.
    /// </summary>
    public Dictionary<string, AttunementDefinition> ToDefinitions()
    {
        if (!_initialized) Initialize();
        
        var result = new Dictionary<string, AttunementDefinition>();
        foreach (var kvp in _attunementMap)
        {
            result[kvp.Key] = kvp.Value.ToDefinition();
        }
        return result;
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Validate all attunements in the database.
    /// </summary>
    public List<string> ValidateAll()
    {
        var issues = new List<string>();
        
        if (attunements == null || attunements.Count == 0)
        {
            issues.Add("Database is empty");
            return issues;
        }
        
        var ids = new HashSet<string>();
        
        foreach (var attunement in attunements)
        {
            if (attunement == null)
            {
                issues.Add("Null attunement reference");
                continue;
            }
            
            // Check for duplicates
            if (ids.Contains(attunement.attunementId))
            {
                issues.Add($"Duplicate ID: {attunement.attunementId}");
            }
            ids.Add(attunement.attunementId);
            
            // Validate individual attunement
            if (!attunement.IsValid(out string error))
            {
                issues.Add($"{attunement.attunementId}: {error}");
            }
        }
        
        // Check expected counts
        int matrixCount = attunements.Count(a => a != null && a.markerMode == MarkerMode.Matrix);
        int recursionCount = attunements.Count(a => a != null && a.markerMode == MarkerMode.Recursion);
        int infinityCount = attunements.Count(a => a != null && a.markerMode == MarkerMode.Infinity);
        
        if (matrixCount != 3)
            issues.Add($"Expected 3 Matrix attunements, found {matrixCount}");
        if (recursionCount != 3)
            issues.Add($"Expected 3 Recursion attunements, found {recursionCount}");
        if (infinityCount != 3)
            issues.Add($"Expected 3 Infinity attunements, found {infinityCount}");
        
        return issues;
    }
    
    #endregion
    
    #region Editor Support
    
    private void OnValidate()
    {
        UpdateEditorStats();
    }
    
    private void UpdateEditorStats()
    {
        _totalCount = attunements?.Count ?? 0;
        _matrixCount = attunements?.Count(a => a != null && a.markerMode == MarkerMode.Matrix) ?? 0;
        _recursionCount = attunements?.Count(a => a != null && a.markerMode == MarkerMode.Recursion) ?? 0;
        _infinityCount = attunements?.Count(a => a != null && a.markerMode == MarkerMode.Infinity) ?? 0;
    }
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Create default attunement database with all 9 attunements.
    /// Call this from editor to populate a new database.
    /// </summary>
    public void PopulateDefaults()
    {
        #if UNITY_EDITOR
        attunements.Clear();
        
        // Create Matrix attunements
        attunements.Add(CreateAttunement("matrix_expanded", "Expanded Expansion", 
            "+1 area dimensions (2x2 → 3x3)", MarkerMode.Matrix, AttunementEffect.ExpandedArea, 
            AttunementTheme.Expansion, 1f, 150));
            
        attunements.Add(CreateAttunement("matrix_concentrated", "Concentrated Expansion", 
            "+1 trigger use (Matrix markers can be triggered twice)", MarkerMode.Matrix, AttunementEffect.ConcentratedCharge, 
            AttunementTheme.Concentration, 1f, 150));
            
        attunements.Add(CreateAttunement("matrix_phaseable", "Phaseable Expansion", 
            "Matrix vs Matrix also paints wave cube face", MarkerMode.Matrix, AttunementEffect.PhaseablePaint, 
            AttunementTheme.Phaseability, 1f, 200));
        
        // Create Recursion attunements
        attunements.Add(CreateAttunement("recursion_concentrated", "Concentrated Concentration", 
            "+2 charges (3 → 5)", MarkerMode.Recursion, AttunementEffect.ConcentratedCharges, 
            AttunementTheme.Concentration, 2f, 150));
            
        attunements.Add(CreateAttunement("recursion_expanded", "Expanded Concentration", 
            "+1 tile to pattern", MarkerMode.Recursion, AttunementEffect.ExpandedPattern, 
            AttunementTheme.Expansion, 1f, 150));
            
        attunements.Add(CreateAttunement("recursion_phaseable", "Phaseable Concentration", 
            "Recursion vs Recursion also paints wave cube face", MarkerMode.Recursion, AttunementEffect.PhaseablePaint, 
            AttunementTheme.Phaseability, 1f, 200));
        
        // Create Infinity attunements
        attunements.Add(CreateAttunement("infinity_potent_matrix", "Potent Matrix Paint", 
            "+1 charge on Matrix painted faces", MarkerMode.Infinity, AttunementEffect.PotentMatrixPaint, 
            AttunementTheme.Concentration, 1f, 200));
            
        attunements.Add(CreateAttunement("infinity_potent_recursion", "Potent Recursion Paint", 
            "+1 charge on Recursion painted faces", MarkerMode.Infinity, AttunementEffect.PotentRecursionPaint, 
            AttunementTheme.Concentration, 1f, 200));
            
        attunements.Add(CreateAttunement("infinity_untethered", "Untethered", 
            "vs Unit = destroy + continue (no wave join)", MarkerMode.Infinity, AttunementEffect.Untethered, 
            AttunementTheme.Phaseability, 1f, 250));
        
        UpdateEditorStats();
        Debug.Log($"[AttunementDB] Populated with {attunements.Count} default attunements");
        #endif
    }
    
    #if UNITY_EDITOR
    private AttunementData CreateAttunement(string id, string name, string desc, 
        MarkerMode mode, AttunementEffect effect, AttunementTheme theme, float value, int cost)
    {
        var data = ScriptableObject.CreateInstance<AttunementData>();
        data.attunementId = id;
        data.displayName = name;
        data.description = desc;
        data.markerMode = mode;
        data.effect = effect;
        data.theme = theme;
        data.effectValue = value;
        data.unlockCost = cost;
        data.name = id;
        
        // Save as asset
        string path = $"Assets/Data/Attunements/{id}.asset";
        System.IO.Directory.CreateDirectory("Assets/Data/Attunements");
        UnityEditor.AssetDatabase.CreateAsset(data, path);
        
        return data;
    }
    #endif
    
    #endregion
}

