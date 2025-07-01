using UnityEngine;

/// <summary>
/// Defines animation trigger points throughout the mode system for visual polish effects.
/// These trigger points provide hooks for future animations like mode transitions, 
/// marker placement effects, and UI flourishes.
/// </summary>
public enum AnimationTriggerPoint
{
    /// <summary>
    /// Triggered when switching between marker modes (Light/Prime/Heavy)
    /// Context: Mode transition from one state to another
    /// </summary>
    ModeSwitch,
    
    /// <summary>
    /// Triggered when placing any type of marker on the grid
    /// Context: Marker placement action with specific marker type
    /// </summary>
    MarkerPlace,
    
    /// <summary>
    /// Triggered when activating/triggering any placed marker
    /// Context: Marker trigger action with effects on targets
    /// </summary>
    MarkerTrigger,
    
    /// <summary>
    /// Triggered when UI elements update (charges, cooldowns, displays)
    /// Context: UI state changes for visual feedback
    /// </summary>
    UIUpdate,
    
    /// <summary>
    /// Triggered when an action fails (placement/trigger denied)
    /// Context: Error feedback for failed actions
    /// </summary>
    ActionFailed,
    
    /// <summary>
    /// Triggered when an action succeeds 
    /// Context: Success feedback for completed actions
    /// </summary>
    ActionSuccess,
    
    /// <summary>
    /// Triggered when cube markers are placed or triggered
    /// Context: Special cube marker interactions
    /// </summary>
    CubeMarkerAction,
    
    /// <summary>
    /// Triggered when resource charges regenerate over time
    /// Context: Resource recovery visual feedback
    /// </summary>
    ResourceRegeneration
}
