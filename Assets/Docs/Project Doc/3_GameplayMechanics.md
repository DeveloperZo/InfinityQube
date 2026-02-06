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
- **Purpose**: Repositioning tool that creates swap markers to rearrange cubes on the grid
- **Charges**: Maximum 2 markers at once, limited charges with cooldown between uses
- **Power**: Creates swap markers that reposition cubes, breaking Infinity walls and repositioning value cubes for better capture opportunities

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
- **Recursion Markers**: Given as grants, create swap markers (manual trigger), maximum 8 at once
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
  - Recursion colliding with Recursion creates an empowered swap marker (2 charges: swap + capture)
  - Matrix captured by non-Matrix creates a Matrix cube marker (2x2 area)
  - Unit colliding with Recursion creates a swap marker (1 charge: repositioning)
  - Recursion colliding with Unit creates a swap marker (1 charge: repositioning)
- **Behavior**: When triggered with R, creates effects based on marker type:
  - Matrix+Matrix cube marker: 3x3 area effect
  - Swap markers: Reposition cubes using + pattern swap (N↔S, W↔E)
  - Empowered swap markers: Execute swap, then capture along chosen axis
  - Matrix (non-matching) cube marker: 2x2 area effect
- **Strategic Resource**: Limited and valuable, earned through skillful cube matching

### Swap Markers
Special repositioning markers created from Recursion collisions:
- **Trigger**: Press R to activate (manual trigger, like cube markers)
- **Direction Selection**: Player hovers over swap marker and selects direction with arrow keys:
  - Left/Right arrow: Horizontal swap (row swap, W↔E)
  - Up/Down arrow: Vertical swap (column swap, N↔S)
- **Visual Preview**: Hover icons appear above N, S, E, W positions showing swap destinations
- **Default Direction**: If no direction selected before wave move, defaults to horizontal (row swap)
- **Swap Execution**: Cardinal neighbors swap positions around the collision point:
  - Horizontal: West ↔ East positions swap
  - Vertical: North ↔ South positions swap
- **Edge Handling**: Swaps stop at grid boundaries (no wrapping)
- **Infinity Handling**: Infinity cubes can be moved by swaps but cannot be captured
- **Empowered Swaps**: Recursion+Recursion collisions create 2-charge swap markers:
  - First charge: Swap along chosen axis (player selects swap direction)
  - Second charge: Capture along opposite axis (player selects capture direction)
  - Both axes chosen independently by player

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
| **Infinity** | Black | Moves one tile per step | **Cannot be captured** | Destroys player cubes, immutable (only Infinity affects Infinity) |
| **Recursion** | Darker/Metallic | Moves one tile per step | Requires 2 hits to capture | Multi-hit durability, creates swap markers when collided with |

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

**Advanced Collision Behaviors** 
| Player Cube | Wave Cube | Behavior | Description |
|-------------|-----------|----------|-------------|
| Matrix | Unit | 2x2 area capture | Player Matrix collides with Wave Unit and triggers a 2x2 capture area expanding from Matrix's position |
| Matrix | Matrix | Triggerable 3x3 marker | Player Matrix collides with Wave Matrix and creates a 3x3 manual marker centered on collision point; single trigger |
| Matrix | Recursion | 2x2 Matrix cube marker | Player Matrix collides with Wave Recursion and creates a 2x2 Matrix cube marker (player triggers with R) |
| Matrix | Infinity | Matrix destroyed | Player Matrix collides with Wave Infinity, Matrix is destroyed - Infinity is immutable |
| Recursion | Unit | 1-charge swap marker | Player Recursion collides with Wave Unit and creates a swap marker; player selects direction (horizontal/vertical) and triggers manually with R |
| Recursion | Matrix | 2x2 Matrix cube marker | Player Recursion collides with Wave Matrix and creates a 2x2 Matrix cube marker (player triggers with R) |
| Recursion | Recursion | Empowered swap marker | Player Recursion collides with Wave Recursion, instantly captures Recursion cube, then creates 2-charge swap marker; player selects swap axis and capture axis independently, triggers manually with R |
| Recursion | Infinity | Recursion destroyed | Player Recursion collides with Wave Infinity, Recursion is destroyed - Infinity is immutable |
| Unit | Recursion | 1-charge swap marker | Player Unit collides with Wave Recursion (applies damage), creates swap marker; player selects direction and triggers manually with R |
| Infinity | Unit | Wave join | Player Infinity destroys Wave Unit, takes its position, joins wave and moves downward with it |
| Infinity | Matrix | Capture and continue | Player Infinity captures Wave Matrix, continues moving upward - Infinity is immutable (no painting) |
| Infinity | Recursion | Capture and continue | Player Infinity captures Wave Recursion, continues moving upward - Infinity is immutable (no painting) |
| Infinity | Infinity | Resonance (immediate) | Player Infinity collides with Wave Infinity, triggers resonance immediately - ALL Infinity cubes become phaseable; Player Infinity consumed as cost |

**Quick Reference: Cube Identities**

| Cube | Identity | Trigger Type | Shape Language |
|------|----------|--------------|----------------|
| Unit | Simple, foundational | Instant | Single tile |
| Matrix | Area, expansion | Manual | 2x2, 3x3 squares |
| Recursion | Repositioning, indirect | Manual | + pattern swap (N↔S, W↔E) |
| Infinity | Immutable, eternal | Resonance (only Infinity affects Infinity) | N/A - destroys other player cubes |

### Infinity Immutability Principle
Infinity cubes are truly immutable - nothing can change them except another Infinity.

#### Core Behavior
- **Immutable Nature**: Infinity cubes cannot be painted, transformed, or captured by non-Infinity player cubes
- **Destruction on Contact**: Matrix and Recursion player cubes are destroyed when colliding with Wave Infinity cubes
- **Unit Exception**: Unit player cubes are also destroyed on contact with Wave Infinity (consistent with early design)
- **Only Infinity Affects Infinity**: The sole positive interaction with Infinity cubes comes from the Infinity marker

#### Player Infinity Behavior
- **Infinity + Unit**: Player Infinity destroys Unit and joins wave (wave join)
- **Infinity + Matrix/Recursion**: Player Infinity captures the wave cube and continues moving upward (no painting)
- **Infinity + Infinity**: Triggers resonance immediately (see below)

#### Resonance System (Infinity vs Infinity)
The only positive interaction with Infinity cubes:
- **Immediate Trigger**: When Player Infinity collides with Wave Infinity, resonance triggers immediately (no delay)
- **Phaseable State**: ALL Infinity cubes currently on the grid become phaseable for 2-4 moves
- **Pass-Through**: Phaseable Infinity cubes can be passed through by other player cubes
- **Cost**: Player Infinity is consumed (destroyed) as the cost of triggering resonance
- **Strategic Value**: Enables access to targets blocked by Infinity cubes - high skill, high reward

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
1. **Identify Infinity Threat**: See Infinity cube in wave blocking valuable targets
2. **Avoidance Strategy**: Navigate around Infinity cubes - they cannot be interacted with positively
3. **Capture Priorities**: Focus on capturable cubes (Unit, Matrix, Recursion) while avoiding Infinity
4. **Resource Conservation**: Don't waste Matrix/Recursion markers on Infinity (they will be destroyed)

### Late Game Loop (Player Infinity Unlocked)
Advanced strategic pattern once Infinity markers are available:
1. **Threat Assessment**: See Infinity cube blocking valuable targets
2. **Infinity Placement**: Place Infinity marker to trigger resonance
3. **Immediate Resonance**: Infinity vs Infinity triggers resonance immediately - all Infinity cubes become phaseable
4. **Follow-Up Timing**: Place Matrix/Recursion markers while resonance is active
5. **Target Access**: Follow-up cubes pass through phaseable Infinity
6. **Target Capture**: Capture targets that were previously blocked by Infinity

### Mastery Play
Advanced techniques for expert players:
- **Resonance Timing**: Use Infinity markers at optimal moments when multiple targets are blocked
- **Follow-Up Coordination**: Time marker placements so follow-up cubes arrive during phaseable window (2-4 moves)
- **Resource Management**: Save Infinity markers for high-value resonance opportunities
- **Pattern Reading**: Analyze wave composition to identify when Infinity blocking creates capture opportunities

## 3.9 Trigger Consistency Rules

Core principles governing marker trigger behavior:

- **Matrix Interactions** = Manual trigger (player detonates)
  - Matrix vs Matrix: 3x3 manual marker
  - Matrix vs Recursion: 2x2 Matrix cube marker (manual trigger)
  - Matrix vs Infinity: Matrix destroyed (Infinity is immutable)
  - Matrix vs Unit: 2x2 area capture

- **Recursion Interactions** = Manual trigger (player triggers swap markers)
  - Recursion vs Recursion: Empowered swap marker (instant capture + 2-charge swap with independent axis selection)
  - Recursion vs Matrix: 2x2 Matrix cube marker (manual trigger)
  - Recursion vs Infinity: Recursion destroyed (Infinity is immutable)
  - Recursion vs Unit: 1-charge swap marker (manual trigger, player selects direction)
  - Unit vs Recursion: 1-charge swap marker (applies damage, manual trigger, player selects direction)

- **Infinity Interactions** = Only Infinity affects Infinity
  - Infinity vs Unit: Wave join (destroys Unit, joins wave)
  - Infinity vs Matrix/Recursion: Captures wave cube, continues moving
  - Infinity vs Infinity: Immediate resonance (all Infinity cubes phaseable)

## 3.10 Shape Language

Visual and mechanical patterns created by cube interactions:

| Interaction Type | Resulting Shape | Trigger Type |
|------------------|-----------------|--------------|
| Unit vs Unit | Single tile | Instant |
| Unit vs Matrix / Matrix vs Unit | 2x2 square | Manual / Area |
| Matrix vs Matrix | 3x3 square | Manual |
| Matrix vs Recursion | 2x2 Matrix cube marker | Manual |
| Recursion vs Unit / Unit vs Recursion | + pattern swap (1 charge) | Manual |
| Recursion vs Matrix | 2x2 Matrix cube marker | Manual |
| Recursion vs Recursion | + pattern swap (2 charges: swap + capture) | Manual |

## 3.11 Resonance Visual Feedback

Player communication system for resonance mechanics:

1. **Phaseable State**: Infinity cubes become visually distinct when phaseable (material change, transparency)
2. **Duration Indicator**: Visual feedback shows remaining phaseable duration
3. **Pass-Through Confirmation**: Clear visual feedback when player cubes pass through phaseable Infinity
4. **Resonance Trigger Effect**: Visual/audio effect when resonance is triggered

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
- **Collision Matrix**: 16 cube type combinations with distinct behaviors
- **Infinity Immutability**: Infinity cubes are truly immutable - only Infinity affects Infinity
- **Resonance**: Immediate resonance trigger enables bypassing Infinity blockers
- **Trigger Split**: Trigger type split: Matrix = manual, Recursion = manual (swap markers)
- **Repositioning Tool**: Recursion creates swap markers for indirect board rearrangement
- **Advance Grid** (Future): Grid paths with direction changes for spatial-temporal puzzles


## 3.13 Open Items for Playtesting

Areas requiring player testing and iteration:

- **Marker Economy**: Exact number of non-Unit markers granted per stage
- **Allocation Method**: Whether marker grants are fixed ratio or player-allocated
- **Wave Density**: Wave density progression across stages
- **Recursion Multi-Hit Count**: Currently 2 hits required, may adjust based on balance
- **Swap Direction Default**: Currently horizontal (row swap), may adjust based on player preference
- **Rotation Schedule**: Rotation schedule timing (every N advances)

---
**Last Updated:** January 27, 2026  
**Implementation Status:** Core mechanics production-ready, Stages 0-4 validated, resonance systems implemented, Recursion redesign complete (swap mechanics), collision matrix updated  
**Major Systems:** Collision matrix, Infinity immutability with immediate resonance, penalty/reward system, marker economy, Recursion swap repositioning system  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
