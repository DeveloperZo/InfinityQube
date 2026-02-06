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
Matrix markers have **2x2 area effect capture** by default.

**Key Combo**: Matrix marker + Matrix cube = **3x3 area effect**
- When a Matrix marker hits a Matrix cube specifically, the capture area expands to 3x3
- This is the core mechanic Stage 4 teaches
- Stage 5 then tests mastery with Infinity barriers

### Teaching Progression
1. **Barrier Basics** - Simple Infinity walls blocking direct access to cubes
2. **Walled Gardens** - Enclosed areas where cubes are trapped behind Infinity
3. **Fortress Lanes** - Vertical Infinity lanes creating corridors
4. **Complex Barriers** - Multiple barrier configurations requiring marker positioning
5. **Mastery Test** - Combines all concepts + teaser for next mechanic

### Key Principle
> **The puzzle must be uniquely solvable via the 3x3 combo** - Place capturable cubes **behind** Infinity barriers where Unit markers cannot reach. The 3x3 area effect "reaches around" the Infinity to capture blocked cubes.

### Design Pattern: "Reach Behind"
```
Player side    |  Barrier  |  Blocked cubes
   M           |     I     |     U U U
               |           |
```
- Matrix cube (M) positioned **adjacent** to Infinity barrier
- Infinity (I) blocks direct Unit marker access  
- Unit cubes (U) behind barrier are ONLY capturable via 3x3 area effect
- Hitting Matrix cube with Matrix marker creates 3x3 that includes blocked cubes

### Critical: 3x3 Range Calculation
The 3x3 is centered on the Matrix cube. For a Matrix at position (X,Y):
- Area covers: (X-1 to X+1, Y-1 to Y+1)
- To reach cubes at column X+2 (two columns away), the Matrix must be at X+1 (adjacent to barrier)

**Example**: Matrix at (2,1), Infinity at (3,1)
- 3x3 covers columns 1,2,3 - reaches the Infinity column but NOT column 4
- Cubes at (3,0), (3,2) are capturable (same column as Infinity, different rows)
- Cubes at (4,*) require Matrix at (3,1) which is blocked by Infinity

### Timing Element
When blocked cubes start above the 3x3 range:
- Player must **wait** for cubes to move down into the capture zone
- Then trigger the 3x3 at the right moment
- This adds strategic timing to the "reach behind" mechanic

---

## Recursion Swap Puzzle Design

### Design Philosophy
Recursion markers create **swap markers** that reposition cubes on the grid. Unlike Matrix's area capture, Recursion is an indirect repositioning tool - like a chess knight, it rearranges the board to create new opportunities.

**Key Mechanic**: + Pattern Swap
- Cardinal neighbors swap positions around collision point
- Horizontal swap: West ↔ East positions swap
- Vertical swap: North ↔ South positions swap
- Player selects direction before triggering (arrow keys)
- Manual trigger (R key) like other cube markers

### Strategic Role
Recursion solves problems Matrix cannot:
- **Breaking Infinity Walls**: Swaps can move Infinity cubes to create survival gaps
- **Repositioning Value Cubes**: Moves valuable cubes (Matrix, Unit) into better capture positions
- **Indirect Strategy**: Requires reading the board and planning ahead
- **Complementary to Matrix**: Matrix captures areas, Recursion rearranges positions

### Teaching Progression
1. **Basic Swaps** - Simple repositioning scenarios (move one cube to better position)
2. **Infinity Wall Breaking** - Use swaps to create gaps in Infinity walls
3. **Multi-Swap Sequences** - Chain multiple swaps to solve complex puzzles
4. **Empowered Swaps** - Recursion+Recursion collisions create 2-charge swaps (swap + capture)
5. **Mastery Test** - Puzzles requiring both Matrix and Recursion working together

### Key Principle
> **The puzzle must require repositioning, not just capture** - Place cubes in positions where direct capture is blocked or inefficient. Swaps must create new opportunities that didn't exist before.

### Design Pattern: "Swap or Die"
```
Row 3: ∞ ∞ ∞ ∞ ∞   <- Full Infinity wall, no gap
Row 2: U ∞ U ∞ U
Row 1: U U U U U
       1 2 3 4 5
         ↑
      Player (will be crushed)
```
- Full Infinity wall at row 3 blocks all escape paths
- Without Recursion: Player gets crushed when wall reaches them
- With Recursion: Swap Infinity cubes to create gap, player survives

### Swap Direction Selection
Players must choose swap direction before triggering:
- **Horizontal (Row Swap)**: Left/Right arrow keys - swaps W↔E positions
- **Vertical (Column Swap)**: Up/Down arrow keys - swaps N↔S positions
- **Visual Preview**: Hover icons appear above N, S, E, W positions showing swap destinations
- **Default**: If no direction selected before wave move, defaults to horizontal (row swap)

### Empowered Swaps (Recursion + Recursion)
When Recursion collides with Recursion:
- **Instant Capture**: Recursion cube is captured immediately
- **2-Charge Swap Marker**: Creates empowered swap with two independent choices:
  - **Swap Axis**: Player chooses horizontal or vertical for repositioning
  - **Capture Axis**: Player chooses opposite axis for capturing cubes
- **Strategic Depth**: Player must decide which axis to use for swap vs capture

### Edge Handling
- **Stop at Edge**: Swaps cannot wrap around grid boundaries
- **Boundary Validation**: Only valid positions within grid are swapped
- **Empty Cells**: Infinity cubes can swap with empty space (repositioning, not capture)

### Multi-Hit System
Wave Recursion cubes require **2 hits** to capture:
- **First Hit**: Applies damage, visual feedback shows damage state
- **Second Hit**: Captures the cube
- **Swap Marker Creation**: Unit+Recursion and Recursion+Unit collisions create swap markers (applies damage on first hit)

### Solvability Criteria for Swap Puzzles
Before finalizing any swap-dependent wave:
- [ ] Player has at least one valid swap marker available
- [ ] Swap can create a solvable path (survival or capture opportunity)
- [ ] Infinity walls can be broken with available swaps
- [ ] Value cubes can be repositioned into capturable positions
- [ ] Direction selection is clear (visual preview works)
- [ ] Default direction provides valid solution if player doesn't select

### Puzzle Types
1. **Survival Puzzles**: Infinity walls that must be broken with swaps
2. **Repositioning Puzzles**: Value cubes in wrong positions, need swaps to move them
3. **Sequencing Puzzles**: Multiple swaps required in specific order
4. **Hybrid Puzzles**: Combine Matrix area capture with Recursion repositioning

---

## Cube Type Reference

| Type | Enum Value | Letter | Color | Behavior |
|------|------------|--------|-------|----------|
| Unit | 0 | U | Blue | Basic capture |
| Matrix | 1 | M | Green | Area capture synergy |
| Infinity | 2 | I | Yellow/Gold | Obstacle, crushes player |
| Recursion | 3 | R | Red/Orange | Multi-hit (2 hits), creates swap markers for repositioning |

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
- [ ] All capturable cubes are reachable (directly or via area effect or swap repositioning)
- [ ] Cube density appropriate for available markers
- [ ] Movement speed allows reaction time
- [ ] Difficulty progression makes sense within stage
- [ ] If swap-dependent: At least one valid swap marker available, swap creates solvable path
- [ ] If Infinity walls present: Either natural gaps exist or swaps can create gaps

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

**Last Updated**: January 27, 2026

### Change Log
- **Jan 18, 2026**: Initial document created
  - Documented cardinal movement constraint
  - Added Infinity placement rules (no checkerboard)
  - Added cube type reference with correct enum values
  - Added stage-specific cube rules
