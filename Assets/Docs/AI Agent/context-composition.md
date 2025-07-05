# Context Composition Guidelines

> **Purpose**: How AI agents should assemble relevant context for different types of tasks  
> **Audience**: AI agents, automation systems, MCP configurations  
> **Authority**: These guidelines optimize context for effective task execution

---

## **Context Assembly Strategy**

### **Layered Context Approach**
```
Base Layer (Always) → Task Layer (Specific) → Domain Layer (As Needed)
```

**Base Layer**: Core constraints and project fundamentals  
**Task Layer**: Specific context for the current task  
**Domain Layer**: Specialized knowledge for the task domain  

---

## **Base Layer Context (Always Include)**

### **Core AI Constraints**
- `AI Agent/core-constraints.md` - Fundamental behavioral rules
- `AI Agent/approval-gates.md` - What requires human approval
- `AI Agent/safe-modification-zones.md` - What can be modified autonomously

### **Project Fundamentals**
- `Project Doc/GameDesignDocument.md` (summary only) - High-level project understanding
- `Technical Doc/TechnicalDebt.md` (current status) - Known issues and priorities

---

## **Task Layer Context (Task-Specific)**

### **For Code Implementation Tasks**
```yaml
required_context:
  - AI Agent/core-constraints.md
  - Technical Doc/TechnicalDebt.md
  - Current task specification
  - Target files for modification

optional_context:
  - Related manager documentation
  - Cross-system dependency notes
  - Performance requirements
```

### **For Design Implementation Tasks**
```yaml
required_context:
  - AI Agent/core-constraints.md
  - Project Doc/3_GameplayMechanics.md (relevant sections)
  - Project Doc/4_LevelDesign.md (if level-related)
  - Current task specification

optional_context:
  - Project Doc/MDA_Framework.md
  - Project Doc/5_ArtisticArchitecture.md
  - User experience considerations
```

### **For Documentation Tasks**
```yaml
required_context:
  - AI Agent/core-constraints.md
  - Target documentation for update
  - Related project documents
  - Current task specification

optional_context:
  - Cross-reference documents
  - Style guides and formatting standards
  - Documentation hierarchy context
```

---

## **Domain Layer Context (Specialized Knowledge)**

### **Unity/Game Development Domain**
**When Needed**: Code implementation, system architecture, Unity-specific tasks

**Context Sources**:
- Unity architecture patterns from `core-constraints.md`
- Manager communication patterns
- Component organization standards
- Debug logging requirements

### **Game Design Domain**
**When Needed**: Mechanics implementation, level design, user experience tasks

**Context Sources**:
- `Project Doc/3_GameplayMechanics.md` - Core game mechanics
- `Project Doc/4_LevelDesign.md` - Progressive learning and stage design
- `Project Doc/MDA_Framework.md` - Design philosophy
- `Project Doc/2_GameOverview.md` - Core game concept

### **Technical Implementation Domain**
**When Needed**: Performance optimization, technical debt resolution, system integration

**Context Sources**:
- `Technical Doc/TechnicalDebt.md` - Current technical issues
- `Technical Doc/DevelopmentVelocity.md` - Performance patterns
- `Technical Doc/FinalIntegrationTestReport.md` - System validation status
- Code architecture patterns and constraints

---

## **Context Composition by Tool**

### **Claude Desktop Context**
**Purpose**: Strategic planning and high-level design

```yaml
focus: Strategic and architectural thinking
context_limit: High (comprehensive documentation access)
include:
  - All Project Doc/* (design and concepts)
  - Technical Doc/TechnicalDebt.md (current state)
  - AI Agent/core-constraints.md (operational boundaries)
  - Code architecture overview (no implementation details)
exclude:
  - Detailed implementation code
  - Work-in-progress files
  - POC/experimental code
```

### **VS Code MCP Context**
**Purpose**: Implementation and detailed code work

```yaml
focus: Implementation and code modification
context_limit: Medium (targeted documentation + full code access)
include:
  - AI Agent/core-constraints.md (behavioral rules)
  - AI Agent/safe-modification-zones.md (operational boundaries)
  - Target files for modification
  - Related system documentation (minimal)
exclude:
  - Comprehensive design documentation
  - Unrelated project context
  - Historical documentation
```

### **Shrimp Task Manager Context**
**Purpose**: Task structuring and workflow management

```yaml
focus: Task organization and dependency management
context_limit: Medium (task-specific + workflow context)
include:
  - AI Agent/task-templates/* (structured approaches)
  - AI Agent/handoff-protocols.md (workflow coordination)
  - Current milestone and priority context
  - Task dependency information
exclude:
  - Detailed implementation specifics
  - Comprehensive project documentation
  - Code implementation details
```

---

## **Context Optimization Strategies**

### **Just-in-Time Context Loading**
- **Start minimal** - Load only essential context initially
- **Expand as needed** - Request additional context when required
- **Cache key information** - Retain important context across task execution
- **Prune irrelevant data** - Remove context that's no longer needed

### **Context Relevance Filtering**
```yaml
high_relevance:
  - Direct task requirements
  - Safety constraints and approval gates
  - Files being modified
  - Immediate dependencies

medium_relevance:
  - Related system documentation
  - Cross-system integration notes
  - Performance considerations
  - Design rationale

low_relevance:
  - Historical documentation
  - Unrelated project areas
  - Future planning documents
  - Alternative approaches not being pursued
```

### **Dynamic Context Adjustment**
- **Task complexity** - More complex tasks require broader context
- **System impact** - Cross-system tasks need integration context
- **Risk level** - Higher risk tasks need more comprehensive validation context
- **Agent experience** - Less experienced agents need more detailed context

---

## **Context Handoff Protocols**

### **Between Planning and Implementation**
```
Claude Desktop (Strategic) → Shrimp (Structured) → VS Code (Implementation)

Context Flow:
1. Claude: High-level design + constraints → Task plan
2. Shrimp: Task plan + templates → Structured implementation guide
3. VS Code: Implementation guide + code access → Working implementation
```

### **Context Preservation**
- **Task continuity** - Maintain context across tool transitions
- **Decision rationale** - Preserve why decisions were made
- **Constraint awareness** - Ensure constraints are communicated forward
- **Progress tracking** - Maintain awareness of what's been completed

### **Context Validation**
- **Consistency checks** - Ensure context aligns across tools
- **Completeness verification** - Confirm all necessary context is available
- **Relevance confirmation** - Validate context matches current task needs

---

## **Common Context Composition Patterns**

### **Feature Implementation Pattern**
```yaml
base_context:
  - AI Agent/core-constraints.md
  - AI Agent/safe-modification-zones.md

task_context:
  - Feature specification
  - Target implementation files
  - Related system documentation

domain_context:
  - Project Doc/3_GameplayMechanics.md (relevant sections)
  - Unity patterns and standards
  - Cross-system integration requirements
```

### **Bug Fix Pattern**
```yaml
base_context:
  - AI Agent/core-constraints.md
  - Technical Doc/TechnicalDebt.md

task_context:
  - Bug description and reproduction steps
  - Affected files and systems
  - Test cases and validation requirements

domain_context:
  - System architecture context
  - Debug logging standards
  - Integration test requirements
```

### **Refactoring Pattern**
```yaml
base_context:
  - AI Agent/core-constraints.md
  - AI Agent/approval-gates.md (file size and architectural limits)

task_context:
  - Current code structure
  - Refactoring objectives
  - Target architecture

domain_context:
  - Code organization standards
  - Performance requirements
  - Testing and validation approaches
```

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial implementation  
**Authority Level**: GUIDANCE - These patterns optimize agent effectiveness