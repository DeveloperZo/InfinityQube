# Game Overview

> This document details the Game Overview section of Infinity Cube's Game Design Document. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Defines the core gameplay loop, setting, and primary gameplay elements, ensuring alignment with the project's thematic vision and core mechanical identity.

## 2.1 Concept
Players interact with a tile-based grid, placing markers to intercept advancing cubes. Each cube type has unique properties and consequences, creating an interlocking system that rewards predictive planning and resource management on an evolving playfield.

## 2.2 Core Gameplay Loop
1. **Marker Placement**
   - Place markers on key tiles for incoming cubes
2. **Cube Capture**
   - Capture cubes as they advance
3. **Detonation Control**
   - Trigger detonations to clear areas of cubes
4. **Resource Management**
   - Manage limited markers
   - Track advantaged-tile charges
5. **Wave Progression**
   - Progress through cube patterns
   - Adapt to increasing challenges
   > Note: Automatic scaling planned for future implementation
6. **Grid Adaptation**
   - Adapt strategy to transforming grid state

## 2.3 Setting
### Minimalist Abstract World
- **Geometry & Color**
  - Clean shapes communicate mechanics
  - Distinct palettes for clear readability
- **Depth & Feedback**
  - Height variation shows state changes
  - Subtle particle effects signal interactions
- **Atmosphere**
  - Dark, star-flecked backdrops
  - Themes of recursion and scale

## 2.4 Gameplay Elements
### Grid & Tile States
| State | Description | Properties |
|-------|-------------|------------|
| Normal | Standard tile | Markable, default height |
| Corrupted | Obstacle tile | Unmarkable, created by Black cubes |
| Advantaged | Enhanced tile | Stores detonation charges |

### Cube Types
| Type | Color | Properties | Effects |
|------|--------|------------|---------|
| Normal | Gray | Basic movement | Minor escape penalty |
| Blue | Blue | Creates detonation points | Transforms tiles to Advantaged |
| Black | Black | Uncapturable | Corrupts tiles, drains charges |

---
**Last Updated:** [Current Date]  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Gameplay Mechanics](3_GameplayMechanics.md)
- [Level Design](4_LevelDesign.md)