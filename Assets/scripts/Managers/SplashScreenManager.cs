using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string nextSceneName = "Stage";
    [SerializeField] private int hubStageIndex = 100; // Hub stage index in StageDB

    [Header("Playback Options")]
    #pragma warning disable CS0414 // Reserved for future skip delay implementation
    [SerializeField] private bool canSkip = true;
    [SerializeField] private float skipDelay = 0.5f;
    #pragma warning restore CS0414
    [SerializeField] private bool showDebugLogs = true;

    [Header("UI References")]
    [SerializeField] public Button startButton;
    [SerializeField] public Button settingsButton;
    [SerializeField] public Button exitButton;
    [SerializeField] public Button closeSettingsButton;
    [SerializeField] public GameObject settingsPopup;
    [SerializeField] public GameObject title;
    [SerializeField] public GameObject mainMenu;

    private void Awake()
    {
        title.SetActive(true);
        mainMenu.SetActive(false);
        settingsPopup.SetActive(false);

        // Auto-find UI elements if not assigned
        if (startButton == null) startButton = FindButtonByName("Start");
        if (settingsButton == null) settingsButton = FindButtonByName("Settings");
        if (exitButton == null) exitButton = FindButtonByName("Exit");
        if (closeSettingsButton == null) closeSettingsButton = FindButtonByName("Button_Close");
        if (settingsPopup == null) settingsPopup = FindGameObjectByName("Popup_Settings");
        if (mainMenu == null) mainMenu = FindGameObjectByName("Menu");
    }

    private void Start()
    {
        SetupUICallbacks();

        // Start with settings popup hidden
        if (settingsPopup != null)
            settingsPopup.SetActive(false);
    }

    private void Update()
    {
        // Keep existing keyboard skip functionality
        if (Input.GetKeyDown(KeyCode.Space)  ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0) && title.activeSelf)
        {
            title.SetActive(false);
            mainMenu.SetActive(true);
        }
    }

    #region UI Setup
    private void SetupUICallbacks()
    {
        if (startButton != null)
            startButton.onClick.AddListener(HandleStartButton);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(HandleSettingsButton);

        if (exitButton != null)
            exitButton.onClick.AddListener(HandleExitButton);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(HandleCloseSettingsButton);

        if (showDebugLogs)
        {
            Debug.Log($"SplashScreenManager: UI setup complete. Found buttons - Start: {startButton != null}, Settings: {settingsButton != null}, Exit: {exitButton != null}, Close: {closeSettingsButton != null}");
        }
    }

    private Button FindButtonByName(string buttonName)
    {
        GameObject obj = GameObject.Find(buttonName);
        return obj?.GetComponent<Button>();
    }

    private GameObject FindGameObjectByName(string objectName)
    {
        return GameObject.Find(objectName);
    }
    #endregion

    #region Button Handlers
    private void HandleStartButton()
    {
        if (showDebugLogs)
            Debug.Log("SplashScreenManager: Start button clicked");

        LoadNextScene();
    }

    private void HandleSettingsButton()
    {
        if (showDebugLogs)
            Debug.Log("SplashScreenManager: Settings button clicked");

        ShowSettingsPopup();
    }

    private void HandleExitButton()
    {
        if (showDebugLogs)
            Debug.Log("SplashScreenManager: Exit button clicked");

        ExitGame();
    }

    private void HandleCloseSettingsButton()
    {
        if (showDebugLogs)
            Debug.Log("SplashScreenManager: Close settings button clicked");

        HideSettingsPopup();
    }
    #endregion

    #region Navigation Methods
    private void LoadNextScene()
    {
        if (showDebugLogs)
            Debug.Log($"SplashScreenManager: Loading scene '{nextSceneName}' with Hub stage {hubStageIndex}");

        // Set SelectedStage to Hub stage index so StageManager loads Hub
        PlayerPrefs.SetInt("SelectedStage", hubStageIndex);
        PlayerPrefs.Save();

        // Use Single mode to ensure current scene is fully unloaded before loading new scene
        // This prevents object duplication when returning to splash and starting again
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }

    private void ShowSettingsPopup()
    {
        if (settingsPopup != null)
        {
            mainMenu.SetActive(false);
            settingsPopup.SetActive(true);

            if (showDebugLogs)
                Debug.Log("SplashScreenManager: Settings popup shown");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("SplashScreenManager: Settings popup not found!");
        }
    }

    private void HideSettingsPopup()
    {
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(false);
            mainMenu.SetActive(true);

            if (showDebugLogs)
                Debug.Log("SplashScreenManager: Settings popup hidden");
        }
    }

    private void ExitGame()
    {
        if (showDebugLogs)
            Debug.Log("SplashScreenManager: Exiting game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Manually trigger scene load (useful for other scripts)
    /// </summary>
    public void LoadScene()
    {
        LoadNextScene();
    }

    /// <summary>
    /// Toggle settings popup visibility
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPopup != null)
        {
            bool isActive = settingsPopup.activeSelf;
            settingsPopup.SetActive(!isActive);
        }
    }
    #endregion
}