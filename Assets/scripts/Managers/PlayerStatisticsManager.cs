using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Linq;

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
    public bool enableDebugLogs = false;
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
    
    // Performance Optimization
    private float positionTrackingInterval = 0.1f;
    private int maxPositionHistory = 1000;
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
        // Always save when quitting, regardless of settings when not in editor
        if ((emergencySaveOnQuit || !Application.isEditor) && currentSession != null)
        {
            // Mark as completed if we have meaningful data
            if (currentSession.sessionDuration > 10f || currentSession.movementData.totalMoves > 5)
            {
                FinalizeSessionData();
                currentSession.isCompleted = true;
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
        
        isCollecting = true;
        
        RecordInitialState();
    }
    
    public void CompleteCurrentSession()
    {
        if (!isCollecting || currentSession == null) return;
        
        FinalizeSessionData();
        SaveSessionData();
        
        isCollecting = false;
        DebugLog($"✅ Session completed: {currentSession.sessionId}");
    }
    
    private void FinalizeSessionData()
    {
        currentSession.sessionDuration = Time.time - sessionStartTime;
        currentSession.isCompleted = true;
        
        // Calculate final movement data
        currentSession.movementData.CalculateDistanceTraveled();
        CalculateTileVisitStatistics();
        
        // Calculate marker efficiency
        CalculateMarkerEfficiency();
        
        // Calculate tutorial progress
        CalculateTutorialProgress();
        
        // Copy final player statistics
        if (playerManager != null)
        {
            currentSession.finalStats = playerManager.GetCurrentStatistics();
        }
    }
    #endregion

    #region Data Collection
    private void UpdateDataCollection()
    {
        TrackPlayerMovement();
        UpdateSessionTimer();
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
                currentSession.markerData.lightMarkerPlacements.Add(placementEvent);
                break;
            case "heavy":
                currentSession.markerData.heavyMarkerPlacements.Add(placementEvent);
                break;
            case "prime":
                currentSession.markerData.primeMarkerPlacements.Add(placementEvent);
                break;
        }
        
        string markerKey = $"{markerType}_{position.x}_{position.y}";
        activeMarkers[markerKey] = placementEvent;
        
        DebugLog($"📍 {markerType} marker placed at {position}");
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
        
        // Update current session data
        currentSession.sessionDuration = Time.time - sessionStartTime;
        currentSession.timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        
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
        if (enableDebugLogs)
        {
            Debug.Log($"[PlayerStatisticsManager] {message}");
        }
    }
    #endregion

    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

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
            ["Markers Placed"] = (currentSession?.markerData?.lightMarkerPlacements?.Count ?? 0) +
                               (currentSession?.markerData?.heavyMarkerPlacements?.Count ?? 0) +
                               (currentSession?.markerData?.primeMarkerPlacements?.Count ?? 0),
            ["Cubes Captured"] = currentSession?.cubeData?.captureEvents?.Count ?? 0,
            ["Tutorial Messages"] = currentSession?.tutorialData?.messageEvents?.Count ?? 0,
            ["Auto Save Enabled"] = autoSaveOnCompletion,
            ["Emergency Save Enabled"] = emergencySaveOnQuit,
            ["Detailed Tracking"] = enableDetailedTracking,
            ["Collection Enabled"] = enableStatisticsCollection
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