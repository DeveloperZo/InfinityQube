using UnityEngine;

/// <summary>
/// Manages hub UI panels. Opens/closes panels based on building clicks.
/// Attach to a persistent object in the Hub scene.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    #region Singleton
    
    private static HubUIManager _instance;
    public static HubUIManager Instance => _instance;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("UI Panels")]
    [SerializeField] private GameObject stageSelectionPanel;
    [SerializeField] private GameObject attunementPanel;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    
    #endregion
    
    #region Runtime State
    
    private GameObject currentPanel;
    
    #endregion
    
    #region Properties
    
    public bool IsPanelOpen => currentPanel != null && currentPanel.activeSelf;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Ensure all panels start closed
        CloseAllPanels();
    }
    
    private void Update()
    {
        // Close panel on Escape
        if (Input.GetKeyDown(closeKey) && IsPanelOpen)
        {
            CloseAllPanels();
        }
    }
    
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
    
    #endregion
    
    #region Panel Management
    
    /// <summary>
    /// Opens the panel corresponding to the building type.
    /// </summary>
    public void OpenPanel(HubBuildingType buildingType)
    {
        CloseAllPanels();
        
        GameObject panelToOpen = GetPanelForBuilding(buildingType);
        if (panelToOpen != null)
        {
            panelToOpen.SetActive(true);
            currentPanel = panelToOpen;
            Debug.Log($"[HubUIManager] Opened {buildingType} panel");
        }
        else
        {
            Debug.LogWarning($"[HubUIManager] No panel assigned for {buildingType}");
        }
    }
    
    /// <summary>
    /// Closes all panels.
    /// </summary>
    public void CloseAllPanels()
    {
        if (stageSelectionPanel != null) stageSelectionPanel.SetActive(false);
        if (attunementPanel != null) attunementPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        
        currentPanel = null;
    }
    
    /// <summary>
    /// Closes the currently open panel.
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }
    }
    
    private GameObject GetPanelForBuilding(HubBuildingType buildingType)
    {
        return buildingType switch
        {
            HubBuildingType.CelestialAtlas => stageSelectionPanel,
            HubBuildingType.ResonanceAlignmentChamber => attunementPanel,
            HubBuildingType.ObservationChronicle => statsPanel,
            _ => null
        };
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Directly open stage selection panel.
    /// </summary>
    public void OpenStageSelection() => OpenPanel(HubBuildingType.CelestialAtlas);
    
    /// <summary>
    /// Directly open attunement panel.
    /// </summary>
    public void OpenAttunements() => OpenPanel(HubBuildingType.ResonanceAlignmentChamber);
    
    /// <summary>
    /// Directly open stats panel.
    /// </summary>
    public void OpenStats() => OpenPanel(HubBuildingType.ObservationChronicle);
    
    #endregion
}

