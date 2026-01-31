# Unity Debugging Guide

## Lessons from Project History

### Bugs That Keep Recurring

1. **NullReferenceException from runtime lookups**
   - Cause: `FindFirstObjectByType<>()` called in Update or runtime methods
   - Found in: CubeManager.MoveForward(), PlayerManager.HandleTileChangeForSegments()
   - Fix: Cache ALL manager references in Start()

2. **Stage progression failures**
   - Cause: Event subscription timing issues
   - Found in: OnAllWavesCompleted() not firing correctly
   - Fix: Subscribe in OnEnable, validate subscription in Start

3. **Out of bounds errors**
   - Cause: Missing validation before grid operations
   - Found in: Stage 1 Wave 4 spawn calculations
   - Fix: Always call IsValidGridPosition() before grid ops

4. **Configuration override conflicts**
   - Cause: Stage vs wave-level settings unclear priority
   - Fix: Stage settings always override wave defaults

## Console Log Analysis

### Log Format Standard (Project Uses)

```
[ClassName] Descriptive message with values
```

Examples (actual project format):
```
[GridManager] Position (3, 5) is valid
[WaveManager] Starting wave 5 with 12 enemies
[PlayerManager] Player health reduced to 75/100
```

Note: Uses `LogExtensions.cs` - call `this.Log("message", enableDebugLogs)`

### Console Filtering

1. **By component**: Type `[GridManager]` in search
2. **By method**: Type `ValidatePosition:` in search
3. **By severity**: Click Log/Warning/Error toggles
4. **Collapse**: Enable "Collapse" to group identical messages

## Common Issues & Solutions

### NullReferenceException

**Symptoms**: "Object reference not set to an instance of an object"

**Investigation steps**:
1. Check the stack trace for exact line
2. Identify which variable is null
3. Trace where it should be assigned

**Common causes**:
- Missing `[SerializeField]` assignment in Inspector
- `FindObjectOfType` called before object exists
- Race condition in Awake/Start timing
- Destroyed object still referenced

**Solutions**:
```csharp
// Defensive coding
if (target == null)
{
    DebugWarning("MethodName", "Target is null, aborting");
    return;
}

// Null coalescing
var manager = GridManager.Instance ?? FindObjectOfType<GridManager>();

// TryGetComponent pattern
if (TryGetComponent<Rigidbody>(out var rb))
{
    rb.AddForce(Vector3.up);
}
```

### Missing Reference in Inspector

**Symptoms**: Field shows "Missing" or "None"

**Investigation**:
1. Check if prefab was deleted
2. Check if GUID changed (scene file corruption)
3. Verify object exists in scene

**Prevention**:
```csharp
private void OnValidate()
{
    if (requiredPrefab == null)
        Debug.LogError($"{name}: Required prefab is not assigned!");
}
```

### Script Execution Order Issues

**Symptoms**: Manager not ready when accessed

**Solutions**:
1. Use Script Execution Order (Edit > Project Settings > Script Execution Order)
2. Use lazy initialization:
```csharp
private GridManager _gridManager;
private GridManager GridManager => _gridManager ??= GridManager.Instance;
```
3. Use events for initialization notification:
```csharp
public static event Action OnManagerReady;

private void Start()
{
    Initialize();
    OnManagerReady?.Invoke();
}
```

### Coroutine Not Running

**Checklist**:
- [ ] GameObject is active (`gameObject.activeInHierarchy`)
- [ ] Component is enabled (`enabled == true`)
- [ ] `StartCoroutine` return value stored if stopping needed
- [ ] Not calling from constructor or field initializer
- [ ] `yield return` exists in coroutine body

```csharp
// Debug coroutine lifecycle
private IEnumerator TrackedCoroutine()
{
    DebugLog("TrackedCoroutine", "Started");
    yield return new WaitForSeconds(1f);
    DebugLog("TrackedCoroutine", "Completed");
}
```

### Event Not Firing

**Checklist**:
- [ ] Event is not null when invoked
- [ ] Subscriber is subscribed before event fires
- [ ] `OnEnable/OnDisable` pairing for subscriptions
- [ ] Event delegate signature matches

```csharp
// Verify subscription
private void OnEnable()
{
    DebugLog("OnEnable", "Subscribing to events");
    GameEvents.OnScoreChanged += HandleScoreChanged;
}

private void OnDisable()
{
    DebugLog("OnDisable", "Unsubscribing from events");
    GameEvents.OnScoreChanged -= HandleScoreChanged;
}
```

## Performance Debugging

### Profiler Markers

```csharp
using Unity.Profiling;

private static readonly ProfilerMarker s_PreparePerfMarker = 
    new ProfilerMarker("MySystem.Prepare");
private static readonly ProfilerMarker s_ExecutePerfMarker = 
    new ProfilerMarker("MySystem.Execute");

private void Update()
{
    using (s_PreparePerfMarker.Auto())
    {
        Prepare();
    }

    using (s_ExecutePerfMarker.Auto())
    {
        Execute();
    }
}
```

### Memory Allocation Tracking

```csharp
// Check for GC allocations
private void Update()
{
    // BAD: Allocates every frame
    string status = $"Health: {health}";

    // GOOD: Use StringBuilder or cache
    statusBuilder.Clear();
    statusBuilder.Append("Health: ");
    statusBuilder.Append(health);
}
```

### Quick Timing

```csharp
private void MeasuredOperation()
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    // Operation to measure
    ExpensiveCalculation();

    sw.Stop();
    DebugLog("MeasuredOperation", $"Took {sw.ElapsedMilliseconds}ms");
}
```

## Visual Debugging

### Gizmos

```csharp
private void OnDrawGizmos()
{
    // Always visible in Scene view
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}

private void OnDrawGizmosSelected()
{
    // Only when selected
    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position, targetPosition);
}
```

### Debug.DrawLine/DrawRay

```csharp
// Visible in Scene view (not Game view)
private void Update()
{
    Debug.DrawLine(start, end, Color.green);
    Debug.DrawRay(origin, direction * 10f, Color.red);
}
```

### Runtime Debug UI

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
private void OnGUI()
{
    GUILayout.BeginArea(new Rect(10, 10, 200, 100));
    GUILayout.Label($"State: {currentState}");
    GUILayout.Label($"Position: {transform.position}");
    GUILayout.Label($"Velocity: {rb.velocity.magnitude:F2}");
    GUILayout.EndArea();
}
#endif
```

## Debug Flags Pattern

```csharp
[Header("Debug")]
[SerializeField] private bool enableDebugLogs = true;
[SerializeField] private bool drawDebugGizmos = true;
[SerializeField] private bool pauseOnError = false;

private void DebugLog(string method, string message)
{
    if (enableDebugLogs)
        Debug.Log($"[{GetType().Name}] {method}: {message}");
}

private void HandleError(string method, string message)
{
    DebugError(method, message);
    if (pauseOnError)
        Debug.Break();
}
```

## Breakpoint Debugging (IDE)

### Visual Studio / Rider Setup

1. Attach to Unity Editor: Debug > Attach to Unity
2. Set breakpoints in C# code
3. Trigger the code path in Unity

### Conditional Breakpoints

```csharp
// Add condition in IDE breakpoint settings
// Example: i == 42 or health < 0
```

### Immediate Window

When paused at breakpoint:
- Evaluate expressions: `transform.position`
- Call methods: `GetComponent<Rigidbody>().velocity`
- Modify values: `health = 100`

## F12 Debug Panel (Project-Specific)

The project has a built-in debug panel accessible via F12:

### Panel Features
- **System panel** - Runtime state inspection, log directory access
- **Prototyping panel** - Stage/wave swapping, quick validation
- **Console capture** - In-game log viewing

### Troubleshooting Debug Panel
| Issue | Solution |
|-------|----------|
| Panel not showing | Check F12 toggle, verify PrototypingSystem component exists |
| Manager shows NULL | Check manager GameObject exists, verify initialization order |
| Console not capturing | Ensure debug panel initialized before logs fire |
| Performance lag | IMGUI overhead - reduce log history size |

### Log File Location
```
%USERPROFILE%\AppData\LocalLow\DefaultCompany\InfinityQube\Logs\
```

## Debugging Checklist

### Before Reporting Bug

- [ ] Reproduce consistently
- [ ] Check console for errors/warnings (filter by `[ClassName]`)
- [ ] Verify Inspector values
- [ ] Check F12 Debug Panel for runtime state
- [ ] Test in isolation (new scene)
- [ ] Check script execution order
- [ ] Verify event subscriptions (OnEnable/OnDisable pairing)
- [ ] Check if manager refs cached vs runtime lookup
- [ ] Profile for performance issues
