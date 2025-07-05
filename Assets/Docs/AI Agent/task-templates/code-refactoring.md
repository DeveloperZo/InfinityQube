# Code Refactoring Task Template

> **Purpose**: Template for structuring code refactoring and cleanup tasks  
> **Usage**: Apply this template when organizing code improvement work  
> **Authority**: Follow this structure to ensure consistent refactoring approaches

---

## **Task Structure Template**

### **Task Identification**
```yaml
task_type: code_refactoring
complexity_estimate: [1-10]
priority: [Low/Medium/High/Critical]
approval_required: [Yes/No - based on scope]
```

### **Refactoring Scope**
```yaml
target_files:
  - path: [file_path]
    current_size: [line_count]
    target_size: [desired_line_count]
    refactoring_type: [extraction/optimization/cleanup/standardization]

affected_systems:
  - [system_name]: [impact_description]
  
dependencies:
  - [prerequisite_task_or_condition]
```

---

## **Implementation Specification**

### **Refactoring Objectives**
- **Primary Goal**: [Main purpose of refactoring - size reduction, clarity, performance, etc.]
- **Secondary Goals**: [Additional benefits expected]
- **Success Metrics**: [How to measure successful completion]

### **Refactoring Strategy**
1. **Analysis Phase**:
   - Identify refactoring candidates
   - Analyze dependencies and impacts
   - Plan extraction or optimization approach

2. **Implementation Phase**:
   - Apply refactoring patterns
   - Maintain functionality and interfaces
   - Update documentation and comments

3. **Validation Phase**:
   - Verify functionality preservation
   - Test integration points
   - Validate performance impact

### **Specific Refactoring Actions**
```yaml
extractions:
  - source_method: [method_name]
    target_class: [new_class_name]
    justification: [why this extraction makes sense]

optimizations:
  - target_area: [code_section]
    optimization_type: [performance/memory/readability]
    expected_improvement: [quantified benefit]

cleanups:
  - cleanup_type: [obsolete_code/unused_variables/redundant_methods]
    target_items: [specific_items_to_clean]
    safety_verification: [how_to_ensure_safe_removal]
```

---

## **Safety and Validation Requirements**

### **Pre-Refactoring Checklist**
- [ ] **File size verification** - Confirm target files are within scope for modification
- [ ] **Dependency analysis** - Identify all code that depends on refactoring targets
- [ ] **Test coverage verification** - Ensure adequate testing for refactored code
- [ ] **Backup strategy** - Plan for rollback if refactoring causes issues

### **During Refactoring**
- [ ] **Incremental validation** - Test after each major refactoring step
- [ ] **Interface preservation** - Maintain public interfaces and contracts
- [ ] **Documentation updates** - Keep documentation synchronized with changes
- [ ] **Code standard compliance** - Follow established coding patterns

### **Post-Refactoring Validation**
- [ ] **Functionality verification** - All original functionality preserved
- [ ] **Integration testing** - Cross-system interactions still work
- [ ] **Performance validation** - No significant performance degradation
- [ ] **Build verification** - Clean compilation without warnings

---

## **Risk Assessment and Mitigation**

### **Common Refactoring Risks**
```yaml
breaking_changes:
  risk: Modifying public interfaces
  mitigation: Preserve all public interfaces during refactoring
  
performance_degradation:
  risk: Refactoring negatively impacts performance
  mitigation: Performance benchmarking before and after changes
  
integration_failures:
  risk: Breaking connections between systems
  mitigation: Comprehensive integration testing
  
complexity_increase:
  risk: Refactoring makes code harder to understand
  mitigation: Code review and simplicity validation
```

### **Approval Gate Triggers**
- **File splitting required** → Human approval mandatory
- **Cross-system impacts** → Human approval mandatory
- **New patterns introduction** → Human approval mandatory
- **Complexity score ≥ 7** → Human approval mandatory

---

## **Implementation Guidelines**

### **Code Extraction Patterns**
```csharp
// Example: Method cluster extraction
// BEFORE: Large method in Manager class
public class PlayerManager : MonoBehaviour 
{
    // 50+ lines of marker-related functionality mixed with other concerns
}

// AFTER: Extracted to specialized class
public class PlayerMarkerHandler 
{
    // Focused marker functionality
    private PlayerManager playerManager; // Reference back to manager
}
```

### **Optimization Patterns**
```csharp
// Example: Performance optimization
// BEFORE: Inefficient lookup pattern
private void Update() 
{
    var manager = FindObjectOfType<SomeManager>(); // Inefficient
}

// AFTER: Cached reference pattern
private SomeManager manager;
private void Start() 
{
    manager = FindObjectOfType<SomeManager>(); // Cache in Start
}
```

### **Cleanup Patterns**
```csharp
// Example: Obsolete code removal
// BEFORE: Obsolete methods with attributes
[System.Obsolete("Use NewMethod instead")]
public void OldMethod() => NewMethod();

// AFTER: Clean implementation
// (OldMethod removed after validation period)
```

---

## **Documentation Requirements**

### **Implementation Documentation**
- **Refactoring rationale** - Why these changes were made
- **Pattern explanations** - How new patterns work and why they were chosen
- **Integration notes** - How refactored code integrates with existing systems
- **Performance impact** - Measured impact on system performance

### **Code Documentation Updates**
- **Method documentation** - Update XML documentation for refactored methods
- **Class documentation** - Update class-level documentation for extracted classes
- **Architecture notes** - Update system architecture documentation if needed

---

## **Example Application**

### **Scenario**: Tile.cs File Size Reduction
```yaml
task_identification:
  task_type: code_refactoring
  complexity_estimate: 6
  priority: Medium
  approval_required: Yes # (requires file extraction)

refactoring_scope:
  target_files:
    - path: Assets/scripts/Core/Tile.cs
      current_size: 700
      target_size: 500
      refactoring_type: extraction
      
  extraction_target: Face Painting subsystem

implementation_plan:
  1. Create TileFacePainting.cs class
  2. Extract face painting methods and state
  3. Maintain interface through Tile.cs delegation
  4. Update cross-references in CubeManager.cs
  5. Validate integration and performance

success_criteria:
  - Tile.cs under 600 lines (within safe zone)
  - All face painting functionality preserved
  - No performance degradation
  - Clean integration with CubeManager
```

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial template  
**Usage**: Apply this template for all code refactoring tasks