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
| **0-1** | Unit + Infinity (Matrix in Stage 1 Wave 4) | Unit only | Learn basics: movement, marker placement, Infinity cube avoidance, Matrix cube discovery |
| **2** | Unit + Matrix + Infinity | Unit only | Matrix cube strategy: value hierarchy and prioritization |
| **3-6** | Unit + Matrix + Infinity | Unit + Matrix | Add Matrix marker: area capture, strategic positioning |
| **7-9** | Unit + Matrix + Recursion + Infinity | Unit + Matrix + Recursion | Add Recursion cube and Recursion marker: multi-hit mechanics, durability |
| **10** | All types | Unit + Matrix + Recursion + Infinity | Add Infinity marker: player Infinity introduction, resonance mechanics |
| **11-12** | All types | All types | Mastery tests: full strategic depth, complex interactions, optimal play |

### Detailed Stage Progression

#### Stages 0-1: Learn Basics (Unit + Infinity, Matrix Discovery)
- **Stage 0 (Tutorial)**: Pure fundamentals - movement, marker placement, basic capture
- **Stage 1**: Infinity cube introduction - learn they're dangerous and uncapturable. Matrix cubes introduced in final wave as capturable alternative.
- **Grid Progression**: Width 5, rows start at 2 (Stage 0) and increase to 3-4 (Stage 1)

#### Stage 2: Matrix Cube Strategy (Unit + Matrix + Infinity)
- **Stage 2**: Matrix cube strategy - prioritize Matrix cubes over Unit cubes; understand value hierarchy (Infinity = avoid, Matrix = pursue, Unit = capture when convenient)
- **Grid Progression**: Width 5, rows at 3

#### Stages 3-6: Matrix Marker Introduction and Mastery
- **Stage 3**: Matrix marker introduction - area capture mechanics (Matrix cubes already known from Stage 1)
- **Stage 4**: Matrix marker strategy - efficient marker usage, resource management
- **Stage 5**: Matrix marker mastery - optimal positioning and timing
- **Stage 6**: Matrix integration - combine Matrix markers with Unit markers for complex solutions
- **Grid Progression**: Width gradually increases from 5 to 7

#### Stages 7-9: Recursion Introduction (Add Recursion Cube + Recursion Marker)
- **Stage 7**: Recursion cube introduction - multi-hit mechanics
- **Stage 8**: Recursion marker introduction - durability management
- **Stage 9**: Recursion strategy - combining with Matrix for complex solutions
- **Grid Progression**: Width increases from 7 to 9

#### Stage 10: Infinity Marker Introduction
- **Stage 10**: Infinity marker unlocked - player Infinity introduction, resonance mechanics
- **Grid Progression**: Width 9-11

#### Stages 11-12: Mastery Tests
- **Stage 11**: Advanced mastery - complex interactions, optimal resource usage
- **Stage 12**: Ultimate test - full strategic depth, all mechanics combined
- **Grid Progression**: Width 11-13 (end game range)

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
- **Stages 0-1**: Width 5 (focused encounters, limited space)
- **Stages 2-3**: Width 5-6 (gradual expansion)
- **Stages 4-6**: Width 6-7 (medium grids, balanced gameplay)
- **Stages 7-9**: Width 7-9 (larger grids, more strategy)
- **Stage 10**: Width 9-11 (wide grids, complex scenarios)
- **Stages 11-12**: Width 11-13 (end game, maximum complexity)

#### Grid Height Progression
- **Early Stages (0-2)**: Height 15-20 (manageable depth)
- **Mid Stages (3-6)**: Height 20-25 (increased planning depth)
- **Late Stages (7-10)**: Height 25-30 (complex forward planning)
- **End Game (11-12)**: Height 30-35+ (maximum strategic depth)

#### Row Count Progression (Wave Spawn Area)
- **Stage 0**: Starts at 2 rows, progresses to 3 rows across waves
- **Stage 1**: 2-3 rows (maintains learning curve)
- **Stages 2-3**: 3 rows (increased density)
- **Stages 4-6**: 3-5 rows (medium complexity)
- **Stages 7-9**: 5-7 rows (high density)
- **Stages 10-12**: 7+ rows (maximum density, mastery tests)

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
- **Grid**: 5x20 (standard starting size)
- **Wave Rows**: Starts at 2 rows, progresses to 3 rows
- **Resources**: Unit markers only (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity (introduces both capturable and dangerous cubes)
- **Learning Goals**: 
  - Grid navigation and positioning
  - Marker placement timing
  - Basic capture mechanics (Unit cubes)
  - Infinity cube danger recognition and avoidance
  - Foundation for all future gameplay

This is the only dedicated tutorial stage (Stage 0). Stages 1-12 are the core game stages that progressively introduce new mechanics while building on tutorial fundamentals.

### Symmetrical Wave System Tutorial Progression

After basic mechanics are established, the game introduces its core innovation: the Symmetrical Wave System. This system teaches players to think about collisions, timing, and spatial relationships.

#### Phase 1: Collision Fundamentals (Stages 0-2)
- **Static Marker Collisions**: Players learn that markers can intercept cubes at any point in their descent
- **Collision Timing Windows**: Introduction to the concept of optimal collision timing
- **Basic Spatial Awareness**: Understanding collision points on the grid

#### Phase 2: Dynamic Collisions (Stages 3-6)
- **Moving Cube Markers**: Cube markers from Matrix captures can move and collide
- **Collision Prediction**: Learning to anticipate where collisions will occur
- **Mid-Flight Conversions**: Converting Infinity cubes by colliding Unit cubes into them

#### Phase 3: Symmetrical Patterns (Stages 7-9)
- **Mirror Mechanics**: Waves spawn in symmetrical patterns requiring matching responses
- **Collision Chains**: Setting up cascading collision sequences
- **Spatial-Temporal Mastery**: Balancing immediate needs with future collision setup

#### Phase 4: Advanced Orchestration (Stage 10-12)
- **Dynamic Collision Zones**: Collision points that move during waves
- **Infinity Patterns**: Waves forming infinity symbol shapes
- **Complete System Mastery**: All collision mechanics combined

## 4.6 Progression Structure


### Act 1: Learn the Rules (Stages 0-2)
**Focus**: Establishing core loop, Infinity cube avoidance, and Matrix cube discovery

### Tutorial Stage (Stage 0)
- **Grid**: 5x20 (width 5)
- **Wave Rows**: 2-3 rows
- **Tools**: Movement + Unit Markers (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity
- **Learning Goal**: Core fundamentals before the real game begins


The tutorial stage teaches basic movement, marker placement, and capture mechanics in a safe environment. Guided highlight sequences provide step-by-step instruction, highlighting specific tiles and cubes to guide player actions. This is Stage 0 and is separate from the main campaign progression.

**Wave Progression** (based on existing wave data):
- Wave 0_01: 2 rows, mix of Unit and one Infinity cubes
- Wave 0_02: 2 rows, 2 infinity cube density
- Wave 0_03: 3 rows, 3 infinity cubes that block 5 unit cubes total


**Focus**: Establishing core loop and primary danger

#### Stage 1: First Contact
- **Grid**: 5x25 (width 5, standard starting size)
- **Wave Spawn Area**: Minimum 5 width x 3 height (rows)
- **Tools**: Movement + Unit Markers (infinite with move-based regeneration)
- **Cubes**: Unit + Infinity (Matrix introduced in Wave 1_04)
- **Learning Goal**: Infinity cubes are dangerous and uncapturable; learn to avoid them. Matrix cubes introduced as capturable alternative.

**Wave Progression**:
- Wave 1_01: 3 rows, gentle introduction - mix of Unit and Infinity cubes with clear capture paths
- Wave 1_02: 3 rows, increased challenge - more Infinity cubes, tighter spacing, requires careful positioning
- Wave 1_03: 4 rows, the blocking problem - Infinity cubes strategically positioned to block multiple Unit cubes, creating frustration and demonstrating the limitation of Unit-only strategies
- Wave 1_04: 4 rows, Matrix solution - Matrix cubes introduced to clear blocked paths, demonstrating their utility for accessing trapped Unit cubes 

#### Stage 2: Strategic Choice
- **Grid**: 5x20 (width 5, maintaining focused learning)
- **Wave Rows**: 3 rows (increased density)
- **Tools**: Movement + Unit Markers
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Matrix cube strategy - prioritize Matrix cubes over Unit cubes; understand value hierarchy (Infinity = avoid, Matrix = pursue, Unit = capture when convenient)

**Wave Progression**:
- Wave 2_01: 3 rows, balanced mix with Matrix cubes - learn Matrix cube value
- Wave 2_02: 3 rows, increased Matrix cube density - practice Matrix prioritization
- Wave 2_03: 3 rows, strategic Matrix placement - test understanding of value hierarchy

### Act 2: Matrix Marker Introduction (Stages 3-6)
**Focus**: Learning Matrix marker mechanics (Matrix cubes already introduced in Stage 1)

#### Stage 3: Matrix Discovery
- **Grid**: 5x20 (width 5, transitioning to 6)
- **Wave Rows**: 3-4 rows
- **Tools**: Unit Markers + **Matrix Markers** (introduced)
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Matrix markers enable area capture - learn to use Matrix markers for efficient multi-cube captures

#### Stage 4: Matrix Strategy
- **Grid**: 6x22 (width 6, expanded)
- **Wave Rows**: 4 rows
- **Tools**: Unit + Matrix Markers
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Efficient Matrix marker usage, resource management

#### Stage 5: Matrix Mastery
- **Grid**: 6-7x25 (width expanding)
- **Wave Rows**: 4-5 rows
- **Tools**: Unit + Matrix Markers
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Optimal Matrix positioning and timing

#### Stage 6: Matrix Integration
- **Grid**: 7x25 (width 7, medium grid)
- **Wave Rows**: 5 rows
- **Tools**: Unit + Matrix Markers
- **Cubes**: Unit + Matrix + Infinity
- **Learning Goal**: Combine Matrix with Unit markers for complex solutions

### Act 3: Recursion Introduction (Stages 7-9)
**Focus**: Learning Recursion cube and Recursion marker mechanics

#### Stage 7: Recursion Discovery
- **Grid**: 7-8x28 (width expanding)
- **Wave Rows**: 5 rows
- **Tools**: Unit + Matrix + **Recursion Markers** (introduced)
- **Cubes**: Unit + Matrix + **Recursion** + Infinity
- **Learning Goal**: Recursion cubes require multiple hits; Recursion markers are essential

#### Stage 8: Recursion Strategy
- **Grid**: 8x30 (width 8)
- **Wave Rows**: 5-6 rows
- **Tools**: Unit + Matrix + Recursion Markers
- **Cubes**: Unit + Matrix + Recursion + Infinity
- **Learning Goal**: Multi-hit mechanics, durability management

#### Stage 9: Recursion Mastery
- **Grid**: 9x32 (width 9, large grid)
- **Wave Rows**: 6-7 rows
- **Tools**: Unit + Matrix + Recursion Markers
- **Cubes**: Unit + Matrix + Recursion + Infinity
- **Learning Goal**: Combining Recursion with Matrix for complex solutions

### Act 4: Infinity Marker Introduction (Stage 10)
**Focus**: Player Infinity and resonance mechanics

#### Stage 10: Infinity Unlocked
- **Grid**: 9-11x35 (width expanding to end game range)
- **Wave Rows**: 7 rows
- **Tools**: Unit + Matrix + Recursion + **Infinity Markers** (introduced)
- **Cubes**: All types
- **Learning Goal**: Player Infinity introduction, resonance mechanics, Infinity-first sequencing

### Act 5: Mastery Tests (Stages 11-12)
**Focus**: Full strategic depth and optimal play

#### Stage 11: Advanced Mastery
- **Grid**: 11x38 (width 11, end game)
- **Wave Rows**: 7+ rows
- **Tools**: All marker types (balanced limits)
- **Cubes**: All types
- **Learning Goal**: Complex interactions, optimal resource usage, strategic depth

#### Stage 12: Ultimate Test
- **Grid**: 11-13x40+ (width 11-13, maximum end game)
- **Wave Rows**: 7+ rows
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
- **Four-Tier Marker System**: Light/Heavy/Matrix/Cube markers fully implemented and integrated
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
   - Collision resonance for symmetrical waves

2. **UI Modernization & Polish**
   - OnGUI → Unity UI conversion for stage interface
   - Enhanced visual feedback for four-tier marker system
   - Improved charge indicators and cooldown displays
   - Modern stage completion/failure screens

3. **Stage Design Enhancement**
   - Leverage completed four-tier system for advanced stage concepts
   - Implement Symmetrical Wave System progression (Acts 4-5)
   - Create stages showcasing marker-to-cube transformation
   - Design collision-based puzzles and timing challenges
   - Integrate infinity symbol theme into level geometry

### Stage Design Focus Areas
- **Four-Tier Mastery Stages**: Dedicated levels teaching optimal marker type selection
- **Recursion Cube Scenarios**: Strategic multi-hit encounters requiring Recursion Markers
- **Matrix Marker Techniques**: Stages emphasizing precision timing and positioning
- **Cube Marker Tactics**: Advanced detonation strategy implementation
- **Symmetrical Wave Training**: Stages introducing moving cube markers and collision mechanics
- **Infinity Bypass Puzzles**: Scenarios requiring Unit cube conversion tactics
- **Pattern Mirroring Challenges**: Complex waves requiring perfect symmetrical responses
- **Cosmic Lo-fi Experience**: Stage pacing synchronized with meditative audio feedback

### Testing and Iteration Focus
- **Four-Tier System Validation**: Ensure all marker types feel distinct and valuable
- **Audio Integration Testing**: Verify sound enhances rather than distracts from strategy
- **Visual Polish Assessment**: Confirm UI updates improve clarity and game feel
- **Performance Optimization**: Maintain 60 FPS with enhanced audio-visual systems

---
**Last Updated**: December 14, 2025  
**Document Type**: Project Design Document  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Game Overview](2_GameOverview.md)
- Technical Documentation (see Technical Doc folder)