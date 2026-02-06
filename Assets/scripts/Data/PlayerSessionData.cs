using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSessionData
{
    [Header("Session Metadata")]
    public string sessionId;
    public string timestamp;
    public float sessionDuration;
    public bool isCompleted;
    public string gameVersion;
    public string stageName;
    
    [Header("Player Performance")]
    public MovementData movementData;
    public MarkerInteractionData markerData;
    public CubeInteractionData cubeData;
    public TutorialProgressData tutorialData;
    
    [Header("Summary Statistics")]
    public PlayerStatistics finalStats;
    
    [Header("Advanced Analytics")]
    public StrategicDecisionData strategicData;
    public ResourceEfficiencyMetrics resourceMetrics;
    public LearningProgressionData learningData;
    
    public PlayerSessionData()
    {
        sessionId = System.Guid.NewGuid().ToString();
        timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        movementData = new MovementData();
        markerData = new MarkerInteractionData();
        cubeData = new CubeInteractionData();
        tutorialData = new TutorialProgressData();
        finalStats = new PlayerStatistics();
        
        // Initialize advanced analytics
        strategicData = new StrategicDecisionData();
        resourceMetrics = new ResourceEfficiencyMetrics();
        learningData = new LearningProgressionData();
        
        // Ensure dictionaries are initialized
        movementData.timeInGridAreas = new Dictionary<string, float>();
        cubeData.cubeTypesCaptured = new Dictionary<string, int>();
        cubeData.cubeTypesEscaped = new Dictionary<string, int>();
        tutorialData.conceptMasteryTimes = new Dictionary<string, float>();
    }
}

[System.Serializable]
public class MovementData
{
    [Header("Position Tracking")]
    public List<Vector2Int> positionHistory = new List<Vector2Int>();
    public List<float> timeStamps = new List<float>();
    public float totalDistanceTraveled;
    public float averageSpeed;
    public int totalMoves;
    
    [Header("Grid Area Analysis")]
    public Dictionary<string, float> timeInGridAreas = new Dictionary<string, float>();
    public Vector2Int mostVisitedTile;
    public int mostVisitedCount;
    
    public void RecordPosition(Vector2Int position, float time)
    {
        positionHistory.Add(position);
        timeStamps.Add(time);
        totalMoves++;
    }
    
    public void CalculateDistanceTraveled()
    {
        totalDistanceTraveled = 0f;
        for (int i = 1; i < positionHistory.Count; i++)
        {
            Vector2Int diff = positionHistory[i] - positionHistory[i - 1];
            totalDistanceTraveled += Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);
        }
        
        if (timeStamps.Count > 1)
        {
            float totalTime = timeStamps[timeStamps.Count - 1] - timeStamps[0];
            averageSpeed = totalTime > 0 ? totalDistanceTraveled / totalTime : 0f;
        }
    }
}

[System.Serializable]
public class MarkerInteractionData
{
    [Header("Placement Timing")]
    public List<MarkerPlacementEvent> unitMarkerPlacements = new List<MarkerPlacementEvent>();
    public List<MarkerPlacementEvent> RecursionMarkerPlacements = new List<MarkerPlacementEvent>();
    public List<MarkerPlacementEvent> matrixMarkerPlacements = new List<MarkerPlacementEvent>();
    
    [Header("Trigger Efficiency")]
    public List<MarkerTriggerEvent> triggerEvents = new List<MarkerTriggerEvent>();
    public float averageTimeBetweenPlaceAndTrigger;
    public float triggerSuccessRate;
    
    [Header("Performance Metrics")]
    public int perfectTimingHits;
    public int missedTriggers;
    public float averageCooldownWait;
}

[System.Serializable]
public class MarkerPlacementEvent
{
    public Vector2Int position;
    public float timestamp;
    public string markerType;
    public bool wasTriggered;
    public float triggerTime;
    
    public MarkerPlacementEvent(Vector2Int pos, float time, string type)
    {
        position = pos;
        timestamp = time;
        markerType = type;
        wasTriggered = false;
        triggerTime = -1f;
    }
}

[System.Serializable]
public class MarkerTriggerEvent
{
    public Vector2Int position;
    public float timestamp;
    public string markerType;
    public bool hitTarget;
    public int cubesAffected;
    public float timeSincePlacement;
    
    public MarkerTriggerEvent(Vector2Int pos, float time, string type, bool hit, int cubes)
    {
        position = pos;
        timestamp = time;
        markerType = type;
        hitTarget = hit;
        cubesAffected = cubes;
    }
}

[System.Serializable]
public class CubeInteractionData
{
    [Header("Capture Events")]
    public List<CubeInteractionEvent> captureEvents = new List<CubeInteractionEvent>();
    public List<CubeInteractionEvent> escapeEvents = new List<CubeInteractionEvent>();
    public List<CubeCollisionEvent> collisionEvents = new List<CubeCollisionEvent>();
    
    [Header("Performance Analysis")]
    public float averageCaptureTime;
    public Dictionary<string, int> cubeTypesCaptured = new Dictionary<string, int>();
    public Dictionary<string, int> cubeTypesEscaped = new Dictionary<string, int>();
}

[System.Serializable]
public class CubeInteractionEvent
{
    public Vector2Int position;
    public float timestamp;
    public string cubeType;
    public string interactionMethod; // "marker", "collision", etc.
    
    public CubeInteractionEvent(Vector2Int pos, float time, string type, string method)
    {
        position = pos;
        timestamp = time;
        cubeType = type;
        interactionMethod = method;
    }
}

[System.Serializable]
public class CubeCollisionEvent
{
    public Vector2Int position;
    public float timestamp;
    public string cubeType;
    public bool resultedInDeath;
    
    public CubeCollisionEvent(Vector2Int pos, float time, string type, bool death)
    {
        position = pos;
        timestamp = time;
        cubeType = type;
        resultedInDeath = death;
    }
}

[System.Serializable]
public class TutorialProgressData
{
    [Header("Message Interaction")]
    public List<MessageInteractionEvent> messageEvents = new List<MessageInteractionEvent>();
    public float totalPauseTime;
    public int messagesSkipped;
    public float averageReadTime;
    
    [Header("Learning Progress")]
    public Dictionary<string, float> conceptMasteryTimes = new Dictionary<string, float>();
    public List<string> strugglingConcepts = new List<string>();
}

[System.Serializable]
public class MessageInteractionEvent
{
    public string messageContent;
    public float displayTime;
    public float readTime;
    public bool wasSkipped;
    public int moveStepDisplayed;
    
    public MessageInteractionEvent(string content, float display, int step)
    {
        messageContent = content;
        displayTime = display;
        moveStepDisplayed = step;
        wasSkipped = false;
        readTime = 0f;
    }
}

[System.Serializable]
public class StrategicDecisionData
{
    [Header("Decision Timing")]
    public List<DecisionEvent> decisionEvents = new List<DecisionEvent>();
    public float averageDecisionTime;
    public float timeBetweenDecisions;
    
    [Header("Risk Assessment")]
    public List<RiskAssessmentEvent> riskEvents = new List<RiskAssessmentEvent>();
    public float riskTolerance;
    public int conservativeDecisions;
    public int aggressiveDecisions;
    
    [Header("Adaptation Patterns")]
    public Dictionary<string, int> strategyChanges = new Dictionary<string, int>();
    public List<AdaptationEvent> adaptationEvents = new List<AdaptationEvent>();
    public float adaptationSpeed;
    
    public StrategicDecisionData()
    {
        strategyChanges = new Dictionary<string, int>();
    }
}

[System.Serializable]
public class DecisionEvent
{
    public float timestamp;
    public string decisionType; // "marker_placement", "movement_direction", "resource_usage"
    public float decisionTime; // Time taken to make decision
    public string context; // What was happening when decision was made
    public bool wasSuccessful;
    
    public DecisionEvent(float time, string type, float duration, string contextInfo)
    {
        timestamp = time;
        decisionType = type;
        decisionTime = duration;
        context = contextInfo;
        wasSuccessful = false;
    }
}

[System.Serializable]
public class RiskAssessmentEvent
{
    public float timestamp;
    public string situation; // "multiple_cubes_nearby", "low_health", "limited_markers"
    public string action; // "retreat", "advance", "wait"
    public float riskLevel; // 0-1 scale
    public bool wasCorrect;
    
    public RiskAssessmentEvent(float time, string sit, string act, float risk)
    {
        timestamp = time;
        situation = sit;
        action = act;
        riskLevel = risk;
        wasCorrect = false;
    }
}

[System.Serializable]
public class AdaptationEvent
{
    public float timestamp;
    public string previousStrategy;
    public string newStrategy;
    public string trigger; // What caused the adaptation
    public float timeToAdapt;
    
    public AdaptationEvent(float time, string prev, string newStrat, string trig)
    {
        timestamp = time;
        previousStrategy = prev;
        newStrategy = newStrat;
        trigger = trig;
        timeToAdapt = 0f;
    }
}

[System.Serializable]
public class ResourceEfficiencyMetrics
{
    [Header("APM Calculations")]
    public List<APMSample> apmSamples = new List<APMSample>();
    public float averageAPM; // Actions Per Minute
    public float peakAPM;
    public float currentAPM;
    
    [Header("Resource Waste Analysis")]
    public int wastedMarkers;
    public int inefficientMovements;
    public float timeWastedIdle;
    public Dictionary<string, int> wasteByCategory = new Dictionary<string, int>();
    
    [Header("Optimal Timing Metrics")]
    public List<TimingEvent> timingEvents = new List<TimingEvent>();
    public float optimalTimingPercentage;
    public float averageReactionTime;
    public int perfectTimingCount;
    public int poorTimingCount;
    
    public ResourceEfficiencyMetrics()
    {
        wasteByCategory = new Dictionary<string, int>();
    }
}

[System.Serializable]
public class APMSample
{
    public float timestamp;
    public int actionsInWindow; // Actions in the last minute
    public float apmValue;
    public string dominantActionType; // "movement", "marker_placement", "interaction"
    
    public APMSample(float time, int actions, string actionType)
    {
        timestamp = time;
        actionsInWindow = actions;
        apmValue = actions; // Will be calculated based on time window
        dominantActionType = actionType;
    }
}

[System.Serializable]
public class TimingEvent
{
    public float timestamp;
    public string eventType; // "marker_trigger", "cube_capture", "movement_decision"
    public float optimalTime; // What the optimal timing would have been
    public float actualTime; // When the player actually acted
    public float efficiency; // 0-1 scale, 1 being perfect timing
    public bool wasPerfect;
    
    public TimingEvent(float time, string type, float optimal, float actual)
    {
        timestamp = time;
        eventType = type;
        optimalTime = optimal;
        actualTime = actual;
        efficiency = 0f;
        wasPerfect = false;
    }
}

[System.Serializable]
public class LearningProgressionData
{
    [Header("Skill Progression Indicators")]
    public List<SkillMeasurement> skillMeasurements = new List<SkillMeasurement>();
    public Dictionary<string, float> skillLevels = new Dictionary<string, float>();
    public float overallSkillProgression;
    public List<string> masteredSkills = new List<string>();
    
    [Header("Plateau Detection")]
    public List<PlateauEvent> plateauEvents = new List<PlateauEvent>();
    public bool isCurrentlyOnPlateau;
    public float plateauDuration;
    public string plateauSkill;
    
    [Header("Improvement Rates")]
    public Dictionary<string, float> improvementRates = new Dictionary<string, float>();
    public List<ImprovementEvent> improvementEvents = new List<ImprovementEvent>();
    public float overallImprovementRate;
    public float learningVelocity;
    
    public LearningProgressionData()
    {
        skillLevels = new Dictionary<string, float>();
        improvementRates = new Dictionary<string, float>();
    }
}

[System.Serializable]
public class SkillMeasurement
{
    public float timestamp;
    public string skillName; // "marker_accuracy", "movement_efficiency", "timing_precision"
    public float skillValue; // 0-1 scale
    public float confidenceLevel; // How reliable this measurement is
    public string measurementMethod; // "performance_based", "time_based", "accuracy_based"
    
    public SkillMeasurement(float time, string skill, float value, string method)
    {
        timestamp = time;
        skillName = skill;
        skillValue = value;
        measurementMethod = method;
        confidenceLevel = 1f;
    }
}

[System.Serializable]
public class PlateauEvent
{
    public float startTime;
    public float endTime;
    public string skillName;
    public float plateauLevel;
    public bool isOngoing;
    public string potentialCause; // "difficulty_spike", "lack_of_challenge", "fatigue"
    
    public PlateauEvent(float start, string skill, float level)
    {
        startTime = start;
        skillName = skill;
        plateauLevel = level;
        isOngoing = true;
        endTime = -1f;
        potentialCause = "unknown";
    }
}

[System.Serializable]
public class ImprovementEvent
{
    public float timestamp;
    public string skillName;
    public float previousLevel;
    public float newLevel;
    public float improvementAmount;
    public string trigger; // What caused the improvement
    
    public ImprovementEvent(float time, string skill, float prev, float newLevel)
    {
        timestamp = time;
        skillName = skill;
        previousLevel = prev;
        this.newLevel = newLevel;
        improvementAmount = newLevel - prev;
        trigger = "unknown";
    }
}