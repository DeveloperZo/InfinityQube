using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Video Configuration")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage displayImage; // UI element to display video
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Playback Options")]
    [SerializeField] private bool canSkip = true;
    [SerializeField] private float skipDelay = 0.5f;
    [SerializeField] private bool showDebugLogs = true;

    private bool videoStarted = false;
    private float timeElapsed = 0f;

    private void Awake()
    {
        // Find components if not assigned
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            LogDebug("VideoPlayer not assigned, finding component on GameObject");
        }

        if (displayImage == null && videoPlayer != null && videoPlayer.renderMode == VideoRenderMode.RenderTexture)
        {
            // Try to find a RawImage in children
            displayImage = GetComponentInChildren<RawImage>();
            LogDebug("RawImage not assigned, searching in children: " + (displayImage != null ? "Found" : "Not found"));
        }

        // Validate required components
        if (videoPlayer == null)
        {
            LogError("VideoPlayer component missing from SplashScreenManager!");
            LoadNextScene();
            return;
        }

        // Configure video player if needed
        if (videoPlayer.renderMode == VideoRenderMode.RenderTexture && videoPlayer.targetTexture == null)
        {
            if (displayImage != null)
            {
                LogDebug("Creating render texture for video");
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
                videoPlayer.targetTexture = renderTexture;
                displayImage.texture = renderTexture;
            }
            else
            {
                LogError("Video set to RenderTexture mode but no RawImage or target texture assigned!");
                LoadNextScene();
                return;
            }
        }

        // Set up video callbacks
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        LogDebug("SplashScreenManager initialized successfully");
    }

    private void Start()
    {
        LogDebug("Preparing video for playback");
        videoPlayer.Prepare();
    }

    private void Update()
    {
        // Skip video logic
        if (videoStarted && canSkip && timeElapsed > skipDelay)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                LogDebug("User skipped video");
                LoadNextScene();
            }
        }

        if (videoStarted)
        {
            timeElapsed += Time.deltaTime;

            // Log playback status periodically for debugging
            if (showDebugLogs && Time.frameCount % 300 == 0) // Every ~5 seconds
            {
                LogDebug($"Video playback: {videoPlayer.time:F1}s / {videoPlayer.length:F1}s");
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        LogDebug("Video prepared, beginning playback");
        videoPlayer.Play();
        videoStarted = true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        LogError($"Video Error: {message}");
        LoadNextScene();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LogDebug("Video playback completed");
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        LogDebug($"Loading next scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[SplashScreen] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SplashScreen] {message}");
    }
}