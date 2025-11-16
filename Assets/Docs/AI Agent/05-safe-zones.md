# Safe Modification Zones

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Clear autonomous boundaries  
**Authority Level**: OPERATIONAL - Defines autonomous work areas  
**Review Cycle**: Weekly (based on stability)  
**Enforcement**: Pre-modification validation  

---

## Purpose
Identifies files, systems, and operations that AI agents can modify autonomously without requiring human approval, enabling efficient development while protecting critical systems.

## Safe Zone Categories

## 🟢 Green Zone (Full Autonomy)
Complete freedom to modify without approval:

### Debug Systems
- All files in `Scripts/Debuggers/`
- Debug panels and visualization
- Logging improvements
- Performance monitoring
- Test utilities

### UI Components  
- `Scripts/UI/` (except core GameUI.cs)
- Debug UI elements
- HUD updates
- Visual feedback systems
- UI animations

### Data Files
- ScriptableObject assets
- Wave configurations
- Stage data files
- Audio configurations
- Prefab modifications

### Test Systems
- All test files
- Validation scripts
- Performance tests
- Integration tests

## 🟡 Yellow Zone (Conditional Autonomy)
Can modify IF conditions are met:

### Existing Managers (Internal Methods Only)
**Safe to Modify**:
- Adding new private methods
- Optimizing existing methods
- Adding debug features
- Improving error handling
- Adding validation

**Requires Approval**:
- Changing public API
- Modifying interfaces
- Altering communication patterns
- Changing initialization

### Components
**Safe to Modify**:
- Tile.cs (visual methods)
- CubeManager.cs (internal logic)
- Player components (non-core)

**Requires Approval**:
- Core behavior changes
- State machine modifications
- Physics alterations

### Utilities
**Safe to Modify**:
- Helper methods
- Extension methods
- Calculation utilities
- Formatting functions

**Requires Approval**:
- Core algorithms
- System utilities
- Critical paths

## 🔴 Red Zone (No Autonomy)
NEVER modify without explicit approval:

### Core Systems
- Enumerations.cs
- Unity lifecycle methods
- Singleton implementations
- Manager communication patterns
- Event systems

### Architecture Files
- GridManager.cs (core methods)
- WaveManager.cs (wave progression)
- PlayerActionManager.cs (input handling)
- StageManager.cs (progression logic)

### Critical Paths
- Save/Load systems
- Player input processing
- Core game loop
- Resource management

## Operation-Specific Permissions

### ✅ Always Allowed
```csharp
// Adding debug logging
DebugLog("MethodName", "Helpful debug message");

// Adding validation
if (parameter == null)
    throw new ArgumentNullException(nameof(parameter));

// Adding comments
// TODO: Optimize this when proven necessary

// Creating POC implementations
// POC: Quick implementation for testing
```

### ⚠️ Conditional Approval
```csharp
// Adding public methods - Need approval if changes interface
public void NewPublicMethod() { }

// Modifying algorithms - Need approval if core logic
private void OptimizedAlgorithm() { }

// Changing data structures - Need approval if affects serialization
[SerializeField] private NewDataType data;
```

### ❌ Never Allowed
```csharp
// Changing Unity lifecycle
void Start() { } // -> void Awake() { } // FORBIDDEN

// Modifying singletons
public static Instance { get; set; } // FORBIDDEN

// Altering enums
public enum CubeType { Unit, Prime, NewType } // FORBIDDEN
```

## File-Specific Rules

### Tile.cs
**Safe Zone**:
- UpdateVisuals()
- SetHighlight()
- Debug methods
- Visual effects

**Danger Zone**:
- Grid coordinate logic
- State management
- Marker placement

### GridManager.cs  
**Safe Zone**:
- Debug visualization
- Validation helpers
- Logging improvements

**Danger Zone**:
- Grid generation
- Coordinate systems
- Tile management

### WaveManager.cs
**Safe Zone**:
- Statistics tracking
- Debug controls
- Visual feedback

**Danger Zone**:
- Wave progression
- Spawn logic
- Timing systems

### PlayerActionManager.cs
**Safe Zone**:
- Visual feedback
- Debug commands
- Statistics

**Danger Zone**:
- Input processing
- Marker systems
- Action validation

## Modification Guidelines

### Before Modifying
1. Check safe zone category
2. Verify conditions if yellow zone
3. Ensure not in red zone
4. Validate no side effects

### During Modification
1. Follow code patterns (04-code-patterns.md)
2. Maintain file standards (03-file-standards.md)
3. Add appropriate debug logging
4. Mark POC code clearly

### After Modification
1. Run validation tests
2. Check file size limits
3. Update documentation if needed
4. Commit with clear message

## Special Permissions

### Time-Limited Autonomy
Certain zones may get temporary autonomy:
- During bug fix windows
- For specific features
- During testing phases
- With explicit time bounds

### Emergency Overrides
In critical situations:
- Document emergency clearly
- Make minimal changes
- Mark as "EMERGENCY FIX"
- Request post-review

## Safe Zone Updates

### Promotion to Safer Zone
Files can be promoted when:
- Stable for 2+ weeks
- Well-tested
- Clear interfaces
- Good documentation

### Demotion to Danger Zone
Files are demoted when:
- Recent instability
- Critical bugs found
- Architecture changes planned
- Complex dependencies discovered

## Quick Reference

### 🟢 Go Ahead
- Debug improvements
- Test additions
- Documentation updates
- POC implementations
- Visual enhancements

### 🟡 Check First  
- New public methods
- Algorithm changes
- Data structure updates
- Cross-file references

### 🔴 Stop and Ask
- Core system changes
- Architecture modifications
- Enum alterations
- Lifecycle changes
- Singleton patterns

## Validation Commands

```bash
# Check if file is in safe zone
check_safe_zone.bat [filename]

# Validate modification allowed
validate_modification.bat [filename] [operation]

# Request zone promotion
request_zone_change.bat [filename] [current] [requested]
```

---

**Zone Status Check**: `zone_status.bat`  
**Override Request**: Use approval gate process  
**Emergency Protocol**: Mark clearly, fix minimally, review after