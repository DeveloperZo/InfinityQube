# Puzzle Design Heuristics

> **Purpose**: Document puzzle design rules and constraints to ensure generated waves are solvable and properly teach mechanics. This is a living document updated as design learnings emerge.

---

## Core Player Constraints

### Movement
- Players can only move to **connected cells in cardinal directions** (up, down, left, right)
- **No diagonal movement**
- Player must have at least one valid escape path at all times

### Survival
- **Infinity cubes crush the player** if they occupy the same cell
- Player cannot pass through Infinity cubes
- Waves must always have a survivable path for the player

---

## Infinity Cube Placement Rules

### AVOID (Until Required Tools Unlocked)
- **Checkerboard/criss-cross patterns** - blocks cardinal movement; requires **Infinity Marker** or **Advanced Grid**
- **Full row barriers at player spawn height** - no escape path without special tools
- **Enclosed boxes** with no exit - player trapped without special tools

> **Note**: These patterns become valid puzzle elements once players unlock Infinity Marker (Stage 10+) or Advanced Grid mechanics. Save them for late-game content.

### SAFE PATTERNS
- **Vertical lanes** - player can move left/right to avoid
- **Horizontal lanes** - player can move up/down to avoid  
- **Scattered obstacles** with gaps of 2+ cells between them
- **Corner placements** - leaves center open for movement
- **Partial walls** - always leave at least 2-cell gap for player escape

### Example - BAD (Checkerboard)
```
I . I . I . I .    <- Infinity cubes in alternating pattern
. I . I . I . I    <- Player has NO cardinal escape route
I . I . I . I .    <- UNSOLVABLE - player will be crushed
```

### Example - GOOD (Vertical Lanes)
```
I . . I I . . I    <- Infinity walls on edges and center
I . . I I . . I    <- Player can move horizontally between lanes
. . . I I . . .    <- Gaps allow escape
```

---

## Matrix Marker Puzzle Design

### Design Philosophy
Matrix markers have **2x2 area effect capture**. Puzzles should require this mechanic.

### Teaching Progression
1. **Barrier Basics** - Simple Infinity walls blocking direct access to cubes
2. **Walled Gardens** - Enclosed areas where cubes are trapped behind Infinity
3. **Fortress Lanes** - Vertical Infinity lanes creating corridors
4. **Complex Barriers** - Multiple barrier configurations requiring marker positioning
5. **Mastery Test** - Combines all concepts + teaser for next mechanic

### Key Principle
> **Without Matrix markers, the wave should be unsolvable** - Infinity barriers block direct Unit marker placement, forcing area-effect capture

---

## Cube Type Reference

| Type | Enum Value | Letter | Color | Behavior |
|------|------------|--------|-------|----------|
| Unit | 0 | U | Blue | Basic capture |
| Matrix | 1 | M | Green | Area capture synergy |
| Infinity | 2 | I | Yellow/Gold | Obstacle, crushes player |
| Recursion | 3 | R | Red/Orange | Multi-hit, requires multiple captures |

---

## Stage-Specific Rules

### Stages 0-2: Unit Fundamentals
- **Cubes**: Unit + Infinity only
- **Focus**: Basic movement, timing, Unit marker placement
- Infinity used sparingly as obstacles

### Stages 3-4: Matrix Cube Learning
- **Cubes**: Unit + Matrix + Infinity
- **Focus**: Understanding Matrix cube behavior
- No Recursion cubes

### Stage 5: Matrix Marker Mastery
- **Cubes**: Unit + Matrix + Infinity (+ Recursion teaser in final wave only)
- **Focus**: Using Matrix marker area effect to capture behind barriers
- **Design**: Infinity barriers that make Unit markers insufficient
- **Recursion**: Only in Wave 5_05 as teaser for future content

### Stages 6+: Future Content
- Recursion mechanics introduced
- Infinity marker (allows passing through Infinity cubes)
- Advanced grid mechanics

---

## Solvability Checklist

Before finalizing any wave:

- [ ] Player has cardinal escape paths from spawn position
- [ ] No checkerboard Infinity patterns
- [ ] All capturable cubes are reachable (directly or via area effect)
- [ ] Cube density appropriate for available markers
- [ ] Movement speed allows reaction time
- [ ] Difficulty progression makes sense within stage

---

## Tool-Gated Patterns

Some patterns are unsolvable until specific tools are unlocked:

| Pattern | Problem | Required Tool | Unlocked At |
|---------|---------|---------------|-------------|
| **Checkerboard Infinity** | No cardinal escape | Infinity Marker | Stage 10+ |
| **Full row barriers** | Blocks all movement | Infinity Marker | Stage 10+ |
| **Enclosed boxes** | Player trapped | Advanced Grid | TBD |

### Design Principle
> Pattern complexity should match available tools. A "hard" puzzle uses available tools cleverly; an "impossible" puzzle requires tools the player doesn't have.

---

## Known Issues / Future Solutions

| Problem | Current Status | Future Solution |
|---------|----------------|-----------------|
| Infinity blocks all paths | Avoid in design | Infinity Marker |
| Complex patterns too hard | Manual validation | Advanced Grid features |
| Player positioning limits | Cardinal movement only | TBD |

---

**Last Updated**: January 18, 2026

### Change Log
- **Jan 18, 2026**: Initial document created
  - Documented cardinal movement constraint
  - Added Infinity placement rules (no checkerboard)
  - Added cube type reference with correct enum values
  - Added stage-specific cube rules
