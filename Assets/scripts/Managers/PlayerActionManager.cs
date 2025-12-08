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

    public UnitMarker(Vector2Int pos, float time)
    {
        position = pos;
        placementTime = time;
    }
}

[System.Serializable]
public class RecursionMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;

    public RecursionMarker(Vector2Int pos, float time)
    {
        position = pos;
        placementTime = time;
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

    public MatrixMarker(Vector2Int center, int markerSize, float time)
    {
        centerPosition = center;
        size = markerSize;
        placementTime = time;
    }
}

[System.Serializable]
public class InfinityMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;

    public InfinityMarker(Vector2Int pos, float time)
    {
        position = pos;
        placementTime = time;
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

    [Header("Unit Marker Settings")]
    [SerializeField] public int maxUnitMarkers;
    [SerializeField] public int unitMarkersPlaced;
    [SerializeField] public int currentUnitMarkers;
    [SerializeField] public int maxUnitMarkerCharges;
    [SerializeField] private int currentUnitMarkerCharges;
    [SerializeField] public float unitMarkerCooldown;
    [SerializeField] public Material unitMarkerMaterial;
    [SerializeField] public float lastUnitMarkerTime;

    [Header("Recursion Marker Settings")]
    [SerializeField] public int maxRecursionMarkers;
    [SerializeField] public int recursionMarkersPlaced;
    [SerializeField] public int currentRecursionMarkers;
    [SerializeField] public int maxRecursionMarkerCharges;
    [SerializeField] private int currentRecursionMarkerCharges;
    [SerializeField] public float recursionMarkerCooldown;
    [SerializeField] public Material recursionMarkerMaterial;
    [SerializeField] public float lastRecursionMarkerTime;

    [Header("Matrix Marker Settings")]
    [SerializeField] public int maxMatrixMarkers;
    [SerializeField] public int matrixMarkersPlaced;
    [SerializeField] public int currentMatrixMarkers;
    [SerializeField] public int maxMatrixMarkerCharges;
    [SerializeField] private int currentMatrixMarkerCharges;
    [SerializeField] public float matrixMarkerCooldown;
    [SerializeField] public float lastMatrixMarkerTime;
    [SerializeField] public int matrixMarkerSize;
    [SerializeField] public int matrixMarkerOnGridLimit;
    [SerializeField] public Material matrixMarkerMaterial;

    [Header("Infinity Marker Settings")]
    [SerializeField] public int maxInfinityMarkers = 2;
    [SerializeField] public int infinityMarkersPlaced;
    [SerializeField] public int currentInfinityMarkers;
    [SerializeField] public int maxInfinityMarkerCharges = 1;
    [SerializeField] private int currentInfinityMarkerCharges;
    [SerializeField] public float infinityMarkerCooldown = 15f;
    [SerializeField] public float lastInfinityMarkerTime;
    [SerializeField] public Material infinityMarkerMaterial;

    [Header("Input Settings")]
    [SerializeField] private KeyCode unitMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode recursionMarkerKey = KeyCode.V;
    [SerializeField] private KeyCode matrixMarkerKey = KeyCode.G;
    [SerializeField] private KeyCode infinityMarkerKey = KeyCode.H;
    [SerializeField] private KeyCode triggerUnitKey = KeyCode.R;
    [SerializeField] private KeyCode triggerRecursionKey = KeyCode.Y;
    [SerializeField] private KeyCode triggerMatrixKey = KeyCode.T;
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
        EnableDebugLogs = true;
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

    private void InitializeReferences()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerManager>();
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();
        if (actionUI == null)
            actionUI = FindObjectOfType<PlayerActionUI>();
        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();
        if (inputFeedbackManager == null)
            inputFeedbackManager = FindObjectOfType<InputFeedbackManager>();
        if (animationTriggerManager == null)
            animationTriggerManager = FindObjectOfType<AnimationTriggerManager>();
            
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
        currentUnitMarkerCharges = maxUnitMarkerCharges;
        currentRecursionMarkerCharges = maxRecursionMarkerCharges;
        currentMatrixMarkerCharges = maxMatrixMarkerCharges;
        currentInfinityMarkerCharges = maxInfinityMarkerCharges;
        inputEnabled = true;
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
                        markerSystem.PlaceUnitMarker(playerPos);
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
                        markerSystem.PlaceRecursionMarker(playerPos);
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
                        markerSystem.PlaceMatrixMarker(playerPos, matrixMarkerSize);
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
                        markerSystem.PlaceInfinityMarker(playerPos);
                        actionSuccessful = true;
                        
                        TriggerInputFeedbackMarkerPlace(MarkerMode.Infinity, playerPos, false);
                        TriggerAnimationMarkerPlace(MarkerMode.Infinity, playerPos, false);
                        ShowActionSuccessFeedback("Infinity marker placed successfully!");
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

    [System.Obsolete("Use HandleUnifiedPlaceInput() instead - individual marker input handlers are deprecated")]
    private void HandleUnitMarkerInput()
    {
        if (Input.GetKeyDown(unitMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (CanPlaceUnitMarker())
            {
                markerSystem.PlaceUnitMarker(playerPos);
            }
        }
    }

    [System.Obsolete("Use HandleUnifiedPlaceInput() instead - individual marker input handlers are deprecated")]
    private void HandleRecursionMarkerInput()
    {
        if (Input.GetKeyDown(recursionMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (CanPlaceRecursionMarker())
            {
                markerSystem.PlaceRecursionMarker(playerPos);
            }
        }
    }

    [System.Obsolete("Use HandleUnifiedPlaceInput() instead - individual marker input handlers are deprecated")]
    private void HandleMatrixMarkerInput()
    {
        if (Input.GetKeyDown(matrixMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (CanPlaceMatrixMarker())
            {
                markerSystem.PlaceMatrixMarker(playerPos, matrixMarkerSize);
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

    /// <summary>
    /// Legacy trigger inputs - kept for backward compatibility but marked as obsolete
    /// </summary>
    [System.Obsolete("Use unified R key triggering instead - individual trigger keys are deprecated")]
    private void HandleLegacyTriggerInputs()
    {
        if (Input.GetKeyDown(triggerUnitKey))
        {
            markerSystem.TriggerNextUnitMarker();
        }

        if (Input.GetKeyDown(triggerRecursionKey))
        {
            markerSystem.TriggerNextRecursionMarker();
        }

        if (Input.GetKeyDown(triggerMatrixKey))
        {
            markerSystem.TriggerNextMatrixMarker();
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
                        return "No Unit marker charges available. Wait for cooldown.";
                    if (currentUnitMarkers >= maxUnitMarkers)
                        return "Maximum Unit markers already placed on grid.";
                    break;

                case MarkerMode.Recursion:
                    if (currentRecursionMarkerCharges <= 0)
                        return "No Recursion marker charges available. Wait for cooldown.";
                    if (currentRecursionMarkers >= maxRecursionMarkers)
                        return "Maximum Recursion markers already placed on grid.";
                    break;

                case MarkerMode.Matrix:
                    if (currentMatrixMarkerCharges <= 0)
                        return "No matrix marker charges available. Wait for cooldown.";
                    if (currentMatrixMarkers >= matrixMarkerOnGridLimit)
                        return "Maximum matrix markers already placed on grid.";
                    break;

                case MarkerMode.Infinity:
                    if (currentInfinityMarkerCharges <= 0)
                        return "No infinity marker charges available. Wait for cooldown.";
                    if (currentInfinityMarkers >= maxInfinityMarkers)
                        return "Maximum infinity markers already placed on grid.";
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
        switch (mode)
        {
            case MarkerMode.Unit:
                if (maxUnitMarkerCharges <= 0)
                {
                    ShowActionErrorFeedback("Unit markers are not available in this wave.");
                    return false;
                }
                break;
                
            case MarkerMode.Recursion:
                if (maxRecursionMarkerCharges <= 0)
                {
                    ShowActionErrorFeedback("Recursion markers are not available in this wave.");
                    return false;
                }
                break;
                
            case MarkerMode.Matrix:
                if (maxMatrixMarkerCharges <= 0)
                {
                    ShowActionErrorFeedback("Matrix markers are not available in this wave.");
                    return false;
                }
                break;
                
            case MarkerMode.Infinity:
                if (maxInfinityMarkerCharges <= 0)
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

    public bool CanPlaceUnitMarker()
    {
        return currentUnitMarkerCharges > 0 &&
               currentUnitMarkers < maxUnitMarkers;
               // Note: unitMarkersPlaced is for statistics only, not for limiting placement
    }

    public bool CanPlaceRecursionMarker()
    {
        return currentRecursionMarkerCharges > 0 &&
               currentRecursionMarkers < maxRecursionMarkers;
               // Note: recursionMarkersPlaced is for statistics only, not for limiting placement
    }

    public bool CanPlaceMatrixMarker()
    {
        return currentMatrixMarkerCharges > 0 &&
               currentMatrixMarkers < matrixMarkerOnGridLimit;
               // Note: matrixMarkersPlaced is for statistics only, not for limiting placement
    }

    public bool CanPlaceInfinityMarker()
    {
        return currentInfinityMarkerCharges > 0 &&
               currentInfinityMarkers < maxInfinityMarkers;
    }

    public void ConsumeUnitCharge()
    {
        currentUnitMarkerCharges--;
        lastUnitMarkerTime = Time.time;
        currentUnitMarkers++;
        unitMarkersPlaced++;

        // Trigger audio event for unit marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.UnitMarkerPlaced, worldPosition);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "unit");
        }
    }

    public void ConsumeRecursionCharge()
    {
        currentRecursionMarkerCharges--;
        lastRecursionMarkerTime = Time.time;
        currentRecursionMarkers++;
        recursionMarkersPlaced++;

        // Trigger audio event for recursion marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.RecursionMarkerPlaced, worldPosition);

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
        lastMatrixMarkerTime = Time.time;
        currentMatrixMarkers++;
        matrixMarkersPlaced++;

        // Trigger audio event for matrix marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(GameAudioEvent.MatrixMarkerPlaced, worldPosition);

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
        lastInfinityMarkerTime = Time.time;
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
        currentUnitMarkers--;
    }

    public void ReleaseRecursionMarker()
    {
        currentRecursionMarkers--;
    }

    public void ReleaseInfinityMarker()
    {
        currentInfinityMarkers--;
    }

    public void ReleaseMatrixMarker()
    {
        currentMatrixMarkers--;
    }

    // Methods to handle marker removal (unmarking)
    public void OnUnitMarkerRemoved()
    {
        // Decrement the placement counter when a marker is removed
        if (unitMarkersPlaced > 0)
        {
            unitMarkersPlaced--;
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
        bool chargesChanged = false;
        chargesChanged |= RegenerateUnitCharges();
        chargesChanged |= RegenerateRecursionCharges();
        chargesChanged |= RegenerateMatrixCharges();
        chargesChanged |= RegenerateInfinityCharges();

        if (chargesChanged)
        {
            UpdateUI();
        }
    }

    private bool RegenerateUnitCharges()
    {
        if (currentUnitMarkerCharges < maxUnitMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastUnitMarkerTime;
            if (timeSinceLastUse >= unitMarkerCooldown)
            {
                currentUnitMarkerCharges++;
                lastUnitMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Unit marker charge");
                
                this.Log($"Unit marker charge regenerated. Charges: {currentUnitMarkerCharges}/{maxUnitMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private bool RegenerateRecursionCharges()
    {
        if (currentRecursionMarkerCharges < maxRecursionMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastRecursionMarkerTime;
            if (timeSinceLastUse >= recursionMarkerCooldown)
            {
                currentRecursionMarkerCharges++;
                lastRecursionMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Recursion marker charge");
                
                this.Log($"Recursion marker charge regenerated. Charges: {currentRecursionMarkerCharges}/{maxRecursionMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private bool RegenerateMatrixCharges()
    {
        if (currentMatrixMarkerCharges < maxMatrixMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastMatrixMarkerTime;
            if (timeSinceLastUse >= matrixMarkerCooldown)
            {
                currentMatrixMarkerCharges++;
                lastMatrixMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Matrix marker charge");
                
                this.Log($"Matrix marker charge regenerated. Charges: {currentMatrixMarkerCharges}/{maxMatrixMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private bool RegenerateInfinityCharges()
    {
        if (currentInfinityMarkerCharges < maxInfinityMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastInfinityMarkerTime;
            if (timeSinceLastUse >= infinityMarkerCooldown)
            {
                currentInfinityMarkerCharges++;
                lastInfinityMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Infinity marker charge");
                
                this.Log($"Infinity marker charge regenerated. Charges: {currentInfinityMarkerCharges}/{maxInfinityMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private void UpdateUI()
    {
        if (actionUI != null)
        {
            actionUI.UpdateCharges(currentUnitMarkerCharges, currentRecursionMarkerCharges, 
                                 currentMatrixMarkerCharges, GetCurrentCubeMarkers());
            actionUI.UpdateCooldowns(unitMarkerCooldown, recursionMarkerCooldown, 
                                   matrixMarkerCooldown, 1f);
        }
    }

    #endregion

    #region Public API (Delegates to MarkerSystem)

    // Next charge time for UI system
    public float GetNextUnitChargeTime() =>
        currentUnitMarkerCharges < maxUnitMarkerCharges ?
        lastUnitMarkerTime + unitMarkerCooldown : Time.time;
    public float GetNextRecursionChargeTime() =>
        currentRecursionMarkerCharges < maxRecursionMarkerCharges ?
        lastRecursionMarkerTime + recursionMarkerCooldown : Time.time;
    public float GetNextMatrixChargeTime() =>
        currentMatrixMarkerCharges < maxMatrixMarkerCharges ?
        lastMatrixMarkerTime + matrixMarkerCooldown : Time.time;

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
    public void RefillUnitMarkerCharges() => currentUnitMarkerCharges = maxUnitMarkerCharges;
    public void RefillRecursionMarkerCharges() => currentRecursionMarkerCharges = maxRecursionMarkerCharges;
    public void RefillMatrixMarkerCharges() => currentMatrixMarkerCharges = maxMatrixMarkerCharges;
    public void RefillInfinityMarkerCharges() => currentInfinityMarkerCharges = maxInfinityMarkerCharges;
    public void RefillAllCharges()
    {
        RefillUnitMarkerCharges();
        RefillRecursionMarkerCharges();
        RefillMatrixMarkerCharges();
        RefillInfinityMarkerCharges();
    }

    // Cooldown information
    public float GetUnitMarkerCooldownRemaining()
    {
        if (currentUnitMarkerCharges >= maxUnitMarkerCharges)
            return 0f;
        return Mathf.Max(0f, unitMarkerCooldown - (Time.time - lastUnitMarkerTime));
    }

    public float GetRecursionMarkerCooldownRemaining()
    {
        if (currentRecursionMarkerCharges >= maxRecursionMarkerCharges)
            return 0f;
        return Mathf.Max(0f, recursionMarkerCooldown - (Time.time - lastRecursionMarkerTime));
    }

    public float GetMatrixMarkerCooldownRemaining()
    {
        if (currentMatrixMarkerCharges >= maxMatrixMarkerCharges)
            return 0f;
        return Mathf.Max(0f, matrixMarkerCooldown - (Time.time - lastMatrixMarkerTime));
    }

    public float GetInfinityMarkerCooldownRemaining()
    {
        if (currentInfinityMarkerCharges >= maxInfinityMarkerCharges)
            return 0f;
        return Mathf.Max(0f, infinityMarkerCooldown - (Time.time - lastInfinityMarkerTime));
    }

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



    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs { get; set; } = true;

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
            ["Unit Cooldown Remaining"] = GetUnitMarkerCooldownRemaining(),
            ["Recursion Markers Placed"] = recursionMarkersPlaced,
            ["Current Recursion Markers"] = currentRecursionMarkers,
            ["Recursion Charges"] = $"{currentRecursionMarkerCharges}/{maxRecursionMarkerCharges}",
            ["Recursion Cooldown Remaining"] = GetRecursionMarkerCooldownRemaining(),
            ["Matrix Markers Placed"] = matrixMarkersPlaced,
            ["Current Matrix Markers"] = currentMatrixMarkers,
            ["Matrix Charges"] = $"{currentMatrixMarkerCharges}/{maxMatrixMarkerCharges}",
            ["Matrix Cooldown Remaining"] = GetMatrixMarkerCooldownRemaining(),
            ["Cube Markers Active"] = GetCurrentCubeMarkers(),
            ["Perfect Timing Hits"] = perfectTimingHits,
            ["Cube Markers Triggered"] = cubeMarkersTriggered,
            ["Input Enabled"] = inputEnabled,
            ["Can Place Unit"] = CanPlaceUnitMarker(),
            ["Can Place Recursion"] = CanPlaceRecursionMarker(),
            ["Can Place Matrix"] = CanPlaceMatrixMarker()
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
            else if (maxRecursionMarkerCharges > 0)
            {
                SetMode(MarkerMode.Recursion);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Recursion mode.", EnableDebugLogs);
                }
            }
            // Fallback to Matrix if Recursion not available
            else if (maxMatrixMarkerCharges > 0)
            {
                SetMode(MarkerMode.Matrix);
                if (EnableDebugLogs)
                {
                    this.Log($"{previousMode} mode was active but markers are not available. Switched to Matrix mode.", EnableDebugLogs);
                }
            }
            // Fallback to Infinity if Matrix not available
            else if (maxInfinityMarkerCharges > 0)
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
        
        // Reset timers
        lastUnitMarkerTime = 0f;
        lastRecursionMarkerTime = 0f;
        lastMatrixMarkerTime = 0f;
        lastInfinityMarkerTime = 0f;
        
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