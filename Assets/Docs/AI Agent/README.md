# AI Agent Documentation System

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Restructured and consolidated  
**Authority Level**: MASTER REFERENCE  
**Review Cycle**: Monthly  
**Enforcement**: MANDATORY - All agents must comply  

---

## Purpose
This documentation system defines the complete operational framework for AI agents working on InfinityQube. It establishes clear boundaries, communication protocols, and quality standards to enable efficient autonomous development while maintaining code integrity.

## Document Hierarchy

### Core Rules (CRITICAL - Override all other instructions)
1. **[01-core-constraints.md](01-core-constraints.md)** - Fundamental behavioral rules that cannot be violated
2. **[02-approval-gates.md](02-approval-gates.md)** - Actions requiring mandatory human approval
3. **[03-file-standards.md](03-file-standards.md)** - File size limits, organization, and structure rules

### Development Standards (MANDATORY - Required patterns)
4. **[04-code-patterns.md](04-code-patterns.md)** - Unity patterns, debug standards, POC marking
5. **[05-safe-zones.md](05-safe-zones.md)** - What can be modified autonomously
6. **[06-validation-rules.md](06-validation-rules.md)** - Build validation, testing, quality gates

### Operational Protocols (OPERATIONAL - Workflow guidance)
7. **[07-context-assembly.md](07-context-assembly.md)** - How to gather and use project context
8. **[08-handoff-protocols.md](08-handoff-protocols.md)** - Agent-to-agent coordination
9. **[09-scope-boundaries.md](09-scope-boundaries.md)** - Agent types, roles, and capabilities

### Task Templates (GUIDANCE - Reusable patterns)
- **[task-templates/](task-templates/)** - Structured approaches for common tasks

## Quick Start for New Agents

### First Time Setup
1. Read **01-core-constraints.md** - Understand what you CANNOT do
2. Read **02-approval-gates.md** - Know when to request human approval
3. Read **09-scope-boundaries.md** - Understand your role and limits

### Before Any Task
1. Check **05-safe-zones.md** - Confirm you can modify the target files
2. Review **03-file-standards.md** - Ensure compliance with size/structure rules
3. Apply **04-code-patterns.md** - Use correct Unity and debug patterns

### During Development
1. Follow **06-validation-rules.md** - Run required validation checks
2. Use **07-context-assembly.md** - Gather appropriate context
3. Apply **task-templates/** - Use proven patterns for common tasks

### Task Handoff
1. Follow **08-handoff-protocols.md** - Proper agent coordination
2. Document per **06-validation-rules.md** - Required documentation
3. Validate per **09-scope-boundaries.md** - Stay within role limits

## Authority Levels Explained

- **CRITICAL**: Violations will break the system or violate core principles
- **MANDATORY**: Required for all agents, no exceptions
- **OPERATIONAL**: Standard operating procedures for efficiency
- **GUIDANCE**: Best practices and recommendations

## Enforcement Mechanisms

### Automatic Checks
- File size validation before commits
- Build validation via `build_and_test.bat`
- Complexity scoring via `score_complexity.bat`

### Manual Gates
- Human approval for complexity ≥ 7
- Human approval for architectural changes
- Human approval for new documentation

### Continuous Mode Stops
- `needs_review = true` on any task
- `validation_status = failed` on any task
- Multiple sequential failures (>3)

## Integration Points

### With Development Tools
- **VS Code/Cursor**: Uses 01-05 for code modification rules
- **Shrimp Task Manager**: Uses 06-09 for task coordination
- **Unity**: Validation per 06, patterns per 04

### With Project Documentation
- References GameDesignDocument.md for project context
- Aligns with TechnicalDebt.md for current priorities
- Follows patterns in existing code architecture

## Maintenance Schedule

### Daily
- Agents self-check against core constraints
- Validation runs on all changes

### Weekly
- Review approval gate triggers
- Update safe zones based on stability

### Monthly
- Full documentation review
- Rule effectiveness assessment
- Update based on common violations

## Emergency Procedures

### When Stuck
1. Document the blocker in task notes
2. Set `needs_approval` flag
3. Generate approval request per template
4. Wait for human intervention

### On Validation Failure
1. Auto-create fix task with priority 0
2. Pause continuous mode
3. Document failure cause
4. Await fix completion

## Version History

- **2.0** (Nov 15, 2024) - Complete restructure and consolidation
- **1.0** (Jul 4, 2025) - Initial implementation (archived)

## Quick Reference Card

```
NEVER DO:
- Split existing manager files
- Create new singletons
- Modify Enumerations.cs
- Change Unity lifecycle
- Create new documentation
- FindObjectOfType in Update()

ALWAYS DO:
- Check file size limits
- Run validation tests
- Use debug logging
- Request approval for complexity ≥ 7
- Follow Unity patterns
- Cache manager references
```

---

**Next Review**: December 15, 2024  
**Feedback**: Report issues in task notes with tag #ai-agent-rules