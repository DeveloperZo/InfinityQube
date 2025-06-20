using UnityEngine;
using System.Collections.Generic;
using System;
using static PlayerActionManager;
using static UnityEditor.PlayerSettings;

public class PlayerActionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerActionUI actionUI;

    [Header("Individual Marker Settings")]
    [SerializeField] public int maxIndividualMarkers = 3;
    [SerializeField] public int individualMarkersPlaced = 0;
    [SerializeField] public int currentIndividualMarkers = 0;
    [SerializeField] public int maxIndividualMarkerCharges = 2;
    [SerializeField] private int currentIndividualMarkerCharges;
    [SerializeField] public float individualMarkerCooldown = 2f;
    [SerializeField] public Material individualMarkerMaterial;
    [SerializeField] public float lastIndividualMarkerTime = 0f;

    [Header("Area Marker Settings")]
    [SerializeField] public int maxAreaMarkers = 2;
    [SerializeField] public int areaMarkersPlaced = 0;
    [SerializeField] public int currentAreaMarkers = 0;
    [SerializeField] public int maxAreaMarkerCharges = 1;
    [SerializeField] private int currentAreaMarkerCharges;
    [SerializeField] public float areaMarkerCooldown = 4f;
    [SerializeField] public float lastAreaMarkerTime = 0f;
    [SerializeField] public int areaMarkerSize = 2;
    [SerializeField] public Material areaMarkerMaterial;

    [Header("Input Settings")]
    [SerializeField] private KeyCode individualMarkerKey = KeyCode.F;
    [SerializeField] private KeyCode areaMarkerKey = KeyCode.G;
    [SerializeField] private KeyCode triggerIndividualKey = KeyCode.R;
    [SerializeField] private KeyCode triggerAreaKey = KeyCode.T;
    [SerializeField] private KeyCode triggerCubeMarkerKey = KeyCode.Q;
    [SerializeField] private KeyCode powerUpCubeMarkerKey = KeyCode.E;

    // Statistics
    private int cubeMarkersTriggered = 0;
    private int perfectTimingHits = 0;
    private bool inputEnabled = false;

    // References to child system
    private PlayerMarkerSystem markerSystem;

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
    }

    private void InitializeCharges()
    {
        currentIndividualMarkerCharges = maxIndividualMarkerCharges;
        currentAreaMarkerCharges = maxAreaMarkerCharges;
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

        HandleIndividualMarkerInput();
        HandleAreaMarkerInput();
        HandleTriggerInputs();
        HandleCubeMarkerInputs();
    }

    private void HandleIndividualMarkerInput()
    {
        if (Input.GetKeyDown(individualMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (markerSystem.HasIndividualMarkerAt(playerPos))
            {
                markerSystem.RemoveIndividualMarkerAt(playerPos);
            }
            else if (CanPlaceIndividualMarker())
            {
                markerSystem.PlaceIndividualMarker(playerPos);
            }
        }
    }

    private void HandleAreaMarkerInput()
    {
        if (Input.GetKeyDown(areaMarkerKey))
        {
            Vector2Int playerPos = playerManager.currentTilePosition;

            if (markerSystem.HasAreaMarkerAt(playerPos))
            {
                markerSystem.RemoveAreaMarkerAt(playerPos);
            }
            else if (CanPlaceAreaMarker())
            {
                markerSystem.PlaceAreaMarker(playerPos, areaMarkerSize);
            }
        }
    }

    private void HandleTriggerInputs()
    {
        if (Input.GetKeyDown(triggerIndividualKey))
        {
            markerSystem.TriggerNextIndividualMarker();
        }

        if (Input.GetKeyDown(triggerAreaKey))
        {
            markerSystem.TriggerNextAreaMarker();
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

    #region Charge Management

    public bool CanPlaceIndividualMarker()
    {
        return currentIndividualMarkerCharges > 0 &&
               currentIndividualMarkers < maxIndividualMarkerCharges &&
               individualMarkersPlaced <= maxIndividualMarkers;
    }

    public bool CanPlaceAreaMarker()
    {
        return currentAreaMarkerCharges > 0 &&
               currentAreaMarkers < maxAreaMarkerCharges &&
               areaMarkersPlaced <= maxAreaMarkers;
    }

    public void ConsumeIndividualCharge()
    {
        currentIndividualMarkerCharges--;
        lastIndividualMarkerTime = Time.time;
        currentIndividualMarkers++;
        individualMarkersPlaced++;

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(true);
        }
    }

    public void ConsumeAreaCharge()
    {
        currentAreaMarkerCharges--;
        lastAreaMarkerTime = Time.time;
        currentAreaMarkers++;
        areaMarkersPlaced++;

        if (actionUI != null)
        {
            actionUI.OnMarkerPlaced(false);
        }
    }

    public void ReleaseIndividualMarker()
    {
        currentIndividualMarkers--;
    }

    public void ReleaseAreaMarker()
    {
        currentAreaMarkers--;
    }

    private void RegenerateCharges()
    {
        RegenerateIndividualCharges();
        RegenerateAreaCharges();
    }

    private void RegenerateIndividualCharges()
    {
        if (currentIndividualMarkerCharges < maxIndividualMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastIndividualMarkerTime;
            if (timeSinceLastUse >= individualMarkerCooldown)
            {
                currentIndividualMarkerCharges++;
                lastIndividualMarkerTime = Time.time;
                Debug.Log($"Individual marker charge regenerated. Charges: {currentIndividualMarkerCharges}/{maxIndividualMarkerCharges}");
            }
        }
    }

    private void RegenerateAreaCharges()
    {
        if (currentAreaMarkerCharges < maxAreaMarkerCharges)
        {
            float timeSinceLastUse = Time.time - lastAreaMarkerTime;
            if (timeSinceLastUse >= areaMarkerCooldown)
            {
                currentAreaMarkerCharges++;
                lastAreaMarkerTime = Time.time;
                Debug.Log($"Area marker charge regenerated. Charges: {currentAreaMarkerCharges}/{maxAreaMarkerCharges}");
            }
        }
    }

    #endregion

    #region Public API (Delegates to MarkerSystem)

    public bool PlaceIndividualMarker(Vector2Int position) => markerSystem.PlaceIndividualMarker(position);
    public bool RemoveIndividualMarkerAt(Vector2Int position) => markerSystem.RemoveIndividualMarkerAt(position);
    public bool HasIndividualMarkerAt(Vector2Int position) => markerSystem.HasIndividualMarkerAt(position);
    public bool TriggerNextIndividualMarker() => markerSystem.TriggerNextIndividualMarker();

    public bool PlaceAreaMarker(Vector2Int centerPosition, int size) => markerSystem.PlaceAreaMarker(centerPosition, size);
    public bool RemoveAreaMarkerAt(Vector2Int centerPosition) => markerSystem.RemoveAreaMarkerAt(centerPosition);
    public bool HasAreaMarkerAt(Vector2Int centerPosition) => markerSystem.HasAreaMarkerAt(centerPosition);
    public bool TriggerNextAreaMarker() => markerSystem.TriggerNextAreaMarker();

    public void CreateCubeMarker(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type = PlayerMarkerSystem.CubeMarkerType.Area) => markerSystem.CreateCubeMarker(position, type);
    public bool TriggerNextCubeMarker() => markerSystem.TriggerNextCubeMarker();
    public bool PowerUpNextCubeMarker() => markerSystem.PowerUpNextCubeMarker();

    public void ClearAllActions() => markerSystem.ClearAllActions();

    // Direct queue access for debugging
    public Queue<PlayerMarkerSystem.IndividualMarker> individualMarkers => markerSystem.IndividualMarkers;
    public Queue<PlayerMarkerSystem.AreaMarker> areaMarkers => markerSystem.AreaMarkers;

    #endregion

    #region Public Information Methods

    // Resource availability checks
    public bool CanPlaceIndividualMarkerCheck() => CanPlaceIndividualMarker();
    public bool CanPlaceAreaMarkerCheck() => CanPlaceAreaMarker();

    // Cooldown information
    public float GetIndividualMarkerCooldownRemaining() =>
        Mathf.Max(0f, individualMarkerCooldown - (Time.time - lastIndividualMarkerTime));
    public float GetAreaMarkerCooldownRemaining() =>
        Mathf.Max(0f, areaMarkerCooldown - (Time.time - lastAreaMarkerTime));

    // Statistics
    public int GetIndividualMarkersPlaced() => individualMarkersPlaced;
    public int GetAreaMarkersPlaced() => areaMarkersPlaced;
    public int GetCubeMarkersTriggered() => markerSystem.GetCubeMarkersTriggered();
    public int GetPerfectTimingHits() => markerSystem.GetPerfectTimingHits();

    public int GetCurrentIndividualMarkers() => currentIndividualMarkers;
    public int GetCurrentAreaMarkers() => currentAreaMarkers;
    public int GetCurrentCubeMarkers() => markerSystem.GetCurrentCubeMarkers();

    public void ResetStatistics()
    {
        individualMarkersPlaced = 0;
        areaMarkersPlaced = 0;
        markerSystem.ResetStatistics();
    }

    #endregion

    #region Legacy Support Methods (for debugging)

    // Keep some legacy methods for compatibility with existing debug panels
    public int CubeMarkerCount => markerSystem.GetCurrentCubeMarkers();
    public bool HasCubeMarkers() => markerSystem.GetCurrentCubeMarkers() > 0;
    public Vector2Int GetNextCubeMarker() => markerSystem.GetNextCubeMarker();

    #endregion

    #region Perfect Timing

    public void IncrementPerfectTimingHits()
    {
        perfectTimingHits++;
    }

    public void IncrementCubeMarkersTriggered()
    {
        cubeMarkersTriggered++;
    }

    #endregion
}

