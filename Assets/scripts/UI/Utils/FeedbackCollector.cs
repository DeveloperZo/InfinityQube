// Simple feedback collector
using UnityEngine;

public class FeedbackCollector : MonoBehaviour
{
    [Header("Feedback Settings")]
    [SerializeField] private KeyCode feedbackKey = KeyCode.F10;
    [SerializeField] private string feedbackEmail = "awilliams9293@gmail.com";

    private void Update()
    {
        if (Input.GetKeyDown(feedbackKey))
        {
            TriggerManualStatisticsSave(); 
        }
    }

    private void OpenFeedbackForm()
    {
        // First, trigger a manual statistics save
        TriggerManualStatisticsSave();
        
        string subject = "Game Feedback - " + Application.productName;
        string body = "Build Version: " + Application.version + "\n\n" +
                     "Feedback:\n\n" +
                     "What did you like?\n\n" +
                     "What was confusing?\n\n" +
                     "Any bugs or issues?\n\n" +
                     "\n[player_statistics.json file should be in your Documents/InfinityQube folder]";

        string mailto = $"mailto:{feedbackEmail}?subject={subject}&body={body}";
        Application.OpenURL(mailto);
    }
    
    private void TriggerManualStatisticsSave()
    {
        try
        {
            var statsManager = PlayerStatisticsManager.Instance;
            if (statsManager != null)
            {
                statsManager.ForceManualSave();
                Debug.Log("[FeedbackCollector] Manual statistics save triggered");
            }
            else
            {
                Debug.LogWarning("[FeedbackCollector] PlayerStatisticsManager not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FeedbackCollector] Failed to trigger manual save: {e.Message}");
        }
    }
}