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
| Demo loop completion | 1 | July 8, 2025 | - |

### Post-Hiatus Work (November-December 2025)
*Part of Milestone 1.1 (~5 pts delivered so far)*

| System | Points | Completion Date | Commit |
|--------|--------|-----------------|--------|
| Marker terminology standardization | 2 | Nov 30, 2025 | `8f1cbb1` |
| Unified marker input system | 2 | Nov 30, 2025 | `cb69513` |
| Collision matrix design | 1 | Dec 1, 2025 | - |

**Total Completed**: 75 points (normalized)

---

## Active Development Tracking

### Currently In Progress: Milestone 1.1 (December 2025)
**Total Points**: 14 pts | **Delivered**: ~5 pts | **Remaining**: ~9 pts

| Item | Points | Status | Notes |
|------|--------|--------|-------|
| Collision Matrix Implementation | 3 pts | Ready to code | Structure exists |
| Line Divider System | 3 pts | Designed | Dynamic difficulty |
| Resonance System | 2 pts | Designed | Infinity vs Infinity |
| Enhanced Face Painting | 2 pts | In Progress | Rotation mechanics |
| Penalty/Reward System | 2 pts | Designed | Line movement |
| Marker Economy | 2 pts | Designed | Per-stage grants |

### Velocity Summary
| Metric | Value |
|--------|-------|
| **Measured Velocity** | **22 points/month** |
| Total Commits | 250+ |
| Total Points Completed | 75 pts |
| Remaining Points | ~184 pts |
| Est. Time to Release | ~8.4 months @ current velocity |

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

**Last Updated**: December 8, 2025

### Documentation Notes
- **Point Scale**: 1 pt ≈ 1 day, 5 pts ≈ 1 week, 22 pts/month measured
- **File Size Violations**: 12 files exceed limits - see [Technical Critiques](../Technical%20Doc/TechnicalCritiques.md)
- **Full Velocity Analysis**: See [Development Velocity](../Technical%20Doc/DevelopmentVelocity.md)
