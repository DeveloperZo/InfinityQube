# Unity Testing Patterns Reference

## Assembly Definition Setup

### Edit Mode Tests

Create `Assets/Tests/EditMode/EditModeTests.asmdef`:

```json
{
    "name": "EditModeTests",
    "rootNamespace": "",
    "references": [
        "GUID:your-main-assembly-guid"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Play Mode Tests

Create `Assets/Tests/PlayMode/PlayModeTests.asmdef`:

```json
{
    "name": "PlayModeTests",
    "rootNamespace": "",
    "references": [
        "GUID:your-main-assembly-guid"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:
- `Calculate_WithNegativeInput_ReturnsZero`
- `Initialize_WhenManagerMissing_LogsWarning`
- `Move_ToOccupiedTile_ReturnsFalse`

## Common Test Patterns

### Testing MonoBehaviour Initialization

```csharp
[UnityTest]
public IEnumerator Manager_OnStart_InitializesCorrectly()
{
    var go = new GameObject("TestManager");
    var manager = go.AddComponent<MyManager>();

    yield return null; // Allow Start() to run

    Assert.IsTrue(manager.IsInitialized);
    Assert.IsNotNull(manager.Data);

    Object.Destroy(go);
}
```

### Testing Events

```csharp
[UnityTest]
public IEnumerator Component_OnAction_FiresEvent()
{
    var go = new GameObject();
    var component = go.AddComponent<MyComponent>();
    bool eventFired = false;
    int receivedValue = 0;

    component.OnValueChanged += (value) =>
    {
        eventFired = true;
        receivedValue = value;
    };

    component.SetValue(42);
    yield return null;

    Assert.IsTrue(eventFired);
    Assert.AreEqual(42, receivedValue);

    Object.Destroy(go);
}
```

### Testing Coroutines

```csharp
[UnityTest]
public IEnumerator Spawner_AfterDelay_SpawnsEnemy()
{
    var go = new GameObject();
    var spawner = go.AddComponent<EnemySpawner>();
    spawner.spawnDelay = 0.1f;

    spawner.StartSpawning();

    // Wait for spawn delay plus buffer
    yield return new WaitForSeconds(0.15f);

    Assert.AreEqual(1, spawner.SpawnedCount);

    Object.Destroy(go);
}
```

### Testing with ScriptableObjects

```csharp
[Test]
public void DataProcessor_WithValidData_ProcessesCorrectly()
{
    var testData = ScriptableObject.CreateInstance<GameData>();
    testData.Initialize(100, "Test");

    var processor = new DataProcessor();
    var result = processor.Process(testData);

    Assert.AreEqual(100, result.Value);

    Object.DestroyImmediate(testData);
}
```

### Mocking Manager References

```csharp
[UnityTest]
public IEnumerator Component_WithMockedManager_BehavesCorrectly()
{
    // Create mock manager first
    var managerGo = new GameObject("MockManager");
    var mockManager = managerGo.AddComponent<MockGridManager>();

    // Create component under test
    var go = new GameObject();
    var component = go.AddComponent<MyComponent>();

    yield return null;

    component.DoAction();
    Assert.IsTrue(mockManager.ActionWasCalled);

    Object.Destroy(go);
    Object.Destroy(managerGo);
}
```

## Scene-Based Tests

```csharp
[UnityTest]
public IEnumerator IntegrationTest_FullSceneSetup()
{
    // Load test scene
    yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Additive);

    var manager = Object.FindObjectOfType<GameManager>();
    Assert.IsNotNull(manager);

    manager.StartGame();
    yield return new WaitForSeconds(0.5f);

    Assert.AreEqual(GameState.Playing, manager.CurrentState);

    // Cleanup
    yield return SceneManager.UnloadSceneAsync("TestScene");
}
```

## Test Utilities

```csharp
public static class TestHelpers
{
    public static IEnumerator WaitForCondition(Func<bool> condition, float timeout = 5f)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (elapsed >= timeout)
            Assert.Fail($"Condition not met within {timeout}s");
    }

    public static T CreateComponent<T>() where T : Component
    {
        var go = new GameObject($"Test_{typeof(T).Name}");
        return go.AddComponent<T>();
    }

    public static void DestroyTestObject(Component component)
    {
        if (component != null && component.gameObject != null)
            Object.Destroy(component.gameObject);
    }
}
```

## Test Categories

Use categories to organize and filter tests:

```csharp
[Test, Category("Unit")]
public void UnitTest_Example() { }

[UnityTest, Category("Integration")]
public IEnumerator IntegrationTest_Example() { yield return null; }

[Test, Category("Performance")]
public void PerformanceTest_Example() { }
```

Run specific categories from command line:
```bash
Unity -runTests -testCategory Unit
```

## Assertions Quick Reference

| Assertion | Usage |
|-----------|-------|
| `Assert.AreEqual(expected, actual)` | Value equality |
| `Assert.AreSame(expected, actual)` | Reference equality |
| `Assert.IsTrue(condition)` | Boolean true |
| `Assert.IsFalse(condition)` | Boolean false |
| `Assert.IsNull(obj)` | Null check |
| `Assert.IsNotNull(obj)` | Not null |
| `Assert.Throws<T>(() => method())` | Exception expected |
| `Assert.DoesNotThrow(() => method())` | No exception |
| `Assert.That(value, Is.InRange(1, 10))` | Range check |
