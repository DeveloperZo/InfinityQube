# Game Design Document

## Executive Summary
* **Title:** Infinity Cube
* **Genre:** Grid-based Tactical Puzzle with Temporal Strategy
* **Target Platform:** PC (Windows) via Steam
* **Target Audience:** Strategic puzzle enthusiasts and hardcore casual gamers
* **Development Stage:** Functional Prototype with Paired Wave System
* **Engine:** Unity 3D with component-based architecture

### High Concept
A grid-based tactical puzzle game where players strategically place markers to intercept advancing cube formations, with the revolutionary twist that marker placements become cube spawn positions in paired waves. This creates a temporal strategy layer where every action has both immediate tactical value and future strategic consequences. Combined with Infinity cube pause mechanics and collision systems, players must balance present survival with future preparation.

### Key Features
- **Paired Wave System**: Marker placements in one wave become cube spawns in the next
- **Temporal Strategy**: Every decision affects both current and future gameplay
- **Five-Tier Marker System**: Light, Heavy, Prime, Cube, and Infinity markers with distinct applications
- **Advanced Cube Mechanics**: Four cube types with Infinity pause states and collision behaviors
- **Dynamic Flow Control**: Infinity cube pause mechanics creating strategic bottlenecks
- **Strategic Continuity**: Actions cascade across wave pairs creating emergent complexity
- **Progressive Stage Design**: 12-stage structured learning curve teaching both systems
- **Compressed Configuration**: Efficient wave data storage with inheritance rules

## Game Overview

### Core Gameplay Loop (Enhanced)
**Wave Pair Selection** → **Wave A Execution** → **Marker Recording** → **Wave B Preparation** → **Inheritance Resolution** → **Pair Completion**

#### Detailed Flow:
1. **Wave Pair Initiation**: Player starts paired wave set (Wave A + Wave B)
2. **Wave A - Tactical Phase**:
   - Standard cube defense
   - Strategic marker placement
   - Position recording for Wave B
   - Immediate survival focus
3. **Transition Phase**:
   - Preview inherited cube positions
   - Prepare for Wave B challenges
   - Resource regeneration
4. **Wave B - Consequence Phase**:
   - Previous markers spawn as cubes
   - New wave configuration overlays
   - Manage created complexity
   - Adapt to self-created challenges
5. **Pair Resolution**:
   - Combined performance scoring
   - Strategic assessment
   - Progression to next pair

### Revolutionary Mechanics
**"Your Defense Becomes Your Offense"** - The paired wave system transforms defensive marker placement into future offensive challenges, creating a unique risk/reward dynamic where optimal immediate solutions may create future problems.

### Setting
**Temporal Loop Arena** - A minimalist abstract world where time loops create echoes of past actions. Clean geometric shapes represent the eternal cycle of action and consequence. The cosmic backdrop emphasizes themes of infinity, recursion, and temporal causality. Visual design prioritizes functional clarity while conveying the weight of decisions that ripple through time.

## Gameplay Mechanics

### Paired Wave System (Core Innovation)

#### Wave Pairing Structure
```
Wave Pair Components:
├── Wave A (Primary)
│   ├── Base cube configuration
│   ├── Standard objectives
│   ├── Marker placement recording
│   └── Position inheritance encoding
└── Wave B (Consequent)
    ├── Inherited cube spawns
    ├── Additional cube configuration
    ├── Merged spawn resolution
    └── Cascading objectives
```

#### Marker-to-Cube Conversion Table
| Marker Type | Spawns in Next Wave | Strategic Implication |
|-------------|-------------------|----------------------|
| Light | 1x Unit Cube | Low risk, manageable consequence |
| Heavy | 1x Recursion Cube | High durability future threat |
| Prime | 1x Prime Cube (center) or 3x3 Units | Resource opportunity or swarm |
| Infinity | 1x Infinity Cube | Extreme future danger for current control |
| Cube | No inheritance | Pure immediate action |

#### Strategic Depth Layers
1. **Immediate Tactical Layer**: Survive current wave
2. **Future Planning Layer**: Minimize next wave difficulty
3. **Resource Optimization Layer**: Balance marker usage across pairs
4. **Pattern Recognition Layer**: Learn optimal placement patterns
5. **Sacrifice Strategy Layer**: Accept current difficulty for future advantage

### Core Systems Implementation

#### **Grid System (GridManager)**
- Singleton-based spatial management with configurable dimensions
- Marker position recording for wave inheritance
- Ghost preview system for future spawns
- Tile state tracking including inheritance markers
- Corruption and enhancement mechanics

#### **Player System (PlayerActionManager)**
Enhanced with temporal awareness:
- Five-tier marker system with inheritance tracking
- Visual feedback showing future spawn positions
- Resource management across wave pairs
- Statistics tracking both immediate and inherited performance
- Strategic decision indicators

#### **Cube System**
| Type | Properties | Inheritance Behavior |
|------|------------|---------------------|
| **Unit** | Basic, capturable | Standard spawn from Unit Markers |
| **Prime** | Generates cube markers | Valuable spawn from Prime markers |
| **Infinity** | Uncapturable, pauseable | Dangerous spawn from Infinity markers |
| **Recursion** | Multi-hit requirement | Challenging spawn from Recursion Markers |

#### **Wave Management (WaveManager)**
Revolutionary paired wave configuration:
```
WaveData Structure:
- pairID: Links waves together
- primaryWave: Configuration for Wave A
- consequentWave: Configuration for Wave B
- inheritanceRules: Marker-to-cube conversion
- overlapStrategy: How inherited spawns merge
- compressionFormat: Optimized data storage
```

### Compressed Configuration System
Efficient storage of paired wave data:
```json
{
  "pairID": "P1",
  "waveA": {
    "spawns": [[2,10,"Unit"], [4,12,"Prime"]],
    "resources": {"light": 5, "heavy": 2}
  },
  "waveB": {
    "inheritedPositions": "auto",
    "additionalSpawns": [[3,15,"Infinity"]],
    "mergeRule": "overlay"
  }
}
```

### Input System
```
Core Controls:
Movement: WASD/Arrows → Grid navigation
Unit Marker: F → Place (spawns Unit in next wave)
Recursion Marker: V → Place (spawns Recursion in next wave)
Prime Marker: G → Place (spawns Prime in next wave)
Infinity Marker: [TBD] → Place (spawns Infinity in next wave)
Cube Marker: Q → Direct destruction (no inheritance)
[Trigger keys remain the same]
Preview Toggle: P → Show/hide inheritance ghosts
```

## Level Design

### Stage Progression Philosophy (Revised)
Teaching both immediate tactics and temporal strategy:

| Stage | Focus | Wave Pairing Lesson |
|-------|-------|-------------------|
| 1-2 | Basic markers | Introduction to inheritance |
| 3-4 | Prime/Area markers | Area inheritance patterns |
| 5-6 | Infinity cubes | Dangerous inheritance |
| 7-8 | Heavy/Recursion | Durability inheritance |
| 9-10 | Full integration | Complex inheritance chains |
| 11-12 | Mastery | Multi-pair planning |

### Learning Curve Design
1. **Discovery Phase**: Players learn marker placement affects next wave
2. **Understanding Phase**: Recognition of conversion patterns
3. **Planning Phase**: Deliberate future wave preparation
4. **Mastery Phase**: Multi-wave strategic chains
5. **Innovation Phase**: Creative use of inheritance for advantage

## Strategic Implications

### Emergent Strategies

#### "The Sacrifice Gambit"
Deliberately struggle in Wave A by conserving markers, making Wave B easier due to fewer inherited spawns.

#### "The Setup"
Place markers in specific patterns during Wave A to create advantageous cube formations in Wave B that generate resources.

#### "The Cascade"
Chain marker placements across multiple wave pairs to create long-term strategic advantages.

#### "The Clean Slate"
Use Cube markers exclusively to avoid inheritance, accepting resource limitations for predictability.

### Risk/Reward Dynamics
- **High Risk**: Aggressive marker use solves immediate problems but creates future chaos
- **High Reward**: Strategic Prime placement creates resource generation opportunities
- **Balanced Play**: Measured marker use with careful position selection
- **Defensive Play**: Minimal marker use, relying on movement and Cube markers

## Technical Architecture

### System Dependencies (Updated)
```
WaveManager (Enhanced)
├── Pair Controller
│   ├── Wave A Handler
│   ├── Inheritance Recorder
│   ├── Wave B Generator
│   └── Merge Resolver
├── Compression Engine
│   ├── Data Optimizer
│   ├── Pattern Library
│   └── Replay System
└── Preview System
    ├── Ghost Renderer
    ├── Timeline Visualizer
    └── Impact Calculator
```

## Design Philosophy

### Core Pillars (Enhanced)
1. **Temporal Consequence**: Every action echoes through time
2. **Strategic Depth**: Simple rules create complex multi-wave strategies
3. **Player Agency**: Choose between immediate success and future ease
4. **Emergent Complexity**: Paired waves create unpredictable scenarios
5. **Accessible Depth**: Easy to understand, lifetime to master

### Unique Selling Points
- **First puzzle game where defense becomes offense**
- **Temporal strategy in bite-sized puzzle format**
- **Every playthrough creates unique challenges**
- **Player creates their own difficulty curve**
- **Compression of strategic depth into simple mechanics**

## Implementation Priorities

### Phase 1: Core Pairing System
1. Marker position recording infrastructure
2. Basic inheritance rules (1:1 marker to cube)
3. Wave B generation from Wave A markers
4. Visual feedback for inheritance

### Phase 2: Advanced Features
1. Ghost preview system
2. Compression algorithm for wave data
3. Overlap resolution strategies
4. Pattern library system

### Phase 3: Polish and Balance
1. Inheritance rule variations per stage
2. Visual language for temporal connections
3. Tutorial system for paired waves
4. Difficulty curve refinement

## Design Critique and Considerations

### Strengths of Paired Wave System
- **Unique Mechanic**: No other puzzle game has this temporal strategy layer
- **Emergent Gameplay**: Simple rule creates endless strategic possibilities
- **Player Expression**: Multiple valid strategies for each wave pair
- **Replayability**: Same wave pair plays differently based on Wave A choices
- **Elegant Compression**: Entire system fits within existing framework

### Potential Concerns and Mitigations

#### Complexity Overload
**Concern**: Players may struggle with planning for future waves
**Mitigation**: 
- Start with simple 1:1 conversions
- Visual previews make consequences clear
- Tutorial stages focus solely on inheritance
- Optional "preview mode" for planning

#### Frustration Potential
**Concern**: Players may feel punished for successful defense
**Mitigation**:
- Frame as strategic choice, not punishment
- Provide Cube markers as inheritance-free option
- Balance so inherited cubes are manageable
- Reward good inheritance patterns

#### Balancing Challenge
**Concern**: Difficulty to balance across skill levels
**Mitigation**:
- Adjustable inheritance delays
- Variable conversion rules per difficulty
- Optional inheritance intensity settings
- Pattern library for struggling players

### Recommended Refinements
1. **Inheritance Delay Options**: Configure how many rows back inherited cubes spawn
2. **Conversion Variations**: Different stages could have unique conversion rules
3. **Inheritance Bonuses**: Reward clever placement patterns with power-ups
4. **Memory Aid**: Visual timeline showing marker history
5. **Undo System**: Allow one undo per wave pair for learning

---
**Last Updated:** November 17, 2025  
**Major Innovation:** Paired Wave System with Marker Inheritance
**Implementation Status:** Core systems complete, paired waves in design phase
**Compression Benefit:** Reduces wave configuration size by 40%
**Related Documents:**
- [Gameplay Mechanics](3_GameplayMechanics_v2.md)
- [Level Design](4_LevelDesign.md)
- [Technical Architecture](TechnicalArchitecture.md)
- [Wave Configuration Schema](WaveConfigSchema.md)