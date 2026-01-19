# Priority Rules & Edge Cases

> This document defines the priority order and resolution rules for gameplay mechanics. Use this to identify gaps, edge cases, and design questions.

## Purpose
Provides a definitive reference for what happens when multiple game events occur simultaneously or in sequence. Helps identify gaps in the rule system and prioritize design questions.

---

## Summary: Game Context for Fun/Quality Analysis

### What This Game Is
**Infinity Cube** is a grid-based tactical puzzle game where players place markers that transform into backward-moving cubes to intercept forward-moving wave cubes. The core tension comes from **predictive positioning**—players must anticipate where cubes will be when collisions occur, creating a rhythm-based strategic challenge.

### Core Gameplay Loop & Fun Factors

**The Loop:**
1. **Analyze** → Player assesses cube formation patterns and movement speed
2. **Place** → Player strategically places markers (4 types: Unit, Matrix, Recursion, Infinity)
3. **Transform** → Markers convert to player cubes that move backward (up) toward waves
4. **Collide** → Player cubes collide with wave cubes, triggering captures, area effects, or face painting
5. **Resolve** → Results tracked, penalties/rewards applied, next wave begins

**What Makes It Fun:**
- **Rhythmic Precision**: Step-based movement creates predictable timing—players learn cube rhythms
- **Spatial Puzzle**: Bidirectional movement (waves down, player cubes up) creates interception puzzles
- **Resource Tension**: Limited marker charges force strategic decisions
- **Cascading Effects**: Face painting creates delayed rewards—paint Infinity cube now, get marker later
- **Risk/Reward**: Place markers early for better position vs. risk of Infinity destroying them
- **Area Control**: Matrix markers create 2x2/3x3 zones—spatial strategy matters
- **Mastery Progression**: Learning cube collision matrix and optimal marker placement

### Key Quality Indicators to Analyze

When evaluating these priority rules for fun/quality, consider:

**1. Clarity & Predictability**
- Are rules consistent? Players need to predict outcomes
- Do edge cases break player expectations?
- Are priority conflicts resolved in intuitive ways?

**2. Strategic Depth**
- Do rules create interesting trade-offs?
- Are there multiple valid strategies?
- Do edge cases add depth or just confusion?

**3. Player Agency**
- Can players plan around these rules?
- Do rules feel fair or arbitrary?
- Are there "gotcha" moments that feel unfair?

**4. Flow & Pacing**
- Do priority conflicts interrupt flow?
- Are resolution times appropriate?
- Do rules support or hinder the rhythm?

**5. Skill Expression**
- Can skilled players exploit these rules?
- Do rules reward planning vs. reaction?
- Are there mastery opportunities?

### Critical Questions for Analysis

**For Each Priority Rule, Ask:**
- Does this rule support the core loop or complicate it?
- Would a new player understand this intuitively?
- Does this create interesting strategic choices or just edge cases?
- Is this rule consistent with the game's rhythm-based nature?
- Does this add depth or just complexity?

**Red Flags (Rules That Hurt Fun):**
- Unpredictable outcomes that feel random
- Rules that punish players for correct play
- Edge cases that break core mechanics
- Priority conflicts that create "gotcha" moments
- Rules that reduce player agency

**Green Flags (Rules That Enhance Fun):**
- Rules that create clear trade-offs
- Edge cases that add strategic depth
- Priority systems that feel fair and predictable
- Rules that reward planning and mastery
- Systems that support the rhythm-based core loop

### How to Use This Document

This document identifies **gaps** (undefined behaviors) and **questions** (design decisions needed). Each gap/question should be evaluated for:
1. **Impact on Fun**: Does resolving this improve or hurt the experience?
2. **Clarity**: Does this need to be defined for players to plan effectively?
3. **Strategic Depth**: Does this create interesting choices or just edge cases?
4. **Consistency**: Does this align with the game's design philosophy?

**Priority for Resolution:**
- **High**: Rules that affect core loop, create confusion, or break player expectations
- **Medium**: Rules that add depth but aren't critical to basic play
- **Low**: Edge cases that rarely occur or have minimal impact

---

## 1. Player Actions (Before Move Forward)

### 1.1 Player Action Phase
Before the "Move Forward" sequence is triggered, players can perform actions:

**Available Actions:**
1. **Place Marker** (Unit, Matrix, Recursion, Infinity)
   - Check placement validation (tile available, charges available, max markers not exceeded)
   - Place marker on selected tile
   - Charge consumed
   - Marker added to tracking list

2. **Trigger Cube Marker** (Press R)
   - Check if cube marker exists
   - Trigger cube marker in order they were placed
   - Capture cubes in area
   - Remove cube marker

3. **Switch Marker Mode**
   - Change between Unit, Matrix, Recursion, Infinity marker types
   - Validate mode is available (charges available, markers available in wave)

4. **Unmark Tile**
   - Remove marker from tile
   - Charge refunded (if applicable)

**Action Priority:**
- Player actions happen before Move Forward sequence
- Multiple actions can be performed in sequence
- Actions are processed immediately (no queuing)
- Marker placement happens before Move Forward converts markers to cubes

### 1.2 Action Validation Rules

- Cannot place marker on occupied tile (marked state or cube occupation both prevent placement)
- Cannot place marker if no charges available
- Cannot place marker if max markers on grid exceeded
- Cannot trigger cube marker if none exists
- Cannot switch to marker mode if no charges available for that type

---

## 2. Move Forward Sequence (Priority Order)

### 2.1 Execution Order
When "Move Forward" is triggered (wave step), events occur in this **strict order**:

1. **Player Cubes Spawn** (from markers)
   - All markers convert to player cubes simultaneously
   - Player cubes spawn at marker positions
   - Markers are removed from grid
   - Wave cubes spawn at top of grid and move down - never spawn on marked tiles
   - First wave step converts marker to cube, next wave step moves cube
   - Cannot place marker on occupied tile (prevented by placement validation)
   - If player cube moves backward into tile that player just marked, newly spawned cube is destroyed (no stacking/displacing)

2. **Player Cubes Move Backward** (up toward wave)
   - All existing player cubes move up one tile
   - Movement is atomic (one tile per step)
   - Player cubes move until collision OR grid boundary
   - If destination tile has wave cube, collision is detected and resolved
   - If player cube moves backward into tile that player just marked, newly spawned cube is destroyed
   - No stacking or displacing - one cube per tile rule enforced

3. **Wave Cubes Move Forward** (down toward escape)
   - All wave cubes move down one tile
   - Movement is atomic (one tile per step)
   - If destination tile has player cube, collision occurs (governed by collision matrix)
   - Each tile can only have one cube - max collision is between 2 cubes (no simultaneous multiple collisions)
   - Collision happens at same time (no desired order, defaults to player cube if needed)

4. **Collision Detection**
   - Check for player cube + wave cube collisions
   - Resolve collisions based on collision matrix
   - Each tile can only have one cube - max collision is between 2 cubes
   - No simultaneous multiple collisions possible (one cube per tile rule)
   - Collision happens at same time (no desired order, defaults to player cube if needed)
   - Collision may need to be detected on spawn if wave cube is adjacent when player marks tile

5. **Cube Landing Events**
   - Fire landing events for all cubes that moved
   - Trigger tile interactions (markers, face painting, etc.)

### 2.2 Spawning Rules

- Wave cubes spawn at top of grid and move down - never spawn on marked tiles
- First wave step converts marker to cube, next wave step moves cube
- Player cannot be on same tile as wave cube - cannot mark a tile that has a wave cube
- A wave cube can collide with a player cube as soon as it's spawning (if adjacent when player marks tile)
- Multiple player cubes never occupy same tile
- If player cube moves backward into tile that player just marked, newly spawned cube is destroyed
- No stacking or displacing - one cube per tile rule enforced
- Marker placement cannot happen on same tile (marked state or occupied by cube both prevent placement)
- Tile goes from marked state to occupied by cube - both cases prevent placing new marker
- Player can only unmark a marked tile, cannot place marker in occupied tile

---

## 3. Collision Resolution Priority

### 2.1 Collision Detection Rules

- Each tile can only have one cube - max collision is between 2 cubes (no simultaneous multiple collisions)
- Collision happens at same time (no desired order, defaults to player cube if needed)
- Multiple player cubes never occupy same tile
- If player cube moves backward into tile that player just marked, newly spawned cube is destroyed
- No stacking or displacing - one cube per tile rule enforced
- Wave cubes cannot stack
- One cube per tile rule applies to all cube types

### 2.2 Collision Matrix Priority
When collision occurs, priority is determined by cube types:

| Player Cube | Wave Cube | Priority | Behavior |
|-------------|-----------|----------|----------|
| Unit | Unit | Standard | Capture wave cube |
| Unit | Matrix | Standard | Capture Matrix, create 2x2 cube marker |
| Unit | Recursion | Standard | Column capture (3 cubes) |
| Unit | Infinity | **High** | Unit destroyed, no face painting |
| Matrix | Unit | Standard | Capture wave cube |
| Matrix | Matrix | Standard | Create 3x3 cube marker |
| Matrix | Recursion | Standard | Auto 1x3 vertical marker |
| Matrix | Infinity | **High** | Paint Matrix face, Matrix destroyed |
| Recursion | Unit | Standard | Capture wave cube |
| Recursion | Matrix | Standard | Auto 1x3 vertical marker |
| Recursion | Recursion | Standard | Cross marker (5 tiles) |
| Recursion | Infinity | **High** | Paint Recursion face, Recursion destroyed |
| Infinity | Unit | **High** | Wave join (Infinity takes position, moves with wave) |
| Infinity | Matrix | **High** | Paint Infinity face, Infinity continues up |
| Infinity | Recursion | **High** | Paint Infinity face, Infinity continues up |
| Infinity | Infinity | **Highest** | Paint Infinity face, Resonance trigger |

**Priority Rules:**
- Infinity collisions have highest priority (always resolve first)
- Standard collisions resolve after Infinity collisions
- Each tile can only have one cube - max collision is between 2 cubes (no simultaneous multiple collisions)
- Collision happens at same time (no desired order, defaults to player cube if needed)

---

## 4. Marker Interaction Rules

### 3.1 Marker Placement Rules

**Can marker be placed?**
- Check: Valid grid position
- Check: Tile not corrupted
- Check: No existing marker on tile
- Check: No cube occupying tile (player cube or wave cube)
- Check: Player has available charges
- Check: Max markers on grid not exceeded
- Cannot place marker on occupied tile (marked state or cube occupation both prevent placement)
- Player can only unmark a marked tile, cannot place marker in occupied tile

**What happens after placement?**
- Marker visual appears
- Charge consumed
- Marker added to tracking list
- Tile goes from marked state to occupied by cube - both cases prevent placing new marker

### 3.2 Marker + Cube Interactions

- Wave cubes spawn at top and move down - never spawn on marked tiles
- Marker converts to cube on first wave step, so wave cube cannot move onto marker
- If wave cube moves into tile that was just marked, collision may need to be detected on spawn
- Marker converts to cube on first wave step - marker and cube cannot coexist
- If player cube moves backward into tile that player just marked, newly spawned cube is destroyed
- Marker placement cannot happen on same tile (marked state or occupied by cube both prevent placement)
- Infinity cubes ignore markers they pass over (Unit, Matrix, Recursion markers)
- Infinity cubes do not destroy or trigger markers they pass over

### 3.3 Cube Marker (Auto-Generated) Rules

- Cube markers created from collisions
- Created immediately on collision
- Player must manually trigger (press R)
- Player cube can move onto tile with cube marker, but would not spawn there (player cannot mark a tile that has a cube marker or a cube)
- Cube markers give player agency to decide when to trigger - it's up to player
- Wave cubes do nothing to tile with cube marker
- Cube marker remains on tile, player can still trigger manually
- We trigger cube markers in the order they were placed
- Player can trigger during cube movement but should ensure consistent timing in respect to move forward
- If wave cube is on tile with cube marker nothing happens unless marker is triggered for capture
- If cube marker is on tile with player cube nothing happens  unless marker is triggered for capture
- Infinity cubes ignore cube markers they pass over (both ones they create and ones created by other collisions)
- Infinity cubes pass over cube markers without interaction - they do not destroy or trigger them

### 3.4 Player-Placed Marker Triggers (Unit, Matrix, Recursion, Infinity)

- Player-placed markers are manually triggered by pressing R
- When triggered, markers affect all cubes in their area (wave cubes and player cubes)
- Matrix markers create area effects (2x2 or 3x3) that capture all non-Infinity cubes in the area
- If matrix marker is triggered when player cubes are in its area, those player cubes are destroyed
- Player cubes destroyed by marker triggers do not create cube markers or trigger other effects
- Wave cubes captured by matrix markers create cube markers as normal (2x2 for non-matching, 3x3 for Matrix+Matrix)
- Infinity cubes are not affected by marker triggers (cannot be captured)

---

## 5. Face Painting Priority

### 4.1 Face Painting Sequence

**Priority Order:**
1. Collision occurs (Player cube + Wave Infinity)
2. Face painting applied to collision face
3. Cube continues moving with wave
4. Face rotates as cube moves
5. When painted face touches grid → Marker appears

**Multiple Face Paintings:**
- Multiple faces can be painted
- New painting overwrites old painting on same face
- Different faces will leave different markers in sequential order

**Painted Face + Existing Marker:**
- If painted face touches grid with existing marker, we will replace it
- Existing marker is replaced by new marker from painted face

---

## 6. Player Death & Respawn Priority

### 5.1 Death Detection

- Player on same tile as any wave cube → Death
- Player cubes do not cause death for player - they can pass through
- We can prevent player death on collision tile because technically no cube made to tile (after collision resolution)

### 5.2 Death Consequences

- Player respawns after delay
- Death count increments
- If safe position is occupied by cube, we increase death count for every move forward it's occupied
- If safe position is occupied by cube, death count increments each move forward until safe position is available

### 5.3 Respawn Behavior

- Player respawns at safe position
- Brief invulnerability period

### 5.4 Death Penalties

- First death: No penalty
- Second death: Bottom row removed
- If row removal makes grid too small, that's fine - we give player freedom to decide when to restart wave/stage

---

## 7. Grid Boundary Rules

### 6.1 Cube Escape Priority

**Wave cubes escape (bottom):**
- Position.y < 0 → Cube escapes
- Escape count increments
- Penalty applies (if non-Infinity)
- Non-infinity cubes cannot escape during collision

**Player cubes destroyed (top):**
- Position.y >= grid.Height → Cube destroyed
- Penalty applies (if non-Infinity)
- Non-infinity cubes can't reach top on a collision
- Non-infinity player cubes that escape to top do nothing for now

### 6.2 Grid Modification Priority

- Row removal: Happens after death penalty
- Row restoration: Happens after perfect wave clear
- Cubes on row removed are lost too
- Player dies if on removed row

---

## 8. Marker Economy Priority

### 7.1 Charge Consumption

**Priority Order:**
1. Check if charge available
2. Consume charge
3. Place marker
4. Update UI

### 7.2 Charge Regeneration

- As wave moves, each stage/wave has a number of move forwards for a charge to be granted (by default it's 1)
- Each stage/wave has a maximum of charges stored
- We increment and decrement accordingly even if same time
- Cannot exceed max charges stored

### 7.3 Grant Application

**Priority Order:**
1. Stage grants applied (at stage start)
2. Wave grants applied (at wave start)
3. Charges capped at max inventory
4. Cannot exceed max inventory

---

## 9. Cube Marker Trigger Priority

### 8.1 Manual Trigger (Press R)

**Priority Order:**
1. Check if cube marker exists
2. Trigger cube marker
3. Capture cubes in area
4. Remove cube marker
5. We trigger cube markers in the order they were placed

---

## 10. Identified Gaps & Design Questions

### 10.1 High Priority Questions

1. **Cube landing events processing order**
   - What order do landing events process?
   - Should landing events happen before or after collision resolution?

### 10.2 Medium Priority Questions

3. **Player cube movement range (future consideration)**
   - **Current**: Move until collision/boundary
   - **Option C (Future)**: Type-based ranges (Unit=10, Matrix=8, Recursion=6, Infinity=12?)
   - Could combine with increasing captures needed as game progresses
   - See Brainstorming.md for full exploration

---

## 11. Testing Checklist

Use this checklist to verify priority rules:

- [x] Player cube spawns on tile with wave cube ✅ **RESOLVED**
- [x] Player cube spawns on tile with another player cube ✅ **RESOLVED**
- [x] Wave cube moves onto tile with marker ✅ **RESOLVED**
- [x] Infinity cube moves over Matrix marker ✅ **RESOLVED**: Infinity cubes ignore markers they pass over
- [x] Infinity cube moves over Recursion marker ✅ **RESOLVED**: Infinity cubes ignore markers they pass over
- [x] Infinity cube moves over Unit marker ✅ **RESOLVED**: Infinity cubes ignore markers they pass over
- [x] Infinity cube moves over cube marker ✅ **RESOLVED**: Infinity cubes ignore cube markers they pass over (both ones they create and ones from other collisions)
- [x] Multiple player cubes on same tile ✅ **RESOLVED**: Cannot occur
- [x] Multiple wave cubes on same tile ✅ **RESOLVED**: Cannot occur
- [x] Player cube + wave cube collision during spawn ✅ **RESOLVED**
- [x] Painted face touches tile with existing marker ✅ **RESOLVED**: Replace existing marker
- [x] Player dies during collision resolution ✅ **RESOLVED**: Prevent death on collision tile (no cube made to tile)
- [x] Cube escapes during collision ✅ **RESOLVED**: Non-infinity cubes cannot escape during collision
- [x] Row removal with cubes on that row ✅ **RESOLVED**: Cubes on removed row are lost
- [x] Player cube moves onto tile with cube marker ✅ **RESOLVED**: Can move onto, but cannot spawn there
- [ ] Player cube reaches fixed range limit (if implemented)
- [ ] Player cube movement range vs collision timing
- [ ] Cube landing events processing order

---

## 12. Recommended Resolutions

### 12.1 High Priority Resolutions Needed

**Infinity cubes and markers:** ✅ **RESOLVED**
- **RESOLVED BEHAVIOR**: Infinity cubes ignore markers they pass over (Unit, Matrix, Recursion markers)
- **RESOLVED BEHAVIOR**: Infinity cubes ignore cube markers they pass over (both ones they create and ones from other collisions)
- **RATIONALE**: Infinity cubes would be too powerful if they cancelled out all other collisions resulting cube markers
- Infinity cubes pass over markers without interaction - they do not destroy or trigger them

**Cube landing events processing order:**
- **RECOMMENDATION**: Process landing events after collision resolution
- Ensures collision outcomes are finalized before triggering tile interactions
- Prevents edge cases where landing events might interfere with collision resolution

### 12.2 Future Considerations

**Player cube movement range:**
- **CURRENT**: Move until collision/boundary
- **FUTURE OPTION**: Type-based ranges (Unit=10, Matrix=8, Recursion=6, Infinity=12?)
- Could combine with increasing captures needed as game progresses
- See Brainstorming.md for full exploration of this mechanic

### 12.3 Implementation Notes

- All priority rules should be tested in isolation
- Edge cases should be logged for debugging
- Player feedback should indicate when edge cases occur
- Design decisions should be documented when resolved

---

## 13. Update Log

- **2025-12-21**: Initial document created
- Identified 10+ gaps in priority rules
- Documented move forward sequence
- Listed collision resolution priorities
- Created testing checklist
- **2025-12-21**: Added player cube movement range design question
  - Documented current behavior (move until collision/boundary)
  - Added three design options (current, fixed range, type-based range)
  - Added to high priority questions and recommended resolutions
- **2025-12-21**: Resolved player cube spawn and movement questions
  - RESOLVED: Player cube spawns on tile even if wave cube present, moves backward next step
  - RESOLVED: Cannot place marker on occupied tile (validation prevents)
  - RESOLVED: Keep move-until-collision for now (Option A)
  - Documented Option C (type-based ranges) in Brainstorming.md for future consideration
- **2025-12-21**: Clarified core mechanics and collision rules
  - RESOLVED: Wave cubes spawn at top and move down - never spawn on marked tiles
  - RESOLVED: First wave step converts marker to cube, next wave step moves cube
  - RESOLVED: Each tile can only have one cube - max collision is between 2 cubes (no simultaneous multiple collisions)
  - RESOLVED: Collision happens at same time (no desired order, defaults to player cube if needed)
  - RESOLVED: Player cannot mark tile with wave cube - collision may need to be detected on spawn if adjacent
  - RESOLVED: Multiple player cubes never occupy same tile - if player cube moves backward into marked tile, newly spawned cube is destroyed
  - RESOLVED: Marker placement cannot happen on same tile (marked state or occupied by cube both prevent placement)
  - RESOLVED: Player can only unmark a marked tile, cannot place marker in occupied tile
- **2025-12-21**: Resolved additional mechanics and edge cases
  - RESOLVED: Player cube can move onto tile with cube marker, but cannot spawn there - cube markers give player agency
  - RESOLVED: Wave cubes do nothing to tile with cube marker
  - RESOLVED: Multiple faces can be painted - new overwrites old, different faces leave different markers sequentially
  - RESOLVED: If painted face touches grid with existing marker, we replace it
  - RESOLVED: Player cubes do not cause death - they can pass through
  - RESOLVED: Prevent player death on collision tile (no cube made to tile after collision)
  - RESOLVED: If safe respawn position is occupied, increase death count for every move forward it's occupied
  - RESOLVED: Non-infinity cubes cannot escape/reach top during collision
  - RESOLVED: Non-infinity player cubes that escape to top do nothing for now
  - RESOLVED: Cubes on removed row are lost, player dies if on removed row
  - RESOLVED: If row removal makes grid too small, player decides when to restart
  - RESOLVED: Charge regeneration - each stage/wave has move forwards per charge (default 1) and max charges stored, increment/decrement even if same time, cannot exceed max
- **2025-12-21**: Restructured document
  - Separated clean rules from gaps/questions
  - Removed RESOLVED/QUESTION markers from rules sections
  - Moved all gaps and design questions to dedicated section
  - Clarified cube marker trigger rules and timing
- **2025-12-21**: Added Player Actions section
  - Documented player action phase that occurs before Move Forward sequence
  - Clarified that marker placement happens before Move Forward converts markers to cubes
  - Added action validation rules
- **2025-12-21**: Resolved Infinity cubes and markers behavior
  - RESOLVED: Infinity cubes ignore markers they pass over (Unit, Matrix, Recursion markers)
  - RESOLVED: Infinity cubes ignore cube markers they pass over (both ones they create and ones from other collisions)
  - RATIONALE: Infinity cubes would be too powerful if they cancelled out all other collisions resulting cube markers
  - Infinity cubes pass over markers without interaction - they do not destroy or trigger them
