# Level Design

> This document details the progressive stage design and learning curve of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Outlines the structured learning progression, stage composition, and difficulty scaling that teaches players core mechanics while providing meaningful challenge escalation.

## 4.1 Design Philosophy
### Progressive Complexity
- **Single Concept Introduction**: Each stage introduces one new element
- **Mastery Reinforcement**: Previous concepts are reinforced before adding complexity
- **Clear Success Metrics**: Explicit objectives and feedback
- **Fail-Safe Learning**: Death teaches without excessive punishment

### Teaching Through Constraint
- **Limited Tools**: Restricting options focuses learning
- **Guided Discovery**: Stage design reveals optimal strategies
- **Resource Pressure**: Scarcity forces efficiency
- **Escalating Stakes**: Increasing consequences for mistakes

## 4.2 Current Implementation Status
### Implemented Stages
- **Stage 01**: Tutorial basics (5x20 grid)
- **Stage 03**: Prime cube mechanics (5x20 grid) 
- **Additional stages**: In development

### Stage Configuration System
```
StageData Properties:
- stageNumber: Sequential identifier
- stageName: Display name
- description: Learning goals and context
- objective: Clear success criteria
- gridWidth/Height: Dimensions
- playerStartPosition: Initial placement
- waveConfigurations: List of WaveData references
- requireAllCubesDestroyed: Completion condition
- requiredCaptureCount: Minimum captures needed
- maxAllowedEscapes: Failure threshold
```

## 4.3 Progression Structure

### Act 1: Learn the Rules (Stages 0-2)
**Focus**: Establishing core loop and primary danger

#### Stage 0: Pure Fundamentals
- **Grid**: 5x20
- **Tools**: Movement + Individual Markers (2 charges, standard cooldown)
- **Cubes**: Normal + Black
- **Learning Goal**: Movement, marker placement, death avoidance

**Wave Progression**:
- Wave 0_01: 2 rows, 1 black cube per row
- Wave 0_02: 2 rows, 2 black cubes per row  
- Wave 0_03: 2 rows, 3 black cubes per row

#### Stage 1: The First Rule - Death Exists
- **Grid**: 5x20
- **Tools**: Movement + Individual Markers (2 charges)
- **Cubes**: Normal + Black
- **Learning Goal**: Infinity cubes are lethal and uncapturable

**Wave Progression**:
- Wave 1_01: 3 rows, 1 black cube per row
- Wave 1_02: 3 rows, 2 black cubes per row
- Wave 1_03: 3 rows, 3 black cubes per row

#### Stage 2: Dancing with Danger  
- **Grid**: 5x20
- **Tools**: Movement + Individual Markers (2 charges)
- **Cubes**: Normal + Black + **Blue Introduction**
- **Learning Goal**: Prime cubes create opportunities

**Wave Progression**:
- Wave 2_01: 5 rows, 1 black cube per row (3 blue cubes)
- Wave 2_02: 5 rows, 2 black cubes per row (2 blue cubes)
- Wave 2_03: 5 rows, 3 black cubes per row (1 blue cube)

### Act 2: Efficiency Under Pressure (Stages 3-5)
**Focus**: Handling density while avoiding death

#### Stage 3: The Squeeze
- **Grid**: 7x25 (wider but more dangerous)
- **Tools**: Movement + Individual Markers (2 charges)
- **Cubes**: Normal + Black + Blue
- **Learning Goal**: Spatial management with increased grid size

**Wave Progression**:
- Wave 3_01: 5 rows, 1 black cube per row (3 blue cubes)
- Wave 3_02: 5 rows, 2 black cubes per row (2 blue cubes)
- Wave 3_03: 5 rows, 3 black cubes per row (1 blue cube)

#### Stage 4: Area Control Revolution
- **Grid**: 7x25
- **Tools**: Individual Markers (2) + **Area Markers** (3 charges, cooldown)
- **Cubes**: Normal + Black + Blue
- **Learning Goal**: Area markers transform impossible situations

**The Relief**: Area markers provide coverage but require strategic positioning - wasting area markers on uncapturable black cubes teaches resource management.

**Wave Progression**:
- Wave 4_01: 7 rows, 1 black cube per row (3 blue cubes)
- Wave 4_02: 7 rows, 2 black cubes per row (2 blue cubes)
- Wave 4_03: 7 rows, 3 black cubes per row (1 blue cube)

#### Stage 5: The Blue Solution
- **Grid**: 7x25
- **Tools**: Individual (2) + Area (2)
- **Cubes**: Normal + Black + Blue
- **Learning Goal**: Prime cubes as the "anti-infinity" - valuable detonation creators

**The Power-Up**: Prime cube captures create cube markers for direct detonation. Players learn the value hierarchy: Infinity = avoid, Prime = pursue aggressively.

**Wave Progression**:
- Wave 5_01: 7 rows, 1 black cube per row (3 blue cubes)
- Wave 5_02: 7 rows, 2 black cubes per row (2 blue cubes)
- Wave 5_03: 7 rows, 3 black cubes per row (1 blue cube)

### Act 3: Advanced Tactics (Stages 6-8)
**Focus**: Mastering complex interactions

#### Stage 6: Chain Reactions
- **Grid**: 9x28
- **Tools**: All previous systems
- **Cubes**: All types
- **Learning Goal**: Multi-step strategy and prediction

**The Combo**: Prime cubes positioned to create chain-clearing opportunities. Infinity cubes threaten perfect setups. Players learn forward thinking: "If I capture this Prime, the detonation will clear those Units before the Infinity cube blocks the lane."

#### Stage 7: The Wall
- **Grid**: 9x30
- **Tools**: All previous systems
- **Cubes**: All types + **Recursion Introduction**
- **Learning Goal**: Durability mechanics and indirect damage

**The Puzzle**: Recursion cubes (2-3 hits required) create barriers. Infinity cubes prevent direct assault. Players must use Prime cube detonations to damage Recursion cubes from safe distances.

#### Stage 8: Resource Management Master Class
- **Grid**: 9x32
- **Tools**: All previous (severely limited charges)
- **Cubes**: All types
- **Learning Goal**: Perfect efficiency under extreme constraints

**The Test**: Brutal resource constraints force optimal play. Every marker placement must be perfect. Infinity cubes punish waste. Prime cubes become precious efficiency multipliers.

### Act 4: Environmental Hazards (Stages 9-10)
**Focus**: Dynamic board states

#### Stage 9: Corrupted Earth
- **Grid**: 9x35
- **Tools**: All previous systems
- **Cubes**: All types
- **Tile States**: Normal + **Corrupted**
- **Learning Goal**: Environmental threats

**The Escalation**: Corrupted tiles make ANY cube passing over them temporarily act like black cubes. Safe cubes become dangerous based on position. The board itself becomes an adversary.

#### Stage 10: Risk and Reward
- **Grid**: 11x38
- **Tools**: All previous + **Tile State Changer** (1 use)
- **Cubes**: All types
- **Tile States**: Corrupted + **Enhanced**
- **Learning Goal**: Risk/reward optimization

**The Choice**: Enhanced tiles supercharge captures but are often positioned near corrupted tiles. Players face constant risk/reward decisions. The tile state changer becomes a crucial strategic tool.

### Act 5: Mastery Test (Stages 11-12)
**Focus**: Synthesis of all systems

#### Stage 11: Chaos Control
- **Grid**: 11x42
- **Tools**: All tools (balanced limits)
- **Cubes**: All types
- **Tile States**: **Dynamic** (changing during waves)
- **Learning Goal**: Adaptation under pressure

**The Pressure**: Everything players learned, accelerated and complex. Tile states shift during waves. Black cubes force constant movement. Blue cubes offer fleeting opportunities for skilled players.

#### Stage 12: The Final Exam
- **Grid**: 11x50
- **Tools**: Precise allocations per wave
- **Cubes**: All types
- **Learning Goal**: Mastery demonstration

**The Ultimate Test**:
- **Wave 1: "The Maze"** - Black cubes create complex navigation puzzles
- **Wave 2: "The Swarm"** - Overwhelming density requiring perfect blue cube usage
- **Wave 3: "The Dance"** - Everything in elegant, brutal harmony

## 4.4 Design Patterns

### Scaffolding Pattern
Each stage builds on previous knowledge:
1. **Isolation**: New mechanic introduced alone
2. **Integration**: Combined with previous mechanics
3. **Pressure**: Tested under increasing difficulty
4. **Mastery**: Required for progression

### Resource Arc Pattern
```
Learning Arc:
Tutorial → Abundance → Scarcity → Optimization → Mastery
```

### Difficulty Curve Principles
- **Gentle Introduction**: New mechanics start easy
- **Rapid Acceleration**: Quick ramp to meaningful challenge
- **Plateau Periods**: Time to internalize before next complexity
- **Spike Management**: Difficulty spikes are intentional teaching moments

## 4.5 Current Development Priorities

### ✅ Completed Foundation Systems (June 2025)
- **Four-Tier Marker System**: Light/Heavy/Prime/Cube markers fully implemented and integrated
- **Face Painting Integration**: Connected to stage progression with rotation tracking
- **Tile State System**: Corrupted/Enhanced tile mechanics operational
- **Cube Type Diversity**: Unit/Prime/Infinity/Recursion cube system with multi-hit mechanics
- **Technical Infrastructure**: Debug systems, analytics, and integration testing complete

### 🔄 Phase 2 Active Priorities (July-August 2025)
1. **Audio System Integration** ⭐ **HIGHEST PRIORITY** - Currently in progress
   - Core action sounds (marker placement, cube capture, detonation)
   - System feedback audio (wave events, resource regeneration)
   - Cosmic atmospheric ambience
   - Dynamic audio scaling with gameplay intensity

2. **UI Modernization & Polish**
   - OnGUI → Unity UI conversion for stage interface
   - Enhanced visual feedback for four-tier marker system
   - Improved charge indicators and cooldown displays
   - Modern stage completion/failure screens

3. **Stage Design Enhancement**
   - Leverage completed four-tier system for advanced stage concepts
   - Refine difficulty progression using production-ready mechanics
   - Create stages that showcase Heavy and Prime marker strategic depth
   - Implement Cube marker tactical scenarios

### Stage Design Focus Areas
- **Four-Tier Mastery Stages**: Dedicated levels teaching optimal marker type selection
- **Recursion Cube Scenarios**: Strategic multi-hit encounters requiring Heavy markers
- **Prime Marker Techniques**: Stages emphasizing precision timing and positioning
- **Cube Marker Tactics**: Advanced detonation strategy implementation
- **Audio-Driven Experience**: Stage pacing synchronized with audio feedback

### Testing and Iteration Focus
- **Four-Tier System Validation**: Ensure all marker types feel distinct and valuable
- **Audio Integration Testing**: Verify sound enhances rather than distracts from strategy
- **Visual Polish Assessment**: Confirm UI updates improve clarity and game feel
- **Performance Optimization**: Maintain 60 FPS with enhanced audio-visual systems

---
**Last Updated**: June 28, 2025  
**Implementation Status**: Four-tier marker system complete, core infrastructure operational, Phase 2 audio and UI enhancement active  
**Cross-Reference Status**: ✅ Updated for production four-tier marker system and current cube terminology  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Game Overview](2_GameOverview.md)
- [Technical Debt](../TechnicalDebt.md)
- [Final Integration Test Report](../FinalIntegrationTestReport.md)