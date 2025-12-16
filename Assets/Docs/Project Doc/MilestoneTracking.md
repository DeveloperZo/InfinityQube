# Milestone Tracking

> **Document Purpose:** This document is a historical record of completed milestones. For current priorities and upcoming work, see [Roadmap.md](Roadmap.md). For velocity metrics, see [Development Velocity](../Technical%20Doc/DevelopmentVelocity.md).

## Point Estimation Scale

> **1 point ≈ 1 day of active development**  
> **5 points ≈ 1 week of active development**  
> **22 points/month** (measured velocity)

---

## Completed Milestones ✅

### Milestone 1.0: Core Game Development (65 pts)
*April-July 2025 (~3 months active work)*

| System | Points | Completion Date | Commit |
|--------|--------|-----------------|--------|
| Project initialization & architecture | 10 | May 2025 | Initial |
| Basic gameplay loop | 5 | May 15, 2025 | - |
| Wave management system | 7 | June 15, 2025 | - |
| Player action system | 7 | June 10, 2025 | - |
| Four-tier marker system | 8 | June 20, 2025 | `bd71b29` |
| Face painting mechanics | 5 | June 20, 2025 | `905032c` |
| Corruption/enhancement tiles | 5 | June 22, 2025 | - |
| Debug infrastructure | 8 | June 20, 2025 | - |
| Audio system | 7 | July 8, 2025 | `24da3f1` |
| Documentation & integration | 3 | June 23, 2025 | - |

### Milestone 1.2: Demo Stage Content (5 pts)
*July 2025 (~1 week work)*

| System | Points | Completion Date | Commit |
|--------|--------|-----------------|--------|
| Wave completion messages | 2 | July 8, 2025 | `62f574f` |
| Stage transition system | 2 | July 8, 2025 | - |
| Demo loop completion | 1 | July 8, 2025 | -

### Milestone 1.5: Demo Stage Content (5 pts) ✅
*December 2025 (~1 week work)*

| System | Points | Completion Date | Notes |
|--------|--------|-----------------|-------|
| Tutorial/demo stage refinement | 1 | Dec 15, 2025 | Stage 0 with highlight sequences |
| Messaging system | 1 | Dec 15, 2025 | MessageHighlightManager system |
| Stage/Wave sequence framework | 1 | Dec 15, 2025 | Highlight sequences with timing, triggers, validation |
| Complete 3-wave configuration | 0.5 | Dec 16, 2025 | All sequences working for all 3 waves |
| Splash to Stage loop | 0.5 | Dec 16, 2025 | Tutorial loop with PlayerPrefs flag system |
| Stage success transition | 0.5 | Dec 16, 2025 | Completion message, K key handling, return to Splash |
| Data collection verification | 0.5 | Dec 16, 2025 | PlayerStatisticsManager tracks tutorial progress | |

### Milestone 1.1: Refine Markers Implementation (14 pts) ✅
*December 2025 (~2 weeks active work)*

| System | Points | Completion Date | Notes |
|--------|--------|-----------------|-------|
| Marker terminology standardization | 2 | Nov 30, 2025 | Unified naming |
| Unified marker input system | 2 | Nov 30, 2025 | Mode keys 1-4, F to place |
| Collision matrix design | 1 | Dec 1, 2025 | All 16 combinations |
| Collision matrix implementation | 3 | Dec 9, 2025 | CubeCollisionManager complete |
| Line Divider System | 2 | Dec 9, 2025 | Blue/red zones, marker restriction |
| Resonance System | 2 | Dec 9, 2025 | Phaseable state (2 moves) |
| Enhanced Face Painting | 2 | Dec 9, 2025 | Front face, 3-move telegraph |
| Penalty/Reward System | 1 | Dec 9, 2025 | Line movement on escape/success |
| Marker Economy | 2 | Dec 9, 2025 | Stage/wave grants with caps |

**Total Completed**: 94 points (normalized)

---

## Active Development Tracking

### Next Up: Milestone 1.2 (RPG/Progression Ideation)
**Total Points**: 10 pts | **Status**: Ready to start

| Item | Points | Status | Notes |
|------|--------|--------|-------|
| RPG elements design | 3 pts | Pending | Brainstorm progression |
| Hub area concept | 2 pts | Pending | Design and layout |
| Meta-progression systems | 3 pts | Pending | Unlocks, upgrades |
| Progression economy | 2 pts | Pending | Resources, currencies |

### Velocity Summary
| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** |
| Total Commits | 260+ |
| Total Points Completed | 89 pts |
| Remaining Points | ~170 pts |
| Est. Time to Release | ~7.7 months @ current velocity |

---

## Commit Statistics

### Monthly Distribution
| Month | Commits | Points | Trend |
|-------|---------|--------|-------|
| April 2025 | 18 | ~10 pts | 🟢 Start |
| May 2025 | 106 | ~25 pts | 🟢 Peak |
| June 2025 | 85 | ~25 pts | 🟢 High |
| July 2025 | 15 | ~10 pts | 🟡 Tapering |
| Aug-Oct 2025 | 0 | 0 pts | ⚫ Hiatus |
| Nov 2025 | 11 | ~3 pts | 🟢 Resumed |
| Dec 2025 | 15+ | ~5 pts | 🟢 Active |

### Key Commit References
| Milestone | Commit | Date |
|-----------|--------|------|
| Four-tier markers | `bd71b29` | June 23 |
| PlayerMarkerSystem | `905032c` | June 20 |
| AudioManager | `24da3f1` | July 4 |
| Unified marker input | `cb69513` | Nov 30 |
| Face painting | `8b8c2f7` | Dec 8 |
| Auto-capture mechanics | `359c1ff` | Dec 8 |

---

**Last Updated**: December 9, 2025

### Documentation Notes
- **Point Scale**: 1 pt ≈ 1 day, 5 pts ≈ 1 week, 22 pts/month measured
- **File Size Violations**: 12 files exceed limits - see [Technical Critiques](../Technical%20Doc/TechnicalCritiques.md)
- **Full Velocity Analysis**: See [Development Velocity](../Technical%20Doc/DevelopmentVelocity.md)
