using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// POC: Placeholder UI component for stage transitions.
/// Displays intro and completion screens when stage events are fired.
/// </summary>
public class StageTransitionUI : MonoBehaviour
{
    #region Inspector Configuration
    [Header("UI References")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private Text stageNameText;
    [SerializeField] private Text stageObjectiveText;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Text completionTitleText;
    [SerializeField] private Text completionMessageText;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool EnableDebugLogs;
    #endregion

    #region Runtime State
    private CanvasGroup introCanvasGroup;
    private CanvasGroup completionCanvasGroup;
    private Coroutine currentTransition;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        ValidateReferences();
    }

    private void OnEnable()
    {
        // Subscribe to GameEvents
        GameEvents.OnStageStart += HandleStageStart;
        GameEvents.OnStageComplete += HandleStageComplete;
        
        DebugLog("OnEnable", "Subscribed to stage events");
    }

    private void OnDisable()
    {
        // Unsubscribe from GameEvents (important to prevent memory leaks!)
        GameEvents.OnStageStart -= HandleStageStart;
        GameEvents.OnStageComplete -= HandleStageComplete;
        
        // Stop any running transitions
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
        
        DebugLog("OnDisable", "Unsubscribed from stage events");
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        // Get or add CanvasGroup components for fade animations
        if (introPanel != null)
        {
            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
            if (introCanvasGroup == null)
            {
                introCanvasGroup = introPanel.AddComponent<CanvasGroup>();
            }
            introPanel.SetActive(false);
        }
        
        if (completionPanel != null)
        {
            completionCanvasGroup = completionPanel.GetComponent<CanvasGroup>();
            if (completionCanvasGroup == null)
            {
                completionCanvasGroup = completionPanel.AddComponent<CanvasGroup>();
            }
            completionPanel.SetActive(false);
        }
    }
    
    private void ValidateReferences()
    {
        if (introPanel == null)
            Debug.LogWarning("[StageTransitionUI] IntroPanel not assigned - stage intro won't display");
            
        if (completionPanel == null)
            Debug.LogWarning("[StageTransitionUI] CompletionPanel not assigned - stage completion won't display");
            
        if (stageNameText == null && introPanel != null)
            Debug.LogWarning("[StageTransitionUI] StageNameText not assigned - stage name won't display");
    }
    #endregion

    #region Event Handlers
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        DebugLog("HandleStageStart", $"Stage {stageIndex} started: {stageData?.stageName ?? "Unknown"}");
        
        if (introPanel == null) return;
        
        // Stop any running transition
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        // Update UI content
        if (stageNameText != null)
        {
            stageNameText.text = stageData?.stageName ?? $"Stage {stageIndex + 1}";
        }
        
        if (stageObjectiveText != null)
        {
            // POC: Simple objective display - can be enhanced later
            string objective = GetStageObjective(stageData);
            stageObjectiveText.text = objective;
        }
        
        // Start intro animation
        currentTransition = StartCoroutine(ShowIntroSequence());
    }
    
    private void HandleStageComplete(int stageIndex, bool success)
    {
        DebugLog("HandleStageComplete", $"Stage {stageIndex} completed - Success: {success}");
        
        if (completionPanel == null) return;
        
        // Stop any running transition
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        // Update completion UI content
        if (completionTitleText != null)
        {
            completionTitleText.text = success ? "Stage Complete!" : "Stage Failed";
            completionTitleText.color = success ? Color.green : Color.red;
        }
        
        if (completionMessageText != null)
        {
            // POC: Simple messages - can be enhanced with more details later
            completionMessageText.text = success ? 
                "Well done! Proceeding to next stage..." : 
                "Try again! The stage will restart...";
        }
        
        // Start completion animation
        currentTransition = StartCoroutine(ShowCompletionSequence());
    }
    #endregion

    #region Animation Sequences
    private IEnumerator ShowIntroSequence()
    {
        // Ensure completion panel is hidden
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
        
        // Show and fade in intro panel
        introPanel.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(introCanvasGroup, 0f, 1f, fadeInDuration));
        
        // Display for specified duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out and hide
        yield return StartCoroutine(FadeCanvasGroup(introCanvasGroup, 1f, 0f, fadeOutDuration));
        introPanel.SetActive(false);
        
        currentTransition = null;
    }
    
    private IEnumerator ShowCompletionSequence()
    {
        // Ensure intro panel is hidden
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }
        
        // Show and fade in completion panel
        completionPanel.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 0f, 1f, fadeInDuration));
        
        // Display for specified duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out and hide
        yield return StartCoroutine(FadeCanvasGroup(completionCanvasGroup, 1f, 0f, fadeOutDuration));
        completionPanel.SetActive(false);
        
        currentTransition = null;
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.alpha = fromAlpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = toAlpha;
    }
    #endregion

    #region Helper Methods
    private string GetStageObjective(StageData stageData)
    {
        if (stageData == null) return "Complete the stage!";
        
        // POC: Build simple objective text from stage data
        string objective = "";
        
        if (stageData.requiredCaptureCount > 0)
        {
            objective += $"Capture {stageData.requiredCaptureCount} cubes";
        }
        
        if (stageData.maxAllowedEscapes >= 0)
        {
            if (!string.IsNullOrEmpty(objective)) objective += "\n";
            objective += $"Let no more than {stageData.maxAllowedEscapes} cubes escape";
        }
        
        // TODO: Add more objective types as stage system expands
        
        return string.IsNullOrEmpty(objective) ? "Complete the stage!" : objective;
    }
    
    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
        {
            Debug.Log($"[StageTransitionUI] {methodName}: {message}");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Force hide all UI panels (useful for cleanup or testing)
    /// </summary>
    public void HideAllPanels()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
        
        if (introPanel != null)
        {
            introPanel.SetActive(false);
            if (introCanvasGroup != null) introCanvasGroup.alpha = 0f;
        }
        
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
            if (completionCanvasGroup != null) completionCanvasGroup.alpha = 0f;
        }
        
        DebugLog("HideAllPanels", "All panels hidden");
    }
    
    /// <summary>
    /// Test the intro sequence with dummy data
    /// </summary>
    [ContextMenu("Test Intro Sequence")]
    public void TestIntroSequence()
    {
        var testData = new StageData
        {
            stageName = "Test Stage",
            requiredCaptureCount = 10,
            maxAllowedEscapes = 3
        };
        
        HandleStageStart(0, testData);
    }
    
    /// <summary>
    /// Test the completion sequence
    /// </summary>
    [ContextMenu("Test Success Sequence")]
    public void TestSuccessSequence()
    {
        HandleStageComplete(0, true);
    }
    
    [ContextMenu("Test Failure Sequence")]
    public void TestFailureSequence()
    {
        HandleStageComplete(0, false);
    }
    #endregion
}
