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
| Corrupted | Rejects markers, paints cubes | Visual corruption effect | Cannot place markers |
| Marked | Contains active marker | Marker visualization | Awaiting detonation |

### Grid Operations
- **IsValidGridPosition()**: Boundary validation
- **GridToWorldPosition()**: Coordinate conversion
- **GetPlayableRowCount()**: Dynamic area calculation
- **Height/Width Properties**: Runtime grid dimensions
- **RecordMarkerPosition()**: Tracks marker placements for next wave spawn

## 3.2 Cube System
### Core Cube Types
| Type | Visual | Movement | Capture Behavior | Special Properties |
|------|--------|----------|------------------|-------------------|
| **Unit** | Gray | Standard step movement | Capturable via markers | Basic scoring |
| **Prime** | Blue | Standard step movement | Creates detonation markers | Generates cube markers on capture |
| **Infinity** | Black | Standard/Paused movement | **Uncapturable** | Can pause, destroys colliding cubes, corrupts tiles |
| **Recursion** | Darker/Metallic | Standard step movement | Requires multiple hits | Increased durability |

### Movement System
- **Step-Based Progression**: Discrete grid movement per wave step
- **Consistent Timing**: Configurable `moveInterval` per wave
- **Forward Only**: Cubes move down the grid toward escape
- **Speed Variants**: Normal and fast movement modes
- **Collision Detection**: With player, grid boundaries, and other cubes
- **Pause States**: Infinity cubes can enter paused movement states

### Infinity Cube Mechanics
Advanced behavior system for Infinity cubes:

#### Infinity Markers
- **Spawn Mechanism**: Special markers that spawn player infinity cubes
- **Interaction**: When colliding with Infinity cubes, causes pause state (ultimately causing the cube behind infinity to collide with it)
- **Strategic Impact**: Creates tactical opportunities by controlling Infinity cube movement
- **Chain Effects**: Paused Infinity cubes affect cube flow behind them


#### Pause Mechanic
- **Trigger Conditions**: Infinity markers or specific game events
- **Behavior**: Infinity cube stops forward movement temporarily
- **Collision During Pause**: Any cube colliding with paused Infinity cube is destroyed
- **Duration**: Configurable pause duration per wave/stage settings
- **Visual Indicator**: Clear visual feedback for paused state
- **Queue Destruction**: Creates strategic bottlenecks as following cubes are eliminated

#### Infinity Collisions
- **Infinity-to-Infinity**: When two Infinity cubes collide (placeholder mechanics):
  - **Option A - Tile Corruption**: Collision point becomes permanently corrupted tile
  - **Option B - Alternating Pause**: Both cubes enter alternating pause pattern (move every other turn)
  - **Option C - Lateral Movement**: Collision triggers sideways movement to adjacent columns
- **Implementation Status**: Placeholder system ready for final mechanic selection
- **Visual Effects**: Dramatic collision feedback regardless of chosen mechanic
- **Strategic Consideration**: Each option creates different tactical scenarios

### Face Painting System
Advanced cube state modification system that dynamically alters cube behavior:
```
FaceStatus Enum:
- None: Standard behavior
- Corrupted: Acts like Infinity cube when active
- Enhanced: Creates detonation when captured
- Paused: Temporary movement suspension (Infinity cubes only)
```

### Cube Properties
- **Position Tracking**: Vector2Int grid coordinates
- **World Position**: 3D transform synchronization
- **Type Inheritance**: Base CubeType with specialized behaviors
- **Capture State**: Tracking capture eligibility
- **Movement State**: Active/paused/destroyed states
- **Collision State**: Tracking collision interactions with other cubes
- **Wave Origin**: Tracks if cube spawned from previous wave's marker position

## 3.3 Player System
### Movement Mechanics
- **Analog Input**: WASD/Arrow keys for smooth movement
- **Grid-Based**: Movement within grid boundaries
- **Collision System**: CharacterController-based physics
- **Smooth Animation**: Velocity-based movement with acceleration/deceleration
- **Rotation**: Faces movement direction dynamically

### Action System (PlayerActionManager)
Comprehensive marker and detonation management using unified input system:

#### Unified Input System
- **Mode Selection**: Keys `1`, `2`, `3`, `4` switch between marker modes
  - `1` = Unit Marker mode
  - `2` = Prime Marker mode
  - `3` = Recursion Marker mode
  - `4` = Infinity Marker mode
- **Placement Key**: `F` - places marker of current mode
- **Automatic Spawning**: When wave moves forward, all placed markers automatically spawn player cubes
- **Cube Marker Trigger**: `R` key triggers cube markers (generated from collisions) to create area effects

#### Unit Markers
- **Mode Key**: `1`
- **Placement**: `F` when in Unit mode
- **Automatic Spawning**: Spawns Unit cube when wave moves forward
- **Charge System**: Limited uses with regeneration
- **Visual Feedback**: Placement indicators and charge display
- **Wave Inheritance**: Position recorded for next wave cube spawn

#### Recursion Markers
- **Mode Key**: `3`
- **Placement**: `F` when in Recursion mode
- **Automatic Spawning**: Spawns Recursion cube when wave moves forward
- **Primary Target**: Enhanced marker specifically designed for Recursion cubes
- **Charge System**: Maximum 2 markers, limited charges with 5-second cooldown
- **Enhanced Power**: Optimized for multi-hit Recursion cube interactions
- **Wave Inheritance**: Position recorded for next wave cube spawn

#### Prime Markers
- **Mode Key**: `2`
- **Placement**: `F` when in Prime mode
- **Automatic Spawning**: Spawns Prime cube when wave moves forward (2x2 area effect, 3x3 for Prime+Prime collisions)
- **Coverage**: 2x2 grid area (from marker), 3x3 for Prime+Prime collisions
- **Cooldown System**: Time-based restrictions
- **Resource Limits**: Configurable maximum on-grid count
- **Wave Inheritance**: Center position recorded for next wave cube spawn

#### Cube Markers
- **Trigger Key**: `R` (KeyCode.R)
- **Power Up Key**: `E` (KeyCode.E) - powers up cube marker (if implemented)
- **Generation**: Created automatically from collisions:
  - Prime+Prime collision → Prime cube marker (3x3 area)
  - Recursion+Recursion collision → Recursion cube marker (2x2 area)
  - Prime captured by non-Prime → Prime cube marker (2x2 area)
- **Behavior**: When triggered with `R`, creates area effect that expands from cube marker position and captures all non-Infinity cubes in the area
  - Prime+Prime cube marker: 3x3 area effect
  - Recursion+Recursion cube marker: 2x2 area effect
  - Prime (non-matching) cube marker: 2x2 area effect
- **Strategic Resource**: Finite and valuable, generated from skillful matching
- **No Wave Inheritance**: Direct action, not placement-based

#### Infinity Markers
- **Mode Key**: `4`
- **Placement**: `F` when in Infinity mode
- **Automatic Spawning**: Spawns Infinity cube when wave moves forward
- **Effect**: Spawns pause-inducing cubes that affect Infinity cubes
- **Charge System**: Limited uses with strategic regeneration (default: 1 charge, 15s cooldown)
- **Interaction Range**: Affects Infinity cubes within proximity
- **Wave Inheritance**: Position recorded for next wave special cube spawn

### Player Statistics
Comprehensive tracking system:
- **Cube Captures**: By type (Unit, Prime, Infinity attempts, Recursion)
- **Marker Usage**: Five-tier marker placement/triggers
- **Wave Pairing Performance**: Success rate across paired waves
- **Strategic Placement**: Marker-to-cube conversion efficiency
- **Movement Tracking**: Distance and time
- **Death/Respawn**: Player mortality events

## 3.4 Wave Management System

### Paired Wave System (NEW)
Revolutionary wave pairing mechanic creating strategic continuity:

#### Wave Pairing Mechanics
- **Wave Structure**: Waves occur in pairs (Wave A → Wave B)
- **Marker Recording**: All marker placements in Wave A are recorded
- **Position Conversion**: Marker positions become cube spawn points in Wave B
- **Type Mapping**: Marker types influence spawned cube types:
  - Unit Marker → Unit Cube
  - Recursion Marker → Recursion Cube
  - Prime Marker → Prime Cube (center of area)
  - Infinity Marker → Special/Infinity Cube
- **Strategic Depth**: Players must balance immediate needs with future consequences

#### Implementation Details
```
Wave Pair Configuration:
- Wave A: Standard cube configuration + marker placement recording
- Wave B: Previous marker positions as spawn points + new cube configuration
- Overlap Handling: New spawns merge with or override marker-based spawns
- Visual Feedback: Ghost previews of future spawn positions
```

### Wave Configuration (WaveData ScriptableObject)
Enhanced configuration supporting paired waves:
```
WaveData Structure:
- waveID: Unique identifier
- pairID: Links paired waves together
- isPrimaryWave: Boolean (true for Wave A, false for Wave B)
- baseSpawns: Standard cube spawn configurations
- markerSpawnRules: How to convert marker positions to cubes
- overlapResolution: How to handle position conflicts
- inheritanceDelay: Rows between marker placement and cube spawn
```

### Marker-to-Cube Conversion Rules
| Marker Type | Default Cube Spawn | Alternative Rules | Special Conditions |
|-------------|-------------------|-------------------|-------------------|
| Light | Unit Cube | Random Unit/Prime | Stage-specific |
| Heavy | Recursion Cube | Dense variant | Resource availability |
| Prime | Prime Cube (center) | 3x3 Unit formation | Area overlap |
| Infinity | Infinity Cube | Paused Infinity | Special wave events |

### Wave Progression
- **Manual Control**: ENTER to start waves
- **Paired Execution**: Waves run in designated pairs
- **Step-Based Movement**: Discrete cube advancement
- **Configurable Timing**: Per-wave `moveInterval` settings
- **Inheritance Tracking**: Visual indicators for marker-to-cube conversion

### Wave Events
- **Pre-Wave Phase**: Display ghost previews of inherited cube positions
- **Spawn Phase**: Initial cube placement + inherited positions
- **Active Phase**: Ongoing cube movement with pause mechanics
- **Recording Phase**: Track all marker placements for next wave
- **Resolution Phase**: Success/failure determination
- **Transition Phase**: Prepare next wave with inheritance data

## 3.5 Marker System
### Marker Placement with Wave Inheritance
Enhanced marker system with future wave implications:

#### Placement Strategy Considerations
- **Immediate Effect**: Marker's current wave impact
- **Future Consequence**: Spawn position in next wave
- **Risk/Reward**: Optimal placement may create future problems
- **Predictive Planning**: Anticipate next wave's cube flow

### Marker Placement Rules
- **Grid Validation**: Must be within valid grid boundaries
- **Tile State Check**: Cannot place on corrupted or occupied tiles
- **Resource Availability**: Sufficient charges/cooldown completed
- **Recording System**: All placements logged for wave inheritance
- **Preview System**: Optional ghost preview of future spawns

### Visual Feedback for Wave Pairing
- **Placement Echo**: Subtle visual echo showing future spawn point
- **Inheritance Trail**: Visual connection between waves
- **Type Indicator**: Shows what cube type will spawn
- **Timing Preview**: Indicates when inherited cube will appear

## 3.6 Strategic Implications

### Paired Wave Strategies
#### Offensive Strategies
- **Spawn Trapping**: Place markers to create difficult next-wave patterns
- **Cascade Setup**: Position markers for chain reactions in next wave
- **Resource Generation**: Strategic Prime marker placement for future Prime cubes

#### Defensive Strategies
- **Safe Zones**: Avoid marker placement in critical defensive positions
- **Controlled Spawning**: Deliberately place markers to control next wave difficulty
- **Infinity Management**: Use Infinity markers strategically for next-wave control

#### Advanced Techniques
- **Wave Sacrifice**: Intentionally struggle in Wave A to optimize Wave B
- **Marker Conservation**: Save markers to minimize next-wave spawns
- **Pattern Recognition**: Learn optimal placement patterns for wave pairs
- **Inheritance Chains**: Multi-wave planning across several pairs

### Balance Considerations
- **Difficulty Scaling**: Paired waves naturally increase complexity
- **Resource Management**: Markers become more precious with dual purpose
- **Learning Curve**: Players must understand both immediate and future impact
- **Comeback Mechanics**: Poor Wave A performance affects Wave B difficulty

## 3.7 Configuration Compression
### Wave Data Optimization
Marker placements can be compressed directly into wave configuration:

```
Compressed Wave Format:
{
  waveID: "W2B",
  pairID: "P1",
  inheritedMarkers: [
    {position: (2,5), type: "Light", delay: 0},
    {position: (4,8), type: "Heavy", delay: 1},
    {position: (6,10), type: "Prime", delay: 2}
  ],
  baseSpawns: [...],
  mergeStrategy: "Override|Combine|Offset"
}
```

### Storage Benefits
- **Reduced Redundancy**: Single configuration handles both waves
- **Replay System**: Easy wave recreation for testing
- **Pattern Library**: Save successful marker patterns
- **Dynamic Difficulty**: Adjust inheritance rules per-player skill

## 3.8 Debug System
### Debug Panels
Enhanced debugging for paired wave system:
- **Wave Pairing Panel**: Visualize wave relationships
- **Inheritance Tracker**: Show marker-to-cube conversions
- **Preview Toggle**: Enable/disable future spawn previews
- **Pattern Analyzer**: Identify optimal placement patterns
- **Replay System**: Recreate specific wave pair scenarios

### Debug Features
- **Marker Recording Override**: Manually set inheritance positions
- **Wave Pair Skipping**: Jump between paired waves
- **Conversion Testing**: Test different marker-to-cube rules
- **Visual Debugging**: Highlight inherited vs base spawns
- **Performance Metrics**: Track wave pair success rates

---
**Last Updated:** November 17, 2025  
**Implementation Status:** Paired wave system in design phase, core mechanics production-ready  
**Major Addition:** Wave pairing with marker inheritance system
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
- [Technical Architecture](TechnicalArchitecture.md)