# Gameplay Mechanics

> This document details the core mechanical systems of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Details the functional rules and systemic interactions, clearly outlining the behavior of grid, cubes, markers, detonation systems, and wave progression as currently implemented.

## 3.1 Grid System
### Core Structure
- **Configurable Dimensions**: Per-stage grid sizing (e.g., 5x20, 8x15)
- **Singleton Management**: GridManager handles all grid operations
- **World Space Mapping**: Vector2Int grid coordinates to 3D world positions
- **Boundary Enforcement**: Automatic player clamping to grid bounds
- **Fallen Row Tracking**: Dynamic reduction of playable area

### Tile States
| State | Behavior | Visual Indicator | Player Interaction |
|-------|----------|------------------|-------------------|
| Normal | Default state | Base height | Can place markers |
| Transformed | Modified by cube interaction | Height variation | Modified marker behavior |

### Grid Operations
- **IsValidGridPosition()**: Boundary validation
- **GridToWorldPosition()**: Coordinate conversion
- **GetPlayableRowCount()**: Dynamic area calculation
- **Height/Width Properties**: Runtime grid dimensions

## 3.2 Cube System
### Core Cube Types
| Type | Visual | Movement | Capture Behavior | Special Properties |
|------|--------|----------|------------------|-------------------|
| **Unit** | Gray | Standard step movement | Capturable via markers | Basic scoring |
| **Prime** | Blue | Standard step movement | Creates detonation markers | Generates cube markers on capture |
| **Infinity** | Black | Standard step movement | **Uncapturable** | Can generate corrupted tiles |
| **Recursion** | Darker/Metallic | Standard step movement | Requires multiple hits | Increased durability |

### Movement System
- **Step-Based Progression**: Discrete grid movement per wave step
- **Consistent Timing**: Configurable `moveInterval` per wave
- **Forward Only**: Cubes move down the grid toward escape
- **Speed Variants**: Normal and fast movement modes
- **Collision Detection**: With player and grid boundaries

### Face Painting System
Advanced cube state modification system that dynamically alters cube behavior:
```
FaceStatus Enum:
- None: Standard behavior
- Corrupted: Acts like Infinity cube when active
- Enhanced: Creates detonation when captured
```

#### Corruption Mechanics
- **Infinity Cube Interaction**: When markers hit Infinity cubes, their top face is painted with Corrupted status
- **Tile Corruption**: Infinity cubes with Corrupted faces corrupt tiles they land on
- **Corrupted Tile Behavior**: Corrupted tiles reject marker placement and paint non-Infinity cubes
- **Duration System**: Corrupted tiles have limited interaction counts and duration timers
- **Cleansing**: Corrupted tiles can be cleansed through time expiration or interaction limits

#### Enhanced Face Mechanics
- **Detonation Generation**: Enhanced faces create detonation markers when captured
- **Strategic Timing**: Enhanced faces provide tactical opportunities for chain reactions
- **Temporary Effect**: Enhanced faces typically have limited duration

Face painting affects cube behavior dynamically based on cube orientation and face state, creating complex tactical scenarios.

### Cube Properties
- **Position Tracking**: Vector2Int grid coordinates
- **World Position**: 3D transform synchronization
- **Type Inheritance**: Base CubeType with specialized behaviors
- **Capture State**: Tracking capture eligibility
- **Movement State**: Active/paused/destroyed states

## 3.3 Player System
### Movement Mechanics
- **Analog Input**: WASD/Arrow keys for smooth movement
- **Grid-Based**: Movement within grid boundaries
- **Collision System**: CharacterController-based physics
- **Smooth Animation**: Velocity-based movement with acceleration/deceleration
- **Rotation**: Faces movement direction dynamically

### Action System (PlayerActionManager)
Comprehensive marker and detonation management:

#### Light Markers (formerly Individual)
- **Placement Key**: F
- **Trigger Key**: R  
- **Charge System**: Limited uses with regeneration
- **Visual Feedback**: Placement indicators and charge display

#### Heavy Markers
- **Placement Key**: V
- **Trigger Key**: Y
- **Primary Target**: Enhanced marker specifically designed for Recursion cubes
- **Charge System**: Maximum 2 markers, limited charges with 5-second cooldown
- **Enhanced Power**: Optimized for multi-hit Recursion cube interactions
- **Universal Compatibility**: Works on all cube types with enhanced effectiveness
- **Strategic Value**: Critical for efficient Recursion cube management

#### Prime Markers (formerly Area)
- **Placement Key**: G
- **Trigger Key**: T
- **Coverage**: 3x3 grid area
- **Cooldown System**: Time-based restrictions
- **Resource Limits**: Configurable maximum on-grid count

#### Cube Markers
- **Trigger Key**: Q
- **Power Up Key**: E
- **Generation**: Created by capturing Prime cubes
- **Direct Detonation**: Immediate cube destruction
- **Strategic Resource**: Finite and valuable

### Heavy Marker Strategic Documentation

#### Overview
Heavy markers represent the specialized tier-2 marker system designed specifically for enhanced cube management, particularly targeting high-durability Recursion cubes. As part of the four-tier marker optimization system (Light/Heavy/Prime/Cube), heavy markers provide strategic depth through enhanced damage output and tactical positioning options.

#### Core Strategic Framework

##### Primary Function
Heavy markers serve as the critical bridge between basic light markers and area-effect prime markers, offering:
- **Enhanced Damage Output**: Significantly increased effectiveness against all cube types
- **Recursion Cube Specialization**: Optimized specifically for multi-hit Recursion cube interactions
- **Resource Efficiency**: Maximum impact per charge in high-value scenarios
- **Strategic Positioning**: Precise placement for maximum tactical advantage

##### Four-Tier System Integration
Heavy markers operate within the complete marker ecosystem:

**Tier 1 - Light Markers**: Basic cube capture, high quantity, short cooldown
**Tier 2 - Heavy Markers**: Enhanced damage, limited quantity, medium cooldown  
**Tier 3 - Prime Markers**: Area coverage, strategic positioning, long cooldown
**Tier 4 - Cube Markers**: Direct targeting, generated resource, immediate effect

#### Advanced Strategic Implementation

##### Recursion Cube Interaction Mastery

**Multi-Hit Reduction Strategy**:
- Standard markers require 3-4 hits for Recursion cube capture
- Heavy markers reduce requirement to 1-2 hits through enhanced damage
- Optimal placement can achieve single-detonation Recursion cube capture
- Critical for waves containing 3+ Recursion cubes

**Timing Window Optimization**:
```
Recursion Cube Approach Pattern:
1. Identify incoming Recursion cubes (3-4 tiles ahead)
2. Place heavy marker in optimal intercept position
3. Coordinate with movement prediction algorithms
4. Execute detonation at precise timing window
5. Confirm capture before cooldown management
```

**Strategic Positioning Matrices**:
- **Lane Control**: Heavy markers in central lanes maximize multi-cube potential
- **Convergence Points**: Place at natural cube path intersections
- **Escape Prevention**: Position 2-3 tiles from grid edge for last-chance captures
- **Corridor Blocking**: Use in narrow sections to guarantee cube interaction

##### Four-Tier System Optimization Techniques

**Synergistic Marker Combinations**:

1. **Heavy-Prime Synergy**:
   - Place Prime marker (3x3 area) in high-traffic zone
   - Position Heavy marker at Prime area edge for Recursion cube overlap
   - Results in dual-detonation scenarios with maximum coverage

2. **Light-Heavy Coordination**:
   - Use Light markers for Unit cube clusters (high quantity, low durability)
   - Reserve Heavy markers exclusively for Recursion cubes
   - Maintain 3:1 Light:Heavy ratio for optimal resource allocation

3. **Heavy-Cube Escalation**:
   - Generate Cube markers through Prime cube captures
   - Use Heavy markers to soften Recursion cubes
   - Finish with Cube markers for guaranteed capture

4. **Sequential Deployment Patterns**:
   ```
   Pattern A - "Cascade Control":
   Wave Start → Light markers for early Unit cubes
   Mid-Wave → Heavy markers for approaching Recursion cubes  
   Late-Wave → Prime markers for final cube clusters
   Emergency → Cube markers for escapees
   
   Pattern B - "Preemptive Strike":
   Pre-Wave → Heavy markers in predicted Recursion paths
   Wave Start → Light markers for immediate threats
   Mid-Wave → Prime markers for area control
   Cleanup → Cube markers for remaining targets
   ```

##### Advanced Tactical Applications

**Multi-Wave Strategic Planning**:
- Analyze upcoming wave compositions during current wave
- Pre-position Heavy markers for known Recursion cube patterns
- Coordinate cooldown timing with wave transition periods
- Maintain strategic reserves for unexpected cube configurations

**Resource Management Optimization**:
```
Heavy Marker Resource Algorithm:
1. Count Recursion cubes in incoming wave
2. Calculate required Heavy marker charges (Recursion count ÷ 2)
3. Plan placement timing based on 5-second cooldown
4. Reserve emergency charge for unexpected Recursion spawns
5. Coordinate with Prime marker availability for area support
```

**Critical Decision Matrix**:

| Scenario | Heavy Marker Priority | Alternative Strategy |
|----------|----------------------|---------------------|
| Single Recursion Cube | **HIGH** - Immediate placement | Light marker backup |
| Multiple Recursion Cubes | **CRITICAL** - Precise timing | Prime marker support |
| Mixed cube composition | **MEDIUM** - Selective targeting | Four-tier coordination |
| Unit cube heavy wave | **LOW** - Reserve for emergencies | Light marker focus |
| Prime cube cluster | **MEDIUM** - Support area capture | Cube marker generation |

##### Performance Optimization Strategies

**Efficiency Metrics**:
- **Capture Rate**: Target 85%+ Recursion cube capture with Heavy markers
- **Resource Utilization**: Achieve 1.5+ cube captures per Heavy marker charge
- **Timing Accuracy**: Maintain <0.3 second deviation from optimal detonation timing
- **Strategic Value**: Generate 150%+ point value compared to Light marker usage

**Advanced Timing Techniques**:
1. **Predictive Placement**: Calculate cube movement patterns and pre-position markers
2. **Perfect Timing Windows**: Execute detonations at exact cube center positioning
3. **Chain Reaction Coordination**: Trigger Heavy markers to initiate larger detonation sequences
4. **Cooldown Synchronization**: Align Heavy marker regeneration with wave progression

**Error Recovery Protocols**:
- **Missed Detonation**: Immediately assess alternative cube targets
- **Resource Depletion**: Switch to Light-Prime coordination strategies
- **Timing Failure**: Implement emergency Cube marker protocols
- **Multiple Recursion Crisis**: Execute containment strategies with available resources

#### Competitive Strategic Applications

**High-Level Play Optimization**:
- Master frame-perfect timing for maximum efficiency
- Develop muscle memory for common Recursion cube patterns
- Practice rapid decision-making for dynamic wave compositions
- Integrate Heavy marker usage into overall strategic flow

**Meta-Strategic Considerations**:
- Analyze map-specific Heavy marker placement opportunities
- Develop stage-specific Recursion cube management strategies
- Create contingency plans for various cube composition scenarios
- Master the psychological pressure management of limited Heavy marker resources

#### Implementation Guidelines

**Training Progression**:
1. **Basic Proficiency**: Consistent Recursion cube targeting
2. **Intermediate Tactics**: Four-tier system coordination
3. **Advanced Strategy**: Predictive placement and timing mastery
4. **Expert Application**: Meta-strategic integration and adaptation

**Common Mistakes to Avoid**:
- Using Heavy markers on Unit cubes (resource waste)
- Poor timing leading to missed Recursion cube captures
- Inadequate cooldown management causing resource gaps
- Failing to coordinate with other marker tiers
- Reactive rather than proactive placement strategies

**Success Indicators**:
- Consistent Recursion cube capture rates above 80%
- Efficient resource utilization across all marker tiers
- Smooth integration of Heavy markers into overall strategy
- Adaptive response to varying wave compositions
- Mastery of timing windows and positioning optimization

### Player Statistics
Comprehensive tracking system:
- **Cube Captures**: By type (Unit, Prime, Infinity attempts, Recursion)
- **Marker Usage**: Four-tier marker placement/triggers (Light, Heavy, Prime, Cube)
- **Detonation Metrics**: Efficiency and timing across all marker types
- **Movement Tracking**: Distance and time
- **Death/Respawn**: Player mortality events
- **Performance Metrics**: Success rates and efficiency per marker type
- **Heavy Marker Analytics**: Recursion cube interaction effectiveness

## 3.4 Wave Management System
### Wave Progression
- **Manual Control**: ENTER to start waves
- **Step-Based Movement**: Discrete cube advancement
- **Configurable Timing**: Per-wave `moveInterval` settings
- **Speed Control**: Normal/fast movement modes
- **Debug Features**: Manual step control and inspection

### Wave Configuration
```
WaveData Properties:
- GridWidth/Height: Grid dimensions
- moveInterval: Time between steps
- fastMoveInterval: Accelerated timing
- waveStartDelay: Initial delay
- CubesData: List of cube definitions
- Marker Limits: Resource constraints per wave
```

### Active Wave Management
- **Cube Tracking**: Live cube count and positions
- **State Management**: Active/paused/completed states
- **Event System**: Wave start/end notifications
- **Debug Controls**: Manual progression and inspection

## 3.5 Input System
### Core Controls
| Action | Input | System | Effect |
|--------|-------|--------|--------|
| **Movement** | WASD/Arrows | PlayerManager | Grid navigation |
| **Light Marker** | F | PlayerActionManager | Place single marker |
| **Heavy Marker** | V | PlayerActionManager | Place enhanced marker |
| **Prime Marker** | G | PlayerActionManager | Place 3x3 area marker |
| **Trigger Light** | R | PlayerActionManager | Activate light markers |
| **Trigger Heavy** | Y | PlayerActionManager | Activate heavy markers |
| **Trigger Prime** | T | PlayerActionManager | Activate prime markers |
| **Trigger Cube Marker** | Q | PlayerActionManager | Direct cube detonation |
| **Power Up Cube Marker** | E | PlayerActionManager | Enhance cube marker |
| **Start Wave** | ENTER | WaveManager | Begin wave progression |
| **Restart Level** | P | Game System | Reset current stage |
| **Quit Game** | ESC | Game System | Exit application |
| **Toggle UI** | TAB | GameUI | Show/hide interface |

### Advanced Controls
| Action | Input | System | Effect |
|--------|-------|--------|--------|
| **Close Dialog** | K | UI System | Dismiss messages |
| **Send Feedback** | F12 | FeedbackCollector | Open email feedback |

## 3.6 Stage System
### Stage Management
- **ScriptableObject Configuration**: Data-driven stage definitions
- **Progressive Difficulty**: Structured learning curve
- **Multi-Wave Composition**: Complex stage structures
- **Completion Tracking**: Win/loss conditions
- **Restart Functionality**: Reset capability

### Stage Types
```
StageType Enum:
- Tutorial: Teaching-focused stages
- Standard: Normal gameplay stages  
- Challenge: Difficult condition stages
- Bonus: Special rule stages
```

### Stage Properties
- **Grid Dimensions**: Per-stage grid sizing
- **Player Start Position**: Initial placement
- **Wave Configurations**: List of wave data references
- **Objective Text**: Clear success criteria
- **Completion Requirements**: Capture counts, escape limits

## 3.7 Detonation System
### Detonation Types
```
DetonationType Enum:
- Large: 3x3 area effect
- Standard: 3x3 area effect  
- Small: 2x2 area effect
- Single: Single tile effect
```

### Activation Methods
1. **Marker-Based**: Cubes pass through marked tiles
2. **Manual Trigger**: Player-activated marker detonation
3. **Cube Marker**: Direct cube targeting
4. **Prime Cube Capture**: Automatic marker generation

### Visual/Audio Feedback
- **Placement Indicators**: Clear marker visualization
- **Detonation Effects**: Particle systems and animations
- **Audio Cues**: Sound feedback for all actions
- **UI Updates**: Real-time charge and count display

## 3.8 Debug System
### Debug Panels
Comprehensive debugging infrastructure:
- **Gameplay Panel**: Stage, wave, player debugging  
- **Testing Panel**: Face painting and scenario testing
- **Wave Debug Panel**: Cube inspection and wave controls
- **Cube Inspector**: Individual cube state examination

### Debug Features
- **Real-Time Inspection**: Live value monitoring
- **Manual Controls**: Override automatic systems
- **State Manipulation**: Direct property modification
- **Scenario Testing**: Rapid iteration tools

---
**Last Updated:** June 27, 2025  
**Implementation Status:** Production-ready with four-tier marker system complete  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
- [Technical Debt](../TechnicalDebt.md)
- [Final Integration Test Report](../FinalIntegrationTestReport.md)