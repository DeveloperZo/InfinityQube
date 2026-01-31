using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI panel for stage selection in the Hub.
/// Displays available stages and handles stage selection.
/// </summary>
public class StageSelectionPanel : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("UI References")]
    [SerializeField] private Transform stageButtonContainer;
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private Text panelTitleText;
    [SerializeField] private Button closeButton;
    
    [Header("Stage Database")]
    [SerializeField] private StageDB stageDatabase;
    
    [Header("Stage Display")]
    [SerializeField] private int maxStagesDisplayed = 12;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f);
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private List<StageButtonData> stageButtons = new List<StageButtonData>();
    private int selectedStageIndex = -1;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        // Load stage database from Resources if not assigned
        if (stageDatabase == null)
        {
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
            if (stageDatabase == null)
            {
                Debug.LogWarning("[StageSelectionPanel] StageDatabase not found in Resources!");
            }
        }
        
        // Initialize database
        if (stageDatabase != null)
        {
            stageDatabase.Initialize();
        }
    }
    
    private void OnEnable()
    {
        RefreshStageList();
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
        
        ClearStageButtons();
    }
    
    #endregion
    
    #region Stage List Management
    
    /// <summary>
    /// Refreshes the stage list based on current progression.
    /// </summary>
    public void RefreshStageList()
    {
        ClearStageButtons();
        
        int highestUnlocked = GetHighestUnlockedStage();
        int totalStages = GetTotalStageCount();
        int stagesToShow = Mathf.Min(totalStages, maxStagesDisplayed);
        
        DebugLog($"Refreshing stage list. Highest unlocked: {highestUnlocked}, Total: {totalStages}");
        
        for (int i = 0; i < stagesToShow; i++)
        {
            CreateStageButton(i, i <= highestUnlocked);
        }
        
        // Update title
        if (panelTitleText != null)
        {
            panelTitleText.text = "Select Stage";
        }
    }
    
    private void ClearStageButtons()
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.button != null)
            {
                buttonData.button.onClick.RemoveAllListeners();
                Destroy(buttonData.buttonObject);
            }
        }
        stageButtons.Clear();
    }
    
    private void CreateStageButton(int stageIndex, bool isUnlocked)
    {
        if (stageButtonContainer == null || stageButtonPrefab == null)
        {
            DebugLog("Cannot create stage button - container or prefab is null");
            return;
        }
        
        GameObject buttonObj = Instantiate(stageButtonPrefab, stageButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        
        if (button == null)
        {
            Debug.LogWarning($"[StageSelectionPanel] Stage button prefab missing Button component");
            Destroy(buttonObj);
            return;
        }
        
        // Configure button appearance
        string stageName = GetStageName(stageIndex);
        if (buttonText != null)
        {
            buttonText.text = isUnlocked ? stageName : $"{stageName} (Locked)";
        }
        
        // Set button color
        var colors = button.colors;
        colors.normalColor = isUnlocked ? unlockedColor : lockedColor;
        colors.disabledColor = lockedColor;
        button.colors = colors;
        
        // Configure interactability
        button.interactable = isUnlocked;
        
        // Add click handler
        int capturedIndex = stageIndex; // Capture for closure
        button.onClick.AddListener(() => OnStageButtonClicked(capturedIndex));
        
        // Store reference
        stageButtons.Add(new StageButtonData
        {
            stageIndex = stageIndex,
            buttonObject = buttonObj,
            button = button,
            isUnlocked = isUnlocked
        });
        
        DebugLog($"Created button for Stage {stageIndex + 1}: {stageName} (Unlocked: {isUnlocked})");
    }
    
    #endregion
    
    #region Button Handlers
    
    private void OnStageButtonClicked(int stageIndex)
    {
        DebugLog($"Stage {stageIndex + 1} selected");
        
        selectedStageIndex = stageIndex;
        
        // Update visual selection
        UpdateButtonSelection();
        
        // Start the stage via HubManager
        if (HubManager.IsInitialized)
        {
            HubManager.Instance.StartStage(stageIndex);
        }
        else
        {
            // Fallback: Direct scene transition
            Debug.LogWarning("[StageSelectionPanel] HubManager not found, using direct transition");
            PlayerPrefs.SetInt("SelectedStage", stageIndex);
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Stage");
        }
    }
    
    private void OnCloseClicked()
    {
        DebugLog("Close button clicked");
        
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.CloseAllPanels();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void UpdateButtonSelection()
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.button == null) continue;
            
            var colors = buttonData.button.colors;
            
            if (buttonData.stageIndex == selectedStageIndex)
            {
                colors.normalColor = selectedColor;
            }
            else
            {
                colors.normalColor = buttonData.isUnlocked ? unlockedColor : lockedColor;
            }
            
            buttonData.button.colors = colors;
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private int GetHighestUnlockedStage()
    {
        if (SaveManager.IsInitialized)
        {
            return SaveManager.Instance.Progression.highestStageUnlocked;
        }
        return 0; // Default to only first stage unlocked
    }
    
    private int GetTotalStageCount()
    {
        // Try to get from stageDatabase if available
        if (stageDatabase != null)
        {
            return stageDatabase.StageCount;
        }
        return maxStagesDisplayed; // Fallback
    }
    
    private string GetStageName(int stageIndex)
    {
        // Try to get from stageDatabase
        if (stageDatabase != null)
        {
            var stageData = stageDatabase.GetStageReference(stageIndex);
            if (stageData != null && !string.IsNullOrEmpty(stageData.stageName))
            {
                return stageData.stageName;
            }
        }
        return $"Stage {stageIndex + 1}";
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[StageSelectionPanel] {message}");
        }
    }
    
    #endregion
    
    #region Data Structures
    
    private class StageButtonData
    {
        public int stageIndex;
        public GameObject buttonObject;
        public Button button;
        public bool isUnlocked;
    }
    
    #endregion
}
