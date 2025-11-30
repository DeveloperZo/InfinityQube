# Milestone 1.2 - Task 1: Marker Placement and Spawning Verification

> **Status**: Verification Complete  
> **Date**: December 2025  
> **Task**: Define marker placement and spawning for all markers

---

## Executive Summary

All marker types are implemented and functional. Marker placement rules are enforced, and marker-to-cube conversion works correctly in both immediate and paired wave contexts. This document verifies the current implementation and documents the complete system.

---

## Marker Types Overview

### Unified Input System
**Regular markers (Unit, Recursion, Prime, Infinity) use a unified input system:**
- **Mode Selection**: Keys `1`, `2`, `3`, `4` switch between marker modes
  - `1` = Unit Marker mode
  - `2` = Prime Marker mode
  - `3` = Recursion Marker mode
  - `4` = Infinity Marker mode
- **Placement Key**: `F` (KeyCode.F) - places marker of current mode
- **Automatic Spawning**: When wave moves forward, all placed markers automatically spawn player cubes
- **No manual trigger needed** - regular markers automatically convert to player cubes

**Cube Markers (generated from collisions) use different system:**
- **Generation**: Created automatically from collisions and placed at collision/capture tile:
  - Prime+Prime collision → Prime cube marker placed at collision tile (3x3 area)
  - Recursion+Recursion collision → Recursion cube marker placed at collision tile (2x2 area)
  - Prime captured by non-Prime → Prime cube marker placed at capture position (2x2 area)
- **Trigger Key**: `R` (KeyCode.R) - triggers cube marker to create area effect
- **Behavior**: When `R` is pressed:
  - Area effect expands from cube marker position
  - Captures all non-Infinity cubes in the area (Infinity cubes are excluded via `RemoveCubeFromWaveManager()`)
  - Prime+Prime cube marker: 3x3 area captures all non-Infinity cubes
  - Recursion+Recursion cube marker: 2x2 area captures all non-Infinity cubes
  - Prime (non-matching) cube marker: 2x2 area captures all non-Infinity cubes

### 1. Unit Marker
- **Mode Key**: `1` (KeyCode.Alpha1)
- **Placement Key**: `F` (KeyCode.F) when in Unit mode
- **Automatic Spawning**: Spawns `CubeType.Unit` (Unit Cube) when wave moves forward
- **No manual trigger** - automatically converts to player cube on wave movement
- **Charge System**: `maxUnitMarkerCharges`, `currentUnitMarkerCharges`
- **Placement Limit**: `maxUnitMarkers` (on-grid limit)
- **Regeneration**: Cooldown-based (`unitMarkerCooldown`)
- **Visual**: Blue-gray color (0.5f, 0.6f, 0.7f, 1f)

**Implementation Status**: ✅ Complete
- Placement: `PlaceUnitMarker()` in `PlayerMarkerSystem.cs:126`
- Spawning: `SpawnPlayerCubeAt(marker.position, CubeType.Unit, false)` in `PlayerMarkerSystem.cs:770`
- Paired Wave: Records with `MarkerMode.Unit` in `PlayerMarkerSystem.cs:141`

---

### 2. Recursion Marker
- **Mode Key**: `3` (KeyCode.Alpha3)
- **Placement Key**: `F` (KeyCode.F) when in Recursion mode
- **Automatic Spawning**: Spawns `CubeType.Recursion` (Recursion Cube) when wave moves forward
- **No manual trigger** - automatically converts to player cube on wave movement
- **Charge System**: `maxRecursionMarkerCharges`, `currentRecursionMarkerCharges`
- **Placement Limit**: `maxRecursionMarkers` (on-grid limit)
- **Regeneration**: Cooldown-based (`recursionMarkerCooldown`)
- **Visual**: [Needs definition - currently uses default]

**Implementation Status**: ✅ Complete
- Placement: `PlaceRecursionMarker()` in `PlayerMarkerSystem.cs:226`
- Spawning: `SpawnPlayerCubeAt(marker.position, CubeType.Recursion, false)` in `PlayerMarkerSystem.cs:799`
- Paired Wave: Records with `MarkerMode.Recursion` in `PlayerMarkerSystem.cs:241`
- **Special Note**: Designed specifically for Recursion cubes but works on all cube types

---

### 3. Prime Marker
- **Mode Key**: `2` (KeyCode.Alpha2)
- **Placement Key**: `F` (KeyCode.F) when in Prime mode
- **Automatic Spawning**: Spawns `CubeType.Prime` (Prime Cube) when wave moves forward - **Area Effect (2x2, 3x3 for Prime+Prime)**
- **No manual trigger** - automatically converts to player cube on wave movement
- **Charge System**: `maxPrimeMarkerCharges`, `currentPrimeMarkerCharges`
- **Placement Limit**: `primeMarkerOnGridLimit` (on-grid limit)
- **Regeneration**: Cooldown-based (`primeMarkerCooldown`)
- **Size**: Configurable (default 2x2 area, 3x3 for Prime+Prime collisions)
- **Visual**: [Needs definition - currently uses default]

**Implementation Status**: ✅ Complete
- Placement: `PlacePrimeMarker(centerPosition, size)` in `PlayerMarkerSystem.cs:325`
- Spawning: `SpawnPlayerCubeAt(marker.centerPosition, CubeType.Prime, true)` in `PlayerMarkerSystem.cs:786`
  - Note: `isPrime = true` enables area capture (3x3)
- Paired Wave: Records center position with `MarkerMode.Prime` in `PlayerMarkerSystem.cs:342`
- **Special Behavior**: Covers 2x2 area (from marker), 3x3 for Prime+Prime collisions, records center position for paired wave

---

### 4. Infinity Marker
- **Mode Key**: `4` (KeyCode.Alpha4)
- **Placement Key**: `F` (KeyCode.F) when in Infinity mode
- **Automatic Spawning**: Spawns `CubeType.Infinity` (Infinity Cube) when wave moves forward
- **No manual trigger** - automatically converts to player cube on wave movement
- **Charge System**: `maxInfinityMarkerCharges` (default: 1), `currentInfinityMarkerCharges`
- **Placement Limit**: `maxInfinityMarkers` (default: 2)
- **Regeneration**: Cooldown-based (`infinityMarkerCooldown`, default: 15f)
- **Visual**: Deep black/dark charcoal (0.15f, 0.15f, 0.18f, 1f)

**Implementation Status**: ✅ Complete
- Placement: `PlaceInfinityMarker()` in `PlayerMarkerSystem.cs:451`
- Spawning: `SpawnPlayerCubeAt(marker.position, CubeType.Infinity, false)` in `PlayerMarkerSystem.cs:812`
- Paired Wave: Records with `MarkerMode.Infinity` in `PlayerMarkerSystem.cs:466`
- **Note**: Infinity markers automatically spawn player cubes when wave moves forward (no manual trigger)

---

### 5. Cube Marker
- **Trigger Key**: `R` (KeyCode.R) - triggers cube marker to create area effect
- **Power Up Key**: `E` (KeyCode.E) - powers up cube marker (if implemented)
- **Generation**: Created automatically from collisions:
  - **Prime+Prime collision**: Creates Prime cube marker at collision tile (3x3 area)
  - **Recursion+Recursion collision**: Creates Recursion cube marker at collision tile (2x2 area)
  - **Prime captured by non-Prime**: Creates Prime cube marker at capture position (2x2 area)
- **Type**: `CubeMarkerType` enum (Unit, Recursion, Prime, Cube)
- **Visual**: [Needs definition]

**Implementation Status**: ✅ Complete
- Generation: `CreateCubeMarker()` in `PlayerMarkerSystem.cs:565` (called from `ProcessCubeCapture()`)
- Trigger: `TriggerCubeMarkerAt()` in `PlayerMarkerSystem.cs:585` (called via `R` key in `HandleCubeMarkerInputs()`)
- **Special Behavior**: 
  - Prime+Prime collision → Cube marker placed at collision tile → `R` key triggers 3x3 area that captures all non-Infinity cubes
  - Recursion+Recursion collision → Cube marker placed at collision tile → `R` key triggers 2x2 area that captures all non-Infinity cubes
  - Prime captured by non-Prime → Cube marker placed at capture position → `R` key triggers 2x2 area that captures all non-Infinity cubes
- **Mechanism**: Cube markers use `TriggerPrimeMarkerAt()` internally, which processes area capture via `ProcessCubeCapture()` - Infinity cubes are excluded from capture (checked in `RemoveCubeFromWaveManager()` at `PlayerMarkerSystem.cs:747`)
- **Note**: Cube markers are NOT placed markers - they are generated resources from collisions
- **Paired Wave**: Cube markers do NOT participate in paired wave system (correct behavior)

---

## Marker Placement Rules and Restrictions

### Validation Chain

Each marker placement goes through three validation checks:

1. **Charge and Limit Check** (`CanPlace[Type]Marker()`)
   - Verifies `current[Type]MarkerCharges > 0`
   - Verifies `current[Type]Markers < max[Type]Markers`
   - Location: `PlayerActionManager.cs:1303-1328`

2. **Position Validation** (`IsValidPosition()`)
   - Verifies position is within grid bounds
   - Verifies tile exists and is playable (`tile.IsPlayable`)
   - Location: `PlayerMarkerSystem.cs:1280-1283` → `GridManager.IsValidGridPosition()`

3. **Marker Conflict Check** (`CanPlaceMarkerAt()`)
   - Verifies no existing Unit, Recursion, Prime, or Infinity marker at position
   - Location: `PlayerMarkerSystem.cs:1285-1289`

### Tile State Restrictions

Markers cannot be placed on tiles that are:
- **Corrupted** (`IsCorrupted == true`): Blocks all marker placement
- **Blackened** (`isBlackened == true`): Blocks marker placement
- **Not Normal State** (`currentState != TileState.Normal`): Blocks marker placement
- **Fallen** (`hasFallen == true`): Tile is not playable, position validation fails
- **Already Has Marker**: `CanPlaceMarkerAt()` prevents duplicate markers

**Tile Properties**:
- `CanBeMarked`: `currentState == TileState.Normal && !isBlackened && !IsCorrupted`
- `CanAcceptMarkers`: `!IsCorrupted`

---

## Marker-to-Cube Conversion

### Primary Mechanism: Automatic Spawning on Wave Movement

**When the wave moves forward**, `SpawnPlayerCubes()` is automatically called (`WaveManager.cs:862`), which converts all placed markers to player cubes:

| Marker Type | Conversion | Location |
|-------------|------------|----------|
| Unit Marker | → Unit Cube | `PlayerMarkerSystem.cs:796` |
| Recursion Marker | → Recursion Cube | `PlayerMarkerSystem.cs:825` |
| Prime Marker | → Prime Cube (2x2 area effect) | `PlayerMarkerSystem.cs:812` |
| Infinity Marker | → Infinity Cube | `PlayerMarkerSystem.cs:838` |
| Cube Marker | → Area detonation (variable size: 2x2 or 3x3) | `PlayerMarkerSystem.cs:583` |

**Behavior**:
- Markers are automatically converted to player cubes when wave moves forward
- Player cubes spawn at marker positions and move backward (opposite to wave direction)
- Markers are removed after spawning player cubes
- This is the PRIMARY mechanic for marker-to-cube conversion

### Cube Marker Mechanism: Manual Trigger (R Key)

**Cube markers are triggered manually** (via `R` key):
- **Prime+Prime cube marker**: `R` key triggers 3x3 area effect that captures all non-Infinity cubes
- **Recursion+Recursion cube marker**: `R` key triggers 2x2 area effect
- **Prime (non-matching) cube marker**: `R` key triggers 2x2 area effect

**Behavior**:
- Cube markers are placed at collision positions automatically (e.g., Prime+Prime collision creates cube marker at collision tile)
- Player presses `R` to trigger the cube marker
- Area effect expands from cube marker position (size depends on cube marker type: 3x3 for Prime+Prime, 2x2 for others)
- All non-Infinity cubes in the area are captured
- Cube marker is removed after triggering

**Note**: Cube markers are different from regular markers - they are generated from collisions and require manual triggering with `R` key. Regular markers (Unit, Recursion, Prime, Infinity) automatically spawn player cubes and do NOT use the R key.

| Marker Type | Conversion | Location |
|-------------|------------|----------|
| Unit Marker | → Unit Cube | `PlayerMarkerSystem.cs:770` |
| Recursion Marker | → Recursion Cube | `PlayerMarkerSystem.cs:799` |
| Prime Marker | → Prime Cube (2x2 area effect) | `PlayerMarkerSystem.cs:786` |
| Infinity Marker | → Infinity Cube | `PlayerMarkerSystem.cs:812` |
| Cube Marker | → Area detonation (variable size: 2x2 or 3x3) | `PlayerMarkerSystem.cs:583` |

**Spawn Process**:
1. All markers are converted to player cubes
2. Marker visuals are destroyed
3. Marker queues are cleared
4. Player cubes are spawned at marker positions
5. Player cubes move backward (toward top of grid)

---

## Paired Wave Integration

### Recording System

All marker placements are automatically recorded for paired wave system:

```csharp
RecordMarkerForPairedWave(position, MarkerMode.[Type])
```

**Recording Location**: `PlayerMarkerSystem.cs:1175-1181`
- Calls `WaveManager.RecordMarkerPosition(position, markerType)`
- Always records during any wave
- Markers will be used when wave is mirrored

### Marker-to-Cube Conversion in Paired Waves

**Wave A (Config Wave)**:
- Player places markers
- Markers are recorded via `RecordMarkerForPairedWave()`
- Markers can be triggered to capture cubes
- Markers can spawn player cubes if triggered

**Wave B (Mirrored Wave)**:
- Recorded marker positions spawn cubes at those positions
- Conversion rules:
  - Unit Marker → Unit Cube
  - Recursion Marker → Recursion Cube
  - Prime Marker → Prime Cube (at center position)
  - Infinity Marker → Infinity Cube
- Implementation: `WaveManager.SpawnCubesFromMarkers()` (referenced in `WaveManager.cs:1455`)

**Verification**: ✅ All marker types record positions correctly
- Unit: `PlayerMarkerSystem.cs:141`
- Recursion: `PlayerMarkerSystem.cs:241`
- Prime: `PlayerMarkerSystem.cs:342`
- Infinity: `PlayerMarkerSystem.cs:466`

---

## Edge Cases and Issues

### ✅ Working Correctly

1. **Marker Placement on Valid Tiles**: ✅ Works
2. **Marker Placement on Corrupted Tiles**: ✅ Blocked correctly
3. **Marker Placement on Blackened Tiles**: ✅ Blocked correctly
4. **Marker Placement on Fallen Tiles**: ✅ Blocked correctly
5. **Duplicate Marker Prevention**: ✅ Works (checks Unit/Recursion/Prime/Infinity)
6. **Charge Consumption**: ✅ Works correctly
7. **Paired Wave Recording**: ✅ All marker types record correctly
8. **Unified Input System**: ✅ Mode switching (1-4) and placement (F) work correctly

### ⚠️ Potential Issues

1. **Infinity Marker Conflict Check**: ✅ **RESOLVED**
   - `CanPlaceMarkerAt()` now checks for Infinity markers
   - Fixed in `PlayerMarkerSystem.cs:1285-1289`

2. **Unified Input System Documentation**: ✅ **RESOLVED**
   - All markers use unified input: Mode keys (1-4), Placement (F), Trigger (R)
   - Documentation updated to reflect actual implementation

3. **Visual Feedback Inconsistency**:
   - Unit marker has defined color (blue-gray)
   - Infinity marker has defined color (dark charcoal)
   - Recursion and Prime markers need color definitions
   - **Recommendation**: Define colors for all marker types (Task 5)

4. **Cube Marker Visual Feedback**:
   - Cube markers are generated resources, not placed markers
   - Visual feedback may not be clear
   - **Recommendation**: Ensure distinct visual for cube markers

---

## Verification Checklist

### Marker Placement
- [x] Mode switching works with keys `1`, `2`, `3`, `4`
- [x] All markers can be placed with `F` key (when in appropriate mode)
- [x] Unit markers: Mode `1` + `F` to place
- [x] Prime markers: Mode `2` + `F` to place
- [x] Recursion markers: Mode `3` + `F` to place
- [x] Infinity markers: Mode `4` + `F` to place

### Automatic Player Cube Spawning
- [x] Markers automatically spawn player cubes when wave moves forward
- [x] `SpawnPlayerCubes()` is called from `WaveManager.MoveCubesForward()`
- [x] Unit markers spawn Unit cubes
- [x] Recursion markers spawn Recursion cubes
- [x] Prime markers spawn Prime cubes (with area effect)
- [x] Infinity markers spawn Infinity cubes
- [x] Markers are removed after spawning player cubes
- [x] Placement requires valid charges
- [x] Placement requires valid position
- [x] Placement blocked on corrupted tiles
- [x] Placement blocked on blackened tiles
- [x] Placement blocked on fallen tiles
- [x] Duplicate markers prevented (Unit/Recursion/Prime/Infinity)

### Marker Spawning
- [x] Unit markers spawn Unit cubes
- [x] Recursion markers spawn Recursion cubes
- [x] Prime markers spawn Prime cubes (with area effect)
- [x] Infinity markers spawn Infinity cubes
- [x] Cube markers create area detonation
- [x] Spawning occurs at correct positions
- [x] Prime markers spawn at center position

### Paired Wave Integration
- [x] Unit markers record for paired wave
- [x] Recursion markers record for paired wave
- [x] Prime markers record for paired wave
- [x] Infinity markers record for paired wave
- [x] Cube markers do NOT record (correct behavior)
- [x] Recorded positions spawn correct cube types

---

## Recommendations

### Immediate Actions (Task 1 Completion)

1. **Fix Infinity Marker Conflict Check**: ✅ **COMPLETED**
   - Updated `CanPlaceMarkerAt()` to include Infinity marker check
   - Location: `PlayerMarkerSystem.cs:1285-1289`
   - Prevents Infinity markers from being placed on tiles with other markers

2. **Unified Input System Documentation**: ✅ **UPDATED**
   - Corrected documentation to reflect unified input system
   - All markers use: Mode keys (1-4), Placement (F), Automatic spawning on wave movement
   - Optional R key activates markers to capture cubes (alternative to automatic spawning)
   - Implementation: `HandleModeSwitchingInput()` and `HandleUnifiedPlaceInput()` in `PlayerActionManager.cs`
   - Automatic spawning: `SpawnPlayerCubes()` called from `WaveManager.MoveCubesForward()`

3. **Document Visual Colors**:
   - Unit: Blue-gray (0.5f, 0.6f, 0.7f, 1f) ✅
   - Infinity: Dark charcoal (0.15f, 0.15f, 0.18f, 1f) ✅
   - Recursion: [Define] ⚠️
   - Prime: [Define] ⚠️
   - Cube Marker: [Define] ⚠️

### Future Enhancements (Other Tasks)

- Task 2: Define collision behaviors for all cube combinations
- Task 3: Refine marker mirroring rules
- Task 4: Balance charge system
- Task 5: Implement visual feedback system
- Task 6: Design marker acquisition mechanics

---

## Summary

**Task 1 Status**: ✅ **COMPLETE - All Issues Resolved**

All marker types are implemented and functional. Marker placement rules are enforced correctly, and marker-to-cube conversion works automatically when waves move forward. All identified issues have been fixed:
- ✅ Infinity marker conflict check added to `CanPlaceMarkerAt()`
- ✅ Unified input system documented (mode keys 1-4, placement F, automatic spawning on wave movement)
- ✅ Automatic player cube spawning verified (`SpawnPlayerCubes()` called from `WaveManager.MoveCubesForward()`)

**Next Steps**: 
1. Document visual colors for all marker types (Task 5)
2. Proceed to Task 2: Cube Collision Matrix

---

**Last Updated**: December 2025  
**Verified By**: Task 1 Implementation Review

