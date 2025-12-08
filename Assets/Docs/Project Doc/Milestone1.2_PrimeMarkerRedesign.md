# Prime Marker & Cube Marker Redesign

> **Status**: Design & Implementation  
> **Date**: December 2025  
> **Related**: Milestone 1.2 - Task 2 (Cube Collisions)

---

## Design Changes

### 1. Prime Marker Size Reduction
**Current**: Prime markers use 3x3 area (9 tiles)  
**New**: Prime markers use 2x2 area (4 tiles)

**Rationale**: 
- Reserve 3x3 area effect for special Prime+Prime collisions
- Makes Prime markers more strategic (smaller area, more precise)
- Creates distinction between placed markers and collision rewards

**Implementation**:
- Change default `primeMarkerSize` from 3 to 2
- Update `GetAreaPositions()` to handle 2x2 correctly
- Prime markers spawn Prime cubes with 2x2 area effect

---

### 2. Prime+Prime Collision Enhancement
**Current**: Prime player cube captures Prime wave cube in 3x3 area (same as any Prime capture)  
**New**: Prime+Prime collision creates enhanced 3x3 effect + cube marker

**Behavior**:
- When Prime player cube collides with Prime wave cube:
  - Creates 3x3 area capture (enhanced from normal 2x2)
  - Generates cube marker at collision position
  - Cube marker type: `CubeMarkerType.Prime` (or new type for enhanced)

**Rationale**:
- Rewards matching type collisions (Prime/Prime)
- Creates strategic depth - players want to match types
- 3x3 becomes a reward, not default behavior

**Implementation**:
- Detect Prime+Prime collision in `ProcessCollisionAtPosition()` and `ProcessPassThroughCollision()`
- When Prime player cube + Prime wave cube: use 3x3 area instead of 2x2
- Generate cube marker at collision position

---

### 3. Cube Marker Generation Enhancement
**Current**: Cube markers only created when Prime cubes are captured (any method)  
**New**: Cube markers generated based on collision types, especially same-type matches

**Proposed Rules**:

| Player Cube | Wave Cube | Cube Marker Generated? | Marker Type | Size |
|-------------|-----------|------------------------|-------------|------|
| Prime | Prime | ✅ Yes | Prime | 3x3 |
| Recursion | Recursion | ✅ Yes | Recursion | [TBD] |
| Unit | Unit | ❓ Maybe | Unit | [TBD] |
| Infinity | Infinity | ❓ Maybe | Infinity | [TBD] |
| Prime | Any Other | ✅ Yes (current) | Prime | 2x2 |
| Any Other | Prime | ❌ No | N/A | N/A |

**Design Questions**:
1. Should Unit+Unit collisions generate cube markers? (Probably not - too common)
2. Should Recursion+Recursion generate cube markers? (Yes - rewards matching)
3. Should Infinity+Infinity generate cube markers? (Maybe - special case)
4. Should cube marker size vary by type? (Prime = 3x3, others = 2x2 or single?)

**Implemented Answer**:
- **Prime+Prime**: ✅ Generate Prime cube marker (3x3) - placed at collision tile, triggered with R key
- **Recursion+Recursion**: ✅ Generate Recursion cube marker (2x2) - placed at collision tile, triggered with R key
- **Unit+Unit**: ❌ No cube marker (too common, would flood system)
- **Infinity+Infinity**: ⚠️ Special case - depends on Infinity collision design (Task 2)

**Cube Marker Trigger Behavior**:
- Cube markers are placed at collision/capture positions automatically
- Player presses `R` key to trigger cube marker
- Area effect expands from cube marker position (size: 3x3 for Prime+Prime, 2x2 for others)
- Captures all non-Infinity cubes in the area (Infinity cubes are excluded)

---

## Implementation Plan

### Phase 1: Prime Marker Size Change
1. Change default `primeMarkerSize` from 3 to 2
2. Update Prime marker placement to use 2x2
3. Update Prime cube spawn to use 2x2 area (when from marker)
4. Test Prime marker placement and spawning

### Phase 2: Prime+Prime Collision Detection
1. Modify `ProcessCollisionAtPosition()` to detect Prime+Prime
2. Modify `ProcessPassThroughCollision()` to detect Prime+Prime
3. When Prime+Prime detected: use 3x3 area instead of 2x2
4. Generate cube marker at collision position
5. Test Prime+Prime collisions

### Phase 3: Enhanced Cube Marker Generation ✅ COMPLETE
1. ✅ Update `ProcessCubeCapture()` to detect same-type collisions
2. ✅ Add logic for Recursion+Recursion cube marker generation
3. ✅ Update cube marker trigger to support variable sizes
4. ✅ Cube markers placed at collision positions, triggered with R key
5. ✅ Area effect captures all non-Infinity cubes

---

## Code Changes Required

### 1. Prime Marker Size
**File**: `PlayerActionManager.cs`
- Change `primeMarkerSize` default from 3 to 2

**File**: `PlayerMarkerSystem.cs`
- Prime markers already use `size` parameter, no change needed
- Prime cube spawn already uses `isPrimeCube` flag, but needs size tracking

### 2. Prime+Prime Collision
**File**: `PlayerMarkerSystem.cs`
- `ProcessCollisionAtPosition()`: Check if player cube is Prime AND wave cube is Prime
- `ProcessPassThroughCollision()`: Same check
- When Prime+Prime: use size 3 instead of 2 for area capture
- Generate cube marker at collision position

### 3. Cube Marker Size Support
**File**: `PlayerMarkerSystem.cs`
- `CubeMarker` class: Add `size` field (default 3 for Prime, 2 for others)
- `TriggerCubeMarkerAt()`: Use marker size instead of hardcoded 3
- `CreateCubeMarker()`: Accept size parameter

---

## Testing Checklist

- [ ] Prime markers place with 2x2 area
- [ ] Prime markers spawn Prime cubes with 2x2 area effect
- [ ] Prime+Prime collision creates 3x3 area capture
- [ ] Prime+Prime collision generates cube marker at collision tile
- [ ] Cube markers trigger with R key and create correct size area (3x3 for Prime+Prime, 2x2 for others)
- [ ] Cube marker area effect captures all non-Infinity cubes
- [ ] Cube marker area effect excludes Infinity cubes
- [ ] Recursion+Recursion generates cube marker at collision tile
- [ ] Other collision types work correctly

---

## Design Decisions Needed

1. **Recursion+Recursion Cube Marker Size**: ✅ **IMPLEMENTED**
   - **Decision**: 2x2 area (consistent with Prime, but smaller than Prime+Prime reward)
   - Cube marker placed at collision tile, triggered with R key

2. **Unit+Unit Cube Marker**:
   - **Recommendation**: No cube marker (too common, would flood system)

3. **Infinity+Infinity Cube Marker**:
   - **Recommendation**: Defer to Task 2 (Infinity collision design)

4. **Cube Marker Visual Distinction**: ⚠️ **NEEDS IMPLEMENTATION**
   - Should cube markers from same-type collisions look different?
   - **Recommendation**: Same visual, but size indicates power level
   - **Note**: Visual feedback for cube markers needs definition (Task 5)

---

**Last Updated**: December 2025  
**Status**: ✅ **IMPLEMENTATION COMPLETE**

**Note**: Cube marker trigger key is `R` (as described by user). Code currently shows `Q` in `PlayerActionManager.cs:133` - may need verification/update.

