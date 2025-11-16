# Context Assembly

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Tool-specific context guidelines  
**Authority Level**: GUIDANCE - Best practices for efficiency  
**Review Cycle**: Monthly  
**Enforcement**: Self-managed by agents  

---

## Purpose
Defines how AI agents should gather, prioritize, and utilize project context to make informed decisions, ensuring efficient use of context windows and maintaining awareness of project state.

## Context Priority Hierarchy

### 🔴 Critical Context (Always Include)
Must be in context for any operation:

1. **Current Task Specification**
   - Task description
   - Acceptance criteria  
   - Dependencies
   - Complexity score

2. **Target File(s)**
   - Complete file if modifying
   - Interface/headers if referencing
   - Related test files

3. **Applicable Rules**
   - Relevant constraints from 01-core-constraints.md
   - Applicable approval gates from 02-approval-gates.md
   - Safe zone status from 05-safe-zones.md

### 🟡 Important Context (Include When Relevant)
Include based on task type:

1. **Related Systems**
   - Interacting managers
   - Dependent components
   - Shared data structures

2. **Project State**
   - Current development phase
   - Recent changes
   - Known issues

3. **Design Documentation**
   - Relevant sections from GameDesignDocument.md
   - Architecture patterns
   - System interactions

### 🟢 Supplementary Context (Optional)
Include if space permits:

1. **Historical Information**
   - Previous similar tasks
   - Lessons learned
   - Common patterns

2. **Future Considerations**
   - Planned features
   - Technical debt
   - Optimization opportunities

## Tool-Specific Context Assembly

### Claude Desktop (Strategic Planning)
**Context Window**: ~100K tokens
**Strategy**: Comprehensive understanding

```yaml
Required Context:
- Full AI Agent documentation set
- Complete Project Doc folder
- GameDesignDocument.md
- Current milestone/roadmap
- Technical debt status

Optional Context:
- Implementation files (reference only)
- Previous task outcomes
- Performance metrics
```

### VS Code/Cursor (Implementation)
**Context Window**: ~8-32K tokens  
**Strategy**: Focused on specific changes

```yaml
Required Context:
- Target file(s) complete
- Related test files
- Applicable AI Agent rules (01-05)
- Direct dependencies

Optional Context:
- Manager interfaces
- Relevant documentation sections
- Recent changes to area
```

### Shrimp Task Manager (Task Structuring)
**Context Window**: ~16K tokens
**Strategy**: Task coordination focus

```yaml
Required Context:
- Current task tree/dependencies
- Task templates
- Handoff protocols
- Milestone priorities

Optional Context:
- Recent task completions
- Velocity metrics
- Resource availability
```

## Context Gathering Strategies

### For Bug Fixes
```
1. Bug report/description
2. Affected file(s) complete
3. Related test files
4. Recent changes to area
5. Similar past fixes
```

### For Feature Implementation
```
1. Feature specification
2. Design documentation
3. Target integration points
4. Existing patterns to follow
5. Test requirements
```

### For Refactoring
```
1. Current implementation complete
2. Refactoring goals
3. Dependent systems
4. Performance baselines
5. Test coverage
```

### For Optimization
```
1. Performance metrics
2. Profiler results
3. Target code complete
4. Algorithm alternatives
5. Benchmark requirements
```

## Context Quality Rules

### DO Include
- ✅ Complete files when modifying
- ✅ Full method signatures when referencing
- ✅ Actual error messages
- ✅ Specific line numbers
- ✅ Clear success criteria

### DON'T Include
- ❌ Entire codebase dumps
- ❌ Irrelevant documentation
- ❌ Outdated information
- ❌ Speculative designs
- ❌ Redundant files

## Context Assembly Templates

### Standard Modification Task
```markdown
## Task Context

### Target File
[Complete file content]

### Modification Goal
[Specific change required]

### Related Systems
[List of interacting components]

### Applicable Rules
- File size limit: [X lines]
- Safe zone status: [Green/Yellow/Red]
- Complexity score: [1-10]

### Success Criteria
[How to verify success]
```

### Cross-System Integration
```markdown
## Integration Context

### Systems Involved
1. [System A - role]
2. [System B - role]

### Integration Points
[Specific interfaces/events]

### Current Communication Pattern
[How they currently interact]

### Required Changes
[What needs modification]

### Impact Analysis
[What else might be affected]
```

## Dynamic Context Adjustment

### When to Expand Context
- Unexpected dependencies discovered
- Compilation errors need resolution
- Test failures need investigation
- Performance issues need profiling

### When to Reduce Context
- Simple, localized changes
- Well-understood patterns
- Independent components
- Clear specifications

### Context Refresh Triggers
- Task completion
- Significant time elapsed (>1 hour)
- Context window approaching limit
- Switching between systems

## Context Validation

### Before Starting Task
Verify you have:
- [ ] Task specification
- [ ] Target file(s)
- [ ] Applicable rules
- [ ] Success criteria
- [ ] Test requirements

### During Task Execution
Maintain awareness of:
- [ ] Current changes made
- [ ] Tests affected
- [ ] Dependencies touched
- [ ] Documentation needed

### After Task Completion
Ensure context includes:
- [ ] Final implementation
- [ ] Test results
- [ ] Validation status
- [ ] Documentation updates

## Memory Management

### Context Window Optimization
```
Priority 1: Current task (20% of window)
Priority 2: Target files (40% of window)
Priority 3: Rules/constraints (10% of window)
Priority 4: Related systems (20% of window)
Priority 5: Documentation (10% of window)
```

### Context Pruning Strategy
When approaching limits:
1. Remove historical information
2. Summarize instead of full content
3. Keep only interfaces, not implementations
4. Focus on current task only

## Context Debugging

### Common Context Issues

#### Missing Dependencies
**Symptom**: Compilation errors
**Solution**: Add complete dependency files

#### Outdated Information
**Symptom**: Conflicts with current state
**Solution**: Refresh from source files

#### Insufficient Context
**Symptom**: Unclear requirements
**Solution**: Request clarification, add documentation

#### Context Overflow
**Symptom**: Truncated responses
**Solution**: Prune non-essential context

## Best Practices

### For Efficiency
1. **Cache common patterns** in memory
2. **Build mental models** of system architecture
3. **Reference documentation** by section
4. **Summarize verbose content**
5. **Focus on interfaces** over implementations

### For Accuracy
1. **Verify file versions** are current
2. **Confirm rule applicability**
3. **Check recent changes**
4. **Validate assumptions**
5. **Test understanding** with small changes first

### For Collaboration
1. **Document context used** for decisions
2. **Share relevant findings**
3. **Update documentation** when learning
4. **Report context gaps**
5. **Suggest improvements** to templates

---

**Context Check**: `validate_context.bat [task_id]`  
**Context Template**: `get_context_template.bat [task_type]`  
**Context Optimizer**: `optimize_context.bat [current_size]`