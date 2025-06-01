# Gameplay Mechanics

> This document details the core mechanical systems of Infinity Cube. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Details the functional rules and systemic interactions, clearly outlying the behavior of grid, cubes, markers, detonation systems, and wave progression.

## 3.1 Grid System
### Core Structure
- Configurable grid dimensions
- Dynamic tile height variations
- Boundary collision detection
- Per-tile state management

### Tile States
| State | Behavior | Interactions |
|-------|----------|--------------|
| Normal | Default state | Accepts markers |
| Marked | Player activated | Triggers cube capture |
| Corrupted | Black cube effect | Blocks movement |
| Advantaged | Blue cube effect | Enhanced detonations |

## 3.2 Cube Behavior
### Movement System
- Consistent step movement
- Forward progression only
- Variable speed states
- Configurable collision detection

### Type Properties
| Type | Behavior | Capture Effect | Escape Effect |
|------|----------|----------------|----------------|
| Normal | Basic movement | Removed + Score | Minor penalty |
| Blue | Enhanced | Creates Advantaged | Tile effect |
| Black | Unstoppable | Cannot capture | Corrupts tiles |

## 3.3 Marker System
- Configurable marker limits
- Visual placement indicators
- Placement validation rules
- Auto-activation system

## 3.4 Detonation System
### Charge Levels
- Small area effect
- Large area effect
- Configurable charge storage

### Mechanics
- Manual activation
- Chain reactions
- Visual/Audio feedback
- Charge management

## 3.5 Player Controls
| Action | Input | Effect |
|--------|-------|--------|
| Movement | Arrow Keys / WASD | Grid navigation |
| Place Marker | Space | Toggle marker |
| Speed Up | Left Shift (hold) | Increase speed |
| Detonate | E | Trigger charges |
| Confirm Wave | Enter | Progress wave |
| Pause | Escape | Toggle pause |

## 3.6 Statistics System
### Tracking
- Cube captures by type
- Escape tracking
- Marker statistics
- Detonation usage
- Tile state changes
- Player performance
- Time tracking
- Movement tracking

### Performance Metrics
- Capture efficiency
- Resource utilization
- Survival metrics
- Wave completion
- Grid management

---
**Last Updated:** June 1, 2025  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Level Design](4_LevelDesign.md)