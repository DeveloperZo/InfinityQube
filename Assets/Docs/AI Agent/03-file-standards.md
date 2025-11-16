# File Standards

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Clarified limits and organization  
**Authority Level**: MANDATORY - Required for code quality  
**Review Cycle**: Monthly  
**Enforcement**: AUTOMATIC via pre-commit hooks  

---

## Purpose
Establishes file size limits, organization patterns, and structural requirements to maintain code readability, maintainability, and consistent architecture across the InfinityQube project.

## File Size Limits

### Hard Limits by Category
| Category | Max Lines | Current Violations | Action Required |
|----------|-----------|-------------------|-----------------|
| **Core Components** | 600 | Tile.cs (~700) | Extract subsystem |
| **Manager Classes** | 400 | GridManager.cs (~650) | Extract subsystem |
| **Utility Classes** | 300 | None | Maintain |
| **Data Classes** | 200 | None | Maintain |
| **Interfaces** | 100 | None | Maintain |

### Measurement Rules
- **Logical lines** = Non-blank, non-comment lines
- **Regions** don't count toward limit
- **Auto-generated** code excluded
- **Unity callbacks** count as single unit

### Size Management Strategy

#### Before Proposing Split
1. **Remove redundancy** - Eliminate duplicate code
2. **Extract utilities** - Move helpers to utility classes
3. **Simplify logic** - Reduce complexity
4. **Region organization** - Improve structure

#### When Splitting Required
1. **Identify subsystems** - Find distinct responsibilities
2. **Maintain cohesion** - Keep related code together
3. **Preserve interfaces** - Don't break public API
4. **Document split** - Explain in approval request

## File Organization Standards

### Required File Structure
```csharp
// Copyright and file header comments

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Other using statements in alphabetical order

namespace InfinityQube // Optional namespace
{
    /// <summary>
    /// XML documentation for class
    /// </summary>
    public class ClassName : MonoBehaviour
    {
        #region Constants
        private const float DEFAULT_VALUE = 1.0f;
        #endregion

        #region Inspector Configuration  
        [Header("Setup")]
        [SerializeField] private GameObject prefab;
        
        [Header("Settings")]
        [Range(0, 1)] public float setting;
        #endregion
        
        #region Manager References
        private WaveManager waveManager;
        private GridManager gridManager;
        #endregion
        
        #region Runtime State
        private bool isInitialized;
        private List<GameObject> activeObjects;
        #endregion
        
        #region Properties
        public static ClassName Instance { get; private set; }
        public bool IsReady => isInitialized && activeObjects != null;
        #endregion
        
        #region Unity Lifecycle
        private void Awake() { }
        private void Start() { }
        private void Update() { }
        private void OnDestroy() { }
        #endregion
        
        #region Public API
        public void PublicMethod() { }
        #endregion
        
        #region Private Methods  
        private void PrivateMethod() { }
        #endregion
        
        #region Event Handlers
        private void OnEventTriggered() { }
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
}
```

### Region Requirements

#### Mandatory Regions (in order)
1. **Constants** - Const and readonly fields
2. **Inspector Configuration** - SerializeField variables
3. **Manager References** - References to other managers
4. **Runtime State** - Private state variables
5. **Properties** - Public and private properties
6. **Unity Lifecycle** - Awake, Start, Update, etc.
7. **Debug** - Debug flags and methods

#### Optional Regions
- **Public API** - Public methods
- **Private Methods** - Internal implementation
- **Event Handlers** - Event response methods
- **Nested Types** - Inner classes/structs
- **Editor Only** - Code in UNITY_EDITOR

## Naming Conventions

### Files and Folders
```
Scripts/
├── Managers/           # Manager classes
│   ├── WaveManager.cs
│   └── GridManager.cs
├── Components/         # MonoBehaviour components  
│   ├── Tile.cs
│   └── CubeManager.cs
├── Data/              # Data classes and ScriptableObjects
│   ├── WaveData.cs
│   └── StageData.cs
├── UI/                # UI-specific scripts
├── Utils/             # Utility and helper classes
└── Interfaces/        # Interface definitions
```

### Class Names
- **Managers**: [Function]Manager (e.g., WaveManager)
- **Components**: Descriptive noun (e.g., Tile, Cube)
- **Data**: [Type]Data (e.g., WaveData)
- **Utilities**: [Function]Utils or [Function]Helper
- **Interfaces**: I[Capability] (e.g., IDebugInterface)

### Variable Names
```csharp
// Inspector variables
[SerializeField] private GameObject targetPrefab;

// Public properties  
public bool IsActive { get; private set; }

// Private fields
private float currentSpeed;
private bool isInitialized;

// Constants
private const float MAX_SPEED = 10f;
public static readonly Vector3 DEFAULT_POSITION = Vector3.zero;

// Collections
private List<GameObject> activeEnemies;
private Dictionary<int, Tile> tileMap;
```

## File Update Rules

### Complete Unit Requirement
When modifying files, changes must be complete units:

#### ✅ Valid Updates
- Entire methods from signature to closing brace
- Complete properties including all accessors
- Full event handler implementations
- Method clusters for cohesive functionality

#### ❌ Invalid Updates  
- Partial method implementations
- Incomplete control structures
- Single lines within methods
- Partial property definitions

### Update Size Guidelines
- **Small**: < 50 lines (direct update)
- **Medium**: 50-200 lines (section update)
- **Large**: > 200 lines (complete file provision)

## File Creation Rules

### When Creating New Files

#### Requires Approval
- New manager classes
- Core system components
- Architectural elements
- Documentation files

#### Auto-Approved
- Test files
- Debug utilities
- Data assets (ScriptableObjects)
- Prefab configurations

### New File Template
Every new code file must include:
```csharp
// InfinityQube - [Component Description]
// Created: [Date]
// Purpose: [Brief description]

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Detailed description of class purpose and usage]
/// </summary>
public class ClassName : MonoBehaviour
{
    // Implementation following standard structure
}
```

## Quality Checks

### Pre-Commit Validation
- File size within limits
- Required regions present
- Naming conventions followed
- Debug pattern implemented

### Build Validation
- No compilation errors
- No missing references
- Debug logs functional
- Performance acceptable

## Common Issues and Solutions

### Issue: File exceeding size limit
**Solution**: Extract cohesive subsystem to new file (with approval)

### Issue: Missing required regions
**Solution**: Reorganize following template structure

### Issue: Inconsistent naming
**Solution**: Refactor to match conventions

### Issue: Incomplete updates
**Solution**: Provide complete functional units

---

**Validation Tool**: `check_file_standards.bat`  
**Auto-Format**: `format_file_structure.bat`  
**Size Check**: `check_file_sizes.bat`