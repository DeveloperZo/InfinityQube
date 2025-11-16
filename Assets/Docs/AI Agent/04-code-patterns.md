# Code Patterns

---
**Last Updated**: November 15, 2024  
**Document Version**: 2.0 - Unity-specific patterns consolidated  
**Authority Level**: MANDATORY - Required patterns  
**Review Cycle**: Quarterly  
**Enforcement**: Code review + build validation  

---

## Purpose
Defines required code patterns, Unity-specific standards, and debug requirements that ensure consistency, maintainability, and debuggability across the InfinityQube codebase.

## Unity Architecture Patterns

### Singleton Pattern (When Approved)
```csharp
public class ManagerName : MonoBehaviour
{
    #region Properties
    public static ManagerName Instance { get; private set; }
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Multiple {GetType().Name} found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Other Awake initialization
        InitializeComponents();
    }
    
    private void OnDestroy()
    {
        // Clean up singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
}
```

### Manager Reference Pattern
```csharp
public class ExampleManager : MonoBehaviour
{
    #region Manager References
    private WaveManager waveManager;
    private GridManager gridManager;
    private AudioManager audioManager;
    #endregion
    
    #region Unity Lifecycle
    private void Start()
    {
        // Method 1: Singleton reference (preferred when available)
        gridManager = GridManager.Instance;
        
        // Method 2: FindObjectOfType (acceptable in Start)
        waveManager = FindObjectOfType<WaveManager>();
        
        // Method 3: Direct assignment via Inspector
        // audioManager assigned in Inspector
        
        // Always validate references
        ValidateReferences();
    }
    
    private void ValidateReferences()
    {
        if (waveManager == null)
            DebugLog("ValidateReferences", "WaveManager not found - features limited");
        
        if (gridManager == null)
            Debug.LogError($"[{GetType().Name}] GridManager required but not found!");
    }
    #endregion
}
```

### Component Communication Pattern
```csharp
// Use UnityEvents for loose coupling
public class CubeManager : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<CubeType> OnCubeCaptured;
    public UnityEvent<Vector3> OnCubeEscaped;
    
    private void CaptureCube(CubeType type, Vector3 position)
    {
        // Process capture
        ProcessCaptureLogic(type);
        
        // Notify listeners
        OnCubeCaptured?.Invoke(type);
        
        // Direct manager notification when needed
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCubeCaptureSound(type, position);
    }
}
```

## Debug Pattern (REQUIRED)

### Standard Debug Implementation
Every manager and major component MUST implement:

```csharp
public class ComponentName : MonoBehaviour
{
    #region Debug
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showDebugGizmos = false;
    
    private void DebugLog(string methodName, string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[{GetType().Name}] {methodName}: {message}");
    }
    
    private void DebugWarning(string methodName, string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[{GetType().Name}] {methodName}: {message}");
    }
    
    private void DebugError(string methodName, string message)
    {
        // Errors always log regardless of flag
        Debug.LogError($"[{GetType().Name}] {methodName}: {message}");
    }
    
    // Optional: Visual debugging
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw debug visuals
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
    #endregion
}
```

### Required Debug Messages
These operations MUST include debug logging:
- State changes (initialization, shutdown)
- Resource allocation/deallocation  
- Manager reference validation
- Critical operations (spawn, capture, destroy)
- Error conditions
- Performance warnings

## POC (Proof of Concept) Pattern

### POC Marking Convention
```csharp
// POC: Quick implementation for testing cube spawning
// TODO: Optimize with object pooling when proven necessary
public GameObject SpawnCube(CubeType type, Vector3 position)
{
    // Simple instantiation for POC
    GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
    
    // POC: Direct configuration - could be data-driven later
    cube.GetComponent<CubeManager>().Initialize(type);
    
    return cube;
}

// POC: Temporary validation - replace with proper system
private bool ValidateSpawnPosition(Vector3 position)
{
    // Quick bounds check for POC
    return position.x >= 0 && position.x < gridWidth &&
           position.z >= 0 && position.z < gridHeight;
}
```

### POC Guidelines
- **Mark clearly** with `// POC:` comment
- **Explain limitation** in comment
- **Keep functional** - must work even if not optimal
- **No upgrade requirement** - working POC is acceptable
- **Document assumptions** for future improvement

## Object Pooling Pattern

### When to Use
- Frequently spawned/destroyed objects (cubes, effects)
- Performance-critical scenarios
- Mobile or low-end target platforms

### Basic Pool Implementation
```csharp
public class ObjectPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new Queue<T>();
    private T prefab;
    private Transform parent;
    
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        
        // Pre-populate pool
        for (int i = 0; i < initialSize; i++)
        {
            T obj = GameObject.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public T Get()
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : GameObject.Instantiate(prefab, parent);
        obj.gameObject.SetActive(true);
        return obj;
    }
    
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

## Coroutine Patterns

### Standard Coroutine Structure
```csharp
private Coroutine activeCoroutine;

public void StartProcess()
{
    // Stop existing coroutine if running
    if (activeCoroutine != null)
    {
        StopCoroutine(activeCoroutine);
    }
    
    activeCoroutine = StartCoroutine(ProcessCoroutine());
}

private IEnumerator ProcessCoroutine()
{
    DebugLog("ProcessCoroutine", "Starting process");
    
    // Initialization
    yield return null; // Wait one frame
    
    // Main loop
    while (isProcessing)
    {
        // Process step
        ProcessStep();
        
        // Wait for interval
        yield return new WaitForSeconds(processInterval);
    }
    
    // Cleanup
    DebugLog("ProcessCoroutine", "Process complete");
    activeCoroutine = null;
}

private void OnDestroy()
{
    // Always stop coroutines on destroy
    if (activeCoroutine != null)
    {
        StopCoroutine(activeCoroutine);
    }
}
```

## Event Pattern

### UnityEvent Usage
```csharp
[System.Serializable]
public class CubeEvent : UnityEvent<CubeType, Vector3> { }

public class EventManager : MonoBehaviour  
{
    [Header("Events")]
    public CubeEvent OnCubeSpawned;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent OnGameOver;
    
    public void TriggerCubeSpawn(CubeType type, Vector3 position)
    {
        DebugLog("TriggerCubeSpawn", $"Spawning {type} at {position}");
        OnCubeSpawned?.Invoke(type, position);
    }
}
```

## Input Handling Pattern

### Input Manager Pattern
```csharp
public class InputManager : MonoBehaviour
{
    #region Input Configuration
    [Header("Movement Keys")]
    [SerializeField] private KeyCode moveUp = KeyCode.W;
    [SerializeField] private KeyCode moveDown = KeyCode.S;
    [SerializeField] private KeyCode moveLeft = KeyCode.A;
    [SerializeField] private KeyCode moveRight = KeyCode.D;
    
    [Header("Action Keys")]
    [SerializeField] private KeyCode placeMarker = KeyCode.F;
    [SerializeField] private KeyCode triggerMarker = KeyCode.R;
    #endregion
    
    #region Properties
    public Vector2 MovementInput { get; private set; }
    public bool PlaceMarkerPressed { get; private set; }
    #endregion
    
    private void Update()
    {
        // Cache input state
        UpdateMovementInput();
        UpdateActionInput();
    }
    
    private void UpdateMovementInput()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(moveLeft)) horizontal = -1f;
        if (Input.GetKey(moveRight)) horizontal = 1f;
        if (Input.GetKey(moveUp)) vertical = 1f;
        if (Input.GetKey(moveDown)) vertical = -1f;
        
        MovementInput = new Vector2(horizontal, vertical);
    }
    
    private void UpdateActionInput()
    {
        PlaceMarkerPressed = Input.GetKeyDown(placeMarker);
    }
}
```

## Data Pattern

### ScriptableObject Configuration
```csharp
[CreateAssetMenu(fileName = "NewWaveData", menuName = "InfinityQube/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Identity")]
    public string waveName;
    public int waveNumber;
    
    [Header("Cube Configuration")]
    public List<CubeSpawnData> cubeSpawns;
    
    [Header("Timing")]
    public float startDelay = 2f;
    public float moveInterval = 1.75f;
    
    // Validation in editor
    private void OnValidate()
    {
        if (cubeSpawns == null)
            cubeSpawns = new List<CubeSpawnData>();
            
        if (moveInterval <= 0)
            moveInterval = 1.75f;
    }
}
```

## Performance Patterns

### Caching Pattern
```csharp
public class PerformantManager : MonoBehaviour
{
    // Cache frequently accessed components
    private Transform cachedTransform;
    private Renderer cachedRenderer;
    
    // Cache calculated values
    private float cachedDistance;
    private bool isDirty = true;
    
    private void Awake()
    {
        // Cache components once
        cachedTransform = transform;
        cachedRenderer = GetComponent<Renderer>();
    }
    
    private float GetDistance()
    {
        if (isDirty)
        {
            cachedDistance = Vector3.Distance(transform.position, target.position);
            isDirty = false;
        }
        return cachedDistance;
    }
    
    private void OnPositionChanged()
    {
        isDirty = true;
    }
}
```

---

**Pattern Validation**: Build system checks for required patterns  
**Pattern Templates**: Available in `Templates/CodePatterns/`  
**Pattern Violations**: Logged as warnings in build output