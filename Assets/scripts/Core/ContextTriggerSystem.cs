using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Base class for context-sensitive triggers that evaluate game state conditions
/// and determine when tutorial messages should be displayed.
/// </summary>
public abstract class ContextTrigger
{
    public string triggerName { get; protected set; }
    public float lastTriggeredTime { get; set; } = 0f;
    public float cooldownSeconds { get; protected set; } = 5f;
    public bool isEnabled { get; set; } = true;

    protected ContextTrigger(string name, float cooldown = 5f)
    {
        triggerName = name;
        cooldownSeconds = cooldown;
    }

    /// <summary>
    /// Evaluate whether this trigger condition is currently met
    /// </summary>
    public abstract bool EvaluateCondition(GameContext context);

    /// <summary>
    /// Check if trigger can fire (considers cooldown and enabled state)
    /// </summary>
    public bool CanTrigger()
    {
        return isEnabled && (Time.time - lastTriggeredTime) >= cooldownSeconds;
    }

    /// <summary>
    /// Mark trigger as fired
    /// </summary>
    public void OnTriggered()
    {
        lastTriggeredTime = Time.time;
    }

    /// <summary>
    /// Get messages associated with this trigger
    /// </summary>
    public abstract List<TutorialMessage> GetTriggeredMessages(MessageDatabase database, GameContext context);
}

/// <summary>
/// Monitors player state changes and triggers appropriate messages
/// </summary>
public class PlayerStateTrigger : ContextTrigger
{
    public enum PlayerState
    {
        LowHealth,
        NoMarkers,
        FewMarkers,
        HasMarkers,
        NearCube,
        FarFromCubes,
        AtGridEdge,
        CenterPosition
    }

    private PlayerState targetState;
    private PlayerState lastState = PlayerState.CenterPosition;
    private int markerThreshold;
    private float proximityThreshold;

    public PlayerStateTrigger(PlayerState state, float cooldown = 3f, int markerThreshold = 1, float proximityThreshold = 2f) 
        : base($"PlayerState_{state}", cooldown)
    {
        this.targetState = state;
        this.markerThreshold = markerThreshold;
        this.proximityThreshold = proximityThreshold;
    }

    public override bool EvaluateCondition(GameContext context)
    {
        PlayerState currentState = DeterminePlayerState(context);
        bool stateChanged = currentState != lastState;
        bool matchesTarget = currentState == targetState;
        
        lastState = currentState;
        return stateChanged && matchesTarget;
    }

    private PlayerState DeterminePlayerState(GameContext context)
    {
        // Check marker availability
        if (context.availableMarkers == 0)
            return PlayerState.NoMarkers;
        else if (context.availableMarkers <= markerThreshold)
            return PlayerState.FewMarkers;
        else if (context.availableMarkers > markerThreshold)
            return PlayerState.HasMarkers;

        // Check cube proximity
        if (context.nearestCubeDistance <= proximityThreshold)
            return PlayerState.NearCube;
        else if (context.nearestCubeDistance > proximityThreshold * 2)
            return PlayerState.FarFromCubes;

        // Check grid position
        // Assuming grid bounds, check if near edges
        if (context.playerPosition.x <= 1 || context.playerPosition.y <= 1)
            return PlayerState.AtGridEdge;

        return PlayerState.CenterPosition;
    }

    public override List<TutorialMessage> GetTriggeredMessages(MessageDatabase database, GameContext context)
    {
        var messages = new List<TutorialMessage>();
        
        switch (targetState)
        {
            case PlayerState.NoMarkers:
                messages.AddRange(FindMessagesWithId(database, "no_markers"));
                break;
            case PlayerState.FewMarkers:
                messages.AddRange(FindMessagesWithId(database, "low_markers"));
                break;
            case PlayerState.NearCube:
                messages.AddRange(FindMessagesWithId(database, "cube_proximity"));
                break;
        }

        return messages;
    }

    private List<TutorialMessage> FindMessagesWithId(MessageDatabase database, string idPattern)
    {
        var results = new List<TutorialMessage>();
        
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = database.GetMessagesByCategory(category);
            results.AddRange(messages.Where(m => m.messageId.Contains(idPattern)));
        }
        
        return results;
    }
}

/// <summary>
/// Monitors wave progression and triggers messages at key moments
/// </summary>
public class WaveProgressTrigger : ContextTrigger
{
    public enum WaveEvent
    {
        WaveStart,
        MidWave,
        WaveEnd,
        CubeSpawned,
        CubeCaptured,
        CubeEscaped,
        FirstCube,
        LastCube
    }

    private WaveEvent targetEvent;
    private int lastMoveStep = -1;
    private int lastCubeCount = 0;

    public WaveProgressTrigger(WaveEvent eventType, float cooldown = 5f) 
        : base($"WaveProgress_{eventType}", cooldown)
    {
        targetEvent = eventType;
    }

    public override bool EvaluateCondition(GameContext context)
    {
        bool triggered = false;
        int currentCubeCount = context.activeCubeTypes.Count;

        switch (targetEvent)
        {
            case WaveEvent.WaveStart:
                triggered = context.currentMoveStep == 0 && lastMoveStep != 0;
                break;
                
            case WaveEvent.MidWave:
                triggered = context.currentMoveStep > 0 && context.currentMoveStep % 5 == 0;
                break;
                
            case WaveEvent.CubeSpawned:
                triggered = currentCubeCount > lastCubeCount;
                break;
                
            case WaveEvent.FirstCube:
                triggered = currentCubeCount > 0 && lastCubeCount == 0;
                break;
                
            case WaveEvent.LastCube:
                triggered = currentCubeCount == 1 && lastCubeCount > 1;
                break;
        }

        lastMoveStep = context.currentMoveStep;
        lastCubeCount = currentCubeCount;
        
        return triggered;
    }

    public override List<TutorialMessage> GetTriggeredMessages(MessageDatabase database, GameContext context)
    {
        var messages = new List<TutorialMessage>();
        
        switch (targetEvent)
        {
            case WaveEvent.WaveStart:
                messages.AddRange(FindMessagesWithId(database, "wave_start"));
                break;
            case WaveEvent.FirstCube:
                messages.AddRange(FindMessagesWithId(database, "first_cube"));
                break;
            case WaveEvent.LastCube:
                messages.AddRange(FindMessagesWithId(database, "last_cube"));
                break;
        }

        return messages;
    }

    private List<TutorialMessage> FindMessagesWithId(MessageDatabase database, string idPattern)
    {
        var results = new List<TutorialMessage>();
        
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = database.GetMessagesByCategory(category);
            results.AddRange(messages.Where(m => m.messageId.Contains(idPattern)));
        }
        
        return results;
    }
}

/// <summary>
/// Monitors marker availability and usage patterns
/// </summary>
public class MarkerAvailabilityTrigger : ContextTrigger
{
    public enum MarkerEvent
    {
        MarkersAvailable,
        MarkersLow,
        MarkersEmpty,
        MarkerRecharge,
        FirstMarkerPlaced,
        ManyMarkersPlaced
    }

    private MarkerEvent targetEvent;
    private int lastMarkerCount = 0;
    private int markerThreshold;

    public MarkerAvailabilityTrigger(MarkerEvent eventType, int threshold = 1, float cooldown = 4f) 
        : base($"MarkerAvailability_{eventType}", cooldown)
    {
        targetEvent = eventType;
        markerThreshold = threshold;
    }

    public override bool EvaluateCondition(GameContext context)
    {
        bool triggered = false;
        int currentMarkers = context.availableMarkers;

        switch (targetEvent)
        {
            case MarkerEvent.MarkersLow:
                triggered = currentMarkers <= markerThreshold && lastMarkerCount > markerThreshold;
                break;
                
            case MarkerEvent.MarkersEmpty:
                triggered = currentMarkers == 0 && lastMarkerCount > 0;
                break;
                
            case MarkerEvent.MarkerRecharge:
                triggered = currentMarkers > lastMarkerCount; // Increased
                break;
                
            case MarkerEvent.FirstMarkerPlaced:
                triggered = currentMarkers < lastMarkerCount && lastMarkerCount > 0; // First use
                break;
                
            case MarkerEvent.MarkersAvailable:
                triggered = currentMarkers >= markerThreshold && lastMarkerCount < markerThreshold;
                break;
        }

        lastMarkerCount = currentMarkers;
        return triggered;
    }

    public override List<TutorialMessage> GetTriggeredMessages(MessageDatabase database, GameContext context)
    {
        var messages = new List<TutorialMessage>();
        
        switch (targetEvent)
        {
            case MarkerEvent.MarkersLow:
                messages.AddRange(FindMessagesWithId(database, "markers_low"));
                break;
            case MarkerEvent.MarkersEmpty:
                messages.AddRange(FindMessagesWithId(database, "markers_empty"));
                break;
            case MarkerEvent.MarkerRecharge:
                messages.AddRange(FindMessagesWithId(database, "marker_recharge"));
                break;
        }

        return messages;
    }

    private List<TutorialMessage> FindMessagesWithId(MessageDatabase database, string idPattern)
    {
        var results = new List<TutorialMessage>();
        
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = database.GetMessagesByCategory(category);
            results.AddRange(messages.Where(m => m.messageId.Contains(idPattern)));
        }
        
        return results;
    }
}

/// <summary>
/// Monitors cube proximity and spatial relationships
/// </summary>
public class CubeProximityTrigger : ContextTrigger
{
    public enum ProximityEvent
    {
        CubeNear,
        CubeFar,
        CubeAdjacent,
        MultipleCubesNear,
        CubeTypeSpecific
    }

    private ProximityEvent targetEvent;
    private float proximityThreshold;
    private CubeType specificCubeType;
    private bool lastProximityState = false;

    public CubeProximityTrigger(ProximityEvent eventType, float threshold = 2f, CubeType cubeType = CubeType.Unit, float cooldown = 3f) 
        : base($"CubeProximity_{eventType}_{cubeType}", cooldown)
    {
        targetEvent = eventType;
        proximityThreshold = threshold;
        specificCubeType = cubeType;
    }

    public override bool EvaluateCondition(GameContext context)
    {
        bool triggered = false;
        bool currentProximityState = false;

        switch (targetEvent)
        {
            case ProximityEvent.CubeNear:
                currentProximityState = context.nearestCubeDistance <= proximityThreshold;
                triggered = currentProximityState && !lastProximityState;
                break;
                
            case ProximityEvent.CubeFar:
                currentProximityState = context.nearestCubeDistance > proximityThreshold * 1.5f;
                triggered = currentProximityState && !lastProximityState;
                break;
                
            case ProximityEvent.CubeAdjacent:
                currentProximityState = context.nearestCubeDistance <= 1.1f; // Adjacent tiles
                triggered = currentProximityState && !lastProximityState;
                break;
                
            case ProximityEvent.CubeTypeSpecific:
                currentProximityState = context.activeCubeTypes.Contains(specificCubeType) && 
                                      context.nearestCubeDistance <= proximityThreshold;
                triggered = currentProximityState && !lastProximityState;
                break;
        }

        lastProximityState = currentProximityState;
        return triggered;
    }

    public override List<TutorialMessage> GetTriggeredMessages(MessageDatabase database, GameContext context)
    {
        var messages = new List<TutorialMessage>();
        
        switch (targetEvent)
        {
            case ProximityEvent.CubeNear:
                messages.AddRange(FindMessagesWithId(database, "cube_near"));
                break;
            case ProximityEvent.CubeAdjacent:
                messages.AddRange(FindMessagesWithId(database, "cube_adjacent"));
                break;
            case ProximityEvent.CubeTypeSpecific:
                messages.AddRange(FindMessagesWithId(database, $"cube_{specificCubeType.ToString().ToLower()}"));
                break;
        }

        return messages;
    }

    private List<TutorialMessage> FindMessagesWithId(MessageDatabase database, string idPattern)
    {
        var results = new List<TutorialMessage>();
        
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = database.GetMessagesByCategory(category);
            results.AddRange(messages.Where(m => m.messageId.Contains(idPattern)));
        }
        
        return results;
    }
}

/// <summary>
/// Manages registration and evaluation of context triggers for the tutorial system
/// </summary>
public class ContextTriggerManager
{
    private List<ContextTrigger> registeredTriggers = new List<ContextTrigger>();
    private MessageDatabase messageDatabase;
    
    // Cached manager references for performance
    private WaveManager waveManager;
    private PlayerManager playerManager;
    private PlayerActionManager playerActionManager;
    private GridManager gridManager;
    
    // Performance tracking
    private int triggerEvaluationsThisFrame = 0;
    private int maxEvaluationsPerFrame = 5;
    private float lastEvaluationTime = 0f;

    public ContextTriggerManager(MessageDatabase database)
    {
        messageDatabase = database;
        InitializeDefaultTriggers();
    }

    public void CacheManagerReferences(WaveManager wave, PlayerManager player, PlayerActionManager playerAction, GridManager grid)
    {
        waveManager = wave;
        playerManager = player;
        playerActionManager = playerAction;
        gridManager = grid;
        
        Debug.Log("[ContextTriggerManager] Manager references cached successfully");
    }

    private void InitializeDefaultTriggers()
    {
        // Player state triggers
        RegisterTrigger(new PlayerStateTrigger(PlayerStateTrigger.PlayerState.NoMarkers, 5f));
        RegisterTrigger(new PlayerStateTrigger(PlayerStateTrigger.PlayerState.FewMarkers, 4f, 2));
        RegisterTrigger(new PlayerStateTrigger(PlayerStateTrigger.PlayerState.NearCube, 3f, 1, 2f));
        
        // Wave progress triggers
        RegisterTrigger(new WaveProgressTrigger(WaveProgressTrigger.WaveEvent.WaveStart, 8f));
        RegisterTrigger(new WaveProgressTrigger(WaveProgressTrigger.WaveEvent.FirstCube, 6f));
        RegisterTrigger(new WaveProgressTrigger(WaveProgressTrigger.WaveEvent.LastCube, 5f));
        
        // Marker availability triggers
        RegisterTrigger(new MarkerAvailabilityTrigger(MarkerAvailabilityTrigger.MarkerEvent.MarkersLow, 1, 4f));
        RegisterTrigger(new MarkerAvailabilityTrigger(MarkerAvailabilityTrigger.MarkerEvent.MarkersEmpty, 0, 6f));
        RegisterTrigger(new MarkerAvailabilityTrigger(MarkerAvailabilityTrigger.MarkerEvent.MarkerRecharge, 1, 3f));
        
        // Cube proximity triggers
        RegisterTrigger(new CubeProximityTrigger(CubeProximityTrigger.ProximityEvent.CubeAdjacent, 1.5f, CubeType.Unit, 4f));
        RegisterTrigger(new CubeProximityTrigger(CubeProximityTrigger.ProximityEvent.CubeTypeSpecific, 2f, CubeType.Prime, 5f));
        RegisterTrigger(new CubeProximityTrigger(CubeProximityTrigger.ProximityEvent.CubeTypeSpecific, 2f, CubeType.Infinity, 6f));
        
        Debug.Log($"[ContextTriggerManager] Initialized {registeredTriggers.Count} default triggers");
    }

    public void RegisterTrigger(ContextTrigger trigger)
    {
        if (trigger != null && !registeredTriggers.Contains(trigger))
        {
            registeredTriggers.Add(trigger);
            Debug.Log($"[ContextTriggerManager] Registered trigger: {trigger.triggerName}");
        }
    }

    public void UnregisterTrigger(ContextTrigger trigger)
    {
        if (registeredTriggers.Remove(trigger))
        {
            Debug.Log($"[ContextTriggerManager] Unregistered trigger: {trigger.triggerName}");
        }
    }

    public List<TutorialMessage> EvaluateTriggersAndGetMessages(GameContext context)
    {
        // Reset frame counter if this is a new frame
        if (Time.time != lastEvaluationTime)
        {
            triggerEvaluationsThisFrame = 0;
            lastEvaluationTime = Time.time;
        }

        var triggeredMessages = new List<TutorialMessage>();
        
        foreach (var trigger in registeredTriggers)
        {
            // Performance limiting - don't evaluate too many triggers per frame
            if (triggerEvaluationsThisFrame >= maxEvaluationsPerFrame)
                break;
                
            if (!trigger.CanTrigger())
                continue;

            triggerEvaluationsThisFrame++;
            
            try
            {
                if (trigger.EvaluateCondition(context))
                {
                    trigger.OnTriggered();
                    
                    if (messageDatabase != null)
                    {
                        var messages = trigger.GetTriggeredMessages(messageDatabase, context);
                        triggeredMessages.AddRange(messages);
                        
                        Debug.Log($"[ContextTriggerManager] Trigger '{trigger.triggerName}' fired, found {messages.Count} messages");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContextTriggerManager] Error evaluating trigger '{trigger.triggerName}': {ex.Message}");
            }
        }

        return triggeredMessages;
    }

    public GameContext BuildGameContext()
    {
        var context = new GameContext();

        // Populate context from cached manager references
        if (playerManager != null)
        {
            context.playerPosition = playerManager.currentTilePosition;
        }

        if (playerActionManager != null)
        {
            context.availableMarkers = playerActionManager.GetCurrentLightCharges() + 
                                     playerActionManager.GetCurrentHeavyCharges() + 
                                     playerActionManager.GetCurrentPrimeCharges();
        }

        if (waveManager != null)
        {
            context.currentMoveStep = waveManager.MoveStep;
            context.activeCubeTypes.Clear();
            
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube != null && !context.activeCubeTypes.Contains(cube.type))
                {
                    context.activeCubeTypes.Add(cube.type);
                }
            }
            
            // Calculate nearest cube distance
            context.nearestCubeDistance = CalculateNearestCubeDistance();
        }

        return context;
    }

    private float CalculateNearestCubeDistance()
    {
        if (playerManager == null || waveManager == null) 
            return float.MaxValue;

        Vector2Int playerPos = playerManager.currentTilePosition;
        float nearestDistance = float.MaxValue;

        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null)
            {
                float distance = Vector2Int.Distance(playerPos, cube.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }
        }

        return nearestDistance;
    }

    public void SetTriggerEnabled(string triggerName, bool enabled)
    {
        var trigger = registeredTriggers.FirstOrDefault(t => t.triggerName == triggerName);
        if (trigger != null)
        {
            trigger.isEnabled = enabled;
            Debug.Log($"[ContextTriggerManager] Trigger '{triggerName}' {(enabled ? "enabled" : "disabled")}");
        }
    }

    public void ResetAllTriggers()
    {
        foreach (var trigger in registeredTriggers)
        {
            trigger.lastTriggeredTime = 0f;
        }
        Debug.Log("[ContextTriggerManager] All triggers reset");
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Registered Triggers"] = registeredTriggers.Count,
            ["Enabled Triggers"] = registeredTriggers.Count(t => t.isEnabled),
            ["Evaluations This Frame"] = triggerEvaluationsThisFrame,
            ["Max Evaluations Per Frame"] = maxEvaluationsPerFrame,
            ["Manager References Valid"] = (waveManager != null && playerManager != null && playerActionManager != null),
            ["Active Trigger Names"] = string.Join(", ", registeredTriggers.Where(t => t.isEnabled).Select(t => t.triggerName))
        };
    }
}
