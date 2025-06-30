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
    [SerializeField] private KeyCode powerUpCubeMarkerKey = KeyCode.E;
    


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
        InitializeReferences();
        InitializeCharges();

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
            
        ValidateAudioManager();
    }

    private void ValidateAudioManager()
    {
        if (audioManager == null)
        {
            Debug.LogWarning("[PlayerActionManager] AudioManager not found! Audio events will not be triggered.");
        }
        else if (!audioManager.IsInitialized)
        {
            Debug.LogWarning("[PlayerActionManager] AudioManager found but not initialized. Audio events may not work correctly.");
        }
    }

    private void InitializeCharges()
    {
        currentLightMarkerCharges = maxLightMarkerCharges;
        currentHeavyMarkerCharges = maxHeavyMarkerCharges;
        currentPrimeMarkerCharges = maxPrimeMarkerCharges;
        inputEnabled = true;
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

        HandleLightMarkerInput();
        HandleHeavyMarkerInput();
        HandlePrimeMarkerInput();
        HandleTriggerInputs();
        HandleCubeMarkerInputs();
    }

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

    private void HandleTriggerInputs()
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
            markerSystem.TriggerNextCubeMarker();
        }

        if (Input.GetKeyDown(powerUpCubeMarkerKey))
        {
            markerSystem.PowerUpNextCubeMarker();
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
                Debug.Log($"[PlayerActionManager] Triggered audio event: {eventType} at position {worldPosition} with intensity {intensity:F2}");
            }
        }
        else if (EnableDebugLogs)
        {
            Debug.LogWarning($"[PlayerActionManager] Cannot trigger audio event {eventType} - AudioManager not available or initialized");
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
                
                Debug.Log($"Light marker charge regenerated. Charges: {currentLightMarkerCharges}/{maxLightMarkerCharges}");
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
                
                Debug.Log($"Heavy marker charge regenerated. Charges: {currentHeavyMarkerCharges}/{maxHeavyMarkerCharges}");
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
                
                Debug.Log($"Prime marker charge regenerated. Charges: {currentPrimeMarkerCharges}/{maxPrimeMarkerCharges}");
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

    public bool EnableDebugLogs { get; set; } = false;

    public string GetDebugStatus()
    {
        return $"PlayerAction: Light:{currentLightMarkerCharges}/{maxLightMarkerCharges} Heavy:{currentHeavyMarkerCharges}/{maxHeavyMarkerCharges} Prime:{currentPrimeMarkerCharges}/{maxPrimeMarkerCharges} OnGrid:{currentLightMarkers}+{currentHeavyMarkers}+{currentPrimeMarkers} Cube:{GetCurrentCubeMarkers()}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
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
    }

    public void ResetToDefaults()
    {
        // Reset charges to max
        currentLightMarkerCharges = maxLightMarkerCharges;
        currentHeavyMarkerCharges = maxHeavyMarkerCharges;
        currentPrimeMarkerCharges = maxPrimeMarkerCharges;
        
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
            Debug.Log("[PlayerActionManager] Reset to defaults completed");
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading from ScriptableObject or JSON
        if (EnableDebugLogs)
            Debug.Log($"[PlayerActionManager] Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving to ScriptableObject or JSON
        if (EnableDebugLogs)
            Debug.Log($"[PlayerActionManager] Saving configuration: {configName} (not yet implemented)");
    }

    #endregion
}