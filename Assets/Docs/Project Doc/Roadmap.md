# Development Roadmap

Document Purpose:
This is the definitive, actionable guide for what to work on next, in priority order.
For a historical record of completed milestones, see `MilestoneTracking.md`.

Overview:
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

Note:
Timeline dates are applied only upon completion. Forward-looking dates are not projected.

---

## Points Policy (Option C: Est + Act, strictly separated)

### EstPts (Estimate Points)
- Planning estimate of active development time for the milestone.
- Used for sequencing, prioritization, and estimating remaining work.

### ActPts (Actual Points)
- Actual active development time recorded after completion.
- Used for velocity measurement and historical analysis.

Rules:
- **Phase/project totals must be reported separately** for EstPts and ActPts.
- **Do not mix EstPts and ActPts in a single total** (e.g., “Completed + Remaining” totals must specify whether they are Est-only or Act-only).
- If EstPts was not tracked historically for a completed milestone, set EstPts = ActPts by default.

Point scale:
- 1 pt ≈ 1 day of active development
- 5 pts ≈ 1 week of active development
- Historical velocity (ActPts): ~22 pts/month (active periods)

Measurement rule (ActPts):
- A “week gap” between commits counts as same-day continuation for the next commit (active-time accounting).

---

## Current Status
Current Phase: Phase 1 — Core Innovation & Content  
Current Milestone: 1.8 — Recursion Cube/Marker (Stages 6–8) ⬜ Pending

---

## Top Priorities (Execution Order)

| Priority | Item | Status | EstPts | ActPts | Notes |
|---|---|---|---:|---:|---|
| DONE | Matrix Cube/Markers (Milestone 1.7) | ✅ Complete | 6 | 6 | |
| DONE | Hub Implementation (Milestone 1.12) | ✅ Complete | 4 | 4 | |
| CRITICAL | Recursion Cube/Marker (Milestone 1.8) | ⬜ Ready to start | 6 | — | |
| MEDIUM | Market Analysis Implementation (Milestone 1.17) | ⬜ After 1.8 | 4 | — | Based on `MarketAnalysis.md` |
| MEDIUM | Infinity Marker (Milestone 1.9) | ⬜ After 1.17 | 5 | — | |
| MEDIUM | Mastery (Milestone 1.10) | ⬜ After 1.9 | 4 | — | |
| MEDIUM | RPG Implementation (Milestone 1.11) | ⬜ After 1.10 | 5 | — | Added context moved to `Milestone_1.11_RPG_Context.md` |
| MEDIUM | File Size Refactoring (Milestone 1.TD) | ⬜ Parallel / Phase 1 | 15 | — | 12 files over limit; full resolution details in `TechnicalDebt.md` |
| DONE | Advanced Grid System (Milestone 1.13) | ✅ Complete | 5 | 5 | |
| DONE | Scenario Testing Framework (Milestone 1.16) | ✅ Complete | 3 | 3 | |
| LOW | Stage Content Refinement (Milestone 1.14) | ⬜ After 1.13 | 5 | — | |
| LOW | Visual Effects & Juice Pass (Milestone 1.15) | ⬜ After 1.14 | 5 | — | |

---

## Phase & Milestone Summary (Est vs Act)

| Phase | Milestone | Status | EstPts | ActPts |
|---|---|---|---:|---:|
| Phase 1 | 1.0 Core Game Development | ✅ Complete | 65 | 65 |
|  | 1.1 Refine Markers | ✅ Complete | 14 | 14 |
|  | 1.2 RPG/Progression | ✅ Complete | 12 | 12 |
|  | 1.3 Database Enhancement | ✅ Complete | 6 | 6 |
|  | 1.4 Stage Content Creation | ✅ Complete | 20 | 20 |
|  | 1.5 Demo Stage Content | ✅ Complete | 5 | 5 |
|  | 1.6 Unit Markers & Infinite Cube (Stages 1–2) | ✅ Complete | 4 | 4 |
|  | 1.7 Matrix Cube/Markers (Stages 3–5) | ✅ Complete | 6 | 6 |
|  | 1.8 Recursion Cube/Marker (Stages 6–8) | ⬜ Pending | 6 | — |
|  | 1.17 Market Analysis Implementation | ⬜ Pending | 4 | — |
|  | 1.9 Infinity Marker (Stages 9–10) | ⬜ Pending | 5 | — |
|  | 1.10 Mastery (Stages 11–12) | ⬜ Pending | 4 | — |
|  | 1.11 RPG Implementation (UI + Integration) | ⬜ Pending | 5 | — |
|  | 1.12 Hub Implementation | ✅ Complete | 4 | 4 |
|  | 1.13 Advanced Grid System (Multi-Segment Stages) | ✅ Complete | 5 | 5 |
|  | 1.14 Stage Content Refinement & Difficulty Curve | ⬜ Pending | 5 | — |
|  | 1.15 Visual Effects & Juice Pass | ⬜ Pending | 5 | — |
|  | 1.16 Scenario Testing Framework | ✅ Complete | 3 | 3 |
|  | 1.TD File Size Refactoring | ⬜ Pending | 15 | — |
| Phase 2 | 2.1–2.8 Content & Polish | ⬜ Pending | 48 | — |
| Phase 3 | 3.1–3.8 Polish & Commercial Prep | ⬜ Pending | 55 | — |
| Phase 4 | 4.1–4.9 Steam & Release | ⬜ Pending | 50 | — |

---

## Effort Summary (Est totals vs Act totals)

| Scope | EstPts Total | ActPts Completed |
|---|---:|---:|
| Phase 1 | 193 | 144 |
| Phase 2 | 48 | — |
| Phase 3 | 55 | — |
| Phase 4 | 50 | — |
| **TOTAL** | **346** | **144** |

Derived (Est-only):
- Remaining EstPts: 346 - 144 = 202

Derived (time heuristic, not a projection):
- If historical velocity holds (~22 ActPts/month), Remaining EstPts (202) implies ~9 active months of work.

---

# Phase 1: Core Innovation & Content (Current Phase)

Goal: Implement core gameplay systems and complete foundational content.  
Phase 1 EstPts Total: 193  
Phase 1 ActPts Completed (so far): 144

---

## Milestone 1.0: Core Game Development ✅ COMPLETE
Target: Establish all foundational game systems  
Points: Est 65 | Act 65  
Status: ✅ COMPLETE  
Completed: April–July 2025

Systems Delivered:
- Four-tier marker system (Unit/Recursion/Matrix/Infinity)
- Unified input system (mode keys 1–4, F for placement)
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

## Milestone 1.1: Refine Markers Implementation ✅ COMPLETE
Target: Review and refine marker system (placement, cube spawn, collisions/interactions) + core gameplay systems  
Points: Est 14 | Act 14  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.0 ✅  
Completed: December 2024

Completed Tasks:
[x] Define marker placement and spawning for all markers  
[x] Design/Define cube collisions for all 16 combinations (including resonance)  
[x] Unified input system (mode keys 1–4, placement key F)  
[x] CubeCollisionManager created with full collision matrix implementation  
[x] Collision matrix code - All 16 cube type combinations implemented  
[x] Resonance System - Infinity+Infinity makes all Infinity cubes phaseable (2 moves)  
[x] Enhanced Face Painting - Front face painting with 3-move telegraph system  
[x] Penalty/Reward System - Rewards and penalties based on cube escapes and achievements  
[x] Marker Economy - Per-stage/wave grants with inventory caps (toggleable)  
[x] Visual feedback for phaseable cubes (material swapping)  
[x] Grid telegraph shows painted face trigger location 1–3 moves ahead  
[x] Charge system coherent with economy toggle for testing

Implementation Details:
- Face Painting: Paints front face → triggers after 3 moves (learnable rhythm)
- Telegraph: progressive visual (dim→bright, small→large, slow→fast pulse)
- Materials: CosmicBlack (opaque), CosmicBlack_Transparent (player), CosmicBlack_Phaseable
- Economy: stage grants (set inventory) + wave grants (add to inventory) + caps

Playtesting Notes:
- Resonance requires sacrificing player Infinity cube (meaningful cost)
- Face painting telegraph gives clear 3-turn warning
- Marker economy forces strategic conservation across waves

---

## Milestone 1.2: RPG/Progression ✅ COMPLETE
Target: Establish framework to deliver progression and RPG vibes for player  
Points: Est 12 | Act 12  
Status: ✅ COMPLETE  
Completed: December 2024  
Planning Document: Milestone1.2_RPGProgressionPlan.md

Completed Deliverables:
[x] SaveManager - Full persistence system (JSON, Steam Cloud ready)  
[x] PlayerProgression - Data structure for currency, attunements, stage progress  
[x] AttunementManager - 9 attunement definitions with query API  
[x] All 9 attunements integrated into CubeCollisionManager  
[x] Economy wired to GameEvents (100 shards/wave, first playthrough only)  
[x] Hub scene foundation - 3 clickable buildings, UI panel system  
[x] Debug panel for attunement testing (F12 → Player → Attunements)

---

## Milestone 1.3: Database & Configuration Enhancement ✅ COMPLETE
Target: Enhance core databases for easier content creation and balance tuning  
Points: Est 6 | Act 6  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.2 ✅  
Completed: December 2024  
Planning Document: Milestone1.3_DatabaseEnhancement.md

Completed Deliverables:
StageDB + WaveData  
[x] Enhanced StageData with marker grants, attunement locking, validation  
[x] Enhanced WaveData with Infinity markers, grants system, validation  
[x] Enhanced CubeData with pre-painted faces, spawn delay  
[x] Enhanced StageDB with validation, query methods

AttunementDatabase  
[x] Created AttunementData ScriptableObject  
[x] Created AttunementDatabase ScriptableObject  
[x] Migrated 9 attunement definitions  
[x] Updated AttunementManager to load from DB (with fallback)

PlayerProgression  
[x] Added lifetime statistics (cubes captured, play time, best times)

---

## Milestone 1.4: Stage Content Creation ✅ COMPLETE
Target: 12–13 stages defined with waves, 4–5 complete stages with progression  
Points: Est 20 | Act 20  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.3 ✅  
Completed: December 2024

Tasks:
[x] Design 13 stages (Stage 0 tutorial + Stages 1–12 core)  
[x] Establish stage numbering convention  
[x] Update StageManager with IsTutorialStage property  
[x] Create tutorial/demo stage with highlight sequence system  
[x] Ensure debuggers allow easy tuning of stages/waves  
[x] Remaining content tasks moved to Milestone 1.6

---

## Milestone 1.5: Demo Stage Content ✅ COMPLETE
Target: Foundational systems + one complete demo stage  
Points: Est 5 | Act 5  
Status: ✅ COMPLETE  
Completed: December 16, 2025

Tasks:
[x] Refine tutorial/demo stage and waves  
[x] Messaging to convey mechanics (highlight sequence system)  
[x] Wave completion messages  
[x] Stage success transition  
[x] Stage/Wave sequence framework  
[x] Complete configuration of waves and stage (3 waves)  
[x] Data collection on play session  
[x] Complete Splash → Stage loop

---

## Milestone 1.6: Unit Markers & Infinite Cube (Stages 1–2) ✅ COMPLETE
Target: Implement stages 1–2 and validate Unit marker and Infinity cube mechanics  
Points: Est 4 | Act 4  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.5 ✅  
Stages: 0–2  
Lesson: Unit marker basics

Mechanics to Validate:
[x] Unit marker placement and cube capture  
[x] Infinity cube avoidance and danger recognition  
[x] Basic movement and positioning  
[x] Core collision mechanics (Unit vs Unit, Unit vs Infinity)

Tasks:
[x] Design wave configurations for stages 1–2  
[x] Implement stages 1–2 with wave configs  
[x] Validate Stage 0 and Stages 1–2 end-to-end  
[x] Penalty system  
[x] Scoring system  
[x] Fix discovered bugs/edge cases

Note:
- Wave similarity issues deferred to Milestone 1.14.

---

## Milestone 1.7: Matrix Cube/Markers (Stages 3–5) ✅ COMPLETE
Target: Implement stages 3–5 and validate Matrix cube and marker mechanics  
Points: Est 6 | Act 6  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.6 ✅  
Stages: 3–5  
Completed: January 2026

Lesson: Area effects and solving Infinity blocking problems

Mechanics Validated:
[x] Matrix cube introduction and area capture  
[x] Matrix marker placement and area coverage  
[x] Cube marker generation from Matrix captures  
[x] Spatial management and area control strategies  
[x] Resource management (marker economy)

Deferred:
- UI improvements (Milestone 1.15)
- Visual feedback enhancements (Phase 2.6)

---

## Milestone 1.8: Recursion Cube/Marker (Stages 6–8)
Target: Implement stages 6–8 and validate Recursion cube/marker mechanics (swap repositioning)  
Points: Est 6 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.7  
Stages: 6–8  
Lesson: Repositioning via + pattern swap (Recursion does not capture; it rearranges). See `RecursionRedesign_SwapMechanic.md`.

Mechanics to Validate:
[ ] Recursion cube collision creates + swap marker at collision point  
[ ] Cardinal neighbors swap (North↔South, West↔East)  
[ ] Player agency: 1 charge = row OR column (player chooses); 2 charges = each independently  
[ ] Swap markers only where a capturable (Unit) cube exists; Recursion creates survival paths by breaking Infinity walls  
[ ] Multi-step strategic planning (read wave → identify ∞ walls → sacrifice Units for swaps → create gaps → Matrix capture)

Tasks:
[ ] Design wave configurations for stages 6–8 (swap-focused puzzles)  
[ ] Implement stages 6–8 with wave configs  
[ ] Implement + swap collision results (Unit vs R, Recursion vs U, Recursion vs R per design doc)  
[ ] Validate Recursion swap mechanics work correctly  
[ ] Fix bugs/edge cases discovered  
[ ] Test swap repositioning and Infinity-wall-breaking scenarios

---

## Milestone 1.17: Market Analysis Implementation
Target: Implement priority recommendations from Market Analysis (product-market fit, replayability, positioning)  
Points: Est 4 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.8  
Planning Reference: `MarketAnalysis.md`

Scope (from Market Analysis priority recommendations):
- **Genre identity**: Decide and document primary comparable (action-puzzle vs tactical puzzle); align messaging and pacing.
- **Level grades**: Add per-stage grade system (e.g. S/A/B/C or 3-star) based on efficiency, mistakes, time.
- **One meta-progression hook**: Add at least one replayability hook (e.g. unlocks, cosmetics, per-stage/aggregate leaderboards, or daily/weekly challenges).
- **"Aha!" moment**: Document or prototype one mechanic that creates emergent discovery (e.g. face painting subversion, resonance chains, turning the wave system against itself).

Tasks:
[ ] Genre identity decision documented; roadmap/messaging updated  
[ ] Level grade system designed and implemented (data + UI)  
[ ] One meta-progression or replayability feature implemented  
[ ] "Aha!" moment identified or prototyped and documented  
[ ] MarketAnalysis.md cross-references and summary updated

---

## Milestone 1.9: Infinity Marker (Stages 9–10)
Target: Implement stages 9–10 and validate Infinity marker mechanics  
Points: Est 5 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.17  
Stages: 9–10  
Lesson: Full system integration

Mechanics to Validate:
[ ] Infinity marker placement and spawning  
[ ] Resonance mechanics (Infinity + Infinity = phaseable) validated and tuned for clarity  
[ ] Infinity marker strategic value  
[ ] All marker types working together  
[ ] Complete marker system integration

Tasks:
[ ] Design wave configurations for stages 9–10  
[ ] Implement stages 9–10 with wave configs  
[ ] Validate Infinity marker mechanics work correctly  
[ ] Assess and improve face painting implementation if needed  
[ ] Fix bugs/edge cases discovered  
[ ] Test all systems in combination

---

## Milestone 1.10: Mastery (Stages 11–12)
Target: Implement final stages and validate complete mastery scenario  
Points: Est 4 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.9  
Stages: 11–12  
Lesson: Complex strategies

Mechanics to Validate:
[ ] All cube types working together  
[ ] All marker types in combination  
[ ] Full strategic depth and complexity  
[ ] Complete system integration  
[ ] Mastery-level challenge validation

Tasks:
[ ] Design wave configurations for stages 11–12  
[ ] Implement stages 11–12 with wave configs  
[ ] Validate all mechanics work in synthesis  
[ ] Fix bugs/edge cases discovered  
[ ] Test complete mastery scenario

---

## Milestone 1.11: RPG Implementation (Attunements & Progression)
Target: Complete RPG systems — attunement UI, progression integration, economy polish  
Points: Est 5 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.10  
Stages:  
Lesson: Progression adds expression without requiring a collision-matrix relearn.

Mechanics to Validate:
[ ] Unlock/purchase/equip flow works end-to-end and persists across sessions  
[ ] Attunement effects apply correctly in gameplay stages (not only debug)  
[ ] Hub panels (attunements, stage select, stats) are fully functional  
[ ] Economy feedback is clear (earn/spend, confirmations, currency display)

Tasks:
[ ] Complete attunement selection UI (Resonance Alignment Chamber panel)  
[ ] Implement purchase/unlock flow  
[ ] Visual feedback for equipped attunements  
[ ] Complete stage selection UI (Celestial Atlas panel)  
[ ] Complete stats/history UI (Observation Chronicle panel)  
[ ] Polish economy integration (earn/spend feedback + persistence)  
[ ] Validate all 9 attunements in actual stages  
[ ] Verify persistence across sessions (fresh save and existing save)

Added Context:
- [Milestone 1.11 RPG Context](Milestone_1.11_RPG_Context.md) (foundation + attunement definitions + UI expectations)

---

## Milestone 1.12: Hub Implementation ✅ COMPLETE
Target: Complete hub scene with full functionality and scene transitions  
Points: Est 4 | Act 4  
Status: ✅ COMPLETE  
Completed: January 2026

Note:
Hub consolidated into Stage scene as Stage 100 (Infinity’s Axiom). RPG UI polish deferred to 1.11.

(Details unchanged; see prior section for full deliverables.)

---

## Milestone 1.13: Advanced Grid System (Multi-Segment Stages) ✅ COMPLETE
Target: Implement multi-segment grid system for advanced stage layouts (L-shape, etc.)  
Points: Est 5 | Act 5  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.12  
Completed: January 2026

(Details unchanged; see prior section for full deliverables.)

---

## Milestone 1.14: Stage Content Refinement & Difficulty Curve
Target: Balance, tune, and establish proper difficulty progression across all stages  
Points: Est 5 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.13 ✅

Tasks:
[ ] Playtest all stages (0–12) for balance and feel  
[ ] Implement proper difficulty curve across all stages  
[ ] Refine wave configurations based on playtesting feedback  
[ ] Balance marker economy and cube spawn rates  
[ ] Validate stage progression feels rewarding

Note:
Mechanics should be validated in 1.6–1.10; this is tuning and redundancy removal.

---

## Milestone 1.15: Visual Effects & Juice Pass
Target: Core visual feedback and effects for gameplay actions  
Points: Est 5 | Act —  
Status: ⬜ Pending  
Dependencies: Milestone 1.14

Tasks:
[ ] Improved particle effects for all major actions  
[ ] Screen shake and juice for impacts  
[ ] Enhanced face painting visualization  
[ ] Tile state change visual effects  
[ ] Marker placement/detonation polish  
[ ] Animation/scripted sequence placeholders for key events  
[ ] Event hooks for major gameplay moments

---

## Milestone 1.16: Scenario Testing Framework ✅ COMPLETE
Target: Automated testing system for validating gameplay scenarios  
Points: Est 3 | Act 3  
Status: ✅ COMPLETE  
Dependencies: Milestone 1.13 ✅  
Completed: January 2026

(Details unchanged; see prior section for full deliverables.)

---

## Milestone 1.TD: File Size Refactoring
Target: Reduce file size violations and improve iteration safety  
Points: Est 15 | Act —  
Status: ⬜ Pending  
Dependencies:  
Stages:  
Lesson: Keep iteration velocity high by preventing monolith drift.

Mechanics to Validate:
[ ] Refactors do not change gameplay behavior (validated via smoke play + scenario tests)

Tasks:
[ ] Address 12 files over limit (Phase 1 target)  
[ ] Extract stable subsystems (managers, configuration, orchestration)  
[ ] Add/extend scenario tests for refactor-touched flows  
[ ] Update `TechnicalDebt.md` with before/after notes and new ownership boundaries

---
## Phase 2: Content Completion & First Polish Pass
**Goal**: Complete all content and achieve first polish level  
**Estimated Total**: ~48 pts

**Note**: Stage content completion moved to Milestone 1.6

### Milestone 2.1: Save/Load & Progress Persistence
- **Points**: 5 (~1 week)
- Complete save/load system for game progress
- **Note**: SaveManager exists, this milestone completes/enhances it
- Remove obsolete marker/cube terminology aliases (see [TechnicalDebt.md](../Technical%20Doc/TechnicalDebt.md) §1)

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
Milestone 1.0 ✅ → 1.1 ✅ → … → 1.7 ✅ → 1.12 ✅ → 1.13 ✅ → 1.16 ✅ → 1.8 🔄 → 1.9 → 1.10 → 1.11 → 2.x → 3.x → 4.x → Release
                                                                              ↑
                                                                        YOU ARE HERE
                                                                              │
                                                                        Recursion Cube/Marker
                                                                        (Stages 6-8, swap mechanic)
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
| RPG/Progression Systems | Nice-to-have, not core puzzle |
| Localization | English-only is acceptable for launch |
| Leaderboards | Can add post-launch |
| Advanced accessibility | Basic options sufficient for launch |

**Note**: Resonance (Infinity + Infinity → phaseable) is in MVP scope but not defined as of now.

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
| **TOTAL** | | **316 pts** | |

**Completed**: 140 pts (Milestones 1.0-1.7, 1.12, 1.13, 1.16)  
**Remaining**: ~176 pts

**Note**: Phases can overlap; calendar time will vary based on availability.

---

## Development Velocity

| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** (active periods) |
| **Completed Points** | 140 pts |
| **Remaining Points** | ~176 pts |
| **Point Definition** | 1 pt ≈ 1 day active work |
| **Week Definition** | 5 pts ≈ 1 week active work |
| **Measurement Rule** | Gap < 1 week = same-day continuation |

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline** ✅ Framework exists
2. **Technical Debt Management** ✅ System operational (12 file size violations)
3. **Community Building** - Development blog, Discord setup
4. **Analytics Dashboard** - Can start early to support playtesting (Milestone 2.8)

---

## Risk Mitigation

| Risk | Mitigation | Fallback |
|------|------------|----------|
| Velocity Sustainability | Weekly tracking | Conservative 15 pts/month |
| Content Creation Scope | Quality over quantity | Launch with 8-10 stages (MVP) |
| Scope Creep | Feature freeze after Phase 1 | Focus on polish |
| File Size Violations | Phased refactoring (15 pts Phase 1; full 12-file resolution see TechnicalDebt.md) | Prioritize critical managers |
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
- [Milestone Tracking](MilestoneTracking.md) - Completed milestones and velocity metrics
- [Technical Debt](../Technical%20Doc/TechnicalDebt.md) - Debt tracking
- [Technical Critiques](../Technical%20Doc/TechnicalCritiques.md) - Code analysis

---

*Last Updated: February 2, 2026*  
*Current Phase: Phase 1 - Core Innovation & Content*  
*Current Milestone: 1.8 - Recursion Cube/Marker (Stages 6-8)*  
*Total Points: 316 | Completed: ~140 | Remaining: ~176*
