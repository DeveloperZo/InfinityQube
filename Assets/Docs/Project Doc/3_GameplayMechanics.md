# Gameplay Mechanics

> This document details the core mechanical systems of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Explains how the game works: what players can do, how cubes behave, how markers function, and how waves progress. Describes the rules and systems as they currently work in the game.

## 3.1 Grid System
### Core Structure
- **Grid Size**: Each stage has its own grid size (width x height)
- **Boundaries**: Player cannot move outside the grid
- **Playable Area**: Grid can shrink when penalties are applied (bottom rows removed)

### Tile States (PAUSED AND NOT IMPLEMENTED)
| State | Behavior | Visual Indicator | Player Interaction |
|-------|----------|------------------|-------------------|
| Normal | Default state | Base height | Can place markers |
| Transformed | Modified by cube interaction | Height variation | Modified marker behavior |
| Corrupted | Rejects markers, paints cubes | Visual corruption effect | Cannot place markers |
| Marked | Contains active marker | Marker visualization | Awaiting detonation |

## 3.2 Marker System
### Marker Types
Players have access to four marker types, each creating different player cubes:

#### Unit Markers
- **Mode**: Press 1 to select
- **Placement**: Press F to place
- **Spawning**: Becomes a Unit cube when move forward occurs
- **Charges**: Limited uses that regenerate over time
- **Visual Feedback**: Shows placement indicators and remaining charges

#### Matrix Markers
- **Mode**: Press 2 to select
- **Placement**: Press F to place
- **Spawning**: Becomes a Matrix cube when move forward occurs
- **Coverage**: Creates 2x2 area effect (3x3 when Matrix collides with Matrix)
- **Cooldown**: Time-based restrictions between uses
- **Resource Limits**: Maximum number that can be on grid at once

#### Recursion Markers
- **Mode**: Press 3 to select
- **Placement**: Press F to place
- **Spawning**: Becomes a Recursion cube when move forward occurs
- **Purpose**: Designed specifically to capture Recursion cubes
- **Charges**: Maximum 2 markers at once, limited charges with cooldown between uses
- **Power**: Works best for multi-hit Recursion cube interactions

#### Infinity Markers
- **Mode**: Press 4 to select
- **Placement**: Press F to place
- **Spawning**: Becomes an Infinity cube when move forward occurs
- **Effect**: Creates cubes that can affect other Infinity cubes
- **Charges**: Very limited uses with long cooldown between uses
- **Range**: Affects Infinity cubes nearby

### Marker Economy
Resource management system that creates strategic depth:

#### Grant System
Markers are given to players at specific times:
| Grant Type | Recursion | Matrix | Infinity | When |
|------------|-----------|--------|----------|------|
| **Stage Start** | 5 markers | 3 markers | 2 markers | At beginning of stage |
| **Wave Start** | +1 marker | +1 marker | +0 markers | At start of each wave |
| **Maximum** | 8 total | 5 total | 3 total | Cannot exceed these limits |

#### Marker Behavior by Type
- **Unit Markers**: Regenerate over time, unlimited total
- **Matrix Markers**: Given as grants, player triggers manually, maximum 5 at once
- **Recursion Markers**: Given as grants, trigger automatically, maximum 8 at once
- **Infinity Markers**: Given as grants, very limited, maximum 3 at once

#### Economy Mode
- **Grant System**: Non-Unit markers use grants (no regeneration) - normal gameplay
- **Cooldown System**: All markers regenerate over time - testing mode only

### Marker Placement Rules
- **Valid Position**: Must be within grid boundaries
- **Tile State**: Cannot place on corrupted tiles or tiles that already have markers
- **Charges**: Must have available charges for that marker type

### Cube Markers
Special markers created from collisions, not player-placed:
- **Trigger**: Press R to activate
- **Generation**: Created automatically from collisions:
  - Matrix colliding with Matrix creates a Matrix cube marker (3x3 area)
  - Recursion colliding with Recursion creates a Recursion cube marker (auto trigger)
  - Matrix captured by non-Matrix creates a Matrix cube marker (2x2 area)
- **Behavior**: When triggered with R, creates an area effect that captures all non-Infinity cubes in the area
  - Matrix+Matrix cube marker: 3x3 area effect
  - Recursion+Recursion cube marker: (auto trigger)
  - Matrix (non-matching) cube marker: 2x2 area effect
- **Strategic Resource**: Limited and valuable, earned through skillful cube matching

## 3.3 Marker to Cube Conversion
**Core Mechanic**: The game's defining system where placed markers transform into player cubes.

### Wave Start Sequence
1. **Wave Starts**: Player presses ENTER to start the wave
2. **Marker Placement**: Player can place markers on the grid
3. **Move Forward Occurs**: When the wave advances:
   - **Wave cubes move down one tile**
   - **All placed markers convert to player cubes** (at marker positions)
   - **Existing player cubes move up one tile**

### Transformation Process
- **Trigger**: When move forward occurs, all placed markers on the grid transform into player cubes
- **Type Matching**: Each marker type becomes its corresponding cube type:
  - Unit marker → Unit cube
  - Matrix marker → Matrix cube
  - Recursion marker → Recursion cube
  - Infinity marker → Infinity cube
- **Position**: Player cubes spawn at the marker's grid position
- **Direction**: Player cubes move backward (up the grid) toward incoming waves

### Backward Movement
- **Direction**: Player cubes move opposite to wave cubes (up vs down)
- **Speed**: Moves one tile per wave step, matching wave movement timing
- **Destruction**: Player cubes destroyed when they reach the top of the grid (position.y >= grid.Height)
- **Penalty**: Player cubes destroyed at the top add to penalty count (same as wave cubes escaping)
- **Purpose**: Creates collision opportunities where player cubes meet wave cubes

### Strategic Implications
- **Placement Timing**: Markers placed earlier create cubes that travel further before collision
- **Position Planning**: Marker placement determines where collisions will occur
- **Resource Management**: Markers are consumed when they transform, requiring strategic placement decisions

## 3.4 Cube System
### Core Cube Types
| Type | Visual | Movement | Capture Behavior | Special Properties |
|------|--------|----------|------------------|-------------------|
| **Unit** | Gray | Moves one tile per step | Can be captured | Basic scoring value |
| **Matrix** | Blue | Moves one tile per step | Can be captured | Creates cube markers when captured |
| **Infinity** | Black | Moves one tile per step | **Cannot be captured** | Destroys player cubes, face paintable |
| **Recursion** | Darker/Metallic | Moves one tile per step | Requires multiple hits to capture | More durable than other cubes |

### Movement System
- **Step-by-Step**: Cubes move one tile at a time when the wave advances
- **Wave Speed**: Each wave moves at its own speed
- **Direction**: Wave cubes move down toward escape; player cubes move up toward waves
- **Collisions**: Cubes collide with player cubes, grid edges, and other cubes

### Cube Collision Matrix

**Current Stage Design Focus**

Early stages use only Unit markers (player) against Unit, Matrix, and Infinity cubes (wave). The collision matrix below documents these three collision types:

| Player Cube | Wave Cube | Behavior | Description |
|-------------|-----------|----------|-------------|
| Unit | Unit | Standard capture | Player Unit collides with Wave Unit and removes it from the grid |
| Unit | Matrix | 2x2 manual marker | Player Unit collides with Wave Matrix, captures Matrix cube, and creates a 2x2 manual trigger marker (player triggers with R) |
| Unit | Infinity | Unit destroyed | Player Unit collides with Wave Infinity, Unit destroyed (no face painting) |

**Advanced Collision Types**

As players progress, Matrix, Recursion, and Infinity markers unlock. These create additional collision combinations:

**Planned Behaviors** 
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
| Infinity | Matrix | Wave join + face paint | Player Infinity collides with Wave Matrix, paints Player Infinity's face with Matrix status, captures Matrix, joins wave and moves downward; when painted face touches grid, 2x2 manual marker placed |
| Infinity | Recursion | Wave join + face paint | Player Infinity collides with Wave Recursion, paints Player Infinity's face with Recursion status, captures Recursion, joins wave and moves downward; when painted face touches grid, auto-capture marker placed |
| Infinity | Infinity | Face paint Wave + consumed | Player Infinity collides with Wave Infinity, paints Wave Infinity's face with Infinity status, Player Infinity is consumed (cost); when painted face touches grid, ALL Infinity cubes become phaseable |

**Quick Reference: Cube Identities**

| Cube | Identity | Trigger Type | Shape Language |
|------|----------|--------------|----------------|
| Unit | Simple, foundational | Instant | Single tile |
| Matrix | Area, expansion | Manual | 2x2, 3x3 squares |
| Recursion | Repetition, concentration | Auto | 1x3 lines, cross |
| Infinity | Immutable, rhythmic | Painted face (inherits target behavior) | N/A - affects other cubes |

### Face Painting System
When certain player cubes collide with Infinity cubes, they paint the collision face. Later, when that painted face touches the grid, a marker appears:

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

## 3.5 Player System
### Movement Mechanics
- **Controls**: WASD or Arrow keys move the player smoothly
- **Grid-Based**: Player moves within grid boundaries
- **Smooth Movement**: Player movement has acceleration and deceleration
- **Rotation**: Player character faces the direction they're moving

### Action System
Players place markers and trigger effects using a unified control scheme:

#### Controls
- **Mode Selection**: Press 1, 2, 3, or 4 to switch between marker types
  - 1 = Unit Marker mode
  - 2 = Matrix Marker mode
  - 3 = Recursion Marker mode
  - 4 = Infinity Marker mode
- **Placement**: Press F to place a marker of the current type
- **Wave Start**: Press ENTER to start wave and trigger marker-to-cube conversion
- **Cube Marker Trigger**: Press R to trigger cube markers (created from collisions) for area effects

### Player Death and Respawn
- **Death Trigger**: Player dies when occupying the same tile as any wave cube
- **Respawn**: Player respawns after a configurable delay
- **Invulnerability**: Brief invulnerability period after respawn
- **Death Penalty**: If player dies twice in a wave, bottom row is removed
- **Wave Failure**: Player death can cause wave failure depending on wave configuration

### Player Statistics
The game tracks player performance:
- **Cube Captures**: Counts captures by cube type
- **Marker Usage**: Tracks how many markers of each type were placed and triggered
- **Wave Performance**: Measures success rate and efficiency
- **Movement**: Tracks how far and how long the player moved
- **Failures**: Tracks when player cubes are destroyed
- **Deaths**: Tracks player death count

## 3.6 Wave Management System

### Wave Configuration
Each wave has:
- **Unique Identifier**: Each wave has a unique name/number
- **Cube Spawns**: Which cubes appear and where
- **Movement Speed**: How fast cubes move
- **Grid Size**: Dimensions for that wave
- **Escape Limits**: Maximum number of cubes that can escape before wave fails

### Wave Progression
- **Manual Start**: Press ENTER to start each wave
- **Step-Based Movement**: Cubes move one step at a time
- **Configurable Speed**: Each wave can have different movement speed
- **Movement Sequence**: On each move forward step:
  - Wave cubes move down one tile
  - Markers convert to player cubes
  - Existing player cubes move up one tile

### Wave Phases
- **Spawn**: Cubes appear on the grid
- **Active**: Cubes move and collisions happen
- **Completion**: Wave ends when all cubes have left the grid area
- **Transition**: Prepares for next wave

### Wave Completion
A wave completes when:
- **All Cubes Left Grid**: All cubes (wave cubes and player cubes) have either escaped, been destroyed, or been captured
- **No Active Cubes**: No cubes remain on the grid
- **Natural Progression**: Wave automatically advances to next wave when complete

### Wave Failure Conditions
Waves can be configured with failure conditions that trigger wave failure:

#### Escape-Based Failure
- **Configurable Limit**: Each wave can set a maximum number of cubes allowed to leave the grid
- **Failure Trigger**: If the number of cubes leaving exceeds the configured limit, the wave fails immediately
- **What Counts**: Both wave cubes escaping (bottom) and player cubes destroyed (top) count toward the escape limit
- **Configuration Options**:
  - `-1` = Unlimited escapes (no failure from escapes)
  - `0` = No escapes allowed (any cube leaving fails the wave)
  - `>0` = Maximum allowed escapes (wave fails if exceeded)
- **Wave-Specific**: Each wave can have its own escape limit separate from the stage's overall limit
- **Failure Behavior**: When wave fails, it stops immediately and can be restarted

#### Other Failure Conditions
- **Player Death**: Waves can be configured to fail on player death (if configured)
- **Custom Criteria**: Additional failure conditions can be configured per wave

### Wave Restart
Waves can be restarted when:
- **Wave Failure**: If a wave fails due to exceeding escape limits or other criteria
- **Conditions Too Harsh**: If penalties make the wave too difficult, player can restart
- **Manual Restart**: Player-initiated restart option available

### Cube Leaving Mechanics
Cubes leave the grid in two ways:
- **Wave Cubes Escape**: Move off the bottom of the grid (position.y < 0)
- **Player Cubes Destroyed**: Reach the top of the grid (position.y >= grid.Height)
- **Completion Trigger**: Wave completes when all cubes have left the grid area

## 3.7 Penalty and Reward System

### Penalty System
Penalties apply when any non-Infinity cube leaves the grid area:

| Action | Penalty |
|--------|---------|
| Any non-Infinity cube leaves grid | Adds to penalty count; contributes to row removal penalties |
| Unit cube leaves (escapes bottom or destroyed at top) | Adds to escape count; when count equals grid width, bottom row removed |
| Matrix cube leaves | Adds to penalty count; contributes to row removal penalties |
| Recursion cube leaves | Adds to penalty count; contributes to row removal penalties |
| Infinity cube leaves | **No penalty** (this is expected behavior) |
| Player dies twice in wave | Bottom row removed (death penalty) |

**Key Rules**:
- **Penalties apply to any non-Infinity cube leaving**: Both wave cubes escaping (bottom) and player cubes destroyed (top) count toward penalties
- **No penalty for Infinity cubes**: Infinity cubes leaving the grid is expected and causes no penalty
- **Player cubes treated same as wave cubes**: Player cubes destroyed at the top are counted the same as wave cubes escaping at the bottom

### Reward System
Currently, only one action provides rewards:

| Action | Reward |
|--------|--------|
| Perfect wave clear (all capturable cubes captured) | Bottom row restored (grid expands by 1 row) |

**Perfect Wave Clear**: A wave achieves perfect clear when all capturable cubes (Unit, Matrix, Recursion) are captured before leaving the grid. This is the only rewardable action currently implemented.

## 3.8 Strategic Implications

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
- **Chain Painting**: Paint multiple Infinity cubes to create predictable resonance windows
- **Timing Optimization**: Time marker placements so follow-up cubes arrive when Infinity cubes are phaseable
- **Resource Management**: Save markers across waves to have the right tools when needed
- **Pattern Reading**: Analyze wave composition to decide when to use scarce markers vs. rely on Unit markers

## 3.9 Trigger Consistency Rules

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

## 3.10 Shape Language

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

## 3.11 Face Painting Visual Feedback

Player communication system for painted face mechanics:

1. **Visual Indicator on Cube**: Painted face has distinct color/glow to show modification
2. **Grid Telegraph**: When painted face is 1 turn from touching grid, target tile pulses to indicate marker placement location
3. **Fixed Rotation Schedule**: Cubes rotate predictably; players learn the rhythm through repeated exposure
4. **Timing Mastery**: Visual feedback enables players to predict and plan marker placement locations

## 3.12 Design Principles

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


## 3.13 Open Items for Playtesting

Areas requiring player testing and iteration:

- **Marker Economy**: Exact number of non-Unit markers granted per stage
- **Allocation Method**: Whether marker grants are fixed ratio or player-allocated
- **Wave Density**: Wave density progression across stages
- **Recursion Capture Count**: Currently 3 cubes, may adjust based on balance
- **Rotation Schedule**: Rotation schedule timing (every N advances)

---
**Last Updated:** December 14, 2025  
**Implementation Status:** Core mechanics production-ready, Stages 0-2 validated, resonance systems implemented, all collision mechanics working, stage progression complete  
**Major Systems:** Collision matrix, face painting with resonance, penalty/reward system, marker economy  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
