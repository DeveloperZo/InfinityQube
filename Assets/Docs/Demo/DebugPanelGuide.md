# Debug Panel Guide

## Overview

The InfinityQube debug system has been refined to provide better testing workflows, clarity, and efficiency. The system maintains the existing architecture while improving separation of concerns, enhancing testing speed, and adding dedicated individual entity testing capabilities.

## Architecture Summary

The debug system consists of 6 specialized panels that work together through shared utilities and coordinated functionality:

### 1. **Gameplay Debug Panel** - Overall Game State
- **Focus**: StageManager + overall game state and progression
- **Primary Use**: Monitoring and controlling high-level game flow
- **Key Features**: Stage management, game state inspection, progression tracking

### 2. **Wave Manager Panel** - Core Gameplay Loop  
- **Focus**: WaveManager - wave control, cube spawning, and wave configuration
- **Primary Use**: Rapid playtesting workflows and wave testing
- **Key Features**: 
  - Fast Testing Mode (disables messages for speed)
  - Wave configuration and editing
  - Batch testing capabilities
  - Save/load wave workflows

### 3. **Grid Debug Panel** - Grid-Wide Operations
- **Focus**: GridManager - grid state, tiles, and markers
- **Primary Use**: Testing grid-wide functions and tile manipulation oriented toward grid testing scenarios
- **Key Features**: 
  - Grid-wide tile operations
  - Tile state visualization
  - Grid pattern testing
  - Bulk tile manipulation

### 4. **Player Actions Panel** - Player and Actions Coordination
- **Focus**: PlayerActionManager + PlayerManager coordination
- **Primary Use**: Testing player actions, batch operations, and statistics tracking
- **Key Features**:
  - Enhanced batch operations
  - Improved statistics tracking
  - Player action testing workflows
  - Action coordination testing

### 5. **Cube-Tile Individual Panel** - Individual Entity Testing
- **Focus**: One-on-one testing scenarios, cube lifecycle testing, detailed face painting
- **Primary Use**: Testing individual cube and tile interactions with precision
- **Key Features**:
  - Individual cube spawning and manipulation
  - Detailed face painting operations
  - Cube lifecycle testing (spawn → paint → move → capture)
  - Cube-tile interaction testing
  - Individual entity inspection

### 6. **Testing Debug Panel** - Complex Scenario Testing
- **Focus**: Cross-system integration testing and complex scenarios
- **Primary Use**: Testing complex interactions between multiple systems
- **Key Features**:
  - Cross-system integration testing
  - Stress testing and performance testing
  - Advanced edge case testing
  - System coordination testing (Stage + Wave + Player interactions)
  - Comprehensive system state validation

## Shared Utilities

The debug system now uses shared utility classes to eliminate code duplication and ensure consistency:

### DebugUIHelpers
- **Purpose**: Consistent UI styling, colors, and common UI patterns
- **Key Methods**:
  - `WithColor()` / `WithBackgroundColor()` - Temporary color overrides
  - `GetCubeDisplayColor()` - Appropriate colors for cube types
  - `DrawToggleButton()` - Styled toggle buttons
  - `DrawTargetPositionControls()` - Position selection with auto-tracking
  - `DrawDurationControl()` - Paint duration selection
  - `DrawFaceStatusSelector()` - Face status (Corrupted/Enhanced) selection

### DebugCubeSpawnHelper
- **Purpose**: Consistent cube spawning, finding, and manipulation
- **Key Methods**:
  - `SpawnCubeAt()` - Spawn cube at specific grid position
  - `SpawnCubeLinePattern()` / `SpawnCubeRectPattern()` - Pattern spawning
  - `FindCubesAt()` - Find all cubes at position
  - `GetCubeStatistics()` - Comprehensive cube analysis
  - `QuickSpawnTestFormation()` - Pre-configured test setups

### DebugTileHelper
- **Purpose**: Tile manipulation and analysis across debug panels
- **Key Methods**:
  - `SetupTilePainting()` / `ClearTilePainting()` - Tile painter configuration
  - `SetupAdvantaged()` / `ClearAdvantaged()` - Advantaged tile management
  - `GetTileStateDescription()` - Human-readable tile state
  - `GetTileDisplayColor()` - Appropriate colors for tile states
  - `GetAreaSummary()` - Area-wide tile analysis

## Panel Integration and Workflows

### Typical Testing Workflows

#### 1. Individual Entity Testing Workflow
1. **Cube-Tile Individual Panel**: Spawn and configure individual cubes
2. **Cube-Tile Individual Panel**: Set up tile painting
3. **Cube-Tile Individual Panel**: Run lifecycle tests
4. **Testing Debug Panel**: Validate cross-system interactions

#### 2. Wave Testing Workflow
1. **Wave Manager Panel**: Enable Fast Testing Mode
2. **Wave Manager Panel**: Configure or load wave
3. **Wave Manager Panel**: Batch test multiple waves
4. **Grid Debug Panel**: Verify grid state
5. **Testing Debug Panel**: Run integration tests

#### 3. Grid-Wide Testing Workflow
1. **Grid Debug Panel**: Set up grid patterns
2. **Player Actions Panel**: Test player movement through grid
3. **Wave Manager Panel**: Spawn cubes on configured grid
4. **Testing Debug Panel**: Validate complete system interaction

### Cross-Panel Communication

Panels communicate through:
- **Shared Utilities**: Common operations and state
- **Manager References**: Direct access to game managers
- **DebugCoordinator**: Cross-system operations and scenario management

## Debug Panel Groups

The debug system organizes panels into logical groups for better organization and navigation:

### Core Group
- **Grid Debug Panel**: Grid state, tiles, and markers
- **Game Control**: System-wide operations and coordination
- **System Management**: Core system state and configuration

### Wave Group
- **Wave Manager Panel**: Wave control, cube spawning, and wave configuration
- **Wave Testing**: Batch testing and rapid iteration workflows

### Cube Group
- **Cube-Tile Individual Panel**: Individual entity testing and lifecycle testing
- **Cube Management**: Cube spawning, manipulation, and inspection

### Gameplay Group
- **Gameplay Debug Panel**: Overall game state and progression
- **Wave Manager Panel**: Core gameplay loop control
- **Player Actions Panel**: Player and action coordination

### Content Group
- **Grid Debug Panel**: Tiles and grid patterns
- **Cube-Tile Individual Panel**: Cubes and cube interactions
- **Player Actions Panel**: Action testing and coordination

### Testing Group
- **Testing Debug Panel**: Cross-system integration testing
- **Face Painting**: Detailed face painting operations
- **Scenarios**: Complex scenario testing and validation

## IManagerDebugInterface Pattern

The debug system uses a standardized interface pattern that all game managers implement. This provides consistent debug capabilities across all systems.

### Interface Capabilities

All managers implementing this pattern provide:
- **Debug Status**: Human-readable string describing current manager state
- **Debug Data**: Dictionary of key-value pairs containing manager state information
- **Reset to Defaults**: Restore manager to initial configuration
- **Configuration Management**: Save and load named configurations
- **Debug Logging Control**: Enable or disable debug logging per manager

### Manager Discovery

The DebugCoordinator automatically discovers all managers implementing this interface in the scene. This allows:
- **Automatic Integration**: Managers are automatically available for debug operations
- **Cross-System Operations**: Reset all managers, enable/disable logging across all systems
- **Unified Status Reports**: Get status from all managers in a single operation
- **Scenario Management**: Save and restore complete system states

### Using Manager Debug Interface

When working with debug panels:
- Managers automatically appear in debug panels when they implement the interface
- Status information is displayed in real-time
- Debug data can be inspected for detailed state information
- Managers can be reset individually or as part of system-wide operations

## DebugCoordinator Integration

The DebugCoordinator provides:
- **Manager Discovery**: Automatic discovery of debug-capable managers
- **Cross-System Operations**: Reset all, enable/disable logging, validation
- **Scenario Management**: Save/load complete system states
- **Performance Monitoring**: Operation timing and statistics
- **Emergency Reset**: Safe system reset functionality

### Key DebugCoordinator Operations
- **Reset All Managers to Defaults**: Reset all systems to initial state
- **Save Current Scenario**: Capture complete system state for later restoration
- **Load Scenario**: Restore previously saved system state
- **Validate All Systems**: Comprehensive system validation across all managers
- **Generate System Report**: Detailed system status report with performance metrics
- **Cross-System Integration Test**: Test coordination between multiple systems
- **Stress Test**: Rapid operation testing for performance validation
- **System Health Report**: Comprehensive health check of all discovered managers

## Usage Guidelines

### Panel Selection Guide

**Use Gameplay Debug Panel when:**
- Testing stage transitions
- Monitoring overall game progression
- Debugging high-level game flow issues

**Use Wave Manager Panel when:**
- Configuring waves for testing
- Rapid iteration on wave parameters
- Batch testing multiple wave configurations
- Need fast testing without UI messages

**Use Grid Debug Panel when:**
- Setting up grid-wide tile configurations
- Testing tile patterns and effects
- Bulk manipulation of tile states
- Grid-centric testing scenarios

**Use Player Actions Panel when:**
- Testing player movement and actions
- Batch player operations
- Player-centric statistics and analysis
- Action coordination testing

**Use Cube-Tile Individual Panel when:**
- Testing specific cube behaviors
- Detailed face painting operations
- Cube lifecycle testing
- One-on-one cube-tile interactions
- Individual entity inspection and debugging

**Use Testing Debug Panel when:**
- Testing complex multi-system scenarios
- Stress testing and performance analysis
- Edge case testing
- System integration validation
- Comprehensive system coordination tests

### Best Practices

1. **Start Specific, Then Integrate**: Begin testing with individual panels, then move to integration testing
2. **Use Fast Testing Mode**: Enable in Wave Manager Panel for rapid iteration
3. **Save Scenarios**: Use DebugCoordinator to save interesting test states
4. **Check Integration**: Always validate with Testing Debug Panel after making changes
5. **Monitor Performance**: Use DebugCoordinator's performance monitoring for optimization

### Common Patterns

#### Setting Up Test Environment
```
1. Grid Debug Panel: Configure tile patterns
2. Cube-Tile Individual Panel: Spawn test cubes
3. Player Actions Panel: Position player
4. Save scenario via DebugCoordinator
```

#### Rapid Wave Testing
```
1. Wave Manager Panel: Enable Fast Testing Mode
2. Wave Manager Panel: Load wave configurations
3. Wave Manager Panel: Batch test all waves
4. Testing Debug Panel: Validate results
```

#### Complex Integration Testing
```
1. Load saved scenario
2. Testing Debug Panel: Run cross-system tests
3. Testing Debug Panel: Monitor system coordination
4. Generate system report for analysis
```

## Troubleshooting

### Panel Not Responding
- Check DebugCoordinator manager discovery
- Verify required managers are present in scene
- Use Emergency Reset if needed

### Inconsistent Behavior
- Validate all systems via DebugCoordinator
- Check for conflicting panel operations
- Reset to known good scenario

### Performance Issues
- Disable Fast Testing Mode if enabled
- Check DebugCoordinator performance statistics
- Use Emergency Reset to clear complex states

### Missing Functionality
- Verify shared utilities are properly referenced
- Check panel initialization in DebugSystem
- Ensure all dependencies are satisfied

## Tutorial Message System

The debug system integrates with the tutorial message system to provide comprehensive testing and monitoring capabilities.

### Message Categories and Priorities

Tutorial messages are organized by priority categories:

- **Essential**: Critical messages that block gameplay until acknowledged. Highest priority (100).
- **Important**: Important guidance that should be prominently displayed. High priority (75).
- **Contextual**: Contextual hints that enhance understanding but don't interrupt flow. Medium priority (50).
- **Debug**: Debug and development messages for testing. Lowest priority (25).

### Message Priority System

Messages are prioritized based on their category, with higher priority messages displayed first. The system ensures:
- Essential messages always take precedence
- Important messages are shown before contextual hints
- Debug messages are shown only when appropriate
- Messages are filtered and sorted by priority before display

### Message Progress Tracking

The MessageProgressTracker system provides:
- **One-Time Message Tracking**: Tracks which messages have been shown once
- **Cooldown Management**: Global and message-specific cooldowns prevent message spam
- **Frequency Limiting**: Limits messages per minute (default: 8 messages/minute)
- **Priority-Based Cooldowns**: Different cooldown multipliers per message category
- **Progress Persistence**: Saves message progress using PlayerPrefs or JSON files
- **Message Statistics**: Tracks message patterns, blocked messages, and completion rates

### Message Filtering and Display

The system filters messages based on:
- **One-Time Status**: Messages marked as "show once" are only displayed once
- **Global Cooldown**: Minimum time between any messages (default: 3 seconds)
- **Message-Specific Cooldown**: Individual message cooldown periods
- **Frequency Limits**: Maximum messages per minute (Essential messages can bypass)
- **Priority Cooldowns**: Category-specific cooldown multipliers
- **Context Relevance**: Messages are filtered based on current game context

### Tutorial Message Manager Features

The TutorialMessageManager provides:
- **Contextual Message Triggering**: Messages triggered based on game state
- **Message Queue Management**: Queues messages for sequential display
- **Progressive Disclosure**: Messages adapt based on player experience level
- **Message Formatting**: Automatic formatting for action-oriented, concise messages
- **Player Capability Detection**: Messages filtered based on available player capabilities
- **Statistics Integration**: Tracks message display and dismissal for analytics

### Debug Integration

The tutorial system integrates with debug panels to provide:
- **Message Status Display**: Current message queue status and progress
- **Message Statistics**: Display counts of shown, skipped, and queued messages
- **Progress Tracking**: View one-time message progress and completion rates
- **Context Monitoring**: View current game context and trigger conditions
- **Message Validation**: Validate all messages for formatting compliance
- **Progress Reset**: Clear all tutorial progress for testing

## Debug Commands and Shortcuts

### Activation
- **F12**: Toggle the debug system on/off
- **Tabbed Interface**: Switch between panels using tabs
- **Panel State**: Panels maintain state when switching between tabs

### Keyboard Shortcuts
- **F12**: Toggle debug system visibility
- **K**: Skip current tutorial message (when message is displayed)
- **Tab Navigation**: Use mouse to switch between debug panels

### Debug Panel Controls
- **Toggle Buttons**: Enable/disable features and modes
- **Fast Testing Mode**: Disable tutorial messages for rapid testing (Wave Manager Panel)
- **Reset Operations**: Reset individual managers or all systems
- **Scenario Management**: Save and load complete system states
- **Validation Tools**: Run system validation and health checks

### Common Debug Workflows

#### Quick System Reset
1. Open debug system (F12)
2. Navigate to Testing Debug Panel
3. Use "Reset All Managers" or "Emergency Reset"
4. System returns to default state

#### Save Test Scenario
1. Configure desired game state using debug panels
2. Navigate to Testing Debug Panel
3. Use "Save Current Scenario" with a name
4. Scenario can be loaded later for consistent testing

#### Monitor System Health
1. Open debug system (F12)
2. Navigate to Testing Debug Panel
3. Use "Generate System Report" or "System Health Report"
4. Review manager statuses and performance metrics

#### Rapid Wave Testing
1. Open debug system (F12)
2. Navigate to Wave Manager Panel
3. Enable "Fast Testing Mode" (disables tutorial messages)
4. Configure and test waves rapidly
5. Disable Fast Testing Mode when done

## Technical Notes

### Activation
- Press **F12** to toggle the debug system
- Tabbed interface with panel switching
- Panels maintain state when switching

### Dependencies
- All panels require their respective managers to be present
- Shared utilities provide fallback behavior for missing dependencies
- DebugCoordinator handles manager discovery automatically
- Tutorial system requires MessageDatabase and UI components

### Performance
- Shared utilities minimize code duplication
- Fast Testing Mode reduces UI overhead
- Performance monitoring tracks operation costs
- Emergency reset provides quick recovery
- Message progress tracking uses efficient data structures

### Integration Points
- **DebugCoordinator**: Central coordination for all debug operations
- **IManagerDebugInterface**: Standardized interface for manager debug capabilities
- **TutorialMessageManager**: Message system integration and monitoring
- **MessageProgressTracker**: Progress tracking and message filtering
- **Debug Panels**: Specialized panels for different testing scenarios

This refined debug system provides comprehensive testing capabilities while maintaining clear separation of concerns and efficient workflows for different testing scenarios.
