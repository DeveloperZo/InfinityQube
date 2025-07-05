# Agent Scope Definitions

> **Purpose**: Defines different AI agent types, their capabilities, and operational boundaries  
> **Audience**: AI agents, automation systems, workflow coordinators  
> **Authority**: These definitions establish clear roles and prevent scope conflicts

---

## **Agent Type Hierarchy**

### **Primary Agents (Core Development Loop)**
1. **Strategic Planning Agent** (Claude Desktop)
2. **Task Structuring Agent** (Shrimp Task Manager)
3. **Implementation Agent** (VS Code/Cursor)
4. **Validation Agent** (Unity + Testing)

### **Specialized Agents (Domain-Specific)**
1. **Documentation Agent** (Markdown and content management)
2. **Code Quality Agent** (Refactoring and cleanup)
3. **Design Implementation Agent** (Game mechanics and balance)
4. **Audio Integration Agent** (Sound system implementation)

---

## **Strategic Planning Agent**

### **Primary Role**
High-level project planning, architecture decisions, and strategic direction

### **Capabilities**
- **Project-wide analysis** - Full access to all project documentation
- **Architectural planning** - System design and integration strategies
- **Milestone planning** - Long-term development roadmap
- **Problem decomposition** - Breaking complex issues into manageable tasks
- **Resource allocation** - Balancing priorities and development effort

### **Scope Boundaries**
```yaml
allowed:
  - Read all project documentation
  - Analyze system architecture without code details
  - Create high-level task plans
  - Identify approval requirements
  - Strategic decision recommendations

prohibited:
  - Direct code modification
  - Detailed implementation decisions
  - File structure changes
  - Immediate execution of plans
```

### **Context Access**
- **Full documentation access** - All Project Doc/* and Technical Doc/*
- **Architecture overview** - System relationships without implementation details
- **Constraint awareness** - Core constraints and approval gates
- **Historical context** - Past decisions and evolution patterns

### **Handoff Responsibilities**
- Generate comprehensive task analysis
- Identify constraints and approval requirements
- Provide strategic context for implementation
- Define success criteria and validation approaches

---

## **Task Structuring Agent**

### **Primary Role**
Transform strategic plans into detailed, executable task specifications

### **Capabilities**
- **Task decomposition** - Breaking strategic plans into specific work items
- **Dependency mapping** - Identifying task relationships and sequencing
- **Resource estimation** - Complexity scoring and effort assessment
- **Template application** - Using predefined task patterns
- **Workflow coordination** - Managing task handoffs and status tracking

### **Scope Boundaries**
```yaml
allowed:
  - Create detailed task specifications
  - Apply task templates and patterns
  - Manage task dependencies and sequencing
  - Coordinate workflow between agents
  - Track progress and status

prohibited:
  - Strategic decision making
  - Direct code implementation
  - Architectural changes
  - Approval gate modifications
```

### **Context Access**
- **Strategic context** - Plans and analysis from Strategic Planning Agent
- **Task templates** - Predefined patterns for common work types
- **Constraint context** - Approval gates and safe modification zones
- **Project structure** - File organization and system boundaries

### **Handoff Responsibilities**
- Translate strategic plans into actionable tasks
- Provide detailed implementation guidance
- Establish validation and testing requirements
- Coordinate between planning and implementation phases

---

## **Implementation Agent**

### **Primary Role**
Execute detailed code changes and implement specific functionality

### **Capabilities**
- **Code modification** - Direct file editing and implementation
- **Pattern application** - Following established coding standards
- **Integration work** - Connecting new code with existing systems
- **Local testing** - Basic validation during implementation
- **Documentation updates** - Code comments and implementation notes

### **Scope Boundaries**
```yaml
allowed:
  - Modify files within safe modification zones
  - Implement specific methods and functionality
  - Apply established coding patterns
  - Update code documentation and comments
  - Perform local build validation

prohibited:
  - Architectural decisions
  - File structure changes requiring approval
  - Cross-system modifications without approval
  - New pattern creation without approval
```

### **Context Access**
- **Implementation guidance** - Detailed specifications from Task Structuring Agent
- **Code access** - Full read/write access to target files
- **Pattern references** - Coding standards and architecture patterns
- **Safety constraints** - File size limits and modification boundaries

### **Handoff Responsibilities**
- Execute implementation according to specifications
- Document implementation decisions and changes
- Prepare validation requirements for testing
- Provide implementation summary for validation phase

---

## **Validation Agent**

### **Primary Role**
Verify implementation quality, integration, and performance

### **Capabilities**
- **Build validation** - Compilation and build process verification
- **Integration testing** - Cross-system functionality validation
- **Performance testing** - Performance impact assessment
- **Quality assessment** - Code quality and standard compliance
- **Documentation validation** - Ensure documentation accuracy and completeness

### **Scope Boundaries**
```yaml
allowed:
  - Execute all testing and validation procedures
  - Run Unity builds and integration tests
  - Performance profiling and assessment
  - Quality metric collection
  - Validation report generation

prohibited:
  - Code modification (except test fixes)
  - Implementation decisions
  - Architectural changes
  - Strategic planning modifications
```

### **Context Access**
- **Implementation details** - What was changed and how
- **Testing requirements** - Specific validation needs from task specification
- **Quality standards** - Performance and quality benchmarks
- **Original objectives** - Success criteria from strategic phase

### **Handoff Responsibilities**
- Execute comprehensive validation process
- Document validation results and findings
- Provide completion assessment
- Recommend next steps or issue resolution

---

## **Documentation Agent**

### **Primary Role**
Specialized agent for documentation creation, updates, and maintenance

### **Capabilities**
- **Content creation** - New documentation writing and structuring
- **Content updates** - Improving existing documentation clarity and accuracy
- **Cross-referencing** - Maintaining links and relationships between documents
- **Format standardization** - Ensuring consistent formatting and structure
- **Content organization** - Managing documentation hierarchy and navigation

### **Scope Boundaries**
```yaml
allowed:
  - Update existing documentation content
  - Improve formatting and organization
  - Create cross-references and navigation
  - Standardize documentation structure
  - Content clarification and correction

prohibited:
  - Creating new document categories without approval
  - Changing documentation hierarchy without approval
  - Modifying constraint documents without approval
  - Strategic content decisions
```

---

## **Code Quality Agent**

### **Primary Role**
Specialized agent for code refactoring, cleanup, and quality improvements

### **Capabilities**
- **Refactoring** - Code organization and structure improvements
- **Cleanup** - Removing obsolete code and technical debt
- **Optimization** - Performance and efficiency improvements
- **Standardization** - Applying coding standards and patterns consistently
- **Debt resolution** - Addressing items from technical debt tracking

### **Scope Boundaries**
```yaml
allowed:
  - Refactor within safe modification zones
  - Remove approved obsolete code
  - Apply standard coding patterns
  - Optimize performance within existing architecture
  - Clean up technical debt items

prohibited:
  - Architectural changes requiring approval
  - New pattern introduction
  - File splitting without approval
  - Cross-system refactoring without approval
```

---

## **Design Implementation Agent**

### **Primary Role**
Specialized agent for implementing game design and mechanics

### **Capabilities**
- **Mechanics implementation** - Translating design specifications into code
- **Balance tuning** - Adjusting game parameters and configurations
- **User experience** - Implementing UI/UX improvements
- **Content integration** - Adding new game content and features
- **Design validation** - Ensuring implementation matches design intent

### **Scope Boundaries**
```yaml
allowed:
  - Implement specified game mechanics
  - Adjust balance parameters within bounds
  - Update UI elements and interactions
  - Add content within established systems
  - Validate design implementation accuracy

prohibited:
  - Design decision making
  - Fundamental mechanic changes without approval
  - New system architecture
  - User experience paradigm changes
```

---

## **Audio Integration Agent**

### **Primary Role**
Specialized agent for audio system implementation and integration

### **Capabilities**
- **Audio asset integration** - Incorporating sound files and music
- **Audio system implementation** - Building audio playback and management
- **Audio balance tuning** - Volume levels and audio mixing
- **Performance optimization** - Audio system efficiency
- **Platform compatibility** - Ensuring audio works across target platforms

### **Scope Boundaries**
```yaml
allowed:
  - Implement audio playback systems
  - Integrate audio assets and content
  - Tune audio levels and mixing
  - Optimize audio performance
  - Handle audio platform requirements

prohibited:
  - Audio design decisions
  - Fundamental audio architecture changes
  - New audio system paradigms
  - Cross-system modifications affecting audio
```

---

## **Agent Coordination Rules**

### **Scope Conflict Resolution**
1. **Primary agents** take precedence in their domain
2. **Specialized agents** defer to primary agents for architectural decisions
3. **Overlapping work** requires explicit coordination and approval
4. **Scope boundaries** are enforced through approval gates

### **Agent Communication Protocols**
- **Handoff procedures** - Formal transfer of work between agents
- **Status updates** - Regular communication of progress and blockers
- **Conflict notification** - Immediate escalation of scope conflicts
- **Collaboration patterns** - Guidelines for multi-agent coordination

### **Quality Assurance**
- **Scope validation** - Regular verification that agents stay within boundaries
- **Work quality assessment** - Evaluation of agent output quality
- **Continuous improvement** - Regular refinement of scope definitions
- **Training and updates** - Keeping agent knowledge current

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial implementation  
**Authority Level**: ORGANIZATIONAL - These definitions establish clear operational roles