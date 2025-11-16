# Handoff Protocols

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Clear handoff procedures  
**Authority Level**: OPERATIONAL - Standard procedures  
**Review Cycle**: Quarterly  
**Enforcement**: Process validation  

---

## Purpose
Establishes clear protocols for transferring work between different AI agents, tools, and human developers, ensuring continuity, context preservation, and quality maintenance throughout the development workflow.

## Agent Handoff Flow

### Standard Workflow
```
Strategic Planning (Claude Desktop)
    ↓ [Problem Analysis + Solution Design]
Task Structuring (Shrimp)
    ↓ [Detailed Specifications + Dependencies]
Implementation (VS Code/Cursor)
    ↓ [Code Changes + Testing]
Validation (Unity + Tests)
    ↓ [Quality Assurance]
Human Review (If Required)
```

## Handoff Package Requirements

### From Strategic Planning → Task Structuring

#### Required Deliverables
```markdown
## Strategic Analysis Package

### Problem Definition
- Clear problem statement
- Current system state
- Desired end state
- Success criteria

### Solution Approach
- Chosen solution path
- Alternatives considered
- Key trade-offs
- Risk assessment

### Technical Specification
- Systems affected
- Dependencies identified
- Complexity estimate
- Integration points

### Context Summary
- Relevant documentation
- Prior art/patterns
- Constraints applicable
- Approval requirements
```

#### Quality Checklist
- [ ] Problem clearly defined
- [ ] Solution approach justified
- [ ] Complexity assessed
- [ ] Dependencies identified
- [ ] Success criteria measurable

### From Task Structuring → Implementation

#### Required Deliverables
```markdown
## Implementation Specification Package

### Task Details
- Task ID: [UUID]
- Task Name: [Descriptive]
- Complexity: [1-10]
- Priority: [0-3]

### Implementation Guide
1. [Step-by-step instructions]
2. [Specific code locations]
3. [Patterns to follow]
4. [Testing requirements]

### File Modifications
- Target files: [List]
- Safe zone status: [Green/Yellow/Red]
- Size constraints: [Lines limit]
- Change scope: [Specific methods/regions]

### Validation Requirements
- Build must pass
- Tests to run: [List]
- Performance baseline: [Metrics]
- Documentation updates: [Required sections]
```

#### Quality Checklist
- [ ] Steps are actionable
- [ ] File targets identified
- [ ] Patterns specified
- [ ] Tests defined
- [ ] Success measurable

### From Implementation → Validation

#### Required Deliverables
```markdown
## Validation Package

### Changes Made
- Files modified: [List with line ranges]
- Methods added/changed: [List]
- Tests added/updated: [List]
- Documentation updated: [Sections]

### Build Status
- Compilation: [Pass/Fail]
- Warnings: [Count and types]
- Errors: [None expected]

### Test Results
- Unit tests: [Pass/Fail count]
- Integration tests: [Pass/Fail count]
- Performance tests: [Metrics]

### Validation Commands
```bash
build_and_test.bat
validate_system.bat [system]
performance_check.bat
```

### Known Issues
- [Any issues discovered]
- [Workarounds applied]
- [Future improvements]
```

#### Quality Checklist
- [ ] All changes documented
- [ ] Build passes
- [ ] Tests pass
- [ ] Performance acceptable
- [ ] Documentation current

## Cross-Tool Handoff Protocols

### Shrimp → VS Code
```yaml
Handoff Method: Task specification in Shrimp
Context Transfer:
  - Task details via Shrimp UI
  - File paths via task description
  - Context via AI Agent rules reference
Validation: Implementation matches specification
```

### VS Code → Unity
```yaml
Handoff Method: File save + Unity refresh
Context Transfer:
  - Code changes via file system
  - Build validation via Unity console
  - Test results via Test Runner
Validation: Unity compiles without errors
```

### Unity → Shrimp
```yaml
Handoff Method: Validation status update
Context Transfer:
  - Test results via status flag
  - Error details via task notes
  - Metrics via performance logs
Validation: Task marked complete/failed
```

## Human Handoff Scenarios

### When to Escalate to Human

#### Immediate Escalation
- Build breaks that block all progress
- Data loss risk
- Security concerns
- License violations

#### Standard Escalation
- Complexity score ≥ 7
- Architectural decisions needed
- New pattern introduction
- Cross-team dependencies

#### Optional Escalation
- Performance optimization opportunities
- Code quality improvements
- Documentation enhancements
- Future feature considerations

### Human Handoff Package
```markdown
## Human Review Request

### Summary
[One paragraph description]

### Urgency
☐ Critical - Blocking all work
☐ High - Blocking current task
☐ Medium - Need guidance
☐ Low - Information only

### Context
- Current task: [Description]
- Issue encountered: [Specific problem]
- Attempted solutions: [What was tried]
- Recommendation: [Suggested path]

### Decision Required
☐ Approve/Reject proposed solution
☐ Choose between alternatives
☐ Provide missing information
☐ Override constraint

### Supporting Materials
- Relevant code: [File:lines]
- Documentation: [Links]
- Error messages: [If applicable]
- Screenshots: [If helpful]
```

## Handoff Quality Standards

### Completeness Criteria
Every handoff must include:
1. Clear description of work done/needed
2. Specific success criteria
3. Current status/state
4. Next steps identified
5. Context preserved

### Clarity Standards
- Use precise language
- Include specific file paths
- Provide line numbers
- Give exact error messages
- State explicit expectations

### Continuity Requirements
- No information lost between handoffs
- Context accessible to next agent
- Progress trackable
- Rollback possible

## Failed Handoff Recovery

### Detection Signs
- Next agent cannot proceed
- Missing critical information
- Conflicting instructions
- Broken dependencies
- Lost context

### Recovery Protocol
1. **Stop work** immediately
2. **Document issue** in task notes
3. **Identify gap** in handoff
4. **Request clarification** from previous agent
5. **Update handoff** package
6. **Resume work** with complete context

### Prevention Measures
- Use handoff templates
- Validate before passing
- Test instructions
- Maintain audit trail
- Regular reviews

## Asynchronous Handoffs

### Delayed Handoff Protocol
When work will be picked up later:

```markdown
## Async Handoff Package

### Work Completed
- [Specific accomplishments]
- [Current state]

### Work Remaining  
- [Specific tasks]
- [Priority order]

### Context Preservation
- Key decisions: [List with rationale]
- Assumptions made: [List]
- Open questions: [List]

### Pickup Instructions
1. Read this summary
2. Check current state
3. Validate assumptions
4. Continue from: [Specific point]

### Time Sensitivity
- Valid until: [Date/condition]
- Revalidation needed if: [Conditions]
```

## Handoff Metrics

### Quality Metrics
- Handoff success rate
- Rework required
- Information gaps
- Time to understand

### Efficiency Metrics  
- Handoff preparation time
- Context transfer size
- Tool switching overhead
- End-to-end time

### Improvement Tracking
- Common failure points
- Template effectiveness
- Tool integration issues
- Process bottlenecks

---

**Handoff Template**: `get_handoff_template.bat [type]`  
**Validate Handoff**: `validate_handoff.bat [package]`  
**Recovery Protocol**: `recover_handoff.bat [task_id]`