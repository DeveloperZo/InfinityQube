# Message Formatting System Implementation

## Overview

This implementation adds comprehensive message formatting and engagement features to the InfinityQube tutorial system, including:

- **2-Line Message Constraint System**: Enforces maximum 2 lines per message with 50 characters per line
- **Dynamic Variable Substitution**: Real-time context-based content generation using `{variable}` syntax
- **Action-Oriented Language Processing**: Automatically restructures messages to use verb-first, action-focused language
- **Progressive Disclosure**: Smart message adaptation based on player experience and previous viewing
- **Immediate Relevance Filtering**: Shows only messages appropriate to current player capabilities and context

## Key Components

### 1. MessageFormatter (Static Utility Class)
Core formatting engine with methods for:
- `ValidateMessage()` - Check 2-line compliance and action-orientation
- `ProcessDynamicContent()` - Replace `{variables}` with real game data
- `MakeActionOriented()` - Convert passive messages to action-first structure
- `EnforceTwoLineLimit()` - Intelligent wrapping and truncation
- `CreateProgressiveVersion()` - Generate experience-appropriate message variants

### 2. Enhanced TutorialMessage Class
Extended WaveMessage with new methods:
- `GetFormattedMessage()` - Full formatting with progressive disclosure
- `ValidateFormatting()` - Check compliance with formatting rules
- `IsImmediatelyRelevant()` - Context-sensitive relevance checking

### 3. Enhanced TutorialMessageManager
Integrated formatting into existing system:
- `PreviewMessage()` - Preview formatted output before display
- `FormatMessageText()` - Format any text with current context
- `GetFormattingStats()` - Database compliance statistics
- `ValidateAllMessages()` - Bulk validation of message database

### 4. GameUI Integration
Enhanced dynamic tips in GameUI.cs:
- Uses TutorialMessageManager for context-aware tip generation
- Applies formatting and variable substitution to tips
- Maintains backward compatibility with existing tip system

## Usage Examples

### Basic Message Formatting
```csharp
// Validate message compliance
var validation = MessageFormatter.ValidateMessage("Your long message here");
if (!validation.IsValid) {
    Debug.Log($"Fix needed: {validation.ErrorMessage}");
}

// Enforce 2-line limit
string formatted = MessageFormatter.EnforceTwoLineLimit(longMessage);
```

### Dynamic Variable Substitution
```csharp
// Message template with variables
string template = "Move to ({playerX},{playerY}) with {markers} markers available";

// Process with current game context
GameContext context = tutorialManager.GetCurrentContext();
string processed = MessageFormatter.ProcessDynamicContent(template, context);
// Result: "Move to (5,3) with 3 markers available"
```

### Action-Oriented Formatting
```csharp
string passive = "You should try to place a marker";
string active = MessageFormatter.MakeActionOriented(passive);
// Result: "Place a marker"
```

### Progressive Disclosure
```csharp
var progressiveContext = new ProgressiveDisclosureContext {
    HasSeenBefore = true,
    RelatedMessagesShown = 2,
    PlayerExperience = PlayerExperienceLevel.Intermediate
};

string adaptedMessage = MessageFormatter.CreateProgressiveVersion(tutorialMessage, progressiveContext);
// Returns shorter, more focused version for experienced players
```

### Integration with TutorialMessageManager
```csharp
// Format any text with current game context
string formatted = tutorialManager.FormatMessageText("Place marker at ({playerX},{playerY})");

// Get formatting statistics for database
var stats = tutorialManager.GetFormattingStats();
Debug.Log($"Database compliance: {stats.ValidPercentage:F1}%");

// Preview how a message will appear
var preview = tutorialManager.PreviewMessage(tutorialMessage);
Debug.Log($"Will display: {preview.GetFinalMessage()}");
```

## Available Dynamic Variables

The system supports these context variables in message templates:

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `{playerX}` | Player X position | 5 |
| `{playerY}` | Player Y position | 3 |
| `{markers}` | Available markers | 3 |
| `{step}` | Current wave step | 7 |
| `{cubeDistance}` | Distance to nearest cube | 4.2 |
| `{cubeTypes}` | Number of active cube types | 2 |
| `{hasInfinity}` | Has infinity cubes | yes |
| `{hasRecursion}` | Has recursion cubes | no |

## Configuration

### MessageFormatter Constants
```csharp
public const int MAX_LINES = 2;              // Maximum lines per message
public const int MAX_LINE_LENGTH = 50;       // Characters per line
public const string VARIABLE_PATTERN = @"\{(\w+)\}"; // Variable syntax
```

### Player Experience Levels
- **Beginner**: < 5 messages shown
- **Intermediate**: 5-14 messages shown  
- **Advanced**: 15-29 messages shown
- **Expert**: 30+ messages shown

## Testing and Validation

### MessageFormatterDemo Component
Attach to any GameObject for testing:
- `RunAllTests()` - Comprehensive test suite
- `ValidateAllMessagesInScene()` - Check database compliance
- `ShowFormattingStatistics()` - Display formatting metrics

### Validation Methods
```csharp
// Check single message
var validation = message.ValidateFormatting();

// Validate entire database
tutorialManager.ValidateAllMessages();

// Get compliance statistics
var stats = tutorialManager.GetFormattingStats();
```

## Integration Points

### Existing Systems
- **WaveMessage**: Enhanced with formatting methods for backward compatibility
- **GameUI**: Dynamic tips now use formatting system
- **TutorialMessageManager**: Core display logic enhanced with formatting
- **MessageDatabase**: Validation and statistics for content compliance

### Future Extensibility
- Foundation hooks ready for animation/voice integration
- Modular design allows easy addition of new formatting rules
- Context system extensible for new game state variables
- Progressive disclosure adaptable to different player metrics

## Performance Considerations

- **Validation**: Only performed once at startup or on-demand
- **Context Updates**: Cached and updated at 0.5-second intervals
- **Variable Substitution**: Uses compiled regex for efficiency
- **Memory**: Minimal overhead with shared formatting utilities

## Best Practices

1. **Message Design**: Start with action verbs, keep under 50 characters per line
2. **Variable Usage**: Use descriptive variable names, provide fallbacks
3. **Progressive Disclosure**: Design short versions for repeat messages
4. **Testing**: Use MessageFormatterDemo for validation during development
5. **Context Awareness**: Design messages that work with variable substitution

This system maintains full backward compatibility while providing powerful new formatting capabilities for enhanced player engagement.
