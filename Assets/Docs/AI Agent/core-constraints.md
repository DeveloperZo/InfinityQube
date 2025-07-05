# AI Agent Core Constraints

> **Purpose**: Fundamental behavioral rules that ALL AI agents must follow when working on InfinityQube  
> **Audience**: AI agents, automation systems, development tools  
> **Authority**: These constraints override all other instructions

---

## **Critical Safety Rules**

### **Approval Gates - NEVER Proceed Without Human Approval**
- **Architectural redesigns** - Changes affecting multiple systems or core patterns
- **Cross-file refactors** - Modifications spanning multiple scripts/modules  
- **Introduction of new subsystems** - New manager classes, systems, or major components
- **File splitting** - NEVER split existing manager files (GridManager.cs, PlayerManager.cs, etc.)
- **Singleton patterns** - NEVER introduce new singleton patterns
- **Core enumerations** - NEVER modify Enumerations.cs
- **Unity lifecycle patterns** - NEVER change Unity lifecycle patterns
- **Documentation creation** - NEVER create new documentation without approval

### **Required Approval Format**
When you need approval, present:
```
Problem Statement: [Clear description of issue]
Available Options: [2-3 viable approaches]
Chosen Path: [Selected solution with rationale]
Key Trade-offs: [Benefits vs costs/risks]
```

---

## **Autonomous CPAR Gates**

### **Complexity Auto-Scoring**
- After every plan, run **score_complexity.bat**
- Write `complexity = 1-10`
- If `complexity ≥ 7` **OR** `priority = Critical` → set `needs_review = true`

### **Validation Workflow**
- For each work-task **T** mark `validation_status = pending`
- `build_and_test.bat` shall:
  - Set `validation_status = passed` if Unity build + tests succeed
  - Set `validation_status = failed` and open child task "Fix build for T" (priority 0) if not

### **Loop-Pause Rules**
Shrimp must **stop continuous mode** when:
1. The next ready task has `needs_review = true`, **OR**
2. Any task has `validation_status = failed`

Resuming requires either:
- Human runs **approve <TASK_ID>** (clears `needs_review`), **OR**
- The failed validation task is fixed and marked done

---

## **File Size Enforcement**

### **Strict Limits**
- **Core Components**: 600 logical lines max (Tile.cs, GridManager.cs, PlayerManager.cs)
- **Other Managers**: 400 logical lines max 
- **Utility Classes**: 300 logical lines max
- **Exception**: Complex components may exceed if properly regionized and justified

### **Files Currently Over Limit**
- **Tile.cs** (~700 lines) - Consider extracting Face Painting subsystem
- **GridManager.cs** (~650 lines) - Consider extracting Object Pooling subsystem

### **Size Management Strategy**
- **Before proposing splits**: Check if code can be simplified or redundancy removed
- **Splitting strategy**: Extract distinct subsystems while maintaining single responsibility
- **Major changes**: Always provide the entire file contents in updates

---

## **Code Update Rules**

### **Valid Update Units**
- **Complete methods** - From signature to closing brace
- **Method clusters** - Related methods that work together cohesively
- **Constructors, properties, event handlers** - Complete units
- **Simple declarations** - Variables, fields (exception case)

### **Invalid Updates**
- **Partial method bodies** - Incomplete implementations
- **Incomplete control structures** - Half-written if/for/while blocks
- **Individual lines within complex methods** - Context-breaking changes

---

## **Manager Communication Pattern**

### **Required Pattern**
```csharp
public class ExampleManager : MonoBehaviour 
{
    #region Manager References
    private WaveManager waveManager;
    private GridManager gridManager;
    #endregion
    
    private void Start() 
    {
        // FindObjectOfType in Start() is acceptable
        waveManager = FindObjectOfType<WaveManager>();
        gridManager = GridManager.Instance; // Use Instance when available
        
        ValidateReferences();
    }
    
    private void ValidateReferences()
    {
        if (waveManager == null) 
            DebugLog("ValidateReferences", "WaveManager not found - some features limited");
    }
}
```

### **Communication Rules**
- `FindObjectOfType<>()` in `Start()` is **acceptable**
- `FindObjectOfType<>()` in `Update()` is **prohibited**
- Use `ManagerName.Instance` when singleton pattern available
- Cache references and validate in `Start()`

---

## **Debug Logging Standard**

### **Required Implementation**
```csharp
[Header("Debug")]
public bool enableDebugLogs = true;

private void DebugLog(string methodName, string message) 
{
    if (enableDebugLogs) 
        Debug.Log($"[{GetType().Name}] {methodName}: {message}");
}
```

### **Required Debug Messages**
- **Grid operations**: All marker placement/removal, tile state changes
- **Cube interactions**: Capture, destruction, movement events  
- **Player actions**: Movement, death, respawn events
- **Wave progression**: Wave start/end, cube spawning events

---

## **POC Code Guidelines**

### **Marking System**
```csharp
// POC: Quick implementation for testing - may need refinement
public void HandleTemporaryFeature()
{
    // Working but not optimized implementation
}
```

### **POC Philosophy**
- Mark "quick and dirty" implementations with `// POC:` comment
- POC code should work but doesn't need to be production-quality
- No threshold for upgrading - if it works, it works
- Focus on functionality over perfection in POC phase

---

## **Unity Architecture Standards**

### **Singleton Pattern**
```csharp
public class [ManagerName] : MonoBehaviour
{
    public static [ManagerName] Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple [ManagerName] found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
}
```

### **Component Organization**
```csharp
public class ExampleManager : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Setup")]
    [SerializeField] private GameObject prefab;
    #endregion
    
    #region Manager References  
    private WaveManager waveManager;
    #endregion
    
    #region Runtime State  
    private bool isInitialized = false;
    #endregion
    
    #region Properties
    public static ExampleManager Instance { get; private set; }
    #endregion
    
    #region Unity Lifecycle
    private void Awake() { /* singleton setup */ }
    private void Start() { /* manager references & initialization */ }
    private void OnDestroy() { /* cleanup */ }
    #endregion
    
    #region Debug
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private void DebugLog(string methodName, string message) 
    {
        if (enableDebugLogs) 
            Debug.Log($"[{GetType().Name}] {methodName}: {message}");
    }
    #endregion
}
```

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Extracted from shrimp-rules.md  
**Authority Level**: CRITICAL - These constraints override all other instructions