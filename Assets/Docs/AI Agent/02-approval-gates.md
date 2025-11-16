# Approval Gates

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Consolidated and clarified  
**Authority Level**: MANDATORY - Cannot be bypassed  
**Review Cycle**: Weekly (high-frequency touchpoint)  
**Enforcement**: AUTOMATIC complexity scoring + manual review  

---

## Purpose
Defines specific actions and conditions that require explicit human approval before AI agents can proceed. These gates protect system integrity while enabling autonomous work within safe boundaries.

## Automatic Approval Triggers

### Complexity-Based Gates
After every task planning:
1. **Run** `score_complexity.bat`
2. **Assign** complexity score (1-10)
3. **Check** threshold triggers:
   - Score ≥ 7 → `needs_review = true`
   - Priority = Critical → `needs_review = true`
   - Cross-system impact → `needs_review = true`

### Complexity Scoring Guide
```
1-3: Single file, localized, well-understood patterns
4-6: Multiple files, moderate complexity, established patterns  
7-8: Cross-system changes, new patterns, coordination required
9-10: Architectural impact, multiple systems, high risk
```

### Validation Failure Gates
- Build failure → Automatic stop
- Test failure → Automatic stop  
- Performance regression → Automatic stop
- Multiple failures (>3) → Escalation required

## Mandatory Approval Categories

### 🏗️ Architectural Changes
**Requires Approval**:
- System redesigns affecting multiple components
- New manager classes or subsystems
- Core pattern modifications
- Singleton pattern additions
- Event system changes

**Approval Not Required**:
- Internal method refactoring
- Local optimizations
- POC implementations
- Debug additions

### 📁 File Structure Changes  
**Requires Approval**:
- Splitting existing files (any manager or core component)
- Creating new manager files
- Directory restructuring
- New file creation for core systems

**Approval Not Required**:
- Creating test files
- Adding debug utilities
- Creating data files (ScriptableObjects)
- Adding prefabs or assets

### ⚙️ Core System Modifications
**Requires Approval**:
- Enumerations.cs modifications
- Unity lifecycle changes
- Manager communication pattern changes
- Core data structure modifications

**Approval Not Required**:
- Adding new methods to existing managers
- Extending existing enums (with caution)
- Adding debug features
- Performance optimizations

### 📝 Documentation Changes
**Requires Approval**:
- Creating new documentation files
- Restructuring documentation
- Changing documentation standards
- Removing documentation

**Approval Not Required**:
- Updating existing documentation
- Adding code comments
- Updating timestamps
- Fixing typos or clarity

## Approval Request Template

```markdown
# Approval Request: [Task Title]

## Summary
[One-line description of what needs approval]

## Complexity Score: [X/10]

## Category
☐ Architectural Change
☐ File Structure Change  
☐ Core System Modification
☐ Documentation Change
☐ Other: [specify]

## Problem Statement
[Clear description of why this change is needed]

## Proposed Solution
[Detailed description of what will be changed]

## Alternatives Considered
1. [Alternative 1 - why rejected]
2. [Alternative 2 - why rejected]

## Impact Analysis
- **Files Affected**: [list]
- **Systems Impacted**: [list]  
- **Breaking Changes**: [yes/no - details]
- **Performance Impact**: [assessment]

## Risk Assessment
- **Risk Level**: [Low/Medium/High]
- **Mitigation**: [how risks are addressed]
- **Rollback Plan**: [how to undo if needed]

## Testing Plan
[How changes will be validated]

## Timeline
- **Implementation**: [estimated time]
- **Testing**: [estimated time]
- **Total**: [total time]
```

## Fast-Track Approvals

### Pre-Approved Patterns
These patterns are pre-approved if within constraints:
- Adding debug logging to existing methods
- Creating POC implementations (marked appropriately)
- Optimizing existing code without changing interfaces
- Adding validation checks
- Improving error handling

### Conditional Approvals
These have automatic approval IF conditions met:
- File splitting IF under size limit → Auto-approve
- New methods IF no interface change → Auto-approve  
- Refactoring IF same public API → Auto-approve
- Bug fixes IF no architecture change → Auto-approve

## Approval Workflow

### Standard Flow
```
1. Agent detects approval needed
2. Agent sets needs_review = true
3. Agent generates approval request
4. Human reviews request
5. Human provides decision:
   - APPROVED → Agent proceeds
   - MODIFIED → Agent adjusts and resubmits
   - DENIED → Agent documents and moves on
```

### Emergency Flow
```
1. Critical issue detected
2. Agent documents emergency
3. Agent implements minimal fix
4. Agent marks as "emergency-fix"
5. Human reviews post-implementation
```

## Continuous Mode Rules

### Automatic Stops
Continuous mode STOPS when:
- Next task has `needs_review = true`
- Any task has `validation_status = failed`  
- Complexity score ≥ 7 detected
- Approval gate triggered

### Resume Conditions
Continuous mode RESUMES when:
- Human runs `approve <TASK_ID>`
- Failed validation is fixed
- Human provides explicit override
- All gates cleared

## Approval Metrics

### Tracking Requirements
- Log all approval requests
- Track approval/denial rates
- Monitor common triggers
- Identify pattern opportunities

### Review Frequency
- **Daily**: Review pending approvals
- **Weekly**: Analyze approval patterns
- **Monthly**: Update pre-approved list

## Common Scenarios Guide

### Scenario: Need to fix a bug across multiple files
- Complexity: Usually 4-6
- Approval: Not required if no architecture change
- Action: Proceed with validation

### Scenario: Need to add new cube type
- Complexity: 7-8
- Approval: REQUIRED (Enumerations.cs change)
- Action: Submit approval request

### Scenario: Optimizing manager performance
- Complexity: 3-5
- Approval: Not required if API unchanged
- Action: Proceed with POC marking

### Scenario: Creating new debug panel
- Complexity: 5-6
- Approval: Not required if follows patterns
- Action: Proceed with validation

---

**Quick Approval**: Use `quick_approve.bat` for complexity < 4  
**Standard Approval**: Use approval template for complexity ≥ 4  
**Emergency Override**: Requires written justification + post-review