using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Test utility for demonstrating and validating MessageFormatter functionality.
/// Can be attached to any GameObject to test message formatting features.
/// </summary>
public class MessageFormatterDemo : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private bool enableVerboseLogging = true;
    
    [Header("Sample Messages for Testing")]
    [TextArea(3, 5)]
    [SerializeField] private string[] testMessages = {
        "You should try to place a light marker near the cubes to capture them effectively",
        "Move your player character to position ({playerX},{playerY}) and then press F to place a marker",
        "The wave has {activeCubeTypes} different cube types approaching your position",
        "Recursion cubes are dangerous and should be avoided at all costs unless you have heavy markers",
        "When you see infinity cubes, target them with light markers for maximum points and detonations"
    };

    [Header("Mock Game Context")]
    [SerializeField] private Vector2Int mockPlayerPosition = new Vector2Int(5, 3);
    [SerializeField] private int mockAvailableMarkers = 3;
    [SerializeField] private int mockMoveStep = 2;
    [SerializeField] private float mockNearestCubeDistance = 4.5f;
    [SerializeField] private CubeType[] mockActiveCubeTypes = { CubeType.Infinity, CubeType.Unit };

    private void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }

    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("=== MessageFormatter Demo - Starting Tests ===");
        
        TestMessageValidation();
        TestDynamicVariableSubstitution();
        TestActionOrientedFormatting();
        TestProgressiveDisclosure();
        TestTwoLineEnforcement();
        TestFormattingPreview();
        
        Debug.Log("=== MessageFormatter Demo - Tests Complete ===");
    }

    [ContextMenu("Test Message Validation")]
    public void TestMessageValidation()
    {
        Debug.Log("--- Testing Message Validation ---");
        
        foreach (string message in testMessages)
        {
            var validation = MessageFormatter.ValidateMessage(message);
            
            LogTest($"Message: \"{message}\"");
            LogTest($"Valid: {validation.IsValid}, Action-Oriented: {validation.IsActionOriented}");
            LogTest($"Lines: {validation.LineCount}, Max Line Length: {validation.MaxLineLength}");
            
            if (!validation.IsValid)
            {
                LogTest($"Error: {validation.ErrorType} - {validation.ErrorMessage}");
                if (!string.IsNullOrEmpty(validation.SuggestedFix))
                {
                    LogTest($"Suggested Fix: \"{validation.SuggestedFix}\"");
                }
            }
            LogTest("---");
        }
    }

    [ContextMenu("Test Dynamic Variable Substitution")]
    public void TestDynamicVariableSubstitution()
    {
        Debug.Log("--- Testing Dynamic Variable Substitution ---");
        
        var mockContext = CreateMockGameContext();
        
        string templateMessage = "Player at ({playerX},{playerY}) has {markers} markers, step {step}, nearest cube {cubeDistance:F1}";
        string processed = MessageFormatter.ProcessDynamicContent(templateMessage, mockContext);
        
        LogTest($"Template: \"{templateMessage}\"");
        LogTest($"Processed: \"{processed}\"");
        
        // Test with additional variables
        var additionalVars = new Dictionary<string, object>
        {
            ["customValue"] = 42,
            ["playerName"] = "TestPlayer"
        };
        
        string templateWithCustom = "Hello {playerName}! Custom value: {customValue}, markers: {markers}";
        string processedWithCustom = MessageFormatter.ProcessDynamicContent(templateWithCustom, mockContext, additionalVars);
        
        LogTest($"Custom Template: \"{templateWithCustom}\"");
        LogTest($"Custom Processed: \"{processedWithCustom}\"");
    }

    [ContextMenu("Test Action-Oriented Formatting")]
    public void TestActionOrientedFormatting()
    {
        Debug.Log("--- Testing Action-Oriented Formatting ---");
        
        string[] nonActionMessages = {
            "You should move to the left",
            "It would be good to place a marker",
            "Try pressing F when you're ready",
            "The best strategy is to avoid the cubes"
        };
        
        foreach (string message in nonActionMessages)
        {
            bool isActionOriented = MessageFormatter.IsActionOriented(message);
            string actionVersion = MessageFormatter.MakeActionOriented(message);
            
            LogTest($"Original: \"{message}\" (Action-oriented: {isActionOriented})");
            LogTest($"Action Version: \"{actionVersion}\"");
            LogTest("---");
        }
    }

    [ContextMenu("Test Progressive Disclosure")]
    public void TestProgressiveDisclosure()
    {
        Debug.Log("--- Testing Progressive Disclosure ---");
        
        // Create mock tutorial message
        var tutorialMessage = new TutorialMessage
        {
            Message = "Place light markers in the path of infinity cubes to capture them and earn detonations",
            shortMessage = "Place markers for cubes",
            messageId = "test_progressive",
            useShortMessageOnRepeat = true,
            category = MessageCategory.Important
        };
        
        var mockContext = CreateMockGameContext();
        
        // Test first time viewing
        var firstTimeContext = new ProgressiveDisclosureContext
        {
            gameContext = mockContext,
            HasSeenBefore = false,
            RelatedMessagesShown = 0,
            PlayerExperience = PlayerExperienceLevel.Beginner
        };
        
        string firstTime = MessageFormatter.CreateProgressiveVersion(tutorialMessage, firstTimeContext);
        LogTest($"First Time: \"{firstTime}\"");
        
        // Test repeat viewing
        var repeatContext = new ProgressiveDisclosureContext
        {
            gameContext = mockContext,
            HasSeenBefore = true,
            RelatedMessagesShown = 2,
            PlayerExperience = PlayerExperienceLevel.Intermediate
        };
        
        string repeat = MessageFormatter.CreateProgressiveVersion(tutorialMessage, repeatContext);
        LogTest($"Repeat Viewing: \"{repeat}\"");
        
        // Test building on previous knowledge
        var buildingContext = new ProgressiveDisclosureContext
        {
            gameContext = mockContext,
            HasSeenBefore = false,
            RelatedMessagesShown = 3,
            PlayerExperience = PlayerExperienceLevel.Advanced
        };
        
        string building = MessageFormatter.CreateProgressiveVersion(tutorialMessage, buildingContext);
        LogTest($"Building on Knowledge: \"{building}\"");
    }

    [ContextMenu("Test Two Line Enforcement")]
    public void TestTwoLineEnforcement()
    {
        Debug.Log("--- Testing Two Line Enforcement ---");
        
        string[] longMessages = {
            "This is a very long message that definitely exceeds the two line limit and should be truncated or wrapped appropriately to fit within the constraints",
            "Line one is okay\nLine two is also fine\nBut line three should be removed\nAnd line four definitely should not appear",
            "Short message",
            "This message has words that are too long for a single line and needs intelligent wrapping to maintain readability while respecting the character limits per line"
        };
        
        foreach (string message in longMessages)
        {
            string enforced = MessageFormatter.EnforceTwoLineLimit(message);
            var validation = MessageFormatter.ValidateMessage(enforced);
            
            LogTest($"Original: \"{message}\"");
            LogTest($"Enforced: \"{enforced}\"");
            LogTest($"Valid after enforcement: {validation.IsValid}");
            LogTest("---");
        }
    }

    [ContextMenu("Test Formatting Preview")]
    public void TestFormattingPreview()
    {
        Debug.Log("--- Testing Formatting Preview ---");
        
        var tutorialMessage = new TutorialMessage
        {
            Message = "You should move to position ({playerX},{playerY}) and place {markers} light markers to capture the approaching infinity cubes",
            messageId = "test_preview",
            category = MessageCategory.Important
        };
        
        var mockContext = CreateMockGameContext();
        var progressiveContext = new ProgressiveDisclosureContext
        {
            gameContext = mockContext,
            HasSeenBefore = false,
            RelatedMessagesShown = 1,
            PlayerExperience = PlayerExperienceLevel.Beginner
        };
        
        var preview = MessageFormatter.GeneratePreview(tutorialMessage, mockContext, progressiveContext);
        
        LogTest($"Original Message: \"{preview.OriginalMessage}\"");
        LogTest($"Processed Message: \"{preview.ProcessedMessage}\"");
        LogTest($"Progressive Message: \"{preview.ProgressiveMessage}\"");
        LogTest($"Final Message: \"{preview.GetFinalMessage()}\"");
        LogTest($"Was Formatted: {preview.WasFormatted}");
        
        if (preview.ValidationResult != null)
        {
            LogTest($"Validation - Valid: {preview.ValidationResult.IsValid}, Action-Oriented: {preview.ValidationResult.IsActionOriented}");
            if (!preview.ValidationResult.IsValid)
            {
                LogTest($"Validation Error: {preview.ValidationResult.ErrorMessage}");
            }
        }
    }

    [ContextMenu("Test Integration with TutorialMessageManager")]
    public void TestTutorialManagerIntegration()
    {
        Debug.Log("--- Testing TutorialMessageManager Integration ---");
        
        var tutorialManager = TutorialMessageManager.Instance;
        if (tutorialManager == null)
        {
            LogTest("TutorialMessageManager not found in scene - cannot test integration");
            return;
        }
        
        // Test formatting stats
        var stats = tutorialManager.GetFormattingStats();
        LogTest($"Formatting Statistics: {stats}");
        
        // Test message formatting
        string testText = "You should try to place markers at ({playerX},{playerY}) when you have {markers} available";
        string formatted = tutorialManager.FormatMessageText(testText);
        LogTest($"Original: \"{testText}\"");
        LogTest($"Formatted: \"{formatted}\"");
        
        // Test context
        var context = tutorialManager.GetCurrentContext();
        LogTest($"Current Context - Markers: {context.availableMarkers}, Step: {context.currentMoveStep}, Cube Distance: {context.nearestCubeDistance:F1}");
    }

    private GameContext CreateMockGameContext()
    {
        var context = new GameContext
        {
            playerPosition = mockPlayerPosition,
            availableMarkers = mockAvailableMarkers,
            currentMoveStep = mockMoveStep,
            nearestCubeDistance = mockNearestCubeDistance,
            isGamePaused = false
        };
        
        context.activeCubeTypes.AddRange(mockActiveCubeTypes);
        return context;
    }

    private void LogTest(string message)
    {
        if (enableVerboseLogging)
        {
            Debug.Log($"[MessageFormatter Test] {message}");
        }
    }

    [ContextMenu("Validate All Messages in Scene")]
    public void ValidateAllMessagesInScene()
    {
        var tutorialManager = TutorialMessageManager.Instance;
        if (tutorialManager != null)
        {
            tutorialManager.ValidateAllMessages();
        }
        else
        {
            Debug.LogWarning("TutorialMessageManager not found - cannot validate messages");
        }
    }

    [ContextMenu("Show Formatting Statistics")]
    public void ShowFormattingStatistics()
    {
        var tutorialManager = TutorialMessageManager.Instance;
        if (tutorialManager != null)
        {
            var stats = tutorialManager.GetFormattingStats();
            Debug.Log($"Message Formatting Statistics:\n{stats}");
        }
        else
        {
            Debug.LogWarning("TutorialMessageManager not found - cannot show statistics");
        }
    }
}
