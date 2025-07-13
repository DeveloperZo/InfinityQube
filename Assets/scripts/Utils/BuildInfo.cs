// Add this to a new script for build info
using UnityEngine;

public class BuildInfo : MonoBehaviour
{
    [Header("Build Information")]
    public string buildVersion = "0.3.708-alpha";
    public string buildDate = "2025-07-08";

    // Add to BuildInfo.cs around line 15
    private void Awake()
    {
        // Show build info in development builds
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
                     $"Build: {buildVersion} | {buildDate}\nDemo Build");
        }
    }
}