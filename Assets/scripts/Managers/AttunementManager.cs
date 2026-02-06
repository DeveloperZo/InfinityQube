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
    /// Design: One active per marker type. Power/Resource/Utility pattern.
    /// </summary>
    private void InitializeHardcodedDefinitions()
    {
        _loadedFromDatabase = false;
        _attunementDefinitions = new Dictionary<string, AttunementDefinition>
        {
            // ===================
            // MATRIX ATTUNEMENTS
            // ===================
            
            ["matrix_mastery"] = new AttunementDefinition
            {
                id = "matrix_mastery",
                displayName = "Matrix Mastery",
                description = "All Matrix areas are 3x3 instead of 2x2",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.MatrixMastery,
                effectValue = 3f, // Area size
                unlockCost = 150
            },
            ["matrix_abundance"] = new AttunementDefinition
            {
                id = "matrix_abundance",
                displayName = "Matrix Abundance",
                description = "+2 Matrix markers per stage",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.MatrixAbundance,
                effectValue = 2f, // +2 markers
                unlockCost = 150
            },
            ["infinity_forge"] = new AttunementDefinition
            {
                id = "infinity_forge",
                displayName = "Infinity Forge",
                description = "Matrix + ∞ collision creates area marker (∞ cubes are opportunities)",
                markerMode = MarkerMode.Matrix,
                effect = AttunementEffect.InfinityForge,
                effectValue = 1f, // Boolean flag
                unlockCost = 200
            },
            
            // ======================
            // RECURSION ATTUNEMENTS
            // ======================
            
            ["recursion_clone"] = new AttunementDefinition
            {
                id = "recursion_clone",
                displayName = "Recursion Clone",
                description = "R+R becomes clone+swap instead of capture+swap (multiply cubes)",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.RecursionClone,
                effectValue = 1f, // Boolean flag
                unlockCost = 150
            },
            ["recursion_abundance"] = new AttunementDefinition
            {
                id = "recursion_abundance",
                displayName = "Recursion Abundance",
                description = "+2 Recursion markers per stage",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.RecursionAbundance,
                effectValue = 2f, // +2 markers
                unlockCost = 150
            },
            ["infinity_gateway"] = new AttunementDefinition
            {
                id = "infinity_gateway",
                displayName = "Infinity Gateway",
                description = "Recursion + ∞ collision creates swap marker (∞ walls become opportunities)",
                markerMode = MarkerMode.Recursion,
                effect = AttunementEffect.InfinityGateway,
                effectValue = 1f, // Boolean flag
                unlockCost = 200
            }
            
            // ======================
            // INFINITY ATTUNEMENTS
            // ======================
            // TBD - Pending playtesting of Infinity marker mechanics
            // Will follow same Power/Resource/Utility pattern
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
    
    // ===================
    // MATRIX QUERIES
    // ===================
    
    /// <summary>
    /// Get Matrix area size (base 2, or 3 if Matrix Mastery equipped).
    /// </summary>
    public int GetMatrixAreaSize()
    {
        if (HasAttunement(MarkerMode.Matrix, AttunementEffect.MatrixMastery))
        {
            return 3; // 3x3 instead of 2x2
        }
        return 2; // Default 2x2
    }
    
    /// <summary>
    /// Get bonus Matrix markers per stage (0 or +2 if Matrix Abundance equipped).
    /// </summary>
    public int GetBonusMatrixMarkers()
    {
        if (HasAttunement(MarkerMode.Matrix, AttunementEffect.MatrixAbundance))
        {
            return (int)GetEffectValue(MarkerMode.Matrix, AttunementEffect.MatrixAbundance);
        }
        return 0;
    }
    
    /// <summary>
    /// Check if Matrix can collide with ∞ cubes to create area marker (Infinity Forge).
    /// </summary>
    public bool CanMatrixCollideWithInfinity()
    {
        return HasAttunement(MarkerMode.Matrix, AttunementEffect.InfinityForge);
    }
    
    // ======================
    // RECURSION QUERIES
    // ======================
    
    /// <summary>
    /// Check if Recursion is in Clone mode (R+R becomes clone+swap instead of capture+swap).
    /// </summary>
    public bool IsRecursionCloneMode()
    {
        return HasAttunement(MarkerMode.Recursion, AttunementEffect.RecursionClone);
    }
    
    /// <summary>
    /// Get bonus Recursion markers per stage (0 or +2 if Recursion Abundance equipped).
    /// </summary>
    public int GetBonusRecursionMarkers()
    {
        if (HasAttunement(MarkerMode.Recursion, AttunementEffect.RecursionAbundance))
        {
            return (int)GetEffectValue(MarkerMode.Recursion, AttunementEffect.RecursionAbundance);
        }
        return 0;
    }
    
    /// <summary>
    /// Check if Recursion can collide with ∞ cubes to create swap marker (Infinity Gateway).
    /// </summary>
    public bool CanRecursionCollideWithInfinity()
    {
        return HasAttunement(MarkerMode.Recursion, AttunementEffect.InfinityGateway);
    }
    
    // ======================
    // INFINITY QUERIES (TBD)
    // ======================
    // Infinity attunement queries will be added after playtesting
    
    // ======================
    // LEGACY METHODS (kept for backward compatibility)
    // ======================
    
    /// <summary>
    /// Legacy: Get Matrix marker trigger uses.
    /// </summary>
    public int GetMatrixMarkerUses()
    {
        return 1; // Legacy default
    }
    
    /// <summary>
    /// Legacy alias for GetMatrixMarkerUses.
    /// </summary>
    public int GetMatrixChargesPerTile() => GetMatrixMarkerUses();
    
    /// <summary>
    /// Legacy: Check if Matrix+Matrix should paint face.
    /// </summary>
    public bool ShouldMatrixMatrixPaintFace()
    {
        return HasAttunement(MarkerMode.Matrix, AttunementEffect.PhaseablePaint);
    }
    
    /// <summary>
    /// Legacy: Get Recursion charges.
    /// </summary>
    public int GetRecursionCharges()
    {
        return 3; // Legacy default
    }
    
    /// <summary>
    /// Legacy: Get Recursion pattern tiles.
    /// </summary>
    public int GetRecursionPatternTiles()
    {
        return 1; // Legacy default
    }
    
    /// <summary>
    /// Legacy: Check if Recursion+Recursion should paint face.
    /// </summary>
    public bool ShouldRecursionRecursionPaintFace()
    {
        return HasAttunement(MarkerMode.Recursion, AttunementEffect.PhaseablePaint);
    }
    
    /// <summary>
    /// Legacy: Get Matrix paint bonus charges.
    /// </summary>
    public int GetMatrixPaintBonusCharges()
    {
        return 0; // Legacy default
    }
    
    /// <summary>
    /// Legacy: Get Recursion paint bonus charges.
    /// </summary>
    public int GetRecursionPaintBonusCharges()
    {
        return 0; // Legacy default
    }
    
    /// <summary>
    /// Legacy: Check if Infinity should skip wave join.
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
/// Design: One active per marker type, fundamental identity shifts (not stat tweaks).
/// </summary>
public enum AttunementEffect
{
    None,
    
    // Matrix attunements (designed)
    MatrixMastery,         // All Matrix areas are 3x3 instead of 2x2
    MatrixAbundance,       // +2 Matrix markers per stage
    InfinityForge,         // Matrix + ∞ collision creates area marker
    
    // Recursion attunements (designed)
    RecursionClone,        // R+R becomes clone+swap instead of capture+swap
    RecursionAbundance,    // +2 Recursion markers per stage
    InfinityGateway,       // Recursion + ∞ collision creates swap marker
    
    // Infinity attunements (TBD - pending playtesting)
    // Will follow same Power/Resource/Utility pattern
    
    // Legacy (kept for backward compatibility, may be removed)
    ExpandedArea,          // Old: +1 area dimensions
    ConcentratedCharge,    // Old: +1 charge per tile
    ConcentratedCharges,   // Old: +2 charges
    ExpandedPattern,       // Old: +1 tile to pattern
    PhaseablePaint,        // Old: Same-type collision paints wave cube face
    PotentMatrixPaint,     // Old: +1 charge on Matrix paints
    PotentRecursionPaint,  // Old: +1 charge on Recursion paints
    Untethered             // Old: vs Unit = destroy + continue
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

