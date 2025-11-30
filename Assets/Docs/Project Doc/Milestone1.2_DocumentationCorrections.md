# Milestone 1.2 - Documentation Corrections

> **Date**: December 2025  
> **Purpose**: Document corrections made to reflect actual marker system behavior

---

## Key Corrections Made

### 1. Unified Input System (Not Individual Keys)

**Previous (Incorrect)**:
- Unit markers: F key to place, R key to trigger
- Recursion markers: V key to place, Y key to trigger
- Matrix markers: G key to place, T key to trigger
- Infinity markers: H key to place, [undefined] to trigger

**Correct**:
- **Mode Selection**: Keys `1`, `2`, `3`, `4` switch between marker modes
  - `1` = Unit Marker mode
  - `2` = Matrix Marker mode
  - `3` = Recursion Marker mode
  - `4` = Infinity Marker mode
- **Placement**: `F` key places marker of current mode
- **No manual trigger for regular markers** - they automatically spawn player cubes

---

### 2. Automatic Player Cube Spawning (Not Manual Trigger)

**Previous (Incorrect)**:
- Markers are triggered manually with R key to spawn player cubes
- Triggering converts markers to player cubes

**Correct**:
- **Automatic Spawning**: When wave moves forward, `SpawnPlayerCubes()` is automatically called
- All placed markers automatically convert to player cubes that move backward
- This is the PRIMARY mechanism for marker-to-cube conversion
- No manual trigger needed for regular markers

**Implementation**:
- `WaveManager.MoveCubesForward()` calls `SpawnPlayerCubes()` at `WaveManager.cs:862`
- `SpawnPlayerCubes()` converts all markers to player cubes at `PlayerMarkerSystem.cs:770`

---

### 3. Cube Markers vs Regular Markers

**Previous (Incorrect)**:
- R key triggers regular markers
- Cube markers use Q key

**Correct**:
- **Regular Markers** (Unit, Recursion, Matrix, Infinity):
  - Automatically spawn player cubes when wave moves forward
  - No manual trigger needed
- **Cube Markers** (generated from collisions):
  - Created automatically at collision positions
  - **R key triggers cube marker** to create area effect
  - Area effect captures all non-Infinity cubes in the area
  - Different from regular markers - these require manual triggering

**Cube Marker Behavior**:
- Matrix+Matrix collision → Cube marker placed at collision tile → R key triggers 3x3 area
- Recursion+Recursion collision → Cube marker placed at collision tile → R key triggers 2x2 area
- Matrix captured by non-Matrix → Cube marker placed at capture position → R key triggers 2x2 area

---

### 4. Matrix Marker Size

**Previous (Incorrect)**:
- Matrix markers use 3x3 area

**Correct**:
- Matrix markers use **2x2 area** (from placement)
- Matrix+Matrix collisions create **3x3 area effect** (enhanced reward)
- Matrix+Matrix collisions generate **3x3 cube marker** (triggered with R key)

---

## Files Updated

1. **Milestone1.2_Task1_Verification.md**:
   - Updated unified input system description
   - Corrected automatic spawning mechanism
   - Clarified cube marker vs regular marker distinction
   - Updated Matrix marker size information

2. **3_GameplayMechanics.md**:
   - Updated marker system section with unified input
   - Removed outdated individual keys
   - Documented automatic spawning
   - Updated Matrix marker coverage

3. **Milestone1.2_Planning.md**:
   - Updated current state description
   - Marked automatic spawning verification as complete

4. **Milestone1.2_MatrixMarkerRedesign.md**:
   - Updated cube marker trigger behavior
   - Documented R key trigger for cube markers
   - Updated testing checklist

5. **Milestone1.2_MatrixMarkerRedesign_Summary.md**:
   - Updated cube marker generation rules
   - Documented R key trigger behavior

---

## Key Takeaways

1. **Regular Markers**: Place with F (after mode selection 1-4), automatically spawn player cubes on wave movement
2. **Cube Markers**: Generated from collisions, triggered with R key to create area effects
3. **Matrix Markers**: 2x2 from placement, 3x3 for Matrix+Matrix collisions
4. **No Manual Trigger**: Regular markers do NOT use R key - they automatically convert

---

**Last Updated**: December 2025  
**Status**: Documentation corrected to match actual implementation

