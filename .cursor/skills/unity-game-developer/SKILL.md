---
name: unity-game-developer
description: Advanced Unity game development guidance covering C# scripting, architecture patterns, gameplay systems, UI, debugging, testing, and asset management. Use when writing Unity scripts, creating MonoBehaviours, implementing managers, debugging gameplay issues, setting up tests, or organizing project assets.
---

# Unity Game Developer

## Lessons Learned (From Project History)

### Recurring Bug Patterns

These bugs have appeared repeatedly - **avoid them proactively**:

| Bug Type | Cause | Prevention |
|----------|-------|------------|
| NullReferenceException | `FindFirstObjectByType` in Update/runtime | Cache ALL manager refs in Start() |
| Stage progression fails | Event subscription timing | Subscribe in OnEnable, validate in Start |
| Out of bounds errors | Missing validation before grid ops | Always validate positions before use |
| Race conditions | Awake/Start timing between managers | Use events for cross-manager init |

### Anti-Patterns Found in Codebase (Don't Repeat)

```csharp
// BAD: Found in CubeManager.MoveForward() - called every frame
var waveManager = FindFirstObjectByType<WaveManager>();

// BAD: Found in PlayerManager.HandleTileChangeForSegments()
var actionManager = FindFirstObjectByType<PlayerActionManager>();

// GOOD: Cache in Start(), use cached reference
private WaveManager waveManager;
private void Start() => waveManager = WaveManager.Instance;
```

### What Worked Well

1. **Tile decomposition pattern** - Breaking Tile.cs into TileVisuals, TileMarker, TileCorruption components
2. **Event-driven audio** - AudioManager subsystems with clean decoupling
3. **POC philosophy** - Mark experimental code, don't over-engineer prototypes
4. **Static enumeration imports** - `using static Enumerations;` universally adopted

## Core Principles

1. **Cache ALL references** - Never use `FindFirstObjectByType<>()` in Update or runtime methods
2. **Validate after caching** - Null-check in dedicated `ValidateReferences()` method
3. **Region organization** - Follow standard file structure for all classes
4. **Debug systematically** - Use `LogExtensions` pattern with toggleable flags
5. **Decompose early** - When a file approaches 400 lines, plan extraction

## MonoBehaviour Patterns

### Component Setup

```csharp
using UnityEngine;
using static Enumerations;

public class ExampleComponent : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private GameObject targetPrefab;
    #endregion

    #region Manager References
    private GridManager gridManager;
    #endregion

    #region Runtime State
    private bool isInitialized;
    #endregion

    #region Properties
    public bool IsActive { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        gridManager = GridManager.Instance;
        ValidateReferences();
    }
    #endregion

    #region Private Methods
    private void ValidateReferences()
    {
        if (gridManager == null)
            DebugWarning("ValidateReferences", "GridManager not found");
    }
    #endregion

    #region Debug
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Preferred: Use LogExtensions (project standard)
    // this.Log("message", enableDebugLogs);
    // Output: [ExampleComponent] message

    // Alternative: Wrapper method for consistency
    private void DebugLog(string message) => this.Log(message, enableDebugLogs);
    private void DebugWarning(string message) => this.LogWarning(message, enableDebugLogs);
    private void DebugError(string message) => this.LogError(message); // Always logs
    #endregion
}
```

### Singleton Pattern

```csharp
#region Properties
public static ManagerName Instance { get; private set; }
#endregion

#region Unity Lifecycle
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
    }
    else if (Instance != this)
    {
        Debug.LogWarning($"Multiple {GetType().Name} found! Destroying duplicate.");
        Destroy(gameObject);
        return;
    }
}

private void OnDestroy()
{
    if (Instance == this)
        Instance = null;
}
#endregion
```

## Architecture Patterns

### Manager Communication (Critical - Bug Source)

**Priority order for getting references:**
1. Singleton `Instance` property (preferred)
2. `FindFirstObjectByType<>()` in `Start()` only (acceptable)
3. Passed via `Init()` parameter (for dependencies)

**NEVER do this (found causing bugs):**
```csharp
// These caused NullReferenceExceptions in production:
private void MoveForward() {
    var waveManager = FindFirstObjectByType<WaveManager>(); // BAD
}
private void HandleTileChange() {
    var actionManager = FindFirstObjectByType<PlayerActionManager>(); // BAD
}
```

**Always validate after caching:**
```csharp
private void Start()
{
    CacheManagerReferences();
    ValidateReferences();
}

private void CacheManagerReferences()
{
    gridManager = GridManager.Instance;
    waveManager = WaveManager.Instance ?? FindFirstObjectByType<WaveManager>();
    playerManager = FindFirstObjectByType<PlayerManager>();
}

private void ValidateReferences()
{
    if (gridManager == null) DebugLog("GridManager not found - features limited");
    if (waveManager == null) DebugLog("WaveManager not found - features limited");
}
```

**Event subscription pattern (prevents race conditions):**
- Subscribe to events in `OnEnable()`, unsubscribe in `OnDisable()`
- For cross-manager initialization, use events not direct calls

### Event Patterns

```csharp
// C# events for code-only
public event Action<int> OnScoreChanged;
OnScoreChanged?.Invoke(newScore);

// UnityEvent for Inspector configuration
[SerializeField] private UnityEvent<int> onScoreChanged;
onScoreChanged?.Invoke(newScore);
```

### Coroutine Management

```csharp
private Coroutine activeCoroutine;

public void StartProcess()
{
    if (activeCoroutine != null)
        StopCoroutine(activeCoroutine);
    activeCoroutine = StartCoroutine(ProcessRoutine());
}

private IEnumerator ProcessRoutine()
{
    yield return new WaitForSeconds(1f);
    // Work here
}

private void OnDestroy()
{
    if (activeCoroutine != null)
        StopCoroutine(activeCoroutine);
}
```

## File Decomposition (Critical - 12 Files Over Limit)

### When to Decompose

| File Size | Action |
|-----------|--------|
| < 400 lines | No action needed |
| 400-600 lines | Plan extraction points |
| > 600 lines | **Must decompose** |

### Successful Pattern: Tile System Decomposition

The Tile system was successfully decomposed - use this as model:

```
Before: Tile.cs (1000+ lines, doing everything)

After:
Tile.cs (facade, ~200 lines)
├── TileVisuals.cs (visual state, materials)
├── TileMarker.cs (marker display logic)
├── TileCorruption.cs (corruption mechanics)
└── TileFacePainting.cs (face painting system)
```

**Facade pattern implementation:**
```csharp
public class Tile : MonoBehaviour
{
    // Component references (lazy initialized)
    private TileVisuals visuals;
    private TileMarker marker;
    
    public TileVisuals Visuals => visuals ??= GetComponent<TileVisuals>();
    public TileMarker Marker => marker ??= GetComponent<TileMarker>();
    
    // Facade methods delegate to components
    public void SetHighlight(bool active) => Visuals.SetHighlight(active);
    public void PlaceMarker(MarkerType type) => Marker.Place(type);
}
```

### Files Needing Decomposition (Current)

| File | Lines | Extract To |
|------|-------|------------|
| WaveManager | 2083 | WaveSpawnSystem, WaveStatistics, WaveUI |
| PlayerActionManager | 1936 | MarkerEconomy, MarkerPlacement, MarkerTrigger |
| CubeManager | 1535 | CubeFacePainting, CubeAudio, CubePhysics |
| GridManager | 2036 | GridCoordinates, GridBatchOps, RowManager |

### POC Philosophy

Mark experimental code, don't over-engineer:

```csharp
// POC: Simple escape effect - works for now, optimize if needed
private void SpawnEscapeEffect(Vector3 position)
{
    Instantiate(escapeEffectPrefab, position, Quaternion.identity);
}
```

- POC code must be functional
- No upgrade requirement if it works
- Can violate optimization rules, not safety rules

## Debugging Strategies

### Console Filtering

Use prefixed logs for easy filtering:
- `[ClassName] message` format (project standard via LogExtensions)
- Filter in Unity Console by typing `[GridManager]` etc.

### Issues Actually Encountered (Project History)

| Issue | Root Cause Found | Fix Applied |
|-------|------------------|-------------|
| Stage 1 → 2 progression fails | `OnAllWavesCompleted()` event timing | Fixed event subscription order |
| Out of bounds Stage 1 Wave 4 | Missing bounds check before spawn | Added `IsValidGridPosition()` validation |
| NullRef in CubeManager | `FindFirstObjectByType` in `MoveForward()` | Cache WaveManager in Start() |
| NullRef in PlayerManager | Runtime lookup for PlayerActionManager | Cache reference in Start() |
| Wave override conflicts | Stage vs wave config priority unclear | Stage settings override wave defaults |

### Common Debug Workflow

1. **Check console** - Filter by `[ManagerName]`
2. **Verify Inspector** - Are references assigned?
3. **Check execution order** - Edit > Project Settings > Script Execution Order
4. **Validate event subscriptions** - OnEnable/OnDisable pairing
5. **Use F12 Debug Panel** - Runtime state inspection

### Performance Debugging

```csharp
// Profile specific code sections
using (new ProfilerMarker("MyOperation").Auto())
{
    // Code to profile
}

// Or use Stopwatch for quick timing
var sw = System.Diagnostics.Stopwatch.StartNew();
// Operation
DebugLog("Timing", $"Took {sw.ElapsedMilliseconds}ms");
```

## Testing Patterns

### Edit Mode Tests

```csharp
using NUnit.Framework;

[TestFixture]
public class MyTests
{
    [Test]
    public void MethodName_Condition_ExpectedResult()
    {
        // Arrange
        var data = new TestData();

        // Act
        var result = data.Calculate();

        // Assert
        Assert.AreEqual(expected, result);
    }
}
```

### Play Mode Tests

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class PlayModeTests
{
    [UnityTest]
    public IEnumerator Component_Action_ExpectedBehavior()
    {
        // Arrange
        var go = new GameObject();
        var component = go.AddComponent<MyComponent>();

        // Act
        yield return null; // Wait one frame

        // Assert
        Assert.IsTrue(component.IsInitialized);

        // Cleanup
        Object.Destroy(go);
    }
}
```

### Test Organization

```
Assets/
  Tests/
    EditMode/
      TestAssembly.asmdef  (Editor only)
      DataTests.cs
    PlayMode/
      TestAssembly.asmdef  (Include Editor + platforms)
      ComponentTests.cs
```

## Asset Management

### Project Structure

```
Assets/
  data/              # ScriptableObject instances
    stages/
    waves/
  Prefabs/           # Prefab assets
  Scenes/
  scripts/
    Components/      # Gameplay components
    Data/            # ScriptableObject definitions
    Managers/        # Singleton managers
    Utils/           # Helpers and utilities
    Debuggers/       # Debug tools
```

### ScriptableObject Pattern

```csharp
[CreateAssetMenu(fileName = "NewData", menuName = "Game/DataType")]
public class MyData : ScriptableObject
{
    [Header("Configuration")]
    [SerializeField] private string dataName;
    [SerializeField] private int value;

    public string DataName => dataName;
    public int Value => value;

    private void OnValidate()
    {
        if (value < 0)
            value = 0;
    }
}
```

### Asset Loading

```csharp
// Resources (avoid for large assets)
var prefab = Resources.Load<GameObject>("Prefabs/Enemy");

// Addressables (preferred for production)
var handle = Addressables.LoadAssetAsync<GameObject>("enemy");
yield return handle;
var prefab = handle.Result;

// Direct reference (best for frequently used)
[SerializeField] private GameObject enemyPrefab;
```

## File Size Limits (Enforced)

| Type | Max Lines | Current Violations |
|------|-----------|-------------------|
| Core Components | 750 | Tile (878) |
| Manager Classes | 600 | WaveManager (2083), PlayerActionManager (1936), CubeManager (1535), GridManager (1384) |
| Utility Classes | 300 | PlayerMarkerSystem (1122), IQWaveGenerator (1043) |
| Data Classes | 300 | - |
| Interfaces | 200 | - |

> **Note**: See `TechnicalDebt.md` for current violation counts and extraction plans.

**Warning signs to watch:**
- Approaching 400 lines → Plan extraction points
- Multiple `#region` sections getting large → Extract to component
- Methods over 50 lines → Extract to helper class

## DOTween Patterns

The project uses DOTween Pro for animations and tweening.

### Basic Tweens

```csharp
using DG.Tweening;

// Transform tweens
transform.DOMove(targetPosition, 0.5f);
transform.DORotate(new Vector3(0, 180, 0), 0.3f);
transform.DOScale(Vector3.one * 1.2f, 0.2f);

// Fade (CanvasGroup or SpriteRenderer)
canvasGroup.DOFade(0f, 0.3f);
spriteRenderer.DOFade(1f, 0.5f);

// Color
image.DOColor(Color.red, 0.2f);
```

### Sequences (Chained Animations)

```csharp
// Create reusable sequences for UI
private Sequence CreatePanelOpenSequence(RectTransform panel, CanvasGroup group)
{
    return DOTween.Sequence()
        .Append(panel.DOScale(Vector3.one, 0.3f).From(Vector3.zero))
        .Join(group.DOFade(1f, 0.2f).From(0f))
        .SetEase(Ease.OutBack);
}

// Use with callbacks
sequence.OnComplete(() => OnAnimationComplete());
```

### Tween Settings

```csharp
// Easing (common choices)
transform.DOMove(target, 0.5f).SetEase(Ease.OutQuad);   // Smooth decelerate
transform.DOScale(target, 0.3f).SetEase(Ease.OutBack);  // Overshoot bounce
transform.DOFade(0f, 0.2f).SetEase(Ease.InQuad);        // Smooth accelerate

// Looping
transform.DORotate(new Vector3(0, 360, 0), 1f)
    .SetLoops(-1, LoopType.Restart);  // Infinite rotation

// Kill previous tweens before starting new
transform.DOKill();
transform.DOMove(newTarget, 0.5f);
```

### Cleanup (Critical)

```csharp
private Tween activeTween;

private void OnDestroy()
{
    activeTween?.Kill();
    // Or kill all tweens on this object
    transform.DOKill();
}
```

## Odin Inspector Patterns

The project uses Odin Inspector & Serializer for enhanced editor workflows.

### Common Attributes

```csharp
using Sirenix.OdinInspector;

public class GameSettings : MonoBehaviour
{
    [Title("Movement Settings")]
    [Range(0, 100)]
    public float speed = 10f;
    
    [Title("Visual Settings")]
    [PreviewField(50)]
    public Sprite icon;
    
    [FoldoutGroup("Advanced")]
    public bool enableDebug;
    
    [FoldoutGroup("Advanced")]
    [ShowIf("enableDebug")]
    public string debugTag;
}
```

### Validation

```csharp
[Required]
public GameObject requiredPrefab;

[ValidateInput("IsValidName", "Name cannot be empty")]
public string entityName;

private bool IsValidName(string name) => !string.IsNullOrEmpty(name);

[AssetsOnly]  // Only allow prefabs, not scene objects
public GameObject prefabOnly;

[SceneObjectsOnly]  // Only allow scene objects
public Transform sceneTarget;
```

### Organization

```csharp
[TabGroup("Stats")]
public int health;

[TabGroup("Stats")]
public int damage;

[TabGroup("Visual")]
public Material material;

[BoxGroup("Spawn Settings")]
public float spawnRate;

[BoxGroup("Spawn Settings")]
public int maxSpawns;

[HorizontalGroup("Position")]
public float x, y, z;
```

### Buttons (Editor Actions)

```csharp
[Button("Spawn Test Enemy")]
private void SpawnTestEnemy()
{
    Instantiate(enemyPrefab, transform.position, Quaternion.identity);
}

[Button("Reset to Defaults"), GUIColor(1, 0.5f, 0.5f)]
private void ResetDefaults()
{
    speed = 10f;
    health = 100;
}
```

### ScriptableObjects with Odin

```csharp
[CreateAssetMenu]
public class WaveConfig : SerializedScriptableObject  // Note: SerializedScriptableObject
{
    [TableList]
    public List<SpawnEntry> spawns;
    
    // Odin can serialize interfaces, dictionaries, etc.
    public Dictionary<string, int> rewards;
}
```

## UI Panel Patterns

### Panel Lifecycle

```csharp
public class BasePanel : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform panelRect;
    
    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panelRect = GetComponent<RectTransform>();
    }
    
    public virtual void Open()
    {
        gameObject.SetActive(true);
        PlayOpenAnimation();
    }
    
    public virtual void Close()
    {
        PlayCloseAnimation(() => gameObject.SetActive(false));
    }
    
    protected virtual void PlayOpenAnimation()
    {
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.8f;
        
        DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.2f))
            .Join(panelRect.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
    }
    
    protected virtual void PlayCloseAnimation(Action onComplete)
    {
        DOTween.Sequence()
            .Append(canvasGroup.DOFade(0f, 0.15f))
            .Join(panelRect.DOScale(Vector3.one * 0.8f, 0.15f))
            .OnComplete(() => onComplete?.Invoke());
    }
}
```

### Panel Manager Pattern

```csharp
public class PanelManager : MonoBehaviour
{
    [SerializeField] private BasePanel[] panels;
    
    private BasePanel currentPanel;
    private Stack<BasePanel> panelHistory = new();
    
    public void ShowPanel<T>() where T : BasePanel
    {
        var panel = GetPanel<T>();
        if (panel == null) return;
        
        if (currentPanel != null)
        {
            panelHistory.Push(currentPanel);
            currentPanel.Close();
        }
        
        currentPanel = panel;
        currentPanel.Open();
    }
    
    public void GoBack()
    {
        if (panelHistory.Count == 0) return;
        
        currentPanel?.Close();
        currentPanel = panelHistory.Pop();
        currentPanel.Open();
    }
    
    private T GetPanel<T>() where T : BasePanel
    {
        return panels.OfType<T>().FirstOrDefault();
    }
}
```

### Data Binding Pattern

```csharp
public class StatsPanel : BasePanel
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image healthFill;
    
    private PlayerStats stats;
    
    public void Bind(PlayerStats playerStats)
    {
        stats = playerStats;
        stats.OnHealthChanged += UpdateHealth;
        stats.OnScoreChanged += UpdateScore;
        RefreshAll();
    }
    
    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= UpdateHealth;
            stats.OnScoreChanged -= UpdateScore;
        }
    }
    
    private void RefreshAll()
    {
        UpdateHealth(stats.CurrentHealth, stats.MaxHealth);
        UpdateScore(stats.Score);
    }
    
    private void UpdateHealth(int current, int max)
    {
        healthText.text = $"{current}/{max}";
        healthFill.DOFillAmount((float)current / max, 0.3f);
    }
    
    private void UpdateScore(int score)
    {
        scoreText.text = score.ToString("N0");
    }
}
```

### Button Setup Pattern

```csharp
public class MenuPanel : BasePanel
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    private void OnDestroy()
    {
        playButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
    }
    
    private void OnPlayClicked()
    {
        // Button feedback
        playButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        // Action
        GameManager.Instance.StartGame();
    }
}
```

## Quick Reference

### Naming Conventions

- Managers: `[Function]Manager.cs`
- Data: `[Type]Data.cs`
- Utilities: `[Function]Utils.cs`
- Interfaces: `I[Capability].cs`
- Private fields: `camelCase`
- Constants: `UPPER_CASE`
- Properties: `PascalCase`

### Required Regions Order

1. Inspector Configuration
2. Manager References
3. Runtime State
4. Properties
5. Unity Lifecycle
6. Public API
7. Private Methods
8. Debug

### Enumerations Usage

```csharp
using static Enumerations;

// Use directly without prefix
CubeType.Unit
MarkerMode.Unit
GameAudioEvent.WaveStarted
```

## Additional Resources

- For detailed testing patterns, see [testing-patterns.md](testing-patterns.md)
- For debugging workflows, see [debugging-guide.md](debugging-guide.md)
