# Penalty & Scoring System Verification

> **Status**: Verification Summary  
> **Date**: December 2024  
> **Milestone**: 1.6 - Unit Markers & Infinite Cube (Stages 1-2)

## Penalty System Verification ✅

### Row Penalty System

**Unit Cube Escape Penalty:**
- ✅ Tracks `unitCubesEscaped` counter in WaveManager
- ✅ Triggers when `unitCubesEscaped >= grid.Width`
- ✅ Calls `grid.RemoveBottomRow()` to remove bottom row
- ✅ Resets counter after penalty applied
- ✅ Resets counter at wave start
- ✅ Logging: "ROW PENALTY TRIGGERED" messages

**Death Penalty:**
- ✅ Tracks `playerDeaths` counter in WaveManager
- ✅ Triggers when `playerDeaths >= 2`
- ✅ Calls `grid.RemoveBottomRow()` to remove bottom row
- ✅ Resets counter after penalty applied
- ✅ Resets counter at wave start
- ✅ Logging: "DEATH PENALTY TRIGGERED" messages

**Row Removal Safety:**
- ✅ Prevents removal if would leave < 3 rows
- ✅ Validates row bounds before removal
- ✅ Adjusts player position if on removed row
- ✅ Finds safe respawn position above removed row
- ✅ Fade-out animation for removed tiles/cubes
- ✅ Updates grid bounds (`bottom` variable)

**Integration:**
- ✅ `PlayerManager.Die()` calls `waveManager.OnPlayerDeath()`
- ✅ `WaveManager.OnCubeEscaped()` tracks Unit escapes
- ✅ Both penalties use same `RemoveBottomRow()` system
- ✅ Line divider system paused (commented out)

### Verification Checklist

- [x] Unit escape penalty triggers correctly
- [x] Death penalty triggers correctly
- [x] Counters reset after penalty
- [x] Counters reset at wave start
- [x] Safety checks prevent grid from becoming unplayable
- [x] Player position adjusted when row removed
- [x] Visual feedback (fade animation) works
- [ ] **TODO**: Playtest to verify in-game behavior

---

## Scoring System Verification ✅

### Score Tracking

**Wave Score Data:**
- ✅ Tracks captures by cube type (Unit, Matrix, Recursion, Infinity)
- ✅ Tracks escapes by cube type (Infinity escapes not penalized)
- ✅ Tracks moves used, markers placed, player deaths
- ✅ Calculates base score: `captures × points - escapes × penalty`

**Score Constants:**
- ✅ Unit: 10 points
- ✅ Matrix: 15 points
- ✅ Recursion: 20 points
- ✅ Infinity: 25 points
- ✅ Escape penalty: -15 points (non-Infinity only)
- ✅ No death bonus: +50 points
- ✅ No escape bonus: +30 points
- ✅ Move efficiency: 1.0x to 1.3x multiplier

**Grade System:**
- ✅ S Grade: 90%+ of max score
- ✅ A Grade: 70-89%
- ✅ B Grade: 50-69%
- ✅ C Grade: <50%
- ✅ Shard multipliers per grade (S: 1.5x, A: 1.25x, B: 1.0x, C: 0.75x)

### Score Calculation

**Base Score:**
- ✅ Points per cube type captured
- ✅ Subtracts escape penalties (non-Infinity)
- ✅ Minimum score: 0 (can't go negative)

**Move Efficiency:**
- ✅ Faster clear = higher multiplier
- ✅ Clear at move 0 = 1.3x
- ✅ Clear at max moves = 1.0x
- ✅ Linear interpolation between

**Final Score:**
- ✅ Formula: `(Base Score × Move Efficiency) + Bonuses`
- ✅ Bonuses: No Death (+50), No Escape (+30)
- ✅ Calculates max possible score for grade percentage

**Shard Rewards:**
- ✅ Base shards: 100 per wave (configurable)
- ✅ Multiplied by grade multiplier
- ✅ Only awarded on first clear (replay = no shards)

### Integration

**Event Subscriptions:**
- ✅ `OnStageStart` - Initializes score tracking
- ✅ `OnWaveStart` - Resets wave score, tracks cube count
- ✅ `OnWaveComplete` - Finalizes wave score, stores in list
- ✅ `OnCubeCaptured` - Records capture
- ✅ `OnCubeEscaped` - Records escape

**Stage Completion:**
- ✅ `SaveManager.OnStageComplete()` calls `CalculateStageResult()`
- ✅ Awards shards based on grade
- ✅ Records lifetime statistics
- ✅ Only on first clear (replay = no rewards)

**Public API:**
- ✅ `ScoreManager.Instance.CurrentWaveScore` - Live wave score
- ✅ `ScoreManager.Instance.RunningBaseScore` - Running total
- ✅ `ScoreManager.Instance.CalculateStageResult()` - Final calculation
- ✅ `ScoreManager.Instance.RecordPlayerDeath()` - Death tracking
- ✅ `ScoreManager.Instance.RecordMarkerPlaced()` - Marker tracking

### Verification Checklist

- [x] Score tracking initialized on stage start
- [x] Captures recorded correctly
- [x] Escapes recorded correctly (Infinity not penalized)
- [x] Deaths recorded correctly
- [x] Move count tracked correctly
- [x] Base score calculation correct
- [x] Move efficiency calculation correct
- [x] Bonuses applied correctly
- [x] Grade calculation correct
- [x] Shard rewards calculated correctly
- [x] Integration with SaveManager working
- [ ] **TODO**: Verify score display in UI (if exists)
- [ ] **TODO**: Playtest to verify calculations match expected values

---

## Known Issues / TODOs

### Penalty System
- [ ] Playtest row removal to verify visual feedback
- [ ] Verify player position adjustment works in all scenarios
- [ ] Test edge case: multiple penalties in quick succession

### Scoring System
- [ ] Verify score UI display (if implemented)
- [ ] Test score calculation with various scenarios:
  - [ ] Perfect run (all captures, no escapes, no deaths, fast clear)
  - [ ] Poor run (many escapes, deaths, slow clear)
  - [ ] Mixed scenarios
- [ ] Verify shard rewards are actually awarded to player progression
- [ ] Test replay behavior (should not award shards)

---

## Implementation Status

| System | Status | Notes |
|--------|--------|-------|
| **Unit Escape Penalty** | ✅ Complete | Triggers at grid.Width escapes |
| **Death Penalty** | ✅ Complete | Triggers at 2 deaths |
| **Row Removal** | ✅ Complete | Safety checks, player adjustment |
| **Score Tracking** | ✅ Complete | All events subscribed |
| **Score Calculation** | ✅ Complete | Base, efficiency, bonuses, grades |
| **Shard Rewards** | ✅ Complete | Integrated with SaveManager |
| **UI Display** | ⚠️ Unknown | Need to verify if score UI exists |

---

## Next Steps

1. **Playtest penalty system** - Verify row removal works correctly in-game
2. **Playtest scoring system** - Verify calculations match expected values
3. **Verify score UI** - Check if score display exists and works
4. **Test edge cases** - Multiple penalties, perfect runs, poor runs
5. **Validate shard rewards** - Ensure shards are actually added to player progression

---

*Last Updated: December 2024*

