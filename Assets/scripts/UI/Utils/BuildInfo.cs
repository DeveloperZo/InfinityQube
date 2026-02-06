using UnityEngine;

/// <summary>
/// Displays build info in debug builds.
/// Creates its own persistent object - does NOT call DontDestroyOnLoad on parent.
/// </summary>
public class BuildInfo : MonoBehaviour
{
    private static BuildInfo _instance;
    
    [Header("Build Information")]
    [Tooltip("Pulled from Project Settings > Player > Version")]
    public string BuildVersion => Application.version;
    
    [SerializeField]
    private string buildDate = "2025-12-10";

    public string BuildDate => buildDate;

    private void Awake()
    {
        if (!Debug.isDebugBuild)
        {
            Destroy(this); // Remove component in release builds
            return;
        }
        
        // Singleton pattern - if instance exists, destroy this one
        if (_instance != null && _instance != this)
        {
            Destroy(this); // Just destroy the component, not the whole GameObject
            return;
        }
        
        _instance = this;
        
        // DON'T call DontDestroyOnLoad here - it would persist the parent (GameWorld)
        // BuildInfo just displays GUI, it doesn't need to persist
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, Screen.height - 60, 400, 40),
                     $"Build: {BuildVersion} | {buildDate}\nDemo Build");
        }
    }
}