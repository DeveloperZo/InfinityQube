# Clarifying Questions for InfinityQube Development

> This document compiles all implementation ambiguities and design questions discovered during documentation updates. Questions are organized by system, priority, and impact on gameplay.

**Purpose**: Guide future development decisions by identifying areas requiring design team clarification or implementation decisions.

**Last Updated**: November 16, 2024

---

## Table of Contents

1. [Paint System Questions](#paint-system-questions)
2. [Marker System Questions](#marker-system-questions)
3. [Cube System Questions](#cube-system-questions)
4. [Progression System Questions](#progression-system-questions)
5. [Visual Design Questions](#visual-design-questions)
6. [Action Items](#action-items)

---

## Paint System Questions

### Critical Priority

#### Q1: Recursive Paint Behavior
**Question**: What is the intended behavior of "Recursive Paint"? Is this a distinct paint type, or does it refer to paint spreading from cube faces to tiles and back to cubes?

**Context**: 
- Face painting system supports Corrupted and Enhanced statuses
- Tiles can paint cube faces when cubes land on them
- Infinity cubes with Corrupted faces can corrupt tiles
- Code reference: `Tile.cs:446-448` - Infinity cubes with painted faces corrupt tiles

**Impact**: High - Affects core gameplay mechanics and strategic depth

**Suggested Implementation**:
- Option A: Recursive Paint is a chain reaction where painted cubes paint tiles, which paint other cubes
- Option B: Recursive Paint is a specific paint type that spreads to adjacent tiles/cubes
- Option C: Recursive Paint refers to the existing corruption spread mechanic

**Action Required**: Design decision on recursive paint definition and mechanics

---

#### Q2: Paint Duration System Rules
**Question**: What are the complete rules for paint duration? When should paint be temporary vs permanent? How do durations interact with cube rotation?

**Context**:
- Paint duration: `-1` = permanent, `>0` = temporary (decrements per move step)
- Code reference: `CubeManager.cs:675-722` - Paint duration processing
- Code reference: `Tile.cs:24` - Tile paint duration default is 3 moves
- Enhanced faces are described as "typically temporary" but no clear rules

**Impact**: High - Affects strategic planning and resource management

**Current Implementation**:
- Face paint durations decrement each move step
- Duration `-1` means permanent paint
- Duration `0` clears paint status
- Tiles have default duration of 3 moves

**Unclear Areas**:
1. Should Enhanced faces always be temporary, or can they be permanent?
2. Should Corrupted faces from Infinity cubes be permanent or temporary?
3. How does cube rotation affect duration tracking?
4. Should tile paint duration differ from cube face paint duration?

**Suggested Implementation**:
- Enhanced faces: Temporary by default (3-5 moves), but allow permanent for special scenarios
- Corrupted faces from Infinity cubes: Permanent until cleansed
- Cube rotation: Duration tracks per-face, not per-cube
- Tile paint: Separate duration system, defaults to 3 moves

**Action Required**: Define duration rules for each paint type and interaction scenario

---

### Important Priority

#### Q3: Corruption Spread Rules
**Question**: What are the complete rules for corruption spread? How does corruption propagate from Infinity cubes to tiles to other cubes?

**Context**:
- Code reference: `Tile.cs:446-464` - Corruption mechanics
- Infinity cubes with Corrupted faces corrupt tiles on landing
- Corrupted tiles paint cube faces when cubes land on them
- Corruption can be cleansed by interaction limits or duration
- Code reference: `TileCorruption.cs:84-99` - Corruption decay system

**Impact**: Medium-High - Affects strategic positioning and risk management

**Current Implementation**:
- Infinity cubes with Corrupted faces → corrupt tiles (duration: 5 moves, max 3 interactions)
- Corrupted tiles → paint cube faces (permanent paint, `-1` duration)
- Corruption cleanses after duration expires OR after 3 cube interactions
- Corruption countdown visible if `showCorruptionCountdown` enabled

**Unclear Areas**:
1. Can corruption spread to adjacent tiles, or only through cube movement?
2. What happens when a Corrupted cube moves to a normal tile?
3. Can Enhanced faces prevent corruption spread?
4. Should corruption have a visual "spread" animation?

**Suggested Implementation**:
- Corruption spreads only through cube movement (no adjacent tile spread)
- Corrupted cubes paint tiles they land on
- Enhanced faces do not prevent corruption (separate systems)
- Visual spread animation optional but recommended for clarity

**Action Required**: Clarify corruption propagation rules and visual feedback

---

#### Q4: Paint Duration Visual Distinction
**Question**: How should temporary vs permanent face status be visually distinguished to players?

**Context**:
- Code reference: `2_GameOverview.md:195` - Question about paint duration visuals
- Current system tracks duration but visual distinction unclear
- Players need to know if paint will expire or persist

**Impact**: Medium - Affects player understanding and strategic planning

**Suggested Implementation**:
- Option A: Permanent paint = solid color, temporary = pulsing/glowing effect
- Option B: Permanent paint = full opacity, temporary = reduced opacity with countdown
- Option C: Permanent paint = distinct border/outline, temporary = no border
- Option D: Visual indicator showing remaining duration (number or progress bar)

**Action Required**: Design decision on visual distinction method

---

## Marker System Questions

### Critical Priority

#### Q5: Recursion Marker Interaction with Recursion Cubes
**Question**: What are the exact mechanics for Recursion Markers capturing Recursion cubes? How many hits required? Is there visual feedback for partial damage?

**Context**:
- Code reference: `2_GameOverview.md:182` - Recursion Marker mechanics question
- Code reference: `3_GameplayMechanics.md:172-176` - Recursion Marker reduces Recursion hits from 3-4 to 1-2
- Recursion Markers are "optimized for Recursion cube capture"
- Code reference: `3_GameplayMechanics.md:98-105` - Recursion Marker description

**Impact**: High - Core mechanic for Recursion cube management

**Current Implementation**:
- Standard markers require 3-4 hits for Recursion cube capture
- Recursion Markers reduce requirement to 1-2 hits
- Optimal placement can achieve single-detonation capture

**Unclear Areas**:
1. Exact hit count reduction: Is it always 1-2 hits, or variable based on placement?
2. Visual feedback: Should Recursion cubes show damage state (cracks, color change)?
3. Partial damage: Do Recursion Markers deal partial damage if cube doesn't die?
4. Stacking: Can multiple Recursion Markers stack damage on same Recursion cube?

**Suggested Implementation**:
- Recursion Markers reduce Recursion cube hits from 3-4 to 1-2 (exact value TBD)
- Visual feedback: Recursion cubes show damage state (cracks or color fade)
- Partial damage: Recursion Markers deal 2x damage, so 2 hits = 4 damage (kills 3-hit Recursion)
- Stacking: Multiple Recursion Markers can stack, but optimal is single well-placed marker

**Action Required**: Define exact hit counts and visual feedback system

---

#### Q6: Cube Marker Activation and Mechanics
**Question**: How are Cube markers activated after generation? What are the complete mechanics for Cube marker usage?

**Context**:
- Code reference: `2_GameOverview.md:184` - Cube marker activation question
- Code reference: `3_GameplayMechanics.md:116-144` - Cube marker generation and usage
- Cube markers generated exclusively from Prime cube captures
- Code reference: `3_GameplayMechanics.md:125-144` - Cube marker generation process

**Impact**: High - Core mechanic for Prime cube value

**Current Implementation**:
- Cube markers generated when Prime cubes are captured
- Q key triggers next cube marker from FIFO queue
- Cube markers provide 3x3 area detonation
- No placement phase - appear where Prime cube was captured
- No cooldowns - can be triggered immediately

**Unclear Areas**:
1. Activation method: Q key confirmed, but is there visual queue indicator?
2. Queue management: What happens if multiple cube markers are queued?
3. Power scaling: How does marker type that captured Prime affect cube marker power?
4. Visual feedback: How do players see available cube markers?

**Suggested Implementation**:
- Q key activation confirmed
- Visual queue indicator showing number of available cube markers
- FIFO queue: First generated = first triggered
- Power scaling: Cube marker inherits power from capturing marker type
- Visual feedback: UI indicator showing cube marker count and queue position

**Action Required**: Implement visual feedback and confirm queue mechanics

---

### Important Priority

#### Q7: Marker Overlap and Amplification
**Question**: Should overlapping markers amplify effects, or do they operate independently?

**Context**:
- Code reference: `Brainstorming.md:28-33` - Overlapping marker amplification idea (rating: 5/10)
- Current system: Markers operate independently
- Question: Should overlapping markers create combined effects?

**Impact**: Medium - Could significantly change strategic depth

**Current Implementation**:
- Markers operate independently
- Each marker triggers separately when cube enters tile
- No amplification or stacking effects

**Suggested Implementation**:
- Keep independent operation (current implementation)
- Overlapping markers = multiple separate detonations
- No amplification to maintain clarity and balance

**Action Required**: Confirm independent operation (likely already decided)

---

## Cube System Questions

### Critical Priority

#### Q8: Recursion Cube Visual Representation
**Question**: What should the visual representation be for Recursion cubes? Material, color, and special effects.

**Context**:
- Code reference: `2_GameOverview.md:180,193` - Recursion cube visuals questions
- Code reference: `2_GameOverview.md:79` - Material TBD for Recursion cubes
- Code supports Recursion cubes but visual representation undefined
- Other cube types: Gray (Unit), Blue (Prime), Black (Infinity)

**Impact**: High - Affects player recognition and gameplay clarity

**Current Implementation**:
- Recursion cubes functionally implemented
- Visual representation not defined
- Code reference: `Enumerations.cs` - CubeType enum includes Recursion

**Suggested Implementation**:
- Option A: Unique color (e.g., Purple, Orange, Green)
- Option B: Special texture/material (metallic, crystalline, etc.)
- Option C: Visual effects (glow, particles, outline)
- Option D: Combination of color + effects

**Action Required**: Design decision on Recursion cube appearance

---

#### Q9: Face Rotation Rules During Movement
**Question**: When cubes move forward, do they rotate (tumble)? If so, which face becomes the new bottom face?

**Context**:
- Code reference: `2_GameOverview.md:186` - Face rotation rules question
- Code reference: `2_GameOverview.md:175` - Face rotation mechanics need clarification
- System tracks faces but rotation during movement unclear
- Code reference: `CubeManager.cs:693-697` - `GetCurrentDownFace()` method exists

**Impact**: High - Affects face painting strategy and cube behavior

**Current Implementation**:
- `GetCurrentDownFace()` method exists
- Face painting system tracks 4 faces
- Rotation mechanics during movement not clearly defined

**Unclear Areas**:
1. Do cubes rotate when moving forward?
2. If rotating, what is the rotation pattern (90° per step, random, sequential)?
3. How does rotation affect face status activation?
4. Should rotation be visual (cube tumbles) or just logical (face tracking)?

**Suggested Implementation**:
- Option A: Cubes rotate 90° forward each step (tumbling motion)
- Option B: Cubes rotate randomly each step
- Option C: Cubes don't rotate, face status only matters on initial placement
- Option D: Rotation only occurs when cube changes direction (not applicable - forward only)

**Recommended**: Option A - Sequential 90° forward rotation creates predictable face activation patterns

**Action Required**: Define rotation rules and implement visual feedback

---

### Important Priority

#### Q10: Detonation Chain Reaction Mechanics
**Question**: How do detonations propagate? What determines the size and pattern of chain reactions?

**Context**:
- Code reference: `2_GameOverview.md:188` - Detonation chains question
- Code reference: `2_GameOverview.md:173` - Detonation system referenced but mechanics unclear
- Code reference: `3_GameplayMechanics.md:380-417` - Detonation system description
- Enhanced faces create detonations when captured

**Impact**: Medium-High - Affects strategic depth and combo potential

**Current Implementation**:
- Detonation types: Large/Standard (3x3), Small (2x2), Single (1x1)
- Enhanced faces create detonations when captured
- Prime cube captures generate cube markers (3x3 area)
- Code reference: `3_GameplayMechanics.md:391-417` - Detonation flow

**Unclear Areas**:
1. Can detonations trigger other detonations (chain reactions)?
2. Do Enhanced face detonations trigger nearby Enhanced faces?
3. What is the maximum chain reaction size?
4. Should chain reactions have visual/audio feedback?

**Suggested Implementation**:
- Detonations can trigger nearby Enhanced faces (chain reactions)
- Chain reaction limit: 3-5 steps to prevent infinite loops
- Visual feedback: Sequential detonation animations
- Audio feedback: Escalating sound for chain reactions

**Action Required**: Define chain reaction rules and implement mechanics

---

## Progression System Questions

### Important Priority

#### Q11: Wave Failure Recovery
**Question**: When a wave fails due to escapes, does the player retry the same wave or continue with penalties?

**Context**:
- Code reference: `2_GameOverview.md:198` - Wave failure recovery question
- Code reference: `4_LevelDesign.md:85` - Failure handling configuration
- Waves can fail due to escape limits or player death
- Code reference: `3_GameplayMechanics.md:449-450` - Wave completion conditions

**Impact**: Medium - Affects difficulty curve and player experience

**Current Implementation**:
- Waves can be configured to restart on failure or return to menu
- Escape limits can be set per wave
- Player death immediately fails wave

**Unclear Areas**:
1. Should failed waves be retried automatically or manually?
2. Should there be penalties for wave failure (resource loss, difficulty increase)?
3. Should escape limits be cumulative across waves or per-wave?
4. How many retries should players have?

**Suggested Implementation**:
- Tutorial stages: Automatic retry with no penalties
- Standard stages: Manual retry option, no penalties
- Challenge stages: Limited retries or penalties
- Escape limits: Per-wave (not cumulative)

**Action Required**: Define failure recovery rules per stage type

---

#### Q12: Resource Regeneration Rules
**Question**: Are marker charges regenerated between waves, over time, or through specific actions?

**Context**:
- Code reference: `2_GameOverview.md:200` - Resource regeneration question
- Code reference: `3_GameplayMechanics.md:435-461` - Resource regeneration system
- Code reference: `4_LevelDesign.md:458` - Resource regeneration between waves

**Impact**: Medium - Affects strategic planning and resource management

**Current Implementation**:
- Unit Markers: Automatic regeneration after cooldown
- Recursion Markers: Automatic regeneration after cooldown (longer than Light)
- Prime markers: Automatic regeneration after cooldown (longest)
- Charges regenerate between waves
- Code reference: `3_GameplayMechanics.md:440-455` - Regeneration per marker type

**Unclear Areas**:
1. Should regeneration pause during active waves?
2. Should regeneration speed vary by difficulty?
3. Can players influence regeneration through actions?
4. Should there be maximum charge limits per wave?

**Suggested Implementation**:
- Regeneration continues during waves (current implementation)
- Regeneration speed constant (no difficulty scaling)
- No player influence on regeneration (automatic only)
- Maximum charges per wave configurable per stage

**Action Required**: Confirm regeneration rules match design intent

---

#### Q13: Difficulty Scaling Methodology
**Question**: How should wave complexity increase? More cube types, faster movement, or more complex patterns?

**Context**:
- Code reference: `2_GameOverview.md:202` - Difficulty scaling question
- Code reference: `4_LevelDesign.md:420-425` - Difficulty curve principles
- Progressive complexity across acts and stages

**Impact**: Medium - Affects learning curve and player engagement

**Current Implementation**:
- Stages progress from simple to complex
- Grid sizes increase (5x20 → 7x25 → 9x30+)
- Cube types introduced gradually
- Resource constraints increase in later stages

**Unclear Areas**:
1. Primary scaling method: Cube density, speed, or complexity?
2. Should all stages scale uniformly, or vary by stage type?
3. How should Recursion cubes be introduced in difficulty scaling?
4. Should difficulty scale within a single stage across waves?

**Suggested Implementation**:
- Primary scaling: Cube density and type variety
- Secondary scaling: Grid size and resource constraints
- Movement speed: Constant (player controls with fast mode)
- Within-stage scaling: Gradual increase across waves

**Action Required**: Define scaling methodology and implement progression curves

---

## Visual Design Questions

### Important Priority

#### Q14: Default Grid Dimensions
**Question**: What are the intended default grid dimensions (X by Y)? Current implementation is configurable but needs baseline.

**Context**:
- Code reference: `2_GameOverview.md:191` - Grid dimensions question
- Code reference: `4_LevelDesign.md:132-145` - Grid configuration examples
- Grid dimensions vary per stage

**Impact**: Low-Medium - Affects level design consistency

**Current Implementation**:
- Small grids: 5x20 (tutorial stages)
- Medium grids: 7x25 (standard stages)
- Large grids: 9x30+ (advanced stages)
- Configurable per stage

**Suggested Implementation**:
- Default: 7x25 (medium grid)
- Tutorial: 5x20 (small grid)
- Advanced: 9x30+ (large grid)
- Maintain current configurable system

**Action Required**: Confirm default dimensions match design intent

---

## Action Items

### Immediate Priority (Critical Questions)

1. **Define Recursive Paint Behavior** (Q1)
   - Determine if recursive paint is a distinct mechanic or refers to existing corruption spread
   - Document complete recursive paint rules if it's a new system
   - **Owner**: Design Team
   - **Timeline**: Before implementing advanced paint mechanics

2. **Establish Paint Duration Rules** (Q2)
   - Define duration rules for each paint type (Enhanced, Corrupted)
   - Clarify interaction between cube rotation and duration tracking
   - Document duration defaults and exceptions
   - **Owner**: Design Team + Technical Lead
   - **Timeline**: Before finalizing face painting system

3. **Clarify Recursion Marker Recursion Mechanics** (Q5)
   - Define exact hit count reduction (1-2 hits confirmed?)
   - Design visual feedback system for partial damage
   - Implement damage state visualization
   - **Owner**: Design Team + Art Team
   - **Timeline**: Before Recursion cube stage implementation

4. **Define Recursion Cube Visuals** (Q8)
   - Choose color, material, and effects for Recursion cubes
   - Create visual assets matching chosen design
   - Ensure visual distinction from other cube types
   - **Owner**: Art Team + Design Team
   - **Timeline**: Before Recursion cube stage implementation

5. **Establish Face Rotation Rules** (Q9)
   - Define rotation pattern during cube movement
   - Implement visual rotation feedback
   - Document rotation impact on face status activation
   - **Owner**: Technical Lead + Design Team
   - **Timeline**: Before advanced face painting stages

### High Priority (Important Questions)

6. **Clarify Corruption Spread Rules** (Q3)
   - Document complete corruption propagation mechanics
   - Design visual spread animation (if applicable)
   - Implement corruption interaction limits
   - **Owner**: Design Team
   - **Timeline**: Before corruption-focused stages

7. **Design Paint Duration Visuals** (Q4)
   - Choose visual distinction method (pulsing, opacity, border, countdown)
   - Implement visual feedback system
   - Test clarity with players
   - **Owner**: Art Team + Design Team
   - **Timeline**: Before finalizing face painting UI

8. **Implement Cube Marker Visual Feedback** (Q6)
   - Design queue indicator UI
   - Implement visual queue display
   - Test queue management clarity
   - **Owner**: UI Team + Design Team
   - **Timeline**: Before Prime cube stages

9. **Define Detonation Chain Rules** (Q10)
   - Establish chain reaction mechanics
   - Implement chain reaction limits
   - Design visual/audio feedback for chains
   - **Owner**: Design Team + Technical Lead
   - **Timeline**: Before chain reaction stages

### Medium Priority (Nice-to-Have Questions)

10. **Define Wave Failure Recovery** (Q11)
    - Establish retry rules per stage type
    - Implement failure handling system
    - Test player experience with different recovery methods
    - **Owner**: Design Team
    - **Timeline**: Before challenge stage implementation

11. **Confirm Resource Regeneration Rules** (Q12)
    - Verify regeneration rules match design intent
    - Document regeneration behavior per marker type
    - Test regeneration timing and balance
    - **Owner**: Design Team
    - **Timeline**: Before finalizing resource balance

12. **Establish Difficulty Scaling Methodology** (Q13)
    - Define primary and secondary scaling methods
    - Create progression curves for each stage type
    - Implement scaling system
    - **Owner**: Design Team
    - **Timeline**: Before creating full stage set

13. **Confirm Default Grid Dimensions** (Q14)
    - Verify default dimensions match design intent
    - Document dimension guidelines per stage type
    - Update level design documentation
    - **Owner**: Level Design Team
    - **Timeline**: Before creating stage templates

---

## Notes

- Questions are based on code analysis and documentation review
- Priority levels: Critical (blocks implementation), Important (affects gameplay), Nice-to-have (polish/balance)
- Suggested implementations are recommendations based on existing patterns
- All questions should be resolved before implementing related features
- This document should be updated as questions are resolved

---

**Document Status**: Active - Questions being collected and organized
**Next Review**: After design team review of critical priority questions

