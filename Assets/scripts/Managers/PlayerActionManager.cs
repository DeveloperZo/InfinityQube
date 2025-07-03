using UnityEngine;
using System.Collections.Generic;
using System;


[System.Serializable]
public class LightMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;

    public LightMarker(Vector2Int pos, float time)
    {
        position = pos;
        placementTime = time;
    }
}

[System.Serializable]
public class HeavyMarker
{
    public Vector2Int position;
    public float placementTime;
    public GameObject visualObject;
    public bool isPerfectTiming = false;

    public HeavyMarker(Vector2Int pos, float time)
    {
        position = pos;
        placementTime = time;
    }
}



[System.Serializable]
public class PrimeMarker
{
    public Vector2Int centerPosition;
    public int size;
    public float placementTime;
    public List<GameObject> visualObjects = new List<GameObject>();
    public List<Vector2Int> affectedPositions = new List<Vector2Int>();

    public PrimeMarker(Vector2Int center, int markerSize, float time)
    {
        centerPosition = center;
        size = markerSize;
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

    [Header("Light Marker Settings")]
    [SerializeField] public int maxLightMarkers = 3;
    [SerializeField] public int lightMarkersPlaced = 0;
    [SerializeField] public int currentLightMarkers = 0;
    [SerializeField] public int maxLightMarkerCharges = 2;
    [SerializeField] private int currentLightMarkerCharges;
    [SerializeField] public float lightMarkerCooldown = 2f;
    [SerializeField] public Material lightMarkerMaterial;
    [SerializeField] public float lastLightMarkerTime = 0f;

    [Header("Heavy Marker Settings")]
    [SerializeField] public int maxHeavyMarkers = 2;
    [SerializeField] public int heavyMarkersPlaced = 0;
    [SerializeField] public int currentHeavyMarkers = 0;
    [SerializeField] public int maxHeavyMarkerCharges = 1;
    [SerializeField] private int currentHeavyMarkerCharges;
    [SerializeField] public float heavyMarkerCooldown = 5f;
    [SerializeField] public Material heavyMarkerMaterial;
    [SerializeField] public float lastHeavyMarkerTime = 0f;

    [Header("Prime Marker Settings")]
    [SerializeField] public int maxPrimeMarkers = 2;
    [SerializeField] public int primeMarkersPlaced = 0;
    [SerializeField] public int currentPrimeMarkers = 0;
    [SerializeField] public int maxPrimeMarkerCharges = 2;
    [SerializeField] private int currentPrimeMarkerCharges;
    [SerializeField] public float primeMarkerCooldown = 4f;
    [SerializeField] public float lastPrimeMarkerTime = 0f;
    [SerializeField] public int primeMarkerSize = 2;
    [SerializeField] public int primeMarkerOnGridLimit = 1;
    [SerializeField] public Material primeMarkerMaterial;

    [Header("Input Settings")]
    [SerializeField] private KeyCode lightMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode heavyMarkerKey = KeyCode.V;
    [SerializeField] private KeyCode primeMarkerKey = KeyCode.G;
    [SerializeField] private KeyCode triggerLightKey = KeyCode.R;
    [SerializeField] private KeyCode triggerHeavyKey = KeyCode.Y;
    [SerializeField] private KeyCode triggerPrimeKey = KeyCode.T;
    [SerializeField] private KeyCode triggerCubeMarkerKey = KeyCode.Q;
    


    // Marker Mode System
    [Header("Marker Mode System")]
    [SerializeField] private Enumerations.MarkerMode currentMarkerMode = Enumerations.MarkerMode.Light;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;
    private bool inputEnabled = false;

    public GridManager GridManager => gridManager;
    public PlayerManager PlayerManager => playerManager;
    public WaveManager WaveManager => waveManager;

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
        currentLightMarkerCharges = maxLightMarkerCharges;
        currentHeavyMarkerCharges = maxHeavyMarkerCharges;
        currentPrimeMarkerCharges = maxPrimeMarkerCharges;
        inputEnabled = true;
    }

    private void InitializeMarkerMode()
    {
        currentMarkerMode = Enumerations.MarkerMode.Light;
        
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
    private void TriggerAnimationModeSwitch(Enumerations.MarkerMode previousMode, Enumerations.MarkerMode newMode, Vector2Int playerPosition)
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
    private void TriggerAnimationMarkerPlace(Enumerations.MarkerMode markerMode, Vector2Int position, bool wasReplacement)
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
    private void TriggerAnimationMarkerTrigger(Enumerations.MarkerMode markerMode, Vector2Int position, int targetCount)
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
    }

    /// <summary>
    /// Handles mode switching input using number keys 1-3
    /// </summary>
    private void HandleModeSwitchingInput()
    {
        Enumerations.MarkerMode targetMode = currentMarkerMode;
        Enumerations.GameAudioEvent audioEvent = Enumerations.GameAudioEvent.ModeSwitchedToLight;
        bool modeSwitchRequested = false;

        // Check for number key presses
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            targetMode = Enumerations.MarkerMode.Light;
            audioEvent = Enumerations.GameAudioEvent.ModeSwitchedToLight;
            modeSwitchRequested = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            targetMode = Enumerations.MarkerMode.Prime;
            audioEvent = Enumerations.GameAudioEvent.ModeSwitchedToPrime;
            modeSwitchRequested = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            targetMode = Enumerations.MarkerMode.Heavy;
            audioEvent = Enumerations.GameAudioEvent.ModeSwitchedToHeavy;
            modeSwitchRequested = true;
        }

        // Only process mode switch if a key was pressed and mode is different
        if (modeSwitchRequested && targetMode != currentMarkerMode)
        {
            Enumerations.MarkerMode previousMode = currentMarkerMode;
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
            Enumerations.MarkerMode currentMode = GetCurrentMode();
            bool actionSuccessful = false;

            switch (currentMode)
            {
                case Enumerations.MarkerMode.Light:
                    if (markerSystem.HasLightMarkerAt(playerPos))
                    {
                        markerSystem.RemoveLightMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Light, playerPos, true);
                    }
                    else if (CanPlaceLightMarker())
                    {
                        markerSystem.PlaceLightMarker(playerPos);
                        actionSuccessful = true;
                        
                        // Trigger feedback for new marker placement
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Light, playerPos, false);
                        
                        // Trigger animation for marker placement
                        TriggerAnimationMarkerPlace(Enumerations.MarkerMode.Light, playerPos, false);
                        
                        // Show success feedback for successful placement
                        ShowActionSuccessFeedback("Light marker placed successfully!");
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
                            this.LogWarning($"Light marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                case Enumerations.MarkerMode.Heavy:
                    if (markerSystem.HasHeavyMarkerAt(playerPos))
                    {
                        markerSystem.RemoveHeavyMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Heavy, playerPos, true);
                    }
                    else if (CanPlaceHeavyMarker())
                    {
                        markerSystem.PlaceHeavyMarker(playerPos);
                        actionSuccessful = true;
                        
                        // Trigger feedback for new marker placement
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Heavy, playerPos, false);
                        
                        // Trigger animation for marker placement
                        TriggerAnimationMarkerPlace(Enumerations.MarkerMode.Heavy, playerPos, false);
                        
                        // Show success feedback for successful placement
                        ShowActionSuccessFeedback("Heavy marker placed successfully!");
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
                            this.LogWarning($"Heavy marker placement failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    break;

                case Enumerations.MarkerMode.Prime:
                    if (markerSystem.HasPrimeMarkerAt(playerPos))
                    {
                        markerSystem.RemovePrimeMarkerAt(playerPos);
                        actionSuccessful = true;
                        // Trigger feedback for marker removal (replacement)
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Prime, playerPos, true);
                    }
                    else if (CanPlacePrimeMarker())
                    {
                        markerSystem.PlacePrimeMarker(playerPos, primeMarkerSize);
                        actionSuccessful = true;
                        
                        // Trigger feedback for new marker placement
                        TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode.Prime, playerPos, false);
                        
                        // Trigger animation for marker placement
                        TriggerAnimationMarkerPlace(Enumerations.MarkerMode.Prime, playerPos, false);
                        
                        // Show success feedback for successful placement
                        ShowActionSuccessFeedback("Prime marker placed successfully!");
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
                            this.LogWarning($"Prime marker placement failed: {errorMessage}", EnableDebugLogs);
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
    private void HandleLightMarkerInput()
    {
        if (Input.GetKeyDown(lightMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (markerSystem.HasLightMarkerAt(playerPos))
            {
                markerSystem.RemoveLightMarkerAt(playerPos);
            }
            else if (CanPlaceLightMarker())
            {
                markerSystem.PlaceLightMarker(playerPos);
            }
        }
    }

    [System.Obsolete("Use HandleUnifiedPlaceInput() instead - individual marker input handlers are deprecated")]
    private void HandleHeavyMarkerInput()
    {
        if (Input.GetKeyDown(heavyMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (markerSystem.HasHeavyMarkerAt(playerPos))
            {
                markerSystem.RemoveHeavyMarkerAt(playerPos);
            }
            else if (CanPlaceHeavyMarker())
            {
                markerSystem.PlaceHeavyMarker(playerPos);
            }
        }
    }

    [System.Obsolete("Use HandleUnifiedPlaceInput() instead - individual marker input handlers are deprecated")]
    private void HandlePrimeMarkerInput()
    {
        if (Input.GetKeyDown(primeMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (markerSystem.HasPrimeMarkerAt(playerPos))
            {
                markerSystem.RemovePrimeMarkerAt(playerPos);
            }
            else if (CanPlacePrimeMarker())
            {
                markerSystem.PlacePrimeMarker(playerPos, primeMarkerSize);
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
            Enumerations.MarkerMode currentMode = GetCurrentMode();
            bool actionSuccessful = false;

            switch (currentMode)
            {
                case Enumerations.MarkerMode.Light:
                    actionSuccessful = markerSystem.TriggerNextLightMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Light marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        // Note: We use player position as approximation since triggered marker position may vary
                        TriggerInputFeedbackMarkerTrigger(Enumerations.MarkerMode.Light, GetCurrentPlayerPosition(), 1);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(Enumerations.MarkerMode.Light, GetCurrentPlayerPosition(), 1);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Light marker triggered successfully!");
                    }
                    break;

                case Enumerations.MarkerMode.Heavy:
                    actionSuccessful = markerSystem.TriggerNextHeavyMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Heavy marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        TriggerInputFeedbackMarkerTrigger(Enumerations.MarkerMode.Heavy, GetCurrentPlayerPosition(), 1);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(Enumerations.MarkerMode.Heavy, GetCurrentPlayerPosition(), 1);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Heavy marker triggered successfully!");
                    }
                    break;

                case Enumerations.MarkerMode.Prime:
                    actionSuccessful = markerSystem.TriggerNextPrimeMarker();
                    if (!actionSuccessful)
                    {
                        string errorMessage = GetModeActionErrorMessage(currentMode, "trigger");
                        ShowActionErrorFeedback(errorMessage);
                        
                        // Trigger feedback for failed action
                        TriggerInputFeedbackActionFailed("trigger", errorMessage, GetCurrentPlayerPosition(), 0.6f);
                        
                        if (EnableDebugLogs)
                        {
                            this.LogWarning($"Prime marker trigger failed: {errorMessage}", EnableDebugLogs);
                        }
                    }
                    else
                    {
                        // Trigger feedback for successful marker trigger
                        // Prime markers affect multiple targets, so we estimate area
                        int estimatedTargets = primeMarkerSize * primeMarkerSize;
                        TriggerInputFeedbackMarkerTrigger(Enumerations.MarkerMode.Prime, GetCurrentPlayerPosition(), estimatedTargets);
                        
                        // Trigger animation for marker trigger
                        TriggerAnimationMarkerTrigger(Enumerations.MarkerMode.Prime, GetCurrentPlayerPosition(), estimatedTargets);
                        
                        // Show success feedback for successful trigger
                        ShowActionSuccessFeedback("Prime marker triggered successfully!");
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
        if (Input.GetKeyDown(triggerLightKey))
        {
            markerSystem.TriggerNextLightMarker();
        }

        if (Input.GetKeyDown(triggerHeavyKey))
        {
            markerSystem.TriggerNextHeavyMarker();
        }

        if (Input.GetKeyDown(triggerPrimeKey))
        {
            markerSystem.TriggerNextPrimeMarker();
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
    public string GetModeActionErrorMessage(Enumerations.MarkerMode mode, string actionType)
    {
        if (actionType == "place")
        {
            switch (mode)
            {
                case Enumerations.MarkerMode.Light:
                    if (currentLightMarkerCharges <= 0)
                        return "No light marker charges available. Wait for cooldown.";
                    if (currentLightMarkers >= maxLightMarkerCharges)
                        return "Maximum light markers already placed on grid.";
                    if (lightMarkersPlaced > maxLightMarkers)
                        return "Light marker placement limit reached for this stage.";
                    break;

                case Enumerations.MarkerMode.Heavy:
                    if (currentHeavyMarkerCharges <= 0)
                        return "No heavy marker charges available. Wait for cooldown.";
                    if (currentHeavyMarkers >= maxHeavyMarkerCharges)
                        return "Maximum heavy markers already placed on grid.";
                    if (heavyMarkersPlaced > maxHeavyMarkers)
                        return "Heavy marker placement limit reached for this stage.";
                    break;

                case Enumerations.MarkerMode.Prime:
                    if (currentPrimeMarkerCharges <= 0)
                        return "No prime marker charges available. Wait for cooldown.";
                    if (currentPrimeMarkers >= primeMarkerOnGridLimit)
                        return "Maximum prime markers already placed on grid.";
                    if (primeMarkersPlaced > maxPrimeMarkers)
                        return "Prime marker placement limit reached for this stage.";
                    break;
            }
        }
        else if (actionType == "trigger")
        {
            switch (mode)
            {
                case Enumerations.MarkerMode.Light:
                    if (markerSystem.LightMarkers.Count == 0)
                        return "No light markers available to trigger.";
                    break;

                case Enumerations.MarkerMode.Heavy:
                    if (markerSystem.HeavyMarkers.Count == 0)
                        return "No heavy markers available to trigger.";
                    break;

                case Enumerations.MarkerMode.Prime:
                    if (markerSystem.PrimeMarkers.Count == 0)
                        return "No prime markers available to trigger.";
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
        TriggerAudioEvent(Enumerations.GameAudioEvent.ActionError, playerWorldPos, 0.7f);

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
        TriggerAudioEvent(Enumerations.GameAudioEvent.ActionSuccess, playerWorldPos, 0.8f);

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
    private void TriggerAudioEvent(Enumerations.GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
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
    private void TriggerInputFeedbackModeSwitch(Enumerations.MarkerMode previousMode, Enumerations.MarkerMode newMode, Vector2Int playerPosition)
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
    private void TriggerInputFeedbackMarkerPlace(Enumerations.MarkerMode markerMode, Vector2Int position, bool wasReplacement)
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
    private void TriggerInputFeedbackMarkerTrigger(Enumerations.MarkerMode markerMode, Vector2Int position, int targetCount)
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
    public Enumerations.MarkerMode GetCurrentMode()
    {
        return currentMarkerMode;
    }

    /// <summary>
    /// Sets the current marker mode with validation
    /// </summary>
    /// <param name="mode">The desired marker mode</param>
    /// <returns>True if mode was successfully changed, false otherwise</returns>
    public bool SetMode(Enumerations.MarkerMode mode)
    {
        if (!CanSwitchMode(mode))
        {
            if (EnableDebugLogs)
            {
                this.LogWarning($"Cannot switch to mode {mode} - validation failed", EnableDebugLogs);
            }
            return false;
        }

        Enumerations.MarkerMode previousMode = currentMarkerMode;
        currentMarkerMode = mode;

        // Update UI to reflect mode change
        if (actionUI != null)
        {
            // Force UI update to show new mode indicator
            UpdateUI();
        }

        if (EnableDebugLogs)
        {
            this.Log($"Mode switched from {previousMode} to {currentMarkerMode}", EnableDebugLogs);
        }

        return true;
    }

    /// <summary>
    /// Validates if switching to the specified mode is allowed
    /// </summary>
    /// <param name="mode">The mode to validate</param>
    /// <returns>True if mode switch is valid, false otherwise</returns>
    private bool CanSwitchMode(Enumerations.MarkerMode mode)
    {
        // Basic validation: ensure mode is defined
        if (!System.Enum.IsDefined(typeof(Enumerations.MarkerMode), mode))
        {
            return false;
        }

        // Allow switching to any valid mode (no restrictions for now)
        // Future restrictions could be added here (e.g., based on game state, unlocks, etc.)
        return true;
    }

    #endregion

    #region Charge Management

    public bool CanPlaceLightMarker()
    {
        return currentLightMarkerCharges > 0 &&
               currentLightMarkers < maxLightMarkerCharges &&
               lightMarkersPlaced <= maxLightMarkers;
    }

    public bool CanPlaceHeavyMarker()
    {
        return currentHeavyMarkerCharges > 0 &&
               currentHeavyMarkers < maxHeavyMarkerCharges &&
               heavyMarkersPlaced <= maxHeavyMarkers;
    }

    public bool CanPlacePrimeMarker()
    {
        return currentPrimeMarkerCharges > 0 &&
               currentPrimeMarkers < primeMarkerOnGridLimit &&
               primeMarkersPlaced <= maxPrimeMarkers;
    }

    public void ConsumeLightCharge()
    {
        currentLightMarkerCharges--;
        lastLightMarkerTime = Time.time;
        currentLightMarkers++;
        lightMarkersPlaced++;

        // Trigger audio event for light marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(Enumerations.GameAudioEvent.LightMarkerPlaced, worldPosition);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "light");
        }
    }

    public void ConsumeHeavyCharge()
    {
        currentHeavyMarkerCharges--;
        lastHeavyMarkerTime = Time.time;
        currentHeavyMarkers++;
        heavyMarkersPlaced++;

        // Trigger audio event for heavy marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(Enumerations.GameAudioEvent.HeavyMarkerPlaced, worldPosition);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "heavy");
        }
    }

    public void ConsumePrimeCharge()
    {
        currentPrimeMarkerCharges--;
        lastPrimeMarkerTime = Time.time;
        currentPrimeMarkers++;
        primeMarkersPlaced++;

        // Trigger audio event for prime marker placement
        Vector3 worldPosition = GetWorldPositionForAudio(playerManager.currentTilePosition);
        TriggerAudioEvent(Enumerations.GameAudioEvent.PrimeMarkerPlaced, worldPosition);

        UpdateUI();

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(false);
        }
        
        // Notify statistics manager
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMarkerPlaced(playerManager.currentTilePosition, "prime");
        }
    }

    public void ReleaseLightMarker()
    {
        currentLightMarkers--;
    }

    public void ReleaseHeavyMarker()
    {
        currentHeavyMarkers--;
    }

    public void ReleasePrimeMarker()
    {
        currentPrimeMarkers--;
    }

    private void RegenerateCharges()
    {
        bool chargesChanged = false;
        chargesChanged |= RegenerateLightCharges();
        chargesChanged |= RegenerateHeavyCharges();
        chargesChanged |= RegeneratePrimeCharges();

        if (chargesChanged)
        {
            UpdateUI();
        }
    }

    private bool RegenerateLightCharges()
    {
        if (currentLightMarkerCharges < maxLightMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastLightMarkerTime;
            if (timeSinceLastUse >= lightMarkerCooldown)
            {
                currentLightMarkerCharges++;
                lastLightMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(Enumerations.GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Light marker charge");
                
                this.Log($"Light marker charge regenerated. Charges: {currentLightMarkerCharges}/{maxLightMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private bool RegenerateHeavyCharges()
    {
        if (currentHeavyMarkerCharges < maxHeavyMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastHeavyMarkerTime;
            if (timeSinceLastUse >= heavyMarkerCooldown)
            {
                currentHeavyMarkerCharges++;
                lastHeavyMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(Enumerations.GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Heavy marker charge");
                
                this.Log($"Heavy marker charge regenerated. Charges: {currentHeavyMarkerCharges}/{maxHeavyMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private bool RegeneratePrimeCharges()
    {
        if (currentPrimeMarkerCharges < maxPrimeMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastPrimeMarkerTime;
            if (timeSinceLastUse >= primeMarkerCooldown)
            {
                currentPrimeMarkerCharges++;
                lastPrimeMarkerTime = Time.time;
                
                // Trigger audio event for resource regeneration
                Vector3 playerWorldPos = GetWorldPositionForAudio(playerManager.currentTilePosition);
                TriggerAudioEvent(Enumerations.GameAudioEvent.ResourceRegeneration, playerWorldPos, 0.8f);
                
                // Trigger animation for resource regeneration
                TriggerAnimationResourceRegeneration(playerWorldPos, "Prime marker charge");
                
                this.Log($"Prime marker charge regenerated. Charges: {currentPrimeMarkerCharges}/{maxPrimeMarkerCharges}", EnableDebugLogs);
                return true;
            }
        }
        return false;
    }

    private void UpdateUI()
    {
        if (actionUI != null)
        {
            actionUI.UpdateCharges(currentLightMarkerCharges, currentHeavyMarkerCharges, 
                                 currentPrimeMarkerCharges, GetCurrentCubeMarkers());
            actionUI.UpdateCooldowns(lightMarkerCooldown, heavyMarkerCooldown, 
                                   primeMarkerCooldown, 1f);
        }
    }

    #endregion

    #region Public API (Delegates to MarkerSystem)

    // Next charge time for UI system
    public float GetNextLightChargeTime() =>
        currentLightMarkerCharges < maxLightMarkerCharges ?
        lastLightMarkerTime + lightMarkerCooldown : Time.time;
    public float GetNextHeavyChargeTime() =>
        currentHeavyMarkerCharges < maxHeavyMarkerCharges ?
        lastHeavyMarkerTime + heavyMarkerCooldown : Time.time;
    public float GetNextPrimeChargeTime() =>
        currentPrimeMarkerCharges < maxPrimeMarkerCharges ?
        lastPrimeMarkerTime + primeMarkerCooldown : Time.time;

    // Light marker methods
    public bool PlaceLightMarker(Vector2Int position) => markerSystem.PlaceLightMarker(position);
    public bool RemoveLightMarkerAt(Vector2Int position) => markerSystem.RemoveLightMarkerAt(position);
    public bool HasLightMarkerAt(Vector2Int position) => markerSystem.HasLightMarkerAt(position);
    public bool TriggerNextLightMarker() => markerSystem.TriggerNextLightMarker();

    // Heavy marker methods
    public bool PlaceHeavyMarker(Vector2Int position) => markerSystem.PlaceHeavyMarker(position);
    public bool RemoveHeavyMarkerAt(Vector2Int position) => markerSystem.RemoveHeavyMarkerAt(position);
    public bool HasHeavyMarkerAt(Vector2Int position) => markerSystem.HasHeavyMarkerAt(position);
    public bool TriggerNextHeavyMarker() => markerSystem.TriggerNextHeavyMarker();

    // Prime marker methods
    public bool PlacePrimeMarker(Vector2Int centerPosition, int size) => markerSystem.PlacePrimeMarker(centerPosition, size);
    public bool RemovePrimeMarkerAt(Vector2Int centerPosition) => markerSystem.RemovePrimeMarkerAt(centerPosition);
    public bool HasPrimeMarkerAt(Vector2Int centerPosition) => markerSystem.HasPrimeMarkerAt(centerPosition);
    public bool TriggerNextPrimeMarker() => markerSystem.TriggerNextPrimeMarker();

    public void CreateCubeMarker(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type = PlayerMarkerSystem.CubeMarkerType.Prime) => markerSystem.CreateCubeMarker(position, type);
    public bool TriggerNextCubeMarker() => markerSystem.TriggerNextCubeMarker();
    public bool PowerUpNextCubeMarker() => markerSystem.PowerUpNextCubeMarker();

    public void ClearAllActions() => markerSystem.ClearAllActions();

    // Direct queue access for debugging
    public Queue<LightMarker> lightMarkers => markerSystem.LightMarkers;
    public Queue<HeavyMarker> heavyMarkers => markerSystem.HeavyMarkers;
    public Queue<PrimeMarker> primeMarkers => markerSystem.PrimeMarkers;


    
    


    #endregion

    #region Public Information Methods

    // Resource availability checks
    public bool CanPlaceLightMarkerCheck() => CanPlaceLightMarker();
    public bool CanPlaceHeavyMarkerCheck() => CanPlaceHeavyMarker();
    public bool CanPlacePrimeMarkerCheck() => CanPlacePrimeMarker();

    // Cooldown information
    public float GetLightMarkerCooldownRemaining()
    {
        if (currentLightMarkerCharges >= maxLightMarkerCharges)
            return 0f;
        return Mathf.Max(0f, lightMarkerCooldown - (Time.time - lastLightMarkerTime));
    }

    public float GetHeavyMarkerCooldownRemaining()
    {
        if (currentHeavyMarkerCharges >= maxHeavyMarkerCharges)
            return 0f;
        return Mathf.Max(0f, heavyMarkerCooldown - (Time.time - lastHeavyMarkerTime));
    }

    public float GetPrimeMarkerCooldownRemaining()
    {
        if (currentPrimeMarkerCharges >= maxPrimeMarkerCharges)
            return 0f;
        return Mathf.Max(0f, primeMarkerCooldown - (Time.time - lastPrimeMarkerTime));
    }

    // Statistics
    public int GetLightMarkersPlaced() => lightMarkersPlaced;
    public int GetHeavyMarkersPlaced() => heavyMarkersPlaced;
    public int GetPrimeMarkersPlaced() => primeMarkersPlaced;
    public int GetCubeMarkersTriggered() => cubeMarkersTriggered;
    public int GetPerfectTimingHits() => perfectTimingHits;

    // Current state information
    public int GetCurrentLightMarkers() => currentLightMarkers;
    public int GetCurrentHeavyMarkers() => currentHeavyMarkers;
    public int GetCurrentPrimeMarkers() => currentPrimeMarkers;
    public int GetCurrentCubeMarkers() => markerSystem?.cubeMarkers?.Count ?? 0;
    public int GetCurrentLightCharges() => currentLightMarkerCharges;
    public int GetCurrentHeavyCharges() => currentHeavyMarkerCharges;
    public int GetCurrentPrimeCharges() => currentPrimeMarkerCharges;



    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs { get; set; } = true;

    public string GetDebugStatus()
    {
        return $"PlayerAction: Light:{currentLightMarkerCharges}/{maxLightMarkerCharges} Heavy:{currentHeavyMarkerCharges}/{maxHeavyMarkerCharges} Prime:{currentPrimeMarkerCharges}/{maxPrimeMarkerCharges} OnGrid:{currentLightMarkers}+{currentHeavyMarkers}+{currentPrimeMarkers} Cube:{GetCurrentCubeMarkers()}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Current Marker Mode"] = currentMarkerMode.ToString(),
            ["Light Markers Placed"] = lightMarkersPlaced,
            ["Current Light Markers"] = currentLightMarkers,
            ["Light Charges"] = $"{currentLightMarkerCharges}/{maxLightMarkerCharges}",
            ["Light Cooldown Remaining"] = GetLightMarkerCooldownRemaining(),
            ["Heavy Markers Placed"] = heavyMarkersPlaced,
            ["Current Heavy Markers"] = currentHeavyMarkers,
            ["Heavy Charges"] = $"{currentHeavyMarkerCharges}/{maxHeavyMarkerCharges}",
            ["Heavy Cooldown Remaining"] = GetHeavyMarkerCooldownRemaining(),
            ["Prime Markers Placed"] = primeMarkersPlaced,
            ["Current Prime Markers"] = currentPrimeMarkers,
            ["Prime Charges"] = $"{currentPrimeMarkerCharges}/{maxPrimeMarkerCharges}",
            ["Prime Cooldown Remaining"] = GetPrimeMarkerCooldownRemaining(),
            ["Cube Markers Active"] = GetCurrentCubeMarkers(),
            ["Perfect Timing Hits"] = perfectTimingHits,
            ["Cube Markers Triggered"] = cubeMarkersTriggered,
            ["Input Enabled"] = inputEnabled,
            ["Can Place Light"] = CanPlaceLightMarker(),
            ["Can Place Heavy"] = CanPlaceHeavyMarker(),
            ["Can Place Prime"] = CanPlacePrimeMarker()
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

    public void ResetToDefaults()
    {
        // Reset charges to max
        currentLightMarkerCharges = maxLightMarkerCharges;
        currentHeavyMarkerCharges = maxHeavyMarkerCharges;
        currentPrimeMarkerCharges = maxPrimeMarkerCharges;
        
        // Reset marker mode
        currentMarkerMode = Enumerations.MarkerMode.Light;
        
        // Clear all markers
        if (markerSystem != null)
        {
            markerSystem.ClearAllActions();
        }
        
        // Reset counters
        lightMarkersPlaced = 0;
        heavyMarkersPlaced = 0;
        primeMarkersPlaced = 0;
        currentLightMarkers = 0;
        currentHeavyMarkers = 0;
        currentPrimeMarkers = 0;
        cubeMarkersTriggered = 0;
        perfectTimingHits = 0;
        
        // Reset timers
        lastLightMarkerTime = 0f;
        lastHeavyMarkerTime = 0f;
        lastPrimeMarkerTime = 0f;
        
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