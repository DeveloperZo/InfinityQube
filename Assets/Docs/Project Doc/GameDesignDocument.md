# Game Design Document

## Executive Summary
* **Title:** Infinity Cube
* **Genre:** Grid-based Tactical Puzzle
* **Target Platform:** PC (Windows) via Steam
* **Target Audience:** Strategic puzzle enthusiasts and hardcore casual gamers
* **Development Stage:** Production Ready with Core Systems Complete
* **Engine:** Unity 3D with component-based architecture

### High Concept
A grid-based tactical puzzle game where players strategically place markers to intercept advancing cube formations before they escape. Combining the spatial reasoning of classic puzzle games with resource management and predictive planning in a minimalist 3D environment that prioritizes mechanical clarity and strategic depth.

### Key Features
- **Four-Tier Marker System**: Light, Heavy, Prime, and Cube markers with distinct strategic applications ✅ (Production Complete)
- **Diverse Cube Mechanics**: Four cube types (Unit, Prime, Infinity, Recursion) creating varied tactical scenarios ✅ 
- **Progressive Stage Design**: 12-stage structured learning curve with clear pedagogical progression
- **Wave Completion Feedback**: Comprehensive wave completion messages with progress tracking ✅ (July 2025)
- **Stage Transition System**: Smooth stage success transitions with demo completion flow ✅ (July 2025)
- **Audio System Foundation**: Comprehensive audio manager with subsystem architecture ✅ (July 2025)
- **Comprehensive Statistics**: Detailed performance tracking enabling improvement and replayability ✅
- **Robust Debug Infrastructure**: Extensive developer tooling supporting rapid iteration ✅
- **Cosmic-themed Minimalist Aesthetics**: Clean geometric design with stellar atmospheric elements

## Architecture Documentation

This Game Design Document serves as the master reference, with detailed implementation specifications provided in specialized architecture documents:

- **[Artistic Architecture](5_ArtisticArchitecture.md)**: Comprehensive visual identity framework for external graphics and animation teams
- **[Sound Architecture](6_SoundArchitecture.md)**: Complete audio system specifications with focus on infinity cube signature sounds and dynamic cadence patterns

## Game Overview

### Core Gameplay Loop
**Wave Initiation** → **Strategic Positioning** → **Marker Placement** → **Active Engagement** → **Dynamic Response** → **Wave Resolution**

1. **Wave Initiation**: Player-controlled wave starts (ENTER key) with predetermined cube formations
2. **Strategic Positioning**: WASD navigation, threat assessment, and opportunity recognition
3. **Marker Placement**: Light markers (F), Prime markers (G), resource management
4. **Active Engagement**: Cube advancement, capture events, prime cube rewards, manual detonation control
5. **Dynamic Response**: Resource regeneration, state adaptation, threat reaction
6. **Wave Resolution**: Success conditions, statistics tracking, progression to next challenge

### Setting
**Minimalist Abstract World** featuring clean geometric shapes, color-coded mechanics communication, dynamic height variations, and cosmic backdrop with subtle stellar elements. The visual design prioritizes functional clarity while maintaining atmospheric depth through themes of infinity and mathematical beauty.

## Gameplay Mechanics

### Core Systems Implementation

#### **Grid System (GridManager)**
- Singleton-based spatial management with configurable dimensions per stage
- Vector2Int to 3D world position mapping with boundary enforcement
- Fallen row tracking with dynamic playable area reduction
- Runtime grid operations with validation and collision detection

#### **Player System (PlayerManager + PlayerActionManager)**
- Smooth analog movement (WASD) with acceleration/deceleration physics
- Four-tier marker system: Light (F/R), Heavy (V/Y), Prime (G/T), Cube markers (Q/E)
- Resource management with charge limits, cooldowns, and regeneration
- Comprehensive statistics tracking: captures, escapes, efficiency metrics

#### **Cube System**
| Type | Properties | Strategic Value |
|------|------------|-----------------|
| **Unit** | Capturable, basic scoring | Safe interaction, foundational gameplay |
| **Prime** | Capturable, generates cube markers | High value, creates detonation resources |
| **Infinity** | Uncapturable, face painting mechanics | Absolute threat, forces repositioning, enables corruption |
| **Recursion** | Capturable, Multi-hit requirement | High durability, optimized for heavy markers |

#### **Wave Management (WaveManager)** ✅
- Manual wave initiation with step-based cube advancement
- Configurable timing parameters and resource constraints per wave
- Wave completion messages showing progress (e.g., "Wave 1/3") with statistics ✅
- Pause functionality for tutorial feedback messages (Press K to continue) ✅
- Debug controls for testing and manual progression
- ScriptableObject-based wave configuration system
- Event-driven architecture for stage integration (OnWaveComplete, OnWaveFailed, OnAllWavesComplete) ✅

### Input System
```
Core Controls:
Movement: WASD/Arrows → Grid navigation
Light Marker: F → Single-tile placement  
Heavy Marker: V → Enhanced single-tile placement
Prime Marker: G → 3x3 area placement
Trigger Light: R → Activate light markers
Trigger Heavy: Y → Activate heavy markers
Trigger Prime: T → Activate prime markers
Trigger Cube Marker: Q → Direct cube detonation
Power Up Cube Marker: E → Enhanced cube marker detonation
Wave Control: ENTER → Start wave progression
System: P (restart), ESC (quit), TAB (toggle UI)
```

## Level Design  

### Learning Curve Structure

#### **Act 1: Learn the Rules (Stages 0-2)**
- **Focus**: Core loop establishment and danger recognition
- **Grid Size**: 5x20 with basic tool introduction
- **Key Learning**: Movement, marker placement, infinity cube lethality

#### **Act 2: Efficiency Under Pressure (Stages 3-5)**  
- **Focus**: Density management and resource optimization
- **Grid Size**: 7x25 with prime marker introduction
- **Key Learning**: Resource constraints, prime cube value, spatial management

#### **Act 3: Advanced Tactics (Stages 6-8)**
- **Focus**: Complex interactions and forward planning
- **Grid Size**: 9x28-32 with dense cube introduction
- **Key Learning**: Chain reactions, durability mechanics, perfect efficiency

#### **Act 4: Environmental Hazards (Stages 9-10)**
- **Focus**: Dynamic board states and environmental interaction
- **Grid Size**: 9x35-11x38 with tile state systems
- **Key Learning**: Corrupted/Enhanced tiles, risk/reward optimization

#### **Act 5: Mastery Test (Stages 11-12)**
- **Focus**: Synthesis and ultimate challenge
- **Grid Size**: 11x42-50 with dynamic conditions
- **Key Learning**: Adaptation under pressure, mastery demonstration

### Stage Configuration System
```
StageData Components:
- Grid dimensions and player start position
- Wave configuration references and sequencing
- Success criteria: capture requirements, escape limits
- Learning objectives and contextual descriptions
```

## Technical Architecture

### **Technology Stack**
- **Engine**: Unity 3D with component-based architecture
- **Platform**: PC/Windows with Steam distribution target
- **Performance Target**: Stable 60 FPS with minimal system requirements

### **Core Architecture Patterns**
- **Singleton Managers**: GridManager, centralized system coordination
- **Component Composition**: PlayerManager + PlayerActionManager separation
- **Data-Driven Configuration**: ScriptableObject-based stage and wave definitions
- **Event-Driven Updates**: Statistics and UI notifications

### **Debug and Testing Infrastructure**
- **Modular Debug Panels**: Gameplay, Testing, Wave, Cube inspection systems
- **Real-time Value Monitoring**: Live system state examination
- **Manual Control Overrides**: Testing edge cases and scenarios
- **Comprehensive Logging**: Performance tracking and issue identification

### **Scalability and Maintenance**
- **400-Line File Limit**: Maintainable code organization
- **Single Responsibility Principle**: Clear method boundaries for easy modification
- **Modular Design**: Independent component modification capability
- **POC-Focused Architecture**: Working implementation over premature optimization

## Visual Design

### **Art Style Philosophy**
- **Minimalist Geometric Aesthetic**: Clean shapes communicating mechanical function
- **Clear Visual Communication**: Color coding and height variations indicating game states
- **Cosmic Atmospheric Elements**: Dark space backgrounds with subtle particle effects
- **Dynamic Feedback**: Responsive visual cues for player actions and state changes

### **Detailed Visual Specifications**
For comprehensive visual identity implementation, including infinity cube distinctive features, cosmic material systems, and external team integration guidelines, see:

**→ [Artistic Architecture Document](5_ArtisticArchitecture.md)**

This specialized document provides detailed specifications for:
- Infinity Cube visual identity with luscious black & white cosmic dust materials
- Four-cube aesthetic hierarchy and material systems
- Cosmic/mathematical design principles with "Geometric Precision meets Cosmic Chaos"
- Animation frameworks synchronized to 120 BPM rhythm
- UI/UX cosmic control panel specifications
- Unity prefab architecture for external graphics teams

### **UI/UX Design Principles**  
- **Contextual Information**: TAB-toggleable interface with dynamic tips system
- **Minimal HUD Elements**: Essential information without screen clutter
- **Clear State Communication**: Visual indicators for charges, cooldowns, objectives
- **Intuitive Control Feedback**: Immediate response to player input

## Audio Design

### **Sound Design Philosophy**
- **Mechanical Clarity**: Audio cues supporting visual feedback
- **Strategic Information**: Sound indicating off-screen cube movement and state changes
- **Minimal Ambient Design**: Subtle atmospheric audio supporting concentration
- **Responsive Feedback**: Audio confirmation of player actions and game events

### **Comprehensive Audio System Specifications**
For complete audio implementation including the critical infinity cube signature sound and dynamic cadence patterns, see:

**→ [Sound Architecture Document](6_SoundArchitecture.md)**

This specialized document provides detailed specifications for:
- **Infinity Cube Signature Sound**: The distinctive audio identity that defines the game's core sound
- **Dynamic Cadence System**: Revolutionary audio system that transforms gameplay into living musical composition
- **"Cosmic Jazz" Audio Philosophy**: Intelligent mastery of cube rhythms with cosmic wanderlust
- **Step-based rhythm synchronization** with WaveManager timing (120 BPM framework)
- **Unity AudioSource integration** with 3D positioning and mixer architecture
- **Performance optimization** for complex audio scenarios

### **Implementation Priority**
- **HIGHEST**: Infinity cube signature sound and cadence pattern system (4 complexity points)
- **Core Action Sounds**: Marker placement, cube capture, detonation effects
- **System Feedback**: Wave progression, resource regeneration, state transitions
- **Atmospheric Enhancement**: Subtle background elements supporting cosmic theme

## Current Development Status

### **Completed Systems** ✅
- Complete core gameplay loop with all primary mechanics
- **Four-tier marker system (Light/Heavy/Prime/Cube) - PRODUCTION COMPLETE** (June 23, 2025)
- Face painting mechanics with rotation tracking
- Corruption/enhancement tile system integrated
- Recursion cube multi-hit mechanics with heavy marker optimization
- Comprehensive statistics tracking and performance analysis
- Production-quality debug tooling and testing infrastructure
- Integration testing framework with 100% system validation (June 23, 2025)
- Wave management system with editor tools
- Player action system with full input handling
- New cube terminology (Unit/Prime/Infinity/Recursion) fully implemented
- **Wave completion feedback messages with progress tracking** ✅ (July 8, 2025)
- **Stage success transitions and demo completion flow** ✅ (July 8, 2025)
- **Audio system foundation with comprehensive subsystem architecture** ✅ (July 8, 2025)

### **Current Development Focus - Phase 2: Audio + UI + Polish** (July 2025)
- **Audio system implementation** ✅ FOUNDATION COMPLETE (July 8, 2025)
  - AudioManager singleton with DontDestroyOnLoad ✅
  - Subsystem architecture: AudioSourcePool, AudioPlaybackSystem, AudioVolumeController, CubeAudioSystem ✅
  - Event-driven audio triggers integrated with game events ✅
  - Volume category management system ✅
  - Debug testing tools implemented ✅
  - **Remaining**: Audio content creation and integration testing
- **UI modernization** (OnGUI → Unity UI conversion - 3 complexity points) - IN PROGRESS
- **Visual polish pass** (particle effects, game feel - 2 complexity points)
- Cosmic theme visual integration

### **Short-Term Development** (August 2025)
- Stage content creation (6-8 additional stages)
- Meta-progression systems (achievements, ratings)
- Save/load system for progress persistence
- Performance optimization

### **Release Preparation** (September 2025)
- Steam platform integration and distribution preparation
- Performance optimization and platform-specific enhancements
- Final QA testing and polish
- Marketing materials and launch preparation

---
**Last Updated:** July 13, 2025  
**Document Version:** 3.3 - Wave Feedback and Audio Foundation Update  
**Development Phase:** Phase 2 - Audio + UI + Polish  
**Core Systems Status:** Four-tier marker system PRODUCTION COMPLETE (June 23, 2025)  
**Recent Completions:** Wave completion messages, stage transitions, audio system foundation (July 8, 2025)  
**Measured Development Velocity:** 17.8 complexity points/month  
**Projected Release:** September 15, 2025  
**Architecture Documents:** [Artistic Architecture](5_ArtisticArchitecture.md) | [Sound Architecture](6_SoundArchitecture.md)