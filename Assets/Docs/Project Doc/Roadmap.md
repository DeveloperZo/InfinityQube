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
| **Phase 2** | 2.1-2.9 Content & Polish | ⬜ Pending | 60 pts |
| **Phase 3** | 3.1-3.5 QA & Refinement | ⬜ Pending | 35 pts |
| **Phase 4** | 4.1-4.7 Steam & Release | ⬜ Pending | 40 pts |

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
**Estimated Total**: ~60 pts (~3 months at 22 pts/month)

### Milestone 2.1: Complete Stage Content
- **Points**: 10 (~2 weeks)
- Finish all 12-13 stages with full configurations

### Milestone 2.2: Save/Load & Progress Persistence
- **Points**: 5 (~1 week)
- Complete save/load system for game progress

### Milestone 2.3: Settings & Options System
- **Points**: 5 (~1 week)
- Complete settings menu with persistence

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

## Phase 3: Polish, Refinement & QA
**Goal**: Achieve commercial polish level through iterative refinement  
**Estimated Total**: ~35 pts (~1.5 months at 22 pts/month)

### Milestone 3.1: Second Polish Pass - Balance & Tuning
- **Points**: 10 (~2 weeks)
- Comprehensive balance pass and difficulty tuning

### Milestone 3.2: UI/UX Polish Pass
- **Points**: 7 (~1.5 weeks)
- Polish all user interfaces and user experience

### Milestone 3.3: Performance Optimization
- **Points**: 5 (~1 week)
- Optimize performance for commercial release

### Milestone 3.4: Comprehensive QA & Bug Fixing
- **Points**: 8 (~1.5 weeks)
- Find and fix all critical bugs

### Milestone 3.5: Accessibility & Localization Prep
- **Points**: 5 (~1 week)
- Ensure game is accessible and ready for localization

---

## Phase 4: Steam Integration & Release Preparation
**Goal**: Platform integration and commercial release readiness  
**Estimated Total**: ~40 pts (~2 months at 22 pts/month)

### Milestone 4.1: Steam SDK Integration
- **Points**: 7 (~1.5 weeks)
- Integrate Steamworks SDK

### Milestone 4.2: Steam Achievements & Stats
- **Points**: 5 (~1 week)
- Implement Steam achievements and statistics

### Milestone 4.3: Steam Cloud Saves
- **Points**: 5 (~1 week)
- Implement Steam cloud save functionality

### Milestone 4.4: Steam Store Page & Marketing Assets
- **Points**: 7 (~1.5 weeks)
- Create Steam store presence

### Milestone 4.5: Final Polish & Release Candidate
- **Points**: 7 (~1.5 weeks)
- Final polish pass and release candidate build

### Milestone 4.6: Beta Testing Program
- **Points**: 5 (~1 week)
- Execute beta testing and gather feedback

### Milestone 4.7: Launch Day Preparation
- **Points**: 4 (~1 week)
- Prepare for commercial launch

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

## Effort Summary

| Phase | Focus | Points | Est. Time @ 22 pts/mo | Status |
|-------|-------|--------|----------------------|--------|
| Phase 1 | Core Innovation & Content | 124 pts | ~5.6 months | 🔄 In Progress |
| Phase 2 | Content Completion & Polish | 60 pts | ~2.7 months | ⬜ Pending |
| Phase 3 | Polish, Refinement & QA | 35 pts | ~1.6 months | ⬜ Pending |
| Phase 4 | Steam Integration & Release | 40 pts | ~1.8 months | ⬜ Pending |
| **TOTAL** | | **259 pts** | **~11.8 months** | |

**Completed**: 75 pts (Milestone 1.0 + 1.2)  
**Remaining**: ~184 pts (~8.4 months at current velocity)

**Note**: Phases can overlap; calendar time will vary based on availability.

---

## Development Velocity

| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** (active periods) |
| **Completed Points** | 75 pts (normalized) |
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
| Velocity Sustainability | Weekly tracking | Conservative 15 pts/month (~10.8 months remaining) |
| Content Creation Scope | Quality over quantity | Launch with 8-10 stages |
| Scope Creep | Feature freeze after Phase 1 | Focus on polish |
| File Size Violations | Phased refactoring | Prioritize critical managers |

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
