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

**Current Stage Design Focus**

Current stages use only Unit markers (player) against Unit, Matrix, and Infinity cubes (wave). The collision matrix below documents these three collision types:

| Player Cube | Wave Cube | Behavior | Description |
|-------------|-----------|----------|-------------|
| Unit | Unit | Standard capture | Player Unit collides with Wave Unit and removes it from the grid |
| Unit | Matrix | 2x2 manual marker | Player Unit collides with Wave Matrix, captures Matrix cube, and creates a 2x2 manual trigger marker (player triggers with R) |
| Unit | Infinity | Unit destroyed, marker placement | Player Unit collides with Wave Infinity, Unit destroyed (no face painting); when face touches grid, Unit marker placed at that tile; auto-captures next cube that passes over |

**Future Collision Types** (To be documented when implementation reaches these combinations)

The following collision combinations will be documented as Matrix, Recursion, and Infinity markers are introduced in later stages:
- Matrix marker collisions (Matrix vs Unit, Matrix, Recursion, Infinity)
- Recursion marker collisions (Recursion vs Unit, Matrix, Recursion, Infinity)
- Infinity marker collisions (Infinity vs Unit, Matrix, Recursion, Infinity)

Tentative Behavior 
| Player Cube | Wave Cube | Behavior | Description |
|-------------|-----------|----------|-------------|
| Matrix | Unit | 2x2 area capture | Player Matrix collides with Wave Unit and triggers a 2x2 capture area expanding from Matrix's position |
| Matrix | Matrix | Triggerable 3x3 marker | Player Matrix collides with Wave Matrix and creates a 3x3 manual marker centered on collision point; single trigger |
| Matrix | Recursion | Degrading 2x2 marker | Player Matrix collides with Wave Recursion and creates a 2x2 area marker; each tile has 1 charge; collision point and diagonal opposite degrade last; player manually triggers; area shrinks over triggers |
| Matrix | Infinity | Face paint, Matrix destroyed | Player Matrix collides with Wave Infinity, paints collision face, Matrix destroyed; when face touches grid, 2x2 manual marker placed at that tile |
| Recursion | Unit | A recursion marker with 3 charges | Player Recursion collides with Wave Unit a creates a marker; auto-captures 3 cubes or expires after 5 move forwards |
| Recursion | Matrix | Auto 3x1 horizontal marker | Player Recursion collides with Wave Matrix and creates a 3x1 horizontal marker (3 tiles wide); 2 charges total, auto-captures as wave passes |
| Recursion | Recursion | Cross marker | Player Recursion collides with Wave Recursion and creates a cross-shaped marker (5 tiles - 1x3 vertical + 1x3 horizontal, overlapping at center); each tile auto-captures as wave passes |
| Recursion | Infinity | Face paint + marker, Recursion destroyed | Player Recursion collides with Wave Infinity, paints Wave Infinity's face with Recursion status, leaves 1-charge recursion marker at collision point, Recursion destroyed |
| Infinity | Unit | Wave join | Player Infinity destroys Wave Unit, takes its position, joins wave and moves downward with it |
| Infinity | Matrix | Face paint Player (1 charge), continue up | Player Infinity collides with Wave Matrix, paints Player Infinity's face with Matrix status (1 charge), captures Matrix, continues up; when face touches grid, 2x2 manual marker placed |
| Infinity | Recursion | Face paint Player, continue up | Player Infinity collides with Wave Recursion, paints Player Infinity's face with Recursion status, captures Recursion, continues up; when face touches grid, auto-capture marker placed |
| Infinity | Infinity | Face paint Wave, Player destroyed | Player Infinity collides with Wave Infinity, paints Wave Infinity's face with Infinity status, Player Infinity destroyed (cost); when face touches grid, ALL Infinity cubes become phaseable |

---

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
- **Collision Trigger**: When Matrix, Recursion, or Infinity player cubes collide with a Wave Infinity cube, the collision face gets painted
- **Unit Cube Exception**: Unit cubes do NOT paint Infinity cubes - they are destroyed on collision without painting
- **Cube Continuation**: The painted Infinity cube continues moving with the wave after collision
- **Predictable Rotation**: Cubes rotate on a fixed, predictable schedule as the wave advances
- **Marker Placement**: When the painted face rotates down and touches the grid, a marker of the painted type appears at that tile
- **Configurable Telegraph Window**: Each wave can configure how many moves ahead to show telegraph indicators (default: 3 moves)
- **Telegraph System**: Visual indicators show on destination tiles for all painted faces that will touch the grid within the configured window
- **Timing Mastery**: Players learn the rotation rhythm and use telegraphs to predict where markers will appear

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

#### Recursion Markers
- **Mode Key**: `3`
- **Placement**: `F` when in Recursion mode
- **Automatic Spawning**: Spawns Recursion cube when wave moves forward
- **Primary Target**: Enhanced marker specifically designed for Recursion cubes
- **Charge System**: Maximum 2 markers, limited charges with 5-second cooldown
- **Enhanced Power**: Optimized for multi-hit Recursion cube interactions

#### Matrix Markers
- **Mode Key**: `2`
- **Placement**: `F` when in Matrix mode
- **Automatic Spawning**: Spawns Matrix cube when wave moves forward (2x2 area effect, 3x3 for Matrix+Matrix collisions)
- **Coverage**: 2x2 grid area (from marker), 3x3 for Matrix+Matrix collisions
- **Cooldown System**: Time-based restrictions
- **Resource Limits**: Configurable maximum on-grid count

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

#### Infinity Markers
- **Mode Key**: `4`
- **Placement**: `F` when in Infinity mode
- **Automatic Spawning**: Spawns Infinity cube when wave moves forward
- **Effect**: Spawns pause-inducing cubes that affect Infinity cubes
- **Charge System**: Limited uses with strategic regeneration (default: 1 charge, 15s cooldown)
- **Interaction Range**: Affects Infinity cubes within proximity

### Player Statistics
Comprehensive tracking system:
- **Cube Captures**: By type (Unit, Matrix, Infinity attempts, Recursion)
- **Marker Usage**: Four-tier marker placement/triggers (Unit, Matrix, Recursion, Infinity)
- **Wave Performance**: Success rate and efficiency metrics
- **Movement Tracking**: Distance and time
- **Death/Respawn**: Player mortality events

## 3.4 Wave Management System

### Wave Configuration (WaveData ScriptableObject)
```
WaveData Structure:
- waveID: Unique identifier
- baseSpawns: Cube spawn configurations
- moveInterval: Time between cube movements
- gridWidth/gridHeight: Wave dimensions
```

### Wave Progression
- **Manual Control**: ENTER to start waves
- **Step-Based Movement**: Discrete cube advancement
- **Configurable Timing**: Per-wave `moveInterval` settings

### Wave Events
- **Spawn Phase**: Initial cube placement
- **Active Phase**: Ongoing cube movement with pause mechanics
- **Resolution Phase**: Success/failure determination
- **Transition Phase**: Prepare next wave

## 3.5 Marker System

### Marker Economy
Resource management system that creates strategic depth:

#### Grant System (Hybrid Stage + Wave)
| Grant Type | Recursion | Matrix | Infinity | Behavior |
|------------|-----------|--------|----------|----------|
| **Stage Grant** | 5 | 3 | 2 | SET inventory (at stage start) |
| **Wave Grant** | +1 | +1 | +0 | ADD to inventory (at wave start) |
| **Inventory Cap** | 8 | 5 | 3 | Maximum holdings |

#### Marker Behavior by Type
- **Unit Markers**: Cooldown-based regeneration, always accessible (unlimited total)
- **Matrix Markers**: Grant-based, manual trigger, caps at 5
- **Recursion Markers**: Grant-based, auto-trigger, caps at 8
- **Infinity Markers**: Grant-based, very scarce, caps at 3

#### Economy Toggle
- **useMarkerEconomy = true**: Non-Unit markers use grant system (no regeneration)
- **useMarkerEconomy = false**: All markers use cooldown-based regeneration (testing mode)

### Marker Placement Rules
- **Grid Validation**: Must be within valid grid boundaries
- **Tile State Check**: Cannot place on corrupted or occupied tiles
- **Resource Availability**: Sufficient charges available
- **Line Divider Restriction**: Player must be in safe zone (below line) to place markers

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

## 3.8 Trigger Consistency Rules

Core principles governing marker trigger behavior:

- **Matrix Interactions** = Manual trigger (player detonates)
  - Matrix vs Matrix: 3x3 manual marker
  - Matrix vs Recursion: 2x2 degrading manual marker
  - Matrix vs Infinity: 2x2 manual marker (from painted face)
  - Matrix vs Unit: 2x2 area capture

- **Recursion Interactions** = Auto trigger (wave movement triggers)
  - Recursion vs Recursion: Cross marker (auto-captures)
  - Recursion vs Matrix: 3x1 horizontal marker (auto-captures)
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
| Recursion vs Matrix | 3x1 horizontal line | Auto |
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
- **Collision Matrix**: Current focus on Unit marker collisions (Unit-Unit, Unit-Matrix, Unit-Infinity); other combinations documented as future additions
- **Face Painting**: Face painting as delayed marker placement system
- **Resonance**: Resonance as long-term strategic goal
- **Trigger Split**: Trigger type split: Matrix = manual, Recursion = auto
- **Dynamic Difficulty**: Line divider as dynamic difficulty instead of stage shrinkage

## 3.12 Debug System
### Debug Panels
- **Wave Panel**: Visualize wave configuration and progress
- **Collision Panel**: Test collision matrix behavior
- **Marker Panel**: Track marker placement and charges
- **Line Divider Panel**: View and adjust line position
- **Performance Metrics**: Track success rates and timing

### Debug Features
- **Spawn Override**: Manually spawn cubes at positions
- **Wave Skipping**: Jump between waves for testing
- **Visual Debugging**: Highlight collision zones and marker areas
- **Performance Metrics**: Track wave success rates

## 3.13 Open Items for Playtesting

Areas requiring player testing and iteration:

- **Marker Economy**: Exact number of non-Unit markers granted per stage
- **Allocation Method**: Whether marker grants are fixed ratio or player-allocated
- **Line Movement Values**: Fine-tuning line movement values (rows up/down)
- **Wave Density**: Wave density progression across stages
- **Recursion Capture Count**: Currently 3 cubes, may adjust based on balance
- **Rotation Schedule**: Rotation schedule timing (every N advances)

---
**Last Updated:** December 14, 2025  
**Implementation Status:** Core mechanics production-ready, line divider and resonance systems in design phase  
**Major Systems:** Collision matrix, face painting with resonance, penalty/reward system, marker economy, line divider
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
