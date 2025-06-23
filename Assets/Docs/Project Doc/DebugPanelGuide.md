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

## DebugCoordinator Integration

The `DebugCoordinator` provides:
- **Manager Discovery**: Automatic discovery of debug-capable managers
- **Cross-System Operations**: Reset all, enable/disable logging, validation
- **Scenario Management**: Save/load complete system states
- **Performance Monitoring**: Operation timing and statistics
- **Emergency Reset**: Safe system reset functionality

### Key DebugCoordinator Methods
- `ResetAllManagersToDefaults()` - Reset all systems
- `SaveCurrentScenario()` / `LoadScenario()` - State management
- `ValidateAllSystems()` - Comprehensive system validation
- `GenerateSystemReport()` - Detailed system status report

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

## Technical Notes

### Activation
- Press **F12** to toggle the debug system
- Tabbed interface with panel switching
- Panels maintain state when switching

### Dependencies
- All panels require their respective managers to be present
- Shared utilities provide fallback behavior for missing dependencies
- DebugCoordinator handles manager discovery automatically

### Performance
- Shared utilities minimize code duplication
- Fast Testing Mode reduces UI overhead
- Performance monitoring tracks operation costs
- Emergency reset provides quick recovery

This refined debug system provides comprehensive testing capabilities while maintaining clear separation of concerns and efficient workflows for different testing scenarios.
