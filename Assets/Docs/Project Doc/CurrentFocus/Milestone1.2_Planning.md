# Milestone 1.2: Refine Markers Implementation - Planning Document

> **Status**: Planning Phase  
> **Target**: Review and refine marker system (placement, cube spawn, cube collision/interactions)  
> **Complexity**: 3 points  
> **Effort Estimate**: ~1 week active development  
> **Dependencies**: Core marker system complete

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
- **Basic Collision**: Player cubes can capture wave cubes
- **Matrix Area Effect**: Matrix cubes capture in 3x3 area

### ⚠️ What Needs Definition/Refinement
- **Cube Collision Matrix**: ✅ Complete (including resonance) - See GameplayMechanics.md
- **Charge System Coherence**: Ensure all regeneration mechanics are balanced
- **Visual Feedback**: Distinct colors and indicators for all marker types
- **Non-Unit Marker Acquisition**: Ways to acquire markers beyond initial charges
- **Line Divider System**: Dynamic difficulty mechanism restricting marker placement
- **Resonance System**: Infinity vs Infinity phaseable effect implementation
- **Enhanced Face Painting**: Rotation mechanics, timing, visual feedback
- **Penalty/Reward System**: Line movement based on cube falls and achievements
- **Marker Economy**: Per-stage grants and cross-wave resource management

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
- [x] Document unified input system (mode keys 1-4, placement F, automatic spawning) ✅

**Acceptance Criteria**:
- ✅ All marker types can be placed and spawn appropriate cubes
- ✅ Marker placement rules are clearly defined and enforced
- ✅ Marker-to-cube conversion works correctly

---

### Task 2: Design/Define Cube Collisions for All Combinations ✅ **DESIGN COMPLETE**

**Current State**: ✅ **ALL COLLISIONS DEFINED AND DOCUMENTED**
- Complete collision matrix documented for all 16 combinations
- Same-type matching rewards designed (Matrix+Matrix, Recursion+Recursion)
- Infinity+Infinity collision behavior defined (face paint + resonance)
- Face painting system integrated with all Infinity collisions
- **Documentation**: Complete in GameplayMechanics.md section 3.2

**Focus Areas** (as specified in roadmap): ✅ **COMPLETE**
- **Matrix/Matrix Recursion/Recursion interactions**: ✅ Defined - Creates meaningful gameplay rewarding player for matching
- **Infinity/Infinity interactions**: ✅ Defined - Face paint + resonance effect

**Collision Matrix**: ✅ **DEFINED** - See [Refined Collision Table](3_GameplayMechanics.md#cube-collision-matrix) in GameplayMechanics.md

**Status**: All collision combinations have been defined and documented. See `Milestone1.2_Task2_CollisionMatrix.md` for detailed implementation notes.

**Design Questions**: ✅ **ANSWERED**

1. **Matrix/Matrix Recursion/Recursion Matching Rewards**: ✅ **DEFINED**
   - Matrix+Matrix: Creates 3x3 triggerable marker (enhanced reward)
   - Recursion+Recursion: Creates cross-shaped marker (5 tiles, auto-captures)
   - Rewards matching with expanded area effects

2. **Infinity/Infinity Collision**: ✅ **DEFINED**
   - **Decision**: Face paint + resonance effect
   - When painted face touches grid, ALL Infinity cubes on grid become phaseable for that turn
   - Maintains Infinity's immutable nature while providing strategic interaction

3. **Infinity Player Cube Interactions**: ✅ **DEFINED**
   - Infinity + Unit: Wave join (removes Unit, takes position, moves with wave)
   - Infinity + Matrix: Face paint, continue up (tile becomes 2x2 manual marker)
   - Infinity + Recursion: Face paint, continue up (tile auto-captures 3 cubes)
   - Infinity + Infinity: Face paint + resonance (all Infinity cubes phaseable)

**Actions Needed**:
- [x] Design collision matrix for all combinations ✅ **COMPLETE**
- [x] Update documentation with collision rules ✅ **COMPLETE**
- [ ] Implement matching type rewards (Matrix/Matrix, Recursion/Recursion) - **Code Implementation**
- [ ] Implement Infinity/Infinity collision behavior (face paint + resonance) - **Code Implementation**
- [ ] Implement Infinity player cube interactions - **Code Implementation**
- [ ] Add visual feedback for special collision types - **Code Implementation**

**Note**: Design and documentation are complete. Remaining items are code implementation tasks. Resonance system design is complete - see GameplayMechanics.md section 3.2 for details.

**Acceptance Criteria**:
- ✅ Collision logic defined for all cube type combinations (16 combinations)
- ✅ Complete collision matrix documented in GameplayMechanics.md
- ✅ Matching type collisions (Matrix+Matrix, Recursion+Recursion) provide meaningful rewards
  - Matrix+Matrix: 3x3 triggerable marker
  - Recursion+Recursion: Cross marker (5 tiles)
- ✅ Infinity+Infinity collision behavior defined (face paint + resonance effect)
- ✅ All Infinity collision behaviors defined (face painting system)
- ✅ All collision behaviors documented in Task 2 document

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

### Task 6: Implement Line Divider System

**Current State**: 
- System designed and documented in GameplayMechanics.md
- Line divider creates dynamic difficulty by restricting marker placement
- Line moves up as reward, down as penalty

**Design**: ✅ **COMPLETE** - See GameplayMechanics.md section 3.1

**Key Features**:
- **Divider Position**: Line divides grid (e.g., row 10 on 20-row grid)
- **Placement Restriction**: Players can only place markers below the line
- **Dynamic Movement**: Line moves up (rewards) or down (penalties) based on performance
- **Strategic Tension**: Threats visible above line but cannot be acted upon until they cross

**Penalty System** (from GameplayMechanics.md):
- Unit cube falls off grid → Line moves down 1 row
- Matrix cube falls off grid → Line moves down 2 rows
- Recursion cube falls off grid → Line moves down 2 rows
- Infinity cube falls off grid → No penalty (intended behavior)

**Reward System** (from GameplayMechanics.md):
- Perfect wave clear (all non-Infinity captured) → Line moves up 1 row
- Painted face triggers → Line moves up 1 row
- Resonance triggers (all Infinity phaseable) → Line moves up 2 rows

**Actions Needed**:
- [ ] Implement line divider position tracking in GridManager
- [ ] Add marker placement restriction (only below line)
- [ ] Implement penalty system (track cube falls, move line down)
- [ ] Implement reward system (track achievements, move line up)
- [ ] Add visual indicator for line divider position
- [ ] Add visual feedback when line moves (up/down animation)
- [ ] Test line movement balance (penalty/reward values)
- [ ] Document line divider implementation

**Acceptance Criteria**:
- ✅ Line divider restricts marker placement to lower rows
- ✅ Line moves down when cubes fall off grid (appropriate penalties)
- ✅ Line moves up for perfect clears and achievements (appropriate rewards)
- ✅ Visual feedback clearly shows line position and movement
- ✅ System creates strategic tension and meaningful decisions

---

### Task 7: Implement Resonance System

**Current State**: 
- System designed and documented in GameplayMechanics.md
- Infinity vs Infinity collision creates resonance effect
- When painted face touches grid, ALL Infinity cubes become phaseable

**Design**: ✅ **COMPLETE** - See GameplayMechanics.md section 3.2

**Key Features**:
- **Trigger**: Player Infinity collides with Wave Infinity
- **Face Painting**: Collision face is painted
- **Resonance Activation**: When painted face touches grid, all Infinity cubes become phaseable
- **Phaseable State**: Phaseable Infinity cubes can be passed through by other player cubes
- **Strategic Sequencing**: Enables advanced strategy - paint multiple Infinity cubes, then sequence follow-up cubes

**Actions Needed**:
- [ ] Implement Infinity vs Infinity collision detection
- [ ] Implement face painting for Infinity+Infinity collision
- [ ] Track painted face rotation and grid contact
- [ ] Implement phaseable state for Infinity cubes
- [ ] Add phaseable state to collision detection (allow passing through)
- [ ] Add visual feedback for phaseable Infinity cubes
- [ ] Add visual feedback for resonance activation
- [ ] Test resonance timing and sequencing strategies
- [ ] Document resonance implementation

**Acceptance Criteria**:
- ✅ Infinity vs Infinity collision paints face correctly
- ✅ Painted face triggers resonance when touching grid
- ✅ All Infinity cubes become phaseable during resonance window
- ✅ Player cubes can pass through phaseable Infinity cubes
- ✅ Visual feedback clearly shows phaseable state
- ✅ Resonance enables advanced strategic sequencing

---

### Task 8: Implement Enhanced Face Painting System

**Current State**: 
- Basic face painting exists
- Enhanced system designed with rotation mechanics and visual feedback
- System documented in GameplayMechanics.md

**Design**: ✅ **COMPLETE** - See GameplayMechanics.md section 3.2

**Key Features**:
- **Rotation Schedule**: Cubes rotate on fixed, predictable schedule
- **Timing Mastery**: Players learn rotation rhythm to predict marker placement
- **Visual Indicators**: Painted face has distinct color/glow
- **Grid Telegraph**: Target tile pulses when painted face is 1 turn from touching grid
- **Face Status Types**: Matrix, Recursion, Unit, Infinity (resonance)

**Actions Needed**:
- [ ] Implement predictable cube rotation schedule
- [ ] Track painted face rotation state
- [ ] Add visual indicator on painted face (color/glow)
- [ ] Implement grid telegraph (pulse target tile 1 turn before contact)
- [ ] Add visual feedback for face status types
- [ ] Test rotation timing and predictability
- [ ] Ensure players can learn and master rotation rhythm
- [ ] Document face painting rotation mechanics

**Acceptance Criteria**:
- ✅ Cubes rotate on fixed, predictable schedule
- ✅ Painted faces have distinct visual indicators
- ✅ Grid telegraph shows marker placement location 1 turn before
- ✅ Rotation timing is learnable and masterable
- ✅ Visual feedback clearly communicates face painting state

---

### Task 9: Implement Marker Economy System

**Current State**: 
- System designed and documented in GameplayMechanics.md
- Per-stage grant system creates strategic scarcity
- Cross-wave resource management

**Design**: ✅ **COMPLETE** - See GameplayMechanics.md section 3.5

**Key Features**:
- **Per Stage Grant**: Fixed number of non-Unit markers at stage start
- **Cross-Wave Management**: Players manage inventory across all waves in stage
- **No Replenishment**: Spent markers do not replenish until next stage
- **Strategic Conservation**: Forces balance between immediate needs and future waves

**Marker Behavior**:
- Unit Markers: Unlimited availability
- Matrix Markers: Scarce resource, manual trigger
- Recursion Markers: Scarce resource, auto trigger
- Infinity Markers: Very scarce, unlocked later in progression

**Actions Needed**:
- [ ] Implement per-stage marker grant system
- [ ] Track marker inventory across waves within stage
- [ ] Prevent marker replenishment during stage
- [ ] Reset marker inventory at stage start
- [ ] Add UI indicators for marker inventory
- [ ] Balance marker grant amounts per stage
- [ ] Test marker economy across different stage lengths
- [ ] Document marker economy implementation

**Acceptance Criteria**:
- ✅ Players receive fixed marker grants at stage start
- ✅ Marker inventory persists across waves within stage
- ✅ Markers do not replenish during stage
- ✅ UI clearly shows marker inventory
- ✅ Economy creates strategic scarcity and meaningful decisions

---

### Task 10: Explore Interesting Ways to Acquire Non-Unit Markers

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
1. Task 1: Verify marker placement and spawning ✅
2. Task 4: Review charge system coherence
3. Task 5: Implement basic visual feedback

### Phase 2: Core Gameplay Systems (Days 3-7)
4. Task 6: Implement Line Divider System (Days 3-4)
5. Task 7: Implement Resonance System (Days 4-5)
6. Task 8: Implement Enhanced Face Painting (Days 5-6)
7. Task 9: Implement Marker Economy (Day 6-7)

### Phase 3: Collision & Mirroring (Days 8-9)
8. Task 2: Implement cube collision matrix (code implementation)
9. Task 3: Define marker mirroring rules

### Phase 4: Enhancement (Days 10-11)
10. Task 5: Enhance visual feedback with indicators
11. Task 10: Implement marker acquisition mechanics

### Phase 5: Testing & Documentation (Day 12-14)
12. Integration testing
13. Documentation updates
14. Playtesting checkpoint

---

## Acceptance Criteria Summary

- ✅ Markers can be placed and spawn the appropriate cube
- ✅ Collision logic has been defined for all scenarios (including resonance)
- ✅ Charge system and regeneration work correctly
- ✅ Visual feedback clearly shows marker and spawned cube
- ✅ Marker mirroring rules are defined and implemented
- ✅ Line divider system creates dynamic difficulty and strategic tension
- ✅ Resonance system enables advanced Infinity strategies
- ✅ Enhanced face painting with rotation mechanics is learnable
- ✅ Penalty/reward system moves line appropriately
- ✅ Marker economy creates strategic scarcity across waves
- ✅ Non-unit marker acquisition creates strategic depth

---

## Playtesting Checkpoint

**After Implementation**:
- Playtest: Place all marker types, verify mechanics work
- Verify: Marker dynamics creates strategic depth
- Test: Line divider creates tension and meaningful decisions
- Test: Resonance enables advanced Infinity sequencing strategies
- Test: Face painting rotation is learnable and rewarding
- Test: Penalty/reward system feels balanced and fair
- Test: Marker economy creates scarcity without frustration
- Test: All collision combinations work as designed
- Validate: Mirroring creates interesting strategic decisions
- Iterate: Balance charge system, line movement, and visual feedback

---

## Documentation Updates Required

1. **GameplayMechanics.md**: ✅ Collision matrix section complete (including resonance)
2. **GameplayMechanics.md**: ✅ Line divider system documented
3. **GameplayMechanics.md**: ✅ Face painting system documented (enhanced)
4. **GameplayMechanics.md**: ✅ Penalty/reward system documented
5. **GameplayMechanics.md**: ✅ Marker economy documented
6. **GameplayMechanics.md**: Update marker mirroring section (when implemented)
7. **GameplayMechanics.md**: Update charge system section (when balanced)
8. **Technical Doc**: Update collision implementation details (when implemented)
9. **Technical Doc**: Update marker system architecture (when implemented)
10. **Technical Doc**: Document line divider implementation
11. **Technical Doc**: Document resonance system implementation
12. **Technical Doc**: Document face painting rotation mechanics

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
**Status**: Design phase complete for new systems, ready for implementation  
**Next Steps**: Begin Phase 1 implementation, then proceed to Phase 2 (Core Gameplay Systems)

