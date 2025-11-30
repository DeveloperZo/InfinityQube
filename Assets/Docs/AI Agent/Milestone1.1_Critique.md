# Milestone 1.1 Critique & Polish Report

> **Document Purpose:** Critical analysis and polish of the Paired Wave System implementation.  
> **Status:** ✅ **COMPLETED** - November 2025

---

## Executive Summary

The Paired Wave System was functionally complete but had several behavioral inconsistencies and maintainability issues that made it harder to tune and modify. This polish pass consolidated duplicate code paths, enforced consistent marker-to-cube mapping rules, and standardized naming conventions.

---

## Issues Identified & Resolved

### ✅ Issue 1: Inconsistent MarkerSpawnRules Enforcement

**Problem**: Two code paths existed for spawning inherited cubes:
- `SpawnInheritedCubes()` - respected `MarkerSpawnRules` configuration
- `SpawnCubesFromMarkers()` - ignored rules entirely (hardcoded)

**Impact**: Designer-configured rules were only applied in some scenarios.

**Resolution**: Updated `SpawnCubesFromMarkers()` to respect `MarkerSpawnRules` configuration.

---

### ✅ Issue 2: Inconsistent Naming - "Light" and "Heavy" Terminology

**Problem**: Mixed terminology used throughout the codebase:
- "Unit Markers" instead of "Unit markers"
- "Recursion Markers" instead of "Recursion markers"
- Inconsistent field casing (`RecursionMarkerPositions` vs `MatrixMarkerPositions`)

**Impact**: Confusing codebase, harder to maintain.

**Resolution**: Standardized all naming to use correct marker types:
- `lightMarkerPositions` → `unitMarkerPositions`
- `RecursionMarkerPositions` → `recursionMarkerPositions`
- `lightSpawnsUnit` → `unitSpawnsUnit`
- `heavySpawnsRecursion` → `recursionSpawnsRecursion`
- Updated all field references across the codebase

---

### ✅ Issue 3: WaveData Field Naming

**Problem**: WaveData used outdated field names:
- `maxLightMarkerCharge`, `maxLightMarkerCount`

**Resolution**: Renamed to:
- `maxUnitMarkerCharge`, `maxUnitMarkerCount`

---

## Two Wave Modes - Documentation

The paired wave system supports two distinct modes:

### 1. Additive Mode (`SpawnInheritedCubes`)
- Used during standard wave setup when `HasBeenMirrored = true`
- Spawns inherited cubes **IN ADDITION TO** base wave configuration cubes
- Respects `MarkerSpawnRules` configuration
- **Use case**: Wave A has predefined cubes, plus inherited cubes from previous markers

### 2. Replacement Mode (`SpawnMirroredWave` → `SpawnCubesFromMarkers`)
- Used for dedicated mirrored waves
- Spawns **ONLY** marker-inherited cubes (no base config cubes)
- Respects `MarkerSpawnRules` configuration
- **Use case**: Wave B is entirely player-created from Wave A placements

---

## Files Modified

| File | Changes |
|------|---------|
| `WaveData.cs` | Renamed `MarkerSpawnRules` fields, updated tooltips |
| `WaveManager.cs` | Updated `SpawnCubesFromMarkers()`, renamed `RecordedMarkerPositions` fields, added documentation |
| `PlayerActionManager.cs` | Renamed all light/Recursion Marker fields to unit/recursion |
| `PlayerPanel.cs` | Updated all field references |
| `WavePrototyper.cs` | Updated marker position references |
| `PlayerActionUI.cs` | Updated field references |
| `TutorialMessageManager.cs` | Updated field references |
| `StageDB.cs` | Updated validation references |

---

## Acceptance Criteria - Verified

- ✅ Only ONE behavior for each wave mode (additive vs replacement)
- ✅ `MarkerSpawnRules` configuration is respected in ALL scenarios
- ✅ Consistent naming across all marker-related fields (Unit, Recursion, Matrix, Infinity)
- ✅ Build passes with no errors
- ✅ Existing stages remain compatible

---

## Complete Naming Standardization

All instances of "light" and "heavy" marker terminology have been renamed to "unit" and "recursion" respectively throughout the entire codebase:

- **Method Names**: `PlaceLightMarker()` → `PlaceUnitMarker()`, etc.
- **Audio Events**: `GameAudioEvent.LightMarkerPlaced` → `GameAudioEvent.UnitMarkerPlaced`
- **Statistics**: `lightMarkerPlacements` → `unitMarkerPlacements`
- **Comments & Strings**: All user-facing and code comments updated
- **Constants**: `LIGHT_MARKER_BASE_CHARGE` → `UNIT_MARKER_BASE_CHARGE`

### Additional Files Modified (Complete Naming Pass)
| File | Changes |
|------|---------|
| `PlayerMarkerSystem.cs` | Renamed all Light/Heavy methods to Unit/Recursion |
| `Enumerations.cs` | Renamed enum values and comments |
| `AudioManager.cs` | Updated audio event references |
| `FinalIntegrationTest.cs` | Updated test method names and assertions |
| `GameUI.cs` | Updated UI strings and comments |
| `PlayerSessionData.cs` | Renamed data fields |
| `PlayerStatisticsManager.cs` | Updated statistics references |
| `WaveAnalyzer.cs` | Updated constants and comments |
| `MessageFormatter.cs` | Updated message filtering |
| `MessageFormatterDemo.cs` | Updated demo messages |

---

*Completed: November 2025*

