using UnityEngine;

public class BuildInfo : MonoBehaviour
{
    [Header("Build Information")]
    [Tooltip("Pulled from Project Settings > Player > Version")]
    public string BuildVersion => Application.version;
    
    [SerializeField]
    private string buildDate = "2025-12-10";

    public string BuildDate => buildDate;

    private void Awake()
    {
        if (Debug.isDebugBuild)
        {
            DontDestroyOnLoad(gameObject);
        }
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