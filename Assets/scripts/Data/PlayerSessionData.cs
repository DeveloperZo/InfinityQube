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
    
    public PlayerSessionData()
    {
        sessionId = System.Guid.NewGuid().ToString();
        timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        movementData = new MovementData();
        markerData = new MarkerInteractionData();
        cubeData = new CubeInteractionData();
        tutorialData = new TutorialProgressData();
        finalStats = new PlayerStatistics();
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
    public List<MarkerPlacementEvent> lightMarkerPlacements = new List<MarkerPlacementEvent>();
    public List<MarkerPlacementEvent> heavyMarkerPlacements = new List<MarkerPlacementEvent>();
    public List<MarkerPlacementEvent> primeMarkerPlacements = new List<MarkerPlacementEvent>();
    
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