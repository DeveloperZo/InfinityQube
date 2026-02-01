using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires up the Layer Lab StageSelect prefab with game data.
/// Place the prefab in scene, attach this script. Finds buttons automatically.
/// </summary>
public class StageSelectionPanel : MonoBehaviour
{
    [Header("UI References (auto-found if not assigned)")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button closeButton;
    
    [Header("Data")]
    [SerializeField] private StageDB stageDatabase;
    
    private Button[] stageButtons;
    private bool initialized;
    
    private void Awake()
    {
        if (stageDatabase == null)
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
        stageDatabase?.Initialize();
        
        FindReferences();
    }
    
    private void FindReferences()
    {
        if (initialized) return;
        
        // Find button container (Stage_Group with GridLayoutGroup)
        if (buttonContainer == null)
        {
            var grid = GetComponentInChildren<GridLayoutGroup>(true);
            if (grid != null) buttonContainer = grid.transform;
        }
        
        // Find close button (Button_Back or any button outside container)
        if (closeButton == null)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn.name.Contains("Back") || btn.name.Contains("Close") || btn.name.Contains("Exit"))
                {
                    closeButton = btn;
                    break;
                }
            }
        }
        
        // Cache stage buttons from container
        if (buttonContainer != null)
            stageButtons = buttonContainer.GetComponentsInChildren<Button>(true);
        else
            stageButtons = new Button[0];
        
        Debug.Log($"[StageSelectionPanel] Found {stageButtons.Length} stage buttons, closeButton={closeButton?.name ?? "null"}");
        initialized = true;
    }
    
    private void OnEnable()
    {
        FindReferences();
        closeButton?.onClick.AddListener(Close);
        WireUpButtons();
    }
    
    private void OnDisable()
    {
        closeButton?.onClick.RemoveListener(Close);
        
        // Remove listeners from stage buttons
        if (stageButtons != null)
        {
            foreach (var btn in stageButtons)
                btn?.onClick.RemoveAllListeners();
        }
    }
    
    private void WireUpButtons()
    {
        if (stageButtons == null || stageButtons.Length == 0)
        {
            Debug.LogWarning("[StageSelectionPanel] No stage buttons found");
            return;
        }
        
        int unlocked = SaveManager.IsInitialized ? SaveManager.Instance.Progression.highestStageUnlocked : 0;
        int stageCount = stageDatabase?.StageCount ?? stageButtons.Length;
        
        for (int i = 0; i < stageButtons.Length; i++)
        {
            var btn = stageButtons[i];
            if (btn == null) continue;
            
            bool hasStage = i < stageCount;
            bool isUnlocked = hasStage && i <= unlocked;
            
            // Get stage name
            string stageName = $"{i + 1}";
            
            // Update button text
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = isUnlocked ? stageName : (hasStage ? $"{stageName} (Locked)" : "---");
            else
            {
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                    txt.text = isUnlocked ? stageName : (hasStage ? $"{stageName} (Locked)" : "---");
            }
            
            btn.interactable = isUnlocked;
            btn.gameObject.SetActive(hasStage);
            
            // Wire up click
            btn.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                int idx = i;
                btn.onClick.AddListener(() => SelectStage(idx));
            }
        }
        
        Debug.Log($"[StageSelectionPanel] Wired {stageButtons.Length} buttons, {unlocked + 1} unlocked");
    }
    
    private void SelectStage(int index)
    {
        Debug.Log($"[StageSelectionPanel] Selected stage {index}");
        
        if (HubManager.IsInitialized)
            HubManager.Instance.StartStage(index);
        else
        {
            PlayerPrefs.SetInt("SelectedStage", index);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Stage");
        }
    }
    
    private void Close()
    {
        Debug.Log("[StageSelectionPanel] Close button clicked");
        
        if (HubUIManager.Instance != null)
            HubUIManager.Instance.CloseAllPanels();
        else
            gameObject.SetActive(false);
    }
}
