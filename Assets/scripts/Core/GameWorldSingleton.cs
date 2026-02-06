using UnityEngine;

/// <summary>
/// Prevents duplicate GameWorld objects when reloading the same scene.
/// Attach this to the GameWorld root object.
/// When scene reloads, destroys the OLD persisted GameWorld so the fresh one takes over.
/// </summary>
public class GameWorldSingleton : MonoBehaviour
{
    private static GameWorldSingleton _instance;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Another GameWorld exists (from DontDestroyOnLoad)
            // Destroy the OLD one so this fresh scene instance takes over
            if (enableDebugLogs)
                Debug.Log($"[GameWorldSingleton] Destroying old GameWorld, keeping fresh scene instance");
            
            Destroy(_instance.gameObject);
        }
        
        _instance = this;
        
        if (enableDebugLogs)
            Debug.Log($"[GameWorldSingleton] GameWorld initialized");
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
