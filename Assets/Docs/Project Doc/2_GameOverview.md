# Game Overview

> This document details the Game Overview section of Infinity Cube's Game Design Document. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Defines the core gameplay loop, setting, and primary gameplay elements of InfinityQube - a strategic cube-capture game built on marker to cube transformation mechanics.

## 2.1 Concept

### Core Mechanics
Players strategically place markers on a grid to intercept advancing cube waves. The game's signature mechanic is the **Marker to Cube System**, where placed markers transform into cubes that move backward toward incoming waves, creating dynamic collision-based captures.

### Primary Systems
- **Cubes**: Four distinct cube types (Unit, Matrix, Infinity, and Recursion), each with unique capture requirements and strategic value
- **Face Status System**: Cube faces can be modified with Corrupted (prevents capture) or Enhanced (creates bonus effects) status effects
- **Marker System**: Four marker types (Unit, Matrix, Recursion, Infinity) provide different capture capabilities
- **Marker to Cube System**: The game's defining mechanic - markers transform into backward-moving cubes that collide with forward-moving waves

### Progression Structure
Players advance through increasingly complex stages featuring diverse cube formations, status effect patterns, and resource constraints. Success requires mastering the timing and positioning of marker-to-cube transformations.

## 2.2 Core Gameplay Loop

### Overview
Players progress through stages composed of multiple waves. Each wave presents a formation of cubes moving across a grid. The objective is to capture all non-Infinity cubes while allowing Infinity cubes to pass through.

### 1. **Wave Initialization**
- Player triggers wave start through input
- Cube formations spawn at grid edge and begin forward movement
- Wave-specific constraints (marker limits, cooldowns, cube types) become active

### 2. **Strategic Analysis**
- Assess cube formation patterns and movement speed
- Identify high-priority targets (Matrix cubes for resources, dangerous Recursion cubes)
- Plan marker placement considering backward movement trajectories
- Account for face status effects that modify cube behavior
- Consider line divider position: threats above the line are visible but cannot be acted upon until they cross below

### 3. **Marker Placement & Transformation**
- **Unit Markers** (Mode 1, F Key): Single-tile markers that transform into backward-moving Unit cubes
- **Matrix Markers** (Mode 2, F Key): Area-effect markers creating 2x2 capture zones when transformed
- **Recursion Markers** (Mode 3, F Key): Enhanced markers that become Recursion cubes for Recursion capture
- **Infinity Markers** (Mode 4, F Key): Spawn pause-inducing Infinity cubes
- **Cube Markers** (R Key): Generated resources from Matrix cube captures, triggered with R key
- **Transformation Process**: Upon placement, markers immediately convert to cubes and begin backward movement

### 4. **Bidirectional Movement Phase**
- **Wave Cubes**: Continue forward movement at configured speed
- **Player Cubes**: Move backward from marker placement positions
- **Collision Calculation**: System continuously tracks approaching collision points
- **Strategic Timing**: Distance and timing determine where collisions occur on the grid

### 5. **Collision Resolution**
- **Capture Collisions**: When player cube meets compatible wave cube, capture occurs
- **Type Matching**: Player cubes match their marker type (Unit cubes from Unit markers, Recursion cubes from Recursion markers)
- **Area Effects**: Matrix cube collisions affect 3x3 zones
- **Same-Type Interactions**: Matching cube types (Matrix-Matrix, Recursion-Recursion) generate marker resources
- **Conversion Tactics**: Unit cubes can revert to markers mid-movement for tactical advantages

### 6. **Face Status Processing** (CURRENTLY IMPLEMENTED)
- **Face Status System**: Each cube face can hold status effects affecting behavior
    - **Corrupted Status**: Makes cube uncapturable while active
        - Visual: Black effect on affected face
        - Activates when corrupted face contacts grid
        - Duration: Configurable as temporary or permanent
    
    - **Enhanced Status**: Triggers bonus effects upon capture
        - Visual: Blue effect on affected face
        - Creates detonation zones or chain reactions
        - Integrates with scoring multipliers
    
    **Technical Note**: Implemented via FaceStatus enum with efficient face tracking per cube.

### 7. **Wave Completion**
- Wave ends when all mobile cubes are resolved (captured or escaped)
- System tracks:
    - Capture count by cube type
    - Escape count (failure if exceeds threshold)
    - Resource usage and generation
    - Performance metrics for scoring
- Failure triggers wave retry or stage penalty based on configuration
- Success advances to next wave with potential resource rewards

## 2.3 Setting - The Grid Arena

### Spatial Framework

#### **The Grid**
- Configurable X by Y tile grid serving as the primary play space
- Tiles support marker placement and cube movement
- Edge boundaries define escape zones and spawn points
- **Line Divider**: Dynamic line that restricts marker placement to lower rows, creating strategic tension as threats approach from above

#### **Visual Design System** (AS IMPLEMENTED)

- **Cube Type Identification**: 
  - Gray = Unit (standard capture target)
  - Blue = Matrix (resource-generating type)
  - Black = Infinity (uncapturable obstacle)
  - [Pending] = Recursion (multi-hit requirement)

#### **Environmental Design**
- **Background**: Deep space environments with cosmic elements
- **Effects**: Particle systems for captures and collisions
- **Grid Highlighting**: Dynamic tile states for strategic feedback

## 2.4 Gameplay Elements - Core Components

### Cube Types - Strategic Targets

| Cube Type | Mechanical Role | Capture Method | Strategic Value |
|-----------|-----------------|----------------|-----------------|
| **Unit** | Standard target | Single collision with Unit cube (from Unit marker) | Basic scoring, conversion potential |
| **Matrix** | Resource generator | Requires Matrix marker collision | Generates Cube markers, area clearing |
| **Infinity** | Obstacle | Cannot be captured | Must be avoided or bypassed |
| **Recursion** | Durable target | Multiple Recursion cube collisions (from Recursion markers) | High score value, challenge element |

### Face Status System - Behavioral Modifiers (IMPLEMENTED)

| Face Status | System Implementation | Mechanical Effect | Visual Feedback |
|-------------|----------------------|-------------------|-----------------|
| **Corrupted** | FaceStatus.Corrupted | Prevents capture while active | Black face effect |
| **Enhanced** | FaceStatus.Enhanced | Triggers bonus effects on capture | Blue face effect |
| **None** | FaceStatus.None | Standard cube behavior | No modification |

### Status Mechanics
- **Face Application**: Each cube tracks status on four faces
- **Activation Trigger**: Status activates when painted face contacts grid
- **Duration System**: Temporary effects decay over time, permanent effects persist
- **Compound Effects**: Multiple status types create complex behavioral patterns

### Marker System - Player Tools (IMPLEMENTED)

#### **Four-Tier Marker Framework**
Unified input system for rapid marker deployment:
- Mode Keys 1-4: Switch between marker modes (1=Unit, 2=Matrix, 3=Recursion, 4=Infinity)
- F Key: Place marker of current mode
- R Key: Trigger Cube markers (generated from collisions)

#### **Unit Markers (Mode 1, F Key)**
- Single-tile precision placement
- Transform into backward-moving Unit cubes
- Optimal for Unit cube interception
- Resource-efficient standard tool with move-based regeneration

#### **Matrix Markers (Mode 2, F Key)**
- 2x2 area coverage capability
- Transform into Matrix cubes
- Capture multiple targets simultaneously
- Generate Cube markers from successful Matrix captures

#### **Recursion Markers (Mode 3, F Key)**
- Enhanced single-tile markers
- Transform into Recursion cubes
- Required for Recursion cube capture
- Multi-hit capability for durable targets

#### **Infinity Markers (Mode 4, F Key)**
- Spawn pause-inducing Infinity cubes
- Affects Infinity cubes within proximity
- Strategic resource for flow control
- Limited availability (grant-based economy)

#### **Cube Markers (R Key - Generated Resource)**
- Generated from Matrix cube collisions (not player-placed)
- Instant detonation capability when triggered
- Strategic resource for emergency situations
- Limited availability based on collision success

### Marker to Cube System - Core Innovation

#### **Fundamental Concept**
Markers transform into player cubes that move backward:
- Forward movement: Wave cubes advancing toward grid edge
- Backward movement: Player cubes moving from marker placement points
- Intersection: Collision points where captures occur

#### **Strategic Mechanics**
- **Trajectory Planning**: Backward movement distance equals forward interception range
- **Timing Windows**: Early placement + far position = late-stage collision
- **Pattern Matching**: Success requires positioning markers to intercept wave formations
- **Dynamic Conversion**: Transform Unit cubes to markers for tactical repositioning
- **Resource Loops**: Same-type collisions generate new marker resources

## 2.5 Player Skill Development

### Progression of Mastery
1. **Foundation**: Understanding basic collision mechanics and marker transformation
2. **Advancement**: Managing face status effects and resource constraints
3. **Expertise**: Optimizing collision points and conversion strategies
4. **Mastery**: Perfect wave clearance through precise symmetrical positioning

## 2.6 Design Principles

### **Mechanical Clarity**
Every system interaction has clear, predictable outcomes. Collision physics follow consistent rules.

### **Strategic Depth**
Simple placement mechanics create complex tactical decisions through movement vectors and timing.

### **Visual Communication**
All gameplay elements use distinct visual language for instant recognition during fast-paced waves.

### **Accessible Complexity**
Core mechanics are immediately understandable while mastery requires deep strategic thinking.

## 2.7 Implementation Status & Technical Details

### ✅ Currently Implemented
- **Cube Types**: All four types with distinct behaviors and capture requirements
- **Face Status System**: Complete implementation via FacePaintingManager
- **Marker System**: Four-tier system with dedicated key bindings
- **Wave Management**: Configurable wave parameters and progression tracking
- **Grid Infrastructure**: Full tile system with state management
- **Audio Integration**: Event-driven sound system for all actions
- **Tutorial Framework**: Highlight sequence system with guided messages, visual highlights, and interactive validation

### 🚧 To Be Implemented
- **Marker to Cube System**: Marker-to-cube transformation mechanics ✅ Implemented
- **Backward Movement**: Reverse cube trajectories from marker positions
- **Collision Detection**: Bidirectional collision resolution system
- **Conversion System**: Unit cube to marker transformation mid-flight
- **Resource Generation**: Same-type collision marker dropping

### ⚠️ Partially Implemented
- **Recursion Cubes**: Logic complete, visual representation pending
- **Detonation Chains**: System referenced but propagation rules undefined
- **Cube Marker Generation**: Creation from Matrix captures needs activation logic
- **Face Rotation**: Tracking implemented but movement rotation needs specification

### ❓ Technical Specifications Needed

#### Collision System
1. **Collision Detection Range**: Pixel-perfect or tile-based collision boundaries?
2. **Simultaneous Collisions**: Resolution order for multiple simultaneous impacts?
3. **Collision Feedback**: Visual/audio requirements for different collision types?

#### Movement Mechanics  
1. **Movement Speed**: Uniform speed or type-specific velocities?
2. **Rotation During Movement**: Do cubes rotate/tumble while moving?
3. **Path Deviation**: Strictly linear or curve support for advanced waves?

#### Visual Requirements
1. **Grid Dimensions**: Default grid size for standard gameplay?
2. **Recursion Appearance**: Visual differentiation from other cube types?
3. **Status Duration Indicators**: How to show temporary vs permanent effects?

#### Balance Parameters
1. **Collision Timing Windows**: Frame-perfect or generous collision detection?
2. **Resource Regeneration**: Marker recharge rates between waves?
3. **Difficulty Scaling**: Progressive complexity through speed, patterns, or constraints?

### 📝 Development Notes
- FaceStatus system provides efficient status tracking per cube face
- Wave system uses event-driven architecture for flexible stage integration
- Marker mode switching enables rapid tactical decisions
- Tutorial system uses highlight sequences for guided instruction with messages, visual highlights, and interactive validation

---
**Last Updated:** December 14, 2025  
**Core System:** Marker to Cube Transformation Mechanics  
**Design Focus:** Strategic depth through bidirectional movement and collision-based gameplay