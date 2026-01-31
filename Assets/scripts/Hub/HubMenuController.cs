using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple main menu controller for the Hub scene.
/// Provides buttons to enter hub world, select stages, view attunements/stats.
/// Auto-creates UI if buttons not assigned in Inspector.
/// </summary>
public class HubMenuController : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("Buttons")]
    [SerializeField] private Button enterHubButton;
    [SerializeField] private Button stageSelectButton;
    [SerializeField] private Button attunementButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Title")]
    [SerializeField] private Text titleText;
    
    [Header("Auto-Create Settings")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private Font buttonFont;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        // Auto-create UI if not assigned
        if (autoCreateUI && enterHubButton == null)
        {
            CreateUI();
        }
        
        SetupButtons();
        
        // Set title if assigned
        if (titleText != null)
        {
            titleText.text = "Infinity's Axiom";
        }
    }
    
    private void OnDestroy()
    {
        CleanupButtons();
    }
    
    #endregion
    
    #region Setup
    
    private void CreateUI()
    {
        // Get or create RectTransform
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = gameObject.AddComponent<RectTransform>();
        }
        
        // Center and size the panel
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 500);
        rt.anchoredPosition = Vector2.zero;
        
        // Add vertical layout
        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Add background
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Create title
        titleText = CreateText("Infinity's Axiom", 42);
        titleText.fontStyle = FontStyle.Bold;
        
        // Create spacer
        CreateSpacer(30);
        
        // Create buttons
        enterHubButton = CreateButton("Enter Hub World", new Color(0.2f, 0.5f, 0.8f));
        stageSelectButton = CreateButton("Stage Select", new Color(0.3f, 0.3f, 0.4f));
        attunementButton = CreateButton("Attunements", new Color(0.3f, 0.3f, 0.4f));
        statsButton = CreateButton("Statistics", new Color(0.3f, 0.3f, 0.4f));
        
        // Create spacer
        CreateSpacer(20);
        
        quitButton = CreateButton("Quit", new Color(0.5f, 0.2f, 0.2f));
        
        Debug.Log("[HubMenuController] UI created automatically");
    }
    
    private Button CreateButton(string label, Color bgColor)
    {
        GameObject btnObj = new GameObject(label.Replace(" ", "") + "Button");
        btnObj.transform.SetParent(transform, false);
        
        // RectTransform
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);
        
        // Background image
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        // Button component
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        // Set up color block for hover/press
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        colors.selectedColor = bgColor;
        btn.colors = colors;
        
        // Create text child
        Text txt = CreateText(label, 24, btnObj.transform);
        txt.alignment = TextAnchor.MiddleCenter;
        
        // Stretch text to fill button
        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.anchoredPosition = Vector2.zero;
        
        return btn;
    }
    
    private Text CreateText(string content, int fontSize, Transform parent = null)
    {
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(parent ?? transform, false);
        
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, fontSize + 20);
        
        Text txt = txtObj.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        
        // Use assigned font or default
        if (buttonFont != null)
        {
            txt.font = buttonFont;
        }
        else
        {
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        return txt;
    }
    
    private void CreateSpacer(float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(transform, false);
        
        RectTransform rt = spacer.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);
        
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }
    
    private void SetupButtons()
    {
        // Enter Hub World - loads HubStage in Stage scene
        if (enterHubButton != null)
        {
            enterHubButton.onClick.AddListener(OnEnterHubClicked);
        }
        
        // Stage Selection - opens panel
        if (stageSelectButton != null)
        {
            stageSelectButton.onClick.AddListener(OnStageSelectClicked);
        }
        
        // Attunement - opens panel
        if (attunementButton != null)
        {
            attunementButton.onClick.AddListener(OnAttunementClicked);
        }
        
        // Stats - opens panel
        if (statsButton != null)
        {
            statsButton.onClick.AddListener(OnStatsClicked);
        }
        
        // Quit
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    private void CleanupButtons()
    {
        if (enterHubButton != null) enterHubButton.onClick.RemoveListener(OnEnterHubClicked);
        if (stageSelectButton != null) stageSelectButton.onClick.RemoveListener(OnStageSelectClicked);
        if (attunementButton != null) attunementButton.onClick.RemoveListener(OnAttunementClicked);
        if (statsButton != null) statsButton.onClick.RemoveListener(OnStatsClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
    }
    
    #endregion
    
    #region Button Handlers
    
    private void OnEnterHubClicked()
    {
        Debug.Log("[HubMenuController] Enter Hub clicked");
        if (HubManager.IsInitialized)
        {
            HubManager.Instance.EnterHubWorld();
        }
        else
        {
            Debug.LogWarning("[HubMenuController] HubManager not initialized");
        }
    }
    
    private void OnStageSelectClicked()
    {
        Debug.Log("[HubMenuController] Stage Select clicked");
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenStageSelection();
        }
    }
    
    private void OnAttunementClicked()
    {
        Debug.Log("[HubMenuController] Attunement clicked");
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenAttunements();
        }
    }
    
    private void OnStatsClicked()
    {
        Debug.Log("[HubMenuController] Stats clicked");
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenStats();
        }
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("[HubMenuController] Quit clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
}
