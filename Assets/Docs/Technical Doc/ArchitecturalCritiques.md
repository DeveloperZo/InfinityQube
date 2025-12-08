# Architectural Critiques

> **Document Purpose:** This document provides a critical analysis of the codebase architecture, identifying strengths, weaknesses, and opportunities for improvement. It serves as a guide for refactoring decisions and architectural evolution.

## Overview

**Analysis Date**: November 28, 2025  
**Codebase State**: Post-hiatus, paired wave system in design  
**Total Scripts**: 84 C# files  
**Total Lines**: ~26,751 lines of C# code

---

## Executive Summary

### Architecture Health Score: 🟡 **6.5/10**

**Strengths**:
- Clear manager-based architecture
- Good use of ScriptableObjects for data
- Event-driven communication patterns
- Comprehensive debug infrastructure
- Well-documented enumeration system

**Critical Issues**:
- 10 files violate size limits (some by 200%+)
- God class anti-pattern in major managers
- Tight coupling between some systems
- OnGUI still used for debug panels (acceptable for debug-only)

---

## 1. File Size Violations (Critical)

### Current Violations

The project establishes these limits:
- **Core Components**: 750 lines max
- **Manager Classes**: 600 lines max
- **Utility Classes**: 300 lines max

**Violation Summary**:

| File | Lines | Limit | Over By | Priority |
|------|-------|-------|---------|----------|
| WaveManager.cs | 1949 | 600 | **225%** | 🔴 Critical |
| PlayerActionManager.cs | 1655 | 600 | **176%** | 🔴 Critical |
| CubeManager.cs | 1378 | 600 | **130%** | 🔴 Critical |
| PlayerStatisticsManager.cs | 1347 | 600 | **125%** | 🔴 Critical |
| PlayerMarkerSystem.cs | 1347 | 600 | **125%** | 🔴 Critical |
| GridManager.cs | 1276 | 600 | **113%** | 🔴 Critical |
| IQWaveGenerator.cs | 1043 | 750 | **39%** | 🟡 Medium |
| TutorialMessageManager.cs | 997 | 600 | **66%** | 🟡 Medium |
| FacePaintingManager.cs | 861 | 600 | **44%** | 🟡 Medium |
| MessageProgressTracker.cs | 775 | 750 | **3%** | 🟢 Low |

### Impact Assessment

**Maintenance Burden**:
- Difficult to locate specific functionality
- High cognitive load when making changes
- Merge conflict risk in team scenarios
- Test isolation challenges

**Recommended Extractions**:

#### WaveManager.cs (1949 → target 600)
Extract these subsystems:
1. `WaveSpawnSystem.cs` (~400 lines) - Cube spawning and positioning logic
2. `WavePairingSystem.cs` (~300 lines) - Marker inheritance and paired wave logic
3. `WaveStatistics.cs` (~200 lines) - Wave tracking and statistics
4. `WaveUIController.cs` (~150 lines) - Message display and UI interaction

#### PlayerActionManager.cs (1655 → target 600)
Extract these subsystems:
1. `MarkerPlacementController.cs` (~400 lines) - Placement validation and execution
2. `MarkerTriggerController.cs` (~300 lines) - Trigger logic and effects
3. `MarkerChargeSystem.cs` (~200 lines) - Charge management and regeneration
4. Keep core coordination in PlayerActionManager (~400 lines)

#### CubeManager.cs (1378 → target 600)
Extract these subsystems:
1. `CubeMovementController.cs` (~300 lines) - Movement and animation
2. `CubeFacePaintingController.cs` (~250 lines) - Face status and painting
3. `CubeCollisionHandler.cs` (~200 lines) - Collision detection and response
4. Keep core cube state in CubeManager (~400 lines)

---

## 2. Design Pattern Analysis

### Patterns Successfully Applied ✅

#### Singleton Pattern
```
GridManager.Instance
AudioManager (DontDestroyOnLoad)
```
**Assessment**: Appropriately used for truly global managers. No over-application detected.

#### Facade Pattern (Tile.cs)
```csharp
// Tile delegates to specialized components:
private TileVisuals tileVisuals;
private TileMarker tileMarker;
private TileCorruption tileCorruption;
private TileFacePainting tileFacePainting;
```
**Assessment**: Excellent application. Tile.cs is well-organized at 602 lines despite complex functionality.

#### Observer Pattern (UnityEvents)
```csharp
[SerializeField] public UnityEvent<int> OnWaveComplete;
[SerializeField] public UnityEvent<int> OnWaveFailed;
[SerializeField] public UnityEvent OnAllWavesComplete;
```
**Assessment**: Good use for cross-manager communication. Reduces coupling.

#### ScriptableObject Data Pattern
```
WaveData, StageData, CubeTypeDefinitions, CubeAudioConfiguration
```
**Assessment**: Excellent for configuration. Enables designer-friendly tuning.

#### Static Using Pattern
```csharp
using static Enumerations;
// Enables: CubeType.Unit instead of Enumerations.CubeType.Unit
```
**Assessment**: Clean implementation. Reduces verbosity throughout codebase.

### Patterns Needing Attention ⚠️

#### God Class Anti-Pattern
`WaveManager` and `PlayerActionManager` exhibit god class characteristics:
- Too many responsibilities
- High coupling to other systems
- Difficult to test in isolation

**Recommendation**: Apply Single Responsibility Principle through extraction (see Section 1).

#### Missing Strategy Pattern
Cube behavior varies by type but is handled through switch statements:
```csharp
// Anti-pattern in CubeManager:
switch (type) {
    case CubeType.Unit: // behavior
    case CubeType.Matrix: // different behavior
    // etc.
}
```
**Recommendation**: Consider `ICubeBehavior` interface with type-specific implementations.

#### Missing Factory Pattern
Cube instantiation is scattered across WaveManager and debug tools.

**Recommendation**: Implement `CubeFactory` for centralized cube creation with proper initialization.

---

## 3. Coupling Analysis

### High Coupling Areas 🔴

#### WaveManager Dependencies
```
WaveManager requires:
├── GridManager (direct reference)
├── PlayerManager (direct reference)
├── PlayerActionManager (direct reference)
├── GameUI (direct reference)
├── AudioManager (runtime lookup)
└── StageManager (event-based - good)
```
**Impact**: Changes to any dependent manager risk breaking WaveManager.

#### PlayerActionManager Dependencies
```
PlayerActionManager requires:
├── GridManager (direct reference)
├── PlayerManager (direct reference)
├── WaveManager (direct reference)
├── PlayerActionUI (direct reference)
├── PlayerMarkerSystem (direct reference)
├── AudioManager (direct reference)
├── InputFeedbackManager (direct reference)
└── AnimationTriggerManager (direct reference)
```
**Impact**: 8 direct dependencies. High risk of cascade failures.

### Low Coupling Areas ✅

#### Event-Driven Audio System
```csharp
public enum GameAudioEvent {
    CubeLanded, CubeCaptured, CubeEscaped,
    LightMarkerPlaced, MatrixMarkerPlaced, RecursionMarkerPlaced,
    // etc.
}
```
**Assessment**: AudioManager subscribes to events. Excellent decoupling.

#### Tile Component Delegation
```csharp
// Clean component isolation:
tileVisuals.ShowHighlight();
tileMarker.PlaceMarker(type);
tileCorruption.ApplyCorruption();
```
**Assessment**: Each component is independently testable.

### Coupling Improvement Recommendations

1. **Introduce Service Locator or Dependency Injection**
   - Register managers at startup
   - Resolve dependencies at runtime
   - Enables mock injection for testing

2. **Expand Event-Driven Communication**
   - Add more UnityEvents for cross-manager communication
   - Reduce direct method calls between managers

3. **Create Interface Contracts**
   ```csharp
   public interface IGridProvider {
       bool IsValidGridPosition(Vector2Int position);
       Vector3 GridToWorldPosition(int x, int y, float height);
   }
   ```
   - Managers depend on interfaces, not concrete classes

---

## 4. Component Architecture

### Well-Organized Components ✅

#### Tile System (Assets/scripts/Components/Tile/)
```
Tile/
├── Tile.cs (602 lines) - Facade
├── TileVisuals.cs - Visual state
├── TileMarker.cs - Marker management
├── TileCorruption.cs - Corruption state
└── TileFacePainting.cs - Face painting
```
**Assessment**: Excellent component separation. Model for other systems.

#### AudioManager System (Assets/scripts/Managers/AudioManager/)
```
AudioManager/
├── AudioManager.cs - Core singleton
├── AudioSourcePool.cs - Resource pooling
├── AudioPlaybackSystem.cs - Playback control
├── AudioVolumeController.cs - Volume management
├── CubeAudioSystem.cs - Cube-specific audio
└── AudioDebugSystem.cs - Debug tools
```
**Assessment**: Well-decomposed. Each file has single responsibility.

### Monolithic Components ⚠️

#### Manager Classes
Most managers are monolithic:
```
Managers/
├── WaveManager.cs (1949 lines) - MONOLITHIC
├── PlayerActionManager.cs (1655 lines) - MONOLITHIC
├── CubeManager.cs (1378 lines) - MONOLITHIC
├── GridManager.cs (1276 lines) - MONOLITHIC
└── StageManager.cs (771 lines) - Acceptable
```

**Recommendation**: Apply Tile-style decomposition to major managers.

---

## 5. Data Architecture

### ScriptableObject Usage ✅

**Strengths**:
- Configuration separated from code
- Designer-friendly editing
- Easy A/B testing and balancing

**Current ScriptableObjects**:
| Asset | Purpose | Location |
|-------|---------|----------|
| WaveData | Wave configuration | Assets/data/ |
| StageData | Stage configuration | Assets/data/ |
| CubeTypeDefinitions | Cube type data | Assets/data/ |
| CubeAudioConfiguration | Audio settings | Assets/data/ |
| MessageDatabase | Tutorial messages | Assets/data/ |

### Data Flow Concerns ⚠️

#### Paired Wave System Data
Current implementation stores marker positions in runtime dictionary:
```csharp
private RecordedMarkerPositions previousWaveMarkers = null;
private Dictionary<WaveData, bool> waveMirrorState = new Dictionary<WaveData, bool>();
```

**Concern**: Runtime state not persisted. Lost on scene reload.

**Recommendation**: Extend WaveData ScriptableObject to store marker inheritance configuration.

#### Statistics Persistence
`PlayerStatisticsManager` (1347 lines) handles both tracking and persistence.

**Recommendation**: Separate into:
- `PlayerStatisticsTracker.cs` - Runtime tracking
- `PlayerStatisticsSerializer.cs` - Save/Load operations

---

## 6. Debug Infrastructure

### Current Debug Architecture

**Panel System**:
- Uses OnGUI for rendering (acceptable for debug-only)
- `IManagerDebugInterface` for consistent panel integration
- Prototyping system for rapid iteration

**Strengths**:
- Comprehensive coverage of all systems
- Real-time state inspection
- In-game parameter adjustment

**Concerns**:
- Debug panels mixed with production code in some managers
- Some managers implement IManagerDebugInterface with significant code

### Recommendations

1. **Separate Debug Code**
   ```csharp
   #if UNITY_EDITOR || DEVELOPMENT_BUILD
   // Debug-only code
   #endif
   ```

2. **Centralized Debug Panel Registration**
   - Move panel definitions out of manager files
   - Create `DebugPanelDefinitions.cs` for panel layouts

---

## 7. Memory and Performance Patterns

### Good Patterns ✅

#### Object Pooling Ready
```csharp
// GridManager
public bool useObjectPooling = false;
public int pooledTileCount = 100;
private Queue<GameObject> tilePool = new Queue<GameObject>();
```
**Assessment**: Infrastructure exists but not fully utilized.

#### Cached References
```csharp
// Managers cache references in Start()
private GridManager gridManager;
private void Start() {
    gridManager = GridManager.Instance;
}
```
**Assessment**: Follows recommended pattern. Avoids FindObjectOfType in Update.

### Potential Issues ⚠️

#### FindAnyObjectByType Usage
```csharp
// CubeManager.Init()
playerActionManager = FindAnyObjectByType<PlayerActionManager>();
```
**Concern**: Called during cube initialization. Could be expensive with many cubes.

**Recommendation**: Pass reference through Init() parameter instead.

#### Large Dictionaries
```csharp
// WaveManager
private Dictionary<WaveData, bool> waveMirrorState = new Dictionary<WaveData, bool>();
```
**Concern**: Unbounded growth potential. Need cleanup strategy.

---

## 8. Naming and Conventions

### Consistent Patterns ✅

**Enumerations**: Clear, descriptive names
```csharp
CubeType.Unit, CubeType.Matrix, CubeType.Infinity, CubeType.Recursion
MarkerType.Light, MarkerType.Heavy, MarkerType.Matrix, MarkerType.Cube
```

**Manager Naming**: `[Domain]Manager` pattern consistently applied
```
WaveManager, GridManager, PlayerManager, AudioManager, StageManager
```

### Inconsistencies ⚠️

**Marker Classes**: Mixed naming
```csharp
LightMarker // Class in PlayerActionManager.cs
RecursionMarker // Class in PlayerActionManager.cs
MatrixMarker // Class in PlayerActionManager.cs
// But also:
PlayerMarkerSystem // Separate manager
```
**Recommendation**: Consolidate marker data classes into PlayerMarkerSystem or dedicated file.

---

## 9. Priority Refactoring Roadmap

### Phase 1: Critical (Immediate - Next Sprint)
1. **WaveManager Decomposition** (Complexity: 6)
   - Extract WaveSpawnSystem
   - Extract WavePairingSystem
   - Reduce to 600 lines

2. **PlayerActionManager Decomposition** (Complexity: 5)
   - Extract MarkerPlacementController
   - Extract MarkerChargeSystem
   - Reduce to 600 lines

### Phase 2: High Priority (Q1 2026)
3. **CubeManager Decomposition** (Complexity: 4)
   - Extract CubeMovementController
   - Extract CubeFacePaintingController

4. **GridManager Decomposition** (Complexity: 4)
   - Extract GridObjectPool
   - Extract GridBatchOperations

### Phase 3: Medium Priority (Q2 2026)
5. **PlayerStatisticsManager Decomposition** (Complexity: 3)
6. **TutorialMessageManager Decomposition** (Complexity: 3)
7. **Interface Extraction for Dependency Injection** (Complexity: 4)

### Phase 4: Low Priority (Post-Release)
8. **Strategy Pattern for Cube Behaviors** (Complexity: 3)
9. **Factory Pattern Implementation** (Complexity: 2)
10. **Full Dependency Injection Framework** (Complexity: 5)

---

## 10. Architecture Evolution Vision

### Current State (November 2025)
```
[Monolithic Managers] → [Direct References] → [Tight Coupling]
```

### Target State (Post-Refactoring)
```
[Decomposed Subsystems] → [Interface Contracts] → [Event-Driven Communication]
```

### Benefits of Target State
- **Testability**: Each subsystem independently testable
- **Maintainability**: Changes isolated to specific subsystems
- **Scalability**: New features don't bloat existing systems
- **Velocity**: Faster development with clear boundaries

---

## Appendix: Code Quality Metrics

### Lines of Code by Category
| Category | Files | Total Lines | Avg/File |
|----------|-------|-------------|----------|
| Managers | 17 | ~15,000 | 882 |
| Components | 8 | ~2,500 | 313 |
| Core | 6 | ~4,500 | 750 |
| Data | 17 | ~1,500 | 88 |
| UI | 6 | ~2,200 | 367 |
| Utils | 8 | ~1,500 | 188 |

### Complexity Distribution
- **High Complexity** (>1000 lines): 6 files
- **Medium Complexity** (500-1000 lines): 8 files
- **Low Complexity** (<500 lines): 70 files

---

*Last Updated: November 28, 2025*  
*Analysis Methodology: Manual code review + static analysis*  
*Next Review: January 2026*

## (END OF DOCUMENT)
