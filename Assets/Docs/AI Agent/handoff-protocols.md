# Agent Handoff Protocols

> **Purpose**: Defines how AI agents coordinate work and transfer context between tools  
> **Audience**: AI agents, automation systems, workflow coordinators  
> **Authority**: These protocols ensure smooth transitions and maintain work continuity

---

## **Development Loop Integration**

### **Tool Chain Flow**
```
Claude Desktop → Shrimp Task Manager → VS Code/Cursor → Unity Testing → Loop Back
     ↓               ↓                    ↓              ↓
Strategic        Structured          Implementation    Validation
Planning         Task Design         & Execution       & Testing
```

### **Handoff Points**
1. **Strategic → Structured**: Claude Desktop → Shrimp Task Manager
2. **Structured → Implementation**: Shrimp Task Manager → VS Code/Cursor
3. **Implementation → Validation**: VS Code/Cursor → Unity Testing
4. **Validation → Strategic**: Unity Testing → Claude Desktop (loop completion)

---

## **Handoff 1: Strategic → Structured Planning**

### **From: Claude Desktop (Strategic Agent)**
### **To: Shrimp Task Manager (Task Structuring Agent)**

**Handoff Package Contents**:
```yaml
strategic_analysis:
  - Problem definition and scope
  - Solution approach and rationale
  - Key constraints and considerations
  - Success criteria and validation requirements

context_summary:
  - Relevant project documentation references
  - Technical constraints from core-constraints.md
  - Cross-system dependencies identified
  - Approval requirements (if any)

deliverables:
  - High-level task description
  - Implementation objectives
  - Resource and timeline considerations
```

**Handoff Protocol**:
1. **Claude generates** comprehensive task analysis
2. **Claude documents** strategic context and rationale
3. **Claude creates** handoff summary with key information
4. **Shrimp receives** strategic context for task structuring
5. **Shrimp validates** understanding before proceeding

**Example Handoff Document**:
```markdown
# Strategic Handoff: [Task Name]

## Strategic Analysis
**Problem**: [Clear problem statement]
**Approach**: [Recommended solution approach]
**Constraints**: [Key limitations and requirements]

## Context References
- Project Doc/[relevant-docs]
- Technical Doc/[relevant-docs]
- AI Agent/[relevant-constraints]

## Approval Status
- [X] No approval required - within safe zones
- [ ] Approval required - [specific reason]

## Success Criteria
- [Measurable outcome 1]
- [Measurable outcome 2]
- [Validation method]
```

---

## **Handoff 2: Structured → Implementation**

### **From: Shrimp Task Manager (Task Structuring Agent)**
### **To: VS Code/Cursor (Implementation Agent)**

**Handoff Package Contents**:
```yaml
structured_plan:
  - Detailed task breakdown
  - Implementation steps and sequence
  - File targets and modification scope
  - Dependencies and prerequisites

technical_context:
  - Code patterns and standards to follow
  - Integration points and coordination needs
  - Testing and validation requirements
  - Performance considerations

safety_context:
  - Safe modification zones applicable
  - Approval gates relevant to the task
  - Risk assessment and mitigation
  - Rollback procedures if needed
```

**Handoff Protocol**:
1. **Shrimp creates** detailed implementation plan
2. **Shrimp documents** technical approach and constraints
3. **Shrimp generates** file-specific implementation guidance
4. **VS Code agent receives** structured plan and context
5. **VS Code agent confirms** implementation approach before starting

**Example Handoff Document**:
```markdown
# Implementation Handoff: [Task Name]

## Task Structure
### Primary Objectives
- [Specific implementation goal 1]
- [Specific implementation goal 2]

### Implementation Steps
1. [Step 1 with specific files and methods]
2. [Step 2 with validation requirements]
3. [Step 3 with integration testing]

## Technical Specifications
**Files to Modify**:
- `[file1.cs]` - [specific changes]
- `[file2.cs]` - [specific changes]

**Patterns to Follow**:
- [Coding standard reference]
- [Architecture pattern reference]

## Safety Checks
**Verification Required**:
- [ ] File size within limits
- [ ] Build compilation successful
- [ ] Integration tests pass
- [ ] Performance baseline maintained
```

---

## **Handoff 3: Implementation → Validation**

### **From: VS Code/Cursor (Implementation Agent)**
### **To: Unity Testing (Validation Agent)**

**Handoff Package Contents**:
```yaml
implementation_summary:
  - Changes made and files modified
  - Implementation decisions and rationale
  - Code patterns used and standards followed
  - Potential integration points affected

testing_requirements:
  - Specific test cases to validate
  - Integration scenarios to verify
  - Performance benchmarks to check
  - Edge cases to validate

validation_context:
  - Original task objectives
  - Success criteria from strategic phase
  - Risk areas requiring special attention
  - Rollback procedures if validation fails
```

**Handoff Protocol**:
1. **Implementation agent completes** code changes
2. **Implementation agent documents** what was changed and why
3. **Implementation agent creates** validation checklist
4. **Testing agent receives** implementation summary and test requirements
5. **Testing agent executes** validation and reports results

**Example Handoff Document**:
```markdown
# Validation Handoff: [Task Name]

## Implementation Summary
**Files Modified**:
- `[file1.cs]` - [description of changes]
- `[file2.cs]` - [description of changes]

**Implementation Decisions**:
- [Decision 1 and rationale]
- [Decision 2 and rationale]

## Validation Requirements
**Build Validation**:
- [ ] Clean compilation without warnings
- [ ] No broken references or dependencies
- [ ] File size limits respected

**Functional Validation**:
- [ ] [Specific test case 1]
- [ ] [Specific test case 2]
- [ ] [Integration scenario]

**Performance Validation**:
- [ ] No frame rate degradation
- [ ] Memory usage within bounds
- [ ] Load time impact acceptable

## Risk Areas
- [Area 1 requiring special attention]
- [Area 2 requiring validation]
```

---

## **Handoff 4: Validation → Strategic (Loop Completion)**

### **From: Unity Testing (Validation Agent)**
### **To: Claude Desktop (Strategic Agent)**

**Handoff Package Contents**:
```yaml
validation_results:
  - Test outcomes and validation status
  - Performance impact assessment
  - Integration verification results
  - Any issues discovered during testing

completion_summary:
  - Objectives achieved vs. planned
  - Implementation quality assessment
  - Lessons learned and insights
  - Recommendations for future work

loop_closure:
  - Task completion confirmation
  - Documentation updates needed
  - Next priority identification
  - Continuous improvement insights
```

**Handoff Protocol**:
1. **Testing agent completes** validation process
2. **Testing agent documents** results and findings
3. **Testing agent provides** completion assessment
4. **Strategic agent receives** validation results
5. **Strategic agent updates** project status and identifies next priorities

**Example Handoff Document**:
```markdown
# Loop Completion Handoff: [Task Name]

## Validation Results
**Status**: ✅ PASSED / ❌ FAILED / ⚠️ PARTIAL

**Test Results**:
- Build validation: [PASS/FAIL]
- Functional tests: [PASS/FAIL]
- Performance tests: [PASS/FAIL]
- Integration tests: [PASS/FAIL]

## Completion Assessment
**Objectives Achieved**:
- [✅/❌] [Objective 1]
- [✅/❌] [Objective 2]

**Quality Assessment**:
- Code quality: [Rating/Notes]
- Documentation: [Rating/Notes]
- Testing coverage: [Rating/Notes]

## Next Steps
**Immediate Actions**:
- [Action if issues found]
- [Documentation updates needed]

**Strategic Recommendations**:
- [Insight for future work]
- [Process improvement suggestion]
```

---

## **Cross-Handoff Communication**

### **Persistent Context Maintenance**
- **Task ID tracking** - Maintain consistent task identification across all handoffs
- **Context preservation** - Key information travels through the entire loop
- **Decision audit trail** - Why decisions were made remains visible
- **Progress tracking** - Completion status visible to all agents in the loop

### **Error Handling and Recovery**
```yaml
handoff_failure_protocol:
  1. Agent identifies cannot proceed with received handoff
  2. Agent documents specific issue preventing progress
  3. Agent requests clarification or additional context
  4. Previous agent provides missing information
  5. If unresolvable, escalate to human intervention

rollback_procedures:
  1. Document current state before changes
  2. Maintain rollback checkpoints at each handoff
  3. Clear procedure for undoing changes if validation fails
  4. Communication protocol for rollback decisions
```

### **Quality Gates**
Each handoff includes quality validation:
- **Completeness check** - All required information provided
- **Clarity verification** - Receiving agent understands requirements
- **Context validation** - Relevant constraints and context preserved
- **Success criteria confirmation** - Clear understanding of what constitutes success

---

## **Handoff Templates and Automation**

### **Standardized Handoff Templates**
Located in `AI Agent/task-templates/`:
- `strategic-to-structured-handoff.md`
- `structured-to-implementation-handoff.md`
- `implementation-to-validation-handoff.md`
- `validation-to-strategic-handoff.md`

### **Automation Integration**
```yaml
shrimp_automation:
  - Automatic handoff document generation
  - Context validation checks
  - Template population with task-specific data
  - Handoff status tracking and monitoring

mcp_integration:
  - Context transfer between tool environments
  - File access coordination
  - State synchronization across tools
```

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial implementation  
**Authority Level**: OPERATIONAL - These protocols ensure workflow continuity