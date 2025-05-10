using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.IO;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Video Configuration")]
    [SerializeField] private string nextSceneName = "Sandbox";

    [Header("Playback Options")]
    [SerializeField] private bool canSkip = true;
    [SerializeField] private float skipDelay = 0.5f;
    [SerializeField] private bool showDebugLogs = true;


    private void Awake()
    {
    }



    private void Update()
    {

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                LoadNextScene();
            }
        

    }

    private void LoadNextScene()
    {

        // Don't unload the current scene first - just load the new scene
        // This is more reliable for scene transitions
        SceneManager.LoadScene(nextSceneName);
    }

}