using UnityEngine;
using static Enumerations;

/// <summary>
/// ScriptableObject defining a single attunement.
/// Create via: Assets > Create > Infinity Qube > Attunement Data
/// </summary>
[CreateAssetMenu(fileName = "New Attunement", menuName = "Infinity Qube/Attunement Data")]
public class AttunementData : ScriptableObject
{
    #region Identity
    
    [Header("Identity")]
    [Tooltip("Unique identifier for this attunement (e.g., 'matrix_expanded')")]
    public string attunementId;
    
    [Tooltip("Display name shown to player")]
    public string displayName;
    
    [Tooltip("Description of the attunement effect")]
    [TextArea(2, 4)]
    public string description;
    
    #endregion
    
    #region Classification
    
    [Header("Classification")]
    [Tooltip("Which marker type this attunement applies to")]
    public MarkerMode markerMode;
    
    [Tooltip("The effect type this attunement provides")]
    public AttunementEffect effect;
    
    [Tooltip("Theme/category for grouping (Expansion, Concentration, Phaseability)")]
    public AttunementTheme theme;
    
    #endregion
    
    #region Effect Values
    
    [Header("Effect Values")]
    [Tooltip("Primary numeric effect value (interpretation depends on effect type)")]
    public float effectValue = 1f;
    
    [Tooltip("Secondary effect value (for complex effects)")]
    public float secondaryValue = 0f;
    
    #endregion
    
    #region Economy
    
    [Header("Economy")]
    [Tooltip("Axiom Shards cost to unlock")]
    [Range(50, 500)] public int unlockCost = 150;
    
    [Tooltip("Minimum stage to unlock (0 = always available)")]
    [Range(0, 20)] public int requiredStage = 0;
    
    #endregion
    
    #region Visuals
    
    [Header("Visuals (Optional)")]
    [Tooltip("Icon displayed in UI")]
    public Sprite icon;
    
    [Tooltip("Color associated with this attunement")]
    public Color themeColor = Color.white;
    
    #endregion
    
    #region Validation
    
    private void OnValidate()
    {
        // Auto-generate ID from name if empty
        if (string.IsNullOrEmpty(attunementId) && !string.IsNullOrEmpty(displayName))
        {
            attunementId = displayName.ToLower().Replace(" ", "_");
        }
    }
    
    /// <summary>
    /// Validate this attunement data.
    /// </summary>
    public bool IsValid(out string error)
    {
        if (string.IsNullOrEmpty(attunementId))
        {
            error = "Attunement ID is empty";
            return false;
        }
        
        if (string.IsNullOrEmpty(displayName))
        {
            error = "Display name is empty";
            return false;
        }
        
        if (markerMode == MarkerMode.Unit)
        {
            error = "Unit markers do not support attunements";
            return false;
        }
        
        if (effect == AttunementEffect.None)
        {
            error = "Effect type is None";
            return false;
        }
        
        error = null;
        return true;
    }
    
    #endregion
    
    #region Conversion
    
    /// <summary>
    /// Convert to the runtime AttunementDefinition format.
    /// </summary>
    public AttunementDefinition ToDefinition()
    {
        return new AttunementDefinition
        {
            id = attunementId,
            displayName = displayName,
            description = description,
            markerMode = markerMode,
            effect = effect,
            effectValue = effectValue,
            unlockCost = unlockCost
        };
    }
    
    #endregion
}

/// <summary>
/// Thematic grouping for attunements.
/// </summary>
public enum AttunementTheme
{
    Expansion,      // Matrix theme - area/size increases
    Concentration,  // Recursion theme - charges/intensity
    Phaseability    // Infinity theme - face painting/phasing
}

