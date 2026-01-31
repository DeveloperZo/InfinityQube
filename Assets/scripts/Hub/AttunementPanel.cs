using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POC: Placeholder panel for attunement selection in the Hub.
/// Will be expanded in Milestone 1.11 (RPG Implementation).
/// </summary>
public class AttunementPanel : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text axiomShardsText;
    
    [Header("Placeholder Content")]
    [SerializeField] private string placeholderTitle = "Resonance Alignment Chamber";
    [SerializeField, TextArea] private string placeholderDescription = 
        "Attunements will allow you to customize your marker abilities.\n\n" +
        "Coming soon in a future update!";
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }
    
    private void OnEnable()
    {
        RefreshPanel();
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
    
    #endregion
    
    #region Panel Management
    
    /// <summary>
    /// Refreshes panel content.
    /// </summary>
    public void RefreshPanel()
    {
        if (titleText != null)
        {
            titleText.text = placeholderTitle;
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = placeholderDescription;
        }
        
        UpdateAxiomShards();
    }
    
    private void UpdateAxiomShards()
    {
        if (axiomShardsText != null)
        {
            int shards = 0;
            if (SaveManager.IsInitialized)
            {
                shards = SaveManager.Instance.AxiomShards;
            }
            axiomShardsText.text = $"Axiom Shards: {shards}";
        }
    }
    
    #endregion
    
    #region Button Handlers
    
    private void OnCloseClicked()
    {
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.CloseAllPanels();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    #endregion
}
