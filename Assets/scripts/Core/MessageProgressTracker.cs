using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Enumerations;

/// <summary>
/// Handles persistence and tracking of tutorial message progress, including one-time messages,
/// cooldown management, and message priority processing.
/// </summary>
[Serializable]
public class MessageProgressTracker : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Progress Persistence")]
    [SerializeField, Tooltip("Use PlayerPrefs for quick POC implementation")]
    private bool usePlayerPrefs = true;
    
    [SerializeField, Tooltip("Use JSON file for more robust persistence (future)")]
    private bool useJsonPersistence = false;
    
    [Header("Timing Configuration")]
    [SerializeField, Tooltip("Global cooldown between any messages")]
    private float globalMessageCooldown = 3f;
    
    [SerializeField, Tooltip("Priority-based cooldown multipliers")]
    private PriorityCooldownSettings priorityCooldowns = new PriorityCooldownSettings();
    
    [Header("Message Limiting")]
    [SerializeField, Tooltip("Maximum messages per minute during gameplay")]
    private int maxMessagesPerMinute = 8;
    
    [SerializeField, Tooltip("Essential messages can bypass frequency limits")]
    private bool essentialBypassLimits = true;
    
    [Header("Debug")]
    public bool showProgressDetails = false;
    #endregion

    #region Runtime State
    // Progress Tracking
    private PlayerProgressData currentProgress;
    private Dictionary<string, float> messageCooldowns = new Dictionary<string, float>();
    private Queue<DateTime> recentMessageTimes = new Queue<DateTime>();
    
    // Message Priority System
    private Dictionary<MessageCategory, int> priorityWeights = new Dictionary<MessageCategory, int>
    {
        { MessageCategory.Essential, 100 },
        { MessageCategory.Important, 75 },
        { MessageCategory.Contextual, 50 },
        { MessageCategory.Debug, 25 }
    };

    // Statistics
    private int messagesProcessed = 0;
    private int messagesBlocked = 0;
    private int oneTimeMessagesShown = 0;
    private float lastSaveTime = 0f;
    private float autoSaveInterval = 30f;
    #endregion

    #region Properties
    public static MessageProgressTracker Instance { get; private set; }
    public PlayerProgressData CurrentProgress => currentProgress;
    public int MessagesProcessed => messagesProcessed;
    public int MessagesBlocked => messagesBlocked;
    public bool IsFrequencyLimited => GetRecentMessageCount() >= maxMessagesPerMinute;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializeProgress();
    }

    private void Start()
    {
        EnableDebugLogs = true;
        LoadProgress();
        StartAutoSave();
    }

    private void Update()
    {
        UpdateCooldowns();
        HandleAutoSave();
        CleanupRecentMessages();
    }

    private void OnDestroy()
    {
        SaveProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveProgress();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeProgress()
    {
        currentProgress = new PlayerProgressData();
        messageCooldowns.Clear();
        recentMessageTimes.Clear();
        
        messagesProcessed = 0;
        messagesBlocked = 0;
        oneTimeMessagesShown = 0;
        lastSaveTime = Time.time;

        DebugLog("InitializeProgress", "Message progress tracker initialized");
    }

    private void StartAutoSave()
    {
        // Auto-save will be handled in Update loop
        DebugLog("StartAutoSave", $"Auto-save enabled with {autoSaveInterval}s interval");
    }
    #endregion

    #region Message Processing
    public bool CanShowMessage(TutorialMessage message)
    {
        if (message == null) return false;

        // Check one-time message tracking
        if (message.showOnce && HasMessageBeenShown(message.messageId))
        {
            DebugLog("CanShowMessage", $"One-time message already shown: {message.messageId}");
            return false;
        }

        // Check global cooldown
        if (!IsGlobalCooldownExpired())
        {
            DebugLog("CanShowMessage", "Global cooldown active");
            return false;
        }

        // Check message-specific cooldown
        if (!IsMessageCooldownExpired(message.messageId, message.cooldownSeconds))
        {
            DebugLog("CanShowMessage", $"Message cooldown active: {message.messageId}");
            return false;
        }

        // Check frequency limiting (Essential messages can bypass)
        if (IsFrequencyLimited && message.category != MessageCategory.Essential && !essentialBypassLimits)
        {
            DebugLog("CanShowMessage", "Frequency limit reached");
            return false;
        }

        // Check priority-based cooldown
        if (!IsPriorityCooldownExpired(message.category))
        {
            DebugLog("CanShowMessage", $"Priority cooldown active for {message.category}");
            return false;
        }

        return true;
    }

    public void OnMessageShown(TutorialMessage message)
    {
        if (message == null) return;

        // Track one-time messages
        if (message.showOnce)
        {
            MarkMessageAsShown(message.messageId);
            oneTimeMessagesShown++;
        }

        // Update cooldowns
        SetMessageCooldown(message.messageId, message.cooldownSeconds);
        SetGlobalCooldown();
        SetPriorityCooldown(message.category);

        // Track recent message times for frequency limiting
        recentMessageTimes.Enqueue(DateTime.Now);

        // Update progress statistics
        UpdateMessageProgress(message);

        messagesProcessed++;
        DebugLog("OnMessageShown", $"Message processed: {message.messageId} (Category: {message.category})");
    }

    public void OnMessageBlocked(TutorialMessage message, string reason)
    {
        if (message == null) return;

        messagesBlocked++;
        
        // Track blocked message for analytics
        TrackBlockedMessage(message, reason);
        
        DebugLog("OnMessageBlocked", $"Message blocked: {message.messageId}, Reason: {reason}");
    }

    public List<TutorialMessage> FilterAndPrioritizeMessages(List<TutorialMessage> messages)
    {
        if (messages == null || messages.Count == 0) return new List<TutorialMessage>();

        var filteredMessages = new List<TutorialMessage>();

        foreach (var message in messages)
        {
            if (CanShowMessage(message))
            {
                filteredMessages.Add(message);
            }
            else
            {
                OnMessageBlocked(message, GetBlockReason(message));
            }
        }

        // Sort by priority and timing
        return PrioritizeMessages(filteredMessages);
    }

    private List<TutorialMessage> PrioritizeMessages(List<TutorialMessage> messages)
    {
        return messages
            .OrderByDescending(m => priorityWeights.ContainsKey(m.category) ? priorityWeights[m.category] : 0)
            .ThenBy(m => GetLastShownTime(m.messageId))
            .ToList();
    }

    private string GetBlockReason(TutorialMessage message)
    {
        if (message.showOnce && HasMessageBeenShown(message.messageId))
            return "One-time message already shown";
        
        if (!IsGlobalCooldownExpired())
            return "Global cooldown active";
        
        if (!IsMessageCooldownExpired(message.messageId, message.cooldownSeconds))
            return "Message cooldown active";
        
        if (IsFrequencyLimited && message.category != MessageCategory.Essential)
            return "Frequency limit reached";
        
        if (!IsPriorityCooldownExpired(message.category))
            return "Priority cooldown active";
        
        return "Unknown reason";
    }
    #endregion

    #region Cooldown Management
    private bool IsGlobalCooldownExpired()
    {
        return Time.time >= GetGlobalCooldownExpireTime();
    }

    private bool IsMessageCooldownExpired(string messageId, float cooldown)
    {
        if (!messageCooldowns.ContainsKey(messageId)) return true;
        return Time.time >= messageCooldowns[messageId] + cooldown;
    }

    private bool IsPriorityCooldownExpired(MessageCategory category)
    {
        float categoryMultiplier = priorityCooldowns.GetMultiplier(category);
        float priorityCooldown = globalMessageCooldown * categoryMultiplier;
        
        string categoryKey = $"priority_{category}";
        if (!messageCooldowns.ContainsKey(categoryKey)) return true;
        
        return Time.time >= messageCooldowns[categoryKey] + priorityCooldown;
    }

    private void SetGlobalCooldown()
    {
        messageCooldowns["global"] = Time.time;
    }

    private void SetMessageCooldown(string messageId, float cooldown)
    {
        messageCooldowns[messageId] = Time.time;
    }

    private void SetPriorityCooldown(MessageCategory category)
    {
        string categoryKey = $"priority_{category}";
        messageCooldowns[categoryKey] = Time.time;
    }

    private float GetGlobalCooldownExpireTime()
    {
        return messageCooldowns.ContainsKey("global") ? 
               messageCooldowns["global"] + globalMessageCooldown : 0f;
    }

    private void UpdateCooldowns()
    {
        // Cleanup expired cooldowns for memory management
        var expiredKeys = messageCooldowns
            .Where(kvp => Time.time > kvp.Value + 300f) // 5 minutes old
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            messageCooldowns.Remove(key);
        }
    }
    #endregion

    #region One-Time Message Tracking
    public bool HasMessageBeenShown(string messageId)
    {
        return currentProgress.shownMessages.Contains(messageId);
    }

    public void MarkMessageAsShown(string messageId)
    {
        if (!currentProgress.shownMessages.Contains(messageId))
        {
            currentProgress.shownMessages.Add(messageId);
            currentProgress.lastUpdated = DateTime.Now;
            DebugLog("MarkMessageAsShown", $"Marked as shown: {messageId}");
        }
    }

    public void ResetOneTimeMessage(string messageId)
    {
        if (currentProgress.shownMessages.Remove(messageId))
        {
            currentProgress.lastUpdated = DateTime.Now;
            DebugLog("ResetOneTimeMessage", $"Reset one-time status: {messageId}");
        }
    }

    public void ClearAllOneTimeMessages()
    {
        int count = currentProgress.shownMessages.Count;
        currentProgress.shownMessages.Clear();
        currentProgress.lastUpdated = DateTime.Now;
        DebugLog("ClearAllOneTimeMessages", $"Cleared {count} one-time messages");
    }
    #endregion

    #region Frequency Limiting
    private int GetRecentMessageCount()
    {
        CleanupRecentMessages();
        return recentMessageTimes.Count;
    }

    private void CleanupRecentMessages()
    {
        DateTime cutoff = DateTime.Now.AddMinutes(-1);
        while (recentMessageTimes.Count > 0 && recentMessageTimes.Peek() < cutoff)
        {
            recentMessageTimes.Dequeue();
        }
    }
    #endregion

    #region Progress Statistics
    private void UpdateMessageProgress(TutorialMessage message)
    {
        // Update category statistics
        if (!currentProgress.messageStats.ContainsKey(message.category.ToString()))
        {
            currentProgress.messageStats[message.category.ToString()] = 0;
        }
        currentProgress.messageStats[message.category.ToString()]++;

        // Update timing statistics
        currentProgress.totalMessagesShown++;
        currentProgress.lastMessageTime = DateTime.Now;

        // Track message patterns
        TrackMessagePattern(message);
    }

    private void TrackMessagePattern(TutorialMessage message)
    {
        var pattern = new MessagePattern
        {
            messageId = message.messageId,
            category = message.category.ToString(),
            timestamp = DateTime.Now,
            showCount = GetMessageShowCount(message.messageId)
        };

        currentProgress.messagePatterns.Add(pattern);

        // Limit pattern history to prevent memory growth
        if (currentProgress.messagePatterns.Count > 100)
        {
            currentProgress.messagePatterns.RemoveAt(0);
        }
    }

    private int GetMessageShowCount(string messageId)
    {
        return currentProgress.messagePatterns.Count(p => p.messageId == messageId) + 1;
    }

    private float GetLastShownTime(string messageId)
    {
        return messageCooldowns.ContainsKey(messageId) ? messageCooldowns[messageId] : 0f;
    }

    private void TrackBlockedMessage(TutorialMessage message, string reason)
    {
        var blockedEvent = new BlockedMessageEvent
        {
            messageId = message.messageId,
            category = message.category.ToString(),
            reason = reason,
            timestamp = DateTime.Now
        };

        currentProgress.blockedMessages.Add(blockedEvent);

        // Limit blocked message history
        if (currentProgress.blockedMessages.Count > 50)
        {
            currentProgress.blockedMessages.RemoveAt(0);
        }
    }
    #endregion

    #region Persistence System
    public void SaveProgress()
    {
        try
        {
            if (usePlayerPrefs)
            {
                SaveToPlayerPrefs();
            }

            if (useJsonPersistence)
            {
                SaveToJsonFile();
            }

            lastSaveTime = Time.time;
            DebugLog("SaveProgress", "Progress saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MessageProgressTracker] Failed to save progress: {e.Message}");
        }
    }

    public void LoadProgress()
    {
        try
        {
            bool loaded = false;

            if (usePlayerPrefs)
            {
                loaded = LoadFromPlayerPrefs();
            }

            if (!loaded && useJsonPersistence)
            {
                loaded = LoadFromJsonFile();
            }

            if (!loaded)
            {
                DebugLog("LoadProgress", "No existing progress found, starting fresh");
            }
            else
            {
                DebugLog("LoadProgress", $"Progress loaded: {currentProgress.shownMessages.Count} messages tracked");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MessageProgressTracker] Failed to load progress: {e.Message}");
            InitializeProgress(); // Fallback to fresh state
        }
    }

    private void SaveToPlayerPrefs()
    {
        string key = "MessageProgress";
        string jsonData = JsonUtility.ToJson(currentProgress);
        PlayerPrefs.SetString(key, jsonData);
        PlayerPrefs.Save();
    }

    private bool LoadFromPlayerPrefs()
    {
        string key = "MessageProgress";
        if (PlayerPrefs.HasKey(key))
        {
            string jsonData = PlayerPrefs.GetString(key);
            currentProgress = JsonUtility.FromJson<PlayerProgressData>(jsonData);
            return true;
        }
        return false;
    }

    private void SaveToJsonFile()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "tutorial_progress.json");
        string jsonData = JsonUtility.ToJson(currentProgress, true);
        System.IO.File.WriteAllText(filePath, jsonData);
    }

    private bool LoadFromJsonFile()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "tutorial_progress.json");
        if (System.IO.File.Exists(filePath))
        {
            string jsonData = System.IO.File.ReadAllText(filePath);
            currentProgress = JsonUtility.FromJson<PlayerProgressData>(jsonData);
            return true;
        }
        return false;
    }

    private void HandleAutoSave()
    {
        if (Time.time - lastSaveTime >= autoSaveInterval)
        {
            SaveProgress();
        }
    }
    #endregion

    #region PlayerStatisticsManager Integration
    public void SyncWithStatisticsManager()
    {
        var statsManager = PlayerStatisticsManager.Instance;
        if (statsManager != null)
        {
            // Sync tutorial completion data
            currentProgress.tutorialCompletionRate = CalculateTutorialCompletionRate();
            currentProgress.averageMessageReadTime = CalculateAverageMessageReadTime();
            
            DebugLog("SyncWithStatisticsManager", "Synced with PlayerStatisticsManager");
        }
    }

    private float CalculateTutorialCompletionRate()
    {
        // Simple completion rate based on essential messages shown
        int essentialShown = currentProgress.messageStats.ContainsKey("Essential") ? 
                           currentProgress.messageStats["Essential"] : 0;
        
        // Assume 10 essential messages for full tutorial (configurable)
        int totalEssential = 10;
        
        return Mathf.Clamp01((float)essentialShown / totalEssential);
    }

    private float CalculateAverageMessageReadTime()
    {
        // This would need integration with actual message display timing
        // For now, return a placeholder based on message count
        return currentProgress.totalMessagesShown > 0 ? 3.5f : 0f;
    }
    #endregion

    #region Public API
    public void SetGlobalCooldown(float cooldown)
    {
        globalMessageCooldown = Mathf.Max(0.5f, cooldown);
        DebugLog("SetGlobalCooldown", $"Global cooldown set to {globalMessageCooldown}s");
    }

    public void SetMaxMessagesPerMinute(int maxMessages)
    {
        maxMessagesPerMinute = Mathf.Max(1, maxMessages);
        DebugLog("SetMaxMessagesPerMinute", $"Message frequency limit set to {maxMessagesPerMinute}/minute");
    }

    public void ResetAllProgress()
    {
        InitializeProgress();
        SaveProgress();
        DebugLog("ResetAllProgress", "All tutorial progress reset");
    }

    public MessageProgressSummary GetProgressSummary()
    {
        return new MessageProgressSummary
        {
            totalMessagesShown = currentProgress.totalMessagesShown,
            oneTimeMessagesShown = currentProgress.shownMessages.Count,
            messagesProcessed = messagesProcessed,
            messagesBlocked = messagesBlocked,
            tutorialCompletionRate = currentProgress.tutorialCompletionRate,
            lastUpdate = currentProgress.lastUpdated,
            isFrequencyLimited = IsFrequencyLimited,
            recentMessageCount = GetRecentMessageCount()
        };
    }
    #endregion

    #region Utility Methods
    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.Log($"[MessageProgressTracker] {methodName}: {message}");
    }
    #endregion

    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs { get; set; } = true;

    public string GetDebugStatus()
    {
        string limitStatus = IsFrequencyLimited ? "LIMITED" : "NORMAL";
        return $"MessageProgress: {currentProgress.totalMessagesShown} shown, {messagesBlocked} blocked, {limitStatus}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Total Messages Shown"] = currentProgress.totalMessagesShown,
            ["One-Time Messages Tracked"] = currentProgress.shownMessages.Count,
            ["Messages Processed"] = messagesProcessed,
            ["Messages Blocked"] = messagesBlocked,
            ["Is Frequency Limited"] = IsFrequencyLimited,
            ["Recent Message Count"] = GetRecentMessageCount(),
            ["Global Cooldown"] = globalMessageCooldown,
            ["Max Messages Per Minute"] = maxMessagesPerMinute,
            ["Tutorial Completion Rate"] = currentProgress.tutorialCompletionRate,
            ["Average Message Read Time"] = currentProgress.averageMessageReadTime,
            ["Use Player Prefs"] = usePlayerPrefs,
            ["Use JSON Persistence"] = useJsonPersistence,
            ["Last Save Time"] = lastSaveTime,
            ["Auto Save Interval"] = autoSaveInterval,
            ["Active Cooldowns"] = messageCooldowns.Count,
            ["Message Patterns Tracked"] = currentProgress.messagePatterns.Count,
            ["Blocked Messages Tracked"] = currentProgress.blockedMessages.Count
        };
    }

    public void ResetToDefaults()
    {
        // Reset all progress and statistics
        ResetAllProgress();
        
        // Reset runtime counters
        messagesProcessed = 0;
        messagesBlocked = 0;
        oneTimeMessagesShown = 0;
        
        // Clear cooldowns
        messageCooldowns.Clear();
        recentMessageTimes.Clear();
        
        // Reset timing
        lastSaveTime = Time.time;
        
        if (EnableDebugLogs)
            Debug.Log("[MessageProgressTracker] Reset to defaults completed");
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

#region Data Structures
[Serializable]
public class PlayerProgressData
{
    [Header("Message Tracking")]
    public List<string> shownMessages = new List<string>();
    public Dictionary<string, int> messageStats = new Dictionary<string, int>();
    public int totalMessagesShown = 0;
    public DateTime lastMessageTime = DateTime.Now;
    public DateTime lastUpdated = DateTime.Now;
    
    [Header("Analytics")]
    public List<MessagePattern> messagePatterns = new List<MessagePattern>();
    public List<BlockedMessageEvent> blockedMessages = new List<BlockedMessageEvent>();
    public float tutorialCompletionRate = 0f;
    public float averageMessageReadTime = 0f;
    
    [Header("Session Info")]
    public string sessionId = System.Guid.NewGuid().ToString();
    public DateTime sessionStart = DateTime.Now;
}

[Serializable]
public class MessagePattern
{
    public string messageId;
    public string category;
    public DateTime timestamp;
    public int showCount;
}

[Serializable]
public class BlockedMessageEvent
{
    public string messageId;
    public string category;
    public string reason;
    public DateTime timestamp;
}

[Serializable]
public class PriorityCooldownSettings
{
    [SerializeField] private float essentialMultiplier = 0.5f;
    [SerializeField] private float importantMultiplier = 1.0f;
    [SerializeField] private float contextualMultiplier = 1.5f;
    [SerializeField] private float debugMultiplier = 2.0f;
    
    public float GetMultiplier(MessageCategory category)
    {
        switch (category)
        {
            case MessageCategory.Essential: return essentialMultiplier;
            case MessageCategory.Important: return importantMultiplier;
            case MessageCategory.Contextual: return contextualMultiplier;
            case MessageCategory.Debug: return debugMultiplier;
            default: return 1.0f;
        }
    }
}

[Serializable]
public class MessageProgressSummary
{
    public int totalMessagesShown;
    public int oneTimeMessagesShown;
    public int messagesProcessed;
    public int messagesBlocked;
    public float tutorialCompletionRate;
    public DateTime lastUpdate;
    public bool isFrequencyLimited;
    public int recentMessageCount;
}
#endregion
