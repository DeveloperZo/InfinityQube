using UnityEngine;

/// <summary>
/// Manages hub UI panels. Opens/closes panels based on building interactions.
/// Panels should be placed in the scene. Auto-finds if not assigned.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    public static HubUIManager Instance { get; private set; }
    
    [Header("Panels (auto-found if not assigned)")]
    [SerializeField] private GameObject stageSelectionPanel;
    [SerializeField] private GameObject attunementPanel;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    
    [Header("Debug")]
    [SerializeField] private bool autoOpenStagePanel = false; // Set true to test panel visibility
    
    public bool IsPanelOpen => 
        (stageSelectionPanel != null && stageSelectionPanel.activeSelf) ||
        (attunementPanel != null && attunementPanel.activeSelf) ||
        (statsPanel != null && statsPanel.activeSelf);
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        FindPanels();
        CloseAllPanels();
    }
    
    private void Start()
    {
        // Auto-open for testing
        if (autoOpenStagePanel && stageSelectionPanel != null)
        {
            Debug.Log("[HubUIManager] Auto-opening StageSelectionPanel for testing");
            OpenStageSelection();
        }
    }
    
    private void FindPanels()
    {
        // Auto-find panels if not assigned
        if (stageSelectionPanel == null)
        {
            var panel = FindObjectOfType<StageSelectionPanel>(true);
            if (panel != null) stageSelectionPanel = panel.gameObject;
        }
        
        if (attunementPanel == null)
        {
            var panel = FindObjectOfType<AttunementPanel>(true);
            if (panel != null) attunementPanel = panel.gameObject;
        }
        
        if (statsPanel == null)
        {
            var panel = FindObjectOfType<StatsPanel>(true);
            if (panel != null) statsPanel = panel.gameObject;
        }
        
        Debug.Log($"[HubUIManager] Panels found - Stage:{stageSelectionPanel != null}, Attunement:{attunementPanel != null}, Stats:{statsPanel != null}");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(closeKey) && IsPanelOpen)
            CloseAllPanels();
        
        // Debug keys for testing panels
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1)) OpenStageSelection();
        if (Input.GetKeyDown(KeyCode.F2)) OpenAttunements();
        if (Input.GetKeyDown(KeyCode.F3)) OpenStats();
        #endif
    }
    
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    public void OpenPanel(HubBuildingType type)
    {
        CloseAllPanels();
        
        var panel = type switch
        {
            HubBuildingType.CelestialAtlas => stageSelectionPanel,
            HubBuildingType.ResonanceAlignmentChamber => attunementPanel,
            HubBuildingType.ObservationChronicle => statsPanel,
            _ => null
        };
        
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log($"[HubUIManager] Opened {type} panel");
        }
    }
    
    public void CloseAllPanels()
    {
        bool wasOpen = IsPanelOpen;
        if (stageSelectionPanel != null) stageSelectionPanel.SetActive(false);
        if (attunementPanel != null) attunementPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        
        if (wasOpen)
            Debug.Log("[HubUIManager] All panels closed");
    }
    
    public void OpenStageSelection() => OpenPanel(HubBuildingType.CelestialAtlas);
    public void OpenAttunements() => OpenPanel(HubBuildingType.ResonanceAlignmentChamber);
    public void OpenStats() => OpenPanel(HubBuildingType.ObservationChronicle);
}
