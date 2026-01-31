using UnityEngine;

/// <summary>
/// Manages hub UI panels. Opens/closes panels based on building interactions.
/// Panels should be placed in the scene and assigned in inspector.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    public static HubUIManager Instance { get; private set; }
    
    [Header("Panels (assign in inspector)")]
    [SerializeField] private GameObject stageSelectionPanel;
    [SerializeField] private GameObject attunementPanel;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Settings")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    
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
        CloseAllPanels();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(closeKey) && IsPanelOpen)
            CloseAllPanels();
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
        if (stageSelectionPanel != null) stageSelectionPanel.SetActive(false);
        if (attunementPanel != null) attunementPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
    }
    
    public void OpenStageSelection() => OpenPanel(HubBuildingType.CelestialAtlas);
    public void OpenAttunements() => OpenPanel(HubBuildingType.ResonanceAlignmentChamber);
    public void OpenStats() => OpenPanel(HubBuildingType.ObservationChronicle);
}
