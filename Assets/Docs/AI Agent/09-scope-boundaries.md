# Scope Boundaries

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Clear role definitions  
**Authority Level**: ORGANIZATIONAL - Defines agent roles  
**Review Cycle**: Quarterly  
**Enforcement**: Tool configuration  

---

## Purpose
Defines the specific roles, capabilities, and boundaries for different types of AI agents, ensuring each operates within their optimal scope while preventing overreach and maintaining system integrity.

## Agent Type Definitions

## 🎯 Strategic Planning Agent
**Tools**: Claude Desktop, ChatGPT, High-context AI
**Context Window**: 100K+ tokens
**Primary Role**: Analysis, design, and planning

### Responsibilities
- Problem analysis and definition
- Solution architecture design
- Complexity assessment
- Risk evaluation
- Approach recommendation
- Context synthesis

### Capabilities
- Read entire documentation sets
- Analyze system-wide patterns
- Propose architectural changes
- Evaluate trade-offs
- Create implementation strategies

### Boundaries
#### ✅ CAN DO
- Design solutions
- Analyze problems
- Create specifications
- Evaluate alternatives
- Assess complexity
- Recommend approaches

#### ❌ CANNOT DO
- Modify code directly
- Make implementation decisions
- Execute tests
- Deploy changes
- Access file system
- Run commands

### Deliverables
- Problem analysis documents
- Solution proposals
- Complexity assessments
- Risk evaluations
- Context summaries

## 🔧 Task Structuring Agent
**Tools**: Shrimp Task Manager
**Context Window**: 16K tokens
**Primary Role**: Task decomposition and coordination

### Responsibilities
- Convert strategies into tasks
- Define dependencies
- Assign complexity scores
- Set priorities
- Coordinate handoffs
- Track progress

### Capabilities
- Create detailed task specifications
- Manage task dependencies
- Apply task templates
- Calculate complexity
- Coordinate workflow
- Monitor velocity

### Boundaries
#### ✅ CAN DO
- Create tasks
- Update task status
- Set dependencies
- Apply templates
- Track progress
- Coordinate handoffs

#### ❌ CANNOT DO
- Modify code
- Make architectural decisions
- Override complexity limits
- Skip validation
- Bypass approval gates
- Change core systems

### Deliverables
- Task specifications
- Dependency graphs
- Complexity scores
- Progress reports
- Handoff packages

## 💻 Implementation Agent
**Tools**: VS Code, Cursor, IDE-integrated AI
**Context Window**: 8-32K tokens
**Primary Role**: Code modification and testing

### Responsibilities
- Implement specifications
- Write tests
- Fix bugs
- Optimize code
- Update documentation
- Validate changes

### Capabilities
- Modify code files
- Create new methods
- Write unit tests
- Refactor code
- Add debug logging
- Update comments

### Boundaries
#### ✅ CAN DO
- Modify safe zone files
- Add methods to managers
- Create POC implementations
- Write tests
- Fix bugs
- Add documentation

#### ❌ CANNOT DO
- Change architecture
- Modify enumerations
- Create new managers
- Split files
- Change lifecycles
- Skip validation

### Deliverables
- Code changes
- Test implementations
- Bug fixes
- Documentation updates
- Validation results

## ✅ Validation Agent
**Tools**: Unity, Test Runners, CI/CD
**Context Window**: Variable
**Primary Role**: Quality assurance and validation

### Responsibilities
- Run build validation
- Execute tests
- Check performance
- Verify integration
- Validate requirements
- Report results

### Capabilities
- Compile code
- Run test suites
- Profile performance
- Check integration
- Validate behavior
- Generate reports

### Boundaries
#### ✅ CAN DO
- Run validations
- Execute tests
- Profile performance
- Check compliance
- Report issues
- Verify requirements

#### ❌ CANNOT DO
- Modify code
- Skip tests
- Override failures
- Ignore requirements
- Bypass gates
- Change criteria

### Deliverables
- Test results
- Build status
- Performance metrics
- Validation reports
- Issue logs

## Role Interaction Matrix

| From → To | Strategic | Task | Implementation | Validation |
|-----------|-----------|------|----------------|------------|
| **Strategic** | - | Problem & Solution | - | - |
| **Task** | Clarification | - | Specification | - |
| **Implementation** | - | Status Update | - | Changes |
| **Validation** | - | Results | Issues | - |

## Capability Boundaries

### Code Modification Rights
```
Strategic Agent:     ❌ No code access
Task Agent:          ❌ No code access  
Implementation Agent: ✅ Within safe zones
Validation Agent:     ❌ Read-only access
Human Developer:      ✅ Full access
```

### Decision Authority
```
Strategic Agent:     Recommend only
Task Agent:          Tactical decisions
Implementation Agent: Technical choices
Validation Agent:     Pass/fail determination
Human Developer:      Final authority
```

### Approval Rights
```
Strategic Agent:     Cannot approve
Task Agent:          Cannot approve
Implementation Agent: Cannot approve
Validation Agent:     Cannot approve
Human Developer:      Can approve all
```

## Escalation Boundaries

### When Agents Must Escalate

#### Strategic Agent Escalates When
- Multiple valid solutions exist
- Trade-offs require business decision
- Risk exceeds acceptable levels
- Requirements unclear

#### Task Agent Escalates When
- Complexity ≥ 7
- Dependencies circular
- Resources insufficient
- Timeline unrealistic

#### Implementation Agent Escalates When
- Architecture change needed
- File split required
- Pattern unclear
- Tests failing mysteriously

#### Validation Agent Escalates When
- Build repeatedly fails
- Performance degrades
- Integration breaks
- Requirements not met

## Boundary Enforcement

### Automatic Enforcement
- Tool permissions
- File access controls
- Command restrictions
- API limitations

### Process Enforcement
- Handoff requirements
- Validation gates
- Approval workflows
- Audit trails

### Human Enforcement
- Review cycles
- Approval gates
- Override controls
- Exception handling

## Boundary Violations

### Detection Methods
- Automated scanning
- Validation failures
- Audit log analysis
- Pattern recognition

### Response Protocol
1. **Detect** violation
2. **Stop** work immediately
3. **Document** violation type
4. **Rollback** if necessary
5. **Report** to human
6. **Adjust** boundaries if needed

### Common Violations
| Violation | Agent Type | Response |
|-----------|------------|----------|
| Direct code change | Strategic | Block & educate |
| Architecture modify | Implementation | Rollback & escalate |
| Skip validation | Task | Force validation |
| Override complexity | Implementation | Require approval |

## Evolution Protocol

### Boundary Adjustments
Boundaries can be adjusted when:
- Capabilities proven
- Trust established  
- Tools improve
- Needs change

### Expansion Process
1. Identify limitation
2. Propose expansion
3. Test in sandbox
4. Limited trial
5. Full rollout

### Contraction Process
1. Identify risk/issue
2. Document problems
3. Restrict immediately
4. Review systematically
5. Adjust permanently

## Quick Reference

### By Agent Type

#### Strategic (Claude Desktop)
```
Focus: WHY and WHAT
Output: Designs and plans
Cannot: Touch code
```

#### Task (Shrimp)
```
Focus: WHEN and WHO
Output: Tasks and coordination
Cannot: Modify files
```

#### Implementation (VS Code)
```
Focus: HOW
Output: Code and tests
Cannot: Change architecture
```

#### Validation (Unity)
```
Focus: CORRECT
Output: Results and metrics
Cannot: Fix problems
```

---

**Boundary Check**: `check_boundaries.bat [agent_type] [action]`  
**Violation Report**: `report_violation.bat [type] [details]`  
**Expansion Request**: `request_boundary_change.bat [agent] [capability]`