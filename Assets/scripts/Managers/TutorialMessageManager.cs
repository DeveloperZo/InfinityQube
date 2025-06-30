using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Enumerations;

/// <summary>
/// Manages contextual tutorial and guidance messages throughout the game.
/// Extends IMessageSystem with sophisticated timing, context awareness, and progress tracking.
/// Follows singleton pattern and integrates with existing manager architecture.
/// </summary>
public class TutorialMessageManager : MonoBehaviour, IMessageSystem, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Database Configuration")]
    [SerializeField, Tooltip("Central message database containing all tutorial content")]
    private MessageDatabase messageDatabase;
    
    [Header("Timing Configuration")]
    [SerializeField, Tooltip("Minimum seconds between any messages")]
    private float messageCooldown = 3f;
    
    [SerializeField, Tooltip("Maximum number of messages in queue")]
    private int maxQueueSize = 5;
    
    [Header("UI References")]
    [SerializeField, Tooltip("Panel for displaying tutorial messages")]
    private GameObject messagePanel;
    
    [SerializeField, Tooltip("Text component for message content")]
    private TMPro.TextMeshProUGUI messageText;
    
    [SerializeField, Tooltip("Continue prompt for pause messages")]
    private GameObject continuePrompt;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool enableContextualMessages = true;
    public bool showTestMessages = false;
    #endregion

    #region Manager References
    private WaveManager waveManager;
    private GridManager gridManager;
    private PlayerManager playerManager;
    private PlayerActionManager playerActionManager;
    private StageManager stageManager;
    private MessageProgressTracker progressTracker;
    #endregion

    #region Runtime State
    // Message Queue and Processing
    private Queue<TutorialMessage> messageQueue = new Queue<TutorialMessage>();
    private bool isProcessingQueue = false;
    private int currentMessageId = 0;
    private float lastMessageTime = 0f;
    
    // Progress Tracking
    private HashSet<string> shownOnceMessages = new HashSet<string>();
    private Dictionary<string, float> lastMessageTimes = new Dictionary<string, float>();
    
    // Context Monitoring and Triggers
    private GameContext currentContext = new GameContext();
    private ContextTriggerManager triggerManager;
    private Coroutine contextUpdateCoroutine;
    
    // Statistics
    private int messagesDisplayed = 0;
    private int messagesSkipped = 0;
    private int messagesQueued = 0;
    private int triggersEvaluated = 0;
    #endregion

    #region Properties
    public static TutorialMessageManager Instance { get; private set; }
    public bool IsShowingMessage => messagePanel != null && messagePanel.activeInHierarchy;
    public int QueuedMessageCount => messageQueue.Count;
    public int ShownOnceCount => shownOnceMessages.Count;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        ValidateConfiguration();
    }

    private void Start()
    {
        CacheManagerReferences();
        InitializeMessageSystem();
        StartContextMonitoring();
    }

    private void OnDestroy()
    {
        CleanupMessageSystem();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DebugLog("InitializeSingleton", "Multiple TutorialMessageManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void ValidateConfiguration()
    {
        if (messageDatabase == null)
        {
            DebugLog("ValidateConfiguration", "MessageDatabase not assigned - tutorial messages will not work!");
            enabled = false;
            return;
        }

        if (messagePanel == null || messageText == null)
        {
            DebugLog("ValidateConfiguration", "UI components not assigned - messages cannot be displayed!");
        }

        messageCooldown = Mathf.Max(0.5f, messageCooldown);
        maxQueueSize = Mathf.Max(1, maxQueueSize);
    }

    private void CacheManagerReferences()
    {
        waveManager = FindObjectOfType<WaveManager>();
        gridManager = GridManager.Instance;
        playerManager = FindObjectOfType<PlayerManager>();
        playerActionManager = FindObjectOfType<PlayerActionManager>();
        stageManager = FindObjectOfType<StageManager>();
        progressTracker = MessageProgressTracker.Instance;

        ValidateManagerReferences();
        
        // Cache references in trigger manager if it exists
        if (triggerManager != null)
        {
            triggerManager.CacheManagerReferences(waveManager, playerManager, playerActionManager, gridManager);
        }
    }

    private void ValidateManagerReferences()
    {
        if (waveManager == null) 
            DebugLog("ValidateManagerReferences", "WaveManager not found - wave context unavailable");
        if (gridManager == null) 
            DebugLog("ValidateManagerReferences", "GridManager not found - grid context unavailable");
        if (playerManager == null) 
            DebugLog("ValidateManagerReferences", "PlayerManager not found - player context unavailable");
        if (playerActionManager == null) 
            DebugLog("ValidateManagerReferences", "PlayerActionManager not found - action context unavailable");
        if (progressTracker == null) 
            DebugLog("ValidateManagerReferences", "MessageProgressTracker not found - progress tracking limited");
    }

    private void InitializeMessageSystem()
    {
        if (messagePanel != null) 
            messagePanel.SetActive(false);
        
        // Initialize with clean state
        messageQueue.Clear();
        shownOnceMessages.Clear();
        lastMessageTimes.Clear();
        
        messagesDisplayed = 0;
        messagesSkipped = 0;
        messagesQueued = 0;
        triggersEvaluated = 0;

        // Initialize context trigger system
        InitializeContextTriggerSystem();

        DebugLog("InitializeMessageSystem", "Tutorial message system initialized");
    }

    private void InitializeContextTriggerSystem()
    {
        if (messageDatabase != null)
        {
            triggerManager = new ContextTriggerManager(messageDatabase);
            DebugLog("InitializeContextTriggerSystem", "Context trigger system initialized");
        }
        else
        {
            DebugLog("InitializeContextTriggerSystem", "Cannot initialize trigger system - no message database");
        }
    }

    private void StartContextMonitoring()
    {
        if (contextUpdateCoroutine != null)
            StopCoroutine(contextUpdateCoroutine);
        
        contextUpdateCoroutine = StartCoroutine(UpdateGameContext());
    }
    #endregion

    #region IMessageSystem Implementation
    public void ShowMessage(string text, bool requireConfirmation, float autoHideDelay)
    {
        // Create a simple tutorial message for compatibility
        var tutorialMessage = new TutorialMessage
        {
            Message = text,
            RequirePause = requireConfirmation,
            AutoHideDelay = autoHideDelay,
            category = MessageCategory.Important,
            messageId = MessageDatabase.GenerateMessageId(text)
        };

        QueueMessage(tutorialMessage);
    }

    public void HideMessage(int messageId)
    {
        if (IsShowingMessage && currentMessageId == messageId)
        {
            StartCoroutine(HideCurrentMessage());
        }
    }

    public void HideAllMessages()
    {
        messageQueue.Clear();
        if (IsShowingMessage)
        {
            StartCoroutine(HideCurrentMessage());
        }
        DebugLog("HideAllMessages", "All messages hidden and queue cleared");
    }

    public int GetActiveMessageCount()
    {
        return QueuedMessageCount + (IsShowingMessage ? 1 : 0);
    }

    public event System.Action<int> OnMessageClosed;
    #endregion

    #region Message Queue Management
    public void QueueMessage(TutorialMessage message)
    {
        if (message == null) return;

        // Check if message should be filtered
        if (!ShouldDisplayMessage(message))
        {
            messagesSkipped++;
            DebugLog("QueueMessage", $"Message skipped: {message.messageId}");
            return;
        }

        // Manage queue size
        if (messageQueue.Count >= maxQueueSize)
        {
            var droppedMessage = messageQueue.Dequeue();
            DebugLog("QueueMessage", $"Queue full - dropped message: {droppedMessage.messageId}");
        }

        messageQueue.Enqueue(message);
        messagesQueued++;
        
        DebugLog("QueueMessage", $"Message queued: {message.messageId} (Queue: {messageQueue.Count})");

        // Start processing if not already running
        if (!isProcessingQueue && enableContextualMessages)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }

    private bool ShouldDisplayMessage(TutorialMessage message)
    {
        // Use MessageProgressTracker if available for sophisticated filtering
        if (progressTracker != null)
        {
            return progressTracker.CanShowMessage(message);
        }
        
        // Fallback to original logic if progress tracker unavailable
        // Check global cooldown
        if (Time.time - lastMessageTime < messageCooldown)
            return false;

        // Check one-time messages
        if (message.showOnce && shownOnceMessages.Contains(message.messageId))
            return false;

        // Check message-specific cooldown
        if (lastMessageTimes.ContainsKey(message.messageId))
        {
            float timeSinceLastShow = Time.time - lastMessageTimes[message.messageId];
            if (timeSinceLastShow < message.cooldownSeconds)
                return false;
        }

        // Check context triggers
        if (!message.ShouldDisplay(currentContext))
            return false;

        return true;
    }

    private IEnumerator ProcessMessageQueue()
    {
        isProcessingQueue = true;

        while (messageQueue.Count > 0 && enableContextualMessages)
        {
            var message = messageQueue.Dequeue();
            
            // Double-check if message should still be displayed
            if (ShouldDisplayMessage(message))
            {
                yield return DisplayMessage(message);
                
                // Update tracking
                lastMessageTime = Time.time;
                lastMessageTimes[message.messageId] = Time.time;
                
                if (message.showOnce)
                    shownOnceMessages.Add(message.messageId);
                
                messagesDisplayed++;
            }
            else
            {
                messagesSkipped++;
                DebugLog("ProcessMessageQueue", $"Message no longer valid: {message.messageId}");
            }

            yield return null; // Allow frame processing
        }

        isProcessingQueue = false;
    }

    private IEnumerator DisplayMessage(TutorialMessage message)
    {
        if (messagePanel == null || messageText == null)
            yield break;

        currentMessageId++;
        
        // Setup message display
        bool isRepeat = shownOnceMessages.Contains(message.messageId);
        string displayText = message.GetDisplayMessage(isRepeat);
        
        messagePanel.SetActive(true);
        messageText.text = displayText;
        
        if (continuePrompt != null)
            continuePrompt.SetActive(message.RequirePause);

        DebugLog("DisplayMessage", $"Showing message: {message.messageId}");
        
        // Notify progress tracker that message is being shown
        if (progressTracker != null)
        {
            progressTracker.OnMessageShown(message);
        }
        
        // Notify statistics manager about message display
        if (PlayerStatisticsManager.Instance != null)
        {
            int currentStep = waveManager != null ? waveManager.MoveStep : 0;
            PlayerStatisticsManager.Instance.OnMessageDisplayed(message.Message, currentStep);
        }

        bool wasSkipped = false;
        
        // Handle message timing
        if (message.RequirePause)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
        }
        else if (message.AutoHideDelay > 0)
        {
            float timer = 0f;
            while (timer < message.AutoHideDelay)
            {
                if (Input.GetKeyDown(KeyCode.K)) // Allow skipping
                {
                    wasSkipped = true;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }
        }
        
        // Notify statistics manager about message dismissal
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMessageDismissed(message.Message, wasSkipped);
        }

        yield return HideCurrentMessage();
    }

    private IEnumerator HideCurrentMessage()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);
        
        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        OnMessageClosed?.Invoke(currentMessageId);
        
        DebugLog("HideCurrentMessage", $"Message hidden: ID {currentMessageId}");
        yield return null;
    }
    #endregion

    #region Context Monitoring
    private IEnumerator UpdateGameContext()
    {
        while (enabled)
        {
            UpdateContextData();
            
            // Check for contextual messages
            if (enableContextualMessages && messageDatabase != null)
            {
                CheckContextualTriggers();
            }

            yield return new WaitForSeconds(0.5f); // Update context twice per second
        }
    }

    private void UpdateContextData()
    {
        // Update player context
        if (playerManager != null)
        {
            currentContext.playerPosition = playerManager.currentTilePosition;
        }

        // Update marker context
        if (playerActionManager != null)
        {
            currentContext.availableMarkers = playerActionManager.maxHeavyMarkerCharges + playerActionManager.maxLightMarkerCharges + playerActionManager.maxPrimeMarkerCharges;
        }

        // Update wave context
        if (waveManager != null)
        {
            currentContext.currentMoveStep = waveManager.MoveStep;
            currentContext.activeCubeTypes.Clear();
            
            foreach (var cube in waveManager.activeCubes)
            {
                if (cube != null && !currentContext.activeCubeTypes.Contains(cube.type))
                {
                    currentContext.activeCubeTypes.Add(cube.type);
                }
            }
        }

        // Update proximity context
        UpdateNearestCubeDistance();
    }

    private void UpdateNearestCubeDistance()
    {
        currentContext.nearestCubeDistance = float.MaxValue;

        if (waveManager == null || playerManager == null || gridManager == null)
            return;

        Vector2Int playerPos = currentContext.playerPosition;
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null)
            {
                float distance = Vector2Int.Distance(playerPos, cube.position);
                if (distance < currentContext.nearestCubeDistance)
                {
                    currentContext.nearestCubeDistance = distance;
                }
            }
        }
    }

    private void CheckContextualTriggers()
    {
        // Use new trigger system if available
        if (triggerManager != null)
        {
            // Build current context from cached manager data
            currentContext = triggerManager.BuildGameContext();
            
            // Evaluate triggers and get triggered messages
            var triggeredMessages = triggerManager.EvaluateTriggersAndGetMessages(currentContext);
            triggersEvaluated += triggeredMessages.Count;
            
            // Filter messages through progress tracker if available
            if (progressTracker != null)
            {
                var filteredMessages = progressTracker.FilterAndPrioritizeMessages(triggeredMessages);
                
                // Queue filtered and prioritized messages
                foreach (var message in filteredMessages)
                {
                    QueueMessage(message);
                    DebugLog("CheckContextualTriggers", $"Queued triggered message: {message.messageId}");
                }
            }
            else
            {
                // Fallback to simple filtering
                foreach (var message in triggeredMessages)
                {
                    if (ShouldDisplayMessage(message))
                    {
                        QueueMessage(message);
                        DebugLog("CheckContextualTriggers", $"Queued triggered message: {message.messageId}");
                    }
                }
            }
        }
        else
        {
            // Fallback to original contextual message system
            var contextualMessages = messageDatabase.GetContextualMessages(currentContext, MessageCategory.Contextual);
            
            foreach (var message in contextualMessages)
            {
                // Only queue if not already processed recently
                if (ShouldDisplayMessage(message))
                {
                    QueueMessage(message);
                    break; // Only queue one contextual message per update
                }
            }
        }
    }
    #endregion

    #region Public Interface Methods
    public void ShowEssentialMessage(string messageId)
    {
        var message = messageDatabase?.FindMessageById(messageId);
        if (message != null && message.category == MessageCategory.Essential)
        {
            QueueMessage(message);
        }
        else
        {
            DebugLog("ShowEssentialMessage", $"Essential message not found: {messageId}");
        }
    }

    public void ShowContextualHint(MessageCategory maxCategory = MessageCategory.Important)
    {
        if (messageDatabase == null) return;

        var availableMessages = messageDatabase.GetContextualMessages(currentContext, maxCategory);
        var unshownMessages = availableMessages.Where(m => !shownOnceMessages.Contains(m.messageId) || !m.showOnce).ToList();
        
        if (unshownMessages.Count > 0)
        {
            var selectedMessage = unshownMessages[Random.Range(0, unshownMessages.Count)];
            QueueMessage(selectedMessage);
        }
    }

    public void ClearProgress()
    {
        shownOnceMessages.Clear();
        lastMessageTimes.Clear();
        
        // Reset trigger system if available
        if (triggerManager != null)
        {
            triggerManager.ResetAllTriggers();
        }
        
        // Reset progress tracker if available
        if (progressTracker != null)
        {
            progressTracker.ClearAllOneTimeMessages();
        }
        
        DebugLog("ClearProgress", "Tutorial progress cleared");
    }

    public void RegisterCustomTrigger(ContextTrigger trigger)
    {
        if (triggerManager != null)
        {
            triggerManager.RegisterTrigger(trigger);
            DebugLog("RegisterCustomTrigger", $"Registered custom trigger: {trigger.triggerName}");
        }
        else
        {
            DebugLog("RegisterCustomTrigger", "Cannot register trigger - trigger manager not initialized");
        }
    }

    public void SetTriggerEnabled(string triggerName, bool enabled)
    {
        if (triggerManager != null)
        {
            triggerManager.SetTriggerEnabled(triggerName, enabled);
            DebugLog("SetTriggerEnabled", $"Trigger '{triggerName}' {(enabled ? "enabled" : "disabled")}");
        }
        else
        {
            DebugLog("SetTriggerEnabled", "Cannot modify trigger - trigger manager not initialized");
        }
    }

    public GameContext GetCurrentContext()
    {
        if (triggerManager != null)
        {
            return triggerManager.BuildGameContext();
        }
        return currentContext;
    }
    #endregion

    #region Cleanup
    private void CleanupMessageSystem()
    {
        if (contextUpdateCoroutine != null)
        {
            StopCoroutine(contextUpdateCoroutine);
            contextUpdateCoroutine = null;
        }

        messageQueue.Clear();
        isProcessingQueue = false;
        
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }
    #endregion

    #region Debug Support
    private void DebugLog(string methodName, string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[TutorialMessageManager] {methodName}: {message}");
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
        string status = isProcessingQueue ? "PROCESSING" : "IDLE";
        string showing = IsShowingMessage ? "SHOWING" : "HIDDEN";
        return $"Tutorial: {status} ({showing}) Queue:{QueuedMessageCount} Shown:{messagesDisplayed} Progress:{ShownOnceCount}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Is Processing Queue"] = isProcessingQueue,
            ["Is Showing Message"] = IsShowingMessage,
            ["Queued Messages"] = QueuedMessageCount,
            ["Messages Displayed"] = messagesDisplayed,
            ["Messages Skipped"] = messagesSkipped,
            ["Messages Queued Total"] = messagesQueued,
            ["One-Time Progress"] = ShownOnceCount,
            ["Message Cooldown"] = messageCooldown,
            ["Max Queue Size"] = maxQueueSize,
            ["Enable Contextual"] = enableContextualMessages,
            ["Current Context Available Markers"] = currentContext.availableMarkers,
            ["Current Context Move Step"] = currentContext.currentMoveStep,
            ["Current Context Active Cube Types"] = currentContext.activeCubeTypes.Count,
            ["Current Context Nearest Cube"] = currentContext.nearestCubeDistance,
            ["Database Assigned"] = messageDatabase != null,
            ["Last Message Time"] = lastMessageTime,
            ["Triggers Evaluated"] = triggersEvaluated,
            ["Trigger Manager Active"] = triggerManager != null,
            ["Progress Tracker Active"] = progressTracker != null,
            ["Progress Tracker Status"] = progressTracker?.GetDebugStatus() ?? "Not Available",
            ["Manager References Valid"] = (waveManager != null) && (gridManager != null) && (playerManager != null)
        };
    }

    public void ResetToDefaults()
    {
        // Stop processing
        CleanupMessageSystem();
        
        // Reset statistics
        messagesDisplayed = 0;
        messagesSkipped = 0;
        messagesQueued = 0;
        triggersEvaluated = 0;
        currentMessageId = 0;
        lastMessageTime = 0f;
        
        // Clear progress tracking
        ClearProgress();
        
        // Restart system
        InitializeMessageSystem();
        StartContextMonitoring();
        
        DebugLog("ResetToDefaults", "Reset to defaults completed");
    }

    public void LoadConfiguration(string configName)
    {
        DebugLog("LoadConfiguration", $"Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        DebugLog("SaveConfiguration", $"Saving configuration: {configName} (not yet implemented)");
    }
    #endregion
}
