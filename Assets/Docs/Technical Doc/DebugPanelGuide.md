# Debug Panel Guide

## Overview

The InfinityQube debug system provides comprehensive playtesting and prototyping tools through an in-game IMGUI panel system. Toggle the panel with **F12** to access all debug features without leaving the game.

## Architecture Summary

The system uses a tabbed IMGUI interface (`PrototypingSystem`) with specialized panels that share common utilities through `PrototypingPanelBase`.

### Panel Hierarchy

```
PrototypingSystem (F12 toggle)
├── QuickDebugPanel    - Fast access to common actions
├── WavePrototyper     - Wave design and control
├── CollisionPanel     - Collision testing matrix
├── GridDesigner       - Grid and tile manipulation
├── PlayerPanel        - Marker settings and attunements
├── StagePanel         - Time and stage control
├── SystemPanel        - Performance and manager status
└── ConsolePanel       - In-game console viewer
```

## Panel Reference

### 1. Quick Debug Panel (Priority: First Tab)

**Purpose**: One-click access to the most common debug actions for rapid iteration.

**Features**:
- **Time Controls**: Pause/Play, speed presets (0.25x, 0.5x, 1x, 2x)
- **Wave Controls**: Start, Stop, Respawn current wave
- **Clear Controls**: Clear Cubes, Clear Markers, Clear ALL
- **Marker Shortcuts**: Refill All Markers, Enable Unlimited Mode
- **State Snapshots**: Save/Load game state for quick iteration
- **Manual Step Controls**: Step wave forward/backward for frame-by-frame testing
- **Test Scenarios**: Pre-built collision tests (Unit vs Unit, Matrix 3x3, Recursion Cross)
- **Quick Spawn**: Full Row, Mixed Wave, Stress Test

**Keyboard Shortcuts** (when panel visible):
- `P` - Pause/Resume
- `R` - Respawn wave
- `C` - Clear all

### 2. Wave Prototyper Panel

**Purpose**: Design, edit, and test wave configurations with a visual editor.

**Features**:
- **Track Board Mode**: Live view of current cubes on grid with edit capability
- **Design New Mode**: Visual wave designer with grid-based editing
- **Cube Brush**: Select cube type (Unit, Matrix, Recursion, Infinity) or Erase
- **Wave Controls**: Start, Stop, Respawn, Pause/Resume, Step Forward/Back
- **Speed Control**: Slider and presets (0.5x, 1x, 2x, 4x)
- **Quick Spawn**: Spawn single cubes at specific columns
- **Wave Asset Management**: Load, Save, Create wave ScriptableObjects

**Wave Designer**:
- Adjustable grid size (Width/Height)
- Click cells to place/remove cubes
- Fill patterns (Unit, Matrix, Recursion)
- Copy live board to designer

### 3. Collision Panel

**Purpose**: Test all 16 cube collision combinations systematically.

**Features**:
- **Collision Matrix**: 4x4 grid showing all Player vs Wave combinations
- **Quick Presets**: Test any cube type vs all wave types
- **Special Tests**: Same-Type collisions, Area Effects, ALL 16 combinations
- **Custom Test Setup**: Configure specific player/wave type combinations
- **Test Controls**: Reset, Clear All, Step, Pause, Speed adjustment

**Collision Icons**:
- `1` - Single capture
- `2x2` - 2x2 area capture
- `3x3` - 3x3 triggerable marker
- `C3` - Column capture (3 cubes)
- `+` - Cross pattern (5 tiles)
- `FP` - Face paint
- `WJ` - Wave join

### 4. Grid Designer Panel

**Purpose**: Manipulate grid dimensions and tile states.

**Features**:
- **Resize Grid**: Width/Height controls with presets (6x15, 10x20, 15x30)
- **Tile State**: Set individual tiles to Normal or Transformed
- **Target Position**: X/Y controls to select specific tiles
- **Apply Operations**: Single tile, Row, Column
- **Patterns**: Checkerboard, Cross, Border, Diagonal, Random
- **Utilities**: Regenerate grid, Debug info

### 5. Player Panel

**Purpose**: Configure marker settings, charges, and attunements.

**Features**:
- **Mode Selection**: Switch between Unit/Matrix/Recursion/Infinity modes
- **Unlimited Mode**: Toggle unlimited markers for testing
- **Marker Economy**: Toggle economy system, Apply Stage/Wave Grants
- **Marker Settings**: Per-type charge limits, recharge rates, max on grid
- **Quick Presets**: Reset Defaults, No Cooldowns, Refill Charges
- **Marker Placement**: Place markers at player position
- **Player Control**: Teleport player (directional, center)
- **Attunements**: View/unlock/equip attunements, currency controls

### 6. Stage Panel

**Purpose**: Control time scale and navigate waves/stages.

**Features**:
- **Time Control**: Slider and presets (Pause, 0.25x, 0.5x, 1x, 2x, 4x)
- **Wave Navigation**: Start/Stop/Skip wave, Clear cubes, Prev/Next wave
- **Stage Control**: Previous/Restart/Next stage (when StageManager available)
- **Auto-Advance Toggle**: Enable/disable automatic wave progression

### 7. System Panel

**Purpose**: Monitor performance and system health.

**Features**:
- **Performance Metrics**:
  - FPS with color coding (green ≥60, yellow ≥30, red <30)
  - Visual FPS graph (30-sample history)
  - Min/Max FPS tracking with reset
  - Memory usage and GC stats
  - Active cube/marker counts
- **Debug Toggles**: Enable/disable debug logging per manager
  - WaveManager Debug
  - GridManager Debug
  - AudioManager Debug
  - Enable/Disable All
- **Manager Status**: Live status of all core managers with extra info
- **Gameplay Toggles**: Line Divider controls, game state info
- **Tools**: Validate Grid, Force GC, Screenshot, Print Hierarchy

### 8. Console Panel

**Purpose**: View Unity console logs without leaving the game.

**Features**:
- **Log Display**: Shows recent logs, warnings, errors
- **Type Filtering**: Toggle visibility by log type (Log/Warning/Error)
- **Text Search**: Filter logs by text content
- **Collapse Identical**: Combine repeated messages with count badge
- **Auto-Scroll**: Follow new logs automatically
- **Stack Trace Viewer**: Click any log to see full message and stack trace
- **Copy to Clipboard**: Share error details easily
- **Clear**: Reset console view

## Global Keyboard Shortcuts

These shortcuts work regardless of panel visibility:

| Shortcut | Action |
|----------|--------|
| `F12` | Toggle debug panel |
| `Ctrl+Shift+P` | Pause/Resume game |
| `Ctrl+Shift+R` | Respawn current wave |
| `Ctrl+Shift+C` | Clear everything (cubes, markers, player cubes) |
| `Ctrl+Shift+M` | Refill all marker charges |
| `Ctrl+Shift+1` | Set speed to 0.25x |
| `Ctrl+Shift+2` | Set speed to 0.5x |
| `Ctrl+Shift+3` | Set speed to 1x (normal) |
| `Ctrl+Shift+4` | Set speed to 2x |

## Common Workflows

### Rapid Wave Testing
1. Open debug panel (F12)
2. Go to **Quick** tab
3. Click **Unlimited Mode** for infinite markers
4. Use **Start Wave** / **Respawn** for quick iteration
5. Adjust speed with time controls as needed

### Testing Specific Collision
1. Go to **Collision** tab
2. Click the cell in the matrix for Player Type vs Wave Type
3. Observe collision behavior at 0.5x speed
4. Use **Reset** to repeat the test

### Designing Custom Wave
1. Go to **Wave** tab
2. Click **Design New** mode
3. Set grid size with W/H controls
4. Select brush (cube type) and click grid cells
5. Click **Spawn Wave** to test
6. Click **Save to Current Wave** or **Create New Wave** to persist

### Debugging Performance Issues
1. Go to **System** tab
2. Monitor FPS graph for drops
3. Check Active Cubes count
4. Use **Force GC** if memory is high
5. Check **Console** tab for errors

### Frame-by-Frame Analysis
1. Go to **Quick** tab
2. Click **Pause**
3. Use **Step Fwd** to advance one wave move
4. Observe cube positions and collisions
5. Use **Step Back** to reverse (note: doesn't restore captured cubes)

### Saving Test State
1. Configure game to desired state
2. Go to **Quick** tab
3. Click **Save Snapshot**
4. Test your changes
5. Click **Load Snapshot** to return to saved state

## IManagerDebugInterface

Managers can implement this interface to integrate with the debug system:

```csharp
public interface IManagerDebugInterface
{
    bool EnableDebugLogs { get; set; }
    string GetDebugStatus();
    Dictionary<string, object> GetDebugData();
    void ResetToDefaults();
    void LoadConfiguration(string configName);
    void SaveConfiguration(string configName);
}
```

**Implementing managers gain**:
- Toggle-able debug logging from System panel
- Status display in Manager Status section
- Integration with "Enable All Debug" / "Disable All Debug" buttons

## Technical Details

### Panel Base Class

All panels extend `PrototypingPanelBase` which provides:
- Manager reference caching (GridManager, WaveManager, PlayerManager, StageManager)
- Common UI helpers (DrawSection, DrawToggleSection, DrawButtonRow, DrawSlider, DrawIntStepper)
- Logging utility (LogAction)

### Adding New Panels

1. Create class extending `PrototypingPanelBase`
2. Implement required properties: `PanelName`, `PanelIcon`, `Category`, `Priority`
3. Implement `DrawGUI()` method
4. Add panel to `PrototypingSystem.InitializePanels()`

### Panel Priority

Lower priority numbers appear first in tab order:
- QuickDebugPanel: 5
- WavePrototyper: 10
- CollisionPanel: 15
- GridDesigner: 25
- PlayerPanel: 30
- StagePanel: 40
- SystemPanel: 50
- ConsolePanel: 55

### Window Controls

- **Drag**: Click and drag header area to move window
- **Resize**: Drag bottom-right corner (◢ indicator)
- **Close**: Click X button or press F12

## Troubleshooting

### Panel Not Showing
- Press F12 to toggle visibility
- Check if `PrototypingSystem` component exists in scene
- Verify `showOnStart` is false if panel shouldn't auto-open

### Manager Shows NULL
- Ensure manager GameObject exists in scene
- Check manager initialization order (Awake vs Start)
- Click "Refresh Refs" in System panel

### Console Not Capturing Logs
- ConsolePanel subscribes to `Application.logMessageReceived`
- Ensure panel was initialized (check Console tab exists)
- Logs before panel initialization won't be captured

### Performance Lag with Panel Open
- IMGUI has some overhead; close panel for accurate FPS testing
- Reduce Console panel log history if many logs
- Disable auto-scroll in Console if log spam

## Best Practices

1. **Use Quick Panel First**: Most common actions are accessible here
2. **Save Snapshots**: Before making experimental changes
3. **Check Console**: When something unexpected happens
4. **Use Ctrl+Shift Shortcuts**: Faster than opening panel
5. **Slow Time for Observation**: 0.25x or 0.5x to see collision details
6. **Step Mode for Debugging**: Pause + Step for precise control
7. **Monitor FPS in System**: Especially when testing performance-sensitive changes
