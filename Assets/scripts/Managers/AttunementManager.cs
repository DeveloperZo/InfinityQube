using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Manages attunement definitions and provides query API for gameplay systems.
/// Attunements modify cube properties, not collision matrix.
/// 
/// Supports loading from AttunementDatabase (ScriptableObject) or hardcoded fallback.
/// 
/// Usage:
/// - AttunementManager.Instance.HasAttunement(MarkerMode.Matrix, AttunementEffect.ExpandedArea)
/// - AttunementManager.Instance.GetEffectValue(MarkerMode.Matrix, AttunementEffect.ExpandedArea)
/// </summary>
public class AttunementManager : MonoBehaviour, IManagerDebugInterface
{
    #region Singleton
    
    private static AttunementManager _instance;
    public static AttunementManager Instance => _instance;
    
    public static bool IsInitialized => _instance != null;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Database Configuration")]
    [Tooltip("Attunement database to load from. If null, uses hardcoded defaults.")]
    [SerializeField] private AttunementDatabase attunementDatabase;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Attunement Definitions
    
    private Dictionary<string, AttunementDefinition> _attunementDefinitions;
    private bool _loadedFromDatabase = false;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// All defined attunements.
    /// </summary>
    public IReadOnlyDictionary<string, AttunementDefinition> Definitions => _attunementDefinitions;
    
    /// <summary>
    /// Whether definitions were loaded from database (vs hardcoded).
    /// </summary>
    public bool LoadedFromDatabase => _loadedFromDatabase;
    
    /// <summary>
    /// Reference to the database (if assigned).
    /// </summary>
    public AttunementDatabase Database => attunementDatabase;
    
    #endregion
    
    #region IManagerDebugInterface
    
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }
    
    public string GetDebugStatus()
    {
        if (!SaveManager.IsInitialized) return "SaveManager not ready";
        
        string matrix = GetEquippedAttunementName(MarkerMode.Matrix);
        string recursion = GetEquippedAttunementName(MarkerMode.Recursion);
        string infinity = GetEquippedAttunementName(MarkerMode.Infinity);
        
        return $"Matrix: {matrix} | Recursion: {recursion} | Infinity: {infinity}";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        var data = new Dictionary<string, object>
        {
            ["Source"] = _loadedFromDatabase ? "Database" : "Hardcoded",
            ["Total Definitions"] = _attunementDefinitions?.Count ?? 0,
            ["Matrix Equipped"] = GetEquippedAttunementName(MarkerMode.Matrix),
            ["Recursion Equipped"] = GetEquippedAttunementName(MarkerMode.Recursion),
            ["Infinity Equipped"] = GetEquippedAttunementName(MarkerMode.Infinity),
        };
        
        if (SaveManager.IsInitialized)
        {
            data["Unlocked Count"] = SaveManager.Instance.Progression?.unlockedAttunements?.Count ?? 0;
        }
        
        return data;
    }
    
    public void ResetToDefaults()
    {
        DebugLog("ResetToDefaults - reinitializing definitions");
        InitializeDefinitions();
    }
    
    public void LoadConfiguration(string configName)
    {
        DebugLog($"LoadConfiguration '{configName}' - no action (uses SaveManager)");
    }
    
    public void SaveConfiguration(string configName)
    {
        DebugLog($"SaveConfiguration '{configName}' - no action (uses SaveManager)");
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugLog("Duplicate AttunementManager detected, destroying");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeDefinitions();
        string source = _loadedFromDatabase ? "database" : "hardcoded defaults";
        DebugLog($"AttunementManager initialized with {_attunementDefinitions.Count} definitions from {source}");
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeDefinitions()
    {
        // Try to load from database first
        if (attunementDatabase != null)
        {
            LoadFromDatabase();
            return;
        }
        
        // Fall back to hardcoded definitions
        DebugLog("No database assigned, using hardcoded defaults");
        InitializeHardcodedDefinitions();
    }
    
    /// <summary>
    /// Load definitions from the AttunementDatabase ScriptableObject.
    /// </summary>
    private void LoadFromDatabase()
    {
        attunementDatabase.Initialize();
        _attunementDefinitions = attunementDatabase.ToDefinitions();
        _loadedFromDatabase = true;
        
        // Validate
        var issues = attunementDatabase.ValidateAll();
        if (issues.Count > 0)
        {
            foreach (var issue in issues)
            {
                Debug.LogWarning($"[AttunementManager] Database validation: {issue}");
            }
        }
    }
    
    /// <summary>
    /// Initialize with hardcoded definitions (fallback).
    /// </summary>
    private void InitializeHardcodedDefinitions()
    {
        _loadedFromDatabase = false;
        _attunementDefinitions = new Dictionary<string, AttunementDefinition>
        {
            // Matrix Attunements (Expansion Theme)
            ["matrix_expanded"] = new AttunementDefinition
            {
                id = "matrix_expanded",
                displayName = "Expanded Expansion",
                description = "+1 area dimensions (2x2 → 3x3)",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.ExpandedArea,
                effectValue = 1f, // +1 to area size
                unlockCost = 150
            },
            ["matrix_concentrated"] = new AttunementDefinition
            {
                id = "matrix_concentrated",
                displayName = "Concentrated Expansion",
                description = "+1 trigger use (Matrix markers can be triggered twice)",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.ConcentratedCharge,
                effectValue = 1f, // +1 use
                unlockCost = 150
            },
            ["matrix_phaseable"] = new AttunementDefinition
            {
                id = "matrix_phaseable",
                displayName = "Phaseable Expansion",
                description = "Matrix vs Matrix also paints wave cube face",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.PhaseablePaint,
                effectValue = 1f, // Boolean flag
                unlockCost = 200
            },
            
            // Recursion Attunements (Concentration Theme)
            ["recursion_concentrated"] = new AttunementDefinition
            {
                id = "recursion_concentrated",
                displayName = "Concentrated Concentration",
                description = "+2 charges (3 → 5)",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.ConcentratedCharges,
                effectValue = 2f, // +2 charges
                unlockCost = 150
            },
            ["recursion_expanded"] = new AttunementDefinition
            {
                id = "recursion_expanded",
                displayName = "Expanded Concentration",
                description = "+1 tile to pattern",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.ExpandedPattern,
                effectValue = 1f, // +1 tile
                unlockCost = 150
            },
            ["recursion_phaseable"] = new AttunementDefinition
            {
                id = "recursion_phaseable",
                displayName = "Phaseable Concentration",
                description = "Recursion vs Recursion also paints wave cube face",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.PhaseablePaint,
                effectValue = 1f, // Boolean flag
                unlockCost = 200
            },
            
            // Infinity Attunements (Phaseability Theme)
            ["infinity_potent_matrix"] = new AttunementDefinition
            {
                id = "infinity_potent_matrix",
                displayName = "Potent Matrix Paint",
                description = "+1 charge on Matrix painted faces",
                markerMode = MarkerMode.Infinity,
                effect = AttunementEffect.PotentMatrixPaint,
                effectValue = 1f, // +1 charge
                unlockCost = 200
            },
            ["infinity_potent_recursion"] = new AttunementDefinition
            {
                id = "infinity_potent_recursion",
                displayName = "Potent Recursion Paint",
                description = "+1 charge on Recursion painted faces",
                markerMode = MarkerMode.Infinity,
                effect = AttunementEffect.PotentRecursionPaint,
                effectValue = 1f, // +1 charge
                unlockCost = 200
            },
            ["infinity_untethered"] = new AttunementDefinition
            {
                id = "infinity_untethered",
                displayName = "Untethered",
                description = "vs Unit = destroy + continue (no wave join)",
                markerMode = MarkerMode.Infinity,
                effect = AttunementEffect.Untethered,
                effectValue = 1f, // Boolean flag
                unlockCost = 250
            }
        };
    }
    
    #endregion
    
    #region Query API
    
    /// <summary>
    /// Check if a specific attunement effect is active for a marker type.
    /// This is the main query method for collision handlers.
    /// </summary>
    public bool HasAttunement(MarkerMode mode, AttunementEffect effect)
    {
        if (!SaveManager.IsInitialized) return false;
        
        string equippedId = SaveManager.Instance.GetEquippedAttunement(mode);
        if (string.IsNullOrEmpty(equippedId)) return false;
        
        if (_attunementDefinitions.TryGetValue(equippedId, out var def))
        {
            return def.effect == effect;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the effect value for an equipped attunement.
    /// Returns 0 if no matching attunement is equipped.
    /// </summary>
    public float GetEffectValue(MarkerMode mode, AttunementEffect effect)
    {
        if (!SaveManager.IsInitialized) return 0f;
        
        string equippedId = SaveManager.Instance.GetEquippedAttunement(mode);
        if (string.IsNullOrEmpty(equippedId)) return 0f;
        
        if (_attunementDefinitions.TryGetValue(equippedId, out var def))
        {
            if (def.effect == effect)
            {
                return def.effectValue;
            }
        }
        
        return 0f;
    }
    
    /// <summary>
    /// Get the currently equipped attunement definition for a marker type.
    /// Returns null if none equipped.
    /// </summary>
    public AttunementDefinition GetEquippedAttunement(MarkerMode mode)
    {
        if (!SaveManager.IsInitialized) return null;
        
        string equippedId = SaveManager.Instance.GetEquippedAttunement(mode);
        if (string.IsNullOrEmpty(equippedId)) return null;
        
        _attunementDefinitions.TryGetValue(equippedId, out var def);
        return def;
    }
    
    /// <summary>
    /// Get the display name of the equipped attunement, or "None" if not equipped.
    /// </summary>
    public string GetEquippedAttunementName(MarkerMode mode)
    {
        var def = GetEquippedAttunement(mode);
        return def?.displayName ?? "None";
    }
    
    /// <summary>
    /// Get all attunement definitions for a marker type.
    /// </summary>
    public List<AttunementDefinition> GetAttunmentsForMarker(MarkerMode mode)
    {
        var result = new List<AttunementDefinition>();
        foreach (var def in _attunementDefinitions.Values)
        {
            if (def.markerMode == mode)
            {
                result.Add(def);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get an attunement definition by ID.
    /// </summary>
    public AttunementDefinition GetDefinition(string attunementId)
    {
        if (string.IsNullOrEmpty(attunementId)) return null;
        _attunementDefinitions.TryGetValue(attunementId, out var def);
        return def;
    }
    
    /// <summary>
    /// Check if an attunement is unlocked.
    /// </summary>
    public bool IsUnlocked(string attunementId)
    {
        if (!SaveManager.IsInitialized) return false;
        return SaveManager.Instance.Progression.IsAttunementUnlocked(attunementId);
    }
    
    #endregion
    
    #region Convenience Methods for Collision Handlers
    
    /// <summary>
    /// Get Matrix area size (base 2, +1 if Expanded Expansion equipped).
    /// </summary>
    public int GetMatrixAreaSize()
    {
        int baseSize = 2;
        if (HasAttunement(MarkerMode.Matrix, AttunementEffect.ExpandedArea))
        {
            return baseSize + (int)GetEffectValue(MarkerMode.Matrix, AttunementEffect.ExpandedArea);
        }
        return baseSize;
    }
    
    /// <summary>
    /// Get Matrix marker trigger uses (base 1, +1 if Concentrated Expansion equipped).
    /// With attunement: Matrix CubeMarkers can be triggered twice before disappearing.
    /// </summary>
    public int GetMatrixMarkerUses()
    {
        int baseUses = 1;
        if (HasAttunement(MarkerMode.Matrix, AttunementEffect.ConcentratedCharge))
        {
            return baseUses + (int)GetEffectValue(MarkerMode.Matrix, AttunementEffect.ConcentratedCharge);
        }
        return baseUses;
    }
    
    /// <summary>
    /// Legacy alias for GetMatrixMarkerUses (for backward compatibility).
    /// </summary>
    public int GetMatrixChargesPerTile() => GetMatrixMarkerUses();
    
    /// <summary>
    /// Check if Matrix+Matrix collision should paint wave cube face.
    /// </summary>
    public bool ShouldMatrixMatrixPaintFace()
    {
        return HasAttunement(MarkerMode.Matrix, AttunementEffect.PhaseablePaint);
    }
    
    /// <summary>
    /// Get Recursion charges (base 3, +2 if Concentrated Concentration equipped).
    /// </summary>
    public int GetRecursionCharges()
    {
        int baseCharges = 3;
        if (HasAttunement(MarkerMode.Recursion, AttunementEffect.ConcentratedCharges))
        {
            return baseCharges + (int)GetEffectValue(MarkerMode.Recursion, AttunementEffect.ConcentratedCharges);
        }
        return baseCharges;
    }
    
    /// <summary>
    /// Get Recursion pattern tile count (base 1, +1 if Expanded Concentration equipped).
    /// </summary>
    public int GetRecursionPatternTiles()
    {
        int baseTiles = 1;
        if (HasAttunement(MarkerMode.Recursion, AttunementEffect.ExpandedPattern))
        {
            return baseTiles + (int)GetEffectValue(MarkerMode.Recursion, AttunementEffect.ExpandedPattern);
        }
        return baseTiles;
    }
    
    /// <summary>
    /// Check if Recursion+Recursion collision should paint wave cube face.
    /// </summary>
    public bool ShouldRecursionRecursionPaintFace()
    {
        return HasAttunement(MarkerMode.Recursion, AttunementEffect.PhaseablePaint);
    }
    
    /// <summary>
    /// Get bonus charges for Matrix face paints (0 or +1 if Potent Matrix Paint equipped).
    /// </summary>
    public int GetMatrixPaintBonusCharges()
    {
        if (HasAttunement(MarkerMode.Infinity, AttunementEffect.PotentMatrixPaint))
        {
            return (int)GetEffectValue(MarkerMode.Infinity, AttunementEffect.PotentMatrixPaint);
        }
        return 0;
    }
    
    /// <summary>
    /// Get bonus charges for Recursion face paints (0 or +1 if Potent Recursion Paint equipped).
    /// </summary>
    public int GetRecursionPaintBonusCharges()
    {
        if (HasAttunement(MarkerMode.Infinity, AttunementEffect.PotentRecursionPaint))
        {
            return (int)GetEffectValue(MarkerMode.Infinity, AttunementEffect.PotentRecursionPaint);
        }
        return 0;
    }
    
    /// <summary>
    /// Check if Infinity should skip wave join vs Unit (Untethered attunement).
    /// </summary>
    public bool ShouldInfinitySkipWaveJoin()
    {
        return HasAttunement(MarkerMode.Infinity, AttunementEffect.Untethered);
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AttunementManager] {message}");
        }
    }
    
    #endregion
}

/// <summary>
/// Attunement effect types matching the design document.
/// </summary>
public enum AttunementEffect
{
    None,
    
    // Matrix attunements
    ExpandedArea,          // +1 area dimensions
    ConcentratedCharge,    // +1 charge per tile
    
    // Recursion attunements
    ConcentratedCharges,   // +2 charges
    ExpandedPattern,       // +1 tile to pattern
    
    // Shared
    PhaseablePaint,        // Same-type collision paints wave cube face
    
    // Infinity attunements
    PotentMatrixPaint,     // +1 charge on Matrix paints
    PotentRecursionPaint,  // +1 charge on Recursion paints
    Untethered             // vs Unit = destroy + continue
}

/// <summary>
/// Definition of an attunement with all metadata.
/// </summary>
[System.Serializable]
public class AttunementDefinition
{
    public string id;
    public string displayName;
    public string description;
    public MarkerMode markerMode;
    public AttunementEffect effect;
    public float effectValue;
    public int unlockCost;
}

