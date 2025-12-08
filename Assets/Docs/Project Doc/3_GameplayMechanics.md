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

### The Line Divider System
Dynamic difficulty mechanism that creates strategic tension:
- **Divider Position**: A line divides the grid (e.g., row 10 on a 20-row grid)
- **Marker Placement Restriction**: Players can only place markers below the line divider
- **Dynamic Movement**: The line moves up as reward, down as penalty
- **Strategic Tension**: Players see threats approaching from above but can only act when they're close enough (below the line)
- **Reaction Space**: Higher line position provides more reaction time and strategic options

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
| **Matrix** | Blue | Standard step movement | Creates detonation markers | Generates cube markers on capture |
| **Infinity** | Black | Standard/Paused movement | **Uncapturable** | Can pause, destroys colliding cubes, corrupts tiles |
| **Recursion** | Darker/Metallic | Standard step movement | Requires multiple hits | Increased durability |

### Movement System
- **Step-Based Progression**: Discrete grid movement per wave step
- **Consistent Timing**: Configurable `moveInterval` per wave
- **Forward Only**: Cubes move down the grid toward escape
- **Speed Variants**: Normal and fast movement modes
- **Collision Detection**: With player, grid boundaries, and other cubes
- **Pause States**: Infinity cubes can enter paused movement states

### Cube Collision Matrix

**Refined Collision Table**

| Player Cube | Wave Cube | Behavior | Description |
|-------------|-----------|----------|-------------|
| Unit | Unit | Standard capture | Player Unit collides with Wave Unit and removes it from the grid |
| Unit | Matrix | 2x2 area capture | Player Unit collides with Wave Matrix and triggers a 2x2 capture area centered on collision point |
| Unit | Recursion | Column capture | Player Unit collides with Wave Recursion and auto-captures 3 cubes as wave passes over collision tile |
| Unit | Infinity | Face paint, Unit destroyed | Player Unit collides with Wave Infinity, paints collision face, Unit destroyed; when face touches grid, Unit marker placed at that tile; auto-captures next cube that passes over |
| Matrix | Unit | 2x2 area capture | Player Matrix collides with Wave Unit and triggers a 2x2 capture area expanding from Matrix's position |
| Matrix | Matrix | Triggerable 3x3 marker | Player Matrix collides with Wave Matrix and creates a 3x3 manual marker centered on collision point; single trigger |
| Matrix | Recursion | Degrading 2x2 marker | Player Matrix collides with Wave Recursion and creates a 2x2 area marker; each tile has 1 charge; collision point and diagonal opposite degrade last; player manually triggers; area shrinks over triggers |
| Matrix | Infinity | Face paint, Matrix destroyed | Player Matrix collides with Wave Infinity, paints collision face, Matrix destroyed; when face touches grid, 2x2 manual marker placed at that tile |
| Recursion | Unit | Column capture | Player Recursion collides with Wave Unit and auto-captures 3 cubes as wave passes over collision tile |
| Recursion | Matrix | Auto 1x3 marker | Player Recursion collides with Wave Matrix and creates a 1x3 vertical marker (3 tiles deep); each tile auto-captures as wave passes |
| Recursion | Recursion | Cross marker | Player Recursion collides with Wave Recursion and creates a cross-shaped marker (5 tiles - 1x3 vertical + 1x3 horizontal, overlapping at center); each tile auto-captures as wave passes |
| Recursion | Infinity | Face paint, Recursion destroyed | Player Recursion collides with Wave Recursion, paints collision face, Recursion destroyed; when face touches grid, auto-capture marker placed at that tile; captures 3 cubes as wave passes |
| Infinity | Unit | Wave join | Player Infinity collides with Wave Unit, removes Unit, takes its position; moves with wave; passes through harmlessly at player edge |
| Infinity | Matrix | Face paint, continue up | Player Infinity collides with Wave Matrix, paints collision face, continues up until exiting grid; when face touches grid, 2x2 manual marker placed at that tile |
| Infinity | Recursion | Face paint, continue up | Player Infinity collides with Wave Recursion, paints collision face, continues up until exiting grid; when face touches grid, auto-capture marker placed at that tile; captures 3 cubes as wave passes |
| Infinity | Infinity | Face paint, resonance | Player Infinity collides with Wave Infinity, paints collision face, continues up until exiting grid; when face touches grid, ALL Infinity cubes on grid become phaseable for that turn |

---

**Quick Reference: Cube Identities**

| Cube | Identity | Trigger Type | Shape Language |
|------|----------|--------------|----------------|
| Unit | Simple, foundational | Instant | Single tile |
| Matrix | Area, expansion | Manual | 2x2, 3x3 squares |
| Recursion | Repetition, concentration | Auto | 1x3 lines, cross |
| Infinity | Immutable, rhythmic | Painted face (inherits target behavior) | N/A - affects other cubes |


### Face Painting System
Advanced cube state modification system that dynamically alters Infinity cube behavior through delayed marker placement:

#### Core Mechanism
- **Collision Trigger**: When any player cube collides with a Wave Infinity cube, the collision face gets painted
- **Cube Continuation**: The painted Infinity cube continues moving with the wave after collision
- **Predictable Rotation**: Cubes rotate on a fixed, predictable schedule as the wave advances
- **Marker Placement**: When the painted face rotates down and touches the grid, a marker of the painted type appears at that tile
- **Timing Mastery**: Players must learn the rotation rhythm to predict where markers will appear

#### Face Status Types
- **None**: Standard behavior, no modification
- **Matrix**: When face touches the grid, a 2x2 manual marker is placed that player can trigger for area capture
- **Recursion**: When face touches the grid, an auto-capture marker is placed that automatically captures 3 cubes as the wave passes over
- **Unit**: When face touches the grid, a Unit marker is placed that auto-captures the next cube that passes over
- **Infinity**: When face touches the grid, triggers Resonance effect (see Resonance System below)

#### Resonance System (Infinity vs Infinity)
Special interaction when Player Infinity collides with Wave Infinity:
- **Face Painting**: Collision face is painted as normal
- **Resonance Trigger**: When the painted face touches the grid, ALL Infinity cubes currently on the grid become phaseable for that turn
- **Phaseable State**: Phaseable Infinity cubes can be passed through by other player cubes
- **Strategic Sequencing**: Enables advanced strategy: paint multiple Infinity cubes, then sequence follow-up cubes to hit targets behind them
- **High Reward**: Resonance triggers provide significant strategic advantage and reward

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
  - `2` = Matrix Marker mode
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

#### Matrix Markers
- **Mode Key**: `2`
- **Placement**: `F` when in Matrix mode
- **Automatic Spawning**: Spawns Matrix cube when wave moves forward (2x2 area effect, 3x3 for Matrix+Matrix collisions)
- **Coverage**: 2x2 grid area (from marker), 3x3 for Matrix+Matrix collisions
- **Cooldown System**: Time-based restrictions
- **Resource Limits**: Configurable maximum on-grid count
- **Wave Inheritance**: Center position recorded for next wave cube spawn

#### Cube Markers
- **Trigger Key**: `R` (KeyCode.R)
- **Power Up Key**: `E` (KeyCode.E) - powers up cube marker (if implemented)
- **Generation**: Created automatically from collisions:
  - Matrix+Matrix collision → Matrix cube marker (3x3 area)
  - Recursion+Recursion collision → Recursion cube marker (2x2 area)
  - Matrix captured by non-Matrix → Matrix cube marker (2x2 area)
- **Behavior**: When triggered with `R`, creates area effect that expands from cube marker position and captures all non-Infinity cubes in the area
  - Matrix+Matrix cube marker: 3x3 area effect
  - Recursion+Recursion cube marker: 2x2 area effect
  - Matrix (non-matching) cube marker: 2x2 area effect
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
- **Cube Captures**: By type (Unit, Matrix, Infinity attempts, Recursion)
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
  - Matrix Marker → Matrix Cube (center of area)
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
| Light | Unit Cube | Random Unit/Matrix | Stage-specific |
| Heavy | Recursion Cube | Dense variant | Resource availability |
| Matrix | Matrix Cube (center) | 3x3 Unit formation | Area overlap |
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

### Marker Economy
Resource management system that creates strategic depth:

#### Per Stage Grant
- **Fixed Allocation**: Players receive a fixed number of non-Unit markers at stage start
- **Cross-Wave Management**: Players manage this inventory across all waves in that stage
- **No Replenishment**: Spent markers do not replenish until next stage
- **Strategic Conservation**: Forces players to balance immediate needs with future wave requirements

#### Marker Behavior by Type
- **Unit Markers**: Unlimited availability, always accessible
- **Matrix Markers**: Scarce resource, manual trigger required
- **Recursion Markers**: Scarce resource, auto-trigger behavior
- **Infinity Markers**: Very scarce, unlocked later in progression

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

## 3.6 Penalty and Reward System

### Penalty System
Consequences for player cube failures:

| Action | Penalty |
|--------|---------|
| Unit cube falls off grid | Line moves down 1 row |
| Matrix cube falls off grid | Line moves down 2 rows |
| Recursion cube falls off grid | Line moves down 2 rows |
| Infinity cube falls off grid | No penalty (intended behavior) |

### Reward System
Benefits for successful play:

| Action | Reward |
|--------|--------|
| Perfect wave clear (all non-Infinity captured) | Line moves up 1 row |
| Painted face triggers | Line moves up 1 row |
| Resonance triggers (all Infinity phaseable) | Line moves up 2 rows |

## 3.7 Strategic Implications

### Early Game Loop (No Player Infinity)
Strategic pattern for stages before Infinity markers unlock:
1. **Identify Infinity Threat**: See Infinity cube in wave
2. **Sacrifice Decision**: Sacrifice Matrix/Recursion marker to paint it
3. **Marker Destruction**: Marker is destroyed, face is painted
4. **Timing Wait**: Wait for painted face to touch grid
5. **Marker Appearance**: Marker appears on grid at that tile
6. **Trigger Execution**: Marker triggers (manual or auto) to capture cubes

### Late Game Loop (Player Infinity Unlocked)
Advanced strategic pattern once Infinity markers are available:
1. **Threat Assessment**: See Infinity cube blocking valuable targets
2. **Infinity Placement**: Place Infinity marker first
3. **Resonance Setup**: Infinity vs Infinity = face painted, resonance window coming
4. **Follow-Up Placement**: Place Matrix/Recursion marker behind Infinity marker
5. **Resonance Activation**: When resonance triggers, all Infinity cubes become phaseable
6. **Target Access**: Follow-up marker's cube passes through phaseable Infinity
7. **Target Capture**: Captures targets behind Infinity that were previously unreachable

### Mastery Play
Advanced techniques for expert players:
- **Chain Painting**: Chain multiple Infinity paintings to create predictable resonance windows
- **Timing Optimization**: Time marker placements so follow-up cubes arrive during phaseable turns
- **Resource Management**: Manage marker economy across waves to ensure tools are available when needed
- **Line Advancement**: Push line upward through perfect clears and painted triggers to maximize reaction space
- **Pattern Reading**: Read wave composition to decide when to spend scarce markers vs. rely on Units

### Paired Wave Strategies
#### Offensive Strategies
- **Spawn Trapping**: Place markers to create difficult next-wave patterns
- **Cascade Setup**: Position markers for chain reactions in next wave
- **Resource Generation**: Strategic Matrix marker placement for future Matrix cubes

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

## 3.8 Trigger Consistency Rules

Core principles governing marker trigger behavior:

- **Matrix Interactions** = Manual trigger (player detonates)
  - Matrix vs Matrix: 3x3 manual marker
  - Matrix vs Recursion: 2x2 degrading manual marker
  - Matrix vs Infinity: 2x2 manual marker (from painted face)
  - Matrix vs Unit: 2x2 area capture

- **Recursion Interactions** = Auto trigger (wave movement triggers)
  - Recursion vs Recursion: Cross marker (auto-captures)
  - Recursion vs Matrix: 1x3 vertical marker (auto-captures)
  - Recursion vs Infinity: Auto-capture marker (from painted face)
  - Recursion vs Unit: Column capture (auto)

- **Infinity Painted Faces** = Inherit trigger type of painted cube
  - Unit/Recursion painted = Auto trigger
  - Matrix painted = Manual trigger

## 3.9 Shape Language

Visual and mechanical patterns created by cube interactions:

| Interaction Type | Resulting Shape | Trigger Type |
|------------------|-----------------|--------------|
| Unit vs Unit | Single tile | Instant |
| Unit vs Matrix / Matrix vs Unit | 2x2 square | Manual / Area |
| Matrix vs Matrix | 3x3 square | Manual |
| Matrix vs Recursion | 2x2 degrading (1 charge per tile) | Manual |
| Recursion vs Unit / Unit vs Recursion | Single tile, 3 charges | Auto |
| Recursion vs Matrix | 1x3 vertical line | Auto |
| Recursion vs Recursion | Cross (5 tiles) | Auto |

## 3.10 Face Painting Visual Feedback

Player communication system for painted face mechanics:

1. **Visual Indicator on Cube**: Painted face has distinct color/glow to show modification
2. **Grid Telegraph**: When painted face is 1 turn from touching grid, target tile pulses to indicate marker placement location
3. **Fixed Rotation Schedule**: Cubes rotate predictably; players learn the rhythm through repeated exposure
4. **Timing Mastery**: Visual feedback enables players to predict and plan marker placement locations

## 3.11 Design Principles

### From Intelligence Qube
Core mechanics inherited from the inspiration:
- **Wave Pressure**: Wave pressure toward player creates urgency
- **Cube Clearing**: Cube clearing as core gameplay loop
- **Hazard Cubes**: Hazard cube you must avoid (Infinity = Forbidden)
- **Multi-Cube Tools**: Markers that enable multi-cube clears
- **Spatial Puzzle**: Spatial puzzle solving under time pressure

### Evolution Beyond IQ
Mechanical innovations that extend the formula:
- **Player Cubes as Projectiles**: Player cubes as projectiles, not static markers
- **Full Collision Matrix**: Complete collision matrix with meaningful combinations
- **Face Painting**: Face painting as delayed marker placement system
- **Resonance**: Resonance as long-term strategic goal
- **Trigger Split**: Trigger type split: Matrix = manual, Recursion = auto
- **Dynamic Difficulty**: Line divider as dynamic difficulty instead of stage shrinkage

## 3.12 Configuration Compression
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
    {position: (6,10), type: "Matrix", delay: 2}
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

## 3.13 Debug System
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

## 3.14 Open Items for Playtesting

Areas requiring player testing and iteration:

- **Marker Economy**: Exact number of non-Unit markers granted per stage
- **Allocation Method**: Whether marker grants are fixed ratio or player-allocated
- **Line Movement Values**: Fine-tuning line movement values (rows up/down)
- **Wave Density**: Wave density progression across stages
- **Recursion Capture Count**: Currently 3 cubes, may adjust based on balance
- **Rotation Schedule**: Rotation schedule timing (every N advances)

---
**Last Updated:** November 17, 2025  
**Implementation Status:** Core mechanics production-ready, line divider and resonance systems in design phase  
**Major Additions:** Line divider system, enhanced face painting with resonance, penalty/reward system, marker economy
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
- [Technical Architecture](TechnicalArchitecture.md)