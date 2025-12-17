# Game Design Document

## Executive Summary
* **Title:** Infinity Cube
* **Genre:** Grid-based Tactical Puzzle
* **Target Platform:** PC (Windows) via Steam
* **Target Audience:** Strategic puzzle enthusiasts and hardcore casual gamers
* **Development Stage:** Functional Prototype
* **Engine:** Unity 3D with component-based architecture

### High Concept
A grid-based tactical puzzle game where players strategically place markers to intercept advancing cube formations. Players spawn player cubes that move backward through the grid to capture wave cubes advancing toward them. Combined with Infinity cube pause mechanics, collision systems, and face painting mechanics, players must balance immediate tactical decisions with strategic planning.

### Key Features
- **Five-Tier Marker System**: Unit, Recursion, Matrix, Cube, and Infinity markers with distinct applications
- **Advanced Cube Mechanics**: Four cube types with Infinity pause states and collision behaviors
- **Dynamic Flow Control**: Infinity cube pause mechanics creating strategic bottlenecks
- **Face Painting System**: Cube collisions paint faces that trigger effects when touching grid
- **Progressive Stage Design**: Structured learning curve teaching all systems
- **Line Divider System**: Dynamic difficulty mechanism restricting marker placement

## Game Overview

### Core Gameplay Loop
**Wave Start** → **Marker Placement** → **Cube Spawning** → **Collision Resolution** → **Wave Completion**

#### Detailed Flow:
1. **Wave Initiation**: Player starts wave from configuration
2. **Tactical Phase**:
   - Wave cubes advance toward player
   - Strategic marker placement
   - Player cubes spawn from markers
   - Immediate survival focus
3. **Collision Phase**:
   - Player cubes move backward into wave cubes
   - Collision behaviors resolve
   - Face painting effects trigger
4. **Resolution Phase**:
   - Track captured/escaped cubes
   - Apply penalties/rewards
   - Advance to next wave

### Setting
**Tactical Arena** - A minimalist abstract world with clean geometric shapes. The cosmic backdrop emphasizes themes of infinity and tactical precision. Visual design prioritizes functional clarity while conveying the weight of tactical decisions.

## Gameplay Mechanics

### Core Systems Implementation

#### **Grid System (GridManager)**
- Singleton-based spatial management with configurable dimensions
- Tile state tracking
- Line divider system for dynamic difficulty
- Corruption and enhancement mechanics

#### **Player System (PlayerActionManager)**
- Five-tier marker system
- Resource management
- Visual feedback for marker states
- Statistics tracking

#### **Cube System**
| Type | Properties | Behavior |
|------|------------|----------|
| **Unit** | Basic, capturable | Standard capture |
| **Matrix** | Generates cube markers | Creates area capture resources |
| **Infinity** | Uncapturable, pauseable | Face painting, resonance |
| **Recursion** | Multi-hit requirement | Requires matching marker |

#### **Wave Management (WaveManager)**
Wave configuration system:
```
WaveData Structure:
- Index: Wave number
- GridHeight/Width: Grid dimensions
- CubesData: Cube spawn positions and types
- Marker Settings: Available markers per wave
- Timing: Movement intervals
- Success Criteria: Win/lose conditions
```

### Input System
```
Core Controls:
Movement: WASD/Arrows → Grid navigation
Mode Selection: 1-4 → Switch marker mode
  1 = Unit Marker mode
  2 = Matrix Marker mode
  3 = Recursion Marker mode
  4 = Infinity Marker mode
Marker Placement: F → Place marker of current mode
Cube Marker Trigger: R → Trigger cube marker area effect
```

## Level Design

### Stage Progression Philosophy
Teaching immediate tactics and cube interactions:

| Stage | Focus | Lesson |
|-------|-------|--------|
| 0-2 | Unit markers, Infinite Cube | Unit marker basics |
| 3-5 | Matrix cube/markers | Area effects |
| 6-8 | Recursion cube/marker | Multi-hit mechanics |
| 9-10 | Infinity Marker | All systems |
| 11-12 | Mastery | Complex strategies |

### Learning Curve Design
1. **Discovery Phase**: Players learn marker placement
2. **Understanding Phase**: Recognition of cube behaviors
3. **Planning Phase**: Strategic marker positioning
4. **Mastery Phase**: Complex collision chains
5. **Innovation Phase**: Creative use of mechanics

## Strategic Implications

### Emergent Strategies

#### "Area Control"
Use Matrix markers to create large capture areas for incoming wave formations.

#### "The Setup"
Place markers in specific patterns to create advantageous cube collision chains.

#### "Infinity Timing"
Use Infinity cubes strategically to pause and redirect cube flows.

#### "Resource Management"
Balance marker usage across waves to maintain strategic options.

### Risk/Reward Dynamics
- **High Risk**: Aggressive marker use depletes resources
- **High Reward**: Strategic Matrix placement creates resource generation
- **Balanced Play**: Measured marker use with careful position selection
- **Defensive Play**: Minimal marker use, relying on Cube markers

## Technical Architecture

### System Dependencies
```
WaveManager
├── Cube Spawning
│   ├── Configuration Handler
│   ├── Position Calculator
│   └── Cube Instantiation
├── Wave Flow
│   ├── Movement System
│   ├── Completion Tracking
│   └── Statistics
└── Player Integration
    ├── Marker System
    └── Collision Resolution
```

## Design Philosophy

### Core Pillars
1. **Tactical Depth**: Simple rules create complex strategies
2. **Player Agency**: Multiple valid approaches to each wave
3. **Clear Feedback**: Visual clarity on all game states
4. **Accessible Depth**: Easy to understand, lifetime to master
5. **Strategic Variety**: Multiple marker types create diverse gameplay

### Unique Selling Points
- **Grid-based tactical puzzle with cube collision mechanics**
- **Five distinct marker types with unique behaviors**
- **Face painting system creating strategic timing opportunities**
- **Dynamic difficulty through line divider system**
- **Infinity cubes creating flow control mechanics**

## Implementation Priorities

### Phase 1: Core Systems ✅
1. Grid and wave management
2. Basic marker placement
3. Cube spawning and movement
4. Collision detection

### Phase 2: Advanced Features
1. Face painting system
2. Line divider mechanics
3. Resonance system
4. Marker economy

### Phase 3: Polish and Balance
1. Visual feedback refinement
2. Tutorial system
3. Difficulty curve adjustment
4. Stage content creation

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
- Line divider creates manageable challenge area
- Cube markers provide emergency capture options
- Balance wave difficulty appropriately

#### Balancing Challenge
**Concern**: Difficulty to balance across skill levels
**Mitigation**:
- Configurable wave difficulty
- Variable marker limits per stage
- Adjustable line divider position

---
**Last Updated:** December 8, 2025  
**Implementation Status:** Core systems complete, collision mechanics in refinement
**Related Documents:**
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Level Design](4_LevelDesign.md)
- [Technical Architecture](TechnicalArchitecture.md)
