// Simple feedback collector
using UnityEngine;

public class FeedbackCollector : MonoBehaviour
{
    [Header("Feedback Settings")]
    [SerializeField] private KeyCode feedbackKey = KeyCode.F12;
    [SerializeField] private string feedbackEmail = "awilliams9293@gmail.com";

    private void Update()
    {
        if (Input.GetKeyDown(feedbackKey))
        {
            OpenFeedbackForm();
        }
    }

    private void OpenFeedbackForm()
    {
        string subject = "Game Feedback - " + Application.productName;
        string body = "Build Version: " + Application.version + "\n\n" +
                     "Feedback:\n\n" +
                     "What did you like?\n\n" +
                     "What was confusing?\n\n" +
                     "Any bugs or issues?\n\n";

        string mailto = $"mailto:{feedbackEmail}?subject={subject}&body={body}";
        Application.OpenURL(mailto);
    }
}