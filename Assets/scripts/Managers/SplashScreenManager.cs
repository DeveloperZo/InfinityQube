using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [SerializeField] public VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "Sandbox";
    [SerializeField] private bool canSkip = true;

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("VideoPlayer component missing from SplashScreenManager!");
                LoadNextScene();
                return;
            }
        }

        // Set up the video player
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    private void Update()
    {
        // Allow skipping if enabled
        if (canSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
        {
            LoadNextScene();
        }
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}