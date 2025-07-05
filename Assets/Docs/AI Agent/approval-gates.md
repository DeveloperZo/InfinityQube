# Approval Gates

> **Purpose**: Defines what requires human approval before AI agents can proceed  
> **Audience**: AI agents, automation systems  
> **Authority**: These gates are MANDATORY - agents must stop and request approval

---

## **Mandatory Human Approval Required**

### **Architectural Changes**
- **System redesigns** affecting multiple components
- **New manager classes** or singleton patterns
- **Core pattern modifications** (Unity lifecycle, event systems)
- **Cross-cutting concerns** that span multiple files or systems

### **File Structure Changes**
- **File splitting** of any existing manager files:
  - GridManager.cs
  - PlayerManager.cs
  - Tile.cs
  - CubeManager.cs
  - Any other manager or core component
- **New file creation** for managers or core systems
- **Directory structure changes** affecting project organization

### **Core System Modifications**
- **Enumerations.cs changes** - Adding, removing, or modifying enums
- **Unity lifecycle pattern changes** - Awake, Start, Update, OnDestroy sequences
- **Singleton pattern modifications** - Changing Instance management
- **Manager communication patterns** - How managers find and reference each other

### **Documentation Changes**
- **New documentation creation** - Any new .md files
- **Documentation structure changes** - Moving or reorganizing docs
- **Major documentation updates** - Significant rewrites or restructuring

---

## **Complexity-Based Approval Gates**

### **Automatic Complexity Scoring**
After every plan creation, agents must:
1. Run **score_complexity.bat**
2. Assign `complexity = 1-10` based on:
   - Number of files affected
   - System interdependencies
   - Potential for breaking changes
   - Learning curve for future developers

### **Approval Triggers**
- `complexity ≥ 7` → **Human approval required**
- `priority = Critical` → **Human approval required**
- Cross-system dependencies → **Human approval required**

### **Complexity Scoring Guidelines**
```
1-3: Single file, localized changes, well-understood patterns
4-6: Multiple files, moderate complexity, established patterns
7-8: Cross-system changes, new patterns, significant coordination
9-10: Architectural impact, multiple systems, high coordination risk
```

---

## **Validation-Based Approval Gates**

### **Build Validation Requirements**
- All tasks start with `validation_status = pending`
- **build_and_test.bat** must run successfully
- If validation fails → automatic child task "Fix build for [original task]"
- Failed validation tasks **pause continuous mode**

### **Continuous Mode Pause Triggers**
Shrimp must **stop continuous mode** when:
1. Next ready task has `needs_review = true`
2. Any task has `validation_status = failed`
3. Multiple sequential build failures (>3 in a row)

### **Resume Conditions**
Continuous mode can only resume when:
- Human runs **approve <TASK_ID>** (clears `needs_review`), OR
- Failed validation task is fixed and marked done, OR
- Human manually overrides pause condition

---

## **Approval Request Format**

When requesting approval, agents must provide:

```markdown
# Approval Request: [Task Title]

## Problem Statement
[Clear description of the issue requiring resolution]

## Available Options
1. **Option A**: [Description, pros, cons]
2. **Option B**: [Description, pros, cons]  
3. **Option C**: [Description, pros, cons]

## Recommended Solution
**Chosen Path**: [Selected option]
**Rationale**: [Why this option was selected]

## Key Trade-offs
**Benefits**: [What this solution provides]
**Costs**: [What this solution costs/risks]
**Alternatives Rejected**: [Why other options weren't chosen]

## Impact Assessment
**Files Affected**: [List of files that will change]
**Systems Impacted**: [List of systems that will be affected]
**Testing Requirements**: [What needs to be validated]
**Rollback Plan**: [How to undo if problems arise]

## Complexity Score
**Score**: [1-10]
**Justification**: [Why this complexity level]
```

---

## **Emergency Procedures**

### **When Agents Get Stuck**
If an agent encounters an approval gate but cannot continue:
1. **Document the blocker** in task notes
2. **Set task status** to `needs_approval`
3. **Pause continuous mode**
4. **Generate approval request** using the required format
5. **Wait for human intervention**

### **Escalation Path**
1. **Agent identifies approval need** → Generates request
2. **Human reviews request** → Approves, modifies, or rejects
3. **If approved** → Agent proceeds with specified constraints
4. **If modified** → Agent updates plan and re-requests approval
5. **If rejected** → Agent marks task as blocked and moves to next task

---

## **Approval Tracking**

### **Required Documentation**
- **Approval requests** logged in task notes
- **Human responses** recorded with timestamp
- **Modifications** to original plan documented
- **Final implementation** compared against approved plan

### **Audit Trail**
- All approval gates must maintain audit trail
- Decisions and rationale preserved for future reference
- Pattern recognition for improving future approval processes

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial implementation  
**Authority Level**: MANDATORY - These gates cannot be bypassed