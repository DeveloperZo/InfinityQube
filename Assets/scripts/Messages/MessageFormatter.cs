using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Utility class for formatting and validating tutorial messages with 2-line constraints,
/// dynamic variable substitution, and action-oriented language processing.
/// Integrates with existing TutorialMessage system for enhanced engagement.
/// </summary>
public static class MessageFormatter
{
    #region Constants
    public const int MAX_LINES = 2;
    public const int MAX_LINE_LENGTH = 50; // Characters per line for readability
    public const string VARIABLE_PATTERN = @"\{(\w+)\}"; // Matches {variableName}
    
    // Action-oriented verb prefixes for message templates
    private static readonly string[] ACTION_VERBS = {
        "Place", "Move", "Trigger", "Light", "Press", "Use", "Avoid", "Target", "Navigate", "Capture"
    };
    
    // Progressive disclosure transition words
    private static readonly string[] TRANSITION_WORDS = {
        "Now", "Next", "Then", "Also", "Remember", "Try", "Continue"
    };
    #endregion

    #region Message Validation
    /// <summary>
    /// Validate that a message meets the 2-line constraint and formatting requirements
    /// </summary>
    public static MessageValidationResult ValidateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return new MessageValidationResult
            {
                IsValid = false,
                ErrorType = MessageValidationError.EmptyMessage,
                ErrorMessage = "Message cannot be empty"
            };
        }

        // Split into lines and check count
        string[] lines = SplitIntoLines(message);
        
        if (lines.Length > MAX_LINES)
        {
            return new MessageValidationResult
            {
                IsValid = false,
                ErrorType = MessageValidationError.TooManyLines,
                ErrorMessage = $"Message has {lines.Length} lines, maximum is {MAX_LINES}",
                SuggestedFix = TruncateToMaxLines(message)
            };
        }

        // Check line length
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > MAX_LINE_LENGTH)
            {
                return new MessageValidationResult
                {
                    IsValid = false,
                    ErrorType = MessageValidationError.LineTooLong,
                    ErrorMessage = $"Line {i + 1} is {lines[i].Length} characters, maximum is {MAX_LINE_LENGTH}",
                    SuggestedFix = WrapLongLines(message)
                };
            }
        }

        // Check for action-oriented structure
        bool isActionOriented = IsActionOriented(message);
        
        return new MessageValidationResult
        {
            IsValid = true,
            IsActionOriented = isActionOriented,
            LineCount = lines.Length,
            MaxLineLength = lines.Max(l => l.Length),
            SuggestedFix = isActionOriented ? null : MakeActionOriented(message)
        };
    }

    /// <summary>
    /// Enforce 2-line limit by truncating or wrapping text intelligently
    /// </summary>
    public static string EnforceTwoLineLimit(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        var validation = ValidateMessage(message);
        if (validation.IsValid) return message;

        // Try suggested fix first
        if (!string.IsNullOrEmpty(validation.SuggestedFix))
        {
            var fixValidation = ValidateMessage(validation.SuggestedFix);
            if (fixValidation.IsValid) return validation.SuggestedFix;
        }

        // Fallback to aggressive truncation
        return TruncateToMaxLines(message);
    }
    #endregion

    #region Dynamic Variable Substitution
    /// <summary>
    /// Process message with dynamic variable substitution based on current game state
    /// </summary>
    public static string ProcessDynamicContent(string messageTemplate, GameContext context, Dictionary<string, object> additionalVariables = null)
    {
        if (string.IsNullOrEmpty(messageTemplate)) return messageTemplate;

        var variables = BuildVariableDict(context, additionalVariables);
        string processedMessage = messageTemplate;

        // Replace all variables using regex
        processedMessage = Regex.Replace(processedMessage, VARIABLE_PATTERN, match =>
        {
            string variableName = match.Groups[1].Value;
            if (variables.TryGetValue(variableName, out object value))
            {
                return FormatVariableValue(value);
            }
            
            // Keep original placeholder if variable not found
            Debug.LogWarning($"MessageFormatter: Variable '{variableName}' not found in context");
            return match.Value;
        });

        return processedMessage;
    }

    /// <summary>
    /// Build dictionary of available variables from game context
    /// </summary>
    private static Dictionary<string, object> BuildVariableDict(GameContext context, Dictionary<string, object> additionalVariables)
    {
        var variables = new Dictionary<string, object>
        {
            ["playerX"] = context.playerPosition.x,
            ["playerY"] = context.playerPosition.y,
            ["markers"] = context.availableMarkers,
            ["step"] = context.currentMoveStep,
            ["cubeDistance"] = context.nearestCubeDistance,
            ["cubeTypes"] = context.activeCubeTypes.Count,
            ["isPaused"] = context.isGamePaused
        };

        // Add cube type specific variables
        foreach (CubeType cubeType in System.Enum.GetValues(typeof(CubeType)))
        {
            bool hasType = context.activeCubeTypes.Contains(cubeType);
            variables[$"has{cubeType}"] = hasType;
        }

        // Merge additional variables
        if (additionalVariables != null)
        {
            foreach (var kvp in additionalVariables)
            {
                variables[kvp.Key] = kvp.Value;
            }
        }

        return variables;
    }

    /// <summary>
    /// Format variable value for display in message
    /// </summary>
    private static string FormatVariableValue(object value)
    {
        switch (value)
        {
            case float f:
                return f.ToString("F1");
            case double d:
                return d.ToString("F1");
            case bool b:
                return b ? "yes" : "no";
            case Vector2Int v:
                return $"({v.x},{v.y})";
            default:
                return value?.ToString() ?? "null";
        }
    }
    #endregion

    #region Action-Oriented Language Processing
    /// <summary>
    /// Check if message uses action-oriented language structure
    /// </summary>
    public static bool IsActionOriented(string message)
    {
        if (string.IsNullOrEmpty(message)) return false;

        string firstWord = GetFirstWord(message);
        return ACTION_VERBS.Any(verb => firstWord.Equals(verb, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Convert message to action-oriented structure
    /// </summary>
    public static string MakeActionOriented(string message)
    {
        if (string.IsNullOrEmpty(message) || IsActionOriented(message)) return message;

        // Analyze message content to determine appropriate action verb
        string actionVerb = DetermineActionVerb(message);
        if (string.IsNullOrEmpty(actionVerb)) return message;

        // Restructure message to be action-first
        return RestructureWithActionVerb(message, actionVerb);
    }

    /// <summary>
    /// Determine appropriate action verb based on message content
    /// </summary>
    private static string DetermineActionVerb(string message)
    {
        string lowerMessage = message.ToLower();

        // Keyword-based action verb detection
        if (lowerMessage.Contains("marker") || lowerMessage.Contains("place")) return "Place";
        if (lowerMessage.Contains("move") || lowerMessage.Contains("position")) return "Move";
        if (lowerMessage.Contains("trigger") || lowerMessage.Contains("activate")) return "Trigger";
        if (lowerMessage.Contains("light") || lowerMessage.Contains("illuminate")) return "Light";
        if (lowerMessage.Contains("press") || lowerMessage.Contains("key")) return "Press";
        if (lowerMessage.Contains("avoid") || lowerMessage.Contains("dodge")) return "Avoid";
        if (lowerMessage.Contains("target") || lowerMessage.Contains("aim")) return "Target";
        if (lowerMessage.Contains("capture") || lowerMessage.Contains("collect")) return "Capture";

        // Default fallback
        return "Use";
    }

    /// <summary>
    /// Restructure message to start with action verb
    /// </summary>
    private static string RestructureWithActionVerb(string message, string actionVerb)
    {
        // Simple restructuring - prepend action verb and adjust grammar
        string cleaned = message.Trim();
        
        // Remove redundant words that might conflict with action verb
        cleaned = RemoveRedundantWords(cleaned, actionVerb);
        
        // Ensure proper capitalization
        if (!string.IsNullOrEmpty(cleaned))
        {
            cleaned = char.ToLower(cleaned[0]) + cleaned.Substring(1);
        }

        return $"{actionVerb} {cleaned}";
    }

    /// <summary>
    /// Remove words that would be redundant with the action verb
    /// </summary>
    private static string RemoveRedundantWords(string message, string actionVerb)
    {
        string lowerVerb = actionVerb.ToLower();
        string lowerMessage = message.ToLower();

        // Remove redundant action words
        var redundantWords = new List<string> { lowerVerb, "you can", "you should", "try to", "make sure to" };
        
        foreach (string redundant in redundantWords)
        {
            if (lowerMessage.StartsWith(redundant))
            {
                message = message.Substring(redundant.Length).Trim();
                lowerMessage = message.ToLower();
            }
        }

        return message;
    }
    #endregion

    #region Progressive Disclosure
    /// <summary>
    /// Create progressive disclosure version of message based on player experience
    /// </summary>
    public static string CreateProgressiveVersion(TutorialMessage originalMessage, ProgressiveDisclosureContext context)
    {
        if (originalMessage == null) return string.Empty;

        // Use existing short message if available and appropriate
        if (context.HasSeenBefore && originalMessage.useShortMessageOnRepeat && !string.IsNullOrEmpty(originalMessage.shortMessage))
        {
            return ProcessDynamicContent(originalMessage.shortMessage, context.gameContext);
        }

        // Generate progressive version based on context
        string baseMessage = originalMessage.Message;
        
        if (context.HasSeenBefore)
        {
            return CreateConciseVersion(baseMessage, context);
        }
        else if (context.RelatedMessagesShown > 0)
        {
            return CreateBuildingVersion(baseMessage, context);
        }

        // First time seeing - return full message
        return ProcessDynamicContent(baseMessage, context.gameContext);
    }

    /// <summary>
    /// Create concise version for repeat viewings
    /// </summary>
    private static string CreateConciseVersion(string message, ProgressiveDisclosureContext context)
    {
        // Extract key action from original message
        string keyAction = ExtractKeyAction(message);
        
        // Add transition word for flow
        string transition = TRANSITION_WORDS[UnityEngine.Random.Range(0, TRANSITION_WORDS.Length)];
        
        return $"{transition}: {keyAction}";
    }

    /// <summary>
    /// Create building version that references previous knowledge
    /// </summary>
    private static string CreateBuildingVersion(string message, ProgressiveDisclosureContext context)
    {
        // Add contextual reference to build on previous messages
        string baseMessage = ProcessDynamicContent(message, context.gameContext);
        
        if (context.RelatedMessagesShown == 1)
        {
            return $"Now: {baseMessage}";
        }
        else if (context.RelatedMessagesShown >= 2)
        {
            return $"Next: {ExtractKeyAction(baseMessage)}";
        }

        return baseMessage;
    }

    /// <summary>
    /// Extract the key action or instruction from a message
    /// </summary>
    private static string ExtractKeyAction(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        // Split into sentences and take the first actionable one
        string[] sentences = message.Split('.', '!', '?');
        
        foreach (string sentence in sentences)
        {
            string trimmed = sentence.Trim();
            if (!string.IsNullOrEmpty(trimmed) && IsActionOriented(trimmed))
            {
                return trimmed;
            }
        }

        // Fallback: return first sentence
        return sentences.Length > 0 ? sentences[0].Trim() : message;
    }
    #endregion

    #region Immediate Relevance Filtering
    /// <summary>
    /// Check if message is immediately relevant to current player capabilities and context
    /// </summary>
    public static bool IsImmediatelyRelevant(TutorialMessage message, GameContext context, PlayerCapabilities capabilities)
    {
        if (message == null || context == null || capabilities == null) return false;

        // Check if player has the required capabilities for this message
        if (!HasRequiredCapabilities(message, capabilities))
        {
            return false;
        }

        // Check contextual relevance
        if (!message.ShouldDisplay(context))
        {
            return false;
        }

        // Check timing relevance (not too early, not too late)
        if (!IsTimingAppropriate(message, context, capabilities))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if player has capabilities required for this message
    /// </summary>
    private static bool HasRequiredCapabilities(TutorialMessage message, PlayerCapabilities capabilities)
    {
        string messageText = message.Message.ToLower();

        // Check for marker-related messages
        if (messageText.Contains("light marker") && !capabilities.CanUseUnitMarkers) return false;
        if (messageText.Contains("heavy marker") && !capabilities.CanUseRecursionMarkers) return false;
        if (messageText.Contains("prime marker") && !capabilities.CanUsePrimeMarkers) return false;
        if (messageText.Contains("cube marker") && !capabilities.CanUseCubeMarkers) return false;

        // Check for movement-related messages
        if (messageText.Contains("move") && !capabilities.CanMove) return false;

        return true;
    }

    /// <summary>
    /// Check if timing is appropriate for showing this message
    /// </summary>
    private static bool IsTimingAppropriate(TutorialMessage message, GameContext context, PlayerCapabilities capabilities)
    {
        // Essential messages are always timely
        if (message.category == MessageCategory.Essential) return true;

        // For early game messages, check if player is still learning
        if (message.category == MessageCategory.Important && capabilities.ExperienceLevel > PlayerExperienceLevel.Beginner)
        {
            // Experienced players don't need basic guidance
            return false;
        }

        // Contextual messages should match current situation urgency
        if (message.category == MessageCategory.Contextual)
        {
            // If cubes are very close, only show urgent messages
            if (context.nearestCubeDistance < 2f)
            {
                return message.Message.ToLower().Contains("urgent") || message.Message.ToLower().Contains("quick");
            }
        }

        return true;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Split message into lines respecting word boundaries
    /// </summary>
    private static string[] SplitIntoLines(string message)
    {
        if (string.IsNullOrEmpty(message)) return new string[0];
        
        return message.Split('\n', '\r')
                     .Where(line => !string.IsNullOrWhiteSpace(line))
                     .Select(line => line.Trim())
                     .ToArray();
    }

    /// <summary>
    /// Truncate message to maximum lines
    /// </summary>
    private static string TruncateToMaxLines(string message)
    {
        string[] lines = SplitIntoLines(message);
        if (lines.Length <= MAX_LINES) return message;

        return string.Join("\n", lines.Take(MAX_LINES));
    }

    /// <summary>
    /// Wrap long lines to fit within character limit
    /// </summary>
    private static string WrapLongLines(string message)
    {
        string[] lines = SplitIntoLines(message);
        var wrappedLines = new List<string>();

        foreach (string line in lines)
        {
            if (line.Length <= MAX_LINE_LENGTH)
            {
                wrappedLines.Add(line);
            }
            else
            {
                wrappedLines.AddRange(WrapSingleLine(line));
            }
        }

        // Ensure we don't exceed max lines after wrapping
        if (wrappedLines.Count > MAX_LINES)
        {
            wrappedLines = wrappedLines.Take(MAX_LINES).ToList();
            // Truncate last line if needed
            if (wrappedLines.Count == MAX_LINES && wrappedLines[MAX_LINES - 1].Length > MAX_LINE_LENGTH)
            {
                wrappedLines[MAX_LINES - 1] = wrappedLines[MAX_LINES - 1].Substring(0, MAX_LINE_LENGTH - 3) + "...";
            }
        }

        return string.Join("\n", wrappedLines);
    }

    /// <summary>
    /// Wrap a single long line into multiple lines
    /// </summary>
    private static List<string> WrapSingleLine(string line)
    {
        var result = new List<string>();
        string[] words = line.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(currentLine))
            {
                currentLine = word;
            }
            else if (currentLine.Length + 1 + word.Length <= MAX_LINE_LENGTH)
            {
                currentLine += " " + word;
            }
            else
            {
                result.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            result.Add(currentLine);
        }

        return result;
    }

    /// <summary>
    /// Get first word from message
    /// </summary>
    private static string GetFirstWord(string message)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        
        int spaceIndex = message.IndexOf(' ');
        return spaceIndex > 0 ? message.Substring(0, spaceIndex) : message;
    }
    #endregion

    #region Message Preview System
    /// <summary>
    /// Generate preview of how message will appear with current context
    /// </summary>
    public static MessagePreview GeneratePreview(TutorialMessage message, GameContext context, ProgressiveDisclosureContext progressiveContext = null)
    {
        if (message == null) return null;

        var preview = new MessagePreview
        {
            OriginalMessage = message.Message,
            MessageId = message.messageId,
            Category = message.category
        };

        // Process dynamic content
        preview.ProcessedMessage = ProcessDynamicContent(message.Message, context);

        // Apply progressive disclosure if context provided
        if (progressiveContext != null)
        {
            preview.ProgressiveMessage = CreateProgressiveVersion(message, progressiveContext);
        }

        // Validate final message
        preview.ValidationResult = ValidateMessage(preview.GetFinalMessage());

        // Apply formatting if needed
        if (!preview.ValidationResult.IsValid)
        {
            preview.FormattedMessage = EnforceTwoLineLimit(preview.GetFinalMessage());
            preview.WasFormatted = true;
        }

        return preview;
    }
    #endregion
}

#region Supporting Classes and Enums
/// <summary>
/// Result of message validation process
/// </summary>
[Serializable]
public class MessageValidationResult
{
    public bool IsValid;
    public bool IsActionOriented;
    public int LineCount;
    public int MaxLineLength;
    public MessageValidationError ErrorType;
    public string ErrorMessage;
    public string SuggestedFix;
}

/// <summary>
/// Types of message validation errors
/// </summary>
public enum MessageValidationError
{
    None,
    EmptyMessage,
    TooManyLines,
    LineTooLong,
    NotActionOriented
}

/// <summary>
/// Context for progressive disclosure decisions
/// </summary>
[Serializable]
public class ProgressiveDisclosureContext
{
    public GameContext gameContext;
    public bool HasSeenBefore;
    public int RelatedMessagesShown;
    public float TimeSinceLastShown;
    public PlayerExperienceLevel PlayerExperience;
}

/// <summary>
/// Player capability information for relevance filtering
/// </summary>
[Serializable]
public class PlayerCapabilities
{
    public bool CanMove = true;
    public bool CanUseUnitMarkers = true;
    public bool CanUseRecursionMarkers = false;
    public bool CanUsePrimeMarkers = false;
    public bool CanUseCubeMarkers = false;
    public PlayerExperienceLevel ExperienceLevel = PlayerExperienceLevel.Beginner;
}

/// <summary>
/// Player experience levels for timing decisions
/// </summary>
public enum PlayerExperienceLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}

/// <summary>
/// Preview of message with all formatting applied
/// </summary>
[Serializable]
public class MessagePreview
{
    public string OriginalMessage;
    public string ProcessedMessage;
    public string ProgressiveMessage;
    public string FormattedMessage;
    public string MessageId;
    public MessageCategory Category;
    public MessageValidationResult ValidationResult;
    public bool WasFormatted;

    public string GetFinalMessage()
    {
        if (WasFormatted && !string.IsNullOrEmpty(FormattedMessage))
            return FormattedMessage;
        if (!string.IsNullOrEmpty(ProgressiveMessage))
            return ProgressiveMessage;
        if (!string.IsNullOrEmpty(ProcessedMessage))
            return ProcessedMessage;
        return OriginalMessage;
    }
}

/// <summary>
/// Statistics about message formatting compliance in the database
/// </summary>
[Serializable]
public class MessageFormattingStats
{
    public int TotalMessages;
    public int ValidMessages;
    public int ActionOrientedMessages;
    public int TooManyLinesCount;
    public int LineTooLongCount;
    public int NotActionOrientedCount;
    
    public float ValidPercentage => TotalMessages > 0 ? (ValidMessages / (float)TotalMessages) * 100f : 0f;
    public float ActionOrientedPercentage => TotalMessages > 0 ? (ActionOrientedMessages / (float)TotalMessages) * 100f : 0f;
    
    public override string ToString()
    {
        return $"Messages: {ValidMessages}/{TotalMessages} valid ({ValidPercentage:F1}%), " +
               $"{ActionOrientedMessages} action-oriented ({ActionOrientedPercentage:F1}%), " +
               $"Issues: {TooManyLinesCount} too many lines, {LineTooLongCount} too long, {NotActionOrientedCount} not action-oriented";
    }
}
#endregion
