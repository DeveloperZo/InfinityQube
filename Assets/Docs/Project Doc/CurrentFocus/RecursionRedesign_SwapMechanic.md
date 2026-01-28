# Recursion Redesign: Swap Mechanic

> **Status**: Design Proposal  
> **Date**: January 27, 2026  
> **Purpose**: Define Recursion's identity as a repositioning tool via + pattern swap

---

## Problem Statement

Previous Recursion designs (multi-hit, cross capture, line capture) overlap too heavily with Matrix's area capture identity. Both tools end up being "capture more cubes" with different shapes.

---

## Solution: Recursion = Repositioning

Recursion doesn't capture. It **rearranges**.

### Tool Role Taxonomy

| Marker | Availability | Role | Action |
|--------|--------------|------|--------|
| Unit | Abundant | Precision | Single capture |
| Matrix | Limited | Efficiency | Area capture (2x2, 3x3) |
| Recursion | Limited | Repositioning | + pattern swap |
| Infinity | Limited | Specialist | Phases through ∞ |

---

## + Swap Mechanic

### How It Works

1. Player launches Recursion cube
2. Collides with Wave cube
3. Creates + swap marker at collision point
4. Cardinal neighbors swap positions:
   - North ↔ South
   - West ↔ East

```
Before swap:         After swap:
    N                    S
  W ● E        →       E ● W
    S                    N
```

### Collision Results

| Player Cube | Wave Cube | Result |
|-------------|-----------|--------|
| Unit | R | 1 swap charge: row OR column (player chooses) |
| Recursion | U | 1 swap charge: row OR column (player chooses) |
| Recursion | R | 2 swap charges: player chooses each independently |

### Player Agency

- **1 charge**: Player chooses row swap OR column swap
- **2 charges**: Player chooses each (row/row, col/col, or row/col)

This mirrors the knight in chess — indirect, requires reading the board, masters leverage it.

---

## Strategic Value

### Why Swap Matters

In Intelligence Qube style gameplay:
- Wave advances toward player
- Player is ON the grid
- ∞ cubes cannot be captured, will pass through
- If ∞ forms a wall with no gaps, player gets **crushed**

**Recursion creates survival paths by breaking ∞ walls.**

### Constraint: Must Hit a Capturable Cube

Swap markers can only be placed where a Unit cube exists. This means:
- Player must sacrifice Unit cubes to place swaps
- Row with only ∞ cannot be directly swapped
- Must swap from below to pull ∞ downward

---

## Puzzle Design Implications

### Before Recursion
- Matrix clears areas efficiently
- Player can "auto-pilot" through waves
- ∞ walls = death, no counterplay

### With Recursion
- Player must **read the wave first**
- Identify ∞ walls that block survival
- Calculate which Units to sacrifice for swaps
- Execute swaps to create gaps
- THEN use Matrix for efficient capture

**Recursion breaks auto-pilot.**

---

## Example: Unsolvable → Solvable

### Starting Position (Death Wall)

```
Row 3: ∞ ∞ ∞ ∞ ∞   ← full wall, no gap
Row 2: U ∞ U ∞ U
Row 1: U U U U U
       1 2 3 4 5
         ↑
      Player
```

Without Recursion: Player gets crushed when row 3 reaches them.

### Solution Sequence

1. Capture (1,1) with Unit marker
2. Place Recursion marker at (2,1), hits Unit
3. Choose column swap: (2,2)∞ swaps with (2,0)empty — ∞ moves off grid? Or choose row swap
4. Repeat to open gap in row 3
5. Player survives in opened column

### After Swaps

```
Row 3: ·  ∞  ∞  ∞  U
Row 2: ·  U  ·  U  U
Row 1: ∞  U  ∞  U  U
       1  2  3  4  5
           ↑
        Player survives in col 2
```

---

## Balance with Matrix

| Situation | Matrix | Recursion |
|-----------|--------|-----------|
| Dense cluster, no ∞ | Clears all efficiently | Does nothing |
| Scattered ∞, no walls | Works fine | Unnecessary |
| ∞ wall blocking value | Can't help | Opens access |
| ∞ wall blocking survival | Can't help | Creates escape path |

**They don't compete. They address different problems.**

---

## Wave Generation Approach

### Sudoku-Style Degradation

1. **Start with solved wave** (all Units)
2. **Replace Units** with ∞, M, R one at a time
3. **After each replacement**, verify solvable with resources
4. **Stop at target difficulty**

### Replacement Effects

| Replace U with | Effect on Puzzle |
|----------------|------------------|
| ∞ | Blocks column, threatens survival |
| M | Adds value target, rewards Matrix marker use |
| R | Adds swap amplifier (R+R = 2 charges) |

### Difficulty Gradient

| ∞ Count | Wall Formation | Requires |
|---------|----------------|----------|
| 0-2 | Scattered | Unit only |
| 3-5 | Partial clusters | Matrix helpful |
| 6-8 | Partial wall, natural gap | Matrix required |
| 9-12 | Full wall, 1-2 gaps | Recursion for value |
| 13+ | Layered walls | Recursion for survival |

---

## Resource Matching

Same wave, different puzzles based on player resources:

| Resources | Experience |
|-----------|------------|
| 0 Matrix, 0 Recursion | Survival only, lose blocked value |
| 1 Matrix, 0 Recursion | Survival only, one area clear |
| 0 Matrix, 2 Recursion | Open columns, rescue value with Unit |
| 2 Matrix, 3 Recursion | Full solve potential, sequencing puzzle |

---

## Configuration Examples

### Free Amplification (No special markers needed)

**Player Resources**: 0 Matrix, 0 Recursion

```
Row 5: U U U U U
Row 4: U M U M U
Row 3: U U U U U
Row 2: U R U R U
Row 1: U U U U U
       1 2 3 4 5
```

- M cubes accessible → Unit marker gets 2x2 free
- R cubes accessible → Unit marker gets row/col swap free
- No walls. Player uses wave cubes as amplifiers.

### Swap Dependency

**Player Resources**: 0 Matrix, 2 Recursion

```
Row 5: U U M U U
Row 4: ∞ ∞ ∞ ∞ ∞
Row 3: U R U R U
Row 2: U U U U U
Row 1: U U U U U
       1 2 3 4 5
```

- Full ∞ wall at row 4 — death without intervention
- Two R cubes at row 3
- Recursion marker + R = 2 swap charges
- Use swaps to move ∞ from row 4, create gap
- M at row 5 is the reward for solving

### Maximum Tension (8x8)

**Player Resources**: 1 Matrix, 2 Recursion

```
Row 8: M R M R M R M R
Row 7: ∞ ∞ ∞ U ∞ ∞ ∞ ∞
Row 6: U U U U U U U U
Row 5: ∞ U U U U U U ∞
Row 4: U U R U U R U U
Row 3: U ∞ U U U U ∞ U
Row 2: U U U U U U U U
Row 1: U U U U U U U U
       1 2 3 4 5 6 7 8
```

- Row 7: near-full wall, single gap at column 4
- Row 8: alternating M and R — high value, high amplification
- Row 4: two R cubes accessible for swap charges
- Limited resources force choices

---

## Updated Collision Table

| Player | Wave | Charges | Result |
|--------|------|---------|--------|
| Unit | U | — | Single capture |
| Unit | M | — | 2x2 marker |
| Matrix | U | — | 2x2 marker |
| Matrix | M | — | 3x3 marker |
| Unit | R | 1 | Row OR column swap (player chooses) |
| Recursion | U | 1 | Row OR column swap (player chooses) |
| Recursion | R | 2 | Each chosen independently |

---

## Open Questions

1. **Swap boundaries**: What happens at grid edges?
2. **Empty cells**: Can ∞ swap with empty space?
3. **Visual feedback**: How to preview swap result before committing?
4. **Timing**: When does swap execute — immediately or on next wave move?

---

## Next Steps

1. Prototype + swap in Unity
2. Test swap boundaries and edge cases
3. Create 5-10 test waves using swap-dependent puzzles
4. Playtest for feel and balance
5. Iterate based on feedback

---

**Last Updated:** January 27, 2026  
**Related Documents:**
- [Gameplay Mechanics](../3_GameplayMechanics.md)
- [Puzzle Heuristics](../5_PuzzleHeuristics.md)
- [Brainstorming](../Brainstorming.md)
