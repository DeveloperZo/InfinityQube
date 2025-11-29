using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Linq;
using static Enumerations;

public class PlayerStatisticsManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Core References")]
    public PlayerManager playerManager;
    public WaveManager waveManager;
    public PlayerActionManager playerActionManager;
    public GridManager gridManager;
    
    [Header("Data Collection Settings")]
    public bool enableStatisticsCollection = true;
    public bool enableDetailedTracking = true;
    public bool autoSaveOnCompletion = true;
    public bool emergencySaveOnQuit = true;
    
    [Header("Debug & Testing")]
    public bool showCollectionStatus = false;
    #endregion

    #region Runtime State
    // Current Session Data
    private PlayerSessionData currentSession;
    private bool isCollecting = false;
    private float sessionStartTime;
    private float lastPositionRecordTime;
    private Vector2Int lastRecordedPosition;
    
    // Tracking Variables
    private Dictionary<Vector2Int, int> tileVisitCounts = new Dictionary<Vector2Int, int>();
    private List<MessageInteractionEvent> activeMessageEvents = new List<MessageInteractionEvent>();
    private Dictionary<string, MarkerPlacementEvent> activeMarkers = new Dictionary<string, MarkerPlacementEvent>();
    
    // Advanced Analytics Tracking
    private Dictionary<string, float> strategicDecisionTimes = new Dictionary<string, float>();
    private List<FacePaintingEvent> facePaintingEvents = new List<FacePaintingEvent>();
    private float sessionAPM = 0f;
    private int totalActions = 0;
    private float lastActionTime = 0f;
    private List<float> recentActionTimes = new List<float>();
    private Dictionary<string, int> actionCounts = new Dictionary<string, int>();
    private float lastDecisionStartTime = 0f;
    private string currentDecisionType = "";
    
    // Performance Optimization
    private float positionTrackingInterval = 0.1f;
    private int maxPositionHistory = 1000;
    private float apmCalculationInterval = 5f; // Calculate APM every 5 seconds
    private float lastAPMCalculationTime = 0f;
    #endregion

    #region Singleton Pattern
    private static PlayerStatisticsManager _instance;
    public static PlayerStatisticsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerStatisticsManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerStatisticsManager");
                    _instance = go.AddComponent<PlayerStatisticsManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        EnableDebugLogs = true;
        InitializeManager();
    }
    
    private void Update()
    {
        if (isCollecting && enableStatisticsCollection)
        {
            UpdateDataCollection();
        }
    }
    
    private void OnDestroy()
    {
        CleanupManager();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && emergencySaveOnQuit && currentSession != null)
        {
            PerformEmergencySave();
        }
    }
    
    private void OnApplicationQuit()
    {
        DebugLog("🚪 Application quitting - preparing final save");
        
        // Always save when quitting, regardless of settings when not in editor
        if ((emergencySaveOnQuit || !Application.isEditor) && currentSession != null)
        {
            // Calculate final duration
            float currentTime = Time.time;
            float calculatedDuration = currentTime - sessionStartTime;
            currentSession.sessionDuration = Mathf.Max(0f, calculatedDuration);
            
            DebugLog($"💾 Final save: {calculatedDuration:F2}s duration, {currentSession.movementData.totalMoves} moves");
            
            // Mark as completed if we have meaningful data
            if (currentSession.sessionDuration > 10f || currentSession.movementData.totalMoves > 5)
            {
                FinalizeSessionData();
                currentSession.isCompleted = true;
            }
            else
            {
                // For very short sessions, still save but mark as incomplete
                currentSession.isCompleted = false;
            }
            
            PerformEmergencySave();
        }
    }
    #endregion

    #region Initialization & Cleanup
    private void InitializeManager()
    {
        FindManagerReferences();
        SetupEventSubscriptions();
        
        // Auto-start session for demo
        if (enableStatisticsCollection)
        {
            InitializeNewSession("Demo");
        }
        
        DebugLog("✅ PlayerStatisticsManager Initialized");
    }
    
    private void FindManagerReferences()
    {
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerManager>();
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();
        if (playerActionManager == null)
            playerActionManager = FindObjectOfType<PlayerActionManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
            
        ValidateReferences();
    }
    
    private void ValidateReferences()
    {
        if (playerManager == null)
            Debug.LogWarning("[PlayerStatisticsManager] PlayerManager reference missing - some features limited");
        if (waveManager == null)
            Debug.LogWarning("[PlayerStatisticsManager] WaveManager reference missing - wave tracking limited");
        if (playerActionManager == null)
            Debug.LogWarning("[PlayerStatisticsManager] PlayerActionManager reference missing - marker tracking limited");
        if (gridManager == null)
            Debug.LogWarning("[PlayerStatisticsManager] GridManager reference missing - position tracking limited");
    }
    
    private void SetupEventSubscriptions()
    {
        if (playerManager != null)
        {
            playerManager.OnPlayerDied += OnPlayerDied;
            playerManager.OnPlayerRespawned += OnPlayerRespawned;
            playerManager.OnStatisticsUpdated += OnPlayerStatisticsUpdated;
        }
    }
    
    private void CleanupManager()
    {
        if (playerManager != null)
        {
            playerManager.OnPlayerDied -= OnPlayerDied;
            playerManager.OnPlayerRespawned -= OnPlayerRespawned;
            playerManager.OnStatisticsUpdated -= OnPlayerStatisticsUpdated;
        }
        
        if (isCollecting && autoSaveOnCompletion)
        {
            CompleteCurrentSession();
        }
    }
    #endregion

    #region Session Management
    public void StartNewSession(string stageName = "Demo")
    {
        if (isCollecting)
        {
            CompleteCurrentSession();
        }
        
        InitializeNewSession(stageName);
        DebugLog($"🆕 New session started: {currentSession.sessionId}");
    }
    
    private void InitializeNewSession(string stageName = "Demo")
    {
        currentSession = new PlayerSessionData();
        currentSession.stageName = stageName;
        currentSession.gameVersion = GetGameVersion();
        
        sessionStartTime = Time.time;
        lastPositionRecordTime = 0f;
        lastRecordedPosition = Vector2Int.zero;
        
        tileVisitCounts.Clear();
        activeMessageEvents.Clear();
        activeMarkers.Clear();
        
        // Clear advanced analytics tracking
        strategicDecisionTimes.Clear();
        facePaintingEvents.Clear();
        sessionAPM = 0f;
        totalActions = 0;
        lastActionTime = 0f;
        recentActionTimes.Clear();
        actionCounts.Clear();
        lastDecisionStartTime = 0f;
        currentDecisionType = "";
        lastAPMCalculationTime = 0f;
        
        isCollecting = true;
        
        RecordInitialState();
        
        DebugLog($"📊 Session initialized at time {sessionStartTime}, ID: {currentSession.sessionId}");
    }
    
    public void CompleteCurrentSession()
    {
        if (!isCollecting || currentSession == null) return;
        
        FinalizeSessionData();
        SaveSessionData();
        
        isCollecting = false;
        DebugLog($"✅ Session completed: {currentSession.sessionId}");
    }
    
    // Helper class for face painting tracking
    [System.Serializable]
    public class FacePaintingEvent
    {
        public float timestamp;
        public Vector2Int cubePosition;
        public string faceDirection;
        public bool wasSuccessful;
        
        public FacePaintingEvent(float time, Vector2Int pos, string face)
        {
            timestamp = time;
            cubePosition = pos;
            faceDirection = face;
            wasSuccessful = false;
        }
    }
    
    private void FinalizeSessionData()
    {
        float currentTime = Time.time;
        float calculatedDuration = currentTime - sessionStartTime;
        
        // Ensure duration is never negative
        currentSession.sessionDuration = Mathf.Max(0f, calculatedDuration);
        currentSession.isCompleted = true;
        
        DebugLog($"🕐 Session duration calculated: {currentSession.sessionDuration:F2}s (start: {sessionStartTime:F2}, end: {currentTime:F2})");
        
        // Calculate final movement data
        currentSession.movementData.CalculateDistanceTraveled();
        CalculateTileVisitStatistics();
        
        // Calculate marker efficiency
        CalculateMarkerEfficiency();
        
        // Calculate tutorial progress
        CalculateTutorialProgress();
        
        // Calculate advanced analytics
        CalculateAdvancedAnalytics();
        
        // Copy final player statistics
        if (playerManager != null)
        {
            currentSession.finalStats = playerManager.GetCurrentStatistics();
        }
        
        DebugLog($"📈 Final stats: {currentSession.movementData.totalMoves} moves, {currentSession.finalStats.playerDeaths} deaths");
    }
    #endregion

    #region Data Collection
    private void UpdateDataCollection()
    {
        TrackPlayerMovement();
        UpdateSessionTimer();
        UpdateAPMCalculation();
    }
    
    private void TrackPlayerMovement()
    {
        if (playerManager == null || !enableDetailedTracking) return;
        
        float currentTime = Time.time;
        if (currentTime - lastPositionRecordTime >= positionTrackingInterval)
        {
            Vector2Int currentPos = playerManager.currentTilePosition;
            
            if (currentPos != lastRecordedPosition || lastPositionRecordTime == 0f)
            {
                RecordPlayerPosition(currentPos, currentTime);
                lastRecordedPosition = currentPos;
                lastPositionRecordTime = currentTime;
            }
        }
    }
    
    private void RecordPlayerPosition(Vector2Int position, float time)
    {
        currentSession.movementData.RecordPosition(position, time);
        
        // Track tile visits
        string tileKey = $"{position.x},{position.y}";
        if (!tileVisitCounts.ContainsKey(position))
            tileVisitCounts[position] = 0;
        tileVisitCounts[position]++;
        
        // Record movement as an action for APM calculation
        RecordAction("movement");
        
        // Maintain position history limit
        if (currentSession.movementData.positionHistory.Count > maxPositionHistory)
        {
            currentSession.movementData.positionHistory.RemoveAt(0);
            currentSession.movementData.timeStamps.RemoveAt(0);
        }
    }
    
    private void UpdateSessionTimer()
    {
        // Timer updates handled in FinalizeSessionData
    }
    
    private void UpdateAPMCalculation()
    {
        float currentTime = Time.time;
        if (currentTime - lastAPMCalculationTime >= apmCalculationInterval)
        {
            CalculateAPM();
            lastAPMCalculationTime = currentTime;
        }
    }
    
    private void RecordInitialState()
    {
        if (playerManager != null)
        {
            RecordPlayerPosition(playerManager.currentTilePosition, Time.time);
        }
    }
    #endregion

    #region Event Handlers
    private void OnPlayerDied()
    {
        if (!isCollecting) return;
        
        Vector2Int deathPosition = playerManager?.currentTilePosition ?? Vector2Int.zero;
        var collisionEvent = new CubeCollisionEvent(deathPosition, Time.time, "Unknown", true);
        currentSession.cubeData.collisionEvents.Add(collisionEvent);
        
        RecordAction("death");
        DebugLog($"💀 Player death recorded at {deathPosition}");
    }
    
    private void OnPlayerRespawned()
    {
        if (!isCollecting) return;
        
        DebugLog("🔄 Player respawn recorded");
    }
    
    private void OnPlayerStatisticsUpdated(PlayerStatistics stats)
    {
        if (!isCollecting) return;
        
        // Statistics are automatically captured in FinalizeSessionData
        DebugLog("📊 Player statistics updated");
    }
    #endregion

    #region Marker Tracking
    public void OnMarkerPlaced(Vector2Int position, string markerType)
    {
        if (!isCollecting) return;
        
        var placementEvent = new MarkerPlacementEvent(position, Time.time, markerType);
        
        switch (markerType.ToLower())
        {
            case "light":
                currentSession.markerData.unitMarkerPlacements.Add(placementEvent);
                break;
            case "heavy":
                currentSession.markerData.RecursionMarkerPlacements.Add(placementEvent);
                break;
            case "prime":
                currentSession.markerData.primeMarkerPlacements.Add(placementEvent);
                break;
        }
        
        string markerKey = $"{markerType}_{position.x}_{position.y}";
        activeMarkers[markerKey] = placementEvent;
        
        // Add strategic analysis
        RecordStrategicDecision("marker_placement", position, markerType);
        RecordAction("marker_placement");
        
        DebugLog($"📍 {markerType} marker placed at {position}");
    }

    public void OnMarkerRemoved(Vector2Int position, string markerType)
    {
        if (!isCollecting) return;

        MarkerPlacementEvent placementEvent = null;

        switch (markerType.ToLower())
        {
            case "light":
                placementEvent = currentSession.markerData.unitMarkerPlacements.FirstOrDefault(x => x.position == position);
                if (placementEvent != null)
                {
                    currentSession.markerData.unitMarkerPlacements.Remove(placementEvent);
                }
                break;
            case "heavy":
                placementEvent = currentSession.markerData.RecursionMarkerPlacements.FirstOrDefault(x => x.position == position);
                if (placementEvent != null)
                {
                    currentSession.markerData.RecursionMarkerPlacements.Remove(placementEvent);
                }
                break;
            case "prime":
                placementEvent = currentSession.markerData.primeMarkerPlacements.FirstOrDefault(x => x.position == position);
                if (placementEvent != null)
                {
                    currentSession.markerData.primeMarkerPlacements.Remove(placementEvent);
                }
                break;
        }

        string markerKey = $"{markerType}_{position.x}_{position.y}";
        activeMarkers.Remove(markerKey);

        // Add strategic analysis
        RecordStrategicDecision("marker_removal", position, markerType);
        RecordAction("marker_removal");

        DebugLog($"🗑️ {markerType} marker removed at {position}");
    }

    public void OnMarkerTriggered(Vector2Int position, string markerType, bool hitTarget, int cubesAffected)
    {
        if (!isCollecting) return;
        
        var triggerEvent = new MarkerTriggerEvent(position, Time.time, markerType, hitTarget, cubesAffected);
        currentSession.markerData.triggerEvents.Add(triggerEvent);
        
        // Update corresponding placement event
        string markerKey = $"{markerType}_{position.x}_{position.y}";
        if (activeMarkers.ContainsKey(markerKey))
        {
            var placementEvent = activeMarkers[markerKey];
            placementEvent.wasTriggered = true;
            placementEvent.triggerTime = Time.time;
            triggerEvent.timeSincePlacement = Time.time - placementEvent.timestamp;
            
            activeMarkers.Remove(markerKey);
        }
        
        RecordAction("marker_trigger");
        DebugLog($"🎯 {markerType} marker triggered at {position}, hit: {hitTarget}, cubes: {cubesAffected}");
    }
    #endregion

    #region Cube Interaction Tracking
    public void OnCubeCaptured(Vector2Int position, string cubeType, string method = "marker")
    {
        if (!isCollecting) return;
        
        var captureEvent = new CubeInteractionEvent(position, Time.time, cubeType, method);
        currentSession.cubeData.captureEvents.Add(captureEvent);
        
        // Update cube type counts
        if (!currentSession.cubeData.cubeTypesCaptured.ContainsKey(cubeType))
            currentSession.cubeData.cubeTypesCaptured[cubeType] = 0;
        currentSession.cubeData.cubeTypesCaptured[cubeType]++;
        
        RecordAction("cube_capture");
        DebugLog($"📦 {cubeType} cube captured at {position} via {method}");
    }
    
    public void OnCubeEscaped(Vector2Int position, string cubeType)
    {
        if (!isCollecting) return;
        
        var escapeEvent = new CubeInteractionEvent(position, Time.time, cubeType, "escape");
        currentSession.cubeData.escapeEvents.Add(escapeEvent);
        
        // Update cube type counts
        if (!currentSession.cubeData.cubeTypesEscaped.ContainsKey(cubeType))
            currentSession.cubeData.cubeTypesEscaped[cubeType] = 0;
        currentSession.cubeData.cubeTypesEscaped[cubeType]++;
        
        DebugLog($"🏃 {cubeType} cube escaped from {position}");
    }
    
    public void OnCubeCollision(Vector2Int position, string cubeType, bool causedDeath)
    {
        if (!isCollecting) return;
        
        var collisionEvent = new CubeCollisionEvent(position, Time.time, cubeType, causedDeath);
        currentSession.cubeData.collisionEvents.Add(collisionEvent);
        
        DebugLog($"💥 Collision with {cubeType} cube at {position}, death: {causedDeath}");
    }
    #endregion

    #region Advanced Analytics Event Handlers
    public void OnFacePainted(Vector2Int cubePosition, CubeFace face, FaceStatus status)
    {
        if (!isCollecting) return;
        
        string faceDirection = ConvertCubeFaceToString(face);
        var paintingEvent = new FacePaintingEvent(Time.time, cubePosition, faceDirection);
        paintingEvent.wasSuccessful = (status != FaceStatus.None);
        
        facePaintingEvents.Add(paintingEvent);
        currentSession.facePaintingData.paintingEvents.Add(new CubePaintingEvent(Time.time, cubePosition, faceDirection, "paint"));
        
        // Update face painting statistics
        if (!currentSession.facePaintingData.facesPaintedByType.ContainsKey(faceDirection))
            currentSession.facePaintingData.facesPaintedByType[faceDirection] = 0;
        currentSession.facePaintingData.facesPaintedByType[faceDirection]++;
        
        currentSession.facePaintingData.totalFacesPainted++;
        
        RecordAction("face_painting");
        DebugLog($"🎨 Face painted: {faceDirection} at {cubePosition}, success: {paintingEvent.wasSuccessful}");
    }
    
    public void OnResourceWasted(string resourceType, float wasteAmount)
    {
        if (!isCollecting) return;
        
        if (!currentSession.resourceMetrics.wasteByCategory.ContainsKey(resourceType))
            currentSession.resourceMetrics.wasteByCategory[resourceType] = 0;
        currentSession.resourceMetrics.wasteByCategory[resourceType]++;
        
        if (resourceType == "marker")
            currentSession.resourceMetrics.wastedMarkers++;
            
        DebugLog($"🗑️ Resource wasted: {resourceType}, amount: {wasteAmount}");
    }
    
    public void OnSkillImprovement(string skillArea, float improvement)
    {
        if (!isCollecting) return;
        
        var skillMeasurement = new SkillMeasurement(Time.time, skillArea, improvement, "performance_based");
        currentSession.learningData.skillMeasurements.Add(skillMeasurement);
        
        // Update skill levels
        if (!currentSession.learningData.skillLevels.ContainsKey(skillArea))
            currentSession.learningData.skillLevels[skillArea] = 0f;
        
        float previousLevel = currentSession.learningData.skillLevels[skillArea];
        float newLevel = previousLevel + improvement;
        currentSession.learningData.skillLevels[skillArea] = newLevel;
        
        // Record improvement event if significant
        if (improvement > 0.1f)
        {
            var improvementEvent = new ImprovementEvent(Time.time, skillArea, previousLevel, newLevel);
            currentSession.learningData.improvementEvents.Add(improvementEvent);
        }
        
        DebugLog($"📈 Skill improvement: {skillArea}, +{improvement:F2} (now {newLevel:F2})");
    }
    #endregion
    
    #region Tutorial Progress Tracking
    public void OnMessageDisplayed(string messageContent, int moveStep)
    {
        if (!isCollecting) return;
        
        var messageEvent = new MessageInteractionEvent(messageContent, Time.time, moveStep);
        activeMessageEvents.Add(messageEvent);
        currentSession.tutorialData.messageEvents.Add(messageEvent);
        
        DebugLog($"💬 Tutorial message displayed at step {moveStep}");
    }
    
    public void OnMessageDismissed(string messageContent, bool wasSkipped)
    {
        if (!isCollecting) return;
        
        // Find and update the active message event
        var activeEvent = activeMessageEvents.LastOrDefault(e => e.messageContent == messageContent);
        if (activeEvent != null)
        {
            activeEvent.readTime = Time.time - activeEvent.displayTime;
            activeEvent.wasSkipped = wasSkipped;
            
            if (wasSkipped)
                currentSession.tutorialData.messagesSkipped++;
                
            activeMessageEvents.Remove(activeEvent);
        }
        
        DebugLog($"💬 Tutorial message dismissed, skipped: {wasSkipped}");
    }
    #endregion

    #region Data Analysis & Calculation
    private void CalculateTileVisitStatistics()
    {
        if (tileVisitCounts.Count == 0) return;
        
        var mostVisited = tileVisitCounts.OrderByDescending(kvp => kvp.Value).First();
        currentSession.movementData.mostVisitedTile = mostVisited.Key;
        currentSession.movementData.mostVisitedCount = mostVisited.Value;
        
        // Calculate time in grid areas (simplified)
        foreach (var visit in tileVisitCounts)
        {
            string areaKey = GetGridAreaKey(visit.Key);
            if (!currentSession.movementData.timeInGridAreas.ContainsKey(areaKey))
                currentSession.movementData.timeInGridAreas[areaKey] = 0f;
            currentSession.movementData.timeInGridAreas[areaKey] += visit.Value * positionTrackingInterval;
        }
    }
    
    private void CalculateMarkerEfficiency()
    {
        var allTriggers = currentSession.markerData.triggerEvents;
        if (allTriggers.Count == 0) return;
        
        float totalTimeBetween = allTriggers.Where(t => t.timeSincePlacement > 0)
                                           .Sum(t => t.timeSincePlacement);
        int validTriggers = allTriggers.Count(t => t.timeSincePlacement > 0);
        
        currentSession.markerData.averageTimeBetweenPlaceAndTrigger = 
            validTriggers > 0 ? totalTimeBetween / validTriggers : 0f;
            
        int successfulTriggers = allTriggers.Count(t => t.hitTarget);
        currentSession.markerData.triggerSuccessRate = 
            allTriggers.Count > 0 ? (float)successfulTriggers / allTriggers.Count : 0f;
    }
    
    private void CalculateTutorialProgress()
    {
        var messages = currentSession.tutorialData.messageEvents;
        if (messages.Count == 0) return;
        
        var readMessages = messages.Where(m => !m.wasSkipped && m.readTime > 0);
        if (readMessages.Any())
        {
            currentSession.tutorialData.averageReadTime = readMessages.Average(m => m.readTime);
        }
        
        currentSession.tutorialData.totalPauseTime = messages.Sum(m => m.readTime);
    }
    
    private void CalculateAdvancedAnalytics()
    {
        // Final APM calculation
        CalculateAPM();
        
        // Calculate average APM for the session
        if (currentSession.resourceMetrics.apmSamples.Count > 0)
        {
            currentSession.resourceMetrics.averageAPM = 
                currentSession.resourceMetrics.apmSamples.Average(s => s.apmValue);
        }
        
        // Calculate strategic decision analytics
        if (currentSession.strategicData.decisionEvents.Count > 0)
        {
            currentSession.strategicData.averageDecisionTime = 
                currentSession.strategicData.decisionEvents.Average(d => d.decisionTime);
                
            // Calculate time between decisions
            var sortedDecisions = currentSession.strategicData.decisionEvents
                .OrderBy(d => d.timestamp).ToList();
            if (sortedDecisions.Count > 1)
            {
                float totalTimeBetween = 0f;
                for (int i = 1; i < sortedDecisions.Count; i++)
                {
                    totalTimeBetween += sortedDecisions[i].timestamp - sortedDecisions[i - 1].timestamp;
                }
                currentSession.strategicData.timeBetweenDecisions = totalTimeBetween / (sortedDecisions.Count - 1);
            }
        }
        
        // Analyze face painting efficiency
        AnalyzeFacePaintingEfficiency();
        
        // Update learning progression
        UpdateLearningProgression();
        
        // Calculate resource efficiency metrics
        CalculateResourceEfficiency();
        
        DebugLog("📊 Advanced analytics calculations completed");
    }
    
    private void CalculateResourceEfficiency()
    {
        // Calculate optimal timing percentage
        var timingEvents = currentSession.resourceMetrics.timingEvents;
        if (timingEvents.Count > 0)
        {
            int perfectTimings = timingEvents.Count(t => t.wasPerfect);
            currentSession.resourceMetrics.optimalTimingPercentage = (float)perfectTimings / timingEvents.Count;
            currentSession.resourceMetrics.perfectTimingCount = perfectTimings;
            currentSession.resourceMetrics.poorTimingCount = timingEvents.Count - perfectTimings;
            
            // Calculate average reaction time
            currentSession.resourceMetrics.averageReactionTime = 
                timingEvents.Average(t => Mathf.Abs(t.actualTime - t.optimalTime));
        }
        
        // Count inefficient movements (simplified)
        if (currentSession.movementData.positionHistory.Count > 2)
        {
            int backAndForthMovements = 0;
            for (int i = 2; i < currentSession.movementData.positionHistory.Count; i++)
            {
                Vector2Int current = currentSession.movementData.positionHistory[i];
                Vector2Int previous = currentSession.movementData.positionHistory[i - 1];
                Vector2Int twoBefore = currentSession.movementData.positionHistory[i - 2];
                
                // Check if player moved back to where they were 2 moves ago
                if (current == twoBefore && current != previous)
                {
                    backAndForthMovements++;
                }
            }
            currentSession.resourceMetrics.inefficientMovements = backAndForthMovements;
        }
    }
    
    private string GetGridAreaKey(Vector2Int position)
    {
        // Simplified grid area classification
        if (gridManager == null) return "unknown";
        
        int width = gridManager.Width;
        int height = gridManager.Height;
        
        if (position.y < height / 3) return "bottom";
        if (position.y < 2 * height / 3) return "middle";
        return "top";
    }
    
    private string GetGameVersion()
    {
        return Application.version;
    }
    #endregion

    #region Advanced Analytics Calculations
    private void RecordStrategicDecision(string decisionType, Vector2Int position, string context)
    {
        if (!isCollecting) return;
        
        float currentTime = Time.time;
        
        // If we were tracking a decision, finalize it
        if (!string.IsNullOrEmpty(currentDecisionType) && lastDecisionStartTime > 0)
        {
            float decisionTime = currentTime - lastDecisionStartTime;
            var decisionEvent = new DecisionEvent(lastDecisionStartTime, currentDecisionType, decisionTime, context);
            currentSession.strategicData.decisionEvents.Add(decisionEvent);
            
            DebugLog($"⚡ Decision completed: {currentDecisionType} took {decisionTime:F2}s");
        }
        
        // Start tracking this new decision
        lastDecisionStartTime = currentTime;
        currentDecisionType = decisionType;
        
        if (!strategicDecisionTimes.ContainsKey(decisionType))
            strategicDecisionTimes[decisionType] = 0f;
    }
    
    private void RecordAction(string actionType)
    {
        if (!isCollecting) return;
        
        float currentTime = Time.time;
        totalActions++;
        lastActionTime = currentTime;
        
        // Track recent actions for APM calculation
        recentActionTimes.Add(currentTime);
        
        // Remove actions older than 1 minute
        recentActionTimes.RemoveAll(time => currentTime - time > 60f);
        
        // Update action counts
        if (!actionCounts.ContainsKey(actionType))
            actionCounts[actionType] = 0;
        actionCounts[actionType]++;
    }
    
    private void CalculateAPM()
    {
        if (!isCollecting || recentActionTimes.Count == 0) return;
        
        float currentTime = Time.time;
        
        // Count actions in the last minute
        int actionsInLastMinute = recentActionTimes.Count(time => currentTime - time <= 60f);
        sessionAPM = actionsInLastMinute;
        
        // Create APM sample
        string dominantAction = GetDominantActionType();
        var apmSample = new APMSample(currentTime, actionsInLastMinute, dominantAction);
        apmSample.apmValue = sessionAPM;
        currentSession.resourceMetrics.apmSamples.Add(apmSample);
        
        // Update peak APM
        if (sessionAPM > currentSession.resourceMetrics.peakAPM)
            currentSession.resourceMetrics.peakAPM = sessionAPM;
            
        currentSession.resourceMetrics.currentAPM = sessionAPM;
        
        DebugLog($"📊 APM calculated: {sessionAPM:F1} (peak: {currentSession.resourceMetrics.peakAPM:F1})");
    }
    
    private void AnalyzeFacePaintingEfficiency()
    {
        if (facePaintingEvents.Count == 0) return;
        
        int successfulPaints = facePaintingEvents.Count(e => e.wasSuccessful);
        currentSession.facePaintingData.paintingSuccessRate = (float)successfulPaints / facePaintingEvents.Count;
        
        // Calculate most painted face
        if (currentSession.facePaintingData.facesPaintedByType.Count > 0)
        {
            var mostPainted = currentSession.facePaintingData.facesPaintedByType
                .OrderByDescending(kvp => kvp.Value)
                .First();
            currentSession.facePaintingData.mostPaintedFace = mostPainted.Key;
            currentSession.facePaintingData.mostPaintedFaceCount = mostPainted.Value;
        }
        
        // Calculate success rate by face
        foreach (var faceGroup in facePaintingEvents.GroupBy(e => e.faceDirection))
        {
            string face = faceGroup.Key;
            int total = faceGroup.Count();
            int successful = faceGroup.Count(e => e.wasSuccessful);
            currentSession.facePaintingData.successRateByFace[face] = total > 0 ? (float)successful / total : 0f;
        }
        
        // Calculate average painting interval
        if (facePaintingEvents.Count > 1)
        {
            var sortedEvents = facePaintingEvents.OrderBy(e => e.timestamp).ToList();
            float totalInterval = 0f;
            for (int i = 1; i < sortedEvents.Count; i++)
            {
                totalInterval += sortedEvents[i].timestamp - sortedEvents[i - 1].timestamp;
            }
            currentSession.facePaintingData.averagePaintingInterval = totalInterval / (sortedEvents.Count - 1);
        }
    }
    
    private void UpdateLearningProgression()
    {
        // Calculate overall skill progression
        if (currentSession.learningData.skillLevels.Count > 0)
        {
            currentSession.learningData.overallSkillProgression = 
                currentSession.learningData.skillLevels.Values.Average();
        }
        
        // Calculate improvement rates
        foreach (var skill in currentSession.learningData.skillLevels.Keys)
        {
            var improvements = currentSession.learningData.improvementEvents
                .Where(e => e.skillName == skill)
                .OrderBy(e => e.timestamp)
                .ToList();
                
            if (improvements.Count > 1)
            {
                float totalImprovement = improvements.Sum(e => e.improvementAmount);
                float timeSpan = improvements.Last().timestamp - improvements.First().timestamp;
                currentSession.learningData.improvementRates[skill] = timeSpan > 0 ? totalImprovement / timeSpan : 0f;
            }
        }
        
        // Calculate overall improvement rate
        if (currentSession.learningData.improvementRates.Count > 0)
        {
            currentSession.learningData.overallImprovementRate = 
                currentSession.learningData.improvementRates.Values.Average();
        }
    }
    
    private string GetDominantActionType()
    {
        if (actionCounts.Count == 0) return "movement";
        
        return actionCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    }
    
    private string ConvertCubeFaceToString(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.Front: return "front";
            case CubeFace.Back: return "back";
            case CubeFace.Top: return "top";
            case CubeFace.Bottom: return "bottom";
            default: return "unknown";
        }
    }
    #endregion

    #region Data Persistence
    private void SaveSessionData()
    {
        try
        {
            // Save with simple name for friends to find easily
            string simpleFileName = "player_statistics.json";
            string detailedFileName = $"InfinityQube_DemoStats_{currentSession.sessionId}_{currentSession.timestamp}.json";
            
            // Save to multiple locations for maximum accessibility
            SaveToLocation(Application.persistentDataPath, simpleFileName);
            SaveToLocation(Application.persistentDataPath, detailedFileName);
            
            // Also save to game directory if possible (build location)
            if (!Application.isEditor)
            {
                string gameDirectory = Path.GetDirectoryName(Application.dataPath);
                SaveToLocation(gameDirectory, simpleFileName);
            }
            
            // Save to Documents folder for easy access
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string gameDocumentsFolder = Path.Combine(documentsPath, "InfinityQube");
            Directory.CreateDirectory(gameDocumentsFolder);
            SaveToLocation(gameDocumentsFolder, simpleFileName);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerStatisticsManager] Failed to save session data: {e.Message}");
        }
    }
    
    private void SaveToLocation(string directory, string fileName)
    {
        try
        {
            string filePath = Path.Combine(directory, fileName);
            string jsonData = JsonUtility.ToJson(currentSession, true);
            File.WriteAllText(filePath, jsonData);
            
            DebugLog($"💾 Session data saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerStatisticsManager] Could not save to {directory}: {e.Message}");
        }
    }
    
    private void PerformEmergencySave()
    {
        try
        {
            if (currentSession == null) return;
            
            // Update session data for emergency save
            currentSession.sessionDuration = Time.time - sessionStartTime;
            currentSession.isCompleted = false; // Mark as emergency save
            
            // Update timestamp for final save
            currentSession.timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            
            // Save as both emergency backup and regular file
            string emergencyFileName = $"InfinityQube_EmergencyStats_{currentSession.sessionId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
            string regularFileName = "player_statistics.json";
            
            // Save emergency backup
            SaveToLocation(Application.persistentDataPath, emergencyFileName);
            
            // Save regular file for friends
            SaveToLocation(Application.persistentDataPath, regularFileName);
            
            // Also save to Documents folder
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string gameDocumentsFolder = Path.Combine(documentsPath, "InfinityQube");
            Directory.CreateDirectory(gameDocumentsFolder);
            SaveToLocation(gameDocumentsFolder, regularFileName);
            
            // Save to game directory if not in editor
            if (!Application.isEditor)
            {
                string gameDirectory = Path.GetDirectoryName(Application.dataPath);
                SaveToLocation(gameDirectory, regularFileName);
            }
            
            DebugLog($"🚨 Emergency save completed to multiple locations");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerStatisticsManager] Emergency save failed: {e.Message}");
        }
    }
    
    public PlayerSessionData LoadStatisticsFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[PlayerStatisticsManager] File not found: {filePath}");
                return null;
            }
            
            string jsonData = File.ReadAllText(filePath);
            PlayerSessionData sessionData = JsonUtility.FromJson<PlayerSessionData>(jsonData);
            
            DebugLog($"📂 Statistics file loaded: {filePath}");
            return sessionData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerStatisticsManager] Failed to load statistics file: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Future Analytics Placeholders
    public Dictionary<Vector2Int, float> PrepareHeatmapData()
    {
        // Placeholder for movement heatmap data preparation
        Dictionary<Vector2Int, float> heatmapData = new Dictionary<Vector2Int, float>();
        
        if (currentSession?.movementData?.positionHistory != null)
        {
            foreach (var position in currentSession.movementData.positionHistory)
            {
                if (!heatmapData.ContainsKey(position))
                    heatmapData[position] = 0f;
                heatmapData[position] += 1f;
            }
        }
        
        DebugLog($"🗺️ Heatmap data prepared with {heatmapData.Count} positions");
        return heatmapData;
    }
    
    public PlayerJourneyAnalysis AnalyzePlayerJourney()
    {
        // Placeholder for comprehensive player journey analysis
        var analysis = new PlayerJourneyAnalysis();
        
        if (currentSession != null)
        {
            analysis.sessionId = currentSession.sessionId;
            analysis.overallPerformance = CalculateOverallPerformance();
            analysis.learningCurve = AnalyzeLearningProgression();
            analysis.strugglingAreas = IdentifyStrugglingAreas();
            analysis.recommendations = GenerateRecommendations();
        }
        
        DebugLog("🎯 Player journey analysis completed");
        return analysis;
    }
    
    public List<string> GenerateInsights()
    {
        // Placeholder for automated insight generation
        List<string> insights = new List<string>();
        
        if (currentSession?.finalStats != null)
        {
            var stats = currentSession.finalStats;
            
            if (stats.playerDeaths > 5)
                insights.Add("Player experienced high death rate - consider easier tutorial progression");
                
            if (currentSession.markerData.triggerSuccessRate < 0.6f)
                insights.Add("Low marker trigger success rate - tutorial needs marker timing guidance");
                
            if (currentSession.tutorialData.messagesSkipped > 3)
                insights.Add("Multiple tutorial messages skipped - content may be too verbose");
        }
        
        DebugLog($"💡 Generated {insights.Count} insights");
        return insights;
    }
    
    // Supporting classes for analytics placeholders
    [System.Serializable]
    public class PlayerJourneyAnalysis
    {
        public string sessionId;
        public float overallPerformance;
        public List<float> learningCurve = new List<float>();
        public List<string> strugglingAreas = new List<string>();
        public List<string> recommendations = new List<string>();
    }
    
    private float CalculateOverallPerformance()
    {
        // Simplified performance calculation
        if (currentSession?.finalStats == null) return 0f;
        
        var stats = currentSession.finalStats;
        float captureRate = stats.TotalCubesInteracted > 0 ? 
            (float)stats.TotalCubesCaptured / stats.TotalCubesInteracted : 0f;
        float survivalRate = stats.totalPlayTime > 0 ? 
            stats.timeAlive / stats.totalPlayTime : 0f;
        
        return (captureRate + survivalRate) / 2f;
    }
    
    private List<float> AnalyzeLearningProgression()
    {
        // Placeholder for learning curve analysis
        return new List<float> { 0.3f, 0.5f, 0.7f, 0.8f, 0.9f };
    }
    
    private List<string> IdentifyStrugglingAreas()
    {
        // Placeholder for identifying areas where player struggled
        List<string> areas = new List<string>();
        
        if (currentSession?.markerData.triggerSuccessRate < 0.5f)
            areas.Add("Marker Timing");
            
        if (currentSession?.finalStats.playerDeaths > 3)
            areas.Add("Collision Avoidance");
            
        return areas;
    }
    
    private List<string> GenerateRecommendations()
    {
        // Placeholder for generating improvement recommendations
        return new List<string>
        {
            "Practice marker timing in safe scenarios",
            "Focus on movement patterns to avoid collisions",
            "Review tutorial messages for strategy tips"
        };
    }
    #endregion

    #region Public API
    public bool IsCollecting => isCollecting;
    public PlayerSessionData CurrentSession => currentSession;
    public string GetCurrentSessionId() => currentSession?.sessionId ?? "No Active Session";
    public float GetSessionDuration() => isCollecting ? Time.time - sessionStartTime : 0f;
    
    public void ResetCurrentSession()
    {
        if (isCollecting)
        {
            CompleteCurrentSession();
        }
        StartNewSession();
        DebugLog("🔄 Current session reset");
    }
    
    public void ForceManualSave()
    {
        if (currentSession == null)
        {
            DebugLog("⚠️ No active session to save");
            return;
        }
        
        // Update current session data with proper duration calculation
        float currentTime = Time.time;
        float calculatedDuration = currentTime - sessionStartTime;
        currentSession.sessionDuration = Mathf.Max(0f, calculatedDuration);
        currentSession.timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        
        DebugLog($"💾 Manual save: duration {currentSession.sessionDuration:F2}s, moves {currentSession.movementData.totalMoves}");
        
        // Finalize data if we have meaningful content
        if (currentSession.movementData.totalMoves > 0 || currentSession.sessionDuration > 5f)
        {
            FinalizeSessionData();
        }
        
        // Perform the save
        SaveSessionData();
        
        DebugLog("💾 Manual save completed");
    }
    #endregion

    #region Utility Methods
    private void DebugLog(string message)
    {
        if (EnableDebugLogs)
        {
            Debug.Log($"[PlayerStatisticsManager] {message}");
        }
    }
    #endregion

    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs { get; set; } = true;

    public string GetDebugStatus()
    {
        string status = isCollecting ? "COLLECTING" : "STOPPED";
        string sessionId = currentSession?.sessionId?.Substring(0, 8) ?? "none";
        float duration = GetSessionDuration();
        return $"Statistics: {status} Session:{sessionId} Duration:{duration:F1}s";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Is Collecting"] = isCollecting,
            ["Session ID"] = GetCurrentSessionId(),
            ["Session Duration"] = GetSessionDuration(),
            ["Positions Recorded"] = currentSession?.movementData?.positionHistory?.Count ?? 0,
            ["Markers Placed"] = (currentSession?.markerData?.unitMarkerPlacements?.Count ?? 0) +
                               (currentSession?.markerData?.RecursionMarkerPlacements?.Count ?? 0) +
                               (currentSession?.markerData?.primeMarkerPlacements?.Count ?? 0),
            ["Cubes Captured"] = currentSession?.cubeData?.captureEvents?.Count ?? 0,
            ["Tutorial Messages"] = currentSession?.tutorialData?.messageEvents?.Count ?? 0,
            ["Auto Save Enabled"] = autoSaveOnCompletion,
            ["Emergency Save Enabled"] = emergencySaveOnQuit,
            ["Detailed Tracking"] = enableDetailedTracking,
            ["Collection Enabled"] = enableStatisticsCollection,
            ["Current APM"] = sessionAPM,
            ["Total Actions"] = totalActions,
            ["Strategic Decisions"] = currentSession?.strategicData?.decisionEvents?.Count ?? 0,
            ["Face Painting Events"] = facePaintingEvents?.Count ?? 0,
            ["APM Samples"] = currentSession?.resourceMetrics?.apmSamples?.Count ?? 0,
            ["Recent Actions (1min)"] = recentActionTimes?.Count ?? 0
        };
    }

    public void ResetToDefaults()
    {
        // Stop current collection
        if (isCollecting)
        {
            CompleteCurrentSession();
        }
        
        // Reset all tracking data
        tileVisitCounts.Clear();
        activeMessageEvents.Clear();
        activeMarkers.Clear();
        
        // Reset advanced analytics tracking
        strategicDecisionTimes.Clear();
        facePaintingEvents.Clear();
        sessionAPM = 0f;
        totalActions = 0;
        lastActionTime = 0f;
        recentActionTimes.Clear();
        actionCounts.Clear();
        lastDecisionStartTime = 0f;
        currentDecisionType = "";
        lastAPMCalculationTime = 0f;
        
        // Reset state
        isCollecting = false;
        sessionStartTime = 0f;
        lastPositionRecordTime = 0f;
        lastRecordedPosition = Vector2Int.zero;
        currentSession = null;
        
        if (EnableDebugLogs)
            Debug.Log("[PlayerStatisticsManager] Reset to defaults completed");
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for statistics settings
        if (EnableDebugLogs)
            Debug.Log($"[PlayerStatisticsManager] Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for statistics settings
        if (EnableDebugLogs)
            Debug.Log($"[PlayerStatisticsManager] Saving configuration: {configName} (not yet implemented)");
    }
    #endregion
}