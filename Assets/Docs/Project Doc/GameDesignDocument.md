# Game Design Document

## Purpose
This document provides a high-level overview of Infinity Cube's game design, covering core mechanics, systems, and design philosophy. It serves as the primary reference for understanding the game's structure and serves as a summary of the detailed technical documentation found in related documents.

## Executive Summary
* **Title:** Infinity Cube
* **Genre:** Grid-based Tactical Puzzle
* **Target Platform:** PC (Windows) via Steam
* **Target Audience:** Strategic puzzle enthusiasts and hardcore casual gamers
* **Development Stage:** Functional Prototype
* **Engine:** Unity 3D with component-based architecture

### High Concept
A grid-based tactical puzzle game where players strategically place markers to intercept advancing cube formations. Markers transform into player cubes that move backward to collide with forward-moving wave cubes, preventing them from escaping the grid. 

### Key Features
- **Four-Tier Marker System**: Unit, Matrix, Recursion, and Infinity markers with distinct applications
- **Advanced Cube Mechanics**: Four cube types with Infinity phaseable states and collision behaviors
- **Dynamic Flow Control**: Infinity cube phaseable mechanics creating strategic opportunities
- **Face Painting System**: Cube collisions paint faces that trigger effects when touching grid
- **Progressive Stage Design**: Structured learning curve teaching all systems

## Game Overview

### Core Gameplay Loop
**Place Markers** → **Cubes Collide** → **Capture or Escape** → **Next Wave**

#### Simple Flow:
1. **Place Markers**: Player places markers on grid (markers transform into backward-moving cubes)
2. **Cubes Move**: Wave cubes advance forward; player cubes move backward from markers
3. **Collisions**: Cubes collide—captures occur, face painting triggers, area effects activate
4. **Resolution**: Track results, apply rewards/penalties, advance to next wave

#### Key Mechanics:
- **Bidirectional Movement**: Forward-moving waves meet backward-moving player cubes
- **Face Painting**: Collisions with Infinity cubes paint faces that later place markers

### Setting
Minimalist abstract world with clean geometric shapes and cosmic backdrop. Visual design prioritizes functional clarity.

## Gameplay Mechanics

### Core Systems Implementation

#### **Grid System (GridManager)**
- Singleton-based spatial management with configurable dimensions
- Tile state tracking (Normal, Transformed, Corrupted, Marked)
- Corruption and enhancement mechanics affecting cube behavior

#### **Player System (PlayerActionManager)**
- Four-tier marker system (Unit, Matrix, Recursion, Infinity)
- Resource management with grant-based economy for non-Unit markers
- Visual feedback for marker states and placement
- Statistics tracking for performance metrics

#### **Cube System**
| Type | Properties | Behavior |
|------|------------|----------|
| **Unit** | Basic, capturable | Standard capture |
| **Matrix** | Generates cube markers | Creates area capture resources (2x2, 3x3) |
| **Infinity** | Uncapturable, phaseable | Face painting, resonance effects |
| **Recursion** | Multi-hit requirement | Requires matching marker, multi-charge capture |

#### **Face Painting System**
Collisions paint cube faces that place markers later:
- **Collision Trigger**: Matrix, Recursion, or Infinity player cubes collide with Wave Infinity cubes → collision face gets painted
- **Unit Exception**: Unit cubes don't paint Infinity cubes—they're destroyed on collision
- **Rotation**: Cubes rotate on a fixed schedule as waves advance
- **Marker Placement**: When painted face rotates down and touches grid, a marker appears at that tile
- **Telegraph System**: Visual indicators show where markers will appear (default: 3 moves ahead)
- **Face Types**: Matrix (2x2 manual marker), Recursion (auto-capture marker), Unit (single auto-capture), Infinity (resonance effect)

#### **Marker to Cube Conversion**
Core mechanic: When wave moves forward, placed markers transform into player cubes that move backward:
- **Transformation**: Markers become cubes matching their type (Unit marker → Unit cube, Matrix marker → Matrix cube, etc.)
- **Backward Movement**: Player cubes move backward from marker position toward incoming waves
- **Collision Opportunities**: Different cube types create different collision behaviors (capture, area effects, face painting)



## Level Design

### Stage Progression Philosophy
Teaching immediate tactics and cube interactions:

| Stage | Focus | Lesson |
|-------|-------|--------|
| 0-2 | Unit markers, Infinite Cube | Unit marker basics |
| 3-5 | Matrix markers | Area effects |
| 6-8 | Recursion markers | Multi-hit mechanics |
| 9-10 | Infinity Marker | All systems |
| 11-12 | Mastery | Complex strategies |

### Learning Curve Design
1. **Discovery Phase**: Players learn marker placement
2. **Understanding Phase**: Recognition of cube behaviors
3. **Planning Phase**: Strategic marker positioning
4. **Mastery Phase**: Complex collision chains
5. **Innovation Phase**: Creative use of mechanics


## Design Philosophy

### Core Pillars
1. **Simple Rules, Complex Strategies**: Basic mechanics create depth
2. **Multiple Solutions**: Different approaches work for each wave
3. **Clear Feedback**: Visual clarity on game states
4. **Easy to Learn**: Simple to understand, takes time to master
5. **Variety**: Four marker types create different gameplay options

### Key Systems
- **Bidirectional Movement**: Markers become backward-moving cubes that intercept forward-moving waves
- **Cube Collisions**: Cube collisions create different outcomes
- **Face Painting**: Collisions paint faces that place markers later
- **Marker to Cube Conversion**: Marker types convert to cubes
- **Marker Economy**: Grant-based resource system with four marker types


## Design Considerations

### Strengths
- **Clear Mechanics**: Each system has defined behavior
- **Strategic Depth**: Multiple valid strategies
- **Replayability**: Same wave can be approached differently
- **Visual Clarity**: Distinct cube and marker types

### Potential Concerns and Mitigations

#### Complexity Overload
**Concern**: Players may struggle with multiple systems
**Mitigation**: 
- Tutorial stages introduce one concept at a time
- Visual previews make mechanics clear
- Progressive complexity unlock

#### Frustration Potential
**Concern**: Players may feel overwhelmed by cube waves
**Mitigation**:
- Cube markers provide emergency capture options
- Balance wave difficulty appropriately

#### Balancing Challenge
**Concern**: Difficulty to balance across skill levels
**Mitigation**:
- Configurable wave difficulty
- Variable marker limits per stage

---
**Last Updated:** December 14, 2025  
**Implementation Status:** Core systems complete, face painting implemented, collision mechanics in refinement

## Consistency Notes
- **Marker to Cube**: Core mechanic where markers transform into backward-moving cubes when wave advances
- **Phaseable States**: Infinity cubes can become phaseable (passable) through resonance, not "pauseable"
- **Face Painting**: Only Matrix, Recursion, and Infinity player cubes can paint Wave Infinity cubes (Unit cubes are destroyed instead)  
**Related Documents:**
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Level Design](4_LevelDesign.md)
- [Game Overview](2_GameOverview.md)
