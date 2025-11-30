# Development Roadmap

> **Document Purpose:** This is the definitive, actionable guide for what to work on next, in priority order. For a historical record of completed milestones, see [MilestoneTracking.md](MilestoneTracking.md).

## Overview
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

**Note**: Timeline dates are applied only upon completion. Forward-looking dates are not projected.

---

## Current Status
### ✅ **COMPLETED SYSTEMS** 
- **Four-tier marker system** (Light/Heavy/Prime/Cube) - ✅ (June 23, 2025)
- **Face painting mechanics** with rotation tracking - ✅ Fully operational
- **Corruption/enhancement tile system** - ✅ Integrated and tested
- **Recursion cube multi-hit mechanics** - ✅ Working with Recursion Markers
- **Comprehensive debug infrastructure** - ✅ Production quality (OnGUI used only for debuggers)
- **Statistics and analytics system** - ✅ Complete tracking implemented
- **Integration testing framework** - ✅ All systems validated
- **Wave management system** - ✅ Functional with editor tools
- **Player action system** - ✅ Full input handling operational
- **Technical debt management** - ✅ Phase 1 validation completed
- **Wave completion messages** - ✅ Progress tracking with pause system (July 8, 2025)
- **Stage transition system** - ✅ Demo completion flow and splash return (July 8, 2025)
- **Audio system** - ✅ Complete AudioManager implementation with 7+ subsystems:
  - AudioManager.cs (singleton core)
  - AudioSourcePool.cs (resource management)
  - AudioPlaybackSystem.cs (playback control)
  - AudioVolumeController.cs (volume management)
  - CubeAudioSystem.cs (cube-specific sounds)
  - CubeAudioConfiguration.cs (configuration data)
  - AudioTesting.cs (debug tools)
- **Production UI System** - ✅ Unity UI implementation complete:
  - PlayerActionUI.cs (charge indicators, cooldown displays)
  - StageTransitionUI.cs (stage intro/completion screens)
  - WaveProgressUI.cs (wave progress tracking)
  - PlayerUI.cs, StageInfoUI.cs (status displays)

### 🔄 **TOP PRIORITY - IN PROGRESS** 
- **Infinity Markers** - Designed but not implemented (can be prioritized)

---

## Phase 1: Core Innovation & Content (Current Phase)
**Goal**: Implement core paired wave system and complete foundational content
**Estimated Total Effort**: 6-8 weeks active development

### Milestone 1.1: Paired Wave System Implementation - HIGHEST PRIORITY
- **Target**: Implement marker inheritance system where Wave A marker placements become Wave B cube spawns
- **Complexity**: 6 points
- **Effort Estimate**: 1-2 weeks active development
- **Status**: ✅ **COMPLETE** - Core innovation mechanic implemented and functional
- **Dependencies**: None (foundational system)
- **Tasks**:
  - [x] Extend WaveData ScriptableObject with paired wave fields (pairID, isPrimaryWave, markerSpawnRules, inheritanceDelay)
  - [x] Should be no need to **Update StageDB/StageData** to support paired wave configuration and validation
  - [x] Implement marker position recording system in GridManager/PlayerActionManager
  - [x] Ensure wave manager alternates between wave and mirror wave
  - [x] Implement marker-to-cube conversion rules (Light→Unit, Heavy→Recursion, Prime→Prime, Infinity→Infinity)
  - [x] Add visual feedback (placement echo, inheritance trail, type indicators)
  - [x] Update debug panels for paired wave visualization
  - [x] Integration testing with existing wave system
- **Acceptance Criteria**:
  - ✅ Marker placements in Wave A are recorded and visible in debug panel
  - ✅ Wave B spawns cubes at recorded marker positions from Wave A
  - ✅ Marker-to-cube conversion works correctly (Light→Unit, Heavy→Recursion, Prime→Prime, Infinity→Infinity)
  - ✅ Visual feedback clearly shows inheritance connections (debug logging and prototype system)
  - ✅ Existing 3 stages can be loaded and played with paired waves
- **Playtesting Checkpoint**:
  - Playtest: Create a simple 2-wave pair, place markers in Wave A, verify they spawn as cubes in Wave B
  - Verify: Inheritance is visually clear and gameplay feels strategic
  - Iterate: Adjust visual feedback and conversion rules based on playtest feedback

### Milestone 1.2: Infinity Markers Implementation
- **Target**: Complete Infinity marker system (placement, triggering, wave inheritance)
- **Complexity**: 3 points
- **Effort Estimate**: ~1 week active development
- **Status**: Designed but not implemented
- **Dependencies**: Milestone 1.1 ✅ (paired wave system complete)
- **Tasks**:
  - [ ] Define Infinity marker placement key and trigger key
  - [ ] Implement Infinity marker placement mechanics
  - [ ] Create pause-inducing cube spawn system
  - [ ] Integrate with wave inheritance (Infinity markers spawn Infinity cubes in next wave)
  - [ ] Add charge system and regeneration mechanics
  - [ ] Visual feedback and interaction range indicators
  - [ ] Integration with existing marker system
- **Acceptance Criteria**:
  - ✅ Infinity markers can be placed and triggered with defined keys
  - ✅ Infinity markers spawn pause-inducing cubes that affect Infinity cubes
  - ✅ Infinity markers in Wave A spawn Infinity cubes in Wave B
  - ✅ Charge system and regeneration work correctly
  - ✅ Visual feedback clearly shows Infinity marker effects
- **Playtesting Checkpoint**:
  - Playtest: Place Infinity markers, trigger them, verify pause mechanics work
  - Verify: Infinity marker inheritance creates strategic depth
  - Iterate: Balance charge system and visual feedback

### Milestone 1.3: First Pass at Complete Stage 1 Content
- **Target**: Foundational Systems + One Complete Demo Stage
- **Complexity**: 3 points
- **Tasks**:
  - [x] Foundational Message System ✅ (July 8, 2025)
  - [x] Modal Control Scheme Implementation ✅
  - [x] Wave Completion Messages ✅ (July 8, 2025)
  - [x] Stage Success Transitions ✅ (July 8, 2025)
  - [x] Stage/Wave Sequence Framework  ✅ (July 8, 2025)
  - [x] Complete Splash to Stage loop  ✅ (July 8, 2025)

### Milestone 1.4: StageDB Enhancement & Migration
- **Target**: Update StageDB system to fully support paired waves and migrate existing content
- **Complexity**: 3 points
- **Effort Estimate**: ~1 week active development
- **Dependencies**: Milestone 1.1 ✅ (paired wave system complete)
- **Tasks**:
  - [ ] Extend StageDB validation to check paired wave integrity
  - [ ] Create StageDB migration tools for converting existing stages to paired wave format
  - [ ] Update StageData inspector/editor tools for paired wave configuration
  - [ ] Add StageDB validation for pairID consistency and wave relationships
  - [ ] Create StageDB debug tools for visualizing paired wave relationships
  - [ ] Migrate existing 3 stages (stage_01, stage_02, stage_03) to paired wave format
  - [ ] Test StageDB loading/saving with paired wave data
- **Acceptance Criteria**:
  - ✅ StageDB validates paired wave integrity (all waves have valid pairIDs)
  - ✅ Migration tool successfully converts existing stages
  - ✅ Editor tools allow easy configuration of paired waves
  - ✅ All 3 existing stages load and play correctly with paired waves
  - ✅ Debug tools visualize wave relationships clearly
- **Playtesting Checkpoint**:
  - Playtest: Load migrated stages, verify they play correctly with paired waves
  - Verify: Stage progression feels natural with paired wave mechanics
  - Iterate: Adjust stage configurations based on playtest experience

### Milestone 1.5: Stage Content Creation
- **Target**: 12-13 stages defined with waves, 4-5 complete stages with progression
- **Complexity**: 7 points
- **Effort Estimate**: 3-4 weeks active development
- **Dependencies**: Milestone 1.1 ✅, 1.4 (needs paired waves ✅ and StageDB tools)
- **Tasks**:
  - [ ] Design remaining 9-10 stages with paired wave mechanics
  - [ ] Create wave pairs for each stage (Wave A → Wave B structure)
  - [ ] Implement proper difficulty curve across all stages
  - [ ] Create tutorial stages specifically teaching paired wave mechanics
  - [ ] Ensure debuggers allow easy tuning of stages/waves
  - [ ] Balance marker inheritance rules per stage difficulty
  - [ ] Create stage progression flow (tutorial → standard → challenge → bonus stages)
  - [ ] Validate all stages with StageDB validation tools
- **Acceptance Criteria**:
  - ✅ 12-13 stages designed and configured in StageDB
  - ✅ At least 4-5 stages are fully playable end-to-end
  - ✅ Tutorial stages clearly teach paired wave mechanics
  - ✅ Difficulty curve is smooth and progressive
  - ✅ All stages pass StageDB validation
  - ✅ Debug tools allow rapid iteration on stage design
- **Playtesting Checkpoint**:
  - Playtest: Play through all 4-5 complete stages, verify difficulty curve
  - Verify: Tutorial stages effectively teach paired wave mechanics
  - Iterate: Adjust difficulty, wave composition, and inheritance rules based on playtest
  - **Note**: This milestone can be split into smaller playtestable chunks (2-3 stages at a time)

### Milestone 1.6: RPG/Progression Ideation
- **Target**: Establish framework to deliver progression and RPG vibes for player
- **Complexity**: 4 points
- **Effort Estimate**: 1-2 weeks active development
- **Tasks**:
  - [ ] Brainstorm and design RPG elements and progression
  - [ ] Design hub area concept and layout
  - [ ] Plan meta-progression systems (unlocks, upgrades, character growth)
  - [ ] Create progression economy design (resources, currencies, rewards)

### Milestone 1.7: Visual Polish Pass
- **Target**: Enhanced game feel and visual feedback
- **Complexity**: 2 points
- **Effort Estimate**: 1-2 weeks active development
- **Tasks**:
  - [ ] Improved particle effects for all major actions
  - [ ] Screen shake and juice for impacts
  - [ ] Enhanced face painting visualization
  - [ ] Tile state change visual effects
  - [ ] Marker placement/detonation polish
  - [ ] Animation/scripted sequence placeholders for key events (stage intro, wave intro, etc)
  - [ ] Event hooks for identifying and triggering major gameplay moments (stage start, wave start, boss intro, etc)

---

## Phase 2: Content Completion & First Polish Pass
**Goal**: Complete all content and achieve first polish level
**Estimated Total Effort**: 8-10 weeks active development

### Milestone 2.1: Complete Stage Content
- **Target**: Finish all 12-13 stages with full wave pairs
- **Complexity**: 5 points
- **Effort Estimate**: 2-3 weeks active development
- **Dependencies**: Milestone 1.5 (builds on initial stage content)
- **Tasks**:
  - [ ] Complete remaining stage designs and implementations
  - [ ] Create all wave pairs for each stage
  - [ ] Balance difficulty curve across all stages
  - [ ] Create challenge and bonus stage variants
  - [ ] Validate all stages with comprehensive testing
  - [ ] StageDB final validation and integrity checks
- **Acceptance Criteria**:
  - ✅ All 12-13 stages are complete and playable
  - ✅ All stages have properly configured wave pairs
  - ✅ Difficulty curve is balanced across all stages
  - ✅ Challenge and bonus stages provide variety
  - ✅ All stages pass comprehensive testing
- **Playtesting Checkpoint**:
  - Playtest: Full playthrough of all stages, verify progression feels good
  - Verify: No stages feel too easy or too hard
  - Iterate: Fine-tune difficulty and wave composition

### Milestone 2.2: Save/Load & Progress Persistence
- **Target**: Complete save/load system for game progress
- **Complexity**: 3 points
- **Effort Estimate**: 1-2 weeks active development
- **Dependencies**: Milestone 2.1 (needs stages to save progress for)
- **Tasks**:
  - [ ] Design save data structure (stage progress, unlocks, statistics)
  - [ ] Implement save/load system (extend existing PlayerPrefs/JSON system)
  - [ ] Add save slot management (multiple save files)
  - [ ] Implement progress persistence across sessions
  - [ ] Create save data migration system (for future updates)
  - [ ] Add save/load UI (save game, load game, delete save)
  - [ ] Test save/load with all game systems
- **Acceptance Criteria**:
  - ✅ Save data structure captures all necessary progress
  - ✅ Save/load works correctly (can save, quit, reload, continue)
  - ✅ Multiple save slots work independently
  - ✅ Progress persists correctly across game sessions
  - ✅ Save/load UI is functional and clear
  - ✅ Save system handles edge cases (corrupted saves, missing files)
- **Playtesting Checkpoint**:
  - Playtest: Play stages, save, quit, reload, verify progress is correct
  - Verify: Save/load feels seamless and reliable
  - Iterate: Improve UI and error handling based on testing

### Milestone 2.3: Settings & Options System
- **Target**: Complete settings menu with persistence
- **Complexity**: 2 points
- **Effort Estimate**: ~1 week active development
- **Tasks**:
  - [ ] Create settings menu UI (graphics, audio, controls, gameplay)
  - [ ] Implement settings persistence (PlayerPrefs/JSON)
  - [ ] Add graphics options (resolution, quality, fullscreen)
  - [ ] Add audio options (master, music, SFX volume sliders)
  - [ ] Add control rebinding system
  - [ ] Add accessibility options (colorblind mode, subtitles, etc.)
  - [ ] Settings reset to defaults functionality

### Milestone 2.4: Interactive Tutorial System
- **Target**: Comprehensive tutorial teaching all mechanics including paired waves
- **Complexity**: 4 points
- **Effort Estimate**: 1-2 weeks active development
- **Dependencies**: Milestone 1.1 ✅, 1.5 (needs paired waves ✅ and stages)
- **Tasks**:
  - [ ] Design tutorial flow for all mechanics
  - [ ] Create interactive tutorial stages (guided placement, step-by-step)
  - [ ] Implement tutorial for paired wave mechanics (visual demonstrations)
  - [ ] Add tutorial skip/resume functionality
  - [ ] Create tutorial progress tracking
  - [ ] Test tutorial with new players (usability testing)
- **Acceptance Criteria**:
  - ✅ Tutorial covers all core mechanics (markers, cubes, paired waves)
  - ✅ Tutorial is interactive and guides player step-by-step
  - ✅ Paired wave mechanics are clearly demonstrated
  - ✅ Tutorial can be skipped or resumed
  - ✅ Tutorial progress is tracked and saved
  - ✅ New players can complete tutorial and understand game
- **Playtesting Checkpoint**:
  - Playtest: Have someone new to the game play through tutorial
  - Verify: They understand all mechanics after tutorial
  - Iterate: Refine tutorial based on where players get confused
  - **Critical**: This is essential for first-time game dev - get external playtesters

### Milestone 2.5: Stage Progression & Unlock System
- **Target**: Stage unlock system and progression flow
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Implement stage unlock logic (complete stage N to unlock N+1)
  - [ ] Create stage select screen/menu
  - [ ] Add stage completion tracking and display
  - [ ] Implement stage replay functionality
  - [ ] Add stage difficulty indicators
  - [ ] Create stage completion rewards/unlocks

### Milestone 2.6: Cosmic Theme Integration
- **Target**: Deliver on aesthetic vision
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Cosmic background and environment art
  - [ ] Enhanced particle systems with stellar effects
  - [ ] Visual theme consistency across all systems
  - [ ] Cosmic liquid animations for face painting
  - [ ] Atmospheric visual enhancement

### Milestone 2.7: Audio Content & Polish
- **Target**: Add actual audio content to completed system
- **Complexity**: 2 points (system already complete)
- **Tasks**:
  - [ ] Source/create audio clips for existing AudioManager system
  - [ ] Test audio integration with all 7+ subsystems
  - [ ] Balance volume levels across categories
  - [ ] Add music and ambient sounds
  - [ ] Add audio for paired wave mechanics (inheritance feedback sounds)
  - [ ] Polish audio timing and synchronization

### Milestone 2.8: First Polish Pass - Gameplay
- **Target**: First comprehensive polish pass focusing on gameplay feel
- **Complexity**: 4 points
- **Tasks**:
  - [ ] Improved particle effects for all major actions
  - [ ] Screen shake and juice for impacts
  - [ ] Enhanced face painting visualization
  - [ ] Tile state change visual effects
  - [ ] Marker placement/detonation polish
  - [ ] Animation/scripted sequence placeholders for key events
  - [ ] Event hooks for identifying and triggering major gameplay moments
  - [ ] Polish paired wave visual feedback (inheritance trails, previews)
  - [ ] Improve game feel and responsiveness

### Milestone 2.9: Meta-Progression Systems
- **Target**: Player retention, achievement, and expressive progression
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Achievement system implementation
  - [ ] Performance ratings (bronze/silver/gold)
  - [ ] Post-stage breakdown and statistics
  - [ ] Unlockable content or modes
  - [ ] Settings persistence system
  - [ ] **RPG-like progression system** (character growth, unlockable abilities, or upgrades)
  - [ ] **Hub area (village/town)**: A central location where player progression is visually and interactively expressed (inspired by Bastion's village)
  - [ ] Visual and interactive feedback for progression in the hub
  - [ ] Integration of meta-progression with core gameplay and hub

---

## Phase 3: Polish, Refinement & QA
**Goal**: Achieve commercial polish level through iterative refinement
**Estimated Total Effort**: 4-6 weeks active development

### Milestone 3.1: Second Polish Pass - Balance & Tuning
- **Target**: Comprehensive balance pass and difficulty tuning
- **Complexity**: 4 points
- **Tasks**:
  - [ ] Balance all stages for difficulty curve
  - [ ] Tune paired wave inheritance rules per stage
  - [ ] Balance marker resource economy
  - [ ] Tune cube movement speeds and timing
  - [ ] Balance marker cooldowns and charges
  - [ ] Playtest all stages with multiple skill levels
  - [ ] Iterate on difficulty based on playtest feedback
  - [ ] Create difficulty variants if needed (easy/normal/hard)

### Milestone 3.2: UI/UX Polish Pass
- **Target**: Polish all user interfaces and user experience
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Polish main menu and navigation
  - [ ] Improve stage select screen
  - [ ] Enhance HUD clarity and information display
  - [ ] Polish settings menu and options
  - [ ] Improve tutorial UI and flow
  - [ ] Add tooltips and help text where needed
  - [ ] Polish transitions between screens
  - [ ] Improve error messages and feedback

### Milestone 3.3: Performance Optimization
- **Target**: Optimize performance for commercial release
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Profile performance on minimum spec hardware
  - [ ] Optimize frame rate (target 60 FPS consistent)
  - [ ] Optimize memory usage
  - [ ] Optimize load times (target <3 seconds)
  - [ ] Optimize particle systems and visual effects
  - [ ] Optimize audio system performance
  - [ ] Stress test with maximum cubes/markers on screen
  - [ ] Performance testing across different hardware configurations

### Milestone 3.4: Comprehensive QA & Bug Fixing
- **Target**: Find and fix all critical bugs
- **Complexity**: 4 points
- **Tasks**:
  - [ ] Create comprehensive test plan
  - [ ] Execute full game playthrough testing
  - [ ] Test all edge cases and error conditions
  - [ ] Test save/load system thoroughly
  - [ ] Test paired wave system edge cases
  - [ ] Test stage progression and unlocks
  - [ ] Fix all critical and high-priority bugs
  - [ ] Regression testing after bug fixes
  - [ ] Create bug tracking and prioritization system

### Milestone 3.5: Accessibility & Localization Prep
- **Target**: Ensure game is accessible and ready for localization
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Implement colorblind-friendly color schemes
  - [ ] Add subtitle support for audio cues
  - [ ] Test with screen readers (if applicable)
  - [ ] Add keyboard-only navigation support
  - [ ] Prepare text for localization (externalize all strings)
  - [ ] Create localization framework (if planning multiple languages)
  - [ ] Test UI with different text lengths (for future localization)

## Phase 4: Steam Integration & Release Preparation
**Goal**: Platform integration and commercial release readiness
**Estimated Total Effort**: 4-5 weeks active development

### Milestone 4.1: Steam SDK Integration
- **Target**: Integrate Steamworks SDK
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Install and configure Steamworks SDK
  - [ ] Implement Steam initialization
  - [ ] Add Steam overlay support
  - [ ] Test Steam integration in development
  - [ ] Handle Steam API errors gracefully
  - [ ] Test offline mode functionality

### Milestone 4.2: Steam Achievements & Stats
- **Target**: Implement Steam achievements and statistics
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Design achievement list (20-30 achievements)
  - [ ] Implement achievement unlocking system
  - [ ] Integrate with Steam achievement API
  - [ ] Add Steam statistics tracking
  - [ ] Test achievement unlocking in Steam
  - [ ] Create achievement icons and descriptions

### Milestone 4.3: Steam Cloud Saves
- **Target**: Implement Steam cloud save functionality
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Integrate Steam cloud save API
  - [ ] Implement cloud save upload/download
  - [ ] Handle save conflict resolution
  - [ ] Test cloud saves across multiple devices
  - [ ] Add cloud save status indicators
  - [ ] Test cloud saves with Steam offline mode

### Milestone 4.4: Steam Store Page & Marketing Assets
- **Target**: Create Steam store presence
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Write store page copy (description, features, etc.)
  - [ ] Create store page screenshots (10+ images)
  - [ ] Create store page capsule images (header, main, small)
  - [ ] Create store page trailer video
  - [ ] Set up Steam store page (categories, tags, pricing)
  - [ ] Create press kit with screenshots and assets
  - [ ] Prepare Steam community hub content

### Milestone 4.5: Final Polish & Release Candidate
- **Target**: Final polish pass and release candidate build
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Final visual polish pass
  - [ ] Final audio polish pass
  - [ ] Final balance tuning pass
  - [ ] Create release candidate build
  - [ ] Comprehensive testing of release candidate
  - [ ] Fix any release-blocking issues
  - [ ] Prepare release notes and changelog
  - [ ] Create build verification checklist

### Milestone 4.6: Beta Testing Program
- **Target**: Execute beta testing and gather feedback
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Recruit beta testers (10-20 testers)
  - [ ] Create beta testing feedback forms
  - [ ] Distribute beta builds via Steam
  - [ ] Collect and analyze beta feedback
  - [ ] Prioritize and implement critical beta feedback
  - [ ] Iterate on beta builds based on feedback
  - [ ] Thank beta testers and prepare credits

### Milestone 4.7: Launch Day Preparation
- **Target**: Prepare for commercial launch
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Create launch day checklist
  - [ ] Prepare launch day communications (social media, etc.)
  - [ ] Set up community moderation tools
  - [ ] Prepare customer support resources
  - [ ] Create launch day monitoring plan
  - [ ] Prepare hotfix process for launch issues
  - [ ] Final build verification and upload
  - [ ] Launch day execution and monitoring

---

## Implementation & Playtesting Guidelines

### Milestone Implementation Process
For each milestone, follow this process:

1. **Review Milestone**
   - Read all tasks and acceptance criteria
   - Check dependencies (ensure prerequisites are complete)
   - Understand the playtesting checkpoint

2. **Implementation**
   - Implement tasks in logical order
   - Use existing debug tools for rapid iteration
   - Follow project coding standards and patterns
   - Mark tasks complete as you go

3. **Validation**
   - Run build validation (Unity compiles, no errors)
   - Run integration tests if complexity ≥ 5
   - Verify acceptance criteria are met
   - Check that existing functionality still works

4. **Playtesting Checkpoint**
   - Playtest the milestone feature in-game
   - Verify it works as intended
   - Check for obvious issues or missing polish
   - Iterate based on playtest feedback
   - **Note**: Don't skip playtesting - it's critical for first-time game dev

5. **Mark Complete**
   - All tasks checked off
   - All acceptance criteria met
   - Playtesting checkpoint passed
   - Ready to move to next milestone

### Playtesting Best Practices
- **After Each Milestone**: Quick playtest to verify feature works
- **After Major Milestones**: More thorough playtest with focus on that feature
- **After Phase Completion**: Full playthrough to check integration
- **External Playtesters**: Get fresh eyes, especially for tutorial and onboarding
- **Document Feedback**: Keep notes on what feels good/bad, iterate quickly

### Milestone Dependencies
Milestones are ordered to minimize dependencies, but some key paths:
- **1.1 → 1.4 → 1.5**: Paired waves → StageDB → Content creation
- **1.1 → 1.2**: Paired waves → Infinity markers (needs inheritance)
- **2.1 → 2.2**: Complete stages → Save/load (needs content to save)
- **All → 3.x**: Polish phases need complete content

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline** ✅ **Framework exists**
   - [ ] Weekly playtesting sessions (after each milestone)
   - [ ] Analytics integration for player behavior
   - [ ] A/B testing for difficulty curves
   - [ ] Document playtest feedback and iterate

2. **Technical Debt Management** ✅ **System operational**
   - [ ] Bi-weekly code quality reviews
   - [ ] Performance profiling checkpoints
   - [ ] Documentation maintenance
   - [ ] Refactor as needed between milestones

3. **Community Building**
   - [ ] Development blog/devlog with milestone updates
   - [ ] GIF creation pipeline for progress sharing
   - [ ] Discord community setup
   - [ ] Beta testing program (Phase 4)

---

## Risk Mitigation

### Identified Risks:
1. **Paired Wave System Complexity**: Core innovation requires careful implementation
   - **Mitigation**: Start with simple 1:1 marker-to-cube conversion, iterate
   - **Fallback**: Phased implementation - basic inheritance first, advanced features later

2. **Velocity Sustainability**: Development velocity may fluctuate
   - **Mitigation**: Weekly velocity tracking with rolling averages
   - **Fallback**: Conservative 15 points/month estimate for planning

3. **Content Creation Scope**: Remaining stage content and polish
   - **Mitigation**: Focus on quality over quantity for initial release
   - **Fallback**: Launch with fewer but more polished stages

4. **Scope Creep**: Feature additions could derail timeline
   - **Mitigation**: Strict feature freeze after Phase 1, focus on polish only

5. **Infinity Marker Integration**: New marker type requires system integration
   - **Mitigation**: Follow existing marker system patterns, reuse infrastructure
   - **Fallback**: Defer to Phase 2 if Phase 1 priorities require focus

6. **StageDB Migration Complexity**: Updating existing stages to paired wave format
   - **Mitigation**: Create migration tools and validation systems early
   - **Fallback**: Manual migration with careful testing

7. **First-Time Game Dev Learning Curve**: Additional polish/refinement time needed
   - **Mitigation**: Built-in polish phases (Phase 3) and beta testing
   - **Fallback**: Extended polish timeline, focus on core experience first

8. **Content Creation Scope**: 12-13 stages is significant content work
   - **Mitigation**: Reuse patterns, create stage templates, use debug tools
   - **Fallback**: Launch with 8-10 polished stages, add more post-launch

---

## Success Metrics

### Development Velocity Tracking:
- **Target Velocity**: 15+ complexity points/month (conservative estimate)
- **Weekly Check-ins**: Velocity recalculation as milestones complete
- **Milestone Gates**: No phase advancement without 90% completion

### Quality Gates:
- **Fundamentals**: 90% of players understand core loop in < 2 minutes
- **Performance**: Consistent 60 FPS on minimum spec hardware
- **Player Experience**: Average session length > 20 minutes
- **Stage Completion**: >70% completion rate for early stages
- **Paired Wave Understanding**: Players recognize marker inheritance within 3 wave pairs

---

## Development Velocity Dashboard

### **Current Metrics**:
- **Measured Velocity**: ~22 complexity points/month (active periods)
- **Completed Complexity**: 55 points
- **Remaining Work**: ~38 complexity points (across remaining phases)
- **Estimated Total Effort**: ~8-12 weeks of active development

### **Effort Summary by Phase**:
| Phase | Focus | Effort Estimate |
|-------|-------|-----------------|
| Phase 1 | Core Innovation & Content | 6-8 weeks active |
| Phase 2 | Content Completion & Polish | 8-10 weeks active |
| Phase 3 | Polish, Refinement & QA | 4-6 weeks active |
| Phase 4 | Steam Integration & Release | 4-5 weeks active |

**Note**: Phases can overlap; some work can be parallelized.

### **Priority Tracking**:
| Priority | Item | Status | Effort |
|----------|------|--------|--------|
| 🔴 Critical | Paired Wave System Implementation | ✅ Complete | 1-2 weeks |
| 🔴 High | File Size Refactoring (WaveManager) | Planned | 2-3 weeks |
| 🟡 High | Infinity Markers Implementation | Designed | ~1 week |
| 🟡 Medium | Stage Content Creation | Not Started | 3-4 weeks |

### **System Status**:
| System | Status | Notes |
|--------|--------|-------|
| Audio System | ✅ Complete | 7+ files implemented |
| Production UI | ✅ Complete | Unity UI systems operational |
| Debug Systems | ✅ Complete | OnGUI (intentional for debug) |
| Paired Wave System | ✅ Complete | Core innovation implemented |
| File Size Compliance | 🔴 Violation | 10 files exceed limits |

### **Critical Path**:
```
Paired Wave System ✅ → Infinity Markers (1 wk) → Stage Content (3-4 wks) → Tutorial (1-2 wks) → Polish (2-4 wks) → Release
```

---

**Related Documents**:
- [Milestone Tracking](MilestoneTracking.md) - Historical record
- [Development Velocity](../Technical%20Doc/DevelopmentVelocity.md) - Detailed metrics
- [Technical Debt](../Technical%20Doc/TechnicalDebt.md) - Debt tracking
- [Architectural Critiques](../Technical%20Doc/ArchitecturalCritiques.md) - Code analysis

---

*Last Updated: December 2025*  
*Development Phase: Phase 1 - Infinity Markers & Content Creation*