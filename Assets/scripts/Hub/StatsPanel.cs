using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POC: Placeholder panel for player statistics in the Hub.
/// Will be expanded in Milestone 1.11 (RPG Implementation).
/// </summary>
public class StatsPanel : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsContentText;
    [SerializeField] private Button closeButton;
    
    [Header("Placeholder Content")]
    [SerializeField] private string placeholderTitle = "Observation Chronicle";
    
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
        RefreshStats();
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
    
    #endregion
    
    #region Stats Display
    
    /// <summary>
    /// Refreshes the stats display.
    /// </summary>
    public void RefreshStats()
    {
        if (titleText != null)
        {
            titleText.text = placeholderTitle;
        }
        
        if (statsContentText != null)
        {
            statsContentText.text = BuildStatsText();
        }
    }
    
    private string BuildStatsText()
    {
        if (!SaveManager.IsInitialized)
        {
            return "Statistics unavailable.\n\nProgress will be tracked once you begin playing.";
        }
        
        var progression = SaveManager.Instance.Progression;
        
        string stats = "";
        stats += $"<b>Progress</b>\n";
        stats += $"Highest Stage Unlocked: {progression.highestStageUnlocked + 1}\n";
        stats += $"Axiom Shards: {progression.axiomShards}\n";
        stats += "\n";
        
        stats += $"<b>Lifetime Statistics</b>\n";
        stats += $"Total Cubes Captured: {progression.lifetimeCubesCaptured}\n";
        stats += $"Total Cubes Escaped: {progression.lifetimeCubesEscaped}\n";
        stats += $"Stages Completed: {progression.lifetimeStagesCompleted}\n";
        stats += $"Total Play Time: {FormatPlayTime(progression.lifetimePlayTimeSeconds)}\n";
        stats += "\n";
        
        stats += $"<b>Buildings Unlocked</b>\n";
        stats += $"Celestial Atlas: Always\n";
        stats += $"Resonance Alignment: {(progression.resonanceAlignmentUnlocked ? "Yes" : "No")}\n";
        stats += $"Observation Chronicle: {(progression.observationChronicleUnlocked ? "Yes" : "No")}\n";
        
        return stats;
    }
    
    private string FormatPlayTime(float totalSeconds)
    {
        if (totalSeconds < 60)
        {
            return $"{(int)totalSeconds}s";
        }
        else if (totalSeconds < 3600)
        {
            int minutes = (int)(totalSeconds / 60);
            int seconds = (int)(totalSeconds % 60);
            return $"{minutes}m {seconds}s";
        }
        else
        {
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            return $"{hours}h {minutes}m";
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
