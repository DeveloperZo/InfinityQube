# Development Roadmap

> **Document Purpose:** This is the definitive, actionable guide for what to work on next, in priority order. For a historical record of completed milestones, see [MilestoneTracking.md](MilestoneTracking.md).

## Overview
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

**Note**: Timeline dates are applied only upon completion. Forward-looking dates are not projected.

---

## Current Status

### 📍 **Current Phase**: Phase 1 - Core Innovation & Content
### 📍 **Current Milestone**: 1.6 - Unit Markers & Infinite Cube (Stages 1-2) ⬜ Pending

### 🔴 **TOP PRIORITIES**
| Priority | Item | Status | Points |
|----------|------|--------|--------|
| 🔴 Critical | Unit Markers & Infinite Cube (Milestone 1.6) | Ready to start | 4 pts |
| 🔴 High | Matrix Cube/Markers (Milestone 1.7) | After 1.6 | 6 pts |
| 🔴 High | Recursion Cube/Marker (Milestone 1.8) | After 1.7 | 6 pts |
| 🟡 Medium | Infinity Marker (Milestone 1.9) | After 1.8 | 5 pts |
| 🟡 Medium | Mastery (Milestone 1.10) | After 1.9 | 4 pts |
| 🟡 Medium | RPG Implementation (Milestone 1.11) | After 1.10 | 5 pts |
| 🟡 Medium | Hub Implementation (Milestone 1.12) | After 1.11 | 4 pts |
| 🟡 Medium | File Size Refactoring | 12 files over limit | 15 pts |
| 🟢 Low | Stage Content Refinement (Milestone 1.13) | After 1.12 | 5 pts |
| 🟢 Low | Visual Effects & Juice Pass (Milestone 1.14) | After 1.13 | 5 pts |
| 🟢 Low | Hub polish pass | Deferred | 2 pts |

---

## Point Estimation Scale

> **1 point ≈ 1 day of active development**  
> **5 points ≈ 1 week of active development**  
> **22 points ≈ 1 month of active development** (measured velocity)

Points reflect actual development time, not complexity. A "week gap" between commits counts as same-day start for the next commit.

---

## Phase & Milestone Summary

| Phase | Milestone | Status | Points |
|-------|-----------|--------|--------|
| **Phase 1** | 1.0 Core Game Development | ✅ Complete | 65 pts |
| | 1.1 Refine Markers | ✅ Complete | 14 pts |
| | 1.2 RPG/Progression | ✅ Complete | 12 pts |
| | 1.3 Database Enhancement | ✅ Complete | 6 pts |
| | **1.4 Stage Content Creation** | ✅ **Complete** | 20 pts |
| | **1.5 Demo Stage Content** | ✅ **Complete** | 5 pts |
| | 1.6 Unit Markers & Infinite Cube (Stages 1-2) | ⬜ Pending | 4 pts |
| | 1.7 Matrix Cube/Markers (Stages 3-5) | ⬜ Pending | 6 pts |
| | 1.8 Recursion Cube/Marker (Stages 6-8) | ⬜ Pending | 6 pts |
| | 1.9 Infinity Marker (Stages 9-10) | ⬜ Pending | 5 pts |
| | 1.10 Mastery (Stages 11-12) | ⬜ Pending | 4 pts |
| | 1.11 RPG Implementation (Attunements & Progression) | ⬜ Pending | 5 pts |
| | 1.12 Hub Implementation | ⬜ Pending | 4 pts |
| | 1.13 Stage Content Refinement & Difficulty Curve | ⬜ Pending | 5 pts |
| | 1.14 Visual Effects & Juice Pass | ⬜ Pending | 5 pts |
| **Phase 2** | 2.1-2.10 Content & Polish | ⬜ Pending | 68 pts |
| **Phase 3** | 3.1-3.8 Polish & Commercial Prep | ⬜ Pending | 55 pts |
| **Phase 4** | 4.1-4.9 Steam & Release | ⬜ Pending | 50 pts |

---

## Phase 1: Core Innovation & Content (Current Phase)
**Goal**: Implement core gameplay systems and complete foundational content  
**Estimated Total**: ~160 pts (~7-8 months at 22 pts/month)

### Milestone 1.0: Core Game Development ✅ COMPLETE
- **Target**: Establish all foundational game systems
- **Points**: 65 (measured: ~3 months active work)
- **Status**: ✅ **COMPLETE** (April-July 2025)
- **Systems Delivered**:
  - Four-tier marker system (Unit/Recursion/Matrix/Infinity)
  - Unified input system (mode keys 1-4, F for placement)
  - Basic face painting mechanics
  - Corruption/enhancement tile system
  - Recursion cube multi-hit mechanics
  - Comprehensive debug infrastructure
  - Statistics and analytics system
  - Integration testing framework
  - Wave management system with editor tools
  - Player action system with full input handling
  - Wave completion messages with progress tracking
  - Stage transition system with demo flow
  - Audio system (7+ subsystem files):
    - AudioManager.cs, AudioSourcePool.cs, AudioPlaybackSystem.cs
    - AudioVolumeController.cs, CubeAudioSystem.cs, CubeAudioConfiguration.cs
  - Production UI system:
    - PlayerActionUI.cs, StageTransitionUI.cs, WaveProgressUI.cs
    - PlayerUI.cs, StageInfoUI.cs

---

### Milestone 1.1: Refine Markers Implementation ✅ COMPLETE
- **Target**: Review and refine marker system (placement, cube spawn, cube collision/interactions) + Core gameplay systems
- **Points**: 14 (~2-3 weeks active development)
- **Status**: ✅ **COMPLETE** - All systems implemented and verified
- **Dependencies**: Milestone 1.0 ✅
- **Completed**: December 2024

#### Completed Tasks:
- [x] Define marker placement and spawning for all markers
- [x] Design/Define cube collisions for all 16 combinations (including resonance)
- [x] Unified input system (mode keys 1-4, placement key F)
- [x] CubeCollisionManager created with full collision matrix implementation
- [x] **Collision matrix code** - All 16 cube type combinations implemented
- [x] **Resonance System** - Infinity+Infinity makes all Infinity cubes phaseable (2 moves)
- [x] **Enhanced Face Painting** - Front face painting with 3-move telegraph system
- [x] **Penalty/Reward System** - Rewards and penalties based on cube escapes and achievements
- [x] **Marker Economy** - Per-stage/wave grants with inventory caps (toggleable)
- [x] Visual feedback for phaseable cubes (material swapping)
- [x] Grid telegraph shows painted face trigger location 1-3 moves ahead
- [x] Charge system coherent with economy toggle for testing

#### Implementation Details:
- **Face Painting**: Paints Front face → triggers after 3 moves (learnable rhythm)
- **Telegraph**: Progressive visual (dim→bright, small→large, slow→fast pulse)
- **Materials**: CosmicBlack (opaque), CosmicBlack_Transparent (player), CosmicBlack_Phaseable
- **Economy**: Stage grants (set inventory) + Wave grants (add to inventory) + Caps

#### Playtesting Notes:
- Resonance requires sacrificing player Infinity cube (meaningful cost)
- Face painting telegraph gives clear 3-turn warning
- Marker economy forces strategic conservation across waves

---

### Milestone 1.2: RPG/Progression ✅ COMPLETE

**Target:** Establish framework to deliver progression and RPG vibes for player

**Points:** 12 (exceeded original 10-point ideation scope with full implementation)

**Status:** ✅ **COMPLETE** - December 2024

**Planning Document:** [Milestone1.2_RPGProgressionPlan.md](CurrentFocus/Milestone1.2_RPGProgressionPlan.md)

#### Completed Deliverables:
- [x] **SaveManager** - Full persistence system (JSON, Steam Cloud ready)
- [x] **PlayerProgression** - Data structure for currency, attunements, stage progress
- [x] **AttunementManager** - 9 attunement definitions with query API
- [x] **All 9 attunements integrated** into CubeCollisionManager
- [x] **Economy wired** to GameEvents (100 shards/wave, first playthrough only)
- [x] **Hub scene foundation** - 3 clickable buildings, UI panel system
- [x] **Debug panel** for attunement testing (F12 → Player → Attunements)

#### Implementation Beyond Original Scope:
Original milestone was ideation/design docs. Delivered full working implementation:
- Concentrated Expansion: Matrix markers trigger twice
- Expanded Concentration: Recursion creates 2-tile vertical pattern
- Phaseable attunements: Same-type collisions paint wave cube faces
- Untethered: Infinity vs Unit destroys and continues (no wave join)

---

### Hub: Infinity's Axiom

| Area | Function | Unlock |
|------|----------|--------|
| Celestial Atlas | Stage selection | Always |
| Resonance Alignment Chamber | Marker attunements | Stage 3 |
| Observation Chronicle | Stats/history | Stage 3 |

---

### Marker Attunements

One attunement per marker type. Modifies cube properties, not collision matrix.

#### Matrix Attunements (Expansion Theme)

| Attunement | Effect |
|------------|--------|
| Expanded Expansion | +1 area dimensions (2x2 → 3x3) |
| Concentrated Expansion | +1 charge per tile |
| Phaseable Expansion | Matrix vs Matrix also paints wave cube face |

#### Recursion Attunements (Concentration Theme)

| Attunement | Effect |
|------------|--------|
| Concentrated Concentration | +2 charges (3 → 5) |
| Expanded Concentration | +1 tile to pattern |
| Phaseable Concentration | Recursion vs Recursion also paints wave cube face |

#### Infinity Attunements (Phaseability Theme)

| Attunement | Effect |
|------------|--------|
| Potent Matrix Paint | +1 charge on Matrix painted faces |
| Potent Recursion Paint | +1 charge on Recursion painted faces |
| Untethered | vs Unit = destroy + continue (no wave join) |

---

### Economy

| Aspect | Value |
|--------|-------|
| Currency | Axiom Shards |
| Earn | 100 shards flat per wave |
| Spend | Attunement unlocks (100-250 each) |
| Replay | No currency (anti-farming) |

---

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Attunements modify cube, not collision matrix | Players learn collision matrix once, attunements layer on top |
| Phaseable attunements enable same-type face painting | High-skill reward, extends existing mechanic |
| Locked per stage | Forces commitment, meaningful choice |
| Passives removed | Attunements provide sufficient expression |

---

### Tasks

- [x] Hub architecture design (3 areas, functions, unlock flow)
- [x] Attunement system design (9 attunements with exact values)
- [x] Economy design (earn rates, costs, pacing)
- [x] Technical prep (data structures, integration points)

---

### Milestone 1.3: Database & Configuration Enhancement ✅ COMPLETE
- **Target**: Enhance core databases for easier content creation and balance tuning
- **Points**: 6 (~1 week active development)
- **Status**: ✅ **COMPLETE** - December 2024
- **Dependencies**: Milestone 1.2 ✅
- **Planning Document**: [Milestone1.3_DatabaseEnhancement.md](CurrentFocus/Milestone1.3_DatabaseEnhancement.md)

#### Completed Deliverables

**StageDB + WaveData**
- [x] Enhanced StageData with marker grants, attunement locking, validation
- [x] Enhanced WaveData with Infinity markers, grants system, validation  
- [x] Enhanced CubeData with pre-painted faces, spawn delay
- [x] Enhanced StageDB with validation, query methods

**AttunementDatabase**
- [x] Created AttunementData ScriptableObject
- [x] Created AttunementDatabase ScriptableObject
- [x] Migrated 9 attunement definitions
- [x] Updated AttunementManager to load from DB (with fallback)

**PlayerProgression**
- [x] Added lifetime statistics (cubes captured, play time, best times)

---

### Milestone 1.4: Stage Content Creation ✅ COMPLETE
- **Target**: 12-13 stages defined with waves, 4-5 complete stages with progression
- **Points**: 20 (~4 weeks active development)
- **Status**: ✅ **COMPLETE** - December 2024
- **Dependencies**: Milestone 1.3 ✅
- **Progress**: multiple stages exist (0-3), design doc complete for all 13 stages
- **Tasks**:
  - [x] Design 13 stages (1 tutorial/demo stage and 12 game stages) - **COMPLETE** (Level Design doc updated)
  - [x] Establish stage numbering convention (Stage 0 = Tutorial, Stages 1-12 = Core game) - **COMPLETE**
  - [x] Update StageManager with IsTutorialStage property - **COMPLETE**
  - [x] Create tutorial/demo stage teaching core mechanics - **COMPLETE** (Stage 0 with highlight sequence system)
  - [x] Ensure debuggers allow easy tuning of stages/waves - **COMPLETE** (Prototyping panel exists)
  - [x] Remaining content tasks moved to Milestone 1.6 - **COMPLETE** 

---

### Milestone 1.5: Demo Stage Content ✅ COMPLETE
- **Target**: Foundational Systems + One Complete Demo Stage
- **Points**: 5 (measured: ~1 week work)
- **Status**: ✅ **COMPLETE** (December 16, 2025)
- **Tasks**:
  - [x] Further refine tutorial/demo stage and waves - **COMPLETE** (Stage 0 with highlight sequences)
  - [x] Ensure proper messaging is implemented to convey game mechanics - **COMPLETE** (Highlight sequence system)
  - [x] Wave Completion Messages - **COMPLETE** (Integrated into sequence system)
  - [x] Stage Success Transition - **COMPLETE** (Completion message with stats, K key handling, return to Splash)
  - [x] Stage/Wave Sequence Framework - **COMPLETE** (Highlight sequences with timing, triggers, validation)
  - [x] Complete configuration of waves and stage (3 waves) - **COMPLETE** (All sequences working for all 3 waves)
  - [x] Data collection on play session - **COMPLETE** (PlayerStatisticsManager tracks tutorial progress, messages, actions)
  - [x] Complete Splash to Stage loop - **COMPLETE** (Tutorial loop working, PlayerPrefs flag system)

---

### Milestone 1.6: Unit Markers & Infinite Cube (Stages 1-2)
- **Target**: Implement stages 1-2 and validate Unit marker and Infinity cube mechanics
- **Points**: 4 (~1 week active development)
- **Status**: 🔄 In Progress
- **Dependencies**: Milestone 1.5 ✅
- **Stages**: 0-2 (Unit markers, Infinite Cube)
- **Lesson**: Unit marker basics
- **Mechanics to Validate**:
  - [x] Unit marker placement and cube capture (Stage 0 validated)
  - [ ] Infinity cube avoidance and danger recognition
  - [ ] Basic movement and positioning
  - [ ] Core collision mechanics (Unit vs Unit, Unit vs Infinity)
- **Tasks**:
  - [x] Design wave configurations for stages 1-2
  - [x] Implement stages 1-2 with wave configs
  - [x] Validate Stage 0 (all systems working, playable end-to-end)
  - [ ] Validate Stages 1-2 (playtesting in progress)
  - [x] Define and Implement Penalty system
  - [x] Define and Implement Scoring System
  - [x] Fix bugs/edge cases discovered (max unit charges wave override)
  - [x] Fix out of bounds error in Stage 1 Wave 4 spawn calculation
  - [ ] Ensure stages 1-2 are playable end-to-end
- **Playtesting Issues Found**:
  - [x] **Out of bounds error**: Stage 1 Wave 4 - Fixed with bounds validation in spawn calculation
  - [ ] **Wave similarity**: Stage 0 Wave 3 and Stage 1 Wave 1 are too similar (deferred to Milestone 1.14 for tuning)
- **Note**: Tuning and balance deferred to Milestone 1.14 (Stage Content Refinement & Difficulty Curve)

---

### Milestone 1.7: Matrix Cube/Markers (Stages 3-5)
- **Target**: Implement stages 3-5 and validate Matrix cube and marker mechanics
- **Points**: 6 (~1.5 weeks active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.6
- **Stages**: 3-5 (Matrix cube/markers introduced)
- **Lesson**: Area effects and solving Infinity blocking problems
- **Mechanics to Validate**:
  - [ ] Matrix cube introduction and area capture
  - [ ] Matrix vs Infinity face painting mechanics
  - [ ] Matrix marker placement and area coverage
  - [ ] Cube marker generation from Matrix captures
  - [ ] Spatial management and area control strategies
  - [ ] Resource management (marker economy)
- **Tasks**:
  - [ ] Design wave configurations for stages 3-5
  - [ ] Implement stages 3-5 with wave configs
  - [ ] Validate Matrix cube and marker mechanics work correctly
  - [ ] Improve UI around markers
  - [ ] Assess and improve face painting implementation if needed
  - [ ] Fix bugs/edge cases discovered
  - [ ] Test area effect systems

---

### Milestone 1.8: Recursion Cube/Marker (Stages 6-8)
- **Target**: Implement stages 6-8 and validate Recursion cube and marker mechanics
- **Points**: 6 (~1.5 weeks active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.7
- **Stages**: 6-8 (Recursion cube/marker)
- **Lesson**: Multi-hit mechanics
- **Mechanics to Validate**:
  - [ ] Recursion cube multi-hit mechanics
  - [ ] Recursion marker placement and patterns
  - [ ] Chain reaction detonations
  - [ ] Multi-step strategic planning
  - [ ] Complex interaction chains
- **Tasks**:
  - [ ] Design wave configurations for stages 6-8
  - [ ] Implement stages 6-8 with wave configs
  - [ ] Validate Recursion mechanics work correctly
  - [ ] Assess and improve face painting implementation if needed
  - [ ] Fix bugs/edge cases discovered
  - [ ] Test multi-hit and chain reaction systems

---

### Milestone 1.9: Infinity Marker (Stages 9-10)
- **Target**: Implement stages 9-10 and validate Infinity marker mechanics
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.8
- **Stages**: 9-10 (Infinity Marker)
- **Lesson**: All systems
- **Mechanics to Validate**:
  - [ ] Infinity marker placement and spawning
  - [ ] Resonance mechanics (Infinity + Infinity = phaseable)
  - [ ] Infinity marker strategic value
  - [ ] All marker types working together
  - [ ] Complete marker system integration
- **Tasks**:
  - [ ] Design wave configurations for stages 9-10
  - [ ] Implement stages 9-10 with wave configs
  - [ ] Validate Infinity marker mechanics work correctly
  - [ ] Assess and improve face painting implementation if needed
  - [ ] Fix bugs/edge cases discovered
  - [ ] Test all systems in combination

---
### Milestone 1.10: RPG Implementation (Attunements & Progression)
- **Target**: Complete RPG systems - attunement UI, progression integration, economy polish
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.10
- **Note**: Framework exists from Milestone 1.2. This milestone completes the UI and integration.

#### RPG Systems Overview
**Foundation (from Milestone 1.2):**
- SaveManager - Full persistence system (JSON, Steam Cloud ready)
- PlayerProgression - Data structure for currency, attunements, stage progress
- AttunementManager - 9 attunement definitions with query API
- All 9 attunements integrated into CubeCollisionManager
- Economy wired to GameEvents (100 shards/wave, first playthrough only)
- Hub scene foundation - 3 clickable buildings, UI panel system

**This Milestone Completes:**
- Full UI implementation for all RPG systems
- Attunement unlock/purchase flow
- Stage selection interface
- Stats/history display
- Economy feedback and polish

#### Marker Attunements System
One attunement per marker type. Modifies cube properties, not collision matrix.

**Matrix Attunements (Expansion Theme):**
- Expanded Expansion: +1 area dimensions (2x2 → 3x3)
- Concentrated Expansion: +1 charge per tile
- Phaseable Expansion: Matrix vs Matrix also paints wave cube face

**Recursion Attunements (Concentration Theme):**
- Concentrated Concentration: +2 charges (3 → 5)
- Expanded Concentration: +1 tile to pattern
- Phaseable Concentration: Recursion vs Recursion also paints wave cube face

**Infinity Attunements (Phaseability Theme):**
- Potent Matrix Paint: +1 charge on Matrix painted faces
- Potent Recursion Paint: +1 charge on Recursion painted faces
- Untethered: vs Unit = destroy + continue (no wave join)

#### Economy System
| Aspect | Value |
|--------|-------|
| Currency | Axiom Shards |
| Earn | 100 shards flat per wave |
| Spend | Attunement unlocks (100-250 each) |
| Replay | No currency (anti-farming) |

#### Tasks:
- [ ] Complete attunement selection UI (Resonance Alignment Chamber panel)
  - [ ] Display all 9 attunements with descriptions
  - [ ] Show unlock status and costs
  - [ ] Implement purchase/unlock flow
  - [ ] Attunement preview/description system
  - [ ] Visual feedback for equipped attunements
- [ ] Complete stage selection UI (Celestial Atlas panel)
  - [ ] Display all available stages
  - [ ] Show stage completion status
  - [ ] Stage selection and launch functionality
  - [ ] Stage preview/description
- [ ] Complete stats/history UI (Observation Chronicle panel)
  - [ ] Display player statistics
  - [ ] Show stage completion history
  - [ ] Display attunement usage stats
  - [ ] Session history and analytics
- [ ] Polish economy integration
  - [ ] Shard earning feedback (visual/audio)
  - [ ] Purchase confirmation flows
  - [ ] Currency display updates
  - [ ] Economy balance validation
- [ ] Validate attunement effects work correctly in gameplay
  - [ ] Test all 9 attunements in actual stages
  - [ ] Verify attunement effects apply correctly
  - [ ] Validate attunement persistence across sessions

---

### Milestone 1.11: Mastery (Stages 11-12)
- **Target**: Implement final stages and validate complete mastery scenario
- **Points**: 4 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.9
- **Stages**: 11-12 (Mastery)
- **Lesson**: Complex strategies
- **Mechanics to Validate**:
  - [ ] All cube types working together
  - [ ] All marker types in combination
  - [ ] Full strategic depth and complexity
  - [ ] Complete system integration
  - [ ] Mastery-level challenge validation
- **Tasks**:
  - [ ] Design wave configurations for stages 11-12
  - [ ] Implement stages 11-12 with wave configs
  - [ ] Validate all mechanics work in synthesis
  - [ ] Fix bugs/edge cases discovered
  - [ ] Test complete mastery scenario

---

---

### Milestone 1.12: Hub Implementation
- **Target**: Complete hub scene with full functionality and scene transitions
- **Points**: 4 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.11 (RPG Implementation)
- **Note**: Hub scene foundation exists. This milestone completes functionality and polish.

#### Hub: Infinity's Axiom
The hub serves as the central navigation point between gameplay sessions.

**Hub Areas:**
| Area | Function | Unlock |
|------|----------|--------|
| Celestial Atlas | Stage selection | Always |
| Resonance Alignment Chamber | Marker attunements | Stage 3 |
| Observation Chronicle | Stats/history | Stage 3 |

**Foundation (from Milestone 1.2):**
- Hub scene exists with 3 clickable buildings
- HubBuilding component with click handling
- HubUIManager for panel management
- Basic building unlock system (checks SaveManager.Progression)

**This Milestone Completes:**
- Full scene transitions (hub ↔ stage)
- Complete visual polish
- Navigation and camera systems
- Building unlock feedback
- Complete hub loop validation

#### Tasks:
- [ ] Complete hub scene layout and visuals
  - [ ] Finalize building placement and layout
  - [ ] Visual polish for buildings and environment
  - [ ] Lighting and atmosphere
  - [ ] Building hover/click visual feedback
- [ ] Implement hub-to-stage scene transitions
  - [ ] Stage selection triggers scene load
  - [ ] Transition animations/effects
  - [ ] Stage loading with selected stage index
  - [ ] Error handling for invalid selections
- [ ] Implement stage-to-hub return flow
  - [ ] Return to hub after stage completion
  - [ ] Return to hub after stage failure/quit
  - [ ] Preserve hub state across transitions
  - [ ] Update hub UI based on progression changes
- [ ] Complete building unlock system
  - [ ] Unlock Resonance Alignment Chamber after Stage 3
  - [ ] Unlock Observation Chronicle after Stage 3
  - [ ] Visual feedback for locked buildings
  - [ ] Unlock animation/celebration
- [ ] Hub navigation and camera system
  - [ ] Camera positioning and movement
  - [ ] Navigation between buildings (if needed)
  - [ ] Smooth camera transitions
- [ ] Hub UI polish and feedback
  - [ ] Panel animations (open/close)
  - [ ] Building tooltips/descriptions
  - [ ] Currency display in hub
  - [ ] Visual feedback for all interactions
- [ ] Validate complete hub → stage → hub loop
  - [ ] Test full flow: Hub → Select Stage → Play → Return to Hub
  - [ ] Verify progression updates reflect in hub
  - [ ] Test attunement changes persist
  - [ ] Validate save/load integration

---

### Milestone 1.13: Determine any brainstorm implementation
- **Target**: Implement, balance, tune, and establish mechanics taken from brainstorm (if any are deemed worth complexity)
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.11
- **Tasks**:
  - [ ] Playtest select stages for balance and feel improvements
  - [ ] Validate stages feel mor fun or rewarding
- **Note**: Focus on play experience and fun.

---

### Milestone 1.14: Stage Content Refinement & Difficulty Curve
- **Target**: Balance, tune, and establish proper difficulty progression across all stages
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.12 (Hub Implementation)
- **Tasks**:
  - [ ] Playtest all stages (0-12) for balance and feel
  - [ ] Implement proper difficulty curve across all stages
  - [ ] Refine wave configurations based on playtesting feedback
  - [ ] Balance marker economy and cube spawn rates
  - [ ] Validate stage progression feels rewarding
- **Note**: Focus on tuning and balance. Mechanics should be validated in 1.6-1.10.

---

### Milestone 1.15: Visual Effects & Juice Pass
- **Target**: Core visual feedback and effects for gameplay actions
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.13 (can start in parallel with refinement)
- **Note**: Focuses on visual effects and feedback. Gameplay feel polish comes in 2.6.
- **Tasks**:
  - [ ] Improved particle effects for all major actions
  - [ ] Screen shake and juice for impacts
  - [ ] Enhanced face painting visualization
  - [ ] Tile state change visual effects
  - [ ] Marker placement/detonation polish
  - [ ] Animation/scripted sequence placeholders for key events
  - [ ] Event hooks for major gameplay moments

---

## Phase 2: Content Completion & First Polish Pass
**Goal**: Complete all content and achieve first polish level  
**Estimated Total**: ~48 pts

**Note**: Stage content completion moved to Milestone 1.6

### Milestone 2.1: Save/Load & Progress Persistence
- **Points**: 5 (~1 week)
- Complete save/load system for game progress
- **Note**: SaveManager exists, this milestone completes/enhances it

**Note**: Interactive Tutorial System completed in Milestone 1.5

### Milestone 2.2: Stage Progression & Unlock System
- **Points**: 5 (~1 week)
- Stage unlock system and progression flow
- **Dependencies**: Milestone 2.1 (needs save system for progression persistence)

### Milestone 2.3: Menu & UI Systems
- **Points**: 8 (~1.5 weeks)
- **Dependencies**: Milestone 2.2 (UI needs progression logic to display unlocks)
- **Tasks**:
  - [ ] Pause menu (resume, settings, quit to menu)
  - [ ] Stage select screen (requires unlock system from 2.2)
  - [ ] Settings persistence (audio, display)
  - [ ] Stage results/rating display (requires progression from 2.2)

### Milestone 2.4: Cosmic Theme Integration
- **Points**: 7 (~1.5 weeks)
- Deliver on aesthetic vision

### Milestone 2.5: Audio Content & Polish
- **Points**: 5 (~1 week)
- Add actual audio content to completed system

### Milestone 2.6: First Polish Pass - Gameplay
- **Points**: 8 (~1.5 weeks)
- First comprehensive polish pass focusing on gameplay feel

### Milestone 2.7: Meta-Progression Systems
- **Points**: 5 (~1 week)
- Player retention, achievement, and expressive progression

### Milestone 2.8: Analytics Dashboard
- **Points**: 5 (~1 week)
- **Goal**: Simple web tool to visualize player stats JSON for playtesting insights
- **Tasks**:
  - [ ] Parse InfinityQube stats JSON format (DemoStats, EmergencyStats, player_statistics)
  - [ ] Session overview (duration, completion status, APM over time)
  - [ ] Movement heatmap visualization
  - [ ] Marker usage breakdown (placement timing, trigger success rate)
  - [ ] Cube capture/escape timeline
  - [ ] Tutorial message read times and skip rates
  - [ ] Export/share session summaries
- **Tech**: Static HTML/JS site (no backend needed)

---

## Phase 3: Polish, Refinement & Commercial Prep
**Goal**: Achieve commercial polish level and prepare for Steam release  
**Estimated Total**: ~55 pts

### Milestone 3.1: Second Polish Pass - Balance & Tuning
- **Points**: 10
- Comprehensive balance pass and difficulty tuning

### Milestone 3.2: UI/UX Polish Pass
- **Points**: 7
- Polish all user interfaces and user experience

### Milestone 3.3: Input & Display Systems
- **Points**: 8
- **Tasks**:
  - [ ] Controller support (full gamepad playability)
  - [ ] Keyboard/controller rebinding
  - [ ] Resolution and display mode options
  - [ ] UI scaling for different resolutions

### Milestone 3.4: Performance Optimization
- **Points**: 5
- Optimize performance for commercial release

### Milestone 3.5: Comprehensive QA & Bug Fixing
- **Points**: 8
- Find and fix all critical bugs

### Milestone 3.6: Accessibility & Localization Prep
- **Points**: 5
- Ensure game is accessible and ready for localization

### Milestone 3.7: Business & Legal Setup
- **Points**: 5
- **Tasks**:
  - [ ] Steamworks Partner account registration
  - [ ] Privacy Policy creation
  - [ ] EULA preparation
  - [ ] IARC age rating questionnaire
  - [ ] Business entity decisions (if needed)

### Milestone 3.8: Demo Build (Steam Next Fest)
- **Points**: 7
- **Tasks**:
  - [ ] Define demo scope (stages 1-3 or subset)
  - [ ] Demo-specific intro/outro messaging
  - [ ] Wishlist CTA integration
  - [ ] Demo build pipeline
  - [ ] Apply to Steam Next Fest (4-6 weeks before event)

---

## Phase 4: Steam Integration & Release
**Goal**: Platform integration, store presence, and commercial launch  
**Estimated Total**: ~50 pts

### Milestone 4.1: Steam SDK Integration
- **Points**: 7
- **Tasks**:
  - [ ] Steamworks.NET integration
  - [ ] Steam initialization and error handling
  - [ ] Steam overlay support
  - [ ] Build pipeline for Steam uploads

### Milestone 4.2: Steam Achievements & Stats
- **Points**: 5
- **Tasks**:
  - [ ] Define achievement list (10-15 achievements MVP)
  - [ ] Implement achievement triggers
  - [ ] Steam stats tracking integration

### Milestone 4.3: Steam Cloud Saves
- **Points**: 5
- Implement Steam cloud save functionality

### Milestone 4.4: Steam Store Page & Assets
- **Points**: 8
- **Tasks**:
  - [ ] Capsule images (header, library, small, hero)
  - [ ] 5+ high-quality screenshots
  - [ ] Store description and tags
  - [ ] System requirements
  - [ ] "Coming Soon" page live (start wishlist building)

### Milestone 4.5: Launch Trailer
- **Points**: 5
- **Tasks**:
  - [ ] Gameplay capture
  - [ ] Trailer editing (30-60 seconds)
  - [ ] Trailer thumbnail
  - [ ] Upload to Steam and YouTube

### Milestone 4.6: Final Polish & Release Candidate
- **Points**: 7
- Final polish pass and release candidate build

### Milestone 4.7: Beta Testing (Steam Playtest)
- **Points**: 5
- **Tasks**:
  - [ ] Steam Playtest branch setup
  - [ ] Recruit beta testers
  - [ ] Feedback collection and triage
  - [ ] Critical bug fixes

### Milestone 4.8: Launch Preparation
- **Points**: 5
- **Tasks**:
  - [ ] Press/streamer key distribution
  - [ ] Launch day announcement prep
  - [ ] Community response plan
  - [ ] Day-1 patch pipeline ready

### Milestone 4.9: Post-Launch Buffer
- **Points**: 3
- **Tasks**:
  - [ ] Day-1/Week-1 patch releases
  - [ ] Review monitoring and response
  - [ ] Critical bug triage

---

## Critical Path

```
Milestone 1.0 ✅ → 1.1 ✅ → 1.2 🔄 → 1.3 → 1.4 → 2.x → 3.x → 4.x → Release
                            ↑
                      YOU ARE HERE
                            │
                      Hub Architecture
                      Attunement System
                      Progression Economy
                      Technical Prep
```

---

## Minimum Viable Product (MVP) Definition

**Purpose**: Define the shippable floor if timeline compresses. This gives permission to cut scope without compromising core identity.

### MVP Requirements (Must Ship)

| Element | Full Scope | MVP Scope | Notes |
|---------|------------|-----------|-------|
| **Stages** | 12-13 | 8 (incl. tutorial) | Quality over quantity |
| **Cube Types** | All 4 | All 4 | Core identity |
| **Marker Types** | All 4 | All 4 | Core identity |
| **Collision Matrix** | Full 16 combos | Full 16 combos | Core differentiation |
| **Face Painting** | Full system | Basic (Unit trigger only) | Can simplify |
| **Tutorial** | Interactive sequences | Highlight sequences | Framework complete |
| **Achievements** | 20+ | 10 basic | Reduce scope |
| **Controller** | Full support | Full support | Steam expectation |

### MVP Cuts (Can Defer to Post-Launch)

| Feature | Reason to Cut |
|---------|---------------|
| Resonance System | Advanced mechanic, not core loop |
| RPG/Progression Systems | Nice-to-have, not core puzzle |
| Localization | English-only is acceptable for launch |
| Leaderboards | Can add post-launch |
| Advanced accessibility | Basic options sufficient for launch |

### MVP Quality Bar

- All 4 cube types feel distinct and purposeful
- Player understands core loop within 2 minutes
- 60 FPS on mid-range hardware
- No game-breaking bugs
- Complete gameplay loop (start → stages → completion)
- Save/load works reliably
- Controller and keyboard both work

---

## Effort Summary

| Phase | Focus | Points | Status |
|-------|-------|--------|--------|
| Phase 1 | Core Innovation & Content | 160 pts | 🔄 In Progress |
| Phase 2 | Content Completion & Polish | 48 pts | ⬜ Pending |
| Phase 3 | Polish & Commercial Prep | 55 pts | ⬜ Pending |
| Phase 4 | Steam Integration & Release | 50 pts | ⬜ Pending |
| **TOTAL** | | **313 pts** | |

**Completed**: 122 pts (Milestone 1.0 + 1.1 + 1.2 + 1.3 + 1.4 + 1.5)  
**Remaining**: ~191 pts

**Note**: Phases can overlap; calendar time will vary based on availability.

---

## Development Velocity

| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** (active periods) |
| **Completed Points** | 122 pts |
| **Remaining Points** | ~191 pts |
| **Point Definition** | 1 pt ≈ 1 day active work |
| **Week Definition** | 5 pts ≈ 1 week active work |
| **Measurement Rule** | Gap < 1 week = same-day continuation |

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline** ✅ Framework exists
2. **Technical Debt Management** ✅ System operational (12 file size violations)
3. **Community Building** - Development blog, Discord setup
4. **Analytics Dashboard** - Can start early to support playtesting (Milestone 2.10)

---

## Risk Mitigation

| Risk | Mitigation | Fallback |
|------|------------|----------|
| Velocity Sustainability | Weekly tracking | Conservative 15 pts/month |
| Content Creation Scope | Quality over quantity | Launch with 8-10 stages (MVP) |
| Scope Creep | Feature freeze after Phase 1 | Focus on polish |
| File Size Violations | Phased refactoring | Prioritize critical managers |
| Store Page Too Late | Start assets in Phase 3 | Simple capsules, iterate |
| Launch Without Wishlists | Demo in Next Fest | Direct community outreach |

---

## Steam Release Guardrails

**Checkpoints to ensure commercial readiness:**

| Checkpoint | When | Gate |
|------------|------|------|
| **MVP Scope Locked** | End of Phase 1 | Know exactly what's shipping |
| **Feature Freeze** | Start of Phase 3 | No new features, polish only |
| **Business/Legal Complete** | Mid Phase 3 | Steamworks account, policies ready |
| **Store Page Live** | End of Phase 3 | "Coming Soon" visible, wishlisting active |
| **Controller Verified** | End of Phase 3 | Full gamepad playability confirmed |
| **Demo Submitted** | Phase 3 (if Next Fest) | Apply 4-6 weeks before event |
| **Content Freeze** | Start of Phase 4 | No new stages, bugs only |
| **Beta Complete** | Mid Phase 4 | Critical feedback addressed |
| **Release Candidate** | End of 4.6 | Build that could ship |
| **Launch** | After 4.8 | Store page updated, build live |

### Steam Store Requirements Checklist

**Required for Store Page:**
- [ ] Header capsule (460x215, 920x430)
- [ ] Small capsule (231x87)
- [ ] Main capsule (616x353)
- [ ] Library capsule (600x900) - for library view
- [ ] Hero image (3840x1240) - optional but recommended
- [ ] 5+ screenshots (1920x1080 recommended)
- [ ] Short description (< 300 chars)
- [ ] Full description
- [ ] System requirements
- [ ] Privacy policy URL
- [ ] IARC age rating

**Required for Launch:**
- [ ] Gameplay trailer
- [ ] All store assets finalized
- [ ] Build uploaded and tested
- [ ] Achievements configured
- [ ] Cloud saves tested
- [ ] Pricing set
- [ ] Release date set

---

## Success Metrics

- **Fundamentals**: 90% understand core loop in < 2 minutes
- **Performance**: Consistent 60 FPS
- **Session Length**: Average > 20 minutes
- **Stage Completion**: >70% for early stages

---

**Related Documents**:
- [Milestone Tracking](MilestoneTracking.md) - Historical record
- [Development Velocity](../Technical%20Doc/DevelopmentVelocity.md) - Detailed metrics
- [Technical Debt](../Technical%20Doc/TechnicalDebt.md) - Debt tracking
- [Technical Critiques](../Technical%20Doc/TechnicalCritiques.md) - Code analysis

---

*Last Updated: December 14, 2025*  
*Current Phase: Phase 1 - Core Innovation & Content*  
*Current Milestone: 1.4 - Stage Content Creation*  
*Total Points: 297 | Completed: ~100 | Remaining: ~197*
