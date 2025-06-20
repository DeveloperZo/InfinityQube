# Game Design Document

## Executive Summary
* **Title:** Infinity Cube
* **Genre:** Grid-based Tactical Puzzle
* **Target Platform:** PC (Windows) via Steam
* **Target Audience:** Strategic puzzle enthusiasts and hardcore casual gamers
* **Development Stage:** Functional Prototype with Implemented Core Systems
* **Engine:** Unity 3D with component-based architecture

### High Concept
A grid-based tactical puzzle game where players strategically place markers to intercept advancing cube formations before they escape. Combining the spatial reasoning of classic puzzle games with resource management and predictive planning in a minimalist 3D environment that prioritizes mechanical clarity and strategic depth.

### Key Features
- **Multi-layered Marker System**: Individual, area, and cube markers with distinct strategic applications
- **Diverse Cube Mechanics**: Four cube types (Normal, Blue, Black, Reinforced) creating varied tactical scenarios  
- **Progressive Stage Design**: 12-stage structured learning curve with clear pedagogical progression
- **Comprehensive Statistics**: Detailed performance tracking enabling improvement and replayability
- **Robust Debug Infrastructure**: Extensive developer tooling supporting rapid iteration
- **Cosmic-themed Minimalist Aesthetics**: Clean geometric design with stellar atmospheric elements

## Game Overview

### Core Gameplay Loop
**Wave Initiation** → **Strategic Positioning** → **Marker Placement** → **Active Engagement** → **Dynamic Response** → **Wave Resolution**

1. **Wave Initiation**: Player-controlled wave starts (ENTER key) with predetermined cube formations
2. **Strategic Positioning**: WASD navigation, threat assessment, and opportunity recognition
3. **Marker Placement**: Individual markers (F), Area markers (G), resource management
4. **Active Engagement**: Cube advancement, capture events, blue cube rewards, manual detonation control
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
- Three-tier marker system: Individual (F/R), Area (G/T), Cube markers (Q)
- Resource management with charge limits, cooldowns, and regeneration
- Comprehensive statistics tracking: captures, escapes, efficiency metrics

#### **Cube System**
| Type | Properties | Strategic Value |
|------|------------|-----------------|
| **Normal** | Capturable, basic scoring | Safe interaction, foundational gameplay |
| **Blue** | Capturable, generates cube markers | High value, creates detonation resources |
| **Black** | Uncapturable, lethal to player | Absolute threat, forces repositioning |
| **Reinforced** | Multi-hit requirement | High durability, resource optimization challenge |

#### **Wave Management (WaveManager)**
- Manual wave initiation with step-based cube advancement
- Configurable timing parameters and resource constraints per wave
- Debug controls for testing and manual progression
- ScriptableObject-based wave configuration system

### Input System
```
Core Controls:
Movement: WASD/Arrows → Grid navigation
Individual Marker: F → Single-tile placement  
Area Marker: G → 2x2 area placement
Trigger Individual: R → Activate individual markers
Trigger Area: T → Activate area markers
Trigger Cube Marker: Q → Direct cube detonation
Wave Control: ENTER → Start wave progression
System: P (restart), ESC (quit), TAB (toggle UI)
```

## Level Design  

### Learning Curve Structure

#### **Act 1: Learn the Rules (Stages 0-2)**
- **Focus**: Core loop establishment and danger recognition
- **Grid Size**: 5x20 with basic tool introduction
- **Key Learning**: Movement, marker placement, black cube lethality

#### **Act 2: Efficiency Under Pressure (Stages 3-5)**  
- **Focus**: Density management and resource optimization
- **Grid Size**: 7x25 with area marker introduction
- **Key Learning**: Resource constraints, blue cube value, spatial management

#### **Act 3: Advanced Tactics (Stages 6-8)**
- **Focus**: Complex interactions and forward planning
- **Grid Size**: 9x28-32 with reinforced cube introduction
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

### **Implementation Priority**
- **Core Action Sounds**: Marker placement, cube capture, detonation effects
- **System Feedback**: Wave progression, resource regeneration, state transitions
- **Atmospheric Enhancement**: Subtle background elements supporting cosmic theme

## Current Development Status

### **Implemented Systems**
- Complete core gameplay loop with all primary mechanics
- Comprehensive marker and detonation systems
- Full statistics tracking and performance analysis
- Extensive debug tooling and testing infrastructure
- Foundational stage progression with tutorial content

### **Near-Term Development**
- Advanced cube type implementation (Reinforced mechanics)
- Complete tile state system (Corrupted/Enhanced tiles)
- Remaining stage content creation and balance testing
- Audio system integration and feedback implementation

### **Future Expansion**
- Steam platform integration and distribution preparation
- Advanced tutorial systems and player onboarding
- Post-launch content pipeline and community feedback systems
- Performance optimization and platform-specific enhancements

---
**Last Updated:** December 20, 2024  
**Document Version:** 2.0 - Reflecting Current Implementation  
**Development Phase:** Functional Prototype with Core Systems Complete