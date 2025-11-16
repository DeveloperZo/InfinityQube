# Game Overview

> This document details the Game Overview section of Infinity Cube's Game Design Document. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Defines the core gameplay loop, setting, and primary gameplay elements through the lens of "A modern and cosmic take on the classic intelligence qube formula"

## 2.1 Concept

### The Cosmic Dance
Players navigate various cubes traversing a grid. Players must learn how the cubes and various markers interact. 

### The Core 
- **Cubes**: Cube types include Unit, Prime, Infinity, and Recursion cubes, each with unique behaviors and interactions.
- **Face Status System**: Cube faces can be painted with two status types: Corrupted (acts like Infinity cubes) or Enhanced (creates detonations)
- **Marker Modes**: Three marker modes (Light, Prime, Heavy) plus special Cube markers from captured Prime cubes

### The Player's Journey
The player starts off with simple puzzles and limited amount of cube types. As the player progresses, they will encounter more complex cube behaviors, paint mechanics, and other challenges.

## 2.2 Core Gameplay Loop 

### Overview
The player progresses through stages. Each stages consists of multiple waves. Each wave consists of multiple cubes moving across a grid with the goal of only allowing infinity qubes to fall over the edge

### 1. **Wave Initiation **
- Player input to start the initial wave
- Cubes spawn in unique formations that determine where and how you can place markers as they march forward in unison

### 2. **Strategizing with constraints**
- Each level will impose limits on marker count, type, and cooldowns for markers
- Players should strategize in real time on how to best handle the wave with the constraints given

### 3. **Marker Placement**
- **Light Markers** (Key: 1): Standard single-tile captures for Unit cubes
- **Prime Markers** (Key: 2): Area captures creating 2x2 zones
- **Heavy Markers** (Key: 3): Enhanced markers designed for Recursion cube engagement

### 4. **Face Status Transformation** (CURRENTLY IMPLEMENTED)
- **Face Painting System**: Cube faces can have status effects (managed by FacePaintingManager)
    - **Corrupted Status**: When a corrupted face touches the grid, the cube acts like an Infinity cube (cannot be captured)
        - Visual: Black paint effect on face
        - Duration: Can be temporary or permanent based on configuration
        - Tiles can be configured to paint cubes on landing or exit
    
    - **Enhanced Status**: When an enhanced face touches the grid, it creates detonation effects when captured
        - Visual: Blue paint effect on face  
        - Creates additional capture zones or bonus effects
        - Works with the detonation system for chain reactions
    
    **Implementation Note**: The system uses FaceStatus enum (None/Corrupted/Enhanced) instead of separate paint types. Face painting can be applied via patterns, batch operations, or individual tile configuration.


### 5. **Wave Resolution** (CURRENT IMPLEMENTATION)
- Wave completes when all non-Infinity cubes are processed (captured or escaped)
- Each wave tracks:
    - Cubes captured by type (Unit/Prime/Recursion)
    - Cubes escaped (triggers failure if exceeds maxAllowedEscapes)
    - Markers placed and detonations used
- Wave failure conditions:
    - Too many cube escapes (configurable per wave)
    - Custom success criteria not met (hasOwnSuccessCriteria flag)
- Wave completion triggers events for StageManager integration


## 2.3 Setting - The Cosmic Stage

### A Universe of Rhythm and Chaos

#### **The Grid**
- A X by Y grid that allows cubes to move in unison (just like intelligence qube)

#### **Visual Language of Duality** (AS IMPLEMENTED)

- **Cube Type Colors**: 
  - Gray = Unit (basic cube type)
  - Blue = Prime (area coverage type)
  - Black = Infinity (corruption type, cannot be captured)
  - [Material TBD] = Recursion (enhanced durability type)


#### **Atmosphere**
- **Background**: Deep space and cosmic phenomena
- **Particle Effects**: Cosmic dust dancing to the rhythm


## 2.4 Gameplay Elements - Instruments of Order and Chaos

### The Rhythm Makers - Cube Types

| Cube Type | Rhythm Role | Chaos Interaction | Player Response |
|-----------|-------------|-------------------|-----------------|
| **Unit** | Basic beat, steady tempo | Can be corrupted or enhanced | Standard capture timing |
| **Prime** | Valuable notes, create crescendos | Generate detonation resources | Aggressive pursuit |
| **Infinity** | Dangerous discord, break rhythm | Cannot be transformed | Avoidance choreography |
| **Recursion** | Strong beats, require multiple hits | Resist transformation | Extended engagement |

### The Chaos Bringers - Face Status System (IMPLEMENTED)

| Face Status | Implementation | Effect When Active | Visual Indicator |
|-------------|----------------|-------------------|------------------|
| **Corrupted** | FaceStatus.Corrupted | Acts like Infinity cube - cannot be captured | Black paint on cube face |
| **Enhanced** | FaceStatus.Enhanced | Creates detonation effects when captured | Blue paint on cube face |
| **None** | FaceStatus.None | Normal cube behavior | No visual effect |

### The Transformation Mechanics
- **Face Painting**: Cubes have four faces that can hold paint
- **Rotation Activation**: Effects trigger when painted face touches grid
- **Duration Dynamics**: Temporary vs permanent rhythm changes
- **Compound Chaos**: Multiple paint types create complex behaviors

### The Conductor's Tools - Marker System (IMPLEMENTED)

#### **Marker Mode System**
The game uses a unified MarkerMode enum with numeric key switching:
- Press 1: Switch to Light marker mode
- Press 2: Switch to Prime marker mode  
- Press 3: Switch to Heavy marker mode

#### **Light Markers (Mode 1)**
- Single-tile precision targeting
- Basic capture for Unit cubes
- Configurable charges and count limits per wave

#### **Prime Markers (Mode 2)**
- 2x2 area coverage zones
- Capture multiple cubes simultaneously
- Configurable charges and count limits per wave

#### **Heavy Markers (Mode 3)**
- Enhanced single-tile markers
- Specifically designed for Recursion cube capture
- Requires multiple hits to capture durable cubes

#### **Cube Markers (Special)**
- Generated from successful Prime cube captures
- Direct detonation capabilities
- Not part of the mode switching system
- Exploring cube markers mechanic for recursion cube (may drop if no suitable mechanic found)

## 2.5 The Player's Evolution



## 2.6 Design Principles

### **Rhythmic Clarity**
The wave-based progression with configurable timing intervals creates a clear rhythm of gameplay. Players can control pacing with speed-up mechanics.

### **Cosmic Wanderlust**
The cosmic theme permeates the visual design with space backgrounds and particle effects representing cosmic dust.

### **Yugen (幽玄)**


### **Accessible Depth**



## 2.7 Implementation Status & Clarifying Questions

### ✅ Currently Implemented
- **Cube Types**: All four types (Unit, Prime, Infinity, Recursion) with basic behaviors
- **Face Status System**: Corrupted and Enhanced face painting mechanics via FacePaintingManager
- **Marker Modes**: Three-mode system (Light/Prime/Heavy) with numeric key switching
- **Wave System**: Complete wave management with configurable parameters per wave
- **Grid System**: Functional grid with tile states and marker placement
- **Audio Events**: Event-driven audio system for all major game actions
- **Tutorial System**: Message display with pause/auto-hide functionality

### ⚠️ Partially Implemented or Unclear
- **Recursion Cubes**: Core functionality exists but visual representation needs definition
- **Detonation System**: Referenced in code but full chain reaction mechanics unclear
- **Cube Markers**: Mentioned as generated from Prime captures but activation method undefined
- **Face Rotation**: System tracks faces but rotation mechanics during movement need clarification

### ❓ Clarifying Questions for Design Team

#### Gameplay Mechanics
1. **Recursion Cube Visuals**: What should the visual representation be for Recursion cubes? Current code supports them but material/color undefined.

2. **Heavy Marker Mechanics**: How many hits should be required to capture a Recursion cube with Heavy markers? Is there a visual feedback system for partial damage?

3. **Cube Markers**: How are Cube markers activated after being generated from Prime cube captures? Is there a separate key binding or automatic trigger?

4. **Face Rotation Rules**: When cubes move forward, do they rotate (tumble)? If so, which face becomes the new bottom face?

5. **Detonation Chains**: How do detonations propagate? What determines the size and pattern of chain reactions?

#### Visual Design
1. **Grid Dimensions**: What are the intended default grid dimensions (X by Y)? Current implementation is configurable but needs baseline.

2. **Recursion Cube Appearance**: Should they have a unique color/texture or use special effects to indicate durability?

3. **Paint Duration Visuals**: How should temporary vs permanent face status be visually distinguished?

#### Balance & Progression
1. **Wave Failure Recovery**: When a wave fails due to escapes, does the player retry the same wave or continue with penalties?

2. **Resource Regeneration**: Are marker charges regenerated between waves, over time, or through specific actions?

3. **Difficulty Scaling**: How should wave complexity increase? More cube types, faster movement, or more complex patterns?

### 📝 Notes for Future Development
- The paint system has been refactored from separate paint types to a unified FaceStatus system
- Wave completion is event-driven, allowing flexible integration with stage progression
- The marker system uses a mode-switching approach rather than separate key bindings per marker type
- Tutorial messages can be configured per wave with specific timing and pause requirements

---
**Last Updated:** November 15, 2025  
**Core Theme:** Intelligent Mastery of Cube Rhythms with Cosmic Wanderlust  
**Design Philosophy:** Where Mathematical Precision Dances with Cosmic Chaos