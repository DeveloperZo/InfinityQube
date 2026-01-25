using UnityEngine;
using System.Collections.Generic;
using System;
using static Enumerations;


[System.Serializable]
public class UnitMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;
    public GridSegmentController segment; // Segment-aware: which segment this marker is on

    public UnitMarker(Vector2Int pos, float time, GridSegmentController seg = null)
    {
        position = pos;
        placementTime = time;
        segment = seg;
    }
}

[System.Serializable]
public class RecursionMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;
    public GridSegmentController segment; // Segment-aware: which segment this marker is on

    public RecursionMarker(Vector2Int pos, float time, GridSegmentController seg = null)
    {
        position = pos;
        placementTime = time;
        segment = seg;
    }
}



[System.Serializable]
public class MatrixMarker
{
    public Vector2Int centerPosition;
    public int size;
    public float placementTime;
    public List<GameObject> visualObjects = new List<GameObject>();
    public List<Vector2Int> affectedPositions = new List<Vector2Int>();
    public GridSegmentController segment; // Segment-aware: which segment this marker is on

    public MatrixMarker(Vector2Int center, int markerSize, float time, GridSegmentController seg = null)
    {
        centerPosition = center;
        size = markerSize;
        placementTime = time;
        segment = seg;
    }
}

[System.Serializable]
public class InfinityMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;
    public GridSegmentController segment; // Segment-aware: which segment this marker is on

    public InfinityMarker(Vector2Int pos, float time, GridSegmentController seg = null)
    {
        position = pos;
        placementTime = time;
        segment = seg;
    }
}

public class PlayerActionManager : MonoBehaviour, IManagerDebugInterface
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerActionUI actionUI;
    [SerializeField] private PlayerMarkerSystem markerSystem;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private InputFeedbackManager inputFeedbackManager;
    [SerializeField] private AnimationTriggerManager animationTriggerManager;

    [Header("Unit Marker Settings - Runtime State")]
    [SerializeField] public int maxUnitMarkers;
    [SerializeField] public int unitMarkersPlaced;
    [SerializeField] public int currentUnitMarkers;
    [SerializeField] private int currentUnitMarkerCharges;
    [SerializeField] private int unitMarkerRechargeProgress = 0; // Current progress toward next charge
    
    [Header("Unit Marker Settings - Configuration (Debug/Prototyping Only)")]
    [Tooltip("DEBUG ONLY: Max Unit marker charges. Production: Use StageData.stageGrants.maxUnitMarkerCharges")]
    [SerializeField] public int maxUnitMarkerCharges;
    [Tooltip("DEBUG ONLY: Unit marker recharge rate. Production: Use StageData.stageGrants.unitMarkerRechargeRate")]
    [SerializeField] public int unitMarkerRechargeRate = 3; // Moves per charge (replaces time-based cooldown)
    
    [Header("Unit Marker Visual")]
    [SerializeField] public Material unitMarkerMaterial;

    [Header("Recursion Marker Settings")]
    [SerializeField] public int maxRecursionMarkers;
    [SerializeField] public int recursionMarkersPlaced;
    [SerializeField] public int currentRecursionMarkers;
    [SerializeField] public int maxRecursionMarkerCharges;
    [SerializeField] private int currentRecursionMarkerCharges;
    [SerializeField] public Material recursionMarkerMaterial;

    [Header("Matrix Marker Settings")]
    [SerializeField] public int maxMatrixMarkers;
    [SerializeField] public int matrixMarkersPlaced;
    [SerializeField] public int currentMatrixMarkers;
    [SerializeField] public int maxMatrixMarkerCharges;
    [SerializeField] private int currentMatrixMarkerCharges;
    [SerializeField] public int matrixMarkerSize;
    [SerializeField] public int matrixMarkerOnGridLimit;
    [SerializeField] public Material matrixMarkerMaterial;

    [Header("Infinity Marker Settings")]
    [SerializeField] public int maxInfinityMarkers = 2;
    [SerializeField] public int infinityMarkersPlaced;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs;
    [SerializeField] public int currentInfinityMarkers;
    [SerializeField] public int maxInfinityMarkerCharges = 1;
    [SerializeField] private int currentInfinityMarkerCharges;
    [SerializeField] public Material infinityMarkerMaterial;

    [Header("Marker Economy")]
    [Tooltip("Enable per-stage/wave grant system instead of time-based regeneration for non-Unit markers")]
    [SerializeField] public bool useMarkerEconomy = true;
    
    [Header("Marker Economy - Inventory Caps")]
    [SerializeField] public int maxRecursionInventory = 8;
    [SerializeField] public int maxMatrixInventory = 5;
    [SerializeField] public int maxInfinityInventory = 3;

    [Header("Input Settings")]
    [SerializeField] private KeyCode unitMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode recursionMarkerKey = KeyCode.V;
    [SerializeField] private KeyCode matrixMarkerKey = KeyCode.G;
    [SerializeField] private KeyCode triggerCubeMarkerKey = KeyCode.Q;
    


    // Marker Mode System
    [Header("Marker Mode System")]
    [SerializeField] private MarkerMode currentMarkerMode = MarkerMode.Unit;

    // Statistics
    private int cubeMarkersTriggered;
    private int perfectTimingHits;
    private bool inputEnabled = false;

    public GridManager GridManager => gridManager;
    public PlayerManager PlayerManager => playerManager;
    public WaveManager WaveManager => waveManager;
    public PlayerMarkerSystem MarkerSystem => markerSystem;

    #region Unity Lifecycle

    private void Start()
    {
        
        InitializeReferences();
        InitializeCharges();
        InitializeMarkerMode();

        markerSystem = GetComponent<PlayerMarkerSystem>();
        if (markerSystem == null)
        {
            markerSystem = gameObject.AddComponent<PlayerMarkerSystem>();
        }
        markerSystem.Initialize(this);

        // Update UI with initial values
        UpdateUI();
    }

    private void Update()
    {
        if (inputEnabled)
        {
            HandleInput();
        }
        RegenerateCharges();
    }

    private void OnEnable()
    {
        // Subscribe to marker economy events
        GameEvents.OnStageStart += HandleStageStart;
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveStep += HandleWaveStep;
    }

    private void OnDisable()
    {
        // Unsubscribe from marker economy events
        GameEvents.OnStageStart -= HandleStageStart;
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveStep -= HandleWaveStep;
    }

    private void InitializeReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
        if (actionUI == null)
            actionUI = FindFirstObjectByType<PlayerActionUI>();
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();
        if (inputFeedbackManager == null)
            inputFeedbackManager = FindFirstObjectByType<InputFeedbackManager>();
        if (animationTriggerManager == null)
            animationTriggerManager = FindFirstObjectByType<AnimationTriggerManager>();
            
        ValidateAudioManager();
        ValidateInputFeedbackManager();
        ValidateAnimationTriggerManager();
    }

    private void ValidateAudioManager()
    {
        if (audioManager == null)
        {
            this.LogWarning("AudioManager not found! Audio events will not be triggered.", EnableDebugLogs);
        }
        else if (!audioManager.IsInitialized)
        {
            this.LogWarning("AudioManager found but not initialized. Audio events may not work correctly.", EnableDebugLogs);
        }
    }

    private void ValidateInputFeedbackManager()
    {
        if (inputFeedbackManager == null)
        {
            this.LogWarning("InputFeedbackManager not found! Input feedback hooks will not be triggered.", EnableDebugLogs);
        }
        else if (EnableDebugLogs)
        {
            this.Log($"InputFeedbackManager found with {inputFeedbackManager.GetRegisteredHookCount()} hooks registered.", EnableDebugLogs);
        }
    }

    private void ValidateAnimationTriggerManager()
    {
        if (animationTriggerManager == null)
        {
            this.LogWarning("AnimationTriggerManager not found! Animation triggers will not be fired.", EnableDebugLogs);
        }
        else if (EnableDebugLogs)
        {
            this.Log($"AnimationTriggerManager found with {animationTriggerManager.GetTotalReceiverCount()} receivers registered.", EnableDebugLogs);
        }
    }

    private void InitializeCharges()
    {
        // Unit markers are INFINITE with move-based regeneration
        // Use debug defaults only - will be overridden by StageData when stage loads
        int defaultMaxCharges = maxUnitMarkerCharges > 0 ? maxUnitMarkerCharges : 3;
        int defaultRechargeRate = unitMarkerRechargeRate > 0 ? unitMarkerRechargeRate : 3;
        
        maxUnitMarkerCharges = defaultMaxCharges;
        currentUnitMarkerCharges = maxUnitMarkerCharges;
        unitMarkerRechargeRate = defaultRechargeRate;
        unitMarkerRechargeProgress = 0; // Start fresh
        
        // Non-Unit markers use inventory system when economy enabled
        currentRecursionMarkerCharges = maxRecursionMarkerCharges;
        currentMatrixMarkerCharges = maxMatrixMarkerCharges;
        currentInfinityMarkerCharges = maxInfinityMarkerCharges;
        inputEnabled = true;
        
        Debug.Log($"[MarkerEconomy] InitializeCharges (debug defaults): Unit={currentUnitMarkerCharges}/{maxUnitMarkerCharges} recharge={unitMarkerRechargeRate} moves");
    }

    private void InitializeMarkerMode()
    {
        currentMarkerMode = MarkerMode.Unit;
        
        if (EnableDebugLogs)
        {
            this.Log($"Marker mode initialized to: {currentMarkerMode}", EnableDebugLogs);
        }
    }

    #endregion

    #region Animation Trigger Integration

    /// <summary>
    /// Triggers animation for mode switching
    /// </summary>
    /// <param name="previousMode">The mode being switched from</param>
    /// <param name="newMode">The mode being switched to</param>
    /// <param name="playerPosition">Current player position</param>
    private void TriggerAnimationModeSwitch(MarkerMode previousMode, MarkerMode newMode, Vector2Int playerPosition)
    {
        if (animationTriggerManager != null)
        {
            Vector3 worldPos = GetWorldPositionForAudio(playerPosition);
            animationTriggerManager.TriggerModeSwitch(worldPos, previousMode, newMode, 1.0f);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger mode switch animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for marker placement
    /// </summary>
    /// <param name="markerMode">Type of marker that was placed</param>
    /// <param name="position">Grid position where marker was placed</param>
    /// <param name="wasReplacement">True if this replaced an existing marker</param>
    private void TriggerAnimationMarkerPlace(MarkerMode markerMode, Vector2Int position, bool wasReplacement)
    {
        if (animationTriggerManager != null)
        {
            Vector3 worldPos = GetWorldPositionForAudio(position);
            animationTriggerManager.TriggerMarkerPlace(worldPos, markerMode, wasReplacement, 1.0f);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger marker place animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for marker triggering
    /// </summary>
    /// <param name="markerMode">Type of marker that was triggered</param>
    /// <param name="position">Grid position of the triggered marker</param>
    /// <param name="targetCount">Number of targets affected by the trigger</param>
    private void TriggerAnimationMarkerTrigger(MarkerMode markerMode, Vector2Int position, int targetCount)
    {
        if (animationTriggerManager != null)
        {
            Vector3 worldPos = GetWorldPositionForAudio(position);
            animationTriggerManager.TriggerMarkerTrigger(worldPos, markerMode, targetCount, 1.0f);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger marker trigger animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for cube marker actions
    /// </summary>
    /// <param name="position">Position of the cube marker</param>
    /// <param name="effect">Description of the effect caused</param>
    private void TriggerAnimationCubeMarkerAction(Vector2Int position, string effect)
    {
        if (animationTriggerManager != null)
        {
            Vector3 worldPos = GetWorldPositionForAudio(position);
            var context = AnimationTriggerContext.Create(worldPos, 1.0f);
            context.additionalData = effect;
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.CubeMarkerAction, context);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger cube marker animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for action failures
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="failureReason">Human-readable reason for the failure</param>
    private void TriggerAnimationActionFailed(Vector3 playerPosition, string failureReason)
    {
        if (animationTriggerManager != null)
        {
            var context = AnimationTriggerContext.Create(playerPosition, 0.7f);
            context.additionalData = failureReason;
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.ActionFailed, context);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger action failed animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for action successes
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="successMessage">Human-readable success message</param>
    private void TriggerAnimationActionSuccess(Vector3 playerPosition, string successMessage)
    {
        if (animationTriggerManager != null)
        {
            var context = AnimationTriggerContext.Create(playerPosition, 1.0f);
            context.additionalData = successMessage;
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.ActionSuccess, context);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger action success animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers animation for resource regeneration
    /// </summary>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="resourceType">Type of resource that regenerated</param>
    private void TriggerAnimationResourceRegeneration(Vector3 playerPosition, string resourceType)
    {
        if (animationTriggerManager != null)
        {
            var context = AnimationTriggerContext.Create(playerPosition, 0.8f);
            context.additionalData = resourceType;
            context.duration = 1.5f; // Longer duration for regeneration glow
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.ResourceRegeneration, context);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger resource regeneration animation - AnimationTriggerManager not available", EnableDebugLogs);
        }
    }

    #endregion

    #region Input Handling

    public void SetInput(bool condition)
    {
        playerManager.isDead = condition;
    }

    private void HandleInput()
    {
        if (playerManager == null || !playerManager.IsAlive()) return;

        HandleModeSwitchingInput();
        HandleUnifiedPlaceInput();
        HandleTriggerInputs();
        HandleCubeMarkerInputs();
        HandleDebugInput(); // Add debug input handling
    }

    /// <summary>
    /// Handles mode switching input using number keys 1-3
    /// </summary>
    private void HandleModeSwitchingInput()
    {
MarkerMode targetMode = currentMarkerMode;
GameAudioEvent audioEvent = GameAudioEvent.ModeSwitchedToUnit;
        bool modeSwitchRequested = false;

        // Check for number key presses (1-4 for marker types)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            targetMode = MarkerMode.Unit;
            audioEvent = GameAudioEvent.ModeSwitchedToUnit;
            modeSwitchRequested = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            targetMode = MarkerMode.Matrix;
            audioEvent = GameAudioEvent.ModeSwitchedToMatrix;
            modeSwitchRequested = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            targetMode = MarkerMode.Recursion;
            audioEvent = GameAudioEvent.ModeSwitchedToRecursion;
            modeSwitchRequested = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            targetMode = MarkerMode.Infinity;
            audioEvent = GameAudioEvent.ModeSwitchedToRecursion; // TODO: Add infinity audio event
            modeSwitchRequested = true;
        }

        // Only process mode switch if a key was pressed and mode is different
        if (modeSwitchRequested && targetMode != currentMarkerMode)
        {
MarkerMode previousMode = currentMarkerMode;
            if (SetMode(targetMode))
            {
                // Trigger audio feedback for successful mode switch
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(audioEvent, playerWorldPos, 1.0f);

                // Trigger input feedback hooks for mode switch
                TriggerInputFeedbackModeSwitch(previousMode, targetMode, playerManager.currentTilePosition);

                // Trigger animation for mode switch
                TriggerAnimationModeSwitch(previousMode, targetMode, playerManager.currentTilePosition);

                if (EnableDebugLogs)
                {
                    this.Log($"Mode switched to {targetMode} via number key input", EnableDebugLogs);
                }
            }
        }
        else if (modeSwitchRequested && targetMode == currentMarkerMode)
        {
            // User pressed key for current mode - provide feedback but don't switch
            if (EnableDebugLogs)
            {
                this.Log($"Already in {targetMode} mode - no switch needed", EnableDebugLogs);
            }
        }
    }

    /// <summary>
    /// Handles unified marker placement input using F key based on current mode
    /// </summary>
    private void HandleUnifiedPlaceInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;
            MarkerMode currentMode = GetCurrentMode();
            bool actionSuccessful = false;
            
            // SEGMENT CHECK: Verify player is on the same segment as the wave BEFORE any marker action
            if (!markerSystem.IsPlayerOnWaveSegment())
            {
                string segmentError = markerSystem.GetSegmentMismatchReason() ?? "Move to the wave's segment to place markers.";
                ShowActionErrorFeedback(segmentError);
                TriggerInputFeedbackActionFailed("place", segmentError, playerPos, 0.7f);
                
                if (EnableDebugLogs)
                {
                    this.LogWarning($"Marker placement blocked: {segmentError}", EnableDebugLogs);
                }
                return; // Early exit - don't process marker placement
            }

            switch (currentMode)
            {
                case MarkerMode.Unit:
                    if (markerSystem.HasUnitMarkerAt(playerPos))
                    {
                        markerSystem.RemoveUnitMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(MarkerMode.Unit, playerPos, true);
                    }
                    else if (CanPlaceUnitMarker())
                    {
                        // Actually attempt placement and check if it succeeded
                        bool placed = markerSystem.PlaceUnitMarker(playerPos);
                        if (placed)
                        {
                            actionSuccessful = true;
                            
                            // Trigger feedback for new marker placement
                            TriggerInputFeedbackMarkerPlace(MarkerMode.Unit, playerPos, false);
                            
                            // Trigger animation for marker placement
                            TriggerAnimationMarkerPlace(MarkerMode.Unit, playerPos, false);
                            
                            // Show success feedback for successful placement
                            ShowActionSuccessFeedback("Unit marker placed successfully!");
                        }
                        else
                        {
                            // Placement failed (e.g., line divider restriction)
                            ShowActionErrorFeedback("Cannot place marker in danger zone!");
                            TriggerInputFeedbackActionFailed("place", "Cannot place above line divider", playerPos, 0.7f);
                        }
                    }
                    else
                    {
                        // Show error feedback for failed placement
                        string errorMessage = GetModeActionErrorMessage(currentMode, "place");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("place", errorMessage, playerPos, 0.7f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Unit marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                case MarkerMode.Recursion:
                    if (markerSystem.HasRecursionMarkerAt(playerPos))
                    {
                        markerSystem.RemoveRecursionMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(MarkerMode.Recursion, playerPos, true);
                    }
                    else if (CanPlaceRecursionMarker())
                    {
                        // Actually attempt placement and check if it succeeded
                        bool placed = markerSystem.PlaceRecursionMarker(playerPos);
                        if (placed)
                        {
                            actionSuccessful = true;
                            
                            // Trigger feedback for new marker placement
                            TriggerInputFeedbackMarkerPlace(MarkerMode.Recursion, playerPos, false);
                            
                            // Trigger animation for marker placement
                            TriggerAnimationMarkerPlace(MarkerMode.Recursion, playerPos, false);
                            
                            // Show success feedback for successful placement
                            ShowActionSuccessFeedback("Recursion marker placed successfully!");
                        }
                        else
                        {
                            // Placement failed (e.g., line divider restriction)
                            ShowActionErrorFeedback("Cannot place marker in danger zone!");
                            TriggerInputFeedbackActionFailed("place", "Cannot place above line divider", playerPos, 0.7f);
                        }
                    }
                    else
                    {
                        // Show error feedback for failed placement
                        string errorMessage = GetModeActionErrorMessage(currentMode, "place");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("place", errorMessage, playerPos, 0.7f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Recursion marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                case MarkerMode.Matrix:
                    if (markerSystem.HasMatrixMarkerAt(playerPos))
                    {
                        markerSystem.RemoveMatrixMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(MarkerMode.Matrix, playerPos, true);
                    }
                    else if (CanPlaceMatrixMarker())
                    {
                        // Actually attempt placement and check if it succeeded
                        bool placed = markerSystem.PlaceMatrixMarker(playerPos, matrixMarkerSize);
                        if (placed)
                        {
                            actionSuccessful = true;
                            
                            // Trigger feedback for new marker placement
                            TriggerInputFeedbackMarkerPlace(MarkerMode.Matrix, playerPos, false);
                            
                            // Trigger animation for marker placement
                            TriggerAnimationMarkerPlace(MarkerMode.Matrix, playerPos, false);
                            
                            // Show success feedback for successful placement
                            ShowActionSuccessFeedback("Matrix marker placed successfully!");
                        }
                        else
                        {
                            // Placement failed (e.g., line divider restriction)
                            ShowActionErrorFeedback("Cannot place marker in danger zone!");
                            TriggerInputFeedbackActionFailed("place", "Cannot place above line divider", playerPos, 0.7f);
                        }
                    }
                    else
                    {
                        // Show error feedback for failed placement
                        string errorMessage = GetModeActionErrorMessage(currentMode, "place");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("place", errorMessage, playerPos, 0.7f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Matrix marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                case MarkerMode.Infinity:
                    if (markerSystem.HasInfinityMarkerAt(playerPos))
                    {
                        markerSystem.RemoveInfinityMarkerAt(playerPos);
                        actionSuccessful = true;
                        TriggerInputFeedbackMarkerPlace(MarkerMode.Infinity, playerPos, true);
                    }
                    else if (CanPlaceInfinityMarker())
                    {
                        // Actually attempt placement and check if it succeeded
                        bool placed = markerSystem.PlaceInfinityMarker(playerPos);
                        if (placed)
                        {
                            actionSuccessful = true;
                            
                            TriggerInputFeedbackMarkerPlace(MarkerMode.Infinity, playerPos, false);
                            TriggerAnimationMarkerPlace(MarkerMode.Infinity, playerPos, false);
                            ShowActionSuccessFeedback("Infinity marker placed successfully!");
                        }
                        else
                        {
                            // Placement failed (e.g., line divider restriction)
                            ShowActionErrorFeedback("Cannot place marker in danger zone!");
                            TriggerInputFeedbackActionFailed("place", "Cannot place above line divider", playerPos, 0.7f);
                        }
                    }
                    else
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "place");
                        ShowActionErrorFeedback(errorMessage);
                        TriggerInputFeedbackActionFailed("place", errorMessage, playerPos, 0.7f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Infinity marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                default:
                    if (EnableDebugLogs)
                    {
                        this.LogWarning($"Unhandled marker mode in unified place input: {GetCurrentMode()}", EnableDebugLogs);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Handles unified marker triggering input using R key based on current mode
    /// </summary>
    private void HandleTriggerInputs()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            MarkerMode currentMode = GetCurrentMode();
            bool actionSuccessful = false;
            
            // SEGMENT CHECK: Verify player is on the same segment as the wave for trigger actions
            if (!markerSystem.IsPlayerOnWaveSegment())
            {
                string segmentError = markerSystem.GetSegmentMismatchReason() ?? "Move to the wave's segment to trigger markers.";
                ShowActionErrorFeedback(segmentError);
                TriggerInputFeedbackActionFailed("trigger", segmentError, GetCurrentPlayerPosition(), 0.6f);
                
                if (EnableDebugLogs)
                {
                    this.LogWarning($"Marker trigger blocked: {segmentError}", EnableDebugLogs);
                }
                return; // Early exit - don't process marker triggering
            }

            switch (currentMode)
            {
                case MarkerMode.Unit:
                    actionSuccessful = markerSystem.TriggerNextUnitMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Unit marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        // Note: We use player position as approximation since triggered marker position may vary
                        TriggerInputFeedbackMarkerTrigger(MarkerMode.Unit, GetCurrentPlayerPosition(), 1);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(MarkerMode.Unit, GetCurrentPlayerPosition(), 1);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Unit marker triggered successfully!");
                    }
                    break;

                case MarkerMode.Recursion:
                    actionSuccessful = markerSystem.TriggerNextRecursionMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Recursion marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        TriggerInputFeedbackMarkerTrigger(MarkerMode.Recursion, GetCurrentPlayerPosition(), 1);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(MarkerMode.Recursion, GetCurrentPlayerPosition(), 1);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Recursion marker triggered successfully!");
                    }
                    break;

                case MarkerMode.Matrix:
                    actionSuccessful = markerSystem.TriggerNextMatrixMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Matrix marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        // Matrix markers affect multiple targets, so we estimate area
                        int estimatedTargets = matrixMarkerSize * matrixMarkerSize;
                        TriggerInputFeedbackMarkerTrigger(MarkerMode.Matrix, GetCurrentPlayerPosition(), estimatedTargets);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(MarkerMode.Matrix, GetCurrentPlayerPosition(), estimatedTargets);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Matrix marker triggered successfully!");
                    }
                    break;

                case MarkerMode.Infinity:
                    actionSuccessful = markerSystem.TriggerNextInfinityMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Infinity marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        TriggerInputFeedbackMarkerTrigger(MarkerMode.Infinity, GetCurrentPlayerPosition(), 1);
                        TriggerAnimationMarkerTrigger(MarkerMode.Infinity, GetCurrentPlayerPosition(), 1);
                        ShowActionSuccessFeedback("Infinity marker triggered successfully!");
                    }
                    break;

                default:
                    if (EnableDebugLogs)
                    {
                        this.LogWarning($"Unhandled marker mode in unified trigger input: {GetCurrentMode()}", EnableDebugLogs);
                    }
                    break;
            }
        }
    }

    private void HandleCubeMarkerInputs()
    {
        if (Input.GetKeyDown(triggerCubeMarkerKey))
        {
            // Get current player position for feedback
            Vector2Int playerPos = GetCurrentPlayerPosition();
            bool wasTriggered = markerSystem.TriggerNextCubeMarker();
            
            if (wasTriggered)
            {
                // Trigger feedback for cube marker trigger
                TriggerInputFeedbackCubeMarkerTrigger("Cube", playerPos, "Standard cube marker effect");
                
                // Trigger animation for cube marker action
                TriggerAnimationCubeMarkerAction(playerPos, "Standard cube marker effect");
            }
        }
    }

    /// <summary>
    /// Handles debug input for development and testing purposes
    /// </summary>
    private void HandleDebugInput()
    {
        // H key: Hello World debug message
        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHelloWorldMessage();
        }
    }

    /// <summary>
    /// Displays a Hello World debug message following project standards
    /// </summary>
    private void ShowHelloWorldMessage()
    {
        // Use established debug logging format: [ManagerName] method: message
        if (EnableDebugLogs)
        {
            this.Log("Hello World from InfinityQube! Debug message system working.", EnableDebugLogs);
        }
        
        // Also show success feedback using existing UI system
        ShowActionSuccessFeedback("Hello World! Debug system active.");
        
        // Trigger audio event for feedback
        Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.ActionSuccess, playerWorldPos, 0.5f);
        
        // Trigger animation for feedback
        TriggerAnimationActionSuccess(playerWorldPos, "Hello World debug message");
    }

    /// <summary>
    /// Helper method to get current player position
    /// </summary>
    /// <returns>Current player grid position</returns>
    private Vector2Int GetCurrentPlayerPosition()
    {
        return playerManager?.currentTilePosition ?? Vector2Int.zero;
    }

    #endregion

    #region Mode-Aware Error Feedback

    /// <summary>
    /// Gets a mode-specific error message when an action cannot be performed
    /// </summary>
    /// <param name="mode">The marker mode being used</param>
    /// <param name="actionType">The type of action being attempted ("place" or "trigger")</param>
    /// <returns>A descriptive error message explaining why the action failed</returns>
    public string GetModeActionErrorMessage(MarkerMode mode, string actionType)
    {
        if (actionType == "place")
        {
            switch (mode)
            {
                case MarkerMode.Unit:
                    if (currentUnitMarkerCharges <= 0)
                        return "No Unit marker charges available. Wait for charges to regenerate.";
                    if (currentUnitMarkers >= maxUnitMarkers)
                        return "Maximum Unit markers already placed on grid.";
                    break;

                case MarkerMode.Recursion:
                    if (currentRecursionMarkerCharges <= 0)
                        return "No Recursion marker charges available.";
                    if (currentRecursionMarkers >= maxRecursionMarkers)
                        return "Maximum Recursion markers already placed on grid.";
                    break;

                case MarkerMode.Matrix:
                    if (currentMatrixMarkerCharges <= 0)
                        return "No Matrix marker charges available.";
                    int effectiveMaxMatrix = maxMatrixMarkers > 0 ? maxMatrixMarkers : 2; // Default to 2 if not set
                    if (currentMatrixMarkers >= effectiveMaxMatrix)
                        return "Maximum Matrix markers already placed on grid.";
                    break;

                case MarkerMode.Infinity:
                    if (currentInfinityMarkerCharges <= 0)
                        return "No Infinity marker charges available.";
                    if (currentInfinityMarkers >= maxInfinityMarkers)
                        return "Maximum Infinity markers already placed on grid.";
                    break;
            }
        }
        else if (actionType == "trigger")
        {
            switch (mode)
            {
                case MarkerMode.Unit:
                    if (markerSystem.UnitMarkers.Count == 0)
                        return "No Unit markers available to trigger.";
                    break;

                case MarkerMode.Recursion:
                    if (markerSystem.RecursionMarkers.Count == 0)
                        return "No Recursion markers available to trigger.";
                    break;

                case MarkerMode.Matrix:
                    if (markerSystem.MatrixMarkers.Count == 0)
                        return "No matrix markers available to trigger.";
                    break;

                case MarkerMode.Infinity:
                    if (markerSystem.InfinityMarkers.Count == 0)
                        return "No infinity markers available to trigger.";
                    break;
            }
        }

        return "Action cannot be performed."; // Generic fallback
    }

    /// <summary>
    /// Shows error feedback to the player when an action fails
    /// </summary>
    /// <param name="errorMessage">The error message to display</param>
    private void ShowActionErrorFeedback(string errorMessage)
    {
        // Show UI feedback using the existing PlayerActionUI pattern
        if (actionUI != null)
        {
            actionUI.ShowActionFeedback(errorMessage, true);
        }

        // Trigger error feedback audio event
        Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.ActionError, playerWorldPos, 0.7f);

        // Trigger animation for action failed
        TriggerAnimationActionFailed(playerWorldPos, errorMessage);

        if (EnableDebugLogs)
        {
            this.Log($"Error feedback shown: {errorMessage}", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Shows success feedback to the player when an action succeeds
    /// </summary>
    /// <param name="successMessage">The success message to display</param>
    private void ShowActionSuccessFeedback(string successMessage)
    {
        // Show UI feedback using the existing PlayerActionUI pattern
        if (actionUI != null)
        {
            actionUI.ShowActionFeedback(successMessage, false);
        }

        // Trigger success feedback audio event
        Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.ActionSuccess, playerWorldPos, 0.8f);

        // Trigger animation for action success
        TriggerAnimationActionSuccess(playerWorldPos, successMessage);

        if (EnableDebugLogs)
        {
            this.Log($"Success feedback shown: {successMessage}", EnableDebugLogs);
        }
    }

    #endregion

    #region Audio Event Integration

    /// <summary>
    /// Triggers an audio event if AudioManager is available
    /// </summary>
    /// <param name="eventType">The type of audio event to trigger</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    private void TriggerAudioEvent(GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        if (audioManager != null && audioManager.IsInitialized)
        {
            audioManager.TriggerAudioEvent(eventType, worldPosition, intensity);
            
            if (EnableDebugLogs)
            {
                this.Log($"Triggered audio event: {eventType} at position {worldPosition} with intensity {intensity:F2}", EnableDebugLogs);
            }
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger audio event {eventType} - AudioManager not available or initialized", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Converts grid position to world position for audio positioning
    /// </summary>
    /// <param name="gridPosition">Grid position to convert</param>
    /// <returns>World position for audio</returns>
    private Vector3 GetWorldPositionForAudio(Vector2Int gridPosition)
    {
        if (gridManager != null)
        {
            return gridManager.GridToWorldPosition(gridPosition.x, gridPosition.y);
        }
        
        // Fallback: approximate world position
        return new Vector3(gridPosition.x, 0f, gridPosition.y);
    }

    #endregion

    #region Input Feedback Integration

    /// <summary>
    /// Triggers input feedback hooks for mode switching
    /// </summary>
    /// <param name="previousMode">The mode being switched from</param>
    /// <param name="newMode">The mode being switched to</param>
    /// <param name="playerPosition">Current player position</param>
    private void TriggerInputFeedbackModeSwitch(MarkerMode previousMode, MarkerMode newMode, Vector2Int playerPosition)
    {
        if (inputFeedbackManager != null)
        {
            inputFeedbackManager.TriggerModeSwitch(previousMode, newMode, playerPosition);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger mode switch feedback - InputFeedbackManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers input feedback hooks for marker placement
    /// </summary>
    /// <param name="markerMode">Type of marker that was placed</param>
    /// <param name="position">Grid position where marker was placed</param>
    /// <param name="wasReplacement">True if this replaced an existing marker</param>
    private void TriggerInputFeedbackMarkerPlace(MarkerMode markerMode, Vector2Int position, bool wasReplacement)
    {
        if (inputFeedbackManager != null)
        {
            inputFeedbackManager.TriggerMarkerPlace(markerMode, position, wasReplacement);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger marker place feedback - InputFeedbackManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers input feedback hooks for marker triggering
    /// </summary>
    /// <param name="markerMode">Type of marker that was triggered</param>
    /// <param name="position">Grid position of the triggered marker</param>
    /// <param name="targetCount">Number of targets affected by the trigger</param>
    private void TriggerInputFeedbackMarkerTrigger(MarkerMode markerMode, Vector2Int position, int targetCount)
    {
        if (inputFeedbackManager != null)
        {
            inputFeedbackManager.TriggerMarkerTrigger(markerMode, position, targetCount);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger marker trigger feedback - InputFeedbackManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers input feedback hooks for cube marker actions
    /// </summary>
    /// <param name="cubeMarkerType">Type of cube marker triggered</param>
    /// <param name="position">Position of the cube marker</param>
    /// <param name="effect">Description of the effect caused</param>
    private void TriggerInputFeedbackCubeMarkerTrigger(string cubeMarkerType, Vector2Int position, string effect)
    {
        if (inputFeedbackManager != null)
        {
            inputFeedbackManager.TriggerCubeMarkerTrigger(cubeMarkerType, position, effect);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger cube marker feedback - InputFeedbackManager not available", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Triggers input feedback hooks for action failures
    /// </summary>
    /// <param name="actionType">Type of action that failed</param>
    /// <param name="failureReason">Human-readable reason for the failure</param>
    /// <param name="playerPosition">Current player position</param>
    /// <param name="intensity">Failure severity for proportional feedback</param>
    private void TriggerInputFeedbackActionFailed(string actionType, string failureReason, Vector2Int playerPosition, float intensity = 0.5f)
    {
        if (inputFeedbackManager != null)
        {
            inputFeedbackManager.TriggerActionFailed(actionType, failureReason, playerPosition, intensity);
        }
        else if (EnableDebugLogs)
        {
            this.LogWarning($"Cannot trigger action failed feedback - InputFeedbackManager not available", EnableDebugLogs);
        }
    }

    #endregion

    #region Marker Mode Management

    /// <summary>
    /// Gets the current active marker mode
    /// </summary>
    /// <returns>The currently active marker mode</returns>
    public MarkerMode GetCurrentMode()
    {
        return currentMarkerMode;
    }

    /// <summary>
    /// Sets the current marker mode with validation
    /// </summary>
    /// <param name="mode">The desired marker mode</param>
    /// <returns>True if mode was successfully changed, false otherwise</returns>
    public bool SetMode(MarkerMode mode)
    {
        if (!CanSwitchMode(mode))
        {
            if (EnableDebugLogs)
            {
                this.LogWarning($"Cannot switch to mode {mode} - validation failed", EnableDebugLogs);
            }
            return false;
        }

        MarkerMode previousMode = currentMarkerMode;
        currentMarkerMode = mode;

        // Update UI to reflect mode change
        if (actionUI != null)
        {
            // Force UI update to show new mode indicator
            UpdateUI();
        }

        // Update tile hover color to match new mode
        UpdateTileHoverColor();

        if (EnableDebugLogs)
        {
            this.Log($"Mode switched from {previousMode} to {currentMarkerMode}", EnableDebugLogs);
        }

        return true;
    }

    /// <summary>
    /// Updates the player's current tile hover color to match the selected marker mode.
    /// </summary>
    private void UpdateTileHoverColor()
    {
        if (playerManager == null || gridManager == null) return;

        Tile currentTile = gridManager.GetTileAt(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y);
        if (currentTile != null)
        {
            Color hoverColor = GetMarkerModeColor(currentMarkerMode);
            currentTile.SetHoverColor(hoverColor);
        }
    }

    /// <summary>
    /// Gets the highlight color for a specific marker mode.
    /// </summary>
    public static Color GetMarkerModeColor(MarkerMode mode)
    {
        return mode switch
        {
            MarkerMode.Unit => new Color(0.5f, 0.6f, 0.7f, 0.6f),      // Blue-gray
            MarkerMode.Matrix => new Color(0.3f, 0.7f, 1f, 0.6f),       // Vibrant light blue
            MarkerMode.Recursion => new Color(0.8f, 0.5f, 0.2f, 0.6f), // Deep amber brown
            MarkerMode.Infinity => new Color(0.15f, 0.15f, 0.18f, 0.6f), // Deep black/charcoal
            _ => new Color(0.5f, 0.6f, 0.7f, 0.6f)                     // Default to Unit
        };
    }

    /// <summary>
    /// Validates if switching to the specified mode is allowed
    /// </summary>
    /// <param name="mode">The mode to validate</param>
    /// <returns>True if mode switch is valid, false otherwise</returns>
    private bool CanSwitchMode(MarkerMode mode)
    {
        // Basic validation: ensure mode is defined
        if (!System.Enum.IsDefined(typeof(MarkerMode), mode))
        {
            return false;
        }

        // Prevent switching to modes if markers are not available
        // NOTE: Unit markers are ALWAYS available (infinite with move-based regeneration)
        switch (mode)
        {
            case MarkerMode.Unit:
                // Unit markers are always available - they use move-based regeneration
                break;
                
            case MarkerMode.Recursion:
                if (maxRecursionInventory <= 0)
                {
                    ShowActionErrorFeedback("Recursion markers are not available in this wave.");
                    return false;
                }
                break;
                
            case MarkerMode.Matrix:
                if (maxMatrixInventory <= 0)
                {
                    ShowActionErrorFeedback("Matrix markers are not available in this wave.");
                    return false;
                }
                break;
                
            case MarkerMode.Infinity:
                if (maxInfinityInventory <= 0)
                {
                    ShowActionErrorFeedback("Infinity markers are not available in this wave.");
                    return false;
                }
                break;
        }

        return true;
    }

    #endregion

    #region Charge Management

    #region Marker Economy
    
    // Cached stage data for grant application
    private StageData _currentStageData;
    
    /// <summary>
    /// Handle stage start - apply stage grants from StageData
    /// </summary>
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        _currentStageData = stageData;
        
        if (!useMarkerEconomy) return;
        
        ApplyStageGrants(stageData);
    }
    
    /// <summary>
    /// Handle wave start - apply wave grants from WaveData
    /// </summary>
    private void HandleWaveStart(int waveIndex, WaveData waveData)
    {
        if (!useMarkerEconomy) return;
        
        // Wave grants are ALWAYS applied (combinatorial with stage grants)
        // waveGrantsFromWaveData controls the SOURCE (WaveData vs inspector defaults)
        if (_currentStageData != null && _currentStageData.waveGrantsFromWaveData && waveData != null)
        {
            ApplyWaveGrants(waveData);
        }
        else
        {
            // Use inspector defaults for wave grants
            ApplyWaveGrantsDefault();
        }
    }
    
    /// <summary>
    /// Handle wave step - regenerate Unit marker charges based on wave moves
    /// Move-based regeneration: charges regenerate based on wave cadence, not real-time
    /// </summary>
    private void HandleWaveStep(int waveIndex, int stepNumber)
    {
        // Only regenerate if we're below max charges
        if (currentUnitMarkerCharges >= maxUnitMarkerCharges)
        {
            unitMarkerRechargeProgress = 0; // Reset progress when full
            return;
        }
        
        // Increment progress toward next charge
        unitMarkerRechargeProgress++;
        
        // Check if we've accumulated enough moves for a charge
        if (unitMarkerRechargeProgress >= unitMarkerRechargeRate)
        {
            unitMarkerRechargeProgress = 0; // Reset progress
            currentUnitMarkerCharges = Mathf.Min(currentUnitMarkerCharges + 1, maxUnitMarkerCharges);
            
            // Trigger audio event for resource regeneration
            Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
            TriggerAudioEvent(GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
            
            // Trigger animation for resource regeneration
            TriggerAnimationResourceRegeneration(playerWorldPos, "Unit marker charge");
            
            this.Log($"Unit marker charge regenerated (move {stepNumber}). Charges: {currentUnitMarkerCharges}/{maxUnitMarkerCharges}", EnableDebugLogs);
            
            UpdateUI();
        }
    }
    
    /// <summary>
    /// Apply stage grants from StageData - sets inventory to stage values
    /// This is the PRODUCTION path - StageData is the source of truth for marker configuration
    /// </summary>
    public void ApplyStageGrants(StageData stageData)
    {
        if (stageData?.stageGrants != null)
        {
            var grants = stageData.stageGrants;
            
            // Unit markers are INFINITE with move-based regeneration
            // Configuration comes from StageData.stageGrants (production) or inspector defaults (debug)
            int effectiveMaxCharges = grants.maxUnitMarkerCharges > 0 ? grants.maxUnitMarkerCharges : (maxUnitMarkerCharges > 0 ? maxUnitMarkerCharges : 3);
            int effectiveRechargeRate = grants.unitMarkerRechargeRate > 0 ? grants.unitMarkerRechargeRate : (unitMarkerRechargeRate > 0 ? unitMarkerRechargeRate : 3);
            
            maxUnitMarkerCharges = effectiveMaxCharges;
            currentUnitMarkerCharges = maxUnitMarkerCharges; // Always start full
            maxUnitMarkers = grants.unitMaxOnGrid > 0 ? grants.unitMaxOnGrid : 5; // Max on grid
            unitMarkerRechargeRate = effectiveRechargeRate;
            unitMarkerRechargeProgress = 0; // Start fresh
            
            // Non-Unit markers use inventory system
            maxRecursionInventory = Mathf.Max(maxRecursionInventory, grants.recursionCharges);
            maxMatrixInventory = Mathf.Max(maxMatrixInventory, grants.matrixCharges);
            maxInfinityInventory = Mathf.Max(maxInfinityInventory, grants.infinityCharges);
            
            // Stage grants SET the inventory (not add), capped at max
            currentRecursionMarkerCharges = Mathf.Min(grants.recursionCharges, maxRecursionInventory);
            currentMatrixMarkerCharges = Mathf.Min(grants.matrixCharges, maxMatrixInventory);
            currentInfinityMarkerCharges = Mathf.Min(grants.infinityCharges, maxInfinityInventory);
            
            // Apply max on grid limits for non-Unit
            maxRecursionMarkers = grants.recursionMaxOnGrid > 0 ? grants.recursionMaxOnGrid : 3;
            maxMatrixMarkers = grants.matrixMaxOnGrid > 0 ? grants.matrixMaxOnGrid : 2;
            maxInfinityMarkers = grants.infinityMaxOnGrid > 0 ? grants.infinityMaxOnGrid : 1;
            
            Debug.Log($"[MarkerEconomy] Stage grants applied: Unit=∞({currentUnitMarkerCharges}/{maxUnitMarkerCharges}, recharge:{unitMarkerRechargeRate} moves, grid:{maxUnitMarkers}), Rec={grants.recursionCharges}/{maxRecursionInventory}, Mat={grants.matrixCharges}/{maxMatrixInventory}, Inf={grants.infinityCharges}/{maxInfinityInventory}");
        }
        else
        {
            // Fallback to inspector defaults
            ApplyStageGrantsDefault();
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Apply wave grants from WaveData - adds to current inventory
    /// </summary>
    public void ApplyWaveGrants(WaveData waveData)
    {
        if (waveData == null) return;
        
        // Track overrides for logging
        string overrideLog = "";
        
        // Unit markers are INFINITE - wave grants don't affect charges
        // But wave CAN override max-on-grid, recharge rate, and max charges
        if (waveData.overrideUnitMaxOnGrid > 0)
        {
            maxUnitMarkers = waveData.overrideUnitMaxOnGrid;
            overrideLog += $" UnitGrid:{maxUnitMarkers}";
        }
        if (waveData.overrideUnitMarkerRechargeRate > 0)
        {
            unitMarkerRechargeRate = waveData.overrideUnitMarkerRechargeRate;
            overrideLog += $" UnitRecharge:{unitMarkerRechargeRate}";
        }
        if (waveData.overrideMaxUnitMarkerCharges > 0)
        {
            maxUnitMarkerCharges = waveData.overrideMaxUnitMarkerCharges;
            currentUnitMarkerCharges = Mathf.Min(currentUnitMarkerCharges, maxUnitMarkerCharges); // Cap current to new max
            overrideLog += $" UnitMaxCharges:{maxUnitMarkerCharges}";
        }
        
        // Non-Unit markers use inventory system
        if (waveData.grantsAddToInventory)
        {
            // Wave grants ADD to current inventory, capped at max
            currentRecursionMarkerCharges = Mathf.Min(currentRecursionMarkerCharges + waveData.grantRecursionCharges, maxRecursionInventory);
            currentMatrixMarkerCharges = Mathf.Min(currentMatrixMarkerCharges + waveData.grantMatrixCharges, maxMatrixInventory);
            currentInfinityMarkerCharges = Mathf.Min(currentInfinityMarkerCharges + waveData.grantInfinityCharges, maxInfinityInventory);
        }
        else
        {
            // Wave grants SET inventory (override)
            currentRecursionMarkerCharges = Mathf.Min(waveData.grantRecursionCharges, maxRecursionInventory);
            currentMatrixMarkerCharges = Mathf.Min(waveData.grantMatrixCharges, maxMatrixInventory);
            currentInfinityMarkerCharges = Mathf.Min(waveData.grantInfinityCharges, maxInfinityInventory);
        }
        
        // Apply per-wave max overrides if specified
        if (waveData.overrideRecursionMaxOnGrid > 0)
        {
            maxRecursionMarkers = waveData.overrideRecursionMaxOnGrid;
            overrideLog += $" RecGrid:{maxRecursionMarkers}";
        }
        if (waveData.overrideMatrixMaxOnGrid > 0)
        {
            maxMatrixMarkers = waveData.overrideMatrixMaxOnGrid;
            overrideLog += $" MatGrid:{maxMatrixMarkers}";
        }
        if (waveData.overrideInfinityMaxOnGrid > 0)
        {
            maxInfinityMarkers = waveData.overrideInfinityMaxOnGrid;
            overrideLog += $" InfGrid:{maxInfinityMarkers}";
        }
        
        string overrideStr = string.IsNullOrEmpty(overrideLog) ? "" : $" [Overrides:{overrideLog}]";
        Debug.Log($"[MarkerEconomy] Wave grants applied: Unit=∞(grid:{maxUnitMarkers}), Rec=+{waveData.grantRecursionCharges}, Mat=+{waveData.grantMatrixCharges}, Inf=+{waveData.grantInfinityCharges}{overrideStr}");
        
        UpdateUI();
    }
    
    /// <summary>
    /// Fallback method when StageData is not available
    /// Sets minimal defaults for Unit markers only (non-Unit markers get 0 charges)
    /// Production code should always use ApplyStageGrants(StageData) instead
    /// </summary>
    public void ApplyStageGrantsDefault()
    {
        Debug.LogWarning("[MarkerEconomy] StageData not available - using minimal defaults. Unit markers only. Non-Unit markers will have 0 charges until StageData is provided.");
        
        // Unit markers are INFINITE with move-based regeneration
        // Use reasonable defaults for Unit markers only
        int defaultMaxCharges = maxUnitMarkerCharges > 0 ? maxUnitMarkerCharges : 3;
        int defaultRechargeRate = unitMarkerRechargeRate > 0 ? unitMarkerRechargeRate : 3;
        
        maxUnitMarkerCharges = defaultMaxCharges;
        maxUnitMarkers = 5; // Max on grid
        currentUnitMarkerCharges = maxUnitMarkerCharges; // Start full
        unitMarkerRechargeRate = defaultRechargeRate;
        unitMarkerRechargeProgress = 0; // Start fresh
        
        // Non-Unit markers: Set to 0 since no StageData is available
        // Ensure inventory caps are set (for UI display purposes)
        if (maxRecursionInventory <= 0) maxRecursionInventory = 8;
        if (maxMatrixInventory <= 0) maxMatrixInventory = 5;
        if (maxInfinityInventory <= 0) maxInfinityInventory = 3;
        
        // No stage grants without StageData - set to 0
        currentRecursionMarkerCharges = 0;
        currentMatrixMarkerCharges = 0;
        currentInfinityMarkerCharges = 0;
        
        // Ensure max on grid for non-Unit if not set
        if (maxRecursionMarkers <= 0) maxRecursionMarkers = 3;
        if (maxMatrixMarkers <= 0) maxMatrixMarkers = 2;
        if (maxInfinityMarkers <= 0) maxInfinityMarkers = 1;
        
        Debug.Log($"[MarkerEconomy] Stage grants applied (minimal defaults - no StageData): Unit=∞({currentUnitMarkerCharges}/{maxUnitMarkerCharges}, recharge:{unitMarkerRechargeRate} moves), Rec=0/{maxRecursionInventory}, Mat=0/{maxMatrixInventory}, Inf=0/{maxInfinityInventory}");
        UpdateUI();
    }
    
    /// <summary>
    /// <summary>
    /// Fallback method when WaveData is not available
    /// Does not apply any wave grants (non-Unit markers remain unchanged)
    /// Production code should always use ApplyWaveGrants(WaveData) instead
    /// </summary>
    public void ApplyWaveGrantsDefault()
    {
        Debug.LogWarning("[MarkerEconomy] WaveData not available - no wave grants applied. Non-Unit marker charges remain unchanged.");
        
        // Unit markers are INFINITE - no wave grants needed (recharge rate override handled in ApplyWaveGrants)
        // Non-Unit markers: No grants without WaveData - charges remain unchanged
        // (This method intentionally does nothing - it's a fallback that preserves current state)
        
        Debug.Log($"[MarkerEconomy] Wave grants applied (no WaveData): Unit=∞, Rec=+0, Mat=+0, Inf=+0");
        UpdateUI();
    }
    
    /// <summary>
    /// Legacy method for prototyping panel compatibility
    /// </summary>
    public void ApplyStageGrants() => ApplyStageGrantsDefault();
    
    /// <summary>
    /// Legacy method for prototyping panel compatibility
    /// </summary>
    public void ApplyWaveGrants() => ApplyWaveGrantsDefault();
    
    #endregion

    public bool CanPlaceUnitMarker()
    {
        // Unit markers are INFINITE - only limited by:
        // 1. Available charges (handled by currentUnitMarkerCharges which regenerates via wave moves)
        // 2. Max on grid (maxUnitMarkers)
        // If maxUnitMarkers is 0 or not set, use a reasonable default
        int effectiveMaxOnGrid = maxUnitMarkers > 0 ? maxUnitMarkers : 5;
        
        return currentUnitMarkerCharges > 0 &&
               currentUnitMarkers < effectiveMaxOnGrid;
    }

    public bool CanPlaceRecursionMarker()
    {
        return currentRecursionMarkerCharges > 0 &&
               currentRecursionMarkers < maxRecursionMarkers;
               // Note: recursionMarkersPlaced is for statistics only, not for limiting placement
    }

    public bool CanPlaceMatrixMarker()
    {
        int effectiveMaxMatrix = maxMatrixMarkers > 0 ? maxMatrixMarkers : 2; // Default to 2 if not set
        return currentMatrixMarkerCharges > 0 &&
               currentMatrixMarkers < effectiveMaxMatrix;
               // Note: matrixMarkersPlaced is for statistics only, not for limiting placement
    }

    public bool CanPlaceInfinityMarker()
    {
        return currentInfinityMarkerCharges > 0 &&
               currentInfinityMarkers < maxInfinityMarkers;
    }

    public void ConsumeUnitCharge(Vector2Int? position = null)
    {
        Vector2Int markerPosition = position ?? playerManager.currentTilePosition;
        
        currentUnitMarkerCharges--;
        // Note: Recharge progress is NOT reset when placing - it continues from where it was
        // This way players aren't "punished" for using markers
        currentUnitMarkers++;
        unitMarkersPlaced++;

        // Trigger audio event for unit marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(markerPosition);
        TriggerAudioEvent(GameAudioEvent.UnitMarkerPlaced, worldPosition);

        // Fire marker placed event for MessageHighlightManager validation
        GameEvents.FireMarkerPlaced(markerPosition, MarkerType.Unit);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(markerPosition, "unit");
        }
    }

    public void ConsumeRecursionCharge()
    {
        currentRecursionMarkerCharges--;
        currentRecursionMarkers++;
        recursionMarkersPlaced++;

        // Trigger audio event for recursion marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.RecursionMarkerPlaced, worldPosition);

        // Fire marker placed event for MessageHighlightManager validation
        GameEvents.FireMarkerPlaced(playerManager.currentTilePosition, MarkerType.Recursion);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "recursion");
        }
    }

    public void ConsumeMatrixCharge()
    {
        currentMatrixMarkerCharges--;
        currentMatrixMarkers++;
        matrixMarkersPlaced++;

        // Trigger audio event for matrix marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.MatrixMarkerPlaced, worldPosition);

        // Fire marker placed event for MessageHighlightManager validation
        GameEvents.FireMarkerPlaced(playerManager.currentTilePosition, MarkerType.Matrix);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(false);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "matrix");
        }
    }

    public void ConsumeInfinityCharge()
    {
        currentInfinityMarkerCharges--;
        currentInfinityMarkers++;
        infinityMarkersPlaced++;

        // Trigger audio event for infinity marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.MatrixMarkerPlaced, worldPosition); // TODO: Add infinity audio event

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(false);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "infinity");
        }
    }

    public void ReleaseUnitMarker()
    {
        currentUnitMarkers = Mathf.Max(0, currentUnitMarkers - 1);
    }

    public void ReleaseRecursionMarker()
    {
        currentRecursionMarkers = Mathf.Max(0, currentRecursionMarkers - 1);
    }

    public void ReleaseInfinityMarker()
    {
        currentInfinityMarkers = Mathf.Max(0, currentInfinityMarkers - 1);
    }

    public void ReleaseMatrixMarker()
    {
        currentMatrixMarkers = Mathf.Max(0, currentMatrixMarkers - 1);
    }

    // Methods to handle marker removal (unmarking)
    public void OnUnitMarkerRemoved()
    {
        // Decrement the placement counter when a marker is removed
        if (unitMarkersPlaced > 0)
        {
            unitMarkersPlaced--;
        }
        
        // Restore charge when marker is removed (if not at max)
        if (currentUnitMarkerCharges < maxUnitMarkerCharges)
        {
            currentUnitMarkerCharges++;
            this.Log($"Unit marker charge restored. Charges: {currentUnitMarkerCharges}/{maxUnitMarkerCharges}", EnableDebugLogs);
            UpdateUI();
        }
        
        // Notify statistics manager about marker removal
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerRemoved(playerManager.currentTilePosition, "unit");
        }
        
        if (EnableDebugLogs)
        {
            this.Log($"Unit marker removed. Total placed: {unitMarkersPlaced}", EnableDebugLogs);
        }
    }

    public void OnRecursionMarkerRemoved()
    {
        // Decrement the placement counter when a marker is removed
        if (recursionMarkersPlaced > 0)
        {
            recursionMarkersPlaced--;
        }
        
        // Notify statistics manager about marker removal
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerRemoved(playerManager.currentTilePosition, "recursion");
        }
        
        if (EnableDebugLogs)
        {
            this.Log($"Recursion marker removed. Total placed: {recursionMarkersPlaced}", EnableDebugLogs);
        }
    }

    public void OnMatrixMarkerRemoved()
    {
        // Decrement the placement counter when a marker is removed
        if (matrixMarkersPlaced > 0)
        {
            matrixMarkersPlaced--;
        }
        
        // Notify statistics manager about marker removal
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerRemoved(playerManager.currentTilePosition, "matrix");
        }
        
        if (EnableDebugLogs)
        {
            this.Log($"Matrix marker removed. Total placed: {matrixMarkersPlaced}", EnableDebugLogs);
        }
    }

    private void RegenerateCharges()
    {
        // Unit markers use MOVE-BASED regeneration via HandleWaveStep()
        // Non-Unit markers (Recursion, Matrix, Infinity) use inventory grants only - no regeneration
        // This method is kept for potential future use but currently does nothing
    }

    private void UpdateUI()
    {
        if (actionUI != null)
        {
            actionUI.UpdateCharges(currentUnitMarkerCharges, currentRecursionMarkerCharges, 
                                 currentMatrixMarkerCharges, GetCurrentCubeMarkers());
            // Pass recharge progress as fraction for UI display (move-based for Unit, no cooldown for others)
            float unitRechargeProgress = unitMarkerRechargeRate > 0 ? 
                (float)unitMarkerRechargeProgress / unitMarkerRechargeRate : 0f;
            // Non-Unit markers use inventory grants only - no cooldown regeneration
            actionUI.UpdateCooldowns(unitRechargeProgress, 0f, 0f, 0f);
        }
    }

    #endregion

    #region Public API (Delegates to MarkerSystem)

    // Next charge time for UI system
    // Move-based recharge: Returns moves remaining until next charge (as float for UI compatibility)
    public float GetNextUnitChargeTime() =>
        currentUnitMarkerCharges < maxUnitMarkerCharges ?
        (float)(unitMarkerRechargeRate - unitMarkerRechargeProgress) : 0f;
    // Non-Unit markers don't regenerate - they use inventory grants only
    public float GetNextRecursionChargeTime() => 0f;
    public float GetNextMatrixChargeTime() => 0f;

    // Unit marker methods
    public bool PlaceUnitMarker(Vector2Int position) => markerSystem.PlaceUnitMarker(position);
    public bool RemoveUnitMarkerAt(Vector2Int position) => markerSystem.RemoveUnitMarkerAt(position);
    public bool HasUnitMarkerAt(Vector2Int position) => markerSystem.HasUnitMarkerAt(position);
    public bool TriggerNextUnitMarker() => markerSystem.TriggerNextUnitMarker();

    // Recursion marker methods
    public bool PlaceRecursionMarker(Vector2Int position) => markerSystem.PlaceRecursionMarker(position);
    public bool RemoveRecursionMarkerAt(Vector2Int position) => markerSystem.RemoveRecursionMarkerAt(position);
    public bool HasRecursionMarkerAt(Vector2Int position) => markerSystem.HasRecursionMarkerAt(position);
    public bool TriggerNextRecursionMarker() => markerSystem.TriggerNextRecursionMarker();

    // Matrix marker methods
    public bool PlaceMatrixMarker(Vector2Int centerPosition, int size) => markerSystem.PlaceMatrixMarker(centerPosition, size);
    public bool RemoveMatrixMarkerAt(Vector2Int centerPosition) => markerSystem.RemoveMatrixMarkerAt(centerPosition);
    public bool HasMatrixMarkerAt(Vector2Int centerPosition) => markerSystem.HasMatrixMarkerAt(centerPosition);
    public bool TriggerNextMatrixMarker() => markerSystem.TriggerNextMatrixMarker();

    // Infinity marker methods
    public bool PlaceInfinityMarker(Vector2Int position) => markerSystem.PlaceInfinityMarker(position);
    public bool RemoveInfinityMarkerAt(Vector2Int position) => markerSystem.RemoveInfinityMarkerAt(position);
    public bool HasInfinityMarkerAt(Vector2Int position) => markerSystem.HasInfinityMarkerAt(position);
    public bool TriggerNextInfinityMarker() => markerSystem.TriggerNextInfinityMarker();

    public void CreateCubeMarker(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type = PlayerMarkerSystem.CubeMarkerType.Matrix, int size = 3) => markerSystem.CreateCubeMarker(position, type, size);
    public bool TriggerNextCubeMarker() => markerSystem.TriggerNextCubeMarker();
    public bool PowerUpNextCubeMarker() => markerSystem.PowerUpNextCubeMarker();

    public void ClearAllActions() => markerSystem.ClearAllActions();

    // Direct queue access for debugging
    public Queue<UnitMarker> UnitMarkers => markerSystem.UnitMarkers;
    public Queue<RecursionMarker> recursionMarkers => markerSystem.RecursionMarkers;
    public Queue<MatrixMarker> matrixMarkers => markerSystem.MatrixMarkers;


    
    


    #endregion

    #region Public Information Methods

    // Resource availability checks
    public bool CanPlaceUnitMarkerCheck() => CanPlaceUnitMarker();
    public bool CanPlaceRecursionMarkerCheck() => CanPlaceRecursionMarker();
    public bool CanPlaceMatrixMarkerCheck() => CanPlaceMatrixMarker();

    // Charge refill methods (for prototyping tools)
    // Unit always uses maxCharges (move-based regeneration)
    // Non-Unit uses inventory cap when economy enabled, otherwise maxCharges
    public void RefillUnitMarkerCharges() => currentUnitMarkerCharges = maxUnitMarkerCharges;
    public void RefillRecursionMarkerCharges() => currentRecursionMarkerCharges = useMarkerEconomy ? maxRecursionInventory : maxRecursionMarkerCharges;
    public void RefillMatrixMarkerCharges() => currentMatrixMarkerCharges = useMarkerEconomy ? maxMatrixInventory : maxMatrixMarkerCharges;
    public void RefillInfinityMarkerCharges() => currentInfinityMarkerCharges = useMarkerEconomy ? maxInfinityInventory : maxInfinityMarkerCharges;
    public void RefillAllCharges()
    {
        RefillUnitMarkerCharges();
        RefillRecursionMarkerCharges();
        RefillMatrixMarkerCharges();
        RefillInfinityMarkerCharges();
    }

    // Recharge information
    public float GetUnitMarkerCooldownRemaining()
    {
        if (currentUnitMarkerCharges >= maxUnitMarkerCharges)
            return 0f;
        // Move-based: Return moves remaining as float (for UI compatibility)
        return (float)(unitMarkerRechargeRate - unitMarkerRechargeProgress);
    }

    // Non-Unit markers don't regenerate - they use inventory grants only
    // These methods return 0 to indicate no cooldown/wait time
    public float GetRecursionMarkerCooldownRemaining() => 0f;
    public float GetMatrixMarkerCooldownRemaining() => 0f;
    public float GetInfinityMarkerCooldownRemaining() => 0f;

    // Statistics
    public int GetunitMarkersPlaced() => unitMarkersPlaced;
    public int GetrecursionMarkersPlaced() => recursionMarkersPlaced;
    public int GetMatrixMarkersPlaced() => matrixMarkersPlaced;
    public int GetCubeMarkersTriggered() => cubeMarkersTriggered;
    public int GetPerfectTimingHits() => perfectTimingHits;

    // Current state information
    public int GetCurrentUnitMarkers() => currentUnitMarkers;
    public int GetCurrentRecursionMarkers() => currentRecursionMarkers;
    public int GetCurrentMatrixMarkers() => currentMatrixMarkers;
    public int GetCurrentCubeMarkers() => markerSystem?.cubeMarkers?.Count ?? 0;
    public int GetCurrentUnitCharges() => currentUnitMarkerCharges;
    public int GetCurrentRecursionCharges() => currentRecursionMarkerCharges;
    public int GetCurrentMatrixCharges() => currentMatrixMarkerCharges;
    public int GetCurrentInfinityCharges() => currentInfinityMarkerCharges;
    public int GetCurrentInfinityMarkers() => currentInfinityMarkers;
    
    // Setters for scenario system
    public void SetUnitMarkerCharges(int value) => currentUnitMarkerCharges = Mathf.Clamp(value, 0, maxUnitMarkerCharges > 0 ? maxUnitMarkerCharges : 99);
    public void SetRecursionMarkerCharges(int value) => currentRecursionMarkerCharges = Mathf.Clamp(value, 0, useMarkerEconomy ? maxRecursionInventory : 99);
    public void SetMatrixMarkerCharges(int value) => currentMatrixMarkerCharges = Mathf.Clamp(value, 0, useMarkerEconomy ? maxMatrixInventory : 99);
    public void SetInfinityMarkerCharges(int value) => currentInfinityMarkerCharges = Mathf.Clamp(value, 0, useMarkerEconomy ? maxInfinityInventory : 99);

    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        return $"PlayerAction: Unit:{currentUnitMarkerCharges}/{maxUnitMarkerCharges} Recursion:{currentRecursionMarkerCharges}/{maxRecursionMarkerCharges} Matrix:{currentMatrixMarkerCharges}/{maxMatrixMarkerCharges} OnGrid:{currentUnitMarkers}+{currentRecursionMarkers}+{currentMatrixMarkers} Cube:{GetCurrentCubeMarkers()}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Current Marker Mode"] = currentMarkerMode.ToString(),
            ["Unit Markers Placed"] = unitMarkersPlaced,
            ["Current Unit Markers"] = currentUnitMarkers,
            ["Unit Charges"] = $"{currentUnitMarkerCharges}/{maxUnitMarkerCharges}",
            ["Unit Recharge Moves Remaining"] = GetUnitMarkerCooldownRemaining(),
            ["Recursion Markers Placed"] = recursionMarkersPlaced,
            ["Current Recursion Markers"] = currentRecursionMarkers,
            ["Recursion Charges"] = $"{currentRecursionMarkerCharges}/{maxRecursionInventory}",
            ["Matrix Markers Placed"] = matrixMarkersPlaced,
            ["Current Matrix Markers"] = currentMatrixMarkers,
            ["Matrix Charges"] = $"{currentMatrixMarkerCharges}/{maxMatrixInventory}",
            ["Infinity Charges"] = $"{currentInfinityMarkerCharges}/{maxInfinityInventory}",
            ["Cube Markers Active"] = GetCurrentCubeMarkers(),
            ["Perfect Timing Hits"] = perfectTimingHits,
            ["Cube Markers Triggered"] = cubeMarkersTriggered,
            ["Input Enabled"] = inputEnabled,
            ["Can Place Unit"] = CanPlaceUnitMarker(),
            ["Can Place Recursion"] = CanPlaceRecursionMarker(),
            ["Can Place Matrix"] = CanPlaceMatrixMarker(),
            ["Can Place Infinity"] = CanPlaceInfinityMarker()
        };

        // Add input feedback system debug information
        if (inputFeedbackManager != null)
        {
            debugData["Feedback System Available"] = true;
            debugData["Registered Feedback Hooks"] = inputFeedbackManager.GetRegisteredHookCount();
            debugData["Active Feedback Hooks"] = inputFeedbackManager.GetActiveHookCount();
            debugData["Feedback Hook Names"] = string.Join(", ", inputFeedbackManager.GetRegisteredHookNames());
        }
        else
        {
            debugData["Feedback System Available"] = false;
            debugData["Registered Feedback Hooks"] = 0;
            debugData["Active Feedback Hooks"] = 0;
        }

        // Add animation trigger system debug information
        if (animationTriggerManager != null)
        {
            debugData["Animation System Available"] = true;
            debugData["Animation Receivers"] = animationTriggerManager.GetTotalReceiverCount();
            debugData["Animation System Status"] = animationTriggerManager.GetDebugStatus();
        }
        else
        {
            debugData["Animation System Available"] = false;
            debugData["Animation Receivers"] = 0;
        }

        return debugData;
    }

    /// <summary>
    /// Validates and adjusts current mode based on available marker types
    /// Called when wave configuration changes
    /// </summary>
    public void ValidateCurrentMode()
    {
        // Check if current mode is still valid, if not switch to first available mode
        if (!CanSwitchMode(currentMarkerMode))
        {
            MarkerMode previousMode = currentMarkerMode;
            
            // Try to switch to Unit mode first (most common)
            if (maxUnitMarkerCharges > 0)
            {
                SetMode(MarkerMode.Unit);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Unit mode.", EnableDebugLogs);
                }
            }
            // Fallback to Recursion if Unit not available
            else if (maxRecursionInventory > 0)
            {
                SetMode(MarkerMode.Recursion);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Recursion mode.", EnableDebugLogs);
                }
            }
            // Fallback to Matrix if Recursion not available
            else if (maxMatrixInventory > 0)
            {
                SetMode(MarkerMode.Matrix);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Matrix mode.", EnableDebugLogs);
                }
            }
            // Fallback to Infinity if Matrix not available
            else if (maxInfinityInventory > 0)
            {
                SetMode(MarkerMode.Infinity);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Infinity mode.", EnableDebugLogs);
                }
            }
        }
        
        // Update UI to reflect any changes
        UpdateUI();
    }

    public void ResetToDefaults()
    {
        // Reset charges to max
        currentUnitMarkerCharges = maxUnitMarkerCharges;
        currentRecursionMarkerCharges = maxRecursionMarkerCharges;
        currentMatrixMarkerCharges = maxMatrixMarkerCharges;
        currentInfinityMarkerCharges = maxInfinityMarkerCharges;
        
        // Reset marker mode (direct assignment for reset operation)
        currentMarkerMode = MarkerMode.Unit;
        
        // Clear all markers
        if (markerSystem != null)
        {
            markerSystem.ClearAllActions();
        }
        
        // Reset counters
        unitMarkersPlaced = 0;
        recursionMarkersPlaced = 0;
        matrixMarkersPlaced = 0;
        infinityMarkersPlaced = 0;
        currentUnitMarkers = 0;
        currentRecursionMarkers = 0;
        currentMatrixMarkers = 0;
        currentInfinityMarkers = 0;
        cubeMarkersTriggered = 0;
        perfectTimingHits = 0;
        
        // Reset recharge progress (Unit markers only - others use inventory grants)
        unitMarkerRechargeProgress = 0;
        
        // Update UI
        UpdateUI();
        
        if (EnableDebugLogs)
            this.Log("Reset to defaults completed", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading from ScriptableObject or JSON
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving to ScriptableObject or JSON
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}