# Level Design

> This document details the progressive stage design and learning curve of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Outlines the structured learning progression, stage composition, and difficulty scaling that teaches players core mechanics while providing meaningful challenge escalation.

## 4.1 Stage Numbering Convention

The game uses an offset stage numbering system:

| Stage Index | Purpose | Description |
|-------------|---------|-------------|
| **Stage 0** | Tutorial | Dedicated tutorial stage teaching fundamental mechanics |
| **Stages 1-12** | Core Game | Main campaign stages with progressive difficulty |

This offset ensures:
- Tutorial is always accessible at index 0
- Core game stages use intuitive 1-based numbering for players
- Clear separation between learning and challenge content

## 4.2 Design Philosophy
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

## 4.3 Onboarding Pacing

Structured progression that introduces mechanics gradually while building mastery:

| Stage Range | Wave Cubes | Player Markers | Teaching Focus |
|-------------|------------|----------------|----------------|
| **0-1** | Unit + Infinity | Unit only | Learn basics: movement, marker placement, Infinity cube avoidance |
| **2** | Unit + Infinity (Matrix discovery in final wave) | Unit only | Master Unit + Infinity fundamentals; single Matrix cube teaser at end |
| **3-4** | Unit + Matrix + Infinity | Unit only | Matrix cube learning: isolated introduction (early waves), then Infinity integration (later waves) |
| **5-6** | Unit + Matrix + Infinity | Unit + Matrix | Add Matrix marker: area capture, strategic positioning |
| **7-9** | Unit + Matrix + Recursion + Infinity | Unit + Matrix + Recursion | Add Recursion cube and Recursion marker: multi-hit mechanics, durability |
| **10** | All types | Unit + Matrix + Recursion + Infinity | Add Infinity marker: player Infinity introduction, resonance mechanics |
| **11-12** | All types | All types | Mastery tests: full strategic depth, complex interactions, optimal play |

### Detailed Stage Progression

#### Stages 0-1: Learn Basics (Unit + Infinity)
- **Stage 0 (Tutorial)**: Core fundamentals - movement, marker placement, basic capture, Infinity cube awareness
- **Stage 1**: Infinity cube mastery - learn navigation around dangerous cubes, strategic positioning
- **Grid Progression**: Narrow width, shallow spawn area expanding slightly

#### Stage 2: Fundamentals Mastery + Matrix Discovery
- **Stage 2**: Master Unit + Infinity interaction; Matrix cube teased in final wave as discovery moment
- **Grid Progression**: Same width, deeper spawn area

#### Stages 3-4: Matrix Cube Learning Phase
- **Stage 3**: Matrix cube introduction - isolated learning with no Infinity (early waves), then Matrix + Infinity integration
- **Stage 4**: Matrix cube strategy - efficient capture prioritization, value hierarchy understanding
- **Grid Progression**: First width expansion

#### Stages 5-6: Matrix Marker Phase
- **Stage 5**: Matrix marker mastery - optimal positioning and timing
- **Stage 6**: Matrix integration - combine Matrix markers with Unit markers for complex solutions
- **Grid Progression**: Consistent with Matrix learning phase

#### Stages 7-9: Recursion Introduction (Add Recursion Cube + Recursion Marker)
- **Stage 7**: Recursion cube introduction - multi-hit mechanics
- **Stage 8**: Recursion marker introduction - durability management
- **Stage 9**: Recursion strategy - combining with Matrix for complex solutions
- **Grid Progression**: Second width expansion

#### Stage 10: Infinity Marker Introduction
- **Stage 10**: Infinity marker unlocked - player Infinity introduction, resonance mechanics
- **Grid Progression**: Approaching end game width

#### Stages 11-12: Mastery Tests
- **Stage 11**: Advanced mastery - complex interactions, optimal resource usage
- **Stage 12**: Ultimate test - full strategic depth, all mechanics combined
- **Grid Progression**: Maximum width (end game)

### Pacing Principles
- **Single New Element Per Stage Range**: Each stage range introduces one new cube type or marker type
- **Reinforcement Before Advancement**: Previous mechanics are practiced before adding complexity
- **Marker Economy Introduction**: Non-Unit markers become scarce resources requiring management
- **Strategic Depth Unfolding**: Early stages teach basics, later stages reveal advanced combinations
- **Grid Scaling**: Width gradually increases from 5 (early stages) to 11-13 (end game), with row count increasing within early stages

## 4.4 Stage System Architecture

### Stage Types
The game features four distinct stage types, each serving different purposes in the player experience:

#### Tutorial Stages
- **Purpose**: Teaching-focused stages that introduce core mechanics
- **Characteristics**: 
  - Focused on single mechanic introduction
  - Generous resource allocation for learning
  - Clear objectives and feedback
  - Forgiving failure conditions
- **Progression**: Tutorial (Stage 0) unlocks standard stages (Stages 1-12)
- **Examples**: Stage 0 (Tutorial - Pure Fundamentals)

#### Standard Stages
- **Purpose**: Normal gameplay with balanced difficulty progression
- **Characteristics**:
  - Balanced resource allocation
  - Progressive complexity increases
  - Standard win/loss conditions
  - Core gameplay loop emphasis
- **Progression**: Standard stages form the main campaign path
- **Examples**: Most stages in Acts 2-5 follow standard progression

#### Challenge Stages
- **Purpose**: Difficult stages with special conditions and constraints
- **Characteristics**:
  - Restricted resources or special limitations
  - Higher difficulty thresholds
  - Unique win conditions
  - Test mastery of specific mechanics
- **Progression**: Challenge stages provide optional difficulty spikes
- **Examples**: Resource-limited stages, precision-timing challenges, survival scenarios

#### Bonus Stages
- **Purpose**: Special stages with unique rules and mechanics
- **Characteristics**:
  - Unique gameplay variations
  - Experimental mechanics
  - Special rewards or achievements
  - Non-standard progression rules
- **Progression**: Bonus stages offer alternative gameplay experiences
- **Examples**: Speed challenges, puzzle variations, special rule stages

### Stage Configuration Options
Each stage can be configured with the following parameters:

- **Grid Dimensions**: Width and height determine playable area size
- **Player Start Position**: Initial player placement on the grid
- **Wave Configurations**: List of waves that compose the stage
- **Success Conditions**: 
  - Require all cubes destroyed (strict completion)
  - Minimum capture count (flexible completion)
  - Maximum allowed escapes (failure threshold)
- **Stage Identity**: Name, description, and objective text displayed to players

### Stage Progression Flow
Stages follow a structured progression pattern:

1. **Stage Loading**: When a stage begins, the grid is configured to the stage's dimensions, the player is positioned at the start location, and all systems are reset
2. **Wave Sequence**: Stages consist of multiple waves that play sequentially. Each wave spawns cubes and presents a new challenge
3. **Wave Completion**: As each wave completes, players see progress feedback showing which wave they're on (e.g., "Wave 2/5") along with capture statistics
4. **Stage Completion**: When all waves in a stage are completed successfully, the stage is marked as complete. Players can then advance to the next stage automatically or manually
5. **Failure Handling**: If a wave fails (too many escapes, player death), the stage can be configured to restart automatically or return to the menu

### Progression Mechanics
Stages advance through a structured progression system:

- **Automatic Advancement**: Stages can automatically advance to the next stage upon completion
- **Manual Control**: Players can restart stages or select specific stages for practice
- **Transition Delays**: Configurable delays between stage transitions for pacing
- **Failure Handling**: Stages can be configured to restart on failure or return to menu
- **Completion Tracking**: System tracks stage attempts, completions, and statistics

### Wave Generation Patterns
Waves can be created through two primary methods:

#### Pre-Configured Waves
- **Manual Design**: Carefully crafted wave compositions for specific learning goals
- **Precision Control**: Exact cube placement and timing for teaching moments
- **Narrative Integration**: Waves designed to support stage themes and objectives
- **Use Cases**: Tutorial stages, key learning moments, story-critical encounters

#### Procedural Generation
- **Pattern-Based**: Uses predefined patterns that can be combined and modified
- **Difficulty Scaling**: Automatically adjusts cube composition based on stage difficulty
- **Cube Distribution**: Configurable percentages for each cube type (Unit, Matrix, Infinity, Recursion)
- **Spacing Control**: Maintains appropriate spacing between cubes for playability
- **Solvability Analysis**: System ensures generated waves are solvable
- **Use Cases**: Replayability, varied challenges, dynamic difficulty adjustment

#### Wave Generation Parameters
Procedural generation can be configured with:
- **Grid Constraints**: Minimum and maximum grid dimensions
- **Cube Distribution**: Percentage allocation for each cube type
- **Difficulty Multiplier**: Scales overall wave difficulty
- **Base Cube Count**: Starting number of cubes per wave
- **Cube Spacing**: Minimum and maximum spacing between cubes
- **Pattern Selection**: Available wave patterns for generation

### Wave Configuration Per Stage
Each wave within a stage can be individually configured:

- **Cube Placement**: Exact positions where cubes spawn at the start of the wave
- **Marker Availability**: Each wave can specify how many marker charges and counts are available (Unit, Matrix, Recursion, Infinity markers)
- **Timing Settings**: Wave-specific movement speeds and start delays
- **Success Criteria**: Waves can have their own completion requirements (capture counts, escape limits)
- **Highlight Sequences**: Waves can define guided sequences that combine messages, visual highlights, and interactive validation to teach mechanics

### Grid Configuration Per Stage
Stages can configure grid dimensions to create different tactical challenges:

#### Grid Width Progression
- **Early Stages (0-2)**: Narrow width (focused encounters, limited lateral options)
- **Matrix Learning (3-4)**: First width expansion (Matrix cube learning requires more space)
- **Matrix Marker (5-6)**: Consistent with learning phase
- **Recursion (7-9)**: Second width expansion (larger grids enable more strategy)
- **Infinity Marker (10)**: Approaching end game width
- **Mastery (11-12)**: Maximum width (end game complexity)

#### Grid Height Progression
- **Early Stages (0-2)**: Shallow depth (manageable forward planning)
- **Mid Stages (3-6)**: Moderate depth (increased planning requirements)
- **Late Stages (7-10)**: Deep grids (complex forward planning)
- **End Game (11-12)**: Maximum depth (full strategic depth)

#### Row Count Progression (Wave Spawn Area)
- **Tutorial (0)**: Minimal rows (gentle introduction)
- **Early Stages (1-2)**: Shallow spawn area, expanding within stage
- **Matrix Learning (3-4)**: Moderate spawn density
- **Matrix Marker (5-6)**: Consistent density
- **Recursion (7-9)**: High density spawn areas
- **Mastery (10-12)**: Maximum density (mastery tests)

Grid size directly impacts:
- Player movement options
- Marker placement strategies
- Cube path complexity
- Spatial awareness requirements
- Resource efficiency demands

## 4.5 Tutorial Stage Structure

### Tutorial Design Principles
Tutorial stages follow a specific structure to maximize learning effectiveness:

#### Introduction Phase
- **Single Mechanic Focus**: Each tutorial introduces one new concept
- **Guided Sequences**: Highlight sequences combine messages, visual highlights, and pauses to clearly communicate objectives
- **Generous Resources**: Extra charges and forgiving conditions allow experimentation
- **Immediate Feedback**: Visual highlights, audio cues, and interactive validation reinforce correct actions

#### Practice Phase
- **Reinforcement**: Same mechanic repeated with slight variations
- **Gradual Complexity**: Subtle increases in difficulty within the same mechanic
- **Safe Failure**: Mistakes don't immediately end the stage
- **Pattern Recognition**: Players begin to see optimal strategies
- **Sequence Guidance**: Highlight sequences can trigger at specific moments to reinforce learning

#### Mastery Phase
- **Integration**: New mechanic combined with previously learned concepts
- **Resource Constraints**: Slightly reduced resources encourage efficiency
- **Success Requirement**: Must demonstrate understanding to progress
- **Confidence Building**: Players feel capable before moving forward

### Tutorial Stage (Stage 0)

#### Stage 0: Tutorial - Pure Fundamentals
- **Type**: Tutorial
- **Mechanics Introduced**: Movement, marker placement, basic cube capture, Infinity cube avoidance
- **Grid**: Narrow width, standard starting depth
- **Wave Rows**: Minimal spawn area, expanding slightly across waves
- **Resources**: Unit markers only (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity (introduces both capturable and dangerous cubes)
- **Learning Goals**: 
  - Grid navigation and positioning
  - Marker placement timing
  - Basic capture mechanics (Unit cubes)
  - Infinity cube danger recognition and avoidance
  - Foundation for all future gameplay

This is the only dedicated tutorial stage (Stage 0). Stages 1-12 are the core game stages that progressively introduce new mechanics while building on tutorial fundamentals.

### Marker to Cube System Tutorial Progression

After basic mechanics are established, the game introduces its core innovation: the Marker to Cube System. This system teaches players to think about collisions, timing, and spatial relationships.

#### Phase 1: Collision Fundamentals (Stages 0-2)
- **Static Marker Collisions**: Players learn that markers can intercept cubes at any point in their descent
- **Collision Timing Windows**: Introduction to the concept of optimal collision timing
- **Basic Spatial Awareness**: Understanding collision points on the grid

#### Phase 2: Dynamic Collisions (Stages 3-6)
- **Moving Cube Markers**: Cube markers from Matrix captures can move and collide
- **Collision Prediction**: Learning to anticipate where collisions will occur
- **Mid-Flight Conversions**: Converting Infinity cubes by colliding Unit cubes into them

#### Phase 3: Advanced Collision Patterns (Stages 7-9)
- **Complex Formations**: Waves spawn in complex patterns requiring strategic marker placement
- **Collision Chains**: Setting up cascading collision sequences
- **Spatial-Temporal Mastery**: Balancing immediate needs with future collision setup

#### Phase 4: Advanced Orchestration (Stage 10-12)
- **Dynamic Collision Zones**: Collision points that move during waves
- **Pattern Recognition**: Identifying optimal marker placement for wave formations
- **Complete System Mastery**: All collision mechanics combined

## 4.6 Progression Structure


### Act 1: Learn the Rules (Stages 0-2)
**Focus**: Establishing core loop, Infinity cube avoidance, and Matrix cube discovery

### Tutorial Stage (Stage 0)
- **Grid**: Narrow width, standard depth
- **Wave Rows**: Minimal spawn area, expanding across waves
- **Tools**: Movement + Unit Markers (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity
- **Learning Goal**: Core fundamentals before the real game begins
- **Max Escapes**: Forgiving for learning

The tutorial stage teaches basic movement, marker placement, and capture mechanics in a safe environment. Guided highlight sequences provide step-by-step instruction, highlighting specific tiles and cubes to guide player actions. This is Stage 0 and is separate from the main campaign progression.

**Wave Progression**:
- **Early waves**: Basic movement and capture with guided highlight sequences; minimal Infinity presence
- **Middle waves**: Multiple Infinity cubes introduce danger awareness and strategic avoidance
- **Later waves**: Increased Infinity density teaches escape penalty mechanics and blocking concepts


**Focus**: Establishing core loop and primary danger

#### Stage 1: First Contact
- **Grid**: Narrow width, moderate depth
- **Wave Rows**: Shallow spawn area, expanding across waves
- **Tools**: Movement + Unit Markers (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity
- **Learning Goal**: Movement fundamentals and Infinity cube avoidance. Players learn that Infinity cubes are dangerous and uncapturable, and must be avoided. Infinity cubes block player movement and prevent access to Unit cubes behind them.
- **Max Escapes**: Moderate tolerance
- **Stage Grants**: Unit markers only

**Wave Progression**:
- **Early waves**: Dense Infinity navigation with balanced Unit capture opportunities
- **Later waves**: Expanded spawn area with heavy Infinity; final wave provides reward with many capture opportunities

#### Stage 2: Strategic Movement
- **Grid**: Narrow width, standard depth
- **Wave Rows**: Moderate spawn area, expanding across waves
- **Tools**: Movement + Unit Markers (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity
- **Learning Goal**: Master movement and Infinity avoidance. Players must navigate around Infinity cubes to reach Unit cubes. Learn the blocking power of Infinity cubes and strategic positioning.
- **Max Escapes**: Configurable
- **Stage Grants**: Unit markers only

**Wave Progression**:
- **Early waves**: Movement mastery with strategic positioning around Infinity obstacles
- **Later waves**: Expanded spawn area with complex Infinity blocking; **final wave introduces a single Matrix cube as discovery moment**

### Act 2: Matrix Cube Learning (Stages 3-4)
**Focus**: Learning Matrix cube behavior before Matrix markers are introduced

#### Stage 3: Matrix Cube Introduction
- **Grid**: Wider spawn area (grid expansion from previous stages)
- **Tools**: Unit Markers only
- **Cubes**: Unit + Matrix (early waves: no Infinity for isolated learning), then Unit + Matrix + Infinity
- **Learning Goal**: Understand Matrix cube behavior in isolation, then integrate with Infinity avoidance
- **Special**: Stricter escape requirements

**Wave Progression**:
- **Early waves**: Pure Matrix cube learning with no Infinity cubes (isolated mechanic introduction)
- **Later waves**: Matrix + Infinity integration, building to full complexity challenge

#### Stage 4: Matrix Cube Strategy
- **Grid**: Consistent with Stage 3 (wider spawn area)
- **Tools**: Unit Markers only
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Efficient Matrix cube capture prioritization, value hierarchy mastery
- **Special**: Stricter escape requirements; introduces face painting mechanics

**Wave Progression**:
- **Early waves**: Focused scenarios introducing specific mechanics (Matrix collisions, face painting)
- **Later waves**: Full complexity with Matrix cube prioritization amid Infinity obstacles; final wave provides reward with many capture opportunities

### Act 3: Matrix Marker Introduction (Stages 5-6)
**Focus**: Learning Matrix marker mechanics (Matrix cubes already mastered from Stages 3-4)

#### Stage 5: Matrix Marker Introduction
- **Grid**: Moderate width, increased depth
- **Wave Rows**: Moderate spawn density
- **Tools**: Unit Markers + **Matrix Markers** (introduced)
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Matrix markers enable area capture - learn efficient multi-cube captures

#### Stage 6: Matrix Marker Mastery
- **Grid**: Consistent with Stage 5
- **Wave Rows**: Increasing spawn density
- **Tools**: Unit + Matrix Markers
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Combine Matrix markers with Unit markers for complex solutions

### Act 4: Recursion Introduction (Stages 7-9)
**Focus**: Learning Recursion cube and Recursion marker mechanics

#### Stage 7: Recursion Discovery
- **Grid**: Second width expansion, deep grid
- **Wave Rows**: High spawn density
- **Tools**: Unit + Matrix + **Recursion Markers** (introduced)
- **Cubes**: Unit + Matrix + **Recursion** + Infinity
- **Learning Goal**: Recursion cubes require multiple hits; Recursion markers are essential

#### Stage 8: Recursion Strategy
- **Grid**: Consistent with Stage 7
- **Wave Rows**: High spawn density
- **Tools**: Unit + Matrix + Recursion Markers
- **Cubes**: Unit + Matrix + Recursion + Infinity
- **Learning Goal**: Multi-hit mechanics, durability management

#### Stage 9: Recursion Mastery
- **Grid**: Large grid (approaching end game scale)
- **Wave Rows**: High spawn density
- **Tools**: Unit + Matrix + Recursion Markers
- **Cubes**: Unit + Matrix + Recursion + Infinity
- **Learning Goal**: Combining Recursion with Matrix for complex solutions

### Act 5: Infinity Marker Introduction (Stage 10)
**Focus**: Player Infinity and resonance mechanics

#### Stage 10: Infinity Unlocked
- **Grid**: Approaching end game width and depth
- **Wave Rows**: High spawn density
- **Tools**: Unit + Matrix + Recursion + **Infinity Markers** (introduced)
- **Cubes**: All types
- **Learning Goal**: Player Infinity introduction, resonance mechanics, Infinity-first sequencing

### Act 6: Mastery Tests (Stages 11-12)
**Focus**: Full strategic depth and optimal play

#### Stage 11: Advanced Mastery
- **Grid**: End game width and depth
- **Wave Rows**: Maximum spawn density
- **Tools**: All marker types (balanced limits)
- **Cubes**: All types
- **Learning Goal**: Complex interactions, optimal resource usage, strategic depth

#### Stage 12: Ultimate Test
- **Grid**: Maximum dimensions (end game)
- **Wave Rows**: Maximum spawn density
- **Tools**: All marker types (precise allocations per wave)
- **Cubes**: All types
- **Learning Goal**: Complete mastery demonstration, all mechanics combined, perfect efficiency


## 4.7 Challenge and Bonus Stage Variations

### Challenge Stage Design Patterns

#### Resource Constraint Challenges
- **Limited Charges**: Severely restricted marker charges force perfect efficiency
- **No Regeneration**: Charges don't regenerate, requiring careful planning
- **Single-Use Tools**: Each marker type can only be used once per stage
- **Learning Goal**: Master resource optimization and strategic planning

#### Precision Timing Challenges
- **Tight Windows**: Very short timing windows for optimal marker placement
- **Perfect Timing Required**: Success depends on frame-perfect execution
- **No Margin for Error**: Mistakes immediately fail the challenge
- **Learning Goal**: Develop muscle memory and timing precision

#### Survival Challenges
- **Endless Waves**: Continuous waves until failure
- **Escalating Difficulty**: Each wave increases in complexity
- **Score-Based**: Success measured by survival time or cubes captured
- **Learning Goal**: Adapt to increasing pressure and maintain composure

#### Puzzle Challenges
- **Specific Solutions**: Stages with exact solution paths
- **Limited Options**: Very few valid strategies
- **Logic-Based**: Requires understanding of cube interactions
- **Learning Goal**: Deep understanding of game mechanics and interactions

### Bonus Stage Design Patterns

#### Speed Challenges
- **Time Limits**: Complete stages within time constraints
- **Fast Movement**: Cubes move at accelerated pace
- **Rapid Decision Making**: Quick strategic choices required
- **Reward**: Achievement or special recognition for completion

#### Puzzle Variations
- **Unique Rules**: Special mechanics not found in standard stages
- **Experimental Gameplay**: Test new concepts and interactions
- **Creative Solutions**: Multiple valid approaches encouraged
- **Reward**: Unlock special content or cosmetics

#### Special Rule Stages
- **Modified Mechanics**: Standard rules altered in interesting ways
- **Power-Ups**: Temporary advantages or special abilities
- **Environmental Effects**: Dynamic board states or special conditions
- **Reward**: Alternative progression paths or bonus content

## 4.8 Design Patterns

### Collision Learning Pattern
Progressive collision mechanic introduction:
1. **Static Collisions**: Fixed markers intercepting cubes
2. **Moving Collisions**: Cube markers with predictable movement
3. **Conversion Collisions**: Mid-flight Infinity cube conversions
4. **Chain Collisions**: Multiple collision cascades
5. **Symmetrical Collisions**: Perfect mirrored collision patterns

### Spatial-Temporal Trade-off Pattern
```
Complexity Progression:
Immediate Placement → Delayed Timing → Predictive Positioning → Chain Planning → Symmetrical Orchestration
```

### Collision Difficulty Metrics
- **Timing Precision**: Window size for successful collisions (measured in milliseconds)
- **Collision Count**: Number of simultaneous collisions required
- **Chain Length**: Sequential collisions needed for success
- **Symmetry Accuracy**: Deviation tolerance from perfect symmetry
- **Temporal Complexity**: Number of future states to consider

## 4.9 Wave Progression Mechanics

### Wave Timing and Pacing
Waves progress through configurable timing systems:

- **Movement Intervals**: Time between cube movement steps (typically 1.75 seconds)
- **Fast Movement Mode**: Accelerated timing for faster gameplay (typically 0.1 seconds)
- **Wave Start Delay**: Brief pause before wave begins (typically 0.75 seconds)
- **Speed Control**: Players can toggle between normal and fast movement modes by holding a key (typically Left Shift)

### Wave Lifecycle
Each wave follows a structured sequence:

1. **Wave Start**: After a brief delay, cubes spawn at their configured positions on the grid
2. **Movement Phase**: Cubes move forward one step at a time at regular intervals. Players can speed up this process
3. **Active Play**: Players place markers, capture cubes, and manage threats as cubes advance
4. **Completion Check**: The wave continuously monitors whether completion conditions are met
5. **Wave End**: When complete, the wave shows completion feedback and prepares for the next wave

### Wave Completion Conditions
Waves complete when:

- **All Capturable Cubes Processed**: All Unit, Matrix, and Recursion cubes have been either captured or have escaped. The wave ends when no capturable cubes remain active
- **Escape Limit Exceeded**: If a wave has an escape limit configured, exceeding that limit immediately fails the wave
- **Player Death**: If the player is killed by an Infinity cube, the wave fails immediately

### Wave-to-Wave Transitions
Smooth transitions between waves maintain gameplay flow:

- **Completion Messages**: After each wave, players see a progress message showing which wave they completed (e.g., "Wave 2/5") along with capture and escape statistics
- **Transition Delays**: A configurable pause occurs between waves, giving players time to process the previous wave's results
- **State Reset**: All markers are cleared from the grid, preparing a clean slate for the next wave
- **Resource Regeneration**: Marker charges continue regenerating between waves, so players start each new wave with refreshed resources
- **Player Reset**: The player remains in position, ready for the next wave's challenge

### Wave Success Criteria
Waves can have individual success criteria that differ from the stage's overall requirements:

- **Wave-Specific Escape Limits**: A wave can fail if too many cubes escape, even if the stage allows more escapes overall
- **Wave-Specific Capture Requirements**: A wave might require capturing a minimum number of cubes to succeed
- **Default Behavior**: If a wave doesn't specify its own criteria, it uses the stage's success conditions

### Wave Highlight Sequence System
Waves use guided highlight sequences to teach mechanics and provide context:

- **Highlight Sequences**: Each wave can define sequences that combine messages, visual highlights, and game pauses
- **Sequence Timing**: Sequences can trigger at wave start (move step 0), specific movement steps, or wave end
- **Event Triggers**: Sequences can trigger when markers are placed at specific positions or when cubes are captured
- **Visual Guidance**: Sequences can highlight specific tiles or cubes to guide player attention
- **Interactive Validation**: Sequences can pause the wave and require players to place markers at specific positions before continuing
- **Completion Feedback**: Wave completion shows progress (e.g., "Wave 2/5") and capture statistics

### Wave Event System
Waves trigger events that affect stage progression:

- **Wave Complete**: Successful wave completion advances to the next wave in the sequence
- **Wave Failed**: Failure triggers stage failure handling (restart or return to menu)
- **All Waves Complete**: When the final wave in a stage completes, the stage is marked as successful
- **Audio Feedback**: Wave events trigger appropriate audio cues to enhance player feedback

## 4.10 Current Development Priorities

### ✅ Completed Foundation Systems (June 2025)
- **Four-Tier Marker System**: Unit/Matrix/Recursion/Infinity markers fully implemented and integrated
- **Face Painting Integration**: Connected to stage progression with rotation tracking
- **Tile State System**: Corrupted/Enhanced tile mechanics operational
- **Cube Type Diversity**: Unit/Matrix/Infinity/Recursion cube system with multi-hit mechanics
- **Technical Infrastructure**: Debug systems, analytics, and integration testing complete

### 🔄 Phase 2 Active Priorities (July-August 2025)
1. **Cosmic Lo-fi Audio System** ⭐ **HIGHEST PRIORITY** - Currently in progress
   - Meditative marker placement tones
   - Harmonic capture feedback
   - Ambient cosmic soundscape
   - Rhythmic wave progression audio
   - Collision resonance for marker to cube interactions

2. **UI Modernization & Polish**
   - OnGUI → Unity UI conversion for stage interface
   - Enhanced visual feedback for four-tier marker system
   - Improved charge indicators and cooldown displays
   - Modern stage completion/failure screens

3. **Stage Design Enhancement**
   - Leverage completed four-tier system for advanced stage concepts
   - Implement marker to cube system progression (Acts 4-5)
   - Create stages showcasing marker-to-cube transformation
   - Design collision-based puzzles and timing challenges
   - Integrate infinity symbol theme into level geometry

### Stage Design Focus Areas
- **Four-Tier Mastery Stages**: Dedicated levels teaching optimal marker type selection
- **Recursion Cube Scenarios**: Strategic multi-hit encounters requiring Recursion Markers
- **Matrix Marker Techniques**: Stages emphasizing precision timing and positioning
- **Cube Marker Tactics**: Advanced detonation strategy implementation
- **Marker to Cube Training**: Stages introducing marker transformation and collision mechanics
- **Infinity Bypass Puzzles**: Scenarios requiring Unit cube conversion tactics
- **Pattern Mirroring Challenges**: Complex waves requiring perfect symmetrical responses
- **Cosmic Lo-fi Experience**: Stage pacing synchronized with meditative audio feedback

### Testing and Iteration Focus
- **Four-Tier System Validation**: Ensure all marker types feel distinct and valuable
- **Audio Integration Testing**: Verify sound enhances rather than distracts from strategy
- **Visual Polish Assessment**: Confirm UI updates improve clarity and game feel
- **Performance Optimization**: Maintain 60 FPS with enhanced audio-visual systems

---
**Last Updated**: January 18, 2026  
**Document Type**: Project Design Document  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Game Overview](2_GameOverview.md)
- Technical Documentation (see Technical Doc folder)