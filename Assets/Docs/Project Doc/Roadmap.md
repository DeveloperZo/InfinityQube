# Development Roadmap

> **Document Purpose:** This is the definitive, actionable guide for what to work on next, in priority order. For a historical record of completed milestones, see [MilestoneTracking.md](MilestoneTracking.md).

## Overview
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

**Note**: Timeline dates are applied only upon completion. Forward-looking dates are not projected.

---

## Current Status

### 📍 **Current Phase**: Phase 1 - Core Innovation & Content
### 📍 **Current Milestone**: 1.1 - Refine Markers Implementation (In Progress)

### 🔴 **TOP PRIORITIES**
| Priority | Item | Status | Points |
|----------|------|--------|--------|
| 🔴 Critical | Collision Matrix Implementation | Designed, needs code | 3 pts |
| 🔴 Critical | Line Divider System | Designed, needs implementation | 3 pts |
| 🔴 Critical | Resonance System | Designed, needs implementation | 2 pts |
| 🔴 High | Enhanced Face Painting | Rotation mechanics need implementation | 2 pts |
| 🔴 High | Penalty/Reward System | Designed, needs implementation | 2 pts |
| 🟡 Medium | Marker Economy | Per-stage grant system designed | 2 pts |
| 🟡 Medium | File Size Refactoring | 12 files over limit | 15 pts |

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
| | **1.1 Refine Markers** | 🔄 **In Progress** | 14 pts |
| | 1.2 Demo Stage Content | ✅ Complete | 5 pts |
| | 1.3 StageDB Enhancement | ⬜ Pending | 5 pts |
| | 1.4 Stage Content Creation | ⬜ Pending | 20 pts |
| | 1.5 RPG/Progression Ideation | ⬜ Pending | 10 pts |
| | 1.6 Visual Polish Pass | ⬜ Pending | 5 pts |
| **Phase 2** | 2.1-2.9 Content & Polish | ⬜ Pending | 63 pts |
| **Phase 3** | 3.1-3.8 Polish & Commercial Prep | ⬜ Pending | 55 pts |
| **Phase 4** | 4.1-4.9 Steam & Release | ⬜ Pending | 50 pts |

---

## Phase 1: Core Innovation & Content (Current Phase)
**Goal**: Implement core gameplay systems and complete foundational content  
**Estimated Total**: ~124 pts (~5-6 months at 22 pts/month)

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

### Milestone 1.1: Refine Markers Implementation 🔄 IN PROGRESS
- **Target**: Review and refine marker system (placement, cube spawn, cube collision/interactions) + Core gameplay systems
- **Points**: 14 (~2-3 weeks active development)
- **Status**: 🔄 **IN PROGRESS** - Collision matrix designed, unified input implemented
- **Dependencies**: Milestone 1.0 ✅

#### Completed:
- [x] Define marker placement and spawning for all markers
- [x] Design/Define cube collisions for all 16 combinations (including resonance)
- [x] Unified input system (mode keys 1-4, placement key F)
- [x] CubeCollisionManager created (collision logic structure)

#### In Progress / Remaining:
- [ ] **Implement collision matrix code** (CubeCollisionManager has structure, needs full implementation)
- [ ] Ensure charge system and regeneration mechanics are coherent
- [ ] Visual feedback for markers (distinct color per marker type)
- [ ] Ensure cohesive marker system
- [ ] **Implement Line Divider System** (dynamic difficulty, marker placement restriction)
- [ ] **Implement Resonance System** (Infinity vs Infinity phaseable effect)
- [ ] **Implement Enhanced Face Painting** (rotation mechanics, visual feedback, timing)
- [ ] **Implement Penalty/Reward System** (line movement based on cube falls and achievements)
- [ ] **Implement Marker Economy** (per-stage grants, cross-wave resource management)

#### Playtesting Checkpoint:
- Test: Line divider creates tension and strategic decisions
- Test: Resonance enables advanced Infinity strategies
- Test: Face painting timing is learnable and rewarding
- Iterate: Balance charge system, line movement, and visual feedback

---

### Milestone 1.2: Demo Stage Content ✅ COMPLETE
- **Target**: Foundational Systems + One Complete Demo Stage
- **Points**: 5 (measured: ~1 week work)
- **Status**: ✅ **COMPLETE** (July 8, 2025)
- **Tasks**:
  - [x] Foundational Message System
  - [x] Modal Control Scheme Implementation
  - [x] Wave Completion Messages
  - [x] Stage Success Transitions
  - [x] Stage/Wave Sequence Framework
  - [x] Complete Splash to Stage loop

---

### Milestone 1.3: StageDB Enhancement
- **Target**: Update StageDB system and migrate existing content
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.1
- **Tasks**:
  - [ ] Extend StageDB validation
  - [ ] Update StageData inspector/editor tools
  - [ ] Create StageDB debug tools
  - [ ] Migrate existing 3 stages
  - [ ] Test StageDB loading/saving

---

### Milestone 1.4: Stage Content Creation
- **Target**: 12-13 stages defined with waves, 4-5 complete stages with progression
- **Points**: 20 (~4 weeks active development)
- **Status**: ⬜ Pending
- **Dependencies**: Milestone 1.3
- **Tasks**:
  - [ ] Design remaining 9-10 stages
  - [ ] Create wave configurations for each stage
  - [ ] Implement proper difficulty curve across all stages
  - [ ] Create tutorial stages teaching core mechanics
  - [ ] Ensure debuggers allow easy tuning of stages/waves
  - [ ] Create stage progression flow (tutorial → standard → challenge → bonus)
  - [ ] Validate all stages with StageDB validation tools

---

### Milestone 1.5: RPG/Progression Ideation
- **Target**: Establish framework to deliver progression and RPG vibes for player
- **Points**: 10 (~2 weeks active development)
- **Status**: ⬜ Pending
- **Tasks**:
  - [ ] Brainstorm and design RPG elements and progression
  - [ ] Design hub area concept and layout
  - [ ] Plan meta-progression systems (unlocks, upgrades, character growth)
  - [ ] Create progression economy design (resources, currencies, rewards)

---

### Milestone 1.6: Visual Polish Pass
- **Target**: Enhanced game feel and visual feedback
- **Points**: 5 (~1 week active development)
- **Status**: ⬜ Pending
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
**Estimated Total**: ~63 pts

### Milestone 2.1: Complete Stage Content
- **Points**: 10 (~2 weeks)
- Finish all 12-13 stages with full configurations

### Milestone 2.2: Save/Load & Progress Persistence
- **Points**: 5 (~1 week)
- Complete save/load system for game progress

### Milestone 2.3: Menu & UI Systems
- **Points**: 8 (~1.5 weeks)
- **Tasks**:
  - [ ] Pause menu (resume, settings, quit to menu)
  - [ ] Stage select screen
  - [ ] Settings persistence (audio, display)
  - [ ] Stage results/rating display

### Milestone 2.4: Interactive Tutorial System
- **Points**: 10 (~2 weeks)
- Comprehensive tutorial teaching all mechanics

### Milestone 2.5: Stage Progression & Unlock System
- **Points**: 5 (~1 week)
- Stage unlock system and progression flow

### Milestone 2.6: Cosmic Theme Integration
- **Points**: 7 (~1.5 weeks)
- Deliver on aesthetic vision

### Milestone 2.7: Audio Content & Polish
- **Points**: 5 (~1 week)
- Add actual audio content to completed system

### Milestone 2.8: First Polish Pass - Gameplay
- **Points**: 8 (~1.5 weeks)
- First comprehensive polish pass focusing on gameplay feel

### Milestone 2.9: Meta-Progression Systems
- **Points**: 5 (~1 week)
- Player retention, achievement, and expressive progression

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
Milestone 1.0 ✅ → 1.1 🔄 → 1.3 → 1.4 → 2.x → 3.x → 4.x → Release
                    ↑
              YOU ARE HERE
                    │
              Collision Implementation
              Line Divider
              Resonance
              Face Painting
              Penalty/Reward
              Marker Economy
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
| **Line Divider** | Dynamic | Static position | Can simplify |
| **Tutorial** | Interactive | Message-based | Can simplify |
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
| Phase 1 | Core Innovation & Content | 124 pts | 🔄 In Progress |
| Phase 2 | Content Completion & Polish | 63 pts | ⬜ Pending |
| Phase 3 | Polish & Commercial Prep | 55 pts | ⬜ Pending |
| Phase 4 | Steam Integration & Release | 50 pts | ⬜ Pending |
| **TOTAL** | | **292 pts** | |

**Completed**: 75 pts (Milestone 1.0 + 1.2)  
**Remaining**: ~217 pts

**Note**: Phases can overlap; calendar time will vary based on availability.

---

## Development Velocity

| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** (active periods) |
| **Completed Points** | 75 pts |
| **Remaining Points** | ~217 pts |
| **Point Definition** | 1 pt ≈ 1 day active work |
| **Week Definition** | 5 pts ≈ 1 week active work |
| **Measurement Rule** | Gap < 1 week = same-day continuation |

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline** ✅ Framework exists
2. **Technical Debt Management** ✅ System operational (12 file size violations)
3. **Community Building** - Development blog, Discord setup

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

*Last Updated: December 8, 2025*  
*Current Phase: Phase 1 - Core Innovation & Content*  
*Current Milestone: 1.1 - Refine Markers Implementation*  
*Total Points: 292 | Completed: 75 | Remaining: 217*
