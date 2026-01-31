using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Wires up the Layer Lab StageSelect prefab with game data.
/// Place the prefab in scene, attach this script, assign references in inspector.
/// </summary>
public class StageSelectionPanel : MonoBehaviour
{
    [Header("UI References (assign in inspector)")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button stageButtonTemplate;
    
    [Header("Data")]
    [SerializeField] private StageDB stageDatabase;
    [SerializeField] private int maxStages = 12;
    
    private List<Button> stageButtons = new List<Button>();
    
    private void Awake()
    {
        if (stageDatabase == null)
            stageDatabase = Resources.Load<StageDB>("StageDatabase");
        stageDatabase?.Initialize();
    }
    
    private void OnEnable()
    {
        closeButton?.onClick.AddListener(Close);
        RefreshStages();
    }
    
    private void OnDisable()
    {
        closeButton?.onClick.RemoveListener(Close);
    }
    
    private void RefreshStages()
    {
        // Clear old buttons (except template)
        foreach (var btn in stageButtons)
            if (btn != null) Destroy(btn.gameObject);
        stageButtons.Clear();
        
        if (buttonContainer == null || stageButtonTemplate == null) return;
        
        int unlocked = SaveManager.IsInitialized ? SaveManager.Instance.Progression.highestStageUnlocked : 0;
        int count = Mathf.Min(stageDatabase?.StageCount ?? maxStages, maxStages);
        
        stageButtonTemplate.gameObject.SetActive(false);
        
        for (int i = 0; i < count; i++)
        {
            var btn = Instantiate(stageButtonTemplate, buttonContainer);
            btn.gameObject.SetActive(true);
            
            string name = stageDatabase?.GetStageReference(i)?.stageName ?? $"Stage {i + 1}";
            bool isUnlocked = i <= unlocked;
            
            // Set button text
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = isUnlocked ? name : $"{name} (Locked)";
            else
            {
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = isUnlocked ? name : $"{name} (Locked)";
            }
            
            btn.interactable = isUnlocked;
            
            int idx = i;
            btn.onClick.AddListener(() => SelectStage(idx));
            stageButtons.Add(btn);
        }
    }
    
    private void SelectStage(int index)
    {
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
        if (HubUIManager.Instance != null)
            HubUIManager.Instance.CloseAllPanels();
        else
            gameObject.SetActive(false);
    }
}
