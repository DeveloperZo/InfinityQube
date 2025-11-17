# Development Roadmap

> **Document Purpose:** This is the definitive, actionable guide for what to work on next, in priority order. For a historical record of completed milestones, see [MilestoneTracking.md](MilestoneTracking.md).

## Overview
This roadmap outlines the development path from current production-ready state to a polished, commercial release with validated core mechanics across all complexity levels.

**🚀 UPDATED TIMELINE**: Based on measured development velocity of 18.9 complexity points/month (updated November 16, 2025), reflecting actual development progress. Timeline revised for realistic completion targets.

---

## Current Status (November 16, 2025)
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
- **Audio system** - ✅ Complete AudioManager implementation with 7+ subsystems:
  - AudioManager.cs (singleton core)
  - AudioSourcePool.cs (resource management)
  - AudioPlaybackSystem.cs (playback control)
  - AudioVolumeController.cs (volume management)
  - CubeAudioSystem.cs (cube-specific sounds)
  - CubeAudioConfiguration.cs (configuration data)
  - AudioTesting.cs (debug tools)

### 🔄 **TOP PRIORITY - IN PROGRESS** 
- **UI Modernization** - ⚡ HIGHEST PRIORITY (OnGUI → Unity UI conversion) - Still using OnGUI, needs immediate attention

---

## Phase 1: UI Foundation & Content (Current Phase) - NOVEMBER 2025
**Goal**: Eliminate critical experiential gaps and modernize presentation



### Milestone 1.2: First Pass at Complete Stage 1 Content
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

### Milestone 1.1: UI Modernization - HIGHEST PRIORITY
- **Target**: Replace OnGUI with modern Unity UI
- **Complexity**: 3 points 
- **Timeline**: Immediate priority - November 2025
- **Status**: 🔴 CRITICAL - Still using OnGUI
- **Tasks**:
  - [ ] Convert HUD elements from OnGUI to Unity UI
  - [ ] Create main menu and options system
  - [ ] Implement pause menu with restart functionality
  - [ ] Add stage complete/failed screens
  - [ ] Modernize charge indicators and cooldown displays
  - [ ] Remove all OnGUI dependencies


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

## Phase 2: Content & Polish Completion (4-5 weeks) - Q1 2026
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

### Milestone 2.3: Audio Content & Polish
- **Target**: Add actual audio content to completed system
- **Complexity**: 2 points (system already complete)
- **Tasks**:
  - [ ] Source/create audio clips for existing AudioManager system
  - [ ] Test audio integration with all 7+ subsystems
  - [ ] Balance volume levels across categories
  - [ ] Add music and ambient sounds


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

## Phase 3: Release Preparation (3-4 weeks) - Q2 2026
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
1. **UI Modernization Delay**: OnGUI still in use after 4+ months
   - **Mitigation**: Prioritize UI conversion as highest priority task
   - **Fallback**: Focus on core gameplay UI first, defer menus if needed

2. **Velocity Sustainability**: 18.9 points/month may fluctuate
   - **Mitigation**: Weekly velocity tracking with rolling averages
   - **Fallback**: Conservative 15 points/month estimate for planning

3. **Content Creation Scope**: Remaining stage content and polish
   - **Mitigation**: Focus on quality over quantity for initial release
   - **Fallback**: Launch with fewer but more polished stages

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
- **Phase 1**: 3-4 weeks (November-December 2025) - UI Modernization & Core Content
- **Phase 2**: 4-5 weeks (January-February 2026) - Content & Polish  
- **Phase 3**: 3-4 weeks (March 2026) - Release Preparation

**Total**: 3-4 months to commercial release candidate

### **Key Milestones** (Updated November 16, 2025):
- **December 2025**: UI modernization complete (critical priority)
- **January 2026**: Stage content finalized
- **February 2026**: Polish and audio content complete
- **March 2026**: Release candidate ready
- **April 2026**: Commercial release target

---

## Development Velocity Dashboard

### **Current Metrics** (As of November 16, 2025):
- **Measured Velocity**: 18.9 complexity points/month (updated from MilestoneTracking)
- **Development Period**: April 29 - November 16, 2025 (7.3 months)
- **Completed Complexity**: 44 points (including full AudioManager implementation)
- **Remaining Work**: 31 complexity points
- **Projected Completion**: April 2026 (based on current velocity)

### **Priority Tracking** (Updated November 16, 2025):
- **Immediate Priority**: UI Modernization - OnGUI replacement
- **Next Priority**: Complete Stage 1 content
- **Following**: Stage content creation and polish
- **Status**: Audio system COMPLETE (7+ files implemented)
- **Critical Issue**: UI still using OnGUI after 4+ months

---

*Last Updated: November 16, 2025*  
*Next Review: December 1, 2025 (bi-weekly review cycle)*  
*Development Phase: Phase 1 - UI Modernization CRITICAL PRIORITY*