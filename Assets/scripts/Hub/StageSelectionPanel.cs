using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
    private List<int> stageIds; // Actual stage IDs from database
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
        
        // Get actual stage IDs from database (excluding Hub stage 100)
        stageIds = stageDatabase?.GetAllStageIds()
            .Where(id => id < 100) // Exclude Hub and special stages
            .OrderBy(id => id)
            .ToList() ?? new List<int>();
        
        int unlocked = SaveManager.IsInitialized ? SaveManager.Instance.Progression.highestStageUnlocked : 0;
        
        for (int i = 0; i < stageButtons.Length; i++)
        {
            var btn = stageButtons[i];
            if (btn == null) continue;
            
            bool hasStage = i < stageIds.Count;
            int stageId = hasStage ? stageIds[i] : -1;
            bool isUnlocked = hasStage && stageId <= unlocked;
            
            // Get stage name from database
            string stageName = hasStage 
                ? $"{stageId}"
                : "---";
            
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
            
            // Wire up click with actual stage ID
            btn.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                int id = stageId; // Capture actual stage ID
                btn.onClick.AddListener(() => SelectStage(id));
            }
        }
        
        Debug.Log($"[StageSelectionPanel] Wired {stageButtons.Length} buttons for {stageIds.Count} stages, unlocked up to stage {unlocked}");
    }
    
    private void SelectStage(int stageId)
    {
        string stageName = stageDatabase?.GetStageReference(stageId)?.stageName ?? $"Stage {stageId}";
        Debug.Log($"[StageSelectionPanel] Selected stage {stageId}: {stageName}");
        
        // Close panel first
        Close();
        
        // Use StageManager to load the stage - same as any other stage transition
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null)
        {
            stageManager.LoadStage(stageId);
        }
        else
        {
            Debug.LogError("[StageSelectionPanel] StageManager not found!");
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
