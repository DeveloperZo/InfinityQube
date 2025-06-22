# Development Roadmap

## Overview
This roadmap outlines the development path from current POC state to a polished, presentable game with validated core mechanics across all complexity levels.

---

## Phase 1: Core Demo Polish (Current - 2 weeks)
**Goal**: Create a polished demonstration of fundamental mechanics

### Milestone 1.1: Fundamentals Demo Stage ✅ IN PROGRESS
- **Mechanics**: Individual markers (F/R), Black & Normal cubes only
- **Grid**: 5x20, clean visual presentation
- **Tasks**:
  - [ ] Hide advanced UI elements (area markers, cube markers)
  - [ ] Implement clean wave spawn without debug messages
  - [ ] Add visual polish to marker placement/detonation
  - [ ] Create focused tutorial tooltips for F and R keys only
  - [ ] Implement basic audio feedback (3 sounds: place, detonate, capture)
  - [ ] Add simple particle effects for cube destruction
  - [ ] Create win/lose conditions with clear messaging

### Milestone 1.2: Stage/Wave Integration Cleanup
- **Tasks**:
  - [ ] Refactor StageManager to properly orchestrate WaveManager
  - [ ] Clean up wave completion → stage completion flow
  - [ ] Remove circular dependencies between managers
  - [ ] Implement proper state machine for stage progression
  - [ ] Add stage restart without memory leaks

### Milestone 1.3: UI Modernization (Basics)
- **Tasks**:
  - [ ] Replace OnGUI with Unity UI for HUD elements
  - [ ] Create minimal, elegant charge indicator
  - [ ] Add pause menu with restart option
  - [ ] Implement stage complete/failed screens
  - [ ] Keep debug panels but make them hideable (F1 key)

---

## Phase 2: Expanded Mechanics Demo (2-3 weeks)
**Goal**: Validate that additional mechanics enhance rather than complicate

### Milestone 2.1: Area Markers Demo Stage
- **Mechanics**: Individual + Area markers (G/T), Blue + Black + Normal cubes
- **Grid**: 7x25
- **Tasks**:
  - [ ] Implement area marker visualization (2x2 preview)
  - [ ] Add blue cube → cube marker conversion system
  - [ ] Create UI for all three resource types (charges/cooldown/cube markers)
  - [ ] Design wave that teaches area efficiency
  - [ ] Add visual distinction between marker types
  - [ ] Implement cooldown visualization for area markers

### Milestone 2.2: Blue Cube Integration
- **Tasks**:
  - [ ] Polish blue cube capture → cube marker feedback
  - [ ] Add cube marker targeting system (Q key)
  - [ ] Create visual trail from cube marker to target
  - [ ] Balance blue cube spawn rates
  - [ ] Add statistics tracking for cube marker efficiency

---

## Phase 3: Advanced Mechanics Demo (3-4 weeks)
**Goal**: Prove that complex mechanics create depth, not confusion

### Milestone 3.1: Reinforced Cubes Demo Stage 
- **Mechanics**: All markers, Reinforced + Blue + Black + Normal cubes
- **Grid**: 9x30
- **Tasks**:
  - [ ] Implement CubeHealth component system
  - [ ] Add durability visualization (cracks, damage states)
  - [ ] Create multi-hit feedback (screen shake on partial damage)
  - [ ] Design waves that require strategic durability management
  - [ ] Add reinforced cube special behaviors (slower? different patterns?)

### Milestone 3.2: Performance & Polish Pass
- **Tasks**:
  - [ ] Implement object pooling for all cubes and effects
  - [ ] Optimize grid rendering for larger sizes
  - [ ] Add graphics quality options
  - [ ] Profile and fix frame drops
  - [ ] Stress test with 100+ cubes

---

## Phase 4: Environmental Mechanics Demo (4-5 weeks)
**Goal**: Validate that tile states add strategic depth

### Milestone 4.1: Face Painting & Tile States Demo
- **Mechanics**: All previous + Face painting + Corrupted/Enhanced tiles
- **Grid**: 11x35 with dynamic tile states
- **Tasks**:
  - [ ] Implement face painting visualization system
  - [ ] Create tile state change effects (corruption spread, enhancement glow)
  - [ ] Add face status HUD when hovering cubes
  - [ ] Implement painted cube → tile state propagation
  - [ ] Design waves that teach face orientation strategy
  - [ ] Add tile state persistence across waves

### Milestone 4.2: Complete Tile State System
- **Tasks**:
  - [ ] Corrupted tiles damage/slow player
  - [ ] Enhanced tiles provide benefits (faster charge regen?)
  - [ ] Fallen rows create permanent obstacles
  - [ ] Environmental hazard warning system
  - [ ] Strategic tile state management mechanics

---

## Phase 5: Full Game Structure (5-6 weeks)
**Goal**: Transform demos into cohesive game experience

### Milestone 5.1: Complete Stage Progression
- **Tasks**:
  - [ ] Create all 12 stages with proper difficulty curve
  - [ ] Implement save/load system for progress
  - [ ] Add stage select screen with completion indicators
  - [ ] Create transition cutscenes or flavor text
  - [ ] Balance resource limits per stage

### Milestone 5.2: Meta Systems
- **Tasks**:
  - [ ] Implement comprehensive statistics tracking
  - [ ] Add post-stage performance breakdown
  - [ ] Create efficiency ratings (bronze/silver/gold)
  - [ ] Add unlockable content or modes
  - [ ] Implement settings persistence

---

## Phase 6: Polish & Release Prep (6-8 weeks)
**Goal**: Create commercially viable product

### Milestone 6.1: Audio Complete
- **Priority Tasks** (from Critiques.md):
  - [ ] Complete audio implementation (biggest current gap)
  - [ ] Core action sounds with satisfying feedback
  - [ ] Dynamic audio based on danger level
  - [ ] Ambient space atmosphere
  - [ ] UI interaction sounds

### Milestone 6.2: Visual Polish
- **Tasks**:
  - [ ] Cosmic theme integration (particles, backgrounds)
  - [ ] Enhanced visual effects for all actions
  - [ ] Screen shake and juice for impacts
  - [ ] Accessibility options (colorblind modes)
  - [ ] Resolution and display options

### Milestone 6.3: Release Features
- **Tasks**:
  - [ ] Steam integration setup
  - [ ] Achievement system
  - [ ] Cloud saves
  - [ ] Trading cards (if applicable)
  - [ ] Press kit preparation

---

## High-Value Parallel Activities

### Throughout All Phases:
1. **Playtesting Pipeline**
   - [ ] Set up automated feedback collection (FeedbackCollector.cs)
   - [ ] Weekly playtesting sessions
   - [ ] Analytics integration for player behavior
   - [ ] A/B testing for difficulty curves

2. **Technical Debt Management**
   - [ ] Keep all files under 400 lines
   - [ ] Maintain single responsibility principle
   - [ ] Regular refactoring sessions
   - [ ] Performance profiling checkpoints

3. **Debug Infrastructure Enhancement**
   - [ ] Convert debug tools to development console
   - [ ] Add replay system for bug reproduction
   - [ ] Create automated testing scenarios
   - [ ] Build level editor from debug tools

4. **Community Building**
   - [ ] Development blog/devlog
   - [ ] GIF creation pipeline for progress sharing
   - [ ] Discord or community hub setup
   - [ ] Beta testing program

---

## Risk Mitigation

### Identified Risks (from documentation):
1. **Complexity Creep**: Each mechanic adds significant complexity
   - Mitigation: Strict playtesting gates between phases
   
2. **Tutorial Burden**: Face painting is non-intuitive
   - Mitigation: Interactive tutorial development in Phase 4
   
3. **Performance at Scale**: Large grids with many cubes
   - Mitigation: Early optimization in Phase 3
   
4. **Audio Implementation**: Currently the biggest gap
   - Mitigation: Prioritized in Phase 1 basics

---

## Success Metrics

### Per Demo Milestone:
- **Fundamentals**: 90% of players understand core loop in < 2 minutes
- **Area Markers**: Players naturally discover efficiency strategies
- **Reinforced Cubes**: Durability adds depth, not frustration
- **Tile States**: Environmental strategy feels integral, not tacked-on

### Overall:
- Consistent 60 FPS on minimum spec hardware
- Average session length > 20 minutes
- Stage completion rate > 70% for early stages
- Positive playtester feedback on each mechanic

---

## Timeline Summary
- **Phase 1**: 2 weeks - Core polish
- **Phase 2**: 3 weeks - Expanded mechanics
- **Phase 3**: 4 weeks - Advanced mechanics  
- **Phase 4**: 5 weeks - Environmental systems
- **Phase 5**: 6 weeks - Full game structure
- **Phase 6**: 8 weeks - Polish & release

**Total**: ~6 months to full release candidate

---

*Last Updated: [Current Date]*
*Next Review: After Fundamentals Demo completion*