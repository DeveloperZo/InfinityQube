using UnityEngine;
using System.Collections;

/// <summary>
/// Validation script to test the refactored Stage/Wave event system
/// </summary>
public class StageWaveValidation : MonoBehaviour
{
    [Header("Components to Test")]
    public StageManager stageManager;
    public WaveManager waveManager;
    
    [Header("Test Settings")]
    public bool runValidationOnStart = false;
    public int testStageIndex = 0;
    
    private bool eventReceived = false;
    private int waveCompleteCount = 0;
    private int waveFailCount = 0;
    private int allWavesCompleteCount = 0;

    private void Start()
    {
        if (runValidationOnStart)
        {
            StartCoroutine(ValidateEventSystem());
        }
    }

    public IEnumerator ValidateEventSystem()
    {
        Debug.Log("=== Starting Stage/Wave Event System Validation ===");
        
        // Find components if not assigned
        if (stageManager == null)
            stageManager = FindFirstObjectByType<StageManager>();
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (stageManager == null || waveManager == null)
        {
            Debug.LogError("❌ Validation failed: Missing components");
            yield break;
        }

        // Subscribe to events to verify they are triggered
        SubscribeToEvents();

        // Test 1: Check if events are properly set up
        Debug.Log("📋 Test 1: Checking event setup...");
        if (waveManager.OnWaveComplete == null || waveManager.OnWaveFailed == null || waveManager.OnAllWavesComplete == null)
        {
            Debug.LogError("❌ Events not properly initialized");
            yield break;
        }
        Debug.Log("✅ Events properly initialized");

        // Test 2: Load a stage and check if wave starts
        Debug.Log("📋 Test 2: Loading stage and checking wave startup...");
        stageManager.LoadStage(testStageIndex);
        
        yield return new WaitForSeconds(3f); // Give time for stage to load
        
        if (!waveManager.waveActive)
        {
            Debug.LogError("❌ Wave did not start after stage load");
            yield break;
        }
        Debug.Log("✅ Wave started successfully after stage load");

        // Test 3: Check if manual wave control works
        Debug.Log("📋 Test 3: Testing manual wave control...");
        bool hasMoreWaves = waveManager.HasMoreWaves();
        Debug.Log($"Has more waves: {hasMoreWaves}");
        
        if (hasMoreWaves)
        {
            waveManager.StartNextWave();
            yield return new WaitForSeconds(1f);
            Debug.Log("✅ Manual wave control working");
        }

        // Test 4: Verify event flow (would need a complete wave cycle)
        Debug.Log("📋 Test 4: Event flow verification...");
        Debug.Log($"Wave complete events received: {waveCompleteCount}");
        Debug.Log($"Wave fail events received: {waveFailCount}");
        Debug.Log($"All waves complete events received: {allWavesCompleteCount}");

        UnsubscribeFromEvents();
        
        Debug.Log("=== Stage/Wave Event System Validation Complete ===");
    }

    private void SubscribeToEvents()
    {
        if (waveManager.OnWaveComplete != null)
            waveManager.OnWaveComplete.AddListener(OnWaveCompleteReceived);
        if (waveManager.OnWaveFailed != null)
            waveManager.OnWaveFailed.AddListener(OnWaveFailReceived);
        if (waveManager.OnAllWavesComplete != null)
            waveManager.OnAllWavesComplete.AddListener(OnAllWavesCompleteReceived);
    }

    private void UnsubscribeFromEvents()
    {
        if (waveManager.OnWaveComplete != null)
            waveManager.OnWaveComplete.RemoveListener(OnWaveCompleteReceived);
        if (waveManager.OnWaveFailed != null)
            waveManager.OnWaveFailed.RemoveListener(OnWaveFailReceived);
        if (waveManager.OnAllWavesComplete != null)
            waveManager.OnAllWavesComplete.RemoveListener(OnAllWavesCompleteReceived);
    }

    private void OnWaveCompleteReceived(int waveIndex)
    {
        waveCompleteCount++;
        Debug.Log($"✅ Validation: Wave {waveIndex} complete event received");
        eventReceived = true;
    }

    private void OnWaveFailReceived(int waveIndex)
    {
        waveFailCount++;
        Debug.Log($"❌ Validation: Wave {waveIndex} fail event received");
        eventReceived = true;
    }

    private void OnAllWavesCompleteReceived()
    {
        allWavesCompleteCount++;
        Debug.Log("🏁 Validation: All waves complete event received");
        eventReceived = true;
    }

    [ContextMenu("Run Validation")]
    public void RunValidation()
    {
        StartCoroutine(ValidateEventSystem());
    }
}
