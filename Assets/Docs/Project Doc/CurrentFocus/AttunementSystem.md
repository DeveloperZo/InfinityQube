# Attunement System Design

> **Document Purpose:** Focused attunement design for InfinityQube's RPG progression layer
> **Last Updated:** January 31, 2026
> **Design Philosophy:** Fundamental identity shifts, not stat tweaks. One attunement active per marker type.

---

## Core Principles

1. **One active attunement per marker type** - No stacking
2. **Toggle between paths** - Players choose their preferred playstyle
3. **Fundamental changes** - Each attunement represents a different way to experience that marker
4. **Clear tradeoffs** - Choosing one means giving up the others
5. **Collision rules sacred** - Never change what captures what

---

## Design Pattern

Each marker type follows the same identity structure:

| Identity | Description | Example |
|----------|-------------|---------|
| **Power** | More impact per use | Bigger area, stronger effect |
| **Resource** | More opportunities | +markers per stage |
| **Utility** | New strategic option | Interact with ∞ differently |

---

## Matrix Attunements

**Core mechanic:** Place marker → collision → creates area marker → trigger → captures 2x2 area

**Player desire:** "Efficiently capture multiple cubes with one limited marker"

### Attunement Options

| Attunement | Core Change | Identity |
|------------|-------------|----------|
| **Matrix Mastery** | All Matrix areas are 3x3 instead of 2x2 | "My captures are powerful" |
| **Matrix Abundance** | +2 Matrix markers per stage | "I use Matrix often" |
| **Infinity Forge** | Matrix + ∞ collision creates area marker | "∞ cubes are opportunities" |

### Identity Breakdown

**Matrix Mastery** (Power)
- Simple: Every Matrix capture covers more ground
- Playstyle: Confident, go-big player
- Tradeoff: Higher ∞ collision risk, may waste on sparse areas
- Player feels: "Every placement is a statement"

**Matrix Abundance** (Resource)
- Simple: More Matrix markers available
- Playstyle: Frequent Matrix user, experimental
- Tradeoff: Each placement less precious, encourages sloppier play
- Player feels: "Matrix is my primary tool"

**Infinity Forge** (Utility)
- Simple: ∞ cubes become valid collision targets for Matrix
- Normal behavior: Matrix + ∞ = Matrix destroyed, wasted
- With attunement: Matrix + ∞ = area marker created, ∞ remains
- Playstyle: Board reader, sees opportunities others don't
- Tradeoff: Encourages riskier positioning near ∞
- Player feels: "The whole board is my canvas"

---

## Recursion Attunements

**Core mechanic:** + pattern swap - repositions cubes to create survival paths

**Base R+R collision:** swap one axis + capture another axis (player chooses independently)

**Player desire:** "Rearrange the board to survive ∞ walls and access blocked value"

### Attunement Options

| Attunement | Core Change | Identity |
|------------|-------------|----------|
| **Recursion Clone** | R+R becomes clone + swap (replaces capture) | "I invest for bigger payoffs" |
| **Recursion Abundance** | +2 Recursion markers per stage | "I reposition often" |
| **Infinity Gateway** | Recursion + ∞ collision creates swap marker | "∞ walls become opportunities" |

### Identity Breakdown

**Recursion Clone** (Power)
- Normal R+R: swap axis + capture axis
- With attunement: swap axis + clone axis (clone replaces capture)
- Clone duplicates cube to opposite position on chosen axis
- ∞ restriction: Cannot clone ∞, cannot replace ∞

```
Normal R+R (swap + capture):       Clone R+R (swap + clone):
      U                                  U
    M ● R   →  M↔R swapped             M ● R   →  M cloned to E position
      U        U,U captured (col)        U        N↔S swapped
                                               Result: M ● M with swap
```

**Strategic value:**
- Creates MORE valuable cubes on the board
- Turn one Matrix cube into two Matrix captures
- Delayed gratification: cloned cubes still need capturing
- Sets up bigger combos for next markers

**Tradeoff:**
- Lose immediate capture value from R+R
- Must invest additional markers to capture cloned cubes
- Only useful when cloning valuable cube types (M, R)

**Player feels:** "I see one Matrix cube, I turn it into two"

---

**Recursion Abundance** (Resource)
- Simple: +2 Recursion markers per stage
- Playstyle: Frequent repositioner, experimental
- Tradeoff: Less precious per swap, may over-rely on repositioning
- Player feels: "I always have an escape option"

---

**Infinity Gateway** (Utility)
- Normal behavior: Recursion must hit capturable cube (U, M, R) to create swap
- With attunement: Recursion + ∞ = swap marker created at ∞ position
- The ∞ cube participates in the swap!
- Playstyle: Aggressive wall-breaker
- Tradeoff: Must get close to ∞ walls, risky positioning
- Player feels: "No wall can stop me"

**Why Infinity Gateway matters:**
- Normally, a row of pure ∞ cannot be directly swapped (no valid collision target)
- With this attunement, you CAN create swaps in ∞-heavy rows
- Opens entirely new puzzle solutions

---

## Infinity Attunements (Future)

**Core mechanic:** Phases through ∞ cubes, face painting

*Pending design - will follow same Power/Resource/Utility pattern*

---

## Unit Attunements

**Design decision:** Unit markers have no attunements.

**Reasoning:**
- Unit is abundant, always available
- Attunements reward using limited resources (Matrix, Recursion, Infinity)
- Unit is the baseline that other markers build upon

---

## UI: Attunement Panel

### Player Experience

1. Open Attunement Panel from Hub
2. See all unlocked attunements organized by marker type
3. Toggle ONE attunement per marker (or none)
4. Changes take effect immediately for next stage

### Panel Layout (Simplified)

```
┌─────────────────────────────────────────────────┐
│  ATTUNEMENTS                        [ESC] Close │
├─────────────────────────────────────────────────┤
│                                                 │
│  MATRIX                                         │
│  ○ Matrix Mastery    - All areas 3x3           │
│  ○ Matrix Abundance  - +2 markers              │
│  ● Infinity Forge    - ∞ collision works  ← ON │
│                                                 │
│  RECURSION                                      │
│  ● Recursion Clone   - Clone+swap (R+R)   ← ON │
│  ○ Recursion Abundance - +2 markers            │
│  ○ Infinity Gateway  - ∞ collision works       │
│                                                 │
│  INFINITY                                       │
│  (TBD - pending playtesting)                   │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## Unlock Progression

Attunements unlock via stage completion or shard purchase:

| Attunement | Unlock Condition |
|------------|------------------|
| Matrix Mastery | Complete Stage 5 |
| Matrix Abundance | 150 Axiom Shards |
| Infinity Forge | Complete Stage 8 |
| Recursion Clone | Complete Stage 10 |
| Recursion Abundance | 150 Axiom Shards |
| Infinity Gateway | Complete Stage 12 |

*Numbers subject to tuning based on playtest*

---

## Implementation Notes

### Data Structure

```csharp
public enum AttunementEffect
{
    None,
    
    // Matrix
    MatrixMastery,      // 3x3 instead of 2x2
    MatrixAbundance,    // +2 markers per stage
    InfinityForge,      // Matrix + ∞ = area marker
    
    // Recursion
    RecursionClone,     // Clone instead of swap
    RecursionAbundance, // +2 markers per stage
    InfinityGateway,    // Recursion + ∞ = swap marker
}
```

### Query API

```csharp
// Matrix queries
int GetMatrixAreaSize();        // Returns 2 or 3
int GetBonusMatrixMarkers();    // Returns 0 or 2
bool CanMatrixCollideWithInfinity(); // Infinity Forge

// Recursion queries
bool IsRecursionCloneMode();    // Clone instead of swap
int GetBonusRecursionMarkers(); // Returns 0 or 2
bool CanRecursionCollideWithInfinity(); // Infinity Gateway
```

---

## Design Summary

| Marker | Power | Resource | Utility |
|--------|-------|----------|---------|
| Matrix | 3x3 areas | +2 markers | ∞ collision valid |
| Recursion | Clone (R+R clone+swap instead of capture+swap) | +2 markers | ∞ collision valid |
| Infinity | TBD | TBD | TBD |

Each attunement answers: **"How do you want to experience this marker?"**

---

*Status: Matrix & Recursion Designed, Infinity Pending*
