using System;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Centralized database for all tutorial and guidance messages in InfinityQube.
/// Provides organized access to messages by category, context, and importance.
/// </summary>
[CreateAssetMenu(fileName = "MessageDatabase", menuName = "Infinity Qube/Message Database")]
public class MessageDatabase : ScriptableObject
{
    [Header("Message Collections")]
    [Tooltip("Essential messages that block gameplay until acknowledged")]
    public List<TutorialMessage> essentialMessages = new List<TutorialMessage>();
    
    [Tooltip("Important guidance messages shown prominently")]
    public List<TutorialMessage> importantMessages = new List<TutorialMessage>();
    
    [Tooltip("Contextual hints that enhance understanding")]
    public List<TutorialMessage> contextualMessages = new List<TutorialMessage>();
    
    [Tooltip("Debug and development messages")]
    public List<TutorialMessage> debugMessages = new List<TutorialMessage>();

    [Header("Legacy Message Migration")]
    [Tooltip("Existing WaveMessage instances for backward compatibility")]
    public List<CategorizedWaveMessage> migratedMessages = new List<CategorizedWaveMessage>();

    [Header("Database Statistics")]
    [SerializeField, Tooltip("Total messages in database")]
    private int totalMessageCount;
    
    [SerializeField, Tooltip("Messages missing IDs")]
    private int messagesWithoutIds;

    /// <summary>
    /// Wrapper for existing WaveMessage instances with category assignment
    /// </summary>
    [Serializable]
    public class CategorizedWaveMessage
    {
        public WaveMessage originalMessage;
        public MessageCategory assignedCategory;
        public string sourceLocation; // Which wave/stage this came from
        public string analysisNotes;  // Categorization reasoning
    }

    /// <summary>
    /// Get all messages of a specific category
    /// </summary>
    public List<TutorialMessage> GetMessagesByCategory(MessageCategory category)
    {
        switch (category)
        {
            case MessageCategory.Essential:
                return essentialMessages;
            case MessageCategory.Important:
                return importantMessages;
            case MessageCategory.Contextual:
                return contextualMessages;
            case MessageCategory.Debug:
                return debugMessages;
            default:
                return new List<TutorialMessage>();
        }
    }

    /// <summary>
    /// Find a message by its unique ID
    /// </summary>
    public TutorialMessage FindMessageById(string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return null;

        foreach (var category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = GetMessagesByCategory((MessageCategory)category);
            foreach (var message in messages)
            {
                if (message.messageId == messageId)
                    return message;
            }
        }
        return null;
    }

    /// <summary>
    /// Get messages appropriate for current game context
    /// </summary>
    public List<TutorialMessage> GetContextualMessages(GameContext context, MessageCategory maxCategory = MessageCategory.Debug)
    {
        var result = new List<TutorialMessage>();
        
        // Check each category up to the specified maximum
        for (int i = 0; i <= (int)maxCategory; i++)
        {
            var category = (MessageCategory)i;
            var messages = GetMessagesByCategory(category);
            
            foreach (var message in messages)
            {
                if (message.ShouldDisplay(context))
                {
                    result.Add(message);
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// Validate database integrity and update statistics
    /// </summary>
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        totalMessageCount = 0;
        messagesWithoutIds = 0;
        var allIds = new HashSet<string>();
        var duplicateIds = new List<string>();

        // Check all categories
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = GetMessagesByCategory(category);
            totalMessageCount += messages.Count;

            foreach (var message in messages)
            {
                if (string.IsNullOrEmpty(message.messageId))
                {
                    messagesWithoutIds++;
                }
                else
                {
                    if (allIds.Contains(message.messageId))
                    {
                        duplicateIds.Add(message.messageId);
                    }
                    else
                    {
                        allIds.Add(message.messageId);
                    }
                }
            }
        }

        if (duplicateIds.Count > 0)
        {
            Debug.LogWarning($"MessageDatabase: Found {duplicateIds.Count} duplicate message IDs: {string.Join(", ", duplicateIds)}");
        }

        Debug.Log($"MessageDatabase Validation Complete:\n" +
                  $"Total Messages: {totalMessageCount}\n" +
                  $"Messages without IDs: {messagesWithoutIds}\n" +
                  $"Duplicate IDs: {duplicateIds.Count}");
    }

    /// <summary>
    /// Generate unique message ID based on content hash
    /// </summary>
    public static string GenerateMessageId(string messageText)
    {
        if (string.IsNullOrEmpty(messageText)) return string.Empty;
        
        // Simple hash-based ID generation
        var hash = messageText.GetHashCode();
        return $"msg_{Math.Abs(hash):X8}";
    }

    private void OnValidate()
    {
        // Auto-generate IDs for messages that don't have them
        bool needsUpdate = false;
        
        foreach (MessageCategory category in System.Enum.GetValues(typeof(MessageCategory)))
        {
            var messages = GetMessagesByCategory(category);
            foreach (var message in messages)
            {
                if (string.IsNullOrEmpty(message.messageId) && !string.IsNullOrEmpty(message.Message))
                {
                    message.messageId = GenerateMessageId(message.Message);
                    needsUpdate = true;
                }
            }
        }

        if (needsUpdate)
        {
            Debug.Log("MessageDatabase: Auto-generated missing message IDs");
        }
    }
}
