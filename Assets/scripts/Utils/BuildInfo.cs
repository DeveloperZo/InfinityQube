// Add this to a new script for build info
using UnityEngine;

public class BuildInfo : MonoBehaviour
{
    [Header("Build Information")]
    public string buildVersion = "0.1.0-alpha";
    public string buildDate;

    // Add to BuildInfo.cs around line 15
    private void Awake()
    {
        // Show build info in development builds
        if (Debug.isDebugBuild)
        {
            buildDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            DontDestroyOnLoad(gameObject);

        }
    }

    private void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, Screen.height - 60, 400, 40),
                     $"Build: {buildVersion} | {buildDate}\nFriend Test Build");
        }
    }
}