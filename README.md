## Infinity Cube - Game Design Document (v0.2 - Working Prototype)

### 1. Game Overview
**Game Concept:**  
*Infinity Cube* is a grid-based strategic puzzle game inspired by *Intelligent Qube*, *A Trip to Infinity*, *Limbo*, and *Hades*. Players interact with a tile-based board, marking specific tiles in anticipation of incoming cubes that march across the platform. Each cube carries a level of exponential growth pressure, and failure to intercept them before they escape triggers system-level consequences. The prototype emphasizes clarity, turn-based logic, and visual feedback for tactical decision-making.

**Platform:** Unity (3D Grid Logic)

**Core Inspirations:** Intelligent Qube (mechanics), A Trip to Infinity (aesthetic & music), Limbo (atmosphere), Hades (roguelike structure)

**Target:** PC / Unity Desktop

---

### 2. Current Gameplay Systems (MVP Scope)

#### ✅ Tile Grid System
- 6x6 grid generated at runtime
- Each tile can:
  - Be marked (red cylinder visual + tile tint)
  - Be cleared manually or after interaction

#### ✅ Player Control (Selector)
- Player moves a visible selector cube across the grid
- Can mark/unmark tiles with `Space`
- Limit of 2 markers per round

#### ✅ Cube Behavior
- Cubes spawn from top row (Z=5) and move row-by-row to Z=0
- Each cube has a level (currently default: 1)
- If it hits a marked tile:
  - Level is reduced by 1
  - If level = 0 → cube is destroyed
  - If level > 0 → continues moving
- If it escapes off-grid (Z < 0):
  - Logs escape message (future versions will punish escapes)

#### ✅ Wave Manager
- Handles round progression
- Spawns wave of cubes (random X, fixed top Z)
- Moves each cube forward every 0.5s
- Ends round when all cubes are resolved
- Resets marker state and reenables player input

---

### 3. Technical Summary

#### Scripts:
- `GridManager.cs`: Manages tile generation and global marker clearing
- `Tile.cs`: Handles individual marker visuals, toggling, and feedback
- `PlayerController.cs`: Controls selector movement, marker logic
- `CubeBehavior.cs`: Handles cube movement, marker detection, destruction
- `WaveManager.cs`: Spawns and drives cube progression, manages round state

#### Visual Feedback:
- Selector = yellow cube, hover height = 0.2f
- Marked tile = red cylinder + red-tinted tile
- Post-capture = tile resets to original color

#### Structural Cleanliness:
- All tiles parented to `GridManager`
- All cubes parented to `CubeParent` (for hierarchy clarity)

---

### 4. Known Gaps / Next Steps
- Escaped cubes do not yet trigger duplication or board degradation
- No cube return logic
- No difficulty scaling, cube level variance, or powerups
- No procedural generation of future waves
- No animation or sound feedback for captures/damage

---

### 5. Future Features (Post-Prototype Phase)
- Cube duplication or mutation on escape
- Platform shrinkage or tile destruction based on escapes
- Meta-progression system (insight/unlocks)
- Mutators per run (e.g., altered cube paths or resistances)
- UI feedback (marker counter, waves survived)
- Procedural cube spawn patterns
- Music/sound design for atmosphere
- Run-based scoring and escalation

---

### 6. Version Summary
**Version:** 0.2 (Functional demo)
**Date:** April 29, 2025

**Validated Features:**
- Tile grid system
- Selector and marker placement
- Cube wave movement and destruction
- Round-based play loop

**Next Milestone:** Escape consequences and exponential return logic
