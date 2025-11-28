// Simple test to verify audio event system works
// Usage: AudioManager.Instance.TriggerCubeAudioEvent(GameAudioEvent.CubeLanded, CubeType.Unit, Vector3.zero);

using UnityEngine;
using static Enumerations;

public class AudioEventTest : MonoBehaviour
{
    void Start()
    {
        // Test after a short delay to ensure AudioManager is initialized
        Invoke(nameof(TestAudioEvents), 1f);
    }
    
    void TestAudioEvents()
    {
        if (AudioManager.Instance == null)
        {
            Debug.Log("[AudioEventTest] AudioManager not found!");
            return;
        }
        
        Debug.Log("[AudioEventTest] Testing simplified audio event system...");
        
        // Test basic event
        AudioManager.Instance.TriggerAudioEvent(GameAudioEvent.PlayerMoved, transform.position);
        
        // Test cube event  
        AudioManager.Instance.TriggerCubeAudioEvent(GameAudioEvent.CubeLanded, CubeType.Unit, transform.position);
        
        // Test with AudioEventData
        AudioEventData eventData = new AudioEventData(GameAudioEvent.WaveStarted, Vector3.zero, 0.8f);
        AudioManager.Instance.TriggerAudioEvent(eventData.eventType, eventData.worldPosition);
        
        Debug.Log("[AudioEventTest] Test completed successfully!");
    }
}
