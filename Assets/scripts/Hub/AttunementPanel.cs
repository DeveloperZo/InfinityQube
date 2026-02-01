using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires up the Layer Lab Equipment prefab for attunements.
/// Place the prefab in scene, attach this script. Auto-finds close button.
/// POC: Will be expanded in Milestone 1.11 (RPG Implementation).
/// </summary>
public class AttunementPanel : MonoBehaviour
{
    [Header("UI References (auto-found if not assigned)")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI shardsText;
    
    [Header("Content")]
    [SerializeField] private string title = "Resonance Alignment Chamber";
    [SerializeField, TextArea] private string description = 
        "Attunements allow you to customize your marker abilities.\n\nComing soon in a future update!";
    
    private void Awake()
    {
        FindCloseButton();
    }
    
    private void FindCloseButton()
    {
        if (closeButton != null) return;
        
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn.name.Contains("Back") || btn.name.Contains("Close") || btn.name.Contains("Exit"))
            {
                closeButton = btn;
                Debug.Log($"[AttunementPanel] Found close button: {btn.name}");
                break;
            }
        }
    }
    
    private void OnEnable()
    {
        FindCloseButton();
        closeButton?.onClick.AddListener(Close);
        Refresh();
    }
    
    private void OnDisable()
    {
        closeButton?.onClick.RemoveListener(Close);
    }
    
    private void Refresh()
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        
        int shards = SaveManager.IsInitialized ? SaveManager.Instance.AxiomShards : 0;
        if (shardsText != null) shardsText.text = $"Axiom Shards: {shards}";
    }
    
    private void Close()
    {
        if (HubUIManager.Instance != null)
            HubUIManager.Instance.CloseAllPanels();
        else
            gameObject.SetActive(false);
    }
}
