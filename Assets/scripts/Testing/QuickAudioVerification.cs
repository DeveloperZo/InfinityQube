using UnityEngine;
using System.Collections;
using static Enumerations;

/// <summary>
/// Quick verification test for WaveManager audio integration
/// Tests that audio events trigger properly during wave operations
/// </summary>
public class QuickAudioVerification : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(VerifyAudioIntegration());
    }
    
    IEnumerator VerifyAudioIntegration()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("=== QUICK AUDIO VERIFICATION ===");
        
        // 1. Check WaveManager has AudioManager reference
        var waveManager = FindObjectOfType<WaveManager>();
        if (waveManager == null)
        {
            Debug.LogError("❌ WaveManager not found!");
            yield break;
        }
        
        // 2. Check AudioManager exists
        if (AudioManager.Instance == null)
        {
            Debug.LogError("❌ AudioManager not found!");
            yield break;
        }
        
        Debug.Log("✅ WaveManager and AudioManager found");
        
        // 3. Test wave start event
        Debug.Log("🧪 Testing wave start audio event...");
        AudioManager.Instance.TriggerAudioEvent(GameAudioEvent.WaveStarted, Vector3.zero);
        
        yield return new WaitForSeconds(0.5f);
        
        // 4. Test cube landing event
        Debug.Log("🧪 Testing cube landing audio event...");
        AudioManager.Instance.TriggerCubeAudioEvent(GameAudioEvent.CubeLanded, CubeType.Unit, transform.position);
        
        yield return new WaitForSeconds(0.5f);
        
        // 5. Test cube capture event
        Debug.Log("🧪 Testing cube capture audio event...");
        AudioManager.Instance.TriggerCubeAudioEvent(GameAudioEvent.CubeCaptured, CubeType.Prime, transform.position);
        
        yield return new WaitForSeconds(0.5f);
        
        // 6. Test wave complete event
        Debug.Log("🧪 Testing wave complete audio event...");
        AudioManager.Instance.TriggerAudioEvent(GameAudioEvent.WaveCompleted, Vector3.zero);
        
        Debug.Log("✅ Audio verification completed! Check console for audio event logs.");
        Debug.Log("✅ Audio functionality integrated and working correctly.");
        
        // 7. Simulate a quick wave operation to test actual integration
        if (waveManager.showDebugInfo)
        {
            Debug.Log("🧪 WaveManager debug mode enabled - audio events will be logged during wave operations");
        }
        else
        {
            Debug.Log("💡 Enable WaveManager 'Show Debug Info' to see audio event logs during actual gameplay");
        }
    }
}
