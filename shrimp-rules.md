# InfinityQube Development Standards (Refined)

## Project Overview
**Project Name**: InfinityQube  
**Type**: Unity 3D Grid-based Tactical Puzzle Game  
**Architecture**: Component-based with Singleton Managers  
**Development Phase**: Functional Prototype with POC Philosophy  

---

## Change Approval Process

### Mandatory Approval Required
Present a concise proposal and wait for explicit approval before implementing:

- **Architectural redesigns** - Changes affecting multiple systems or core patterns
- **Cross-file refactors** - Modifications spanning multiple scripts/modules  
- **Introduction of new subsystems** - New manager classes, systems, or major components

### Proposal Format
```
Problem Statement: [Clear description of issue]
Available Options: [2-3 viable approaches]
Chosen Path: [Selected solution with rationale]
Key Trade-offs: [Benefits vs costs/risks]
```

### Prohibited Without Approval
- NEVER split existing manager files without approval (GridManager.cs, PlayerManager.cs, etc.)
- NEVER introduce new singleton patterns without approval
- NEVER modify core enumerations (Enumerations.cs) without approval
- NEVER change Unity lifecycle patterns without approval

---

## File Organization and Size Limits

### File Size Enforcement (Updated)
- **Core Components**: 600 logical lines max (Tile.cs, GridManager.cs, PlayerManager.cs)
- **Other Managers**: 400 logical lines max 
- **Utility Classes**: 300 logical lines max
- **Exception**: Complex components may exceed if properly regionized and justified

### Current Files Needing Attention
- **Tile.cs** (~700 lines) - Consider extracting Face Painting subsystem
- **GridManager.cs** (~650 lines) - Consider extracting Object Pooling subsystem

### File Size Management Strategy
- **Before proposing splits**: Check if code can be simplified or redundancy removed
- **Splitting strategy**: Extract distinct subsystems while maintaining single responsibility
- **Major changes**: Always provide the entire file contents in updates

---

## Code Update Standards

### Update Granularity Rules (Refined)
- **Minimum update unit**: Complete methods OR method clusters for cohesive functionality
- **Method clusters**: Related methods that work together (e.g., `UpdateTileVisuals()` → `UpdateStateOverlay()` → `DetermineOverlayState()`)
- **Exception**: Simple variable declarations may be updated in isolation

### What Constitutes Valid Updates
**Valid**: Entire method from signature to closing brace  
**Valid**: Method cluster handling cohesive functionality  
**Valid**: Constructor, property getter/setter, event handler  
**Invalid**: Partial method body, incomplete control structures  
**Invalid**: Individual lines within complex methods  

---

## Manager Communication Pattern

### Required Pattern (Simple & Practical)
```csharp
public class ExampleManager : MonoBehaviour 
{
    #region Manager References
    // Cache manager references in Start(), not Update()
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

### Communication Rules
- `FindObjectOfType<>()` in `Start()` is acceptable
- `FindObjectOfType<>()` in `Update()` is prohibited
- Use `ManagerName.Instance` when singleton pattern available
- Cache references and validate in `Start()`

---

## Debug Logging Standard (Enforced)

### Required Debug Configuration
```csharp
[Header("Debug")]
public bool enableDebugLogs = true;

private void DebugLog(string methodName, string message) 
{
    if (enableDebugLogs) 
        Debug.Log($"[{GetType().Name}] {methodName}: {message}");
}

// Usage example:
public void PlaceMarker(int x, int y)
{
    DebugLog("PlaceMarker", $"Placing marker at ({x}, {y})");
    // method implementation...
}
```

### Central Debug Manager (Optional Enhancement)
Consider implementing `DebugLogManager` for global debug control when needed.

### Required Debug Messages
- **Grid operations**: All marker placement/removal, tile state changes
- **Cube interactions**: Capture, destruction, movement events  
- **Player actions**: Movement, death, respawn events
- **Wave progression**: Wave start/end, cube spawning events

---

## POC Code Marking

### POC Identification System
```csharp
// POC: Quick implementation for testing - may need refinement
public void HandleTemporaryFeature()
{
    // Working but not optimized implementation
}

// POC: Placeholder system - replace with proper implementation later
private bool IsTemporaryCheck() => true;
```

### POC Guidelines
- Mark any "quick and dirty" implementations with `// POC:` comment
- POC code should work but doesn't need to be production-quality
- No threshold for upgrading - if it works, it works
- Focus on functionality over perfection in POC phase

---

## Unity Architecture Standards

### Manager Singleton Pattern (Unchanged)
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

### Component Organization (Standardized)
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

## Cross-System Dependencies (Practical Approach)

### Multi-File Coordination Requirements
When modifying systems, consider these common coordination needs:

**Player Systems:**
- PlayerManager.cs + PlayerActionManager.cs often need coordinated updates

**Grid Systems:**
- GridManager.cs changes may affect Tile.cs coordinate handling
- Tile state changes should validate against grid bounds

**Cube Systems:**
- CubeManager.cs changes may require Enumerations.cs updates
- New cube types need corresponding enum additions

**UI Systems:**
- Debug panel changes coordinate with related manager debug flags

### Dependency Validation Approach
- Update systems independently when possible
- Test cross-system integration after related changes
- Use debug logging to trace cross-system communication
- Handle coordination issues as they arise (practical vs. over-engineered)

---

## Examples

### Good Practice - Method Cluster Update
```csharp
// Update entire method cluster for cohesive tile visual functionality
public void UpdateTileVisuals()
{
    UpdateStateOverlay();
}

private void UpdateStateOverlay()
{
    (bool needsOverlay, Color overlayColor) = DetermineOverlayState();
    
    if (needsOverlay)
    {
        CreateOrUpdateOverlay(overlayColor);
    }
    else
    {
        RemoveOverlay();
    }
}

private (bool needsOverlay, Color color) DetermineOverlayState()
{
    if (hasMarker) return (true, markerColor);
    if (isBlackened) return (true, corruptedColor);
    return (false, Color.white);
}
```

### Good Practice - Manager Setup
```csharp
public class WaveManager : MonoBehaviour
{
    #region Manager References
    private GridManager gridManager;
    private PlayerManager playerManager;
    #endregion
    
    private void Start()
    {
        gridManager = GridManager.Instance;
        playerManager = FindObjectOfType<PlayerManager>();
        
        ValidateManagerReferences();
        InitializeWaveSystem();
    }
    
    private void ValidateManagerReferences()
    {
        if (gridManager == null) 
            DebugLog("ValidateManagerReferences", "GridManager not found!");
        if (playerManager == null) 
            DebugLog("ValidateManagerReferences", "PlayerManager not found!");
    }
}
```

### Bad Practice
```csharp
// DON'T: FindObjectOfType in Update
private void Update()
{
    var waveManager = FindObjectOfType<WaveManager>(); // PROHIBITED
}

// DON'T: Partial method update without context
public void PlaceMarker(int x, int y)
{
    if (!IsValidGridPosition(x, y))
        return; // Incomplete - missing implementation and debug logging
}
```

---

## Decision Summary

**File Size**: 600 lines for core components, extract distinct subsystems when needed  
**Updates**: Method clusters acceptable for cohesive functionality  
**Manager Deps**: FindObjectOfType in Start() is sufficient  
**Debug**: Standardized debug flags + `[ManagerName] {method}: Message` format  
**POC Marking**: Use `// POC:` comments for quick implementations  
**Tracking**: No formal technical debt tracking - if it works, it works  

---

**Last Updated**: June 22, 2025  
**Document Version**: 2.0 - Refined for Practical Development  
**Target Audience**: AI Development Agents + Human Developers