# Development Roadmap

> **Document Purpose:** This is the definitive, actionable guide for what to work on next, in priority order. For a historical record of completed milestones, see [MilestoneTracking.md](MilestoneTracking.md).

## Overview
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

**🚀 UPDATED TIMELINE**: Based on measured development velocity of 18.7 complexity points/month (updated June 27), with post-integration acceleration to 37.5 points/month observed. Timeline optimized for September 2025 release with high confidence.

---

## Current Status (July 13, 2025)
### ✅ **COMPLETED SYSTEMS** 
- **Four-tier marker system** (Light/Heavy/Prime/Cube) - ✅ (June 23, 2025)
- **Face painting mechanics** with rotation tracking - ✅ Fully operational
- **Corruption/enhancement tile system** - ✅ Integrated and tested
- **Recursion cube multi-hit mechanics** - ✅ Working with heavy markers
- **Comprehensive debug infrastructure** - ✅ Production quality
- **Statistics and analytics system** - ✅ Complete tracking implemented
- **Integration testing framework** - ✅ All systems validated
- **Wave management system** - ✅ Functional with editor tools
- **Player action system** - ✅ Full input handling operational
- **Technical debt management** - ✅ Phase 1 validation completed
- **Wave completion messages** - ✅ Progress tracking with pause system (July 8, 2025)
- **Stage transition system** - ✅ Demo completion flow and splash return (July 8, 2025)
- **Audio system foundation** - ✅ Full architecture with event integration (July 8, 2025)

### 🔄 **IN PROGRESS** 
- **UI Modernization** - ⚡ ACTIVE (OnGUI → Unity UI conversion)
- **First Pass at Complete Stage 1 Content**

---

## Phase 1: Audio & UI Foundation (2-3 weeks) - JULY 2025
**Goal**: Eliminate critical experiential gaps and modernize presentation

### Milestone 1.0: Audio System Implementation  ✅ COMPLETED (July 8, 2025)
- **Target**: Complete audio foundation
- **Complexity**: 3 points (foundation complete)
- **Completed**: AudioManager with full subsystem architecture
- **Tasks**:
  - [x] Audio system architecture foundation - **COMPLETE**
    - [x] AudioManager singleton with DontDestroyOnLoad ✅
    - [x] Subsystem architecture: AudioSourcePool, AudioPlaybackSystem, AudioVolumeController, CubeAudioSystem ✅
    - [x] Event-driven audio triggers integrated with game events ✅
    - [x] Volume category management system ✅
    - [x] Debug testing tools and validation interface ✅

### Milestone 1.1: First Pass at Complete Stage 1 Content
- **Target**: Foundational Systems + One Complete Demo Stage
- **Complexity**: 3 points
- **Tasks**:
  - [x] Foundational Message System ✅ (July 8, 2025)
  - [x] Modal Control Scheme Implementation ✅
  - [x] Wave Completion Messages ✅ (July 8, 2025)
  - [x] Stage Success Transitions ✅ (July 8, 2025)
  - [x] Stage/Wave Sequence Framework  ✅ (July 8, 2025)
  - [x] Complete Splash to Stage loop  ✅ (July 8, 2025)


### Milestone 1.3: Stage Content Creation
- **Target**: 12-13 stages defined with waves and 4-5 complete stages with progression
- **Complexity**: 7 points
- **Tasks**:
  - [ ] Design, implement and verify remaining stages and waves
  - [ ] Proper difficulty curve implementation
  - [ ] Ensure debuggers allow easy tuning of stages/waves

### Milestone 1.4: RPG/Progression Ideation
- **Target**: Establish framework to deliver progression and RPG vibes for player
- **Complexity**: 4 points
- **Tasks**:
  - [ ] Brainstorm and design RPG elements and progression

### Milestone 1.2: UI Modernization 
- **Target**: Replace OnGUI with modern Unity UI
- **Complexity**: 3 points 
- **Timeline**: Started July 8, target completion July 22
- **Status**: 🔄 IN PROGRESS
- **Tasks**:
  - [ ] Convert HUD elements to Unity UI
  - [ ] Create main menu and options system
  - [ ] Implement pause menu with restart functionality
  - [ ] Add stage complete/failed screens
  - [ ] Modernize charge indicators and cooldown displays


### Milestone 1.5: Visual Polish Pass
- **Target**: Enhanced game feel and visual feedback
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Improved particle effects for all major actions
  - [ ] Screen shake and juice for impacts
  - [ ] Enhanced face painting visualization
  - [ ] Tile state change visual effects
  - [ ] Marker placement/detonation polish
  - [ ] Animation/scripted sequence placeholders for key events (stage intro, wave intro, etc)
  - [ ] Event hooks for identifying and triggering major gameplay moments (stage start, wave start, boss intro, etc)

---

## Phase 2: Content & Polish Completion (3-4 weeks) - AUGUST 2025
**Goal**: Complete remaining content and achieve commercial polish level

### Milestone 2.1: Polish Stage Content
- **Target**: Review Stage/Wave content
- **Complexity**: 5 points
- **Tasks**:
  - [ ] Analyze and verify stages
  - [ ] Proper difficulty curve implementation
  - [ ] Interactive tutorial system
  - [ ] Stage progression and unlock system
  - [ ] Save/load system for progress persistence

### Milestone 2.2: Cosmic Theme Integration
- **Target**: Deliver on aesthetic vision
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Cosmic background and environment art
  - [ ] Enhanced particle systems with stellar effects
  - [ ] Visual theme consistency across all systems
  - [ ] Cosmic liquid animations for face painting
  - [ ] Atmospheric visual enhancement

### Milestone 2.3: Polish Audio System
- **Target**: Complete audio content creation and polish
- **Complexity**: 5 points
- **Tasks**:
  - [ ] Complete core action sounds integration testing
  - [ ] Assign placeholder audio clips to CubeAudioConfiguration
  - [ ] Execute comprehensive audio testing with game managers
  - [ ] Verify audio performance under load
  - [ ] **Core Action Sound Content Creation**:
    - [ ] Generate/source marker placement sounds (3 variations)
    - [ ] Create cube capture sounds by type (8 variations: 2 per cube type)
    - [ ] Generate detonation effect sounds (3 variations)
    - [ ] Add UI interaction sounds (click, hover, success/failure)
  - [ ] **System Feedback Sound Content**:
    - [ ] Generate wave start/complete sound effects
    - [ ] Create resource regeneration audio cues
    - [ ] Add player movement feedback sounds
    - [ ] Generate marker trigger confirmation sounds
  - [ ] **Atmospheric Audio Layer**:
    - [ ] Source/create cosmic ambient background music (2-3 minute loop)
    - [ ] Implement dynamic ambient layers (calm, active, tense)
    - [ ] Add stage transition musical stingers
    - [ ] Create environmental audio atmosphere
  - [ ] **Dynamic Audio Scaling**:
    - [ ] Implement intensity-based volume scaling
    - [ ] Add gameplay state audio adaptation
    - [ ] Create wave composition enhancements
    - [ ] Optimize audio performance for complex scenes
  - [ ] **Advanced UI Interaction Audio**:
    - [ ] Complete UI sound integration across all menus
    - [ ] Add accessibility audio cues
    - [ ] Implement audio options menu
    - [ ] Create audio feedback for tutorial elements


### Milestone 2.4: Meta-Progression Systems
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

## Phase 3: Release Preparation (2-3 weeks) - SEPTEMBER 2025
**Goal**: Commercial release readiness

### Milestone 3.1: Steam Integration & Distribution
- **Target**: Platform readiness
- **Complexity**: 3 points
- **Tasks**:
  - [ ] Steam SDK integration
  - [ ] Steam achievements implementation
  - [ ] Cloud saves functionality
  - [ ] Trading cards setup (if applicable)
  - [ ] Store page optimization

### Milestone 3.2: Performance & Quality Assurance
- **Target**: Commercial stability
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Performance optimization and stress testing
  - [ ] Bug fixing and edge case handling
  - [ ] Accessibility options implementation
  - [ ] Resolution and graphics options
  - [ ] Comprehensive QA testing

### Milestone 3.3: Marketing & Launch Preparation
- **Target**: Market readiness
- **Complexity**: 2 points
- **Tasks**:
  - [ ] Trailer creation and marketing materials
  - [ ] Press kit preparation
  - [ ] Community hub setup
  - [ ] Beta testing program execution
  - [ ] Launch day preparation

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline** ✅ **Framework exists**
   - [ ] Weekly playtesting sessions with corrected timeline
   - [ ] Analytics integration for player behavior
   - [ ] A/B testing for difficulty curves

2. **Technical Debt Management** ✅ **System operational**
   - [ ] Bi-weekly code quality reviews (accelerated schedule)
   - [ ] Performance profiling checkpoints
   - [ ] Documentation maintenance

3. **Community Building**
   - [ ] Development blog/devlog with accelerated timeline
   - [ ] GIF creation pipeline for progress sharing
   - [ ] Discord community setup
   - [ ] Beta testing program

---

## Risk Mitigation (Updated for Accelerated Timeline)

### Identified Risks:
1. **Audio Implementation Complexity**: May require learning curve
   - **Mitigation**: Start with simple placeholder sounds, iterate rapidly
   - **Fallback**: Asset store integration for basic audio

2. **UI Modernization Scope**: Could expand beyond estimates
   - **Mitigation**: Leverage existing debug UI patterns, focus on core functionality first
   - **Fallback**: Keep OnGUI for non-critical elements initially

3. **Velocity Sustainability**: 17.8 points/month may not be sustainable
   - **Mitigation**: Weekly velocity recalculation with rolling averages
   - **Fallback**: Conservative 12 points/month fallback estimate

4. **Scope Creep**: Feature additions could derail timeline
   - **Mitigation**: Strict feature freeze after Phase 1, focus on polish only

---

## Success Metrics (Updated)

### Development Velocity Tracking:
- **Target Velocity**: 15+ complexity points/month (conservative estimate)
- **Weekly Check-ins**: Monday velocity recalculation
- **Milestone Gates**: No phase advancement without 90% completion

### Quality Gates:
- **Fundamentals**: 90% of players understand core loop in < 2 minutes
- **Performance**: Consistent 60 FPS on minimum spec hardware
- **Player Experience**: Average session length > 20 minutes
- **Stage Completion**: >70% completion rate for early stages

---

## Revised Timeline Summary
- **Phase 1**: 2-3 weeks (July 2025) - Audio & UI Foundation
- **Phase 2**: 3-4 weeks (August 2025) - Content & Polish  
- **Phase 3**: 2-3 weeks (September 2025) - Release Preparation

**Total**: 2.5 months to commercial release candidate

### **Key Milestones** (Updated June 27, 2025):
- **July 8**: Audio system operational (accelerated due to early start)
- **July 22**: UI modernization complete
- **August 15**: Content and polish complete
- **September 1**: Release candidate ready
- **September 15**: Commercial release target (2 weeks earlier)

---

## Development Velocity Dashboard

### **Current Metrics** (As of July 13, 2025):
- **Measured Velocity**: 17.8 complexity points/month (baseline)
- **Recent Sprint Velocity**: 7 points in 5 days (July 8 sprint)
- **Development Period**: April 29 - July 13, 2025 (75 days)
- **Completed Complexity**: 40 points (including wave messages, transitions, audio)
- **Remaining Work**: 35 complexity points
- **Projected Completion**: September 15, 2025 (on track)

### **Weekly Tracking** (Updated):
- **Week of June 24**: Audio system initiated ✅
- **Week of June 28**: Audio foundation established ✅
- **Week of July 1**: Development sprint planning
- **Week of July 8**: Major completion sprint ✅
  - Wave completion messages implemented
  - Stage transitions completed
  - Audio system architecture finalized
- **Week of July 15**: UI modernization in progress 🔄
- **Week of July 22**: UI modernization target completion

---

*Last Updated: July 13, 2025*  
*Next Review: July 30, 2025 (monthly review cycle)*  
*Development Phase: Phase 1 - UI Modernization Active*