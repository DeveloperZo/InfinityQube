# Milestone 1.2: Refine Markers Implementation - Planning Document

> **Status**: Planning Phase  
> **Target**: Review and refine marker system (placement, cube spawn, cube collision/interactions)  
> **Complexity**: 3 points  
> **Effort Estimate**: ~1 week active development  
> **Dependencies**: Milestone 1.1 ✅ (paired wave system complete)

---

## Overview

This milestone focuses on refining the marker system to ensure cohesive gameplay, clear collision mechanics, and strategic depth. The goal is to define and implement all marker interactions, cube collision behaviors, and visual feedback systems.

---

## Current System State

### ✅ What's Already Implemented
- **Marker Types**: Unit, Recursion, Matrix, Infinity, Cube markers
- **Marker Placement**: All marker types can be placed with charge system
- **Marker Spawning**: Markers automatically convert to player cubes when wave moves forward
- **Charge System**: Regeneration mechanics exist for all marker types
- **Paired Wave System**: Markers from Wave A spawn cubes in Wave B
- **Basic Collision**: Player cubes can capture wave cubes
- **Matrix Area Effect**: Matrix cubes capture in 3x3 area

### ⚠️ What Needs Definition/Refinement
- **Cube Collision Matrix**: Complete interaction rules for all cube type combinations
- **Marker Mirroring Rules**: Which markers get mirrored and how
- **Charge System Coherence**: Ensure all regeneration mechanics are balanced
- **Visual Feedback**: Distinct colors and indicators for all marker types
- **Non-Unit Marker Acquisition**: Ways to acquire markers beyond initial charges

---

## Task Breakdown

### Task 1: Define Marker Placement and Spawning for All Markers

**Current State**: 
- Unit, Recursion, Matrix, Infinity markers can be placed using unified input system (mode keys 1-4, placement key F)
- Cube markers are generated from Matrix cube captures
- All markers automatically spawn appropriate cube types when wave moves forward
- Optional R key trigger activates markers to capture cubes (alternative to automatic spawning)

**Actions Needed**:
- [x] Verify all marker types spawn correct cube types:
  - Unit Marker → Unit Cube ✅ (automatic on wave movement)
  - Recursion Marker → Recursion Cube ✅ (automatic on wave movement)
  - Matrix Marker → Matrix Cube ✅ (automatic on wave movement, 2x2 area)
  - Infinity Marker → Infinity Cube ✅ (automatic on wave movement)
  - Cube Marker → Area detonation (variable size: 2x2 or 3x3) ✅
- [x] Document marker placement rules and restrictions ✅
- [x] Verify marker-to-cube conversion in paired wave system ✅
- [x] Document unified input system (mode keys 1-4, placement F, automatic spawning) ✅

**Acceptance Criteria**:
- ✅ All marker types can be placed and spawn appropriate cubes
- ✅ Marker placement rules are clearly defined and enforced
- ✅ Marker-to-cube conversion works correctly in both immediate and paired wave contexts

---

### Task 2: Design/Define Cube Collisions for All Combinations

**Current State**:
- Player cubes can capture wave cubes (Matrix = area, others = single target)
- Infinity cubes are uncapturable via markers
- Basic collision detection exists

**Focus Areas** (as specified in roadmap):
- **Matrix/Matrix Recursion/Recursion interactions**: Create meaningful gameplay rewarding player for matching
- **Infinity/Infinity interactions**: Define collision behavior

**Collision Matrix to Define**:

#### Player Cube → Wave Cube Collisions

| Player Cube | Wave Cube | Current Behavior | Proposed Behavior | Strategic Value |
|-------------|-----------|------------------|-------------------|----------------|
| Unit | Unit | Captures | Captures + bonus? | Standard capture |
| Unit | Recursion | Captures (1 hit) | Captures (1 hit) | Standard capture |
| Unit | Matrix | Captures | Captures | Standard capture |
| Unit | Infinity | Cannot capture | Cannot capture | N/A |
| Recursion | Unit | Captures | Captures | Standard capture |
| Recursion | Recursion | Captures (1 hit) | **Captures + resource reward?** | **Reward matching** |
| Recursion | Matrix | Captures | Captures | Standard capture |
| Recursion | Infinity | Cannot capture | Cannot capture | N/A |
| Matrix | Unit | Area capture (3x3) | Area capture (3x3) | Area effect |
| Matrix | Recursion | Area capture (3x3) | Area capture (3x3) | Area effect |
| Matrix | Matrix | Area capture (3x3) | **Area capture + enhanced reward?** | **Reward matching** |
| Matrix | Infinity | Cannot capture | Cannot capture | N/A |
| Infinity | Unit | Pause effect? | **Define interaction** | **Strategic pause** |
| Infinity | Recursion | Pause effect? | **Define interaction** | **Strategic pause** |
| Infinity | Matrix | Pause effect? | **Define interaction** | **Strategic pause** |
| Infinity | Infinity | **Undefined** | **Define collision behavior** | **Critical interaction** |

#### Wave Cube → Wave Cube Collisions (Same Position)

| Cube Type 1 | Cube Type 2 | Current Behavior | Proposed Behavior |
|-------------|-------------|------------------|-------------------|
| Infinity | Infinity | **Undefined** | **Tile corruption OR alternating pause OR lateral movement** |
| Infinity | Any Other | Destroys other | Destroys other (existing) |
| Matrix | Matrix | **Undefined** | **Define interaction** |
| Recursion | Recursion | **Undefined** | **Define interaction** |

**Design Questions to Answer**:
1. **Matrix/Matrix Recursion/Recursion Matching Rewards**:
   - Should matching types provide bonus resources (marker charges)?
   - Should matching types provide score multipliers?
   - Should matching types create special effects (detonations, area effects)?

2. **Infinity/Infinity Collision** (from GameplayMechanics.md):
   - Option A: Tile Corruption - Collision point becomes permanently corrupted
   - Option B: Alternating Pause - Both cubes enter alternating pause pattern
   - Option C: Lateral Movement - Collision triggers sideways movement
   - **Decision Needed**: Choose one or combination

3. **Infinity Player Cube Interactions**:
   - How do player Infinity cubes interact with wave cubes?
   - Should they pause wave cubes?
   - Should they destroy wave cubes on collision?

**Actions Needed**:
- [ ] Design collision matrix for all combinations
- [ ] Implement matching type rewards (Matrix/Matrix, Recursion/Recursion)
- [ ] Implement Infinity/Infinity collision behavior (choose option)
- [ ] Implement Infinity player cube interactions
- [ ] Update documentation with collision rules
- [ ] Add visual feedback for special collision types

**Acceptance Criteria**:
- ✅ Collision logic defined for all cube type combinations
- ✅ Matching type collisions (Matrix/Matrix, Recursion/Recursion) provide meaningful rewards
- ✅ Infinity/Infinity collision behavior is implemented and tested
- ✅ All collision behaviors are documented

---

### Task 3: Design/Define Marker Mirroring and Gameplay Mechanics

**Current State**:
- Paired wave system records all marker positions
- Markers from Wave A spawn cubes in Wave B (mirrored wave)
- Mirroring is automatic for all marker types

**Design Questions**:
1. **Which Markers Should Be Mirrored?**
   - Current: All markers are mirrored
   - Question: Should some markers NOT be mirrored (e.g., Cube markers)?
   - Question: Should mirroring be optional per marker type?

2. **Mirroring Mechanics**:
   - Current: Markers spawn cubes at recorded positions
   - Question: Should mirroring include position transformations (Y-axis flip, offset)?
   - Question: Should mirroring preserve marker type or allow type conversion?

3. **Strategic Depth**:
   - How can mirroring create interesting strategic decisions?
   - Should players be able to "opt out" of mirroring for certain markers?
   - Should mirroring have resource costs or limitations?

**Proposed Mirroring Rules**:

| Marker Type | Should Mirror? | Spawn Type | Special Rules |
|-------------|----------------|-----------|---------------|
| Unit | ✅ Yes | Unit Cube | Standard mirroring |
| Recursion | ✅ Yes | Recursion Cube | Standard mirroring |
| Matrix | ✅ Yes | Matrix Cube | Spawns at center position |
| Infinity | ✅ Yes | Infinity Cube | Standard mirroring |
| Cube | ❓ TBD | N/A | Cube markers are direct actions, not placements |

**Actions Needed**:
- [ ] Define mirroring rules for each marker type
- [ ] Implement mirroring exclusions if needed (e.g., Cube markers)
- [ ] Design mirroring position transformations (if any)
- [ ] Add visual indicators for which markers will be mirrored
- [ ] Test mirroring with all marker combinations
- [ ] Document mirroring mechanics

**Acceptance Criteria**:
- ✅ Mirroring rules are defined for all marker types
- ✅ Mirroring mechanics create strategic depth
- ✅ Visual feedback shows which markers will be mirrored
- ✅ Mirroring behavior is documented

---

### Task 4: Ensure Charge System and Regeneration Mechanics Are Coherent

**Current State**:
- Each marker type has charge system with regeneration
- Cooldown-based regeneration exists
- Charge limits are configurable

**Review Areas**:
1. **Charge Limits**:
   - Unit: High quantity, fast regeneration
   - Recursion: Medium quantity, medium regeneration
   - Matrix: Low quantity, slow regeneration
   - Infinity: Very low quantity, very slow regeneration
   - **Verify**: Are these limits balanced?

2. **Regeneration Timing**:
   - All types use cooldown-based regeneration
   - **Verify**: Are cooldowns appropriate for gameplay pacing?

3. **Resource Economy**:
   - How do charges relate to wave difficulty?
   - Are players able to manage resources effectively?
   - **Verify**: Is the economy balanced?

**Actions Needed**:
- [ ] Review current charge limits and regeneration rates
- [ ] Test charge system across different wave scenarios
- [ ] Balance charge economy for strategic depth
- [ ] Ensure regeneration feels fair and predictable
- [ ] Document charge system mechanics
- [ ] Add UI indicators for regeneration progress

**Acceptance Criteria**:
- ✅ Charge limits are balanced and create strategic decisions
- ✅ Regeneration rates feel fair and predictable
- ✅ Resource economy supports strategic gameplay
- ✅ Charge system is documented

---

### Task 5: Visual Feedback for Markers

**Current State**:
- Markers have basic visual representations
- Tile highlighting exists for some marker types
- Visual feedback may not be distinct enough

**Requirements**:
- **Distinct Color Per Marker Type**:
  - Unit Marker: [Define color]
  - Recursion Marker: [Define color]
  - Matrix Marker: [Define color]
  - Infinity Marker: [Define color - currently dark charcoal]
  - Cube Marker: [Define color]

- **Interaction Indicators** (if applicable):
  - Range indicators for area effects (Matrix)
  - Pause effect indicators (Infinity)
  - Charge status indicators
  - Mirroring preview indicators

**Actions Needed**:
- [ ] Define color scheme for all marker types
- [ ] Implement distinct visual styles for each marker type
- [ ] Add interaction range indicators where applicable
- [ ] Add charge status visual feedback
- [ ] Add mirroring preview visuals
- [ ] Test visual clarity in various scenarios

**Acceptance Criteria**:
- ✅ Each marker type has distinct, recognizable visual style
- ✅ Visual feedback clearly shows marker and spawned cube relationship
- ✅ Interaction indicators are clear and helpful
- ✅ Visual feedback works in all lighting/scenario conditions

---

### Task 6: Explore Interesting Ways to Acquire Non-Unit Markers

**Current State**:
- Markers are acquired through charge regeneration
- Cube markers are generated from Matrix cube captures
- No other acquisition methods exist

**Design Goal**: Create scarcity and strategic depth

**Proposed Acquisition Methods**:

1. **Same-Type Collision Rewards**:
   - Matrix player cube + Matrix wave cube = Matrix marker charge
   - Recursion player cube + Recursion wave cube = Recursion marker charge
   - **Benefit**: Rewards skillful matching, creates resource generation loop

2. **Wave Completion Rewards**:
   - Complete wave with high performance = bonus marker charges
   - **Benefit**: Rewards skilled play, provides comeback mechanics

3. **Strategic Tile Interactions**:
   - Capture cubes on specific tile types = bonus charges
   - **Benefit**: Encourages strategic positioning

4. **Combo System**:
   - Chain multiple captures = bonus charges
   - **Benefit**: Rewards aggressive play, creates risk/reward

5. **Stage Progression Rewards**:
   - Complete stages = permanent charge increases
   - **Benefit**: Meta-progression, RPG elements

**Actions Needed**:
- [ ] Design acquisition mechanics that create strategic depth
- [ ] Implement same-type collision rewards (if chosen)
- [ ] Test acquisition mechanics for balance
- [ ] Ensure scarcity creates meaningful decisions
- [ ] Document acquisition methods

**Acceptance Criteria**:
- ✅ Non-unit marker acquisition methods are implemented
- ✅ Acquisition creates strategic depth and scarcity
- ✅ Methods feel rewarding and balanced
- ✅ Acquisition mechanics are documented

---

## Implementation Order

### Phase 1: Foundation (Days 1-2)
1. Task 1: Verify marker placement and spawning
2. Task 4: Review charge system coherence
3. Task 5: Implement basic visual feedback

### Phase 2: Core Mechanics (Days 3-4)
4. Task 2: Design and implement cube collision matrix
5. Task 3: Define marker mirroring rules

### Phase 3: Enhancement (Days 5-6)
6. Task 5: Enhance visual feedback with indicators
7. Task 6: Implement marker acquisition mechanics

### Phase 4: Testing & Documentation (Day 7)
8. Integration testing
9. Documentation updates
10. Playtesting checkpoint

---

## Acceptance Criteria Summary

- ✅ Markers can be placed and spawn the appropriate cube
- ✅ Collision logic has been defined for all scenarios
- ✅ Charge system and regeneration work correctly
- ✅ Visual feedback clearly shows marker and spawned cube
- ✅ Marker mirroring rules are defined and implemented
- ✅ Non-unit marker acquisition creates strategic depth

---

## Playtesting Checkpoint

**After Implementation**:
- Playtest: Place all marker types, verify mechanics work
- Verify: Marker dynamics creates strategic depth
- Iterate: Balance charge system and visual feedback
- Test: All collision combinations work as designed
- Validate: Mirroring creates interesting strategic decisions

---

## Documentation Updates Required

1. **GameplayMechanics.md**: Update collision matrix section
2. **GameplayMechanics.md**: Update marker mirroring section
3. **GameplayMechanics.md**: Update charge system section
4. **Technical Doc**: Update collision implementation details
5. **Technical Doc**: Update marker system architecture

---

## Risk Mitigation

### Identified Risks:
1. **Collision Complexity**: Too many special cases could confuse players
   - **Mitigation**: Keep collision rules simple and consistent
   - **Fallback**: Start with basic rules, add complexity iteratively

2. **Visual Clarity**: Too many visual indicators could be overwhelming
   - **Mitigation**: Use clear, distinct colors and minimal indicators
   - **Fallback**: Allow players to toggle indicators on/off

3. **Balance Issues**: Acquisition mechanics could break resource economy
   - **Mitigation**: Test thoroughly, start conservative
   - **Fallback**: Make acquisition rates easily adjustable

---

## Success Metrics

- **Clarity**: Players understand all marker types and their uses
- **Strategic Depth**: Marker choices create meaningful decisions
- **Visual Feedback**: Players can quickly identify marker types and states
- **Balance**: Charge system feels fair and strategic
- **Fun Factor**: Matching type collisions feel rewarding

---

**Last Updated**: December 2025  
**Next Steps**: Begin Phase 1 implementation

