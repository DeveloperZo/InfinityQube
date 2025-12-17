using UnityEngine;

/// <summary>
/// Test script to verify LogExtensions functionality.
/// This can be attached to any GameObject in the scene for testing.
/// </summary>
public class LogExtensionsTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool EnableDebugLogs;
    
    private void Start()
    {
        TestBasicLogging();
        TestStateChangeLogging();
        TestActionLogging();
        TestInitializationLogging();
        TestPerformanceLogging();
        TestErrorLogging();
    }
    
    private void TestBasicLogging()
    {
        // Test basic log with debug flag enabled
        this.Log("Basic log message test", EnableDebugLogs);
        
        // Test warning
        this.LogWarning("Warning message test", EnableDebugLogs);
        
        // Test with debug flag disabled
        this.Log("This should not appear if EnableDebugLogs is false", false);
    }
    
    private void TestStateChangeLogging()
    {
        // Test state change logging
        int oldHealth = 100;
        int newHealth = 75;
        this.LogStateChange("Health", oldHealth, newHealth, EnableDebugLogs);
        
        // Test with string values
        string oldState = "Idle";
        string newState = "Active";
        this.LogStateChange("GameState", oldState, newState, EnableDebugLogs);
    }
    
    private void TestActionLogging()
    {
        // Test simple action
        this.LogAction("Player moved", "to position (5, 10)", null, EnableDebugLogs);
        
        // Test action with duration
        this.LogAction("Wave spawned", "15 cubes", 125.5f, EnableDebugLogs);
        
        // Test action without details
        this.LogAction("Game paused", "", null, EnableDebugLogs);
    }
    
    private void TestInitializationLogging()
    {
        // Test initialization with dependencies
        this.LogInitialization(EnableDebugLogs, "GridManager: Found", "WaveManager: Found", "AudioManager: Missing");
        
        // Test initialization without dependencies
        this.LogInitialization(EnableDebugLogs);
    }
    
    private void TestPerformanceLogging()
    {
        // Test performance logging
        this.LogPerformance("Grid generation", 64, 45.2f, EnableDebugLogs);
        
        // Test with zero operations (edge case)
        this.LogPerformance("Empty operation", 0, 0f, EnableDebugLogs);
    }
    
    private void TestErrorLogging()
    {
        // Test error logging (always logs regardless of flag)
        this.LogError("Critical error test - this always appears");
        
        // Verify it works even with null/empty message
        this.LogError("");
    }
    
    // Test from a non-MonoBehaviour class
    private void TestFromOtherClass()
    {
        TestHelper helper = new TestHelper();
        helper.DoSomething(EnableDebugLogs);
    }
    
    // Helper class to test extension methods work from any class
    private class TestHelper
    {
        public void DoSomething(bool enableLogs)
        {
            this.Log("Message from TestHelper class", enableLogs);
        }
    }
}
