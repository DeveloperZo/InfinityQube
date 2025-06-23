# Gameplay Mechanics

> This document details the core mechanical systems of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Details the functional rules and systemic interactions, clearly outlining the behavior of grid, cubes, markers, detonation systems, and wave progression as currently implemented.

## 3.1 Grid System
### Core Structure
- **Configurable Dimensions**: Per-stage grid sizing (e.g., 5x20, 8x15)
- **Singleton Management**: GridManager handles all grid operations
- **World Space Mapping**: Vector2Int grid coordinates to 3D world positions
- **Boundary Enforcement**: Automatic player clamping to grid bounds
- **Fallen Row Tracking**: Dynamic reduction of playable area

### Tile States
| State | Behavior | Visual Indicator | Player Interaction |
|-------|----------|------------------|-------------------|
| Normal | Default state | Base height | Can place markers |
| Transformed | Modified by cube interaction | Height variation | Modified marker behavior |

### Grid Operations
- **IsValidGridPosition()**: Boundary validation
- **GridToWorldPosition()**: Coordinate conversion
- **GetPlayableRowCount()**: Dynamic area calculation
- **Height/Width Properties**: Runtime grid dimensions

## 3.2 Cube System
### Core Cube Types
| Type | Visual | Movement | Capture Behavior | Special Properties |
|------|--------|----------|------------------|-------------------|
| **Unit** | Gray | Standard step movement | Capturable via markers | Basic scoring |
| **Prime** | Blue | Standard step movement | Creates detonation markers | Generates cube markers on capture |
| **Infinity** | Black | Standard step movement | **Uncapturable** | Kills player on contact |
| **Dense** | Darker/Metallic | Standard step movement | Requires multiple hits | Increased durability |

### Movement System
- **Step-Based Progression**: Discrete grid movement per wave step
- **Consistent Timing**: Configurable `moveInterval` per wave
- **Forward Only**: Cubes move down the grid toward escape
- **Speed Variants**: Normal and fast movement modes
- **Collision Detection**: With player and grid boundaries

### Face Painting System
Advanced cube state modification system:
```
FaceStatus Enum:
- None: Standard behavior
- Corrupted: Acts like Black cube when active
- Enhanced: Creates detonation when captured
```

Face painting affects cube behavior dynamically based on cube orientation and face state.

### Cube Properties
- **Position Tracking**: Vector2Int grid coordinates
- **World Position**: 3D transform synchronization
- **Type Inheritance**: Base CubeType with specialized behaviors
- **Capture State**: Tracking capture eligibility
- **Movement State**: Active/paused/destroyed states

## 3.3 Player System
### Movement Mechanics
- **Analog Input**: WASD/Arrow keys for smooth movement
- **Grid-Based**: Movement within grid boundaries
- **Collision System**: CharacterController-based physics
- **Smooth Animation**: Velocity-based movement with acceleration/deceleration
- **Rotation**: Faces movement direction dynamically

### Action System (PlayerActionManager)
Comprehensive marker and detonation management:

#### Light Markers
- **Placement Key**: F
- **Trigger Key**: R  
- **Charge System**: Limited uses with regeneration
- **Visual Feedback**: Placement indicators and charge display

#### Prime Markers  
- **Placement Key**: G
- **Trigger Key**: T
- **Coverage**: 2x2 grid area
- **Cooldown System**: Time-based restrictions
- **Resource Limits**: Configurable maximum on-grid count

#### Cube Markers
- **Trigger Key**: Q
- **Generation**: Created by capturing Prime cubes
- **Direct Detonation**: Immediate cube destruction
- **Strategic Resource**: Finite and valuable

### Player Statistics
Comprehensive tracking system:
- **Cube Captures**: By type (Unit, Prime, Infinity attempts)
- **Marker Usage**: Light and prime marker placement/triggers
- **Detonation Metrics**: Efficiency and timing
- **Movement Tracking**: Distance and time
- **Death/Respawn**: Player mortality events
- **Performance Metrics**: Success rates and efficiency

## 3.4 Wave Management System
### Wave Progression
- **Manual Control**: ENTER to start waves
- **Step-Based Movement**: Discrete cube advancement
- **Configurable Timing**: Per-wave `moveInterval` settings
- **Speed Control**: Normal/fast movement modes
- **Debug Features**: Manual step control and inspection

### Wave Configuration
```
WaveData Properties:
- GridWidth/Height: Grid dimensions
- moveInterval: Time between steps
- fastMoveInterval: Accelerated timing
- waveStartDelay: Initial delay
- CubesData: List of cube definitions
- Marker Limits: Resource constraints per wave
```

### Active Wave Management
- **Cube Tracking**: Live cube count and positions
- **State Management**: Active/paused/completed states
- **Event System**: Wave start/end notifications
- **Debug Controls**: Manual progression and inspection

## 3.5 Input System
### Core Controls
| Action | Input | System | Effect |
|--------|-------|--------|--------|
| **Movement** | WASD/Arrows | PlayerManager | Grid navigation |
| **Light Marker** | F | PlayerActionManager | Place single marker |
| **Prime Marker** | G | PlayerActionManager | Place 2x2 area marker |
| **Trigger Light** | R | PlayerActionManager | Activate light markers |
| **Trigger Prime** | T | PlayerActionManager | Activate prime markers |
| **Trigger Cube Marker** | Q | PlayerActionManager | Direct cube detonation |
| **Start Wave** | ENTER | WaveManager | Begin wave progression |
| **Restart Level** | P | Game System | Reset current stage |
| **Quit Game** | ESC | Game System | Exit application |
| **Toggle UI** | TAB | GameUI | Show/hide interface |

### Advanced Controls
| Action | Input | System | Effect |
|--------|-------|--------|--------|
| **Close Dialog** | K | UI System | Dismiss messages |
| **Send Feedback** | F12 | FeedbackCollector | Open email feedback |

## 3.6 Stage System
### Stage Management
- **ScriptableObject Configuration**: Data-driven stage definitions
- **Progressive Difficulty**: Structured learning curve
- **Multi-Wave Composition**: Complex stage structures
- **Completion Tracking**: Win/loss conditions
- **Restart Functionality**: Reset capability

### Stage Types
```
StageType Enum:
- Tutorial: Teaching-focused stages
- Standard: Normal gameplay stages  
- Challenge: Difficult condition stages
- Bonus: Special rule stages
```

### Stage Properties
- **Grid Dimensions**: Per-stage grid sizing
- **Player Start Position**: Initial placement
- **Wave Configurations**: List of wave data references
- **Objective Text**: Clear success criteria
- **Completion Requirements**: Capture counts, escape limits

## 3.7 Detonation System
### Detonation Types
```
DetonationType Enum:
- Large: 3x3 area effect
- Standard: 3x3 area effect  
- Small: 2x2 area effect
- Single: Single tile effect
```

### Activation Methods
1. **Marker-Based**: Cubes pass through marked tiles
2. **Manual Trigger**: Player-activated marker detonation
3. **Cube Marker**: Direct cube targeting
4. **Prime Cube Capture**: Automatic marker generation

### Visual/Audio Feedback
- **Placement Indicators**: Clear marker visualization
- **Detonation Effects**: Particle systems and animations
- **Audio Cues**: Sound feedback for all actions
- **UI Updates**: Real-time charge and count display

## 3.8 Debug System
### Debug Panels
Comprehensive debugging infrastructure:
- **Gameplay Panel**: Stage, wave, player debugging  
- **Testing Panel**: Face painting and scenario testing
- **Wave Debug Panel**: Cube inspection and wave controls
- **Cube Inspector**: Individual cube state examination

### Debug Features
- **Real-Time Inspection**: Live value monitoring
- **Manual Controls**: Override automatic systems
- **State Manipulation**: Direct property modification
- **Scenario Testing**: Rapid iteration tools

---
**Last Updated:** December 20, 2024  
**Implementation Status:** Current codebase reflects all documented mechanics  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)