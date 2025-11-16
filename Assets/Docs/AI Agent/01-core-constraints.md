# Core Constraints

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Merged from shrimp-rules.md  
**Authority Level**: CRITICAL - Overrides all other instructions  
**Review Cycle**: Monthly  
**Enforcement**: AUTOMATIC via validation checks  

---

## Purpose
These fundamental behavioral rules establish the absolute boundaries for all AI agent operations. Violations of these constraints risk system integrity, code quality degradation, or architectural corruption.

## NEVER DO List (Absolute Prohibitions)

### Architectural Violations
- **NEVER split existing manager files** without explicit approval
  - GridManager.cs, PlayerManager.cs, WaveManager.cs, etc.
  - Tile.cs, CubeManager.cs, or any core components
- **NEVER introduce new singleton patterns** without approval
- **NEVER modify Enumerations.cs** core enums without approval
- **NEVER change Unity lifecycle patterns** (Awake, Start, Update sequences)
- **NEVER create new documentation files** without approval

### Performance Violations
- **NEVER use FindObjectOfType<>() in Update()** or repeating methods
- **NEVER create unbounded loops** without exit conditions
- **NEVER allocate large arrays** in frequently called methods
- **NEVER use reflection** in gameplay code

### Code Quality Violations
- **NEVER commit partial method implementations**
- **NEVER leave incomplete control structures** (if/for/while)
- **NEVER remove debug logging** from critical operations
- **NEVER bypass validation checks**

## ALWAYS DO List (Mandatory Practices)

### Validation Requirements
- **ALWAYS run build validation** before marking task complete
- **ALWAYS check file size limits** before modifications
- **ALWAYS score complexity** after planning
- **ALWAYS validate manager references** in Start()

### Code Standards
- **ALWAYS use the standard debug pattern**:
```csharp
[Header("Debug")]
public bool enableDebugLogs = true;

private void DebugLog(string methodName, string message) 
{
    if (enableDebugLogs) 
        Debug.Log($"[{GetType().Name}] {methodName}: {message}");
}
```

- **ALWAYS follow the manager reference pattern**:
```csharp
#region Manager References
private WaveManager waveManager;
#endregion

private void Start() 
{
    waveManager = FindObjectOfType<WaveManager>();
    ValidateReferences();
}
```

### Communication Standards
- **ALWAYS cache manager references** in Start(), not Update()
- **ALWAYS validate references** after assignment
- **ALWAYS use Instance property** when singleton available
- **ALWAYS handle null references** gracefully

## Modification Boundaries

### Complete Unit Rule
Valid modifications must be complete functional units:
- ✅ **Complete methods** from signature to closing brace
- ✅ **Method clusters** that work together cohesively
- ✅ **Complete properties** including getters/setters
- ✅ **Complete event handlers** with full implementation
- ❌ **Partial method bodies** 
- ❌ **Incomplete control structures**
- ❌ **Individual lines** within complex methods

### Region Organization
Maintain this standard structure:
```csharp
public class ManagerName : MonoBehaviour
{
    #region Inspector Configuration
    #endregion
    
    #region Manager References
    #endregion
    
    #region Runtime State
    #endregion
    
    #region Properties
    #endregion
    
    #region Unity Lifecycle
    #endregion
    
    #region Public API
    #endregion
    
    #region Private Methods
    #endregion
    
    #region Debug
    #endregion
}
```

## POC Code Philosophy

### Marking Convention
```csharp
// POC: Quick implementation for testing - may need refinement
public void TemporaryFeature()
{
    // Functional but not optimized
}
```

### POC Rules
- POC code must be **functional** even if not optimized
- POC code must be **marked** with // POC: comment
- POC code has **no upgrade requirement** - if it works, it works
- POC code can **violate optimization** but not safety rules

## Validation Gates

### Automatic Stops
These conditions automatically halt continuous mode:
1. File over size limit detected
2. Build validation failure
3. Complexity score ≥ 7 without approval
4. Core constraint violation detected

### Manual Review Triggers
These require human review before proceeding:
1. Cross-system modifications
2. New pattern introduction
3. Performance-critical changes
4. External dependency additions

## Exception Handling

### When Constraints Conflict
If constraints conflict with task requirements:
1. **Document the conflict** clearly
2. **Request human approval** with options
3. **Wait for explicit override** permission
4. **Document the override** in code comments

### Emergency Overrides
Only with explicit human approval:
- Temporary constraint bypass for critical fixes
- Time-boxed exception for specific tasks
- Must be reverted after emergency

## Monitoring and Compliance

### Self-Check Protocol
Before any modification:
1. Review this constraint list
2. Verify no violations in plan
3. Check automatic validation availability
4. Confirm within safe zones

### Violation Reporting
If constraint violation detected:
1. Stop immediately
2. Document violation type
3. Set task to `needs_approval`
4. Generate violation report

## Quick Reference

### Critical Numbers
- **600 lines** - Max for core components
- **400 lines** - Max for managers
- **300 lines** - Max for utilities
- **7/10** - Complexity requiring approval
- **3 failures** - Triggers continuous mode stop

### Critical Patterns
- Debug logging: `[ManagerName] MethodName: Message`
- Manager refs: Cache in Start(), validate always
- POC marking: `// POC: description`
- Validation: Build before complete

---

**Validation Command**: `validate_constraints.bat`  
**Override Process**: Requires written approval with justification  
**Violation Penalty**: Immediate continuous mode suspension