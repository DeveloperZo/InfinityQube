using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages state tracking for all marker types (Light, Heavy, Prime) with precise timing
/// and event-driven state transitions. Provides foundational architecture for marker
/// system communication and timing displays.
/// </summary>
public class MarkerStateManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Timing Configuration")]
    [SerializeField] private float lightMarkerCooldown = 2.0f;
    [SerializeField] private float heavyMarkerCooldown = 5.0f;
    [SerializeField] private float primeMarkerCooldown = 8.0f;
    
    [Header("Charge Settings")]
    [SerializeField] private int maxLightCharges = 3;
    [SerializeField] private int maxHeavyCharges = 2;
    [SerializeField] private int maxPrimeCharges = 1;
    #endregion

    #region Manager References
    private PlayerActionManager playerActionManager;
    private InputFeedbackManager inputFeedbackManager;
    private AnimationTriggerManager animationTriggerManager;
    #endregion

    #region Runtime State
    private Dictionary<MarkerType, MarkerStateData> markerStates;
    private float cachedTime;
    private bool isInitialized = false;
    #endregion

    #region Properties
    public static MarkerStateManager Instance { get; private set; }
    
    // State queries
    public MarkerState GetMarkerState(MarkerType markerType) => markerStates[markerType].currentState;
    public float GetCooldownProgress(MarkerType markerType) => markerStates[markerType].cooldownProgress;
    public int