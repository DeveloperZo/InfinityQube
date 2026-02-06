using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wires up the Layer Lab Character prefab for player stats.
/// Place the prefab in scene, attach this script. Auto-finds close button.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("UI References (auto-found if not assigned)")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statsText;
    
    [Header("Content")]
    [SerializeField] private string title = "Observation Chronicle";
    
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
                Debug.Log($"[StatsPanel] Found close button: {btn.name}");
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
        if (statsText != null) statsText.text = BuildStats();
    }
    
    private string BuildStats()
    {
        if (!SaveManager.IsInitialized)
            return "Statistics unavailable.\n\nProgress will be tracked once you begin playing.";
        
        var p = SaveManager.Instance.Progression;
        return $"<b>Progress</b>\n" +
               $"Highest Stage: {p.highestStageUnlocked + 1}\n" +
               $"Axiom Shards: {p.axiomShards}\n\n" +
               $"<b>Lifetime Statistics</b>\n" +
               $"Cubes Captured: {p.lifetimeCubesCaptured}\n" +
               $"Cubes Escaped: {p.lifetimeCubesEscaped}\n" +
               $"Stages Completed: {p.lifetimeStagesCompleted}\n" +
               $"Play Time: {FormatTime(p.lifetimePlayTimeSeconds)}\n\n" +
               $"<b>Buildings</b>\n" +
               $"Celestial Atlas: Always\n" +
               $"Resonance Chamber: {(p.resonanceAlignmentUnlocked ? "Yes" : "No")}\n" +
               $"Chronicle: {(p.observationChronicleUnlocked ? "Yes" : "No")}";
    }
    
    private string FormatTime(float s)
    {
        if (s < 60) return $"{(int)s}s";
        if (s < 3600) return $"{(int)(s/60)}m {(int)(s%60)}s";
        return $"{(int)(s/3600)}h {(int)((s%3600)/60)}m";
    }
    
    private void Close()
    {
        if (HubUIManager.Instance != null)
            HubUIManager.Instance.CloseAllPanels();
        else
            gameObject.SetActive(false);
    }
}
