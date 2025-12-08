# Validation Rules

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Comprehensive validation framework  
**Authority Level**: MANDATORY - Required for quality assurance  
**Review Cycle**: Bi-weekly (high-touch validation)  
**Enforcement**: AUTOMATIC via build system  

---

## Purpose
Establishes validation requirements, testing protocols, and quality gates that all code changes must pass before being considered complete, ensuring system stability and preventing regressions.

## Validation Pipeline

### Stage 1: Pre-Commit Validation
Before any code can be committed:

```bash
# Run automatically or manually
pre_commit_validate.bat

Checks:
☐ File size limits respected
☐ Required patterns present  
☐ No partial implementations
☐ Debug logging included
☐ POC code marked
```

### Stage 2: Build Validation
After code changes:

```bash
# Must pass before task completion
build_and_test.bat

Checks:
☐ Unity compilation successful
☐ No missing references
☐ No null reference exceptions
☐ All managers initialize
☐ Debug systems functional
```

### Stage 3: Integration Testing
For cross-system changes:

```bash
# Run for complexity ≥ 5
integration_test.bat

Checks:
☐ Manager communication working
☐ Event systems connected
☐ Data flow correct
☐ No circular dependencies
☐ Performance acceptable
```

### Stage 4: Regression Testing  
Before marking complete:

```bash
# Ensures no functionality broken
regression_test.bat

Checks:
☐ Core game loop intact
☐ All cube types functional
☐ Marker systems working
☐ Wave progression stable
☐ UI responsive
```

## Required Validation by Complexity

### Complexity 1-3 (Simple Changes)
- [x] Pre-commit validation
- [x] Build validation
- [ ] Integration testing (optional)
- [ ] Regression testing (optional)

### Complexity 4-6 (Moderate Changes)
- [x] Pre-commit validation
- [x] Build validation
- [x] Integration testing
- [ ] Regression testing (recommended)

### Complexity 7-10 (Complex Changes)
- [x] Pre-commit validation
- [x] Build validation
- [x] Integration testing
- [x] Regression testing
- [x] Performance profiling
- [x] Human review required

## Validation Status Management

### Task Status Flow
```
pending → in_progress → validating → passed/failed → complete

If failed:
- Auto-create fix task (priority 0)
- Set parent task to blocked
- Pause continuous mode
- Document failure reason
```

### Status Tracking
```csharp
// Every task must track
public class TaskValidation
{
    public ValidationStatus Status { get; set; } = ValidationStatus.Pending;
    public List<ValidationResult> Results { get; set; }
    public string FailureReason { get; set; }
    public DateTime LastValidation { get; set; }
}

public enum ValidationStatus
{
    Pending,
    InProgress,
    Validating,
    Passed,
    Failed,
    Blocked
}
```

## Specific Validation Tests

### Manager Validation
Each manager must pass initialization tests:

```csharp
[Test]
public void Manager_Initializes_Successfully()
{
    // Arrange
    GameObject managerObject = new GameObject();
    var manager = managerObject.AddComponent<WaveManager>();
    
    // Act
    manager.Start();
    
    // Assert
    Assert.IsNotNull(manager);
    Assert.IsTrue(manager.IsInitialized);
    Assert.AreEqual(0, manager.ErrorCount);
}
```

### Marker System Validation
```csharp
[Test]
public void FourTierMarkerSystem_Functions_Correctly()
{
    // Test Unit Markers
    TestMarkerPlacement(MarkerType.Light, KeyCode.F);
    TestMarkerTrigger(MarkerType.Light, KeyCode.R);
    
    // Test Recursion Markers
    TestMarkerPlacement(MarkerType.Heavy, KeyCode.V);
    TestMarkerTrigger(MarkerType.Heavy, KeyCode.Y);
    
    // Test Prime markers
    TestMarkerPlacement(MarkerType.Prime, KeyCode.G);
    TestMarkerTrigger(MarkerType.Prime, KeyCode.T);
    
    // Test Cube markers (auto-generated)
    TestCubeMarkerGeneration();
    TestCubeMarkerTrigger(KeyCode.Q);
}
```

### Performance Validation
```csharp
[Test]
public void Performance_Meets_Requirements()
{
    // Arrange
    var stopwatch = new Stopwatch();
    
    // Act
    stopwatch.Start();
    SpawnCubes(100);
    ProcessFrame();
    stopwatch.Stop();
    
    // Assert
    Assert.Less(stopwatch.ElapsedMilliseconds, 16); // 60 FPS
}
```

## Validation Failure Protocols

### Automatic Response to Failure
1. **Log failure** with detailed information
2. **Create fix task** with priority 0
3. **Block parent task** until fixed
4. **Pause continuous mode**
5. **Notify via debug log**

### Fix Task Template
```markdown
# Fix Validation Failure: [Parent Task Name]

## Failure Details
- **Failed Test**: [Test name]
- **Error Message**: [Specific error]
- **Stack Trace**: [If applicable]
- **Time**: [Timestamp]

## Required Fix
[Specific action needed]

## Validation Command
`build_and_test.bat`

## Success Criteria
- Build passes
- Specific test passes
- No regressions
```

## Performance Benchmarks

### Required Performance Metrics
| Metric | Requirement | Test Command |
|--------|------------|--------------|
| Frame Rate | ≥ 60 FPS | `perf_test_fps.bat` |
| Load Time | < 3 seconds | `perf_test_load.bat` |
| Memory Usage | < 2GB | `perf_test_memory.bat` |
| Cube Spawn | < 5ms | `perf_test_spawn.bat` |
| Marker Response | < 100ms | `perf_test_input.bat` |

### Performance Regression Detection
```bash
# Compare against baseline
performance_regression_check.bat

# Outputs:
- Current metrics
- Baseline metrics  
- Deviation percentage
- Pass/Fail status
```

## Documentation Validation

### Required Documentation Updates
When modifying code, update:
- [ ] Code comments for complex logic
- [ ] XML documentation for public APIs
- [ ] README if behavior changes
- [ ] Related .md files if system changes

### Documentation Check
```bash
# Verify documentation current
check_documentation.bat [file]

Validates:
- Comments present for complex methods
- Public APIs documented
- TODOs are tracked
- POC code is marked
```

## Quality Gates

### Definition of Done
A task is only complete when:
1. ✅ All code changes committed
2. ✅ Build validation passed
3. ✅ Integration tests passed (if required)
4. ✅ Regression tests passed (if required)
5. ✅ Performance acceptable
6. ✅ Documentation updated
7. ✅ Approval obtained (if complexity ≥ 7)

### Continuous Mode Gates
Continuous mode stops when:
- Any validation fails
- Performance regression detected
- Documentation missing
- Approval needed

## Validation Tools

### Command Line Tools
```bash
# Full validation suite
validate_all.bat

# Quick validation
quick_validate.bat

# Specific system validation
validate_system.bat [system_name]

# Performance validation
validate_performance.bat

# Documentation validation
validate_docs.bat
```

### Unity Test Runner
```
Window > General > Test Runner

Run:
- EditMode tests (fast)
- PlayMode tests (comprehensive)
- Performance tests (baseline)
```

## Common Validation Issues

### Issue: Build fails after merge
**Solution**: Run `fix_references.bat`, then rebuild

### Issue: Performance regression
**Solution**: Profile with Unity Profiler, optimize hotspots

### Issue: Test timeout
**Solution**: Check for infinite loops, add timeout handling

### Issue: Documentation out of sync
**Solution**: Update immediately, don't accumulate debt

---

**Quick Validation**: `quick_validate.bat` (< 30 seconds)  
**Full Validation**: `validate_all.bat` (~ 5 minutes)  
**Emergency Skip**: Requires written justification + post-review