# Gameplay Mechanics

> This document details the core mechanical systems of InfinityQube from a gameplay design perspective. For technical implementation details, see the Technical Documentation folder.

## Purpose
Outlines the functional rules, player interactions, and systemic behaviors that define InfinityQube's gameplay experience. This document focuses on what players experience and how game systems behave, rather than technical implementation details.

## 3.1 Grid System

### Core Structure
The game takes place on a grid-based battlefield where cubes move in opposing directions - wave cubes advancing toward escape and player cubes moving backward to intercept them. Each stage features a configurable grid that defines the playable area.

- **Grid Dimensions**: Vary per stage, creating different tactical challenges
- **Tile-Based Movement**: All entities (player, cubes) move in discrete grid steps
- **Boundary Enforcement**: Movement is constrained to valid grid positions
- **Bidirectional Flow**: Grid supports both forward (wave) and backward (player) cube movement

### Tile States
Tiles can exist in different states that affect gameplay:

| State | Behavior | Visual Indicator | Player Interaction |
|-------|----------|------------------|-------------------|
| Normal | Default state | Base appearance | Can place markers freely |
| Corrupted | Modified by Infinity cube | Corruption visual | Cannot place markers |
| Occupied | Contains marker awaiting transformation | Marker visual | Cannot place additional marker |

## 3.2 Cube System

### Core Cube Types
Four distinct cube types create varied tactical challenges:

| Type | Movement | Capture Behavior | Special Properties |
|------|----------|------------------|-------------------|
| **Unit** | Standard step movement | Single collision capture | Basic scoring, mid-flight conversion capability |
| **Prime** | Standard step movement | Single collision capture | Generates cube markers when captured |
| **Infinity** | Standard step movement | Cannot be captured | Corrupts tiles, player cubes pass through |
| **Recursion** | Standard step movement | Multiple hits required | High durability, requires Heavy cube collisions |

### Movement System
- **Step-Based Progression**: All cubes move in discrete steps
- **Directional Flow**: Wave cubes move forward, player cubes move backward
- **Consistent Timing**: Movement occurs at regular intervals per wave step
- **Speed Variants**: Normal and fast movement modes available
- **Collision Detection**: Cubes interact when occupying same tile

### Face Painting System
An advanced system that dynamically modifies cube behavior based on which face of the cube is active:

**Face Status Types**:
- **None**: Standard cube behavior
- **Corrupted**: Acts like Infinity cube when active (blocks capture)
- **Enhanced**: Creates additional effects when captured

**Strategic Implications**:
- Cube orientation affects collision outcomes
- Face status can change cube behavior mid-movement
- Creates complex tactical scenarios requiring adaptive strategies

#### Corruption Mechanics
Infinity cubes interact with the environment in unique ways:

- **Tile Corruption**: Infinity cubes corrupt tiles they pass through
- **Corrupted Tile Behavior**: 
  - Rejects marker placement
  - Can paint faces of cubes that land on them
  - Visual indicators show corruption state
- **Cleansing**: Corrupted tiles return to normal after time or interaction limits

### Cube Properties
- **Position Tracking**: Cubes exist at specific grid coordinates
- **Type System**: Each cube has a base type that determines collision behavior
- **Capture Eligibility**: Some cubes cannot be captured (Infinity type, Corrupted faces)
- **Movement State**: Cubes progress through the grid during wave steps

## 3.3 Symmetrical Wave System (Core Mechanic)

### Fundamental Concept
The Symmetrical Wave System is the core gameplay mechanic where players place markers that transform into backward-moving cubes to intercept forward-moving wave cubes. This creates an infinity symbol (∞) pattern of opposing forces meeting at calculated collision points.

### Marker-to-Cube Transformation

#### Marker Types and Their Moving Cube Forms

**Light Markers → Light Cubes**
- **Placement**: Press F to place Light marker at current position
- **Transformation**: Converts to Light cube on next wave step
- **Collision Behavior**: Standard capture on collision with wave cubes
- **Resource Cost**: Consumes one Light charge
- **Regeneration**: Charges regenerate automatically after cooldown

**Heavy Markers → Heavy Cubes**
- **Placement**: Press V to place Heavy marker at current position
- **Transformation**: Converts to Heavy cube on next wave step
- **Collision Behavior**: Enhanced damage, effective against Recursion cubes
- **Resource Cost**: Consumes one Heavy charge
- **Regeneration**: Longer cooldown than Light markers

**Prime Markers → Prime Cubes**
- **Placement**: Press G to place Prime marker at current position
- **Transformation**: Converts to Prime cube on next wave step
- **Collision Behavior**: 3x3 area effect at collision point
- **Resource Cost**: Consumes one Prime charge
- **Regeneration**: Longest cooldown period

**Cube Markers (Special Case)**
- **Generation**: Created exclusively when capturing Prime cubes
- **Activation**: Press Q to detonate immediately (does not transform)
- **Behavior**: Direct detonation at placement location
- **No Transformation**: Remains static, provides instant area effect

### Movement Mechanics

#### Player Cube Movement
- **Backward Movement**: Player cubes move backward at one tile per wave step
- **Synchronized Timing**: Movement matches wave cube progression exactly
- **Grid Boundaries**: Player cubes vanish when reaching grid edge
- **Pass-Through**: Player cubes pass through each other without collision

#### Wave Cube Movement
- **Forward Movement**: Wave cubes advance toward escape line
- **Step Progression**: One tile per wave step
- **Escape Boundary**: Reaching bottom of grid counts as escape
- **Collision Priority**: Wave cubes interact with player cubes when paths cross

### Collision System

#### Collision Detection
- **Spatial Overlap**: Collision occurs when cubes occupy same tile
- **Step-Based Resolution**: Collisions checked each wave step
- **Type Matching**: Outcome depends on cube type combinations
- **Simultaneous Processing**: Multiple collisions resolved in single step

#### Collision Outcomes

| Player Cube | Wave Cube | Result |
|------------|-----------|---------|
| Light | Unit | Unit captured, both cubes removed |
| Light | Prime | Prime captured, cube marker generated, both removed |
| Light | Recursion | Partial damage to Recursion, Light removed |
| Light | Infinity | No effect, Light passes through |
| Heavy | Unit | Unit captured, both cubes removed |
| Heavy | Prime | Prime captured, cube marker generated, both removed |
| Heavy | Recursion | Major damage (2-3 hits worth), both removed |
| Heavy | Infinity | No effect, Heavy passes through |
| Prime | Any (3x3) | Area capture at collision point |

#### Same-Type Collision Mechanics
Special resource generation when matching cube types collide:

- **Prime + Prime**: Both destroyed, Prime marker dropped at collision point
- **Heavy + Recursion**: Heavy deals damage, if Recursion destroyed, Heavy marker dropped
- **Resource Recycling**: Generated markers available after cooldown

### Mid-Flight Conversion (Unit Cubes)

#### Conversion Mechanics
- **Exclusive to Unit Cubes**: Only Light cubes from Unit markers can convert
- **Timing**: Can convert at any point during backward movement
- **Result**: Cube becomes static Light marker at current position
- **Strategic Use**: Bypass Infinity cubes or create delayed traps

#### Conversion Process
1. Light cube moving backward (from Unit marker)
2. Player initiates conversion (contextual command)
3. Cube transforms to Light marker at current tile
4. Marker awaits next wave for potential collision
5. No charge refund (strategic cost for flexibility)

### Strategic Depth

#### Spatial-Temporal Planning
- **Distance Calculation**: Far markers = late collisions, near markers = early collisions
- **Pattern Recognition**: Analyze wave patterns to predict collision points
- **Resource Allocation**: Balance marker types for optimal coverage
- **Timing Windows**: Place markers before wave step to ensure transformation

#### Infinity Symbol (∞) Gameplay
- **Two Loops**: Forward waves and backward player cubes form infinity
- **Meeting Points**: Strategic placement creates calculated intersections
- **Pattern Mirroring**: Success requires reverse-engineering wave patterns
- **Continuous Flow**: Endless cycle of placement, transformation, collision

## 3.4 Player System

### Movement Controls
- **Grid Navigation**: WASD/Arrow keys for player movement
- **Free Movement**: Player can move independently of wave timing
- **Positioning Strategy**: Choose optimal positions for marker placement
- **No Direct Combat**: Player cannot directly destroy cubes

### Marker Mode Selection
- **Press 1**: Switch to Light marker mode
- **Press 2**: Switch to Prime marker mode
- **Press 3**: Switch to Heavy marker mode
- **Visual Indicator**: Current mode displayed in UI

### Resource Management

#### Charge System
- **Light Charges**: High quantity, fast regeneration
- **Heavy Charges**: Medium quantity, medium regeneration
- **Prime Charges**: Low quantity, slow regeneration
- **Cube Markers**: Generated through Prime cube captures

#### Regeneration Mechanics
- **Automatic Recovery**: Charges regenerate over time
- **Independent Timers**: Each marker type has separate cooldown
- **Maximum Capacity**: Cannot exceed starting charge limits
- **Continuous Process**: Regeneration occurs during wave progression

### Player Statistics Tracking
- **Cube Captures**: Tracked by type (Unit, Prime, Recursion)
- **Marker Placements**: Total markers placed per type
- **Collision Success Rate**: Percentage of successful interceptions
- **Resource Efficiency**: Captures per marker placed
- **Perfect Timing**: Optimal collision achievements

## 3.5 Wave Management System

### Wave Progression
- **Manual Start**: Press ENTER to begin wave
- **Step-Based Movement**: Discrete advancement for all cubes
- **Transformation Trigger**: Placed markers convert on first step
- **Continuous Flow**: Cubes move until captured or escaped

### Wave Configuration
- **Cube Composition**: Types and quantities of wave cubes
- **Movement Timing**: Speed of wave progression
- **Grid Dimensions**: Playable area for the wave
- **Escape Threshold**: Maximum allowed escapes

### Wave States
- **Preparation**: Markers can be placed, no movement
- **Active**: Cubes moving, collisions occurring
- **Completed**: All cubes captured or escaped
- **Failed**: Escape limit exceeded

## 3.6 Input System

### Core Controls
| Action | Input | Description |
|--------|-------|-------------|
| **Movement** | WASD/Arrows | Navigate grid |
| **Place Light Marker** | F | Place Light marker (transforms next step) |
| **Place Heavy Marker** | V | Place Heavy marker (transforms next step) |
| **Place Prime Marker** | G | Place Prime marker (transforms next step) |
| **Detonate Cube Marker** | Q | Instantly detonate cube marker |
| **Select Light Mode** | 1 | Switch to Light marker mode |
| **Select Prime Mode** | 2 | Switch to Prime marker mode |
| **Select Heavy Mode** | 3 | Switch to Heavy marker mode |
| **Start Wave** | ENTER | Begin wave progression |
| **Restart Level** | P | Reset current stage |
| **Quit Game** | ESC | Exit application |

### Advanced Controls
| Action | Input | Effect |
|--------|-------|--------|
| **Continue** | K | Dismiss messages/continue |
| **Toggle UI** | TAB | Show/hide interface |
| **Send Feedback** | F12 | Open feedback system |

## 3.7 Stage System

### Stage Structure
- **Multi-Wave Composition**: Stages contain multiple waves
- **Progressive Difficulty**: Increasing complexity through stages
- **Learning Curve**: Introduces mechanics gradually
- **Win Conditions**: Specific capture requirements per stage

### Stage Types
- **Tutorial**: Teaches core mechanics step-by-step
- **Standard**: Normal gameplay with balanced difficulty
- **Challenge**: Special conditions and restrictions
- **Endless**: Continuous waves for high scores

### Stage Properties
- **Grid Size**: Defines playable area
- **Wave Count**: Number of waves in stage
- **Resource Limits**: Starting charges for markers
- **Objectives**: Clear success criteria

## 3.8 Collision and Capture System

### Collision Processing

#### Detection Phase
1. **Position Check**: Compare all cube positions each step
2. **Type Resolution**: Determine collision outcome by types
3. **Effect Application**: Apply capture, damage, or special effects
4. **Cleanup**: Remove destroyed cubes from grid

#### Capture Mechanics
- **Standard Capture**: Unit and Prime cubes destroyed on collision
- **Multi-Hit System**: Recursion cubes require multiple hits
- **Immunity**: Infinity cubes cannot be captured
- **Area Effects**: Prime cube collisions affect 3x3 area

### Visual and Audio Feedback

#### Visual Systems
- **Marker Indicators**: Show placement positions
- **Transformation Effects**: Marker-to-cube conversion animation
- **Movement Trails**: Indicate cube movement direction
- **Collision Impact**: Clear feedback when cubes collide
- **Capture Effects**: Distinct visuals for successful captures

#### Audio Design (Cosmic Lo-fi)
- **Ambient Soundscape**: Meditative background atmosphere
- **Placement Tones**: Soft confirmation sounds
- **Transformation Audio**: Subtle conversion effects
- **Collision Sounds**: Harmonic impact feedback
- **Success Indicators**: Positive reinforcement audio

## 3.9 Resource Management and Regeneration

### Charge Management

#### Initial Resources
- **Starting Charges**: Each marker type begins at maximum
- **Stage Variations**: Different stages may have different limits
- **No Pickups**: Resources regenerate only through time

#### Regeneration System
- **Automatic Process**: No player action required
- **Time-Based**: Fixed cooldown periods per type
- **Independent Cycles**: Each marker type regenerates separately
- **Continuous**: Occurs during all game states

### Strategic Resource Usage
- **Conservation**: Save charges for critical moments
- **Spam Prevention**: Cooldowns enforce strategic placement
- **Type Balancing**: Mix marker types for optimal coverage
- **Recovery Planning**: Account for regeneration timing

## 3.10 Advanced Strategies

### Tactical Patterns

#### Interception Strategies
- **Single-Point**: Target specific high-value cubes
- **Multi-Layer**: Create depth with staggered markers
- **Area Denial**: Use Prime cubes to control zones
- **Resource Farming**: Generate markers through same-type collisions

#### Timing Optimization
- **Wave Analysis**: Study patterns before placing
- **Predictive Placement**: Calculate future collision points
- **Rhythm Mastery**: Synchronize with wave step timing
- **Efficiency Focus**: Maximize captures per marker

### Skill Progression
1. **Beginner**: Understanding transformation mechanics
2. **Intermediate**: Timing collisions correctly
3. **Advanced**: Pattern prediction and resource management
4. **Expert**: Same-type collision farming
5. **Master**: Dynamic adaptation to complex patterns

---
**Last Updated:** November 16, 2024  
**Document Type:** Project Design Document  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)
- Technical Documentation (see Technical Doc folder)